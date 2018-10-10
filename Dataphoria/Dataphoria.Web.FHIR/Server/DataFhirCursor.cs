using Alphora.Dataphor.DAE;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Server
{
	public class DataFhirCursor
	{
		DataFhirCursorKey _key;
		IServerCursor _cursor;

		public DataFhirCursor(DataFhirCursorKey key, IServerCursor cursor)
		{
			_key = key;
			_cursor = cursor;
		}

		public Bundle GetNextPage(int pageSize)
		{
			var bundle = new Bundle();

			var rowCount = 0;
			var row = _cursor.Plan.RequestRow();
			try
			{
				_cursor.Plan.Process.BeginTransaction(IsolationLevel.Browse);
				try
				{
					while (_cursor.Next())
					{
						_cursor.Select(row);
						var resource = row["Content"] as Resource;
						bundle.AddResourceEntry(resource, resource.ResourceIdentity().ToString());
						rowCount++;

						if (rowCount >= pageSize)
						{
							break;
						}
					}
				}
				finally
				{
					_cursor.Plan.Process.CommitTransaction();
				}
			}
			finally
			{
				_cursor.Plan.ReleaseRow(row);
			}

			// TODO: Set bundle.NextLink using __cursorId as a parameter
			bundle.Total = rowCount;

			return bundle;
		}

		public Resource GetResource()
		{
			var row = _cursor.Plan.RequestRow();
			try
			{
				_cursor.Select(row);
				return row["Content"] as Resource;
			}
			finally
			{
				_cursor.Plan.ReleaseRow(row);
			}
		}

		public void Close()
		{
			if (_cursor != null)
			{
				var session = _cursor.Plan.Process.Session;
				_cursor.Plan.Process.CloseCursor(_cursor);
				session.Server.Disconnect(session);
				_cursor = null;
			}
		}
	}
}