/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
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
	// Reference	
	public class Reference : CatalogObject
	{
		// constructor
		public Reference(string AName) : base(AName) {}
		public Reference(int AID, string AName) : base(AID, AName) {}
		public Reference(int AID, string AName, MetaData AMetaData) : base(AID, AName)
		{
			MetaData = AMetaData;
		}

		public override string[] GetRights()
		{
			return new string[]
			{
				Name + Schema.RightNames.Alter,
				Name + Schema.RightNames.Drop
			};
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Reference"), DisplayName); } }

		// SourceTable
		private TableVar FSourceTable;
		public TableVar SourceTable
		{
			get { return FSourceTable; }
			set { FSourceTable = value; }
		}

		// TargetTable
		private TableVar FTargetTable;
		public TableVar TargetTable
		{
			get { return FTargetTable; }
			set { FTargetTable = value; }
		}
		
		// SourceKey
		private JoinKey FSourceKey = new JoinKey();
		public JoinKey SourceKey { get { return FSourceKey; } }
		
		// TargetKey		
		private JoinKey FTargetKey = new JoinKey();
		public JoinKey TargetKey { get { return FTargetKey; } }
		
		// Enforced
		private bool FEnforced = true;
		/// <summary>Indicates whether or not the constraint is enforced.</summary>
		/// <remarks>Set by the DAE.Enforced tag when the constraint is created.</remarks>
		public bool Enforced
		{
			get { return FEnforced; }
			set { FEnforced = value; }
		}
		
		// ParentReference		
		private Schema.Reference FParentReference;
		/// <summary>For derived references, this is the reference from which this reference was derived.</summary>
		public Schema.Reference ParentReference
		{
			get { return FParentReference; }
			set { FParentReference = value; }
		}
		
		private bool FIsExcluded;
		/// <summary>True if this reference has been excluded by some operation in the expression.</summary>
		/// <remarks>
		/// Excluded references should not be considered as an inferred reference, but need to be tracked so
		/// that the exclusion algorithm for joins works properly through multiple joins. Note that IsExcluded
		/// is only valid for derived references.
		/// </remarks>
		public bool IsExcluded 
		{ 
			get { return FIsExcluded; } 
			set { FIsExcluded = value; }
		}
		
		// IsDerived
		public bool IsDerived { get { return FParentReference != null; } }
		
		/// <summary>For derived references, this is the base reference from which this reference was derived. Otherwise, this is the reference name.</summary>
		public string OriginatingReferenceName()
		{
			if (IsDerived)
				return ParentReference.OriginatingReferenceName();
			return Name;
		}
		
		// UpdateReferenceAction
		private ReferenceAction FUpdateReferenceAction;
		public ReferenceAction UpdateReferenceAction
		{
			get { return FUpdateReferenceAction; }
			set { FUpdateReferenceAction = value; }
		}
		
		// UpdateReferenceExpressions
		private Expressions FUpdateReferenceExpressions = new Expressions();
		public Expressions UpdateReferenceExpressions { get { return FUpdateReferenceExpressions; } }
		
		// DeleteReferenceAction
		private ReferenceAction FDeleteReferenceAction;
		public ReferenceAction DeleteReferenceAction
		{
			get { return FDeleteReferenceAction; }
			set { FDeleteReferenceAction = value; }
		}
		
		// DeleteReferenceExpressions
		private Expressions FDeleteReferenceExpressions = new Expressions();
		public Expressions DeleteReferenceExpressions { get { return FDeleteReferenceExpressions; } } 
		
        // The constraints used to enforce this reference (if necessary)
        private CatalogConstraint FCatalogConstraint;
        public CatalogConstraint CatalogConstraint
        {
			get { return FCatalogConstraint; }
			set { FCatalogConstraint = value; }
        }
        
        private TransitionConstraint FSourceConstraint;
        public TransitionConstraint SourceConstraint
        {
			get { return FSourceConstraint; }
			set { FSourceConstraint = value; }
		}
        
        private TransitionConstraint FTargetConstraint;
        public TransitionConstraint TargetConstraint
        {
			get { return FTargetConstraint; }
			set { FTargetConstraint = value; }
        }
        
        private EventHandler FUpdateHandler;
        public EventHandler UpdateHandler
        {
			get { return FUpdateHandler; }
			set { FUpdateHandler = value; }
        }
        
        private EventHandler FDeleteHandler;
        public EventHandler DeleteHandler
        {
			get { return FDeleteHandler; }
			set { FDeleteHandler = value; }
        }
        
		public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			if (!ATargetCatalog.Contains(Name))
			{
				base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
				ATargetCatalog.Add(this);
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			CreateReferenceStatement LStatement = new CreateReferenceStatement();
			if (SessionObjectName != null)
			{
				LStatement.IsSession = true;
				LStatement.ReferenceName = Schema.Object.EnsureRooted(SessionObjectName);
			}
			else
				LStatement.ReferenceName = Schema.Object.EnsureRooted(Name);
			LStatement.TableVarName = SourceTable.Name;
			foreach (TableVarColumn LColumn in SourceKey.Columns)
				LStatement.Columns.Add(new ReferenceColumnDefinition(LColumn.Name));
			LStatement.MetaData = MetaData == null ? new MetaData() : MetaData.Copy();
			if (SessionObjectName != null)
				LStatement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", Name, true);
			LStatement.ReferencesDefinition = new ReferencesDefinition();
			LStatement.ReferencesDefinition.TableVarName = TargetTable.Name;
			foreach (TableVarColumn LColumn in TargetKey.Columns)
				LStatement.ReferencesDefinition.Columns.Add(new ReferenceColumnDefinition(LColumn.Name));
			LStatement.ReferencesDefinition.UpdateReferenceAction = UpdateReferenceAction;
			LStatement.ReferencesDefinition.UpdateReferenceExpressions.AddRange(UpdateReferenceExpressions);
			LStatement.ReferencesDefinition.DeleteReferenceAction = DeleteReferenceAction;
			LStatement.ReferencesDefinition.DeleteReferenceExpressions.AddRange(DeleteReferenceExpressions);
			return LStatement;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropReferenceStatement LStatement = new DropReferenceStatement();
			LStatement.ReferenceName = Name;
			return LStatement;
		}
	}
    
    /// <remarks> References </remarks>
	public class References : Objects
    {
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is Reference))
				throw new SchemaException(SchemaException.Codes.ReferenceContainer);
			base.Validate(AItem);
		}
		#endif

		public new Reference this[int AIndex]
		{
			get { return (Reference)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public new Reference this[string AName]
		{
			get { return (Reference)base[AName]; }
			set { base[AName] = value; }
		}
		
		public int IndexOfOriginatingReference(string AOriginatingReferenceName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (Schema.Object.NamesEqual(this[LIndex].OriginatingReferenceName(), AOriginatingReferenceName))
					return LIndex;
			return -1;
		}
		
		public bool ContainsOriginatingReference(string AOriginatingReferenceName)
		{
			return IndexOfOriginatingReference(AOriginatingReferenceName) >= 0;
		}
		
		public bool ContainsSourceReference(Schema.Reference AReference)
		{
			foreach (Schema.Reference LReference in this)
				if ((LReference.OriginatingReferenceName() == AReference.OriginatingReferenceName()) && LReference.SourceKey.Equals(AReference.SourceKey))
					return true;
			return false;
		}
		
		public bool ContainsTargetReference(Schema.Reference AReference)
		{
			foreach (Schema.Reference LReference in this)
				if ((LReference.OriginatingReferenceName() == AReference.OriginatingReferenceName()) && LReference.TargetKey.Equals(AReference.TargetKey))
					return true;
			return false;
		}
		
		public int AddInCreationOrder(Reference AReference)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].ID > AReference.ID)
				{
					Insert(LIndex, AReference);
					return LIndex;
				}

			return Add(AReference);
		}
    }
}