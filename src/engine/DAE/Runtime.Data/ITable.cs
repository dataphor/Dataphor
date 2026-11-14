/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	// TODO: Define separate interfaces for cursor capabilities?
	public interface ITable : IDataValue
	{
		Program Program { get; }
		new Schema.ITableType DataType { get; }
		TableNode Node { get; }
		CursorType CursorType { get; }
		CursorCapability Capabilities { get; }
		CursorIsolation Isolation { get; }
		bool Supports(CursorCapability capability);
		void CheckCapability(CursorCapability capability);
		void Open();
		void Close();
		bool Active { get; set; }
		void Reset();
		void Select(IRow row);
		IRow Select();
		bool Next();
		void Last();
		bool BOF();
		bool EOF();
		bool IsEmpty();
		bool Prior();
		void First();
		IRow GetBookmark();
		bool GotoBookmark(IRow bookmark, bool forward);
		bool GotoBookmark(IRow bookmark);
		int CompareBookmarks(IRow bookmark1, IRow bookmark2);
		Schema.Order Order { get; }
		IRow GetKey();
		bool FindKey(IRow row);
		bool FindKey(IRow row, bool forward);
		void FindNearest(IRow row);
		bool Refresh(IRow row);
		bool OptimisticRefresh(IRow row);
		int RowCount();
		void Insert(IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue);
		void Insert(IRow row);
		void Update(IRow row, BitArray valueFlags, bool uncheckedValue);
		void Update(IRow row);
		void Delete(bool uncheckedValue);
		void Delete();
		void Truncate();
	}
}
