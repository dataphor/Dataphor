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
using Alphora.Dataphor.DAE.Device.Catalog;
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
		public Reference(string name) : base(name) {}
		public Reference(int iD, string name) : base(iD, name) {}
		public Reference(int iD, string name, MetaData metaData) : base(iD, name)
		{
			MetaData = metaData;
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
		[Reference]
		private TableVar _sourceTable;
		public TableVar SourceTable
		{
			get { return _sourceTable; }
			set { _sourceTable = value; }
		}

		// TargetTable
		[Reference]
		private TableVar _targetTable;
		public TableVar TargetTable
		{
			get { return _targetTable; }
			set { _targetTable = value; }
		}
		
		// SourceKey
		private JoinKey _sourceKey = new JoinKey();
		public JoinKey SourceKey { get { return _sourceKey; } }
		
		// TargetKey		
		private JoinKey _targetKey = new JoinKey();
		public JoinKey TargetKey { get { return _targetKey; } }
		
		// Enforced
		private bool _enforced = true;
		/// <summary>Indicates whether or not the constraint is enforced.</summary>
		/// <remarks>Set by the DAE.Enforced tag when the constraint is created.</remarks>
		public bool Enforced
		{
			get { return _enforced; }
			set { _enforced = value; }
		}
		
		// ParentReference		
		[Reference]
		private Schema.Reference _parentReference;
		/// <summary>For derived references, this is the reference from which this reference was derived.</summary>
		public Schema.Reference ParentReference
		{
			get { return _parentReference; }
			set { _parentReference = value; }
		}
		
		private bool _isExcluded;
		/// <summary>True if this reference has been excluded by some operation in the expression.</summary>
		/// <remarks>
		/// Excluded references should not be considered as an inferred reference, but need to be tracked so
		/// that the exclusion algorithm for joins works properly through multiple joins. Note that IsExcluded
		/// is only valid for derived references.
		/// </remarks>
		public bool IsExcluded 
		{ 
			get { return _isExcluded; } 
			set { _isExcluded = value; }
		}
		
		// IsDerived
		public bool IsDerived { get { return _parentReference != null; } }
		
		/// <summary>For derived references, this is the base reference from which this reference was derived. Otherwise, this is the reference name.</summary>
		public string OriginatingReferenceName()
		{
			if (IsDerived)
				return ParentReference.OriginatingReferenceName();
			return Name;
		}
		
		// UpdateReferenceAction
		private ReferenceAction _updateReferenceAction;
		public ReferenceAction UpdateReferenceAction
		{
			get { return _updateReferenceAction; }
			set { _updateReferenceAction = value; }
		}
		
		// UpdateReferenceExpressions
		private Expressions _updateReferenceExpressions = new Expressions();
		public Expressions UpdateReferenceExpressions { get { return _updateReferenceExpressions; } }
		
		// DeleteReferenceAction
		private ReferenceAction _deleteReferenceAction;
		public ReferenceAction DeleteReferenceAction
		{
			get { return _deleteReferenceAction; }
			set { _deleteReferenceAction = value; }
		}
		
		// DeleteReferenceExpressions
		private Expressions _deleteReferenceExpressions = new Expressions();
		public Expressions DeleteReferenceExpressions { get { return _deleteReferenceExpressions; } } 
		
        // The constraints used to enforce this reference (if necessary)
		[Reference]
        private CatalogConstraint _catalogConstraint;
        public CatalogConstraint CatalogConstraint
        {
			get { return _catalogConstraint; }
			set { _catalogConstraint = value; }
        }
        
		[Reference]
        private TransitionConstraint _sourceConstraint;
        public TransitionConstraint SourceConstraint
        {
			get { return _sourceConstraint; }
			set { _sourceConstraint = value; }
		}
        
		[Reference]
        private TransitionConstraint _targetConstraint;
        public TransitionConstraint TargetConstraint
        {
			get { return _targetConstraint; }
			set { _targetConstraint = value; }
        }
        
		[Reference]
        private EventHandler _updateHandler;
        public EventHandler UpdateHandler
        {
			get { return _updateHandler; }
			set { _updateHandler = value; }
        }
        
		[Reference]
        private EventHandler _deleteHandler;
        public EventHandler DeleteHandler
        {
			get { return _deleteHandler; }
			set { _deleteHandler = value; }
        }
        
		public override void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			if (!targetCatalog.Contains(Name))
			{
				base.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
				targetCatalog.Add(this);
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			CreateReferenceStatement statement = new CreateReferenceStatement();
			if (SessionObjectName != null)
			{
				statement.IsSession = true;
				statement.ReferenceName = Schema.Object.EnsureRooted(SessionObjectName);
			}
			else
				statement.ReferenceName = Schema.Object.EnsureRooted(Name);
			statement.TableVarName = SourceTable.Name;
			foreach (TableVarColumn column in SourceKey.Columns)
				statement.Columns.Add(new ReferenceColumnDefinition(column.Name));
			statement.MetaData = MetaData == null ? new MetaData() : MetaData.Copy();
			if (SessionObjectName != null)
				statement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", Name, true);
			statement.ReferencesDefinition = new ReferencesDefinition();
			statement.ReferencesDefinition.TableVarName = TargetTable.Name;
			foreach (TableVarColumn column in TargetKey.Columns)
				statement.ReferencesDefinition.Columns.Add(new ReferenceColumnDefinition(column.Name));
			statement.ReferencesDefinition.UpdateReferenceAction = UpdateReferenceAction;
			statement.ReferencesDefinition.UpdateReferenceExpressions.AddRange(UpdateReferenceExpressions);
			statement.ReferencesDefinition.DeleteReferenceAction = DeleteReferenceAction;
			statement.ReferencesDefinition.DeleteReferenceExpressions.AddRange(DeleteReferenceExpressions);
			return statement;
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropReferenceStatement statement = new DropReferenceStatement();
			statement.ReferenceName = Name;
			return statement;
		}
	}
    
    /// <remarks> References </remarks>
	public class References : Objects
    {
		#if USEOBJECTVALIDATE
		protected override void Validate(Object item)
		{
			if (!(item is Reference))
				throw new SchemaException(SchemaException.Codes.ReferenceContainer);
			base.Validate(item);
		}
		#endif

		public new Reference this[int index]
		{
			get { return (Reference)base[index]; }
			set { base[index] = value; }
		}

		public new Reference this[string name]
		{
			get { return (Reference)base[name]; }
			set { base[name] = value; }
		}
		
		public int IndexOfOriginatingReference(string originatingReferenceName)
		{
			for (int index = 0; index < Count; index++)
				if (Schema.Object.NamesEqual(this[index].OriginatingReferenceName(), originatingReferenceName))
					return index;
			return -1;
		}
		
		public bool ContainsOriginatingReference(string originatingReferenceName)
		{
			return IndexOfOriginatingReference(originatingReferenceName) >= 0;
		}
		
		public bool ContainsSourceReference(Schema.Reference reference)
		{
			foreach (Schema.Reference localReference in this)
				if ((localReference.OriginatingReferenceName() == reference.OriginatingReferenceName()) && localReference.SourceKey.Equals(reference.SourceKey))
					return true;
			return false;
		}
		
		public bool ContainsTargetReference(Schema.Reference reference)
		{
			foreach (Schema.Reference localReference in this)
				if ((localReference.OriginatingReferenceName() == reference.OriginatingReferenceName()) && localReference.TargetKey.Equals(reference.TargetKey))
					return true;
			return false;
		}
		
		public int AddInCreationOrder(Reference reference)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].ID > reference.ID)
				{
					Insert(index, reference);
					return index;
				}

			return Add(reference);
		}
    }
}