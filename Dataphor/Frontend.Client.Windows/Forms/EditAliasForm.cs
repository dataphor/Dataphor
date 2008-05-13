/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Form for adding and editing ServerAlias instances. </summary>
	public class EditAliasForm : BaseForm
	{
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.PropertyGrid SessionInfoPropertyGrid;
		private System.Windows.Forms.Button AdvancedButton;
		private System.Windows.Forms.TextBox tbAliasName;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private TabControl tcAliasType;
		private TabPage tpConnectionAlias;
		private System.Windows.Forms.CheckBox cbClientSideLogging;
		private System.Windows.Forms.TextBox tbHost;
		private Label label7;
		private System.Windows.Forms.TextBox tbPortNumber;
		private Label label8;
		private TabPage tpInProcessAlias;
		private Label label5;
		private Label label3;
		private System.Windows.Forms.CheckBox cbEmbedded;
		private System.Windows.Forms.CheckBox cbLogErrors;
		private System.Windows.Forms.CheckBox cbTracingEnabled;
		private Button button1;
		private System.Windows.Forms.TextBox tbLibraryDirectory;
		private Label label9;
		private System.Windows.Forms.TextBox tbInProcessPortNumber;
		private Label label11;
		private System.Windows.Forms.TextBox tbCatalogStoreClassName;
		private Label label6;
		private Label label10;
		private System.Windows.Forms.TextBox tbCatalogStoreConnectionString;
		private System.Windows.Forms.CheckBox cbIsUserAlias;

		public EditAliasForm()
		{
			InitializeComponent();

			SessionInfoPropertyGrid.SelectedObject = FSessionInfo;

			// so that the prop grid gets hidden.
			Size LSize = ClientSize;
			ClientSize = new Size(LSize.Width, LSize.Height - (SessionInfoPropertyGrid.Height + 12));
			SessionInfoPropertyGrid.Visible = false;
			AdvancedButton.Text = Strings.CAdvancedButton;
			FStatusBar.Visible = false;
			SetAcceptReject(true, false);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SessionInfoPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.tbAliasName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.AdvancedButton = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.cbIsUserAlias = new System.Windows.Forms.CheckBox();
			this.tcAliasType = new System.Windows.Forms.TabControl();
			this.tpConnectionAlias = new System.Windows.Forms.TabPage();
			this.label6 = new System.Windows.Forms.Label();
			this.cbClientSideLogging = new System.Windows.Forms.CheckBox();
			this.tbHost = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.tbPortNumber = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.tpInProcessAlias = new System.Windows.Forms.TabPage();
			this.tbCatalogStoreConnectionString = new System.Windows.Forms.TextBox();
			this.label10 = new System.Windows.Forms.Label();
			this.tbCatalogStoreClassName = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.cbEmbedded = new System.Windows.Forms.CheckBox();
			this.cbLogErrors = new System.Windows.Forms.CheckBox();
			this.cbTracingEnabled = new System.Windows.Forms.CheckBox();
			this.button1 = new System.Windows.Forms.Button();
			this.tbLibraryDirectory = new System.Windows.Forms.TextBox();
			this.label9 = new System.Windows.Forms.Label();
			this.tbInProcessPortNumber = new System.Windows.Forms.TextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.FContentPanel.SuspendLayout();
			this.tcAliasType.SuspendLayout();
			this.tpConnectionAlias.SuspendLayout();
			this.tpInProcessAlias.SuspendLayout();
			this.SuspendLayout();
			// 
			// FContentPanel
			// 
			this.FContentPanel.AutoScroll = true;
			this.FContentPanel.Controls.Add(this.label1);
			this.FContentPanel.Controls.Add(this.cbIsUserAlias);
			this.FContentPanel.Controls.Add(this.AdvancedButton);
			this.FContentPanel.Controls.Add(this.tcAliasType);
			this.FContentPanel.Controls.Add(this.tbAliasName);
			this.FContentPanel.Controls.Add(this.SessionInfoPropertyGrid);
			this.FContentPanel.Size = new System.Drawing.Size(325, 546);
			this.FContentPanel.Controls.SetChildIndex(this.SessionInfoPropertyGrid, 0);
			this.FContentPanel.Controls.SetChildIndex(this.tbAliasName, 0);
			this.FContentPanel.Controls.SetChildIndex(this.tcAliasType, 0);
			this.FContentPanel.Controls.SetChildIndex(this.AdvancedButton, 0);
			this.FContentPanel.Controls.SetChildIndex(this.cbIsUserAlias, 0);
			this.FContentPanel.Controls.SetChildIndex(this.label1, 0);
			// 
			// SessionInfoPropertyGrid
			// 
			this.SessionInfoPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.SessionInfoPropertyGrid.Location = new System.Drawing.Point(8, 358);
			this.SessionInfoPropertyGrid.Name = "SessionInfoPropertyGrid";
			this.SessionInfoPropertyGrid.Size = new System.Drawing.Size(309, 181);
			this.SessionInfoPropertyGrid.TabIndex = 8;
			this.SessionInfoPropertyGrid.ToolbarVisible = false;
			// 
			// tbAliasName
			// 
			this.tbAliasName.Location = new System.Drawing.Point(88, 8);
			this.tbAliasName.Name = "tbAliasName";
			this.tbAliasName.Size = new System.Drawing.Size(224, 20);
			this.tbAliasName.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Location = new System.Drawing.Point(16, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Alias Name";
			// 
			// AdvancedButton
			// 
			this.AdvancedButton.BackColor = System.Drawing.Color.Transparent;
			this.AdvancedButton.Location = new System.Drawing.Point(8, 329);
			this.AdvancedButton.Name = "AdvancedButton";
			this.AdvancedButton.Size = new System.Drawing.Size(88, 23);
			this.AdvancedButton.TabIndex = 7;
			this.AdvancedButton.UseVisualStyleBackColor = false;
			this.AdvancedButton.Click += new System.EventHandler(this.AdvancedButton_Click);
			// 
			// cbIsUserAlias
			// 
			this.cbIsUserAlias.AutoSize = true;
			this.cbIsUserAlias.Location = new System.Drawing.Point(8, 306);
			this.cbIsUserAlias.Name = "cbIsUserAlias";
			this.cbIsUserAlias.Size = new System.Drawing.Size(225, 17);
			this.cbIsUserAlias.TabIndex = 9;
			this.cbIsUserAlias.Text = "Allow this alias to be used by this user only";
			this.cbIsUserAlias.UseVisualStyleBackColor = true;
			// 
			// tcAliasType
			// 
			this.tcAliasType.Controls.Add(this.tpConnectionAlias);
			this.tcAliasType.Controls.Add(this.tpInProcessAlias);
			this.tcAliasType.Location = new System.Drawing.Point(12, 34);
			this.tcAliasType.Name = "tcAliasType";
			this.tcAliasType.SelectedIndex = 0;
			this.tcAliasType.Size = new System.Drawing.Size(300, 266);
			this.tcAliasType.TabIndex = 6;
			this.tcAliasType.Selected += new System.Windows.Forms.TabControlEventHandler(this.tcAliasType_Selected);
			// 
			// tpConnectionAlias
			// 
			this.tpConnectionAlias.Controls.Add(this.label6);
			this.tpConnectionAlias.Controls.Add(this.cbClientSideLogging);
			this.tpConnectionAlias.Controls.Add(this.tbHost);
			this.tpConnectionAlias.Controls.Add(this.label7);
			this.tpConnectionAlias.Controls.Add(this.tbPortNumber);
			this.tpConnectionAlias.Controls.Add(this.label8);
			this.tpConnectionAlias.Location = new System.Drawing.Point(4, 22);
			this.tpConnectionAlias.Name = "tpConnectionAlias";
			this.tpConnectionAlias.Padding = new System.Windows.Forms.Padding(3);
			this.tpConnectionAlias.Size = new System.Drawing.Size(292, 240);
			this.tpConnectionAlias.TabIndex = 0;
			this.tpConnectionAlias.Text = "Connect";
			this.tpConnectionAlias.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(8, 7);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(276, 30);
			this.label6.TabIndex = 17;
			this.label6.Text = "Connect to an existing server using a machine name (Host) and Port Number.";
			// 
			// cbClientSideLogging
			// 
			this.cbClientSideLogging.Location = new System.Drawing.Point(6, 95);
			this.cbClientSideLogging.Name = "cbClientSideLogging";
			this.cbClientSideLogging.Size = new System.Drawing.Size(168, 24);
			this.cbClientSideLogging.TabIndex = 16;
			this.cbClientSideLogging.Text = "Client-Side Logging Enabled";
			// 
			// tbHost
			// 
			this.tbHost.Location = new System.Drawing.Point(78, 45);
			this.tbHost.Name = "tbHost";
			this.tbHost.Size = new System.Drawing.Size(183, 20);
			this.tbHost.TabIndex = 12;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 45);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(29, 13);
			this.label7.TabIndex = 15;
			this.label7.Text = "Host";
			// 
			// tbPortNumber
			// 
			this.tbPortNumber.Location = new System.Drawing.Point(78, 69);
			this.tbPortNumber.Name = "tbPortNumber";
			this.tbPortNumber.Size = new System.Drawing.Size(183, 20);
			this.tbPortNumber.TabIndex = 13;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 69);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(66, 13);
			this.label8.TabIndex = 14;
			this.label8.Text = "Port Number";
			// 
			// tpInProcessAlias
			// 
			this.tpInProcessAlias.Controls.Add(this.tbCatalogStoreConnectionString);
			this.tpInProcessAlias.Controls.Add(this.label10);
			this.tpInProcessAlias.Controls.Add(this.tbCatalogStoreClassName);
			this.tpInProcessAlias.Controls.Add(this.label5);
			this.tpInProcessAlias.Controls.Add(this.label3);
			this.tpInProcessAlias.Controls.Add(this.cbEmbedded);
			this.tpInProcessAlias.Controls.Add(this.cbLogErrors);
			this.tpInProcessAlias.Controls.Add(this.cbTracingEnabled);
			this.tpInProcessAlias.Controls.Add(this.button1);
			this.tpInProcessAlias.Controls.Add(this.tbLibraryDirectory);
			this.tpInProcessAlias.Controls.Add(this.label9);
			this.tpInProcessAlias.Controls.Add(this.tbInProcessPortNumber);
			this.tpInProcessAlias.Controls.Add(this.label11);
			this.tpInProcessAlias.Location = new System.Drawing.Point(4, 22);
			this.tpInProcessAlias.Name = "tpInProcessAlias";
			this.tpInProcessAlias.Padding = new System.Windows.Forms.Padding(3);
			this.tpInProcessAlias.Size = new System.Drawing.Size(292, 240);
			this.tpInProcessAlias.TabIndex = 1;
			this.tpInProcessAlias.Text = "In-Process";
			this.tpInProcessAlias.UseVisualStyleBackColor = true;
			// 
			// tbCatalogStoreConnectionString
			// 
			this.tbCatalogStoreConnectionString.Location = new System.Drawing.Point(8, 168);
			this.tbCatalogStoreConnectionString.Name = "tbCatalogStoreConnectionString";
			this.tbCatalogStoreConnectionString.PasswordChar = '*';
			this.tbCatalogStoreConnectionString.Size = new System.Drawing.Size(278, 20);
			this.tbCatalogStoreConnectionString.TabIndex = 11;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(8, 7);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(278, 28);
			this.label10.TabIndex = 46;
			this.label10.Text = "Start a new server in-process using the following configuration:";
			// 
			// tbCatalogStoreClassName
			// 
			this.tbCatalogStoreClassName.Location = new System.Drawing.Point(8, 129);
			this.tbCatalogStoreClassName.Name = "tbCatalogStoreClassName";
			this.tbCatalogStoreClassName.Size = new System.Drawing.Size(278, 20);
			this.tbCatalogStoreClassName.TabIndex = 7;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(8, 152);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(158, 13);
			this.label5.TabIndex = 40;
			this.label5.Text = "Catalog Store Connection String";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 113);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(130, 13);
			this.label3.TabIndex = 38;
			this.label3.Text = "Catalog Store Class Name";
			// 
			// cbEmbedded
			// 
			this.cbEmbedded.AutoSize = true;
			this.cbEmbedded.Location = new System.Drawing.Point(196, 198);
			this.cbEmbedded.Name = "cbEmbedded";
			this.cbEmbedded.Size = new System.Drawing.Size(77, 17);
			this.cbEmbedded.TabIndex = 5;
			this.cbEmbedded.Text = "Embedded";
			this.cbEmbedded.UseVisualStyleBackColor = true;
			// 
			// cbLogErrors
			// 
			this.cbLogErrors.Location = new System.Drawing.Point(114, 194);
			this.cbLogErrors.Name = "cbLogErrors";
			this.cbLogErrors.Size = new System.Drawing.Size(76, 24);
			this.cbLogErrors.TabIndex = 4;
			this.cbLogErrors.Text = "Log Errors";
			// 
			// cbTracingEnabled
			// 
			this.cbTracingEnabled.Location = new System.Drawing.Point(8, 194);
			this.cbTracingEnabled.Name = "cbTracingEnabled";
			this.cbTracingEnabled.Size = new System.Drawing.Size(111, 24);
			this.cbTracingEnabled.TabIndex = 3;
			this.cbTracingEnabled.Text = "Tracing Enabled";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(264, 90);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(24, 22);
			this.button1.TabIndex = 29;
			this.button1.Text = "...";
			this.button1.Click += new System.EventHandler(this.LibraryDirectoryLookupButton_Click);
			// 
			// tbLibraryDirectory
			// 
			this.tbLibraryDirectory.Location = new System.Drawing.Point(8, 90);
			this.tbLibraryDirectory.Name = "tbLibraryDirectory";
			this.tbLibraryDirectory.Size = new System.Drawing.Size(256, 20);
			this.tbLibraryDirectory.TabIndex = 2;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(8, 74);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(83, 13);
			this.label9.TabIndex = 33;
			this.label9.Text = "Library Directory";
			// 
			// tbInProcessPortNumber
			// 
			this.tbInProcessPortNumber.Location = new System.Drawing.Point(8, 51);
			this.tbInProcessPortNumber.Name = "tbInProcessPortNumber";
			this.tbInProcessPortNumber.Size = new System.Drawing.Size(184, 20);
			this.tbInProcessPortNumber.TabIndex = 0;
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(8, 35);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(66, 13);
			this.label11.TabIndex = 32;
			this.label11.Text = "Port Number";
			// 
			// EditAliasForm
			// 
			this.ClientSize = new System.Drawing.Size(325, 617);
			this.ControlBox = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "EditAliasForm";
			this.ShowInTaskbar = false;
			this.Text = "Server Alias";
			this.FContentPanel.ResumeLayout(false);
			this.FContentPanel.PerformLayout();
			this.tcAliasType.ResumeLayout(false);
			this.tpConnectionAlias.ResumeLayout(false);
			this.tpConnectionAlias.PerformLayout();
			this.tpInProcessAlias.ResumeLayout(false);
			this.tpInProcessAlias.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		public static ServerAlias ExecuteAdd()
		{
			return ExecuteEdit(new ConnectionAlias());	// create from a defaulted alias
		}

		public static ServerAlias ExecuteEdit(ServerAlias AAlias)
		{
			using (EditAliasForm LForm = new EditAliasForm())
			{
				LForm.SetFromAlias(AAlias);
				if (LForm.ShowDialog() != DialogResult.OK)
					throw new AbortException();
				return LForm.CreateAlias();
			}
		}

		private DAE.SessionInfo FSessionInfo = new DAE.SessionInfo();

		public void SetFromAlias(ServerAlias AAlias)
		{
			tbAliasName.Text = AAlias.Name;
			CopyInstance(AAlias.SessionInfo, FSessionInfo);
			SessionInfoPropertyGrid.Refresh();
			cbIsUserAlias.Checked = AAlias.IsUserAlias;

			InProcessAlias LInProcess = AAlias as InProcessAlias;
			ConnectionAlias LConnection = AAlias as ConnectionAlias;
			if (LInProcess != null)
			{
				tcAliasType.SelectedTab = tpInProcessAlias;
				tbInProcessPortNumber.Text = AAlias.PortNumber.ToString();
				//StartupScriptUriTextBox.Text = LInProcess.StartupScriptUri;
				//tbCatalogDirectory.Text = LInProcess.CatalogDirectory;
				tbCatalogStoreClassName.Text = LInProcess.CatalogStoreClassName;
				tbCatalogStoreConnectionString.Text = LInProcess.CatalogStoreConnectionString;
				//cbCatalogStoreShared.Checked = LInProcess.CatalogStoreShared;
				tbLibraryDirectory.Text = LInProcess.LibraryDirectory;
				cbTracingEnabled.Checked = LInProcess.TracingEnabled;
				cbLogErrors.Checked = LInProcess.LogErrors;
				cbEmbedded.Checked = LInProcess.IsEmbedded;
			}
			else
			{
				tcAliasType.SelectedTab = tpConnectionAlias;
				tbPortNumber.Text = AAlias.PortNumber.ToString();
				tbHost.Text = LConnection.HostName;
				cbClientSideLogging.Checked = LConnection.ClientSideLoggingEnabled;
			}
		}

		public ServerAlias CreateAlias()
		{
			ServerAlias LResult;
			if (tcAliasType.SelectedTab == tpInProcessAlias)
			{
				InProcessAlias LInProcess = new InProcessAlias();
				LInProcess.PortNumber = Int32.Parse(tbInProcessPortNumber.Text);
				//LInProcess.StartupScriptUri = StartupScriptUriTextBox.Text;
				//LInProcess.CatalogDirectory = tbCatalogDirectory.Text;
				LInProcess.CatalogStoreClassName = tbCatalogStoreClassName.Text;
				LInProcess.CatalogStoreConnectionString = tbCatalogStoreConnectionString.Text;
				//LInProcess.CatalogStoreShared = cbCatalogStoreShared.Checked;
				LInProcess.LibraryDirectory = tbLibraryDirectory.Text;
				LInProcess.TracingEnabled = cbTracingEnabled.Checked;
				LInProcess.LogErrors = cbLogErrors.Checked;
				LInProcess.IsEmbedded = cbEmbedded.Checked;
				LResult = LInProcess;
			}
			else
			{
				ConnectionAlias LConnection = new ConnectionAlias();
				LConnection.PortNumber = Int32.Parse(tbPortNumber.Text);
				LConnection.HostName = tbHost.Text;
				LConnection.ClientSideLoggingEnabled = cbClientSideLogging.Checked;
				LResult = LConnection;
			}
			
			LResult.Name = tbAliasName.Text;
			LResult.IsUserAlias = cbIsUserAlias.Checked;
			CopyInstance(FSessionInfo, LResult.SessionInfo);

			return LResult;
		}

		private void CopyInstance(object ASource, object ADestination)
		{
			System.Xml.XmlDocument LDocument = new System.Xml.XmlDocument();
			new BOP.Serializer().Serialize(LDocument, ASource);
			new BOP.Deserializer().Deserialize(LDocument, ADestination);
		}

		private void AdvancedButton_Click(object sender, System.EventArgs e)
		{
			Size LSize = ClientSize;
			if (SessionInfoPropertyGrid.Visible)
			{
				ClientSize = new Size(LSize.Width, LSize.Height - (SessionInfoPropertyGrid.Height + 12));
				SessionInfoPropertyGrid.Visible = false;
				AdvancedButton.Text = Strings.CAdvancedButton;
			}
			else
			{
				SessionInfoPropertyGrid.Visible = true;
				ClientSize = new Size(LSize.Width, LSize.Height + (SessionInfoPropertyGrid.Height + 12));
				AdvancedButton.Text = Strings.CBasicButton;
			}		
		}

		private void PortNumberConnectionTextBox_TextChanged(object sender, System.EventArgs e)
		{
			Int32.Parse(tbPortNumber.Text);
		}

		private void PortNumberInProcessTextBox_TextChanged(object sender, System.EventArgs e)
		{
			Int32.Parse(tbInProcessPortNumber.Text);
		}

		private void LibraryDirectoryLookupButton_Click(object sender, System.EventArgs e)
		{
			tbLibraryDirectory.Text = FolderUtility.GetDirectory(tbLibraryDirectory.Text);
		}

		private void tcAliasType_Selected(object sender, TabControlEventArgs e)
		{
			if ((e.TabPage == tpInProcessAlias) && (tbInProcessPortNumber.Text == String.Empty))
				tbInProcessPortNumber.Text = tbPortNumber.Text;
			else if ((e.TabPage == tpConnectionAlias) && (tbPortNumber.Text == String.Empty))
				tbPortNumber.Text = tbInProcessPortNumber.Text;
		}
	}
}
