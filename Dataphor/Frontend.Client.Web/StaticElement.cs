/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Web.UI;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Web
{
    public class StaticText : Element, IStaticText
    {
		const int DefaultWidth = 40;

		// Text

		protected string _text = String.Empty;
		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		// Width

		protected int _width = DefaultWidth;
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					if (_width < 1)
						throw new ClientException(ClientException.Codes.CharsPerLineInvalid);
					_width = value;
				}
			}
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			string hint = GetHint();
			if (hint != String.Empty)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				writer.Write(HttpUtility.HtmlEncode(_text));
				writer.RenderEndTag();
			}
			else
				writer.Write(HttpUtility.HtmlEncode(_text));
		}

    }

	public class StaticImage : Element, IStaticImage
	{
		// TODO: True support for border (with property)
		// TODO: Support for alignment

		// VerticalAlignment

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set	{ _verticalAlignment = value; }
		}

		// HorizontalAlignment

		protected HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set { _horizontalAlignment = value; }
		}

		// ImageWidth

		private int _imageWidth = -1;
		[DefaultValue(-1)]
		public int ImageWidth
		{
			get { return _imageWidth; }
			set { _imageWidth = value; }
		}

		// ImageHeight

		private int _imageHeight = -1;
		[DefaultValue(-1)]
		public int ImageHeight
		{
			get { return _imageHeight; }
			set { _imageHeight = value; }
		}

		// StretchStyle
		
		private StretchStyles _stretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		public StretchStyles StretchStyle
		{
			get { return _stretchStyle; }
			set { _stretchStyle = value; }
		}

		// Image

		private string _image = String.Empty;
		[DefaultValue("")]
		public string Image
		{
			get { return _image; }
			set 
			{
				if (_image != value)
				{
					_image = value;
					DeallocateImage();
					if (Active)
						AllocateImage();
				}
			}
		}

		private void AllocateImage()
		{
			_imageID = WebSession.ImageCache.Allocate(_image);
		}

		private void DeallocateImage()
		{
			if (_imageID != String.Empty)
			{
				WebSession.ImageCache.Deallocate(_imageID);
				_imageID = String.Empty;
			}
		}

		private string _imageID = String.Empty;
		public string ImageID { get { return _imageID; } }

		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			if (_horizontalAlignment != HorizontalAlignment.Left)
			{
				if (_horizontalAlignment == HorizontalAlignment.Center)
					writer.AddAttribute("align", "center", false);
				else
					writer.AddAttribute(HtmlTextWriterAttribute.Align, "right", false);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
			}
			
			string hint = GetHint();
			if (hint != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);

			writer.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?ImageID=" + _imageID);
			RenderStretchAttributes(writer, _stretchStyle, _imageWidth, _imageHeight);
			writer.AddAttribute(HtmlTextWriterAttribute.Valign, _verticalAlignment.ToString().ToLower());
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			if (_horizontalAlignment != HorizontalAlignment.Left)
				writer.RenderEndTag();
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			AllocateImage();
		}

		protected override void Deactivate()
		{
			DeallocateImage();
			base.Deactivate();
		}

		// Static utilities

		public static void RenderStretchAttributes(HtmlTextWriter writer, StretchStyles stretchStyle, int width, int height)
		{
			switch (stretchStyle)
			{
				case StretchStyles.StretchFill :
					if (width >= 0)
						writer.AddAttribute(HtmlTextWriterAttribute.Width, width.ToString());
					if (height >= 0)
						writer.AddAttribute(HtmlTextWriterAttribute.Height, height.ToString());
					break;
				case StretchStyles.StretchRatio :
					if ((width >= 0) && (height >= 0))
					{
						// If both dimensions are constrained, then we must resize the image on the client once loading has completed
						int localWidth = (width >= 0 ? width : Int32.MaxValue);
						int localHeight = (height >= 0 ? height : Int32.MaxValue);
						writer.AddAttribute("onload", String.Format("ConstrainSizeWithRatio(this, {0}, {1})", localWidth, localHeight));
					}
					else
						goto case StretchStyles.StretchFill;	// if only one dimension is constrained, then static width/height attribute should do it
					break;
			}
		}
	}

	public class HtmlBox : Element, IHtmlBox
	{
		// PixelWidth

		private int _pixelWidth = 100;
		public int PixelWidth
		{
			get { return _pixelWidth; }
			set { _pixelWidth = value; }
		}

		// PixelHeight

		private int _pixelHeight = 100;
		public int PixelHeight
		{
			get { return _pixelHeight; }
			set { _pixelHeight = value; }
		}

		// URL

		private string _uRL = String.Empty;
		public string URL
		{
			get { return _uRL; }
			set { _uRL = value; }
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Src, _uRL);
			writer.AddAttribute(HtmlTextWriterAttribute.Width, Convert.ToString(_pixelWidth));
			writer.AddAttribute(HtmlTextWriterAttribute.Height, Convert.ToString(_pixelHeight));
			writer.RenderBeginTag(HtmlTextWriterTag.Iframe);
			writer.RenderEndTag();
		}
	}
}
