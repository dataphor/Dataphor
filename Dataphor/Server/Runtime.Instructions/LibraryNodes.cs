/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION
#define LOADFROMLIBRARIES

using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System.Collections.Generic;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Server;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.Windows;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Device.Catalog;

	// operator CreateLibrary(const ALibraryDescriptor : LibraryDescriptor);
	public class SystemCreateLibraryNode : InstructionNode
	{
		public static void CreateLibrary(Program program, Schema.Library library, bool updateCatalogTimeStamp, bool shouldNotify)
		{
			lock (program.Catalog.Libraries)
			{
				string libraryDirectory = library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);
				if (program.Catalog.Libraries.Contains(library.Name))
				{
					Schema.Library existingLibrary = program.Catalog.Libraries[library.Name];

					if (library.Directory != String.Empty)
					{
						string existingLibraryDirectory = existingLibrary.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);
						if (Directory.Exists(existingLibraryDirectory))
						{
							#if !RESPECTREADONLY
							PathUtility.EnsureWriteable(existingLibraryDirectory, true);
							#endif
							Directory.Delete(existingLibraryDirectory, true);
						}
						
						existingLibrary.Directory = library.Directory;

						if (!Directory.Exists(libraryDirectory))
							Directory.CreateDirectory(libraryDirectory);
						existingLibrary.SaveToFile(Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(existingLibrary.Name)));

						((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetLibraryDirectory(library.Name, libraryDirectory);
					}

					SystemSetLibraryDescriptorNode.ChangeLibraryVersion(program, library.Name, library.Version, updateCatalogTimeStamp);
					SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(program, library.Name, library.DefaultDeviceName, updateCatalogTimeStamp);

					foreach (Schema.LibraryReference reference in library.Libraries)
						if (!existingLibrary.Libraries.Contains(reference))
							SystemSetLibraryDescriptorNode.AddLibraryRequisite(program, library.Name, reference);

					foreach (Schema.FileReference reference in library.Files)
						if (!existingLibrary.Files.Contains(reference))
							SystemSetLibraryDescriptorNode.AddLibraryFile(program, library.Name, reference);

					if (library.MetaData != null)
						#if USEHASHTABLEFORTAGS
						foreach (Tag tag in ALibrary.MetaData.Tags)
							SystemSetLibraryDescriptorNode.AddLibrarySetting(AProgram, ALibrary.Name, tag);
						#else
						for (int index = 0; index < library.MetaData.Tags.Count; index++)
							SystemSetLibraryDescriptorNode.AddLibrarySetting(program, library.Name, library.MetaData.Tags[index]);
						#endif
				}
				else
				{
					Compiler.CheckValidCatalogObjectName(program.Plan, null, library.Name);
					program.Catalog.Libraries.Add(library);
					try
					{
						if (updateCatalogTimeStamp)
							program.Catalog.UpdateTimeStamp();

						if (!Directory.Exists(libraryDirectory))
							Directory.CreateDirectory(libraryDirectory);
						library.SaveToFile(Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(library.Name)));
					}
					catch
					{
						program.Catalog.Libraries.Remove(library);
						throw;
					}
				}

				if (shouldNotify)
				{
					program.Catalog.Libraries.DoLibraryCreated(program, library.Name);
					program.Catalog.Libraries.DoLibraryAdded(program, library.Name);
					if (library.Directory != String.Empty)
						((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetLibraryDirectory(library.Name, libraryDirectory);
				}
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			CreateLibrary(program, (Schema.Library)arguments[0], true, true);
			return null;
		}
	}
	
	// operator DropLibrary(const ALibraryName : Name);
	public class SystemDropLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			LibraryUtility.DropLibrary(program, (string)arguments[0], true);
			return null;
		}
	}

	// operator RenameLibrary(const ALibraryName : Name, const ANewLibraryName : Name);
	public class SystemRenameLibraryNode : InstructionNode
	{
		public static void RenameLibrary(Program program, string libraryName, string newLibraryName, bool updateCatalogTimeStamp)
		{
			newLibraryName = Schema.Object.EnsureUnrooted(newLibraryName);
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
				if (newLibraryName != library.Name)
				{
					Compiler.CheckValidCatalogObjectName(program.Plan, null, newLibraryName);
					if (program.CatalogDeviceSession.IsLoadedLibrary(library.Name))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRenameRegisteredLibrary, library.Name);
						
					string oldName = library.Name;
					program.Catalog.Libraries.Remove(library);
					try
					{
						library.Name = newLibraryName;
						
						string oldLibraryDirectory = Schema.LibraryUtility.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory, oldName, library.Directory);
						string libraryDirectory = library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);
						string oldLibraryName = Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(oldName));
						string localLibraryName = Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(library.Name));
						try
						{
							program.Catalog.Libraries.Add(library);
							try
							{
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(oldLibraryDirectory, true);
								#endif
								if (oldLibraryDirectory != libraryDirectory)
									Directory.Move(oldLibraryDirectory, libraryDirectory);
								try
								{
									#if !RESPECTREADONLY
									FileUtility.EnsureWriteable(oldLibraryName);
									#endif
									File.Delete(oldLibraryName);
									library.SaveToFile(localLibraryName);
								}
								catch
								{
									if (oldLibraryDirectory != libraryDirectory)
										Directory.Move(libraryDirectory, oldLibraryDirectory);
									oldLibraryName = Path.Combine(oldLibraryDirectory, Schema.LibraryUtility.GetFileName(library.Name));
									if (File.Exists(oldLibraryName))
										File.Delete(oldLibraryName);
									throw;
								}

								if (updateCatalogTimeStamp)
									program.Catalog.UpdateTimeStamp();
							}
							catch
							{
								program.Catalog.Libraries.Remove(library);
								throw;
							}
						}
						catch
						{
							library.Name = oldName;
							library.SaveToFile(Path.Combine(oldLibraryDirectory, Schema.LibraryUtility.GetFileName(library.Name))); // ensure that the file is restored to its original state
							throw;
						}
					}
					catch
					{
						program.Catalog.Libraries.Add(library);
						throw;
					}
					program.Catalog.Libraries.DoLibraryRenamed(program, oldName, library.Name);
				}
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			RenameLibrary(program, (string)arguments[0], (string)arguments[1], true);
			return null;
		}
	}

	// operator RefreshLibraries(const ALibraryName : Name);
	public class SystemRefreshLibrariesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			LibraryUtility.RefreshLibraries(program);
			return null;
		}
	}
	
	// operator AttachLibraries(string ADirectory)
	// Attaches all libraries found in the given directory
	public class SystemAttachLibrariesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string libraryDirectory = (string)arguments[0];
			
			Schema.Libraries libraries = new Schema.Libraries();
			Schema.LibraryUtility.GetAvailableLibraries(((Server)program.ServerProcess.ServerSession.Server).InstanceDirectory, libraryDirectory, libraries, true);

			lock (program.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library library in libraries)
					if (!program.Catalog.Libraries.ContainsName(library.Name) && !program.Catalog.ContainsName(library.Name))
						LibraryUtility.AttachLibrary(program, library, false);
			}
			
			return null;
		}
	}
	
	// operator AttachLibrary(string ALibraryName, string ALibraryDirectory)
	public class SystemAttachLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string libraryName = (string)arguments[0];
			string libraryDirectory = (string)arguments[1];
			
			LibraryUtility.AttachLibrary(program, libraryName, libraryDirectory, false);
			
			return null;
		}
	}
	
	// operator DetachLibrary(string ALibraryName)
	public class SystemDetachLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string libraryName = (string)arguments[0];

			LibraryUtility.DetachLibrary(program, libraryName);
			
			return null;
		}
	}
	
	// operator GetLibraryDescriptor(const ALibraryName : Name) : LibraryDescriptor;
	public class SystemGetLibraryDescriptorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			lock (program.Catalog.Libraries)
			{
				return (Schema.Library)program.Catalog.Libraries[(string)arguments[0]].Clone();
			}
		}
	}
	
	// operator SetLibraryDescriptor(const ALibraryName : Name, const ALibraryDescriptor : LibraryDescriptor);
	public class SystemSetLibraryDescriptorNode : InstructionNode
	{
		public static void ChangeLibraryVersion(Program program, string libraryName, VersionNumber version, bool updateTimeStamp)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (!(library.Version.Equals(version)))
				{
					Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(library.Name, false);
					if (loadedLibrary != null)
					{
						foreach (Schema.LoadedLibrary dependentLoadedLibrary in loadedLibrary.RequiredByLibraries)
						{
							Schema.Library dependentLibrary = program.Catalog.Libraries[dependentLoadedLibrary.Name];
							Schema.LibraryReference libraryReference = dependentLibrary.Libraries[library.Name];
							if (!VersionNumber.Compatible(libraryReference.Version, version))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.RegisteredLibraryHasDependents, library.Name, dependentLibrary.Name, libraryReference.Version.ToString());
						}
					}
					library.Version = version;

					if (updateTimeStamp)
						program.Catalog.UpdateTimeStamp();

					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				
					if (program.CatalogDeviceSession.IsLoadedLibrary(library.Name))	
						((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetCurrentLibraryVersion(library.Name, library.Version);
				}
			}
		}
		public static void SetLibraryDefaultDeviceName(Program program, string libraryName, string defaultDeviceName, bool updateTimeStamp)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (library.DefaultDeviceName != defaultDeviceName)
				{
					library.DefaultDeviceName = defaultDeviceName;

					if (updateTimeStamp)
						program.Catalog.UpdateTimeStamp();

					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
			}
		}

		public static void AddLibraryRequisite(Program program, string libraryName, Schema.LibraryReference requisiteLibrary)
		{
			lock (program.Catalog.Libraries)
			{
				if (!program.Catalog.Libraries.Contains(libraryName))
					SystemCreateLibraryNode.CreateLibrary(program, new Schema.Library(Schema.Object.EnsureUnrooted(libraryName)), true, false);

				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				library.Libraries.Add(requisiteLibrary);
				try
				{
					LibraryUtility.CheckCircularLibraryReference(program, library, requisiteLibrary.Name);
					Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(library.Name, false);
					if (loadedLibrary != null)
					{
						LibraryUtility.EnsureLibraryRegistered(program, requisiteLibrary, true);
						if (!loadedLibrary.RequiredLibraries.Contains(requisiteLibrary.Name))
						{
							loadedLibrary.RequiredLibraries.Add(program.CatalogDeviceSession.ResolveLoadedLibrary(requisiteLibrary.Name));
							program.Catalog.OperatorResolutionCache.Clear(loadedLibrary.GetNameResolutionPath(program.ServerProcess.ServerSession.Server.SystemLibrary));
							loadedLibrary.ClearNameResolutionPath();
						}
						loadedLibrary.AttachLibrary();
					}

					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					library.Libraries.Remove(requisiteLibrary);
					throw;
				}
			}
		}

		public static void UpdateLibraryRequisite(Program program, string libraryName, Schema.LibraryReference oldReference, Schema.LibraryReference newReference)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (String.Compare(oldReference.Name, newReference.Name, false) != 0)
				{
					RemoveLibraryRequisite(program, libraryName, oldReference);
					AddLibraryRequisite(program, libraryName, newReference);
				}
				else
				{
					if (!VersionNumber.Equals(oldReference.Version, newReference.Version))
					{
						if (program.CatalogDeviceSession.IsLoadedLibrary(library.Name))
						{
							// Ensure that the registered library satisfies the new requisite
							Schema.Library requiredLibrary = program.Catalog.Libraries[oldReference.Name];
							if (!VersionNumber.Compatible(newReference.Version, requiredLibrary.Version))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidLibraryReference, oldReference.Name, libraryName, newReference.Version.ToString(), requiredLibrary.Version.ToString());
						}

						library.Libraries[oldReference.Name].Version = newReference.Version;

						library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
					}
				}
			}
		}

		public static void RemoveLibraryRequisite(Program program, string libraryName, Schema.LibraryReference requisiteLibrary)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (program.CatalogDeviceSession.IsLoadedLibrary(library.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRemoveRequisitesFromRegisteredLibrary, library.Name);

				library.Libraries.Remove(requisiteLibrary);
				library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
			}
		}

		public static void AddLibraryFile(Program program, string libraryName, Schema.FileReference file)
		{
			lock (program.Catalog.Libraries)
			{
				if (!program.Catalog.Libraries.Contains(libraryName))
					SystemCreateLibraryNode.CreateLibrary(program, new Schema.Library(Schema.Object.EnsureUnrooted(libraryName)), true, false);

				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				bool fileExists = library.Files.Contains(file.FileName);
				bool oldIsAssembly = fileExists ? library.Files[file.FileName].IsAssembly : false;
				if (fileExists)
					library.Files[file.FileName].IsAssembly = file.IsAssembly;
				else
					library.Files.Add(file);
				try
				{
					// ensure that all assemblies are registered
					Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(library.Name, false);
					if (loadedLibrary != null)
						LibraryUtility.RegisterLibraryFiles(program, library, loadedLibrary);

					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					if (fileExists)
						library.Files[file.FileName].IsAssembly = oldIsAssembly;
					else
						library.Files.Remove(file);
					throw;
				}
			}
		}
		
		public static void RemoveLibraryFile(Program program, string libraryName, Schema.FileReference file)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				if (library.Files[file.FileName].Environments.Count > 0)
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, String.Format("File '{0}' in library '{1}' cannot be removed because it has environments defined.", file.FileName, libraryName));
					
				FileReference removedFile = library.Files.RemoveAt(library.Files.IndexOf(file.FileName));
				try
				{
					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					library.Files.Add(removedFile);
					throw;
				}
			}
		}
		
		public static void AddLibraryFileEnvironment(Program program, string libraryName, string fileName, string environment)
		{
			lock (program.Catalog.Libraries)
			{
				if (!program.Catalog.Libraries.Contains(libraryName))
					SystemCreateLibraryNode.CreateLibrary(program, new Schema.Library(Schema.Object.EnsureUnrooted(libraryName)), true, false);

				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				int fileIndex = library.Files.IndexOf(fileName);
				bool fileAdded = false;
				if (fileIndex < 0)
				{
					fileAdded = true;
					fileIndex = library.Files.Add(new FileReference(fileName));
				}
					
				library.Files[fileIndex].Environments.Add(environment);
				try
				{
					// ensure that all assemblies are registered
					Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(library.Name, false);
					if (loadedLibrary != null)
						LibraryUtility.RegisterLibraryFiles(program, library, loadedLibrary);

					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					library.Files[fileIndex].Environments.Remove(environment);
					if (fileAdded)
						library.Files.RemoveAt(fileIndex);
					throw;
				}
			}
		}
		
		public static void RemoveLibraryFileEnvironment(Program program, string libraryName, string fileName, string environment)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				library.Files[fileName].Environments.Remove(environment);
				try
				{
					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					library.Files[fileName].Environments.Add(environment);
					throw;
				}
			}
		}

		public static void AddLibrarySetting(Program program, string libraryName, Tag setting)
		{
			lock (program.Catalog.Libraries)
			{
				if (!program.Catalog.Libraries.Contains(libraryName))
					SystemCreateLibraryNode.CreateLibrary(program, new Schema.Library(Schema.Object.EnsureUnrooted(libraryName)), true, false);

				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (library.MetaData == null)
					library.MetaData = new MetaData();

				library.MetaData.Tags.Add(setting);
				try
				{
					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					library.MetaData.Tags.Remove(setting.Name);
					throw;
				}
			}
		}

		public static void UpdateLibrarySetting(Program program, string libraryName, Tag oldSetting, Tag newSetting)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (String.Compare(oldSetting.Name, newSetting.Name, false) != 0)
				{
					library.MetaData.Tags.Remove(oldSetting.Name);
					library.MetaData.Tags.Add(newSetting);
				}
				else
					library.MetaData.Tags.Update(oldSetting.Name, newSetting.Value);
				try
				{
					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					if (String.Compare(oldSetting.Name, newSetting.Name, false) != 0)
					{
						library.MetaData.Tags.Remove(newSetting.Name);
						library.MetaData.Tags.Add(oldSetting);
					}
					else
					{
						library.MetaData.Tags.Update(oldSetting.Name, oldSetting.Value);
					}
					throw;
				}
			}
		}

		public static void RemoveLibrarySetting(Program program, string libraryName, Tag tag)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				if (Schema.Object.NamesEqual(library.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(library.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				library.MetaData.Tags.Remove(tag.Name);
				try
				{
					library.SaveToFile(Path.Combine(library.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(library.Name)));
				}
				catch
				{
					library.MetaData.Tags.Add(tag);
					throw;
				}
			}
		}

		public static void SetLibraryDescriptor(Program program, string oldLibraryName, Schema.Library newLibrary, bool updateTimeStamp)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library oldLibrary = program.Catalog.Libraries[oldLibraryName];

				if (Schema.Object.NamesEqual(oldLibrary.Name, Server.SystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(oldLibrary.Name, Server.GeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (oldLibrary.Name != newLibrary.Name)
					Compiler.CheckValidCatalogObjectName(program.Plan, null, newLibrary.Name);

				if (program.CatalogDeviceSession.IsLoadedLibrary(oldLibrary.Name))
				{
					foreach (Schema.LibraryReference reference in newLibrary.Libraries)
						LibraryUtility.CheckCircularLibraryReference(program, newLibrary, reference.Name);

					// cannot rename a loaded library
					if (oldLibrary.Name != newLibrary.Name)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRenameRegisteredLibrary, oldLibrary.Name);

					// cannot change the version of a loaded library
					if (!(oldLibrary.Version.Equals(newLibrary.Version)))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotChangeRegisteredLibraryVersion, oldLibrary.Name);

					// cannot remove a required library
					foreach (Schema.LibraryReference reference in oldLibrary.Libraries)
						if (!newLibrary.Libraries.Contains(reference))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRemoveRequisitesFromRegisteredLibrary, oldLibrary.Name);

					Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(newLibrary.Name);
					foreach (Schema.LibraryReference reference in newLibrary.Libraries)
					{
						LibraryUtility.EnsureLibraryRegistered(program, reference, true);
						if (!loadedLibrary.RequiredLibraries.Contains(reference.Name))
						{
							loadedLibrary.RequiredLibraries.Add(program.CatalogDeviceSession.ResolveLoadedLibrary(reference.Name));
							program.Catalog.OperatorResolutionCache.Clear(loadedLibrary.GetNameResolutionPath(program.ServerProcess.ServerSession.Server.SystemLibrary));
							loadedLibrary.ClearNameResolutionPath();
						}
					}
					loadedLibrary.AttachLibrary();

					// ensure that all assemblies are registered
					LibraryUtility.RegisterLibraryFiles(program, newLibrary, program.CatalogDeviceSession.ResolveLoadedLibrary(newLibrary.Name));
				}

				program.Catalog.Libraries.Remove(oldLibrary);
				try
				{
					program.Catalog.Libraries.Add(newLibrary);
					try
					{
						if (updateTimeStamp)
							program.Catalog.UpdateTimeStamp();

						string libraryDirectory = newLibrary.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);
						string libraryName = Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(newLibrary.Name));
						try
						{
							if (oldLibrary.Name != newLibrary.Name)
							{
								string oldLibraryDirectory = oldLibrary.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);
								string localOldLibraryName = Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(oldLibrary.Name));
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(oldLibraryDirectory, true);
								FileUtility.EnsureWriteable(localOldLibraryName);
								#endif
								Directory.Move(oldLibraryDirectory, libraryDirectory);
								try
								{
									File.Delete(localOldLibraryName);
								}
								catch
								{
									Directory.Move(libraryDirectory, oldLibraryDirectory);
									throw;
								}
								program.Catalog.Libraries.DoLibraryRenamed(program, oldLibrary.Name, newLibrary.Name);
							}
							newLibrary.SaveToFile(libraryName);
						}
						catch
						{
							if (oldLibrary.Name != newLibrary.Name)
								oldLibrary.SaveToFile(Path.Combine(oldLibrary.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(oldLibrary.Name)));
							throw;
						}
					}
					catch
					{
						program.Catalog.Libraries.Remove(newLibrary);
						throw;
					}
				}
				catch
				{
					program.Catalog.Libraries.Add(oldLibrary);
					throw;
				}
			}
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			SetLibraryDescriptor(program, (string)arguments[0], (Schema.Library)arguments[1], true);
			return null;
		}
	}

	// operator LibrarySetting(const ASettingName : String) : String;
	public class SystemLibrarySettingNode : InstructionNode
	{
		// Find the first unambiguous setting value for the given setting name in a breadth-first traversal of the library dependency graph
		private static string ResolveSetting(Plan plan, string settingName)
		{
			Tag tag = Tag.None;
			foreach (Schema.LoadedLibraries level in plan.NameResolutionPath)
			{
				List<string> containingLibraries = new List<string>();
				foreach (Schema.LoadedLibrary loadedLibrary in level)
				{
					Schema.Library library = plan.Catalog.Libraries[loadedLibrary.Name];
					if ((library.MetaData != null) && library.MetaData.Tags.Contains(settingName))
						containingLibraries.Add(library.Name);
				}
				
				if (containingLibraries.Count == 1)
				{
					tag = plan.Catalog.Libraries[containingLibraries[0]].MetaData.Tags[settingName];
					break;
				}
				else if (containingLibraries.Count > 1)
				{
					StringBuilder builder = new StringBuilder();
					foreach (string libraryName in containingLibraries)
					{
						if (builder.Length > 0)
							builder.Append(", ");
						builder.AppendFormat("{0}", libraryName);
					}
					
					throw new Schema.SchemaException(Schema.SchemaException.Codes.AmbiguousLibrarySetting, settingName, builder.ToString());
				}
			}

			return tag == Tag.None ? null : tag.Value;
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ResolveSetting(program.Plan, (string)arguments[0]);
		}
	}
	
	// operator RegisterLibrary(const ALibraryName : Name);	
	// operator RegisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemRegisterLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string libraryName = (string)arguments[0];
			bool withReconciliation = arguments.Length > 1 ? (bool)arguments[1] : true;
			lock (program.Catalog.Libraries)
			{
				if (!program.CatalogDeviceSession.IsLoadedLibrary(libraryName))
					LibraryUtility.RegisterLibrary(program, libraryName, withReconciliation);
			}
			return null;
		}
	}
	
	// operator EnsureLibraryRegistered(const ALibraryName : Name);
	public class SystemEnsureLibraryRegisteredNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			LibraryUtility.EnsureLibraryRegistered(program, (string)arguments[0], true);
			return null;
		}
	}
	
	// operator UnregisterLibrary(const ALibraryName : Name);
	// operator UnregisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemUnregisterLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			LibraryUtility.UnregisterLibrary(program, (string)arguments[0], arguments.Length > 1 ? (bool)arguments[1] : true);
			return null;
		}
	}
	
	// operator LoadLibrary(const ALibraryName : Name);
	public class SystemLoadLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "LoadLibrary is obsolete. Use RegisterLibrary instead.");
		}
	}
	
	public class SystemUpgradeLibraryNode : InstructionNode
	{
		/*
			2.1 -> Upgrading in the presence of dependencies:
			
			The upgrade system must track version dependencies per upgrade to ensure
			that upgrades run in the context in which they were injected. In other
			words, each upgrade must run with the version of each requisite library
			at the time the upgrade was injected. To manage these dependencies, we
			introduce the following operators:
			
			UpgradeRequisites
			(
				const ALibraryName : Name, 
				const AUpgradeVersion : VersionNumber
			) : 
				table 
				{ 
					Required_Library_Name : Name, 
					Required_Library_Version : VersionNumber 
				};
				
				Given an Upgrade denoted by ALibraryName and AUpgradeVersion,
				returns the set of libraries, together with their version, required
				by the library at the time the upgrade was injected.
			
			SaveUpgradeRequisite
			(
				const ALibraryName : Name, 
				const AUpgradeVersion : VersionNumber, 
				const ARequiredLibraryName : Name, 
				const ARequiredLibraryVersion : VersionNumber
			);
			
				Records the version number of the required library for a specific upgrade,
				with replace semantics for existing upgrade requisites.
			
			DeleteUpgradeRequisite
			(
				const ALibraryName : Name,
				const AUpgradeVersion : VersionNumber,
				const ARequiredLibraryName : Name,
				const ARequiredLibraryVersion : VersionNumber
			);
			
				Deletes the specified upgrade requisite from the given upgrade, with
				ignore semantics for non-existent upgrade requisites.
			
			RequiredUpgrades
			(
				const ALibraryName : Name, 
				const AVersion : VersionNumber
			) : 
				table 
				{ 
					Required_Library_Name : Name, 
					Required_Library_Version : VersionNumber 
				};
				
				Returns the set of library requisites for the given upgrade. If the upgrade has no upgrade requisites,
				the current version (not loaded version) of each requisite library of the library is returned.
				
				LibraryRequisites where Library_Name = ALibraryName { Required_Library_Name, Required_Library_Version }
					left join (UpgradeRequisites(ALibraryName, AVersion) { Upgrade_Library_Name, Upgrade_Library_Version }
						by Required_Library_Name = Upgrade_Library_Name
				{ 
					IfNil(Upgrade_Library_Name, Required_Library_Name) Required_Library_Name,
					IfNil(Upgrade_Library_Version, Required_Library_Version) Required_Library_Version
				}

			DependentUpgrades
			(
				const ALibraryName : Name,
				const AVersion : VersionNumber
			) :
				table
				{
					Dependent_Library_Name : Name,
					Dependent_Library_Version : VersionNumber
				};
				
				Returns the set of dependent libraries that have an upgrade that depends on a version less than
				the given upgrade version.

				UpgradeDependent(ALibraryName, AVersion)
					foreach DependentLibrary of ALibraryName
						foreach Upgrade of ADependentLibrary > CurrentVersion
							foreach UpgradeRequisite < AVersion
								Add to Result

			The UpgradeLibrary(ALibraryName) operator now has the following semantics:
			
				foreach VersionNumber LVersion in UpgradeVersions(ALibraryName)
					UpgradeLibrary(ALibraryName, LVersion);
				foreach Name LLibraryName in DependentLibraries(ALibraryName)
					UpgradeLibrary(ALibraryName);
					
			UpgradeLibrary(const ALibraryName : Name, const AVersion : VersionNumber);
			
				foreach row in RequiredUpgrades(ALibraryName, AVersion)
					UpgradeLibrary(Required_Library_Name, Required_Version);
				foreach row in DependentUpgrades(ALibraryName, AVersion)
					UpgradeLibrary(Dependent_Library_Name, Dependent_Version);
				Run the upgrade given by ALibraryName and AVersion	

		*/

		/*
			Upgrade all Libraries ->
				Calculate library load order for all loaded libraries
				foreach library in the library load order ->
					upgrade the library
					if an exception occurs ->
						unload each dependent library
						unload the library
						load the library
						load each dependent library
						rethrow
						
			Upgrade a single library ->
				Calculate library load order for all required
				foreach library in the library load order ->
					upgrade the library
					if an exception occurs ->
						unload each dependent library
						unload the library
						load the library
						load each dependent library
						rethrow
		*/
		
		private static void GatherDependents(Schema.LoadedLibrary library, Schema.LoadedLibraries dependents)
		{
			foreach (Schema.LoadedLibrary localLibrary in library.RequiredByLibraries)
			{
				if (!dependents.ContainsName(localLibrary.Name))
				{
					GatherDependents(localLibrary, dependents);
					dependents.Add(localLibrary);
				}
			}
		}
		
		private static void UpgradeLibraries(Program program, string[] libraries)
		{
			foreach (string libraryName in libraries)
				UpgradeLibrary(program, libraryName);
		}
		
		private static void InternalUpgradeLibrary(Program program, string libraryName, VersionNumber currentVersion, VersionNumber targetVersion)
		{
			DataParams paramsValue = new DataParams();
			paramsValue.Add(new DataParam("ALibraryName", program.DataTypes.SystemName, Modifier.Const, libraryName));
			paramsValue.Add(new DataParam("ACurrentVersion", Compiler.ResolveCatalogIdentifier(program.Plan, "System.VersionNumber", true) as Schema.IDataType, Modifier.Const, currentVersion));
			paramsValue.Add(new DataParam("ATargetVersion", Compiler.ResolveCatalogIdentifier(program.Plan, "System.VersionNumber", true) as Schema.IDataType, Modifier.Const, targetVersion));
			IServerStatementPlan plan =
				((IServerProcess)program.ServerProcess).PrepareStatement
				(
					@"
						begin
							var LRow : typeof(row from System.UpgradeVersions(ALibraryName));
							var LCursor := cursor(System.UpgradeVersions(ALibraryName) where (Version > ACurrentVersion) and (Version <= ATargetVersion));
							try
								while LCursor.Next() do
								begin
									LRow := LCursor.Select();
									BeginTransaction();
									try
										LogMessage('Executing upgrade script for library ' + ALibraryName + ', version ' + (Version from LRow).ToString());
										SetLibrary(ALibraryName);
										Execute(System.LoadUpgrade(ALibraryName, Version from LRow));
										if exists (LibraryVersions where LibraryName = ALibraryName) then
											update LibraryVersions set { Version := Version from LRow } where LibraryName = ALibraryName
										else
											insert table { row { ALibraryName LibraryName, Version from LRow Version } } into LibraryVersions;
										CommitTransaction();
									except
										RollbackTransaction();
										raise;
									end;
								end;
							finally
								LCursor.Close();
							end;
						end;
					",
					paramsValue
				);
			try
			{
				plan.Execute(paramsValue);
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareStatement(plan);
			}
		}
		
		public static void UpgradeLibrary(Program program, string libraryName)
		{
			lock (program.Catalog.Libraries)
			{
				Schema.Library library = program.Catalog.Libraries[libraryName];
				Schema.LoadedLibrary loadedLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary(libraryName);
				VersionNumber currentVersion = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetCurrentLibraryVersion(libraryName);
				if (VersionNumber.Compare(library.Version, currentVersion) > 0)
				{
					SessionInfo sessionInfo = new SessionInfo(loadedLibrary.Owner.ID == Server.SystemUserID ? Server.AdminUserID : loadedLibrary.Owner.ID, "", loadedLibrary.Name);
					sessionInfo.DefaultUseImplicitTransactions = false;
					IServerSession session = ((Server)program.ServerProcess.ServerSession.Server).ConnectAs(sessionInfo);
					try
					{
						ServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo)) as ServerProcess;
						try
						{
							Program localProgram = new Program(process);
							localProgram.Start(null);
							try
							{
								InternalUpgradeLibrary(localProgram, library.Name, currentVersion, library.Version);
							}
							finally
							{
								localProgram.Stop(null);
							}
						}
						finally
						{
							session.StopProcess(process);
						}
					}
					finally
					{
						((IServer)program.ServerProcess.ServerSession.Server).Disconnect(session);
					}
					((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetCurrentLibraryVersion(libraryName, library.Version);
				}
			}				
		}
		
		private static void GatherRequisites(Schema.LoadedLibrary library, Schema.LoadedLibraries requisites)
		{
			foreach (Schema.LoadedLibrary localLibrary in library.RequiredLibraries)
				GatherRequisites(localLibrary, requisites);
			
			if (!requisites.Contains(library.Name))	
				requisites.Add(library);
		}
		
		public static void UpgradeLibraries(Program program)
		{
			Schema.LoadedLibraries libraries = new Schema.LoadedLibraries();
			foreach (Schema.LoadedLibrary library in program.Catalog.LoadedLibraries)
				GatherRequisites(library, libraries);
			string[] libraryArray = new string[libraries.Count];
			for (int index = 0; index < libraries.Count; index++)
				libraryArray[index] = libraries[index].Name;
			UpgradeLibraries(program, libraryArray);
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			string libraryName = (string)arguments[0];
			Schema.LoadedLibrary library = program.CatalogDeviceSession.ResolveLoadedLibrary(libraryName);
			Schema.LoadedLibraries requisites = new Schema.LoadedLibraries();
			GatherRequisites(library, requisites);
			string[] libraries = new string[requisites.Count];
			for (int index = 0; index < requisites.Count; index++)
				libraries[index] = requisites[index].Name;
			UpgradeLibraries(program, libraries);
			return null;
		}
	}

	// operator UpgradeLibraries();	
	public class SystemUpgradeLibrariesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			SystemUpgradeLibraryNode.UpgradeLibraries(program);
			return null;
		}
	}

	// operator UnloadLibrary(const ALibraryName : Name);
	public class SystemUnloadLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "UnloadLibrary is obsolete. Use UnregisterLibrary instead.");
		}
	}
	
	public class SystemSaveLibraryCatalogNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "SaveLibraryCatalog is obsolete. Use ScriptCatalog instead.");
		}
	}
	
	public class UpgradeUtility : System.Object
	{
		public const string UpgradeDirectory = "Upgrades";
		public const string UpgradeFileName = "Upgrade";
		
		public static bool IsValidVersionNumber(VersionNumber version)
		{
			return (version.Revision >= 0) && (version.Build < 0);
		}
		
		public static void CheckValidVersionNumber(VersionNumber version)
		{
			// VersionNumbers used in Upgrades must be specified to the revision number only
			if (!IsValidVersionNumber(version))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidUpgradeVersionNumber, version.ToString());
		}
		
		public static VersionNumber GetVersionFromFileName(string fileName)
		{
			VersionNumber result = VersionNumber.Parse(fileName.Substring(fileName.IndexOf(".") + 1, fileName.IndexOf(".d4") - (fileName.IndexOf(".") + 1)));
			CheckValidVersionNumber(result);
			return result;
		}
		
		public static string GetFileNameFromVersion(VersionNumber version)
		{
			CheckValidVersionNumber(version);
			return String.Format("{0}.{1}.d4", UpgradeFileName, version.ToString().Replace(".*", ""));
		}
		
		public static string GetRequisitesFileNameFromVersion(VersionNumber version)
		{
			CheckValidVersionNumber(version);
			return String.Format("{0}.{1}.d4r", UpgradeFileName, version.ToString().Replace(".*", ""));
		}

		public static string GetUpgradeDirectory(Program program, string libraryName, string libraryDirectory)
		{
			return Path.Combine(Schema.LibraryUtility.GetLibraryDirectory(((Server)program.ServerProcess.ServerSession.Server).LibraryDirectory, libraryName, libraryDirectory), UpgradeDirectory);
		}
		
		public static void EnsureUpgradeDirectory(Program program, string libraryName, string libraryDirectory)
		{
			string upgradeDirectory = GetUpgradeDirectory(program, libraryName, libraryDirectory);
			if (!Directory.Exists(upgradeDirectory))
				Directory.CreateDirectory(upgradeDirectory);
		}
		
		public static string GetFileName(Program program, string libraryName, string libraryDirectory, VersionNumber version)
		{
			return Path.Combine(GetUpgradeDirectory(program, libraryName, libraryDirectory), GetFileNameFromVersion(version));
		}
		
		public static string GetRequisitesFileName(Program program, string libraryName, string libraryDirectory, VersionNumber version)
		{
			return Path.Combine(GetUpgradeDirectory(program, libraryName, libraryDirectory), GetRequisitesFileNameFromVersion(version));
		}
	}

	// create operator System.UpgradeVersions(const ALibraryName : Name, const ACurrentVersion : VersionNumber, const ATargetVersion : VersionNumber) : table { Version : VersionNumber };
	public class UpgradeVersionsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Version", Compiler.ResolveCatalogIdentifier(plan, "System.VersionNumber", true) as Schema.ScalarType));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Version"]}));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(Program program, Table table, Row row, VersionNumber version)
		{
			row[0] = version;
			table.Insert(row);
		}
		
		private void PopulateUpgradeVersions(Program program, Table table, Row row, string libraryName)
		{
			Schema.Library library = program.Catalog.Libraries[libraryName];
			string upgradeDirectory = UpgradeUtility.GetUpgradeDirectory(program, library.Name, library.Directory);
			if (Directory.Exists(upgradeDirectory))
			{
				string[] fileNames = Directory.GetFiles(upgradeDirectory, String.Format("{0}*.d4", UpgradeUtility.UpgradeFileName));
				for (int index = 0; index < fileNames.Length; index++)
					PopulateUpgradeVersion(program, table, row, UpgradeUtility.GetVersionFromFileName(Path.GetFileName(fileNames[index])));
			}
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					PopulateUpgradeVersions(program, result, row, (string)Nodes[0].Execute(program));
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}
	
