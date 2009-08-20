namespace Alphora.Dataphor.Dataphoria
{
	partial class ViewCallStack
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
			this.FCallStackDataView = new Alphora.Dataphor.DAE.Client.DataView();
			this.panel1 = new System.Windows.Forms.Panel();
			this.FRefreshButton = new System.Windows.Forms.Button();
			this.dbGrid1 = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			((System.ComponentModel.ISupportInitialize)(this.FCallStackDataView)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// FCallStackSource
			// 
			this.FCallStackSource.DataSet = this.FCallStackDataView;
			// 
			// FCallStackDataView
			// 
			this.FCallStackDataView.Expression = "\tSessions { ID, User_ID, HostName, Connection_Name }\r\n\t\twhere (ID <> SessionID())" +
				" and (ID <> 0)\r\n\t\tleft join (GetSessions() { Session_ID ID }) include rowexists " +
				"IsAttached\r\n";
			this.FCallStackDataView.IsReadOnly = true;
			this.FCallStackDataView.RequestedCapabilities = ((Alphora.Dataphor.DAE.CursorCapability)((((Alphora.Dataphor.DAE.CursorCapability.Navigable | Alphora.Dataphor.DAE.CursorCapability.BackwardsNavigable)
						| Alphora.Dataphor.DAE.CursorCapability.Bookmarkable)
						| Alphora.Dataphor.DAE.CursorCapability.Searchable)));
			this.FCallStackDataView.SessionName = "";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.FRefreshButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(712, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(157, 256);
			this.panel1.TabIndex = 3;
			// 
			// FRefreshButton
			// 
			this.FRefreshButton.Location = new System.Drawing.Point(3, 3);
			this.FRefreshButton.Name = "FRefreshButton";
			this.FRefreshButton.Size = new System.Drawing.Size(148, 24);
			this.FRefreshButton.TabIndex = 1;
			this.FRefreshButton.Text = "Refresh";
			this.FRefreshButton.UseVisualStyleBackColor = true;
			// 
			// dbGrid1
			// 
			this.dbGrid1.CausesValidation = false;
			this.dbGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dbGrid1.Location = new System.Drawing.Point(0, 0);
			this.dbGrid1.Name = "dbGrid1";
			this.dbGrid1.Size = new System.Drawing.Size(869, 256);
			this.dbGrid1.Source = this.FCallStackSource;
			this.dbGrid1.TabIndex = 2;
			this.dbGrid1.Text = "dbGrid1";
			// 
			// ViewCallStack
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(869, 256);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.dbGrid1);
			this.Name = "ViewCallStack";
			this.Text = "ViewCallStack";
			((System.ComponentModel.ISupportInitialize)(this.FCallStackDataView)).EndInit();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Alphora.Dataphor.DAE.Client.DataSource FCallStackSource;
		private Alphora.Dataphor.DAE.Client.DataView FCallStackDataView;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button FRefreshButton;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid dbGrid1;
	}
}