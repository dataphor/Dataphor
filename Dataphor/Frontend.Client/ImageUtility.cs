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
			Bitmap bitmap = new Bitmap(11, 14);
			try
			{
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.Clear(Color.White);
					using (SolidBrush brush = new SolidBrush(Color.Red))
					{
						using (Font font = new Font("Arial", 9, FontStyle.Bold))
						{
							graphics.DrawString("X", font, brush, 0, 0);
						}
					}
				}
			}
			catch
			{
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}
	}

	public class AsyncImageRequest
	{
		private PipeRequest _imageRequest = null;
		private Node _requester;
		private string _imageExpression;
		private event EventHandler FCallBack;
		
		private Image _image;
		public Image Image
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
			if (_image != null)
				_image.Dispose();
			_image = null;
		}

		protected void ImageRead(PipeRequest request, Pipe pipe)
		{
			_imageRequest = null;
			try
			{
				if (request.Result.IsNative)
				{
					byte[] resultBytes = request.Result.AsByteArray;
					_image = System.Drawing.Image.FromStream(new MemoryStream(resultBytes, 0, resultBytes.Length, false, true));
				}
				else
				{
					using (Stream stream = request.Result.OpenStream())
					{
						MemoryStream copyStream = new MemoryStream();
						StreamUtility.CopyStream(stream, copyStream);
						_image = System.Drawing.Image.FromStream(copyStream);
					}
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