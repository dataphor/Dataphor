/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client
{
	public sealed class ImageUtility
	{
		/// <summary> Returns an error image icon. </summary>
		public static ImageSource GetErrorImage()
		{
			return GetBitmap("Frontend.Client.Silverlight;component/Frontend.Client/Images/ImageError.png");
		}
		
		public static BitmapImage GetBitmap(string resourceName)
		{
            var info = Application.GetResourceStream(new Uri(resourceName, UriKind.Relative));
            using (info.Stream)
				return BitmapImageFromStream(info.Stream);
		}

		public static BitmapImage BitmapImageFromBytes(byte[] imageBytes)
		{
			return 
				BitmapImageFromStream
				(
					new MemoryStream(imageBytes, 0, imageBytes.Length, false, true)
				);
		}

		public static BitmapImage BitmapImageFromStream(Stream stream)
		{
			return 
				(BitmapImage)
				(
					Silverlight.Session.DispatchAndWait
					(
						(Func<BitmapImage>)
						(
							() =>
							{
								var source = new BitmapImage();
								source.SetSource(stream);
								return source;
							}
						)
					)
				);
		}
	}

	public class AsyncImageRequest
	{
		private PipeRequest _imageRequest = null;
		private Node _requester;
		private string _imageExpression;
		private event EventHandler FCallBack;
		
		private ImageSource _image;
		public ImageSource Image
		{
			get { return _image; }
		}

		public AsyncImageRequest(Node requester, string imageExpression, EventHandler callback) 
		{
			_requester = requester;
			_imageExpression = imageExpression;
			FCallBack += callback;
			UpdateImage();
		}

		private void CancelImageRequest()
		{
			if (_imageRequest != null)
			{
				_requester.HostNode.Pipe.CancelRequest(_imageRequest);
				_imageRequest = null;
			}
		}

		private void UpdateImage()
		{
			CancelImageRequest();
			if (_imageExpression == String.Empty)
				ClearImage();
			else
			{
				// Queue up an asynchronous request
				_imageRequest = new PipeRequest(_imageExpression, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				_requester.HostNode.Pipe.QueueRequest(_imageRequest);
			}
		}

		private void ClearImage()
		{
			_image = null;
		}

		protected void ImageRead(PipeRequest request, Pipe pipe)
		{
			_imageRequest = null;
			try
			{
				if (request.Result.IsNative)
					_image = ImageUtility.BitmapImageFromBytes(request.Result.AsByteArray);
				else
				{
					using (Stream stream = request.Result.OpenStream())
						_image = ImageUtility.BitmapImageFromStream(stream);
				}
			}
			catch
			{
				_image = ImageUtility.GetErrorImage();
			}
			FCallBack(this, new EventArgs());
		}

		protected void ImageError(PipeRequest request, Pipe pipe, Exception exception)
		{
			_imageRequest = null;
			_image = ImageUtility.GetErrorImage();
			FCallBack(this, new EventArgs());
		}
	}
}