using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using Alphora.Dataphor.DAE.NativeCLI;
using Newtonsoft.Json.Linq;

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
			var result = ProcessorInstance.Instance.Evaluate(String.Format("select Get('{0}')", table), null);
			return JsonInterop.NativeResultToJson((NativeResult)result);
		}

		[HttpGet, Route("{table}/{key}")]
		public JToken Get(string table, string key)
		{
			var result = ProcessorInstance.Instance.Evaluate(String.Format("select GetByKey('{0}', '{1}')", table, key), null);
			return JsonInterop.NativeResultToJson((NativeResult)result);
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
