/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define REQUIRECASEMATCHONRESOLVE // Use this to require a case match when resolving identifiers (see IBAS Proposal #26889)

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Store.SQLCE;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Connection;

// TODO: Study the performance impact of using literals vs. parameterized statements, including the impact of the required String.Replace calls

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	internal abstract class StoreObjectHeader
	{
		public StoreObjectHeader(string AName) : base()
		{
			FName = AName;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		public override int GetHashCode()
		{
			return FName.GetHashCode();
		}

		public override bool Equals(object AObject)
		{
			StoreObjectHeader LHeader = AObject as StoreObjectHeader;
			return (LHeader != null) && (LHeader.Name == FName);
		}
	}
	
	internal class StoreTableHeader : StoreObjectHeader
	{
		public StoreTableHeader(string ATableName, string APrimaryKeyName) : base(ATableName)
		{
			FPrimaryKeyName = APrimaryKeyName;
		}
		
		private string FPrimaryKeyName;
		public string PrimaryKeyName { get { return FPrimaryKeyName; } }
	}
	
	internal class StoreIndexHeader : StoreObjectHeader
	{
		public StoreIndexHeader(string ATableName, string AIndexName, List<string> AColumns) : base(AIndexName)
		{
			FTableName = ATableName;
			FColumns = AColumns;
		}
		
		private string FTableName;
		public string TableName { get { return FTableName; } }

		private List<string> FColumns;
		public List<string> Columns { get { return FColumns; } }
	}
	
	internal class StoreTableHeaders : Dictionary<string, StoreTableHeader> {}

	internal class StoreIndexHeaders : Dictionary<string, StoreIndexHeader> {}

	public class CatalogStore : System.Object
	{
		public CatalogStore()
		{
			FStore = new SQLCEStore();
		}
		
		/// <summary>Initializes the catalog store, ensuring the store has been created.</summary>
		public void Initialize(Server.Server AServer)
		{
			FStore.Initialize();
			
			// Establish a connection to the catalog store server
			SQLStoreConnection LConnection = FStore.Connect();
			try
			{
				// TODO: This needs to be specific to the type of store
				// if there is no DAEServerInfo table
				if ((int)LConnection.ExecuteScalar("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'DAEServerInfo'") == 0)
				{
					// run the SystemStoreCatalog sql script
					using (Stream LStream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.SystemStoreCatalog.sql"))
					{
						LConnection.ExecuteScript(new StreamReader(LStream).ReadToEnd());
					}
					
					LConnection.ExecuteStatement
					(
						String.Format
						(
							"insert into DAEServerInfo (Name, Version, MaxConcurrentProcesses, ProcessWaitTimeout, ProcessTerminationTimeout, PlanCacheSize) values ('{0}', '{1}', {2}, {3}, {4}, {5})",
							AServer.Name.Replace("'", "''"),
							GetType().Assembly.GetName().Version.ToString(),
							AServer.MaxConcurrentProcesses,
							(int)AServer.ProcessWaitTimeout.TotalMilliseconds,
							(int)AServer.ProcessTerminationTimeout.TotalMilliseconds,
							AServer.PlanCacheSize
						)
					);
					
					LConnection.ExecuteStatement(String.Format("insert into DAELoadedLibraries (Library_Name) values ('{0}')", Server.Server.CSystemLibraryName));
					LConnection.ExecuteStatement(String.Format("insert into DAELibraryVersions (Library_Name, VersionNumber) values ('{0}', '{1}')", Server.Server.CSystemLibraryName, GetType().Assembly.GetName().Version.ToString()));
					LConnection.ExecuteStatement(String.Format("insert into DAELibraryOwners (Library_Name, Owner_User_ID) values ('{0}', '{1}')", Server.Server.CSystemLibraryName, Server.Server.CSystemUserID));
				}
			}
			finally
			{
				LConnection.Dispose();
			}
		}
		
		private SQLStore FStore;
		
		// TODO: This needs to be connection-string based...
		public string DatabaseFileName
		{
			get { return FStore.DatabaseFileName; }
			set { FStore.DatabaseFileName = value; }
		}
		
		public string Password
		{
			get { return FStore.Password; }
			set { FStore.Password = value; }
		}
		
		public int MaxConnections
		{
			get { return FStore.MaxConnections; }
			set { FStore.MaxConnections = value; }
		}
		
		#region Connection
		
		// TODO: I'm not happy with this but without a major re-architecture of the crap-wrapper layer, I'm not sure what else to do...
		// The provider factory seems attractive, but it fails fundamentally to deliver on actual abstraction because it ignores 
		// even basic syntactic disparity like parameter markers and basic semantic disparity like parameter types. The crap wrapper
		// is already plugged in to the catalog device (see CatalogDeviceTable), so I'm taking the easy way out on this one...
		public SQLConnection GetSQLConnection()
		{
			return FStore.GetSQLConnection();
		}
		
		public CatalogStoreConnection Connect()
		{
			return new CatalogStoreConnection(this, FStore.Connect());
		}

		private List<CatalogStoreConnection> FConnectionPool = new List<CatalogStoreConnection>();
		
		public CatalogStoreConnection AcquireConnection()
		{
			lock (FConnectionPool)
			{
				if (FConnectionPool.Count > 0)
				{
					CatalogStoreConnection LConnection = FConnectionPool[0];
					FConnectionPool.RemoveAt(0);
					return LConnection;
				}
			}
			
			return Connect();
		}
		
		public void ReleaseConnection(CatalogStoreConnection AConnection)
		{
			lock (FConnectionPool)
			{
				FConnectionPool.Add(AConnection);
			}
		}
		
		#endregion

		#region StoreStructureDefinitions
		
		private StoreTableHeaders FTableHeaders = new StoreTableHeaders();
		
		private StoreTableHeader BuildTableHeader(string ATableName)
		{
			switch (ATableName)
			{
				case "DAEServerInfo" : return new StoreTableHeader(ATableName, "PK_DAEServerInfo");
				case "DAEUsers" : return new StoreTableHeader(ATableName, "PK_DAEUsers");
				case "DAELoadedLibraries" : return new StoreTableHeader(ATableName, "PK_DAELoadedLibraries");
				case "DAELibraryDirectories" : return new StoreTableHeader(ATableName, "PK_DAELibraryDirectories");
				case "DAELibraryVersions" : return new StoreTableHeader(ATableName, "PK_DAELibraryVersions");
				case "DAELibraryOwners" : return new StoreTableHeader(ATableName, "PK_DAELibraryOwners");
				case "DAEObjects" : return new StoreTableHeader(ATableName, "PK_DAEObjects");
				case "DAEObjectDependencies" : return new StoreTableHeader(ATableName, "PK_DAEObjectDependencies");
				case "DAECatalogObjects" : return new StoreTableHeader(ATableName, "PK_DAECatalogObjects");
				case "DAECatalogObjectNames" : return new StoreTableHeader(ATableName, "PK_DAECatalogObjectNames");
				case "DAEBaseCatalogObjects" : return new StoreTableHeader(ATableName, "PK_DAEBaseCatalogObjects");
				case "DAEScalarTypes" : return new StoreTableHeader(ATableName, "PK_DAEScalarTypes");
				case "DAEOperatorNames" : return new StoreTableHeader(ATableName, "PK_DAEOperatorNames");
				case "DAEOperatorNameNames" : return new StoreTableHeader(ATableName, "PK_DAEOperatorNameNames");
				case "DAEOperators" : return new StoreTableHeader(ATableName, "PK_DAEOperators");
				case "DAEEventHandlers" : return new StoreTableHeader(ATableName, "PK_DAEEventHandlers");
				case "DAEApplicationTransactionTableMaps" : return new StoreTableHeader(ATableName, "PK_DAEApplicationTransactionTableMaps");
				case "DAEApplicationTransactionOperatorNameMaps" : return new StoreTableHeader(ATableName, "PK_DAEApplicationTransactionOperatorNameMaps");
				case "DAEApplicationTransactionOperatorMaps" : return new StoreTableHeader(ATableName, "PK_DAEApplicationTransactionOperatorMaps");
				case "DAEUserRoles" : return new StoreTableHeader(ATableName, "PK_DAEUserRoles");
				case "DAERights" : return new StoreTableHeader(ATableName, "PK_DAERights");
				case "DAERoleRightAssignments" : return new StoreTableHeader(ATableName, "PK_DAERoleRightAssignments");
				case "DAEUserRightAssignments" : return new StoreTableHeader(ATableName, "PK_DAEUserRightAssignments");
				case "DAEDevices" : return new StoreTableHeader(ATableName, "PK_DAEDevices");
				case "DAEDeviceUsers" : return new StoreTableHeader(ATableName, "PK_DAEDeviceUsers");
				case "DAEDeviceObjects" : return new StoreTableHeader(ATableName, "PK_DAEDeviceObjects");
			}

			Error.Fail("Table header could not be constructed for store table \"{0}\".", ATableName);
			return null;
		}
		
		internal StoreTableHeader GetTableHeader(string ATableName)
		{
			lock (FTableHeaders)
			{
				StoreTableHeader LTableHeader;
				if (FTableHeaders.TryGetValue(ATableName, out LTableHeader))
					return LTableHeader;
				
				LTableHeader = BuildTableHeader(ATableName);
				FTableHeaders.Add(ATableName, LTableHeader);
				return LTableHeader;
			}
		}
		
		private StoreIndexHeaders FIndexHeaders = new StoreIndexHeaders();
		
		private StoreIndexHeader BuildIndexHeader(string AIndexName)
		{
			List<string> LColumns = new List<string>();
			
			switch (AIndexName)
			{
				case "PK_DAEServerInfo" : 
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEServerInfo", AIndexName, LColumns);
					
				case "PK_DAEUsers" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEUsers", AIndexName, LColumns);
				
				case "PK_DAELoadedLibraries" :
					LColumns.Add("Library_Name");
					return new StoreIndexHeader("DAELoadedLibraries", AIndexName, LColumns);
				
				case "PK_DAELibraryDirectories" :
					LColumns.Add("Library_Name");
					return new StoreIndexHeader("DAELibraryDirectories", AIndexName, LColumns);
				
				case "PK_DAELibraryVersions" :
					LColumns.Add("Library_Name");
					return new StoreIndexHeader("DAELibraryVersions", AIndexName, LColumns);
				
				case "PK_DAELibraryOwners" :
					LColumns.Add("Library_Name");
					return new StoreIndexHeader("DAELibraryOwners", AIndexName, LColumns);
				
				case "PK_DAEObjects" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEObjects", AIndexName, LColumns);
				
				case "IDX_DAEObjects_Catalog_Object_ID" :
					LColumns.Add("Catalog_Object_ID");
					return new StoreIndexHeader("DAEObjects", AIndexName, LColumns);
					
				case "IDX_DAEObjects_Parent_Object_ID" :
					LColumns.Add("Parent_Object_ID");
					return new StoreIndexHeader("DAEObjects", AIndexName, LColumns);
					
				case "IDX_DAEObjects_Generator_Object_ID" :
					LColumns.Add("Generator_Object_ID");
					return new StoreIndexHeader("DAEObjects", AIndexName, LColumns);
				
				case "PK_DAEObjectDependencies" :
					LColumns.Add("Object_ID");
					LColumns.Add("Dependency_Object_ID");
					return new StoreIndexHeader("DAEObjectDependencies", AIndexName, LColumns);
				
				case "IDX_DAEObjectDependencies_Dependency_Object_ID" :
					LColumns.Add("Dependency_Object_ID");
					return new StoreIndexHeader("DAEObjectDependencies", AIndexName, LColumns);
				
				case "PK_DAECatalogObjects" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAECatalogObjects", AIndexName, LColumns);
				
				case "IDX_DAECatalogObjects_Owner_User_ID" :
					LColumns.Add("Owner_User_ID");
					return new StoreIndexHeader("DAECatalogObjects", AIndexName, LColumns);
					
				case "IDX_DAECatalogObjects_Library_Name" :
					LColumns.Add("Library_Name");
					return new StoreIndexHeader("DAECatalogObjects", AIndexName, LColumns);
					
				case "UIDX_DAECatalogObjects_Name" :
					LColumns.Add("Name");
					return new StoreIndexHeader("DAECatalogObjects", AIndexName, LColumns);
				
				case "PK_DAECatalogObjectNames" :
					LColumns.Add("Depth");
					LColumns.Add("Name");
					LColumns.Add("ID");
					return new StoreIndexHeader("DAECatalogObjectNames", AIndexName, LColumns);
				
				case "IDX_DAECatalogObjectNames_ID" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAECatalogObjectNames", AIndexName, LColumns);
				
				case "PK_DAEBaseCatalogObjects" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEBaseCatalogObjects", AIndexName, LColumns);
					
				case "PK_DAEScalarTypes" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEScalarTypes", AIndexName, LColumns);
					
				case "PK_DAEOperatorNames" :
					LColumns.Add("Name");
					return new StoreIndexHeader("DAEOperatorNames", AIndexName, LColumns);
				
				case "PK_DAEOperatorNameNames" :
					LColumns.Add("Depth");
					LColumns.Add("Name");
					LColumns.Add("OperatorName");
					return new StoreIndexHeader("DAEOperatorNameNames", AIndexName, LColumns);
				
				case "IDX_DAEOperatorNameNames_OperatorName" :
					LColumns.Add("OperatorName");
					return new StoreIndexHeader("DAEOperatorNameNames", AIndexName, LColumns);
				
				case "PK_DAEOperators" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEOperators", AIndexName, LColumns);
				
				case "IDX_DAEOperators_OperatorName" :
					LColumns.Add("OperatorName");
					return new StoreIndexHeader("DAEOperators", AIndexName, LColumns);
					
				case "PK_DAEEventHandlers" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEEventHandlers", AIndexName, LColumns);
					
				case "IDX_DAEEventHandlers_Operator_ID" :
					LColumns.Add("Operator_ID");
					return new StoreIndexHeader("DAEEventHandlers", AIndexName, LColumns);
					
				case "IDX_DAEEventHandlers_Source_Object_ID" :
					LColumns.Add("Source_Object_ID");
					return new StoreIndexHeader("DAEEventHandlers", AIndexName, LColumns);
					
				case "PK_DAEApplicationTransactionTableMaps" :
					LColumns.Add("Source_TableVar_ID");
					return new StoreIndexHeader("DAEApplicationTransactionTableMaps", AIndexName, LColumns);
					
				case "PK_DAEApplicationTransactionOperatorNameMaps" :
					LColumns.Add("Source_OperatorName");
					return new StoreIndexHeader("DAEApplicationTransactionOperatorNameMaps", AIndexName, LColumns);
					
				case "PK_DAEApplicationTransactionOperatorMaps" :
					LColumns.Add("Source_Operator_ID");
					return new StoreIndexHeader("DAEApplicationTransactionOperatorMaps", AIndexName, LColumns);
					
				case "IDX_DAEApplicationTransactionOperatorMaps_Translated_Operator_ID" :
					LColumns.Add("Translated_Operator_ID");
					return new StoreIndexHeader("DAEApplicationTransactionOperatorMaps", AIndexName, LColumns);
					
				case "PK_DAEUserRoles" :
					LColumns.Add("User_ID");
					LColumns.Add("Role_ID");
					return new StoreIndexHeader("DAEUserRoles", AIndexName, LColumns);
					
				case "IDX_DAEUserRoles_Role_ID" :
					LColumns.Add("Role_ID");
					return new StoreIndexHeader("DAEUserRoles", AIndexName, LColumns);
					
				case "PK_DAERights" :
					LColumns.Add("Name");
					return new StoreIndexHeader("DAERights", AIndexName, LColumns);
					
				case "IDX_DAERights_Owner_User_ID" :
					LColumns.Add("Owner_User_ID");
					return new StoreIndexHeader("DAERights", AIndexName, LColumns);
					
				case "IDX_DAERights_Catalog_Object_ID" :
					LColumns.Add("Catalog_Object_ID");
					return new StoreIndexHeader("DAERights", AIndexName, LColumns);
				
				case "PK_DAERoleRightAssignments" :
					LColumns.Add("Role_ID");
					LColumns.Add("Right_Name");
					return new StoreIndexHeader("DAERoleRightAssignments", AIndexName, LColumns);
					
				case "IDX_DAERoleRightAssignments_Right_Name" :
					LColumns.Add("Right_Name");
					return new StoreIndexHeader("DAERoleRightAssignments", AIndexName, LColumns);
					
				case "PK_DAEUserRightAssignments" :
					LColumns.Add("User_ID");
					LColumns.Add("Right_Name");
					return new StoreIndexHeader("DAEUserRightAssignments", AIndexName, LColumns);
					
				case "IDX_DAEUserRightAssignments_Right_Name" :
					LColumns.Add("Right_Name");
					return new StoreIndexHeader("DAEUserRightAssignments", AIndexName, LColumns);
					
				case "PK_DAEDevices" :
					LColumns.Add("ID");
					return new StoreIndexHeader("DAEDevices", AIndexName, LColumns);
					
				case "PK_DAEDeviceUsers" :
					LColumns.Add("User_ID");
					LColumns.Add("Device_ID");
					return new StoreIndexHeader("DAEDeviceUsers", AIndexName, LColumns);
					
				case "IDX_DAEDeviceUsers_Device_ID" :
					LColumns.Add("Device_ID");
					return new StoreIndexHeader("DAEDeviceUsers", AIndexName, LColumns);
					
				case "PK_DAEDeviceObjects" :
					LColumns.Add("ID");
					LColumns.Add("Device_ID");
					LColumns.Add("Mapped_Object_ID");
					return new StoreIndexHeader("DAEDeviceObjects", AIndexName, LColumns);
					
				case "IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID" :
					LColumns.Add("Device_ID");
					LColumns.Add("Mapped_Object_ID");
					return new StoreIndexHeader("DAEDeviceObjects", AIndexName, LColumns);
				
			}

			Error.Fail("Index header could not be constructed for store index name \"{0}\".", AIndexName);
			return null;
		}
		
		internal StoreIndexHeader GetIndexHeader(string AIndexName)
		{
			lock (FIndexHeaders)
			{
				StoreIndexHeader LIndexHeader;
				if (FIndexHeaders.TryGetValue(AIndexName, out LIndexHeader))
					return LIndexHeader;
					
				LIndexHeader = BuildIndexHeader(AIndexName);
				FIndexHeaders.Add(AIndexName, LIndexHeader);
				return LIndexHeader;
			}
		}
			
		#endregion
	}
	
	public class SQLStoreCursorCache : Dictionary<string, SQLStoreCursor> {}
	
	public class CatalogStoreConnection : System.Object, IDisposable
	{
		internal CatalogStoreConnection(CatalogStore AStore, SQLStoreConnection AConnection)
		{
			FStore = AStore;
			FConnection = AConnection;
		}

		public void Dispose()
		{
			FlushCache();
			
			if (FConnection != null)
			{
				FConnection.Dispose();
				FConnection = null;
			}
			
			FStore = null;
		}
		
		private SQLStoreConnection FConnection;
		
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			if (FConnection.TransactionCount == 0)
				FlushCache();
			System.Data.IsolationLevel LIsolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (AIsolationLevel)
			{
				case IsolationLevel.Isolated : LIsolationLevel = System.Data.IsolationLevel.Serializable; break;
				case IsolationLevel.CursorStability : LIsolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case IsolationLevel.Browse : LIsolationLevel = System.Data.IsolationLevel.ReadUncommitted; break;
			}
			FConnection.BeginTransaction(LIsolationLevel);
		}

		public void CommitTransaction()
		{
			if (FConnection.TransactionCount == 1)
				FlushCache();
			FConnection.CommitTransaction();
		}

		public void RollbackTransaction()
		{
			if (FConnection.TransactionCount == 1)
				FlushCache();
			FConnection.RollbackTransaction();
		}
		
		private CatalogStore FStore;
		public CatalogStore Store { get { return FStore; } }
		
		private SQLStoreCursorCache FCursorCache = new SQLStoreCursorCache();
		
		private void FlushCache()
		{
			foreach (SQLStoreCursor LCursor in FCursorCache.Values)
				LCursor.Dispose();
			FCursorCache.Clear();
		}
		
		public void CloseCursor(SQLStoreCursor ACursor)
		{
			if (!FCursorCache.ContainsKey(ACursor.CursorName))
				FCursorCache.Add(ACursor.CursorName, ACursor);
			else
				ACursor.Dispose();
		}
		
		public SQLStoreCursor OpenCursor(string AIndexName, bool AIsUpdatable)
		{
			string LCursorName = AIndexName + AIsUpdatable.ToString();
			SQLStoreCursor LCursor;
			if (FCursorCache.TryGetValue(LCursorName, out LCursor))
			{
				FCursorCache.Remove(LCursorName);
				LCursor.SetRange(null, null);
			}
			else
			{
				StoreIndexHeader LIndexHeader = Store.GetIndexHeader(AIndexName);
				LCursor = FConnection.OpenCursor(LIndexHeader.TableName, LIndexHeader.Name, LIndexHeader.Columns, AIsUpdatable);
			}
			return LCursor;
		}
		
		public SQLStoreCursor OpenRangedCursor(string AIndexName, bool AIsUpdatable, object[] AStartValues, object[] AEndValues)
		{
			SQLStoreCursor LCursor = OpenCursor(AIndexName, AIsUpdatable);
			try
			{
				LCursor.SetRange(AStartValues, AEndValues);
				return LCursor;
			}
			catch
			{
				LCursor.Dispose();
				throw;
			}
		}
		
		public SQLStoreCursor OpenMatchedCursor(string AIndexName, bool AIsUpdatable, params object[] AMatchValues)
		{
			SQLStoreCursor LCursor = OpenCursor(AIndexName, AIsUpdatable);
			try
			{
				LCursor.SetRange(AMatchValues, AMatchValues);
				return LCursor;
			}
			catch
			{
				LCursor.Dispose();
				throw;
			}
		}
		
		public void InsertRow(string ATableName, params object[] ARowValues)
		{
			SQLStoreCursor LCursor = OpenCursor(Store.GetTableHeader(ATableName).PrimaryKeyName, true);
			try
			{
				LCursor.Insert(ARowValues);
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void DeleteRows(string AIndexName, params object[] AKeyValues)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor(AIndexName, true, AKeyValues);
			try
			{
				while (LCursor.Next())
					LCursor.Delete();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		#region Platform Independent Internal Logic
		
		private void InsertObjectDependencies(SQLStoreCursor ADependencies, Schema.Object AObject)
		{
			if (AObject.HasDependencies())
				for (int LIndex = 0; LIndex < AObject.Dependencies.Count; LIndex++)
					InsertRow("DAEObjectDependencies", AObject.ID, AObject.Dependencies.IDs[LIndex]);
		}
		
		private void InsertObjectAndDependencies(SQLStoreCursor AObjects, SQLStoreCursor ADependencies, Schema.Object AObject)
		{
			InsertRow
			(
				"DAEObjects", 
				AObject.ID, 
				Schema.Object.EnsureNameLength(AObject.Name), 
				AObject.Library == null ? String.Empty : AObject.Library.Name, 
				Schema.Object.EnsureDescriptionLength(AObject.DisplayName), 
				Schema.Object.EnsureDescriptionLength(AObject.Description),
				AObject.GetType().Name,
				AObject.IsSystem,
				AObject.IsRemotable,
				AObject.IsGenerated,
				AObject.IsATObject,
				AObject.IsSessionObject,
				AObject.IsPersistent,
				AObject.CatalogObjectID,
				AObject.ParentObjectID,
				AObject.GeneratorID
			);

			InsertObjectDependencies(ADependencies, AObject);
		}
		
		private void InsertAllObjectsAndDependencies(Schema.Object AObject)
		{
			// ScalarType
				// ScalarTypeDefault
				// ScalarTypeConstraints
				// Representations
					// Properties
				// Specials
			// Sort
			// Conversion
			// ScalarTypeEventHandler
			// BaseTableVar
				// TableVarColumns
					// TableVarColumnDefault
					// TableVarColumnConstraints
				// Keys
				// Orders
				// RowConstraints
				// TransitionConstraints
			// TableVarColumnEventHandler
			// TableVarEventHandler
			// DerivedTableVar
				// TableVarColumns
					// TableVarColumnDefault
					// TableVarColumnConstraints
				// Keys
				// Orders
				// RowConstraints
				// TransitionConstraints
			// Reference
			// CatalogConstraint
			// Device
			// DeviceScalarType
			// DeviceOperator
			// Operator
			
			SQLStoreCursor LObjects = OpenCursor("PK_DAEObjects", false);
			try
			{
				SQLStoreCursor LDependencies = OpenCursor("PK_DAEObjectDependencies", false);
				try
				{
					InsertObjectDependencies(LDependencies, AObject);
			
					int LIndex;
					int LSubIndex;
					Schema.ScalarType LScalarType = AObject as Schema.ScalarType;
					if (LScalarType != null)
					{
						for (LIndex = 0; LIndex < LScalarType.Representations.Count; LIndex++)
						{
							Schema.Representation LRepresentation = LScalarType.Representations[LIndex];
							if (!LRepresentation.IsPersistent)
							{
								InsertObjectAndDependencies(LObjects, LDependencies, LRepresentation);
								for (LSubIndex = 0; LSubIndex < LRepresentation.Properties.Count; LSubIndex++)
									InsertObjectAndDependencies(LObjects, LDependencies, LRepresentation.Properties[LSubIndex]);
							}
						}

						for (LIndex = 0; LIndex < LScalarType.Specials.Count; LIndex++)
							if (!LScalarType.Specials[LIndex].IsPersistent)
								InsertObjectAndDependencies(LObjects, LDependencies, LScalarType.Specials[LIndex]);
							
						if (LScalarType.Default != null)
							if (!LScalarType.Default.IsPersistent)
								InsertObjectAndDependencies(LObjects, LDependencies, LScalarType.Default);
						
						for (LIndex = 0; LIndex < LScalarType.Constraints.Count; LIndex++)
							if (!LScalarType.Constraints[LIndex].IsPersistent)
								InsertObjectAndDependencies(LObjects, LDependencies, LScalarType.Constraints[LIndex]);
							
						return;
					}
					
					{
						Schema.Representation LRepresentation = AObject as Schema.Representation;
						if (LRepresentation != null)
						{
							for (LIndex = 0; LIndex < LRepresentation.Properties.Count; LIndex++)
								InsertObjectAndDependencies(LObjects, LDependencies, LRepresentation.Properties[LIndex]);
						
							return;
						}
					}
					
					Schema.TableVar LTableVar = AObject as Schema.TableVar;
					if (LTableVar != null)
					{
						for (LIndex = 0; LIndex < LTableVar.Columns.Count; LIndex++)
						{
							Schema.TableVarColumn LColumn = LTableVar.Columns[LIndex];
							InsertObjectAndDependencies(LObjects, LDependencies, LColumn);

							if (LColumn.Default != null)
								if (!LColumn.Default.IsPersistent)
									InsertObjectAndDependencies(LObjects, LDependencies, LColumn.Default);
								
							if (LColumn.HasConstraints())
								for (LSubIndex = 0; LSubIndex < LColumn.Constraints.Count; LSubIndex++)
									if (!LColumn.Constraints[LSubIndex].IsPersistent)
										InsertObjectAndDependencies(LObjects, LDependencies, LColumn.Constraints[LSubIndex]);
						}

						for (LIndex = 0; LIndex < LTableVar.Keys.Count; LIndex++)
							InsertObjectAndDependencies(LObjects, LDependencies, LTableVar.Keys[LIndex]);
							
						for (LIndex = 0; LIndex < LTableVar.Orders.Count; LIndex++)
							InsertObjectAndDependencies(LObjects, LDependencies, LTableVar.Orders[LIndex]);

						for (LIndex = 0; LIndex < LTableVar.Constraints.Count; LIndex++)
							if (!LTableVar.Constraints[LIndex].IsGenerated && !LTableVar.Constraints[LIndex].IsPersistent) // Generated constraints are maintained internally
								InsertObjectAndDependencies(LObjects, LDependencies, LTableVar.Constraints[LIndex]);
					}
				}
				finally
				{
					CloseCursor(LDependencies);
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}
		
		private void DeleteObjectDependencies(Schema.Object AObject)
		{
			DeleteRows("PK_DAEObjectDependencies", AObject.ID);
			SQLStoreCursor LObjects = OpenMatchedCursor("IDX_DAEObjects_Catalog_Object_ID", false, AObject.ID);
			try
			{
				while (LObjects.Next())
					DeleteRows("PK_DAEObjectDependencies", LObjects[0]);
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}

		#endregion
		
		#region API
		
		public void LoadServerSettings(Server.Server AServer)
		{
			SQLStoreCursor LCursor = OpenCursor("PK_DAEServerInfo", false);
			try
			{
				if (LCursor.Next())
				{
					AServer.MaxConcurrentProcesses = (int)LCursor[3];
					AServer.ProcessWaitTimeout = TimeSpan.FromMilliseconds((int)LCursor[4]);
					AServer.ProcessTerminationTimeout = TimeSpan.FromMilliseconds((int)LCursor[5]);
					AServer.PlanCacheSize = (int)LCursor[6];
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void SaveServerSettings(Server.Server AServer)
		{
			SQLStoreCursor LCursor = OpenCursor("PK_DAEServerInfo", true);
			try
			{
				if (LCursor.Next())
				{
					LCursor[3] = AServer.MaxConcurrentProcesses;
					LCursor[4] = (int)AServer.ProcessWaitTimeout.TotalMilliseconds;
					LCursor[5] = (int)AServer.ProcessTerminationTimeout.TotalMilliseconds;
					LCursor[6] = AServer.PlanCacheSize;
					LCursor.Update();
				}
				else
				{
					LCursor.Insert
					(
						new object[]
						{
							"ID",
							AServer.Name,
							GetType().Assembly.GetName().Version.ToString(),
							AServer.MaxConcurrentProcesses,
							(int)AServer.ProcessWaitTimeout.TotalMilliseconds,
							(int)AServer.ProcessTerminationTimeout.TotalMilliseconds,
							AServer.PlanCacheSize
						}
					);
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public Schema.Right SelectRight(string ARightName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAERights", false, ARightName);
			try
			{
				if (LCursor.Next())
					return new Schema.Right((string)LCursor[0], (string)LCursor[1], (int)LCursor[2]);
					
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void InsertRight(string ARightName, string AUserID)
		{
			InsertRow("DAERights", ARightName, AUserID, -1);
		}
		
		public void UpdateRight(string ARightName, string AUserID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAERights", true, ARightName);
			try
			{
				if (LCursor.Next())
				{
					LCursor[1] = AUserID;
					LCursor.Update();
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void DeleteRight(string ARightName)
		{
			// Delete role right assignments for the right
			DeleteRows("IDX_DAERoleRightAssignments_Right_Name", ARightName);
			
			// Delete user right assignments for the right
			DeleteRows("IDX_DAEUserRightAssignments_Right_Name", ARightName);
			
			// Delete the right
			DeleteRows("PK_DAERights", ARightName);
		}
		
		public void InsertRole(Schema.Role ARole, string AObjectScript)
		{
			InsertPersistentObject(ARole, AObjectScript);
		}
		
		public void DeleteRole(Schema.Role ARole)
		{
			// Delete the DAERoleRightAssignments rows for the rights assigned to the role
			DeleteRows("PK_DAERoleRightAssignments", ARole.ID);

			// Delete the DAEUserRoles rows for the role
			DeleteRows("IDX_DAEUserRoles_Role_ID", ARole.ID);
			
			// Delete the role
			DeletePersistentObject(ARole);
		}
		
		public List<string> SelectRoleUsers(int ARoleID)
		{
			List<string> LUsers = new List<string>();
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEUserRoles_Role_ID", false, ARoleID);
			try
			{
				while (LCursor.Next())
					LUsers.Add((string)LCursor[0]);
					
				return LUsers;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public Schema.RightAssignment SelectRoleRightAssignment(int ARoleID, string ARightName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAERoleRightAssignments", false, ARoleID, ARightName);
			try
			{
				if (LCursor.Next())
					return new Schema.RightAssignment((string)LCursor[1], (bool)LCursor[2]);
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void EnsureRoleRightAssignment(int ARoleID, string ARightName, bool AGranted)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAERoleRightAssignments", true, ARoleID, ARightName);
			try
			{
				if (LCursor.Next())
				{
					LCursor[2] = AGranted;
					LCursor.Update();
				}
				else
					LCursor.Insert(new object[] { ARoleID, ARightName, AGranted });
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void DeleteRoleRightAssignment(int ARoleID, string ARightName)
		{
			DeleteRows("PK_DAERoleRightAssignments", ARoleID, ARightName);
		}
		
		public void EnsureUserRightAssignment(string AUserID, string ARightName, bool AGranted)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEUserRightAssignments", true, AUserID, ARightName);
			try
			{
				if (LCursor.Next())
				{
					LCursor[2] = AGranted;
					LCursor.Update();
				}
				else
					LCursor.Insert(new object[] { AUserID, ARightName, AGranted });
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void DeleteUserRightAssignment(string AUserID, string ARightName)
		{
			DeleteRows("PK_DAEUserRightAssignments", AUserID, ARightName);
		}
		
		public Schema.User SelectUser(string AUserID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEUsers", false, AUserID);
			try
			{
				if (LCursor.Next())
					return new Schema.User((string)LCursor[0], (string)LCursor[1], (string)LCursor[2]);
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void InsertUser(Schema.User AUser)
		{
			InsertRow("DAEUsers", AUser.ID, AUser.Name, AUser.Password);
		}
		
		public void UpdateUser(Schema.User AUser)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEUsers", true, AUser.ID);
			try
			{
				if (LCursor.Next())
				{
					LCursor[1] = AUser.Name;
					LCursor[2] = AUser.Password;
					LCursor.Update();
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void DeleteUser(string AUserID)
		{
			// Delete DAEDeviceUsers for the user
			DeleteRows("PK_DAEDeviceUsers", AUserID);
			
			// Delete DAEUserRightAssginments for the user
			DeleteRows("PK_DAEUserRightAssignments", AUserID);
			
			// Delete DAEUserRoles for the user
			DeleteRows("PK_DAEUserRoles", AUserID);

			// Delete DAEUsers for the user
			DeleteRows("PK_DAEUsers", AUserID);
		}
		
		public bool UserOwnsObjects(string AUserID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAECatalogObjects_Owner_User_ID", false, AUserID);
			try
			{
				return LCursor.Next();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public bool UserOwnsRights(string AUserID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAERights_Owner_User_ID", false, AUserID);
			try
			{
				return LCursor.Next();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		/// <summary>Returns a list of headers for each operator owned by the given user.</summary>
		public Schema.CatalogObjectHeaders SelectUserOperators(string AUserID)
		{
			Schema.CatalogObjectHeaders LHeaders = new Schema.CatalogObjectHeaders();
			SQLStoreCursor LCatalogObjects = OpenMatchedCursor("IDX_DAECatalogObjects_Owner_User_ID", false, AUserID);
			try
			{
				SQLStoreCursor LOperators = OpenCursor("PK_DAEOperators", false);
				try
				{
					while (LCatalogObjects.Next())
						if (LOperators.FindKey(new object[] { LCatalogObjects[0] }))
							LHeaders.Add(new Schema.CatalogObjectHeader((int)LCatalogObjects[0], (string)LCatalogObjects[1], (string)LCatalogObjects[2], (string)LCatalogObjects[3]));

					return LHeaders;
				}
				finally
				{
					CloseCursor(LOperators);
				}
			}
			finally
			{
				CloseCursor(LCatalogObjects);
			}
		}

		public void InsertUserRole(string AUserID, int ARoleID)
		{
			InsertRow("DAEUserRoles", AUserID, ARoleID); 
		}
		
		public void DeleteUserRole(string AUserID, int ARoleID)
		{
			DeleteRows("PK_DAEUserRoles", AUserID, ARoleID);
		}
		
		public bool UserHasRight(string AUserID, string ARightName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAERights", false, ARightName);
			try
			{
				// If the right does not exist, it is implicitly granted.
				// This behavior ensures that rights for objects created only in the internal cache will always be granted.
				// Checking the rights for objects such as internal constraint check tables and, in certain configurations, 
				// A/T and session tables will never result in a security exception.
				if (!LCursor.Next())
					return true;
					
				if (String.Compare((string)LCursor[1], AUserID, true) == 0)
					return true;
			}
			finally
			{
				CloseCursor(LCursor);
			}
			
			LCursor = OpenMatchedCursor("PK_DAEUserRightAssignments", false, AUserID, ARightName);
			try
			{
				if (LCursor.Next())
					return (bool)LCursor[2];
			}
			finally
			{
				CloseCursor(LCursor);
			}
			
			bool LGranted = false;
			SQLStoreCursor LRoles = OpenMatchedCursor("PK_DAEUserRoles", false, AUserID);
			try
			{
				while (LRoles.Next())
				{
					SQLStoreCursor LRoleRights = OpenMatchedCursor("PK_DAERoleRightAssignments", false, LRoles[1], ARightName);
					try
					{
						if (LRoleRights.Next())
						{
							LGranted = (bool)LRoleRights[2];
							if (!LGranted)
								return LGranted;
						}
					}
					finally
					{
						CloseCursor(LRoleRights);
					}
				}
			}
			finally
			{
				CloseCursor(LRoles);
			}
			return LGranted;
		}
		
		public Schema.DeviceUser SelectDeviceUser(Schema.Device ADevice, Schema.User AUser)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEDeviceUsers", false, AUser.ID, ADevice.ID);
			try
			{
				if (LCursor.Next())
					return new Schema.DeviceUser(AUser, ADevice, (string)LCursor[2], (string)LCursor[3], (string)LCursor[4]);
					
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void InsertDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			InsertRow("DAEDeviceUsers", ADeviceUser.User.ID, ADeviceUser.Device.ID, ADeviceUser.DeviceUserID, ADeviceUser.DevicePassword, ADeviceUser.ConnectionParameters);
		}
		
		public void UpdateDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEDeviceUsers", true, ADeviceUser.User.ID, ADeviceUser.Device.ID);
			try
			{
				if (LCursor.Next())
				{
					LCursor[2] = ADeviceUser.DeviceUserID;
					LCursor[3] = ADeviceUser.DevicePassword;
					LCursor[4] = ADeviceUser.ConnectionParameters;
					LCursor.Update();
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public void DeleteDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			DeleteRows("PK_DAEDeviceUsers", ADeviceUser.User.ID, ADeviceUser.Device.ID);
		}
		
		/// <summary>Returns true if there are any device objects registered for the given device, false otherwise.</summary>
		public bool HasDeviceObjects(int ADeviceID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID", false, ADeviceID);
			try
			{
				return LCursor.Next();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		/// <summary>Returns the ID of the device object map for the device with ID ADeviceID and object AObjectID, if it exists, -1 otherwise.</summary>
		public int SelectDeviceObjectID(int ADeviceID, int AObjectID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID", false, ADeviceID, AObjectID);
			try
			{
				if (LCursor.Next())
					return (int)LCursor[0];
				return -1;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		/// <summary>Returns true if there are no catalog objects in the catalog. This will only be true on a first-time startup.</summary>
		public bool IsEmpty()
		{
			SQLStoreCursor LCursor = OpenCursor("PK_DAECatalogObjects", false);
			try
			{
				return !LCursor.Next();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		/// <summary>Takes a snapshot of the base system objects. Should only be run once during first-time startup, immediately after the creation of the base system objects.</summary>
		/// <remarks>
		/// The base system objects are the minimum set of objects required to establish connection to and begin compiling D4 statements.
		/// This is the set of objects that are created programmatically by server startup, and are the only objects that will be in 
		/// a repository-mode server at initial startup. Because of this, they also make up the set up of objects that will always be
		/// present in any given client-side cache, and are therefore used as the seed for the client-side cache tracking mechanisms in the
		/// main server (see GatherDefaultCachedObjects).
		/// </remarks>
		public void SnapshotBase()
		{
			SQLStoreCursor LObjects = OpenCursor("PK_DAECatalogObjects", false);
			try
			{
				SQLStoreCursor LBaseObjects = OpenCursor("PK_DAEBaseCatalogObjects", true);
				try
				{
					while (LObjects.Next())
						LBaseObjects.Insert(new object[] { LObjects[0] });
				}
				finally
				{
					CloseCursor(LBaseObjects);
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}
		
		/// <summary>Returns a list of catalog headers for the base system objects.</summary>
		public Schema.CatalogObjectHeaders SelectBaseCatalogObjects()
		{
			SQLStoreCursor LBaseObjects = OpenCursor("PK_DAEBaseCatalogObjects", false);
			try
			{
				SQLStoreCursor LObjects = OpenCursor("PK_DAECatalogObjects", false);
				try
				{
					Schema.CatalogObjectHeaders LHeaders = new Schema.CatalogObjectHeaders();

					while (LBaseObjects.Next())
						if (LObjects.FindKey(new object[] { LBaseObjects[0] }))
							LHeaders.Add(new Schema.CatalogObjectHeader((int)LObjects[0], (string)LObjects[1], (string)LObjects[2], (string)LObjects[3]));
							
					return LHeaders;
				}
				finally
				{
					CloseCursor(LObjects);
				}
			}
			finally
			{
				CloseCursor(LBaseObjects);
			}
		}

		public Schema.CatalogObjectHeaders SelectLibraryCatalogObjects(string ALibraryName)
		{
			SQLStoreCursor LObjects = OpenMatchedCursor("IDX_DAECatalogObjects_Library_Name", false, ALibraryName);
			try
			{
				Schema.CatalogObjectHeaders LHeaders = new Schema.CatalogObjectHeaders();
				
				while (LObjects.Next())
					LHeaders.Add(new Schema.CatalogObjectHeader((int)LObjects[0], (string)LObjects[1], (string)LObjects[2], (string)LObjects[3]));
					
				return LHeaders;
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}

		public Schema.CatalogObjectHeaders SelectGeneratedObjects(int AObjectID)
		{
			SQLStoreCursor LObjects = OpenMatchedCursor("IDX_DAEObjects_Generator_Object_ID", false, AObjectID);
			try
			{
				SQLStoreCursor LCatalogObjects = OpenCursor("PK_DAECatalogObjects", false);
				try
				{
					Schema.CatalogObjectHeaders LHeaders = new Schema.CatalogObjectHeaders();
					
					while (LObjects.Next())
						if (LCatalogObjects.FindKey(new object[] { LObjects[0] }))
							LHeaders.Add(new Schema.CatalogObjectHeader((int)LCatalogObjects[0], (string)LCatalogObjects[1], (string)LCatalogObjects[2], (string)LCatalogObjects[3]));
							
					return LHeaders;
				}
				finally
				{
					CloseCursor(LCatalogObjects);
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}

		public int GetMaxObjectID()
		{
			SQLStoreCursor LCursor = OpenCursor("PK_DAEObjects", false);
			try
			{
				LCursor.Last();
				if (LCursor.Prior())
					return (int)LCursor[0];
				return 0;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public List<string> SelectLoadedLibraries()
		{
			List<string> LLibraryNames = new List<string>();
			SQLStoreCursor LCursor = OpenCursor("PK_DAELoadedLibraries", false);
			try
			{
				while (LCursor.Next())
					LLibraryNames.Add((string)LCursor[0]);
			}
			finally
			{
				CloseCursor(LCursor);
			}
			return LLibraryNames;
		}

		public bool LoadedLibraryExists(string ALibraryName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAELoadedLibraries", false, ALibraryName);
			try
			{
				return LCursor.Next();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void InsertLoadedLibrary(string ALibraryName)
		{
			InsertRow("DAELoadedLibraries", ALibraryName);
		}

		public void DeleteLoadedLibrary(string ALibraryName)
		{
			DeleteRows("PK_DAELoadedLibraries", ALibraryName);
		}

		public SQLStoreCursor SelectLibraryDirectories()
		{
			return OpenCursor("PK_DAELibraryDirectories", false);
		}

		public void SetLibraryDirectory(string ALibraryName, string ALibraryDirectory)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAELibraryDirectories", true, ALibraryName);
			try
			{
				if (LCursor.Next())
				{
					LCursor[1] = ALibraryDirectory;
					LCursor.Update();
				}
				else
					LCursor.Insert(new object[] { ALibraryName, ALibraryDirectory });
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void DeleteLibraryDirectory(string ALibraryName)
		{
			DeleteRows("PK_DAELibraryDirectories", ALibraryName);
		}

		public SQLStoreCursor SelectLibraryOwners()
		{
			return OpenCursor("PK_DAELibraryOwners", false);
		}

		public string SelectLibraryOwner(string ALibraryName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAELibraryOwners", false, ALibraryName);
			try
			{
				if (LCursor.Next())
					return (string)LCursor[1];
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void InsertLibraryOwner(string ALibraryName, string AUserID)
		{
			InsertRow("DAELibraryOwners", ALibraryName, AUserID);
		}

		public void UpdateLibraryOwner(string ALibraryName, string AUserID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAELibraryOwners", true, ALibraryName);
			try
			{
				if (LCursor.Next())
				{
					LCursor[1] = AUserID;
					LCursor.Update();
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void DeleteLibraryOwner(string ALibraryName)
		{
			DeleteRows("PK_DAELibraryOwners", ALibraryName);
		}

		public SQLStoreCursor SelectLibraryVersions()
		{
			return OpenCursor("PK_DAELibraryVersions", false);
		}

		public string SelectLibraryVersion(string ALibraryName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAELibraryVersions", false, ALibraryName);
			try
			{
				if (LCursor.Next())
					return (string)LCursor[1];
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void InsertLibraryVersion(string ALibraryName, VersionNumber AVersion)
		{
			InsertRow("DAELibraryVersions", ALibraryName, AVersion.ToString());
		}
		
		public void UpdateLibraryVersion(string ALibraryName, VersionNumber AVersion)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAELibraryVersions", true, ALibraryName);
			try
			{
				if (LCursor.Next())
				{
					LCursor[1] = AVersion.ToString();
					LCursor.Update();
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void DeleteLibraryVersion(string ALibraryName)
		{
			DeleteRows("PK_DAELibraryVersions", ALibraryName);
		}

		/// <summary>Returns true if an object of the given name is already present in the database.</summary>
		public bool CatalogObjectExists(string AObjectName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("UIDX_DAECatalogObjects_Name", false, AObjectName);
			try
			{
				return LCursor.Next();
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		private int GetMaxCatalogObjectNamesIndexDepth()
		{
			SQLStoreCursor LCursor = OpenCursor("PK_DAECatalogObjectNames", false);
			try
			{
				int LMax = 0; // TODO: Could cache this... would only ever be changed by adding or deleting catalog objects
				LCursor.Last();
				if (LCursor.Prior())
					LMax = (int)LCursor[0];
				return LMax;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		/// <summary>Returns a list of CatalogObjectHeaders that matched the given name.</summary>		
		public Schema.CatalogObjectHeaders ResolveCatalogObjectName(string AName)
		{
			Schema.CatalogObjectHeaders LHeaders = new Schema.CatalogObjectHeaders();
			if (Schema.Object.IsRooted(AName))
			{
				AName = Schema.Object.EnsureUnrooted(AName);
				SQLStoreCursor LCursor = OpenMatchedCursor("UIDX_DAECatalogObjects_Name", false, AName);
				try
				{
					#if REQUIRECASEMATCHONRESOLVE
					if (LCursor.Next() && (AName == (string)LCursor[1]))
					#else
					if (LCursor.Next())
					#endif
						LHeaders.Add(new Schema.CatalogObjectHeader((int)LCursor[0], (string)LCursor[1], (string)LCursor[2], (string)LCursor[3]));
						
					return LHeaders;
				}
				finally
				{
					CloseCursor(LCursor);
				}
			}
			
			for (int LIndex = GetMaxCatalogObjectNamesIndexDepth() - Schema.Object.GetQualifierCount(AName); LIndex >= 0; LIndex--)
			{
				SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAECatalogObjectNames", false, LIndex, AName);
				try
				{
					SQLStoreCursor LObjects = OpenCursor("PK_DAECatalogObjects", false);
					try
					{
						while (LCursor.Next())
							#if REQUIRECASEMATCHONRESOLVE
							if ((AName == (string)LCursor[1]) && LObjects.FindKey(new object[] { LCursor[2] }))
							#else
							if (LObjects.FindKey(new object[] { LCursor[2] }))
							#endif
								LHeaders.Add(new Schema.CatalogObjectHeader((int)LObjects[0], (string)LObjects[1], (string)LObjects[2], (string)LObjects[3]));
					}
					finally
					{
						CloseCursor(LObjects);
					}
				}
				finally
				{
					CloseCursor(LCursor);
				}
			}
			
			return LHeaders;
		}
		
		private int GetMaxOperatorNameNamesIndexDepth()
		{
			SQLStoreCursor LCursor = OpenCursor("PK_DAEOperatorNameNames", false);
			try
			{
				int LMax = 0; // TODO: Could cache this... would only ever be changed by adding or deleting catalog objects
				LCursor.Last();
				if (LCursor.Prior())
					LMax = (int)LCursor[0];
				return LMax;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		/// <summary>Returns a list of CatalogObjectHeaders for operators whose operator name matched the given name.</summary>
		public Schema.CatalogObjectHeaders ResolveOperatorName(string AName)
		{
			Schema.CatalogObjectHeaders LHeaders = new Schema.CatalogObjectHeaders();
			if (Schema.Object.IsRooted(AName))
			{
				AName = Schema.Object.EnsureUnrooted(AName);
				SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEOperators_OperatorName", false, AName);
				try
				{
					SQLStoreCursor LObjects = OpenCursor("PK_DAECatalogObjects", false);
					try
					{
						while (LCursor.Next())
							#if REQUIRECASEMATCHONRESOLVE
							if ((AName == (string)LCursor[1]) && LObjects.FindKey(new object[] { LCursor[0] }))
							#else
							if (LObjects.FindKey(new object[] { LCursor[0] }))
							#endif
								LHeaders.Add(new Schema.CatalogObjectHeader((int)LObjects[0], (string)LObjects[1], (string)LObjects[2], (string)LObjects[3]));
					}
					finally
					{
						CloseCursor(LObjects);
					}
						
					return LHeaders;
				}
				finally
				{
					CloseCursor(LCursor);
				}
			}
			
			for (int LIndex = GetMaxOperatorNameNamesIndexDepth() - Schema.Object.GetQualifierCount(AName); LIndex >= 0; LIndex--)
			{
				SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEOperatorNameNames", false, LIndex, AName);
				try
				{
					SQLStoreCursor LObjects = OpenCursor("PK_DAECatalogObjects", false);
					try
					{
						while (LCursor.Next())
						{
							#if REQUIRECASEMATCHONRESOLVE
							if (AName == (string)LCursor[1])
							{
							#endif
								SQLStoreCursor LOperators = OpenMatchedCursor("IDX_DAEOperators_OperatorName", false, LCursor[2]);
								try
								{
									while (LOperators.Next())
										if (LObjects.FindKey(new object[] { LOperators[0] }))
											LHeaders.Add(new Schema.CatalogObjectHeader((int)LObjects[0], (string)LObjects[1], (string)LObjects[2], (string)LObjects[3]));
								}
								finally
								{
									CloseCursor(LOperators);
								}
							#if REQUIRECASEMATCHONRESOLVE
							}
							#endif
						}
					}
					finally
					{
						CloseCursor(LObjects);
					}
				}
				finally
				{
					CloseCursor(LCursor);
				}
			}
			
			return LHeaders;
		}
		
		public string SelectOperatorName(int AObjectID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEOperators", false, AObjectID);
			try
			{
				if (LCursor.Next())
					return (string)LCursor[1];

				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public Schema.PersistentObjectHeader SelectPersistentObject(int AObjectID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEObjects", false, AObjectID);
			try
			{
				if (LCursor.Next())
					return
						new Schema.PersistentObjectHeader
						(
							(int)LCursor[0], // ID
							(string)LCursor[1], // Name
							(string)LCursor[2], // Library_Name
							(string)LCursor[15], // Script
							(string)LCursor[3], // DisplayName
							(string)LCursor[5], // ObjectType
							(bool)LCursor[6], // IsSystem
							(bool)LCursor[7], // IsRemotable
							(bool)LCursor[8], // IsGenerated
							(bool)LCursor[9], // IsATObject
							(bool)LCursor[10] // IsSessionObject
						);
						
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		public Schema.FullCatalogObjectHeader SelectCatalogObject(int AObjectID)
		{
			SQLStoreCursor LObjects = OpenMatchedCursor("PK_DAEObjects", false, AObjectID);
			try
			{
				SQLStoreCursor LCatalogObjects = OpenCursor("PK_DAECatalogObjects", false);
				try
				{
					if (LObjects.Next() && LCatalogObjects.FindKey(new object[] { LObjects[0] }))
						return
							new Schema.FullCatalogObjectHeader
							(
								(int)LObjects[0], // ID
								(string)LObjects[1], // Name
								(string)LObjects[2], // LibraryName
								(string)LCatalogObjects[3], // OwnerID
								(string)LObjects[15], // Script
								(string)LObjects[3], // DisplayName
								(string)LObjects[5], // ObjectType
								(bool)LObjects[6], // IsSystem
								(bool)LObjects[7], // IsRemotable
								(bool)LObjects[8], // IsGenerated
								(bool)LObjects[9], // IsATObject
								(bool)LObjects[10], // IsSessionObject
								(int)LObjects[14] // GeneratorObjectID
							);
							
					return null;
				}
				finally
				{
					CloseCursor(LCatalogObjects);
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}
		
		public void InsertPersistentObject(Schema.Object AObject, string AObjectScript)
		{
			// Insert the persistent objects row
			InsertRow
			(
				"DAEObjects", 
				AObject.ID, 
				AObject.Name, 
				AObject.Library == null ? String.Empty : AObject.Library.Name,
				Schema.Object.EnsureDescriptionLength(AObject.DisplayName),
				Schema.Object.EnsureDescriptionLength(AObject.Description),
				AObject.GetType().Name,
				AObject.IsSystem,
				AObject.IsRemotable,
				AObject.IsGenerated,
				AObject.IsATObject,
				AObject.IsSessionObject,
				AObject.IsPersistent,
				AObject.CatalogObjectID,
				AObject.ParentObjectID,
				AObject.GeneratorID,
				AObjectScript
			);
			
			Schema.CatalogObject LCatalogObject = AObject as Schema.CatalogObject;
			if (LCatalogObject != null)
			{
				// Insert the DAECatalogObjects row
				InsertRow("DAECatalogObjects", LCatalogObject.ID, LCatalogObject.Name, LCatalogObject.Library == null ? String.Empty : LCatalogObject.Library.Name, LCatalogObject.Owner.ID);
				
				// Insert the DAECatalogObjectNames rows
				SQLStoreCursor LCatalogObjectNames = OpenCursor("PK_DAECatalogObjectNames", true);
				try
				{
					string LName = LCatalogObject.Name;
					int LDepth = Schema.Object.GetQualifierCount(LName);
					for (int LIndex = 0; LIndex <= LDepth; LIndex++)
					{
						LCatalogObjectNames.Insert(new object[] { LIndex, LName, LCatalogObject.ID });
						LName = Schema.Object.Dequalify(LName);
					}
				}
				finally
				{
					CloseCursor(LCatalogObjectNames);
				}
				
				Schema.ScalarType LScalarType = AObject as Schema.ScalarType;
				if (LScalarType != null)
				{
					// Insert the DAEScalarTypes row
					InsertRow("DAEScalarTypes", new object[] { LScalarType.ID, LScalarType.UniqueSortID, LScalarType.SortID });
				}
				
				Schema.Operator LOperator = AObject as Schema.Operator;
				if (LOperator != null)			
				{
					// Ensure the DAEOperatorNames and DAEOperatorNameNames rows exist
					SQLStoreCursor LOperatorNames = OpenMatchedCursor("PK_DAEOperatorNames", true, LOperator.OperatorName);
					try
					{
						if (!LOperatorNames.Next())
						{
							LOperatorNames.Insert(new object[] { LOperator.OperatorName });
							
							SQLStoreCursor LOperatorNameNames = OpenCursor("PK_DAEOperatorNameNames", true);
							try
							{
								string LName = LOperator.OperatorName;
								int LDepth = Schema.Object.GetQualifierCount(LName);
								for (int LIndex = 0; LIndex <= LDepth; LIndex++)
								{
									LOperatorNameNames.Insert(new object[] { LIndex, LName, LOperator.OperatorName });
									LName = Schema.Object.Dequalify(LName);
								}
							}
							finally
							{
								CloseCursor(LOperatorNameNames);
							}
						}
					}
					finally
					{
						CloseCursor(LOperatorNames);
					}
					
					// Insert the DAEOperators row
					InsertRow("DAEOperators", LOperator.ID, LOperator.OperatorName, LOperator.Signature.ToString());
				}
				
				if (AObject is Schema.EventHandler)
				{
					Schema.ScalarTypeEventHandler LScalarTypeEventHandler = AObject as Schema.ScalarTypeEventHandler;
					if (LScalarTypeEventHandler != null)
						InsertRow("DAEEventHandlers", LScalarTypeEventHandler.ID, LScalarTypeEventHandler.Operator.ID, LScalarTypeEventHandler.ScalarType.ID);
					
					Schema.TableVarEventHandler LTableVarEventHandler = AObject as Schema.TableVarEventHandler;
					if (LTableVarEventHandler != null)
						InsertRow("DAEEventHandlers", LTableVarEventHandler.ID, LTableVarEventHandler.Operator.ID, LTableVarEventHandler.TableVar.ID);
						
					Schema.TableVarColumnEventHandler LTableVarColumnEventHandler = AObject as Schema.TableVarColumnEventHandler;
					if (LTableVarColumnEventHandler != null)
						InsertRow("DAEEventHandlers", LTableVarColumnEventHandler.ID, LTableVarColumnEventHandler.Operator.ID, LTableVarColumnEventHandler.TableVarColumn.ID);
				}
				
				// Insert the DAEDevices row
				Schema.Device LDevice = AObject as Schema.Device;
				if (LDevice != null)
					InsertRow("DAEDevices", LDevice.ID, LDevice.ResourceManagerID, LDevice.ReconcileMaster.ToString(), LDevice.ReconcileMode.ToString());
				
				Schema.DeviceScalarType LDeviceScalarType = AObject as Schema.DeviceScalarType;
				if (LDeviceScalarType != null)
					InsertRow("DAEDeviceObjects", LDeviceScalarType.ID, LDeviceScalarType.Device.ID, LDeviceScalarType.ScalarType.ID);
				
				Schema.DeviceOperator LDeviceOperator = AObject as Schema.DeviceOperator;
				if (LDeviceOperator != null)
					InsertRow("DAEDeviceObjects", LDeviceOperator.ID, LDeviceOperator.Device.ID, LDeviceOperator.Operator.ID);
				
				// Insert the DAERights rows
				string[] LRights = LCatalogObject.GetRights();
				for (int LIndex = 0; LIndex < LRights.Length; LIndex++)
					InsertRow("DAERights", LRights[LIndex], LCatalogObject.Owner.ID, LCatalogObject.ID);
			}
			
			InsertAllObjectsAndDependencies(AObject);
		}
		
		public void UpdatePersistentObjectData(Schema.Object AObject, string AObjectScript)
		{
			SQLStoreCursor LObjects = OpenMatchedCursor("PK_DAEObjects", true, AObject.ID);
			try
			{
				if (LObjects.Next())
				{
					LObjects[15] = AObjectScript;
					LObjects.Update();
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}
		
		public void UpdatePersistentObject(Schema.Object AObject, string AObjectScript)
		{
			// Update the DAEDevices row
			Schema.Device LDevice = AObject as Schema.Device;
			if (LDevice != null)
			{
				SQLStoreCursor LDevices = OpenMatchedCursor("PK_DAEDevices", true, LDevice.ID);
				try
				{
					if (LDevices.Next())
					{
						LDevices[1] = LDevice.ResourceManagerID;
						LDevices[2] = LDevice.ReconcileMaster.ToString();
						LDevices[3] = LDevice.ReconcileMode.ToString();
						LDevices.Update();
					}
				}
				finally
				{
					CloseCursor(LDevices);
				}
			}
				
			// Update the DAEScalarTypes row
			Schema.ScalarType LScalarType = AObject as Schema.ScalarType;
			if (LScalarType != null)
			{
				SQLStoreCursor LScalarTypes = OpenMatchedCursor("PK_DAEScalarTypes", true, LScalarType.ID);
				try
				{
					if (LScalarTypes.Next())
					{
						LScalarTypes[1] = LScalarType.UniqueSortID;
						LScalarTypes[2] = LScalarType.SortID;
						LScalarTypes.Update();
					}
				}
				finally
				{
					CloseCursor(LScalarTypes);
				}
			}

			// Delete the DAEObjectDependencies rows
			DeleteObjectDependencies(AObject);
		
			// Delete the DAEObjects rows
			DeleteRows("PK_DAEObjects", AObject.ID);
			DeleteRows("IDX_DAEObjects_Catalog_Object_ID", AObject.ID);

			// Insert the DAEObjects row for the main object
			InsertRow
			(
				"DAEObjects", 
				AObject.ID, 
				AObject.Name, 
				AObject.Library == null ? String.Empty : AObject.Library.Name,
				Schema.Object.EnsureDescriptionLength(AObject.DisplayName),
				Schema.Object.EnsureDescriptionLength(AObject.Description),
				AObject.GetType().Name,
				AObject.IsSystem,
				AObject.IsRemotable,
				AObject.IsGenerated,
				AObject.IsATObject,
				AObject.IsSessionObject,
				AObject.IsPersistent,
				AObject.CatalogObjectID,
				AObject.ParentObjectID,
				AObject.GeneratorID,
				AObjectScript
			);
			
			// Insert the DAEObjects rows
			// Insert the DAEOObjectDependencies rows
			InsertAllObjectsAndDependencies(AObject);
		}

		public void DeletePersistentObject(Schema.Object AObject)
		{
			// Delete the DAEDeviceUsers rows
			if (AObject is Schema.Device)
				DeleteRows("IDX_DAEDeviceUsers_Device_ID", AObject.ID);
				
			// Delete the DAERoleRightAssignments rows
			// Delete the DAEUserRightAssignments rows
			SQLStoreCursor LRights = OpenMatchedCursor("IDX_DAERights_Catalog_Object_ID", false, AObject.ID);
			try
			{
				while (LRights.Next())
				{
					DeleteRows("IDX_DAERoleRightAssignments_Right_Name", LRights[0]);
					DeleteRows("IDX_DAEUserRightAssignments_Right_Name", LRights[0]);
				}
			}
			finally
			{
				CloseCursor(LRights);
			}
			
			// Delete the DAERights rows
			DeleteRows("IDX_DAERights_Catalog_Object_ID", AObject.ID);
			
			// Delete the DAEObjectDependencies rows
			DeleteObjectDependencies(AObject);
			
			// Delete the DAEObjects rows
			DeleteRows("PK_DAEObjects", AObject.ID);
			DeleteRows("IDX_DAEObjects_Catalog_Object_ID", AObject.ID);
			
			// Delete the DAEScalarTypes row
			if (AObject is Schema.ScalarType)
				DeleteRows("PK_DAEScalarTypes", AObject.ID);
			
			// Delete the DAEOperators row
			if (AObject is Schema.Operator)
				DeleteRows("PK_DAEOperators", AObject.ID);
				
			// Delete the DAEEventHandlers row
			if (AObject is Schema.EventHandler)
				DeleteRows("PK_DAEEventHandlers", AObject.ID);
				
			// Delete the DAEDevices row
			if (AObject is Schema.Device)
				DeleteRows("PK_DAEDevices", AObject.ID);
				
			// Delete the DAEDeviceObjects rows
			if (AObject is Schema.DeviceObject)
				DeleteRows("PK_DAEDeviceObjects", AObject.ID);
			
			if (AObject is Schema.CatalogObject)
			{
				// Delete the DAECatalogObjectNames rows
				DeleteRows("IDX_DAECatalogObjectNames_ID", AObject.ID);
			
				// Delete the DAECatalogObjects row
				DeleteRows("PK_DAECatalogObjects", AObject.ID);
			}
		}
		
		public void SetCatalogObjectOwner(int ACatalogObjectID, string AUserID)
		{
			SQLStoreCursor LCatalogObjects = OpenMatchedCursor("PK_DAECatalogObjects", true, ACatalogObjectID);
			try
			{
				if (LCatalogObjects.Next())
				{
					LCatalogObjects[3] = AUserID;
					LCatalogObjects.Update();
				}
			}
			finally
			{
				CloseCursor(LCatalogObjects);
			}
			
			SQLStoreCursor LRights = OpenMatchedCursor("IDX_DAERights_Catalog_Object_ID", true, ACatalogObjectID);
			try
			{
				while (LRights.Next())
				{
					LRights[1] = AUserID;
					LRights.Update();
				}
			}
			finally
			{
				CloseCursor(LRights);
			}
		}
		
		public Schema.ObjectHeader SelectObject(int AObjectID)
		{
			SQLStoreCursor LObjects = OpenMatchedCursor("PK_DAEObjects", false, AObjectID);
			try
			{
				if (LObjects.Next())
					return
						new Schema.ObjectHeader
						(
							(int)LObjects[0], // ID
							(string)LObjects[1], // Name
							(string)LObjects[2], // LibraryName,
							(string)LObjects[3], // DisplayName,
							(string)LObjects[5], // Type,
							(bool)LObjects[6], // IsSystem,
							(bool)LObjects[7], // IsRemotable,
							(bool)LObjects[8], // IsGenerated,
							(bool)LObjects[9], // IsATObject,
							(bool)LObjects[10], // IsSessionObject,
							(bool)LObjects[11], // IsPersistent,
							(int)LObjects[12], // CatalogObjectID,
							(int)LObjects[13], // ParentObjectID
							(int)LObjects[14] // GeneratorObjectID
						);
						
				return null;
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}
		
		public Schema.FullObjectHeader SelectFullObject(int AObjectID)
		{
			SQLStoreCursor LObjects = OpenMatchedCursor("PK_DAEObjects", false, AObjectID);
			try
			{
				if (LObjects.Next())
					return
						new Schema.FullObjectHeader
						(
							(int)LObjects[0], // ID
							(string)LObjects[1], // Name
							(string)LObjects[2], // LibraryName,
							(string)LObjects[15], // Script,
							(string)LObjects[3], // DisplayName,
							(string)LObjects[5], // Type,
							(bool)LObjects[6], // IsSystem,
							(bool)LObjects[7], // IsRemotable,
							(bool)LObjects[8], // IsGenerated,
							(bool)LObjects[9], // IsATObject,
							(bool)LObjects[10], // IsSessionObject,
							(bool)LObjects[11], // IsPersistent,
							(int)LObjects[12], // CatalogObjectID,
							(int)LObjects[13], // ParentObjectID
							(int)LObjects[14] // GeneratorObjectID
						);
						
				return null;
			}
			finally
			{
				CloseCursor(LObjects);
			}
		}
		
		public Schema.FullObjectHeaders SelectChildObjects(int AParentObjectID)
		{
			Schema.FullObjectHeaders LHeaders = new Schema.FullObjectHeaders();
			SQLStoreCursor LObjects = OpenMatchedCursor("IDX_DAEObjects_Parent_Object_ID", false, AParentObjectID);
			try
			{
				while (LObjects.Next())
				{
					LHeaders.Add
					(
						new Schema.FullObjectHeader
						(
							(int)LObjects[0], // ID
							(string)LObjects[1], // Name
							(string)LObjects[2], // LibraryName,
							(string)LObjects[15], // Script,
							(string)LObjects[3], // DisplayName,
							(string)LObjects[5], // Type,
							(bool)LObjects[6], // IsSystem,
							(bool)LObjects[7], // IsRemotable,
							(bool)LObjects[8], // IsGenerated,
							(bool)LObjects[9], // IsATObject,
							(bool)LObjects[10], // IsSessionObject,
							(bool)LObjects[11], // IsPersistent,
							(int)LObjects[12], // CatalogObjectID,
							(int)LObjects[13], // ParentObjectID
							(int)LObjects[14] // GeneratorObjectID
						)
					);
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
			return LHeaders;
		}
		
		public Schema.PersistentObjectHeaders SelectPersistentChildObjects(int ACatalogObjectID)
		{
			Schema.PersistentObjectHeaders LHeaders = new Schema.PersistentObjectHeaders();
			SQLStoreCursor LObjects = OpenMatchedCursor("IDX_DAEObjects_Catalog_Object_ID", false, ACatalogObjectID);
			try
			{
				while (LObjects.Next())
				{
					if ((bool)LObjects[11])
					{
						LHeaders.Add
						(
							new Schema.PersistentObjectHeader
							(
								(int)LObjects[0], // ID
								(string)LObjects[1], // Name
								(string)LObjects[2], // LibraryName
								(string)LObjects[15], // Script
								(string)LObjects[3], // DisplayName
								(string)LObjects[5], // ObjectType
								(bool)LObjects[6], // IsSystem 
								(bool)LObjects[7], // IsRemotable
								(bool)LObjects[8], // IsGenerated
								(bool)LObjects[9], // IsATObject
								(bool)LObjects[10] // IsSessionObject
							)
						);
					}
				}
			}
			finally
			{
				CloseCursor(LObjects);
			}
			return LHeaders;
		}
		
		private void SelectObjectDependents(SQLStoreCursor AObjects, int AObjectID, int ALevel, Schema.DependentObjectHeaders AHeaders, bool ARecursive)
		{
			SQLStoreCursor LDependencies = OpenMatchedCursor("IDX_DAEObjectDependencies_Dependency_Object_ID", false, AObjectID);
			try
			{
				while (LDependencies.Next())
				{
					if (!AHeaders.Contains((int)LDependencies[0]))
					{
						if (AObjects.FindKey(new object[] { LDependencies[0] }))
						{
							AHeaders.Add
							(
								new Schema.DependentObjectHeader
								(
									(int)AObjects[0], // ID
									(string)AObjects[1], // Name
									(string)AObjects[2], // LibraryName
									(string)AObjects[3], // DisplayName
									(string)AObjects[4], // Description
									(string)AObjects[5], // ObjectType
									(bool)AObjects[6], // IsSystem 
									(bool)AObjects[7], // IsRemotable
									(bool)AObjects[8], // IsGenerated
									(bool)AObjects[9], // IsATObject
									(bool)AObjects[10], // IsSessionObject
									(bool)AObjects[11], // IsPersistent
									(int)AObjects[12], // CatalogObjectID
									(int)AObjects[13], // ParentObjectID,
									(int)AObjects[14], // GeneratorObjectID,
									ALevel, // Level
									AHeaders.Count + 1 // Sequence
								)
							);
							
							if (ARecursive)
								SelectObjectDependents(AObjects, (int)LDependencies[0], ALevel + 1, AHeaders, ARecursive);
						}
					}
				}
			}
			finally
			{
				CloseCursor(LDependencies);
			}
		}

		public Schema.DependentObjectHeaders SelectObjectDependents(int AObjectID, bool ARecursive)
		{
			Schema.DependentObjectHeaders LHeaders = new Schema.DependentObjectHeaders();
			SQLStoreCursor LObjects = OpenCursor("PK_DAEObjects", false);
			try
			{
				SelectObjectDependents(LObjects, AObjectID, 1, LHeaders, ARecursive);
			}
			finally
			{
				CloseCursor(LObjects);
			}
			return LHeaders;
		}
		
		private void SelectObjectDependencies(SQLStoreCursor AObjects, int AObjectID, int ALevel, Schema.DependentObjectHeaders AHeaders, bool ARecursive)
		{
			SQLStoreCursor LDependencies = OpenMatchedCursor("PK_DAEObjectDependencies", false, AObjectID);
			try
			{
				while (LDependencies.Next())
				{
					if (!AHeaders.Contains((int)LDependencies[1]))
					{
						if (AObjects.FindKey(new object[] { LDependencies[1] }))
						{
							AHeaders.Add
							(
								new Schema.DependentObjectHeader
								(
									(int)AObjects[0], // ID
									(string)AObjects[1], // Name
									(string)AObjects[2], // LibraryName
									(string)AObjects[3], // DisplayName
									(string)AObjects[4], // Description
									(string)AObjects[5], // ObjectType
									(bool)AObjects[6], // IsSystem 
									(bool)AObjects[7], // IsRemotable
									(bool)AObjects[8], // IsGenerated
									(bool)AObjects[9], // IsATObject
									(bool)AObjects[10], // IsSessionObject
									(bool)AObjects[11], // IsPersistent
									(int)AObjects[12], // CatalogObjectID
									(int)AObjects[13], // ParentObjectID,
									(int)AObjects[14], // GeneratorObjectID,
									ALevel, // Level
									AHeaders.Count + 1 // Sequence
								)
							);
							
							if (ARecursive)
								SelectObjectDependencies(AObjects, (int)LDependencies[1], ALevel + 1, AHeaders, ARecursive);
						}
					}
				}
			}
			finally
			{
				CloseCursor(LDependencies);
			}
		}

		public Schema.DependentObjectHeaders SelectObjectDependencies(int AObjectID, bool ARecursive)
		{
			Schema.DependentObjectHeaders LHeaders = new Schema.DependentObjectHeaders();
			SQLStoreCursor LObjects = OpenCursor("PK_DAEObjects", false);
			try
			{
				SelectObjectDependencies(LObjects, AObjectID, 1, LHeaders, ARecursive);
			}
			finally
			{
				CloseCursor(LObjects);
			}
			return LHeaders;
		}
		
		public Schema.ScalarTypeHeader SelectScalarType(int AScalarTypeID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEScalarTypes", false, AScalarTypeID);
			try
			{
				if (LCursor.Next())
					return new Schema.ScalarTypeHeader((int)LCursor[0], (int)LCursor[1], (int)LCursor[2]);
					
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}
		
		/// <summary>Returns the set of handlers that invoke the given operator</summary>
		public List<int> SelectOperatorHandlers(int AOperatorID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEEventHandlers_Operator_ID", false, AOperatorID);
			try
			{
				List <int> LHandlers = new List<int>();
				
				while (LCursor.Next())
					LHandlers.Add((int)LCursor[0]);
				
				return LHandlers;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public List<int> SelectObjectHandlers(int ASourceObjectID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEEventHandlers_Source_Object_ID", false, ASourceObjectID);
			try
			{
				List<int> LHandlers = new List<int>();
				
				while (LCursor.Next())
					LHandlers.Add((int)LCursor[0]);
				
				return LHandlers;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public TableMapHeader SelectApplicationTransactionTableMap(int ASourceTableVarID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEApplicationTransactionTableMaps", false, ASourceTableVarID);
			try
			{
				if (LCursor.Next())
					return new TableMapHeader((int)LCursor[0], (int)LCursor[1], (int)LCursor[2]);
					
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void InsertApplicationTransactionTableMap(int ASourceTableVarID, int ATranslatedTableVarID)
		{
			InsertRow("DAEApplicationTransactionTableMaps", ASourceTableVarID, ATranslatedTableVarID, -1);
		}

		public void UpdateApplicationTransactionTableMap(int ASourceTableVarID, int ADeletedTableVarID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEApplicationTransactionTableMaps", true, ASourceTableVarID);
			try
			{
				if (LCursor.Next())
				{
					LCursor[2] = ADeletedTableVarID;
					LCursor.Update();
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void DeleteApplicationTransactionTableMap(int ASourceTableVarID)
		{
			DeleteRows("PK_DAEApplicationTransactionTableMaps", ASourceTableVarID);
		}

		public int SelectTranslatedApplicationTransactionOperatorID(int ASourceOperatorID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEApplicationTransactionOperatorMaps", false, ASourceOperatorID);
			try
			{
				if (LCursor.Next())
					return (int)LCursor[1];
				return -1;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public int SelectSourceApplicationTransactionOperatorID(int ATranslatedOperatorID)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("IDX_DAEApplicationTransactionOperatorMaps_Translated_Operator_ID", false, ATranslatedOperatorID);
			try
			{
				if (LCursor.Next())
					return (int)LCursor[0];
				return -1;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void InsertApplicationTransactionOperatorMap(int ASourceOperatorID, int ATranslatedOperatorID)
		{
			InsertRow("DAEApplicationTransactionOperatorMaps", ASourceOperatorID, ATranslatedOperatorID);
		}

		public void DeleteApplicationTransactionOperatorMap(int ASourceOperatorID)
		{
			DeleteRows("PK_DAEApplicationTransactionOperatorMaps", ASourceOperatorID);
		}

		public string SelectApplicationTransactionOperatorNameMap(string ASourceOperatorName)
		{
			SQLStoreCursor LCursor = OpenMatchedCursor("PK_DAEApplicationTransactionOperatorNameMaps", false, ASourceOperatorName);
			try
			{
				if (LCursor.Next())
					return (string)LCursor[1];
				return null;
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		public void InsertApplicationTransactionOperatorNameMap(string ASourceOperatorName, string ATranslatedOperatorName)
		{
			InsertRow("DAEApplicationTransactionOperatorNameMaps", ASourceOperatorName, ATranslatedOperatorName);
		}

		#endregion
	}
}