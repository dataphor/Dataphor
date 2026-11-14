using Alphora.Dataphor.DAE.REST;
using Alphora.Dataphor.Dataphoria.Web.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Mvc;

namespace Alphora.Dataphor.Dataphoria.Web.API.Controllers
{
	public class QueryController : Controller
    {
        public object Index(string e, string a = null)
        {
            var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
            
			return JsonConvert.SerializeObject(((RESTResult)result).Value);
        }
    }
}
