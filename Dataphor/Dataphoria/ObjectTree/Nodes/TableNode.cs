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
	public class TableListNode : SchemaListNode
	{
		public TableListNode(string ALibraryName) : base (ALibraryName)
		{
			Text = Strings.ObjectTree_TableListNodeText;
			ImageIndex = 13;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.BaseTableVars " + CSchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new TableNode(this, (string)ARow["Name"]);
		}
	}

	public class TableNode : BaseTableNode
	{
		public TableNode(SchemaListNode AParent, string AName) : base(AParent, AName)
		{
			ImageIndex = 14;
			SelectedImageIndex = 14;
		}

		protected override string GetViewExpression()
		{
			return ".System.BaseTableVars";
		}
	}

	public abstract class BaseTableNode : SchemaItemNode
	{
		public BaseTableNode(SchemaListNode AParent, string AName) : base()
		{
			ParentSchemaList = AParent;
			ObjectName = AName;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LMenu.MenuItems.Add(0, new MenuItem("-"));
			MenuItem LBrowseMenuItem = new MenuItem(Strings.ObjectTree_BrowseMenuText, new EventHandler(BrowseClicked));
			LBrowseMenuItem.DefaultItem = true;
			LMenu.MenuItems.Add(0, LBrowseMenuItem);
			LMenu.MenuItems.Add(1, new MenuItem(Strings.ObjectTree_DeriveMenuText, new EventHandler(DeriveClicked)));
			return LMenu;
		}

		protected void BrowseClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.Session LSession = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				LSession.SetFormDesigner();
				LSession.StartCallback(String.Format(".Frontend.Derive('{0}', 'Browse')", ObjectName), null);
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}

		protected void DeriveClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.Session LSession = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				LSession.SetFormDesigner();
				Frontend.Client.Windows.IWindowsFormInterface LForm = LSession.LoadForm(null, ".Frontend.Form('.Frontend', 'DerivedFormLauncher')");
				try
				{
					((Frontend.Client.ISource)LForm.FindNode("Main")).DataView.Fields["Query"].AsString = ObjectName;
					LForm.Show();
				}
				catch
				{
					LForm.HostNode.Dispose();
					throw;
				}
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}

		public override void ItemDrag()
		{
			TreeView.DoDragDrop(new TableData(this), DragDropEffects.Link);
		}
	}

	public class TableData : DataObject
	{
		public TableData(BaseTableNode ANode) : base()
		{
			FNode = ANode;
		}

		private BaseTableNode FNode;
		public BaseTableNode Node
		{
			get { return FNode; }
		}
	}
}
