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
		public readonly static Char[] ColumnNameDelimiters = new Char[] {',',';'};

		public DataSet() : base()
		{
			_buffer = new DataSetBuffer();
			BufferClear();
			_fields = new Schema.Objects(10);
			_dataFields = new DataFields(this);
			RefreshAfterPost = true;
			_isWriteOnly = false;
		}

		public DataSet(IContainer container) : this()
		{
			if (container != null)
				container.Add(this);
		}

		protected virtual void InternalDispose(bool disposing)
		{
			Close();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)	// If not being properly disposed, don't bother, just causes errors
			{
				_disposing = true;
				try
				{
					try
					{
						InternalDispose(disposing);
					}
					finally
					{
						base.Dispose(disposing);

						if (_fields != null)
						{
							_fields.Clear();
							_fields = null;
						}

						if (_sources != null)
						{
							while(_sources.Count != 0)
								_sources[_sources.Count - 1].DataSet = null;
							_sources = null;
						}

						ClearOriginalRow();

						if (_buffer != null)
						{
							_buffer.Dispose();
							_buffer = null; 
						}
					}
				}
				finally
				{
					_disposing = false;
				}
			}
		}
		
		protected bool _disposing;
		
		#region Buffer Maintenance

		// <summary> Row buffer.  Always contains at least two Rows. </summary>
		internal DataSetBuffer _buffer;

		// <summary> The number of "filled" rows in the buffer. </summary>
		private int _endOffset;

		/// <summary> Offset of current Row from origin of buffer. </summary>
		private int _currentOffset;

		/// <summary> Offset of active Row from origin of buffer. </summary>
		internal int _activeOffset;

		/// <summary> The actively selected row of the DataSet's buffer. </summary>
		[Browsable(false)]
		public IRow ActiveRow { get { return _buffer[_activeOffset].Row; } }
		
		/// <summary> Tracks whether the values for each column in the active row have been modified. </summary>
		protected internal BitArray _valueFlags;
		
		protected IRow _originalRow;
		/// <summary> Tracks the original row during an edit. </summary>
		/// <remarks> The original values for a row can only be accessed when the dataset is in edit state. </remarks>
		protected internal IRow OriginalRow
		{
			get
			{
				if (_state == DataSetState.Edit)
					throw new ClientException(ClientException.Codes.OriginalValueNotAvailable);
				return _originalRow;
			}
		}

		protected IRow _oldRow;
		/// <summary> The old row for the current row changed event. </summary>
		/// <remarks> This property can only be accessed during change events for the field and dataset. </remarks>
		protected internal IRow OldRow
		{
			get
			{
				if (_oldRow == null)
					throw new ClientException(ClientException.Codes.OldValueNotAvailable);
				return _oldRow;
			}
		}

		/// <summary> Gets the maximum number of rows that are buffered for active controls on this DataSet. </summary>
		/// <remarks> This may be useful for determining MoveBy deltas when the desire is to move by "pages". </remarks>
		[Browsable(false)]
		public int BufferCount
		{
			get { return _buffer.Count - 1; }
		}

		internal int EndOffset
		{
			get { return _endOffset; }
		}

		protected IRow RememberActive()
		{
			if (_activeOffset <= _endOffset)
				return (IRow)_buffer[_activeOffset].Row.Copy();
			else
				return null;
		}

		private void SaveOriginalRow()
		{
			IRow row = RememberActive();
			if (row != null)
				_originalRow = row;
			else
				ClearOriginalRow();
				
			if (_valueFlags != null)
				_valueFlags.SetAll(false);
		}
		
		private void ClearOriginalRow()
		{
			if (_originalRow != null)
			{
				_originalRow.Dispose();
				_originalRow = null;
			}
			
			if (_valueFlags != null)
				_valueFlags.SetAll(false);
		}
		
		/// <summary> Used when the cursor no longer is in sync with the current row offset value. </summary>
		private void CurrentReset()
		{
			_currentOffset = -1;
		}
		
		/// <summary> Sets the underlying cursor to the active row. </summary>
		/// <param name="strict"> If true, will throw and exception if unable to position on the buffered row. </param>
		private void CurrentGotoActive(bool strict)
		{
			if (_endOffset > -1)
				CurrentGoto(_activeOffset, strict, true);
		}
		
		/// <summary> Sets the underlying cursor to a specific buffer row. </summary>
		/// <param name="strict"> If true, will throw an exception if unable to position on the buffered row. </param>
		/// <param name="forward"> Hint about the intended direction of movement after cursor positioning. </param>
		private void CurrentGoto(int value, bool strict, bool forward)
		{
			if (_currentOffset != value)
			{
				RowFlag flag = _buffer[value].RowFlag;
				
				// If exception is thrown, current should still be valid (cursor should not move if unable to comply)
				bool result;
				if ((flag & RowFlag.Data) != 0)
					result = InternalGotoBookmark(_buffer[value].Bookmark, forward);
				else if ((flag & RowFlag.Inserted) != 0)
				{
					result = true;
					if ((flag & RowFlag.BOF) != 0)
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
					result = InternalFindKey(_buffer[value].Row);
				}

				if (strict && !result)
					throw new ClientException(ClientException.Codes.BookmarkNotFound);

				_currentOffset = value;
			}
		}

		private void FinalizeBuffers(int LFirst, int LLast)
		{
			List<Guid> bookmarks = new List<Guid>(LLast - LFirst + 1);
			for (int i = LFirst; i <= LLast; i++)
			{
				var bookmark = _buffer[i].Bookmark;
				if (bookmark != Guid.Empty)
					bookmarks.Add(bookmark);
				_buffer[i].Bookmark = Guid.Empty;
				_buffer[i].Row.ClearValues();
			}
			InternalDisposeBookmarks(bookmarks.ToArray());
		}

		private void FinalizeBuffer(DataSetRow row)
		{
			if (row.Bookmark != Guid.Empty)
			{
				InternalDisposeBookmark(row.Bookmark);
				row.Bookmark = Guid.Empty;
			}
			row.Row.ClearValues();
		}

		/// <summary> Selects values from cursor into specified Row of buffer. </summary>
		private void CursorSelect(int index)
		{
			DataSetRow row = _buffer[index];
			FinalizeBuffer(row);
			InternalSelect(row.Row);
			row.Bookmark = InternalGetBookmark();
			row.RowFlag = RowFlag.Data;
		}

		private void BufferClear()
		{
			_endOffset = -1;
			_activeOffset = 0;
			_currentOffset = -1;
			_internalBOF = true;
			_internalEOF = true;
		}
		
		private void BufferActivate()
		{
			_endOffset = 0;
			_activeOffset = 0;
			_currentOffset = 0;
			_internalBOF = false;
			_internalEOF = false;
		}
		
		// Closes the buffer, finalizing the rows it contains, but leaves the buffer space intact. Used when closing the underlying cursor.
		private void BufferClose()
		{
			BufferClear();
			FinalizeBuffers(0, _buffer.Count - 1);
		}

		// Closes the buffer, finalizing the rows it contains, and clearing the buffer space. Only used when closing the dataset.
		private void CloseBuffer()
		{
			BufferClose();
			_buffer.Clear();
		}

		/// <summary> Reads a row into the end of the buffer, scrolling if necessary. </summary>
		/// <remarks> This call will adjust FCurrentOffset, FActiveOffset, and FEndOffset appropriately. </remarks>
		private bool CursorNextRow()
		{
			bool getNext = true;

			// Navigate to the last occupied row in the buffer
			if (_endOffset >= 0)
			{
				if ((_buffer[_endOffset].RowFlag & RowFlag.EOF) != 0)
					return false;

				CurrentGoto(_endOffset, false, true);
			
				// Skip the move next if the row in question is being inserted (it's cursor info is a duplicate of the preceeding row)
				if ((_state == DataSetState.Insert) && (_currentOffset == _activeOffset))
					getNext = false;
			}

			// Attempt to navigate to the next row
			if (getNext && !InternalNext())
			{
				CurrentReset();
				if (_endOffset >= 0)
					_buffer[_endOffset].RowFlag |= RowFlag.EOF;
				return false;
			}

			if (_endOffset == -1)
			{
				// Initially active the buffer
				CursorSelect(0);
				BufferActivate();
			}
			else
			{
				if (_endOffset < (_buffer.Count - 2))
				{
					// Add an additional row to the buffer
					_endOffset++;
					CursorSelect(_endOffset);
				}
				else
				{
					// Scroll the buffer, there are no more available rows
					CursorSelect(_endOffset + 1);
					_buffer.Move(0, _endOffset + 1);
					_activeOffset--;		// Note that this could potentially place the active offset outside of the buffer boundary (the caller should account for this)
				}
			}
			_currentOffset = _endOffset;
			return true;
		}
		
		/// <summary> Reads rows onto the end of the buffer until the buffer is full. </summary>
		private int CursorNextRows()
		{
			int result = 0;
			while ((_endOffset < (_buffer.Count - 2)) && CursorNextRow())
				result++;
			return result;
		}

		/// <summary> Reads a row into the beginning of the buffer scrolling others down. </summary>
		/// <remarks> This call will adjust FCurrentOffset, FActiveOffset, and FEndOffset appropriately. </remarks>
		private bool CursorPriorRow()
		{
			// Navigate to the first row in the buffer (if we have one)
			if (_endOffset > -1)
			{
				if ((_buffer[0].RowFlag & RowFlag.BOF) != 0)
					return false;

				CurrentGoto(0, false, false);
			}
		
			// Attempt to navigate to the prior row
			if (!InternalPrior())
			{
				CurrentReset();
				if (_endOffset > -1)
					_buffer[0].RowFlag |= RowFlag.BOF;
				return false;
			}
		
			// Select a row into the scratchpad row
			CursorSelect(_endOffset + 1);
		
			if (_endOffset == -1)
				BufferActivate();	// Initially activate the buffer
			else
			{
				// Move the scratchpad row into the first slot of the buffer and updated the other offsets
				_buffer.Move(_endOffset + 1, 0);
				if (_endOffset < (_buffer.Count - 2))
					_endOffset++;
				_activeOffset++;	// Note that this could potentially place the active offset outside of the buffer boundary (the caller should account for this)
			}

			_currentOffset = 0;		
			
			return true;
		}
		
		/// <summary> Reads records onto the beginning of the buffer until it is full. </summary>
		private int CursorPriorRows()
		{
			int result = 0;
			while ((_endOffset < (_buffer.Count - 2)) && CursorPriorRow())
				result++;
			return result;
		}
		
		/// <summary> Resyncronizes the row buffer from the cursor. </summary>
		private void Resync(bool exact, bool center)
		{
			if (exact)
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
			int offset;
			if (center)
				offset = (_buffer.Count - 2) / 2;
			else
				offset = _activeOffset;
			BufferActivate();

			try
			{
				while ((offset > 0) && CursorPriorRow())
					offset--;
				CursorNextRows();
				CursorPriorRows();
			}
			finally
			{
				ForcedDataChanged();
			}
		}

		/// <summary> Reactivates the buffer, optionally from a row. </summary>
		/// <param name="row"> An optional row indicating a row to sync to. </param>
		private void Resume(IRow row)
		{
			if (row != null)
				FindNearest(row);
			else
			{
				BufferClear();
				try
				{
					CursorNextRows();
					if (_endOffset >= 0)
						_buffer[0].RowFlag |= RowFlag.BOF;
				}
				finally
				{
					_internalBOF = true;
					ForcedDataChanged();
				}
			}
		}

		/// <summary> Updates the number and values of rows in the buffer. </summary>
		/// <param name="first"> When true, no attempt is made to fill the datasource "backwards". </param>
		internal void BufferUpdateCount(bool first)
		{
			int i;
			DataLink[] links = EnumerateLinks();

			// Determine the range of buffer utilization from the links
			int min = _activeOffset;
			int max = _activeOffset;
			foreach (DataLink link in links)
			{
				link.UpdateRange();	// Make sure the link's virtual buffer range encompasses the active row
				min = Math.Min(min, link._firstOffset);
				max = Math.Max(max, (link._firstOffset + (link.BufferCount - 1)));
			}

			if ((min != 0) || (max != (_buffer.Count - 2)))
			{
				// Add the necessary rows to reach the new capacity
				_buffer.Add(Math.Max(0, (max - min) - (_buffer.Count - 2)), this);

				// Adjust the beginning of the buffer
				int offsetDelta;
				if (min > 0)
				{
					// Remove any unneeded rows from the beginning of the buffer
					offsetDelta = -min;
					FinalizeBuffers(0, min - 1);
					for (i = 0; i < min; i++)
						_buffer.RemoveAt(0);
					if (_currentOffset > min)
						CurrentReset();
					else
						_currentOffset += offsetDelta;
					_activeOffset += offsetDelta;
					_endOffset += offsetDelta;
				}
				else
				{
					// Add any newly needed rows to the bottom of the buffer
					offsetDelta = 0;
					if (!first)
						for (i = 0; i > min; i--)
							if (!CursorPriorRow())
								break;
							else
								offsetDelta++;
				}

				// Adjust the end of the buffer
				int newEnd = max - min;
				if (newEnd < (_buffer.Count - 2))
				{
					// Shrink the end of the buffer
					FinalizeBuffers(newEnd + 1, (_buffer.Count - 2));
					for (i = (_buffer.Count - 2); i > newEnd; i--)
						_buffer.RemoveAt(i);
					if (_currentOffset > newEnd)
						CurrentReset();
					if (_endOffset > newEnd)
						_endOffset = newEnd;
				}
				
				// Grow the end of the buffer if necessary
				CursorNextRows();

				// Indicate that the the first row is BOF (if we are told that the cursor is located at the first row)
				if (first && (_endOffset >= 0))
					_buffer[0].RowFlag |= RowFlag.BOF;

				// Delta each DataLink to maintain the same relative position within their buffers
				if (offsetDelta != 0)
					foreach (DataLink link in links)
						link.UpdateFirstOffset(offsetDelta);
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
		protected abstract void InternalSelect(IRow row);
		protected abstract void InternalReset();
		protected abstract bool InternalGetBOF();
		protected abstract bool InternalGetEOF();
		
		// BackwardsNavigable
		protected abstract void InternalFirst();
		protected abstract bool InternalPrior();
		
		// Bookmarkable
		protected abstract Guid InternalGetBookmark();
		protected abstract bool InternalGotoBookmark(Guid bookmark, bool forward);
		protected abstract void InternalDisposeBookmark(Guid bookmark);
		protected abstract void InternalDisposeBookmarks(Guid[] bookmarks);
		
		// Searchable
		protected abstract void InternalRefresh(IRow row);
		protected abstract IRow InternalGetKey();
		protected abstract bool InternalFindKey(IRow key);
		protected abstract void InternalFindNearest(IRow key);

		// Updateable
		protected virtual void InternalEdit() {}
		protected virtual void InternalInsertAppend() {}
		protected virtual void InternalInitializeRow(IRow row)
		{
			InternalDefault(row);
		}
		protected virtual void InternalCancel() {}
		protected virtual void InternalValidate(bool isPosting) 
		{
			// Ensure that all non-nillable are not nil
			foreach (DAE.Schema.TableVarColumn column in TableVar.Columns)
				if (!column.IsNilable && this[column.Name].IsNil)
				{
					this[column.Name].FocusControl();
					throw new ClientException(ClientException.Codes.ColumnRequired, column.Name);
				}
		}

		protected virtual void InternalDefault(IRow row)
		{
			DoDefault();
		}
		
		protected virtual bool InternalColumnChanging(DataField field, IRow oldRow, IRow newRow)
		{
			DoRowChanging(field);
			return true;
		}
		
		protected virtual bool InternalColumnChanged(DataField field, IRow oldRow, IRow newRow)
		{
			DoRowChanged(field);
			return true;
		}
		
		protected virtual void InternalChangeColumn(DataField field, IRow oldRow, IRow newRow)
		{
			try
			{
				InternalColumnChanging(field, oldRow, newRow);
			}
			catch
			{
				if (oldRow.HasValue(field.ColumnIndex))
					newRow[field.ColumnIndex] = oldRow[field.ColumnIndex];
				else
					newRow.ClearValue(field.ColumnIndex);
				throw;
			}
			
			InternalColumnChanged(field, oldRow, newRow);
		}
		
		protected abstract void InternalPost(IRow row);
		protected abstract void InternalDelete();
		protected abstract void InternalCursorSetChanged(IRow row, bool reprepare);

		#endregion
		
		#region DataLinks & Sources

		private List<DataSource> _sources = new List<DataSource>();

		internal void AddSource(DataSource dataSource)
		{
			_sources.Add(dataSource);
			if (Active)
				BufferUpdateCount(false);
		}

		internal void RemoveSource(DataSource dataSource)
		{
			_sources.Remove(dataSource);
			if (Active)
				BufferUpdateCount(false);
		}

		/// <summary> Requests that the associated links make any pending updates to the DataSet. </summary>
		public void RequestSave()
		{
			if ((_state == DataSetState.Edit) || (_state == DataSetState.Insert))
				foreach (DataLink link in EnumerateLinks())
					link.SaveRequested();
		}
		
		/// <summary> Returns the current set of DataLinks that are associated with all DataSources that reference this DataSet. </summary>
		/// <returns> 
		///		It is important that this method generate an array, so that a snapshot of the links is used for iteration; 
		///		operations on the array may cause DataLinks to be created or destroyed. 
		///	</returns>
		protected DataLink[] EnumerateLinks()
		{
			List<DataLink> tempList = new List<DataLink>();
			foreach (DataSource source in _sources)
				source.EnumerateLinks(tempList);
			return tempList.ToArray();
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
			if (_state == DataSetState.Inactive)
			{
				DoBeforeOpen();
				InternalConnect();
				InternalOpen();
				CreateFields();
				BufferUpdateCount(true);
				_internalBOF = true;
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
			if (_state != DataSetState.Inactive)
			{
				Cancel();
				DoBeforeClose();
				CoreClose();
				DoAfterClose();
			}
		}
		
		#endregion

		#region State

		private DataSetState _state;
		/// <summary> The current state of the DataSet. </summary>
		[Browsable(false)]
		public DataSetState State { get { return _state; } }

		/// <summary> Throws an exception if the the DataSet's state is in the given set of states. </summary>
		public void CheckState(params DataSetState[] states)
		{
			foreach (DataSetState state in states)
				if (_state == state)
					return;
			throw new ClientException(ClientException.Codes.IncorrectState, _state.ToString());
		}

		//Note: SetState has side affects, there is no guarantee that the state has not changed if an exception is thrown.
		private void SetState(DataSetState state)
		{
			if (_state != state)
			{
				_state = state;
				_isModified = false;
				StateChanged();
			}
		}
		
		/// <summary>Forces a set state, ignoring any exceptions that occur.</summary>
		private void ForcedSetState(DataSetState state)
		{
			try
			{
				SetState(state);
			}
			catch
			{
				// Ignore an exception, we are performing a forced set state
			}
		}

		protected void EnsureBrowseState(bool postChanges)
		{
			CheckActive();

			if ((_state == DataSetState.Edit) || (_state == DataSetState.Insert))
			{
				RequestSave();
				if (IsModified && postChanges)
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
			get { return _state != DataSetState.Inactive; }
			set
			{
				if (_initializing)
					_delayedActive = value;
				else
				{
					if (value != (_state != DataSetState.Inactive))
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
			if (_state == DataSetState.Inactive)
				throw new ClientException(ClientException.Codes.NotActive);
		}
		
		/// <summary> Throws an exception if the DataSet is active. </summary>
		public void CheckInactive()
		{
			if (_state != DataSetState.Inactive)
				throw new ClientException(ClientException.Codes.Active);
		}

		protected void CursorSetChanged(IRow row, bool reprepare)
		{
			if (State != DataSetState.Inactive)
			{
				EnsureBrowseState();
				BufferClose();
				try
				{
					InternalCursorSetChanged(row, reprepare);
				}
				catch
				{
					Close();
					throw;
				}
				Resume(row);
			}
		}
		
		#endregion

		#region ISupportInitialize

		private bool _initializing;

		// The Active properties value during the intialization period defined in ISupportInitialize.
		private bool _delayedActive;

		/// <summary> Called to indicate that the properties of the DataSet are being read (and therefore DataSet should not activate yet). </summary>
		public void BeginInit()
		{
			_initializing = true;
		}

		/// <summary> Called to indicate that the properties of the DataSet have been read (and therefore the DataSet can be activated). </summary>
		public virtual void EndInit()
		{
			_initializing = false;
			Active = _delayedActive;
		}

		#endregion

		#region Navigation
		
		private bool _internalBOF;

		/// <summary> True when the DataSet is on its first row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the first row. </remarks>
		[Browsable(false)]
		public bool IsFirstRow
		{
			get { return _internalBOF || ((_activeOffset >= 0) && ((_buffer[_activeOffset].RowFlag & RowFlag.BOF) != 0)); }
		}
		
		/// <summary> True when the DataSet is on its first row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the first row. </remarks>
		[Browsable(false)]
		public bool BOF
		{
			get { return _internalBOF; }
		}

		private bool _internalEOF;

		/// <summary> True when the DataSet is on its last row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the last row. </remarks>
		[Browsable(false)]
		public bool IsLastRow
		{
			get { return _internalEOF || ((_activeOffset >= 0) && ((_buffer[_activeOffset].RowFlag & RowFlag.EOF) != 0)); }
		}
		
		/// <summary> True when the DataSet is on its last row. </summary>
		/// <remarks> Note that this may not be set until attempting to navigate past the last row. </remarks>
		[Browsable(false)]
		public bool EOF
		{
			get { return _internalEOF; }
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
				if (_endOffset >= 0)
					_buffer[0].RowFlag |= RowFlag.BOF;
			}
			finally
			{
				_internalBOF = true;
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
				_internalEOF = true;
				DoDataChanged();
			}
		}
		
		/// <summary> Attempts to scroll the DataSet by the specified delta. </summary>
		/// <remarks> Any outstanding Insert or Edit will first be posted. </remarks>
		/// <param name="delta"> Number of rows, positive or negative to move relative to the current active row. </param>
		/// <returns> Number of rows actually scrolled. </returns>
		public int MoveBy(int delta)
		{
			int result = 0;
			if (delta != 0)
			{
				EnsureBrowseState();
				if (((delta > 0) && !IsLastRow) || ((delta < 0) && !IsFirstRow))
				{
					_internalBOF = false;
					_internalEOF = false;
					int offsetDelta = 0;
					bool willScroll;
					try
					{
						if (delta > 0)
						{
							// Move the active offset as far a possible within the current buffer
							result = Math.Min(delta, _endOffset - _activeOffset);
							delta -= result;
							_activeOffset += result;

							// Advance any additional rows by progressing through the cursor
							while (delta > 0) 
							{
								willScroll = (_endOffset >= (_buffer.Count - 2));	// Note whether the read of the next row will cause the buffer to scroll
								if (CursorNextRow())
								{
									if (willScroll)
										offsetDelta--;
									_activeOffset++;
									delta--;
									result++;
								}
								else
								{
									_internalEOF = true;
									break;
								}
							}
						}
						else if (delta < 0)
						{
							// Move the active offset as far a possible within the current buffer
							result = Math.Max(delta, -_activeOffset);
							delta -= result;
							_activeOffset += result;

							// Retreive any additional rows by digressing through the cursor
							while (delta < 0)
							{
								willScroll = (_endOffset >= (_buffer.Count - 2));	// Note whether the read of the next row will cause the buffer to scroll
								if (CursorPriorRow())
								{
									if (willScroll)
										offsetDelta++;
									_activeOffset--;
									delta++;
									result--;
								}
								else
								{
									_internalBOF = true;
									break;
								}
							}
						}
					}
					finally
					{
						if (offsetDelta != 0)
							UpdateFirstOffsets(offsetDelta);
						DoDataChanged();
					}
				}
			}
			return result;
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
			return _endOffset == -1;
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
				if (_activeOffset <= _endOffset)
				{
					InternalRefresh(_buffer[_activeOffset].Row);
					_currentOffset = _activeOffset;
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
		public void Refresh(IRow row)
		{
			EnsureBrowseState();
			try
			{
				InternalRefresh(row);
				CurrentReset();
			}
			finally
			{
				Resync(false, false);
			}
		}
		
		#endregion

		#region Modification

		protected bool _isModified;
		
		/// <summary> Indicates whether the DataSet has been modified. </summary>
		[Browsable(false)]
		public bool IsModified
		{
			get
			{
				CheckActive();
				return _isModified;
			}
		}

		private bool _isReadOnly;

		protected virtual bool InternalIsReadOnly
		{
			get { return _isReadOnly; }
			set { _isReadOnly = value; }
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
		
		protected bool _isWriteOnly;

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
			get { return _isWriteOnly; }
			set
			{
				if (_isWriteOnly != value)
				{
					CheckState(DataSetState.Inactive);
					_isWriteOnly = value;
				}
			}
		}
		
		/// <summary> Throws an exception if the DataSet is in WriteOnly mode and cannot be used to Edit or Delete </summary>
		public virtual void CheckCanRead()
		{
			if (_isWriteOnly)
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
			if ((_state != DataSetState.Edit) && (_state != DataSetState.Insert))
			{
				if (_endOffset == -1)
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
		
		protected void InitializeRow(int rowIndex)
		{
			IRow targetRow = _buffer[rowIndex].Row;
			targetRow.ClearValues();
			InternalInitializeRow(targetRow);
		}
		
		protected internal void ChangeColumn(DataField field, IScalar value)
		{
			Edit();
			IRow activeRow = _buffer[_activeOffset].Row;
			IRow saveOldRow = _oldRow;
			_oldRow = new Row(activeRow.Manager, activeRow.DataType);
			try
			{
				activeRow.CopyTo(_oldRow);
				if (value == null)
					activeRow.ClearValue(field.ColumnIndex);
				else
					activeRow[field.ColumnIndex] = value;
					
				_valueFlags[field.ColumnIndex] = true;
					
				InternalChangeColumn(field, _oldRow, activeRow);
					
				_isModified = true;
			}
			finally
			{
				_oldRow.Dispose();
				_oldRow = saveOldRow;
			}
		}

		private void EndInsertAppend()
		{
			InternalInsertAppend();
			try
			{
				SetState(DataSetState.Insert);
				InitializeRow(_activeOffset);
				_isModified = false;
				DoDataChanged();
				DoAfterInsert();
			}
			catch
			{
				try
				{
					Cancel();
				}
				catch
				{
					// Ignore exceptions here, we're trying to clean up
				}

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
			DataSetRow activeRow = _buffer[_activeOffset];
			_buffer.Move(_buffer.Count - 1, _activeOffset);
			if (_endOffset == -1)
				_buffer[_activeOffset].RowFlag = RowFlag.Inserted | RowFlag.BOF | RowFlag.EOF;
			else
			{
				_buffer[_activeOffset].RowFlag = RowFlag.Inserted | RowFlag.Data | (activeRow.RowFlag & RowFlag.BOF);
				_buffer[_activeOffset].Bookmark = activeRow.Bookmark;
				activeRow.RowFlag &= ~RowFlag.BOF;
			}
			if (_currentOffset >= _activeOffset)
				_currentOffset++;
			if (_endOffset < (_buffer.Count - 2))
				_endOffset++;
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
			bool wasEmpty = (_endOffset < 0);
			if (wasEmpty || ((_buffer[_endOffset].RowFlag & RowFlag.EOF) == 0))	// Will we have to resync to the end
			{
				// If the EOF row is not in our buffer, we will have to clear the buffer and start with a new row at the end.
				BufferClear();
				_endOffset = 0;
				_buffer[0].RowFlag = RowFlag.Inserted | RowFlag.EOF;

				// If there were rows before, read them again
				if (!wasEmpty)
					CursorPriorRows();
			}
			else
			{
				// The EOF row is in our buffer, as an optimization, append our row after the EOF row
				DataSetRow lastRow = _buffer[_endOffset];
				if (_endOffset == (_buffer.Count - 2))	// Buffer is full so bump the first item
				{
					_buffer.Move(0, _endOffset);
					if (_currentOffset >= 0)
						_currentOffset--;
				}
				else									// The buffer is not full, pull the scratch pad row
				{
					_buffer.Move(_buffer.Count - 1, _endOffset);
					_endOffset++;
				}
				_buffer[_endOffset].RowFlag = RowFlag.Inserted | RowFlag.EOF | (lastRow.RowFlag & RowFlag.EOF);
				lastRow.RowFlag &= ~RowFlag.EOF;
			}
			_activeOffset = _endOffset;
			_internalBOF = false;
			EndInsertAppend();
		}

		/// <summary> Cancel's the edit or insert state. </summary>
		/// <remarks> 
		///		If the DataSet is not in Insert or Edit state then this method does nothing.
		///	</remarks>
		public void Cancel()
		{
			if ((_state == DataSetState.Insert) || (_state == DataSetState.Edit))
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
							if ((_buffer[_activeOffset].RowFlag & RowFlag.Inserted) != 0)
							{
								// Move active row to scratchpad if this was an insert and not the only row
								bool wasLastRow = (_buffer[_activeOffset].RowFlag & RowFlag.EOF) != 0;
								if (!wasLastRow)
									_buffer.Move(_activeOffset, _buffer.Count - 1);
								else
									_buffer[_activeOffset].RowFlag &= ~(RowFlag.Inserted | RowFlag.Data);

								if (_endOffset == 0)
								{
									if (_originalRow != null)
										_originalRow.CopyTo(_buffer[_activeOffset].Row);
									else
										_endOffset--;
								}
								else
								{
									_endOffset--;
									if (wasLastRow) // if append
										_activeOffset--;
								}
							}
							else
							{
								_originalRow.CopyTo(_buffer[_activeOffset].Row);
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
			if ((_state == DataSetState.Insert) || (_state == DataSetState.Edit))
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
			if ((_state == DataSetState.Insert) || (_state == DataSetState.Edit))
			{
				DoValidate();
				InternalValidate(true);
				DoBeforePost();
				if (UpdatesThroughCursor() && (_state == DataSetState.Edit))
					CurrentGotoActive(true);

				// Make sure that all details have posted
				DoPrepareToPost();
				InternalPost(_buffer[_activeOffset].Row);
				_isModified = false;
				SetState(DataSetState.Browse);
				if (RefreshAfterPost)
					Resync(false, false);
				else
				{
					if ((_buffer[_activeOffset].RowFlag & RowFlag.Inserted) != 0)
						_buffer[_activeOffset].RowFlag &= ~(RowFlag.Inserted | RowFlag.Data);
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
			if (_state == DataSetState.Insert)
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
			InitializeRow(_activeOffset);
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
			foreach (DataLink link in EnumerateLinks())
				link.Default();
		}
		
		/// <summary> Called before posting to validate the DataSet's data. </summary>
		public event EventHandler OnValidate;

		protected virtual void DoValidate()
		{
			if (OnValidate != null)
				OnValidate(this, EventArgs.Empty);
		}

		public event ErrorsOccurredHandler OnErrors;

		protected void ReportErrors(CompilerMessages messages)
		{
			if (OnErrors != null)
				OnErrors(this, messages);
		}

		/// <summary>Forces a data changed, ignoring any exceptions that occur.</summary>
		protected void ForcedDataChanged()
		{
			try
			{
				DoDataChanged();
			}
			catch (Exception exception)
			{
				ReportErrors(new CompilerMessages { exception });
				// Don't rethrow
			}
		}
		
		public event EventHandler DataChanged;
		
		/// <summary> Occurs the active row or any other part of the buffer changes. </summary>
		protected virtual void DoDataChanged()
		{
			if (DataChanged != null)
				DataChanged(this, EventArgs.Empty);
				
			Exception firstException = null;
			foreach (DataLink link in EnumerateLinks())
			{
				try
				{
					link.InternalDataChanged();
				}
				catch (Exception exception)
				{
					if (firstException == null)
						firstException = exception;
				}
			}
			if (firstException != null)
				throw firstException;
		}
		
		/// <summary> Occurs when the active row's position within the buffer changes. </summary>
		protected virtual void UpdateFirstOffsets(int delta)
		{
			foreach (DataLink link in EnumerateLinks())
				link.UpdateFirstOffset(delta);
		}
		
		/// <summary> Occurs before posting (typically to allow any dependants to post). </summary>
		protected virtual void DoPrepareToPost()
		{
			foreach (DataLink link in EnumerateLinks())
				link.PrepareToPost();
		}

		/// <summary> Occurs before canceling (typically to allow any dependants to cancel). </summary>
		protected virtual void DoPrepareToCancel()
		{
			foreach (DataLink link in EnumerateLinks())
				link.PrepareToCancel();
		}
		
		public event FieldChangeEventHandler RowChanging;

		/// <summary> Occurs only when the fields in the active record are changing. </summary>
		/// <param name="field"> Valid reference to a field if one field is changing. Null otherwise. </param>
		protected virtual void DoRowChanging(DataField field)
		{
			if (RowChanging != null)
				RowChanging(this, field);
			foreach (DataLink link in EnumerateLinks())
				link.RowChanging(field);
		}

		public event FieldChangeEventHandler RowChanged;
		
		/// <summary> Occurs only when the fields in the active record has changed. </summary>
		/// <param name="field"> Valid reference to a field if one field has changed. Null otherwise. </param>
		protected virtual void DoRowChanged(DataField field)
		{
			if (RowChanged != null)
				RowChanged(this, field);
			foreach (DataLink link in EnumerateLinks())
				link.RowChanged(field);
		}
		
		/// <summary> Occurs when a request is made that any control(s) associated with the given field should be focused. </summary>
		/// <remarks> This is typically invoked when validation fails for a field (so that the user can use the control to correct the problem). </remarks>
		protected internal virtual void FocusControl(DataField field)
		{
			foreach (DataLink link in EnumerateLinks())
				link.FocusControl(field);
		}

		/// <summary> Occurs in reponse to any change in the state of the DataSet. </summary>
		protected virtual void StateChanged()
		{
			Exception firstException = null;
			foreach (DataLink link in EnumerateLinks())
			{
				try
				{
					link.StateChanged();
				}
				catch (Exception exception)
				{
					if (firstException == null)
						firstException = exception;
				}
			}
			if (firstException != null)
				throw firstException;
		}
		
		#endregion

		#region Fields
		
		private DataFields _dataFields;
		/// <summary> Collection of DataField objects representing the columns of the active row. </summary>
		[Browsable(false)]
		public DataFields Fields { get { return _dataFields; } }
		
		/// <summary> Internal fields list. </summary>
		internal Schema.Objects _fields;

		/// <summary> Attempts to retrieve a DataField by index. </summary>
		/// <remarks> An exception is thrown if the specified index is out of bounds. </remarks>
		public DataField this[int index]
		{
			get
			{
				DataField field = (DataField)_fields[index];
				if (field == null)
					throw new ClientException(ClientException.Codes.FieldForColumnNotFound, index);
				return field;
			}
		}
		
		/// <summary> Attempts to retrieve a DataField by name. </summary>
		/// <remarks> An exception is thrown if the specified name is not found. </remarks>
		public DataField this[string columnName]
		{
			get
			{
				DataField field = (DataField)_fields[columnName];
				if (field == null)
					throw new ClientException(ClientException.Codes.FieldForColumnNotFound, columnName);
				return field;
			}
		}

		/// <summary> The number of DataFields in the DataSet. </summary>
		[Browsable(false)]
		public int FieldCount
		{
			get { return _fields.Count; }
		}

		private void CreateFields()
		{
			int index = 0;
			DataField field;
			foreach (Schema.Column column in InternalGetTableType().Columns)
			{
				field = new DataField(this, column);
				_fields.Add(field);
				index++;
			}

			_valueFlags = new BitArray(_fields.Count);
			_valueFlags.SetAll(false);
		}
		
		private void FreeFields()
		{
			if (_fields != null)
				_fields.Clear();
				
			_valueFlags = null;
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

		public static string GetNamesFromKey(Schema.Key key)
		{
			StringBuilder result = new StringBuilder();
			if (key != null)
				foreach (Schema.TableVarColumn column in key.Columns)
				{
					if (result.Length > 0)
						result.Append(ColumnNameDelimiters[0]);
					result.Append(column.Name);
				}
			return result.ToString();
		}

		public static Schema.Key GetKeyFromNames(string keyNames)
		{
			Schema.Key result = new Schema.Key();
			string trimmed;
			foreach (string item in keyNames.Split(ColumnNameDelimiters))
			{
				trimmed = item.Trim();
				if (trimmed != String.Empty)
					result.Columns.Add(new Schema.TableVarColumn(new Schema.Column(trimmed, null)));
			}
			return result;
		}

		public static string GetNamesFromOrder(Schema.Order order)
		{
			StringBuilder result = new StringBuilder();
			if (order != null)
				foreach (Schema.OrderColumn column in order.Columns)
				{
					if (result.Length > 0)
						result.Append(ColumnNameDelimiters[0]);
					result.Append(column.Column.Name);
					if (!column.Ascending)
						result.AppendFormat(" {0}", Keywords.Asc);
					if (column.IncludeNils)
						result.AppendFormat(" {0} {1}", Keywords.Include, Keywords.Nil);
				}
			return result.ToString();
		}

		public static Schema.Order GetOrderFromNames(string orderNames)
		{
			Schema.Order result = new Schema.Order();
			string trimmed;
			foreach (string item in orderNames.Split(ColumnNameDelimiters))
			{
				trimmed = item.Trim();
				if (trimmed != String.Empty)
				{
					string[] items = item.Split(' ');
					Schema.OrderColumn orderColumn = new Schema.OrderColumn(new Schema.TableVarColumn(new Schema.Column(items[0], null)), true, false);
					
					if (items.Length > 1)
					{
						switch (items[1])
						{
							case Keywords.Desc : orderColumn.Ascending = false; break;
							case Keywords.Include : orderColumn.IncludeNils = true; break;
						}
					}
					
					if ((items.Length > 2) && (items[2] == Keywords.Include))
						orderColumn.IncludeNils = true;
					
					result.Columns.Add(orderColumn);
				}
			}
			return result;
		}

		/// <summary> Gets a row representing a key for the active row. </summary>
		/// <returns> A row containing key information.  It is the callers responsability to Dispose this row when it is no longer required. </returns>
		public IRow GetKey()
		{
			EnsureBrowseState();
			CheckNotEmpty();
			CurrentGotoActive(true);
			return InternalGetKey();
		}

		/// <summary> Attempts navigation to a specific row given a key. </summary>
		/// <param name="key"> A row containing key information.  This is typically retrieved using <see cref="GetKey()"/>. </param>
		/// <returns> 
		///		True if the row was located and navigation occurred.  If this method returns false, indicating that the 
		///		specified key was not located, then the active row will not have changed from before the call. 
		///	</returns>
		public bool FindKey(IRow key)
		{
			EnsureBrowseState();
			try
			{
				return InternalFindKey(key);
			}
			finally
			{
				Resync(true, true);
			}
		}
		
		/// <summary> Navigates the DataSet to the row nearest the specified key or partial key. </summary>
		/// <param name="key"> A full or partial row containing search criteria for the current order. </param>
		public void FindNearest(IRow key)
		{
			EnsureBrowseState();
			try
			{
				InternalFindNearest(key);
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
			public DataSetEnumerator(DataSet dataSet)
			{
				_dataSet = dataSet;
			}

			private DataSet _dataSet;
			private bool _initial = true;

			public void Reset()
			{
				_dataSet.First();
				_initial = true;
			}

			public object Current
			{
				get { return _dataSet.ActiveRow; }
			}

			public bool MoveNext()
			{
				bool result = !_dataSet.IsEmpty() && (_initial || (_dataSet.MoveBy(1) == 1));
				_initial = false;
				return result;
			}
		}

		#endregion

		#region class DataSetRow

		internal class DataSetRow : Disposable
		{
			public DataSetRow() : base(){}
			public DataSetRow(IRow row, RowFlag rowFlag, Guid bookmark)
			{
				_row = row;
				_rowFlag = rowFlag;
				_bookmark = bookmark;
			}
			
			protected override void Dispose(bool disposing)
			{
				if (_row != null)
				{
					_row.Dispose();
					_row = null;
				}
			}
			
			private IRow _row;
			public IRow Row
			{
				get { return _row; }
				set 
				{ 
					if (_row != null)
						_row.Dispose();
					_row = value; 
				}
			}
			
			private RowFlag _rowFlag;
			public RowFlag RowFlag
			{
				get { return _rowFlag; }
				set { _rowFlag = value; }
			}
			
			private Guid _bookmark;
			public Guid Bookmark
			{
				get { return _bookmark; }
				set { _bookmark = value; }
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
			public void Add(int count, DataSet dataSet)
			{
				for (int index = 0; index < count; index++)
					Add(new DataSetRow(new Row(dataSet.Process.ValueManager, dataSet.TableType.RowType), new RowFlag(), Guid.Empty));
			}
		}
		
		#endregion
	}

	public class DataSetException : Exception
	{
		public DataSetException(string message, Exception inner) : base(message, inner) {}
		public DataSetException(string message) : base(message) {}
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
		internal DataFields(DataSet dataSet)
		{
			_dataSet = dataSet;
		}
		
		DataSet _dataSet;
		
		public int Count
		{
			get { return _dataSet.FieldCount; }
		}
		
		public IEnumerator GetEnumerator()
		{
			return new DataFieldEnumerator(_dataSet);
		}
		
		public DataField this[int index]
		{
			get { return _dataSet[index]; }
		}
		
		public DataField this[string columnName]
		{
			get { return _dataSet[columnName]; }
		}
		
		public void CopyTo(Array target, int startIndex)
		{
			for (int i = 0; i < Count; i++)
				target.SetValue(this[i], i + startIndex);
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
			get { return _dataSet._fields; }
		}

		public class DataFieldEnumerator : IEnumerator
		{
			internal DataFieldEnumerator(DataSet dataSet)
			{
				_dataSet = dataSet;
			}
			
			private DataSet _dataSet;
			private int _index = -1;
			
			public void Reset()
			{
				_index = -1;
			}
			
			public bool MoveNext()
			{
				_index++;
				return (_index < _dataSet.FieldCount);
			}
			
			public object Current
			{
				get { return _dataSet[_index]; }
			}
		}
    }
    
    public class DataField : Schema.Object, IConvertible
    {
		public DataField(DataSet dataSet, Schema.Column column) : base(column.Name)
		{
			_dataSet = dataSet;
			_column = column;
			_columnIndex = dataSet.TableType.Columns.IndexOfName(column.Name);
		}

		private DataSet _dataSet;
		/// <summary> The DataSet this field is contained in. </summary>
		public DataSet DataSet
		{
			get { return _dataSet; }
		}

		/// <summary> The name of the underlying column. </summary>
		public string ColumnName
		{
			get { return _column.Name; }
		}

		private Schema.Column _column;
		public Schema.IScalarType DataType
		{
			get { return (Schema.IScalarType)_column.DataType; }
		}

		private int _columnIndex;
		public int ColumnIndex
		{
			get { return _columnIndex; }
		}

		/// <summary> Asks any control(s) that might be attached to this field to set focus. </summary>
		public void FocusControl()
		{
			_dataSet.FocusControl(this);
		}

		private void CheckHasValue()
		{
			if (!HasValue())
				throw new ClientException(ClientException.Codes.NoValue, ColumnName);
		}

		private void CheckDataSetNotEmpty()
		{
			if (_dataSet.IsEmpty())
				throw new ClientException(ClientException.Codes.EmptyDataSet);
		}
		
		// Value Access
		/// <summary>Gets or sets the current value of this field as a Scalar.</summary>
		/// <remarks>Setting this value will cause field and dataset level validation and change events to be invoked.
		/// The validate event is invoked first, followed by the change event. If an exception is thrown during the 
		/// validation event, the field will not be set to the new value. However, if an exception is thrown during
		/// the change event, the field will still be set to the new value.</remarks>
		public IScalar Value
		{
			get
			{
				CheckHasValue();
				return (IScalar)_dataSet.ActiveRow.GetValue(_columnIndex);
			}
			set
			{
				_dataSet.ChangeColumn(this, value);
			}
		}
		
		/// <summary>Indicates whether the value for this column has been modified.</summary>
		public bool Modified { get { return _dataSet._valueFlags == null ? false : _dataSet._valueFlags[_columnIndex]; } }

		/// <summary>Retrieves the old value for this field during a change or validate event.</summary>
		/// <remarks>This value is only available during a change or validate event. A ClientException exception will be
		/// thrown if this property is accessed at any other time.</remarks>
		public IScalar OldValue
		{
			get
			{
				return (IScalar)_dataSet.OldRow.GetValue(_columnIndex);
			}
		}

		/// <summary>Retrieves the original value for this field prior to any changes made during the edit.</summary>
		/// <remarks>This value is only available when the dataset is in edit state. A ClientException exception will be
		/// thrown if this property is accessed at any other time.</remarks>		
		public IScalar OriginalValue
		{
			get
			{
				return (IScalar)_dataSet.OriginalRow.GetValue(_columnIndex);
			}
		}
		
		/// <summary> True when the field has a value. </summary>
		public bool HasValue()
		{
			CheckDataSetNotEmpty();
			return _dataSet.ActiveRow.HasValue(_columnIndex);
		}

		public bool IsNil
		{
			get
			{
				CheckDataSetNotEmpty();
				return !_dataSet.ActiveRow.HasValue(_columnIndex);
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsNative = value; // Has to be done thiw way in order to fire the appropriate dataset events
				Value = newValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsBoolean = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public bool GetAsBoolean(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsBoolean(representationName);
			return false;
		}
		
		public void SetAsBoolean(string representationName, bool value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsBoolean(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsByte = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public byte GetAsByte(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsByte(representationName);
			return (byte)0;
		}
		
		public void SetAsByte(string representationName, byte value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsByte(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsDecimal = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public decimal GetAsDecimal(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsDecimal(representationName);
			return 0m;
		}
		
		public void SetAsDecimal(string representationName, decimal value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsDecimal(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsTimeSpan = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public TimeSpan GetAsTimeSpan(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsTimeSpan(representationName);
			return TimeSpan.Zero;
		}
		
		public void SetAsTimeSpan(string representationName, TimeSpan value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsTimeSpan(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsDateTime = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}

		public DateTime GetAsDateTime(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsDateTime(representationName);
			return DateTime.MinValue;
		}
		
		public void SetAsDateTime(string representationName, DateTime value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsDateTime(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
		}
		
		private const string DoubleNotSupported = "Alphora.Dataphor.DAE.Client.DataField: Double not supported.  Use Float(single) instead.";

		public double AsDouble
		{
			get
			{
				throw new Exception(DoubleNotSupported);
			}
			set
			{
				throw new Exception(DoubleNotSupported);
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsInt16 = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public short GetAsInt16(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsInt16(representationName);
			return (short)0;
		}
		
		public void SetAsInt16(string representationName, short value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsInt16(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsInt32 = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public int GetAsInt32(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsInt32(representationName);
			return 0;
		}
		
		public void SetAsInt32(string representationName, int value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsInt32(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsInt64 = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public long GetAsInt64(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsInt64(representationName);
			return (long)0;
		}
		
		public void SetAsInt64(string representationName, long value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsInt64(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsString = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public string GetAsString(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsString(representationName);
			return String.Empty;
		}
		
		public void SetAsString(string representationName, string value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsString(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsDisplayString = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
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
				Scalar newValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
				newValue.AsGuid = value; // Has to be done this way in order to fire the appropriate dataset events
				Value = newValue;
			}
		}
		
		public Guid GetAsGuid(string representationName)
		{
			CheckDataSetNotEmpty();
			if (HasValue())
				return Value.GetAsGuid(representationName);
			return Guid.Empty;
		}
		
		public void SetAsGuid(string representationName, Guid value)
		{
			CheckDataSetNotEmpty();
			Scalar localValue = new Scalar(_dataSet.Process.ValueManager, DataType, null);
			localValue.SetAsGuid(representationName, value); // Has to be done this way in order to fire the appropriate dataset events
			Value = localValue;
		}
		
		public object AsType(Type type)
		{
			switch (type.ToString())
			{
				case ("Boolean") : return AsBoolean;
				case ("Byte") : return AsByte;
				case ("Int16") : return AsInt16;
				case ("Int32") : return AsInt32;
				case ("Int64") : return AsInt64;
				case ("String") : return AsString;
				default : throw new ClientException(ClientException.Codes.CannotConvertFromType, type.ToString());
			}
		}
		
		// IConvertible interface
		TypeCode IConvertible.GetTypeCode()
		{
			throw new DataSetException("Internal Error: DataField.IConvertible.GetTypeCode()");
		}
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return AsBoolean;
		}
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return AsByte;
		}
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return (char)AsType(typeof(char));
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			return (DateTime)AsType(typeof(DateTime));
		}
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return AsDecimal;
		}
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			throw new Exception(DoubleNotSupported);
		}
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return AsInt16;
		}
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return AsInt32;
		}
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return AsInt64;
		}
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return (sbyte)AsType(typeof(sbyte));
		}
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return (float)AsType(typeof(float));
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return AsString;
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return AsType(conversionType);
		}
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return (ushort)AsType(typeof(ushort));
		}
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return (uint)AsType(typeof(uint));
		}
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return (ulong)AsType(typeof(ulong));
		}
		
		// Explicit operator conversions
		public static explicit operator bool(DataField field)
		{
			return field.AsBoolean;
		}
		public static explicit operator byte(DataField field)
		{
			return field.AsByte;
		}
		public static explicit operator decimal(DataField field)
		{
			return field.AsDecimal;
		}
		public static explicit operator int(DataField field)
		{
			return field.AsInt32;
		}
		public static explicit operator long(DataField field)
		{
			return field.AsInt64;
		}
		public static explicit operator short(DataField field)
		{
			return field.AsInt16;
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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}
		
		private DataSource _source;
		/// <summary> The <see cref="DataSource"/> associated with this DataLink. </summary>
		public DataSource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					if (_source != null)
					{
						_source.RemoveLink(this);
						_source.Disposed -= new EventHandler(DataSourceDisposed);
						_source.OnDataSetChanged -= new EventHandler(DataSourceDataSetChanged);
					}
					_source = value;
					if (_source != null)
					{
						_source.OnDataSetChanged += new EventHandler(DataSourceDataSetChanged);
						_source.Disposed += new EventHandler(DataSourceDisposed);
						_source.AddLink(this);
					}

					DataSetChanged();
				}
			}
		}
		
		private void DataSourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}
		
		private void DataSourceDataSetChanged(object sender, EventArgs args)
		{
			DataSetChanged();
		}

		private void DataSetChanged()
		{
			// Set FActive to opposite of Active to ensure that ActiveChanged is fired (so that the control always get's an ActiveChanged on way or another)
			_active = !Active;
			StateChanged();
		}
		
		/// <summary> Gets the DataSet associated with this link, or null if none. </summary>
		public DataSet DataSet
		{
			get
			{
				if (_source == null)
					return null;
				else
					return _source.DataSet;
			}
		}
		
		/// <summary> Indicates whether the link is active (connected to an active <see cref="DataSet"/>). </summary>
		public bool Active
		{
			get { return (DataSet != null) && DataSet.Active; }
		}

		internal int _firstOffset;	// This is maintained by the DataSet
		
		/// <summary> Gets the active row offset relative to the link. </summary>
		/// <remarks> Do not attempt to retrieve this if the link is not <see cref="Active"/>. </remarks>
		public int ActiveOffset
		{
			get { return DataSet._activeOffset - _firstOffset; }
		}

		/// <summary> Retrieves a DataSet buffer row relative to this link. </summary>
		/// <remarks> 
		///		Do not attempt to access this if the link is not <see cref="Active"/>.  Do not modify 
		///		the buffer rows in any way, use them only to read data. 
		///	</remarks>
		public DAE.Runtime.Data.IRow Buffer(int index)
		{
			return DataSet._buffer[index + _firstOffset].Row;
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
				int result = _firstOffset + (_bufferCount - 1);
				if (result > DataSet.EndOffset)
					result = DataSet.EndOffset;
				return result - _firstOffset;
			}
		}

		private int _bufferCount = 1;
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
			get { return _bufferCount; }
			set
			{
				if (value < 1)
					value = 1;
				if (_bufferCount != value)
				{
					_bufferCount = value;
					if (Active)
						_source.LinkBufferRangeChanged();	// Make any adjustment to the DataSet buffer size
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
		protected internal virtual void RowChanging(DataField field)
		{
			if (OnRowChanging != null)
				OnRowChanging(this, DataSet, field);
		}

		/// <summary> Called when the active row's data changed as a result of data edits. </summary>
		public event DataLinkFieldHandler OnRowChanged;
		/// <summary> Called when the active row's data changed as a result of data edits. </summary>
		protected internal virtual void RowChanged(DataField field)
		{
			if (OnRowChanged != null)
				OnRowChanged(this, DataSet, field);
		}

		/// <summary> Called when determining the default values for a newly inserted row. </summary>
		/// <remarks> Setting column values in this handler will not set the IsModified of the DataSet. </remarks>
		public event DataLinkHandler OnDefault;
		/// <summary> Called when determining the default values for a newly inserted row. </summary>
		/// <remarks> Setting column values in this handler will not set the IsModified of the DataSet. </remarks>
		protected internal virtual void Default()
		{
			if (OnDefault != null)
				OnDefault(this, DataSet);
		}

		/// <summary> Ensures that the link's buffer range encompasses the active row. </summary>
		/// <remarks> Assumes that the link is active. </remarks>
		protected internal virtual void UpdateRange()
		{
			_firstOffset += Math.Min(DataSet._activeOffset - _firstOffset, 0);							// Slide buffer down if active is below first
			_firstOffset += Math.Max(DataSet._activeOffset - (_firstOffset + (_bufferCount - 1)), 0);	// Slide buffer up if active is above last
		}

		/// <summary> Scrolls the links buffer by the given delta, then ensures that the link buffer is kept in range of the active row. </summary>
		/// <remarks> 
		///		A delta is passed when the DataSet's buffer size is adjusted (to keep the link as close as 
		///		possible to its prior relative position). For example, if the DataSet discards the first couple 
		///		rows of its buffer, this method is called with a Delta of -2 so that the rows in this link's 
		///		buffer appear to be unmoved. 
		///	</remarks>
		protected internal virtual void UpdateFirstOffset(int delta)
		{
			_firstOffset += delta;
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
		protected internal virtual void FocusControl(DataField field)
		{
			if (OnFocusControl != null)
				OnFocusControl(this, DataSet, field);
		}
		
		/// <summary> Called when the DataSet's state has changed. </summary>
		public event DataLinkHandler OnStateChanged;
		/// <summary> Called when the DataSet's state has changed. </summary>
		protected internal virtual void StateChanged()
		{
			if (Active != _active)
			{
				ActiveChanged();
				_active = Active;
			}

			if (OnStateChanged != null)
				OnStateChanged(this, DataSet);
		}
		
		private bool _active;
		/// <summary> Called when the DataSet's active state changes or when attached/detached from an active DataSet. </summary>
		public event DataLinkHandler OnActiveChanged;
		/// <summary> Called when the DataSet's active state changes or when attached/detached from an active DataSet. </summary>
		protected internal virtual void ActiveChanged()
		{
			if (Active)
				UpdateRange();
			else
				_firstOffset = 0;
				
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

		public DataSource(IContainer container) : this()
		{
			if (container != null)
				container.Add(this);
		}

		protected override void Dispose(bool disposing)
		{
			DataSet = null;
			base.Dispose(disposing);
			if (_links != null)
			{
				while (_links.Count > 0)
					_links[_links.Count - 1].Source = null;
				_links = null;
			}
		}

		internal List<DataLink> _links = new List<DataLink>();
		
		internal void AddLink(DataLink link)
		{
			_links.Add(link);
			LinkBufferRangeChanged();
		}
		
		internal void RemoveLink(DataLink link)
		{
			_links.Remove(link);
			LinkBufferRangeChanged();
		}
		
		internal void LinkBufferRangeChanged()
		{
			if ((_dataSet != null) && (_dataSet.Active))
				_dataSet.BufferUpdateCount(false);
		}
		
		public void EnumerateLinks(List<DataLink> links)
		{
			foreach (DataLink link in _links)
				links.Add(link);
		}
		
		private DataSet _dataSet;
		/// <summary> The <see cref="DataSet"/> that is associated with this DataSource. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		public DataSet DataSet
		{
			get { return _dataSet; }
			set
			{
				if (_dataSet != value)
				{
					if (_dataSet != null)
					{
						_dataSet.RemoveSource(this);
						_dataSet.Disposed -= new EventHandler(DataSetDisposed);
					}
					_dataSet = value;
					if (_dataSet != null)
					{
						_dataSet.AddSource(this);
						_dataSet.Disposed += new EventHandler(DataSetDisposed);
					}
					DataSetChanged();
				}
			}
		}
		
		private void DataSetDisposed(object sender, EventArgs args)
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

