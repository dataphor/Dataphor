/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Debug
{
	/// <summary>
	/// Implements a Debugger for use in debugging D4 processes.
	/// </summary>
	public class Debugger : IDisposable
	{
		public Debugger(ServerSession session)
		{
			SetSession(session);
			_waitSignal = new AutoResetEvent(false);
			_pauseSignal = new ManualResetEvent(true);
			_processes = new ServerProcesses();
			_sessions = new ServerSessions();
		}

		#region IDisposable Members
		
		private bool _disposed;
		
		public void Dispose()
		{
			lock (_syncHandle)
			{
				_disposed = true;
				
				InternalRun();

				if (_sessions != null)
				{
					while (_sessions.Count > 0)
						DetachSession(_sessions[_sessions.Count - 1]);
					_sessions.Dispose();
					_sessions = null;
				}

				if (_processes != null)
				{
					while (_processes.Count > 0)
						Detach(_processes[_processes.Count - 1]);
					_processes.Dispose();
					_processes = null;
				}
				
				if (_brokenProcesses != null)
				{
					_brokenProcesses.DisownAll();
					_brokenProcesses.Dispose();
					_brokenProcesses = null;
				}
				
				if (_waitSignal != null)
				{
					_waitSignal.Set();
					_waitSignal.Close();
					_waitSignal = null;
				}
				
				if (_pauseSignal != null)
				{
					_pauseSignal.Set();
					_pauseSignal.Close();
					_pauseSignal = null;
				}
				
				SetSession(null);
			}
		}

		#endregion
		
		public int DebuggerID { get { return _session.SessionID; } }

		private ServerSession _session;
		public ServerSession Session { get { return _session; } }
		
		private void SetSession(ServerSession session)
		{
			if (_session != null)
			{
				_session.Disposed -= new EventHandler(SessionDisposed);
				_session.SetDebugger(null);
				_session = null;
			}
			
			if (session != null)
			{
				_session = session;
				_session.SetDebugger(this);
				_session.Disposed += new EventHandler(SessionDisposed);
			}
		}

		private void SessionDisposed(object sender, EventArgs args)
		{
			SetSession(null);
		}
		
		private AutoResetEvent _waitSignal;
		private ManualResetEvent _pauseSignal;
		private object _syncHandle = new object();

		private bool _isPauseRequested;
		public bool IsPauseRequested { get { return _isPauseRequested; } }
		
		private int _pausedCount;
		public bool IsPaused 
		{ 
			get 
			{ 
				lock (_syncHandle) 
				{ 
					return _isPauseRequested && (GetRunningCount() == _pausedCount); 
				} 
			} 
		}
		
		private bool _breakOnStart;
		public bool BreakOnStart
		{
			get { return _breakOnStart; }
			set { _breakOnStart = value; }
		}
		
		private bool _breakOnException;
		public bool BreakOnException
		{
			get { return _breakOnException; }
			set { _breakOnException = value; }
		}
		
		private Breakpoints _breakpoints = new Breakpoints();
		public Breakpoints Breakpoints { get { return _breakpoints; } }
		
		private ServerSessions _sessions;
		//public ServerSessions Sessions { get { return FSessions; } }
		
		private ServerProcesses _processes;
		//public ServerProcesses Processes { get { return FProcesses; } }

		private ServerProcesses _brokenProcesses = new ServerProcesses();
		//public ServerProcesses BrokenProcesses { get { return FBrokenProcesses; } }
		
		/// <summary>
		/// Stops the debugger
		/// </summary>
		public void Stop()
		{
			Dispose();
		}

		/// <summary>
		/// Attaches the debugger to a process
		/// </summary>
		public void Attach(ServerProcess process)
		{
			lock (_syncHandle)
			{
				process.SetDebuggedBy(this);
				_processes.Add(process);
			}
		}

		/// <summary>
		/// Detaches the debugger from a process.
		/// </summary>
		public void Detach(ServerProcess process)
		{
			lock (_syncHandle)
			{
				_processes.Disown(process);
				_brokenProcesses.SafeDisown(process);
				process.SetDebuggedBy(null);
			}
			
			Pulse();
		}
		
		/// <summary>
		/// Attaches the debugger to a session.
		/// </summary>
		/// <remarks>
		/// When the debugger is attached to a session, all running processes on that session
		/// are attached to the debugger. In addition, any processes subsequently started
		/// on that session are automatically attached to the debugger.
		/// </remarks>
		public void AttachSession(ServerSession session)
		{
			lock (_syncHandle)
			{
				session.SetDebuggedByID(DebuggerID);
				_sessions.Add(session);
				lock (session.Processes)
					foreach (ServerProcess process in session.Processes)
						Attach(process);
			}
		}
		
		public void DetachSession(ServerSession session)
		{
			lock (_syncHandle)
			{
				lock (session.Processes)
					foreach (ServerProcess process in session.Processes)
						if (process.DebuggedBy == this)
							Detach(process);
							
				_sessions.Disown(session);
				session.SetDebuggedByID(0);
			}
		}
		
		/// <summary>
		/// Returns the number of attached processes that are currently running.
		/// </summary>
		/// <remarks>
		/// This method assumes the sync handle has already been acquired by the calling thread.
		/// </remarks>
		private int GetRunningCount()
		{
			int runningCount = 0;
			for (int index = 0; index < _processes.Count; index++)
				if (_processes[index].IsRunning)
					runningCount++;
			return runningCount;
		}
		
		/// <summary>
		/// Waits for the debugger to pause
		/// </summary>
		public void WaitForPause(Program program, PlanNode node)
		{
			while (true)
			{
				if (IsPaused)
					return;
					
				if (_disposed)
					return;
				
				_waitSignal.WaitOne(500);
				program.Yield(node, false);
			}
		}
		
		private void InternalPause()
		{
			lock (_syncHandle)
			{
				_isPauseRequested = true;
				_pauseSignal.Reset();
			}
		}
		
		/// <summary>
		/// Initiates a pause.
		/// </summary>
		public void Pause()
		{
			InternalPause();
			_waitSignal.Set();
		}
		
		private void InternalRun()
		{
			lock (_syncHandle)
			{
				_isPauseRequested = false;
				_brokenProcesses.DisownAll();
				_pauseSignal.Set();
			}
		}
		
		/// <summary>
		/// Runs all processes.
		/// </summary>
		public void Run()
		{
			InternalRun();
		}
		
		/// <summary>
		/// Pulses the debugger to allow paused processes to check for abort due to a terminate request coming from the server.
		/// </summary>
		public void Pulse()
		{
			lock (_syncHandle)
			{
				if (_isPauseRequested)
				{
					_pauseSignal.Set();
					_pauseSignal.Reset();
				}
			}
		}
		
/*
		public void RunTo(ServerProcess AProcess, ExecutionContext AContext)
		{
		}
*/
		
		public void StepOver(int processID)
		{
			lock (_syncHandle)
			{
				if (IsPaused)
				{
					_processes.GetProcess(processID).SetStepOver();
					InternalRun();
				}
			}
		}
		
		public void StepInto(int processID)
		{
			lock (_syncHandle)
			{
				if (IsPaused)
				{
					_processes.GetProcess(processID).SetStepInto();
					InternalRun();
				}
			}
		}
		
		private bool ShouldBreak(ServerProcess process, PlanNode node)
		{
			if (_disposed)
				return false;
				
			if (process.ShouldBreak())
				return true;
				
			if (_breakpoints.Count > 0)
			{
				DebugLocator currentLocation = process.ExecutingProgram.GetCurrentLocation();
				
				// Determine whether or not a breakpoint has been hit
				for (int index = 0; index < _breakpoints.Count; index++)
				{
					Breakpoint breakpoint = _breakpoints[index];
					if 
					(
						(breakpoint.Locator == currentLocation.Locator) 
							&& (breakpoint.Line == currentLocation.Line) 
							&& ((breakpoint.LinePos == -1) || (breakpoint.LinePos == currentLocation.LinePos))
					)
						return true;
				}
			}
				
			return false;
		}
		
		private void CheckPaused()
		{
			if (!IsPaused)
				throw new ServerException(ServerException.Codes.DebuggerRunning);
		}
		
		/// <summary>
		/// Returns the list of currently debugged sessions.
		/// </summary>
		public List<DebugSessionInfo> GetSessions()
		{
			List<DebugSessionInfo> sessions = new List<DebugSessionInfo>();
			lock (_syncHandle)
			{
				foreach (ServerSession session in _sessions)
					sessions.Add(new DebugSessionInfo { SessionID = session.SessionID });
			}
			return sessions;
		}
		
		/// <summary>
		/// Returns the list of current debugged processes, with the running status and current location of each.
		/// </summary>
		public List<DebugProcessInfo> GetProcesses()
		{
			List<DebugProcessInfo> processes = new List<DebugProcessInfo>();
			lock (_syncHandle)
			{
				bool isPaused = IsPaused;
				foreach (ServerProcess process in _processes)
					processes.Add
					(
						new DebugProcessInfo
						{
							ProcessID = process.ProcessID,
							IsPaused = process.IsRunning && isPaused,
							Location = (process.IsRunning && isPaused) ? process.ExecutingProgram.SafeGetCurrentLocation() : null,
							DidBreak = _brokenProcesses.Contains(process),
							Error = (process.IsRunning && isPaused) ? process.ExecutingProgram.Stack.ErrorVar as Exception : null
						}
					);
			}
			return processes;
		}
		
		/// <summary>
		/// Returns the current call stack of a process.
		/// </summary>
		public CallStack GetCallStack(int processID)
		{
			lock (_syncHandle)
			{
				CheckPaused();
			
				ServerProcess process = _processes.GetProcess(processID);
					
				CallStack callStack = new CallStack();
				
				if (process.IsRunning)
				{
					for (int programIndex = process.ExecutingPrograms.Count - 1; programIndex > 0; programIndex--)
					{
						Program program = process.ExecutingPrograms[programIndex];
						PlanNode currentNode = program.CurrentNode;
						bool afterNode = program.AfterNode;
						
						foreach (RuntimeStackWindow window in program.Stack.GetCallStack())
						{
							callStack.Add
							(
								new CallStackEntry
								(
									callStack.Count, 
									window.Originator != null 
										? window.Originator.Description 
										: 
										(
											program.Code != null
												? program.Code.Description
												: window.Locator.Locator
										), 
									currentNode != null 
										? 
											new DebugLocator
											(
												window.Locator, 
												afterNode ? currentNode.EndLine : currentNode.Line, 
												(afterNode && currentNode.Line != currentNode.EndLine) ? currentNode.EndLinePos : currentNode.LinePos
											) 
										: new DebugLocator(window.Locator, -1, -1),
									window.Originator != null
										? DebugLocator.OperatorLocator(window.GetOriginatingOperator().DisplayName)
										: DebugLocator.ProgramLocator(program.ID),
									currentNode != null
										? currentNode.SafeEmitStatementAsString()
										: 
										(
											program.Code != null 
												? program.Code.SafeEmitStatementAsString() 
												: "<no statement available>"
										)
								)
							);
								
							currentNode = window.Originator;
							afterNode = false;
						}
					}
				}
				
				return callStack;
			}
		}
		
		public Program FindProgram(Guid programID)
		{
			lock (_syncHandle)
			{
				CheckPaused();
			
				foreach (ServerProcess process in _processes)
					foreach (Program program in process.ExecutingPrograms)
						if (program.ID == programID)
							return program;
							
				return null;
			}
		}
		
		public Program GetProgram(Guid programID)
		{
			Program program = FindProgram(programID);
			if (program == null)
				throw new ServerException(ServerException.Codes.ProgramNotFound, programID);
				
			return program;
		}
		
		/// <summary>
		/// Returns the current stack of a process within a specific window.
		/// </summary>
		public List<StackEntry> GetStack(int processID, int windowIndex)
		{
			lock (_syncHandle)
			{
				CheckPaused();
				
				ServerProcess process = _processes.GetProcess(processID);

				List<StackEntry> stack = new List<StackEntry>();
				
				if (process.IsRunning)
				{
					for (int programIndex = process.ExecutingPrograms.Count - 1; programIndex > 0; programIndex--)
					{
						Program program = process.ExecutingPrograms[programIndex];
						PlanNode currentNode = program.CurrentNode;
						// temporarily clear the debugger so we can evaluate against the target process without yielding
						Debugger debugger = program.ServerProcess.DebuggedBy;
						program.ServerProcess.SetDebuggedBy(null);					
						try
						{							
							if (windowIndex < 0)
								break;
							
							if (windowIndex < program.Stack.CallDepth)
							{
								object[] stackWindow = program.Stack.GetStack(windowIndex);
								int index;
								for (int stackWindowIndex = stackWindow.Length - 1; stackWindowIndex >= 0; stackWindowIndex--)
								{
									// reverse the index of the entries
									index = stackWindow.Length - (stackWindowIndex + 1);
									stack.Add
									(
										new StackEntry
										{
											Index = index,
											Name = String.Format("Location{0}", index),
											Type = stackWindow[stackWindowIndex] == null ? "<no value>" : stackWindow[stackWindowIndex].GetType().FullName,
											Value = stackWindow[stackWindowIndex] == null ? "<no value>" : stackWindow[stackWindowIndex].ToString()
										}
									);
								}
								
								break;
							}
							else
							{
								windowIndex -= program.Stack.CallDepth;
							}	  
						}
						finally
						{
							program.ServerProcess.SetDebuggedBy(debugger);
						}
					}
				}

				return stack;
			}
		}
		
		/// <summary>
		/// Toggles a breakpoint, returning true if the breakpoint was set, and false if it was cleared.
		/// </summary>
		/// <param name="locator">A locator identifying the document or operator in which the breakpoint is set.</param>
		/// <param name="line">The line on which the breakpoint is set.</param>
		/// <param name="linePos">The line position, -1 for no line position.</param>
		/// <returns>True if the breakpoint was set, false if it was cleared.</returns>
		public bool ToggleBreakpoint(string locator, int line, int linePos)
		{
			lock (_syncHandle)
			{
				Breakpoint breakpoint = new Breakpoint(locator, line, linePos);
				int index = _breakpoints.IndexOf(breakpoint);
				if (index >= 0)
				{
					_breakpoints.Remove(breakpoint);
					return false;
				}
				else
				{
					_breakpoints.Add(breakpoint);
					return true;
				}
			}
		}
		
		/// <summary>
		/// Yields the current program to the debugger if a breakpoint or break condition is satisfied.
		/// </summary>
		public void Yield(ServerProcess process, PlanNode node)
		{
			if (!process.IsLoading())
			{
				try
				{
					Monitor.Enter(_syncHandle);
					try
					{
						if (ShouldBreak(process, node))
						{
							_brokenProcesses.Add(process);
							InternalPause();
						}

						while (_isPauseRequested && _processes.Contains(process))
						{
							_pausedCount++;
							Monitor.Exit(_syncHandle);
							try
							{
								#if !SILVERLIGHT
								WaitHandle.SignalAndWait(_waitSignal, _pauseSignal);
								#endif
							}
							finally
							{
								Monitor.Enter(_syncHandle);
								_pausedCount--;
							}
						}
					}
					finally
					{
						Monitor.Exit(_syncHandle);
					}
				}
				catch
				{
					// Do nothing, no error should ever be thrown from here
				}
			}
		}
	}
}
