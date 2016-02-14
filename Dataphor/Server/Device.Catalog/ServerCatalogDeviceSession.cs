/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define LOGDDLINSTRUCTIONS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Store;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class ServerCatalogDeviceSession : CatalogDeviceSession
	{
		protected internal ServerCatalogDeviceSession(Schema.Device device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo){}
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			DisposeCatalogStoreConnection();
		}

		public new ServerCatalogDevice Device { get { return (ServerCatalogDevice)base.Device; } }
		
		#region Execute
		
		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			var catalogDevicePlanNode = planNode.DeviceNode as CatalogDevicePlanNode;
			if (catalogDevicePlanNode != null)
			{
				CatalogDeviceTable table = new CatalogDeviceTable(catalogDevicePlanNode, program, this);
				try
				{
					table.Open();
					return table;
				}
				catch
				{
					table.Dispose();
					throw;
				}
			}
			else
				return base.InternalExecute(program, planNode);
		}
		
		#endregion

		#region Catalog Store

		private bool _isUpdatable;
		private int _acquireCount;
		private CatalogStoreConnection _catalogStoreConnection;

		public CatalogStoreConnection CatalogStoreConnection
		{
			get
			{
				Error.AssertFail(_catalogStoreConnection != null, "Internal Error: No catalog store connection established.");
				return _catalogStoreConnection;
			}
		}

		/// <summary>Requests that the session acquire a connection to the catalog store.</summary>
		/// <remarks>		
		/// If the connection is requested updatable, this will be a dedicated connection owned by the session.
		/// Otherwise, the connection is acquired from a pool of connections maintained by the store,
		/// and must be released by a call to ReleaseCatalogConnection. Calling ReleaseCatalogConnection
		/// with the dedicated updatable connection will have no affect.
		/// </remarks>
		protected void AcquireCatalogStoreConnection(bool isUpdatable)
		{
			_acquireCount++;

			if (_catalogStoreConnection == null)
			{
				if (isUpdatable)
					_catalogStoreConnection = Device.Store.Connect();
				else
					_catalogStoreConnection = Device.Store.AcquireConnection();
			}

			if (isUpdatable)
			{
				if (_isUpdatable != isUpdatable)
				{
					_isUpdatable = true;
					for (int index = 0; index < ServerProcess.TransactionCount; index++)
						_catalogStoreConnection.BeginTransaction(ServerProcess.Transactions[index].IsolationLevel);
				}
			}
		}

		/// <summary>Releases a previously acquired catalog store connection back to the connection pool.</summary>
		/// <remarks>
		/// Note that if the given connection was acquired updatable, then this call will have no affect, because
		/// the store connection is owned by the device session.
		/// </remarks>
		protected void ReleaseCatalogStoreConnection()
		{
			_acquireCount--;

			if ((_acquireCount == 0) && !_isUpdatable)
			{
				Device.Store.ReleaseConnection(_catalogStoreConnection);
				_catalogStoreConnection = null;
			}
		}

		private void DisposeCatalogStoreConnection()
		{
			if (_catalogStoreConnection != null)
			{
				_catalogStoreConnection.Dispose();
				_catalogStoreConnection = null;
			}
		}

		public void PopulateStoreCounters(Table table, IRow row)
		{
			SQLStoreCounter counter;
			for (int index = 0; index < Device.Store.Counters.Count; index++)
			{
			    counter = Device.Store.Counters[index];
			    row[0] = index;
			    row[1] = counter.Operation;
			    row[2] = counter.TableName;
			    row[3] = counter.IndexName;
			    row[4] = counter.IsMatched;
			    row[5] = counter.IsRanged;
			    row[6] = counter.IsUpdatable;
			    row[7] = counter.Duration;
			    table.Insert(row);
			}
		}

		public void ClearStoreCounters()
		{
			Device.Store.Counters.Clear();
		}

		#endregion
		
		#region Persistence

		public void SaveServerSettings(Server.Engine server)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SaveServerSettings(server);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void LoadServerSettings(Server.Engine server)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				CatalogStoreConnection.LoadServerSettings(server);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		/// <summary>Returns true if the catalog contains no objects. This will only be true on first-time startup of a server.</summary>
		public bool IsEmpty()
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.IsEmpty();
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SnapshotBase()
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SnapshotBase();
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public Schema.Objects GetBaseCatalogObjects()
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Schema.CatalogObjectHeaders headers = CatalogStoreConnection.SelectBaseCatalogObjects();
				Schema.Objects objects = new Schema.Objects();
				for (int index = 0; index < headers.Count; index++)
					objects.Add(ResolveCatalogObject(headers[index].ID));
				return objects;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.CatalogObjectHeaders SelectLibraryCatalogObjects(string libraryName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectLibraryCatalogObjects(libraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.CatalogObjectHeaders SelectGeneratedObjects(int objectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectGeneratedObjects(objectID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public int GetMaxObjectID()
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.GetMaxObjectID();
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void InternalInsertPersistentObject(Schema.Object objectValue)
		{
			CatalogStoreConnection.InsertPersistentObject(objectValue, ScriptPersistentObject(objectValue));
		}

		private void InsertPersistentChildren(Schema.Object objectValue)
		{
			// ScalarType
			Schema.ScalarType scalarType = objectValue as Schema.ScalarType;
			if (scalarType != null)
			{
				// Persistent representations
				for (int index = 0; index < scalarType.Representations.Count; index++)
					if (scalarType.Representations[index].IsPersistent)
						InternalInsertPersistentObject(scalarType.Representations[index]);

				// Persistent default
				if ((scalarType.Default != null) && scalarType.Default.IsPersistent)
					InternalInsertPersistentObject(scalarType.Default);

				// Persistent constraints
				for (int index = 0; index < scalarType.Constraints.Count; index++)
					if (scalarType.Constraints[index].IsPersistent)
						InternalInsertPersistentObject(scalarType.Constraints[index]);

				// Persistent specials
				for (int index = 0; index < scalarType.Specials.Count; index++)
					if (scalarType.Specials[index].IsPersistent)
						InternalInsertPersistentObject(scalarType.Specials[index]);
			}

			// TableVar
			Schema.TableVar tableVar = objectValue as Schema.TableVar;
			if (tableVar != null)
			{
				for (int index = 0; index < tableVar.Columns.Count; index++)
				{
					Schema.TableVarColumn column = tableVar.Columns[index];

					// Persistent column default
					if ((column.Default != null) && column.Default.IsPersistent)
						InternalInsertPersistentObject(column.Default);

					// Persistent column constraints
					for (int subIndex = 0; subIndex < column.Constraints.Count; subIndex++)
						if (column.Constraints[subIndex].IsPersistent)
							InternalInsertPersistentObject(column.Constraints[subIndex]);
				}

				// Persistent constraints
				if (tableVar.HasConstraints())
				for (int index = 0; index < tableVar.Constraints.Count; index++)
					if (tableVar.Constraints[index].IsPersistent && !tableVar.Constraints[index].IsGenerated)
						InternalInsertPersistentObject(tableVar.Constraints[index]);
			}
		}

		private void InsertPersistentObject(Schema.Object objectValue)
		{
			if (objectValue is CatalogObject)
				Device.NameCache.Clear(objectValue.Name);

			if (objectValue is Operator)
				Device.OperatorNameCache.Clear(objectValue.Name);

			AcquireCatalogStoreConnection(true);
			try
			{
				InternalInsertPersistentObject(objectValue);
				InsertPersistentChildren(objectValue);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void UpdatePersistentObject(Schema.Object objectValue)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdatePersistentObject(objectValue, ScriptPersistentObject(objectValue));
				InsertPersistentChildren(objectValue);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void UpdatePersistentObjectData(Schema.Object objectValue)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdatePersistentObjectData(objectValue, ScriptPersistentObject(objectValue));
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void DeletePersistentObject(Schema.Object objectValue)
		{
			if (objectValue is Schema.CatalogObject)
				Device.NameCache.Clear(objectValue.Name);

			if (objectValue is Schema.Operator)
				Device.OperatorNameCache.Clear(objectValue.Name);

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeletePersistentObject(objectValue);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void ComputeLoadOrderForHandlers(Schema.FullObjectHeaders loadOrder, int objectID)
		{
			List<int> handlers = CatalogStoreConnection.SelectObjectHandlers(objectID);
			for (int index = 0; index < handlers.Count; index++)
				ComputeLoadOrder(loadOrder, handlers[index], -1);
		}

		private void ComputeLoadOrderForGeneratedObjects(Schema.FullObjectHeaders loadOrder, int objectID)
		{
			Schema.CatalogObjectHeaders generatedObjects = CatalogStoreConnection.SelectGeneratedObjects(objectID);
			for (int index = 0; index < generatedObjects.Count; index++)
				ComputeLoadOrder(loadOrder, generatedObjects[index].ID, -1);
		}

		private void ComputeLoadOrderForImplicitConversions(Schema.FullObjectHeaders loadOrder, int objectID)
		{
			Schema.DependentObjectHeaders dependents = CatalogStoreConnection.SelectObjectDependents(objectID, false);
			for (int index = 0; index < dependents.Count; index++)
				if (dependents[index].ObjectType == "Conversion")
					ComputeLoadOrder(loadOrder, dependents[index].ID, -1);
		}

		private void ComputeLoadOrderForConstraints(Schema.FullObjectHeaders loadOrder, int objectID)
		{
			Schema.DependentObjectHeaders dependents = CatalogStoreConnection.SelectObjectDependents(objectID, false);
			for (int index = 0; index < dependents.Count; index++)
				if ((dependents[index].ObjectType == "Reference") || (dependents[index].ObjectType == "CatalogConstraint"))
					ComputeLoadOrder(loadOrder, dependents[index].ID, -1);
		}

		private void ComputeLoadOrderForDependencies(Schema.FullObjectHeaders loadOrder, int objectID)
		{
			Schema.DependentObjectHeaders dependencies = CatalogStoreConnection.SelectObjectDependencies(objectID, false);
			for (int index = 0; index < dependencies.Count; index++)
				if (!dependencies[index].IsPersistent)
					ComputeLoadOrder(loadOrder, dependencies[index].CatalogObjectID, -1);
				else
					ComputeLoadOrder(loadOrder, dependencies[index].ID, dependencies[index].CatalogObjectID);
		}

		private void ComputeLoadOrder(Schema.FullObjectHeaders loadOrder, int objectID, int catalogObjectID)
		{
			// If this object is not already in the load order and it is not in the cache
			if
			(
				(
					((catalogObjectID < 0) && !Device.CatalogIndex.ContainsKey(objectID)) ||
					((catalogObjectID >= 0) && !Device.CatalogIndex.ContainsKey(catalogObjectID))
				) &&
				!loadOrder.Contains(objectID)
			)
			{
				// Compute the load order for all dependencies of the object
				ComputeLoadOrderForDependencies(loadOrder, objectID);

				// If this is a child object, ensure the catalog object is loaded
				if (catalogObjectID >= 0)
					ComputeLoadOrder(loadOrder, catalogObjectID, -1);

				if (!loadOrder.Contains(objectID))
				{
					// Load the catalog object header from the store
					Schema.FullObjectHeader header = null;

					if (catalogObjectID < 0)
					{
						header = CatalogStoreConnection.SelectCatalogObject(objectID);
						if (header == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectHeaderNotFound, objectID);
					}
					else
					{
						header = CatalogStoreConnection.SelectFullObject(objectID);
						if (header == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectHeaderNotFound, objectID);
					}

					// Dependencies of non-persistent immediate children and subchildren
					Schema.FullObjectHeaders children = CatalogStoreConnection.SelectChildObjects(objectID);
					for (int index = 0; index < children.Count; index++)
						if (!children[index].IsPersistent)
						{
							ComputeLoadOrderForDependencies(loadOrder, children[index].ID);
							Schema.FullObjectHeaders subChildren = CatalogStoreConnection.SelectChildObjects(children[index].ID);
							for (int subIndex = 0; subIndex < subChildren.Count; subIndex++)
								if (!subChildren[subIndex].IsPersistent)
									ComputeLoadOrderForDependencies(loadOrder, subChildren[subIndex].ID);
						}

					// Add the object to the load order
					loadOrder.Add(header);

					// Add the objects children and necessary dependents to the load order
					switch (header.ObjectType)
					{
						case "Special":
							// Generated Objects
							ComputeLoadOrderForGeneratedObjects(loadOrder, objectID);
							break;

						case "Representation":
							// Generated Objects
							ComputeLoadOrderForGeneratedObjects(loadOrder, objectID);

							for (int index = 0; index < children.Count; index++)
								ComputeLoadOrderForGeneratedObjects(loadOrder, children[index].ID);
							break;

						case "ScalarType":
							// Generated objects for non-persistent representations
							for (int index = 0; index < children.Count; index++)
							{
								if ((children[index].ObjectType == "Representation") && !children[index].IsPersistent)
								{
									// Generated Objects
									ComputeLoadOrderForGeneratedObjects(loadOrder, children[index].ID);

									Schema.FullObjectHeaders properties = CatalogStoreConnection.SelectChildObjects(children[index].ID);
									for (int propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
										ComputeLoadOrderForGeneratedObjects(loadOrder, properties[propertyIndex].ID);
								}
							}

							// Generated Objects
							ComputeLoadOrderForGeneratedObjects(loadOrder, objectID);

							// Persistent representations and generated objects for them
							for (int index = 0; index < children.Count; index++)
								if ((children[index].ObjectType == "Representation") && children[index].IsPersistent)
									ComputeLoadOrder(loadOrder, children[index].ID, children[index].CatalogObjectID);

							// Implicit Conversions
							ComputeLoadOrderForImplicitConversions(loadOrder, objectID);

							// Default, Constraints and Specials
							for (int index = 0; index < children.Count; index++)
							{
								if (children[index].ObjectType != "Representation")
								{
									if (children[index].IsPersistent)
										ComputeLoadOrder(loadOrder, children[index].ID, children[index].CatalogObjectID);

									ComputeLoadOrderForGeneratedObjects(loadOrder, children[index].ID);
								}
							}

							// Sorts
							Schema.ScalarTypeHeader scalarTypeHeader = CatalogStoreConnection.SelectScalarType(objectID);
							Error.AssertFail(scalarTypeHeader != null, "Scalar type header not found for scalar type ({0})", objectID);

							if (scalarTypeHeader.UniqueSortID >= 0)
								ComputeLoadOrder(loadOrder, scalarTypeHeader.UniqueSortID, -1);

							if (scalarTypeHeader.SortID >= 0)
								ComputeLoadOrder(loadOrder, scalarTypeHeader.SortID, -1);

							// Handlers
							ComputeLoadOrderForHandlers(loadOrder, objectID);
							break;

						case "BaseTableVar":
						case "DerivedTableVar":
							// Immediate persistent children
							for (int index = 0; index < children.Count; index++)
							{
								if (children[index].IsPersistent)
									ComputeLoadOrder(loadOrder, children[index].ID, children[index].CatalogObjectID);

								if (children[index].ObjectType == "TableVarColumn")
								{
									// Defaults and Constraints
									Schema.FullObjectHeaders columnChildren = CatalogStoreConnection.SelectChildObjects(children[index].ID);
									for (int columnChildIndex = 0; columnChildIndex < columnChildren.Count; columnChildIndex++)
										if (columnChildren[columnChildIndex].IsPersistent)
											ComputeLoadOrder(loadOrder, columnChildren[columnChildIndex].ID, children[index].CatalogObjectID);

									// Handlers
									ComputeLoadOrderForHandlers(loadOrder, children[index].ID);
								}
							}

							// Constraints
							ComputeLoadOrderForConstraints(loadOrder, objectID);

							// Handlers
							ComputeLoadOrderForHandlers(loadOrder, objectID);

							break;
					}
				}
			}
		}

		public override bool CatalogObjectExists(string objectName)
		{
			// Search for a positive in the cache first
			Schema.CatalogObjectHeaders result = Device.NameCache.Resolve(objectName);
			if (result != null)
				return result.Count > 0;
			else
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					return CatalogStoreConnection.CatalogObjectExists(objectName);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		#endregion

		#region Libraries
		
		protected void InsertLibrary(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemCreateLibraryNode.CreateLibrary
			(
				program,
				new Schema.Library
				(
					(string)row[0],
					(string)row[1],
					(VersionNumber)row[2],
					(string)row[3]
				),
				false,
				true
			);
		}

		protected void UpdateLibrary(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			string AOldName = (string)oldRow[0];
			string ANewName = (string)newRow[0];
			VersionNumber ANewVersion = (VersionNumber)newRow[2];
			SystemRenameLibraryNode.RenameLibrary(program, Schema.Object.EnsureRooted(AOldName), ANewName, true);
			SystemSetLibraryDescriptorNode.ChangeLibraryVersion(program, Schema.Object.EnsureRooted(ANewName), ANewVersion, false);
			SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(program, Schema.Object.EnsureRooted(ANewName), (string)newRow[3], false);
		}

		protected void DeleteLibrary(Program program, Schema.TableVar tableVar, IRow row)
		{
			LibraryUtility.DropLibrary(program, Schema.Object.EnsureRooted((string)row[0]), true);
		}

		protected void InsertLibraryRequisite(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.AddLibraryRequisite(program, Schema.Object.EnsureRooted((string)row[0]), new LibraryReference((string)row[1], (VersionNumber)row[2]));
		}

		protected void UpdateLibraryRequisite(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			if (String.Compare((string)oldRow[0], (string)newRow[0]) != 0)
			{
				SystemSetLibraryDescriptorNode.RemoveLibraryRequisite(program, Schema.Object.EnsureRooted((string)oldRow[0]), new LibraryReference((string)oldRow[1], (VersionNumber)oldRow[2]));
				SystemSetLibraryDescriptorNode.AddLibraryRequisite(program, Schema.Object.EnsureRooted((string)newRow[0]), new LibraryReference((string)newRow[1], (VersionNumber)newRow[2]));
			}
			else
				SystemSetLibraryDescriptorNode.UpdateLibraryRequisite
				(
					program,
					Schema.Object.EnsureRooted((string)oldRow[0]),
					new LibraryReference((string)oldRow[1], (VersionNumber)oldRow[2]),
					new LibraryReference((string)newRow[1], (VersionNumber)newRow[2])
				);
		}

		protected void DeleteLibraryRequisite(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryRequisite(program, Schema.Object.EnsureRooted((string)row[0]), new LibraryReference((string)row[1], (VersionNumber)row[2]));
		}

		protected void InsertLibrarySetting(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.AddLibrarySetting(program, Schema.Object.EnsureRooted((string)row[0]), new Tag((string)row[1], (string)row[2]));
		}

		protected void UpdateLibrarySetting(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			if (String.Compare((string)oldRow[0], (string)newRow[0]) != 0)
			{
				SystemSetLibraryDescriptorNode.RemoveLibrarySetting(program, Schema.Object.EnsureRooted((string)oldRow[0]), new Tag((string)oldRow[1], (string)oldRow[2]));
				SystemSetLibraryDescriptorNode.AddLibrarySetting(program, Schema.Object.EnsureRooted((string)newRow[0]), new Tag((string)newRow[1], (string)newRow[2]));
			}
			else
				SystemSetLibraryDescriptorNode.UpdateLibrarySetting
				(
					program,
					Schema.Object.EnsureRooted((string)oldRow[0]),
					new Tag((string)oldRow[1], (string)oldRow[2]),
					new Tag((string)newRow[1], (string)newRow[2])
				);
		}

		protected void DeleteLibrarySetting(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.RemoveLibrarySetting(program, Schema.Object.EnsureRooted((string)row[0]), new Tag((string)row[1], (string)row[2]));
		}

		protected void InsertLibraryFile(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.AddLibraryFile(program, Schema.Object.EnsureRooted((string)row[0]), new FileReference((string)row[1], (bool)row[2]));
		}
		
		protected void UpdateLibraryFile(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			if 
			(
				((string)oldRow[0] != (string)newRow[0])
					|| ((string)oldRow[1] != (string)newRow[1])
					|| ((bool)oldRow[2] != (bool)newRow[2])
			)
			{
				SystemSetLibraryDescriptorNode.RemoveLibraryFile(program, Schema.Object.EnsureRooted((string)oldRow[0]), new FileReference((string)oldRow[1], (bool)oldRow[2]));
				SystemSetLibraryDescriptorNode.AddLibraryFile(program, Schema.Object.EnsureRooted((string)newRow[0]), new FileReference((string)newRow[1], (bool)newRow[2]));
			}
		}

		protected void DeleteLibraryFile(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFile(program, Schema.Object.EnsureRooted((string)row[0]), new FileReference((string)row[1], (bool)row[2]));
		}
		
		protected void InsertLibraryFileEnvironment(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.AddLibraryFileEnvironment(program, Schema.Object.EnsureRooted((string)row[0]), (string)row[1], (string)row[2]);
		}
		
		protected void UpdateLibraryFileEnvironment(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFileEnvironment(program, Schema.Object.EnsureRooted((string)oldRow[0]), (string)oldRow[1], (string)oldRow[2]);
			SystemSetLibraryDescriptorNode.AddLibraryFileEnvironment(program, Schema.Object.EnsureRooted((string)newRow[0]), (string)newRow[1], (string)newRow[2]);
		}
		
		protected void DeleteLibraryFileEnvironment(Program program, Schema.TableVar tableVar, IRow row)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFileEnvironment(program, Schema.Object.EnsureRooted((string)row[0]), (string)row[1], (string)row[2]);
		}

		protected internal void SelectLibraryVersions(Program program, NativeTable nativeTable, IRow row)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				using (SQLStoreCursor cursor = CatalogStoreConnection.SelectLibraryVersions())
				{
					while (cursor.Next())
					{
						row[0] = (string)cursor[0];
						row[1] = VersionNumber.Parse((string)cursor[1]);
						nativeTable.Insert(program.ValueManager, row);
					}
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void InsertLibraryVersion(Program program, Schema.TableVar tableVar, IRow row)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertLibraryVersion((string)row[0], (VersionNumber)row[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void UpdateLibraryVersion(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				if ((string)oldRow[0] != (string)newRow[0])
				{
					CatalogStoreConnection.DeleteLibraryVersion((string)oldRow[0]);
					CatalogStoreConnection.InsertLibraryVersion((string)newRow[0], (VersionNumber)newRow[1]);
				}
				else
					CatalogStoreConnection.UpdateLibraryVersion((string)oldRow[0], (VersionNumber)newRow[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void DeleteLibraryVersion(Program program, Schema.TableVar tableVar, IRow row)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryVersion((string)row[0]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected internal void SelectLibraryOwners(Program program, NativeTable nativeTable, IRow row)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				using (SQLStoreCursor cursor = CatalogStoreConnection.SelectLibraryOwners())
				{
					while (cursor.Next())
					{
						row[0] = (string)cursor[0];
						row[1] = (string)cursor[1];
						nativeTable.Insert(program.ValueManager, row);
					}
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void InsertLibraryOwner(Program program, Schema.TableVar tableVar, IRow row)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertLibraryOwner((string)row[0], (string)row[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void UpdateLibraryOwner(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				if ((string)oldRow[0] != (string)newRow[0])
				{
					CatalogStoreConnection.DeleteLibraryOwner((string)oldRow[0]);
					CatalogStoreConnection.InsertLibraryOwner((string)newRow[0], (string)newRow[1]);
				}
				else
					CatalogStoreConnection.UpdateLibraryOwner((string)oldRow[0], (string)newRow[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void DeleteLibraryOwner(Program program, Schema.TableVar tableVar, IRow row)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryOwner((string)row[0]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		/// <summary>Attach each library that was attached from a different library directory.</summary>
		public void AttachLibraries()
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Program program = new Program(ServerProcess);
				program.Start(null);
				try
				{
					using (SQLStoreCursor cursor = CatalogStoreConnection.SelectLibraryDirectories())
					{
						while (cursor.Next())
							LibraryUtility.AttachLibrary(program, (string)cursor[0], (string)cursor[1], true);
					}
				}
				finally
				{
					program.Stop(null);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetLibraryDirectory(string libraryName, string libraryDirectory)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SetLibraryDirectory(libraryName, libraryDirectory);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void DeleteLibraryDirectory(string libraryName)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryDirectory(libraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		protected override void InternalInsertRow(Program program, Schema.TableVar tableVar, IRow row, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "System.LibraryVersions" : InsertLibraryVersion(program, tableVar, row); break;
				case "System.LibraryOwners" : InsertLibraryOwner(program, tableVar, row); break;
				case "System.Libraries" : InsertLibrary(program, tableVar, row); break;
				case "System.LibraryRequisites" : InsertLibraryRequisite(program, tableVar, row); break;
				case "System.LibrarySettings" : InsertLibrarySetting(program, tableVar, row); break;
				case "System.LibraryFiles" : InsertLibraryFile(program, tableVar, row); break;
				case "System.LibraryFileEnvironments" : InsertLibraryFileEnvironment(program, tableVar, row); break;
			}
			// TODO: This hack enables A/T style editing of a library (the requisites and files adds will automatically create a library)
			// Basically it's a deferred constraint check
			if ((tableVar.Name == "System.Libraries") && GetTables(tableVar.Scope)[tableVar].HasRow(ServerProcess.ValueManager, row))
				return;
			base.InternalInsertRow(program, tableVar, row, valueFlags);
		}
		
		private void LibraryUpdateRow(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "System.LibraryVersions": UpdateLibraryVersion(program, tableVar, oldRow, newRow); break;
				case "System.LibraryOwners" : UpdateLibraryOwner(program, tableVar, oldRow, newRow); break;
				case "System.Libraries" : UpdateLibrary(program, tableVar, oldRow, newRow); break;
				case "System.LibraryRequisites" : UpdateLibraryRequisite(program, tableVar, oldRow, newRow); break;
				case "System.LibrarySettings" : UpdateLibrarySetting(program, tableVar, oldRow, newRow); break;
				case "System.LibraryFiles" : UpdateLibraryFile(program, tableVar, oldRow, newRow); break;
				case "System.LibraryFileEnvironments" : UpdateLibraryFileEnvironment(program, tableVar, oldRow, newRow); break;
			}
		}
		
		protected override void InternalDeleteRow(Program program, Schema.TableVar tableVar, IRow row)
		{
			switch (tableVar.Name)
			{
				case "System.LibraryVersions" : DeleteLibraryVersion(program, tableVar, row); break;
				case "System.LibraryOwners" : DeleteLibraryOwner(program, tableVar, row); break;
				case "System.Libraries" : DeleteLibrary(program, tableVar, row); break;
				case "System.LibraryRequisites" : DeleteLibraryRequisite(program, tableVar, row); break;
				case "System.LibrarySettings" : DeleteLibrarySetting(program, tableVar, row); break;
				case "System.LibraryFiles" : DeleteLibraryFile(program, tableVar, row); break;
				case "System.LibraryFileEnvironments" : DeleteLibraryFileEnvironment(program, tableVar, row); break;
				default : throw new CatalogException(CatalogException.Codes.UnsupportedUpdate, tableVar.Name);
			}
			base.InternalDeleteRow(program, tableVar, row);
		}

		protected override void InternalInsertLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			// If this is not a repository, and we are not deserializing, insert the loaded library in the catalog store
			if (!ServerProcess.InLoadingContext())
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.InsertLoadedLibrary(loadedLibrary.Name);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		protected override void InternalDeleteLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLoadedLibrary(loadedLibrary.Name);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		public List<string> SelectLoadedLibraries()
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectLoadedLibraries();
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		public override void ResolveLoadedLibraries()
		{
			// TODO: I don't have a better way to ensure that these are always in memory. The footprint should be small, but how else do you answer the question, who's looking at me?
			AcquireCatalogStoreConnection(false);
			try
			{
				foreach (string libraryName in CatalogStoreConnection.SelectLoadedLibraries())
					ResolveLoadedLibrary(libraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		protected override LoadedLibrary InternalResolveLoadedLibrary(Schema.Library LLibrary)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				if (CatalogStoreConnection.LoadedLibraryExists(LLibrary.Name))
				{
					ServerProcess.PushLoadingContext(new LoadingContext(ServerProcess.ServerSession.User, String.Empty));
					try
					{
						Program program = new Program(ServerProcess);
						program.Start(null);
						try
						{
							LibraryUtility.LoadLibrary(program, LLibrary.Name);
							return Catalog.LoadedLibraries[LLibrary.Name];
						}
						finally
						{
							program.Stop(null);
						}
					}
					finally
					{
						ServerProcess.PopLoadingContext();
					}
				}
				return null;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public string GetLibraryOwner(string libraryName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string ownerID = CatalogStoreConnection.SelectLibraryOwner(libraryName);
				return ownerID == null ? Server.Engine.AdminUserID : ownerID;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void ClearTableCache(string tableName)
		{
			int index = Catalog.IndexOfName(tableName);
			if (index >= 0)
			{
				lock (Device.Headers)
				{
					Device.Headers[(Schema.TableVar)Catalog[index]].Cached = false;
				}
			}
		}
		
		public void SetLibraryOwner(string libraryName, string userID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				string ownerID = CatalogStoreConnection.SelectLibraryOwner(libraryName);
				if ((ownerID == null) || (ownerID != userID))
				{
					ClearTableCache("System.LibraryOwners");
					if (ownerID == null)
						CatalogStoreConnection.InsertLibraryOwner(libraryName, userID);
					else
						CatalogStoreConnection.UpdateLibraryOwner(libraryName, userID);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void ClearLibraryOwner(string libraryName)
		{
			ClearTableCache("System.LibraryOwners");
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryOwner(libraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public VersionNumber GetCurrentLibraryVersion(string libraryName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string version = CatalogStoreConnection.SelectLibraryVersion(libraryName);
				return version == null ? Catalog.Libraries[libraryName].Version : VersionNumber.Parse(version);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetCurrentLibraryVersion(string libraryName, VersionNumber versionNumber)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				string version = CatalogStoreConnection.SelectLibraryVersion(libraryName);
				if ((version == null) || !VersionNumber.Parse(version).Equals(versionNumber))
				{
					ClearTableCache("System.LibraryVersions");
					if (version == null)
						CatalogStoreConnection.InsertLibraryVersion(libraryName, versionNumber);
					else
						CatalogStoreConnection.UpdateLibraryVersion(libraryName, versionNumber);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void ClearCurrentLibraryVersion(string libraryName)
		{
			ClearTableCache("System.LibraryVersions");
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryVersion(libraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		#endregion

		#region Object Load
		
		private void FixupSystemScalarTypeReferences(ScalarType LScalarType)
		{
			switch (LScalarType.Name)
			{
				case Schema.DataTypes.SystemScalarName: Catalog.DataTypes.SystemScalar = LScalarType; break;
				case Schema.DataTypes.SystemBooleanName: Catalog.DataTypes.SystemBoolean = LScalarType; LScalarType.NativeType = typeof(bool); break;
				case Schema.DataTypes.SystemDecimalName: Catalog.DataTypes.SystemDecimal = LScalarType; LScalarType.NativeType = typeof(decimal); break;
				case Schema.DataTypes.SystemLongName: Catalog.DataTypes.SystemLong = LScalarType; LScalarType.NativeType = typeof(long); break;
				case Schema.DataTypes.SystemIntegerName: Catalog.DataTypes.SystemInteger = LScalarType; LScalarType.NativeType = typeof(int); break;
				case Schema.DataTypes.SystemShortName: Catalog.DataTypes.SystemShort = LScalarType; LScalarType.NativeType = typeof(short); break;
				case Schema.DataTypes.SystemByteName: Catalog.DataTypes.SystemByte = LScalarType; LScalarType.NativeType = typeof(byte); break;
				case Schema.DataTypes.SystemStringName: Catalog.DataTypes.SystemString = LScalarType; LScalarType.NativeType = typeof(string); break;
				case Schema.DataTypes.SystemTimeSpanName: Catalog.DataTypes.SystemTimeSpan = LScalarType; LScalarType.NativeType = typeof(TimeSpan); break;
				case Schema.DataTypes.SystemDateTimeName: Catalog.DataTypes.SystemDateTime = LScalarType; LScalarType.NativeType = typeof(DateTime); break;
				case Schema.DataTypes.SystemDateName: Catalog.DataTypes.SystemDate = LScalarType; LScalarType.NativeType = typeof(DateTime); break;
				case Schema.DataTypes.SystemTimeName: Catalog.DataTypes.SystemTime = LScalarType; LScalarType.NativeType = typeof(DateTime); break;
				case Schema.DataTypes.SystemMoneyName: Catalog.DataTypes.SystemMoney = LScalarType; LScalarType.NativeType = typeof(decimal); break;
				case Schema.DataTypes.SystemGuidName: Catalog.DataTypes.SystemGuid = LScalarType; LScalarType.NativeType = typeof(Guid); break;
				case Schema.DataTypes.SystemBinaryName: Catalog.DataTypes.SystemBinary = LScalarType; LScalarType.NativeType = typeof(byte[]); break;
				case Schema.DataTypes.SystemGraphicName: Catalog.DataTypes.SystemGraphic = LScalarType; LScalarType.NativeType = typeof(byte[]); break;
				case Schema.DataTypes.SystemErrorName: Catalog.DataTypes.SystemError = LScalarType; LScalarType.NativeType = typeof(Exception); break;
				case Schema.DataTypes.SystemNameName: Catalog.DataTypes.SystemName = LScalarType; LScalarType.NativeType = typeof(string); break;
			}
		}

		/*
			LoadCatalogObject ->
			
				Compute Load Order
					LoadDependencies
					LoadObject
					LoadPersistentChildren
					if (ScalarType)
						LoadImplicitConversions
						LoadHandlers
						LoadGeneratedDependents
					if (Table)
						LoadConstraints
						LoadHandlers
					if (Column)
						LoadHandlers
						
				Load Each Object in load order (do not allow loading while loading)
					Perform Fixups as each object is loaded
		*/
		private Schema.CatalogObject LoadCatalogObject(int objectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Program program = new Program(ServerProcess);
				program.Start(null);
				try
				{
					Schema.CatalogObject result = null;
					Schema.Objects scalarTypes = new Schema.Objects();
					Schema.FullObjectHeaders loadOrder = new Schema.FullObjectHeaders();
					ComputeLoadOrder(loadOrder, objectID, -1);

					Schema.FullObjectHeader objectHeader;
					Schema.FullCatalogObjectHeader catalogObjectHeader;
					for (int index = 0; index < loadOrder.Count; index++)
					{
						objectHeader = loadOrder[index];
						catalogObjectHeader = objectHeader as Schema.FullCatalogObjectHeader;
						if (catalogObjectHeader != null)
						{
							Schema.CatalogObject objectValue = LoadCatalogObject(program, objectHeader.ID, ResolveUser(catalogObjectHeader.OwnerID), objectHeader.LibraryName, objectHeader.Script, objectHeader.IsGenerated, objectHeader.IsATObject);
							if (objectValue is Schema.ScalarType)
								scalarTypes.Add(objectValue);
							if (catalogObjectHeader.ID == objectID)
								result = objectValue;
						}
						else
							LoadPersistentObject(program, objectHeader.ID, ResolveCachedCatalogObject(objectHeader.CatalogObjectID, true).Owner, objectHeader.LibraryName, objectHeader.Script, objectHeader.IsATObject);
					}

					// Once all the objects have loaded, fixup pointers to generated objects
					for (int index = 0; index < scalarTypes.Count; index++)
						((Schema.ScalarType)scalarTypes[index]).ResolveGeneratedDependents(this);

					if (result != null)
						return result;

					throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectLoadFailed, ErrorSeverity.System, objectID);
				}
				finally
				{
					program.Stop(null);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private Schema.CatalogObject LoadCatalogObject(Program program, int objectID, Schema.User user, string libraryName, string script, bool isGenerated, bool isATObject)
		{
			// Load the object itself
			LoadPersistentObject(program, objectID, user, libraryName, script, isATObject);

			string objectName;
			if (Device.CatalogIndex.TryGetValue(objectID, out objectName))
			{
				Schema.CatalogObject result = (Schema.CatalogObject)Catalog[objectName];
				if (isGenerated)
				{
					result.IsGenerated = true;
					result.LoadGeneratorID();
				}

				Schema.ScalarType scalarType = result as Schema.ScalarType;
				if ((scalarType != null) && scalarType.IsSystem)
					FixupSystemScalarTypeReferences(scalarType);

				Schema.Device device = result as Schema.Device;
				if (device != null)
				{
					if (!device.Registered && (HasDeviceObjects(device) || (device is MemoryDevice)))
						device.SetRegistered();

					if (device.Registered)
					{
						ServerProcess.PushLoadingContext(new LoadingContext(user, libraryName));
						try
						{
							// The device must be started within a loading context so that system object maps are not re-created
							ServerProcess.ServerSession.Server.StartDevice(ServerProcess, device);
						}
						finally
						{
							ServerProcess.PopLoadingContext();
						}
					}
				}

				return result;
			}

			throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectLoadFailed, ErrorSeverity.System, objectID);
		}
		
		private void EnsureLibraryLoaded(Program program, string libraryName)
		{
			EnsureLibraryLoaded(program, libraryName, ResolveUser(GetLibraryOwner(libraryName)));
		}

		private void EnsureLibraryLoaded(Program program, string libraryName, Schema.User user)
		{
			// Ensure that the required library is loaded
			if ((libraryName != String.Empty) && !Catalog.LoadedLibraries.Contains(libraryName))
			{
				ServerProcess.PushLoadingContext(new LoadingContext(user, String.Empty));
				try
				{
					LibraryUtility.LoadLibrary(program, libraryName);
				}
				finally
				{
					ServerProcess.PopLoadingContext();
				}
			}
		}
		
		private void LoadPersistentObject(Program program, int objectID, Schema.User user, string libraryName, string script, bool isATObject)
		{
			// Ensure that the required library is loaded
			EnsureLibraryLoaded(program, libraryName, user);

			// Compile and execute the object creation script
			ServerProcess.PushLoadingContext(new LoadingContext(user, libraryName));
			try
			{
				ServerProcess.EnterTimeStampSafeContext();
				try
				{
					if (!isATObject)
						ServerProcess.PushGlobalContext();
					try
					{
						ParserMessages parserMessages = new ParserMessages();
						Statement statement = new Parser().ParseScript(script, parserMessages);
						Plan plan = new Plan(ServerProcess);
						try
						{
							plan.PushSourceContext(new Debug.SourceContext(script, null));
							try
							{
								//LPlan.PlanCatalog.AddRange(LCurrentPlan.PlanCatalog); // add the set of objects currently being compiled
								plan.Messages.AddRange(parserMessages);
								plan.PushSecurityContext(new SecurityContext(user));
								try
								{
									PlanNode planNode = null;
									try
									{
										planNode = Compiler.Compile(plan, statement);
									}
									finally
									{
										//LCurrentPlan.Messages.AddRange(LPlan.Messages); // Propagate compiler exceptions to the outer plan
									}
									try
									{
										plan.CheckCompiled();
										planNode.Execute(program);
									}
									catch (Exception E)
									{
										throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogDeserializationError, ErrorSeverity.System, E, objectID);
									}
								}
								finally
								{
									plan.PopSecurityContext();
								}
							}
							finally
							{
								plan.PopSourceContext();
							}
						}
						finally
						{
							plan.Dispose();
						}
					}
					finally
					{
						if (!isATObject)
							ServerProcess.PopGlobalContext();
					}
				}
				finally
				{
					ServerProcess.ExitTimeStampSafeContext();
				}
			}
			finally
			{
				ServerProcess.PopLoadingContext();
			}
		}

		private Schema.CatalogObjectHeaders CachedResolveCatalogObjectName(string name)
		{
			Schema.CatalogObjectHeaders result = Device.NameCache.Resolve(name);

			if (result == null)
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					result = CatalogStoreConnection.ResolveCatalogObjectName(name);
					Device.NameCache.Add(name, result);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}

			return result;
		}

		/// <summary>Resolves the given name and returns the catalog object, if an unambiguous match is found. Otherwise, returns null.</summary>
		public override Schema.CatalogObject ResolveName(string name, NameResolutionPath path, List<string> names)
		{
			bool rooted = Schema.Object.IsRooted(name);
			
			// If the name is rooted, then it is safe to search for it in the catalog cache first
			if (rooted)
			{
				int index = Catalog.ResolveName(name, path, names);
				if (index >= 0)
					return (Schema.CatalogObject)Catalog[index];
			}

			Schema.CatalogObjectHeaders headers = CachedResolveCatalogObjectName(name);

			if (!rooted)
			{
				Schema.CatalogObjectHeaders levelHeaders = new Schema.CatalogObjectHeaders();

				for (int levelIndex = 0; levelIndex < path.Count; levelIndex++)
				{
					if (levelIndex > 0)
						levelHeaders.Clear();

					for (int index = 0; index < headers.Count; index++)
						if ((headers[index].LibraryName == String.Empty) || path[levelIndex].ContainsName(headers[index].LibraryName))
							levelHeaders.Add(headers[index]);

					if (levelHeaders.Count > 0)
					{
						for (int index = 0; index < levelHeaders.Count; index++)
							names.Add(levelHeaders[index].Name);

						return levelHeaders.Count == 1 ? ResolveCatalogObject(levelHeaders[0].ID) : null;
					}
				}
			}

			// Only resolve objects in loaded libraries
			Schema.CatalogObjectHeader header = null;
			for (int index = 0; index < headers.Count; index++)
			{
				if ((headers[index].LibraryName == String.Empty) || Catalog.LoadedLibraries.Contains(headers[index].LibraryName))
				{
					names.Add(headers[index].Name);
					if (header == null)
						header = headers[index];
					else
						header = null;
				}
			}

			if ((names.Count == 1) && (header != null))
				return ResolveCatalogObject(header.ID);

			// If there is still no resolution, and there is one header, resolve the library and resolve to that name
			if ((headers.Count == 1) && (ResolveLoadedLibrary(headers[0].LibraryName, false) != null))
			{
				names.Add(headers[0].Name);
				return ResolveCatalogObject(headers[0].ID);
			}

			return null;
		}

		protected override Schema.CatalogObjectHeaders CachedResolveOperatorName(string name)
		{
			Schema.CatalogObjectHeaders result = base.CachedResolveOperatorName(name);

			if (result == null)
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					result = CatalogStoreConnection.ResolveOperatorName(name);
					Device.OperatorNameCache.Add(name, result);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}

			return result;
		}

		/// <summary>Resolves the catalog object with the given id. If the object is not found, an error is raised.</summary>
		/// <remarks>
		/// This routine first searches for the object in the catalog cache. If it is not found, it's header is retrieved from
		/// the catalog store, and the object is deserialized using that information. After this routine returns, the object
		/// will be present in the catalog cache.
		/// </remarks>
		public override Schema.CatalogObject ResolveCatalogObject(int objectID)
		{
			// TODO: Catalog deserialization concurrency
			// Right now, use the same lock as the user's cache to ensure no deadlocks can occur during deserialization.
			// This effectively places deserialization granularity at the server level, but until we
			// can provide a solution to the deserialization concurrency deadlock problem, this is all we can do.
			lock (Catalog)
			{
				// Lookup the object in the catalog index
				Schema.CatalogObject result = base.ResolveCatalogObject(objectID);
				if (result == null)
				{
					if (!ServerProcess.InLoadingContext())
						return LoadCatalogObject(objectID);

					// It is an error to attempt to resolve an object that would need to be loaded while we are loading. These
					// dependencies will always be loaded by the LoadCatalogObject call.
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectAlreadyLoading, ErrorSeverity.System, objectID);
				}
				else
					return result;
			}
		}

		public Schema.ObjectHeader SelectObjectHeader(int objectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObject(objectID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.ObjectHeader GetObjectHeader(int objectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Schema.ObjectHeader header = CatalogStoreConnection.SelectObject(objectID);
				if (header == null)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectHeaderNotFound, objectID);

				return header;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion

		#region Catalog Object Update

		protected override void InternalInsertCatalogObject(Schema.CatalogObject objectValue)
		{
			// If this is not a repository, and we are not deserializing, and the object should be persisted, save the object to the catalog store
			if (ShouldSerializeCatalogObject(objectValue))
				InsertPersistentObject(objectValue);
		}

		protected override void InternalUpdateCatalogObject(Schema.CatalogObject objectValue)
		{
			// If this is not a repository, and we are not deserializing, and the object should be persisted, update the object in the catalog store
			if (ShouldSerializeCatalogObject(objectValue))
				UpdatePersistentObject(objectValue);
		}

		protected override void InternalDeleteCatalogObject(Schema.CatalogObject objectValue)
		{
			// If this is not a repository, and the object should be persisted, remove the object from the catalog store
			if (ShouldSerializeCatalogObject(objectValue))
				DeletePersistentObject(objectValue);
		}

		#endregion
		
		#region Transaction
		
		protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
		{
			base.InternalBeginTransaction(isolationLevel);

			if ((_catalogStoreConnection != null) && _isUpdatable)
				_catalogStoreConnection.BeginTransaction(isolationLevel);
		}

		protected override void InternalAfterCommitTransaction()
		{
			base.InternalAfterCommitTransaction();
			
			if ((_catalogStoreConnection != null) && _isUpdatable)
				_catalogStoreConnection.CommitTransaction();
		}

		protected override void InternalAfterRollbackTransaction()
		{
			base.InternalAfterRollbackTransaction();
			
			if ((_catalogStoreConnection != null) && _isUpdatable)
				_catalogStoreConnection.RollbackTransaction();
		}
		
		#endregion

		#region Object Selection

		public override List<int> SelectOperatorHandlers(int operatorID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectOperatorHandlers(operatorID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override List<int> SelectObjectHandlers(int sourceObjectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObjectHandlers(sourceObjectID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

		}

		public override Schema.DependentObjectHeaders SelectObjectDependents(int objectID, bool recursive)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObjectDependents(objectID, recursive);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.DependentObjectHeaders SelectObjectDependencies(int objectID, bool recursive)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObjectDependencies(objectID, recursive);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion

		#region Security

		public override Right ResolveRight(string rightName, bool mustResolve)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Right right = CatalogStoreConnection.SelectRight(rightName);
				if ((right == null) && mustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.RightNotFound, rightName);
				return right;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override void InsertRight(string rightName, string userID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertRight(rightName, userID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override void DeleteRight(string rightName)
		{
			base.DeleteRight(rightName);

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRight(rightName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected override void InternalInsertRole(Schema.Role role)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertRole(role, ScriptCatalogObject(role));
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected override void InternalDeleteRole(Schema.Role role)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRole(role);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public bool RoleHasRight(Schema.Role role, string rightName)
		{
			// TODO: Implement role right assignments caching
			AcquireCatalogStoreConnection(false);
			try
			{
				RightAssignment rightAssignment = CatalogStoreConnection.SelectRoleRightAssignment(role.ID, rightName);
				return (rightAssignment != null) && rightAssignment.Granted;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected override Schema.User InternalResolveUser(string userID, Schema.User LUser)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				LUser = CatalogStoreConnection.SelectUser(userID);
				if (LUser != null)
					InternalCacheUser(LUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
			return LUser;
		}
		
		public bool UserExists(string userID)
		{
			return ResolveUser(userID, false) != null;
		}

		public override void InsertUser(Schema.User user)
		{
			base.InsertUser(user);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new CreateUserInstruction(user));
			#endif

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.InsertUser(user);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public void SetUserPassword(string userID, string password)
		{
			Schema.User user = null;

			lock (Catalog)
			{
				user = ResolveUser(userID);

				#if LOGDDLINSTRUCTIONS
				string userPassword = user.Password;
				#endif
				user.Password = password;
				#if LOGDDLINSTRUCTIONS
				if (ServerProcess.InTransaction)
					_instructions.Add(new SetUserPasswordInstruction(user, userPassword));
				#endif
			}

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.UpdateUser(user);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public void SetUserName(string userID, string userName)
		{
			Schema.User user = null;

			lock (Catalog)
			{
				user = ResolveUser(userID);

				#if LOGDDLINSTRUCTIONS
				string localUserName = user.Name;
				#endif
				user.Name = userName;
				#if LOGDDLINSTRUCTIONS
				if (ServerProcess.InTransaction)
					_instructions.Add(new SetUserNameInstruction(user, localUserName));
				#endif
			}

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.UpdateUser(user);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public void DeleteUser(Schema.User user)
		{
			ClearUser(user.ID);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new DropUserInstruction(user));
			#endif

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.DeleteUser(user.ID);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public bool UserOwnsObjects(string userID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.UserOwnsObjects(userID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public bool UserOwnsRights(string userID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.UserOwnsRights(userID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override bool UserHasRight(string userID, string rightName)
		{
			if (ServerProcess.IsLoading() || (String.Compare(userID, Server.Engine.SystemUserID, true) == 0) || (String.Compare(userID, Server.Engine.AdminUserID, true) == 0))
				return true;

			lock (Catalog)
			{
				Schema.User user = ResolveUser(userID);

				Schema.RightAssignment rightAssignment = user.FindCachedRightAssignment(rightName);

				if (rightAssignment == null)
				{
					AcquireCatalogStoreConnection(false);
					try
					{
						rightAssignment = new Schema.RightAssignment(rightName, CatalogStoreConnection.UserHasRight(userID, rightName));
						user.CacheRightAssignment(rightAssignment);
					}
					finally
					{
						ReleaseCatalogStoreConnection();
					}
				}

				return rightAssignment.Granted;
			}
		}

		public void InsertUserRole(string userID, int roleID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertUserRole(userID, roleID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
			ClearUserCachedRightAssignments(userID);
		}

		public void DeleteUserRole(string userID, int roleID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteUserRole(userID, roleID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			ClearUserCachedRightAssignments(userID);

			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(userID);
		}

		private void MarkRoleOperatorsForRecompile(int roleID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				List<String> users = CatalogStoreConnection.SelectRoleUsers(roleID);
				for (int index = 0; index < users.Count; index++)
					MarkUserOperatorsForRecompile(users[index]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void MarkUserOperatorsForRecompile(string userID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string objectName;
				Schema.CatalogObjectHeaders headers = CatalogStoreConnection.SelectUserOperators(userID);
				for (int index = 0; index < headers.Count; index++)
					if (Device.CatalogIndex.TryGetValue(headers[index].ID, out objectName))
						((Schema.Operator)Catalog[objectName]).ShouldRecompile = true;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void GrantRightToRole(string rightName, int roleID)
		{
			lock (Catalog)
			{
				foreach (Schema.User user in Device.UsersCache.Values)
					user.ClearCachedRightAssignment(rightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureRoleRightAssignment(roleID, rightName, true);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			// Mark operators for each user that is a member of this role to be recompiled on next execution
			if (!ServerProcess.IsLoading())
				MarkRoleOperatorsForRecompile(roleID);
		}

		public void GrantRightToUser(string rightName, string userID)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (Device.UsersCache.TryGetValue(userID, out user))
					user.ClearCachedRightAssignment(rightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureUserRightAssignment(userID, rightName, true);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			// Mark operators for this user to be recompiled on next execution
			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(userID);
		}

		public void RevokeRightFromRole(string rightName, int roleID)
		{
			lock (Catalog)
			{
				foreach (Schema.User user in Device.UsersCache.Values)
					user.ClearCachedRightAssignment(rightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureRoleRightAssignment(roleID, rightName, false);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkRoleOperatorsForRecompile(roleID);
		}

		public void RevokeRightFromUser(string rightName, string userID)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (Device.UsersCache.TryGetValue(userID, out user))
					user.ClearCachedRightAssignment(rightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureUserRightAssignment(userID, rightName, false);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(userID);
		}

		public void RevertRightForRole(string rightName, int roleID)
		{
			lock (Catalog)
			{
				foreach (Schema.User user in Device.UsersCache.Values)
					user.ClearCachedRightAssignment(rightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRoleRightAssignment(roleID, rightName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkRoleOperatorsForRecompile(roleID);
		}

		public void RevertRightForUser(string rightName, string userID)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (Device.UsersCache.TryGetValue(userID, out user))
					user.ClearCachedRightAssignment(rightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteUserRightAssignment(userID, rightName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(userID);
		}

		public void SetCatalogObjectOwner(int catalogObjectID, string userID)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (Device.UsersCache.TryGetValue(userID, out user))
					user.ClearCachedRightAssignments();
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SetCatalogObjectOwner(catalogObjectID, userID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			// TODO: If the object is an operator, and we are not loading, and the object is not generated, and the object is in the cache, mark it for recompile
			// TODO: If we are not loading, for each immediate dependent of this object that is a non-generated operator currently in the cache, mark it for recompile
		}

		public void SetRightOwner(string rightName, string userID)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (Device.UsersCache.TryGetValue(userID, out user))
					user.ClearCachedRightAssignments();
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateRight(rightName, userID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion		
		
		#region Device Users
		
		public override Schema.DeviceUser ResolveDeviceUser(Schema.Device device, Schema.User user, bool mustResolve)
		{
			lock (device.Users)
			{
				Schema.DeviceUser deviceUser;
				if (!device.Users.TryGetValue(user.ID, out deviceUser))
				{
					AcquireCatalogStoreConnection(false);
					try
					{
						deviceUser = CatalogStoreConnection.SelectDeviceUser(device, user);
						if (deviceUser != null)
							device.Users.Add(deviceUser);
					}
					finally
					{
						ReleaseCatalogStoreConnection();
					}
				}

				if ((deviceUser == null) && mustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DeviceUserNotFound, user.ID);

				return deviceUser;
			}
		}

		private void CacheDeviceUser(Schema.DeviceUser deviceUser)
		{
			lock (deviceUser.Device.Users)
			{
				deviceUser.Device.Users.Add(deviceUser);
			}
		}

		private void ClearDeviceUser(Schema.DeviceUser deviceUser)
		{
			lock (deviceUser.Device.Users)
			{
				deviceUser.Device.Users.Remove(deviceUser.User.ID);
			}
		}

		public void InsertDeviceUser(Schema.DeviceUser deviceUser)
		{
			CacheDeviceUser(deviceUser);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new CreateDeviceUserInstruction(deviceUser));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertDeviceUser(deviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetDeviceUserID(DeviceUser deviceUser, string userID)
		{
			#if LOGDDLINSTRUCTIONS
			string originalDeviceUserID = deviceUser.DeviceUserID;
			#endif
			deviceUser.DeviceUserID = userID;
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new SetDeviceUserIDInstruction(deviceUser, originalDeviceUserID));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateDeviceUser(deviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetDeviceUserPassword(DeviceUser deviceUser, string password)
		{
			#if LOGDDLINSTRUCTIONS
			string originalDevicePassword = deviceUser.DevicePassword;
			#endif
			deviceUser.DevicePassword = password;
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new SetDeviceUserPasswordInstruction(deviceUser, originalDevicePassword));
				#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateDeviceUser(deviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetDeviceUserConnectionParameters(DeviceUser deviceUser, string connectionParameters)
		{
			#if LOGDDLINSTRUCTIONS
			string originalConnectionParameters = deviceUser.ConnectionParameters;
			#endif
			deviceUser.ConnectionParameters = connectionParameters;
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new SetDeviceUserConnectionParametersInstruction(deviceUser, originalConnectionParameters));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateDeviceUser(deviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void DeleteDeviceUser(Schema.DeviceUser deviceUser)
		{
			ClearDeviceUser(deviceUser);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new DropDeviceUserInstruction(deviceUser));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteDeviceUser(deviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
				
		#endregion

		#region Device Objects

		public override bool HasDeviceObjects(Schema.Device device)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.HasDeviceObjects(device.ID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.DeviceObject ResolveDeviceObject(Schema.Device device, Schema.Object objectValue)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				int deviceObjectID = CatalogStoreConnection.SelectDeviceObjectID(device.ID, objectValue.ID);
				if (deviceObjectID >= 0)
				{
					// If we are already loading, then a resolve that must load from the cache will fail, 
					// and is a dependency on an object mapping that did not exist when the initial object was created.
					// Therefore if the object is not present in the cache, it is as though the object does not exist.
					if (ServerProcess.InLoadingContext())
						return ResolveCachedCatalogObject(deviceObjectID, false) as Schema.DeviceObject;
					return ResolveCatalogObject(deviceObjectID) as Schema.DeviceObject;
				}
				return null;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion
		
		#region Classes
		
		public string GetClassLibrary(string className)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string libraryName = CatalogStoreConnection.SelectClassLibrary(className);
				if (String.IsNullOrEmpty(libraryName))
					throw new ServerException(ServerException.Codes.ClassAliasNotFound, className);
				return libraryName;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void LoadLibraryForClass(ClassDefinition classDefinition)
		{
			Program program = new Program(ServerProcess);
			program.Start(null);
			try
			{
				EnsureLibraryLoaded(program, GetClassLibrary(classDefinition.ClassName));
			}
			finally
			{
				program.Stop(null);
			}
		}

		protected override void InsertRegisteredClasses(Schema.LoadedLibrary loadedLibrary, SettingsList registeredClasses)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertRegisteredClasses(loadedLibrary.Name, registeredClasses);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		protected override void DeleteRegisteredClasses(Schema.LoadedLibrary loadedLibrary, SettingsList registeredClasses)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRegisteredClasses(loadedLibrary.Name, registeredClasses);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion
			
		#region Updates

		protected override void InternalUpdateRow(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "System.ServerSettings": UpdateServerSettings(program, tableVar, oldRow, newRow); return;
				case "System.Sessions": UpdateSessions(program, tableVar, oldRow, newRow); return;
				case "System.Processes": UpdateProcesses(program, tableVar, oldRow, newRow); return;
			}
			LibraryUpdateRow(program, tableVar, oldRow, newRow, valueFlags);
			base.InternalUpdateRow(program, tableVar, oldRow, newRow, valueFlags);
		}

		protected void UpdateServerSettings(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			if ((bool)oldRow["LogErrors"] ^ (bool)newRow["LogErrors"])
				ServerProcess.ServerSession.Server.LogErrors = (bool)newRow["LogErrors"];

			if ((int)oldRow["MaxConcurrentProcesses"] != (int)newRow["MaxConcurrentProcesses"])
				ServerProcess.ServerSession.Server.MaxConcurrentProcesses = (int)newRow["MaxConcurrentProcesses"];

			if ((TimeSpan)oldRow["ProcessWaitTimeout"] != (TimeSpan)newRow["ProcessWaitTimeout"])
				ServerProcess.ServerSession.Server.ProcessWaitTimeout = (TimeSpan)newRow["ProcessWaitTimeout"];

			if ((TimeSpan)oldRow["ProcessTerminateTimeout"] != (TimeSpan)newRow["ProcessTerminateTimeout"])
				ServerProcess.ServerSession.Server.ProcessTerminationTimeout = (TimeSpan)newRow["ProcessTerminateTimeout"];

			if ((int)oldRow["PlanCacheSize"] != (int)newRow["PlanCacheSize"])
				ServerProcess.ServerSession.Server.PlanCacheSize = (int)newRow["PlanCacheSize"];

			SaveServerSettings(ServerProcess.ServerSession.Server);
		}

		protected void UpdateSessions(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			ServerSession session = ServerProcess.ServerSession.Server.Sessions.GetSession((int)newRow["ID"]);

			if (session.SessionID != ServerProcess.ServerSession.SessionID)
				CheckUserHasRight(ServerProcess.ServerSession.User.ID, Schema.RightNames.MaintainUserSessions);

			if ((string)oldRow["Current_Library_Name"] != (string)newRow["Current_Library_Name"])
				session.CurrentLibrary = ServerProcess.CatalogDeviceSession.ResolveLoadedLibrary((string)newRow["Current_Library_Name"]);

			if ((string)oldRow["DefaultIsolationLevel"] != (string)newRow["DefaultIsolationLevel"])
				session.SessionInfo.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)newRow["DefaultIsolationLevel"], true);

			if ((bool)oldRow["DefaultUseDTC"] ^ (bool)newRow["DefaultUseDTC"])
				session.SessionInfo.DefaultUseDTC = (bool)newRow["DefaultUseDTC"];

			if ((bool)oldRow["DefaultUseImplicitTransactions"] ^ (bool)newRow["DefaultUseImplicitTransactions"])
				session.SessionInfo.DefaultUseImplicitTransactions = (bool)newRow["DefaultUseImplicitTransactions"];

			if ((string)oldRow["Language"] != (string)newRow["Language"])
				session.SessionInfo.Language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), (string)newRow["Language"], true);

			if ((int)oldRow["DefaultMaxStackDepth"] != (int)newRow["DefaultMaxStackDepth"])
				session.SessionInfo.DefaultMaxStackDepth = (int)newRow["DefaultMaxStackDepth"];

			if ((int)oldRow["DefaultMaxCallDepth"] != (int)newRow["DefaultMaxCallDepth"])
				session.SessionInfo.DefaultMaxCallDepth = (int)newRow["DefaultMaxCallDepth"];

			if ((bool)oldRow["UsePlanCache"] ^ (bool)newRow["UsePlanCache"])
				session.SessionInfo.UsePlanCache = (bool)newRow["UsePlanCache"];

			if ((bool)oldRow["ShouldEmitIL"] ^ (bool)newRow["ShouldEmitIL"])
				session.SessionInfo.ShouldEmitIL = (bool)newRow["ShouldEmitIL"];

			if ((bool)oldRow["ShouldElaborate"] ^ (bool)newRow["ShouldElaborate"])
				session.SessionInfo.ShouldElaborate = (bool)newRow["ShouldElaborate"];
		}

		protected void UpdateProcesses(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			ServerSession session = ServerProcess.ServerSession.Server.Sessions.GetSession((int)newRow["Session_ID"]);

			if (session.SessionID != ServerProcess.ServerSession.SessionID)
				CheckUserHasRight(ServerProcess.ServerSession.User.ID, Schema.RightNames.MaintainUserSessions);

			ServerProcess process = session.Processes.GetProcess((int)newRow["ID"]);

			if ((string)oldRow["DefaultIsolationLevel"] != (string)newRow["DefaultIsolationLevel"])
				process.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)newRow["DefaultIsolationLevel"], true);

			if ((bool)oldRow["UseDTC"] ^ (bool)newRow["UseDTC"])
				process.UseDTC = (bool)newRow["UseDTC"];

			if ((bool)oldRow["UseImplicitTransactions"] ^ (bool)newRow["UseImplicitTransactions"])
				process.UseImplicitTransactions = (bool)newRow["UseImplicitTransactions"];

			if ((string)oldRow["Language"] != (string)newRow["Language"])
				process.ProcessInfo.Language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), (string)newRow["Language"], true);

			if ((int)oldRow["MaxStackDepth"] != (int)newRow["MaxStackDepth"])
				process.MaxStackDepth = (int)newRow["MaxStackDepth"];

			if ((int)oldRow["MaxCallDepth"] != (int)newRow["MaxCallDepth"])
				process.MaxCallDepth = (int)newRow["MaxCallDepth"];
		}

		#endregion
		
		#region Instructions
		
		private class CreateDeviceUserInstruction : DDLInstruction
		{
			public CreateDeviceUserInstruction(Schema.DeviceUser deviceUser) : base()
			{
				_deviceUser = deviceUser;
			}
			
			private Schema.DeviceUser _deviceUser;
			
			public override void Undo(CatalogDeviceSession session)
			{
				((ServerCatalogDeviceSession)session).ClearDeviceUser(_deviceUser);
			}
		}
		
		private class DropDeviceUserInstruction : DDLInstruction
		{
			public DropDeviceUserInstruction(Schema.DeviceUser deviceUser) : base()
			{
				_deviceUser = deviceUser;
			}
			
			private Schema.DeviceUser _deviceUser;
			
			public override void Undo(CatalogDeviceSession session)
			{
				((ServerCatalogDeviceSession)session).CacheDeviceUser(_deviceUser);
			}
		}
		
		#endregion
		
		#region Emission
		
		private static D4TextEmitter _emitter = new D4TextEmitter();
		
		public string ScriptPersistentObject(Schema.Object objectValue)
		{
			return _emitter.Emit(objectValue.EmitStatement(EmitMode.ForStorage));
		}
		
		public string ScriptCatalogObject(Schema.CatalogObject objectValue)
		{
			return _emitter.Emit(Catalog.EmitStatement(this, EmitMode.ForStorage, new string[] { objectValue.Name }, String.Empty, true, true, false, true));
		}

		#endregion
	}
}
