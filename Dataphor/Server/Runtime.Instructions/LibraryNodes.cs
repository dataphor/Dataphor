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
		public static void CreateLibrary(Program AProgram, Schema.Library ALibrary, bool AUpdateCatalogTimeStamp, bool AShouldNotify)
		{
			lock (AProgram.Catalog.Libraries)
			{
				string LLibraryDirectory = ALibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);
				if (AProgram.Catalog.Libraries.Contains(ALibrary.Name))
				{
					Schema.Library LExistingLibrary = AProgram.Catalog.Libraries[ALibrary.Name];

					if (ALibrary.Directory != String.Empty)
					{
						string LExistingLibraryDirectory = LExistingLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);
						if (Directory.Exists(LExistingLibraryDirectory))
						{
							#if !RESPECTREADONLY
							PathUtility.EnsureWriteable(LExistingLibraryDirectory, true);
							#endif
							Directory.Delete(LExistingLibraryDirectory, true);
						}
						
						LExistingLibrary.Directory = ALibrary.Directory;

						if (!Directory.Exists(LLibraryDirectory))
							Directory.CreateDirectory(LLibraryDirectory);
						LExistingLibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(LExistingLibrary.Name)));

						((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
					}

					SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProgram, ALibrary.Name, ALibrary.Version, AUpdateCatalogTimeStamp);
					SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProgram, ALibrary.Name, ALibrary.DefaultDeviceName, AUpdateCatalogTimeStamp);

					foreach (Schema.LibraryReference LReference in ALibrary.Libraries)
						if (!LExistingLibrary.Libraries.Contains(LReference))
							SystemSetLibraryDescriptorNode.AddLibraryRequisite(AProgram, ALibrary.Name, LReference);

					foreach (Schema.FileReference LReference in ALibrary.Files)
						if (!LExistingLibrary.Files.Contains(LReference))
							SystemSetLibraryDescriptorNode.AddLibraryFile(AProgram, ALibrary.Name, LReference);

					if (ALibrary.MetaData != null)
						#if USEHASHTABLEFORTAGS
						foreach (Tag LTag in ALibrary.MetaData.Tags)
							SystemSetLibraryDescriptorNode.AddLibrarySetting(AProgram, ALibrary.Name, LTag);
						#else
						for (int LIndex = 0; LIndex < ALibrary.MetaData.Tags.Count; LIndex++)
							SystemSetLibraryDescriptorNode.AddLibrarySetting(AProgram, ALibrary.Name, ALibrary.MetaData.Tags[LIndex]);
						#endif
				}
				else
				{
					Compiler.CheckValidCatalogObjectName(AProgram.Plan, null, ALibrary.Name);
					AProgram.Catalog.Libraries.Add(ALibrary);
					try
					{
						if (AUpdateCatalogTimeStamp)
							AProgram.Catalog.UpdateTimeStamp();

						if (!Directory.Exists(LLibraryDirectory))
							Directory.CreateDirectory(LLibraryDirectory);
						ALibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(ALibrary.Name)));
					}
					catch
					{
						AProgram.Catalog.Libraries.Remove(ALibrary);
						throw;
					}
				}

				if (AShouldNotify)
				{
					AProgram.Catalog.Libraries.DoLibraryCreated(AProgram, ALibrary.Name);
					AProgram.Catalog.Libraries.DoLibraryAdded(AProgram, ALibrary.Name);
					if (ALibrary.Directory != String.Empty)
						((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
				}
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			CreateLibrary(AProgram, (Schema.Library)AArguments[0], true, true);
			return null;
		}
	}
	
	// operator DropLibrary(const ALibraryName : Name);
	public class SystemDropLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			LibraryUtility.DropLibrary(AProgram, (string)AArguments[0], true);
			return null;
		}
	}

	// operator RenameLibrary(const ALibraryName : Name, const ANewLibraryName : Name);
	public class SystemRenameLibraryNode : InstructionNode
	{
		public static void RenameLibrary(Program AProgram, string ALibraryName, string ANewLibraryName, bool AUpdateCatalogTimeStamp)
		{
			ANewLibraryName = Schema.Object.EnsureUnrooted(ANewLibraryName);
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
				if (ANewLibraryName != LLibrary.Name)
				{
					Compiler.CheckValidCatalogObjectName(AProgram.Plan, null, ANewLibraryName);
					if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRenameRegisteredLibrary, LLibrary.Name);
						
					string LOldName = LLibrary.Name;
					AProgram.Catalog.Libraries.Remove(LLibrary);
					try
					{
						LLibrary.Name = ANewLibraryName;
						
						string LOldLibraryDirectory = Schema.LibraryUtility.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory, LOldName, LLibrary.Directory);
						string LLibraryDirectory = LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);
						string LOldLibraryName = Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(LOldName));
						string LLibraryName = Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(LLibrary.Name));
						try
						{
							AProgram.Catalog.Libraries.Add(LLibrary);
							try
							{
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(LOldLibraryDirectory, true);
								#endif
								if (LOldLibraryDirectory != LLibraryDirectory)
									Directory.Move(LOldLibraryDirectory, LLibraryDirectory);
								try
								{
									#if !RESPECTREADONLY
									FileUtility.EnsureWriteable(LOldLibraryName);
									#endif
									File.Delete(LOldLibraryName);
									LLibrary.SaveToFile(LLibraryName);
								}
								catch
								{
									if (LOldLibraryDirectory != LLibraryDirectory)
										Directory.Move(LLibraryDirectory, LOldLibraryDirectory);
									LOldLibraryName = Path.Combine(LOldLibraryDirectory, Schema.LibraryUtility.GetFileName(LLibrary.Name));
									if (File.Exists(LOldLibraryName))
										File.Delete(LOldLibraryName);
									throw;
								}

								if (AUpdateCatalogTimeStamp)
									AProgram.Catalog.UpdateTimeStamp();
							}
							catch
							{
								AProgram.Catalog.Libraries.Remove(LLibrary);
								throw;
							}
						}
						catch
						{
							LLibrary.Name = LOldName;
							LLibrary.SaveToFile(Path.Combine(LOldLibraryDirectory, Schema.LibraryUtility.GetFileName(LLibrary.Name))); // ensure that the file is restored to its original state
							throw;
						}
					}
					catch
					{
						AProgram.Catalog.Libraries.Add(LLibrary);
						throw;
					}
					AProgram.Catalog.Libraries.DoLibraryRenamed(AProgram, LOldName, LLibrary.Name);
				}
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			RenameLibrary(AProgram, (string)AArguments[0], (string)AArguments[1], true);
			return null;
		}
	}

	// operator RefreshLibraries(const ALibraryName : Name);
	public class SystemRefreshLibrariesNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			LibraryUtility.RefreshLibraries(AProgram);
			return null;
		}
	}
	
	// operator AttachLibraries(string ADirectory)
	// Attaches all libraries found in the given directory
	public class SystemAttachLibrariesNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryDirectory = (string)AArguments[0];
			
			Schema.Libraries LLibraries = new Schema.Libraries();
			Schema.LibraryUtility.GetAvailableLibraries(((Server)AProgram.ServerProcess.ServerSession.Server).InstanceDirectory, LLibraryDirectory, LLibraries, true);

			lock (AProgram.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library LLibrary in LLibraries)
					if (!AProgram.Catalog.Libraries.ContainsName(LLibrary.Name) && !AProgram.Catalog.ContainsName(LLibrary.Name))
						LibraryUtility.AttachLibrary(AProgram, LLibrary, false);
			}
			
			return null;
		}
	}
	
	// operator AttachLibrary(string ALibraryName, string ALibraryDirectory)
	public class SystemAttachLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			string LLibraryDirectory = (string)AArguments[1];
			
			LibraryUtility.AttachLibrary(AProgram, LLibraryName, LLibraryDirectory, false);
			
			return null;
		}
	}
	
	// operator DetachLibrary(string ALibraryName)
	public class SystemDetachLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];

			LibraryUtility.DetachLibrary(AProgram, LLibraryName);
			
			return null;
		}
	}
	
	// operator GetLibraryDescriptor(const ALibraryName : Name) : LibraryDescriptor;
	public class SystemGetLibraryDescriptorNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			lock (AProgram.Catalog.Libraries)
			{
				return (Schema.Library)AProgram.Catalog.Libraries[(string)AArguments[0]].Clone();
			}
		}
	}
	
	// operator SetLibraryDescriptor(const ALibraryName : Name, const ALibraryDescriptor : LibraryDescriptor);
	public class SystemSetLibraryDescriptorNode : InstructionNode
	{
		public static void ChangeLibraryVersion(Program AProgram, string ALibraryName, VersionNumber AVersion, bool AUpdateTimeStamp)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (!(LLibrary.Version.Equals(AVersion)))
				{
					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
					{
						foreach (Schema.LoadedLibrary LDependentLoadedLibrary in LLoadedLibrary.RequiredByLibraries)
						{
							Schema.Library LDependentLibrary = AProgram.Catalog.Libraries[LDependentLoadedLibrary.Name];
							Schema.LibraryReference LLibraryReference = LDependentLibrary.Libraries[LLibrary.Name];
							if (!VersionNumber.Compatible(LLibraryReference.Version, AVersion))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.RegisteredLibraryHasDependents, LLibrary.Name, LDependentLibrary.Name, LLibraryReference.Version.ToString());
						}
					}
					LLibrary.Version = AVersion;

					if (AUpdateTimeStamp)
						AProgram.Catalog.UpdateTimeStamp();

					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				
					if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))	
						((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetCurrentLibraryVersion(LLibrary.Name, LLibrary.Version);
				}
			}
		}
		public static void SetLibraryDefaultDeviceName(Program AProgram, string ALibraryName, string ADefaultDeviceName, bool AUpdateTimeStamp)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (LLibrary.DefaultDeviceName != ADefaultDeviceName)
				{
					LLibrary.DefaultDeviceName = ADefaultDeviceName;

					if (AUpdateTimeStamp)
						AProgram.Catalog.UpdateTimeStamp();

					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
			}
		}

		public static void AddLibraryRequisite(Program AProgram, string ALibraryName, Schema.LibraryReference ARequisiteLibrary)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (!AProgram.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProgram, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);

				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				LLibrary.Libraries.Add(ARequisiteLibrary);
				try
				{
					LibraryUtility.CheckCircularLibraryReference(AProgram, LLibrary, ARequisiteLibrary.Name);
					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
					{
						LibraryUtility.EnsureLibraryRegistered(AProgram, ARequisiteLibrary, true);
						if (!LLoadedLibrary.RequiredLibraries.Contains(ARequisiteLibrary.Name))
						{
							LLoadedLibrary.RequiredLibraries.Add(AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ARequisiteLibrary.Name));
							AProgram.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProgram.ServerProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
						LLoadedLibrary.AttachLibrary();
					}

					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					LLibrary.Libraries.Remove(ARequisiteLibrary);
					throw;
				}
			}
		}

		public static void UpdateLibraryRequisite(Program AProgram, string ALibraryName, Schema.LibraryReference AOldReference, Schema.LibraryReference ANewReference)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (String.Compare(AOldReference.Name, ANewReference.Name, false) != 0)
				{
					RemoveLibraryRequisite(AProgram, ALibraryName, AOldReference);
					AddLibraryRequisite(AProgram, ALibraryName, ANewReference);
				}
				else
				{
					if (!VersionNumber.Equals(AOldReference.Version, ANewReference.Version))
					{
						if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
						{
							// Ensure that the registered library satisfies the new requisite
							Schema.Library LRequiredLibrary = AProgram.Catalog.Libraries[AOldReference.Name];
							if (!VersionNumber.Compatible(ANewReference.Version, LRequiredLibrary.Version))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidLibraryReference, AOldReference.Name, ALibraryName, ANewReference.Version.ToString(), LRequiredLibrary.Version.ToString());
						}

						LLibrary.Libraries[AOldReference.Name].Version = ANewReference.Version;

						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
					}
				}
			}
		}

		public static void RemoveLibraryRequisite(Program AProgram, string ALibraryName, Schema.LibraryReference ARequisiteLibrary)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRemoveRequisitesFromRegisteredLibrary, LLibrary.Name);

				LLibrary.Libraries.Remove(ARequisiteLibrary);
				LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
			}
		}

		public static void AddLibraryFile(Program AProgram, string ALibraryName, Schema.FileReference AFile)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (!AProgram.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProgram, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);

				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				bool LFileExists = LLibrary.Files.Contains(AFile.FileName);
				bool LOldIsAssembly = LFileExists ? LLibrary.Files[AFile.FileName].IsAssembly : false;
				if (LFileExists)
					LLibrary.Files[AFile.FileName].IsAssembly = AFile.IsAssembly;
				else
					LLibrary.Files.Add(AFile);
				try
				{
					// ensure that all assemblies are registered
					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
						LibraryUtility.RegisterLibraryFiles(AProgram, LLibrary, LLoadedLibrary);

					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					if (LFileExists)
						LLibrary.Files[AFile.FileName].IsAssembly = LOldIsAssembly;
					else
						LLibrary.Files.Remove(AFile);
					throw;
				}
			}
		}
		
		public static void RemoveLibraryFile(Program AProgram, string ALibraryName, Schema.FileReference AFile)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				if (LLibrary.Files[AFile.FileName].Environments.Count > 0)
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, String.Format("File '{0}' in library '{1}' cannot be removed because it has environments defined.", AFile.FileName, ALibraryName));
					
				FileReference LRemovedFile = LLibrary.Files.RemoveAt(LLibrary.Files.IndexOf(AFile.FileName));
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					LLibrary.Files.Add(LRemovedFile);
					throw;
				}
			}
		}
		
		public static void AddLibraryFileEnvironment(Program AProgram, string ALibraryName, string AFileName, string AEnvironment)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (!AProgram.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProgram, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);

				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				int LFileIndex = LLibrary.Files.IndexOf(AFileName);
				bool LFileAdded = false;
				if (LFileIndex == 0)
				{
					LFileAdded = true;
					LFileIndex = LLibrary.Files.Add(new FileReference(AFileName));
				}
					
				LLibrary.Files[LFileIndex].Environments.Add(AEnvironment);
				try
				{
					// ensure that all assemblies are registered
					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
						LibraryUtility.RegisterLibraryFiles(AProgram, LLibrary, LLoadedLibrary);

					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					LLibrary.Files[LFileIndex].Environments.Remove(AEnvironment);
					if (LFileAdded)
						LLibrary.Files.RemoveAt(LFileIndex);
					throw;
				}
			}
		}
		
		public static void RemoveLibraryFileEnvironment(Program AProgram, string ALibraryName, string AFileName, string AEnvironment)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				LLibrary.Files[AFileName].Environments.Remove(AEnvironment);
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					LLibrary.Files[AFileName].Environments.Add(AEnvironment);
					throw;
				}
			}
		}

		public static void AddLibrarySetting(Program AProgram, string ALibraryName, Tag ASetting)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (!AProgram.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProgram, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);

				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (LLibrary.MetaData == null)
					LLibrary.MetaData = new MetaData();

				LLibrary.MetaData.Tags.Add(ASetting);
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					LLibrary.MetaData.Tags.Remove(ASetting.Name);
					throw;
				}
			}
		}

		public static void UpdateLibrarySetting(Program AProgram, string ALibraryName, Tag AOldSetting, Tag ANewSetting)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (String.Compare(AOldSetting.Name, ANewSetting.Name, false) != 0)
				{
					LLibrary.MetaData.Tags.Remove(AOldSetting.Name);
					LLibrary.MetaData.Tags.Add(ANewSetting);
				}
				else
					LLibrary.MetaData.Tags.Update(AOldSetting.Name, ANewSetting.Value);
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					if (String.Compare(AOldSetting.Name, ANewSetting.Name, false) != 0)
					{
						LLibrary.MetaData.Tags.Remove(ANewSetting.Name);
						LLibrary.MetaData.Tags.Add(AOldSetting);
					}
					else
					{
						LLibrary.MetaData.Tags.Update(AOldSetting.Name, AOldSetting.Value);
					}
					throw;
				}
			}
		}

		public static void RemoveLibrarySetting(Program AProgram, string ALibraryName, Tag ATag)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				LLibrary.MetaData.Tags.Remove(ATag.Name);
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LLibrary.Name)));
				}
				catch
				{
					LLibrary.MetaData.Tags.Add(ATag);
					throw;
				}
			}
		}

		public static void SetLibraryDescriptor(Program AProgram, string AOldLibraryName, Schema.Library ANewLibrary, bool AUpdateTimeStamp)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LOldLibrary = AProgram.Catalog.Libraries[AOldLibraryName];

				if (Schema.Object.NamesEqual(LOldLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LOldLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (LOldLibrary.Name != ANewLibrary.Name)
					Compiler.CheckValidCatalogObjectName(AProgram.Plan, null, ANewLibrary.Name);

				if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LOldLibrary.Name))
				{
					foreach (Schema.LibraryReference LReference in ANewLibrary.Libraries)
						LibraryUtility.CheckCircularLibraryReference(AProgram, ANewLibrary, LReference.Name);

					// cannot rename a loaded library
					if (LOldLibrary.Name != ANewLibrary.Name)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRenameRegisteredLibrary, LOldLibrary.Name);

					// cannot change the version of a loaded library
					if (!(LOldLibrary.Version.Equals(ANewLibrary.Version)))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotChangeRegisteredLibraryVersion, LOldLibrary.Name);

					// cannot remove a required library
					foreach (Schema.LibraryReference LReference in LOldLibrary.Libraries)
						if (!ANewLibrary.Libraries.Contains(LReference))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRemoveRequisitesFromRegisteredLibrary, LOldLibrary.Name);

					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ANewLibrary.Name);
					foreach (Schema.LibraryReference LReference in ANewLibrary.Libraries)
					{
						LibraryUtility.EnsureLibraryRegistered(AProgram, LReference, true);
						if (!LLoadedLibrary.RequiredLibraries.Contains(LReference.Name))
						{
							LLoadedLibrary.RequiredLibraries.Add(AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
							AProgram.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProgram.ServerProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
					}
					LLoadedLibrary.AttachLibrary();

					// ensure that all assemblies are registered
					LibraryUtility.RegisterLibraryFiles(AProgram, ANewLibrary, AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ANewLibrary.Name));
				}

				AProgram.Catalog.Libraries.Remove(LOldLibrary);
				try
				{
					AProgram.Catalog.Libraries.Add(ANewLibrary);
					try
					{
						if (AUpdateTimeStamp)
							AProgram.Catalog.UpdateTimeStamp();

						string LLibraryDirectory = ANewLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);
						string LLibraryName = Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(ANewLibrary.Name));
						try
						{
							if (LOldLibrary.Name != ANewLibrary.Name)
							{
								string LOldLibraryDirectory = LOldLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory);
								string LOldLibraryName = Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(LOldLibrary.Name));
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(LOldLibraryDirectory, true);
								FileUtility.EnsureWriteable(LOldLibraryName);
								#endif
								Directory.Move(LOldLibraryDirectory, LLibraryDirectory);
								try
								{
									File.Delete(LOldLibraryName);
								}
								catch
								{
									Directory.Move(LLibraryDirectory, LOldLibraryDirectory);
									throw;
								}
								AProgram.Catalog.Libraries.DoLibraryRenamed(AProgram, LOldLibrary.Name, ANewLibrary.Name);
							}
							ANewLibrary.SaveToFile(LLibraryName);
						}
						catch
						{
							if (LOldLibrary.Name != ANewLibrary.Name)
								LOldLibrary.SaveToFile(Path.Combine(LOldLibrary.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory), Schema.LibraryUtility.GetFileName(LOldLibrary.Name)));
							throw;
						}
					}
					catch
					{
						AProgram.Catalog.Libraries.Remove(ANewLibrary);
						throw;
					}
				}
				catch
				{
					AProgram.Catalog.Libraries.Add(LOldLibrary);
					throw;
				}
			}
		}

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SetLibraryDescriptor(AProgram, (string)AArguments[0], (Schema.Library)AArguments[1], true);
			return null;
		}
	}

	// operator LibrarySetting(const ASettingName : String) : String;
	public class SystemLibrarySettingNode : InstructionNode
	{
		// Find the first unambiguous setting value for the given setting name in a breadth-first traversal of the library dependency graph
		private static string ResolveSetting(Plan APlan, string ASettingName)
		{
			Tag LTag = Tag.None;
			foreach (Schema.LoadedLibraries LLevel in APlan.NameResolutionPath)
			{
				List<string> LContainingLibraries = new List<string>();
				foreach (Schema.LoadedLibrary LLoadedLibrary in LLevel)
				{
					Schema.Library LLibrary = APlan.Catalog.Libraries[LLoadedLibrary.Name];
					if ((LLibrary.MetaData != null) && LLibrary.MetaData.Tags.Contains(ASettingName))
						LContainingLibraries.Add(LLibrary.Name);
				}
				
				if (LContainingLibraries.Count == 1)
				{
					LTag = APlan.Catalog.Libraries[LContainingLibraries[0]].MetaData.Tags[ASettingName];
					break;
				}
				else if (LContainingLibraries.Count > 1)
				{
					StringBuilder LBuilder = new StringBuilder();
					foreach (string LLibraryName in LContainingLibraries)
					{
						if (LBuilder.Length > 0)
							LBuilder.Append(", ");
						LBuilder.AppendFormat("{0}", LLibraryName);
					}
					
					throw new Schema.SchemaException(Schema.SchemaException.Codes.AmbiguousLibrarySetting, ASettingName, LBuilder.ToString());
				}
			}

			return LTag == Tag.None ? null : LTag.Value;
		}

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ResolveSetting(AProgram.Plan, (string)AArguments[0]);
		}
	}
	
	// operator RegisterLibrary(const ALibraryName : Name);	
	// operator RegisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemRegisterLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			bool LWithReconciliation = AArguments.Length > 1 ? (bool)AArguments[1] : true;
			lock (AProgram.Catalog.Libraries)
			{
				if (!AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibraryName))
					LibraryUtility.RegisterLibrary(AProgram, LLibraryName, LWithReconciliation);
			}
			return null;
		}
	}
	
	// operator EnsureLibraryRegistered(const ALibraryName : Name);
	public class SystemEnsureLibraryRegisteredNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			LibraryUtility.EnsureLibraryRegistered(AProgram, (string)AArguments[0], true);
			return null;
		}
	}
	
	// operator UnregisterLibrary(const ALibraryName : Name);
	// operator UnregisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemUnregisterLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			LibraryUtility.UnregisterLibrary(AProgram, (string)AArguments[0], AArguments.Length > 1 ? (bool)AArguments[1] : true);
			return null;
		}
	}
	
	// operator LoadLibrary(const ALibraryName : Name);
	public class SystemLoadLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		
		private static void GatherDependents(Schema.LoadedLibrary ALibrary, Schema.LoadedLibraries ADependents)
		{
			foreach (Schema.LoadedLibrary LLibrary in ALibrary.RequiredByLibraries)
			{
				if (!ADependents.ContainsName(LLibrary.Name))
				{
					GatherDependents(LLibrary, ADependents);
					ADependents.Add(LLibrary);
				}
			}
		}
		
		private static void UpgradeLibraries(Program AProgram, string[] ALibraries)
		{
			foreach (string LLibraryName in ALibraries)
				UpgradeLibrary(AProgram, LLibraryName);
		}
		
		private static void InternalUpgradeLibrary(Program AProgram, string ALibraryName, VersionNumber ACurrentVersion, VersionNumber ATargetVersion)
		{
			DataParams LParams = new DataParams();
			LParams.Add(new DataParam("ALibraryName", AProgram.DataTypes.SystemName, Modifier.Const, ALibraryName));
			LParams.Add(new DataParam("ACurrentVersion", Compiler.ResolveCatalogIdentifier(AProgram.Plan, "System.VersionNumber", true) as Schema.IDataType, Modifier.Const, ACurrentVersion));
			LParams.Add(new DataParam("ATargetVersion", Compiler.ResolveCatalogIdentifier(AProgram.Plan, "System.VersionNumber", true) as Schema.IDataType, Modifier.Const, ATargetVersion));
			IServerStatementPlan LPlan =
				((IServerProcess)AProgram.ServerProcess).PrepareStatement
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
					LParams
				);
			try
			{
				LPlan.Execute(LParams);
			}
			finally
			{
				((IServerProcess)AProgram.ServerProcess).UnprepareStatement(LPlan);
			}
		}
		
		public static void UpgradeLibrary(Program AProgram, string ALibraryName)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName);
				VersionNumber LCurrentVersion = ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetCurrentLibraryVersion(ALibraryName);
				if (VersionNumber.Compare(LLibrary.Version, LCurrentVersion) > 0)
				{
					SessionInfo LSessionInfo = new SessionInfo(LLoadedLibrary.Owner.ID == Server.CSystemUserID ? Server.CAdminUserID : LLoadedLibrary.Owner.ID, "", LLoadedLibrary.Name);
					LSessionInfo.DefaultUseImplicitTransactions = false;
					IServerSession LSession = ((Server)AProgram.ServerProcess.ServerSession.Server).ConnectAs(LSessionInfo);
					try
					{
						ServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo)) as ServerProcess;
						try
						{
							Program LProgram = new Program(LProcess);
							LProgram.Start(null);
							try
							{
								InternalUpgradeLibrary(LProgram, LLibrary.Name, LCurrentVersion, LLibrary.Version);
							}
							finally
							{
								LProgram.Stop(null);
							}
						}
						finally
						{
							LSession.StopProcess(LProcess);
						}
					}
					finally
					{
						((IServer)AProgram.ServerProcess.ServerSession.Server).Disconnect(LSession);
					}
					((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SetCurrentLibraryVersion(ALibraryName, LLibrary.Version);
				}
			}				
		}
		
		private static void GatherRequisites(Schema.LoadedLibrary ALibrary, Schema.LoadedLibraries ARequisites)
		{
			foreach (Schema.LoadedLibrary LLibrary in ALibrary.RequiredLibraries)
				GatherRequisites(LLibrary, ARequisites);
			
			if (!ARequisites.Contains(ALibrary.Name))	
				ARequisites.Add(ALibrary);
		}
		
		public static void UpgradeLibraries(Program AProgram)
		{
			Schema.LoadedLibraries LLibraries = new Schema.LoadedLibraries();
			foreach (Schema.LoadedLibrary LLibrary in AProgram.Catalog.LoadedLibraries)
				GatherRequisites(LLibrary, LLibraries);
			string[] LLibraryArray = new string[LLibraries.Count];
			for (int LIndex = 0; LIndex < LLibraries.Count; LIndex++)
				LLibraryArray[LIndex] = LLibraries[LIndex].Name;
			UpgradeLibraries(AProgram, LLibraryArray);
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			Schema.LoadedLibrary LLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibraryName);
			Schema.LoadedLibraries LRequisites = new Schema.LoadedLibraries();
			GatherRequisites(LLibrary, LRequisites);
			string[] LLibraries = new string[LRequisites.Count];
			for (int LIndex = 0; LIndex < LRequisites.Count; LIndex++)
				LLibraries[LIndex] = LRequisites[LIndex].Name;
			UpgradeLibraries(AProgram, LLibraries);
			return null;
		}
	}

	// operator UpgradeLibraries();	
	public class SystemUpgradeLibrariesNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SystemUpgradeLibraryNode.UpgradeLibraries(AProgram);
			return null;
		}
	}

	// operator UnloadLibrary(const ALibraryName : Name);
	public class SystemUnloadLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "UnloadLibrary is obsolete. Use UnregisterLibrary instead.");
		}
	}
	
	public class SystemSaveLibraryCatalogNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "SaveLibraryCatalog is obsolete. Use ScriptCatalog instead.");
		}
	}
	
	public class UpgradeUtility : System.Object
	{
		public const string CUpgradeDirectory = "Upgrades";
		public const string CUpgradeFileName = "Upgrade";
		
		public static bool IsValidVersionNumber(VersionNumber AVersion)
		{
			return (AVersion.Revision >= 0) && (AVersion.Build < 0);
		}
		
		public static void CheckValidVersionNumber(VersionNumber AVersion)
		{
			// VersionNumbers used in Upgrades must be specified to the revision number only
			if (!IsValidVersionNumber(AVersion))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidUpgradeVersionNumber, AVersion.ToString());
		}
		
		public static VersionNumber GetVersionFromFileName(string AFileName)
		{
			VersionNumber LResult = VersionNumber.Parse(AFileName.Substring(AFileName.IndexOf(".") + 1, AFileName.IndexOf(".d4") - (AFileName.IndexOf(".") + 1)));
			CheckValidVersionNumber(LResult);
			return LResult;
		}
		
		public static string GetFileNameFromVersion(VersionNumber AVersion)
		{
			CheckValidVersionNumber(AVersion);
			return String.Format("{0}.{1}.d4", CUpgradeFileName, AVersion.ToString().Replace(".*", ""));
		}
		
		public static string GetRequisitesFileNameFromVersion(VersionNumber AVersion)
		{
			CheckValidVersionNumber(AVersion);
			return String.Format("{0}.{1}.d4r", CUpgradeFileName, AVersion.ToString().Replace(".*", ""));
		}

		public static string GetUpgradeDirectory(Program AProgram, string ALibraryName, string ALibraryDirectory)
		{
			return Path.Combine(Schema.LibraryUtility.GetLibraryDirectory(((Server)AProgram.ServerProcess.ServerSession.Server).LibraryDirectory, ALibraryName, ALibraryDirectory), CUpgradeDirectory);
		}
		
		public static void EnsureUpgradeDirectory(Program AProgram, string ALibraryName, string ALibraryDirectory)
		{
			string LUpgradeDirectory = GetUpgradeDirectory(AProgram, ALibraryName, ALibraryDirectory);
			if (!Directory.Exists(LUpgradeDirectory))
				Directory.CreateDirectory(LUpgradeDirectory);
		}
		
		public static string GetFileName(Program AProgram, string ALibraryName, string ALibraryDirectory, VersionNumber AVersion)
		{
			return Path.Combine(GetUpgradeDirectory(AProgram, ALibraryName, ALibraryDirectory), GetFileNameFromVersion(AVersion));
		}
		
		public static string GetRequisitesFileName(Program AProgram, string ALibraryName, string ALibraryDirectory, VersionNumber AVersion)
		{
			return Path.Combine(GetUpgradeDirectory(AProgram, ALibraryName, ALibraryDirectory), GetRequisitesFileNameFromVersion(AVersion));
		}
	}

	// create operator System.UpgradeVersions(const ALibraryName : Name, const ACurrentVersion : VersionNumber, const ATargetVersion : VersionNumber) : table { Version : VersionNumber };
	public class UpgradeVersionsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Version", Compiler.ResolveCatalogIdentifier(APlan, "System.VersionNumber", true) as Schema.ScalarType));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Version"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(Program AProgram, Table ATable, Row ARow, VersionNumber AVersion)
		{
			ARow[0] = AVersion;
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
					PopulateUpgradeVersions(AProgram, LResult, LRow, (string)Nodes[0].Execute(AProgram));
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
		public static string LoadUpgrade(Program AProgram, Schema.Library ALibrary, VersionNumber AVersion)
		{
			StreamReader LReader = new StreamReader(UpgradeUtility.GetFileName(AProgram, ALibrary.Name, ALibrary.Directory, AVersion));
			try
			{
				return LReader.ReadToEnd();
			}
			finally
			{
				LReader.Close();
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return 
				LoadUpgrade
				(
					AProgram, 
					AProgram.Catalog.Libraries[(string)AArguments[0]], 
					(VersionNumber)AArguments[1]
				);
		}
	}
	
	// create operator System.SaveUpgrade(const ALibraryName : Name, const AVersion : VersionNumber, const AScript : String);
	public class SaveUpgradeNode : InstructionNode
	{
		public static void SaveUpgrade(Program AProgram, Schema.Library ALibrary, VersionNumber AVersion, string AScript)
		{
			UpgradeUtility.EnsureUpgradeDirectory(AProgram, ALibrary.Name, ALibrary.Directory);
			string LFileName = UpgradeUtility.GetFileName(AProgram, ALibrary.Name, ALibrary.Directory, AVersion);
			FileUtility.EnsureWriteable(LFileName);
			StreamWriter LWriter = new StreamWriter(LFileName, false);
			try
			{
				LWriter.Write(AScript);
				LWriter.Flush();
			}
			finally
			{
				LWriter.Close();
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[1] == null)
				InjectUpgradeNode.InjectUpgrade(AProgram, AProgram.Catalog.Libraries[(string)AArguments[0]], (string)AArguments[2]);
			else
				SaveUpgrade(AProgram, AProgram.Catalog.Libraries[(string)AArguments[0]], (VersionNumber)AArguments[1], (string)AArguments[2]);
			return null;
		}
	}
	
	// create operator System.DeleteUpgrade(const ALibraryName : Name, const AVersion : VersionNumber);
	public class DeleteUpgradeNode : InstructionNode
	{
		public static void DeleteUpgrade(Program AProgram, Schema.Library ALibrary, VersionNumber AVersion)
		{
			string LFileName = UpgradeUtility.GetFileName(AProgram, ALibrary.Name, ALibrary.Directory, AVersion);
			FileUtility.EnsureWriteable(LFileName);
			File.Delete(LFileName);
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			DeleteUpgrade(AProgram, AProgram.Catalog.Libraries[(string)AArguments[0]], (VersionNumber)AArguments[1]);
			return null;
		}
	}
	
	// create operator System.InjectUpgrade(const ALibraryName : Name, const AScript : String) : VersionNumber;
	public class InjectUpgradeNode : InstructionNode
	{
		public static VersionNumber InjectUpgrade(Program AProgram, Schema.Library ALibrary, string AScript)
		{
			VersionNumber LLibraryVersion = ALibrary.Version;
			if (LLibraryVersion.Revision < 0)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidUpgradeLibraryVersionNumber, ALibrary.Name, LLibraryVersion.ToString());
			SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProgram, ALibrary.Name, new VersionNumber(LLibraryVersion.Major, LLibraryVersion.Minor, LLibraryVersion.Revision + 1, LLibraryVersion.Build), true);
			try
			{
				VersionNumber LUpgradeVersion = new VersionNumber(LLibraryVersion.Major, LLibraryVersion.Minor, ALibrary.Version.Revision, -1);
				UpgradeUtility.CheckValidVersionNumber(LUpgradeVersion);
				SaveUpgradeNode.SaveUpgrade(AProgram, ALibrary, LUpgradeVersion, AScript);
				return LUpgradeVersion;
			}
			catch
			{
				SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProgram, ALibrary.Name, LLibraryVersion, false);
				throw;
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return InjectUpgrade(AProgram, AProgram.Catalog.Libraries[(string)AArguments[0]], (string)AArguments[1]);
		}
	}
}
