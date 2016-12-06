using Hl7.Fhir.Model;
using System;
using System.Net;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.ExceptionHandling
{
	public class DataFhirException : Exception
	{
		public HttpStatusCode StatusCode;
		public OperationOutcome Outcome { get; set; }

		public DataFhirException(HttpStatusCode statuscode, string message = null) : base(message)
		{
			this.StatusCode = statuscode;
		}

		public DataFhirException(HttpStatusCode statuscode, string message, params object[] values)
			: base(string.Format(message, values))
		{
			this.StatusCode = statuscode;
		}

		public DataFhirException(string message) : base(message)
		{
			this.StatusCode = HttpStatusCode.BadRequest;
		}

		public DataFhirException(HttpStatusCode statuscode, string message, Exception inner) : base(message, inner)
		{
			this.StatusCode = statuscode;
		}

		public DataFhirException(HttpStatusCode statuscode, OperationOutcome outcome, string message = null)
			: this(statuscode, message)
		{
			this.Outcome = outcome;
		}
	}
}