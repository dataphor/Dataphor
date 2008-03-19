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
	/// <summary> ApplicationListForm </summary>
	public class ApplicationListForm : BaseForm
	{
		private Alphora.Dataphor.DAE.Client.DataSource FDataSource;
		private Label uriLabel;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid FGrid;
		private System.ComponentModel.IContainer components;

		public ApplicationListForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			SetAcceptReject(true, false);
			FStatusBar.Visible = false;
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
			this.FDataSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this.FGrid = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			this.uriLabel = new System.Windows.Forms.Label();
			this.FContentPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// FContentPanel
			// 
			this.FContentPanel.Controls.Add(this.uriLabel);
			this.FContentPanel.Controls.Add(this.FGrid);
			this.FContentPanel.Size = new System.Drawing.Size(444, 194);
			// 
			// FGrid
			// 
			this.FGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.FGrid.BackColor = System.Drawing.Color.Transparent;
			this.FGrid.CausesValidation = false;
			this.FGrid.Columns.Add(new Alphora.Dataphor.DAE.Client.Controls.TextColumn("Description", "Description", 398, System.Windows.Forms.HorizontalAlignment.Left, System.Windows.Forms.HorizontalAlignment.Left, Alphora.Dataphor.DAE.Client.Controls.VerticalAlignment.Top, System.Drawing.Color.Transparent, true, System.Windows.Forms.Border3DStyle.RaisedInner, ((System.Windows.Forms.Border3DSide)((((System.Windows.Forms.Border3DSide.Left | System.Windows.Forms.Border3DSide.Top)
								| System.Windows.Forms.Border3DSide.Right)
								| System.Windows.Forms.Border3DSide.Bottom))), -1, -1, new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))), System.Drawing.SystemColors.ControlText, false, false));
			this.FGrid.Location = new System.Drawing.Point(10, 27);
			this.FGrid.Name = "FGrid";
			this.FGrid.NoValueBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))), ((int)(((byte)(240)))));
			this.FGrid.Size = new System.Drawing.Size(426, 158);
			this.FGrid.Source = this.FDataSource;
			this.FGrid.TabIndex = 1;
			this.FGrid.Text = "dbGrid1";
			// 
			// uriLabel
			// 
			this.uriLabel.AutoSize = true;
			this.uriLabel.BackColor = System.Drawing.Color.Transparent;
			this.uriLabel.Location = new System.Drawing.Point(7, 9);
			this.uriLabel.Name = "uriLabel";
			this.uriLabel.Size = new System.Drawing.Size(98, 13);
			this.uriLabel.TabIndex = 15;
			this.uriLabel.Text = "&Server Applications";
			// 
			// ApplicationListForm
			// 
			this.ClientSize = new System.Drawing.Size(444, 265);
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "ApplicationListForm";
			this.Text = "Select Application";
			this.FContentPanel.ResumeLayout(false);
			this.FContentPanel.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		public static void Execute(DAE.Client.DataView AView)
		{
			using (ApplicationListForm LForm = new ApplicationListForm())
			{
				LForm.FDataSource.DataSet = AView;
				if (LForm.ShowDialog() != DialogResult.OK)
					throw new AbortException();
			}
		}

		private void FGrid_DoubleClick(object sender, System.EventArgs e)
		{
			Close(CloseBehavior.AcceptOrClose);
		}

		private void RefreshClicked(object sender, EventArgs e)
		{
			FDataSource.DataSet.Refresh();
		}
	}
}
