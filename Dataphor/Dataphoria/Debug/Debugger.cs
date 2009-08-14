using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE;
using System.Threading;

namespace Alphora.Dataphor.Dataphoria
{
	public class Debugger : IDisposable
	{
		public Debugger(IDataphoria ADataphoria)
		{
			FDataphoria = ADataphoria;
			FDataphoria.ExecuteScript(@".System.Debug.Start();");
			new Thread(new ThreadStart(DebuggerThread)).Start();
		}

		public void Dispose()
		{
			if (!FIsDisposed)
			{
				FIsDisposed = true;
				FDataphoria.ExecuteScript(@".System.Debug.Stop();");
			}
		}

		private bool FIsDisposed;
		
		private bool FIsPaused;
		
		public bool IsPaused { get { return FIsPaused; } }
		
		private int FSelectedProcessID;
		
		public int SelectedProcessID
		{
			get { return FSelectedProcessID; }
			set { FSelectedProcessID = value; }
		}
		
		private IDataphoria FDataphoria;
		
		public IDataphoria Dataphoria
		{
			get { return FDataphoria; }
		}

		public void Run()
		{
			FDataphoria.ExecuteScript(@".System.Debug.Run();");
		}

		public void Pause()
		{
			FDataphoria.ExecuteScript(@".System.Debug.Pause();");
		}

		public void DebuggerThread()
		{
			try
			{
				var LProcess = FDataphoria.DataSession.ServerSession.StartProcess(new ProcessInfo(FDataphoria.DataSession.ServerSession.SessionInfo));
				try
				{
					var LDebugBreakDelegate = new ThreadStart(delegate { DebugBreak(); });
					while (!FIsDisposed)
					{
						LProcess.Execute("Debug.WaitForBreak();", null);
						FDataphoria.Invoke(LDebugBreakDelegate);
					}
				}
				finally
				{
					FDataphoria.DataSession.ServerSession.StopProcess(LProcess);
				}
			}
			catch (Exception LException)
			{
				FDataphoria.Invoke(new ThreadStart(delegate { FDataphoria.Warnings.AppendError(null, LException, false); }));
			}
		}

		private void DebugBreak()
		{
			FIsPaused = true;
			
			var LProcesses = FDataphoria.OpenCursor(".System.Debug.GetProcesses()");
			try
			{
				int LCandidateID = -1;
				while (LProcesses.Next())
				{
					var LRow = LProcesses.Select();
					LCandidateID = (int)LRow["Process_ID"];
					if ((bool)LRow["DidBreak"])
						break;
				}
				SelectedProcessID = LCandidateID;
			}
			finally
			{
				FDataphoria.CloseCursor(LProcesses);
			}
		}
	}
}
