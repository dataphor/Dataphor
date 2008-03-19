/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.IO;

using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Dataphoria.Designers;

namespace Alphora.Dataphor.Dataphoria.ObjectTree
{
	public class DocumentListNode : BrowseNode
	{
		public DocumentListNode(string ALibraryName)
		{
			Text = "Documents";
			ImageIndex = 7;
			SelectedImageIndex = ImageIndex;

			FLibraryName = ALibraryName;
		}

		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }

		protected override string GetChildExpression()
		{
			return ".Frontend.Documents where Library_Name = ALibraryName over { Name, Type_ID } rename Main";
		}
		
		protected override Alphora.Dataphor.DAE.Runtime.DataParams GetParams()
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			LParams.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "ALibraryName", LibraryName));
			return LParams;
		}

		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			string LName = ARow["Main.Name"].AsString;
			string LType = ARow["Type_ID"].AsString;
			switch (LType)
			{
				case "d4" : return new D4DocumentNode(this, LName, LType);
				case "dfd" : 
				case "dfdx" : return new FormDocumentNode(this, LName, LType);
				default : return new DocumentNode(this, LName, LType);
			}
		}
		
		protected override string AddDocument()
		{
			return 
				String.Format
				(
					"Frontend.Derive('.Frontend.Documents adorn {{ Library_Name {{ default ''{0}'' }} }} tags {{ Frontend.Title = ''Document'' }}', 'Add', 'Main.Library_Name', 'Main.Library_Name')",
					LibraryName
				);
		}
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			
			FAddSeparator.Visible = false;
			FAddMenuItem.Visible = false;
			
			return LMenu;
		}			
		
		protected override void InternalRefresh()
		{
			Dataphoria.ExecuteScript(String.Format(".Frontend.RefreshDocuments('{0}');", LibraryName));
			base.InternalRefresh();
		}

		private string FindUniqueDocumentName(string ALibraryName, string ADocumentName)
		{
			if (!Dataphoria.DocumentExists(ALibraryName, ADocumentName))
				return ADocumentName;

			int LCount = 1;

			int LNumIndex = ADocumentName.Length - 1;
			while ((LNumIndex >= 0) && Char.IsNumber(ADocumentName, LNumIndex))
				LNumIndex--;
			if (LNumIndex < (ADocumentName.Length - 1))
			{
				LCount = Int32.Parse(ADocumentName.Substring(LNumIndex + 1));
				ADocumentName = ADocumentName.Substring(0, LNumIndex + 1);
			}

			string LName;
			do
			{
				LName = ADocumentName + LCount.ToString();
				LCount++;
			}  while (Dataphoria.DocumentExists(ALibraryName, LName));

			return LName;
		}

		public void CopyFromDocument(string ALibraryName, string ADocumentName)
		{
			string LNewDocumentName = FindUniqueDocumentName(LibraryName, ADocumentName);
			Dataphoria.ExecuteScript
			(
				String.Format
				(
					".Frontend.CopyDocument('{0}', '{1}', '{2}', '{3}');",
					DAE.Schema.Object.EnsureRooted(ALibraryName),
					DAE.Schema.Object.EnsureRooted(ADocumentName),
					DAE.Schema.Object.EnsureRooted(LibraryName),
					LNewDocumentName
				)
			);
			ReconcileChildren();
		}

		public void MoveFromDocument(string ALibraryName, string ADocumentName)
		{
			Dataphoria.CheckDocumentOverwrite(LibraryName, ADocumentName);
			Dataphoria.ExecuteScript
			(
				String.Format
				(
					".Frontend.MoveDocument('{0}', '{1}', '{2}', '{3}');",
					DAE.Schema.Object.EnsureRooted(ALibraryName),
					DAE.Schema.Object.EnsureRooted(ADocumentName),
					DAE.Schema.Object.EnsureRooted(LibraryName),
					ADocumentName
				)
			);
			ReconcileChildren();
		}

		public void CopyFromFiles(Array AFileList)
		{
			FileStream LStream;
			DocumentDesignBuffer LBuffer;
			foreach (string LFileName in AFileList)
			{
				LBuffer = new DocumentDesignBuffer(Dataphoria, LibraryName, Path.GetFileNameWithoutExtension(LFileName));
				Dataphoria.CheckDocumentOverwrite(LBuffer.LibraryName, LBuffer.DocumentName);
				LBuffer.DocumentType = Dataphoria.DocumentTypeFromFileName(LFileName);
				LStream = new FileStream(LFileName, FileMode.Open, FileAccess.Read);
				try
				{
					LBuffer.SaveBinaryData(LStream);
				}
				finally
				{
					LStream.Close();
				}
			}
			ReconcileChildren();
		}

		public void MoveFromFiles(Array AFileList)
		{
			CopyFromFiles(AFileList);
			foreach (string LFileName in AFileList)
				File.Delete(LFileName);
		}

		public override void DragDrop(DragEventArgs AArgs)
		{
			DocumentData LSource = AArgs.Data as DocumentData;
			if (LSource != null)
			{
				switch (AArgs.Effect)
				{
					case DragDropEffects.Copy | DragDropEffects.Move :
						new DocumentListDropMenu(LSource, this).Show(TreeView, TreeView.PointToClient(System.Windows.Forms.Control.MousePosition));
						break;
					case DragDropEffects.Copy :
						CopyFromDocument(LSource.Node.LibraryName, LSource.Node.DocumentName);
						break;
					case DragDropEffects.Move :
						MoveFromDocument(LSource.Node.LibraryName, LSource.Node.DocumentName);
						((DocumentListNode)LSource.Node.Parent).ReconcileChildren();
						break;
				}
			}
			else if (AArgs.Data.GetDataPresent(DataFormats.FileDrop))
			{
				Array LFileList = (Array)AArgs.Data.GetData(DataFormats.FileDrop);
				switch (AArgs.Effect)
				{
					case DragDropEffects.Copy | DragDropEffects.Move :
						new DocumentListFileDropMenu(LFileList, this).Show(TreeView, TreeView.PointToClient(System.Windows.Forms.Control.MousePosition));
						break;
					case DragDropEffects.Copy :
						CopyFromFiles(LFileList);
						break;
					case DragDropEffects.Move :
						MoveFromFiles(LFileList);
						break;
				}
			}
		}

		public override void DragOver(DragEventArgs AArgs)
		{
			base.DragOver(AArgs);
			DocumentData LSource = AArgs.Data as DocumentData;
			if (LSource != null)
			{
				if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
				{
					if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
						AArgs.Effect = DragDropEffects.Copy;
					else
						if (LSource.Node.LibraryName != LibraryName)
							AArgs.Effect = DragDropEffects.Move;
				}
				else if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Right)
					AArgs.Effect = DragDropEffects.Copy | DragDropEffects.Move;
			}
			else if (AArgs.Data.GetDataPresent(DataFormats.FileDrop))
			{
				if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
					if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
						AArgs.Effect = DragDropEffects.Move;
					else
						AArgs.Effect = DragDropEffects.Copy;
				else
					AArgs.Effect = DragDropEffects.Copy | DragDropEffects.Move;
			}
			if (AArgs.Effect != DragDropEffects.None)
				TreeView.SelectedNode = this;
		}
	}

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
			
			MenuItem LOpenMenuItem = new MenuItem(Strings.Get("ObjectTree.OpenMenuText"), new EventHandler(OpenClicked));
			LOpenMenuItem.DefaultItem = true;
			LMenu.MenuItems.Add(0, LOpenMenuItem);

			LMenu.MenuItems.Add(1, new MenuItem(Strings.Get("ObjectTree.OpenWithMenuText"), new EventHandler(OpenWithClicked)));
			LMenu.MenuItems.Add(2, new MenuItem("-"));

			return LMenu;
		}

		protected override void UpdateText()
		{
			Text = String.Format("{0}  [{1}]", FDocumentName, FDocumentType);
		}

		public override bool IsEqual(DAE.Runtime.Data.Row ARow)
		{
			return (ARow["Main.Name"].AsString == FDocumentName) && (ARow["Main.Type_ID"].AsString == FDocumentType);
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
								Strings.Get("OtherDesignerOpen"), 
								Strings.Get("OtherDesignerOpenTitle"), 
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

	public class D4DocumentNode : DocumentNode
	{
		public D4DocumentNode(DocumentListNode AParent, string ADocumentName, string ADocumentType) : base(AParent, ADocumentName, ADocumentType) 
		{
			ImageIndex = 9;
			SelectedImageIndex = ImageIndex;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			
			LMenu.MenuItems.Add(2, new MenuItem(Strings.Get("ObjectTree.ExecuteMenuText"), new EventHandler(ExecuteClicked), Shortcut.F9));

			return LMenu;
		}

		private void ExecuteClicked(object ASender, EventArgs AArgs)
		{
			using 
			(
				DAE.Runtime.Data.DataValue LScript = 
					Dataphoria.FrontendSession.Pipe.RequestDocument
					(
						String.Format
						(
							".Frontend.Load('{0}', '{1}')", 
							DAE.Schema.Object.EnsureRooted(LibraryName), 
							DAE.Schema.Object.EnsureRooted(DocumentName)
						)
					)
			)
			{
				Dataphoria.ExecuteScript(LScript.AsString);
			}
		}
	}

	public class FormDocumentNode : DocumentNode
	{
		public FormDocumentNode(DocumentListNode AParent, string ADocumentName, string ADocumentType) : base(AParent, ADocumentName, ADocumentType) 
		{
			ImageIndex = 10;
			SelectedImageIndex = ImageIndex;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			
			LMenu.MenuItems.Add(2, new MenuItem(Strings.Get("ObjectTree.CustomizeMenuText"), new EventHandler(CustomizeClicked)));
			LMenu.MenuItems.Add(3, new MenuItem(Strings.Get("ObjectTree.ShowMenuText"), new EventHandler(StartClicked), Shortcut.F9));

			return LMenu;
		}

		private string GetDocumentExpression()
		{
			return 
				String.Format
				(
					".Frontend.Form('{0}', '{1}')",
					DAE.Schema.Object.EnsureRooted(LibraryName),
					DAE.Schema.Object.EnsureRooted(DocumentName)
				);
		}

		private void CustomizeClicked(object ASender, EventArgs AArgs)
		{
			Dataphor.Dataphoria.FormDesigner.CustomFormDesigner LDesigner = new Dataphor.Dataphoria.FormDesigner.CustomFormDesigner(Dataphoria, "DFDX");
			try
			{
				BOP.Ancestors LAncestors = new BOP.Ancestors();
				LAncestors.Add(GetDocumentExpression());
				LDesigner.New(LAncestors);
				((IDesigner)LDesigner).Show();
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
			
		}

		private void StartClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.Session LSession = Dataphoria.GetLiveDesignableFrontendSession();
			try
			{
				LSession.SetLibrary(LibraryName);
				LSession.StartCallback(GetDocumentExpression(), null);
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}
	}

	public class DocumentData : DataObject
	{
		public DocumentData(DocumentNode ANode)
		{
			FNode = ANode;
		}

		private DocumentNode FNode;
		public DocumentNode Node
		{
			get { return FNode; }
		}

		public override object GetData(string AFormat)
		{
			if (AFormat == DataFormats.FileDrop)
			{
				// produce a file for copy move operation
				// TODO: figure out how to delete this file after the caller is done with it (if they don't move it)
				DocumentDesignBuffer LBuffer = new DocumentDesignBuffer(FNode.Dataphoria, FNode.LibraryName, FNode.DocumentName);
				string LFileName = String.Format("{0}{1}.{2}", Path.GetTempPath(), LBuffer.DocumentName, FNode.DocumentType);
				using (FileStream LStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
				{
					LBuffer.LoadData(LStream);
				}
				return new string[] {LFileName};
			}
			else
				return null;
		}

		public override object GetData(string AFormat, bool AAutoConvert)
		{
			return GetData(AFormat);
		}

		public override bool GetDataPresent(string AFormat)
		{
			return AFormat == DataFormats.FileDrop;
		}

		public override bool GetDataPresent(string AFormat, bool AAutoConvert)
		{
			return GetDataPresent(AFormat);
		}

		public override string[] GetFormats()
		{
			return new string[] {DataFormats.FileDrop};
		}

		public override string[] GetFormats(bool AAutoConvert)
		{
			return GetFormats();
		}
	}

	public class DocumentListDropMenu : ContextMenu
	{
		public DocumentListDropMenu(DocumentData ASource, DocumentListNode ATarget)
		{
			FSource = ASource;
			FTarget = ATarget;

			MenuItems.Add(new MenuItem(Strings.Get("DropMenu.Copy"), new EventHandler(CopyClick)));
			MenuItems.Add(new MenuItem(Strings.Get("DropMenu.Move"), new EventHandler(MoveClick)));
			MenuItems.Add(new MenuItem("-"));
			MenuItems.Add(new MenuItem(Strings.Get("DropMenu.Cancel")));
		}

		private DocumentData FSource;
		private DocumentListNode FTarget;

		private void CopyClick(object ASender, EventArgs AArgs)
		{
			FTarget.CopyFromDocument(FSource.Node.LibraryName, FSource.Node.DocumentName);
		}

		private void MoveClick(object ASender, EventArgs AArgs)
		{
			FTarget.MoveFromDocument(FSource.Node.LibraryName, FSource.Node.DocumentName);
			((DocumentListNode)FSource.Node.Parent).ReconcileChildren();
		}
	}

	public class DocumentListFileDropMenu : ContextMenu
	{
		public DocumentListFileDropMenu(Array ASource, DocumentListNode ATarget)
		{
			FSource = ASource;
			FTarget = ATarget;

			MenuItems.Add(new MenuItem(Strings.Get("DropMenu.Copy"), new EventHandler(CopyClick)));
			MenuItems.Add(new MenuItem(Strings.Get("DropMenu.Move"), new EventHandler(MoveClick)));
			MenuItems.Add(new MenuItem("-"));
			MenuItems.Add(new MenuItem(Strings.Get("DropMenu.Cancel")));
		}

		private Array FSource;
		private DocumentListNode FTarget;

		private void CopyClick(object ASender, EventArgs AArgs)
		{
			FTarget.CopyFromFiles(FSource);
		}

		private void MoveClick(object ASender, EventArgs AArgs)
		{
			FTarget.MoveFromFiles(FSource);
		}
	}

}
