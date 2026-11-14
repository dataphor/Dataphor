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
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
    // operator iUnion(table{}, table{}) : table{}
	public class UnionNode : BinaryTableNode
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
			_tableVar.JoinInheritMetaData(RightTableVar.MetaData);

			// Determine columns
			CopyTableVarColumns(LeftTableVar.Columns);
			
			Schema.TableVarColumn leftColumn;
			foreach (Schema.TableVarColumn rightColumn in RightTableVar.Columns)
			{
				leftColumn = TableVar.Columns[TableVar.Columns.IndexOfName(rightColumn.Name)];
				leftColumn.IsDefaultRemotable = leftColumn.IsDefaultRemotable && rightColumn.IsDefaultRemotable;
				leftColumn.IsChangeRemotable = leftColumn.IsChangeRemotable && rightColumn.IsChangeRemotable;
				leftColumn.IsValidateRemotable = leftColumn.IsValidateRemotable && rightColumn.IsValidateRemotable;
				leftColumn.JoinInheritMetaData(rightColumn.MetaData);
			}
			
			DetermineRemotable(plan);
			
			// Determine key
			Schema.Key key = new Schema.Key();
			key.IsInherited = true;
			foreach (Schema.TableVarColumn column in TableVar.Columns)
				key.Columns.Add(column);
			TableVar.Keys.Add(key);
			
			DetermineOrder(plan);

			// Determine orders
			CopyOrders(LeftTableVar.Orders);
			foreach (Schema.Order order in RightTableVar.Orders)
				if (!TableVar.Orders.Contains(order))
					TableVar.Orders.Add(CopyOrder(order));
			
			#if UseReferenceDerivation		
			// NOTE: This isn't exactly the same, as the previous logic would copy source references from both tables, then target references from both tables. Shouldn't be an issue but....
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
			{
				CopyReferences(plan, LeftTableVar);
				CopyReferences(plan, RightTableVar);
			}
			#endif
		}
		
		private void DetermineOrder(Plan plan)
		{
			Order = Compiler.OrderFromKey(plan, _tableVar.Keys.MinimumKey(true));
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);

			// Set IsNilable for each column in the left and right tables based on PropagateXXXLeft and Right
			bool isNilable = 
				(PropagateInsertLeft == PropagateAction.False) || !PropagateUpdateLeft ||
				(PropagateInsertRight == PropagateAction.False) || !PropagateUpdateRight;
				
			if (isNilable)
				foreach (Schema.TableVarColumn column in TableVar.Columns)
					column.IsNilable = isNilable;
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
				CursorCapability.BackwardsNavigable |
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(
						(LeftNode.CursorCapabilities & CursorCapability.Updateable) |
						(RightNode.CursorCapabilities & CursorCapability.Updateable)
					)
				) |
				(
					plan.CursorContext.CursorCapabilities & (LeftNode.CursorCapabilities | RightNode.CursorCapabilities) & CursorCapability.Elaborable
				);

			_cursorIsolation = plan.CursorContext.CursorIsolation;
			
			DetermineOrder(plan);
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			UnionExpression expression = new UnionExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		public override object InternalExecute(Program program)
		{
			UnionTable table = new UnionTable(this, program);
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
			
			_tableVar.ShouldValidate = _tableVar.ShouldValidate || LeftTableVar.ShouldValidate || RightTableVar.ShouldValidate;
			_tableVar.ShouldDefault = _tableVar.ShouldDefault || LeftTableVar.ShouldDefault || RightTableVar.ShouldDefault;
			_tableVar.ShouldChange = _tableVar.ShouldChange || LeftTableVar.ShouldChange || RightTableVar.ShouldChange;
			
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				int columnIndex;
				Schema.TableVarColumn sourceColumn;
				
				columnIndex = LeftTableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
				{
					sourceColumn = LeftTableVar.Columns[columnIndex];
					column.ShouldDefault = column.ShouldDefault || sourceColumn.ShouldDefault;
					_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;
					
					column.ShouldValidate = column.ShouldValidate || sourceColumn.ShouldValidate;
					_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;

					column.ShouldChange = column.ShouldChange || sourceColumn.ShouldChange;
					_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;
				}

				columnIndex = RightTableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
				{
					sourceColumn = RightTableVar.Columns[columnIndex];
					column.ShouldDefault = column.ShouldDefault || sourceColumn.ShouldDefault;
					_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;
					
					column.ShouldValidate = column.ShouldValidate || sourceColumn.ShouldValidate;
					_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;

					column.ShouldChange = column.ShouldChange || sourceColumn.ShouldChange;
					_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;
				}
			}
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if (isDescending)
			{
				BitArray localValueFlags = newRow.GetValueFlags();
				bool changed = false;
				if (PropagateDefaultLeft)
					changed = LeftNode.Default(program, oldRow, newRow, valueFlags, columnName);
				if (PropagateDefaultRight)
					changed = RightNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
				if (changed)
					for (int index = 0; index < newRow.DataType.Columns.Count; index++)
						if (!localValueFlags[index] && newRow.HasValue(index))
							Change(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[index].Name);
				return changed;
			}
			return false;
		}
		
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			bool changed = false;
			if (PropagateChangeLeft)
				changed = LeftNode.Change(program, oldRow, newRow, valueFlags, columnName);
			if (PropagateChangeRight)
				changed = RightNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;
			return changed;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (isDescending)
			{
				bool changed = false;
				if (PropagateValidateLeft)
					changed = LeftNode.Validate(program, oldRow, newRow, valueFlags, columnName);
				if (PropagateValidateRight)
					changed = RightNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;
				return changed;
			}
			return false;
		}
		
		protected void InternalInsertLeft(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True : 
					LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
				break;

				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
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
		
		protected void InternalInsertRight(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertRight)
			{
				case PropagateAction.True : 
					RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
				break;

				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row rightRow = new Row(program.ValueManager, RightNode.DataType.RowType))
					{
						newRow.CopyTo(rightRow);
						using (IRow currentRow = RightNode.Select(program, rightRow))
						{
							if (currentRow != null)
							{
								if (PropagateInsertRight == PropagateAction.Ensure)
									RightNode.Update(program, currentRow, newRow, valueFlags, false, uncheckedValue);
							}
							else
								RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
						}
					}
				break;
			}
		}
		
		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			// Attempt to insert the row in the left node.
			if ((PropagateInsertLeft != PropagateAction.False) && (PropagateInsertRight != PropagateAction.False) && EnforcePredicate)
			{
				try
				{
					InternalInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
				}
				catch (DataphorException exception)
				{
					if ((exception.Severity == ErrorSeverity.User) || (exception.Severity == ErrorSeverity.Application))
					{
						InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
						return;
					}
					throw;
				}

				// Attempt to insert the row in the right node.
				try
				{
					InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
				}
				catch (DataphorException exception)
				{									
					if ((exception.Severity != ErrorSeverity.User) && (exception.Severity != ErrorSeverity.Application))
						throw;
				}
			}
			else
			{
				InternalInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
		}

		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateUpdateLeft && PropagateUpdateRight && EnforcePredicate)
			{
				// Attempt to update the row in the left node
				try
				{
					if (PropagateUpdateLeft)
					{
						using (IRow leftRow = LeftNode.FullSelect(program, oldRow))
						{
							if (leftRow != null)
								LeftNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);

						}

						LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
				}
				catch (DataphorException exception)
				{
					if ((exception.Severity == ErrorSeverity.User) || (exception.Severity == ErrorSeverity.Application))
					{
						if (PropagateUpdateRight)
						{
							using (IRow rightRow = RightNode.FullSelect(program, oldRow))
							{
								if (rightRow != null)
									RightNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);
							}

							RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
						}
						return;
					}
					throw;
				}
				
				// Attempt to update the row in the right node
				try
				{
					if (PropagateUpdateRight)
					{
						using (IRow rightRow = RightNode.FullSelect(program, oldRow))
						{
							if (rightRow != null)
								RightNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);
						}

						RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
				}
				catch (DataphorException exception)
				{
					if ((exception.Severity != ErrorSeverity.User) && (exception.Severity != ErrorSeverity.Application))
						throw;
				}
			}
			else
			{
				if (PropagateUpdateLeft)
					LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
				
				if (PropagateUpdateRight)
					RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
		}
		
		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateDeleteLeft)
				using (IRow leftRow = LeftNode.FullSelect(program, row))
				{
					if (leftRow != null)
						LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
				}

			if (PropagateDeleteRight)
				using (IRow rightRow = RightNode.FullSelect(program, row))
				{
					if (rightRow != null)
						RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
				}
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			LeftNode.JoinApplicationTransaction(program, row);
			RightNode.JoinApplicationTransaction(program, row);
		}
	}
}