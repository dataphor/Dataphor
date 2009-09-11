/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;

namespace Alphora.Dataphor.DAE.Connection
{
	public class MSSQLServerCursor : DotNetCursor
	{
		public MSSQLServerCursor(MSSQLCommand ACommand, IDataReader ACursor) : base(ACommand, ACursor) { }

		protected override void Dispose(bool ADisposing)
		{
			if (FCursor != null)
			{
				// If most recent open or fetch indicates EOF, then the server cursor is already closed.  In order to determine this, 
				//  however, we must get the result param which can only be obtained after closing the current reader.  Additionally,
				//  if the current reader is from the open, we must fetch the server cursor handle so that we have a handle to close.
				CloseAndProcessFetchResult();

				if (!FEOF)
				{
					// The most recent open or fetch did not indicate EOF, so the server cursor remains open.  Use "our" connection 
					//  to close the server cursor now that the latest reader is closed.
					if (FCursorHandle != 0)
					{
						((MSSQLCommand)FCommand).CloseServerCursor(FCursorHandle);
						FCursorHandle = 0;
					}
				}
			}

			base.Dispose(ADisposing);
		}

		private int FCursorHandle;
		private bool FEOF;	// Indicates that the end of the current fetch cursor is know to be the end of the entire dataset

		protected override bool InternalNext()
		{
			bool LGotRow = FCursor.Read();
			if (!LGotRow)
			{
				// Either we are at the end of the data set or we are just at the end of the current fetch buffer.  We must close the
				//  current reader so that we can get the output params and determine the server cursor handle and the EOF flag.
				CloseAndProcessFetchResult();

				// The EOF flag has been updated, so we can tell whether or not to proceed with a fetch
				if (!FEOF)
				{
					FCursor = ((MSSQLCommand)FCommand).FetchServerCursor(FCursorHandle);
					FRecord = (IDataRecord)FCursor;

					// Attempt to read the first row
					LGotRow = FCursor.Read();
				}
			}
			return LGotRow;
		}

		/// <summary> Closes the current reader and examines the EOF and server cursor handle output parameters. </summary>
		private void CloseAndProcessFetchResult()
		{
			if (FCursor != null)
			{
				FCursor.Dispose();
				FCursor = null;
			}

			// Indicate to the command that now is the time to read any output (user) parameters
			MSSQLCommand LCommand = (MSSQLCommand)FCommand;
			LCommand.ParametersAreAvailable();	

			// Determine if there are additional fetches to do, or if this is it
			if (LCommand.ContainsParameter("@retval"))
				switch ((int)LCommand.GetParameterValue("@retval"))
				{
					case 16: FEOF = true; break;
					case 0: FEOF = false; break;
					default: throw new ConnectionException(ConnectionException.Codes.UnableToOpenCursor);
				}
			
			// If we have not yet obtained the cursor, retrieve it (should only happen after reading the row of the open cursor call).
			if ((FCursorHandle == 0) && LCommand.ContainsParameter("@cursor"))
				FCursorHandle = (int)LCommand.GetParameterValue("@cursor");
		}

/*
		TODO: Implement cursor updates?

		protected override void InternalInsert(string[] ANames, object[] AValues)
		{
		}

		protected override void InternalUpdate(string[] ANames, object[] AValues)
		{
		}

		protected override void InternalDelete()
		{
		}
*/
	}

	public enum SQLServerCursorScrollOptions : int
	{
		Keyset = 0x0001,
		Dynamic = 0x0002,
		ForwardOnly = 0x0004,
		Static = 0x0008,
		FastForward = 0x0010,
		Parameterized = 0x1000,
		AutoFetch = 0x2000,
		AutoClose = 0x4000,
		CheckAcceptable = 0x8000,
		KeysetAcceptable = 0x10000,
		DynamicAcceptable = 0x20000,
		ForwardOnlyAcceptable = 0x40000,
		StaticAcceptable = 0x80000,
		FastForwardAcceptable = 0x100000
	}

	public enum SQLServerCursorConcurrencyOptions : int
	{
		ReadOnly = 0x0001,
		ScrollLocks = 0x0002,
		OptimisticTimeStamps = 0x0004,
		OptimisticValues = 0x0008,
		OpenOnAnySQL = 0x2000,
		OpenKeysetInPlace = 0x4000,
		ReadOnlyAcceptable = 0x10000,
		LocksAcceptable = 0x20000,
		OptimisticAcceptable = 0x40000
	}
}
