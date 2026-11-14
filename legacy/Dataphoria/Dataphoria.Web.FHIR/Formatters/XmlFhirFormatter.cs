using Alphora.Dataphor.Dataphoria.Web.FHIR.ExceptionHandling;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Extensions;
using Alphora.Dataphor.Dataphoria.Web.FHIR.Models;
using Alphora.Dataphor.Dataphoria.Web.Core.Utilities;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Task = System.Threading.Tasks.Task;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Formatters
{
	public class XmlFhirFormatter : FhirMediaTypeFormatter
	{
		public XmlFhirFormatter() : base()
		{
			foreach (var mediaType in ContentType.XML_CONTENT_HEADERS)
			{
				SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
			}
		}

		public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
		{
			base.SetDefaultContentHeaders(type, headers, mediaType);
			headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(type, ResourceFormat.Xml);
		}

		public override bool CanWriteType(Type type)
		{
			return typeof(Resource).IsAssignableFrom(type) || type == typeof(FhirResponse);
		}

		public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
		{
			return Task.Factory.StartNew<object>(() =>
			{
				try
				{
					var body = base.ReadBodyFromStream(readStream, content);

					if (type == typeof(Bundle))
					{
						if (XmlSignatureHelper.IsSigned(body))
						{
							if (!XmlSignatureHelper.VerifySignature(body))
								throw ExceptionHandling.Error.BadRequest("Digital signature in body failed verification");
						}
					}

					if (typeof(Resource).IsAssignableFrom(type))
					{
						Resource resource = FhirParser.ParseResourceFromXml(body);
						return resource;
					}
					else
						throw ExceptionHandling.Error.Internal("The type {0} expected by the controller can not be deserialized", type.Name);
				}
				catch (FormatException exc)
				{
					throw ExceptionHandling.Error.BadRequest("Body parsing failed: " + exc.Message);
				}
			});
		}

		public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
		{

			return Task.Factory.StartNew(() =>
			{
				XmlWriter writer = new XmlTextWriter(writeStream, new UTF8Encoding(false));
				var summary = requestMessage.RequestSummary();

				if (type == typeof(OperationOutcome))
				{
					Resource resource = (Resource)value;
					FhirSerializer.SerializeResource(resource, writer, summary);
				}
				else if (type.IsAssignableFrom(typeof(Resource)))
				{
					Resource resource = (Resource)value;
					FhirSerializer.SerializeResource(resource, writer, summary);

					content.Headers.ContentLocation = resource.ExtractKey().ToUri();
				}
				else if (type == typeof(FhirResponse))
				{
					FhirResponse response = (value as FhirResponse);
					if (response.HasBody)
						FhirSerializer.SerializeResource(response.Resource, writer, summary);
				}

				writer.Flush();
			});
		}
	}
}