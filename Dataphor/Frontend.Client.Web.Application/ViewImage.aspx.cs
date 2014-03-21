/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Net;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client.Web;

namespace Alphora.Dataphor.Frontend.Client.Web.Application
{
	public class ViewImage : System.Web.UI.Page
	{
		public const string ContentLengthHeader = "Content-Length";

		public Web.Session WebSession;
		
		public void WriteImage()
		{
			WebSession = (Web.Session)Session["WebSession"];

			Response.ClearContent();
			Response.BufferOutput = false;

			string imageID = Request.QueryString["ImageID"];
			if ((imageID != null) && (imageID != String.Empty))
			{
				using (Stream stream = WebSession.ImageCache.Read(imageID))
				{
					Response.AppendHeader(ContentLengthHeader, stream.Length.ToString());
					StreamUtility.CopyStream(stream, Response.OutputStream);
				}
			}
			else
			{
				imageID = Request.QueryString["HandlerID"];
				if ((imageID != null) && (imageID != String.Empty))
				{
					LoadImageHandler handler = WebSession.ImageCache.GetImageHandler(imageID);
					if (handler != null)
						handler(Context, imageID, Response.OutputStream);
				}
			}
		}
	}
}