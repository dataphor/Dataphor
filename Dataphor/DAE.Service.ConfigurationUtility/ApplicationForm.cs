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
using System.Threading;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	public class ApplicationForm : System.Windows.Forms.Form
	{
		// Do not localize
		public const string TrayDisplayText = "Dataphor Configuration Utility";		
		private const string ProcessName = "DAE.Service.ConfigurationUtility";
		private const string RunningBMPName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Running.gif";
		private const string StoppedBMPName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Stopped.gif";
		private const string RunningIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Running.ico";
		private const string StoppedIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Stopped.ico";
		private const string UnavailableBMPName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Unavailable.gif";
		private const string UnavailableIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Unavailable.ico";
		private const string ConfigurationUtilitySettingsFileName = @"DAEConfigurationUtility.config";
		public bool _mainFormIsShowing = false;
		private const int ServicePollingInterval = 5000;

		public ConfigurationUtilitySettings _configurationUtilitySettings = new ConfigurationUtilitySettings();
		public ServiceStatus _serviceStatus = ServiceStatus.Unassigned;
		public NotifyIcon _trayIcon = new NotifyIcon();
		private ContextMenu _menu = new ContextMenu();
		private MainForm _mainForm;
		
		public string SelectedInstanceName
		{
			get { return _configurationUtilitySettings.SelectedInstanceName; }
			set
			{
				_configurationUtilitySettings.SelectedInstanceName = value;
				Timer_Tick(this, null);
			}
		}

		public enum ImageStatus { Running, Stopped, Unavailable };

		public System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();

		private System.ComponentModel.Container components = null;

		public ApplicationForm(bool silentModeSetting)
		{
			try
			{
				// Deserialize the app settings
				string fileName = PathUtility.CommonAppDataPath() + ConfigurationUtilitySettingsFileName;
				if (System.IO.File.Exists(fileName))
				{
					using (System.IO.FileStream stream = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					{
						new BOP.Deserializer().Deserialize(stream, _configurationUtilitySettings);
					}
				}

				// We need to do this, because if they uninstall and re-install, the box will be checked
				// but the registry will not have a value.
				using(Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(MainForm.AppAutoStartRegKeyName, true))
				{
					if (_configurationUtilitySettings.AppAutoStart == true)
                        regKey.SetValue(MainForm.AppAutoStartRegValueName, Application.ExecutablePath + " " + Program.SilentMode);
					else
						regKey.DeleteValue(MainForm.AppAutoStartRegValueName, false);
				}

				_trayIcon.Text = TrayDisplayText;
				_trayIcon.Visible = _configurationUtilitySettings.ShowTrayIcon;
				_trayIcon.DoubleClick += new System.EventHandler(this.TrayIcon_DoubleClick);
				// Set up the context pop-up menu
				_menu.MenuItems.Add(new MenuItem("Open", new System.EventHandler(this.TrayIcon_DoubleClick)));
				_menu.MenuItems.Add(new MenuItem("Exit", new System.EventHandler(this.TrayIcon_PopupMenu_Exit)));
				_trayIcon.ContextMenu = _menu;

				// Check every n second(s) to see if the service status has been changed by some other program
				_timer.Tick += new System.EventHandler(this.Timer_Tick);
				_timer.Interval = ServicePollingInterval;
				_timer.Start();

				if (silentModeSetting == false)
				{
					ShowMainForm();
				}

				// Run this once so that when the app starts we don't see the controls enabled for the 1/2 second
				// until the timer fires.
				Timer_Tick(this, null);
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}


		private void TrayIcon_DoubleClick(object sender, System.EventArgs args)
		{
			ShowMainForm();
		}

		private void TrayIcon_PopupMenu_Exit(object sender, System.EventArgs args)
		{
			if (_mainFormIsShowing == true)
			{
				Serialize();
				_trayIcon.Visible = false;

				_mainForm.Close();
				_mainFormIsShowing = false;
			}

			_trayIcon.Visible = false;
			Application.Exit();
		}

		private void Timer_Tick(object sender, System.EventArgs args)
		{
			try
			{
				if (_mainFormIsShowing == true)
				{
					_mainForm.ServiceAutoStart.Checked = ServiceUtility.GetServiceAutoStart(SelectedInstanceName);
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}

			CheckServiceStatus();
		}

		public void SetImageStatus(ImageStatus imageStatus)
		{
			string iconName = string.Empty;
			string bMPName = string.Empty;

			switch (imageStatus)
			{
				case ImageStatus.Running:
					iconName = RunningIconName;
					bMPName = RunningBMPName;
					break;
				case ImageStatus.Stopped:
					iconName = StoppedIconName;
					bMPName = StoppedBMPName;
					break;
				case ImageStatus.Unavailable:
					iconName = UnavailableIconName;
					bMPName = UnavailableBMPName;
					break;
			}

			Icon icon = new Icon(this.GetType().Assembly.GetManifestResourceStream(iconName));
			_trayIcon.Icon = icon;

			if (_mainFormIsShowing == true)
			{
				Bitmap bitmap = new Bitmap(this.GetType().Assembly.GetManifestResourceStream(bMPName));
				bitmap.MakeTransparent(System.Drawing.Color.White);
				_mainForm.ServerStatusPicture.Image = bitmap;
			}
		}

		public void Serialize()
		{
			try
			{
				using (System.IO.FileStream stream = new System.IO.FileStream(PathUtility.CommonAppDataPath() + ConfigurationUtilitySettingsFileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
				{
					new BOP.Serializer().Serialize(stream, _configurationUtilitySettings);
				}
			}
			catch (Exception exception)
			{
				Application.OnThreadException(exception);
			}
		}

		public void CheckServiceStatus()
		{
			_serviceStatus = ServiceUtility.GetServiceStatus(SelectedInstanceName);

			switch (_serviceStatus)
			{
				case ServiceStatus.Running:
					SetImageStatus(ApplicationForm.ImageStatus.Running);
					if (_mainFormIsShowing == true)
					{
						_mainForm.StartStopButton.Text = "Stop";
						_mainForm.StartStopButton.Enabled = true;
						_mainForm.InstallButton.Text = "Uninstall";
						_mainForm.InstallButton.Enabled = false;
						_mainForm.ServiceAutoStart.Enabled = true;
					}
				break;

				case ServiceStatus.Stopped:
					SetImageStatus(ApplicationForm.ImageStatus.Stopped);
					if (_mainFormIsShowing == true)
					{
						_mainForm.StartStopButton.Text = "Start";
						_mainForm.StartStopButton.Enabled = true;
						_mainForm.InstallButton.Text = "Uninstall";
						_mainForm.InstallButton.Enabled = true;
						_mainForm.ServiceAutoStart.Enabled = true;
					}
				break;

				case ServiceStatus.Unavailable:
				default:
					SetImageStatus(ApplicationForm.ImageStatus.Unavailable);
					if (_mainFormIsShowing == true)
					{
						_mainForm.StartStopButton.Text = "Not-Available";
						_mainForm.StartStopButton.Enabled = false;
						_mainForm.InstallButton.Text = "Install";
						_mainForm.InstallButton.Enabled = true;
						_mainForm.ServiceAutoStart.Enabled = false;
					}
				break;
			}
		}

		private void ShowMainForm()
		{
			if (!_mainFormIsShowing)
			{
				_mainForm = new MainForm(this);
				_mainFormIsShowing = true;
				Timer_Tick(this, null);
				_mainForm.Show();
				_mainForm.ShowInTaskbar = true;
			}
			else
			{
				_mainForm.WindowState = FormWindowState.Normal;
				_mainForm.BringToFront();
			}
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ApplicationForm));
			// 
			// ApplicationForm
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ApplicationForm";
			this.Text = "ApplicationForm";

		}
		#endregion

		

		
	}
}
