/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define USEINCLUDENILSWITHBROWSE
#define UseReferenceDerivation
#define UseElaborable
	
using System;
using System.Linq;
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
		protected Schema.Order _requestedOrder;
		public Schema.Order RequestedOrder
		{
			get { return _requestedOrder; }
			set { _requestedOrder = value; }
		}
		
		protected CursorCapability _requestedCapabilities;
		public CursorCapability RequestedCapabilities
		{
			get { return _requestedCapabilities; }
			set { _requestedCapabilities = value; }
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return Nodes[0].EmitStatement(mode);
		}
		
		public override void InferPopulateNode(Plan plan)
		{
			if (SourceNode.PopulateNode != null)
				_populateNode = SourceNode.PopulateNode;
		}
		
		protected bool _isAccelerator;
		public bool IsAccelerator
		{
			get { return _isAccelerator; }
			set { _isAccelerator = value; }
		}
		
		public override void DetermineDataType(Plan plan)		 
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);

			CopyTableVarColumns(SourceTableVar.Columns);
			
			DetermineRemotable(plan);

			CopyKeys(SourceTableVar.Keys);
			CopyOrders(SourceTableVar.Orders);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
			
			DetermineOrder(plan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public virtual void DetermineOrder(Plan plan)
		{
			// Set up the order columns
			Order = new Schema.Order(_requestedOrder.MetaData);
			Order.IsInherited = false;

			Schema.OrderColumn newColumn;			
			Schema.OrderColumn column;
			for (int index = 0; index < _requestedOrder.Columns.Count; index++)
			{
				column = _requestedOrder.Columns[index];
				newColumn = new Schema.OrderColumn(TableVar.Columns[column.Column], column.Ascending, column.IncludeNils);
				newColumn.Sort = column.Sort;
				newColumn.IsDefaultSort = column.IsDefaultSort;
				Error.AssertWarn(newColumn.Sort != null, "Sort is null");
				if (newColumn.IsDefaultSort)
					plan.AttachDependency(newColumn.Sort);
				else
				{
					if (newColumn.Sort.HasDependencies())
						plan.AttachDependencies(newColumn.Sort.Dependencies);
				}
				Order.Columns.Add(newColumn);
			}
			
			Compiler.EnsureOrderUnique(plan, TableVar, Order);
		}
    }
    
	// operator iOrder(presentation{}) : presentation{}    
    public class OrderNode : BaseOrderNode
    {
		protected int _sequenceColumnIndex = -1;
		public int SequenceColumnIndex { get { return _sequenceColumnIndex; } }
		
		protected IncludeColumnExpression _sequenceColumn;
		public IncludeColumnExpression SequenceColumn
		{
			get { return _sequenceColumn; }
			set { _sequenceColumn = value; }
		}
		
		// physical access path used by the order node when device supported
		protected Schema.Order _physicalOrder;
		public Schema.Order PhysicalOrder 
		{ 
			get { return _physicalOrder; } 
			set { _physicalOrder = value; } 
		}
		
		// direction of the physical access path usage
		protected ScanDirection _scanDirection;
		public ScanDirection ScanDirection 
		{ 
			get { return _scanDirection; } 
			set { _scanDirection = value; } 
		}
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);

			if (_sequenceColumn != null)
			{
				Schema.TableVarColumn column =
					Compiler.CompileIncludeColumnExpression
					(
						plan,
						_sequenceColumn,
						Keywords.Sequence,
						plan.DataTypes.SystemInteger,
						Schema.TableVarColumnType.Sequence
					);
				DataType.Columns.Add(column.Column);
				TableVar.Columns.Add(column);
				_sequenceColumnIndex = TableVar.Columns.Count - 1;

				Schema.Key sequenceKey = new Schema.Key();
				sequenceKey.IsInherited = true;
				sequenceKey.Columns.Add(column);
				TableVar.Keys.Add(sequenceKey);
			}
		}
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorIsolation = plan.CursorContext.CursorIsolation;

			if (ShouldExecute())
			{
				_cursorType = CursorType.Static;
				_cursorCapabilities = 
					CursorCapability.Navigable | 
					CursorCapability.BackwardsNavigable |
					CursorCapability.Bookmarkable |
					CursorCapability.Searchable |
					CursorCapability.Countable |
					(
						(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
						(SourceNode.CursorCapabilities & CursorCapability.Updateable)
					) |
					(
						plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
					);
			}
			else
			{
				_cursorType = SourceNode.CursorType;
				_cursorCapabilities = SourceNode.CursorCapabilities;
			}
		}
		
		private bool ShouldExecute()
		{
			// Only execute the order if it is required
			// i.e., if the source node has no ordering, or the source node's ordering is not equivalent to this order, or there is some capability requested of this node that is not provided by the source node
			return (SourceNode.Order == null) || !Order.Equivalent(SourceNode.Order) || ((SourceNode.CursorCapabilities & RequestedCapabilities) != RequestedCapabilities);
		}
		
		public override object InternalExecute(Program program)
		{
			if (ShouldExecute())
			{
				OrderTable table = new OrderTable(this, program);
				try
				{
					table.Open();
					return table;
				}
				catch
				{
					table.Dispose();
					throw;
				}
			}
			
			return SourceNode.Execute(program);
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			if (_sequenceColumnIndex >= 0)
			{
				// Exclude any columns from AKey which were included by this node
				Schema.RowType rowType = new Schema.RowType();
				foreach (Schema.Column column in row.DataType.Columns)
					if (SourceNode.DataType.Columns.ContainsName(column.Name))
						rowType.Columns.Add(column.Copy());

				Row localRow = new Row(program.ValueManager, rowType);
				try
				{
					row.CopyTo(localRow);
					SourceNode.JoinApplicationTransaction(program, localRow);
				}
				finally
				{
					localRow.Dispose();
				}
			}
			else
				base.JoinApplicationTransaction(program, row);
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			if (IsAccelerator)
				return Nodes[0].EmitStatement(mode);
			else
			{
				OrderExpression orderExpression = new OrderExpression();
				orderExpression.Expression = (Expression)Nodes[0].EmitStatement(mode);
				for (int index = 0; index < RequestedOrder.Columns.Count; index++)
					orderExpression.Columns.Add(RequestedOrder.Columns[index].EmitStatement(mode));
				return orderExpression;
			}
		}
    }

	// operator iCopy(const AValue : table) : table
	// operator iCopy(const AValue : presentation) : presentation
    public class CopyNode : BaseOrderNode // Internal node used to materialize an intermediate result set, used to ensure static cursors and certain cursor capabilities like countable
    {
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = CursorType.Static;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable | 
				CursorCapability.BackwardsNavigable |
				CursorCapability.Bookmarkable |
				CursorCapability.Searchable |
				CursorCapability.Countable |
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;
		}
		
		public override object InternalExecute(Program program)
		{
			CopyTable table = new CopyTable(this, program);
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}
    }
    
	public class BrowseVariant
	{
		public BrowseVariant() : base(){}
		public BrowseVariant(PlanNode node, int originIndex, bool forward, bool inclusive) : base()
		{
			_node = node;
			_originIndex = originIndex;
			_forward = forward;
			_inclusive = inclusive;
		}
		
		private PlanNode _node;
		public PlanNode Node { get { return _node; } set { _node = value; } }
		
		private int _originIndex = -1;
		public int OriginIndex { get { return _originIndex; } set { _originIndex = value; } }
		
		private bool _forward;
		public bool Forward { get { return _forward; } set { _forward = value; } }
		
		private bool _inclusive;
		public bool Inclusive { get { return _inclusive; } set { _inclusive = value; } }
	}
	
	public class BrowseVariants : List
	{		
		public new BrowseVariant this[int index]
		{
			get { return (BrowseVariant)base[index]; }
			set { base[index] = value; }
		}
		
		public BrowseVariant this[int originIndex, bool forward, bool inclusive]
		{
			get
			{
				int index = IndexOf(originIndex, forward, inclusive);
				if (index >= 0)
					return this[index];
				throw new RuntimeException(RuntimeException.Codes.BrowseVariantNotFound, originIndex.ToString(), forward.ToString(), inclusive.ToString());
			}
		}
		
		public int IndexOf(int originIndex, bool forward, bool inclusive)
		{
			BrowseVariant browseVariant;
			for (int index = 0; index < Count; index++)
			{
				browseVariant = this[index];
				if ((browseVariant.OriginIndex == originIndex) && (browseVariant.Forward == forward) && (browseVariant.Inclusive == inclusive))
					return index;
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
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = SourceNode.CursorType;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable | 
				CursorCapability.BackwardsNavigable |
				CursorCapability.Bookmarkable |
				CursorCapability.Searchable |
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;
		}
		
		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			if ((_cursorCapabilities & CursorCapability.Updateable) == 0)
				_symbols = Compiler.SnapshotSymbols(plan);
		}
		
		public override void BindToProcess(Plan plan)
		{
			foreach (BrowseVariant variant in _browseVariants)
				variant.Node.BindToProcess(plan);
			base.BindToProcess(plan);
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

		protected Expression EmitBrowseColumnExpression(Schema.OrderColumn column, string instruction)
		{
			Expression expression = 
				new BinaryExpression
				(
					new IdentifierExpression(column.Column.Name),
					instruction, 
					new IdentifierExpression(Schema.Object.Qualify(column.Column.Name, Keywords.Origin))
				);
				
			if (column.Column.IsNilable && column.IncludeNils)
			{
				switch (instruction)
				{
					case Instructions.Equal :
						expression =
							new BinaryExpression
							(
								new BinaryExpression
								(
									EmitBrowseNilExpression(column.Column, true),
									Instructions.And,
									EmitOriginNilExpression(column.Column, true)
								),
								Instructions.Or,
								expression
							);
					break;
					
					case Instructions.InclusiveGreater :
						expression =
							new BinaryExpression
							(
								EmitOriginNilExpression(column.Column, true),
								Instructions.Or,
								expression
							);
					break;
					
					case Instructions.Greater :
						expression =
							new BinaryExpression
							(
								new BinaryExpression
								(
									EmitOriginNilExpression(column.Column, true),
									Instructions.And,
									EmitBrowseNilExpression(column.Column, false)
								),
								Instructions.Or,
								expression
							);
					break;
					
					case Instructions.InclusiveLess :
						expression =
							new BinaryExpression
							(
								EmitBrowseNilExpression(column.Column, true),
								Instructions.Or,
								expression
							);
					break;

					case Instructions.Less :
						expression =
							new BinaryExpression
							(
								new BinaryExpression
								(
									EmitBrowseNilExpression(column.Column, true),
									Instructions.And,
									EmitOriginNilExpression(column.Column, false)
								),
								Instructions.Or,
								expression
							);
					break;
				}
			}
			
			return expression;
		}
		
		protected PlanNode EmitBrowseColumnNode(Plan plan, Schema.OrderColumn column, string instruction)
		{
			PlanNode node = 
				Compiler.EmitBinaryNode
				(
					plan, 
					Compiler.EmitIdentifierNode(plan, column.Column.Name), 
					instruction, 
					Compiler.EmitIdentifierNode(plan, Schema.Object.Qualify(column.Column.Name, Keywords.Origin))
				);
				
			if (column.Column.IsNilable && column.IncludeNils)
			{
				switch (instruction)
				{
					case Instructions.Equal :
						node =
							Compiler.EmitBinaryNode
							(
								plan,
								Compiler.EmitBinaryNode
								(
									plan,
									EmitBrowseNilNode(plan, column.Column, true),
									Instructions.And,
									EmitOriginNilNode(plan, column.Column, true)
								),
								Instructions.Or,
								node
							);
					break;
					
					case Instructions.InclusiveGreater :
						node =
							Compiler.EmitBinaryNode
							(
								plan,
								EmitOriginNilNode(plan, column.Column, true),
								Instructions.Or,
								node
							);
					break;
					
					case Instructions.Greater :
						node =
							Compiler.EmitBinaryNode
							(
								plan,
								Compiler.EmitBinaryNode
								(
									plan,
									EmitOriginNilNode(plan, column.Column, true),
									Instructions.And,
									EmitBrowseNilNode(plan, column.Column, false)
								),
								Instructions.Or,
								node
							);
					break;
					
					case Instructions.InclusiveLess :
						node =
							Compiler.EmitBinaryNode
							(
								plan,
								EmitBrowseNilNode(plan, column.Column, true),
								Instructions.Or,
								node
							);
					break;

					case Instructions.Less :
						node =
							Compiler.EmitBinaryNode
							(
								plan,
								Compiler.EmitBinaryNode
								(
									plan,
									EmitBrowseNilNode(plan, column.Column, true),
									Instructions.And,
									EmitOriginNilNode(plan, column.Column, false)
								),
								Instructions.Or,
								node
							);
					break;
				}
			}
			
			return node;
		}

		protected Expression EmitBrowseNilExpression(Schema.TableVarColumn column, bool isNil)
		{
			if (isNil)
			{
				return new CallExpression("IsNil", new Expression[] { new IdentifierExpression(column.Name) });
			}
			else
			{
				return new UnaryExpression(Instructions.Not, EmitBrowseNilExpression(column, true));
			}
		}

		protected PlanNode EmitBrowseNilNode(Plan plan, Schema.TableVarColumn column, bool isNil)
		{
			if (isNil)
			{
				return 
					Compiler.EmitCallNode
					(
						plan, 
						"IsNil",
						new PlanNode[]{Compiler.EmitIdentifierNode(plan, column.Name)}
					);
			}
			else
			{
				return 
					Compiler.EmitUnaryNode
					(
						plan,
						Instructions.Not,
						EmitBrowseNilNode(plan, column, true)
					);
			}
		}

		protected Expression EmitOriginNilExpression(Schema.TableVarColumn column, bool isNil)
		{
			if (isNil)
				return new CallExpression("IsNil", new Expression[] { new IdentifierExpression(Schema.Object.Qualify(column.Name, Keywords.Origin)) });
			else
				return new UnaryExpression(Instructions.Not, EmitOriginNilExpression(column, true));
		}
		
		protected PlanNode EmitOriginNilNode(Plan plan, Schema.TableVarColumn column, bool isNil)
		{
			if (isNil)
				return
					Compiler.EmitCallNode
					(
						plan, 
						"IsNil", 
						new PlanNode[]{Compiler.EmitIdentifierNode(plan, Schema.Object.Qualify(column.Name, Keywords.Origin))}
					);
			else
				return
					Compiler.EmitUnaryNode
					(
						plan,
						Instructions.Not,
						EmitOriginNilNode(plan, column, true)
					);
		}

		protected Expression EmitBrowseComparisonExpression
		(
			Schema.Order order,
			bool forward,
			bool inclusive,
			int originIndex
		)
		{
			Expression expression = null;
			Schema.OrderColumn originColumn = order.Columns[originIndex];
			if (originColumn.Ascending != forward)
			{
				if (inclusive && (originIndex == order.Columns.Count - 1))
					expression = EmitBrowseColumnExpression(originColumn, Instructions.InclusiveLess);
				else
					expression = EmitBrowseColumnExpression(originColumn, Instructions.Less);
			}
			else
			{
				if (inclusive && (originIndex == order.Columns.Count - 1))
					expression = EmitBrowseColumnExpression(originColumn, Instructions.InclusiveGreater);
				else
					expression = EmitBrowseColumnExpression(originColumn, Instructions.Greater);
			}

			return expression;
		}
        
		protected PlanNode EmitBrowseComparisonNode
		(
			Plan plan,
			Schema.Order order, 
			bool forward, 
			bool inclusive, 
			int originIndex
		)
		{
			PlanNode node = null;
			Schema.OrderColumn originColumn = order.Columns[originIndex];
			if (originColumn.Ascending != forward)
			{
				if (inclusive && (originIndex == order.Columns.Count - 1))
					node = EmitBrowseColumnNode(plan, originColumn, Instructions.InclusiveLess);
				else
					node = EmitBrowseColumnNode(plan, originColumn, Instructions.Less);
			}
			else
			{
				if (inclusive && (originIndex == order.Columns.Count - 1))
					node = EmitBrowseColumnNode(plan, originColumn, Instructions.InclusiveGreater);
				else
					node = EmitBrowseColumnNode(plan, originColumn, Instructions.Greater);
			}
			return node;
		}

		protected Expression EmitBrowseOriginExpression
		(
			Schema.Order order,
			bool forward,
			bool inclusive,
			int originIndex
		)
		{
			Expression expression = null;
			for (int index = 0; index < originIndex; index++)
			{
				Expression equalExpression = EmitBrowseColumnExpression(order.Columns[index], Instructions.Equal);
						
				expression = AppendExpression(expression, Instructions.And, equalExpression);
			}
			
			expression = 
				AppendExpression
				(
					expression, 
					Instructions.And, 
					EmitBrowseComparisonExpression(order, forward, inclusive, originIndex)
				);
			
			for (int index = originIndex + 1; index < order.Columns.Count; index++)
				if (order.Columns[index].Column.IsNilable && !order.Columns[index].IncludeNils)
					expression = 
						AppendExpression
						(
							expression, 
							Instructions.And, 
							EmitBrowseNilExpression(order.Columns[index].Column, false)
						);

			return expression;
		}
        
		protected PlanNode EmitBrowseOriginNode
		(
			Plan plan,
			Schema.Order order, 
			bool forward, 
			bool inclusive, 
			int originIndex
		)
		{
			PlanNode node = null;
			for (int index = 0; index < originIndex; index++)
			{
				PlanNode equalNode = EmitBrowseColumnNode(plan, order.Columns[index], Instructions.Equal);
						
				node = Compiler.AppendNode(plan, node, Instructions.And, equalNode);
			}
			
			node = 
				Compiler.AppendNode
				(
					plan, 
					node, 
					Instructions.And, 
					EmitBrowseComparisonNode(plan, order, forward, inclusive, originIndex)
				);
			
			for (int index = originIndex + 1; index < order.Columns.Count; index++)
				if (order.Columns[index].Column.IsNilable && !order.Columns[index].IncludeNils)
					node = 
						Compiler.AppendNode
						(
							plan, 
							node, 
							Instructions.And, 
							EmitBrowseNilNode(plan, order.Columns[index].Column, false)
						);
			return node;
		}

		protected Expression EmitBrowseConditionExpression
		(
			Schema.Order order,
			Schema.IRowType origin,
			bool forward,
			bool inclusive
		)
		{
			Expression expression = null;
			for (int orderIndex = order.Columns.Count - 1; orderIndex >= 0; orderIndex--)
			{
				if ((origin != null) && (orderIndex < origin.Columns.Count))
					expression = 
						AppendExpression
						(
							expression, 
							Instructions.Or, 
							EmitBrowseOriginExpression(order, forward, inclusive, orderIndex)
						);
				else
					#if USEINCLUDENILSWITHBROWSE
					if (order.Columns[orderIndex].Column.IsNilable && !order.Columns[orderIndex].IncludeNils)
					#endif
					{
						expression = 
							AppendExpression
							(
								expression, 
								Instructions.And, 
								EmitBrowseNilExpression(order.Columns[orderIndex].Column, false)
							);
					}
			}

			if (expression == null)
				expression = new ValueExpression(inclusive, TokenType.Boolean);
			
			return expression;
		}

		protected Expression AppendExpression(Expression leftExpression, string instruction, Expression rightExpression)
		{
			if (leftExpression != null)
			{
				return new BinaryExpression(leftExpression, instruction, rightExpression);
			}
			else
			{
				return rightExpression;
			}
		}
        
		protected PlanNode EmitBrowseConditionNode
		(
			Plan plan,
			Schema.Order order, 
			Schema.IRowType origin, 
			bool forward, 
			bool inclusive
		)
		{
			PlanNode node = null;
			for (int orderIndex = order.Columns.Count - 1; orderIndex >= 0; orderIndex--)
			{
				if ((origin != null) && (orderIndex < origin.Columns.Count))
					node = 
						Compiler.AppendNode
						(
							plan, 
							node, 
							Instructions.Or, 
							EmitBrowseOriginNode(plan, order, forward, inclusive, orderIndex)
						);
				else
					#if USEINCLUDENILSWITHBROWSE
					if (order.Columns[orderIndex].Column.IsNilable && !order.Columns[orderIndex].IncludeNils)
					#endif
					{
						node = 
							Compiler.AppendNode
							(
								plan, 
								node, 
								Instructions.And, 
								EmitBrowseNilNode(plan, order.Columns[orderIndex].Column, false)
							);
					}
			}

			if (node == null)
				node = new ValueNode(plan.DataTypes.SystemBoolean, inclusive);

			return node;
		}

		protected Expression EmitBrowseVariantExpression(Schema.Order originOrder, Schema.IRowType origin, int originIndex, bool forward, bool inclusive)
		{
			return 
				new OrderExpression
				(
					new RestrictExpression
					(
						_sourceExpression, 
						EmitBrowseConditionExpression(originIndex < 0 ? Order : originOrder, originIndex < 0 ? null : origin, forward, inclusive)
					), 
					(from c in (new Schema.Order(Order, !forward)).Columns select (OrderColumnDefinition)c.EmitStatement(EmitMode.ForCopy)).ToArray()
				);
		}
		
		protected BrowseVariants _browseVariants = new BrowseVariants();
		public BrowseVariants BrowseVariants { get { return _browseVariants; } }

		protected PlanNode EmitBrowseVariantNode(Plan plan, int originIndex, bool forward, bool inclusive)
		{
			Schema.Order originOrder = new Schema.Order();
			for (int index = 0; index <= originIndex; index++)
				originOrder.Columns.Add(new Schema.OrderColumn(Order.Columns[index].Column, Order.Columns[index].Ascending, Order.Columns[index].IncludeNils));
			Schema.IRowType origin = new Schema.RowType(originOrder.Columns, Keywords.Origin);
			//plan.PushCursorContext(new CursorContext(CursorType, CursorCapabilities & ~(CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Countable), CursorIsolation));
			//try
			//{
				plan.EnterRowContext();
				try
				{
					plan.Symbols.Push(new Symbol(String.Empty, origin));
					try
					{
						var cursorExpression = EmitBrowseVariantExpression(originOrder, origin, originIndex, forward, inclusive);
						var cursorDefinition = 
							new CursorDefinition
							(
								cursorExpression, 
								CursorCapabilities & ~(CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Countable), 
								CursorIsolation, 
								CursorType
							);
						var resultNode = Compiler.Compile(plan, cursorDefinition, true);

						return resultNode.ExtractNode<TableNode>();
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.ExitRowContext();
				}
			//}
			//finally
			//{
			//	plan.PopCursorContext();
			//}
		}
		
		private Expression _sourceExpression;
		private void EnsureSourceExpression()
		{
			if (_sourceExpression == null)
				_sourceExpression = (Expression)SourceNode.EmitStatement(EmitMode.ForCopy);
		}
		
		private PlanNode _sourceNode;		
		private PlanNode GetSourceNode(Plan plan)
		{
			if (_sourceNode == null)
				_sourceNode = Compiler.CompileExpression(plan, _sourceExpression);
			return _sourceNode;
		}
		
		public bool HasBrowseVariant(int originIndex, bool forward, bool inclusive)
		{
			return _browseVariants.IndexOf(originIndex, forward, inclusive) >= 0;
		}
		
		public void CompileBrowseVariant(Program program, int originIndex, bool forward, bool inclusive)
		{
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				plan.PushATCreationContext();
				try
				{
					PushSymbols(plan, _symbols);
					try
					{
						EnsureSourceExpression();
						_browseVariants.Add
						(
							new BrowseVariant
							(
								EmitBrowseVariantNode
								(
									plan, 
									originIndex, 
									forward, 
									inclusive
								), 
								originIndex, 
								forward, 
								inclusive
							)
						);
					}
					finally
					{
						PopSymbols(plan, _symbols);
					}
				}
				finally
				{
					plan.PopATCreationContext();
				}
			}
			finally
			{
				plan.Dispose();
			}
		}
		
		public PlanNode GetBrowseVariantNode(Plan plan, int originIndex, bool forward, bool inclusive)
		{
			return _browseVariants[originIndex, forward, inclusive].Node;
		}
		
		public override object InternalExecute(Program program)
		{
			BrowseTable table = new BrowseTable(this, program);
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (IsAccelerator)
				return Nodes[0].EmitStatement(mode);
			else
			{
				BrowseExpression browseExpression = new BrowseExpression();
				browseExpression.Expression = (Expression)Nodes[0].EmitStatement(mode);
				for (int index = 0; index < RequestedOrder.Columns.Count; index++)
					browseExpression.Columns.Add(RequestedOrder.Columns[index].EmitStatement(mode));
				return browseExpression;
			}
		}
	}
}