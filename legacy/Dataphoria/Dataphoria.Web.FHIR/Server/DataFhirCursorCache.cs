using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Server
{
	public class DataFhirCursorCache
	{
		Dictionary<DataFhirCursorKey, DataFhirCursor> _cache = new Dictionary<DataFhirCursorKey, DataFhirCursor>();


	}
}