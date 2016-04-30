using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Alphora.Dataphor.Dataphoria.Web.Extensions
{
	public static class HttpRequestExtensions
	{
		public static List<Tuple<string, string>> TupledParameters(this HttpRequestMessage request)
		{
			var list = new List<Tuple<string, string>>();

			IEnumerable<KeyValuePair<string, string>> query = request.GetQueryNameValuePairs();
			foreach (var pair in query)
			{
				list.Add(new Tuple<string, string>(pair.Key, pair.Value));
			}
			return list;
		}

		public static SearchParams GetSearchParams(this HttpRequestMessage request)
		{
			var parameters = request.TupledParameters().Where(tp => tp.Item1 != "_format");
			UriParamList actualParameters = new UriParamList(parameters);
			var searchCommand = SearchParams.FromUriParamList(parameters);
			return searchCommand;
		}
	}
}