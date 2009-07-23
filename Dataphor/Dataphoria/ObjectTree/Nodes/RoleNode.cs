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
	public class RoleListNode : SchemaListNode
	{
		public RoleListNode(string ALibraryName) : base (ALibraryName)
		{
			Text = Strings.ObjectTree_RolesNodeText;
			ImageIndex = 4;
			SelectedImageIndex = ImageIndex;
		}
		
		protected override string GetChildExpression()
		{
			return ".System.Roles " + CSchemaListFilter + " over { Name }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new RoleNode(this, (string)ARow["Name"]);
		}
		
	}

	public class RoleNode : SchemaItemNode
	{
		public RoleNode(RoleListNode ANode, string ARoleName) : base()
		{
			ParentSchemaList = ANode;
			ObjectName = ARoleName;
			ImageIndex = 4;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetViewExpression()
		{
			return ".System.Roles";
		}
	}
}
