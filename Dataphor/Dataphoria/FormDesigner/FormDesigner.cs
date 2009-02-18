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

    public class FormDesigner : BaseForm, ILiveDesigner, IErrorSource, IServiceProvider, IContainer, IChildFormWithToolBar
	{
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.ImageList ToolBarImageList;
		private System.Windows.Forms.Panel FPalettePanel;
		private Syncfusion.Windows.Forms.Tools.GroupBar FPaletteGroupBar;
		private Syncfusion.Windows.Forms.Tools.GroupView FPointerGroupView;
		private System.Windows.Forms.ImageList FPointerImageList;
		private System.Windows.Forms.PropertyGrid FPropertyGrid;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ChildFrameBarManager FFrameBarManager;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAsFile;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAsDocumentMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCloseMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FFileMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FMainMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FFileBar;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FEditMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCutMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCopyMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FPasteMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FDeleteMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FRenameMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FViewMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FShowPaletteMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FShowPropertiesMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FShowFormMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FEditBar;
		private Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenu FNodesPopupMenu;
		protected Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree FNodesTree;
		private Alphora.Dataphor.Dataphoria.FormDesigner.FormPanel FFormPanel;
        private WeifenLuo.WinFormsUI.Docking.DockPanel FDockPanel;
		//protected Syncfusion.Windows.Forms.Tools.DockingManager FDockingManager;
		//private Syncfusion.Windows.Forms.Tools.DockingClientPanel FDockingPanel;
		private System.Windows.Forms.ImageList FNodesImageList;


        protected DockContent FDockContentPalettePanel;
        protected DockContent FDockContentFormPanel;
        protected DockContent FDockContentNodesTree;
        private MenuStrip FMenuStrip;
        private ToolStripMenuItem FFileToolStripMenuItem;
        private ToolStripMenuItem FSaveAsFileToolStripMenuItem;
        private ToolStripMenuItem FSaveAsDocumentToolStripMenuItem;
        private ToolStripMenuItem FCloseToolStripMenuItem;
        private ToolStripMenuItem FEditToolStripMenuItem;
        private ToolStripMenuItem FCutToolStripMenuItem;
        private ToolStripMenuItem FCopyToolStripMenuItem;
        private ToolStripMenuItem FPasteToolStripMenuItem;
        private ToolStripMenuItem FDeleteToolStripMenuItem;
        private ToolStripMenuItem FRenameToolStripMenuItem;
        private ToolStripMenuItem FViewToolStripMenuItem;
        private ToolStripMenuItem FPaletteToolStripMenuItem;
        private ToolStripMenuItem FPropertiesToolStripMenuItem;
        private ToolStripMenuItem FFormToolStripMenuItem;
        private ToolStrip FToolStrip;
        private ToolStripButton FSaveToolStripButton;
        private ToolStripButton FSaveAsFileToolStripButton;
        private ToolStripButton FSaveAsDocumentToolStripButton;
        private ToolStripSeparator FToolStripSeparator;
        private ToolStripButton FCutToolStripButton;
        private ToolStripButton FCopyToolStripButton2;
        private ToolStripButton FPasteToolStripButton3;
        private ToolStripButton FDeleteToolStripButton4;
        private ToolStripButton FRenameToolStripButton1;
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
						ClearPalette();
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

		#region Windows Form Designer generated code
		/// <summary>
		///		Required method for Designer support - do not modify
		///		the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDesigner));
            this.FPointerImageList = new System.Windows.Forms.ImageList(this.components);
            this.FNodesImageList = new System.Windows.Forms.ImageList(this.components);
            this.ToolBarImageList = new System.Windows.Forms.ImageList(this.components);
            this.FFrameBarManager = new Syncfusion.Windows.Forms.Tools.XPMenus.ChildFrameBarManager(this);
            this.FMainMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "MainMenu");
            this.FFileMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FSaveMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FSaveAsFile = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FSaveAsDocumentMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FCloseMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FEditMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FCutMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FCopyMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FPasteMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FDeleteMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FRenameMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FViewMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FShowPaletteMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FShowPropertiesMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FShowFormMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FFileBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "FileBar");
            this.FEditBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "EditBar");
            this.FNodesPopupMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenu(this.components);
            this.FDockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.FMenuStrip = new System.Windows.Forms.MenuStrip();
            this.FFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FSaveAsFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FSaveAsDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FCloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FEditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FCutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FCopyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FPasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDeleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FRenameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FPaletteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FFormToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FToolStrip = new System.Windows.Forms.ToolStrip();
            this.FSaveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FSaveAsFileToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FSaveAsDocumentToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.FCutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FCopyToolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.FPasteToolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.FDeleteToolStripButton4 = new System.Windows.Forms.ToolStripButton();
            this.FRenameToolStripButton1 = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).BeginInit();
            this.FMenuStrip.SuspendLayout();
            this.FToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // FPointerImageList
            // 
            this.FPointerImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FPointerImageList.ImageStream")));
            this.FPointerImageList.TransparentColor = System.Drawing.Color.Lime;
            this.FPointerImageList.Images.SetKeyName(0, "");
            // 
            // FNodesImageList
            // 
            this.FNodesImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FNodesImageList.ImageStream")));
            this.FNodesImageList.TransparentColor = System.Drawing.Color.LimeGreen;
            this.FNodesImageList.Images.SetKeyName(0, "");
            // 
            // ToolBarImageList
            // 
            this.ToolBarImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ToolBarImageList.ImageStream")));
            this.ToolBarImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.ToolBarImageList.Images.SetKeyName(0, "");
            this.ToolBarImageList.Images.SetKeyName(1, "");
            this.ToolBarImageList.Images.SetKeyName(2, "");
            this.ToolBarImageList.Images.SetKeyName(3, "");
            this.ToolBarImageList.Images.SetKeyName(4, "");
            this.ToolBarImageList.Images.SetKeyName(5, "");
            this.ToolBarImageList.Images.SetKeyName(6, "");
            this.ToolBarImageList.Images.SetKeyName(7, "");
            this.ToolBarImageList.Images.SetKeyName(8, "");
            this.ToolBarImageList.Images.SetKeyName(9, "");
            this.ToolBarImageList.Images.SetKeyName(10, "");
            this.ToolBarImageList.Images.SetKeyName(11, "");
            // 
            // FFrameBarManager
            // 
            this.FFrameBarManager.BarPositionInfo = ((System.IO.MemoryStream)(resources.GetObject("FFrameBarManager.BarPositionInfo")));
            this.FFrameBarManager.Bars.Add(this.FMainMenu);
            this.FFrameBarManager.Bars.Add(this.FFileBar);
            this.FFrameBarManager.Bars.Add(this.FEditBar);
            this.FFrameBarManager.Categories.Add("File");
            this.FFrameBarManager.Categories.Add("Edit");
            this.FFrameBarManager.Categories.Add("View");
            this.FFrameBarManager.Categories.Add("Status");
            this.FFrameBarManager.CurrentBaseFormType = "Alphora.Dataphor.Dataphoria.BaseForm";
            this.FFrameBarManager.Form = this;
            this.FFrameBarManager.FormName = "Form Designer";
            this.FFrameBarManager.ImageList = this.ToolBarImageList;
            this.FFrameBarManager.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FSaveAsFile,
            this.FSaveAsDocumentMenuItem,
            this.FCloseMenuItem,
            this.FFileMenu,
            this.FSaveMenuItem,
            this.FEditMenuItem,
            this.FCutMenuItem,
            this.FCopyMenuItem,
            this.FPasteMenuItem,
            this.FDeleteMenuItem,
            this.FRenameMenuItem,
            this.FViewMenu,
            this.FShowPaletteMenuItem,
            this.FShowPropertiesMenuItem,
            this.FShowFormMenuItem});
            this.FFrameBarManager.LargeImageList = null;
            this.FFrameBarManager.UsePartialMenus = false;
            this.FFrameBarManager.ItemClicked += new Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventHandler(this.FrameBarManagerItemClicked);
            // 
            // FMainMenu
            // 
            this.FMainMenu.BarName = "MainMenu";
            this.FMainMenu.BarStyle = ((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle)(((((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.AllowQuickCustomizing | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.IsMainMenu)
                        | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.Visible)
                        | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.UseWholeRow)
                        | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.DrawDragBorder)));
            this.FMainMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FFileMenu,
            this.FEditMenuItem,
            this.FViewMenu});
            this.FMainMenu.Manager = this.FFrameBarManager;
            // 
            // FFileMenu
            // 
            this.FFileMenu.CategoryIndex = 0;
            this.FFileMenu.ID = "File";
            this.FFileMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FSaveMenuItem,
            this.FSaveAsFile,
            this.FSaveAsDocumentMenuItem,
            this.FCloseMenuItem});
            this.FFileMenu.MergeOrder = 1;
            this.FFileMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FFileMenu.Text = "&File";
            // 
            // FSaveMenuItem
            // 
            this.FSaveMenuItem.CategoryIndex = 0;
            this.FSaveMenuItem.ID = "Save";
            this.FSaveMenuItem.ImageIndex = 9;
            this.FSaveMenuItem.MergeOrder = 20;
            this.FSaveMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
            this.FSaveMenuItem.Text = "&Save";
            // 
            // FSaveAsFile
            // 
            this.FSaveAsFile.CategoryIndex = 0;
            this.FSaveAsFile.ID = "SaveAsFile";
            this.FSaveAsFile.ImageIndex = 11;
            this.FSaveAsFile.MergeOrder = 20;
            this.FSaveAsFile.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftF;
            this.FSaveAsFile.Text = "Save As &File...";
            // 
            // FSaveAsDocumentMenuItem
            // 
            this.FSaveAsDocumentMenuItem.CategoryIndex = 0;
            this.FSaveAsDocumentMenuItem.ID = "SaveAsDocument";
            this.FSaveAsDocumentMenuItem.ImageIndex = 10;
            this.FSaveAsDocumentMenuItem.MergeOrder = 20;
            this.FSaveAsDocumentMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftD;
            this.FSaveAsDocumentMenuItem.Text = "Save As &Document...";
            // 
            // FCloseMenuItem
            // 
            this.FCloseMenuItem.CategoryIndex = 0;
            this.FCloseMenuItem.ID = "Close";
            this.FCloseMenuItem.ImageIndex = 0;
            this.FCloseMenuItem.MergeOrder = 20;
            this.FCloseMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF4;
            this.FCloseMenuItem.Text = "&Close";
            // 
            // FEditMenuItem
            // 
            this.FEditMenuItem.CategoryIndex = 1;
            this.FEditMenuItem.ID = "Edit";
            this.FEditMenuItem.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FCutMenuItem,
            this.FCopyMenuItem,
            this.FPasteMenuItem,
            this.FDeleteMenuItem,
            this.FRenameMenuItem});
            this.FEditMenuItem.MergeOrder = 5;
            this.FEditMenuItem.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FEditMenuItem.Text = "&Edit";
            // 
            // FCutMenuItem
            // 
            this.FCutMenuItem.CategoryIndex = 1;
            this.FCutMenuItem.ID = "Cut";
            this.FCutMenuItem.ImageIndex = 6;
            this.FCutMenuItem.MergeOrder = 10;
            this.FCutMenuItem.ShortcutText = "Ctrl+X";
            this.FCutMenuItem.Text = "C&ut";
            // 
            // FCopyMenuItem
            // 
            this.FCopyMenuItem.CategoryIndex = 1;
            this.FCopyMenuItem.ID = "Copy";
            this.FCopyMenuItem.ImageIndex = 7;
            this.FCopyMenuItem.MergeOrder = 10;
            this.FCopyMenuItem.ShortcutText = "Ctrl+C";
            this.FCopyMenuItem.Text = "&Copy";
            // 
            // FPasteMenuItem
            // 
            this.FPasteMenuItem.CategoryIndex = 1;
            this.FPasteMenuItem.ID = "Paste";
            this.FPasteMenuItem.ImageIndex = 8;
            this.FPasteMenuItem.MergeOrder = 10;
            this.FPasteMenuItem.ShortcutText = "Ctrl+V";
            this.FPasteMenuItem.Text = "&Paste";
            // 
            // FDeleteMenuItem
            // 
            this.FDeleteMenuItem.CategoryIndex = 1;
            this.FDeleteMenuItem.ID = "Delete";
            this.FDeleteMenuItem.ImageIndex = 4;
            this.FDeleteMenuItem.MergeOrder = 10;
            this.FDeleteMenuItem.ShortcutText = "Delete";
            this.FDeleteMenuItem.Text = "&Delete";
            // 
            // FRenameMenuItem
            // 
            this.FRenameMenuItem.CategoryIndex = 1;
            this.FRenameMenuItem.ID = "Rename";
            this.FRenameMenuItem.ImageIndex = 5;
            this.FRenameMenuItem.MergeOrder = 10;
            this.FRenameMenuItem.ShortcutText = "F2";
            this.FRenameMenuItem.Text = "&Rename";
            // 
            // FViewMenu
            // 
            this.FViewMenu.CategoryIndex = 2;
            this.FViewMenu.ID = "View";
            this.FViewMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FShowPaletteMenuItem,
            this.FShowPropertiesMenuItem,
            this.FShowFormMenuItem});
            this.FViewMenu.MergeOrder = 10;
            this.FViewMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FViewMenu.SeparatorIndices.AddRange(new int[] {
            0});
            this.FViewMenu.Text = "&View";
            // 
            // FShowPaletteMenuItem
            // 
            this.FShowPaletteMenuItem.CategoryIndex = 2;
            this.FShowPaletteMenuItem.ID = "ShowPalette";
            this.FShowPaletteMenuItem.MergeOrder = 20;
            this.FShowPaletteMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF11;
            this.FShowPaletteMenuItem.Text = "P&alette";
            // 
            // FShowPropertiesMenuItem
            // 
            this.FShowPropertiesMenuItem.CategoryIndex = 2;
            this.FShowPropertiesMenuItem.ID = "ShowProperties";
            this.FShowPropertiesMenuItem.MergeOrder = 20;
            this.FShowPropertiesMenuItem.Shortcut = System.Windows.Forms.Shortcut.F11;
            this.FShowPropertiesMenuItem.Text = "&Properties";
            // 
            // FShowFormMenuItem
            // 
            this.FShowFormMenuItem.CategoryIndex = 2;
            this.FShowFormMenuItem.ID = "ShowForm";
            this.FShowFormMenuItem.MergeOrder = 20;
            this.FShowFormMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF12;
            this.FShowFormMenuItem.Text = "&Form";
            // 
            // FFileBar
            // 
            this.FFileBar.BarName = "FileBar";
            this.FFileBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FSaveMenuItem,
            this.FSaveAsFile,
            this.FSaveAsDocumentMenuItem});
            this.FFileBar.Manager = this.FFrameBarManager;
            // 
            // FEditBar
            // 
            this.FEditBar.BarName = "EditBar";
            this.FEditBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FCutMenuItem,
            this.FCopyMenuItem,
            this.FPasteMenuItem,
            this.FDeleteMenuItem,
            this.FRenameMenuItem});
            this.FEditBar.Manager = this.FFrameBarManager;
            // 
            // FNodesPopupMenu
            // 
            this.FNodesPopupMenu.ParentBarItem = this.FEditMenuItem;
            // 
            // FDockPanel
            // 
            this.FDockPanel.ActiveAutoHideContent = null;
            this.FDockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FDockPanel.DocumentStyle = WeifenLuo.WinFormsUI.Docking.DocumentStyle.DockingSdi;
            this.FDockPanel.Location = new System.Drawing.Point(0, 0);
            this.FDockPanel.Name = "FDockPanel";
            this.FDockPanel.Size = new System.Drawing.Size(687, 518);
            this.FDockPanel.TabIndex = 4;
            // 
            // FMenuStrip
            // 
            this.FMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FFileToolStripMenuItem,
            this.FEditToolStripMenuItem,
            this.FViewToolStripMenuItem});
            this.FMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.FMenuStrip.Name = "FMenuStrip";
            this.FMenuStrip.Size = new System.Drawing.Size(687, 24);
            this.FMenuStrip.TabIndex = 9;
            this.FMenuStrip.Text = "menuStrip1";
            // 
            // FFileToolStripMenuItem
            // 
            this.FFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSaveAsFileToolStripMenuItem,
            this.FSaveAsDocumentToolStripMenuItem,
            this.FCloseToolStripMenuItem});
            this.FFileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
            this.FFileToolStripMenuItem.Name = "FFileToolStripMenuItem";
            this.FFileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.FFileToolStripMenuItem.Text = "File";
            // 
            // FSaveAsFileToolStripMenuItem
            // 
            this.FSaveAsFileToolStripMenuItem.Name = "FSaveAsFileToolStripMenuItem";
            this.FSaveAsFileToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FSaveAsFileToolStripMenuItem.Text = "Save As File";
            // 
            // FSaveAsDocumentToolStripMenuItem
            // 
            this.FSaveAsDocumentToolStripMenuItem.Name = "FSaveAsDocumentToolStripMenuItem";
            this.FSaveAsDocumentToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FSaveAsDocumentToolStripMenuItem.Text = "Save As Document";
            // 
            // FCloseToolStripMenuItem
            // 
            this.FCloseToolStripMenuItem.Name = "FCloseToolStripMenuItem";
            this.FCloseToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FCloseToolStripMenuItem.Text = "Close";
            // 
            // FEditToolStripMenuItem
            // 
            this.FEditToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FCutToolStripMenuItem,
            this.FCopyToolStripMenuItem,
            this.FPasteToolStripMenuItem,
            this.FDeleteToolStripMenuItem,
            this.FRenameToolStripMenuItem});
            this.FEditToolStripMenuItem.Name = "FEditToolStripMenuItem";
            this.FEditToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.FEditToolStripMenuItem.Text = "Edit";
            // 
            // FCutToolStripMenuItem
            // 
            this.FCutToolStripMenuItem.Name = "FCutToolStripMenuItem";
            this.FCutToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.FCutToolStripMenuItem.Text = "Cut";
            // 
            // FCopyToolStripMenuItem
            // 
            this.FCopyToolStripMenuItem.Name = "FCopyToolStripMenuItem";
            this.FCopyToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.FCopyToolStripMenuItem.Text = "Copy";
            // 
            // FPasteToolStripMenuItem
            // 
            this.FPasteToolStripMenuItem.Name = "FPasteToolStripMenuItem";
            this.FPasteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.FPasteToolStripMenuItem.Text = "Paste";
            // 
            // FDeleteToolStripMenuItem
            // 
            this.FDeleteToolStripMenuItem.Name = "FDeleteToolStripMenuItem";
            this.FDeleteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.FDeleteToolStripMenuItem.Text = "Delete";
            // 
            // FRenameToolStripMenuItem
            // 
            this.FRenameToolStripMenuItem.Name = "FRenameToolStripMenuItem";
            this.FRenameToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.FRenameToolStripMenuItem.Text = "Rename";
            // 
            // FViewToolStripMenuItem
            // 
            this.FViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FPaletteToolStripMenuItem,
            this.FPropertiesToolStripMenuItem,
            this.FFormToolStripMenuItem});
            this.FViewToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
            this.FViewToolStripMenuItem.Name = "FViewToolStripMenuItem";
            this.FViewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.FViewToolStripMenuItem.Text = "View";
            // 
            // FPaletteToolStripMenuItem
            // 
            this.FPaletteToolStripMenuItem.Name = "FPaletteToolStripMenuItem";
            this.FPaletteToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.FPaletteToolStripMenuItem.Text = "Palette";
            // 
            // FPropertiesToolStripMenuItem
            // 
            this.FPropertiesToolStripMenuItem.Name = "FPropertiesToolStripMenuItem";
            this.FPropertiesToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.FPropertiesToolStripMenuItem.Text = "Properties";
            // 
            // FFormToolStripMenuItem
            // 
            this.FFormToolStripMenuItem.Name = "FFormToolStripMenuItem";
            this.FFormToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.FFormToolStripMenuItem.Text = "Form";
            // 
            // FToolStrip
            // 
            this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSaveToolStripButton,
            this.FSaveAsFileToolStripButton,
            this.FSaveAsDocumentToolStripButton,
            this.FToolStripSeparator,
            this.FCutToolStripButton,
            this.FCopyToolStripButton2,
            this.FPasteToolStripButton3,
            this.FDeleteToolStripButton4,
            this.FRenameToolStripButton1});
            this.FToolStrip.Location = new System.Drawing.Point(0, 24);
            this.FToolStrip.Name = "FToolStrip";
            this.FToolStrip.Size = new System.Drawing.Size(687, 25);
            this.FToolStrip.TabIndex = 10;
            this.FToolStrip.Text = "toolStrip1";
            // 
            // FSaveToolStripButton
            // 
            this.FSaveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FSaveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FSaveToolStripButton.Image")));
            this.FSaveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FSaveToolStripButton.Name = "FSaveToolStripButton";
            this.FSaveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FSaveToolStripButton.Text = "toolStripButton1";
            // 
            // FSaveAsFileToolStripButton
            // 
            this.FSaveAsFileToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FSaveAsFileToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FSaveAsFileToolStripButton.Image")));
            this.FSaveAsFileToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FSaveAsFileToolStripButton.Name = "FSaveAsFileToolStripButton";
            this.FSaveAsFileToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FSaveAsFileToolStripButton.Text = "toolStripButton2";
            // 
            // FSaveAsDocumentToolStripButton
            // 
            this.FSaveAsDocumentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FSaveAsDocumentToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FSaveAsDocumentToolStripButton.Image")));
            this.FSaveAsDocumentToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FSaveAsDocumentToolStripButton.Name = "FSaveAsDocumentToolStripButton";
            this.FSaveAsDocumentToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FSaveAsDocumentToolStripButton.Text = "toolStripButton3";
            // 
            // FToolStripSeparator
            // 
            this.FToolStripSeparator.Name = "FToolStripSeparator";
            this.FToolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // FCutToolStripButton
            // 
            this.FCutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FCutToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FCutToolStripButton.Image")));
            this.FCutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FCutToolStripButton.Name = "FCutToolStripButton";
            this.FCutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FCutToolStripButton.Text = "toolStripButton1";
            // 
            // FCopyToolStripButton2
            // 
            this.FCopyToolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FCopyToolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("FCopyToolStripButton2.Image")));
            this.FCopyToolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FCopyToolStripButton2.Name = "FCopyToolStripButton2";
            this.FCopyToolStripButton2.Size = new System.Drawing.Size(23, 22);
            this.FCopyToolStripButton2.Text = "toolStripButton2";
            // 
            // FPasteToolStripButton3
            // 
            this.FPasteToolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FPasteToolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("FPasteToolStripButton3.Image")));
            this.FPasteToolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FPasteToolStripButton3.Name = "FPasteToolStripButton3";
            this.FPasteToolStripButton3.Size = new System.Drawing.Size(23, 22);
            this.FPasteToolStripButton3.Text = "toolStripButton3";
            // 
            // FDeleteToolStripButton4
            // 
            this.FDeleteToolStripButton4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDeleteToolStripButton4.Image = ((System.Drawing.Image)(resources.GetObject("FDeleteToolStripButton4.Image")));
            this.FDeleteToolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDeleteToolStripButton4.Name = "FDeleteToolStripButton4";
            this.FDeleteToolStripButton4.Size = new System.Drawing.Size(23, 22);
            this.FDeleteToolStripButton4.Text = "toolStripButton4";
            // 
            // FRenameToolStripButton1
            // 
            this.FRenameToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FRenameToolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("FRenameToolStripButton1.Image")));
            this.FRenameToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FRenameToolStripButton1.Name = "FRenameToolStripButton1";
            this.FRenameToolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.FRenameToolStripButton1.Text = "toolStripButton1";
            // 
            // FormDesigner
            // 
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(687, 518);
            this.Controls.Add(this.FToolStrip);
            this.Controls.Add(this.FMenuStrip);
            this.Controls.Add(this.FDockPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormDesigner";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            ((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).EndInit();
            this.FMenuStrip.ResumeLayout(false);
            this.FMenuStrip.PerformLayout();
            this.FToolStrip.ResumeLayout(false);
            this.FToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

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
			FDeleteMenuItem.Enabled = LEditsAllowed;
			FRenameMenuItem.Enabled = LEditsAllowed;
			FCutMenuItem.Enabled = LEditsAllowed;
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

		private void FrameBarManagerItemClicked(object ASender, Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventArgs AArgs)
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
