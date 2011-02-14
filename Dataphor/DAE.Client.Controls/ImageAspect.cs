/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	using System.ComponentModel;

	/// <summary> <c>StretchStyles</c> define the styles for stretching an image. </summary>
	public enum StretchStyles 
	{
		/// <summary> Maintain the aspect ratio while stretching. </summary>
		StretchRatio,
		/// <summary> Stretch the image to fill the entire client area. </summary>
		StretchFill,
		/// <summary> Maintain the original size of the image. </summary>
		NoStretch
	}

	[ToolboxItem(true)]
	[DefaultProperty("Image")]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.ImageAspect),"Icons.ImageAspect.bmp")]
	public class ImageAspect : BorderedControl
	{
		/// <summary> Initializes a new instance of an ImageAspect. </summary>
		public ImageAspect() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			CausesValidation = false;
			_center = true;
			_focusedBorderWidth = 2;
			_focusedBorderColor = SystemColors.WindowFrame;
			BorderStyle = BorderStyle.None;
		}

		protected override void Dispose(bool disposing)
		{
			_image = null;
			base.Dispose(disposing);
		}

		[Category("Image")]
		public event EventHandler ImageChanged;
		protected virtual void OnImageChanged(EventArgs args)
		{
			if (ImageChanged != null)
				ImageChanged(this, args);
		}

		[DefaultValue(false)]
		[Category("Behavior")]
		public virtual bool ReadOnly
		{
			get { return !GetStyle(ControlStyles.Selectable); }
			set { SetStyle(ControlStyles.Selectable, !value); }
		}

		protected Image _image;
		[DefaultValue(null)]
		[Category("Appearance")]
		public virtual System.Drawing.Image Image
		{
			get { return _image; }
			set
			{
				if (_image != value)
				{
					_image = value;
					Invalidate(Region);
					OnImageChanged(EventArgs.Empty);
				}
			}
		}

		protected StretchStyles _stretchStyle;
		[Category("Behavior")]
		[DefaultValue(StretchStyles.StretchRatio)]
		[Description("How to stretch the image.")]
		public StretchStyles StretchStyle
		{
			get { return _stretchStyle; }
			set
			{
				if (_stretchStyle != value)
				{
					_stretchStyle = value;
					Invalidate(Region);
				}
			}
		}

		protected bool _center;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Centers the Image in the control.")]
		public bool Center
		{
			get { return _center; }
			set
			{
				if (_center != value)
				{
					_center = value;
					Invalidate(Region);
				}
			}
		}

		private Color _focusedBorderColor;
		/// <summary>
		/// Color of the focused border.
		/// </summary>
		[Category("Appearance")]
		[Description("Color of the focused border.")]
		public Color FocusedBorderColor
		{
			get { return _focusedBorderColor; }
			set
			{
				if (_focusedBorderColor != value)
					_focusedBorderColor = value;
			}
		}

		private int _focusedBorderWidth;
		/// <summary>
		/// Width of the focused border.
		/// </summary>
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("Width of the border when focused")]
		public int FocusedBorderWidth
		{
			get { return _focusedBorderWidth; }
			set
			{
				if ((_focusedBorderWidth != value) && (value >= 0))
				{
					_focusedBorderWidth = value;
					if (Focused)
					{
						EraseFocusBorder();
						using (Graphics graphics = CreateGraphics())
							PaintFocusBorder(graphics);
					}
				}	
			}
		}

		protected virtual Rectangle DestinationRect()
		{
			if (_image != null)
				return InternalGetDestinationRect();
			else
				return new Rectangle(0, 0, 0, 0);
		}

		/// <summary>
		///	Returns true if the client rectangle is taller than the images aspect rectangle.
		/// </summary>
		/// <param name="imageRectangle"> See <see cref="Rectangle"/> </param>
		/// <remarks>
		///		Compares the ratio(Height / Width) of the image with the components ratio(Height / Width),
		///		returns true if the client ratio is greater or equal to the Image ratio.
		///	</remarks>
		public static bool IsClientTaller(Rectangle imageRectangle, Rectangle clientRectangle)
		{
			return ((float)imageRectangle.Height / (float)imageRectangle.Width) <= ((float)clientRectangle.Height / (float)clientRectangle.Width);
		}
		
		public static Rectangle ImageAspectRectangle(Rectangle imageRectangle, Rectangle clientRectangle)
		{
			if (ImageAspect.IsClientTaller(imageRectangle, clientRectangle))
			{
				float dy = imageRectangle.Height * ((float)clientRectangle.Width / (float)imageRectangle.Width);
				imageRectangle.Height = (int)Math.Round(dy);
				imageRectangle.Width = clientRectangle.Width;
			}
			else
			{
				float dx = imageRectangle.Width * ((float)clientRectangle.Height / (float)imageRectangle.Height);
				imageRectangle.Width = (int)Math.Round(dx);
				imageRectangle.Height = clientRectangle.Height;
			}
			return imageRectangle;
		}

		/// <summary>
		/// Returns the rectangular region to paint the image based on the StretchStyle.
		/// </summary>
		private Rectangle InternalGetDestinationRect()
		{
			Rectangle rectangle = new Rectangle(0, 0, _image.Width, _image.Height);
			if (_stretchStyle == StretchStyles.StretchRatio)
			{
				if ((rectangle.Width != 0) && (rectangle.Height != 0))
					rectangle = ImageAspectRectangle(rectangle, ClientRectangle);
				if (_center)
				{
					rectangle.Offset
						(
						(this.ClientRectangle.Width - rectangle.Width) / 2,
						(this.ClientRectangle.Height - rectangle.Height) / 2
						);
				}
			}
			else if (_stretchStyle == StretchStyles.StretchFill)
			{
				rectangle.Width = this.ClientRectangle.Width;
				rectangle.Height = this.ClientRectangle.Height;
			}
			else
			{
				if (_center)
				{
					rectangle.Offset
					(
						(this.ClientRectangle.Width - rectangle.Width) / 2,
						(this.ClientRectangle.Height - rectangle.Height) / 2
					);
				}
			}
			_lastDestinationRect = rectangle;
			return rectangle;
		}

		protected virtual void PaintImage(PaintEventArgs args)
		{
			using (SolidBrush backBrush = new SolidBrush(BackColor))
				using (Region region = new Region(args.ClipRectangle))
					args.Graphics.FillRegion(backBrush, region);
			if ((_image != null) && (!_image.Size.IsEmpty))
			{
				Rectangle destRect = DestinationRect();
				args.Graphics.DrawImage(Image, destRect);
			}
		}

		protected virtual void PaintFocusBorder(Graphics graphics)
		{
			if (Focused && (_focusedBorderWidth > 0))
			{
				Pen pen = new Pen(_focusedBorderColor, _focusedBorderWidth);
				graphics.DrawRectangle(pen, ClientRectangle);
			}
		}

		protected virtual void EraseFocusBorder()
		{
			//Erase top then bottom.
			Rectangle rect = new Rectangle(0,0, ClientRectangle.Width, FocusedBorderWidth);
			Region regionTopThenBottom = new Region(rect);
			this.Invalidate(regionTopThenBottom);
			regionTopThenBottom.Translate(0,ClientRectangle.Height - FocusedBorderWidth);
			this.Invalidate(regionTopThenBottom);

			//Erase Left then right.
			rect.Width = FocusedBorderWidth;
			rect.Height = ClientRectangle.Height;
			Region regionLeftThenRight = new Region(rect);
			this.Invalidate(regionLeftThenRight);
			regionLeftThenRight.Translate(ClientRectangle.Width - FocusedBorderWidth,0);
			this.Invalidate(regionLeftThenRight);
		}

		protected override void OnPaint(PaintEventArgs paintEventArgs)
		{
			base.OnPaint(paintEventArgs);
			if ((!paintEventArgs.ClipRectangle.IsEmpty) && (paintEventArgs.ClipRectangle.IntersectsWith(DestinationRect())))
				PaintImage(paintEventArgs);
			PaintFocusBorder(paintEventArgs.Graphics);
		}

		private Rectangle _lastDestinationRect = Rectangle.Empty;
		protected override void OnResize(EventArgs eventArgs)
		{
			base.OnResize(eventArgs);
			Rectangle lastDestinationRect = _lastDestinationRect;
			Rectangle newDestinationRect = DestinationRect();
			if (newDestinationRect.Contains(lastDestinationRect))
				Invalidate(newDestinationRect);
			else
				Invalidate(Rectangle.Union(lastDestinationRect, newDestinationRect));
		}

		protected override void OnGotFocus(EventArgs eventArgs)
		{
			base.OnGotFocus(eventArgs);
			using (Graphics graphics = CreateGraphics())
			{
				PaintFocusBorder(graphics);
			}
		}

		protected override void OnLostFocus(EventArgs eventArgs)
		{
			base.OnLostFocus(eventArgs);
			EraseFocusBorder();
		}

		protected override void OnMouseDown(MouseEventArgs mouseEventArgs)
		{
			if ((mouseEventArgs.Button == MouseButtons.Left) && CanSelect && CanFocus)
				Focus();
			base.OnMouseDown(mouseEventArgs);
		}

		private bool _controlKeyPressed;
		protected override void OnKeyDown(KeyEventArgs keyEventArgs)
		{
			_controlKeyPressed = keyEventArgs.Control;
			base.OnKeyDown(keyEventArgs);
		}

		protected override void OnKeyPress(KeyPressEventArgs keyPressEventArgs)
		{
			base.OnKeyPress(keyPressEventArgs);
			if (_controlKeyPressed)
			{				
				switch (keyPressEventArgs.KeyChar)
				{
					case (char)24 : UnsafeNativeMethods.SendMessage(Handle, NativeMethods.WM_CUT, IntPtr.Zero, IntPtr.Zero);
						break;
					case (char)3 : UnsafeNativeMethods.SendMessage(Handle, NativeMethods.WM_COPY, IntPtr.Zero, IntPtr.Zero);
						break;
					case (char)22 : UnsafeNativeMethods.SendMessage(Handle, NativeMethods.WM_PASTE, IntPtr.Zero, IntPtr.Zero);
						break;
				}
			}
		}

		protected virtual void WMCut(ref Message message)
		{
			if ((_image != null) && !ReadOnly)
			{
				WMCopy(ref message);
				_image = null;
				Invalidate(Region);
				OnImageChanged(EventArgs.Empty);
			}
		}

		protected virtual void WMCopy(ref Message message)
		{
			if (_image != null)
				Clipboard.SetDataObject(Image, false);
		}

		private void PasteFromClipboard()
		{
			IDataObject dataObjectInterface = Clipboard.GetDataObject();
			if (dataObjectInterface != null)
			{
				Image image = null;
				if (dataObjectInterface.GetDataPresent(typeof(System.Drawing.Bitmap)))
					image = (Bitmap)dataObjectInterface.GetData(typeof(System.Drawing.Bitmap));
				else if (dataObjectInterface.GetDataPresent(typeof(System.Drawing.Imaging.Metafile)))
					image = (System.Drawing.Imaging.Metafile)dataObjectInterface.GetData(typeof(System.Drawing.Imaging.Metafile));
				if (image != null)
				{
					_image = image;
					Invalidate(Region);
					OnImageChanged(EventArgs.Empty);
				}
			}
		}

		protected virtual void WMPaste(ref Message message)
		{
			if (!ReadOnly)
			{
				PasteFromClipboard();
				//ToDo: Fix streaming anomalies when image is set by a paste operation.
				//Workaround Paste copy then paste again. Seems to fix it hmmm ugly.
				WMCopy(ref message);
				PasteFromClipboard();
			}
		}
		
		protected override void WndProc(ref Message message)
		{
			switch (message.Msg)
			{
				case NativeMethods.WM_CUT:
					WMCut(ref message);
					break;
				case NativeMethods.WM_COPY:
					WMCopy(ref message);
					break;
				case NativeMethods.WM_PASTE:
					WMPaste(ref message);
					break;
			}
			base.WndProc(ref message);
		}
		
	}
}
