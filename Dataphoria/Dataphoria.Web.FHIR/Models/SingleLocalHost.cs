using Alphora.Dataphor.Dataphoria.Web.FHIR.Utilities;
using System;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Models
{
	public class Localhost : ILocalhost
	{
		public Uri DefaultBase { get; set; }

		public Localhost(Uri baseuri)
		{
			this.DefaultBase = baseuri;
		}

		public Uri Absolute(Uri uri)
		{
			if (uri.IsAbsoluteUri)
			{
				return uri;
			}
			else
			{
				string _base = DefaultBase.ToString().TrimEnd('/') + "/";
				string path = uri.ToString();
				return new Uri(_base + uri);
			}
		}

		public bool IsBaseOf(Uri uri)
		{
			if (uri.IsAbsoluteUri)
			{
				bool isbase = DefaultBase.Bugfixed_IsBaseOf(uri);
				return isbase;
			}
			else
			{
				return false;
			}

		}

		public Uri GetBaseOf(Uri uri)
		{
			return (this.IsBaseOf(uri)) ? this.DefaultBase : null;
		}
	}
}