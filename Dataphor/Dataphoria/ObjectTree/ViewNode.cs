/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria;

namespace Alphora.Dataphor.Dataphoria.ObjectTree
{
	public class ViewListNode : SchemaListNode
	{
		public ViewListNode(string ALibraryName)
			: base(ALibraryName)
		{
			Text = Strings.Get("ObjectTree.ViewListNodeText");
			ImageIndex = 29;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.DerivedTableVars " + CSchemaListFilter + " over { Name }";
		}

		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new ViewNode(this, ARow["Name"].AsString);
		}
	}

	public class ViewNode : BaseTableNode
	{
		public ViewNode(SchemaListNode AParent, string AName)
			: base(AParent, AName)
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