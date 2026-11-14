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
	public class ViewListNode : SchemaListNode
	{
		public ViewListNode(string libraryName)
			: base(libraryName)
		{
			Text = Strings.ObjectTree_ViewListNodeText;
			ImageIndex = 29;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.DerivedTableVars " + SchemaListFilter + " over { Name }";
		}

		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new ViewNode(this, (string)row["Name"]);
		}
	}

	public class ViewNode : BaseTableNode
	{
		public ViewNode(SchemaListNode parent, string name)
			: base(parent, name)
		{
			ImageIndex = 30;
			SelectedImageIndex = 30;
		}

		protected override string GetViewExpression()
		{
			return ".System.DerivedTableVars";
		}
	}
}
