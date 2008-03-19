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

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	/// <summary> Shown while processing is taking place. </summary>
	public class StatusForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label FLabel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public StatusForm(string AMessage, Form AParent)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			Size = new Size(450, 60);
			int LXPos = (AParent.Size.Width / 2) + AParent.DesktopLocation.X - (Size.Width / 2);
			int LYPos = (AParent.Size.Height / 2) + AParent.DesktopLocation.Y - (Size.Height / 2);
			DesktopLocation = new Point(LXPos, LYPos);

			if (AMessage != String.Empty)
				FLabel.Text = AMessage;
			Show();
			Update();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(StatusForm));
			this.panel1 = new System.Windows.Forms.Panel();
			this.FLabel = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.AddRange(new System.Windows.Forms.Control[] {
																				 this.FLabel});
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(450, 60);
			this.panel1.TabIndex = 0;
			// 
			// FLabel
			// 
			this.FLabel.Image = ((System.Drawing.Bitmap)(resources.GetObject("FLabel.Image")));
			this.FLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.FLabel.Location = new System.Drawing.Point(8, 16);
			this.FLabel.Name = "FLabel";
			this.FLabel.Size = new System.Drawing.Size(432, 32);
			this.FLabel.TabIndex = 3;
			this.FLabel.Text = "Processing...";
			this.FLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// StatusForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(450, 60);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.panel1});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "StatusForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "StatusForm";
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public string Label
		{
			get { return FLabel.Text; }
			set { FLabel.Text = value; }
		}
	}
}
