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

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	public class ApplicationForm : System.Windows.Forms.Form
	{
		// Do not localize
		public const string CTrayDisplayText = "Dataphor Configuration Utility";
		public const string CSilentMode = @"/s";
		private const string CProcessName = "DAE.Service.ConfigurationUtility";
		private const string CRunningBMPName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Running.gif";
		private const string CStoppedBMPName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Stopped.gif";
		private const string CRunningIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Running.ico";
		private const string CStoppedIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Stopped.ico";
		private const string CUnavailableBMPName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Unavailable.gif";
		private const string CUnavailableIconName = "Alphora.Dataphor.DAE.Service.ConfigurationUtility.Images.Unavailable.ico";
		private const string CConfigurationUtilitySettingsFileName = @"DAEConfigurationUtility.config";
		public bool FMainFormIsShowing = false;
		private const int CServicePollingInterval = 5000;

		public ConfigurationUtilitySettings FConfigurationUtilitySettings = new ConfigurationUtilitySettings();
		public ServiceStatus FServiceStatus = ServiceStatus.Unassigned;
		public NotifyIcon FTrayIcon = new NotifyIcon();
		private ContextMenu FMenu = new ContextMenu();
		private MainForm FMainForm;
		
		public string SelectedInstanceName
		{
			get { return FConfigurationUtilitySettings.SelectedInstanceName; }
			set
			{
				FConfigurationUtilitySettings.SelectedInstanceName = value;
				Timer_Tick(this, null);
			}
		}

		public enum ImageStatus { Running, Stopped, Unavailable };

		public System.Windows.Forms.Timer FTimer = new System.Windows.Forms.Timer();

		private System.ComponentModel.Container components = null;

		public ApplicationForm(bool ASilentModeSetting)
		{
			try
			{
				// Deserialize the app settings
				string LFileName = PathUtility.CommonAppDataPath() + CConfigurationUtilitySettingsFileName;
				if (System.IO.File.Exists(LFileName))
				{
					using (System.IO.FileStream LStream = new System.IO.FileStream(LFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					{
						new BOP.Deserializer().Deserialize(LStream, FConfigurationUtilitySettings);
					}
				}

				// We need to do this, because if they uninstall and re-install, the box will be checked
				// but the registry will not have a value.
				using(Microsoft.Win32.RegistryKey LRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(MainForm.CAppAutoStartRegKeyName, true))
				{
					if (FConfigurationUtilitySettings.AppAutoStart == true)
						LRegKey.SetValue(MainForm.CAppAutoStartRegValueName, Application.ExecutablePath + " " + ApplicationForm.CSilentMode);
					else
						LRegKey.DeleteValue(MainForm.CAppAutoStartRegValueName, false);
				}

				FTrayIcon.Text = CTrayDisplayText;
				FTrayIcon.Visible = FConfigurationUtilitySettings.ShowTrayIcon;
				FTrayIcon.DoubleClick += new System.EventHandler(this.TrayIcon_DoubleClick);
				// Set up the context pop-up menu
				FMenu.MenuItems.Add(new MenuItem("Open", new System.EventHandler(this.TrayIcon_DoubleClick)));
				FMenu.MenuItems.Add(new MenuItem("Exit", new System.EventHandler(this.TrayIcon_PopupMenu_Exit)));
				FTrayIcon.ContextMenu = FMenu;

				// Check every n second(s) to see if the service status has been changed by some other program
				FTimer.Tick += new System.EventHandler(this.Timer_Tick);
				FTimer.Interval = CServicePollingInterval;
				FTimer.Start();

				if (ASilentModeSetting == false)
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


		private void TrayIcon_DoubleClick(object sender, System.EventArgs AArgs)
		{
			ShowMainForm();
		}

		private void TrayIcon_PopupMenu_Exit(object sender, System.EventArgs AArgs)
		{
			if (FMainFormIsShowing == true)
			{
				Serialize();
				FTrayIcon.Visible = false;

				FMainForm.Close();
				FMainFormIsShowing = false;
			}

			FTrayIcon.Visible = false;
			Application.Exit();
		}

		private void Timer_Tick(object sender, System.EventArgs AArgs)
		{
			try
			{
				if (FMainFormIsShowing == true)
				{
					FMainForm.ServiceAutoStart.Checked = ServiceUtility.GetServiceAutoStart(SelectedInstanceName);
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}

			CheckServiceStatus();
		}

		public void SetImageStatus(ImageStatus AImageStatus)
		{
			string LIconName = string.Empty;
			string LBMPName = string.Empty;

			switch (AImageStatus)
			{
				case ImageStatus.Running:
					LIconName = CRunningIconName;
					LBMPName = CRunningBMPName;
					break;
				case ImageStatus.Stopped:
					LIconName = CStoppedIconName;
					LBMPName = CStoppedBMPName;
					break;
				case ImageStatus.Unavailable:
					LIconName = CUnavailableIconName;
					LBMPName = CUnavailableBMPName;
					break;
			}

			Icon LIcon = new Icon(this.GetType().Assembly.GetManifestResourceStream(LIconName));
			FTrayIcon.Icon = LIcon;

			if (FMainFormIsShowing == true)
			{
				Bitmap LBitmap = new Bitmap(this.GetType().Assembly.GetManifestResourceStream(LBMPName));
				LBitmap.MakeTransparent(System.Drawing.Color.White);
				FMainForm.ServerStatusPicture.Image = LBitmap;
			}
		}

		public void Serialize()
		{
			try
			{
				using (System.IO.FileStream LStream = new System.IO.FileStream(PathUtility.CommonAppDataPath() + CConfigurationUtilitySettingsFileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
				{
					new BOP.Serializer().Serialize(LStream, FConfigurationUtilitySettings);
				}
			}
			catch (Exception LException)
			{
				Application.OnThreadException(LException);
			}
		}

		public void CheckServiceStatus()
		{
			FServiceStatus = ServiceUtility.GetServiceStatus(SelectedInstanceName);

			switch (FServiceStatus)
			{
				case ServiceStatus.Running:
					SetImageStatus(ApplicationForm.ImageStatus.Running);
					if (FMainFormIsShowing == true)
					{
						FMainForm.StartStopButton.Text = "Stop";
						FMainForm.StartStopButton.Enabled = true;
						FMainForm.InstallButton.Text = "Uninstall";
						FMainForm.InstallButton.Enabled = false;
						FMainForm.ServiceAutoStart.Enabled = true;
					}
				break;

				case ServiceStatus.Stopped:
					SetImageStatus(ApplicationForm.ImageStatus.Stopped);
					if (FMainFormIsShowing == true)
					{
						FMainForm.StartStopButton.Text = "Start";
						FMainForm.StartStopButton.Enabled = true;
						FMainForm.InstallButton.Text = "Uninstall";
						FMainForm.InstallButton.Enabled = true;
						FMainForm.ServiceAutoStart.Enabled = true;
					}
				break;

				case ServiceStatus.Unavailable:
				default:
					SetImageStatus(ApplicationForm.ImageStatus.Unavailable);
					if (FMainFormIsShowing == true)
					{
						FMainForm.StartStopButton.Text = "Not-Available";
						FMainForm.StartStopButton.Enabled = false;
						FMainForm.InstallButton.Text = "Install";
						FMainForm.InstallButton.Enabled = true;
						FMainForm.ServiceAutoStart.Enabled = false;
					}
				break;
			}
		}

		private void ShowMainForm()
		{
			if (!FMainFormIsShowing)
			{
				FMainForm = new MainForm(this);
				FMainFormIsShowing = true;
				Timer_Tick(this, null);
				FMainForm.Show();
				FMainForm.ShowInTaskbar = true;
			}
			else
			{
				FMainForm.WindowState = FormWindowState.Normal;
				FMainForm.BringToFront();
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

		[STAThread]
		static void Main(string[] AArgs)
		{
			Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
			System.Diagnostics.Process[] LProcesses;
			// See if this is already running
			LProcesses = System.Diagnostics.Process.GetProcessesByName("DAEConfigUtil");
			// There will be 1 running...this one!  But any more, and we just exit.
			if (LProcesses.Length <= 1)
			{
				ApplicationForm LAppForm;
				if ((AArgs.Length > 0) && (AArgs[0] == CSilentMode))
					LAppForm = new ApplicationForm(true);
				else
					LAppForm = new ApplicationForm(false);
				Application.Run();
			}
		}

		protected static void ThreadException(object ASender, ThreadExceptionEventArgs AArgs)
		{
			MessageBox.Show(AArgs.Exception.ToString());
		}
	}
}
