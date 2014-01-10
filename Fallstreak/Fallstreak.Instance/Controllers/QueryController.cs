using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using Alphora.Dataphor.DAE.NativeCLI;
using Fallstreak.Processing;
using Newtonsoft.Json.Linq;

namespace Fallstreak.Instance.Controllers
{
    public class QueryController : Controller
    {
        public JsonNetResult Index(string e, string a = null)
        {
            var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
            return new JsonNetResult { Data = JsonInterop.NativeResultToJson((NativeResult)result), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}
