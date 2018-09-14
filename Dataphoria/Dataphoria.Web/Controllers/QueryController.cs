using Alphora.Dataphor.DAE.REST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Mvc;

namespace Alphora.Dataphor.Dataphoria.Web.Controllers
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
