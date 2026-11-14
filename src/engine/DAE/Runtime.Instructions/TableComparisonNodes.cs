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
using Alphora.Dataphor.DAE.Compiling.Visitors;
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
		public TableComparisonNode()
		{
			ExpectsTableValues = false;
		}

		protected PlanNode _rowEqualNode;
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = plan.DataTypes.SystemBoolean;
			
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

			plan.Symbols.PushWindow(0);
			try
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableNode.DataType.RowType));
					#else
					Schema.IRowType leftRowType = LeftTableNode.DataType.CreateRowType(Keywords.Left);
					APlan.Symbols.Push(new Symbol(String.Empty, leftRowType));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, RightTableNode.DataType.RowType));
						#else
						Schema.IRowType rightRowType = RightTableNode.DataType.CreateRowType(Keywords.Right);
						APlan.Symbols.Push(new Symbol(String.Empty, rightRowType));
						#endif
						try
						{
							_rowEqualNode = 
								#if USENAMEDROWVARIABLES
								Compiler.CompileExpression(plan, Compiler.BuildRowEqualExpression(plan, Keywords.Left, Keywords.Right, LeftTableNode.TableVar.Columns, RightTableNode.TableVar.Columns));
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
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			Nodes[1] = visitor.Visit(plan, Nodes[1]);
			#else
			LeftTableNode.BindingTraversal(plan, visitor);
			RightTableNode.BindingTraversal(plan, visitor);
			#endif
			plan.Symbols.PushWindow(0);
			try
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableNode.DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, LeftTableNode.DataType.CreateRowType(Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, RightTableNode.DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, RightTableNode.DataType.CreateRowType(Keywords.Right)));
						#endif
						try
						{
							#if USEVISIT
							_rowEqualNode = visitor.Visit(plan, _rowEqualNode);
							#else
							RowEqualNode.BindingTraversal(plan, visitor);
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
			finally
			{
				plan.Symbols.PopWindow();
			}
		}

		protected bool IsSubset(Program program, ITable leftTable, ITable rightTable)
		{
			Row leftRow = new Row(program.ValueManager, LeftTableNode.DataType.RowType);
			try
			{
				Row rightRow = new Row(program.ValueManager, RightTableNode.DataType.RowType);
				try
				{
					if (leftTable.Supports(CursorCapability.BackwardsNavigable))
						leftTable.First();
					else
						leftTable.Reset();
					while (leftTable.Next())
					{
						leftTable.Select(leftRow);
						bool hasRow = false;
						if (rightTable.Supports(CursorCapability.BackwardsNavigable))
							rightTable.First();
						else
							rightTable.Reset();
						while (rightTable.Next())
						{
							rightTable.Select(rightRow);
							program.Stack.PushWindow(0);
							try
							{
								program.Stack.Push(leftRow);
								try
								{
									program.Stack.Push(rightRow);
									try
									{
										object result = RowEqualNode.Execute(program);
										if ((result != null) && (bool)result)
										{
											hasRow = true;
											break;
										}
									}
									finally
									{
										program.Stack.Pop();
									}
								}
								finally
								{
									program.Stack.Pop();
								}
							}
							finally
							{
								program.Stack.PopWindow();
							}
						}
						if (!hasRow)
							return false;
					}
					return true;
				}
				finally
				{
					rightRow.Dispose();
				}
			}
			finally
			{
				leftRow.Dispose();
			}
		}
		
		protected bool IsSuperset(Program program, ITable leftTable, ITable rightTable)
		{
			Row leftRow = new Row(program.ValueManager, LeftTableNode.DataType.RowType);
			try
			{
				Row rightRow = new Row(program.ValueManager, RightTableNode.DataType.RowType);
				try
				{
					if (rightTable.Supports(CursorCapability.BackwardsNavigable))
						rightTable.First();
					else
						rightTable.Reset();
					while (rightTable.Next())
					{
						rightTable.Select(rightRow);
						bool hasRow = false;
						if (leftTable.Supports(CursorCapability.BackwardsNavigable))
							leftTable.First();
						else
							leftTable.Reset();
						while (leftTable.Next())
						{
							leftTable.Select(leftRow);
							program.Stack.PushWindow(0);
							try
							{
								program.Stack.Push(leftRow);
								try
								{
									program.Stack.Push(rightRow);
									try
									{
										object result = RowEqualNode.Execute(program);
										if ((result != null) && (bool)result)
										{
											hasRow = true;
											break;
										}
									}
									finally
									{
										program.Stack.Pop();
									}
								}
								finally
								{
									program.Stack.Pop();
								}
							}
							finally
							{
								program.Stack.PopWindow();
							}
						}
						if (!hasRow)
							return false;
					}
					return true;
				}
				finally
				{
					rightRow.Dispose();
				}
			}
			finally
			{
				leftRow.Dispose();
			}
		}
		
		protected TableNode LeftTableNode { get { return (TableNode)Nodes[0]; } }
		protected TableNode RightTableNode { get { return (TableNode)Nodes[1]; } }
		protected PlanNode RowEqualNode { get { return _rowEqualNode; } }
		public override object InternalExecute(Program program, object[] arguments) { return null; }
	}
	
	// Table comparison operators
	// operator iEqual(table, table) : bool;
	// operator iEqual(presentation, presentation) : bool;
	// A = B iff A <= B and A >= B
	public class TableEqualNode : TableComparisonNode
	{
		public override object InternalExecute(Program program)
		{
			using (ITable leftTable = (ITable)Nodes[0].Execute(program))
			{
				using (ITable rightTable = (ITable)Nodes[1].Execute(program))
				{
					#if NILPROPOGATION
					if ((leftTable == null) || (rightTable == null))
						return null;
					#endif

					return IsSubset(program, leftTable, rightTable) && IsSuperset(program, leftTable, rightTable);
				}
			}
		}
	}
	
	// operator iNotEqual(table, table) : bool;
	// operator iNotEqual(presentation, presentation) : bool;
	// A != B iff A !<= B or A !>= B
	public class TableNotEqualNode : TableComparisonNode
	{
		public override object InternalExecute(Program program)
		{
			using (ITable leftTable = (ITable)Nodes[0].Execute(program))
			{
				using (ITable rightTable = (ITable)Nodes[1].Execute(program))
				{
					#if NILPROPOGATION
					if ((leftTable == null) || (rightTable == null))
						return null;
					#endif

					return !IsSubset(program, leftTable, rightTable) || !IsSuperset(program, leftTable, rightTable);
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
		public override object InternalExecute(Program program)
		{
			using (ITable leftTable = (ITable)Nodes[0].Execute(program))
			{
				using (ITable rightTable = (ITable)Nodes[1].Execute(program))
				{
					#if NILPROPOGATION
					if ((leftTable == null) || (rightTable == null))
						return null;
					#endif

					return IsSubset(program, leftTable, rightTable) && !IsSuperset(program, leftTable, rightTable);
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
		public override object InternalExecute(Program program)
		{
			using (ITable leftTable = (ITable)Nodes[0].Execute(program))
			{
				using (ITable rightTable = (ITable)Nodes[1].Execute(program))
				{
					#if NILPROPOGATION
					if ((leftTable == null) || (rightTable == null))
						return null;
					#endif

					return IsSubset(program, leftTable, rightTable);
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
		public override object InternalExecute(Program program)
		{
			using (ITable leftTable = (ITable)Nodes[0].Execute(program))
			{
				using (ITable rightTable = (ITable)Nodes[1].Execute(program))
				{
					#if NILPROPOGATION
					if ((leftTable == null) || (rightTable == null))
						return null;
					#endif

					return IsSuperset(program, leftTable, rightTable) && !IsSubset(program, leftTable, rightTable);
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
		public override object InternalExecute(Program program)
		{
			using (ITable leftTable = (ITable)Nodes[0].Execute(program))
			{
				using (ITable rightTable = (ITable)Nodes[1].Execute(program))
				{
					#if NILPROPOGATION
					if ((leftTable == null) || (rightTable == null))
						return null;
					#endif

					return IsSuperset(program, leftTable, rightTable);
				}
			}
		}
	}
}
