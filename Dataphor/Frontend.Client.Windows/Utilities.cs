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
		public static string GetDirectory(string defaultValue)
		{
			FolderBrowserDialog browser = new FolderBrowserDialog();
			if ((defaultValue == String.Empty) || !System.IO.Directory.Exists(defaultValue))
				browser.RootFolder = System.Environment.SpecialFolder.Desktop;
			else
				browser.SelectedPath = defaultValue;
			if (browser.ShowDialog() != DialogResult.OK)
				throw new AbortException();
			return browser.SelectedPath;
		}
	}

	public sealed class ControlUtility
	{
		public static Control InnermostActiveControl(ContainerControl control)
		{
			ContainerControl localControl = control;
            while ((localControl.ActiveControl != null) && (localControl.ActiveControl is ContainerControl))
                  localControl = (ContainerControl)localControl.ActiveControl;
			if (localControl.ActiveControl != null)
				return localControl.ActiveControl;
			else
				return localControl;
		}
	}
}