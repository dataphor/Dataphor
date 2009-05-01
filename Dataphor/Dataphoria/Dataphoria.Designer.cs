/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Text;

#if TRACEHANDLECOLLECTOR 
using System.Reflection;
using System.Diagnostics;
#endif

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Dataphoria.ObjectTree;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.Services;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;

using Syncfusion.Windows.Forms.Tools;
using Syncfusion.Windows.Forms.Tools.XPMenus;

namespace Alphora.Dataphor.Dataphoria
{
	/// <summary> Dataphoria Server Manager Form. </summary>
	partial class Dataphoria : System.Windows.Forms.Form, IErrorSource, IServiceProvider
	{
		private Alphora.Dataphor.Dataphoria.ObjectTree.DataTree FExplorer;
		private Syncfusion.Windows.Forms.Tools.DockingManager FDockingManager;
		private Syncfusion.Windows.Forms.Tools.XPMenus.MainFrameBarManager FFrameBarManager;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FFileMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FNewMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FNewScriptMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FOpenMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FOpenWithMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExitMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FConnectMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FDisconnectMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FLaunchFormMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FMainMenuBar;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FViewMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FShowExplorerMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FShowWarningsMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenusManager FPopupManager;
		private Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenu FWarningsPopup;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FWarningsPopupParent;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FConfigureMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FHelpMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FDocumentationMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FAboutMenuItem;
		private System.Windows.Forms.ImageList FTreeImageList;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FFileBar;
		private System.Windows.Forms.ImageList FMenuImageList;
		private Alphora.Dataphor.Frontend.Client.Windows.ErrorListView FErrorListView;
		private System.ComponentModel.IContainer components;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FDesignerLibrariesMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FDocumentTypesMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FWebsiteMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FGroupsMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FWebDocumentationMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FClearWarningsContextMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FClearWarningsMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAllMenuItem;
		private Syncfusion.Windows.Forms.Tools.CommandBar FStatusCommandBar;
		private Syncfusion.Windows.Forms.Tools.StatusBarAdv FStatusBar;

