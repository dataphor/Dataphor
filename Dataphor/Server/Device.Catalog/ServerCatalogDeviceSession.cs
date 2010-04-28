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
		protected internal ServerCatalogDeviceSession(Schema.Device ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);

			DisposeCatalogStoreConnection();
		}

		public new ServerCatalogDevice Device { get { return (ServerCatalogDevice)base.Device; } }
		
		#region Execute
		
		protected override object InternalExecute(Program AProgram, Schema.DevicePlan ADevicePlan)
		{
			CatalogDevicePlan LDevicePlan = (CatalogDevicePlan)ADevicePlan;
			if (LDevicePlan.IsStorePlan)
			{
				CatalogDeviceTable LTable = new CatalogDeviceTable(LDevicePlan.Node.DeviceNode as CatalogDevicePlanNode, AProgram, this);
				try
				{
					LTable.Open();
					return LTable;
				}
				catch
				{
					LTable.Dispose();
					throw;
				}
			}
			else
				return base.InternalExecute(AProgram, ADevicePlan);
		}
		
		#endregion

		#region Catalog Store

		private bool FIsUpdatable;
		private int FAcquireCount;
		private CatalogStoreConnection FCatalogStoreConnection;

		public CatalogStoreConnection CatalogStoreConnection
		{
			get
			{
				Error.AssertFail(FCatalogStoreConnection != null, "Internal Error: No catalog store connection established.");
				return FCatalogStoreConnection;
			}
		}

		/// <summary>Requests that the session acquire a connection to the catalog store.</summary>
		/// <remarks>		
		/// If the connection is requested updatable, this will be a dedicated connection owned by the session.
		/// Otherwise, the connection is acquired from a pool of connections maintained by the store,
		/// and must be released by a call to ReleaseCatalogConnection. Calling ReleaseCatalogConnection
		/// with the dedicated updatable connection will have no affect.
		/// </remarks>
		protected void AcquireCatalogStoreConnection(bool AIsUpdatable)
		{
			FAcquireCount++;

			if (FCatalogStoreConnection == null)
			{
				if (AIsUpdatable)
					FCatalogStoreConnection = Device.Store.Connect();
				else
					FCatalogStoreConnection = Device.Store.AcquireConnection();
			}

			if (AIsUpdatable)
			{
				if (FIsUpdatable != AIsUpdatable)
				{
					FIsUpdatable = true;
					for (int LIndex = 0; LIndex < ServerProcess.TransactionCount; LIndex++)
						FCatalogStoreConnection.BeginTransaction(ServerProcess.Transactions[LIndex].IsolationLevel);
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
			FAcquireCount--;

			if ((FAcquireCount == 0) && !FIsUpdatable)
			{
				Device.Store.ReleaseConnection(FCatalogStoreConnection);
				FCatalogStoreConnection = null;
			}
		}

		private void DisposeCatalogStoreConnection()
		{
			if (FCatalogStoreConnection != null)
			{
				FCatalogStoreConnection.Dispose();
				FCatalogStoreConnection = null;
			}
		}

		public void PopulateStoreCounters(Table ATable, Row ARow)
		{
			SQLStoreCounter LCounter;
			for (int LIndex = 0; LIndex < Device.Store.Counters.Count; LIndex++)
			{
			    LCounter = Device.Store.Counters[LIndex];
			    ARow[0] = LIndex;
			    ARow[1] = LCounter.Operation;
			    ARow[2] = LCounter.TableName;
			    ARow[3] = LCounter.IndexName;
			    ARow[4] = LCounter.IsMatched;
			    ARow[5] = LCounter.IsRanged;
			    ARow[6] = LCounter.IsUpdatable;
			    ARow[7] = LCounter.Duration;
			    ATable.Insert(ARow);
			}
		}

		public void ClearStoreCounters()
		{
			Device.Store.Counters.Clear();
		}

		#endregion
		
		#region Persistence

		public void SaveServerSettings(Server.Engine AServer)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SaveServerSettings(AServer);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void LoadServerSettings(Server.Engine AServer)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				CatalogStoreConnection.LoadServerSettings(AServer);
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
				Schema.CatalogObjectHeaders LHeaders = CatalogStoreConnection.SelectBaseCatalogObjects();
				Schema.Objects LObjects = new Schema.Objects();
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					LObjects.Add(ResolveCatalogObject(LHeaders[LIndex].ID));
				return LObjects;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.CatalogObjectHeaders SelectLibraryCatalogObjects(string ALibraryName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectLibraryCatalogObjects(ALibraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.CatalogObjectHeaders SelectGeneratedObjects(int AObjectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectGeneratedObjects(AObjectID);
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

		private void InternalInsertPersistentObject(Schema.Object AObject)
		{
			CatalogStoreConnection.InsertPersistentObject(AObject, ScriptPersistentObject(AObject));
		}

		private void InsertPersistentChildren(Schema.Object AObject)
		{
			// ScalarType
			Schema.ScalarType LScalarType = AObject as Schema.ScalarType;
			if (LScalarType != null)
			{
				// Persistent representations
				for (int LIndex = 0; LIndex < LScalarType.Representations.Count; LIndex++)
					if (LScalarType.Representations[LIndex].IsPersistent)
						InternalInsertPersistentObject(LScalarType.Representations[LIndex]);

				// Persistent default
				if ((LScalarType.Default != null) && LScalarType.Default.IsPersistent)
					InternalInsertPersistentObject(LScalarType.Default);

				// Persistent constraints
				for (int LIndex = 0; LIndex < LScalarType.Constraints.Count; LIndex++)
					if (LScalarType.Constraints[LIndex].IsPersistent)
						InternalInsertPersistentObject(LScalarType.Constraints[LIndex]);

				// Persistent specials
				for (int LIndex = 0; LIndex < LScalarType.Specials.Count; LIndex++)
					if (LScalarType.Specials[LIndex].IsPersistent)
						InternalInsertPersistentObject(LScalarType.Specials[LIndex]);
			}

			// TableVar
			Schema.TableVar LTableVar = AObject as Schema.TableVar;
			if (LTableVar != null)
			{
				for (int LIndex = 0; LIndex < LTableVar.Columns.Count; LIndex++)
				{
					Schema.TableVarColumn LColumn = LTableVar.Columns[LIndex];

					// Persistent column default
					if ((LColumn.Default != null) && LColumn.Default.IsPersistent)
						InternalInsertPersistentObject(LColumn.Default);

					// Persistent column constraints
					for (int LSubIndex = 0; LSubIndex < LColumn.Constraints.Count; LSubIndex++)
						if (LColumn.Constraints[LSubIndex].IsPersistent)
							InternalInsertPersistentObject(LColumn.Constraints[LSubIndex]);
				}

				// Persistent constraints
				for (int LIndex = 0; LIndex < LTableVar.Constraints.Count; LIndex++)
					if (LTableVar.Constraints[LIndex].IsPersistent && !LTableVar.Constraints[LIndex].IsGenerated)
						InternalInsertPersistentObject(LTableVar.Constraints[LIndex]);
			}
		}

		private void InsertPersistentObject(Schema.Object AObject)
		{
			if (AObject is CatalogObject)
				Device.NameCache.Clear(AObject.Name);

			if (AObject is Operator)
				Device.OperatorNameCache.Clear(AObject.Name);

			AcquireCatalogStoreConnection(true);
			try
			{
				InternalInsertPersistentObject(AObject);
				InsertPersistentChildren(AObject);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void UpdatePersistentObject(Schema.Object AObject)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdatePersistentObject(AObject, ScriptPersistentObject(AObject));
				InsertPersistentChildren(AObject);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void UpdatePersistentObjectData(Schema.Object AObject)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdatePersistentObjectData(AObject, ScriptPersistentObject(AObject));
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void DeletePersistentObject(Schema.Object AObject)
		{
			if (AObject is Schema.CatalogObject)
				Device.NameCache.Clear(AObject.Name);

			if (AObject is Schema.Operator)
				Device.OperatorNameCache.Clear(AObject.Name);

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeletePersistentObject(AObject);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void ComputeLoadOrderForHandlers(Schema.FullObjectHeaders ALoadOrder, int AObjectID)
		{
			List<int> LHandlers = CatalogStoreConnection.SelectObjectHandlers(AObjectID);
			for (int LIndex = 0; LIndex < LHandlers.Count; LIndex++)
				ComputeLoadOrder(ALoadOrder, LHandlers[LIndex], -1);
		}

		private void ComputeLoadOrderForGeneratedObjects(Schema.FullObjectHeaders ALoadOrder, int AObjectID)
		{
			Schema.CatalogObjectHeaders LGeneratedObjects = CatalogStoreConnection.SelectGeneratedObjects(AObjectID);
			for (int LIndex = 0; LIndex < LGeneratedObjects.Count; LIndex++)
				ComputeLoadOrder(ALoadOrder, LGeneratedObjects[LIndex].ID, -1);
		}

		private void ComputeLoadOrderForImplicitConversions(Schema.FullObjectHeaders ALoadOrder, int AObjectID)
		{
			Schema.DependentObjectHeaders LDependents = CatalogStoreConnection.SelectObjectDependents(AObjectID, false);
			for (int LIndex = 0; LIndex < LDependents.Count; LIndex++)
				if (LDependents[LIndex].ObjectType == "Conversion")
					ComputeLoadOrder(ALoadOrder, LDependents[LIndex].ID, -1);
		}

		private void ComputeLoadOrderForConstraints(Schema.FullObjectHeaders ALoadOrder, int AObjectID)
		{
			Schema.DependentObjectHeaders LDependents = CatalogStoreConnection.SelectObjectDependents(AObjectID, false);
			for (int LIndex = 0; LIndex < LDependents.Count; LIndex++)
				if ((LDependents[LIndex].ObjectType == "Reference") || (LDependents[LIndex].ObjectType == "CatalogConstraint"))
					ComputeLoadOrder(ALoadOrder, LDependents[LIndex].ID, -1);
		}

		private void ComputeLoadOrderForDependencies(Schema.FullObjectHeaders ALoadOrder, int AObjectID)
		{
			Schema.DependentObjectHeaders LDependencies = CatalogStoreConnection.SelectObjectDependencies(AObjectID, false);
			for (int LIndex = 0; LIndex < LDependencies.Count; LIndex++)
				if (!LDependencies[LIndex].IsPersistent)
					ComputeLoadOrder(ALoadOrder, LDependencies[LIndex].CatalogObjectID, -1);
				else
					ComputeLoadOrder(ALoadOrder, LDependencies[LIndex].ID, LDependencies[LIndex].CatalogObjectID);
		}

		private void ComputeLoadOrder(Schema.FullObjectHeaders ALoadOrder, int AObjectID, int ACatalogObjectID)
		{
			// If this object is not already in the load order and it is not in the cache
			if
			(
				(
					((ACatalogObjectID < 0) && !Device.CatalogIndex.ContainsKey(AObjectID)) ||
					((ACatalogObjectID >= 0) && !Device.CatalogIndex.ContainsKey(ACatalogObjectID))
				) &&
				!ALoadOrder.Contains(AObjectID)
			)
			{
				// Compute the load order for all dependencies of the object
				ComputeLoadOrderForDependencies(ALoadOrder, AObjectID);

				// If this is a child object, ensure the catalog object is loaded
				if (ACatalogObjectID >= 0)
					ComputeLoadOrder(ALoadOrder, ACatalogObjectID, -1);

				if (!ALoadOrder.Contains(AObjectID))
				{
					// Load the catalog object header from the store
					Schema.FullObjectHeader LHeader = null;

					if (ACatalogObjectID < 0)
					{
						LHeader = CatalogStoreConnection.SelectCatalogObject(AObjectID);
						if (LHeader == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectHeaderNotFound, AObjectID);
					}
					else
					{
						LHeader = CatalogStoreConnection.SelectFullObject(AObjectID);
						if (LHeader == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectHeaderNotFound, AObjectID);
					}

					// Dependencies of non-persistent immediate children and subchildren
					Schema.FullObjectHeaders LChildren = CatalogStoreConnection.SelectChildObjects(AObjectID);
					for (int LIndex = 0; LIndex < LChildren.Count; LIndex++)
						if (!LChildren[LIndex].IsPersistent)
						{
							ComputeLoadOrderForDependencies(ALoadOrder, LChildren[LIndex].ID);
							Schema.FullObjectHeaders LSubChildren = CatalogStoreConnection.SelectChildObjects(LChildren[LIndex].ID);
							for (int LSubIndex = 0; LSubIndex < LSubChildren.Count; LSubIndex++)
								if (!LSubChildren[LSubIndex].IsPersistent)
									ComputeLoadOrderForDependencies(ALoadOrder, LSubChildren[LSubIndex].ID);
						}

					// Add the object to the load order
					ALoadOrder.Add(LHeader);

					// Add the objects children and necessary dependents to the load order
					switch (LHeader.ObjectType)
					{
						case "Special":
							// Generated Objects
							ComputeLoadOrderForGeneratedObjects(ALoadOrder, AObjectID);
							break;

						case "Representation":
							// Generated Objects
							ComputeLoadOrderForGeneratedObjects(ALoadOrder, AObjectID);

							for (int LIndex = 0; LIndex < LChildren.Count; LIndex++)
								ComputeLoadOrderForGeneratedObjects(ALoadOrder, LChildren[LIndex].ID);
							break;

						case "ScalarType":
							// Generated objects for non-persistent representations
							for (int LIndex = 0; LIndex < LChildren.Count; LIndex++)
							{
								if ((LChildren[LIndex].ObjectType == "Representation") && !LChildren[LIndex].IsPersistent)
								{
									// Generated Objects
									ComputeLoadOrderForGeneratedObjects(ALoadOrder, LChildren[LIndex].ID);

									Schema.FullObjectHeaders LProperties = CatalogStoreConnection.SelectChildObjects(LChildren[LIndex].ID);
									for (int LPropertyIndex = 0; LPropertyIndex < LProperties.Count; LPropertyIndex++)
										ComputeLoadOrderForGeneratedObjects(ALoadOrder, LProperties[LPropertyIndex].ID);
								}
							}

							// Generated Objects
							ComputeLoadOrderForGeneratedObjects(ALoadOrder, AObjectID);

							// Persistent representations and generated objects for them
							for (int LIndex = 0; LIndex < LChildren.Count; LIndex++)
								if ((LChildren[LIndex].ObjectType == "Representation") && LChildren[LIndex].IsPersistent)
									ComputeLoadOrder(ALoadOrder, LChildren[LIndex].ID, LChildren[LIndex].CatalogObjectID);

							// Implicit Conversions
							ComputeLoadOrderForImplicitConversions(ALoadOrder, AObjectID);

							// Default, Constraints and Specials
							for (int LIndex = 0; LIndex < LChildren.Count; LIndex++)
							{
								if (LChildren[LIndex].ObjectType != "Representation")
								{
									if (LChildren[LIndex].IsPersistent)
										ComputeLoadOrder(ALoadOrder, LChildren[LIndex].ID, LChildren[LIndex].CatalogObjectID);

									ComputeLoadOrderForGeneratedObjects(ALoadOrder, LChildren[LIndex].ID);
								}
							}

							// Sorts
							Schema.ScalarTypeHeader LScalarTypeHeader = CatalogStoreConnection.SelectScalarType(AObjectID);
							Error.AssertFail(LScalarTypeHeader != null, "Scalar type header not found for scalar type ({0})", AObjectID);

							if (LScalarTypeHeader.UniqueSortID >= 0)
								ComputeLoadOrder(ALoadOrder, LScalarTypeHeader.UniqueSortID, -1);

							if (LScalarTypeHeader.SortID >= 0)
								ComputeLoadOrder(ALoadOrder, LScalarTypeHeader.SortID, -1);

							// Handlers
							ComputeLoadOrderForHandlers(ALoadOrder, AObjectID);
							break;

						case "BaseTableVar":
						case "DerivedTableVar":
							// Immediate persistent children
							for (int LIndex = 0; LIndex < LChildren.Count; LIndex++)
							{
								if (LChildren[LIndex].IsPersistent)
									ComputeLoadOrder(ALoadOrder, LChildren[LIndex].ID, LChildren[LIndex].CatalogObjectID);

								if (LChildren[LIndex].ObjectType == "TableVarColumn")
								{
									// Defaults and Constraints
									Schema.FullObjectHeaders LColumnChildren = CatalogStoreConnection.SelectChildObjects(LChildren[LIndex].ID);
									for (int LColumnChildIndex = 0; LColumnChildIndex < LColumnChildren.Count; LColumnChildIndex++)
										if (LColumnChildren[LColumnChildIndex].IsPersistent)
											ComputeLoadOrder(ALoadOrder, LColumnChildren[LColumnChildIndex].ID, LChildren[LIndex].CatalogObjectID);

									// Handlers
									ComputeLoadOrderForHandlers(ALoadOrder, LChildren[LIndex].ID);
								}
							}

							// Constraints
							ComputeLoadOrderForConstraints(ALoadOrder, AObjectID);

							// Handlers
							ComputeLoadOrderForHandlers(ALoadOrder, AObjectID);

							break;
					}
				}
			}
		}

		public override bool CatalogObjectExists(string AObjectName)
		{
			// Search for a positive in the cache first
			Schema.CatalogObjectHeaders LResult = Device.NameCache.Resolve(AObjectName);
			if (LResult != null)
				return true;
			else
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					return CatalogStoreConnection.CatalogObjectExists(AObjectName);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		#endregion

		#region Libraries
		
		protected void InsertLibrary(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemCreateLibraryNode.CreateLibrary
			(
				AProgram,
				new Schema.Library
				(
					(string)ARow[0],
					(string)ARow[1],
					(VersionNumber)ARow[2],
					(string)ARow[3]
				),
				false,
				true
			);
		}

		protected void UpdateLibrary(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			string AOldName = (string)AOldRow[0];
			string ANewName = (string)ANewRow[0];
			VersionNumber ANewVersion = (VersionNumber)ANewRow[2];
			SystemRenameLibraryNode.RenameLibrary(AProgram, Schema.Object.EnsureRooted(AOldName), ANewName, true);
			SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProgram, Schema.Object.EnsureRooted(ANewName), ANewVersion, false);
			SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProgram, Schema.Object.EnsureRooted(ANewName), (string)ANewRow[3], false);
		}

		protected void DeleteLibrary(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			LibraryUtility.DropLibrary(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), true);
		}

		protected void InsertLibraryRequisite(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.AddLibraryRequisite(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new LibraryReference((string)ARow[1], (VersionNumber)ARow[2]));
		}

		protected void UpdateLibraryRequisite(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			if (String.Compare((string)AOldRow[0], (string)ANewRow[0]) != 0)
			{
				SystemSetLibraryDescriptorNode.RemoveLibraryRequisite(AProgram, Schema.Object.EnsureRooted((string)AOldRow[0]), new LibraryReference((string)AOldRow[1], (VersionNumber)AOldRow[2]));
				SystemSetLibraryDescriptorNode.AddLibraryRequisite(AProgram, Schema.Object.EnsureRooted((string)ANewRow[0]), new LibraryReference((string)ANewRow[1], (VersionNumber)ANewRow[2]));
			}
			else
				SystemSetLibraryDescriptorNode.UpdateLibraryRequisite
				(
					AProgram,
					Schema.Object.EnsureRooted((string)AOldRow[0]),
					new LibraryReference((string)AOldRow[1], (VersionNumber)AOldRow[2]),
					new LibraryReference((string)ANewRow[1], (VersionNumber)ANewRow[2])
				);
		}

		protected void DeleteLibraryRequisite(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryRequisite(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new LibraryReference((string)ARow[1], (VersionNumber)ARow[2]));
		}

		protected void InsertLibrarySetting(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.AddLibrarySetting(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new Tag((string)ARow[1], (string)ARow[2]));
		}

		protected void UpdateLibrarySetting(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			if (String.Compare((string)AOldRow[0], (string)ANewRow[0]) != 0)
			{
				SystemSetLibraryDescriptorNode.RemoveLibrarySetting(AProgram, Schema.Object.EnsureRooted((string)AOldRow[0]), new Tag((string)AOldRow[1], (string)AOldRow[2]));
				SystemSetLibraryDescriptorNode.AddLibrarySetting(AProgram, Schema.Object.EnsureRooted((string)ANewRow[0]), new Tag((string)ANewRow[1], (string)ANewRow[2]));
			}
			else
				SystemSetLibraryDescriptorNode.UpdateLibrarySetting
				(
					AProgram,
					Schema.Object.EnsureRooted((string)AOldRow[0]),
					new Tag((string)AOldRow[1], (string)AOldRow[2]),
					new Tag((string)ANewRow[1], (string)ANewRow[2])
				);
		}

		protected void DeleteLibrarySetting(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibrarySetting(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new Tag((string)ARow[1], (string)ARow[2]));
		}

		protected void InsertLibraryFile(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.AddLibraryFile(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new FileReference((string)ARow[1], (bool)ARow[2]));
		}
		
		protected void UpdateLibraryFile(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			if 
			(
				((string)AOldRow[0] != (string)ANewRow[0])
					|| ((string)AOldRow[1] != (string)ANewRow[1])
					|| ((bool)AOldRow[2] != (bool)ANewRow[2])
			)
			{
				SystemSetLibraryDescriptorNode.RemoveLibraryFile(AProgram, Schema.Object.EnsureRooted((string)AOldRow[0]), new FileReference((string)AOldRow[1], (bool)AOldRow[2]));
				SystemSetLibraryDescriptorNode.AddLibraryFile(AProgram, Schema.Object.EnsureRooted((string)ANewRow[0]), new FileReference((string)ANewRow[1], (bool)ANewRow[2]));
			}
		}

		protected void DeleteLibraryFile(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFile(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new FileReference((string)ARow[1], (bool)ARow[2]));
		}
		
		protected void InsertLibraryFileEnvironment(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.AddLibraryFileEnvironment(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), (string)ARow[1], (string)ARow[2]);
		}
		
		protected void UpdateLibraryFileEnvironment(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFileEnvironment(AProgram, Schema.Object.EnsureRooted((string)AOldRow[0]), (string)AOldRow[1], (string)AOldRow[2]);
			SystemSetLibraryDescriptorNode.AddLibraryFileEnvironment(AProgram, Schema.Object.EnsureRooted((string)ANewRow[0]), (string)ANewRow[1], (string)ANewRow[2]);
		}
		
		protected void DeleteLibraryFileEnvironment(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFileEnvironment(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), (string)ARow[1], (string)ARow[2]);
		}

		protected internal void SelectLibraryVersions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				using (SQLStoreCursor LCursor = CatalogStoreConnection.SelectLibraryVersions())
				{
					while (LCursor.Next())
					{
						ARow[0] = (string)LCursor[0];
						ARow[1] = VersionNumber.Parse((string)LCursor[1]);
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void InsertLibraryVersion(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertLibraryVersion((string)ARow[0], (VersionNumber)ARow[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void UpdateLibraryVersion(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				if ((string)AOldRow[0] != (string)ANewRow[0])
				{
					CatalogStoreConnection.DeleteLibraryVersion((string)AOldRow[0]);
					CatalogStoreConnection.InsertLibraryVersion((string)ANewRow[0], (VersionNumber)ANewRow[1]);
				}
				else
					CatalogStoreConnection.UpdateLibraryVersion((string)AOldRow[0], (VersionNumber)ANewRow[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void DeleteLibraryVersion(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryVersion((string)ARow[0]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected internal void SelectLibraryOwners(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				using (SQLStoreCursor LCursor = CatalogStoreConnection.SelectLibraryOwners())
				{
					while (LCursor.Next())
					{
						ARow[0] = (string)LCursor[0];
						ARow[1] = (string)LCursor[1];
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void InsertLibraryOwner(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertLibraryOwner((string)ARow[0], (string)ARow[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void UpdateLibraryOwner(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				if ((string)AOldRow[0] != (string)ANewRow[0])
				{
					CatalogStoreConnection.DeleteLibraryOwner((string)AOldRow[0]);
					CatalogStoreConnection.InsertLibraryOwner((string)ANewRow[0], (string)ANewRow[1]);
				}
				else
					CatalogStoreConnection.UpdateLibraryOwner((string)AOldRow[0], (string)ANewRow[1]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected void DeleteLibraryOwner(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryOwner((string)ARow[0]);
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
				Program LProgram = new Program(ServerProcess);
				LProgram.Start(null);
				try
				{
					using (SQLStoreCursor LCursor = CatalogStoreConnection.SelectLibraryDirectories())
					{
						while (LCursor.Next())
							LibraryUtility.AttachLibrary(LProgram, (string)LCursor[0], (string)LCursor[1], true);
					}
				}
				finally
				{
					LProgram.Stop(null);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetLibraryDirectory(string ALibraryName, string ALibraryDirectory)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SetLibraryDirectory(ALibraryName, ALibraryDirectory);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void DeleteLibraryDirectory(string ALibraryName)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryDirectory(ALibraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		protected override void InternalInsertRow(Program AProgram, Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "System.LibraryVersions" : InsertLibraryVersion(AProgram, ATableVar, ARow); break;
				case "System.LibraryOwners" : InsertLibraryOwner(AProgram, ATableVar, ARow); break;
				case "System.Libraries" : InsertLibrary(AProgram, ATableVar, ARow); break;
				case "System.LibraryRequisites" : InsertLibraryRequisite(AProgram, ATableVar, ARow); break;
				case "System.LibrarySettings" : InsertLibrarySetting(AProgram, ATableVar, ARow); break;
				case "System.LibraryFiles" : InsertLibraryFile(AProgram, ATableVar, ARow); break;
				case "System.LibraryFileEnvironments" : InsertLibraryFileEnvironment(AProgram, ATableVar, ARow); break;
			}
			// TODO: This hack enables A/T style editing of a library (the requisites and files adds will automatically create a library)
			// Basically it's a deferred constraint check
			if ((ATableVar.Name == "System.Libraries") && GetTables(ATableVar.Scope)[ATableVar].HasRow(ServerProcess.ValueManager, ARow))
				return;
			base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
		}
		
		private void LibraryUpdateRow(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "System.LibraryVersions": UpdateLibraryVersion(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryOwners" : UpdateLibraryOwner(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.Libraries" : UpdateLibrary(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryRequisites" : UpdateLibraryRequisite(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibrarySettings" : UpdateLibrarySetting(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryFiles" : UpdateLibraryFile(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryFileEnvironments" : UpdateLibraryFileEnvironment(AProgram, ATableVar, AOldRow, ANewRow); break;
			}
		}
		
		protected override void InternalDeleteRow(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			switch (ATableVar.Name)
			{
				case "System.LibraryVersions" : DeleteLibraryVersion(AProgram, ATableVar, ARow); break;
				case "System.LibraryOwners" : DeleteLibraryOwner(AProgram, ATableVar, ARow); break;
				case "System.Libraries" : DeleteLibrary(AProgram, ATableVar, ARow); break;
				case "System.LibraryRequisites" : DeleteLibraryRequisite(AProgram, ATableVar, ARow); break;
				case "System.LibrarySettings" : DeleteLibrarySetting(AProgram, ATableVar, ARow); break;
				case "System.LibraryFiles" : DeleteLibraryFile(AProgram, ATableVar, ARow); break;
				case "System.LibraryFileEnvironments" : DeleteLibraryFileEnvironment(AProgram, ATableVar, ARow); break;
				default : throw new CatalogException(CatalogException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalDeleteRow(AProgram, ATableVar, ARow);
		}

		protected override void InternalInsertLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			// If this is not a repository, and we are not deserializing, insert the loaded library in the catalog store
			if (!ServerProcess.InLoadingContext())
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.InsertLoadedLibrary(ALoadedLibrary.Name);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		protected override void InternalDeleteLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLoadedLibrary(ALoadedLibrary.Name);
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
				foreach (string LLibraryName in CatalogStoreConnection.SelectLoadedLibraries())
					ResolveLoadedLibrary(LLibraryName);
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
						Program LProgram = new Program(ServerProcess);
						LProgram.Start(null);
						try
						{
							LibraryUtility.LoadLibrary(LProgram, LLibrary.Name);
							return Catalog.LoadedLibraries[LLibrary.Name];
						}
						finally
						{
							LProgram.Stop(null);
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

		public string GetLibraryOwner(string ALibraryName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string LOwnerID = CatalogStoreConnection.SelectLibraryOwner(ALibraryName);
				return LOwnerID == null ? Server.Engine.CAdminUserID : LOwnerID;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void ClearTableCache(string ATableName)
		{
			int LIndex = Catalog.IndexOfName(ATableName);
			if (LIndex >= 0)
			{
				lock (Device.Headers)
				{
					Device.Headers[(Schema.TableVar)Catalog[LIndex]].Cached = false;
				}
			}
		}
		
		public void SetLibraryOwner(string ALibraryName, string AUserID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				string LOwnerID = CatalogStoreConnection.SelectLibraryOwner(ALibraryName);
				if ((LOwnerID == null) || (LOwnerID != AUserID))
				{
					ClearTableCache("System.LibraryOwners");
					if (LOwnerID == null)
						CatalogStoreConnection.InsertLibraryOwner(ALibraryName, AUserID);
					else
						CatalogStoreConnection.UpdateLibraryOwner(ALibraryName, AUserID);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void ClearLibraryOwner(string ALibraryName)
		{
			ClearTableCache("System.LibraryOwners");
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryOwner(ALibraryName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public VersionNumber GetCurrentLibraryVersion(string ALibraryName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string LVersion = CatalogStoreConnection.SelectLibraryVersion(ALibraryName);
				return LVersion == null ? Catalog.Libraries[ALibraryName].Version : VersionNumber.Parse(LVersion);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetCurrentLibraryVersion(string ALibraryName, VersionNumber AVersionNumber)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				string LVersion = CatalogStoreConnection.SelectLibraryVersion(ALibraryName);
				if ((LVersion == null) || !VersionNumber.Parse(LVersion).Equals(AVersionNumber))
				{
					ClearTableCache("System.LibraryVersions");
					if (LVersion == null)
						CatalogStoreConnection.InsertLibraryVersion(ALibraryName, AVersionNumber);
					else
						CatalogStoreConnection.UpdateLibraryVersion(ALibraryName, AVersionNumber);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void ClearCurrentLibraryVersion(string ALibraryName)
		{
			ClearTableCache("System.LibraryVersions");
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteLibraryVersion(ALibraryName);
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
				case Schema.DataTypes.CSystemScalar: Catalog.DataTypes.SystemScalar = LScalarType; break;
				case Schema.DataTypes.CSystemBoolean: Catalog.DataTypes.SystemBoolean = LScalarType; LScalarType.NativeType = typeof(bool); break;
				case Schema.DataTypes.CSystemDecimal: Catalog.DataTypes.SystemDecimal = LScalarType; LScalarType.NativeType = typeof(decimal); break;
				case Schema.DataTypes.CSystemLong: Catalog.DataTypes.SystemLong = LScalarType; LScalarType.NativeType = typeof(long); break;
				case Schema.DataTypes.CSystemInteger: Catalog.DataTypes.SystemInteger = LScalarType; LScalarType.NativeType = typeof(int); break;
				case Schema.DataTypes.CSystemShort: Catalog.DataTypes.SystemShort = LScalarType; LScalarType.NativeType = typeof(short); break;
				case Schema.DataTypes.CSystemByte: Catalog.DataTypes.SystemByte = LScalarType; LScalarType.NativeType = typeof(byte); break;
				case Schema.DataTypes.CSystemString: Catalog.DataTypes.SystemString = LScalarType; LScalarType.NativeType = typeof(string); break;
				case Schema.DataTypes.CSystemTimeSpan: Catalog.DataTypes.SystemTimeSpan = LScalarType; LScalarType.NativeType = typeof(TimeSpan); break;
				case Schema.DataTypes.CSystemDateTime: Catalog.DataTypes.SystemDateTime = LScalarType; LScalarType.NativeType = typeof(DateTime); break;
				case Schema.DataTypes.CSystemDate: Catalog.DataTypes.SystemDate = LScalarType; LScalarType.NativeType = typeof(DateTime); break;
				case Schema.DataTypes.CSystemTime: Catalog.DataTypes.SystemTime = LScalarType; LScalarType.NativeType = typeof(DateTime); break;
				case Schema.DataTypes.CSystemMoney: Catalog.DataTypes.SystemMoney = LScalarType; LScalarType.NativeType = typeof(decimal); break;
				case Schema.DataTypes.CSystemGuid: Catalog.DataTypes.SystemGuid = LScalarType; LScalarType.NativeType = typeof(Guid); break;
				case Schema.DataTypes.CSystemBinary: Catalog.DataTypes.SystemBinary = LScalarType; LScalarType.NativeType = typeof(byte[]); break;
				case Schema.DataTypes.CSystemGraphic: Catalog.DataTypes.SystemGraphic = LScalarType; LScalarType.NativeType = typeof(byte[]); break;
				case Schema.DataTypes.CSystemError: Catalog.DataTypes.SystemError = LScalarType; LScalarType.NativeType = typeof(Exception); break;
				case Schema.DataTypes.CSystemName: Catalog.DataTypes.SystemName = LScalarType; LScalarType.NativeType = typeof(string); break;
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
		private Schema.CatalogObject LoadCatalogObject(int AObjectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Program LProgram = new Program(ServerProcess);
				LProgram.Start(null);
				try
				{
					Schema.CatalogObject LResult = null;
					Schema.Objects LScalarTypes = new Schema.Objects();
					Schema.FullObjectHeaders LLoadOrder = new Schema.FullObjectHeaders();
					ComputeLoadOrder(LLoadOrder, AObjectID, -1);

					Schema.FullObjectHeader LObjectHeader;
					Schema.FullCatalogObjectHeader LCatalogObjectHeader;
					for (int LIndex = 0; LIndex < LLoadOrder.Count; LIndex++)
					{
						LObjectHeader = LLoadOrder[LIndex];
						LCatalogObjectHeader = LObjectHeader as Schema.FullCatalogObjectHeader;
						if (LCatalogObjectHeader != null)
						{
							Schema.CatalogObject LObject = LoadCatalogObject(LProgram, LObjectHeader.ID, ResolveUser(LCatalogObjectHeader.OwnerID), LObjectHeader.LibraryName, LObjectHeader.Script, LObjectHeader.IsGenerated, LObjectHeader.IsATObject);
							if (LObject is Schema.ScalarType)
								LScalarTypes.Add(LObject);
							if (LCatalogObjectHeader.ID == AObjectID)
								LResult = LObject;
						}
						else
							LoadPersistentObject(LProgram, LObjectHeader.ID, ResolveCachedCatalogObject(LObjectHeader.CatalogObjectID, true).Owner, LObjectHeader.LibraryName, LObjectHeader.Script, LObjectHeader.IsATObject);
					}

					// Once all the objects have loaded, fixup pointers to generated objects
					for (int LIndex = 0; LIndex < LScalarTypes.Count; LIndex++)
						((Schema.ScalarType)LScalarTypes[LIndex]).ResolveGeneratedDependents(this);

					if (LResult != null)
						return LResult;

					throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectLoadFailed, ErrorSeverity.System, AObjectID);
				}
				finally
				{
					LProgram.Stop(null);
				}
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private Schema.CatalogObject LoadCatalogObject(Program AProgram, int AObjectID, Schema.User AUser, string ALibraryName, string AScript, bool AIsGenerated, bool AIsATObject)
		{
			// Load the object itself
			LoadPersistentObject(AProgram, AObjectID, AUser, ALibraryName, AScript, AIsATObject);

			string LObjectName;
			if (Device.CatalogIndex.TryGetValue(AObjectID, out LObjectName))
			{
				Schema.CatalogObject LResult = (Schema.CatalogObject)Catalog[LObjectName];
				if (AIsGenerated)
				{
					LResult.IsGenerated = true;
					LResult.LoadGeneratorID();
				}

				Schema.ScalarType LScalarType = LResult as Schema.ScalarType;
				if ((LScalarType != null) && LScalarType.IsSystem)
					FixupSystemScalarTypeReferences(LScalarType);

				Schema.Device LDevice = LResult as Schema.Device;
				if (LDevice != null)
				{
					if (!LDevice.Registered && (HasDeviceObjects(LDevice) || (LDevice is MemoryDevice)))
						LDevice.SetRegistered();

					if (LDevice.Registered)
					{
						ServerProcess.PushLoadingContext(new LoadingContext(AUser, ALibraryName));
						try
						{
							// The device must be started within a loading context so that system object maps are not re-created
							ServerProcess.ServerSession.Server.StartDevice(ServerProcess, LDevice);
						}
						finally
						{
							ServerProcess.PopLoadingContext();
						}
					}
				}

				return LResult;
			}

			throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectLoadFailed, ErrorSeverity.System, AObjectID);
		}
		
		private void EnsureLibraryLoaded(Program AProgram, string ALibraryName)
		{
			EnsureLibraryLoaded(AProgram, ALibraryName, ResolveUser(GetLibraryOwner(ALibraryName)));
		}

		private void EnsureLibraryLoaded(Program AProgram, string ALibraryName, Schema.User AUser)
		{
			// Ensure that the required library is loaded
			if ((ALibraryName != String.Empty) && !Catalog.LoadedLibraries.Contains(ALibraryName))
			{
				ServerProcess.PushLoadingContext(new LoadingContext(AUser, String.Empty));
				try
				{
					LibraryUtility.LoadLibrary(AProgram, ALibraryName);
				}
				finally
				{
					ServerProcess.PopLoadingContext();
				}
			}
		}
		
		private void LoadPersistentObject(Program AProgram, int AObjectID, Schema.User AUser, string ALibraryName, string AScript, bool AIsATObject)
		{
			// Ensure that the required library is loaded
			EnsureLibraryLoaded(AProgram, ALibraryName, AUser);

			// Compile and execute the object creation script
			ServerProcess.PushLoadingContext(new LoadingContext(AUser, ALibraryName));
			try
			{
				ApplicationTransaction.ApplicationTransaction LAT = null;
				if (!AIsATObject && (ServerProcess.ApplicationTransactionID != Guid.Empty))
				{
					LAT = ServerProcess.GetApplicationTransaction();
					LAT.PushGlobalContext();
				}
				try
				{
					ParserMessages LParserMessages = new ParserMessages();
					Statement LStatement = new Parser().ParseScript(AScript, LParserMessages);
					Plan LPlan = new Plan(ServerProcess);
					try
					{
						LPlan.PushSourceContext(new Debug.SourceContext(AScript, null));
						try
						{
							//LPlan.PlanCatalog.AddRange(LCurrentPlan.PlanCatalog); // add the set of objects currently being compiled
							LPlan.Messages.AddRange(LParserMessages);
							LPlan.PushSecurityContext(new SecurityContext(AUser));
							try
							{
								PlanNode LPlanNode = null;
								try
								{
									LPlanNode = Compiler.Bind(LPlan, Compiler.CompileStatement(LPlan, LStatement));
								}
								finally
								{
									//LCurrentPlan.Messages.AddRange(LPlan.Messages); // Propagate compiler exceptions to the outer plan
								}
								try
								{
									LPlan.CheckCompiled();
									LPlanNode.Execute(AProgram);
								}
								catch (Exception E)
								{
									throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogDeserializationError, ErrorSeverity.System, E, AObjectID);
								}
							}
							finally
							{
								LPlan.PopSecurityContext();
							}
						}
						finally
						{
							LPlan.PopSourceContext();
						}
					}
					finally
					{
						LPlan.Dispose();
					}
				}
				finally
				{
					if (LAT != null)
					{
						LAT.PopGlobalContext();
						Monitor.Exit(LAT);
					}
				}
			}
			finally
			{
				ServerProcess.PopLoadingContext();
			}
		}

		private Schema.CatalogObjectHeaders CachedResolveCatalogObjectName(string AName)
		{
			Schema.CatalogObjectHeaders LResult = Device.NameCache.Resolve(AName);

			if (LResult == null)
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					LResult = CatalogStoreConnection.ResolveCatalogObjectName(AName);
					Device.NameCache.Add(AName, LResult);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}

			return LResult;
		}

		/// <summary>Resolves the given name and returns the catalog object, if an unambiguous match is found. Otherwise, returns null.</summary>
		public override Schema.CatalogObject ResolveName(string AName, NameResolutionPath APath, List<string> ANames)
		{
			bool LRooted = Schema.Object.IsRooted(AName);
			
			// If the name is rooted, then it is safe to search for it in the catalog cache first
			if (LRooted)
			{
				int LIndex = Catalog.ResolveName(AName, APath, ANames);
				if (LIndex >= 0)
					return (Schema.CatalogObject)Catalog[LIndex];
			}

			Schema.CatalogObjectHeaders LHeaders = CachedResolveCatalogObjectName(AName);

			if (!LRooted)
			{
				Schema.CatalogObjectHeaders LLevelHeaders = new Schema.CatalogObjectHeaders();

				for (int LLevelIndex = 0; LLevelIndex < APath.Count; LLevelIndex++)
				{
					if (LLevelIndex > 0)
						LLevelHeaders.Clear();

					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
						if ((LHeaders[LIndex].LibraryName == String.Empty) || APath[LLevelIndex].ContainsName(LHeaders[LIndex].LibraryName))
							LLevelHeaders.Add(LHeaders[LIndex]);

					if (LLevelHeaders.Count > 0)
					{
						for (int LIndex = 0; LIndex < LLevelHeaders.Count; LIndex++)
							ANames.Add(LLevelHeaders[LIndex].Name);

						return LLevelHeaders.Count == 1 ? ResolveCatalogObject(LLevelHeaders[0].ID) : null;
					}
				}
			}

			// Only resolve objects in loaded libraries
			Schema.CatalogObjectHeader LHeader = null;
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if ((LHeaders[LIndex].LibraryName == String.Empty) || Catalog.LoadedLibraries.Contains(LHeaders[LIndex].LibraryName))
				{
					ANames.Add(LHeaders[LIndex].Name);
					if (LHeader == null)
						LHeader = LHeaders[LIndex];
					else
						LHeader = null;
				}
			}

			if ((ANames.Count == 1) && (LHeader != null))
				return ResolveCatalogObject(LHeader.ID);

			// If there is still no resolution, and there is one header, resolve the library and resolve to that name
			if ((LHeaders.Count == 1) && (ResolveLoadedLibrary(LHeaders[0].LibraryName, false) != null))
			{
				ANames.Add(LHeaders[0].Name);
				return ResolveCatalogObject(LHeaders[0].ID);
			}

			return null;
		}

		protected override Schema.CatalogObjectHeaders CachedResolveOperatorName(string AName)
		{
			Schema.CatalogObjectHeaders LResult = base.CachedResolveOperatorName(AName);

			if (LResult == null)
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					LResult = CatalogStoreConnection.ResolveOperatorName(AName);
					Device.OperatorNameCache.Add(AName, LResult);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}

			return LResult;
		}

		/// <summary>Resolves the catalog object with the given id. If the object is not found, an error is raised.</summary>
		/// <remarks>
		/// This routine first searches for the object in the catalog cache. If it is not found, it's header is retrieved from
		/// the catalog store, and the object is deserialized using that information. After this routine returns, the object
		/// will be present in the catalog cache.
		/// </remarks>
		public override Schema.CatalogObject ResolveCatalogObject(int AObjectID)
		{
			// TODO: Catalog deserialization concurrency
			// Right now, use the same lock as the user's cache to ensure no deadlocks can occur during deserialization.
			// This effectively places deserialization granularity at the server level, but until we
			// can provide a solution to the deserialization concurrency deadlock problem, this is all we can do.
			lock (Catalog)
			{
				// Lookup the object in the catalog index
				Schema.CatalogObject LResult = base.ResolveCatalogObject(AObjectID);
				if (LResult == null)
				{
					if (!ServerProcess.InLoadingContext())
						return LoadCatalogObject(AObjectID);

					// It is an error to attempt to resolve an object that would need to be loaded while we are loading. These
					// dependencies will always be loaded by the LoadCatalogObject call.
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectAlreadyLoading, ErrorSeverity.System, AObjectID);
				}
				else
					return LResult;
			}
		}

		public Schema.ObjectHeader SelectObjectHeader(int AObjectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObject(AObjectID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.ObjectHeader GetObjectHeader(int AObjectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Schema.ObjectHeader LHeader = CatalogStoreConnection.SelectObject(AObjectID);
				if (LHeader == null)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectHeaderNotFound, AObjectID);

				return LHeader;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion

		#region Catalog Object Update

		protected override void InternalInsertCatalogObject(Schema.CatalogObject AObject)
		{
			// If this is not a repository, and we are not deserializing, and the object should be persisted, save the object to the catalog store
			if (ShouldSerializeCatalogObject(AObject))
				InsertPersistentObject(AObject);
		}

		protected override void InternalUpdateCatalogObject(Schema.CatalogObject AObject)
		{
			// If this is not a repository, and we are not deserializing, and the object should be persisted, update the object in the catalog store
			if (ShouldSerializeCatalogObject(AObject))
				UpdatePersistentObject(AObject);
		}

		protected override void InternalDeleteCatalogObject(Schema.CatalogObject AObject)
		{
			// If this is not a repository, and the object should be persisted, remove the object from the catalog store
			if (ShouldSerializeCatalogObject(AObject))
				DeletePersistentObject(AObject);
		}

		#endregion
		
		#region Transaction
		
		protected override void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			base.InternalBeginTransaction(AIsolationLevel);

			if ((FCatalogStoreConnection != null) && FIsUpdatable)
				FCatalogStoreConnection.BeginTransaction(AIsolationLevel);
		}

		protected override void InternalAfterCommitTransaction()
		{
			base.InternalAfterCommitTransaction();
			
			if ((FCatalogStoreConnection != null) && FIsUpdatable)
				FCatalogStoreConnection.CommitTransaction();
		}

		protected override void InternalAfterRollbackTransaction()
		{
			base.InternalAfterRollbackTransaction();
			
			if ((FCatalogStoreConnection != null) && FIsUpdatable)
				FCatalogStoreConnection.RollbackTransaction();
		}
		
		#endregion

		#region Object Selection

		public override List<int> SelectOperatorHandlers(int AOperatorID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectOperatorHandlers(AOperatorID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override List<int> SelectObjectHandlers(int ASourceObjectID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObjectHandlers(ASourceObjectID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

		}

		public override Schema.DependentObjectHeaders SelectObjectDependents(int AObjectID, bool ARecursive)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObjectDependents(AObjectID, ARecursive);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.DependentObjectHeaders SelectObjectDependencies(int AObjectID, bool ARecursive)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.SelectObjectDependencies(AObjectID, ARecursive);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion

		#region Security

		public override Right ResolveRight(string ARightName, bool AMustResolve)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				Right LRight = CatalogStoreConnection.SelectRight(ARightName);
				if ((LRight == null) && AMustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.RightNotFound, ARightName);
				return LRight;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override void InsertRight(string ARightName, string AUserID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertRight(ARightName, AUserID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override void DeleteRight(string ARightName)
		{
			base.DeleteRight(ARightName);

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRight(ARightName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected override void InternalInsertRole(Schema.Role ARole)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertRole(ARole, ScriptCatalogObject(ARole));
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected override void InternalDeleteRole(Schema.Role ARole)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRole(ARole);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public bool RoleHasRight(Schema.Role ARole, string ARightName)
		{
			// TODO: Implement role right assignments caching
			AcquireCatalogStoreConnection(false);
			try
			{
				RightAssignment LRightAssignment = CatalogStoreConnection.SelectRoleRightAssignment(ARole.ID, ARightName);
				return (LRightAssignment != null) && LRightAssignment.Granted;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		protected override Schema.User InternalResolveUser(string AUserID, Schema.User LUser)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				LUser = CatalogStoreConnection.SelectUser(AUserID);
				if (LUser != null)
					InternalCacheUser(LUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
			return LUser;
		}
		
		public bool UserExists(string AUserID)
		{
			return ResolveUser(AUserID, false) != null;
		}

		public override void InsertUser(Schema.User AUser)
		{
			base.InsertUser(AUser);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new CreateUserInstruction(AUser));
			#endif

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.InsertUser(AUser);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public void SetUserPassword(string AUserID, string APassword)
		{
			Schema.User LUser = null;

			lock (Catalog)
			{
				LUser = ResolveUser(AUserID);

				#if LOGDDLINSTRUCTIONS
				string LUserPassword = LUser.Password;
				#endif
				LUser.Password = APassword;
				#if LOGDDLINSTRUCTIONS
				if (ServerProcess.InTransaction)
					FInstructions.Add(new SetUserPasswordInstruction(LUser, LUserPassword));
				#endif
			}

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.UpdateUser(LUser);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public void SetUserName(string AUserID, string AUserName)
		{
			Schema.User LUser = null;

			lock (Catalog)
			{
				LUser = ResolveUser(AUserID);

				#if LOGDDLINSTRUCTIONS
				string LUserName = LUser.Name;
				#endif
				LUser.Name = AUserName;
				#if LOGDDLINSTRUCTIONS
				if (ServerProcess.InTransaction)
					FInstructions.Add(new SetUserNameInstruction(LUser, LUserName));
				#endif
			}

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.UpdateUser(LUser);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public void DeleteUser(Schema.User AUser)
		{
			ClearUser(AUser.ID);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new DropUserInstruction(AUser));
			#endif

			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				AcquireCatalogStoreConnection(true);
				try
				{
					CatalogStoreConnection.DeleteUser(AUser.ID);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}
		}

		public bool UserOwnsObjects(string AUserID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.UserOwnsObjects(AUserID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public bool UserOwnsRights(string AUserID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.UserOwnsRights(AUserID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override bool UserHasRight(string AUserID, string ARightName)
		{
			if (ServerProcess.IsLoading() || (String.Compare(AUserID, Server.Engine.CSystemUserID, true) == 0) || (String.Compare(AUserID, Server.Engine.CAdminUserID, true) == 0))
				return true;

			lock (Catalog)
			{
				Schema.User LUser = ResolveUser(AUserID);

				Schema.RightAssignment LRightAssignment = LUser.FindCachedRightAssignment(ARightName);

				if (LRightAssignment == null)
				{
					AcquireCatalogStoreConnection(false);
					try
					{
						LRightAssignment = new Schema.RightAssignment(ARightName, CatalogStoreConnection.UserHasRight(AUserID, ARightName));
						LUser.CacheRightAssignment(LRightAssignment);
					}
					finally
					{
						ReleaseCatalogStoreConnection();
					}
				}

				return LRightAssignment.Granted;
			}
		}

		public void InsertUserRole(string AUserID, int ARoleID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertUserRole(AUserID, ARoleID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
			ClearUserCachedRightAssignments(AUserID);
		}

		public void DeleteUserRole(string AUserID, int ARoleID)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteUserRole(AUserID, ARoleID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			ClearUserCachedRightAssignments(AUserID);

			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(AUserID);
		}

		private void MarkRoleOperatorsForRecompile(int ARoleID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				List<String> LUsers = CatalogStoreConnection.SelectRoleUsers(ARoleID);
				for (int LIndex = 0; LIndex < LUsers.Count; LIndex++)
					MarkUserOperatorsForRecompile(LUsers[LIndex]);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		private void MarkUserOperatorsForRecompile(string AUserID)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string LObjectName;
				Schema.CatalogObjectHeaders LHeaders = CatalogStoreConnection.SelectUserOperators(AUserID);
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					if (Device.CatalogIndex.TryGetValue(LHeaders[LIndex].ID, out LObjectName))
						((Schema.Operator)Catalog[LObjectName]).ShouldRecompile = true;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void GrantRightToRole(string ARightName, int ARoleID)
		{
			lock (Catalog)
			{
				foreach (Schema.User LUser in Device.UsersCache.Values)
					LUser.ClearCachedRightAssignment(ARightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureRoleRightAssignment(ARoleID, ARightName, true);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			// Mark operators for each user that is a member of this role to be recompiled on next execution
			if (!ServerProcess.IsLoading())
				MarkRoleOperatorsForRecompile(ARoleID);
		}

		public void GrantRightToUser(string ARightName, string AUserID)
		{
			lock (Catalog)
			{
				Schema.User LUser;
				if (Device.UsersCache.TryGetValue(AUserID, out LUser))
					LUser.ClearCachedRightAssignment(ARightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureUserRightAssignment(AUserID, ARightName, true);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			// Mark operators for this user to be recompiled on next execution
			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(AUserID);
		}

		public void RevokeRightFromRole(string ARightName, int ARoleID)
		{
			lock (Catalog)
			{
				foreach (Schema.User LUser in Device.UsersCache.Values)
					LUser.ClearCachedRightAssignment(ARightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureRoleRightAssignment(ARoleID, ARightName, false);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkRoleOperatorsForRecompile(ARoleID);
		}

		public void RevokeRightFromUser(string ARightName, string AUserID)
		{
			lock (Catalog)
			{
				Schema.User LUser;
				if (Device.UsersCache.TryGetValue(AUserID, out LUser))
					LUser.ClearCachedRightAssignment(ARightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.EnsureUserRightAssignment(AUserID, ARightName, false);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(AUserID);
		}

		public void RevertRightForRole(string ARightName, int ARoleID)
		{
			lock (Catalog)
			{
				foreach (Schema.User LUser in Device.UsersCache.Values)
					LUser.ClearCachedRightAssignment(ARightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRoleRightAssignment(ARoleID, ARightName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkRoleOperatorsForRecompile(ARoleID);
		}

		public void RevertRightForUser(string ARightName, string AUserID)
		{
			lock (Catalog)
			{
				Schema.User LUser;
				if (Device.UsersCache.TryGetValue(AUserID, out LUser))
					LUser.ClearCachedRightAssignment(ARightName);
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteUserRightAssignment(AUserID, ARightName);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			if (!ServerProcess.IsLoading())
				MarkUserOperatorsForRecompile(AUserID);
		}

		public void SetCatalogObjectOwner(int ACatalogObjectID, string AUserID)
		{
			lock (Catalog)
			{
				Schema.User LUser;
				if (Device.UsersCache.TryGetValue(AUserID, out LUser))
					LUser.ClearCachedRightAssignments();
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.SetCatalogObjectOwner(ACatalogObjectID, AUserID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}

			// TODO: If the object is an operator, and we are not loading, and the object is not generated, and the object is in the cache, mark it for recompile
			// TODO: If we are not loading, for each immediate dependent of this object that is a non-generated operator currently in the cache, mark it for recompile
		}

		public void SetRightOwner(string ARightName, string AUserID)
		{
			lock (Catalog)
			{
				Schema.User LUser;
				if (Device.UsersCache.TryGetValue(AUserID, out LUser))
					LUser.ClearCachedRightAssignments();
			}

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateRight(ARightName, AUserID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion		
		
		#region Device Users
		
		public override Schema.DeviceUser ResolveDeviceUser(Schema.Device ADevice, Schema.User AUser, bool AMustResolve)
		{
			lock (ADevice.Users)
			{
				Schema.DeviceUser LDeviceUser;
				if (!ADevice.Users.TryGetValue(AUser.ID, out LDeviceUser))
				{
					AcquireCatalogStoreConnection(false);
					try
					{
						LDeviceUser = CatalogStoreConnection.SelectDeviceUser(ADevice, AUser);
						if (LDeviceUser != null)
							ADevice.Users.Add(LDeviceUser);
					}
					finally
					{
						ReleaseCatalogStoreConnection();
					}
				}

				if ((LDeviceUser == null) && AMustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DeviceUserNotFound, AUser.ID);

				return LDeviceUser;
			}
		}

		private void CacheDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			lock (ADeviceUser.Device.Users)
			{
				ADeviceUser.Device.Users.Add(ADeviceUser);
			}
		}

		private void ClearDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			lock (ADeviceUser.Device.Users)
			{
				ADeviceUser.Device.Users.Remove(ADeviceUser.User.ID);
			}
		}

		public void InsertDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			CacheDeviceUser(ADeviceUser);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new CreateDeviceUserInstruction(ADeviceUser));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertDeviceUser(ADeviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetDeviceUserID(DeviceUser ADeviceUser, string AUserID)
		{
			#if LOGDDLINSTRUCTIONS
			string LOriginalDeviceUserID = ADeviceUser.DeviceUserID;
			#endif
			ADeviceUser.DeviceUserID = AUserID;
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new SetDeviceUserIDInstruction(ADeviceUser, LOriginalDeviceUserID));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateDeviceUser(ADeviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetDeviceUserPassword(DeviceUser ADeviceUser, string APassword)
		{
			#if LOGDDLINSTRUCTIONS
			string LOriginalDevicePassword = ADeviceUser.DevicePassword;
			#endif
			ADeviceUser.DevicePassword = APassword;
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new SetDeviceUserPasswordInstruction(ADeviceUser, LOriginalDevicePassword));
				#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateDeviceUser(ADeviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void SetDeviceUserConnectionParameters(DeviceUser ADeviceUser, string AConnectionParameters)
		{
			#if LOGDDLINSTRUCTIONS
			string LOriginalConnectionParameters = ADeviceUser.ConnectionParameters;
			#endif
			ADeviceUser.ConnectionParameters = AConnectionParameters;
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new SetDeviceUserConnectionParametersInstruction(ADeviceUser, LOriginalConnectionParameters));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.UpdateDeviceUser(ADeviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void DeleteDeviceUser(Schema.DeviceUser ADeviceUser)
		{
			ClearDeviceUser(ADeviceUser);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new DropDeviceUserInstruction(ADeviceUser));
			#endif

			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteDeviceUser(ADeviceUser);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
				
		#endregion

		#region Device Objects

		public override bool HasDeviceObjects(Schema.Device ADevice)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				return CatalogStoreConnection.HasDeviceObjects(ADevice.ID);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public override Schema.DeviceObject ResolveDeviceObject(Schema.Device ADevice, Schema.Object AObject)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				int LDeviceObjectID = CatalogStoreConnection.SelectDeviceObjectID(ADevice.ID, AObject.ID);
				if (LDeviceObjectID >= 0)
				{
					// If we are already loading, then a resolve that must load from the cache will fail, 
					// and is a dependency on an object mapping that did not exist when the initial object was created.
					// Therefore if the object is not present in the cache, it is as though the object does not exist.
					if (ServerProcess.InLoadingContext())
						return ResolveCachedCatalogObject(LDeviceObjectID, false) as Schema.DeviceObject;
					return ResolveCatalogObject(LDeviceObjectID) as Schema.DeviceObject;
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
		
		public string GetClassLibrary(string AClassName)
		{
			AcquireCatalogStoreConnection(false);
			try
			{
				string LLibraryName = CatalogStoreConnection.SelectClassLibrary(AClassName);
				if (String.IsNullOrEmpty(LLibraryName))
					throw new ServerException(ServerException.Codes.ClassAliasNotFound, AClassName);
				return LLibraryName;
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		public void LoadLibraryForClass(ClassDefinition AClassDefinition)
		{
			Program LProgram = new Program(ServerProcess);
			LProgram.Start(null);
			try
			{
				EnsureLibraryLoaded(LProgram, GetClassLibrary(AClassDefinition.ClassName));
			}
			finally
			{
				LProgram.Stop(null);
			}
		}

		protected override void InsertRegisteredClasses(Schema.LoadedLibrary ALoadedLibrary, SettingsList ARegisteredClasses)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.InsertRegisteredClasses(ALoadedLibrary.Name, ARegisteredClasses);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}
		
		protected override void DeleteRegisteredClasses(Schema.LoadedLibrary ALoadedLibrary, SettingsList ARegisteredClasses)
		{
			AcquireCatalogStoreConnection(true);
			try
			{
				CatalogStoreConnection.DeleteRegisteredClasses(ALoadedLibrary.Name, ARegisteredClasses);
			}
			finally
			{
				ReleaseCatalogStoreConnection();
			}
		}

		#endregion
			
		#region Updates

		protected override void InternalUpdateRow(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "System.ServerSettings": UpdateServerSettings(AProgram, ATableVar, AOldRow, ANewRow); return;
				case "System.Sessions": UpdateSessions(AProgram, ATableVar, AOldRow, ANewRow); return;
				case "System.Processes": UpdateProcesses(AProgram, ATableVar, AOldRow, ANewRow); return;
			}
			LibraryUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
			base.InternalUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
		}

		protected void UpdateServerSettings(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			if ((bool)AOldRow["LogErrors"] ^ (bool)ANewRow["LogErrors"])
				ServerProcess.ServerSession.Server.LogErrors = (bool)ANewRow["LogErrors"];

			if ((int)AOldRow["MaxConcurrentProcesses"] != (int)ANewRow["MaxConcurrentProcesses"])
				ServerProcess.ServerSession.Server.MaxConcurrentProcesses = (int)ANewRow["MaxConcurrentProcesses"];

			if ((TimeSpan)AOldRow["ProcessWaitTimeout"] != (TimeSpan)ANewRow["ProcessWaitTimeout"])
				ServerProcess.ServerSession.Server.ProcessWaitTimeout = (TimeSpan)ANewRow["ProcessWaitTimeout"];

			if ((TimeSpan)AOldRow["ProcessTerminateTimeout"] != (TimeSpan)ANewRow["ProcessTerminateTimeout"])
				ServerProcess.ServerSession.Server.ProcessTerminationTimeout = (TimeSpan)ANewRow["ProcessTerminateTimeout"];

			if ((int)AOldRow["PlanCacheSize"] != (int)ANewRow["PlanCacheSize"])
				ServerProcess.ServerSession.Server.PlanCacheSize = (int)ANewRow["PlanCacheSize"];

			SaveServerSettings(ServerProcess.ServerSession.Server);
		}

		protected void UpdateSessions(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			ServerSession LSession = ServerProcess.ServerSession.Server.Sessions.GetSession((int)ANewRow["ID"]);

			if (LSession.SessionID != ServerProcess.ServerSession.SessionID)
				CheckUserHasRight(ServerProcess.ServerSession.User.ID, Schema.RightNames.MaintainUserSessions);

			if ((string)AOldRow["Current_Library_Name"] != (string)ANewRow["Current_Library_Name"])
				LSession.CurrentLibrary = ServerProcess.CatalogDeviceSession.ResolveLoadedLibrary((string)ANewRow["Current_Library_Name"]);

			if ((string)AOldRow["DefaultIsolationLevel"] != (string)ANewRow["DefaultIsolationLevel"])
				LSession.SessionInfo.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)ANewRow["DefaultIsolationLevel"], true);

			if ((bool)AOldRow["DefaultUseDTC"] ^ (bool)ANewRow["DefaultUseDTC"])
				LSession.SessionInfo.DefaultUseDTC = (bool)ANewRow["DefaultUseDTC"];

			if ((bool)AOldRow["DefaultUseImplicitTransactions"] ^ (bool)ANewRow["DefaultUseImplicitTransactions"])
				LSession.SessionInfo.DefaultUseImplicitTransactions = (bool)ANewRow["DefaultUseImplicitTransactions"];

			if ((string)AOldRow["Language"] != (string)ANewRow["Language"])
				LSession.SessionInfo.Language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), (string)ANewRow["Language"], true);

			if ((int)AOldRow["DefaultMaxStackDepth"] != (int)ANewRow["DefaultMaxStackDepth"])
				LSession.SessionInfo.DefaultMaxStackDepth = (int)ANewRow["DefaultMaxStackDepth"];

			if ((int)AOldRow["DefaultMaxCallDepth"] != (int)ANewRow["DefaultMaxCallDepth"])
				LSession.SessionInfo.DefaultMaxCallDepth = (int)ANewRow["DefaultMaxCallDepth"];

			if ((bool)AOldRow["UsePlanCache"] ^ (bool)ANewRow["UsePlanCache"])
				LSession.SessionInfo.UsePlanCache = (bool)ANewRow["UsePlanCache"];

			if ((bool)AOldRow["ShouldEmitIL"] ^ (bool)ANewRow["ShouldEmitIL"])
				LSession.SessionInfo.ShouldEmitIL = (bool)ANewRow["ShouldEmitIL"];
		}

		protected void UpdateProcesses(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			ServerSession LSession = ServerProcess.ServerSession.Server.Sessions.GetSession((int)ANewRow["Session_ID"]);

			if (LSession.SessionID != ServerProcess.ServerSession.SessionID)
				CheckUserHasRight(ServerProcess.ServerSession.User.ID, Schema.RightNames.MaintainUserSessions);

			ServerProcess LProcess = LSession.Processes.GetProcess((int)ANewRow["ID"]);

			if ((string)AOldRow["DefaultIsolationLevel"] != (string)ANewRow["DefaultIsolationLevel"])
				LProcess.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)ANewRow["DefaultIsolationLevel"], true);

			if ((bool)AOldRow["UseDTC"] ^ (bool)ANewRow["UseDTC"])
				LProcess.UseDTC = (bool)ANewRow["UseDTC"];

			if ((bool)AOldRow["UseImplicitTransactions"] ^ (bool)ANewRow["UseImplicitTransactions"])
				LProcess.UseImplicitTransactions = (bool)ANewRow["UseImplicitTransactions"];

			if ((int)AOldRow["MaxStackDepth"] != (int)ANewRow["MaxStackDepth"])
				LProcess.MaxStackDepth = (int)ANewRow["MaxStackDepth"];

			if ((int)AOldRow["MaxCallDepth"] != (int)ANewRow["MaxCallDepth"])
				LProcess.MaxCallDepth = (int)ANewRow["MaxCallDepth"];
		}

		#endregion
		
		#region Instructions
		
		private class CreateDeviceUserInstruction : DDLInstruction
		{
			public CreateDeviceUserInstruction(Schema.DeviceUser ADeviceUser) : base()
			{
				FDeviceUser = ADeviceUser;
			}
			
			private Schema.DeviceUser FDeviceUser;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				((ServerCatalogDeviceSession)ASession).ClearDeviceUser(FDeviceUser);
			}
		}
		
		private class DropDeviceUserInstruction : DDLInstruction
		{
			public DropDeviceUserInstruction(Schema.DeviceUser ADeviceUser) : base()
			{
				FDeviceUser = ADeviceUser;
			}
			
			private Schema.DeviceUser FDeviceUser;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				((ServerCatalogDeviceSession)ASession).CacheDeviceUser(FDeviceUser);
			}
		}
		
		#endregion
		
		#region Emission
		
		private static D4TextEmitter FEmitter = new D4TextEmitter();
		
		public string ScriptPersistentObject(Schema.Object AObject)
		{
			return FEmitter.Emit(AObject.EmitStatement(EmitMode.ForStorage));
		}
		
		public string ScriptCatalogObject(Schema.CatalogObject AObject)
		{
			return FEmitter.Emit(Catalog.EmitStatement(this, EmitMode.ForStorage, new string[] { AObject.Name }, String.Empty, true, true, false, true));
		}

		#endregion
	}
}
