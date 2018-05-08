/*
	Dataphor
	© Copyright 2000-2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class NativeMap : System.Object
	{
		public NativeMap(IValueManager manager, Schema.TableVar tableVar, Schema.Key key, Dictionary<String, Schema.Sort> equalitySorts) : base()
		{
			TableVar = tableVar;
			Key = key;
			EqualitySorts = equalitySorts;
			Create(manager);
		}
		
		public Schema.TableVar TableVar;

		public Schema.Key Key;

		public Dictionary<String, Schema.Sort> EqualitySorts;
		
		public Schema.ITableType TableType;
		
		public Schema.IRowType RowType;

		public NativeRowMap Index;
		
		private void Create(IValueManager manager)
		{
			TableType = TableVar.DataType;
			RowType = TableType.RowType;

			Schema.RowType keyRowType;
			Schema.RowType dataRowType;

			// Create the map index required to store data as described by the given table variable
			keyRowType = new Schema.RowType(Key.Columns);
			dataRowType = new Schema.RowType();
			foreach (Schema.Column column in TableVar.DataType.Columns)
				dataRowType.Columns.Add(new Schema.Column(column.Name, column.DataType));

			Index =
				new NativeRowMap
				(
					Key,
					EqualitySorts,
					keyRowType,
					dataRowType
				);
		}
		
		public void Drop(IValueManager manager)
		{
			Index.Drop(manager);
		}
		
		/// <summary>Inserts the given row into the index of the table value.</summary>
		/// <param name="row">The given row must conform to the structure of the table value.</param>
		public void Insert(IValueManager manager, IRow row)
		{
			int columnIndex;
			Row key = new Row(manager, Index.KeyRowType);
			try
			{
				Row data = new Row(manager, Index.DataRowType);
				try
				{
					key.ValuesOwned = false;
					data.ValuesOwned = false;

					for (int localIndex = 0; localIndex < key.DataType.Columns.Count; localIndex++)
					{
						#if USEINTERNALID
						if (key.DataType.Columns[localIndex].Name == InternalIDColumnName)
							key[localIndex] = internalID;
						else
						#endif
						{
							columnIndex = row.DataType.Columns.GetIndexOfColumn(key.DataType.Columns[localIndex].Name);
							if (row.HasValue(columnIndex))
								key[localIndex] = row.GetNativeValue(columnIndex);
							else
								key.ClearValue(localIndex);
						}
					}

					for (int localIndex = 0; localIndex < data.DataType.Columns.Count; localIndex++)
					{
						#if USEINTERNALID
						if (data.DataType.Columns[localIndex].Name == InternalIDColumnName)
							data[localIndex] = internalID;
						else
						#endif
						{
							columnIndex = row.DataType.Columns.GetIndexOfColumn(data.DataType.Columns[localIndex].Name);
							if (row.HasValue(columnIndex))
								data[localIndex] = row.GetNativeValue(columnIndex);
							else
								data.ClearValue(localIndex);
						}
					}

					Index.Insert(manager, (NativeRow)key.AsNative, (NativeRow)data.AsNative);
				}
				finally
				{
					data.Dispose();
				}
			}
			finally
			{
				key.Dispose();
			}
		}
		
		private Row GetIndexData(IValueManager manager, Schema.IRowType rowType, IRow[] sourceRows)
		{
			Row row = new Row(manager, rowType);
			try
			{
				int columnIndex;
				bool found;
				for (int index = 0; index < row.DataType.Columns.Count; index++)
				{
					found = false;
					foreach (Row sourceRow in sourceRows)
					{
						columnIndex = sourceRow.DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
						if (columnIndex >= 0)
						{
							if (sourceRow.HasValue(columnIndex))
								row[index] = sourceRow.GetNativeValue(columnIndex);
							else
								row.ClearValue(index);
							found = true;
							break;
						}
					}
					if (found)
						continue;
					
					throw new RuntimeException(RuntimeException.Codes.UnableToConstructIndexKey);
				}
				return row;
			}
			catch
			{
				row.Dispose();
				throw;
			}
		}
		
		private bool GetIsIndexAffected(Schema.IRowType rowType, IRow row)
		{
			foreach (Schema.Column column in rowType.Columns)
				if (row.DataType.Columns.ContainsName(column.Name))
					return true;
			return false;
		}
		
		public bool HasRow(IValueManager manager, IRow row)
		{
			IRow key = GetIndexData(manager, Index.KeyRowType, new IRow[]{row});
			try
			{
				return Index.ContainsKey(manager, (NativeRow)key.AsNative);
			}
			finally
			{
				key.Dispose();
			}
		}

		public void Update(IValueManager manager, IRow oldRow, IRow newRow)
		{
			Row oldKey = GetIndexData(manager, Index.KeyRowType, new IRow[] { oldRow });
			try
			{
				Row newKey = GetIndexData(manager, Index.KeyRowType, new IRow[] { newRow });
				try
				{
					Row newData = GetIndexData(manager, Index.DataRowType, new IRow[] { newRow });
					try
					{
						Index.Update(manager, (NativeRow)oldKey.AsNative, (NativeRow)newKey.AsNative, (NativeRow)newData.AsNative);
						newKey.ValuesOwned = false;
						newData.ValuesOwned = false;
					}
					finally
					{
						newData.Dispose();
					}
				}
				finally
				{
					newKey.Dispose();
				}
			}
			finally
			{
				oldKey.Dispose();
			}
		}
		
		public void Delete(IValueManager manager, IRow row)
		{
			// Delete the row from all indexes
			Row key = GetIndexData(manager, Index.KeyRowType, new IRow[]{row});
			try
			{
				Index.Delete(manager, (NativeRow)key.AsNative);
			}
			finally
			{	
				key.Dispose();
			}
		}
		
		public void Truncate(IValueManager manager)
		{
			Drop(manager);
			Create(manager);
		}
	}

	public class NativeMaps : List
	{
		public NativeMaps() : base(){}
		
		public new NativeMap this[int index]
		{
			get { lock (this) { return (NativeMap)base[index]; } } 
			set { lock (this) { base[index] = value; } }
		}

		public int IndexOf(Schema.TableVar tableVar)
		{
			lock (this)
			{
				for (int index = 0; index < Count; index++)
					if (this[index].TableVar == tableVar)
						return index;
				return -1;
			}
		}
		
		public bool Contains(Schema.TableVar tableVar)
		{
			return IndexOf(tableVar) >= 0;
		}
		
		public NativeMap this[Schema.TableVar tableVar]
		{
			get
			{
				lock (this)
				{
					int index = IndexOf(tableVar);
					if (index < 0)
						throw new RuntimeException(RuntimeException.Codes.NativeTableNotFound, tableVar.DisplayName);
					return this[index];
				}
			}
		}
	}
}
