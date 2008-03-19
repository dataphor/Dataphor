/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Web;

namespace Alphora.Dataphor.Frontend.Server
{
	/// <summary> Handles requests for images located in the Frontend Server resources. </summary>
	/// <remarks>
	///		The images which are referenced from the built-in derivation pages are routed to
	///		this handler to eliminate the necessity of these files in every derived application.
	/// </remarks>
	public class ImageHandler : IHttpHandler
	{
		// Do not localize
		public const string CImageNamespace = "Alphora.Dataphor.Frontend.Server.Images.{0}.gif";
		public const string CContentLengthHeader = "Content-Length";
		public const string CContentType = "image/gif";

		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext AContext)
		{ 
			AContext.Response.ClearContent();
			AContext.Response.ContentType = CContentType;
			AContext.Response.BufferOutput = false;

			// Strip off the extension and prefix
			string LFileName = Path.ChangeExtension(AContext.Request.Url.AbsolutePath, String.Empty);
			LFileName = Path.GetExtension(LFileName.Substring(0, LFileName.Length - 1)).Substring(1).ToLower();

			// Copy the image data from the resource into the response
			using (Stream LSource = GetType().Assembly.GetManifestResourceStream(String.Format(CImageNamespace, LFileName)))
			{
				if (LSource == null)
					throw new ServerException(ServerException.Codes.DerivationImageNotFound, LFileName); 
				AContext.Response.AppendHeader(CContentLengthHeader, LSource.Length.ToString());
				StreamUtility.CopyStream(LSource, AContext.Response.OutputStream);
			}

			AContext.Response.End();
		}
	}
}