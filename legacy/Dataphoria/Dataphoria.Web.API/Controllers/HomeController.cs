using System.Web.Mvc;

namespace Alphora.Dataphor.Dataphoria.Web.API.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			ViewBag.Title = "Home Page";

			return View();
		}
	}
}
