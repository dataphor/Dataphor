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
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Listener;
using Alphora.Dataphor.DAE.Contracts;

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
		private ComboBox _cbInProcessInstanceName;
		private Button EditInstanceButton;
		private Button NewInstanceButton;
		private System.Windows.Forms.CheckBox cbEmbedded;
		private System.Windows.Forms.TextBox tbOverridePortNumber;
		private Label label2;
		private Label label3;
		private ComboBox cbSecurityMode;
		private Label label5;
		private Label label4;
		private ComboBox cbListenerSecurityMode;
		private System.Windows.Forms.TextBox tbOverrideListenerPortNumber;
		private System.Windows.Forms.TextBox tbEndpointConfigurationName;
		private Label label9;
		private Label label12;
		private System.Windows.Forms.CheckBox cbIsUserAlias;

		public EditAliasForm()
		{
			InitializeComponent();

			SessionInfoPropertyGrid.SelectedObject = _sessionInfo;

			// so that the prop grid gets hidden.
			Size size = ClientSize;
			ClientSize = new Size(size.Width, size.Height - (SessionInfoPropertyGrid.Height + 12));
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
			this.label12 = new System.Windows.Forms.Label();
			this.tbEndpointConfigurationName = new System.Windows.Forms.TextBox();
			this.label9 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.cbListenerSecurityMode = new System.Windows.Forms.ComboBox();
			this.tbOverrideListenerPortNumber = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.cbSecurityMode = new System.Windows.Forms.ComboBox();
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
			this._cbInProcessInstanceName = new System.Windows.Forms.ComboBox();
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
			this.FContentPanel.Size = new System.Drawing.Size(325, 625);
			// 
			// SessionInfoPropertyGrid
			// 
			this.SessionInfoPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.SessionInfoPropertyGrid.Location = new System.Drawing.Point(13, 434);
			this.SessionInfoPropertyGrid.Name = "SessionInfoPropertyGrid";
			this.SessionInfoPropertyGrid.Size = new System.Drawing.Size(299, 181);
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
			this.AdvancedButton.Location = new System.Drawing.Point(12, 405);
			this.AdvancedButton.Name = "AdvancedButton";
			this.AdvancedButton.Size = new System.Drawing.Size(88, 23);
			this.AdvancedButton.TabIndex = 7;
			this.AdvancedButton.UseVisualStyleBackColor = false;
			this.AdvancedButton.Click += new System.EventHandler(this.AdvancedButton_Click);
			// 
			// cbIsUserAlias
			// 
			this.cbIsUserAlias.AutoSize = true;
			this.cbIsUserAlias.Location = new System.Drawing.Point(12, 382);
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
			this.tcAliasType.Size = new System.Drawing.Size(300, 342);
			this.tcAliasType.TabIndex = 6;
			// 
			// tpConnectionAlias
			// 
			this.tpConnectionAlias.Controls.Add(this.label12);
			this.tpConnectionAlias.Controls.Add(this.tbEndpointConfigurationName);
			this.tpConnectionAlias.Controls.Add(this.label9);
			this.tpConnectionAlias.Controls.Add(this.label5);
			this.tpConnectionAlias.Controls.Add(this.label4);
			this.tpConnectionAlias.Controls.Add(this.cbListenerSecurityMode);
			this.tpConnectionAlias.Controls.Add(this.tbOverrideListenerPortNumber);
			this.tpConnectionAlias.Controls.Add(this.label3);
			this.tpConnectionAlias.Controls.Add(this.cbSecurityMode);
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
			this.tpConnectionAlias.Size = new System.Drawing.Size(292, 316);
			this.tpConnectionAlias.TabIndex = 0;
			this.tpConnectionAlias.Text = "Connect";
			this.tpConnectionAlias.UseVisualStyleBackColor = true;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(6, 197);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(276, 57);
			this.label12.TabIndex = 29;
			this.label12.Text = "If a configuration name is specified, it will override all settings and the conne" +
    "ction will be based on the information specified in the configuration file for t" +
    "he named endpoint.";
			// 
			// tbEndpointConfigurationName
			// 
			this.tbEndpointConfigurationName.Location = new System.Drawing.Point(87, 261);
			this.tbEndpointConfigurationName.Name = "tbEndpointConfigurationName";
			this.tbEndpointConfigurationName.Size = new System.Drawing.Size(174, 20);
			this.tbEndpointConfigurationName.TabIndex = 27;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(6, 264);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(68, 13);
			this.label9.TabIndex = 28;
			this.label9.Text = "Config Name";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(102, 122);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(48, 13);
			this.label5.TabIndex = 26;
			this.label5.Text = "Instance";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(202, 122);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(44, 13);
			this.label4.TabIndex = 25;
			this.label4.Text = "Listener";
			// 
			// cbListenerSecurityMode
			// 
			this.cbListenerSecurityMode.FormattingEnabled = true;
			this.cbListenerSecurityMode.Items.AddRange(new object[] {
            "Default",
            "None",
            "Transport"});
			this.cbListenerSecurityMode.Location = new System.Drawing.Point(184, 169);
			this.cbListenerSecurityMode.Name = "cbListenerSecurityMode";
			this.cbListenerSecurityMode.Size = new System.Drawing.Size(77, 21);
			this.cbListenerSecurityMode.TabIndex = 24;
			// 
			// tbOverrideListenerPortNumber
			// 
			this.tbOverrideListenerPortNumber.Location = new System.Drawing.Point(184, 141);
			this.tbOverrideListenerPortNumber.Name = "tbOverrideListenerPortNumber";
			this.tbOverrideListenerPortNumber.Size = new System.Drawing.Size(77, 20);
			this.tbOverrideListenerPortNumber.TabIndex = 23;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 172);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(75, 13);
			this.label3.TabIndex = 22;
			this.label3.Text = "Security Mode";
			// 
			// cbSecurityMode
			// 
			this.cbSecurityMode.FormattingEnabled = true;
			this.cbSecurityMode.Items.AddRange(new object[] {
            "Default",
            "None",
            "Transport"});
			this.cbSecurityMode.Location = new System.Drawing.Point(87, 169);
			this.cbSecurityMode.Name = "cbSecurityMode";
			this.cbSecurityMode.Size = new System.Drawing.Size(77, 21);
			this.cbSecurityMode.TabIndex = 21;
			// 
			// tbOverridePortNumber
			// 
			this.tbOverridePortNumber.Location = new System.Drawing.Point(87, 141);
			this.tbOverridePortNumber.Name = "tbOverridePortNumber";
			this.tbOverridePortNumber.Size = new System.Drawing.Size(77, 20);
			this.tbOverridePortNumber.TabIndex = 19;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 144);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(69, 13);
			this.label2.TabIndex = 20;
			this.label2.Text = "Override Port";
			// 
			// cbInstanceName
			// 
			this.cbInstanceName.FormattingEnabled = true;
			this.cbInstanceName.Location = new System.Drawing.Point(87, 93);
			this.cbInstanceName.Name = "cbInstanceName";
			this.cbInstanceName.Size = new System.Drawing.Size(174, 21);
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
			this.cbClientSideLogging.Location = new System.Drawing.Point(9, 293);
			this.cbClientSideLogging.Name = "cbClientSideLogging";
			this.cbClientSideLogging.Size = new System.Drawing.Size(168, 17);
			this.cbClientSideLogging.TabIndex = 16;
			this.cbClientSideLogging.Text = "Client-Side Logging Enabled";
			// 
			// tbHost
			// 
			this.tbHost.Location = new System.Drawing.Point(87, 67);
			this.tbHost.Name = "tbHost";
			this.tbHost.Size = new System.Drawing.Size(174, 20);
			this.tbHost.TabIndex = 12;
			this.tbHost.TextChanged += new System.EventHandler(this.tbHost_TextChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 70);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(29, 13);
			this.label7.TabIndex = 15;
			this.label7.Text = "Host";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 96);
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
			this.tpInProcessAlias.Controls.Add(this._cbInProcessInstanceName);
			this.tpInProcessAlias.Controls.Add(this.label10);
			this.tpInProcessAlias.Controls.Add(this.label11);
			this.tpInProcessAlias.Location = new System.Drawing.Point(4, 22);
			this.tpInProcessAlias.Name = "tpInProcessAlias";
			this.tpInProcessAlias.Padding = new System.Windows.Forms.Padding(3);
			this.tpInProcessAlias.Size = new System.Drawing.Size(292, 316);
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
			// _cbInProcessInstanceName
			// 
			this._cbInProcessInstanceName.FormattingEnabled = true;
			this._cbInProcessInstanceName.Location = new System.Drawing.Point(82, 45);
			this._cbInProcessInstanceName.Name = "_cbInProcessInstanceName";
			this._cbInProcessInstanceName.Size = new System.Drawing.Size(155, 21);
			this._cbInProcessInstanceName.TabIndex = 47;
			this._cbInProcessInstanceName.DropDown += new System.EventHandler(this.cbInProcessInstanceName_DropDown);
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
			this.ClientSize = new System.Drawing.Size(325, 696);
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

		public static ServerAlias ExecuteEdit(ServerAlias alias)
		{
			using (EditAliasForm form = new EditAliasForm())
			{
				form.SetFromAlias(alias);
				if (form.ShowDialog() != DialogResult.OK)
					throw new AbortException();
				return form.CreateAlias();
			}
		}

		private DAE.SessionInfo _sessionInfo = new DAE.SessionInfo();

		public int OverridePortNumber
		{
			get 
			{
				int portNumber;
				return Int32.TryParse(tbOverridePortNumber.Text, out portNumber) ? portNumber : 0;
			}
			set { tbOverridePortNumber.Text = value.ToString(); }
		}
		
		public ConnectionSecurityMode SecurityMode
		{
			get { return (ConnectionSecurityMode)Math.Min(Math.Max(cbSecurityMode.SelectedIndex, 0), 2); }
			set { cbSecurityMode.SelectedIndex = (int)value; }
		}
		
		public int OverrideListenerPortNumber
		{
			get
			{
				int portNumber;
				return Int32.TryParse(tbOverrideListenerPortNumber.Text, out portNumber) ? portNumber : 0;
			}
			set { tbOverrideListenerPortNumber.Text = value.ToString(); }
		}
		
		public ConnectionSecurityMode ListenerSecurityMode
		{
			get { return (ConnectionSecurityMode)Math.Min(Math.Max(cbListenerSecurityMode.SelectedIndex, 0), 2); }
			set { cbListenerSecurityMode.SelectedIndex = (int)value; }
		}

		public void SetFromAlias(ServerAlias alias)
		{
			tbAliasName.Text = alias.Name;
			CopyInstance(alias.SessionInfo, _sessionInfo);
			SessionInfoPropertyGrid.Refresh();
			cbIsUserAlias.Checked = alias.IsUserAlias;

			InProcessAlias inProcess = alias as InProcessAlias;
			ConnectionAlias connection = alias as ConnectionAlias;
			if (inProcess != null)
			{
				tcAliasType.SelectedTab = tpInProcessAlias;
				_cbInProcessInstanceName.Text = inProcess.InstanceName;
				cbEmbedded.Checked = inProcess.IsEmbedded;
			}
			else
			{
				tcAliasType.SelectedTab = tpConnectionAlias;
				tbHost.Text = connection.HostName;
				cbInstanceName.Text = connection.InstanceName;
				OverridePortNumber = connection.OverridePortNumber;
				SecurityMode = connection.SecurityMode;
				OverrideListenerPortNumber = connection.OverrideListenerPortNumber;
				ListenerSecurityMode = connection.ListenerSecurityMode;
				tbEndpointConfigurationName.Text = connection.ClientConfigurationName;
				cbClientSideLogging.Checked = connection.ClientSideLoggingEnabled;
			}
		}
		
		public ServerAlias CreateAlias()
		{
			ServerAlias result;
			if (tcAliasType.SelectedTab == tpInProcessAlias)
			{
				InProcessAlias inProcess = new InProcessAlias();
				inProcess.InstanceName = _cbInProcessInstanceName.Text;
				inProcess.IsEmbedded = cbEmbedded.Checked;
				result = inProcess;
			}
			else
			{
				ConnectionAlias connection = new ConnectionAlias();
				connection.HostName = tbHost.Text;
				connection.InstanceName = cbInstanceName.Text;
				connection.OverridePortNumber = OverridePortNumber;
				connection.SecurityMode = SecurityMode;
				connection.OverrideListenerPortNumber = OverrideListenerPortNumber;
				connection.ListenerSecurityMode = ListenerSecurityMode;
				connection.ClientConfigurationName = tbEndpointConfigurationName.Text;
				connection.ClientSideLoggingEnabled = cbClientSideLogging.Checked;
				result = connection;
			}
			
			result.Name = tbAliasName.Text;
			result.IsUserAlias = cbIsUserAlias.Checked;
			CopyInstance(_sessionInfo, result.SessionInfo);

			return result;
		}

		private void CopyInstance(object source, object destination)
		{
			System.Xml.Linq.XDocument document = new System.Xml.Linq.XDocument();
			new BOP.Serializer().Serialize(document, source);
			new BOP.Deserializer().Deserialize(document, destination);
		}

		private void AdvancedButton_Click(object sender, System.EventArgs e)
		{
			Size size = ClientSize;
			if (SessionInfoPropertyGrid.Visible)
			{
				ClientSize = new Size(size.Width, size.Height - (SessionInfoPropertyGrid.Height + 12));
				SessionInfoPropertyGrid.Visible = false;
				AdvancedButton.Text = Strings.CAdvancedButton;
			}
			else
			{
				SessionInfoPropertyGrid.Visible = true;
				ClientSize = new Size(size.Width, size.Height + (SessionInfoPropertyGrid.Height + 12));
				AdvancedButton.Text = Strings.CBasicButton;
			}		
		}

		private bool _instancesEnumerated;

		private void cbInstanceName_DropDown(object sender, EventArgs e)
		{
			if ((tbHost.Text != String.Empty) && (!_instancesEnumerated))
			{
				try
				{
					_instancesEnumerated = true;
					cbInstanceName.Items.Clear();
					string[] instanceNames = ListenerFactory.EnumerateInstances(tbHost.Text, OverrideListenerPortNumber);
					for (int index = 0; index < instanceNames.Length; index++)
						cbInstanceName.Items.Add(instanceNames[index]);
				}
				catch (Exception exception)
				{
					_instancesEnumerated = false;
					MessageBox.Show(String.Format("Could not enumerate instances for host \"{0}\".\r\n{1}", tbHost.Text, exception.Message));
				}
			}
		}

		private void tbHost_TextChanged(object sender, EventArgs e)
		{
			_instancesEnumerated = false;
		}
		
		private bool _localInstancesEnumerated;
		
		private void LoadInstances(InstanceConfiguration configuration)
		{
			_localInstancesEnumerated = true;
		    InstanceList instances = configuration.Instances;
            _cbInProcessInstanceName.Items.Clear();
		    for (int index = 0; index < instances.Count; index++)
			{
			    _cbInProcessInstanceName.Items.Add(instances[index].Name);
			}
		}

		private void cbInProcessInstanceName_DropDown(object sender, EventArgs e)
		{
			if (!_localInstancesEnumerated)
				LoadInstances(InstanceManager.LoadConfiguration());
		}

		private void NewInstanceButton_Click(object sender, EventArgs e)
		{
			ServerConfiguration instance = EditInstanceForm.ExecuteAdd();
			InstanceConfiguration configuration = InstanceManager.LoadConfiguration();
			configuration.Instances.Add(instance);
			InstanceManager.SaveConfiguration(configuration);

			_localInstancesEnumerated = false;
			LoadInstances(configuration);

			_cbInProcessInstanceName.Text = instance.Name;
		}

		private void EditInstanceButton_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrEmpty(_cbInProcessInstanceName.Text))
			{
				InstanceConfiguration configuration = InstanceManager.LoadConfiguration();
				ServerConfiguration instance = configuration.Instances[_cbInProcessInstanceName.Text];
				if (instance == null)
				{
					instance = new ServerConfiguration();
					instance.Name = _cbInProcessInstanceName.Text;
				}
				else
					configuration.Instances.Remove(instance.Name);
				
				instance = EditInstanceForm.ExecuteEdit(instance);
				
				configuration.Instances.Add(instance);
				InstanceManager.SaveConfiguration(configuration);

				_localInstancesEnumerated = false;
				LoadInstances(configuration);
				
				_cbInProcessInstanceName.Text = instance.Name;
			}
		}
	}
}
