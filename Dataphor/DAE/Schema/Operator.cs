/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Schema
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Catalog;

	// TODO: Refactor these dependencies
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public class Operand : System.Object
    {
		public Operand(Operator operatorValue, string name, IDataType dataType) : base()
		{
			_name = name;
			_operator = operatorValue;
			_dataType = dataType;
		}

		public Operand(Operator operatorValue, string name, IDataType dataType, Modifier modifier) : base()
		{				
			_name = name;
			_operator = operatorValue;
			_dataType = dataType;
			_modifier = modifier;
		}
		
		// Name
		private string _name;
		public string Name { get { return _name; } }
		
		// Operator
		[Reference]
		private Operator _operator;
		public Operator Operator { get { return _operator; } }

		// Modifier
		private Modifier _modifier;
		public Modifier Modifier
		{
			get { return _modifier; }
			set { _modifier = value; }
		}
		
        // DataType
		[Reference]
        private IDataType _dataType;
        public IDataType DataType
        {
			get { return _dataType; }
			set { _dataType = value; }
		}
    }
    
	public class Operands : NotifyingBaseList<Operand>
    {
    }
    
    public class OperatorBlock : System.Object
    {
		// LineInfo
		protected LineInfo _lineInfo;
		public LineInfo LineInfo { get { return _lineInfo; } }
		
		public void SetLineInfo(Plan plan, LineInfo lineInfo)
		{
			if (lineInfo != null)
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				
				_lineInfo.Line = lineInfo.Line - plan.CompilingOffset.Line;
				_lineInfo.LinePos = lineInfo.LinePos - ((plan.CompilingOffset.Line == lineInfo.Line) ? plan.CompilingOffset.LinePos : 0);
				_lineInfo.EndLine = lineInfo.EndLine - plan.CompilingOffset.Line;
				_lineInfo.EndLinePos = lineInfo.EndLinePos - ((plan.CompilingOffset.Line == lineInfo.EndLine) ? plan.CompilingOffset.LinePos : 0);
			}
		}

		// StackDisplacement
		protected int _stackDisplacement = 0;
		public int StackDisplacement
		{
			get { return _stackDisplacement; }
			set { _stackDisplacement = value; }
		}

        // ClassDefinition
        protected ClassDefinition _classDefinition;
        public ClassDefinition ClassDefinition
        {
			get { return _classDefinition; }
			set { _classDefinition = value; }
        }
        
        // BlockNode
        protected PlanNode _blockNode;
        public PlanNode BlockNode
        {
			get { return _blockNode; }
			set { _blockNode = value; }
        }
        
        public void EmitStatement(EmitMode mode, D4.OperatorBlock block)
        {
			if (ClassDefinition != null)
				block.ClassDefinition = (ClassDefinition)ClassDefinition.Clone();
			else
				block.Block = BlockNode.EmitStatement(mode);
        }
        
        public AccessorBlock EmitAccessorBlock(EmitMode mode)
        {
			AccessorBlock block = new AccessorBlock();
			if (ClassDefinition != null)
				block.ClassDefinition = (ClassDefinition)ClassDefinition.Clone();
			else
				block.Block = BlockNode.EmitStatement(mode);
			return block;
        }
    }
    
	public class Operator : CatalogObject
    {
		public Operator(string name) : base(name)
		{
			OperatorName = name;
			InternalInitialize();
		}
		
		public Operator(int iD, string name) : base(iD, name)
		{
			OperatorName = name;
			InternalInitialize();
		}

		public Operator(int iD, string name, Operand[] operands, IDataType returnType) : base(iD, name)
		{
			OperatorName = name;
			_returnDataType = returnType;
			_operands.AddRange(operands);
			InternalInitialize();
			OperandsChanged();
		}

		public Operator(int iD, string name, string className, IDataType returnType, Operand[] operands) 
			: this(iD, name, className, returnType, operands, false)
		{ }
		
		public Operator(int iD, string name, string className, IDataType returnType, Operand[] operands, bool isBuiltin) 
			: this(iD, name, className, returnType, operands, isBuiltin, true)
		{ }
		
		public Operator(int iD, string name, string className, IDataType returnType, Operand[] operands, bool isBuiltin, bool isRemotable) 
			: base(iD, name)
		{
			OperatorName = name;
			_block.ClassDefinition = new ClassDefinition(className);
			_returnDataType = returnType;
			_operands.AddRange(operands);
			InternalInitialize();
			OperandsChanged();
			IsBuiltin = isBuiltin;
			IsRemotable = isRemotable;
		}

		private void InternalInitialize()
		{
			_isLiteral = true;
			_isFunctional = true;
			_isDeterministic = true;
			_isRepeatable = true;
			_isNilable = false;
			_operands.Changed += OperandsChanged;
		}
		
		public override string DisplayName { get { return String.Format("{0}{1}", _operatorName, _signature.ToString()); } }
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Operator"), OperatorName, Signature.ToString()); } }

		private void OperandsChanged(NotifyingBaseList<Operand> sender, bool isAdded, Operand item, int index)
		{
			OperandsChanged();
		}
		
		/// <summary>Forces the signature to be recreated.</summary>
		public void OperandsChanged()
		{
			_nameReset = true;
		}

		public override string[] GetRights()
		{
			return new string[]
			{
				Name + Schema.RightNames.Alter,
				Name + Schema.RightNames.Drop,
				Name + Schema.RightNames.Execute
			};
		}

		public static string EnsureValidIdentifier(string stringValue)
		{
			StringBuilder result = new StringBuilder();
			for (int index = 0; index < stringValue.Length; index++)
				if (Char.IsLetterOrDigit(stringValue[index]) || (stringValue[index] == '_'))
					result.Append(stringValue[index]);
				else if (stringValue[index] != ' ')
					result.Append('_');
			return result.ToString();
		}
		
		private void EnsureNameReset()
		{
			if (_nameReset)
			{
				_signature = new Signature(_operands);
				Name = GetGeneratedName(_operatorName, ID);
				_nameReset = false;
			}
		}
		
		/// <summary>Returns the mangled name of the operator.</summary>
		/// <remarks>
		/// Used to reference the operator by a unique name independent of it's object id.
		/// Because the mangled name may exceed the maximum identifier length, this is only useful for
		/// in-memory resolution, it is not used as a persistent reference to the operator.
		/// </remarks>
		public string MangledName { get { return String.Format("{0}{1}", _operatorName, EnsureValidIdentifier(_signature.ToString())); } }
		
		private bool _nameReset = true;
		public override string Name
		{
			get
			{
				EnsureNameReset();
				return base.Name;	
			}
			set { base.Name = value; }
		}
		
		// OperatorName
		private string _operatorName = String.Empty;
		public string OperatorName
		{
			get { return _operatorName; }
			set
			{
				if (_operatorName != value)
				{
					_operatorName = (value == null ? String.Empty : value);
					Name = String.Format("{0}_{1}", _operatorName.Length > Schema.Object.MaxGeneratedNameLength ? _operatorName.Substring(0, Schema.Object.MaxGeneratedNameLength) : _operatorName, ID.ToString().PadLeft(Schema.Object.MaxObjectIDLength, '0'));
					OperandsChanged();
				}
			}
		}
		
		// DeclarationText
		private string _declarationText;
		public string DeclarationText
		{
			get { return _declarationText; }
			set { _declarationText = value; }
		}
		
		// BodyText
		private string _bodyText;
		public string BodyText
		{
			get { return _bodyText; }
			set { _bodyText = value; }
		}
		
		// Locator
		private DebugLocator _locator;
		public DebugLocator Locator
		{
			get { return _locator; }
			set { _locator = value; }
		}
		
		/// <summary>
		/// Parses the DebugLocator from the DAE.Locator tag, if present.
		/// </summary>
		public static DebugLocator GetLocator(MetaData metaData)
		{
			Tag tag = MetaData.RemoveTag(metaData, "DAE.Locator");
			if (tag != Tag.None)
				return DebugLocator.Parse(tag.Value);
				
			return null;
		}

		public void SaveLocator()
		{
			if (_locator != null)
				AddMetaDataTag("DAE.Locator", _locator.ToString(), true);
		}
		
		public void RemoveLocator()
		{
			RemoveMetaDataTag("DAE.Locator");
		}

		// ATOperatorName
		private string _sourceOperatorName;
		public string SourceOperatorName
		{
			get { return _sourceOperatorName; }
			set { _sourceOperatorName = value; }
		}
		
		public override bool IsATObject { get { return (SourceOperatorName != null); } }
		
        // IsLiteral - Indicates that the operator is literal, in other words, compile-time evaluable
        protected bool _isLiteral;
        public bool IsLiteral
        {
			get { return _isLiteral; }
			set { _isLiteral = value; }
        }
        
        // IsFunctional - Indicates that the operator makes no changes to its arguments, or the global state of the system
        protected bool _isFunctional;
        public bool IsFunctional
        {
			get { return _isFunctional; }
			set { _isFunctional = value; }
        }
        
        // IsDeterministic - Indicates whether repeated invocations of the operator with the same arguments return the same result
        protected bool _isDeterministic;
        public bool IsDeterministic
        {
			get { return _isDeterministic; }
			set { _isDeterministic = value; }
        }
        
        // IsRepeatable - Indicates whether repeated invocations of the operator with the same arguments return the same result within the same transaction
        protected bool _isRepeatable;
        public bool IsRepeatable
        {
			get { return _isRepeatable; }
			set { _isRepeatable = value; }
        }
        
        // IsNilable - Indicates whether the operator could return a nil
        protected bool _isNilable;
        public bool IsNilable
        {
			get { return _isNilable; }
			set { _isNilable = value; }
		}
        
		// IsBuiltin - True if this is a builtin operator (i.e. +, -, *, / etc, (parser recognized operator))
		private bool _isBuiltin;
		public bool IsBuiltin 
		{ 
			get { return _isBuiltin; } 
			set { _isBuiltin = value; } 
		}
		
		// ShouldTranslate
		/// <summary>Indicates whether or not this operator should be translated into an application transaction.</summary>
		/// <remarks>
		/// By default, operators are translated into an application transaction if they are not host-implemented, 
		/// access global state, and are not functional.  To change this behavior, use the DAE.ShouldTranslate tag.
		/// </remarks>
		public bool ShouldTranslate
		{
			get { return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.ShouldTranslate", ((Block.BlockNode != null) && !IsRemotable && !IsFunctional).ToString())); }
			set
			{
				if (MetaData == null)
					MetaData = new MetaData();
				MetaData.Tags.AddOrUpdate("DAE.ShouldTranslate", value.ToString(), true);
			}
		}
		
		// ShouldRecompile - True if this operator should be recompiled when it is next invoked
		private bool _shouldRecompile;
		public bool ShouldRecompile
		{
			get { return _shouldRecompile; }
			set { _shouldRecompile = value; }
		}
		
        // Operands
        private Operands _operands = new Operands();
        public Operands Operands { get { return _operands; } }
        
        // Signature
        private Signature _signature = new Signature(new SignatureElement[]{});
        public Signature Signature 
        { 
			get 
			{ 
				EnsureNameReset();
				return _signature; 
			} 
		}

        // OperatorSignature
        private OperatorSignature _operatorSignature;
        public OperatorSignature OperatorSignature
        {
			get { return _operatorSignature; }
			set { _operatorSignature = value; }
        }

		#if USEVIRTUAL        
		// IsAbstract        
		private bool FIsAbstract;
		public bool IsAbstract
		{
			get { return FIsAbstract; }
			set { FIsAbstract = value; }
		}
		
		// IsVirtual
		private bool FIsVirtual;
		public bool IsVirtual
		{
			get { return FIsVirtual; }
			set { FIsVirtual = value; }
		}

		// IsOverride
		private bool FIsOverride;
		public bool IsOverride
		{
			get { return FIsOverride; }
			set { FIsOverride = value; }
		}
		
		// IsReintroduced -- this is redundant, as it can be derived from the operator hierarchy, but it makes statement emission easier
		private bool FIsReintroduced;
		public bool IsReintroduced
		{
			get { return FIsReintroduced; }
			set { FIsReintroduced = value; }
		}
		
		// IsVirtualCall
		public bool IsVirtualCall
		{
			get { return FIsAbstract || FIsVirtual || FIsOverride; }
		}
		#endif
		
        // ReturnDataType
		[Reference]
        private IDataType _returnDataType;
		public IDataType ReturnDataType
        {
			get { return _returnDataType; }
			set { _returnDataType = value; }
        }

		// Block        
        private OperatorBlock _block = new OperatorBlock();
        public OperatorBlock Block { get { return _block; } }

		public override void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			if ((SourceOperatorName != null) && (mode == EmitMode.ForRemote))
			{
				var sourceObjectName = MetaData.GetTag(MetaData, "DAE.SourceObjectName", SourceOperatorName);
				sourceCatalog[sourceObjectName].IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
			}
			else
			{
				if (!targetCatalog.Contains(this))
				{
					targetCatalog.Add(this);
					base.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
					foreach (Operand operand in Operands)
						operand.DataType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
					if (ReturnDataType != null)
						ReturnDataType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
				}
			}
		}
		
		public virtual Statement EmitHeader()
		{
			CreateOperatorStatement statement = (CreateOperatorStatement)EmitStatement(EmitMode.ForCopy);
			statement.Block.ClassDefinition = null;
			statement.Block.Block = null;
			statement.MetaData = null;
			return statement;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveGeneratorID();
				SaveLocator();
			}
			else
			{
				RemoveObjectID();
				RemoveGeneratorID();
				RemoveLocator();
			}
			try
			{
				IMetaData result;
				if ((mode != EmitMode.ForRemote) && (_declarationText != null))
				{
					SourceStatement statement = new SourceStatement();
					statement.Source = _declarationText + _bodyText;
					result = statement;
				}
				else
				{
					CreateOperatorStatement statement = new CreateOperatorStatement();
					statement.OperatorName = Schema.Object.EnsureRooted(OperatorName);
					foreach (Operand operand in Operands)
					{
						FormalParameter formalParameter = new FormalParameter();
						formalParameter.Identifier = operand.Name;
						formalParameter.TypeSpecifier = operand.DataType.EmitSpecifier(mode);
						formalParameter.Modifier = operand.Modifier;
						statement.FormalParameters.Add(formalParameter);
					}
					if (ReturnDataType != null)
						statement.ReturnType = ReturnDataType.EmitSpecifier(mode);
					#if USEVIRTUAL
					statement.IsVirtual = IsVirtual;
					statement.IsAbstract = IsAbstract;
					statement.IsOverride = IsOverride;
					statement.IsReintroduced = IsReintroduced;
					#endif
					if ((mode == EmitMode.ForRemote) && !IsRemotable)
						statement.Block.Block = new Block();
					else
						Block.EmitStatement(mode, statement.Block);
					result = statement;
				}

				result.MetaData = MetaData == null ? null : MetaData.Copy();

				if (SessionObjectName != null)
				{
					if (result.MetaData == null)
						result.MetaData = new MetaData();
					result.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", OperatorName, true);
				}
				return (Statement)result;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
				{
					RemoveObjectID();
					RemoveGeneratorID();
					RemoveLocator();
				}
			}
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropOperatorStatement statement = new DropOperatorStatement();
			statement.ObjectName = Schema.Object.EnsureRooted(OperatorName);
			foreach (Operand operand in Operands)
			{
				FormalParameterSpecifier specifier = new FormalParameterSpecifier();
				specifier.Modifier = operand.Modifier;
				specifier.TypeSpecifier = operand.DataType.EmitSpecifier(mode);
				statement.FormalParameterSpecifiers.Add(specifier);
			}
			return statement;
		}
    }
    
    public class AggregateOperator : Operator
    {
		public AggregateOperator(string name) : base(name) {}
		public AggregateOperator(int iD, string name) : base(iD, name) {}

		// System constructor -> any operator created with this constructor will have IsSystem set to true
		public AggregateOperator(int iD, string name, Operand[] operands, IDataType returnType, ClassDefinition initialization, ClassDefinition aggregation, ClassDefinition finalization) : base(iD, name, operands, returnType)
		{
			Initialization.ClassDefinition = initialization;
			Aggregation.ClassDefinition = aggregation;
			Finalization.ClassDefinition = finalization;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.AggregateOperator"), OperatorName, Signature.ToString()); } }

		// Initialization
        private OperatorBlock _initialization = new OperatorBlock();
        public OperatorBlock Initialization { get { return _initialization; } }
        
        // Aggregation (same as Operator.Block)
        public OperatorBlock Aggregation { get { return Block; } }

		// Finalization
        private OperatorBlock _finalization = new OperatorBlock();
        public OperatorBlock Finalization { get { return _finalization; } }
        
		// InitializationText
		private string _initializationText;
		public string InitializationText
		{
			get { return _initializationText; }
			set { _initializationText = value; }
		}
		
		public string AggregationText
		{
			get { return BodyText; }
			set { BodyText = value; }
		}
		
		// FinalizationText
		private string _finalizationText;
		public string FinalizationText
		{
			get { return _finalizationText; }
			set { _finalizationText = value; }
		}
		
        // IsOrderDependent
        private bool _isOrderDependent;
        public bool IsOrderDependent
        {
			get { return _isOrderDependent; }
			set { _isOrderDependent = value; }
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveLocator();
			}
			else
			{
				RemoveObjectID();
				RemoveLocator();
			}
			try
			{
				IMetaData result;
			
				if ((mode != EmitMode.ForRemote) && (DeclarationText != null))
				{
					SourceStatement statement = new SourceStatement();
					statement.Source = DeclarationText + InitializationText + AggregationText + FinalizationText;
					result = statement;
				}
				else
				{
					CreateAggregateOperatorStatement statement = new CreateAggregateOperatorStatement();
					statement.OperatorName = Schema.Object.EnsureRooted(OperatorName);
					foreach (Operand operand in Operands)
					{
						FormalParameter formalParameter = new FormalParameter();
						formalParameter.Identifier = operand.Name;
						formalParameter.TypeSpecifier = operand.DataType.EmitSpecifier(mode);
						formalParameter.Modifier = operand.Modifier;
						statement.FormalParameters.Add(formalParameter);
					}
					statement.ReturnType = ReturnDataType.EmitSpecifier(mode);
					#if USEVIRTUAL
					statement.IsVirtual = IsVirtual;
					statement.IsAbstract = IsAbstract;
					statement.IsOverride = IsOverride;
					statement.IsReintroduced = IsReintroduced;
					#endif
					if ((mode == EmitMode.ForRemote) && !IsRemotable)
					{
						statement.Initialization.Block = new Block();
						statement.Aggregation.Block = new Block();
						statement.Finalization.Block = new Block();
					}
					else
					{
						Initialization.EmitStatement(mode, statement.Initialization);
						Aggregation.EmitStatement(mode, statement.Aggregation);
						Finalization.EmitStatement(mode, statement.Finalization);
					}
					result = statement;
				}

				result.MetaData = MetaData == null ? null : MetaData.Copy();
				if (SessionObjectName != null)
				{
					if (result.MetaData == null)
						result.MetaData = new MetaData();
					result.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", OperatorName, true);
				}
				return (Statement)result;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
				{
					RemoveObjectID();
					RemoveLocator();
				}
			}
		}

		public override Statement EmitHeader()
		{
			CreateAggregateOperatorStatement statement = (CreateAggregateOperatorStatement)EmitStatement(EmitMode.ForCopy);
			statement.Initialization.ClassDefinition = null;
			statement.Initialization.Block = null;
			statement.Aggregation.ClassDefinition = null;
			statement.Aggregation.Block = null;
			statement.Finalization.ClassDefinition = null;
			statement.Finalization.Block = null;
			statement.MetaData = null;
			return statement;
		}
    }
}