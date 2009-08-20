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
			FDataphoria = ADataphoria;
			FDataphoria.Connected += new EventHandler(DataphoriaConnected);
			FDataphoria.Disconnected += new EventHandler(DataphoriaDisconnected);
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
			Run();
			Stop();
		}

		private void DataphoriaConnected(object sender, EventArgs e)
		{
			
		}

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
		
		public void Start()
		{
			if (!FIsStarted)
			{
				FDataphoria.ExecuteScript(@".System.Debug.Start();");
				FIsStarted = true;
				NotifyPropertyChanged("IsStarted");
			}
		}
		
		public void Stop()
		{
			if (FIsStarted)
			{
				FDataphoria.ExecuteScript(@".System.Debug.Stop();");
				FIsStarted = true;
				NotifyPropertyChanged("IsStarted");
			}
		}
		
		private bool FIsPaused;
		
		public bool IsPaused { get { return FIsPaused; } }
		
		private void SetIsPaused(bool AValue)
		{
			if (AValue != FIsPaused)
			{
				FIsPaused = AValue;
				NotifyPropertyChanged("IsPaused");
			}
		}
		
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
			InternalSetCallStackIndex(0);
		}

		private DebugLocator FCurrentLocation;
		
		public DebugLocator CurrentLocation
		{
			get { return FCurrentLocation; }
		}
		
		private void UpdateCurrentLocation()
		{
			if (FSelectedProcessID >= 0)
			{
				var LWindow = FDataphoria.EvaluateQuery(String.Format("(.System.Debug.GetCallStack({0}) where Index = {1})[]", FSelectedProcessID, FSelectedCallStackIndex)) as Row;
				DebugLocator LLocation = new DebugLocator((string)LWindow["Locator"], (int)LWindow["Line"], (int)LWindow["LinePos"]);
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

		public void Run()
		{
			if (IsPaused)
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
				// Note: Pause is asynchronous, so we aren't actually paused until our debugger thread is woken by a response to WaitForBreak
				FDataphoria.ExecuteScript(@".System.Debug.Pause();");
			}
		}
		
		public void AttachSession(int ASessionID)
		{
			FDataphoria.ExecuteScript(String.Format(".System.Debug.AttachSession({0});", ASessionID));
		}

		private void InternalUnpause()
		{
			SetIsPaused(false);
			new Thread(new ThreadStart(DebuggerThread)).Start();
		}

		public void DebuggerThread()
		{
			try
			{
				var LProcess = FDataphoria.DataSession.ServerSession.StartProcess(new ProcessInfo(FDataphoria.DataSession.ServerSession.SessionInfo));
				try
				{
					var LDebugBreakDelegate = new ThreadStart(delegate { DebuggerPaused(); });
					LProcess.Execute("Debug.WaitForPause();", null);
					FDataphoria.Invoke(LDebugBreakDelegate);
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

		/// <summary> Invoked when the debugger pauses or breaks. </summary>
		private void DebuggerPaused()
		{
			SetIsPaused(true);
			ResetSelectedProcess();
		}
	}
}
