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
		private Schema.AggregateOperator FOperator;
		public Schema.AggregateOperator Operator
		{
			get { return FOperator; }
			set { FOperator = value; }
		}
		
		protected int[] FAggregateColumnIndexes;
		public int[] AggregateColumnIndexes
		{
			get { return FAggregateColumnIndexes; }
			set { FAggregateColumnIndexes = value; }
		}
		
		protected string[] FValueNames;
		public string[] ValueNames
		{
			get { return FValueNames; }
			set { FValueNames = value; }
		}
		
		public TableNode SourceNode { get { return (TableNode)Nodes[0]; } }
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			
			// if the operator being invoked is order dependent, verify that the source is requested ordered by a unique order
			if (Operator.IsOrderDependent && !APlan.SuppressWarnings)
			{
				OrderNode LOrderNode = SourceNode as OrderNode;
				if (LOrderNode == null)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidOrderDependentAggregateInvocation, CompilerErrorLevel.Warning, Operator.OperatorName));
				else if (!Compiler.IsOrderUnique(APlan, SourceNode.TableVar, LOrderNode.RequestedOrder))
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidOrderDependentAggregateInvocationOrder, CompilerErrorLevel.Warning, Operator.OperatorName));
			}
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			if (Modifiers != null)
			{
				FIsLiteral = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsLiteral", Operator.IsLiteral.ToString()));
				FIsFunctional = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsFunctional", Operator.IsFunctional.ToString()));
				FIsDeterministic = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsDeterministic", Operator.IsDeterministic.ToString()));
				FIsRepeatable = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsRepeatable", Operator.IsRepeatable.ToString()));
				FIsNilable = Convert.ToBoolean(LanguageModifiers.GetModifier(Modifiers, "IsNilable", Operator.IsNilable.ToString()));
			}
			else
			{
				FIsLiteral = Operator.IsLiteral;
				FIsFunctional = Operator.IsFunctional;
				FIsDeterministic = Operator.IsDeterministic;
				FIsRepeatable = Operator.IsRepeatable;
				FIsNilable = Operator.IsNilable;
			}

			for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
			{
				FIsLiteral = FIsLiteral && Nodes[LIndex].IsLiteral;
				FIsFunctional = FIsFunctional && Nodes[LIndex].IsFunctional;
				FIsDeterministic = FIsDeterministic && Nodes[LIndex].IsDeterministic;
				FIsRepeatable = FIsRepeatable && Nodes[LIndex].IsRepeatable;
				FIsNilable = FIsNilable || Nodes[LIndex].IsNilable;
			} 
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.PushWindow(0);
			try
			{
				APlan.Symbols.Push(new Symbol(Keywords.Result, FDataType));
				try
				{
					Nodes[1].DetermineBinding(APlan);

					for (int LIndex = 0; LIndex < FAggregateColumnIndexes.Length; LIndex++)
						APlan.Symbols.Push(new Symbol(FValueNames[LIndex], SourceNode.DataType.Columns[FAggregateColumnIndexes[LIndex]].DataType));
					try
					{
						// This AllowExtraWindowAccess call remains in the runtime because it allows the
						// determine binding step to find the reference to the external source restriction values
						APlan.Symbols.AllowExtraWindowAccess = true;
						try
						{
							Nodes[0].DetermineBinding(APlan);
						}
						finally
						{
							APlan.Symbols.AllowExtraWindowAccess = false;
						}
						
						APlan.Symbols.PushFrame();
						try
						{
							Nodes[2].DetermineBinding(APlan);
						}
						finally
						{
							APlan.Symbols.PopFrame();
						}
					}
					finally
					{
						for (int LIndex = 0; LIndex < FAggregateColumnIndexes.Length; LIndex++)
							APlan.Symbols.Pop();
					}

					Nodes[3].DetermineBinding(APlan);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public override void BindToProcess(Plan APlan)
		{
			if (Operator != null)
			{
				APlan.CheckRight(Operator.GetRight(Schema.RightNames.Execute));
				APlan.EnsureApplicationTransactionOperator(Operator);
			}
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack.PushWindow(0, this, Operator.Locator);
			try
			{
				Table LTable = null;
				try
				{
					AProgram.Stack.Push(null); // result
					int LStackDepth = AProgram.Stack.Count;

					// Initialization
					try
					{
						Nodes[1].Execute(AProgram);
					}
					catch (ExitError){}
					
					// Aggregation
					Row LRow = null;
					if (FAggregateColumnIndexes.Length > 0)
						LRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType);
					//object[] LValues = new object[FAggregateColumnIndexes.Length];
					for (int LIndex = 0; LIndex < FAggregateColumnIndexes.Length; LIndex++)
					{
						Schema.IDataType LType = SourceNode.TableVar.Columns[FAggregateColumnIndexes[LIndex]].DataType;
						//LValues[LIndex] = new DataVar(FValueNames[LIndex], LType, null);
						AProgram.Stack.Push(null);
					}
					try
					{
						while (true)
						{
							#if DEBUG
							// This AllowExtraWindowAccess call is only necessary debug because the check is not
							// made in release executables
							AProgram.Stack.AllowExtraWindowAccess = true;
							try
							{
							#endif
								if (LTable == null)
								{
									LTable = (Table)Nodes[0].Execute(AProgram);
									LTable.Open();
								}

								if (!LTable.Next())
									break;
									
								if (FAggregateColumnIndexes.Length > 0)
									LTable.Select(LRow);
							#if DEBUG
							}
							finally
							{
								AProgram.Stack.AllowExtraWindowAccess = false;
							}
							#endif
							
							for (int LIndex = 0; LIndex < FAggregateColumnIndexes.Length; LIndex++)
								if (LRow.HasValue(FAggregateColumnIndexes[LIndex]))
									AProgram.Stack.Poke(FAggregateColumnIndexes.Length - 1 - LIndex, LRow[FAggregateColumnIndexes[LIndex]]);
								else
									AProgram.Stack.Poke(FAggregateColumnIndexes.Length - 1 - LIndex, null);
							
							AProgram.Stack.PushFrame();
							try
							{
								Nodes[2].Execute(AProgram);
							}
							catch (ExitError){}
							finally
							{
								AProgram.Stack.PopFrame();
							}
						}
					}
					finally
					{
						for (int LIndex = 0; LIndex < FAggregateColumnIndexes.Length; LIndex++)
							AProgram.Stack.Pop();

						if (FAggregateColumnIndexes.Length > 0)
							LRow.Dispose();
					}
					
					// Finalization
					try
					{
						Nodes[3].Execute(AProgram);
					}
					catch (ExitError){}
					
					return AProgram.Stack.Peek(AProgram.Stack.Count - LStackDepth);
				}
				finally
				{
					if (LTable != null)
						LTable.Dispose();
				}
			}
			finally
			{
				AProgram.Stack.PopWindow();
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			CallExpression LExpression = new CallExpression();
			LExpression.Identifier = Schema.Object.EnsureRooted(FOperator.OperatorName);
			Expression LSourceExpression = (Expression)Nodes[0].EmitStatement(AMode);
			if (FAggregateColumnIndexes.Length > 0)
			{
				if (FAggregateColumnIndexes.Length == 0)
					LExpression.Expressions.Add(new ColumnExtractorExpression(((TableNode)Nodes[0]).DataType.Columns[FAggregateColumnIndexes[0]].Name, LSourceExpression));
				else
				{
					ColumnExtractorExpression LColumnExpression = new ColumnExtractorExpression();
					LColumnExpression.Expression = LSourceExpression;
					for (int LIndex = 0; LIndex < FAggregateColumnIndexes.Length; LIndex++)
						LColumnExpression.Columns.Add(new ColumnExpression(((TableNode)Nodes[0]).DataType.Columns[FAggregateColumnIndexes[LIndex]].Name));
					LExpression.Expressions.Add(LColumnExpression);
				}
			}
			else
				LExpression.Expressions.Add(LSourceExpression);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
	}
	
	public class CountInitializationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = 0;
			return null;
		}
	}

    public class IntegerInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = null;
			return null;
		}
    }
    
    public class EmptyFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			return null;
		}
    }

    public class CountAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = checked((int)AProgram.Stack[0] + 1);
			return null;
		}
    }
    
    public class ObjectCountAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				AProgram.Stack[1] = checked((int)AProgram.Stack[1] + 1);
			return null;
		}
    }
    
    public class IntegerSumAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				AProgram.Stack[1] =
					checked
					(
						(int)AProgram.Stack[0] +
						(AProgram.Stack[1] == null ? 0 : (int)AProgram.Stack[1])
					);
			return null;
		}
    }
    
    public class IntegerMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if 
			(
				AProgram.Stack[0] != null && 
				(
					AProgram.Stack[1] == null || 
					((int)AProgram.Stack[0] < (int)AProgram.Stack[1])
				)
			)
				AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }
    
    public class IntegerMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if 
			(
				AProgram.Stack[0] != null && 
				(
					AProgram.Stack[1] == null || 
					((int)AProgram.Stack[0] > (int)AProgram.Stack[1])
				)
			)
				AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }
    
    public class IntegerAvgInitializationNode : PlanNode
    {
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.Push(new Symbol("LCounter", APlan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack.Push(0);
			AProgram.Stack[1] = 0;
			return null;
		}
    }
    
    public class IntegerAvgAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
			{
				AProgram.Stack[1] = checked((int)AProgram.Stack[1] + 1);
				AProgram.Stack[2] = checked((int)AProgram.Stack[2] + (int)AProgram.Stack[0]);
			}
			return null;
		}
    }
    
    public class IntegerAvgFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if ((int)AProgram.Stack[0] == 0)
				AProgram.Stack[1] = null;
			else
				AProgram.Stack[1] = (decimal)(int)AProgram.Stack[1] / (decimal)(int)AProgram.Stack[0];
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
		public override void InternalDetermineBinding(Plan APlan)
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
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = null;
			return null;
		}
    }
    
    public class DecimalSumAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				AProgram.Stack[1] = 
					AProgram.Stack[1] == null ? 
						(decimal)AProgram.Stack[0] : 
						((decimal)AProgram.Stack[1] + (decimal)AProgram.Stack[0]);
			return null;
		}
    }
    
    public class DecimalMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || ((decimal)AProgram.Stack[0] < (decimal)AProgram.Stack[1]))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }
    
    public class DecimalMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || ((decimal)AProgram.Stack[0] > (decimal)AProgram.Stack[1]))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }
    
    public class DecimalAvgInitializationNode : PlanNode
    {
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.Push(new Symbol("LCounter", APlan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack.Push(0);
			AProgram.Stack[1] = 0.0m;
			return null;
		}
    }
    
    public class DecimalAvgAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
			{
				AProgram.Stack[1] = checked((int)AProgram.Stack[1] + 1);
				AProgram.Stack[2] = (decimal)AProgram.Stack[2] + (decimal)AProgram.Stack[0];
			}
			return null;
		}
    }
    
    public class DecimalAvgFinalizationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if ((int)AProgram.Stack[0] == 0)
				AProgram.Stack[1] = null;
			else
				AProgram.Stack[1] = (decimal)AProgram.Stack[1] / (int)AProgram.Stack[0];
			return null;
		}
    }
    
	public class MoneyInitializationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = null;
			return null;
		}
	}
    
	public class MoneySumAggregationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				AProgram.Stack[1] = 
					AProgram.Stack[1] == null ? 
						(decimal)AProgram.Stack[0] :
						((decimal)AProgram.Stack[1] + (decimal)AProgram.Stack[0]);
			return null;
		}
	}
	
	public class MoneyMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || ((decimal)AProgram.Stack[0] < (decimal)AProgram.Stack[1]))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
	}
    
	public class MoneyMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || ((decimal)AProgram.Stack[0] > (decimal)AProgram.Stack[1]))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
	}
	
	public class MoneyAvgInitializationNode : PlanNode
	{
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.Push(new Symbol("LCounter", APlan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack.Push(0);
			AProgram.Stack[1] = 0.0m;
			return null;
		}
	}
    
	public class MoneyAvgAggregationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
			{
				AProgram.Stack[1] = checked((int)AProgram.Stack[1] + 1);
				AProgram.Stack[2] = (decimal)AProgram.Stack[2] + (decimal)AProgram.Stack[0];
			}
			return null;
		}
	}
	
	public class MoneyAvgFinalizationNode : PlanNode
	{
		public override object InternalExecute(Program AProgram)
		{
			if ((int)AProgram.Stack[0] == 0)
				AProgram.Stack[1] = null;
			else
				AProgram.Stack[1] = (decimal)AProgram.Stack[1] / (int)AProgram.Stack[0];
			return null;
		}
	}
	
	public class StringInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = null;
			return null;
		}
    }
    
    public class StringMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || (String.Compare((string)AProgram.Stack[0], (string)AProgram.Stack[1], false) < 0))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }
    
    public class StringMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if (AProgram.Stack[1] == null || (String.Compare((string)AProgram.Stack[0], (string)AProgram.Stack[1], false) > 0))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }

    public class VersionNumberMinAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if ((AProgram.Stack[1] == null) || (VersionNumber.Compare((VersionNumber)AProgram.Stack[0], (VersionNumber)AProgram.Stack[1]) < 0))
					AProgram.Stack[1] = AProgram.Stack[0];
			return null;
		}
    }
    
    public class VersionNumberMaxAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				if ((AProgram.Stack[1] == null) || (VersionNumber.Compare((VersionNumber)AProgram.Stack[0], (VersionNumber)AProgram.Stack[1]) > 0))
					AProgram.Stack[1] = AProgram.Stack[0];
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
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = true;
			return null;
		}
	}
	    
    public class BooleanAllAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				AProgram.Stack[1] = (bool)AProgram.Stack[1] && (bool)AProgram.Stack[0];
			return null;
		}
    }
    
    public class BooleanAnyInitializationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			AProgram.Stack[0] = false;
			return null;
		}
    }
    
    public class BooleanAnyAggregationNode : PlanNode
    {
		public override object InternalExecute(Program AProgram)
		{
			if (AProgram.Stack[0] != null)
				AProgram.Stack[1] = (bool)AProgram.Stack[1] || (bool)AProgram.Stack[0];
			return null;
		}
    }
}

