/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;
using System.Net;
using System.IO;
using System.Xml;
using System.Text;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.Frontend.Server.Customization
{
	/// <summary> Saves a DIL or DILX file </summary>
	public class Save : IHttpHandler
	{
		public const string CAddLinkMap = "addlinkmap";

		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext AContext) 
		{ 
			string LUri = HttpUtility.UrlDecode(AContext.Request.QueryString[DilxUtility.UriParameter]);
			if(LUri == null) 
				throw new ServerException(ServerException.Codes.MissingParameter, DilxUtility.UriParameter);
			string LTemp = AContext.Request.MapPath(new Uri(LUri).LocalPath);

			// TODO: Authenticate save request (security, or at least make sure we aren't overwriting a dynamic page)

			switch (Path.GetExtension(LTemp).ToLower())
			{
				case DilxUtility.CDilxExtension :
					// write to a memory stream first, then write to the file.
					using (MemoryStream LMemoryStream = new MemoryStream()) 
					{
						Process(AContext.Request.InputStream, LMemoryStream, AContext);
						using (FileStream LStream = new FileStream(LTemp, FileMode.Create, FileAccess.Write))
						{
							LMemoryStream.Position = 0;
							StreamUtility.CopyStream(LMemoryStream, LStream);
						}
					}
					break;
				case DilxUtility.CDilExtension :
					using (FileStream LStream = new FileStream(LTemp, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						StreamUtility.CopyStream(AContext.Request.InputStream, LStream);
					}
					break;
				default :
					throw new ServerException(ServerException.Codes.UnknownExtension, Path.GetExtension(LTemp));
			}

			LTemp = AContext.Request.QueryString[CAddLinkMap];
			if ((LTemp != null) && (LTemp != String.Empty))
			{
				LTemp = new Uri(LTemp).PathAndQuery.Remove(0, AContext.Request.ApplicationPath.Length + 1);
				LUri = new Uri(LUri).PathAndQuery.Remove(0, AContext.Request.ApplicationPath.Length + 1);
				((ApplicationServer)AContext.ApplicationInstance).AddLinkMap(WebUtility.SortUri(HttpUtility.UrlDecode(LTemp)), LUri);
			}
		}

		public void Process(Stream ASource, Stream ATarget, HttpContext AContext)
		{
			DilxDocument LDocument = new DilxDocument();
			LDocument.Read(ASource);

			// Prepare ancestors
			XmlDocument LMergedAncestors = DilxUtility.MergeAncestors(null, LDocument.Ancestors, AContext);

			// Prepare current
			XmlDocument LCurrent = new XmlDocument();
			LCurrent.Load(LDocument.Content);

			//Perform the diff
			LDocument.Content.SetLength(0);
			Inheritance.Diff(LMergedAncestors, LCurrent).Save(LDocument.Content);
			LDocument.Write(ATarget);
		}
	}

	public class GetRaw : IHttpHandler
	{
		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext AContext)
		{ 
			AContext.Response.ContentType = FrontendUtility.CXmlContentType;

			string LUri = HttpUtility.UrlDecode(AContext.Request.QueryString[DilxUtility.UriParameter]);
			if(LUri == null) 
				throw new ServerException(ServerException.Codes.MissingParameter, DilxUtility.UriParameter);
			LUri = AContext.Request.MapPath(new Uri(LUri).LocalPath);

			// Validate that the request is for a DILX document
			if (String.Compare(Path.GetExtension(LUri), DilxUtility.CDilxExtension, true) != 0)
				throw new ServerException(ServerException.Codes.UnknownExtension, Path.GetExtension(LUri));

			// Read the DILX file
			using (FileStream LStream = new FileStream(LUri, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				StreamUtility.CopyStream(LStream, AContext.Response.OutputStream);
			}

			AContext.Response.End();
		}
	}

	public class GetLineage : IHttpHandler
	{
		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext AContext)
		{ 
			AContext.Response.ContentType = FrontendUtility.CXmlContentType;

			// Read the DILX content
			DilxDocument LDocument = new DilxDocument();
			LDocument.Read(AContext.Request.InputStream);

			// Prepare ancestors
			XmlDocument LMergedAncestors = DilxUtility.MergeAncestors(null, LDocument.Ancestors, AContext);

			// Write DIL
			LMergedAncestors.Save(AContext.Response.OutputStream);

			AContext.Response.End();
		}
	}
}
