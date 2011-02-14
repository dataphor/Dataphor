using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	public class ConfigurationUtilitySettings : System.Object 
	{
		public ConfigurationUtilitySettings()
		{
			_selectedInstanceName = Server.Engine.DefaultServerName;
			_showTrayIcon = true;
			_appAutoStart = false;
		}
		
		private string _selectedInstanceName;
		public string SelectedInstanceName
		{
			get { return _selectedInstanceName; }
			set { _selectedInstanceName = value; }
		}

		private bool _showTrayIcon;
		public bool ShowTrayIcon
		{
			get	{ return _showTrayIcon; }
			set { _showTrayIcon = value; }
		}

		private bool _appAutoStart;
		public bool AppAutoStart
		{
			get { return _appAutoStart; }
			set { _appAutoStart = value; }
		}
	}
}
