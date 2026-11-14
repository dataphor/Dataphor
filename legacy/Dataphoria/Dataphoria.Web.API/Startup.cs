using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Alphora.Dataphor.Dataphoria.Web.API.Startup))]

namespace Alphora.Dataphor.Dataphoria.Web.API
{
	public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
			Core.Startup.ConfigureAuth(app);
        }
    }
}
