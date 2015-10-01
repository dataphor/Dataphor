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
		public OperatorListNode(string libraryName) : base (libraryName)
		{
			Text = "Operators";
			ImageIndex = 12;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.Operators " + SchemaListFilter + " { Name, OperatorName + Signature DisplayName, Locator, Line, LinePos }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new OperatorNode(this, (string)row["Name"], (string)row["DisplayName"], new DebugLocator((string)row["Locator"], (int)row["Line"], (int)row["LinePos"]));
		}
	}

	public class OperatorNode : SchemaItemNode
	{
		public OperatorNode(OperatorListNode node, string catalogName, string operatorName, DebugLocator locator) : base()
		{
			ParentSchemaList = node;
			_friendlyName = operatorName;
			ObjectName = catalogName;
			ImageIndex = 12;
			SelectedImageIndex = ImageIndex;
			_locator = locator;
		}

		private string _friendlyName;
		private DebugLocator _locator;

		protected override void UpdateText()
		{
			Text = ParentSchemaList.UnqualifyObjectName(_friendlyName);
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
			ContextMenu menu = base.GetContextMenu();
			menu.MenuItems.Add
			(
				0, 
				new MenuItem(Strings.ObjectTree_OpenMenuText, new EventHandler(OpenClicked)) { DefaultItem = true }
			);
			return menu;
		}

		private void OpenClicked(object sender, EventArgs args)
		{
			Dataphoria.OpenLocator(_locator);
		}
	}
}