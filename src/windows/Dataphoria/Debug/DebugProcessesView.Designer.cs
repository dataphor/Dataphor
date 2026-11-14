namespace Alphora.Dataphor.Dataphoria
{
	partial class DebugProcessesView
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
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.FDebugProcessDataView = new Alphora.Dataphor.DAE.Client.DataView(this.components);
			this.dbGrid1 = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			this.FContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.FSelectContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDetachContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FRefreshContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDebugProcessSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.FToolStrip = new System.Windows.Forms.ToolStrip();
			this.FSelectButton = new System.Windows.Forms.ToolStripButton();
			this.FDetachButton = new System.Windows.Forms.ToolStripButton();
			this.FRefreshButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.FDebugProcessDataView)).BeginInit();
			this.FContextMenu.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.FToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// FDebugProcessDataView
			// 
			this.FDebugProcessDataView.Expression = ".System.Debug.GetProcesses()\r\n\tadd { (Process_ID = ASelectedProcessID) IsSelected" +
				" }\r\n\tbrowse by { Process_ID }";
			this.FDebugProcessDataView.IsReadOnly = true;
			this.FDebugProcessDataView.RequestedCapabilities = ((Alphora.Dataphor.DAE.CursorCapability)((((Alphora.Dataphor.DAE.CursorCapability.Navigable | Alphora.Dataphor.DAE.CursorCapability.BackwardsNavigable)
						| Alphora.Dataphor.DAE.CursorCapability.Bookmarkable)
						| Alphora.Dataphor.DAE.CursorCapability.Searchable)));
			this.FDebugProcessDataView.SessionName = "";
			this.FDebugProcessDataView.DataChanged += new System.EventHandler(this.FDebugProcessDataView_DataChanged);
			// 
			// dbGrid1
			// 
			this.dbGrid1.CausesValidation = false;
			this.dbGrid1.ContextMenuStrip = this.FContextMenu;
			this.dbGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dbGrid1.Location = new System.Drawing.Point(0, 0);
			this.dbGrid1.Name = "dbGrid1";
			this.dbGrid1.Size = new System.Drawing.Size(863, 203);
			this.dbGrid1.Source = this.FDebugProcessSource;
			this.dbGrid1.TabIndex = 0;
			this.dbGrid1.Text = "dbGrid1";
			this.dbGrid1.DoubleClick += new System.EventHandler(this.FSelectButton_Click);
			// 
			// FContextMenu
			// 
			this.FContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSelectContextMenuItem,
            this.FDetachContextMenuItem,
            this.FRefreshContextMenuItem});
			this.FContextMenu.Name = "FContextMenu";
			this.FContextMenu.Size = new System.Drawing.Size(154, 70);
			// 
			// FSelectContextMenuItem
			// 
			this.FSelectContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
			this.FSelectContextMenuItem.Name = "FSelectContextMenuItem";
			this.FSelectContextMenuItem.Size = new System.Drawing.Size(153, 22);
			this.FSelectContextMenuItem.Text = "Select";
			this.FSelectContextMenuItem.Click += new System.EventHandler(this.FSelectButton_Click);
			// 
			// FDetachContextMenuItem
			// 
			this.FDetachContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugDetach;
			this.FDetachContextMenuItem.Name = "FDetachContextMenuItem";
			this.FDetachContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.FDetachContextMenuItem.Size = new System.Drawing.Size(153, 22);
			this.FDetachContextMenuItem.Text = "Detach";
			this.FDetachContextMenuItem.Click += new System.EventHandler(this.FDetachButton_Click);
			// 
			// FRefreshContextMenuItem
			// 
			this.FRefreshContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Refresh;
			this.FRefreshContextMenuItem.Name = "FRefreshContextMenuItem";
			this.FRefreshContextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.FRefreshContextMenuItem.Size = new System.Drawing.Size(153, 22);
			this.FRefreshContextMenuItem.Text = "Refresh";
			this.FRefreshContextMenuItem.Click += new System.EventHandler(this.FRefreshButton_Click);
			// 
			// FDebugProcessSource
			// 
			this.FDebugProcessSource.DataSet = this.FDebugProcessDataView;
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.AutoScroll = true;
			this.toolStripContainer1.ContentPanel.Controls.Add(this.dbGrid1);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(863, 203);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(863, 228);
			this.toolStripContainer1.TabIndex = 2;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.FToolStrip);
			// 
			// FToolStrip
			// 
			this.FToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSelectButton,
            this.FDetachButton,
            this.FRefreshButton});
			this.FToolStrip.Location = new System.Drawing.Point(3, 0);
			this.FToolStrip.Name = "FToolStrip";
			this.FToolStrip.Size = new System.Drawing.Size(81, 25);
			this.FToolStrip.TabIndex = 0;
			// 
			// FSelectButton
			// 
			this.FSelectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FSelectButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
			this.FSelectButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FSelectButton.Name = "FSelectButton";
			this.FSelectButton.Size = new System.Drawing.Size(23, 22);
			this.FSelectButton.Text = "Select";
			this.FSelectButton.Click += new System.EventHandler(this.FSelectButton_Click);
			// 
			// FDetachButton
			// 
			this.FDetachButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FDetachButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugDetach;
			this.FDetachButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FDetachButton.Name = "FDetachButton";
			this.FDetachButton.Size = new System.Drawing.Size(23, 22);
			this.FDetachButton.Text = "Detach";
			this.FDetachButton.Click += new System.EventHandler(this.FDetachButton_Click);
			// 
			// FRefreshButton
			// 
			this.FRefreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FRefreshButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Refresh;
			this.FRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FRefreshButton.Name = "FRefreshButton";
			this.FRefreshButton.Size = new System.Drawing.Size(23, 22);
			this.FRefreshButton.Text = "Refresh";
			this.FRefreshButton.Click += new System.EventHandler(this.FRefreshButton_Click);
			// 
			// DebugProcessesView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.toolStripContainer1);
			this.Name = "DebugProcessesView";
			this.Size = new System.Drawing.Size(863, 228);
			((System.ComponentModel.ISupportInitialize)(this.FDebugProcessDataView)).EndInit();
			this.FContextMenu.ResumeLayout(false);
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.FToolStrip.ResumeLayout(false);
			this.FToolStrip.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private Alphora.Dataphor.DAE.Client.DataView FDebugProcessDataView;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid dbGrid1;
		private Alphora.Dataphor.DAE.Client.DataSource FDebugProcessSource;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.ToolStrip FToolStrip;
		private System.Windows.Forms.ToolStripButton FRefreshButton;
		private System.Windows.Forms.ContextMenuStrip FContextMenu;
		private System.Windows.Forms.ToolStripMenuItem FRefreshContextMenuItem;
		private System.Windows.Forms.ToolStripButton FDetachButton;
		private System.Windows.Forms.ToolStripMenuItem FDetachContextMenuItem;
		private System.Windows.Forms.ToolStripButton FSelectButton;
		private System.Windows.Forms.ToolStripMenuItem FSelectContextMenuItem;

	}
}
