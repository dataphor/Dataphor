/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public class OperatorListNode : SchemaListNode
	{
		public OperatorListNode(string ALibraryName) : base (ALibraryName)
		{
			Text = "Operators";
			ImageIndex = 12;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.Operators " + CSchemaListFilter + " { Name, OperatorName + Signature DisplayName, Locator, Line, LinePos }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new OperatorNode(this, (string)ARow["Name"], (string)ARow["DisplayName"], new DebugLocator((string)ARow["Locator"], (int)ARow["Line"], (int)ARow["LinePos"]));
		}
	}

	public class OperatorNode : SchemaItemNode
	{
		public OperatorNode(OperatorListNode ANode, string ACatalogName, string AOperatorName, DebugLocator ALocator) : base()
		{
			ParentSchemaList = ANode;
			FFriendlyName = AOperatorName;
			ObjectName = ACatalogName;
			ImageIndex = 12;
			SelectedImageIndex = ImageIndex;
			FLocator = ALocator;
		}

		private string FFriendlyName;
		private DebugLocator FLocator;

		protected override void UpdateText()
		{
			Text = ParentSchemaList.UnqualifyObjectName(FFriendlyName);
		}

		public override string GetFilter()
		{
			return String.Format("Name = '{0}'", ObjectName);
		}

		protected override string GetViewExpression()
		{
			return ".System.Operators";
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LMenu.MenuItems.Add
			(
				0, 
				new MenuItem(Strings.ObjectTree_OpenMenuText, new EventHandler(OpenClicked)) { DefaultItem = true }
			);
			return LMenu;
		}

		private void OpenClicked(object ASender, EventArgs AArgs)
		{
			Dataphoria.OpenLocator(FLocator);
		}
	}
}