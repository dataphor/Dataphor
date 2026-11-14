/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Compiling.Visitors;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// Nodes[0] - SourceNode
	// Nodes[1] - InitializationNode
	// Nodes[2] - AggregationNode
	// Nodes[3] - FinalizationNode
	public class AggregateCallNode : PlanNode
	{
		// Operator
		// The operator this node is implementing
		private Schema.AggregateOperator _operator;
		public Schema.AggregateOperator Operator
		{
			get { return _operator; }
			set { _operator = value; }
		}
		
		protected int[] _aggregateColumnIndexes;
		public int[] AggregateColumnIndexes
		{
			get { return _aggregateColumnIndexes; }
			set { _aggregateColumnIndexes = value; }
		}
		
		protected string[] _valueNames;
		public string[] ValueNames
		{
			get { return _valueNames; }
			set { _valueNames = value; }
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newAggregateNode = (AggregateCallNode)newNode;
			newAggregateNode.Operator = _operator;
			newAggregateNode.AggregateColumnIndexes = _aggregateColumnIndexes;
			newAggregateNode.ValueNames = _valueNames;
		}
		
		public TableNode SourceNode { get { return (TableNode)Nodes[0]; } }
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			
			// if the operator being invoked is order dependent, verify that the source is requested ordered by a unique order
			if (Operator.IsOrderDependent && !plan.SuppressWarnings)
			{
				OrderNode orderNode = SourceNode as OrderNode;
				if (orderNode == null)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidOrderDependentAggregateInvocation, CompilerErrorLevel.Warning, Operator.OperatorName));
				else if (!Compiler.IsOrderUnique(plan, SourceNode.TableVar, orderNode.RequestedOrder))
					plan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidOrderDependentAggregateInvocationOrder, CompilerErrorLevel.Warning, Operator.OperatorName));
			}
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			if (Modifiers != null)
			{
				IsLiteral = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsLiteral", Operator.IsLiteral.ToString()));
				IsFunctional = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsFunctional", Operator.IsFunctional.ToString()));
				IsDeterministic = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsDeterministic", Operator.IsDeterministic.ToString()));
				IsRepeatable = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsRepeatable", Operator.IsRepeatable.ToString()));
				IsNilable = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsNilable", Operator.IsNilable.ToString()));
			}
			else
			{
				IsLiteral = Operator.IsLiteral;
				IsFunctional = Operator.IsFunctional;
				IsDeterministic = Operator.IsDeterministic;
				IsRepeatable = Operator.IsRepeatable;
				IsNilable = Operator.IsNilable;
			}

			// Characteristics of an aggregate operator, unless overridden, are always based on the source node, rather than the
			// actual operands to the operator.
			IsLiteral = IsLiteral && Nodes[0].IsLiteral;
			IsFunctional = IsFunctional && Nodes[0].IsFunctional;
			IsDeterministic = IsDeterministic && Nodes[0].IsDeterministic;
			IsRepeatable = IsRepeatable && Nodes[0].IsRepeatable;
			IsNilable = IsNilable || Nodes[0].IsNilable;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.PushWindow(0);
			try
			{
				plan.Symbols.Push(new Symbol(Keywords.Result, _dataType));
				try
				{
					#if USEVISIT
					Nodes[1] = visitor.Visit(plan, Nodes[1]);
					#else
					Nodes[1].BindingTraversal(plan, visitor);
					#endif

					for (int index = 0; index < _aggregateColumnIndexes.Length; index++)
						plan.Symbols.Push(new Symbol(_valueNames[index], SourceNode.DataType.Columns[_aggregateColumnIndexes[index]].DataType));
					try
					{
						// This AllowExtraWindowAccess call remains in the runtime because it allows the
						// determine binding step to find the reference to the external source restriction values
						plan.Symbols.AllowExtraWindowAccess = true;
						try
						{
							#if USEVISIT
							Nodes[0] = visitor.Visit(plan, Nodes[0]);
							#else
							Nodes[0].BindingTraversal(plan, visitor);
							#endif
						}
						finally
						{
							plan.Symbols.AllowExtraWindowAccess = false;
						}
						
						plan.Symbols.PushFrame();
						try
						{
							#if USEVISIT
							Nodes[2] = visitor.Visit(plan, Nodes[2]);
							#else
							Nodes[2].BindingTraversal(plan, visitor);
							#endif
						}
						finally
						{
							plan.Symbols.PopFrame();
						}
					}
					finally
					{
						for (int index = 0; index < _aggregateColumnIndexes.Length; index++)
							plan.Symbols.Pop();
					}

					#if USEVISIT
					Nodes[3] = visitor.Visit(plan, Nodes[3]);
					#else
					Nodes[3].BindingTraversal(plan, visitor);
					#endif
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public override void BindToProcess(Plan plan)
		{
			if (Operator != null)
			{
				plan.CheckRight(Operator.GetRight(Schema.RightNames.Execute));
				plan.EnsureApplicationTransactionOperator(Operator);
			}
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			program.Stack.PushWindow(0, this, Operator.Locator);
			try
			{
				ITable table = null;
				try
				{
					program.Stack.Push(null); // result
					int stackDepth = program.Stack.Count;

					// Initialization
					try
					{
						Nodes[1].Execute(program);
					}
					catch (ExitError){}
					
					// Aggregation
					Row row = null;
					if (_aggregateColumnIndexes.Length > 0)
						row = new Row(program.ValueManager, SourceNode.DataType.RowType);
					//object[] LValues = new object[FAggregateColumnIndexes.Length];
					for (int index = 0; index < _aggregateColumnIndexes.Length; index++)
					{
						Schema.IDataType type = SourceNode.TableVar.Columns[_aggregateColumnIndexes[index]].DataType;
						//LValues[LIndex] = new DataVar(FValueNames[LIndex], LType, null);
						program.Stack.Push(null);
					}
					try
					{
						while (true)
						{
							#if DEBUG
							// This AllowExtraWindowAccess call is only necessary debug because the check is not
							// made in release executables
							program.Stack.AllowExtraWindowAccess = true;
							try
							{
							#endif
								if (table == null)
								{
									table = (ITable)Nodes[0].Execute(program);
									table.Open();
								}

								if (!table.Next())
									break;
									
								if (_aggregateColumnIndexes.Length > 0)
									table.Select(row);
							#if DEBUG
							}
							finally
							{
								program.Stack.AllowExtraWindowAccess = false;
							}
							#endif
							
							for (int index = 0; index < _aggregateColumnIndexes.Length; index++)
								if (row.HasValue(_aggregateColumnIndexes[index]))
									program.Stack.Poke(_aggregateColumnIndexes.Length - 1 - index, row[_aggregateColumnIndexes[index]]);
								else
									program.Stack.Poke(_aggregateColumnIndexes.Length - 1 - index, null);
							
							program.Stack.PushFrame();
							try
							{
								Nodes[2].Execute(program);
							}
							catch (ExitError){}
							finally
							{
								program.Stack.PopFrame();
							}
						}
					}
					finally
					{
						for (int index = 0; index < _aggregateColumnIndexes.Length; index++)
							program.Stack.Pop();

						if (_aggregateColumnIndexes.Length > 0)
							row.Dispose();
					}
					
					// Finalization
					try
					{
						Nodes[3].Execute(program);
					}
					catch (ExitError){}
					
					return program.Stack.Peek(program.Stack.Count - stackDepth);
				}
				finally
				{
					if (table != null)
						table.Dispose();
				}
			}
			finally
			{
				program.Stack.PopWindow();
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			CallExpression expression = new CallExpression();
			expression.Identifier = Schema.Object.EnsureRooted(_operator.OperatorName);
			Expression sourceExpression = (Expression)Nodes[0].EmitStatement(mode);
			if (_aggregateColumnIndexes.Length > 0)
			{
				if (_aggregateColumnIndexes.Length == 0)
					expression.Expressions.Add(new ColumnExtractorExpression(((TableNode)Nodes[0]).DataType.Columns[_aggregateColumnIndexes[0]].Name, sourceExpression));
				else
				{
					ColumnExtractorExpression columnExpression = new ColumnExtractorExpression();
					columnExpression.Expression = sourceExpression;
					for (int index = 0; index < _aggregateColumnIndexes.Length; index++)
						columnExpression.Columns.Add(new ColumnExpression(((TableNode)Nodes[0]).DataType.Columns[_aggregateColumnIndexes[index]].Name));
					expression.Expressions.Add(columnExpression);
				}
			}
			else
				expression.Expressions.Add(sourceExpression);
			expression.Modifiers = Modifiers;
			return expression;
		}
	}
	
	public class CountInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = 0;
			return null;
		}
	}

    public class IntegerInitializationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
    }
    
    public class EmptyFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			return null;
		}
    }

    public class CountAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = checked((int)program.Stack[0] + 1);
			return null;
		}
    }
    
    public class ObjectCountAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] = checked((int)program.Stack[1] + 1);
			return null;
		}
    }
    
    public class IntegerSumAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] =
					checked
					(
						(int)program.Stack[0] +
						(program.Stack[1] == null ? 0 : (int)program.Stack[1])
					);
			return null;
		}
    }
    
    public class IntegerMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if 
			(
				program.Stack[0] != null && 
				(
					program.Stack[1] == null || 
					((int)program.Stack[0] < (int)program.Stack[1])
				)
			)
				program.Stack[1] = program.Stack[0];
			return null;
		}
    }
    
    public class IntegerMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if 
			(
				program.Stack[0] != null && 
				(
					program.Stack[1] == null || 
					((int)program.Stack[0] > (int)program.Stack[1])
				)
			)
				program.Stack[1] = program.Stack[0];
			return null;
		}
    }
    
    public class IntegerAvgInitializationNode : PlanNode
    {
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.Push(new Symbol("LCounter", plan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program program)
		{
			program.Stack.Push(0);
			program.Stack[1] = 0;
			return null;
		}
    }
    
    public class IntegerAvgAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
			{
				program.Stack[1] = checked((int)program.Stack[1] + 1);
				program.Stack[2] = checked((int)program.Stack[2] + (int)program.Stack[0]);
			}
			return null;
		}
    }
    
    public class IntegerAvgFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if ((int)program.Stack[0] == 0)
				program.Stack[1] = null;
			else
				program.Stack[1] = (decimal)(int)program.Stack[1] / (decimal)(int)program.Stack[0];
			return null;
		}
    }

	public class LongInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}

	public class LongSumAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] =
					checked
					(
						(long)program.Stack[0] +
						(program.Stack[1] == null ? 0 : (long)program.Stack[1])
					);
			return null;
		}
	}

	public class LongMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if
			(
				program.Stack[0] != null &&
				(
					program.Stack[1] == null ||
					((long)program.Stack[0] < (long)program.Stack[1])
				)
			)
				program.Stack[1] = program.Stack[0];
			return null;
		}
	}

	public class LongMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if
			(
				program.Stack[0] != null &&
				(
					program.Stack[1] == null ||
					((long)program.Stack[0] > (long)program.Stack[1])
				)
			)
				program.Stack[1] = program.Stack[0];
			return null;
		}
	}

	public class LongAvgInitializationNode : PlanNode
	{
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.Push(new Symbol("LCounter", plan.DataTypes.SystemInteger));
		}

		public override object InternalExecute(Program program)
		{
			program.Stack.Push(0);
			program.Stack[1] = (long)0;
			return null;
		}
	}

	public class LongAvgAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
			{
				program.Stack[1] = checked((int)program.Stack[1] + 1);
				program.Stack[2] = checked((long)program.Stack[2] + (long)program.Stack[0]);
			}
			return null;
		}
	}

	public class LongAvgFinalizationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if ((int)program.Stack[0] == 0)
				program.Stack[1] = null;
			else
				program.Stack[1] = (decimal)(long)program.Stack[1] / (decimal)(int)program.Stack[0];
			return null;
		}
	}

	#if USEDOUBLE    
    public class DoubleInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0].Value = Scalar.FromDouble((double)0.0);
			return null;
		}
    }
    
    public class DoubleMinInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0].Value = Scalar.FromDouble(double.MaxValue);
			return null;
		}
    }
    
    public class DoubleMaxInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0].Value = Scalar.FromDouble(double.MinValue);
			return null;
		}
    }
    
    public class DoubleSumAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[1] = Scalar.FromDouble(AProgram.Stack[1].Value.AsDouble() + AProgram.Stack[0].Value.AsDouble());
			return null;
		}
    }
    
    public class DoubleMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0].Value.AsDouble() < AProgram.Stack[1].Value.AsDouble())
				AProgram.Stack[1] = AProgram.Stack[0].Value.Copy();
			return null;
		}
    }
    
    public class DoubleMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0].Value.AsDouble() > AProgram.Stack[1].Value.AsDouble())
				AProgram.Stack[1] = AProgram.Stack[0].Value.Copy();
			return null;
		}
    }
    
    public class DoubleAvgInitializationNode : PlanNode
    {
		protected override void InternalBindingTraversal(Plan APlan, PlanNodeVisitor visitor)
		{
			APlan.Symbols.Push(new Symbol("LCounter", Schema.DataType.SystemInteger));
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack.Push(0);
			AProgram.Stack[1] = Scalar.FromDouble((double)0.0);
			return null;
		}
    }
    
    public class DoubleAvgAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[1] = Scalar.FromInt32(AProcess, (int)AProgram.Stack[1] + 1);
			AProgram.Stack[2].Value = Scalar.FromDouble(AProgram.Stack[2].Value.AsDouble() + AProgram.Stack[0].Value.AsDouble());
			return null;
		}
    }
    
    public class DoubleAvgFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[1] = Scalar.FromDouble(AProgram.Stack[1].Value.AsDouble() / (int)AProgram.Stack[0]);
			return null;
		}
    }
    #endif
    
    public class DecimalInitializationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
    }
    
    public class DecimalSumAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] = 
					program.Stack[1] == null ? 
						(decimal)program.Stack[0] : 
						((decimal)program.Stack[1] + (decimal)program.Stack[0]);
			return null;
		}
    }
    
    public class DecimalMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if (program.Stack[1] == null || ((decimal)program.Stack[0] < (decimal)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
    }
    
    public class DecimalMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if (program.Stack[1] == null || ((decimal)program.Stack[0] > (decimal)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
    }
    
    public class DecimalAvgInitializationNode : PlanNode
    {
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.Push(new Symbol("LCounter", plan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program program)
		{
			program.Stack.Push(0);
			program.Stack[1] = 0.0m;
			return null;
		}
    }
    
    public class DecimalAvgAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
			{
				program.Stack[1] = checked((int)program.Stack[1] + 1);
				program.Stack[2] = (decimal)program.Stack[2] + (decimal)program.Stack[0];
			}
			return null;
		}
    }
    
    public class DecimalAvgFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if ((int)program.Stack[0] == 0)
				program.Stack[1] = null;
			else
				program.Stack[1] = (decimal)program.Stack[1] / (int)program.Stack[0];
			return null;
		}
    }
    
	public class MoneyInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
    
	public class MoneySumAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] = 
					program.Stack[1] == null ? 
						(decimal)program.Stack[0] :
						((decimal)program.Stack[1] + (decimal)program.Stack[0]);
			return null;
		}
	}
	
	public class MoneyMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if (program.Stack[1] == null || ((decimal)program.Stack[0] < (decimal)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class MoneyMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if (program.Stack[1] == null || ((decimal)program.Stack[0] > (decimal)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
	
	public class MoneyAvgInitializationNode : PlanNode
	{
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.Push(new Symbol("LCounter", plan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program program)
		{
			program.Stack.Push(0);
			program.Stack[1] = 0.0m;
			return null;
		}
	}
    
	public class MoneyAvgAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
			{
				program.Stack[1] = checked((int)program.Stack[1] + 1);
				program.Stack[2] = (decimal)program.Stack[2] + (decimal)program.Stack[0];
			}
			return null;
		}
	}
	
	public class MoneyAvgFinalizationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if ((int)program.Stack[0] == 0)
				program.Stack[1] = null;
			else
				program.Stack[1] = (decimal)program.Stack[1] / (int)program.Stack[0];
			return null;
		}
	}
	
	public class StringInitializationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
    }
    
    public class StringMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if (program.Stack[1] == null || (String.Compare((string)program.Stack[0], (string)program.Stack[1], StringComparison.OrdinalIgnoreCase) < 0))
					program.Stack[1] = program.Stack[0];
			return null;
		}
    }
    
    public class StringMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if (program.Stack[1] == null || (String.Compare((string)program.Stack[0], (string)program.Stack[1], StringComparison.OrdinalIgnoreCase) > 0))
					program.Stack[1] = program.Stack[0];
			return null;
		}
    }

    public class VersionNumberMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || (VersionNumber.Compare((VersionNumber)program.Stack[0], (VersionNumber)program.Stack[1]) < 0))
					program.Stack[1] = program.Stack[0];
			return null;
		}
    }
    
    public class VersionNumberMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || (VersionNumber.Compare((VersionNumber)program.Stack[0], (VersionNumber)program.Stack[1]) > 0))
					program.Stack[1] = program.Stack[0];
			return null;
		}
    }

	#if USEISTRING    
    public class IStringInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemIString, null);
			return null;
		}
    }
    
    public class IStringMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || (String.Compare((string)AProgram.Stack[0], (string)AProgram.Stack[1], true) < 0))
					AProgram.Stack[1] = AProgram.Stack[0].Value.Copy();
			return null;
		}
    }
    
    public class IStringMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || (String.Compare((string)AProgram.Stack[0], AProgram.Stack[2].Value.AsString, true) > 0))
					AProgram.Stack[1] = AProgram.Stack[0].Value.Copy();
			return null;
		}
    }
    #endif
    
	public class BooleanAllInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = true;
			return null;
		}
	}
	    
    public class BooleanAllAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] = (bool)program.Stack[1] && (bool)program.Stack[0];
			return null;
		}
    }
    
    public class BooleanAnyInitializationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = false;
			return null;
		}
    }
    
    public class BooleanAnyAggregationNode : PlanNode
    {
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] = (bool)program.Stack[1] || (bool)program.Stack[0];
			return null;
		}
    }
}

