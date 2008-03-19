/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Xml;
using System.Web;
using System.Text;
using System.Web.Hosting;
using System.Net;
using System.IO;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Server.Customization
{
	public sealed class DilxUtility
	{
		// Do not localize
		public const string CDilxExtension = ".dilx";
		public const string CDilExtension = ".dil";
		public const string UriParameter = "uri";

		public static XmlDocument MergeAncestors(XmlDocument ADocument, Ancestors AAncestors, HttpContext AContext)
		{
			// Process any ancestors
			foreach (string LAncestor in AAncestors)
			{
				if (ADocument == null)
					ADocument = ReadAncestor(LAncestor, AContext);
				else
					ADocument = Inheritance.Merge(ADocument, ReadAncestor(LAncestor, AContext));
			}
			return ADocument;
		}

		public static string RequestDocument(string AUri, HttpContext AContext)
		{
			// TODO: Look into app request processing

			UriBuilder LFull= new UriBuilder(new Uri(AContext.Request.Url, AUri));
			LFull.Query = (LFull.Query != String.Empty ? LFull.Query.Substring(1) + "&noremap=True" : "noremap=True");
			WebRequest LRequest = WebRequest.Create(LFull.ToString());

			// hook up cookies so that the request will authenticate
			if (LRequest is HttpWebRequest)
			{
				// they have to be copied individualy because the sytem.net and system.web classes are not compatible.
				((HttpWebRequest)LRequest).CookieContainer = new CookieContainer(AContext.Request.Cookies.Count);
				foreach (String LCookieName in AContext.Request.Cookies.AllKeys)
					((HttpWebRequest)LRequest).CookieContainer.Add(LFull.Uri, new Cookie(LCookieName, AContext.Request.Cookies[LCookieName].Value, AContext.Request.Cookies[LCookieName].Path));
			}

			using (StreamReader LReader = new StreamReader(LRequest.GetResponse().GetResponseStream()))
			{
				return LReader.ReadToEnd();
			}
		}

		public static XmlDocument ReadAncestor(string AUri, HttpContext AContext)
		{
			string LResponse = RequestDocument(AUri, AContext);
			try
			{
				XmlDocument LResult = new XmlDocument();
				LResult.LoadXml(LResponse);
				return LResult;
			}
			catch (Exception AException)
			{
				throw new ServerException(ServerException.Codes.InvalidXMLDocument, AException, LResponse);
			}
		}
	}
}