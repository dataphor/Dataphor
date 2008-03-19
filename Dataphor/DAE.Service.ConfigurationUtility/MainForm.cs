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
		public const string CServiceStartModeRegName = "Start";
		public const string CServiceAutoStartRegValueName = "Start";
		public const string CAppAutoStartRegKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
		public const string CAppAutoStartRegValueName = "Dataphor Service Manager";
		public const int CServiceAutoStart = 2;
		public const int CServiceManualStart = 3;

		public string FServiceAutoStartRegKeyName = String.Format(@"System\CurrentControlSet\Services\{0}", ServerService.GetServiceName());

		public System.Windows.Forms.CheckBox ServiceAutoStart;
		public System.Windows.Forms.PictureBox ServerStatusPicture;
		public System.Windows.Forms.Button StartStopButton;
		private System.Windows.Forms.GroupBox ServiceStatus;
		private System.Windows.Forms.Button configureButton;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem ShowTrayIcon;
		private System.Windows.Forms.MenuItem AppAutoStart;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
			this.ServiceStatus = new System.Windows.Forms.GroupBox();
			this.StartStopButton = new System.Windows.Forms.Button();
			this.configureButton = new System.Windows.Forms.Button();
			this.ServiceAutoStart = new System.Windows.Forms.CheckBox();
			this.ServerStatusPicture = new System.Windows.Forms.PictureBox();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.ShowTrayIcon = new System.Windows.Forms.MenuItem();
			this.AppAutoStart = new System.Windows.Forms.MenuItem();
			this.ServiceStatus.SuspendLayout();
			this.SuspendLayout();
			// 
			// ServiceStatus
			// 
			this.ServiceStatus.Controls.Add(this.StartStopButton);
			this.ServiceStatus.Controls.Add(this.configureButton);
			this.ServiceStatus.Controls.Add(this.ServiceAutoStart);
			this.ServiceStatus.Controls.Add(this.ServerStatusPicture);
			this.ServiceStatus.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.ServiceStatus.Location = new System.Drawing.Point(8, 8);
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
			this.configureButton.Size = new System.Drawing.Size(207, 28);
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
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(244, 139);
			this.Controls.Add(this.ServiceStatus);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Menu = this.mainMenu1;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Dataphor Status";
			this.ServiceStatus.ResumeLayout(false);
			this.ResumeLayout(false);

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

				using(Microsoft.Win32.RegistryKey LRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(CAppAutoStartRegKeyName, true))
				{
					if (AppAutoStart.Checked)
						LRegKey.SetValue(CAppAutoStartRegValueName, Application.ExecutablePath + " " + ApplicationForm.CSilentMode);
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
				Microsoft.Win32.RegistryKey LRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(FServiceAutoStartRegKeyName, true);
				if (ServiceAutoStart.Checked)
					LRegKey.SetValue(CServiceAutoStartRegValueName, CServiceAutoStart);
				else
					LRegKey.SetValue(CServiceAutoStartRegValueName, CServiceManualStart);
				LRegKey.Close();
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		private void configureButton_Click(object sender, System.EventArgs AArgs)
		{
			ConfigForm LConfigForm = new ConfigForm();
			ServerService LServerService = new ServerService();

			string LConfigFileName = ServerService.GetServiceConfigFileName();

			try
			{
				LServerService.CatalogDirectory = DAE.Server.Server.GetDefaultCatalogDirectory();

				// Deserialize the server service class
				if (System.IO.File.Exists(LConfigFileName))
				{
					using (System.IO.FileStream LStream = new System.IO.FileStream(LConfigFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					{
						new BOP.Deserializer().Deserialize(LStream, LServerService);
					}
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}

			LConfigForm.Port = LServerService.PortNumber;
			LConfigForm.CatalogDirectory = LServerService.CatalogDirectory;
			LConfigForm.CatalogStoreDatabaseName = LServerService.CatalogStoreDatabaseName;
			LConfigForm.CatalogStorePassword = LServerService.CatalogStorePassword;
			LConfigForm.LibraryDirectory = LServerService.LibraryDirectory;
			LConfigForm.StartupScriptFile = LServerService.StartupScriptUri;
			LConfigForm.TracingEnabled = LServerService.TracingEnabled;
			LConfigForm.LogErrors = LServerService.LogErrors;

			if (LConfigForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				LServerService.PortNumber = LConfigForm.Port;
				LServerService.CatalogDirectory = LConfigForm.CatalogDirectory;
				LServerService.CatalogStoreDatabaseName = LConfigForm.CatalogStoreDatabaseName;
				LServerService.CatalogStorePassword = LConfigForm.CatalogStorePassword;
				LServerService.LibraryDirectory = LConfigForm.LibraryDirectory;
				LServerService.StartupScriptUri = LConfigForm.StartupScriptFile;
				LServerService.TracingEnabled = LConfigForm.TracingEnabled;
				LServerService.LogErrors = LConfigForm.LogErrors;
					
				try
				{
					using (System.IO.FileStream LStream = new System.IO.FileStream(LConfigFileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
					{
						new BOP.Serializer().Serialize(LStream, LServerService);
					}
				}
				catch(Exception e)
				{
					MessageBox.Show(e.Message);
				}

				if (FAppForm.FDAEServiceStatus == ApplicationForm.DAEServiceStatus.Running)
					MessageBox.Show(CRestartToApply);
			}
		}

		private void StartStopButton_Click(object sender, System.EventArgs e)
		{
			if (FAppForm.FDAEServiceStatus == ApplicationForm.DAEServiceStatus.Stopped)
				StartService();
			else
				StopService();
		}

		private void StartService()
		{
			if ((FAppForm.FDAEServiceStatus == ApplicationForm.DAEServiceStatus.Running) || (FAppForm.FDAEServiceStatus == ApplicationForm.DAEServiceStatus.Unavailable))
				return;

			System.ServiceProcess.ServiceController LServiceController = new System.ServiceProcess.ServiceController(ServerService.GetServiceName());

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
					FAppForm.SetImageStatus(ApplicationForm.ImageStatus.Running);
					StartStopButton.Text = "Stop";
					FAppForm.FDAEServiceStatus = ApplicationForm.DAEServiceStatus.Running;
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
			if ((FAppForm.FDAEServiceStatus == ApplicationForm.DAEServiceStatus.Stopped) || (FAppForm.FDAEServiceStatus == ApplicationForm.DAEServiceStatus.Unavailable))
				return;

			System.ServiceProcess.ServiceController LServiceController = new System.ServiceProcess.ServiceController(ServerService.GetServiceName());

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
					FAppForm.SetImageStatus(ApplicationForm.ImageStatus.Stopped);
					StartStopButton.Text = "Start";
					FAppForm.FDAEServiceStatus = ApplicationForm.DAEServiceStatus.Stopped;
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
	}
}