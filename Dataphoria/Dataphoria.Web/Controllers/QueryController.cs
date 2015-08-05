using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.Dataphoria.Web;
using Alphora.Dataphor.Dataphoria.Processing;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Dataphoria.Web.Controllers
{
    public class QueryController : Controller
    {
        public object Index(string e, string a = null)
        {
            var result = ProcessorInstance.Instance.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
            return JsonInterop.NativeResultToJson((NativeResult)result);
        }
    }
}
