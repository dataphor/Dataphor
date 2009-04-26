/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt

	Assumption: this designer assumes that its lifetime is for a single document.  It will 
	 currently not close any existing document before operations such a New(), Open().  This
	 behavior is okay for now because Dataphoria does not ask designers to change documents.
*/

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Web;
using System.Text;
using System.Xml;

using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.BOP;

using Syncfusion.Windows.Forms.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
	// Don't put any definitions above the FormDesiger class

    public partial class FormDesigner : BaseForm, ILiveDesigner, IErrorSource, IServiceProvider, IContainer, IChildFormWithToolBar
	{



        protected DockContent FDockContentPalettePanel;
        protected DockContent FDockContentFormPanel;
        protected DockContent FDockContentNodesTree;
        
        protected DockContent FDockContentPropertyGrid;

		public FormDesigner()	// dummy constructor for SyncFusion's MDI menu merging
		{
			InitializeComponent();
            InitializeDocking();
		}

		public FormDesigner(IDataphoria ADataphoria, string ADesignerID)
		{
			InitializeComponent();



            InitializeDocking();

            
            FDesignerID = ADesignerID;

			FNodesTree.FormDesigner = this;

			InitializeService(ADataphoria);



			PrepareSession();
			ADataphoria.OnFormDesignerLibrariesChanged += new EventHandler(FormDesignerLibrariesChanged);
		}

        private void InitializeDocking()
        {
            // 
            // FPaletteGroupBar
            // 
            this.FPaletteGroupBar = new Syncfusion.Windows.Forms.Tools.GroupBar();
            this.FPaletteGroupBar.AllowDrop = true;
            this.FPaletteGroupBar.BackColor = System.Drawing.SystemColors.Control;
            this.FPaletteGroupBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.FPaletteGroupBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FPaletteGroupBar.Location = new System.Drawing.Point(0, 24);
            this.FPaletteGroupBar.Name = "FPaletteGroupBar";
            this.FPaletteGroupBar.SelectedItem = 0;
            this.FPaletteGroupBar.Size = new System.Drawing.Size(163, 163);
            this.FPaletteGroupBar.TabIndex = 1;
            // 
            // FPointerGroupView
            // 
            this.FPointerGroupView = new Syncfusion.Windows.Forms.Tools.GroupView();
            this.FPointerGroupView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.FPointerGroupView.ButtonView = true;
            this.FPointerGroupView.Dock = System.Windows.Forms.DockStyle.Top;
            this.FPointerGroupView.GroupViewItems.AddRange(new Syncfusion.Windows.Forms.Tools.GroupViewItem[] {
																												  new Syncfusion.Windows.Forms.Tools.GroupViewItem("Pointer", 0)});
            this.FPointerGroupView.IntegratedScrolling = true;
            this.FPointerGroupView.ItemYSpacing = 2;
            this.FPointerGroupView.LargeImageList = null;
            this.FPointerGroupView.Location = new System.Drawing.Point(0, 0);
            this.FPointerGroupView.Name = "FPointerGroupView";
            this.FPointerGroupView.SelectedItem = 0;
            this.FPointerGroupView.Size = new System.Drawing.Size(163, 24);
            this.FPointerGroupView.SmallImageList = this.FPointerImageList;
            this.FPointerGroupView.SmallImageView = true;
            this.FPointerGroupView.TabIndex = 0;
            this.FPointerGroupView.Text = "groupView2";
            this.FPointerGroupView.GroupViewItemSelected += new System.EventHandler(this.FPointerGroupView_GroupViewItemSelected);

            // 
            // FPalettePanel
            // 
            this.FPalettePanel = new System.Windows.Forms.Panel();
            this.FPalettePanel.Controls.Add(this.FPaletteGroupBar);
            this.FPalettePanel.Controls.Add(this.FPointerGroupView);
            //this.FDockingManager.SetEnableDocking(this.FPalettePanel, true);
            this.FPalettePanel.Location = new System.Drawing.Point(1, 21);
            this.FPalettePanel.Name = "FPalettePanel";
            this.FPalettePanel.Size = new System.Drawing.Size(163, 187);
            this.FPalettePanel.TabIndex = 1;
            this.FPalettePanel.Dock = DockStyle.Fill;

            FDockContentPalettePanel = new DockContent();
            FDockContentPalettePanel.Controls.Add(FPalettePanel);
            FDockContentPalettePanel.TabText = "Forms Palette";
            FDockContentPalettePanel.Text = "Palette";
            FDockContentPalettePanel.ShowHint = DockState.DockLeft;
            FDockContentPalettePanel.Show(FDockPanel);

            // 
            // FFormPanel
            // 
            this.FFormPanel = new Alphora.Dataphor.Dataphoria.FormDesigner.FormPanel();
            this.FFormPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            //this.FDockingManager.SetEnableDocking(this.FFormPanel, true);
            this.FFormPanel.Location = new System.Drawing.Point(1, 21);
            this.FFormPanel.Name = "FFormPanel";
            this.FFormPanel.Size = new System.Drawing.Size(685, 283);
            this.FFormPanel.TabIndex = 3;
            this.FFormPanel.Dock = DockStyle.Fill;

            FDockContentFormPanel = new DockContent();
            FDockContentFormPanel.Controls.Add(FFormPanel);
            FDockContentFormPanel.ShowHint = DockState.Document;
            FDockContentFormPanel.Show(this.FDockPanel);

            // 
            // FNodesTree
            // 
            this.FNodesTree = new Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree();
            this.FNodesTree.AllowDrop = true;
            this.FNodesTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.FNodesTree.CausesValidation = false;
            this.FNodesTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FNodesTree.HideSelection = false;
            this.FNodesTree.ImageList = this.FNodesImageList;
            this.FNodesTree.Location = new System.Drawing.Point(0, 0);
            this.FNodesTree.Name = "FNodesTree";
            this.FNodesTree.ShowRootLines = false;
            this.FNodesTree.Size = new System.Drawing.Size(283, 209);
            this.FNodesTree.TabIndex = 0;
            this.FNodesTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FNodesTree_AfterSelect);
            this.FNodesTree.Dock = DockStyle.Fill;


            FDockContentNodesTree = new DockContent();
            FDockContentNodesTree.Controls.Add(FNodesTree);
            FDockContentNodesTree.TabText = "Forms Nodes Tree";
            FDockContentNodesTree.Text = "Nodes Tree";
            FDockContentNodesTree.ShowHint = DockState.DockRight;
            FDockContentNodesTree.Show(FDockPanel);


            this.FPropertyGrid = new System.Windows.Forms.PropertyGrid();
            // 
            // FPropertyGrid
            // 
            this.FPropertyGrid.BackColor = System.Drawing.SystemColors.Control;
            this.FPropertyGrid.CausesValidation = false;
            this.FPropertyGrid.CommandsVisibleIfAvailable = true;
            this.FPropertyGrid.Cursor = System.Windows.Forms.Cursors.HSplit;
            //this.FDockingManager.SetEnableDocking(this.FPropertyGrid, true);
            this.FPropertyGrid.LargeButtons = false;
            this.FPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.FPropertyGrid.Location = new System.Drawing.Point(1, 21);
            this.FPropertyGrid.Name = "FPropertyGrid";
            this.FPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            this.FPropertyGrid.Size = new System.Drawing.Size(229, 187);
            this.FPropertyGrid.TabIndex = 2;
            this.FPropertyGrid.Text = "Properties of the Currently Selected Node";
            this.FPropertyGrid.ToolbarVisible = false;
            this.FPropertyGrid.ViewBackColor = System.Drawing.SystemColors.Window;
            this.FPropertyGrid.ViewForeColor = System.Drawing.SystemColors.WindowText;
            this.FPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.NodePropertyGrid_PropertyValueChanged);
            this.FPropertyGrid.Dock = DockStyle.Fill;

            FDockContentPropertyGrid = new DockContent();
            FDockContentPropertyGrid.Controls.Add(FPropertyGrid);
            FDockContentPropertyGrid.TabText = "Forms Properties Grid";
            FDockContentPropertyGrid.Text = "Properties Grid";
            FDockContentPropertyGrid.ShowHint = DockState.DockRight;
            FDockContentPropertyGrid.Show(FDockPanel);
        }

		

		// Dataphoria

		[Browsable(false)]
		public IDataphoria Dataphoria
		{
			get { return (FService == null ? null : FService.Dataphoria); }
		}

		

		protected override void OnClosing(System.ComponentModel.CancelEventArgs AArgs) 
		{
			base.OnClosing(AArgs);
			try
			{
				FService.CheckModified();
				if (FIsDesignHostOwner && (!FrontendSession.CloseAllForms(FDesignHost, CloseBehavior.AcceptOrClose)))	// if we are hosting, close the child forms
					throw new AbortException();
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}

		#region FrontendSession

		private Frontend.Client.Windows.Session FFrontendSession;
		[Browsable(false)]
		public Frontend.Client.Windows.Session FrontendSession
		{
			get { return FFrontendSession; }
		}

		/// <summary> Prepares (or re-prepares) the frontend session and the component palette </summary>
		private void PrepareSession()
		{
			if (FFrontendSession == null)
				FFrontendSession = Dataphoria.GetLiveDesignableFrontendSession();
			FFrontendSession.SetFormDesigner();
			ClearPalette();
			LoadPalette();
		}

		private void FormDesignerLibrariesChanged(object ASender, EventArgs AArgs)
		{
			PrepareSession();
		}

		#endregion

		#region Service

		public void InitializeService(IDataphoria ADataphoria)
		{
			FService = new DesignService(ADataphoria, this);
			FService.OnModifiedChanged += new EventHandler(NameOrModifiedChanged);
			FService.OnNameChanged += new EventHandler(NameOrModifiedChanged);
			FService.OnRequestLoad += new RequestHandler(RequestLoad);
			FService.OnRequestSave += new RequestHandler(RequestSave);
		}

		private IDesignService FService;
		[Browsable(false)]
		public IDesignService Service
		{
			get { return FService; }
		}

		private void NameOrModifiedChanged(object ASender, EventArgs AArgs)
		{
			UpdateTitle();
		}

		protected virtual void RequestLoad(DesignService AService, DesignBuffer ABuffer)
		{
			SetDesignHost(HostFromBuffer(ABuffer), true);
		}

		protected virtual void RequestSave(DesignService AService, DesignBuffer ABuffer)
		{
			Frontend.Client.Serializer LSerializer = FrontendSession.CreateSerializer();
			XmlDocument LDocument = new XmlDocument();
			LSerializer.Serialize(LDocument, FDesignHost.Children[0]);
			Dataphoria.Warnings.AppendErrors(this, LSerializer.Errors, true);

            MemoryStream LStream = new MemoryStream();
            XmlTextWriter LXMLTextWriter = new XmlTextWriter(LStream, Encoding.UTF8);
            LXMLTextWriter.Formatting = Formatting.Indented;
            LDocument.Save(LXMLTextWriter);
            byte[] LWriterString = LStream.ToArray();
            ABuffer.SaveData(Encoding.UTF8.GetString(LWriterString, 0, LWriterString.Length));

            UpdateHostsDocument(ABuffer);

		}

		#endregion

		#region Tree Nodes

		private void FNodesTree_AfterSelect(object ASender, TreeViewEventArgs AArgs)
		{
			ActivateNode((DesignerNode)AArgs.Node);
		}

		public void ActivateNode(DesignerNode ANode)
		{
			if ((FPropertyGrid.SelectedObject != null) && (FPropertyGrid.SelectedObject is IDisposableNotify))
				((IDisposableNotify)FPropertyGrid.SelectedObject).Disposed -= new EventHandler(SelectedNodeDisposed);

			bool LEditsAllowed;
			if (ANode == null)
			{
				FPropertyGrid.SelectedObject = null;
				LEditsAllowed = false;
			}
			else
			{
				FPropertyGrid.SelectedObject = ANode.Node;
				ANode.Node.Disposed += new EventHandler(SelectedNodeDisposed);
				LEditsAllowed = !ANode.ReadOnly;
			}
			FDeleteToolStripMenuItem.Enabled = LEditsAllowed;
            FRenameToolStripMenuItem.Enabled = LEditsAllowed;
            FCutToolStripMenuItem.Enabled = LEditsAllowed;
		}

		private void SelectedNodeDisposed(object ASender, EventArgs AArgs)
		{
			ActivateNode(FNodesTree.SelectedNode);
		}

		private void NodePropertyGrid_PropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
		{
			FService.SetModified(true);
		}

		#endregion

		#region IErrorSource

		void IErrorSource.ErrorHighlighted(Exception AException)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception AException)
		{
			this.Focus();
		}

		#endregion

		#region Palette

		private void ClearPalette()
		{
			FPaletteGroupBar.GroupBarItems.Clear();
		}

		private bool IsTypeListed(Type AType)
		{
			ListInDesignerAttribute LListIn = (ListInDesignerAttribute)ReflectionUtility.GetAttribute(AType, typeof(ListInDesignerAttribute));
			if (LListIn != null)  
				return LListIn.IsListed;
			else
				return true;
		}

		private string GetDescription(Type AType)
		{
			DescriptionAttribute LDescription = (DescriptionAttribute)ReflectionUtility.GetAttribute(AType, typeof(DescriptionAttribute));
			if (LDescription != null) 
				return LDescription.Description;
			else
				return String.Empty;
		}

		private string GetDesignerCategory(Type AType)
		{
			DesignerCategoryAttribute LCategory = (DesignerCategoryAttribute)ReflectionUtility.GetAttribute(AType, typeof(DesignerCategoryAttribute));
			if (LCategory != null) 
				return LCategory.Category;
			else
				return Strings.UnspecifiedCategory;
		}

		private Hashtable FImageIndex = new Hashtable();

		private System.Drawing.Image LoadImage(string AImageExpression)
		{
			try
			{
				using (DAE.Runtime.Data.DataValue LImageData = FrontendSession.Pipe.RequestDocument(AImageExpression))
				{
					MemoryStream LStreamCopy = new MemoryStream();
					Stream LStream = LImageData.OpenStream();
					try
					{
						StreamUtility.CopyStream(LStream, LStreamCopy);
					}
					finally
					{
						LStream.Close();
					}
					return System.Drawing.Image.FromStream(LStreamCopy);
				}
			}
			catch (Exception LException)
			{
				Dataphoria.Warnings.AppendError(this, LException, true);
				// Don't rethrow
			}
			return null;
		}

		public int GetDesignerImage(Type AType)
		{
			DesignerImageAttribute LImageAttribute = (DesignerImageAttribute)ReflectionUtility.GetAttribute(AType, typeof(DesignerImageAttribute));
			if (LImageAttribute != null)
			{
				object LIndexResult = FImageIndex[LImageAttribute.ImageExpression];
				if (LIndexResult == null)
				{
					System.Drawing.Image LImage = LoadImage(LImageAttribute.ImageExpression);
					if (LImage != null)
					{
						if (LImage is Bitmap)
							((Bitmap)LImage).MakeTransparent();
						FNodesImageList.Images.Add(LImage);
						int LIndex = FNodesImageList.Images.Count - 1;
						FImageIndex.Add(LImageAttribute.ImageExpression, LIndex);
						return LIndex;
					}
					else
						FImageIndex.Add(LImageAttribute.ImageExpression, 0);
				}
				else
					return (int)LIndexResult;
			}
			return 0;	// Zero is the reserved index for the default image
		}

		private GroupView EnsureCategory(string ACategoryName)
		{
			GroupBarItem LItem = FindPaletteBarItem(ACategoryName);
			if (LItem == null)
			{
				GroupView LView = new GroupView();
				LView.BorderStyle = System.Windows.Forms.BorderStyle.None;
				LView.IntegratedScrolling = false;
				LView.ItemYSpacing = 2;
				LView.SmallImageList = FNodesImageList;
				LView.SmallImageView = true;
				LView.SelectedTextColor = Color.Navy;
				LView.GroupViewItemSelected += new EventHandler(CategoryGroupViewItemSelected);

				LItem = new GroupBarItem();
				LItem.Client = LView;
				LItem.Text = ACategoryName;
				FPaletteGroupBar.GroupBarItems.Add(LItem);
			}
			return (GroupView)LItem.Client;
		}

		private void LoadPalette()
		{
			PaletteItem LItem;
			NodeTypeEntry LNodeTypeEntry;
			Type LType;

			foreach (String LName in FrontendSession.NodeTypeTable.Keys) 
			{
				LNodeTypeEntry = FrontendSession.NodeTypeTable[LName];
				LType = FrontendSession.NodeTypeTable.GetClassType(LName);

				if (IsTypeListed(LType))
				{
					LItem = new PaletteItem();
					LItem.ClassName = LType.Name;
					LItem.Text = LType.Name;
					LItem.Description = GetDescription(LType);
					LItem.ImageIndex = GetDesignerImage(LType);
					EnsureCategory(GetDesignerCategory(LType)).GroupViewItems.Add(LItem);
				}
			}
		}

		private PaletteItem FSelectedPaletteItem;
		[Browsable(false)]
		public PaletteItem SelectedPaletteItem
		{
			get { return FSelectedPaletteItem; }
		}

		public void SelectPaletteItem(PaletteItem AItem, bool AIsMultiDrop)
		{
			if (AItem != FSelectedPaletteItem)
			{
				FIsMultiDrop = AIsMultiDrop && (AItem != null);

				if (FSelectedPaletteItem != null)
				{
					FSelectedPaletteItem.GroupView.ButtonView = false;
					FSelectedPaletteItem.GroupView.SelectedTextColor = Color.Navy;
				}

				FSelectedPaletteItem = AItem;

				if (FSelectedPaletteItem != null)
				{
					FSelectedPaletteItem.GroupView.ButtonView = true;
					FSelectedPaletteItem.GroupView.SelectedItem = FSelectedPaletteItem.GroupView.GroupViewItems.IndexOf(FSelectedPaletteItem);

					if (FIsMultiDrop)
						FSelectedPaletteItem.GroupView.SelectedTextColor = Color.Blue;

					FNodesTree.PaletteItem = (PaletteItem)FSelectedPaletteItem;
					SetStatus(FSelectedPaletteItem.Description);
					FPointerGroupView.ButtonView = false;
				}
				else
				{
					FNodesTree.PaletteItem = null;
					SetStatus(String.Empty);
					FPointerGroupView.ButtonView = true;
				}

				FNodesTree.Select();
			}
		}

		private bool FIsMultiDrop;
		[Browsable(false)]
		public bool IsMultiDrop
		{
			get { return FIsMultiDrop; }
		}

		public void PaletteItemDropped()
		{
			if (!IsMultiDrop)
				SelectPaletteItem(null, false);
		}

		private GroupBarItem FindPaletteBarItem(string AText)
		{
			foreach (GroupBarItem LItem in FPaletteGroupBar.GroupBarItems)
			{
				if (String.Compare(LItem.Text, AText, true) == 0)
					return LItem;
			}
			return null;
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if 
			(
				((AKey & Keys.Modifiers) == Keys.None) && 
				((AKey & Keys.KeyCode) == Keys.Escape) &&
				(FSelectedPaletteItem != null)
			)
			{
				SelectPaletteItem(null, false);
				return true;
			}
			else
				return base.ProcessDialogKey(AKey);
		}

		private void FPointerGroupView_GroupViewItemSelected(object sender, System.EventArgs e)
		{
			SelectPaletteItem(null, false);
		}

		private void CategoryGroupViewItemSelected(object ASender, EventArgs AArgs)
		{
			GroupView LView = (GroupView)ASender;
			SelectPaletteItem
			(
				(PaletteItem)LView.GroupViewItems[LView.SelectedItem], 
				Control.ModifierKeys == Keys.Shift
			);
		}

		#endregion

		#region IDesigner, New, Loading, Saving

		private string FDesignerID;
		[Browsable(false)]
		public string DesignerID
		{
			get { return FDesignerID; }
		}

		public void Open(DesignBuffer ABuffer)
		{
			FService.Open(ABuffer);
		}

		/// <remarks> 
		///		Note that this method should not be confused with Form.Close().  
		///		Be sure to deal with a compile-time instance of type IDesigner 
		///		to invoke this method. 
		///	</remarks>
		void Dataphor.Dataphoria.Designers.IDesigner.Show()
		{
			UpdateTitle();
			Dataphoria.AttachForm(this);

			// HACK: Don't know why, but for some reason, setting the MDIParent of this form collapses the nodes tree.
			FNodesTree.ExpandAll();
		}

		public virtual void New()
		{
			IHost LHost = FrontendSession.CreateHost();
			try
			{
				INode LNode = GetNewDesignNode();
				LHost.Children.Add(LNode);
				LHost.Open();
				InternalNew(LHost, true);
			}
			catch
			{
				LHost.Dispose();
				throw;
			}
		}

		public bool CloseSafely()
		{
			Close();
			return IsDisposed;
		}

		protected void InternalNew(IHost AHost, bool AOwner)
		{
			FService.SetBuffer(null);
			FService.SetModified(false);
			SetDesignHost(AHost, AOwner);
		}

		public void Save()
		{
			FService.Save();
		}

		public void SaveAsFile()
		{
			FService.SaveAsFile();
		}

		public void SaveAsDocument()
		{
			FService.SaveAsDocument();
		}

		protected virtual INode GetNewDesignNode()
		{
			IWindowsFormInterface LForm = (IWindowsFormInterface)FrontendSession.CreateForm();
			Dataphoria.AddDesignerForm(LForm, this);
			return LForm;
		}

		protected DocumentDesignBuffer BufferFromHost(IHost AHost)
		{
            DocumentExpression LExpression = Program.GetDocumentExpression(AHost.Document);
			DocumentDesignBuffer LBuffer = new DocumentDesignBuffer(Dataphoria, LExpression.DocumentArgs.LibraryName, LExpression.DocumentArgs.DocumentName);
			return LBuffer;
		}

		public void New(IHost AHost)
		{
			InternalNew(AHost, false);
		}

		public virtual void Open(IHost AHost)
		{
			DocumentDesignBuffer LBuffer = BufferFromHost(AHost);
			FService.ValidateBuffer(LBuffer);
			SetDesignHost(AHost, false);
			FService.SetBuffer(LBuffer);
			FService.SetModified(false);
		}

		protected IHost HostFromBuffer(DesignBuffer ABuffer)
		{
			return HostFromDocumentData(ABuffer.LoadData(), GetDocumentExpression(ABuffer));
		}

		protected IHost HostFromDocumentData(XmlDocument ADocumentData, string ADocumentExpression)
		{
			IHost LHost = FrontendSession.CreateHost();
			try
			{
				Frontend.Client.Deserializer LDeserializer = FrontendSession.CreateDeserializer();
				INode LInstance = GetNewDesignNode();
				try
				{
					LDeserializer.Deserialize(ADocumentData, LInstance);
					Dataphoria.Warnings.AppendErrors(this, LDeserializer.Errors, true);
					LHost.Children.Add(LInstance);
					LHost.Document = ADocumentExpression;
				}
				catch
				{
					LInstance.Dispose();
					throw;
				}
				LHost.Open();

				return LHost;
			}
			catch
			{
				LHost.Dispose();
				throw;
			}
		}

		protected IHost HostFromDocumentData(string ADocumentData, string ADocumentExpression)
		{
			IHost LHost = FrontendSession.CreateHost();
			try
			{
				Frontend.Client.Deserializer LDeserializer = FrontendSession.CreateDeserializer();
				INode LInstance = GetNewDesignNode();
				try
				{
					LDeserializer.Deserialize(ADocumentData, LInstance);
					Dataphoria.Warnings.AppendErrors(this, LDeserializer.Errors, true);
					LHost.Children.Add(LInstance);
					LHost.Document = ADocumentExpression;
				}
				catch
				{
					LInstance.Dispose();
					throw;
				}
				LHost.Open();

				return LHost;
			}
			catch
			{
				LHost.Dispose();
				throw;
			}
		}

		private void UpdateTitle()
		{
			Text = 
				String.Format
				(
					"{0} - {1}{2}",
					(FIsDesignHostOwner ? Strings.Designer : Strings.LiveDesigner ),
					FService.GetDescription(),
					(FService.IsModified ? "*" : String.Empty)
				);
		}
		
		protected string GetDocumentExpression(DesignBuffer ABuffer)
		{
			DocumentDesignBuffer LBuffer = ABuffer as DocumentDesignBuffer;
			if (LBuffer == null)
				return String.Empty;
			else
				return String.Format(".Frontend.Form('{0}', '{1}')", LBuffer.LibraryName, LBuffer.DocumentName);
		}

		protected void UpdateHostsDocument(DesignBuffer ABuffer)
		{
			DesignHost.Document = GetDocumentExpression(ABuffer);
		}

		#endregion

		#region DesignHost

		private IHost FDesignHost;
		[Browsable(false)]
		public IHost DesignHost
		{
			get	{ return FDesignHost; }
		}

		private bool FIsDesignHostOwner;
		[Browsable(false)]
		public bool IsDesignHostOwner
		{
			get { return FIsDesignHostOwner; }
		}

		private bool FDesignFormClosing;

		protected virtual void DetachDesignHost()
		{
			IWindowsFormInterface LForm = FDesignHost.Children[0] as IWindowsFormInterface;
			if (LForm != null)
				LForm.Form.Closing -= new CancelEventHandler(DesignFormClosing);
			FFormPanel.ClearHostedForm();
		}

		protected virtual void AttachDesignHost(IHost AHost)
		{
			IWindowsFormInterface LForm = AHost.Children[0] as IWindowsFormInterface;
			if (LForm != null)
			{
				LForm.Form.Closing += new CancelEventHandler(DesignFormClosing);
				FFormPanel.SetHostedForm(LForm, FIsDesignHostOwner);
			}
		}

		private void ClearNodesTree()
		{
			foreach (DesignerNode LRoot in FNodesTree.Nodes)
				LRoot.Dispose();
			FNodesTree.Nodes.Clear();
		}

		protected void SetDesignHost(IHost AHost, bool AOwner)
		{
			if (AHost != FDesignHost)
			{
				SuspendLayout();
				try
				{
					if (FDesignHost != null)
					{
						ActivateNode(null);
						SelectPaletteItem(null, false);

						DetachDesignHost();
						if (FIsDesignHostOwner && !FDesignFormClosing)
							((IWindowsFormInterface)FDesignHost.Children[0]).Close(CloseBehavior.RejectOrClose);
						FDesignHost = null;

						FNodesTree.BeginUpdate();
						try
						{
							ClearNodesTree();
						}
						finally
						{
							FNodesTree.EndUpdate();
						}
					}

					FDesignHost = AHost;
					FIsDesignHostOwner = AOwner;
					try
					{
						if (FDesignHost != null)
						{
							FNodesTree.BeginUpdate();
							try
							{
								if (FDesignHost.Children.Count != 0) 
								{
									FNodesTree.SelectedNode = FNodesTree.AddNode(FDesignHost.Children[0]);
									FNodesTree.SelectedNode.SetReadOnly(true, false);
									ActivateNode(FNodesTree.SelectedNode);	// the tree doesn't initially raise an ActiveChanged event
								}
							}
							finally
							{
								FNodesTree.EndUpdate();
							}

							AttachDesignHost(FDesignHost);
						}
					}
					catch
					{
						FDesignHost = null;
						ClearNodesTree();
						throw;
					}
				}
				finally
				{
					ResumeLayout(true);
				}
			}
		}

		protected void DesignFormClosing(object sender, System.ComponentModel.CancelEventArgs e) 
		{
			try
			{
				if (!e.Cancel)
				{
					FDesignFormClosing = true;
					try
					{
						Close();
						if (!IsDisposed)	// The abort of the close does not propigate, so we have to check (&%!@#*)
							throw new AbortException();
					}
					finally
					{
						FDesignFormClosing = false;
					}
				}
			}
			catch
			{
				e.Cancel = true;
				throw;
			}
		}

		#endregion

		#region Commands

		private void DeleteNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.Delete();
		}

		private void RenameNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.Rename();
		}

		private void PasteNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.PasteFromClipboard();
		}

		private void CopyNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.CopyToClipboard();
		}

		private void CutNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.CutToClipboard();
		}

		private void ShowPalette()
		{
			//FDockingManager.ActivateControl(FPalettePanel);
		}

		private void ShowProperties()
		{
			//FDockingManager.ActivateControl(FPropertyGrid);
		}

		private void ShowForm()
		{
			//FDockingManager.ActivateControl(FFormPanel);
		}

		/*private void FrameBarManagerItemClicked(object ASender, Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventArgs AArgs)
		{
			switch (AArgs.ClickedBarItem.ID)
			{
				case "Save" : Save(); break;
				case "SaveAsFile" : SaveAsFile(); break;
				case "SaveAsDocument" : SaveAsDocument(); break;
				case "Close" : Close(); break;
				case "Cut" : CutNode(); break;
				case "Copy" : CopyNode(); break;
				case "Paste" : PasteNode(); break;
				case "Delete" : DeleteNode(); break;
				case "Rename" : RenameNode(); break;
				case "ShowPalette" : ShowPalette(); break;
				case "ShowProperties" : ShowProperties(); break;
				case "ShowForm" : ShowForm(); break;
			}
		}*/

        private void FMainMenuStrip_ItemClicked(object ASender, EventArgs AArgs)
        {          
            if (ASender == FSaveToolStripButton) {
                Save();
            }
            else if (ASender == FSaveAsFileToolStripMenuItem || ASender == FSaveAsFileToolStripButton) 
            {
                SaveAsFile();
            }
            else if (ASender ==FSaveAsDocumentToolStripMenuItem || ASender == FSaveAsDocumentToolStripButton){
                SaveAsDocument();
            }
            else if (ASender == FCloseToolStripMenuItem) {
                Close();
            }
            else if (ASender == FCutToolStripMenuItem || ASender == FCutToolStripButton) {
                CutNode();
            }
            else if (ASender == FCopyToolStripMenuItem || ASender == FCopyToolStripButton)
            {
                CopyNode();
            }
            else if (ASender == FPasteToolStripMenuItem || ASender == FPasteToolStripButton)
            {
                PasteNode();
            }
            else if (ASender == FDeleteToolStripMenuItem || ASender == FDeleteToolStripButton) {
                DeleteNode();
            }
            else if (ASender == FRenameToolStripMenuItem || ASender == FRenameToolStripButton) {
                RenameNode();
            }
            else if (ASender == FPaletteToolStripMenuItem) {
                ShowPalette();
            }
            else if (ASender == FPropertiesToolStripMenuItem)
            {
                ShowProperties();
            }
            else if (ASender == FFormToolStripMenuItem) {
                ShowForm();
            }
        }

		#endregion

		#region IServiceProvider Members

		public new virtual object GetService(Type AServiceType)
		{
			if (AServiceType == typeof(IDesignService))
				return Service;
			else
			{
				object LResult = base.GetService(AServiceType);
				if (LResult != null)
					return LResult;
				else
					return Dataphoria.GetService(AServiceType);
			}
		}

		#endregion

		#region Help

		protected override void OnHelpRequested(HelpEventArgs AArgs)
		{
			base.OnHelpRequested(AArgs);
			string LKeyword;
			if (SelectedPaletteItem != null)
				LKeyword = SelectedPaletteItem.ClassName;
			else
			{
				if (ActiveControl.Name == "FNodesTree")
					LKeyword = FNodesTree.SelectedNode.Node.GetType().Name;
				else
					LKeyword = FPropertyGrid.SelectedObject.GetType().Name;
			}
			NodeTypeEntry LEntry = FrontendSession.NodeTypeTable[LKeyword];
			if (LEntry != null)
				LKeyword = LEntry.Namespace + "." + LKeyword;
			Dataphoria.InvokeHelp(LKeyword);
		}
		
		#endregion

		#region IContainer

		// IContainer is implemented because Sites are required to have containers

		public ComponentCollection Components
		{
			get
			{
				return new ComponentCollection(new IComponent[] {});
			}
		}

		public void Remove(IComponent component)
		{
			// nadda
		}

		public void Add(IComponent component, string name)
		{
			// nadda
		}

		void System.ComponentModel.IContainer.Add(IComponent component)
		{
			// nadda
		}

		#endregion

		#region IDisposable Members

		void System.IDisposable.Dispose()
		{
			// TODO:  Add FormDesigner.System.IDisposable.Dispose implementation
		}

		#endregion

        #region IChildFormWithToolBar Members

        public void MergeWith(ToolStrip AParentToolStrip)
        {
            ToolStripManager.Merge(this.FToolStrip,AParentToolStrip);            
        }

        #endregion

      
    }

	public class PaletteItem : GroupViewItem
	{
		private string FDescription = String.Empty;
		public string Description
		{
			get { return FDescription; }
			set { FDescription = value; }
		}

		private string FClassName;
		public string ClassName
		{
			get { return FClassName; }
			set { FClassName = value; }
		}
	}

	public class FormPanel : ContainerControl
	{
		public FormPanel()
		{
			BackColor = SystemColors.ControlDark;

			SuspendLayout();

			FHScrollBar = new HScrollBar();
			FHScrollBar.Dock = DockStyle.Bottom;
			FHScrollBar.SmallChange = 5;
			FHScrollBar.Scroll += new ScrollEventHandler(HScrollBarScroll);
			Controls.Add(FHScrollBar);

			FVScrollBar = new VScrollBar();
			FVScrollBar.Dock = DockStyle.Right;
			FVScrollBar.SmallChange = 5;
			FVScrollBar.Scroll += new ScrollEventHandler(VScrollBarScroll);
			Controls.Add(FVScrollBar);

			ResumeLayout(false);
		}

		private HScrollBar FHScrollBar;
		private VScrollBar FVScrollBar;

		private Point FOriginalLocation;
		private bool FIsOwner;
		
		private Form FHostedForm;
		public Form HostedForm
		{
			get { return FHostedForm; }
		}

		public void SetHostedForm(IWindowsFormInterface AForm, bool AIsOwner)
		{
			InternalClear();
			FHostedForm = (Form)AForm.Form;
			if (FHostedForm != null)
			{
				FIsOwner = AIsOwner;
				if (!AIsOwner)
					FOriginalLocation = FHostedForm.Location;
				SuspendLayout();
				try
				{
					AForm.BeginUpdate();
					try
					{
						FHostedForm.TopLevel = false;
						Controls.Add(FHostedForm);
						FHostedForm.SendToBack();
						if (AIsOwner)
							AForm.Show();
					}
					finally
					{
						AForm.EndUpdate(false);
					}
				}
				finally
				{
					ResumeLayout(true);
				}
			}
		}

		public void ClearHostedForm()
		{
			InternalClear();
			FHostedForm = null;
		}

		private void InternalClear()
		{
			if (FHostedForm != null)
			{
				FHostedForm.Hide();
				Controls.Remove(FHostedForm);
				if (!FIsOwner)
				{
					FHostedForm.TopLevel = true;
					FHostedForm.Location = FOriginalLocation;
					FHostedForm.Show();
					FHostedForm.BringToFront();
				}
			}
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			if ((AArgs.AffectedControl == null) || (AArgs.AffectedControl == FHostedForm) || (AArgs.AffectedControl == this))
			{
				// Prepare the "adjusted" clients size
				Size LAdjustedClientSize = ClientSize;
				LAdjustedClientSize.Width -= FVScrollBar.Width;
				LAdjustedClientSize.Height -= FHScrollBar.Height;
				// Ensure a minimum client size to avoid errors setting scrollbar limits etc.
				if (LAdjustedClientSize.Width <= 0)
					LAdjustedClientSize.Width = 1;
				if (LAdjustedClientSize.Height <= 0)
					LAdjustedClientSize.Height = 1;
				
				if (FHostedForm != null)
				{
					int LMaxValue;

					LMaxValue = Math.Max(0, FHostedForm.Width - LAdjustedClientSize.Width);
					if (FHScrollBar.Value > LMaxValue)
						FHScrollBar.Value = LMaxValue;
					FHScrollBar.Maximum = Math.Max(0, FHostedForm.Width);
					FHScrollBar.Visible = (FHScrollBar.Maximum - LAdjustedClientSize.Width) > 0;
					if (FHScrollBar.Visible)
						FHScrollBar.LargeChange = LAdjustedClientSize.Width;

					LMaxValue = Math.Max(0, FHostedForm.Height - LAdjustedClientSize.Height);
					if (FVScrollBar.Value > LMaxValue)
						FVScrollBar.Value = LMaxValue;
					FVScrollBar.Maximum = Math.Max(0, FHostedForm.Height);
					FVScrollBar.Visible = (FVScrollBar.Maximum - LAdjustedClientSize.Height) > 0;
					if (FVScrollBar.Visible)
						FVScrollBar.LargeChange = LAdjustedClientSize.Height;
					
					FHostedForm.Location = new Point(-FHScrollBar.Value, -FVScrollBar.Value);
					FHostedForm.SendToBack();
				}
				else
				{
					FHScrollBar.Visible = false;
					FVScrollBar.Visible = false;
				}
			}
			base.OnLayout(AArgs);
		}

		protected override void OnControlAdded(ControlEventArgs AArgs)
		{
			base.OnControlAdded(AArgs);
			AArgs.Control.Move += new EventHandler(ControlMove);
		}

		protected override void OnControlRemoved(ControlEventArgs AArgs)
		{
			AArgs.Control.Move -= new EventHandler(ControlMove);
			base.OnControlRemoved(AArgs);
		}

		private void ControlMove(object ASender, EventArgs AArgs)
		{
			Control LControl = (Control)ASender;
			if ((LControl.IsHandleCreated) && (LControl.Location != Point.Empty))
				PerformLayout();
		}

		private void HScrollBarScroll(object ASender, ScrollEventArgs AArgs)
		{
			PerformLayout(FHostedForm, "Location");
		}

		private void VScrollBarScroll(object ASender, ScrollEventArgs AArgs)
		{
			PerformLayout(FHostedForm, "Location");
		}

	}
}
