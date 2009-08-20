using System;
using System.Diagnostics;
using System.Threading;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.Logging;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> Asyncronously executes queries. </summary>
	/// <remarks> Should not be used multiple times (discard and create another instance). </remarks>
	internal class ScriptExecutor : Object
	{
		static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof(ScriptExecutor));
		public const int CStopTimeout = 10000; // ten seconds to synchronously stop

		private bool FIsRunning = false;
		private IServerProcess FProcess;
		private int FProcessID;
		private Thread FAsyncThread;

		private IServerSession FSession;
		private string FScript;
		private ExecuteFinishedHandler FExecuteFinished;
		private ReportScriptProgressHandler FExecuteProgress;
		private DebugLocator FLocator;
		
		public ScriptExecutor
		(
			IServerSession ASession, 
			String AScript, 
			ReportScriptProgressHandler AExecuteProgress,
			ExecuteFinishedHandler AExecuteFinished,
			DebugLocator ALocator
		)
		{
			FSession = ASession;
			FScript = AScript;
			FExecuteFinished = AExecuteFinished;
			FExecuteProgress = AExecuteProgress;
			FLocator = ALocator;
		}

		public bool IsRunning
		{
			get { return FIsRunning; }
		}

		public void Start()
		{
			if (!FIsRunning)
			{
				CleanupProcess();
				FProcess = FSession.StartProcess(new ProcessInfo(FSession.SessionInfo));
				FProcessID = FProcess.ProcessID;
				FIsRunning = true;
				FAsyncThread = new Thread(ExecuteAsync);
				FAsyncThread.Start();
			}
		}

		private void CleanupProcess()
		{
			if (FProcess != null)
			{
				try
				{
					FSession.StopProcess(FProcess);
				}
				catch(Exception LException)
				{
					SRFLogger.WriteLine(TraceLevel.Error, "Exception at CleanupProcess {0}", LException);
					// Don't rethrow, the session may have already been stopped
				}
				FProcess = null;
				FProcessID = 0;
			}
		}

		public void Stop()
		{
			if (FIsRunning)
			{
				FIsRunning = false;

				// Asyncronously request that the server process be stopped
				new AsyncStopHandler(AsyncStop).BeginInvoke(FProcessID, FSession, null, null);
				FProcess = null;
				FProcessID = 0;
			}
		}

		private void AsyncStop(int AProcessID, IServerSession ASession)
		{
			IServerProcess LProcess = ASession.StartProcess(new ProcessInfo(FSession.SessionInfo));
			try
			{
				LProcess.Execute("StopProcess(" + AProcessID + ")", null);
			}
			finally
			{
				ASession.StopProcess(LProcess);
			}
		}

		private void ExecuteAsync()
		{
			try
			{
				ErrorList LErrors = null;
				TimeSpan LElapsed = TimeSpan.Zero;

				try
				{
					ScriptExecutionUtility.ExecuteScript
					(
						FProcess,
						FScript,
						ScriptExecuteOption.All,
						out LErrors,
						out LElapsed,
						(AStatistics, AResults) => 
							Session.SafelyInvoke
							(
								new ReportScriptProgressHandler(AsyncProgress),
								new object[] {AStatistics, AResults}
							),
						FLocator
					);
				}
				finally
				{
					Session.SafelyInvoke(new ExecuteFinishedHandler(AsyncFinish), new object[] {LErrors, LElapsed});
				}
			}
			catch(Exception LException)
			{
				SRFLogger.WriteLine(TraceLevel.Error, "Exception at ExecuteAsync {0}", LException);
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private void AsyncProgress(PlanStatistics AStatistics, string AResults)
		{
			// Return what results we got even if stopped.
			FExecuteProgress(AStatistics, AResults);
		}

		private void AsyncFinish(ErrorList AErrors, TimeSpan AElapsedTime)
		{
			CleanupProcess();
			if (FIsRunning)
				FExecuteFinished(AErrors, AElapsedTime);
		}

		#region Nested type: AsyncStopHandler

		private delegate void AsyncStopHandler(int AProcessID, IServerSession ASession);

		#endregion
	}

	[Flags]
	public enum ExportType
	{
		Data = 1,
		Schema = 2
	}

	internal delegate void ExecuteFinishedHandler(ErrorList AErrors, TimeSpan AElapsedTime);
}