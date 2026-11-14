/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define UseElaborable
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

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

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
    // operator iDifference(table{}, table{}): table{}
	public class DifferenceNode : BinaryTableNode
	{		
		// EnforcePredicate
		private bool _enforcePredicate = true;
		public bool EnforcePredicate
		{
			get { return _enforcePredicate; }
			set { _enforcePredicate = value; }
		}
		
		protected override void DetermineModifiers(Plan plan)
		{
			base.DetermineModifiers(plan);
			
			EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
		}

		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			if (Nodes[0].DataType.Is(Nodes[1].DataType))
				Nodes[0] = Compiler.Upcast(plan, Nodes[0], Nodes[1].DataType);
			else if (Nodes[1].DataType.Is(Nodes[0].DataType))
				Nodes[1] = Compiler.Upcast(plan, Nodes[1], Nodes[0].DataType);
			else
			{
				ConversionContext context = Compiler.FindConversionPath(plan, Nodes[0].DataType, Nodes[1].DataType);
				if (context.CanConvert)
					Nodes[0] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[0], context), Nodes[1].DataType);
				else
				{
					context = Compiler.FindConversionPath(plan, Nodes[1].DataType, Nodes[0].DataType);
					Compiler.CheckConversionContext(plan, context);
					Nodes[1] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[1], context), Nodes[0].DataType);
				}
			}

			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(LeftTableVar.MetaData);
			CopyTableVarColumns(LeftTableVar.Columns);
			
			DetermineRemotable(plan);

			CopyKeys(LeftTableVar.Keys);
			CopyOrders(LeftTableVar.Orders);
			if (LeftNode.Order != null)
				Order = CopyOrder(LeftNode.Order);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, LeftTableVar);
			#endif
			
			#if UseScannedDifference
			// Scanned Difference Algorithm - Brute Force, for every row in the left, scan the right for a matching row
			FDifferenceAlgorithm = typeof(ScannedDifferenceTable);
			Schema.IRowType leftRowType = DataType.CreateRowType(Keywords.Left);
			APlan.Symbols.Push(new Symbol(leftRowType));
			try
			{
				// Compile a row equality node
				Schema.IRowType rightRowType = DataType.CreateRowType(Keywords.Right);
				APlan.Symbols.Push(new Symbol(rightRowType));
				try
				{
					FEqualNode = Compiler.CompileExpression(APlan, Compiler.BuildKeyEqualExpression(APlan, leftRowType.Columns, rightRowType.Columns));
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
			#endif
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			bool isNilable = (PropagateInsertLeft == PropagateAction.False || !PropagateUpdateLeft);
			
			if (isNilable)
				foreach (Schema.TableVarColumn column in TableVar.Columns)
					column.IsNilable = isNilable;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			base.InternalBindingTraversal(plan, visitor);
			#if UseScannedDifference
			Schema.IRowType leftRowType = DataType.CreateRowType(Keywords.Left);
			APlan.Symbols.Push(new Symbol(leftRowType));
			try
			{
				Schema.IRowType rightRowType = DataType.CreateRowType(Keywords.Right);
				APlan.Symbols.Push(new Symbol(rightRowType));
				try
				{
					FEqualNode.BindingTraversal(APlan, visitor);
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
			#endif
		}

		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			if (!DeviceSupported)
			{
				_differenceAlgorithm = typeof(SearchedDifferenceTable);
				// ensure that the right side is a searchable node
				Schema.Key searchKey = new Schema.Key();
				foreach (Schema.TableVarColumn column in RightTableVar.Columns)
					searchKey.Columns.Add(column);

				Nodes[1] = Compiler.EnsureSearchableNode(plan, RightNode, searchKey);
			}
		}
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			if ((LeftNode.CursorType == CursorType.Dynamic) || (RightNode.CursorType == CursorType.Dynamic))
				_cursorType = CursorType.Dynamic;
			else
				_cursorType = CursorType.Static;
			_requestedCursorType = plan.CursorContext.CursorType;

			_cursorCapabilities = 
				CursorCapability.Navigable |
				(LeftNode.CursorCapabilities & CursorCapability.BackwardsNavigable) |
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(LeftNode.CursorCapabilities & CursorCapability.Updateable) &
					(RightNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & (LeftNode.CursorCapabilities | RightNode.CursorCapabilities) & CursorCapability.Elaborable
				);

			_cursorIsolation = plan.CursorContext.CursorIsolation;
			
			if (LeftNode.Order != null)
				Order = CopyOrder(LeftNode.Order);
			else
				Order = null;
		}
		
		// EqualNode
		protected PlanNode _equalNode;
		public PlanNode EqualNode
		{
			get { return _equalNode; }
			set { _equalNode = value; }
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			DifferenceExpression expression = new DifferenceExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		protected Type _differenceAlgorithm;
		
		public override object InternalExecute(Program program)
		{
			DifferenceTable table = (DifferenceTable)Activator.CreateInstance(_differenceAlgorithm, new object[]{this, program});
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
		
		public override void DetermineRemotable(Plan plan)
		{
			base.DetermineRemotable(plan);
			
			_tableVar.ShouldChange = PropagateChangeLeft && (_tableVar.ShouldChange || LeftTableVar.ShouldChange);
			_tableVar.ShouldDefault = PropagateDefaultLeft && (_tableVar.ShouldDefault || LeftTableVar.ShouldDefault);
			_tableVar.ShouldValidate = PropagateValidateLeft && (_tableVar.ShouldValidate || LeftTableVar.ShouldValidate);
			
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				Schema.TableVarColumn sourceColumn = LeftTableVar.Columns[LeftTableVar.Columns.IndexOfName(column.Name)];

				column.ShouldChange = PropagateChangeLeft && (column.ShouldChange || sourceColumn.ShouldChange);
				_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;

				column.ShouldDefault = PropagateDefaultLeft && (column.ShouldDefault || sourceColumn.ShouldDefault);
				_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;

				column.ShouldValidate = PropagateValidateLeft && (column.ShouldValidate || sourceColumn.ShouldValidate);
				_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;
			}
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if (isDescending && PropagateDefaultLeft)
				return LeftNode.Default(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (PropagateChangeLeft)
				return LeftNode.Change(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (isDescending && PropagateValidateLeft)
				return LeftNode.Validate(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected void ValidatePredicate(Program program, IRow row)
		{
			if (EnforcePredicate)
			{
				bool rightNodeValid = false;
				try
				{
					RightNode.Insert(program, null, row, null, false);
					rightNodeValid = true;
					RightNode.Delete(program, row, false, false);
				}
				catch (DataphorException exception)
				{
					if ((exception.Severity != ErrorSeverity.User) && (exception.Severity != ErrorSeverity.Application))
						throw;
				}
				
				if (rightNodeValid)
					throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
			}
		}
		
		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True :
					if (!uncheckedValue)
						ValidatePredicate(program, newRow);
					LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					if (!uncheckedValue)
						ValidatePredicate(program, newRow);
					
					using (Row leftRow = new Row(program.ValueManager, LeftNode.DataType.RowType))
					{
						newRow.CopyTo(leftRow);
						using (IRow currentRow = LeftNode.Select(program, leftRow))
						{
							if (currentRow != null)
							{
								if (PropagateInsertLeft == PropagateAction.Ensure)
									LeftNode.Update(program, currentRow, newRow, valueFlags, false, uncheckedValue);
							}
							else
								LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
						}
					}
				break;
			}
		}
		
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateUpdateLeft)
			{
				if (!uncheckedValue)
					ValidatePredicate(program, newRow);
				LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
		}
		
		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateDeleteLeft)
				LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			LeftNode.JoinApplicationTransaction(program, row);
		}
	}
}