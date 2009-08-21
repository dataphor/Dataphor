/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define USECLEANUPNODES // Part of cleanup processing, currently disabled because cleanup processing is done with InstructionNodeBase.CleanupOperand

using System;
using System.Reflection;
using System.Reflection.Emit;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	/*
		InstructionNode prepares the AArguments set for use in virtual resolution.

		Passing an argument by value (in or no modifier) is a copy.  For local table
		variables this is accomplished as it is for all other variable types, by
		executing the argument node. For non-local table variables, this is accomplished
		by converting the result to a local table variable for passing into the operator.

		Passing an argument by reference (var, out or const) is not a copy.  For local
		table variables this is accomplished as it is for all other variable types, by 
		referencing the object from the current stack frame in the argument set.
		Non-local table variables cannot be passed by reference.
	*/
	public abstract class InstructionNodeBase : PlanNode
	{
        // constructor
        public InstructionNodeBase() : base() 
        {
			// InstructionNodes are by default not breakable (because for 99% of instructions this would be the
			// equivalent of breaking into the multiplication instruction in the processor, for example.
			//IsBreakable = true;
        }
        
		// Operator
		// The operator this node is implementing
		[Reference]
		private Schema.Operator FOperator;
		public Schema.Operator Operator
		{
			get { return FOperator; }
			set { FOperator = value; }
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{		
			if (FOperator.IsBuiltin && (NodeCount > 0) && (NodeCount <= 2))
			{
				Expression LExpression;
				if (Nodes.Count == 1)
					LExpression =
						new UnaryExpression
						(
							FOperator.OperatorName, 
							FOperator.Operands[0].Modifier == Modifier.Var ? 
								new ParameterExpression(Modifier.Var, (Expression)Nodes[0].EmitStatement(AMode)) : 
								(Expression)Nodes[0].EmitStatement(AMode)
						);
				else
					LExpression =
						new BinaryExpression
						(
							FOperator.Operands[0].Modifier == Modifier.Var ?
								new ParameterExpression(Modifier.Var, (Expression)Nodes[0].EmitStatement(AMode)) :
								(Expression)Nodes[0].EmitStatement(AMode), 
							FOperator.OperatorName, 
							FOperator.Operands[1].Modifier == Modifier.Var ?
								new ParameterExpression(Modifier.Var, (Expression)Nodes[1].EmitStatement(AMode)) :
								(Expression)Nodes[1].EmitStatement(AMode)
						);
				LExpression.Modifiers = Modifiers;
				return LExpression;
			}
			else
			{
				CallExpression LExpression = new CallExpression();
				LExpression.Identifier = Schema.Object.EnsureRooted(FOperator.OperatorName);
				for (int LIndex = 0; LIndex < NodeCount; LIndex++)
					if (FOperator.Operands[LIndex].Modifier == Modifier.Var)
						LExpression.Expressions.Add(new ParameterExpression(Modifier.Var, (Expression)Nodes[LIndex].EmitStatement(AMode)));
					else
						LExpression.Expressions.Add((Expression)Nodes[LIndex].EmitStatement(AMode));
				LExpression.Modifiers = Modifiers;
				return LExpression;
			}
		}
		
		#if USECLEANUPNODES
		protected PlanNodes FCleanupNodes;
		public PlanNodes CleanupNodes { get { return FCleanupNodes; } }
		#endif
		
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
			
			FIsOrderPreserving = Convert.ToBoolean(MetaData.GetTag(Operator.MetaData, "DAE.IsOrderPreserving", FIsOrderPreserving.ToString()));
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = Operator.ReturnDataType;
			
			for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
			{
				switch (Operator.Operands[LIndex].Modifier)
				{
					case Modifier.In : break;
					
					case Modifier.Var :
						if (Nodes[LIndex] is ParameterNode)
							Nodes[LIndex] = Nodes[LIndex].Nodes[0];
							
						if (!(Nodes[LIndex] is StackReferenceNode) || APlan.Symbols[((StackReferenceNode)Nodes[LIndex]).Location].IsConstant)
							throw new CompilerException(CompilerException.Codes.ConstantObjectCannotBePassedByReference, APlan.CurrentStatement(), Operator.Operands[LIndex].Name);

						((StackReferenceNode)Nodes[LIndex]).ByReference = true;
					break;

					case Modifier.Const :
						if (Nodes[LIndex] is ParameterNode)
							Nodes[LIndex] = Nodes[LIndex].Nodes[0];
							
						if (Nodes[LIndex] is StackReferenceNode)
							((StackReferenceNode)Nodes[LIndex]).ByReference = true;
						else if (Nodes[LIndex] is StackColumnReferenceNode)
							((StackColumnReferenceNode)Nodes[LIndex]).ByReference = true;
					break;
				}
			}
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			
			#if USECLEANUPNODES
			if (CleanupNodes != null)
				foreach (PlanNode LNode in CleanupNodes)
					LNode.DetermineBinding(APlan);
			#endif
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
		
		protected virtual void EmitInstructionIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath, LocalBuilder AArguments)
		{
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			if (ShouldEmitIL)
			{
				int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
				
				LocalBuilder LArguments = AGenerator.DeclareLocal(typeof(object[]));
				
				AGenerator.Emit(OpCodes.Ldc_I4, NodeCount);
				AGenerator.Emit(OpCodes.Newarr, typeof(object));
				AGenerator.Emit(OpCodes.Stloc, LArguments);
				
				for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
				{
					AGenerator.Emit(OpCodes.Ldloc, LArguments);
					AGenerator.Emit(OpCodes.Ldc_I4, LIndex);

					EmitEvaluate(APlan, AGenerator, LExecutePath, LIndex);
					
					AGenerator.Emit(OpCodes.Stelem_Ref);
				}
				
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					AGenerator.BeginExceptionBlock();
				#endif
				
				EmitInstructionIL(APlan, AGenerator, AExecutePath, LArguments);
				
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
				{
					AGenerator.BeginFinallyBlock();

					for (int LIndex = 0; LIndex < FCleanupNodes.Count; LIndex++)
					{
						FCleanupNodes[LIndex].EmitIL(APlan, AGenerator, AExecutePath);
						
						// Pop the return value if necessary
						if (FCleanupNodes[LIndex].DataType != null)
							AGenerator.Emit(OpCodes.Pop);
					}

					AGenerator.EndExceptionBlock();
				}
				#endif
			}
			else
				base.EmitIL(APlan, AGenerator, AExecutePath);
		}
		
		// TODO: This should be compiled and added as cleanup nodes
		// TODO: Handle var arguments for more than just stack references
		protected void CleanupOperand(ServerProcess AProcess, Schema.SignatureElement ASignatureElement, PlanNode AArgumentNode, object AArgumentValue)
		{
			switch (ASignatureElement.Modifier)
			{
				case Modifier.In :
					DataValue.DisposeValue(AProcess, AArgumentValue);
				break;
					
				case Modifier.Var :
					if (AArgumentNode.DataType is Schema.ScalarType)
						AArgumentValue = ((Schema.ScalarType)AArgumentNode.DataType).ValidateValue(AProcess, AArgumentValue, FOperator);
					AProcess.Stack.Poke(((StackReferenceNode)AArgumentNode).Location, AArgumentValue);
				break;
			}
		}
		
		public override string Category
		{
			get { return "Instruction"; }
		}
	}
	
	public abstract class NilaryInstructionNode : InstructionNodeBase
	{
		public abstract object NilaryInternalExecute(ServerProcess AProcess);

		public override object InternalExecute(ServerProcess AProcess)
		{
			#if USECLEANUPNODES
			try
			{
			#endif
				
				if (IsBreakable)
				{
				    AProcess.Stack.PushWindow(0); //, AProcess.ExecutingPlan, this);
				    try
				    {
				        return NilaryInternalExecute(AProcess);
				    }
				    finally
				    {
				        AProcess.Stack.PopWindow();
				    }
				}
				else
				{
					return NilaryInternalExecute(AProcess);
				}

			#if USECLEANUPNODES
			}
			finally
			{
				if (FCleanupNodes != null)
					for (int LIndex = 0; LIndex < FCleanupNodes.Count; LIndex++)
						FCleanupNodes[LIndex].Execute(AProcess);
			}
			#endif
		}
	}
	
	public abstract class UnaryInstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(ServerProcess AProcess, object AArgument1);
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			object LArgument = Nodes[0].Execute(AProcess);
			try
			{
				if (IsBreakable)
				{
				    AProcess.Stack.PushWindow(0); //, AProcess.ExecutingPlan, this);
				    try
				    {
				        return InternalExecute(AProcess, LArgument);
				    }
				    finally
				    {
				        AProcess.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(AProcess, LArgument);
				}
			}
			finally
			{
				if (Operator != null)
					CleanupOperand(AProcess, Operator.Signature[0], Nodes[0], LArgument);
				
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int LIndex = 0; LIndex < FCleanupNodes.Count; LIndex++)
						FCleanupNodes[LIndex].Execute(AProcess);
				#endif
			}
		}
	}
	
	public abstract class BinaryInstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2);
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			object LArgument1 = Nodes[0].Execute(AProcess);
			object LArgument2 = Nodes[1].Execute(AProcess);
			try
			{
				if (IsBreakable)
				{
				    AProcess.Stack.PushWindow(0); //, AProcess.ExecutingPlan, this);
				    try
				    {
				        return InternalExecute(AProcess, LArgument1, LArgument2);
				    }
				    finally
				    {
				        AProcess.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(AProcess, LArgument1, LArgument2);
				}
			}
			finally
			{
				if (Operator != null) 
				{
					CleanupOperand(AProcess, Operator.Signature[0], Nodes[0], LArgument1);
					CleanupOperand(AProcess, Operator.Signature[1], Nodes[1], LArgument2);
				}
					
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int LIndex = 0; LIndex < FCleanupNodes.Count; LIndex++)
						FCleanupNodes[LIndex].Execute(AProcess);
				#endif
			}
		}
	}
	
	public abstract class TernaryInstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3);
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			object LArgument1 = Nodes[0].Execute(AProcess);
			object LArgument2 = Nodes[1].Execute(AProcess);
			object LArgument3 = Nodes[2].Execute(AProcess);
			try
			{
				if (IsBreakable)
				{
				    AProcess.Stack.PushWindow(0); //, AProcess.ExecutingPlan, this);
				    try
				    {
				        return InternalExecute(AProcess, LArgument1, LArgument2, LArgument3);
				    }
				    finally
				    {
				        AProcess.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(AProcess, LArgument1, LArgument2, LArgument3);
				}
			}
			finally
			{
				if (Operator != null) 
				{
					CleanupOperand(AProcess, Operator.Signature[0], Nodes[0], LArgument1);
					CleanupOperand(AProcess, Operator.Signature[1], Nodes[1], LArgument2);
					CleanupOperand(AProcess, Operator.Signature[2], Nodes[2], LArgument3);
				}
					
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int LIndex = 0; LIndex < FCleanupNodes.Count; LIndex++)
						FCleanupNodes[LIndex].Execute(AProcess);
				#endif
			}
		}
	}
	
	public abstract class InstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(ServerProcess AProcess, object[] AArguments);

		public override object InternalExecute(ServerProcess AProcess)
		{
			object[] LArguments = new object[NodeCount];
			for (int LIndex = 0; LIndex < LArguments.Length; LIndex++)
				LArguments[LIndex] = Nodes[LIndex].Execute(AProcess);
			try
			{
				if (IsBreakable)
				{
				    AProcess.Stack.PushWindow(0); //, AProcess.ExecutingPlan, this);
				    try
				    {
				        return InternalExecute(AProcess, LArguments);
				    }
				    finally
				    {
				        AProcess.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(AProcess, LArguments);
				}
			}
			finally
			{
				// TODO: Compile the dispose calls and var argument assignment calls
				if (Operator != null)
					for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
						CleanupOperand(AProcess, Operator.Signature[LIndex], Nodes[LIndex], LArguments[LIndex]);

				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int LIndex = 0; LIndex < FCleanupNodes.Count; LIndex++)
						FCleanupNodes[LIndex].Execute(AProcess);
				#endif
			}
		}
	}
}

