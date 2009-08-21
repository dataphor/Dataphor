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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dataphoria));
			this.FTreeImageList = new System.Windows.Forms.ImageList(this.components);
			this.FStatusStrip = new System.Windows.Forms.StatusStrip();
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
			this.FDataphorExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FWarningsErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.FClearWarningsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.LConfigureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDesignerLibrariesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDocumentTypesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.LHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDataphorDocumentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this.FAlphoraWebSiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FAlphoraDiscussionGroupsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FWebDocumentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
			this.FAboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDebugStopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDebugRunMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDebugPauseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
			this.FViewSessionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FViewProcessesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FViewDebugProcessesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FViewCallStackMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
			this.FBreakOnExceptionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FToolStrip = new System.Windows.Forms.ToolStrip();
			this.FNewToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.FNewScriptToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.FOpenFileToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.FOpenFileWithToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.FLaunchFormToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.FDebugStopButton = new System.Windows.Forms.ToolStripButton();
			this.FDebugRunButton = new System.Windows.Forms.ToolStripButton();
			this.FDebugPauseButton = new System.Windows.Forms.ToolStripButton();
			this.FBreakOnExceptionButton = new System.Windows.Forms.ToolStripButton();
			this.FViewSessionsButton = new System.Windows.Forms.ToolStripButton();
			this.FViewProcessesButton = new System.Windows.Forms.ToolStripButton();
			this.FViewDebugProcessesButton = new System.Windows.Forms.ToolStripButton();
			this.FViewCallStackButton = new System.Windows.Forms.ToolStripButton();
			this.FDockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
			this.FMainMenuStrip.SuspendLayout();
			this.FToolStrip.SuspendLayout();
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
			// FStatusStrip
			// 
			this.FStatusStrip.Location = new System.Drawing.Point(0, 485);
			this.FStatusStrip.Name = "FStatusStrip";
			this.FStatusStrip.Size = new System.Drawing.Size(664, 22);
			this.FStatusStrip.TabIndex = 2;
			this.FStatusStrip.Text = "statusStrip1";
			// 
			// FMainMenuStrip
			// 
			this.FMainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FFileToolStripMenuItem,
            this.FViewToolStripMenuItem,
            this.LConfigureToolStripMenuItem,
            this.LHelpToolStripMenuItem,
            this.debugToolStripMenuItem});
			this.FMainMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.FMainMenuStrip.Name = "FMainMenuStrip";
			this.FMainMenuStrip.Size = new System.Drawing.Size(664, 24);
			this.FMainMenuStrip.TabIndex = 3;
			this.FMainMenuStrip.Text = "menuStrip1";
			this.FMainMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
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
			this.FFileToolStripMenuItem.Name = "FFileToolStripMenuItem";
			this.FFileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.FFileToolStripMenuItem.Text = "File";
			this.FFileToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
			// 
			// FConnectToolStripMenuItem
			// 
			this.FConnectToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Connect;
			this.FConnectToolStripMenuItem.Name = "FConnectToolStripMenuItem";
			this.FConnectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.C)));
			this.FConnectToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FConnectToolStripMenuItem.Text = "Connect";
			// 
			// FDisconnectToolStripMenuItem
			// 
			this.FDisconnectToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Disconnect;
			this.FDisconnectToolStripMenuItem.Name = "FDisconnectToolStripMenuItem";
			this.FDisconnectToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FDisconnectToolStripMenuItem.Text = "Disconnect";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(194, 6);
			// 
			// FNewToolStripMenuItem
			// 
			this.FNewToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.New;
			this.FNewToolStripMenuItem.Name = "FNewToolStripMenuItem";
			this.FNewToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.N)));
			this.FNewToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FNewToolStripMenuItem.Text = "New";
			// 
			// FNewScriptToolStripMenuItem
			// 
			this.FNewScriptToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.NewScript;
			this.FNewScriptToolStripMenuItem.Name = "FNewScriptToolStripMenuItem";
			this.FNewScriptToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.FNewScriptToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FNewScriptToolStripMenuItem.Text = "New Script";
			// 
			// FOpenFileToolStripMenuItem
			// 
			this.FOpenFileToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Open;
			this.FOpenFileToolStripMenuItem.Name = "FOpenFileToolStripMenuItem";
			this.FOpenFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.FOpenFileToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FOpenFileToolStripMenuItem.Text = "Open File";
			// 
			// FOpenFileWithToolStripMenuItem
			// 
			this.FOpenFileWithToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.OpenWith;
			this.FOpenFileWithToolStripMenuItem.Name = "FOpenFileWithToolStripMenuItem";
			this.FOpenFileWithToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.FOpenFileWithToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FOpenFileWithToolStripMenuItem.Text = "Open File With";
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(194, 6);
			// 
			// FSaveAllToolStripMenuItem
			// 
			this.FSaveAllToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveAll;
			this.FSaveAllToolStripMenuItem.Name = "FSaveAllToolStripMenuItem";
			this.FSaveAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.S)));
			this.FSaveAllToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FSaveAllToolStripMenuItem.Text = "Save All";
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(194, 6);
			// 
			// FLaunchFormToolStripMenuItem
			// 
			this.FLaunchFormToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.LaunchForm;
			this.FLaunchFormToolStripMenuItem.Name = "FLaunchFormToolStripMenuItem";
			this.FLaunchFormToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
			this.FLaunchFormToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FLaunchFormToolStripMenuItem.Text = "Launch Form";
			// 
			// FExitToolStripMenuItem
			// 
			this.FExitToolStripMenuItem.Name = "FExitToolStripMenuItem";
			this.FExitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.FExitToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.FExitToolStripMenuItem.Text = "Exit";
			// 
			// FViewToolStripMenuItem
			// 
			this.FViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FDataphorExplorerToolStripMenuItem,
            this.FWarningsErrorsToolStripMenuItem,
            this.toolStripMenuItem4,
            this.FClearWarningsToolStripMenuItem});
			this.FViewToolStripMenuItem.Name = "FViewToolStripMenuItem";
			this.FViewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.FViewToolStripMenuItem.Text = "View";
			this.FViewToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
			// 
			// FDataphorExplorerToolStripMenuItem
			// 
			this.FDataphorExplorerToolStripMenuItem.Name = "FDataphorExplorerToolStripMenuItem";
			this.FDataphorExplorerToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F12;
			this.FDataphorExplorerToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.FDataphorExplorerToolStripMenuItem.Text = "Dataphor Explorer";
			// 
			// FWarningsErrorsToolStripMenuItem
			// 
			this.FWarningsErrorsToolStripMenuItem.Name = "FWarningsErrorsToolStripMenuItem";
			this.FWarningsErrorsToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.FWarningsErrorsToolStripMenuItem.Text = "Warnings and Errors";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(190, 6);
			// 
			// FClearWarningsToolStripMenuItem
			// 
			this.FClearWarningsToolStripMenuItem.Name = "FClearWarningsToolStripMenuItem";
			this.FClearWarningsToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.FClearWarningsToolStripMenuItem.Text = "Clear Warnings";
			// 
			// LConfigureToolStripMenuItem
			// 
			this.LConfigureToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FDesignerLibrariesToolStripMenuItem,
            this.FDocumentTypesToolStripMenuItem});
			this.LConfigureToolStripMenuItem.Name = "LConfigureToolStripMenuItem";
			this.LConfigureToolStripMenuItem.Size = new System.Drawing.Size(72, 20);
			this.LConfigureToolStripMenuItem.Text = "Configure";
			this.LConfigureToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
			// 
			// FDesignerLibrariesToolStripMenuItem
			// 
			this.FDesignerLibrariesToolStripMenuItem.Name = "FDesignerLibrariesToolStripMenuItem";
			this.FDesignerLibrariesToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.FDesignerLibrariesToolStripMenuItem.Text = "Designer Libraries";
			// 
			// FDocumentTypesToolStripMenuItem
			// 
			this.FDocumentTypesToolStripMenuItem.Name = "FDocumentTypesToolStripMenuItem";
			this.FDocumentTypesToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.FDocumentTypesToolStripMenuItem.Text = "Document Types";
			// 
			// LHelpToolStripMenuItem
			// 
			this.LHelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FDataphorDocumentationToolStripMenuItem,
            this.toolStripMenuItem5,
            this.FAlphoraWebSiteToolStripMenuItem,
            this.FAlphoraDiscussionGroupsToolStripMenuItem,
            this.FWebDocumentationToolStripMenuItem,
            this.toolStripMenuItem6,
            this.FAboutToolStripMenuItem});
			this.LHelpToolStripMenuItem.Name = "LHelpToolStripMenuItem";
			this.LHelpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.LHelpToolStripMenuItem.Text = "Help";
			this.LHelpToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
			// 
			// FDataphorDocumentationToolStripMenuItem
			// 
			this.FDataphorDocumentationToolStripMenuItem.Name = "FDataphorDocumentationToolStripMenuItem";
			this.FDataphorDocumentationToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FDataphorDocumentationToolStripMenuItem.Text = "Dataphor Documentation";
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(213, 6);
			// 
			// FAlphoraWebSiteToolStripMenuItem
			// 
			this.FAlphoraWebSiteToolStripMenuItem.Name = "FAlphoraWebSiteToolStripMenuItem";
			this.FAlphoraWebSiteToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FAlphoraWebSiteToolStripMenuItem.Text = "Alphora WebSite";
			// 
			// FAlphoraDiscussionGroupsToolStripMenuItem
			// 
			this.FAlphoraDiscussionGroupsToolStripMenuItem.Name = "FAlphoraDiscussionGroupsToolStripMenuItem";
			this.FAlphoraDiscussionGroupsToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FAlphoraDiscussionGroupsToolStripMenuItem.Text = "Alphora Discussion Groups";
			// 
			// FWebDocumentationToolStripMenuItem
			// 
			this.FWebDocumentationToolStripMenuItem.Name = "FWebDocumentationToolStripMenuItem";
			this.FWebDocumentationToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FWebDocumentationToolStripMenuItem.Text = "Web Documentation";
			// 
			// toolStripMenuItem6
			// 
			this.toolStripMenuItem6.Name = "toolStripMenuItem6";
			this.toolStripMenuItem6.Size = new System.Drawing.Size(213, 6);
			// 
			// FAboutToolStripMenuItem
			// 
			this.FAboutToolStripMenuItem.Name = "FAboutToolStripMenuItem";
			this.FAboutToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FAboutToolStripMenuItem.Text = "About";
			// 
			// debugToolStripMenuItem
			// 
			this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FDebugStopMenuItem,
            this.FDebugRunMenuItem,
            this.FDebugPauseMenuItem,
            this.toolStripMenuItem7,
            this.FViewSessionsMenuItem,
            this.FViewProcessesMenuItem,
            this.FViewDebugProcessesMenuItem,
            this.FViewCallStackMenuItem,
            this.toolStripMenuItem8,
            this.FBreakOnExceptionMenuItem});
			this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
			this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.debugToolStripMenuItem.Text = "Debug";
			this.debugToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FToolStrip_ItemClicked);
			// 
			// FDebugStopMenuItem
			// 
			this.FDebugStopMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("FDebugStopMenuItem.Image")));
			this.FDebugStopMenuItem.Name = "FDebugStopMenuItem";
			this.FDebugStopMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FDebugStopMenuItem.Text = "Stop";
			// 
			// FDebugRunMenuItem
			// 
			this.FDebugRunMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugRun;
			this.FDebugRunMenuItem.Name = "FDebugRunMenuItem";
			this.FDebugRunMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FDebugRunMenuItem.Text = "Run";
			// 
			// FDebugPauseMenuItem
			// 
			this.FDebugPauseMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugPause;
			this.FDebugPauseMenuItem.Name = "FDebugPauseMenuItem";
			this.FDebugPauseMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FDebugPauseMenuItem.Text = "Pause";
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
			this.FViewSessionsMenuItem.Text = "View Sessions";
			// 
			// FViewProcessesMenuItem
			// 
			this.FViewProcessesMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugProcesses;
			this.FViewProcessesMenuItem.Name = "FViewProcessesMenuItem";
			this.FViewProcessesMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FViewProcessesMenuItem.Text = "View Processes";
			// 
			// FViewDebugProcessesMenuItem
			// 
			this.FViewDebugProcessesMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugDebugProcesses;
			this.FViewDebugProcessesMenuItem.Name = "FViewDebugProcessesMenuItem";
			this.FViewDebugProcessesMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FViewDebugProcessesMenuItem.Text = "View Debug Processes";
			// 
			// FViewCallStackMenuItem
			// 
			this.FViewCallStackMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
			this.FViewCallStackMenuItem.Name = "FViewCallStackMenuItem";
			this.FViewCallStackMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FViewCallStackMenuItem.Text = "View Call Stack";
			// 
			// toolStripMenuItem8
			// 
			this.toolStripMenuItem8.Name = "toolStripMenuItem8";
			this.toolStripMenuItem8.Size = new System.Drawing.Size(188, 6);
			// 
			// FBreakOnExceptionMenuItem
			// 
			this.FBreakOnExceptionMenuItem.CheckOnClick = true;
			this.FBreakOnExceptionMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugBreakException;
			this.FBreakOnExceptionMenuItem.Name = "FBreakOnExceptionMenuItem";
			this.FBreakOnExceptionMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FBreakOnExceptionMenuItem.Text = "Break On Exception";
			// 
			// FToolStrip
			// 
			this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FNewToolStripButton,
            this.FNewScriptToolStripButton,
            this.FOpenFileToolStripButton,
            this.FOpenFileWithToolStripButton,
            this.FLaunchFormToolStripButton,
            this.toolStripSeparator1,
            this.FDebugStopButton,
            this.FDebugRunButton,
            this.FDebugPauseButton,
            this.FBreakOnExceptionButton,
            this.FViewSessionsButton,
            this.FViewProcessesButton,
            this.FViewDebugProcessesButton,
            this.FViewCallStackButton});
			this.FToolStrip.Location = new System.Drawing.Point(0, 24);
			this.FToolStrip.Name = "FToolStrip";
			this.FToolStrip.Size = new System.Drawing.Size(664, 25);
			this.FToolStrip.TabIndex = 4;
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
			// FDebugStopButton
			// 
			this.FDebugStopButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FDebugStopButton.Image = ((System.Drawing.Image)(resources.GetObject("FDebugStopButton.Image")));
			this.FDebugStopButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FDebugStopButton.Name = "FDebugStopButton";
			this.FDebugStopButton.Size = new System.Drawing.Size(23, 22);
			this.FDebugStopButton.Text = "Stop Debugging";
			// 
			// FDebugRunButton
			// 
			this.FDebugRunButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FDebugRunButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugRun;
			this.FDebugRunButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FDebugRunButton.Name = "FDebugRunButton";
			this.FDebugRunButton.Size = new System.Drawing.Size(23, 22);
			this.FDebugRunButton.Text = "Debug Run";
			// 
			// FDebugPauseButton
			// 
			this.FDebugPauseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FDebugPauseButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugPause;
			this.FDebugPauseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FDebugPauseButton.Name = "FDebugPauseButton";
			this.FDebugPauseButton.Size = new System.Drawing.Size(23, 22);
			this.FDebugPauseButton.Text = "Debug Pause";
			// 
			// FBreakOnExceptionButton
			// 
			this.FBreakOnExceptionButton.CheckOnClick = true;
			this.FBreakOnExceptionButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FBreakOnExceptionButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugBreakException;
			this.FBreakOnExceptionButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FBreakOnExceptionButton.Name = "FBreakOnExceptionButton";
			this.FBreakOnExceptionButton.Size = new System.Drawing.Size(23, 22);
			this.FBreakOnExceptionButton.Text = "Break On Exception";
			// 
			// FViewSessionsButton
			// 
			this.FViewSessionsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FViewSessionsButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugSessions;
			this.FViewSessionsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FViewSessionsButton.Name = "FViewSessionsButton";
			this.FViewSessionsButton.Size = new System.Drawing.Size(23, 22);
			this.FViewSessionsButton.Text = "View Sessions";
			// 
			// FViewProcessesButton
			// 
			this.FViewProcessesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FViewProcessesButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugProcesses;
			this.FViewProcessesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FViewProcessesButton.Name = "FViewProcessesButton";
			this.FViewProcessesButton.Size = new System.Drawing.Size(23, 22);
			this.FViewProcessesButton.Text = "View Processes";
			// 
			// FViewDebugProcessesButton
			// 
			this.FViewDebugProcessesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FViewDebugProcessesButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugDebugProcesses;
			this.FViewDebugProcessesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FViewDebugProcessesButton.Name = "FViewDebugProcessesButton";
			this.FViewDebugProcessesButton.Size = new System.Drawing.Size(23, 22);
			this.FViewDebugProcessesButton.Text = "View Debug Processes";
			// 
			// FViewCallStackButton
			// 
			this.FViewCallStackButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FViewCallStackButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
			this.FViewCallStackButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FViewCallStackButton.Name = "FViewCallStackButton";
			this.FViewCallStackButton.Size = new System.Drawing.Size(23, 22);
			this.FViewCallStackButton.Text = "View Call Stack";
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
			// Dataphoria
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(664, 507);
			this.Controls.Add(this.FDockPanel);
			this.Controls.Add(this.FToolStrip);
			this.Controls.Add(this.FMainMenuStrip);
			this.Controls.Add(this.FStatusStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.IsMdiContainer = true;
			this.MainMenuStrip = this.FMainMenuStrip;
			this.Name = "Dataphoria";
			this.Text = "Dataphoria";
			this.Shown += new System.EventHandler(this.Dataphoria_Shown);
			this.FMainMenuStrip.ResumeLayout(false);
			this.FMainMenuStrip.PerformLayout();
			this.FToolStrip.ResumeLayout(false);
			this.FToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ImageList FTreeImageList;
        private System.Windows.Forms.StatusStrip FStatusStrip;
        private System.Windows.Forms.MenuStrip FMainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem FFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FConnectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FDisconnectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FNewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FNewScriptToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FOpenFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FOpenFileWithToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FSaveAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FLaunchFormToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FDataphorExplorerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FWarningsErrorsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FClearWarningsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LConfigureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FDesignerLibrariesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FDocumentTypesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LHelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FDataphorDocumentationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FAlphoraWebSiteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FAlphoraDiscussionGroupsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FWebDocumentationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FAboutToolStripMenuItem;
        private System.Windows.Forms.ToolStrip FToolStrip;
        private System.Windows.Forms.ToolStripButton FNewToolStripButton;
        private System.Windows.Forms.ToolStripButton FNewScriptToolStripButton;
        private System.Windows.Forms.ToolStripButton FOpenFileToolStripButton;
        private System.Windows.Forms.ToolStripButton FOpenFileWithToolStripButton;
        private System.Windows.Forms.ToolStripButton FLaunchFormToolStripButton;
        private WeifenLuo.WinFormsUI.Docking.DockPanel FDockPanel;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton FDebugStopButton;
		private System.Windows.Forms.ToolStripButton FViewSessionsButton;
		private System.Windows.Forms.ToolStripButton FViewProcessesButton;
		private System.Windows.Forms.ToolStripButton FViewDebugProcessesButton;
		private System.Windows.Forms.ToolStripButton FDebugPauseButton;
		private System.Windows.Forms.ToolStripButton FDebugRunButton;
		private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugStopMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
		private System.Windows.Forms.ToolStripMenuItem FViewSessionsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FViewProcessesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FViewDebugProcessesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugPauseMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FDebugRunMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FBreakOnExceptionMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
		private System.Windows.Forms.ToolStripButton FBreakOnExceptionButton;
		private System.Windows.Forms.ToolStripButton FViewCallStackButton;
		private System.Windows.Forms.ToolStripMenuItem FViewCallStackMenuItem;
    }
}
