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
		public DocumentNode(DocumentListNode AParent, string ADocumentName, string ADocumentType) : base()
		{
			FDocumentType = ADocumentType;
			FDocumentName = ADocumentName;
			UpdateText();
			ImageIndex = 8;
			SelectedImageIndex = ImageIndex;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			
			MenuItem LOpenMenuItem = new MenuItem(Strings.ObjectTree_OpenMenuText, new EventHandler(OpenClicked));
			LOpenMenuItem.DefaultItem = true;
			LMenu.MenuItems.Add(0, LOpenMenuItem);

			LMenu.MenuItems.Add(1, new MenuItem(Strings.ObjectTree_OpenWithMenuText, new EventHandler(OpenWithClicked)));
			LMenu.MenuItems.Add(2, new MenuItem("-"));

			return LMenu;
		}

		protected override void UpdateText()
		{
			Text = String.Format("{0}  [{1}]", FDocumentName, FDocumentType);
		}

		public override bool IsEqual(DAE.Runtime.Data.Row ARow)
		{
			return ((string)ARow["Main.Name"] == FDocumentName) && ((string)ARow["Main.Type_ID"] == FDocumentType);
		}

		public override string GetFilter()
		{
			return String.Format("(Library_Name = '{0}') and (Main.Name = '{1}')", LibraryName, FDocumentName);
		}

		protected override string KeyColumnName()
		{
			return "Main.Name";
		}

		private string FDocumentName;
		public string DocumentName
		{
			get { return FDocumentName; }
		}

		private string FDocumentType;
		public string DocumentType
		{
			get { return FDocumentType; }
		}

		public string LibraryName
		{
			get { return ((LibraryNode)Parent.Parent).LibraryName; }
		}

		protected override void NameChanged(string ANewName)
		{
			FDocumentName = ANewName;
			base.NameChanged(ANewName);
		}

		protected DocumentDesignBuffer GetBuffer()
		{
			return new DocumentDesignBuffer(Dataphoria, LibraryName, FDocumentName);
		}

		private void OpenDesigner(DesignerInfo AInfo)
		{
			Dataphoria.OpenDesigner(AInfo, GetBuffer());
		}

		private void OpenClicked(object ASender, EventArgs AArgs)
		{
			IDesigner LDesigner = Dataphoria.GetDesigner(GetBuffer());
			if (LDesigner != null)
				LDesigner.Select();
			else
				OpenDesigner(Dataphoria.GetDefaultDesigner(FDocumentType));
		}

		private void OpenWithClicked(object ASender, EventArgs AArgs)
		{
			DesignerInfo LInfo = Dataphoria.ChooseDesigner(FDocumentType);
			IDesigner LDesigner = Dataphoria.GetDesigner(GetBuffer());
			if (LDesigner != null)
			{
				if (LDesigner.DesignerID != LInfo.ID)
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
						LDesigner.CloseSafely()
					)
						OpenDesigner(LInfo);
				}
				else
					LDesigner.Select();
			}
			else
				OpenDesigner(LInfo);
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
			MouseButtons LButton = System.Windows.Forms.Control.MouseButtons;
			if ((LButton == MouseButtons.Left) || (LButton == MouseButtons.Right))
				TreeView.DoDragDrop(new DocumentData(this), DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link);
			else
				base.ItemDrag();
		}

		public override void Delete()
		{
			IDesigner LDesigner = Dataphoria.GetDesigner(GetBuffer());
			if ((LDesigner == null) || ((LDesigner != null) && LDesigner.CloseSafely()))
				base.Delete();
		}
	}
}
