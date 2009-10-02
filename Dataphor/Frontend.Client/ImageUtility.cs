/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;

namespace Alphora.Dataphor.Frontend.Client
{
	public sealed class ImageUtility
	{
		/// <summary> Returns an error image icon. </summary>
		public static System.Drawing.Image GetErrorImage()
		{
			Bitmap LBitmap = new Bitmap(11, 14);
			try
			{
				using (Graphics LGraphics = Graphics.FromImage(LBitmap))
				{
					LGraphics.Clear(Color.White);
					using (SolidBrush LBrush = new SolidBrush(Color.Red))
					{
						using (Font LFont = new Font("Arial", 9, FontStyle.Bold))
						{
							LGraphics.DrawString("X", LFont, LBrush, 0, 0);
						}
					}
				}
			}
			catch
			{
				LBitmap.Dispose();
				throw;
			}
			return LBitmap;
		}
	}

	public class AsyncImageRequest
	{
		private PipeRequest FImageRequest = null;
		private Node FRequester;
		private string FImageExpression;
		private event EventHandler FCallBack;
		
		private Image FImage;
		public Image Image
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
			if (FImage != null)
				FImage.Dispose();
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
					FImage = System.Drawing.Image.FromStream(new MemoryStream(LResultBytes, 0, LResultBytes.Length, false, true));
				}
				else
				{
					using (Stream LStream = ARequest.Result.OpenStream())
					{
						MemoryStream LCopyStream = new MemoryStream();
						StreamUtility.CopyStream(LStream, LCopyStream);
						FImage = System.Drawing.Image.FromStream(LCopyStream);
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