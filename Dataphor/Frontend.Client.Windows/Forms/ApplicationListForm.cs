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
	public class ApplicationListForm : DialogForm
	{
		private Alphora.Dataphor.DAE.Client.DataSource _dataSource;
		private Label uriLabel;
		private Alphora.Dataphor.DAE.Client.Controls.DBGrid _grid;
		private System.ComponentModel.IContainer components;

		public ApplicationListForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			SetAcceptReject(true, false);
			FStatusBar.Visible = false;
			_grid.DoubleClick += FGrid_DoubleClick;
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
			this._dataSource = new Alphora.Dataphor.DAE.Client.DataSource(this.components);
			this._grid = new Alphora.Dataphor.DAE.Client.Controls.DBGrid();
			this.uriLabel = new System.Windows.Forms.Label();
			this.FContentPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// FContentPanel
			// 
			this.FContentPanel.Controls.Add(this.uriLabel);
			this.FContentPanel.Controls.Add(this._grid);
			this.FContentPanel.Size = new System.Drawing.Size(444, 194);
			// 
			// _grid
			// 
			this._grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._grid.BackColor = System.Drawing.Color.Transparent;
			this._grid.CausesValidation = false;
			this._grid.Columns.Add(new Alphora.Dataphor.DAE.Client.Controls.TextColumn("Description", "Description", 398, System.Windows.Forms.HorizontalAlignment.Left, System.Windows.Forms.HorizontalAlignment.Left, Alphora.Dataphor.DAE.Client.Controls.VerticalAlignment.Top, System.Drawing.Color.Transparent, true, System.Windows.Forms.Border3DStyle.RaisedInner, ((System.Windows.Forms.Border3DSide)((((System.Windows.Forms.Border3DSide.Left | System.Windows.Forms.Border3DSide.Top) 
                    | System.Windows.Forms.Border3DSide.Right) 
                    | System.Windows.Forms.Border3DSide.Bottom))), -1, -1, new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))), System.Drawing.SystemColors.ControlText, false, false));
			this._grid.Location = new System.Drawing.Point(10, 27);
			this._grid.Name = "_grid";
			this._grid.NoValueBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))), ((int)(((byte)(240)))));
			this._grid.Size = new System.Drawing.Size(426, 158);
			this._grid.Source = this._dataSource;
			this._grid.TabIndex = 1;
			this._grid.Text = "dbGrid1";
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
			this.Name = "ApplicationListForm";
			this.Text = "Select Application";
			this.DoubleClick += new System.EventHandler(this.FGrid_DoubleClick);
			this.FContentPanel.ResumeLayout(false);
			this.FContentPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		public static void Execute(DAE.Client.DataView view)
		{
			using (ApplicationListForm form = new ApplicationListForm())
			{
				form._dataSource.DataSet = view;
				if (form.ShowDialog() != DialogResult.OK)
					throw new AbortException();
			}
		}

		private void FGrid_DoubleClick(object sender, System.EventArgs e)
		{
			Close(CloseBehavior.AcceptOrClose);
		}

		private void RefreshClicked(object sender, EventArgs e)
		{
			_dataSource.DataSet.Refresh();
		}
	}
}
