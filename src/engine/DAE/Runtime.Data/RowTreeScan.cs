/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public enum ScanDirection {Forward, Backward}
	    
	///	<remarks>
	/// Provides a "scan" on the given NativeTable using the given access path (NativeRowTree).
	/// A scan implements the Navigable, BackwardsNavigable, and Searchable CursorCapabilities in
	/// the same way as a Table value is expected to. A scan is an active window onto the given
	/// table value, changes made to the underlying data are immediately reflected in the scan.
	/// Every public method that reads the tree or the scan's position holds the table's read lock for the length of the call
	/// (see NativeTable). The tree's row-moved and row-deleted handlers run on a writer's thread inside the table's write lock,
	/// so they, and the methods' bodies when they call each other, use the unlocked Internal methods.
	/// </remarks>
	public class Scan : Disposable
	{
		/// <remarks> Scan range keys are inclusive. </remarks>		
		public Scan(IValueManager manager, NativeTable table, NativeRowTree accessPath, ScanDirection direction, IRow firstKey, IRow lastKey)
		{
			_manager = manager;
			_table = table;
			_accessPath = accessPath;
			_direction = direction;
			_firstKey = firstKey;
			_lastKey = lastKey;
		}

		private IValueManager _manager;
		private NativeTable _table;
		private NativeRowTree _accessPath;
		private ScanDirection _direction;
		private IRow _firstKey;
		private IRow _lastKey;
		private bool _bOF;
		private bool _eOF;
		private RowTreeNode _indexNode;
		private int _entryNumber;

		private void SetIndexNode(RowTreeNode indexNode)
		{
			// This implements crabbing, because the new node is locked before the old node lock is released
			if (_indexNode != null)
				_indexNode.Dispose();
			_indexNode = indexNode;
		}
		
		public void Open()
		{
			Active = true;
		}
		
		public void Close()
		{
			Active = false;
		}
		
		private bool _active;
		public bool Active
		{
			get { return _active; }
			set
			{
				// Locked: opening subscribes to the tree and positions the scan, closing clears the position, and a writer's
				// handler must not run on this scan in between.
				_table.EnterReadLock();
				try
				{
					if (value && !_active)
					{
						_active = true;
						InternalOpen();
					}
					else if (!value && _active)
					{
						InternalClose();
						_active = false;
					}
				}
				finally
				{
					_table.ExitReadLock();
				}
			}
		}
		
		private void CheckActive()
		{
			if (!Active)
				throw new ScanException(ScanException.Codes.ScanInactive);
		}
		
		private void CheckNotCrack()
		{
			if (_bOF || _eOF)
				throw new ScanException(ScanException.Codes.NoActiveRow);
		}
		
		private void InternalOpen()
		{
			// The access path was read from the table before this call took the table's lock.
			_accessPath = _table.CurrentIndex(_accessPath);
			_accessPath.OnRowsMoved += new NativeRowTreeRowsMovedHandler(RowTreeRowsMoved);
			_accessPath.OnRowDeleted += new NativeRowTreeRowDeletedHandler(RowTreeRowDeleted);
			InternalFirst();
		}

		private void InternalClose()
		{
			_accessPath.OnRowDeleted -= new NativeRowTreeRowDeletedHandler(RowTreeRowDeleted);
			_accessPath.OnRowsMoved -= new NativeRowTreeRowsMovedHandler(RowTreeRowsMoved);
			SetIndexNode(null);
		}

		/// <summary>
		/// Moves the scan to the table's current tree before it positions from scratch. A Truncate (the catalog device
		/// repopulates its tables with one on every read) drops the scan's tree and builds new ones; until the scan repositions,
		/// it keeps reading the dropped tree's rows.
		/// </summary>
		private void FollowTruncate()
		{
			NativeRowTree current = _table.CurrentIndex(_accessPath);
			if (current != _accessPath)
			{
				InternalClose();
				_accessPath = current;
				_accessPath.OnRowsMoved += new NativeRowTreeRowsMovedHandler(RowTreeRowsMoved);
				_accessPath.OnRowDeleted += new NativeRowTreeRowDeletedHandler(RowTreeRowDeleted);
			}
		}
		
		public void Reset()
		{
			_table.EnterReadLock();
			try
			{
				#if SAFETABLES
				CheckActive();
				#endif
				SetIndexNode(null);
				InternalFirst();
			}
			finally
			{
				_table.ExitReadLock();
			}
		}
		
		protected override void Dispose(bool disposing)
		{
			Close();
			base.Dispose(disposing);
		}
		
		private void UpdateScanPointer()
		{
			_entryNumber += _direction == ScanDirection.Forward ? -1 : 1;
			InternalNext();
		}
		
		private void RowTreeRowsMoved(NativeRowTree rowTree, NativeRowTreeNode oldNode, int oldEntryNumberMin, int oldEntryNumberMax, NativeRowTreeNode newNode, int entryNumberDelta)
		{
			if ((_indexNode.Node == oldNode) && (_entryNumber >= oldEntryNumberMin) && (_entryNumber <= oldEntryNumberMax))
			{
				if (oldNode != newNode)
					SetIndexNode(new RowTreeNode(_manager, _indexNode.Tree, newNode, LockMode.Shared));
					
				_entryNumber += entryNumberDelta;
				UpdateScanPointer();
			}
		}
		
		private void RowTreeRowDeleted(NativeRowTree rowTree, NativeRowTreeNode node, int entryNumber)
		{
			if ((_indexNode.Node == node) && (_entryNumber == entryNumber))
				UpdateScanPointer();
		}
		
		private bool FindIndexKey(Schema.IRowType keyRowType, NativeRow key, out RowTreeNode indexNode, out int entryNumber)
		{
			RowTreeSearchPath searchPath = new RowTreeSearchPath();
			try
			{
				bool result = _accessPath.FindKey(_manager, keyRowType, key, searchPath, out entryNumber);
				indexNode = searchPath.DisownAt(searchPath.Count - 1);
				return result;
			}
			finally
			{
				searchPath.Dispose();
			}
		}

		private bool NextNode()
		{
			while (true)
			{
				if (_indexNode.Node.NextNode == null)
					return false;
				else
				{
					SetIndexNode(new RowTreeNode(_manager, _indexNode.Tree, _indexNode.Node.NextNode, LockMode.Shared));
					if (_indexNode.Node.EntryCount > 0)
					{
						_entryNumber = 0;
						return true;
					}
				}
			}
		}
		
		private bool PriorNode()
		{
			while (true)
			{
				if (_indexNode.Node.PriorNode == null)
					return false;
				else
				{
					SetIndexNode(new RowTreeNode(_manager, _indexNode.Tree, _indexNode.Node.PriorNode, LockMode.Shared));
					if (_indexNode.Node.EntryCount > 0)
					{
						_entryNumber = _indexNode.Node.EntryCount - 1;
						return true;
					}
				}
			}
		}
		
		public void First()
		{
			_table.EnterReadLock();
			try
			{
				InternalFirst();
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private void InternalFirst()
		{
			#if SAFETABLES
			CheckActive();
			#endif

			FollowTruncate();
			_bOF = true;
			_eOF = false;
			bool result = false;
			if (_firstKey != null)
			{
				RowTreeNode indexNode;
				result = FindIndexKey(_firstKey.DataType, (NativeRow)_firstKey.AsNative, out indexNode, out _entryNumber);
				SetIndexNode(indexNode);
			}
			else
			{
				if (_direction == ScanDirection.Forward)
				{
					SetIndexNode(new RowTreeNode(_manager, _accessPath, _accessPath.Head, LockMode.Shared));
					_entryNumber = 0;
				}
				else
				{
					SetIndexNode(new RowTreeNode(_manager, _accessPath, _accessPath.Tail, LockMode.Shared));
					_entryNumber = _indexNode.Node.EntryCount - 1;
				}
			}
			
			if (!result)
			{
				// Determine FEOF
				RowTreeNode saveIndexNode = new RowTreeNode(_manager, _indexNode.Tree, _indexNode.Node, LockMode.Shared);
				int saveEntryNumber = _entryNumber;
				InternalNext();
				_bOF = true;
				SetIndexNode(saveIndexNode);
				_entryNumber = saveEntryNumber;
			}
		}		
		
		public bool Prior()
		{
			_table.EnterReadLock();
			try
			{
				return InternalPrior();
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private bool InternalPrior()
		{
			#if SAFETABLES
			CheckActive();
			#endif

			bool eOF = _eOF;
			
			if (_direction == ScanDirection.Forward)
			{
				if (_eOF)
					_eOF = false;
				else
					_entryNumber--;
					
				if (((_entryNumber < 0) || (_indexNode.Node.EntryCount == 0)) && !PriorNode())
				{
					_entryNumber = 0;
					_bOF = true;
				}

				// Make sure that the entry is >= FFirstKey
				if (!_bOF && (_firstKey != null) && (_accessPath.Compare(_manager, _indexNode.Tree.KeyRowType, _indexNode.Node.Keys[_entryNumber], _firstKey.DataType, (NativeRow)_firstKey.AsNative) < 0))
					InternalFirst();
			}
			else
			{
				if (_eOF)
					_eOF = false;
				else
					_entryNumber++;

				if ((_entryNumber >= _indexNode.Node.EntryCount) && !NextNode())
				{
					_entryNumber = _indexNode.Node.EntryCount - 1;
					_entryNumber = _entryNumber < 0 ? 0 : _entryNumber;
					_bOF = true;
				}
					
				// Make sure that the entry is <= FFirstKey
				if (!_bOF && (_firstKey != null) && (_accessPath.Compare(_manager, _indexNode.Tree.KeyRowType, _indexNode.Node.Keys[_entryNumber], _firstKey.DataType, (NativeRow)_firstKey.AsNative) > 0))
					InternalFirst();
			}
			
			// Make sure that if the scan is empty, the EOF flag is still true
			if (eOF && _bOF)
				_eOF = true;
				
			return !_bOF;
		}
		
		public bool Next()
		{
			_table.EnterReadLock();
			try
			{
				return InternalNext();
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private bool InternalNext()
		{
			#if SAFETABLES
			CheckActive();
			#endif

			bool bOF = _bOF;

			if (_direction == ScanDirection.Forward)
			{
				if (_bOF)
					_bOF = false;
				else
					_entryNumber++;

				if ((_entryNumber >= _indexNode.Node.EntryCount) && !NextNode())
				{
					_entryNumber = _indexNode.Node.EntryCount - 1;
					_entryNumber = _entryNumber < 0 ? 0 : _entryNumber;
					_eOF = true;
				}
					
				// Make sure that the entry is <= LLastKey
				if (!_eOF && (_lastKey != null) && (_accessPath.Compare(_manager, _indexNode.Tree.KeyRowType, _indexNode.Node.Keys[_entryNumber], _lastKey.DataType, (NativeRow)_lastKey.AsNative) > 0))
					InternalLast();
			}
			else
			{
				if (_bOF)
					_bOF = false;
				else
					_entryNumber--;
					
				if (((_entryNumber < 0) || (_indexNode.Node.EntryCount == 0)) && !PriorNode())
				{
					_entryNumber = 0;
					_eOF = true;
				}
				
				// Make sure that the entry is >= LLastKey
				if (!_eOF && (_lastKey != null) && (_accessPath.Compare(_manager, _indexNode.Tree.KeyRowType, _indexNode.Node.Keys[_entryNumber], _lastKey.DataType, (NativeRow)_lastKey.AsNative) < 0))
					InternalLast();
			}
				
			// Make sure that if the scan is empty, the BOF flag is still true
			if (bOF && _eOF)
				_bOF = true;
				
			return !_eOF;
		}
		
		public void Last()
		{
			_table.EnterReadLock();
			try
			{
				InternalLast();
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private void InternalLast()
		{
			#if SAFETABLES
			CheckActive();
			#endif

			FollowTruncate();
			_eOF = true;
			_bOF = false;
			bool result = false;
			if (_lastKey != null)
			{
				RowTreeNode indexNode;
				result = FindIndexKey(_lastKey.DataType, (NativeRow)_lastKey.AsNative, out indexNode, out _entryNumber);
				SetIndexNode(indexNode);
			}
			else
			{
				if (_direction == ScanDirection.Forward)
				{
					SetIndexNode(new RowTreeNode(_manager, _accessPath, _accessPath.Tail, LockMode.Shared));
					_entryNumber = _indexNode.Node.EntryCount - 1;
				}
				else
				{
					SetIndexNode(new RowTreeNode(_manager, _accessPath, _accessPath.Head, LockMode.Shared));
					_entryNumber = 0;
				}
			}
			
			if (!result)
			{
				// Determine FBOF
				RowTreeNode saveIndexNode = new RowTreeNode(_manager, _indexNode.Tree, _indexNode.Node, LockMode.Shared);
				int saveEntryNumber = _entryNumber;
				InternalPrior();
				_eOF = true;
				if (_indexNode != null)
					_indexNode.Dispose();
				_indexNode = saveIndexNode;
				_entryNumber = saveEntryNumber;
			}
		}
		
		public bool BOF()
		{
			_table.EnterReadLock();
			try
			{
				#if SAFETABLES
				CheckActive();
				#endif
				return _bOF;
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		public bool EOF()
		{
			_table.EnterReadLock();
			try
			{
				#if SAFETABLES
				CheckActive();
				#endif
				return _eOF;
			}
			finally
			{
				_table.ExitReadLock();
			}
		}
		
		public IRow GetRow()
		{
			Row row = new Row(_manager, _table.RowType);
			GetRow(row);
			return row;
		}
		
		private bool IsSubset(IRow row, NativeRowTree index)
		{
			foreach (Schema.Column column in row.DataType.Columns)
				if (!index.KeyRowType.Columns.ContainsName(column.Name) && !index.DataRowType.Columns.Contains(column.Name))
					return false;
			return true;
		}
		
		public void GetRow(IRow row)
		{
			_table.EnterReadLock();
			try
			{
				InternalGetRow(row);
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private void InternalGetRow(IRow row)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			CheckNotCrack();
			
			if ((_accessPath.IsClustered) || IsSubset(row, _accessPath))
			{
				Row localRow = new Row(_manager, _accessPath.KeyRowType, _indexNode.Node.Keys[_entryNumber]);
				try
				{
					localRow.CopyTo(row);
				}
				finally
				{
					localRow.Dispose();
				}
				
				localRow = new Row(_manager, _accessPath.DataRowType, _indexNode.DataNode.Rows[_entryNumber]);
				try
				{ 
					localRow.CopyTo(row); 
				}
				finally
				{
					localRow.Dispose();
				}
			}
			else
			{
				// NOTE: after another session's Truncate, a scan that has only moved with Next or Prior is still on the dropped
				// tree (see FollowTruncate) but looks its row up here in the new clustered tree, so a row the repopulate left out
				// raises ClusteredRowNotFound. Today only catalog tables read in a non-clustered order with a cursor held open
				// meet it; if it is ever reported, make Next and Prior follow the truncate by seeking the current key.
				using (RowTreeSearchPath searchPath = new RowTreeSearchPath())
				{
					int entryNumber;
					bool result = _table.ClusteredIndex.FindKey(_manager, _table.ClusteredIndex.KeyRowType, _indexNode.DataNode.Rows[_entryNumber], searchPath, out entryNumber);
					if (result)
					{
						Row localRow = new Row(_manager, _table.ClusteredIndex.KeyRowType, searchPath.DataNode.Node.Keys[entryNumber]);
						try
						{
							localRow.CopyTo(row);
						}
						finally
						{
							localRow.Dispose();
						}

						localRow = new Row(_manager, _table.ClusteredIndex.DataRowType, searchPath.DataNode.DataNode.Rows[entryNumber]);
						try
						{ 
							localRow.CopyTo(row); 
						}
						finally
						{
							localRow.Dispose();
						}
					}
					else
						throw new ScanException(ScanException.Codes.ClusteredRowNotFound);
				}
			}
		}
		
		public IRow GetKey()
		{
			_table.EnterReadLock();
			try
			{
				#if SAFETABLES
				CheckActive();
				#endif
				CheckNotCrack();
				return new Row(_manager, _accessPath.KeyRowType, _indexNode.Node.Keys[_entryNumber]);
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		public IRow GetData()
		{
			_table.EnterReadLock();
			try
			{
				#if SAFETABLES
				CheckActive();
				#endif
				CheckNotCrack();
				return new Row(_manager, _accessPath.DataRowType, _indexNode.Node.Keys[_entryNumber]);
			}
			finally
			{
				_table.ExitReadLock();
			}
		}
		
		private IRow EnsureKeyRow(IRow key)
		{
			IRow localKey = key;
			bool isKeyRow = key.DataType.Columns.Count <= _accessPath.KeyRowType.Columns.Count;
			for (int index = 0; index < _accessPath.KeyRowType.Columns.Count; index++)
			{
				if (index >= key.DataType.Columns.Count)
					break;
				else
				{
					if (!Schema.Object.NamesEqual(key.DataType.Columns[index].Name, _accessPath.KeyRowType.Columns[index].Name))
					{
						isKeyRow = false;
						break;
					}
				}
			}
			
			if (!isKeyRow)
			{
				localKey = new Row(_manager, _accessPath.KeyRowType);
				key.CopyTo(localKey);
			}

			return localKey;
		}
		
		public bool FindKey(IRow key)
		{
			_table.EnterReadLock();
			try
			{
				return InternalFindKey(key);
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private bool InternalFindKey(IRow key)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			FollowTruncate();
			int entryNumber;
			RowTreeNode indexNode;
			IRow localKey = EnsureKeyRow(key);
			try
			{
				if (_firstKey != null)
				{
					int compareResult = _accessPath.Compare(_manager, _firstKey.DataType, (NativeRow)_firstKey.AsNative, localKey.DataType, (NativeRow)localKey.AsNative);
					if (((_direction == ScanDirection.Forward) && (compareResult > 0)) || ((_direction == ScanDirection.Backward) && (compareResult < 0)))
						return false;
				}
				
				if (_lastKey != null)
				{
					int compareResult = _accessPath.Compare(_manager, _lastKey.DataType, (NativeRow)_lastKey.AsNative, localKey.DataType, (NativeRow)localKey.AsNative);
					if (((_direction == ScanDirection.Forward) && (compareResult < 0)) || ((_direction == ScanDirection.Backward) && (compareResult < 0)))
						return false;
				}

				bool result = FindIndexKey(localKey.DataType, (NativeRow)localKey.AsNative, out indexNode, out entryNumber);
				if (result)
				{
					SetIndexNode(indexNode);
					_entryNumber = entryNumber;
					_bOF = false;
					_eOF = false;
				}
				else
					indexNode.Dispose();
				return result;
			}
			finally
			{
				if (!ReferenceEquals(key, localKey))
					localKey.Dispose();
			}
		}
		
		public bool FindNearest(IRow key)
		{
			_table.EnterReadLock();
			try
			{
				return InternalFindNearest(key);
			}
			finally
			{
				_table.ExitReadLock();
			}
		}

		private bool InternalFindNearest(IRow key)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			FollowTruncate();
			int entryNumber;
			RowTreeNode indexNode;
			IRow localKey = EnsureKeyRow(key);
			try
			{
				if (_firstKey != null)
				{
					int compareResult = _accessPath.Compare(_manager, _firstKey.DataType, (NativeRow)_firstKey.AsNative, localKey.DataType, (NativeRow)localKey.AsNative);
					if (((_direction == ScanDirection.Forward) && (compareResult > 0)) || ((_direction == ScanDirection.Backward) && (compareResult < 0)))
					{
						InternalFirst();
						InternalNext();
						return false;
					}
				}

				if (_lastKey != null)
				{
					int compareResult = _accessPath.Compare(_manager, _lastKey.DataType, (NativeRow)_lastKey.AsNative, localKey.DataType, (NativeRow)localKey.AsNative);
					if (((_direction == ScanDirection.Forward) && (compareResult < 0)) || ((_direction == ScanDirection.Backward) && (compareResult < 0)))
					{
						InternalLast();
						InternalPrior();
						return false;
					}
				}

				bool result = FindIndexKey(localKey.DataType, (NativeRow)localKey.AsNative, out indexNode, out entryNumber);
				SetIndexNode(indexNode);
				_entryNumber = entryNumber;

				if (_entryNumber >= _indexNode.Node.EntryCount)
				{
					InternalNext();
					if (_eOF)
					{
						InternalLast();
						InternalPrior();
					}
				}
				else
				{
					_bOF = false;
					_eOF = false;
				}

				return result;
			}
			finally
			{
				if (!ReferenceEquals(key, localKey))
					localKey.Dispose();
			}
		}

		/// <remarks>The keys passed to this function must be of the same row type as the key for the accesspath for the scan</remarks>
		// Not locked: it compares the given rows by the access path's order, which never changes, and reads no node.
		public int CompareKeys(IRow key1, IRow key2)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			return _accessPath.Compare(_manager, key1.DataType, (NativeRow)key1.AsNative, key2.DataType, (NativeRow)key2.AsNative);
		}
	}
}
