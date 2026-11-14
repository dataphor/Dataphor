using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client.Tests.CLI
{
	[TestFixture]
	public class CLITest : OutOfProcessTestFixture
	{
		[Test]
		public void TestCLI()
		{
			IServerProcess LProcess = DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.SessionInfo));
			try
			{
				var LFetchCount = DataSession.ServerSession.SessionInfo.FetchCount;
				LProcess.Execute("create table Test { ID : Integer, key { ID } };", null);
				LProcess.Execute(String.Format("for var LIndex := 1 to {0} do insert row {{ LIndex ID }} into Test;", LFetchCount.ToString()), null);
				
				IServerCursor LCursor = LProcess.OpenCursor("select Test order by { ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable }", null);
				try
				{
					var LCounter = 0;
					while (LCursor.Next())
					{
						using (IRow LRow = LCursor.Select())
						{
							LCounter += (int)LRow[0];
						}
					}
					
					if (LCounter != (LFetchCount * (LFetchCount + 1)) / 2)
						throw new Exception("Fetch count summation failed");
						
					LCursor.Reset();
					LCounter = 0;

					while (LCursor.Next())
					{
						using (IRow LRow = LCursor.Select())
						{
							LCounter++;
							if (LCounter != (int)LRow[0])
								throw new Exception("Select failed");
						}
					}
					
					LCursor.Reset();
					LCounter = 0;
					
					LCursor.Next();
					LCursor.Next();
					
					using (IRow LRow = LCursor.Select())
					{
						LRow[0] = -1;
						LCursor.Update(LRow);
					}
					
					using (IRow LRow = LCursor.Select())
					{
						if ((int)LRow[0] != -1)
							throw new Exception("Update failed");
					}
					
					LCursor.Delete();
					
					using (IRow LRow = LCursor.Select())
					{
						if ((int)LRow[0] != 1)
							throw new Exception("Delete failed");
							
						LRow[0] = 2;
						LCursor.Insert(LRow);
					}
					
					using (IRow LRow = LCursor.Select())
					{
						if ((int)LRow[0] != 2)
							throw new Exception("Insert failed");
					}
					
					LCursor.Reset();
					LCounter = 0;
					Guid LBookmark = Guid.Empty;
					
					while (LCursor.Next())
					{
						using (IRow LRow = LCursor.Select())
						{
							LCounter++;
							if (LCounter == 5)
								LBookmark = LCursor.GetBookmark();
						}
					}
					
					if (!LCursor.GotoBookmark(LBookmark, true))
						throw new Exception("GotoBookmark failed");
						
					using (IRow LRow = LCursor.Select())
					{
						if ((int)LRow[0] != 5)
							throw new Exception("GotoBookmark failed");
					}
					
					LCursor.DisposeBookmark(LBookmark);
				}
				finally
				{
					LProcess.CloseCursor(LCursor);
				}

				LProcess.Execute("delete Test;", null);
				LFetchCount *= 10;
				LProcess.Execute(String.Format("for var LIndex := 1 to {0} do insert row {{ LIndex ID }} into Test;", LFetchCount.ToString()), null);
				
				LCursor = LProcess.OpenCursor("select Test order by { ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable }", null);
				try
				{
					var LCounter = 0;
					while (LCursor.Next())
					{
						using (IRow LRow = LCursor.Select())
						{
							LCounter += (int)LRow[0];
						}
					}
					
					if (LCounter != (LFetchCount * (LFetchCount + 1)) / 2)
						throw new Exception("Fetch count summation failed");
						
					LCursor.Reset();
					LCounter = 0;

					while (LCursor.Next())
					{
						using (IRow LRow = LCursor.Select())
						{
							LCounter++;
							if (LCounter != (int)LRow[0])
								throw new Exception("Select failed");
						}
					}
				}
				finally
				{
					LProcess.CloseCursor(LCursor);
				}
			}
			finally
			{
				DataSession.ServerSession.StopProcess(LProcess);
			}
		}
		
		[Test]
		public void TestDeleteAtEOF()
		{
			IServerProcess LProcess = DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.SessionInfo));
			try
			{
				var LFetchCount = DataSession.ServerSession.SessionInfo.FetchCount;
				LProcess.Execute("create table TestDeleteAtEOF { ID : Integer, Name : String, key { ID } };", null);
				LProcess.Execute("insert row { 1 ID, 'Joe' Name } into TestDeleteAtEOF;", null);
				LProcess.Execute("insert row { 2 ID, 'John' Name } into TestDeleteAtEOF;", null);
				
				IServerCursor LCursor = LProcess.OpenCursor("select TestDeleteAtEOF browse by { ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse", null);
				try
				{
					var LRow = LCursor.Plan.RequestRow();
					try
					{
						LCursor.Next();
						LCursor.Next();
						LCursor.Delete();
						if (LCursor.EOF() && !LCursor.Prior())
							throw new Exception("Delete At EOF failed");
					}
					finally
					{
						LCursor.Plan.ReleaseRow(LRow);
					}
					
				}
				finally
				{
					LProcess.CloseCursor(LCursor);
				}
			}
			finally
			{
				DataSession.ServerSession.StopProcess(LProcess);
			}
		}

		[Test]
		public void TestDeleteAtBOF()
		{
			IServerProcess LProcess = DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.SessionInfo));
			try
			{
				var LFetchCount = DataSession.ServerSession.SessionInfo.FetchCount;
				LProcess.Execute("create table TestDeleteAtBOF { ID : Integer, Name : String, key { ID } };", null);
				LProcess.Execute("insert row { 1 ID, 'Joe' Name } into TestDeleteAtBOF;", null);
				LProcess.Execute("insert row { 2 ID, 'John' Name } into TestDeleteAtBOF;", null);
				
				IServerCursor LCursor = LProcess.OpenCursor("select TestDeleteAtBOF browse by { ID } capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable } isolation browse", null);
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
							throw new Exception("Delete At BOF failed");
					}
					finally
					{
						LCursor.Plan.ReleaseRow(LRow);
					}
					
				}
				finally
				{
					LProcess.CloseCursor(LCursor);
				}
			}
			finally
			{
				DataSession.ServerSession.StopProcess(LProcess);
			}
		}
	}
}