		protected override void Dispose( bool disposing )
		{
			try
			{
				if (!IsDisposed)
				{
					try
					{
						SaveSettings();
					}
					finally
					{
						try
						{
							InternalDisconnect();
						}
						finally
						{
							if( disposing )
							{
								if(components != null)
								{
									components.Dispose();
								}
							}
						}
					}
				}
			}
			finally
			{	
				base.Dispose( disposing );
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dataphoria));
			this.FTreeImageList = new System.Windows.Forms.ImageList(this.components);
			this.FExplorer = new Alphora.Dataphor.Dataphoria.ObjectTree.DataTree();
			this.FDockingManager = new Syncfusion.Windows.Forms.Tools.DockingManager(this.components);
			this.FErrorListView = new Alphora.Dataphor.Frontend.Client.Windows.ErrorListView();
			this.FFrameBarManager = new Syncfusion.Windows.Forms.Tools.XPMenus.MainFrameBarManager(this);
			this.FMainMenuBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "MainMenu");
			this.FFileMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FConnectMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FDisconnectMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FNewMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FNewScriptMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FOpenMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FOpenWithMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FSaveAllMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FLaunchFormMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FExitMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FViewMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FShowExplorerMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FShowWarningsMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FClearWarningsMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FConfigureMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FDesignerLibrariesMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FDocumentTypesMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FHelpMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FDocumentationMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FWebsiteMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FGroupsMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FWebDocumentationMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FAboutMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FFileBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "FileBar");
			this.FStatusCommandBar = new Syncfusion.Windows.Forms.Tools.CommandBar();
			this.FStatusBar = new Syncfusion.Windows.Forms.Tools.StatusBarAdv();
			this.FMenuImageList = new System.Windows.Forms.ImageList(this.components);
			this.FClearWarningsContextMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FWarningsPopupParent = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FPopupManager = new Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenusManager(this.components);
			this.FWarningsPopup = new Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenu(this.components);
			((System.ComponentModel.ISupportInitialize)(this.FDockingManager)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).BeginInit();
			this.FStatusCommandBar.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.FStatusBar)).BeginInit();
			this.SuspendLayout();
			// 
			// FTreeImageList
			// 
			this.FTreeImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FTreeImageList.ImageStream")));
			this.FTreeImageList.TransparentColor = System.Drawing.Color.Lime;
			this.FTreeImageList.Images.SetKeyName(0, "");
			this.FTreeImageList.Images.SetKeyName(1, "");
			this.FTreeImageList.Images.SetKeyName(2, "");
			this.FTreeImageList.Images.SetKeyName(3, "");
			this.FTreeImageList.Images.SetKeyName(4, "");
			this.FTreeImageList.Images.SetKeyName(5, "");
			this.FTreeImageList.Images.SetKeyName(6, "");
			this.FTreeImageList.Images.SetKeyName(7, "");
			this.FTreeImageList.Images.SetKeyName(8, "");
			this.FTreeImageList.Images.SetKeyName(9, "");
			this.FTreeImageList.Images.SetKeyName(10, "");
			this.FTreeImageList.Images.SetKeyName(11, "");
			this.FTreeImageList.Images.SetKeyName(12, "");
			this.FTreeImageList.Images.SetKeyName(13, "Tables2.png");
			this.FTreeImageList.Images.SetKeyName(14, "Table2.png");
			this.FTreeImageList.Images.SetKeyName(15, "");
			this.FTreeImageList.Images.SetKeyName(16, "");
			this.FTreeImageList.Images.SetKeyName(17, "");
			this.FTreeImageList.Images.SetKeyName(18, "");
			this.FTreeImageList.Images.SetKeyName(19, "");
			this.FTreeImageList.Images.SetKeyName(20, "");
			this.FTreeImageList.Images.SetKeyName(21, "");
			this.FTreeImageList.Images.SetKeyName(22, "");
			this.FTreeImageList.Images.SetKeyName(23, "");
			this.FTreeImageList.Images.SetKeyName(24, "");
			this.FTreeImageList.Images.SetKeyName(25, "");
			this.FTreeImageList.Images.SetKeyName(26, "");
			this.FTreeImageList.Images.SetKeyName(27, "");
			this.FTreeImageList.Images.SetKeyName(28, "");
			this.FTreeImageList.Images.SetKeyName(29, "Views.png");
			this.FTreeImageList.Images.SetKeyName(30, "View.png");
			// 
			// FExplorer
			// 
			this.FExplorer.AllowDrop = true;
			this.FExplorer.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.FExplorer.CausesValidation = false;
			this.FDockingManager.SetEnableDocking(this.FExplorer, true);
			this.FExplorer.HideSelection = false;
			this.FExplorer.ImageIndex = 0;
			this.FExplorer.ImageList = this.FTreeImageList;
			this.FExplorer.Location = new System.Drawing.Point(1, 21);
			this.FExplorer.Name = "FExplorer";
			this.FExplorer.SelectedImageIndex = 0;
			this.FExplorer.ShowRootLines = false;
			this.FExplorer.Size = new System.Drawing.Size(248, 168);
			this.FExplorer.TabIndex = 1;
			this.FExplorer.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.FExplorer_HelpRequested);
			// 
			// FDockingManager
			// 
			this.FDockingManager.AutoHideTabFont = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.World);
			this.FDockingManager.DockLayoutStream = ((System.IO.MemoryStream)(resources.GetObject("FDockingManager.DockLayoutStream")));
			this.FDockingManager.DockTabFont = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.World);
			this.FDockingManager.ForwardMenuShortcuts = false;
			this.FDockingManager.HostControl = this;
			this.FDockingManager.SetDockLabel(this.FExplorer, "Dataphor Explorer");
			this.FDockingManager.SetDockLabel(this.FErrorListView, "Warnings");
			this.FDockingManager.SetAutoHideOnLoad(this.FErrorListView, true);
			// 
			// FErrorListView
			// 
			this.FDockingManager.SetEnableDocking(this.FErrorListView, true);
			this.FErrorListView.Location = new System.Drawing.Point(1, 21);
			this.FErrorListView.Name = "FErrorListView";
			this.FErrorListView.Size = new System.Drawing.Size(677, 163);
			this.FErrorListView.TabIndex = 11;
			this.FPopupManager.SetXPContextMenu(this.FErrorListView, this.FWarningsPopup);
			this.FErrorListView.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.FErrorListView_HelpRequested);
			// 
			// FFrameBarManager
			// 
			this.FFrameBarManager.AutoLoadToolBarPositions = false;
			this.FFrameBarManager.AutoPersistCustomization = false;
			this.FFrameBarManager.BarPositionInfo = ((System.IO.MemoryStream)(resources.GetObject("FFrameBarManager.BarPositionInfo")));
			this.FFrameBarManager.Bars.Add(this.FMainMenuBar);
			this.FFrameBarManager.Bars.Add(this.FFileBar);
			this.FFrameBarManager.Categories.Add("File");
			this.FFrameBarManager.Categories.Add("View");
			this.FFrameBarManager.Categories.Add("Configure");
			this.FFrameBarManager.Categories.Add("Help");
			this.FFrameBarManager.CurrentBaseFormType = "System.Windows.Forms.Form";
			this.FFrameBarManager.DetachedCommandBars.Add(this.FStatusCommandBar);
			this.FFrameBarManager.Form = this;
			this.FFrameBarManager.ImageList = this.FMenuImageList;
			this.FFrameBarManager.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FFileMenu,
            this.FConnectMenuItem,
            this.FDisconnectMenuItem,
            this.FLaunchFormMenuItem,
            this.FNewMenuItem,
            this.FNewScriptMenuItem,
            this.FOpenMenuItem,
            this.FOpenWithMenuItem,
            this.FExitMenuItem,
            this.FViewMenu,
            this.FShowExplorerMenuItem,
            this.FShowWarningsMenuItem,
            this.FClearWarningsContextMenuItem,
            this.FWarningsPopupParent,
            this.FConfigureMenu,
            this.FDesignerLibrariesMenuItem,
            this.FHelpMenu,
            this.FDocumentationMenuItem,
            this.FAboutMenuItem,
            this.FDocumentTypesMenuItem,
            this.FWebsiteMenuItem,
            this.FGroupsMenuItem,
            this.FWebDocumentationMenuItem,
            this.FClearWarningsMenuItem,
            this.FSaveAllMenuItem});
			this.FFrameBarManager.LargeImageList = null;
			this.FFrameBarManager.ResetCustomization = false;
			this.FFrameBarManager.Style = Syncfusion.Windows.Forms.VisualStyle.Office2003;
			this.FFrameBarManager.UsePartialMenus = false;
			this.FFrameBarManager.ItemClicked += new Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventHandler(this.FrameBarManagerItemClicked);
			// 
			// FMainMenuBar
			// 
			this.FMainMenuBar.BarName = "MainMenu";
			this.FMainMenuBar.BarStyle = ((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle)(((((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.AllowQuickCustomizing | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.IsMainMenu)
						| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.RotateWhenVertical)
						| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.Visible)
						| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.DrawDragBorder)));
			this.FMainMenuBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FFileMenu,
            this.FViewMenu,
            this.FConfigureMenu,
            this.FHelpMenu});
			this.FMainMenuBar.Manager = this.FFrameBarManager;
			// 
			// FFileMenu
			// 
			this.FFileMenu.CategoryIndex = 0;
			this.FFileMenu.ID = "File";
			this.FFileMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FConnectMenuItem,
            this.FDisconnectMenuItem,
            this.FNewMenuItem,
            this.FNewScriptMenuItem,
            this.FOpenMenuItem,
            this.FOpenWithMenuItem,
            this.FSaveAllMenuItem,
            this.FLaunchFormMenuItem,
            this.FExitMenuItem});
			this.FFileMenu.MergeOrder = 1;
			this.FFileMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
			this.FFileMenu.SeparatorIndices.AddRange(new int[] {
            2,
            6,
            7});
			this.FFileMenu.Text = "&File";
			// 
			// FConnectMenuItem
			// 
			this.FConnectMenuItem.CategoryIndex = 0;
			this.FConnectMenuItem.ID = "Connect";
			this.FConnectMenuItem.ImageIndex = 7;
			this.FConnectMenuItem.MergeOrder = 10;
			this.FConnectMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftC;
			this.FConnectMenuItem.Text = "&Connect";
			// 
			// FDisconnectMenuItem
			// 
			this.FDisconnectMenuItem.CategoryIndex = 0;
			this.FDisconnectMenuItem.ID = "Disconnect";
			this.FDisconnectMenuItem.ImageIndex = 8;
			this.FDisconnectMenuItem.MergeOrder = 10;
			this.FDisconnectMenuItem.Text = "&Disconnect";
			// 
			// FNewMenuItem
			// 
			this.FNewMenuItem.CategoryIndex = 0;
			this.FNewMenuItem.ID = "New";
			this.FNewMenuItem.ImageIndex = 3;
			this.FNewMenuItem.MergeOrder = 10;
			this.FNewMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftN;
			this.FNewMenuItem.Text = "&New...";
			// 
			// FNewScriptMenuItem
			// 
			this.FNewScriptMenuItem.CategoryIndex = 0;
			this.FNewScriptMenuItem.ID = "NewScript";
			this.FNewScriptMenuItem.ImageIndex = 12;
			this.FNewScriptMenuItem.MergeOrder = 10;
			this.FNewScriptMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.FNewScriptMenuItem.Text = "N&ew Script";
			// 
			// FOpenMenuItem
			// 
			this.FOpenMenuItem.CategoryIndex = 0;
			this.FOpenMenuItem.ID = "Open";
			this.FOpenMenuItem.ImageIndex = 4;
			this.FOpenMenuItem.MergeOrder = 10;
			this.FOpenMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.FOpenMenuItem.Text = "&Open File...";
			// 
			// FOpenWithMenuItem
			// 
			this.FOpenWithMenuItem.CategoryIndex = 0;
			this.FOpenWithMenuItem.ID = "OpenWith";
			this.FOpenWithMenuItem.ImageIndex = 5;
			this.FOpenWithMenuItem.MergeOrder = 10;
			this.FOpenWithMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlW;
			this.FOpenWithMenuItem.Text = "Open File &With...";
			// 
			// FSaveAllMenuItem
			// 
			this.FSaveAllMenuItem.CategoryIndex = 0;
			this.FSaveAllMenuItem.ID = "SaveAll";
			this.FSaveAllMenuItem.ImageIndex = 11;
			this.FSaveAllMenuItem.MergeOrder = 15;
			this.FSaveAllMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftS;
			this.FSaveAllMenuItem.Text = "Sa&ve All";
			// 
			// FLaunchFormMenuItem
			// 
			this.FLaunchFormMenuItem.CategoryIndex = 0;
			this.FLaunchFormMenuItem.ID = "LaunchForm";
			this.FLaunchFormMenuItem.ImageIndex = 6;
			this.FLaunchFormMenuItem.MergeOrder = 900;
			this.FLaunchFormMenuItem.Shortcut = System.Windows.Forms.Shortcut.F6;
			this.FLaunchFormMenuItem.Text = "&Launch Form...";
			// 
			// FExitMenuItem
			// 
			this.FExitMenuItem.CategoryIndex = 0;
			this.FExitMenuItem.ID = "Exit";
			this.FExitMenuItem.MergeOrder = 1000;
			this.FExitMenuItem.Shortcut = System.Windows.Forms.Shortcut.AltF4;
			this.FExitMenuItem.Text = "E&xit";
			// 
			// FViewMenu
			// 
			this.FViewMenu.CategoryIndex = 1;
			this.FViewMenu.ID = "View";
			this.FViewMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FShowExplorerMenuItem,
            this.FShowWarningsMenuItem,
            this.FClearWarningsMenuItem});
			this.FViewMenu.MergeOrder = 10;
			this.FViewMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
			this.FViewMenu.SeparatorIndices.AddRange(new int[] {
            2});
			this.FViewMenu.Text = "&View";
			// 
			// FShowExplorerMenuItem
			// 
			this.FShowExplorerMenuItem.CategoryIndex = 1;
			this.FShowExplorerMenuItem.ID = "ShowExplorer";
			this.FShowExplorerMenuItem.MergeOrder = 10;
			this.FShowExplorerMenuItem.Shortcut = System.Windows.Forms.Shortcut.F12;
			this.FShowExplorerMenuItem.Text = "&Dataphor Explorer";
			// 
			// FShowWarningsMenuItem
			// 
			this.FShowWarningsMenuItem.CategoryIndex = 1;
			this.FShowWarningsMenuItem.ID = "ShowWarnings";
			this.FShowWarningsMenuItem.MergeOrder = 10;
			this.FShowWarningsMenuItem.Text = "&Warnings && Errors";
			// 
			// FClearWarningsMenuItem
			// 
			this.FClearWarningsMenuItem.CategoryIndex = 1;
			this.FClearWarningsMenuItem.ID = "ClearWarnings";
			this.FClearWarningsMenuItem.MergeOrder = 150;
			this.FClearWarningsMenuItem.Text = "&Clear Warnings";
			// 
			// FConfigureMenu
			// 
			this.FConfigureMenu.CategoryIndex = 2;
			this.FConfigureMenu.ID = "Configure";
			this.FConfigureMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FDesignerLibrariesMenuItem,
            this.FDocumentTypesMenuItem});
			this.FConfigureMenu.MergeOrder = 20;
			this.FConfigureMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
			this.FConfigureMenu.Text = "&Configure";
			// 
			// FDesignerLibrariesMenuItem
			// 
			this.FDesignerLibrariesMenuItem.CategoryIndex = 2;
			this.FDesignerLibrariesMenuItem.ID = "DesignerLibraries";
			this.FDesignerLibrariesMenuItem.MergeOrder = 10;
			this.FDesignerLibrariesMenuItem.Text = "D&esigner Libraries...";
			// 
			// FDocumentTypesMenuItem
			// 
			this.FDocumentTypesMenuItem.CategoryIndex = 2;
			this.FDocumentTypesMenuItem.ID = "DocumentTypes";
			this.FDocumentTypesMenuItem.MergeOrder = 10;
			this.FDocumentTypesMenuItem.Text = "D&ocument Types...";
			// 
			// FHelpMenu
			// 
			this.FHelpMenu.CategoryIndex = 3;
			this.FHelpMenu.ID = "Help";
			this.FHelpMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FDocumentationMenuItem,
            this.FWebsiteMenuItem,
            this.FGroupsMenuItem,
            this.FWebDocumentationMenuItem,
            this.FAboutMenuItem});
			this.FHelpMenu.MergeOrder = 1000;
			this.FHelpMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
			this.FHelpMenu.SeparatorIndices.AddRange(new int[] {
            1,
            4});
			this.FHelpMenu.Text = "&Help";
			// 
			// FDocumentationMenuItem
			// 
			this.FDocumentationMenuItem.CategoryIndex = 3;
			this.FDocumentationMenuItem.ID = "Documentation";
			this.FDocumentationMenuItem.ImageIndex = 9;
			this.FDocumentationMenuItem.MergeOrder = 10;
			this.FDocumentationMenuItem.Text = "&Dataphor Documentation...";
			// 
			// FWebsiteMenuItem
			// 
			this.FWebsiteMenuItem.CategoryIndex = 3;
			this.FWebsiteMenuItem.ID = "Website";
			this.FWebsiteMenuItem.ImageIndex = 10;
			this.FWebsiteMenuItem.Text = "A&lphora Website...";
			// 
			// FGroupsMenuItem
			// 
			this.FGroupsMenuItem.CategoryIndex = 3;
			this.FGroupsMenuItem.ID = "Groups";
			this.FGroupsMenuItem.Text = "Alphora Discussion &Groups...";
			// 
			// FWebDocumentationMenuItem
			// 
			this.FWebDocumentationMenuItem.CategoryIndex = 3;
			this.FWebDocumentationMenuItem.ID = "WebDocumentation";
			this.FWebDocumentationMenuItem.Text = "&Web Documentation...";
			// 
			// FAboutMenuItem
			// 
			this.FAboutMenuItem.CategoryIndex = 3;
			this.FAboutMenuItem.ID = "About";
			this.FAboutMenuItem.MergeOrder = 10;
			this.FAboutMenuItem.Text = "&About...";
			// 
			// FFileBar
			// 
			this.FFileBar.BarName = "FileBar";
			this.FFileBar.BarStyle = ((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle)((((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.AllowQuickCustomizing | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.RotateWhenVertical)
						| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.Visible)
						| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.DrawDragBorder)));
			this.FFileBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FNewMenuItem,
            this.FNewScriptMenuItem,
            this.FOpenMenuItem,
            this.FOpenWithMenuItem,
            this.FLaunchFormMenuItem});
			this.FFileBar.Manager = this.FFrameBarManager;
			this.FFileBar.SeparatorIndices.AddRange(new int[] {
            3});
			// 
			// FStatusCommandBar
			// 
			this.FStatusCommandBar.AllowedDockBorders = Syncfusion.Windows.Forms.Tools.CommandBarDockBorder.Bottom;
			this.FStatusCommandBar.Controls.Add(this.FStatusBar);
			this.FStatusCommandBar.DockState = Syncfusion.Windows.Forms.Tools.CommandBarDockState.Bottom;
			this.FStatusCommandBar.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.World);
			this.FStatusCommandBar.HideDropDownButton = true;
			this.FStatusCommandBar.HideGripper = true;
			this.FStatusCommandBar.MaxLength = 200;
			this.FStatusCommandBar.MinHeight = 22;
			this.FStatusCommandBar.MinLength = 50;
			this.FStatusCommandBar.Name = "FStatusCommandBar";
			this.FStatusCommandBar.OccupyFullRow = true;
			this.FStatusCommandBar.RowIndex = 0;
			this.FStatusCommandBar.RowOffset = 0;
			this.FStatusCommandBar.TabIndex = 2;
			this.FStatusCommandBar.TabStop = false;
			// 
			// FStatusBar
			// 
			this.FStatusBar.BorderColor = System.Drawing.Color.Black;
			this.FStatusBar.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.FStatusBar.CustomLayoutBounds = new System.Drawing.Rectangle(0, 0, 0, 0);
			this.FStatusBar.Location = new System.Drawing.Point(2, 1);
			this.FStatusBar.Name = "FStatusBar";
			this.FStatusBar.Padding = new System.Windows.Forms.Padding(3);
			this.FStatusBar.Size = new System.Drawing.Size(676, 21);
			this.FStatusBar.Spacing = new System.Drawing.Size(2, 2);
			this.FStatusBar.TabIndex = 1;
			// 
			// FMenuImageList
			// 
			this.FMenuImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FMenuImageList.ImageStream")));
			this.FMenuImageList.TransparentColor = System.Drawing.Color.Transparent;
		
			// 
			// FClearWarningsContextMenuItem
			// 
			this.FClearWarningsContextMenuItem.CategoryIndex = -1;
			this.FClearWarningsContextMenuItem.ID = "ContextClearWarnings";
			this.FClearWarningsContextMenuItem.Text = "&Clear Warnings";
			this.FClearWarningsContextMenuItem.Click += new System.EventHandler(this.ClearWarningsClicked);
			// 
			// FWarningsPopupParent
			// 
			this.FWarningsPopupParent.CategoryIndex = -1;
			this.FWarningsPopupParent.ID = "WarningsMenu";
			this.FWarningsPopupParent.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FClearWarningsContextMenuItem});
			// 
			// FWarningsPopup
			// 
			this.FWarningsPopup.ParentBarItem = this.FWarningsPopupParent;
			// 
			// Dataphoria
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(679, 453);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.IsMdiContainer = true;
			this.KeyPreview = true;
			this.Name = "Dataphoria";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Dataphoria - Not Connected";
			this.Shown += new System.EventHandler(this.Dataphoria_Shown);
			((System.ComponentModel.ISupportInitialize)(this.FDockingManager)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).EndInit();
			this.FStatusCommandBar.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.FStatusBar)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		
		
	}

	
}
