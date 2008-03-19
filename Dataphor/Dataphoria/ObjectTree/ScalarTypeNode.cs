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
	public class ScalarTypeListNode : SchemaListNode
	{
		public ScalarTypeListNode(string ALibraryName) : base (ALibraryName)
		{
			Text = "Types";
			ImageIndex = 22;
			SelectedImageIndex = ImageIndex;
		}
		
		protected override string GetChildExpression()
		{
			return ".System.ScalarTypes " + CSchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new ScalarTypeNode(this, ARow["Name"].AsString);
		}
		
	}

	public class ScalarTypeNode : SchemaItemNode
	{
		public ScalarTypeNode(ScalarTypeListNode ANode, string AScalarTypeName) : base()
		{
			ParentSchemaList = ANode;
			ObjectName = AScalarTypeName;
			ImageIndex = 23;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetViewExpression()
		{
			return ".System.ScalarTypes";
		}
	}
}