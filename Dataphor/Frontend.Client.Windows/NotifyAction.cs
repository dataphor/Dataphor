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
		private string FTipTitle;
		[DefaultValue("")]
		[Description("The title to display.")]
		public string TipTitle
		{
			get { return FTipTitle; }
			set { FTipTitle = value; }
		}

		private string FTipText;
		[DefaultValue("")]
		[Description("The text to display.")]
		public string TipText
		{
			get { return FTipText; }
			set { FTipText = value; }
		} 

		private NotifyIcon FTipIcon;
		[DefaultValue(NotifyIcon.Info)]
		[Description("The icon to display.")]
		public NotifyIcon TipIcon
		{
			get { return FTipIcon; }
			set { FTipIcon = value; }
		}

		public const int CDefaultTimeout = 10000000;
		public const int CTimeoutRatio = 50000;
		private int GetTimeout()
		{
			if (String.IsNullOrEmpty(FTipText))
				return CDefaultTimeout;
			return CTimeoutRatio * FTipText.Length;
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			((Session)HostNode.Session).NotifyIcon.ShowBalloonTip(GetTimeout(), FTipTitle, FTipText, (System.Windows.Forms.ToolTipIcon)FTipIcon);
		}
	}
}
