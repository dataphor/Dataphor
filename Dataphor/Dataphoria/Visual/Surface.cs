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

		private Control _rememberedActive;

		public void RememberActive()
		{
			_rememberedActive = ActiveControl;
			_rememberedActive.Disposed += new EventHandler(RememberedActiveDisposed);
		}

		public void RecallActive()
		{
			if (_rememberedActive != null)
				_rememberedActive.Disposed -= new EventHandler(RememberedActiveDisposed);
			ActiveControl = _rememberedActive;
		}

		private void RememberedActiveDisposed(object sender, EventArgs args)
		{
			_rememberedActive.Disposed -= new EventHandler(RememberedActiveDisposed);
			_rememberedActive = null;
		}
	}
}
