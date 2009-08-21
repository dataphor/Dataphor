/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define USEINCLUDENILSWITHBROWSE
#define UseReferenceDerivation
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public abstract class BaseOrderNode : UnaryTableNode
    {
		protected Schema.Order FRequestedOrder;
		public Schema.Order RequestedOrder
		{
			get { return FRequestedOrder; }
			set { FRequestedOrder = value; }
		}
		
		protected CursorCapability FRequestedCapabilities;
		public CursorCapability RequestedCapabilities
		{
			get { return FRequestedCapabilities; }
			set { FRequestedCapabilities = value; }
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return Nodes[0].EmitStatement(AMode);
		}
		
		public override void InferPopulateNode(Plan APlan)
		{
			if (SourceNode.PopulateNode != null)
				FPopulateNode = SourceNode.PopulateNode;
		}
		
		protected bool FIsAccelerator;
		public bool IsAccelerator
		{
			get { return FIsAccelerator; }
			set { FIsAccelerator = value; }
		}
		
		public override void DetermineDataType(Plan APlan)		 
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);

			CopyTableVarColumns(SourceTableVar.Columns);
			
			DetermineRemotable(APlan);

			CopyKeys(SourceTableVar.Keys);
			CopyOrders(SourceTableVar.Orders);
			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif
			
			DetermineOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public virtual void DetermineOrder(Plan APlan)
		{
			// Set up the order columns
			Order = new Schema.Order(FRequestedOrder.MetaData);
			Order.IsInherited = false;

			Schema.OrderColumn LNewColumn;			
			Schema.OrderColumn LColumn;
			for (int LIndex = 0; LIndex < FRequestedOrder.Columns.Count; LIndex++)
			{
				LColumn = FRequestedOrder.Columns[LIndex];
				LNewColumn = new Schema.OrderColumn(TableVar.Columns[LColumn.Column], LColumn.Ascending, LColumn.IncludeNils);
				LNewColumn.Sort = LColumn.Sort;
				LNewColumn.IsDefaultSort = LColumn.IsDefaultSort;
				Error.AssertWarn(LNewColumn.Sort != null, "Sort is null");
				if (LNewColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LNewColumn.Sort.Dependencies);
				Order.Columns.Add(LNewColumn);
			}
			
			Compiler.EnsureOrderUnique(APlan, TableVar, Order);
		}
    }
    
	// operator iOrder(presentation{}) : presentation{}    
    public class OrderNode : BaseOrderNode
    {
		protected int FSequenceColumnIndex = -1;
		public int SequenceColumnIndex { get { return FSequenceColumnIndex; } }
		
		protected IncludeColumnExpression FSequenceColumn;
		public IncludeColumnExpression SequenceColumn
		{
			get { return FSequenceColumn; }
			set { FSequenceColumn = value; }
		}
		
		// physical access path used by the order node when device supported
		protected Schema.Order FPhysicalOrder;
		public Schema.Order PhysicalOrder 
		{ 
			get { return FPhysicalOrder; } 
			set { FPhysicalOrder = value; } 
		}
		
		// direction of the physical access path usage
		protected ScanDirection FScanDirection;
		public ScanDirection ScanDirection 
		{ 
			get { return FScanDirection; } 
			set { FScanDirection = value; } 
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);

			if (FSequenceColumn != null)
			{
				Schema.TableVarColumn LColumn =
					Compiler.CompileIncludeColumnExpression
					(
						APlan,
						FSequenceColumn,
						Keywords.Sequence,
						APlan.Catalog.DataTypes.SystemInteger,
						Schema.TableVarColumnType.Sequence
					);
				DataType.Columns.Add(LColumn.Column);
				TableVar.Columns.Add(LColumn);
				FSequenceColumnIndex = TableVar.Columns.Count - 1;

				Schema.Key LSequenceKey = new Schema.Key();
				LSequenceKey.IsInherited = true;
				LSequenceKey.Columns.Add(LColumn);
				TableVar.Keys.Add(LSequenceKey);
			}
		}
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorIsolation = APlan.CursorContext.CursorIsolation;

			if (ShouldExecute())
			{
				FCursorType = CursorType.Static;
				FCursorCapabilities = 
					CursorCapability.Navigable | 
					CursorCapability.BackwardsNavigable |
					CursorCapability.Bookmarkable |
					CursorCapability.Searchable |
					CursorCapability.Countable |
					(
						(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
						(SourceNode.CursorCapabilities & CursorCapability.Updateable)
					);
			}
			else
			{
				FCursorType = SourceNode.CursorType;
				FCursorCapabilities = SourceNode.CursorCapabilities;
			}
		}
		
		private bool ShouldExecute()
		{
			// Only execute the order if it is required
			// i.e., if the source node has no ordering, or the source node's ordering is not equivalent to this order, or there is some capability requested of this node that is not provided by the source node
			return (SourceNode.Order == null) || !Order.Equivalent(SourceNode.Order) || ((SourceNode.CursorCapabilities & RequestedCapabilities) != RequestedCapabilities);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (ShouldExecute())
			{
				OrderTable LTable = new OrderTable(this, AProcess);
				try
				{
					LTable.Open();
					return LTable;
				}
				catch
				{
					LTable.Dispose();
					throw;
				}
			}
			
			return SourceNode.Execute(AProcess);
		}
		
		public override void JoinApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			if (FSequenceColumnIndex >= 0)
			{
				// Exclude any columns from AKey which were included by this node
				Schema.RowType LRowType = new Schema.RowType();
				foreach (Schema.Column LColumn in ARow.DataType.Columns)
					if (SourceNode.DataType.Columns.ContainsName(LColumn.Name))
						LRowType.Columns.Add(LColumn.Copy());

				Row LRow = new Row(AProcess, LRowType);
				try
				{
					ARow.CopyTo(LRow);
					SourceNode.JoinApplicationTransaction(AProcess, LRow);
				}
				finally
				{
					LRow.Dispose();
				}
			}
			else
				base.JoinApplicationTransaction(AProcess, ARow);
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			if (IsAccelerator)
				return Nodes[0].EmitStatement(AMode);
			else
			{
				OrderExpression LOrderExpression = new OrderExpression();
				LOrderExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
				for (int LIndex = 0; LIndex < RequestedOrder.Columns.Count; LIndex++)
					LOrderExpression.Columns.Add(RequestedOrder.Columns[LIndex].EmitStatement(AMode));
				return LOrderExpression;
			}
		}
    }

	// operator iCopy(const AValue : table) : table
	// operator iCopy(const AValue : presentation) : presentation
    public class CopyNode : BaseOrderNode // Internal node used to materialize an intermediate result set, used to ensure static cursors and certain cursor capabilities like countable
    {
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = CursorType.Static;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable | 
				CursorCapability.BackwardsNavigable |
				CursorCapability.Bookmarkable |
				CursorCapability.Searchable |
				CursorCapability.Countable |
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			CopyTable LTable = new CopyTable(this, AProcess);
			try
			{
				LTable.Open();
				return LTable;
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}
    }
    
	public class BrowseVariant
	{
		public BrowseVariant() : base(){}
		public BrowseVariant(PlanNode ANode, int AOriginIndex, bool AForward, bool AInclusive) : base()
		{
			FNode = ANode;
			FOriginIndex = AOriginIndex;
			FForward = AForward;
			FInclusive = AInclusive;
		}
		
		private PlanNode FNode;
		public PlanNode Node { get { return FNode; } set { FNode = value; } }
		
		private int FOriginIndex = -1;
		public int OriginIndex { get { return FOriginIndex; } set { FOriginIndex = value; } }
		
		private bool FForward;
		public bool Forward { get { return FForward; } set { FForward = value; } }
		
		private bool FInclusive;
		public bool Inclusive { get { return FInclusive; } set { FInclusive = value; } }
	}
	
	public class BrowseVariants : List
	{		
		public new BrowseVariant this[int AIndex]
		{
			get { return (BrowseVariant)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public BrowseVariant this[int AOriginIndex, bool AForward, bool AInclusive]
		{
			get
			{
				int LIndex = IndexOf(AOriginIndex, AForward, AInclusive);
				if (LIndex >= 0)
					return this[LIndex];
				throw new RuntimeException(RuntimeException.Codes.BrowseVariantNotFound, AOriginIndex.ToString(), AForward.ToString(), AInclusive.ToString());
			}
		}
		
		public int IndexOf(int AOriginIndex, bool AForward, bool AInclusive)
		{
			BrowseVariant LBrowseVariant;
			for (int LIndex = 0; LIndex < Count; LIndex++)
			{
				LBrowseVariant = this[LIndex];
				if ((LBrowseVariant.OriginIndex == AOriginIndex) && (LBrowseVariant.Forward == AForward) && (LBrowseVariant.Inclusive == AInclusive))
					return LIndex;
			}
			return -1;
		}
	}
	
    public class BrowseNode : BaseOrderNode
    {
		public BrowseNode() : base()
		{
			IgnoreUnsupported = true;
		}
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = SourceNode.CursorType;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable | 
				CursorCapability.BackwardsNavigable |
				CursorCapability.Bookmarkable |
				CursorCapability.Searchable |
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
		}
		
		public override void DetermineDevice(Plan APlan)
		{
			base.DetermineDevice(APlan);
			if ((FCursorCapabilities & CursorCapability.Updateable) == 0)
				FSymbols = Compiler.SnapshotSymbols(APlan);
		}
		
		public override void BindToProcess(Plan APlan)
		{
			foreach (BrowseVariant LVariant in FBrowseVariants)
				LVariant.Node.BindToProcess(APlan);
			base.BindToProcess(APlan);
		}
		
		/*
			for each column in the order descending
				if the current order column is also in the origin
					[or]
					for each column in the origin less than the current order column
						[and] 
						current origin column = current origin value
                        
					[and]
					if the current order column is ascending xor the requested set is forward
						if requested set is inclusive and current order column is the last origin column
							current order column <= current origin value
						else
							current order column < current origin value
					else
						if requested set is inclusive and the current order column is the last origin column
							current order column >= current origin value
						else
							current order column > current origin value
                            
					for each column in the order greater than the current order column
						if the current order column does not include nulls
							and current order column is not null
				else
					if the current order column does not include nulls
						[and] current order column is not null
		*/
		
		protected PlanNode EmitBrowseColumnNode(Plan APlan, Schema.OrderColumn AColumn, string AInstruction)
		{
			PlanNode LNode = 
				Compiler.EmitBinaryNode
				(
					APlan, 
					Compiler.EmitIdentifierNode(APlan, AColumn.Column.Name), 
					AInstruction, 
					Compiler.EmitIdentifierNode(APlan, Schema.Object.Qualify(AColumn.Column.Name, Keywords.Origin))
				);
				
			if (AColumn.Column.IsNilable && AColumn.IncludeNils)
			{
				switch (AInstruction)
				{
					case Instructions.Equal :
						LNode =
							Compiler.EmitBinaryNode
							(
								APlan,
								Compiler.EmitBinaryNode
								(
									APlan,
									EmitBrowseNilNode(APlan, AColumn.Column, true),
									Instructions.And,
									EmitOriginNilNode(APlan, AColumn.Column, true)
								),
								Instructions.Or,
								LNode
							);
					break;
					
					case Instructions.InclusiveGreater :
						LNode =
							Compiler.EmitBinaryNode
							(
								APlan,
								EmitOriginNilNode(APlan, AColumn.Column, true),
								Instructions.Or,
								LNode
							);
					break;
					
					case Instructions.Greater :
						LNode =
							Compiler.EmitBinaryNode
							(
								APlan,
								Compiler.EmitBinaryNode
								(
									APlan,
									EmitOriginNilNode(APlan, AColumn.Column, true),
									Instructions.And,
									EmitBrowseNilNode(APlan, AColumn.Column, false)
								),
								Instructions.Or,
								LNode
							);
					break;
					
					case Instructions.InclusiveLess :
						LNode =
							Compiler.EmitBinaryNode
							(
								APlan,
								EmitBrowseNilNode(APlan, AColumn.Column, true),
								Instructions.Or,
								LNode
							);
					break;

					case Instructions.Less :
						LNode =
							Compiler.EmitBinaryNode
							(
								APlan,
								Compiler.EmitBinaryNode
								(
									APlan,
									EmitBrowseNilNode(APlan, AColumn.Column, true),
									Instructions.And,
									EmitOriginNilNode(APlan, AColumn.Column, false)
								),
								Instructions.Or,
								LNode
							);
					break;
				}
			}
			
			return LNode;
		}

		protected PlanNode EmitBrowseNilNode(Plan APlan, Schema.TableVarColumn AColumn, bool AIsNil)
		{
			if (AIsNil)
			{
				return 
					Compiler.EmitCallNode
					(
						APlan, 
						"IsNil",
						new PlanNode[]{Compiler.EmitIdentifierNode(APlan, AColumn.Name)}
					);
			}
			else
			{
				return 
					Compiler.EmitUnaryNode
					(
						APlan,
						Instructions.Not,
						EmitBrowseNilNode(APlan, AColumn, true)
					);
			}
		}
		
		protected PlanNode EmitOriginNilNode(Plan APlan, Schema.TableVarColumn AColumn, bool AIsNil)
		{
			if (AIsNil)
				return
					Compiler.EmitCallNode
					(
						APlan, 
						"IsNil", 
						new PlanNode[]{Compiler.EmitIdentifierNode(APlan, Schema.Object.Qualify(AColumn.Name, Keywords.Origin))}
					);
			else
				return
					Compiler.EmitUnaryNode
					(
						APlan,
						Instructions.Not,
						EmitOriginNilNode(APlan, AColumn, true)
					);
		}
        
		protected PlanNode EmitBrowseComparisonNode
		(
			Plan APlan,
			Schema.Order AOrder, 
			bool AForward, 
			bool AInclusive, 
			int AOriginIndex
		)
		{
			PlanNode LNode = null;
			Schema.OrderColumn LOriginColumn = AOrder.Columns[AOriginIndex];
			if (LOriginColumn.Ascending != AForward)
			{
				if (AInclusive && (AOriginIndex == AOrder.Columns.Count - 1))
					LNode = EmitBrowseColumnNode(APlan, LOriginColumn, Instructions.InclusiveLess);
				else
					LNode = EmitBrowseColumnNode(APlan, LOriginColumn, Instructions.Less);
			}
			else
			{
				if (AInclusive && (AOriginIndex == AOrder.Columns.Count - 1))
					LNode = EmitBrowseColumnNode(APlan, LOriginColumn, Instructions.InclusiveGreater);
				else
					LNode = EmitBrowseColumnNode(APlan, LOriginColumn, Instructions.Greater);
			}
			return LNode;
		}
        
		protected PlanNode EmitBrowseOriginNode
		(
			Plan APlan,
			Schema.Order AOrder, 
			bool AForward, 
			bool AInclusive, 
			int AOriginIndex
		)
		{
			PlanNode LNode = null;
			for (int LIndex = 0; LIndex < AOriginIndex; LIndex++)
			{
				PlanNode LEqualNode = EmitBrowseColumnNode(APlan, AOrder.Columns[LIndex], Instructions.Equal);
						
				LNode = Compiler.AppendNode(APlan, LNode, Instructions.And, LEqualNode);
			}
			
			LNode = 
				Compiler.AppendNode
				(
					APlan, 
					LNode, 
					Instructions.And, 
					EmitBrowseComparisonNode(APlan, AOrder, AForward, AInclusive, AOriginIndex)
				);
			
			for (int LIndex = AOriginIndex + 1; LIndex < AOrder.Columns.Count; LIndex++)
				if (AOrder.Columns[LIndex].Column.IsNilable && !AOrder.Columns[LIndex].IncludeNils)
					LNode = 
						Compiler.AppendNode
						(
							APlan, 
							LNode, 
							Instructions.And, 
							EmitBrowseNilNode(APlan, AOrder.Columns[LIndex].Column, false)
						);
			return LNode;
		}
        
		protected PlanNode EmitBrowseConditionNode
		(
			Plan APlan,
			Schema.Order AOrder, 
			Schema.IRowType AOrigin, 
			bool AForward, 
			bool AInclusive
		)
		{
			PlanNode LNode = null;
			for (int LOrderIndex = AOrder.Columns.Count - 1; LOrderIndex >= 0; LOrderIndex--)
			{
				if ((AOrigin != null) && (LOrderIndex < AOrigin.Columns.Count))
					LNode = 
						Compiler.AppendNode
						(
							APlan, 
							LNode, 
							Instructions.Or, 
							EmitBrowseOriginNode(APlan, AOrder, AForward, AInclusive, LOrderIndex)
						);
				else
					#if USEINCLUDENILSWITHBROWSE
					if (AOrder.Columns[LOrderIndex].Column.IsNilable && !AOrder.Columns[LOrderIndex].IncludeNils)
					#endif
					{
						LNode = 
							Compiler.AppendNode
							(
								APlan, 
								LNode, 
								Instructions.And, 
								EmitBrowseNilNode(APlan, AOrder.Columns[LOrderIndex].Column, false)
							);
					}
			}

			if (LNode == null)
				LNode = new ValueNode(APlan.ServerProcess.DataTypes.SystemBoolean, AInclusive);

			return LNode;
		}
		
		protected BrowseVariants FBrowseVariants = new BrowseVariants();
		public BrowseVariants BrowseVariants { get { return FBrowseVariants; } }
		
		protected PlanNode EmitBrowseVariantNode(Plan APlan, int AOriginIndex, bool AForward, bool AInclusive)
		{
			Schema.Order LOriginOrder = new Schema.Order();
			for (int LIndex = 0; LIndex <= AOriginIndex; LIndex++)
				LOriginOrder.Columns.Add(new Schema.OrderColumn(Order.Columns[LIndex].Column, Order.Columns[LIndex].Ascending, Order.Columns[LIndex].IncludeNils));
			Schema.IRowType LOrigin = new Schema.RowType(LOriginOrder.Columns, Keywords.Origin);
			APlan.PushCursorContext(new CursorContext(CursorType, CursorCapabilities & ~(CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Countable), CursorIsolation));
			try
			{
				APlan.EnterRowContext();
				try
				{
					APlan.Symbols.Push(new Symbol(LOrigin));
					try
					{
						PlanNode LResultNode;
						PlanNode LSourceNode = GetSourceNode(APlan);
						APlan.Symbols.Push(new Symbol(DataType.RowType));
						try
						{
							LResultNode =
								Compiler.EmitOrderNode
								(
									APlan,
									Compiler.EmitRestrictNode
									(
										APlan,
										LSourceNode,
										EmitBrowseConditionNode
										(
											APlan,
											AOriginIndex < 0 ? Order : LOriginOrder,
											AOriginIndex < 0 ? null : LOrigin,
											AForward,
											AInclusive
										)
									),
									new Schema.Order(Order, !AForward),
									true
								);
						}
						finally
						{
							APlan.Symbols.Pop();
						}

						LResultNode = Compiler.OptimizeNode(APlan, LResultNode);
						LResultNode = Compiler.BindNode(APlan, LResultNode);
						return LResultNode;
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.ExitRowContext();
				}
			}
			finally
			{
				APlan.PopCursorContext();
			}
		}
		
		private Expression FSourceExpression;
		private void EnsureSourceExpression()
		{
			if (FSourceExpression == null)
				FSourceExpression = (Expression)SourceNode.EmitStatement(EmitMode.ForCopy);
		}
		
		private PlanNode FSourceNode;		
		private PlanNode GetSourceNode(Plan APlan)
		{
			if (FSourceNode == null)
				FSourceNode = Compiler.CompileExpression(APlan, FSourceExpression);
			return FSourceNode;
		}
		
		public bool HasBrowseVariant(int AOriginIndex, bool AForward, bool AInclusive)
		{
			return FBrowseVariants.IndexOf(AOriginIndex, AForward, AInclusive) >= 0;
		}
		
		public void CompileBrowseVariant(ServerProcess AProcess, int AOriginIndex, bool AForward, bool AInclusive)
		{
			ServerStatementPlan LServerPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LServerPlan);
				try
				{
					LServerPlan.Plan.PushATCreationContext();
					try
					{
						PushSymbols(LServerPlan.Plan, FSymbols);
						try
						{
							EnsureSourceExpression();
							FBrowseVariants.Add
							(
								new BrowseVariant
								(
									EmitBrowseVariantNode
									(
										LServerPlan.Plan, 
										AOriginIndex, 
										AForward, 
										AInclusive
									), 
									AOriginIndex, 
									AForward, 
									AInclusive
								)
							);
						}
						finally
						{
							PopSymbols(LServerPlan.Plan, FSymbols);
						}
					}
					finally
					{
						LServerPlan.Plan.PopATCreationContext();
					}
				}
				finally
				{
					AProcess.PopExecutingPlan(LServerPlan);
				}
			}
			finally
			{
				LServerPlan.Dispose();
			}
		}
		
		public PlanNode GetBrowseVariantNode(Plan APlan, int AOriginIndex, bool AForward, bool AInclusive)
		{
			return FBrowseVariants[AOriginIndex, AForward, AInclusive].Node;
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			BrowseTable LTable = new BrowseTable(this, AProcess);
			try
			{
				LTable.Open();
				return LTable;
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (IsAccelerator)
				return Nodes[0].EmitStatement(AMode);
			else
			{
				BrowseExpression LBrowseExpression = new BrowseExpression();
				LBrowseExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
				for (int LIndex = 0; LIndex < RequestedOrder.Columns.Count; LIndex++)
					LBrowseExpression.Columns.Add(RequestedOrder.Columns[LIndex].EmitStatement(AMode));
				return LBrowseExpression;
			}
		}
	}
}