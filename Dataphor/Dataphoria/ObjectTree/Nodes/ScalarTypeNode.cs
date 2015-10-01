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
	public class ScalarTypeListNode : SchemaListNode
	{
		public ScalarTypeListNode(string libraryName) : base (libraryName)
		{
			Text = "Types";
			ImageIndex = 22;
			SelectedImageIndex = ImageIndex;
		}
		
		protected override string GetChildExpression()
		{
			return ".System.ScalarTypes " + SchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new ScalarTypeNode(this, (string)row["Name"]);
		}
		
	}

	public class ScalarTypeNode : SchemaItemNode
	{
		public ScalarTypeNode(ScalarTypeListNode node, string scalarTypeName) : base()
		{
			ParentSchemaList = node;
			ObjectName = scalarTypeName;
			ImageIndex = 23;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetViewExpression()
		{
			return ".System.ScalarTypes";
		}
	}
}