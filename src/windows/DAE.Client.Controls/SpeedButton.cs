/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	using System.ComponentModel;

	[ToolboxItem(false)]
	public class SpeedButton : BitmapButton
	{
		public SpeedButton() : base()
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
		}

		protected override bool ProcessMnemonic(char ACharCode)
		{
			if (Button.IsMnemonic(ACharCode, this.Text))
				OnClick(EventArgs.Empty);
			return base.ProcessMnemonic(ACharCode);
		}

	}
}
