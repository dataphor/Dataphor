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
		public const string CParamNamespace = "AMaster";

		public DAEDataSet()
		{
			FParamGroups = new DataSetParamGroups();
			FParamGroups.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			FParamGroups.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			FCursorType = CursorType.Dynamic;
			FRequestedIsolation = CursorIsolation.Browse;
			FRequestedCapabilities = CursorCapability.Navigable | CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Updateable;
		}

		protected override void InternalDispose(bool ADisposing)
		{
			try
			{
				base.InternalDispose(ADisposing);
			}
			finally
			{
				if (FParamGroups != null)
				{
					FParamGroups.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
					FParamGroups.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
					FParamGroups.Dispose();
					FParamGroups = null;
				}

				Session = null;
			}
		}

		#region Session

		private bool ContainerContainsSession(DataSession ASession)
		{
			#if !SILVERLIGHT
			if ((ASession == null) || (Container == null))
				return false;
			foreach (Component LComponent in Container.Components)
				if (LComponent == ASession)
					return true;
			#endif
			return false;
		}

		protected bool ShouldSerializeSession()
		{
			return ContainerContainsSession(FSession);
		}

		protected bool ShouldSerializeSessionName()
		{
			return !ContainerContainsSession(FSession);
		}
		
		// Session
		private DataSession FSession;
		/// <summary> Attached to a DataSession object for connection to a Server. </summary>
		[Category("Data")]
		[Description("Connection to a Server.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public DataSession Session
		{
			get { return FSession; }
			set
			{
				if (FSession != value)
				{
					Close();
					if (FSession != null)
					{
						FSession.OnClosing -= new EventHandler(SessionClosing);
						FSession.Disposed -= new EventHandler(SessionDisposed);
					}
					FSession = value;
					if (FSession != null)
					{
						FSession.OnClosing += new EventHandler(SessionClosing);
						FSession.Disposed += new EventHandler(SessionDisposed);
					}
				}
			}
		}

		public override void EndInit()
		{
			// This call ensures that the session is set active prior to the dataset, no matter the order of the EndInit calls
			if (FSession != null)
				FSession.DataSetEndInit();
			base.EndInit();
		}

		[Category("Data")]
		[Description("Connection to a Server.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Editor("Alphora.Dataphor.DAE.Client.Design.SessionEditor,Alphora.Dataphor.DAE.Client", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public string SessionName
		{
			get { return FSession != null ? FSession.SessionName : string.Empty; }
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
		
		private void SessionDisposed(object ASender, EventArgs AArgs)
		{
			Session = null;
		}

		private void SessionClosing(object ASender, EventArgs AArgs)
		{
			Close();
		}

		protected void CheckSession()
		{
			if (FSession == null)
				throw new ClientException(ClientException.Codes.SessionMissing);
		}
		
		#endregion

		#region Process

		// Process
		protected IServerProcess FProcess;

		protected override IServerProcess InternalGetProcess()
		{
			return FProcess;
		}
		
		// IsolationLevel
		protected IsolationLevel FIsolationLevel = IsolationLevel.Browse;
		[Category("Behavior")]
		[DefaultValue(IsolationLevel.Browse)]
		[Description("The isolation level for transactions performed by this dataset.")]
		public IsolationLevel IsolationLevel
		{
			get { return FIsolationLevel; }
			set 
			{ 
				FIsolationLevel = value; 
				if (FProcess != null)
					FProcess.ProcessInfo.DefaultIsolationLevel = FIsolationLevel;
			}
		}
		
		// RequestedIsolation
		protected CursorIsolation FRequestedIsolation;
		[Category("Behavior")]
		[DefaultValue(CursorIsolation.Browse)]
		[Description("The requested relative isolation of the cursor.  This will be used in conjunction with the isolation level of the transaction to determine the actual isolation of the cursor.")]
		public CursorIsolation RequestedIsolation
		{
			get { return FRequestedIsolation; }
			set 
			{ 
				CheckState(DataSetState.Inactive);
				FRequestedIsolation = value; 
			}
		}

		// Capabilities
		protected CursorCapability FRequestedCapabilities;
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
			get { return FRequestedCapabilities; }
			set 
			{ 
				CheckState(DataSetState.Inactive);
				FRequestedCapabilities = value; 
				if ((FRequestedCapabilities & CursorCapability.Updateable) != 0)
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
					FRequestedCapabilities &= ~CursorCapability.Updateable;
				else
					FRequestedCapabilities |= CursorCapability.Updateable;
				base.InternalIsReadOnly = value; 
			}
		}

		private void StartProcess()
		{
			ProcessInfo LProcessInfo = new ProcessInfo(FSession.SessionInfo);
			LProcessInfo.DefaultIsolationLevel = FIsolationLevel;
			LProcessInfo.FetchAtOpen = ShouldFetchAtOpen();
			FProcess = FSession.ServerSession.StartProcess(LProcessInfo);
			FCursor = new DAECursor(FProcess);
			FCursor.OnErrors += new CursorErrorsOccurredHandler(CursorOnErrors);
		}
		
		internal delegate void StopProcessHandler(IServerProcess AProcess);
		
		private void StopProcess()
		{
			if (FProcess != null)
			{
				if (FCursor != null)
				{
					FCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
					FCursor.Dispose();
					FCursor = null;
				}
				#if USEASYNCSTOPPROCESS
				new StopProcessHandler(FSession.ServerSession.StopProcess).BeginInvoke(FProcess, null, null);
				#else
				FSession.ServerSession.StopProcess(FProcess);
				#endif
				FProcess = null;
			}
		}
		
		#endregion

		#region Cursor

		// Cursor
		protected DAECursor FCursor;
		
		protected override Schema.TableVar InternalGetTableVar()
		{
			return FCursor.TableVar;
		}
		
		protected virtual DAECursor GetEditCursor()
		{
			return FCursor;
		}
		
		protected abstract string InternalGetExpression();

		private void PrepareExpression()
		{
			FCursor.Expression = InternalGetExpression();
		}
		
		// CursorType
		protected CursorType FCursorType = CursorType.Dynamic;
		[Category("Behavior")]
		[DefaultValue(CursorType.Dynamic)]
		[Description("Determines the behavior of the cursor with respect to updates made after the cursor is opened.")]
		public CursorType CursorType
		{
			get { return FCursorType; }
			set 
			{ 
				CheckState(DataSetState.Inactive);
				FCursorType = value; 
			}
		}
		
		protected void CursorOnErrors(DAECursor ACursor, CompilerMessages AMessages)
		{
			ReportErrors(AMessages);
		}

		public event ErrorsOccurredHandler OnErrors;
		protected void ReportErrors(CompilerMessages AMessages)
		{
			if (OnErrors != null)
				OnErrors(this, AMessages);
		}
		
		protected virtual void OpenCursor()
		{
			try
			{
				SetParamValues();
				FCursor.ShouldOpen = ShouldOpenCursor();
				FCursor.Open();
				GetParamValues();
			}
			catch
			{
				CloseCursor();
				throw;
			}
		}

		protected virtual void CloseCursor()
		{
			FCursor.Close();
		}

		private void PrepareCursor()
		{
			FCursor.Prepare();
		}
		
		private void UnprepareCursor()
		{
			FCursor.Unprepare();
		}

		#endregion

		#region Parameters

		public DataSetParamGroups FParamGroups;
		/// <summary> A collection of parameter groups. </summary>
		/// <remarks> All parameters from all groups are used to parameterize the expression. </remarks>
		[Category("Data")]
		[Description("Parameter Groups")]
		public DataSetParamGroups ParamGroups { get { return FParamGroups; } }
		
		private void DataSetParamChanged(object ASender)
		{
			if (Active)
				CursorSetChanged(null, false);
		}
		
		private void DataSetParamStructureChanged(object ASender)
		{
			if (Active)
				CursorSetChanged(null, true);
		}

		protected static string GetParameterName(string AColumnName)
		{
			return CParamNamespace + AColumnName.Replace(".", "_");
		}

		protected override void InternalCursorSetChanged(Row ARow, bool AReprepare)
		{
			if (AReprepare)
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
			foreach (DataSetParamGroup LGroup in FParamGroups)
				foreach (DataSetParam LParam in LGroup.Params)
					if (LGroup.Source != null)
						FCursor.Params.Add(new MasterDataSetDataParam(LParam.Name, LGroup.Source.DataSet.TableType.Columns[LParam.ColumnName].DataType, LParam.Modifier, LParam.ColumnName, LGroup.Source, false));
					else
						FCursor.Params.Add(new SourceDataSetDataParam(LParam));
		}
		
		private void PrepareParams()
		{
			FCursor.Params.Clear();
			try
			{
				InternalPrepareParams();
			}
			catch
			{
				FCursor.Params.Clear();
				throw;
			}
		}
		
		private void UnprepareParams()
		{
			FCursor.Params.Clear();
		}

		protected virtual void SetParamValues()
		{
			foreach (DataSetDataParam LParam in FCursor.Params)
				LParam.Bind(Process);
		}
		
		private void GetParamValues()
		{
			foreach (DataSetDataParam LParam in FCursor.Params)
				if (LParam is SourceDataSetDataParam)
					if (LParam.Modifier == Modifier.Var)
						((SourceDataSetDataParam)LParam).SourceParam.Value = LParam.Value;
		}
		
		/// <summary> Populates a given a (non null) DataParams collection with the actual params used by the DataSet. </summary>
		public void GetAllParams(DAE.Runtime.DataParams AParams)
		{
			CheckActive();
			foreach (DataSetDataParam LParam in FCursor.Params)
				AParams.Add(LParam);
		}

		/// <summary> Returns the list of the actual params used by the DataSet. </summary>
		/// <remarks> The user should not modify the list of values of these parameters. </remarks>
		[Browsable(false)]
		public DAE.Runtime.DataParams AllParams
		{
			get { return FCursor.Params; }
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
			FCursor.Reset();
		}

		protected override void InternalRefresh(Row ARow)
		{
			FCursor.Refresh(ARow);
		}

		protected override void InternalFirst()
		{
			FCursor.First();
		}

		protected override void InternalLast()
		{
			FCursor.Last();
		}
		
		protected override bool InternalNext()
		{
			return FCursor.Next();
		}
		
		protected override bool InternalPrior()
		{
			return FCursor.Prior();
		}

		protected override void InternalSelect(Row ARow)
		{
			FCursor.Select(ARow);
		}
		
		protected override bool InternalGetBOF()
		{
			return FCursor.BOF();
		}

		protected override bool InternalGetEOF()
		{
			return FCursor.EOF();
		}

		protected override Guid InternalGetBookmark()
		{
			return FCursor.GetBookmark();
		}

		protected override bool InternalGotoBookmark(Guid ABookmark, bool AForward)
		{
			return FCursor.GotoBookmark(ABookmark, AForward);
		}

		protected override void InternalDisposeBookmark(Guid ABookmark)
		{
			FCursor.DisposeBookmark(ABookmark);
		}

		protected override void InternalDisposeBookmarks(Guid[] ABookmarks)
		{
			FCursor.DisposeBookmarks(ABookmarks);
		}

		protected override Row InternalGetKey()
		{
			return FCursor.GetKey();
		}

		protected override bool InternalFindKey(Row AKey)
		{
			return FCursor.FindKey(AKey);
		}

		protected override void InternalFindNearest(Row AKey)
		{
			FCursor.FindNearest(AKey);
		}
		
		protected virtual void InternalInsert(Row ARow)
		{
			GetEditCursor().Insert(ARow, FValueFlags);
		}
		
		protected virtual void InternalUpdate(Row ARow)
		{
			GetEditCursor().Update(ARow, FValueFlags);
		}
		
		protected virtual void BeginTransaction()
		{
			FProcess.BeginTransaction(FIsolationLevel);
		}
		
		protected virtual void PrepareTransaction()
		{
			FProcess.PrepareTransaction();
		}
		
		protected virtual void CommitTransaction()
		{
			FProcess.CommitTransaction();
		}
		
		protected override void InternalPost(Row ARow)
		{
			// TODO: Test to ensure Optimistic concurrency check.
			bool LUpdateSucceeded = false;
			BeginTransaction();
			try
			{
				if (State == DataSetState.Insert)
					InternalInsert(ARow);
				else
					InternalUpdate(ARow);
									
				LUpdateSucceeded = true;
				
				PrepareTransaction();
				CommitTransaction();
			}
			catch (Exception E)
			{
				try
				{
					if (FProcess.InTransaction)
						FProcess.RollbackTransaction();
					if ((State == DataSetState.Edit) && LUpdateSucceeded)
						GetEditCursor().Refresh(FOriginalRow);
				}
				catch (Exception LRollbackException)
				{
					throw new DAE.Server.ServerException(DAE.Server.ServerException.Codes.RollbackError, E, LRollbackException.ToString());
				}
				throw;
			}
		}

		protected override void InternalDelete()
		{
			FCursor.Delete();
		}
		
		#endregion

		#region class DAECursor
		
		protected delegate void CursorErrorsOccurredHandler(DAECursor ACursor, CompilerMessages AMessages);

		protected class DAECursor : Disposable
		{
			public DAECursor(IServerProcess AProcess) : base()
			{
				FProcess = AProcess;
				FParams = new Runtime.DataParams();
			}

			protected override void Dispose(bool ADisposing)
			{
				Close();
				Unprepare();
				FProcess = null;
				base.Dispose(ADisposing);
			}

			private string FExpression;
			public string Expression
			{
				get { return FExpression; }
				set { FExpression = value; }
			}

			private Runtime.DataParams FParams;
			public Runtime.DataParams Params { get { return FParams; } }

			private IServerProcess FProcess;
			public IServerProcess Process { get { return FProcess; } }
			
			private IServerExpressionPlan FPlan;
			public IServerExpressionPlan Plan 
			{ 
				get 
				{ 
					InternalPrepare();
					return FPlan; 
				} 
			}
			
			private Schema.TableVar FTableVar;
			public Schema.TableVar TableVar
			{
				get
				{
					if (FTableVar == null)
						InternalPrepare();
					return FTableVar;
				}
			}
			
			public event CursorErrorsOccurredHandler OnErrors;
			private void ErrorsOccurred(CompilerMessages AErrors)
			{
				if (OnErrors != null)
					OnErrors(this, AErrors);
			}
			
			private IServerCursor FCursor;
			
			/// <summary>Update cursor used to invoke updates and proposables if ShouldOpen is false.</summary>
			private IServerCursor FUpdateCursor;
			
			private void EnsureUpdateCursor()
			{
				if (FUpdateCursor == null)
					FUpdateCursor = Plan.Open(FParams);
			}
			
			private void CloseUpdateCursor()
			{
				if (FUpdateCursor != null)
				{
					FPlan.Close(FUpdateCursor);
					FUpdateCursor = null;
				}
			}
			
			private bool FShouldOpen = true;
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
				get { return FShouldOpen; } 
				set { FShouldOpen = value; } 
			}
			
			private void InternalPrepare()
			{
				if (FPlan == null)
				{
					FPlan = FProcess.PrepareExpression(FExpression, FParams);
					try
					{
						ErrorsOccurred(FPlan.Messages);

						if (!(FPlan.DataType is Schema.ITableType))
							throw new ClientException(ClientException.Codes.InvalidResultType, FPlan.DataType == null ? "<invalid expression>" : FPlan.DataType.Name);
							
						FTableVar = FPlan.TableVar;
					}
					catch
					{
						FPlan = null;
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
				if (FPlan != null)
				{
					if (FCursor != null)
					{
						try
						{
							FProcess.CloseCursor(FCursor);
						}
						finally
						{
							FCursor = null;
						}
					}
					else
					{
						try
						{
							FProcess.UnprepareExpression(FPlan);
						}
						finally
						{
							FPlan = null;
						}
					}
				}
			}

			public void Open()
			{
				if (FShouldOpen && (FCursor == null))
				{
					if (FPlan == null)
					{
						try
						{
							FCursor = FProcess.OpenCursor(FExpression, FParams);
						}
						catch (Exception LException)
						{
							CompilerMessages LMessages = new CompilerMessages();
							LMessages.Add(LException);
							ErrorsOccurred(LMessages);
							throw LException;
						}
						
						FPlan = FCursor.Plan;
						FTableVar = FPlan.TableVar;
						ErrorsOccurred(FPlan.Messages);
                    }
					else
						FCursor = FPlan.Open(FParams);
				}
			}

			public void Close()
			{
				CloseUpdateCursor();

				if (FCursor != null)
				{
					if (FPlan != null)
					{
						try
						{
							FPlan.Close(FCursor);
						}
						finally
						{
							FCursor = null;
						}
					}
				}
			}
			
			public void Reset()
			{
				if (FCursor != null)
					FCursor.Reset();
			}
			
			public bool Next()
			{
				if (FCursor != null)
					return FCursor.Next();
				else
					return false;
			}
			
			public void Last()
			{
				if (FCursor != null)
					FCursor.Last();
			}
			
			public bool BOF()
			{
				if (FCursor != null)
					return FCursor.BOF();
				else
					return true;
			}
			
			public bool EOF()
			{
				if (FCursor != null)
					return FCursor.EOF();
				else
					return true;
			}
			
			public bool IsEmpty()
			{
				if (FCursor != null)
					return FCursor.IsEmpty();
				else
					return true;
			}
			
			public Row Select()
			{
				if (FCursor != null)
					return FCursor.Select();
				else
					return null;
			}
			
			public void Select(Row ARow)
			{
				if (FCursor != null)
					FCursor.Select(ARow);
			}
			
			public void First()
			{
				if (FCursor != null)
					FCursor.First();
			}
			
			public bool Prior()
			{
				if (FCursor != null)
					return FCursor.Prior();
				else
					return false;
			}
			
			public Guid GetBookmark()
			{
				if (FCursor != null)
					return FCursor.GetBookmark();
				else
					return Guid.Empty;
			}

			public bool GotoBookmark(Guid ABookmark, bool AForward)
			{
				if (FCursor != null)
					return FCursor.GotoBookmark(ABookmark, AForward);
				else
					return false;
			}
			
			public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
			{
				if (FCursor != null)
					return FCursor.CompareBookmarks(ABookmark1, ABookmark2);
				else
					return -1;
			}
			
			public void DisposeBookmark(Guid ABookmark)
			{
				if (FCursor != null)
					FCursor.DisposeBookmark(ABookmark);
			}
			
			public void DisposeBookmarks(Guid[] ABookmarks)
			{
				if (FCursor != null)
					FCursor.DisposeBookmarks(ABookmarks);
			}
			
			public Schema.Order Order 
			{ 
				get 
				{ 
					if (FCursor != null)
						return FCursor.Order;
					
					InternalPrepare();
					return FPlan.Order;
				} 
			}
			
			public Row GetKey()
			{
				if (FCursor != null)
					return FCursor.GetKey();
				else
					return null;
			}
			
			public bool FindKey(Row AKey)
			{
				if (FCursor != null)
					return FCursor.FindKey(AKey);
				else
					return false;
			}
			
			public void FindNearest(Row AKey)
			{
				if (FCursor != null)
					FCursor.FindNearest(AKey);
			}
			
			public bool Refresh(Row ARow)
			{
				if (FCursor != null)
					return FCursor.Refresh(ARow);
				else
					return false;
			}
			
			public void Insert(Row ARow, BitArray AValueFlags)
			{
				if (FCursor != null)
					FCursor.Insert(ARow, AValueFlags);
				else
				{
					EnsureUpdateCursor();
					FUpdateCursor.Insert(ARow, AValueFlags);
				}
			}
			
			public void Update(Row ARow, BitArray AValueFlags)
			{
				if (FCursor != null)
					FCursor.Update(ARow, AValueFlags);
			}
			
			public void Delete()
			{
				if (FCursor != null)
					FCursor.Delete();
			}
			
			public int RowCount()
			{
				if (FCursor != null)
					return FCursor.RowCount();
				else
					return 0;
			}
			
			public bool Default(Row ARow, string AColumnName)
			{
				if (FCursor != null)
					return FCursor.Default(ARow, AColumnName);
				else
				{
					EnsureUpdateCursor();
					return FUpdateCursor.Default(ARow, AColumnName);
				}
			}
			
			public bool Change(Row AOldRow, Row ANewRow, string AColumnName)
			{
				if (FCursor != null)
					return FCursor.Change(AOldRow, ANewRow, AColumnName);
				else
				{
					EnsureUpdateCursor();
					return FUpdateCursor.Change(AOldRow, ANewRow, AColumnName);
				}
			}
			
			public bool Validate(Row AOldRow, Row ANewRow, string AColumnName)
			{
				if (FCursor != null)
					return FCursor.Validate(AOldRow, ANewRow, AColumnName);
				else
				{
					EnsureUpdateCursor();
					return FUpdateCursor.Validate(AOldRow, ANewRow, AColumnName);
				}
			}
		}
		
		#endregion
	}

	public delegate void ParamStructureChangedHandler(object ASender);
	public delegate void ParamChangedHandler(object ASender);
	
	public class DataSetParam : object
	{
		private string FName;
		public string Name
		{
			get { return FName; }
			set 
			{ 
				if (FName != value)
				{
					FName = value == null ? String.Empty : value;
					ParamStructureChanged();
				}
			}
		}
		
		private string FColumnName;
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FColumnName != value)
				{
					FColumnName = value == null ? String.Empty : value;
					ParamChanged();
				}
			}
		}
		
		private Schema.IDataType FDataType;
		public Schema.IDataType DataType
		{
			get { return FDataType; }
			set 
			{ 
				if (FDataType != value)
				{
					FDataType = value; 
					ParamStructureChanged();
				}
			}
		}
		
		private Modifier FModifier;
		public Modifier Modifier
		{
			get { return FModifier; }
			set
			{
				if (FModifier != value)
				{
					FModifier = value;
					ParamStructureChanged();
				}
			}
		}
		
		private object FValue;
		public object Value
		{
			get { return FValue; }
			set
			{
				if (FValue != value)
				{
					FValue = value;
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
		protected override void Adding(DataSetParam AValue, int AIndex)
		{
			//base.Adding(AValue, AIndex);
			AValue.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			AValue.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			DataSetParamStructureChanged(null);
		}
		
		protected override void Removing(DataSetParam AValue, int AIndex)
		{
			AValue.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
			AValue.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
			DataSetParamStructureChanged(null);
			//base.Removing(AValue, AIndex);
		}
	#endif
		
		private void DataSetParamChanged(object ASender)
		{
			ParamChanged(ASender);
		}
		
		private void DataSetParamStructureChanged(object ASender)
		{
			ParamStructureChanged(ASender);
		}
		
		public event ParamStructureChangedHandler OnParamStructureChanged;
		private void ParamStructureChanged(object ASender)
		{
			if (OnParamStructureChanged != null)
				OnParamStructureChanged(ASender);
		}
		
		public event ParamChangedHandler OnParamChanged;
		private void ParamChanged(object ASender)
		{
			if (OnParamChanged != null)
				OnParamChanged(ASender);
		}
	}

	public class DataSetParamGroup : Disposable
	{
		public DataSetParamGroup() : base()
		{
			FMasterLink = new DataLink();
			FMasterLink.OnRowChanged += new DataLinkFieldHandler(MasterRowChanged);
			FMasterLink.OnDataChanged += new DataLinkHandler(MasterDataChanged);
			FMasterLink.OnActiveChanged += new DataLinkHandler(MasterActiveChanged);
			
			FParams = new DataSetParams();
			FParams.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			FParams.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FParams != null)
			{
				FParams.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
				FParams.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
				FParams = null;
			}
			
			if (FMasterLink != null)
			{
				FMasterLink.OnRowChanged -= new DataLinkFieldHandler(MasterRowChanged);
				FMasterLink.OnDataChanged -= new DataLinkHandler(MasterDataChanged);
				FMasterLink.OnActiveChanged -= new DataLinkHandler(MasterActiveChanged);
				FMasterLink.Dispose();
				FMasterLink = null;
			}

			base.Dispose(ADisposing);
		}

		private DataSetParams FParams;
		public DataSetParams Params { get { return FParams; } }
		
		// DataSource
		private DataLink FMasterLink;
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Parameter data source")]
		public DataSource Source
		{
			get { return FMasterLink.Source; }
			set
			{
				if (FMasterLink.Source != value)
				{
					FMasterLink.Source = value;
					ParamChanged();
				}
			}
		}

		private void MasterActiveChanged(DataLink ALink, DataSet ADataSet)
		{
			ParamChanged();
		}

		private void MasterRowChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			ParamChanged();
		}
		
		private void MasterDataChanged(DataLink ALink, DataSet ADataSet)
		{
			ParamChanged();
		}

		private void DataSetParamStructureChanged(object ASender)
		{
			ParamStructureChanged();
		}
		
		private void DataSetParamChanged(object ASender)
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
		protected override void Adding(DataSetParamGroup AValue, int AIndex)
		{
			//base.Adding(AValue, AIndex);
			AValue.OnParamChanged += new ParamChangedHandler(DataSetParamChanged);
			AValue.OnParamStructureChanged += new ParamStructureChangedHandler(DataSetParamStructureChanged);
			DataSetParamStructureChanged(null);
		}
		
		protected override void Removing(DataSetParamGroup AValue, int AIndex)
		{
			AValue.OnParamStructureChanged -= new ParamStructureChangedHandler(DataSetParamStructureChanged);
			AValue.OnParamChanged -= new ParamChangedHandler(DataSetParamChanged);
			DataSetParamStructureChanged(null);
			//base.Removing(AValue, AIndex);
		}
	#endif
		
		private void DataSetParamChanged(object ASender)
		{
			ParamChanged(ASender);
		}
		
		private void DataSetParamStructureChanged(object ASender)
		{
			ParamStructureChanged(ASender);
		}
		
		public event ParamStructureChangedHandler OnParamStructureChanged;
		protected virtual void ParamStructureChanged(object ASender)
		{
			if (OnParamStructureChanged != null)
				OnParamStructureChanged(ASender);
		}
		
		public event ParamChangedHandler OnParamChanged;
		protected virtual void ParamChanged(object ASender)
		{
			if (OnParamChanged != null)
				OnParamChanged(ASender);
		}
	}

	internal abstract class DataSetDataParam : Runtime.DataParam
	{
		public DataSetDataParam(string AName, Schema.IDataType ADataType, Modifier AModifier) : base(AName, ADataType, AModifier) {}
		
		public abstract void Bind(IServerProcess AProcess);
	}
	
	internal class MasterDataSetDataParam : DataSetDataParam
	{
		public MasterDataSetDataParam(string AName, Schema.IDataType ADataType, Modifier AModifier, string AColumnName, DataSource ASource, bool AIsMaster) : base(AName, ADataType, AModifier)
		{
			ColumnName = AColumnName;
			Source = ASource;
			IsMaster = AIsMaster;
		}
		
		private string ColumnName;
		public DataSource Source;
		public bool IsMaster; // true if this parameter is part of a master/detail relationship
		
		public override void Bind(IServerProcess AProcess)
		{
			if (!Source.DataSet.IsEmpty() && Source.DataSet.Fields[ColumnName].HasValue())
				Value = Source.DataSet.Fields[ColumnName].AsNative;
		}
	}
	
	internal class SourceDataSetDataParam : DataSetDataParam
	{
		public SourceDataSetDataParam(DataSetParam ASourceParam) : base(ASourceParam.Name, ASourceParam.DataType, ASourceParam.Modifier) 
		{
			SourceParam = ASourceParam;
		}
		
		public DataSetParam SourceParam;
		
		public override void Bind(IServerProcess AProcess)
		{
			Value = SourceParam.Value;
		}
	}
}

