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
	public class Debugger : IDisposable, INotifyMultiPropertyChanged, Frontend.Client.Windows.IErrorSource
	{
		public Debugger(IDataphoria dataphoria)
		{
			InitializeBreakpoints();
			_dataphoria = dataphoria;
			_dataphoria.Connected += new EventHandler(DataphoriaConnected);
			_dataphoria.Disconnected += new EventHandler(DataphoriaDisconnected);
			UpdateDebuggerState();
		}
		
		public void Dispose()
		{
			InternalClearState();
			if (_dataphoria != null)
			{
				_dataphoria.Connected -= new EventHandler(DataphoriaConnected);
				_dataphoria.Disconnected -= new EventHandler(DataphoriaDisconnected);
				_dataphoria = null;
			}
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

		public event EventHandler Disposed;

		// TODO: Improve the debugger performance by implementing a property notification service which 
		//  allows multiple properties to be included in a single notification.
		
		public event MultiPropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string[] propertyNames)
		{
			try
			{
				if (PropertyChanged != null)
					PropertyChanged(this, propertyNames);
			}
			catch (Exception exception)
			{
				if (_dataphoria.IsConnected)
					_dataphoria.Invoke(new ThreadStart(delegate { _dataphoria.Warnings.AppendError(null, exception, false); }));
			}
		}

		private IDataphoria _dataphoria;

		public IDataphoria Dataphoria
		{
			get { return _dataphoria; }
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
			if (_dataphoria.IsConnected)
			{
				var debugger = _dataphoria.EvaluateQuery("(.System.Debug.GetDebuggers() where Session_ID = SessionID())[]") as IRow;
				if (debugger != null)
					InternalInitializeState((bool)debugger["IsPaused"], (bool)debugger["BreakOnException"], (bool)debugger["BreakOnStart"]);
				else
					InternalClearState();
			}
			else
				InternalClearState();
		}

		private void InternalInitializeState(bool isPaused, bool breakOnException, bool breakOnStart)
		{
			_isStarted = true;
			_isPaused = isPaused;
			_breakOnException = breakOnException;
			_breakOnStart = breakOnStart;
			_selectedProcessID = -1;
			_selectedCallStackIndex = -1;
			_currentLocation = null;

			NotifyPropertyChanged(new string[] { "IsStarted", "IsPaused", "BreakOnException", "BreakOnStart", "SelectedProcessID", "SelectedCallStackIndex", "CurrentLocation" });

			if (!isPaused)
				InternalUnpause();
			
			ResetSelectedProcess();
			
			if (((IScalar)Dataphoria.EvaluateQuery("exists(.System.Debug.GetBreakpoints())")).AsBoolean)
				RefreshBreakpoints();
		}

		private void InternalClearState()
		{
			_isStarted = false;
			_isPaused = false;
			_breakOnException = false;
			_breakOnStart = false;
			_selectedProcessID = -1;
			_selectedCallStackIndex = -1;
			_currentLocation = null;

			NotifyPropertyChanged(new string[] { "IsStarted", "IsPaused", "BreakOnException", "BreakOnStart", "SelectedProcessID", "SelectedCallStackIndex", "CurrentLocation" });

			RefreshBreakpoints();
			ClearSelectedError();
		}

		// IsStarted
		
		private bool _isStarted;

		public bool IsStarted
		{
			get { return _isStarted; }
			set
			{
				if (_isStarted != value)
				{
					if (value)
						Start();
					else
						Stop();
				}
			}
		}
		
		private void InternalSetIsStarted(bool tempValue)
		{
				_isStarted = tempValue;
				NotifyPropertyChanged(new string[] { "IsStarted" });
		}
		
		public void Start()
		{
			if (!_isStarted && _dataphoria.IsConnected)
			{
				_dataphoria.ExecuteScript(@".System.Debug.Start();");
				UpdateDebuggerState();
			}
		}

		public void Stop()
		{
			if (_isStarted)
			{
				_dataphoria.ExecuteScript(@".System.Debug.Stop();");
				InternalClearState();
			}
		}

		// IsPaused
		
		private bool _isPaused;
		
		public bool IsPaused { get { return _isPaused; } }
		
		private void InternalSetIsPaused(bool tempValue)
		{
			if (tempValue != _isPaused)
			{
				_isPaused = tempValue;
				NotifyPropertyChanged(new string[] { "IsPaused" });
			}
		}

		public void Run()
		{
			if (IsStarted && IsPaused && _dataphoria.IsConnected)
			{
				_dataphoria.ExecuteScript(@".System.Debug.Run();");
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
				_dataphoria.ExecuteScript(@".System.Debug.Pause();");
			}
		}

		private void InternalUnpause()
		{
			SelectedProcessID = -1;
			InternalSetIsPaused(false);
			ClearSelectedError();
			new Thread(new ThreadStart(DebuggerThread)).Start();
		}

		public void DebuggerThread()
		{
			try
			{
				var process = _dataphoria.DataSession.ServerSession.StartProcess(new ProcessInfo(_dataphoria.DataSession.ServerSession.SessionInfo));
				try
				{
					process.Execute(".System.Debug.WaitForPause();", null);
					if (_dataphoria.IsConnected)
						_dataphoria.Invoke(new ThreadStart(delegate { DebuggerPaused(); }));
				}
				finally
				{
					if (_dataphoria.IsConnected)
						_dataphoria.DataSession.ServerSession.StopProcess(process);
				}
			}
			catch (Exception exception)
			{
				if (_dataphoria.IsConnected)
					_dataphoria.Invoke(new ThreadStart(delegate { _dataphoria.Warnings.AppendError(null, exception, false); }));
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
		
		private bool _breakOnException;
		
		public bool BreakOnException 
		{ 
			get { return _breakOnException; } 
			set 
			{
				if (_breakOnException != value)
				{
					_breakOnException = value;
					if (_isStarted)
						ApplyBreakOnException();
					NotifyPropertyChanged(new string[] { "BreakOnException" });
				}
			}
		}

		private void ApplyBreakOnException()
		{
			_dataphoria.ExecuteScript(".System.Debug.SetBreakOnException(" + _breakOnException.ToString().ToLowerInvariant() + ")");
		}

		// BreakOnStart

		private bool _breakOnStart;

		public bool BreakOnStart
		{
			get { return _breakOnStart; }
			set
			{
				if (_breakOnStart != value)
				{
					_breakOnStart = value;
					if (_isStarted)
						ApplyBreakOnStart();
					NotifyPropertyChanged(new string[] { "BreakOnStart" });
				}
			}
		}

		private void ApplyBreakOnStart()
		{
			_dataphoria.ExecuteScript(".System.Debug.SetBreakOnStart(" + _breakOnStart.ToString().ToLowerInvariant() + ")");
		}

		// SelectedProcessID
		
		private int _selectedProcessID = -1;
		
		public int SelectedProcessID
		{
			get { return _selectedProcessID; }
			set 
			{ 
				if (value != _selectedProcessID)
				{
					_selectedProcessID = value; 
					NotifyPropertyChanged(new string[] { "SelectedProcessID" });
					ResetSelectedCallStackIndex();
				}
			}
		}

		/// <summary> Sets the selected process to a debugged process, preferrably the one that broke. </summary>
		private void ResetSelectedProcess()
		{
			var processes = _dataphoria.OpenCursor(".System.Debug.GetProcesses() where IsPaused");
			try
			{
				ClearSelectedError();
				int candidateID = -1;
				IRow row = null;
				while (processes.Next())
				{
					if (row == null)
						row = processes.Select();
					else
						processes.Select(row);
					candidateID = (int)row["Process_ID"];
					if (row.HasValue("DidBreak") && (bool)row["DidBreak"])
					{
						if (row.HasValue("Error"))
						{
							var error = (Exception)row["Error"];
							if (error != null)
								SetSelectedError(error);
						}
						break;
					}
				}
				SelectedProcessID = candidateID;
			}
			finally
			{
				_dataphoria.CloseCursor(processes);
			}
		}

		// SelectedError
		
		private void SetSelectedError(Exception error)
		{
			Dataphoria.Warnings.AppendError(this, error, false);
		}

		private void ClearSelectedError()
		{
			if ((Dataphoria != null) && (Dataphoria.Warnings != null))
				Dataphoria.Warnings.ClearErrors(this);
		}

		// SelectedCallStackIndex
		
		private int _selectedCallStackIndex = -1;
		
		public int SelectedCallStackIndex
		{
			get { return _selectedCallStackIndex; }
			set
			{
				if (value != _selectedCallStackIndex)
					InternalSetCallStackIndex(value);
			}
		}

		private void InternalSetCallStackIndex(int tempValue)
		{
			_selectedCallStackIndex = tempValue;
			NotifyPropertyChanged(new string[] { "SelectedCallStackIndex" });
			UpdateCurrentLocation();
		}

		private void ResetSelectedCallStackIndex()
		{
			// Don't check that it's different in a reset
			if (_selectedProcessID >= 0)
				InternalSetCallStackIndex(0);
			else
				InternalSetCallStackIndex(-1);
		}

		// CurrentLocation
		
		private DebugLocator _currentLocation;
		
		public DebugLocator CurrentLocation
		{
			get { return _currentLocation; }
		}
		
		private void UpdateCurrentLocation()
		{
			if (_selectedProcessID >= 0)
			{
				DebugLocator location = null;
				var window = _dataphoria.EvaluateQuery(String.Format("(.System.Debug.GetCallStack({0}) where Index = {1})[]", _selectedProcessID, _selectedCallStackIndex)) as IRow;
				if (window != null)
				{
				    location = 
						new DebugLocator
						(
							(string)window["Locator"], 
							(int)window["Line"], 
							(int)window["LinePos"]
						);
				}
				InternalSetCurrentLocation(location);
			}
			else
				if (_currentLocation != null)
					InternalSetCurrentLocation(null);
		}

		private void InternalSetCurrentLocation(DebugLocator LLocation)
		{
			if 
			(
				((LLocation == null) != (_currentLocation == null)) 
					|| (LLocation == null 
					|| _currentLocation == null 
					|| !LLocation.Equals(_currentLocation))
			)
			{
				_currentLocation = LLocation;
				NotifyPropertyChanged(new string[] { "CurrentLocation" });
			}
		}
		
		// Breakpoints

		private bool _settingBreakpoint;
		
		private NotifyingBaseList<DebugLocator> _breakpoints;

		public NotifyingBaseList<DebugLocator> Breakpoints
		{
			get { return _breakpoints; }
		}

		private void InitializeBreakpoints()
		{
			_breakpoints = new NotifyingBaseList<DebugLocator>();
			_breakpoints.Changed += new NotifyingListChangeEventHandler<DebugLocator>(BreakpointsChanged);
		}

		private void BreakpointsChanged(NotifyingBaseList<DebugLocator> sender, bool isAdded, DebugLocator item, int index)
		{
			if (!_settingBreakpoint)
			{
				// Assumption: AIsAdded should always be in sync with the toggle
				InternalToggleBreakpoint(item);
			}
		}

		public void ToggleBreakpoint(DebugLocator locator)
		{
			if (locator != null && !String.IsNullOrEmpty(locator.Locator))
			{
				InternalToggleBreakpoint(locator);
				RefreshBreakpoints();
			}
		}

		private void InternalToggleBreakpoint(DebugLocator locator)
		{
			if (locator != null && !String.IsNullOrEmpty(locator.Locator))
			{
				Start();
				_dataphoria.ExecuteScript(String.Format(".System.Debug.ToggleBreakpoint('{0}', {1}, {2});", locator.Locator.Replace("'", "''"), locator.Line, locator.LinePos));
			}
		}

		private void RefreshBreakpoints()
		{
			_settingBreakpoint = true;
			try
			{
				if (IsStarted)
				{
					Set<DebugLocator> remaining = new Set<DebugLocator>(_breakpoints);
					
					var breakpoints = _dataphoria.OpenCursor(".System.Debug.GetBreakpoints()");
					try
					{
						IRow row = null;
						while (breakpoints.Next())
						{
							if (row == null)
								row = breakpoints.Select();
							else
								breakpoints.Select(row);
							
							var locator = new DebugLocator((string)row["Locator"], (int)row["Line"], (int)row["LinePos"]);
							if (!remaining.Remove(locator))
								_breakpoints.Add(locator);
						}
					}
					finally
					{
						_dataphoria.CloseCursor(breakpoints);
					}
					
					foreach (var locator in remaining)
						_breakpoints.Remove(locator);
				}
				else
					_breakpoints.Clear();
			}
			finally
			{
				_settingBreakpoint = false;
			}
		}

		// Functions

		public event EventHandler SessionAttached;
		
		public void AttachSession(int sessionID)
		{
			Start();
			_dataphoria.ExecuteScript(String.Format(".System.Debug.AttachSession({0});", sessionID));
			if (SessionAttached != null)
				SessionAttached(this, EventArgs.Empty);
		}

		public event EventHandler SessionDetached;

		public void DetachSession(int sessionID)
		{
			Start();
			_dataphoria.ExecuteScript(String.Format(".System.Debug.DetachSession({0});", sessionID));
			if (SessionDetached != null)
				SessionDetached(this, EventArgs.Empty);
		}

		public event EventHandler ProcessAttached;

		public void AttachProcess(int processID)
		{
			Start();
			_dataphoria.ExecuteScript(String.Format(".System.Debug.AttachProcess({0});", processID));
			if (ProcessAttached != null)
				ProcessAttached(this, EventArgs.Empty);
		}

		public event EventHandler ProcessDetached;

		public void DetachProcess(int processID)
		{
			Start();
			_dataphoria.ExecuteScript(String.Format(".System.Debug.DetachProcess({0});", processID));
			if (ProcessDetached != null)
				ProcessDetached(this, EventArgs.Empty);
		}
		
		public void StepOver()
		{
			if (IsPaused && _selectedProcessID >= 0)
			{
				_dataphoria.ExecuteScript(String.Format(".System.Debug.StepOver({0});", _selectedProcessID));
				InternalUnpause();
			}
		}
		
		public void StepInto()
		{
			if (IsPaused && _selectedProcessID >= 0)
			{
				_dataphoria.ExecuteScript(String.Format(".System.Debug.StepInto({0});", _selectedProcessID));
				InternalUnpause();
			}
		}

		#region IErrorSource Members

		public void ErrorHighlighted(Exception exception)
		{
			// Nothing
		}

		public void ErrorSelected(Exception exception)
		{
			// Nothing
		}

		#endregion
	}
	
	public interface INotifyMultiPropertyChanged
	{
		event MultiPropertyChangedEventHandler PropertyChanged;
	}
	
	public delegate void MultiPropertyChangedEventHandler(object ASender, string[] APropertyNames);
}
