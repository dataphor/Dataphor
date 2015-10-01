/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.ObjectTree.Nodes;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DocumentNode : EditableItemNode
	{
		public DocumentNode(DocumentListNode parent, string documentName, string documentType) : base()
		{
			_documentType = documentType;
			_documentName = documentName;
			UpdateText();
			ImageIndex = 8;
			SelectedImageIndex = ImageIndex;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			
			MenuItem openMenuItem = new MenuItem(Strings.ObjectTree_OpenMenuText, new EventHandler(OpenClicked));
			openMenuItem.DefaultItem = true;
			menu.MenuItems.Add(0, openMenuItem);

			menu.MenuItems.Add(1, new MenuItem(Strings.ObjectTree_OpenWithMenuText, new EventHandler(OpenWithClicked)));
			menu.MenuItems.Add(2, new MenuItem("-"));

			return menu;
		}

		protected override void UpdateText()
		{
			Text = String.Format("{0}  [{1}]", _documentName, _documentType);
		}

		public override bool IsEqual(DAE.Runtime.Data.IRow row)
		{
			return ((string)row["Main.Name"] == _documentName) && ((string)row["Main.Type_ID"] == _documentType);
		}

		public override string GetFilter()
		{
			return String.Format("(Library_Name = '{0}') and (Main.Name = '{1}')", LibraryName, _documentName);
		}

		protected override string KeyColumnName()
		{
			return "Main.Name";
		}

		private string _documentName;
		public string DocumentName
		{
			get { return _documentName; }
		}

		private string _documentType;
		public string DocumentType
		{
			get { return _documentType; }
		}

		public string LibraryName
		{
			get { return ((LibraryNode)Parent.Parent).LibraryName; }
		}

		protected override void NameChanged(string newName)
		{
			_documentName = newName;
			base.NameChanged(newName);
		}

		protected DocumentDesignBuffer GetBuffer()
		{
			return new DocumentDesignBuffer(Dataphoria, Alphora.Dataphor.DAE.Schema.Object.EnsureRooted(LibraryName), _documentName);
		}

		private void OpenDesigner(DesignerInfo info)
		{
			Dataphoria.OpenDesigner(info, GetBuffer());
		}

		private void OpenClicked(object sender, EventArgs args)
		{
			IDesigner designer = Dataphoria.GetDesigner(GetBuffer());
			if (designer != null)
				designer.Select();
			else
				OpenDesigner(Dataphoria.GetDefaultDesigner(_documentType));
		}

		private void OpenWithClicked(object sender, EventArgs args)
		{
			DesignerInfo info = Dataphoria.ChooseDesigner(_documentType);
			IDesigner designer = Dataphoria.GetDesigner(GetBuffer());
			if (designer != null)
			{
				if (designer.DesignerID != info.ID)
				{
					if 
					(
						(
							MessageBox.Show
							(
								Strings.OtherDesignerOpen, 
								Strings.OtherDesignerOpenTitle, 
								MessageBoxButtons.YesNo, 
								MessageBoxIcon.Question, 
								MessageBoxDefaultButton.Button1
							) == DialogResult.Yes
						) &&
						designer.CloseSafely()
					)
						OpenDesigner(info);
				}
				else
					designer.Select();
			}
			else
				OpenDesigner(info);
		}

		protected override string EditDocument()
		{
			return "Frontend.Derive('.Frontend.Documents', 'Edit', 'Main.Library_Name', 'Main.Library_Name')";
		}

		protected override string DeleteDocument()
		{
			return "Frontend.Derive('.Frontend.Documents', 'Delete', 'Main.Library_Name', 'Main.Library_Name', false)";
		}
		
		protected override string ViewDocument()
		{
			return "Frontend.Derive('.Frontend.Documents', 'View', 'Main.Library_Name', 'Main.Library_Name', false)";
		}

		public override void ItemDrag()
		{
			MouseButtons button = System.Windows.Forms.Control.MouseButtons;
			if ((button == MouseButtons.Left) || (button == MouseButtons.Right))
				TreeView.DoDragDrop(new DocumentData(this), DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link);
			else
				base.ItemDrag();
		}

		public override void Delete()
		{
			IDesigner designer = Dataphoria.GetDesigner(GetBuffer());
			if ((designer == null) || ((designer != null) && designer.CloseSafely()))
				base.Delete();
		}
	}
}
