/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

    public class LocalTable : Table
    {
		public LocalTable(TableNode tableNode, Program program, TableValue tableValue) : base(tableNode, program)
		{
			_nativeTable = (NativeTable)tableValue.AsNative;
			_key = program.OrderFromKey(program.FindClusteringKey(_nativeTable.TableVar));
		}
		
		public LocalTable(TableNode tableNode, Program program) : base(tableNode, program)
		{
			_nativeTable = new NativeTable(program.ValueManager, tableNode.TableVar);
			_key = program.OrderFromKey(program.FindClusteringKey(tableNode.TableVar));
		}
		
		protected override void Dispose(bool disposing)
		{
			Close();
			if (_nativeTable != null)
			{
				_nativeTable.Drop(Program.ValueManager);
				_nativeTable = null;
			}
		}
		
		public new TableNode Node { get { return (TableNode)_node; } }
		
		protected internal NativeTable _nativeTable;
		private Schema.Order _key;
		private Scan _scan;

		protected override void InternalOpen()
		{
			if (_nativeTable.ClusteredIndex.Key.Equivalent(_key))
				_scan = new Scan(Manager, _nativeTable, _nativeTable.ClusteredIndex, ScanDirection.Forward, null, null);
			else
				_scan = new Scan(Manager, _nativeTable, _nativeTable.NonClusteredIndexes[_key], ScanDirection.Forward, null, null);
			_scan.Open();
		}
		
		protected override void InternalClose()
		{
			_scan.Dispose();
			_scan = null;
		}
		
		protected override void InternalReset()
		{
			_scan.Reset();
		}
		
		protected override void InternalSelect(IRow row)
		{
			_scan.GetRow(row);
		}
		
		protected override void InternalFirst()
		{
			_scan.First();
		}
		
		protected override bool InternalPrior()
		{
			return _scan.Prior();
		}
		
		protected override bool InternalNext()
		{
			return _scan.Next();
		}
		
		protected override void InternalLast()
		{
			_scan.Last();
		}
		
		protected override bool InternalBOF()
		{
			return _scan.BOF();
		}
		
		protected override bool InternalEOF()
		{
			return _scan.EOF();
		}

		// Bookmarkable

		protected override IRow InternalGetBookmark()
		{
			return _scan.GetKey();
		}

		protected override bool InternalGotoBookmark(IRow bookmark, bool forward)
		{
			return _scan.FindKey(bookmark);
		}
        
		protected override int InternalCompareBookmarks(IRow bookmark1, IRow bookmark2)
		{
			return _scan.CompareKeys(bookmark1, bookmark2);
		}

		// Searchable

		protected override Schema.Order InternalGetOrder()
		{
			return _key;
		}
		
		protected override IRow InternalGetKey()
		{
			return _scan.GetKey();
		}

		protected override bool InternalFindKey(IRow key, bool forward)
		{
			return _scan.FindKey(key);
		}
		
		protected override void InternalFindNearest(IRow key)
		{
			_scan.FindNearest(key);
		}
		
		protected override bool InternalRefresh(IRow key)
		{
			return _scan.FindNearest(key);
		}

		protected override void InternalInsert(IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			_nativeTable.Insert(Manager, newRow);
		}
		
		protected override void InternalUpdate(IRow row, BitArray valueFlags, bool uncheckedValue)
		{
			_nativeTable.Update(Manager, Select(), row);
		}
		
		protected override void InternalDelete(bool uncheckedValue)
		{
			_nativeTable.Delete(Manager, Select());
		}
    }
}