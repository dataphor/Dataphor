/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Specialized;
using System.Text;	   

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Client
{
	/// <summary> A DataSet descendent implementing behavior common to all datasets connecting to a Dataphor Server. </summary>
	public abstract class DAEDataSet : DataSet
	{
		// Do not localize
		public const string ParamNamespace = "AMaster";

		public DAEDataSet()
		{
			_paramGroups = new DataSetParamGroups();
			_paramGroups.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			_paramGroups.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			_cursorType = CursorType.Dynamic;
			_requestedIsolation = CursorIsolation.Browse;
			_requestedCapabilities = CursorCapability.Navigable | CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Updateable;
		}

		protected override void InternalDispose(bool disposing)
		{
			try
			{
				base.InternalDispose(disposing);
			}
			finally
			{
				if (_paramGroups != null)
				{
					_paramGroups.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
					_paramGroups.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
					_paramGroups.Dispose();
					_paramGroups = null;
				}

				Session = null;
			}
		}

		#region Session

		private bool ContainerContainsSession(DataSession session)
		{
			#if !SILVERLIGHT
			if ((session == null) || (Container == null))
				return false;
			foreach (Component component in Container.Components)
				if (component == session)
					return true;
			#endif
			return false;
		}

		protected bool ShouldSerializeSession()
		{
			return ContainerContainsSession(_session);
		}

		protected bool ShouldSerializeSessionName()
		{
			return !ContainerContainsSession(_session);
		}
		
		// Session
		private DataSession _session;
		/// <summary> Attached to a DataSession object for connection to a Server. </summary>
		[Category("Data")]
		[Description("Connection to a Server.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public DataSession Session
		{
			get { return _session; }
			set
			{
				if (_session != value)
				{
					try
					{
						Close();
					}
					finally
					{
						if (_session != null)
						{
							_session.OnClosing -= new EventHandler(SessionClosing);
							_session.Disposed -= new EventHandler(SessionDisposed);
						}
						_session = value;
						if (_session != null)
						{
							_session.OnClosing += new EventHandler(SessionClosing);
							_session.Disposed += new EventHandler(SessionDisposed);
						}
					}
				}
			}
		}

		public override void EndInit()
		{
			// This call ensures that the session is set active prior to the dataset, no matter the order of the EndInit calls
			if (_session != null)
				_session.DataSetEndInit();
			base.EndInit();
		}

		[Category("Data")]
		[Description("Connection to a Server.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Editor("Alphora.Dataphor.DAE.Client.Design.SessionEditor,Alphora.Dataphor.DAE.Client", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public string SessionName
		{
			get { return _session != null ? _session.SessionName : string.Empty; }
			set
			{
				if (SessionName != value)
				{
					if (value == string.Empty)
						Session = null;
					else
						Session = DataSession.Sessions[value];
				}
			}
		}
		
		private void SessionDisposed(object sender, EventArgs args)
		{
			Session = null;
		}

		private void SessionClosing(object sender, EventArgs args)
		{
			Close();
		}

		protected void CheckSession()
		{
			if (_session == null)
				throw new ClientException(ClientException.Codes.SessionMissing);
		}
		
		#endregion

		#region Process

		// Process
		protected IServerProcess _process;

		protected override IServerProcess InternalGetProcess()
		{
			return _process;
		}
		
		// IsolationLevel
		protected IsolationLevel _isolationLevel = IsolationLevel.Browse;
		[Category("Behavior")]
		[DefaultValue(IsolationLevel.Browse)]
		[Description("The isolation level for transactions performed by this dataset.")]
		public IsolationLevel IsolationLevel
		{
			get { return _isolationLevel; }
			set 
			{ 
				_isolationLevel = value; 
				if (_process != null)
					_process.ProcessInfo.DefaultIsolationLevel = _isolationLevel;
			}
		}
		
		// RequestedIsolation
		protected CursorIsolation _requestedIsolation;
		[Category("Behavior")]
		[DefaultValue(CursorIsolation.Browse)]
		[Description("The requested relative isolation of the cursor.  This will be used in conjunction with the isolation level of the transaction to determine the actual isolation of the cursor.")]
		public CursorIsolation RequestedIsolation
		{
			get { return _requestedIsolation; }
			set 
			{ 
				CheckState(DataSetState.Inactive);
				_requestedIsolation = value; 
			}
		}

		// Capabilities
		protected CursorCapability _requestedCapabilities;
		[Category("Behavior")]
		[
			DefaultValue
			(
				CursorCapability.Navigable |
				CursorCapability.BackwardsNavigable |
				CursorCapability.Bookmarkable |
				CursorCapability.Searchable |
				CursorCapability.Updateable
			)
		]
		[Description("Determines the requested behavior of the cursor")]
		public CursorCapability RequestedCapabilities
		{
			get { return _requestedCapabilities; }
			set 
			{ 
				CheckState(DataSetState.Inactive);
				_requestedCapabilities = value; 
				if ((_requestedCapabilities & CursorCapability.Updateable) != 0)
					InternalIsReadOnly = false;
				else
					InternalIsReadOnly = true;
			}
		}

		protected override bool InternalIsReadOnly
		{
			get { return base.InternalIsReadOnly; }
			set 
			{ 
				if (value)
					_requestedCapabilities &= ~CursorCapability.Updateable;
				else
					_requestedCapabilities |= CursorCapability.Updateable;
				base.InternalIsReadOnly = value; 
			}
		}

		private void StartProcess()
		{
			ProcessInfo processInfo = new ProcessInfo(_session.SessionInfo);
			processInfo.DefaultIsolationLevel = _isolationLevel;
			processInfo.FetchAtOpen = ShouldFetchAtOpen();
			_process = _session.ServerSession.StartProcess(processInfo);
			_cursor = new DAECursor(_process);
			_cursor.OnErrors += new CursorErrorsOccurredHandler(CursorOnErrors);
		}
		
		internal delegate void StopProcessHandler(IServerProcess AProcess);
		
		private void StopProcess()
		{
			if (_process != null)
			{
				try
				{
					if (_cursor != null)
					{
						try
						{
							_cursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
							_cursor.Dispose();
						}
						catch
						{
							// Errors closing the cursor should not prevent the process from stopping
							_cursor = null;
						}
					}

					#if USEASYNCSTOPPROCESS
					new StopProcessHandler(FSession.ServerSession.StopProcess).BeginInvoke(FProcess, null, null);
					#else
					_session.ServerSession.StopProcess(_process);
					#endif
				}
				finally
				{
					_process = null;
				}
			}
		}
		
		#endregion

		#region Cursor

		// Cursor
		protected DAECursor _cursor;
		
		protected override Schema.TableVar InternalGetTableVar()
		{
			return _cursor.TableVar;
		}
		
		protected virtual DAECursor GetEditCursor()
		{
			return _cursor;
		}
		
		protected abstract string InternalGetExpression();

		private void PrepareExpression()
		{
			_cursor.Expression = InternalGetExpression();
		}
		
		// CursorType
		protected CursorType _cursorType = CursorType.Dynamic;
		[Category("Behavior")]
		[DefaultValue(CursorType.Dynamic)]
		[Description("Determines the behavior of the cursor with respect to updates made after the cursor is opened.")]
		public CursorType CursorType
		{
			get { return _cursorType; }
			set 
			{ 
				CheckState(DataSetState.Inactive);
				_cursorType = value; 
			}
		}
		
		protected void CursorOnErrors(DAECursor cursor, CompilerMessages messages)
		{
			ReportErrors(messages);
		}

		protected virtual void OpenCursor()
		{
			try
			{
				SetParamValues();
				_cursor.ShouldOpen = ShouldOpenCursor();
				_cursor.Open();
				GetParamValues();
			}
			catch
			{
				try
				{
					CloseCursor();
				}
				catch
				{
					// Errors closing the cursor if it errored on opening should be ignored
				}

				throw;
			}
		}

		protected virtual void CloseCursor()
		{
			_cursor.Close();
		}

		private void PrepareCursor()
		{
			_cursor.Prepare();
		}
		
		private void UnprepareCursor()
		{
			_cursor.Unprepare();
		}

		#endregion

		#region Parameters

		public DataSetParamGroups _paramGroups;
		/// <summary> A collection of parameter groups. </summary>
		/// <remarks> All parameters from all groups are used to parameterize the expression. </remarks>
		[Category("Data")]
		[Description("Parameter Groups")]
		public DataSetParamGroups ParamGroups { get { return _paramGroups; } }
		
		private void DataSetParamChanged(object sender)
		{
			if (Active)
				CursorSetChanged(null, false);
		}
		
		private void DataSetParamStructureChanged(object sender)
		{
			if (Active)
				CursorSetChanged(null, true);
		}

		protected static string GetParameterName(string columnName)
		{
			return ParamNamespace + columnName.Replace(".", "_");
		}

		protected override void InternalCursorSetChanged(IRow row, bool reprepare)
		{
			if (reprepare)
			{
				InternalClose();
				InternalOpen();
			}
			else
			{
				CloseCursor();
				OpenCursor();
			}
		}
		
		protected virtual void InternalPrepareParams()
		{
			foreach (DataSetParamGroup group in _paramGroups)
				foreach (DataSetParam param in group.Params)
					if (group.Source != null)
						_cursor.Params.Add(new MasterDataSetDataParam(param.Name, group.Source.DataSet.TableType.Columns[param.ColumnName].DataType, param.Modifier, param.ColumnName, group.Source, false));
					else
						_cursor.Params.Add(new SourceDataSetDataParam(param));
		}
		
		private void PrepareParams()
		{
			_cursor.Params.Clear();
			try
			{
				InternalPrepareParams();
			}
			catch
			{
				_cursor.Params.Clear();
				throw;
			}
		}
		
		private void UnprepareParams()
		{
			_cursor.Params.Clear();
		}

		protected virtual void SetParamValues()
		{
			foreach (DataSetDataParam param in _cursor.Params)
				param.Bind(Process);
		}
		
		private void GetParamValues()
		{
			foreach (DataSetDataParam param in _cursor.Params)
				if (param is SourceDataSetDataParam)
					if (param.Modifier == Modifier.Var)
						((SourceDataSetDataParam)param).SourceParam.Value = param.Value;
		}
		
		/// <summary> Populates a given a (non null) DataParams collection with the actual params used by the DataSet. </summary>
		public void GetAllParams(DAE.Runtime.DataParams paramsValue)
		{
			CheckActive();
			foreach (DataSetDataParam param in _cursor.Params)
				paramsValue.Add(param);
		}

		/// <summary> Returns the list of the actual params used by the DataSet. </summary>
		/// <remarks> The user should not modify the list of values of these parameters. </remarks>
		[Browsable(false)]
		public DAE.Runtime.DataParams AllParams
		{
			get { return _cursor.Params; }
		}
		
		#endregion

		#region Open

		protected virtual bool ShouldOpenCursor()
		{
			return !IsWriteOnly;
		}
		
		protected virtual bool ShouldFetchAtOpen()
		{
			return !IsWriteOnly;
		}

		protected override void InternalConnect()
		{
			CheckSession();
			StartProcess();
		}

		protected override void InternalDisconnect()
		{
			StopProcess();
		}
		
		protected override void InternalOpen()
		{
			PrepareParams();
			PrepareExpression();
			PrepareCursor();
			OpenCursor();
		}
		
		#endregion

		#region Close

		protected override void InternalClose()
		{
			CloseCursor();
			UnprepareCursor();
			UnprepareParams();
		}

		#endregion
		
		#region DataSet Implementation

		protected override void InternalReset()
		{
			_cursor.Reset();
		}

		protected override void InternalRefresh(IRow row)
		{
			_cursor.Refresh(row);
		}

		protected override void InternalFirst()
		{
			_cursor.First();
		}

		protected override void InternalLast()
		{
			_cursor.Last();
		}
		
		protected override bool InternalNext()
		{
			return _cursor.Next();
		}
		
		protected override bool InternalPrior()
		{
			return _cursor.Prior();
		}

		protected override void InternalSelect(IRow row)
		{
			_cursor.Select(row);
		}
		
		protected override bool InternalGetBOF()
		{
			return _cursor.BOF();
		}

		protected override bool InternalGetEOF()
		{
			return _cursor.EOF();
		}

		protected override Guid InternalGetBookmark()
		{
			return _cursor.GetBookmark();
		}

		protected override bool InternalGotoBookmark(Guid bookmark, bool forward)
		{
			return _cursor.GotoBookmark(bookmark, forward);
		}

		protected override void InternalDisposeBookmark(Guid bookmark)
		{
			_cursor.DisposeBookmark(bookmark);
		}

		protected override void InternalDisposeBookmarks(Guid[] bookmarks)
		{
			_cursor.DisposeBookmarks(bookmarks);
		}

		protected override IRow InternalGetKey()
		{
			return _cursor.GetKey();
		}

		protected override bool InternalFindKey(IRow key)
		{
			return _cursor.FindKey(key);
		}

		protected override void InternalFindNearest(IRow key)
		{
			_cursor.FindNearest(key);
		}
		
		protected virtual void InternalInsert(IRow row)
		{
			GetEditCursor().Insert(row, _valueFlags);
		}
		
		protected virtual void InternalUpdate(IRow row)
		{
			GetEditCursor().Update(row, _valueFlags);
		}
		
		protected virtual void BeginTransaction()
		{
			_process.BeginTransaction(_isolationLevel);
		}
		
		protected virtual void PrepareTransaction()
		{
			_process.PrepareTransaction();
		}
		
		protected virtual void CommitTransaction()
		{
			_process.CommitTransaction();
		}
		
		protected override void InternalPost(IRow row)
		{
			// TODO: Test to ensure Optimistic concurrency check.
			bool updateSucceeded = false;
			BeginTransaction();
			try
			{
				if (State == DataSetState.Insert)
					InternalInsert(row);
				else
					InternalUpdate(row);
									
				updateSucceeded = true;
				
				PrepareTransaction();
				CommitTransaction();
			}
			catch (Exception E)
			{
				try
				{
					if (_process.InTransaction)
						_process.RollbackTransaction();
					if ((State == DataSetState.Edit) && updateSucceeded)
						GetEditCursor().Refresh(_originalRow);
				}
				catch (Exception rollbackException)
				{
					throw new DAE.Server.ServerException(DAE.Server.ServerException.Codes.RollbackError, E, rollbackException.ToString());
				}
				throw;
			}
		}

		protected override void InternalDelete()
		{
			_cursor.Delete();
		}
		
		#endregion

		#region class DAECursor
		
		protected delegate void CursorErrorsOccurredHandler(DAECursor ACursor, CompilerMessages AMessages);

		protected class DAECursor : Disposable
		{
			public DAECursor(IServerProcess process) : base()
			{
				_process = process;
				_params = new Runtime.DataParams();
			}

			protected override void Dispose(bool disposing)
			{
				Close();
				Unprepare();
				_process = null;
				base.Dispose(disposing);
			}

			private string _expression;
			public string Expression
			{
				get { return _expression; }
				set { _expression = value; }
			}

			private Runtime.DataParams _params;
			public Runtime.DataParams Params { get { return _params; } }

			private IServerProcess _process;
			public IServerProcess Process { get { return _process; } }
			
			private IServerExpressionPlan _plan;
			public IServerExpressionPlan Plan 
			{ 
				get 
				{ 
					InternalPrepare();
					return _plan; 
				} 
			}
			
			private Schema.TableVar _tableVar;
			public Schema.TableVar TableVar
			{
				get
				{
					if (_tableVar == null)
						InternalPrepare();
					return _tableVar;
				}
			}
			
			public event CursorErrorsOccurredHandler OnErrors;
			private void ErrorsOccurred(CompilerMessages errors)
			{
				if (OnErrors != null)
					OnErrors(this, errors);
			}
			
			private IServerCursor _cursor;
			
			/// <summary>Update cursor used to invoke updates and proposables if ShouldOpen is false.</summary>
			private IServerCursor _updateCursor;
			
			private void EnsureUpdateCursor()
			{
				if (_updateCursor == null)
					_updateCursor = Plan.Open(_params);
			}
			
			private void CloseUpdateCursor()
			{
				if (_updateCursor != null)
				{
					_plan.Close(_updateCursor);
					_updateCursor = null;
				}
			}
			
			private bool _shouldOpen = true;
			/// <summary>Indicates whether or not opening the underlying cursor is necessary.</summary>
			/// <remarks>
			///	This option is used as an optimization to prevent the opening of a known incorrect or invalid result set.
			/// It is used by the master/detail implementation of the TableDataSet to prevent the unnecessary open of an
			/// incompletely setup detail dataset. It is also used by the WriteOnly property to open a dataset that is
			/// only to be used for insert. In this case, the opening the underlying cursor is unnecessary, because no data
			/// actually needs to be read, only inserted.
			/// </remarks>
			public bool ShouldOpen 
			{ 
				get { return _shouldOpen; } 
				set { _shouldOpen = value; } 
			}
			
			private void InternalPrepare()
			{
				if (_plan == null)
				{
					_plan = _process.PrepareExpression(_expression, _params);
					try
					{
						ErrorsOccurred(_plan.Messages);

						if (!(_plan.DataType is Schema.ITableType))
							throw new ClientException(ClientException.Codes.InvalidResultType, _plan.DataType == null ? "<invalid expression>" : _plan.DataType.Name);
							
						_tableVar = _plan.TableVar;
					}
					catch
					{
						_plan = null;
						throw;
					}
				}
			}

			public void Prepare()
			{
				// The prepare call is deferred until it is required
			}

			public void Unprepare()
			{
				if (_plan != null)
				{
					if (_cursor != null)
					{
						try
						{
							_process.CloseCursor(_cursor);
						}
						finally
						{
							_cursor = null;
						}
					}
					else
					{
						try
						{
							_process.UnprepareExpression(_plan);
						}
						finally
						{
							_plan = null;
						}
					}
				}
			}

			public void Open()
			{
				if (_shouldOpen && (_cursor == null))
				{
					if (_plan == null)
					{
						try
						{
							_cursor = _process.OpenCursor(_expression, _params);
						}
						catch (Exception exception)
						{
							CompilerMessages messages = new CompilerMessages();
							messages.Add(exception);
							ErrorsOccurred(messages);
							throw exception;
						}
						
						_plan = _cursor.Plan;
						_tableVar = _plan.TableVar;
						ErrorsOccurred(_plan.Messages);
                    }
					else
						_cursor = _plan.Open(_params);
				}
			}

			public void Close()
			{
				CloseUpdateCursor();

				if (_cursor != null)
				{
					if (_plan != null)
					{
						try
						{
							_plan.Close(_cursor);
						}
						finally
						{
							_cursor = null;
						}
					}
				}
			}
			
			public void Reset()
			{
				if (_cursor != null)
					_cursor.Reset();
			}
			
			public bool Next()
			{
				if (_cursor != null)
					return _cursor.Next();
				else
					return false;
			}
			
			public void Last()
			{
				if (_cursor != null)
					_cursor.Last();
			}
			
			public bool BOF()
			{
				if (_cursor != null)
					return _cursor.BOF();
				else
					return true;
			}
			
			public bool EOF()
			{
				if (_cursor != null)
					return _cursor.EOF();
				else
					return true;
			}
			
			public bool IsEmpty()
			{
				if (_cursor != null)
					return _cursor.IsEmpty();
				else
					return true;
			}
			
			public IRow Select()
			{
				if (_cursor != null)
					return _cursor.Select();
				else
					return null;
			}
			
			public void Select(IRow row)
			{
				if (_cursor != null)
					_cursor.Select(row);
			}
			
			public void First()
			{
				if (_cursor != null)
					_cursor.First();
			}
			
			public bool Prior()
			{
				if (_cursor != null)
					return _cursor.Prior();
				else
					return false;
			}
			
			public Guid GetBookmark()
			{
				if (_cursor != null)
					return _cursor.GetBookmark();
				else
					return Guid.Empty;
			}

			public bool GotoBookmark(Guid bookmark, bool forward)
			{
				if (_cursor != null)
					return _cursor.GotoBookmark(bookmark, forward);
				else
					return false;
			}
			
			public int CompareBookmarks(Guid bookmark1, Guid bookmark2)
			{
				if (_cursor != null)
					return _cursor.CompareBookmarks(bookmark1, bookmark2);
				else
					return -1;
			}
			
			public void DisposeBookmark(Guid bookmark)
			{
				if (_cursor != null)
					_cursor.DisposeBookmark(bookmark);
			}
			
			public void DisposeBookmarks(Guid[] bookmarks)
			{
				if (_cursor != null)
					_cursor.DisposeBookmarks(bookmarks);
			}
			
			public Schema.Order Order 
			{ 
				get 
				{ 
					if (_cursor != null)
						return _cursor.Order;
					
					InternalPrepare();
					return _plan.Order;
				} 
			}
			
			public IRow GetKey()
			{
				if (_cursor != null)
					return _cursor.GetKey();
				else
					return null;
			}
			
			public bool FindKey(IRow key)
			{
				if (_cursor != null)
					return _cursor.FindKey(key);
				else
					return false;
			}
			
			public void FindNearest(IRow key)
			{
				if (_cursor != null)
					_cursor.FindNearest(key);
			}
			
			public bool Refresh(IRow row)
			{
				if (_cursor != null)
					return _cursor.Refresh(row);
				else
					return false;
			}
			
			public void Insert(IRow row, BitArray valueFlags)
			{
				if (_cursor != null)
					_cursor.Insert(row, valueFlags);
				else
				{
					EnsureUpdateCursor();
					_updateCursor.Insert(row, valueFlags);
				}
			}
			
			public void Update(IRow row, BitArray valueFlags)
			{
				if (_cursor != null)
					_cursor.Update(row, valueFlags);
			}
			
			public void Delete()
			{
				if (_cursor != null)
					_cursor.Delete();
			}
			
			public int RowCount()
			{
				if (_cursor != null)
					return _cursor.RowCount();
				else
					return 0;
			}
			
			public bool Default(IRow row, string columnName)
			{
				if (_cursor != null)
					return _cursor.Default(row, columnName);
				else
				{
					EnsureUpdateCursor();
					return _updateCursor.Default(row, columnName);
				}
			}
			
			public bool Change(IRow oldRow, IRow newRow, string columnName)
			{
				if (_cursor != null)
					return _cursor.Change(oldRow, newRow, columnName);
				else
				{
					EnsureUpdateCursor();
					return _updateCursor.Change(oldRow, newRow, columnName);
				}
			}
			
			public bool Validate(IRow oldRow, IRow newRow, string columnName)
			{
				if (_cursor != null)
					return _cursor.Validate(oldRow, newRow, columnName);
				else
				{
					EnsureUpdateCursor();
					return _updateCursor.Validate(oldRow, newRow, columnName);
				}
			}
		}
		
		#endregion
	}

	public delegate void ParamStructureChangedHandler(object ASender);
	public delegate void ParamChangedHandler(object ASender);
	
	public class DataSetParam : object
	{
		private string _name;
		public string Name
		{
			get { return _name; }
			set 
			{ 
				if (_name != value)
				{
					_name = value == null ? String.Empty : value;
					ParamStructureChanged();
				}
			}
		}
		
		private string _columnName;
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (_columnName != value)
				{
					_columnName = value == null ? String.Empty : value;
					ParamChanged();
				}
			}
		}
		
		private Schema.IDataType _dataType;
		public Schema.IDataType DataType
		{
			get { return _dataType; }
			set 
			{ 
				if (_dataType != value)
				{
					_dataType = value; 
					ParamStructureChanged();
				}
			}
		}
		
		private Modifier _modifier;
		public Modifier Modifier
		{
			get { return _modifier; }
			set
			{
				if (_modifier != value)
				{
					_modifier = value;
					ParamStructureChanged();
				}
			}
		}
		
		private object _value;
		public object Value
		{
			get { return _value; }
			set
			{
				if (_value != value)
				{
					_value = value;
					ParamChanged();
				}
			}
		}
		
		public event ParamStructureChangedHandler OnParamStructureChanged;
		private void ParamStructureChanged()
		{
			if (OnParamStructureChanged != null)
				OnParamStructureChanged(this);
		}
		
		public event ParamChangedHandler OnParamChanged;
		private void ParamChanged()
		{
			if (OnParamChanged != null)
				OnParamChanged(this);
		}
	}
	
	#if USETYPEDLIST
	public class DataSetParams : TypedList
	{
		public DataSetParams() : base(typeof(DataSetParam)) {}
		
		public new DataSetParam this[int AIndex]
		{
			get { return (DataSetParam)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Adding(object AValue, int AIndex)
		{
			((DataSetParam)AValue).OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			((DataSetParam)AValue).OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			DataSetParamStructureChanged(null);
		}
		
		protected override void Removing(object AValue, int AIndex)
		{
			((DataSetParam)AValue).OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
			((DataSetParam)AValue).OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
			DataSetParamStructureChanged(null);
		}
	#else
	public class DataSetParams : ValidatingBaseList<DataSetParam>
	{
		protected override void Adding(DataSetParam value, int index)
		{
			//base.Adding(AValue, AIndex);
			value.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			value.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			DataSetParamStructureChanged(null);
		}
		
		protected override void Removing(DataSetParam value, int index)
		{
			value.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
			value.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
			DataSetParamStructureChanged(null);
			//base.Removing(AValue, AIndex);
		}
	#endif
		
		private void DataSetParamChanged(object sender)
		{
			ParamChanged(sender);
		}
		
		private void DataSetParamStructureChanged(object sender)
		{
			ParamStructureChanged(sender);
		}
		
		public event ParamStructureChangedHandler OnParamStructureChanged;
		private void ParamStructureChanged(object sender)
		{
			if (OnParamStructureChanged != null)
				OnParamStructureChanged(sender);
		}
		
		public event ParamChangedHandler OnParamChanged;
		private void ParamChanged(object sender)
		{
			if (OnParamChanged != null)
				OnParamChanged(sender);
		}
	}

	public class DataSetParamGroup : Disposable
	{
		public DataSetParamGroup() : base()
		{
			_masterLink = new DataLink();
			_masterLink.OnRowChanged += new DataLinkFieldHandler(MasterRowChanged);
			_masterLink.OnDataChanged += new DataLinkHandler(MasterDataChanged);
			_masterLink.OnActiveChanged += new DataLinkHandler(MasterActiveChanged);
			
			_params = new DataSetParams();
			_params.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			_params.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_params != null)
			{
				_params.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
				_params.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
				_params = null;
			}
			
			if (_masterLink != null)
			{
				_masterLink.OnRowChanged -= new DataLinkFieldHandler(MasterRowChanged);
				_masterLink.OnDataChanged -= new DataLinkHandler(MasterDataChanged);
				_masterLink.OnActiveChanged -= new DataLinkHandler(MasterActiveChanged);
				_masterLink.Dispose();
				_masterLink = null;
			}

			base.Dispose(disposing);
		}

		private DataSetParams _params;
		public DataSetParams Params { get { return _params; } }
		
		// DataSource
		private DataLink _masterLink;
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Parameter data source")]
		public DataSource Source
		{
			get { return _masterLink.Source; }
			set
			{
				if (_masterLink.Source != value)
				{
					_masterLink.Source = value;
					ParamChanged();
				}
			}
		}

		private void MasterActiveChanged(DataLink link, DataSet dataSet)
		{
			ParamChanged();
		}

		private void MasterRowChanged(DataLink link, DataSet dataSet, DataField field)
		{
			ParamChanged();
		}
		
		private void MasterDataChanged(DataLink link, DataSet dataSet)
		{
			ParamChanged();
		}

		private void DataSetParamStructureChanged(object sender)
		{
			ParamStructureChanged();
		}
		
		private void DataSetParamChanged(object sender)
		{
			ParamChanged();
		}
		
		public event ParamStructureChangedHandler OnParamStructureChanged;
		protected virtual void ParamStructureChanged()
		{
			if (OnParamStructureChanged != null)
				OnParamStructureChanged(this);
		}
		
		public event ParamChangedHandler OnParamChanged;
		protected virtual void ParamChanged()
		{
			if (OnParamChanged != null)
				OnParamChanged(this);
		}

	}
	
	#if USETYPEDLIST
	public class DataSetParamGroups : DisposableTypedList
	{
		public DataSetParamGroups() : base(typeof(DataSetParamGroup)) {}
		
		public new DataSetParamGroup this[int AIndex]
		{
			get { return (DataSetParamGroup)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			((DataSetParamGroup)AValue).OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			((DataSetParamGroup)AValue).OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			DataSetParamStructureChanged(null);
		}
		
		protected override void Removing(object AValue, int AIndex)
		{
			((DataSetParamGroup)AValue).OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
			((DataSetParamGroup)AValue).OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
			DataSetParamStructureChanged(null);
			base.Removing(AValue, AIndex);
		}
	#else
	public class DataSetParamGroups : DisposableList<DataSetParamGroup>
	{
		protected override void Adding(DataSetParamGroup value, int index)
		{
			//base.Adding(AValue, AIndex);
			value.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			value.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			DataSetParamStructureChanged(null);
		}
		
		protected override void Removing(DataSetParamGroup value, int index)
		{
			value.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
			value.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
			DataSetParamStructureChanged(null);
			//base.Removing(AValue, AIndex);
		}
	#endif
		
		private void DataSetParamChanged(object sender)
		{
			ParamChanged(sender);
		}
		
		private void DataSetParamStructureChanged(object sender)
		{
			ParamStructureChanged(sender);
		}
		
		public event ParamStructureChangedHandler OnParamStructureChanged;
		protected virtual void ParamStructureChanged(object sender)
		{
			if (OnParamStructureChanged != null)
				OnParamStructureChanged(sender);
		}
		
		public event ParamChangedHandler OnParamChanged;
		protected virtual void ParamChanged(object sender)
		{
			if (OnParamChanged != null)
				OnParamChanged(sender);
		}
	}

	internal abstract class DataSetDataParam : Runtime.DataParam
	{
		public DataSetDataParam(string name, Schema.IDataType dataType, Modifier modifier) : base(name, dataType, modifier) {}
		
		public abstract void Bind(IServerProcess process);
	}
	
	internal class MasterDataSetDataParam : DataSetDataParam
	{
		public MasterDataSetDataParam(string name, Schema.IDataType dataType, Modifier modifier, string columnName, DataSource source, bool isMaster) : base(name, dataType, modifier)
		{
			ColumnName = columnName;
			Source = source;
			IsMaster = isMaster;
		}
		
		private string ColumnName;
		public DataSource Source;
		public bool IsMaster; // true if this parameter is part of a master/detail relationship
		
		public override void Bind(IServerProcess process)
		{
			if (!Source.DataSet.IsEmpty() && Source.DataSet.Fields[ColumnName].HasValue())
				Value = Source.DataSet.Fields[ColumnName].AsNative;
			else
				Value = null;
		}
	}
	
	internal class SourceDataSetDataParam : DataSetDataParam
	{
		public SourceDataSetDataParam(DataSetParam sourceParam) : base(sourceParam.Name, sourceParam.DataType, sourceParam.Modifier) 
		{
			SourceParam = sourceParam;
		}
		
		public DataSetParam SourceParam;
		
		public override void Bind(IServerProcess process)
		{
			Value = SourceParam.Value;
		}
	}
}

