using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alphora.Dataphor.DAE.REST;
using Alphora.Dataphor.Dataphoria.Web.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dataphoria.Web.API.Controllers
{
    [Route("api/[controller]")]
    public class QueryController : Controller
    {
        [HttpGet]
        public object Get(string e, string a = null)
        {
            var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));

            return JsonConvert.SerializeObject(((RESTResult)result).Value);
        }
    }
}
