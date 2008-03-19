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
		const int CDefaultWidth = 40;

		// Text

		protected string FText = String.Empty;
		public string Text
		{
			get { return FText; }
			set { FText = value; }
		}

		// Width

		protected int FWidth = CDefaultWidth;
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					if (FWidth < 1)
						throw new ClientException(ClientException.Codes.CharsPerLineInvalid);
					FWidth = value;
				}
			}
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			string LHint = GetHint();
			if (LHint != String.Empty)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);
				AWriter.Write(HttpUtility.HtmlEncode(FText));
				AWriter.RenderEndTag();
			}
			else
				AWriter.Write(HttpUtility.HtmlEncode(FText));
		}

    }

	public class StaticImage : Element, IStaticImage
	{
		// TODO: True support for border (with property)
		// TODO: Support for alignment

		// VerticalAlignment

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set	{ FVerticalAlignment = value; }
		}

		// HorizontalAlignment

		protected HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set { FHorizontalAlignment = value; }
		}

		// ImageWidth

		private int FImageWidth = -1;
		[DefaultValue(-1)]
		public int ImageWidth
		{
			get { return FImageWidth; }
			set { FImageWidth = value; }
		}

		// ImageHeight

		private int FImageHeight = -1;
		[DefaultValue(-1)]
		public int ImageHeight
		{
			get { return FImageHeight; }
			set { FImageHeight = value; }
		}

		// StretchStyle
		
		private StretchStyles FStretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		public StretchStyles StretchStyle
		{
			get { return FStretchStyle; }
			set { FStretchStyle = value; }
		}

		// Image

		private string FImage = String.Empty;
		[DefaultValue("")]
		public string Image
		{
			get { return FImage; }
			set 
			{
				if (FImage != value)
				{
					FImage = value;
					DeallocateImage();
					if (Active)
						AllocateImage();
				}
			}
		}

		private void AllocateImage()
		{
			FImageID = WebSession.ImageCache.Allocate(FImage);
		}

		private void DeallocateImage()
		{
			if (FImageID != String.Empty)
			{
				WebSession.ImageCache.Deallocate(FImageID);
				FImageID = String.Empty;
			}
		}

		private string FImageID = String.Empty;
		public string ImageID { get { return FImageID; } }

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			if (FHorizontalAlignment != HorizontalAlignment.Left)
			{
				if (FHorizontalAlignment == HorizontalAlignment.Center)
					AWriter.AddAttribute("align", "center", false);
				else
					AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "right", false);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);
			}
			
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?ImageID=" + FImageID);
			RenderStretchAttributes(AWriter, FStretchStyle, FImageWidth, FImageHeight);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, FVerticalAlignment.ToString().ToLower());
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			if (FHorizontalAlignment != HorizontalAlignment.Left)
				AWriter.RenderEndTag();
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

		public static void RenderStretchAttributes(HtmlTextWriter AWriter, StretchStyles AStretchStyle, int AWidth, int AHeight)
		{
			switch (AStretchStyle)
			{
				case StretchStyles.StretchFill :
					if (AWidth >= 0)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Width, AWidth.ToString());
					if (AHeight >= 0)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Height, AHeight.ToString());
					break;
				case StretchStyles.StretchRatio :
					if ((AWidth >= 0) && (AHeight >= 0))
					{
						// If both dimensions are constrained, then we must resize the image on the client once loading has completed
						int LWidth = (AWidth >= 0 ? AWidth : Int32.MaxValue);
						int LHeight = (AHeight >= 0 ? AHeight : Int32.MaxValue);
						AWriter.AddAttribute("onload", String.Format("ConstrainSizeWithRatio(this, {0}, {1})", LWidth, LHeight));
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

		private int FPixelWidth = 100;
		public int PixelWidth
		{
			get { return FPixelWidth; }
			set { FPixelWidth = value; }
		}

		// PixelHeight

		private int FPixelHeight = 100;
		public int PixelHeight
		{
			get { return FPixelHeight; }
			set { FPixelHeight = value; }
		}

		// URL

		private string FURL = String.Empty;
		public string URL
		{
			get { return FURL; }
			set { FURL = value; }
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, FURL);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, Convert.ToString(FPixelWidth));
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, Convert.ToString(FPixelHeight));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Iframe);
			AWriter.RenderEndTag();
		}
	}
}
