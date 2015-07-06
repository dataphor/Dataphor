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
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.BOP;

// TODO: Study the performance impact of using literals vs. parameterized statements, including the impact of the required String.Replace calls

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	internal abstract class StoreObjectHeader
	{
		public StoreObjectHeader(string name) : base()
		{
			_name = name;
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}

		public override bool Equals(object objectValue)
		{
			StoreObjectHeader header = objectValue as StoreObjectHeader;
			return (header != null) && (header.Name == _name);
		}
	}
	
	internal class StoreTableHeader : StoreObjectHeader
	{
		public StoreTableHeader(string tableName, List<string> columns, string primaryKeyName) : base(tableName)
		{
			_columns = columns;
			_primaryKeyName = primaryKeyName;
		}
		
		private List<string> _columns;
		public List<string> Columns { get { return _columns; } }
		
		private string _primaryKeyName;
		public string PrimaryKeyName { get { return _primaryKeyName; } }
	}
	
	internal class StoreIndexHeader : StoreObjectHeader
	{
		public StoreIndexHeader(string tableName, string indexName, bool isUnique, List<string> columns) : base(indexName)
		{
			_tableName = tableName;
			_isUnique = isUnique;
			_columns = columns;
		}
		
		private string _tableName;
		public string TableName { get { return _tableName; } }
		
		private bool _isUnique;
		public bool IsUnique { get { return _isUnique; } }

		private List<string> _columns;
		public List<string> Columns { get { return _columns; } }
		
		private SQLIndex _sQLIndex;
		public SQLIndex SQLIndex
		{
			get
			{
				if (_sQLIndex == null)
				{
					_sQLIndex = new SQLIndex(Name);
					_sQLIndex.IsUnique = _isUnique;
					foreach (String column in _columns)
						_sQLIndex.Columns.Add(new SQLIndexColumn(column, true));
				}
				
				return _sQLIndex;
			}
		}
	}
	
	internal class StoreTableHeaders : Dictionary<string, StoreTableHeader> {}

	internal class StoreIndexHeaders : Dictionary<string, StoreIndexHeader> {}

	public class CatalogStore : System.Object
	{
		public CatalogStore() { }
		
		public void CreateStore()
		{
			_store = (SQLStore)Activator.CreateInstance(Type.GetType(_storeClassName, true, true));
			_store.ConnectionString = _storeConnectionString;
			_store.MaxConnections = _maxConnections;
		}
		
		/// <summary>Initializes the catalog store, ensuring the store has been created.</summary>
		public void Initialize(Server.Engine server)
		{
			CreateStore();
			_store.Initialize();
			
			// Establish a connection to the catalog store server
			SQLStoreConnection connection = _store.Connect();
			try
			{
				// if there is no DAEServerInfo table
				if (!connection.HasTable("DAEServerInfo"))
				{
					// run the SystemStoreCatalog sql script
					using (Stream stream = _store.GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.SystemStoreCatalog.sql"))
					{
						connection.ExecuteScript(new StreamReader(stream).ReadToEnd());
					}
					
					connection.ExecuteStatement
					(
						String.Format
						(
							"insert into DAEServerInfo (Name, Version, MaxConcurrentProcesses, ProcessWaitTimeout, ProcessTerminationTimeout, PlanCacheSize) values ('{0}', '{1}', {2}, {3}, {4}, {5})",
							server.Name.Replace("'", "''"),
							server.Catalog.Libraries[server.SystemLibrary.Name].Version.ToString(),
							server.MaxConcurrentProcesses,
							(int)server.ProcessWaitTimeout.TotalMilliseconds,
							(int)server.ProcessTerminationTimeout.TotalMilliseconds,
							server.PlanCacheSize
						)
					);
					
					connection.ExecuteStatement(String.Format("insert into DAELoadedLibraries (Library_Name) values ('{0}')", Server.Engine.SystemLibraryName));
					connection.ExecuteStatement(String.Format("insert into DAELibraryVersions (Library_Name, VersionNumber) values ('{0}', '{1}')", Server.Engine.SystemLibraryName, GetType().Assembly.GetName().Version.ToString()));
					connection.ExecuteStatement(String.Format("insert into DAELibraryOwners (Library_Name, Owner_User_ID) values ('{0}', '{1}')", Server.Engine.SystemLibraryName, Server.Engine.SystemUserID));
				}
			}
			finally
			{
				connection.Dispose();
			}
		}
		
		private SQLStore _store;
		
		public bool UseCursorCache { get { return _store.SupportsMARS; } }
		public bool InsertThroughCursor { get { return _store.SupportsUpdatableCursor; } }
		public bool DeleteThroughCursor { get { return _store.SupportsUpdatableCursor; } }
		
		public SQLStoreCounters Counters { get { return _store.Counters; } }
		
		private string _storeClassName;
		public string StoreClassName
		{
			get { return _storeClassName; }
			set
			{
				if (_storeClassName != value)
				{
					if (_store != null)
						throw new CatalogException(CatalogException.Codes.CatalogStoreInitialized);
						
					if (String.IsNullOrEmpty(value))
						throw new CatalogException(CatalogException.Codes.CatalogStoreClassNameRequired);
						
					_storeClassName = value;
				}
			}
		}
		
		private string _storeConnectionString;
		public string StoreConnectionString
		{
			get { return _storeConnectionString; }
			set
			{
				if (_storeConnectionString != value)
				{
					if (_store != null)
						throw new CatalogException(CatalogException.Codes.CatalogStoreInitialized);
						
					_storeConnectionString = value;
				}
			}
		}
		
		private int _maxConnections;
		public int MaxConnections
		{
			get { return _maxConnections; }
			set 
			{ 
				_maxConnections = value;
				if (_store != null)
					_store.MaxConnections = value; 
			}
		}

        private int _maxNameIndexDepth = -1;
        public int GetMaxNameIndexDepth()
        {
            return _maxNameIndexDepth;
        }

        public void SetMaxNameIndexDepth(int maxDepth)
        {
            _maxNameIndexDepth = maxDepth;
        }

        public void ClearMaxNameIndexDepth()
        {
            _maxNameIndexDepth = -1;
        }

		#region Connection
		
		// TODO: I'm not happy with this but without a major re-architecture of the crap-wrapper layer, I'm not sure what else to do...
		// The provider factory seems attractive, but it fails fundamentally to deliver on actual abstraction because it ignores 
		// even basic syntactic disparity like parameter markers and basic semantic disparity like parameter types. The crap wrapper
		// is already plugged in to the catalog device (see CatalogDeviceTable), so I'm taking the easy way out on this one...
		public SQLConnection GetSQLConnection()
		{
			return _store.GetSQLConnection();
		}
		
		public CatalogStoreConnection Connect()
		{
			return new CatalogStoreConnection(this, _store.Connect());
		}

		private List<CatalogStoreConnection> _connectionPool = new List<CatalogStoreConnection>();
		
		public CatalogStoreConnection AcquireConnection()
		{
			lock (_connectionPool)
			{
				if (_connectionPool.Count > 0)
				{
					CatalogStoreConnection connection = _connectionPool[0];
					_connectionPool.RemoveAt(0);
					return connection;
				}
			}
			
			return Connect();
		}
		
		public void ReleaseConnection(CatalogStoreConnection connection)
		{
			lock (_connectionPool)
			{
				_connectionPool.Add(connection);
			}
		}
		
		#endregion

		#region StoreStructureDefinitions
		
		private StoreTableHeaders _tableHeaders = new StoreTableHeaders();
		
		private StoreTableHeader BuildTableHeader(string tableName)
		{
			List<string> columns = new List<string>();
			
			switch (tableName)
			{
				case "DAEServerInfo" : 
					columns.Add("ID");
					columns.Add("Name");
					columns.Add("Version");
					columns.Add("MaxConcurrentProcesses");
					columns.Add("ProcessWaitTimeout");
					columns.Add("ProcessTerminationTimeout");
					columns.Add("PlanCacheSize");
					return new StoreTableHeader(tableName, columns, "PK_DAEServerInfo");

				case "DAEUsers" : 
					columns.Add("ID");
					columns.Add("Name");
					columns.Add("Data");
					return new StoreTableHeader(tableName, columns, "PK_DAEUsers");
					
				case "DAELoadedLibraries" : 
					columns.Add("Library_Name");
					return new StoreTableHeader(tableName, columns, "PK_DAELoadedLibraries");
					
				case "DAELibraryDirectories" : 
					columns.Add("Library_Name");
					columns.Add("Directory");
					return new StoreTableHeader(tableName, columns, "PK_DAELibraryDirectories");
					
				case "DAELibraryVersions" : 
					columns.Add("Library_Name");
					columns.Add("VersionNumber");
					return new StoreTableHeader(tableName, columns, "PK_DAELibraryVersions");
					
				case "DAELibraryOwners" : 
					columns.Add("Library_Name");
					columns.Add("Owner_User_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAELibraryOwners");
					
				case "DAEObjects" : 
					columns.Add("ID");
					columns.Add("Name");
					columns.Add("Library_Name");
					columns.Add("DisplayName");
					columns.Add("Description");
					columns.Add("Type");
					columns.Add("IsSystem");
					columns.Add("IsRemotable");
					columns.Add("IsGenerated");
					columns.Add("IsATObject");
					columns.Add("IsSessionObject");
					columns.Add("IsPersistent");
					columns.Add("Catalog_Object_ID");
					columns.Add("Parent_Object_ID");
					columns.Add("Generator_Object_ID");
					columns.Add("ServerData");
					return new StoreTableHeader(tableName, columns, "PK_DAEObjects");
					
				case "DAEObjectDependencies" : 
					columns.Add("Object_ID");
					columns.Add("Dependency_Object_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEObjectDependencies");

				case "DAECatalogObjects" : 
					columns.Add("ID");
					columns.Add("Name");
					columns.Add("Library_Name");
					columns.Add("Owner_User_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAECatalogObjects");

				case "DAECatalogObjectNames" : 
					columns.Add("Depth");
					columns.Add("Name");
					columns.Add("ID");
					return new StoreTableHeader(tableName, columns, "PK_DAECatalogObjectNames");
					
				case "DAEBaseCatalogObjects" : 
					columns.Add("ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEBaseCatalogObjects");
					
				case "DAEScalarTypes" : 
					columns.Add("ID");
					columns.Add("Unique_Sort_ID");
					columns.Add("Sort_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEScalarTypes");

				case "DAEOperatorNames" : 
					columns.Add("Name");
					return new StoreTableHeader(tableName, columns, "PK_DAEOperatorNames");
				
				case "DAEOperatorNameNames" : 
					columns.Add("Depth");
					columns.Add("Name");
					columns.Add("OperatorName");
					return new StoreTableHeader(tableName, columns, "PK_DAEOperatorNameNames");

				case "DAEOperators" : 
					columns.Add("ID");
					columns.Add("OperatorName");
					columns.Add("Signature");
					columns.Add("Locator");
					columns.Add("Line");
					columns.Add("LinePos");
					return new StoreTableHeader(tableName, columns, "PK_DAEOperators");

				case "DAEEventHandlers" : 
					columns.Add("ID");
					columns.Add("Operator_ID");
					columns.Add("Source_Object_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEEventHandlers");

				case "DAEApplicationTransactionTableMaps" : 
					columns.Add("Source_TableVar_ID");
					columns.Add("Translated_TableVar_ID");
					columns.Add("Deleted_TableVar_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEApplicationTransactionTableMaps");

				case "DAEApplicationTransactionOperatorNameMaps" : 
					columns.Add("Source_OperatorName");
					columns.Add("Translated_OperatorName");
					return new StoreTableHeader(tableName, columns, "PK_DAEApplicationTransactionOperatorNameMaps");
				
				case "DAEApplicationTransactionOperatorMaps" : 
					columns.Add("Source_Operator_ID");
					columns.Add("Translated_Operator_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEApplicationTransactionOperatorMaps");

				case "DAEUserRoles" : 
					columns.Add("User_ID");
					columns.Add("Role_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEUserRoles");

				case "DAERights" : 
					columns.Add("Name");
					columns.Add("Owner_User_ID");
					columns.Add("Catalog_Object_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAERights");

				case "DAERoleRightAssignments" : 
					columns.Add("Role_ID");
					columns.Add("Right_Name");
					columns.Add("IsGranted");
					return new StoreTableHeader(tableName, columns, "PK_DAERoleRightAssignments");

				case "DAEUserRightAssignments" : 
					columns.Add("User_ID");
					columns.Add("Right_Name");
					columns.Add("IsGranted");
					return new StoreTableHeader(tableName, columns, "PK_DAEUserRightAssignments");

				case "DAEDevices" : 
					columns.Add("ID");
					columns.Add("ReconciliationMaster");
					columns.Add("ReconciliationMode");
					return new StoreTableHeader(tableName, columns, "PK_DAEDevices");
				
				case "DAEDeviceUsers" : 
					columns.Add("User_ID");
					columns.Add("Device_ID");
					columns.Add("UserID");
					columns.Add("Data");
					columns.Add("ConnectionParameters");
					return new StoreTableHeader(tableName, columns, "PK_DAEDeviceUsers");

				case "DAEDeviceObjects" : 
					columns.Add("ID");
					columns.Add("Device_ID");
					columns.Add("Mapped_Object_ID");
					return new StoreTableHeader(tableName, columns, "PK_DAEDeviceObjects");
					
				case "DAEClasses" :
					columns.Add("Name");
					columns.Add("Library_Name");
					return new StoreTableHeader(tableName, columns, "PK_DAEClasses");
			}

			Error.Fail("Table header could not be constructed for store table \"{0}\".", tableName);
			return null;
		}
		
		internal StoreTableHeader GetTableHeader(string tableName)
		{
			lock (_tableHeaders)
			{
				StoreTableHeader tableHeader;
				if (_tableHeaders.TryGetValue(tableName, out tableHeader))
					return tableHeader;
				
				tableHeader = BuildTableHeader(tableName);
				_tableHeaders.Add(tableName, tableHeader);
				return tableHeader;
			}
		}
		
		private StoreIndexHeaders _indexHeaders = new StoreIndexHeaders();
		
		private StoreIndexHeader BuildIndexHeader(string indexName)
		{
			List<string> columns = new List<string>();
			
			switch (indexName)
			{
				case "PK_DAEServerInfo" : 
					columns.Add("ID");
					return new StoreIndexHeader("DAEServerInfo", indexName, true, columns);
					
				case "PK_DAEUsers" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEUsers", indexName, true, columns);
				
				case "PK_DAELoadedLibraries" :
					columns.Add("Library_Name");
					return new StoreIndexHeader("DAELoadedLibraries", indexName, true, columns);
				
				case "PK_DAELibraryDirectories" :
					columns.Add("Library_Name");
					return new StoreIndexHeader("DAELibraryDirectories", indexName, true, columns);
				
				case "PK_DAELibraryVersions" :
					columns.Add("Library_Name");
					return new StoreIndexHeader("DAELibraryVersions", indexName, true, columns);
				
				case "PK_DAELibraryOwners" :
					columns.Add("Library_Name");
					return new StoreIndexHeader("DAELibraryOwners", indexName, true, columns);
				
				case "PK_DAEObjects" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEObjects", indexName, true, columns);
				
				case "IDX_DAEObjects_Catalog_Object_ID" :
					columns.Add("Catalog_Object_ID");
					return new StoreIndexHeader("DAEObjects", indexName, false, columns);
					
				case "IDX_DAEObjects_Parent_Object_ID" :
					columns.Add("Parent_Object_ID");
					return new StoreIndexHeader("DAEObjects", indexName, false, columns);
					
				case "IDX_DAEObjects_Generator_Object_ID" :
					columns.Add("Generator_Object_ID");
					return new StoreIndexHeader("DAEObjects", indexName, false, columns);
				
				case "PK_DAEObjectDependencies" :
					columns.Add("Object_ID");
					columns.Add("Dependency_Object_ID");
					return new StoreIndexHeader("DAEObjectDependencies", indexName, true, columns);
				
				case "IDX_DAEObjectDependencies_Dependency_Object_ID" :
					columns.Add("Dependency_Object_ID");
					return new StoreIndexHeader("DAEObjectDependencies", indexName, false, columns);
				
				case "PK_DAECatalogObjects" :
					columns.Add("ID");
					return new StoreIndexHeader("DAECatalogObjects", indexName, true, columns);
				
				case "IDX_DAECatalogObjects_Owner_User_ID" :
					columns.Add("Owner_User_ID");
					return new StoreIndexHeader("DAECatalogObjects", indexName, false, columns);
					
				case "IDX_DAECatalogObjects_Library_Name" :
					columns.Add("Library_Name");
					return new StoreIndexHeader("DAECatalogObjects", indexName, false, columns);
					
				case "UIDX_DAECatalogObjects_Name" :
					columns.Add("Name");
					return new StoreIndexHeader("DAECatalogObjects", indexName, true, columns);
				
				case "PK_DAECatalogObjectNames" :
					columns.Add("Depth");
					columns.Add("Name");
					columns.Add("ID");
					return new StoreIndexHeader("DAECatalogObjectNames", indexName, true, columns);
				
				case "IDX_DAECatalogObjectNames_ID" :
					columns.Add("ID");
					return new StoreIndexHeader("DAECatalogObjectNames", indexName, false, columns);
				
				case "PK_DAEBaseCatalogObjects" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEBaseCatalogObjects", indexName, true, columns);
					
				case "PK_DAEScalarTypes" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEScalarTypes", indexName, true, columns);
					
				case "PK_DAEOperatorNames" :
					columns.Add("Name");
					return new StoreIndexHeader("DAEOperatorNames", indexName, true, columns);
				
				case "PK_DAEOperatorNameNames" :
					columns.Add("Depth");
					columns.Add("Name");
					columns.Add("OperatorName");
					return new StoreIndexHeader("DAEOperatorNameNames", indexName, true, columns);
				
				case "IDX_DAEOperatorNameNames_OperatorName" :
					columns.Add("OperatorName");
					return new StoreIndexHeader("DAEOperatorNameNames", indexName, false, columns);
				
				case "PK_DAEOperators" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEOperators", indexName, true, columns);
				
				case "IDX_DAEOperators_OperatorName" :
					columns.Add("OperatorName");
					return new StoreIndexHeader("DAEOperators", indexName, false, columns);
					
				case "PK_DAEEventHandlers" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEEventHandlers", indexName, true, columns);
					
				case "IDX_DAEEventHandlers_Operator_ID" :
					columns.Add("Operator_ID");
					return new StoreIndexHeader("DAEEventHandlers", indexName, false, columns);
					
				case "IDX_DAEEventHandlers_Source_Object_ID" :
					columns.Add("Source_Object_ID");
					return new StoreIndexHeader("DAEEventHandlers", indexName, false, columns);
					
				case "PK_DAEApplicationTransactionTableMaps" :
					columns.Add("Source_TableVar_ID");
					return new StoreIndexHeader("DAEApplicationTransactionTableMaps", indexName, true, columns);
					
				case "PK_DAEApplicationTransactionOperatorNameMaps" :
					columns.Add("Source_OperatorName");
					return new StoreIndexHeader("DAEApplicationTransactionOperatorNameMaps", indexName, true, columns);
					
				case "PK_DAEApplicationTransactionOperatorMaps" :
					columns.Add("Source_Operator_ID");
					return new StoreIndexHeader("DAEApplicationTransactionOperatorMaps", indexName, true, columns);
					
				case "IDX_DAEApplicationTransactionOperatorMaps_Translated_Operator_ID" :
					columns.Add("Translated_Operator_ID");
					return new StoreIndexHeader("DAEApplicationTransactionOperatorMaps", indexName, false, columns);
					
				case "PK_DAEUserRoles" :
					columns.Add("User_ID");
					columns.Add("Role_ID");
					return new StoreIndexHeader("DAEUserRoles", indexName, true, columns);
					
				case "IDX_DAEUserRoles_Role_ID" :
					columns.Add("Role_ID");
					return new StoreIndexHeader("DAEUserRoles", indexName, false, columns);
					
				case "PK_DAERights" :
					columns.Add("Name");
					return new StoreIndexHeader("DAERights", indexName, true, columns);
					
				case "IDX_DAERights_Owner_User_ID" :
					columns.Add("Owner_User_ID");
					return new StoreIndexHeader("DAERights", indexName, false, columns);
					
				case "IDX_DAERights_Catalog_Object_ID" :
					columns.Add("Catalog_Object_ID");
					return new StoreIndexHeader("DAERights", indexName, false, columns);
				
				case "PK_DAERoleRightAssignments" :
					columns.Add("Role_ID");
					columns.Add("Right_Name");
					return new StoreIndexHeader("DAERoleRightAssignments", indexName, true, columns);
					
				case "IDX_DAERoleRightAssignments_Right_Name" :
					columns.Add("Right_Name");
					return new StoreIndexHeader("DAERoleRightAssignments", indexName, false, columns);
					
				case "PK_DAEUserRightAssignments" :
					columns.Add("User_ID");
					columns.Add("Right_Name");
					return new StoreIndexHeader("DAEUserRightAssignments", indexName, true, columns);
					
				case "IDX_DAEUserRightAssignments_Right_Name" :
					columns.Add("Right_Name");
					return new StoreIndexHeader("DAEUserRightAssignments", indexName, false, columns);
					
				case "PK_DAEDevices" :
					columns.Add("ID");
					return new StoreIndexHeader("DAEDevices", indexName, true, columns);
					
				case "PK_DAEDeviceUsers" :
					columns.Add("User_ID");
					columns.Add("Device_ID");
					return new StoreIndexHeader("DAEDeviceUsers", indexName, true, columns);
					
				case "IDX_DAEDeviceUsers_Device_ID" :
					columns.Add("Device_ID");
					return new StoreIndexHeader("DAEDeviceUsers", indexName, false, columns);
					
				case "PK_DAEDeviceObjects" :
					columns.Add("ID");
					columns.Add("Device_ID");
					columns.Add("Mapped_Object_ID");
					return new StoreIndexHeader("DAEDeviceObjects", indexName, true, columns);
					
				case "IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID" :
					columns.Add("Device_ID");
					columns.Add("Mapped_Object_ID");
					return new StoreIndexHeader("DAEDeviceObjects", indexName, false, columns);
					
				case "PK_DAEClasses" :
					columns.Add("Name");
					columns.Add("Library_Name");
					return new StoreIndexHeader("DAEClasses", indexName, true, columns);
				
			}

			Error.Fail("Index header could not be constructed for store index name \"{0}\".", indexName);
			return null;
		}
		
		internal StoreIndexHeader GetIndexHeader(string indexName)
		{
			lock (_indexHeaders)
			{
				StoreIndexHeader indexHeader;
				if (_indexHeaders.TryGetValue(indexName, out indexHeader))
					return indexHeader;
					
				indexHeader = BuildIndexHeader(indexName);
				_indexHeaders.Add(indexName, indexHeader);
				return indexHeader;
			}
		}
			
		#endregion
	}
	
	public class SQLStoreCursorCache : Dictionary<string, SQLStoreCursor> {}
	
	public class CatalogStoreConnection : System.Object, IDisposable
	{
		internal CatalogStoreConnection(CatalogStore store, SQLStoreConnection connection)
		{
			_store = store;
			_connection = connection;
		}

		public void Dispose()
		{
			FlushCache();
			
			if (_connection != null)
			{
				_connection.Dispose();
				_connection = null;
			}
			
			_store = null;
		}
		
		private SQLStoreConnection _connection;
		
		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			if (_connection.TransactionCount == 0)
				FlushCache();
			_connection.BeginTransaction(SQLUtility.IsolationLevelToSQLIsolationLevel(isolationLevel));
		}

		public void CommitTransaction()
		{
			if (_connection.TransactionCount == 1)
				FlushCache();
			_connection.CommitTransaction();
		}

		public void RollbackTransaction()
		{
			if (_connection.TransactionCount == 1)
				FlushCache();
			_connection.RollbackTransaction();
		}
		
		public int TransactionCount { get { return _connection.TransactionCount; } }
		
		public bool InTransaction { get { return _connection.TransactionCount > 0; } }
		
		private CatalogStore _store;
		public CatalogStore Store { get { return _store; } }
		
		private SQLStoreCursorCache _cursorCache = new SQLStoreCursorCache();
		
		private void FlushCache()
		{
			foreach (SQLStoreCursor cursor in _cursorCache.Values)
				cursor.Dispose();
			_cursorCache.Clear();
		}
		
		public void CloseCursor(SQLStoreCursor cursor)
		{
			if (_store.UseCursorCache && (_connection.TransactionCount > 0) && !_cursorCache.ContainsKey(cursor.CursorName))
				_cursorCache.Add(cursor.CursorName, cursor);
			else
				cursor.Dispose();
		}
		
		public SQLStoreCursor OpenCursor(string indexName, bool isUpdatable)
		{
			string cursorName = indexName + isUpdatable.ToString();
			SQLStoreCursor cursor;
			if (_store.UseCursorCache && _cursorCache.TryGetValue(cursorName, out cursor))
			{
				_cursorCache.Remove(cursorName);
				cursor.SetRange(null, null);
			}
			else
			{
				StoreIndexHeader indexHeader = Store.GetIndexHeader(indexName);
				StoreTableHeader tableHeader = Store.GetTableHeader(indexHeader.TableName);
				cursor = _connection.OpenCursor(indexHeader.TableName, tableHeader.Columns, indexHeader.SQLIndex, isUpdatable);
			}
			return cursor;
		}
		
		public SQLStoreCursor OpenRangedCursor(string indexName, bool isUpdatable, object[] startValues, object[] endValues)
		{
			SQLStoreCursor cursor = OpenCursor(indexName, isUpdatable);
			try
			{
				cursor.SetRange(startValues, endValues);
				return cursor;
			}
			catch
			{
				cursor.Dispose();
				throw;
			}
		}
		
		public SQLStoreCursor OpenMatchedCursor(string indexName, bool isUpdatable, params object[] matchValues)
		{
			SQLStoreCursor cursor = OpenCursor(indexName, isUpdatable);
			try
			{
				cursor.SetRange(matchValues, matchValues);
				return cursor;
			}
			catch
			{
				cursor.Dispose();
				throw;
			}
		}
		
		public void InsertRow(string tableName, params object[] rowValues)
		{
			if (_store.InsertThroughCursor)
			{
				SQLStoreCursor cursor = OpenCursor(Store.GetTableHeader(tableName).PrimaryKeyName, true);
				try
				{
					cursor.Insert(rowValues);
				}
				finally
				{
					CloseCursor(cursor);
				}
			}
			else
			{
				StoreTableHeader header = Store.GetTableHeader(tableName);
				StoreIndexHeader indexHeader = Store.GetIndexHeader(header.PrimaryKeyName);
				_connection.PerformInsert(tableName, header.Columns, indexHeader.Columns, rowValues);
			}
		}
		
		public void DeleteRows(string indexName, params object[] keyValues)
		{
			if (_store.DeleteThroughCursor)
			{
				SQLStoreCursor cursor = OpenMatchedCursor(indexName, true, keyValues);
				try
				{
					while (cursor.Next())
						cursor.Delete();
				}
				finally
				{
					CloseCursor(cursor);
				}
			}
			else
			{
				StoreIndexHeader indexHeader = Store.GetIndexHeader(indexName);
				StoreTableHeader tableHeader = Store.GetTableHeader(indexHeader.TableName);
				if (indexHeader.IsUnique && (keyValues.Length >= indexHeader.Columns.Count))
				{
					object[] row = new object[tableHeader.Columns.Count];
					for (int index = 0; index < keyValues.Length; index++)
						row[tableHeader.Columns.IndexOf(indexHeader.Columns[index])] = keyValues[index];
					_connection.PerformDelete(indexHeader.TableName, tableHeader.Columns, indexHeader.Columns, row);
				}
				else
				{
					List<object[]> rows = new List<object[]>();
					SQLStoreCursor cursor = OpenMatchedCursor(indexName, true, keyValues);
					try
					{
						while (cursor.Next())
							rows.Add(cursor.Select());
					}
					finally
					{
						CloseCursor(cursor);
					}
					
					for (int index = 0; index < rows.Count; index++)
						_connection.PerformDelete(indexHeader.TableName, tableHeader.Columns, indexHeader.Columns, rows[index]);
				}
			}
		}
		
		#region Platform Independent Internal Logic
		
		private void InsertObjectDependencies(Schema.Object objectValue)
		{
			if (objectValue.HasDependencies())
				for (int index = 0; index < objectValue.Dependencies.Count; index++)
					InsertRow("DAEObjectDependencies", objectValue.ID, objectValue.Dependencies.IDs[index]);
		}
		
		private void InsertObjectAndDependencies(Schema.Object objectValue)
		{
			InsertRow
			(
				"DAEObjects", 
				objectValue.ID, 
				Schema.Object.EnsureNameLength(objectValue.Name), 
				objectValue.Library == null ? String.Empty : objectValue.Library.Name, 
				Schema.Object.EnsureDescriptionLength(objectValue.DisplayName), 
				Schema.Object.EnsureDescriptionLength(objectValue.Description),
				objectValue.GetType().Name,
				objectValue.IsSystem,
				objectValue.IsRemotable,
				objectValue.IsGenerated,
				objectValue.IsATObject,
				objectValue.IsSessionObject,
				objectValue.IsPersistent,
				objectValue.CatalogObjectID,
				objectValue.ParentObjectID,
				objectValue.GeneratorID
			);

			InsertObjectDependencies(objectValue);
		}
		
		private void InsertAllObjectsAndDependencies(Schema.Object objectValue)
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
			
			InsertObjectDependencies(objectValue);
	
			int index;
			int subIndex;
			Schema.ScalarType scalarType = objectValue as Schema.ScalarType;
			if (scalarType != null)
			{
				for (index = 0; index < scalarType.Representations.Count; index++)
				{
					Schema.Representation representation = scalarType.Representations[index];
					if (!representation.IsPersistent)
					{
						InsertObjectAndDependencies(representation);
						for (subIndex = 0; subIndex < representation.Properties.Count; subIndex++)
							InsertObjectAndDependencies(representation.Properties[subIndex]);
					}
				}

				for (index = 0; index < scalarType.Specials.Count; index++)
					if (!scalarType.Specials[index].IsPersistent)
						InsertObjectAndDependencies(scalarType.Specials[index]);
					
				if (scalarType.Default != null)
					if (!scalarType.Default.IsPersistent)
						InsertObjectAndDependencies(scalarType.Default);
				
				for (index = 0; index < scalarType.Constraints.Count; index++)
					if (!scalarType.Constraints[index].IsPersistent)
						InsertObjectAndDependencies(scalarType.Constraints[index]);
					
				return;
			}
			
			{
				Schema.Representation representation = objectValue as Schema.Representation;
				if (representation != null)
				{
					for (index = 0; index < representation.Properties.Count; index++)
						InsertObjectAndDependencies(representation.Properties[index]);
				
					return;
				}
			}
			
			Schema.TableVar tableVar = objectValue as Schema.TableVar;
			if (tableVar != null)
			{
				for (index = 0; index < tableVar.Columns.Count; index++)
				{
					Schema.TableVarColumn column = tableVar.Columns[index];
					InsertObjectAndDependencies(column);

					if (column.Default != null)
						if (!column.Default.IsPersistent)
							InsertObjectAndDependencies(column.Default);
						
					if (column.HasConstraints())
						for (subIndex = 0; subIndex < column.Constraints.Count; subIndex++)
							if (!column.Constraints[subIndex].IsPersistent)
								InsertObjectAndDependencies(column.Constraints[subIndex]);
				}

				for (index = 0; index < tableVar.Keys.Count; index++)
					InsertObjectAndDependencies(tableVar.Keys[index]);
					
				for (index = 0; index < tableVar.Orders.Count; index++)
					InsertObjectAndDependencies(tableVar.Orders[index]);

				if (tableVar.HasConstraints())
					for (index = 0; index < tableVar.Constraints.Count; index++)
						if (!tableVar.Constraints[index].IsGenerated && !tableVar.Constraints[index].IsPersistent) // Generated constraints are maintained internally
							InsertObjectAndDependencies(tableVar.Constraints[index]);
			}
		}
		
		private void DeleteObjectDependencies(Schema.Object objectValue)
		{
			DeleteRows("PK_DAEObjectDependencies", objectValue.ID);
			List<object> objectList = new List<object>();
			SQLStoreCursor objects = OpenMatchedCursor("IDX_DAEObjects_Catalog_Object_ID", false, objectValue.ID);
			try
			{
				while (objects.Next())
					objectList.Add(objects[0]);
			}
			finally
			{
				CloseCursor(objects);
			}
			
			for (int objectIndex = 0; objectIndex < objectList.Count; objectIndex++)
				DeleteRows("PK_DAEObjectDependencies", objectList[objectIndex]);
		}

		#endregion
		
		#region API
		
		public void LoadServerSettings(Server.Engine server)
		{
			SQLStoreCursor cursor = OpenCursor("PK_DAEServerInfo", false);
			try
			{
				if (cursor.Next())
				{
					server.MaxConcurrentProcesses = (int)cursor[3];
					server.ProcessWaitTimeout = TimeSpan.FromMilliseconds((int)cursor[4]);
					server.ProcessTerminationTimeout = TimeSpan.FromMilliseconds((int)cursor[5]);
					server.PlanCacheSize = (int)cursor[6];
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void SaveServerSettings(Server.Engine server)
		{
			SQLStoreCursor cursor = OpenCursor("PK_DAEServerInfo", true);
			try
			{
				if (cursor.Next())
				{
					cursor[3] = server.MaxConcurrentProcesses;
					cursor[4] = (int)server.ProcessWaitTimeout.TotalMilliseconds;
					cursor[5] = (int)server.ProcessTerminationTimeout.TotalMilliseconds;
					cursor[6] = server.PlanCacheSize;
					cursor.Update();
				}
				else
				{
					cursor.Insert
					(
						new object[]
						{
							"ID",
							server.Name,
							GetType().Assembly.GetName().Version.ToString(),
							server.MaxConcurrentProcesses,
							(int)server.ProcessWaitTimeout.TotalMilliseconds,
							(int)server.ProcessTerminationTimeout.TotalMilliseconds,
							server.PlanCacheSize
						}
					);
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public Schema.Right SelectRight(string rightName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAERights", false, rightName);
			try
			{
				if (cursor.Next())
					return new Schema.Right((string)cursor[0], (string)cursor[1], (int)cursor[2]);
					
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void InsertRight(string rightName, string userID)
		{
			InsertRow("DAERights", rightName, userID, -1);
		}
		
		public void UpdateRight(string rightName, string userID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAERights", true, rightName);
			try
			{
				if (cursor.Next())
				{
					cursor[1] = userID;
					cursor.Update();
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void DeleteRight(string rightName)
		{
			// Delete role right assignments for the right
			DeleteRows("IDX_DAERoleRightAssignments_Right_Name", rightName);
			
			// Delete user right assignments for the right
			DeleteRows("IDX_DAEUserRightAssignments_Right_Name", rightName);
			
			// Delete the right
			DeleteRows("PK_DAERights", rightName);
		}
		
		public void InsertRole(Schema.Role role, string objectScript)
		{
			InsertPersistentObject(role, objectScript);
		}
		
		public void DeleteRole(Schema.Role role)
		{
			// Delete the DAERoleRightAssignments rows for the rights assigned to the role
			DeleteRows("PK_DAERoleRightAssignments", role.ID);

			// Delete the DAEUserRoles rows for the role
			DeleteRows("IDX_DAEUserRoles_Role_ID", role.ID);
			
			// Delete the role
			DeletePersistentObject(role);
		}
		
		public List<string> SelectRoleUsers(int roleID)
		{
			List<string> users = new List<string>();
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEUserRoles_Role_ID", false, roleID);
			try
			{
				while (cursor.Next())
					users.Add((string)cursor[0]);
					
				return users;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public Schema.RightAssignment SelectRoleRightAssignment(int roleID, string rightName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAERoleRightAssignments", false, roleID, rightName);
			try
			{
				if (cursor.Next())
					return new Schema.RightAssignment((string)cursor[1], (bool)cursor[2]);
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void EnsureRoleRightAssignment(int roleID, string rightName, bool granted)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAERoleRightAssignments", true, roleID, rightName);
			try
			{
				if (cursor.Next())
				{
					cursor[2] = granted;
					cursor.Update();
				}
				else
					cursor.Insert(new object[] { roleID, rightName, granted });
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void DeleteRoleRightAssignment(int roleID, string rightName)
		{
			DeleteRows("PK_DAERoleRightAssignments", roleID, rightName);
		}
		
		public void EnsureUserRightAssignment(string userID, string rightName, bool granted)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEUserRightAssignments", true, userID, rightName);
			try
			{
				if (cursor.Next())
				{
					cursor[2] = granted;
					cursor.Update();
				}
				else
					cursor.Insert(new object[] { userID, rightName, granted });
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void DeleteUserRightAssignment(string userID, string rightName)
		{
			DeleteRows("PK_DAEUserRightAssignments", userID, rightName);
		}
		
		public Schema.User SelectUser(string userID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEUsers", false, userID);
			try
			{
				if (cursor.Next())
					return new Schema.User((string)cursor[0], (string)cursor[1], (string)cursor[2]);
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void InsertUser(Schema.User user)
		{
			InsertRow("DAEUsers", user.ID, user.Name, user.Password);
		}
		
		public void UpdateUser(Schema.User user)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEUsers", true, user.ID);
			try
			{
				if (cursor.Next())
				{
					cursor[1] = user.Name;
					cursor[2] = user.Password;
					cursor.Update();
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void DeleteUser(string userID)
		{
			// Delete DAEDeviceUsers for the user
			DeleteRows("PK_DAEDeviceUsers", userID);
			
			// Delete DAEUserRightAssginments for the user
			DeleteRows("PK_DAEUserRightAssignments", userID);
			
			// Delete DAEUserRoles for the user
			DeleteRows("PK_DAEUserRoles", userID);

			// Delete DAEUsers for the user
			DeleteRows("PK_DAEUsers", userID);
		}
		
		public bool UserOwnsObjects(string userID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAECatalogObjects_Owner_User_ID", false, userID);
			try
			{
				return cursor.Next();
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public bool UserOwnsRights(string userID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAERights_Owner_User_ID", false, userID);
			try
			{
				return cursor.Next();
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		/// <summary>Returns a list of headers for each operator owned by the given user.</summary>
		public Schema.CatalogObjectHeaders SelectUserOperators(string userID)
		{
			Schema.CatalogObjectHeaders headers = new Schema.CatalogObjectHeaders();
			List<object[]> objectList = new List<object[]>();
			SQLStoreCursor catalogObjects = OpenMatchedCursor("IDX_DAECatalogObjects_Owner_User_ID", false, userID);
			try
			{
				while (catalogObjects.Next())
					objectList.Add(new object[] { catalogObjects[0], catalogObjects[1], catalogObjects[2], catalogObjects[3] });
			}
			finally
			{
				CloseCursor(catalogObjects);
			}

			SQLStoreCursor operators = OpenCursor("PK_DAEOperators", false);
			try
			{
				for (int index = 0; index < objectList.Count; index++)
					if (operators.FindKey(new object[] { objectList[index][0] }))
						headers.Add(new Schema.CatalogObjectHeader((int)objectList[index][0], (string)objectList[index][1], (string)objectList[index][2], (string)objectList[index][3]));

				return headers;
			}
			finally
			{
				CloseCursor(operators);
			}
		}

		public void InsertUserRole(string userID, int roleID)
		{
			InsertRow("DAEUserRoles", userID, roleID); 
		}
		
		public void DeleteUserRole(string userID, int roleID)
		{
			DeleteRows("PK_DAEUserRoles", userID, roleID);
		}
		
		public bool UserHasRight(string userID, string rightName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAERights", false, rightName);
			try
			{
				// If the right does not exist, it is implicitly granted.
				// This behavior ensures that rights for objects created only in the internal cache will always be granted.
				// Checking the rights for objects such as internal constraint check tables and, in certain configurations, 
				// A/T and session tables will never result in a security exception.
				if (!cursor.Next())
					return true;
					
				if (String.Compare((string)cursor[1], userID, true) == 0)
					return true;
			}
			finally
			{
				CloseCursor(cursor);
			}
			
			cursor = OpenMatchedCursor("PK_DAEUserRightAssignments", false, userID, rightName);
			try
			{
				if (cursor.Next())
					return (bool)cursor[2];
			}
			finally
			{
				CloseCursor(cursor);
			}
			
			bool granted = false;
			List<object> roleNames = new List<object>();
			SQLStoreCursor roles = OpenMatchedCursor("PK_DAEUserRoles", false, userID);
			try
			{
				while (roles.Next())
					roleNames.Add(roles[1]);
			}
			finally
			{
				CloseCursor(roles);
			}

			for (int index = 0; index < roleNames.Count; index++)
			{
				SQLStoreCursor roleRights = OpenMatchedCursor("PK_DAERoleRightAssignments", false, roleNames[index], rightName);
				try
				{
					if (roleRights.Next())
					{
						granted = (bool)roleRights[2];
						if (!granted)
							return granted;
					}
				}
				finally
				{
					CloseCursor(roleRights);
				}
			}

			return granted;
		}
		
		public Schema.DeviceUser SelectDeviceUser(Schema.Device device, Schema.User user)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEDeviceUsers", false, user.ID, device.ID);
			try
			{
				if (cursor.Next())
					return new Schema.DeviceUser(user, device, (string)cursor[2], (string)cursor[3], (string)cursor[4]);
					
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void InsertDeviceUser(Schema.DeviceUser deviceUser)
		{
			InsertRow("DAEDeviceUsers", deviceUser.User.ID, deviceUser.Device.ID, deviceUser.DeviceUserID, deviceUser.DevicePassword, deviceUser.ConnectionParameters);
		}
		
		public void UpdateDeviceUser(Schema.DeviceUser deviceUser)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEDeviceUsers", true, deviceUser.User.ID, deviceUser.Device.ID);
			try
			{
				if (cursor.Next())
				{
					cursor[2] = deviceUser.DeviceUserID;
					cursor[3] = deviceUser.DevicePassword;
					cursor[4] = deviceUser.ConnectionParameters;
					cursor.Update();
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void DeleteDeviceUser(Schema.DeviceUser deviceUser)
		{
			DeleteRows("PK_DAEDeviceUsers", deviceUser.User.ID, deviceUser.Device.ID);
		}
		
		/// <summary>Returns true if there are any device objects registered for the given device, false otherwise.</summary>
		public bool HasDeviceObjects(int deviceID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID", false, deviceID);
			try
			{
				return cursor.Next();
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		/// <summary>Returns the ID of the device object map for the device with ID ADeviceID and object AObjectID, if it exists, -1 otherwise.</summary>
		public int SelectDeviceObjectID(int deviceID, int objectID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID", false, deviceID, objectID);
			try
			{
				if (cursor.Next())
					return (int)cursor[0];
				return -1;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		/// <summary>Returns true if there are no catalog objects in the catalog. This will only be true on a first-time startup.</summary>
		public bool IsEmpty()
		{
			SQLStoreCursor cursor = OpenCursor("PK_DAECatalogObjects", false);
			try
			{
				return !cursor.Next();
			}
			finally
			{
				CloseCursor(cursor);
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
			List<object[]> objectList = new List<object[]>();
			SQLStoreCursor objects = OpenCursor("PK_DAECatalogObjects", false);
			try
			{
				while (objects.Next())
					objectList.Add(new object[] { objects[0] });
			}
			finally
			{
				CloseCursor(objects);
			}
			
			if (_store.InsertThroughCursor)
			{
				SQLStoreCursor baseObjects = OpenCursor("PK_DAEBaseCatalogObjects", true);
				try
				{
					for (int index = 0; index < objectList.Count; index++)
						baseObjects.Insert(objectList[index]);
				}
				finally
				{
					CloseCursor(baseObjects);
				}
			}
			else
			{
				for (int index = 0; index < objectList.Count; index++)
					InsertRow("DAEBaseCatalogObjects", objectList[index]);
			}
		}
		
		/// <summary>Returns a list of catalog headers for the base system objects.</summary>
		public Schema.CatalogObjectHeaders SelectBaseCatalogObjects()
		{
			List<object[]> objectList = new List<object[]>();
			SQLStoreCursor baseObjects = OpenCursor("PK_DAEBaseCatalogObjects", false);
			try
			{
				while (baseObjects.Next())
					objectList.Add(new object[] { baseObjects[0] });
			}
			finally
			{
				CloseCursor(baseObjects);
			}
					
			SQLStoreCursor objects = OpenCursor("PK_DAECatalogObjects", false);
			try
			{
				Schema.CatalogObjectHeaders headers = new Schema.CatalogObjectHeaders();

				for (int index = 0; index < objectList.Count; index++)
					if (objects.FindKey(objectList[index]))
						headers.Add(new Schema.CatalogObjectHeader((int)objects[0], (string)objects[1], (string)objects[2], (string)objects[3]));
						
				return headers;
			}
			finally
			{
				CloseCursor(objects);
			}
		}

		public Schema.CatalogObjectHeaders SelectLibraryCatalogObjects(string libraryName)
		{
			SQLStoreCursor objects = OpenMatchedCursor("IDX_DAECatalogObjects_Library_Name", false, libraryName);
			try
			{
				Schema.CatalogObjectHeaders headers = new Schema.CatalogObjectHeaders();
				
				while (objects.Next())
					headers.Add(new Schema.CatalogObjectHeader((int)objects[0], (string)objects[1], (string)objects[2], (string)objects[3]));
					
				return headers;
			}
			finally
			{
				CloseCursor(objects);
			}
		}

		public Schema.CatalogObjectHeaders SelectGeneratedObjects(int objectID)
		{
			List<object[]> objectList = new List<object[]>();
			
			SQLStoreCursor objects = OpenMatchedCursor("IDX_DAEObjects_Generator_Object_ID", false, objectID);
			try
			{
				while (objects.Next())
					objectList.Add(new object[] { objects[0] });
			}
			finally
			{
				CloseCursor(objects);
			}
			
			SQLStoreCursor catalogObjects = OpenCursor("PK_DAECatalogObjects", false);
			try
			{
				Schema.CatalogObjectHeaders headers = new Schema.CatalogObjectHeaders();
					
				for (int index = 0; index < objectList.Count; index++)
					if (catalogObjects.FindKey(objectList[index]))
						headers.Add(new Schema.CatalogObjectHeader((int)catalogObjects[0], (string)catalogObjects[1], (string)catalogObjects[2], (string)catalogObjects[3]));
							
				return headers;
			}
			finally
			{
				CloseCursor(catalogObjects);
			}
		}

		public int GetMaxObjectID()
		{
			SQLStoreCursor cursor = OpenCursor("PK_DAEObjects", false);
			try
			{
				cursor.Last();
				if (cursor.Prior())
					return (int)cursor[0];
				return 0;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public List<string> SelectLoadedLibraries()
		{
			List<string> libraryNames = new List<string>();
			SQLStoreCursor cursor = OpenCursor("PK_DAELoadedLibraries", false);
			try
			{
				while (cursor.Next())
					libraryNames.Add((string)cursor[0]);
			}
			finally
			{
				CloseCursor(cursor);
			}
			return libraryNames;
		}

		public bool LoadedLibraryExists(string libraryName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAELoadedLibraries", false, libraryName);
			try
			{
				return cursor.Next();
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void InsertLoadedLibrary(string libraryName)
		{
			InsertRow("DAELoadedLibraries", libraryName);
		}

		public void DeleteLoadedLibrary(string libraryName)
		{
			DeleteRows("PK_DAELoadedLibraries", libraryName);
		}

		public SQLStoreCursor SelectLibraryDirectories()
		{
			return OpenCursor("PK_DAELibraryDirectories", false);
		}

		public void SetLibraryDirectory(string libraryName, string libraryDirectory)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAELibraryDirectories", true, libraryName);
			try
			{
				if (cursor.Next())
				{
					cursor[1] = libraryDirectory;
					cursor.Update();
				}
				else
					cursor.Insert(new object[] { libraryName, libraryDirectory });
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void DeleteLibraryDirectory(string libraryName)
		{
			DeleteRows("PK_DAELibraryDirectories", libraryName);
		}

		public SQLStoreCursor SelectLibraryOwners()
		{
			return OpenCursor("PK_DAELibraryOwners", false);
		}

		public string SelectLibraryOwner(string libraryName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAELibraryOwners", false, libraryName);
			try
			{
				if (cursor.Next())
					return (string)cursor[1];
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void InsertLibraryOwner(string libraryName, string userID)
		{
			InsertRow("DAELibraryOwners", libraryName, userID);
		}

		public void UpdateLibraryOwner(string libraryName, string userID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAELibraryOwners", true, libraryName);
			try
			{
				if (cursor.Next())
				{
					cursor[1] = userID;
					cursor.Update();
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void DeleteLibraryOwner(string libraryName)
		{
			DeleteRows("PK_DAELibraryOwners", libraryName);
		}

		public SQLStoreCursor SelectLibraryVersions()
		{
			return OpenCursor("PK_DAELibraryVersions", false);
		}

		public string SelectLibraryVersion(string libraryName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAELibraryVersions", false, libraryName);
			try
			{
				if (cursor.Next())
					return (string)cursor[1];
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void InsertLibraryVersion(string libraryName, VersionNumber version)
		{
			InsertRow("DAELibraryVersions", libraryName, version.ToString());
		}
		
		public void UpdateLibraryVersion(string libraryName, VersionNumber version)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAELibraryVersions", true, libraryName);
			try
			{
				if (cursor.Next())
				{
					cursor[1] = version.ToString();
					cursor.Update();
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void DeleteLibraryVersion(string libraryName)
		{
			DeleteRows("PK_DAELibraryVersions", libraryName);
		}

		public string SelectClassLibrary(string className)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEClasses", false, className);
			try
			{
				if (cursor.Next())
					return (string)cursor[1];
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public void InsertRegisteredClasses(string libraryName, SettingsList registeredClasses)
		{
			foreach (SettingsItem registeredClass in registeredClasses.Values)
				InsertRow("DAEClasses", registeredClass.Name, libraryName);
		}
		
		public void DeleteRegisteredClasses(string libraryName, SettingsList registeredClasses)
		{
			foreach (SettingsItem registeredClass in registeredClasses.Values)
				DeleteRows("PK_DAEClasses", registeredClass.Name);
		}

		/// <summary>Returns true if an object of the given name is already present in the database.</summary>
		public bool CatalogObjectExists(string objectName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("UIDX_DAECatalogObjects_Name", false, objectName);
			try
			{
				return cursor.Next();
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
        private int GetMaxCatalogObjectNamesIndexDepth()
        {
            var maxDepth = Store.GetMaxNameIndexDepth();
            if (maxDepth < 0)
            {
                maxDepth = InternalGetMaxCatalogObjectNamesIndexDepth();
                Store.SetMaxNameIndexDepth(maxDepth);
            }
            return maxDepth;
        }

		private int InternalGetMaxCatalogObjectNamesIndexDepth()
		{
			SQLStoreCursor cursor = OpenCursor("PK_DAECatalogObjectNames", false);
			try
			{
				int max = 0; 
				cursor.Last();
				if (cursor.Prior())
					max = (int)cursor[0];
				return max;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		/// <summary>Returns a list of CatalogObjectHeaders that matched the given name.</summary>		
		public Schema.CatalogObjectHeaders ResolveCatalogObjectName(string name)
		{
			Schema.CatalogObjectHeaders headers = new Schema.CatalogObjectHeaders();
			if (Schema.Object.IsRooted(name))
			{
				name = Schema.Object.EnsureUnrooted(name);
				SQLStoreCursor cursor = OpenMatchedCursor("UIDX_DAECatalogObjects_Name", false, name);
				try
				{
					#if REQUIRECASEMATCHONRESOLVE
					if (cursor.Next() && (name == (string)cursor[1]))
					#else
					if (cursor.Next())
					#endif
						headers.Add(new Schema.CatalogObjectHeader((int)cursor[0], (string)cursor[1], (string)cursor[2], (string)cursor[3]));
						
					return headers;
				}
				finally
				{
					CloseCursor(cursor);
				}
			}
			
			for (int index = GetMaxCatalogObjectNamesIndexDepth() - Schema.Object.GetQualifierCount(name); index >= 0; index--)
			{
				List<object[]> names = new List<object[]>();

				SQLStoreCursor cursor = OpenMatchedCursor("PK_DAECatalogObjectNames", false, index, name);
				try
				{
					while (cursor.Next())
						#if REQUIRECASEMATCHONRESOLVE
						names.Add(new object[] { cursor[1], cursor[2] });
						#else
						names.Add(new object[] { cursor[2] });
						#endif
				}
				finally
				{
					CloseCursor(cursor);
				}
				
				if (names.Count > 0)
				{
					SQLStoreCursor objects = OpenCursor("PK_DAECatalogObjects", false);
					try
					{
						for (int nameIndex = 0; nameIndex < names.Count; nameIndex++)
							#if REQUIRECASEMATCHONRESOLVE
							if ((name == (string)names[nameIndex][0]) && objects.FindKey(new object[] { names[nameIndex][1] }))
							#else
							if (objects.FindKey(names[nameIndex]))
							#endif
								headers.Add(new Schema.CatalogObjectHeader((int)objects[0], (string)objects[1], (string)objects[2], (string)objects[3]));
					}
					finally
					{
						CloseCursor(objects);
					}
				}
			}
			
			return headers;
		}
		
		private int GetMaxOperatorNameNamesIndexDepth()
		{
			SQLStoreCursor cursor = OpenCursor("PK_DAEOperatorNameNames", false);
			try
			{
				int max = 0; // TODO: Could cache this... would only ever be changed by adding or deleting catalog objects
				cursor.Last();
				if (cursor.Prior())
					max = (int)cursor[0];
				return max;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		/// <summary>Returns a list of CatalogObjectHeaders for operators whose operator name matched the given name.</summary>
		public Schema.CatalogObjectHeaders ResolveOperatorName(string name)
		{
			Schema.CatalogObjectHeaders headers = new Schema.CatalogObjectHeaders();
			if (Schema.Object.IsRooted(name))
			{
				name = Schema.Object.EnsureUnrooted(name);
				List<object[]> nameList = new List<object[]>();
				
				SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEOperators_OperatorName", false, name);
				try
				{
					while (cursor.Next())
						#if REQUIRECASEMATCHONRESOLVE
						nameList.Add(new object[] { cursor[1], cursor[0] });
						#else
						nameList.Add(new object[] { cursor[0] });
						#endif
				}
				finally
				{
					CloseCursor(cursor);
				}
				
				if (nameList.Count > 0)
				{
					SQLStoreCursor objects = OpenCursor("PK_DAECatalogObjects", false);
					try
					{
						for (int nameIndex = 0; nameIndex < nameList.Count; nameIndex++)
							#if REQUIRECASEMATCHONRESOLVE
							if ((name == (string)nameList[nameIndex][0]) && objects.FindKey(new object[] { nameList[nameIndex][1] }))
							#else
							if (objects.FindKey(nameList[nameIndex]))
							#endif
								headers.Add(new Schema.CatalogObjectHeader((int)objects[0], (string)objects[1], (string)objects[2], (string)objects[3]));
					}
					finally
					{
						CloseCursor(objects);
					}
				}
					
				return headers;
			}
			
			for (int index = GetMaxOperatorNameNamesIndexDepth() - Schema.Object.GetQualifierCount(name); index >= 0; index--)
			{
				List<object[]> operatorNameList = new List<object[]>();
				SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEOperatorNameNames", false, index, name);
				try
				{
					while (cursor.Next())
						operatorNameList.Add(new object[] { cursor[1], cursor[2] });
				}
				finally
				{
					CloseCursor(cursor);
				}
				
				List<object> operatorList = new List<object>();
				for (int operatorNameIndex = 0; operatorNameIndex < operatorNameList.Count; operatorNameIndex++)
				{
					#if REQUIRECASEMATCHONRESOLVE
					if (name == (string)operatorNameList[operatorNameIndex][0])
					{
					#endif
						SQLStoreCursor operators = OpenMatchedCursor("IDX_DAEOperators_OperatorName", false, operatorNameList[operatorNameIndex][1]);
						try
						{
							while (operators.Next())
								operatorList.Add(operators[0]);
						}
						finally
						{
							CloseCursor(operators);
						}
					#if REQUIRECASEMATCHONRESOLVE
					}
					#endif
				}
				
				if (operatorList.Count > 0)
				{
					SQLStoreCursor objects = OpenCursor("PK_DAECatalogObjects", false);
					try
					{
						for (int operatorIndex = 0; operatorIndex < operatorList.Count; operatorIndex++)
							if (objects.FindKey(new object[] { operatorList[operatorIndex] }))
								headers.Add(new Schema.CatalogObjectHeader((int)objects[0], (string)objects[1], (string)objects[2], (string)objects[3]));
					}
					finally
					{
						CloseCursor(objects);
					}
				}
			}
			
			return headers;
		}
		
		public string SelectOperatorName(int objectID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEOperators", false, objectID);
			try
			{
				if (cursor.Next())
					return (string)cursor[1];

				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public Schema.PersistentObjectHeader SelectPersistentObject(int objectID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEObjects", false, objectID);
			try
			{
				if (cursor.Next())
					return
						new Schema.PersistentObjectHeader
						(
							(int)cursor[0], // ID
							(string)cursor[1], // Name
							(string)cursor[2], // Library_Name
							(string)cursor[15], // Script
							(string)cursor[3], // DisplayName
							(string)cursor[5], // ObjectType
							(bool)cursor[6], // IsSystem
							(bool)cursor[7], // IsRemotable
							(bool)cursor[8], // IsGenerated
							(bool)cursor[9], // IsATObject
							(bool)cursor[10] // IsSessionObject
						);
						
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		public Schema.FullCatalogObjectHeader SelectCatalogObject(int objectID)
		{
			object[] objectValue = null;
			SQLStoreCursor objects = OpenMatchedCursor("PK_DAEObjects", false, objectID);
			try
			{
				if (objects.Next())
					objectValue = objects.Select();
			}
			finally
			{
				CloseCursor(objects);
			}
			
			if (objectValue != null)
			{
				SQLStoreCursor catalogObjects = OpenCursor("PK_DAECatalogObjects", false);
				try
				{
					if (catalogObjects.FindKey(new object[] { objectValue[0] }))
						return
							new Schema.FullCatalogObjectHeader
							(
								(int)objectValue[0], // ID
								(string)objectValue[1], // Name
								(string)objectValue[2], // LibraryName
								(string)catalogObjects[3], // OwnerID
								(string)objectValue[15], // Script
								(string)objectValue[3], // DisplayName
								(string)objectValue[5], // ObjectType
								(bool)objectValue[6], // IsSystem
								(bool)objectValue[7], // IsRemotable
								(bool)objectValue[8], // IsGenerated
								(bool)objectValue[9], // IsATObject
								(bool)objectValue[10], // IsSessionObject
								(int)objectValue[14] // GeneratorObjectID
							);
				}
				finally
				{
					CloseCursor(catalogObjects);
				}
			}

			return null;
		}
		
		public void InsertPersistentObject(Schema.Object objectValue, string objectScript)
		{
			// Insert the persistent objects row
			InsertRow
			(
				"DAEObjects", 
				objectValue.ID, 
				objectValue.Name, 
				objectValue.Library == null ? String.Empty : objectValue.Library.Name,
				Schema.Object.EnsureDescriptionLength(objectValue.DisplayName),
				Schema.Object.EnsureDescriptionLength(objectValue.Description),
				objectValue.GetType().Name,
				objectValue.IsSystem,
				objectValue.IsRemotable,
				objectValue.IsGenerated,
				objectValue.IsATObject,
				objectValue.IsSessionObject,
				objectValue.IsPersistent,
				objectValue.CatalogObjectID,
				objectValue.ParentObjectID,
				objectValue.GeneratorID,
				objectScript
			);
			
			Schema.CatalogObject catalogObject = objectValue as Schema.CatalogObject;
			if (catalogObject != null)
			{
				// Insert the DAECatalogObjects row
				InsertRow("DAECatalogObjects", catalogObject.ID, catalogObject.Name, catalogObject.Library == null ? String.Empty : catalogObject.Library.Name, catalogObject.Owner.ID);
				
				// Insert the DAECatalogObjectNames rows
				SQLStoreCursor catalogObjectNames = OpenCursor("PK_DAECatalogObjectNames", true);
				try
				{
					string name = catalogObject.Name;
					int depth = Schema.Object.GetQualifierCount(name);
					for (int index = 0; index <= depth; index++)
					{
						catalogObjectNames.Insert(new object[] { index, name, catalogObject.ID });
						name = Schema.Object.Dequalify(name);
					}
				
					// Set if depth of clearing name is greater than cached value
                    if (Store.GetMaxNameIndexDepth() < depth)
                    {
                        Store.ClearMaxNameIndexDepth();
                    }
				}
				finally
				{
					CloseCursor(catalogObjectNames);
				}
				
				Schema.ScalarType scalarType = objectValue as Schema.ScalarType;
				if (scalarType != null)
				{
					// Insert the DAEScalarTypes row
					InsertRow("DAEScalarTypes", new object[] { scalarType.ID, scalarType.UniqueSortID, scalarType.SortID });
				}
				
				Schema.Operator operatorValue = objectValue as Schema.Operator;
				if (operatorValue != null)			
				{
					// Ensure the DAEOperatorNames and DAEOperatorNameNames rows exist
					bool didInsert = false;
					SQLStoreCursor operatorNames = OpenMatchedCursor("PK_DAEOperatorNames", true, operatorValue.OperatorName);
					try
					{
						if (!operatorNames.Next())
						{
							operatorNames.Insert(new object[] { operatorValue.OperatorName });
							didInsert = true;
						}
					}
					finally
					{
						CloseCursor(operatorNames);
					}
					
					if (didInsert)
					{
						SQLStoreCursor operatorNameNames = OpenCursor("PK_DAEOperatorNameNames", true);
						try
						{
							string name = operatorValue.OperatorName;
							int depth = Schema.Object.GetQualifierCount(name);
							for (int index = 0; index <= depth; index++)
							{
								operatorNameNames.Insert(new object[] { index, name, operatorValue.OperatorName });
								name = Schema.Object.Dequalify(name);
							}
						}
						finally
						{
							CloseCursor(operatorNameNames);
						}
					}
					
					// Insert the DAEOperators row
					InsertRow("DAEOperators", operatorValue.ID, operatorValue.OperatorName, operatorValue.Signature.ToString(), operatorValue.Locator.Locator, operatorValue.Locator.Line, operatorValue.Locator.LinePos);
				}
				
				if (objectValue is Schema.EventHandler)
				{
					Schema.ScalarTypeEventHandler scalarTypeEventHandler = objectValue as Schema.ScalarTypeEventHandler;
					if (scalarTypeEventHandler != null)
						InsertRow("DAEEventHandlers", scalarTypeEventHandler.ID, scalarTypeEventHandler.Operator.ID, scalarTypeEventHandler.ScalarType.ID);
					
					Schema.TableVarEventHandler tableVarEventHandler = objectValue as Schema.TableVarEventHandler;
					if (tableVarEventHandler != null)
						InsertRow("DAEEventHandlers", tableVarEventHandler.ID, tableVarEventHandler.Operator.ID, tableVarEventHandler.TableVar.ID);
						
					Schema.TableVarColumnEventHandler tableVarColumnEventHandler = objectValue as Schema.TableVarColumnEventHandler;
					if (tableVarColumnEventHandler != null)
						InsertRow("DAEEventHandlers", tableVarColumnEventHandler.ID, tableVarColumnEventHandler.Operator.ID, tableVarColumnEventHandler.TableVarColumn.ID);
				}
				
				// Insert the DAEDevices row
				Schema.Device device = objectValue as Schema.Device;
				if (device != null)
					InsertRow("DAEDevices", device.ID, device.ReconcileMaster.ToString(), device.ReconcileMode.ToString());
				
				Schema.DeviceScalarType deviceScalarType = objectValue as Schema.DeviceScalarType;
				if (deviceScalarType != null)
					InsertRow("DAEDeviceObjects", deviceScalarType.ID, deviceScalarType.Device.ID, deviceScalarType.ScalarType.ID);
				
				Schema.DeviceOperator deviceOperator = objectValue as Schema.DeviceOperator;
				if (deviceOperator != null)
					InsertRow("DAEDeviceObjects", deviceOperator.ID, deviceOperator.Device.ID, deviceOperator.Operator.ID);
				
				// Insert the DAERights rows
				string[] rights = catalogObject.GetRights();
				for (int index = 0; index < rights.Length; index++)
					InsertRow("DAERights", rights[index], catalogObject.Owner.ID, catalogObject.ID);
			}
			
			InsertAllObjectsAndDependencies(objectValue);
		}
		
		public void UpdatePersistentObjectData(Schema.Object objectValue, string objectScript)
		{
			SQLStoreCursor objects = OpenMatchedCursor("PK_DAEObjects", true, objectValue.ID);
			try
			{
				if (objects.Next())
				{
					objects[15] = objectScript;
					objects.Update();
				}
			}
			finally
			{
				CloseCursor(objects);
			}
		}
		
		public void UpdatePersistentObject(Schema.Object objectValue, string objectScript)
		{
			// Update the DAEDevices row
			Schema.Device device = objectValue as Schema.Device;
			if (device != null)
			{
				SQLStoreCursor devices = OpenMatchedCursor("PK_DAEDevices", true, device.ID);
				try
				{
					if (devices.Next())
					{
						devices[1] = device.ReconcileMaster.ToString();
						devices[2] = device.ReconcileMode.ToString();
						devices.Update();
					}
				}
				finally
				{
					CloseCursor(devices);
				}
			}
				
			// Update the DAEScalarTypes row
			Schema.ScalarType scalarType = objectValue as Schema.ScalarType;
			if (scalarType != null)
			{
				SQLStoreCursor scalarTypes = OpenMatchedCursor("PK_DAEScalarTypes", true, scalarType.ID);
				try
				{
					if (scalarTypes.Next())
					{
						scalarTypes[1] = scalarType.UniqueSortID;
						scalarTypes[2] = scalarType.SortID;
						scalarTypes.Update();
					}
				}
				finally
				{
					CloseCursor(scalarTypes);
				}
			}

			// Delete the DAEObjectDependencies rows
			DeleteObjectDependencies(objectValue);
		
			// Delete the DAEObjects rows
			DeleteRows("PK_DAEObjects", objectValue.ID);
			DeleteRows("IDX_DAEObjects_Catalog_Object_ID", objectValue.ID);

			// Insert the DAEObjects row for the main object
			InsertRow
			(
				"DAEObjects", 
				objectValue.ID, 
				objectValue.Name, 
				objectValue.Library == null ? String.Empty : objectValue.Library.Name,
				Schema.Object.EnsureDescriptionLength(objectValue.DisplayName),
				Schema.Object.EnsureDescriptionLength(objectValue.Description),
				objectValue.GetType().Name,
				objectValue.IsSystem,
				objectValue.IsRemotable,
				objectValue.IsGenerated,
				objectValue.IsATObject,
				objectValue.IsSessionObject,
				objectValue.IsPersistent,
				objectValue.CatalogObjectID,
				objectValue.ParentObjectID,
				objectValue.GeneratorID,
				objectScript
			);
			
			// Insert the DAEObjects rows
			// Insert the DAEOObjectDependencies rows
			InsertAllObjectsAndDependencies(objectValue);
		}

		public void DeletePersistentObject(Schema.Object objectValue)
		{
			// Delete the DAEDeviceUsers rows
			if (objectValue is Schema.Device)
				DeleteRows("IDX_DAEDeviceUsers_Device_ID", objectValue.ID);
				
			// Delete the DAERoleRightAssignments rows
			// Delete the DAEUserRightAssignments rows
			List<object> rightList = new List<object>();
			SQLStoreCursor rights = OpenMatchedCursor("IDX_DAERights_Catalog_Object_ID", false, objectValue.ID);
			try
			{
				while (rights.Next())
					rightList.Add(rights[0]);
			}
			finally
			{
				CloseCursor(rights);
			}
			
			for (int index = 0; index < rightList.Count; index++)
			{
				DeleteRows("IDX_DAERoleRightAssignments_Right_Name", rightList[index]);
				DeleteRows("IDX_DAEUserRightAssignments_Right_Name", rightList[index]);
			}
			
			// Delete the DAERights rows
			DeleteRows("IDX_DAERights_Catalog_Object_ID", objectValue.ID);
			
			// Delete the DAEObjectDependencies rows
			DeleteObjectDependencies(objectValue);
			
			// Delete the DAEObjects rows
			DeleteRows("PK_DAEObjects", objectValue.ID);
			DeleteRows("IDX_DAEObjects_Catalog_Object_ID", objectValue.ID);
			
			// Delete the DAEScalarTypes row
			if (objectValue is Schema.ScalarType)
				DeleteRows("PK_DAEScalarTypes", objectValue.ID);
			
			// Delete the DAEOperators row
			if (objectValue is Schema.Operator)
				DeleteRows("PK_DAEOperators", objectValue.ID);
				
			// Delete the DAEEventHandlers row
			if (objectValue is Schema.EventHandler)
				DeleteRows("PK_DAEEventHandlers", objectValue.ID);
				
			// Delete the DAEDevices row
			if (objectValue is Schema.Device)
				DeleteRows("PK_DAEDevices", objectValue.ID);
				
			// Delete the DAEDeviceObjects rows
			if (objectValue is Schema.DeviceObject)
				DeleteRows("PK_DAEDeviceObjects", objectValue.ID);
			
			if (objectValue is Schema.CatalogObject)
			{
				// Maintain the max names depth cache
				var depth = Schema.Object.GetQualifierCount(objectValue.Name);
				if (depth >= Store.GetMaxNameIndexDepth())
					Store.ClearMaxNameIndexDepth();
				
				// Delete the DAECatalogObjectNames rows
				DeleteRows("IDX_DAECatalogObjectNames_ID", objectValue.ID);
			
				// Delete the DAECatalogObjects row
				DeleteRows("PK_DAECatalogObjects", objectValue.ID);
			}
		}
		
		public void SetCatalogObjectOwner(int catalogObjectID, string userID)
		{
			SQLStoreCursor catalogObjects = OpenMatchedCursor("PK_DAECatalogObjects", true, catalogObjectID);
			try
			{
				if (catalogObjects.Next())
				{
					catalogObjects[3] = userID;
					catalogObjects.Update();
				}
			}
			finally
			{
				CloseCursor(catalogObjects);
			}
			
			// TODO: UpdateThroughCursor property?
			SQLStoreCursor rights = OpenMatchedCursor("IDX_DAERights_Catalog_Object_ID", true, catalogObjectID);
			try
			{
				while (rights.Next())
				{
					rights[1] = userID;
					rights.Update();
				}
			}
			finally
			{
				CloseCursor(rights);
			}
		}
		
		public Schema.ObjectHeader SelectObject(int objectID)
		{
			SQLStoreCursor objects = OpenMatchedCursor("PK_DAEObjects", false, objectID);
			try
			{
				if (objects.Next())
					return
						new Schema.ObjectHeader
						(
							(int)objects[0], // ID
							(string)objects[1], // Name
							(string)objects[2], // LibraryName,
							(string)objects[3], // DisplayName,
							(string)objects[5], // Type,
							(bool)objects[6], // IsSystem,
							(bool)objects[7], // IsRemotable,
							(bool)objects[8], // IsGenerated,
							(bool)objects[9], // IsATObject,
							(bool)objects[10], // IsSessionObject,
							(bool)objects[11], // IsPersistent,
							(int)objects[12], // CatalogObjectID,
							(int)objects[13], // ParentObjectID
							(int)objects[14] // GeneratorObjectID
						);
						
				return null;
			}
			finally
			{
				CloseCursor(objects);
			}
		}
		
		public Schema.FullObjectHeader SelectFullObject(int objectID)
		{
			SQLStoreCursor objects = OpenMatchedCursor("PK_DAEObjects", false, objectID);
			try
			{
				if (objects.Next())
					return
						new Schema.FullObjectHeader
						(
							(int)objects[0], // ID
							(string)objects[1], // Name
							(string)objects[2], // LibraryName,
							(string)objects[15], // Script,
							(string)objects[3], // DisplayName,
							(string)objects[5], // Type,
							(bool)objects[6], // IsSystem,
							(bool)objects[7], // IsRemotable,
							(bool)objects[8], // IsGenerated,
							(bool)objects[9], // IsATObject,
							(bool)objects[10], // IsSessionObject,
							(bool)objects[11], // IsPersistent,
							(int)objects[12], // CatalogObjectID,
							(int)objects[13], // ParentObjectID
							(int)objects[14] // GeneratorObjectID
						);
						
				return null;
			}
			finally
			{
				CloseCursor(objects);
			}
		}
		
		public Schema.FullObjectHeaders SelectChildObjects(int parentObjectID)
		{
			Schema.FullObjectHeaders headers = new Schema.FullObjectHeaders();
			SQLStoreCursor objects = OpenMatchedCursor("IDX_DAEObjects_Parent_Object_ID", false, parentObjectID);
			try
			{
				while (objects.Next())
				{
					headers.Add
					(
						new Schema.FullObjectHeader
						(
							(int)objects[0], // ID
							(string)objects[1], // Name
							(string)objects[2], // LibraryName,
							(string)objects[15], // Script,
							(string)objects[3], // DisplayName,
							(string)objects[5], // Type,
							(bool)objects[6], // IsSystem,
							(bool)objects[7], // IsRemotable,
							(bool)objects[8], // IsGenerated,
							(bool)objects[9], // IsATObject,
							(bool)objects[10], // IsSessionObject,
							(bool)objects[11], // IsPersistent,
							(int)objects[12], // CatalogObjectID,
							(int)objects[13], // ParentObjectID
							(int)objects[14] // GeneratorObjectID
						)
					);
				}
			}
			finally
			{
				CloseCursor(objects);
			}
			return headers;
		}
		
		public Schema.PersistentObjectHeaders SelectPersistentChildObjects(int catalogObjectID)
		{
			Schema.PersistentObjectHeaders headers = new Schema.PersistentObjectHeaders();
			SQLStoreCursor objects = OpenMatchedCursor("IDX_DAEObjects_Catalog_Object_ID", false, catalogObjectID);
			try
			{
				while (objects.Next())
				{
					if ((bool)objects[11])
					{
						headers.Add
						(
							new Schema.PersistentObjectHeader
							(
								(int)objects[0], // ID
								(string)objects[1], // Name
								(string)objects[2], // LibraryName
								(string)objects[15], // Script
								(string)objects[3], // DisplayName
								(string)objects[5], // ObjectType
								(bool)objects[6], // IsSystem 
								(bool)objects[7], // IsRemotable
								(bool)objects[8], // IsGenerated
								(bool)objects[9], // IsATObject
								(bool)objects[10] // IsSessionObject
							)
						);
					}
				}
			}
			finally
			{
				CloseCursor(objects);
			}
			return headers;
		}
		
		private object[] SelectObjectRow(int objectID)
		{
			SQLStoreCursor objects = OpenCursor("PK_DAEObjects", false);
			try
			{
				if (objects.FindKey(new object[] { objectID }))
					return objects.Select();
				return null;
			}
			finally
			{
				CloseCursor(objects);
			}
		}

		private void SelectObjectDependents(int objectID, int level, Schema.DependentObjectHeaders headers, bool recursive)
		{
			List<int> dependencyList = new List<int>();
			SQLStoreCursor dependencies = OpenMatchedCursor("IDX_DAEObjectDependencies_Dependency_Object_ID", false, objectID);
			try
			{
				while (dependencies.Next())
					if (!headers.Contains((int)dependencies[0]))
						dependencyList.Add((int)dependencies[0]);
			}
			finally
			{
				CloseCursor(dependencies);
			}
			
			for (int dependencyIndex = 0; dependencyIndex < dependencyList.Count; dependencyIndex++)
			{
				object[] objectValue = SelectObjectRow(dependencyList[dependencyIndex]);
				if (objectValue != null)
				{
				    var iD = (int)objectValue[0];
				    var name = (string)objectValue[1];
                    var libraryName = (string)objectValue[2];
				    var displayName = (string)objectValue[3];
				    var description = (string)objectValue[4];
				    var objectType = (string)objectValue[5];
				    var isSystem = (bool)objectValue[6];
				    var isRemotable = (bool)objectValue[7];
				    var isGenerated = (bool)objectValue[8];
				    var isATObject = (bool)objectValue[9];
				    var isSessionObject = (bool)objectValue[10];
				    var isPersistent = (bool)objectValue[11];
				    var catalogObjectID = (int)objectValue[12];
				    var parentObjectID = (int)objectValue[13];
				    var generatorObjectID = (int)objectValue[14];
				    var header = new Schema.DependentObjectHeader
				        (
                            iD, name, 
                            libraryName, displayName, 
                            description, objectType, 
				            isSystem,  isRemotable, 
				            isGenerated, isATObject,
				            isSessionObject, isPersistent, 
				            catalogObjectID, parentObjectID,
                            generatorObjectID, 
				            level,
				            headers.Count + 1 
				        );
				    
                    headers.Add(header);
					
					if (recursive)
						SelectObjectDependents(dependencyList[dependencyIndex], level + 1, headers, recursive);
				}
			}
		}
		
		public Schema.DependentObjectHeaders SelectObjectDependents(int objectID, bool recursive)
		{
			Schema.DependentObjectHeaders headers = new Schema.DependentObjectHeaders();
			SelectObjectDependents(objectID, 1, headers, recursive);
			return headers;
		}
		
		private void SelectObjectDependencies(int objectID, int level, Schema.DependentObjectHeaders headers, bool recursive)
		{
			List<int> dependencyList = new List<int>();
			SQLStoreCursor dependencies = OpenMatchedCursor("PK_DAEObjectDependencies", false, objectID);
			try
			{
				while (dependencies.Next())
					if (!headers.Contains((int)dependencies[1]))
						dependencyList.Add((int)dependencies[1]);
			}
			finally
			{
				CloseCursor(dependencies);
			}
			
			for (int dependencyIndex = 0; dependencyIndex < dependencyList.Count; dependencyIndex++)
			{
				object[] objectValue = SelectObjectRow(dependencyList[dependencyIndex]);
				if (objectValue != null)
				{
					headers.Add
					(
						new Schema.DependentObjectHeader
						(
							(int)objectValue[0], // ID
							(string)objectValue[1], // Name
							(string)objectValue[2], // LibraryName
							(string)objectValue[3], // DisplayName
							(string)objectValue[4], // Description
							(string)objectValue[5], // ObjectType
							(bool)objectValue[6], // IsSystem 
							(bool)objectValue[7], // IsRemotable
							(bool)objectValue[8], // IsGenerated
							(bool)objectValue[9], // IsATObject
							(bool)objectValue[10], // IsSessionObject
							(bool)objectValue[11], // IsPersistent
							(int)objectValue[12], // CatalogObjectID
							(int)objectValue[13], // ParentObjectID,
							(int)objectValue[14], // GeneratorObjectID,
							level, // Level
							headers.Count + 1 // Sequence
						)
					);
					
					if (recursive)
						SelectObjectDependencies(dependencyList[dependencyIndex], level + 1, headers, recursive);
				}
			}
		}

		public Schema.DependentObjectHeaders SelectObjectDependencies(int objectID, bool recursive)
		{
			Schema.DependentObjectHeaders headers = new Schema.DependentObjectHeaders();
			SelectObjectDependencies(objectID, 1, headers, recursive);
			return headers;
		}
		
		public Schema.ScalarTypeHeader SelectScalarType(int scalarTypeID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEScalarTypes", false, scalarTypeID);
			try
			{
				if (cursor.Next())
					return new Schema.ScalarTypeHeader((int)cursor[0], (int)cursor[1], (int)cursor[2]);
					
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}
		
		/// <summary>Returns the set of handlers that invoke the given operator</summary>
		public List<int> SelectOperatorHandlers(int operatorID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEEventHandlers_Operator_ID", false, operatorID);
			try
			{
				List <int> handlers = new List<int>();
				
				while (cursor.Next())
					handlers.Add((int)cursor[0]);
				
				return handlers;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public List<int> SelectObjectHandlers(int sourceObjectID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEEventHandlers_Source_Object_ID", false, sourceObjectID);
			try
			{
				List<int> handlers = new List<int>();
				
				while (cursor.Next())
					handlers.Add((int)cursor[0]);
				
				return handlers;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public TableMapHeader SelectApplicationTransactionTableMap(int sourceTableVarID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEApplicationTransactionTableMaps", false, sourceTableVarID);
			try
			{
				if (cursor.Next())
					return new TableMapHeader((int)cursor[0], (int)cursor[1], (int)cursor[2]);
					
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void InsertApplicationTransactionTableMap(int sourceTableVarID, int translatedTableVarID)
		{
			InsertRow("DAEApplicationTransactionTableMaps", sourceTableVarID, translatedTableVarID, -1);
		}

		public void UpdateApplicationTransactionTableMap(int sourceTableVarID, int deletedTableVarID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEApplicationTransactionTableMaps", true, sourceTableVarID);
			try
			{
				if (cursor.Next())
				{
					cursor[2] = deletedTableVarID;
					cursor.Update();
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void DeleteApplicationTransactionTableMap(int sourceTableVarID)
		{
			DeleteRows("PK_DAEApplicationTransactionTableMaps", sourceTableVarID);
		}

		public int SelectTranslatedApplicationTransactionOperatorID(int sourceOperatorID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEApplicationTransactionOperatorMaps", false, sourceOperatorID);
			try
			{
				if (cursor.Next())
					return (int)cursor[1];
				return -1;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public int SelectSourceApplicationTransactionOperatorID(int translatedOperatorID)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("IDX_DAEApplicationTransactionOperatorMaps_Translated_Operator_ID", false, translatedOperatorID);
			try
			{
				if (cursor.Next())
					return (int)cursor[0];
				return -1;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void InsertApplicationTransactionOperatorMap(int sourceOperatorID, int translatedOperatorID)
		{
			InsertRow("DAEApplicationTransactionOperatorMaps", sourceOperatorID, translatedOperatorID);
		}

		public void DeleteApplicationTransactionOperatorMap(int sourceOperatorID)
		{
			DeleteRows("PK_DAEApplicationTransactionOperatorMaps", sourceOperatorID);
		}

		public string SelectApplicationTransactionOperatorNameMap(string sourceOperatorName)
		{
			SQLStoreCursor cursor = OpenMatchedCursor("PK_DAEApplicationTransactionOperatorNameMaps", false, sourceOperatorName);
			try
			{
				if (cursor.Next())
					return (string)cursor[1];
				return null;
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		public void InsertApplicationTransactionOperatorNameMap(string sourceOperatorName, string translatedOperatorName)
		{
			InsertRow("DAEApplicationTransactionOperatorNameMaps", sourceOperatorName, translatedOperatorName);
		}

		#endregion
	}
}