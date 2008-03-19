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

namespace ControlsSample
{
	/// <summary>
	/// Summary description for PubInfoEdit.
	/// </summary>
	public class PubInfoEdit : System.Windows.Forms.Form
	{
		public Alphora.Dataphor.DAE.Client.DataSource dataSource1;
		private Alphora.Dataphor.DAE.Client.Controls.DBTextBox dbTextBox1;
		private Alphora.Dataphor.DAE.Client.Controls.DBTextBox dbTextBox2;
		private Alphora.Dataphor.DAE.Client.Controls.DBImageAspect dbImageAspect1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.ComponentModel.IContainer components;

		public PubInfoEdit()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			this.components = new System.ComponentModel.Container();
			this.dataSource1 = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this.dbTextBox1 = new Alphora.Dataphor.DAE.Client.Controls.DBTextBox();
			this.dbTextBox2 = new Alphora.Dataphor.DAE.Client.Controls.DBTextBox();
			this.dbImageAspect1 = new Alphora.Dataphor.DAE.Client.Controls.DBImageAspect();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// dbTextBox1
			// 
			this.dbTextBox1.BackColor = System.Drawing.SystemColors.Window;
			this.dbTextBox1.ColumnName = "pub_id";
			this.dbTextBox1.Location = new System.Drawing.Point(16, 32);
			this.dbTextBox1.Name = "dbTextBox1";
			this.dbTextBox1.NoValueBackColor = System.Drawing.Color.FromArgb(((System.Byte)(240)), ((System.Byte)(255)), ((System.Byte)(240)));
			this.dbTextBox1.NoValueReadOnlyBackColor = System.Drawing.Color.FromArgb(((System.Byte)(200)), ((System.Byte)(220)), ((System.Byte)(200)));
			this.dbTextBox1.Size = new System.Drawing.Size(392, 20);
			this.dbTextBox1.Source = this.dataSource1;
			this.dbTextBox1.TabIndex = 0;
			// 
			// dbTextBox2
			// 
			this.dbTextBox2.BackColor = System.Drawing.SystemColors.Window;
			this.dbTextBox2.ColumnName = "pr_info";
			this.dbTextBox2.Location = new System.Drawing.Point(16, 72);
			this.dbTextBox2.Multiline = true;
			this.dbTextBox2.Name = "dbTextBox2";
			this.dbTextBox2.NoValueBackColor = System.Drawing.Color.FromArgb(((System.Byte)(240)), ((System.Byte)(255)), ((System.Byte)(240)));
			this.dbTextBox2.NoValueReadOnlyBackColor = System.Drawing.Color.FromArgb(((System.Byte)(200)), ((System.Byte)(220)), ((System.Byte)(200)));
			this.dbTextBox2.Size = new System.Drawing.Size(392, 248);
			this.dbTextBox2.Source = this.dataSource1;
			this.dbTextBox2.TabIndex = 1;
			// 
			// dbImageAspect1
			// 
			this.dbImageAspect1.BackColor = System.Drawing.SystemColors.Control;
			this.dbImageAspect1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.dbImageAspect1.ColumnName = "logo";
			this.dbImageAspect1.FocusedBorderColor = System.Drawing.SystemColors.WindowFrame;
			this.dbImageAspect1.Location = new System.Drawing.Point(16, 336);
			this.dbImageAspect1.Name = "dbImageAspect1";
			this.dbImageAspect1.NoValueBackColor = System.Drawing.Color.FromArgb(((System.Byte)(240)), ((System.Byte)(255)), ((System.Byte)(240)));
			this.dbImageAspect1.ReadOnly = true;
			this.dbImageAspect1.Size = new System.Drawing.Size(392, 200);
			this.dbImageAspect1.Source = this.dataSource1;
			this.dbImageAspect1.TabIndex = 2;
			this.dbImageAspect1.Text = "dbImageAspect1";
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button1.Location = new System.Drawing.Point(80, 568);
			this.button1.Name = "button1";
			this.button1.TabIndex = 3;
			this.button1.Text = "button1";
			// 
			// button2
			// 
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button2.Location = new System.Drawing.Point(200, 568);
			this.button2.Name = "button2";
			this.button2.TabIndex = 4;
			this.button2.Text = "button2";
			// 
			// PubInfoEdit
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.button2;
			this.ClientSize = new System.Drawing.Size(440, 614);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.button2,
																		  this.button1,
																		  this.dbImageAspect1,
																		  this.dbTextBox2,
																		  this.dbTextBox1});
			this.Name = "PubInfoEdit";
			this.Text = "PubInfoEdit";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
