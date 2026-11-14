namespace Alphora.Dataphor.Dataphoria
{
	partial class CallStackView
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.FCallStackSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this.FCallStackDataView = new Alphora.Dataphor.DAE.Client.DataView(this.components);
			this.FToolStrip = new System.Windows.Forms.ToolStrip();
			this.FSelectButton = new System.Windows.Forms.ToolStripButton();
			this.FRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.dbGrid1 = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			this.FContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.FSelectContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FRefreshContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			((System.ComponentModel.ISupportInitialize)(this.FCallStackDataView)).BeginInit();
			this.FToolStrip.SuspendLayout();
			this.FContextMenu.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// FCallStackSource
			// 
			this.FCallStackSource.DataSet = this.FCallStackDataView;
			// 
			// FCallStackDataView
			// 
			this.FCallStackDataView.Expression = ".System.Debug.GetCallStack(AProcessID) \r\n\tadd { IfNil(Index = ASelectedIndex, fal" +
				"se) IsSelected }\r\n\tbrowse by { Index }";
			this.FCallStackDataView.IsReadOnly = true;
			this.FCallStackDataView.RequestedCapabilities = ((Alphora.Dataphor.DAE.CursorCapability)((((Alphora.Dataphor.DAE.CursorCapability.Navigable | Alphora.Dataphor.DAE.CursorCapability.BackwardsNavigable)
						| Alphora.Dataphor.DAE.CursorCapability.Bookmarkable)
						| Alphora.Dataphor.DAE.CursorCapability.Searchable)));
			this.FCallStackDataView.SessionName = "";
			this.FCallStackDataView.DataChanged += new System.EventHandler(this.FCallStackDataView_DataChanged);
			// 
			// FToolStrip
			// 
			this.FToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSelectButton,
            this.FRefreshButton});
			this.FToolStrip.Location = new System.Drawing.Point(3, 0);
			this.FToolStrip.Name = "FToolStrip";
			this.FToolStrip.Size = new System.Drawing.Size(58, 25);
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
			// dbGrid1
			// 
			this.dbGrid1.CausesValidation = false;
			this.dbGrid1.ContextMenuStrip = this.FContextMenu;
			this.dbGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dbGrid1.Location = new System.Drawing.Point(0, 0);
			this.dbGrid1.Name = "dbGrid1";
			this.dbGrid1.Size = new System.Drawing.Size(869, 231);
			this.dbGrid1.Source = this.FCallStackSource;
			this.dbGrid1.TabIndex = 0;
			this.dbGrid1.Text = "dbGrid1";
			this.dbGrid1.DoubleClick += new System.EventHandler(this.FSelectButton_Click);
			// 
			// FContextMenu
			// 
			this.FContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FSelectContextMenuItem,
            this.FRefreshContextMenuItem});
			this.FContextMenu.Name = "FContextMenu";
			this.FContextMenu.Size = new System.Drawing.Size(133, 48);
			// 
			// FSelectContextMenuItem
			// 
			this.FSelectContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugCallStack;
			this.FSelectContextMenuItem.Name = "FSelectContextMenuItem";
			this.FSelectContextMenuItem.Size = new System.Drawing.Size(132, 22);
			this.FSelectContextMenuItem.Text = "Select";
			this.FSelectContextMenuItem.Click += new System.EventHandler(this.FSelectButton_Click);
			// 
			// FRefreshContextMenuItem
			// 
			this.FRefreshContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Refresh;
			this.FRefreshContextMenuItem.Name = "FRefreshContextMenuItem";
			this.FRefreshContextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.FRefreshContextMenuItem.Size = new System.Drawing.Size(132, 22);
			this.FRefreshContextMenuItem.Text = "Refresh";
			this.FRefreshContextMenuItem.Click += new System.EventHandler(this.FRefreshButton_Click);
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.AutoScroll = true;
			this.toolStripContainer1.ContentPanel.Controls.Add(this.dbGrid1);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(869, 231);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(869, 256);
			this.toolStripContainer1.TabIndex = 3;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.FToolStrip);
			// 
			// CallStackView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.toolStripContainer1);
			this.Name = "CallStackView";
			this.Size = new System.Drawing.Size(869, 256);
			((System.ComponentModel.ISupportInitialize)(this.FCallStackDataView)).EndInit();
			this.FToolStrip.ResumeLayout(false);
			this.FToolStrip.PerformLayout();
			this.FContextMenu.ResumeLayout(false);
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private Alphora.Dataphor.DAE.Client.DataSource FCallStackSource;
		private Alphora.Dataphor.DAE.Client.DataView FCallStackDataView;
		private System.Windows.Forms.ToolStrip FToolStrip;
		private System.Windows.Forms.ToolStripButton FSelectButton;
		private System.Windows.Forms.ToolStripButton FRefreshButton;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid dbGrid1;
		private System.Windows.Forms.ContextMenuStrip FContextMenu;
		private System.Windows.Forms.ToolStripMenuItem FSelectContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FRefreshContextMenuItem;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
	}
}