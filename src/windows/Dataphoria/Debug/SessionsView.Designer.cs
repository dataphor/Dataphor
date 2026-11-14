namespace Alphora.Dataphor.Dataphoria
{
	partial class SessionsView
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
			this.FSessionDataView = new Alphora.Dataphor.DAE.Client.DataView(this.components);
			this.dbGrid1 = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			this.FContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.FAttachContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FDetachContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FRefreshContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FSessionSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.FToolStrip = new System.Windows.Forms.ToolStrip();
			this.FAttachButton = new System.Windows.Forms.ToolStripButton();
			this.FDetachButton = new System.Windows.Forms.ToolStripButton();
			this.FRefreshButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.FSessionDataView)).BeginInit();
			this.FContextMenu.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.FToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// FSessionDataView
			// 
			this.FSessionDataView.Expression = "\t.System.Sessions { ID, User_ID, HostName, Connection_Name }\r\n\t\twhere (ID <> Sess" +
				"ionID()) and (ID <> 0)\r\n\t\tleft join (GetSessions() { Session_ID ID }) include ro" +
				"wexists IsAttached\r\n\t\tbrowse by { ID }";
			this.FSessionDataView.IsReadOnly = true;
			this.FSessionDataView.RequestedCapabilities = ((Alphora.Dataphor.DAE.CursorCapability)((((Alphora.Dataphor.DAE.CursorCapability.Navigable | Alphora.Dataphor.DAE.CursorCapability.BackwardsNavigable)
						| Alphora.Dataphor.DAE.CursorCapability.Bookmarkable)
						| Alphora.Dataphor.DAE.CursorCapability.Searchable)));
			this.FSessionDataView.SessionName = "";
			this.FSessionDataView.DataChanged += new System.EventHandler(this.FSessionDataView_DataChanged);
			// 
			// dbGrid1
			// 
			this.dbGrid1.CausesValidation = false;
			this.dbGrid1.ContextMenuStrip = this.FContextMenu;
			this.dbGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dbGrid1.Location = new System.Drawing.Point(0, 0);
			this.dbGrid1.Name = "dbGrid1";
			this.dbGrid1.Size = new System.Drawing.Size(863, 203);
			this.dbGrid1.Source = this.FSessionSource;
			this.dbGrid1.TabIndex = 0;
			this.dbGrid1.Text = "dbGrid1";
			this.dbGrid1.DoubleClick += new System.EventHandler(this.FAttachButton_Click);
			// 
			// FContextMenu
			// 
			this.FContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FAttachContextMenuItem,
            this.FDetachContextMenuItem,
            this.FRefreshContextMenuItem});
			this.FContextMenu.Name = "FContextMenu";
			this.FContextMenu.Size = new System.Drawing.Size(154, 70);
			// 
			// FAttachContextMenuItem
			// 
			this.FAttachContextMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugAttach;
			this.FAttachContextMenuItem.Name = "FAttachContextMenuItem";
			this.FAttachContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.FAttachContextMenuItem.Size = new System.Drawing.Size(153, 22);
			this.FAttachContextMenuItem.Text = "Attach";
			this.FAttachContextMenuItem.Click += new System.EventHandler(this.FAttachButton_Click);
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
			// FSessionSource
			// 
			this.FSessionSource.DataSet = this.FSessionDataView;
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
            this.FAttachButton,
            this.FDetachButton,
            this.FRefreshButton});
			this.FToolStrip.Location = new System.Drawing.Point(3, 0);
			this.FToolStrip.Name = "FToolStrip";
			this.FToolStrip.Size = new System.Drawing.Size(81, 25);
			this.FToolStrip.TabIndex = 0;
			// 
			// FAttachButton
			// 
			this.FAttachButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FAttachButton.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.DebugAttach;
			this.FAttachButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FAttachButton.Name = "FAttachButton";
			this.FAttachButton.Size = new System.Drawing.Size(23, 22);
			this.FAttachButton.Text = "Attach";
			this.FAttachButton.Click += new System.EventHandler(this.FAttachButton_Click);
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
			// SessionsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.toolStripContainer1);
			this.Name = "SessionsView";
			this.Size = new System.Drawing.Size(863, 228);
			((System.ComponentModel.ISupportInitialize)(this.FSessionDataView)).EndInit();
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

		private Alphora.Dataphor.DAE.Client.DataView FSessionDataView;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid dbGrid1;
		private Alphora.Dataphor.DAE.Client.DataSource FSessionSource;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.ToolStrip FToolStrip;
		private System.Windows.Forms.ToolStripButton FAttachButton;
		private System.Windows.Forms.ToolStripButton FRefreshButton;
		private System.Windows.Forms.ContextMenuStrip FContextMenu;
		private System.Windows.Forms.ToolStripMenuItem FAttachContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FRefreshContextMenuItem;
		private System.Windows.Forms.ToolStripButton FDetachButton;
		private System.Windows.Forms.ToolStripMenuItem FDetachContextMenuItem;

	}
}
