/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Schema
{
	// Delegate classes for Table, Row and Column Trigger and Validation events
	#if USEVALIDATIONEVENTS
    public delegate void RowValidateHandler(object ASender, IServerSession ASession, Row ARow, string AColumnName);
    public delegate void RowChangeHandler(object ASender, IServerSession ASession, Row ARow, string AColumnName, out bool AChanged);
    public delegate void RowInsertHandler(object ASender, IServerSession ASession, Row ARow);
    public delegate void RowUpdateHandler(object ASender, IServerSession ASession, Row AOldRow, Row ANewRow);
    public delegate void RowDeleteHandler(object ASender, IServerSession ASession, Row ARow);
    public delegate void ColumnValidateHandler(object ASender, IServerSession ASession, object AValue);
    public delegate void ColumnChangeHandler(object ASender, IServerSession ASession, ref object AValue, out bool AChanged);
    #endif
    
    /// <summary>Maintains a set of objects by ID, with the ability to resolve a reference if necessary, caching that reference.</summary>
    /// <remarks>This class is used to track dependencies for catalog objects while they are in the cache.</remarks>
    public class ObjectList : System.Object, ICollection<int>
    {
		private List<int> _iDs = new List<int>(0);
		/// <summary>Provides access to the IDs of the objects in the list, by index.</summary>
		public List<int> IDs { get { return _iDs; } }
		
		private List<Schema.Object> _objects = new List<Schema.Object>(0);
		/// <summary>Provides access to the references to the objects in the list, by index.</summary>
		/// <remarks>Note that the object reference may be null if it has not yet been resolved to an actual object reference in the catalog.</remarks>
		public List<Schema.Object> Objects { get { return _objects; } }

		/// <summary>Ensures that the given object ID and object reference is in the list. AObject may be a null reference.</summary>		
		public void Ensure(int iD, Schema.Object objectValue)
		{
			if (!_iDs.Contains(iD))
			{
				_iDs.Add(iD);
				_objects.Add(objectValue);
			}
		}

		/// <summary>Ensures that the given ID is in the list, adding it with a null reference if necessary.</summary>
		public void Ensure(int iD)
		{
			Ensure(iD, null);
		}

		/// <summary>Ensures that the given object is in the list by ID, adding the reference as well. AObject may not be a null reference.</summary>		
		public void Ensure(Schema.Object objectValue)
		{
			Ensure(objectValue.ID, objectValue);
		}
		
		/// <summary>Adds the given ID and object reference to the list. If the list already has an entry for AID, an exception is raised. AObject may be a null reference.</summary>
		public void Add(int iD, Schema.Object objectValue)
		{
			if (_iDs.Contains(iD))
				throw new SchemaException(SchemaException.Codes.DuplicateObject, ErrorSeverity.System, iD);
			_iDs.Add(iD);
			_objects.Add(objectValue);
		}
		
		/// <summary>Adds the given object to the list by ID, adding the reference as well. AObject may not be a null reference.</summary>
		public void Add(Schema.Object objectValue)
		{
			Add(objectValue.ID, objectValue);
		}
		
		/// <summary>Retrieves the object reference for the object at the given index in this list.</summary>
		/// <remarks>
		/// If the object reference is already available in the list, it is used. 
		/// Otherwise, the given catalog device session is used to resolve the object 
		/// reference by ID.
		/// </remarks>
		public Schema.Object ResolveObject(CatalogDeviceSession session, int index)
		{
			Schema.Object objectValue = _objects[index];
			if (objectValue == null)
			{
				objectValue = session.ResolveObject(_iDs[index]);
				_objects[index] = objectValue;
			}
			
			return objectValue;
		}
		
		/// <summary>Copies the contents of this object list to AObjectList.</summary>
		public void CopyTo(ObjectList objectList)
		{
			for (int index = 0; index < Count; index++)
				objectList.Add(_iDs[index], _objects[index]);
		}
		
		#region ICollection<int> Members

		/// <summary>Adds the given ID to the list with a null reference for the object. If the ID is already in the list, an exception is raised.</summary>
		public void Add(int iD)
		{
			Add(iD, null);
		}

		/// <summary>Clears the IDs and object references for this object list.</summary>		
		public void Clear()
		{
			_iDs.Clear();
			_objects.Clear();
		}

		/// <summary>Returns true if the list contains the given ID, false otherwise.</summary>
		public bool Contains(int iD)
		{
			return _iDs.Contains(iD);
		}

		/// <summary>This method is not implemented, calling it will throw an exception.</summary>
		public void CopyTo(int[] array, int arrayIndex)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>Returns the number of items in the list.</summary>
		public int Count
		{
			get { return _iDs.Count; }
		}

		/// <summary>Always returns false.</summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>Removes the given ID and its associated object reference, if any, from the list. Returns true if the object was in the list, false otherwise.</summary>
		public bool Remove(int item)
		{
			int index = _iDs.IndexOf(item);
			if (index >= 0)
			{
				_iDs.RemoveAt(index);
				_objects.RemoveAt(index);
				return true;
			}
			return false;
		}

		#endregion

		#region IEnumerable<int> Members

		public IEnumerator<int> GetEnumerator()
		{
			return _iDs.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _iDs.GetEnumerator();
		}

		#endregion
	}

	/// <summary>
	/// Simple base object containing only a name.
	/// </summary>
	public abstract class ObjectBase : System.Object
	{
		public ObjectBase(string name) : base()
		{
			_name = name;
		}

		// Name
		private string _name;
		/// <summary>The name of the object.</summary>
		public virtual string Name
		{
			get { return _name; }
			set { _name = value; }
		}

        public override bool Equals(object objectValue)
        {
			if ((objectValue is Object) && ((((Object)objectValue).Name != String.Empty) || Name != String.Empty))
				return Object.NamesEqual(((Object)objectValue).Name, Name);
			else
				return base.Equals(objectValue);
        }

        public override int GetHashCode()
        {
			return Object.Unqualify(_name).GetHashCode();
        }
        
        public override string ToString()
        {
			return _name == String.Empty ? base.ToString() : _name;
        }
	}
    
	/// <summary>The abstract base class for all schema objects in the catalog cache.</summary>
	public abstract class Object : ObjectBase, IMetaData
    {
		public const int MaxObjectNameLength = 200; // Maximum length of an object name
		public const int MaxDescriptionLength = 200; // Maximum length of the persisted description of an object
		public const string Ellipsis = "..."; // Appended to a description that was truncated for persistence
		public const int MaxObjectIDLength = 10; // Int32.MaxValue.ToString().Length;
		public const int MaxGeneratedNameLength = MaxObjectNameLength - MaxObjectIDLength;
		
		public Object(int iD, string name) : base(name)
		{
			_iD = iD;
		}
		
		public Object(string name) : base(name)
		{
			_iD = GetNextObjectID();
		}

		// ID
		protected int _iD;
		/// <summary>Auto generated surrogate key for the object.</summary>
		public int ID { get { return _iD; } }
		
		// Library
		[Reference]
		protected LoadedLibrary _library;
		public LoadedLibrary Library
		{
			get { return _library; }
			set { _library = value; }
		}

		// CatalogObjectID
		/// <summary>The ID of the catalog object that ultimately contains this object, if this is not a catalog object.</summary>
		/// <remarks>
		/// For all catalog objects, the value of this property is -1. For all non-catalog objects, the
		/// value of this property is the ID of the catalog object that ultimately contains this object. For example,
		/// a column will have a the ID of the table it is part of as the CatalogObjectID. A default on that column
		/// will also have the ID of the table as the CatalogObjectID, but would have the ID of the containing column
		/// as the value of the ParentObjectID property.
		/// </remarks>
		public virtual int CatalogObjectID { get { return -1; } }

		// ParentObjectID		
		/// <summary>The ID of the schema object that immediately contains this object, if this is not a catalog object.</summary>
		/// <remarks>
		/// For all catalog objects, the value of this property is -1. For all non-catalog objects, the
		/// value of this property is the ID of the schema object that immediately contains this object. For example,
		/// a column will have the ID of the table it is part of as the ParentObjectID. A default on that column,
		/// however, will have the ID of the column as the ParentObjectID, but would have the ID of the table
		/// as the value of the CatalogObjectID property.
		/// </remarks>
		public virtual int ParentObjectID { get { return -1; } }
		
		// DisplayName
		/// <summary>Returns a user-friendly name for the object.</summary>
		/// <remarks>
		/// The DisplayName property is the same as the Name property with the following exceptions:
		///		Device maps have an internally unique name, so the DisplayName for these objects is built using the device name and the name of the object being mapped.
		///		The DisplayName for session-specific objects is the name of the object in the session in which it was created.
		///		The DisplayName for application-transaction-specific objects is the DisplayName of the object being mapped into the application transaction context.
		/// </remarks>
		public virtual string DisplayName { get { return Name; } }
		
		// Description
		/// <summary>Returns a more detailed description of the object.</summary>
		public virtual string Description { get { return DisplayName; } }
		
		// IsSystem
		private bool _isSystem;
		/// <summary>Returns true if this object is part of the system catalog created and managed by the system, false otherwise.</summary>
		public virtual bool IsSystem
		{
			get { return _isSystem; }
			set { _isSystem = value; }
		}
		
		// Generator
		[Reference]
		private Schema.Object _generator;
		/// <summary>A reference to the object that generated this object.</summary>
		/// <remarks>Should only be accessed directly for management. To select the generator, use ResolveGenerator.</remarks>
		public Schema.Object Generator
		{
			get { return _generator; }
			set
			{
				_generator = value;
				_generatorID = value == null ? -1 : _generator.ID;
			}
		}
		
		public Schema.Object ResolveGenerator(CatalogDeviceSession session)
		{
			if ((_generator == null) && (_generatorID >= 0))
				_generator = session.ResolveObject(_generatorID);
			return _generator;
		}
		
		// GeneratorID
		private int _generatorID = -1;
		/// <summary>The ID of the object responsible for generating this object, if this is a generated object.</summary>
		/// <remarks>
		/// If this is a generated object, this will be the ID of the object that generated it. Otherwise, this property will be -1.
		/// </remarks>
		public int GeneratorID
		{
			get { return _generatorID; }
			set { _generatorID = value; }
		}
		
		public void LoadGeneratorID()
		{
			Tag tag = RemoveMetaDataTag("DAE.GeneratorID");
			if (tag != Tag.None)
				_generatorID = Int32.Parse(tag.Value);
		}
		
		public void SaveGeneratorID()
		{
			if (_generatorID >= 0)
				AddMetaDataTag("DAE.GeneratorID", _generatorID.ToString(), true);
		}

		public void RemoveGeneratorID()
		{
			RemoveMetaDataTag("DAE.GeneratorID");
		}

		public void LoadIsGenerated()
		{
			Tag tag = RemoveMetaDataTag("DAE.IsGenerated");
			if (tag != Tag.None)
				_isGenerated = Boolean.Parse(tag.Value);
		}

		public void SaveIsGenerated()
		{
			if (_isGenerated)
				AddMetaDataTag("DAE.IsGenerated", _isGenerated.ToString(), true);
		}
		
		public void RemoveIsGenerated()
		{
			RemoveMetaDataTag("DAE.IsGenerated");
		}

		// IsGenerated
		private bool _isGenerated;
		/// <summary>Returns true if this object was generated by the compiler, rather than explicitly created by a DDL statement, false otherwise.</summary>
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}

		/// <summary>Returns true if this is a session-specific object, false otherwise.</summary>		
		public virtual bool IsSessionObject { get { return false; } }
		
		/// <summary>Returns true if this is an application-transaction-specific object, false otherwise.</summary>
		public virtual bool IsATObject { get { return false; } }
		
		/// <summary>Returns true if this object is persisted independently, false otherwuise.</summary>
		public virtual bool IsPersistent { get { return false; } }

		// Objects that this object depends on
		[Reference]
		private ObjectList _dependencies;
		public ObjectList Dependencies 
		{ 
			get 
			{
				if (_dependencies == null)
					_dependencies = new ObjectList();
				return _dependencies;
			} 
		}
		
		public bool HasDependencies()
		{
			return (_dependencies != null) && (_dependencies.Count > 0);
		}

		public void AddDependency(Object objectValue)
		{
			Dependencies.Ensure(objectValue.ID, objectValue);
		}
		
		public void AddDependencies(ObjectList dependencies)
		{
			for (int index = 0; index < dependencies.Count; index++)
				Dependencies.Ensure(dependencies.IDs[index], dependencies.Objects[index]);
		}
		
		public void RemoveDependency(Object objectValue)
		{
			if (_dependencies != null)
				_dependencies.Remove(objectValue.ID);
		}
		
		public void LoadDependencies(CatalogDeviceSession session)
		{
			Schema.DependentObjectHeaders dependencies = session.SelectObjectDependencies(ID, false);
			for (int index = 0; index < dependencies.Count; index++)
				Dependencies.Ensure(dependencies[index].ID);
		}

		// MetaData
		private MetaData _metaData;
		/// <summary>The MetaData for this object.</summary>
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
		
		public void AddMetaDataTag(string tagName, string tagValue, bool isStatic)
		{
			if (_metaData == null)
				_metaData = new MetaData();
			_metaData.Tags.AddOrUpdate(tagName, tagValue, isStatic);
		}

		public Tag RemoveMetaDataTag(string tagName)
		{
			if (_metaData != null)
			{
				Tag result = _metaData.Tags.RemoveTag(tagName);
				if (_metaData.Tags.Count == 0)
					_metaData = null;
				return result;
			}
			return Tag.None;
		}

		public Tag GetMetaDataTag(string tagName)
		{
			if (_metaData != null && _metaData.HasTags())
			{
				return _metaData.Tags.GetTag(tagName);
			}

			return Tag.None;
		}

		/// <summary>Merges all tags from the given MetaData into the metadata for this object.</summary>
		public void MergeMetaData(MetaData metaData)
		{
			if (metaData != null && metaData.HasTags())
			{
				if (_metaData == null)
					_metaData = new MetaData();
					
				_metaData.Merge(metaData);
			}
		}
		
		/// <summary>References each dynamic tag in the given metadata, if the metadata for this object does not already contain it.</summary>
		public void InheritMetaData(MetaData metaData)
		{
			if (metaData != null && metaData.HasTags())
			{
				if (_metaData == null)
					_metaData = metaData.Inherit();
				else
					_metaData.Inherit(metaData);
			}
		}
		
		/// <summary>Joins each dynamic tag in the given meatadata to the metadata for this object, using copy semantics.</summary>
		public void JoinMetaData(MetaData metaData)
		{
			if (metaData != null && metaData.HasTags())
			{
				if (_metaData == null)
					_metaData = new MetaData();
					
				_metaData.Join(metaData);

				if (_metaData.Tags.Count == 0)
					_metaData = null;
			}
		}
		
		/// <summary>Joins each dynamic tag in the given metadata to the metadata for this object, using reference semantics.</summary>
		public void JoinInheritMetaData(MetaData metaData)
		{
			if (metaData != null && metaData.HasTags())
			{
				if (_metaData == null)
					_metaData = new MetaData();
					
				_metaData.JoinInherit(metaData);

				if (_metaData.Tags.Count == 0)
					_metaData = null;
			}
		}
		
		// IsRemotable
		private bool _isRemotable = true;
		/// <summary>Determines whether this object can be transported across the remoting boundary.</summary>
		/// <remarks>
		/// A catalog object is considered remotable by default
		/// Remotable rules per object type ->
		///		Constraint -> Remotable if all dependencies are remotable.
		///		Default -> Remotable if all dependencies are remotable.
		///		Sort -> Remotable if all dependencies are remotable.
		///		Column -> Remotable if its type is remotable as well as its defaults and constraints.
		///		ScalarType -> Remotable if all dependencies are remotable, as well as its defaults, constraints, representations and specials.
		///		RowType -> Remotable if all columns are remotable
		///		TableType -> Remotable if all dependencies are remotable, as well as its columns and constraints.
		///		ListType -> Remotable if all dependencies are remotable.
		///		CursorType -> Remotable if all dependencies are remotable.
		///		Representation -> Remotable if its selector and all its properties are remotable.
		///		Property -> Remotable if its accessors are remotable.
		///		Special -> Remotable if all dependencies are remotable.
		///		Order -> Remotable if all columns are remotable.
		///		OrderColumn -> Remotable if the column it refers to is remotable.
		///		Reference -> Not remotable, as it references nonremotable objects.
		///		TableVar -> Not remotable
		///		Operand -> Remotable if all dependencies are remotable.
		///		Operator -> Remotable if all dependencies are remotable.
		///		Device -> Not remotable.
		/// </remarks>
		public bool IsRemotable
		{
			get { return _isRemotable; }
			set { _isRemotable = value; }
		}
		
		public virtual void DetermineRemotable(CatalogDeviceSession session)
		{
			if (_dependencies != null)
				for (int index = 0; index < _dependencies.Count; index++)
					if (!_dependencies.ResolveObject(session, index).IsRemotable)
					{
						_isRemotable = false;
						break;
					}
		}
        
        public virtual void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
			if (_dependencies != null)
				for (int index = 0; index < _dependencies.Count; index++)
					_dependencies.ResolveObject(session, index).IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
        }
        
        public virtual void IncludeHandlers(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
        }
        
        public bool HasDependentConstraints(CatalogDeviceSession session)
        {
			List<Schema.DependentObjectHeader> headers = session.SelectObjectDependents(ID, true);
			for (int index = 0; index < headers.Count; index++)
				switch (headers[index].ObjectType)
				{
					case "ScalarTypeConstraint":
					case "TableVarColumnConstraint":
					case "RowConstraint":
					case "TransitionConstraint":
					case "CatalogConstraint":
					case "Reference":
						return true;
				}
			return false;
        }
        
        public bool HasDependents(CatalogDeviceSession session)
        {
			List<Schema.DependentObjectHeader> headers = session.SelectObjectDependents(ID, false);
			return (headers.Count > 0);
        }
        
        public static bool NamesEqual(string leftName, string rightName)
        {
			if (((leftName.Length > 0) && (leftName[0].Equals('.'))) || ((rightName.Length > 0) && (rightName[0].Equals('.'))))
				return 
					String.Compare
					(
						leftName.Substring(leftName.IndexOf(Keywords.Qualifier) == 0 ? 1 : 0), 
						rightName.Substring(rightName.IndexOf(Keywords.Qualifier) == 0 ? 1 : 0)
					) == 0;
			else
			{
				int leftIndex = leftName.Length - 1;
				int rightIndex = rightName.Length - 1;
				
				if (leftIndex >= rightIndex)
				{
					while (true)
					{
						if (rightIndex < 0)
						{
							if ((leftIndex < 0) || leftName[leftIndex].Equals('.'))
								return true;
							else
								return false;
						}
						
						if (!leftName[leftIndex].Equals(rightName[rightIndex]))
							return false;
						
						rightIndex--;
						leftIndex--;
					}
				}

				return false;
			}
        }

		public static int GetQualifierCount(string name)
		{
			int result = 0;
			for (int index = 0; index < name.Length; index++)
				if (name[index] == '.')
					result++;
			return result;
		}
		
        public static string Qualify(string name, string nameSpace)
        {
			if (name.IndexOf(Keywords.Qualifier) == 0)
				return name.Substring(1);
			else if (nameSpace != String.Empty)
				return String.Format("{0}{1}{2}", nameSpace, Keywords.Qualifier, name);
			else
				return name;
        }
        
        public static bool IsRooted(string name)
        {
			return name.IndexOf(Keywords.Qualifier) == 0;
        }
        
        public static string EnsureRooted(string name)
        {
			return String.Format("{0}{1}", name.IndexOf(Keywords.Qualifier) == 0 ? String.Empty : Keywords.Qualifier, name);
        }
        
        public static string EnsureUnrooted(string name)
        {
			return name.IndexOf(Keywords.Qualifier) == 0 ? name.Substring(1) : name;
        }
        
        /// <summary>Replaces all qualifiers in the given name with an underscore.</summary>
        public static string MangleQualifiers(string name)
        {
			return name.Replace(Keywords.Qualifier, "_");
		}

		/// <summary>Removes one level of qualification from the given identifier.</summary>
		public static string Dequalify(string name)
		{
			name = EnsureUnrooted(name);
			int index = name.IndexOf(Keywords.Qualifier);
			if ((index > 0) && (index < name.Length - 1))
				return name.Substring(index + 1);
			else
				return name;
		}
		
		/// <summary>Returns the unqualified identifier.</summary>
		public static string Unqualify(string name)
		{
			int index = name.LastIndexOf(Keywords.Qualifier);
			if ((index > 0) && (index < name.Length - 1))
				return name.Substring(index + 1);
			else
				return name;
		}
		
		/// <summary>Returns the qualifier of the given name.  If the name does not contain a qualifier, the empty string is returned.</summary>
		public static string Qualifier(string name)
		{
			name = EnsureUnrooted(name);
			int index = name.IndexOf(Keywords.Qualifier);
			if (index >= 0)
				return name.Substring(0, index);
			else
				return String.Empty;
		}
		
		/// <summary> Returns true if the given identifier isn't qualified (not including the root).</summary>
		public static bool IsQualified(string name)
		{
			name = EnsureUnrooted(name);
			return name.IndexOf(Keywords.Qualifier) >= 0;
		}
		
		/// <summary>Returns the given name with the given qualifier removed.  If the name does not begin with the given qualifier, the given name is returned unchanged.  If the name is equal to the given qualifier, the empty string is returned.</summary>
		public static string RemoveQualifier(string name, string qualifier)
		{
			int index = name.IndexOf(qualifier);
			if (index == 0)
				if (name.Length == qualifier.Length)
					return String.Empty;
				else
					return name.Substring(qualifier.Length + 1);
			else
				return name;
		}
		
		public static string GetUniqueName()
		{
			return NameFromGuid(Guid.NewGuid());
		}
		
		/// <summary>Ensures that the given description is not longer than the maximum description length.</summary>
		/// <remarks>
		/// If the given description is longer than the maximum object description length (200 characters), the first
		/// 200 characters less the length of the ellipsis mark (...), or 197 characters, is returned with the ellipsis
		/// mark appended. Otherwise, the description is returned unchanged.
		/// </remarks>
		public static string EnsureDescriptionLength(string description)
		{
			if (description.Length > MaxDescriptionLength)
				return description.Substring(0, MaxDescriptionLength - Ellipsis.Length) + Ellipsis;
			return description;
		}
		
		/// <summary>Ensures that the given name is not longer than the maximum object name length.</summary>
		/// <remarks>
		/// If the given name is longer than the maximum object name length (200 characters), the first 200 characters of the
		/// name are returned. Otheriwse, the name is returned unchanged.
		/// </remarks>
		public static string EnsureNameLength(string name)
		{
			if (name.Length > MaxObjectNameLength)
				return name.Substring(0, MaxObjectNameLength);
			return name;
		}
		
		public static string GetGeneratedName(string seed, int objectID)
		{
			return String.Format("{0}{1}", seed.Length > MaxGeneratedNameLength ? seed.Substring(0, MaxGeneratedNameLength) : seed, objectID.ToString().PadLeft(MaxObjectIDLength, '0'));
		}
		
		public static string NameFromGuid(Guid iD)
		{
			return String.Format("Object_{0}", iD.ToString().Replace("-", "_"));
		}

		private static int _nextID = 0;		
		public static int GetNextObjectID()
		{
			return Interlocked.Increment(ref _nextID);
		}
		
		public static void SetNextObjectID(int nextID)
		{
			lock (typeof(Schema.Object))
			{
				_nextID = nextID;
			}
		}
		
		/// <summary>Gets the object id from the given meta data and removes the DAE.ObjectID tag, if it exists. Otherwise, returns the value of GetNextObjectID().</summary>
		public static int GetObjectID(MetaData metaData)
		{
			Tag tag = MetaData.RemoveTag(metaData, "DAE.ObjectID");
			if (tag != Tag.None)
				return Int32.Parse(tag.Value);
			return GetNextObjectID();
		}

		/// <summary>Ensures that the object has metadata and a DAE.ObjectID tag with the id of the object.</summary>		
		public void SaveObjectID()
		{
			AddMetaDataTag("DAE.ObjectID", _iD.ToString(), true);
		}
		
		public void RemoveObjectID()
		{
			RemoveMetaDataTag("DAE.ObjectID");
		}

        public virtual Statement EmitStatement(EmitMode mode)
        {
			throw new SchemaException(SchemaException.Codes.StatementCannotBeEmitted, GetType().Name);
        }
        
        public virtual Statement EmitDropStatement(EmitMode mode)
        {
			throw new SchemaException(SchemaException.Codes.DropStatementCannotBeEmitted, GetType().Name);
        }
        
        public ObjectHeader GetHeader()
        {
			return new ObjectHeader(this);
        }
        
		public virtual Object GetObjectFromHeader(ObjectHeader header)
		{
			if (_iD == header.ID)
				return this;
			throw new Schema.SchemaException(Schema.SchemaException.Codes.CouldNotResolveObjectHeader, ErrorSeverity.System, header.ID, header.Name);
		}
	}
	
	/// <summary>ObjectHeader</summary>
	public class ObjectHeader : System.Object
	{
		public ObjectHeader(Schema.Object objectValue) : base()
		{
			_iD = objectValue.ID;
			_name = objectValue.Name;
			_libraryName = objectValue.Library == null ? String.Empty : objectValue.Library.Name;
			_displayName = objectValue.DisplayName;
			_objectType = objectValue.GetType().Name;
			_isSystem = objectValue.IsSystem;
			_isRemotable = objectValue.IsRemotable;
			_isGenerated = objectValue.IsGenerated;
			_isATObject = objectValue.IsATObject;
			_isSessionObject = objectValue.IsSessionObject;
			_isPersistent = objectValue.IsPersistent;
			_catalogObjectID = objectValue.CatalogObjectID;
			_parentObjectID = objectValue.ParentObjectID;
			_generatorObjectID = objectValue.GeneratorID;
		}
		
		public ObjectHeader
		(
			int iD,
			string name,
			string libraryName,
			string displayName,
			string objectType,
			bool isSystem,
			bool isRemotable,
			bool isGenerated,
			bool isATObject,
			bool isSessionObject,
			bool isPersistent,
			int catalogObjectID,
			int parentObjectID,
			int generatorObjectID
		) : base()
		{
			_iD = iD;
			_name = name;
			_libraryName = libraryName;
			_displayName = displayName;
			_objectType = objectType;
			_isSystem = isSystem;
			_isRemotable = isRemotable;
			_isGenerated = isGenerated;
			_isATObject = isATObject;
			_isSessionObject = isSessionObject;
			_isPersistent = isPersistent;
			_catalogObjectID = catalogObjectID;
			_parentObjectID = parentObjectID;
			_generatorObjectID = generatorObjectID;
		}
		
		private int _iD;
		public int ID { get { return _iD; } }
		
		private string _name;
		public string Name { get { return _name; } }
		
		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		private string _displayName;
		public string DisplayName { get { return _displayName; } }
		
		private string _objectType;
		public string ObjectType { get { return _objectType; } }
		
		private bool _isSystem;
		public bool IsSystem { get { return _isSystem; } }
		
		private bool _isRemotable;
		public bool IsRemotable { get { return _isRemotable; } }
		
		private bool _isGenerated;
		public bool IsGenerated { get { return _isGenerated; } }
		
		private bool _isATObject;
		public bool IsATObject { get { return _isATObject; } }
		
		private bool _isSessionObject;
		public bool IsSessionObject { get { return _isSessionObject; } }
		
		private bool _isPersistent;
		public bool IsPersistent { get { return _isPersistent; } }
		
		private int _catalogObjectID;
		public int CatalogObjectID { get { return _catalogObjectID; } }
		
		private int _parentObjectID;
		public int ParentObjectID { get { return _parentObjectID; } }
		
		private int _generatorObjectID;
		public int GeneratorObjectID { get { return _generatorObjectID; } }
		
		public override int GetHashCode()
		{
			return _iD.GetHashCode();
		}

		public override bool Equals(object objectValue)
		{
			ObjectHeader localObjectValue = objectValue as ObjectHeader;
			return (localObjectValue != null) && (_iD == localObjectValue.ID);
		}
		
		[Reference]
		private Schema.Object _object;
		public Schema.Object ResolveObject(CatalogDeviceSession session)
		{
			if (_object == null)
				_object = session.ResolveObject(_iD);

			return _object;
		}
	}
	
	public class DependentObjectHeaders : List<DependentObjectHeader>
	{
		/// <summary>Returns true if this list contains a header with the given ID.</summary>
		public bool Contains(int objectID)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].ID == objectID)
					return true;
			return false;
		}
	}
	
	/// <summary>DependentObjectHeader</summary>
	public class DependentObjectHeader : ObjectHeader
	{
		public DependentObjectHeader
		(
			int iD,
			string name,
			string libraryName,
			string displayName,
			string description,
			string objectType,
			bool isSystem,
			bool isRemotable,
			bool isGenerated,
			bool isATObject,
			bool isSessionObject,
			bool isPersistent,
			int catalogObjectID,
			int parentObjectID,
			int generatorObjectID,
			int level,
			int sequence
		) 
			: base
			(
				iD,
				name,
				libraryName,
				displayName,
				objectType,
				isSystem,
				isRemotable,
				isGenerated,
				isATObject,
				isSessionObject,
				isPersistent,
				catalogObjectID,
				parentObjectID,
				generatorObjectID
			) 
		{
			_description = description;
			_level = level;
			_sequence = sequence;
		}
		
		private string _description;
		public string Description { get { return _description; } }

		private int _level;
		public int Level { get { return _level; } }
		
		private int _sequence;
		public int Sequence { get { return _sequence; } }		
	}
	
	public class PersistentObjectHeaders : List<PersistentObjectHeader> { }
    
	public class PersistentObjectHeader : System.Object
	{
		public PersistentObjectHeader
		(
			int iD,
			string name,
			string libraryName,
			string script,
			string displayName,
			string objectType,
			bool isSystem,
			bool isRemotable,
			bool isGenerated,
			bool isATObject,
			bool isSessionObject
		) : base()
		{
			_iD = iD;
			_name = name;
			_libraryName = libraryName;
			_script = script;
			_displayName = displayName;
			_objectType = objectType;
			_isSystem = isSystem;
			_isRemotable = isRemotable;
			_isGenerated = isGenerated;
			_isATObject = isATObject;
			_isSessionObject = isSessionObject;
		}
		
		private int _iD;
		public int ID { get { return _iD; } }
		
		private string _name;
		public string Name { get { return _name; } }
		
		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		private string _script;
		public string Script { get { return _script; } }

		private string _displayName;
		public string DisplayName { get { return _displayName; } }
		
		private string _objectType;
		public string ObjectType { get { return _objectType; } }
		
		private bool _isSystem;
		public bool IsSystem { get { return _isSystem; } }
		
		private bool _isRemotable;
		public bool IsRemotable { get { return _isRemotable; } }
		
		private bool _isGenerated;
		public bool IsGenerated { get { return _isGenerated; } }
		
		private bool _isATObject;
		public bool IsATObject { get { return _isATObject; } }
		
		private bool _isSessionObject;
		public bool IsSessionObject { get { return _isSessionObject; } }
	}
	
	public class CatalogObjectHeaders : List<CatalogObjectHeader> { }
	
	public class CatalogObjectHeader : System.Object
	{
		public CatalogObjectHeader
		(
			int iD,
			string name,
			string libraryName,
			string ownerID
		) : base()
		{
			_iD = iD;
			_name = name;
			_libraryName = libraryName;
			_ownerID = ownerID;
		}
		
		private int _iD;
		public int ID { get { return _iD; } }
		
		private string _name;
		public string Name { get { return _name; } }
		
		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		private string _ownerID;
		public string OwnerID { get { return _ownerID; } }
	}
	
	public class ScalarTypeHeader : System.Object
	{
		public ScalarTypeHeader(int iD, int uniqueSortID, int sortID) : base()
		{
			_iD = iD;
			_uniqueSortID = uniqueSortID;
			_sortID = sortID;
		}
		
		private int _iD;
		public int ID { get { return _iD; } }
		
		private int _uniqueSortID;
		public int UniqueSortID { get { return _uniqueSortID; } }
		
		private int _sortID;
		public int SortID { get { return _sortID; } }
	}
	
	public class FullObjectHeaders : System.Object
	{
		private List<FullObjectHeader> _headers = new List<FullObjectHeader>();
		
		/// <summary>Stores the index of the header with a given ID.</summary>
		private Dictionary<int, int> _headerHash = new Dictionary<int, int>();
		
		public void Add(FullObjectHeader header)
		{
			_headers.Add(header);
			_headerHash.Add(header.ID, _headers.Count - 1);
		}
		
		public int Count { get { return _headers.Count; } }
		
		public FullObjectHeader this[int index] { get { return _headers[index]; } }
		
		/// <summary>Returns true if this list contains a header with the given ID.</summary>
		public bool Contains(int objectID)
		{
			return _headerHash.ContainsKey(objectID);
		}
	}
	
	/// <summary>FullObjectHeader</summary>
	public class FullObjectHeader : System.Object
	{
		public FullObjectHeader
		(
			int iD,
			string name,
			string libraryName,
			string script,
			string displayName,
			string objectType,
			bool isSystem,
			bool isRemotable,
			bool isGenerated,
			bool isATObject,
			bool isSessionObject,
			bool isPersistent,
			int catalogObjectID,
			int parentObjectID,
			int generatorObjectID
		) : base()
		{
			_iD = iD;
			_name = name;
			_libraryName = libraryName;
			_script = script;
			_displayName = displayName;
			_objectType = objectType;
			_isSystem = isSystem;
			_isRemotable = isRemotable;
			_isGenerated = isGenerated;
			_isATObject = isATObject;
			_isSessionObject = isSessionObject;
			_isPersistent = isPersistent;
			_catalogObjectID = catalogObjectID;
			_parentObjectID = parentObjectID;
			_generatorObjectID = generatorObjectID;
		}
		
		private int _iD;
		public int ID { get { return _iD; } }
		
		private string _name;
		public string Name { get { return _name; } }
		
		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		private string _script;
		public string Script { get { return _script; } }

		private string _displayName;
		public string DisplayName { get { return _displayName; } }
		
		private string _objectType;
		public string ObjectType { get { return _objectType; } }
		
		private bool _isSystem;
		public bool IsSystem { get { return _isSystem; } }
		
		private bool _isRemotable;
		public bool IsRemotable { get { return _isRemotable; } }
		
		private bool _isGenerated;
		public bool IsGenerated { get { return _isGenerated; } }
		
		private bool _isATObject;
		public bool IsATObject { get { return _isATObject; } }
		
		private bool _isSessionObject;
		public bool IsSessionObject { get { return _isSessionObject; } }
		
		private bool _isPersistent;
		public bool IsPersistent { get { return _isPersistent; } }
		
		private int _catalogObjectID;
		public int CatalogObjectID { get { return _catalogObjectID; } }
		
		private int _parentObjectID;
		public int ParentObjectID { get { return _parentObjectID; } }
		
		private int _generatorObjectID;
		public int GeneratorObjectID { get { return _generatorObjectID; } }
		
		public override int GetHashCode()
		{
			return _iD.GetHashCode();
		}

		public override bool Equals(object objectValue)
		{
			FullObjectHeader localObjectValue = objectValue as FullObjectHeader;
			return (localObjectValue != null) && (_iD == localObjectValue.ID);
		}

		public override string ToString()
		{
			return String.Format("{0}: {1}", _iD.ToString(), _displayName);
		}
	}
	
	public class FullCatalogObjectHeader : FullObjectHeader
	{
		public FullCatalogObjectHeader
		(
			int iD,
			string name,
			string libraryName,
			string ownerID,
			string script,
			string displayName,
			string objectType,
			bool isSystem,
			bool isRemotable,
			bool isGenerated,
			bool isATObject,
			bool isSessionObject,
			int generatorObjectID
		) : base(iD, name, libraryName, script, displayName, objectType, isSystem, isRemotable, isGenerated, isATObject, isSessionObject, true, -1, -1, generatorObjectID)
		{
			_ownerID = ownerID;
		}
		
		private string _ownerID;
		public string OwnerID { get { return _ownerID; } }
	}
	
	public class IntegerList : BaseList<int> { }

	public class Hashtables : BaseList<Dictionary<string, IntegerList>> { }

    public class BaseObjects<T> : System.Object, IList where T : ObjectBase
    {
		private const int DefaultInitialCapacity = 0;
		private const int DefaultLowerBoundGrowth = 1;
		private const int DefaultRolloverCount = 100;
		
		private T[] _items;
		private int _count;
		private int _initialCapacity;
		
		public BaseObjects() : base()
		{
			_initialCapacity = DefaultInitialCapacity;
		}
		
		public BaseObjects(int initialCapacity) : base()
		{
			_initialCapacity = initialCapacity;
		}
		
		public T this[int index] 
		{ 
			get
			{ 
				try
				{
					return _items[index];
				}
				catch (IndexOutOfRangeException e)
				{
					throw new SchemaException(SchemaException.Codes.IndexOutOfRange, index);
				}
			} 
			set
			{ 
				lock (this)
				{
					InternalRemoveAt(index);
					InternalInsert(index, value);
				}
			} 
		}

		#if SINGLENAMESPACE		
		public Object this[string AName, string ANameSpace]
		{
			get
			{
				lock (this)
				{
					int LIndex = InternalIndexOf(AName, ANameSpace, true);
					if (LIndex >= 0)
						return this[LIndex];
					else
						throw new SchemaException(SchemaException.Codes.ObjectNotFound, AName);
				}
			}
			set
			{
				lock (this)
				{
					int LIndex = InternalIndexOf(AName, ANameSpace, true);
					if (LIndex >= 0)
						this[LIndex] = value;
					else
						throw new SchemaException(SchemaException.Codes.ObjectNotFound, AName);
				}
			}
		}

		public Object this[string AName]
		{
			get { return this[AName, String.Empty]; }
			set { this[AName, String.Empty] = value; }
		}
		#else
		public T this[string name]
		{
			get
			{
				lock (this)
				{
					int index = InternalIndexOf(name, true);
					if (index >= 0)
						return this[index];
					throw new SchemaException(SchemaException.Codes.ObjectNotFound, name);
				}
			}
			set
			{
				lock (this)
				{
					int index = InternalIndexOf(name, true);
					if (index >= 0)
						this[index] = value;
					throw new SchemaException(SchemaException.Codes.ObjectNotFound, name);
				}
			}
		}
		#endif

		protected int InternalIndexOfName(string name)
		{
			if (name.IndexOf(Keywords.Qualifier) == 0)
				name = name.Substring(1);

			if (IsRolledOver)
			{
				var index = GetNameIndexForDepth(0);
				IntegerList nameBucket;
				if (index.TryGetValue(name, out nameBucket))
					return nameBucket[0];
				else
					return -1;
			}
			else
			{
				for (int index = 0; index < Count; index++)
					if (String.Compare(this[index].Name, name) == 0)
						return index;
				return -1;
			}
		}

		public int IndexOfName(string name)
		{
			lock (this)
			{
				return InternalIndexOfName(name);
			}
		}
		
		protected int InternalIndexOf(string name, bool throwIfAmbiguous)
		{
			List<string> names = new List<string>();
			int objectIndex = InternalIndexOf(name, names);
			if ((objectIndex < 0) && (names.Count > 1) && throwIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, name, ExceptionUtility.StringsToCommaList(names));
			return objectIndex;
		}
		
		protected IntegerList InternalIndexesOf(string name)
		{
			int objectIndex;
			IntegerList indexes = new IntegerList();
			if ((name.Length > 0) && (name[0] == '.'))
			{
				objectIndex = InternalIndexOfName(name);
				if (objectIndex >= 0)
					indexes.Add(objectIndex);
				return indexes;
			}

			if (IsRolledOver)
			{
				int depth = Schema.Object.GetQualifierCount(name);
				if (depth > _nameIndex.Count)
					return indexes;
					
				for (int index = _nameIndex.Count - 1 - depth; index >= 0; index--)
				{
					var nameIndex = GetNameIndexForDepth(index);
					IntegerList nameBucket;
					if (nameIndex.TryGetValue(name, out nameBucket))
						indexes.AddRange(nameBucket);
				}
				return indexes;
			}
			else
			{
				for (int index = 0; index < Count; index++)
					if (Object.NamesEqual(this[index].Name, name))
						indexes.Add(index);
				return indexes;
			}
		}
		
		public IntegerList IndexesOf(string name)
		{
			lock (this)
			{
				return InternalIndexesOf(name);
			}
		}
		
		protected int InternalIndexOf(string name, List<String> names)
		{
			IntegerList indexes = InternalIndexesOf(name);
			for (int index = 0; index < indexes.Count; index++)
				names.Add(this[indexes[index]].Name);
			return (indexes.Count == 1) ? indexes[0] : -1;
		}

		public int IndexOf(string name)
		{
			return IndexOf(name, true);
		}
		
		public int IndexOf(string name, bool throwIfAmbiguous)
		{
			List<string> names = new List<string>();
			int index = IndexOf(name, names);
			if ((index < 0) && (names.Count > 1) && throwIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, name, ExceptionUtility.StringsToCommaList(names));
			return index;
		}

		public int IndexOf(string name, List<String> names)
		{
			lock (this)
			{
				return InternalIndexOf(name, names);
			}
		}
		
		#if SINGLENAMESPACE
		protected int InternalIndexOf(string AName, string ANameSpace, bool AThrowIfAmbiguous)
		{
			List<string> LNames = new List<string>();
			int LObjectIndex = InternalIndexOf(AName, ANameSpace, LNames);
			if ((LObjectIndex < 0) && (LNames.Count > 1) && AThrowIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LObjectIndex;
		}
		
		protected int InternalIndexOf(string AName, string ANameSpace, List<string> ANames)
		{
			int LObjectIndex = -1;
			if ((AName.IndexOf(Keywords.Qualifier) != 0) && (ANameSpace != String.Empty))
				LObjectIndex = InternalIndexOf(Schema.Object.Qualify(AName, ANameSpace), ANames);
			if ((ANames.Count < 1) && (LObjectIndex < 0))
				LObjectIndex = InternalIndexOf(AName, ANames);
			return LObjectIndex;
		}

		public int IndexOf(string AName, string ANameSpace)
		{
			return IndexOf(AName, ANameSpace, true);
		}
		
		public int IndexOf(string AName, string ANameSpace, bool AThrowIfAmbiguous)
		{
			List<string> LNames = new List<string>();
			int LIndex = IndexOf(AName, ANameSpace, LNames);
			if ((LIndex < 0) && (LNames.Count > 1) && AThrowIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LIndex;
		}
		
		public int IndexOf(string AName, string ANameSpace, List<string> ANames)
		{
			lock (this)
			{
				return InternalIndexOf(AName, ANameSpace, ANames);
			}
		}
		#endif
		
		protected int InternalIndexOf(T item)
		{
			return InternalIndexOfName(item.Name);
		}

		public int IndexOf(T item)
		{
			lock (this)
			{
				return InternalIndexOf(item);
			}
		}
		
		public bool Contains(string name)
		{
			return (IndexOf(name) >= 0);
		}
		
		public bool ContainsName(string name)
		{
			return (IndexOfName(name) >= 0);
		}
		
		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}
		
		public int Add(object item)
		{
			lock (this)
			{
				int index = Count;
				InternalInsert(index, item);
				return index;
			}
		}
		
		public void AddRange(ICollection collection)
		{
			foreach (object lObject in collection)
				Add(lObject);
		}
		
		protected void InternalInsert(int index, object item)
		{
			T objectValue = item as T;
			if (objectValue == null)
				throw new SchemaException(SchemaException.Codes.ObjectContainer);

			#if USEOBJECTVALIDATE
			Validate(objectValue);
			#endif
			
			if (_items == null)
				_items = new T[_initialCapacity];
			
			if (_count >= _items.Length)
				InternalSetCapacity(_items.Length * 2);
			for (int localIndex = _count - 1; localIndex >= index; localIndex--)
				_items[localIndex + 1] = _items[localIndex];
			_items[index] = objectValue;
			_count++;

			Adding(objectValue, index);
		}
		
		public void Insert(int index, object item)
		{
			lock (this)
			{
				InternalInsert(index, item);
			}
		}
		
		protected void InternalSetCapacity(int tempValue)
		{
			if (tempValue <= 0)
				tempValue = DefaultLowerBoundGrowth;
				
			if (_items.Length != tempValue)
			{
				T[] newItems = new T[tempValue];
				for (int index = 0; index < ((_count > tempValue) ? tempValue : _count); index++)
					newItems[index] = _items[index];

				if (_count > tempValue)						
					for (int index = _count - 1; index > tempValue; index--)
						RemoveAt(index);
						
				_items = newItems;
			}
		}
		
		public int Capacity
		{
			get { return _items == null ? _initialCapacity : _items.Length; }
			set
			{
				lock (this)
				{
					if (_items == null)
						_items = new T[value];
					InternalSetCapacity(value);
				}
			}
		}
		
		protected void InternalRemoveAt(int index)
		{
			Removing(this[index], index);

			_count--;			
			for (int localIndex = index; localIndex < _count; localIndex++)
				_items[localIndex] = _items[localIndex + 1];
			_items[_count] = null; // Clear the last item to prevent a resource leak
		}
		
		public void RemoveAt(int index)
		{
			lock (this)
			{
				InternalRemoveAt(index);
			}
		}
		
		public void Remove(T tempValue)
		{
			lock (this)
			{
				InternalRemoveAt(InternalIndexOf(tempValue));
			}
		}
		
		public void SafeRemove(T tempValue)
		{
			if (tempValue != null)
			{
				lock (this)
				{
					int index = InternalIndexOf(tempValue);
					if (index >= 0)
						InternalRemoveAt(index);
				}
			}
		}
		
		public void SafeRemove(string name)
		{
			lock (this)
			{
				int index = InternalIndexOfName(name);
				if (index >= 0)
					InternalRemoveAt(index);
			}
		}
		
		public void Clear()
		{
			lock (this)
			{
				while (_count > 0)
					InternalRemoveAt(_count - 1);
			}
		}
		
		protected virtual void Validate(T objectValue)
		{
			#if USEOBJECTVALIDATE
			if (objectValue.Name == String.Empty)
				throw new SchemaException(SchemaException.Codes.ObjectNameRequired);
				
			string objectName = objectValue.Name;
			#if DISALLOWAMBIGUOUSNAMES
			int index = InternalIndexOf(objectName, true);
			#else
			int index = InternalIndexOfName(objectName);
			#endif
			if (index >= 0)
			{
				if (String.Compare(this[index].Name, objectName) == 0)
					throw new SchemaException(SchemaException.Codes.DuplicateObjectName, objectName);
				#if DISALLOWAMBIGUOUSNAMES
				else
					throw new SchemaException(SchemaException.Codes.AmbiguousObjectName, objectName, this[index].Name);
				#endif
			}
			#endif
		
			#if DISALLOWAMBIGUOUSNAMES
			IntegerList indexes;
			while (objectName.IndexOf(Keywords.Qualifier) >= 0)
			{
				objectName = Schema.Object.Dequalify(objectName);
				indexes = IndexesOf(objectName);
				for (int objectIndex = 0; objectIndex < indexes.Count; objectIndex++)
					if (String.Compare(this[indexes[objectIndex]].Name, objectName) == 0)
						throw new SchemaException(SchemaException.Codes.CreatingAmbiguousObjectName, AObject.Name, objectName);
			}
			#endif
		}
		
		public bool IsValidObjectName(string name, List<string> names)
		{
			string objectName = name;
			
			int index = InternalIndexOf(objectName, true);
			if (index >= 0)
			{
				#if DISALLOWAMBIGUOUSNAMES
				ANames.Add(this[index].Name);
				return false;
				#else
				if (String.Compare(name, this[index].Name) == 0)
				{
					names.Add(this[index].Name);
					return false;
				}
				#endif
			}

			#if DISALLOWAMBIGUOUSNAMES			
			IntegerList indexes;
			while (objectName.IndexOf(Keywords.Qualifier) >= 0)
			{
				objectName = Schema.Object.Dequalify(objectName);
				indexes = IndexesOf(objectName);
				for (int objectIndex = 0; objectIndex < indexes.Count; objectIndex++)
					if (String.Compare(this[indexes[objectIndex]].Name, objectName) == 0)
						ANames.Add(objectName);
			}
			#endif
			
			return names.Count == 0;
		}
		
		protected virtual void Adding(T objectValue, int index)
		{
			if (IsRolledOver)
			{
				for (int localIndex = index + 1; localIndex < Count; localIndex++)
					UpdateObjectIndex(this[localIndex], localIndex - 1, localIndex);
				IndexObject(objectValue, index);
			}
			else
				CheckRollover();
		}
		
		protected virtual void Removing(T objectValue, int index)
		{
			if (IsRolledOver)
			{
				UnindexObject(objectValue, index);
				for (int localIndex = index + 1; localIndex < Count; localIndex++)
					UpdateObjectIndex(this[localIndex], localIndex, localIndex - 1);
			}
		}

		protected Hashtables _nameIndex;
		
		protected int _rolloverCount = DefaultRolloverCount;
		/// <value>
		/// An integer value indicating at what size to begin maintenance of secondary indexes on the objects in the list.  
		/// A value of Int32.MaxValue indicates no maintenance is to be performed.  This value is defaulted to 20.
		/// </value>
		[DefaultValue(DefaultRolloverCount)]
		public int RolloverCount
		{
			get { return _rolloverCount; }
			set
			{
				lock (this)
				{
					_rolloverCount = value;
					CheckRollover();
				}
			}
		}
		
		public bool IsRolledOver { get { return _nameIndex != null; } }
		
		protected void CheckRollover()
		{
			if (Count > _rolloverCount)
			{
				if (!IsRolledOver)
					Rollover();
			}
			else
			{
				if (IsRolledOver)
					Rollunder();
			}
		}
		
		protected void Rollover()
		{
			_nameIndex = new Hashtables();
			for (int index = 0; index < Count; index++)
				IndexObject(this[index], index);
		}
		
		protected void Rollunder()
		{
			_nameIndex = null;
		}

		protected Dictionary<string, IntegerList> GetNameIndexForDepth(int depth)
		{
			while (depth > _nameIndex.Count - 1)
				_nameIndex.Add(new Dictionary<string, IntegerList>());
			return _nameIndex[depth];
		}
		
		protected void IndexObject(T objectValue, int index)
		{
			// Add the object to the name index
			string name = objectValue.Name;
			int depth = Schema.Object.GetQualifierCount(name);
			for (int localIndex = 0; localIndex <= depth; localIndex++)
			{
				Dictionary<string, IntegerList> nameIndex = GetNameIndexForDepth(localIndex);
				IntegerList nameBucket;
				if (!nameIndex.TryGetValue(name, out nameBucket))
				{
					nameBucket = new IntegerList();
					nameIndex.Add(name, nameBucket);
				}
				nameBucket.Add(index);
				name = Object.Dequalify(name);
			}
		}
		
		protected void UnindexObject(T objectValue, int index)
		{
			// Remove the object from the name index
			string name = objectValue.Name;
			int depth = Schema.Object.GetQualifierCount(name);
			for (int localIndex = 0; localIndex <= depth; localIndex++)
			{
				Dictionary<string, IntegerList> nameIndex = GetNameIndexForDepth(localIndex);
				IntegerList nameBucket;
				if (!nameIndex.TryGetValue(name, out nameBucket))
					throw new SchemaException(SchemaException.Codes.IndexBucketNotFound, name);
				nameBucket.Remove(index);
				if (nameBucket.Count == 0)
					nameIndex.Remove(name);
				name = Object.Dequalify(name);
			}
		}
		
		protected void UpdateObjectIndex(T objectValue, int oldIndex, int newIndex)
		{
			// Update the objects index in the Name index
			string name = objectValue.Name;
			int depth = Schema.Object.GetQualifierCount(name);
			for (int index = 0; index <= depth; index++)
			{
				IntegerList nameBucket;
				Dictionary<string, IntegerList> nameIndex = GetNameIndexForDepth(index);
				if (!nameIndex.TryGetValue(name, out nameBucket))
					throw new SchemaException(SchemaException.Codes.IndexBucketNotFound, name);
				nameBucket.Remove(oldIndex);
				nameBucket.Add(newIndex);
				name = Object.Dequalify(name);
			}
		}
		
		// IList
		object IList.this[int index] { get { return this[index]; } set { this[index] = (T)value; } }
		int IList.IndexOf(object item) { return (item is T) ? IndexOf((T)item) : -1; }
		bool IList.Contains(object item) { return (item is T) ? Contains((T)item) : false; }
		void IList.Remove(object item) { RemoveAt(IndexOf((T)item)); }
		bool IList.IsFixedSize { get { return false; } }
		bool IList.IsReadOnly { get { return false; } }
		
		// ICollection
		public int Count { get { return _count; } }
		public bool IsSynchronized { get { return false; } }
		public object SyncRoot { get { return this; } }
		public void CopyTo(Array array, int index)
		{
			IList localArray = (IList)array;
			for (int localIndex = 0; localIndex < Count; localIndex++)
				localArray[index + localIndex] = this[localIndex];
		}

		// IEnumerable
		public SchemaObjectEnumerator GetEnumerator()
		{
			return new SchemaObjectEnumerator(this);
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public class SchemaObjectEnumerator : IEnumerator
		{
			public SchemaObjectEnumerator(BaseObjects<T> objects) : base()
			{
				_objects = objects;
			}
			
			private BaseObjects<T> _objects;
			private int _current =  -1;

			public T Current { get { return _objects[_current]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				_current++;
				return (_current < _objects.Count);
			}
			
			public void Reset()
			{
				_current = -1;
			}
		}
    }

	public class Objects<T> : BaseObjects<T> where T : Object
	{
		public Objects() : base() { }
		public Objects(int initialCapacity) : base(initialCapacity) { }

		public int ResolveName(string name, NameResolutionPath path, List<string> names)
		{
			lock (this)
			{
				IntegerList indexes = InternalIndexesOf(name);
				
				if (!Schema.Object.IsRooted(name))
				{
					IntegerList levelIndexes = new IntegerList();
					T objectValue;
					
					for (int levelIndex = 0; levelIndex < path.Count; levelIndex++)
					{
						if (levelIndex > 0)
							levelIndexes.Clear();
							
						for (int index = 0; index < indexes.Count; index++)
						{
							objectValue = this[indexes[index]];
							if ((objectValue.Library == null) || path[levelIndex].ContainsName(objectValue.Library.Name))
								levelIndexes.Add(indexes[index]);
						}
						
						if (levelIndexes.Count > 0)
						{
							for (int index = 0; index < levelIndexes.Count; index++)
								names.Add(this[levelIndexes[index]].Name);
								
							return levelIndexes.Count == 1 ? levelIndexes[0] : -1;
						}
					}
				}
				
				if (indexes.Count > 0)
				{
					for (int index = 0; index < indexes.Count; index++)
						names.Add(this[indexes[index]].Name);
						
					return indexes.Count == 1 ? indexes[0] : -1;
				}
				
				return -1;
			}
		}
		
		public int ResolveName(string name, NameResolutionPath path)
		{
			List<string> names = new List<string>();
			int index = ResolveName(name, path, names);
			if ((index < 0) && (names.Count > 1))
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, name, ExceptionUtility.StringsToCommaList(names));
			return index;
		}
	}

	public class Objects : Objects<Object>
	{
		public Objects() : base() { }
		public Objects(int initialCapacity) : base(initialCapacity) { }
	}

	public abstract class CatalogObject : Object
	{
		public CatalogObject(string name) : base(name) {}
		public CatalogObject(int iD, string name) : base(iD, name) {}
		
		// Name
		public override string Name
		{
			get { return base.Name; }
			set
			{
				if (value.Length > MaxObjectNameLength)
					throw new SchemaException(SchemaException.Codes.MaxObjectNameLengthExceeded, value, MaxObjectNameLength);
				base.Name = value;
			}
		}
		
		// Owner
		[Reference]
		protected User _owner;
		public User Owner
		{
			get { return _owner; }
			set { _owner = value; }
		}
		
		// CatalogObjectID
		public override int CatalogObjectID	{ get { return -1; } }

		// ParentObjectID
		public override int ParentObjectID { get { return -1; } }
		
		// IsSystem
		public override bool IsSystem 
		{ 
			get { return (_owner != null) && (_owner.ID == Server.Engine.SystemUserID); }
			set { throw new RuntimeException(RuntimeException.Codes.InternalError, "Cannot set IsSystem for a Catalog Object"); } 
		}
		
		/// <summary>Returns true if AUser is the owner of this object, or is a member of a parent Group of the owner of this object, recursively.</summary>
		public bool IsOwner(User user)
		{
			if ((user.ID == Server.Engine.SystemUserID) || (user.ID == Server.Engine.AdminUserID) || (user.ID == _owner.ID)) 
				return true;
				
			return false;
		}
		
		public virtual string[] GetRights()
		{
			return new string[]{};
		}
		
		public virtual string GetRight(string rightName)
		{
			return Name + rightName;
		}

		// SessionObjectName
		private string _sessionObjectName;
		public string SessionObjectName
		{
			get { return _sessionObjectName; }
			set { _sessionObjectName = value; }
		}

		// SessionID
		private int _sessionID;
		public int SessionID
		{
			get { return _sessionID; }
			set { _sessionID = value; }
		}
		
		public override bool IsSessionObject { get { return _sessionObjectName != null; } }
		
		/// <summary>Catalog objects are always persistent.</summary>
		public override bool IsPersistent { get { return true; } }
		
		public override string DisplayName { get { return _sessionObjectName == null ? Name : _sessionObjectName; } }
	}
    
    public class SessionObject : Schema.Object
    {
		public SessionObject(string name) : base(name) {}
		public SessionObject(string name, string globalName) : base(name)
		{
			GlobalName = globalName;
		}
		
		private string _globalName;
		public string GlobalName
		{
			get { return _globalName; }
			set { _globalName = value == null ? String.Empty : value; }
		}
    }
}
