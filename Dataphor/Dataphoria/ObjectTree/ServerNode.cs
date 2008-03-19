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

namespace Alphora.Dataphor.Dataphoria.ObjectTree
{
	public class ServerNode : DataNode
	{
		public ServerNode(bool AInProcess)
		{
			ImageIndex = AInProcess ? 0 : 1;
			SelectedImageIndex = ImageIndex;
			Build();
		}
		
		protected override void InternalReconcileChildren()
		{
			if (FApplicationNode == null)
			{
				FApplicationNode = new ApplicationListNode();
				AddBaseNode(FApplicationNode);
			}

			if (FLibraryNode == null)
			{
				FLibraryNode = new LibraryListNode();
				AddBaseNode(FLibraryNode);
			}

			if (FSecurityNode == null)
			{
				FSecurityNode = new SecurityListNode();
				AddBaseNode(FSecurityNode);
			}
		}

		private LibraryListNode FLibraryNode;
		public LibraryListNode LibraryNode { get { return FLibraryNode; } }

		private SecurityListNode FSecurityNode;
		public SecurityListNode SecurityNode { get { return FSecurityNode; } }

		private ApplicationListNode FApplicationNode;
		public ApplicationListNode ApplicationNode { get { return FApplicationNode; } }

		private MenuItem FDisconnectMenuItem;
		private MenuItem FNewDesignerMenuItem;
		private MenuItem FOpenFileMenuItem;
		private MenuItem FOpenFileWithMenuItem;
		//private MenuItem FSaveCatalogMenuItem;
		//private MenuItem FBackupCatalogMenuItem;
		private MenuItem FUpgradeLibrariesMenuItem;
		private MenuItem FViewLogMenuItem;

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = new ContextMenu();
			LMenu.MenuItems.Add(FDisconnectMenuItem = new MenuItem(Strings.Get("DisconnectMenuText"), new EventHandler(DisconnectClicked)));
			LMenu.MenuItems.Add(FViewLogMenuItem = new MenuItem(Strings.Get("ViewLogMenuText"), new EventHandler(ViewLogClicked)));
			LMenu.MenuItems.Add(new MenuItem("-"));
			LMenu.MenuItems.Add(FNewDesignerMenuItem = new MenuItem(Strings.Get("NewDesignerMenuText"), new EventHandler(NewDesignerClicked)));
			LMenu.MenuItems.Add(FOpenFileMenuItem = new MenuItem(Strings.Get("OpenFileMenuText"), new EventHandler(OpenFileClicked)));
			LMenu.MenuItems.Add(FOpenFileWithMenuItem = new MenuItem(Strings.Get("OpenFileWithMenuText"), new EventHandler(OpenFileWithClicked)));
			LMenu.MenuItems.Add(new MenuItem("-"));
			//LMenu.MenuItems.Add(FSaveCatalogMenuItem = new MenuItem(Strings.Get("SaveCatalogMenuText"), new EventHandler(SaveCatalogClicked)));
			//LMenu.MenuItems.Add(FBackupCatalogMenuItem = new MenuItem(Strings.Get("BackupCatalogMenuText"), new EventHandler(BackupCatalogClicked)));
			LMenu.MenuItems.Add(FUpgradeLibrariesMenuItem = new MenuItem(Strings.Get("UpgradeLibrariesMenuText"), new EventHandler(UpgradeLibrariesClicked)));
			return LMenu;
		}

		private void DisconnectClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.Disconnect();
		}
		
		private void ViewLogClicked(object ASender, EventArgs AArgs)
		{
			using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load("Derive('System.ListLogs', 'List')", LForm);
				LForm.HostNode.Open();
				LForm.ShowModal(Frontend.Client.FormMode.None);
				Dataphoria.EvaluateAndEdit("ShowLog(" + LForm.MainSource["Sequence"].AsString + ")", "txt");
			}
		}

		private void NewDesignerClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.NewDesigner();
		}

		private void OpenFileClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.OpenFile();
		}

		private void OpenFileWithClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.OpenFileWith();
		}
		
		private void SaveCatalogClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.SaveCatalog();
			MessageBox.Show(Strings.Get("SaveCatalogSuccess"));
		}

		private void BackupCatalogClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.BackupCatalog();
			MessageBox.Show(Strings.Get("BackupCatalogSuccess"));
		}
		
		private void UpgradeLibrariesClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.UpgradeLibraries();
			FLibraryNode.RefreshRegistered();
			MessageBox.Show(Strings.Get("UpgradeLibrariesSuccess"));
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