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
		
		#endregion
	}
}
