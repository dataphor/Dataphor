/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using SD = ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client.Windows;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> Text Editor form for Dataphoria. </summary>
    public class TextEditor : BaseForm, IDesigner, IChildFormWithToolBar
	{
		private System.ComponentModel.IContainer components = null;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.ChildFrameBarManager FFrameBarManager;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FFileMenu;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAsFile;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAsDocument;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCloseMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FPrintMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCutMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCopyMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FPasteMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FFindMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FReplaceMenuItem;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FFindNext;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FUndo;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FRedo;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSplit;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.Bar FMainMenu;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.Bar FFileBar;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.Bar FEditBar;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FEditMenu;
		
		private System.Windows.Forms.ImageList FToolbarImageList;
		protected Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSelectAllMenuItem;
        protected Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FViewMenu;
        protected WeifenLuo.WinFormsUI.Docking.DockPanel FDockPanel;
        protected DockContent FDockContentTextEdit;
        private ToolStrip FToolStrip;
        private MenuStrip FMenuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem FSaveToolStripMenuItem;
        private ToolStripMenuItem FSaveAsFileToolStripMenuItem;
        private ToolStripMenuItem FSaveAsDocumentToolStripMenuItem;
        private ToolStripMenuItem FCloseToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem FPrintToolStripMenuItem;
        private ToolStripMenuItem FEditToolStripMenuItem;
        private ToolStripMenuItem FUndoToolStripMenuItem;
        private ToolStripMenuItem FRedoToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem FCutToolStripMenuItem;
        private ToolStripMenuItem FCopyToolStripMenuItem;
        private ToolStripMenuItem FPasteToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem FSelectAllToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem4;
        private ToolStripMenuItem FFindToolStripMenuItem;
        private ToolStripMenuItem FReplaceToolStripMenuItem;
        private ToolStripMenuItem FFindNextToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripButton FSaveToolStripButton;
        private ToolStripButton FSaveAsFileToolStripButton;
        private ToolStripButton FSaveAsDocumentToolStripButton;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton FPrintToolStripButton;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton FCutToolStripButton;
        private ToolStripButton FCopyToolStripButton;
        private ToolStripButton FPasteToolStripButton;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton FFindToolStripButton;
        private ToolStripButton FReplaceToolStripButton;
        private ToolStripButton FFindNextToolStripButton;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton FUndoToolStripButton;
        private ToolStripButton FRedoToolStripButton;
        private ToolStripMenuItem FSplitWindowToolStripMenuItem;

		protected Alphora.Dataphor.Frontend.Client.Windows.TextEdit FTextEdit;

		public TextEditor()	// for the designer
		{
			InitializeComponent();
            InitializeDocking();
		}

		public TextEditor(IDataphoria ADataphoria, string ADesignerID)
		{
			InitializeComponent();

            InitializeDocking();

            FDesignerID = ADesignerID;

            FService = new DesignService(ADataphoria, this);
            FService.OnModifiedChanged += new EventHandler(NameOrModifiedChanged);
            FService.OnNameChanged += new EventHandler(NameOrModifiedChanged);
            FService.OnRequestLoad += new RequestHandler(LoadData);
            FService.OnRequestSave += new RequestHandler(SaveData);

            FTextEdit.HelpRequested += new HelpEventHandler(FTextArea_HelpRequested);

            FTextEdit.Document.HighlightingStrategy = GetHighlightingStrategy();
            FTextEdit.Document.DocumentChanged += new SD.Document.DocumentEventHandler(DocumentChanged);
            TextEditInitialized(FTextEdit, FTextEdit.ActiveTextAreaControl);
            FTextEdit.OnInitializeTextAreaControl += new InitializeTextAreaControlHandler(TextEditInitialized);
            FTextEdit.BeginningFind += new EventHandler(BeginningFind);
            FTextEdit.ReplacementsPerformed += new ReplacementsPerformedHandler(ReplacementsPerformed);
            FTextEdit.TextNotFound += new EventHandler(TextNotFound);
			//FEditorPanel.Controls.Add(FTextEdit);

			UpdateLineNumberStatus();
			UpdateTitle();
		}

        private void InitializeDocking()
        {
            // 
            // FTextEdit
            // 
            this.FTextEdit = new Alphora.Dataphor.Frontend.Client.Windows.TextEdit();
            this.FTextEdit.CausesValidation = false;
            this.FTextEdit.EnableFolding = false;
            this.FTextEdit.IndentStyle = ICSharpCode.TextEditor.Document.IndentStyle.Auto;
            this.FTextEdit.Location = new System.Drawing.Point(0, 0);
            this.FTextEdit.Name = "FTextEdit";
            this.FTextEdit.ShowInvalidLines = false;
            this.FTextEdit.ShowLineNumbers = false;
            this.FTextEdit.ShowVRuler = true;
            this.FTextEdit.Size = new System.Drawing.Size(455, 376);
            this.FTextEdit.TabIndent = 3;

            this.FTextEdit.VRulerRow = 100;
            this.FTextEdit.Dock = DockStyle.Fill;

            FDockContentTextEdit = new DockContent();
            FDockContentTextEdit.Controls.Add(FTextEdit);
            FDockContentTextEdit.ShowHint = DockState.Document;
            FDockContentTextEdit.Show(FDockPanel);

          
        }

		/// <summary> Clean up any resources being used. </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextEditor));
            this.FFrameBarManager = new Syncfusion.Windows.Forms.Tools.XPMenus.ChildFrameBarManager(this);
            this.FMainMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "MainMenu");
            this.FFileMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FSaveMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FSaveAsFile = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FSaveAsDocument = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FCloseMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FPrintMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FEditMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FUndo = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FRedo = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FCutMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FCopyMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FPasteMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FSelectAllMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FFindMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FReplaceMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FFindNext = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FViewMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FSplit = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FFileBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "FileBar");
            this.FEditBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "EditBar");
            this.FToolbarImageList = new System.Windows.Forms.ImageList(this.components);
            this.FDockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.FMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FSaveAsFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FSaveAsDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FCloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.FPrintToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FEditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FUndoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FRedoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.FCutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FCopyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FPasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.FSelectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.FFindToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FReplaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FFindNextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FSplitWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FToolStrip = new System.Windows.Forms.ToolStrip();
            this.FSaveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FSaveAsFileToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FSaveAsDocumentToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.FPrintToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.FCutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FCopyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FPasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.FFindToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FReplaceToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FFindNextToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.FUndoToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FRedoToolStripButton = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).BeginInit();
            this.FMenuStrip.SuspendLayout();
            this.FToolStrip.SuspendLayout();
            this.SuspendLayout();
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
            this.FFrameBarManager.FormName = "Text Editor";
            this.FFrameBarManager.ImageList = this.FToolbarImageList;
            this.FFrameBarManager.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FFileMenu,
            this.FSaveMenuItem,
            this.FSaveAsFile,
            this.FSaveAsDocument,
            this.FCloseMenuItem,
            this.FPrintMenuItem,
            this.FEditMenu,
            this.FCutMenuItem,
            this.FCopyMenuItem,
            this.FPasteMenuItem,
            this.FFindMenuItem,
            this.FReplaceMenuItem,
            this.FFindNext,
            this.FUndo,
            this.FRedo,
            this.FSplit,
            this.FSelectAllMenuItem,
            this.FViewMenu});
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
            this.FEditMenu,
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
            this.FSaveAsDocument,
            this.FCloseMenuItem,
            this.FPrintMenuItem});
            this.FFileMenu.MergeOrder = 1;
            this.FFileMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FFileMenu.SeparatorIndices.AddRange(new int[] {
            4});
            this.FFileMenu.Text = "&File";
            // 
            // FSaveMenuItem
            // 
            this.FSaveMenuItem.CategoryIndex = 0;
            this.FSaveMenuItem.ID = "Save";
            this.FSaveMenuItem.ImageIndex = 3;
            this.FSaveMenuItem.MergeOrder = 20;
            this.FSaveMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
            this.FSaveMenuItem.Text = "&Save";
            // 
            // FSaveAsFile
            // 
            this.FSaveAsFile.CategoryIndex = 0;
            this.FSaveAsFile.ID = "SaveAsFile";
            this.FSaveAsFile.ImageIndex = 10;
            this.FSaveAsFile.MergeOrder = 20;
            this.FSaveAsFile.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftF;
            this.FSaveAsFile.Text = "Save As &File...";
            // 
            // FSaveAsDocument
            // 
            this.FSaveAsDocument.CategoryIndex = 0;
            this.FSaveAsDocument.ID = "SaveAsDocument";
            this.FSaveAsDocument.ImageIndex = 11;
            this.FSaveAsDocument.MergeOrder = 20;
            this.FSaveAsDocument.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftD;
            this.FSaveAsDocument.Text = "Save As &Document...";
            // 
            // FCloseMenuItem
            // 
            this.FCloseMenuItem.CategoryIndex = 0;
            this.FCloseMenuItem.ID = "Close";
            this.FCloseMenuItem.ImageIndex = 12;
            this.FCloseMenuItem.MergeOrder = 20;
            this.FCloseMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF4;
            this.FCloseMenuItem.Text = "&Close";
            // 
            // FPrintMenuItem
            // 
            this.FPrintMenuItem.CategoryIndex = 0;
            this.FPrintMenuItem.ID = "Print";
            this.FPrintMenuItem.ImageIndex = 8;
            this.FPrintMenuItem.MergeOrder = 30;
            this.FPrintMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlP;
            this.FPrintMenuItem.Text = "&Print";
            // 
            // FEditMenu
            // 
            this.FEditMenu.CategoryIndex = 1;
            this.FEditMenu.ID = "Edit";
            this.FEditMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FUndo,
            this.FRedo,
            this.FCutMenuItem,
            this.FCopyMenuItem,
            this.FPasteMenuItem,
            this.FSelectAllMenuItem,
            this.FFindMenuItem,
            this.FReplaceMenuItem,
            this.FFindNext});
            this.FEditMenu.MergeOrder = 5;
            this.FEditMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FEditMenu.SeparatorIndices.AddRange(new int[] {
            2,
            5,
            6});
            this.FEditMenu.Text = "&Edit";
            // 
            // FUndo
            // 
            this.FUndo.CategoryIndex = 1;
            this.FUndo.ID = "Undo";
            this.FUndo.ImageIndex = 6;
            this.FUndo.MergeOrder = 10;
            this.FUndo.ShortcutText = "Ctrl+Z";
            this.FUndo.Text = "&Undo";
            // 
            // FRedo
            // 
            this.FRedo.CategoryIndex = 1;
            this.FRedo.ID = "Redo";
            this.FRedo.ImageIndex = 7;
            this.FRedo.MergeOrder = 10;
            this.FRedo.ShortcutText = "Ctrl+Y";
            this.FRedo.Text = "&Redo";
            // 
            // FCutMenuItem
            // 
            this.FCutMenuItem.CategoryIndex = 1;
            this.FCutMenuItem.ID = "Cut";
            this.FCutMenuItem.ImageIndex = 0;
            this.FCutMenuItem.MergeOrder = 10;
            this.FCutMenuItem.ShortcutText = "Ctrl+X";
            this.FCutMenuItem.Text = "C&ut";
            // 
            // FCopyMenuItem
            // 
            this.FCopyMenuItem.CategoryIndex = 1;
            this.FCopyMenuItem.ID = "Copy";
            this.FCopyMenuItem.ImageIndex = 1;
            this.FCopyMenuItem.MergeOrder = 10;
            this.FCopyMenuItem.ShortcutText = "Ctrl+C";
            this.FCopyMenuItem.Text = "&Copy";
            // 
            // FPasteMenuItem
            // 
            this.FPasteMenuItem.CategoryIndex = 1;
            this.FPasteMenuItem.ID = "Paste";
            this.FPasteMenuItem.ImageIndex = 2;
            this.FPasteMenuItem.MergeOrder = 10;
            this.FPasteMenuItem.ShortcutText = "Ctrl+V";
            this.FPasteMenuItem.Text = "&Paste";
            // 
            // FSelectAllMenuItem
            // 
            this.FSelectAllMenuItem.CategoryIndex = 1;
            this.FSelectAllMenuItem.ID = "SelectAll";
            this.FSelectAllMenuItem.ImageIndex = 14;
            this.FSelectAllMenuItem.MergeOrder = 15;
            this.FSelectAllMenuItem.ShortcutText = "Ctrl+A";
            this.FSelectAllMenuItem.Text = "Select &All";
            // 
            // FFindMenuItem
            // 
            this.FFindMenuItem.CategoryIndex = 1;
            this.FFindMenuItem.ID = "Find";
            this.FFindMenuItem.ImageIndex = 4;
            this.FFindMenuItem.MergeOrder = 20;
            this.FFindMenuItem.ShortcutText = "Ctrl+F";
            this.FFindMenuItem.Text = "&Find...";
            // 
            // FReplaceMenuItem
            // 
            this.FReplaceMenuItem.CategoryIndex = 1;
            this.FReplaceMenuItem.ID = "Replace";
            this.FReplaceMenuItem.ImageIndex = 9;
            this.FReplaceMenuItem.MergeOrder = 20;
            this.FReplaceMenuItem.ShortcutText = "Ctrl+H";
            this.FReplaceMenuItem.Text = "&Replace...";
            // 
            // FFindNext
            // 
            this.FFindNext.CategoryIndex = 1;
            this.FFindNext.ID = "FindNext";
            this.FFindNext.ImageIndex = 5;
            this.FFindNext.MergeOrder = 20;
            this.FFindNext.ShortcutText = "F3";
            this.FFindNext.Text = "Find &Next";
            // 
            // FViewMenu
            // 
            this.FViewMenu.CategoryIndex = 2;
            this.FViewMenu.ID = "View";
            this.FViewMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FSplit});
            this.FViewMenu.MergeOrder = 10;
            this.FViewMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FViewMenu.Text = "&View";
            // 
            // FSplit
            // 
            this.FSplit.CategoryIndex = 2;
            this.FSplit.ID = "Split";
            this.FSplit.ImageIndex = 13;
            this.FSplit.MergeOrder = 140;
            this.FSplit.Shortcut = System.Windows.Forms.Shortcut.CtrlShift1;
            this.FSplit.ShortcutText = "Ctrl+Shift+1";
            this.FSplit.Text = "&Split Window";
            // 
            // FFileBar
            // 
            this.FFileBar.BarName = "FileBar";
            this.FFileBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FSaveMenuItem,
            this.FSaveAsFile,
            this.FSaveAsDocument,
            this.FPrintMenuItem});
            this.FFileBar.Manager = this.FFrameBarManager;
            this.FFileBar.SeparatorIndices.AddRange(new int[] {
            3});
            // 
            // FEditBar
            // 
            this.FEditBar.BarName = "EditBar";
            this.FEditBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FCutMenuItem,
            this.FCopyMenuItem,
            this.FPasteMenuItem,
            this.FFindMenuItem,
            this.FReplaceMenuItem,
            this.FFindNext,
            this.FUndo,
            this.FRedo});
            this.FEditBar.Manager = this.FFrameBarManager;
            this.FEditBar.SeparatorIndices.AddRange(new int[] {
            3,
            6});
            // 
            // FToolbarImageList
            // 
            this.FToolbarImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FToolbarImageList.ImageStream")));
            this.FToolbarImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.FToolbarImageList.Images.SetKeyName(0, "");
            this.FToolbarImageList.Images.SetKeyName(1, "");
            this.FToolbarImageList.Images.SetKeyName(2, "");
            this.FToolbarImageList.Images.SetKeyName(3, "");
            this.FToolbarImageList.Images.SetKeyName(4, "");
            this.FToolbarImageList.Images.SetKeyName(5, "");
            this.FToolbarImageList.Images.SetKeyName(6, "");
            this.FToolbarImageList.Images.SetKeyName(7, "");
            this.FToolbarImageList.Images.SetKeyName(8, "");
            this.FToolbarImageList.Images.SetKeyName(9, "");
            this.FToolbarImageList.Images.SetKeyName(10, "");
            this.FToolbarImageList.Images.SetKeyName(11, "");
            this.FToolbarImageList.Images.SetKeyName(12, "");
            this.FToolbarImageList.Images.SetKeyName(13, "");
            this.FToolbarImageList.Images.SetKeyName(14, "");
            // 
            // FDockPanel
            // 
            this.FDockPanel.ActiveAutoHideContent = null;
            this.FDockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FDockPanel.DocumentStyle = WeifenLuo.WinFormsUI.Docking.DocumentStyle.DockingSdi;
            this.FDockPanel.Location = new System.Drawing.Point(0, 0);
            this.FDockPanel.Name = "FDockPanel";
            this.FDockPanel.Size = new System.Drawing.Size(455, 376);
            this.FDockPanel.TabIndex = 4;
            // 
            // FMenuStrip
            // 
            this.FMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.FEditToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.FMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.FMenuStrip.Name = "FMenuStrip";
            this.FMenuStrip.Size = new System.Drawing.Size(455, 24);
            this.FMenuStrip.TabIndex = 9;
            this.FMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSaveToolStripMenuItem,
            this.FSaveAsFileToolStripMenuItem,
            this.FSaveAsDocumentToolStripMenuItem,
            this.FCloseToolStripMenuItem,
            this.toolStripMenuItem1,
            this.FPrintToolStripMenuItem});
            this.fileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // FSaveToolStripMenuItem
            // 
            this.FSaveToolStripMenuItem.Name = "FSaveToolStripMenuItem";
            this.FSaveToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FSaveToolStripMenuItem.Text = "Save";
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
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(170, 6);
            // 
            // FPrintToolStripMenuItem
            // 
            this.FPrintToolStripMenuItem.Name = "FPrintToolStripMenuItem";
            this.FPrintToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.FPrintToolStripMenuItem.Text = "Print";
            // 
            // FEditToolStripMenuItem
            // 
            this.FEditToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FUndoToolStripMenuItem,
            this.FRedoToolStripMenuItem,
            this.toolStripMenuItem2,
            this.FCutToolStripMenuItem,
            this.FCopyToolStripMenuItem,
            this.FPasteToolStripMenuItem,
            this.toolStripMenuItem3,
            this.FSelectAllToolStripMenuItem,
            this.toolStripMenuItem4,
            this.FFindToolStripMenuItem,
            this.FReplaceToolStripMenuItem,
            this.FFindNextToolStripMenuItem});
            this.FEditToolStripMenuItem.Name = "FEditToolStripMenuItem";
            this.FEditToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.FEditToolStripMenuItem.Text = "Edit";
            // 
            // FUndoToolStripMenuItem
            // 
            this.FUndoToolStripMenuItem.Name = "FUndoToolStripMenuItem";
            this.FUndoToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FUndoToolStripMenuItem.Text = "Undo";
            // 
            // FRedoToolStripMenuItem
            // 
            this.FRedoToolStripMenuItem.Name = "FRedoToolStripMenuItem";
            this.FRedoToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FRedoToolStripMenuItem.Text = "Redo";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(121, 6);
            // 
            // FCutToolStripMenuItem
            // 
            this.FCutToolStripMenuItem.Name = "FCutToolStripMenuItem";
            this.FCutToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FCutToolStripMenuItem.Text = "Cut";
            // 
            // FCopyToolStripMenuItem
            // 
            this.FCopyToolStripMenuItem.Name = "FCopyToolStripMenuItem";
            this.FCopyToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FCopyToolStripMenuItem.Text = "Copy";
            // 
            // FPasteToolStripMenuItem
            // 
            this.FPasteToolStripMenuItem.Name = "FPasteToolStripMenuItem";
            this.FPasteToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FPasteToolStripMenuItem.Text = "Paste";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(121, 6);
            // 
            // FSelectAllToolStripMenuItem
            // 
            this.FSelectAllToolStripMenuItem.Name = "FSelectAllToolStripMenuItem";
            this.FSelectAllToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FSelectAllToolStripMenuItem.Text = "Select All";
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(121, 6);
            // 
            // FFindToolStripMenuItem
            // 
            this.FFindToolStripMenuItem.Name = "FFindToolStripMenuItem";
            this.FFindToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FFindToolStripMenuItem.Text = "Find";
            // 
            // FReplaceToolStripMenuItem
            // 
            this.FReplaceToolStripMenuItem.Name = "FReplaceToolStripMenuItem";
            this.FReplaceToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FReplaceToolStripMenuItem.Text = "Replace";
            // 
            // FFindNextToolStripMenuItem
            // 
            this.FFindNextToolStripMenuItem.Name = "FFindNextToolStripMenuItem";
            this.FFindNextToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.FFindNextToolStripMenuItem.Text = "Find Next";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSplitWindowToolStripMenuItem});
            this.viewToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // FSplitWindowToolStripMenuItem
            // 
            this.FSplitWindowToolStripMenuItem.Name = "FSplitWindowToolStripMenuItem";
            this.FSplitWindowToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.FSplitWindowToolStripMenuItem.Text = "Split Window";
            // 
            // FToolStrip
            // 
            this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSaveToolStripButton,
            this.FSaveAsFileToolStripButton,
            this.FSaveAsDocumentToolStripButton,
            this.toolStripSeparator1,
            this.FPrintToolStripButton,
            this.toolStripSeparator2,
            this.FCutToolStripButton,
            this.FCopyToolStripButton,
            this.FPasteToolStripButton,
            this.toolStripSeparator3,
            this.FFindToolStripButton,
            this.FReplaceToolStripButton,
            this.FFindNextToolStripButton,
            this.toolStripSeparator4,
            this.FUndoToolStripButton,
            this.FRedoToolStripButton});
            this.FToolStrip.Location = new System.Drawing.Point(0, 24);
            this.FToolStrip.Name = "FToolStrip";
            this.FToolStrip.Size = new System.Drawing.Size(455, 25);
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // FPrintToolStripButton
            // 
            this.FPrintToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FPrintToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FPrintToolStripButton.Image")));
            this.FPrintToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FPrintToolStripButton.Name = "FPrintToolStripButton";
            this.FPrintToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FPrintToolStripButton.Text = "toolStripButton4";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // FCutToolStripButton
            // 
            this.FCutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FCutToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FCutToolStripButton.Image")));
            this.FCutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FCutToolStripButton.Name = "FCutToolStripButton";
            this.FCutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FCutToolStripButton.Text = "toolStripButton5";
            // 
            // FCopyToolStripButton
            // 
            this.FCopyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FCopyToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FCopyToolStripButton.Image")));
            this.FCopyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FCopyToolStripButton.Name = "FCopyToolStripButton";
            this.FCopyToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FCopyToolStripButton.Text = "toolStripButton6";
            // 
            // FPasteToolStripButton
            // 
            this.FPasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FPasteToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FPasteToolStripButton.Image")));
            this.FPasteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FPasteToolStripButton.Name = "FPasteToolStripButton";
            this.FPasteToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FPasteToolStripButton.Text = "toolStripButton7";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // FFindToolStripButton
            // 
            this.FFindToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FFindToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FFindToolStripButton.Image")));
            this.FFindToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FFindToolStripButton.Name = "FFindToolStripButton";
            this.FFindToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FFindToolStripButton.Text = "toolStripButton8";
            // 
            // FReplaceToolStripButton
            // 
            this.FReplaceToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FReplaceToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FReplaceToolStripButton.Image")));
            this.FReplaceToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FReplaceToolStripButton.Name = "FReplaceToolStripButton";
            this.FReplaceToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FReplaceToolStripButton.Text = "toolStripButton9";
            // 
            // FFindNextToolStripButton
            // 
            this.FFindNextToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FFindNextToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FFindNextToolStripButton.Image")));
            this.FFindNextToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FFindNextToolStripButton.Name = "FFindNextToolStripButton";
            this.FFindNextToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FFindNextToolStripButton.Text = "toolStripButton10";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // FUndoToolStripButton
            // 
            this.FUndoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FUndoToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FUndoToolStripButton.Image")));
            this.FUndoToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FUndoToolStripButton.Name = "FUndoToolStripButton";
            this.FUndoToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FUndoToolStripButton.Text = "toolStripButton11";
            // 
            // FRedoToolStripButton
            // 
            this.FRedoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FRedoToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("FRedoToolStripButton.Image")));
            this.FRedoToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FRedoToolStripButton.Name = "FRedoToolStripButton";
            this.FRedoToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FRedoToolStripButton.Text = "toolStripButton12";
            // 
            // TextEditor
            // 
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(455, 376);
            this.Controls.Add(this.FToolStrip);
            this.Controls.Add(this.FMenuStrip);
            this.Controls.Add(this.FDockPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TextEditor";
            this.TabText = "Untitled";
            this.Text = "Untitled";
            ((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).EndInit();
            this.FMenuStrip.ResumeLayout(false);
            this.FMenuStrip.PerformLayout();
            this.FToolStrip.ResumeLayout(false);
            this.FToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		protected virtual SD.Document.IHighlightingStrategy GetHighlightingStrategy()
		{
			switch (FDesignerID)
			{
				case "XML" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("XML");
				case "CS" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("CS");
				case "VB" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("VBNET");
				default : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("Default");
			}
		}

		private void TextEditInitialized(object ASender, ICSharpCode.TextEditor.TextAreaControl ANewControl)
		{
			ANewControl.TextArea.Caret.PositionChanged += new EventHandler(CaretPositionChanged);
			ANewControl.TextArea.Enter += new EventHandler(TextAreaEnter);
		}

		// Service

		private IDesignService FService;
		[Browsable(false)]
		public IDesignService Service
		{
			get { return FService; }
		}

		// Dataphoria

		protected IDataphoria Dataphoria
		{
			get { return (FService == null ? null : FService.Dataphoria); }
		}

		// DataSession

		protected DAE.Client.DataSession DataSession
		{
			get { return FService.Dataphoria.DataSession; }
		}

		// IDesigner

		public void Open(DesignBuffer ABuffer)
		{
			FService.Open(ABuffer);
		}

		public void New()
		{
			FService.New();
		}

		/// <remarks> 
		///		Note that this method should not be confused with Form.Close().  
		///		Be sure to deal with a compile-time instance of type IDesigner 
		///		to invoke this method. 
		///	</remarks>
		void IDesigner.Show()
		{
			Dataphoria.AttachForm(this);
		}

		private string FDesignerID;
		[Browsable(false)]
		public string DesignerID
		{
			get { return FDesignerID; }
		}

		public bool CloseSafely()
		{
			Close();
			return IsDisposed;
		}

		// EditorText

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string EditorText
		{
			get { return FTextEdit.Document.TextContent; }
			set { FTextEdit.SetText(value); }
		}

		// Title

		private void UpdateTitle()
		{
			Text = FService.GetDescription() + (FService.IsModified ? "*" : "");
		}

		// Service callbacks

		private void NameOrModifiedChanged(object ASender, EventArgs AArgs)
		{
			UpdateTitle();
		}

		private void LoadData(DesignService AService, DesignBuffer ABuffer)
		{
			EditorText = ABuffer.LoadData();
		}

		private void SaveData(DesignService AService, DesignBuffer ABuffer)
		{
			ABuffer.SaveData(EditorText);
		}

		// Status bar

		private void TextAreaEnter(object sender, EventArgs e)
		{
			UpdateLineNumberStatus();
		}

		private void CaretPositionChanged(object sender, EventArgs e)
		{
			UpdateLineNumberStatus();
		}

		private void UpdateLineNumberStatus()
		{
			FPositionStatus.Text = FTextEdit.GetLineNumberText();
		}

		// Form events

		public void DocumentChanged(object ASender, SD.Document.DocumentEventArgs AArgs) 
		{
			FService.SetModified(true);
			SetStatus(String.Empty);
		}

		private void Print()
		{
			using (PrintDialog LDialog = new PrintDialog()) 
			{
				LDialog.Document  = FTextEdit.PrintDocument;
				LDialog.AllowSomePages = true;
				if (LDialog.ShowDialog() == DialogResult.OK) 
					FTextEdit.PrintDocument.Print();
			}
		}
		
		private void Undo()
		{
			FTextEdit.Undo();
		}

		private void Redo()
		{
			FTextEdit.Redo();
		}

		private void Split()
		{
			FTextEdit.Split();
		}

		protected void BeginningFind(object sender, EventArgs e)
		{
			SetStatus(String.Empty);
		}

		protected void ReplacementsPerformed(object ASender, int ACount)
		{
			SetStatus(String.Format(Strings.ReplacementsPerformed, ACount));
		}

		protected void TextNotFound(object sender, EventArgs e)
		{
			SetStatus(Strings.FindTextNotFound);
		}

		private void Replace()
		{
			FTextEdit.PromptFindReplace(true);
		}

		private void Find()
		{
			FTextEdit.PromptFindReplace(false);
		}

		private void FindNext()
		{
			try
			{
				SetStatus(String.Empty);
				FTextEdit.FindAgain();
			}
			catch (Exception LException)
			{
				SetStatus(LException.Message);
				// Don't rethrow
			}
		}

		protected override void OnActivated(EventArgs e)
		{
			ActiveControl = FTextEdit;  // always focus on the editor control
			base.OnActivated (e);
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			FService.CheckModified();
		}

		private void ShowSaved()
		{
			SetStatus(Strings.Saved);
		}

		private void FrameBarManagerItemClicked(object ASender, Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventArgs AArgs)
		{
			try
			{
				switch (AArgs.ClickedBarItem.ID)
				{
					case "Close" : Close(); break;
					case "Print" : Print(); break;
					case "Cut" : new SD.Actions.Cut().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "Copy" : new SD.Actions.Copy().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "Paste" : new SD.Actions.Paste().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "SelectAll" : new SD.Actions.SelectWholeDocument().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "SaveAsDocument" : 			
						FService.SaveAsDocument();
						ShowSaved();
						break;
					case "SaveAsFile" :
						FService.SaveAsFile();
						ShowSaved();
						break;
					case "Save" :
						FService.Save();
						ShowSaved();
						break;
					case "Find" : Find(); break;
					case "Replace" : Replace(); break;
					case "FindNext" : FindNext(); break;
					case "Undo" : Undo(); break;
					case "Redo" : Redo(); break;
					case "Split" : Split(); break;
				}
			}
			catch (AbortException)
			{
				// nothing
			}
		}

		private void FTextArea_HelpRequested(object ASender, HelpEventArgs AArgs)
		{
			if (FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				Dataphoria.InvokeHelp(FTextEdit.ActiveTextAreaControl.SelectionManager.SelectedText);
			else
			{
				string LWord = "";
				// the next 7 lines added/modified to overcome bug in FindWordStart
				// runtime error if Caret is at end of document
				int LCaretOffset = FTextEdit.ActiveTextAreaControl.Caret.Offset;
				int LTextLength = FTextEdit.Document.TextLength;
				if (LTextLength > 0)
				{
					if (LCaretOffset == LTextLength)
						LCaretOffset--;
					int LFirst = TextEdit.FindWordStart(FTextEdit.Document, LCaretOffset);
					int LLast = TextEdit.FindWordEnd(FTextEdit.Document, LCaretOffset);
					LWord = FTextEdit.Document.GetText(LFirst, LLast - LFirst);
				}
				if (LWord.Trim().Length == 0)
					Dataphoria.InvokeHelp(DesignerID);
				else
					Dataphoria.InvokeHelp(LWord);
			}
		}

		#region StatusBar

		protected Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel FPositionStatus;

		protected override void InitializeStatusBar()
		{
			base.InitializeStatusBar();

			FPositionStatus = new Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel();
			FPositionStatus.Text = "0:0";
			FPositionStatus.SizeToContent = true;
			FPositionStatus.HAlign = Syncfusion.Windows.Forms.Tools.HorzFlowAlign.Right;
			FPositionStatus.Alignment = HorizontalAlignment.Center;
			FPositionStatus.BorderStyle = BorderStyle.FixedSingle;
			FPositionStatus.BorderColor = Color.Gray;
		}

		protected override void DisposeStatusBar()
		{
			FPositionStatus.Dispose();
			FPositionStatus = null;

			base.DisposeStatusBar();
		}

		public override void Merge(Control AStatusBar)
		{
			base.Merge(AStatusBar);

			AStatusBar.Controls.Add(FPositionStatus);
		}

		public override void Unmerge(Control AStatusBar)
		{
			base.Unmerge(AStatusBar);

			AStatusBar.Controls.Remove(FPositionStatus);
		}

		#endregion

        #region IChildFormWithToolBar Members

        public void MergeWith(ToolStrip AParentToolStrip)
        {
            ToolStripManager.Merge(this.FToolStrip, AParentToolStrip);
        }

        #endregion
    }
}
