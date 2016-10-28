using Alphora.Dataphor.DAE.REST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Alphora.Dataphor.Dataphoria.Web.Controllers
{
	[RoutePrefix("data")]
	[EnableCors("*", "*", "*")]
	public class DataController : ApiController
	{
		[HttpGet, Route("{table}")]
		public JToken Get(string table)
		{
			// Libraries/Qualifiers?
			// Filter?
			// Order By?
			// Project?
			// Links?
			// Includes?
			// Paging?
			// Functions?
			var result = ProcessorInstance.Instance.Evaluate(string.Format("select Get('{0}')", table), null);
			return JsonConvert.SerializeObject(((RESTResult)result).Value);
		}

		[HttpGet, Route("{table}/{key}")]
		public JToken Get(string table, string key)
		{
			//select GetByKey('Patients', '1')
			var result = ProcessorInstance.Instance.Evaluate(string.Format("select GetByKey('{0}', '{1}')", table, key), null);
			return JsonConvert.SerializeObject(((RESTResult)result).Value);
		}

		[HttpPost, Route("{table}")]
		public void Post(string table, [FromBody]object value)
		{
			// TODO: Upsert conditions...
			ProcessorInstance.Instance.Evaluate(String.Format("Post('{0}', ARow)", table), new Dictionary<string, object> { { "ARow", value } });
		}

		[HttpPut, Route("{table}/{key}")]
		public void Put(string table, string key, [FromBody]object value)
		{
			// TODO: Concurrency using ETags...
			// TODO: if-match conditions?
			ProcessorInstance.Instance.Evaluate(String.Format("Put('{0}', '{1}', ARow)", table, key), new Dictionary<string, object> { { "ARow", value } });
		}

		[HttpPatch, Route("{table}/{key}")]
		public void Patch(string table, string key, [FromBody]object updatedValues)
		{
			// TODO: Concurrency using ETags...
			// TODO: if-match conditions?
			ProcessorInstance.Instance.Evaluate(String.Format("Patch('{0}', '{1}', ARow)", table, key), new Dictionary<string, object> { { "ARow", updatedValues } });
		}

		[HttpDelete, Route("{table}/{key}")]
		public void Delete(string table, string key)
		{
			ProcessorInstance.Instance.Evaluate(String.Format("Delete('{0}', '{1}')", table, key), null);
		}
	}
}
