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
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Runtime;

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
			FBreakSignal = new AutoResetEvent(false);
			FPauseSignal = new ManualResetEvent(true);
			FProcesses = new ServerProcesses();
			FSessions = new ServerSessions();
		}

		#region IDisposable Members

		public void Dispose()
		{
			// Run to restart any paused processes.
			if (FPauseSignal != null)
				Run();
				
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
			
			if (FBreakSignal != null)
			{
				FBreakSignal.Close();
				FBreakSignal = null;
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
		
		private AutoResetEvent FBreakSignal;
		private ManualResetEvent FPauseSignal;
		private object FSyncHandle = new object();
		private int FPausedCount;
		
		private bool FIsPaused;
		public bool IsPaused { get { return FIsPaused; } }
		
		private bool FBreakOnException;
		public bool BreakOnException
		{
			get { return FBreakOnException; }
			set { FBreakOnException = value; }
		}
		
		private Breakpoints FBreakpoints = new Breakpoints();
		public Breakpoints Breakpoints { get { return FBreakpoints; } }
		
		private ServerSessions FSessions;
		public ServerSessions Sessions { get { return FSessions; } }
		
		private ServerProcesses FProcesses;
		public ServerProcesses Processes { get { return FProcesses; } }

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
				AProcess.SetDebugger(null);
			}
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
		/// Waits for a debugged thread to break
		/// </summary>
		public void WaitForBreak()
		{
			while (true)
			{
				lock (FSyncHandle)
				{
					if (GetRunningCount() == FPausedCount)
						return;
				}
				
				FBreakSignal.WaitOne();
			}
		}
		
		private void InternalPause()
		{
			lock (FSyncHandle)
			{
				FIsPaused = true;
				FPauseSignal.Reset();
			}
		}
		
		/// <summary>
		/// Initiates a pause.
		/// </summary>
		public void Pause()
		{
			InternalPause();

			bool LShouldBreak;
			lock (FSyncHandle)
			{
				LShouldBreak = GetRunningCount() == FPausedCount;
			}
			
			if (LShouldBreak)
				FBreakSignal.Set();
		}
		
		private void InternalRun()
		{
			lock (FSyncHandle)
			{
				FIsPaused = false;
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
		public void CheckForAbort()
		{
			FPauseSignal.Set();
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
				// Use call stack or executing plan source to build a locator
				DebugLocator LCurrentLocator = null;
				InstructionNodeBase LOriginator = AProcess.Context.CurrentStackWindow.Originator as InstructionNodeBase;
				if ((LOriginator != null) && (LOriginator.Operator != null))
					LCurrentLocator = new DebugLocator(LOriginator.Operator.Locator, ANode.Line, ANode.LinePos);
				else
				{
					if (AProcess.ExecutingPlan.Locator != null)
						LCurrentLocator = new DebugLocator(AProcess.ExecutingPlan.Locator, ANode.Line, ANode.LinePos);
					else
						LCurrentLocator = new DebugLocator(DebugLocator.CDynamicLocator, ANode.Line, ANode.LinePos);
				}
				
				// Determine whether or not a breakpoint has been hit
				for (int LIndex = 0; LIndex < FBreakpoints.Count; LIndex++)
				{
					Breakpoint LBreakpoint = FBreakpoints[LIndex];
					if 
					(
						(LBreakpoint.Locator == LCurrentLocator.Locator) 
							&& (LBreakpoint.Line == LCurrentLocator.Line) 
							&& ((LBreakpoint.LinePos == -1) || (LBreakpoint.LinePos == LCurrentLocator.LinePos))
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
		
		public CallStack GetCallStack(int AProcessID)
		{
			CheckPaused();
			
			lock (FSyncHandle)
			{
				ServerProcess LProcess = FProcesses.GetProcess(AProcessID);
					
				List<StackWindow> LStackWindows = LProcess.Context.GetCallStack();
				
				CallStack LCallStack = new CallStack();
				for (int LIndex = 0; LIndex < LStackWindows.Count; LIndex++)
				{
					InstructionNodeBase LOriginator = LStackWindows[LIndex].Originator as InstructionNodeBase;
					if ((LOriginator != null) && (LOriginator.Operator != null))
						LCallStack.Add(new CallStackEntry(LIndex, LOriginator.Operator.DisplayName));
					else
						LCallStack.Add(new CallStackEntry(LIndex, "<Plan>"));
				}
				return LCallStack;
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
		/// <param name="AContext"></param>
		public void Yield(ServerProcess AProcess, PlanNode ANode, Exception AException)
		{
			if (ShouldBreak(AProcess, ANode, AException))
				FPauseSignal.Reset();

			Interlocked.Increment(ref FPausedCount);
			WaitHandle.SignalAndWait(FBreakSignal, FPauseSignal);
			Interlocked.Decrement(ref FPausedCount);
		}
	}
}
