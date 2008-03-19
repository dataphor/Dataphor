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

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class ViewImage : System.Web.UI.Page
	{
		public const string CContentLengthHeader = "Content-Length";

		public Web.Session WebSession;
		
		public void WriteImage()
		{
			WebSession = (Web.Session)Session["WebSession"];

			Response.ClearContent();
			Response.BufferOutput = false;

			string LImageID = Request.QueryString["ImageID"];
			if ((LImageID != null) && (LImageID != String.Empty))
			{
				using (Stream LStream = WebSession.ImageCache.Read(LImageID))
				{
					Response.AppendHeader(CContentLengthHeader, LStream.Length.ToString());
					StreamUtility.CopyStream(LStream, Response.OutputStream);
				}
			}
			else
			{
				LImageID = Request.QueryString["HandlerID"];
				if ((LImageID != null) && (LImageID != String.Empty))
				{
					LoadImageHandler LHandler = WebSession.ImageCache.GetImageHandler(LImageID);
					if (LHandler != null)
						LHandler(Context, LImageID, Response.OutputStream);
				}
			}
		}
	}

	/// <summary> Maintains and caches images. </summary>
	/// <remarks> There are two methods for working with the ImageCache:
	///		<list type="bullet">
	///		<item><term>Allocation/Deallocate</term><description>This method caches the images when they are first read.</description></item>
	///		<item><term>RegisterImageHandler/UnregisterImageHandler</term><description>This method does not cache the images, but rather passes the request to a specified handler.</description></item>
	///		</list>
	/// </remarks>
	public class ImageCache
	{
		public const string CCacheFileExtension = ".dfc";

		/// <summary> Instantiates a new DocumentCache. </summary>
		/// <param name="ACachePath"> Specifies the path to use to store cached items.  </param>
		public ImageCache(string ACachePath, Session ASession)
		{
			// Prepare the folder
			Directory.CreateDirectory(ACachePath);
			FCachePath = ACachePath;
			FSession = ASession;
			FIdentifiers = new Hashtable();
		}

		public void Dispose()
		{
			if (FIdentifiers != null)
			{
				string LFileName;
				foreach (DictionaryEntry LEntry in FIdentifiers)
				{
					LFileName = BuildFileName((string)LEntry.Key);
					if (File.Exists(LFileName))
						File.Delete(LFileName);
				}
				FIdentifiers = null;
			}
		}

		// FImageHandlers

		private Hashtable FImageHandlers = new Hashtable();

		public LoadImageHandler GetImageHandler(string AID)
		{
			return (LoadImageHandler)FImageHandlers[AID];
		}

		public string RegisterImageHandler(LoadImageHandler AHandler)
		{
			string LID = Session.GenerateID();
			FImageHandlers[LID] = AHandler;
			return LID;
		}

		public void UnregisterImageHandler(string AID)
		{
			FImageHandlers.Remove(AID);
		}

		// Allocate / Deallocate

		private Hashtable FIdentifiers;

		private string FCachePath;
		public string CachePath { get { return FCachePath; } }

		private Session FSession;

		private string BuildFileName(string AImageID)
		{
			return String.Format("{0}\\{1}{2}", FCachePath, AImageID, CCacheFileExtension);
		}

		private string GetImageID()
		{
			return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('/', '_').Replace('+', '~');
		}

		public string Allocate(string AImageExpression)
		{
			if (AImageExpression != String.Empty)
			{
				string LImageID = GetImageID();
				FIdentifiers.Add(LImageID, AImageExpression);
				return LImageID;
			}
			else
				return String.Empty;
		}

		public string Allocate(GetImageHandler AGetImage)
		{
			if (AGetImage != null)
			{
				string LImageID = GetImageID();
				FIdentifiers.Add(LImageID, AGetImage);
				return LImageID;
			}
			else
				return String.Empty;
		}

		public void Deallocate(string AImageID)
		{
			if (AImageID != String.Empty)
			{
				string LFileName = BuildFileName(AImageID);
				if (File.Exists(LFileName))
					File.Delete(LFileName);
				FIdentifiers.Remove(AImageID);
			}
		}

		public Stream Read(string AImageID)
		{
			string LFileName = BuildFileName(AImageID);
			if (File.Exists(LFileName))
				return File.OpenRead(LFileName);
			else
			{
				FileStream LFileStream = new FileStream(LFileName, FileMode.CreateNew, FileAccess.ReadWrite);
				try
				{
					object LValue = FIdentifiers[AImageID];
					if (LValue is String)
					{
						using (DAE.Runtime.Data.Scalar LScalar = (DAE.Runtime.Data.Scalar)FSession.Pipe.RequestDocument((string)LValue))
							StreamUtility.CopyStream(LScalar.OpenStream(), LFileStream);
					}
					else
						((GetImageHandler)LValue)(AImageID, LFileStream); 
					LFileStream.Position = 0;
					return LFileStream;
				}
				catch
				{
					LFileStream.Close();
					if (File.Exists(LFileName))
						File.Delete(LFileName);
					throw;
				}
			}
		}
	}

	public delegate void GetImageHandler(string AImageID, Stream AStream);

	public delegate void LoadImageHandler(HttpContext AContext, string AID, Stream AStream);

}