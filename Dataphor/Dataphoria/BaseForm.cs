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

        private System.Windows.Forms.StatusStrip FFormStatus;
        
        public BaseForm() : base()
		{
			AutoScaleMode = AutoScaleMode.None;
		    InitializeComponent();
			InitializeStatusBar();
			FStatusHighlightTimer = new Timer();
			FStatusHighlightTimer.Interval = 1000;
			FStatusHighlightTimer.Tick += new EventHandler(StatusHighlightTimerTick);
		}

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="ADisposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				DisposeStatusBar();
				FStatusHighlightTimer.Dispose();
				FStatusHighlightTimer = null;
				base.Dispose(ADisposing);
			}
		}

		#region StatusBar
		
		private Timer FStatusHighlightTimer;

		public void SetTooltip(string ATipText)
		{			    
            FToolTip.SetToolTip(FFormStatus, ATipText);
		}

		public void SetStatus(string AStatus)
		{
			if (FFormStatus.Text != AStatus)
			{
				FFormStatus.Text = AStatus;
				if (AStatus != String.Empty)
				{
					FFormStatus.BackColor = System.Drawing.SystemColors.Highlight;
					FFormStatus.ForeColor = System.Drawing.SystemColors.HighlightText;
					FStatusHighlightTimer.Start();
				}
			}
		}

		protected virtual void InitializeStatusBar() 
		{

            this.FFormStatus = new StatusStrip 
            {
                Name = "FFormStatus",
                Dock = DockStyle.Bottom
            };
            // 
            // FFormStatus
            // 

		    /*FFormStatus = new StatusBarAdvPanel();
			FFormStatus.HAlign = HorzFlowAlign.Justify;
			FFormStatus.Alignment = HorizontalAlignment.Left;
			FFormStatus.BorderStyle = BorderStyle.None;*/
		}

		protected virtual void DisposeStatusBar() 
		{ 
			FFormStatus.Dispose();
			FFormStatus = null;
		}

		public virtual void Merge(StatusStrip AStatusBar) 
		{
            ToolStripManager.Merge(FFormStatus, AStatusBar);
            
            
            //AStatusBar.Controls.Add(FFormStatus);
		}

		public virtual void Unmerge(StatusStrip AStatusBar) 
		{
		    ToolStripManager.RevertMerge(AStatusBar, FFormStatus);
            
            //AStatusBar.Controls.Remove(FFormStatus);
		}

		private void StatusHighlightTimerTick(object ASender, EventArgs AArgs)
		{
			if (FFormStatus != null)
			{
				FFormStatus.BackColor = System.Drawing.Color.Transparent;
				FFormStatus.ForeColor = this.ForeColor;
			}
			FStatusHighlightTimer.Stop();
		}

		#endregion

		protected override void OnClosed(EventArgs AArgs)
		{
			if (MdiParent != null)
				MdiParent.Focus();
			base.OnClosed(AArgs);
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			// do nothing
		}

		protected override void OnPaintBackground(PaintEventArgs AArgs)
		{
			if (DesignMode)
				base.OnPaintBackground(AArgs);
			// else do nothing
		}

		#region Key Handling

		protected override bool ProcessDialogKey(Keys AKeyData)
		{
			if (((AKeyData & Keys.KeyCode) == Keys.F4) && ((AKeyData & Keys.Modifiers) == Keys.Control))
			{
				Close();
				return true;
			}
			else
				return base.ProcessDialogKey(AKeyData);
		}

		protected override bool ProcessCmdKey(ref Message AMessage, Keys AKeyData)
		{
			// HACK: This works around the issue where keys are not treated as handled when an exception is thrown
			bool LResult;
			try
			{
				LResult = base.ProcessCmdKey(ref AMessage, AKeyData);
			}
			catch (Exception LException)
			{
				Program.HandleException(LException);
				return true;
			}
			return LResult;
		}

		protected override bool ProcessMnemonic(char AKey)
		{
			try
			{
				if (CanSelect && ContainsFocus)
					return base.ProcessMnemonic(AKey);
				else
					return false;
			}
			catch (Exception LException)
			{
                Program.HandleException(LException);
				return true;
			}
		}

		#endregion
	}
}
