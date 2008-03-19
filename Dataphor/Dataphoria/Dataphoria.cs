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
	public class Dataphoria : System.Windows.Forms.Form, IErrorSource, IServiceProvider
	{
		public const string CConfigurationFileName = "Dataphoria{0}.config";
		
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

		public Dataphoria()
		{
			InitializeComponent();

			ICSharpCode.TextEditor.Document.HighlightingManager.Manager.AddSyntaxModeFileProvider(new ICSharpCode.TextEditor.Document.FileSyntaxModeProvider(PathUtility.GetBinDirectory()));

			FServices.Add(typeof(DAE.Client.Controls.Design.IPropertyTextEditorService), new PropertyTextEditorService());

			// Configure tree
			FExplorer.Dataphoria = this;
			FExplorer.Select();

			FErrorListView.OnErrorsAdded += new EventHandler(ErrorsAdded);
			FErrorListView.OnWarningsAdded += new EventHandler(WarningsAdded);

			FTabbedMDIManager = new TabbedMDIManager();
			FTabbedMDIManager.AttachToMdiContainer(this);

			LoadSettings();
		}

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

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			// HACK: Something in the WinForms validation process is returning false.  We don't care so always make sure Cancel is false at the beginning of OnClosing
			AArgs.Cancel = false;
			base.OnClosing(AArgs);
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
			this.FMenuImageList.Images.SetKeyName(0, "");
			this.FMenuImageList.Images.SetKeyName(1, "");
			this.FMenuImageList.Images.SetKeyName(2, "");
			this.FMenuImageList.Images.SetKeyName(3, "");
			this.FMenuImageList.Images.SetKeyName(4, "");
			this.FMenuImageList.Images.SetKeyName(5, "");
			this.FMenuImageList.Images.SetKeyName(6, "");
			this.FMenuImageList.Images.SetKeyName(7, "");
			this.FMenuImageList.Images.SetKeyName(8, "");
			this.FMenuImageList.Images.SetKeyName(9, "");
			this.FMenuImageList.Images.SetKeyName(10, "");
			this.FMenuImageList.Images.SetKeyName(11, "");
			this.FMenuImageList.Images.SetKeyName(12, "");
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

		#region Settings

		private Settings FSettings;
		public Settings Settings { get { return FSettings; } }

		// when the form state is saved when maximized, the restore size needs to be remembered
		private Rectangle FNormalBounds = Rectangle.Empty;

		public string GetConfigurationFileName(string AType)
		{
			return Path.Combine(PathUtility.UserAppDataPath(), String.Format(CConfigurationFileName, AType));
		}

		private ObjectTree.ServerNode FServerNode;
		
		public void RefreshLibraries()
		{
			FServerNode.Build();
			FServerNode.LibraryNode.Refresh();
		}
		
		public void RefreshDocuments(string ALibraryName)
		{
			FServerNode.Build();
			LibraryNode LLibraryNode = (LibraryNode)FServerNode.LibraryNode.FindByText(ALibraryName);
			if ((LLibraryNode != null) && (LLibraryNode.DocumentListNode != null))
				LLibraryNode.DocumentListNode.Refresh();
		}

		protected override void OnLoad(EventArgs AArgs)
		{
			base.OnLoad(AArgs);
			RestoreBoundsAndState();
		}

		private void RestoreBoundsAndState()
		{
			try
			{
				Rectangle LBounds = SystemInformation.WorkingArea;
				LBounds = (Rectangle)FSettings.GetSetting("Dataphoria.Bounds", typeof(Rectangle), LBounds);
				FNormalBounds = LBounds;
				Bounds = LBounds;

				FormWindowState LState = (FormWindowState)FSettings.GetSetting("Dataphoria.WindowState", typeof(FormWindowState), FormWindowState.Normal);
				if (LState == FormWindowState.Minimized)  // don't start minimized, because it gets messed up
					LState = FormWindowState.Normal;
				WindowState = LState;
			}
			catch (Exception LException)
			{
				Warnings.AppendError(this, LException, true);
				// don't rethrow
			}
		}

		private void LoadSettings()
		{
			try
			{
				string LFileName = GetConfigurationFileName(String.Empty);
				// Load configuration settings
				try
				{
					FSettings = new Settings(LFileName);
				}
				catch
				{
					FSettings = new Settings();
					throw;
				}

			}
			catch (Exception LException)
			{
				Warnings.AppendError(this, LException, true);
				// don't rethrow
			}
		}

		private void SaveSettings()
		{
			if (FSettings != null)
			{
				// Save the configuration settings
				FSettings.SetSetting("Dataphoria.WindowState", WindowState);
				if (WindowState == FormWindowState.Normal)
					FSettings.SetSetting("Dataphoria.Bounds", Bounds);
				else
				{
					if (FNormalBounds != Rectangle.Empty)
						FSettings.SetSetting("Dataphoria.Bounds", FNormalBounds);
				}
				FSettings.SaveSettings(GetConfigurationFileName(String.Empty));
			}
		}

		protected override void OnSizeChanged(EventArgs AArgs)
		{
			if (WindowState == FormWindowState.Normal)
				FNormalBounds = Bounds;
			base.OnSizeChanged(AArgs);
		}

		#endregion

		#region Connection

		private Alphora.Dataphor.DAE.Client.DataSession FDataSession;
		public Alphora.Dataphor.DAE.Client.DataSession DataSession { get { return FDataSession; } }
		
		private Frontend.Client.Windows.Session FFrontendSession;
		public Frontend.Client.Windows.Session FrontendSession { get { return FFrontendSession; } }
		
		private DAE.IServerProcess FUtilityProcess;
		public DAE.IServerProcess UtilityProcess { get { return FUtilityProcess; } }

		public void EnsureServerConnection()
		{
			if (FDataSession == null)
			{
				AliasConfiguration LConfiguration = AliasManager.LoadConfiguration();
				if (LConfiguration.Aliases.Count == 0)
				{
					InProcessAlias LAlias = new InProcessAlias();
					LAlias.LibraryDirectory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)), @"Libraries");
					LAlias.CatalogDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"Catalog");
					LAlias.Name = "In Process";
					LAlias.PortNumber = 8062;	// don't use the same default port as the service
					LConfiguration.Aliases.Add(LAlias);
				}
				Alphora.Dataphor.Frontend.Client.Windows.ServerConnectForm.Execute(LConfiguration);
				using (Frontend.Client.Windows.StatusForm LStatusForm = new Frontend.Client.Windows.StatusForm(Strings.Get("Connecting")))
				{
					FDataSession = new DataSession();
					try
					{
						FDataSession.AliasName = LConfiguration.DefaultAliasName;
						FDataSession.Open();

						FUtilityProcess = FDataSession.ServerSession.StartProcess(new ProcessInfo(FDataSession.ServerSession.SessionInfo));
						try
						{
							EnsureFrontendRegistered();
							
							FFrontendSession = new Frontend.Client.Windows.Session(FDataSession, false);
							try
							{
								FFrontendSession.SetLibrary("Frontend");
								FFrontendSession.OnDeserializationErrors += new DeserializationErrorsHandler(FrontendSessionDeserializationErrors);
								FServerNode = new ObjectTree.ServerNode(FDataSession.Server != null);
								FServerNode.Text = FDataSession.Alias.ToString();
								FExplorer.AddBaseNode(FServerNode);
								try
								{
									FServerNode.Expand();
									
									FDockingManager.SetDockVisibility(FExplorer, true);
									FConnectMenuItem.Visible = false;
									FDisconnectMenuItem.Visible = true;
									Text = Strings.Get("DataphoriaTitle") + " - " + FDataSession.Alias.ToString();
								}
								catch
								{
									FServerNode = null;
									FExplorer.Nodes.Clear();
									throw;
								}
							}
							catch
							{
								FFrontendSession.Dispose();
								FFrontendSession = null;
								throw;
							}
						}
						catch
						{
							if (FUtilityProcess != null)
							{
								FDataSession.ServerSession.StopProcess(FUtilityProcess);
								FUtilityProcess = null;
							}
							throw;
						}
					}
					catch
					{
						if (FDataSession != null)
						{
							FDataSession.Dispose();
							FDataSession = null;
						}
						throw;
					}
				}
			}
		}

		private void Dataphoria_Shown(object sender, EventArgs e)
		{
			EnsureServerConnection();
		}

		private void InternalDisconnect()
		{
			using (Frontend.Client.Windows.StatusForm LStatusForm = new Frontend.Client.Windows.StatusForm(Strings.Get("Disconnecting")))
			{
				if (FTabbedMDIManager != null)
					DisposeChildren();		// Don't CloseChildren(), we already did that in Disconnect()

				if (FDataSession != null)
				{
					try
					{
						Text = Strings.Get("DataphoriaTitle");
						FConnectMenuItem.Visible = true;
						FDisconnectMenuItem.Visible = false;
						FDockingManager.SetDockVisibility(FExplorer, false);
					
						FExplorer.Nodes.Clear();
						FServerNode = null;
					}
					finally
					{
						try
						{
							if (FUtilityProcess != null)
							{
								FDataSession.ServerSession.StopProcess(FUtilityProcess);
								FUtilityProcess = null;
							}
						}
						finally
						{
							try
							{
								if (FFrontendSession != null)
								{
									FFrontendSession.Dispose();	// Will dispose the connection
									FFrontendSession = null;
								}
							}
							finally
							{
								if (FDataSession != null)
								{
									FDataSession.Dispose();
									FDataSession = null;
								}
							}							
						}
					}
				}
			}
		}

		public void Disconnect()
		{
			if (FTabbedMDIManager != null)
			{
				// Make sure we can safely disconnect
				CloseChildren();
				if (FTabbedMDIManager.MdiChildren.Length > 0)
					throw new AbortException();
			}

			InternalDisconnect();
		}

		private void FrontendSessionDeserializationErrors(Host AHost, ErrorList AErrors)
		{
			IErrorSource LSource = null;
			
			if ((AHost != null) && (AHost.Children.Count > 0))
				LSource = AHost.Children[0] as IErrorSource;

			Warnings.AppendErrors(LSource, AErrors, true);
		}

		#endregion

		#region Live Designer Support

		private Bitmap LoadBitmap(string AResourceName)
		{
			Bitmap LResult = new Bitmap(GetType().Assembly.GetManifestResourceStream(AResourceName));
			LResult.MakeTransparent();
			return LResult;
		}

		public Frontend.Client.Windows.Session GetLiveDesignableFrontendSession()
		{
			Frontend.Client.Windows.Session LSession = new Frontend.Client.Windows.Session(FDataSession, false);
			LSession.AfterFormActivate += new FormInterfaceHandler(AfterFormActivated);
			LSession.OnDeserializationErrors += new DeserializationErrorsHandler(FrontendSessionDeserializationErrors);
			return LSession;
		}

		private Hashtable FDesignedForms = new Hashtable();

		private void AfterFormActivated(IFormInterface AInterface)
		{
			AInterface.HostNode.OnDocumentChanged += new EventHandler(HostDocumentChanged);
			UpdateDesignerActions(AInterface);
		}

		private void HostDocumentChanged(object ASender, EventArgs AArgs)
		{
			UpdateDesignerActions((IFormInterface)((IHost)ASender).Children[0]);
		}

		private void UpdateDesignerActions(IFormInterface AInterface)
		{
			IWindowsFormInterface LForm = (IWindowsFormInterface)AInterface;

			LForm.ClearCustomActions();

			DocumentExpression LExpression = new DocumentExpression(AInterface.HostNode.Document);

			if (LExpression.Type != DocumentType.None)
				LForm.AddCustomAction
				(
					Strings.Get("CustomizeMenuText"), 
					LoadBitmap("Alphora.Dataphor.Dataphoria.Images.Customize.bmp"), 
					new FormInterfaceHandler(CustomizeForm)
				);

			LForm.AddCustomAction
			(
				Strings.Get("EditCopyMenuText"), 
				LoadBitmap("Alphora.Dataphor.Dataphoria.Images.EditCopy.bmp"), 
				new FormInterfaceHandler(EditCopyForm)
			);

			if (LExpression.Type == DocumentType.Document)
			{
                LForm.AddCustomAction
				(
					Strings.Get("EditMenuText"),  
					LoadBitmap("Alphora.Dataphor.Dataphoria.Images.Edit.bmp"), 
					new FormInterfaceHandler(EditForm)
				);
			}
		}

		private void CheckExclusiveDesigner(IFormInterface AInterface)
		{
			if (FDesignedForms[AInterface] != null) 
				throw new DataphoriaException(DataphoriaException.Codes.SingleDesigner);
		}

		public void AddDesignerForm(IFormInterface AInterface, IDesigner ADesigner)
		{
			FDesignedForms.Add(AInterface, ADesigner);
			ADesigner.Disposed += new EventHandler(DesignerDisposed);
		}

		/// <summary> Gets a new document expression for a given expression string. </summary>
		public static DocumentExpression GetDocumentExpression(string AExpression)
		{
			DocumentExpression LExpression = new DocumentExpression(AExpression);
			if (LExpression.Type != DocumentType.Document)
				throw new DataphoriaException(DataphoriaException.Codes.CanOnlyLiveEditDocuments);
			return LExpression;
		}

		private void EditForm(IFormInterface AInterface)
		{
			CheckExclusiveDesigner(AInterface);

			DocumentExpression LExpression = GetDocumentExpression(AInterface.HostNode.Document);
			string LDocumentType;
			using 
			(
				DAE.Runtime.Data.DataValue LDocumentTypeValue = 
					EvaluateQuery
					(
						String.Format
						(
							".Frontend.GetDocumentType('{0}', '{1}')", 
							LExpression.DocumentArgs.LibraryName, 
							LExpression.DocumentArgs.DocumentName
						)
					)
			)
			{
				LDocumentType = LDocumentTypeValue.AsString;
			}

			ILiveDesigner LDesigner;
			switch (LDocumentType)
			{
				case "dfd" : LDesigner = new FormDesigner.FormDesigner(this, "DFD"); break;
				case "dfdx" : LDesigner = new FormDesigner.CustomFormDesigner(this, "DFDX"); break;
				default : throw new DataphoriaException(DataphoriaException.Codes.DocumentTypeLiveEditNotSupported, LDocumentType);
			}
			try
			{
				LDesigner.Open(AInterface.HostNode);
				((IDesigner)LDesigner).Show();
				
				AddDesignerForm(AInterface, LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void EditCopyForm(IFormInterface AInterface)
		{
			CheckExclusiveDesigner(AInterface);

			FormDesigner.FormDesigner LDesigner = new FormDesigner.FormDesigner(this, "DFD");
			try
			{
				LDesigner.New(AInterface.HostNode);
				((IDesigner)LDesigner).Show();
				
				AddDesignerForm(AInterface, LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void CustomizeForm(IFormInterface AInterface)
		{
			CheckExclusiveDesigner(AInterface);

			FormDesigner.CustomFormDesigner LDesigner = new FormDesigner.CustomFormDesigner(this, "DFDX");
			try
			{
				LDesigner.New(AInterface.HostNode);
				((IDesigner)LDesigner).Show();
				
				AddDesignerForm(AInterface, LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void DesignerDisposed(object ASender, EventArgs AArgs)
		{
			// Remove the designer once it is closed
			IDictionaryEnumerator LEnumerator = FDesignedForms.GetEnumerator();
			while (LEnumerator.MoveNext())
				if (LEnumerator.Value == ASender)
				{
					FDesignedForms.Remove(LEnumerator.Key);
					break;
				}
		}

		#endregion

		#region Designer support

		private Hashtable FDesigners = new Hashtable();
		
		public void CheckNotRegistered(DesignBuffer ABuffer)
		{
			if (FDesigners[ABuffer] != null)
				throw new DataphoriaException(DataphoriaException.Codes.AlreadyDesigning, ABuffer.GetDescription());
		}

		public void RegisterDesigner(DesignBuffer ABuffer, IDesigner ADesigner)
		{
			CheckNotRegistered(ABuffer);
			FDesigners.Add(ABuffer, ADesigner);
		}

		public void UnregisterDesigner(DesignBuffer ABuffer)
		{
			FDesigners.Remove(ABuffer);
		}

		public IDesigner GetDesigner(DesignBuffer ABuffer)
		{
			return (IDesigner)FDesigners[ABuffer];
		}

		/// <summary> Opens up a new query window against the specified server. </summary>
		/// <returns> The newly created script editor. </returns>
		public IDesigner NewDesigner()
		{
			DesignerInfo LInfo = new DesignerInfo();
			Frontend.Client.Windows.IWindowsFormInterface LForm = FrontendSession.LoadForm(null, String.Format(".Frontend.Derive('Designers adorn {{ ClassName tags {{ Frontend.Browse.Visible = ''false'' }} }} tags {{ Frontend.Caption = ''{0}'' }}')", Strings.Get("SelectDesigner")));
			try
			{
				if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
				LInfo.ID = LForm.MainSource.DataView.Fields["Main.ID"].AsString;
				LInfo.ClassName = LForm.MainSource.DataView.Fields["Main.ClassName"].AsString;
			}
			finally
			{
				LForm.HostNode.Dispose();
			}

			return OpenDesigner(LInfo, null);
		}

		/// <summary> Determine the default designer for the specified document type ID. </summary>
		public DesignerInfo GetDefaultDesigner(string ADocumentTypeID)
		{
			DAE.IServerCursor LCursor = OpenCursor(String.Format("DocumentTypeDefaultDesigners where DocumentType_ID = '{0}' join Designers by ID = Default_Designer_ID over {{ ID, ClassName }}", ADocumentTypeID));
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					if (!LCursor.Next())
						throw new DataphoriaException(DataphoriaException.Codes.NoDefaultDesignerForDocumentType, ADocumentTypeID);
					LCursor.Select(LRow);
					DesignerInfo LResult = new DesignerInfo();
					LResult.ID = LRow["ID"].AsString;
					LResult.ClassName = LRow["ClassName"].AsString;
					return LResult;
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		/// <summary> Allow the user to choose from the designers associated with the specified document type ID. </summary>
		public DesignerInfo ChooseDesigner(string ADocumentTypeID)
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = 
				FrontendSession.LoadForm
				(
					null,
					String.Format
					(
						@"	
							Derive
							(
								'	
									Frontend.DocumentTypeDesigners 
										where DocumentType_ID = ''{0}'' 
										adorn {{ DocumentType_ID {{ default ''{0}'' }} }} 
										remove {{ DocumentType_ID }} 
										join (Frontend.Designers rename {{ ID Designer_ID }}) 
										adorn {{ ClassName tags {{ Frontend.Browse.Visible = ''false'' }} }}
								'
							)
						",
						ADocumentTypeID
					)
				);
			try
			{
				LForm.Text = Strings.Get("SelectDesigner");
				if (LForm.MainSource.DataView.IsEmpty())
					throw new DataphoriaException(DataphoriaException.Codes.NoDesignersForDocumentType, ADocumentTypeID);
				if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
				DesignerInfo LResult = new DesignerInfo();
				LResult.ID = LForm.MainSource.DataView.Fields["Main.Designer_ID"].AsString;
				LResult.ClassName = LForm.MainSource.DataView.Fields["Main.ClassName"].AsString;
				return LResult;
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		public IDesigner OpenDesigner(DesignerInfo AInfo, DesignBuffer ABuffer)
		{
			IDesigner LDesigner = (IDesigner)Activator.CreateInstance(Type.GetType(AInfo.ClassName, true), new object[] {this, AInfo.ID});
			try
			{
				if (ABuffer != null)
					LDesigner.Open(ABuffer);
				else
					LDesigner.New();
				LDesigner.Show();
				return LDesigner;
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		/// <summary> Instantiates a new D4 text editor with the specified initial content. </summary>
		public TextEditor.TextEditor NewEditor(string AText, string ADocumentTypeID)
		{
			TextEditor.TextEditor LEditor = (TextEditor.TextEditor)OpenDesigner(GetDefaultDesigner(ADocumentTypeID), null);
			try
			{
				LEditor.New();
				LEditor.EditorText = AText;
				LEditor.Service.SetModified(false);
				((IDesigner)LEditor).Show();	// IDesigner.Show is distinct from Control.Show
				return LEditor;
			}
			catch
			{
				LEditor.Dispose();
				throw;
			}
		}

		/// <summary> Evaluates a D4 expression that returns a D4 document, and shows the document in an editor. </summary>
		public TextEditor.TextEditor EvaluateAndEdit(string AExpression, string ADocumentTypeID)
		{
			using (DAE.Runtime.Data.DataValue LScript = EvaluateQuery(AExpression))
			{
				return NewEditor(LScript.AsString, ADocumentTypeID);
			}
		}

		// DesignBuffers

		private void SetInsertOpenState(Frontend.Client.IFormInterface AForm)
		{
			AForm.MainSource.OpenState = DAE.Client.DataSetState.Insert;
		}

		public DocumentDesignBuffer PromptForDocumentBuffer(IDesigner ADesigner, string ADefaultLibraryName, string ADefaultDocumentName)
		{
			ExecuteScript
			(
				String.Format
				(
					@"	create session view FilteredDocumentTypes
							DocumentTypeDesigners where Designer_ID = '{0}' over {{ DocumentType_ID }}
								join (DocumentTypes rename {{ ID DocumentType_ID }})
							tags {{ Frontend.Title = 'Document Types' }};
						create session table NewDocument in System.Temp from Documents where false;
						create session reference NewDocument_Libraries NewDocument {{ Library_Name }} references Libraries {{ Name }} tags {{ Frontend.Detail.Visible = 'false', Frontend.Lookup.Title = 'Library' }};
						create session reference NewDocument_FilteredDocumentTypes NewDocument {{ Type_ID }} references FilteredDocumentTypes {{ DocumentType_ID }} tags {{ Frontend.Detail.Visible = 'false', Frontend.Lookup.Title = 'Document Type' }};
					",
					ADesigner.DesignerID
				)
			);
			try
			{
				Frontend.Client.Windows.IWindowsFormInterface LForm = FrontendSession.LoadForm(null, ".Frontend.Derive('NewDocument', 'Add')", new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
				try
				{
					LForm.Text = Strings.Get("SaveAsDocumentFormTitle");
					LForm.MainSource.DataView.Edit();
					if (ADefaultLibraryName != String.Empty)
						LForm.MainSource.DataView["Main.Library_Name"].AsString = ADefaultLibraryName;
					if (ADefaultDocumentName != String.Empty)
						LForm.MainSource.DataView["Main.Name"].AsString = ADefaultDocumentName;
					LForm.MainSource.DataView["Main.Type_ID"].AsString = GetDefaultDocumentType(ADesigner);
					LForm.MainSource.DataView.OnValidate += new EventHandler(SaveFormValidate);

					if (LForm.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
						throw new AbortException();

					DocumentDesignBuffer LBuffer = 
						new DocumentDesignBuffer
						(
							this,
							LForm.MainSource.DataView["Main.Library_Name"].AsString,
							LForm.MainSource.DataView["Main.Name"].AsString
						);
					LBuffer.DocumentType = LForm.MainSource.DataView["Main.Type_ID"].AsString;
					return LBuffer;
				}
				finally
				{
					LForm.HostNode.Dispose();
				}
			}
			finally
			{
				ExecuteScript
				(
					@"	drop reference NewDocument_FilteredDocumentTypes;
						drop reference NewDocument_Libraries;
						drop table NewDocument;
						drop view FilteredDocumentTypes;
					"
				);

			}
		}

		private void SaveFormValidate(object ASender, EventArgs AArgs)
		{
			DAE.Client.DataView LView = (DAE.Client.DataView)ASender;
			CheckDocumentOverwrite(LView["Main.Library_Name"].AsString, LView["Main.Name"].AsString);
		}

		public FileDesignBuffer PromptForFileBuffer(IDesigner ADesigner, string ADefaultFileName)
		{
			using (SaveFileDialog LDialog = new SaveFileDialog())
			{
				LDialog.InitialDirectory = (string)FSettings.GetSetting("Dataphoria.SaveDirectory", typeof(string), ".");
				LDialog.Filter = GetSaveFilter(ADesigner);
				LDialog.FilterIndex = 0;
				LDialog.RestoreDirectory = false;
				LDialog.Title = Strings.Get("SaveDialogTitle");
				LDialog.AddExtension = true;
				if (ADefaultFileName != String.Empty)
					LDialog.DefaultExt = Path.GetExtension(ADefaultFileName);
				else
				{
					string LDefaultDocumentType = GetDefaultDocumentType(ADesigner);
					if (LDefaultDocumentType.Length != 0)
						LDialog.DefaultExt = "." + LDefaultDocumentType;
					else
					{
						LDialog.DefaultExt = String.Empty;
						LDialog.AddExtension = false;
					}
				}
				LDialog.CheckPathExists = true;
				LDialog.OverwritePrompt = true;

				if (LDialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				FSettings.SetSetting("Dataphoria.SaveDirectory", Path.GetDirectoryName(LDialog.FileName));

				return new FileDesignBuffer(this, LDialog.FileName);
			}
		}

		public void SaveAll()
		{
			IDesigner LDesigner;
			foreach (Form LForm in FTabbedMDIManager.MdiChildren)
			{
				LDesigner = LForm as IDesigner;
				if ((LDesigner != null) && LDesigner.Service.IsModified)
					LDesigner.Service.Save();
			}
		}

		#endregion

		#region IServiceProvider Members

		private Hashtable FServices = new Hashtable();
		public Hashtable Services { get { return FServices; } }

		public new virtual object GetService(Type AServiceType)
		{
			object LResult = base.GetService(AServiceType);
			if (LResult != null)
				return LResult;
			else
				return FServices[AServiceType];
		}

		#endregion

		#region File Support

		public static string DocumentTypeFromFileName(string AFileName)
		{
			string LResult = Path.GetExtension(AFileName).ToLower();
			if (LResult.Length > 0)
				LResult = LResult.Substring(1);
			return LResult;
		}

		private string GetOpenFilter()
		{
			StringBuilder LFilter = new StringBuilder();
			LFilter.Append(Strings.Get("AllFilesFilter"));
			DAE.IServerCursor LCursor = OpenCursor("DocumentTypes over { ID, Description }");
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						LFilter.AppendFormat("|{1} (*.{0})|*.{0}", LRow["ID"].AsString, LRow["Description"].AsString);
					}
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				LCursor.Plan.Close(LCursor);
			}
			return LFilter.ToString();
		}

		private string GetSaveFilter(IDesigner ADesigner)
		{
			StringBuilder LFilter = new StringBuilder();
			DAE.IServerCursor LCursor = 
				OpenCursor
				(
					String.Format
					(
						"DocumentTypeDesigners where Designer_ID = '{0}' join DocumentTypes by ID = DocumentType_ID over {{ ID, Description }}",
						ADesigner.DesignerID
					)
				);
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						if (LFilter.Length > 0)
							LFilter.Append('|');
						LFilter.AppendFormat("{1} (*.{0})|*.{0}", LRow["ID"].AsString, LRow["Description"].AsString);
					}
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				LCursor.Plan.Close(LCursor);
			}
			if (LFilter.Length > 0)
				LFilter.Append('|');
			LFilter.Append(Strings.Get("AllFilesFilter"));
			return LFilter.ToString();
		}

		private string[] FileOpenPrompt(bool AAllowMultiple)
		{
			using (OpenFileDialog LDialog = new OpenFileDialog())
			{
				LDialog.InitialDirectory = (string)FSettings.GetSetting("Dataphoria.OpenDirectory", typeof(string), ".");
				LDialog.Filter = GetOpenFilter();
				LDialog.FilterIndex = 0;
				LDialog.RestoreDirectory = false;
				LDialog.Title = Strings.Get("FileOpenTitle");
				LDialog.Multiselect = AAllowMultiple;
				LDialog.CheckFileExists = true;

				if (LDialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				FSettings.SetSetting("Dataphoria.OpenDirectory", Path.GetDirectoryName(LDialog.FileName));

				return LDialog.FileNames;
			}
		}

		public void OpenFiles(string[] AFileNames)
		{
			string LFileName;
			FileDesignBuffer LBuffer;

			for (int i = 0; i < AFileNames.Length; i++)
			{
				LFileName = AFileNames[i];
				DesignerInfo LInfo = GetDefaultDesigner(DocumentTypeFromFileName(LFileName));
				LBuffer = new FileDesignBuffer(this, LFileName);
				try
				{
					OpenDesigner
					(
						LInfo, 
						LBuffer
					);
				}
				catch (Exception LException)
				{
					HandleException(LException);
				}
			}
		}

		public void OpenFile()
		{
			OpenFiles(FileOpenPrompt(true));
		}

		public void OpenFileWith()
		{
			string[] LFileNames = FileOpenPrompt(false);
			string LFileName = LFileNames[0];

			DesignerInfo LInfo = ChooseDesigner(DocumentTypeFromFileName(LFileName));

			FileDesignBuffer LBuffer = new FileDesignBuffer(this, LFileName);

			OpenDesigner(LInfo, LBuffer);
		}
		
		public void SaveCatalog()
		{
			ExecuteScript("SaveCatalog();");
		}

		public void BackupCatalog()
		{
			ExecuteScript("BackupCatalog();");
		}
		
		public void UpgradeLibraries()
		{
			ExecuteScript("UpgradeLibraries();");
		}

		#endregion

		#region Child Forms

		private TabbedMDIManager FTabbedMDIManager;

		public void AttachForm(Form AForm) 
		{
			AForm.MdiParent = this;
			AForm.Show();
		}

		private void CloseChildren()
		{
			Form[] LForms = FTabbedMDIManager.MdiChildren;
			foreach (Form LForm in LForms)
				LForm.Close();
		}

		private void DisposeChildren()
		{
			Form[] LForms = FTabbedMDIManager.MdiChildren;
			foreach (Form LForm in LForms)
				LForm.Dispose();
		}

		private IStatusBarClient FCurrentStatusBarClient;
		
		protected override void OnMdiChildActivate(EventArgs AArgs)
		{
			base.OnMdiChildActivate(AArgs);
			if (FCurrentStatusBarClient != ActiveMdiChild)
			{
				SuspendLayout();
				try
				{
					if (FCurrentStatusBarClient != null)
					{
						FCurrentStatusBarClient.Unmerge(FStatusBar);
						FCurrentStatusBarClient = null;
					}
					IStatusBarClient LClient = ActiveMdiChild as IStatusBarClient;
					if (LClient != null)
					{
						LClient.Merge(FStatusBar);
						FCurrentStatusBarClient = LClient;
					}
				}
				finally
				{
					ResumeLayout(true);
				}
			}
		}

		#endregion

		#region DAE & Frontend Server Helpers

		// these two methods should be moved to serverconnection or even higher
		public void ExecuteScript(string AScript)
		{
			ExecuteScript(AScript, null);
		}

		/// <summary> Executes a string on the dataphor server. </summary>
		public void ExecuteScript(string AScript, DAE.Runtime.DataParams AParams)
		{
			if (AScript != String.Empty)
			{
				Cursor LOldCursor = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				try
				{
					DAE.IServerScript LScript = FUtilityProcess.PrepareScript(AScript);
					try
					{
						LScript.Execute(AParams);
					}
					finally
					{
						FUtilityProcess.UnprepareScript(LScript);
					}
				}
				finally
				{
					Cursor.Current = LOldCursor;
				}
			}
		}

		public DAE.IServerCursor OpenCursor(string AQuery)
		{
			return OpenCursor(AQuery, null);
		}

		public DAE.IServerCursor OpenCursor(string AQuery, DAE.Runtime.DataParams AParams)
		{
			Cursor LOldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				DAE.IServerExpressionPlan LPlan = FUtilityProcess.PrepareExpression(AQuery, AParams);
				try
				{
					return LPlan.Open(AParams);
				}
				catch
				{
					FUtilityProcess.UnprepareExpression(LPlan);
					throw;
				}
			}
			finally
			{
				Cursor.Current = LOldCursor;
			}
		}

		public void CloseCursor(DAE.IServerCursor ACursor)
		{
			DAE.IServerExpressionPlan LPlan = ACursor.Plan;
			LPlan.Close(ACursor);
			FUtilityProcess.UnprepareExpression(LPlan);
		}

		public DAE.Runtime.Data.DataValue EvaluateQuery(string AQuery)
		{
			return EvaluateQuery(AQuery, null);
		}

		public DAE.Runtime.Data.DataValue EvaluateQuery(string AQuery, DAE.Runtime.DataParams AParams)
		{
			Cursor LOldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				DAE.IServerExpressionPlan LPlan = FUtilityProcess.PrepareExpression(AQuery, AParams);
				DAE.Runtime.Data.DataValue LResult;
				try
				{
					LResult = LPlan.Evaluate(AParams);
				}
				finally
				{
					FUtilityProcess.UnprepareExpression(LPlan);                
				}
				return LResult;
			}
			finally
			{
				Cursor.Current = LOldCursor;
			}
		}

		public bool DocumentExists(string ALibraryName, string ADocumentName)
		{
			using 
			(
				DAE.Runtime.Data.DataValue LDocumentExistsData = 
					EvaluateQuery
					(
						String.Format
						(
							@".Frontend.DocumentExists('{0}', '{1}')",
							DAE.Schema.Object.EnsureRooted(ALibraryName),
							DAE.Schema.Object.EnsureRooted(ADocumentName)
						)
					)
			)
			{
				return LDocumentExistsData.AsBoolean;
			}
		}

		public void CheckDocumentOverwrite(string ALibraryName, string ADocumentName)
		{
			if (DocumentExists(ALibraryName, ADocumentName))
				if 
				(
					MessageBox.Show
					(
						String.Format
						(
							Strings.Get("SaveAsDialogReplaceText"),
							ALibraryName,
							ADocumentName
						), 
						Strings.Get("SaveAsDocumentFormTitle"), 
						MessageBoxButtons.YesNo, 
						MessageBoxIcon.Warning, 
						MessageBoxDefaultButton.Button1
					) != DialogResult.Yes
				)
					throw new AbortException();
		}

		public void EnsureFrontendRegistered()
		{
			ExecuteScript
			(
				@"
					begin
						var LLibraryName := LibraryName();
						try
							System.EnsureLibraryRegistered('Frontend');
							System.UpgradeLibrary('Frontend');
						finally
							SetLibrary(LLibraryName);
						end;
					end;
				"
			);
		}
		
		public void EnsureSecurityRegistered()
		{
			ExecuteScript
			(
				@"
					begin
						var LLibraryName := LibraryName();
						try
							System.EnsureLibraryRegistered('Security');
						finally
							SetLibrary(LLibraryName);
						end;
					end;
				"
			);
		}

		public string GetCurrentLibraryName()
		{
			using (DAE.Runtime.Data.DataValue LScalar = EvaluateQuery("LibraryName()"))
			{
				return LScalar.AsDisplayString;
			}
		}

		public string GetDefaultDocumentType(IDesigner ADesigner)
		{
			using 
			(
				DAE.Runtime.Data.DataValue LDefaultTypeData = 
					EvaluateQuery
					(
						String.Format
						(
							@".Frontend.GetDefaultDocumentType('{0}')",
							ADesigner.DesignerID
						)
					)
			)
			{
				return LDefaultTypeData.AsString;
			}
		}

		#endregion

		#region Warnings

		/// <summary> Provides access to the warnings / errors list pane. </summary>
		public ErrorListView Warnings
		{
			get { return FErrorListView; }
		}

		private void ShowWarnings()
		{
			FDockingManager.SetDockVisibility(FErrorListView, true);
			FDockingManager.ActivateControl(FErrorListView);
		}

		private void WarningsAdded(object ASender, EventArgs AArgs)
		{
			ShowWarnings();
		}

		private void ErrorsAdded(object ASender, EventArgs AArgs)
		{
			ShowWarnings();
			FDockingManager.ActivateControl(FErrorListView);
		}

		private void ClearWarnings()
		{
			FErrorListView.ClearErrors();
		}

		// IErrorSource

		void IErrorSource.ErrorHighlighted(Exception AException)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception AException)
		{
			// nothing
		}

		#endregion

		#region Commands

		public event EventHandler OnFormDesignerLibrariesChanged;

		private void BrowseDesignerLibraries()
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = FrontendSession.LoadForm(null, ".Frontend.Derive('FormDesignerLibraries')");
			try
			{
				LForm.ShowModal(Frontend.Client.FormMode.None);
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
			if (OnFormDesignerLibrariesChanged != null)
				OnFormDesignerLibrariesChanged(this, EventArgs.Empty);
		}

		private void BrowseDocumentTypes()
		{
			IWindowsFormInterface LForm = FrontendSession.LoadForm(null, ".Frontend.Derive('DocumentTypes')", null);
			try
			{
				LForm.ShowModal(Frontend.Client.FormMode.None);
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		private void Documentation()
		{
			string LFileName = GetHelpFileName();
			try 
			{
				System.Diagnostics.Process.Start(LFileName);
			}
			catch (Exception LException)
			{
				HandleException(new DataphoriaException(DataphoriaException.Codes.UnableToOpenHelp, LException, LFileName));
			}
		}

		private void About()
		{
			using (AboutForm LAboutForm = new AboutForm())
			{
				LAboutForm.ShowDialog(this);
			}
		}

		private void LaunchForm()
		{
			Frontend.Client.Windows.Session LSession = GetLiveDesignableFrontendSession();
			try
			{
				LSession.SetFormDesigner();
				Frontend.Client.Windows.IWindowsFormInterface LForm = LSession.LoadForm(null, ".Frontend.Form('.Frontend', 'DerivedFormLauncher')");
				try
				{
					LForm.Show();
				}
				catch
				{
					LForm.HostNode.Dispose();
					throw;
				}
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}

		public void ShowDataphorExplorer()
		{
			FDockingManager.SetDockVisibility(FExplorer, true);
			FDockingManager.ActivateControl(FExplorer);
		}

		public void LaunchAlphoraWebsite()
		{
			System.Diagnostics.Process.Start(@"http://www.alphora.com/");
		}

		public void LaunchWebDocumentation()
		{
			System.Diagnostics.Process.Start(@"http://docs.alphora.com/");
		}

		public void LaunchAlphoraGroups()
		{
			System.Diagnostics.Process.Start(@"http://news.alphora.com/");
		}

		private void FrameBarManagerItemClicked(object ASender, BarItemClickedEventArgs AArgs)
		{
			try
			{
				switch (AArgs.ClickedBarItem.ID)
				{
					case "ClearWarnings" : ClearWarnings(); break;
					case "Connect" : EnsureServerConnection(); break;
					case "Disconnect" : Disconnect(); break;
					case "New" : NewDesigner(); break;
					case "NewScript" : NewEditor(String.Empty, "d4"); break;
					case "Open" : OpenFile(); break;
					case "OpenWith" : OpenFileWith(); break;
					case "SaveAll" : SaveAll(); break;
					case "LaunchForm" : LaunchForm(); break;
					case "Exit" : Close(); break;
					case "ShowExplorer" : ShowDataphorExplorer(); break;
					case "ShowWarnings" : ShowWarnings(); break;
					case "DesignerLibraries" : BrowseDesignerLibraries(); break;
					case "Documentation" : Documentation(); break;
					case "About" : About(); break;
					case "Website" : LaunchAlphoraWebsite(); break;
					case "WebDocumentation" : LaunchWebDocumentation(); break;
					case "Groups" : LaunchAlphoraGroups(); break;
					case "DocumentTypes" : BrowseDocumentTypes(); break;
				}
			}
			catch (Exception LException)
			{
				HandleException(LException);
			}
		}

		private void ClearWarningsClicked(object sender, System.EventArgs e)
		{
			ClearWarnings();
		}

		#endregion
		
		#region Help

		public const string CDefaultHelpFileName = @"..\Documentation\Dataphor.chm";

		private string GetHelpFileName()
		{
			return Path.Combine(Application.StartupPath, (string)FSettings.GetSetting("HelpFileName", typeof(string), CDefaultHelpFileName));
		}

		public void InvokeHelp(string AKeyword)
		{
			Help.ShowHelp(null, GetHelpFileName(), HelpNavigator.KeywordIndex, AKeyword.Trim());
		}

		protected override void OnHelpRequested(HelpEventArgs AArgs)
		{
			base.OnHelpRequested(AArgs);
			InvokeHelp("Dataphoria");
		}

		private void FErrorListView_HelpRequested(object ASender, HelpEventArgs AArgs)
		{
			string LKeyword = "Errors and Warnings";
			DataphorException LException = FErrorListView.SelectedError as DataphorException;
			if (LException != null)
				LKeyword = LException.Code.ToString();

			InvokeHelp(LKeyword);
		}

		private void FExplorer_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			InvokeHelp("Dataphor Explorer");
		}

		#endregion

		#region Main Static (Thread exception handling)

		private static Dataphoria FDataphoriaInstance;
		public static Dataphoria DataphoriaInstance
		{
			get { return FDataphoriaInstance; }
		}

		/// <summary> The main entry point for the application. </summary>
		[STAThread]
		static void Main(string[] AArgs) 
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainException);
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, true);
			Application.EnableVisualStyles();

			Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
			try
			{
				try
				{
					FDataphoriaInstance = new Dataphoria();
					try
					{
						FDataphoriaInstance.OpenFiles(AArgs);
						Application.Run(FDataphoriaInstance);
					}
					finally
					{
						FDataphoriaInstance.Dispose();
						FDataphoriaInstance = null;
					}
				}
				catch (Exception LException)
				{
					HandleException(LException);
				}
			}
			finally
			{
				Application.ThreadException -= new ThreadExceptionEventHandler(ThreadException);
			}
		}

		static void AppDomainException(object sender, UnhandledExceptionEventArgs e)
		{
		}

		protected static void ThreadException(object ASender, ThreadExceptionEventArgs AArgs)
		{
			HandleException(AArgs.Exception);
		}

		public static void HandleException(System.Exception AException)
		{
			if (AException is ThreadAbortException)
				Thread.ResetAbort();
			Frontend.Client.Windows.Session.HandleException(AException);
		}

		#endregion
	}

	public interface IStatusBarClient
	{
		void Merge(Control AStatusBar);
		void Unmerge(Control AStatusBar);
	}
}
