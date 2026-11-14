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
		
		private MenuItem _attachMenuItem;
		private MenuItem _attachLibrariesMenuItem;

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			_attachMenuItem = new MenuItem(Strings.ObjectTree_AttachMenuText, new EventHandler(AttachClicked));
			menu.MenuItems.Add(1, _attachMenuItem);
			_attachLibrariesMenuItem = new MenuItem(Strings.ObjectTree_AttachLibrariesMenuText, new EventHandler(AttachLibrariesClicked));
			menu.MenuItems.Add(2, _attachLibrariesMenuItem);
			return menu;
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
		
		protected void AttachClicked(object sender, EventArgs args)
		{
			Frontend.Client.Windows.IWindowsFormInterface form = Dataphoria.FrontendSession.LoadForm(null, "Derive('System.AttachLibrary', 'Add', '', '', false)", new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				if (form.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
					throw new AbortException();
				BaseNode node = CreateChildNode(form.MainSource.DataView.ActiveRow);
				AddNode(node);
				TreeView.SelectedNode = node;
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		protected void AttachLibrariesClicked(object sender, EventArgs args)
		{
			Frontend.Client.Windows.IWindowsFormInterface form = Dataphoria.FrontendSession.LoadForm(null, "Derive('System.AttachLibraries', 'Add', '', '', false)", new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				if (form.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				form.HostNode.Dispose();
			}

			Refresh();
		}

		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			LibraryNode node = new LibraryNode(this, (string)row["Main.Name"]);
			if (row.DataType.Columns.ContainsName("Main.IsLoaded"))
				UpdateNode(node, row);
			return node;
		}

		private void UpdateNode(LibraryNode node, DAE.Runtime.Data.IRow row)
		{
			node.Registered = (bool)row["Main.IsLoaded"];
			node.CanLoad = true; //(bool)ARow["Main.CanLoad"];
			node.IsSuspect = (bool)row["Main.IsSuspect"];
			node.UpgradeRequired = (bool)row["Main.UpgradeRequired"];
		}

		protected override string AddDocument()
		{
			return ".Frontend.Derive('System.Libraries', 'Add')";
		}
		
		public void RefreshRegistered()
		{
            Dataphoria.Execute(GetChildExpression(), GetParams(), ARow =>
                                                                      {
                                                                          var node = FindByKey(ARow) as LibraryNode;
                                                                          if (node != null)
                                                                              UpdateNode(node, ARow); 
                                                                      });           
		}
		
		public void RefreshCurrent()
		{
			string currentLibrary = Dataphoria.GetCurrentLibraryName();
			foreach (LibraryNode node in Nodes)
			{
				node.Current = node.LibraryName == currentLibrary;
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
		public LibraryNode(LibraryListNode node, string libraryName) : base()
		{
			_libraryName = libraryName;
			UpdateText();
			UpdateImageIndex();
		}

		public override BuildMode BuildMode
		{
			get { return BuildMode.OnExpand; }
		}

		public override bool IsEqual(DAE.Runtime.Data.IRow row)
		{
			return ((string)row["Main.Name"] == _libraryName);
		}

		public override string GetFilter()
		{
			return String.Format("Main.Name = '{0}'", _libraryName);
		}

		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		private bool _canLoad = true;
		public bool CanLoad
		{
			get { return _canLoad; }
			set { _canLoad = value; }
		}
		
		private bool _upgradeRequired;
		public bool UpgradeRequired
		{
			get { return _upgradeRequired; }
			set 
			{ 
				if (_upgradeRequired != value)
				{
					_upgradeRequired = value; 
					UpdateImageIndex();
				}
			}
		}

		private bool _isSuspect;
		public bool IsSuspect
		{
			get { return _isSuspect; }
			set
			{
				if (_isSuspect = value)
				{
					_isSuspect = value;
					UpdateImageIndex();
				}
			}
		}
		
		private bool _registered;
		public bool Registered
		{
			get { return _registered; }
			set
			{
				if (_registered != value)
				{
					_registered = value;
					UpdateImageIndex();
					UpdateText();
					Refresh();
				}
			}
		}

		private bool _current;
		public bool Current
		{
			get { return _current; }
			set
			{
				if (_current != value)
				{
					_current = value;
					UpdateImageIndex();
					UpdateText();
				}
			}
		}
		
		private void UpdateImageIndex()
		{
			if (_registered)
				if (_current)
					if (_upgradeRequired)
						ImageIndex = 28;
					else
						ImageIndex = 25;
				else
					if (_upgradeRequired)
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
			if (_current)
			{
				NodeFont = new Font(TreeView.Font, FontStyle.Bold);
				// HACK: add trailing spaces to work around bold cut off bug (tried setting the TreeView's font to no avail)
				int blankCount;
				using (Graphics graphics = TreeView.CreateGraphics())
				{
					blankCount = 
						(int)Math.Ceiling
						(
							(
								graphics.MeasureString(_libraryName, NodeFont).Width - 
								graphics.MeasureString(_libraryName, TreeView.Font).Width
							) / 
							graphics.MeasureString(" ", NodeFont).Width
						) + 3;
				}
				System.Text.StringBuilder builder = new System.Text.StringBuilder(_libraryName, _libraryName.Length + blankCount);
				while (blankCount > 0)
				{
					builder.Append(' ');
					blankCount--;
				}
				Text = builder.ToString();
			}
			else
			{
				NodeFont = null;
				Text = _libraryName;
			}
		}

		private MenuItem _detachMenuItem;		
		private MenuItem _loadMenuItem;
		private MenuItem _registerMenuItem;
		private MenuItem _setAsCurrentMenuItem;
		private MenuItem _upgradeLibraryMenuItem;
		//private MenuItem FScriptChangesMenuItem;
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			
			menu.MenuItems.Add(new MenuItem("-"));	

			_registerMenuItem = new MenuItem(Strings.ObjectTree_RegisterMenuText, new EventHandler(RegisterToggleClicked));
			menu.MenuItems.Add(_registerMenuItem);
			
			_loadMenuItem = new MenuItem(Strings.ObjectTree_LoadMenuText, new EventHandler(LoadToggleClicked));
			menu.MenuItems.Add(_loadMenuItem);
			
			_detachMenuItem = new MenuItem(Strings.ObjectTree_DetachMenuText, new EventHandler(DetachClicked));
			menu.MenuItems.Add(_detachMenuItem);

			_setAsCurrentMenuItem = new MenuItem(Strings.ObjectTree_SetAsCurrentMenuText, new EventHandler(SetAsCurrentClicked));
			_setAsCurrentMenuItem.DefaultItem = true;
			menu.MenuItems.Add(_setAsCurrentMenuItem);

			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_OpenRegisterScriptMenuText, new EventHandler(OpenRegisterScriptClicked)));
			menu.MenuItems.Add(new MenuItem("-"));	
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_UpgradesMenuText, new EventHandler(UpgradesClicked)));
			
			_upgradeLibraryMenuItem = new MenuItem(Strings.ObjectTree_UpgradeLibraryMenuText, new EventHandler(UpgradeLibraryClicked));
			menu.MenuItems.Add(_upgradeLibraryMenuItem);

			//FScriptChangesMenuItem = new MenuItem(Strings.Get("ObjectTree.ScriptChangesText"), new EventHandler(ScriptChangesClicked));
			//LMenu.MenuItems.Add(FScriptChangesMenuItem);

			return menu;
		}

		protected override void UpdateContextMenu(ContextMenu menu)
		{
			_setAsCurrentMenuItem.Enabled = Registered && !Current;
			_registerMenuItem.Text = Registered ? Strings.ObjectTree_UnregisterMenuText : Strings.ObjectTree_RegisterMenuText;
			_loadMenuItem.Text = Registered ? Strings.ObjectTree_UnloadMenuText : Strings.ObjectTree_LoadMenuText;
			_detachMenuItem.Enabled = !Registered;
			_registerMenuItem.Enabled = !((LibraryName == "System") || (LibraryName == "General") || (LibraryName == "Frontend") || (LibraryName == "SimpleDevice"));
			_loadMenuItem.Enabled = _registerMenuItem.Enabled && (Registered || CanLoad);
			_upgradeLibraryMenuItem.Enabled = Registered && UpgradeRequired;
			//FScriptChangesMenuItem.Enabled = Registered;
		}
		
		private void DetachClicked(object sender, EventArgs args)
		{
			using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(".Frontend.Derive('.System.DetachLibrary', 'Delete', false)", form);
				form.MainSource.Filter = GetFilter();
				form.HostNode.Open();
				if (form.ShowModal(Frontend.Client.FormMode.Delete) != DialogResult.OK)
					throw new AbortException();
			}
			Remove();
		}

		private void RegisterToggleClicked(object sender, EventArgs args)
		{
			Dataphoria.Warnings.ClearErrors(Dataphoria);
			if (!Registered)
			{
				try
				{
					DAE.IServerCursor cursor = 
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
								_libraryName
							)
						);
					try
					{
						using (DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow())
						{
							while (cursor.Next())
							{
								cursor.Select(row);
								try
								{
									Dataphoria.ExecuteScript(String.Format("RegisterLibrary('{0}');", (string)row["Library_Name"]));
								}
								catch (Exception exception)
								{
									Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
								}
							}
						}
					}
					finally
					{
						Dataphoria.CloseCursor(cursor);
					}
					
					try
					{
						Dataphoria.ExecuteScript(String.Format("RegisterLibrary(\"{0}\");", _libraryName));
					}
					catch (Exception exception)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
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
					using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
					{
						Dataphoria.FrontendSession.CreateHost().Load(".Frontend.Form('Frontend', 'UnregisterLibrary')", form);
						form.MainSource.Filter = GetFilter();
						form.HostNode.Open();
						if (form.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
							throw new AbortException();
						DAE.Client.DataView view = ((Frontend.Client.ISource)form.FindNode("Dependencies")).DataView;
						view.First();
						foreach (DAE.Runtime.Data.Row row in view)
						{
							try
							{
								Dataphoria.ExecuteScript(String.Format("UnregisterLibrary(\"{0}\");", (string)row["Library_Name"]));
							}
							catch (Exception exception)
							{
								Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
							}
						}
					}

					try
					{
						Dataphoria.ExecuteScript(String.Format("UnregisterLibrary(\"{0}\");", _libraryName));
					}
					catch (Exception exception)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
					}
				}
				finally
				{
					((LibraryListNode)Parent).RefreshCurrent();
					((LibraryListNode)Parent).RefreshRegistered();
				}
			}
		}

		private void LoadToggleClicked(object sender, EventArgs args)
		{
			Dataphoria.Warnings.ClearErrors(Dataphoria);
			if (!Registered)
			{
				try
				{
					DAE.IServerCursor cursor = 
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
								_libraryName
							)
						);
					try
					{
						using (DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow())
						{
							while (cursor.Next())
							{
								cursor.Select(row);
								try
								{
									Dataphoria.ExecuteScript(String.Format("RegisterLibrary('{0}', false);", (string)row["Library_Name"]));
								}
								catch (Exception exception)
								{
									Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
								}
							}
						}
					}
					finally
					{
						Dataphoria.CloseCursor(cursor);
					}
	
					try
					{				
						Dataphoria.ExecuteScript(String.Format("RegisterLibrary('{0}', false);", _libraryName));
					}
					catch (Exception exception)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
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
					using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
					{
						Dataphoria.FrontendSession.CreateHost().Load(".Frontend.Form('Frontend', 'UnloadLibrary')", form);
						form.MainSource.Filter = GetFilter();
						form.HostNode.Open();
						if (form.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
							throw new AbortException();
						DAE.Client.DataView view = ((Frontend.Client.ISource)form.FindNode("Dependencies")).DataView;
						view.First();
						foreach (DAE.Runtime.Data.Row row in view)
						{
							try
							{
								Dataphoria.ExecuteScript(String.Format("UnregisterLibrary('{0}', false);", (string)row["Library_Name"]));
							}
							catch (Exception exception)
							{
								Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
							}
						}
					}

					try
					{
						Dataphoria.ExecuteScript(String.Format("UnregisterLibrary('{0}', false);", _libraryName));
					}
					catch (Exception exception)
					{
						Dataphoria.Warnings.AppendError(Dataphoria, exception, false);
					}
					_canLoad = true;
				}
				finally
				{
					((LibraryListNode)Parent).RefreshCurrent();
					((LibraryListNode)Parent).RefreshRegistered();
				}
			}
		}

		private void SetAsCurrentClicked(object sender, EventArgs args)
		{
			if (Registered && !Current)
			{
				Dataphoria.ExecuteScript(String.Format("System.SetLibrary('{0}');", _libraryName));
				((LibraryListNode)Parent).RefreshCurrent();
			}
		}		
		
		public override void Edit()
		{
			base.Edit();
			
			if (Registered)
				((LibraryListNode)Parent).RefreshRegistered();
		}

		private void OpenRegisterScriptClicked(object sender, EventArgs args)
		{
			DesignerInfo info = Dataphoria.GetDefaultDesigner("d4");
			DocumentDesignBuffer buffer = new DocumentDesignBuffer(Dataphoria, LibraryName, "Register");
			Dataphoria.OpenDesigner(info, buffer);
		}

		private void ScriptChangesClicked(object sender, EventArgs args)
		{
			string catalogDirectory = FolderUtility.GetDirectory(String.Empty);

			using (Frontend.Client.Windows.StatusForm statusForm = new Frontend.Client.Windows.StatusForm(Strings.ComparingSchema))
			{
				Dataphoria.EvaluateAndEdit(String.Format(".System.ScriptLibraryChanges('{0}', '{1}')", catalogDirectory.Replace("'", "''"), LibraryName), "d4");
			}
		}
		
		private void UpgradesClicked(object sender, EventArgs args)
		{
			IWindowsFormInterface form =
				Dataphoria.FrontendSession.LoadForm
				(
					null,
					@".Frontend.Form('Frontend', 'UpgradeBrowse')",
					null
				);
			try
			{
				form.HostNode.Open();
				Frontend.Client.ISource source = (Frontend.Client.ISource)form.FindNode("Libraries");
				using (DAE.Runtime.Data.Row row = new DAE.Runtime.Data.Row(source.DataView.Process.ValueManager, source.DataView.TableType.RowType))
				{
					row["Name"] = LibraryName;
					source.DataView.FindKey(row);
				}
				form.ShowModal(Frontend.Client.FormMode.None);
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}
		
		private void UpgradeLibraryClicked(object sender, EventArgs args)
		{
			Dataphoria.ExecuteScript(String.Format(".System.UpgradeLibrary('{0}');", LibraryName));
			UpgradeRequired = false;
			MessageBox.Show(Strings.UpgradeLibrarySuccess);
		}

		private RootSchemaNode _rootSchemaNode;

		private DocumentListNode _documentListNode;
		public DocumentListNode DocumentListNode { get { return _documentListNode; } }

		protected override void InternalReconcileChildren()
		{
			// Schema
			if (Registered != (_rootSchemaNode != null))
				if (Registered)
				{
					_rootSchemaNode = new RootSchemaNode(_libraryName);
					InsertBaseNode(0, _rootSchemaNode);
				}
				else
				{
					Nodes.Remove(_rootSchemaNode);
					_rootSchemaNode = null;
				}

			// Documents
			if ((_documentListNode == null) && (_libraryName != "System"))
			{
				_documentListNode = new DocumentListNode(_libraryName);
				AddBaseNode(_documentListNode);
			}
		}

		protected override void PrepareEditForm(Frontend.Client.Windows.IWindowsFormInterface form)
		{
			base.PrepareEditForm(form);
			if (Registered)
			{
				Frontend.Client.ITextBox textBox = ((Frontend.Client.ITextBox)form.FindNode("MainColumnMain.Name"));
				textBox.ReadOnly = true;
				textBox.Title = textBox.Title + Strings.LibraryNode_CannotEditWhileRegistered;
			}
		}

		protected override string KeyColumnName()
		{
			return "Main.Name";
		}

		protected override void NameChanged(string newName)
		{
			_libraryName = newName;
			// Refresh children so that they get the new library name (the lib name is cached in these nodes)
			if (Built)
			{
				Nodes.Clear();
				_documentListNode = null;
				_rootSchemaNode = null;
				Refresh();
			}
			base.NameChanged(newName);
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
		public RootSchemaNode(string libraryName)
		{
			Text = Strings.ObjectTree_SchemaNodeText;
			_libraryName = libraryName;
			ImageIndex = 15;
			SelectedImageIndex = ImageIndex;
		}
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = new ContextMenu();
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitCreateScriptMenuText, new EventHandler(EmitCreateLibraryScriptClicked)));
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitDropScriptMenuText, new EventHandler(EmitDropLibraryScriptClicked)));
			return menu;
		}

		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		protected override void InternalReconcileChildren()
		{
			Nodes.Clear();
			AddBaseNode(new ScalarTypeListNode(_libraryName));
			AddBaseNode(new TableListNode(_libraryName));
			AddBaseNode(new ViewListNode(_libraryName));
			AddBaseNode(new OperatorListNode(_libraryName));
			AddBaseNode(new ConstraintListNode(_libraryName));
			AddBaseNode(new ReferenceListNode(_libraryName));
			AddBaseNode(new DeviceListNode(_libraryName));
			AddBaseNode(new RoleListNode(_libraryName));
		}

		private void EmitCreateLibraryScriptClicked(object sender, EventArgs args)
		{
			Dataphoria.EvaluateAndEdit(String.Format("ScriptLibrary('{0}')", LibraryName), "d4");
		}

		private void EmitDropLibraryScriptClicked(object sender, EventArgs args)
		{
			Dataphoria.EvaluateAndEdit(String.Format("ScriptDropLibrary('{0}')", LibraryName), "d4");
		}
	}
}


