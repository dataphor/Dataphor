using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class StaticImage : Element, IStaticImage
	{
		// IVerticalAlignedElement

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					UpdateBinding(FrameworkElement.VerticalAlignmentProperty);
				}
			}
		}

		protected override object UIGetVerticalAlignment()
		{
			return ConvertVerticalAlignment(FVerticalAlignment);
		}

		// IHorizontalAlignedElement

		protected HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		public HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set
			{
				if (FHorizontalAlignment != value)
				{
					FHorizontalAlignment = value;
					UpdateBinding(FrameworkElement.HorizontalAlignmentProperty);
				}
			}
		}

		protected override object UIGetHorizontalAlignment()
		{
			return ConvertHorizontalAlignment(FHorizontalAlignment);
		}

		private int FImageWidth = -1;
		public int ImageWidth
		{
			get { return FImageWidth; }
			set
			{
				if (FImageWidth != value)
				{
					FImageWidth = value;
					UpdateBinding(FrameworkElement.WidthProperty);
				}
			}
		}
		
		private object UIGetWidth()
		{
			if (FImageWidth >= 0)
				return (double)FImageWidth;
			else if (FSource != null && (FSource is BitmapImage))
				return (double)((BitmapImage)FSource).PixelWidth;
			else
				return 0d;
		}

		private int FImageHeight = -1;
		public int ImageHeight
		{
			get { return FImageHeight; }
			set
			{
				if (FImageHeight != value)
				{
					FImageHeight = value;
					UpdateBinding(FrameworkElement.HeightProperty);
				}
			}
		}

		private object UIGetHeight()
		{
			if (FImageHeight >= 0)
				return (double)FImageHeight;
			else if (FSource != null && (FSource is BitmapImage))
				return (double)((BitmapImage)FSource).PixelHeight;
			else
				return 0d;
		}

		// StretchStyle
		
		private StretchStyles FStretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		[Description("Specifies how to fit the image into the specified space.")]
		public StretchStyles StretchStyle
		{
			get { return FStretchStyle; }
			set 
			{ 
				if (FStretchStyle != value)
				{
					FStretchStyle = value; 
					UpdateBinding(System.Windows.Controls.Image.StretchProperty);
				}
			}
		}
		
		private object UIGetStretch()
		{
			switch (FStretchStyle)
			{
				case StretchStyles.NoStretch : return Stretch.None;
				case StretchStyles.StretchFill : return Stretch.Fill;
				default /* case StretchStyles.StretchRatio */ : return Stretch.Uniform;
			}
		}

		private string FImage = String.Empty;
		public string Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					if (Active)
						UpdateSource();
				}
			}
		}

		// Source
		
		private ImageSource FSource;
		
		public ImageSource Source
		{
			get { return FSource; }
			private set
			{
				if (FSource != value)
				{
					FSource = value;
					UpdateBinding(System.Windows.Controls.Image.SourceProperty);
					UpdateBinding(FrameworkElement.WidthProperty);
					UpdateBinding(FrameworkElement.HeightProperty);
				}
			}
		}

		private object UIGetSource()
		{
			return FSource;
		}

		protected PipeRequest FImageRequest;

		private void CancelImageRequest()
		{
			if (FImageRequest != null)
			{
				HostNode.Pipe.CancelRequest(FImageRequest);
				FImageRequest = null;
			}
		}

		private void UpdateSource()
		{
			CancelImageRequest();
			if (FImage == String.Empty)
				Source = null;
			else
			{
				// Queue up an asynchronous request
				FImageRequest = new PipeRequest(FImage, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				HostNode.Pipe.QueueRequest(FImageRequest);
			}
		}

		protected void ImageRead(PipeRequest ARequest, Pipe APipe)
		{
			if (Active)
			{
				FImageRequest = null;
				try
				{
					if (ARequest.Result.IsNative)
						Source = ImageUtility.BitmapImageFromBytes(ARequest.Result.AsByteArray);
					else
						using (Stream LStream = ARequest.Result.OpenStream())
							Source = ImageUtility.BitmapImageFromStream(LStream);
				}
				catch
				{
					Source = ImageUtility.GetErrorImage();
				}
			}
		}

		protected void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			Source = ImageUtility.GetErrorImage();
			this.HandleException(AException);
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		protected override FrameworkElement CreateFrameworkElement()
		{
			return new System.Windows.Controls.Image();
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(FrameworkElement.HeightProperty, new Func<object>(UIGetHeight));
			AddBinding(FrameworkElement.WidthProperty, new Func<object>(UIGetWidth));
			AddBinding(System.Windows.Controls.Image.StretchProperty, new Func<object>(UIGetStretch));
			AddBinding(System.Windows.Controls.Image.SourceProperty, new Func<object>(UIGetSource));
		}

		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				CancelImageRequest();
			}
		}

		internal protected override void AfterActivate()
		{
			UpdateSource();
			base.AfterActivate();
		}
	}
}
