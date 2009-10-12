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
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

namespace Alphora.Dataphor.DAE.Schema
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Catalog;

	// TODO: Need to refactor these dependencies
	//using Alphora.Dataphor.DAE.Compiling;
	//using Alphora.Dataphor.DAE.Server;
	//using Alphora.Dataphor.DAE.Streams;
	//using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data; // Need to move NativeRepresentation resolution to the ValueManager
	using Alphora.Dataphor.DAE.Runtime.Instructions; // PlanNode

	public class Property : Object
	{
		public Property(int AID, string AName) : base(AID, AName) {}
		
		public Property(int AID, string AName, IDataType ADataType) : base(AName)
		{
			FDataType = ADataType;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Property"), DisplayName, FRepresentation.DisplayName, FRepresentation.ScalarType.DisplayName); } }

		public override int CatalogObjectID { get { return FRepresentation == null ? -1 : FRepresentation.CatalogObjectID; } }

		public override int ParentObjectID { get { return FRepresentation == null ? -1 : FRepresentation.ID; } }

		[Reference]
		internal Representation FRepresentation;
		public Representation Representation
		{
			get { return FRepresentation; }
			set
			{
				if (FRepresentation != null)
					FRepresentation.Properties.Remove(this);
				if (value != null)
					value.Properties.Add(this);
			}
		}
		
		[Reference]
		private IDataType FDataType;
		public IDataType DataType
		{
			get { return FDataType; }
			set { FDataType = value; }
		}

		private bool FIsDefaultReadAccessor;
		public bool IsDefaultReadAccessor
		{
			get { return FIsDefaultReadAccessor; }
			set { FIsDefaultReadAccessor = value; }
		}
		
		private int FReadAccessorID = -1;
		public int ReadAccessorID
		{
			get { return FReadAccessorID; }
			set { FReadAccessorID = value; }
		}

		public void LoadReadAccessorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.ReadAccessorID");
			if (LTag != Tag.None)
				FReadAccessorID = Int32.Parse(LTag.Value);
		}
		
		public void SaveReadAccessorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.ReadAccessorID", FReadAccessorID.ToString(), true);
		}
		
		public void RemoveReadAccessorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.ReadAccessorID");
		}

		public void ResolveReadAccessor(CatalogDeviceSession ASession)
		{
			if ((FReadAccessor == null) && (FReadAccessorID > -1))
				FReadAccessor = ASession.ResolveCatalogObject(FReadAccessorID) as Schema.Operator;
		}

        // ReadAccessor
		[Reference]
        private Operator FReadAccessor;
        public Operator ReadAccessor
        {
			get { return FReadAccessor; }
			set 
			{ 
				FReadAccessor = value; 
				FReadAccessorID = value == null ? -1 : value.ID;
			}
        }
        
		private bool FIsDefaultWriteAccessor;
		public bool IsDefaultWriteAccessor
		{
			get { return FIsDefaultWriteAccessor; }
			set { FIsDefaultWriteAccessor = value; }
		}
		
		private int FWriteAccessorID = -1;
		public int WriteAccessorID
		{
			get { return FWriteAccessorID; }
			set { FWriteAccessorID = value; }
		}

		public void LoadWriteAccessorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.WriteAccessorID");
			if (LTag != Tag.None)
				FWriteAccessorID = Int32.Parse(LTag.Value);
		}
		
		public void SaveWriteAccessorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.WriteAccessorID", FWriteAccessorID.ToString(), true);
		}
		
		public void RemoveWriteAccessorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.WriteAccessorID");
		}

		public void ResolveWriteAccessor(CatalogDeviceSession ASession)
		{
			if ((FWriteAccessor == null) && (FWriteAccessorID > -1))
				FWriteAccessor = ASession.ResolveCatalogObject(FWriteAccessorID) as Schema.Operator;
		}

        // WriteAccessor
		[Reference]
        private Operator FWriteAccessor;
        public Operator WriteAccessor
        {
			get { return FWriteAccessor; }
			set 
			{ 
				FWriteAccessor = value; 
				FWriteAccessorID = value == null ? -1 : value.ID;
			}
        }

		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveReadAccessorID();
				SaveWriteAccessorID();
			}
			else
			{
				RemoveObjectID();
				RemoveReadAccessorID();
				RemoveWriteAccessorID();
			}
			
			PropertyDefinition LProperty = new PropertyDefinition(Name, DataType.EmitSpecifier(AMode));
			if (!FIsDefaultReadAccessor)
				LProperty.ReadAccessorBlock = FReadAccessor.Block.EmitAccessorBlock(AMode);
			
			if (!FIsDefaultWriteAccessor)
				LProperty.WriteAccessorBlock = FWriteAccessor.Block.EmitAccessorBlock(AMode);
			
			LProperty.MetaData = MetaData == null ? null : MetaData.Copy();
			return LProperty;
		}

		public override void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			base.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			
			ReadAccessor.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			WriteAccessor.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
		}

		public bool HasExternalDependencies(Schema.ScalarType LSystemType)
		{
			if (HasDependencies())
			{
				if ((Representation != null) && (Representation.ScalarType != null) && ((Representation.ScalarType.LikeType != null) || (LSystemType != null)))
				{
					for (int LIndex = 0; LIndex < Dependencies.Count; LIndex++)
						if 
						(
							((Representation.ScalarType.LikeType != null) && (Dependencies.IDs[LIndex] != Representation.ScalarType.LikeType.ID)) || 
							((LSystemType != null) && (Dependencies.IDs[LIndex] != LSystemType.ID))
						)
							return true;
				}
				else
					return true;
			}
			
			return false;
		}
	}
	
    /// <remarks> Properties </remarks>
	public class Properties : Objects
    {
		public Properties(Representation ARepresentation) : base()
		{
			FRepresentation = ARepresentation;
		}
		
		[Reference]
		private Representation FRepresentation;
		public Representation Representation { get { return FRepresentation; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is Property))
				throw new SchemaException(SchemaException.Codes.PropertyContainer);
			base.Validate(AItem);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((Property)AItem).FRepresentation = FRepresentation;
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			((Property)AItem).FRepresentation = null;
			base.Removing(AItem, AIndex);
		}

		public new Property this[int AIndex]
		{
			get { return (Property)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public new Property this[string AName]
		{
			get { return (Property)base[AName]; }
			set { base[AName] = value; }
		}
    }

	public class Representation : Object
	{
		public Representation(int AID, string AName) : base(AID, AName) 
		{
			FProperties = new Properties(this);
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Representation"), DisplayName, FScalarType.DisplayName); } }

		public override int CatalogObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

 		public override int ParentObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

		[Reference]
		internal ScalarType FScalarType;
		public ScalarType ScalarType
		{
			get { return FScalarType; }
			set
			{
				if (FScalarType != null)
					FScalarType.Representations.Remove(this);
				if (value != null)
					value.Representations.Add(this);
			}
		}
		
		private bool FIsDefaultSelector;
		public bool IsDefaultSelector
		{
			get { return FIsDefaultSelector; }
			set { FIsDefaultSelector = value; }
		}
		
		private int FSelectorID = -1;
		public int SelectorID
		{
			get { return FSelectorID; }
			set { FSelectorID = value; }
		}
		
		public void LoadSelectorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.SelectorID");
			if (LTag != Tag.None)
				FSelectorID = Int32.Parse(LTag.Value);
		}

		public void SaveSelectorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.SelectorID", FSelectorID.ToString(), true);
		}
		
		public void RemoveSelectorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.SelectorID");
		}

		public void ResolveSelector(CatalogDeviceSession ASession)
		{
			if ((FSelector == null) && (FSelectorID > -1))
				FSelector = ASession.ResolveCatalogObject(FSelectorID) as Schema.Operator;
		}

        // Selector -- the selector operator for this representation
		[Reference]
        private Operator FSelector;
        public Operator Selector
        {
			get { return FSelector; }
			set 
			{ 
				FSelector = value; 
				FSelectorID = value == null ? -1 : value.ID;
			}
        }

		// Properties
		private Properties FProperties;
		public Properties Properties { get { return FProperties; } } 
		
		public RepresentationDefinition EmitDefinition(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveSelectorID();
				SaveIsGenerated();
				SaveGeneratorID();
			}
			else
			{
				RemoveObjectID();
				RemoveSelectorID();
				RemoveIsGenerated();
				RemoveGeneratorID();
			}
			
			RepresentationDefinition LRepresentation = new RepresentationDefinition(Name);
			foreach (Property LProperty in Properties)
				LRepresentation.Properties.Add(LProperty.EmitStatement(AMode));
			if (!IsDefaultSelector)
				LRepresentation.SelectorAccessorBlock = Selector.Block.EmitAccessorBlock(AMode);
			
			LRepresentation.MetaData = MetaData == null ? null : MetaData.Copy();
			return LRepresentation;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AlterScalarTypeStatement LStatement = new AlterScalarTypeStatement();
			LStatement.ScalarTypeName = ScalarType.Name;
			LStatement.CreateRepresentations.Add(EmitDefinition(AMode));
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			AlterScalarTypeStatement LStatement = new AlterScalarTypeStatement();
			LStatement.ScalarTypeName = ScalarType.Name;
			LStatement.DropRepresentations.Add(new DropRepresentationDefinition(Name));
			return LStatement;
		}

        public override void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			base.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			
			Selector.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
				
			foreach (Property LProperty in Properties)
				LProperty.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
        }
        
		public override Object GetObjectFromHeader(ObjectHeader AHeader)
		{
			if (FID == AHeader.ParentObjectID)
				foreach (Property LProperty in FProperties)
					if (AHeader.ID == LProperty.ID)
						return LProperty;

			return base.GetObjectFromHeader(AHeader);
		}

        private PlanNode FReadNode;
        public PlanNode ReadNode
        {
			get { return FReadNode; }
			set { FReadNode = value; }
		}
        
        private PlanNode FWriteNode;
        public PlanNode WriteNode
        {
			get { return FWriteNode; }
			set { FWriteNode = value; }
		}
        
		public bool IsNativeAccessorRepresentation(NativeAccessor ANativeAccessor, bool AExplicit)
		{
			return 
				(Name == MetaData.GetTag(MetaData, String.Format("DAE.{0}", ANativeAccessor.Name), ANativeAccessor.Name)) ||
				(
					!AExplicit &&
					(Properties.Count == 1) && 
					(Properties[0].DataType is Schema.ScalarType) && 
					(((Schema.ScalarType)Properties[0].DataType).NativeType == ANativeAccessor.NativeType)
				);
		}
		
        public bool IsNativeAccessorRepresentation(bool AExplicit)
        {
			return 
				IsNativeAccessorRepresentation(NativeAccessors.AsBoolean, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsByte, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsByteArray, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsDateTime, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsDecimal, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsDisplayString, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsException, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsGuid, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsInt16, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsInt32, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsInt64, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsString, AExplicit) ||
				IsNativeAccessorRepresentation(NativeAccessors.AsTimeSpan, AExplicit);
		}
		
		/// <summary>A represntation is persistent if it has external dependencies.</summary>
		public override bool IsPersistent { get { return HasExternalDependencies(); } }

		/// <summary>Returns true is this representation is not the system provided representation and this representation or any of its properties have dependencies on something other than the like type.</summary>
		public bool HasExternalDependencies()
		{
			if ((ScalarType != null) && ScalarType.IsDefaultConveyor && IsDefaultSelector)
				return false;
				
			Schema.ScalarType LSystemType = (ScalarType != null) && ScalarType.IsDefaultConveyor && IsDefaultSelector && (Properties.Count == 1) ? Properties[0].DataType as Schema.ScalarType : null;
			if (HasDependencies())
			{
				if ((ScalarType != null) && ((ScalarType.LikeType != null) || (LSystemType != null)))
				{
					for (int LIndex = 0; LIndex < Dependencies.Count; LIndex++)
						if 
						(
							((ScalarType.LikeType != null) && Dependencies.IDs[LIndex] != ScalarType.LikeType.ID) || 
							((LSystemType != null) && Dependencies.IDs[LIndex] != LSystemType.ID)
						)
							return true;
				}
				else
					return true;
			}
				
			for (int LIndex = 0; LIndex < FProperties.Count; LIndex++)
				if (FProperties[LIndex].HasExternalDependencies(LSystemType))
					return true;
			
			return false;
		}
	}

    /// <remarks> Representations </remarks>
	public class Representations : Objects
    {
		public Representations(ScalarType AScalarType) : base()
		{
			FScalarType = AScalarType;
		}
		
		[Reference]
		private ScalarType FScalarType;
		public ScalarType ScalarType { get { return FScalarType; } }
	
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is Representation))
				throw new SchemaException(SchemaException.Codes.RepresentationContainer);
			base.Validate(AItem);
			FScalarType.ValidateChildObjectName(AItem.Name);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((Representation)AItem).FScalarType = FScalarType;
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			((Representation)AItem).FScalarType = null;
			base.Removing(AItem, AIndex);
		}

		public new Representation this[int AIndex]
		{
			get { return (Representation)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public new Representation this[string AName]
		{
			get { return (Representation)base[AName]; }
			set { base[AName] = value; }
		}
    }
    
    public class Sort : CatalogObject
    {
		public Sort(int AID, string AName, IDataType ADataType) : base(AID, AName) 
		{
			FDataType = ADataType;
		}
		public Sort(int AID, string AName, IDataType ADataType, PlanNode ACompareNode) : base(AID, AName) 
		{
			FDataType = ADataType;
			FCompareNode = ACompareNode;
		}

		[Reference]
		private IDataType FDataType;
		public IDataType DataType { get { return FDataType; } }
		
		private bool FIsUnique;
		public bool IsUnique
		{
			get { return FIsUnique; }
			set { FIsUnique = value; }
		}
		
		private PlanNode FCompareNode;
		public PlanNode CompareNode
		{
			get { return FCompareNode; }
			set { FCompareNode = value; }
		}
		
		private string GetDataTypeDisplayName()
		{
			if (DataType is Schema.ScalarType)
				return ((Schema.ScalarType)DataType).DisplayName;
			else
				return DataType.Name;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Sort"), DisplayName, GetDataTypeDisplayName()); } }
		
		public SortDefinition EmitDefinition(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			SortDefinition LSortDefinition = new SortDefinition();
			LSortDefinition.Expression = (Expression)FCompareNode.EmitStatement(AMode);
			LSortDefinition.MetaData = MetaData == null ? null : MetaData.Copy();
			return LSortDefinition;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			CreateSortStatement LStatement = new CreateSortStatement(); 
			LStatement.ScalarTypeName = Schema.Object.EnsureRooted(DataType.Name);
			LStatement.Expression = (Expression)FCompareNode.EmitStatement(AMode);
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			return LStatement;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			return new DropSortStatement(DataType.Name, FIsUnique);
		}

		/// <summary>Returns true if the compare expression for this sort is syntactically equivalent to the compare expression of the given sort.</summary>
		public bool Equivalent(Sort ASort)
		{
			if ((FCompareNode != null) && (ASort.CompareNode != null))
				return Object.ReferenceEquals(FCompareNode, ASort.CompareNode) || (String.Compare(FCompareNode.EmitStatementAsString(), ASort.CompareNode.EmitStatementAsString()) == 0);
			else
				return false;
		}
    }
    
    public class Conversion : CatalogObject
    {
		public Conversion(int AID, string AName, ScalarType ASourceScalarType, ScalarType ATargetScalarType, Operator AOperator, bool AIsNarrowing) : base(AID, AName) 
		{
			FSourceScalarType = ASourceScalarType;
			FTargetScalarType = ATargetScalarType;
			FOperator = AOperator;
			FIsNarrowing = AIsNarrowing;
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Conversion"), IsNarrowing ? Strings.Get("SchemaObjectDescription.Narrowing") : Strings.Get("SchemaObjectDescription.Widening"), FSourceScalarType.DisplayName, FTargetScalarType.DisplayName); } }
		
		[Reference]
		private ScalarType FSourceScalarType;
		public ScalarType SourceScalarType { get { return FSourceScalarType; } }
		
		[Reference]
		private ScalarType FTargetScalarType;
		public ScalarType TargetScalarType { get { return FTargetScalarType; } }
		
		[Reference]
		private Operator FOperator;
		public Operator Operator { get { return FOperator; } }
		
		private bool FIsNarrowing = true;
		public bool IsNarrowing { get { return FIsNarrowing; } }
		
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

			CreateConversionStatement LStatement = new CreateConversionStatement();
			LStatement.SourceScalarTypeName = SourceScalarType.EmitSpecifier(AMode);
			LStatement.TargetScalarTypeName = TargetScalarType.EmitSpecifier(AMode);
			LStatement.OperatorName = new IdentifierExpression(Schema.Object.EnsureRooted(Operator.OperatorName));
			LStatement.IsNarrowing = IsNarrowing;
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			if (AMode == EmitMode.ForRemote)
			{
				if (LStatement.MetaData == null)
					LStatement.MetaData = new MetaData();
				LStatement.MetaData.Tags.AddOrUpdate("DAE.RootedIdentifier", Schema.Object.EnsureRooted(Name));
			}
			return LStatement;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropConversionStatement LStatement = new DropConversionStatement();
			LStatement.SourceScalarTypeName = SourceScalarType.EmitSpecifier(AMode);
			LStatement.TargetScalarTypeName = TargetScalarType.EmitSpecifier(AMode);
			return LStatement;
		}
		
		public override void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			if (!ATargetCatalog.Contains(Name))
			{
				ATargetCatalog.Add(this);
				base.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			}
		}
    }
    
    public class Conversions : Objects
    {
		public new Conversion this[int AIndex]
		{
			get { return (Conversion)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public new Object this[string AName]
		{
			get { return (Conversion)base[AName]; }
			set { base[AName] = value; }
		}
    }
    
	public class ScalarConversionPath : Conversions
	{
		public ScalarConversionPath() : base()
		{
			//FRolloverCount = 4;
		}
		
		public ScalarConversionPath(ScalarConversionPath APath) : base()
		{
			//FRolloverCount = 4;
			AddRange(APath);
		}

		#if USEOBJECTVALIDATE		
		protected override void Validate(Object AObject)
		{
			// Don't validate, duplicates will never be added to a path
		}
		#endif
		
		/// <summary>The initial conversion for this conversion path.</summary>
		public Schema.Conversion Conversion { get { return this[0]; } }

		/// <summary>Indicates the degree of narrowing that will occur on this conversion path.  Each narrowing conversion encountered along the path decreases the narrowing score by 1.  If no narrowing conversions occur along this path, then this number is 0. </summary>
		public int NarrowingScore;

		protected override void Adding(Schema.Object AObject, int AIndex)
		{
			base.Adding(AObject, AIndex);
			if (((Conversion)AObject).IsNarrowing)
				NarrowingScore--;
		}

		protected override void Removing(Schema.Object AObject, int AIndex)
		{
			base.Removing(AObject, AIndex);
			if (((Conversion)AObject).IsNarrowing)
				NarrowingScore++;
		}
		
		public override string ToString()
		{
			StringBuilder LBuilder = new StringBuilder();
			foreach (Conversion LConversion in this)
			{
				if (LBuilder.Length > 0)
					LBuilder.Append(", ");
				LBuilder.AppendFormat(LConversion.Name);
			}
			return LBuilder.ToString();
		}
		
		/// <summary>Returns true if this path goes through the given scalar type.</summary>
		public bool Contains(ScalarType AScalarType)
		{
			foreach (Conversion LConversion in this)
				if ((AScalarType.Equals(LConversion.SourceScalarType)) || (AScalarType.Equals(LConversion.TargetScalarType)))
					return true;
			return false;
		}
	}
	
	#if USETYPEDLIST
	public class ScalarConversionPathList : TypedList
	{
		public ScalarConversionPathList() : base(typeof(ScalarConversionPath)) {}

		public new ScalarConversionPath this[int AIndex]
		{
			get { return (ScalarConversionPath)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class ScalarConversionPathList : ValidatingBaseList<ScalarConversionPath> { }
	#endif
	
	public class ScalarConversionPaths : ScalarConversionPathList
	{
		private int FBestNarrowingScore = Int32.MinValue;
		/// <summary>Indicates the best narrowing score encountered so far.  Conversion paths with lower narrowing scores than this need not be pursued any further.</summary>
		public int BestNarrowingScore { get { return FBestNarrowingScore; } }
		
		private int FShortestLength = Int32.MaxValue;
		/// <summary>Indicates the shortest path length among paths with the current BestNarrowingScore.</summary>
		public int ShortestLength { get { return FShortestLength; } }
		
		private void FindShortestLength()
		{
			FShortestLength = Int32.MaxValue;
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].NarrowingScore == BestNarrowingScore)
					if (this[LIndex].Count < FShortestLength)
						FShortestLength = this[LIndex].Count;
		}
		
		private void ComputeBestPaths()
		{
			foreach (ScalarConversionPath LPath in this)
				if (LPath.NarrowingScore == FBestNarrowingScore)
					FBestPaths.Add(LPath);
		}
		
		private void ComputeBestPath()
		{
			FBestPath = null;
			foreach (ScalarConversionPath LPath in BestPaths)
				if (LPath.Count == FShortestLength)
					if (FBestPath != null)
					{
						FBestPath = null;
						break;
					}
					else
						FBestPath = LPath;
		}
		
		#if USETYPEDLIST
		protected override void Adding(object AValue, int AIndex)
		{
			ScalarConversionPath LConversionPath = (ScalarConversionPath)AValue;
		#else
		protected override void Adding(ScalarConversionPath LConversionPath, int AIndex)
		{
		#endif
			if (LConversionPath.NarrowingScore > BestNarrowingScore)
			{
				FBestNarrowingScore = LConversionPath.NarrowingScore;
				FindShortestLength();
			}
			else if (LConversionPath.NarrowingScore == BestNarrowingScore)
				if (LConversionPath.Count < FShortestLength)
					FShortestLength = LConversionPath.Count;
					
			FBestPaths = null;				
			FBestPath = null;
					
			//base.Adding(AValue, AIndex);
		}
		
		private ScalarConversionPathList FBestPaths;
		/// <summary>Contains the set of conversion paths with the current best narrowing score.</summary>
		public ScalarConversionPathList BestPaths 
		{ 
			get 
			{ 
				if (FBestPaths == null)
				{
					FBestPaths = new ScalarConversionPathList();
					FBestPath = null;
					ComputeBestPaths();
					ComputeBestPath();
				}
				return FBestPaths; 
			} 
		}
		
		/// <summary>Returns true if there is only one conversion path with the best narrowing score and shortest length, false otherwise.</summary>
		public bool CanConvert { get { return BestPath != null; } }
		
		private ScalarConversionPath FBestPath;
		/// <summary>Returns the single conversion path with the best narrowing score and shortest path length, null if there are multiple paths with the same narrowing score and path length.</summary>
		public ScalarConversionPath BestPath
		{ 
			get 
			{ 
				if (FBestPaths == null)
				{
					FBestPaths = new ScalarConversionPathList();
					FBestPath = null;
					ComputeBestPaths();
					ComputeBestPath();
				}
				return FBestPath;
			}
		}
	}
	
	public class ScalarConversionPathCache : System.Object
	{
		private Hashtable FPaths = new Hashtable();
		
		private class EndPoints : System.Object
		{
			public EndPoints(Schema.ScalarType ASourceType, Schema.ScalarType ATargetType)
			{
				SourceType = ASourceType;
				TargetType = ATargetType;
			}
			
			[Reference]
			public Schema.ScalarType SourceType;
	
			[Reference]
			public Schema.ScalarType TargetType;
			
			public override bool Equals(object AObject)
			{
				return (AObject is EndPoints) && ((EndPoints)AObject).SourceType.Equals(SourceType) && ((EndPoints)AObject).TargetType.Equals(TargetType);
			}
			
			public override int GetHashCode()
			{
				return SourceType.GetHashCode() ^ TargetType.GetHashCode();
			}
		}
		
		public ScalarConversionPath this[Schema.ScalarType ASourceType, Schema.ScalarType ATargetType]
		{
			get { return FPaths[new EndPoints(ASourceType, ATargetType)] as ScalarConversionPath; }
		}
		
		public void Add(Schema.ScalarType ASourceType, Schema.ScalarType ATargetType, Schema.ScalarConversionPath APath)
		{
			FPaths.Add(new EndPoints(ASourceType, ATargetType), APath);
		}
		
		/// <summary>Clears the entire conversion path cache.</summary>
		public void Clear()
		{
			FPaths.Clear();
		}
		
		/// <summary>Removes any cache entries for conversion paths which reference the specified scalar type.</summary>
		public void Clear(Schema.ScalarType AScalarType)
		{
			ArrayList LRemoveList = new ArrayList();
			foreach (DictionaryEntry LEntry in FPaths)
				if (((ScalarConversionPath)LEntry.Value).Contains(AScalarType))
					LRemoveList.Add(LEntry.Key);
					
			foreach (EndPoints LEndPoints in LRemoveList)
				FPaths.Remove(LEndPoints);
		}

		/// <summary>Removes any cache entries for conversion paths which reference the specified conversion.</summary>		
		public void Clear(Schema.Conversion AConversion)
		{
			ArrayList LRemoveList = new ArrayList();
			foreach (DictionaryEntry LEntry in FPaths)
				if (((ScalarConversionPath)LEntry.Value).Contains(AConversion))
					LRemoveList.Add(LEntry.Key);
					
			foreach (EndPoints LEndPoints in LRemoveList)
				FPaths.Remove(LEndPoints);
		}
	}
	
	public class Special : Object
    {
		public Special(int AID, string AName) : base(AID, AName) {}
		public Special(int AID, string AName, PlanNode AValueNode) : base(AID, AName)
		{
			FValueNode = AValueNode;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Special"), DisplayName, FScalarType.DisplayName); } }
		
		/// <summary>Specials are always persistent.</summary>
		public override bool IsPersistent { get { return true; } }

		public override int CatalogObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }
		
		public override int ParentObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

		[Reference]
		internal ScalarType FScalarType;
		public ScalarType ScalarType
		{
			get { return FScalarType; }
			set
			{
				if (FScalarType != null)
					FScalarType.Specials.Remove(this);
				if (value != null)
					value.Specials.Add(this);
			}
		}

		private PlanNode FValueNode;		
		public PlanNode ValueNode
		{
			get { return FValueNode; }
			set { FValueNode = value; }
		}
		
		private int FSelectorID = -1;
		public int SelectorID
		{
			get { return FSelectorID; }
			set { FSelectorID = value; }
		}
		
		public void LoadSelectorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.SelectorID");
			if (LTag != Tag.None)
				FSelectorID = Int32.Parse(LTag.Value);
		}

		public void SaveSelectorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.SelectorID", FSelectorID.ToString(), true);
		}
		
		public void RemoveSelectorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.SelectorID");
		}

		public void ResolveSelector(CatalogDeviceSession ASession)
		{
			if ((FSelector == null) && (FSelectorID > -1))
				FSelector = ASession.ResolveCatalogObject(FSelectorID) as Schema.Operator;
		}

		[Reference]
		private Operator FSelector;
		public Operator Selector
		{
			get { return FSelector; }
			set 
			{ 
				FSelector = value; 
				FSelectorID = value == null ? -1 : value.ID;
			}
		}
		
		private int FComparerID = -1;
		public int ComparerID
		{
			get { return FComparerID; }
			set { FComparerID = value; }
		}
		
		public void LoadComparerID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.ComparerID");
			if (LTag != Tag.None)
				FComparerID = Int32.Parse(LTag.Value);
		}

		public void SaveComparerID()
		{
			MetaData.Tags.AddOrUpdate("DAE.ComparerID", FComparerID.ToString(), true);
		}
		
		public void RemoveComparerID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.ComparerID");
		}

		public void ResolveComparer(CatalogDeviceSession ASession)
		{
			if ((FComparer == null) && (FComparerID > -1))
				FComparer = ASession.ResolveCatalogObject(FComparerID) as Schema.Operator;
		}

		[Reference]
		private Operator FComparer;
		public Operator Comparer
		{
			get { return FComparer; }
			set 
			{ 
				FComparer = value; 
				FComparerID = value == null ? -1 : value.ID;
			}
		}
		
		public SpecialDefinition EmitDefinition(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveIsGenerated();
				SaveGeneratorID();
				SaveSelectorID();
				SaveComparerID();
			}
			else
			{
				RemoveObjectID();
				RemoveIsGenerated();
				RemoveGeneratorID();
				RemoveSelectorID();
				RemoveComparerID();
			}
			
			SpecialDefinition LSpecial = new SpecialDefinition();
			LSpecial.Name = Name;
			LSpecial.Value = (Expression)ValueNode.EmitStatement(AMode);
			LSpecial.MetaData = MetaData == null ? null : MetaData.Copy();
			return LSpecial;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AlterScalarTypeStatement LStatement = new AlterScalarTypeStatement();
			LStatement.ScalarTypeName = ScalarType.Name;
			LStatement.CreateSpecials.Add(EmitDefinition(AMode));
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			AlterScalarTypeStatement LAlterStatement = new AlterScalarTypeStatement();
			LAlterStatement.ScalarTypeName = ScalarType.Name;
			LAlterStatement.DropSpecials.Add(new DropSpecialDefinition(Name));
			return LAlterStatement;
		}

		public override void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			base.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			
			Selector.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
			Comparer.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
		}
    }

    /// <remarks> Specials </remarks>
	public class Specials : Objects
    {
		public Specials(ScalarType AScalarType) : base()
		{
			FScalarType = AScalarType;
		}
		
		[Reference]
		private ScalarType FScalarType;
		public ScalarType ScalarType { get { return FScalarType; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is Special))
				throw new SchemaException(SchemaException.Codes.SpecialContainer);
			base.Validate(AItem);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((Special)AItem).FScalarType = FScalarType;
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			((Special)AItem).FScalarType = null;
			base.Removing(AItem, AIndex);
		}

		public new Special this[int AIndex]
		{
			get { return (Special)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public new Special this[string AName]
		{
			get { return (Special)base[AName]; }
			set { base[AName] = value; }
		}
    }
    
    public interface IScalarType : IDataType
    {
		ScalarTypeConstraints Constraints { get; }
		#if USETYPEINHERITANCE
		ScalarTypes ParentTypes { get; }
		#endif
    }
    
	/// <remarks> Implements the representation of scalar data types. </remarks>
	public class ScalarType : CatalogObject, IScalarType
    {
		// constructor
		public ScalarType(int AID, string AName) : base(AID, AName)
		{
			InternalInitialize();
		}

		private void InternalInitialize()
		{
			FIsDisposable = true;
			#if USETYPEINHERITANCE
			FParentTypes = new ScalarTypes();
			//FParentTypes.OnValidate += new SchemaObjectListEventHandler(ChildObjectValidate);
			#endif
			FRepresentations = new Representations(this);
			//FRepresentations.OnValidate += new SchemaObjectListEventHandler(ChildObjectValidate);
			FSpecials = new Specials(this);
			//FSpecials.OnValidate += new SchemaObjectListEventHandler(ChildObjectValidate);
			FConstraints = new ScalarTypeConstraints(this);
			//FConstraints.OnValidate += new SchemaObjectListEventHandler(ChildObjectValidate);
		}

		public override string[] GetRights()
		{
			return new string[]
			{
				Name + Schema.RightNames.Alter,
				Name + Schema.RightNames.Drop
			};
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.ScalarType"), DisplayName); } }

		// IsGeneric
		// Indicates whether this data type is a generic data type (i.e. table, not table{})
		private bool FIsGeneric;
		public bool IsGeneric
		{
			get { return FIsGeneric; }
			set { FIsGeneric = value; }
		}
		
		public bool IsNil { get { return false; } }
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool FIsDisposable = false;
		public bool IsDisposable
		{
			get { return FIsDisposable; }
			set { FIsDisposable = value; }
		}
		
		// IsCompound
		// Indicates whether the native representation for this type is multi-property and system-provided
		public bool IsCompound;
		
		public Schema.IRowType CompoundRowType;
		
		public void ValidateChildObjectName(string AName)
		{
			if 
			(
				(FConstraints.IndexOfName(AName) >= 0) 
					|| ((FDefault != null) && (String.Compare(FDefault.Name, AName) == 0)) 
					|| (FRepresentations.IndexOfName(AName) >= 0)
			)
				throw new SchemaException(SchemaException.Codes.DuplicateChildObjectName, AName);
		}
		
		public bool Equivalent(IDataType ADataType)
		{
			return Equals(ADataType);
		}
		
		public bool Equals(IDataType ADataType)
		{
			return (ADataType is IScalarType) && Schema.Object.NamesEqual(Name, ADataType.Name);
		}

		// Is
		public bool Is(IDataType ADataType)
		{
			if (ADataType is IGenericType)
				return true;
			else if (ADataType is IScalarType)
			{
				if (!this.Equals(ADataType))
				{
					if (ADataType.IsGeneric)
						return true;
					
					#if USETYPEINHERITANCE	
					foreach (IScalarType LParentType in FParentTypes)
						if (LParentType.Is(ADataType))
							return true;
					#endif

					return false;
				}
				return true;
			}
			return false;
		}

		// Compatible
		// Compatible is A is B or B is A
		public bool Compatible(IDataType ADataType)
		{
			return Is(ADataType) || ADataType.Is(this);
		}

		#if NATIVEROW
/*
		public int GetByteSize(object AValue)
		{
		}
*/
		#else
		// Indicates the physical size of data to be stored in the index nodes before using overflow streams
		// Must be at least Index.MinimumStaticByteSize to support overflow streams
		private int FStaticByteSize;
		public int StaticByteSize
		{
			get { return FStaticByteSize; }
			set { FStaticByteSize = value; }
		}
		#endif

        // Class Definition
        private ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
        }
        
		private bool FIsDefaultConveyor;
		public bool IsDefaultConveyor
		{
			get { return FIsDefaultConveyor; }
			set { FIsDefaultConveyor = value; }
		}
		
		public void LoadIsDefaultConveyor()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.IsDefaultConveyor");
			if (LTag != Tag.None)
				FIsDefaultConveyor = Boolean.Parse(LTag.Value);
		}

		public void SaveIsDefaultConveyor()
		{
			MetaData.Tags.AddOrUpdate("DAE.IsDefaultConveyor", FIsDefaultConveyor.ToString(), true);
		}

		public void RemoveIsDefaultConveyor()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.IsDefaultConveyor");
		}
		
		[Reference]
		private Type FNativeType;
		public Type NativeType
		{
			get { return FNativeType; }
			set { FNativeType = value; }
		}
		
		public bool HasRepresentation(NativeAccessor ANativeAccessor)
		{
			return HasRepresentation(ANativeAccessor, false);
		}
		
		public bool HasRepresentation(NativeAccessor ANativeAccessor, bool AExplicit)
		{
			return FindRepresentation(ANativeAccessor, AExplicit) != null;
		}
		
		public Schema.Representation FindRepresentation(NativeAccessor ANativeAccessor)
		{
			return FindRepresentation(ANativeAccessor, false);
		}
		
		private object FNativeRepresentationsHandle = new object(); // sync handle for the native representation cache
		private Hashtable FNativeRepresentations;
		protected Hashtable NativeRepresentations 
		{ 
			get 
			{ 
				if (FNativeRepresentations == null) 
					FNativeRepresentations = new Hashtable(); 
				return FNativeRepresentations; 
			} 
		}

		private class NativeRepresentation
		{
			public NativeRepresentation(Representation ARepresentation, bool AExplicit)
			{
				Representation = ARepresentation;
				Explicit = AExplicit;
			}
			
			[Reference]
			public Representation Representation;
			public bool Explicit;
		}
		
		public void ResetNativeRepresentationCache()
		{
			lock (FNativeRepresentationsHandle)
			{
				FNativeRepresentations = null;
			}
		}
		
		private NativeRepresentation FindNativeRepresentation(NativeAccessor ANativeAccessor)
		{
			string LRepresentationName = MetaData.GetTag(MetaData, String.Format("DAE.{0}", ANativeAccessor.Name), String.Empty);
			int LRepresentationIndex = FRepresentations.IndexOf(LRepresentationName);
			if (LRepresentationIndex >= 0)
				return new NativeRepresentation(FRepresentations[LRepresentationIndex], true);

			foreach (Schema.Representation LRepresentation in FRepresentations)
				if ((LRepresentation.Properties.Count == 1) && (LRepresentation.Properties[0].DataType is Schema.ScalarType) && (((Schema.ScalarType)LRepresentation.Properties[0].DataType).NativeType == ANativeAccessor.NativeType))
					return new NativeRepresentation(LRepresentation, false);
			
			return new NativeRepresentation(null, false);
		}
		
		public Schema.Representation FindRepresentation(NativeAccessor ANativeAccessor, bool AExplicit)
		{
			NativeRepresentation LNativeRepresentation;

			lock (FNativeRepresentationsHandle)
			{
				LNativeRepresentation = (NativeRepresentation)NativeRepresentations[ANativeAccessor.Name];
			
				if (LNativeRepresentation == null)
				{
					LNativeRepresentation = FindNativeRepresentation(ANativeAccessor);
					NativeRepresentations.Add(ANativeAccessor.Name, LNativeRepresentation);
				}
			}

			if (AExplicit && !LNativeRepresentation.Explicit)
				return null;
				
			return LNativeRepresentation.Representation;			
		}
		
		public Schema.Representation GetRepresentation(NativeAccessor ANativeAccessor)
		{
			Schema.Representation LRepresentation = FindRepresentation(ANativeAccessor);
			if (LRepresentation == null)
				throw new SchemaException(SchemaException.Codes.UnableToLocateConversionRepresentation, Name, ANativeAccessor.Name);
			return LRepresentation;
		}
		
		#if USETYPEINHERITANCE
        // ParentTypes
        private ScalarTypes FParentTypes;
		public ScalarTypes ParentTypes { get { return FParentTypes; } }
		#endif
		
		// LikeType
		[Reference]
		private ScalarType FLikeType;
		public ScalarType LikeType 
		{ 
			get { return FLikeType; } 
			set { FLikeType = value; }
		}
		
        // Representations
        private Representations FRepresentations;
        public Representations Representations { get { return FRepresentations; } }

		// Constraints
		private ScalarTypeConstraints FConstraints;
		public ScalarTypeConstraints Constraints { get { return FConstraints; } }

        // Specials
        private Specials FSpecials;
        public Specials Specials { get { return FSpecials; } }
        
		private int FIsSpecialOperatorID = -1;
		public int IsSpecialOperatorID
		{
			get { return FIsSpecialOperatorID; }
			set { FIsSpecialOperatorID = value; }
		}
		
		public void LoadIsSpecialOperatorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.IsSpecialOperatorID");
			if (LTag != Tag.None)
				FIsSpecialOperatorID = Int32.Parse(LTag.Value);
		}

		public void SaveIsSpecialOperatorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.IsSpecialOperatorID", FIsSpecialOperatorID.ToString(), true);
		}
		
		public void RemoveIsSpecialOperatorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.IsSpecialOperatorID");
		}
		
		public void ResolveIsSpecialOperator(CatalogDeviceSession ASession)
		{
			if ((FIsSpecialOperator == null) && (FIsSpecialOperatorID > -1))
				FIsSpecialOperator = ASession.ResolveCatalogObject(FIsSpecialOperatorID) as Schema.Operator;
		}

        // IsSpecialOperator
		[Reference]
        private Operator FIsSpecialOperator;
        public Operator IsSpecialOperator
        {
			get { return FIsSpecialOperator; }
			set 
			{ 
				FIsSpecialOperator = value; 
				FIsSpecialOperatorID = value == null ? -1 : value.ID;
			}
        }
        
		private int FEqualityOperatorID = -1;
		public int EqualityOperatorID
		{
			get { return FEqualityOperatorID; }
			set { FEqualityOperatorID = value; }
		}
		
		public void LoadEqualityOperatorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.EqualityOperatorID");
			if (LTag != Tag.None)
				FEqualityOperatorID = Int32.Parse(LTag.Value);
		}

		public void SaveEqualityOperatorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.EqualityOperatorID", FEqualityOperatorID.ToString(), true);
		}

		public void RemoveEqualityOperatorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.EqualityOperatorID");
		}
		
		public void ResolveEqualityOperator(CatalogDeviceSession ASession)
		{
			if ((FEqualityOperator == null) && (FEqualityOperatorID > -1))
				FEqualityOperator = ASession.ResolveCatalogObject(FEqualityOperatorID) as Schema.Operator;
		}

        // EqualityOperator
		[Reference]
        private Operator FEqualityOperator;
        public Operator EqualityOperator
        {
			get { return FEqualityOperator; }
			set 
			{ 
				FEqualityOperator = value; 
				FEqualityOperatorID = value == null ? -1 : value.ID;
			}
		}

		private int FComparisonOperatorID = -1;
		public int ComparisonOperatorID
		{
			get { return FComparisonOperatorID; }
			set { FComparisonOperatorID = value; }
		}
		
		public void LoadComparisonOperatorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.ComparisonOperatorID");
			if (LTag != Tag.None)
				FComparisonOperatorID = Int32.Parse(LTag.Value);
		}

		public void SaveComparisonOperatorID()
		{
			MetaData.Tags.AddOrUpdate("DAE.ComparisonOperatorID", FComparisonOperatorID.ToString(), true);
		}

		public void RemoveComparisonOperatorID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.ComparisonOperatorID");
		}
		
		public void ResolveComparisonOperator(CatalogDeviceSession ASession)
		{
			if ((FComparisonOperator == null) && (FComparisonOperatorID > -1))
				FComparisonOperator = ASession.ResolveCatalogObject(FComparisonOperatorID) as Schema.Operator;
		}

        // ComparisonOperator
		[Reference]
        private Operator FComparisonOperator;
        public Operator ComparisonOperator
        {
			get { return FComparisonOperator; }
			set 
			{ 
				FComparisonOperator = value; 
				FComparisonOperatorID = value == null ? -1 : value.ID;
			}
        }

		// Default
		private ScalarTypeDefault FDefault;
		public ScalarTypeDefault Default
		{
			get { return FDefault; }
			set
			{
				if (FDefault != value)
				{
					ScalarTypeDefault FOldDefault = FDefault;
					FDefault = null;
					try
					{
						if (value != null)
							ValidateChildObjectName(value.Name);
						if (FOldDefault != null)
							FOldDefault.FScalarType = null;
						FDefault = value;
						if (FDefault != null)
							FDefault.FScalarType = this;
					}
					catch
					{
						FDefault = FOldDefault;
						throw;
					}
				}
			}
		}
		
		private int FSortID = -1;
		public int SortID
		{
			get { return FSortID; }
			set { FSortID = value; }
		}
		
		public void LoadSortID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.SortID");
			if (LTag != Tag.None)
			FSortID = Int32.Parse(LTag.Value);
		}

		public void SaveSortID()
		{
			MetaData.Tags.AddOrUpdate("DAE.SortID", FSortID.ToString(), true);
		}
		
		public void RemoveSortID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.SortID");
		}
		
		[Reference]
		private Sort FSort;
		public Sort Sort
		{
			get { return FSort; }
			set 
			{ 
				FSort = value; 
				FSortID = value == null ? -1 : value.ID;
			}
		}
		
		private int FUniqueSortID = -1;
		public int UniqueSortID
		{
			get { return FUniqueSortID; }
			set { FUniqueSortID = value; }
		}
		
		public void LoadUniqueSortID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.UniqueSortID");
			if (LTag != Tag.None)
				FUniqueSortID = Int32.Parse(LTag.Value);
		}

		public void SaveUniqueSortID()
		{
			MetaData.Tags.AddOrUpdate("DAE.UniqueSortID", FUniqueSortID.ToString(), true);
		}

		public void RemoveUniqueSortID()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.UniqueSortID");
		}
		
		[Reference]
		private Sort FUniqueSort;
		public Sort UniqueSort
		{
			get { return FUniqueSort; }
			set 
			{ 
				FUniqueSort = value; 
				FUniqueSortID = value == null ? -1 : value.ID;
			}
		}

		// HasHandlers
		public bool HasHandlers()
		{
			return (FEventHandlers != null) && (FEventHandlers.Count > 0);
		}
		
		public bool HasHandlers(EventType AEventType)
		{
			return (FEventHandlers != null) && FEventHandlers.HasHandlers(AEventType);
		}
		
		// EventHandlers
		private ScalarTypeEventHandlers FEventHandlers;
		public ScalarTypeEventHandlers EventHandlers 
		{ 
			get 
			{ 
				if (FEventHandlers == null)
					FEventHandlers = new ScalarTypeEventHandlers(this);
				return FEventHandlers; 
			} 
		}

		#if USETYPEINHERITANCE	
		// ExplicitCastOperators
		private Objects FExplicitCastOperators = new Objects();
		public Objects ExplicitCastOperators { get { return FExplicitCastOperators; } }
		#endif

		// ImplicitConversions
		private Conversions FImplicitConversions = new Conversions();
		public Conversions ImplicitConversions { get { return FImplicitConversions; } }
		
		#if USEPROPOSABLEEVENTS
        // OnValidateValue
        public event ColumnValidateHandler OnValidateValue;
        public virtual void DoValidateValue(IServerSession ASession, object AValue)
        {
            if (OnValidateValue != null)
                OnValidateValue(this, ASession, AValue);
        }

        // OnChangeValue
        public event ColumnChangeHandler OnChangeValue;
        public virtual bool DoChangeValue(IServerSession ASession, ref object AValue)
        {
            bool LChanged = false;
            if (OnChangeValue != null)
                OnChangeValue(this, ASession, ref AValue, out LChanged);
            return LChanged;
        }

        // OnDefaultValue
        public event ColumnChangeHandler OnDefaultValue;
        public virtual bool DoDefaultValue(IServerSession ASession, ref object AValue)
        {
            bool LChanged = false;
            if (OnDefaultValue != null)
                OnDefaultValue(this, ASession, ref AValue, out LChanged);
            return LChanged;
        }
        #endif

        public override void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			if (!ATargetCatalog.Contains(Name))
			{
				base.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
				
				ATargetCatalog.Add(this);
			
				if ((Default != null) && ((AMode != EmitMode.ForRemote) || Default.IsRemotable))
					Default.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
					
				foreach (Constraint LConstraint in Constraints)
					if ((AMode != EmitMode.ForRemote) || LConstraint.IsRemotable)
						LConstraint.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
						
				foreach (Representation LRepresentation in Representations)
					LRepresentation.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
					
				if (FIsSpecialOperator != null)
					FIsSpecialOperator.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);

				foreach (Special LSpecial in Specials)
					LSpecial.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
					
			}
        }

        public override void IncludeHandlers(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			if (FEventHandlers != null)
				foreach (EventHandler LHandler in FEventHandlers)
					if ((AMode != EmitMode.ForRemote) || LHandler.IsRemotable)
						LHandler.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
        }
        
        public override Statement EmitStatement(EmitMode AMode)
        {
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveIsSpecialOperatorID();
				SaveEqualityOperatorID();
				SaveComparisonOperatorID();
				SaveSortID();
				SaveUniqueSortID();
			}
			else
			{
				RemoveObjectID();
				RemoveIsSpecialOperatorID();
				RemoveEqualityOperatorID();
				RemoveComparisonOperatorID();
				RemoveSortID();
				RemoveUniqueSortID();
			}

			CreateScalarTypeStatement LStatement = new CreateScalarTypeStatement();
			LStatement.ScalarTypeName = Schema.Object.EnsureRooted(Name);
			#if USETYPEINHERITANCE
			foreach (ScalarType LParentType in ParentTypes)
				LStatement.ParentScalarTypes.Add(new ScalarTypeNameDefinition(LParentType.Name));
			#endif
			
			if (LikeType != null)
				LStatement.LikeScalarTypeName = LikeType.Name;

			foreach (Representation LRepresentation in Representations)
				if ((!LRepresentation.IsGenerated || (AMode == EmitMode.ForStorage)) && !LRepresentation.HasExternalDependencies())
					LStatement.Representations.Add(LRepresentation.EmitDefinition(AMode));
					
			// specials, representations w/dependencies, constraints and defaults are emitted by the catalog as alter statements because they may have dependencies on operators that are undefined when the create scalar type statement is executed.

			LStatement.ClassDefinition = IsDefaultConveyor || (FClassDefinition == null) ? null : (ClassDefinition)FClassDefinition.Clone();
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			return LStatement;
        }
        
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropScalarTypeStatement LStatement = new DropScalarTypeStatement();
			LStatement.ObjectName = Name;
			return LStatement;
		}

        public TypeSpecifier EmitSpecifier(EmitMode AMode)
        {
			return new ScalarTypeSpecifier(Name);
        }
        
		public override Object GetObjectFromHeader(ObjectHeader AHeader)
		{
			switch (AHeader.ObjectType)
			{
				case "ScalarTypeConstraint" :
					foreach (Constraint LConstraint in Constraints)
						if (AHeader.ID == LConstraint.ID)
							return LConstraint;
				break;
						
				case "Representation" :
					foreach (Representation LRepresentation in Representations)
						if (AHeader.ID == LRepresentation.ID)
							return LRepresentation;
				break;
				
				case "Property" :
					foreach (Representation LRepresentation in Representations)
						if (AHeader.ParentObjectID == LRepresentation.ID)
							return LRepresentation.GetObjectFromHeader(AHeader);
				break;
				
				case "Special" :
					foreach (Special LSpecial in FSpecials)
						if (AHeader.ID == LSpecial.ID)
							return LSpecial;
				break;
						
				case "ScalarTypeDefault" :
					if ((FDefault != null) && (AHeader.ID == FDefault.ID))
						return FDefault;
				break;
			}
			
			return base.GetObjectFromHeader(AHeader);
		}

		public void ResolveGeneratedDependents(CatalogDeviceSession ASession)
		{
			if (FRepresentations.Count > 0)
			{
				ResolveEqualityOperator(ASession);
				ResolveComparisonOperator(ASession);
				ResolveIsSpecialOperator(ASession);
				
				foreach (Schema.Representation LRepresentation in FRepresentations)
				{
					LRepresentation.ResolveSelector(ASession);
					foreach (Schema.Property LProperty in LRepresentation.Properties)
					{
						LProperty.ResolveReadAccessor(ASession);
						LProperty.ResolveWriteAccessor(ASession);
					}
				}
				
				foreach (Schema.Special LSpecial in FSpecials)
				{
					LSpecial.ResolveSelector(ASession);
					LSpecial.ResolveComparer(ASession);
				}
			}
		}
	}

    /// <remarks> ScalarTypes </remarks>
	public class ScalarTypes : Objects
    {
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is ScalarType))
				throw new SchemaException(SchemaException.Codes.ScalarTypeContainer);
			base.Validate(AItem);
		}
		#endif

		public new ScalarType this[int AIndex]
		{
			get { return (ScalarType)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public new ScalarType this[string AName]
		{
			get { return (ScalarType)base[AName]; }
			set { base[AName] = value; }
		}
    }
} 