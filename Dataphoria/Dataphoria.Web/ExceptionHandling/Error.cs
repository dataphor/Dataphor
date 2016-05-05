using Alphora.Dataphor.Dataphoria.Web.Models.DataFhir;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Net;

namespace Alphora.Dataphor.Dataphoria.Web.ExceptionHandling
{
	public static class Error
	{
		public static DataFhirException Create(HttpStatusCode code, string message, params object[] values)
		{
			return new DataFhirException(code, message, values);
		}

		public static DataFhirException BadRequest(string message, params object[] values)
		{
			return new DataFhirException(HttpStatusCode.BadRequest, message, values);
		}

		public static DataFhirException NotFound(string message, params object[] values)
		{
			return new DataFhirException(HttpStatusCode.NotFound, message, values);
		}

		public static DataFhirException NotFound(IKey key)
		{
			if (key.VersionId == null)
			{
				return NotFound("No {0} resource with id {1} was found.", key.TypeName, key.ResourceId);
			}
			else
			{
				return NotFound("There is no {0} resource with id {1}, or there is no version {2}", key.TypeName, key.ResourceId, key.VersionId);
			}
		}

		public static DataFhirException NotAllowed(string message)
		{
			return new DataFhirException(HttpStatusCode.Forbidden, message);
		}

		public static DataFhirException Internal(string message, params object[] values)
		{
			return new DataFhirException(HttpStatusCode.InternalServerError, message, values);
		}

		public static DataFhirException NotSupported(string message, params object[] values)
		{
			return new DataFhirException(HttpStatusCode.NotImplemented, message, values);
		}

		private static OperationOutcome.IssueComponent CreateValidationResult(string details, IEnumerable<string> location)
		{
			return new OperationOutcome.IssueComponent()
			{
				Severity = OperationOutcome.IssueSeverity.Error,
				Code = OperationOutcome.IssueType.Invalid,
				Details = new CodeableConcept("http://hl7.org/fhir/issue-type", "invalid"),
				Diagnostics = details,
				Location = location
			};
		}
	}
}