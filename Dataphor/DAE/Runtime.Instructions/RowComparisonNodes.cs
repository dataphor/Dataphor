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

	// operator =(row, row) : bool
	// operator =(entry, entry) : bool
	// A row is equal to another row if it has the same number of columns and all columns by name have equal values
	public class RowEqualNode : InstructionNode
	{
		protected PlanNode FComparisonNode;
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = APlan.Catalog.DataTypes.SystemBoolean;
			
			if (!Nodes[0].DataType.IsGeneric && !Nodes[1].DataType.IsGeneric) // Generic row comparison must be done at run-time
			{
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

				Schema.RowType LLeftRowType = new Schema.RowType(((Schema.RowType)Nodes[0].DataType).Columns, Keywords.Left);
				Schema.RowType LRightRowType = new Schema.RowType(((Schema.RowType)Nodes[1].DataType).Columns, Keywords.Right);
				APlan.EnterRowContext();
				try
				{
					APlan.Symbols.Push(new Symbol(LLeftRowType));
					try
					{
						APlan.Symbols.Push(new Symbol(LRightRowType));
						try
						{
							FComparisonNode = Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, LLeftRowType.Columns, LRightRowType.Columns));
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
		}

		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			Nodes[1].DetermineBinding(APlan);
			if (FComparisonNode != null)
			{
				APlan.EnterRowContext();
				try
				{
					APlan.Symbols.Push(new Symbol(new Schema.RowType(((Schema.RowType)Nodes[0].DataType).Columns, Keywords.Left)));
					try
					{
						APlan.Symbols.Push(new Symbol(new Schema.RowType(((Schema.RowType)Nodes[1].DataType).Columns, Keywords.Right)));
						try
						{
							FComparisonNode.DetermineBinding(APlan);
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
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments) { return null; }
		public override object InternalExecute(ServerProcess AProcess)
		{
			object LLeftValue = Nodes[0].Execute(AProcess);
			object LRightValue = Nodes[1].Execute(AProcess);
			
			#if NILPROPOGATION
			if ((LLeftValue == null) || (LRightValue == null))
				return null;
			#endif

			if (FComparisonNode != null)
			{
				AProcess.Context.Push(LLeftValue);
				try
				{
					AProcess.Context.Push(LRightValue);
					try
					{
						return FComparisonNode.Execute(AProcess);
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
			else
			{
				RowEqualNode LNode = new RowEqualNode();
				LNode.Nodes.Add(new ValueNode(((Row)LLeftValue).DataType, LLeftValue));
				LNode.Nodes.Add(new ValueNode(((Row)LRightValue).DataType, LRightValue));
				LNode.DetermineDataType(AProcess.Plan);
				return LNode.Execute(AProcess);
			}
		}
	}

	public class CompoundScalarEqualNode : InstructionNode
	{
		protected PlanNode FComparisonNode;
		
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

			Schema.RowType LLeftRowType = new Schema.RowType(((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, Keywords.Left);
			Schema.RowType LRightRowType = new Schema.RowType(((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns, Keywords.Right);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(LLeftRowType));
				try
				{
					APlan.Symbols.Push(new Symbol(LRightRowType));
					try
					{
						FComparisonNode = Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, LLeftRowType.Columns, LRightRowType.Columns));
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
			Nodes[0].DetermineBinding(APlan);
			Nodes[1].DetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(new Schema.RowType(((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, Keywords.Left)));
				try
				{
					APlan.Symbols.Push(new Symbol(new Schema.RowType(((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns, Keywords.Right)));
					try
					{
						FComparisonNode.DetermineBinding(APlan);
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
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments) { return null; }
		public override object InternalExecute(ServerProcess AProcess)
		{
			object LLeftVar = Nodes[0].Execute(AProcess);
			object LRightVar = Nodes[1].Execute(AProcess);
			
			#if NILPROPOGATION
			if ((LLeftVar == null) || (LRightVar == null))
				return null;
			#endif
			
			object LLeftValue = DataValue.FromNative(AProcess, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType, LLeftVar);
			object LRightValue = DataValue.FromNative(AProcess, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType, LRightVar);
			AProcess.Context.Push(LLeftValue);
			try
			{
				AProcess.Context.Push(LRightValue);
				try
				{
					return FComparisonNode.Execute(AProcess);
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
}
