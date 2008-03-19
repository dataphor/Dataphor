/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public abstract class Surface : ContainerControl
	{
		public Surface()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SuspendLayout();
			try
			{
				AutoScroll = false;
				BackColor = Color.FromArgb(148, 148, 255);
				Dock = System.Windows.Forms.DockStyle.Fill;
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		// Focus

		private Control FRememberedActive;

		public void RememberActive()
		{
			FRememberedActive = ActiveControl;
			FRememberedActive.Disposed += new EventHandler(RememberedActiveDisposed);
		}

		public void RecallActive()
		{
			if (FRememberedActive != null)
				FRememberedActive.Disposed -= new EventHandler(RememberedActiveDisposed);
			ActiveControl = FRememberedActive;
		}

		private void RememberedActiveDisposed(object ASender, EventArgs AArgs)
		{
			FRememberedActive.Disposed -= new EventHandler(RememberedActiveDisposed);
			FRememberedActive = null;
		}
	}
}
