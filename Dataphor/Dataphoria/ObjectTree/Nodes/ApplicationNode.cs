/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public class ApplicationListNode : BrowseNode
	{
		public ApplicationListNode() : base()
		{
			Text = "Applications";
			ImageIndex = 2;
			SelectedImageIndex = ImageIndex;
		}
		
		protected override string GetChildExpression()
		{
			return ".Frontend.Applications rename Main";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new ApplicationNode(this, ARow["Main.ID"].AsString, ARow["Main.Description"].AsString);
		}
		
		protected override string AddDocument()
		{
			return ".Frontend.Derive('Frontend.Applications', 'Add')";
		}

		public ApplicationNode FindByID(string AID)
		{
			ApplicationNode LApplicationNode;
			foreach (TreeNode LNode in Nodes)
			{
				LApplicationNode = LNode as ApplicationNode;
				if ((LApplicationNode != null) && (LApplicationNode.ID == AID))
					return LApplicationNode;
			}
			return null;
		}

		public override void DragDrop(DragEventArgs AArgs)
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = Dataphoria.FrontendSession.LoadForm(null, AddDocument(), new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				DocumentData LDocument = AArgs.Data as DocumentData;
				Frontend.Client.ISource LLibrariesSource = ((Frontend.Client.IFrame)LForm.FindNode("Frontend.ApplicationLibraries_ApplicationsFrame")).FrameInterfaceNode.MainSource;
				if (LDocument != null)
				{
					LForm.MainSource["Main.ID"].AsString = LDocument.Node.DocumentName;
					LForm.MainSource["Main.Description"].AsString = LDocument.Node.DocumentName;
					LForm.MainSource["Main.StartDocument"].AsString = String.Format(".Frontend.Form('{0}', '{1}')", LDocument.Node.LibraryName.Replace("'", "''"), LDocument.Node.DocumentName.Replace("'", "''"));
					LLibrariesSource.Insert();
					LLibrariesSource["Main.Library_Name"].AsString = LDocument.Node.LibraryName;
					LLibrariesSource.Post();
				}
				else
				{
					TableData LTable = AArgs.Data as TableData;
					if (LTable != null)
					{
						LForm.MainSource["Main.ID"].AsString = LTable.Node.ObjectName;
						LForm.MainSource["Main.Description"].AsString = LTable.Node.ObjectName;
						LForm.MainSource["Main.StartDocument"].AsString = String.Format(".Frontend.Derive('{0}')", LTable.Node.ObjectName.Replace("'", "''"));
						LLibrariesSource.Insert();
						LLibrariesSource["Main.Library_Name"].AsString = ((SchemaListNode)LTable.Node.Parent).LibraryName;
						LLibrariesSource.Post();
					}
				}
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

		public override void DragOver(DragEventArgs AArgs)
		{
			base.DragOver(AArgs);
			DocumentData LDocument = AArgs.Data as DocumentData;
			if 
			(
				(
					(LDocument != null) && 
					(
						(LDocument.Node.DocumentType == "dfd") || 
						(LDocument.Node.DocumentType == "dfdx")
					)
				) ||
				(AArgs.Data is TableData)
			)
			{
				TreeView.SelectedNode = this;
				AArgs.Effect = DragDropEffects.Link;
			}
		}
	}

	public class ApplicationNode : EditableItemNode
	{
		public ApplicationNode(ApplicationListNode AListNode, string AApplicationID, string AApplicationDescription) : base()
		{
			FID = AApplicationID;
			Text = AApplicationDescription;
			ImageIndex = 5;
			SelectedImageIndex = ImageIndex;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LMenu.MenuItems.Add(0, new MenuItem("-"));

			MenuItem LStartMenuItem = new MenuItem(Strings.ObjectTree_StartMenuText, new EventHandler(StartClicked));
			LStartMenuItem.DefaultItem = true;
			LMenu.MenuItems.Add(0, LStartMenuItem);

			LMenu.MenuItems.Add(1, new MenuItem(Strings.ObjectTree_EmitCreateScriptMenuText, new EventHandler(EmitCreateScriptClicked)));

			return LMenu;
		}

		private string FID;
		public string ID { get { return FID; } set { FID = value; } }

		public override bool IsEqual(DAE.Runtime.Data.Row ARow)
		{
			return (ARow["Main.ID"].AsString == FID);
		}

		public override string GetFilter()
		{
			return String.Format("Main.ID = '{0}'", FID);
		}

		protected override string EditDocument()
		{
			return ".Frontend.Derive('Frontend.Applications', 'Edit')";
		}
		
		protected override string DeleteDocument()
		{
			return ".Frontend.Derive('Frontend.Applications', 'Delete', false)";
		}

		protected override string ViewDocument()
		{
			return ".Frontend.Derive('Frontend.Applications', 'View', false)";
		}

		protected override void NameChanged(string ANewName)
		{
			Text = ANewName;
			base.NameChanged(ANewName);
		}

		private void StartClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.Session LSession = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				LSession.StartCallback(LSession.SetApplication(FID), null);
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
			Dataphoria.RefreshLibraries();
		}

		private void EmitCreateScriptClicked(object ASender, EventArgs AArgs)
		{
			 Dataphoria.EvaluateAndEdit(String.Format(".Frontend.ScriptApplication('{0}')", FID), "d4");
		}
	}
}
					
