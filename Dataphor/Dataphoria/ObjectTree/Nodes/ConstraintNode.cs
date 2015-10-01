/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Specialized;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public class ConstraintListNode : SchemaListNode
	{
		public ConstraintListNode(string libraryName) : base (libraryName)
		{
			Text = "Constraints";
			ImageIndex = 20;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.CatalogConstraints " + SchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new ConstraintNode(this, (string)row["Name"]);
		}
	}

	public class ConstraintNode : SchemaItemNode
	{
		public ConstraintNode(ConstraintListNode node, string constraintName) : base()
		{
			ParentSchemaList = node;
			ObjectName = constraintName;
			ImageIndex = 21;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetViewExpression()
		{
			return ".System.CatalogConstraints";
		}
	}
}