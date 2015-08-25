using System;
using System.Diagnostics;
using System.Threading;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> Asyncronously executes queries. </summary>
	/// <remarks> Should not be used multiple times (discard and create another instance). </remarks>
	internal class ScriptExecutor : Object
	{
		public const int StopTimeout = 10000; // ten seconds to synchronously stop

		private bool _isRunning = false;
		private IServerProcess _process;
		private int _processID;
		private Thread _asyncThread;

		private IServerSession _session;
		private string _script;
		private QueryLanguage _language;
		private ExecuteFinishedHandler _executeFinished;
		private ReportScriptProgressHandler _executeProgress;
		private DebugLocator _locator;
		
		public ScriptExecutor
		(
			IServerSession session, 
			String script, 
			QueryLanguage language,
			ReportScriptProgressHandler executeProgress,
			ExecuteFinishedHandler executeFinished,
			DebugLocator locator
		)
		{
			_session = session;
			_script = script;
			_language = language;
			_executeFinished = executeFinished;
			_executeProgress = executeProgress;
			_locator = locator;
		}

		public bool IsRunning
		{
			get { return _isRunning; }
		}

		public void Start()
		{
			if (!_isRunning)
			{
				CleanupProcess();
				var processInfo = new ProcessInfo(_session.SessionInfo);
				processInfo.Language = _language;
				_process = _session.StartProcess(processInfo);
				_processID = _process.ProcessID;
				_isRunning = true;
				_asyncThread = new Thread(ExecuteAsync);
				_asyncThread.Start();
			}
		}

		private void CleanupProcess()
		{
			if (_process != null)
			{
				try
				{
					_session.StopProcess(_process);
				}
				catch
				{
					// Don't rethrow, the session may have already been stopped
				}
				_process = null;
				_processID = 0;
			}
		}

		public void Stop()
		{
			if (_isRunning)
			{
				_isRunning = false;

				// Asyncronously request that the server process be stopped
				new AsyncStopHandler(AsyncStop).BeginInvoke(_processID, _session, null, null);
				_process = null;
				_processID = 0;
			}
		}

		private void AsyncStop(int processID, IServerSession session)
		{
			IServerProcess process = session.StartProcess(new ProcessInfo(_session.SessionInfo));
			try
			{
				process.Execute("StopProcess(" + processID + ")", null);
			}
			finally
			{
				session.StopProcess(process);
			}
		}

		private void ExecuteAsync()
		{
			try
			{
				ErrorList errors = null;
				TimeSpan elapsed = TimeSpan.Zero;

				try
				{
					ScriptExecutionUtility.ExecuteScript
					(
						_process,
						_script,
						ScriptExecuteOption.All,
						out errors,
						out elapsed,
						(AStatistics, AResults) => 
							Session.SafelyInvoke
							(
								new ReportScriptProgressHandler(AsyncProgress),
								new object[] {AStatistics, AResults}
							),
						_locator
					);
				}
				finally
				{
					Session.SafelyInvoke(new ExecuteFinishedHandler(AsyncFinish), new object[] {errors, elapsed});
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private void AsyncProgress(PlanStatistics statistics, string results)
		{
			// Return what results we got even if stopped.
			_executeProgress(statistics, results);
		}

		private void AsyncFinish(ErrorList errors, TimeSpan elapsedTime)
		{
			CleanupProcess();
			if (_isRunning)
				_executeFinished(errors, elapsedTime);
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