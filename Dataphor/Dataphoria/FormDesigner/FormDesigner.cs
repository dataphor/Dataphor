/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt

	Assumption: this designer assumes that its lifetime is for a single document.  It will 
	 currently not close any existing document before operations such a New(), Open().  This
	 behavior is okay for now because Dataphoria does not ask designers to change documents.
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using WeifenLuo.WinFormsUI.Docking;
using Cursors=System.Windows.Forms.Cursors;
using Image=System.Drawing.Image;
using Session=Alphora.Dataphor.Frontend.Client.Windows.Session;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
    // Don't put any definitions above the FormDesiger class

    public partial class FormDesigner : BaseForm, ILiveDesigner, IErrorSource, IServiceProvider, IContainer, IChildFormWithToolBar
    {
        protected DockContent FDockContentFormPanel;
        protected DockContent FDockContentNodesTree;
        protected DockContent FDockContentPalettePanel;
        protected DockContent FDockContentPropertyGrid;
        private ToolBox.ToolBox FPalettePanel;

        public FormDesigner() // dummy constructor for MDI menu merging?
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
            ADataphoria.OnFormDesignerLibrariesChanged += FormDesignerLibrariesChanged;
        }

        protected override void Dispose(bool ADisposed)
        {
            if (!IsDisposed && (Dataphoria != null))
            {
                try
                {
                    SetDesignHost(null, true);
                }
                finally
                {
                    try
                    {
                        FPalettePanel.ClearPalette();
                    }
                    finally
                    {
                        try
                        {
                            if (FFrontendSession != null)
                            {
                                FFrontendSession.Dispose();
                                FFrontendSession = null;
                            }
                        }
                        finally
                        {
                            try
                            {
                                Dataphoria.OnFormDesignerLibrariesChanged -= new EventHandler(FormDesignerLibrariesChanged);
                            }
                            finally
                            {
                                try
                                {
                                    if (components != null)
                                        components.Dispose();
                                }
                                finally
                                {
                                    base.Dispose(ADisposed);
                                }
                            }
                        }
                    }
                }
            }
        }


        // Dataphoria

        [Browsable(false)]
        public IDataphoria Dataphoria
        {
            get { return (FService == null ? null : FService.Dataphoria); }
        }

        #region IChildFormWithToolBar Members

        public void MergeToolbarWith(ToolStrip AParentToolStrip)
        {
            ToolStripManager.Merge(FToolStrip, AParentToolStrip);
        }

        #endregion

        #region ILiveDesigner Members

        void IDisposable.Dispose()
        {
            // TODO:  Add FormDesigner.System.IDisposable.Dispose implementation
        }

        #endregion

        #region IServiceProvider Members

        public new virtual object GetService(Type AServiceType)
        {
            if (AServiceType == typeof (IDesignService))
                return Service;
            object LResult = base.GetService(AServiceType);
            if (LResult != null)
                return LResult;
            return Dataphoria.GetService(AServiceType);
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
            get { return new ComponentCollection(new IComponent[] {}); }
        }

        public void Remove(IComponent AComponent)
        {
            // nadda
        }

        public void Add(IComponent AComponent, string AName)
        {
            // nadda
        }

        void IContainer.Add(IComponent AComponent)
        {
            // nadda
        }

        #endregion

        private void InitializeDocking()
        {
            // 
            // FPaletteGroupBar
            // 
            /*FPaletteGroupBar = new GroupBar();
            FPaletteGroupBar.AllowDrop = true;
            FPaletteGroupBar.BackColor = SystemColors.Control;
            FPaletteGroupBar.BorderStyle = BorderStyle.FixedSingle;
            FPaletteGroupBar.Dock = DockStyle.Fill;
            FPaletteGroupBar.Location = new Point(0, 24);
            FPaletteGroupBar.Name = "FPaletteGroupBar";
            FPaletteGroupBar.SelectedItem = 0;
            FPaletteGroupBar.Size = new Size(163, 163);
            FPaletteGroupBar.TabIndex = 1;*/
            // 
            // FPointerGroupView
            // 
            /*FPointerGroupView = new GroupView
                                    {
                                        BorderStyle = BorderStyle.None,
                                        ButtonView = true,
                                        Dock = DockStyle.Top
                                    };
            FPointerGroupView.GroupViewItems.AddRange(new[]
                                                          {
                                                              new GroupViewItem("Pointer", 0)
                                                          });
            FPointerGroupView.IntegratedScrolling = true;
            FPointerGroupView.ItemYSpacing = 2;
            FPointerGroupView.LargeImageList = null;
            FPointerGroupView.Location = new Point(0, 0);
            FPointerGroupView.Name = "FPointerGroupView";
            FPointerGroupView.SelectedItem = 0;
            FPointerGroupView.Size = new Size(163, 24);
            FPointerGroupView.SmallImageList = FPointerImageList;
            FPointerGroupView.SmallImageView = true;
            FPointerGroupView.TabIndex = 0;
            FPointerGroupView.Text = "groupView2";
            FPointerGroupView.GroupViewItemSelected += FPointerGroupView_GroupViewItemSelected;
            */

            // 
            // FNodesTree
            // 
            FNodesTree = new DesignerTree.DesignerTree();
            FNodesTree.AllowDrop = true;
            FNodesTree.BorderStyle = BorderStyle.None;
            FNodesTree.CausesValidation = false;
            FNodesTree.Dock = DockStyle.Fill;
            FNodesTree.HideSelection = false;
            FNodesTree.ImageList = FNodesImageList;
            FNodesTree.Location = new Point(0, 0);
            FNodesTree.Name = "FNodesTree";
            FNodesTree.ShowRootLines = false;
            FNodesTree.Size = new Size(283, 209);
            FNodesTree.TabIndex = 0;
            FNodesTree.AfterSelect += FNodesTree_AfterSelect;
            FNodesTree.Dock = DockStyle.Fill;
            
            
            // 
            // FPalettePanel
            // 
            FPalettePanel = new ToolBox.ToolBox();
            //FPalettePanel.Controls.Add(FPaletteGroupBar);
            //FPalettePanel.Controls.Add(FPointerGroupView);
            //this.FDockingManager.SetEnableDocking(this.FPalettePanel, true);
            FPalettePanel.NodesTree = this.FNodesTree;
            FPalettePanel.Location = new Point(1, 21);
            FPalettePanel.Name = "FPalettePanel";
            FPalettePanel.Size = new Size(163, 187);
            FPalettePanel.TabIndex = 1;
            FPalettePanel.Dock = DockStyle.Fill;
            FPalettePanel.StatusChanged += FPalettePanel_StatusChanged;

            FDockContentPalettePanel = new DockContent();
            FDockContentPalettePanel.Controls.Add(FPalettePanel);
            FDockContentPalettePanel.TabText = "Forms Palette";
            FDockContentPalettePanel.Text = "Palette";
            FDockContentPalettePanel.ShowHint = DockState.DockLeft;
            FDockContentPalettePanel.Show(FDockPanel);

            // 
            // FFormPanel
            // 
            FFormPanel = new FormPanel();
            FFormPanel.BackColor = SystemColors.ControlDark;
            //this.FDockingManager.SetEnableDocking(this.FFormPanel, true);
            FFormPanel.Location = new Point(1, 21);
            FFormPanel.Name = "FFormPanel";
            FFormPanel.Size = new Size(685, 283);
            FFormPanel.TabIndex = 3;
            FFormPanel.Dock = DockStyle.Fill;

            FDockContentFormPanel = new DockContent();
            FDockContentFormPanel.Controls.Add(FFormPanel);
            FDockContentFormPanel.ShowHint = DockState.Document;
            FDockContentFormPanel.Show(FDockPanel);

           


            FDockContentNodesTree = new DockContent();
            FDockContentNodesTree.Controls.Add(FNodesTree);
            FDockContentNodesTree.TabText = "Forms Nodes Tree";
            FDockContentNodesTree.Text = "Nodes Tree";
            FDockContentNodesTree.ShowHint = DockState.DockRight;
            FDockContentNodesTree.Show(FDockPanel);


            FPropertyGrid = new PropertyGrid();
            // 
            // FPropertyGrid
            // 
            FPropertyGrid.BackColor = SystemColors.Control;
            FPropertyGrid.CausesValidation = false;
            FPropertyGrid.CommandsVisibleIfAvailable = true;
            FPropertyGrid.Cursor = Cursors.HSplit;
            //this.FDockingManager.SetEnableDocking(this.FPropertyGrid, true);
            FPropertyGrid.LargeButtons = false;
            FPropertyGrid.LineColor = SystemColors.ScrollBar;
            FPropertyGrid.Location = new Point(1, 21);
            FPropertyGrid.Name = "FPropertyGrid";
            FPropertyGrid.PropertySort = PropertySort.Alphabetical;
            FPropertyGrid.Size = new Size(229, 187);
            FPropertyGrid.TabIndex = 2;
            FPropertyGrid.Text = "Properties of the Currently Selected Node";
            FPropertyGrid.ToolbarVisible = false;
            FPropertyGrid.ViewBackColor = SystemColors.Window;
            FPropertyGrid.ViewForeColor = SystemColors.WindowText;
            FPropertyGrid.PropertyValueChanged +=NodePropertyGrid_PropertyValueChanged;
            FPropertyGrid.Dock = DockStyle.Fill;

            FDockContentPropertyGrid = new DockContent();
            FDockContentPropertyGrid.Controls.Add(FPropertyGrid);
            FDockContentPropertyGrid.TabText = "Forms Properties Grid";
            FDockContentPropertyGrid.Text = "Properties Grid";
            FDockContentPropertyGrid.ShowHint = DockState.DockRight;
            FDockContentPropertyGrid.Show(FDockPanel);
        }

        private void FPalettePanel_StatusChanged(object ASender, StatusEventArgs AArgs)
        {
            this.SetStatus(AArgs.Description);
        }

        protected override void OnClosing(CancelEventArgs AArgs)
        {
            base.OnClosing(AArgs);
            try
            {
                FService.CheckModified();
                if (FIsDesignHostOwner && (!FrontendSession.CloseAllForms(FDesignHost, CloseBehavior.AcceptOrClose)))
                    // if we are hosting, close the child forms
                    throw new AbortException();
            }
            catch
            {
                AArgs.Cancel = true;
                throw;
            }
        }

        #region FrontendSession

        private Session FFrontendSession;

        [Browsable(false)]
        public Session FrontendSession
        {
            get { return FFrontendSession; }
        }

        /// <summary> Prepares (or re-prepares) the frontend session and the component palette </summary>
        private void PrepareSession()
        {
            if (FFrontendSession == null)
            {
                FFrontendSession = Dataphoria.GetLiveDesignableFrontendSession();
                FPalettePanel.FrontendSession = this.FFrontendSession;
            }
            FFrontendSession.SetFormDesigner();
            FPalettePanel.ClearPalette();
            FPalettePanel.LoadPalette();
        }

        private void FormDesignerLibrariesChanged(object ASender, EventArgs AArgs)
        {
            PrepareSession();
        }

        #endregion

        #region Service

        private IDesignService FService;

        [Browsable(false)]
        public IDesignService Service
        {
            get { return FService; }
        }

        public void InitializeService(IDataphoria ADataphoria)
        {
            FService = new DesignService(ADataphoria, this);
            FService.OnModifiedChanged += NameOrModifiedChanged;
            FService.OnNameChanged += NameOrModifiedChanged;
            FService.OnRequestLoad += RequestLoad;
            FService.OnRequestSave += RequestSave;
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
            Serializer LSerializer = FrontendSession.CreateSerializer();
            var LDocument = new XmlDocument();
            LSerializer.Serialize(LDocument, FDesignHost.Children[0]);
            Dataphoria.Warnings.AppendErrors(this, LSerializer.Errors, true);

            var LStream = new MemoryStream();
            var LXmlTextWriter = new XmlTextWriter(LStream, Encoding.UTF8);
            LXmlTextWriter.Formatting = Formatting.Indented;
            LDocument.Save(LXmlTextWriter);
            byte[] LWriterString = LStream.ToArray();
            ABuffer.SaveData(Encoding.UTF8.GetString(LWriterString, 0, LWriterString.Length));

            UpdateHostsDocument(ABuffer);
        }

        #endregion

        #region Tree Nodes

        private void FNodesTree_AfterSelect(object ASender, TreeViewEventArgs AArgs)
        {
            ActivateNode((DesignerTree.DesignerNode)AArgs.Node);
        }

        public void ActivateNode(DesignerTree.DesignerNode ANode)
        {
            if ((FPropertyGrid.SelectedObject != null) && (FPropertyGrid.SelectedObject is IDisposableNotify))
                ((IDisposableNotify) FPropertyGrid.SelectedObject).Disposed -= SelectedNodeDisposed;

            bool LEditsAllowed;
            if (ANode == null)
            {
                FPropertyGrid.SelectedObject = null;
                LEditsAllowed = false;
            }
            else
            {
                FPropertyGrid.SelectedObject = ANode.Node;
                ANode.Node.Disposed += SelectedNodeDisposed;
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

        private void NodePropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
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
            Focus();
        }

        #endregion

        #region Palette

        private Hashtable FImageIndex = new Hashtable();
        private bool FIsMultiDrop;
        private PaletteItem FSelectedPaletteItem;

        [Browsable(false)]
        public PaletteItem SelectedPaletteItem
        {
            get { return FSelectedPaletteItem; }
        }

        [Browsable(false)]
        public bool IsMultiDrop
        {
            get { return FIsMultiDrop; }
        }

       

        private bool IsTypeListed(Type AType)
        {
            var LListIn =
                (ListInDesignerAttribute) ReflectionUtility.GetAttribute(AType, typeof (ListInDesignerAttribute));
            if (LListIn != null)
                return LListIn.IsListed;
            return true;
        }

        private string GetDescription(Type AType)
        {
            var LDescription =
                (DescriptionAttribute) ReflectionUtility.GetAttribute(AType, typeof (DescriptionAttribute));
            if (LDescription != null)
                return LDescription.Description;
            return String.Empty;
        }

        private string GetDesignerCategory(Type AType)
        {
            var LCategory =
                (DesignerCategoryAttribute) ReflectionUtility.GetAttribute(AType, typeof (DesignerCategoryAttribute));
            if (LCategory != null)
                return LCategory.Category;
            return Strings.UnspecifiedCategory;
        }

        private Image LoadImage(string AImageExpression)
        {
            try
            {
                using (DataValue LImageData = FrontendSession.Pipe.RequestDocument(AImageExpression))
                {
                    var LStreamCopy = new MemoryStream();
                    Stream LStream = LImageData.OpenStream();
                    try
                    {
                        StreamUtility.CopyStream(LStream, LStreamCopy);
                    }
                    finally
                    {
                        LStream.Close();
                    }
                    return Image.FromStream(LStreamCopy);
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
            var LImageAttribute =
                (DesignerImageAttribute) ReflectionUtility.GetAttribute(AType, typeof (DesignerImageAttribute));
            if (LImageAttribute != null)
            {
                object LIndexResult = FImageIndex[LImageAttribute.ImageExpression];
                if (LIndexResult == null)
                {
                    Image LImage = LoadImage(LImageAttribute.ImageExpression);
                    if (LImage != null)
                    {
                        if (LImage is Bitmap)
                            ((Bitmap) LImage).MakeTransparent();
                        FNodesImageList.Images.Add(LImage);
                        int LIndex = FNodesImageList.Images.Count - 1;
                        FImageIndex.Add(LImageAttribute.ImageExpression, LIndex);
                        return LIndex;
                    }
                    FImageIndex.Add(LImageAttribute.ImageExpression, 0);
                }
                else
                    return (int) LIndexResult;
            }
            return 0; // Zero is the reserved index for the default image
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
        void IDesigner.Show()
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

        public virtual void Open(IHost AHost)
        {
            DocumentDesignBuffer LBuffer = BufferFromHost(AHost);
            FService.ValidateBuffer(LBuffer);
            SetDesignHost(AHost, false);
            FService.SetBuffer(LBuffer);
            FService.SetModified(false);
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
            var LForm = (IWindowsFormInterface) FrontendSession.CreateForm();
            Dataphoria.AddDesignerForm(LForm, this);
            return LForm;
        }

        protected DocumentDesignBuffer BufferFromHost(IHost AHost)
        {
            DocumentExpression LExpression = Program.GetDocumentExpression(AHost.Document);
            var LBuffer = new DocumentDesignBuffer(Dataphoria, LExpression.DocumentArgs.LibraryName,
                                                   LExpression.DocumentArgs.DocumentName);
            return LBuffer;
        }

        public void New(IHost AHost)
        {
            InternalNew(AHost, false);
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
                Deserializer LDeserializer = FrontendSession.CreateDeserializer();
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
                Deserializer LDeserializer = FrontendSession.CreateDeserializer();
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
                    (FIsDesignHostOwner ? Strings.Designer : Strings.LiveDesigner),
                    FService.GetDescription(),
                    (FService.IsModified ? "*" : String.Empty)
                    );
        }

        protected string GetDocumentExpression(DesignBuffer ABuffer)
        {
            var LBuffer = ABuffer as DocumentDesignBuffer;
            if (LBuffer == null)
                return String.Empty;
            return String.Format(".Frontend.Form('{0}', '{1}')", LBuffer.LibraryName, LBuffer.DocumentName);
        }

        protected void UpdateHostsDocument(DesignBuffer ABuffer)
        {
            DesignHost.Document = GetDocumentExpression(ABuffer);
        }

        #endregion

        #region DesignHost

        private bool FDesignFormClosing;
        private IHost FDesignHost;

        private bool FIsDesignHostOwner;

        [Browsable(false)]
        public IHost DesignHost
        {
            get { return FDesignHost; }
        }

        [Browsable(false)]
        public bool IsDesignHostOwner
        {
            get { return FIsDesignHostOwner; }
        }

        protected virtual void DetachDesignHost()
        {
            var LForm = FDesignHost.Children[0] as IWindowsFormInterface;
            if (LForm != null)
                LForm.Form.Closing -= DesignFormClosing;
            FFormPanel.ClearHostedForm();
        }

        protected virtual void AttachDesignHost(IHost AHost)
        {
            var LForm = AHost.Children[0] as IWindowsFormInterface;
            if (LForm != null)
            {
                LForm.Form.Closing += DesignFormClosing;
                FFormPanel.SetHostedForm(LForm, FIsDesignHostOwner);
            }
        }

        private void ClearNodesTree()
        {
            foreach (DesignerTree.DesignerNode LRoot in FNodesTree.Nodes)
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
                        FPalettePanel.SelectPaletteItem(null, false);

                        DetachDesignHost();
                        if (FIsDesignHostOwner && !FDesignFormClosing)
                            ((IWindowsFormInterface) FDesignHost.Children[0]).Close(CloseBehavior.RejectOrClose);
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
                                    ActivateNode(FNodesTree.SelectedNode);
                                        // the tree doesn't initially raise an ActiveChanged event
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

        protected void DesignFormClosing(object ASender, CancelEventArgs AArgs)
        {
            try
            {
                if (!AArgs.Cancel)
                {
                    FDesignFormClosing = true;
                    try
                    {
                        Close();
                        if (!IsDisposed) // The abort of the close does not propigate, so we have to check (&%!@#*)
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
                AArgs.Cancel = true;
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


        private void FMainMenuStrip_ItemClicked(object ASender, EventArgs AArgs)
        {
            if (ASender == FSaveToolStripButton)
            {
                Save();
            }
            else if (ASender == FSaveAsFileToolStripMenuItem || ASender == FSaveAsFileToolStripButton)
            {
                SaveAsFile();
            }
            else if (ASender == FSaveAsDocumentToolStripMenuItem || ASender == FSaveAsDocumentToolStripButton)
            {
                SaveAsDocument();
            }
            else if (ASender == FCloseToolStripMenuItem)
            {
                Close();
            }
            else if (ASender == FCutToolStripMenuItem || ASender == FCutToolStripButton)
            {
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
            else if (ASender == FDeleteToolStripMenuItem || ASender == FDeleteToolStripButton)
            {
                DeleteNode();
            }
            else if (ASender == FRenameToolStripMenuItem || ASender == FRenameToolStripButton)
            {
                RenameNode();
            }
            else if (ASender == FPaletteToolStripMenuItem)
            {
                ShowPalette();
            }
            else if (ASender == FPropertiesToolStripMenuItem)
            {
                ShowProperties();
            }
            else if (ASender == FFormToolStripMenuItem)
            {
                ShowForm();
            }
        }

        #endregion

        public void PaletteItemDropped()
        {
            FPalettePanel.PaletteItemDropped();
        }
    }
}