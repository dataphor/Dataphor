/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define WRAPRUNTIMEEXCEPTIONS // Determines whether or not runtime exceptions are wrapped
//#define TRACKCALLDEPTH // Determines whether or not call depth tracking is enabled

using System;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	/*
		DAE Processor Calling Convention ->
		
			The calling convention is distributed among several diferrent nodes, PlanNode, InstructionNode, CallNode, AggregateCallNode, and TableNode.
			
			PlanNode is the base class for all nodes in the processor, and introduces the methods used in the execution.
				
				object Execute(ServerProcess AProcess)
					This method passes execution to the device if it is supported.
					Otherwise, it passes execution to the InternalExecute(ServerProcess) method.
				
				abstract object InternalExecute(ServerProcess AProcess)
				
				virtual object InternalExecute(ServerProcess AProcess, object[] AArguments)
					This method raises on PlanNode, and is overridden by Instructions to perform their operations.
				
			InstructionNode is the base class for all instructions in the processor, and performs argument evaluation and virtual resolution.
			
				override object InternalExecute(Program AProgram)
					Builds the arguments array based on the parameters in Operator, and the child nodes of the node.
					Performs virtual resolution, if necessary
					Calls InternalExecute(ServerProcess, object[])
					Cleans up the arguments list
					
			CallNode is the class used to invoke D4 compiled operators, and prepares the stack prior to the call, and cleans it up afterwards.
			
				override object InternalExecute(Program AProgram, object[] AArguments)
					Pushes the arguments onto the stack, and performs a Call on the stack
					Calls Execute on the compiled BlockNode for Operator
					Pops the arguments off the stack and performs a return, pushing the result if necessary
					
			AggregateCallNode is used to invoke D4 aggregate operators.
			
			TableNode is the base class for all host implemented table instructions.
					
	*/
	
	public delegate object ExecuteDelegate(Program AProgram);
        
	/// <summary> PlanNode </summary>	
	public abstract class PlanNode : System.Object
	{
		public PlanNode() : base()
		{
 		}
		
        // IsLiteral
        protected bool _isLiteral = true;
        public bool IsLiteral
        {
			get { return _isLiteral; }
			set { _isLiteral = value; }
        }
        
        // IsFunctional
        protected bool _isFunctional = true;
        public bool IsFunctional
        {
			get { return _isFunctional; }
			set { _isFunctional = value; }
        }
        
        // IsDeterministic
        protected bool _isDeterministic = true;
        public bool IsDeterministic
        {
			get { return _isDeterministic; }
			set { _isDeterministic = value; }
        }
        
        // IsRepeatable
        protected bool _isRepeatable = true;
        public bool IsRepeatable
        {
			get { return _isRepeatable; }
			set { _isRepeatable = value; }
        }

        // IsNilable
        protected bool _isNilable = false;
        public bool IsNilable
        {
			get { return _isNilable; }
			set { _isNilable = value; }
		}
        
		public static string CharacteristicsToString(PlanNode node)
		{
			StringBuilder stringValue = new StringBuilder();
			stringValue.Append(node.IsLiteral ? Strings.Get("Characteristics.Literal") : Strings.Get("Characteristics.NonLiteral"));
			stringValue.AppendFormat(", {0}", node.IsFunctional ? Strings.Get("Characteristics.Functional") : Strings.Get("Characteristics.NonFunctional"));
			stringValue.AppendFormat(", {0}", node.IsDeterministic ? Strings.Get("Characteristics.Deterministic") : Strings.Get("Characteristics.NonDeterministic"));
			stringValue.AppendFormat(", {0}", node.IsRepeatable ? Strings.Get("Characteristics.Repeatable") : Strings.Get("Characteristics.NonRepeatable"));
			stringValue.AppendFormat(", {0}", node.IsNilable ? Strings.Get("Characteristics.Nilable") : Strings.Get("Characteristics.NonNilable"));
			return stringValue.ToString();
		}

        // IsOrderPreserving (see the sargability discussion in RestrictNode.cs for a description of this characteristic)
        protected bool _isOrderPreserving = false;
        public bool IsOrderPreserving
        {
			get { return _isOrderPreserving; }
			set { _isOrderPreserving = value; }
		}
		
		// IsContextLiteral (see the sargability discussion in RestrictNode.cs for a description of this characteristic)
		public virtual bool IsContextLiteral(int location)
		{
			if (_nodes != null)
				for (int index = 0; index < Nodes.Count; index++)
					if (!Nodes[index].IsContextLiteral(location))
						return false;
			return true;
		}
		
		// Modifiers
		protected LanguageModifiers _modifiers;
		public LanguageModifiers Modifiers
		{
			get { return _modifiers; }
			set { _modifiers = value; }
		}
        
        // Device
		[Reference]
        protected Schema.Device _device;
        public Schema.Device Device
        {
			get { return _device; }
			set { _device = value; }
        }
        
        // DeviceNode
        protected DevicePlanNode _deviceNode;
        public DevicePlanNode DeviceNode
        {
			get { return _deviceNode; }
			set { _deviceNode = value; }
		}
        
        // NoDevice
        protected bool _noDevice;
        public bool NoDevice
        {
			get { return _noDevice; }
			set { _noDevice = value; }
        }
        
        // DeviceSupported
        protected bool _deviceSupported;
        public bool DeviceSupported
        {
			get { return _deviceSupported; }
			set { _deviceSupported = value; }
        }
        
        // DeviceMessages
        // Set by the device during the prepare phase.
        // Will be null if this node has not been prepared, so test all access to this reference.
        protected Schema.TranslationMessages _deviceMessages;
        public Schema.TranslationMessages DeviceMessages { get { return _deviceMessages; } }
        
		public void ClearDeviceSubNodes()
		{
			if (_nodes != null)
			{
				for (int index = 0; index < _nodes.Count; index++)
				{
					_nodes[index].ClearDeviceNode();
				}
			}
		}

		public virtual void ClearDeviceNode()
		{
			this._deviceNode = null;
		}

		// DetermineDevice
		public virtual void DetermineDevice(Plan plan)
		{
			//	if child nodes are flagged with nodevice, or have more than one non-null device
			//		this node is flagged as nodevice
			//	otherwise
			//		this node uses the same device as its children
            Schema.Device childDevice = null;
            Schema.Device currentChildDevice = null;
            _noDevice = !ShouldSupport;
            
            if (_nodes != null)
				for (int index = 0; index < _nodes.Count; index++)
				{
					_noDevice = _noDevice || _nodes[index].NoDevice;
					if (!_noDevice)
					{
						childDevice = _nodes[index].Device;
						if (childDevice != null)
						{
							if (currentChildDevice == null)
								currentChildDevice = childDevice;
							else if (currentChildDevice != childDevice)
							{
								_noDevice = true;
								break;
							}
						}
					}
				}

            if (!_noDevice)
            {
				_device = currentChildDevice;
				if (_device != null)
				{							
					Schema.DevicePlan devicePlan = null;
					if (ShouldSupport)
					{
						plan.EnsureDeviceStarted(_device);
						devicePlan = _device.Prepare(plan, this);
					}
					
					if (devicePlan != null)
						_deviceMessages = devicePlan.TranslationMessages;

					if ((devicePlan != null) && devicePlan.IsSupported)
					{
						// If the plan could be supported via parameterization, it is not actually supported by the device
						// and setting the device supported to false ensures that if this node is actually executed, the
						// device will not be asked to perform a useless parameterization.
						_deviceSupported = !_couldSupport;
						plan.AddDevicePlan(devicePlan);

						// Remove device plan nodes from prepared sub nodes to reduce memory usage of prepared plans
						ClearDeviceSubNodes();
					}
					else
					{
						if (!_device.IgnoreUnsupported && (DataType != null) && !IgnoreUnsupported && !plan.InTypeOfContext)
						{
							if ((devicePlan != null) && !plan.SuppressWarnings)
								plan.Messages.Add(new CompilerException(CompilerException.Codes.UnsupportedPlan, CompilerErrorLevel.Warning, _device.Name, SafeEmitStatementAsString(), devicePlan.TranslationMessages.ToString()));
						}
						_deviceSupported = false;
						_noDevice = true;
					}
				}
				else
				{
					_deviceSupported = false;
					// BTR 4/28/2005 ->
					// Not sure why this is here, but it is preventing the translation of several otherwise translatable items.
					// I will turn it off and see what happens.
					//if (DataType is Schema.ITableType)
					//	FNoDevice = true;
				}
			}
			else
				_deviceSupported = false;
		}
		
		// IgnoreUnsupported -- only applies if DataType is not null
		private bool _ignoreUnsupported;
		public bool IgnoreUnsupported
		{
			get { return _ignoreUnsupported; }
			set { _ignoreUnsupported = value; }
		}
		
		private bool _couldSupport = false;
		/// <summary>Set by the device to indicate that the node could be supported if necessary, but only by parameterization.</summary>
		public bool CouldSupport
		{
			get { return _couldSupport; }
			set { _couldSupport = value; }
		}
		
		private bool _shouldSupport = true;
		public bool ShouldSupport
		{
			get { return _shouldSupport; }
			set { _shouldSupport = value; }
		}
		
		private bool _shouldEmitIL = false;
		public bool ShouldEmitIL
		{
			get { return _shouldEmitIL; }
			set { _shouldEmitIL = value; }
		}
		
		private bool _isBreakable = false;
		public bool IsBreakable
		{
			get { return _isBreakable; }
			set { _isBreakable = value; }
		}
		
		private LineInfo _lineInfo;
		public LineInfo LineInfo
		{
			get { return _lineInfo; }
			set { _lineInfo = value; }
		}
		
		public void SetLineInfo(Plan plan, LineInfo lineInfo)
		{
			if (lineInfo != null)
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				
				if (plan.CompilingOffset != null)
				{
					_lineInfo.Line = lineInfo.Line - plan.CompilingOffset.Line;
					_lineInfo.LinePos = lineInfo.LinePos - ((plan.CompilingOffset.Line == lineInfo.Line) ? plan.CompilingOffset.LinePos : 0);
					_lineInfo.EndLine = lineInfo.EndLine - plan.CompilingOffset.Line;
					_lineInfo.EndLinePos = lineInfo.EndLinePos - ((plan.CompilingOffset.Line == lineInfo.EndLine) ? plan.CompilingOffset.LinePos : 0);
				}
				else
				{
					_lineInfo.SetFromLineInfo(lineInfo);
				}
			}
		}

		public int Line 
		{ 
			get { return _lineInfo == null ? -1 : _lineInfo.Line; } 
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.Line = value;
			}
		}
		
		public int LinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.LinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.LinePos = value;
			}
		}
		
		public int EndLine
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLine; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLine = value;
			}
		}
		
		public int EndLinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLinePos = value;
			}
		}
		
        // Nodes
        private PlanNodes _nodes;
        public PlanNodes Nodes
        {
			get
			{
				if (_nodes == null)
					_nodes = new PlanNodes();
				return _nodes;
			}
		}
		
		public int NodeCount { get { return _nodes == null ? 0 : _nodes.Count; } }

		// IL Generation        
        public void EmitIL(Plan plan, bool parentEmitted) 
        {
			if (!_deviceSupported)
			{
				if (!parentEmitted)
					InternalEmitIL(plan);

				if (_nodes != null)				
					for (int index = 0; index < Nodes.Count; index++)
						Nodes[index].EmitIL(plan, _shouldEmitIL);
			}
        }

		protected static FieldInfo _nodesFieldInfo = typeof(PlanNode).GetField("Nodes");
		protected static FieldInfo _planNodesFieldInfo = typeof(PlanNodes).GetField("FNodes", BindingFlags.NonPublic | BindingFlags.Instance);        
        protected static FieldInfo _executeFieldInfo = typeof(PlanNode).GetField("Execute");
        protected static MethodInfo _executeMethodInfo = typeof(ExecuteDelegate).GetMethod("Invoke", new Type[] { typeof(ServerProcess) });
        protected static MethodInfo _planNodesIndexerInfo = typeof(PlanNodes).GetMethod("get_Item", new Type[] { typeof(int) });

		/// <summary>
		/// Prepares a dynamic method for use in generating the IL for a node.
		/// </summary>
		/// <returns>The DynamicMethod instance representing the Execute method being constructed.</returns>
		protected DynamicMethod BeginEmitIL()
		{
            #if (SILVERLIGHT)
            return new DynamicMethod(GetType().Name, typeof(object), new Type[] { typeof(PlanNode), typeof(ServerProcess) });
            #else
			return new DynamicMethod(GetType().Name, typeof(object), new Type[] { typeof(PlanNode), typeof(ServerProcess) }, GetType(), true);
            #endif
		}

		/// <summary>
		/// If AExecute is not null, calls the CreateDelegate of the DynamicMethod and sets the execute field of the node to the value returned.
		/// </summary>
		/// <param name="execute">The DynamicMethod instance returned by a previous call to BeginEmitIL.</param>
		protected void EndEmitIL(DynamicMethod execute, ILGenerator generator)
		{
			if (DataType == null)
				generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Ret);
			ILExecute = (ExecuteDelegate)execute.CreateDelegate(typeof(ExecuteDelegate), this);
		}
		
		public object TestExecute(Program program)
		{
			return Nodes[0].Execute(program);
		}
		
		public LocalBuilder EmitThis(Plan plan, ILGenerator generator, int[] executePath)
		{
			// Declare a local variable to store the "this" pointer for the method (This is a path to the PlanNode for this node from the root IL generation node)
			LocalBuilder thisValue = generator.DeclareLocal(typeof(PlanNode));
			
			// Load this of root plan node
			generator.Emit(OpCodes.Ldarg_0);

			// for each index in the execute path, load the node reference in the Nodes list for that node
			for (int index = 0; index < executePath.Length; index++)
			{
			    generator.Emit(OpCodes.Ldfld, _nodesFieldInfo);
				generator.Emit(OpCodes.Ldfld, _planNodesFieldInfo);
				generator.Emit(OpCodes.Ldc_I4, executePath[index]);
				generator.Emit(OpCodes.Ldelem_Ref);
			}
			
			// Store it in the "this" local
			generator.Emit(OpCodes.Stloc, thisValue);

			return thisValue;			
		}
		
		public int[] PrepareExecutePath(Plan plan, int[] executePath)
		{
			int[] localExecutePath = new int[executePath.Length + 1];
			executePath.CopyTo(localExecutePath, 0);
			return localExecutePath;
		}
		
		public void EmitExecute(Plan plan, ILGenerator generator, int[] executePath, int nodeIndex)
		{
			executePath[executePath.Length - 1] = nodeIndex;
			Nodes[nodeIndex].EmitIL(plan, generator, executePath);
			
			// Pop the return value if necessary
			if (Nodes[nodeIndex].DataType != null)
				generator.Emit(OpCodes.Pop);
		}
		
		public void EmitEvaluate(Plan plan, ILGenerator generator, int[] executePath, int nodeIndex)
		{
			executePath[executePath.Length - 1] = nodeIndex;
			Nodes[nodeIndex].EmitIL(plan, generator, executePath);
		}
		
		public virtual void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			// Generate a call to the Execute delegate for this node
			
			LocalBuilder thisValue = EmitThis(plan, generator, executePath);
			
			// Load the address of the execute delegate for the "this" node
			generator.Emit(OpCodes.Ldloc, thisValue);
			generator.Emit(OpCodes.Ldfld, _executeFieldInfo);
			
			// Prepare the execute call arguments
			generator.Emit(OpCodes.Ldarg_1);		 // AProcess
			
			// Invoke Execute
			generator.EmitCall(OpCodes.Callvirt, _executeMethodInfo, null);
			
			// If no return value, eat the null
			if (DataType == null)
				generator.Emit(OpCodes.Pop);
		}
		
		protected virtual void InternalEmitIL(Plan plan) 
		{
			if (_shouldEmitIL)
			{
				DynamicMethod execute = BeginEmitIL();
				ILGenerator generator = execute.GetILGenerator();
				EmitIL(plan, generator, new int[]{});
				EndEmitIL(execute, generator);
			} 
		}
		
		// Statement
		public virtual Statement EmitStatement(EmitMode mode)
		{
			throw new RuntimeException(RuntimeException.Codes.StatementNotSupported, ToString());
		}
		
		public string EmitStatementAsString(bool removeLineBreaks)
		{
			string statement = new D4TextEmitter().Emit(EmitStatement(EmitMode.ForCopy));
			if (removeLineBreaks)
			{
				bool inWhiteSpace = false;
				StringBuilder builder = new StringBuilder();
				for (int index = 0; index < statement.Length; index++)
				{
					if (Char.IsWhiteSpace(statement, index))
					{
						if (!inWhiteSpace)
						{
							inWhiteSpace = true;
							builder.Append(" ");
						}
					}
					else
					{
						inWhiteSpace = false;
						builder.Append(statement[index]);
					}
				}
				return builder.ToString();
			}
			return statement;
		}
		
		public string EmitStatementAsString()
		{
			return EmitStatementAsString(true);
		}
		
		public string SafeEmitStatementAsString()
		{
			return SafeEmitStatementAsString(true);
		}
		
		public string SafeEmitStatementAsString(bool removeLineBreaks)
		{
			try
			{
				return EmitStatementAsString(removeLineBreaks);
			}
			catch
			{
				return String.Format(@"Statement cannot be emitted for plan nodes of type ""{0}"".", GetType().Name);
			}
		}

		// DataType
		[Reference] // TODO: This should be a reference for scalar types, but owned for all other types
		protected Schema.IDataType _dataType;
		public virtual Schema.IDataType DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}
		
        // DetermineDataType
        public virtual void DetermineDataType(Plan plan) {}
        
        // DetermineModifiers
        protected virtual void DetermineModifiers(Plan plan) 
        {
			if (Modifiers != null)
			{
				IgnoreUnsupported = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "IgnoreUnsupported", IgnoreUnsupported.ToString()));
				ShouldSupport = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "ShouldSupport", ShouldSupport.ToString()));
				ShouldEmitIL = _shouldEmitIL && (Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "ShouldEmitIL", ShouldEmitIL.ToString())));
			}
        }

		// DetermineCharacteristics        
		public virtual void DetermineCharacteristics(Plan plan) {}
		
        // DetermineBinding
        public virtual void DetermineBinding(Plan plan)
        {
			if (_dataType != null)
				plan.PushTypeContext(_dataType);
			try
			{
				InternalDetermineBinding(plan);
			}
			finally
			{
				if (_dataType != null)
					plan.PopTypeContext(_dataType);
			}
			
			DetermineDevice(plan);
        }

		public virtual void InternalDetermineBinding(Plan plan)
		{
			if (_nodes != null)
				for (int index = 0; index < Nodes.Count; index++)
					Nodes[index].DetermineBinding(plan);
		}
		
		/// <summary>Rechecks security for the plan using the given plan and associated security context.</summary>
		public virtual void BindToProcess(Plan plan)
		{
			if (_nodes != null)
				for (int index = 0; index < Nodes.Count; index++)
					Nodes[index].BindToProcess(plan);
		}
		
        // ILExecute
        protected ExecuteDelegate ILExecute;

		// BeforeExecute        
        protected virtual void InternalBeforeExecute(Program program) { }
        
		public object Execute(Program program)
		{
			#if WRAPRUNTIMEEXCEPTIONS
			try
			{
			#endif

				if (IsBreakable)
					program.Yield(this, false);
				else
					program.CheckAborted();

				if (ILExecute != null)
					return ILExecute(program);
				InternalBeforeExecute(program);
				if (_deviceSupported)
					return program.DeviceExecute(_device, this);
				return InternalExecute(program);

			#if WRAPRUNTIMEEXCEPTIONS
			}
			catch (Exception exception)
			{
				bool isNew = false;
				Exception toThrow = null;
				
				RuntimeException runtimeException = exception as RuntimeException;
				if (runtimeException != null)
				{
					if (!runtimeException.HasContext())
					{
						runtimeException.SetLocator(program.GetLocation(this, true));
						isNew = true;
					}
					toThrow = runtimeException;
				}
				
				if ((toThrow == null) && (exception is ControlError))
					throw exception;

				if ((toThrow == null) && (exception is NullReferenceException))
				{
					toThrow = new RuntimeException(RuntimeException.Codes.NilEncountered, exception, program.GetLocation(this, true));
					isNew = true;
				}
					
				if (toThrow == null)
				{
					DataphorException dataphorException = exception as DataphorException;
					if (dataphorException != null)
					{
						if ((dataphorException.Severity == ErrorSeverity.User) || (dataphorException.ServerContext != null) || (dataphorException.Code == (int)RuntimeException.Codes.RuntimeError))
							toThrow = dataphorException;
						else
						{
							toThrow = new RuntimeException(RuntimeException.Codes.RuntimeError, dataphorException.Severity, dataphorException, program.GetLocation(this, true), dataphorException.Message);
							isNew = true;
						}
					}
				}
				
				if ((toThrow == null) && ((exception is FormatException) || (exception is ArgumentException) || (exception is ArithmeticException)))
				{
					toThrow = new DataphorException(ErrorSeverity.User, DataphorException.ApplicationError, exception.Message, exception);
					isNew = true;
				}
					
				if (toThrow == null)
				{
					toThrow = new RuntimeException(RuntimeException.Codes.RuntimeError, ErrorSeverity.Application, exception, program.GetLocation(this, true), exception.Message);
					isNew = true;
				}
				
				if (isNew)
					program.ReportThrow();
					
				if (IsBreakable)
					program.Yield(this, true);
					
				throw toThrow;
			}
			#endif
		}
		
        public abstract object InternalExecute(Program program);
        
		#region ShowPlan

		public virtual string Description
		{
			get 
			{
				string name = GetType().Name;
				if (name.EndsWith("Node"))
					return name.Substring(0, name.Length - 4);
				else
					return name;
			}
		}

		public virtual string Category
		{
			get { return "Unknown"; }
		}

		protected virtual void WritePlanElement(System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement(GetType().Name);
		}

		protected virtual void WritePlanNodes(System.Xml.XmlWriter writer)
		{
			if (_nodes != null)
				for (int index = 0; index < Nodes.Count; index++)
					Nodes[index].WritePlan(writer);
		}

		protected static void WritePlanTags(System.Xml.XmlWriter writer, MetaData metaData)
		{
			if (metaData != null)
			{
				#if USEHASHTABLEFORTAGS
				foreach (Tag tag in AMetaData.Tags)
				{
				#else
				Tag tag;
				for (int index = 0; index < metaData.Tags.Count; index++)
				{
					tag = metaData.Tags[index];
				#endif
					writer.WriteStartElement("Tags.Tag");
					writer.WriteAttributeString("Name", tag.Name);
					writer.WriteAttributeString("Value", tag.Value);
					writer.WriteEndElement();
				}
			}
		}

		protected virtual void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			writer.WriteAttributeString("ID", GetHashCode().ToString());
			writer.WriteAttributeString("Statement", SafeEmitStatementAsString(false));
			writer.WriteAttributeString("Class", GetType().FullName);
			writer.WriteAttributeString("Description", Description);
			writer.WriteAttributeString("Category", Category);
			if (_dataType != null)
				writer.WriteAttributeString("Type", _dataType.Name);
			writer.WriteAttributeString("Characteristics", CharacteristicsToString(this));
			writer.WriteAttributeString("DeviceSupported", Convert.ToString(DeviceSupported));
			if (DeviceSupported)
				writer.WriteAttributeString("Device", _device.DisplayName);
			writer.WriteAttributeString("CouldSupport", Convert.ToString(_couldSupport));
			writer.WriteAttributeString("ShouldSupport", Convert.ToString(_shouldSupport));
			writer.WriteAttributeString("IgnoreUnsupported", Convert.ToString(_ignoreUnsupported));
			writer.WriteAttributeString("DeviceAssociative", Convert.ToString((_device == null) && !_noDevice));
			if (_deviceMessages != null)
				writer.WriteAttributeString("DeviceMessages", _deviceMessages.ToString());
		}

        public virtual void WritePlan(System.Xml.XmlWriter writer)
        {
			WritePlanElement(writer);
			WritePlanAttributes(writer);
			WritePlanNodes(writer);
			writer.WriteEndElement();
        }

		#endregion
	}
	
	public class PlanNodes : System.Object, IList
	{
		public const int InitialCapacity = 0;
		
		public PlanNodes() : base(){}

		private int _count;
		private PlanNode[] _nodes;
		
        public PlanNode this[int index]
        {	
			get 
			{ 
				#if DEBUG
				if (index >= _count)
					throw new IndexOutOfRangeException();
				#endif
				return _nodes[index];
			}
			set 
			{ 
				#if DEBUG
				if (index >= _count)
					throw new IndexOutOfRangeException();
				#endif
				_nodes[index] = value;
			}
        }
        
        private void EnsureCapacity(int requiredCapacity)
        {
			if ((_nodes == null) || (_nodes.Length <= requiredCapacity))
			{
				PlanNode[] FNewNodes = new PlanNode[Math.Max(_nodes == null ? 0 : _nodes.Length, 1) * 2];
				if (_nodes != null)
					for (int index = 0; index < _nodes.Length; index++)
						FNewNodes[index] = _nodes[index];
				_nodes = FNewNodes;
			}
        }
        
        public int Add(PlanNode tempValue)
        {
			EnsureCapacity(_count);
			_nodes[_count] = tempValue;
			_count++;
			return _count - 1;
        }
        
        public void Insert(int index, PlanNode tempValue)
        {
			EnsureCapacity(_count);
			for (int localIndex = _count - 1; localIndex >= index; localIndex--)
				_nodes[localIndex + 1] = _nodes[localIndex];
			_nodes[index] = tempValue;
			_count++;
        }

        public PlanNode Remove(PlanNode tempValue)
        {
			return RemoveAt(IndexOf(tempValue));
        }
        
        public PlanNode RemoveAt(int index)
        {
			PlanNode node = _nodes[index];
			_count--;
			for (int localIndex = index; localIndex < _count; localIndex++)
				_nodes[localIndex] = _nodes[localIndex + 1];
			_nodes[_count] = null; // Clear the last item to prevent a resource leak
			return node;
        }
        
        public void Clear()
        {
			while (_count > 0)
				RemoveAt(_count - 1);
        }
        
        public bool Contains(PlanNode tempValue)
        {
			return IndexOf(tempValue) >= 0;
        }

        public int IndexOf(object tempValue)
        {
			for (int index = 0; index < _count; index++)
				if (_nodes[index] == tempValue)
					return index;
			return -1;
        }

        public int Count { get { return _count; } }

        // IEnumerable interface
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public PlanNodeEnumerator GetEnumerator()
        {
			return new PlanNodeEnumerator(this);
        }

        public class PlanNodeEnumerator : IEnumerator
        {
            public PlanNodeEnumerator(PlanNodes planNodes) : base()
            {
                _planNodes = planNodes;
            }
            
            private int _current = -1;
            private PlanNodes _planNodes;

            public PlanNode Current { get { return _planNodes[_current]; } }
            
            object IEnumerator.Current { get { return _planNodes[_current]; } }

            public void Reset()
            {
                _current = -1;
            }

            public bool MoveNext()
            {
				_current++;
				return _current < _planNodes.Count;
            }
        }
        
        // ICollection
        void ICollection.CopyTo(System.Array array, int index)
        {
			IList list = (IList)array;
			for (int localIndex = 0; localIndex < Count; localIndex++)
				list[index + localIndex] = this[localIndex];
        }
        
        bool ICollection.IsSynchronized { get { return false; } }
        
        object ICollection.SyncRoot { get { return this; } }
        
        // IList
        int IList.Add(object tempValue)
        {
			return Add((PlanNode)tempValue);
        }
        
        bool IList.Contains(object tempValue)
        {
			return IndexOf(tempValue) >= 0;
        }
        
        void IList.Insert(int index, object tempValue)
        {
			Insert(index, (PlanNode)tempValue);
        }
        
        bool IList.IsFixedSize { get { return false; } }
        
        bool IList.IsReadOnly { get { return false; } }
        
        void IList.Remove(object tempValue)
        {
			Remove((PlanNode)tempValue);
        }
        
        void IList.RemoveAt(int index)
        {
			RemoveAt(index);
        }
        
        object IList.this[int index]
        {
			get { return this[index]; }
			set { this[index] = (PlanNode)value; }
        }
	}
	
	public class DevicePlanNode : System.Object
	{
		public DevicePlanNode(PlanNode node) : base()
		{
			_node = node;
		}
		
		protected PlanNode _node;
		public PlanNode Node { get { return _node; } }
	}
}

