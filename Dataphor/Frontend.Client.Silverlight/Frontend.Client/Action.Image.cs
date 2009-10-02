/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client
{
	public partial class Action
	{
		// Image

		public event EventHandler OnImageChanged;

		private string FImage = String.Empty;
		[DefaultValue("")]
		[Description("An image used by this action's controls as an icon.")]
		public string Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					if (Active)
						InternalUpdateImage();
				}
			}
		}

		private ImageSource FLoadedImage;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public ImageSource LoadedImage
		{
			get { return FLoadedImage; }
		}

		private void SetImage(ImageSource AValue)
		{
			FLoadedImage = AValue;
			if (OnImageChanged != null)
				OnImageChanged(this, EventArgs.Empty);
		}

		private PipeRequest FImageRequest;

		private void CancelImageRequest()
		{
			if (FImageRequest != null)
			{
				HostNode.Pipe.CancelRequest(FImageRequest);
				FImageRequest = null;
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
					FImageRequest = new PipeRequest(Image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
					HostNode.Pipe.QueueRequest(FImageRequest);
				}
			}
			else
				SetImage(null);
		}

		private void ImageRead(PipeRequest ARequest, Pipe APipe)
		{
			if (Active)
			{
				FImageRequest = null;
				try
				{
					if (ARequest.Result.IsNative)
					{
						byte[] LResultBytes = ARequest.Result.AsByteArray;
						var LSource = new BitmapImage();
						LSource.SetSource(new MemoryStream(LResultBytes, 0, LResultBytes.Length, false, true));
						SetImage(LSource);
					}
					else
					{
						using (Stream LStream = ARequest.Result.OpenStream())
						{
							MemoryStream LCopyStream = new MemoryStream();
							StreamUtility.CopyStream(LStream, LCopyStream);
							BitmapImage LImage = new BitmapImage();
							LImage.SetSource(LCopyStream);
							SetImage(LImage);
						}
					}
				}
				catch
				{
					SetImage(ImageUtility.GetErrorImage());
				}
			}
		}

		private void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			FImageRequest = null;
			SetImage(ImageUtility.GetErrorImage());
		}

	}
}
