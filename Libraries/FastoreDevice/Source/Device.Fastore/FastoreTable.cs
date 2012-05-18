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
	//TODO: FIX NASTY ASSUMPTIONS! (First column is ID column -- This is clearly wrong, but I'm just getting things up and running)
	public class FastoreTable : System.Object
	{
		//Tied Directly to device for time being...
		public FastoreTable(IValueManager manager, Schema.TableVar tableVar, FastoreDevice device)
		{
			TableVar = tableVar;
			Device = device;

			_keys = new Schema.Keys();
			_orders = new Schema.Orders();
			EnsureColumns();
		}

		public FastoreDevice Device;
		public Schema.TableVar TableVar;

		private Keys _keys;
		public Keys Keys { get { return _keys; } }

		private Orders _orders;
		public Orders Orders { get { return _orders; } }

		private int[] _columns;
		public int[] Columns { get { return _columns; } }


		protected string MapTypeNames(string tname)
		{
			string name = "";
			switch (tname)
			{
				case "System.String":
					name = "WString";
					break;
				case "System.Integer":
					name = "Int";
					break;
				case "System.Boolean":
					name = "Bool";
					break;
				case "System.Long":
					name = "Long";
					break;
				default:
					break;
			}

			return name;
		}

		protected void EnsureColumns()
		{
			List<int> columnIds = new List<int>();

			for (int i = 0; i < TableVar.DataType.Columns.Count; i++)
			{
				var col = TableVar.DataType.Columns[i];
				ColumnDef def = new ColumnDef();

				columnIds.Add(col.ID);

				def.ColumnID = col.ID;
				def.Type = MapTypeNames(col.DataType.Name);
				def.Name = col.Description;
				def.IDType = "Int";

				//Unique? Required?
				if (i == 1)
				{
					def.IsUnique = true;

				}
				else
				{
					def.IsUnique = false;
				}

				if (!Device.Database.ExistsColumn(col.ID))
				{
					Device.Database.CreateColumn(def);
				}
			}

			_columns = columnIds.ToArray();
		}

		public void Insert(IValueManager manager, Row row, Session session)
		{
			if (row.HasValue(0))
			{
				object[] items = ((NativeRow)row.AsNative).Values;

				session.Include(_columns, items[0], items);
			}
		}

		public void Update(IValueManager manager, Row oldrow, Row newrow, Session session)
		{
			if (oldrow.HasValue(0))
			{
				var id = oldrow[0];
				for (int i = 0; i < _columns.Length; i++)
				{
					session.Exclude(_columns, id);
				}
			}

			if (newrow.HasValue(0))
			{
				object[] items = ((NativeRow)newrow.AsNative).Values;
				session.Include(_columns, items[0], items);
			}
		}

		public void Delete(IValueManager manager, Row row, Session session)
		{
			//If no value at zero, no ID (Based on our wrong assumptions)
			if (row.HasValue(0))
			{
				session.Exclude(_columns, row[0]);
			}
		}

		public void Drop(IValueManager manager)
		{
			foreach (var col in _columns)
			{
				if (Device.Database.ExistsColumn(col))
				{
					Device.Database.DeleteColumn(col);
				}
			}
		}
	}
}
