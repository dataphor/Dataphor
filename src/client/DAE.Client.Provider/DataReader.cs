/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Data.SqlTypes;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	/// <summary> Data provider for the Dataphor Data Access Engine. </summary>
	/// <remarks>
	///		Can provide custom native type specification through the use of a
	///		meta-tag: <c>DAEDataReader.NativeType</c>.  The specified native
	///		type must support INullable.
	/// </remarks>
	public class DAEDataReader : DbDataReader, IEnumerable, IDataReader, IDisposable, IDataRecord
	{
		public const string MetaDataNamespace = "DAEDataReader";
		public const string NativeTypeTag = "NativeType";
		public const string ColumnSizeTag = "ColumnSize";
		public const string IsReadOnlyTag = "IsReadOnly";
		public const string IsAutoIncrementTag = "IsAutoIncrement";
		public const string IsLongTag = "IsLong";
		public const string NumericPrecisionTag = "NumericPrecision";
		public const string NumericScaleTag = "NumericScale";
		public const int DefaultNumericPrecision = 28;
		public const int DefaultNumericScale = 4;

		protected internal DAEDataReader(IServerCursor cursor, DAECommand command)
		{
			_cursor = cursor;
			_command = command;

			// Cache native types
			_nativeTypes = new ArrayList();
			foreach (TableVarColumn column in _cursor.Plan.TableVar.Columns)
				_nativeTypes.Add(GetNativeType(column.DataType, column, _cursor.Plan.Process.DataTypes));
		}

		/// <summary>
		/// A special reader that returns 1 for records affected.
		/// When records affected is required for an execute statement.
		/// </summary>
		/// <param name="command"></param>
		protected internal DAEDataReader(DAECommand command)
		{
			_command = command;
			_rowCount = 1; //Records effected is always one when the reader is in a non-cursor.
			_nativeTypes = new ArrayList();
		}

		private DAECommand _command;
		private IServerCursor _cursor;
		private int _rowCount;
		private ArrayList _nativeTypes;

		public static Type GetNativeType(IDataType dataType, DataTypes dataTypes)
		{
			if (dataType.Is(dataTypes.SystemBoolean))
				return typeof(bool);
			else if (dataType.Is(dataTypes.SystemByte))
				return typeof(byte);
			#if UseUnsignedIntegers
			else if (ADataType.Is(ADataTypes.SystemPByte))
				return typeof(byte);
			else if (ADataType.Is(ADataTypes.SystemPShort))
				return typeof(ushort);
			else if (ADataType.Is(ADataTypes.SystemPInteger))
				return typeof(uint);
			else if (DataType.Is(ADataTypes.SystemPLong))
				return typeof(ulong);
			else if (ADataType.Is(ADataTypes.SystemSByte))
				return typeof(sbyte);
			else if (ADataType.Is(ADataTypes.SystemUShort))
				return typeof(ushort);
			else if (ADataType.Is(ADataTypes.SystemUInteger))
				return typeof(uint);
			else if (ADataType.Is(ADataTypes.SystemULong))
				return typeof(ulong);
			#endif
			else if (dataType.Is(dataTypes.SystemShort))
				return typeof(short);
			else if (dataType.Is(dataTypes.SystemInteger))
				return typeof(int);
			else if (dataType.Is(dataTypes.SystemLong))
				return typeof(long);
			else if (dataType.Is(dataTypes.SystemDateTime) || dataType.Is(dataTypes.SystemDate) || dataType.Is(dataTypes.SystemTime))
				return typeof(DateTime);
			else if (dataType.Is(dataTypes.SystemTimeSpan))
				return typeof(TimeSpan);
			else if (dataType.Is(dataTypes.SystemDecimal) || dataType.Is(dataTypes.SystemMoney))
				return typeof(decimal);
			else if (dataType.Is(dataTypes.SystemGuid))
				return typeof(System.Guid);
			#if USEISTRING
			else if (ADataType.Is(ADataTypes.SystemString) || ADataType.Is(ADataTypes.SystemName) || ADataType.Is(ADataTypes.SystemIString))
			#else
			else if (dataType.Is(dataTypes.SystemString) || dataType.Is(dataTypes.SystemName))
			#endif
				return typeof(string);
			else
				return typeof(byte[]);	// All others -> byte array.
		}

		public static Type GetNativeType(IDataType dataType, TableVarColumn column, Schema.DataTypes dataTypes)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.NativeType");
            if (tag != Tag.None)
				return Type.GetType(tag.Value, true, true);
			else
				return GetNativeType(dataType, dataTypes);
		}
		
		public override IEnumerator GetEnumerator()
		{
			return new System.Data.Common.DbEnumerator(this, _command.Behavior == CommandBehavior.CloseConnection);
		}

		public override bool HasRows { get { return (_cursor != null) && !(_cursor.BOF() && _cursor.EOF()); } }

		public override int Depth
		{
			get { return 0; }
		}

		public override bool IsClosed
		{
			get { return _cursor == null; }
		}

		public override int RecordsAffected
		{
			get { return _rowCount; }
		}

		public override void Close()
		{
			if (!IsClosed)
			{
				try
				{
					DisposeRow();
				}
				finally
				{
					try
					{
						_cursor.Plan.Close(_cursor);
					}
					finally
					{
						_cursor = null;
					}
				}
			}
		}

		private Column GetInternalColumn(int index)
		{
			return ((TableType)_cursor.Plan.DataType).Columns[index];
		}

		private int GetColumnSize(TableVarColumn column)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.ColumnSize");
			if (tag != null)
				return System.Convert.ToInt32(tag.Value);
			else
				return System.Int32.MaxValue;
		}

		private bool GetIsLong(TableVarColumn column, Schema.DataTypes dataTypes)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.IsLong");
			if (tag != null)
				return tag.Value.ToLower() == "true";
			else
				return (GetNativeType(column.DataType, column, dataTypes) == typeof(byte[]));
		}

		private bool GetIsReadOnly(TableVarColumn column)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.IsReadOnly");
			if (tag != null)
				return tag.Value.ToLower() == "true";
			else
				return false;
		}

		private bool GetIsAutoIncrement(TableVarColumn column)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.IsAutoIncrement");
			if (tag != null)
				return tag.Value.ToLower() == "true";
			else
				return false;
		}

		private int GetNumericPrecision(TableVarColumn column)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.NumericPrecision");
			if (tag != null)
				return System.Convert.ToInt32(tag.Value);
			else
				return DefaultNumericPrecision;
		}

		private int GetNumericScale(TableVarColumn column)
		{
			DAE.Language.D4.Tag tag = column.GetMetaDataTag("DAEDataReader.NumericScale");
			if (tag != null)
				return System.Convert.ToInt32(tag.Value);
			else
				return DefaultNumericScale;
		}

		private DataColumn CreateSchemaColumn(string columnName, Type dataType)
		{
			DataColumn column = new DataColumn(columnName, dataType);
			//We don't need the DataTable to enforce these
			column.AllowDBNull = true;
			return column;
		}

		/// <summary> Returns a DataTable that describes the column metadata of the IDataReader. </summary>
		/// <remarks>
		/// The following Dataphor tags can be used to override column attributes.
		/// DAEDataReader.ColumnSize : The maximum possible length of a value in the column.
		/// The default is System.Int32.MaxValue.
		/// DAEDataReader.IsReadOnly : True if the column can be modified; otherwise false. The default is false.
		/// DAEDataReader.IsLong : True if the column is a (BLOB) otherwise false. The default is false.
		/// DAEDataReader.IsAutoIncrement : When true the column assigns values to new rows in fixed increments.
		/// Otherwise the column does not assign values to new rows in fixed increments. The default is false.
		/// DAEDataReader.NumericPrecision : Only applies to data types of SystemDecimal or descendents of SystemDecimal. 
		/// The maximum precision of the column. The default is 28.
		/// DAEDataReader.NumericScale : Only applies to data types of SystemDecimal or descendents of SystemDecimal.
		/// The number of digits to the right of the decimal point. The default is 4.
		/// </remarks>
		/// <returns> A DataTable that describes the column metadata. </returns>
		public override DataTable GetSchemaTable()
		{
			DataTable result = new DataTable("SchemaTable");

			result.Columns.Add(CreateSchemaColumn("ColumnName", typeof(String)));
			result.Columns.Add(CreateSchemaColumn("ColumnOrdinal", typeof(Int32)));
			result.Columns.Add(CreateSchemaColumn("ColumnSize", typeof(Int32)));
			result.Columns.Add(CreateSchemaColumn("NumericPrecision", typeof(Int32)));
			result.Columns.Add(CreateSchemaColumn("NumericScale", typeof(Int32)));
			result.Columns.Add(CreateSchemaColumn("DataType", typeof(Type)));
			result.Columns.Add(CreateSchemaColumn("ProviderType", typeof(Type)));
			result.Columns.Add(CreateSchemaColumn("IsLong", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("AllowDBNull", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("IsReadOnly", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("IsRowVersion", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("IsUnique", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("IsKeyColumn", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("IsAutoIncrement", typeof(Boolean)));
			result.Columns.Add(CreateSchemaColumn("BaseSchemaName", typeof(String)));
			result.Columns.Add(CreateSchemaColumn("BaseCatalogName", typeof(String)));
			result.Columns.Add(CreateSchemaColumn("BaseTableName", typeof(String)));
			result.Columns.Add(CreateSchemaColumn("BaseColumnName", typeof(String)));

			TableType type = (TableType)_cursor.Plan.DataType;
			TableVar var = _cursor.Plan.TableVar;
			DataTypes dataTypes = _cursor.Plan.Process.DataTypes;
			int ordinal = 1;
			foreach (TableVarColumn sourceColumn in var.Columns)
			{
				DataRow targetRow = result.NewRow();
				targetRow["ColumnName"] = sourceColumn.Name;
				targetRow["ColumnOrdinal"] = ordinal;
				//If only it were so simple.
				targetRow["ColumnSize"] = GetColumnSize(sourceColumn);
				if (sourceColumn.DataType.Is(_cursor.Plan.Process.DataTypes.SystemDecimal))
				{
					targetRow["NumericPrecision"] = GetNumericPrecision(sourceColumn);
					targetRow["NumericScale"] = GetNumericScale(sourceColumn);
				}
				else
				{
					targetRow["NumericPrecision"] = DBNull.Value;
					targetRow["NumericScale"] = DBNull.Value;
				}
				Type nativeType = (Type)_nativeTypes[ordinal - 1];
				targetRow["DataType"] = nativeType;
				targetRow["ProviderType"] = sourceColumn.DataType;
				targetRow["IsLong"] = GetIsLong(sourceColumn, dataTypes);
				// No way to determine whether the column is read-only at this point (read only to Dataphor means that some operator will reject an update)
				targetRow["IsReadOnly"] = GetIsReadOnly(sourceColumn);

				bool isUnique = false;
				bool isKeyColumn = false;
				foreach (Key key in var.Keys)
				{
					if (key.Columns.Contains(sourceColumn.Name))
					{
						isKeyColumn = true;
						if (key.Columns.Count == 1)
						{
							isUnique = true;
							break;
						}
					}
				}

				// IsUnique only makes sense at the column level if a key is composed 
				//  of exclusively this column... so look for that.
				targetRow["IsUnique"] = isUnique;
				// Whatever "row version" means... according to the SqlReader IsRowVersion equals IsUnique.
				targetRow["IsRowVersion"] = isUnique;
				// Look for a key which contains this column
				targetRow["IsKeyColumn"] = isKeyColumn;
				// Allow for DAE provider.
				targetRow["AllowDBNull"] = !isUnique;
				// If only it was so simple  :-)
				targetRow["IsAutoIncrement"] = GetIsAutoIncrement(sourceColumn);
				// Doesn't really map well into Dataphor's metaphor
				targetRow["BaseSchemaName"] = DBNull.Value;
				targetRow["BaseCatalogName"] = DBNull.Value;
				// Once again these are overly simplistic as this column could come from any mapping into the base tables
				targetRow["BaseTableName"] = DBNull.Value;
				result.Rows.Add(targetRow);
				ordinal++;
			}

			return result;
		}

		public override bool NextResult()
		{
			// Not supported... as the next "batch" may be a statement not an expression
			return false;
		}

		private DAE.Runtime.Data.Row _internalRow;

		private DAE.Runtime.Data.Row InternalRow
		{
			get
			{
				if (_internalRow == null)
					throw new ProviderException(ProviderException.Codes.CursorEOForBOF);
				return _internalRow;
			}
		}

		private void DisposeRow()
		{
			if (_internalRow != null)
			{
				_internalRow.Dispose();
				_internalRow = null;
			}
		}

		public override bool Read()
		{
			if (_cursor.Next())
			{
				if (_internalRow == null)
					_internalRow = new DAE.Runtime.Data.Row(_cursor.Plan.Process.ValueManager, ((TableType)_cursor.Plan.DataType).RowType);
				_cursor.Select(_internalRow);
				if (!_cursor.EOF())
					_rowCount++;
				return !_cursor.EOF();
			}
			else
				return false;
		}

		// IDataRecord

		public override int FieldCount
		{
			get
			{
				if (_internalRow == null)
					return _nativeTypes.Count;
				else
					return ((TableType)_cursor.Plan.DataType).Columns.Count;
			}
		}

		public override object this[string columnName] { get { return this[GetOrdinal(columnName)]; } }

		public object GetNativeValue(int index)
		{
			DAE.Runtime.Data.Row row = InternalRow;
			if (!row.HasValue(index))
				return DBNull.Value;
			else
				return row[index];
		}

		public override object this[int index] { get { return GetNativeValue(index); } }

		public override bool GetBoolean(int index)
		{
			return (bool)InternalRow[index];
		}

		public override byte GetByte(int index)
		{
			return (byte)InternalRow[index];
		}

		public sbyte GetSByte(int index)
		{
			return Convert.ToSByte((byte)InternalRow[index]);
		}

		public override long GetBytes(int index, long sourceOffset, byte[] target, int targetOffset, int count)
		{
			Stream stream = InternalRow.GetValue(index).OpenStream();
			try
			{
				stream.Position = sourceOffset;
				return stream.Read(target, targetOffset, count);
			}
			finally
			{
				stream.Close();
			}
		}

		public override char GetChar(int index)
		{
			return ((string)InternalRow[index])[0];
		}

		public override long GetChars(int index, long sourceOffset, char[] target, int targetOffset, int count)
		{
			string value = (string)InternalRow[index];
			value.CopyTo((int)sourceOffset, target, targetOffset, count);
			return target.Length;
		}

		public override string GetDataTypeName(int index)
		{
			return GetInternalColumn(index).DataType.Name;
		}

		public override DateTime GetDateTime(int index)
		{
			return (DateTime)InternalRow[index];
		}

		public TimeSpan GetTimeSpan(int index)
		{
			return (TimeSpan)InternalRow[index];
		}

		public override Decimal GetDecimal(int index)
		{
			return (decimal)InternalRow[index];
		}

		public override Double GetDouble(int index)
		{
			return Convert.ToDouble((decimal)InternalRow[index]);
		}

		public override Type GetFieldType(int index)
		{
			return (Type)_nativeTypes[index];
		}

		public override Single GetFloat(int index)
		{
			return Convert.ToSingle((decimal)InternalRow[index]);
		}

		public override Guid GetGuid(int index)
		{
			return (Guid)InternalRow[index];
		}

		public override Int16 GetInt16(int index)
		{
			return (short)InternalRow[index];
		}

		public UInt16 GetUInt16(int index)
		{
			return Convert.ToUInt16((short)InternalRow[index]);
		}

		public override Int32 GetInt32(int index)
		{
			return (int)InternalRow[index];
		}

		public UInt32 GetUInt32(int index)
		{
			return Convert.ToUInt32((int)InternalRow[index]);
		}

		public override Int64 GetInt64(int index)
		{
			return (long)InternalRow[index];
		}

		public UInt64 GetUInt64(int index)
		{
			return Convert.ToUInt64((long)InternalRow[index]);
		}

		public override string GetName(int index)
		{
			return GetInternalColumn(index).Name;
		}

		public override int GetOrdinal(string name)
		{
			return ((TableType)_cursor.Plan.DataType).Columns.IndexOf(name);
		}

		public override string GetString(int index)
		{
			return (string)InternalRow[index];
		}

		public override object GetValue(int index)
		{
			return GetNativeValue(index);
		}

		public override int GetValues(object[] values)
		{
			int i;
			for (i = 0; i < FieldCount; i++)
				values[i] = GetValue(i);
			return i;
		}

		public override bool IsDBNull(int index)
		{
			return !InternalRow.HasValue(index);
		}
	}
}
