namespace Alphora.Dataphor.Dataphoria
{
    partial class Dataphoria
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
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
            System.Windows.Forms.ToolStripMenuItem FDataphorExplorerToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem FWarningsErrorsToolStripMenuItem;
            System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
            System.Windows.Forms.ToolStripMenuItem FClearWarningsToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem FDesignerLibrariesToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem FDocumentTypesToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem FDataphorDocumentationToolStripMenuItem;
            System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
            System.Windows.Forms.ToolStripMenuItem FAlphoraWebSiteToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem FAlphoraDiscussionGroupsToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem FWebDocumentationToolStripMenuItem;
            System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
            System.Windows.Forms.ToolStripMenuItem FAboutToolStripMenuItem;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dataphoria));
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
            this.FDockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.FTopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.FMainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.FFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FConnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDisconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.FNewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FNewScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FOpenFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FOpenFileWithToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.FSaveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.FLaunchFormToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LConfigureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDebugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDebugStopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDebugRunMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDebugPauseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
            this.FViewSessionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FViewProcessesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FViewDebugProcessesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FViewCallStackMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FViewStackMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
            this.FBreakOnExceptionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FBreakOnStartMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDebugStepOverMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FDebugStepIntoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FToolStrip = new System.Windows.Forms.ToolStrip();
            this.FNewToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FNewScriptToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FOpenFileToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FOpenFileWithToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.FLaunchFormToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.FDebugToolStrip = new System.Windows.Forms.ToolStrip();
            this.FDebugStopButton = new System.Windows.Forms.ToolStripButton();
            this.FDebugRunButton = new System.Windows.Forms.ToolStripButton();
            this.FDebugPauseButton = new System.Windows.Forms.ToolStripButton();
            this.FDebugStepIntoButton = new System.Windows.Forms.ToolStripButton();
            this.FDebugStepOverButton = new System.Windows.Forms.ToolStripButton();
            this.FBreakOnExceptionButton = new System.Windows.Forms.ToolStripButton();
            this.FBreakOnStartButton = new System.Windows.Forms.ToolStripButton();
            this.FViewSessionsButton = new System.Windows.Forms.ToolStripButton();
            this.FViewProcessesButton = new System.Windows.Forms.ToolStripButton();
            this.FViewDebugProcessesButton = new System.Windows.Forms.ToolStripButton();
            this.FViewCallStackButton = new System.Windows.Forms.ToolStripButton();
            this.FViewStackButton = new System.Windows.Forms.ToolStripButton();
            this.FBottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.FStatusStrip = new System.Windows.Forms.StatusStrip();
            this.FTreeImageList = new System.Windows.Forms.ImageList(this.components);
            FDataphorExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            FWarningsErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            FClearWarningsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            FDesignerLibrariesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            FDocumentTypesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            FDataphorDocumentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            FAlphoraWebSiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            FAlphoraDiscussionGroupsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            FWebDocumentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            FAboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FTopToolStripPanel.SuspendLayout();
            this.FMainMenuStrip.SuspendLayout();
            this.FToolStrip.SuspendLayout();
            this.FDebugToolStrip.SuspendLayout();
            this.FBottomToolStripPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // FDataphorExplorerToolStripMenuItem
            // 
            FDataphorExplorerToolStripMenuItem.MergeIndex = 10;
            FDataphorExplorerToolStripMenuItem.Name = "FDataphorExplorerToolStripMenuItem";
            FDataphorExplorerToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F12;
            FDataphorExplorerToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            FDataphorExplorerToolStripMenuItem.Text = "&Dataphor Explorer";
            // 
            // FWarningsErrorsToolStripMenuItem
            // 
            FWarningsErrorsToolStripMenuItem.MergeIndex = 20;
            FWarningsErrorsToolStripMenuItem.Name = "FWarningsErrorsToolStripMenuItem";
            FWarningsErrorsToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            FWarningsErrorsToolStripMenuItem.Text = "&Warnings and Errors";
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.MergeIndex = 30;
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(190, 6);
            // 
            // FClearWarningsToolStripMenuItem
            // 
            FClearWarningsToolStripMenuItem.MergeIndex = 40;
            FClearWarningsToolStripMenuItem.Name = "FClearWarningsToolStripMenuItem";
            FClearWarningsToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            FClearWarningsToolStripMenuItem.Text = "&Clear Warnings";
            // 
            // FDesignerLibrariesToolStripMenuItem
            // 
            FDesignerLibrariesToolStripMenuItem.MergeIndex = 10;
            FDesignerLibrariesToolStripMenuItem.Name = "FDesignerLibrariesToolStripMenuItem";
            FDesignerLibrariesToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            FDesignerLibrariesToolStripMenuItem.Text = "&Designer Libraries";
            // 
            // FDocumentTypesToolStripMenuItem
            // 
            FDocumentTypesToolStripMenuItem.MergeIndex = 20;
            FDocumentTypesToolStripMenuItem.Name = "FDocumentTypesToolStripMenuItem";
            FDocumentTypesToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            FDocumentTypesToolStripMenuItem.Text = "D&ocument Types";
            // 
            // FDataphorDocumentationToolStripMenuItem
            // 
            FDataphorDocumentationToolStripMenuItem.Name = "FDataphorDocumentationToolStripMenuItem";
            FDataphorDocumentationToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
            FDataphorDocumentationToolStripMenuItem.Text = "&Dataphor Documentation";
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new System.Drawing.Size(213, 6);
            // 
            // FAlphoraWebSiteToolStripMenuItem
            // 
            FAlphoraWebSiteToolStripMenuItem.Name = "FAlphoraWebSiteToolStripMenuItem";
            FAlphoraWebSiteToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
            FAlphoraWebSiteToolStripMenuItem.Text = "Alphora &WebSite";
            // 
            // FAlphoraDiscussionGroupsToolStripMenuItem
            // 
            FAlphoraDiscussionGroupsToolStripMenuItem.Name = "FAlphoraDiscussionGroupsToolStripMenuItem";
            FAlphoraDiscussionGroupsToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
            FAlphoraDiscussionGroupsToolStripMenuItem.Text = "&Alphora Discussion Groups";
            // 
            // FWebDocumentationToolStripMenuItem
            // 
            FWebDocumentationToolStripMenuItem.Name = "FWebDocumentationToolStripMenuItem";
            FWebDocumentationToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
            FWebDocumentationToolStripMenuItem.Text = "W&eb Documentation";
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new System.Drawing.Size(213, 6);
            // 
            // FAboutToolStripMenuItem
            // 
            FAboutToolStripMenuItem.Name = "FAboutToolStripMenuItem";
            FAboutToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
            FAboutToolStripMenuItem.Text = "&About";
            // 
            // BottomToolStripPanel
            // 
            this.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.BottomToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // TopToolStripPanel
            // 
            this.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.TopToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // RightToolStripPanel
            // 
            this.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.RightToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // LeftToolStripPanel
            // 
            this.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.LeftToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Size = new System.Drawing.Size(664, 458);
            // 
            // FDockPanel
            // 
            this.FDockPanel.ActiveAutoHideContent = null;
            this.FDockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FDockPanel.Location = new System.Drawing.Point(0, 49);
            this.FDockPanel.Name = "FDockPanel";
            this.FDockPanel.Size = new System.Drawing.Size(664, 436);
            this.FDockPanel.TabIndex = 8;
            // 
            // FTopToolStripPanel
            // 
            this.FTopToolStripPanel.Controls.Add(this.FMainMenuStrip);
            this.FTopToolStripPanel.Controls.Add(this.FToolStrip);
            this.FTopToolStripPanel.Controls.Add(this.FDebugToolStrip);
            this.FTopToolStripPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.FTopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.FTopToolStripPanel.Name = "FTopToolStripPanel";
            this.FTopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.FTopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.FTopToolStripPanel.Size = new System.Drawing.Size(664, 49);
            // 
            // FMainMenuStrip
            // 
            this.FMainMenuStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.FMainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FFileToolStripMenuItem,
            this.FViewToolStripMenuItem,
            this.LConfigureToolStripMenuItem,
            this.FDebugToolStripMenuItem,
            this.LHelpToolStripMenuItem});
            this.FMainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.FMainMenuStrip.Name = "FMainMenuStrip";
            this.FMainMenuStrip.Size = new System.Drawing.Size(664, 24);
            this.FMainMenuStrip.TabIndex = 17;
            this.FMainMenuStrip.Text = "menuStrip1";
            // 
            // FFileToolStripMenuItem
            // 
            this.FFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FConnectToolStripMenuItem,
            this.FDisconnectToolStripMenuItem,
            this.toolStripMenuItem1,
            this.FNewToolStripMenuItem,
            this.FNewScriptToolStripMenuItem,
            this.FOpenFileToolStripMenuItem,
            this.FOpenFileWithToolStripMenuItem,
            this.toolStripMenuItem2,
            this.FSaveAllToolStripMenuItem,
            this.toolStripMenuItem3,
            this.FLaunchFormToolStripMenuItem,
            this.FExitToolStripMenuItem});
            this.FFileToolStripMenuItem.MergeIndex = 10;
            this.FFileToolStripMenuItem.Name = "FFileToolStripMenuItem";
            this.FFileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.FFileToolStripMenuItem.Text = "&File";
            this.FFileToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
            // 
            // FConnectToolStripMenuItem
            // 
            this.FConnectToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Connect;
            this.FConnectToolStripMenuItem.MergeIndex = 10;
            this.FConnectToolStripMenuItem.Name = "FConnectToolStripMenuItem";
            this.FConnectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.C)));
            this.FConnectToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FConnectToolStripMenuItem.Text = "&Connect";
            // 
            // FDisconnectToolStripMenuItem
            // 
            this.FDisconnectToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Disconnect;
            this.FDisconnectToolStripMenuItem.MergeIndex = 20;
            this.FDisconnectToolStripMenuItem.Name = "FDisconnectToolStripMenuItem";
            this.FDisconnectToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FDisconnectToolStripMenuItem.Text = "&Disconnect";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.MergeIndex = 30;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(204, 6);
            // 
            // FNewToolStripMenuItem
            // 
            this.FNewToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.New;
            this.FNewToolStripMenuItem.MergeIndex = 40;
            this.FNewToolStripMenuItem.Name = "FNewToolStripMenuItem";
            this.FNewToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.N)));
            this.FNewToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FNewToolStripMenuItem.Text = "&New";
            // 
            // FNewScriptToolStripMenuItem
            // 
            this.FNewScriptToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.NewScript;
            this.FNewScriptToolStripMenuItem.MergeIndex = 50;
            this.FNewScriptToolStripMenuItem.Name = "FNewScriptToolStripMenuItem";
            this.FNewScriptToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.FNewScriptToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FNewScriptToolStripMenuItem.Text = "N&ew Script";
            // 
            // FOpenFileToolStripMenuItem
            // 
            this.FOpenFileToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Open;
            this.FOpenFileToolStripMenuItem.MergeIndex = 60;
            this.FOpenFileToolStripMenuItem.Name = "FOpenFileToolStripMenuItem";
            this.FOpenFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.FOpenFileToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FOpenFileToolStripMenuItem.Text = "&Open File";
            // 
            // FOpenFileWithToolStripMenuItem
            // 
            this.FOpenFileWithToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.OpenWith;
            this.FOpenFileWithToolStripMenuItem.MergeIndex = 70;
            this.FOpenFileWithToolStripMenuItem.Name = "FOpenFileWithToolStripMenuItem";
            this.FOpenFileWithToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.FOpenFileWithToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FOpenFileWithToolStripMenuItem.Text = "Open File &With";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.MergeIndex = 80;
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(204, 6);
            // 
            // FSaveAllToolStripMenuItem
            // 
            this.FSaveAllToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveAll;
            this.FSaveAllToolStripMenuItem.MergeIndex = 90;
            this.FSaveAllToolStripMenuItem.Name = "FSaveAllToolStripMenuItem";
            this.FSaveAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.S)));
            this.FSaveAllToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FSaveAllToolStripMenuItem.Text = "Save &All";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.MergeIndex = 100;
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(204, 6);
            // 
            // FLaunchFormToolStripMenuItem
            // 
            this.FLaunchFormToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.LaunchForm;
            this.FLaunchFormToolStripMenuItem.MergeIndex = 110;
            this.FLaunchFormToolStripMenuItem.Name = "FLaunchFormToolStripMenuItem";
            this.FLaunchFormToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.FLaunchFormToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FLaunchFormToolStripMenuItem.Text = "&Launch Form";
            // 
            // FExitToolStripMenuItem
            // 
            this.FExitToolStripMenuItem.MergeIndex = 120;
            this.FExitToolStripMenuItem.Name = "FExitToolStripMenuItem";
            this.FExitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.FExitToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.FExitToolStripMenuItem.Text = "E&xit";
            // 
            // FViewToolStripMenuItem
            // 
            this.FViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            FDataphorExplorerToolStripMenuItem,
            FWarningsErrorsToolStripMenuItem,
            toolStripMenuItem4,
            FClearWarningsToolStripMenuItem});
            this.FViewToolStripMenuItem.MergeIndex = 20;
            this.FViewToolStripMenuItem.Name = "FViewToolStripMenuItem";
            this.FViewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.FViewToolStripMenuItem.Text = "&View";
            this.FViewToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
            // 
            // LConfigureToolStripMenuItem
            // 
            this.LConfigureToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            FDesignerLibrariesToolStripMenuItem,
            FDocumentTypesToolStripMenuItem});
            this.LConfigureToolStripMenuItem.MergeIndex = 30;
            this.LConfigureToolStripMenuItem.Name = "LConfigureToolStripMenuItem";
            this.LConfigureToolStripMenuItem.Size = new System.Drawing.Size(72, 20);
            this.LConfigureToolStripMenuItem.Text = "&Configure";
            this.LConfigureToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
            // 
            // FDebugToolStripMenuItem
            // 
            this.FDebugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FDebugStopMenuItem,
            this.FDebugRunMenuItem,
            this.FDebugPauseMenuItem,
            this.toolStripMenuItem7,
            this.FViewSessionsMenuItem,
            this.FViewProcessesMenuItem,
            this.FViewDebugProcessesMenuItem,
            this.FViewCallStackMenuItem,
            this.FViewStackMenuItem,
            this.toolStripMenuItem8,
            this.FBreakOnExceptionMenuItem,
            this.FBreakOnStartMenuItem,
            this.FDebugStepOverMenuItem,
            this.FDebugStepIntoMenuItem});
            this.FDebugToolStripMenuItem.MergeIndex = 40;
            this.FDebugToolStripMenuItem.Name = "FDebugToolStripMenuItem";
            this.FDebugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.FDebugToolStripMenuItem.Text = "&Debug";
            this.FDebugToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.DebugMenuItemClicked);
            // 
            // FDebugStopMenuItem
            // 
            this.FDebugStopMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStop;
            this.FDebugStopMenuItem.Name = "FDebugStopMenuItem";
            this.FDebugStopMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F5)));
            this.FDebugStopMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FDebugStopMenuItem.Text = "&Stop";
            // 
            // FDebugRunMenuItem
            // 
            this.FDebugRunMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugRun;
            this.FDebugRunMenuItem.Name = "FDebugRunMenuItem";
            this.FDebugRunMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.FDebugRunMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FDebugRunMenuItem.Text = "&Run";
            // 
            // FDebugPauseMenuItem
            // 
            this.FDebugPauseMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugPause;
            this.FDebugPauseMenuItem.Name = "FDebugPauseMenuItem";
            this.FDebugPauseMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FDebugPauseMenuItem.Text = "&Pause";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(188, 6);
            // 
            // FViewSessionsMenuItem
            // 
            this.FViewSessionsMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugSessions;
            this.FViewSessionsMenuItem.Name = "FViewSessionsMenuItem";
            this.FViewSessionsMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FViewSessionsMenuItem.Text = "&View Sessions";
            // 
            // FViewProcessesMenuItem
            // 
            this.FViewProcessesMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugProcesses;
            this.FViewProcessesMenuItem.Name = "FViewProcessesMenuItem";
            this.FViewProcessesMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FViewProcessesMenuItem.Text = "View &Processes";
            // 
            // FViewDebugProcessesMenuItem
            // 
            this.FViewDebugProcessesMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugDebugProcesses;
            this.FViewDebugProcessesMenuItem.Name = "FViewDebugProcessesMenuItem";
            this.FViewDebugProcessesMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FViewDebugProcessesMenuItem.Text = "Vi&ew Debug Processes";
            // 
            // FViewCallStackMenuItem
            // 
            this.FViewCallStackMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
            this.FViewCallStackMenuItem.Name = "FViewCallStackMenuItem";
            this.FViewCallStackMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FViewCallStackMenuItem.Text = "Vie&w Call Stack";
            // 
            // FViewStackMenuItem
            // 
            this.FViewStackMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStack;
            this.FViewStackMenuItem.Name = "FViewStackMenuItem";
            this.FViewStackMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FViewStackMenuItem.Text = "View S&tack";
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(188, 6);
            // 
            // FBreakOnExceptionMenuItem
            // 
            this.FBreakOnExceptionMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugBreakException;
            this.FBreakOnExceptionMenuItem.Name = "FBreakOnExceptionMenuItem";
            this.FBreakOnExceptionMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FBreakOnExceptionMenuItem.Text = "&Break On Exception";
            // 
            // FBreakOnStartMenuItem
            // 
            this.FBreakOnStartMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugBreakStart;
            this.FBreakOnStartMenuItem.Name = "FBreakOnStartMenuItem";
            this.FBreakOnStartMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FBreakOnStartMenuItem.Text = "Bre&ak On Start";
            // 
            // FDebugStepOverMenuItem
            // 
            this.FDebugStepOverMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStepOver;
            this.FDebugStepOverMenuItem.Name = "FDebugStepOverMenuItem";
            this.FDebugStepOverMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F10;
            this.FDebugStepOverMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FDebugStepOverMenuItem.Text = "Step &Over";
            // 
            // FDebugStepIntoMenuItem
            // 
            this.FDebugStepIntoMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStepInto;
            this.FDebugStepIntoMenuItem.Name = "FDebugStepIntoMenuItem";
            this.FDebugStepIntoMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F12;
            this.FDebugStepIntoMenuItem.Size = new System.Drawing.Size(191, 22);
            this.FDebugStepIntoMenuItem.Text = "Step &Into";
            // 
            // LHelpToolStripMenuItem
            // 
            this.LHelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            FDataphorDocumentationToolStripMenuItem,
            toolStripMenuItem5,
            FAlphoraWebSiteToolStripMenuItem,
            FAlphoraDiscussionGroupsToolStripMenuItem,
            FWebDocumentationToolStripMenuItem,
            toolStripMenuItem6,
            FAboutToolStripMenuItem});
            this.LHelpToolStripMenuItem.MergeIndex = 1000;
            this.LHelpToolStripMenuItem.Name = "LHelpToolStripMenuItem";
            this.LHelpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.LHelpToolStripMenuItem.Text = "&Help";
            this.LHelpToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
            // 
            // FToolStrip
            // 
            this.FToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FNewToolStripButton,
            this.FNewScriptToolStripButton,
            this.FOpenFileToolStripButton,
            this.FOpenFileWithToolStripButton,
            this.FLaunchFormToolStripButton,
            this.toolStripSeparator1});
            this.FToolStrip.Location = new System.Drawing.Point(3, 24);
            this.FToolStrip.Name = "FToolStrip";
            this.FToolStrip.Size = new System.Drawing.Size(133, 25);
            this.FToolStrip.TabIndex = 15;
            this.FToolStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
            // 
            // FNewToolStripButton
            // 
            this.FNewToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FNewToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.New;
            this.FNewToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FNewToolStripButton.Name = "FNewToolStripButton";
            this.FNewToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FNewToolStripButton.Text = "New";
            // 
            // FNewScriptToolStripButton
            // 
            this.FNewScriptToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FNewScriptToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.NewScript;
            this.FNewScriptToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FNewScriptToolStripButton.Name = "FNewScriptToolStripButton";
            this.FNewScriptToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FNewScriptToolStripButton.Text = "New script";
            // 
            // FOpenFileToolStripButton
            // 
            this.FOpenFileToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FOpenFileToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Open;
            this.FOpenFileToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FOpenFileToolStripButton.Name = "FOpenFileToolStripButton";
            this.FOpenFileToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FOpenFileToolStripButton.Text = "Open file";
            // 
            // FOpenFileWithToolStripButton
            // 
            this.FOpenFileWithToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FOpenFileWithToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.OpenWith;
            this.FOpenFileWithToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FOpenFileWithToolStripButton.Name = "FOpenFileWithToolStripButton";
            this.FOpenFileWithToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FOpenFileWithToolStripButton.Text = "Open file with";
            // 
            // FLaunchFormToolStripButton
            // 
            this.FLaunchFormToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FLaunchFormToolStripButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.LaunchForm;
            this.FLaunchFormToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FLaunchFormToolStripButton.Name = "FLaunchFormToolStripButton";
            this.FLaunchFormToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.FLaunchFormToolStripButton.Text = "Launch form";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // FDebugToolStrip
            // 
            this.FDebugToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.FDebugToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FDebugStopButton,
            this.FDebugRunButton,
            this.FDebugPauseButton,
            this.FDebugStepIntoButton,
            this.FDebugStepOverButton,
            this.FBreakOnExceptionButton,
            this.FBreakOnStartButton,
            this.FViewSessionsButton,
            this.FViewProcessesButton,
            this.FViewDebugProcessesButton,
            this.FViewCallStackButton,
            this.FViewStackButton});
            this.FDebugToolStrip.Location = new System.Drawing.Point(136, 24);
            this.FDebugToolStrip.Name = "FDebugToolStrip";
            this.FDebugToolStrip.Size = new System.Drawing.Size(288, 25);
            this.FDebugToolStrip.TabIndex = 16;
            this.FDebugToolStrip.Text = "Debug";
            this.FDebugToolStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.DebugMenuItemClicked);
            // 
            // FDebugStopButton
            // 
            this.FDebugStopButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDebugStopButton.Enabled = false;
            this.FDebugStopButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStop;
            this.FDebugStopButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDebugStopButton.Name = "FDebugStopButton";
            this.FDebugStopButton.Size = new System.Drawing.Size(23, 22);
            this.FDebugStopButton.Text = "Stop Debugging";
            // 
            // FDebugRunButton
            // 
            this.FDebugRunButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDebugRunButton.Enabled = false;
            this.FDebugRunButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugRun;
            this.FDebugRunButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDebugRunButton.Name = "FDebugRunButton";
            this.FDebugRunButton.Size = new System.Drawing.Size(23, 22);
            this.FDebugRunButton.Text = "Debug Run";
            // 
            // FDebugPauseButton
            // 
            this.FDebugPauseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDebugPauseButton.Enabled = false;
            this.FDebugPauseButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugPause;
            this.FDebugPauseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDebugPauseButton.Name = "FDebugPauseButton";
            this.FDebugPauseButton.Size = new System.Drawing.Size(23, 22);
            this.FDebugPauseButton.Text = "Debug Pause";
            // 
            // FDebugStepIntoButton
            // 
            this.FDebugStepIntoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDebugStepIntoButton.Enabled = false;
            this.FDebugStepIntoButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStepInto;
            this.FDebugStepIntoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDebugStepIntoButton.Name = "FDebugStepIntoButton";
            this.FDebugStepIntoButton.Size = new System.Drawing.Size(23, 22);
            this.FDebugStepIntoButton.Text = "Step Into";
            // 
            // FDebugStepOverButton
            // 
            this.FDebugStepOverButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FDebugStepOverButton.Enabled = false;
            this.FDebugStepOverButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStepOver;
            this.FDebugStepOverButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FDebugStepOverButton.Name = "FDebugStepOverButton";
            this.FDebugStepOverButton.Size = new System.Drawing.Size(23, 22);
            this.FDebugStepOverButton.Text = "Step Over";
            // 
            // FBreakOnExceptionButton
            // 
            this.FBreakOnExceptionButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FBreakOnExceptionButton.Enabled = false;
            this.FBreakOnExceptionButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugBreakException;
            this.FBreakOnExceptionButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FBreakOnExceptionButton.Name = "FBreakOnExceptionButton";
            this.FBreakOnExceptionButton.Size = new System.Drawing.Size(23, 22);
            this.FBreakOnExceptionButton.Text = "Break On Exception";
            // 
            // FBreakOnStartButton
            // 
            this.FBreakOnStartButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FBreakOnStartButton.Enabled = false;
            this.FBreakOnStartButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugBreakStart;
            this.FBreakOnStartButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FBreakOnStartButton.Name = "FBreakOnStartButton";
            this.FBreakOnStartButton.Size = new System.Drawing.Size(23, 22);
            this.FBreakOnStartButton.Text = "Break On Start";
            // 
            // FViewSessionsButton
            // 
            this.FViewSessionsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FViewSessionsButton.Enabled = false;
            this.FViewSessionsButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugSessions;
            this.FViewSessionsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FViewSessionsButton.Name = "FViewSessionsButton";
            this.FViewSessionsButton.Size = new System.Drawing.Size(23, 22);
            this.FViewSessionsButton.Text = "View Sessions";
            // 
            // FViewProcessesButton
            // 
            this.FViewProcessesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FViewProcessesButton.Enabled = false;
            this.FViewProcessesButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugProcesses;
            this.FViewProcessesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FViewProcessesButton.Name = "FViewProcessesButton";
            this.FViewProcessesButton.Size = new System.Drawing.Size(23, 22);
            this.FViewProcessesButton.Text = "View Processes";
            // 
            // FViewDebugProcessesButton
            // 
            this.FViewDebugProcessesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FViewDebugProcessesButton.Enabled = false;
            this.FViewDebugProcessesButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugDebugProcesses;
            this.FViewDebugProcessesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FViewDebugProcessesButton.Name = "FViewDebugProcessesButton";
            this.FViewDebugProcessesButton.Size = new System.Drawing.Size(23, 22);
            this.FViewDebugProcessesButton.Text = "View Debug Processes";
            // 
            // FViewCallStackButton
            // 
            this.FViewCallStackButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FViewCallStackButton.Enabled = false;
            this.FViewCallStackButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
            this.FViewCallStackButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FViewCallStackButton.Name = "FViewCallStackButton";
            this.FViewCallStackButton.Size = new System.Drawing.Size(23, 22);
            this.FViewCallStackButton.Text = "View Call Stack";
            // 
            // FViewStackButton
            // 
            this.FViewStackButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FViewStackButton.Enabled = false;
            this.FViewStackButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugStack;
            this.FViewStackButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FViewStackButton.Name = "FViewStackButton";
            this.FViewStackButton.Size = new System.Drawing.Size(23, 22);
            this.FViewStackButton.Text = "View Stack";
            // 
            // FBottomToolStripPanel
            // 
            this.FBottomToolStripPanel.Controls.Add(this.FStatusStrip);
            this.FBottomToolStripPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.FBottomToolStripPanel.Location = new System.Drawing.Point(0, 485);
            this.FBottomToolStripPanel.Name = "FBottomToolStripPanel";
            this.FBottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.FBottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.FBottomToolStripPanel.Size = new System.Drawing.Size(664, 22);
            // 
            // FStatusStrip
            // 
            this.FStatusStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.FStatusStrip.Location = new System.Drawing.Point(0, 0);
            this.FStatusStrip.Name = "FStatusStrip";
            this.FStatusStrip.Size = new System.Drawing.Size(664, 22);
            this.FStatusStrip.TabIndex = 4;
            this.FStatusStrip.Text = "statusStrip1";
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
            this.FTreeImageList.Images.SetKeyName(13, "Tables.png");
            this.FTreeImageList.Images.SetKeyName(14, "Table.png");
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
            this.FTreeImageList.Images.SetKeyName(31, "Tables - System.png");
            this.FTreeImageList.Images.SetKeyName(32, "Table - System.png");
            this.FTreeImageList.Images.SetKeyName(33, "Views - System.png");
            this.FTreeImageList.Images.SetKeyName(34, "View - System.png");
            this.FTreeImageList.Images.SetKeyName(35, "Tables - Generated.png");
            this.FTreeImageList.Images.SetKeyName(36, "Table - Generated.png");
            // 
            // Dataphoria
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 507);
            this.Controls.Add(this.FDockPanel);
            this.Controls.Add(this.FBottomToolStripPanel);
            this.Controls.Add(this.FTopToolStripPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.FMainMenuStrip;
            this.Name = "Dataphoria";
            this.Text = "Dataphoria";
            this.Shown += new System.EventHandler(this.Dataphoria_Shown);
            this.FTopToolStripPanel.ResumeLayout(false);
            this.FTopToolStripPanel.PerformLayout();
            this.FMainMenuStrip.ResumeLayout(false);
            this.FMainMenuStrip.PerformLayout();
            this.FToolStrip.ResumeLayout(false);
            this.FToolStrip.PerformLayout();
            this.FDebugToolStrip.ResumeLayout(false);
            this.FDebugToolStrip.PerformLayout();
            this.FBottomToolStripPanel.ResumeLayout(false);
            this.FBottomToolStripPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WeifenLuo.WinFormsUI.Docking.DockPanel FDockPanel;
		private System.Windows.Forms.ToolStripPanel FTopToolStripPanel;
		private System.Windows.Forms.ToolStripPanel FBottomToolStripPanel;
		private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
		private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
		private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
		private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
		private System.Windows.Forms.ToolStripContentPanel ContentPanel;
		private System.Windows.Forms.MenuStrip FMainMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem FFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FConnectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDisconnectToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem FNewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FNewScriptToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FOpenFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FOpenFileWithToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem FSaveAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem FLaunchFormToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FExitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FViewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem LConfigureToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugStopMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugRunMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugPauseMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
		private System.Windows.Forms.ToolStripMenuItem FViewSessionsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FViewProcessesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FViewDebugProcessesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FViewCallStackMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
		private System.Windows.Forms.ToolStripMenuItem FBreakOnExceptionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FBreakOnStartMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugStepOverMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugStepIntoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem LHelpToolStripMenuItem;
		private System.Windows.Forms.ToolStrip FToolStrip;
		private System.Windows.Forms.ToolStripButton FNewToolStripButton;
		private System.Windows.Forms.ToolStripButton FNewScriptToolStripButton;
		private System.Windows.Forms.ToolStripButton FOpenFileToolStripButton;
		private System.Windows.Forms.ToolStripButton FOpenFileWithToolStripButton;
		private System.Windows.Forms.ToolStripButton FLaunchFormToolStripButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStrip FDebugToolStrip;
		private System.Windows.Forms.ToolStripButton FDebugStopButton;
		private System.Windows.Forms.ToolStripButton FDebugRunButton;
		private System.Windows.Forms.ToolStripButton FDebugPauseButton;
		private System.Windows.Forms.ToolStripButton FDebugStepIntoButton;
		private System.Windows.Forms.ToolStripButton FDebugStepOverButton;
		private System.Windows.Forms.ToolStripButton FBreakOnExceptionButton;
		private System.Windows.Forms.ToolStripButton FBreakOnStartButton;
		private System.Windows.Forms.ToolStripButton FViewSessionsButton;
		private System.Windows.Forms.ToolStripButton FViewProcessesButton;
		private System.Windows.Forms.ToolStripButton FViewDebugProcessesButton;
		private System.Windows.Forms.ToolStripButton FViewCallStackButton;
		private System.Windows.Forms.ToolStripButton FViewStackButton;
		private System.Windows.Forms.StatusStrip FStatusStrip;
		private System.Windows.Forms.ToolStripMenuItem FViewStackMenuItem;
        private System.Windows.Forms.ImageList FTreeImageList;
    }
}
