using System;

using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	class ServerCatalogDeviceSession : CatalogDeviceSession
	{
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
			SystemDropLibraryNode.DropLibrary(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), true);
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
			SystemSetLibraryDescriptorNode.RemoveLibraryFile(AProgram, Schema.Object.EnsureRooted((string)AOldRow[0]), new FileReference((string)AOldRow[1], (bool)AOldRow[2]));
			SystemSetLibraryDescriptorNode.AddLibraryFile(AProgram, Schema.Object.EnsureRooted((string)ANewRow[0]), new FileReference((string)ANewRow[1], (bool)ANewRow[2]));
		}

		protected void DeleteLibraryFile(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			SystemSetLibraryDescriptorNode.RemoveLibraryFile(AProgram, Schema.Object.EnsureRooted((string)ARow[0]), new FileReference((string)ARow[1], (bool)ARow[2]));
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
							SystemAttachLibraryNode.AttachLibrary(LProgram, (string)LCursor[0], (string)LCursor[1], true);
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
			}
			// TODO: This hack enables A/T style editing of a library (the requisites and files adds will automatically create a library)
			// Basically it's a deferred constraint check
			if ((ATableVar.Name == "System.Libraries") && GetTables(ATableVar.Scope)[ATableVar].HasRow(ServerProcess.ValueManager, ARow))
				return;
			base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
		}
		
		protected override void InternalUpdateRow(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "System.LibraryVersions" : UpdateLibraryVersion(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryOwners" : UpdateLibraryOwner(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.Libraries" : UpdateLibrary(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryRequisites" : UpdateLibraryRequisite(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibrarySettings" : UpdateLibrarySetting(AProgram, ATableVar, AOldRow, ANewRow); break;
				case "System.LibraryFiles" : UpdateLibraryFile(AProgram, ATableVar, AOldRow, ANewRow); break;
			}
			base.InternalUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
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
				default : throw new CatalogException(CatalogException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalDeleteRow(AProgram, ATableVar, ARow);
		}

		#endregion

		#region ObjectLoad
		
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
			if (Device.FCatalogIndex.TryGetValue(AObjectID, out LObjectName))
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

		private void LoadPersistentObject(Program AProgram, int AObjectID, Schema.User AUser, string ALibraryName, string AScript, bool AIsATObject)
		{
			// Ensure that the required library is loaded
			if ((ALibraryName != String.Empty) && !Catalog.LoadedLibraries.Contains(ALibraryName))
			{
				ServerProcess.PushLoadingContext(new LoadingContext(AUser, String.Empty));
				try
				{
					SystemLoadLibraryNode.LoadLibrary(AProgram, ALibraryName);
				}
				finally
				{
					ServerProcess.PopLoadingContext();
				}
			}

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
			Schema.CatalogObjectHeaders LResult = Device.FNameCache.Resolve(AName);

			if (LResult == null)
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					LResult = CatalogStoreConnection.ResolveCatalogObjectName(AName);
					Device.FNameCache.Add(AName, LResult);
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
			// If the name is rooted, then it is safe to search for it in the catalog cache first
			if (Schema.Object.IsRooted(AName))
			{
				int LIndex = Catalog.ResolveName(AName, APath, ANames);
				if (LIndex >= 0)
					return (Schema.CatalogObject)Catalog[LIndex];
			}

			Schema.CatalogObjectHeaders LHeaders = CachedResolveCatalogObjectName(AName);

			if (!Schema.Object.IsRooted(AName))
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

		private Schema.CatalogObjectHeaders CachedResolveOperatorName(string AName)
		{
			Schema.CatalogObjectHeaders LResult = base.CachedResolveOperatorName(AName);

			if (LResult == null)
			{
				AcquireCatalogStoreConnection(false);
				try
				{
					LResult = CatalogStoreConnection.ResolveOperatorName(AName);
					Device.FOperatorNameCache.Add(AName, LResult);
				}
				finally
				{
					ReleaseCatalogStoreConnection();
				}
			}

			return LResult;
		}

		#endregion
	}
}
