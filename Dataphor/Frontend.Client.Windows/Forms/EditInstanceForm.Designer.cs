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
        private System.Windows.Forms.TextBox FInstanceNameTextBox;
        private System.Windows.Forms.OpenFileDialog FOpenFileDialog1;
        private Button FSelectInstanceDirectoryButton;
        private System.Windows.Forms.TextBox FInstanceDirectoryTextBox;
        private System.Windows.Forms.TextBox FPortNumberTextBox;
        private Label label11;
        private GroupBox groupBox2;
        private Label label6;
        private Label label5;
        private Label label4;
        private Button FRemoveDeviceSettingButton;
        private Button FUpdateDeviceSettingButton;
        private Button FAddDeviceSettingButton;
        private System.Windows.Forms.TextBox FSettingValueTextBox;
        private System.Windows.Forms.TextBox FSettingNameTextBox;
        private System.Windows.Forms.TextBox FDeviceNameTextBox;
        private ListBox FDeviceSettingsListBox;
        private GroupBox groupBox1;
        private Label label3;
        private System.Windows.Forms.TextBox FCatalogStoreConnectionStringTextBox;
        private Label label2;
        private System.Windows.Forms.TextBox FCatalogStoreClassNameTextBox;
        private GroupBox groupBox4;
        private GroupBox groupBox3;
        private Button FMoveLibraryDirectoryDownButton;
        private Button FMoveLibraryDirectoryUpButton;
        private Button FRemoveLibraryDirectoryButton;
        private Button FUpdateLibraryDirectoryButton;
        private Button FAddLibraryDirectoryButton;
        private ListBox FLibraryDirectoriesListBox;
        private Button FSelectLibraryDirectoryButton;
        private System.Windows.Forms.TextBox FLibraryDirectoriesTextBox;
        private System.Windows.Forms.CheckBox FRequireSecureConnectionComboBox;
		private GroupBox FGroupBox5;
        private System.Windows.Forms.CheckBox FShouldListenComboBox;
		private Label FLabel10;
        private System.Windows.Forms.CheckBox FRequireSecureListenerConnectionComboBox;
        private Label FLabel9;
        private System.Windows.Forms.TextBox FOverrideListenerPortNumberTextBox;
        private System.Windows.Forms.CheckBox FAllowSilverlightClientsComboBox;
        private Label label13;



        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.FInstanceNameTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.FOpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.label13 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.FPortNumberTextBox = new System.Windows.Forms.TextBox();
			this.FInstanceDirectoryTextBox = new System.Windows.Forms.TextBox();
			this.FSelectInstanceDirectoryButton = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.FCatalogStoreConnectionStringTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.FCatalogStoreClassNameTextBox = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.FRemoveDeviceSettingButton = new System.Windows.Forms.Button();
			this.FUpdateDeviceSettingButton = new System.Windows.Forms.Button();
			this.FAddDeviceSettingButton = new System.Windows.Forms.Button();
			this.FSettingValueTextBox = new System.Windows.Forms.TextBox();
			this.FSettingNameTextBox = new System.Windows.Forms.TextBox();
			this.FDeviceNameTextBox = new System.Windows.Forms.TextBox();
			this.FDeviceSettingsListBox = new System.Windows.Forms.ListBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.FMoveLibraryDirectoryDownButton = new System.Windows.Forms.Button();
			this.FMoveLibraryDirectoryUpButton = new System.Windows.Forms.Button();
			this.FRemoveLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.FUpdateLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.FAddLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.FLibraryDirectoriesListBox = new System.Windows.Forms.ListBox();
			this.FSelectLibraryDirectoryButton = new System.Windows.Forms.Button();
			this.FLibraryDirectoriesTextBox = new System.Windows.Forms.TextBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.FRequireSecureConnectionComboBox = new System.Windows.Forms.CheckBox();
			this.FGroupBox5 = new System.Windows.Forms.GroupBox();
			this.FUseServiceConfigurationCheckBox = new System.Windows.Forms.CheckBox();
			this.FAllowSilverlightClientsComboBox = new System.Windows.Forms.CheckBox();
			this.FShouldListenComboBox = new System.Windows.Forms.CheckBox();
			this.FLabel10 = new System.Windows.Forms.Label();
			this.FRequireSecureListenerConnectionComboBox = new System.Windows.Forms.CheckBox();
			this.FLabel9 = new System.Windows.Forms.Label();
			this.FOverrideListenerPortNumberTextBox = new System.Windows.Forms.TextBox();
			this.FContentPanel.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.FGroupBox5.SuspendLayout();
			this.SuspendLayout();
			// 
			// FContentPanel
			// 
			this.FContentPanel.AutoScroll = true;
			this.FContentPanel.Controls.Add(this.FGroupBox5);
			this.FContentPanel.Controls.Add(this.groupBox4);
			this.FContentPanel.Controls.Add(this.groupBox3);
			this.FContentPanel.Controls.Add(this.groupBox2);
			this.FContentPanel.Controls.Add(this.groupBox1);
			this.FContentPanel.Size = new System.Drawing.Size(687, 447);
			// 
			// FInstanceNameTextBox
			// 
			this.FInstanceNameTextBox.Location = new System.Drawing.Point(9, 30);
			this.FInstanceNameTextBox.Name = "FInstanceNameTextBox";
			this.FInstanceNameTextBox.Size = new System.Drawing.Size(197, 20);
			this.FInstanceNameTextBox.TabIndex = 0;
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
			// FPortNumberTextBox
			// 
			this.FPortNumberTextBox.Location = new System.Drawing.Point(9, 70);
			this.FPortNumberTextBox.Name = "FPortNumberTextBox";
			this.FPortNumberTextBox.Size = new System.Drawing.Size(64, 20);
			this.FPortNumberTextBox.TabIndex = 1;
			this.FPortNumberTextBox.TextChanged += new System.EventHandler(this.tbPortNumber_TextChanged);
			// 
			// FInstanceDirectoryTextBox
			// 
			this.FInstanceDirectoryTextBox.Location = new System.Drawing.Point(9, 110);
			this.FInstanceDirectoryTextBox.Name = "FInstanceDirectoryTextBox";
			this.FInstanceDirectoryTextBox.Size = new System.Drawing.Size(261, 20);
			this.FInstanceDirectoryTextBox.TabIndex = 2;
			// 
			// FSelectInstanceDirectoryButton
			// 
			this.FSelectInstanceDirectoryButton.Location = new System.Drawing.Point(276, 108);
			this.FSelectInstanceDirectoryButton.Name = "FSelectInstanceDirectoryButton";
			this.FSelectInstanceDirectoryButton.Size = new System.Drawing.Size(24, 22);
			this.FSelectInstanceDirectoryButton.TabIndex = 56;
			this.FSelectInstanceDirectoryButton.Text = "...";
			this.FSelectInstanceDirectoryButton.Click += new System.EventHandler(this.SelectInstanceDirectoryButton_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.FCatalogStoreConnectionStringTextBox);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.FCatalogStoreClassNameTextBox);
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
			// FCatalogStoreConnectionStringTextBox
			// 
			this.FCatalogStoreConnectionStringTextBox.Location = new System.Drawing.Point(14, 72);
			this.FCatalogStoreConnectionStringTextBox.Name = "FCatalogStoreConnectionStringTextBox";
			this.FCatalogStoreConnectionStringTextBox.Size = new System.Drawing.Size(331, 20);
			this.FCatalogStoreConnectionStringTextBox.TabIndex = 1;
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
			// FCatalogStoreClassNameTextBox
			// 
			this.FCatalogStoreClassNameTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.FCatalogStoreClassNameTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this.FCatalogStoreClassNameTextBox.Location = new System.Drawing.Point(14, 32);
			this.FCatalogStoreClassNameTextBox.Name = "FCatalogStoreClassNameTextBox";
			this.FCatalogStoreClassNameTextBox.Size = new System.Drawing.Size(331, 20);
			this.FCatalogStoreClassNameTextBox.TabIndex = 0;
			this.FCatalogStoreClassNameTextBox.Tag = "n";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.FRemoveDeviceSettingButton);
			this.groupBox2.Controls.Add(this.FUpdateDeviceSettingButton);
			this.groupBox2.Controls.Add(this.FAddDeviceSettingButton);
			this.groupBox2.Controls.Add(this.FSettingValueTextBox);
			this.groupBox2.Controls.Add(this.FSettingNameTextBox);
			this.groupBox2.Controls.Add(this.FDeviceNameTextBox);
			this.groupBox2.Controls.Add(this.FDeviceSettingsListBox);
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
			// FRemoveDeviceSettingButton
			// 
			this.FRemoveDeviceSettingButton.Location = new System.Drawing.Point(172, 64);
			this.FRemoveDeviceSettingButton.Name = "FRemoveDeviceSettingButton";
			this.FRemoveDeviceSettingButton.Size = new System.Drawing.Size(75, 23);
			this.FRemoveDeviceSettingButton.TabIndex = 86;
			this.FRemoveDeviceSettingButton.Text = "Remove";
			this.FRemoveDeviceSettingButton.UseVisualStyleBackColor = true;
			this.FRemoveDeviceSettingButton.Click += new System.EventHandler(this.RemoveDeviceSettingButton_Click);
			// 
			// FUpdateDeviceSettingButton
			// 
			this.FUpdateDeviceSettingButton.Location = new System.Drawing.Point(91, 64);
			this.FUpdateDeviceSettingButton.Name = "FUpdateDeviceSettingButton";
			this.FUpdateDeviceSettingButton.Size = new System.Drawing.Size(75, 23);
			this.FUpdateDeviceSettingButton.TabIndex = 85;
			this.FUpdateDeviceSettingButton.Text = "Update";
			this.FUpdateDeviceSettingButton.UseVisualStyleBackColor = true;
			this.FUpdateDeviceSettingButton.Click += new System.EventHandler(this.UpdateDeviceSettingButton_Click);
			// 
			// FAddDeviceSettingButton
			// 
			this.FAddDeviceSettingButton.Location = new System.Drawing.Point(10, 64);
			this.FAddDeviceSettingButton.Name = "FAddDeviceSettingButton";
			this.FAddDeviceSettingButton.Size = new System.Drawing.Size(75, 23);
			this.FAddDeviceSettingButton.TabIndex = 84;
			this.FAddDeviceSettingButton.Text = "Add";
			this.FAddDeviceSettingButton.UseVisualStyleBackColor = true;
			this.FAddDeviceSettingButton.Click += new System.EventHandler(this.AddDeviceSettingButton_Click);
			// 
			// FSettingValueTextBox
			// 
			this.FSettingValueTextBox.Location = new System.Drawing.Point(238, 39);
			this.FSettingValueTextBox.Name = "FSettingValueTextBox";
			this.FSettingValueTextBox.Size = new System.Drawing.Size(108, 20);
			this.FSettingValueTextBox.TabIndex = 2;
			// 
			// FSettingNameTextBox
			// 
			this.FSettingNameTextBox.Location = new System.Drawing.Point(124, 39);
			this.FSettingNameTextBox.Name = "FSettingNameTextBox";
			this.FSettingNameTextBox.Size = new System.Drawing.Size(108, 20);
			this.FSettingNameTextBox.TabIndex = 1;
			// 
			// FDeviceNameTextBox
			// 
			this.FDeviceNameTextBox.Location = new System.Drawing.Point(10, 39);
			this.FDeviceNameTextBox.Name = "FDeviceNameTextBox";
			this.FDeviceNameTextBox.Size = new System.Drawing.Size(108, 20);
			this.FDeviceNameTextBox.TabIndex = 0;
			// 
			// FDeviceSettingsListBox
			// 
			this.FDeviceSettingsListBox.FormattingEnabled = true;
			this.FDeviceSettingsListBox.Location = new System.Drawing.Point(9, 93);
			this.FDeviceSettingsListBox.Name = "FDeviceSettingsListBox";
			this.FDeviceSettingsListBox.Size = new System.Drawing.Size(336, 82);
			this.FDeviceSettingsListBox.TabIndex = 3;
			this.FDeviceSettingsListBox.SelectedIndexChanged += new System.EventHandler(this.DeviceSettingsListBox_SelectedIndexChanged);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.FMoveLibraryDirectoryDownButton);
			this.groupBox3.Controls.Add(this.FMoveLibraryDirectoryUpButton);
			this.groupBox3.Controls.Add(this.FRemoveLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.FUpdateLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.FAddLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.FLibraryDirectoriesListBox);
			this.groupBox3.Controls.Add(this.FSelectLibraryDirectoryButton);
			this.groupBox3.Controls.Add(this.FLibraryDirectoriesTextBox);
			this.groupBox3.Location = new System.Drawing.Point(12, 148);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(306, 288);
			this.groupBox3.TabIndex = 1;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Library Directories";
			// 
			// FMoveLibraryDirectoryDownButton
			// 
			this.FMoveLibraryDirectoryDownButton.Location = new System.Drawing.Point(239, 49);
			this.FMoveLibraryDirectoryDownButton.Name = "FMoveLibraryDirectoryDownButton";
			this.FMoveLibraryDirectoryDownButton.Size = new System.Drawing.Size(58, 23);
			this.FMoveLibraryDirectoryDownButton.TabIndex = 78;
			this.FMoveLibraryDirectoryDownButton.Text = "Down";
			this.FMoveLibraryDirectoryDownButton.UseVisualStyleBackColor = true;
			this.FMoveLibraryDirectoryDownButton.Click += new System.EventHandler(this.MoveLibraryDirectoryDownButton_Click);
			// 
			// FMoveLibraryDirectoryUpButton
			// 
			this.FMoveLibraryDirectoryUpButton.Location = new System.Drawing.Point(181, 49);
			this.FMoveLibraryDirectoryUpButton.Name = "FMoveLibraryDirectoryUpButton";
			this.FMoveLibraryDirectoryUpButton.Size = new System.Drawing.Size(58, 23);
			this.FMoveLibraryDirectoryUpButton.TabIndex = 77;
			this.FMoveLibraryDirectoryUpButton.Text = "Up";
			this.FMoveLibraryDirectoryUpButton.UseVisualStyleBackColor = true;
			this.FMoveLibraryDirectoryUpButton.Click += new System.EventHandler(this.MoveLibraryDirectoryUpButton_Click);
			// 
			// FRemoveLibraryDirectoryButton
			// 
			this.FRemoveLibraryDirectoryButton.Location = new System.Drawing.Point(123, 49);
			this.FRemoveLibraryDirectoryButton.Name = "FRemoveLibraryDirectoryButton";
			this.FRemoveLibraryDirectoryButton.Size = new System.Drawing.Size(58, 23);
			this.FRemoveLibraryDirectoryButton.TabIndex = 76;
			this.FRemoveLibraryDirectoryButton.Text = "Remove";
			this.FRemoveLibraryDirectoryButton.UseVisualStyleBackColor = true;
			this.FRemoveLibraryDirectoryButton.Click += new System.EventHandler(this.RemoveLibraryDirectoryButton_Click);
			// 
			// FUpdateLibraryDirectoryButton
			// 
			this.FUpdateLibraryDirectoryButton.Location = new System.Drawing.Point(65, 49);
			this.FUpdateLibraryDirectoryButton.Name = "FUpdateLibraryDirectoryButton";
			this.FUpdateLibraryDirectoryButton.Size = new System.Drawing.Size(58, 23);
			this.FUpdateLibraryDirectoryButton.TabIndex = 75;
			this.FUpdateLibraryDirectoryButton.Text = "Update";
			this.FUpdateLibraryDirectoryButton.UseVisualStyleBackColor = true;
			this.FUpdateLibraryDirectoryButton.Click += new System.EventHandler(this.UpdateLibraryDirectoryButton_Click);
			// 
			// FAddLibraryDirectoryButton
			// 
			this.FAddLibraryDirectoryButton.Location = new System.Drawing.Point(7, 49);
			this.FAddLibraryDirectoryButton.Name = "FAddLibraryDirectoryButton";
			this.FAddLibraryDirectoryButton.Size = new System.Drawing.Size(58, 23);
			this.FAddLibraryDirectoryButton.TabIndex = 74;
			this.FAddLibraryDirectoryButton.Text = "Add";
			this.FAddLibraryDirectoryButton.UseVisualStyleBackColor = true;
			this.FAddLibraryDirectoryButton.Click += new System.EventHandler(this.AddLibraryDirectoryButton_Click);
			// 
			// FLibraryDirectoriesListBox
			// 
			this.FLibraryDirectoriesListBox.FormattingEnabled = true;
			this.FLibraryDirectoriesListBox.Location = new System.Drawing.Point(6, 78);
			this.FLibraryDirectoriesListBox.Name = "FLibraryDirectoriesListBox";
			this.FLibraryDirectoriesListBox.Size = new System.Drawing.Size(291, 199);
			this.FLibraryDirectoriesListBox.TabIndex = 1;
			this.FLibraryDirectoriesListBox.SelectedIndexChanged += new System.EventHandler(this.LibraryDirectoriesListBox_SelectedIndexChanged);
			// 
			// FSelectLibraryDirectoryButton
			// 
			this.FSelectLibraryDirectoryButton.Location = new System.Drawing.Point(273, 22);
			this.FSelectLibraryDirectoryButton.Name = "FSelectLibraryDirectoryButton";
			this.FSelectLibraryDirectoryButton.Size = new System.Drawing.Size(24, 22);
			this.FSelectLibraryDirectoryButton.TabIndex = 72;
			this.FSelectLibraryDirectoryButton.Text = "...";
			this.FSelectLibraryDirectoryButton.Click += new System.EventHandler(this.SelectLibraryDirectoryButton_Click);
			// 
			// FLibraryDirectoriesTextBox
			// 
			this.FLibraryDirectoriesTextBox.Location = new System.Drawing.Point(6, 24);
			this.FLibraryDirectoriesTextBox.Name = "FLibraryDirectoriesTextBox";
			this.FLibraryDirectoriesTextBox.Size = new System.Drawing.Size(261, 20);
			this.FLibraryDirectoriesTextBox.TabIndex = 0;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.FRequireSecureConnectionComboBox);
			this.groupBox4.Controls.Add(this.FInstanceNameTextBox);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this.label13);
			this.groupBox4.Controls.Add(this.label11);
			this.groupBox4.Controls.Add(this.FSelectInstanceDirectoryButton);
			this.groupBox4.Controls.Add(this.FPortNumberTextBox);
			this.groupBox4.Controls.Add(this.FInstanceDirectoryTextBox);
			this.groupBox4.Location = new System.Drawing.Point(12, 4);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(306, 138);
			this.groupBox4.TabIndex = 0;
			this.groupBox4.TabStop = false;
			// 
			// FRequireSecureConnectionComboBox
			// 
			this.FRequireSecureConnectionComboBox.AutoSize = true;
			this.FRequireSecureConnectionComboBox.Location = new System.Drawing.Point(110, 72);
			this.FRequireSecureConnectionComboBox.Name = "FRequireSecureConnectionComboBox";
			this.FRequireSecureConnectionComboBox.Size = new System.Drawing.Size(157, 17);
			this.FRequireSecureConnectionComboBox.TabIndex = 60;
			this.FRequireSecureConnectionComboBox.Text = "Require Secure Connection";
			this.FRequireSecureConnectionComboBox.UseVisualStyleBackColor = true;
			// 
			// FGroupBox5
			// 
			this.FGroupBox5.Controls.Add(this.FUseServiceConfigurationCheckBox);
			this.FGroupBox5.Controls.Add(this.FAllowSilverlightClientsComboBox);
			this.FGroupBox5.Controls.Add(this.FShouldListenComboBox);
			this.FGroupBox5.Controls.Add(this.FLabel10);
			this.FGroupBox5.Controls.Add(this.FRequireSecureListenerConnectionComboBox);
			this.FGroupBox5.Controls.Add(this.FLabel9);
			this.FGroupBox5.Controls.Add(this.FOverrideListenerPortNumberTextBox);
			this.FGroupBox5.Location = new System.Drawing.Point(326, 4);
			this.FGroupBox5.Name = "FGroupBox5";
			this.FGroupBox5.Size = new System.Drawing.Size(354, 138);
			this.FGroupBox5.TabIndex = 4;
			this.FGroupBox5.TabStop = false;
			this.FGroupBox5.Text = "Advanced Network Configuration";
			// 
			// FUseServiceConfigurationCheckBox
			// 
			this.FUseServiceConfigurationCheckBox.AutoSize = true;
			this.FUseServiceConfigurationCheckBox.Location = new System.Drawing.Point(179, 19);
			this.FUseServiceConfigurationCheckBox.Name = "FUseServiceConfigurationCheckBox";
			this.FUseServiceConfigurationCheckBox.Size = new System.Drawing.Size(155, 17);
			this.FUseServiceConfigurationCheckBox.TabIndex = 71;
			this.FUseServiceConfigurationCheckBox.Text = "Use Service Configuration?";
			this.FUseServiceConfigurationCheckBox.UseVisualStyleBackColor = true;
			// 
			// FAllowSilverlightClientsComboBox
			// 
			this.FAllowSilverlightClientsComboBox.AutoSize = true;
			this.FAllowSilverlightClientsComboBox.Location = new System.Drawing.Point(14, 112);
			this.FAllowSilverlightClientsComboBox.Name = "FAllowSilverlightClientsComboBox";
			this.FAllowSilverlightClientsComboBox.Size = new System.Drawing.Size(139, 17);
			this.FAllowSilverlightClientsComboBox.TabIndex = 70;
			this.FAllowSilverlightClientsComboBox.Text = "Allow Silverlight Clients?";
			this.FAllowSilverlightClientsComboBox.UseVisualStyleBackColor = true;
			// 
			// FShouldListenComboBox
			// 
			this.FShouldListenComboBox.AutoSize = true;
			this.FShouldListenComboBox.Location = new System.Drawing.Point(14, 19);
			this.FShouldListenComboBox.Name = "FShouldListenComboBox";
			this.FShouldListenComboBox.Size = new System.Drawing.Size(150, 17);
			this.FShouldListenComboBox.TabIndex = 69;
			this.FShouldListenComboBox.Text = "Should Establish Listener?";
			this.FShouldListenComboBox.UseVisualStyleBackColor = true;
			// 
			// FLabel10
			// 
			this.FLabel10.AutoSize = true;
			this.FLabel10.Location = new System.Drawing.Point(11, 38);
			this.FLabel10.Name = "FLabel10";
			this.FLabel10.Size = new System.Drawing.Size(117, 13);
			this.FLabel10.TabIndex = 68;
			this.FLabel10.Text = "Listener Port Overrides:";
			// 
			// FRequireSecureListenerConnectionComboBox
			// 
			this.FRequireSecureListenerConnectionComboBox.AutoSize = true;
			this.FRequireSecureListenerConnectionComboBox.Location = new System.Drawing.Point(122, 73);
			this.FRequireSecureListenerConnectionComboBox.Name = "FRequireSecureListenerConnectionComboBox";
			this.FRequireSecureListenerConnectionComboBox.Size = new System.Drawing.Size(157, 17);
			this.FRequireSecureListenerConnectionComboBox.TabIndex = 65;
			this.FRequireSecureListenerConnectionComboBox.Text = "Require Secure Connection";
			this.FRequireSecureListenerConnectionComboBox.UseVisualStyleBackColor = true;
			// 
			// FLabel9
			// 
			this.FLabel9.AutoSize = true;
			this.FLabel9.Location = new System.Drawing.Point(11, 55);
			this.FLabel9.Name = "FLabel9";
			this.FLabel9.Size = new System.Drawing.Size(66, 13);
			this.FLabel9.TabIndex = 64;
			this.FLabel9.Text = "Port Number";
			// 
			// FOverrideListenerPortNumberTextBox
			// 
			this.FOverrideListenerPortNumberTextBox.Location = new System.Drawing.Point(14, 72);
			this.FOverrideListenerPortNumberTextBox.Name = "FOverrideListenerPortNumberTextBox";
			this.FOverrideListenerPortNumberTextBox.Size = new System.Drawing.Size(64, 20);
			this.FOverrideListenerPortNumberTextBox.TabIndex = 63;
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
			this.FGroupBox5.ResumeLayout(false);
			this.FGroupBox5.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }
        #endregion

		private System.Windows.Forms.CheckBox FUseServiceConfigurationCheckBox;

    }
}
