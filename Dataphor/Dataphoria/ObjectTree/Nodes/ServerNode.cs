/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public class ServerNode : DataNode
	{
		public ServerNode(bool inProcess)
		{
			ImageIndex = inProcess ? 0 : 1;
			SelectedImageIndex = ImageIndex;
			Build();
		}
		
		protected override void InternalReconcileChildren()
		{
			if (_applicationNode == null)
			{
				_applicationNode = new ApplicationListNode();
				AddBaseNode(_applicationNode);
			}

			if (_libraryNode == null)
			{
				_libraryNode = new LibraryListNode();
				AddBaseNode(_libraryNode);
			}

			if (_securityNode == null)
			{
				_securityNode = new SecurityListNode();
				AddBaseNode(_securityNode);
			}
		}

		private LibraryListNode _libraryNode;
		public LibraryListNode LibraryNode { get { return _libraryNode; } }

		private SecurityListNode _securityNode;
		public SecurityListNode SecurityNode { get { return _securityNode; } }

		private ApplicationListNode _applicationNode;
		public ApplicationListNode ApplicationNode { get { return _applicationNode; } }

		private MenuItem _disconnectMenuItem;
		private MenuItem _newDesignerMenuItem;
		private MenuItem _openFileMenuItem;
		private MenuItem _openFileWithMenuItem;
		//private MenuItem FSaveCatalogMenuItem;
		//private MenuItem FBackupCatalogMenuItem;
		private MenuItem _upgradeLibrariesMenuItem;
		private MenuItem _viewLogMenuItem;

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = new ContextMenu();
			menu.MenuItems.Add(_disconnectMenuItem = new MenuItem(Strings.DisconnectMenuText, new EventHandler(DisconnectClicked)));
			menu.MenuItems.Add(_viewLogMenuItem = new MenuItem(Strings.ViewLogMenuText, new EventHandler(ViewLogClicked)));
			menu.MenuItems.Add(new MenuItem("-"));
			menu.MenuItems.Add(_newDesignerMenuItem = new MenuItem(Strings.NewDesignerMenuText, new EventHandler(NewDesignerClicked)));
			menu.MenuItems.Add(_openFileMenuItem = new MenuItem(Strings.OpenFileMenuText, new EventHandler(OpenFileClicked)));
			menu.MenuItems.Add(_openFileWithMenuItem = new MenuItem(Strings.OpenFileWithMenuText, new EventHandler(OpenFileWithClicked)));
			menu.MenuItems.Add(new MenuItem("-"));
			//LMenu.MenuItems.Add(FSaveCatalogMenuItem = new MenuItem(Strings.Get("SaveCatalogMenuText"), new EventHandler(SaveCatalogClicked)));
			//LMenu.MenuItems.Add(FBackupCatalogMenuItem = new MenuItem(Strings.Get("BackupCatalogMenuText"), new EventHandler(BackupCatalogClicked)));
			menu.MenuItems.Add(_upgradeLibrariesMenuItem = new MenuItem(Strings.UpgradeLibrariesMenuText, new EventHandler(UpgradeLibrariesClicked)));
			return menu;
		}

		private void DisconnectClicked(object sender, EventArgs args)
		{
			Dataphoria.Disconnect();
		}
		
		private void ViewLogClicked(object sender, EventArgs args)
		{
			using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load("Derive('System.ListLogs', 'List')", form);
				form.HostNode.Open();
				form.ShowModal(Frontend.Client.FormMode.None);
				Dataphoria.EvaluateAndEdit("ShowLog(" + form.MainSource["Sequence"].AsString + ")", "txt");
			}
		}

		private void NewDesignerClicked(object sender, EventArgs args)
		{
			Dataphoria.NewDesigner();
		}

		private void OpenFileClicked(object sender, EventArgs args)
		{
			Dataphoria.OpenFile();
		}

		private void OpenFileWithClicked(object sender, EventArgs args)
		{
			Dataphoria.OpenFileWith();
		}
		
		private void SaveCatalogClicked(object sender, EventArgs args)
		{
			Dataphoria.SaveCatalog();
			MessageBox.Show(Strings.SaveCatalogSuccess);
		}

		private void BackupCatalogClicked(object sender, EventArgs args)
		{
			Dataphoria.BackupCatalog();
			MessageBox.Show(Strings.BackupCatalogSuccess);
		}
		
		private void UpgradeLibrariesClicked(object sender, EventArgs args)
		{
			Dataphoria.UpgradeLibraries();
			_libraryNode.RefreshRegistered();
			MessageBox.Show(Strings.UpgradeLibrariesSuccess);
		}

		// TODO: Schema editor disabled until complete
//		private void EditSchemaClicked(object ASender, EventArgs AArgs)
//		{
//			Connect();
//
//			string LScript;
//
//			IServerProcess LProcess = Server.DataSession.ServerSession.StartProcess();
//			try
//			{
//				IServerExpressionPlan LPlan = LProcess.PrepareExpression("table { row { ScriptCatalog() Script } }", null);
//				try
//				{
//					IServerCursor LCursor = LPlan.Open(null);
//					try
//					{
//						LCursor.Next();
//						using (DAE.Runtime.Data.Row LRow = LCursor.Select())
//							LScript = LRow[0].EditConveyor.GetAsString(LRow[0]);
//					}
//					finally
//					{
//						LPlan.Close(LCursor);
//					}
//				}
//				finally
//				{
//					LProcess.UnprepareExpression(LPlan);
//				}
//			}
//			finally
//			{
//				Server.DataSession.ServerSession.StopProcess(LProcess);
//			}
//			
//			SchemaDesigner.SchemaForm LForm = new SchemaDesigner.SchemaForm(LScript);
//			try
//			{
//				Dataphoria.AttachForm(LForm, LForm.Text);
//			}
//			catch
//			{
//				LForm.Dispose();
//				throw;
//			}
//		}
	}
}
