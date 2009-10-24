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
		
		public static BitmapImage GetBitmap(string AResourceName)
		{
            var LInfo = Application.GetResourceStream(new Uri(AResourceName, UriKind.Relative));
            using (LInfo.Stream)
				return BitmapImageFromStream(LInfo.Stream);
		}

		public static BitmapImage BitmapImageFromBytes(byte[] AImageBytes)
		{
			return 
				BitmapImageFromStream
				(
					new MemoryStream(AImageBytes, 0, AImageBytes.Length, false, true)
				);
		}

		public static BitmapImage BitmapImageFromStream(Stream AStream)
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
								var LSource = new BitmapImage();
								LSource.SetSource(AStream);
								return LSource;
							}
						)
					)
				);
		}
	}

	public class AsyncImageRequest
	{
		private PipeRequest FImageRequest = null;
		private Node FRequester;
		private string FImageExpression;
		private event EventHandler FCallBack;
		
		private ImageSource FImage;
		public ImageSource Image
		{
			get { return FImage; }
		}

		public AsyncImageRequest(Node ARequester, string AImageExpression, EventHandler ACallback) 
		{
			FRequester = ARequester;
			FImageExpression = AImageExpression;
			FCallBack += ACallback;
			UpdateImage();
		}

		private void CancelImageRequest()
		{
			if (FImageRequest != null)
			{
				FRequester.HostNode.Pipe.CancelRequest(FImageRequest);
				FImageRequest = null;
			}
		}

		private void UpdateImage()
		{
			CancelImageRequest();
			if (FImageExpression == String.Empty)
				ClearImage();
			else
			{
				// Queue up an asynchronous request
				FImageRequest = new PipeRequest(FImageExpression, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				FRequester.HostNode.Pipe.QueueRequest(FImageRequest);
			}
		}

		private void ClearImage()
		{
			FImage = null;
		}

		protected void ImageRead(PipeRequest ARequest, Pipe APipe)
		{
			FImageRequest = null;
			try
			{
				if (ARequest.Result.IsNative)
					FImage = ImageUtility.BitmapImageFromBytes(ARequest.Result.AsByteArray);
				else
				{
					using (Stream LStream = ARequest.Result.OpenStream())
						FImage = ImageUtility.BitmapImageFromStream(LStream);
				}
			}
			catch
			{
				FImage = ImageUtility.GetErrorImage();
			}
			FCallBack(this, new EventArgs());
		}

		protected void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			FImageRequest = null;
			FImage = ImageUtility.GetErrorImage();
			FCallBack(this, new EventArgs());
		}
	}
}