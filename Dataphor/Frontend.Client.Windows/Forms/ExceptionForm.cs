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
		private System.Windows.Forms.TextBox ExceptionMessage;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Button detailButton;
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ExceptionForm));
			this.ExceptionMessage = new System.Windows.Forms.TextBox();
			this.closeButton = new System.Windows.Forms.Button();
			this.detailButton = new System.Windows.Forms.Button();
			this.FStopImage = new System.Windows.Forms.PictureBox();
			this.SuspendLayout();
			// 
			// ExceptionMessage
			// 
			this.ExceptionMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.ExceptionMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ExceptionMessage.Location = new System.Drawing.Point(60, 9);
			this.ExceptionMessage.MaxLength = 0;
			this.ExceptionMessage.Multiline = true;
			this.ExceptionMessage.Name = "ExceptionMessage";
			this.ExceptionMessage.ReadOnly = true;
			this.ExceptionMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.ExceptionMessage.Size = new System.Drawing.Size(217, 26);
			this.ExceptionMessage.TabIndex = 2;
			this.ExceptionMessage.TabStop = false;
			this.ExceptionMessage.Text = "";
			// 
			// closeButton
			// 
			this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.closeButton.Location = new System.Drawing.Point(55, 44);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(106, 28);
			this.closeButton.TabIndex = 0;
			this.closeButton.Text = "&Close";
			this.closeButton.Click += new System.EventHandler(this.Ok_Click);
			// 
			// detailButton
			// 
			this.detailButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.detailButton.Location = new System.Drawing.Point(171, 44);
			this.detailButton.Name = "detailButton";
			this.detailButton.Size = new System.Drawing.Size(105, 27);
			this.detailButton.TabIndex = 1;
			this.detailButton.Text = "&Details >>";
			this.detailButton.Click += new System.EventHandler(this.detailButton_Click);
			// 
			// FStopImage
			// 
			this.FStopImage.Image = ((System.Drawing.Image)(resources.GetObject("FStopImage.Image")));
			this.FStopImage.Location = new System.Drawing.Point(12, 12);
			this.FStopImage.Name = "FStopImage";
			this.FStopImage.Size = new System.Drawing.Size(38, 36);
			this.FStopImage.TabIndex = 3;
			this.FStopImage.TabStop = false;
			// 
			// ExceptionForm
			// 
			this.AcceptButton = this.closeButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
			this.CancelButton = this.closeButton;
			this.ClientSize = new System.Drawing.Size(288, 81);
			this.ControlBox = false;
			this.Controls.Add(this.FStopImage);
			this.Controls.Add(this.detailButton);
			this.Controls.Add(this.closeButton);
			this.Controls.Add(this.ExceptionMessage);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExceptionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Exception";
			this.ResumeLayout(false);

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
				detailButton.Text = Strings.CLessDetails;
				ExceptionMessage.WordWrap = false;
			}
			else
			{
				detailButton.Text = Strings.CMoreDetails;
				ExceptionMessage.WordWrap = true;
			}
			
			if (FException != null)
			{
				if (FExpanded)
					ExceptionMessage.Text = ExceptionUtility.DetailedDescription(FException);
				else
					ExceptionMessage.Text = ExceptionUtility.BriefDescription(FException);
			}
			else
				ExceptionMessage.Text = String.Empty;
			UpdateSize();
		}

		private void UpdateSize()
		{
			if (this.IsHandleCreated)
			{
				Size LOverhead = Size - ExceptionMessage.DisplayRectangle.Size;
				Rectangle LWorking = FWorking;
				LWorking.Size = LWorking.Size - LOverhead;	// Convert working to inner size
				LWorking.Width -= System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
				LWorking.Height -= System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				Size LSize;
				using (System.Drawing.Graphics LGraphics = ExceptionMessage.CreateGraphics())
				{
					if (ExceptionMessage.WordWrap)
						LSize = Size.Ceiling(LGraphics.MeasureString(ExceptionMessage.Text, ExceptionMessage.Font, LWorking.Width));
					else
						LSize = Size.Ceiling(LGraphics.MeasureString(ExceptionMessage.Text, ExceptionMessage.Font));
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
				ExceptionMessage.ScrollBars = LScrollBars;
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
