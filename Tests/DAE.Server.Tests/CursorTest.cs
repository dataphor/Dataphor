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
	}
}
