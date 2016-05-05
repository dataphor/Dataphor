using System;

namespace Alphora.Dataphor.Dataphoria.Web.Models.DataFhir
{
	public interface ILocalhost
	{
		Uri DefaultBase { get; }
		Uri Absolute(Uri uri);
		bool IsBaseOf(Uri uri);
		Uri GetBaseOf(Uri uri);
	}
}