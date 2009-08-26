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
		private System.Windows.Forms.TextBox FExceptionMessage;
		private System.Windows.Forms.Button FCloseButton;
		private System.Windows.Forms.Button FDetailButton;
		private System.Windows.Forms.PictureBox FStopImage;
		private System.ComponentModel.Container components = null;

		public ExceptionForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			((Bitmap)FStopImage.Image).MakeTransparent();

			FWorking = Screen.FromPoint(Control.MousePosition).WorkingArea;
#if DEBUG
			FExpanded = true;
#else
			FExpanded = false;
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
			this.FExceptionMessage = new System.Windows.Forms.TextBox();
			this.FCloseButton = new System.Windows.Forms.Button();
			this.FDetailButton = new System.Windows.Forms.Button();
			this.FStopImage = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.FStopImage)).BeginInit();
			this.SuspendLayout();
			// 
			// FExceptionMessage
			// 
			this.FExceptionMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.FExceptionMessage.Location = new System.Drawing.Point(50, 8);
			this.FExceptionMessage.MaxLength = 0;
			this.FExceptionMessage.Multiline = true;
			this.FExceptionMessage.Name = "FExceptionMessage";
			this.FExceptionMessage.ReadOnly = true;
			this.FExceptionMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.FExceptionMessage.Size = new System.Drawing.Size(229, 33);
			this.FExceptionMessage.TabIndex = 2;
			this.FExceptionMessage.TabStop = false;
			// 
			// FCloseButton
			// 
			this.FCloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.FCloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.FCloseButton.Location = new System.Drawing.Point(94, 49);
			this.FCloseButton.Name = "FCloseButton";
			this.FCloseButton.Size = new System.Drawing.Size(88, 24);
			this.FCloseButton.TabIndex = 0;
			this.FCloseButton.Text = "&Close";
			this.FCloseButton.Click += new System.EventHandler(this.Ok_Click);
			// 
			// FDetailButton
			// 
			this.FDetailButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.FDetailButton.Location = new System.Drawing.Point(190, 49);
			this.FDetailButton.Name = "FDetailButton";
			this.FDetailButton.Size = new System.Drawing.Size(88, 24);
			this.FDetailButton.TabIndex = 1;
			this.FDetailButton.Text = "&Details >>";
			this.FDetailButton.Click += new System.EventHandler(this.detailButton_Click);
			// 
			// FStopImage
			// 
			this.FStopImage.Image = ((System.Drawing.Image)(resources.GetObject("FStopImage.Image")));
			this.FStopImage.Location = new System.Drawing.Point(10, 10);
			this.FStopImage.Name = "FStopImage";
			this.FStopImage.Size = new System.Drawing.Size(32, 32);
			this.FStopImage.TabIndex = 3;
			this.FStopImage.TabStop = false;
			// 
			// ExceptionForm
			// 
			this.AcceptButton = this.FCloseButton;
			this.CancelButton = this.FCloseButton;
			this.ClientSize = new System.Drawing.Size(288, 81);
			this.ControlBox = false;
			this.Controls.Add(this.FStopImage);
			this.Controls.Add(this.FDetailButton);
			this.Controls.Add(this.FCloseButton);
			this.Controls.Add(this.FExceptionMessage);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExceptionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Exception";
			((System.ComponentModel.ISupportInitialize)(this.FStopImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private Rectangle FWorking;

		private bool FExpanded;
		public bool Expanded
		{
			get { return FExpanded; }
			set
			{
				if (FExpanded != value)
				{
					FExpanded = value;
					UpdateExpanded();
				}
			}
		}

		private void UpdateExpanded()
		{
			if (FExpanded)
			{
				FDetailButton.Text = Strings.CLessDetails;
				FExceptionMessage.WordWrap = false;
			}
			else
			{
				FDetailButton.Text = Strings.CMoreDetails;
				FExceptionMessage.WordWrap = true;
			}
			
			if (FException != null)
			{
				if (FExpanded)
					FExceptionMessage.Text = ExceptionUtility.DetailedDescription(FException);
				else
					FExceptionMessage.Text = ExceptionUtility.BriefDescription(FException);
			}
			else
				FExceptionMessage.Text = String.Empty;
			UpdateSize();
		}

		private void UpdateSize()
		{
			if (this.IsHandleCreated)
			{
				Size LOverhead = Size - FExceptionMessage.DisplayRectangle.Size;
				Rectangle LWorking = FWorking;
				LWorking.Size = LWorking.Size - LOverhead;	// Convert working to inner size
				LWorking.Width -= System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
				LWorking.Height -= System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				Size LSize;
				using (System.Drawing.Graphics LGraphics = FExceptionMessage.CreateGraphics())
				{
					if (FExceptionMessage.WordWrap)
						LSize = Size.Ceiling(LGraphics.MeasureString(FExceptionMessage.Text, FExceptionMessage.Font, LWorking.Width));
					else
						LSize = Size.Ceiling(LGraphics.MeasureString(FExceptionMessage.Text, FExceptionMessage.Font));
				}
				ScrollBars LScrollBars = ScrollBars.None;
				if (LSize.Height > LWorking.Height)
				{
					LSize.Height = LWorking.Height;
					LScrollBars |= ScrollBars.Vertical;
				}
				if (LSize.Width > LWorking.Width)
				{
					LSize.Width = LWorking.Width;
					LScrollBars |= ScrollBars.Horizontal;
				}
				FExceptionMessage.ScrollBars = LScrollBars;
				Element.ConstrainMin(ref LSize, new Size(220, 26));
				LSize += LOverhead;
				this.Bounds = 
					new Rectangle
					(
						new Point
						(
							((FWorking.Width / 2) - (LSize.Width / 2)) + FWorking.Left, 
							((FWorking.Height / 2) - (LSize.Height / 2)) + FWorking.Top
						),
						LSize
					);
			}
		}

		private Exception FException;
		public Exception Exception
		{
			get { return FException; }
			set
			{
				FException = value;
				UpdateExpanded();
				if (FException != null)
					Text = FException.GetType().ToString();
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

		protected override void OnLoad(EventArgs AArgs)
		{
			base.OnLoad(AArgs);
			UpdateSize();
		}

	}
}
