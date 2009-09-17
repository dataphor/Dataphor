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

	// operator CreateLibrary(const ALibraryDescriptor : LibraryDescriptor);
	public class SystemCreateLibraryNode : InstructionNode
	{
		public static void CreateLibrary(Program AProgram, Schema.Library ALibrary, bool AUpdateCatalogTimeStamp, bool AShouldNotify)
		{
			lock (AProgram.Catalog.Libraries)
			{
				string LLibraryDirectory = ALibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);
				if (AProgram.Catalog.Libraries.Contains(ALibrary.Name))
				{
					Schema.Library LExistingLibrary = AProgram.Catalog.Libraries[ALibrary.Name];

					if (ALibrary.Directory != String.Empty)
					{
						string LExistingLibraryDirectory = LExistingLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);
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
						LExistingLibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LExistingLibrary.Name)));

						AProgram.CatalogDeviceSession.SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
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
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{
							if (!Directory.Exists(LLibraryDirectory))
								Directory.CreateDirectory(LLibraryDirectory);
							ALibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(ALibrary.Name)));
						}
						finally
						{
							AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
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
						AProgram.CatalogDeviceSession.SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
				}
			}
		}
		
		public static void AttachLibrary(Program AProgram, Schema.Library ALibrary, bool AIsAttached)
		{
			lock (AProgram.Catalog.Libraries)
			{
				string LLibraryDirectory = ALibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);

				AProgram.Catalog.Libraries.Add(ALibrary);
				AProgram.Catalog.UpdateTimeStamp();
				AProgram.Catalog.Libraries.DoLibraryAdded(AProgram, ALibrary.Name);
				if ((ALibrary.Directory != String.Empty) && !AIsAttached)
					AProgram.CatalogDeviceSession.SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
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
		public static void DropLibrary(Program AProgram, string ALibraryName, bool AUpdateCatalogTimeStamp)
		{
			lock (AProgram.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProgram.Catalog.Libraries[ALibraryName];
				if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDropRegisteredLibrary, LLibrary.Name);
					
				string LLibraryDirectory = LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);

				AProgram.Catalog.Libraries.DoLibraryRemoved(AProgram, LLibrary.Name);
				AProgram.Catalog.Libraries.DoLibraryDeleted(AProgram, LLibrary.Name);
				try
				{
					AProgram.Catalog.Libraries.Remove(LLibrary);
					try
					{
						if (AUpdateCatalogTimeStamp)
							AProgram.Catalog.UpdateTimeStamp();
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{		
							if (Directory.Exists(LLibraryDirectory))
							{
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(LLibraryDirectory, true);
								#endif
								Directory.Delete(LLibraryDirectory, true);
							}
						}
						finally
						{
							AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
						
						AProgram.CatalogDeviceSession.ClearLibraryOwner(ALibraryName);
						AProgram.CatalogDeviceSession.ClearCurrentLibraryVersion(ALibraryName);
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
					
				string LLibraryDirectory = LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);

				AProgram.Catalog.Libraries.DoLibraryRemoved(AProgram, LLibrary.Name);
				AProgram.Catalog.Libraries.Remove(LLibrary);
				AProgram.Catalog.UpdateTimeStamp();
				AProgram.CatalogDeviceSession.DeleteLibraryDirectory(LLibrary.Name);
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			DropLibrary(AProgram, (string)AArguments[0], true);
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
						
						string LOldLibraryDirectory = Schema.Library.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory, LOldName, LLibrary.Directory);
						string LLibraryDirectory = LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);
						string LOldLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LOldName));
						string LLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LLibrary.Name));
						try
						{
							AProgram.Catalog.Libraries.Add(LLibrary);
							try
							{
								AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
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
										LOldLibraryName = Path.Combine(LOldLibraryDirectory, Schema.Library.GetFileName(LLibrary.Name));
										if (File.Exists(LOldLibraryName))
											File.Delete(LOldLibraryName);
										throw;
									}
								}
								finally
								{
									AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
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
							AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
							try
							{
								LLibrary.SaveToFile(Path.Combine(LOldLibraryDirectory, Schema.Library.GetFileName(LLibrary.Name))); // ensure that the file is restored to its original state
							}
							finally
							{
								AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
							}
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
		private static void EnsureLibraryUnregistered(Program AProgram, Schema.Library ALibrary, bool AWithReconciliation)
		{
			Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibrary.Name, false);
			if (LLoadedLibrary != null)
			{
				while (LLoadedLibrary.RequiredByLibraries.Count > 0)
					EnsureLibraryUnregistered(AProgram, AProgram.Catalog.Libraries[LLoadedLibrary.RequiredByLibraries[0].Name], AWithReconciliation);
				SystemUnregisterLibraryNode.UnregisterLibrary(AProgram, ALibrary.Name, AWithReconciliation);
			}
		}
		
		private static void RemoveLibrary(Program AProgram, Schema.Library ALibrary)
		{
			// Ensure that the library and any dependencies of it are unregistered
			EnsureLibraryUnregistered(AProgram, ALibrary, false);
			SystemDropLibraryNode.DetachLibrary(AProgram, ALibrary.Name);
		}
		
		public static void RefreshLibraries(Program AProgram)
		{
			// Get the list of available libraries from the library directory
			Schema.Libraries LLibraries = new Schema.Libraries();
			Schema.Libraries LOldLibraries = new Schema.Libraries();
			Schema.Library.GetAvailableLibraries(AProgram.ServerProcess.ServerSession.Server.InstanceDirectory, AProgram.ServerProcess.ServerSession.Server.LibraryDirectory, LLibraries);
			
			lock (AProgram.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library LLibrary in LLibraries)
					if (!AProgram.Catalog.Libraries.ContainsName(LLibrary.Name) && !AProgram.Catalog.ContainsName(LLibrary.Name))
						SystemCreateLibraryNode.AttachLibrary(AProgram, LLibrary, false);
						
				// Ensure that each library in the DAE is supported by a library in the directory, remove non-existent libraries
				// The System library is not required to have a directory.
				foreach (Schema.Library LLibrary in AProgram.Catalog.Libraries)
					if ((Server.CSystemLibraryName != LLibrary.Name) && (LLibrary.Directory == String.Empty) && !LLibraries.ContainsName(LLibrary.Name))
						LOldLibraries.Add(LLibrary);
						
				foreach (Schema.Library LLibrary in LOldLibraries)
					RemoveLibrary(AProgram, LLibrary);
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			RefreshLibraries(AProgram);
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
			Schema.Library.GetAvailableLibraries(AProgram.ServerProcess.ServerSession.Server.InstanceDirectory, LLibraryDirectory, LLibraries, true);

			lock (AProgram.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library LLibrary in LLibraries)
					if (!AProgram.Catalog.Libraries.ContainsName(LLibrary.Name) && !AProgram.Catalog.ContainsName(LLibrary.Name))
						SystemCreateLibraryNode.AttachLibrary(AProgram, LLibrary, false);
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
			
			AttachLibrary(AProgram, LLibraryName, LLibraryDirectory, false);
			
			return null;
		}
		
		/// <summary>Attaches the library given by ALibraryName from ALibraryDirectory. AIsAttached indicates whether this library is being attached as part of catalog startup.</summary>
		public static void AttachLibrary(Program AProgram, string ALibraryName, string ALibraryDirectory, bool AIsAttached)
		{
			Schema.Library LLibrary = Schema.Library.GetAvailableLibrary(AProgram.ServerProcess.ServerSession.Server.InstanceDirectory, ALibraryName, ALibraryDirectory);
			if ((LLibrary != null) && !AProgram.Catalog.Libraries.ContainsName(LLibrary.Name))
				SystemCreateLibraryNode.AttachLibrary(AProgram, LLibrary, AIsAttached);
		}
	}
	
	// operator DetachLibrary(string ALibraryName)
	public class SystemDetachLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			SystemDropLibraryNode.DetachLibrary(AProgram, LLibraryName);
			
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
				Schema.Library LLibrary = AProgram.Catalog.Libraries[(string)AArguments[0]];
				return (Schema.Library)LLibrary.Clone();
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

					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				
					if (AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))	
						AProgram.CatalogDeviceSession.SetCurrentLibraryVersion(LLibrary.Name, LLibrary.Version);
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

					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
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
					SystemRegisterLibraryNode.CheckCircularLibraryReference(AProgram, LLibrary, ARequisiteLibrary.Name);
					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
					{
						SystemRegisterLibraryNode.EnsureLibraryRegistered(AProgram, ARequisiteLibrary, true);
						if (!LLoadedLibrary.RequiredLibraries.Contains(ARequisiteLibrary.Name))
						{
							LLoadedLibrary.RequiredLibraries.Add(AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ARequisiteLibrary.Name));
							AProgram.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProgram.ServerProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
						LLoadedLibrary.AttachLibrary();
					}

					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
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

						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{
							LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
						}
						finally
						{
							AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
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
				AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
				}
				finally
				{
					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
				}
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

				LLibrary.Files.Add(AFile);
				try
				{
					// ensure that all assemblies are registered
					Schema.LoadedLibrary LLoadedLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
						SystemRegisterLibraryNode.RegisterLibraryFiles(AProgram, LLibrary, LLoadedLibrary);

					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
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

				LLibrary.Files.Remove(AFile);
				try
				{
					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					LLibrary.Files.Add(AFile);
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
					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
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
					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
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
					AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
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
						SystemRegisterLibraryNode.CheckCircularLibraryReference(AProgram, ANewLibrary, LReference.Name);

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
						SystemRegisterLibraryNode.EnsureLibraryRegistered(AProgram, LReference, true);
						if (!LLoadedLibrary.RequiredLibraries.Contains(LReference.Name))
						{
							LLoadedLibrary.RequiredLibraries.Add(AProgram.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
							AProgram.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProgram.ServerProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
					}
					LLoadedLibrary.AttachLibrary();

					// ensure that all assemblies are registered
					SystemRegisterLibraryNode.RegisterLibraryFiles(AProgram, ANewLibrary, AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ANewLibrary.Name));
				}

				AProgram.Catalog.Libraries.Remove(LOldLibrary);
				try
				{
					AProgram.Catalog.Libraries.Add(ANewLibrary);
					try
					{
						if (AUpdateTimeStamp)
							AProgram.Catalog.UpdateTimeStamp();

						string LLibraryDirectory = ANewLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);
						string LLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(ANewLibrary.Name));
						AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{
							try
							{
								if (LOldLibrary.Name != ANewLibrary.Name)
								{
									string LOldLibraryDirectory = LOldLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory);
									string LOldLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LOldLibrary.Name));
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
									LOldLibrary.SaveToFile(Path.Combine(LOldLibrary.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LOldLibrary.Name)));
								throw;
							}
						}
						finally
						{
							AProgram.ServerProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
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
		private string ResolveSetting(Plan APlan, string ASettingName)
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
		public const string CRegisterFileName = @"Documents\Register.d4";
		public const string CRegisterDocumentLocator = @"doc:{0}:Register";

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
						
						AProgram.ServerProcess.ServerSession.Server.DoLibraryLoading(LLibrary.Name);
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

									AProgram.CatalogDeviceSession.SetCurrentLibraryVersion(LLibrary.Name, LLibrary.Version);
									AProgram.CatalogDeviceSession.SetLibraryOwner(LLoadedLibrary.Name, LLoadedLibrary.Owner.ID);
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
							AProgram.ServerProcess.ServerSession.Server.DoLibraryLoaded(ALibraryName);
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
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			bool LWithReconciliation = AArguments.Length > 1 ? (bool)AArguments[1] : true;
			lock (AProgram.Catalog.Libraries)
			{
				if (!AProgram.CatalogDeviceSession.IsLoadedLibrary(LLibraryName))
					RegisterLibrary(AProgram, LLibraryName, LWithReconciliation);
			}
			return null;
		}
	}
	
	// operator EnsureLibraryRegistered(const ALibraryName : Name);
	public class SystemEnsureLibraryRegisteredNode : InstructionNode
	{
		public static void EnsureLibraryRegistered(Program AProgram, string ALibraryName, bool AWithReconciliation)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (AProgram.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName, false) == null)
					SystemRegisterLibraryNode.RegisterLibrary(AProgram, ALibraryName, AWithReconciliation);
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			EnsureLibraryRegistered(AProgram, (string)AArguments[0], true);
			return null;
		}
	}
	
	// operator UnregisterLibrary(const ALibraryName : Name);
	// operator UnregisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemUnregisterLibraryNode : InstructionNode
	{
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
						
						if (Schema.Object.NamesEqual(LLoadedLibrary.Name, Server.CSystemLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterSystemLibrary);
							
						if (Schema.Object.NamesEqual(LLoadedLibrary.Name, Server.CGeneralLibraryName))
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
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			UnregisterLibrary(AProgram, (string)AArguments[0], AArguments.Length > 1 ? (bool)AArguments[1] : true);
			return null;
		}
	}
	
	// operator LoadLibrary(const ALibraryName : Name);
	public class SystemLoadLibraryNode : InstructionNode
	{
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
					VersionNumber LCurrentVersion = AProgram.CatalogDeviceSession.GetCurrentLibraryVersion(ALibraryName);
					
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
							SystemRegisterLibraryNode.RegisterLibraryFiles(AProgram, LLibrary, LLoadedLibrary);
							
							LAreAssembliesRegistered = true;

							AProgram.CatalogDeviceSession.InsertLoadedLibrary(LLoadedLibrary);
							LLoadedLibrary.AttachLibrary();
							try
							{
								AProgram.CatalogDeviceSession.SetLibraryOwner(LLoadedLibrary.Name, LLoadedLibrary.Owner.ID);
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
							SystemUnregisterLibraryNode.UnregisterLibrary(AProgram, ALibraryName, false);
						else if (LAreAssembliesRegistered)
							SystemRegisterLibraryNode.UnregisterLibraryAssemblies(AProgram, LLoadedLibrary);
							
						throw;
					}

					AProgram.CatalogDeviceSession.SetCurrentLibraryVersion(LLibrary.Name, LCurrentVersion); // Once a library has loaded, record the version number
					
					AProgram.Catalog.Libraries.DoLibraryLoaded(AProgram, LLibrary.Name);
				}
				catch
				{
					if (AProgram.ServerProcess.ServerSession.Server.State == ServerState.Started)
						throw;
				}
			}
		}
		
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
				VersionNumber LCurrentVersion = AProgram.CatalogDeviceSession.GetCurrentLibraryVersion(ALibraryName);
				if (VersionNumber.Compare(LLibrary.Version, LCurrentVersion) > 0)
				{
					SessionInfo LSessionInfo = new SessionInfo(LLoadedLibrary.Owner.ID == Server.CSystemUserID ? Server.CAdminUserID : LLoadedLibrary.Owner.ID, "", LLoadedLibrary.Name);
					LSessionInfo.DefaultUseImplicitTransactions = false;
					IServerSession LSession = AProgram.ServerProcess.ServerSession.Server.ConnectAs(LSessionInfo);
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
					AProgram.CatalogDeviceSession.SetCurrentLibraryVersion(ALibraryName, LLibrary.Version);
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
			return Path.Combine(Schema.Library.GetLibraryDirectory(AProgram.ServerProcess.ServerSession.Server.LibraryDirectory, ALibraryName, ALibraryDirectory), CUpgradeDirectory);
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
