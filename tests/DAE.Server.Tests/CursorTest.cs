/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>Deleting the last or the first row of a browse cursor, recovering from the crack it leaves, and leaving a cursor open at the end of a process.</summary>
	/// <remarks>
	/// The same two cases, against the same expressions and with the same assertions, were also in
	/// tests/DAE.Client.Tests/CLI/CLITest.cs, over a connection to a server in another program. That project was deleted
	/// (test-client-server-cursors) and these were not duplicated: what they establish is cursor-at-boundary behaviour, not
	/// wire behaviour, and in process is the lowest layer that shows it. The one case of that project worth having over the
	/// wire - a cursor over more rows than the client buffers at a time - is
	/// <see cref="OutOfProcessProtocolTest.ACursorCrossesFetchBatchesAndChangesThroughItself"/>.
	/// </remarks>
	[TestFixture]
	public class CursorTest : InProcessTestFixture
	{
		[Test]
		public void TestDeleteAtEOF()
		{
			ExecuteScript("create table TestDeleteAtEOF { ID : Integer, Name : String, key { ID } };");
			ExecuteScript("insert row { 1 ID, 'Joe' Name } into TestDeleteAtEOF;");
			ExecuteScript("insert row { 2 ID, 'John' Name } into TestDeleteAtEOF;");
			
			IServerCursor LCursor = Process.OpenCursor("select TestDeleteAtEOF browse by { ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse", null);
			try
			{
				var LRow = LCursor.Plan.RequestRow();
				try
				{
					LCursor.Next();
					LCursor.Next();
					LCursor.Delete();
					if (LCursor.EOF() && !LCursor.Prior())
						throw new Exception("Delete At EOF Failed");
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				Process.CloseCursor(LCursor);
			}
		}

		[Test]
		public void TestDeleteAtBOF()
		{
			ExecuteScript("create table TestDeleteAtBOF { ID : Integer, Name : String, key { ID } };");
			ExecuteScript("insert row { 1 ID, 'Joe' Name } into TestDeleteAtBOF;");
			ExecuteScript("insert row { 2 ID, 'John' Name } into TestDeleteAtBOF;");
			
			IServerCursor LCursor = Process.OpenCursor("select TestDeleteAtBOF browse by { ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse", null);
			try
			{
				var LRow = LCursor.Plan.RequestRow();
				try
				{
					LCursor.Last();
					LCursor.Prior();
					LCursor.Prior();
					LCursor.Delete();
					if (LCursor.BOF() && !LCursor.Next())
						throw new Exception("Delete At BOF Failed");
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				Process.CloseCursor(LCursor);
			}
		}

		/// <summary>
		/// A script that opens a D4 cursor and never closes it. The cursor belongs to the session's cursor manager,
		/// which the session disposes after its processes, so the cursor used to be disposed with the process that
		/// opened it already torn down - and a cursor holds a copy of that process's stack values, which it releases
		/// through that process's managers, so releasing them there dereferenced the process's nulled session. The
		/// process now closes its own cursors while it is still usable
		/// (bug-a-cursor-copies-the-stack-and-never-gives-it-back).
		/// </summary>
		/// <remarks>
		/// The assertion is the fixture's teardown, which stops the process and disconnects the session: an exception
		/// from either fails the test. The body asserts nothing itself, and the Binary is what gives the cursor's
		/// snapshot a value to release.
		/// </remarks>
		[Test]
		public void ACursorLeftOpenAtTheEndOfAProcessDoesNotFailTheTeardown()
		{
			ExecuteScript
			(
				@"
					begin
						var LB : Binary := Binary('00FF');
						var LC : cursor(table { ID : Integer }) := cursor(table { row { 1 ID } });
					end;
				"
			);
		}
	}
}
