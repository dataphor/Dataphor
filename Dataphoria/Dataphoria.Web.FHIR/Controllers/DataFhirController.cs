/*
	Alphora Dataphor
	© Copyright 2000-2018 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using Alphora.Dataphor.Dataphoria.Web.FHIR.Extensions;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Models;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Server;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Controllers
{
	[RoutePrefix("datafhir")]
	[EnableCors("*", "*", "*")]
	public class DataFhirController : ApiController
	{
		private DataFhirCursorManager _cursorManager;
		private int _defaultPageSize = 20;

		public DataFhirController() : base()
		{
			// TODO: Inject
			_cursorManager = DataFhirServerManager.CursorManager;
		}

		#region public API

		[HttpGet, Route("{type}")]
		public FhirResponse Search([FromUri]string type)
		{
			var cursor = _cursorManager.AcquireCursor(type, Request.GetQueryNameValuePairs());
			try
			{
				var pageSize = Request.GetParameterAsIntWithDefault("_count", _defaultPageSize);
				var bundle = cursor.GetNextPage(pageSize);
				return Respond.WithBundle(bundle);
			}
			finally
			{
				_cursorManager.ReleaseCursor(cursor);
			}
		}

		[HttpGet, Route("{type}/{id}")]
		public FhirResponse Get([FromUri]string type, [FromUri]string id)
		{
			var searchParameters = new Dictionary<String, String>();
			searchParameters.Add("_id", id);
			var cursor = _cursorManager.AcquireCursor(type, searchParameters);
			try
			{
				return Respond.WithResource(cursor.GetResource());
			}
			finally
			{
				_cursorManager.ReleaseCursor(cursor);
			}
		}

		[HttpPost, Route("{table}")]
		public void Post(string table, [FromBody]object value)
		{
			// TODO: Upsert conditions...
			//ProcessorInstance.Instance.Evaluate(String.Format("Post('{0}', ARow)", table), new Dictionary<string, object> { { "ARow", value } });
		}

		[HttpPut, Route("{table}/{key}")]
		public void Put(string table, string key, [FromBody]object value)
		{
			// TODO: Concurrency using ETags...
			// TODO: if-match conditions?
			//ProcessorInstance.Instance.Evaluate(String.Format("Put('{0}', '{1}', ARow)", table, key), new Dictionary<string, object> { { "ARow", value } });
		}

		[HttpPatch, Route("{table}/{key}")]
		public void Patch(string table, string key, [FromBody]object updatedValues)
		{
			// TODO: Concurrency using ETags...
			// TODO: if-match conditions?
			//ProcessorInstance.Instance.Evaluate(String.Format("Patch('{0}', '{1}', ARow)", table, key), new Dictionary<string, object> { { "ARow", updatedValues } });
		}

		[HttpDelete, Route("{table}/{key}")]
		public void Delete(string table, string key)
		{
			//ProcessorInstance.Instance.Evaluate(String.Format("Delete('{0}', '{1}')", table, key), null);
		}

		#endregion
	}
}