/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt

	Assumption: this designer assumes that its lifetime is for a single document.  It will 
	 currently not close any existing document before operations such a New(), Open().  This
	 behavior is okay for now because Dataphoria does not ask designers to change documents.
*/

using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
	// Don't put any definitions above the FormDesiger class

    partial class FormDesigner 
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ImageList ToolBarImageList;

        private System.Windows.Forms.ImageList FPointerImageList;
        private System.Windows.Forms.PropertyGrid FPropertyGrid;

        protected Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree.DesignerTree FNodesTree;
        private Alphora.Dataphor.Dataphoria.FormDesigner.FormPanel FFormPanel;
        private WeifenLuo.WinFormsUI.Docking.DockPanel FDockPanel;
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
			this.FSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
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
			// FDockPanel
			// 
			this.FDockPanel.ActiveAutoHideContent = null;
			this.FDockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FDockPanel.DockBottomPortion = 0.4;
			this.FDockPanel.DockLeftPortion = 210;
			this.FDockPanel.DockRightPortion = 220;
			this.FDockPanel.DockTopPortion = 220;
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
			this.FMenuStrip.Visible = false;
			// 
			// FFileToolStripMenuItem
			// 
			this.FFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSaveToolStripMenuItem,
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
			this.FSaveAsFileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FSaveAsFileToolStripMenuItem.MergeIndex = 8;
			this.FSaveAsFileToolStripMenuItem.Name = "FSaveAsFileToolStripMenuItem";
			this.FSaveAsFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.F)));
			this.FSaveAsFileToolStripMenuItem.Size = new System.Drawing.Size(247, 22);
			this.FSaveAsFileToolStripMenuItem.Text = "Save As File";
			this.FSaveAsFileToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsDocumentToolStripMenuItem
			// 
			this.FSaveAsDocumentToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
			this.FSaveAsDocumentToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FSaveAsDocumentToolStripMenuItem.MergeIndex = 9;
			this.FSaveAsDocumentToolStripMenuItem.Name = "FSaveAsDocumentToolStripMenuItem";
			this.FSaveAsDocumentToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.D)));
			this.FSaveAsDocumentToolStripMenuItem.Size = new System.Drawing.Size(247, 22);
			this.FSaveAsDocumentToolStripMenuItem.Text = "Save As Document";
			this.FSaveAsDocumentToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FCloseToolStripMenuItem
			// 
			this.FCloseToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Close;
			this.FCloseToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FCloseToolStripMenuItem.MergeIndex = 12;
			this.FCloseToolStripMenuItem.Name = "FCloseToolStripMenuItem";
			this.FCloseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F4)));
			this.FCloseToolStripMenuItem.Size = new System.Drawing.Size(247, 22);
			this.FCloseToolStripMenuItem.Text = "Close";
			this.FCloseToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FEditToolStripMenuItem
			// 
			this.FEditToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FCutToolStripMenuItem,
            this.FCopyToolStripMenuItem,
            this.FPasteToolStripMenuItem,
            this.FDeleteToolStripMenuItem,
            this.FRenameToolStripMenuItem});
			this.FEditToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FEditToolStripMenuItem.MergeIndex = 1;
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
			this.FCutToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FCopyToolStripMenuItem
			// 
			this.FCopyToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
			this.FCopyToolStripMenuItem.Name = "FCopyToolStripMenuItem";
			this.FCopyToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.FCopyToolStripMenuItem.Text = "Copy";
			this.FCopyToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FPasteToolStripMenuItem
			// 
			this.FPasteToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Paste;
			this.FPasteToolStripMenuItem.Name = "FPasteToolStripMenuItem";
			this.FPasteToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.FPasteToolStripMenuItem.Text = "Paste";
			this.FPasteToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FDeleteToolStripMenuItem
			// 
			this.FDeleteToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Delete;
			this.FDeleteToolStripMenuItem.Name = "FDeleteToolStripMenuItem";
			this.FDeleteToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.FDeleteToolStripMenuItem.Text = "Delete";
			this.FDeleteToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FRenameToolStripMenuItem
			// 
			this.FRenameToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Rename;
			this.FRenameToolStripMenuItem.Name = "FRenameToolStripMenuItem";
			this.FRenameToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.FRenameToolStripMenuItem.Text = "Rename";
			this.FRenameToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FViewToolStripMenuItem
			// 
			this.FViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
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
			this.FPaletteToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FPropertiesToolStripMenuItem
			// 
			this.FPropertiesToolStripMenuItem.Name = "FPropertiesToolStripMenuItem";
			this.FPropertiesToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
			this.FPropertiesToolStripMenuItem.Text = "Properties";
			this.FPropertiesToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FFormToolStripMenuItem
			// 
			this.FFormToolStripMenuItem.Name = "FFormToolStripMenuItem";
			this.FFormToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
			this.FFormToolStripMenuItem.Text = "Form";
			this.FFormToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
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
			this.FToolStrip.Visible = false;
			// 
			// FSaveToolStripButton
			// 
			this.FSaveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSaveToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			this.FSaveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSaveToolStripButton.Name = "FSaveToolStripButton";
			this.FSaveToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FSaveToolStripButton.Text = "toolStripButton1";
			this.FSaveToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsFileToolStripButton
			// 
			this.FSaveAsFileToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSaveAsFileToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
			this.FSaveAsFileToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSaveAsFileToolStripButton.Name = "FSaveAsFileToolStripButton";
			this.FSaveAsFileToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FSaveAsFileToolStripButton.Text = "toolStripButton2";
			this.FSaveAsFileToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveAsDocumentToolStripButton
			// 
			this.FSaveAsDocumentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSaveAsDocumentToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
			this.FSaveAsDocumentToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSaveAsDocumentToolStripButton.Name = "FSaveAsDocumentToolStripButton";
			this.FSaveAsDocumentToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FSaveAsDocumentToolStripButton.Text = "toolStripButton3";
			this.FSaveAsDocumentToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
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
			this.FCutToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FCopyToolStripButton
			// 
			this.FCopyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FCopyToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
			this.FCopyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FCopyToolStripButton.Name = "FCopyToolStripButton";
			this.FCopyToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FCopyToolStripButton.Text = "toolStripButton2";
			this.FCopyToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FPasteToolStripButton
			// 
			this.FPasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FPasteToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Paste;
			this.FPasteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FPasteToolStripButton.Name = "FPasteToolStripButton";
			this.FPasteToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FPasteToolStripButton.Text = "toolStripButton3";
			this.FPasteToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FDeleteToolStripButton
			// 
			this.FDeleteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FDeleteToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Delete;
			this.FDeleteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FDeleteToolStripButton.Name = "FDeleteToolStripButton";
			this.FDeleteToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FDeleteToolStripButton.Text = "toolStripButton4";
			this.FDeleteToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FRenameToolStripButton
			// 
			this.FRenameToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FRenameToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Rename;
			this.FRenameToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FRenameToolStripButton.Name = "FRenameToolStripButton";
			this.FRenameToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.FRenameToolStripButton.Text = "toolStripButton1";
			this.FRenameToolStripButton.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// FSaveToolStripMenuItem
			// 
			this.FSaveToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			this.FSaveToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FSaveToolStripMenuItem.MergeIndex = 8;
			this.FSaveToolStripMenuItem.Name = "FSaveToolStripMenuItem";
			this.FSaveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.FSaveToolStripMenuItem.Size = new System.Drawing.Size(247, 22);
			this.FSaveToolStripMenuItem.Text = "Save";
			this.FSaveToolStripMenuItem.Click += new System.EventHandler(this.FMainMenuStrip_ItemClicked);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
			// 
			// FormDesigner
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(687, 518);
			this.Controls.Add(this.FMenuStrip);
			this.Controls.Add(this.FToolStrip);
			this.Controls.Add(this.FDockPanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "FormDesigner";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.FMenuStrip.ResumeLayout(false);
			this.FMenuStrip.PerformLayout();
			this.FToolStrip.ResumeLayout(false);
			this.FToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }
        #endregion

		private ToolStripMenuItem FSaveToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem1;


	}
}
