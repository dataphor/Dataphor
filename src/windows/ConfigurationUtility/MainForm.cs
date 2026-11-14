/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Reflection;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	// This must be the 1st class
	public class MainForm : System.Windows.Forms.Form
	{
		// Localize these strings
		public const string CatalogBrowseTitle = "Catalog Directory";
		public const string LibraryBrowseTitle = "Library Directory";
		public const string StartupScriptBrowseTitle = "Open Startup Script";
		public const string StartupScriptBrowseFilter = "D4 Script Files (*.d4)|*.d4|All Files (*.*)|*.*";
		public const string RestartToApply = "The new settings will not apply until the service has been re-started.";

		// Do not localize
		public const string WindowIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.ConfigUtil-big.ico";
		public const string StartupScriptDefaultExtension = ".d4";
		public const string AppAutoStartRegKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
		public const string AppAutoStartRegValueName = "Dataphor Service Manager";

		public System.Windows.Forms.CheckBox ServiceAutoStart;
		public System.Windows.Forms.PictureBox ServerStatusPicture;
		public System.Windows.Forms.Button StartStopButton;
		private System.Windows.Forms.GroupBox ServiceStatus;
		private System.Windows.Forms.Button configureButton;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem ShowTrayIcon;
		private System.Windows.Forms.MenuItem AppAutoStart;
		private ComboBox cbInstance;
		private Button NewInstanceButton;
		private Label label1;
		public Button InstallButton;
		private IContainer components;

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.ServiceStatus = new System.Windows.Forms.GroupBox();
			this.StartStopButton = new System.Windows.Forms.Button();
			this.configureButton = new System.Windows.Forms.Button();
			this.ServiceAutoStart = new System.Windows.Forms.CheckBox();
			this.ServerStatusPicture = new System.Windows.Forms.PictureBox();
			this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.ShowTrayIcon = new System.Windows.Forms.MenuItem();
			this.AppAutoStart = new System.Windows.Forms.MenuItem();
			this.cbInstance = new System.Windows.Forms.ComboBox();
			this.NewInstanceButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.InstallButton = new System.Windows.Forms.Button();
			this.ServiceStatus.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ServerStatusPicture)).BeginInit();
			this.SuspendLayout();
			// 
			// ServiceStatus
			// 
			this.ServiceStatus.Controls.Add(this.InstallButton);
			this.ServiceStatus.Controls.Add(this.StartStopButton);
			this.ServiceStatus.Controls.Add(this.configureButton);
			this.ServiceStatus.Controls.Add(this.ServiceAutoStart);
			this.ServiceStatus.Controls.Add(this.ServerStatusPicture);
			this.ServiceStatus.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.ServiceStatus.Location = new System.Drawing.Point(10, 44);
			this.ServiceStatus.Name = "ServiceStatus";
			this.ServiceStatus.Size = new System.Drawing.Size(225, 122);
			this.ServiceStatus.TabIndex = 12;
			this.ServiceStatus.TabStop = false;
			this.ServiceStatus.Text = "Service Status";
			// 
			// StartStopButton
			// 
			this.StartStopButton.Location = new System.Drawing.Point(103, 19);
			this.StartStopButton.Name = "StartStopButton";
			this.StartStopButton.Size = new System.Drawing.Size(93, 26);
			this.StartStopButton.TabIndex = 17;
			this.StartStopButton.Text = "Start";
			this.StartStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
			// 
			// configureButton
			// 
			this.configureButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.configureButton.Location = new System.Drawing.Point(9, 84);
			this.configureButton.Name = "configureButton";
			this.configureButton.Size = new System.Drawing.Size(97, 28);
			this.configureButton.TabIndex = 16;
			this.configureButton.Text = "&Configure...";
			this.configureButton.Click += new System.EventHandler(this.configureButton_Click);
			// 
			// ServiceAutoStart
			// 
			this.ServiceAutoStart.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.ServiceAutoStart.Location = new System.Drawing.Point(103, 56);
			this.ServiceAutoStart.Name = "ServiceAutoStart";
			this.ServiceAutoStart.Size = new System.Drawing.Size(113, 19);
			this.ServiceAutoStart.TabIndex = 10;
			this.ServiceAutoStart.Text = "Auto Start Service";
			this.ServiceAutoStart.Click += new System.EventHandler(this.ServiceAutoStart_Click);
			// 
			// ServerStatusPicture
			// 
			this.ServerStatusPicture.Location = new System.Drawing.Point(18, 19);
			this.ServerStatusPicture.Name = "ServerStatusPicture";
			this.ServerStatusPicture.Size = new System.Drawing.Size(57, 56);
			this.ServerStatusPicture.TabIndex = 0;
			this.ServerStatusPicture.TabStop = false;
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.ShowTrayIcon,
            this.AppAutoStart});
			this.menuItem1.Text = "&Options";
			// 
			// ShowTrayIcon
			// 
			this.ShowTrayIcon.Index = 0;
			this.ShowTrayIcon.Text = "Show &Icon in the System Tray";
			this.ShowTrayIcon.Click += new System.EventHandler(this.ShowTrayIcon_Click);
			// 
			// AppAutoStart
			// 
			this.AppAutoStart.Index = 1;
			this.AppAutoStart.Text = "&Run Configuration Utility at Startup";
			this.AppAutoStart.Click += new System.EventHandler(this.AppAutoStart_Click);
			// 
			// cbInstance
			// 
			this.cbInstance.FormattingEnabled = true;
			this.cbInstance.Location = new System.Drawing.Point(63, 12);
			this.cbInstance.Name = "cbInstance";
			this.cbInstance.Size = new System.Drawing.Size(123, 21);
			this.cbInstance.TabIndex = 13;
			this.cbInstance.SelectedIndexChanged += new System.EventHandler(this.cbInstance_SelectedIndexChanged);
			// 
			// NewInstanceButton
			// 
			this.NewInstanceButton.Location = new System.Drawing.Point(189, 12);
			this.NewInstanceButton.Name = "NewInstanceButton";
			this.NewInstanceButton.Size = new System.Drawing.Size(46, 22);
			this.NewInstanceButton.TabIndex = 14;
			this.NewInstanceButton.Text = "New...";
			this.NewInstanceButton.UseVisualStyleBackColor = true;
			this.NewInstanceButton.Click += new System.EventHandler(this.NewInstanceButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 15;
			this.label1.Text = "Instance";
			// 
			// InstallButton
			// 
			this.InstallButton.Location = new System.Drawing.Point(112, 84);
			this.InstallButton.Name = "InstallButton";
			this.InstallButton.Size = new System.Drawing.Size(104, 28);
			this.InstallButton.TabIndex = 18;
			this.InstallButton.Text = "Install...";
			this.InstallButton.UseVisualStyleBackColor = true;
			this.InstallButton.Click += new System.EventHandler(this.InstallButton_Click);
			// 
			// MainForm
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(244, 178);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.NewInstanceButton);
			this.Controls.Add(this.cbInstance);
			this.Controls.Add(this.ServiceStatus);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Menu = this.mainMenu1;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Dataphor Status";
			this.ServiceStatus.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.ServerStatusPicture)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private ApplicationForm _appForm;

		public MainForm(ApplicationForm LAppForm)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			_appForm = LAppForm;

			LoadInstances(InstanceManager.LoadConfiguration());
			cbInstance.Text = _appForm.SelectedInstanceName;
			ShowTrayIcon.Checked = _appForm._configurationUtilitySettings.ShowTrayIcon;
			AppAutoStart.Checked = _appForm._configurationUtilitySettings.AppAutoStart;
		}
	
		private void ShowTrayIcon_Click(object sender, System.EventArgs args)
		{
			_appForm._configurationUtilitySettings.ShowTrayIcon = !_appForm._configurationUtilitySettings.ShowTrayIcon;
			ShowTrayIcon.Checked = _appForm._configurationUtilitySettings.ShowTrayIcon;

			_appForm.Serialize();

			_appForm._trayIcon.Visible = ShowTrayIcon.Checked;
		}

		private void AppAutoStart_Click(object sender, System.EventArgs args)
		{
			try
			{
				_appForm._configurationUtilitySettings.AppAutoStart = !_appForm._configurationUtilitySettings.AppAutoStart;
				AppAutoStart.Checked = _appForm._configurationUtilitySettings.AppAutoStart;

				_appForm.Serialize();

				using (Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(AppAutoStartRegKeyName, true))
				{
					if (AppAutoStart.Checked)
                        regKey.SetValue(AppAutoStartRegValueName, Application.ExecutablePath + " " + Program.SilentMode);
					else
						regKey.DeleteValue(AppAutoStartRegValueName, false);
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			_appForm._mainFormIsShowing = false;

			if (_appForm._configurationUtilitySettings.ShowTrayIcon == false)
				Application.Exit();

			base.OnClosing(args);
		}

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
		
		private void ServiceAutoStart_Click(object sender, System.EventArgs args)
		{
			try
			{
				ServiceUtility.SetServiceAutoStart(_appForm.SelectedInstanceName, ServiceAutoStart.Checked);
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		private void StartStopButton_Click(object sender, System.EventArgs e)
		{
			if (_appForm._serviceStatus == DAE.Service.ServiceStatus.Stopped)
				StartService();
			else
				StopService();
		}

		private void StartService()
		{
			if ((_appForm._serviceStatus == DAE.Service.ServiceStatus.Running) || (_appForm._serviceStatus == DAE.Service.ServiceStatus.Unavailable))
				return;

			System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController(ServiceUtility.GetServiceName(_appForm.SelectedInstanceName));
			try
			{				
				using(StatusForm statusForm = new StatusForm("Starting...", this))
				{
					// Start the service
					Cursor.Current = Cursors.WaitCursor;
					_appForm._timer.Stop();
					serviceController.Start(new string[] { "-n", _appForm.SelectedInstanceName });
					// Wait for the service to start...give it 30 seconds
					serviceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
					_appForm.CheckServiceStatus();
				}
			}
			catch (Exception AException)
			{
				Application.OnThreadException(AException);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
				_appForm._timer.Start();
			}		
		}

		private void StopService()
		{
			if ((_appForm._serviceStatus == DAE.Service.ServiceStatus.Stopped) || (_appForm._serviceStatus == DAE.Service.ServiceStatus.Unavailable))
				return;

			System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController(ServiceUtility.GetServiceName(_appForm.SelectedInstanceName));

			try
			{
				using(StatusForm statusForm = new StatusForm("Stopping...", this))
				{
					// Stop the service
					Cursor.Current = Cursors.WaitCursor;
					_appForm._timer.Stop();
					serviceController.Stop();
					// Wait for the service to start...give it 30 seconds
					serviceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
					_appForm.CheckServiceStatus();
				}
			}
			catch (Exception AException)
			{
				Application.OnThreadException(AException);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
				_appForm._timer.Start();
			}		
		}
		
		private void LoadInstances(InstanceConfiguration configuration)
		{
			cbInstance.Items.Clear();
			foreach (ServerConfiguration instance in configuration.Instances.Values)
				cbInstance.Items.Add(instance.Name);
		}

		private void NewInstanceButton_Click(object sender, EventArgs e)
		{
			InstanceConfiguration configuration = InstanceManager.LoadConfiguration();
			try
			{
				ServerConfiguration instance = EditInstanceForm.ExecuteAdd();
				configuration.Instances.Add(instance);
				InstanceManager.SaveConfiguration(configuration);
				LoadInstances(configuration);
				cbInstance.SelectedItem = instance.Name;
			}
			catch (AbortException)
			{
			}
		}

		private void configureButton_Click(object sender, System.EventArgs args)
		{
			InstanceConfiguration configuration = InstanceManager.LoadConfiguration();
			ServerConfiguration instance = configuration.Instances[cbInstance.Text];
			if (instance == null)
				instance = ServerConfiguration.DefaultInstance(cbInstance.Text);
			else
				configuration.Instances.Remove(instance.Name);
			try
			{
				instance = EditInstanceForm.ExecuteEdit(instance);
				configuration.Instances.Add(instance);
				InstanceManager.SaveConfiguration(configuration);
				LoadInstances(configuration);
				cbInstance.SelectedItem = instance.Name;
			}
			catch (AbortException)
			{
			}
		}

		private void cbInstance_SelectedIndexChanged(object sender, EventArgs e)
		{
			_appForm.SelectedInstanceName = cbInstance.SelectedItem as String;
			_appForm.Serialize();
		}

		private void InstallButton_Click(object sender, EventArgs e)
		{
			switch (_appForm._serviceStatus)
			{
				case DAE.Service.ServiceStatus.Running:
					MessageBox.Show("Service must be stopped before it can be uninstalled.", "Service Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				break;
					
				case DAE.Service.ServiceStatus.Stopped:
					if (MessageBox.Show("Are you sure you want to uninstall the service?", "Uninstall Service?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
					{
						Cursor.Current = Cursors.WaitCursor;
						try
						{
							_appForm._timer.Stop();
							ServiceUtility.Uninstall(_appForm.SelectedInstanceName);
							_appForm.CheckServiceStatus();
						}
						catch (Exception exception)
						{
							Application.OnThreadException(exception);
						}
						finally
						{
							Cursor.Current = Cursors.Default;
							_appForm._timer.Start();
						}
					}
				break;
					
				case DAE.Service.ServiceStatus.Unavailable:
					if (MessageBox.Show("Are you sure you want to install the service?", "Install Service?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
					{
						Cursor.Current = Cursors.WaitCursor;
						try
						{
							_appForm._timer.Stop();
							ServiceUtility.Install(_appForm.SelectedInstanceName);
							_appForm.CheckServiceStatus();
						}
						catch (Exception exception)
						{
							Application.OnThreadException(exception);
						}
						finally
						{
							Cursor.Current = Cursors.Default;
							_appForm._timer.Start();
						}
					}
				break;
				
				case DAE.Service.ServiceStatus.Unassigned:
					MessageBox.Show("There is no currently selected instance to install. Please create and configure a new instance.");
				break;
			}
		}
	}
}