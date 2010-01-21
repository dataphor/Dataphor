/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Drawing;

using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public class LibraryListNode : BrowseNode
	{
		public LibraryListNode()
		{
			Text = "Libraries";
			ImageIndex = 3;
			SelectedImageIndex = ImageIndex;
		}
		
		private MenuItem FAttachMenuItem;
		private MenuItem FAttachLibrariesMenuItem;

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			FAttachMenuItem = new MenuItem(Strings.ObjectTree_AttachMenuText, new EventHandler(AttachClicked));
			LMenu.MenuItems.Add(1, FAttachMenuItem);
			FAttachLibrariesMenuItem = new MenuItem(Strings.ObjectTree_AttachLibrariesMenuText, new EventHandler(AttachLibrariesClicked));
			LMenu.MenuItems.Add(2, FAttachLibrariesMenuItem);
			return LMenu;
		}

		protected override string GetChildExpression()
		{
			return 
				@"
					.System.Libraries 
						left join .System.LoadedLibraries 
							include rowexists IsLoaded 
						left join (.System.LibraryVersions rename { LibraryName Name, Version LoadedVersion })
						add { IfNil(LoadedVersion < Version, false) UpgradeRequired }
						rename Main
				";
		}
		
		protected void AttachClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = Dataphoria.FrontendSession.LoadForm(null, "Derive('System.AttachLibrary', 'Add', '', '', false)", new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				if (LForm.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
					throw new AbortException();
				BaseNode LNode = CreateChildNode(LForm.MainSource.DataView.ActiveRow);
				AddNode(LNode);
				TreeView.SelectedNode = LNode;
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		protected void AttachLibrariesClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = Dataphoria.FrontendSession.LoadForm(null, "Derive('System.AttachLibraries', 'Add', '', '', false)", new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				if (LForm.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				LForm.HostNode.Dispose();
			}

			Refresh();
		}

		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			LibraryNode LNode = new LibraryNode(this, (string)ARow["Main.Name"]);
			if (ARow.DataType.Columns.ContainsName("Main.IsLoaded"))
				UpdateNode(LNode, ARow);
			return LNode;
		}

		private void UpdateNode(LibraryNode ANode, DAE.Runtime.Data.Row ARow)
		{
			ANode.Registered = (bool)ARow["Main.IsLoaded"];
			ANode.CanLoad = true; //(bool)ARow["Main.CanLoad"];
			ANode.IsSuspect = (bool)ARow["Main.IsSuspect"];
			ANode.UpgradeRequired = (bool)ARow["Main.UpgradeRequired"];
		}

		protected override string AddDocument()
		{
			return ".Frontend.Derive('System.Libraries', 'Add')";
		}
		
		public void RefreshRegistered()
		{
			DAE.IServerCursor LCursor = Dataphoria.OpenCursor(GetChildExpression(), GetParams());
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					LibraryNode LNode;
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						LNode = FindByKey(LRow) as LibraryNode;
						if (LNode != null)
							UpdateNode(LNode, LRow);
					}
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				Dataphoria.CloseCursor(LCursor);
			}
		}
		
		public void RefreshCurrent()
		{
			string LCurrentLibrary = Dataphoria.GetCurrentLibraryName();
			foreach (LibraryNode LNode in Nodes)
			{
				LNode.Current = LNode.LibraryName == LCurrentLibrary;
			}
		}

		protected override void InternalRefresh()
		{
			Dataphoria.ExecuteScript("System.RefreshLibraries();");
			base.InternalRefresh();
			RefreshCurrent();
			RefreshRegistered();
		}

		protected override void InternalReconcileChildren()
		{
			base.InternalReconcileChildren();
			RefreshCurrent();
		}
	}

	public class LibraryNode : EditableItemNode
	{
		public LibraryNode(LibraryListNode ANode, string ALibraryName) : base()
		{
			FLibraryName = ALibraryName;
			UpdateText();
			UpdateImageIndex();
		}

		public override BuildMode BuildMode
		{
			get { return BuildMode.OnExpand; }
		}

		public override bool IsEqual(DAE.Runtime.Data.Row ARow)
		{
			return ((string)ARow["Main.Name"] == FLibraryName);
		}

		public override string GetFilter()
		{
			return String.Format("Main.Name = '{0}'", FLibraryName);
		}

		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private bool FCanLoad = true;
		public bool CanLoad
		{
			get { return FCanLoad; }
			set { FCanLoad = value; }
		}
		
		private bool FUpgradeRequired;
		public bool UpgradeRequired
		{
			get { return FUpgradeRequired; }
			set 
			{ 
				if (FUpgradeRequired != value)
				{
					FUpgradeRequired = value; 
					UpdateImageIndex();
				}
			}
		}

		private bool FIsSuspect;
		public bool IsSuspect
		{
			get { return FIsSuspect; }
			set
			{
				if (FIsSuspect = value)
				{
					FIsSuspect = value;
					UpdateImageIndex();
				}
			}
		}
		
		private bool FRegistered;
		public bool Registered
		{
			get { return FRegistered; }
			set
			{
				if (FRegistered != value)
				{
					FRegistered = value;
					UpdateImageIndex();
					UpdateText();
					Refresh();
				}
			}
		}

		private bool FCurrent;
		public bool Current
		{
			get { return FCurrent; }
			set
			{
				if (FCurrent != value)
				{
					FCurrent = value;
					UpdateImageIndex();
					UpdateText();
				}
			}
		}
		
		private void UpdateImageIndex()
		{
			if (FRegistered)
				if (FCurrent)
					if (FUpgradeRequired)
						ImageIndex = 28;
					else
						ImageIndex = 25;
				else
					if (FUpgradeRequired)
						ImageIndex = 27;
					else
						ImageIndex = 24;
			else
				if (IsSuspect)
					ImageIndex = 26;
				else
					ImageIndex = 6;
			SelectedImageIndex = ImageIndex;
		}

		protected override void UpdateText()
		{
			if (FCurrent)
			{
				NodeFont = new Font(TreeView.Font, FontStyle.Bold);
				// HACK: add trailing spaces to work around bold cut off bug (tried setting the TreeView's font to no avail)
				int LBlankCount;
				using (Graphics LGraphics = TreeView.CreateGraphics())
				{
					LBlankCount = 
						(int)Math.Ceiling
						(
							(
								LGraphics.MeasureString(FLibraryName, NodeFont).Width - 
								LGraphics.MeasureString(FLibraryName, TreeView.Font).Width
							) / 
							LGraphics.MeasureString(" ", NodeFont).Width
						) + 3;
				}
				System.Text.StringBuilder LBuilder = new System.Text.StringBuilder(FLibraryName, FLibraryName.Length + LBlankCount);
				while (LBlankCount > 0)
				{
					LBuilder.Append(' ');
					LBlankCount--;
				}
				Text = LBuilder.ToString();
			}
			else
			{
				NodeFont = null;
				Text = FLibraryName;
			}
		}

		private MenuItem FDetachMenuItem;		
		private MenuItem FLoadMenuItem;
		private MenuItem FRegisterMenuItem;
		private MenuItem FSetAsCurrentMenuItem;
		private MenuItem FUpgradeLibraryMenuItem;
		//private MenuItem FScriptChangesMenuItem;
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			
			LMenu.MenuItems.Add(new MenuItem("-"));	

			FRegisterMenuItem = new MenuItem(Strings.ObjectTree_RegisterMenuText, new EventHandler(RegisterToggleClicked));
			LMenu.MenuItems.Add(FRegisterMenuItem);
			
			FLoadMenuItem = new MenuItem(Strings.ObjectTree_LoadMenuText, new EventHandler(LoadToggleClicked));
			LMenu.MenuItems.Add(FLoadMenuItem);
			
			FDetachMenuItem = new MenuItem(Strings.ObjectTree_DetachMenuText, new EventHandler(DetachClicked));
			LMenu.MenuItems.Add(FDetachMenuItem);

			FSetAsCurrentMenuItem = new MenuItem(Strings.ObjectTree_SetAsCurrentMenuText, new EventHandler(SetAsCurrentClicked));
			FSetAsCurrentMenuItem.DefaultItem = true;
			LMenu.MenuItems.Add(FSetAsCurrentMenuItem);

			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_OpenRegisterScriptMenuText, new EventHandler(OpenRegisterScriptClicked)));
			LMenu.MenuItems.Add(new MenuItem("-"));	
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_UpgradesMenuText, new EventHandler(UpgradesClicked)));
			
			FUpgradeLibraryMenuItem = new MenuItem(Strings.ObjectTree_UpgradeLibraryMenuText, new EventHandler(UpgradeLibraryClicked));
			LMenu.MenuItems.Add(FUpgradeLibraryMenuItem);

			//FScriptChangesMenuItem = new MenuItem(Strings.Get("ObjectTree.ScriptChangesText"), new EventHandler(ScriptChangesClicked));
			//LMenu.MenuItems.Add(FScriptChangesMenuItem);

			return LMenu;
		}

		protected override void UpdateContextMenu(ContextMenu AMenu)
		{
			FSetAsCurrentMenuItem.Enabled = Registered && !Current;
			FRegisterMenuItem.Text = Registered ? Strings.ObjectTree_UnregisterMenuText : Strings.ObjectTree_RegisterMenuText;
			FLoadMenuItem.Text = Registered ? Strings.ObjectTree_UnloadMenuText : Strings.ObjectTree_LoadMenuText;
			FDetachMenuItem.Enabled = !Registered;
			FRegisterMenuItem.Enabled = !((LibraryName == "System") || (LibraryName == "General") || (LibraryName == "Frontend") || (LibraryName == "SimpleDevice"));
			FLoadMenuItem.Enabled = FRegisterMenuItem.Enabled && (Registered || CanLoad);
			FUpgradeLibraryMenuItem.Enabled = Registered && UpgradeRequired;
			//FScriptChangesMenuItem.Enabled = Registered;
		}
		
		private void DetachClicked(object ASender, EventArgs AArgs)
		{
			using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(".Frontend.Derive('.System.DetachLibrary', 'Delete', false)", LForm);
				LForm.MainSource.Filter = GetFilter();
				LForm.HostNode.Open();
				if (LForm.ShowModal(Frontend.Client.FormMode.Delete) != DialogResult.OK)
					throw new AbortException();
			}
			Remove();
		}

		private void RegisterToggleClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.Warnings.ClearErrors(Dataphoria);
			if (!Registered)
			{
				try
				{
					DAE.IServerCursor LCursor = 
						Dataphoria.OpenCursor
						(
							String.Format
							(
								@"
									select 
										RequiredLibraries('{0}') 
											group by {{ Library_Name }}
											add {{ Max(Level) Level }}
											where not exists (System.LoadedLibraries where Name = Library_Name) 
											order by {{ Level desc }};
								", 
								FLibraryName
							)
						);
					try
					{
						using (DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow())
						{
							while (LCursor.Next())
							{
								LCursor.Select(LRow);
								try
								{
									Dataphoria.ExecuteScript(String.Format("RegisterLibrary('{0}');", (string)LRow["Library_Name"]));
								}
								catch (Exception LException)
								{
									Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
								}
							}
						}
					}
					finally
					{
						Dataphoria.CloseCursor(LCursor);
					}
					
					try
					{
						Dataphoria.ExecuteScript(String.Format("RegisterLibrary(\"{0}\");", FLibraryName));
					}
					catch (Exception LException)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
					}
				}
				finally
				{
					((LibraryListNode)Parent).RefreshRegistered();
				}
				((LibraryListNode)Parent).RefreshCurrent();
			}
			else
			{
				try
				{
					using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
					{
						Dataphoria.FrontendSession.CreateHost().Load(".Frontend.Form('Frontend', 'UnregisterLibrary')", LForm);
						LForm.MainSource.Filter = GetFilter();
						LForm.HostNode.Open();
						if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
							throw new AbortException();
						DAE.Client.DataView LView = ((Frontend.Client.ISource)LForm.FindNode("Dependencies")).DataView;
						LView.First();
						foreach (DAE.Runtime.Data.Row LRow in LView)
						{
							try
							{
								Dataphoria.ExecuteScript(String.Format("UnregisterLibrary(\"{0}\");", (string)LRow["Library_Name"]));
							}
							catch (Exception LException)
							{
								Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
							}
						}
					}

					try
					{
						Dataphoria.ExecuteScript(String.Format("UnregisterLibrary(\"{0}\");", FLibraryName));
					}
					catch (Exception LException)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
					}
				}
				finally
				{
					((LibraryListNode)Parent).RefreshCurrent();
					((LibraryListNode)Parent).RefreshRegistered();
				}
			}
		}

		private void LoadToggleClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.Warnings.ClearErrors(Dataphoria);
			if (!Registered)
			{
				try
				{
					DAE.IServerCursor LCursor = 
						Dataphoria.OpenCursor
						(
							String.Format
							(
								@"
									select 
										RequiredLibraries('{0}') 
											group by {{ Library_Name }}
											add {{ Max(Level) Level }}
											where not exists (System.LoadedLibraries where Name = Library_Name) 
											order by {{ Level desc }};
								", 
								FLibraryName
							)
						);
					try
					{
						using (DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow())
						{
							while (LCursor.Next())
							{
								LCursor.Select(LRow);
								try
								{
									Dataphoria.ExecuteScript(String.Format("RegisterLibrary('{0}', false);", (string)LRow["Library_Name"]));
								}
								catch (Exception LException)
								{
									Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
								}
							}
						}
					}
					finally
					{
						Dataphoria.CloseCursor(LCursor);
					}
	
					try
					{				
						Dataphoria.ExecuteScript(String.Format("RegisterLibrary('{0}', false);", FLibraryName));
					}
					catch (Exception LException)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
					}
				}
				finally
				{
					((LibraryListNode)Parent).RefreshRegistered();
				}
				((LibraryListNode)Parent).RefreshCurrent();
			}
			else
			{
				try
				{
					using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
					{
						Dataphoria.FrontendSession.CreateHost().Load(".Frontend.Form('Frontend', 'UnloadLibrary')", LForm);
						LForm.MainSource.Filter = GetFilter();
						LForm.HostNode.Open();
						if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
							throw new AbortException();
						DAE.Client.DataView LView = ((Frontend.Client.ISource)LForm.FindNode("Dependencies")).DataView;
						LView.First();
						foreach (DAE.Runtime.Data.Row LRow in LView)
						{
							try
							{
								Dataphoria.ExecuteScript(String.Format("UnregisterLibrary('{0}', false);", (string)LRow["Library_Name"]));
							}
							catch (Exception LException)
							{
								Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
							}
						}
					}

					try
					{
						Dataphoria.ExecuteScript(String.Format("UnregisterLibrary('{0}', false);", FLibraryName));
					}
					catch (Exception LException)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, LException, false);
					}
					FCanLoad = true;
				}
				finally
				{
					((LibraryListNode)Parent).RefreshCurrent();
					((LibraryListNode)Parent).RefreshRegistered();
				}
			}
		}

		private void SetAsCurrentClicked(object ASender, EventArgs AArgs)
		{
			if (Registered && !Current)
			{
				Dataphoria.ExecuteScript(String.Format("System.SetLibrary('{0}');", FLibraryName));
				((LibraryListNode)Parent).RefreshCurrent();
			}
		}		
		
		public override void Edit()
		{
			base.Edit();
			
			if (Registered)
				((LibraryListNode)Parent).RefreshRegistered();
		}

		private void OpenRegisterScriptClicked(object ASender, EventArgs AArgs)
		{
			DesignerInfo LInfo = Dataphoria.GetDefaultDesigner("d4");
			DocumentDesignBuffer LBuffer = new DocumentDesignBuffer(Dataphoria, LibraryName, "Register");
			Dataphoria.OpenDesigner(LInfo, LBuffer);
		}

		private void ScriptChangesClicked(object ASender, EventArgs AArgs)
		{
			string LCatalogDirectory = FolderUtility.GetDirectory(String.Empty);

			using (Frontend.Client.Windows.StatusForm LStatusForm = new Frontend.Client.Windows.StatusForm(Strings.ComparingSchema))
			{
				Dataphoria.EvaluateAndEdit(String.Format(".System.ScriptLibraryChanges('{0}', '{1}')", LCatalogDirectory.Replace("'", "''"), LibraryName), "d4");
			}
		}
		
		private void UpgradesClicked(object ASender, EventArgs AArgs)
		{
			IWindowsFormInterface LForm =
				Dataphoria.FrontendSession.LoadForm
				(
					null,
					@".Frontend.Form('Frontend', 'UpgradeBrowse')",
					null
				);
			try
			{
				LForm.HostNode.Open();
				Frontend.Client.ISource LSource = (Frontend.Client.ISource)LForm.FindNode("Libraries");
				using (DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(LSource.DataView.Process.ValueManager, LSource.DataView.TableType.RowType))
				{
					LRow["Name"] = LibraryName;
					LSource.DataView.FindKey(LRow);
				}
				LForm.ShowModal(Frontend.Client.FormMode.None);
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}
		
		private void UpgradeLibraryClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.ExecuteScript(String.Format(".System.UpgradeLibrary('{0}');", LibraryName));
			UpgradeRequired = false;
			MessageBox.Show(Strings.UpgradeLibrarySuccess);
		}

		private RootSchemaNode FRootSchemaNode;

		private DocumentListNode FDocumentListNode;
		public DocumentListNode DocumentListNode { get { return FDocumentListNode; } }

		protected override void InternalReconcileChildren()
		{
			// Schema
			if (Registered != (FRootSchemaNode != null))
				if (Registered)
				{
					FRootSchemaNode = new RootSchemaNode(FLibraryName);
					InsertBaseNode(0, FRootSchemaNode);
				}
				else
				{
					Nodes.Remove(FRootSchemaNode);
					FRootSchemaNode = null;
				}

			// Documents
			if ((FDocumentListNode == null) && (FLibraryName != "System"))
			{
				FDocumentListNode = new DocumentListNode(FLibraryName);
				AddBaseNode(FDocumentListNode);
			}
		}

		protected override void PrepareEditForm(Frontend.Client.Windows.IWindowsFormInterface AForm)
		{
			base.PrepareEditForm(AForm);
			if (Registered)
			{
				Frontend.Client.ITextBox LTextBox = ((Frontend.Client.ITextBox)AForm.FindNode("MainColumnMain.Name"));
				LTextBox.ReadOnly = true;
				LTextBox.Title = LTextBox.Title + Strings.LibraryNode_CannotEditWhileRegistered;
			}
		}

		protected override string KeyColumnName()
		{
			return "Main.Name";
		}

		protected override void NameChanged(string ANewName)
		{
			FLibraryName = ANewName;
			// Refresh children so that they get the new library name (the lib name is cached in these nodes)
			if (Built)
			{
				Nodes.Clear();
				FDocumentListNode = null;
				FRootSchemaNode = null;
				Refresh();
			}
			base.NameChanged(ANewName);
		}

		protected override string EditDocument()
		{
			return ".Frontend.Derive('.System.Libraries', 'Edit')";
		}
		
		protected override string DeleteDocument()
		{
			return ".Frontend.Derive('.System.Libraries', 'Delete', false)";
		}

		protected override string ViewDocument()
		{
			return ".Frontend.Derive('.System.LibraryView', 'View')";
		}
	}

	public class RootSchemaNode : DataNode
	{
		public RootSchemaNode(string ALibraryName)
		{
			Text = Strings.ObjectTree_SchemaNodeText;
			FLibraryName = ALibraryName;
			ImageIndex = 15;
			SelectedImageIndex = ImageIndex;
		}
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = new ContextMenu();
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitCreateScriptMenuText, new EventHandler(EmitCreateLibraryScriptClicked)));
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitDropScriptMenuText, new EventHandler(EmitDropLibraryScriptClicked)));
			return LMenu;
		}

		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		protected override void InternalReconcileChildren()
		{
			Nodes.Clear();
			AddBaseNode(new ScalarTypeListNode(FLibraryName));
			AddBaseNode(new TableListNode(FLibraryName));
			AddBaseNode(new ViewListNode(FLibraryName));
			AddBaseNode(new OperatorListNode(FLibraryName));
			AddBaseNode(new ConstraintListNode(FLibraryName));
			AddBaseNode(new ReferenceListNode(FLibraryName));
			AddBaseNode(new DeviceListNode(FLibraryName));
			AddBaseNode(new RoleListNode(FLibraryName));
		}

		private void EmitCreateLibraryScriptClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.EvaluateAndEdit(String.Format("ScriptLibrary('{0}')", LibraryName), "d4");
		}

		private void EmitDropLibraryScriptClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.EvaluateAndEdit(String.Format("ScriptDropLibrary('{0}')", LibraryName), "d4");
		}
	}
}


