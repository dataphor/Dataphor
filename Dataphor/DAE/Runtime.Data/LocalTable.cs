/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.Collections;
	using System.Diagnostics;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public class LocalTable : Table
    {
		public LocalTable(TableNode ATableNode, ServerProcess AProcess, TableValue ATableValue) : base(ATableNode, AProcess)
		{
			FNativeTable = (NativeTable)ATableValue.AsNative;
			FKey = new Schema.Order(FNativeTable.TableVar.FindClusteringKey(), AProcess.Plan);
		}
		
		public LocalTable(TableNode ATableNode, ServerProcess AProcess) : base(ATableNode, AProcess)
		{
			FNativeTable = new NativeTable(AProcess, ATableNode.TableVar);
			FKey = new Schema.Order(ATableNode.TableVar.FindClusteringKey(), AProcess.Plan);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			Close();
			if (FNativeTable != null)
			{
				FNativeTable.Drop(Process);
				FNativeTable = null;
			}
			base.Dispose(ADisposing);
		}
		
		public new TableNode Node { get { return (TableNode)FNode; } }
		
		protected internal NativeTable FNativeTable;
		private Schema.Order FKey;
		private Scan FScan;

		protected override void InternalOpen()
		{
			if (FNativeTable.ClusteredIndex.Key.Equivalent(FKey))
				FScan = new Scan(Process, FNativeTable, FNativeTable.ClusteredIndex, ScanDirection.Forward, null, null);
			else
				FScan = new Scan(Process, FNativeTable, FNativeTable.NonClusteredIndexes[FKey], ScanDirection.Forward, null, null);
			FScan.Open();
		}
		
		protected override void InternalClose()
		{
			FScan.Dispose();
			FScan = null;
		}
		
		protected override void InternalReset()
		{
			FScan.Reset();
		}
		
		protected override void InternalSelect(Row ARow)
		{
			FScan.GetRow(ARow);
		}
		
		protected override void InternalFirst()
		{
			FScan.First();
		}
		
		protected override bool InternalPrior()
		{
			return FScan.Prior();
		}
		
		protected override bool InternalNext()
		{
			return FScan.Next();
		}
		
		protected override void InternalLast()
		{
			FScan.Last();
		}
		
		protected override bool InternalBOF()
		{
			return FScan.BOF();
		}
		
		protected override bool InternalEOF()
		{
			return FScan.EOF();
		}

		// Bookmarkable

		protected override Row InternalGetBookmark()
		{
			return FScan.GetKey();
		}

		protected override bool InternalGotoBookmark(Row ABookmark, bool AForward)
		{
			return FScan.FindKey(ABookmark);
		}
        
		protected override int InternalCompareBookmarks(Row ABookmark1, Row ABookmark2)
		{
			return FScan.CompareKeys(ABookmark1, ABookmark2);
		}

		// Searchable

		protected override Schema.Order InternalGetOrder()
		{
			return FKey;
		}
		
		protected override Row InternalGetKey()
		{
			return FScan.GetKey();
		}

		protected override bool InternalFindKey(Row AKey, bool AForward)
		{
			return FScan.FindKey(AKey);
		}
		
		protected override void InternalFindNearest(Row AKey)
		{
			FScan.FindNearest(AKey);
		}
		
		protected override bool InternalRefresh(Row AKey)
		{
			return FScan.FindNearest(AKey);
		}

		protected override void InternalInsert(Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			FNativeTable.Insert(Process, ANewRow);
		}
		
		protected override void InternalUpdate(Row ARow, BitArray AValueFlags, bool AUnchecked)
		{
			FNativeTable.Update(Process, Select(), ARow);
		}
		
		protected override void InternalDelete(bool AUnchecked)
		{
			FNativeTable.Delete(Process, Select());
		}
    }
}