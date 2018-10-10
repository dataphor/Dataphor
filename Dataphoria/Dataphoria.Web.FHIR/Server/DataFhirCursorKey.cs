using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Server
{
	// TODO: Using this as a key for now, but need to associate with user context
	// SECURITY: Need to make sure that the cursorId is only retrievable by the requesting user
	public class DataFhirCursorKey
	{
		private static int _nextId = 0;

		private static int GetNextId()
		{
			return Interlocked.Increment(ref _nextId);
		}

		public DataFhirCursorKey() 
		{
			_cursorId = GetNextId();
		}
		
		int _cursorId = 0;

		public override bool Equals(object other) 
		{
			return (other as DataFhirCursorKey)?._cursorId == _cursorId; 
		}

		public override int GetHashCode()
		{
			return _cursorId.GetHashCode();
		}
	}
}