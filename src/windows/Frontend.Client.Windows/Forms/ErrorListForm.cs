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
	/// <summary> Displays a list of exceptions contained in an <see cref="ErrorList"/>. </summary>
	public class ErrorListForm : System.Windows.Forms.Form
	{
		private Alphora.Dataphor.Frontend.Client.Windows.ErrorListView _errorListView;

		public ErrorListForm()
		{
			InitializeComponent();

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
//				if(components != null)
//				{
//					components.Dispose();
//				}
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorListForm));
            this._errorListView = new Alphora.Dataphor.Frontend.Client.Windows.ErrorListView();
            this.SuspendLayout();
            // 
            // FErrorListView
            // 
            this._errorListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._errorListView.Location = new System.Drawing.Point(0, 0);
            this._errorListView.Name = "FErrorListView";
            this._errorListView.Size = new System.Drawing.Size(584, 254);
            this._errorListView.TabIndex = 0;
            // 
            // ErrorListForm
            // 
            this.ClientSize = new System.Drawing.Size(584, 254);
            this.Controls.Add(this._errorListView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ErrorListForm";
            this.Text = "Errors";
            this.ResumeLayout(false);

		}
		#endregion

		public static void ShowErrorList(ErrorList list, bool warning)
		{
			if ((list != null) && (list.Count > 0))
			{
				using (ErrorListForm form = new ErrorListForm())
				{
					form._errorListView.AppendErrors(null, list, warning);
					form.ShowDialog();
				}
			}
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			else
				return base.ProcessDialogKey(keyData);
		}
	}
}
