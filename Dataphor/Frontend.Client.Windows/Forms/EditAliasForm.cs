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
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Form for adding and editing Dataphor instance configurations. </summary>
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
		private Label label8;
		private TabPage tpInProcessAlias;
		private Label label11;
		private Label label6;
		private Label label10;
		private ComboBox cbInstanceName;
		private ComboBox cbInProcessInstanceName;
		private Button EditInstanceButton;
		private Button NewInstanceButton;
		private System.Windows.Forms.CheckBox cbEmbedded;
		private System.Windows.Forms.TextBox tbOverridePortNumber;
		private Label label2;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditAliasForm));
			this.SessionInfoPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.tbAliasName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.AdvancedButton = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.cbIsUserAlias = new System.Windows.Forms.CheckBox();
			this.tcAliasType = new System.Windows.Forms.TabControl();
			this.tpConnectionAlias = new System.Windows.Forms.TabPage();
			this.tbOverridePortNumber = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.cbInstanceName = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.cbClientSideLogging = new System.Windows.Forms.CheckBox();
			this.tbHost = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.tpInProcessAlias = new System.Windows.Forms.TabPage();
			this.cbEmbedded = new System.Windows.Forms.CheckBox();
			this.EditInstanceButton = new System.Windows.Forms.Button();
			this.NewInstanceButton = new System.Windows.Forms.Button();
			this.cbInProcessInstanceName = new System.Windows.Forms.ComboBox();
			this.label10 = new System.Windows.Forms.Label();
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
			this.FContentPanel.Size = new System.Drawing.Size(325, 501);
			// 
			// SessionInfoPropertyGrid
			// 
			this.SessionInfoPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.SessionInfoPropertyGrid.Location = new System.Drawing.Point(8, 313);
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
			this.label1.Location = new System.Drawing.Point(16, 11);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Alias Name";
			// 
			// AdvancedButton
			// 
			this.AdvancedButton.BackColor = System.Drawing.Color.Transparent;
			this.AdvancedButton.Location = new System.Drawing.Point(8, 284);
			this.AdvancedButton.Name = "AdvancedButton";
			this.AdvancedButton.Size = new System.Drawing.Size(88, 23);
			this.AdvancedButton.TabIndex = 7;
			this.AdvancedButton.UseVisualStyleBackColor = false;
			this.AdvancedButton.Click += new System.EventHandler(this.AdvancedButton_Click);
			// 
			// cbIsUserAlias
			// 
			this.cbIsUserAlias.AutoSize = true;
			this.cbIsUserAlias.Location = new System.Drawing.Point(8, 261);
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
			this.tcAliasType.Size = new System.Drawing.Size(300, 212);
			this.tcAliasType.TabIndex = 6;
			// 
			// tpConnectionAlias
			// 
			this.tpConnectionAlias.Controls.Add(this.tbOverridePortNumber);
			this.tpConnectionAlias.Controls.Add(this.label2);
			this.tpConnectionAlias.Controls.Add(this.cbInstanceName);
			this.tpConnectionAlias.Controls.Add(this.label6);
			this.tpConnectionAlias.Controls.Add(this.cbClientSideLogging);
			this.tpConnectionAlias.Controls.Add(this.tbHost);
			this.tpConnectionAlias.Controls.Add(this.label7);
			this.tpConnectionAlias.Controls.Add(this.label8);
			this.tpConnectionAlias.Location = new System.Drawing.Point(4, 22);
			this.tpConnectionAlias.Name = "tpConnectionAlias";
			this.tpConnectionAlias.Padding = new System.Windows.Forms.Padding(3);
			this.tpConnectionAlias.Size = new System.Drawing.Size(292, 186);
			this.tpConnectionAlias.TabIndex = 0;
			this.tpConnectionAlias.Text = "Connect";
			this.tpConnectionAlias.UseVisualStyleBackColor = true;
			// 
			// tbOverridePortNumber
			// 
			this.tbOverridePortNumber.Location = new System.Drawing.Point(78, 129);
			this.tbOverridePortNumber.Name = "tbOverridePortNumber";
			this.tbOverridePortNumber.Size = new System.Drawing.Size(183, 20);
			this.tbOverridePortNumber.TabIndex = 19;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 132);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(69, 13);
			this.label2.TabIndex = 20;
			this.label2.Text = "Override Port";
			// 
			// cbInstanceName
			// 
			this.cbInstanceName.FormattingEnabled = true;
			this.cbInstanceName.Location = new System.Drawing.Point(78, 102);
			this.cbInstanceName.Name = "cbInstanceName";
			this.cbInstanceName.Size = new System.Drawing.Size(183, 21);
			this.cbInstanceName.TabIndex = 18;
			this.cbInstanceName.DropDown += new System.EventHandler(this.cbInstanceName_DropDown);
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(6, 7);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(276, 57);
			this.label6.TabIndex = 17;
			this.label6.Text = resources.GetString("label6.Text");
			// 
			// cbClientSideLogging
			// 
			this.cbClientSideLogging.Location = new System.Drawing.Point(9, 163);
			this.cbClientSideLogging.Name = "cbClientSideLogging";
			this.cbClientSideLogging.Size = new System.Drawing.Size(168, 17);
			this.cbClientSideLogging.TabIndex = 16;
			this.cbClientSideLogging.Text = "Client-Side Logging Enabled";
			// 
			// tbHost
			// 
			this.tbHost.Location = new System.Drawing.Point(78, 76);
			this.tbHost.Name = "tbHost";
			this.tbHost.Size = new System.Drawing.Size(183, 20);
			this.tbHost.TabIndex = 12;
			this.tbHost.TextChanged += new System.EventHandler(this.tbHost_TextChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 79);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(29, 13);
			this.label7.TabIndex = 15;
			this.label7.Text = "Host";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 105);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(48, 13);
			this.label8.TabIndex = 14;
			this.label8.Text = "Instance";
			// 
			// tpInProcessAlias
			// 
			this.tpInProcessAlias.Controls.Add(this.cbEmbedded);
			this.tpInProcessAlias.Controls.Add(this.EditInstanceButton);
			this.tpInProcessAlias.Controls.Add(this.NewInstanceButton);
			this.tpInProcessAlias.Controls.Add(this.cbInProcessInstanceName);
			this.tpInProcessAlias.Controls.Add(this.label10);
			this.tpInProcessAlias.Controls.Add(this.label11);
			this.tpInProcessAlias.Location = new System.Drawing.Point(4, 22);
			this.tpInProcessAlias.Name = "tpInProcessAlias";
			this.tpInProcessAlias.Padding = new System.Windows.Forms.Padding(3);
			this.tpInProcessAlias.Size = new System.Drawing.Size(292, 186);
			this.tpInProcessAlias.TabIndex = 1;
			this.tpInProcessAlias.Text = "In-Process";
			this.tpInProcessAlias.UseVisualStyleBackColor = true;
			// 
			// cbEmbedded
			// 
			this.cbEmbedded.AutoSize = true;
			this.cbEmbedded.Location = new System.Drawing.Point(11, 108);
			this.cbEmbedded.Name = "cbEmbedded";
			this.cbEmbedded.Size = new System.Drawing.Size(117, 17);
			this.cbEmbedded.TabIndex = 50;
			this.cbEmbedded.Text = "Embedded Server?";
			this.cbEmbedded.UseVisualStyleBackColor = true;
			// 
			// EditInstanceButton
			// 
			this.EditInstanceButton.Location = new System.Drawing.Point(133, 72);
			this.EditInstanceButton.Name = "EditInstanceButton";
			this.EditInstanceButton.Size = new System.Drawing.Size(45, 21);
			this.EditInstanceButton.TabIndex = 49;
			this.EditInstanceButton.Text = "Edit...";
			this.EditInstanceButton.UseVisualStyleBackColor = true;
			this.EditInstanceButton.Click += new System.EventHandler(this.EditInstanceButton_Click);
			// 
			// NewInstanceButton
			// 
			this.NewInstanceButton.Location = new System.Drawing.Point(82, 72);
			this.NewInstanceButton.Name = "NewInstanceButton";
			this.NewInstanceButton.Size = new System.Drawing.Size(45, 21);
			this.NewInstanceButton.TabIndex = 48;
			this.NewInstanceButton.Text = "New...";
			this.NewInstanceButton.UseVisualStyleBackColor = true;
			this.NewInstanceButton.Click += new System.EventHandler(this.NewInstanceButton_Click);
			// 
			// cbInProcessInstanceName
			// 
			this.cbInProcessInstanceName.FormattingEnabled = true;
			this.cbInProcessInstanceName.Location = new System.Drawing.Point(82, 45);
			this.cbInProcessInstanceName.Name = "cbInProcessInstanceName";
			this.cbInProcessInstanceName.Size = new System.Drawing.Size(155, 21);
			this.cbInProcessInstanceName.TabIndex = 47;
			this.cbInProcessInstanceName.DropDown += new System.EventHandler(this.cbInProcessInstanceName_DropDown);
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(8, 7);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(278, 28);
			this.label10.TabIndex = 46;
			this.label10.Text = "Start a new server in-process using the following configuration:";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(6, 48);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(48, 13);
			this.label11.TabIndex = 32;
			this.label11.Text = "Instance";
			// 
			// EditAliasForm
			// 
			this.ClientSize = new System.Drawing.Size(325, 572);
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
				cbInProcessInstanceName.Text = LInProcess.InstanceName;
				cbEmbedded.Checked = LInProcess.IsEmbedded;
			}
			else
			{
				tcAliasType.SelectedTab = tpConnectionAlias;
				tbHost.Text = LConnection.HostName;
				cbInstanceName.Text = LConnection.InstanceName;
				tbOverridePortNumber.Text = LConnection.OverridePortNumber.ToString();
				cbClientSideLogging.Checked = LConnection.ClientSideLoggingEnabled;
			}
		}

		public ServerAlias CreateAlias()
		{
			ServerAlias LResult;
			if (tcAliasType.SelectedTab == tpInProcessAlias)
			{
				InProcessAlias LInProcess = new InProcessAlias();
				LInProcess.InstanceName = cbInProcessInstanceName.Text;
				LInProcess.IsEmbedded = cbEmbedded.Checked;
				LResult = LInProcess;
			}
			else
			{
				ConnectionAlias LConnection = new ConnectionAlias();
				LConnection.HostName = tbHost.Text;
				LConnection.InstanceName = cbInstanceName.Text;
				LConnection.OverridePortNumber = Int32.Parse(tbOverridePortNumber.Text);
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

		private bool FInstancesEnumerated;

		private void cbInstanceName_DropDown(object sender, EventArgs e)
		{
			if ((tbHost.Text != String.Empty) && (!FInstancesEnumerated))
			{
				try
				{
					FInstancesEnumerated = true;
					cbInstanceName.Items.Clear();
					string[] LInstanceNames = ServerFactory.EnumerateInstances(tbHost.Text);
					for (int LIndex = 0; LIndex < LInstanceNames.Length; LIndex++)
						cbInstanceName.Items.Add(LInstanceNames[LIndex]);
				}
				catch (Exception LException)
				{
					MessageBox.Show(String.Format("Could not enumerate instances for host \"{0}\".\r\n{1}", tbHost.Text, LException.Message));
				}
			}
		}

		private void tbHost_TextChanged(object sender, EventArgs e)
		{
			FInstancesEnumerated = false;
		}
		
		private bool FLocalInstancesEnumerated;
		
		private void LoadInstances(InstanceConfiguration AConfiguration)
		{
			FLocalInstancesEnumerated = true;
			for (int LIndex = 0; LIndex < AConfiguration.Instances.Count; LIndex++)
				cbInProcessInstanceName.Items.Add(AConfiguration.Instances[LIndex].Name);
		}

		private void cbInProcessInstanceName_DropDown(object sender, EventArgs e)
		{
			if (!FLocalInstancesEnumerated)
				LoadInstances(InstanceManager.LoadConfiguration());
		}

		private void NewInstanceButton_Click(object sender, EventArgs e)
		{
			ServerConfiguration LInstance = EditInstanceForm.ExecuteAdd();
			InstanceConfiguration LConfiguration = InstanceManager.LoadConfiguration();
			LConfiguration.Instances.Add(LInstance);
			InstanceManager.SaveConfiguration(LConfiguration);

			FLocalInstancesEnumerated = false;
			LoadInstances(LConfiguration);

			cbInProcessInstanceName.Text = LInstance.Name;
		}

		private void EditInstanceButton_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrEmpty(cbInProcessInstanceName.Text))
			{
				InstanceConfiguration LConfiguration = InstanceManager.LoadConfiguration();
				ServerConfiguration LInstance = LConfiguration.Instances[cbInProcessInstanceName.Text];
				if (LInstance == null)
				{
					LInstance = new ServerConfiguration();
					LInstance.Name = cbInProcessInstanceName.Text;
				}
				else
					LConfiguration.Instances.Remove(LInstance.Name);
				
				LInstance = EditInstanceForm.ExecuteEdit(LInstance);
				
				LConfiguration.Instances.Add(LInstance);
				InstanceManager.SaveConfiguration(LConfiguration);

				FLocalInstancesEnumerated = false;
				LoadInstances(LConfiguration);
				
				cbInProcessInstanceName.Text = LInstance.Name;
			}
		}
	}
}
