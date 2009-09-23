using System;
using System.IO;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Windows;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Device.Catalog;
using System.Reflection;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Schema
{
	public static class LibraryUtility
	{
		public const string CRegisterFileName = @"Documents\Register.d4";
		public const string CRegisterDocumentLocator = @"doc:{0}:Register";

		public static string GetInstanceLibraryDirectory(string AInstanceDirectory, string ALibraryName)
		{
			string LResult = Path.Combine(Path.Combine(AInstanceDirectory, Server.Server.CDefaultLibraryDataDirectory), ALibraryName);
			System.IO.Directory.CreateDirectory(LResult);
			return LResult;
		}
		
		public static Library LoadFromFile(string AFileName, string AInstanceDirectory)
		{
			using (FileStream LStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
			{
				Library LLibrary = LoadFromStream(LStream);
				LLibrary.Name = Path.GetFileNameWithoutExtension(AFileName);
				LLibrary.LoadInfoFromFile(Path.Combine(GetInstanceLibraryDirectory(AInstanceDirectory, LLibrary.Name), GetInfoFileName(LLibrary.Name)));
				return LLibrary;
			}
		}

		public static string GetInfoFileName(string ALibraryName)
		{
			return String.Format("{0}.d4l.info", ALibraryName);
		}
		
		public static Library LoadFromStream(Stream AStream)
		{
			return new Deserializer().Deserialize(AStream, null) as Library;
		}
		
		public static string GetFileName(string ALibraryName)
		{
			return String.Format("{0}.d4l", ALibraryName);
		}
		
		public static string GetLibraryDirectory(string AServerLibraryDirectory, string ALibraryName, string ALibraryDirectory)
		{
			return ALibraryDirectory == String.Empty ? Path.Combine(GetDefaultLibraryDirectory(AServerLibraryDirectory), ALibraryName) : ALibraryDirectory;
		}
		
		public static string GetDefaultLibraryDirectory(string AServerLibraryDirectory)
		{
			return AServerLibraryDirectory.Split(';')[0];
		}

		public static void GetAvailableLibraries(string AInstanceDirectory, string ALibraryDirectory, Libraries ALibraries)
		{
			ALibraries.Clear();
			string[] LDirectories = ALibraryDirectory.Split(';');
			for (int LIndex = 0; LIndex < LDirectories.Length; LIndex++)
				GetAvailableLibraries(AInstanceDirectory, LDirectories[LIndex], ALibraries, LIndex > 0);
		}
		
		public static void GetAvailableLibraries(string AInstanceDirectory, string ALibraryDirectory, Libraries ALibraries, bool ASetLibraryDirectory)
		{
			string LLibraryName;
			string LLibraryFileName;
			string[] LLibraries = System.IO.Directory.GetDirectories(ALibraryDirectory);
			for (int LIndex = 0; LIndex < LLibraries.Length; LIndex++)
			{
				LLibraryName = Path.GetFileName(LLibraries[LIndex]);
				LLibraryFileName = Path.Combine(LLibraries[LIndex], GetFileName(LLibraryName));
				if (File.Exists(LLibraryFileName))
				{
					Schema.Library LLibrary = LoadFromFile(LLibraryFileName, AInstanceDirectory);
					if (ASetLibraryDirectory)
						LLibrary.Directory = LLibraries[LIndex];
					ALibraries.Add(LLibrary);
				}
			}
		}
		
		public static Schema.Library GetAvailableLibrary(string AInstanceDirectory, string ALibraryName, string ALibraryDirectory)
		{
			string LLibraryFileName = Path.Combine(ALibraryDirectory, GetFileName(ALibraryName));
			if (File.Exists(LLibraryFileName))
			{
				Schema.Library LLibrary = LoadFromFile(LLibraryFileName, AInstanceDirectory);
				LLibrary.Directory = ALibraryDirectory;
				return LLibrary;
			}
			return null;
		}

		public static string GetLibraryDirectory(this Library ALibrary, string AServerLibraryDirectory)
		{
			return GetLibraryDirectory(AServerLibraryDirectory, ALibrary.Name, ALibrary.Directory);
		}
		
		public static string GetInstanceLibraryDirectory(this Library ALibrary, string AInstanceDirectory)
		{
			return GetInstanceLibraryDirectory(AInstanceDirectory, ALibrary.Name);
		}
		
		public static void SaveToFile(this Library ALibrary, string AFileName)
		{
			#if !RESPECTREADONLY
			FileUtility.EnsureWriteable(AFileName);
			#endif
			using (FileStream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				ALibrary.SaveToStream(LStream);
			}
		}
		
		public static void SaveInfoToFile(this Library ALibrary, string AFileName)
		{
			FileUtility.EnsureWriteable(AFileName);
			using (FileStream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				new LibraryInfo(ALibrary.Name, ALibrary.IsSuspect, ALibrary.SuspectReason).SaveToStream(LStream);
			}
		}
		
		public static void LoadInfoFromFile(this Library ALibrary, string AFileName)
		{
			if (File.Exists(AFileName))
			{
				using (FileStream LStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
				{
					LibraryInfo LLibraryInfo = LibraryInfo.LoadFromStream(LStream);
					ALibrary.IsSuspect = LLibraryInfo.IsSuspect;
					ALibrary.SuspectReason = LLibraryInfo.SuspectReason;
				}
			}
		}

		private static void EnsureLibraryUnregistered(Program AProgram, Schema.Library ALibrary, bool AWithReconciliation)
		{
			Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibrary.Name, false);
			if (LLoadedLibrary != null)
			{
				while (LLoadedLibrary.RequiredByLibraries.Count > 0)
					EnsureLibraryUnregistered(AProgram, AProgram.Catalog.Libraries[LLoadedLibrary.RequiredByLibraries[0].Name], AWithReconciliation);
				UnregisterLibrary(AProgram, ALibrary.Name, AWithReconciliation);
			}
		}
		
		private static void RemoveLibrary(Program AProgram, Schema.Library ALibrary)
		{
			// Ensure that the library and any dependencies of it are unregistered
			EnsureLibraryUnregistered(AProgram, ALibrary, false);
			DetachLibrary(AProgram, ALibrary.Name);
		}
		
		public static void RefreshLibraries(Program AProgram)
		{
			// Get the list of available libraries from the library directory
			Schema.Libraries LLibraries = new Schema.Libraries();
			Schema.Libraries LOldLibraries = new Schema.Libraries();
			Schema.Library.GetAvailableLibraries(((Server.Server)AProgram.ServerProcess.ServerSession.Server).InstanceDirectory, AProgram.ServerProcess.ServerSession.Server.LibraryDirectory, LLibraries);
			
			lock (AProgram.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library LLibrary in LLibraries)
					if (!AProgram.Catalog.Libraries.ContainsName(LLibrary.Name) && !AProgram.Catalog.ContainsName(LLibrary.Name))
						AttachLibrary(AProgram, LLibrary, false);
						
				// Ensure that each library in the DAE is supported by a library in the directory, remove non-existent libraries
				// The System library is not required to have a directory.
				foreach (Schema.Library LLibrary in AProgram.Catalog.Libraries)
					if ((Engine.CSystemLibraryName != LLibrary.Name) && (LLibrary.Directory == String.Empty) && !LLibraries.ContainsName(LLibrary.Name))
						LOldLibraries.Add(LLibrary);
						
				foreach (Schema.Library LLibrary in LOldLibraries)
					RemoveLibrary(AProgram, LLibrary);
			}
		}

		public static void UnregisterLibrary(Program AProgram, string ALibraryName, bool AWithReconciliation)
		{
			int LSaveReconciliationState = AProgram.ServerProcess.SuspendReconciliationState();
			try
			{
				if (!AWithReconciliation)
					AProgram.ServerProcess.DisableReconciliation();
				try
				{
					lock (AProgram.Catalog.Libraries)
					{
						Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName);
						
						if (Schema.Object.NamesEqual(LLoadedLibrary.Name, Engine.CSystemLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterSystemLibrary);
							
						if (Schema.Object.NamesEqual(LLoadedLibrary.Name, Engine.CGeneralLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterGeneralLibrary);

						if (LLoadedLibrary.RequiredByLibraries.Count > 0)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryIsRequired, LLoadedLibrary.Name);
							
						// Drop all the objects in the library
						AProgram.ServerProcess.ServerSession.Server.RunScript
						(
							AProgram.ServerProcess, 
							AProgram.ServerProcess.ServerSession.Server.ScriptDropLibrary(AProgram.CatalogDeviceSession, ALibraryName), 
							LLoadedLibrary.Name,
							null
						);
						
						// Any session with current library set to the unregistered library will be set to General
						AProgram.ServerProcess.ServerSession.Server.LibraryUnloaded(LLoadedLibrary.Name);

						// Remove the library from the catalog
						AProgram.CatalogDeviceSession.DeleteLoadedLibrary(LLoadedLibrary);
						LLoadedLibrary.DetachLibrary();
						
						// Unregister each assembly that was loaded with this library
						foreach (Assembly LAssembly in LLoadedLibrary.Assemblies)
							AProgram.Catalog.ClassLoader.UnregisterAssembly(LAssembly);

						// TODO: Unregister assemblies when the .NET framework supports it
						
						AProgram.Catalog.Libraries.DoLibraryUnloaded(AProgram, LLoadedLibrary.Name);
					}
				}
				finally
				{
					if (!AWithReconciliation)
						AProgram.ServerProcess.EnableReconciliation();
				}
			}
			finally
			{
				AProgram.ServerProcess.ResumeReconciliationState(LSaveReconciliationState);
			}
		}

		public static void DropLibrary(Program AProgram, string ALibraryName, bool AUpdateCatalogTimeStamp)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDropRegisteredLibrary, LLibrary.Name);
					
				string LLibraryDirectory = LLibrary.GetLibraryDirectory(((Server.Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);

				AProgram.Catalog.Libraries.DoLibraryRemoved(AProgram, LLibrary.Name);
				AProgram.Catalog.Libraries.DoLibraryDeleted(AProgram, LLibrary.Name);
				try
				{
					AProgram.Catalog.Libraries.Remove(LLibrary);
					try
					{
						if (AUpdateCatalogTimeStamp)
							AProgram.Catalog.UpdateTimeStamp();
						if (Directory.Exists(LLibraryDirectory))
						{
							#if !RESPECTREADONLY
							PathUtility.EnsureWriteable(LLibraryDirectory, true);
							#endif
							Directory.Delete(LLibraryDirectory, true);
						}
						
						((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).ClearLibraryOwner(ALibraryName);
						((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).ClearCurrentLibraryVersion(ALibraryName);
					}
					catch
					{
						if (Directory.Exists(LLibraryDirectory))
							AProgram.Catalog.Libraries.Add(LLibrary);
						throw;
					}
				}
				catch
				{
					if (Directory.Exists(LLibraryDirectory))
					{
						AProgram.Catalog.Libraries.DoLibraryCreated(AProgram, LLibrary.Name);
						AProgram.Catalog.Libraries.DoLibraryAdded(AProgram, LLibrary.Name);
					}
					throw;
				}
			}
		}
		
		public static void DetachLibrary(Program AProgram, string ALibraryName)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDetachRegisteredLibrary, LLibrary.Name);
					
				string LLibraryDirectory = LLibrary.GetLibraryDirectory(((Server.Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);

				AProgram.Catalog.Libraries.DoLibraryRemoved(AProgram, LLibrary.Name);
				AProgram.Catalog.Libraries.Remove(LLibrary);
				AProgram.Catalog.UpdateTimeStamp();
				((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).DeleteLibraryDirectory(LLibrary.Name);
			}
		}

		/// <summary>Attaches the library given by ALibraryName from ALibraryDirectory. AIsAttached indicates whether this library is being attached as part of catalog startup.</summary>
		public static void AttachLibrary(Program AProgram, string ALibraryName, string ALibraryDirectory, bool AIsAttached)
		{
			Schema.Library LLibrary = Schema.Library.GetAvailableLibrary(((Server.Server)AProgram.ServerProcess.ServerSession.Server).InstanceDirectory, ALibraryName, ALibraryDirectory);
			if ((LLibrary != null) && !AProgram.Catalog.Libraries.ContainsName(LLibrary.Name))
				AttachLibrary(AProgram, LLibrary, AIsAttached);
		}

		public static void EnsureLibraryRegistered(Program AProgram, string ALibraryName, bool AWithReconciliation)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName, false) == null)
					LibraryUtility.RegisterLibrary(AProgram, ALibraryName, AWithReconciliation);
			}
		}
		
		public static void EnsureLibraryRegistered(Program AProgram, Schema.LibraryReference ALibraryReference, bool AWithReconciliation)
		{
			Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryReference.Name, false);
			if (LLoadedLibrary == null)
			{
				Schema.LoadedLibrary LCurrentLibrary = AProgram.ServerProcess.ServerSession.CurrentLibrary;
				try
				{
					Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryReference.Name];
					if (!VersionNumber.Compatible(ALibraryReference.Version, LLibrary.Version))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, ALibraryReference.Name, ALibraryReference.Version.ToString(), LLibrary.Version.ToString());
					RegisterLibrary(AProgram, LLibrary.Name, AWithReconciliation);
				}
				finally
				{
					AProgram.ServerProcess.ServerSession.CurrentLibrary = LCurrentLibrary;
				}
			}
			else
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryReference.Name];
				if (!VersionNumber.Compatible(ALibraryReference.Version, LLibrary.Version))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, ALibraryReference.Name, ALibraryReference.Version.ToString(), LLibrary.Version.ToString());
			}
		}
		
		public static void RegisterLibraryFiles(Program AProgram, Schema.Library ALibrary, Schema.LoadedLibrary ALoadedLibrary)
		{
			// Register each assembly with the DAE
			#if !LOADFROMLIBRARIES
			foreach (Schema.FileReference LFile in ALibrary.Files)
			{
				string LSourceFile = Path.IsPathRooted(LFile.FileName) ? LFile.FileName : Path.Combine(ALibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), LFile.FileName);
				string LTargetFile = Path.Combine(PathUtility.GetBinDirectory(), Path.GetFileName(LFile.FileName));
				
				if (!File.Exists(LSourceFile))
					throw new System.IO.IOException(String.Format("File \"{0}\" not found.", LSourceFile));
				try
				{
					#if !RESPECTREADONLY
					FileUtility.EnsureWriteable(LTargetFile);
					#endif
                    if ((File.GetLastWriteTimeUtc(LSourceFile) > File.GetLastWriteTimeUtc(LTargetFile))) // source newer than target
                    {
                        File.Copy(LSourceFile, LTargetFile, true);
                    }
				}
				catch (IOException)
				{
					// Ignore this exception so that assembly copying does not fail if the assembly is already loaded
				}
			}
			#endif
			
			// Load assemblies after all files are copied in so that multi-file assemblies and other dependencies are certain to be present
			foreach (Schema.FileReference LFile in ALibrary.Files)
			{
				if (LFile.IsAssembly)
				{
					#if LOADFROMLIBRARIES
					string LSourceFile = Path.IsPathRooted(LFile.FileName) ? LFile.FileName : Path.Combine(ALibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), LFile.FileName);
                    Assembly LAssembly = Assembly.LoadFrom(LSourceFile);
					#else
                    string LTargetFile = Path.Combine(PathUtility.GetBinDirectory(), Path.GetFileName(LFile.FileName));
                    Assembly LAssembly = Assembly.LoadFrom(LTargetFile);
                    #endif
                    AProgram.CatalogDeviceSession.RegisterAssembly(ALoadedLibrary, LAssembly);
				}
			}
		}
		
		public static void UnregisterLibraryAssemblies(Program AProgram, Schema.LoadedLibrary ALoadedLibrary)
		{
			while (ALoadedLibrary.Assemblies.Count > 0)
				AProgram.CatalogDeviceSession.UnregisterAssembly(ALoadedLibrary, ALoadedLibrary.Assemblies[ALoadedLibrary.Assemblies.Count - 1] as Assembly);
		}
		
		public static void CheckCircularLibraryReference(Program AProgram, Schema.Library ALibrary, string ARequiredLibraryName)
		{
			if (IsCircularLibraryReference(AProgram, ALibrary, ARequiredLibraryName))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.CircularLibraryReference, ALibrary.Name, ARequiredLibraryName);
		}
		
		public static bool IsCircularLibraryReference(Program AProgram, Schema.Library ALibrary, string ARequiredLibraryName)
		{
			Schema.Library LRequiredLibrary = AProgram.Catalog.Libraries[ARequiredLibraryName];
			if (Schema.Object.NamesEqual(ALibrary.Name, ARequiredLibraryName))
				return true;
				
			foreach (Schema.LibraryReference LReference in LRequiredLibrary.Libraries)
				if (IsCircularLibraryReference(AProgram, ALibrary, LReference.Name))
					return true;
					
			return false;
		}
		
		public static void RegisterLibrary(Program AProgram, string ALibraryName, bool AWithReconciliation)
		{
			int LSaveReconciliationState = AProgram.ServerProcess.SuspendReconciliationState();
			try
			{
				if (!AWithReconciliation)
					AProgram.ServerProcess.DisableReconciliation();
				try
				{
					lock (AProgram.Catalog.Libraries)
					{
						Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
						
						Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
						if (LLoadedLibrary != null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryAlreadyRegistered, ALibraryName);
							
						LLoadedLibrary = new Schema.LoadedLibrary(ALibraryName);
						LLoadedLibrary.Owner = AProgram.Plan.User;
							
						//	Ensure that each required library is registered
						foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
						{
							CheckCircularLibraryReference(AProgram, LLibrary, LReference.Name);
							EnsureLibraryRegistered(AProgram, LReference, AWithReconciliation);
							LLoadedLibrary.RequiredLibraries.Add(AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
							AProgram.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProgram.ServerProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
						
						((Server.Server)AProgram.ServerProcess.ServerSession.Server).DoLibraryLoading(LLibrary.Name);
						try
						{
							// Register the assemblies
							RegisterLibraryFiles(AProgram, LLibrary, LLoadedLibrary);
							AProgram.CatalogDeviceSession.InsertLoadedLibrary(LLoadedLibrary);
							LLoadedLibrary.AttachLibrary();
							try
							{
								// Set the current library to the newly registered library
								Schema.LoadedLibrary LCurrentLibrary = AProgram.Plan.CurrentLibrary;
								AProgram.ServerProcess.ServerSession.CurrentLibrary = LLoadedLibrary;
								try
								{
									//	run the register.d4 script if it exists in the library
									//		catalog objects created in this script are part of this library
									string LRegisterFileName = Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), CRegisterFileName);
									if (File.Exists(LRegisterFileName))
									{
										try
										{
											using (StreamReader LReader = new StreamReader(LRegisterFileName))
											{
												AProgram.ServerProcess.ServerSession.Server.RunScript
												(
													AProgram.ServerProcess, 
													LReader.ReadToEnd(), 
													ALibraryName, 
													new DAE.Debug.DebugLocator(String.Format(CRegisterDocumentLocator, ALibraryName), 1, 1)
												);
											}
										}
										catch (Exception LException)
										{
											throw new RuntimeException(RuntimeException.Codes.LibraryRegistrationFailed, LException, ALibraryName);
										}
									}

									((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetCurrentLibraryVersion(LLibrary.Name, LLibrary.Version);
									((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetLibraryOwner(LLoadedLibrary.Name, LLoadedLibrary.Owner.ID);
									if (LLibrary.IsSuspect)
									{
										LLibrary.IsSuspect = false;
										LLibrary.SaveInfoToFile(Path.Combine(LLibrary.GetInstanceLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.InstanceDirectory), Schema.Library.GetInfoFileName(LLibrary.Name)));
									}					
								}
								catch
								{
									AProgram.ServerProcess.ServerSession.CurrentLibrary = LCurrentLibrary;
									throw;
								}
							}
							catch
							{
								LLoadedLibrary.DetachLibrary();
								throw;
							}
							AProgram.Catalog.Libraries.DoLibraryLoaded(AProgram, LLibrary.Name);
						}
						finally
						{
							((Server.Server)AProgram.ServerProcess.ServerSession.Server).DoLibraryLoaded(ALibraryName);
						}
					}
				}
				finally
				{
					if (!AWithReconciliation)
						AProgram.ServerProcess.EnableReconciliation();
				}
			}
			finally
			{
				AProgram.ServerProcess.ResumeReconciliationState(LSaveReconciliationState);
			}
		}

		public static void LoadLibrary(Program AProgram, string ALibraryName)
		{
			LoadLibrary(AProgram, ALibraryName, false);
		}
		
		private static void LoadLibrary(Program AProgram, string ALibraryName, bool AIsKnown)
		{
			lock (AProgram.Catalog.Libraries)
			{
				try
				{
					Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
					VersionNumber LCurrentVersion = ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetCurrentLibraryVersion(ALibraryName);
					
					if (AProgram.Catalog.LoadedLibraries.Contains(LLibrary.Name))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryAlreadyLoaded, ALibraryName);

					bool LIsLoaded = false;				
					bool LAreAssembliesRegistered = false;	
					Schema.LoadedLibrary LLoadedLibrary = null;
					try
					{
						LLoadedLibrary = new Schema.LoadedLibrary(ALibraryName);
						LLoadedLibrary.Owner = AProgram.CatalogDeviceSession.ResolveUser(AProgram.CatalogDeviceSession.GetLibraryOwner(ALibraryName));
							
						//	Ensure that each required library is loaded
						foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
						{
							Schema.Library LRequiredLibrary = AProgram.Catalog.Libraries[LReference.Name];
							if (!VersionNumber.Compatible(LReference.Version, LRequiredLibrary.Version))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, LReference.Name, LReference.Version.ToString(), LRequiredLibrary.Version.ToString());

							if (!AProgram.Catalog.LoadedLibraries.Contains(LReference.Name))
							{
								if (!LRequiredLibrary.IsSuspect)
									LoadLibrary(AProgram, LReference.Name, AIsKnown);
								else
									throw new Schema.SchemaException(Schema.SchemaException.Codes.RequiredLibraryNotLoaded, ALibraryName, LReference.Name);
							}
							
							LLoadedLibrary.RequiredLibraries.Add(AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
							AProgram.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProgram.ServerProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
						
						AProgram.ServerProcess.ServerSession.Server.DoLibraryLoading(LLibrary.Name);
						try
						{
							// RegisterAssemblies
							RegisterLibraryFiles(AProgram, LLibrary, LLoadedLibrary);
							
							LAreAssembliesRegistered = true;

							AProgram.CatalogDeviceSession.InsertLoadedLibrary(LLoadedLibrary);
							LLoadedLibrary.AttachLibrary();
							try
							{
								((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetLibraryOwner(LLoadedLibrary.Name, LLoadedLibrary.Owner.ID);
							}
							catch (Exception LRegisterException)
							{
								LLoadedLibrary.DetachLibrary();
								throw LRegisterException;
							}

							LIsLoaded = true; // If we reach this point, a subsequent exception must unload the library
							if (LLibrary.IsSuspect)
							{
								LLibrary.IsSuspect = false;
								LLibrary.SaveInfoToFile(Path.Combine(LLibrary.GetInstanceLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.InstanceDirectory), Schema.Library.GetInfoFileName(LLibrary.Name)));
							}					
						}
						finally
						{
							AProgram.ServerProcess.ServerSession.Server.DoLibraryLoaded(LLibrary.Name);
						}
					}
					catch (Exception LException)
					{
						AProgram.ServerProcess.ServerSession.Server.LogError(LException);
						LLibrary.IsSuspect = true;
						LLibrary.SuspectReason = ExceptionUtility.DetailedDescription(LException);
						LLibrary.SaveInfoToFile(Path.Combine(LLibrary.GetInstanceLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.InstanceDirectory), Schema.Library.GetInfoFileName(LLibrary.Name)));
						
						if (LIsLoaded)
							UnregisterLibrary(AProgram, ALibraryName, false);
						else if (LAreAssembliesRegistered)
							UnregisterLibraryAssemblies(AProgram, LLoadedLibrary);
							
						throw;
					}

					((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetCurrentLibraryVersion(LLibrary.Name, LCurrentVersion); // Once a library has loaded, record the version number
					
					AProgram.Catalog.Libraries.DoLibraryLoaded(AProgram, LLibrary.Name);
				}
				catch
				{
					if (AProgram.ServerProcess.ServerSession.Server.State == ServerState.Started)
						throw;
				}
			}
		}
	}
}
