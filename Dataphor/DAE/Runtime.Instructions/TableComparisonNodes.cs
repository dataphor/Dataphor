/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define NILPROPOGATION
#define USENAMEDROWVARIABLES
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	public abstract class TableComparisonNode : InstructionNode
	{
		protected PlanNode FRowEqualNode;
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = APlan.DataTypes.SystemBoolean;
			
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

			APlan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				APlan.Symbols.Push(new Symbol(Keywords.Left, LeftTableNode.DataType.RowType));
				#else
				Schema.IRowType LLeftRowType = LeftTableNode.DataType.CreateRowType(Keywords.Left);
				APlan.Symbols.Push(new Symbol(String.Empty, LLeftRowType));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Right, RightTableNode.DataType.RowType));
					#else
					Schema.IRowType LRightRowType = RightTableNode.DataType.CreateRowType(Keywords.Right);
					APlan.Symbols.Push(new Symbol(String.Empty, LRightRowType));
					#endif
					try
					{
						FRowEqualNode = 
							#if USENAMEDROWVARIABLES
							Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, Keywords.Left, Keywords.Right, LeftTableNode.TableVar.Columns, RightTableNode.TableVar.Columns));
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
			LeftTableNode.DetermineBinding(APlan);
			RightTableNode.DetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				APlan.Symbols.Push(new Symbol(Keywords.Left, LeftTableNode.DataType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, LeftTableNode.DataType.CreateRowType(Keywords.Left)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Right, RightTableNode.DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, RightTableNode.DataType.CreateRowType(Keywords.Right)));
					#endif
					try
					{
						RowEqualNode.DetermineBinding(APlan);
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

		protected bool IsSubset(Program AProgram, Table ALeftTable, Table ARightTable)
		{
			Row LLeftRow = new Row(AProgram.ValueManager, LeftTableNode.DataType.RowType);
			try
			{
				Row LRightRow = new Row(AProgram.ValueManager, RightTableNode.DataType.RowType);
				try
				{
					if (ALeftTable.Supports(CursorCapability.BackwardsNavigable))
						ALeftTable.First();
					else
						ALeftTable.Reset();
					while (ALeftTable.Next())
					{
						ALeftTable.Select(LLeftRow);
						bool LHasRow = false;
						if (ARightTable.Supports(CursorCapability.BackwardsNavigable))
							ARightTable.First();
						else
							ARightTable.Reset();
						while (ARightTable.Next())
						{
							ARightTable.Select(LRightRow);
							AProgram.Stack.Push(LLeftRow);
							try
							{
								AProgram.Stack.Push(LRightRow);
								try
								{
									object LResult = RowEqualNode.Execute(AProgram);
									if ((LResult != null) && (bool)LResult)
									{
										LHasRow = true;
										break;
									}
								}
								finally
								{
									AProgram.Stack.Pop();
								}
							}
							finally
							{
								AProgram.Stack.Pop();
							}
						}
						if (!LHasRow)
							return false;
					}
					return true;
				}
				finally
				{
					LRightRow.Dispose();
				}
			}
			finally
			{
				LLeftRow.Dispose();
			}
		}
		
		protected bool IsSuperset(Program AProgram, Table ALeftTable, Table ARightTable)
		{
			Row LLeftRow = new Row(AProgram.ValueManager, LeftTableNode.DataType.RowType);
			try
			{
				Row LRightRow = new Row(AProgram.ValueManager, RightTableNode.DataType.RowType);
				try
				{
					if (ARightTable.Supports(CursorCapability.BackwardsNavigable))
						ARightTable.First();
					else
						ARightTable.Reset();
					while (ARightTable.Next())
					{
						ARightTable.Select(LRightRow);
						bool LHasRow = false;
						if (ALeftTable.Supports(CursorCapability.BackwardsNavigable))
							ALeftTable.First();
						else
							ALeftTable.Reset();
						while (ALeftTable.Next())
						{
							ALeftTable.Select(LLeftRow);
							AProgram.Stack.Push(LLeftRow);
							try
							{
								AProgram.Stack.Push(LRightRow);
								try
								{
									object LResult = RowEqualNode.Execute(AProgram);
									if ((LResult != null) && (bool)LResult)
									{
										LHasRow = true;
										break;
									}
								}
								finally
								{
									AProgram.Stack.Pop();
								}
							}
							finally
							{
								AProgram.Stack.Pop();
							}
						}
						if (!LHasRow)
							return false;
					}
					return true;
				}
				finally
				{
					LRightRow.Dispose();
				}
			}
			finally
			{
				LLeftRow.Dispose();
			}
		}
		
		protected TableNode LeftTableNode { get { return (TableNode)Nodes[0]; } }
		protected TableNode RightTableNode { get { return (TableNode)Nodes[1]; } }
		protected PlanNode RowEqualNode { get { return FRowEqualNode; } }
		public override object InternalExecute(Program AProgram, object[] AArguments) { return null; }
	}
	
	// Table comparison operators
	// operator iEqual(table, table) : bool;
	// operator iEqual(presentation, presentation) : bool;
	// A = B iff A <= B and A >= B
	public class TableEqualNode : TableComparisonNode
	{
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProgram))
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProgram))
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || (LRightTable == null))
						return null;
					#endif

					return IsSubset(AProgram, LLeftTable, LRightTable) && IsSuperset(AProgram, LLeftTable, LRightTable);
				}
			}
		}
	}
	
	// operator iNotEqual(table, table) : bool;
	// operator iNotEqual(presentation, presentation) : bool;
	// A != B iff A !<= B or A !>= B
	public class TableNotEqualNode : TableComparisonNode
	{
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProgram))
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProgram))
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || (LRightTable == null))
						return null;
					#endif

					return !IsSubset(AProgram, LLeftTable, LRightTable) || !IsSuperset(AProgram, LLeftTable, LRightTable);
				}
			}
		}
	}
	
	// proper subset operator
	// operator iLess(table, table) : bool;
	// operator iLess(presentation, presentation) : bool;
	// A < B iff A <= B and A !>= B
	public class TableLessNode : TableComparisonNode
	{
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProgram))
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProgram))
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || (LRightTable == null))
						return null;
					#endif

					return IsSubset(AProgram, LLeftTable, LRightTable) && !IsSuperset(AProgram, LLeftTable, LRightTable);
				}
			}
		}
	}
	
	// subset operator
	// operator iInclusiveLess(table, table) : bool;
	// operator iInclusiveLess(presentation, presentation) : bool;
	// A <= B iff each row in A is also in B
	public class TableInclusiveLessNode : TableComparisonNode
	{
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProgram))
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProgram))
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || (LRightTable == null))
						return null;
					#endif

					return IsSubset(AProgram, LLeftTable, LRightTable);
				}
			}
		}
	}
	
	// proper superset operator
	// operator iGreater(table, table) : bool;
	// operator iGreater(presentation, presentation) : bool;
	// A > B iff A >= B and A !<= B
	public class TableGreaterNode : TableComparisonNode
	{
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProgram))
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProgram))
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || (LRightTable == null))
						return null;
					#endif

					return IsSuperset(AProgram, LLeftTable, LRightTable) && !IsSubset(AProgram, LLeftTable, LRightTable);
				}
			}
		}
	}
	
	// superset operator
	// operator iInclusiveGreater(table, table) : bool;
	// operator iInclusiveGreater(presentation, presentation) : bool;
	// A >= B iff each row in B is in A
	public class TableInclusiveGreaterNode : TableComparisonNode
	{
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProgram))
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProgram))
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || (LRightTable == null))
						return null;
					#endif

					return IsSuperset(AProgram, LLeftTable, LRightTable);
				}
			}
		}
	}
}
