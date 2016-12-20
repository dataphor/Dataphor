using Alphora.Dataphor.Dataphoria.Web.Core;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Extensions;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR
{
	public class WebApiApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			GlobalConfiguration.Configure(this.Configure);

			ProcessorInstance.Initialize();
		}

		public void Configure(HttpConfiguration config)
		{
			config.AddFhir();
		}

		protected void Application_End()
		{
			ProcessorInstance.Uninitialize();
		}
	}
}
