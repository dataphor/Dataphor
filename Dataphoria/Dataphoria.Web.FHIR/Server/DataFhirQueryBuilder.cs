using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Server
{
	public class DataFhirQueryBuilder
	{
		public static DataFhirQueryBuilder ForType(String type)
		{
			return new DataFhirQueryBuilder(type);
		}

		public DataFhirQueryBuilder(String type)
		{
			_type = type;
		}

		private String _type;
		private IEnumerable<KeyValuePair<String, String>> _searchParams;
		private Dictionary<String, Object> _arguments = new Dictionary<String, Object>();
		public IEnumerable<KeyValuePair<String, Object>> Arguments
		{
			get 
			{
				return _arguments;
			}
		}

		public DataFhirQueryBuilder ForSearch(IEnumerable<KeyValuePair<String, String>> searchParams)
		{
			_searchParams = searchParams;
			return this;
		}

		private void AddParameter(StringBuilder query, String columnName, String parameterName, String parameterValue)
		{
			if (query.Length > 0)
			{
				query.Append(" and ");
			}

			query.AppendFormat("{0} = {1}");
			_arguments.Add(parameterName, parameterValue);
		}

		public override String ToString()
		{
			StringBuilder query = new StringBuilder();

			// Returns a string to select resources of the given type with the given search parameters
			if (_type != null)
			{
				query.Append("Type = _type");
				_arguments.Add("_type", _type);
			}

			if (_searchParams != null)
			{
				foreach (var param in _searchParams)
				{
					switch (param.Key)
					{
						case "_id": AddParameter(query, "Id", "_id", param.Value); break;
						// TODO: More params....
					}
				}
			}

			if (query.Length > 0)
			{
				query.Insert(0, "select FHIR.Server.Resource where ");
			}

			return query.ToString();
		}
	}
}