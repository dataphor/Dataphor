using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.Dataphoria.Web.Controllers
{
    public class DataController : ApiController
    {
		[HttpGet, Route("{table}")]
		public JsonNetResult Get(string table)
		{
			// Filter?
			// Order By?
			// Project?
			// Links?
			// Includes?
			// Paging?
			// Functions?
            var result = ProcessorInstance.Instance.Evaluate(String.Format("select {0}", table), null);
            return new JsonNetResult { Data = JsonInterop.NativeResultToJson((NativeResult)result), JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet };
		}

		[HttpGet, Route("{table}/{key}")]
		public JsonNetResult Get(string table, string key)
		{
			// How to determine the key?
			throw new NotImplementedException();
		}

		[HttpPost, Route("{table}")]
		public void Post(string table, [FromBody]dynamic value)
		{
			// Construct an insert statement
			throw new NotImplementedException();
		}

		[HttpPut, Route("{table}/{key}")]
		public void Put(string table, string key, [FromBody]dynamic value)
		{
			// Construct an update statement
			// How to test concurrency?
			throw new NotImplementedException();
		}

		[HttpPatch, Route("{table}/{key}")]
		public void Patch(string table, string key, [FromBody]dynamic updatedValues)
		{
			// Construct an update statement
			// How to test concurrency?
			throw new NotImplementedException();
		}

		[HttpDelete, Route("{table}/{key}")]
		public void Delete(string table, string key)
		{
			// Construct a delete statement
			// How to determine the key?
			throw new NotImplementedException();
		}
    }
}
