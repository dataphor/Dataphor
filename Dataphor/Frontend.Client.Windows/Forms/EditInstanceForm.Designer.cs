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
    partial class EditInstanceForm
    {
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbInstanceName;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private Button buttonSelectInstanceDirectory;
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
        private Button buttonSelectLibraryDirectory;
        private System.Windows.Forms.TextBox tbLibraryDirectories;
        private System.Windows.Forms.CheckBox cbRequireSecureConnection;
        private GroupBox groupBox5;
        private Label label7;
        private System.Windows.Forms.TextBox tbSecurePortNumber;
        private System.Windows.Forms.CheckBox cbShouldListen;
        private Label label10;
        private Label label8;
        private System.Windows.Forms.TextBox tbOverrideSecureListenerPortNumber;
        private System.Windows.Forms.CheckBox cbRequireSecureListenerConnection;
        private Label label9;
        private System.Windows.Forms.TextBox tbOverrideListenerPortNumber;
        private System.Windows.Forms.CheckBox cbAllowSilverlightClients;
        private Label label13;



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
            this.buttonSelectInstanceDirectory = new System.Windows.Forms.Button();
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
            this.buttonSelectLibraryDirectory = new System.Windows.Forms.Button();
            this.tbLibraryDirectories = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tbSecurePortNumber = new System.Windows.Forms.TextBox();
            this.cbRequireSecureConnection = new System.Windows.Forms.CheckBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.cbAllowSilverlightClients = new System.Windows.Forms.CheckBox();
            this.cbShouldListen = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.tbOverrideSecureListenerPortNumber = new System.Windows.Forms.TextBox();
            this.cbRequireSecureListenerConnection = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tbOverrideListenerPortNumber = new System.Windows.Forms.TextBox();
            this.FContentPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // FContentPanel
            // 
            this.FContentPanel.AutoScroll = true;
            this.FContentPanel.Controls.Add(this.groupBox5);
            this.FContentPanel.Controls.Add(this.groupBox4);
            this.FContentPanel.Controls.Add(this.groupBox3);
            this.FContentPanel.Controls.Add(this.groupBox2);
            this.FContentPanel.Controls.Add(this.groupBox1);
            this.FContentPanel.Size = new System.Drawing.Size(687, 447);
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
            // buttonSelectInstanceDirectory
            // 
            this.buttonSelectInstanceDirectory.Location = new System.Drawing.Point(276, 108);
            this.buttonSelectInstanceDirectory.Name = "buttonSelectInstanceDirectory";
            this.buttonSelectInstanceDirectory.Size = new System.Drawing.Size(24, 22);
            this.buttonSelectInstanceDirectory.TabIndex = 56;
            this.buttonSelectInstanceDirectory.Text = "...";
            this.buttonSelectInstanceDirectory.Click += new System.EventHandler(this.buttonSelectInstanceDirectory_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tbCatalogStoreConnectionString);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.tbCatalogStoreClassName);
            this.groupBox1.Location = new System.Drawing.Point(326, 148);
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
            this.tbCatalogStoreClassName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.tbCatalogStoreClassName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbCatalogStoreClassName.Location = new System.Drawing.Point(14, 32);
            this.tbCatalogStoreClassName.Name = "tbCatalogStoreClassName";
            this.tbCatalogStoreClassName.Size = new System.Drawing.Size(331, 20);
            this.tbCatalogStoreClassName.TabIndex = 0;
            this.tbCatalogStoreClassName.Tag = "n";
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
            this.groupBox2.Location = new System.Drawing.Point(324, 252);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(354, 184);
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
            this.DeviceSettingsListBox.Size = new System.Drawing.Size(336, 82);
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
            this.groupBox3.Controls.Add(this.buttonSelectLibraryDirectory);
            this.groupBox3.Controls.Add(this.tbLibraryDirectories);
            this.groupBox3.Location = new System.Drawing.Point(12, 148);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(306, 288);
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
            this.LibraryDirectoriesListBox.Size = new System.Drawing.Size(291, 199);
            this.LibraryDirectoriesListBox.TabIndex = 1;
            this.LibraryDirectoriesListBox.SelectedIndexChanged += new System.EventHandler(this.LibraryDirectoriesListBox_SelectedIndexChanged);
            // 
            // buttonSelectLibraryDirectory
            // 
            this.buttonSelectLibraryDirectory.Location = new System.Drawing.Point(273, 22);
            this.buttonSelectLibraryDirectory.Name = "buttonSelectLibraryDirectory";
            this.buttonSelectLibraryDirectory.Size = new System.Drawing.Size(24, 22);
            this.buttonSelectLibraryDirectory.TabIndex = 72;
            this.buttonSelectLibraryDirectory.Text = "...";
            this.buttonSelectLibraryDirectory.Click += new System.EventHandler(this.buttonSelectLibraryDirectory_Click);
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
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.tbSecurePortNumber);
            this.groupBox4.Controls.Add(this.cbRequireSecureConnection);
            this.groupBox4.Controls.Add(this.tbInstanceName);
            this.groupBox4.Controls.Add(this.label1);
            this.groupBox4.Controls.Add(this.label13);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.buttonSelectInstanceDirectory);
            this.groupBox4.Controls.Add(this.tbPortNumber);
            this.groupBox4.Controls.Add(this.tbInstanceDirectory);
            this.groupBox4.Location = new System.Drawing.Point(12, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(306, 138);
            this.groupBox4.TabIndex = 0;
            this.groupBox4.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(76, 53);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(103, 13);
            this.label7.TabIndex = 62;
            this.label7.Text = "Secure Port Number";
            // 
            // tbSecurePortNumber
            // 
            this.tbSecurePortNumber.Location = new System.Drawing.Point(79, 70);
            this.tbSecurePortNumber.Name = "tbSecurePortNumber";
            this.tbSecurePortNumber.Size = new System.Drawing.Size(64, 20);
            this.tbSecurePortNumber.TabIndex = 61;
            this.tbSecurePortNumber.TextChanged += new System.EventHandler(this.tbPortNumber_TextChanged);
            // 
            // cbRequireSecureConnection
            // 
            this.cbRequireSecureConnection.AutoSize = true;
            this.cbRequireSecureConnection.Location = new System.Drawing.Point(149, 72);
            this.cbRequireSecureConnection.Name = "cbRequireSecureConnection";
            this.cbRequireSecureConnection.Size = new System.Drawing.Size(157, 17);
            this.cbRequireSecureConnection.TabIndex = 60;
            this.cbRequireSecureConnection.Text = "Require Secure Connection";
            this.cbRequireSecureConnection.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.cbAllowSilverlightClients);
            this.groupBox5.Controls.Add(this.cbShouldListen);
            this.groupBox5.Controls.Add(this.label10);
            this.groupBox5.Controls.Add(this.label8);
            this.groupBox5.Controls.Add(this.tbOverrideSecureListenerPortNumber);
            this.groupBox5.Controls.Add(this.cbRequireSecureListenerConnection);
            this.groupBox5.Controls.Add(this.label9);
            this.groupBox5.Controls.Add(this.tbOverrideListenerPortNumber);
            this.groupBox5.Location = new System.Drawing.Point(326, 4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(354, 138);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Advanced Network Configuration";
            // 
            // cbAllowSilverlightClients
            // 
            this.cbAllowSilverlightClients.AutoSize = true;
            this.cbAllowSilverlightClients.Location = new System.Drawing.Point(14, 112);
            this.cbAllowSilverlightClients.Name = "cbAllowSilverlightClients";
            this.cbAllowSilverlightClients.Size = new System.Drawing.Size(139, 17);
            this.cbAllowSilverlightClients.TabIndex = 70;
            this.cbAllowSilverlightClients.Text = "Allow Silverlight Clients?";
            this.cbAllowSilverlightClients.UseVisualStyleBackColor = true;
            // 
            // cbShouldListen
            // 
            this.cbShouldListen.AutoSize = true;
            this.cbShouldListen.Location = new System.Drawing.Point(14, 19);
            this.cbShouldListen.Name = "cbShouldListen";
            this.cbShouldListen.Size = new System.Drawing.Size(150, 17);
            this.cbShouldListen.TabIndex = 69;
            this.cbShouldListen.Text = "Should Establish Listener?";
            this.cbShouldListen.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(11, 38);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(117, 13);
            this.label10.TabIndex = 68;
            this.label10.Text = "Listener Port Overrides:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(81, 55);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(103, 13);
            this.label8.TabIndex = 67;
            this.label8.Text = "Secure Port Number";
            // 
            // tbOverrideSecureListenerPortNumber
            // 
            this.tbOverrideSecureListenerPortNumber.Location = new System.Drawing.Point(84, 72);
            this.tbOverrideSecureListenerPortNumber.Name = "tbOverrideSecureListenerPortNumber";
            this.tbOverrideSecureListenerPortNumber.Size = new System.Drawing.Size(64, 20);
            this.tbOverrideSecureListenerPortNumber.TabIndex = 66;
            // 
            // cbRequireSecureListenerConnection
            // 
            this.cbRequireSecureListenerConnection.AutoSize = true;
            this.cbRequireSecureListenerConnection.Location = new System.Drawing.Point(154, 74);
            this.cbRequireSecureListenerConnection.Name = "cbRequireSecureListenerConnection";
            this.cbRequireSecureListenerConnection.Size = new System.Drawing.Size(157, 17);
            this.cbRequireSecureListenerConnection.TabIndex = 65;
            this.cbRequireSecureListenerConnection.Text = "Require Secure Connection";
            this.cbRequireSecureListenerConnection.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 55);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(66, 13);
            this.label9.TabIndex = 64;
            this.label9.Text = "Port Number";
            // 
            // tbOverrideListenerPortNumber
            // 
            this.tbOverrideListenerPortNumber.Location = new System.Drawing.Point(14, 72);
            this.tbOverrideListenerPortNumber.Name = "tbOverrideListenerPortNumber";
            this.tbOverrideListenerPortNumber.Size = new System.Drawing.Size(64, 20);
            this.tbOverrideListenerPortNumber.TabIndex = 63;
            // 
            // EditInstanceForm
            // 
            this.ClientSize = new System.Drawing.Size(687, 518);
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
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

    }
}
