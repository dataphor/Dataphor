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
using System.Text;
using System.IO;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Form for adding and editing ServerAlias instances. </summary>
	public class EditInstanceForm : BaseForm
	{
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbInstanceName;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private Button button2;
		private System.Windows.Forms.TextBox tbInstanceDirectory;
		private System.Windows.Forms.TextBox tbPortNumber;
		private Label label11;
		private GroupBox groupBox2;
		private Label label6;
		private Label label5;
		private Label label4;
		private Button RemoveDeviceSettingButton;
		private Button UpdateDeviceSettingButton;
		private Button AddDeviceSettingButton;
		private System.Windows.Forms.TextBox SettingValueTextBox;
		private System.Windows.Forms.TextBox SettingNameTextBox;
		private System.Windows.Forms.TextBox DeviceNameTextBox;
		private ListBox DeviceSettingsListBox;
		private GroupBox groupBox1;
		private Label label3;
		private System.Windows.Forms.TextBox tbCatalogStoreConnectionString;
		private Label label2;
		private System.Windows.Forms.TextBox tbCatalogStoreClassName;
		private GroupBox groupBox4;
		private GroupBox groupBox3;
		private Button MoveLibraryDirectoryDownButton;
		private Button MoveLibraryDirectoryUpButton;
		private Button RemoveLibraryDirectoryButton;
		private Button UpdateLibraryDirectoryButton;
		private Button AddLibraryDirectoryButton;
		private ListBox LibraryDirectoriesListBox;
		private Button button1;
		private System.Windows.Forms.TextBox tbLibraryDirectories;
		private Label label13;

		public EditInstanceForm()
		{
			InitializeComponent();
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
			this.tbInstanceName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.label13 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.tbPortNumber = new System.Windows.Forms.TextBox();
			this.tbInstanceDirectory = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.tbCatalogStoreConnectionString = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.tbCatalogStoreClassName = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.RemoveDeviceSettingButton = new System.Windows.Forms.Button();
			this.UpdateDeviceSettingButton = new System.Windows.Forms.Button();
			this.AddDeviceSettingButton = new System.Windows.Forms.Button();
			this.SettingValueTextBox = new System.Windows.Forms.TextBox();
			this.SettingNameTextBox = new System.Windows.Forms.TextBox();
			this.DeviceNameTextBox = new System.Windows.Forms.TextBox();
			this.DeviceSettingsListBox = new System.Windows.Forms.ListBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.MoveLibraryDirectoryDownButton = new System.Windows.Forms.Button();
			this.MoveLibraryDirectoryUpButton = new System.Windows.Forms.Button();
			this.RemoveLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.UpdateLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.AddLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.LibraryDirectoriesListBox = new System.Windows.Forms.ListBox();
			this.button1 = new System.Windows.Forms.Button();
			this.tbLibraryDirectories = new System.Windows.Forms.TextBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.FContentPanel.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.SuspendLayout();
			// 
			// FContentPanel
			// 
			this.FContentPanel.AutoScroll = true;
			this.FContentPanel.Controls.Add(this.groupBox4);
			this.FContentPanel.Controls.Add(this.groupBox3);
			this.FContentPanel.Controls.Add(this.groupBox2);
			this.FContentPanel.Controls.Add(this.groupBox1);
			this.FContentPanel.Size = new System.Drawing.Size(683, 334);
			// 
			// tbInstanceName
			// 
			this.tbInstanceName.Location = new System.Drawing.Point(9, 30);
			this.tbInstanceName.Name = "tbInstanceName";
			this.tbInstanceName.Size = new System.Drawing.Size(197, 20);
			this.tbInstanceName.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Location = new System.Drawing.Point(6, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(79, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Instance Name";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(6, 93);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(93, 13);
			this.label13.TabIndex = 58;
			this.label13.Text = "Instance Directory";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(6, 53);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(66, 13);
			this.label11.TabIndex = 59;
			this.label11.Text = "Port Number";
			// 
			// tbPortNumber
			// 
			this.tbPortNumber.Location = new System.Drawing.Point(9, 70);
			this.tbPortNumber.Name = "tbPortNumber";
			this.tbPortNumber.Size = new System.Drawing.Size(64, 20);
			this.tbPortNumber.TabIndex = 1;
			this.tbPortNumber.TextChanged += new System.EventHandler(this.tbPortNumber_TextChanged);
			// 
			// tbInstanceDirectory
			// 
			this.tbInstanceDirectory.Location = new System.Drawing.Point(9, 110);
			this.tbInstanceDirectory.Name = "tbInstanceDirectory";
			this.tbInstanceDirectory.Size = new System.Drawing.Size(261, 20);
			this.tbInstanceDirectory.TabIndex = 2;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(276, 108);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(24, 22);
			this.button2.TabIndex = 56;
			this.button2.Text = "...";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.tbCatalogStoreConnectionString);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.tbCatalogStoreClassName);
			this.groupBox1.Location = new System.Drawing.Point(324, 4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(354, 98);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Catalog Store Settings";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.BackColor = System.Drawing.Color.Transparent;
			this.label3.Location = new System.Drawing.Point(11, 55);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(91, 13);
			this.label3.TabIndex = 68;
			this.label3.Text = "Connection String";
			// 
			// tbCatalogStoreConnectionString
			// 
			this.tbCatalogStoreConnectionString.Location = new System.Drawing.Point(14, 72);
			this.tbCatalogStoreConnectionString.Name = "tbCatalogStoreConnectionString";
			this.tbCatalogStoreConnectionString.Size = new System.Drawing.Size(331, 20);
			this.tbCatalogStoreConnectionString.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.Location = new System.Drawing.Point(11, 15);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(63, 13);
			this.label2.TabIndex = 66;
			this.label2.Text = "Class Name";
			// 
			// tbCatalogStoreClassName
			// 
			this.tbCatalogStoreClassName.Location = new System.Drawing.Point(14, 32);
			this.tbCatalogStoreClassName.Name = "tbCatalogStoreClassName";
			this.tbCatalogStoreClassName.Size = new System.Drawing.Size(331, 20);
			this.tbCatalogStoreClassName.TabIndex = 0;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.RemoveDeviceSettingButton);
			this.groupBox2.Controls.Add(this.UpdateDeviceSettingButton);
			this.groupBox2.Controls.Add(this.AddDeviceSettingButton);
			this.groupBox2.Controls.Add(this.SettingValueTextBox);
			this.groupBox2.Controls.Add(this.SettingNameTextBox);
			this.groupBox2.Controls.Add(this.DeviceNameTextBox);
			this.groupBox2.Controls.Add(this.DeviceSettingsListBox);
			this.groupBox2.Location = new System.Drawing.Point(324, 108);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(354, 219);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Device Settings";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(235, 22);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(34, 13);
			this.label6.TabIndex = 89;
			this.label6.Text = "Value";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(121, 22);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(40, 13);
			this.label5.TabIndex = 88;
			this.label5.Text = "Setting";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 22);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(72, 13);
			this.label4.TabIndex = 87;
			this.label4.Text = "Device Name";
			// 
			// RemoveDeviceSettingButton
			// 
			this.RemoveDeviceSettingButton.Location = new System.Drawing.Point(172, 64);
			this.RemoveDeviceSettingButton.Name = "RemoveDeviceSettingButton";
			this.RemoveDeviceSettingButton.Size = new System.Drawing.Size(75, 23);
			this.RemoveDeviceSettingButton.TabIndex = 86;
			this.RemoveDeviceSettingButton.Text = "Remove";
			this.RemoveDeviceSettingButton.UseVisualStyleBackColor = true;
			this.RemoveDeviceSettingButton.Click += new System.EventHandler(this.RemoveDeviceSettingButton_Click);
			// 
			// UpdateDeviceSettingButton
			// 
			this.UpdateDeviceSettingButton.Location = new System.Drawing.Point(91, 64);
			this.UpdateDeviceSettingButton.Name = "UpdateDeviceSettingButton";
			this.UpdateDeviceSettingButton.Size = new System.Drawing.Size(75, 23);
			this.UpdateDeviceSettingButton.TabIndex = 85;
			this.UpdateDeviceSettingButton.Text = "Update";
			this.UpdateDeviceSettingButton.UseVisualStyleBackColor = true;
			this.UpdateDeviceSettingButton.Click += new System.EventHandler(this.UpdateDeviceSettingButton_Click);
			// 
			// AddDeviceSettingButton
			// 
			this.AddDeviceSettingButton.Location = new System.Drawing.Point(10, 64);
			this.AddDeviceSettingButton.Name = "AddDeviceSettingButton";
			this.AddDeviceSettingButton.Size = new System.Drawing.Size(75, 23);
			this.AddDeviceSettingButton.TabIndex = 84;
			this.AddDeviceSettingButton.Text = "Add";
			this.AddDeviceSettingButton.UseVisualStyleBackColor = true;
			this.AddDeviceSettingButton.Click += new System.EventHandler(this.AddDeviceSettingButton_Click);
			// 
			// SettingValueTextBox
			// 
			this.SettingValueTextBox.Location = new System.Drawing.Point(238, 39);
			this.SettingValueTextBox.Name = "SettingValueTextBox";
			this.SettingValueTextBox.Size = new System.Drawing.Size(108, 20);
			this.SettingValueTextBox.TabIndex = 2;
			// 
			// SettingNameTextBox
			// 
			this.SettingNameTextBox.Location = new System.Drawing.Point(124, 39);
			this.SettingNameTextBox.Name = "SettingNameTextBox";
			this.SettingNameTextBox.Size = new System.Drawing.Size(108, 20);
			this.SettingNameTextBox.TabIndex = 1;
			// 
			// DeviceNameTextBox
			// 
			this.DeviceNameTextBox.Location = new System.Drawing.Point(10, 39);
			this.DeviceNameTextBox.Name = "DeviceNameTextBox";
			this.DeviceNameTextBox.Size = new System.Drawing.Size(108, 20);
			this.DeviceNameTextBox.TabIndex = 0;
			// 
			// DeviceSettingsListBox
			// 
			this.DeviceSettingsListBox.FormattingEnabled = true;
			this.DeviceSettingsListBox.Location = new System.Drawing.Point(9, 93);
			this.DeviceSettingsListBox.Name = "DeviceSettingsListBox";
			this.DeviceSettingsListBox.Size = new System.Drawing.Size(336, 121);
			this.DeviceSettingsListBox.TabIndex = 3;
			this.DeviceSettingsListBox.SelectedIndexChanged += new System.EventHandler(this.DeviceSettingsListBox_SelectedIndexChanged);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.MoveLibraryDirectoryDownButton);
			this.groupBox3.Controls.Add(this.MoveLibraryDirectoryUpButton);
			this.groupBox3.Controls.Add(this.RemoveLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.UpdateLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.AddLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.LibraryDirectoriesListBox);
			this.groupBox3.Controls.Add(this.button1);
			this.groupBox3.Controls.Add(this.tbLibraryDirectories);
			this.groupBox3.Location = new System.Drawing.Point(12, 148);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(306, 179);
			this.groupBox3.TabIndex = 1;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Library Directories";
			// 
			// MoveLibraryDirectoryDownButton
			// 
			this.MoveLibraryDirectoryDownButton.Location = new System.Drawing.Point(239, 49);
			this.MoveLibraryDirectoryDownButton.Name = "MoveLibraryDirectoryDownButton";
			this.MoveLibraryDirectoryDownButton.Size = new System.Drawing.Size(58, 23);
			this.MoveLibraryDirectoryDownButton.TabIndex = 78;
			this.MoveLibraryDirectoryDownButton.Text = "Down";
			this.MoveLibraryDirectoryDownButton.UseVisualStyleBackColor = true;
			this.MoveLibraryDirectoryDownButton.Click += new System.EventHandler(this.MoveLibraryDirectoryDownButton_Click);
			// 
			// MoveLibraryDirectoryUpButton
			// 
			this.MoveLibraryDirectoryUpButton.Location = new System.Drawing.Point(181, 49);
			this.MoveLibraryDirectoryUpButton.Name = "MoveLibraryDirectoryUpButton";
			this.MoveLibraryDirectoryUpButton.Size = new System.Drawing.Size(58, 23);
			this.MoveLibraryDirectoryUpButton.TabIndex = 77;
			this.MoveLibraryDirectoryUpButton.Text = "Up";
			this.MoveLibraryDirectoryUpButton.UseVisualStyleBackColor = true;
			this.MoveLibraryDirectoryUpButton.Click += new System.EventHandler(this.MoveLibraryDirectoryUpButton_Click);
			// 
			// RemoveLibraryDirectoryButton
			// 
			this.RemoveLibraryDirectoryButton.Location = new System.Drawing.Point(123, 49);
			this.RemoveLibraryDirectoryButton.Name = "RemoveLibraryDirectoryButton";
			this.RemoveLibraryDirectoryButton.Size = new System.Drawing.Size(58, 23);
			this.RemoveLibraryDirectoryButton.TabIndex = 76;
			this.RemoveLibraryDirectoryButton.Text = "Remove";
			this.RemoveLibraryDirectoryButton.UseVisualStyleBackColor = true;
			this.RemoveLibraryDirectoryButton.Click += new System.EventHandler(this.RemoveLibraryDirectoryButton_Click);
			// 
			// UpdateLibraryDirectoryButton
			// 
			this.UpdateLibraryDirectoryButton.Location = new System.Drawing.Point(65, 49);
			this.UpdateLibraryDirectoryButton.Name = "UpdateLibraryDirectoryButton";
			this.UpdateLibraryDirectoryButton.Size = new System.Drawing.Size(58, 23);
			this.UpdateLibraryDirectoryButton.TabIndex = 75;
			this.UpdateLibraryDirectoryButton.Text = "Update";
			this.UpdateLibraryDirectoryButton.UseVisualStyleBackColor = true;
			this.UpdateLibraryDirectoryButton.Click += new System.EventHandler(this.UpdateLibraryDirectoryButton_Click);
			// 
			// AddLibraryDirectoryButton
			// 
			this.AddLibraryDirectoryButton.Location = new System.Drawing.Point(7, 49);
			this.AddLibraryDirectoryButton.Name = "AddLibraryDirectoryButton";
			this.AddLibraryDirectoryButton.Size = new System.Drawing.Size(58, 23);
			this.AddLibraryDirectoryButton.TabIndex = 74;
			this.AddLibraryDirectoryButton.Text = "Add";
			this.AddLibraryDirectoryButton.UseVisualStyleBackColor = true;
			this.AddLibraryDirectoryButton.Click += new System.EventHandler(this.AddLibraryDirectoryButton_Click);
			// 
			// LibraryDirectoriesListBox
			// 
			this.LibraryDirectoriesListBox.FormattingEnabled = true;
			this.LibraryDirectoriesListBox.Location = new System.Drawing.Point(6, 78);
			this.LibraryDirectoriesListBox.Name = "LibraryDirectoriesListBox";
			this.LibraryDirectoriesListBox.Size = new System.Drawing.Size(291, 95);
			this.LibraryDirectoriesListBox.TabIndex = 1;
			this.LibraryDirectoriesListBox.SelectedIndexChanged += new System.EventHandler(this.LibraryDirectoriesListBox_SelectedIndexChanged);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(273, 22);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(24, 22);
			this.button1.TabIndex = 72;
			this.button1.Text = "...";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// tbLibraryDirectories
			// 
			this.tbLibraryDirectories.Location = new System.Drawing.Point(6, 24);
			this.tbLibraryDirectories.Name = "tbLibraryDirectories";
			this.tbLibraryDirectories.Size = new System.Drawing.Size(261, 20);
			this.tbLibraryDirectories.TabIndex = 0;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.tbInstanceName);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this.label13);
			this.groupBox4.Controls.Add(this.label11);
			this.groupBox4.Controls.Add(this.button2);
			this.groupBox4.Controls.Add(this.tbPortNumber);
			this.groupBox4.Controls.Add(this.tbInstanceDirectory);
			this.groupBox4.Location = new System.Drawing.Point(12, 4);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(306, 138);
			this.groupBox4.TabIndex = 0;
			this.groupBox4.TabStop = false;
			// 
			// EditInstanceForm
			// 
			this.ClientSize = new System.Drawing.Size(683, 405);
			this.ControlBox = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "EditInstanceForm";
			this.ShowInTaskbar = false;
			this.Text = "Instance Configuration";
			this.FContentPanel.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
		
		public static ServerConfiguration ExecuteAdd()
		{
			return ExecuteEdit(new ServerConfiguration());
		}
		
		public static ServerConfiguration ExecuteEdit(ServerConfiguration AConfiguration)
		{
			using (EditInstanceForm LForm = new EditInstanceForm())
			{
				LForm.SetFromConfiguration(AConfiguration);
				if (LForm.ShowDialog() != DialogResult.OK)
					throw new AbortException();
				return LForm.CreateConfiguration();
			}
		}
		
		public void SetFromConfiguration(ServerConfiguration AConfiguration)
		{
			tbInstanceName.Text = AConfiguration.Name;
			tbPortNumber.Text = AConfiguration.PortNumber.ToString();
			tbInstanceDirectory.Text = AConfiguration.InstanceDirectory;
			LibraryDirectoriesListBox.Items.Clear();
			string[] LLibraryDirectories = AConfiguration.LibraryDirectories.Split(';');
			for (int LIndex = 0; LIndex < LLibraryDirectories.Length; LIndex++)
				LibraryDirectoriesListBox.Items.Add(LLibraryDirectories[LIndex]);
			tbCatalogStoreClassName.Text = AConfiguration.CatalogStoreClassName;
			tbCatalogStoreConnectionString.Text = AConfiguration.CatalogStoreConnectionString;
			DeviceSettingsListBox.Items.Clear();
			foreach (DeviceSetting LDeviceSetting in AConfiguration.DeviceSettings)
				DeviceSettingsListBox.Items.Add(LDeviceSetting);
		}
		
		public ServerConfiguration CreateConfiguration()
		{
			ServerConfiguration LResult = new ServerConfiguration();
			LResult.Name = tbInstanceName.Text;
			LResult.PortNumber = Int32.Parse(tbPortNumber.Text);
			LResult.InstanceDirectory = tbInstanceDirectory.Text;
			StringBuilder LLibraryDirectories = new StringBuilder();
			for (int LIndex = 0; LIndex < LibraryDirectoriesListBox.Items.Count; LIndex++)
			{
				if (LIndex > 0)
					LLibraryDirectories.Append(Path.PathSeparator);
				LLibraryDirectories.Append(LibraryDirectoriesListBox.Items[LIndex] as String);
			}
			LResult.LibraryDirectories = LLibraryDirectories.ToString();
			LResult.CatalogStoreClassName = tbCatalogStoreClassName.Text;
			LResult.CatalogStoreConnectionString = tbCatalogStoreConnectionString.Text;
			for (int LIndex = 0; LIndex < DeviceSettingsListBox.Items.Count; LIndex++)
				LResult.DeviceSettings.Add((DeviceSetting)DeviceSettingsListBox.Items[LIndex]);
			return LResult;
		}

		private void tbPortNumber_TextChanged(object sender, EventArgs e)
		{
			Int32.Parse(tbPortNumber.Text);
		}

		private void LibraryDirectoriesListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			tbLibraryDirectories.Text = LibraryDirectoriesListBox.SelectedItem as String;
		}

		private void AddLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			LibraryDirectoriesListBox.Items.Add(tbLibraryDirectories.Text);
		}

		private void UpdateLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			if (LibraryDirectoriesListBox.SelectedIndex >= 0)
				LibraryDirectoriesListBox.Items[LibraryDirectoriesListBox.SelectedIndex] = tbLibraryDirectories.Text;
		}

		private void RemoveLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			if (LibraryDirectoriesListBox.SelectedIndex >= 0)
				LibraryDirectoriesListBox.Items.RemoveAt(LibraryDirectoriesListBox.SelectedIndex);
		}

		private void MoveLibraryDirectoryUpButton_Click(object sender, EventArgs e)
		{
			int LIndex = LibraryDirectoriesListBox.SelectedIndex;
			if (LIndex >= 1)
			{
				string LValue = LibraryDirectoriesListBox.Items[LIndex] as String;
				LibraryDirectoriesListBox.Items.RemoveAt(LIndex);
				LibraryDirectoriesListBox.Items.Insert(LIndex - 1, LValue);
			}
		}

		private void MoveLibraryDirectoryDownButton_Click(object sender, EventArgs e)
		{
			int LIndex = LibraryDirectoriesListBox.SelectedIndex;
			if ((LIndex >= 0) && (LIndex < LibraryDirectoriesListBox.Items.Count - 1))
			{
				string LValue = LibraryDirectoriesListBox.Items[LIndex] as String;
				LibraryDirectoriesListBox.Items.RemoveAt(LIndex);
				LibraryDirectoriesListBox.Items.Insert(LIndex + 1, LValue);
			}
		}
		
		private void PushSetting(DeviceSetting ASetting)
		{
			ASetting.DeviceName = DeviceNameTextBox.Text;
			ASetting.SettingName = SettingNameTextBox.Text;
			ASetting.SettingValue = SettingValueTextBox.Text;
		}

		private void DeviceSettingsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			DeviceSetting LSetting = DeviceSettingsListBox.SelectedItem as DeviceSetting;
			if (LSetting != null)
			{
				DeviceNameTextBox.Text = LSetting.DeviceName;
				SettingNameTextBox.Text = LSetting.SettingName;
				SettingValueTextBox.Text = LSetting.SettingValue;
			}
		}

		private void AddDeviceSettingButton_Click(object sender, EventArgs e)
		{
			DeviceSetting LSetting = new DeviceSetting();
			PushSetting(LSetting);
			DeviceSettingsListBox.Items.Add(LSetting);
		}

		private void UpdateDeviceSettingButton_Click(object sender, EventArgs e)
		{
			if (DeviceSettingsListBox.SelectedIndex >= 0)
			{
				DeviceSetting LSetting = new DeviceSetting();
				PushSetting(LSetting);
				DeviceSettingsListBox.Items[DeviceSettingsListBox.SelectedIndex] = LSetting;
			}
		}

		private void RemoveDeviceSettingButton_Click(object sender, EventArgs e)
		{
			if (DeviceSettingsListBox.SelectedIndex >= 0)
				DeviceSettingsListBox.Items.RemoveAt(DeviceSettingsListBox.SelectedIndex);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			try
			{
				tbInstanceDirectory.Text = FolderUtility.GetDirectory(tbInstanceDirectory.Text);
			}
			catch (AbortException) { }
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				tbLibraryDirectories.Text = FolderUtility.GetDirectory(tbLibraryDirectories.Text);
			}
			catch (AbortException) { }
		}
	}
}
