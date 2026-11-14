/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria
{
	/// <summary> Base form for all Dataphoria child forms. </summary>
	/// <remarks> This form does not paint it's background (ancestors must completely cover the background). </remarks>
    public partial class BaseForm : DockContent, IStatusBarClient
	{

        public BaseForm() : base()
		{
			AutoScaleMode = AutoScaleMode.None;
		    InitializeComponent();
			InitializeStatusBar();
		}

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				DisposeStatusBar();
				FStatusHighlightTimer.Dispose();
				FStatusHighlightTimer = null;
				base.Dispose(disposing);
			}
		}

		#region StatusBar
		
		protected virtual void InitializeStatusBar()
		{
		}

		protected virtual void DisposeStatusBar()
		{
		}

		public void SetTooltip(string tipText)
		{			    
            FToolTip.SetToolTip(FStatusStrip, tipText);
		}

		public void SetStatus(string status)
		{
			if (FStatusLabel.Text != status)
			{
				FStatusLabel.Text = status;
				if (status != String.Empty)
				{
					FStatusLabel.BackColor = System.Drawing.SystemColors.Highlight;
					FStatusLabel.ForeColor = System.Drawing.SystemColors.HighlightText;
					FStatusHighlightTimer.Start();
				}
			}
		}

		private void StatusHighlightTimerTick(object sender, EventArgs args)
		{
			if (FStatusLabel != null)
			{
				FStatusLabel.BackColor = System.Drawing.Color.Transparent;
				FStatusLabel.ForeColor = this.ForeColor;
			}
			FStatusHighlightTimer.Stop();
		}

		public virtual void MergeStatusBarWith(StatusStrip statusBar) 
		{
            ToolStripManager.Merge(FStatusStrip, statusBar);
            FStatusStrip.Visible = false;
		}
		
		#endregion

		protected override void OnClosed(EventArgs args)
		{
			if (MdiParent != null)
				MdiParent.Focus();
			base.OnClosed(args);
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			// do nothing
		}

		protected override void OnPaintBackground(PaintEventArgs args)
		{
			if (DesignMode)
				base.OnPaintBackground(args);
			// else do nothing
		}

		#region Key Handling

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (((keyData & Keys.KeyCode) == Keys.F4) && ((keyData & Keys.Modifiers) == Keys.Control))
			{
				Close();
				return true;
			}
			else
				return base.ProcessDialogKey(keyData);
		}

		protected override bool ProcessCmdKey(ref Message message, Keys keyData)
		{
			// HACK: This works around the issue where keys are not treated as handled when an exception is thrown
			bool result;
			try
			{
				result = base.ProcessCmdKey(ref message, keyData);
			}
			catch (Exception exception)
			{
				Program.HandleException(exception);
				return true;
			}
			return result;
		}

		protected override bool ProcessMnemonic(char key)
		{
			try
			{
				if (CanSelect && ContainsFocus)
					return base.ProcessMnemonic(key);
				else
					return false;
			}
			catch (Exception exception)
			{
                Program.HandleException(exception);
				return true;
			}
		}

		#endregion
	}
}
