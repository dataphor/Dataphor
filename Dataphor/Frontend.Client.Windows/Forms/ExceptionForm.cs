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
using System.Text;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	// Don't put any definitions above the ExceptionForm class

	/// <summary> Shows error details. </summary>
	public class ExceptionForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox _exceptionMessage;
		private System.Windows.Forms.Button _closeButton;
		private System.Windows.Forms.Button _detailButton;
		private System.Windows.Forms.PictureBox _stopImage;
		private System.ComponentModel.Container components = null;

		public ExceptionForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			((Bitmap)_stopImage.Image).MakeTransparent();

			_working = Screen.FromPoint(Control.MousePosition).WorkingArea;
#if DEBUG
			_expanded = true;
#else
			_expanded = false;
#endif
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExceptionForm));
			this._exceptionMessage = new System.Windows.Forms.TextBox();
			this._closeButton = new System.Windows.Forms.Button();
			this._detailButton = new System.Windows.Forms.Button();
			this._stopImage = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this._stopImage)).BeginInit();
			this.SuspendLayout();
			// 
			// FExceptionMessage
			// 
			this._exceptionMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._exceptionMessage.Location = new System.Drawing.Point(50, 8);
			this._exceptionMessage.MaxLength = 0;
			this._exceptionMessage.Multiline = true;
			this._exceptionMessage.Name = "FExceptionMessage";
			this._exceptionMessage.ReadOnly = true;
			this._exceptionMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this._exceptionMessage.Size = new System.Drawing.Size(229, 33);
			this._exceptionMessage.TabIndex = 2;
			this._exceptionMessage.TabStop = false;
			// 
			// FCloseButton
			// 
			this._closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._closeButton.Location = new System.Drawing.Point(94, 49);
			this._closeButton.Name = "FCloseButton";
			this._closeButton.Size = new System.Drawing.Size(88, 24);
			this._closeButton.TabIndex = 0;
			this._closeButton.Text = "&Close";
			this._closeButton.Click += new System.EventHandler(this.Ok_Click);
			// 
			// FDetailButton
			// 
			this._detailButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._detailButton.Location = new System.Drawing.Point(190, 49);
			this._detailButton.Name = "FDetailButton";
			this._detailButton.Size = new System.Drawing.Size(88, 24);
			this._detailButton.TabIndex = 1;
			this._detailButton.Text = "&Details >>";
			this._detailButton.Click += new System.EventHandler(this.detailButton_Click);
			// 
			// FStopImage
			// 
			this._stopImage.Image = ((System.Drawing.Image)(resources.GetObject("FStopImage.Image")));
			this._stopImage.Location = new System.Drawing.Point(10, 10);
			this._stopImage.Name = "FStopImage";
			this._stopImage.Size = new System.Drawing.Size(32, 32);
			this._stopImage.TabIndex = 3;
			this._stopImage.TabStop = false;
			// 
			// ExceptionForm
			// 
			this.AcceptButton = this._closeButton;
			this.CancelButton = this._closeButton;
			this.ClientSize = new System.Drawing.Size(288, 81);
			this.ControlBox = false;
			this.Controls.Add(this._stopImage);
			this.Controls.Add(this._detailButton);
			this.Controls.Add(this._closeButton);
			this.Controls.Add(this._exceptionMessage);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExceptionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Exception";
			((System.ComponentModel.ISupportInitialize)(this._stopImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private Rectangle _working;

		private bool _expanded;
		public bool Expanded
		{
			get { return _expanded; }
			set
			{
				if (_expanded != value)
				{
					_expanded = value;
					UpdateExpanded();
				}
			}
		}

		private void UpdateExpanded()
		{
			if (_expanded)
			{
				_detailButton.Text = Strings.CLessDetails;
				_exceptionMessage.WordWrap = false;
			}
			else
			{
				_detailButton.Text = Strings.CMoreDetails;
				_exceptionMessage.WordWrap = true;
			}
			
			if (_exception != null)
			{
				if (_expanded)
					_exceptionMessage.Text = ExceptionUtility.DetailedDescription(_exception);
				else
					_exceptionMessage.Text = ExceptionUtility.BriefDescription(_exception);
			}
			else
				_exceptionMessage.Text = String.Empty;
			UpdateSize();
		}

		private void UpdateSize()
		{
			if (this.IsHandleCreated)
			{
				Size overhead = Size - _exceptionMessage.DisplayRectangle.Size;
				Rectangle working = _working;
				working.Size = working.Size - overhead;	// Convert working to inner size
				working.Width -= System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
				working.Height -= System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				Size size;
				using (System.Drawing.Graphics graphics = _exceptionMessage.CreateGraphics())
				{
					if (_exceptionMessage.WordWrap)
						size = Size.Ceiling(graphics.MeasureString(_exceptionMessage.Text, _exceptionMessage.Font, working.Width));
					else
						size = Size.Ceiling(graphics.MeasureString(_exceptionMessage.Text, _exceptionMessage.Font));
				}
				ScrollBars scrollBars = ScrollBars.None;
				if (size.Height > working.Height)
				{
					size.Height = working.Height;
					scrollBars |= ScrollBars.Vertical;
				}
				if (size.Width > working.Width)
				{
					size.Width = working.Width;
					scrollBars |= ScrollBars.Horizontal;
				}
				_exceptionMessage.ScrollBars = scrollBars;
				Element.ConstrainMin(ref size, new Size(220, 26));
				size += overhead;
				this.Bounds = 
					new Rectangle
					(
						new Point
						(
							((_working.Width / 2) - (size.Width / 2)) + _working.Left, 
							((_working.Height / 2) - (size.Height / 2)) + _working.Top
						),
						size
					);
			}
		}

		private Exception _exception;
		public Exception Exception
		{
			get { return _exception; }
			set
			{
				_exception = value;
				UpdateExpanded();
				if (_exception != null)
					Text = _exception.GetType().ToString();
				else
					Text = Strings.ExceptionTitle;
			}
		}

		private void Ok_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void detailButton_Click(object sender, System.EventArgs e)
		{
			Expanded = !Expanded;
		}

		protected override void OnLoad(EventArgs args)
		{
			base.OnLoad(args);
			UpdateSize();
		}

	}
}
