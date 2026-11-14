namespace Alphora.Dataphor.Dataphoria
{
	partial class StackView
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
			this._stackSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this._stackDataView = new Alphora.Dataphor.DAE.Client.DataView(this.components);
			this.FToolStrip = new System.Windows.Forms.ToolStrip();
			this.FRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.dbGrid1 = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			this.FContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.FRefreshContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.FCopyButton = new System.Windows.Forms.ToolStripButton();
			this.FCopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this._stackDataView)).BeginInit();
			this.FToolStrip.SuspendLayout();
			this.FContextMenu.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// _stackSource
			// 
			this._stackSource.DataSet = this._stackDataView;
			// 
			// _stackDataView
			// 
			this._stackDataView.Expression = ".System.Debug.GetStack(AProcessID, ACallStackIndex) \r\n\tbrowse by { Index }";
			this._stackDataView.IsReadOnly = true;
			this._stackDataView.RequestedCapabilities = ((Alphora.Dataphor.DAE.CursorCapability)((((Alphora.Dataphor.DAE.CursorCapability.Navigable | Alphora.Dataphor.DAE.CursorCapability.BackwardsNavigable)
						| Alphora.Dataphor.DAE.CursorCapability.Bookmarkable)
						| Alphora.Dataphor.DAE.CursorCapability.Searchable)));
			this._stackDataView.SessionName = "";
			this._stackDataView.DataChanged += new System.EventHandler(this._stackDataView_DataChanged);
			// 
			// FToolStrip
			// 
			this.FToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.FToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FRefreshButton,
            this.FCopyButton});
			this.FToolStrip.Location = new System.Drawing.Point(3, 0);
			this.FToolStrip.Name = "FToolStrip";
			this.FToolStrip.Size = new System.Drawing.Size(58, 25);
			this.FToolStrip.TabIndex = 0;
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
			this.dbGrid1.Source = this._stackSource;
			this.dbGrid1.TabIndex = 0;
			this.dbGrid1.Text = "dbGrid1";
			// 
			// FContextMenu
			// 
			this.FContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FRefreshContextMenuItem,
            this.FCopyMenuItem});
			this.FContextMenu.Name = "FContextMenu";
			this.FContextMenu.Size = new System.Drawing.Size(246, 48);
			// 
			// FRefreshContextMenuItem
			// 
			this.FRefreshContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Refresh;
			this.FRefreshContextMenuItem.Name = "FRefreshContextMenuItem";
			this.FRefreshContextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.FRefreshContextMenuItem.Size = new System.Drawing.Size(245, 22);
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
			// FCopyButton
			// 
			this.FCopyButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FCopyButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
			this.FCopyButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FCopyButton.Name = "FCopyButton";
			this.FCopyButton.Size = new System.Drawing.Size(23, 22);
			this.FCopyButton.Text = "Copy Value to Clipboard";
			this.FCopyButton.Click += new System.EventHandler(this.FCopyMenuItem_Click);
			// 
			// FCopyMenuItem
			// 
			this.FCopyMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Copy;
			this.FCopyMenuItem.Name = "FCopyMenuItem";
			this.FCopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.FCopyMenuItem.Size = new System.Drawing.Size(245, 22);
			this.FCopyMenuItem.Text = "&Copy Value to Clipboard";
			this.FCopyMenuItem.Click += new System.EventHandler(this.FCopyMenuItem_Click);
			// 
			// StackView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.toolStripContainer1);
			this.Name = "StackView";
			this.Size = new System.Drawing.Size(869, 256);
			((System.ComponentModel.ISupportInitialize)(this._stackDataView)).EndInit();
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

		private Alphora.Dataphor.DAE.Client.DataSource _stackSource;
		private Alphora.Dataphor.DAE.Client.DataView _stackDataView;
		private System.Windows.Forms.ToolStrip FToolStrip;
		private System.Windows.Forms.ToolStripButton FRefreshButton;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid dbGrid1;
		private System.Windows.Forms.ContextMenuStrip FContextMenu;
		private System.Windows.Forms.ToolStripMenuItem FRefreshContextMenuItem;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.ToolStripButton FCopyButton;
		private System.Windows.Forms.ToolStripMenuItem FCopyMenuItem;
	}
}