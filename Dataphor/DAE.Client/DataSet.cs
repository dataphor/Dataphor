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
using System.Collections.Generic;
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
	/// <summary> Base DataSet component providing all the behavior common to all DataSets </summary>
	///	<remarks> Maintains a sized buffer of Rows based on the needs of linked dependants. </remarks>    
	public abstract class DataSet : Component, IDisposableNotify, ISupportInitialize, IEnumerable
	{
		// Do not localize
		public readonly static Char[] CColumnNameDelimiters = new Char[] {',',';'};

		public DataSet() : base()
		{
			FBuffer = new DataSetBuffer();
			BufferClear();
			FFields = new Schema.Objects(10);
			FDataFields = new DataFields(this);
			RefreshAfterPost = true;
			FIsWriteOnly = false;
		}

		public DataSet(IContainer AContainer) : this()
		{
			if (AContainer != null)
				AContainer.Add(this);
		}

		protected virtual void InternalDispose(bool ADisposing)
		{
			Close();
		}

		protected override void Dispose(bool ADisposing)
		{
			FDisposing = true;
			try
			{
				try
				{
					InternalDispose(ADisposing);
				}
				finally
				{
					base.Dispose(ADisposing);

					if (FFields != null)
					{
						FFields.Clear();
						FFields = null;
					}

					if (FSources != null)
					{
						while(FSources.Count != 0)
							FSources[FSources.Count - 1].DataSet = null;
						FSources = null;
					}

					ClearOriginalRow();

					if (FBuffer != null)
					{
						FBuffer.Dispose();
						FBuffer = null; 
					}
				}
			}
			finally
			{
				FDisposing = false;
			}
		}
		
		protected bool FDisposing;
		
		#region Buffer Maintenance

		// <summary> Row buffer.  Always contains at least two Rows. </summary>
		internal DataSetBuffer FBuffer;

		// <summary> The number of "filled" rows in the buffer. </summary>
		private int FEndOffset;

		/// <summary> Offset of current Row from origin of buffer. </summary>
		private int FCurrentOffset;

		/// <summary> Offset of active Row from origin of buffer. </summary>
		internal int FActiveOffset;

		/// <summary> The actively selected row of the DataSet's buffer. </summary>
		[Browsable(false)]
		public Row ActiveRow { get { return FBuffer[FActiveOffset].Row; } }
		
		/// <summary> Tracks whether the values for each column in the active row have been modified. </summary>
		protected internal BitArray FValueFlags;
		
		protected Row FOriginalRow;
		/// <summary> Tracks the original row during an edit. </summary>
		/// <remarks> The original values for a row can only be accessed when the dataset is in edit state. </remarks>
		protected internal Row OriginalRow
		{
			get
			{
				if (FState == DataSetState.Edit)
					throw new ClientException(ClientException.Codes.OriginalValueNotAvailable);
				return FOriginalRow;
			}
		}

		protected Row FOldRow;
		/// <summary> The old row for the current row changed event. </summary>
		/// <remarks> This property can only be accessed during change events for the field and dataset. </remarks>
		protected internal Row OldRow
		{
			get
			{
				if (FOldRow == null)
					throw new ClientException(ClientException.Codes.OldValueNotAvailable);
				return FOldRow;
			}
		}

		/// <summary> Gets the maximum number of rows that are buffered for active controls on this DataSet. </summary>
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

		protected Row RememberActive()
		{
			if (FActiveOffset <= FEndOffset)
				return (Row)FBuffer[FActiveOffset].Row.Copy();
			else
				return null;
		}

		private void SaveOriginalRow()
		{
			Row LRow = RememberActive();
			if (LRow != null)
				FOriginalRow = LRow;
			else
				ClearOriginalRow();
				
			if (FValueFlags != null)
				FValueFlags.SetAll(false);
		}
		
		private void ClearOriginalRow()
		{
			if (FOriginalRow != null)
			{
				FOriginalRow.Dispose();
				FOriginalRow = null;
			}
			
			if (FValueFlags != null)
				FValueFlags.SetAll(false);
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
				CurrentGoto(FActiveOffset, AStrict, true);
		}
		
		/// <summary> Sets the underlying cursor to a specific buffer row. </summary>
		/// <param name="AStrict"> If true, will throw an exception if unable to position on the buffered row. </param>
		/// <param name="AForward"> Hint about the intended direction of movement after cursor positioning. </param>
		private void CurrentGoto(int AValue, bool AStrict, bool AForward)
		{
			if (FCurrentOffset != AValue)
			{
				RowFlag LFlag = FBuffer[AValue].RowFlag;
				
				// If exception is thrown, current should still be valid (cursor should not move if unable to comply)
				bool LResult;
				if ((LFlag & RowFlag.Data) != 0)
					LResult = InternalGotoBookmark(FBuffer[AValue].Bookmark, AForward);
				else if ((LFlag & RowFlag.Inserted) != 0)
				{
					LResult = true;
					if ((LFlag & RowFlag.BOF) != 0)
					{
						InternalFirst();
						InternalNext();
					}
					else
					{
						InternalLast();
						InternalPrior();
					}
				}
				else
				{
					// If there is no bookmark, we are on an inserted row and RefreshAfterPost is false so the cursor has not been
					// repositioned to the newly inserted row. However, if the cursor is searchable, we should be able to reposition
					// to the newly inserted row using the row values, rather than a bookmark.
					LResult = InternalFindKey(FBuffer[AValue].Row);
				}

				if (AStrict && !LResult)
					throw new ClientException(ClientException.Codes.BookmarkNotFound);

				FCurrentOffset = AValue;
			}
		}

		private void FinalizeBuffers(int LFirst, int LLast)
		{
			Guid[] LBookmarks = new Guid[LLast - LFirst + 1];
			for (int i = LFirst; i <= LLast; i++)
			{
				LBookmarks[i - LFirst] = FBuffer[i].Bookmark;
				FBuffer[i].Bookmark = Guid.Empty;
				FBuffer[i].Row.ClearValues();
			}
			InternalDisposeBookmarks(LBookmarks);
		}

		private void FinalizeBuffer(DataSetRow ARow)
		{
			if (ARow.Bookmark != Guid.Empty)
			{
				InternalDisposeBookmark(ARow.Bookmark);
				ARow.Bookmark = Guid.Empty;
			}
			ARow.Row.ClearValues();
		}

		/// <summary> Selects values from cursor into specified Row of buffer. </summary>
		private void CursorSelect(int AIndex)
		{
			DataSetRow LRow = FBuffer[AIndex];
			FinalizeBuffer(LRow);
			InternalSelect(LRow.Row);
			LRow.Bookmark = InternalGetBookmark();
			LRow.RowFlag = RowFlag.Data;
		}

		private void BufferClear()
		{
			FEndOffset = -1;
			FActiveOffset = 0;
			FCurrentOffset = -1;
			FInternalBOF = true;
			FInternalEOF = true;
		}
		
		private void BufferActivate()
		{
			FEndOffset = 0;
			FActiveOffset = 0;
			FCurrentOffset = 0;
			FInternalBOF = false;
			FInternalEOF = false;
		}
		
		// Closes the buffer, finalizing the rows it contains, but leaves the buffer space intact. Used when closing the underlying cursor.
		private void BufferClose()
		{
			BufferClear();
			FinalizeBuffers(0, FBuffer.Count - 1);
		}

		// Closes the buffer, finalizing the rows it contains, and clearing the buffer space. Only used when closing the dataset.
		private void CloseBuffer()
		{
			BufferClose();
			FBuffer.Clear();
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

				CurrentGoto(FEndOffset, false, true);
			
				// Skip the move next if the row in question is being inserted (it's cursor info is a duplicate of the preceeding row)
				if ((FState == DataSetState.Insert) && (FCurrentOffset == FActiveOffset))
					LGetNext = false;
			}

			// Attempt to navigate to the next row
			if (LGetNext && !InternalNext())
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
			int LResult = 0;
			while ((FEndOffset < (FBuffer.Count - 2)) && CursorNextRow())
				LResult++;
			return LResult;
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

				CurrentGoto(0, false, false);
			}
		
			// Attempt to navigate to the prior row
			if (!InternalPrior())
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
			while ((FEndOffset < (FBuffer.Count - 2)) && CursorPriorRow())
				LResult++;
			return LResult;
		}
		
		/// <summary> Resyncronizes the row buffer from the cursor. </summary>
		private void Resync(bool AExact, bool ACenter)
		{
			if (AExact)
			{
				CurrentReset();
				if (InternalGetBOF() || InternalGetEOF())
					throw new ClientException(ClientException.Codes.RecordNotFound);
			}
			else
			{
				// This define forces the server cursor to recognize the BOF and EOF flags by actually navigating
				// If RELYONCURSORFLAGSAFTERUPDATE is defined, the dataset assumes that the BOF and EOF flags are set correctly after an update,
				// which is not always the case. This is a bug with the DAE, and when it is fixed, we can take advantage of this optimization in the dataset.
				#if !RELYONCURSORFLAGSAFTERUPDATE
				if (InternalGetBOF())
					InternalNext();
				if (InternalGetEOF())
					InternalPrior();
				#endif
				if (InternalGetBOF() || InternalGetEOF())
				{
					BufferClear();
					ForcedDataChanged();
					return;
				}
				#if RELYONCURSORFLAGSAFTERUPDATE
				if (InternalGetBOF())
					InternalNext();
				if (InternalGetEOF())
					InternalPrior();
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
				ForcedDataChanged();
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
					FInternalBOF = true;
					ForcedDataChanged();
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
				CursorNextRows();

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
		
		#region Internal Cursor Abstraction
		
		// Connection
		protected abstract void InternalConnect();
		protected abstract void InternalDisconnect();

		// State
		protected abstract void InternalOpen();
		protected abstract void InternalClose();
		
		// Indicates whether or not the dataset will perform modifications using the main cursor
		protected abstract bool UpdatesThroughCursor();
		
		// Navigable
		protected abstract bool InternalNext();
		protected abstract void InternalLast();
		protected abstract void InternalSelect(Row ARow);
		protected abstract void InternalReset();
		protected abstract bool InternalGetBOF();
		protected abstract bool InternalGetEOF();
		
		// BackwardsNavigable
		protected abstract void InternalFirst();
		protected abstract bool InternalPrior();
		
		// Bookmarkable
		protected abstract Guid InternalGetBookmark();
		protected abstract bool InternalGotoBookmark(Guid ABookmark, bool AForward);
		protected abstract void InternalDisposeBookmark(Guid ABookmark);
		protected abstract void InternalDisposeBookmarks(Guid[] ABookmarks);
		
		// Searchable
		protected abstract void InternalRefresh(Row ARow);
		protected abstract Row InternalGetKey();
		protected abstract bool InternalFindKey(Row AKey);
		protected abstract void InternalFindNearest(Row AKey);

		// Updateable
		protected virtual void InternalEdit() {}
		protected virtual void InternalInsertAppend() {}
		protected virtual void InternalInitializeRow(Row ARow)
		{
			InternalDefault(ARow);
		}
		protected virtual void InternalCancel() {}
		protected virtual void InternalValidate(bool AIsPosting) 
		{
			// Ensure that all non-nillable are not nil
			foreach (DAE.Schema.TableVarColumn LColumn in TableVar.Columns)
				if (!LColumn.IsNilable && this[LColumn.Name].IsNil)
				{
					this[LColumn.Name].FocusControl();
					throw new ClientException(ClientException.Codes.ColumnRequired, LColumn.Name);
				}
		}

		protected virtual void InternalDefault(Row ARow)
		{
			DoDefault();
		}
		
		protected virtual bool InternalColumnChanging(DataField AField, Row AOldRow, Row ANewRow)
		{
			DoRowChanging(AField);
			return true;
		}
		
		protected virtual bool InternalColumnChanged(DataField AField, Row AOldRow, Row ANewRow)
		{
			DoRowChanged(AField);
			return true;
		}
		
		protected virtual void InternalChangeColumn(DataField AField, Row AOldRow, Row ANewRow)
		{
			try
			{
				InternalColumnChanging(AField, AOldRow, ANewRow);
			}
			catch
			{
				if (AOldRow.HasValue(AField.ColumnIndex))
					ANewRow[AField.ColumnIndex] = AOldRow[AField.ColumnIndex];
				else
					ANewRow.ClearValue(AField.ColumnIndex);
				throw;
			}
			
			InternalColumnChanged(AField, AOldRow, ANewRow);
		}
		
		protected abstract void InternalPost(Row ARow);
		protected abstract void InternalDelete();
		protected abstract void InternalCursorSetChanged(Row ARow, bool AReprepare);

		#endregion
		
		#region DataLinks & Sources

		private List<DataSource> FSources = new List<DataSource>();

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

		/// <summary> Requests that the associated links make any pending updates to the DataSet. </summary>
		public void RequestSave()
		{
			if ((FState == DataSetState.Edit) || (FState == DataSetState.Insert))
				foreach (DataLink LLink in EnumerateLinks())
					LLink.SaveRequested();
		}
		
		/// <summary> Returns the current set of DataLinks that are associated with all DataSources that reference this DataSet. </summary>
		/// <returns> 
		///		It is important that this method generate an array, so that a snapshot of the links is used for iteration; 
		///		operations on the array may cause DataLinks to be created or destroyed. 
		///	</returns>
		protected DataLink[] EnumerateLinks()
		{
			List<DataLink> LTempList = new List<DataLink>();
			foreach (DataSource LSource in FSources)
				LSource.EnumerateLinks(LTempList);
			return LTempList.ToArray();
		}
								
		#endregion

		#region Schema
		
		protected Schema.TableType InternalGetTableType()
		{
			return InternalGetTableVar().DataType as Schema.TableType;
		}
		
		protected abstract Schema.TableVar InternalGetTableVar();
		
		// TableType
		[Browsable(false)]
		public Schema.TableType TableType { get { return InternalGetTableType(); } }
		
		// TableVar
		[Browsable(false)]
		public Schema.TableVar TableVar { get { return InternalGetTableVar(); } }
		
		#endregion
		
		#region Process

		// Process
		protected abstract IServerProcess InternalGetProcess();
		
		[Browsable(false)]
		public IServerProcess Process { get { return InternalGetProcess(); } }
		
		protected void CheckProcess()
		{
			if (Process == null)
				throw new ClientException(ClientException.Codes.ProcessMissing);
		}
		
		[Browsable(false)]
		public IStreamManager StreamManager 
		{ 
			get 
			{ 
				CheckProcess();
				return (IStreamManager)Process; 
			} 
		}
		
		#endregion

		#region Open

		/// <summary> Activates the DataSet. Setting the Active property to true will call this method. </summary>
		/// <remarks> If the DataSet is already active, then this method has no effect. </remarks>
		public void Open()
		{
			if (FState == DataSetState.Inactive)
			{
				DoBeforeOpen();
				InternalConnect();
				InternalOpen();
				CreateFields();
				BufferUpdateCount(true);
				FInternalBOF = true;
				try
				{
					SetState(DataSetState.Browse);
					DoAfterOpen();
				}
				catch
				{
					CoreClose();
					throw;
				}
			}
		}

		#endregion

		#region Close
		
		private void CoreClose()
		{
			ForcedSetState(DataSetState.Inactive);
			FreeFields();
			CloseBuffer();
			InternalClose();
			InternalDisconnect();
		}

		/// <summary> Closes the DataSet, if it is open. </summary>
		/// <remarks> If the DataSet is in Insert or Edit state, a cancel is performed prior to closing. </remarks>
		public void Close()
		{
			if (FState != DataSetState.Inactive)
			{
				Cancel();
				DoBeforeClose();
				CoreClose();
				DoAfterClose();
			}
		}
		
		#endregion

		#region State

		private DataSetState FState;
		/// <summary> The current state of the DataSet. </summary>
		[Browsable(false)]
		public DataSetState State { get { return FState; } }

		/// <summary> Throws an exception if the the DataSet's state is in the given set of states. </summary>
		public void CheckState(params DataSetState[] AStates)
		{
			foreach (DataSetState LState in AStates)
				if (FState == LState)
					return;
			throw new ClientException(ClientException.Codes.IncorrectState, FState.ToString());
		}

		//Note: SetState has side affects, there is no guarantee that the state has not changed if an exception is thrown.
		private void SetState(DataSetState AState)
		{
			if (FState != AState)
			{
				FState = AState;
				FIsModified = false;
				StateChanged();
			}
		}
		
		/// <summary>Forces a set state, ignoring any exceptions that occur.</summary>
		private void ForcedSetState(DataSetState AState)
		{
			try
			{
				SetState(AState);
			}
			catch
			{
				// Ignore an exception, we are performing a forced set state
			}
		}

		protected void EnsureBrowseState(bool APostChanges)
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

		protected void EnsureBrowseState()
		{
			EnsureBrowseState(true);
		}
		
		/// <summary> Gets and sets the active state of the DataSet. </summary>
		/// <remarks> Setting this property is equivilant to calling <see cref="Open()"/> or <see cref="Close()"/> as appropriate. </remarks>
		[Category("Data")]
		[DefaultValue(false)]
		[Description("Gets and sets the active state of the DataSet")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public bool Active
		{
			get { return FState != DataSetState.Inactive; }
			set
			{
				if (FInitializing)
					FDelayedActive = value;
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

		/// <summary> Throws an exception if the DataSet is not active. </summary>
		public void CheckActive()
		{
			if (FState == DataSetState.Inactive)
				throw new ClientException(ClientException.Codes.NotActive);
		}
		
		/// <summary> Throws an exception if the DataSet is active. </summary>
		public void CheckInactive()
		{
			if (FState != DataSetState.Inactive)
				throw new ClientException(ClientException.Codes.Active);
		}

		protected void CursorSetChanged(Row ARow, bool AReprepare)
		{
			if (State != DataSetState.Inactive)
			{
				EnsureBrowseState();
				BufferClose();
				try
				{
					InternalCursorSetChanged(ARow, AReprepare);
				}
				catch
				{
					Close();
					throw;
				}
				Resume(ARow);
			}
		}
		
		#endregion

		#region ISupportInitialize

		private bool FInitializing;

		// The Active properties value during the intialization period defined in ISupportInitialize.
		private bool FDelayedActive;

		/// <summary> Called to indicate that the properties of the DataSet are being read (and therefore DataSet should not activate yet). </summary>
		public void BeginInit()
		{
			FInitializing = true;
		}

		/// <summary> Called to indicate that the properties of the DataSet have been read (and therefore the DataSet can be activated). </summary>
		public virtual void EndInit()
		{
			FInitializing = false;
			Active = FDelayedActive;
		}

		#endregion

		#region Navigation
		
		private bool FInternalBOF;

		/// <summary> True when the DataSet is on its first row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the first row. </remarks>
		[Browsable(false)]
		public bool IsFirstRow
		{
			get { return FInternalBOF || ((FActiveOffset >= 0) && ((FBuffer[FActiveOffset].RowFlag & RowFlag.BOF) != 0)); }
		}
		
		/// <summary> True when the DataSet is on its first row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the first row. </remarks>
		[Browsable(false)]
		public bool BOF
		{
			get { return FInternalBOF; }
		}

		private bool FInternalEOF;

		/// <summary> True when the DataSet is on its last row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the last row. </remarks>
		[Browsable(false)]
		public bool IsLastRow
		{
			get { return FInternalEOF || ((FActiveOffset >= 0) && ((FBuffer[FActiveOffset].RowFlag & RowFlag.EOF) != 0)); }
		}
		
		/// <summary> True when the DataSet is on its last row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the last row. </remarks>
		[Browsable(false)]
		public bool EOF
		{
			get { return FInternalEOF; }
		}

		/// <summary> Navigates the DataSet to the first row in the data set. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void First()
		{
			EnsureBrowseState();
			BufferClear();
			try
			{
				InternalFirst();
				CursorNextRows();
				if (FEndOffset >= 0)
					FBuffer[0].RowFlag |= RowFlag.BOF;
			}
			finally
			{
				FInternalBOF = true;
				DoDataChanged();
			}
		}

		/// <summary> Navigates the DataSet to the last row in the data set. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void Last()
		{
			EnsureBrowseState();
			BufferClear();
			try
			{
				InternalLast();
				CursorPriorRows();
			}
			finally
			{
				FInternalEOF = true;
				DoDataChanged();
			}
		}
		
		/// <summary> Attempts to scroll the DataSet by the specified delta. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		/// <param name="ADelta"> Number of rows, positive or negative to move relative to the current active row. </param>
		/// <returns> Number of rows actually scrolled. </returns>
		public int MoveBy(int ADelta)
		{
			int LResult = 0;
			if (ADelta != 0)
			{
				EnsureBrowseState();
				if (((ADelta > 0) && !IsLastRow) || ((ADelta < 0) && !IsFirstRow))
				{
					FInternalBOF = false;
					FInternalEOF = false;
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
									FInternalEOF = true;
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
									FInternalBOF = true;
									break;
								}
							}
						}
					}
					finally
					{
						if (LOffsetDelta != 0)
							UpdateFirstOffsets(LOffsetDelta);
						DoDataChanged();
					}
				}
			}
			return LResult;
		}

		/// <summary> Attempts to navigate the DataSet to the next row. </summary>
		/// <remarks> 
		///		Any outstanding Insert or Edit will first be posted.  If there are no more rows, 
		///		no error will occur, but the EOF property will be true. 
		///	</remarks>
		public void Next()
		{
			MoveBy(1);
		}
		
		/// <summary> Attempts to navigate the DataSet to the prior row. </summary>
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

		/// <summary> Indicates whether there is at least one row in the DataSet. </summary>
		/// <remarks> Throws an exception if the DataSet is not active. </remarks>
		[Browsable(false)]
		public bool IsEmpty()
		{
			CheckActive();
			return FEndOffset == -1;
		}
		
		/// <summary> Throws and exception if there are now rows in the DataSet. </summary>
		/// <remarks> Also throws an exception if the DataSet is not active. </remarks>
		public void CheckNotEmpty()
		{
			if (IsEmpty())
				throw new ClientException(ClientException.Codes.EmptyDataSet);
		}

		/// <summary> Refreshes the data in the DataSet from the underlying data set. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		public void Refresh()
		{
			EnsureBrowseState();
			try
			{
				if (FActiveOffset <= FEndOffset)
				{
					InternalRefresh(FBuffer[FActiveOffset].Row);
					FCurrentOffset = FActiveOffset;
				}
				else
				{
					InternalReset();
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
				InternalRefresh(ARow);
				CurrentReset();
			}
			finally
			{
				Resync(false, false);
			}
		}
		
		#endregion

		#region Modification

		protected bool FIsModified;
		
		/// <summary> Indicates whether the DataSet has been modified. </summary>
		[Browsable(false)]
		public bool IsModified
		{
			get
			{
				CheckActive();
				return FIsModified;
			}
		}

		private bool FIsReadOnly;

		protected virtual bool InternalIsReadOnly
		{
			get { return FIsReadOnly; }
			set { FIsReadOnly = value; }
		}

		/// <summary> When true, the DataSet's data cannot be modified. </summary>
		/// <remarks> The DataSet must be inactive to change this property. </remarks>
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
		
		/// <summary> Throws an exception if the DataSet cannot be edited. </summary>
		public virtual void CheckCanModify()
		{
			if (InternalIsReadOnly)
				throw new ClientException(ClientException.Codes.IsReadOnly);
		}
		
		protected bool FIsWriteOnly;

		/// <summary> When true, the underlying data for the dataset will not be retrieved. </summary>
		/// <remarks> 
		/// The DataSet must be inactive to change this property. This property is used as an optimization
		/// when a DataSet is only to be used for insert purposes. In that case, the underlying data need
		/// not be read. Attempting to Edit or Delete a row for a DataSet that is set WriteOnly will
		/// result in an error.
		/// </remarks>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Write only state")]
		public bool IsWriteOnly
		{
			get { return FIsWriteOnly; }
			set
			{
				if (FIsWriteOnly != value)
				{
					CheckState(DataSetState.Inactive);
					FIsWriteOnly = value;
				}
			}
		}
		
		/// <summary> Throws an exception if the DataSet is in WriteOnly mode and cannot be used to Edit or Delete </summary>
		public virtual void CheckCanRead()
		{
			if (FIsWriteOnly)
				throw new ClientException(ClientException.Codes.IsWriteOnly);
		}

		/// <summary> Puts the DataSet into Edit state if not already in edit/insert state. </summary>
		/// <remarks> 
		///		If there are no rows in the data set, an insert/append (same operation for an empty set) will 
		///		be performed.  If <see cref="UseApplicationTransactions"/> is True, and this DataSet is not
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
					CheckCanRead();
					DoBeforeEdit();
					InternalEdit();
					ClearOriginalRow();
					SaveOriginalRow();
					try
					{
						SetState(DataSetState.Edit);
						DoAfterEdit();
					}
					catch
					{
						Cancel();
						throw;
					}
				}
			}
		}
		
		protected void InitializeRow(int ARowIndex)
		{
			Row LTargetRow = FBuffer[ARowIndex].Row;
			LTargetRow.ClearValues();
			InternalInitializeRow(LTargetRow);
		}
		
		protected internal void ChangeColumn(DataField AField, Scalar AValue)
		{
			Edit();
			Row LActiveRow = FBuffer[FActiveOffset].Row;
			Row LSaveOldRow = FOldRow;
			FOldRow = new Row(LActiveRow.Manager, LActiveRow.DataType);
			try
			{
				LActiveRow.CopyTo(FOldRow);
				if (AValue == null)
					LActiveRow.ClearValue(AField.ColumnIndex);
				else
					LActiveRow[AField.ColumnIndex] = AValue;
					
				FValueFlags[AField.ColumnIndex] = true;
					
				InternalChangeColumn(AField, FOldRow, LActiveRow);
					
				FIsModified = true;
			}
			finally
			{
				FOldRow.Dispose();
				FOldRow = LSaveOldRow;
			}
		}

		private void EndInsertAppend()
		{
			InternalInsertAppend();
			try
			{
				SetState(DataSetState.Insert);
				InitializeRow(FActiveOffset);
				FIsModified = false;
				DoDataChanged();
				DoAfterInsert();
			}
			catch
			{
				Cancel();
				throw;
			}
		}
		
		/// <summary> Puts the DataSet into Insert state, with the proposed row inserted above the active row (or in the first position in an empty data set). </summary>
		/// <remarks> 
		///		The actual location of the row once it is posted will depend on the sort order.  The row isn't 
		///		actually posted to the underlying data set until <see cref="Post()"/> is invoked. If the DataSet
		///		is in Insert or Edit state prior to this call, a post will be attempted.
		///	</remarks>
		public void Insert()
		{
			EnsureBrowseState();
			CheckCanModify();
			DoBeforeInsert();
			SaveOriginalRow();
			DataSetRow LActiveRow = FBuffer[FActiveOffset];
			FBuffer.Move(FBuffer.Count - 1, FActiveOffset);
			if (FEndOffset == -1)
				FBuffer[FActiveOffset].RowFlag = RowFlag.Inserted | RowFlag.BOF | RowFlag.EOF;
			else
			{
				FBuffer[FActiveOffset].RowFlag = RowFlag.Inserted | RowFlag.Data | (LActiveRow.RowFlag & RowFlag.BOF);
				FBuffer[FActiveOffset].Bookmark = LActiveRow.Bookmark;
				LActiveRow.RowFlag &= ~RowFlag.BOF;
			}
			if (FCurrentOffset >= FActiveOffset)
				FCurrentOffset++;
			if (FEndOffset < (FBuffer.Count - 2))
				FEndOffset++;
			EndInsertAppend();
		}
		
		/// <summary> Puts the DataSet into Insert state, with the proposed row shown at the end of the data set. </summary>
		/// <remarks> 
		///		The actual location of the row once it is posted will depend on the sort order. The row 
		///		isn't actually posted to the underlying data set until Post() is invoked. If the DataSet
		///		is in Insert or Edit state prior to this call, a post will be attempted.
		///	</remarks>
		public void Append()
		{
			EnsureBrowseState();
			CheckCanModify();
			DoBeforeInsert();
			SaveOriginalRow();
			bool LWasEmpty = (FEndOffset < 0);
			if (LWasEmpty || ((FBuffer[FEndOffset].RowFlag & RowFlag.EOF) == 0))	// Will we have to resync to the end
			{
				// If the EOF row is not in our buffer, we will have to clear the buffer and start with a new row at the end.
				BufferClear();
				FEndOffset = 0;
				FBuffer[0].RowFlag = RowFlag.Inserted | RowFlag.EOF;

				// If there were rows before, read them again
				if (!LWasEmpty)
					CursorPriorRows();
			}
			else
			{
				// The EOF row is in our buffer, as an optimization, append our row after the EOF row
				DataSetRow LLastRow = FBuffer[FEndOffset];
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
				FBuffer[FEndOffset].RowFlag = RowFlag.Inserted | RowFlag.EOF | (LLastRow.RowFlag & RowFlag.EOF);
				LLastRow.RowFlag &= ~RowFlag.EOF;
			}
			FActiveOffset = FEndOffset;
			FInternalBOF = false;
			EndInsertAppend();
		}

		/// <summary> Cancel's the edit or insert state. </summary>
		/// <remarks> 
		///		If the DataSet is not in Insert or Edit state then this method does nothing.
		///	</remarks>
		public void Cancel()
		{
			if ((FState == DataSetState.Insert) || (FState == DataSetState.Edit))
			{
				try
				{
					if (RefreshAfterPost)
						CurrentGotoActive(true);
					DoBeforeCancel();

					// Make sure that all details have canceled
					DoPrepareToCancel();
				}
				finally
				{
					try
					{
						InternalCancel();
					}
					finally
					{
						ForcedSetState(DataSetState.Browse);
						if (RefreshAfterPost)
							Resync(false, false);
						else
						{
							// Buffer cleanup
							if ((FBuffer[FActiveOffset].RowFlag & RowFlag.Inserted) != 0)
							{
								// Move active row to scratchpad if this was an insert and not the only row
								bool LWasLastRow = (FBuffer[FActiveOffset].RowFlag & RowFlag.EOF) != 0;
								if (!LWasLastRow)
									FBuffer.Move(FActiveOffset, FBuffer.Count - 1);
								else
									FBuffer[FActiveOffset].RowFlag &= ~(RowFlag.Inserted | RowFlag.Data);

								if (FEndOffset == 0)
								{
									if (FOriginalRow != null)
										FOriginalRow.CopyTo(FBuffer[FActiveOffset].Row);
									else
										FEndOffset--;
								}
								else
								{
									FEndOffset--;
									if (LWasLastRow) // if append
										FActiveOffset--;
								}
							}
							else
							{
								FOriginalRow.CopyTo(FBuffer[FActiveOffset].Row);
							}
							ForcedDataChanged();
						}
						ClearOriginalRow();
					}
				}
				
				DoAfterCancel();
			}
		}

		/// <summary> Validates any changes to the edited or inserted row (if there is one). </summary>
		/// <remarks> If the DataSet is not in Insert or Edit state then this method does nothing. </remarks>
		public void Validate()
		{
			if ((FState == DataSetState.Insert) || (FState == DataSetState.Edit))
			{
				RequestSave();
				DoValidate();
				InternalValidate(false);
			}
		}
		
		/// <summary> Determines whether or not the dataset will attempt to refresh from the underlying cursor after a post has occurred. </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Determines whether or not the dataset will attempt to refresh from the underlying cursor after a post has occurred.")]
		public bool RefreshAfterPost { get; set; }
		
		/// <summary> Attempts to post the edited or inserted row (if there is one pending). </summary>
		/// <remarks> 
		///		If the DataSet is not in Insert or Edit state then this method does nothing.
		///	</remarks>
		public void Post()
		{
			RequestSave();
			if ((FState == DataSetState.Insert) || (FState == DataSetState.Edit))
			{
				DoValidate();
				InternalValidate(true);
				DoBeforePost();
				if (UpdatesThroughCursor() && (FState == DataSetState.Edit))
					CurrentGotoActive(true);

				// Make sure that all details have posted
				DoPrepareToPost();
				InternalPost(FBuffer[FActiveOffset].Row);
				FIsModified = false;
				SetState(DataSetState.Browse);
				if (RefreshAfterPost)
					Resync(false, false);
				else
				{
					if ((FBuffer[FActiveOffset].RowFlag & RowFlag.Inserted) != 0)
						FBuffer[FActiveOffset].RowFlag &= ~(RowFlag.Inserted | RowFlag.Data);
					DoDataChanged();
				}
				ClearOriginalRow();
				DoAfterPost();
			}
		}
		
		/// <summary> Deletes the current row. </summary>
		/// <remarks> 
		///		This action is not validated with the user.  If the DataSet is in Insert or Edit state,
		///		then a Cancel is performed.  If no row exists, an error is thrown.
		/// </remarks>
		public void Delete()
		{
			if (FState == DataSetState.Insert)
				Cancel();

			CheckActive();
			CheckCanModify();
			CheckCanRead();
			CheckNotEmpty();
			DoBeforeDelete();
			CurrentGotoActive(true);
			InternalDelete();
			SetState(DataSetState.Browse);
			Resync(false, false);
			DoAfterDelete();
		}
		
		/// <summary> Resets the values of fields to their default values for the inserted or edited row. </summary>
		public void ClearFields()
		{
			CheckState(DataSetState.Edit, DataSetState.Insert);
			InitializeRow(FActiveOffset);
			DoRowChanged(null);
		}

		#endregion

		#region Events & Notification
		
		/// <summary> Called before the DataSet is opened. </summary>
		public event EventHandler BeforeOpen;
		
		protected virtual void DoBeforeOpen()
		{
			if (BeforeOpen != null)
				BeforeOpen(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataSet is opened. </summary>
		public event EventHandler AfterOpen;

		protected virtual void DoAfterOpen()
		{
			if (AfterOpen != null)
				AfterOpen(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataSet is closed. </summary>
		public event EventHandler BeforeClose;
		
		protected virtual void DoBeforeClose()
		{
			if (BeforeClose != null)
				BeforeClose(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataSet is closed. </summary>
		public event EventHandler AfterClose;

		protected virtual void DoAfterClose()
		{
			if (AfterClose != null)
				AfterClose(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataSet enters insert mode. </summary>
		public event EventHandler BeforeInsert;
		
		protected virtual void DoBeforeInsert()
		{
			if (BeforeInsert != null)
				BeforeInsert(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataSet enters insert mode. </summary>
		public event EventHandler AfterInsert;

		protected virtual void DoAfterInsert()
		{
			if (AfterInsert != null)
				AfterInsert(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataSet enters edit mode. </summary>
		public event EventHandler BeforeEdit;
		
		protected virtual void DoBeforeEdit()
		{
			if (BeforeEdit != null)
				BeforeEdit(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataSet enters edit mode. </summary>
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
		
		/// <summary> Called before the DataSet is posted. </summary>
		public event EventHandler BeforePost;
		
		protected virtual void DoBeforePost()
		{
			if (BeforePost != null)
				BeforePost(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataSet is posted. </summary>
		public event EventHandler AfterPost;

		protected virtual void DoAfterPost()
		{
			if (AfterPost != null)
				AfterPost(this, EventArgs.Empty);
		}
		
		/// <summary> Called before the DataSet is canceled. </summary>
		public event EventHandler BeforeCancel;
		
		protected virtual void DoBeforeCancel()
		{
			if (BeforeCancel != null)
				BeforeCancel(this, EventArgs.Empty);
		}
		
		/// <summary> Called after the DataSet is canceled. </summary>
		public event EventHandler AfterCancel;

		protected virtual void DoAfterCancel()
		{
			if (AfterCancel != null)
				AfterCancel(this, EventArgs.Empty);
		}
		
		/// <summary> Called when determining the default values for a newly inserted row. </summary>
		/// <remarks> Setting column values in this handler will not set the IsModified of the DataSet. </remarks>
		public event EventHandler OnDefault;
		
		protected virtual void DoDefault()
		{
			if (OnDefault != null)
				OnDefault(this, EventArgs.Empty);
		}
		
		/// <summary> Called before posting to validate the DataSet's data. </summary>
		public event EventHandler OnValidate;

		protected virtual void DoValidate()
		{
			if (OnValidate != null)
				OnValidate(this, EventArgs.Empty);
		}
		
		/// <summary>Forces a data changed, ignoring any exceptions that occur.</summary>
		protected void ForcedDataChanged()
		{
			try
			{
				DoDataChanged();
			}
			catch
			{
				// ignore any exceptions
			}
		}
		
		public event EventHandler DataChanged;
		
		/// <summary> Occurs the active row or any other part of the buffer changes. </summary>
		protected virtual void DoDataChanged()
		{
			if (DataChanged != null)
				DataChanged(this, EventArgs.Empty);
				
			Exception LFirstException = null;
			foreach (DataLink LLink in EnumerateLinks())
			{
				try
				{
					LLink.InternalDataChanged();
				}
				catch (Exception LException)
				{
					if (LFirstException == null)
						LFirstException = LException;
				}
			}
			if (LFirstException != null)
				throw LFirstException;
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

		public event FieldChangeEventHandler RowChanging;

		/// <summary> Occurs only when the fields in the active record are changing. </summary>
		/// <param name="AField"> Valid reference to a field if one field is changing. Null otherwise. </param>
		protected virtual void DoRowChanging(DataField AField)
		{
			if (RowChanging != null)
				RowChanging(this, AField);
			foreach (DataLink LLink in EnumerateLinks())
				LLink.RowChanging(AField);
		}

		public event FieldChangeEventHandler RowChanged;
		
		/// <summary> Occurs only when the fields in the active record has changed. </summary>
		/// <param name="AField"> Valid reference to a field if one field has changed. Null otherwise. </param>
		protected virtual void DoRowChanged(DataField AField)
		{
			if (RowChanged != null)
				RowChanged(this, AField);
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

		/// <summary> Occurs in reponse to any change in the state of the DataSet. </summary>
		protected virtual void StateChanged()
		{
			Exception LFirstException = null;
			foreach (DataLink LLink in EnumerateLinks())
			{
				try
				{
					LLink.StateChanged();
				}
				catch (Exception LException)
				{
					if (LFirstException == null)
						LFirstException = LException;
				}
			}
			if (LFirstException != null)
				throw LFirstException;
		}
		
		#endregion

		#region Fields
		
		private DataFields FDataFields;
		/// <summary> Collection of DataField objects representing the columns of the active row. </summary>
		[Browsable(false)]
		public DataFields Fields { get { return FDataFields; } }
		
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

		/// <summary> The number of DataFields in the DataSet. </summary>
		[Browsable(false)]
		public int FieldCount
		{
			get { return FFields.Count; }
		}

		private void CreateFields()
		{
			int LIndex = 0;
			DataField LField;
			foreach (Schema.Column LColumn in InternalGetTableType().Columns)
			{
				LField = new DataField(this, LColumn);
				FFields.Add(LField);
				LIndex++;
			}

			FValueFlags = new BitArray(FFields.Count);
			FValueFlags.SetAll(false);
		}
		
		private void FreeFields()
		{
			if (FFields != null)
				FFields.Clear();
				
			FValueFlags = null;
		}
		
		#endregion

		#region Bookmarks
#if DATASETBOOKMARKS
		
		/// <summary> Determines if a bookmark can be retrieved for the active row. </summary>
		public bool BookmarkAvailable()
		{
			return (FState != DataSetState.Inactive) &&
				(FActiveOffset <= FEndOffset) && 
				((FBuffer[FActiveOffset].RowFlag & RowFlag.Data) != 0);
		}
		
		/// <summary> Retrieves a bookmark to the active row. </summary>
		/// <remarks> 
		///		Note that some changes to the DataSet will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated.  
		///	</remarks>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Guid GetBookmark()
		{
			CheckActive();
			CheckNotEmpty();
			return InternalGetBookmark();
		}

		/// <summary> Returns to a previously bookmarked position. </summary>
		/// <remarks> 
		///		Note that some changes to the DataSet will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated. 
		///	</remarks>
		public void GotoBookmark(Guid ABookmark)
		{
			CheckActive();
			try
			{
				InternalGotoBookmark(ABookmark);
			}
			finally
			{
				Resync(true, true);
			}
		}

		/// <summary> Compare the relative position of two bookmarked rows. </summary>
		/// <remarks> 
		///		Note that some changes to the DataSet will cause the underlying cursor to be replaced; in 
		///		such a case, previously retrieved bookmarks will be freed and invalidated. 
		///	</remarks>
		/// <returns> -1 if ABookmark1 is less than ABookmark 2.  0 if they are equal. 1 otherwise. </returns>
		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			CheckActive();
			return InternalCompareBookmarks(ABookmark1, ABookmark2);
		}

		/// <summary> Deallocates the resources used to maintain the specified bookmark. </summary>
		/// <remarks> 
		///		All bookmarks retrieved through GetBookmark() should be freed.  Note that some changes to the DataSet 
		///		will cause the underlying cursor to be replaced; in such a case, previously retrieved bookmarks will be 
		///		freed and invalidated. 
		///	</remarks>
		public void FreeBookmark(Guid ABookmark)
		{
			CheckActive();
			InternalDisposeBookmark(ABookmark);
		}

		/// <summary> Determines is a given bookmark is still valid. </summary>
		/// <remarks> 
		///		Note that some changes to the DataSet will cause the underlying cursor to be replaced; in 
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
				InternalCompareBookmarks(ABookmark, ABookmark);
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
					LResult.Columns.Add(new Schema.TableVarColumn(new Schema.Column(LTrimmed, null)));
			}
			return LResult;
		}

		public static string GetNamesFromOrder(Schema.Order AOrder)
		{
			StringBuilder LResult = new StringBuilder();
			if (AOrder != null)
				foreach (Schema.OrderColumn LColumn in AOrder.Columns)
				{
					if (LResult.Length > 0)
						LResult.Append(CColumnNameDelimiters[0]);
					LResult.Append(LColumn.Column.Name);
					if (!LColumn.Ascending)
						LResult.AppendFormat(" {0}", Keywords.Asc);
					if (LColumn.IncludeNils)
						LResult.AppendFormat(" {0} {1}", Keywords.Include, Keywords.Nil);
				}
			return LResult.ToString();
		}

		public static Schema.Order GetOrderFromNames(string AOrderNames)
		{
			Schema.Order LResult = new Schema.Order();
			string LTrimmed;
			foreach (string LItem in AOrderNames.Split(CColumnNameDelimiters))
			{
				LTrimmed = LItem.Trim();
				if (LTrimmed != String.Empty)
				{
					string[] LItems = LItem.Split(' ');
					Schema.OrderColumn LOrderColumn = new Schema.OrderColumn(new Schema.TableVarColumn(new Schema.Column(LItems[0], null)), true, false);
					
					if (LItems.Length > 1)
					{
						switch (LItems[1])
						{
							case Keywords.Desc : LOrderColumn.Ascending = false; break;
							case Keywords.Include : LOrderColumn.IncludeNils = true; break;
						}
					}
					
					if ((LItems.Length > 2) && (LItems[2] == Keywords.Include))
						LOrderColumn.IncludeNils = true;
					
					LResult.Columns.Add(LOrderColumn);
				}
			}
			return LResult;
		}

		/// <summary> Gets a row representing a key for the active row. </summary>
		/// <returns> A row containing key information.  It is the callers responsability to Dispose this row when it is no longer required. </returns>
		public Row GetKey()
		{
			EnsureBrowseState();
			CheckNotEmpty();
			CurrentGotoActive(true);
			return InternalGetKey();
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
				return InternalFindKey(AKey);
			}
			finally
			{
				Resync(true, true);
			}
		}
		
		/// <summary> Navigates the DataSet to the row nearest the specified key or partial key. </summary>
		/// <param name="AKey"> A full or partial row containing search criteria for the current order. </param>
		public void FindNearest(Row AKey)
		{
			EnsureBrowseState();
			try
			{
				InternalFindNearest(AKey);
			}
			finally
			{
				Resync(false, true);
			}
		}

		#endregion

		#region Enumeration

		public IEnumerator GetEnumerator()
		{
			return new DataSetEnumerator(this);
		}

		internal class DataSetEnumerator : IEnumerator
		{
			public DataSetEnumerator(DataSet ADataSet)
			{
				FDataSet = ADataSet;
			}

			private DataSet FDataSet;
			private bool FInitial = true;

			public void Reset()
			{
				FDataSet.First();
				FInitial = true;
			}

			public object Current
			{
				get { return FDataSet.ActiveRow; }
			}

			public bool MoveNext()
			{
				bool LResult = !FDataSet.IsEmpty() && (FInitial || (FDataSet.MoveBy(1) == 1));
				FInitial = false;
				return LResult;
			}
		}

		#endregion

		#region class DataSetRow

		internal class DataSetRow : Disposable
		{
			public DataSetRow() : base(){}
			public DataSetRow(Row ARow, RowFlag ARowFlag, Guid ABookmark)
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

		#region class DataSetBuffer

		#if USETYPEDLIST
		internal class DataSetBuffer : DisposableTypedList
		{
			public DataSetBuffer() : base(typeof(DataSetRow), true, false){}

			public new DataSetRow this[int AIndex]
			{
				get { return (DataSetRow)base[AIndex]; }
				set { base[AIndex] = value; }
			}

		#else
		internal class DataSetBuffer : DisposableList<DataSetRow>
		{
		#endif
			public void Add(int ACount, DataSet ADataSet)
			{
				for (int LIndex = 0; LIndex < ACount; LIndex++)
					Add(new DataSetRow(new Row(ADataSet.Process.ValueManager, ADataSet.TableType.RowType), new RowFlag(), Guid.Empty));
			}
		}
		
		#endregion
	}

	public class DataSetException : Exception
	{
		public DataSetException(string AMessage, Exception AInner) : base(AMessage, AInner) {}
		public DataSetException(string AMessage) : base(AMessage) {}
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

	/// <summary> Identifies the state of the DataSet. </summary>
	public enum DataSetState
	{
		Inactive,	// Not active
		Insert,		// Inserting a new row
		Edit,		// Editing active row
		Browse		// Active, valid and not in insert or edit
	};
	
	/// <summary> Used by the DataSet's buffer management. </summary>
	internal enum RowFlag 
	{
		Data = 1, 
		BOF = 2, 
		EOF = 4, 
		Inserted = 8
	};
	
	public delegate void FieldChangeEventHandler(object ASender, DataField AField);
	
    public class DataFields : ICollection
    {
		internal DataFields(DataSet ADataSet)
		{
			FDataSet = ADataSet;
		}
		
		DataSet FDataSet;
		
		public int Count
		{
			get { return FDataSet.FieldCount; }
		}
		
		public IEnumerator GetEnumerator()
		{
			return new DataFieldEnumerator(FDataSet);
		}
		
		public DataField this[int AIndex]
		{
			get { return FDataSet[AIndex]; }
		}
		
		public DataField this[string AColumnName]
		{
			get { return FDataSet[AColumnName]; }
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
			get { return FDataSet.FFields; }
		}

		public class DataFieldEnumerator : IEnumerator
		{
			internal DataFieldEnumerator(DataSet ADataSet)
			{
				FDataSet = ADataSet;
			}
			
			private DataSet FDataSet;
			private int FIndex = -1;
			
			public void Reset()
			{
				FIndex = -1;
			}
			
			public bool MoveNext()
			{
				FIndex++;
				return (FIndex < FDataSet.FieldCount);
			}
			
			public object Current
			{
				get { return FDataSet[FIndex]; }
			}
		}
    }
    
    public class DataField : Schema.Object, IConvertible
    {
		public DataField(DataSet ADataSet, Schema.Column AColumn) : base(AColumn.Name)
		{
			FDataSet = ADataSet;
			FColumn = AColumn;
			FColumnIndex = ADataSet.TableType.Columns.IndexOfName(AColumn.Name);
		}

		private DataSet FDataSet;
		/// <summary> The DataSet this field is contained in. </summary>
		public DataSet DataSet
		{
			get { return FDataSet; }
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
			FDataSet.FocusControl(this);
		}

		private void CheckHasValue()
		{
			if (!HasValue())
				throw new ClientException(ClientException.Codes.NoValue, ColumnName);
		}

		private void CheckDataSetNotEmpty()
		{
			if (FDataSet.IsEmpty())
				throw new ClientException(ClientException.Codes.EmptyDataSet);
		}
		
		// Value Access
		/// <summary>Gets or sets the current value of this field as a Scalar.</summary>
		/// <remarks>Setting this value will cause field and dataset level validation and change events to be invoked.
		/// The validate event is invoked first, followed by the change event. If an exception is thrown during the 
		/// validation event, the field will not be set to the new value. However, if an exception is thrown during
		/// the change event, the field will still be set to the new value.</remarks>
		public Scalar Value
		{
			get
			{
				CheckHasValue();
				return (Scalar)FDataSet.ActiveRow.GetValue(FColumnIndex);
			}
			set
			{
				FDataSet.ChangeColumn(this, value);
			}
		}
		
		/// <summary>Indicates whether the value for this column has been modified.</summary>
		public bool Modified { get { return FDataSet.FValueFlags == null ? false : FDataSet.FValueFlags[FColumnIndex]; } }

		/// <summary>Retrieves the old value for this field during a change or validate event.</summary>
		/// <remarks>This value is only available during a change or validate event. A ClientException exception will be
		/// thrown if this property is accessed at any other time.</remarks>
		public Scalar OldValue
		{
			get
			{
				return (Scalar)FDataSet.OldRow.GetValue(FColumnIndex);
			}
		}

		/// <summary>Retrieves the original value for this field prior to any changes made during the edit.</summary>
		/// <remarks>This value is only available when the dataset is in edit state. A ClientException exception will be
		/// thrown if this property is accessed at any other time.</remarks>		
		public Scalar OriginalValue
		{
			get
			{
				return (Scalar)FDataSet.OriginalRow.GetValue(FColumnIndex);
			}
		}
		
		/// <summary> True when the field has a value. </summary>
		public bool HasValue()
		{
			CheckDataSetNotEmpty();
			return FDataSet.ActiveRow.HasValue(FColumnIndex);
		}

		public bool IsNil
		{
			get
			{
				CheckDataSetNotEmpty();
				return !FDataSet.ActiveRow.HasValue(FColumnIndex);
			}
		}
		
		public void ClearValue()
		{
			Value = null;
		}
		
		public object AsNative
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsNative;
				return null;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsNative = value; // Has to be done thiw way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}

		public bool AsBoolean
		{
			get 
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsBoolean; 
				return false;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsBoolean = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public bool GetAsBoolean(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsBoolean(ARepresentationName);
			return false;
		}
		
		public void SetAsBoolean(string ARepresentationName, bool AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsBoolean(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public byte AsByte
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsByte;
				return (byte)0;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsByte = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public byte GetAsByte(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsByte(ARepresentationName);
			return (byte)0;
		}
		
		public void SetAsByte(string ARepresentationName, byte AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsByte(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public decimal AsDecimal
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsDecimal;
				return 0m;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsDecimal = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public decimal GetAsDecimal(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsDecimal(ARepresentationName);
			return 0m;
		}
		
		public void SetAsDecimal(string ARepresentationName, decimal AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsDecimal(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public TimeSpan AsTimeSpan
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsTimeSpan;
				return TimeSpan.Zero;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsTimeSpan = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public TimeSpan GetAsTimeSpan(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsTimeSpan(ARepresentationName);
			return TimeSpan.Zero;
		}
		
		public void SetAsTimeSpan(string ARepresentationName, TimeSpan AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsTimeSpan(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public DateTime AsDateTime
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsDateTime;
				return DateTime.MinValue;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsDateTime = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}

		public DateTime GetAsDateTime(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsDateTime(ARepresentationName);
			return DateTime.MinValue;
		}
		
		public void SetAsDateTime(string ARepresentationName, DateTime AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsDateTime(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
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
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsInt16;
				return (short)0;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsInt16 = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public short GetAsInt16(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsInt16(ARepresentationName);
			return (short)0;
		}
		
		public void SetAsInt16(string ARepresentationName, short AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsInt16(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public int AsInt32
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsInt32;
				return 0;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsInt32 = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public int GetAsInt32(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsInt32(ARepresentationName);
			return 0;
		}
		
		public void SetAsInt32(string ARepresentationName, int AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsInt32(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public long AsInt64
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsInt64;
				return (long)0;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsInt64 = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public long GetAsInt64(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsInt64(ARepresentationName);
			return (long)0;
		}
		
		public void SetAsInt64(string ARepresentationName, long AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsInt64(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public string AsString
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsString;
				return String.Empty;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsString = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public string GetAsString(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsString(ARepresentationName);
			return String.Empty;
		}
		
		public void SetAsString(string ARepresentationName, string AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsString(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
			Value = LValue;
		}
		
		public string AsDisplayString
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsDisplayString;
				return String.Empty;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsDisplayString = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public Guid AsGuid
		{
			get
			{
				CheckDataSetNotEmpty();
				if (HasValue())
					return Value.AsGuid;
				return Guid.Empty;
			}
			set
			{
				CheckDataSetNotEmpty();
				Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
				LValue.AsGuid = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = LValue;
			}
		}
		
		public Guid GetAsGuid(string ARepresentationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsGuid(ARepresentationName);
			return Guid.Empty;
		}
		
		public void SetAsGuid(string ARepresentationName, Guid AValue)
		{
			CheckDataSetNotEmpty();
			Scalar LValue = new Scalar(FDataSet.Process.ValueManager, DataType, null);
			LValue.SetAsGuid(ARepresentationName, AValue); // Has to be done this way in order to fire the appropriate dataset events
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
			throw new DataSetException("Internal Error: DataField.IConvertible.GetTypeCode()");
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

	public delegate void ErrorsOccurredHandler(DataSet ADataSet, CompilerMessages AMessages);    
    public delegate void DataLinkHandler (DataLink ALink, DataSet ADataSet);
    public delegate void DataLinkFieldHandler (DataLink ALInk, DataSet ADataSet, DataField AField);
    public delegate void DataLinkScrolledHandler (DataLink ALink, DataSet ADataSet, int ADelta);
	
	/// <summary> An internal link to a DataSet component. </summary>
	/// <remarks> 
	///		This link is to be used by controls and other possible consumers of data from 
	///		a DataSet.  The link provides notification of various state changes and also 
	///		requests to update the DataSet with any changes.  Through the BufferCount, 
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
						FSource.OnDataSetChanged -= new EventHandler(DataSourceDataSetChanged);
					}
					FSource = value;
					if (FSource != null)
					{
						FSource.OnDataSetChanged += new EventHandler(DataSourceDataSetChanged);
						FSource.Disposed += new EventHandler(DataSourceDisposed);
						FSource.AddLink(this);
					}

					DataSetChanged();
				}
			}
		}
		
		private void DataSourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}
		
		private void DataSourceDataSetChanged(object ASender, EventArgs AArgs)
		{
			DataSetChanged();
		}

		private void DataSetChanged()
		{
			// Set FActive to opposite of Active to ensure that ActiveChanged is fired (so that the control always get's an ActiveChanged on way or another)
			FActive = !Active;
			StateChanged();
		}
		
		/// <summary> Gets the DataSet associated with this link, or null if none. </summary>
		public DataSet DataSet
		{
			get
			{
				if (FSource == null)
					return null;
				else
					return FSource.DataSet;
			}
		}
		
		/// <summary> Indicates whether the link is active (connected to an active <see cref="DataSet"/>). </summary>
		public bool Active
		{
			get { return (DataSet != null) && DataSet.Active; }
		}

		internal int FFirstOffset;	// This is maintained by the DataSet
		
		/// <summary> Gets the active row offset relative to the link. </summary>
		/// <remarks> Do not attempt to retrieve this if the link is not <see cref="Active"/>. </remarks>
		public int ActiveOffset
		{
			get { return DataSet.FActiveOffset - FFirstOffset; }
		}

		/// <summary> Retrieves a DataSet buffer row relative to this link. </summary>
		/// <remarks> 
		///		Do not attempt to access this if the link is not <see cref="Active"/>.  Do not modify 
		///		the buffer rows in any way, use them only to read data. 
		///	</remarks>
		public DAE.Runtime.Data.Row Buffer(int AIndex)
		{
			return DataSet.FBuffer[AIndex + FFirstOffset].Row;
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
				if (LResult > DataSet.EndOffset)
					LResult = DataSet.EndOffset;
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
						FSource.LinkBufferRangeChanged();	// Make any adjustment to the DataSet buffer size
				}
			}
		}

		/// <summary> Requests that the link post any data changes that are pending. </summary>
		public event DataLinkHandler OnSaveRequested;
		/// <summary> Requests that the link post any data changes that are pending. </summary>
		public virtual void SaveRequested()
		{
			if (OnSaveRequested != null)
				OnSaveRequested(this, DataSet);
		}
		
		/// <summary> Called right before posting; for example, to ensure that details are posted before posting of the master occurs. </summary>
		public event DataLinkHandler OnPrepareToPost;
		/// <summary> Called right before posting; for example, to ensure that details are posted before posting of the master occurs. </summary>
		protected internal virtual void PrepareToPost()
		{
			if (OnPrepareToPost != null)
				OnPrepareToPost(this, DataSet);
		}

		/// <summary> Called right before canceling; for example, to ensure that details are canceled before canceling of the master occurs. </summary>
		public event DataLinkHandler OnPrepareToCancel;
		/// <summary> Called right before canceling; for example, to ensure that details are canceled before canceling of the master occurs. </summary>
		protected internal virtual void PrepareToCancel()
		{
			if (OnPrepareToCancel != null)
				OnPrepareToCancel(this, DataSet);
		}
		
		/// <summary> Called when the active row's data is changing as a result of data edits. </summary>
		public event DataLinkFieldHandler OnRowChanging;
		/// <summary> Called when the active row's data is changing as a result of data edits. </summary>
		protected internal virtual void RowChanging(DataField AField)
		{
			if (OnRowChanging != null)
				OnRowChanging(this, DataSet, AField);
		}

		/// <summary> Called when the active row's data changed as a result of data edits. </summary>
		public event DataLinkFieldHandler OnRowChanged;
		/// <summary> Called when the active row's data changed as a result of data edits. </summary>
		protected internal virtual void RowChanged(DataField AField)
		{
			if (OnRowChanged != null)
				OnRowChanged(this, DataSet, AField);
		}

		/// <summary> Ensures that the link's buffer range encompasses the active row. </summary>
		/// <remarks> Assumes that the link is active. </remarks>
		protected internal virtual void UpdateRange()
		{
			FFirstOffset += Math.Min(DataSet.FActiveOffset - FFirstOffset, 0);							// Slide buffer down if active is below first
			FFirstOffset += Math.Max(DataSet.FActiveOffset - (FFirstOffset + (FBufferCount - 1)), 0);	// Slide buffer up if active is above last
		}

		/// <summary> Scrolls the links buffer by the given delta, then ensures that the link buffer is kept in range of the active row. </summary>
		/// <remarks> 
		///		A delta is passed when the DataSet's buffer size is adjusted (to keep the link as close as 
		///		possible to its prior relative position). For example, if the DataSet discards the first couple 
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
				OnDataChanged(this, DataSet);
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
				OnFocusControl(this, DataSet, AField);
		}
		
		/// <summary> Called when the DataSet's state has changed. </summary>
		public event DataLinkHandler OnStateChanged;
		/// <summary> Called when the DataSet's state has changed. </summary>
		protected internal virtual void StateChanged()
		{
			if (Active != FActive)
			{
				ActiveChanged();
				FActive = Active;
			}

			if (OnStateChanged != null)
				OnStateChanged(this, DataSet);
		}
		
		private bool FActive;
		/// <summary> Called when the DataSet's active state changes or when attached/detached from an active DataSet. </summary>
		public event DataLinkHandler OnActiveChanged;
		/// <summary> Called when the DataSet's active state changes or when attached/detached from an active DataSet. </summary>
		protected internal virtual void ActiveChanged()
		{
			if (Active)
				UpdateRange();
			else
				FFirstOffset = 0;
				
			if (OnActiveChanged != null)
				OnActiveChanged(this, DataSet);
		}
    }

	/// <summary> A level of indirection between data controls and the DataSet. </summary>
	/// <remarks> Data aware controls are attached to a DataSet by linking them to a DataSource which is connected to a DataSet. </remarks>
	[ToolboxItem(true)]
	[DefaultEvent("OnDataSetChanged")]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DataSource),"Icons.DataSource.bmp")]
    public class DataSource : Component, IDisposableNotify
    {
		public DataSource() : base()
		{
		}

		public DataSource(IContainer AContainer) : this()
		{
			if (AContainer != null)
				AContainer.Add(this);
		}

		protected override void Dispose(bool ADisposing)
		{
			DataSet = null;
			base.Dispose(ADisposing);
			if (FLinks != null)
			{
				while (FLinks.Count > 0)
					FLinks[FLinks.Count - 1].Source = null;
				FLinks = null;
			}
		}

		internal List<DataLink> FLinks = new List<DataLink>();
		
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
			if ((FDataSet != null) && (FDataSet.Active))
				FDataSet.BufferUpdateCount(false);
		}
		
		public void EnumerateLinks(List<DataLink> ALinks)
		{
			foreach (DataLink LLink in FLinks)
				ALinks.Add(LLink);
		}
		
		private DataSet FDataSet;
		/// <summary> The <see cref="DataSet"/> that is associated with this DataSource. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		public DataSet DataSet
		{
			get { return FDataSet; }
			set
			{
				if (FDataSet != value)
				{
					if (FDataSet != null)
					{
						FDataSet.RemoveSource(this);
						FDataSet.Disposed -= new EventHandler(DataSetDisposed);
					}
					FDataSet = value;
					if (FDataSet != null)
					{
						FDataSet.AddSource(this);
						FDataSet.Disposed += new EventHandler(DataSetDisposed);
					}
					DataSetChanged();
				}
			}
		}
		
		private void DataSetDisposed(object ASender, EventArgs AArgs)
		{
			DataSet = null;
		}
		
		public event EventHandler OnDataSetChanged;
		
		protected virtual void DataSetChanged()
		{
			if (OnDataSetChanged != null)
				OnDataSetChanged(this, EventArgs.Empty);
		}
	}
}

