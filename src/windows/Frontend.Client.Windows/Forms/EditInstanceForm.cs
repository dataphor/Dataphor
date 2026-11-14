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
		
		public static ServerConfiguration ExecuteEdit(ServerConfiguration configuration)
		{
			using (EditInstanceForm form = new EditInstanceForm())
			{
				form.SetFromConfiguration(configuration);
				if (form.ShowDialog() != DialogResult.OK)
					throw new AbortException();
				return form.CreateConfiguration();
			}
		}
		
		public void SetFromConfiguration(ServerConfiguration configuration)
		{
			FInstanceNameTextBox.Text = configuration.Name;
			FPortNumberTextBox.Text = configuration.PortNumber.ToString();
			FRequireSecureConnectionComboBox.Checked = configuration.RequireSecureConnection;
			FShouldListenComboBox.Checked = configuration.ShouldListen;
			FUseServiceConfigurationCheckBox.Checked = configuration.UseServiceConfiguration;
			FOverrideListenerPortNumberTextBox.Text = configuration.OverrideListenerPortNumber.ToString();
			FRequireSecureListenerConnectionComboBox.Checked = configuration.RequireSecureListenerConnection;
			FAllowSilverlightClientsComboBox.Checked = configuration.AllowSilverlightClients;
			FInstanceDirectoryTextBox.Text = configuration.InstanceDirectory;
			FLibraryDirectoriesListBox.Items.Clear();
            if (configuration.LibraryDirectories != null)
            {
                string[] libraryDirectories = configuration.LibraryDirectories.Split(';');
                for (int index = 0; index < libraryDirectories.Length; index++)
                    FLibraryDirectoriesListBox.Items.Add(libraryDirectories[index]);
            }
		    FCatalogStoreClassNameTextBox.Text = configuration.CatalogStoreClassName;
			FCatalogStoreConnectionStringTextBox.Text = configuration.CatalogStoreConnectionString;
			FDeviceSettingsListBox.Items.Clear();
			foreach (DeviceSetting deviceSetting in configuration.DeviceSettings)
				FDeviceSettingsListBox.Items.Add(deviceSetting);
		}
		
		public ServerConfiguration CreateConfiguration()
		{
			ServerConfiguration result = new ServerConfiguration();
			result.Name = FInstanceNameTextBox.Text;
			result.PortNumber = Int32.Parse(FPortNumberTextBox.Text);
			result.RequireSecureConnection = FRequireSecureConnectionComboBox.Checked;
			result.ShouldListen = FShouldListenComboBox.Checked;
			result.UseServiceConfiguration = FUseServiceConfigurationCheckBox.Checked;
			result.OverrideListenerPortNumber = Int32.Parse(FOverrideListenerPortNumberTextBox.Text);
			result.RequireSecureListenerConnection = FRequireSecureListenerConnectionComboBox.Checked;
			result.AllowSilverlightClients = FAllowSilverlightClientsComboBox.Checked;
			result.InstanceDirectory = FInstanceDirectoryTextBox.Text;
			StringBuilder libraryDirectories = new StringBuilder();
			for (int index = 0; index < FLibraryDirectoriesListBox.Items.Count; index++)
			{
				if (index > 0)
					libraryDirectories.Append(Path.PathSeparator);
				libraryDirectories.Append(FLibraryDirectoriesListBox.Items[index] as String);
			}
			result.LibraryDirectories = libraryDirectories.ToString();
			result.CatalogStoreClassName = FCatalogStoreClassNameTextBox.Text;
			result.CatalogStoreConnectionString = FCatalogStoreConnectionStringTextBox.Text;
			for (int index = 0; index < FDeviceSettingsListBox.Items.Count; index++)
				result.DeviceSettings.Add((DeviceSetting)FDeviceSettingsListBox.Items[index]);
			return result;
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
			int index = FLibraryDirectoriesListBox.SelectedIndex;
			if (index >= 1)
			{
				string tempValue = FLibraryDirectoriesListBox.Items[index] as String;
				FLibraryDirectoriesListBox.Items.RemoveAt(index);
				FLibraryDirectoriesListBox.Items.Insert(index - 1, tempValue);
			}
		}

		private void MoveLibraryDirectoryDownButton_Click(object sender, EventArgs e)
		{
			int index = FLibraryDirectoriesListBox.SelectedIndex;
			if ((index >= 0) && (index < FLibraryDirectoriesListBox.Items.Count - 1))
			{
				string tempValue = FLibraryDirectoriesListBox.Items[index] as String;
				FLibraryDirectoriesListBox.Items.RemoveAt(index);
				FLibraryDirectoriesListBox.Items.Insert(index + 1, tempValue);
			}
		}
		
		private void PushSetting(DeviceSetting setting)
		{
			setting.DeviceName = FDeviceNameTextBox.Text;
			setting.SettingName = FSettingNameTextBox.Text;
			setting.SettingValue = FSettingValueTextBox.Text;
		}

		private void DeviceSettingsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			DeviceSetting setting = FDeviceSettingsListBox.SelectedItem as DeviceSetting;
			if (setting != null)
			{
				FDeviceNameTextBox.Text = setting.DeviceName;
				FSettingNameTextBox.Text = setting.SettingName;
				FSettingValueTextBox.Text = setting.SettingValue;
			}
		}

		private void AddDeviceSettingButton_Click(object sender, EventArgs e)
		{
			DeviceSetting setting = new DeviceSetting();
			PushSetting(setting);
			FDeviceSettingsListBox.Items.Add(setting);
		}

		private void UpdateDeviceSettingButton_Click(object sender, EventArgs e)
		{
			if (FDeviceSettingsListBox.SelectedIndex >= 0)
			{
				DeviceSetting setting = new DeviceSetting();
				PushSetting(setting);
				FDeviceSettingsListBox.Items[FDeviceSettingsListBox.SelectedIndex] = setting;
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
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] files = directory.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            List<string> catalogStoreClassNames = new List<string>();

			try
			{
				foreach (FileInfo file in files)
				{
					// Load the file into the application domain.
					try
					{
						AssemblyName assemblyName = AssemblyName.GetAssemblyName(file.FullName);
						var assembly = Assembly.Load(assemblyName.ToString());
						foreach (var type in assembly.GetTypes())
						{
							if (type.IsSubclassOf(typeof(Alphora.Dataphor.DAE.Store.SQLStore)))
							{
								catalogStoreClassNames.Add(type.FullName + "," + assemblyName.Name);
							}
						}
					}
					catch (BadImageFormatException)
					{
						//HACK: How do I know if a .dll file is (or not) a .NET assembly?
						// BTR: According to Microsoft, this hack is the correct way to determine whether you have a .NET assembly.
						// See the FileUtility.IsAssembly
					}
				}
			}
			catch
			{
				// Eat the exception here, do not let this stop the configuration utility from loading.
			}
			
            return catalogStoreClassNames.ToArray();
        }
	}
}
