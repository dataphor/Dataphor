using Alphora.Dataphor.Dataphoria.Web.ExceptionHandling;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Alphora.Dataphor.Dataphoria.Web.Filters
{
	public class FhirExceptionFilter : ExceptionFilterAttribute
	{
		private readonly IExceptionResponseMessageFactory exceptionResponseMessageFactory;

		public FhirExceptionFilter(IExceptionResponseMessageFactory exceptionResponseMessageFactory)
		{
			this.exceptionResponseMessageFactory = exceptionResponseMessageFactory;
		}

		public override void OnException(HttpActionExecutedContext context)
		{
			HttpResponseMessage response = exceptionResponseMessageFactory.GetResponseMessage(context.Exception, context.Request);

			throw new HttpResponseException(response);
		}
	}
}