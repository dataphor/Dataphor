/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class StaticButton : Button
	{
		public StaticButton() : base()
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
		}
	}
}
