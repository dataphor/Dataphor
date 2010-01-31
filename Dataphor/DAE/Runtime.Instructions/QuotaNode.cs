/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define USENAMEDROWVARIABLES
	
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

	// operator iQuota(table{}, int) : table{}    
    public class QuotaNode : UnaryTableNode
    {
		private Schema.Order FQuotaOrder;
		public Schema.Order QuotaOrder
		{
			get { return FQuotaOrder; }
			set { FQuotaOrder = value; }
		}
		
		#if USENAMEDROWVARIABLES
		private Schema.IRowType FQuotaRowType;
		#endif
		
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
				if (Compiler.OrderIncludesKey(APlan, FQuotaOrder, LKey))
				{
					LQuotaOrderIncludesKey = true;
					break;
				}
				
			if (LQuotaOrderIncludesKey)
			{
				if ((Nodes[1].IsLiteral) && ((int)APlan.EvaluateLiteralArgument(Compiler.BindNode(APlan, Nodes[1]), "quota") == 1))
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
				#if USENAMEDROWVARIABLES
				FQuotaRowType = new Schema.RowType(FQuotaOrder.Columns);
				APlan.Symbols.Push(new Symbol(Keywords.Left, FQuotaRowType));
				#else
				Schema.IRowType LLeftRowType = new Schema.RowType(FQuotaOrder.Columns, Keywords.Left);
				APlan.Symbols.Push(new Symbol(String.Empty, LLeftRowType));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Right, FQuotaRowType));
					#else
					Schema.IRowType LRightRowType = new Schema.RowType(FQuotaOrder.Columns, Keywords.Right);
					APlan.Symbols.Push(new Symbol(String.Empty, LRightRowType));
					#endif
					try
					{
						FEqualNode = 
							#if USENAMEDROWVARIABLES
							Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, Keywords.Left, Keywords.Right, FQuotaRowType.Columns, FQuotaRowType.Columns));
							#else
							Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, LLeftRowType.Columns, LRightRowType.Columns));
							#endif
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
				#if USENAMEDROWVARIABLES
				APlan.Symbols.Push(new Symbol(Keywords.Left, FQuotaRowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(FQuotaOrder.Columns, Keywords.Left)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Right, FQuotaRowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(FQuotaOrder.Columns, Keywords.Right)));
					#endif
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
		
		public override object InternalExecute(Program AProgram)
		{
			QuotaTable LTable = new QuotaTable(this, AProgram);
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
		
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalDefault(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending);
			return false;
		}
		
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalChange(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		private PlanNode FValidateNode;
		private void EnsureValidateNode(Plan APlan)
		{
			if (FValidateNode == null)
			{
				APlan.Symbols.Push(new Symbol("ARow", TableVar.DataType.RowType));
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

		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (FEnforcePredicate && (AColumnName == String.Empty))
			{
				// ARow in (((table { ARow }) union <source expression>)) <quota clause>
				EnsureValidateNode(AProgram.Plan);
				PushRow(AProgram, ANewRow);
				try
				{
					object LObject = FValidateNode.Execute(AProgram);
					if ((LObject != null) && !(bool)LObject)
						throw new RuntimeException(RuntimeException.Codes.RowViolatesQuotaPredicate, ErrorSeverity.User);
				}
				finally
				{
					PopRow(AProgram);
				}
			}

			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalValidate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			return false;
		}
    }
}