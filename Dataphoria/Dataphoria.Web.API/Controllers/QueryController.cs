using Alphora.Dataphor.DAE.REST;
using Alphora.Dataphor.Dataphoria.Web.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Dataphoria.Web.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        [HttpGet]
        public object Get([FromQuery] string e, [FromQuery] string a = null)
        {
            var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
            
			return JsonConvert.SerializeObject(((RESTResult)result).Value);
        }
    }
}
