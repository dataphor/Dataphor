/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/


using SD = ICSharpCode.TextEditor;



namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> D4 text editor. </summary>
    partial class D4Editor 
    {
        protected override void Dispose(bool disposing)
        {
            try
            {
				Deinitialize();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			System.Windows.Forms.ToolStripMenuItem FViewResultsMenuItem;
			System.Windows.Forms.ToolStripMenuItem prepareToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem analyzeToolStripMenuItem;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
			System.Windows.Forms.ToolStripMenuItem prepareLineToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem analyzeLineToolStripMenuItem;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
			System.Windows.Forms.ToolStripMenuItem selectBlockToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem priorBlockToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem nextBlockToolStripMenuItem;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
			System.Windows.Forms.ToolStripMenuItem setBreakpointToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem schemaOnlyToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem dataOnlyToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem schemaAndDataToolStripMenuItem;
			System.Windows.Forms.ToolStripButton toolStripButton2;
			System.Windows.Forms.ToolStripButton toolStripButton3;
			this.FScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FExecuteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FCancelExecuteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FExecuteLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FExportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FExecuteButton = new System.Windows.Forms.ToolStripButton();
			this.FCancelExecuteButton = new System.Windows.Forms.ToolStripButton();
			FViewResultsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			prepareToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			analyzeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			prepareLineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			analyzeLineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			selectBlockToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			priorBlockToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			nextBlockToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			setBreakpointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			schemaOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			dataOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			schemaAndDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.FMenuStrip.SuspendLayout();
			this.FToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// FDockPanel
			// 
			this.FDockPanel.Location = new System.Drawing.Point(0, 73);
			this.FDockPanel.Size = new System.Drawing.Size(455, 303);
			// 
			// FTextEdit
			// 
			this._textEdit.Location = new System.Drawing.Point(0, 25);
			this._textEdit.Size = new System.Drawing.Size(453, 276);
			// 
			// FViewResultsMenuItem
			// 
			FViewResultsMenuItem.Name = "FViewResultsMenuItem";
			FViewResultsMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F7;
			FViewResultsMenuItem.Size = new System.Drawing.Size(158, 22);
			FViewResultsMenuItem.Text = "View &Results";
			FViewResultsMenuItem.Click += new System.EventHandler(this.ViewResultsClicked);
			this.FViewToolStripMenuItem.DropDownItems.Add(FViewResultsMenuItem);
			// 
			// FScriptMenuItem
			// 
			this.FScriptMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FExecuteMenuItem,
            this.FCancelExecuteMenuItem,
            prepareToolStripMenuItem,
            analyzeToolStripMenuItem,
            toolStripMenuItem1,
            this.FExecuteLineMenuItem,
            prepareLineToolStripMenuItem,
            analyzeLineToolStripMenuItem,
            toolStripMenuItem2,
            selectBlockToolStripMenuItem,
            priorBlockToolStripMenuItem,
            nextBlockToolStripMenuItem,
            toolStripMenuItem3,
            setBreakpointToolStripMenuItem});
			this.FScriptMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FScriptMenuItem.MergeIndex = 3;
			this.FScriptMenuItem.Name = "FScriptMenuItem";
			this.FScriptMenuItem.Size = new System.Drawing.Size(49, 20);
			this.FScriptMenuItem.Text = "&Script";
			// 
			// FExecuteMenuItem
			// 
			this.FExecuteMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Execute;
			this.FExecuteMenuItem.Name = "FExecuteMenuItem";
			this.FExecuteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.FExecuteMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FExecuteMenuItem.Text = "&Execute";
			this.FExecuteMenuItem.Click += new System.EventHandler(this.ExecuteClicked);
			// 
			// FCancelExecuteMenuItem
			// 
			this.FCancelExecuteMenuItem.Enabled = false;
			this.FCancelExecuteMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.CancelExecute;
			this.FCancelExecuteMenuItem.Name = "FCancelExecuteMenuItem";
			this.FCancelExecuteMenuItem.ShortcutKeyDisplayString = "Ctrl+Break";
			this.FCancelExecuteMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FCancelExecuteMenuItem.Text = "&Cancel Execute";
			this.FCancelExecuteMenuItem.Click += new System.EventHandler(this.CancelExecuteClicked);
			// 
			// prepareToolStripMenuItem
			// 
			prepareToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Prepare;
			prepareToolStripMenuItem.Name = "prepareToolStripMenuItem";
			prepareToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			prepareToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			prepareToolStripMenuItem.Text = "&Prepare";
			prepareToolStripMenuItem.Click += new System.EventHandler(this.PrepareClicked);
			// 
			// analyzeToolStripMenuItem
			// 
			analyzeToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Analyze;
			analyzeToolStripMenuItem.Name = "analyzeToolStripMenuItem";
			analyzeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
			analyzeToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			analyzeToolStripMenuItem.Text = "&Analyze";
			analyzeToolStripMenuItem.Click += new System.EventHandler(this.AnalyzeClicked);
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new System.Drawing.Size(213, 6);
			// 
			// FExecuteLineMenuItem
			// 
			this.FExecuteLineMenuItem.Name = "FExecuteLineMenuItem";
			this.FExecuteLineMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.E)));
			this.FExecuteLineMenuItem.Size = new System.Drawing.Size(216, 22);
			this.FExecuteLineMenuItem.Text = "E&xecute Line";
			this.FExecuteLineMenuItem.Click += new System.EventHandler(this.ExecuteLineClicked);
			// 
			// prepareLineToolStripMenuItem
			// 
			prepareLineToolStripMenuItem.Name = "prepareLineToolStripMenuItem";
			prepareLineToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.R)));
			prepareLineToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			prepareLineToolStripMenuItem.Text = "Prepa&re Line";
			prepareLineToolStripMenuItem.Click += new System.EventHandler(this.PrepareLineClicked);
			// 
			// analyzeLineToolStripMenuItem
			// 
			analyzeLineToolStripMenuItem.Name = "analyzeLineToolStripMenuItem";
			analyzeLineToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.T)));
			analyzeLineToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			analyzeLineToolStripMenuItem.Text = "A&nalyze Line";
			analyzeLineToolStripMenuItem.Click += new System.EventHandler(this.AnalyzeLineClicked);
			// 
			// toolStripMenuItem2
			// 
			toolStripMenuItem2.Name = "toolStripMenuItem2";
			toolStripMenuItem2.Size = new System.Drawing.Size(213, 6);
			// 
			// selectBlockToolStripMenuItem
			// 
			selectBlockToolStripMenuItem.Name = "selectBlockToolStripMenuItem";
			selectBlockToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			selectBlockToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			selectBlockToolStripMenuItem.Text = "&Select Block";
			selectBlockToolStripMenuItem.Click += new System.EventHandler(this.SelectBlockClicked);
			// 
			// priorBlockToolStripMenuItem
			// 
			priorBlockToolStripMenuItem.Name = "priorBlockToolStripMenuItem";
			priorBlockToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+,";
			priorBlockToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			priorBlockToolStripMenuItem.Text = "Pr&ior Block";
			priorBlockToolStripMenuItem.Click += new System.EventHandler(this.PriorBlockClicked);
			// 
			// nextBlockToolStripMenuItem
			// 
			nextBlockToolStripMenuItem.Name = "nextBlockToolStripMenuItem";
			nextBlockToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+.";
			nextBlockToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			nextBlockToolStripMenuItem.Text = "Nex&t Block";
			nextBlockToolStripMenuItem.Click += new System.EventHandler(this.NextBlockClicked);
			// 
			// toolStripMenuItem3
			// 
			toolStripMenuItem3.Name = "toolStripMenuItem3";
			toolStripMenuItem3.Size = new System.Drawing.Size(213, 6);
			// 
			// setBreakpointToolStripMenuItem
			// 
			setBreakpointToolStripMenuItem.Name = "setBreakpointToolStripMenuItem";
			setBreakpointToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F9;
			setBreakpointToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			setBreakpointToolStripMenuItem.Text = "Set Breakpoint";
			setBreakpointToolStripMenuItem.Click += new System.EventHandler(this.ToggleBreakpointClicked);
			// 
			// FExportMenuItem
			// 
			this.FExportMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            schemaOnlyToolStripMenuItem,
            dataOnlyToolStripMenuItem,
            schemaAndDataToolStripMenuItem});
			this.FExportMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.FExportMenuItem.MergeIndex = 4;
			this.FExportMenuItem.Name = "FExportMenuItem";
			this.FExportMenuItem.Size = new System.Drawing.Size(52, 20);
			this.FExportMenuItem.Text = "E&xport";
			// 
			// schemaOnlyToolStripMenuItem
			// 
			schemaOnlyToolStripMenuItem.Name = "schemaOnlyToolStripMenuItem";
			schemaOnlyToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			schemaOnlyToolStripMenuItem.Text = "&Schema Only...";
			schemaOnlyToolStripMenuItem.Click += new System.EventHandler(this.ExportSchemaClicked);
			// 
			// dataOnlyToolStripMenuItem
			// 
			dataOnlyToolStripMenuItem.Name = "dataOnlyToolStripMenuItem";
			dataOnlyToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			dataOnlyToolStripMenuItem.Text = "&Data Only...";
			dataOnlyToolStripMenuItem.Click += new System.EventHandler(this.ExportDataClicked);
			// 
			// schemaAndDataToolStripMenuItem
			// 
			schemaAndDataToolStripMenuItem.Name = "schemaAndDataToolStripMenuItem";
			schemaAndDataToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			schemaAndDataToolStripMenuItem.Text = "&Schema and Data...";
			schemaAndDataToolStripMenuItem.Click += new System.EventHandler(this.ExportSchemaAndDataClicked);
			// 
			// toolStripButton2
			// 
			toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton2.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Prepare;
			toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton2.Name = "toolStripButton2";
			toolStripButton2.Size = new System.Drawing.Size(23, 22);
			toolStripButton2.Text = "Prepare";
			toolStripButton2.Click += new System.EventHandler(this.PrepareClicked);
			// 
			// toolStripButton3
			// 
			toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton3.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Analyze;
			toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton3.Name = "toolStripButton3";
			toolStripButton3.Size = new System.Drawing.Size(23, 22);
			toolStripButton3.Text = "Analyze";
			toolStripButton3.Click += new System.EventHandler(this.AnalyzeClicked);
			// 
			// FMenuStrip
			// 
			this.FMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FScriptMenuItem,
            this.FExportMenuItem});
			// 
			// FToolStrip
			// 
			this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FExecuteButton,
            toolStripButton2,
            toolStripButton3,
            this.FCancelExecuteButton});
			// 
			// FExecuteButton
			// 
			this.FExecuteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FExecuteButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Execute;
			this.FExecuteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FExecuteButton.Name = "FExecuteButton";
			this.FExecuteButton.Size = new System.Drawing.Size(23, 22);
			this.FExecuteButton.Text = "Execute";
			this.FExecuteButton.Click += new System.EventHandler(this.ExecuteClicked);
			// 
			// FCancelExecuteButton
			// 
			this.FCancelExecuteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FCancelExecuteButton.Enabled = false;
			this.FCancelExecuteButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.CancelExecute;
			this.FCancelExecuteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FCancelExecuteButton.Name = "FCancelExecuteButton";
			this.FCancelExecuteButton.Size = new System.Drawing.Size(23, 22);
			this.FCancelExecuteButton.Text = "Cancel Execute";
			this.FCancelExecuteButton.Click += new System.EventHandler(this.CancelExecuteClicked);
			// 
			// D4Editor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(455, 376);
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "D4Editor";
			this.Controls.SetChildIndex(this.FDockPanel, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

        }
        #endregion

		private System.Windows.Forms.ToolStripMenuItem FExecuteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FCancelExecuteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FExecuteLineMenuItem;
		private System.Windows.Forms.ToolStripButton FExecuteButton;
		private System.Windows.Forms.ToolStripButton FCancelExecuteButton;
		private System.Windows.Forms.ToolStripMenuItem FScriptMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FExportMenuItem;


    }
}
