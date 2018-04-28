/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define LOADFROMLIBRARIES

using System;
using System.IO;
using System.Reflection;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Windows;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Schema
{
	public static class LibraryUtility
	{
		public const string RegisterFileName = @"Documents\Register.d4";
		public const string RegisterDocumentLocator = @"doc:{0}:Register";

		public static string GetInstanceLibraryDirectory(string instanceDirectory, string libraryName)
		{
			string result = Path.Combine(Path.Combine(instanceDirectory, Server.Server.DefaultLibraryDataDirectory), libraryName);
			System.IO.Directory.CreateDirectory(result);
			return result;
		}
		
		public static Library LoadFromFile(string fileName, string instanceDirectory)
		{
			using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				Library library = LoadFromStream(stream);
				library.Name = Path.GetFileNameWithoutExtension(fileName);
				library.LoadInfoFromFile(Path.Combine(GetInstanceLibraryDirectory(instanceDirectory, library.Name), GetInfoFileName(library.Name)));
				return library;
			}
		}

		public static string GetInfoFileName(string libraryName)
		{
			return String.Format("{0}.d4l.info", libraryName);
		}
		
		public static Library LoadFromStream(Stream stream)
		{
			return new Deserializer().Deserialize(stream, null) as Library;
		}
		
		public static string GetFileName(string libraryName)
		{
			return String.Format("{0}.d4l", libraryName);
		}
		
		public static string GetLibraryDirectory(string serverLibraryDirectory, string libraryName, string libraryDirectory)
		{
			return libraryDirectory == String.Empty ? Path.Combine(GetDefaultLibraryDirectory(serverLibraryDirectory), libraryName) : libraryDirectory;
		}
		
		public static string GetDefaultLibraryDirectory(string serverLibraryDirectory)
		{
			return serverLibraryDirectory.Split(';')[0];
		}

		public static void GetAvailableLibraries(string instanceDirectory, string libraryDirectory, Libraries libraries)
		{
			libraries.Clear();
			string[] directories = libraryDirectory.Split(';');
			for (int index = 0; index < directories.Length; index++)
				GetAvailableLibraries(instanceDirectory, directories[index], libraries, index > 0);
		}
		
		public static void GetAvailableLibraries(string instanceDirectory, string libraryDirectory, Libraries libraries, bool setLibraryDirectory)
		{
			string libraryName;
			string libraryFileName;
			string[] localLibraries = System.IO.Directory.GetDirectories(libraryDirectory);
			for (int index = 0; index < localLibraries.Length; index++)
			{
				libraryName = Path.GetFileName(localLibraries[index]);
				libraryFileName = Path.Combine(localLibraries[index], GetFileName(libraryName));
				if (File.Exists(libraryFileName))
				{
					Schema.Library library = LoadFromFile(libraryFileName, instanceDirectory);
					if (setLibraryDirectory)
						library.Directory = localLibraries[index];
					libraries.Add(library);
				}
			}
		}
		
		public static Schema.Library GetAvailableLibrary(string instanceDirectory, string libraryName, string libraryDirectory)
		{
			string libraryFileName = Path.Combine(libraryDirectory, GetFileName(libraryName));
			if (File.Exists(libraryFileName))
			{
				Schema.Library library = LoadFromFile(libraryFileName, instanceDirectory);
				library.Directory = libraryDirectory;
				return library;
			}
			return null;
		}

		public static string GetLibraryDirectory(this Library library, string serverLibraryDirectory)
		{
			return GetLibraryDirectory(serverLibraryDirectory, library.Name, library.Directory);
		}
		
		public static string GetInstanceLibraryDirectory(this Library library, string instanceDirectory)
		{
			return GetInstanceLibraryDirectory(instanceDirectory, library.Name);
		}
		
		public static void SaveToFile(this Library library, string fileName)
		{
			#if !RESPECTREADONLY
			FileUtility.EnsureWriteable(fileName);
			#endif
			using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				library.SaveToStream(stream);
			}
		}
		
		public static void SaveInfoToFile(this Library library, string fileName)
		{
			FileUtility.EnsureWriteable(fileName);
			using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				new LibraryInfo(library.Name, library.IsSuspect, library.SuspectReason).SaveToStream(stream);
			}
		}
		
		public static void LoadInfoFromFile(this Library library, string fileName)
		{
			if (File.Exists(fileName))
			{
				using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					LibraryInfo libraryInfo = LibraryInfo.LoadFromStream(stream);
					library.IsSuspect = libraryInfo.IsSuspect;
					library.SuspectReason = libraryInfo.SuspectReason;
				}
			}
		}

		private static void EnsureLibraryUnregistered(Program program, Schema.Library library, bool withReconciliation)
		{
			Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(library.Name, false);
			if (loadedLibrary != null)
			{
				while (loadedLibrary.RequiredByLibraries.Count > 0)
					EnsureLibraryUnregistered(program, program.Catalog.Libraries[loadedLibrary.RequiredByLibraries[0].Name], withReconciliation);
				UnregisterLibrary(program, library.Name, withReconciliation);
			}
		}
		
		private static void RemoveLibrary(Program program, Schema.Library library)
		{
			// Ensure that the library and any dependencies of it are unregistered
			EnsureLibraryUnregistered(program, library, false);
			DetachLibrary(program, library.Name);
		}
		
		public static void RefreshLibraries(Program program)
		{
			// Get the list of available libraries from the library directory
			Schema.Libraries libraries = new Schema.Libraries();
			Schema.Libraries oldLibraries = new Schema.Libraries();
			Server.Server server = (Server.Server)program.ServerProcess.ServerSession.Server;
			GetAvailableLibraries(server.InstanceDirectory, server.LibraryDirectory, libraries);
			
			lock (program.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library library in libraries)
					if (!program.Catalog.Libraries.ContainsName(library.Name) && !program.Catalog.ContainsName(library.Name))
						AttachLibrary(program, library, false);
						
				// Ensure that each library in the DAE is supported by a library in the directory, remove non-existent libraries
				// The System library is not required to have a directory.
				foreach (Schema.Library library in program.Catalog.Libraries)
					if ((Engine.SystemLibraryName != library.Name) && (library.Directory == String.Empty) && !libraries.ContainsName(library.Name))
						oldLibraries.Add(library);
						
				foreach (Schema.Library library in oldLibraries)
					RemoveLibrary(program, library);
			}
		}

		public static void UnregisterLibrary(Program program, string libraryName, bool withReconciliation)
		{
			int saveReconciliationState = program.ServerProcess.SuspendReconciliationState();
			try
			{
				if (!withReconciliation)
					program.ServerProcess.DisableReconciliation();
				try
				{
					lock (program.Catalog.Libraries)
					{
						Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(libraryName);
						
						if (Schema.Object.NamesEqual(loadedLibrary.Name, Engine.SystemLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterSystemLibrary);
							
						if (Schema.Object.NamesEqual(loadedLibrary.Name, Engine.GeneralLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterGeneralLibrary);

						if (loadedLibrary.RequiredByLibraries.Count > 0)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryIsRequired, loadedLibrary.Name);
							
						// Drop all the objects in the library
						program.ServerProcess.ServerSession.Server.RunScript
						(
							program.ServerProcess, 
							program.ServerProcess.ServerSession.Server.ScriptDropLibrary(program.CatalogDeviceSession, libraryName), 
							loadedLibrary.Name,
							null
						);
						
						// Any session with current library set to the unregistered library will be set to General
						program.ServerProcess.ServerSession.Server.LibraryUnloaded(loadedLibrary.Name);

						// Remove the library from the catalog
						program.CatalogDeviceSession.DeleteLoadedLibrary(loadedLibrary);
						loadedLibrary.DetachLibrary();
						
						// Unregister each assembly that was loaded with this library
						UnregisterLibraryAssemblies(program, loadedLibrary);

						// TODO: Unregister assemblies when the .NET framework supports it
						
						program.Catalog.Libraries.DoLibraryUnloaded(program, loadedLibrary.Name);
					}
				}
				finally
				{
					if (!withReconciliation)
						program.ServerProcess.EnableReconciliation();
				}
			}
			finally
			{
				program.ServerProcess.ResumeReconciliationState(saveReconciliationState);
			}
		}

		public static void DropLibrary(Program program, string libraryName, bool updateCatalogTimeStamp)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (program.CatalogDeviceSession.IsLoadedLibrary(library.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDropRegisteredLibrary, library.Name);
					
				string libraryDirectory = library.GetLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);

				program.Catalog.Libraries.DoLibraryRemoved(program, library.Name);
				program.Catalog.Libraries.DoLibraryDeleted(program, library.Name);
				try
				{
					program.Catalog.Libraries.Remove(library);
					try
					{
						if (updateCatalogTimeStamp)
							program.Catalog.UpdateTimeStamp();
						if (Directory.Exists(libraryDirectory))
						{
							#if !RESPECTREADONLY
							PathUtility.EnsureWriteable(libraryDirectory, true);
							#endif
							Directory.Delete(libraryDirectory, true);
						}
						
						((ServerCatalogDeviceSession)program.CatalogDeviceSession).ClearLibraryOwner(libraryName);
						((ServerCatalogDeviceSession)program.CatalogDeviceSession).ClearCurrentLibraryVersion(libraryName);
					}
					catch
					{
						if (Directory.Exists(libraryDirectory))
							program.Catalog.Libraries.Add(library);
						throw;
					}
				}
				catch
				{
					if (Directory.Exists(libraryDirectory))
					{
						program.Catalog.Libraries.DoLibraryCreated(program, library.Name);
						program.Catalog.Libraries.DoLibraryAdded(program, library.Name);
					}
					throw;
				}
			}
		}
		
		public static void DetachLibrary(Program program, string libraryName)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (program.CatalogDeviceSession.IsLoadedLibrary(library.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDetachRegisteredLibrary, library.Name);
					
				string libraryDirectory = library.GetLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);

				program.Catalog.Libraries.DoLibraryRemoved(program, library.Name);
				program.Catalog.Libraries.Remove(library);
				program.Catalog.UpdateTimeStamp();
				((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeleteLibraryDirectory(library.Name);
			}
		}

		/// <summary>Attaches the library given by ALibraryName from ALibraryDirectory. AIsAttached indicates whether this library is being attached as part of catalog startup.</summary>
		public static void AttachLibrary(Program program, string libraryName, string libraryDirectory, bool isAttached)
		{
			Schema.Library library = GetAvailableLibrary(((Server.Server)program.ServerProcess.ServerSession.Server).InstanceDirectory, libraryName, libraryDirectory);
			if ((library != null) && !program.Catalog.Libraries.ContainsName(library.Name))
				AttachLibrary(program, library, isAttached);
		}

		public static void AttachLibrary(Program program, Schema.Library library, bool isAttached)
		{
			lock (program.Catalog.Libraries)
			{
				string libraryDirectory = library.GetLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);

				program.Catalog.Libraries.Add(library);
				program.Catalog.UpdateTimeStamp();
				program.Catalog.Libraries.DoLibraryAdded(program, library.Name);
				if ((library.Directory != String.Empty) && !isAttached)
					((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetLibraryDirectory(library.Name, libraryDirectory);
			}
		}
		
		public static void EnsureLibraryRegistered(Program program, string libraryName, bool withReconciliation)
		{
			lock (program.Catalog.Libraries)
			{
				if (program.CatalogDeviceSession.ResolveLoadedLibrary(libraryName, false) == null)
					RegisterLibrary(program, libraryName, withReconciliation);
			}
		}
		
		public static void EnsureLibraryRegistered(Program program, Schema.LibraryReference libraryReference, bool withReconciliation)
		{
			Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(libraryReference.Name, false);
			if (loadedLibrary == null)
			{
				Schema.LoadedLibrary currentLibrary = program.ServerProcess.ServerSession.CurrentLibrary;
				try
				{
					Schema.Library library = program.Catalog.Libraries[libraryReference.Name];
					if (!VersionNumber.Compatible(libraryReference.Version, library.Version))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, libraryReference.Name, libraryReference.Version.ToString(), library.Version.ToString());
					RegisterLibrary(program, library.Name, withReconciliation);
				}
				finally
				{
					program.ServerProcess.ServerSession.CurrentLibrary = currentLibrary;
				}
			}
			else
			{
				Schema.Library library = program.Catalog.Libraries[libraryReference.Name];
				if (!VersionNumber.Compatible(libraryReference.Version, library.Version))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, libraryReference.Name, libraryReference.Version.ToString(), library.Version.ToString());
			}
		}
		
		public static void RegisterLibraryFiles(Program program, Schema.Library library, Schema.LoadedLibrary loadedLibrary)
		{
			// Register each assembly with the DAE
			#if !LOADFROMLIBRARIES
			foreach (Schema.FileReference file in ALibrary.Files)
			{
				if ((file.Environments.Count == 0) || file.Environments.Contains(Environments.WindowsServer))
				{
					string sourceFile = Path.IsPathRooted(file.FileName) ? file.FileName : Path.Combine(ALibrary.GetLibraryDirectory(((Server.Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), file.FileName);
					string targetFile = Path.Combine(PathUtility.GetBinDirectory(), Path.GetFileName(file.FileName));
					
					if (!File.Exists(sourceFile))
						throw new System.IO.IOException(String.Format("File \"{0}\" not found.", sourceFile));
					try
					{
						#if !RESPECTREADONLY
						FileUtility.EnsureWriteable(targetFile);
						#endif
						if ((File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(targetFile))) // source newer than target
						{
							File.Copy(sourceFile, targetFile, true);
						}
					}
					catch (IOException)
					{
						// Ignore this exception so that assembly copying does not fail if the assembly is already loaded
					}
				}
			}
			#endif
			
			// Load assemblies after all files are copied in so that multi-file assemblies and other dependencies are certain to be present
			foreach (Schema.FileReference file in library.Files)
			{
				if ((file.Environments.Count == 0) || file.Environments.Contains(Environments.WindowsServer))
				{
					#if LOADFROMLIBRARIES
					string sourceFile = Path.IsPathRooted(file.FileName) ? file.FileName : Path.Combine(library.GetLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), file.FileName);
					if (FileUtility.IsAssembly(sourceFile))
					{
	                    Assembly assembly = Assembly.LoadFrom(sourceFile);
					#else
                    string targetFile = Path.Combine(PathUtility.GetBinDirectory(), Path.GetFileName(file.FileName));
					if (FileUtility.IsAssembly(targetFile))
					{
	                    Assembly assembly = Assembly.LoadFrom(targetFile);
                    #endif
						if (file.IsAssembly)
		                    program.CatalogDeviceSession.RegisterAssembly(loadedLibrary, assembly);
					}
				}
			}
		}
		
		public static void UnregisterLibraryAssemblies(Program program, Schema.LoadedLibrary loadedLibrary)
		{
			while (loadedLibrary.Assemblies.Count > 0)
				program.CatalogDeviceSession.UnregisterAssembly(loadedLibrary, loadedLibrary.Assemblies[loadedLibrary.Assemblies.Count - 1] as Assembly);
		}
		
		public static void CheckCircularLibraryReference(Program program, Schema.Library library, string requiredLibraryName)
		{
			if (IsCircularLibraryReference(program, library, requiredLibraryName))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.CircularLibraryReference, library.Name, requiredLibraryName);
		}
		
		public static bool IsCircularLibraryReference(Program program, Schema.Library library, string requiredLibraryName)
		{
			Schema.Library requiredLibrary = program.Catalog.Libraries[requiredLibraryName];
			if (Schema.Object.NamesEqual(library.Name, requiredLibraryName))
				return true;
				
			foreach (Schema.LibraryReference reference in requiredLibrary.Libraries)
				if (IsCircularLibraryReference(program, library, reference.Name))
					return true;
					
			return false;
		}
		
		public static void RegisterLibrary(Program program, string libraryName, bool withReconciliation)
		{
			int saveReconciliationState = program.ServerProcess.SuspendReconciliationState();
			try
			{
				if (!withReconciliation)
					program.ServerProcess.DisableReconciliation();
				try
				{
					lock (program.Catalog.Libraries)
					{
						Schema.Library library = program.Catalog.Libraries[libraryName];
						
						Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(library.Name, false);
						if (loadedLibrary != null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryAlreadyRegistered, libraryName);
							
						loadedLibrary = new Schema.LoadedLibrary(libraryName);
						loadedLibrary.Owner = program.Plan.User;
							
						//	Ensure that each required library is registered
						foreach (Schema.LibraryReference reference in library.Libraries)
						{
							CheckCircularLibraryReference(program, library, reference.Name);
							EnsureLibraryRegistered(program, reference, withReconciliation);
							loadedLibrary.RequiredLibraries.Add(program.CatalogDeviceSession.ResolveLoadedLibrary(reference.Name));
							program.Catalog.OperatorResolutionCache.Clear(loadedLibrary.GetNameResolutionPath(program.ServerProcess.ServerSession.Server.SystemLibrary));
							loadedLibrary.ClearNameResolutionPath();
						}
						
						((Server.Server)program.ServerProcess.ServerSession.Server).DoLibraryLoading(library.Name);
						try
						{
							// Register the assemblies
							RegisterLibraryFiles(program, library, loadedLibrary);
							program.CatalogDeviceSession.InsertLoadedLibrary(loadedLibrary);
							loadedLibrary.AttachLibrary();
							try
							{
								// Set the current library to the newly registered library
								Schema.LoadedLibrary currentLibrary = program.Plan.CurrentLibrary;
								program.ServerProcess.ServerSession.CurrentLibrary = loadedLibrary;
								try
								{
									//	run the register.d4 script if it exists in the library
									//		catalog objects created in this script are part of this library
									string registerFileName = Path.Combine(library.GetLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), RegisterFileName);
									if (File.Exists(registerFileName))
									{
										try
										{
											using (StreamReader reader = new StreamReader(registerFileName))
											{
												program.ServerProcess.ServerSession.Server.RunScript
												(
													program.ServerProcess, 
													reader.ReadToEnd(), 
													libraryName, 
													new DAE.Debug.DebugLocator(String.Format(RegisterDocumentLocator, libraryName), 1, 1)
												);
											}
										}
										catch (Exception exception)
										{
											throw new RuntimeException(RuntimeException.Codes.LibraryRegistrationFailed, exception, libraryName);
										}
									}

									((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetCurrentLibraryVersion(library.Name, library.Version);
									((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetLibraryOwner(loadedLibrary.Name, loadedLibrary.Owner.ID);
									if (library.IsSuspect)
									{
										library.IsSuspect = false;
										library.SaveInfoToFile(Path.Combine(library.GetInstanceLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).InstanceDirectory), GetInfoFileName(library.Name)));
									}					
								}
								catch
								{
									program.ServerProcess.ServerSession.CurrentLibrary = currentLibrary;
									throw;
								}
							}
							catch
							{
								loadedLibrary.DetachLibrary();
								throw;
							}
							program.Catalog.Libraries.DoLibraryLoaded(program, library.Name);
						}
						finally
						{
							((Server.Server)program.ServerProcess.ServerSession.Server).DoLibraryLoaded(libraryName);
						}
					}
				}
				finally
				{
					if (!withReconciliation)
						program.ServerProcess.EnableReconciliation();
				}
			}
			finally
			{
				program.ServerProcess.ResumeReconciliationState(saveReconciliationState);
			}
		}

		public static void LoadLibrary(Program program, string libraryName)
		{
			LoadLibrary(program, libraryName, false);
		}
		
		private static void LoadLibrary(Program program, string libraryName, bool isKnown)
		{
			lock (program.Catalog.Libraries)
			{
				try
				{
					Schema.Library library = program.Catalog.Libraries[libraryName];
					VersionNumber currentVersion = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetCurrentLibraryVersion(libraryName);
					
					if (program.Catalog.LoadedLibraries.Contains(library.Name))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryAlreadyLoaded, libraryName);

					bool isLoaded = false;				
					bool areAssembliesRegistered = false;	
					Schema.LoadedLibrary loadedLibrary = null;
					try
					{
						loadedLibrary = new Schema.LoadedLibrary(libraryName);
						loadedLibrary.Owner = program.CatalogDeviceSession.ResolveUser(((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetLibraryOwner(libraryName));
							
						//	Ensure that each required library is loaded
						foreach (Schema.LibraryReference reference in library.Libraries)
						{
							Schema.Library requiredLibrary = program.Catalog.Libraries[reference.Name];
							if (!VersionNumber.Compatible(reference.Version, requiredLibrary.Version))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, reference.Name, reference.Version.ToString(), requiredLibrary.Version.ToString());

							if (!program.Catalog.LoadedLibraries.Contains(reference.Name))
							{
								if (!requiredLibrary.IsSuspect)
									LoadLibrary(program, reference.Name, isKnown);
								else
									throw new Schema.SchemaException(Schema.SchemaException.Codes.RequiredLibraryNotLoaded, libraryName, reference.Name);
							}
							
							loadedLibrary.RequiredLibraries.Add(program.CatalogDeviceSession.ResolveLoadedLibrary(reference.Name));
							program.Catalog.OperatorResolutionCache.Clear(loadedLibrary.GetNameResolutionPath(program.ServerProcess.ServerSession.Server.SystemLibrary));
							loadedLibrary.ClearNameResolutionPath();
						}
						
						program.ServerProcess.ServerSession.Server.DoLibraryLoading(library.Name);
						try
						{
							// RegisterAssemblies
							RegisterLibraryFiles(program, library, loadedLibrary);
							
							areAssembliesRegistered = true;

							program.CatalogDeviceSession.InsertLoadedLibrary(loadedLibrary);
							loadedLibrary.AttachLibrary();
							try
							{
								((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetLibraryOwner(loadedLibrary.Name, loadedLibrary.Owner.ID);
							}
							catch (Exception registerException)
							{
								loadedLibrary.DetachLibrary();
								throw registerException;
							}

							isLoaded = true; // If we reach this point, a subsequent exception must unload the library
							if (library.IsSuspect)
							{
								library.IsSuspect = false;
								library.SaveInfoToFile(Path.Combine(library.GetInstanceLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).InstanceDirectory), Schema.LibraryUtility.GetInfoFileName(library.Name)));
							}					
						}
						finally
						{
							program.ServerProcess.ServerSession.Server.DoLibraryLoaded(library.Name);
						}
					}
					catch (Exception exception)
					{
						program.ServerProcess.ServerSession.Server.LogError(exception);
						library.IsSuspect = true;
						library.SuspectReason = ExceptionUtility.DetailedDescription(exception);
						library.SaveInfoToFile(Path.Combine(library.GetInstanceLibraryDirectory(((Server.Server)program.ServerProcess.ServerSession.Server).InstanceDirectory), Schema.LibraryUtility.GetInfoFileName(library.Name)));
						
						if (isLoaded)
							UnregisterLibrary(program, libraryName, false);
						else if (areAssembliesRegistered)
							UnregisterLibraryAssemblies(program, loadedLibrary);
							
						throw;
					}

					((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetCurrentLibraryVersion(library.Name, currentVersion); // Once a library has loaded, record the version number
					
					program.Catalog.Libraries.DoLibraryLoaded(program, library.Name);
				}
				catch
				{
					if (program.ServerProcess.ServerSession.Server.State == ServerState.Started)
						throw;
				}
			}
		}
	}
}
