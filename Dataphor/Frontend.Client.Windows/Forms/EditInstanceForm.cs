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
            tbCatalogStoreClassName.AutoCompleteCustomSource.AddRange(GetCatalogStoreClassNames());
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
			tbInstanceName.Text = AConfiguration.Name;
			tbPortNumber.Text = AConfiguration.PortNumber.ToString();
			tbSecurePortNumber.Text = AConfiguration.SecurePortNumber.ToString();
			cbRequireSecureConnection.Checked = AConfiguration.RequireSecureConnection;
			cbShouldListen.Checked = AConfiguration.ShouldListen;
			tbOverrideListenerPortNumber.Text = AConfiguration.OverrideListenerPortNumber.ToString();
			tbOverrideSecureListenerPortNumber.Text = AConfiguration.OverrideSecureListenerPortNumber.ToString();
			cbRequireSecureListenerConnection.Checked = AConfiguration.RequireSecureListenerConnection;
			cbAllowSilverlightClients.Checked = AConfiguration.AllowSilverlightClients;
			tbInstanceDirectory.Text = AConfiguration.InstanceDirectory;
			LibraryDirectoriesListBox.Items.Clear();
            if (AConfiguration.LibraryDirectories != null)
            {
                string[] LLibraryDirectories = AConfiguration.LibraryDirectories.Split(';');
                for (int LIndex = 0; LIndex < LLibraryDirectories.Length; LIndex++)
                    LibraryDirectoriesListBox.Items.Add(LLibraryDirectories[LIndex]);
            }
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
			LResult.SecurePortNumber = Int32.Parse(tbSecurePortNumber.Text);
			LResult.RequireSecureConnection = cbRequireSecureConnection.Checked;
			LResult.ShouldListen = cbShouldListen.Checked;
			LResult.OverrideListenerPortNumber = Int32.Parse(tbOverrideListenerPortNumber.Text);
			LResult.OverrideSecureListenerPortNumber = Int32.Parse(tbOverrideSecureListenerPortNumber.Text);
			LResult.RequireSecureListenerConnection = cbRequireSecureListenerConnection.Checked;
			LResult.AllowSilverlightClients = cbAllowSilverlightClients.Checked;
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
			Int32.Parse(((System.Windows.Forms.TextBox)sender).Text);
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

		private void SelectInstanceDirectoryButton_Click(object sender, EventArgs e)
		{
			try
			{
				tbInstanceDirectory.Text = FolderUtility.GetDirectory(tbInstanceDirectory.Text);
			}
			catch (AbortException) { }
		}

		private void SelectLibraryDirectoryButton_Click(object sender, EventArgs e)
		{
			try
			{
				tbLibraryDirectories.Text = FolderUtility.GetDirectory(tbLibraryDirectories.Text);
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
