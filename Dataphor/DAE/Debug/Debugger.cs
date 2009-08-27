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
		public Debugger(ServerSession ASession)
		{
			SetSession(ASession);
			FWaitSignal = new AutoResetEvent(false);
			FPauseSignal = new ManualResetEvent(true);
			FProcesses = new ServerProcesses();
			FSessions = new ServerSessions();
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (FSessions != null)
			{
				while (FSessions.Count > 0)
					DetachSession(FSessions[FSessions.Count - 1]);
				FSessions.Dispose();
				FSessions = null;
			}

			if (FProcesses != null)
			{
				while (FProcesses.Count > 0)
					Detach(FProcesses[FProcesses.Count - 1]);
				FProcesses.Dispose();
				FProcesses = null;
			}
			
			if (FBrokenProcesses != null)
			{
				FBrokenProcesses.DisownAll();
				FBrokenProcesses.Dispose();
				FBrokenProcesses = null;
			}
			
			if (FWaitSignal != null)
			{
				FWaitSignal.Close();
				FWaitSignal = null;
			}
			
			if (FPauseSignal != null)
			{
				FPauseSignal.Close();
				FPauseSignal = null;
			}
			
			SetSession(null);
		}

		#endregion
		
		public int DebuggerID { get { return FSession.SessionID; } }

		private ServerSession FSession;
		public ServerSession Session { get { return FSession; } }
		
		private void SetSession(ServerSession ASession)
		{
			if (FSession != null)
			{
				FSession.Disposed -= new EventHandler(SessionDisposed);
				FSession.SetDebugger(null);
				FSession = null;
			}
			
			if (ASession != null)
			{
				FSession = ASession;
				FSession.SetDebugger(this);
				FSession.Disposed += new EventHandler(SessionDisposed);
			}
		}

		private void SessionDisposed(object ASender, EventArgs AArgs)
		{
			SetSession(null);
		}
		
		private AutoResetEvent FWaitSignal;
		private ManualResetEvent FPauseSignal;
		private object FSyncHandle = new object();

		private bool FIsPauseRequested;
		public bool IsPauseRequested { get { return FIsPauseRequested; } }
		
		private int FPausedCount;
		public bool IsPaused 
		{ 
			get 
			{ 
				lock (FSyncHandle) 
				{ 
					return FIsPauseRequested && (GetRunningCount() == FPausedCount); 
				} 
			} 
		}
		
		private bool FBreakOnException;
		public bool BreakOnException
		{
			get { return FBreakOnException; }
			set { FBreakOnException = value; }
		}
		
		private Breakpoints FBreakpoints = new Breakpoints();
		public Breakpoints Breakpoints { get { return FBreakpoints; } }
		
		private ServerSessions FSessions;
		//public ServerSessions Sessions { get { return FSessions; } }
		
		private ServerProcesses FProcesses;
		//public ServerProcesses Processes { get { return FProcesses; } }

		private ServerProcesses FBrokenProcesses = new ServerProcesses();
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
		public void Attach(ServerProcess AProcess)
		{
			lock (FSyncHandle)
			{
				AProcess.SetDebugger(this);
				FProcesses.Add(AProcess);
			}
		}

		/// <summary>
		/// Detaches the debugger from a process.
		/// </summary>
		public void Detach(ServerProcess AProcess)
		{
			lock (FSyncHandle)
			{
				FProcesses.Disown(AProcess);
				FBrokenProcesses.SafeDisown(AProcess);
				AProcess.SetDebugger(null);
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
		public void AttachSession(ServerSession ASession)
		{
			lock (FSyncHandle)
			{
				ASession.SetDebuggerID(DebuggerID);
				FSessions.Add(ASession);
				lock (ASession.Processes)
					foreach (ServerProcess LProcess in ASession.Processes)
						Attach(LProcess);
			}
		}
		
		public void DetachSession(ServerSession ASession)
		{
			lock (FSyncHandle)
			{
				lock (ASession.Processes)
					foreach (ServerProcess LProcess in ASession.Processes)
						if (LProcess.Debugger == this)
							Detach(LProcess);
							
				FSessions.Disown(ASession);
				ASession.SetDebuggerID(0);
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
			int LRunningCount = 0;
			for (int LIndex = 0; LIndex < FProcesses.Count; LIndex++)
				if (FProcesses[LIndex].IsRunning)
					LRunningCount++;
			return LRunningCount;
		}
		
		/// <summary>
		/// Waits for the debugger to pause
		/// </summary>
		public void WaitForPause(ServerProcess AProcess)
		{
			while (true)
			{
				if (IsPaused)
					return;
				
				FWaitSignal.WaitOne(500);
				AProcess.CheckAborted();
			}
		}
		
		private void InternalPause()
		{
			lock (FSyncHandle)
			{
				FIsPauseRequested = true;
				FPauseSignal.Reset();
			}
		}
		
		/// <summary>
		/// Initiates a pause.
		/// </summary>
		public void Pause()
		{
			InternalPause();
			FWaitSignal.Set();
		}
		
		private void InternalRun()
		{
			lock (FSyncHandle)
			{
				FIsPauseRequested = false;
				FBrokenProcesses.DisownAll();
				FPauseSignal.Set();
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
			lock (FSyncHandle)
			{
				if (FIsPauseRequested)
				{
					FPauseSignal.Set();
					FPauseSignal.Reset();
				}
			}
		}
		
/*
		public void RunTo(ServerProcess AProcess, ExecutionContext AContext)
		{
		}
		
		public void StepOver(ServerProcess AProcess)
		{
		}
		
		public void StepInto(ServerProcess AProcess)
		{
		}
*/
		
		private bool ShouldBreak(ServerProcess AProcess, PlanNode ANode, Exception AException)
		{
			if (FBreakOnException && (AException != null))
				return true;
				
			if (FBreakpoints.Count > 0)
			{
				DebugLocator LCurrentLocation = AProcess.ExecutingProgram.GetCurrentLocation();
				
				// Determine whether or not a breakpoint has been hit
				for (int LIndex = 0; LIndex < FBreakpoints.Count; LIndex++)
				{
					Breakpoint LBreakpoint = FBreakpoints[LIndex];
					if 
					(
						(LBreakpoint.Locator == LCurrentLocation.Locator) 
							&& (LBreakpoint.Line == LCurrentLocation.Line) 
							&& ((LBreakpoint.LinePos == -1) || (LBreakpoint.LinePos == LCurrentLocation.LinePos))
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
			List<DebugSessionInfo> LSessions = new List<DebugSessionInfo>();
			lock (FSyncHandle)
			{
				foreach (ServerSession LSession in FSessions)
					LSessions.Add(new DebugSessionInfo { SessionID = LSession.SessionID });
			}
			return LSessions;
		}
		
		/// <summary>
		/// Returns the list of current debugged processes, with the running status and current location of each.
		/// </summary>
		public List<DebugProcessInfo> GetProcesses()
		{
			List<DebugProcessInfo> LProcesses = new List<DebugProcessInfo>();
			lock (FSyncHandle)
			{
				bool LIsPaused = IsPaused;
				foreach (ServerProcess LProcess in FProcesses)
					LProcesses.Add
					(
						new DebugProcessInfo
						{
							ProcessID = LProcess.ProcessID,
							IsPaused = LProcess.IsRunning && LIsPaused,
							Location = (LProcess.IsRunning && LIsPaused) ? LProcess.ExecutingProgram.SafeGetCurrentLocation() : null,
							DidBreak = FBrokenProcesses.Contains(LProcess)
						}
					);
			}
			return LProcesses;
		}
		
		/// <summary>
		/// Returns the current call stack of a process.
		/// </summary>
		public CallStack GetCallStack(int AProcessID)
		{
			CheckPaused();
			
			lock (FSyncHandle)
			{
				ServerProcess LProcess = FProcesses.GetProcess(AProcessID);
					
				CallStack LCallStack = new CallStack();
				
				foreach (Program LProgram in LProcess.ExecutingPrograms)
				{
					DebugLocator LCurrentLocation = LProgram.Locator;
					foreach (RuntimeStackWindow LWindow in LProgram.Stack.GetCallStack())
					{
						if (LWindow.Originator != null)
							LCallStack.Add(new CallStackEntry(LCallStack.Count, LWindow.Originator.Description, new DebugLocator(LCurrentLocation, LWindow.Originator.Line, LWindow.Originator.LinePos)));
						else
							LCallStack.Add(new CallStackEntry(LCallStack.Count, String.Format("Program:{0}", LProgram.ID), LCurrentLocation));
						LCurrentLocation = LWindow.Locator;
					}
				}
				
				return LCallStack;
			}
		}
		
		/// <summary>
		/// Returns the current stack of a process within a specific window.
		/// </summary>
		public List<StackEntry> GetStack(int AProcessID, int AWindowIndex)
		{
			CheckPaused();
			
			lock (FSyncHandle)
			{
				List<StackEntry> LStack = new List<StackEntry>();
				//ServerProcess LProcess = FProcesses.GetProcess(AProcessID);
				//object[] LStackWindow = LProcess.Stack.GetStack(AWindowIndex);
				//for (int LIndex = 0; LIndex < LStackWindow.Length; LIndex++)
				//    LStack.Add
				//    (
				//        new StackEntry
				//        { 
				//            Index = LIndex, 
				//            Name = String.Format("Location{0}", LIndex), 
				//            Type = LStackWindow[LIndex] == null ? "<no value>" : LStackWindow[LIndex].GetType().FullName, 
				//            Value = LStackWindow[LIndex] == null ? "<no value>" : LStackWindow[LIndex].ToString() 
				//        }
				//    );
				return LStack;
			}
		}
		
		/// <summary>
		/// Toggles a breakpoint, returning true if the breakpoint was set, and false if it was cleared.
		/// </summary>
		/// <param name="ALocator">A locator identifying the document or operator in which the breakpoint is set.</param>
		/// <param name="ALine">The line on which the breakpoint is set.</param>
		/// <param name="ALinePos">The line position, -1 for no line position.</param>
		/// <returns>True if the breakpoint was set, false if it was cleared.</returns>
		public bool ToggleBreakpoint(string ALocator, int ALine, int ALinePos)
		{
			Breakpoint LBreakpoint = new Breakpoint(ALocator, ALine, ALinePos);
			int LIndex = FBreakpoints.IndexOf(LBreakpoint);
			if (LIndex >= 0)
			{
				FBreakpoints.Remove(LBreakpoint);
				return false;
			}
			else
			{
				FBreakpoints.Add(LBreakpoint);
				return true;
			}
		}
		
		/// <summary>
		/// Yields the current process to the debugger if a breakpoint or break condition is satisfied.
		/// </summary>
		public void Yield(ServerProcess AProcess, PlanNode ANode, Exception AException)
		{
			if (ShouldBreak(AProcess, ANode, AException))
			{
				lock (FSyncHandle)
				{
					FBrokenProcesses.Add(AProcess);
					InternalPause();
				}
			}

			while (FIsPauseRequested && FProcesses.Contains(AProcess))
			{
				Interlocked.Increment(ref FPausedCount);
				WaitHandle.SignalAndWait(FWaitSignal, FPauseSignal);
				Interlocked.Decrement(ref FPausedCount);
			}
		}
	}
}
