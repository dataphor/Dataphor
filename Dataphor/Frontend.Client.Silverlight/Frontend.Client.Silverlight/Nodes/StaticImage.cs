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

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					UpdateBinding(FrameworkElement.VerticalAlignmentProperty);
				}
			}
		}

		protected override object UIGetVerticalAlignment()
		{
			return ConvertVerticalAlignment(_verticalAlignment);
		}

		// IHorizontalAlignedElement

		protected HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set
			{
				if (_horizontalAlignment != value)
				{
					_horizontalAlignment = value;
					UpdateBinding(FrameworkElement.HorizontalAlignmentProperty);
				}
			}
		}

		protected override object UIGetHorizontalAlignment()
		{
			return ConvertHorizontalAlignment(_horizontalAlignment);
		}

		private int _imageWidth = -1;
		[DefaultValue(-1)]
		public int ImageWidth
		{
			get { return _imageWidth; }
			set
			{
				if (_imageWidth != value)
				{
					_imageWidth = value;
					UpdateBinding(FrameworkElement.WidthProperty);
				}
			}
		}
		
		private object UIGetWidth()
		{
			if (_imageWidth >= 0)
				return (double)_imageWidth;
			else 
				return (double)_loadedImageWidth;
		}

		private int _imageHeight = -1;
		[DefaultValue(-1)]
		public int ImageHeight
		{
			get { return _imageHeight; }
			set
			{
				if (_imageHeight != value)
				{
					_imageHeight = value;
					UpdateBinding(FrameworkElement.HeightProperty);
				}
			}
		}

		private object UIGetHeight()
		{
			if (_imageHeight >= 0)
				return (double)_imageHeight;
			else
				return (double)_loadedImageHeight;
		}

		// StretchStyle
		
		private StretchStyles _stretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		public StretchStyles StretchStyle
		{
			get { return _stretchStyle; }
			set 
			{ 
				if (_stretchStyle != value)
				{
					_stretchStyle = value; 
					UpdateBinding(System.Windows.Controls.Image.StretchProperty);
				}
			}
		}
		
		private object UIGetStretch()
		{
			switch (_stretchStyle)
			{
				case StretchStyles.NoStretch : return Stretch.None;
				case StretchStyles.StretchFill : return Stretch.Fill;
				default /* case StretchStyles.StretchRatio */ : return Stretch.Uniform;
			}
		}

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
					if (Active)
						UpdateLoadedImage();
				}
			}
		}

		// LoadedImage
		
		private ImageSource _loadedImage;
		
		public ImageSource LoadedImage
		{
			get { return _loadedImage; }
			private set
			{
				if (_loadedImage != value)
				{
					_loadedImage = value;

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
						var loadedBitmap = _loadedImage as BitmapImage;
						if (loadedBitmap != null)
						{
							_loadedImageWidth = loadedBitmap.PixelWidth;
							_loadedImageHeight = loadedBitmap.PixelHeight;
						}
						else
						{
							_loadedImageWidth = 0;
							_loadedImageHeight = 0;
						}
					}
				)
			);
		}
		
		private int _loadedImageWidth;
		private int _loadedImageHeight;

		private object UIGetSource()
		{
			return _loadedImage;
		}

		protected PipeRequest _imageRequest;

		private void CancelImageRequest()
		{
			if (_imageRequest != null)
			{
				HostNode.Pipe.CancelRequest(_imageRequest);
				_imageRequest = null;
			}
		}

		private void UpdateLoadedImage()
		{
			CancelImageRequest();
			if (_image == String.Empty || !Active)
				LoadedImage = null;
			else
			{
				// Queue up an asynchronous request
				_imageRequest = new PipeRequest(_image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				HostNode.Pipe.QueueRequest(_imageRequest);
			}
		}

		protected void ImageRead(PipeRequest request, Pipe pipe)
		{
			if (Active)
			{
				_imageRequest = null;
				try
				{
					if (request.Result.IsNative)
						LoadedImage = ImageUtility.BitmapImageFromBytes(request.Result.AsByteArray);
					else
						using (Stream stream = request.Result.OpenStream())
							LoadedImage = ImageUtility.BitmapImageFromStream(stream);
				}
				catch
				{
					LoadedImage = ImageUtility.GetErrorImage();
				}
			}
		}

		protected void ImageError(PipeRequest request, Pipe pipe, Exception exception)
		{
			_imageRequest = null;
			LoadedImage = ImageUtility.GetErrorImage();
			this.HandleException(exception);
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
