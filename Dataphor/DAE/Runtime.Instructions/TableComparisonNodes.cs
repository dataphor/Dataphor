/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define NILPROPOGATION
	
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
	
	public abstract class TableComparisonNode : InstructionNode
	{
		protected PlanNode FRowEqualNode;
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = APlan.Catalog.DataTypes.SystemBoolean;
			
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
				Schema.IRowType LLeftRowType = LeftTableNode.DataType.CreateRowType(Keywords.Left);
				APlan.Symbols.Push(new DataVar(LLeftRowType));
				try
				{
					Schema.IRowType LRightRowType = RightTableNode.DataType.CreateRowType(Keywords.Right);
					APlan.Symbols.Push(new DataVar(LRightRowType));
					try
					{
						FRowEqualNode = Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, LLeftRowType.Columns, LRightRowType.Columns));
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
				APlan.Symbols.Push(new DataVar(LeftTableNode.DataType.CreateRowType(Keywords.Left)));
				try
				{
					APlan.Symbols.Push(new DataVar(RightTableNode.DataType.CreateRowType(Keywords.Right)));
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

		protected bool IsSubset(ServerProcess AProcess, Table ALeftTable, Table ARightTable)
		{
			Row LLeftRow = new Row(AProcess, LeftTableNode.DataType.RowType);
			try
			{
				Row LRightRow = new Row(AProcess, RightTableNode.DataType.RowType);
				try
				{
					DataVar LLeftVar = new DataVar(LLeftRow.DataType, LLeftRow);
					DataVar LRightVar = new DataVar(LRightRow.DataType, LRightRow);
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
							AProcess.Context.Push(LLeftVar);
							try
							{
								AProcess.Context.Push(LRightVar);
								try
								{
									DataVar LResult = RowEqualNode.Execute(AProcess);
									if ((LResult.Value != null) && !LResult.Value.IsNil && LResult.Value.AsBoolean)
									{
										LHasRow = true;
										break;
									}
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
		
		protected bool IsSuperset(ServerProcess AProcess, Table ALeftTable, Table ARightTable)
		{
			Row LLeftRow = new Row(AProcess, LeftTableNode.DataType.RowType);
			try
			{
				Row LRightRow = new Row(AProcess, RightTableNode.DataType.RowType);
				try
				{
					DataVar LLeftVar = new DataVar(LLeftRow.DataType, LLeftRow);
					DataVar LRightVar = new DataVar(LRightRow.DataType, LRightRow);
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
							AProcess.Context.Push(LLeftVar);
							try
							{
								AProcess.Context.Push(LRightVar);
								try
								{
									DataVar LResult = RowEqualNode.Execute(AProcess);
									if ((LResult.Value != null) && !LResult.Value.IsNil && LResult.Value.AsBoolean)
									{
										LHasRow = true;
										break;
									}
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
	}
	
	// Table comparison operators
	// operator iEqual(table, table) : bool;
	// operator iEqual(presentation, presentation) : bool;
	// A = B iff A <= B and A >= B
	public class TableEqualNode : TableComparisonNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProcess).Value)
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProcess).Value)
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || LLeftTable.IsNil || (LRightTable == null) || LRightTable.IsNil)
						return new DataVar(FDataType, null);
					#endif

					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, IsSubset(AProcess, LLeftTable, LRightTable) && IsSuperset(AProcess, LLeftTable, LRightTable)));
				}
			}
		}
	}
	
	// operator iNotEqual(table, table) : bool;
	// operator iNotEqual(presentation, presentation) : bool;
	// A != B iff A !<= B or A !>= B
	public class TableNotEqualNode : TableComparisonNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProcess).Value)
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProcess).Value)
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || LLeftTable.IsNil || (LRightTable == null) || LRightTable.IsNil)
						return new DataVar(FDataType, null);
					#endif

					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, !IsSubset(AProcess, LLeftTable, LRightTable) || !IsSuperset(AProcess, LLeftTable, LRightTable)));
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
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProcess).Value)
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProcess).Value)
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || LLeftTable.IsNil || (LRightTable == null) || LRightTable.IsNil)
						return new DataVar(FDataType, null);
					#endif

					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, IsSubset(AProcess, LLeftTable, LRightTable) && !IsSuperset(AProcess, LLeftTable, LRightTable)));
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
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProcess).Value)
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProcess).Value)
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || LLeftTable.IsNil || (LRightTable == null) || LRightTable.IsNil)
						return new DataVar(FDataType, null);
					#endif

					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, IsSubset(AProcess, LLeftTable, LRightTable)));
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
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProcess).Value)
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProcess).Value)
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || LLeftTable.IsNil || (LRightTable == null) || LRightTable.IsNil)
						return new DataVar(FDataType, null);
					#endif

					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, IsSuperset(AProcess, LLeftTable, LRightTable) && !IsSubset(AProcess, LLeftTable, LRightTable)));
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
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLeftTable = (Table)Nodes[0].Execute(AProcess).Value)
			{
				using (Table LRightTable = (Table)Nodes[1].Execute(AProcess).Value)
				{
					#if NILPROPOGATION
					if ((LLeftTable == null) || LLeftTable.IsNil || (LRightTable == null) || LRightTable.IsNil)
						return new DataVar(FDataType, null);
					#endif

					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, IsSuperset(AProcess, LLeftTable, LRightTable)));
				}
			}
		}
	}
}
