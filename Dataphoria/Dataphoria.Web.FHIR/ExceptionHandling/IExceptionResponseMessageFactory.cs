using System;
using System.Net.Http;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.ExceptionHandling
{
	public interface IExceptionResponseMessageFactory
	{
		HttpResponseMessage GetResponseMessage(Exception exception, HttpRequestMessage reques);
	}
}