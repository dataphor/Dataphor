using Alphora.Dataphor.DAE.REST;
using Alphora.Dataphor.Dataphoria.Web.Core;
using Alphora.Dataphor.Dataphoria.Web.Core.Extensions;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Models;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Controllers
{
	[RoutePrefix("datafhir")]
	[EnableCors("*", "*", "*")]
	public class DataFhirController : ApiController
	{
		#region public API

		[HttpPost, Route("")]
		public FhirResponse Add([FromBody]Resource resource)
		{
			switch (resource.GetType().FullName)
			{
				case "Hl7.Fhir.Model.Patient":
					Patient patient = (Patient)resource;

					//insert Resource
					var id = Guid.NewGuid().ToString();
					var insertClause = BuildResourceInsertClause(resource);
					var parameters = BuildResourceInsertParams(id, resource, "Patient");
					var d4Result = ProcessorInstance.Instance.Evaluate(insertClause, parameters);

					//insert Patient
					insertClause = BuildPatientInsertClause(patient);
					parameters = BuildPatientInsertParams(id, patient);
					d4Result = ProcessorInstance.Instance.Evaluate(insertClause, parameters);
					return null;
			}
			return null;
		}

		[HttpDelete, Route("{type}/{id}")]
		public FhirResponse Delete([FromUri]string type, [FromUri]string id)
		{
			IEnumerable<KeyValuePair<string, object>> parameters;
			object d4Result;

			switch (type)
			{
				case "patient":
					parameters = BuildPatientDeleteParams(id);
					d4Result = ProcessorInstance.Instance.Evaluate("delete Patients where ResourceId = AResourceId", parameters);

					break;
			}

			parameters = BuildResourceDeleteParams(id);
			d4Result = ProcessorInstance.Instance.Evaluate("delete Resources where Id = AId", parameters);
			return null;
		}

		[HttpGet, Route("{type}")]
		public FhirResponse Search([FromUri]string type)
		{
			var searchParameters = Request.NativeListedParameters();
			var whereClause = ProcessorInstance.Instance.BuildWhereClause(searchParameters);
			var bundle = new Bundle();

			switch (type)
			{
				case "condition":


					break;
				case "patient":
					var d4Result = ProcessorInstance.Instance.Evaluate(string.Format("select FHIR.Server.FHIRServerPatientView {0}", whereClause), searchParameters);
					var rows = (IEnumerable<object>)((RESTResult)d4Result).Value;
					
					foreach (var row in rows)
					{
						// TODO: ["ResourceContent"] is the column that returns the JSON manifestation of the content.  But is that a hack??
						var patient = ((Dictionary<string, object>)row)["ResourceContent"];

						// this cast only works because we are functioning "in process"
						// the NativeCLI doesn't *really* know how to serialize this object
						var convertedPatient = (Patient)patient;
						bundle.AddResourceEntry(convertedPatient, new Uri("patient/" + convertedPatient.Id, UriKind.Relative).ToString());
					}

					break;
			}
			return Respond.WithBundle(bundle);
		}

		#endregion

		#region private methods

		private string BuildResourceInsertClause(Resource resource)
		{
			var clause = @"insert table
			{
				row
				{
					AId Id,
					AType Type,
					AContent Content,
					ANumber Number,
					ADate Date,
					AString String,
					AToken Token,
					AReference Reference,
					AComposite Composite,
					AQuantity Quantity,
					AUri Uri
				}
			}
			into Resources;";

			return clause;
		}

		private IEnumerable<KeyValuePair<string, object>> BuildResourceInsertParams(string id, Resource resource, string type)
		{
			var parameters = new Dictionary<string, object>
			{
				{ "AId", id },
				{ "AType", type },
				{ "AContent", resource },
				{ "ANumber", "" },
				{ "ADate", DateTime.Now.ToString() },
				{ "AString", "" },
				{ "AToken", "" },
				{ "AReference", "" },
				{ "AComposite", "" },
				{ "AQuantity", "" },
				{ "AUri", "" }
			};
			
			return parameters;
		}

		private IEnumerable<KeyValuePair<string, object>> BuildResourceDeleteParams(string id)
		{
			var parameters = new Dictionary<string, object>
			{
				{ "AId", id }
			};

			return parameters;
		}

		private string BuildPatientInsertClause(Patient patient)
		{
			var clause = @"insert table
			{
				row
				{
					AResourceId ResourceId,
					AActive Active,
					AAddressLine Address,
					AAddressCity AddressCity,
					AAddressCountry AddressCountry,
					AAddressPostalCode AddressPostalCode,
					AAddressState AddressState,
					ABirthdate Birthdate,
					ADeceased Deceased,
					AEmail Email,
					ANameFamily Family,
					AGender Gender,
					ANameGiven Given,
					ALanguage Language,
					AName Name,
					APhone Phone
				}
			} into Patients;";

			return clause;
		}

		private IEnumerable<KeyValuePair<string, object>> BuildPatientInsertParams(string id, Patient patient)
		{
			var parameters = new Dictionary<string, object>
			{
				{ "AResourceId", id },
				{ "AActive", true },
				{ "AAddressLine", "" },
				{ "AAddressCity", "" },
				{ "AAddressCountry", "" },
				{ "AAddressPostalCode", "" },
				{ "AAddressState", "" },
				{ "ABirthdate", "" },
				{ "ADeceased", false },
				{ "AEmail", "" },
				{ "ANameFamily", "" },
				{ "AGender", "" },
				{ "ANameGiven", "" },
				{ "ALanguage", "" },
				{ "AName", "" },
				{ "APhone", "" }
			};

			return parameters;
		}

		private IEnumerable<KeyValuePair<string, object>> BuildPatientDeleteParams(string id)
		{
			var parameters = new Dictionary<string, object>
			{
				{ "AResourceId", id }
			};

			return parameters;
		}

		#endregion
	}
}