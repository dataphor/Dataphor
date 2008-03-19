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
		private System.ComponentModel.IContainer components;

		public DialogForm() : base()
		{
			InitializeComponent();

		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DialogForm));
			this.SuspendLayout();
			// 
			// DialogForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 267);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.ResumeLayout(false);

		}
		#endregion

		protected override bool ProcessDialogKey(Keys AKeyData)
		{
			switch (AKeyData)
			{
				case Keys.Enter : DialogResult = DialogResult.OK; break;
				case Keys.Escape : DialogResult = DialogResult.Cancel; break;
				default : return base.ProcessDialogKey(AKeyData);
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
