/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using DAE = Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Maintains style information for interface. </summary>
	public class Theme
	{
		public Theme() : base()
		{
			HighlightColor = Color.FromArgb(60, SystemColors.Highlight);
		}

		public Color HighlightColor;	// Should have alpha contingent
		public Color ForeColor = SystemColors.ControlText;
		public Color BackgroundColor = Color.Transparent;
		public Color ContainerColor = Color.Transparent;
		public Color GridColor = Color.Transparent;
		public Color FixedColor = SystemColors.ControlLight;
		public Color ControlColor = Color.Transparent;
		public Color TextBackgroundColor = Color.Transparent;
		public Color TreeColor = SystemColors.Window;
		public Color NoValueBackColor = DAE.Client.Controls.ControlColor.NoValueBackColor;
		public Color NoValueReadOnlyBackColor = DAE.Client.Controls.ControlColor.NoValueReadOnlyBackColor;
		public Color InvalidValueBackColor = DAE.Client.Controls.ControlColor.ConversionFailBackColor;
		public Color TabColor = SystemColors.ControlLight;
		public Color TabOriginColor = Color.Wheat;
		public Color TabLineColor = Color.Gray;
		public BorderStyle ImageBorderStyle = BorderStyle.None;

		/// <summary> Custom paints the form's client area. </summary>
		/// <returns> True if the painting was handled by this method. </returns>
		public virtual bool PaintBackground(Control AControl, PaintEventArgs AArgs)
		{
			return false;
		}
	}

	public class GradiantTheme : Theme
	{
		public Color GradiantBackgroundColor;
		public float Angle;
		
		public override bool PaintBackground(Control AControl, PaintEventArgs AArgs)
		{
			Rectangle LRect = AControl.ClientRectangle;
			if ((LRect.Width > 0) && (LRect.Height > 0))
				using (Brush LBrush = new System.Drawing.Drawing2D.LinearGradientBrush(LRect, BackgroundColor, GradiantBackgroundColor, Angle, false))
					AArgs.Graphics.FillRectangle(LBrush, LRect);
			return true;
		}
	}
	
	public class ImageAlignmentTheme : Theme
	{
		private int FImageAlpha = 220;
		public int ImageAlpha
		{
			get { return FImageAlpha; }
			set { FImageAlpha = value; }
		}
		
		private System.Drawing.Image FImage;
		private string FImageFileName;
		public string ImageFileName
		{
			get { return FImageFileName; }
			set
			{
				if (FImageFileName != value)
				{
					FImageFileName = value;
					if (FImage != null)
						FImage.Dispose();
					FImage = System.Drawing.Image.FromFile(FImageFileName);
					if (FImage is Bitmap)
						((Bitmap)FImage).MakeTransparent();
				}
			}
		}
		
		private ContentAlignment FImageAlignment = ContentAlignment.BottomRight;
		public ContentAlignment ImageAlignment
		{
			get { return FImageAlignment; }
			set { FImageAlignment = value; }
		}
		
		private int FTopMargin = 10;
		public int TopMargin
		{
			get { return FTopMargin; }
			set { FTopMargin = value; }
		}
		
		private int FLeftMargin = 10;
		public int LeftMargin
		{
			get { return FLeftMargin; }
			set { FLeftMargin = value; }
		}
		
		private int FBottomMargin = 10;
		public int BottomMargin
		{
			get { return FBottomMargin; }
			set { FBottomMargin = value; }
		}
		
		private int FRightMargin = 10;
		public int RightMargin
		{
			get { return FRightMargin; }
			set { FRightMargin = value; }
		}
		
		public override bool PaintBackground(Control AControl, PaintEventArgs AArgs)
		{
			if (AControl is IBaseForm)
			{
				IBaseForm LForm = (IBaseForm)AControl;
				using (Brush LBrush = new SolidBrush(BackgroundColor))
					AArgs.Graphics.FillRectangle(LBrush, AArgs.ClipRectangle);

				switch (FImageAlignment)
				{
					case ContentAlignment.TopLeft:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Left + FLeftMargin,
							LForm.ClientRectangle.Top + FTopMargin
						);
					break;

					case ContentAlignment.TopCenter:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Left + (LForm.ClientRectangle.Width / 2) - (FImage.Size.Width / 2),
							LForm.ClientRectangle.Top + FTopMargin
						);
					break;

					case ContentAlignment.TopRight:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Right - FImage.Size.Width - FRightMargin,
							LForm.ClientRectangle.Top + FTopMargin
						);
					break;

					case ContentAlignment.MiddleLeft:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Left + FLeftMargin,
							LForm.ClientRectangle.Top + (LForm.ClientRectangle.Height / 2) - (FImage.Size.Height / 2)
						);
					break;

					case ContentAlignment.MiddleCenter:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Left + (LForm.ClientRectangle.Width / 2) - (FImage.Size.Width / 2),
							LForm.ClientRectangle.Top + (LForm.ClientRectangle.Height / 2) - (FImage.Size.Height / 2)
						);
					break;

					case ContentAlignment.MiddleRight:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Right - FImage.Size.Width - FRightMargin,
							LForm.ClientRectangle.Top + (LForm.ClientRectangle.Height / 2) - (FImage.Size.Height / 2)
						);
					break;

					case ContentAlignment.BottomLeft:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Left + FLeftMargin,
							LForm.ClientRectangle.Bottom - FImage.Size.Height - (LForm.ClientSize.Height - LForm.ContentPanel.Bottom) - FBottomMargin
						);
					break;
					
					case ContentAlignment.BottomCenter:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage,
							LForm.ClientRectangle.Left + (LForm.ClientRectangle.Width / 2) - (FImage.Size.Width / 2),
							LForm.ClientRectangle.Bottom - FImage.Size.Height - (LForm.ClientSize.Height - LForm.ContentPanel.Bottom) - FBottomMargin
						);
					break;
					
					case ContentAlignment.BottomRight:
						AArgs.Graphics.DrawImageUnscaled
						(
							FImage, 
							LForm.ClientRectangle.Right - FImage.Size.Width - FRightMargin, 
							LForm.ClientRectangle.Bottom - FImage.Size.Height - (LForm.ClientSize.Height - LForm.ContentPanel.Bottom) - FBottomMargin
						);
					break;
				}
				
				using (Brush LBrush = new SolidBrush(Color.FromArgb(FImageAlpha, BackgroundColor)))
					AArgs.Graphics.FillRectangle(LBrush, AArgs.ClipRectangle);

				return true;
			}
			else
				return base.PaintBackground(AControl, AArgs);
		}
	}
}
