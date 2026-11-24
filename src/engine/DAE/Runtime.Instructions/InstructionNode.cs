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
using Alphora.Dataphor.DAE.Compiling.Visitors;
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
			ExpectsTableValues = true;
        }
        
		// Operator
		// The operator this node is implementing
		[Reference]
		private Schema.Operator _operator;
		public Schema.Operator Operator
		{
			get { return _operator; }
			set { _operator = value; }
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newInstructionNode = (InstructionNode)newNode;
			newInstructionNode.Operator = _operator;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{		
			if (_operator.IsBuiltin && (NodeCount > 0) && (NodeCount <= 2))
			{
				Expression expression;
				if (Nodes.Count == 1)
					expression =
						new UnaryExpression
						(
							_operator.OperatorName, 
							_operator.Operands[0].Modifier == Modifier.Var ? 
								new ParameterExpression(Modifier.Var, (Expression)Nodes[0].EmitStatement(mode)) : 
								(Expression)Nodes[0].EmitStatement(mode)
						);
				else
					expression =
						new BinaryExpression
						(
							_operator.Operands[0].Modifier == Modifier.Var ?
								new ParameterExpression(Modifier.Var, (Expression)Nodes[0].EmitStatement(mode)) :
								(Expression)Nodes[0].EmitStatement(mode), 
							_operator.OperatorName, 
							_operator.Operands[1].Modifier == Modifier.Var ?
								new ParameterExpression(Modifier.Var, (Expression)Nodes[1].EmitStatement(mode)) :
								(Expression)Nodes[1].EmitStatement(mode)
						);
				expression.Modifiers = Modifiers;
				return expression;
			}
			else
			{
				CallExpression expression = new CallExpression();
				expression.Identifier = Schema.Object.EnsureRooted(_operator.OperatorName);
				for (int index = 0; index < NodeCount; index++)
					if (_operator.Operands[index].Modifier == Modifier.Var)
						expression.Expressions.Add(new ParameterExpression(Modifier.Var, (Expression)Nodes[index].EmitStatement(mode)));
					else
						expression.Expressions.Add((Expression)Nodes[index].EmitStatement(mode));
				expression.Modifiers = Modifiers;
				return expression;
			}
		}
		
		#if USECLEANUPNODES
		protected PlanNodes FCleanupNodes;
		public PlanNodes CleanupNodes { get { return FCleanupNodes; } }
		#endif
		
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

			for (int index = 0; index < Operator.Operands.Count; index++)
			{
				IsLiteral = IsLiteral && Nodes[index].IsLiteral;
				IsFunctional = IsFunctional && Nodes[index].IsFunctional;
				IsDeterministic = IsDeterministic && Nodes[index].IsDeterministic;
				IsRepeatable = IsRepeatable && Nodes[index].IsRepeatable;
				IsNilable = IsNilable || Nodes[index].IsNilable;
			} 

			IsUniquePreserving = Convert.ToBoolean(MetaData.GetTag(Operator.MetaData, "DAE.IsUniquePreserving", IsUniquePreserving.ToString()));
			IsOrderPreserving = Convert.ToBoolean(MetaData.GetTag(Operator.MetaData, "DAE.IsOrderPreserving", IsOrderPreserving.ToString()));
			// Order preserving is strictly stronger than unique preserving, so set it explicitly here
			if (IsOrderPreserving)
			{
				IsUniquePreserving = true;
			}
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			if (Operator != null)
			{
				_dataType = Operator.ReturnDataType;
			
				for (int index = 0; index < Operator.Operands.Count; index++)
				{
					switch (Operator.Operands[index].Modifier)
					{
						case Modifier.In : break;
					
						case Modifier.Var :
							if (Nodes[index] is ParameterNode)
								Nodes[index] = Nodes[index].Nodes[0];
							
							if (!(Nodes[index] is StackReferenceNode) || plan.Symbols[((StackReferenceNode)Nodes[index]).Location].IsConstant)
								throw new CompilerException(CompilerException.Codes.ConstantObjectCannotBePassedByReference, plan.CurrentStatement(), Operator.Operands[index].Name);

							((StackReferenceNode)Nodes[index]).ByReference = true;
						break;

						case Modifier.Const :
							if (Nodes[index] is ParameterNode)
								Nodes[index] = Nodes[index].Nodes[0];
							
							// A const operand is the one modifier that can take a value belonging to something else, so it is
							// the one place that asks. The node answers whether it will lend, and CleanupOperand reads that
							// answer back off the node; asking rather than type-testing is what lets a node the test did not
							// know about - ExtractColumnNode, which LR.B compiles to - say so instead of lending unasked.
							Nodes[index].SetByReference();
						break;
					}
				}
			}
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			base.InternalBindingTraversal(plan, visitor);
			
			#if USECLEANUPNODES
			if (CleanupNodes != null)
				foreach (PlanNode node in CleanupNodes)
					node.BindingTraversal(APlan, visitor);
			#endif
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
		
		// TODO: This should be compiled and added as cleanup nodes
		// TODO: Handle var arguments for more than just stack references
		protected void CleanupOperand(Program program, Schema.SignatureElement signatureElement, PlanNode argumentNode, object argumentValue)
		{
			switch (signatureElement.Modifier)
			{
				case Modifier.In :
					// Released unconditionally, and the invariant that makes that safe is the whole load-bearing claim of
					// bug-an-operator-argument-is-released-by-the-wrong-owner: a node lends its value only when it was
					// asked to (PlanNode.SetByReference), and only the const branch of DetermineDataType asks. So a node
					// that could have handed over a stack slot's value, or a column of a row in one, copied instead, and
					// the value here is the callee's own. CallNode hands each operand slot's current value back in
					// argumentValue first, so a body that assigned to this operand released the displaced value itself
					// (Stack.PokeOwned) and this releases the one that replaced it - never one stream twice.
					// NOTE: that invariant is held by inspection, not by the type system. StackReferenceNode.ByReference is a
					// public field, and sites outside the const branch still set it directly: the update and delete targets in
					// Compiler.cs, which are not instruction operands at all; ExtractColumnNode, for its own source; and the
					// by-reference nodes built for a generated equality (Compiler.cs, InNodes.cs, TableNode.cs) or for an event
					// handler's arguments (Compiler.cs), each of which reaches a const or var operand - iEqual declares const,
					// and an event signature is only ever Var or Const. So nothing lands here today. If a new site ever builds
					// a by-reference StackReferenceNode for an in operand, this release takes a stack slot's own value: make
					// that site go through SetByReference, or gate this branch on ReturnsByReference as the const branch is.
					DataValue.DisposeValue(program.ValueManager, argumentValue);
				break;
					
				case Modifier.Var :
					if (argumentNode.DataType is Schema.ScalarType)
						argumentValue = ValueUtility.ValidateValue(program, (Schema.ScalarType)argumentNode.DataType, argumentValue, _operator);
					// NOTE: a plain Poke, not Stack.PokeOwned. This is an ownership return, not a replacement: a var
					// operand is passed by reference, so the callee's slot is an alias of the slot being written here.
					// Either the callee never assigned, and this stores the value the slot already holds, or it did,
					// and the value the slot holds was released by that assignment (Stack.PokeOwned) - releasing it
					// again here would be a second release of one stream. CallNode's write-back is what makes
					// argumentValue the slot's current value on both paths.
					program.Stack.Poke(((StackReferenceNode)argumentNode).Location, argumentValue);
				break;

				case Modifier.Const :
					// A const operand is not copied for the call either, and whether the call owns its value is a property
					// of the node that produced it rather than of the modifier: a stack reference, or a column of a row in
					// a slot, lends what it already holds, while a literal, a conversion or another call produces a value
					// nothing else holds. Releasing both leaves a released value in a caller's variable; releasing neither
					// holds a stream for the life of the session on every call, which is most calls - the compiler promotes
					// a by-value operand the body never assigns to to const, and every comparison, conversion, selector and
					// property accessor the engine generates for a scalar type declares const outright.
					if (!argumentNode.ReturnsByReference)
						DataValue.DisposeValue(program.ValueManager, argumentValue);
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
		public abstract object NilaryInternalExecute(Program program);

		public override object InternalExecute(Program program)
		{
			#if USECLEANUPNODES
			try
			{
			#endif
				
				if (IsBreakable)
				{
				    program.Stack.PushWindow(0, this, Operator.Locator);
				    try
				    {
				        return NilaryInternalExecute(program);
				    }
				    finally
				    {
				        program.Stack.PopWindow();
				    }
				}
				else
				{
					return NilaryInternalExecute(program);
				}

			#if USECLEANUPNODES
			}
			finally
			{
				if (FCleanupNodes != null)
					for (int index = 0; index < FCleanupNodes.Count; index++)
						FCleanupNodes[index].Execute(AProgram);
			}
			#endif
		}
	}
	
	public abstract class UnaryInstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(Program program, object argument1);
		
		public override object InternalExecute(Program program)
		{
			object argument = Nodes[0].Execute(program);
			try
			{
				if (IsBreakable)
				{
				    program.Stack.PushWindow(0, this, Operator.Locator);
				    try
				    {
				        return InternalExecute(program, argument);
				    }
				    finally
				    {
				        program.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(program, argument);
				}
			}
			finally
			{
				if (Operator != null)
					CleanupOperand(program, Operator.Signature[0], Nodes[0], argument);
				
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int index = 0; index < FCleanupNodes.Count; index++)
						FCleanupNodes[index].Execute(AProgram);
				#endif
			}
		}
	}
	
	public abstract class BinaryInstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(Program program, object argument1, object argument2);
		
		public override object InternalExecute(Program program)
		{
			object argument1 = Nodes[0].Execute(program);
			object argument2 = Nodes[1].Execute(program);
			try
			{
				if (IsBreakable)
				{
				    program.Stack.PushWindow(0, this, Operator.Locator);
				    try
				    {
				        return InternalExecute(program, argument1, argument2);
				    }
				    finally
				    {
				        program.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(program, argument1, argument2);
				}
			}
			finally
			{
				if (Operator != null) 
				{
					CleanupOperand(program, Operator.Signature[0], Nodes[0], argument1);
					CleanupOperand(program, Operator.Signature[1], Nodes[1], argument2);
				}
					
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int index = 0; index < FCleanupNodes.Count; index++)
						FCleanupNodes[index].Execute(AProgram);
				#endif
			}
		}
	}
	
	public abstract class TernaryInstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(Program program, object argument1, object argument2, object argument3);
		
		public override object InternalExecute(Program program)
		{
			object argument1 = Nodes[0].Execute(program);
			object argument2 = Nodes[1].Execute(program);
			object argument3 = Nodes[2].Execute(program);
			try
			{
				if (IsBreakable)
				{
				    program.Stack.PushWindow(0, this, Operator.Locator);
				    try
				    {
				        return InternalExecute(program, argument1, argument2, argument3);
				    }
				    finally
				    {
				        program.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(program, argument1, argument2, argument3);
				}
			}
			finally
			{
				if (Operator != null) 
				{
					CleanupOperand(program, Operator.Signature[0], Nodes[0], argument1);
					CleanupOperand(program, Operator.Signature[1], Nodes[1], argument2);
					CleanupOperand(program, Operator.Signature[2], Nodes[2], argument3);
				}
					
				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int index = 0; index < FCleanupNodes.Count; index++)
						FCleanupNodes[index].Execute(AProgram);
				#endif
			}
		}
	}
	
	public abstract class InstructionNode : InstructionNodeBase
	{
		public abstract object InternalExecute(Program program, object[] arguments);

		public override object InternalExecute(Program program)
		{
			object[] arguments = new object[NodeCount];
			for (int index = 0; index < arguments.Length; index++)
				arguments[index] = Nodes[index].Execute(program);
			try
			{
				if (IsBreakable)
				{
				    program.Stack.PushWindow(0, this, Operator.Locator);
				    try
				    {
				        return InternalExecute(program, arguments);
				    }
				    finally
				    {
				        program.Stack.PopWindow();
				    }
				}
				else
				{
					return InternalExecute(program, arguments);
				}
			}
			finally
			{
				// TODO: Compile the dispose calls and var argument assignment calls
				if (Operator != null)
					for (int index = 0; index < Operator.Operands.Count; index++)
						CleanupOperand(program, Operator.Signature[index], Nodes[index], arguments[index]);

				#if USECLEANUPNODES
				if (FCleanupNodes != null)
					for (int index = 0; index < FCleanupNodes.Count; index++)
						FCleanupNodes[index].Execute(AProgram);
				#endif
			}
		}
	}
}

