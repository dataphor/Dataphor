using Alphora.Dataphor.Dataphoria.Web.Extensions;
using Hl7.Fhir.Model;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Alphora.Dataphor.Dataphoria.Web.Handlers
{
	public class FhirErrorMessageHandler : DelegatingHandler
	{
		protected async override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = await base.SendAsync(request, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				ObjectContent content = response.Content as ObjectContent;
				if (content != null && content.ObjectType == typeof(HttpError))
				{
					OperationOutcome outcome = new OperationOutcome().AddError(response.ReasonPhrase);
					return request.CreateResponse(response.StatusCode, outcome);
				}
			}
			return response;
		}
	}
}