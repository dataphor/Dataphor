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
						using (Row LRow = LCursor.Select())
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
						using (Row LRow = LCursor.Select())
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
					
					using (Row LRow = LCursor.Select())
					{
						LRow[0] = -1;
						LCursor.Update(LRow);
					}
					
					using (Row LRow = LCursor.Select())
					{
						if ((int)LRow[0] != -1)
							throw new Exception("Update failed");
					}
					
					LCursor.Delete();
					
					using (Row LRow = LCursor.Select())
					{
						if ((int)LRow[0] != 1)
							throw new Exception("Delete failed");
							
						LRow[0] = 2;
						LCursor.Insert(LRow);
					}
					
					using (Row LRow = LCursor.Select())
					{
						if ((int)LRow[0] != 2)
							throw new Exception("Insert failed");
					}
					
					LCursor.Reset();
					LCounter = 0;
					Guid LBookmark = Guid.Empty;
					
					while (LCursor.Next())
					{
						using (Row LRow = LCursor.Select())
						{
							LCounter++;
							if (LCounter == 5)
								LBookmark = LCursor.GetBookmark();
						}
					}
					
					if (!LCursor.GotoBookmark(LBookmark, true))
						throw new Exception("GotoBookmark failed");
						
					using (Row LRow = LCursor.Select())
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
						using (Row LRow = LCursor.Select())
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
						using (Row LRow = LCursor.Select())
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
	}
}