/*
	// create operator System.UpgradeRequisites(const ALibraryName : Name, const AVersion : VersionNumber) : table { Required_Library_Name : Name, Required_Library_Version : VersionNumber };
	public class UpgradeRequisitesNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Required_Library_Name", APlan.Catalog["System.Name"] as Schema.ScalarType));
			DataType.Columns.Add(new Schema.Column("Required_Library_Version", APlan.Catalog["System.VersionNumber"] as Schema.ScalarType));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Required_Library_Name"]}));

			TableVar.DetermineRemotable();
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(Program AProgram, Table ATable, Row ARow, VersionNumber AVersion)
		{
			ARow[0].AsNative = AVersion;
			ATable.Insert(ARow);
		}
		
		private void PopulateUpgradeVersions(Program AProgram, Table ATable, Row ARow, string ALibraryName)
		{
			Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
			string LUpgradeDirectory = UpgradeUtility.GetUpgradeDirectory(AProgram, LLibrary.Name, LLibrary.Directory);
			if (Directory.Exists(LUpgradeDirectory))
			{
				string[] LFileNames = Directory.GetFiles(LUpgradeDirectory, String.Format("{0}*.d4", UpgradeUtility.CUpgradeFileName));
				for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
					PopulateUpgradeVersion(AProgram, ATable, ARow, UpgradeUtility.GetVersionFromFileName(Path.GetFileName(LFileNames[LIndex])));
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					PopulateUpgradeVersions(AProgram.ValueManager, LResult, LRow, Nodes[0].Execute(AProgram).Value.AsString);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}
	
	// create operator System.UpgradeVersions(const ALibraryName : Name) : table { Version : VersionNumber };
	public class UpgradeVersionsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Version", APlan.Catalog["System.VersionNumber"] as Schema.ScalarType));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Version"]}));

			TableVar.DetermineRemotable();
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(Program AProgram, Table ATable, Row ARow, VersionNumber AVersion)
		{
			ARow[0].AsNative = AVersion;
			ATable.Insert(ARow);
		}
		
		private void PopulateUpgradeVersions(Program AProgram, Table ATable, Row ARow, string ALibraryName)
		{
			Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
			string LUpgradeDirectory = UpgradeUtility.GetUpgradeDirectory(AProgram, LLibrary.Name, LLibrary.Directory);
			if (Directory.Exists(LUpgradeDirectory))
			{
				string[] LFileNames = Directory.GetFiles(LUpgradeDirectory, String.Format("{0}*.d4", UpgradeUtility.CUpgradeFileName));
				for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
					PopulateUpgradeVersion(AProgram, ATable, ARow, UpgradeUtility.GetVersionFromFileName(Path.GetFileName(LFileNames[LIndex])));
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					PopulateUpgradeVersions(AProgram, LResult, LRow, Nodes[0].Execute(AProgram).Value.AsString);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}
*/
	
	// create operator System.LoadUpgrade(const ALibraryName : Name, const AVersion : VersionNumber) : String;
	public class LoadUpgradeNode : InstructionNode
	{
		public static string LoadUpgrade(Program program, Schema.Library library, VersionNumber version)
		{
			StreamReader reader = new StreamReader(UpgradeUtility.GetFileName(program, library.Name, library.Directory, version));
			try
			{
				return reader.ReadToEnd();
			}
			finally
			{
				reader.Close();
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				LoadUpgrade
				(
					program, 
					program.Catalog.Libraries[(string)arguments[0]], 
					(VersionNumber)arguments[1]
				);
		}
	}
	
	// create operator System.SaveUpgrade(const ALibraryName : Name, const AVersion : VersionNumber, const AScript : String);
	public class SaveUpgradeNode : InstructionNode
	{
		public static void SaveUpgrade(Program program, Schema.Library library, VersionNumber version, string script)
		{
			UpgradeUtility.EnsureUpgradeDirectory(program, library.Name, library.Directory);
			string fileName = UpgradeUtility.GetFileName(program, library.Name, library.Directory, version);
			FileUtility.EnsureWriteable(fileName);
			StreamWriter writer = new StreamWriter(fileName, false);
			try
			{
				writer.Write(script);
				writer.Flush();
			}
			finally
			{
				writer.Close();
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments[1] == null)
				InjectUpgradeNode.InjectUpgrade(program, program.Catalog.Libraries[(string)arguments[0]], (string)arguments[2]);
			else
				SaveUpgrade(program, program.Catalog.Libraries[(string)arguments[0]], (VersionNumber)arguments[1], (string)arguments[2]);
			return null;
		}
	}
	
	// create operator System.DeleteUpgrade(const ALibraryName : Name, const AVersion : VersionNumber);
	public class DeleteUpgradeNode : InstructionNode
	{
		public static void DeleteUpgrade(Program program, Schema.Library library, VersionNumber version)
		{
			string fileName = UpgradeUtility.GetFileName(program, library.Name, library.Directory, version);
			FileUtility.EnsureWriteable(fileName);
			File.Delete(fileName);
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			DeleteUpgrade(program, program.Catalog.Libraries[(string)arguments[0]], (VersionNumber)arguments[1]);
			return null;
		}
	}
	
	// create operator System.InjectUpgrade(const ALibraryName : Name, const AScript : String) : VersionNumber;
	public class InjectUpgradeNode : InstructionNode
	{
		public static VersionNumber InjectUpgrade(Program program, Schema.Library library, string script)
		{
			VersionNumber libraryVersion = library.Version;
			if (libraryVersion.Revision < 0)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidUpgradeLibraryVersionNumber, library.Name, libraryVersion.ToString());
			SystemSetLibraryDescriptorNode.ChangeLibraryVersion(program, library.Name, new VersionNumber(libraryVersion.Major, libraryVersion.Minor, libraryVersion.Revision + 1, libraryVersion.Build), true);
			try
			{
				VersionNumber upgradeVersion = new VersionNumber(libraryVersion.Major, libraryVersion.Minor, library.Version.Revision, -1);
				UpgradeUtility.CheckValidVersionNumber(upgradeVersion);
				SaveUpgradeNode.SaveUpgrade(program, library, upgradeVersion, script);
				return upgradeVersion;
			}
			catch
			{
				SystemSetLibraryDescriptorNode.ChangeLibraryVersion(program, library.Name, libraryVersion, false);
				throw;
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			return InjectUpgrade(program, program.Catalog.Libraries[(string)arguments[0]], (string)arguments[1]);
		}
	}
}
