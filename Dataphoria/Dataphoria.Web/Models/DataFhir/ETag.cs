using System.Net.Http.Headers;

namespace Alphora.Dataphor.Dataphoria.Web.Models.DataFhir
{
	public static class ETag
	{
		public static EntityTagHeaderValue Create(string value)
		{
			string tag = "\"" + value + "\"";
			return new EntityTagHeaderValue(tag, true);
		}
	}
}