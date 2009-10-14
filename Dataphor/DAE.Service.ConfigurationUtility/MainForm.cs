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
		public const string CCatalogBrowseTitle = "Catalog Directory";
		public const string CLibraryBrowseTitle = "Library Directory";
		public const string CStartupScriptBrowseTitle = "Open Startup Script";
		public const string CStartupScriptBrowseFilter = "D4 Script Files (*.d4)|*.d4|All Files (*.*)|*.*";
		public const string CRestartToApply = "The new settings will not apply until the service has been re-started.";

		// Do not localize
		public const string CWindowIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.ConfigUtil-big.ico";
		public const string CStartupScriptDefaultExtension = ".d4";
		public const string CAppAutoStartRegKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
		public const string CAppAutoStartRegValueName = "Dataphor Service Manager";

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

		private ApplicationForm FAppForm;

		public MainForm(ApplicationForm LAppForm)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			FAppForm = LAppForm;

			LoadInstances(InstanceManager.LoadConfiguration());
			cbInstance.Text = FAppForm.SelectedInstanceName;
			ShowTrayIcon.Checked = FAppForm.FConfigurationUtilitySettings.ShowTrayIcon;
			AppAutoStart.Checked = FAppForm.FConfigurationUtilitySettings.AppAutoStart;
		}
	
		private void ShowTrayIcon_Click(object sender, System.EventArgs AArgs)
		{
			FAppForm.FConfigurationUtilitySettings.ShowTrayIcon = !FAppForm.FConfigurationUtilitySettings.ShowTrayIcon;
			ShowTrayIcon.Checked = FAppForm.FConfigurationUtilitySettings.ShowTrayIcon;

			FAppForm.Serialize();

			FAppForm.FTrayIcon.Visible = ShowTrayIcon.Checked;
		}

		private void AppAutoStart_Click(object sender, System.EventArgs AArgs)
		{
			try
			{
				FAppForm.FConfigurationUtilitySettings.AppAutoStart = !FAppForm.FConfigurationUtilitySettings.AppAutoStart;
				AppAutoStart.Checked = FAppForm.FConfigurationUtilitySettings.AppAutoStart;

				FAppForm.Serialize();

				using (Microsoft.Win32.RegistryKey LRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(CAppAutoStartRegKeyName, true))
				{
					if (AppAutoStart.Checked)
                        LRegKey.SetValue(CAppAutoStartRegValueName, Application.ExecutablePath + " " + Program.SilentMode);
					else
						LRegKey.DeleteValue(CAppAutoStartRegValueName, false);
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			FAppForm.FMainFormIsShowing = false;

			if (FAppForm.FConfigurationUtilitySettings.ShowTrayIcon == false)
				Application.Exit();

			base.OnClosing(AArgs);
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
		
		private void ServiceAutoStart_Click(object sender, System.EventArgs AArgs)
		{
			try
			{
				ServiceUtility.SetServiceAutoStart(FAppForm.SelectedInstanceName, ServiceAutoStart.Checked);
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		private void StartStopButton_Click(object sender, System.EventArgs e)
		{
			if (FAppForm.FServiceStatus == DAE.Service.ServiceStatus.Stopped)
				StartService();
			else
				StopService();
		}

		private void StartService()
		{
			if ((FAppForm.FServiceStatus == DAE.Service.ServiceStatus.Running) || (FAppForm.FServiceStatus == DAE.Service.ServiceStatus.Unavailable))
				return;

			System.ServiceProcess.ServiceController LServiceController = new System.ServiceProcess.ServiceController(ServiceUtility.GetServiceName(FAppForm.SelectedInstanceName));

			try
			{				
				using(StatusForm LStatusForm = new StatusForm("Starting...", this))
				{
					// Start the service
					Cursor.Current = Cursors.WaitCursor;
					FAppForm.FTimer.Stop();
					LServiceController.Start();
					// Wait for the service to start...give it 30 seconds
					LServiceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
					FAppForm.CheckServiceStatus();
				}
			}
			catch (Exception AException)
			{
				Application.OnThreadException(AException);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
				FAppForm.FTimer.Start();
			}		
		}

		private void StopService()
		{
			if ((FAppForm.FServiceStatus == DAE.Service.ServiceStatus.Stopped) || (FAppForm.FServiceStatus == DAE.Service.ServiceStatus.Unavailable))
				return;

			System.ServiceProcess.ServiceController LServiceController = new System.ServiceProcess.ServiceController(ServiceUtility.GetServiceName(FAppForm.SelectedInstanceName));

			try
			{
				using(StatusForm LStatusForm = new StatusForm("Stopping...", this))
				{
					// Stop the service
					Cursor.Current = Cursors.WaitCursor;
					FAppForm.FTimer.Stop();
					LServiceController.Stop();
					// Wait for the service to start...give it 30 seconds
					LServiceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
					FAppForm.CheckServiceStatus();
				}
			}
			catch (Exception AException)
			{
				Application.OnThreadException(AException);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
				FAppForm.FTimer.Start();
			}		
		}
		
		private void LoadInstances(InstanceConfiguration AConfiguration)
		{
			cbInstance.Items.Clear();
			foreach (ServerConfiguration LInstance in AConfiguration.Instances.Values)
				cbInstance.Items.Add(LInstance.Name);
		}

		private void NewInstanceButton_Click(object sender, EventArgs e)
		{
			InstanceConfiguration LConfiguration = InstanceManager.LoadConfiguration();
			try
			{
				ServerConfiguration LInstance = EditInstanceForm.ExecuteAdd();
				LConfiguration.Instances.Add(LInstance);
				InstanceManager.SaveConfiguration(LConfiguration);
				LoadInstances(LConfiguration);
				cbInstance.SelectedItem = LInstance.Name;
			}
			catch (AbortException)
			{
			}
		}

		private void configureButton_Click(object sender, System.EventArgs AArgs)
		{
			InstanceConfiguration LConfiguration = InstanceManager.LoadConfiguration();
			ServerConfiguration LInstance = LConfiguration.Instances[cbInstance.Text];
			if (LInstance == null)
				LInstance = ServerConfiguration.DefaultInstance(cbInstance.Text);
			else
				LConfiguration.Instances.Remove(LInstance.Name);
			try
			{
				LInstance = EditInstanceForm.ExecuteEdit(LInstance);
				LConfiguration.Instances.Add(LInstance);
				InstanceManager.SaveConfiguration(LConfiguration);
				LoadInstances(LConfiguration);
				cbInstance.SelectedItem = LInstance.Name;
			}
			catch (AbortException)
			{
			}
		}

		private void cbInstance_SelectedIndexChanged(object sender, EventArgs e)
		{
			FAppForm.SelectedInstanceName = cbInstance.SelectedItem as String;
			FAppForm.Serialize();
		}

		private void InstallButton_Click(object sender, EventArgs e)
		{
			switch (FAppForm.FServiceStatus)
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
							FAppForm.FTimer.Stop();
							ServiceUtility.Uninstall(FAppForm.SelectedInstanceName);
							FAppForm.CheckServiceStatus();
						}
						catch (Exception LException)
						{
							Application.OnThreadException(LException);
						}
						finally
						{
							Cursor.Current = Cursors.Default;
							FAppForm.FTimer.Start();
						}
					}
				break;
					
				case DAE.Service.ServiceStatus.Unavailable:
					if (MessageBox.Show("Are you sure you want to install the service?", "Install Service?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
					{
						Cursor.Current = Cursors.WaitCursor;
						try
						{
							FAppForm.FTimer.Stop();
							ServiceUtility.Install(FAppForm.SelectedInstanceName);
							FAppForm.CheckServiceStatus();
						}
						catch (Exception LException)
						{
							Application.OnThreadException(LException);
						}
						finally
						{
							Cursor.Current = Cursors.Default;
							FAppForm.FTimer.Start();
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