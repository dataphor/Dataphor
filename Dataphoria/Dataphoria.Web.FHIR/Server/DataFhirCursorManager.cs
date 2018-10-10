using Alphora.Dataphor.DAE;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Server
{
	public class DataFhirCursorManager
	{
		IServer _server;

		public DataFhirCursorManager(IServer server) 
		{
			_server = server;
		}

		public DataFhirCursor AcquireCursor(String type, IEnumerable<KeyValuePair<String, String>> searchParameters)
		{
			var queryBuilder = DataFhirQueryBuilder.ForType(type).ForSearch(searchParameters);

			// TODO: Session management
			var session = _server.Connect(new SessionInfo());
			var process = session.StartProcess(new DAE.ProcessInfo());
			var cursor = process.OpenCursor(queryBuilder.ToString(), DataFhirMarshal.ToDataParams(process, queryBuilder.Arguments));
			var result = new DataFhirCursor(new DataFhirCursorKey(),  cursor);
			return result;
		}

		public void ReleaseCursor(DataFhirCursor cursor)
		{
			cursor.Close();
		}
	}
}