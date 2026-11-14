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
	public class SecurityListNode : DataNode
	{
		public SecurityListNode()
		{
			Text = Strings.ObjectTree_SecurityNodeText;
			ImageIndex = 11;
			SelectedImageIndex = ImageIndex;
		}

		protected override void InternalReconcileChildren()
		{
			Nodes.Clear();
			AddBaseNode(new UserListNode());
			//AddBaseNode(new GroupListNode());
		}

		#if !ALLOWMANAGEUSERS
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = new ContextMenu();
			menu.MenuItems.Add(0, new MenuItem(Strings.ObjectTree_ManageUsersText, new EventHandler(ManageUsersClicked)));
			return menu;
		}

		protected void ManageUsersClicked(object sender, EventArgs args)
		{
			Dataphoria.EnsureSecurityRegistered();
			Frontend.Client.Windows.IWindowsFormInterface form = Dataphoria.FrontendSession.LoadForm(null, "Form('Security', 'UserBrowse')");
			form.Show();
		}
		#endif
	}
	
	public class UserListNode : ListNode
	{
		public UserListNode()
		{
			Text = "Users";
			ImageIndex = 4;
			SelectedImageIndex = ImageIndex;
		}

		#if ALLOWMANAGEUSERS
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LMenu.MenuItems.Add(0, new MenuItem("-"));
			LMenu.MenuItems.Add(0, new MenuItem(Strings.Get("ObjectTree.ManageUsersText"), new EventHandler(ManageUsersClicked)));
			return LMenu;
		}
		#endif

		protected override string GetChildExpression()
		{
			return ".System.Users over { ID }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new UserNode((string)row["ID"]);
		}

		#if ALLOWMANAGEUSERS		
		protected void ManageUsersClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = Dataphoria.FrontendSession.LoadForm(null, "Form('Security', 'UserBrowse')");
			LForm.Show();
		}
		#endif
	}

	public class UserNode : ItemNode
	{
		public UserNode(string userName) : base()
		{
			Text = userName;
			ImageIndex = 4;
			SelectedImageIndex = ImageIndex;
		}

		public override string GetFilter()
		{
			return String.Format("ID = '{0}'", Text);
		}

		public override bool IsEqual(DAE.Runtime.Data.IRow row)
		{
			return (string)row["ID"] == Text;
		}

		protected override string ViewDocument()
		{
			return "Derive('System.Users', 'View', false)";
		}
	}
}													
