/*
	Alphora Dataphor
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
	// Don't put any definitions above the EditFilterForm class

	/// <summary> UI for editing the DataView Filter. </summary>
	public class EditFilterForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox filterTextBox;
		/// <summary> Required designer variable. </summary>
		private System.ComponentModel.Container components = null;

		public EditFilterForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary> Clean up any resources being used. </summary>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				if (components != null)
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditFilterForm));
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.filterTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(264, 112);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "&OK";
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(344, 112);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "&Cancel";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "Filter Expression";
            // 
            // filterTextBox
            // 
            this.filterTextBox.AcceptsTab = true;
            this.filterTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.filterTextBox.Location = new System.Drawing.Point(8, 24);
            this.filterTextBox.Multiline = true;
            this.filterTextBox.Name = "filterTextBox";
            this.filterTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.filterTextBox.Size = new System.Drawing.Size(408, 80);
            this.filterTextBox.TabIndex = 0;
            // 
            // EditFilterForm
            // 
            this.AcceptButton = this.okButton;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(424, 142);
            this.Controls.Add(this.filterTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EditFilterForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Filter";
            this.ResumeLayout(false);

		}
		#endregion

		public static string ExecuteEditFilter(string filter)
		{
			using (EditFilterForm form = new EditFilterForm())
			{
				form.filterTextBox.Text = filter;
				if (form.ShowDialog() == DialogResult.OK)
					return form.filterTextBox.Text;
				else
					throw new AbortException();
			}
		}
	}
}
