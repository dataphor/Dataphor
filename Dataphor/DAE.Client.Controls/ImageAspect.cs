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
			FCenter = true;
			FFocusedBorderWidth = 2;
			FFocusedBorderColor = SystemColors.WindowFrame;
			BorderStyle = BorderStyle.None;
		}

		protected override void Dispose(bool ADisposing)
		{
			FImage = null;
			base.Dispose(ADisposing);
		}

		[Category("Image")]
		public event EventHandler ImageChanged;
		protected virtual void OnImageChanged(EventArgs AArgs)
		{
			if (ImageChanged != null)
				ImageChanged(this, AArgs);
		}

		[DefaultValue(false)]
		[Category("Behavior")]
		public virtual bool ReadOnly
		{
			get { return !GetStyle(ControlStyles.Selectable); }
			set { SetStyle(ControlStyles.Selectable, !value); }
		}

		protected Image FImage;
		[DefaultValue(null)]
		[Category("Appearance")]
		public virtual System.Drawing.Image Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					Invalidate(Region);
					OnImageChanged(EventArgs.Empty);
				}
			}
		}

		protected StretchStyles FStretchStyle;
		[Category("Behavior")]
		[DefaultValue(StretchStyles.StretchRatio)]
		[Description("How to stretch the image.")]
		public StretchStyles StretchStyle
		{
			get { return FStretchStyle; }
			set
			{
				if (FStretchStyle != value)
				{
					FStretchStyle = value;
					Invalidate(Region);
				}
			}
		}

		protected bool FCenter;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Centers the Image in the control.")]
		public bool Center
		{
			get { return FCenter; }
			set
			{
				if (FCenter != value)
				{
					FCenter = value;
					Invalidate(Region);
				}
			}
		}

		private Color FFocusedBorderColor;
		/// <summary>
		/// Color of the focused border.
		/// </summary>
		[Category("Appearance")]
		[Description("Color of the focused border.")]
		public Color FocusedBorderColor
		{
			get { return FFocusedBorderColor; }
			set
			{
				if (FFocusedBorderColor != value)
					FFocusedBorderColor = value;
			}
		}

		private int FFocusedBorderWidth;
		/// <summary>
		/// Width of the focused border.
		/// </summary>
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("Width of the border when focused")]
		public int FocusedBorderWidth
		{
			get { return FFocusedBorderWidth; }
			set
			{
				if ((FFocusedBorderWidth != value) && (value >= 0))
				{
					FFocusedBorderWidth = value;
					if (Focused)
					{
						EraseFocusBorder();
						using (Graphics LGraphics = CreateGraphics())
							PaintFocusBorder(LGraphics);
					}
				}	
			}
		}

		protected virtual Rectangle DestinationRect()
		{
			if (FImage != null)
				return InternalGetDestinationRect();
			else
				return new Rectangle(0, 0, 0, 0);
		}

		/// <summary>
		///	Returns true if the client rectangle is taller than the images aspect rectangle.
		/// </summary>
		/// <param name="AImageRectangle"> See <see cref="Rectangle"/> </param>
		/// <remarks>
		///		Compares the ratio(Height / Width) of the image with the components ratio(Height / Width),
		///		returns true if the client ratio is greater or equal to the Image ratio.
		///	</remarks>
		public static bool IsClientTaller(Rectangle AImageRectangle, Rectangle AClientRectangle)
		{
			return ((float)AImageRectangle.Height / (float)AImageRectangle.Width) <= ((float)AClientRectangle.Height / (float)AClientRectangle.Width);
		}
		
		public static Rectangle ImageAspectRectangle(Rectangle AImageRectangle, Rectangle AClientRectangle)
		{
			if (ImageAspect.IsClientTaller(AImageRectangle, AClientRectangle))
			{
				float LDy = AImageRectangle.Height * ((float)AClientRectangle.Width / (float)AImageRectangle.Width);
				AImageRectangle.Height = (int)Math.Round(LDy);
				AImageRectangle.Width = AClientRectangle.Width;
			}
			else
			{
				float LDx = AImageRectangle.Width * ((float)AClientRectangle.Height / (float)AImageRectangle.Height);
				AImageRectangle.Width = (int)Math.Round(LDx);
				AImageRectangle.Height = AClientRectangle.Height;
			}
			return AImageRectangle;
		}

		/// <summary>
		/// Returns the rectangular region to paint the image based on the StretchStyle.
		/// </summary>
		private Rectangle InternalGetDestinationRect()
		{
			Rectangle LRectangle = new Rectangle(0, 0, FImage.Width, FImage.Height);
			if (FStretchStyle == StretchStyles.StretchRatio)
			{
				if ((LRectangle.Width != 0) && (LRectangle.Height != 0))
					LRectangle = ImageAspectRectangle(LRectangle, ClientRectangle);
				if (FCenter)
				{
					LRectangle.Offset
						(
						(this.ClientRectangle.Width - LRectangle.Width) / 2,
						(this.ClientRectangle.Height - LRectangle.Height) / 2
						);
				}
			}
			else if (FStretchStyle == StretchStyles.StretchFill)
			{
				LRectangle.Width = this.ClientRectangle.Width;
				LRectangle.Height = this.ClientRectangle.Height;
			}
			else
			{
				if (FCenter)
				{
					LRectangle.Offset
					(
						(this.ClientRectangle.Width - LRectangle.Width) / 2,
						(this.ClientRectangle.Height - LRectangle.Height) / 2
					);
				}
			}
			FLastDestinationRect = LRectangle;
			return LRectangle;
		}

		protected virtual void PaintImage(PaintEventArgs AArgs)
		{
			using (SolidBrush LBackBrush = new SolidBrush(BackColor))
				using (Region LRegion = new Region(AArgs.ClipRectangle))
					AArgs.Graphics.FillRegion(LBackBrush, LRegion);
			if ((FImage != null) && (!FImage.Size.IsEmpty))
			{
				Rectangle LDestRect = DestinationRect();
				AArgs.Graphics.DrawImage(Image, LDestRect);
			}
		}

		protected virtual void PaintFocusBorder(Graphics AGraphics)
		{
			if (Focused && (FFocusedBorderWidth > 0))
			{
				Pen LPen = new Pen(FFocusedBorderColor, FFocusedBorderWidth);
				AGraphics.DrawRectangle(LPen, ClientRectangle);
			}
		}

		protected virtual void EraseFocusBorder()
		{
			//Erase top then bottom.
			Rectangle LRect = new Rectangle(0,0, ClientRectangle.Width, FocusedBorderWidth);
			Region LRegionTopThenBottom = new Region(LRect);
			this.Invalidate(LRegionTopThenBottom);
			LRegionTopThenBottom.Translate(0,ClientRectangle.Height - FocusedBorderWidth);
			this.Invalidate(LRegionTopThenBottom);

			//Erase Left then right.
			LRect.Width = FocusedBorderWidth;
			LRect.Height = ClientRectangle.Height;
			Region LRegionLeftThenRight = new Region(LRect);
			this.Invalidate(LRegionLeftThenRight);
			LRegionLeftThenRight.Translate(ClientRectangle.Width - FocusedBorderWidth,0);
			this.Invalidate(LRegionLeftThenRight);
		}

		protected override void OnPaint(PaintEventArgs APaintEventArgs)
		{
			base.OnPaint(APaintEventArgs);
			if ((!APaintEventArgs.ClipRectangle.IsEmpty) && (APaintEventArgs.ClipRectangle.IntersectsWith(DestinationRect())))
				PaintImage(APaintEventArgs);
			PaintFocusBorder(APaintEventArgs.Graphics);
		}

		private Rectangle FLastDestinationRect = Rectangle.Empty;
		protected override void OnResize(EventArgs AEventArgs)
		{
			base.OnResize(AEventArgs);
			Rectangle LLastDestinationRect = FLastDestinationRect;
			Rectangle LNewDestinationRect = DestinationRect();
			if (LNewDestinationRect.Contains(LLastDestinationRect))
				Invalidate(LNewDestinationRect);
			else
				Invalidate(Rectangle.Union(LLastDestinationRect, LNewDestinationRect));
		}

		protected override void OnGotFocus(EventArgs AEventArgs)
		{
			base.OnGotFocus(AEventArgs);
			using (Graphics LGraphics = CreateGraphics())
			{
				PaintFocusBorder(LGraphics);
			}
		}

		protected override void OnLostFocus(EventArgs AEventArgs)
		{
			base.OnLostFocus(AEventArgs);
			EraseFocusBorder();
		}

		protected override void OnMouseDown(MouseEventArgs AMouseEventArgs)
		{
			if ((AMouseEventArgs.Button == MouseButtons.Left) && CanSelect && CanFocus)
				Focus();
			base.OnMouseDown(AMouseEventArgs);
		}

		private bool FControlKeyPressed;
		protected override void OnKeyDown(KeyEventArgs AKeyEventArgs)
		{
			FControlKeyPressed = AKeyEventArgs.Control;
			base.OnKeyDown(AKeyEventArgs);
		}

		protected override void OnKeyPress(KeyPressEventArgs AKeyPressEventArgs)
		{
			base.OnKeyPress(AKeyPressEventArgs);
			if (FControlKeyPressed)
			{				
				switch (AKeyPressEventArgs.KeyChar)
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

		protected virtual void WMCut(ref Message AMessage)
		{
			if ((FImage != null) && !ReadOnly)
			{
				WMCopy(ref AMessage);
				FImage = null;
				Invalidate(Region);
				OnImageChanged(EventArgs.Empty);
			}
		}

		protected virtual void WMCopy(ref Message AMessage)
		{
			if (FImage != null)
				Clipboard.SetDataObject(Image, false);
		}

		private void PasteFromClipboard()
		{
			IDataObject LDataObjectInterface = Clipboard.GetDataObject();
			if (LDataObjectInterface != null)
			{
				Image LImage = null;
				if (LDataObjectInterface.GetDataPresent(typeof(System.Drawing.Bitmap)))
					LImage = (Bitmap)LDataObjectInterface.GetData(typeof(System.Drawing.Bitmap));
				else if (LDataObjectInterface.GetDataPresent(typeof(System.Drawing.Imaging.Metafile)))
					LImage = (System.Drawing.Imaging.Metafile)LDataObjectInterface.GetData(typeof(System.Drawing.Imaging.Metafile));
				if (LImage != null)
				{
					FImage = LImage;
					Invalidate(Region);
					OnImageChanged(EventArgs.Empty);
				}
			}
		}

		protected virtual void WMPaste(ref Message AMessage)
		{
			if (!ReadOnly)
			{
				PasteFromClipboard();
				//ToDo: Fix streaming anomalies when image is set by a paste operation.
				//Workaround Paste copy then paste again. Seems to fix it hmmm ugly.
				WMCopy(ref AMessage);
				PasteFromClipboard();
			}
		}
		
		protected override void WndProc(ref Message AMessage)
		{
			switch (AMessage.Msg)
			{
				case NativeMethods.WM_CUT:
					WMCut(ref AMessage);
					break;
				case NativeMethods.WM_COPY:
					WMCopy(ref AMessage);
					break;
				case NativeMethods.WM_PASTE:
					WMPaste(ref AMessage);
					break;
			}
			base.WndProc(ref AMessage);
		}
		
	}
}
