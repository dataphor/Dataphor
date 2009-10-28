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
			else 
				return (double)FLoadedImageWidth;
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
			else
				return (double)FLoadedImageHeight;
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
						UpdateLoadedImage();
				}
			}
		}

		// LoadedImage
		
		private ImageSource FLoadedImage;
		
		public ImageSource LoadedImage
		{
			get { return FLoadedImage; }
			private set
			{
				if (FLoadedImage != value)
				{
					FLoadedImage = value;

					StoreBitmapDimensions();
					UpdateBinding(System.Windows.Controls.Image.SourceProperty);
					UpdateBinding(FrameworkElement.WidthProperty);
					UpdateBinding(FrameworkElement.HeightProperty);
				}
			}
		}

		/// <summary> Snapshot the bitmap dimensions to avoid having to hit the pixel dimensions from the bitmap (across threads). </summary>
		private void StoreBitmapDimensions()
		{
			Session.DispatchAndWait
			(
				(System.Action)
				(
					() =>
					{
						var LLoadedBitmap = FLoadedImage as BitmapImage;
						if (LLoadedBitmap != null)
						{
							FLoadedImageWidth = LLoadedBitmap.PixelWidth;
							FLoadedImageHeight = LLoadedBitmap.PixelHeight;
						}
						else
						{
							FLoadedImageWidth = 0;
							FLoadedImageHeight = 0;
						}
					}
				)
			);
		}
		
		private int FLoadedImageWidth;
		private int FLoadedImageHeight;

		private object UIGetSource()
		{
			return FLoadedImage;
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

		private void UpdateLoadedImage()
		{
			CancelImageRequest();
			if (FImage == String.Empty || !Active)
				LoadedImage = null;
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
						LoadedImage = ImageUtility.BitmapImageFromBytes(ARequest.Result.AsByteArray);
					else
						using (Stream LStream = ARequest.Result.OpenStream())
							LoadedImage = ImageUtility.BitmapImageFromStream(LStream);
				}
				catch
				{
					LoadedImage = ImageUtility.GetErrorImage();
				}
			}
		}

		protected void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			FImageRequest = null;
			LoadedImage = ImageUtility.GetErrorImage();
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
			UpdateLoadedImage();
			base.AfterActivate();
		}
	}
}
