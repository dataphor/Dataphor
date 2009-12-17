/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.DAE.Server;
using System.Text;
using System.IO;
using Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;


namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Form for adding and editing ServerAlias instances. </summary>
	public partial class EditInstanceForm : BaseForm
	{

		public EditInstanceForm()
		{
			InitializeComponent();
            FCatalogStoreClassNameTextBox.AutoCompleteCustomSource.AddRange(GetCatalogStoreClassNames());
                //= GetCatalogStoreClassNames();
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
			FInstanceNameTextBox.Text = AConfiguration.Name;
			FPortNumberTextBox.Text = AConfiguration.PortNumber.ToString();
			FSecurePortNumberTextBox.Text = AConfiguration.SecurePortNumber.ToString();
			FRequireSecureConnectionComboBox.Checked = AConfiguration.RequireSecureConnection;
			FShouldListenComboBox.Checked = AConfiguration.ShouldListen;
			FOverrideListenerPortNumberTextBox.Text = AConfiguration.OverrideListenerPortNumber.ToString();
			FOverrideSecureListenerPortNumberTextBox.Text = AConfiguration.OverrideSecureListenerPortNumber.ToString();
			FRequireSecureListenerConnectionComboBox.Checked = AConfiguration.RequireSecureListenerConnection;
			FAllowSilverlightClientsComboBox.Checked = AConfiguration.AllowSilverlightClients;
			FInstanceDirectoryTextBox.Text = AConfiguration.InstanceDirectory;
			FLibraryDirectoriesListBox.Items.Clear();
            if (AConfiguration.LibraryDirectories != null)
            {
                string[] LLibraryDirectories = AConfiguration.LibraryDirectories.Split(';');
                for (int LIndex = 0; LIndex < LLibraryDirectories.Length; LIndex++)
                    FLibraryDirectoriesListBox.Items.Add(LLibraryDirectories[LIndex]);
            }
		    FCatalogStoreClassNameTextBox.Text = AConfiguration.CatalogStoreClassName;
			FCatalogStoreConnectionStringTextBox.Text = AConfiguration.CatalogStoreConnectionString;
			FDeviceSettingsListBox.Items.Clear();
			foreach (DeviceSetting LDeviceSetting in AConfiguration.DeviceSettings)
				FDeviceSettingsListBox.Items.Add(LDeviceSetting);
		}
		
		public ServerConfiguration CreateConfiguration()
		{
			ServerConfiguration LResult = new ServerConfiguration();
			LResult.Name = FInstanceNameTextBox.Text;
			LResult.PortNumber = Int32.Parse(FPortNumberTextBox.Text);
			LResult.SecurePortNumber = Int32.Parse(FSecurePortNumberTextBox.Text);
			LResult.RequireSecureConnection = FRequireSecureConnectionComboBox.Checked;
			LResult.ShouldListen = FShouldListenComboBox.Checked;
			LResult.OverrideListenerPortNumber = Int32.Parse(FOverrideListenerPortNumberTextBox.Text);
			LResult.OverrideSecureListenerPortNumber = Int32.Parse(FOverrideSecureListenerPortNumberTextBox.Text);
			LResult.RequireSecureListenerConnection = FRequireSecureListenerConnectionComboBox.Checked;
			LResult.AllowSilverlightClients = FAllowSilverlightClientsComboBox.Checked;
			LResult.InstanceDirectory = FInstanceDirectoryTextBox.Text;
			StringBuilder LLibraryDirectories = new StringBuilder();
			for (int LIndex = 0; LIndex < FLibraryDirectoriesListBox.Items.Count; LIndex++)
			{
				if (LIndex > 0)
					LLibraryDirectories.Append(Path.PathSeparator);
				LLibraryDirectories.Append(FLibraryDirectoriesListBox.Items[LIndex] as String);
			}
			LResult.LibraryDirectories = LLibraryDirectories.ToString();
			LResult.CatalogStoreClassName = FCatalogStoreClassNameTextBox.Text;
			LResult.CatalogStoreConnectionString = FCatalogStoreConnectionStringTextBox.Text;
			for (int LIndex = 0; LIndex < FDeviceSettingsListBox.Items.Count; LIndex++)
				LResult.DeviceSettings.Add((DeviceSetting)FDeviceSettingsListBox.Items[LIndex]);
			return LResult;
		}

		private void tbPortNumber_TextChanged(object sender, EventArgs e)
		{
			Int32.Parse(((System.Windows.Forms.TextBox)sender).Text);
		}

		private void LibraryDirectoriesListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			FLibraryDirectoriesTextBox.Text = FLibraryDirectoriesListBox.SelectedItem as String;
		}

		private void AddLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			FLibraryDirectoriesListBox.Items.Add(FLibraryDirectoriesTextBox.Text);
		}

		private void UpdateLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			if (FLibraryDirectoriesListBox.SelectedIndex >= 0)
				FLibraryDirectoriesListBox.Items[FLibraryDirectoriesListBox.SelectedIndex] = FLibraryDirectoriesTextBox.Text;
		}

		private void RemoveLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			if (FLibraryDirectoriesListBox.SelectedIndex >= 0)
				FLibraryDirectoriesListBox.Items.RemoveAt(FLibraryDirectoriesListBox.SelectedIndex);
		}

		private void MoveLibraryDirectoryUpButton_Click(object sender, EventArgs e)
		{
			int LIndex = FLibraryDirectoriesListBox.SelectedIndex;
			if (LIndex >= 1)
			{
				string LValue = FLibraryDirectoriesListBox.Items[LIndex] as String;
				FLibraryDirectoriesListBox.Items.RemoveAt(LIndex);
				FLibraryDirectoriesListBox.Items.Insert(LIndex - 1, LValue);
			}
		}

		private void MoveLibraryDirectoryDownButton_Click(object sender, EventArgs e)
		{
			int LIndex = FLibraryDirectoriesListBox.SelectedIndex;
			if ((LIndex >= 0) && (LIndex < FLibraryDirectoriesListBox.Items.Count - 1))
			{
				string LValue = FLibraryDirectoriesListBox.Items[LIndex] as String;
				FLibraryDirectoriesListBox.Items.RemoveAt(LIndex);
				FLibraryDirectoriesListBox.Items.Insert(LIndex + 1, LValue);
			}
		}
		
		private void PushSetting(DeviceSetting ASetting)
		{
			ASetting.DeviceName = FDeviceNameTextBox.Text;
			ASetting.SettingName = FSettingNameTextBox.Text;
			ASetting.SettingValue = FSettingValueTextBox.Text;
		}

		private void DeviceSettingsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			DeviceSetting LSetting = FDeviceSettingsListBox.SelectedItem as DeviceSetting;
			if (LSetting != null)
			{
				FDeviceNameTextBox.Text = LSetting.DeviceName;
				FSettingNameTextBox.Text = LSetting.SettingName;
				FSettingValueTextBox.Text = LSetting.SettingValue;
			}
		}

		private void AddDeviceSettingButton_Click(object sender, EventArgs e)
		{
			DeviceSetting LSetting = new DeviceSetting();
			PushSetting(LSetting);
			FDeviceSettingsListBox.Items.Add(LSetting);
		}

		private void UpdateDeviceSettingButton_Click(object sender, EventArgs e)
		{
			if (FDeviceSettingsListBox.SelectedIndex >= 0)
			{
				DeviceSetting LSetting = new DeviceSetting();
				PushSetting(LSetting);
				FDeviceSettingsListBox.Items[FDeviceSettingsListBox.SelectedIndex] = LSetting;
			}
		}

		private void RemoveDeviceSettingButton_Click(object sender, EventArgs e)
		{
			if (FDeviceSettingsListBox.SelectedIndex >= 0)
				FDeviceSettingsListBox.Items.RemoveAt(FDeviceSettingsListBox.SelectedIndex);
		}

		private void SelectInstanceDirectoryButton_Click(object sender, EventArgs e)
		{
			try
			{
				FInstanceDirectoryTextBox.Text = FolderUtility.GetDirectory(FInstanceDirectoryTextBox.Text);
			}
			catch (AbortException) { }
		}

		private void SelectLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			try
			{
				FLibraryDirectoriesTextBox.Text = FolderUtility.GetDirectory(FLibraryDirectoriesTextBox.Text);
			}
			catch (AbortException) { }
        }

        private string[] GetCatalogStoreClassNames()
        {
            string LPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo LDirectory = new DirectoryInfo(LPath);
            FileInfo[] LFiles = LDirectory.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            List<string> LCatalogStoreClassNames = new List<string>();

            foreach (FileInfo file in LFiles)
            {
                // Load the file into the application domain.
                try
                {
                    AssemblyName LAssemblyName = AssemblyName.GetAssemblyName(file.FullName);
                    var LAssembly = Assembly.Load(LAssemblyName.ToString());
                    foreach (var LType in LAssembly.GetTypes())
                    {
                        if(LType.IsSubclassOf(typeof(Alphora.Dataphor.DAE.Store.SQLStore)))
                        {
                            LCatalogStoreClassNames.Add(LType.FullName +","+ LAssemblyName.Name);
                        }
                    }
                }
                catch (BadImageFormatException LBadImageFormatException)
                {
                    //HACK: How do I know if a .dll file is (or not) a .NET assembly?                   
                }
            }
            return LCatalogStoreClassNames.ToArray();
        }

	}
}
