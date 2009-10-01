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
		public Operand(Operator AOperator, string AName, IDataType ADataType) : base()
		{
			FName = AName;
			FOperator = AOperator;
			FDataType = ADataType;
		}

		public Operand(Operator AOperator, string AName, IDataType ADataType, Modifier AModifier) : base()
		{				
			FName = AName;
			FOperator = AOperator;
			FDataType = ADataType;
			FModifier = AModifier;
		}
		
		// Name
		private string FName;
		public string Name { get { return FName; } }
		
		// Operator
		[Reference]
		private Operator FOperator;
		public Operator Operator { get { return FOperator; } }

		// Modifier
		private Modifier FModifier;
		public Modifier Modifier
		{
			get { return FModifier; }
			set { FModifier = value; }
		}
		
        // DataType
		[Reference]
        private IDataType FDataType;
        public IDataType DataType
        {
			get { return FDataType; }
			set { FDataType = value; }
		}
    }
    
	public class Operands : NotifyingBaseList<Operand>
    {
    }
    
    public class OperatorBlock : System.Object
    {
		// LineInfo
		protected LineInfo FLineInfo;
		public LineInfo LineInfo { get { return FLineInfo; } }
		
		public void SetLineInfo(Plan APlan, LineInfo ALineInfo)
		{
			if (ALineInfo != null)
			{
				if (FLineInfo == null)
					FLineInfo = new LineInfo();
				
				FLineInfo.Line = ALineInfo.Line - APlan.CompilingOffset.Line;
				FLineInfo.LinePos = ALineInfo.LinePos - ((APlan.CompilingOffset.Line == ALineInfo.Line) ? APlan.CompilingOffset.LinePos : 0);
				FLineInfo.EndLine = ALineInfo.EndLine - APlan.CompilingOffset.Line;
				FLineInfo.EndLinePos = ALineInfo.EndLinePos - ((APlan.CompilingOffset.Line == ALineInfo.EndLine) ? APlan.CompilingOffset.LinePos : 0);
			}
		}

		// StackDisplacement
		protected int FStackDisplacement = 0;
		public int StackDisplacement
		{
			get { return FStackDisplacement; }
			set { FStackDisplacement = value; }
		}

        // ClassDefinition
        protected ClassDefinition FClassDefinition;
        public ClassDefinition ClassDefinition
        {
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
        }
        
        // BlockNode
        protected PlanNode FBlockNode;
        public PlanNode BlockNode
        {
			get { return FBlockNode; }
			set { FBlockNode = value; }
        }
        
        public void EmitStatement(EmitMode AMode, D4.OperatorBlock ABlock)
        {
			if (ClassDefinition != null)
				ABlock.ClassDefinition = (ClassDefinition)ClassDefinition.Clone();
			else
				ABlock.Block = BlockNode.EmitStatement(AMode);
        }
        
        public AccessorBlock EmitAccessorBlock(EmitMode AMode)
        {
			AccessorBlock LBlock = new AccessorBlock();
			if (ClassDefinition != null)
				LBlock.ClassDefinition = (ClassDefinition)ClassDefinition.Clone();
			else
				LBlock.Block = BlockNode.EmitStatement(AMode);
			return LBlock;
        }
    }
    
	public class Operator : CatalogObject
    {
		public Operator(string AName) : base(AName)
		{
			OperatorName = AName;
			InternalInitialize();
		}
		
		public Operator(int AID, string AName) : base(AID, AName)
		{
			OperatorName = AName;
			InternalInitialize();
		}

		public Operator(int AID, string AName, Operand[] AOperands, IDataType AReturnType) : base(AID, AName)
		{
			OperatorName = AName;
			FReturnDataType = AReturnType;
			FOperands.AddRange(AOperands);
			InternalInitialize();
			OperandsChanged();
		}

		public Operator(int AID, string AName, string AClassName, IDataType AReturnType, Operand[] AOperands) 
			: this(AID, AName, AClassName, AReturnType, AOperands, false)
		{ }
		
		public Operator(int AID, string AName, string AClassName, IDataType AReturnType, Operand[] AOperands, bool AIsBuiltin) 
			: this(AID, AName, AClassName, AReturnType, AOperands, AIsBuiltin, true)
		{ }
		
		public Operator(int AID, string AName, string AClassName, IDataType AReturnType, Operand[] AOperands, bool AIsBuiltin, bool AIsRemotable) 
			: base(AID, AName)
		{
			OperatorName = AName;
			FBlock.ClassDefinition = new ClassDefinition(AClassName);
			FReturnDataType = AReturnType;
			FOperands.AddRange(AOperands);
			InternalInitialize();
			OperandsChanged();
			IsBuiltin = AIsBuiltin;
			IsRemotable = AIsRemotable;
		}

		private void InternalInitialize()
		{
			FIsLiteral = true;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsNilable = false;
			FOperands.Changed += OperandsChanged;
		}
		
		public override string DisplayName { get { return String.Format("{0}{1}", FOperatorName, FSignature.ToString()); } }
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Operator"), OperatorName, Signature.ToString()); } }

		private void OperandsChanged(NotifyingBaseList<Operand> ASender, bool AIsAdded, Operand AItem, int AIndex)
		{
			OperandsChanged();
		}
		
		/// <summary>Forces the signature to be recreated.</summary>
		public void OperandsChanged()
		{
			FNameReset = true;
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

		public static string EnsureValidIdentifier(string AString)
		{
			StringBuilder LResult = new StringBuilder();
			for (int LIndex = 0; LIndex < AString.Length; LIndex++)
				if (Char.IsLetterOrDigit(AString[LIndex]) || (AString[LIndex] == '_'))
					LResult.Append(AString[LIndex]);
				else if (AString[LIndex] != ' ')
					LResult.Append('_');
			return LResult.ToString();
		}
		
		private void EnsureNameReset()
		{
			if (FNameReset)
			{
				FSignature = new Signature(FOperands);
				Name = GetGeneratedName(FOperatorName, ID);
				FNameReset = false;
			}
		}
		
		/// <summary>Returns the mangled name of the operator.</summary>
		/// <remarks>
		/// Used to reference the operator by a unique name independent of it's object id.
		/// Because the mangled name may exceed the maximum identifier length, this is only useful for
		/// in-memory resolution, it is not used as a persistent reference to the operator.
		/// </remarks>
		public string MangledName { get { return String.Format("{0}{1}", FOperatorName, EnsureValidIdentifier(FSignature.ToString())); } }
		
		private bool FNameReset = true;
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
		private string FOperatorName = String.Empty;
		public string OperatorName
		{
			get { return FOperatorName; }
			set
			{
				if (FOperatorName != value)
				{
					FOperatorName = (value == null ? String.Empty : value);
					Name = String.Format("{0}_{1}", FOperatorName.Length > Schema.Object.CMaxGeneratedNameLength ? FOperatorName.Substring(0, Schema.Object.CMaxGeneratedNameLength) : FOperatorName, ID.ToString().PadLeft(Schema.Object.CMaxObjectIDLength, '0'));
					OperandsChanged();
				}
			}
		}
		
		// DeclarationText
		private string FDeclarationText;
		public string DeclarationText
		{
			get { return FDeclarationText; }
			set { FDeclarationText = value; }
		}
		
		// BodyText
		private string FBodyText;
		public string BodyText
		{
			get { return FBodyText; }
			set { FBodyText = value; }
		}
		
		// Locator
		private DebugLocator FLocator;
		public DebugLocator Locator
		{
			get { return FLocator; }
			set { FLocator = value; }
		}
		
		/// <summary>
		/// Parses the DebugLocator from the DAE.Locator tag, if present.
		/// </summary>
		public static DebugLocator GetLocator(MetaData AMetaData)
		{
			Tag LTag = MetaData.RemoveTag(AMetaData, "DAE.Locator");
			if (LTag != Tag.None)
				return DebugLocator.Parse(LTag.Value);
				
			return null;
		}

		public void SaveLocator()
		{
			if (FLocator != null)
			{
				if (MetaData == null)
					MetaData = new MetaData();
				MetaData.Tags.AddOrUpdate("DAE.Locator", FLocator.ToString(), true);
			}
		}
		
		public void RemoveLocator()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.Locator");
		}

		// ATOperatorName
		private string FSourceOperatorName;
		public string SourceOperatorName
		{
			get { return FSourceOperatorName; }
			set { FSourceOperatorName = value; }
		}
		
		public override bool IsATObject { get { return (SourceOperatorName != null); } }
		
        // IsLiteral - Indicates that the operator is literal, in other words, compile-time evaluable
        protected bool FIsLiteral;
        public bool IsLiteral
        {
			get { return FIsLiteral; }
			set { FIsLiteral = value; }
        }
        
        // IsFunctional - Indicates that the operator makes no changes to its arguments, or the global state of the system
        protected bool FIsFunctional;
        public bool IsFunctional
        {
			get { return FIsFunctional; }
			set { FIsFunctional = value; }
        }
        
        // IsDeterministic - Indicates whether repeated invocations of the operator with the same arguments return the same result
        protected bool FIsDeterministic;
        public bool IsDeterministic
        {
			get { return FIsDeterministic; }
			set { FIsDeterministic = value; }
        }
        
        // IsRepeatable - Indicates whether repeated invocations of the operator with the same arguments return the same result within the same transaction
        protected bool FIsRepeatable;
        public bool IsRepeatable
        {
			get { return FIsRepeatable; }
			set { FIsRepeatable = value; }
        }
        
        // IsNilable - Indicates whether the operator could return a nil
        protected bool FIsNilable;
        public bool IsNilable
        {
			get { return FIsNilable; }
			set { FIsNilable = value; }
		}
        
		// IsBuiltin - True if this is a builtin operator (i.e. +, -, *, / etc, (parser recognized operator))
		private bool FIsBuiltin;
		public bool IsBuiltin 
		{ 
			get { return FIsBuiltin; } 
			set { FIsBuiltin = value; } 
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
		private bool FShouldRecompile;
		public bool ShouldRecompile
		{
			get { return FShouldRecompile; }
			set { FShouldRecompile = value; }
		}
		
        // Operands
        private Operands FOperands = new Operands();
        public Operands Operands { get { return FOperands; } }
        
        // Signature
        private Signature FSignature = new Signature(new SignatureElement[]{});
        public Signature Signature 
        { 
			get 
			{ 
				EnsureNameReset();
				return FSignature; 
			} 
		}

        // OperatorSignature
        private OperatorSignature FOperatorSignature;
        public OperatorSignature OperatorSignature
        {
			get { return FOperatorSignature; }
			set { FOperatorSignature = value; }
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
        private IDataType FReturnDataType;
		public IDataType ReturnDataType
        {
			get { return FReturnDataType; }
			set { FReturnDataType = value; }
        }

		// Block        
        private OperatorBlock FBlock = new OperatorBlock();
        public OperatorBlock Block { get { return FBlock; } }

		public override void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			if ((SourceOperatorName != null) && (AMode == EmitMode.ForRemote))
				ASourceCatalog[SourceOperatorName].IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			else
			{
				if (!ATargetCatalog.Contains(this))
				{
					ATargetCatalog.Add(this);
					base.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
					foreach (Operand LOperand in Operands)
						LOperand.DataType.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
					if (ReturnDataType != null)
						ReturnDataType.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
				}
			}
		}
		
		public virtual Statement EmitHeader()
		{
			CreateOperatorStatement LStatement = (CreateOperatorStatement)EmitStatement(EmitMode.ForCopy);
			LStatement.Block.ClassDefinition = null;
			LStatement.Block.Block = null;
			LStatement.MetaData = null;
			return LStatement;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
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
			
			IMetaData LResult;
			if ((AMode != EmitMode.ForRemote) && (FDeclarationText != null))
			{
				SourceStatement LStatement = new SourceStatement();
				LStatement.Source = FDeclarationText + FBodyText;
				LResult = LStatement;
			}
			else
			{
				CreateOperatorStatement LStatement = new CreateOperatorStatement();
				LStatement.OperatorName = Schema.Object.EnsureRooted(OperatorName);
				foreach (Operand LOperand in Operands)
				{
					FormalParameter LFormalParameter = new FormalParameter();
					LFormalParameter.Identifier = LOperand.Name;
					LFormalParameter.TypeSpecifier = LOperand.DataType.EmitSpecifier(AMode);
					LFormalParameter.Modifier = LOperand.Modifier;
					LStatement.FormalParameters.Add(LFormalParameter);
				}
				if (ReturnDataType != null)
					LStatement.ReturnType = ReturnDataType.EmitSpecifier(AMode);
				#if USEVIRTUAL
				LStatement.IsVirtual = IsVirtual;
				LStatement.IsAbstract = IsAbstract;
				LStatement.IsOverride = IsOverride;
				LStatement.IsReintroduced = IsReintroduced;
				#endif
				if ((AMode == EmitMode.ForRemote) && !IsRemotable)
					LStatement.Block.Block = new Block();
				else
					Block.EmitStatement(AMode, LStatement.Block);
				LResult = LStatement;
			}

			LResult.MetaData = MetaData == null ? null : MetaData.Copy();
			if (SessionObjectName != null)
			{
				if (LResult.MetaData == null)
					LResult.MetaData = new MetaData();
				LResult.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", OperatorName, true);
			}
			return (Statement)LResult;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropOperatorStatement LStatement = new DropOperatorStatement();
			LStatement.ObjectName = Schema.Object.EnsureRooted(OperatorName);
			foreach (Operand LOperand in Operands)
			{
				FormalParameterSpecifier LSpecifier = new FormalParameterSpecifier();
				LSpecifier.Modifier = LOperand.Modifier;
				LSpecifier.TypeSpecifier = LOperand.DataType.EmitSpecifier(AMode);
				LStatement.FormalParameterSpecifiers.Add(LSpecifier);
			}
			return LStatement;
		}
    }
    
    public class AggregateOperator : Operator
    {
		public AggregateOperator(string AName) : base(AName) {}
		public AggregateOperator(int AID, string AName) : base(AID, AName) {}

		// System constructor -> any operator created with this constructor will have IsSystem set to true
		public AggregateOperator(int AID, string AName, Operand[] AOperands, IDataType AReturnType, ClassDefinition AInitialization, ClassDefinition AAggregation, ClassDefinition AFinalization) : base(AID, AName, AOperands, AReturnType)
		{
			Initialization.ClassDefinition = AInitialization;
			Aggregation.ClassDefinition = AAggregation;
			Finalization.ClassDefinition = AFinalization;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.AggregateOperator"), OperatorName, Signature.ToString()); } }

		// Initialization
        private OperatorBlock FInitialization = new OperatorBlock();
        public OperatorBlock Initialization { get { return FInitialization; } }
        
        // Aggregation (same as Operator.Block)
        public OperatorBlock Aggregation { get { return Block; } }

		// Finalization
        private OperatorBlock FFinalization = new OperatorBlock();
        public OperatorBlock Finalization { get { return FFinalization; } }
        
		// InitializationText
		private string FInitializationText;
		public string InitializationText
		{
			get { return FInitializationText; }
			set { FInitializationText = value; }
		}
		
		public string AggregationText
		{
			get { return BodyText; }
			set { BodyText = value; }
		}
		
		// FinalizationText
		private string FFinalizationText;
		public string FinalizationText
		{
			get { return FFinalizationText; }
			set { FFinalizationText = value; }
		}
		
        // IsOrderDependent
        private bool FIsOrderDependent;
        public bool IsOrderDependent
        {
			get { return FIsOrderDependent; }
			set { FIsOrderDependent = value; }
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveLocator();
			}
			else
			{
				RemoveObjectID();
				RemoveLocator();
			}
				
			IMetaData LResult;
			
			if ((AMode != EmitMode.ForRemote) && (DeclarationText != null))
			{
				SourceStatement LStatement = new SourceStatement();
				LStatement.Source = DeclarationText + InitializationText + AggregationText + FinalizationText;
				LResult = LStatement;
			}
			else
			{
				CreateAggregateOperatorStatement LStatement = new CreateAggregateOperatorStatement();
				LStatement.OperatorName = Schema.Object.EnsureRooted(OperatorName);
				foreach (Operand LOperand in Operands)
				{
					FormalParameter LFormalParameter = new FormalParameter();
					LFormalParameter.Identifier = LOperand.Name;
					LFormalParameter.TypeSpecifier = LOperand.DataType.EmitSpecifier(AMode);
					LFormalParameter.Modifier = LOperand.Modifier;
					LStatement.FormalParameters.Add(LFormalParameter);
				}
				LStatement.ReturnType = ReturnDataType.EmitSpecifier(AMode);
				#if USEVIRTUAL
				LStatement.IsVirtual = IsVirtual;
				LStatement.IsAbstract = IsAbstract;
				LStatement.IsOverride = IsOverride;
				LStatement.IsReintroduced = IsReintroduced;
				#endif
				if ((AMode == EmitMode.ForRemote) && !IsRemotable)
				{
					LStatement.Initialization.Block = new Block();
					LStatement.Aggregation.Block = new Block();
					LStatement.Finalization.Block = new Block();
				}
				else
				{
					Initialization.EmitStatement(AMode, LStatement.Initialization);
					Aggregation.EmitStatement(AMode, LStatement.Aggregation);
					Finalization.EmitStatement(AMode, LStatement.Finalization);
				}
				LResult = LStatement;
			}

			LResult.MetaData = MetaData == null ? null : MetaData.Copy();
			if (SessionObjectName != null)
			{
				if (LResult.MetaData == null)
					LResult.MetaData = new MetaData();
				LResult.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", OperatorName, true);
			}
			return (Statement)LResult;
		}

		public override Statement EmitHeader()
		{
			CreateAggregateOperatorStatement LStatement = (CreateAggregateOperatorStatement)EmitStatement(EmitMode.ForCopy);
			LStatement.Initialization.ClassDefinition = null;
			LStatement.Initialization.Block = null;
			LStatement.Aggregation.ClassDefinition = null;
			LStatement.Aggregation.Block = null;
			LStatement.Finalization.ClassDefinition = null;
			LStatement.Finalization.Block = null;
			LStatement.MetaData = null;
			return LStatement;
		}
    }
}