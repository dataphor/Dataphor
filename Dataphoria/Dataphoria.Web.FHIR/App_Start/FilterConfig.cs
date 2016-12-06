using System.Web.Mvc;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}
