namespace Alphora.Dataphor.Dataphoria
{
	partial class SessionView
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
			this.FSessionSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this.panel1 = new System.Windows.Forms.Panel();
			this.FAttachButton = new System.Windows.Forms.Button();
			this.FRefreshButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.FSessionDataView)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// FSessionDataView
			// 
			this.FSessionDataView.Expression = "\tSessions { ID, User_ID, HostName, Connection_Name }\r\n\t\twhere (ID <> SessionID())" +
				" and (ID <> 0)\r\n\t\tleft join (GetSessions() { Session_ID ID }) include rowexists " +
				"IsAttached\r\n";
			this.FSessionDataView.IsReadOnly = true;
			this.FSessionDataView.RequestedCapabilities = ((Alphora.Dataphor.DAE.CursorCapability)((((Alphora.Dataphor.DAE.CursorCapability.Navigable | Alphora.Dataphor.DAE.CursorCapability.BackwardsNavigable)
						| Alphora.Dataphor.DAE.CursorCapability.Bookmarkable)
						| Alphora.Dataphor.DAE.CursorCapability.Searchable)));
			this.FSessionDataView.SessionName = "";
			// 
			// dbGrid1
			// 
			this.dbGrid1.CausesValidation = false;
			this.dbGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dbGrid1.Location = new System.Drawing.Point(0, 0);
			this.dbGrid1.Name = "dbGrid1";
			this.dbGrid1.Size = new System.Drawing.Size(863, 228);
			this.dbGrid1.Source = this.FSessionSource;
			this.dbGrid1.TabIndex = 0;
			this.dbGrid1.Text = "dbGrid1";
			// 
			// FSessionSource
			// 
			this.FSessionSource.DataSet = this.FSessionDataView;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.FRefreshButton);
			this.panel1.Controls.Add(this.FAttachButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(706, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(157, 228);
			this.panel1.TabIndex = 1;
			// 
			// FAttachButton
			// 
			this.FAttachButton.Location = new System.Drawing.Point(5, 3);
			this.FAttachButton.Name = "FAttachButton";
			this.FAttachButton.Size = new System.Drawing.Size(149, 26);
			this.FAttachButton.TabIndex = 0;
			this.FAttachButton.Text = "Attach";
			this.FAttachButton.UseVisualStyleBackColor = true;
			this.FAttachButton.Click += new System.EventHandler(this.FAttachButton_Click);
			// 
			// FRefreshButton
			// 
			this.FRefreshButton.Location = new System.Drawing.Point(5, 33);
			this.FRefreshButton.Name = "FRefreshButton";
			this.FRefreshButton.Size = new System.Drawing.Size(148, 24);
			this.FRefreshButton.TabIndex = 1;
			this.FRefreshButton.Text = "Refresh";
			this.FRefreshButton.UseVisualStyleBackColor = true;
			this.FRefreshButton.Click += new System.EventHandler(this.FRefreshButton_Click);
			// 
			// SessionView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.dbGrid1);
			this.Name = "SessionView";
			this.Size = new System.Drawing.Size(863, 228);
			((System.ComponentModel.ISupportInitialize)(this.FSessionDataView)).EndInit();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Alphora.Dataphor.DAE.Client.DataView FSessionDataView;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid dbGrid1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button FAttachButton;
		private Alphora.Dataphor.DAE.Client.DataSource FSessionSource;
		private System.Windows.Forms.Button FRefreshButton;

	}
}
