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

        protected System.Windows.Forms.StatusStrip FStatusStrip;
        
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
            FToolTip.SetToolTip(FStatusStrip, ATipText);
		}

		public void SetStatus(string AStatus)
		{
			if (FStatusStrip.Text != AStatus)
			{
				FStatusStrip.Text = AStatus;
				if (AStatus != String.Empty)
				{
					FStatusStrip.BackColor = System.Drawing.SystemColors.Highlight;
					FStatusStrip.ForeColor = System.Drawing.SystemColors.HighlightText;
					FStatusHighlightTimer.Start();
				}
			}
		}

		protected virtual void InitializeStatusBar() 
		{

            this.FStatusStrip = new StatusStrip 
            {
                Name = "FStatusStrip",
                Dock = DockStyle.Bottom
            };
            // 
            // FStatusStrip
            // 

		    /*FStatusStrip = new StatusBarAdvPanel();
			FStatusStrip.HAlign = HorzFlowAlign.Justify;
			FStatusStrip.Alignment = HorizontalAlignment.Left;
			FStatusStrip.BorderStyle = BorderStyle.None;*/
		}

		protected virtual void DisposeStatusBar() 
		{ 
			FStatusStrip.Dispose();
			FStatusStrip = null;
		}

		public virtual void MergeStatusBarWith(StatusStrip AStatusBar) 
		{
            ToolStripManager.Merge(FStatusStrip, AStatusBar);
            
            
            //AStatusBar.Controls.Add(FStatusStrip);
		}
		

		private void StatusHighlightTimerTick(object ASender, EventArgs AArgs)
		{
			if (FStatusStrip != null)
			{
				FStatusStrip.BackColor = System.Drawing.Color.Transparent;
				FStatusStrip.ForeColor = this.ForeColor;
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
