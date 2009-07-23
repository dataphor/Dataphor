/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public class ListNode : PlanNode
	{
		// ListType
		public Schema.ListType ListType { get { return (Schema.ListType)FDataType; } } 
		
		// DetermineDataType not used, ListNode.ListType is set by the compiler
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			FIsLiteral = true;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsNilable = false;
			for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
			{
				FIsLiteral = FIsLiteral && Nodes[LIndex].IsLiteral;
				FIsFunctional = FIsFunctional && Nodes[LIndex].IsFunctional;
				FIsDeterministic = FIsDeterministic && Nodes[LIndex].IsDeterministic;
				FIsRepeatable = FIsRepeatable && Nodes[LIndex].IsRepeatable;
			} 
		}
		
		// Evaluate
		public override object InternalExecute(ServerProcess AProcess)
		{
			ListValue LList = new ListValue(AProcess, ListType);
			for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
				LList.Add(Nodes[LIndex].Execute(AProcess));
			return LList;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			ListSelectorExpression LExpression = new ListSelectorExpression();
			LExpression.TypeSpecifier = ListType.EmitSpecifier(AMode);
			foreach (PlanNode LNode in Nodes)
				LExpression.Expressions.Add(LNode.EmitStatement(AMode));
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
	}

	// operator iIndexer(const AList : list, const AIndex : integer) : generic
	public class IndexerNode : BinaryInstructionNode
	{
		public bool ByReference;
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			FDataType = ((Schema.ListType)Nodes[0].DataType).ElementType;
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif
			
			if (ByReference)
				return ((ListValue)AArgument1)[(int)AArgument2];

			return DataValue.CopyValue(AProcess, ((ListValue)AArgument1)[(int)AArgument2]);
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			D4IndexerExpression LExpression = new D4IndexerExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.Indexer = (Expression)Nodes[1].EmitStatement(AMode);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
	}
	
	// operator Count(const AList : list) : integer;	
	public class ListCountNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return ((ListValue)AArgument1).Count();
		}
	}
	
	// operator Clear(var AList : list);
	public class ListClearNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			((ListValue)AArgument1).Clear();
			return null;
		}
	}
	
	// operator Add(var AList : list, AValue : generic) : integer;
	public class ListAddNode : BinaryInstructionNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			if (!Nodes[1].DataType.Is(((Schema.IListType)Nodes[0].DataType).ElementType))
			{
				ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[1].DataType, ((Schema.IListType)Nodes[0].DataType).ElementType);
				Compiler.CheckConversionContext(APlan, LContext);
				Nodes[1] = Compiler.ConvertNode(APlan, Nodes[1], LContext);
			}

			Nodes[1] = Compiler.Upcast(APlan, Nodes[1], ((Schema.IListType)Nodes[0].DataType).ElementType);
			base.DetermineDataType(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			return ((ListValue)AArgument1).Add(AArgument2);
		}
	}
	
	// operator Insert(var AList : list, AValue : generic, AIndex : integer);
	public class ListInsertNode : TernaryInstructionNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			if (!Nodes[1].DataType.Is(((Schema.IListType)Nodes[0].DataType).ElementType))
			{
				ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[1].DataType, ((Schema.IListType)Nodes[0].DataType).ElementType);
				Compiler.CheckConversionContext(APlan, LContext);
				Nodes[1] = Compiler.ConvertNode(APlan, Nodes[1], LContext);
			}

			Nodes[1] = Compiler.Upcast(APlan, Nodes[1], ((Schema.IListType)Nodes[0].DataType).ElementType);
			base.DetermineDataType(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3)
		{
			((ListValue)AArgument1).Insert((int)AArgument3, AArgument2);
			return null;
		}
	}
	
	// operator RemoveAt(var AList : list, const AIndex : integer);
	public class ListRemoveAtNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			((ListValue)AArgument1).RemoveAt((int)AArgument2);
			return null;
		}
	}
	
	public abstract class BaseListIndexOfNode : InstructionNode
	{
		public PlanNode FEqualNode; // The equality node used to compare each item in the list against AValue
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			APlan.Symbols.Push(new Symbol("AValue", Nodes[1].DataType));
			try
			{
				APlan.Symbols.Push(new Symbol("ACompareValue", ((Schema.ListType)Nodes[0].DataType).ElementType));
				try
				{
					FEqualNode = Compiler.CompileExpression(APlan, new BinaryExpression(new IdentifierExpression("AValue"), Instructions.Equal, new IdentifierExpression("ACompareValue")));
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
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			APlan.Symbols.Push(new Symbol("AValue", Nodes[1].DataType));
			try
			{
				APlan.Symbols.Push(new Symbol("ACompareValue", ((Schema.ListType)Nodes[0].DataType).ElementType));
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

		protected object InternalSearch(ServerProcess AProcess, ListValue AList, object AValue, int AStartIndex, int ALength, int AIncrementor)
		{
			if (ALength < 0)
				throw new RuntimeException(RuntimeException.Codes.InvalidLength, ErrorSeverity.Application);
			if (ALength == 0)
				return -1;

			int LStartIndex = Math.Max(Math.Min(AStartIndex, AList.Count() - 1), 0);
			int LEndIndex = Math.Max(Math.Min(AStartIndex + ((ALength - 1) * AIncrementor), AList.Count() - 1), 0);
			
			AProcess.Context.Push(AValue);
			try
			{
				int LIndex = LStartIndex;
				while (((AIncrementor > 0) && (LIndex <= LEndIndex)) || ((AIncrementor < 0) && (LIndex >= LEndIndex)))
				{
					AProcess.Context.Push(AList[LIndex]);
					try
					{
						object LValue = FEqualNode.Execute(AProcess);
						if ((LValue != null) && (bool)LValue)
							return LIndex;
					}
					finally
					{
						AProcess.Context.Pop();
					}
					LIndex += AIncrementor;
				}
			}
			finally
			{
				AProcess.Context.Pop();
			}

			return -1;
		}
	}

	// operator IndexOf(const AList : list, const AValue : generic);
	public class ListIndexOfNode : BaseListIndexOfNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			
			ListValue LList = (ListValue)AArguments[0];
			return InternalSearch(AProcess, LList, AArguments[1], 0, LList.Count(), 1);
		}
	}

	// operator IndexOf(const AList : list, const AValue : generic, const AStartIndex : Integer);
	public class ListIndexOfStartNode : BaseListIndexOfNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			#endif
			
			ListValue LList = (ListValue)AArguments[0];
			return InternalSearch(AProcess, LList, AArguments[1], (int)AArguments[2], LList.Count(), 1);
		}
	}

	// operator IndexOf(const AList : list, const AValue : generic, const AStartIndex : Integer, const ALength : Integer);
	public class ListIndexOfStartLengthNode : BaseListIndexOfNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null || AArguments[3] == null)
				return null;
			#endif
			
			return InternalSearch(AProcess, (ListValue)AArguments[0], AArguments[1], (int)AArguments[2], (int)AArguments[3], 1);
		}
	}

	// operator LastIndexOf(const AList : list, const AValue : generic);
	public class ListLastIndexOfNode : BaseListIndexOfNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			
			ListValue LList = (ListValue)AArguments[0];
			return InternalSearch(AProcess, LList, AArguments[1], LList.Count() - 1, LList.Count(), -1);
		}
	}

	// operator LastIndexOf(const AList : list, const AValue : generic, const AStartIndex : Integer);
	public class ListLastIndexOfStartNode : BaseListIndexOfNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			#endif
			
			ListValue LList = (ListValue)AArguments[0];
			return InternalSearch(AProcess, LList, AArguments[1], (int)AArguments[2], LList.Count(), -1);
		}
	}

	// operator LastIndexOf(const AList : list, const AValue : generic, const AStartIndex : Integer, const ALength : Integer);
	public class ListLastIndexOfStartLengthNode : BaseListIndexOfNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null || AArguments[3] == null)
				return null;
			#endif
			
			return InternalSearch(AProcess, (ListValue)AArguments[0], AArguments[1], (int)AArguments[2], (int)AArguments[3], -1);
		}
	}

	// operator Remove(var AList : list, const AValue : generic);
	public class ListRemoveNode : BinaryInstructionNode
	{
		public PlanNode FEqualNode; // The equality node used to compare each item in the list against AValue
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			APlan.Symbols.Push(new Symbol("AValue", Nodes[1].DataType));
			try
			{
				APlan.Symbols.Push(new Symbol("ACompareValue", ((Schema.ListType)Nodes[0].DataType).ElementType));
				try
				{
					FEqualNode = Compiler.CompileExpression(APlan, new BinaryExpression(new IdentifierExpression("AValue"), Instructions.Equal, new IdentifierExpression("ACompareValue")));
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
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			APlan.Symbols.Push(new Symbol("AValue", Nodes[1].DataType));
			try
			{
				APlan.Symbols.Push(new Symbol("ACompareValue", ((Schema.ListType)Nodes[0].DataType).ElementType));
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
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			ListValue LList = (ListValue)AArgument1;
			int LListIndex = -1;
			AProcess.Context.Push(AArgument2);
			try
			{
				for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
				{
					AProcess.Context.Push(LList[LIndex]);
					try
					{
						object LValue = FEqualNode.Execute(AProcess);
						if ((LValue != null) && (bool)LValue)
						{
							LListIndex = LIndex;
							break;
						}
					}
					finally
					{
						AProcess.Context.Pop();
					}
				}
			}
			finally
			{
				AProcess.Context.Pop();
			}

			((ListValue)AArgument1).RemoveAt(LListIndex);
			return null;
		}
	}
	
	// operator iEqual(const ALeftList : list, const ARightList : list) : Boolean;
	public class ListEqualNode : BinaryInstructionNode
	{
		public PlanNode FEqualNode; // The equality node used to compare successive values in the lists
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			APlan.Symbols.Push(new Symbol("ALeftValue", ((Schema.ListType)Nodes[0].DataType).ElementType));
			try
			{
				APlan.Symbols.Push(new Symbol("ARightValue", ((Schema.ListType)Nodes[1].DataType).ElementType));
				try
				{
					FEqualNode = Compiler.CompileExpression(APlan, new BinaryExpression(new IdentifierExpression("ALeftValue"), Instructions.Equal, new IdentifierExpression("ARightValue")));
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
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			APlan.Symbols.Push(new Symbol("ALeftValue", ((Schema.ListType)Nodes[0].DataType).ElementType));
			try
			{
				APlan.Symbols.Push(new Symbol("ARightValue", ((Schema.ListType)Nodes[1].DataType).ElementType));
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
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			ListValue LLeftList = (ListValue)AArgument1;
			ListValue LRightList = (ListValue)AArgument2;
			#if NILPROPOGATION
			if ((LLeftList == null) || (LRightList == null))
				return null;
			#endif
			
			bool LListsEqual = LLeftList.Count() == LRightList.Count();
			if (LListsEqual)
			{
				for (int LIndex = 0; LIndex < LLeftList.Count(); LIndex++)
				{
					AProcess.Context.Push(LLeftList[LIndex]);
					try
					{
						AProcess.Context.Push(LRightList[LIndex]);
						try
						{
							object LValue = FEqualNode.Execute(AProcess);
							#if NILPROPOGATION
							if ((LValue == null))
								return null;
							#endif

							LListsEqual = (bool)LValue;
							if (!LListsEqual)
								break;
						}
						finally
						{
							AProcess.Context.Pop();
						}
					}
					finally
					{
						AProcess.Context.Pop();
					}
				}
			}

			return LListsEqual;
		}
	}

	// operator ToTable(const AList : list) : table
	// operator ToTable(const AList : list, const AColumnName : Name) : table
	// operator ToTable(const AList : list, const AColumnName : Name, const ASequenceName : Name) : table
	public class ListToTableNode : TableNode
	{
		public const string CDefaultColumnName = "value";
		public const string CDefaultSequenceName = "sequence";
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			Schema.ListType LListType = (Schema.ListType)Nodes[0].DataType;
			if (LListType.ElementType is Schema.RowType)
			{
				// If given a list of rows, use the row's columns for the table
				foreach (Schema.Column LColumn in ((Schema.RowType)LListType.ElementType).Columns)
					DataType.Columns.Add(LColumn.Copy());
			}
			else
			{
				// Determine the name for the value column
				string LColumnName = CDefaultColumnName;
				if (Nodes.Count >= 2)
				{
					if (!Nodes[1].IsLiteral)
						throw new CompilerException(CompilerException.Codes.LiteralArgumentRequired, 2.ToString());
					LColumnName = (string)Nodes[1].Execute(APlan.ServerProcess);
					SystemNameSelectorNode.CheckValidName(LColumnName);
				}
				
				DataType.Columns.Add(new Schema.Column(LColumnName, LListType.ElementType));
			}

			// Determine the sequence column name
			string LSequenceName = CDefaultSequenceName;
			if (Nodes.Count == 3)
			{
				if (!Nodes[2].IsLiteral)
					throw new CompilerException(CompilerException.Codes.LiteralArgumentRequired, "ASequenceName");
				LSequenceName = (string)Nodes[2].Execute(APlan.ServerProcess);
				SystemNameSelectorNode.CheckValidName(LSequenceName);
			}
			
			// Add sequence column and make it the key
			int LSequencePrefix = 0;
			while (DataType.Columns.ContainsName(BuildName(LSequenceName, LSequencePrefix)))
				LSequencePrefix++;
			Schema.Column LSequenceColumn = new Schema.Column(BuildName(LSequenceName, LSequencePrefix), APlan.Catalog.DataTypes.SystemInteger);
			DataType.Columns.Add(LSequenceColumn);
			FTableVar.EnsureTableVarColumns();
			FTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { FTableVar.Columns[LSequenceColumn.Name] }));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		private static string BuildName(string ASequenceName, int ASequencePrefix)
		{
			return ASequenceName + (ASequencePrefix == 0 ? "" : ASequencePrefix.ToString());
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();
				
				using (ListValue LListValue = Nodes[0].Execute(AProcess) as ListValue)
				{
					if (LListValue.DataType.ElementType is Schema.RowType)
					{
						for (int LIndex = 0; LIndex < LListValue.Count(); LIndex++)
						{
							Row LRow = new Row(AProcess, DataType.RowType);
							(LListValue[LIndex] as Row).CopyTo(LRow);
							LRow[DataType.RowType.Columns.Count - 1] = LIndex;
							LResult.Insert(LRow);
						}
					}
					else
					{
						for (int LIndex = 0; LIndex < LListValue.Count(); LIndex++)
						{
							Row LRow = new Row(AProcess, DataType.RowType);
							LRow[0] = LListValue[LIndex];
							LRow[1] = LIndex;
							LResult.Insert(LRow);
						}
					}
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}
	
	// operator ToList(const ATable : cursor) : list
	public class TableToListNode : InstructionNodeBase
	{
		public override void DetermineDataType(Plan APlan)
		{
			FDataType = new Schema.ListType(((Schema.CursorType)Nodes[0].DataType).TableType.RowType);
		}
		
		protected bool CursorNext(ServerProcess AProcess, Cursor ACursor)
		{
			ACursor.SwitchContext(AProcess);
			try
			{
				return ACursor.Table.Next();
			}
			finally
			{
				ACursor.SwitchContext(AProcess);
			}
		}
		
		protected Row CursorSelect(ServerProcess AProcess, Cursor ACursor)
		{
			ACursor.SwitchContext(AProcess);
			try
			{
				return ACursor.Table.Select();
			}
			finally
			{
				ACursor.SwitchContext(AProcess);
			}
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)Nodes[0].Execute(AProcess)).ID);
			try
			{
				ListValue LListValue = new ListValue(AProcess, (Schema.IListType)FDataType);
				while (CursorNext(AProcess, LCursor))
					LListValue.Add(CursorSelect(AProcess, LCursor));

				return LListValue;
			}
			finally
			{
				AProcess.Plan.CursorManager.CloseCursor(LCursor.ID);
			}
		}
	}
}
