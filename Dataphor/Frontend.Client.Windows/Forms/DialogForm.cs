/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class DialogForm : BaseForm
	{
		public DialogForm() : base()
		{
			InitializeComponent();
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
            this.SuspendLayout();
            // 
            // FContentPanel
            // 
            this.FContentPanel.AutoScroll = true;
            this.FContentPanel.BackColor = System.Drawing.SystemColors.Control;
            this.FContentPanel.Size = new System.Drawing.Size(292, 196);
            // 
            // DialogForm
            // 
            this.ClientSize = new System.Drawing.Size(292, 267);
            this.Location = new System.Drawing.Point(0, 0);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogForm";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		protected override bool ProcessDialogKey(Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Enter : DialogResult = DialogResult.OK; break;
				case Keys.Escape : DialogResult = DialogResult.Cancel; break;
				default : return base.ProcessDialogKey(keyData);
			}
			return true;
		}

		private void AcceptClicked(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void RejectClicked(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}
	}
}
