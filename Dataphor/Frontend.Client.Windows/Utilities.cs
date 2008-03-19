/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public sealed class FolderUtility
	{
		/// <summary> Prompts the user for a directory. </summary>
		public static string GetDirectory(string ADefault)
		{
			FolderBrowserDialog LBrowser = new FolderBrowserDialog();
			if ((ADefault == String.Empty) || !System.IO.Directory.Exists(ADefault))
				LBrowser.RootFolder = System.Environment.SpecialFolder.Desktop;
			else
				LBrowser.SelectedPath = ADefault;
			if (LBrowser.ShowDialog() != DialogResult.OK)
				throw new AbortException();
			return LBrowser.SelectedPath;
		}
	}

	public sealed class ControlUtility
	{
		public static Control InnermostActiveControl(ContainerControl AControl)
		{
			ContainerControl LControl = AControl;
            while ((LControl.ActiveControl != null) && (LControl.ActiveControl is ContainerControl))
                  LControl = (ContainerControl)LControl.ActiveControl;
			if (LControl.ActiveControl != null)
				return LControl.ActiveControl;
			else
				return LControl;
		}
	}
}