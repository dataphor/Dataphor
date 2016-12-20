using Alphora.Dataphor.DAE.REST;
using Alphora.Dataphor.Dataphoria.Web.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Web.Http.Cors;
using System.Web.Mvc;

namespace Alphora.Dataphor.Dataphoria.Web.API.Controllers
{
    [Route("query")]
    [EnableCors("*", "*", "*")]
    public class QueryController : Controller
    {
        [HttpPost]
        [Route("")]
        public IActionResult Index(string e, string a = null)
        {
            var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));

            return JsonConvert.SerializeObject(((RESTResult)result).Value);
        }

   //     public IActionResult Index(string e, string a = null)
   //     {
   //         var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
            
			//return JsonConvert.SerializeObject(((RESTResult)result).Value);
   //     }
    }
}
