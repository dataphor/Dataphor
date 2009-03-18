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

    partial class FormDesigner : BaseForm, ILiveDesigner, IErrorSource, IServiceProvider, IContainer, IChildFormWithToolBar
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
        private ToolStripButton FCopyToolStripButton;
        private ToolStripButton FPasteToolStripButton;
        private ToolStripButton FDeleteToolStripButton;
        private ToolStripButton FRenameToolStripButton;




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
            this.FCopyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FPasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FDeleteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FRenameToolStripButton = new System.Windows.Forms.ToolStripButton();
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
            this.FSaveAsFileToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
            this.FSaveAsFileToolStripMenuItem.Name = "FSaveAsFileToolStripMenuItem";
            this.FSaveAsFileToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FSaveAsFileToolStripMenuItem.Text = "Save As File";
            // 
            // FSaveAsDocumentToolStripMenuItem
            // 
            this.FSaveAsDocumentToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
            this.FSaveAsDocumentToolStripMenuItem.Name = "FSaveAsDocumentToolStripMenuItem";
            this.FSaveAsDocumentToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FSaveAsDocumentToolStripMenuItem.Text = "Save As Document";
            // 
            // FCloseToolStripMenuItem
            // 
            this.FCloseToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Close;
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
            this.FCutToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Cut;
            this.FCutToolStripMenuItem.Name = "FCutToolStripMenuItem";
            this.FCutToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.FCutToolStripMenuItem.Text = "Cut";
            // 
            // FCopyToolStripMenuItem
            // 
            this.FCopyToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
            this.FCopyToolStripMenuItem.Name = "FCopyToolStripMenuItem";
            this.FCopyToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.FCopyToolStripMenuItem.Text = "Copy";
            // 
            // FPasteToolStripMenuItem
            // 
            this.FPasteToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Paste;
            this.FPasteToolStripMenuItem.Name = "FPasteToolStripMenuItem";
            this.FPasteToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.FPasteToolStripMenuItem.Text = "Paste";
            // 
            // FDeleteToolStripMenuItem
            // 
            this.FDeleteToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Delete;
            this.FDeleteToolStripMenuItem.Name = "FDeleteToolStripMenuItem";
            this.FDeleteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.FDeleteToolStripMenuItem.Text = "Delete";
            // 
            // FRenameToolStripMenuItem
            // 
            this.FRenameToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Rename;
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
            this.FCopyToolStripButton,
            this.FPasteToolStripButton,
            this.FDeleteToolStripButton,
            this.FRenameToolStripButton});
            this.FToolStrip.Location = new System.Drawing.Point(0, 24);
            this.FToolStrip.Name = "FToolStrip";
            this.FToolStrip.Size = new System.Drawing.Size(687, 25);
            this.FToolStrip.TabIndex = 10;
            this.FToolStrip.Text = "toolStrip1";
            // 
            // FSaveToolStripButton
            // 
            this.FSaveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FSaveToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
            this.FSaveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FSaveToolStripButton.Name = "FSaveToolStripButton";
            this.FSaveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FSaveToolStripButton.Text = "toolStripButton1";
            // 
            // FSaveAsFileToolStripButton
            // 
            this.FSaveAsFileToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FSaveAsFileToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
            this.FSaveAsFileToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FSaveAsFileToolStripButton.Name = "FSaveAsFileToolStripButton";
            this.FSaveAsFileToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FSaveAsFileToolStripButton.Text = "toolStripButton2";
            // 
            // FSaveAsDocumentToolStripButton
            // 
            this.FSaveAsDocumentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FSaveAsDocumentToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
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
            this.FCutToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Cut;
            this.FCutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FCutToolStripButton.Name = "FCutToolStripButton";
            this.FCutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FCutToolStripButton.Text = "toolStripButton1";
            // 
            // FCopyToolStripButton
            // 
            this.FCopyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FCopyToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
            this.FCopyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FCopyToolStripButton.Name = "FCopyToolStripButton";
            this.FCopyToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FCopyToolStripButton.Text = "toolStripButton2";
            // 
            // FPasteToolStripButton
            // 
            this.FPasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FPasteToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Paste;
            this.FPasteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FPasteToolStripButton.Name = "FPasteToolStripButton";
            this.FPasteToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FPasteToolStripButton.Text = "toolStripButton3";
            // 
            // FDeleteToolStripButton
            // 
            this.FDeleteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDeleteToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Delete;
            this.FDeleteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDeleteToolStripButton.Name = "FDeleteToolStripButton";
            this.FDeleteToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FDeleteToolStripButton.Text = "toolStripButton4";
            // 
            // FRenameToolStripButton
            // 
            this.FRenameToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FRenameToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Rename;
            this.FRenameToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FRenameToolStripButton.Name = "FRenameToolStripButton";
            this.FRenameToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FRenameToolStripButton.Text = "toolStripButton1";
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

    }
}
