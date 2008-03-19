/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
	
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using System.Text;
	using System.Threading;
	using System.Collections;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	// operator iQuota(table{}, int) : table{}    
    public class QuotaNode : UnaryTableNode
    {
		private Schema.Order FQuotaOrder;
		public Schema.Order QuotaOrder
		{
			get { return FQuotaOrder; }
			set { FQuotaOrder = value; }
		}
		
		// EqualNode
		protected PlanNode FEqualNode;
		public PlanNode EqualNode
		{
			get { return FEqualNode; }
			set { FEqualNode = value; }
		}
		
		protected bool FEnforcePredicate = false;
		public bool EnforcePredicate
		{
			get { return FEnforcePredicate; }
			set { FEnforcePredicate = value; }
		}
		
		protected override void DetermineModifiers(Plan APlan)
		{
			base.DetermineModifiers(APlan);
			
			if (Modifiers != null)
				EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			Nodes[0] = Compiler.EmitOrderNode(APlan, SourceNode, new Schema.Order(FQuotaOrder), true);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			DetermineRemotable(APlan);

			bool LQuotaOrderIncludesKey = false;			
			foreach (Schema.Key LKey in SourceNode.TableVar.Keys)
				if (FQuotaOrder.Includes(APlan, LKey))
				{
					LQuotaOrderIncludesKey = true;
					break;
				}
				
			if (LQuotaOrderIncludesKey)
			{
				if ((Nodes[1].IsLiteral) && (Compiler.BindNode(APlan, Nodes[1]).Execute(APlan.ServerProcess).Value.AsInt32 == 1))
				{
					Schema.Key LKey = new Schema.Key();
					LKey.IsInherited = true;
					TableVar.Keys.Add(LKey);
				}
				else
					CopyKeys(SourceTableVar.Keys);
			}
			else
				CopyKeys(SourceTableVar.Keys);
			
			CopyOrders(SourceTableVar.Orders);
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);

			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif

			APlan.EnterRowContext();
			try
			{
				Schema.IRowType LLeftRowType = new Schema.RowType(FQuotaOrder.Columns, Keywords.Left);
				APlan.Symbols.Push(new DataVar(LLeftRowType));
				try
				{
					Schema.IRowType LRightRowType = new Schema.RowType(FQuotaOrder.Columns, Keywords.Right);
					APlan.Symbols.Push(new DataVar(LRightRowType));
					try
					{
						FEqualNode = Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, LLeftRowType.Columns, LRightRowType.Columns));
					}
					finally
					{
						APlan.Symbols.Pop();
					}
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
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new DataVar(new Schema.RowType(FQuotaOrder.Columns, Keywords.Left)));
				try
				{
					APlan.Symbols.Push(new DataVar(new Schema.RowType(FQuotaOrder.Columns, Keywords.Right)));
					try
					{
						FEqualNode.DetermineBinding(APlan);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
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
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = SourceNode.CursorType;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			QuotaExpression LExpression = new QuotaExpression();
			LExpression.Expression = (Expression)Nodes[0].Nodes[0].EmitStatement(AMode);
			LExpression.Quota = (Expression)Nodes[1].EmitStatement(AMode);
			LExpression.HasByClause = true;
			for (int LIndex = 0; LIndex < QuotaOrder.Columns.Count; LIndex++)
				LExpression.Columns.Add(QuotaOrder.Columns[LIndex].EmitStatement(AMode));
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			QuotaTable LTable = new QuotaTable(this, AProcess);
			try
			{
				LTable.Open();
				return new DataVar(String.Empty, FDataType, LTable);
			}
			catch
			{
				LTable.Dispose();
				throw;
			}								  
		}
		
		protected override bool InternalDefault(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalDefault(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending);
			return false;
		}
		
		protected override bool InternalChange(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalChange(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		private PlanNode FValidateNode;
		private void EnsureValidateNode(Plan APlan)
		{
			if (FValidateNode == null)
			{
				APlan.Symbols.Push(new DataVar("ARow", TableVar.DataType.RowType));
				try
				{
					// ARow in (((table { ARow }) union <source expression>) <quota clause>)
					QuotaExpression LQuotaExpression = (QuotaExpression)EmitStatement(EmitMode.ForCopy);
					LQuotaExpression.Expression = new UnionExpression(new TableSelectorExpression(new Expression[]{new IdentifierExpression("ARow")}), LQuotaExpression.Expression);
					Expression LValidateExpression = new BinaryExpression(new IdentifierExpression("ARow"), Instructions.In, LQuotaExpression);
					FValidateNode = Compiler.BindNode(APlan, Compiler.OptimizeNode(APlan, Compiler.CompileExpression(APlan, LValidateExpression)));
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
		}								   
		
		public override void DetermineRemotable(Plan APlan)
		{
			base.DetermineRemotable(APlan);
			
			FTableVar.ShouldValidate = FTableVar.ShouldValidate || FEnforcePredicate;
		}

		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (FEnforcePredicate && (AColumnName == String.Empty))
			{
				// ARow in (((table { ARow }) union <source expression>)) <quota clause>
				EnsureValidateNode(AProcess.Plan);
				PushRow(AProcess, ANewRow);
				try
				{
					DataVar LObject = FValidateNode.Execute(AProcess);
					if ((LObject.Value != null) && !LObject.Value.IsNil && !LObject.Value.AsBoolean)
						throw new RuntimeException(RuntimeException.Codes.RowViolatesQuotaPredicate, ErrorSeverity.User);
				}
				finally
				{
					PopRow(AProcess);
				}
			}

			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalValidate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			return false;
		}
    }
}