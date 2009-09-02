using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Dataphoria
{
	public class Debugger : INotifyPropertyChanged
	{
		public Debugger(IDataphoria ADataphoria)
		{
			InitializeBreakpoints();
			FDataphoria = ADataphoria;
			FDataphoria.Connected += new EventHandler(DataphoriaConnected);
			FDataphoria.Disconnected += new EventHandler(DataphoriaDisconnected);
			UpdateDebuggerState();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		
		protected void NotifyPropertyChanged(string APropertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(APropertyName));
		}

		private IDataphoria FDataphoria;

		public IDataphoria Dataphoria
		{
			get { return FDataphoria; }
		}

		private void DataphoriaDisconnected(object sender, EventArgs e)
		{
			InternalClearState();
		}

		private void DataphoriaConnected(object sender, EventArgs e)
		{
			UpdateDebuggerState();
		}

		private void UpdateDebuggerState()
		{
			if (FDataphoria.IsConnected)
			{
				var LDebugger = FDataphoria.EvaluateQuery("(.System.Debug.GetDebuggers() where Session_ID = SessionID())[]") as Row;
				if (LDebugger != null)
					InternalInitializeState((bool)LDebugger["IsPaused"], (bool)LDebugger["BreakOnException"]);
				else
					InternalClearState();
			}
			else
				InternalClearState();
		}

		private void InternalInitializeState(bool AIsPaused, bool ABreakOnException)
		{
			FIsStarted = true;
			FIsPaused = AIsPaused;
			FBreakOnException = ABreakOnException;
			FSelectedProcessID = -1;
			FSelectedCallStackIndex = -1;
			FCurrentLocation = null;

			NotifyPropertyChanged("IsStarted");
			NotifyPropertyChanged("IsPaused");
			NotifyPropertyChanged("BreakOnException");
			NotifyPropertyChanged("SelectedProcessID");
			NotifyPropertyChanged("SelectedCallStackIndex");
			NotifyPropertyChanged("CurrentLocation");

			if (!AIsPaused)
				InternalUnpause();
			
			ResetSelectedProcess();
			
			if (((Scalar)Dataphoria.EvaluateQuery("exists(.System.Debug.GetBreakpoints())")).AsBoolean)
				RefreshBreakpoints();
		}

		private void InternalClearState()
		{
			FIsStarted = false;
			FIsPaused = false;
			FBreakOnException = false;
			FSelectedProcessID = -1;
			FSelectedCallStackIndex = -1;
			FCurrentLocation = null;

			NotifyPropertyChanged("IsStarted");
			NotifyPropertyChanged("IsPaused");
			NotifyPropertyChanged("BreakOnException");
			NotifyPropertyChanged("SelectedProcessID");
			NotifyPropertyChanged("SelectedCallStackIndex");
			NotifyPropertyChanged("CurrentLocation");

			RefreshBreakpoints();
		}

		// IsStarted
		
		private bool FIsStarted;

		public bool IsStarted
		{
			get { return FIsStarted; }
			set
			{
				if (FIsStarted != value)
				{
					if (value)
						Start();
					else
						Stop();
				}
			}
		}
		
		private void InternalSetIsStarted(bool AValue)
		{
				FIsStarted = AValue;
				NotifyPropertyChanged("IsStarted");
		}
		
		public void Start()
		{
			if (!FIsStarted && FDataphoria.IsConnected)
			{
				FDataphoria.ExecuteScript(@".System.Debug.Start();");
				UpdateDebuggerState();
				ApplyBreakOnException();
			}
		}

		public void Stop()
		{
			if (FIsStarted)
			{
				FDataphoria.ExecuteScript(@".System.Debug.Stop();");
				InternalClearState();
			}
		}

		// IsPaused
		
		private bool FIsPaused;
		
		public bool IsPaused { get { return FIsPaused; } }
		
		private void InternalSetIsPaused(bool AValue)
		{
			if (AValue != FIsPaused)
			{
				FIsPaused = AValue;
				NotifyPropertyChanged("IsPaused");
			}
		}

		public void Run()
		{
			if (IsStarted && IsPaused && FDataphoria.IsConnected)
			{
				FDataphoria.ExecuteScript(@".System.Debug.Run();");
				InternalUnpause();
			}
		}

		/// <summary> Requests that the debugger pause. </summary>
		/// <remarks> IsPaused will not in general be set immediately following this call.  
		///  IsPaused is set when the debugger indicates that all debugged processes are fully paused. </remarks>
		public void Pause()
		{
			if (!IsPaused)
			{
				Start();
				// Note: Pause is asynchronous, so we aren't actually paused until our debugger thread is woken by a response to WaitForBreak
				FDataphoria.ExecuteScript(@".System.Debug.Pause();");
			}
		}

		private void InternalUnpause()
		{
			SelectedProcessID = -1;
			InternalSetIsPaused(false);
			new Thread(new ThreadStart(DebuggerThread)).Start();
		}

		public void DebuggerThread()
		{
			try
			{
				var LProcess = FDataphoria.DataSession.ServerSession.StartProcess(new ProcessInfo(FDataphoria.DataSession.ServerSession.SessionInfo));
				try
				{
					LProcess.Execute(".System.Debug.WaitForPause();", null);
					if (FDataphoria.IsConnected)
						FDataphoria.Invoke(new ThreadStart(delegate { DebuggerPaused(); }));
				}
				finally
				{
					if (FDataphoria.IsConnected)
						FDataphoria.DataSession.ServerSession.StopProcess(LProcess);
				}
			}
			catch (Exception LException)
			{
				if (FDataphoria.IsConnected)
					FDataphoria.Invoke(new ThreadStart(delegate { FDataphoria.Warnings.AppendError(null, LException, false); }));
			}
		}

		/// <summary> Invoked when the debugger pauses or breaks. </summary>
		private void DebuggerPaused()
		{
			if (IsStarted)
			{
				InternalSetIsPaused(true);
				ResetSelectedProcess();
			}
		}

		// BreakOnException
		
		private bool FBreakOnException;
		
		public bool BreakOnException 
		{ 
			get { return FBreakOnException; } 
			set 
			{
				if (FBreakOnException != value)
				{
					if (FIsStarted)
						ApplyBreakOnException();
					FBreakOnException = value;
					NotifyPropertyChanged("BreakOnException");
				}
			}
		}

		private void ApplyBreakOnException()
		{
			FDataphoria.ExecuteScript(".System.Debug.SetBreakOnException(" + FBreakOnException.ToString().ToLowerInvariant() + ")");
		}

		// SelectedProcessID
		
		private int FSelectedProcessID = -1;
		
		public int SelectedProcessID
		{
			get { return FSelectedProcessID; }
			set 
			{ 
				if (value != FSelectedProcessID)
				{
					FSelectedProcessID = value; 
					NotifyPropertyChanged("SelectedProcessID");
					ResetSelectedCallStackIndex();
				}
			}
		}

		/// <summary> Sets the selected process to a debugged process, preferrably the one that broke. </summary>
		private void ResetSelectedProcess()
		{
			var LProcesses = FDataphoria.OpenCursor(".System.Debug.GetProcesses() where IsPaused");
			try
			{
				int LCandidateID = -1;
				Row LRow = null;
				while (LProcesses.Next())
				{
					if (LRow == null)
						LRow = LProcesses.Select();
					else
						LProcesses.Select(LRow);
					LCandidateID = (int)LRow["Process_ID"];
					if (LRow.HasValue("DidBreak") && (bool)LRow["DidBreak"])
						break;
				}
				SelectedProcessID = LCandidateID;
			}
			finally
			{
				FDataphoria.CloseCursor(LProcesses);
			}
		}

		// SelectedCallStackIndex
		
		private int FSelectedCallStackIndex = -1;
		
		public int SelectedCallStackIndex
		{
			get { return FSelectedCallStackIndex; }
			set
			{
				if (value != FSelectedCallStackIndex)
					InternalSetCallStackIndex(value);
			}
		}

		private void InternalSetCallStackIndex(int AValue)
		{
			FSelectedCallStackIndex = AValue;
			NotifyPropertyChanged("SelectedCallStackIndex");
			UpdateCurrentLocation();
		}

		private void ResetSelectedCallStackIndex()
		{
			// Don't check that it's different in a reset
			if (FSelectedProcessID >= 0)
				InternalSetCallStackIndex(0);
			else
				InternalSetCallStackIndex(-1);
		}

		// CurrentLocation
		
		private DebugLocator FCurrentLocation;
		
		public DebugLocator CurrentLocation
		{
			get { return FCurrentLocation; }
		}
		
		private void UpdateCurrentLocation()
		{
			if (FSelectedProcessID >= 0)
			{
				DebugLocator LLocation = null;
				var LWindow = FDataphoria.EvaluateQuery(String.Format("(.System.Debug.GetCallStack({0}) where Index = {1})[]", FSelectedProcessID, FSelectedCallStackIndex)) as Row;
				if (LWindow != null)
				    LLocation = new DebugLocator((string)LWindow["Locator"], (int)LWindow["Line"], (int)LWindow["LinePos"]);
				InternalSetCurrentLocation(LLocation);
			}
			else
				if (FCurrentLocation != null)
					InternalSetCurrentLocation(null);
		}

		private void InternalSetCurrentLocation(DebugLocator LLocation)
		{
			if 
			(
				((LLocation == null) != (FCurrentLocation == null)) 
					|| (LLocation == null 
					|| FCurrentLocation == null 
					|| !LLocation.Equals(FCurrentLocation))
			)
			{
				FCurrentLocation = LLocation;
				NotifyPropertyChanged("CurrentLocation");
			}
		}
		
		// Breakpoints

		private bool FSettingBreakpoint;
		
		private NotifyingBaseList<DebugLocator> FBreakpoints;

		public NotifyingBaseList<DebugLocator> Breakpoints
		{
			get { return FBreakpoints; }
		}

		private void InitializeBreakpoints()
		{
			FBreakpoints = new NotifyingBaseList<DebugLocator>();
			FBreakpoints.Changed += new NotifyingListChangeEventHandler<DebugLocator>(BreakpointsChanged);
		}

		private void BreakpointsChanged(NotifyingBaseList<DebugLocator> ASender, bool AIsAdded, DebugLocator AItem, int AIndex)
		{
			if (!FSettingBreakpoint)
			{
				// Assumption: AIsAdded should always be in sync with the toggle
				InternalToggleBreakpoint(AItem);
			}
		}

		public void ToggleBreakpoint(DebugLocator ALocator)
		{
			if (ALocator != null && !String.IsNullOrEmpty(ALocator.Locator))
			{
				InternalToggleBreakpoint(ALocator);
				RefreshBreakpoints();
			}
		}

		private void InternalToggleBreakpoint(DebugLocator ALocator)
		{
			if (ALocator != null && !String.IsNullOrEmpty(ALocator.Locator))
			{
				Start();
				FDataphoria.ExecuteScript(String.Format(".System.Debug.ToggleBreakpoint('{0}', {1}, {2});", ALocator.Locator.Replace("'", "''"), ALocator.Line, ALocator.LinePos));
			}
		}

		private void RefreshBreakpoints()
		{
			FSettingBreakpoint = true;
			try
			{
				if (IsStarted)
				{
					Set<DebugLocator> LRemaining = new Set<DebugLocator>(FBreakpoints);
					
					var LBreakpoints = FDataphoria.OpenCursor(".System.Debug.GetBreakpoints()");
					try
					{
						Row LRow = null;
						while (LBreakpoints.Next())
						{
							if (LRow == null)
								LRow = LBreakpoints.Select();
							else
								LBreakpoints.Select(LRow);
							
							var LLocator = new DebugLocator((string)LRow["Locator"], (int)LRow["Line"], (int)LRow["LinePos"]);
							if (!LRemaining.Remove(LLocator))
								FBreakpoints.Add(LLocator);
						}
					}
					finally
					{
						FDataphoria.CloseCursor(LBreakpoints);
					}
					
					foreach (var LLocator in LRemaining)
						FBreakpoints.Remove(LLocator);
				}
				else
					FBreakpoints.Clear();
			}
			finally
			{
				FSettingBreakpoint = false;
			}
		}

		// Functions

		public void AttachSession(int ASessionID)
		{
			Start();
			FDataphoria.ExecuteScript(String.Format(".System.Debug.AttachSession({0});", ASessionID));
		}

		public void DetachSession(int ASessionID)
		{
			Start();
			FDataphoria.ExecuteScript(String.Format(".System.Debug.DetachSession({0});", ASessionID));
		}

		public void AttachProcess(int AProcessID)
		{
			Start();
			FDataphoria.ExecuteScript(String.Format(".System.Debug.AttachProcess({0});", AProcessID));
		}

		public void DetachProcess(int AProcessID)
		{
			Start();
			FDataphoria.ExecuteScript(String.Format(".System.Debug.DetachProcess({0});", AProcessID));
		}
		
		public void StepOver()
		{
			if (IsPaused && FSelectedProcessID >= 0)
			{
				FDataphoria.ExecuteScript(String.Format(".System.Debug.StepOver({0});", FSelectedProcessID));
				InternalUnpause();
			}
		}
		
		public void StepInto()
		{
			if (IsPaused && FSelectedProcessID >= 0)
			{
				FDataphoria.ExecuteScript(String.Format(".System.Debug.StepInto({0});", FSelectedProcessID));
				InternalUnpause();
			}
		}
	}
}
