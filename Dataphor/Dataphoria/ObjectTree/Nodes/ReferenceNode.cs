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
	public class ReferenceListNode : SchemaListNode
	{
		public ReferenceListNode(string ALibraryName) : base (ALibraryName)
		{
			Text = "References";
			ImageIndex = 16;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.References " + CSchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new ReferenceNode(this, ARow["Name"].AsString);
		}
		
	}

	public class ReferenceNode : SchemaItemNode
	{
		public ReferenceNode(ReferenceListNode ANode, string AReferenceName) : base()
		{
			ParentSchemaList = ANode;
			ObjectName = AReferenceName;
			ImageIndex = 17;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetViewExpression()
		{
			return ".System.References";
		}
	}
}