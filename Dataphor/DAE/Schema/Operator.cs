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
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
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
    
	public class Operands : NotifyList
    {
		public Operands() : base(false) { }

        public new Operand this[int AIndex]
        {
            get { return (Operand)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class OperatorBlock : System.Object
    {
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
			OperandsChanged(null, null);
		}

		public Operator(int AID, string AName, string AClassName, IDataType AReturnType, Operand[] AOperands) : base(AID, AName)
		{
			OperatorName = AName;
			FBlock.ClassDefinition = new ClassDefinition(AClassName);
			FReturnDataType = AReturnType;
			FOperands.AddRange(AOperands);
			InternalInitialize();
			OperandsChanged(null, null);
		}
		
		public Operator(int AID, string AName, string AClassName, IDataType AReturnType, Operand[] AOperands, bool AIsBuiltin) : base(AID, AName)
		{
			OperatorName = AName;
			FBlock.ClassDefinition = new ClassDefinition(AClassName);
			FReturnDataType = AReturnType;
			FOperands.AddRange(AOperands);
			InternalInitialize();
			OperandsChanged(null, null);
			IsBuiltin = AIsBuiltin;
		}
		
		public Operator(int AID, string AName, string AClassName, IDataType AReturnType, Operand[] AOperands, bool AIsBuiltin, bool AIsRemotable) : base(AID, AName)
		{
			OperatorName = AName;
			FBlock.ClassDefinition = new ClassDefinition(AClassName);
			FReturnDataType = AReturnType;
			FOperands.AddRange(AOperands);
			InternalInitialize();
			OperandsChanged(null, null);
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
			FOperands.OnAdding += new ListEventHandler(OperandsChanged);
			FOperands.OnRemoving += new ListEventHandler(OperandsChanged);
		}
		
		public override string DisplayName { get { return String.Format("{0}{1}", FOperatorName, FSignature.ToString()); } }
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Operator"), OperatorName, Signature.ToString()); } }

		private void OperandsChanged(object ASender, object AItem)
		{
			FNameReset = true;
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
					OperandsChanged(null, null);
				}
			}
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

		public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			if ((SourceOperatorName != null) && (AMode == EmitMode.ForRemote))
				ASourceCatalog[SourceOperatorName].IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			else
			{
				if (!ATargetCatalog.Contains(this))
				{
					ATargetCatalog.Add(this);
					base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
					foreach (Operand LOperand in Operands)
						LOperand.DataType.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
					if (ReturnDataType != null)
						ReturnDataType.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
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
			}
			else
			{
				RemoveObjectID();
				RemoveGeneratorID();
			}

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
			if ((AMode == EmitMode.ForRemote) && (Block.BlockNode != null) && !IsRemotable)
				LStatement.Block.Block = new Block();
			else
				Block.EmitStatement(AMode, LStatement.Block);
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			if (SessionObjectName != null)
			{
				if (LStatement.MetaData == null)
					LStatement.MetaData = new MetaData();
				LStatement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", OperatorName, true);
			}
			return LStatement;
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
				SaveObjectID();
			else
				RemoveObjectID();

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
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			if (SessionObjectName != null)
			{
				if (LStatement.MetaData == null)
					LStatement.MetaData = new MetaData();
				LStatement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", OperatorName, true);
			}
			return LStatement;
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
    
	public class OperatorSignature : System.Object
    {
		public OperatorSignature(Operator AOperator)
		{
			FOperator = AOperator;
			FSignatures = new OperatorSignatures(this);
			#if USEVIRTUAL
			#if USETYPEDLIST
			FParentSignatures = new TypedList(typeof(OperatorSignature), false);
			#else
			FParentSignatures = new BaseList<OperatorSignature>();
			#endif
			#endif
		}

		#if USEVIRTUAL
		#if USETYPEDLIST
		private TypedList FParentSignatures;
		public TypedList ParentSignatures { get { return FParentSignatures; } }
		#else
		private BaseList<OperatorSignature> FParentSiagntures;
		public BaseList<OperatorSignature> ParentSignatures { get { return FParentSignatures; } }
		#endif
		#endif
		
		private OperatorSignatures FSignatures;
		public OperatorSignatures Signatures { get { return FSignatures; } }
		
		[Reference]
		private Operator FOperator;
		public Operator Operator { get { return FOperator; } }
		
		public Signature Signature { get { return FOperator.Signature; } }

		/*
			Virtual Resolution Algorithm ->
				this algorithm works from the following assumptions:
					1. This operator is virtual (abstract, virtual or override is true)
					2. This operator "is" the signature being resolved
					
				foreach signature in this list
					if the signature is an override and it "is" the signature being resolved
						return the ResolveSignature of the signature
				return this signature
		*/
		
		public OperatorSignature ResolveVirtual(Signature ASignature)
		{
			#if USEVIRTUAL
			foreach (OperatorSignature LSignature in FSignatures)
				if (LSignature.Operator.IsOverride && LSignature.Signature.Is(ASignature))
					return LSignature.ResolveVirtual(ASignature);
			#endif
			return this;
		}
		
		public override bool Equals(object AValue)
		{
			return (AValue is OperatorSignature) && Signature.Equals(((OperatorSignature)AValue).Signature);
		}
		
		public override int GetHashCode()
		{
			return Signature.GetHashCode();
		}
		
		public string ShowSignature(int ADepth)
		{
			StringBuilder LString = new StringBuilder();
			LString.Append(new String('\t', ADepth));
			LString.Append(Signature.ToString());
			LString.Append("\n");
			LString.Append(FSignatures.ShowSignatures(ADepth + 1));
			return LString.ToString();
		}
    }
    
	public class OperatorSignatures : System.Object
    {
		public OperatorSignatures(OperatorSignature ASignature) : base()
		{
			FSignature = ASignature;
		}
		
		private OperatorSignature FSignature;		
		private Hashtable FSignatures = new Hashtable(); // keys : Signature, values : OperatorSignature
		
		public int Count { get { return FSignatures.Count; } }
		
		public Hashtable Signatures { get { return FSignatures; } }

		public OperatorSignature this[Signature ASignature]
		{
			get
			{
				OperatorSignature LSignature = FSignatures[ASignature] as OperatorSignature;
				if (LSignature == null)
					throw new SchemaException(SchemaException.Codes.SignatureNotFound, ASignature.ToString());
				return LSignature;
			}
		}
		
		/*
			Insertion Algorithm ->
				for each signature
					if the signature being added is the signature
						add the signature to this signature
					else if the signature is the signature being added
						for each signature in this list
							if the signature is the signature being added
								remove it from this list
								add it to the signature being added
						add all child signatures of signatures in this list to the signature, if applicable
						add the signature to this list
				if the signature has not yet been added
					add all child signatures of signatures in this list to the signature, if applicable
					add the signature to this list
		*/
		
		//public override int Add(object AValue)
		public void Add(OperatorSignature ASignature)
		{
			#if USETYPEINHERITANCE
			bool LAdded = false;
			int LIndex;
			for (LIndex = 0; LIndex < Count; LIndex++)
			{		
				if (ASignature.Signature.Equals(this[LIndex].Signature))
					continue;
				else if (ASignature.Signature.Is(this[LIndex].Signature))
				{
					if (!this[LIndex].Signatures.Contains(ASignature))
						this[LIndex].Signatures.Add(ASignature);
					LAdded = true;
				}
				else if (this[LIndex].Signature.Is(ASignature.Signature))
				{
					if (!Contains(ASignature))
					{
						for (int LInnerIndex = Count - 1; LInnerIndex >= 0; LInnerIndex--)
							if (this[LInnerIndex].Signature.Is(ASignature.Signature))
								if (!ASignature.Signatures.Contains(this[LInnerIndex].Signature))
									ASignature.Signatures.Add(InternalRemoveAt(LInnerIndex));
								else
									InternalRemoveAt(LInnerIndex);
						InternalAdd(ASignature);
					}
					LAdded = true;
				}
			}

			if (!LAdded)
				InternalAdd(ASignature);
			
			Adding(AValue, LIndex);
			return LIndex;
			#endif
			FSignatures.Add(ASignature.Signature, ASignature);
		}
		
		/*
			Removal Algorithm ->
				if the signature is in this list
					Remove the signature
					for each signature in the signature being removed
						if the signature is not in this list
							add the signature to this list
				else
					for each signature in this list
						if the signature being removed is the signature
							remove the signature being removed from the signatures for this signature
					if the signature was not removed
						throw a SignatureNotFound
		*/
		
		public void Remove(Signature ASignature)
		{
			FSignatures.Remove(ASignature);
			#if USETYPEINHERITANCE
			int LIndex = IndexOf(ASignature);
			if (LIndex >= 0)
				RemoveAt(LIndex);
			else
			{
				for (LIndex = 0; LIndex < Count; LIndex++)
					if (ASignature.Is(this[LIndex].Signature))
						this[LIndex].Signatures.Remove(ASignature);
			}
			#endif
		}
		
		public void Remove(OperatorSignature ASignature)
		{
			Remove(ASignature.Signature);
		}
		
		/*
			Resolution Algorithm ->
				if the signature is in this list
					return the operator signature
				else
					for each signature in this list
						if the given signature is the signature
							return the Resolve on the signature
					return null
		*/
		
		public void Resolve(Plan APlan, OperatorBindingContext AContext)
		{
			OperatorSignature LResultSignature = FSignatures[AContext.CallSignature] as OperatorSignature;
			if (LResultSignature != null)
			{
				if (!AContext.Matches.Contains(LResultSignature))
					AContext.Matches.Add(new OperatorMatch(LResultSignature, true));
			}
			else
			{
				OperatorSignature LSignature;
				foreach (DictionaryEntry LEntry in FSignatures)
				{
					LSignature = (OperatorSignature)LEntry.Value;
					if (AContext.CallSignature.Is(LSignature.Signature))
					{
						int LMatchCount = AContext.Matches.Count;
						LSignature.Signatures.Resolve(APlan, AContext);
						if (AContext.Matches.IsExact)
							break;
						else if (LMatchCount == AContext.Matches.Count)
						{
							if (!AContext.Matches.Contains(LSignature))
							{
								OperatorMatch LMatch = new OperatorMatch(LSignature, false);
								for (int LIndex = 0; LIndex < LSignature.Signature.Count; LIndex++)
									LMatch.CanConvert[LIndex] = true;
								AContext.Matches.Add(LMatch);
							}
						}
					}
					else
					{
						if (AContext.CallSignature.Count == LSignature.Signature.Count)
						{
							if (!AContext.Matches.Contains(LSignature))
							{
								OperatorMatch LMatch = new OperatorMatch(LSignature);
								bool LAddMatch = true;
								LMatch.IsMatch = true;
								for (int LElementIndex = 0; LElementIndex < AContext.CallSignature.Count; LElementIndex++)
								{
									LMatch.CanConvert[LElementIndex] = AContext.CallSignature[LElementIndex].DataType.Is(LSignature.Signature[LElementIndex].DataType);
									if (!LMatch.CanConvert[LElementIndex] && (AContext.CallSignature[LElementIndex].Modifier != Modifier.Var) && (LSignature.Signature[LElementIndex].Modifier != Modifier.Var))
									{
										LMatch.ConversionContexts[LElementIndex] = Compiler.FindConversionPath(APlan, AContext.CallSignature[LElementIndex].DataType, LSignature.Signature[LElementIndex].DataType);
										LMatch.CanConvert[LElementIndex] = LMatch.ConversionContexts[LElementIndex].CanConvert;
										
										// As soon as the match being constructed is more narrowing or longer than the best match found so far, it can be safely discarded as a candidate.
										if ((LMatch.NarrowingScore < AContext.Matches.BestNarrowingScore) || ((LMatch.NarrowingScore == AContext.Matches.BestNarrowingScore) && (LMatch.PathLength > AContext.Matches.ShortestPathLength)))
										{
											LAddMatch = false;
											break;
										}
									}

									if (!LMatch.CanConvert[LElementIndex])
										LMatch.IsMatch = false;
								}
								if (LAddMatch)
									AContext.Matches.Add(LMatch);
							}
						}
					}
				}
			}
		}

		#if USEVIRTUAL		
		public OperatorSignature ResolveInherited(Signature ASignature)
		{
			OperatorSignature LSignature = Resolve(ASignature, false);
			if (LSignature == null)
				return null;
			else
			{
				if (LSignature.ParentSignatures.Count == 1)
					return (OperatorSignature)LSignature.ParentSignatures[0];
				else
					throw new SchemaException(SchemaException.Codes.AmbiguousInheritedCall, LSignature.Operator.Name);
			} 
		}
		#endif
		
		public bool Contains(Signature ASignature)
		{
			#if USETYPEINHERITANCE
			if (FSignatures.Contains(ASignature))
				return true;
			foreach (DictionaryEntry LEntry in FSignatures)
				if (((OperatorSignature)LEntry.Value).Contains(ASignature))
					return true;
			return false;
			#else
			return FSignatures.Contains(ASignature);
			#endif
		}
		
		public bool Contains(OperatorSignature ASignature)
		{
			return Contains(ASignature.Signature);
		}

		public string ShowSignatures(int ADepth)
		{
			StringBuilder LString = new StringBuilder();
			foreach (DictionaryEntry LEntry in FSignatures)
				LString.Append(((OperatorSignature)LEntry.Value).ShowSignature(ADepth));
			return LString.ToString();
		}
    }

	/// <summary>Contains information about potential signature resolution matches.</summary>
    public class OperatorMatch : System.Object
    {
		/// <summary>Constructs a potential match.</summary>
		public OperatorMatch(OperatorSignature ASignature) : base()
		{
			Signature = ASignature;
			FConversionContexts = new ConversionContext[ASignature.Signature.Count];
			FCanConvert = new BitArray(ASignature.Signature.Count);
			for (int LIndex = 0; LIndex < FCanConvert.Length; LIndex++)
				FCanConvert[LIndex] = true;
		}

		/// <summary>Constructs an exact or partial match, depending on the value of AIsExact.</summary>		
		public OperatorMatch(OperatorSignature ASignature, bool AIsExact) : base()
		{
			Signature = ASignature;
			FConversionContexts = new ConversionContext[ASignature.Signature.Count];
			FCanConvert = new BitArray(ASignature.Signature.Count);
			IsExact = AIsExact;
			IsMatch = true;
		}
		
		[Reference]
		public OperatorSignature Signature;

		/// <summary>Indicates whether this signature is an exact match with the call signature. (No casting or conversion required)</summary>
		public bool IsExact;
		
		/// <summary>Indicates whether this signature is a match with the call signature. (Casting or conversion may be required)</summary>
		public bool IsMatch;
		
		/// <summary>Indicates that this signature is a match with the call signature but that casting or conversion is required.</summary>
		public bool IsPartial { get { return IsMatch && !IsExact; } }

		private BitArray FCanConvert;
		/// <summary>For each parameter in the signature, indicates whether a potential conversion was found between the calling signature argument type and this signatures parameter type.</summary>
		public BitArray CanConvert { get { return FCanConvert; } }

		private ConversionContext[] FConversionContexts;
		/// <summary>Contains a potential conversion context for each parameter in the signature.  If the reference is null, if CanConvert is true, then no conversion is required, otherwise, the modifiers were not compatible.</summary>
		public ConversionContext[] ConversionContexts { get { return FConversionContexts; } }
		
		/// <summary>Indicates the total narrowing score for this match.  The narrowing score is the sum of the narrowing scores for all conversions in the match. </summary>
		public int NarrowingScore
		{
			get
			{
				int LNarrowingScore = 0;
				for (int LIndex = 0; LIndex < FConversionContexts.Length; LIndex++)
				{
					if (FConversionContexts[LIndex] != null)
					{
						if (FConversionContexts[LIndex].CanConvert)
							LNarrowingScore += FConversionContexts[LIndex].NarrowingScore;
						else
						{
							LNarrowingScore = Int32.MinValue;
							break;
						}
					}
				}
				return LNarrowingScore;
			}
		}				  
		
		/// <summary>Indicates the total path length for the conversions in this match.</summary>
		public int PathLength
		{
			get
			{
				int LPathLength = 0;
				for (int LIndex = 0; LIndex < FConversionContexts.Length; LIndex++)
				{
					if (FConversionContexts[LIndex] != null)
					{
						if (FConversionContexts[LIndex].CanConvert)
							LPathLength += FConversionContexts[LIndex].PathLength;
						else
						{
							LPathLength = Int32.MaxValue;
							break;
						}
					}
				}
				return LPathLength;
			}
		}
    }

	#if USETYPEDLIST    
    public class OperatorMatchList : TypedList
    {
		public OperatorMatchList() : base(typeof(OperatorMatch)) {}
		
		public new OperatorMatch this[int AIndex]
		{
			get { return (OperatorMatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			OperatorMatch LMatch = (OperatorMatch)AValue;
			if (IndexOf(LMatch) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateOperatorMatch, LMatch.Signature.Operator.Name);
		}
	#else
	public class OperatorMatchList : NonNullList<OperatorMatch>
	{
		protected override void Validate(OperatorMatch AValue)
		{
			base.Validate(AValue);
			if (IndexOf(AValue) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateOperatorMatch, AValue.Signature.Operator.Name);
		}
	#endif
	
		public int IndexOf(Operator AOperator)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if 
				(
					(String.Compare(this[LIndex].Signature.Operator.Name, AOperator.Name) == 0) ||
					(
						this[LIndex].Signature.Operator.IsATObject &&
						(String.Compare(this[LIndex].Signature.Operator.SourceOperatorName, AOperator.OperatorName) == 0) &&
						this[LIndex].Signature.Operator.Signature.Equals(AOperator.Signature)
					) ||
					(
						AOperator.IsATObject &&
						(String.Compare(this[LIndex].Signature.Operator.OperatorName, AOperator.SourceOperatorName) == 0) &&
						this[LIndex].Signature.Operator.Signature.Equals(AOperator.Signature)
					)
				)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Operator AOperator)
		{
			return IndexOf(AOperator) >= 0;
		}
		
		public int IndexOf(OperatorSignature ASignature)
		{
			return IndexOf(ASignature.Operator);
		}
		
		public bool Contains(OperatorSignature ASignature)
		{
			return Contains(ASignature.Operator);
		}
		
		#if USETYPEDLIST
		public int IndexOf(OperatorMatch AMatch)
		{
			return IndexOf(AMatch.Signature.Operator);
		}
		
		public bool Contains(OperatorMatch AMatch)
		{
			return Contains(AMatch.Signature.Operator);
		}
		#else
		public override int IndexOf(OperatorMatch AMatch)
		{
			return IndexOf(AMatch.Signature.Operator);
		}
		#endif
    }
    
    public class OperatorMatches : OperatorMatchList
    {
		public OperatorMatches() : base() {}
		
		/// <summary>Indicates whether or not a successful signature match was found.</summary>
		public bool IsMatch { get { return Match != null; } }
		
		/// <summary>Indicates whether the resolved signature is an exact match for the calling signature. (No casting or conversion required) </summary>
		public bool IsExact { get { return Match == null ? false : Match.IsExact; } }

		/// <summary>Indicates whether the resolved signature is a partial match for the calling signature. (Casting or conversion required) </summary>
		public bool IsPartial { get { return Match == null ? false : Match.IsPartial; } }
		
		/// <summary>Indicates whether more than one signature matches the calling signature.</summary>
		public bool IsAmbiguous 
		{ 
			get 
			{
				if (IsExact)
					return false;
					
				int LBestMatchCount = 0;
				foreach (OperatorMatch LMatch in FBestMatches)
					if (LMatch.PathLength == FShortestPathLength)
						LBestMatchCount++;
				return LBestMatchCount > 1;
			} 
		}
		
		private OperatorMatch FMatch;
		/// <summary>Returns the resolved signature for the calling signature.  Null if no match was found.</summary>
		public OperatorMatch Match
		{
			get
			{
				if (!FIsMatchComputed)
				{
					FindMatch();
					FIsMatchComputed = true;
				}
				return FMatch;
			}
		}
		
		private bool FIsMatchComputed;
		
		private void FindMatch()
		{
			FMatch = null;
			int LExactCount = 0;
			foreach (OperatorMatch LMatch in FBestMatches)
			{
				if (LMatch.IsExact)
				{
					LExactCount++;
					if (LExactCount == 1)
						FMatch = LMatch;
					else
					{
						FMatch = null;
						break;
					}
				}
				else if (LMatch.IsPartial)
				{
					if (FMatch == null)
					{
						if (LMatch.PathLength == FShortestPathLength)
							FMatch = LMatch;
					}
					else
					{
						if (FMatch.PathLength == LMatch.PathLength)
						{
							FMatch = null;
							break;
						}
					}
				}
			}
		}
		
		private int FBestNarrowingScore = Int32.MinValue;
		/// <summary>Returns the best narrowing score for the possible matches for the calling signature.</summary>
		public int BestNarrowingScore { get { return FBestNarrowingScore; } }
		
		private int FShortestPathLength = Int32.MaxValue;
		/// <summary>Returns the shortest path length among the possible matches with the best narrowing score.</summary>
		public int ShortestPathLength { get { return FShortestPathLength; } }
		
		private OperatorMatchList FBestMatches = new OperatorMatchList();
		/// <summary>Returns the set of possible matches with the best narrowing score.</summary>
		public OperatorMatchList BestMatches { get { return FBestMatches; } }
		
		/// <summary>Returns the closest match for the given signature.</summary>
		public OperatorMatch ClosestMatch
		{
			get
			{
				// The most converting path with the least narrowing score and shortest path length
				int LMatchCount = 0;
				int LConversionCount = 0;
				int LBestConversionCount = 0;
				int LBestNarrowingScore = Int32.MinValue;
				int LShortestPathLength = Int32.MaxValue;
				OperatorMatch LClosestMatch = null;
				foreach (OperatorMatch LMatch in this)
				{
					LConversionCount = 0;
					foreach (bool LCanConvert in LMatch.CanConvert)
						if (LCanConvert)
							LConversionCount++;

					if ((LClosestMatch == null) || (LConversionCount > LBestConversionCount))
					{
						LBestConversionCount = LConversionCount;
						LBestNarrowingScore = LMatch.NarrowingScore;
						LShortestPathLength = LMatch.PathLength;
						LClosestMatch = LMatch;
						LMatchCount = 1;
					}
					else if (LConversionCount == LBestConversionCount)
					{
						if (LMatch.NarrowingScore > LBestNarrowingScore)
						{
							LBestNarrowingScore = LMatch.NarrowingScore;
							LShortestPathLength = LMatch.PathLength;
							LClosestMatch = LMatch;
							LMatchCount = 1;
						}
						else if (LMatch.NarrowingScore == LBestNarrowingScore)
						{
							if (LMatch.PathLength < LShortestPathLength)
							{
								LShortestPathLength = LMatch.PathLength;
								LClosestMatch = LMatch;
								LMatchCount = 1;
							}
							else
								LMatchCount++;
						}
						else
							LMatchCount++;
					}
				}
				
				return LMatchCount == 1 ? LClosestMatch : null;
			}
		}
		
		private void ComputeBestNarrowingScore()
		{
			FBestNarrowingScore = Int32.MinValue;
			foreach (OperatorMatch LMatch in this)
				if (LMatch.IsMatch && (LMatch.NarrowingScore > FBestNarrowingScore))
					FBestNarrowingScore = LMatch.NarrowingScore;
		}
		
		private void ComputeBestMatches()
		{
			FBestMatches.Clear();
			FShortestPathLength = Int32.MaxValue;
			foreach (OperatorMatch LMatch in this)
				if (LMatch.IsMatch && (LMatch.NarrowingScore == FBestNarrowingScore))
				{
					FBestMatches.Add(LMatch);
					if (LMatch.PathLength < FShortestPathLength)
						FShortestPathLength = LMatch.PathLength;
				}
		}
		
		#if USETYPEDLIST
		protected override void Adding(object AValue, int AIndex)
		{
			OperatorMatch LMatch = (OperatorMatch)AValue;
		#else
		protected override void Adding(OperatorMatch LMatch, int AIndex)
		{
		#endif
			if (LMatch.IsMatch)
			{
				if (LMatch.NarrowingScore > FBestNarrowingScore)
				{
					FBestNarrowingScore = LMatch.NarrowingScore;
					ComputeBestMatches();
				}
				else if (LMatch.NarrowingScore == FBestNarrowingScore)
				{
					FBestMatches.Add(LMatch);
					if (LMatch.PathLength < FShortestPathLength)
						FShortestPathLength = LMatch.PathLength;
				}
			}
			
			FIsMatchComputed = false;

			//base.Adding(AValue, AIndex);
		}

		private bool FIsClearing;
				
		public override void Clear()
		{
			FIsClearing = true;
			try
			{
				base.Clear();
			}
			finally
			{
				FIsClearing = false;
			}

			ComputeBestNarrowingScore();
			ComputeBestMatches();
			FIsMatchComputed = false;
		}
		
		#if USETYPEDLIST
		protected override void Removing(object AValue, int AIndex)
		#else
		protected override void Removing(OperatorMatch AValue, int AIndex)
		#endif
		{
			if (!FIsClearing)
			{
				ComputeBestNarrowingScore();
				ComputeBestMatches();
				FIsMatchComputed = false;
			}
			//base.Removing(AValue, AIndex);
		}
    }
    
	public class OperatorMap : Object
    {
		public OperatorMap(string AName) : base(AName)
		{
			FSignatures = new OperatorSignatures(null);
		}
		
		protected OperatorSignatures FSignatures;
		
		public OperatorSignatures Signatures { get { return FSignatures; } }
		
		public int SignatureCount
		{
			get { return FSignatures.Count; }
		}
		
		public void AddSignature(Operator AOperator)
		{
			FSignatures.Add(new OperatorSignature(AOperator));
		}
		
		public void RemoveSignature(Signature ASignature)
		{
			FSignatures.Remove(ASignature);
		}
		
		public bool ContainsSignature(Signature ASignature)
		{
			return FSignatures.Contains(ASignature);
		}
		
		public void ResolveSignature(Plan APlan, OperatorBindingContext AContext)
		{
			FSignatures.Resolve(APlan, AContext);
		}

		#if USEVIRTUAL		
		public Operator ResolveInheritedSignature(Signature ASignature)
		{
			OperatorSignature LSignature = FSignatures.ResolveInherited(ASignature);
			return LSignature != null ? LSignature.Operator : null;
		}
		#endif
		
		public string ShowMap()
		{
			StringBuilder LString = new StringBuilder(Name);
			LString.Append(":\n");
			LString.Append(FSignatures.ShowSignatures(1));
			return LString.ToString();
		}
    }
    
	public class OperatorMaps : Objects
    {		
		public new OperatorMap this[int AIndex]
		{
			get { return (OperatorMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public new OperatorMap this[string AName]
		{
			get { return (OperatorMap)base[AName]; }
			set { base[AName] = value; }
		}
		
		#if SINGLENAMESPACE
		public new OperatorMap this[string AName, string ANameSpace]
		{
			get { return (OperatorMap)base[AName, ANameSpace]; }
			set { base[AName, ANameSpace] = value; }
		}
		#endif
		
		public void ResolveCall(Plan APlan, OperatorBindingContext AContext)
		{
			lock (this)
			{
				bool LDidResolve = false;
				IntegerList LIndexes = InternalIndexesOf(AContext.OperatorName);
				OperatorBindingContext LContext = new OperatorBindingContext(AContext.Statement, AContext.OperatorName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
				for (int LIndex = 0; LIndex < LIndexes.Count; LIndex++)
				{
					LContext.OperatorNameContext.Names.Add(this[LIndexes[LIndex]].Name);
					this[LIndexes[LIndex]].ResolveSignature(APlan, LContext);
				}
				
				foreach (Schema.LoadedLibraries LLevel in APlan.NameResolutionPath)
				{
					OperatorBindingContext LLevelContext = new OperatorBindingContext(AContext.Statement, AContext.OperatorName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
					foreach (OperatorMatch LMatch in LContext.Matches)
					{
						// If the operator resolution is in any library at this level, add it to a binding context for this level
						if ((LMatch.Signature.Operator.Library == null) || LLevel.ContainsName(LMatch.Signature.Operator.Library.Name))
						{
							if (!LLevelContext.OperatorNameContext.Names.Contains(LMatch.Signature.Operator.OperatorName))
								LLevelContext.OperatorNameContext.Names.Add(LMatch.Signature.Operator.OperatorName);
								
							if (!LLevelContext.Matches.Contains(LMatch))
								LLevelContext.Matches.Add(LMatch);
						}
					}
					
					if (LLevelContext.Matches.IsExact)
					{
						LLevelContext.Operator = LLevelContext.Matches.Match.Signature.Operator;
						LLevelContext.OperatorNameContext.Object = this[IndexOfName(LLevelContext.Operator.OperatorName)];
						LLevelContext.OperatorNameContext.Names.Add(LLevelContext.OperatorNameContext.Object.Name);
						AContext.SetBindingDataFromContext(LLevelContext);
						return;
					}
					else
					{
						// If there is no match, or a partial match, collect the signatures and map names resolved at this level
						foreach (string LName in LLevelContext.OperatorNameContext.Names)
							if (!AContext.OperatorNameContext.Names.Contains(LName))
								AContext.OperatorNameContext.Names.Add(LName);
								
						foreach (OperatorMatch LMatch in LLevelContext.Matches)
							if (!AContext.Matches.Contains(LMatch))
							{
								AContext.Matches.Add(LMatch);
								LDidResolve = true;
							}
					}
				}
				
				// If a partial match is found within the name resolution path, use it
				if (!AContext.IsExact && AContext.Matches.IsPartial)
				{
					LDidResolve = true;
				}
				else
				{
					// The name resolution path has been searched and no match was found, so attempt to resolve based on all signatures
					if (LContext.Matches.IsExact)
					{
						LContext.Operator = LContext.Matches.Match.Signature.Operator;
						LContext.OperatorNameContext.Object = this[IndexOfName(LContext.Operator.OperatorName)];
						LContext.OperatorNameContext.Names.Add(LContext.OperatorNameContext.Object.Name);
						AContext.SetBindingDataFromContext(LContext);
						return;
					}
					else
					{
						// If there is no match, or a partial match, collect the signatures and map names resolved at all levels
						foreach (string LName in LContext.OperatorNameContext.Names)
							if (!AContext.OperatorNameContext.Names.Contains(LName))
								AContext.OperatorNameContext.Names.Add(LName);
								
						foreach (OperatorMatch LMatch in LContext.Matches)
							if (!AContext.Matches.Contains(LMatch))
							{
								AContext.Matches.Add(LMatch);
								LDidResolve = true;
							}
					}
				}
			
				// Ensure that if any resolutions were performed in this catalog, the binding data is set in the context
				if (LDidResolve)
				{
					if (AContext.Matches.IsExact || (!AContext.IsExact && AContext.Matches.IsPartial))
					{
						if ((AContext.Operator == null) || (AContext.Operator != AContext.Matches.Match.Signature.Operator))
						{
							AContext.Operator = AContext.Matches.Match.Signature.Operator;
							AContext.OperatorNameContext.Object = this[AContext.Operator.OperatorName];
						}
					}
					else
						AContext.Operator = null;
				}
			}
		}
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is OperatorMap))
				throw new SchemaException(SchemaException.Codes.OperatorMapContainer);
			base.Validate(AItem);
		}
		#endif
		
		public void AddOperator(Operator AOperator)
		{
			int LIndex = IndexOfName(AOperator.OperatorName);
			if (LIndex < 0)
			{
				OperatorMap LOperatorMap = new OperatorMap(AOperator.OperatorName);
				LOperatorMap.Library = AOperator.Library;
				LIndex = Add(LOperatorMap);
			}
			else
			{
				if (String.Compare(this[LIndex].Name, AOperator.OperatorName) != 0)
					throw new SchemaException(SchemaException.Codes.AmbiguousObjectName, AOperator.OperatorName, this[LIndex].Name);
			}
				
			this[LIndex].AddSignature(AOperator);
		}
		
		public void RemoveOperator(Operator AOperator)
		{
			AOperator.OperatorSignature = null;
			int LIndex = IndexOfName(AOperator.OperatorName);
			if (LIndex >= 0)
			{
				this[LIndex].RemoveSignature(AOperator.Signature);
				if (this[LIndex].SignatureCount == 0)
					RemoveAt(LIndex);
			}
			else
				throw new SchemaException(SchemaException.Codes.OperatorMapNotFound, AOperator.Name);
		}
		
		public bool ContainsOperator(Operator AOperator)
		{
			int LIndex = IndexOfName(AOperator.OperatorName);
			if (LIndex >= 0)
				return this[LIndex].ContainsSignature(AOperator.Signature);
			else
				return false;
		}
		
		public string ShowMaps()
		{
			StringBuilder LString = new StringBuilder();
			foreach (OperatorMap LMap in this)
			{
				LString.Append(LMap.ShowMap());
				LString.Append("\n");
			}
			return LString.ToString();
		}
    }
}