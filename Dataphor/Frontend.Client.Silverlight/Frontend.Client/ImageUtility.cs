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
            {
                var LResult = new BitmapImage();
                LResult.SetSource(LInfo.Stream);
                return LResult;
            }
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
				{
					byte[] LResultBytes = ARequest.Result.AsByteArray;
					var LSource = new BitmapImage();
					LSource.SetSource(new MemoryStream(LResultBytes, 0, LResultBytes.Length, false, true));
					FImage = LSource;
				}
				else
				{
					using (Stream LStream = ARequest.Result.OpenStream())
					{
						MemoryStream LCopyStream = new MemoryStream();
						StreamUtility.CopyStream(LStream, LCopyStream);
						var LSource = new BitmapImage();
						LSource.SetSource(LCopyStream);
						FImage = LSource;
					}
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