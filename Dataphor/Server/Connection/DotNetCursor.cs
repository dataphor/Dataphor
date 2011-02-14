/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection
{
	public class DotNetCursor : SQLCursor
	{
		public DotNetCursor(DotNetCommand command, IDataReader cursor) : base(command)
		{
			_cursor = cursor;
			_record = (IDataRecord)cursor;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_cursor != null)
			{
                try
                {
                    _cursor.Dispose();
                }
                finally
                {
					_cursor = null;
					_record = null;
                }
			}
			base.Dispose(disposing);
		}
		
		protected IDataReader _cursor;
		protected IDataRecord _record;
		
		protected bool IsNull(object tempValue)
		{
			return (tempValue == null) || (tempValue == DBNull.Value);
		}
		
		protected override SQLTableSchema InternalGetSchema()
		{
			DataTable dataTable = _cursor.GetSchemaTable();
			SQLTableSchema schema = new SQLTableSchema();
			
			if (dataTable.Rows.Count > 0)
			{
				SQLIndex index = new SQLIndex("");
				index.IsUnique = true;
				
				bool containsIsHidden = dataTable.Columns.Contains("IsHidden");
				int columnIndex = 0;
				foreach (System.Data.DataRow row in dataTable.Rows)
				{
					string columnName = (string)row["ColumnName"];

					object tempValue = row["IsUnique"];
					bool isUnique = IsNull(tempValue) ? false : (bool)tempValue;

					tempValue = row["IsKey"];
					bool isKey = IsNull(tempValue) ? false : (bool)tempValue;
					
					if (isUnique || isKey)
						index.Columns.Add(new SQLIndexColumn(columnName));
					
					Type dataType = _cursor.GetFieldType(columnIndex);
					
					tempValue = row["ColumnSize"];
					int length = IsNull(tempValue) ? 0 : (int)tempValue;
					
					tempValue = row["NumericPrecision"];
					int precision = IsNull(tempValue) ? 0 : Convert.ToInt32(tempValue);
					
					tempValue = row["NumericScale"];
					int scale = IsNull(tempValue) ? 0 : Convert.ToInt32(tempValue);
					
					tempValue = row["IsLong"];
					bool isLong = IsNull(tempValue) ? false : (bool)tempValue;

					bool isHidden = false;
					if (containsIsHidden)
					{
						tempValue = row["IsHidden"];
						isHidden = IsNull(tempValue) ? false : (bool)tempValue;
					}

                    if (!isHidden)
    					schema.Columns.Add(new SQLColumn(columnName, new SQLDomain(dataType, length, precision, scale, isLong)));
					
					columnIndex++;
				}

				if (index.Columns.Count > 0)
					schema.Indexes.Add(index);
			}
			else
			{
				for (int index = 0; index < _cursor.FieldCount; index++)
					schema.Columns.Add(new SQLColumn(_cursor.GetName(index), new SQLDomain(_cursor.GetFieldType(index), 0, 0, 0, false)));
			}
			
			return schema;
		}
		
		protected override int InternalGetColumnCount()
		{
			return ((System.Data.Common.DbDataReader)_cursor).VisibleFieldCount;	// Visible field count hides internal fields like "rowstat" returned by server side cursors
		}

		protected override string InternalGetColumnName(int index)
		{
			return _cursor.GetName(index);
		}
		
		protected override bool InternalNext()
		{
			return _cursor.Read();
		}
		
		protected override object InternalGetColumnValue(int index)
		{
			return _record.GetValue(index);
		}
		
		protected override bool InternalIsNull(int index)
		{
			return _record.IsDBNull(index);
		}
		
		protected override bool InternalIsDeferred(int index)
		{
			switch (_record.GetDataTypeName(index).ToLower())
			{
				case "image": 
					return true;
				default: return false;
			}
		}
		
		protected override Stream InternalOpenDeferredStream(int index)
		{
			switch (_record.GetDataTypeName(index).ToLower())
			{
				case "image": 
					long length = _record.GetBytes(index, 0, null, 0, 0);
					if (length > Int32.MaxValue)
						throw new ConnectionException(ConnectionException.Codes.DeferredOverflow);
						
					byte[] data = new byte[(int)length];
					_record.GetBytes(index, 0, data, 0, data.Length);
					return new MemoryStream(data, 0, data.Length, true, true);	// Must use this overload or GetBuffer() will not be accessible

				default: throw new ConnectionException(ConnectionException.Codes.NonDeferredDataType, _record.GetDataTypeName(index));
			}
		}
	}
}
