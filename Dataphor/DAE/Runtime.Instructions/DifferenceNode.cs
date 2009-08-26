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
    // operator iDifference(table{}, table{}): table{}
	public class DifferenceNode : BinaryTableNode
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
			CopyTableVarColumns(LeftTableVar.Columns);
			
			DetermineRemotable(APlan);

			CopyKeys(LeftTableVar.Keys);
			CopyOrders(LeftTableVar.Orders);
			if (LeftNode.Order != null)
				Order = CopyOrder(LeftNode.Order);

			#if UseReferenceDerivation
			CopySourceReferences(APlan, LeftTableVar.SourceReferences);
			CopyTargetReferences(APlan, LeftTableVar.TargetReferences);
			#endif
			
			#if UseScannedDifference
			// Scanned Difference Algorithm - Brute Force, for every row in the left, scan the right for a matching row
			FDifferenceAlgorithm = typeof(ScannedDifferenceTable);
			Schema.IRowType LLeftRowType = DataType.CreateRowType(Keywords.Left);
			APlan.Symbols.Push(new Symbol(LLeftRowType));
			try
			{
				// Compile a row equality node
				Schema.IRowType LRightRowType = DataType.CreateRowType(Keywords.Right);
				APlan.Symbols.Push(new Symbol(LRightRowType));
				try
				{
					FEqualNode = Compiler.CompileExpression(APlan, Compiler.BuildKeyEqualExpression(APlan, LLeftRowType.Columns, LRightRowType.Columns));
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
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			bool LIsNilable = (PropagateInsertLeft == PropagateAction.False || !PropagateUpdateLeft);
			
			if (LIsNilable)
				foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
					LColumn.IsNilable = LIsNilable;
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			#if UseScannedDifference
			Schema.IRowType LLeftRowType = DataType.CreateRowType(Keywords.Left);
			APlan.Symbols.Push(new Symbol(LLeftRowType));
			try
			{
				Schema.IRowType LRightRowType = DataType.CreateRowType(Keywords.Right);
				APlan.Symbols.Push(new Symbol(LRightRowType));
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
			#endif
		}
		
		public override void DetermineDevice(Plan APlan)
		{
			base.DetermineDevice(APlan);
			if (!FDeviceSupported)
			{
				FDifferenceAlgorithm = typeof(SearchedDifferenceTable);
				// ensure that the right side is a searchable node
				Schema.Key LSearchKey = new Schema.Key();
				foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
					LSearchKey.Columns.Add(LColumn);

				Nodes[1] = Compiler.EnsureSearchableNode(APlan, RightNode, LSearchKey);
			}
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
				(LeftNode.CursorCapabilities & CursorCapability.BackwardsNavigable) |
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(LeftNode.CursorCapabilities & CursorCapability.Updateable) &
					(RightNode.CursorCapabilities & CursorCapability.Updateable)
				);

			FCursorIsolation = APlan.CursorContext.CursorIsolation;
			
			if (LeftNode.Order != null)
				Order = CopyOrder(LeftNode.Order);
			else
				Order = null;
		}
		
		// EqualNode
		protected PlanNode FEqualNode;
		public PlanNode EqualNode
		{
			get { return FEqualNode; }
			set { FEqualNode = value; }
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			DifferenceExpression LExpression = new DifferenceExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		protected Type FDifferenceAlgorithm;
		
		public override object InternalExecute(Program AProgram)
		{
			DifferenceTable LTable = (DifferenceTable)Activator.CreateInstance(FDifferenceAlgorithm, new object[]{this, AProgram});
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
			
			FTableVar.ShouldChange = PropagateChangeLeft && (FTableVar.ShouldChange || LeftTableVar.ShouldChange);
			FTableVar.ShouldDefault = PropagateDefaultLeft && (FTableVar.ShouldDefault || LeftTableVar.ShouldDefault);
			FTableVar.ShouldValidate = PropagateValidateLeft && (FTableVar.ShouldValidate || LeftTableVar.ShouldValidate);
			
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				Schema.TableVarColumn LSourceColumn = LeftTableVar.Columns[LeftTableVar.Columns.IndexOfName(LColumn.Name)];

				LColumn.ShouldChange = PropagateChangeLeft && (LColumn.ShouldChange || LSourceColumn.ShouldChange);
				FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;

				LColumn.ShouldDefault = PropagateDefaultLeft && (LColumn.ShouldDefault || LSourceColumn.ShouldDefault);
				FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;

				LColumn.ShouldValidate = PropagateValidateLeft && (LColumn.ShouldValidate || LSourceColumn.ShouldValidate);
				FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;
			}
		}
		
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if (AIsDescending && PropagateDefaultLeft)
				return LeftNode.Default(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (PropagateChangeLeft)
				return LeftNode.Change(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (AIsDescending && PropagateValidateLeft)
				return LeftNode.Validate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected void ValidatePredicate(Program AProgram, Row ARow)
		{
			if (EnforcePredicate)
			{
				bool LRightNodeValid = false;
				try
				{
					RightNode.Insert(AProgram, null, ARow, null, false);
					LRightNodeValid = true;
					RightNode.Delete(AProgram, ARow, false, false);
				}
				catch (DataphorException LException)
				{
					if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
						throw;
				}
				
				if (LRightNodeValid)
					throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
			}
		}
		
		protected override void InternalExecuteInsert(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True :
					if (!AUnchecked)
						ValidatePredicate(AProgram, ANewRow);
					LeftNode.Insert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					if (!AUnchecked)
						ValidatePredicate(AProgram, ANewRow);
					
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
		
		protected override void InternalExecuteUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateUpdateLeft)
			{
				if (!AUnchecked)
					ValidatePredicate(AProgram, ANewRow);
				LeftNode.Update(AProgram, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
		}
		
		protected override void InternalExecuteDelete(Program AProgram, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateDeleteLeft)
				LeftNode.Delete(AProgram, ARow, ACheckConcurrency, AUnchecked);
		}
		
		public override void JoinApplicationTransaction(Program AProgram, Row ARow)
		{
			LeftNode.JoinApplicationTransaction(AProgram, ARow);
		}
	}
}