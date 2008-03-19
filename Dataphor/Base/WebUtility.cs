/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;

namespace Alphora.Dataphor
{
	/// <summary> Sundry static web routines. </summary>
	public sealed class WebUtility
	{
		/// <summary> Retrieves a stream containing the result of an HTTP request for a given URI. </summary>
		/// <returns>
		///		A <see cref="Stream"/> containing the results of the request.  This stream should 
		///		be disposed (see <see cref="IDisposable"/>)	by the caller.
		///	</returns>
		public static Stream ReadStreamFromWeb(string AUri)
		{
			return WebRequest.Create(AUri).GetResponse().GetResponseStream();
		}

		/// <summary> Retrieves a string containing the textual result of an HTTP request for a given URI. </summary>
		/// <returns> The string decoded from the response of the request. </returns>
		public static string ReadStringFromWeb(string AUri)
		{
			using (StreamReader LStreamReader = new StreamReader(ReadStreamFromWeb(AUri)))
			{
				return LStreamReader.ReadToEnd();
			}
		}

		/// <summary> Sorts ampersand delimited query strings. </summary>
		/// <remarks>
		///		Sorting a query string allows for comparison with other query strings. The query
		///		strings are compared case-insensitively.
		/// </remarks>
		/// <returns> The lexically ordered query string. </returns>
		public static string SortUriQuery(string AQuery)
		{
			if (AQuery.Length > 0)
			{
				string[] LValues = AQuery.Split('&');
				Array.Sort(LValues, CaseInsensitiveComparer.Default);
				return String.Join("&", LValues);
			}
			else
				return String.Empty;
		}

		public static string SortUri(string AUri)
		{
			// Sort the query string for comparison
			int LQueryIndex = AUri.IndexOf('?');
			if (LQueryIndex >= 0)
				AUri = AUri.Substring(0, LQueryIndex + 1) + SortUriQuery(AUri.Substring(LQueryIndex + 1));
			return AUri;
		}
	}
}
