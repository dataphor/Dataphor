using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	public class ConfigurationUtilitySettings : System.Object 
	{
		public ConfigurationUtilitySettings()
		{
			FSelectedInstanceName = Server.Engine.CDefaultServerName;
			FShowTrayIcon = true;
			FAppAutoStart = false;
		}
		
		private string FSelectedInstanceName;
		public string SelectedInstanceName
		{
			get { return FSelectedInstanceName; }
			set { FSelectedInstanceName = value; }
		}

		private bool FShowTrayIcon;
		public bool ShowTrayIcon
		{
			get	{ return FShowTrayIcon; }
			set { FShowTrayIcon = value; }
		}

		private bool FAppAutoStart;
		public bool AppAutoStart
		{
			get { return FAppAutoStart; }
			set { FAppAutoStart = value; }
		}
	}
}
