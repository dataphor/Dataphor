using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Alphora.Dataphor.DAE
{
	public class NativeMethod
	{
        private System.Reflection.Emit.TypeBuilder _typeBuilder;
        private Emit<Func<IList<object>, Program, object>> _emitter;
        /// <summary> Static arguments are object references given at compile time that are referenced by runtime code. </summary>
        private List<object> _staticArguments = new List<object>();
        private Dictionary<object, int> _staticsByInstance = new Dictionary<object, int>();
		
		public Emit<Func<IList<object>, Program, object>> IL { get { return _emitter; } }
        public string Name { get; private set; }

		internal NativeMethod(System.Reflection.Emit.TypeBuilder typeBuilder, string name)
		{
            _typeBuilder = typeBuilder;
            Name = name;
            #if IL_USE_METHOD_BUILDER
            _emitter = Emit<Func<PlanNode, Program, object>>.BuildStaticMethod(typeBuilder, name, MethodAttributes.Public | MethodAttributes.Static, false,
                #if (DEBUG && VERIFY_IL)
                doVerify: true
                #else
				doVerify: false
                #endif
            );
            #else
            _emitter = Emit<Func<IList<object>, Program, object>>.NewDynamicMethod(name,
            	#if (DEBUG && VERIFY_IL)
            	doVerify: true,
            	strictBranchVerification: true
            	#else
            	doVerify: false,
            	strictBranchVerification: false
            	#endif
            );
            #endif
        }

        /// <summary> Load the given object on the stack. </summary>
        /// <remarks> The given object must be available at runtime. </remarks>
        public void LoadStatic(object value)
        {
            int index;
            if (!_staticsByInstance.TryGetValue(value, out index))
            {
                index = _staticArguments.Count;
                _staticArguments.Add(value);
                _staticsByInstance.Add(value, index);
            }
            IL.LoadArgument(0);
            IL.LoadConstant(index);
            var type = value.GetType();
            IL.CallVirtual(typeof(IList<object>).GetMethod("get_Item", new[] { typeof(int) }));
            IL.CastClass(type);
        }

		public Func<Program, object> CreateDelegate()
		{
            #if IL_USE_METHOD_BUILDER
            var method = IL.CreateMethod(OptimizationOptions.None);
			var newType = _typeBuilder.CreateType();
			var result = Delegate.CreateDelegate(typeof(Func<IList<object>, Program, object>), newType.GetMethod(m.Name)) as Func<PlanNode, Program, object>;
            #else
            var result = IL.CreateDelegate(OptimizationOptions.None);
            #endif

            return p => result(_staticArguments, p);
		}
		
        public void NativeToPhysical(PlanNode node)
		{
			var nativeType = node.NativeType;
			if (nativeType.IsValueType)
			{
				if (node.IsNilable)
					IL.Box(nativeType);
			} else if (nativeType != typeof(object))
				IL.CastClass(nativeType);
		}

		public void PhysicalToNative(PlanNode node, Label nullLabel, bool leave = false)
		{
			if (node.IsNilable)
			{
				var valueLocal = StoreLocal(node.PhysicalType);
				IL.LoadLocal(valueLocal);
				BranchOrLeaveIfFalse(nullLabel, leave);
				IL.LoadLocal(valueLocal);
				PhysicalToNative(node);
				valueLocal.Dispose();
			}
		}

		public void PhysicalToNative(PlanNode node)
		{
			var nativeType = node.NativeType;
			if (node.IsNilable && nativeType.IsValueType)
				IL.UnboxAny(nativeType);
		}

		public void ObjectToPhysical(PlanNode node)
		{
			var nativeType = node.NativeType;
			if (nativeType.IsValueType)
			{
				if (!node.IsNilable)
					IL.UnboxAny(nativeType);
			}
			else if (nativeType != typeof(object))
				IL.CastClass(nativeType);
		}

		public void PhysicalToObject(PlanNode node)
		{
			var nativeType = node.NativeType;
			if (nativeType.IsValueType)
			{
				if (!node.IsNilable)
					IL.Box(nativeType);
			}
		}

		public void NativeToObject(PlanNode node)
		{
			var nativeType = node.NativeType;
			if (nativeType.IsValueType)
				IL.Box(nativeType);
			else if (nativeType != typeof(object))
				IL.CastClass(nativeType);
		}

		public void BranchOrLeave(Sigil.Label endLabel, bool leave = false)
		{
			if (leave)
				IL.Leave(endLabel);
			else
				IL.Branch(endLabel);
		}

		public void BranchOnNilOrFalse(PlanNode node, Label branchLabel, bool leave = false)
		{
			System.Diagnostics.Debug.Assert(node.NativeType == typeof(bool));

			if (node.IsNilable)
			{
				var nilableLocal = StoreLocal(typeof(object));
				IL.LoadLocal(nilableLocal);
				BranchOrLeaveIfFalse(branchLabel, leave);
				IL.LoadLocal(nilableLocal);
				PhysicalToNative(node, branchLabel, leave);
			}
			BranchOrLeaveIfFalse(branchLabel, leave);
		}

		private void BranchOrLeaveIfFalse(Label branchLabel, bool leave)
		{
			if (leave)
			{
				var skipLabel = IL.DefineLabel();
				IL.BranchIfTrue(skipLabel);
				IL.Leave(branchLabel);
				IL.MarkLabel(skipLabel);
				IL.Nop();
			}
			else
				IL.BranchIfFalse(branchLabel);
		}

		public void CopyValue(Action emitValue)
		{
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_ValueManager", new Type[0]));
			emitValue();
			IL.Call(typeof(DataValue).GetMethod("CopyValue", new[] { typeof(IValueManager), typeof(object) }));
		}

		public void DisposeValue(Action emitValue)
		{
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_ValueManager", new Type[0]));
			emitValue();
			IL.Call(typeof(DataValue).GetMethod("DisposeValue", new[] { typeof(IValueManager), typeof(object) }));
		}

		public Local StoreLocal(Type type)
		{
			var valueLocal = IL.DeclareLocal(type, initializeReused: false);
			IL.StoreLocal(valueLocal);
			return valueLocal;
		}

		public void GetStack()
		{
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_Stack", new Type[0]));
		}

		public void StackCount()
		{
			GetStack();
			IL.CallVirtual(typeof(Stack).GetMethod("get_Count", new Type[0]));
		}

		public void GetStackItem(int location)
		{
			GetStackItem(() => IL.LoadConstant(location));
		}

		public void GetStackItem(Action emitLocation)
		{
			GetStack();
			emitLocation();
			IL.CallVirtual(typeof(Stack).GetMethod("get_Item", new[] { typeof(int) }));
		}

		public void SetStackItem(int location, Action emitValue)
		{
			SetStackItem(() => IL.LoadConstant(location), emitValue);
		}

		public void SetStackItem(Action emitLocation, Action emitValue)
		{
			GetStack();
			emitLocation();
			emitValue();
			IL.CallVirtual(typeof(Stack).GetMethod("set_Item", new[] { typeof(int), typeof(object) }));
		}

		public void FrameAround(Action callback)
		{
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_Stack", new Type[0]));
			IL.CallVirtual(typeof(Stack).GetMethod("PushFrame", new Type[0]));

			var tryBlock = IL.BeginExceptionBlock();

			callback();

			var finallyBlock = IL.BeginFinallyBlock(tryBlock);
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_Stack", new Type[0]));
			IL.CallVirtual(typeof(Stack).GetMethod("PopFrame", new Type[0]));
			IL.EndFinallyBlock(finallyBlock);

			IL.EndExceptionBlock(tryBlock);
		}

		public void NodesN(int n)
		{
			IL.Call(typeof(PlanNode).GetMethod("get_Nodes", new Type[0]));
			IL.LoadConstant(n);
			IL.CallVirtual(typeof(PlanNodes).GetMethod("get_Item", new[] { typeof(int) }));
		}

		public void NodeExecute()
		{
			IL.LoadField(typeof(PlanNode).GetField("Execute"));
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Func<Program, object>).GetMethod("Invoke", new[] { typeof(Program) }));
		}

		public void PushStack(Action emitArgument)
		{
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_Stack", new Type[0]));
			emitArgument();
			IL.CallVirtual(typeof(Stack).GetMethod("Push", new[] { typeof(object) }));
		}

		public void PopStack()
		{
			IL.LoadArgument(1);
			IL.CallVirtual(typeof(Program).GetMethod("get_Stack", new Type[0]));
			IL.CallVirtual(typeof(Stack).GetMethod("Pop", new Type[0]));
			IL.Pop();	// throw out result
		}

		public void AbortCheck()
		{
			// TODO: Only check for aborted in loops and such...
			IL.LoadArgument(1);    // program
			IL.CallVirtual(typeof(Program).GetMethod("CheckAborted", new Type[0]));
		}

		public void Node(PlanNode planNode, bool forceObject = false, bool forceCall = false)
		{
			if (planNode.CanEmitIL && !forceCall)
			{
				planNode.EmitIL(this);

				if (forceObject)
					PhysicalToObject(planNode);
			}
			else
			{
                LoadStatic(planNode);
				NodeExecute();

				if (!forceObject)
					ObjectToPhysical(planNode);

				planNode.Prepare();
			}
		}

        /// <summary> Emit conversion from the physical type of a sub-node to the parent node's physical type. </summary>
        public void PhysicalToPhysical(PlanNode source, PlanNode target)
        {
            var sourcePhysical = source.PhysicalType;
            if (sourcePhysical.IsValueType && !target.PhysicalType.IsValueType)
                IL.Box(sourcePhysical);
        }

		public void Not()
		{
			IL.LoadConstant(0);
			IL.CompareEqual();
		}

		public void NativeToAddress(Type t)
		{
			using (var addressLocal = StoreLocal(t))
				IL.LoadLocalAddress(addressLocal);
		}

		public void LoadProgram()
		{
			IL.LoadArgument(1);
		}

		public void EmitCheckArguments(InstructionNodeBase node, Local[] arguments, Sigil.Label nilLabel)
		{
			System.Diagnostics.Debug.Assert(node.ArgumentEmissionStyle == ArgumentEmissionStyle.PhysicalInLocals);

			for (int i = 0; i < node.Nodes.Count; ++i)
			{
				if (node.Nodes[i].IsNilable && arguments[i] != null)
				{
					IL.LoadLocal(arguments[i]);
					IL.BranchIfFalse(nilLabel);
				}
			}
		}
	}
}
