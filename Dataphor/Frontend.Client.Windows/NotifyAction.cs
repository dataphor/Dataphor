/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class NotifyAction : Action, INotifyAction
	{
		private string _tipTitle;
		[DefaultValue("")]
		[Description("The title to display.")]
		public string TipTitle
		{
			get { return _tipTitle; }
			set { _tipTitle = value; }
		}

		private string _tipText;
		[DefaultValue("")]
		[Description("The text to display.")]
		public string TipText
		{
			get { return _tipText; }
			set { _tipText = value; }
		} 

		private NotifyIcon _tipIcon;
		[DefaultValue(NotifyIcon.Info)]
		[Description("The icon to display.")]
		public NotifyIcon TipIcon
		{
			get { return _tipIcon; }
			set { _tipIcon = value; }
		}

		public const int DefaultTimeout = 10000000;
		public const int TimeoutRatio = 50000;
		private int GetTimeout()
		{
			if (String.IsNullOrEmpty(_tipText))
				return DefaultTimeout;
			return TimeoutRatio * _tipText.Length;
		}

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			((Session)HostNode.Session).NotifyIcon.ShowBalloonTip(GetTimeout(), _tipTitle, _tipText, (System.Windows.Forms.ToolTipIcon)_tipIcon);
		}
	}
}
