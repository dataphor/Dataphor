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
    partial class TextEditor 
	{
		private System.ComponentModel.IContainer components = null;
		
		
		private System.Windows.Forms.ImageList FToolbarImageList;		
        protected WeifenLuo.WinFormsUI.Docking.DockPanel FDockPanel;
        private ToolStripMenuItem FSaveToolStripMenuItem;
        private ToolStripMenuItem FSaveAsFileToolStripMenuItem;
        private ToolStripMenuItem FSaveAsDocumentToolStripMenuItem;
        private ToolStripMenuItem FCloseToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem FPrintToolStripMenuItem;
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
			this.FToolbarImageList = new System.Windows.Forms.ImageList(this.components);
			this.FDockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
			this.FMenuStrip = new System.Windows.Forms.MenuStrip();
			this.FFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FSaveAsFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FSaveAsDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
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
			this.FViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
			this.FMenuStrip.SuspendLayout();
			this.FToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// FBottomToolStripPanel
			// 
			this.FBottomToolStripPanel.Location = new System.Drawing.Point(0, 354);
			this.FBottomToolStripPanel.Size = new System.Drawing.Size(455, 22);
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
			this.FDockPanel.Size = new System.Drawing.Size(455, 354);
			this.FDockPanel.TabIndex = 0;
			// 
			// FMenuStrip
			// 
			this.FMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FFileToolStripMenuItem,
            this.FEditToolStripMenuItem,
            this.FViewToolStripMenuItem});
			this.FMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.FMenuStrip.Name = "FMenuStrip";
			this.FMenuStrip.Size = new System.Drawing.Size(455, 24);
			this.FMenuStrip.TabIndex = 9;
			this.FMenuStrip.Text = "menuStrip1";
			this.FMenuStrip.Visible = false;
			// 
			// FFileToolStripMenuItem
			// 
			this.FFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSaveToolStripMenuItem,
            this.FSaveAsFileToolStripMenuItem,
            this.FSaveAsDocumentToolStripMenuItem,
            this.toolStripMenuItem5,
            this.FCloseToolStripMenuItem,
            this.toolStripMenuItem1,
            this.FPrintToolStripMenuItem});
			this.FFileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			this.FFileToolStripMenuItem.MergeIndex = 10;
			this.FFileToolStripMenuItem.Name = "FFileToolStripMenuItem";
			this.FFileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.FFileToolStripMenuItem.Text = "&File";
			// 
			// FSaveToolStripMenuItem
			// 
			this.FSaveToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			this.FSaveToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FSaveToolStripMenuItem.MergeIndex = 60;
			this.FSaveToolStripMenuItem.Name = "FSaveToolStripMenuItem";
			this.FSaveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.FSaveToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			this.FSaveToolStripMenuItem.Text = "&Save";
			this.FSaveToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsFileToolStripMenuItem
			// 
			this.FSaveAsFileToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
			this.FSaveAsFileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FSaveAsFileToolStripMenuItem.MergeIndex = 60;
			this.FSaveAsFileToolStripMenuItem.Name = "FSaveAsFileToolStripMenuItem";
			this.FSaveAsFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
			this.FSaveAsFileToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			this.FSaveAsFileToolStripMenuItem.Text = "Save As &File...";
			this.FSaveAsFileToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsDocumentToolStripMenuItem
			// 
			this.FSaveAsDocumentToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
			this.FSaveAsDocumentToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FSaveAsDocumentToolStripMenuItem.MergeIndex = 60;
			this.FSaveAsDocumentToolStripMenuItem.Name = "FSaveAsDocumentToolStripMenuItem";
			this.FSaveAsDocumentToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.D)));
			this.FSaveAsDocumentToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			this.FSaveAsDocumentToolStripMenuItem.Text = "Save As &Document...";
			this.FSaveAsDocumentToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.toolStripMenuItem5.MergeIndex = 80;
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(253, 6);
			// 
			// FCloseToolStripMenuItem
			// 
			this.FCloseToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Close;
			this.FCloseToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FCloseToolStripMenuItem.MergeIndex = 80;
			this.FCloseToolStripMenuItem.Name = "FCloseToolStripMenuItem";
			this.FCloseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F4)));
			this.FCloseToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			this.FCloseToolStripMenuItem.Text = "C&lose";
			this.FCloseToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.toolStripMenuItem1.MergeIndex = 100;
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(253, 6);
			// 
			// FPrintToolStripMenuItem
			// 
			this.FPrintToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Print;
			this.FPrintToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FPrintToolStripMenuItem.MergeIndex = 100;
			this.FPrintToolStripMenuItem.Name = "FPrintToolStripMenuItem";
			this.FPrintToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.FPrintToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			this.FPrintToolStripMenuItem.Text = "&Print";
			this.FPrintToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
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
			this.FEditToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FEditToolStripMenuItem.MergeIndex = 1;
			this.FEditToolStripMenuItem.Name = "FEditToolStripMenuItem";
			this.FEditToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.FEditToolStripMenuItem.Text = "&Edit";
			// 
			// FUndoToolStripMenuItem
			// 
			this.FUndoToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Undo;
			this.FUndoToolStripMenuItem.Name = "FUndoToolStripMenuItem";
			this.FUndoToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Z";
			this.FUndoToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FUndoToolStripMenuItem.Text = "&Undo";
			this.FUndoToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FRedoToolStripMenuItem
			// 
			this.FRedoToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Redo;
			this.FRedoToolStripMenuItem.Name = "FRedoToolStripMenuItem";
			this.FRedoToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Y";
			this.FRedoToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FRedoToolStripMenuItem.Text = "&Redo";
			this.FRedoToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(161, 6);
			// 
			// FCutToolStripMenuItem
			// 
			this.FCutToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Cut;
			this.FCutToolStripMenuItem.Name = "FCutToolStripMenuItem";
			this.FCutToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+X";
			this.FCutToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FCutToolStripMenuItem.Text = "C&ut";
			this.FCutToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FCopyToolStripMenuItem
			// 
			this.FCopyToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
			this.FCopyToolStripMenuItem.Name = "FCopyToolStripMenuItem";
			this.FCopyToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.FCopyToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FCopyToolStripMenuItem.Text = "&Copy";
			this.FCopyToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FPasteToolStripMenuItem
			// 
			this.FPasteToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Paste;
			this.FPasteToolStripMenuItem.Name = "FPasteToolStripMenuItem";
			this.FPasteToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
			this.FPasteToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FPasteToolStripMenuItem.Text = "&Paste";
			this.FPasteToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(161, 6);
			// 
			// FSelectAllToolStripMenuItem
			// 
			this.FSelectAllToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SelectAll;
			this.FSelectAllToolStripMenuItem.Name = "FSelectAllToolStripMenuItem";
			this.FSelectAllToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.FSelectAllToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FSelectAllToolStripMenuItem.Text = "&Select All";
			this.FSelectAllToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(161, 6);
			// 
			// FFindToolStripMenuItem
			// 
			this.FFindToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Find;
			this.FFindToolStripMenuItem.Name = "FFindToolStripMenuItem";
			this.FFindToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
			this.FFindToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FFindToolStripMenuItem.Text = "&Find...";
			this.FFindToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FReplaceToolStripMenuItem
			// 
			this.FReplaceToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Replace;
			this.FReplaceToolStripMenuItem.Name = "FReplaceToolStripMenuItem";
			this.FReplaceToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+H";
			this.FReplaceToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FReplaceToolStripMenuItem.Text = "R&eplace";
			this.FReplaceToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FFindNextToolStripMenuItem
			// 
			this.FFindNextToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.FindNext;
			this.FFindNextToolStripMenuItem.Name = "FFindNextToolStripMenuItem";
			this.FFindNextToolStripMenuItem.ShortcutKeyDisplayString = "F3";
			this.FFindNextToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.FFindNextToolStripMenuItem.Text = "Find &Next";
			this.FFindNextToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FViewToolStripMenuItem
			// 
			this.FViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSplitWindowToolStripMenuItem});
			this.FViewToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			this.FViewToolStripMenuItem.MergeIndex = 20;
			this.FViewToolStripMenuItem.Name = "FViewToolStripMenuItem";
			this.FViewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.FViewToolStripMenuItem.Text = "&View";
			// 
			// FSplitWindowToolStripMenuItem
			// 
			this.FSplitWindowToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SplitWindow;
			this.FSplitWindowToolStripMenuItem.Name = "FSplitWindowToolStripMenuItem";
			this.FSplitWindowToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.D1)));
			this.FSplitWindowToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FSplitWindowToolStripMenuItem.Text = "&Split Window";
			this.FSplitWindowToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
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
			this.FToolStrip.Location = new System.Drawing.Point(0, 0);
			this.FToolStrip.Name = "FToolStrip";
			this.FToolStrip.Size = new System.Drawing.Size(455, 25);
			this.FToolStrip.TabIndex = 10;
			this.FToolStrip.Text = "toolStrip1";
			this.FToolStrip.Visible = false;
			// 
			// FSaveToolStripButton
			// 
			this.FSaveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSaveToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			this.FSaveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSaveToolStripButton.Name = "FSaveToolStripButton";
			this.FSaveToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FSaveToolStripButton.Text = "Save";
			this.FSaveToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsFileToolStripButton
			// 
			this.FSaveAsFileToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSaveAsFileToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
			this.FSaveAsFileToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSaveAsFileToolStripButton.Name = "FSaveAsFileToolStripButton";
			this.FSaveAsFileToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FSaveAsFileToolStripButton.Text = "Save as";
			this.FSaveAsFileToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsDocumentToolStripButton
			// 
			this.FSaveAsDocumentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSaveAsDocumentToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
			this.FSaveAsDocumentToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSaveAsDocumentToolStripButton.Name = "FSaveAsDocumentToolStripButton";
			this.FSaveAsDocumentToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FSaveAsDocumentToolStripButton.Text = "Save as document";
			this.FSaveAsDocumentToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// FPrintToolStripButton
			// 
			this.FPrintToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FPrintToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Print;
			this.FPrintToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FPrintToolStripButton.Name = "FPrintToolStripButton";
			this.FPrintToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FPrintToolStripButton.Text = "Print";
			this.FPrintToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// FCutToolStripButton
			// 
			this.FCutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FCutToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Cut;
			this.FCutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FCutToolStripButton.Name = "FCutToolStripButton";
			this.FCutToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FCutToolStripButton.Text = "Cut";
			this.FCutToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FCopyToolStripButton
			// 
			this.FCopyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FCopyToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
			this.FCopyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FCopyToolStripButton.Name = "FCopyToolStripButton";
			this.FCopyToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FCopyToolStripButton.Text = "Copy";
			this.FCopyToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FPasteToolStripButton
			// 
			this.FPasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FPasteToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Paste;
			this.FPasteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FPasteToolStripButton.Name = "FPasteToolStripButton";
			this.FPasteToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FPasteToolStripButton.Text = "Paste";
			this.FPasteToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// FFindToolStripButton
			// 
			this.FFindToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FFindToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Find;
			this.FFindToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FFindToolStripButton.Name = "FFindToolStripButton";
			this.FFindToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FFindToolStripButton.Text = "Find";
			this.FFindToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FReplaceToolStripButton
			// 
			this.FReplaceToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FReplaceToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Replace;
			this.FReplaceToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FReplaceToolStripButton.Name = "FReplaceToolStripButton";
			this.FReplaceToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FReplaceToolStripButton.Text = "Replace";
			this.FReplaceToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FFindNextToolStripButton
			// 
			this.FFindNextToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FFindNextToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.FindNext;
			this.FFindNextToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FFindNextToolStripButton.Name = "FFindNextToolStripButton";
			this.FFindNextToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FFindNextToolStripButton.Text = "Find next";
			this.FFindNextToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
			// 
			// FUndoToolStripButton
			// 
			this.FUndoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FUndoToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Undo;
			this.FUndoToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FUndoToolStripButton.Name = "FUndoToolStripButton";
			this.FUndoToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FUndoToolStripButton.Text = "Undo";
			this.FUndoToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FRedoToolStripButton
			// 
			this.FRedoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FRedoToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Redo;
			this.FRedoToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FRedoToolStripButton.Name = "FRedoToolStripButton";
			this.FRedoToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FRedoToolStripButton.Text = "Redo";
			this.FRedoToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// TextEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(455, 376);
			this.Controls.Add(this.FMenuStrip);
			this.Controls.Add(this.FToolStrip);
			this.Controls.Add(this.FDockPanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "TextEditor";
			this.TabText = "Untitled";
			this.Controls.SetChildIndex(this.FBottomToolStripPanel, 0);
			this.Controls.SetChildIndex(this.FDockPanel, 0);
			this.Controls.SetChildIndex(this.FToolStrip, 0);
			this.Controls.SetChildIndex(this.FMenuStrip, 0);
			this.FMenuStrip.ResumeLayout(false);
			this.FMenuStrip.PerformLayout();
			this.FToolStrip.ResumeLayout(false);
			this.FToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

        protected ToolStrip FToolStrip;
		protected MenuStrip FMenuStrip;
		private ToolStripSeparator toolStripMenuItem5;
		protected ToolStripMenuItem FFileToolStripMenuItem;
		protected ToolStripMenuItem FEditToolStripMenuItem;
		protected ToolStripMenuItem FViewToolStripMenuItem;
    }
}
