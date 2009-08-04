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
using System.Runtime.Serialization;

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
using System.Threading;
											   
namespace Alphora.Dataphor.DAE.Schema
{
	// Delegate classes for Table, Row and Column Trigger and Validation events
    public delegate void RowValidateHandler(object ASender, IServerSession ASession, Row ARow, string AColumnName);
    public delegate void RowChangeHandler(object ASender, IServerSession ASession, Row ARow, string AColumnName, out bool AChanged);
    public delegate void RowInsertHandler(object ASender, IServerSession ASession, Row ARow);
    public delegate void RowUpdateHandler(object ASender, IServerSession ASession, Row AOldRow, Row ANewRow);
    public delegate void RowDeleteHandler(object ASender, IServerSession ASession, Row ARow);
    public delegate void ColumnValidateHandler(object ASender, IServerSession ASession, object AValue);
    public delegate void ColumnChangeHandler(object ASender, IServerSession ASession, ref object AValue, out bool AChanged);
    
    /// <summary>Maintains a set of objects by ID, with the ability to resolve a reference if necessary, caching that reference.</summary>
    /// <remarks>This class is used to track dependencies for catalog objects while they are in the cache.</remarks>
    public class ObjectList : System.Object, ICollection<int>
    {
		private List<int> FIDs = new List<int>(0);
		/// <summary>Provides access to the IDs of the objects in the list, by index.</summary>
		public List<int> IDs { get { return FIDs; } }
		
		private List<Schema.Object> FObjects = new List<Schema.Object>(0);
		/// <summary>Provides access to the references to the objects in the list, by index.</summary>
		/// <remarks>Note that the object reference may be null if it has not yet been resolved to an actual object reference in the catalog.</remarks>
		public List<Schema.Object> Objects { get { return FObjects; } }

		/// <summary>Ensures that the given object ID and object reference is in the list. AObject may be a null reference.</summary>		
		public void Ensure(int AID, Schema.Object AObject)
		{
			if (!FIDs.Contains(AID))
			{
				FIDs.Add(AID);
				FObjects.Add(AObject);
			}
		}

		/// <summary>Ensures that the given ID is in the list, adding it with a null reference if necessary.</summary>
		public void Ensure(int AID)
		{
			Ensure(AID, null);
		}

		/// <summary>Ensures that the given object is in the list by ID, adding the reference as well. AObject may not be a null reference.</summary>		
		public void Ensure(Schema.Object AObject)
		{
			Ensure(AObject.ID, AObject);
		}
		
		/// <summary>Adds the given ID and object reference to the list. If the list already has an entry for AID, an exception is raised. AObject may be a null reference.</summary>
		public void Add(int AID, Schema.Object AObject)
		{
			if (FIDs.Contains(AID))
				throw new SchemaException(SchemaException.Codes.DuplicateObject, ErrorSeverity.System, AID);
			FIDs.Add(AID);
			FObjects.Add(AObject);
		}
		
		/// <summary>Adds the given object to the list by ID, adding the reference as well. AObject may not be a null reference.</summary>
		public void Add(Schema.Object AObject)
		{
			Add(AObject.ID, AObject);
		}
		
		/// <summary>Retrieves the object reference for the object at the given index in this list.</summary>
		/// <remarks>
		/// If the object reference is already available in the list, it is used. Otherwise, the given ServerProcess is used to
		/// resolve the object reference by ID.
		/// </remarks>
		public Schema.Object ResolveObject(ServerProcess AProcess, int AIndex)
		{
			Schema.Object LObject = FObjects[AIndex];
			if (LObject == null)
			{
				LObject = AProcess.CatalogDeviceSession.ResolveObject(FIDs[AIndex]);
				FObjects[AIndex] = LObject;
			}
			
			return LObject;
		}
		
		/// <summary>Copies the contents of this object list to AObjectList.</summary>
		public void CopyTo(ObjectList AObjectList)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				AObjectList.Add(FIDs[LIndex], FObjects[LIndex]);
		}
		
		#region ICollection<int> Members

		/// <summary>Adds the given ID to the list with a null reference for the object. If the ID is already in the list, an exception is raised.</summary>
		public void Add(int AID)
		{
			Add(AID, null);
		}

		/// <summary>Clears the IDs and object references for this object list.</summary>		
		public void Clear()
		{
			FIDs.Clear();
			FObjects.Clear();
		}

		/// <summary>Returns true if the list contains the given ID, false otherwise.</summary>
		public bool Contains(int AID)
		{
			return FIDs.Contains(AID);
		}

		/// <summary>This method is not implemented, calling it will throw an exception.</summary>
		public void CopyTo(int[] array, int arrayIndex)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>Returns the number of items in the list.</summary>
		public int Count
		{
			get { return FIDs.Count; }
		}

		/// <summary>Always returns false.</summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>Removes the given ID and its associated object reference, if any, from the list. Returns true if the object was in the list, false otherwise.</summary>
		public bool Remove(int item)
		{
			int LIndex = FIDs.IndexOf(item);
			if (LIndex >= 0)
			{
				FIDs.RemoveAt(LIndex);
				FObjects.RemoveAt(LIndex);
				return true;
			}
			return false;
		}

		#endregion

		#region IEnumerable<int> Members

		public IEnumerator<int> GetEnumerator()
		{
			return FIDs.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return FIDs.GetEnumerator();
		}

