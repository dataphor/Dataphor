/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;
using Alphora.Fastore.Client;


namespace Alphora.Dataphor.DAE.Device.Fastore
{
	//This will have to be a buffered read of the FastoreStorage engine
	public class FastoreScan : Table
	{
		public FastoreScan(Program program, Session session, TableNode node)
			: base(node, program)
		{
			_session = session;
		}

		public FastoreTable FastoreTable;

		private Session _session;

		private DataSet _set;
		private int _currow = -1;

		public Orders Orders = new Orders();

		private Alphora.Fastore.Client.Order[] OrdersToFastoreOrders()
		{
			return new Alphora.Fastore.Client.Order[0];
		}

		//TODO: set parameters before opening. Start and end range, Orders, etc.
		protected override void InternalOpen()
		{
			_set = _session.GetRange(FastoreTable.Columns, OrdersToFastoreOrders(), new Range[0]);
		}

		protected override void InternalClose()
		{
			_set = null;
		}

		protected override void InternalSelect(Row row)
		{
			object[] managedRow = _set[_currow];

			NativeRow nRow = new NativeRow(FastoreTable.TableVar.Columns.Count);
			for (int i = 0; i < FastoreTable.TableVar.Columns.Count; i++)
			{
				nRow.Values[i] = managedRow[i];
			}

			Row localRow = new Row(Manager, FastoreTable.TableVar.DataType.RowType, nRow);

			localRow.CopyTo(row);
		}

		protected override bool InternalNext()
		{
			_currow++;

			return _currow < _set.Count;
		}

		protected override bool InternalBOF()
		{
			return false;
		}

		protected override bool InternalEOF()
		{
			return _currow == _set.Count;
		}
	}
}
