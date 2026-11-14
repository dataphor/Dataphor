/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define UseElaborable
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
	using Alphora.Dataphor.DAE.Compiling.Visitors;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// operator iQuota(table{}, int) : table{}    
    public class QuotaNode : UnaryTableNode
    {
		private Schema.Order _quotaOrder;
		public Schema.Order QuotaOrder
		{
			get { return _quotaOrder; }
			set { _quotaOrder = value; }
		}
		
		#if USENAMEDROWVARIABLES
		private Schema.IRowType _quotaRowType;
		#endif
		
		// EqualNode
		protected PlanNode _equalNode;
		public PlanNode EqualNode
		{
			get { return _equalNode; }
			set { _equalNode = value; }
		}
		
		protected bool _enforcePredicate = false;
		public bool EnforcePredicate
		{
			get { return _enforcePredicate; }
			set { _enforcePredicate = value; }
		}
		
		protected override void DetermineModifiers(Plan plan)
		{
			base.DetermineModifiers(plan);
			
			if (Modifiers != null)
				EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			Nodes[0] = Compiler.EmitOrderNode(plan, SourceNode, new Schema.Order(_quotaOrder), true);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			DetermineRemotable(plan);

			bool quotaOrderIncludesKey = false;			
			foreach (Schema.Key key in SourceNode.TableVar.Keys)
				if (Compiler.OrderIncludesKey(plan, _quotaOrder, key))
				{
					quotaOrderIncludesKey = true;
					break;
				}
				
			if (quotaOrderIncludesKey)
			{
				if ((Nodes[1].IsLiteral) && ((int)plan.EvaluateLiteralArgument(Nodes[1], "quota") == 1))
				{
					Schema.Key key = new Schema.Key();
					key.IsInherited = true;
					TableVar.Keys.Add(key);
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
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif

			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				_quotaRowType = new Schema.RowType(_quotaOrder.Columns);
				plan.Symbols.Push(new Symbol(Keywords.Left, _quotaRowType));
				#else
				Schema.IRowType leftRowType = new Schema.RowType(FQuotaOrder.Columns, Keywords.Left);
				APlan.Symbols.Push(new Symbol(String.Empty, leftRowType));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, _quotaRowType));
					#else
					Schema.IRowType rightRowType = new Schema.RowType(FQuotaOrder.Columns, Keywords.Right);
					APlan.Symbols.Push(new Symbol(String.Empty, rightRowType));
					#endif
					try
					{
						_equalNode = 
							#if USENAMEDROWVARIABLES
							Compiler.CompileExpression(plan, Compiler.BuildRowEqualExpression(plan, Keywords.Left, Keywords.Right, _quotaRowType.Columns, _quotaRowType.Columns));
							#else
							Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, leftRowType.Columns, rightRowType.Columns));
							#endif
					}
					finally
					{
						plan.Symbols.Pop();
					}
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
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			base.InternalBindingTraversal(plan, visitor);
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Left, _quotaRowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(FQuotaOrder.Columns, Keywords.Left)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, _quotaRowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(FQuotaOrder.Columns, Keywords.Right)));
					#endif
					try
					{
						#if USEVISIT
						_equalNode = visitor.Visit(plan, _equalNode);
						#else
						_equalNode.BindingTraversal(plan, visitor);
						#endif
					}
					finally
					{
						plan.Symbols.Pop();
					}
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
		}
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = SourceNode.CursorType;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			QuotaExpression expression = new QuotaExpression();
			expression.Expression = (Expression)Nodes[0].Nodes[0].EmitStatement(mode);
			expression.Quota = (Expression)Nodes[1].EmitStatement(mode);
			expression.HasByClause = true;
			for (int index = 0; index < QuotaOrder.Columns.Count; index++)
				expression.Columns.Add(QuotaOrder.Columns[index].EmitStatement(mode));
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		public override object InternalExecute(Program program)
		{
			QuotaTable table = new QuotaTable(this, program);
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
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalDefault(program, oldRow, newRow, valueFlags, columnName, isDescending);
			return false;
		}
		
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalChange(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		private PlanNode _validateNode;
		private void EnsureValidateNode(Plan plan)
		{
			if (_validateNode == null)
			{
				plan.Symbols.Push(new Symbol("ARow", TableVar.DataType.RowType));
				try
				{
					// ARow in (((table { ARow }) union <source expression>) <quota clause>)
					QuotaExpression quotaExpression = (QuotaExpression)EmitStatement(EmitMode.ForCopy);
					quotaExpression.Expression = new UnionExpression(new TableSelectorExpression(new Expression[]{new IdentifierExpression("ARow")}), quotaExpression.Expression);
					Expression validateExpression = new BinaryExpression(new IdentifierExpression("ARow"), Instructions.In, quotaExpression);
					_validateNode = Compiler.Compile(plan, validateExpression).ExtractNode<ExpressionStatementNode>().Nodes[0];
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
		}								   
		
		public override void DetermineRemotable(Plan plan)
		{
			base.DetermineRemotable(plan);
			
			_tableVar.ShouldValidate = _tableVar.ShouldValidate || _enforcePredicate;
		}

		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (_enforcePredicate && (columnName == String.Empty))
			{
				// ARow in (((table { ARow }) union <source expression>)) <quota clause>
				EnsureValidateNode(program.Plan);
				PushRow(program, newRow);
				try
				{
					object objectValue = _validateNode.Execute(program);
					if ((objectValue != null) && !(bool)objectValue)
						throw new RuntimeException(RuntimeException.Codes.RowViolatesQuotaPredicate, ErrorSeverity.User);
				}
				finally
				{
					PopRow(program);
				}
			}

			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
			return false;
		}
    }
}