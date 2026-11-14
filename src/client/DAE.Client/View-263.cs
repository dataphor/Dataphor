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

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client.Design;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Client
{
	/// <summary> Data navigation and modification component. </summary>
	///	<remarks> Maintains a sized buffer of Rows based on the needs of linked dependants. </remarks>    
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DataView),"Icons.DataView.bmp")]
	[DesignerSerializer(typeof(Alphora.Dataphor.DAE.Client.Design.ActiveLastSerializer), typeof(CodeDomSerializer))]
	public class DataView : Component, IDisposableNotify, ISupportInitialize, IEnumerable
	{
		// Do not localize
		public const string CParamNamespace = "MasterDataView";
		public readonly static Char[] CColumnNameDelimiters = new Char[] {',',';'};

		private BitVector32 FFlags = new BitVector32();
		private static readonly int DisposingMask = BitVector32.CreateMask();
		private static readonly int InitializingMask = BitVector32.CreateMask(DisposingMask);
		private static readonly int DelayedActiveMask = BitVector32.CreateMask(InitializingMask);
		private static readonly int BOFMask = BitVector32.CreateMask(DelayedActiveMask);
		private static readonly int EOFMask = BitVector32.CreateMask(BOFMask);
		private static readonly int UseBrowseMask = BitVector32.CreateMask(EOFMask);
		private static readonly int IsModifiedMask = BitVector32.CreateMask(UseBrowseMask);
		private static readonly int IsReadOnlyMask = BitVector32.CreateMask(IsModifiedMask);
		private static readonly int UseApplicationTransactionsMask = BitVector32.CreateMask(IsReadOnlyMask);
		private static readonly int JoinAsInsertMask = BitVector32.CreateMask(UseApplicationTransactionsMask);
		private static readonly int WriteWhereClauseMask = BitVector32.CreateMask(JoinAsInsertMask);
		private static readonly int IsJoinedMask = BitVector32.CreateMask(WriteWhereClauseMask);
		
		public DataView() : base()
		{
			InternalInitialize();
		}

		public DataView(IContainer AContainer)
		{
			InternalInitialize();
			if (AContainer != null)
				AContainer.Add(this);
		}

		private void InternalInitialize()
		{
			FBuffer = new ViewBuffer();
			FSources = new ArrayList();
			BufferClear();
			FFields = new Schema.Objects(10);
			FMasterLink = new DataLink();
			FMasterLink.OnDataChanged += new DataLinkHandler(MasterDataChanged);
			FMasterLink.OnRowChanged += new DataLinkFieldHandler(MasterRowChanged);
			FMasterLink.OnStateChanged += new DataLinkHandler(MasterStateChanged);
			FMasterLink.OnPrepareToPost += new DataLinkHandler(MasterPrepareToPost);
			FMasterLink.OnPrepareToCancel += new DataLinkHandler(MasterPrepareToCancel);
			FParamGroups = new DataViewParamGroups();
			FParamGroups.OnParamChanged += new ParamChangedHandler(DataViewParamChanged);
			FParamGroups.OnParamStructureChanged += new ParamStructureChangedHandler(DataViewParamStructureChanged);
			FCursorType = CursorType.Dynamic;
			FRequestedIsolation = CursorIsolation.Browse;
			FRequestedCapabilities = CursorCapability.Navigable | CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Updateable;
			InternalUseBrowse = true;
			InternalWriteWhereClause = true;
			UseApplicationTransactions = true;
			FShouldEnlist = EnlistMode.Default;
		}

		protected override void Dispose(bool ADisposing)
		{
			InternalDisposing = true;
			try
			{
				Close();
			}
			finally
			{
				try
				{
					base.Dispose(ADisposing);
				}
				finally
				{
					try
					{
						if (FParamGroups != null)
						{
							FParamGroups.OnParamStructureChanged -= new ParamStructureChangedHandler(DataViewParamStructureChanged);
							FParamGroups.OnParamChanged -= new ParamChangedHandler(DataViewParamChanged);
							FParamGroups.Dispose();
							FParamGroups = null;
						}
					}
					finally
					{
						try
						{
							if (FMasterLink != null)
							{
								FMasterLink.OnPrepareToPost -= new DataLinkHandler(MasterPrepareToPost);
								FMasterLink.OnStateChanged -= new DataLinkHandler(MasterStateChanged);
								FMasterLink.OnRowChanged -= new DataLinkFieldHandler(MasterRowChanged);
								FMasterLink.OnDataChanged -= new DataLinkHandler(MasterDataChanged);
								FMasterLink.Dispose();
								FMasterLink = null;
							}
						}
						finally
						{
							if (FFields != null)
							{
								FFields.Clear();
								FFields = null;
							}
							
							try
							{
								Session = null;

								if (FSources != null)
								{
									while(FSources.Count != 0)
										((DataSource)FSources[0]).View = null;
									FSources = null;
								}
							}
							finally
							{
								try
								{
									ClearOriginalRow();
								}
								finally
								{
									try
									{
										if (FBuffer != null)
										{
											FBuffer.Dispose();
											FBuffer = null; 
										}
									}
									finally
									{
										if (FDataSource != null)
										{
											FDataSource.Dispose();
											FDataSource = null;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		
		private bool InternalDisposing
		{
			get { return FFlags[DisposingMask]; }
			set { FFlags[DisposingMask] = value; }
		}
		
		#region Session

		private bool ContainerContainsSession(DataSessionBase ASession)
		{
			if ((ASession == null) || (Container == null))
				return false;
			foreach (Component LComponent in Container.Components)
				if (LComponent == ASession)
					return true;
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
		private DataSessionBase FSession;
		/// <summary> Attached to a DataSession object for connection to a Server. </summary>
		[Category("Data")]
		[Description("Connection to a Server.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public DataSessionBase Session
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
						FSession.Disposed -= new EventHandler(SessiDisposedd);
					}
					FSession = value;
					if (FSession != null)
					{
						FSession.OnClosing += new EventHandler(SessionClosing);
						FSession.Disposed += new EventHandler(SessiDisposedd);
					}
				}
			}
		}

		[Category("Data")]
		[Description("Connection to a Server.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.SessionEditor), typeof(System.Drawing.Design.UITypeEditor))]
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
						Session = DataSessionBase.Sessions[value];
				}
			}
		}
		
		private void SessiDisposedd(object ASender, EventArgs AArgs)
		{
			Session = null;
		}

		private void SessionClosing(object ASender, EventArgs AArgs)
		{
			Close();
		}

		#endregion

		#region Buffer Maintenance

		// <summary> Row buffer.  Always contains at least two Rows. </summary>
		internal ViewBuffer FBuffer;

		// <summary> The number of "filled" rows in the buffer. </summary>
		private int FEndOffset;

		/// <summary> Offset of current Row from origin of buffer. </summary>
		private int FCurrentOffset;

		/// <summary> Offset of active Row from origin of buffer. </summary>
		internal int FActiveOffset;

		// Tracks the original row during an edit
		private Row FOriginalRow;

		/// <summary> The actively selected for of the DataView's buffer. </summary>
		/// <remarks> This row should be treated as read-only. </remarks>
		[Browsable(false)]
		public Row ActiveRow
		{
			get { return FBuffer[FActiveOffset].Row; }
		}
		
		private Row FOldRow;
		/// <summary> The old row for the current row changed event. </summary>
		[Browsable(false)]
		public Row OldRow
		{
			get { return FOldRow; }
		}

		/// <summary> Gets the maximum number of rows that are buffered for active controls on this DataView. </summary>
		/// <remarks> This may be useful for determining MoveBy deltas when the desire is to move by "pages". </remarks>
		[Browsable(false)]
		public int BufferCount
		{
			get { return FBuffer.Count - 1; }
		}

		internal int EndOffset
		{
			get { return FEndOffset; }
		}

		private Row RememberActive()
		{
			if (FActiveOffset <= FEndOffset)
				return (Row)FBuffer[FActiveOffset].Row.Copy();
			else
				return null;
		}

		private void ClearOriginalRow()
		{
			if (FOriginalRow != null)
			{
				FOriginalRow.Dispose();
				FOriginalRow = null;
			}
		}
		
		/// <summary> Used when the cursor no longer is in sync with the current row offset value. </summary>
		private void CurrentReset()
		{
			FCurrentOffset = -1;
		}
		
		/// <summary> Sets the underlying cursor to the active row. </summary>
		/// <param name="AStrict"> If true, will throw and exception if unable to position on the buffered row. </param>
		private void CurrentGotoActive(bool AStrict)
		{
			if (FEndOffset > -1)
				CurrentGoto(FActiveOffset, AStrict);
		}
		
		/// <summary> Sets the underlying cursor to a specific buffer row. </summary>
		/// <param name="AStrict"> If true, will throw and exception if unable to position on the buffered row. </param>
		private void CurrentGoto(int AValue, bool AStrict)
		{
			if (FCurrentOffset != AValue)
			{
				RowFlag LFlag = FBuffer[AValue].RowFlag;
				
				// If exception is thrown, current should still be valid (cursor should not move if unable to comply)
				if ((LFlag & RowFlag.Data) != 0)
				{
					bool LResult = FCursor.GotoBookmark(FBuffer[AValue].Bookmark);
					if (AStrict && !LResult)
						throw new ClientException(ClientException.Codes.BookmarkNotFound);
				}
				else if ((LFlag & RowFlag.BOF) != 0)
					FCursor.First();
				else if ((LFlag & RowFlag.EOF) != 0)
					FCursor.Last();

				FCurrentOffset = AValue;
			}
		}

		private void FinalizeBuffers(int LFirst, int LLast)
		{
			if (FCursor != null)
			{
				Guid[] LBookmarks = new Guid[LLast - LFirst + 1];
				for (int i = LFirst; i <= LLast; i++)
				{
					LBookmarks[i - LFirst] = FBuffer[i].Bookmark;
					FBuffer[i].Bookmark = Guid.Empty;
					FBuffer[i].Row.ClearValues();
				}
				FCursor.DisposeBookmarks(LBookmarks);
			}
		}

		private void FinalizeBuffer(ViewRow ARow)
		{
			if (ARow.Bookmark != Guid.Empty)
			{
				FCursor.DisposeBookmark(ARow.Bookmark);
				ARow.Bookmark = Guid.Empty;
			}
			ARow.Row.ClearValues();
		}

		/// <summary> Selects values from cursor into specified Row of buffer. </summary>
		private void CursorSelect(int AIndex)
		{
			ViewRow LRow = FBuffer[AIndex];
			FinalizeBuffer(LRow);
			FCursor.Select(LRow.Row);
			LRow.Bookmark = FCursor.GetBookmark();
			LRow.RowFlag = RowFlag.Data;
		}

		private void BufferClear()
		{
			FEndOffset = -1;
			FActiveOffset = 0;
			FCurrentOffset = -1;
			InternalBOF = true;
			InternalEOF = true;
		}
		
		private void BufferActivate()
		{
			FEndOffset = 0;
			FActiveOffset = 0;
			FCurrentOffset = 0;
			InternalBOF = false;
			InternalEOF = false;
		}
		
		/// <summary> Reads a row into the end of the buffer, scrolling if necessary. </summary>
		/// <remarks> This call will adjust FCurrentOffset, FActiveOffset, and FEndOffset appropriately. </remarks>
		private bool CursorNextRow()
		{
			bool LGetNext = true;

			// Navigate to the last occupied row in the buffer
			if (FEndOffset >= 0)
			{
				if ((FBuffer[FEndOffset].RowFlag & RowFlag.EOF) != 0)
					return false;

				CurrentGoto(FEndOffset, false);
			
				// Skip the move next if the row in question is being inserted (it's cursor info is a duplicate of the preceeding row)
				if ((FState == DataSetState.Insert) && (FCurrentOffset == FActiveOffset))
					LGetNext = false;
			}

			// Attempt to navigate to the next row
			if (LGetNext && !FCursor.Next())
			{
				CurrentReset();
				if (FEndOffset >= 0)
					FBuffer[FEndOffset].RowFlag |= RowFlag.EOF;
				return false;
			}

			if (FEndOffset == -1)
			{
				// Initially active the buffer
				CursorSelect(0);
				BufferActivate();
			}
			else
			{
				if (FEndOffset < (FBuffer.Count - 2))
				{
					// Add an additional row to the buffer
					FEndOffset++;
					CursorSelect(FEndOffset);
				}
				else
				{
					// Scroll the buffer, there are no more available rows
					CursorSelect(FEndOffset + 1);
					FBuffer.Move(0, FEndOffset + 1);
					FActiveOffset--;		// Note that this could potentially place the active offset outside of the buffer boundary (the caller should account for this)
				}
			}
			FCurrentOffset = FEndOffset;
			return true;
		}
		
		/// <summary> Reads rows onto the end of the buffer until the buffer is full. </summary>
		private int CursorNextRows()
		{
			if (FOpenState == DataSetState.Browse)
			{
				int LResult = 0;
				while ((FEndOffset < (FBuffer.Count - 2)) && CursorNextRow())
					LResult++;
				return LResult;
			}
			else if ((FOpenState == DataSetState.Edit) && (FEndOffset < 0) && CursorNextRow())
				return 1;
			else
				return 0;
		}

		/// <summary> Reads a row into the beginning of the buffer scrolling others down. </summary>
		/// <remarks> This call will adjust FCurrentOffset, FActiveOffset, and FEndOffset appropriately. </remarks>
		private bool CursorPriorRow()
		{
			// Navigate to the first row in the buffer (if we have one)
			if (FEndOffset > -1)
			{
				if ((FBuffer[0].RowFlag & RowFlag.BOF) != 0)
					return false;

				CurrentGoto(0, false);
			}
		
			// Attempt to navigate to the prior row
			if (!FCursor.Prior())
			{
				CurrentReset();
				if (FEndOffset > -1)
					FBuffer[0].RowFlag |= RowFlag.BOF;
				return false;
			}
		
			// Select a row into the scratchpad row
			CursorSelect(FEndOffset + 1);
		
			if (FEndOffset == -1)
				BufferActivate();	// Initially activate the buffer
			else
			{
				// Move the scratchpad row into the first slot of the buffer and updated the other offsets
				FBuffer.Move(FEndOffset + 1, 0);
				if (FEndOffset < (FBuffer.Count - 2))
					FEndOffset++;
				FActiveOffset++;	// Note that this could potentially place the active offset outside of the buffer boundary (the caller should account for this)
			}

			FCurrentOffset = 0;		
			
			return true;
		}
		
		/// <summary> Reads records onto the beginning of the buffer until it is full. </summary>
		private int CursorPriorRows()
		{
			int LResult = 0;
			if (FOpenState == DataSetState.Browse)
				while ((FEndOffset < (FBuffer.Count - 2)) && CursorPriorRow())
					LResult++;
			return LResult;
		}
		
		/// <summary> Resyncronizes the row buffer from the cursor. </summary>
		private void Resync(bool AExact, bool ACenter)
		{
			if (FOpenState != DataSetState.Browse)
				FCursor.Close();
			else
			{
				if (AExact)
				{
					CurrentReset();
					if (FCursor.BOF() || FCursor.EOF())
						throw new ClientException(ClientException.Codes.RecordNotFound);
				}
				else
				{
					// This define forces the server cursor to recognize the BOF and EOF flags by actually navigating
					// If RELYONCURSORFLAGSAFTERUPDATE is defined, the view assumes that the BOF and EOF flags are set correctly after an update,
					// which is not always the case. This is a bug with the DAE, and when it is fixed, we can take advantage of this optimization in the view.
					#if !RELYONCURSORFLAGSAFTERUPDATE
					if (FCursor.BOF())
						FCursor.Next();
					if (FCursor.EOF())
						FCursor.Prior();
					#endif
					if (FCursor.BOF() || FCursor.EOF())
					{
						BufferClear();
						DataChanged();
						return;
					}
					#if RELYONCURSORFLAGSAFTERUPDATE
					if (FCursor.BOF())
						FCursor.Next();
					if (FCursor.EOF())
						FCursor.Prior();
					#endif
				}
				CursorSelect(0);
				int LOffset;
				if (ACenter)
					LOffset = (FBuffer.Count - 2) / 2;
				else
					LOffset = FActiveOffset;
				BufferActivate();

				try
				{
					while ((LOffset > 0) && CursorPriorRow())
						LOffset--;
					CursorNextRows();
					CursorPriorRows();
				}
				finally
				{
					DataChanged();
				}
			}
		}

		/// <summary> Reactivates the buffer, optionally from a row. </summary>
		/// <param name="ARow"> An optional row indicating a row to sync to. </param>
		private void Resume(Row ARow)
		{
			if (ARow != null)
				FindNearest(ARow);
			else
			{
				BufferClear();
				try
				{
					CursorNextRows();
					if (FEndOffset >= 0)
						FBuffer[0].RowFlag |= RowFlag.BOF;
				}
				finally
				{
					InternalBOF = true;
					DataChanged();
				}
			}
		}

		/// <summary> Updates the number and values of rows in the buffer. </summary>
		/// <param name="AFirst"> When true, no attempt is made to fill the datasource "backwards". </param>
		internal void BufferUpdateCount(bool AFirst)
		{
			int i;
			DataLink[] LLinks = EnumerateLinks();

			// Determine the range of buffer utilization from the links
			int LMin = FActiveOffset;
			int LMax = FActiveOffset;
			foreach (DataLink LLink in LLinks)
			{
				LLink.UpdateRange();	// Make sure the link's virtual buffer range encompasses the active row
				LMin = Math.Min(LMin, LLink.FFirstOffset);
				LMax = Math.Max(LMax, (LLink.FFirstOffset + (LLink.BufferCount - 1)));
			}

			if ((LMin != 0) || (LMax != (FBuffer.Count - 2)))
			{
				// Add the necessary rows to reach the new capacity
				FBuffer.Add(Math.Max(0, (LMax - LMin) - (FBuffer.Count - 2)), this);

				// Adjust the beginning of the buffer
				int LOffsetDelta;
				if (LMin > 0)
				{
					// Remove any unneeded rows from the beginning of the buffer
					LOffsetDelta = -LMin;
					FinalizeBuffers(0, LMin - 1);
					for (i = 0; i < LMin; i++)
						FBuffer.RemoveAt(0);
					if (FCurrentOffset > LMin)
						CurrentReset();
					else
						FCurrentOffset += LOffsetDelta;
					FActiveOffset += LOffsetDelta;
					FEndOffset += LOffsetDelta;
				}
				else
				{
					// Add any newly needed rows to the bottom of the buffer
					LOffsetDelta = 0;
					if (!AFirst)
						for (i = 0; i > LMin; i--)
							if (!CursorPriorRow())
								break;
							else
								LOffsetDelta++;
				}

				// Adjust the end of the buffer
				int LNewEnd = LMax - LMin;
				if (LNewEnd < (FBuffer.Count - 2))
				{
					// Shrink the end of the buffer
					FinalizeBuffers(LNewEnd + 1, (FBuffer.Count - 2));
					for (i = (FBuffer.Count - 2); i > LNewEnd; i--)
						FBuffer.RemoveAt(i);
					if (FCurrentOffset > LNewEnd)
						CurrentReset();
					if (FEndOffset > LNewEnd)
						FEndOffset = LNewEnd;
				}
				// Grow the end of the buffer if necessary
				if (FOpenState == DataSetState.Browse)
					CursorNextRows();
				else if ((FOpenState == DataSetState.Edit) && AFirst)
					CursorNextRow();

				// Indicate that the the first row is BOF (if we are told that the cursor is located at the first row)
				if (AFirst && (FEndOffset >= 0))
					FBuffer[0].RowFlag |= RowFlag.BOF;

				// Delta each DataLink to maintain the same relative position within their buffers
				if (LOffsetDelta != 0)
					foreach (DataLink LLink in LLinks)
						LLink.UpdateFirstOffset(LOffsetDelta);
			}
		}
		
		#endregion

		#region DataLinks & Sources

		private ArrayList FSources;

		internal void AddSource(DataSource ADataSource)
		{
			FSources.Add(ADataSource);
			if (Active)
				BufferUpdateCount(false);
		}

		internal void RemoveSource(DataSource ADataSource)
		{
			FSources.Remove(ADataSource);
			if (Active)
				BufferUpdateCount(false);
		}

		/// <summary> Requests that the associated links make any pending updates to the DataView. </summary>
		public void RequestSave()
		{
			if ((FState == DataSetState.Edit) || (FState == DataSetState.Insert))
				foreach (DataLink LLink in EnumerateLinks())
					LLink.SaveRequested();
		}
		
		/// <summary> Returns the current set of DataLinks that are associated with all DataSources that reference this DataView. </summary>
		/// <returns> 
		///		It is important that this method generate an array, so that a snapshot of the links is used for iteration; 
		///		operations on the array may cause DataLinks to be created or destroyed. 
		///	</returns>
		private DataLink[] EnumerateLinks()
		{
			ArrayList LTempList = new ArrayList();
			foreach (DataSource LSource in FSources)
				foreach (DataLink LLink in LSource.FLinks)
					LTempList.Add(LLink);
			DataLink[] LResult = new DataLink[LTempList.Count];
			LTempList.CopyTo(LResult);
			return LResult;
		}
								
		#endregion

		#region Process

		// Process
		private IServerProcess FProcess;
		public IServerProcess Process { get { return FProcess; } }
		
		[Browsable(false)]
		public IStreamManager StreamManager 
		{ 
			get 
			{ 
				if (FProcess == null)
					throw new ClientException(ClientException.Codes.ProcessMissing);
				return (IStreamManager)FProcess; 
			} 
		}
		
		// IsolationLevel
		private IsolationLevel FIsolationLevel = IsolationLevel.Browse;
		[Category("Behavior")]
		[DefaultValue(IsolationLevel.Browse)]
		[Description("The isolation level for transactions performed by this view.")]
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
		private CursorIsolation FRequestedIsolation;
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
		private CursorCapability FRequestedCapabilities;
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

		#endregion

		#region Cursor

		// Cursor
		private ViewCursor FCursor;
		private ViewCursor FATCursor;
		
		private ViewCursor GetActiveCursor()
		{
			if (IsApplicationTransactionServer && (FOpenState == DataSetState.Browse))
				return FATCursor;
			else
				return FCursor;
		}

		// CursorType
		private CursorType FCursorType = CursorType.Dynamic;
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
		
		#endregion

		#region Schema

		// TableType
		[Browsable(false)]
		public Schema.TableType TableType { get { return (Schema.TableType)FCursor.Plan.DataType; } }
		
		// TableVar
		[Browsable(false)]
		public Schema.TableVar TableVar { get { return (Schema.TableVar)FCursor.Plan.TableVar; } }

		#endregion

		#region Master/Detail

		// DetailKey
		private Schema.Key FDetailKey;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Key DetailKey
		{
			get { return FDetailKey; }
			set
			{
				if (FDetailKey != value)
				{
					FDetailKey = value;
					if (Active)
						ExpressionChanged(null);
				}
			}
		}

		[DefaultValue("")]
		[Category("Behavior")]
		[Description("Detail key names")]
		public string DetailKeyNames
		{
			get { return GetNamesFromKey(FDetailKey); }
			set
			{
				if (DetailKeyNames != value)
					DetailKey = GetKeyFromNames(value);
			}
		}

		// MasterKey
		private Schema.Key FMasterKey;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Key MasterKey
		{
			get { return FMasterKey; }
			set
			{
				if (FMasterKey != value)
				{
					FMasterKey = value;
					if (Active)
						ExpressionChanged(null);
				}
			}
		}

		[DefaultValue("")]
		[Category("Behavior")]
		[Description("Master key names")]
		public string MasterKeyNames
		{
			get { return GetNamesFromKey(FMasterKey); }
			set
			{
				if (MasterKeyNames != value)
					MasterKey = GetKeyFromNames(value);
			}
		}
		
		// MasterSource
		private DataLink FMasterLink;
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Master source")]
		public DataSource MasterSource
		{
			get { return FMasterLink.Source; }
			set
			{
				if (FMasterLink.Source != value)
				{
					if (IsLinkedTo(value))
						throw new ClientException(ClientException.Codes.CircularLink);
					FMasterLink.Source = value;
				}
			}
		}

		public bool IsDetailKey(string AColumnName)
		{
			if (IsMasterSetup())
				return DetailKey.Columns.Contains(AColumnName);
			else
				return false;
		}

		private string[] GetInvariant()
		{
			// The invariant is the first non-empty intersection of any key of the master table type with the master key
			if (IsMasterSetup())
			{					
				ArrayList LInvariant = new ArrayList();
				foreach (Schema.Key LKey in MasterSource.View.TableVar.Keys)
				{
					foreach (Schema.TableVarColumn LColumn in LKey.Columns)
					{
						int LIndex = FMasterKey.Columns.IndexOfName(LColumn.Name);
						if (LIndex >= 0)
							LInvariant.Add(FDetailKey.Columns[LIndex].Name);
					}
					if (LInvariant.Count > 0)
						return (string[])LInvariant.ToArray(typeof(string));
				}
			}
			return new string[]{};
		}
		
		private void MasterStateChanged(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
			{
				if (IsDetail() && !IsMasterSetup())
					InternalClose();
				else
				{
					StateChanged(); // Broadcast a state change to detail data sets so they know to exit the A/T
					ExpressionChanged(null);
				}
			}
		}

		private void MasterPrepareToPost(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
				EnsureBrowseState();
		}

		private void MasterPrepareToCancel(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
				EnsureBrowseState(false);
		}

		private void MasterDataChanged(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
				ParameterChanged(null);
		}
		
		private void MasterRowChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (Active && ((AField == null) || MasterKey.Columns.Contains(AField.ColumnName)))
			{
				if (IsApplicationTransactionClient && !IsJoined)
					ExpressionChanged(null);
				else
					ParameterChanged(null);
			}
		}

		private bool IsLinkedTo(DataSource ASource)
		{
			DataView LView;
			while (ASource != null)
			{
				LView = ASource.View;
				if (LView == null)
					break;
				if (LView == this)
					return true;
				ASource = LView.MasterSource;
			}
			return false;
		}
		
		/// <summary> Returns true if the master is set up (see IsMasterSetup()), and there is a value for each of the master's columns (or WriteWhereClause is true). </summary>
		public bool IsMasterValid()
		{
			if (IsMasterSetup() && (!MasterSource.View.IsDetail() || MasterSource.View.IsMasterValid()) && !MasterSource.View.IsEmpty())
			{
				if (!WriteWhereClause) // If the where clause is custom, allow nil master values
					return true;
					
				foreach (DAE.Schema.TableVarColumn LColumn in FMasterKey.Columns)
					if (!(MasterSource.View.Fields[LColumn.Name].HasValue()))
						return false;
				return true;
			}
			return false;
		}
		
		private bool IsDetail()
		{
			return (MasterSource != null) || (MasterKey != null) || (DetailKey != null);
		}

		/// <summary> Returns true if the master exists, is active and it's schema in relation to the linked fields is known. </summary>
		public bool IsMasterSetup()
		{
			// Make sure that the master detail relationship is fully defined so that parameters can be built
			return
				(MasterSource != null) &&
				(MasterSource.View != null) &&
				(MasterSource.View.Active) &&
				(FMasterKey != null) &&
				(FDetailKey != null);
		}
		
		#endregion

		#region Order

		// Order
		private Schema.Order FOrder;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Order Order
		{
			get { return FOrder; }
			set
			{
				if (Active)
				{
					using (Row LRow = RememberActive())
					{
						FOrder = value;
						ExpressionChanged(LRow);
					}
				}
				else
					FOrder = value;
			}
		}
		
		public Schema.Order StringToOrder(string AOrder)
		{
			if (AOrder.IndexOf(Keywords.Key) >= 0)
			{
				KeyDefinition LKeyDefinition = (new Parser()).ParseKeyDefinition(AOrder);
				Schema.Order LOrder = new Schema.Order();
				foreach (KeyColumnDefinition LColumn in LKeyDefinition.Columns)
					LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[LColumn.ColumnName], true));
				return LOrder;
			}
			else
			{
				OrderDefinition LOrderDefinition = (new Parser()).ParseOrderDefinition(AOrder);
				Schema.Order LOrder = new Schema.Order();
				foreach (OrderColumnDefinition LColumn in LOrderDefinition.Columns)
					LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[LColumn.ColumnName], LColumn.Ascending, LColumn.IncludeNils));
				return LOrder;
			}
		}
		
		public Schema.Order OrderDefinitionToOrder(OrderDefinition AOrder)
		{
			Schema.Order LOrder = new Schema.Order(AOrder.MetaData);
			foreach (OrderColumnDefinition LColumn in AOrder.Columns)
				LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[LColumn.ColumnName], LColumn.Ascending, LColumn.IncludeNils));
			return LOrder;
		}
		
		// OrderString
		[DefaultValue("")]
		[Browsable(false)]
		public string OrderString
		{
			get 
			{
				if (!Active)
					return (FOrderDefinition == null) ? String.Empty : new D4TextEmitter().Emit(FOrderDefinition);
				else
					return Order != null ? Order.Name : String.Empty;
			}
			set
			{
				if (!Active)
					FOrderDefinition = new Parser().ParseOrderDefinition(value);
				else
				{
					if ((value == null) || (value == String.Empty))
						Order = null;
					else
						Order = StringToOrder(value);
				}
			}
		}
		
		private OrderDefinition FOrderDefinition;
		[Category("Data")]
		[DefaultValue(null)]
		[Description("Order of the view.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OrderDefinition OrderDefinition
		{
			get
			{
				if (!Active)
					return FOrderDefinition;
				else
					return Order != null ? (OrderDefinition)Order.EmitStatement(EmitMode.ForCopy) : null;
			}
			set
			{
				if (!Active)
					FOrderDefinition = value;
				else
				{
					if (value == null)
						Order = null;
					else
						Order = OrderDefinitionToOrder(value);
				}
			}
		}
		
		#endregion

		#region Adorn Expression

		private AdornColumnExpressions FColumns = new AdornColumnExpressions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AdornColumnExpressions Columns { get { return FColumns; } }
		
		private CreateConstraintDefinitions FConstraints = new CreateConstraintDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CreateConstraintDefinitions Constraints { get { return FConstraints; } }
		
		private OrderDefinitions FOrders = new OrderDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OrderDefinitions Orders { get { return FOrders; } }

		private KeyDefinitions FKeys = new KeyDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public KeyDefinitions Keys { get { return FKeys; } }

		private string AdornExpressionToString()
		{
			if (IsAdorned())
			{
				AdornExpression LAdornExpression = new AdornExpression();
				LAdornExpression.Expression = new IdentifierExpression("A");
				LAdornExpression.Expressions.AddRange(FColumns);
				LAdornExpression.Constraints.AddRange(FConstraints);
				LAdornExpression.Orders.AddRange(FOrders);
				LAdornExpression.Keys.AddRange(FKeys);
				return new D4TextEmitter().Emit(LAdornExpression);
			}
			else
				return String.Empty;
		}

		private void StringToAdornExpression(string AValue)
		{
			if (AValue == String.Empty)
			{
				FOrders.Clear();
				FConstraints.Clear();
				FColumns.Clear();
			}
			else
			{
				Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				AdornExpression LExpression = (AdornExpression)LParser.ParseExpression(AValue);
				UnInitializeAdornItems();
				try
				{
					FColumns = LExpression.Expressions;
					FConstraints = LExpression.Constraints;
					FOrders = LExpression.Orders;
					FKeys = LExpression.Keys;
					#if USENOTIFYLISTFORMETADATA
					AdornItemChanged(FColumns, null);
					#endif
				}
				finally
				{
					InitializeAdornItems();
				}
			}
		}

		[Browsable(false)]
		[DefaultValue("")]
		public string AdornExpression
		{
			get { return AdornExpressionToString(); }
			set { StringToAdornExpression(value); }
		}
		
		private void InitializeAdornItems()
		{
			#if USENOTIFYLISTFORMETADATA
			FKeys.OnChanged += new ListEventHandler(AdornItemChanged);
			FOrders.OnChanged += new ListEventHandler(AdornItemChanged);
			FColumns.OnChanged += new ListEventHandler(AdornItemChanged);
			FConstraints.OnChanged += new ListEventHandler(AdornItemChanged);
			#endif
		}

		private void UnInitializeAdornItems()
		{
			#if USENOTIFYLISTFORMETADATA
			FKeys.OnChanged -= new ListEventHandler(AdornItemChanged);
			FOrders.OnChanged -= new ListEventHandler(AdornItemChanged);
			FColumns.OnChanged -= new ListEventHandler(AdornItemChanged);
			FConstraints.OnChanged -= new ListEventHandler(AdornItemChanged);
			#endif
		}
		
		#if USENOTIFYLISTFORMETADATA
		private void AdornItemChanged(object ASender, object AItem)
		{
			if (Active)
				ExpressionChanged(null);
		}
		#endif
		
		private bool IsAdorned()
		{
			return
				(FColumns.Count > 0) ||
				(FConstraints.Count > 0) ||
				(FOrders.Count > 0);
		}

		#endregion

		#region Expression

		// Expression
		private string FExpression = String.Empty;
		[DefaultValue("")]
		[Category("Data")]
		[Description("Expression")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		[EditorDocumentType("d4")]
		public string Expression
		{
			get { return FExpression; }
			set
			{
				CheckInactive();
				FExpression = value == null ? String.Empty : value;
			}
		}
		
		private bool InternalWriteWhereClause
		{
			get { return FFlags[WriteWhereClauseMask]; }
			set { FFlags[WriteWhereClauseMask] = value; }
		}

		/// <summary> When true, the DataView will automatically restrict the expression based on the Master/Detail relationship. </summary>
		/// <remarks> 
		///		If this is set to false, the DataView user must manually include the restriction column in the expression.  
		///		The values of the master DataView are exposed as parameters named MasterDataViewXXX where XXX is the name 
		///		of the <emphasis role="bold">detail key name</emphasis> with '.' replaced with '_'.  The default is True.
		///	</remarks>
		public bool WriteWhereClause
		{
			get { return InternalWriteWhereClause; }
			set
			{
				if (InternalWriteWhereClause != value)
				{
					InternalWriteWhereClause = value;
					if (Active)
						ExpressionChanged(null);
				}
			}
		}

		private bool InternalUseBrowse
		{
			get { return FFlags[UseBrowseMask]; }
			set { FFlags[UseBrowseMask] = value; }
		}

		/// <summary> When true, the DataView will use a "browse" rather than an "order" cursor. </summary>
		/// <remarks> 
		///		The default is True.  Typically a browse is used when the underlying storage device 
		///		can effeciently search and navigate based on the DataView's sort order.  If this is not 
		///		the case, then an order may be more effecient. 
		///	</remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public bool UseBrowse
		{
			get { return InternalUseBrowse; }
			set 
			{
				if (InternalUseBrowse != value)
				{
					if (Active)
					{
						using (Row LRow = RememberActive())
						{
							InternalUseBrowse = value; 
							ExpressionChanged(LRow);
						}
					}
					else
						InternalUseBrowse = value;
				}
			}
		}
		
		// Filter
		private string FFilter = String.Empty;
		/// <summary> A D4 restriction filter expression to limit the data set. </summary>
		/// <remarks> The filter is a D4 expression returning a true if the row is to be included or false otherwise. </remarks>
		[DefaultValue("")]
		[Category("Behavior")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Description("A D4 restriction filter expression to limit the data set.")]
		public string Filter
		{
			get { return FFilter; }
			set
			{
				if (Active)
				{
					using (Row LRow = RememberActive())
					{
						string LOldFilter = FFilter;
						FFilter = (value == null ? String.Empty : value);
						try
						{
							ExpressionChanged(LRow);
						}
						catch
						{
							FFilter = LOldFilter;
							Open();
							throw;
						}
					}
				}
				else
					FFilter = (value == null ? String.Empty : value);
			}
		}
		
		private static Expression GetRestrictCondition(Schema.Key ADetailKey)
		{
			Expression LCondition = null;
			Expression LEqualExpression;
			for (int LIndex = 0; LIndex < ADetailKey.Columns.Count; LIndex++)
			{
				LEqualExpression = new BinaryExpression
				(
					new IdentifierExpression(ADetailKey.Columns[LIndex].Name), 
					Instructions.Equal, 
					new IdentifierExpression(GetParameterName(ADetailKey.Columns[LIndex].Name))
				);

				if (LCondition == null)
					LCondition = LEqualExpression;
				else
					LCondition = new BinaryExpression(LCondition, Instructions.And, LEqualExpression);
			}
			return LCondition;
		}
		
		private static Expression MergeRestrictCondition(Expression AExpression, Expression ACondition)
		{
			RestrictExpression LRestrictExpression;
			if (AExpression is RestrictExpression)
				LRestrictExpression = (RestrictExpression)AExpression;
			else
			{
				LRestrictExpression = new RestrictExpression();
				LRestrictExpression.Expression = AExpression;
			}
			if (LRestrictExpression.Condition == null)
				LRestrictExpression.Condition = ACondition;
			else
				LRestrictExpression.Condition = new BinaryExpression(LRestrictExpression.Condition, Instructions.And, ACondition);
			return LRestrictExpression;
		}

		private void InternalExpressionChanged()
		{
			Schema.Order LOrder = FOrder;
			InternalClose();
			try
			{
				FOrder = LOrder;
				InternalOpen();
			}
			catch
			{
				Close();
				throw;
			}
		}

		/// <summary> This is called when the expression returned from GetExpression has changed. </summary>
		private void ExpressionChanged(Row ARow)
		{
			EnsureBrowseState();
			BufferClose();
			if (FOpenState == DataSetState.Browse)
			{
				InternalExpressionChanged();
				Resume(ARow);
			}
		}

		private Parser FParser = new Parser();
		
		// Returns a D4 syntax tree for the base user expression
		protected virtual Expression GetSeedExpression()
		{
			Expression LExpression = FParser.ParseCursorDefinition(FExpression);

			if (LExpression is CursorDefinition)
				LExpression = ((CursorDefinition)LExpression).Expression;

			return LExpression;			
		}
		
		// Sets the values of FOriginalExpression and FTranslatedExpression. PrepareParams must have already been called
		protected virtual string GetExpression()
		{
			Expression LExpression = GetSeedExpression();

			OrderExpression LSaveOrderExpression = null;
			BrowseExpression LSaveBrowseExpression = null;
			if (LExpression is OrderExpression)
			{
				LSaveOrderExpression = (OrderExpression)LExpression;
				LExpression = LSaveOrderExpression.Expression;
			}
			else if (LExpression is BrowseExpression)
			{
				LSaveBrowseExpression = (BrowseExpression)LExpression;
				LExpression = LSaveBrowseExpression.Expression;
			}

			// Eat irrelevant browse and order operators			
			while ((LExpression is OrderExpression) || (LExpression is BrowseExpression))
			{
				if (LExpression is OrderExpression)
					LExpression = ((OrderExpression)LExpression).Expression;
				else
					LExpression = ((BrowseExpression)LExpression).Expression;
			}
				
			if (IsMasterSetup() && InternalWriteWhereClause)
			{
				LExpression = MergeRestrictCondition
				(
					LExpression, 
					GetRestrictCondition(FDetailKey)
				);
			}
			
			if (FFilter != String.Empty)
			{
				LExpression = MergeRestrictCondition
				(
					LExpression, 
					FParser.ParseExpression(FFilter)
				);
			}
			
			if (IsAdorned())
			{
				AdornExpression LAdornExpression = new AdornExpression();
				LAdornExpression.Expression = LExpression;
				LAdornExpression.Expressions.AddRange(FColumns);
				LAdornExpression.Constraints.AddRange(FConstraints);
				LAdornExpression.Orders.AddRange(FOrders);
				LAdornExpression.Keys.AddRange(FKeys);
				LExpression = LAdornExpression;
			}
			
			if (FOrderDefinition != null)
			{
				if (InternalUseBrowse)
				{
					BrowseExpression LBrowseExpression = new Language.D4.BrowseExpression();
					LBrowseExpression.Expression = LExpression;
					LBrowseExpression.Columns.AddRange(FOrderDefinition.Columns);
					LExpression = LBrowseExpression;
				}
				else
				{
					OrderExpression LOrderExpression = new Language.D4.OrderExpression();
					LOrderExpression.Expression = LExpression;
					LOrderExpression.Columns.AddRange(FOrderDefinition.Columns);
					LExpression = LOrderExpression;
				}					
			}
			else if (FOrder != null)
			{
				if (InternalUseBrowse)
				{
					BrowseExpression LBrowseExpression = new BrowseExpression();
					foreach (Schema.OrderColumn LColumn in FOrder.Columns)
						if (LColumn.IsDefaultSort)
							LBrowseExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils));
						else
							LBrowseExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils, (SortDefinition)LColumn.Sort.EmitStatement(EmitMode.ForCopy)));
					LBrowseExpression.Expression = LExpression;
					LExpression = LBrowseExpression;
				}
				else
				{
					OrderExpression LOrderExpression = new OrderExpression();
					foreach (Schema.OrderColumn LColumn in FOrder.Columns)
						if (LColumn.IsDefaultSort)
							LOrderExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils));
						else
							LOrderExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils, (SortDefinition)LColumn.Sort.EmitStatement(EmitMode.ForCopy)));
					LOrderExpression.Expression = LExpression;
					LExpression = LOrderExpression;
				}
			}
			else
			{
				if (LSaveOrderExpression != null)
				{
					LSaveOrderExpression.Expression = LExpression;
					LExpression = LSaveOrderExpression;
				}
				else if (LSaveBrowseExpression != null)
				{
					LSaveBrowseExpression.Expression = LExpression;
					LExpression = LSaveBrowseExpression;
				}
			}
			
			#if OnExpression
			if (LServerName != String.Empty)
			{
				OnExpression LOnExpression = new OnExpression();
				LOnExpression.Expression = LExpression;
				LOnExpression.ServerName = LServerName;
				LExpression = LOnExpression;
			}
			#endif
			
			CursorDefinition LCursorExpression = new CursorDefinition(LExpression);
			LCursorExpression.Isolation = FRequestedIsolation;
			LCursorExpression.Capabilities = FRequestedCapabilities;
			LCursorExpression.SpecifiesType = true;
			LCursorExpression.CursorType = FCursorType;
			
			return new D4TextEmitter().Emit(LCursorExpression);
		}

		#endregion
		
		#region Parameters

		private DataViewParamGroups FParamGroups;
		/// <summary> A collection of parameter groups. </summary>
		/// <remarks> All parameters from all groups are used to parameterize the expression. </remarks>
		[Category("Data")]
		[Description("Parameter Groups")]
		public DataViewParamGroups ParamGroups { get { return FParamGroups; } }
		
		private void DataViewParamChanged(object ASender)
		{
			if (Active)
				ParameterChanged(null);
		}
		
		private void DataViewParamStructureChanged(object ASender)
		{
			if (Active)
				ExpressionChanged(null);
		}

		private static string GetParameterName(string AColumnName)
		{
			return CParamNamespace + AColumnName.Replace(".", "_");
		}

		private void ParameterChanged(Row ARow)
		{
			if ((FState != DataSetState.Inactive) && (FOpenState == DataSetState.Browse))
			{
				EnsureBrowseState();
				BufferClose();
				CloseCursor();
				try
				{
					OpenCursor();
				}
				catch
				{
					Close();
					throw;
				}
				Resume(ARow);
			}
		}
		
		private void PrepareParams()
		{
			FCursor.Params.Clear();
			try
			{
				if (IsMasterSetup())
					for (int LIndex = 0; LIndex < FMasterKey.Columns.Count; LIndex++)
						FCursor.Params.Add(new MasterDataViewDataParam(GetParameterName(FDetailKey.Columns[LIndex].Name), MasterSource.View.TableType.Columns[FMasterKey.Columns[LIndex].Name].DataType, Modifier.Const, FMasterKey.Columns[LIndex].Name, MasterSource, true));
					
				foreach (DataViewParamGroup LGroup in FParamGroups)
					foreach (DataViewParam LParam in LGroup.Params)
						if (LGroup.Source != null)
							FCursor.Params.Add(new MasterDataViewDataParam(LParam.Name, LGroup.Source.View.TableType.Columns[LParam.ColumnName].DataType, LParam.Modifier, LParam.ColumnName, LGroup.Source, false));
						else
							FCursor.Params.Add(new SourceDataViewDataParam(LParam));
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

		private void SetParamValues()
		{
			bool LMasterValid = IsMasterValid();
			foreach (DataViewDataParam LParam in FCursor.Params)
			{
				MasterDataViewDataParam LMasterParam = LParam as MasterDataViewDataParam;
				if 
				(
					(LMasterParam == null) 
					|| (LMasterParam.IsMaster && LMasterValid) 
					|| 
					(
						!LMasterParam.IsMaster 
						&& (LMasterParam.Source != null) 
						&& (LMasterParam.Source.View != null) 
						&& LMasterParam.Source.View.Active
					)
				)
					LParam.Bind(Process);
			}
			GetActiveCursor().ShouldOpen = !IsDetail() || LMasterValid;
		}
		
		private void GetParamValues()
		{
			foreach (DataViewDataParam LParam in FCursor.Params)
				if (LParam is SourceDataViewDataParam)
					if (LParam.Modifier == Modifier.Var)
						((SourceDataViewDataParam)LParam).SourceParam.Value = (Scalar)LParam.Value;
		}
		
		/// <summary> Populates a given a (non null) DataParams collection with the actual params used by the DataView. </summary>
		public void GetAllParams(DAE.Runtime.DataParams AParams)
		{
			CheckActive();
			foreach (DataViewDataParam LParam in FCursor.Params)
				AParams.Add(LParam);
		}

		/// <summary> Returns the list of the actual params used by the Dataview. </summary>
		/// <remarks> The user should not modify the list of values of these parameters. </remarks>
		public DAE.Runtime.DataParams AllParams
		{
			get { return FCursor.Params; }
		}
		
		#endregion

		#region Open

		// Tracks the state used to open the DataView.
		private DataSetState FOpenState = DataSetState.Inactive;
		
		private void StartProcess()
		{
			if (FSession == null)
				throw new ClientException(ClientException.Codes.SessionMissing);
			ProcessInfo LProcessInfo = new ProcessInfo(FSession.SessionInfo);
			LProcessInfo.DefaultIsolationLevel = FIsolationLevel;
			LProcessInfo.FetchAtOpen = FOpenState != DataSetState.Insert;
			FProcess = FSession.ServerSession.StartProcess(LProcessInfo);
			FCursor = new ViewCursor(FProcess);
			FCursor.OnErrors += new CursorErrorsOccurredHandler(CursorOnErrors);
		}
		
		private void CursorOnErrors(ViewCursor ACursor, CompilerMessages AMessages)
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
			
				FCursor.Open();
				GetParamValues();
			}
			catch
			{
				CloseCursor();
				throw;
			}
		}

		protected virtual void InternalOpen()
		{
			TimingUtility.PushTimer("DataView.InternalOpen");
			try
			{
			
				if (FSession == null)
					throw new ClientException(ClientException.Codes.SessionMissing);
					
				if ((FOpenState != DataSetState.Browse) && UseApplicationTransactions && !IsApplicationTransactionClient && (FApplicationTransactionID == Guid.Empty))
					FApplicationTransactionID = BeginApplicationTransaction(FOpenState == DataSetState.Insert);

				if (IsApplicationTransactionClient)
					JoinApplicationTransaction(FOpenState == DataSetState.Insert);

				try
				{
					PrepareParams();
					try
					{
						TimingUtility.PushTimer("DataView.InternalOpen -- Getting Expression");
						try
						{
							FCursor.Expression = GetExpression();
						}
						finally
						{
							TimingUtility.PopTimer();
						}

						TimingUtility.PushTimer("DataView.InternalOpen -- Preparing Expression");
						try
						{
							FCursor.Prepare();
						}
						finally
						{
							TimingUtility.PopTimer();
						}
						try
						{
							TimingUtility.PushTimer("DataView.OpenCursor");
							try
							{
								OpenCursor();
							}
							finally
							{
								TimingUtility.PopTimer();
							}
							FOrder = FCursor.Order;
							FOrderDefinition = null;
						}
						catch
						{
							FCursor.Close();
							FCursor.Unprepare();
							throw;
						}
					}
					catch
					{
						UnprepareParams();
						throw;
					}
				}
				catch
				{
					if (IsApplicationTransactionServer)
					{
						RollbackApplicationTransaction();
						UnprepareApplicationTransactionServer();
					}
					else if (IsApplicationTransactionClient && IsJoined)
						LeaveApplicationTransaction();
					throw;
				}
			}
			finally
			{
				TimingUtility.PopTimer();
			}
		}
		
		/// <summary> Activates the DataView for regular browse usage. </summary>
		/// <remarks> Setting the Active property to true effectively calls this method. </remarks>
		public void Open()
		{
			Open(DataSetState.Browse);
		}

		/// <summary> Activates the DataView, with the specification of the initial state. </summary>
		/// <remarks> If the DataView is already active, then this method has no effect. </remarks>
		/// <param name="AOpenState"> 
		///		The initial state of the DataView.  If the open state is Insert or Edit, then the 
		///		DataView will be closed after posting or canceling the modification.  Also, if the 
		///		open state is Insert or Edit, the DataView is optimized for only that operation 
		///		and no other rows will appear in the buffer besides the row being edited or inserted.
		///		If the open state is specified as Inactive, Browse is assumed.
		///	</param>
		public void Open(DataSetState AOpenState)
		{
			if (FState == DataSetState.Inactive)
			{
				DoBeforeOpen();

				if (AOpenState == DataSetState.Inactive)
					AOpenState = DataSetState.Browse;
				FOpenState = AOpenState;
				StartProcess();
				try
				{
					InternalOpen();
					try
					{
						CreateFields();
						try
						{
							BufferUpdateCount(true);
							try
							{
								InternalBOF = true;
								try
								{
									if (FOpenState == DataSetState.Browse)
										SetState(DataSetState.Browse);
									else if (FOpenState == DataSetState.Insert)
									{
										FState = DataSetState.Browse;
										Insert();
									}
									else
									{
										FState = DataSetState.Browse;
										Edit();
									}
									
									DoAfterOpen();
								}
								catch
								{
									SetState(DataSetState.Inactive);
									throw;
								}
							}
							catch
							{
								CloseBuffer();
								throw;
							}
						}
						catch
						{
							FreeFields();
							throw;
						}
					}
					catch
					{
						InternalClose();
						throw;
					}
				}
				catch
				{
					StopProcess();
					throw;
				}
			}
		}

		#endregion

		#region Close

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
		
		private void CloseOrder()
		{
			FOrderDefinition = FOrder != null ? (OrderDefinition)FOrder.EmitStatement(EmitMode.ForCopy) : null;
			FOrder = null;
		}
		
		// Closes the buffer, finalizing the rows it contains, but leaves the buffer space intact. Used when closing the underlying cursor.
		private void BufferClose()
		{
			BufferClear();
			FinalizeBuffers(0, FBuffer.Count - 1);
		}

		// Closes the buffer, finalizing the rows it contains, and clearing the buffer space. Only used when closing the view.
		private void CloseBuffer()
		{
			BufferClose();
			FBuffer.Clear();
		}

		protected virtual void CloseCursor()
		{
			FCursor.Close();
		}

		protected virtual void InternalClose()
		{
			try
			{
				CloseCursor();
			}
			finally
			{
				try
				{
					FCursor.Unprepare();
					CloseOrder();
				}
				finally
				{
					try
					{
						UnprepareParams();
					}
					finally
					{
						if (IsApplicationTransactionServer)
						{
							RollbackApplicationTransaction();
							UnprepareApplicationTransactionServer();
						}
						else if (IsApplicationTransactionClient && IsJoined)
							LeaveApplicationTransaction();
					}
				}
			}
		}

		/// <summary> Closes the DataView, if it is open. </summary>
		/// <remarks> If the DataView is in Insert or Edit state, a cancel is performed prior to closing. </remarks>
		public void Close()
		{
			if (FState != DataSetState.Inactive)
			{
				Cancel();
				DoBeforeClose();
				SetState(DataSetState.Inactive);
				FreeFields();
				CloseBuffer();
				InternalClose();
				StopProcess();
				DoAfterClose();
			}
		}
		
		#endregion

		#region State

		private DataSetState FState;
		/// <summary> The current state of the DataView. </summary>
		[Browsable(false)]
		public DataSetState State { get { return FState; } }

		/// <summary> Throws an exception if the the DataView's state is in the given set of states. </summary>
		public void CheckState(params DataSetState[] AStates)
		{
			foreach (DataSetState LState in AStates)
				if (FState == LState)
					return;
			throw new ClientException(ClientException.Codes.IncorrectState, FState.ToString());
		}

		//Note: SetState has side affect, there is no guarantee that the state has not changed if an exception is thrown.
		private void SetState(DataSetState AState)
		{
			if (FState != AState)
			{
				FState = AState;
				InternalIsModified = false;
				StateChanged();
			}
		}

		private void EnsureBrowseState(bool APostChanges)
		{
			CheckActive();

			if ((FState == DataSetState.Edit) || (FState == DataSetState.Insert))
			{
				RequestSave();
				if (IsModified && APostChanges)
					Post();
				else
					Cancel();
			}
		}

		private void EnsureBrowseState()
		{
			EnsureBrowseState(true);
		}
		
		/// <summary> Gets and sets the active state of the DataView. </summary>
		/// <remarks> Setting this property is equivilant to calling <see cref="Open()"/> or <see cref="Close()"/> as appropriate. </remarks>
		[Category("Data")]
		[DefaultValue(false)]
		[Description("Gets and sets the active state of the DataView")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public bool Active
		{
			get { return FState != DataSetState.Inactive; }
			set
			{
				if (Initializing)
					DelayedActive = value;
				else
				{
					if (value != (FState != DataSetState.Inactive))
					{
						if (value)
							Open();
						else
							Close();
					}
				}
			}
		}

		/// <summary> Throws an exception if the DataView is not active. </summary>
		public void CheckActive()
		{
			if (FState == DataSetState.Inactive)
				throw new ClientException(ClientException.Codes.NotActive);
		}
		
		/// <summary> Throws an exception if the DataView is active. </summary>
		public void CheckInactive()
		{
			if (FState != DataSetState.Inactive)
				throw new ClientException(ClientException.Codes.Active);
		}

		#endregion

		#region ISupportInitialize

		private bool Initializing
		{
			get { return FFlags[InitializingMask]; }
			set { FFlags[InitializingMask] = value; }
		}

		// The Active properties value during the intialization period defined in ISupportInitialize.
		private bool DelayedActive
		{
			get { return FFlags[DelayedActiveMask]; }
			set { FFlags[DelayedActiveMask] = value; }
		}

		/// <summary> Called to indicate that the properties of the DataView are being read (and therefore DataView should not activate yet). </summary>
		public void BeginInit()
		{
			Initializing = true;
		}

		/// <summary> Called to indicate that the properties of the DataView have been read (and therefore the DataView can be activated). </summary>
		public void EndInit()
		{
			Initializing = false;
			if (FSession != null)
				FSession.ViewEndInit();
			Active = DelayedActive;
		}

		#endregion

		#region Navigation
		
		private bool InternalBOF
		{
			get { return FFlags[BOFMask]; }
			set { FFlags[BOFMask] = value; }
		}

		/// <summary> True when the DataView is on its first row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the first row. </remarks>
		[Browsable(false)]
		public bool BOF
		{
			get { return InternalBOF || ((FActiveOffset >= 0) && ((FBuffer[FActiveOffset].RowFlag & RowFlag.BOF) != 0)); }
		}
		
		private bool InternalEOF
		{
			get { return FFlags[EOFMask]; }
			set { FFlags[EOFMask] = value; }
		}

		/// <summary> True when the DataView is on its last row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the last row. </remarks>
		[Browsable(false)]
		public bool EOF
		{
			get { return InternalEOF || ((FActiveOffset >= 0) && ((FBuffer[FActiveOffset].RowFlag & RowFlag.EOF) != 0)); }
		}

		/// <summary> Navigates the DataView to the first row in the data set. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void First()
		{
			EnsureBrowseState();
			BufferClear();
			try
			{
				FCursor.First();
				CursorNextRows();
				if (FEndOffset >= 0)
					FBuffer[0].RowFlag |= RowFlag.BOF;
			}
			finally
			{
				InternalBOF = true;
				DataChanged();
			}
		}

		/// <summary> Navigates the DataView to the last row in the data set. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void Last()
		{
			EnsureBrowseState();
			BufferClear();
			try
			{
				FCursor.Last();
				CursorPriorRows();
			}
			finally
			{
				InternalEOF = true;
				DataChanged();
			}
		}
		
		/// <summary> Attempts to scroll the dataview by the specified delta. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		/// <param name="ADelta"> Number of rows, positive or negative to move relative to the current active row. </param>
		/// <returns> Number of rows actually scrolled. </returns>
		public int MoveBy(int ADelta)
		{
			int LResult = 0;
			if (ADelta != 0)
			{
				EnsureBrowseState();
				if (((ADelta > 0) && !EOF) || ((ADelta < 0) && !BOF))
				{
					InternalBOF = false;
					InternalEOF = false;
					int LOffsetDelta = 0;
					bool LWillScroll;
					try
					{
						if (ADelta > 0)
						{
							// Move the active offset as far a possible within the current buffer
							LResult = Math.Min(ADelta, FEndOffset - FActiveOffset);
							ADelta -= LResult;
							FActiveOffset += LResult;

							// Advance any additional rows by progressing through the cursor
							while (ADelta > 0) 
							{
								LWillScroll = (FEndOffset >= (FBuffer.Count - 2));	// Note whether the read of the next row will cause the buffer to scroll
								if (CursorNextRow())
								{
									if (LWillScroll)
										LOffsetDelta--;
									FActiveOffset++;
									ADelta--;
									LResult++;
								}
								else
								{
									InternalEOF = true;
									break;
								}
							}
						}
						else if (ADelta < 0)
						{
							// Move the active offset as far a possible within the current buffer
							LResult = Math.Max(ADelta, -FActiveOffset);
							ADelta -= LResult;
							FActiveOffset += LResult;

							// Retreive any additional rows by digressing through the cursor
							while (ADelta < 0)
							{
								LWillScroll = (FEndOffset >= (FBuffer.Count - 2));	// Note whether the read of the next row will cause the buffer to scroll
								if (CursorPriorRow())
								{
									if (LWillScroll)
										LOffsetDelta++;
									FActiveOffset--;
									ADelta++;
									LResult--;
								}
								else
								{
									InternalBOF = true;
									break;
								}
							}
						}
					}
					finally
					{
						if (LOffsetDelta != 0)
							UpdateFirstOffsets(LOffsetDelta);
						DataChanged();
					}
				}
			}
			return LResult;
		}

		/// <summary> Attempts to navigate the DataView to the next row. </summary>
		/// <remarks> 
		///		Any outstanding Insert or Edit will first be posted.  If there are no more rows, 
		///		no error will occur, but the EOF property will be true. 
		///	</remarks>
		public void Next()
		{
			MoveBy(1);
		}
		
		/// <summary> Attempts to navigate the DataView to the prior row. </summary>
		/// <remarks> 
		///		Any outstanding Insert or Edit will first be posted.  If there are no more rows, 
		///		no error will occur, but the BOF property will be true. 
		///	</remarks>
		public void Prior()
		{
			MoveBy(-1);
		}
		
		#endregion

		#region Set Operations

		/// <summary> Indicates whether there is at least one row in the DataView. </summary>
		/// <remarks> Throws an exception if the DataView is not active. </remarks>
		[Browsable(false)]
		public bool IsEmpty()
		{
			CheckActive();
			return FEndOffset == -1;
		}
		
		/// <summary> Throws and exception if there are now rows in the DataView. </summary>
		/// <remarks> Also throws an exception if the DataView is not active. </remarks>
		public void CheckNotEmpty()
		{
			if (IsEmpty())
				throw new ClientException(ClientException.Codes.DataViewEmpty);
		}

		/// <summary> Refreshes the data in the DataView from the underlying data set. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void Refresh()
		{
			EnsureBrowseState();
			try
			{
				if (FActiveOffset <= FEndOffset)
				{
					FCursor.Refresh(FBuffer[FActiveOffset].Row);
					FCurrentOffset = FActiveOffset;
				}
				else
				{
					FCursor.Reset();
					CurrentReset();
				}
			}
			finally
			{
				Resync(false, false);
			}
		}

		/// <summary> Refreshes the data to a specified row. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void Refresh(Row ARow)
		{
			EnsureBrowseState();
			try
			{
				FCursor.Refresh(ARow);
				CurrentReset();
			}
			finally
			{
				Resync(false, false);
			}
		}
		
		#endregion

		#region Modification

		private bool InternalIsModified
		{
			get { return FFlags[IsModifiedMask]; }
			set { FFlags[IsModifiedMask] = true; }
		}
		
		/// <summary> Indicates whether the DataView has been modified. </summary>
		[Browsable(false)]
		public bool IsModified
		{
			get
			{
				CheckActive();
				return InternalIsModified;
			}
		}

		private bool InternalIsReadOnly
		{
			get { return FFlags[IsReadOnlyMask]; }
			set 
			{ 
				if (value)
					FRequestedCapabilities &= ~CursorCapability.Updateable;
				else
					FRequestedCapabilities |= CursorCapability.Updateable;
				FFlags[IsReadOnlyMask] = value; 
			}
		}

		/// <summary> When true, the DataView's data cannot be modified. </summary>
		/// <remarks> The DataView must be inactive to change this property. </remarks>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Read only state")]
		public bool IsReadOnly
		{
			get { return InternalIsReadOnly; }
			set
			{
				if (InternalIsReadOnly != value)
				{
					CheckState(DataSetState.Inactive);
					InternalIsReadOnly = value;
				}
			}
		}
		
		/// <summary> Throws and exception if the DataView cannot be edited. </summary>
		/// <remarks> This is tied to the ReadOnly property unless a custom insert, update, or delete statement has been provided. </remarks>
		public virtual void CheckCanModify()
		{
			if (InternalIsReadOnly && (FInsertStatement == String.Empty) && (FUpdateStatement == String.Empty) && (FDeleteStatement == String.Empty))
				throw new ClientException(ClientException.Codes.IsReadOnly);
		}
		
		// InsertStatement
		private string FInsertStatement = String.Empty;
		/// <summary> A D4 statement that will be used to insert any new rows. </summary>
		/// <remarks> If no statement is specified, the insert will be performed through the cursor to the Dataphor server. </remarks>
		[DefaultValue("")]														   
		[Category("Data")]
		[Description("A D4 statement that will be used to insert any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		[EditorDocumentType("d4")]
		public string InsertStatement
		{
			get { return FInsertStatement; }
			set { FInsertStatement = value == null ? String.Empty : value; }
		}
		
		// UpdateStatement
		private string FUpdateStatement = String.Empty;
		/// <summary> A D4 statement that will be used to update any new rows. </summary>
		/// <remarks> If no statement is specified, the update will be performed through the cursor to the Dataphor server. </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 statement that will be used to update any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		[EditorDocumentType("d4")]
		public string UpdateStatement
		{
			get { return FUpdateStatement; }
			set { FUpdateStatement = value == null ? String.Empty : value; }
		}
		
		// DeleteStatement
		private string FDeleteStatement = String.Empty;
		/// <summary> A D4 statement that will be used to delete any new rows. </summary>
		/// <remarks> If no statement is specified, the delete will be performed through the cursor to the Dataphor server. </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 statement that will be used to delete any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		[EditorDocumentType("d4")]
		public string DeleteStatement
		{
			get { return FDeleteStatement; }
			set { FDeleteStatement = value == null ? String.Empty : value; }
		}
		
		/// <summary> Puts the DataView into Edit state if not already in edit/insert state. </summary>
		/// <remarks> 
		///		If there are no rows in the data set, an insert/append (same operation for an empty set) will 
		///		be performed.  If <see cref="UseApplicationTransactions"/> is True, and this DataView is not
		///		detailed, then an application transaction is begun.
		///	</remarks>
		public void Edit()
		{
			if ((FState != DataSetState.Edit) && (FState != DataSetState.Insert))
			{
				if (FEndOffset == -1)
					Insert();
				else
				{
					EnsureBrowseState();
					CheckCanModify();
					DoBeforeEdit();
					try
					{
						PrepareApplicationTransactionServer(false);
					}
					catch
					{
						Resync(false, false);
						throw;
					}
					ClearOriginalRow();
					FOriginalRow = (Row)FBuffer[FActiveOffset].Row.Copy();
					SetState(DataSetState.Edit);
					DoAfterEdit();
				}
			}
		}
		
		/// <summary> Defaults the key columns of a row matching a subset of the type of this DataView with those of the Master. </summary>
		public void InitializeFromMaster(Row ARow)
		{
			if (IsMasterSetup() && !MasterSource.View.IsEmpty())
			{
				DataField LField;
				for (int LIndex = 0; LIndex < FMasterKey.Columns.Count; LIndex++)
					if (ARow.DataType.Columns.Contains(FDetailKey.Columns[LIndex].Name))
					{
						LField = MasterSource.View.Fields[FMasterKey.Columns[LIndex].Name];
						if (LField.HasValue())
							ARow[FDetailKey.Columns[LIndex].Name] = LField.Value;
					}
			}
		}

		/// <summary> Initializes row values with default data. </summary>
		protected virtual void InternalInitializeRow(int ARowIndex)
		{
			Row LTargetRow = FBuffer[ARowIndex].Row;
			LTargetRow.ClearValues();
			
			Row LOriginalRow = new Row(LTargetRow.Process, LTargetRow.DataType);
			try
			{
				FProcess.BeginTransaction(FIsolationLevel);
				try
				{
					if (IsMasterSetup() && !MasterSource.View.IsEmpty())
					{
						Schema.TableVarColumn LColumn;
						DataField LField;
						DataField LDetailField;
						for (int LIndex = 0; LIndex < FMasterKey.Columns.Count; LIndex++)
						{
							LColumn = FMasterKey.Columns[LIndex];
							LField = MasterSource.View.Fields[LColumn.Name];
							LDetailField = Fields[FDetailKey.Columns[LIndex].Name];
							if (LField.HasValue())
							{
								Row LSaveOldRow = FOldRow;
								FOldRow = LOriginalRow;
								try
								{						
									LTargetRow[LDetailField.Name] = LField.Value;
									try
									{
										// TODO: fire row changing events.  removed because this was throwing when OpenState=Insert
//										RowChanging(LDetailField);
										InternalIsModified = GetActiveCursor().Validate(LOriginalRow, LTargetRow, LDetailField.Name) || InternalIsModified;
									}
									catch
									{
										LTargetRow.ClearValue(LDetailField.Name);
										throw;
									}

									InternalIsModified = GetActiveCursor().Change(LOriginalRow, LTargetRow, LDetailField.Name) || InternalIsModified;
									// See above TODO
//									if (InternalIsModified)
//										RowChanged(null);
//									else
//										RowChanged(LDetailField);
								}
								finally
								{
									FOldRow = LSaveOldRow;
								}
							}
						}
					}
					
					bool LSaveIsModified = InternalIsModified;
					try
					{
						DoDefault();
						
						GetActiveCursor().Default(LTargetRow, String.Empty);
					}
					finally
					{
						InternalIsModified = LSaveIsModified;
					}
						
					FProcess.CommitTransaction();
				}
				catch
				{
					FProcess.RollbackTransaction();
					throw;
				}
			}
			finally
			{
				LOriginalRow.Dispose();
			}
		}
		
		internal void ChangeColumn(DataField AField, DataValue AValue)
		{
			Edit();
			bool LChanged = false;
			Row LActiveRow = FBuffer[FActiveOffset].Row;
			Row LOriginalRow = new Row(LActiveRow.Process, LActiveRow.DataType);
			try
			{
				LActiveRow.CopyTo(LOriginalRow);
				DataValue LOldValue = AField.HasValue() ? AField.Value.Copy() : (DataValue)null;
				try
				{
					if (AValue == null)
						FBuffer[FActiveOffset].Row.ClearValue(AField.ColumnIndex);
					else
						FBuffer[FActiveOffset].Row[AField.ColumnIndex] = AValue;

					FProcess.BeginTransaction(FIsolationLevel);
					try
					{
						Row LSaveOldRow = FOldRow;
						FOldRow = LOriginalRow;
						try
						{						
							try
							{
								RowChanging(AField);
								LChanged = GetActiveCursor().Validate(LOriginalRow, LActiveRow, AField.ColumnName);
							}
							catch
							{
								if (LOldValue == null)
									FBuffer[FActiveOffset].Row.ClearValue(AField.ColumnIndex);
								else
									FBuffer[FActiveOffset].Row[AField.ColumnIndex] = LOldValue;
								LOldValue = null;
								throw;
							}

							if (GetActiveCursor().Change(LOriginalRow, LActiveRow, AField.ColumnName) || LChanged)
								RowChanged(null);
							else
								RowChanged(AField);
						}
						finally
						{
							FOldRow = LSaveOldRow;
						}
							
						FProcess.CommitTransaction();
					}
					catch
					{
						FProcess.RollbackTransaction();
						throw;
					}

					InternalIsModified = true;
				}
				finally
				{
					if (LOldValue != null)
						LOldValue.Dispose();
				}
			}
			finally
			{
				LOriginalRow.Dispose();
			}
		}

		private void EndInsertAppend()
		{
			try
			{
				PrepareApplicationTransactionServer(true);
			}
			catch
			{
				Resync(false, false);
				throw;
			}
			InternalInitializeRow(FActiveOffset);
			SetState(DataSetState.Insert);
			DataChanged();
			DoAfterInsert();
		}
		
		/// <summary> Puts the DataView into Insert state, with the proposed row inserted above the active row (or in the first position in an empty data set). </summary>
		/// <remarks> 
		///		The actual location of the row once it is posted will depend on the sort order.  The row isn't 
		///		actually posted to the underlying data set until <see cref="Post()"/> is invoked. If the DataView
		///		is in Insert or Edit state prior to this call, a post will be attempted.  If 
		///		<see cref="UseApplicationTransactions"/> is True, and this DataView is not detailed, then an 
		///		application transaction is begun.
		///	</remarks>
		public void Insert()
		{
			EnsureBrowseState();
			CheckCanModify();
			DoBeforeInsert();
			Guid LActiveBookmark = FBuffer[FActiveOffset].Bookmark;
			FBuffer.Move(FBuffer.Count - 1, FActiveOffset);
			if (FEndOffset == -1)
				FBuffer[FActiveOffset].RowFlag = RowFlag.BOF;
			else
				FBuffer[FActiveOffset].Bookmark = LActiveBookmark;
			if (FCurrentOffset >= FActiveOffset)
				FCurrentOffset++;
			if (FEndOffset < (FBuffer.Count - 2))
				FEndOffset++;
			EndInsertAppend();
		}
		
		/// <summary> Puts the DataView into Insert state, with the proposed row shown at the end of the data set. </summary>
		/// <remarks> 
		///		The actual location of the row once it is posted will depend on the sort order. The row 
		///		isn't actually posted to the underlying data set until Post() is invoked. If the DataView
		///		is in Insert or Edit state prior to this call, a post will be attempted.  If 
		///		<see cref="UseApplicationTransactions"/> is True, and this DataView is not detailed, then an 
		///		application transaction is begun.
		///	</remarks>
		public void Append()
		{
			EnsureBrowseState();
			CheckCanModify();
			DoBeforeInsert();
			bool LWasEmpty = (FEndOffset < 0);
			if (LWasEmpty || ((FBuffer[FEndOffset].RowFlag & RowFlag.EOF) == 0))	// Will we have to resync to the end
			{
				// If the EOF row is not in our buffer, we will have to clear the buffer and start with a new row at the end.
				BufferClear();
				FEndOffset = 0;
				FBuffer[0].RowFlag = RowFlag.EOF;

				// If there were rows before, read them again
				if (!LWasEmpty)
					CursorPriorRows();
			}
			else
			{
				// The EOF row is in our buffer, as an optimization, append our row after the EOF row
				if (FEndOffset == (FBuffer.Count - 2))	// Buffer is full so bump the first item
				{
					FBuffer.Move(0, FEndOffset);
					if (FCurrentOffset >= 0)
						FCurrentOffset--;
				}
				else									// The buffer is not full, pull the scratch pad row
				{
					FBuffer.Move(FBuffer.Count - 1, FEndOffset);
					FEndOffset++;
				}
				FBuffer[FEndOffset].RowFlag |= RowFlag.EOF;
			}
			FActiveOffset = FEndOffset;
			InternalBOF = false;
			EndInsertAppend();
		}

		/// <summary> Cancel's the edit or insert state. </summary>
		/// <remarks> 
		///		If the OpenState of the DataView is Insert or Edit, the DataView will be closed after this 
		///		call. If the DataView is not in Insert or Edit state then this method does nothing.
		///	</remarks>
		public void Cancel()
		{
			if ((FState == DataSetState.Insert) || (FState == DataSetState.Edit))
			{
				try
				{
					CurrentGotoActive(true);
					
					DoBeforeCancel();

					// Make sure that all details have canceled
					DoPrepareToCancel();
				}
				finally
				{
					try
					{
						if (IsApplicationTransactionServer)
						{
							RollbackApplicationTransaction();
							UnprepareApplicationTransactionServer();
						}
					}
					finally
					{
						try
						{
							ClearOriginalRow();
						}
						finally
						{
							try
							{
								SetState(DataSetState.Browse);
							}
							finally
							{
								Resync(false, false);
							}
						}
					}
				}
				
				DoAfterCancel();
			}
		}

		/// <summary> Called before the DataView is opened. </summary>
		public event EventHandler BeforeOpen;
		
		protected virtual void DoBeforeOpen()
		{
			if (BeforeOpen != null)
				BeforeOpen(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataView is opened. </summary>
		public event EventHandler AfterOpen;

		protected virtual void DoAfterOpen()
		{
			if (AfterOpen != null)
				AfterOpen(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataView is closed. </summary>
		public event EventHandler BeforeClose;
		
		protected virtual void DoBeforeClose()
		{
			if (BeforeClose != null)
				BeforeClose(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataView is closed. </summary>
		public event EventHandler AfterClose;

		protected virtual void DoAfterClose()
		{
			if (AfterClose != null)
				AfterClose(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataView enters insert mode. </summary>
		public event EventHandler BeforeInsert;
		
		protected virtual void DoBeforeInsert()
		{
			if (BeforeInsert != null)
				BeforeInsert(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataView enters insert mode. </summary>
		public event EventHandler AfterInsert;

		protected virtual void DoAfterInsert()
		{
			if (AfterInsert != null)
				AfterInsert(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataView enters edit mode. </summary>
		public event EventHandler BeforeEdit;
		
		protected virtual void DoBeforeEdit()
		{
			if (BeforeEdit != null)
				BeforeEdit(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataView enters edit mode. </summary>
		public event EventHandler AfterEdit;

		protected virtual void DoAfterEdit()
		{
			if (AfterEdit != null)
				AfterEdit(this, EventArgs.Empty);
		}
		
		/// <summary> Called before deletion of a row. </summary>
		public event EventHandler BeforeDelete;
		
		protected virtual void DoBeforeDelete()
		{
			if (BeforeDelete != null)
				BeforeDelete(this, EventArgs.Empty);
		}
		
		/// <summary> Called after deletion of a row. </summary>
		public event EventHandler AfterDelete;

		protected virtual void DoAfterDelete()
		{
			if (AfterDelete != null)
				AfterDelete(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataView is posted. </summary>
		public event EventHandler BeforePost;
		
		protected virtual void DoBeforePost()
		{
			if (BeforePost != null)
				BeforePost(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataView is posted. </summary>
		public event EventHandler AfterPost;

		protected virtual void DoAfterPost()
		{
			if (AfterPost != null)
				AfterPost(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataView is canceled. </summary>
		public event EventHandler BeforeCancel;
		
		protected virtual void DoBeforeCancel()
		{
			if (BeforeCancel != null)
				BeforeCancel(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataView is canceled. </summary>
		public event EventHandler AfterCancel;

		protected virtual void DoAfterCancel()
		{
			if (AfterCancel != null)
				AfterCancel(this, EventArgs.Empty);
		}
		
		/// <summary> Called when determining the default values for a newly inserted row. </summary>
		/// <remarks> Setting column values in this handler will not set the IsModified of the DataView. </remarks>
		public event EventHandler OnDefault;
		
		protected virtual void DoDefault()
		{
			if (OnDefault != null)
				OnDefault(this, EventArgs.Empty);
		}
		
		/// <summary> Called before posting to validate the DataView's data. </summary>
		public event EventHandler OnValidate;

		protected virtual void DoValidate()
		{
			// Ensure that all non-nillable are not nil
			foreach (DAE.Schema.TableVarColumn LColumn in TableVar.Columns)
				if (!LColumn.IsNilable && this[LColumn.Name].IsNil)
				{
					this[LColumn.Name].FocusControl();
					throw new ClientException(ClientException.Codes.ColumnRequired, LColumn.Name);
				}

			if (OnValidate != null)
				OnValidate(this, EventArgs.Empty);
		}
		
		/// <summary> Validates any changes to the edited or inserted row (if there is one). </summary>
		/// <remarks> If the DataView is not in Insert or Edit state then this method does nothing. </remarks>
		public void Validate()
		{
			if ((FState == DataSetState.Insert) || (FState == DataSetState.Edit))
			{
				RequestSave();
				DoValidate();

				// Only need to reposition the cursor if we are using it (we are not an ATServer and we are in edit mode)
				if (!IsApplicationTransactionServer && (FState == DataSetState.Edit))
					CurrentGotoActive(true);
					
				GetActiveCursor().Validate(null, FBuffer[FActiveOffset].Row, String.Empty);
			}
		}

		/// <summary> Attempts to post the edited or inserted row (if there is one pending). </summary>
		/// <remarks> 
		///		If the OpenState of the DataView is Insert or Edit, the DataView will be closed after 
		///		this call. If the DataView is not in Insert or Edit state then this method does nothing.
		///	</remarks>
		public void Post()
		{
			RequestSave();
			if ((FState == DataSetState.Insert) || (FState == DataSetState.Edit))
			{
				DoValidate();
				DoBeforePost();

				// Only need to reposition the cursor if we are using it (we are not an ATServer and we are in edit mode)
				if (!IsApplicationTransactionServer && (FState == DataSetState.Edit))
					CurrentGotoActive(true);

				// Make sure that all details have posted
				DoPrepareToPost();

				// TODO: Test to ensure Optimistic concurrency check.
				bool LUpdateSucceeded = false;
				FProcess.BeginTransaction(FIsolationLevel);
				try
				{
					if (FState == DataSetState.Insert)
					{
						if (FInsertStatement == String.Empty)
							GetActiveCursor().Insert(FBuffer[FActiveOffset].Row);
						else
							InternalInsertRow();
					}
					else
					{
						if (FUpdateStatement == String.Empty)
							GetActiveCursor().Update(FBuffer[FActiveOffset].Row);
						else
							InternalUpdateRow();
					}
					
					LUpdateSucceeded = true;

					// Prepare Phase
					if (IsApplicationTransactionServer)
						PrepareApplicationTransaction();
					Process.PrepareTransaction();

					// Commit Phase
					FProcess.CommitTransaction();
					if (IsApplicationTransactionServer)
						CommitApplicationTransaction();

					// Refresh the main cursor to the newly inserted application transaction row
					if 
					(
						(IsApplicationTransactionServer && (FOpenState == DataSetState.Browse)) || 
						(
							((FState == DataSetState.Insert) && (FInsertStatement != String.Empty)) || 
							((FState == DataSetState.Edit) && (FUpdateStatement != String.Empty))
						)
					)
						FCursor.Refresh(FBuffer[FActiveOffset].Row);
				}
				catch (Exception E)
				{
					try
					{
						if (FProcess.InTransaction)
							FProcess.RollbackTransaction();
						if ((FState == DataSetState.Edit) && LUpdateSucceeded)
							GetActiveCursor().Refresh(FOriginalRow);
					}
					catch (Exception LRollbackException)
					{
						throw new DAE.Server.ServerException(DAE.Server.ServerException.Codes.RollbackError, E, LRollbackException.ToString());
					}
					throw;
				}
				
				try
				{
					try
					{
						ClearOriginalRow();
					}
					finally
					{
						InternalIsModified = false;
						try
						{
							if (IsApplicationTransactionServer)
								UnprepareApplicationTransactionServer();
						}
						finally
						{
							SetState(DataSetState.Browse);
						}
					}
				}
				finally
				{
					Resync(false, false);
				}
				
				DoAfterPost();
			}
		}
		
		/// <summary> Deletes the current row. </summary>
		/// <remarks> 
		///		This action is not validated with the user.  If the DataView is in Insert or Edit state,
		///		then a Cancel is performed.  If no row exists, an error is thrown.
		/// </remarks>
		public void Delete()
		{
			if (FState == DataSetState.Insert)
				Cancel();

			CheckActive();
			CheckNotEmpty();
			DoBeforeDelete();
			CurrentGotoActive(true);
			if (FDeleteStatement == String.Empty)
				FCursor.Delete();
			else
				InternalDeleteRow();
			SetState(DataSetState.Browse);
			Resync(false, false);
			DoAfterDelete();
		}
		
		private DAE.Runtime.DataParams GetParamsFromRow(Row ARow)
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LParams.Add(new DAE.Runtime.DataParam(ARow.DataType.Columns[LIndex].Name, ARow.DataType.Columns[LIndex].DataType, Modifier.In, ARow[LIndex]));
			return LParams;
		}
		
		private void InternalInsertRow()
		{
			DAE.Runtime.DataParams LParams = GetParamsFromRow(FBuffer[FActiveOffset].Row);
			IServerStatementPlan LPlan = FProcess.PrepareStatement(FInsertStatement, LParams);
			try
			{
				LPlan.Execute(LParams);
			}
			finally
			{
				FProcess.UnprepareStatement(LPlan);
			}
		}
		
		private void InternalUpdateRow()
		{
			DAE.Runtime.DataParams LParams = GetParamsFromRow(FBuffer[FActiveOffset].Row);
			IServerStatementPlan LPlan = FProcess.PrepareStatement(FUpdateStatement, LParams);
			try
			{
				LPlan.Execute(LParams);
			}
			finally
			{
				FProcess.UnprepareStatement(LPlan);
			}
		}
		
		private void InternalDeleteRow()
		{
			DAE.Runtime.DataParams LParams = GetParamsFromRow(FBuffer[FActiveOffset].Row);
			IServerStatementPlan LPlan = FProcess.PrepareStatement(FDeleteStatement, LParams);
			try
			{
				LPlan.Execute(LParams);
			}
			finally
			{
				FProcess.UnprepareStatement(LPlan);
			}
		}
		
		/// <summary> Resets the values of fields to their default values for the inserted or edited row. </summary>
		public void ClearFields()
		{
			CheckState(DataSetState.Edit, DataSetState.Insert);
			InternalInitializeRow(FActiveOffset);
			RowChanged(null);
		}

		#endregion

		#region Events & Notification
		
		/// <summary> Occurs the active row or any other part of the buffer changes. </summary>
		protected virtual void DataChanged()
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.InternalDataChanged();
		}
		
		/// <summary> Occurs when the active row's position within the buffer changes. </summary>
		protected virtual void UpdateFirstOffsets(int ADelta)
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.UpdateFirstOffset(ADelta);
		}
		
		/// <summary> Occurs before posting (typically to allow any dependants to post). </summary>
		protected virtual void DoPrepareToPost()
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.PrepareToPost();
		}

		/// <summary> Occurs before canceling (typically to allow any dependants to cancel). </summary>
		protected virtual void DoPrepareToCancel()
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.PrepareToCancel();
		}
		
		/// <summary> Occurs only when the fields in the active record are changing. </summary>
		/// <param name="AField"> Valid reference to a field if one field is changing. Null otherwise. </param>
		protected virtual void RowChanging(DataField AField)
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.RowChanging(AField);
		}

		/// <summary> Occurs only when the fields in the active record has changed. </summary>
		/// <param name="AField"> Valid reference to a field if one field has changed. Null otherwise. </param>
		protected virtual void RowChanged(DataField AField)
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.RowChanged(AField);
		}
		
		/// <summary> Occurs when a request is made that any control(s) associated with the given field should be focused. </summary>
		/// <remarks> This is typically invoked when validation fails for a field (so that the user can use the control to correct the problem). </remarks>
		protected internal virtual void FocusControl(DataField AField)
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.FocusControl(AField);
		}

		/// <summary> Occurs in reponse to any change in the state of the DataView. </summary>
		protected virtual void StateChanged()
		{
			foreach (DataLink LLink in EnumerateLinks())
				LLink.StateChanged();
		}
		
		#endregion

		#region Fields
		
		private DataFields FDataFields;
		/// <summary> Collection of DataField objects representing the columns of the active row. </summary>
		[Browsable(false)]
		public DataFields Fields
		{
			get
			{
				if (FDataFields == null)
					FDataFields = new DataFields(this);
				return FDataFields;
			}
		}
		
		/// <summary> Internal fields list. </summary>
		internal Schema.Objects FFields;

		/// <summary> Attempts to retrieve a DataField by index. </summary>
		/// <remarks> An exception is thrown if the specified index is out of bounds. </remarks>
		public DataField this[int AIndex]
		{
			get
			{
				DataField LField = (DataField)FFields[AIndex];
				if (LField == null)
					throw new ClientException(ClientException.Codes.FieldForColumnNotFound, AIndex);
				return LField;
			}
		}
		
		/// <summary> Attempts to retrieve a DataField by name. </summary>
		/// <remarks> An exception is thrown if the specified name is not found. </remarks>
		public DataField this[string AColumnName]
		{
			get
			{
				DataField LField = (DataField)FFields[AColumnName];
				if (LField == null)
					throw new ClientException(ClientException.Codes.FieldForColumnNotFound, AColumnName);
				return LField;
			}
		}

		/// <summary> The number of DataFields in the DataView. </summary>
		[Browsable(false)]
		public int FieldCount
		{
			get { return FFields.Count; }
		}

		private void CreateFields()
		{
			int LIndex = 0;
			DataField LField;
			foreach (Schema.Column LColumn in TableType.Columns)
			{
				LField = new DataField(this, LColumn);
				FFields.Add(LField);
				LIndex++;
			}
		}
		
		private void FreeFields()
		{
			if (FFields != null)
				FFields.Clear();
		}
		
		#endregion

		#region Bookmarks
#if DATAVIEWBOOKMARKS
		
		/// <summary> Determines if a bookmark can be retrieved for the active row. </summary>
		public bool BookmarkAvailable()
		{
			return (FState != DataSetState.Inactive) &&
				(FActiveOffset <= FEndOffset) && 
				((FBuffer[FActiveOffset].RowFlag & RowFlag.Data) != 0);
		}
		
		/// <summary> Retrieves a bookmark to the active row. </summary>
		/// <remarks> 
		///		Note that some changes to the DataView will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated.  
		///	</remarks>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Guid GetBookmark()
		{
			CheckActive();
			CheckNotEmpty();
			return FCursor.GetBookmark();
		}

		/// <summary> Returns to a previously bookmarked position. </summary>
		/// <remarks> 
		///		Note that some changes to the DataView will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated. 
		///	</remarks>
		public void GotoBookmark(Guid ABookmark)
		{
			CheckActive();
			try
			{
				FCursor.GotoBookmark(ABookmark);
			}
			finally
			{
				Resync(true, true);
			}
		}

		/// <summary> Compare the relative position of two bookmarked rows. </summary>
		/// <remarks> 
		///		Note that some changes to the DataView will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated. 
		///	</remarks>
		/// <returns> -1 if ABookmark1 is less than ABookmark 2.  0 if they are equal. 1 otherwise. </returns>
		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			CheckActive();
			return FCursor.CompareBookmarks(ABookmark1, ABookmark2);
		}

		/// <summary> Deallocates the resources used to maintain the specified bookmark. </summary>
		/// <remarks> 
		///		All bookmarks retrieved through GetBookmark() should be freed.  Note that some changes to the DataView 
		///		will cause the underlying cursor to be replaced; in such a case, previously retrieved bookmarks will be 
		///		freed and invalidated. 
		///	</remarks>
		public void FreeBookmark(Guid ABookmark)
		{
			CheckActive();
			FCursor.DisposeBookmark(ABookmark);
		}

		/// <summary> Determines is a given bookmark is still valid. </summary>
		/// <remarks> 
		///		Note that some changes to the DataView will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated.  If a bookmark is
		///		determined to be invalid (as determined by this method), then an attempt to free the bookmark
		///		will result in an error.  In other words, if a bookmark is no longer valid, it is already
		///		freed and no attempt should be made to free it again.
		///	</remarks>
		public bool IsBookmarkValid(Guid ABookmark)
		{
			CheckActive();
			try
			{
				FCursor.CompareBookmarks(ABookmark, ABookmark);
				return true;
			}
			catch
			{
				return false;
			}
		}
#endif
		#endregion

		#region Keys & Finds

		public static string GetNamesFromKey(Schema.Key AKey)
		{
			StringBuilder LResult = new StringBuilder();
			if (AKey != null)
				foreach (Schema.TableVarColumn LColumn in AKey.Columns)
				{
					if (LResult.Length > 0)
						LResult.Append(CColumnNameDelimiters[0]);
					LResult.Append(LColumn.Name);
				}
			return LResult.ToString();
		}

		public static Schema.Key GetKeyFromNames(string AKeyNames)
		{
			Schema.Key LResult = new Schema.Key();
			string LTrimmed;
			foreach (string LItem in AKeyNames.Split(CColumnNameDelimiters))
			{
				LTrimmed = LItem.Trim();
				if (LTrimmed != String.Empty)
				{
					// TODO: Change this to a StringCollection rather than a Schema.Key
					LResult.Columns.Add(new Schema.TableVarColumn(new Schema.Column(LTrimmed, null)));
				}
			}
			return LResult;
		}

		/// <summary> Gets a row representing a key for the active row. </summary>
		/// <returns> A row containing key information.  It is the callers responsability to Dispose this row when it is no longer required. </returns>
		public Row GetKey()
		{
			EnsureBrowseState();
			CurrentGotoActive(true);
			return FCursor.GetKey();
		}

		/// <summary> Attempts navigation to a specific row given a key. </summary>
		/// <param name="AKey"> A row containing key information.  This is typically retrieved using <see cref="GetKey()"/>. </param>
		/// <returns> 
		///		True if the row was located and navigation occurred.  If this method returns false, indicating that the 
		///		specified key was not located, then the active row will not have changed from before the call. 
		///	</returns>
		public bool FindKey(Row AKey)
		{
			EnsureBrowseState();
			try
			{
				return FCursor.FindKey(AKey);
			}
			finally
			{
				Resync(true, true);
			}
		}
		
		/// <summary> Navigates the DataView to the row nearest the specified key or partial key. </summary>
		/// <param name="AKey"> A full or partial row containing search criteria for the current order. </param>
		public void FindNearest(Row AKey)
		{
			EnsureBrowseState();
			try
			{
				FCursor.FindNearest(AKey);
			}
			finally
			{
				Resync(false, true);
			}
		}

		#endregion

		#region Application Transactions

		/// <summary> Specifies whether or not to start or enlist in application transactions. </summary>
		/// <remarks> 
		///		When true then going into Insert or Edit will start an application transaction or enlist 
		///		in an existing one if this DataView is detailed to another DataView. 
		///	</remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Specifies whether or not to start or enlist in application transactions.")]
		public bool UseApplicationTransactions
		{
			get { return FFlags[UseApplicationTransactionsMask]; }
			set { FFlags[UseApplicationTransactionsMask] = value; }
		}
		
		private EnlistMode FShouldEnlist;
		/// <summary> Specifies whether or not to enlist in the application transaction of the master DataView. </summary>
		[DefaultValue(EnlistMode.Default)]
		[Category("Behavior")]
		[Description("Specifies whether or not to enlist in the application transaction of the master data view")]
		public EnlistMode ShouldEnlist
		{
			get { return FShouldEnlist; }
			set { FShouldEnlist = value; }
		}
		
		private Guid FApplicationTransactionID = Guid.Empty;
		/// <summary> The ID of the application transaction if this DataView started one. </summary>
		[Browsable(false)]
		public Guid ApplicationTransactionID { get { return FApplicationTransactionID; } }

		/// <summary> Returns the DataView that is started the application transaction that this DataView participating in. </summary>
		/// <remarks> This may return this DataView if it was the one that started the application transaction. </remarks>
		[Browsable(false)]
		public DataView ApplicationTransactionServer 
		{ 
			get 
			{
				// return the highest level view in the link tree that is in insert/edit mode
				DataView LApplicationTransactionServer = null;
				if (UseApplicationTransactions)
				{
					if (ApplicationTransactionID != Guid.Empty)
						LApplicationTransactionServer = this;
					else
					{
						if (IsMasterValid())
						{
							switch (FShouldEnlist)
							{
								case EnlistMode.Default :
									bool LIsSuperset = false;
									foreach (Schema.Key LKey in MasterSource.View.TableVar.Keys)
									{
										Schema.Key LMasterKey = new Schema.Key();
										LMasterKey.Columns.AddRange(LKey.Columns);
										if (MasterSource.View.IsMasterSetup())
											foreach (Schema.TableVarColumn LKeyColumn in MasterSource.View.DetailKey.Columns)
												if (!LMasterKey.Columns.Contains(LKeyColumn))
													LMasterKey.Columns.Add(LKeyColumn);
										if (MasterKey.Columns.IsSubsetOf(LMasterKey.Columns) || MasterKey.Columns.IsSupersetOf(LMasterKey.Columns))
										{
											LIsSuperset = true;
											break;
										}
									}
										
									if (LIsSuperset)
										LApplicationTransactionServer = MasterSource.View.ApplicationTransactionServer;
								break;
								
								case EnlistMode.True :
									LApplicationTransactionServer = MasterSource.View.ApplicationTransactionServer;
								break;
							}
						}
					}
				}
				return LApplicationTransactionServer;
			} 
		}
		
		private bool IsApplicationTransactionServer { get { return (ApplicationTransactionServer == this); } }
		
		private bool IsApplicationTransactionClient { get { return (ApplicationTransactionServer != null) && (ApplicationTransactionServer != this); } }
		
		private Guid BeginApplicationTransaction(bool AIsInsert)
		{
			Guid LATID = FProcess.BeginApplicationTransaction(true, AIsInsert);
			IsJoined = true;
			return LATID;
		}
		
		private void JoinApplicationTransaction(bool AIsInsert)
		{
			FProcess.JoinApplicationTransaction(ApplicationTransactionServer.ApplicationTransactionID, AIsInsert);
			IsJoined = true;
		}
		
		private void LeaveApplicationTransaction()
		{
			FProcess.LeaveApplicationTransaction();
			IsJoined = false;
		}
		
		private void PrepareApplicationTransaction()
		{
			FProcess.PrepareApplicationTransaction(FApplicationTransactionID);
		}
		
		private void CommitApplicationTransaction()
		{
			FProcess.CommitApplicationTransaction(FApplicationTransactionID);
			IsJoined = false;
		}

		private void RollbackApplicationTransaction()
		{
			FProcess.RollbackApplicationTransaction(FApplicationTransactionID);
			IsJoined = false;
		}
		
		private bool JoinAsInsert
		{
			get { return FFlags[JoinAsInsertMask]; }
			set { FFlags[JoinAsInsertMask] = value; }
		}

		private bool IsJoined
		{
			get { return FFlags[IsJoinedMask]; }
			set { FFlags[IsJoinedMask] = value; }
		}

		private void PrepareApplicationTransactionServer(bool AIsInsert)
		{
			if (!IsApplicationTransactionClient && UseApplicationTransactions && (FOpenState == DataSetState.Browse))
			{
				using (Row LRow = RememberActive())
				{
					if (FApplicationTransactionID == Guid.Empty)
						FApplicationTransactionID = BeginApplicationTransaction(AIsInsert);
						
					FATCursor = new ViewCursor(FProcess);
					try
					{
						FATCursor.OnErrors += new CursorErrorsOccurredHandler(CursorOnErrors);
						FATCursor.Expression = GetExpression();
						FATCursor.Params.AddRange(FCursor.Params);
						FATCursor.Prepare();
						try
						{
							SetParamValues();
							FATCursor.Open();
							try
							{
								if (!AIsInsert && (LRow != null))
									if (!FATCursor.FindKey(LRow))
										throw new ClientException(ClientException.Codes.RecordNotFound);
							}
							catch
							{
								FATCursor.Close();
								throw;
							}
						}
						catch
						{
							FATCursor.Unprepare();
							throw;
						}
					}
					catch
					{
						FATCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
						FATCursor.Dispose();
						FATCursor = null;
						throw;
					}
				}
			}
		}
		
		private void UnprepareApplicationTransactionServer()
		{
			Guid LID = FApplicationTransactionID;
			FApplicationTransactionID = Guid.Empty;
			if (FOpenState == DataSetState.Browse)
			{
				if (FATCursor != null)
				{
					FATCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
					FATCursor.Dispose();
					FATCursor = null;
				}
			}
			else
				CloseCursor();
		}

		#endregion

		#region Enumeration

		public IEnumerator GetEnumerator()
		{
			return new DataViewEnumerator(this);
		}

		internal class DataViewEnumerator : IEnumerator
		{
			public DataViewEnumerator(DataView AView)
			{
				FView = AView;
			}

			private DataView FView;
			private bool FInitial = true;

			public void Reset()
			{
				FView.First();
				FInitial = true;
			}

			public object Current
			{
				get { return FView.ActiveRow; }
			}

			public bool MoveNext()
			{
				bool LResult = !FView.IsEmpty() && (FInitial || (FView.MoveBy(1) == 1));
				FInitial = false;
				return LResult;
			}
		}


		#endregion

		#region Utility

		private DataSource FDataSource = null;
		/// <summary> Utility DataSource that is linked to this DataView. </summary>
		/// <remarks> 
		///		This DataSource is created, attached and disposed by this DataView.  This is useful when programmatically 
		///		accessing a DataView to avoid having to create attach and deallocate a DataSource. 
		///	</remarks>
		[Browsable(false)]
		public DataSource DataSource
		{
			get
			{
				if (FDataSource == null)
				{
					FDataSource = new DataSource();
					FDataSource.View = this;
				}
				return FDataSource;
			}
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames, DataSetState AInitialState, bool AIsReadOnly)
		{
			DataView LDataView = new DataView();
			try
			{
				LDataView.Session = this.Session;
				LDataView.Expression = AExpression;
				LDataView.IsReadOnly = AIsReadOnly;
				LDataView.MasterSource = DataSource;
				LDataView.MasterKeyNames = AMasterKeyNames;
				LDataView.DetailKeyNames = ADetailKeyNames;
				LDataView.Open(AInitialState);
				return LDataView;
			}
			catch
			{
				LDataView.Dispose();
				throw;
			}
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The ReadOnly property for this overload will be false (read/write). </remarks>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames, DataSetState AInitialState)
		{
			return OpenDetail(AExpression, AMasterKeyNames, ADetailKeyNames, AInitialState, false);
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The OpenState will be Browse. </remarks>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames, bool AReadOnly)
		{
			return OpenDetail(AExpression, AMasterKeyNames, ADetailKeyNames, DataSetState.Browse, AReadOnly);
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The OpenState will be Browse and the ReadOnly property for this overload will be false (read/write). </remarks>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames)
		{
			return OpenDetail(AExpression, AMasterKeyNames, ADetailKeyNames, DataSetState.Browse, false);
		}

		#endregion

		#region class ViewRow

		internal class ViewRow : Disposable
		{
			public ViewRow() : base(){}
			public ViewRow(Row ARow, RowFlag ARowFlag, Guid ABookmark)
			{
				FRow = ARow;
				FRowFlag = ARowFlag;
				FBookmark = ABookmark;
			}
			
			protected override void Dispose(bool ADisposing)
			{
				if (FRow != null)
				{
					FRow.Dispose();
					FRow = null;
				}
			}
			
			private Row FRow;
			public Row Row
			{
				get { return FRow; }
				set 
				{ 
					if (FRow != null)
						FRow.Dispose();
					FRow = value; 
				}
			}
			
			private RowFlag FRowFlag;
			public RowFlag RowFlag
			{
				get { return FRowFlag; }
				set { FRowFlag = value; }
			}
			
			private Guid FBookmark;
			public Guid Bookmark
			{
				get { return FBookmark; }
				set { FBookmark = value; }
			}
		}
	    
		#endregion

		#region class ViewBuffer

		internal class ViewBuffer : DisposableTypedList
		{
			public ViewBuffer() : base(typeof(ViewRow), true, false){}

			public new ViewRow this[int AIndex]
			{
				get { return (ViewRow)base[AIndex]; }
				set { base[AIndex] = value; }
			}

			public void Add(int ACount, DataView AView)
			{
				for (int LIndex = 0; LIndex < ACount; LIndex++)
					Add(new ViewRow(new Row(AView.Process, AView.TableType.RowType), new RowFlag(), Guid.Empty));
			}
		}
		
		#endregion

		#region class ViewCursor
		
		private delegate void CursorErrorsOccurredHandler(ViewCursor ACursor, DAE.Language.D4.CompilerMessages AMessages);

		private class ViewCursor : Disposable
		{
			public ViewCursor(IServerProcess AProcess) : base()
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
			
			public event CursorErrorsOccurredHandler OnErrors;
			private void ErrorsOccurred(CompilerMessages AErrors)
			{
				if (OnErrors != null)
					OnErrors(this, AErrors);
			}
			
			private IServerCursor FCursor;
			
			private bool FShouldOpen = true;
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
						ErrorsOccurred(FPlan.Messages);
					}
					else
						FCursor = FPlan.Open(FParams);
				}
			}

			public void Close()
			{
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
			
			public bool GotoBookmark(Guid ABookmark)
			{
				if (FCursor != null)
					return FCursor.GotoBookmark(ABookmark);
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
			
			public void Insert(Row ARow)
			{
				if (FCursor != null)
					FCursor.Insert(ARow);
			}
			
			public void Update(Row ARow)
			{
				if (FCursor != null)
					FCursor.Update(ARow);
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
					return false;
			}
			
			public bool Change(Row AOldRow, Row ANewRow, string AColumnName)
			{
				if (FCursor != null)
					return FCursor.Change(AOldRow, ANewRow, AColumnName);
				else
					return false;
			}
			
			public bool Validate(Row AOldRow, Row ANewRow, string AColumnName)
			{
				if (FCursor != null)
					return FCursor.Validate(AOldRow, ANewRow, AColumnName);
				else
					return false;
			}
		}
		
		#endregion
	}

	public class DataViewException : Exception
	{
		public DataViewException(string AMessage, Exception AInner) : base(AMessage, AInner) {}
		public DataViewException(string AMessage) : base(AMessage) {}
	}

	/// <summary> Interface for classes which reference a DataSource. </summary>
	public interface IDataSourceReference
	{
		/// <summary> Referenced DataSource instance. </summary>
		DataSource Source { get; set; }
	}

	/// <summary> Interface for classes which reference a column name. </summary>
	public interface IColumnNameReference
	{
		/// <summary> Referenced column name. </summary>
		string ColumnName { get; set; }
	}

	/// <summary> Interface for classes which are able to be configured as read-only. </summary>
	public interface IReadOnly
	{
		/// <summary> When false, editing operations are not permitted. </summary>
		bool ReadOnly { get; set; }
	}

	/// <summary> Identifies the state of the DataView. </summary>
	public enum DataSetState
	{
		Inactive,	// Not active
		Insert,		// Inserting a new row
		Edit,		// Editing active row
		Browse		// Active, valid and not in insert or edit
	};
	
	/// <summary> Used by the DataView's buffer management. </summary>
	internal enum RowFlag 
	{
		Data = 1, 
		BOF = 2, 
		EOF = 4, 
		Inserted = 8
	};
	
	public enum EnlistMode { Default, True, False }
    
    public class DataFields : ICollection
    {
		internal DataFields(DataView AView)
		{
			FView = AView;
		}
		
		DataView FView;
		
		public int Count
		{
			get { return FView.FieldCount; }
		}
		
		public IEnumerator GetEnumerator()
		{
			return new DataFieldEnumerator(FView);
		}
		
		public DataField this[int AIndex]
		{
			get { return FView[AIndex]; }
		}
		
		public DataField this[string AColumnName]
		{
			get { return FView[AColumnName]; }
		}
		
		public void CopyTo(Array ATarget, int AStartIndex)
		{
			for (int i = 0; i < Count; i++)
				ATarget.SetValue(this[i], i + AStartIndex);
		}
		
		public bool IsReadOnly
		{
			get { return true; }
		}
		
		public bool IsSynchronized
		{
			get { return false; }
		}
		
		public object SyncRoot
		{
			get { return FView.FFields; }
		}

		public class DataFieldEnumerator : IEnumerator
		{
			internal DataFieldEnumerator(DataView AView)
			{
				FView = AView;
			}
			
			private DataView FView;
			private int FIndex = -1;
			
			public void Reset()
			{
				FIndex = -1;
			}
			
			public bool MoveNext()
			{
				FIndex++;
				return (FIndex < FView.FieldCount);
			}
			
			public object Current
			{
				get { return FView[FIndex]; }
			}
		}
    }
    
    public class DataField : Schema.Object, IConvertible
    {
		protected internal DataField(DataView AView, Schema.Column AColumn) : base(AColumn.Name)
		{
			FView = AView;
			FColumn = AColumn;
			FColumnIndex = AView.TableType.Columns.IndexOfName(AColumn.Name);
		}

		private DataView FView;
		/// <summary> The DataView this field is contained in. </summary>
		public DataView View
		{
			get { return FView; }
		}

		/// <summary> The name of the underlying column. </summary>
		public string ColumnName
		{
			get { return FColumn.Name; }
		}

		private Schema.Column FColumn;
		public Schema.IScalarType DataType
		{
			get { return (Schema.IScalarType)FColumn.DataType; }
		}

		private int FColumnIndex;
		public int ColumnIndex
		{
			get { return FColumnIndex; }
		}

		/// <summary> Asks any control(s) that might be attached to this field to set focus. </summary>
		public void FocusControl()
		{
			FView.FocusControl(this);
		}

		private void CheckHasValue()
		{
			if (!HasValue())
				throw new ClientException(ClientException.Codes.NoValue, ColumnName);
		}

		private void CheckViewNotEmpty()
		{
			if (FView.IsEmpty())
				throw new ClientException(ClientException.Codes.EmptyView);
		}
		
		// Value Access
		public DataValue Value
		{
			get
			{
				CheckHasValue();
				return FView.ActiveRow[FColumnIndex];
			}
			set
			{
				FView.ChangeColumn(this, value);
			}
		}
		
		public DataValue OldValue
		{
			get
			{
				return FView.OldRow[FColumnIndex];
			}
		}
		
		/// <summary> True when the field has a value. </summary>
		public bool HasValue()
		{
			CheckViewNotEmpty();
			return FView.ActiveRow.HasValue(FColumnIndex);
		}

		public bool IsNil
		{
			get
			{
				CheckViewNotEmpty();
				return !FView.ActiveRow.HasValue(FColumnIndex);
			}
		}
		
		public void ClearValue()
		{
			Value = null;
		}

		public bool AsBoolean
		{
			get 
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsBoolean; 
				return false;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsBoolean = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public bool GetAsBoolean(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsBoolean(ARepresentationName);
			return false;
		}
		
		public void SetAsBoolean(string ARepresentationName, bool AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsBoolean(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public byte AsByte
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsByte;
				return (byte)0;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsByte = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public byte GetAsByte(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsByte(ARepresentationName);
			return (byte)0;
		}
		
		public void SetAsByte(string ARepresentationName, byte AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsByte(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public decimal AsDecimal
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsDecimal;
				return 0m;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsDecimal = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public decimal GetAsDecimal(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsDecimal(ARepresentationName);
			return 0m;
		}
		
		public void SetAsDecimal(string ARepresentationName, decimal AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsDecimal(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public TimeSpan AsTimeSpan
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsTimeSpan;
				return TimeSpan.Zero;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsTimeSpan = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public TimeSpan GetAsTimeSpan(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsTimeSpan(ARepresentationName);
			return TimeSpan.Zero;
		}
		
		public void SetAsTimeSpan(string ARepresentationName, TimeSpan AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsTimeSpan(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public DateTime AsDateTime
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsDateTime;
				return DateTime.MinValue;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsDateTime = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}

		public DateTime GetAsDateTime(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsDateTime(ARepresentationName);
			return DateTime.MinValue;
		}
		
		public void SetAsDateTime(string ARepresentationName, DateTime AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsDateTime(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		private const string CDoubleNotSupported = "Alphora.Dataphor.DAE.Client.DataField: Double not supported.  Use Float(single) instead.";

		public double AsDouble
		{
			get
			{
				throw new Exception(CDoubleNotSupported);
			}
			set
			{
				throw new Exception(CDoubleNotSupported);
			}
		}
		
		public short AsInt16
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsInt16;
				return (short)0;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsInt16 = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public short GetAsInt16(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsInt16(ARepresentationName);
			return (short)0;
		}
		
		public void SetAsInt16(string ARepresentationName, short AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsInt16(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public int AsInt32
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsInt32;
				return 0;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsInt32 = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public int GetAsInt32(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsInt32(ARepresentationName);
			return 0;
		}
		
		public void SetAsInt32(string ARepresentationName, int AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsInt32(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public long AsInt64
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsInt64;
				return (long)0;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsInt64 = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public long GetAsInt64(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsInt64(ARepresentationName);
			return (long)0;
		}
		
		public void SetAsInt64(string ARepresentationName, long AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsInt64(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public string AsString
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsString;
				return String.Empty;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsString = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public string GetAsString(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsString(ARepresentationName);
			return String.Empty;
		}
		
		public void SetAsString(string ARepresentationName, string AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsString(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public string AsDisplayString
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsDisplayString;
				return String.Empty;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsDisplayString = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public Guid AsGuid
		{
			get
			{
				CheckViewNotEmpty();
				if (HasValue())
					return Value.AsGuid;
				return Guid.Empty;
			}
			set
			{
				CheckViewNotEmpty();
				Scalar LValue = new Scalar(FView.Process, DataType, null);
				LValue.AsGuid = value; // Has to be done this way in order to use the native accessor functionality
				Value = LValue;
			}
		}
		
		public Guid GetAsGuid(string ARepresentationName)
		{
			CheckViewNotEmpty();
			if (HasValue())
				return Value.GetAsGuid(ARepresentationName);
			return Guid.Empty;
		}
		
		public void SetAsGuid(string ARepresentationName, Guid AValue)
		{
			CheckViewNotEmpty();
			Scalar LValue = new Scalar(FView.Process, DataType, null);
			LValue.SetAsGuid(ARepresentationName, AValue); // Has to be done this way in order to use the native accessor functionality
			Value = LValue;
		}
		
		public object AsType(Type AType)
		{
			switch (AType.ToString())
			{
				case ("Boolean") : return AsBoolean;
				case ("Byte") : return AsByte;
				case ("Int16") : return AsInt16;
				case ("Int32") : return AsInt32;
				case ("Int64") : return AsInt64;
				case ("String") : return AsString;
				default : throw new ClientException(ClientException.Codes.CannotConvertFromType, AType.ToString());
			}
		}
		
		// IConvertible interface
		TypeCode IConvertible.GetTypeCode()
		{
			throw new DataViewException("Internal Error: DataField.IConvertible.GetTypeCode()");
		}
		bool IConvertible.ToBoolean(IFormatProvider AProvider)
		{
			return AsBoolean;
		}
		byte IConvertible.ToByte(IFormatProvider AProvider)
		{
			return AsByte;
		}
		char IConvertible.ToChar(IFormatProvider AProvider)
		{
			return (char)AsType(typeof(char));
		}
		DateTime IConvertible.ToDateTime(IFormatProvider AProvider)
		{
			return (DateTime)AsType(typeof(DateTime));
		}
		decimal IConvertible.ToDecimal(IFormatProvider AProvider)
		{
			return AsDecimal;
		}
		double IConvertible.ToDouble(IFormatProvider AProvider)
		{
			throw new Exception(CDoubleNotSupported);
		}
		short IConvertible.ToInt16(IFormatProvider AProvider)
		{
			return AsInt16;
		}
		int IConvertible.ToInt32(IFormatProvider AProvider)
		{
			return AsInt32;
		}
		long IConvertible.ToInt64(IFormatProvider AProvider)
		{
			return AsInt64;
		}
		sbyte IConvertible.ToSByte(IFormatProvider AProvider)
		{
			return (sbyte)AsType(typeof(sbyte));
		}
		float IConvertible.ToSingle(IFormatProvider AProvider)
		{
			return (float)AsType(typeof(float));
		}
		string IConvertible.ToString(IFormatProvider AProvider)
		{
			return AsString;
		}
		object IConvertible.ToType(Type AConversionType, IFormatProvider AProvider)
		{
			return AsType(AConversionType);
		}
		ushort IConvertible.ToUInt16(IFormatProvider AProvider)
		{
			return (ushort)AsType(typeof(ushort));
		}
		uint IConvertible.ToUInt32(IFormatProvider AProvider)
		{
			return (uint)AsType(typeof(uint));
		}
		ulong IConvertible.ToUInt64(IFormatProvider AProvider)
		{
			return (ulong)AsType(typeof(ulong));
		}
		
		// Explicit operator conversions
		public static explicit operator bool(DataField AField)
		{
			return AField.AsBoolean;
		}
		public static explicit operator byte(DataField AField)
		{
			return AField.AsByte;
		}
		public static explicit operator decimal(DataField AField)
		{
			return AField.AsDecimal;
		}
		public static explicit operator int(DataField AField)
		{
			return AField.AsInt32;
		}
		public static explicit operator long(DataField AField)
		{
			return AField.AsInt64;
		}
		public static explicit operator short(DataField AField)
		{
			return AField.AsInt16;
		}
    }

	public delegate void ErrorsOccurredHandler(DataView AView, CompilerMessages AMessages);    
    public delegate void DataLinkHandler (DataLink ALink, DataSet ADataSet);
    public delegate void DataLinkFieldHandler (DataLink ALink, DataSet ADataSet, DataField AField);
    public delegate void DataLinkScrolledHandler (DataLink ALink, DataSet ADataSet, int ADelta);
	
	/// <summary> An internal link to a DataView component. </summary>
	/// <remarks> 
	///		This link is to be used by controls and other possible consumers of data from 
	///		a DataView.  The link provides notification of various state changes and also 
	///		requests to update the DataView with any changes.  Through the BufferCount, 
	///		the link can also get a buffer of rows surrounding the active row. 
	///	</remarks>
	[ToolboxItem(false)]
    public class DataLink : Disposable, IDataSourceReference
    {
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Source = null;
		}
		
		private DataSource FSource;
		/// <summary> The <see cref="DataSource"/> associated with this DataLink. </summary>
		public DataSource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
				{
					if (FSource != null)
					{
						FSource.RemoveLink(this);
						FSource.Disposed -= new EventHandler(DataSourceDisposed);
						FSource.OnViewChanged -= new EventHandler(DataSourceViewChanged);
					}
					FSource = value;
					if (FSource != null)
					{
						FSource.OnViewChanged += new EventHandler(DataSourceViewChanged);
						FSource.Disposed += new EventHandler(DataSourceDisposed);
						FSource.AddLink(this);
					}

					DataViewChanged();
				}
			}
		}
		
		private void DataSourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}
		
		private void DataSourceViewChanged(object ASender, EventArgs AArgs)
		{
			DataViewChanged();
		}

		private void DataViewChanged()
		{
			// Set FActive to opposite of Active to ensure that ActiveChanged is fired (so that the control always get's an ActiveChanged on way or another)
			FActive = !Active;
			StateChanged();
		}
		
		/// <summary> Gets the DataView associated with this link, or null if none. </summary>
		public DataView View
		{
			get
			{
				if (FSource == null)
					return null;
				else
					return FSource.View;
			}
		}
		
		/// <summary> Indicates whether the link is active (connected to an active <see cref="DataView"/>). </summary>
		public bool Active
		{
			get { return (View != null) && View.Active; }
		}

		internal int FFirstOffset;	// This is maintained by the DataView
		
		/// <summary> Gets the active row offset relative to the link. </summary>
		/// <remarks> Do not attempt to retrieve this if the link is not <see cref="Active"/>. </remarks>
		public int ActiveOffset
		{
			get { return View.FActiveOffset - FFirstOffset; }
		}

		/// <summary> Retrieves a DataView buffer row relative to this link. </summary>
		/// <remarks> 
		///		Do not attempt to access this if the link is not <see cref="Active"/>.  Do not modify 
		///		the buffer rows in any way, use them only to read data. 
		///	</remarks>
		public DAE.Runtime.Data.Row Buffer(int AIndex)
		{
			return View.FBuffer[AIndex + FFirstOffset].Row;
		}

		/// <summary> The offset of the last valid row relative to the link. </summary>
		/// <remarks> 
		///		This will be less than (BufferCount - 1) if there are insuficient rows present in underlying 
		///		table.  This value will be -1 if there are no rows available.  Do not attempt to retrieve 
		///		this if the link is not <see cref="Active"/>.
		/// </remarks>
		public int LastOffset
		{
			get 
			{ 
				int LResult = FFirstOffset + (FBufferCount - 1);
				if (LResult > View.EndOffset)
					LResult = View.EndOffset;
				return LResult - FFirstOffset;
			}
		}

		private int FBufferCount = 1;
		/// <summary> The maximum number of rows that are needed by this link. </summary>
		/// <remarks> 
		///		The BufferCount will always be at least one.  Attempting to set BufferCount 
		///		to less than 1 will effectively set it to 1.  A link may not actually be 
		///		able to access as many rows as it requests if there are not enough rows in
		///		the underlying data set.  Use <see cref="LastOffset"/> to determine the last 
		///		row in the buffer that can actually be accessed.
		///	</remarks>
		public int BufferCount
		{
			get { return FBufferCount; }
			set
			{
				if (value < 1)
					value = 1;
				if (FBufferCount != value)
				{
					FBufferCount = value;
					if (Active)
						FSource.LinkBufferRangeChanged();	// Make any adjustment to the DataView buffer size
				}
			}
		}

		/// <summary> Requests that the link post any data changes that are pending. </summary>
		public event DataLinkHandler OnSaveRequested;
		/// <summary> Requests that the link post any data changes that are pending. </summary>
		public virtual void SaveRequested()
		{
			if (OnSaveRequested != null)
				OnSaveRequested(this, View);
		}
		
		/// <summary> Called right before posting; for example, to ensure that details are posted before posting of the master occurs. </summary>
		public event DataLinkHandler OnPrepareToPost;
		/// <summary> Called right before posting; for example, to ensure that details are posted before posting of the master occurs. </summary>
		protected internal virtual void PrepareToPost()
		{
			if (OnPrepareToPost != null)
				OnPrepareToPost(this, View);
		}

		/// <summary> Called right before canceling; for example, to ensure that details are canceled before canceling of the master occurs. </summary>
		public event DataLinkHandler OnPrepareToCancel;
		/// <summary> Called right before canceling; for example, to ensure that details are canceled before canceling of the master occurs. </summary>
		protected internal virtual void PrepareToCancel()
		{
			if (OnPrepareToCancel != null)
				OnPrepareToCancel(this, View);
		}
		
		/// <summary> Called when the active row's data is changing as a result of data edits. </summary>
		public event DataLinkFieldHandler OnRowChanging;
		/// <summary> Called when the active row's data is changing as a result of data edits. </summary>
		protected internal virtual void RowChanging(DataField AField)
		{
			if (OnRowChanging != null)
				OnRowChanging(this, View, AField);
		}

		/// <summary> Called when the active row's data changed as a result of data edits. </summary>
		public event DataLinkFieldHandler OnRowChanged;
		/// <summary> Called when the active row's data changed as a result of data edits. </summary>
		protected internal virtual void RowChanged(DataField AField)
		{
			if (OnRowChanged != null)
				OnRowChanged(this, View, AField);
		}

		/// <summary> Ensures that the link's buffer range encompasses the active row. </summary>
		/// <remarks> Assumes that the link is active. </remarks>
		protected internal virtual void UpdateRange()
		{
			FFirstOffset += Math.Min(View.FActiveOffset - FFirstOffset, 0);							// Slide buffer down if active is below first
			FFirstOffset += Math.Max(View.FActiveOffset - (FFirstOffset + (FBufferCount - 1)), 0);	// Slide buffer up if active is above last
		}

		/// <summary> Scrolls the links buffer by the given delta, then ensures that the link buffer is kept in range of the active row. </summary>
		/// <remarks> 
		///		A delta is passed when the DataView's buffer size is adjusted (to keep the link as close as 
		///		possible to its prior relative position). For example, if the DataView discards the first couple 
		///		rows of its buffer, this method is called with a Delta of -2 so that the rows in this link's 
		///		buffer appear to be unmoved. 
		///	</remarks>
		protected internal virtual void UpdateFirstOffset(int ADelta)
		{
			FFirstOffset += ADelta;
			UpdateRange();
		}
	
		/// <summary> Called when data changes except for field-level data edits. </summary>
		public event DataLinkHandler OnDataChanged;
		/// <summary> Called when data changes except for field-level data edits. </summary>
		protected virtual void DataChanged()
		{
			if (OnDataChanged != null)
				OnDataChanged(this, View);
		}
		
		internal void InternalDataChanged()
		{
			if (Active)
				UpdateRange();
			DataChanged();
		}
		
		/// <summary> Asks the link to focus any control that might be associated with this link. </summary>
		public event DataLinkFieldHandler OnFocusControl;
		/// <summary> Asks the link to focus any control that might be associated with this link. </summary>
		protected internal virtual void FocusControl(DataField AField)
		{
			if (OnFocusControl != null)
				OnFocusControl(this, View, AField);
		}
		
		/// <summary> Called when the DataView's state has changed. </summary>
		public event DataLinkHandler OnStateChanged;
		/// <summary> Called when the DataView's state has changed. </summary>
		protected internal virtual void StateChanged()
		{
			if (Active != FActive)
			{
				ActiveChanged();
				FActive = Active;
			}

			if (OnStateChanged != null)
				OnStateChanged(this, View);
		}
		
		private bool FActive;
		/// <summary> Called when the view's active state changes or when attached/detached from an active view. </summary>
		public event DataLinkHandler OnActiveChanged;
		/// <summary> Called when the view's active state changes or when attached/detached from an active view. </summary>
		protected internal virtual void ActiveChanged()
		{
			if (Active)
				UpdateRange();
			else
				FFirstOffset = 0;
				
			if (OnActiveChanged != null)
				OnActiveChanged(this, View);
		}
    }

	/// <summary> A level of indirection between data controls and the DataView. </summary>
	/// <remarks> Data aware controls are attached to a DataView by linking them to a DataSource which is connected to a DataView. </remarks>
	[ToolboxItem(true)]
	[DefaultEvent("OnViewChanged")]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DataSource),"Icons.DataSource.bmp")]
    public class DataSource : Component, IDisposableNotify
    {
		public DataSource() : base()
		{
			InternalInitialize();
		}

		public DataSource(IContainer AContainer)
		{
			InternalInitialize();
			if (AContainer != null)
				AContainer.Add(this);
		}

		private void InternalInitialize()
		{
			FLinks = new ArrayList();
		}

		protected override void Dispose(bool ADisposing)
		{
			View = null;
			base.Dispose(ADisposing);
			FLinks = null;
		}
		
		internal ArrayList FLinks;
		internal void AddLink(DataLink ALink)
		{
			FLinks.Add(ALink);
			LinkBufferRangeChanged();
		}
		
		internal void RemoveLink(DataLink ALink)
		{
			FLinks.Remove(ALink);
			LinkBufferRangeChanged();
		}
		
		internal void LinkBufferRangeChanged()
		{
			if ((FView != null) && (FView.Active))
				FView.BufferUpdateCount(false);
		}
		
		private DataView FView;
		/// <summary> The <see cref="DataView"/> that is associated with this DataSource. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		public DataView View
		{
			get { return FView; }
			set
			{
				if (FView != value)
				{
					if (FView != null)
					{
						FView.RemoveSource(this);
						FView.Disposed -= new EventHandler(ViewDisposed);
					}
					FView = value;
					if (FView != null)
					{
						FView.AddSource(this);
						FView.Disposed += new EventHandler(ViewDisposed);
					}
					ViewChanged();
				}
			}
		}
		
		private void ViewDisposed(object ASender, EventArgs AArgs)
		{
			View = null;
		}
		
		public event EventHandler OnViewChanged;
		
		protected virtual void ViewChanged()
		{
			if (OnViewChanged != null)
				OnViewChanged(this, EventArgs.Empty);
		}
	}
	
	public delegate void ParamStructureChangedHandler(object ASender);
	public delegate void ParamChangedHandler(object ASender);
	
	public class DataViewParam : object
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
		
		private Scalar FValue;
		public Scalar Value
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
	
	public class DataViewParams : TypedList
	{
		public DataViewParams() : base(typeof(DataViewParam)) {}
		
		public new DataViewParam this[int AIndex]
		{
			get { return (DataViewParam)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Adding(object AValue, int AIndex)
		{
			((DataViewParam)AValue).OnParamChanged += new ParamChangedHandler(DataViewParamChanged);
			((DataViewParam)AValue).OnParamStructureChanged += new ParamStructureChangedHandler(DataViewParamStructureChanged);
			DataViewParamStructureChanged(null);
		}
		
		protected override void Removing(object AValue, int AIndex)
		{
			((DataViewParam)AValue).OnParamStructureChanged -= new ParamStructureChangedHandler(DataViewParamStructureChanged);
			((DataViewParam)AValue).OnParamChanged -= new ParamChangedHandler(DataViewParamChanged);
			DataViewParamStructureChanged(null);
		}
		
		private void DataViewParamChanged(object ASender)
		{
			ParamChanged(ASender);
		}
		
		private void DataViewParamStructureChanged(object ASender)
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

	public class DataViewParamGroup : Disposable
	{
		public DataViewParamGroup() : base()
		{
			FMasterLink = new DataLink();
			FMasterLink.OnRowChanged += new DataLinkFieldHandler(MasterRowChanged);
			FMasterLink.OnDataChanged += new DataLinkHandler(MasterDataChanged);
			FMasterLink.OnActiveChanged += new DataLinkHandler(MasterActiveChanged);
			
			FParams = new DataViewParams();
			FParams.OnParamChanged += new ParamChangedHandler(DataViewParamChanged);
			FParams.OnParamStructureChanged += new ParamStructureChangedHandler(DataViewParamStructureChanged);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FParams != null)
			{
				FParams.OnParamStructureChanged -= new ParamStructureChangedHandler(DataViewParamStructureChanged);
				FParams.OnParamChanged -= new ParamChangedHandler(DataViewParamChanged);
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

		private DataViewParams FParams;
		public DataViewParams Params { get { return FParams; } }
		
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

		private void DataViewParamStructureChanged(object ASender)
		{
			ParamStructureChanged();
		}
		
		private void DataViewParamChanged(object ASender)
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
	
	public class DataViewParamGroups : DisposableTypedList
	{
		public DataViewParamGroups() : base(typeof(DataViewParamGroup)) {}
		
		public new DataViewParamGroup this[int AIndex]
		{
			get { return (DataViewParamGroup)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			((DataViewParamGroup)AValue).OnParamChanged += new ParamChangedHandler(DataViewParamChanged);
			((DataViewParamGroup)AValue).OnParamStructureChanged += new ParamStructureChangedHandler(DataViewParamStructureChanged);
			DataViewParamStructureChanged(null);
		}
		
		protected override void Removing(object AValue, int AIndex)
		{
			((DataViewParamGroup)AValue).OnParamStructureChanged -= new ParamStructureChangedHandler(DataViewParamStructureChanged);
			((DataViewParamGroup)AValue).OnParamChanged -= new ParamChangedHandler(DataViewParamChanged);
			DataViewParamStructureChanged(null);
			base.Removing(AValue, AIndex);
		}
		
		private void DataViewParamChanged(object ASender)
		{
			ParamChanged(ASender);
		}
		
		private void DataViewParamStructureChanged(object ASender)
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

	internal abstract class DataViewDataParam : Runtime.DataParam
	{
		public DataViewDataParam(string AName, Schema.IDataType ADataType, Modifier AModifier) : base(AName, ADataType, AModifier) {}
		
		public abstract void Bind(IServerProcess AProcess);
	}
	
	internal class MasterDataViewDataParam : DataViewDataParam
	{
		public MasterDataViewDataParam(string AName, Schema.IDataType ADataType, Modifier AModifier, string AColumnName, DataSource ASource, bool AIsMaster) : base(AName, ADataType, AModifier)
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
			if (!Source.View.IsEmpty() && Source.View.Fields[ColumnName].HasValue())
				Value = Source.View.Fields[ColumnName].Value.Copy(AProcess);
		}
	}
	
	internal class SourceDataViewDataParam : DataViewDataParam
	{
		public SourceDataViewDataParam(DataViewParam ASourceParam) : base(ASourceParam.Name, ASourceParam.DataType, ASourceParam.Modifier) 
		{
			SourceParam = ASourceParam;
		}
		
		public DataViewParam SourceParam;
		
		public override void Bind(IServerProcess AProcess)
		{
			Value = SourceParam.Value.Copy(AProcess);
		}
	}
}

