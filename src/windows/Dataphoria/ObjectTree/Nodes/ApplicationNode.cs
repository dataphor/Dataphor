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
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new ApplicationNode(this, (string)row["Main.ID"], (string)row["Main.Description"]);
		}
		
		protected override string AddDocument()
		{
			return ".Frontend.Derive('Frontend.Applications', 'Add')";
		}

		public ApplicationNode FindByID(string iD)
		{
			ApplicationNode applicationNode;
			foreach (TreeNode node in Nodes)
			{
				applicationNode = node as ApplicationNode;
				if ((applicationNode != null) && (applicationNode.ID == iD))
					return applicationNode;
			}
			return null;
		}

		public override void DragDrop(DragEventArgs args)
		{
			Frontend.Client.Windows.IWindowsFormInterface form = Dataphoria.FrontendSession.LoadForm(null, AddDocument(), new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				DocumentData document = args.Data as DocumentData;
				Frontend.Client.ISource librariesSource = ((Frontend.Client.IFrame)form.FindNode("Frontend.ApplicationLibraries_ApplicationsFrame")).FrameInterfaceNode.MainSource;
				if (document != null)
				{
					form.MainSource["Main.ID"].AsString = document.Node.DocumentName;
					form.MainSource["Main.Description"].AsString = document.Node.DocumentName;
					form.MainSource["Main.StartDocument"].AsString = String.Format(".Frontend.Form('{0}', '{1}')", document.Node.LibraryName.Replace("'", "''"), document.Node.DocumentName.Replace("'", "''"));
					librariesSource.Insert();
					librariesSource["Main.Library_Name"].AsString = document.Node.LibraryName;
					librariesSource.Post();
				}
				else
				{
					TableData table = args.Data as TableData;
					if (table != null)
					{
						form.MainSource["Main.ID"].AsString = table.Node.ObjectName;
						form.MainSource["Main.Description"].AsString = table.Node.ObjectName;
						form.MainSource["Main.StartDocument"].AsString = String.Format(".Frontend.Derive('{0}')", table.Node.ObjectName.Replace("'", "''"));
						librariesSource.Insert();
						librariesSource["Main.Library_Name"].AsString = ((SchemaListNode)table.Node.Parent).LibraryName;
						librariesSource.Post();
					}
				}
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

		public override void DragOver(DragEventArgs args)
		{
			base.DragOver(args);
			DocumentData document = args.Data as DocumentData;
			if 
			(
				(
					(document != null) && 
					(
						(document.Node.DocumentType == "dfd") || 
						(document.Node.DocumentType == "dfdx")
					)
				) ||
				(args.Data is TableData)
			)
			{
				TreeView.SelectedNode = this;
				args.Effect = DragDropEffects.Link;
			}
		}
	}

	public class ApplicationNode : EditableItemNode
	{
		public ApplicationNode(ApplicationListNode listNode, string applicationID, string applicationDescription) : base()
		{
			_iD = applicationID;
			Text = applicationDescription;
			ImageIndex = 5;
			SelectedImageIndex = ImageIndex;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			menu.MenuItems.Add(0, new MenuItem("-"));

			MenuItem startMenuItem = new MenuItem(Strings.ObjectTree_StartMenuText, new EventHandler(StartClicked));
			startMenuItem.DefaultItem = true;
			menu.MenuItems.Add(0, startMenuItem);

			menu.MenuItems.Add(1, new MenuItem(Strings.ObjectTree_EmitCreateScriptMenuText, new EventHandler(EmitCreateScriptClicked)));

			return menu;
		}

		private string _iD;
		public string ID { get { return _iD; } set { _iD = value; } }

		public override bool IsEqual(DAE.Runtime.Data.IRow row)
		{
			return ((string)row["Main.ID"] == _iD);
		}

		public override string GetFilter()
		{
			return String.Format("Main.ID = '{0}'", _iD);
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

		protected override void NameChanged(string newName)
		{
			Text = newName;
			base.NameChanged(newName);
		}

		private void StartClicked(object sender, EventArgs args)
		{
			Frontend.Client.Windows.Session session = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				session.StartCallback(session.SetApplication(_iD), null);
			}
			catch
			{
				session.Dispose();
				throw;
			}
			Dataphoria.RefreshLibraries();
		}

		private void EmitCreateScriptClicked(object sender, EventArgs args)
		{
			Dataphoria.EvaluateAndEdit(String.Format(".Frontend.ScriptApplication('{0}')", _iD), "d4");
		}
	}
}
					
