/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
	
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
		private bool FEnforcePredicate = true;
		public bool EnforcePredicate
		{
			get { return FEnforcePredicate; }
			set { FEnforcePredicate = value; }
		}
		
		protected override void DetermineModifiers(Plan APlan)
		{
			base.DetermineModifiers(APlan);
			
			EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
		}

		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			if (Nodes[0].DataType.Is(Nodes[1].DataType))
				Nodes[0] = Compiler.Upcast(APlan, Nodes[0], Nodes[1].DataType);
			else if (Nodes[1].DataType.Is(Nodes[0].DataType))
				Nodes[1] = Compiler.Upcast(APlan, Nodes[1], Nodes[0].DataType);
			else
			{
				ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[0].DataType, Nodes[1].DataType);
				if (LContext.CanConvert)
					Nodes[0] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[0], LContext), Nodes[1].DataType);
				else
				{
					LContext = Compiler.FindConversionPath(APlan, Nodes[1].DataType, Nodes[0].DataType);
					Compiler.CheckConversionContext(APlan, LContext);
					Nodes[1] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[1], LContext), Nodes[0].DataType);
				}
			}

			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(LeftTableVar.MetaData);
			FTableVar.JoinInheritMetaData(RightTableVar.MetaData);

			// Determine columns
			CopyTableVarColumns(LeftTableVar.Columns);
			
			Schema.TableVarColumn LLeftColumn;
			foreach (Schema.TableVarColumn LRightColumn in RightTableVar.Columns)
			{
				LLeftColumn = TableVar.Columns[TableVar.Columns.IndexOfName(LRightColumn.Name)];
				LLeftColumn.IsDefaultRemotable = LLeftColumn.IsDefaultRemotable && LRightColumn.IsDefaultRemotable;
				LLeftColumn.IsChangeRemotable = LLeftColumn.IsChangeRemotable && LRightColumn.IsChangeRemotable;
				LLeftColumn.IsValidateRemotable = LLeftColumn.IsValidateRemotable && LRightColumn.IsValidateRemotable;
				LLeftColumn.JoinInheritMetaData(LRightColumn.MetaData);
			}
			
			DetermineRemotable(APlan);
			
			// Determine key
			Schema.Key LKey = new Schema.Key();
			LKey.IsInherited = true;
			foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
				LKey.Columns.Add(LColumn);
			TableVar.Keys.Add(LKey);
			
			DetermineOrder(APlan);

			// Determine orders
			CopyOrders(LeftTableVar.Orders);
			foreach (Schema.Order LOrder in RightTableVar.Orders)
				if (!TableVar.Orders.Contains(LOrder))
					TableVar.Orders.Add(CopyOrder(LOrder));
					
			#if UseReferenceDerivation
			CopySourceReferences(APlan, LeftTableVar.SourceReferences);
			CopySourceReferences(APlan, RightTableVar.SourceReferences);
			CopyTargetReferences(APlan, LeftTableVar.TargetReferences);
			CopyTargetReferences(APlan, RightTableVar.TargetReferences);
			#endif
		}
		
		private void DetermineOrder(Plan APlan)
		{
			Order = Compiler.OrderFromKey(APlan, FTableVar.Keys.MinimumKey(true));
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);

			// Set IsNilable for each column in the left and right tables based on PropagateXXXLeft and Right
			bool LIsNilable = 
				(PropagateInsertLeft == PropagateAction.False) || !PropagateUpdateLeft ||
				(PropagateInsertRight == PropagateAction.False) || !PropagateUpdateRight;
				
			if (LIsNilable)
				foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
					LColumn.IsNilable = LIsNilable;
		}
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			if ((LeftNode.CursorType == CursorType.Dynamic) || (RightNode.CursorType == CursorType.Dynamic))
				FCursorType = CursorType.Dynamic;
			else
				FCursorType = CursorType.Static;
			FRequestedCursorType = APlan.CursorContext.CursorType;

			FCursorCapabilities = 
				CursorCapability.Navigable |
				CursorCapability.BackwardsNavigable |
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(
						(LeftNode.CursorCapabilities & CursorCapability.Updateable) |
						(RightNode.CursorCapabilities & CursorCapability.Updateable)
					)
				);

			FCursorIsolation = APlan.CursorContext.CursorIsolation;
			
			DetermineOrder(APlan);
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			UnionExpression LExpression = new UnionExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		public override object InternalExecute(Program AProgram)
		{
			UnionTable LTable = new UnionTable(this, AProgram);
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
		
		public override void DetermineRemotable(Plan APlan)
		{
			base.DetermineRemotable(APlan);
			
			FTableVar.ShouldValidate = FTableVar.ShouldValidate || LeftTableVar.ShouldValidate || RightTableVar.ShouldValidate;
			FTableVar.ShouldDefault = FTableVar.ShouldDefault || LeftTableVar.ShouldDefault || RightTableVar.ShouldDefault;
			FTableVar.ShouldChange = FTableVar.ShouldChange || LeftTableVar.ShouldChange || RightTableVar.ShouldChange;
			
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				int LColumnIndex;
				Schema.TableVarColumn LSourceColumn;
				
				LColumnIndex = LeftTableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
				{
					LSourceColumn = LeftTableVar.Columns[LColumnIndex];
					LColumn.ShouldDefault = LColumn.ShouldDefault || LSourceColumn.ShouldDefault;
					FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;
					
					LColumn.ShouldValidate = LColumn.ShouldValidate || LSourceColumn.ShouldValidate;
					FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;

					LColumn.ShouldChange = LColumn.ShouldChange || LSourceColumn.ShouldChange;
					FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;
				}

				LColumnIndex = RightTableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
				{
					LSourceColumn = RightTableVar.Columns[LColumnIndex];
					LColumn.ShouldDefault = LColumn.ShouldDefault || LSourceColumn.ShouldDefault;
					FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;
					
					LColumn.ShouldValidate = LColumn.ShouldValidate || LSourceColumn.ShouldValidate;
					FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;

					LColumn.ShouldChange = LColumn.ShouldChange || LSourceColumn.ShouldChange;
					FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;
				}
			}
		}
		
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if (AIsDescending)
			{
				BitArray LValueFlags = ANewRow.GetValueFlags();
				bool LChanged = false;
				if (PropagateDefaultLeft)
					LChanged = LeftNode.Default(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
				if (PropagateDefaultRight)
					LChanged = RightNode.Default(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
				if (LChanged)
					for (int LIndex = 0; LIndex < ANewRow.DataType.Columns.Count; LIndex++)
						if (!LValueFlags[LIndex] && ANewRow.HasValue(LIndex))
							Change(AProgram, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LIndex].Name);
				return LChanged;
			}
			return false;
		}
		
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			bool LChanged = false;
			if (PropagateChangeLeft)
				LChanged = LeftNode.Change(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			if (PropagateChangeRight)
				LChanged = RightNode.Change(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
			return LChanged;
		}
		
		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (AIsDescending)
			{
				bool LChanged = false;
				if (PropagateValidateLeft)
					LChanged = LeftNode.Validate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
				if (PropagateValidateRight)
					LChanged = RightNode.Validate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
				return LChanged;
			}
			return false;
		}
		
		protected void InternalInsertLeft(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True : 
					LeftNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked); 
				break;

				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LLeftRow = new Row(AProgram.ValueManager, LeftNode.DataType.RowType))
					{
						ANewRow.CopyTo(LLeftRow);
						using (Row LCurrentRow = LeftNode.Select(AProgram, LLeftRow))
						{
							if (LCurrentRow != null)
							{
								if (PropagateInsertLeft == PropagateAction.Ensure)
									LeftNode.Update(AProgram, LCurrentRow, ANewRow, AValueFlags, false, AUnchecked);
							}
							else
								LeftNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
						}
					}
				break;
			}
		}
		
		protected void InternalInsertRight(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertRight)
			{
				case PropagateAction.True : 
					RightNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked); 
				break;

				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LRightRow = new Row(AProgram.ValueManager, RightNode.DataType.RowType))
					{
						ANewRow.CopyTo(LRightRow);
						using (Row LCurrentRow = RightNode.Select(AProgram, LRightRow))
						{
							if (LCurrentRow != null)
							{
								if (PropagateInsertRight == PropagateAction.Ensure)
									RightNode.Update(AProgram, LCurrentRow, ANewRow, AValueFlags, false, AUnchecked);
							}
							else
								RightNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
						}
					}
				break;
			}
		}
		
		protected override void InternalExecuteInsert(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			// Attempt to insert the row in the left node.
			if ((PropagateInsertLeft != PropagateAction.False) && (PropagateInsertRight != PropagateAction.False) && EnforcePredicate)
			{
				try
				{
					InternalInsertLeft(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
				}
				catch (DataphorException LException)
				{
					if ((LException.Severity == ErrorSeverity.User) || (LException.Severity == ErrorSeverity.Application))
					{
						InternalInsertRight(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
						return;
					}
					throw;
				}

				// Attempt to insert the row in the right node.
				try
				{
					InternalInsertRight(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
				}
				catch (DataphorException LException)
				{									
					if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
						throw;
				}
			}
			else
			{
				InternalInsertLeft(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalInsertRight(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
		}

		protected override void InternalExecuteUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateUpdateLeft && PropagateUpdateRight && EnforcePredicate)
			{
				// Attempt to update the row in the left node
				try
				{
					if (PropagateUpdateLeft)
					{
						using (Row LLeftRow = LeftNode.FullSelect(AProgram, AOldRow))
						{
							if (LLeftRow != null)
								LeftNode.Delete(AProgram, AOldRow, ACheckConcurrency, AUnchecked);

						}

						LeftNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
				}
				catch (DataphorException LException)
				{
					if ((LException.Severity == ErrorSeverity.User) || (LException.Severity == ErrorSeverity.Application))
					{
						if (PropagateUpdateRight)
						{
							using (Row LRightRow = RightNode.FullSelect(AProgram, AOldRow))
							{
								if (LRightRow != null)
									RightNode.Delete(AProgram, AOldRow, ACheckConcurrency, AUnchecked);
							}

							RightNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
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
						using (Row LRightRow = RightNode.FullSelect(AProgram, AOldRow))
						{
							if (LRightRow != null)
								RightNode.Delete(AProgram, AOldRow, ACheckConcurrency, AUnchecked);
						}

						RightNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
				}
				catch (DataphorException LException)
				{
					if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
						throw;
				}
			}
			else
			{
				if (PropagateUpdateLeft)
					LeftNode.Update(AProgram, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
				
				if (PropagateUpdateRight)
					RightNode.Update(AProgram, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
		}
		
		protected override void InternalExecuteDelete(Program AProgram, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateDeleteLeft)
				using (Row LLeftRow = LeftNode.FullSelect(AProgram, ARow))
				{
					if (LLeftRow != null)
						LeftNode.Delete(AProgram, ARow, ACheckConcurrency, AUnchecked);
				}

			if (PropagateDeleteRight)
				using (Row LRightRow = RightNode.FullSelect(AProgram, ARow))
				{
					if (LRightRow != null)
						RightNode.Delete(AProgram, ARow, ACheckConcurrency, AUnchecked);
				}
		}
		
		public override void JoinApplicationTransaction(Program AProgram, Row ARow)
		{
			LeftNode.JoinApplicationTransaction(AProgram, ARow);
			RightNode.JoinApplicationTransaction(AProgram, ARow);
		}
	}
}