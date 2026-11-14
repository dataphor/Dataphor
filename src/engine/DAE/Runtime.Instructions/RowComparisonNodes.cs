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
	// operator =(row, row) : bool
	// operator =(entry, entry) : bool
	// A row is equal to another row if it has the same number of columns and all columns by name have equal values
	public class RowEqualNode : InstructionNode
	{
		protected PlanNode _comparisonNode;
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = plan.DataTypes.SystemBoolean;
			
			if (!Nodes[0].DataType.IsGeneric && !Nodes[1].DataType.IsGeneric) // Generic row comparison must be done at run-time
			{
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
						plan.Symbols.Push(new Symbol(Keywords.Left, (Schema.RowType)Nodes[0].DataType));
						#else
						Schema.RowType leftRowType = new Schema.RowType(((Schema.RowType)Nodes[0].DataType).Columns, Keywords.Left);
						APlan.Symbols.Push(new Symbol(String.Empty, leftRowType));
						#endif
						try
						{
							#if USENAMEDROWVARIABLES
							plan.Symbols.Push(new Symbol(Keywords.Right, (Schema.RowType)Nodes[1].DataType));
							#else
							Schema.RowType rightRowType = new Schema.RowType(((Schema.RowType)Nodes[1].DataType).Columns, Keywords.Right);
							APlan.Symbols.Push(new Symbol(String.Empty, rightRowType));
							#endif
							try
							{
								_comparisonNode = 
									#if USENAMEDROWVARIABLES
									Compiler.CompileExpression(plan, Compiler.BuildRowEqualExpression(plan, Keywords.Left, Keywords.Right, ((Schema.RowType)Nodes[0].DataType).Columns, ((Schema.RowType)Nodes[1].DataType).Columns));
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
		}

		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			Nodes[1] = visitor.Visit(plan, Nodes[1]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			Nodes[1].BindingTraversal(plan, visitor);
			#endif
			if (_comparisonNode != null)
			{
				plan.Symbols.PushWindow(0);
				try
				{
					plan.EnterRowContext();
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Left, (Schema.RowType)Nodes[0].DataType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.RowType)Nodes[0].DataType).Columns, Keywords.Left)));
						#endif
						try
						{
							#if USENAMEDROWVARIABLES
							plan.Symbols.Push(new Symbol(Keywords.Right, (Schema.RowType)Nodes[1].DataType));
							#else
							APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.RowType)Nodes[1].DataType).Columns, Keywords.Right)));
							#endif
							try
							{
								#if USEVISIT
								_comparisonNode = visitor.Visit(plan, _comparisonNode);
								#else
								_comparisonNode.BindingTraversal(plan, visitor);
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
		}
		
		public override object InternalExecute(Program program, object[] arguments) { return null; }
		public override object InternalExecute(Program program)
		{
			object leftValue = Nodes[0].Execute(program);
			object rightValue = Nodes[1].Execute(program);
			
			#if NILPROPOGATION
			if ((leftValue == null) || (rightValue == null))
				return null;
			#endif

			if (_comparisonNode != null)
			{
				program.Stack.PushWindow(0);
				try
				{
					program.Stack.Push(leftValue);
					try
					{
						program.Stack.Push(rightValue);
						try
						{
							return _comparisonNode.Execute(program);
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
			else
			{
				RowEqualNode node = new RowEqualNode();
				node.Nodes.Add(new ValueNode(((IRow)leftValue).DataType, leftValue));
				node.Nodes.Add(new ValueNode(((IRow)rightValue).DataType, rightValue));
				node.DetermineDataType(program.Plan);
				return node.Execute(program);
			}
		}
	}

	public class CompoundScalarEqualNode : InstructionNode
	{
		protected PlanNode _comparisonNode;
		
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
					plan.Symbols.Push(new Symbol(Keywords.Left, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType));
					#else
					Schema.RowType leftRowType = new Schema.RowType(((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, Keywords.Left);
					APlan.Symbols.Push(new Symbol(String.Empty, leftRowType));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(String.Empty, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType));
						#else
						Schema.RowType rightRowType = new Schema.RowType(((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns, Keywords.Right);
						APlan.Symbols.Push(new Symbol(String.Empty, rightRowType));
						#endif
						try
						{
							_comparisonNode = 
								#if USENAMEDROWVARIABLES
								Compiler.CompileExpression(plan, Compiler.BuildRowEqualExpression(plan, Keywords.Left, Keywords.Right, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns));
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
			Nodes[0].BindingTraversal(plan, visitor);
			Nodes[1].BindingTraversal(plan, visitor);
			#endif
			plan.Symbols.PushWindow(0);
			try
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns, Keywords.Right)));
						#endif
						try
						{
							#if USEVISIT
							_comparisonNode = visitor.Visit(plan, _comparisonNode);
							#else
							_comparisonNode.BindingTraversal(plan, visitor);
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
		
		public override object InternalExecute(Program program, object[] arguments) { return null; }
		public override object InternalExecute(Program program)
		{
			object leftVar = Nodes[0].Execute(program);
			object rightVar = Nodes[1].Execute(program);
			
			#if NILPROPOGATION
			if ((leftVar == null) || (rightVar == null))
				return null;
			#endif
			
			object leftValue = DataValue.FromNative(program.ValueManager, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType, leftVar);
			object rightValue = DataValue.FromNative(program.ValueManager, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType, rightVar);
			program.Stack.PushWindow(0);
			try
			{
				program.Stack.Push(leftValue);
				try
				{
					program.Stack.Push(rightValue);
					try
					{
						return _comparisonNode.Execute(program);
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
	}
}
