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
		public TableListNode(string libraryName) : base (libraryName)
		{
			Text = Strings.ObjectTree_TableListNodeText;
			ImageIndex = 13;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.BaseTableVars " + SchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new TableNode(this, (string)row["Name"]);
		}
	}

	public class TableNode : BaseTableNode
	{
		public TableNode(SchemaListNode parent, string name) : base(parent, name)
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
		public BaseTableNode(SchemaListNode parent, string name) : base()
		{
			ParentSchemaList = parent;
			ObjectName = name;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			menu.MenuItems.Add(0, new MenuItem("-"));
			MenuItem browseMenuItem = new MenuItem(Strings.ObjectTree_BrowseMenuText, new EventHandler(BrowseClicked));
			browseMenuItem.DefaultItem = true;
			menu.MenuItems.Add(0, browseMenuItem);
			menu.MenuItems.Add(1, new MenuItem(Strings.ObjectTree_DeriveMenuText, new EventHandler(DeriveClicked)));
			return menu;
		}

		protected void BrowseClicked(object sender, EventArgs args)
		{
			Frontend.Client.Windows.Session session = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				session.SetFormDesigner();
				session.StartCallback(String.Format(".Frontend.Derive('{0}', 'Browse')", ObjectName), null);
			}
			catch
			{
				session.Dispose();
				throw;
			}
		}

		protected void DeriveClicked(object sender, EventArgs args)
		{
			Frontend.Client.Windows.Session session = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				session.SetFormDesigner();
				Frontend.Client.Windows.IWindowsFormInterface form = session.LoadForm(null, ".Frontend.Form('.Frontend', 'DerivedFormLauncher')");
				try
				{
					((Frontend.Client.ISource)form.FindNode("Main")).DataView.Fields["Query"].AsString = ObjectName;
					form.Show();
				}
				catch
				{
					form.HostNode.Dispose();
					throw;
				}
			}
			catch
			{
				session.Dispose();
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
		public TableData(BaseTableNode node) : base()
		{
			_node = node;
		}

		private BaseTableNode _node;
		public BaseTableNode Node
		{
			get { return _node; }
		}
	}
}
