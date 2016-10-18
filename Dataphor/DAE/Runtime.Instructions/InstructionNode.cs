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
using System.Linq;
using System.Collections.Generic;
using Sigil;

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
			
			IsOrderPreserving = Convert.ToBoolean(MetaData.GetTag(Operator.MetaData, "DAE.IsOrderPreserving", IsOrderPreserving.ToString()));
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
							
							if (Nodes[index] is StackReferenceNode)
								((StackReferenceNode)Nodes[index]).ByReference = true;
							else if (Nodes[index] is StackColumnReferenceNode)
								((StackColumnReferenceNode)Nodes[index]).ByReference = true;
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
		// TODO: Native emission
		protected void CleanupOperand(Program program, Schema.SignatureElement signatureElement, PlanNode argumentNode, object argumentValue)
		{
			switch (signatureElement.Modifier)
			{
				case Modifier.In :
					DataValue.DisposeValue(program.ValueManager, argumentValue);
				break;
					
				case Modifier.Var :
					if (argumentNode.DataType is Schema.ScalarType)
						argumentValue = ValueUtility.ValidateValue(program, (Schema.ScalarType)argumentNode.DataType, argumentValue, _operator);
					program.Stack.Poke(((StackReferenceNode)argumentNode).Location, argumentValue);
				break;
			}
		}

        public virtual ArgumentEmissionStyle ArgumentEmissionStyle
        {
            get { return ArgumentEmissionStyle.NativeOnStack; }
        }

		public override bool CanEmitIL => FindStaticExecuteMethod() != null;

		public override bool RequiresEmptyStack 
        { 
            get { return ShouldCleanupOperands(); } 
        }

        public override void InternalEmitIL(NativeMethod m)
		{
			Sigil.Label nullLabel = null;

			var cleanupOperands = ShouldCleanupOperands();
			ExceptionBlock cleanupBlock = null;
			if (cleanupOperands)
				cleanupBlock = m.IL.BeginExceptionBlock();
			// TODO: Capture only arguments that need cleanup

			/* 
				Strategies:
				1. PhysicalInLocals - Capture each agrument in physical form into locals and pass locals.
				2. NativeInLocals - Convert each argument into native form, capture into locals, and pass locals.
				3. NativeOnStack - Convert each argument into native form, and leave on stack.
			*/

			var physicalArguments = cleanupOperands || ArgumentEmissionStyle == ArgumentEmissionStyle.PhysicalInLocals || IsNilable
				? new Local[Nodes.Count]
				: null;
			var nativeArguments = ArgumentEmissionStyle == ArgumentEmissionStyle.NativeInLocals || IsNilable
				? new Local[Nodes.Count]
				: null;

			for (var i = 0; i < Nodes.Count; ++i)
			{
				var node = Nodes[i];

				EmitArgument(m, i);

				if (physicalArguments != null)
					physicalArguments[i] = m.StoreLocal(node.PhysicalType);

				if (ArgumentEmissionStyle != ArgumentEmissionStyle.PhysicalInLocals && !ShouldIgnoreArgument(node))
				{
					if (physicalArguments != null)
						m.IL.LoadLocal(physicalArguments[i]);

					// Perform nil check                
					if (node.IsNilable && IsNilable)
					{
						if (nullLabel == null)
							nullLabel = m.IL.DefineLabel();
						m.IL.BranchIfFalse(nullLabel);
						m.IL.LoadLocal(physicalArguments[i]);
					}

					m.PhysicalToNative(node);

					if (nativeArguments != null)
					{
						nativeArguments[i] = m.StoreLocal(node.NativeType);

						if (ArgumentEmissionStyle == ArgumentEmissionStyle.NativeOnStack && !IsNilable)
						{
							m.IL.LoadLocal(nativeArguments[i]);
							EmitPostArgument(m, i);
						}
					}
					else
						EmitPostArgument(m, i);
				}
			}

			if (ArgumentEmissionStyle == ArgumentEmissionStyle.NativeOnStack && IsNilable)
				for (var i = 0; i < Nodes.Count; ++i)
					if (nativeArguments[i] != null)
					{
						m.IL.LoadLocal(nativeArguments[i]);
						EmitPostArgument(m, i);
					}

			EmitInstruction(m, () => EmitInstructionOperation(m,
				ArgumentEmissionStyle == ArgumentEmissionStyle.PhysicalInLocals ? physicalArguments
					: ArgumentEmissionStyle == ArgumentEmissionStyle.NativeInLocals ? nativeArguments
					: null
			));

			EmitPostInstruction(m);

			if (nullLabel != null)
			{
				var endLabel = m.IL.DefineLabel();
				m.IL.Branch(endLabel);
				m.IL.MarkLabel(nullLabel);
				m.IL.LoadNull();
				m.IL.MarkLabel(endLabel);
				m.IL.Nop();
			}

			if (cleanupOperands)
			{
				var result = m.StoreLocal(PhysicalType);
				var cleanupFinally = m.IL.BeginFinallyBlock(cleanupBlock);
				for (var i = 0; i < physicalArguments.Length; ++i)
					EmitCleanupOperand(m, physicalArguments[i], i);
				m.IL.EndFinallyBlock(cleanupFinally);
				m.IL.EndExceptionBlock(cleanupBlock);
				m.IL.LoadLocal(result);
				result.Dispose();
			}

			// Free up locals to be reused
			DisposeArguments(physicalArguments);
			DisposeArguments(nativeArguments);
		}

		protected virtual bool ShouldIgnoreArgument(PlanNode node)
		{
			return false;
		}

		private static void DisposeArguments(Local[] arguments)
		{
			if (arguments != null)
				foreach (var argument in arguments)
					argument?.Dispose();
		}

		protected virtual void EmitInstruction(NativeMethod m, Action emit)
		{
			emit();
		}

		/// <summary> Overridden to emit the instruction specific operator for the arguments already on the stack. </summary>
		/// <param name="arguments"> Local variables for each argument.  Arguments will only be present ArgumentEmissionStyle is NativeInLocals or PhysicalInLocals. </param>
		protected virtual void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			var staticExecute = FindStaticExecuteMethod();
			if (staticExecute != null)
				m.IL.Call(staticExecute);
			else
				throw new NotImplementedException(GetType().Name + ".EmitInstructionOperation is not implemented.");
		}

		private MethodInfo FindStaticExecuteMethod()
		{
			return GetType().GetMethod("StaticExecute", GetExecuteSignature().ToArray());
		}

		protected virtual IEnumerable<Type> GetExecuteSignature() 
			=> Nodes.Where(n => !ShouldIgnoreArgument(n)).Select(n => n.NativeType);

		protected virtual void EmitPostInstruction(NativeMethod m)
        {
			var staticExecute = FindStaticExecuteMethod();
			if (staticExecute == null || staticExecute.ReturnType != typeof(object))
				m.NativeToPhysical(this);
        }

        protected virtual void EmitArgument(NativeMethod m, int index)
        {
            EmitSubNodeN(index, m);
        }

		protected virtual void EmitPostArgument(NativeMethod m, int index)
		{
		}

		protected void EmitCleanupOperand(NativeMethod m, Local argument, int paramIndex)
		{
            m.LoadStatic(this);
			m.IL.CastClass(typeof(InstructionNodeBase));
            m.LoadProgram();
			m.IL.CallVirtual(typeof(InstructionNodeBase).GetMethod("get_Operator"));
			m.IL.CallVirtual(typeof(Schema.Operator).GetMethod("get_Signature"));
			m.IL.LoadConstant(paramIndex);
			m.IL.CallVirtual(typeof(Schema.Signature).GetMethod("get_Item"));
            m.LoadStatic(this);
            m.NodesN(paramIndex);
			m.IL.LoadLocal(argument);
			m.PhysicalToObject(Nodes[paramIndex]);
			m.IL.CallVirtual(typeof(InstructionNodeBase).GetMethod("CleanupOperand", new[] { typeof(Program), typeof(Schema.SignatureElement), typeof(PlanNode), typeof(object) }));
		}

		protected void EmitValidatedCopy(NativeMethod m)
		{
			var nullLabel = m.IL.DefineLabel();
			var endLabel = m.IL.DefineLabel();

			EmitSubNodeN(0, m, true);
			var valueLocal = m.StoreLocal(typeof(object));
			m.IL.LoadLocal(valueLocal);
			m.IL.BranchIfFalse(nullLabel);

			EmitValidatedAssignment(m, () => m.CopyValue(() => m.IL.LoadLocal(valueLocal)), 0);
			m.IL.Branch(endLabel);

			m.IL.MarkLabel(nullLabel);
			EmitValidatedAssignment(m, () => m.IL.LoadNull(), 0);

			m.IL.MarkLabel(endLabel);
			m.IL.Nop();
		}

		protected void EmitCallStaticExecute(NativeMethod m)
		{
			m.IL.Call(FindStaticExecuteMethod());
		}

		protected void EmitMinMax(NativeMethod m, Local[] arguments, Action emitComparer)
		{
			System.Diagnostics.Debug.Assert(ArgumentEmissionStyle == ArgumentEmissionStyle.PhysicalInLocals);

			var nullLabel = m.IL.DefineLabel();
			var valueLabel = m.IL.DefineLabel();
			var lessLabel = m.IL.DefineLabel();
			var rightDecides = m.IL.DefineLabel();
			var endLabel = m.IL.DefineLabel();

			m.IL.LoadLocal(arguments[0]);
			if (Nodes[0].IsNilable)
			{
				m.IL.BranchIfFalse(rightDecides);
				m.IL.LoadLocal(arguments[0]);
				m.IL.UnboxAny(Nodes[0].NativeType);
			}

			m.IL.LoadLocal(arguments[1]);
			if (Nodes[1].IsNilable)
			{
				m.IL.BranchIfFalse(valueLabel);	// left value already on stack
				m.IL.LoadLocal(arguments[1]);
				m.IL.UnboxAny(Nodes[1].NativeType);
			}

			emitComparer();
			m.IL.BranchIfFalse(lessLabel);

			m.IL.LoadLocal(arguments[0]);
			if (Nodes[0].IsNilable)
				m.IL.UnboxAny(Nodes[0].NativeType);
			m.IL.Branch(valueLabel);

			m.IL.MarkLabel(lessLabel);
			m.IL.LoadLocal(arguments[1]);
			if (Nodes[1].IsNilable)
				m.IL.UnboxAny(Nodes[1].NativeType);
			m.IL.Branch(valueLabel);

			m.IL.MarkLabel(rightDecides);
			m.IL.LoadLocal(arguments[1]);
			if (Nodes[1].IsNilable)
			{
				m.IL.BranchIfFalse(nullLabel);
				m.IL.LoadLocal(arguments[1]);
				m.IL.UnboxAny(Nodes[1].NativeType);
			}

			if (IsNilable)
			{
				m.IL.Branch(valueLabel);
				m.IL.MarkLabel(nullLabel);
				m.IL.LoadNull();
				m.IL.Branch(endLabel);
			}

			m.IL.MarkLabel(valueLabel);
			m.NativeToPhysical(this);

			m.IL.MarkLabel(endLabel);
			m.IL.Nop();
		}

		protected void EmitBetween(NativeMethod m, Local[] arguments, Action emitLTE, Action emitGTE)
		{
			System.Diagnostics.Debug.Assert(ArgumentEmissionStyle == ArgumentEmissionStyle.NativeInLocals);

			// tempValue >= lowerBound
			m.IL.LoadLocal(arguments[0]);
			m.IL.LoadLocal(arguments[1]);
			emitGTE();

			// tempValue <= upperBound
			m.IL.LoadLocal(arguments[0]);
			m.IL.LoadLocal(arguments[2]);
			emitLTE();

			m.IL.And();
		}

		protected void EmitIntBetween(NativeMethod m, Local[] arguments)
		{
			EmitBetween(m, arguments,
				() =>
				{
					m.IL.CompareGreaterThan();
					m.Not();
				},
				() =>
				{
					m.IL.CompareLessThan();
					m.Not();
				}
			);
		}

		protected void EmitCompareBetween(NativeMethod m, Local[] arguments, Type type)
		{
			EmitBetween(m, arguments,
				() =>
				{
					m.IL.Call(type.GetMethod("CompareTo", new[] { type }));
					m.IL.LoadConstant(0);
					m.IL.CompareLessThan();
					m.Not();
				},
				() =>
				{
					m.IL.Call(type.GetMethod("CompareTo", new[] { type }));
					m.IL.LoadConstant(0);
					m.IL.CompareGreaterThan();
					m.Not();
				}
			);
		}

		protected bool ShouldCleanupOperands()
		{
			if (Operator == null)
				return false;

			for (var i = 0; i < Operator.Signature.Count; ++i)
			{
				var nativeType = (Operator.Signature[i].DataType as Schema.IScalarType)?.NativeType ?? null;

				var modifier = Operator.Signature[i].Modifier;
				if (
					(
						modifier == Modifier.In && (nativeType == null || typeof(IDisposable).IsAssignableFrom(nativeType))
					) || modifier == Modifier.Var
				)
					return true;
			}
			return false;
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

		public override bool RequiresEmptyStack { get { return base.RequiresEmptyStack || IsBreakable; } }

		protected override void EmitInstruction(NativeMethod m, Action emit)
		{
			ExceptionBlock windowBlock = null;
			if (IsBreakable)
			{
				m.GetStack();
				m.IL.LoadConstant(0);
				m.IL.CallVirtual(typeof(Stack).GetMethod("PushWindow", new[] { typeof(int) }));
				windowBlock = m.IL.BeginExceptionBlock();
			}
			base.EmitInstruction(m, emit);
			if (IsBreakable)
			{
				var resultLocal = m.StoreLocal(PhysicalType);
				var windowFinally = m.IL.BeginFinallyBlock(windowBlock);
				m.GetStack();
				m.IL.CallVirtual(typeof(Stack).GetMethod("PopWindow", new Type[0]));
				m.IL.EndFinallyBlock(windowFinally);
				m.IL.EndExceptionBlock(windowBlock);
				m.IL.LoadLocal(resultLocal);
				resultLocal.Dispose();
			}
		}
	}
}

