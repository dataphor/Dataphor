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
			return ".System.Operators " + CSchemaListFilter + " add { OperatorName + Signature DisplayName } over { Name, DisplayName }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new OperatorNode(this, ARow["Name"].AsString, ARow["DisplayName"].AsString);
		}
	}

	public class OperatorNode : SchemaItemNode
	{
		public OperatorNode(OperatorListNode ANode, string ACatalogName, string AOperatorName) : base()
		{
			ParentSchemaList = ANode;
			FFriendlyName = AOperatorName;
			ObjectName = ACatalogName;
			ImageIndex = 12;
			SelectedImageIndex = ImageIndex;
		}

		private string FFriendlyName;

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
	}
}