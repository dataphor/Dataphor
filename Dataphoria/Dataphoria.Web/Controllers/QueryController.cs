using Alphora.Dataphor.DAE.REST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Cors;


namespace Alphora.Dataphor.Dataphoria.Web.Controllers
{
    [RoutePrefix("query")]
    [EnableCors("*", "*", "*")]
    public class QueryController : ApiController
    {
        [HttpPost, Route("{query}")]
        public HttpResponseMessage Get([FromBody]string query)
        {
            var result = ProcessorInstance.Instance.Evaluate(query, null);
            var temp = JsonConvert.SerializeObject(((RESTResult)result).Value);

            var res = Request.CreateResponse(System.Net.HttpStatusCode.OK);
            res.Content = new StringContent(temp, Encoding.UTF8, "application/json");
            return res;
        }
    }
}
