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
		public virtual bool PaintBackground(Control control, PaintEventArgs args)
		{
			return false;
		}
	}

	public class GradiantTheme : Theme
	{
		public Color GradiantBackgroundColor;
		public float Angle;
		
		public override bool PaintBackground(Control control, PaintEventArgs args)
		{
			Rectangle rect = control.ClientRectangle;
			if ((rect.Width > 0) && (rect.Height > 0))
				using (Brush brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, BackgroundColor, GradiantBackgroundColor, Angle, false))
					args.Graphics.FillRectangle(brush, rect);
			return true;
		}
	}
	
	public class ImageAlignmentTheme : Theme
	{
		private int _imageAlpha = 220;
		public int ImageAlpha
		{
			get { return _imageAlpha; }
			set { _imageAlpha = value; }
		}
		
		private System.Drawing.Image _image;
		private string _imageFileName;
		public string ImageFileName
		{
			get { return _imageFileName; }
			set
			{
				if (_imageFileName != value)
				{
					_imageFileName = value;
					if (_image != null)
						_image.Dispose();
					_image = System.Drawing.Image.FromFile(_imageFileName);
					if (_image is Bitmap)
						((Bitmap)_image).MakeTransparent();
				}
			}
		}
		
		private ContentAlignment _imageAlignment = ContentAlignment.BottomRight;
		public ContentAlignment ImageAlignment
		{
			get { return _imageAlignment; }
			set { _imageAlignment = value; }
		}
		
		private int _topMargin = 10;
		public int TopMargin
		{
			get { return _topMargin; }
			set { _topMargin = value; }
		}
		
		private int _leftMargin = 10;
		public int LeftMargin
		{
			get { return _leftMargin; }
			set { _leftMargin = value; }
		}
		
		private int _bottomMargin = 10;
		public int BottomMargin
		{
			get { return _bottomMargin; }
			set { _bottomMargin = value; }
		}
		
		private int _rightMargin = 10;
		public int RightMargin
		{
			get { return _rightMargin; }
			set { _rightMargin = value; }
		}
		
		public override bool PaintBackground(Control control, PaintEventArgs args)
		{
			if (control is IBaseForm)
			{
				IBaseForm form = (IBaseForm)control;
				using (Brush brush = new SolidBrush(BackgroundColor))
					args.Graphics.FillRectangle(brush, args.ClipRectangle);

				switch (_imageAlignment)
				{
					case ContentAlignment.TopLeft:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Left + _leftMargin,
							form.ClientRectangle.Top + _topMargin
						);
					break;

					case ContentAlignment.TopCenter:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Left + (form.ClientRectangle.Width / 2) - (_image.Size.Width / 2),
							form.ClientRectangle.Top + _topMargin
						);
					break;

					case ContentAlignment.TopRight:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Right - _image.Size.Width - _rightMargin,
							form.ClientRectangle.Top + _topMargin
						);
					break;

					case ContentAlignment.MiddleLeft:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Left + _leftMargin,
							form.ClientRectangle.Top + (form.ClientRectangle.Height / 2) - (_image.Size.Height / 2)
						);
					break;

					case ContentAlignment.MiddleCenter:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Left + (form.ClientRectangle.Width / 2) - (_image.Size.Width / 2),
							form.ClientRectangle.Top + (form.ClientRectangle.Height / 2) - (_image.Size.Height / 2)
						);
					break;

					case ContentAlignment.MiddleRight:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Right - _image.Size.Width - _rightMargin,
							form.ClientRectangle.Top + (form.ClientRectangle.Height / 2) - (_image.Size.Height / 2)
						);
					break;

					case ContentAlignment.BottomLeft:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Left + _leftMargin,
							form.ClientRectangle.Bottom - _image.Size.Height - (form.ClientSize.Height - form.ContentPanel.Bottom) - _bottomMargin
						);
					break;
					
					case ContentAlignment.BottomCenter:
						args.Graphics.DrawImageUnscaled
						(
							_image,
							form.ClientRectangle.Left + (form.ClientRectangle.Width / 2) - (_image.Size.Width / 2),
							form.ClientRectangle.Bottom - _image.Size.Height - (form.ClientSize.Height - form.ContentPanel.Bottom) - _bottomMargin
						);
					break;
					
					case ContentAlignment.BottomRight:
						args.Graphics.DrawImageUnscaled
						(
							_image, 
							form.ClientRectangle.Right - _image.Size.Width - _rightMargin, 
							form.ClientRectangle.Bottom - _image.Size.Height - (form.ClientSize.Height - form.ContentPanel.Bottom) - _bottomMargin
						);
					break;
				}
				
				using (Brush brush = new SolidBrush(Color.FromArgb(_imageAlpha, BackgroundColor)))
					args.Graphics.FillRectangle(brush, args.ClipRectangle);

				return true;
			}
			else
				return base.PaintBackground(control, args);
		}
	}
}
