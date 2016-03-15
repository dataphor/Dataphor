using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using Alphora.Dataphor.DAE.NativeCLI;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Dataphoria.Web.Controllers
{
	[RoutePrefix("datafhir")]
	[EnableCors("*", "*", "*")]
	public class DataFhirController : ApiController
	{
		[HttpGet, Route("{type}")]
		public JToken Search(string type)
		{
			var searchparams = Request.GetSearchParams();
			//int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
			//string sortby = Request.GetParameter(FhirParameter.SORT);

			if (type == "Appointment")
			{
				var patientSearch = searchparams.Parameters.FirstOrDefault(p => p.Item1.ToLower() == "patient");
				var practitionerSearch = searchparams.Parameters.FirstOrDefault(p => p.Item1.ToLower() == "practitioner");
				if (patientSearch != null)
				{
					var appointments = ProcessorInstance.Instance.Evaluate(String.Format("select GetPatientAppointments('{0}')", patientSearch.Item2), null);
					return JToken.Parse(FhirSerializer.SerializeToJson(ToBundle((NativeResult)appointments)));
					//return JsonInterop.NativeResultToJson((NativeResult)result);
				}
				else if (practitionerSearch != null)
				{
					var appointments = ProcessorInstance.Instance.Evaluate(String.Format("select GetPractitionerAppointments('{0}')", practitionerSearch.Item2), null);
					return JToken.Parse(FhirSerializer.SerializeToJson(ToBundle((NativeResult)appointments)));
					//return JsonInterop.NativeResultToJson((NativeResult)result);
				}
				else
				{
					return FhirSerializer.SerializeToJson(Error("Appointment search is only supported for patients and practitioners."));
				}
			}
			else
			{
				return FhirSerializer.SerializeToJson(Error(String.Format("Searching is not currently supported for resources of type {0}.", type)));
			}
		}

		private Bundle ToBundle(NativeResult resources)
		{
			var resourceList = resources.Value as NativeListValue;
			if (resourceList != null)
			{
				var result = new Bundle();
				result.Type = Bundle.BundleType.Searchset;
				result.Total = resourceList.Elements.Length;
				foreach (var resource in resourceList.Elements)
				{
					result.AddResourceEntry(((NativeScalarValue)resource).Value as Resource, null);
				}

				return result;
			}

			throw new InvalidOperationException("Expected a list of resources.");
		}

		private OperationOutcome Error(string errorMessage)
		{
			var result = new OperationOutcome();
			var issue = 
				new OperationOutcome.IssueComponent 
				{ 
					Severity = OperationOutcome.IssueSeverity.Error, 
					Details = new CodeableConcept { Text = errorMessage } 
				};
			result.Issue.Add(issue);
			result.Text = new Narrative { Div = issue.Details.Text };
			return result;
		}

		[HttpPost, Route("{type}/_search")]
		public JToken SearchWithOperator(string type)
		{
			// todo: get tupled parameters from post.
			return Search(type);
		}

		//[HttpGet, Route("{table}")]
		//public JToken Get(string table)
		//{
		//	// Libraries/Qualifiers?
		//	// Filter?
		//	// Order By?
		//	// Project?
		//	// Links?
		//	// Includes?
		//	// Paging?
		//	// Functions?
		//	var result = ProcessorInstance.Instance.Evaluate(String.Format("select Get('{0}')", table), null);
		//	return JsonInterop.NativeResultToJson((NativeResult)result);
		//}

		//[HttpGet, Route("{table}/{key}")]
		//public JToken Get(string table, string key)
		//{
		//	var result = ProcessorInstance.Instance.Evaluate(String.Format("select GetByKey('{0}', '{1}')", table, key), null);
		//	return JsonInterop.NativeResultToJson((NativeResult)result);
		//}

		//[HttpPost, Route("{table}")]
		//public void Post(string table, [FromBody]object value)
		//{
		//	// TODO: Upsert conditions...
		//	ProcessorInstance.Instance.Evaluate(String.Format("Post('{0}', ARow)", table), new Dictionary<string, object> { { "ARow", value } });
		//}

		//[HttpPut, Route("{table}/{key}")]
		//public void Put(string table, string key, [FromBody]object value)
		//{
		//	// TODO: Concurrency using ETags...
		//	// TODO: if-match conditions?
		//	ProcessorInstance.Instance.Evaluate(String.Format("Put('{0}', '{1}', ARow)", table, key), new Dictionary<string, object> { { "ARow", value } });
		//}

		//[HttpPatch, Route("{table}/{key}")]
		//public void Patch(string table, string key, [FromBody]object updatedValues)
		//{
		//	// TODO: Concurrency using ETags...
		//	// TODO: if-match conditions?
		//	ProcessorInstance.Instance.Evaluate(String.Format("Patch('{0}', '{1}', ARow)", table, key), new Dictionary<string, object> { { "ARow", updatedValues } });
		//}

		//[HttpDelete, Route("{table}/{key}")]
		//public void Delete(string table, string key)
		//{
		//	ProcessorInstance.Instance.Evaluate(String.Format("Delete('{0}', '{1}')", table, key), null);
		//}
	}
}