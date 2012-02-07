/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client
{
	public partial class Action
	{
		// Image

		public event EventHandler OnImageChanged;

		private string _image = String.Empty;
		[DefaultValue("")]
		[Description("An image used by this action's controls as an icon.")]
		public string Image
		{
			get { return _image; }
			set
			{
				if (_image != value)
				{
					_image = value;
					if (Active)
						InternalUpdateImage();
				}
			}
		}

		private System.Drawing.Image _loadedImage;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public System.Drawing.Image LoadedImage
		{
			get { return _loadedImage; }
		}

		private void SetImage(System.Drawing.Image value)
		{
			if (_loadedImage != null)
				_loadedImage.Dispose();
			_loadedImage = value;
			if (OnImageChanged != null)
				OnImageChanged(this, EventArgs.Empty);
		}

		private PipeRequest _imageRequest;

		private void CancelImageRequest()
		{
			if (_imageRequest != null)
			{
				HostNode.Pipe.CancelRequest(_imageRequest);
				_imageRequest = null;
			}
		}

		// TODO: change this to use the AsyncImageRequest class

		/// <summary> Make sure that the image is loaded or creates an async request for it. </summary>
		private void InternalUpdateImage()
		{
			if (HostNode.Session.AreImagesLoaded())
			{
				CancelImageRequest();
				if (Image == String.Empty)
					SetImage(null);
				else
				{
					// Queue up an asynchronous request
					_imageRequest = new PipeRequest(Image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
					HostNode.Pipe.QueueRequest(_imageRequest);
				}
			}
			else
				SetImage(null);
		}

		private void ImageRead(PipeRequest request, Pipe pipe)
		{
			if (Active)
			{
				_imageRequest = null;
				try
				{
					if (request.Result.IsNative)
					{
						byte[] resultBytes = request.Result.AsByteArray;
						SetImage(System.Drawing.Image.FromStream(new MemoryStream(resultBytes, 0, resultBytes.Length, false, true)));
					}
					else
					{
						using (Stream stream = request.Result.OpenStream())
						{
							MemoryStream copyStream = new MemoryStream();
							StreamUtility.CopyStream(stream, copyStream);
							SetImage(System.Drawing.Image.FromStream(copyStream));
						}
					}
				}
				catch
				{
					SetImage(ImageUtility.GetErrorImage());
				}
			}
		}

		private void ImageError(PipeRequest request, Pipe pipe, Exception exception)
		{
			_imageRequest = null;
			SetImage(ImageUtility.GetErrorImage());
		}

	}
}