		#endregion
	}
    
	/// <summary>The abstract base class for all schema objects in the catalog cache.</summary>
	public abstract class Object : System.Object, IMetaData
    {
		public const int CMaxObjectNameLength = 200; // Maximum length of an object name
		public const int CMaxDescriptionLength = 200; // Maximum length of the persisted description of an object
		public const string CEllipsis = "..."; // Appended to a description that was truncated for persistence
		public const int CMaxObjectIDLength = 10; // Int32.MaxValue.ToString().Length;
		public const int CMaxGeneratedNameLength = CMaxObjectNameLength - CMaxObjectIDLength;
		
		public Object(int AID, string AName) : base()
		{
			FID = AID;
			Name = AName;
		}
		
		public Object(string AName) : base()
		{
			FID = GetNextObjectID();
			//FID = -1;
			Name = AName;
		}

		// Name
		private string FName;
		/// <summary>The name of the object.</summary>
		public virtual string Name
		{
			get { return FName; }
			set { FName = value; }
		}
		
		// ID
		protected int FID;
		/// <summary>Auto generated surrogate key for the object.</summary>
		public int ID { get { return FID; } }
		
		// Library
		[Reference]
		protected LoadedLibrary FLibrary;
		public LoadedLibrary Library
		{
			get { return FLibrary; }
			set { FLibrary = value; }
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
		private bool FIsSystem;
		/// <summary>Returns true if this object is part of the system catalog created and managed by the system, false otherwise.</summary>
		public virtual bool IsSystem
		{
			get { return FIsSystem; }
			set { FIsSystem = value; }
		}
		
		// Generator
		[Reference]
		private Schema.Object FGenerator;
		/// <summary>A reference to the object that generated this object.</summary>
		/// <remarks>Should only be accessed directly for management. To select the generator, use ResolveGenerator.</remarks>
		public Schema.Object Generator
		{
			get { return FGenerator; }
			set
			{
				FGenerator = value;
				FGeneratorID = value == null ? -1 : FGenerator.ID;
			}
		}
		
		public Schema.Object ResolveGenerator(ServerProcess AProcess)
		{
			if ((FGenerator == null) && (FGeneratorID >= 0))
				FGenerator = AProcess.CatalogDeviceSession.ResolveObject(FGeneratorID);
			return FGenerator;
		}
		
		// GeneratorID
		private int FGeneratorID = -1;
		/// <summary>The ID of the object responsible for generating this object, if this is a generated object.</summary>
		/// <remarks>
		/// If this is a generated object, this will be the ID of the object that generated it. Otherwise, this property will be -1.
		/// </remarks>
		public int GeneratorID
		{
			get { return FGeneratorID; }
			set { FGeneratorID = value; }
		}
		
		public void LoadGeneratorID()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.GeneratorID");
			if (LTag != Tag.None)
				FGeneratorID = Int32.Parse(LTag.Value);
		}
		
		public void SaveGeneratorID()
		{
			if (FGeneratorID >= 0)
				MetaData.Tags.AddOrUpdate("DAE.GeneratorID", FGeneratorID.ToString(), true);
		}

		public void RemoveGeneratorID()
		{
			if (FMetaData != null)
				FMetaData.Tags.RemoveTag("DAE.GeneratorID");
		}

		public void LoadIsGenerated()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.IsGenerated");
			if (LTag != Tag.None)
				FIsGenerated = Boolean.Parse(LTag.Value);
		}

		public void SaveIsGenerated()
		{
			if (FIsGenerated)
				MetaData.Tags.AddOrUpdate("DAE.IsGenerated", FIsGenerated.ToString(), true);
		}
		
		public void RemoveIsGenerated()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.IsGenerated");
		}

		// IsGenerated
		private bool FIsGenerated;
		/// <summary>Returns true if this object was generated by the compiler, rather than explicitly created by a DDL statement, false otherwise.</summary>
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}

		/// <summary>Returns true if this is a session-specific object, false otherwise.</summary>		
		public virtual bool IsSessionObject { get { return false; } }
		
		/// <summary>Returns true if this is an application-transaction-specific object, false otherwise.</summary>
		public virtual bool IsATObject { get { return false; } }
		
		/// <summary>Returns true if this object is persisted independently, false otherwuise.</summary>
		public virtual bool IsPersistent { get { return false; } }

		// Objects that this object depends on
		[Reference]
		private ObjectList FDependencies;
		public ObjectList Dependencies 
		{ 
			get 
			{
				if (FDependencies == null)
					FDependencies = new ObjectList();
				return FDependencies;
			} 
		}
		
		public bool HasDependencies()
		{
			return (FDependencies != null) && (FDependencies.Count > 0);
		}

		public void AddDependency(Object AObject)
		{
			Dependencies.Ensure(AObject.ID, AObject);
		}
		
		public void AddDependencies(ObjectList ADependencies)
		{
			for (int LIndex = 0; LIndex < ADependencies.Count; LIndex++)
				Dependencies.Ensure(ADependencies.IDs[LIndex], ADependencies.Objects[LIndex]);
		}
		
		public void RemoveDependency(Object AObject)
		{
			if (FDependencies != null)
				FDependencies.Remove(AObject.ID);
		}
		
		public void LoadDependencies(ServerProcess AProcess)
		{
			Schema.DependentObjectHeaders LDependencies = AProcess.CatalogDeviceSession.SelectObjectDependencies(ID, false);
			for (int LIndex = 0; LIndex < LDependencies.Count; LIndex++)
				Dependencies.Ensure(LDependencies[LIndex].ID);
		}

		// MetaData
		private MetaData FMetaData;
		/// <summary>The MetaData for this object.</summary>
		public MetaData MetaData
		{
			get { return FMetaData; }
			set { FMetaData = value; }
		}
		
		/// <summary>Merges all tags from the given MetaData into the metadata for this object.</summary>
		public void MergeMetaData(MetaData AMetaData)
		{
			if (AMetaData != null)
			{
				if (FMetaData == null)
					FMetaData = new MetaData();
					
				FMetaData.Merge(AMetaData);
			}
		}
		
		/// <summary>References each dynamic tag in the given metadata, if the metadata for this object does not already contain it.</summary>
		public void InheritMetaData(MetaData AMetaData)
		{
			if (AMetaData != null)
			{
				if (FMetaData == null)
					FMetaData = new MetaData();
					
				FMetaData.Inherit(AMetaData);
			}
		}
		
		/// <summary>Joins each dynamic tag in the given meatadata to the metadata for this object, using copy semantics.</summary>
		public void JoinMetaData(MetaData AMetaData)
		{
			if (AMetaData != null)
			{
				if (FMetaData == null)
					FMetaData = new MetaData();
					
				FMetaData.Join(AMetaData);
			}
		}
		
		/// <summary>Joins each dynamic tag in the given metadata to the metadata for this object, using reference semantics.</summary>
		public void JoinInheritMetaData(MetaData AMetaData)
		{
			if (AMetaData != null)
			{
				if (FMetaData == null)
					FMetaData = new MetaData();
					
				FMetaData.JoinInherit(AMetaData);
			}
		}
		
		// IsRemotable
		private bool FIsRemotable = true;
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
			get { return FIsRemotable; }
			set { FIsRemotable = value; }
		}
		
		public virtual void DetermineRemotable(ServerProcess AProcess)
		{
			if (FDependencies != null)
				for (int LIndex = 0; LIndex < FDependencies.Count; LIndex++)
					if (!FDependencies.ResolveObject(AProcess, LIndex).IsRemotable)
					{
						FIsRemotable = false;
						break;
					}
		}
        
        public virtual void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			if (FDependencies != null)
				for (int LIndex = 0; LIndex < FDependencies.Count; LIndex++)
					FDependencies.ResolveObject(AProcess, LIndex).IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
        }
        
        public virtual void IncludeHandlers(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
        }
        
        public bool HasDependentConstraints(ServerProcess AProcess)
        {
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(ID, true);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
				switch (LHeaders[LIndex].ObjectType)
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
        
        public bool HasDependents(ServerProcess AProcess)
        {
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(ID, false);
			return (LHeaders.Count > 0);
        }
        
        public static bool NamesEqual(string ALeftName, string ARightName)
        {
			if (((ALeftName.Length > 0) && (ALeftName[0].Equals('.'))) || ((ARightName.Length > 0) && (ARightName[0].Equals('.'))))
				return 
					String.Compare
					(
						ALeftName.Substring(ALeftName.IndexOf(Keywords.Qualifier) == 0 ? 1 : 0), 
						ARightName.Substring(ARightName.IndexOf(Keywords.Qualifier) == 0 ? 1 : 0)
					) == 0;
			else
			{
				int LLeftIndex = ALeftName.Length - 1;
				int LRightIndex = ARightName.Length - 1;
				
				if (LLeftIndex >= LRightIndex)
				{
					while (true)
					{
						if (LRightIndex < 0)
						{
							if ((LLeftIndex < 0) || ALeftName[LLeftIndex].Equals('.'))
								return true;
							else
								return false;
						}
						
						if (!ALeftName[LLeftIndex].Equals(ARightName[LRightIndex]))
							return false;
						
						LRightIndex--;
						LLeftIndex--;
					}
				}

				return false;
			}
        }

		public static int GetQualifierCount(string AName)
		{
			int LResult = 0;
			for (int LIndex = 0; LIndex < AName.Length; LIndex++)
				if (AName[LIndex] == '.')
					LResult++;
			return LResult;
		}
		
        public static string Qualify(string AName, string ANameSpace)
        {
			if (AName.IndexOf(Keywords.Qualifier) == 0)
				return AName.Substring(1);
			else if (ANameSpace != String.Empty)
				return String.Format("{0}{1}{2}", ANameSpace, Keywords.Qualifier, AName);
			else
				return AName;
        }
        
        public static bool IsRooted(string AName)
        {
			return AName.IndexOf(Keywords.Qualifier) == 0;
        }
        
        public static string EnsureRooted(string AName)
        {
			return String.Format("{0}{1}", AName.IndexOf(Keywords.Qualifier) == 0 ? String.Empty : Keywords.Qualifier, AName);
        }
        
        public static string EnsureUnrooted(string AName)
        {
			return AName.IndexOf(Keywords.Qualifier) == 0 ? AName.Substring(1) : AName;
        }
        
        /// <summary>Replaces all qualifiers in the given name with an underscore.</summary>
        public static string MangleQualifiers(string AName)
        {
			return AName.Replace(Keywords.Qualifier, "_");
		}

		/// <summary>Removes one level of qualification from the given identifier.</summary>
		public static string Dequalify(string AName)
		{
			AName = EnsureUnrooted(AName);
			int LIndex = AName.IndexOf(Keywords.Qualifier);
			if ((LIndex > 0) && (LIndex < AName.Length - 1))
				return AName.Substring(LIndex + 1);
			else
				return AName;
		}
		
		/// <summary>Returns the unqualified identifier.</summary>
		public static string Unqualify(string AName)
		{
			int LIndex = AName.LastIndexOf(Keywords.Qualifier);
			if ((LIndex > 0) && (LIndex < AName.Length - 1))
				return AName.Substring(LIndex + 1);
			else
				return AName;
		}
		
		/// <summary>Returns the qualifier of the given name.  If the name does not contain a qualifier, the empty string is returned.</summary>
		public static string Qualifier(string AName)
		{
			AName = EnsureUnrooted(AName);
			int LIndex = AName.IndexOf(Keywords.Qualifier);
			if (LIndex >= 0)
				return AName.Substring(0, LIndex);
			else
				return String.Empty;
		}
		
		/// <summary>Returns the given name with the given qualifier removed.  If the name does not begin with the given qualifier, the given name is returned unchanged.  If the name is equal to the given qualifier, the empty string is returned.</summary>
		public static string RemoveQualifier(string AName, string AQualifier)
		{
			int LIndex = AName.IndexOf(AQualifier);
			if (LIndex == 0)
				if (AName.Length == AQualifier.Length)
					return String.Empty;
				else
					return AName.Substring(AQualifier.Length + 1);
			else
				return AName;
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
		public static string EnsureDescriptionLength(string ADescription)
		{
			if (ADescription.Length > CMaxDescriptionLength)
				return ADescription.Substring(0, CMaxDescriptionLength - CEllipsis.Length) + CEllipsis;
			return ADescription;
		}
		
		/// <summary>Ensures that the given name is not longer than the maximum object name length.</summary>
		/// <remarks>
		/// If the given name is longer than the maximum object name length (200 characters), the first 200 characters of the
		/// name are returned. Otheriwse, the name is returned unchanged.
		/// </remarks>
		public static string EnsureNameLength(string AName)
		{
			if (AName.Length > CMaxObjectNameLength)
				return AName.Substring(0, CMaxObjectNameLength);
			return AName;
		}
		
		public static string GetGeneratedName(string ASeed, int AObjectID)
		{
			return String.Format("{0}{1}", ASeed.Length > CMaxGeneratedNameLength ? ASeed.Substring(0, CMaxGeneratedNameLength) : ASeed, AObjectID.ToString().PadLeft(CMaxObjectIDLength, '0'));
		}
		
		public static string NameFromGuid(Guid AID)
		{
			return String.Format("Object_{0}", AID.ToString().Replace("-", "_"));
		}

		private static int FNextID = 0;		
		public static int GetNextObjectID()
		{
			return Interlocked.Increment(ref FNextID);
		}
		
		public static void SetNextObjectID(int ANextID)
		{
			lock (typeof(Schema.Object))
			{
				FNextID = ANextID;
			}
		}
		
		/// <summary>Gets the object id from the given meta data and removes the DAE.ObjectID tag, if it exists. Otherwise, returns the value of GetNextObjectID().</summary>
		public static int GetObjectID(MetaData AMetaData)
		{
			Tag LTag = MetaData.RemoveTag(AMetaData, "DAE.ObjectID");
			if (LTag != Tag.None)
				return Int32.Parse(LTag.Value);
			return GetNextObjectID();
		}

		/// <summary>Ensures that the object has metadata and a DAE.ObjectID tag with the id of the object.</summary>		
		public void SaveObjectID()
		{
			if (FMetaData == null)
				FMetaData = new MetaData();
			FMetaData.Tags.AddOrUpdate("DAE.ObjectID", FID.ToString(), true);
		}
		
		public void RemoveObjectID()
		{
			if (FMetaData != null)
				FMetaData.Tags.RemoveTag("DAE.ObjectID");
		}

        public override string ToString()
        {
			return FName == String.Empty ? base.ToString() : FName;
        }

        public override bool Equals(object AObject)
        {
			if ((AObject is Object) && ((((Object)AObject).Name != String.Empty) || Name != String.Empty))
				return NamesEqual(((Object)AObject).Name, Name);
			else
				return base.Equals(AObject);
        }

        public override int GetHashCode()
        {
			return Unqualify(FName).GetHashCode();
        }
        
        public virtual Statement EmitStatement(EmitMode AMode)
        {
			throw new SchemaException(SchemaException.Codes.StatementCannotBeEmitted, GetType().Name);
        }
        
        public virtual Statement EmitDropStatement(EmitMode AMode)
        {
			throw new SchemaException(SchemaException.Codes.DropStatementCannotBeEmitted, GetType().Name);
        }
        
        public ObjectHeader GetHeader()
        {
			return new ObjectHeader(this);
        }
        
		public virtual Object GetObjectFromHeader(ObjectHeader AHeader)
		{
			if (FID == AHeader.ID)
				return this;
			throw new Schema.SchemaException(Schema.SchemaException.Codes.CouldNotResolveObjectHeader, ErrorSeverity.System, AHeader.ID, AHeader.Name);
		}
	}
	
	/// <summary>ObjectHeader</summary>
	public class ObjectHeader : System.Object
	{
		public ObjectHeader(Schema.Object AObject) : base()
		{
			FID = AObject.ID;
			FName = AObject.Name;
			FLibraryName = AObject.Library == null ? String.Empty : AObject.Library.Name;
			FDisplayName = AObject.DisplayName;
			FObjectType = AObject.GetType().Name;
			FIsSystem = AObject.IsSystem;
			FIsRemotable = AObject.IsRemotable;
			FIsGenerated = AObject.IsGenerated;
			FIsATObject = AObject.IsATObject;
			FIsSessionObject = AObject.IsSessionObject;
			FIsPersistent = AObject.IsPersistent;
			FCatalogObjectID = AObject.CatalogObjectID;
			FParentObjectID = AObject.ParentObjectID;
			FGeneratorObjectID = AObject.GeneratorID;
		}
		
		public ObjectHeader
		(
			int AID,
			string AName,
			string ALibraryName,
			string ADisplayName,
			string AObjectType,
			bool AIsSystem,
			bool AIsRemotable,
			bool AIsGenerated,
			bool AIsATObject,
			bool AIsSessionObject,
			bool AIsPersistent,
			int ACatalogObjectID,
			int AParentObjectID,
			int AGeneratorObjectID
		) : base()
		{
			FID = AID;
			FName = AName;
			FLibraryName = ALibraryName;
			FDisplayName = ADisplayName;
			FObjectType = AObjectType;
			FIsSystem = AIsSystem;
			FIsRemotable = AIsRemotable;
			FIsGenerated = AIsGenerated;
			FIsATObject = AIsATObject;
			FIsSessionObject = AIsSessionObject;
			FIsPersistent = AIsPersistent;
			FCatalogObjectID = ACatalogObjectID;
			FParentObjectID = AParentObjectID;
			FGeneratorObjectID = AGeneratorObjectID;
		}
		
		private int FID;
		public int ID { get { return FID; } }
		
		private string FName;
		public string Name { get { return FName; } }
		
		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private string FDisplayName;
		public string DisplayName { get { return FDisplayName; } }
		
		private string FObjectType;
		public string ObjectType { get { return FObjectType; } }
		
		private bool FIsSystem;
		public bool IsSystem { get { return FIsSystem; } }
		
		private bool FIsRemotable;
		public bool IsRemotable { get { return FIsRemotable; } }
		
		private bool FIsGenerated;
		public bool IsGenerated { get { return FIsGenerated; } }
		
		private bool FIsATObject;
		public bool IsATObject { get { return FIsATObject; } }
		
		private bool FIsSessionObject;
		public bool IsSessionObject { get { return FIsSessionObject; } }
		
		private bool FIsPersistent;
		public bool IsPersistent { get { return FIsPersistent; } }
		
		private int FCatalogObjectID;
		public int CatalogObjectID { get { return FCatalogObjectID; } }
		
		private int FParentObjectID;
		public int ParentObjectID { get { return FParentObjectID; } }
		
		private int FGeneratorObjectID;
		public int GeneratorObjectID { get { return FGeneratorObjectID; } }
		
		public override int GetHashCode()
		{
			return FID.GetHashCode();
		}

		public override bool Equals(object AObject)
		{
			ObjectHeader LObject = AObject as ObjectHeader;
			return (LObject != null) && (FID == LObject.ID);
		}
		
		[Reference]
		private Schema.Object FObject;
		public Schema.Object ResolveObject(ServerProcess AProcess)
		{
			if (FObject == null)
				FObject = AProcess.CatalogDeviceSession.ResolveObject(FID);

			return FObject;
		}
	}
	
	public class DependentObjectHeaders : List<DependentObjectHeader>
	{
		/// <summary>Returns true if this list contains a header with the given ID.</summary>
		public bool Contains(int AObjectID)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].ID == AObjectID)
					return true;
			return false;
		}
	}
	
	/// <summary>DependentObjectHeader</summary>
	public class DependentObjectHeader : ObjectHeader
	{
		public DependentObjectHeader
		(
			int AID,
			string AName,
			string ALibraryName,
			string ADisplayName,
			string ADescription,
			string AObjectType,
			bool AIsSystem,
			bool AIsRemotable,
			bool AIsGenerated,
			bool AIsATObject,
			bool AIsSessionObject,
			bool AIsPersistent,
			int ACatalogObjectID,
			int AParentObjectID,
			int AGeneratorObjectID,
			int ALevel,
			int ASequence
		) 
			: base
			(
				AID,
				AName,
				ALibraryName,
				ADisplayName,
				AObjectType,
				AIsSystem,
				AIsRemotable,
				AIsGenerated,
				AIsATObject,
				AIsSessionObject,
				AIsPersistent,
				ACatalogObjectID,
				AParentObjectID,
				AGeneratorObjectID
			) 
		{
			FDescription = ADescription;
			FLevel = ALevel;
			FSequence = ASequence;
		}
		
		private string FDescription;
		public string Description { get { return FDescription; } }

		private int FLevel;
		public int Level { get { return FLevel; } }
		
		private int FSequence;
		public int Sequence { get { return FSequence; } }		
	}
	
	public class PersistentObjectHeaders : List<PersistentObjectHeader> { }
    
	public class PersistentObjectHeader : System.Object
	{
		public PersistentObjectHeader
		(
			int AID,
			string AName,
			string ALibraryName,
			string AScript,
			string ADisplayName,
			string AObjectType,
			bool AIsSystem,
			bool AIsRemotable,
			bool AIsGenerated,
			bool AIsATObject,
			bool AIsSessionObject
		) : base()
		{
			FID = AID;
			FName = AName;
			FLibraryName = ALibraryName;
			FScript = AScript;
			FDisplayName = ADisplayName;
			FObjectType = AObjectType;
			FIsSystem = AIsSystem;
			FIsRemotable = AIsRemotable;
			FIsGenerated = AIsGenerated;
			FIsATObject = AIsATObject;
			FIsSessionObject = AIsSessionObject;
		}
		
		private int FID;
		public int ID { get { return FID; } }
		
		private string FName;
		public string Name { get { return FName; } }
		
		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private string FScript;
		public string Script { get { return FScript; } }

		private string FDisplayName;
		public string DisplayName { get { return FDisplayName; } }
		
		private string FObjectType;
		public string ObjectType { get { return FObjectType; } }
		
		private bool FIsSystem;
		public bool IsSystem { get { return FIsSystem; } }
		
		private bool FIsRemotable;
		public bool IsRemotable { get { return FIsRemotable; } }
		
		private bool FIsGenerated;
		public bool IsGenerated { get { return FIsGenerated; } }
		
		private bool FIsATObject;
		public bool IsATObject { get { return FIsATObject; } }
		
		private bool FIsSessionObject;
		public bool IsSessionObject { get { return FIsSessionObject; } }
	}
	
	public class CatalogObjectHeaders : List<CatalogObjectHeader> { }
	
	public class CatalogObjectHeader : System.Object
	{
		public CatalogObjectHeader
		(
			int AID,
			string AName,
			string ALibraryName,
			string AOwnerID
		) : base()
		{
			FID = AID;
			FName = AName;
			FLibraryName = ALibraryName;
			FOwnerID = AOwnerID;
		}
		
		private int FID;
		public int ID { get { return FID; } }
		
		private string FName;
		public string Name { get { return FName; } }
		
		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private string FOwnerID;
		public string OwnerID { get { return FOwnerID; } }
	}
	
	public class ScalarTypeHeader : System.Object
	{
		public ScalarTypeHeader(int AID, int AUniqueSortID, int ASortID) : base()
		{
			FID = AID;
			FUniqueSortID = AUniqueSortID;
			FSortID = ASortID;
		}
		
		private int FID;
		public int ID { get { return FID; } }
		
		private int FUniqueSortID;
		public int UniqueSortID { get { return FUniqueSortID; } }
		
		private int FSortID;
		public int SortID { get { return FSortID; } }
	}
	
	public class FullObjectHeaders : System.Object
	{
		private List<FullObjectHeader> FHeaders = new List<FullObjectHeader>();
		
		/// <summary>Stores the index of the header with a given ID.</summary>
		private Dictionary<int, int> FHeaderHash = new Dictionary<int, int>();
		
		public void Add(FullObjectHeader AHeader)
		{
			FHeaders.Add(AHeader);
			FHeaderHash.Add(AHeader.ID, FHeaders.Count - 1);
		}
		
		public int Count { get { return FHeaders.Count; } }
		
		public FullObjectHeader this[int AIndex] { get { return FHeaders[AIndex]; } }
		
		/// <summary>Returns true if this list contains a header with the given ID.</summary>
		public bool Contains(int AObjectID)
		{
			return FHeaderHash.ContainsKey(AObjectID);
		}
	}
	
	/// <summary>FullObjectHeader</summary>
	public class FullObjectHeader : System.Object
	{
		public FullObjectHeader
		(
			int AID,
			string AName,
			string ALibraryName,
			string AScript,
			string ADisplayName,
			string AObjectType,
			bool AIsSystem,
			bool AIsRemotable,
			bool AIsGenerated,
			bool AIsATObject,
			bool AIsSessionObject,
			bool AIsPersistent,
			int ACatalogObjectID,
			int AParentObjectID,
			int AGeneratorObjectID
		) : base()
		{
			FID = AID;
			FName = AName;
			FLibraryName = ALibraryName;
			FScript = AScript;
			FDisplayName = ADisplayName;
			FObjectType = AObjectType;
			FIsSystem = AIsSystem;
			FIsRemotable = AIsRemotable;
			FIsGenerated = AIsGenerated;
			FIsATObject = AIsATObject;
			FIsSessionObject = AIsSessionObject;
			FIsPersistent = AIsPersistent;
			FCatalogObjectID = ACatalogObjectID;
			FParentObjectID = AParentObjectID;
			FGeneratorObjectID = AGeneratorObjectID;
		}
		
		private int FID;
		public int ID { get { return FID; } }
		
		private string FName;
		public string Name { get { return FName; } }
		
		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private string FScript;
		public string Script { get { return FScript; } }

		private string FDisplayName;
		public string DisplayName { get { return FDisplayName; } }
		
		private string FObjectType;
		public string ObjectType { get { return FObjectType; } }
		
		private bool FIsSystem;
		public bool IsSystem { get { return FIsSystem; } }
		
		private bool FIsRemotable;
		public bool IsRemotable { get { return FIsRemotable; } }
		
		private bool FIsGenerated;
		public bool IsGenerated { get { return FIsGenerated; } }
		
		private bool FIsATObject;
		public bool IsATObject { get { return FIsATObject; } }
		
		private bool FIsSessionObject;
		public bool IsSessionObject { get { return FIsSessionObject; } }
		
		private bool FIsPersistent;
		public bool IsPersistent { get { return FIsPersistent; } }
		
		private int FCatalogObjectID;
		public int CatalogObjectID { get { return FCatalogObjectID; } }
		
		private int FParentObjectID;
		public int ParentObjectID { get { return FParentObjectID; } }
		
		private int FGeneratorObjectID;
		public int GeneratorObjectID { get { return FGeneratorObjectID; } }
		
		public override int GetHashCode()
		{
			return FID.GetHashCode();
		}

		public override bool Equals(object AObject)
		{
			FullObjectHeader LObject = AObject as FullObjectHeader;
			return (LObject != null) && (FID == LObject.ID);
		}

		public override string ToString()
		{
			return String.Format("{0}: {1}", FID.ToString(), FDisplayName);
		}
	}
	
	public class FullCatalogObjectHeader : FullObjectHeader
	{
		public FullCatalogObjectHeader
		(
			int AID,
			string AName,
			string ALibraryName,
			string AOwnerID,
			string AScript,
			string ADisplayName,
			string AObjectType,
			bool AIsSystem,
			bool AIsRemotable,
			bool AIsGenerated,
			bool AIsATObject,
			bool AIsSessionObject,
			int AGeneratorObjectID
		) : base(AID, AName, ALibraryName, AScript, ADisplayName, AObjectType, AIsSystem, AIsRemotable, AIsGenerated, AIsATObject, AIsSessionObject, true, -1, -1, AGeneratorObjectID)
		{
			FOwnerID = AOwnerID;
		}
		
		private string FOwnerID;
		public string OwnerID { get { return FOwnerID; } }
	}
	
	#if USETYPEDLIST
    public class IntegerList : System.Object
    {
		public const int CDefaultInitialCapacity = 4;
		
		private int[] FItems;

		private int FCount;
		public int Count { get { return FCount; } }
		
		public IntegerList() : base()
		{
			FItems = new int[CDefaultInitialCapacity];
		}
		
		public int this[int AIndex]
		{
			get
			{
				if ((AIndex < 0) || (AIndex >= FCount))
					throw new SchemaException(SchemaException.Codes.IndexOutOfRange, AIndex);
				return FItems[AIndex];
			}
			set { FItems[AIndex] = value; }
		}
		
		public int Add(int AValue)
		{
			int LIndex = Count;
			Insert(LIndex, AValue);
			return LIndex;
		}
		
		public void AddRange(IntegerList ARange)
		{
			for (int LIndex = 0; LIndex < ARange.Count; LIndex++)
				Add(ARange[LIndex]);
		}
		
		public void Insert(int AIndex, int AValue)
		{
			if (FCount >= FItems.Length)
				InternalSetCapacity(FItems.Length * 2);
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = AValue;
			FCount++;
		}
		
		protected void InternalSetCapacity(int AValue)
		{
			if (FItems.Length != AValue)
			{
				int[] LNewItems = new int[AValue];
				for (int LIndex = 0; LIndex < ((FCount > AValue) ? AValue : FCount); LIndex++)
					LNewItems[LIndex] = FItems[LIndex];

				if (FCount > AValue)						
					for (int LIndex = FCount - 1; LIndex > AValue; LIndex--)
						RemoveAt(LIndex);
						
				FItems = LNewItems;
			}
		}
		
		public int Capacity
		{
			get { return FItems.Length; }
			set { InternalSetCapacity(value); }
		}
		
		public void RemoveAt(int AIndex)
		{
			FCount--;			
			for (int LIndex = AIndex; LIndex < FCount; LIndex++)
				FItems[LIndex] = FItems[LIndex + 1];
			//FItems[FCount] = null; // Clear the last item to prevent a resource leak
		}
		
		public void Remove(int AValue)
		{
			RemoveAt(IndexOf(AValue));
		}
		
		public void SafeRemove(int AValue)
		{
			int LIndex = IndexOf(AValue);
			if (LIndex >= 0)
				RemoveAt(LIndex);
		}
		
		public void Clear()
		{
			while (FCount > 0)
				RemoveAt(FCount - 1);
		}
		
		public int IndexOf(int AValue)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (FItems[LIndex] == AValue)
					return LIndex;
			return -1;
		}
		
		public bool Contains(int AValue)
		{
			return IndexOf(AValue) >= 0;
		}
    }

    public class Hashtables : System.Object
    {
		public const int CDefaultInitialCapacity = 4;
		
		private Hashtable[] FItems;

		private int FCount;
		public int Count { get { return FCount; } }
		
		public Hashtables() : base()
		{
			FItems = new Hashtable[CDefaultInitialCapacity];
		}
		
		public Hashtable this[int AIndex]
		{
			get
			{
				if ((AIndex < 0) || (AIndex >= FCount))
					throw new SchemaException(SchemaException.Codes.IndexOutOfRange, AIndex);
				return FItems[AIndex];
			}
			set { FItems[AIndex] = value; }
		}
		
		public int Add(Hashtable AValue)
		{
			int LIndex = Count;
			Insert(LIndex, AValue);
			return LIndex;
		}
		
		public void Insert(int AIndex, Hashtable AValue)
		{
			if (FCount >= FItems.Length)
				InternalSetCapacity(FItems.Length * 2);
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = AValue;
			FCount++;
		}
		
		protected void InternalSetCapacity(int AValue)
		{
			if (FItems.Length != AValue)
			{
				Hashtable[] LNewItems = new Hashtable[AValue];
				for (int LIndex = 0; LIndex < ((FCount > AValue) ? AValue : FCount); LIndex++)
					LNewItems[LIndex] = FItems[LIndex];

				if (FCount > AValue)						
					for (int LIndex = FCount - 1; LIndex > AValue; LIndex--)
						RemoveAt(LIndex);
						
				FItems = LNewItems;
			}
		}
		
		public int Capacity
		{
			get { return FItems.Length; }
			set { InternalSetCapacity(value); }
		}
		
		public void RemoveAt(int AIndex)
		{
			FCount--;			
			for (int LIndex = AIndex; LIndex < FCount; LIndex++)
				FItems[LIndex] = FItems[LIndex + 1];
			FItems[FCount] = null; // Clear the last item to prevent a resource leak
		}
		
		public void Remove(Hashtable AValue)
		{
			RemoveAt(IndexOf(AValue));
		}
		
		public void SafeRemove(Hashtable AValue)
		{
			int LIndex = IndexOf(AValue);
			if (LIndex >= 0)
				RemoveAt(LIndex);
		}
		
		public void Clear()
		{
			while (FCount > 0)
				RemoveAt(FCount - 1);
		}
		
		public int IndexOf(Hashtable AValue)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (FItems[LIndex] == AValue)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Hashtable AValue)
		{
			return IndexOf(AValue) >= 0;
		}
    }
	#else
	public class IntegerList : BaseList<int> { }

	public class Hashtables : BaseList<Hashtable> { }
	#endif

    public class Objects : System.Object, IList
    {
		private const int CDefaultInitialCapacity = 0;
		private const int CDefaultLowerBoundGrowth = 1;
		private const int CDefaultRolloverCount = 100;
		
		private Object[] FItems;
		private int FCount;
		private int FInitialCapacity;
		
		public Objects() : base()
		{
			FInitialCapacity = CDefaultInitialCapacity;
		}
		
		public Objects(int AInitialCapacity) : base()
		{
			FInitialCapacity = AInitialCapacity;
		}
		
		public Object this[int AIndex] 
		{ 
			get
			{ 
				if ((AIndex < 0) || (AIndex >= Count))
					throw new SchemaException(SchemaException.Codes.IndexOutOfRange, AIndex);
				return FItems[AIndex];
			} 
			set
			{ 
				lock (this)
				{
					InternalRemoveAt(AIndex);
					InternalInsert(AIndex, value);
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
		public Object this[string AName]
		{
			get
			{
				lock (this)
				{
					int LIndex = InternalIndexOf(AName, true);
					if (LIndex >= 0)
						return this[LIndex];
					throw new SchemaException(SchemaException.Codes.ObjectNotFound, AName);
				}
			}
			set
			{
				lock (this)
				{
					int LIndex = InternalIndexOf(AName, true);
					if (LIndex >= 0)
						this[LIndex] = value;
					throw new SchemaException(SchemaException.Codes.ObjectNotFound, AName);
				}
			}
		}
		#endif

		protected int InternalIndexOfName(string AName)
		{
			if (AName.IndexOf(Keywords.Qualifier) == 0)
				AName = AName.Substring(1);

			if (IsRolledOver)
			{
				IntegerList LNameBucket = (IntegerList)GetNameIndexForDepth(0)[AName];
				return (LNameBucket == null) || (LNameBucket.Count == 0) ? -1 : LNameBucket[0];
			}
			else
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (String.Compare(this[LIndex].Name, AName) == 0)
						return LIndex;
				return -1;
			}
		}

		public int IndexOfName(string AName)
		{
			lock (this)
			{
				return InternalIndexOfName(AName);
			}
		}
		
		protected int InternalIndexOf(string AName, bool AThrowIfAmbiguous)
		{
			StringCollection LNames = new StringCollection();
			int LObjectIndex = InternalIndexOf(AName, LNames);
			if ((LObjectIndex < 0) && (LNames.Count > 1) && AThrowIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LObjectIndex;
		}
		
		protected IntegerList InternalIndexesOf(string AName)
		{
			int LObjectIndex;
			IntegerList LIndexes = new IntegerList();
			if ((AName.Length > 0) && (AName[0] == '.'))
			{
				LObjectIndex = InternalIndexOfName(AName);
				if (LObjectIndex >= 0)
					LIndexes.Add(LObjectIndex);
				return LIndexes;
			}

			if (IsRolledOver)
			{
				int LDepth = Schema.Object.GetQualifierCount(AName);
				if (LDepth > FNameIndex.Count)
					return LIndexes;
					
				IntegerList LNameBucket;
				for (int LIndex = FNameIndex.Count - 1 - LDepth; LIndex >= 0; LIndex--)
				{
					LNameBucket = (IntegerList)GetNameIndexForDepth(LIndex)[AName];
					if (LNameBucket != null)
						LIndexes.AddRange(LNameBucket);
				}
				return LIndexes;
			}
			else
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (Object.NamesEqual(this[LIndex].Name, AName))
						LIndexes.Add(LIndex);
				return LIndexes;
			}
		}
		
		public IntegerList IndexesOf(string AName)
		{
			lock (this)
			{
				return InternalIndexesOf(AName);
			}
		}
		
		protected int InternalIndexOf(string AName, StringCollection ANames)
		{
			IntegerList LIndexes = InternalIndexesOf(AName);
			for (int LIndex = 0; LIndex < LIndexes.Count; LIndex++)
				ANames.Add(this[LIndexes[LIndex]].Name);
			return (LIndexes.Count == 1) ? LIndexes[0] : -1;
		}

		public int IndexOf(string AName)
		{
			return IndexOf(AName, true);
		}
		
		public int IndexOf(string AName, bool AThrowIfAmbiguous)
		{
			StringCollection LNames = new StringCollection();
			int LIndex = IndexOf(AName, LNames);
			if ((LIndex < 0) && (LNames.Count > 1) && AThrowIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LIndex;
		}

		public int IndexOf(string AName, StringCollection ANames)
		{
			lock (this)
			{
				return InternalIndexOf(AName, ANames);
			}
		}
		
		#if SINGLENAMESPACE
		protected int InternalIndexOf(string AName, string ANameSpace, bool AThrowIfAmbiguous)
		{
			StringCollection LNames = new StringCollection();
			int LObjectIndex = InternalIndexOf(AName, ANameSpace, LNames);
			if ((LObjectIndex < 0) && (LNames.Count > 1) && AThrowIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LObjectIndex;
		}
		
		protected int InternalIndexOf(string AName, string ANameSpace, StringCollection ANames)
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
			StringCollection LNames = new StringCollection();
			int LIndex = IndexOf(AName, ANameSpace, LNames);
			if ((LIndex < 0) && (LNames.Count > 1) && AThrowIfAmbiguous)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LIndex;
		}
		
		public int IndexOf(string AName, string ANameSpace, StringCollection ANames)
		{
			lock (this)
			{
				return InternalIndexOf(AName, ANameSpace, ANames);
			}
		}
		#endif
		
		public int ResolveName(string AName, NameResolutionPath APath, StringCollection ANames)
		{
			lock (this)
			{
				IntegerList LIndexes = InternalIndexesOf(AName);
				
				if (!Schema.Object.IsRooted(AName))
				{
					IntegerList LLevelIndexes = new IntegerList();
					Schema.Object LObject;
					
					for (int LLevelIndex = 0; LLevelIndex < APath.Count; LLevelIndex++)
					{
						if (LLevelIndex > 0)
							LLevelIndexes.Clear();
							
						for (int LIndex = 0; LIndex < LIndexes.Count; LIndex++)
						{
							LObject = this[LIndexes[LIndex]];
							if ((LObject.Library == null) || APath[LLevelIndex].ContainsName(LObject.Library.Name))
								LLevelIndexes.Add(LIndexes[LIndex]);
						}
						
						if (LLevelIndexes.Count > 0)
						{
							for (int LIndex = 0; LIndex < LLevelIndexes.Count; LIndex++)
								ANames.Add(this[LLevelIndexes[LIndex]].Name);
								
							return LLevelIndexes.Count == 1 ? LLevelIndexes[0] : -1;
						}
					}
				}
				
				if (LIndexes.Count > 0)
				{
					for (int LIndex = 0; LIndex < LIndexes.Count; LIndex++)
						ANames.Add(this[LIndexes[LIndex]].Name);
						
					return LIndexes.Count == 1 ? LIndexes[0] : -1;
				}
				
				return -1;
			}
		}
		
		public int ResolveName(string AName, NameResolutionPath APath)
		{
			StringCollection LNames = new StringCollection();
			int LIndex = ResolveName(AName, APath, LNames);
			if ((LIndex < 0) && (LNames.Count > 1))
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectReference, AName, ExceptionUtility.StringsToCommaList(LNames));
			return LIndex;
		}
		
		protected int InternalIndexOf(Object AItem)
		{
			return InternalIndexOfName(AItem.Name);
		}

		public int IndexOf(Object AItem)
		{
			lock (this)
			{
				return InternalIndexOf(AItem);
			}
		}
		
		public bool Contains(string AName)
		{
			return (IndexOf(AName) >= 0);
		}
		
		public bool ContainsName(string AName)
		{
			return (IndexOfName(AName) >= 0);
		}
		
		public bool Contains(Object AItem)
		{
			return IndexOf(AItem) >= 0;
		}
		
		public int Add(object AItem)
		{
			lock (this)
			{
				int LIndex = Count;
				InternalInsert(LIndex, AItem);
				return LIndex;
			}
		}
		
		public void AddRange(ICollection ACollection)
		{
			foreach (object AObject in ACollection)
				Add(AObject);
		}
		
		protected void InternalInsert(int AIndex, object AItem)
		{
			Object LObject = AItem as Object;
			if (LObject == null)
				throw new SchemaException(SchemaException.Codes.ObjectContainer);

			#if USEOBJECTVALIDATE
			Validate(LObject);
			#endif
			
			if (FItems == null)
				FItems = new Object[FInitialCapacity];
			
			if (FCount >= FItems.Length)
				InternalSetCapacity(FItems.Length * 2);
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = LObject;
			FCount++;

			Adding(LObject, AIndex);
		}
		
		public void Insert(int AIndex, object AItem)
		{
			lock (this)
			{
				InternalInsert(AIndex, AItem);
			}
		}
		
		protected void InternalSetCapacity(int AValue)
		{
			if (AValue <= 0)
				AValue = CDefaultLowerBoundGrowth;
				
			if (FItems.Length != AValue)
			{
				Object[] LNewItems = new Object[AValue];
				for (int LIndex = 0; LIndex < ((FCount > AValue) ? AValue : FCount); LIndex++)
					LNewItems[LIndex] = FItems[LIndex];

				if (FCount > AValue)						
					for (int LIndex = FCount - 1; LIndex > AValue; LIndex--)
						RemoveAt(LIndex);
						
				FItems = LNewItems;
			}
		}
		
		public int Capacity
		{
			get { return FItems == null ? FInitialCapacity : FItems.Length; }
			set
			{
				lock (this)
				{
					if (FItems == null)
						FItems = new Object[value];
					InternalSetCapacity(value);
				}
			}
		}
		
		protected void InternalRemoveAt(int AIndex)
		{
			Removing(this[AIndex], AIndex);

			FCount--;			
			for (int LIndex = AIndex; LIndex < FCount; LIndex++)
				FItems[LIndex] = FItems[LIndex + 1];
			FItems[FCount] = null; // Clear the last item to prevent a resource leak
		}
		
		public void RemoveAt(int AIndex)
		{
			lock (this)
			{
				InternalRemoveAt(AIndex);
			}
		}
		
		public void Remove(Object AValue)
		{
			lock (this)
			{
				InternalRemoveAt(InternalIndexOf(AValue));
			}
		}
		
		public void SafeRemove(Object AValue)
		{
			if (AValue != null)
			{
				lock (this)
				{
					int LIndex = InternalIndexOf(AValue);
					if (LIndex >= 0)
						InternalRemoveAt(LIndex);
				}
			}
		}
		
		public void SafeRemove(string AName)
		{
			lock (this)
			{
				int LIndex = InternalIndexOfName(AName);
				if (LIndex >= 0)
					InternalRemoveAt(LIndex);
			}
		}
		
		public void Clear()
		{
			lock (this)
			{
				while (FCount > 0)
					InternalRemoveAt(FCount - 1);
			}
		}
		
		protected virtual void Validate(Object AObject)
		{
			#if USEOBJECTVALIDATE
			if (AObject.Name == String.Empty)
				throw new SchemaException(SchemaException.Codes.ObjectNameRequired);
				
			string LObjectName = AObject.Name;
			#if DISALLOWAMBIGUOUSNAMES
			int LIndex = InternalIndexOf(LObjectName, true);
			#else
			int LIndex = InternalIndexOfName(LObjectName);
			#endif
			if (LIndex >= 0)
			{
				if (String.Compare(this[LIndex].Name, LObjectName) == 0)
					throw new SchemaException(SchemaException.Codes.DuplicateObjectName, LObjectName);
				#if DISALLOWAMBIGUOUSNAMES
				else
					throw new SchemaException(SchemaException.Codes.AmbiguousObjectName, LObjectName, this[LIndex].Name);
				#endif
			}
			#endif
		
			#if DISALLOWAMBIGUOUSNAMES
			IntegerList LIndexes;
			while (LObjectName.IndexOf(Keywords.Qualifier) >= 0)
			{
				LObjectName = Schema.Object.Dequalify(LObjectName);
				LIndexes = IndexesOf(LObjectName);
				for (int LObjectIndex = 0; LObjectIndex < LIndexes.Count; LObjectIndex++)
					if (String.Compare(this[LIndexes[LObjectIndex]].Name, LObjectName) == 0)
						throw new SchemaException(SchemaException.Codes.CreatingAmbiguousObjectName, AObject.Name, LObjectName);
			}
			#endif
		}
		
		public bool IsValidObjectName(string AName, StringCollection ANames)
		{
			string LObjectName = AName;
			
			int LIndex = InternalIndexOf(LObjectName, true);
			if (LIndex >= 0)
			{
				#if DISALLOWAMBIGUOUSNAMES
				ANames.Add(this[LIndex].Name);
				return false;
				#else
				if (String.Compare(AName, this[LIndex].Name) == 0)
				{
					ANames.Add(this[LIndex].Name);
					return false;
				}
				#endif
			}

			#if DISALLOWAMBIGUOUSNAMES			
			IntegerList LIndexes;
			while (LObjectName.IndexOf(Keywords.Qualifier) >= 0)
			{
				LObjectName = Schema.Object.Dequalify(LObjectName);
				LIndexes = IndexesOf(LObjectName);
				for (int LObjectIndex = 0; LObjectIndex < LIndexes.Count; LObjectIndex++)
					if (String.Compare(this[LIndexes[LObjectIndex]].Name, LObjectName) == 0)
						ANames.Add(LObjectName);
			}
			#endif
			
			return ANames.Count == 0;
		}
		
		protected virtual void Adding(Object AObject, int AIndex)
		{
			if (IsRolledOver)
			{
				for (int LIndex = AIndex + 1; LIndex < Count; LIndex++)
					UpdateObjectIndex(this[LIndex], LIndex - 1, LIndex);
				IndexObject(AObject, AIndex);
			}
			else
				CheckRollover();
		}
		
		protected virtual void Removing(Object AObject, int AIndex)
		{
			if (IsRolledOver)
			{
				UnindexObject(AObject, AIndex);
				for (int LIndex = AIndex + 1; LIndex < Count; LIndex++)
					UpdateObjectIndex(this[LIndex], LIndex, LIndex - 1);
			}
		}

		protected Hashtables FNameIndex;
		
		protected int FRolloverCount = CDefaultRolloverCount;
		/// <value>
		/// An integer value indicating at what size to begin maintenance of secondary indexes on the objects in the list.  
		/// A value of Int32.MaxValue indicates no maintenance is to be performed.  This value is defaulted to 20.
		/// </value>
		[DefaultValue(CDefaultRolloverCount)]
		public int RolloverCount
		{
			get { return FRolloverCount; }
			set
			{
				lock (this)
				{
					FRolloverCount = value;
					CheckRollover();
				}
			}
		}
		
		public bool IsRolledOver { get { return FNameIndex != null; } }
		
		protected void CheckRollover()
		{
			if (Count > FRolloverCount)
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
			FNameIndex = new Hashtables();
			for (int LIndex = 0; LIndex < Count; LIndex++)
				IndexObject(this[LIndex], LIndex);
		}
		
		protected void Rollunder()
		{
			FNameIndex = null;
		}
		
		protected Hashtable GetNameIndexForDepth(int ADepth)
		{
			while (ADepth > FNameIndex.Count - 1)
				FNameIndex.Add(new Hashtable());
			return FNameIndex[ADepth];
		}
		
		protected void IndexObject(Object AObject, int AIndex)
		{
			// Add the object to the name index
			string LName = AObject.Name;
			int LDepth = Schema.Object.GetQualifierCount(LName);
			IntegerList LNameBucket;
			Hashtable LNameIndex;
			for (int LIndex = 0; LIndex <= LDepth; LIndex++)
			{
				LNameIndex = GetNameIndexForDepth(LIndex);
				LNameBucket = (IntegerList)LNameIndex[LName];
				if (LNameBucket == null)
				{
					LNameBucket = new IntegerList();
					LNameIndex.Add(LName, LNameBucket);
				}
				LNameBucket.Add(AIndex);
				LName = Object.Dequalify(LName);
			}
		}
		
		protected void UnindexObject(Object AObject, int AIndex)
		{
			// Remove the object from the name index
			string LName = AObject.Name;
			int LDepth = Schema.Object.GetQualifierCount(LName);
			IntegerList LNameBucket;
			Hashtable LNameIndex;
			for (int LIndex = 0; LIndex <= LDepth; LIndex++)
			{
				LNameIndex = GetNameIndexForDepth(LIndex);
				LNameBucket = (IntegerList)LNameIndex[LName];
				if (LNameBucket == null)
					throw new SchemaException(SchemaException.Codes.IndexBucketNotFound, LName);
				LNameBucket.Remove(AIndex);
				if (LNameBucket.Count == 0)
					LNameIndex.Remove(LName);
				LName = Object.Dequalify(LName);
			}
		}
		
		protected void UpdateObjectIndex(Object AObject, int AOldIndex, int ANewIndex)
		{
			// Update the objects index in the Name index
			string LName = AObject.Name;
			int LDepth = Schema.Object.GetQualifierCount(LName);
			IntegerList LNameBucket;
			Hashtable LNameIndex;
			for (int LIndex = 0; LIndex <= LDepth; LIndex++)
			{
				LNameIndex = GetNameIndexForDepth(LIndex);
				LNameBucket = (IntegerList)LNameIndex[LName];
				if (LNameBucket == null)
					throw new SchemaException(SchemaException.Codes.IndexBucketNotFound, LName);
				LNameBucket.Remove(AOldIndex);
				LNameBucket.Add(ANewIndex);
				LName = Object.Dequalify(LName);
			}
		}
		
		// IList
		object IList.this[int AIndex] { get { return this[AIndex]; } set { this[AIndex] = (Object)value; } }
		int IList.IndexOf(object AItem) { return (AItem is Object) ? IndexOf((Object)AItem) : -1; }
		bool IList.Contains(object AItem) { return (AItem is Object) ? Contains((Object)AItem) : false; }
		void IList.Remove(object AItem) { RemoveAt(IndexOf((Object)AItem)); }
		bool IList.IsFixedSize { get { return false; } }
		bool IList.IsReadOnly { get { return false; } }
		
		// ICollection
		public int Count { get { return FCount; } }
		public bool IsSynchronized { get { return false; } }
		public object SyncRoot { get { return this; } }
		public void CopyTo(Array AArray, int AIndex)
		{
			IList LArray = (IList)AArray;
			for (int LIndex = 0; LIndex < Count; LIndex++)
				LArray[AIndex + LIndex] = this[LIndex];
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
			public SchemaObjectEnumerator(Objects AObjects) : base()
			{
				FObjects = AObjects;
			}
			
			private Objects FObjects;
			private int FCurrent =  -1;

			public Object Current { get { return FObjects[FCurrent]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				FCurrent++;
				return (FCurrent < FObjects.Count);
			}
			
			public void Reset()
			{
				FCurrent = -1;
			}
		}
    }

	public abstract class CatalogObject : Object
	{
		public CatalogObject(string AName) : base(AName) {}
		public CatalogObject(int AID, string AName) : base(AID, AName) {}
		
		// Name
		public override string Name
		{
			get { return base.Name; }
			set
			{
				if (value.Length > CMaxObjectNameLength)
					throw new SchemaException(SchemaException.Codes.MaxObjectNameLengthExceeded, value, CMaxObjectNameLength);
				base.Name = value;
			}
		}
		
		// Owner
		[Reference]
		protected User FOwner;
		public User Owner
		{
			get { return FOwner; }
			set { FOwner = value; }
		}
		
		// CatalogObjectID
		public override int CatalogObjectID	{ get { return -1; } }

		// ParentObjectID
		public override int ParentObjectID { get { return -1; } }
		
		// IsSystem
		public override bool IsSystem 
		{ 
			get { return (FOwner != null) && (FOwner.ID == Server.Server.CSystemUserID); }
			set { throw new RuntimeException(RuntimeException.Codes.InternalError, "Cannot set IsSystem for a Catalog Object"); } 
		}
		
		/// <summary>Returns true if AUser is the owner of this object, or is a member of a parent Group of the owner of this object, recursively.</summary>
		public bool IsOwner(User AUser)
		{
			if ((AUser.ID == Server.Server.CSystemUserID) || (AUser.ID == Server.Server.CAdminUserID) || (AUser.ID == FOwner.ID)) 
				return true;
				
			return false;
		}
		
		public virtual string[] GetRights()
		{
			return new string[]{};
		}
		
		public virtual string GetRight(string ARightName)
		{
			return Name + ARightName;
		}

		// SessionObjectName
		private string FSessionObjectName;
		public string SessionObjectName
		{
			get { return FSessionObjectName; }
			set { FSessionObjectName = value; }
		}

		// SessionID
		private int FSessionID;
		public int SessionID
		{
			get { return FSessionID; }
			set { FSessionID = value; }
		}
		
		public override bool IsSessionObject { get { return FSessionObjectName != null; } }
		
		/// <summary>Catalog objects are always persistent.</summary>
		public override bool IsPersistent { get { return true; } }
		
		public override string DisplayName { get { return FSessionObjectName == null ? Name : FSessionObjectName; } }
	}
    
    public class SessionObject : Schema.Object
    {
		public SessionObject(string AName) : base(AName) {}
		public SessionObject(string AName, string AGlobalName) : base(AName)
		{
			GlobalName = AGlobalName;
		}
		
		private string FGlobalName;
		public string GlobalName
		{
			get { return FGlobalName; }
			set { FGlobalName = value == null ? String.Empty : value; }
		}
    }
}
