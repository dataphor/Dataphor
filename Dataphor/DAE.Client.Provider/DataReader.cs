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
		public const string CMetaDataNamespace = "DAEDataReader";
		public const string CNativeTypeTag = "NativeType";
		public const string CColumnSizeTag = "ColumnSize";
		public const string CIsReadOnlyTag = "IsReadOnly";
		public const string CIsAutoIncrementTag = "IsAutoIncrement";
		public const string CIsLongTag = "IsLong";
		public const string CNumericPrecisionTag = "NumericPrecision";
		public const string CNumericScaleTag = "NumericScale";
		public const int CDefaultNumericPrecision = 28;
		public const int CDefaultNumericScale = 4;

		protected internal DAEDataReader(IServerCursor ACursor, DAECommand ACommand)
		{
			FCursor = ACursor;
			FCommand = ACommand;

			// Cache native types
			FNativeTypes = new ArrayList();
			foreach (TableVarColumn LColumn in FCursor.Plan.TableVar.Columns)
				FNativeTypes.Add(GetNativeType(LColumn.DataType, LColumn, FCursor.Plan.Process.DataTypes));
		}

		/// <summary>
		/// A special reader that returns 1 for records affected.
		/// When records affected is required for an execute statement.
		/// </summary>
		/// <param name="ACommand"></param>
		protected internal DAEDataReader(DAECommand ACommand)
		{
			FCommand = ACommand;
			FRowCount = 1; //Records effected is always one when the reader is in a non-cursor.
			FNativeTypes = new ArrayList();
		}

		private DAECommand FCommand;
		private IServerCursor FCursor;
		private int FRowCount;
		private ArrayList FNativeTypes;

		public static Type GetNativeType(IDataType ADataType, DataTypes ADataTypes)
		{
			if (ADataType.Is(ADataTypes.SystemBoolean))
				return typeof(bool);
			else if (ADataType.Is(ADataTypes.SystemByte))
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
			else if (ADataType.Is(ADataTypes.SystemShort))
				return typeof(short);
			else if (ADataType.Is(ADataTypes.SystemInteger))
				return typeof(int);
			else if (ADataType.Is(ADataTypes.SystemLong))
				return typeof(long);
			else if (ADataType.Is(ADataTypes.SystemDateTime) || ADataType.Is(ADataTypes.SystemDate) || ADataType.Is(ADataTypes.SystemTime))
				return typeof(DateTime);
			else if (ADataType.Is(ADataTypes.SystemTimeSpan))
				return typeof(TimeSpan);
			else if (ADataType.Is(ADataTypes.SystemDecimal) || ADataType.Is(ADataTypes.SystemMoney))
				return typeof(decimal);
			else if (ADataType.Is(ADataTypes.SystemGuid))
				return typeof(System.Guid);
			#if USEISTRING
			else if (ADataType.Is(ADataTypes.SystemString) || ADataType.Is(ADataTypes.SystemName) || ADataType.Is(ADataTypes.SystemIString))
			#else
			else if (ADataType.Is(ADataTypes.SystemString) || ADataType.Is(ADataTypes.SystemName))
			#endif
				return typeof(string);
			else
				return typeof(byte[]);	// All others -> byte array.
		}

		public static Type GetNativeType(IDataType ADataType, TableVarColumn AColumn, Schema.DataTypes ADataTypes)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.NativeType");
			if (LTag != null)
				return Type.GetType(LTag.Value, true, true);
			else
				return GetNativeType(ADataType, ADataTypes);
		}
		
		public override IEnumerator GetEnumerator()
		{
			return new System.Data.Common.DbEnumerator(this, FCommand.Behavior == CommandBehavior.CloseConnection);
		}

		public override bool HasRows { get { return (FCursor != null) && !(FCursor.BOF() && FCursor.EOF()); } }

		public override int Depth
		{
			get { return 0; }
		}

		public override bool IsClosed
		{
			get { return FCursor == null; }
		}

		public override int RecordsAffected
		{
			get { return FRowCount; }
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
						FCursor.Plan.Close(FCursor);
					}
					finally
					{
						FCursor = null;
					}
				}
			}
		}

		private Column GetInternalColumn(int AIndex)
		{
			return ((TableType)FCursor.Plan.DataType).Columns[AIndex];
		}

		private int GetColumnSize(TableVarColumn AColumn)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.ColumnSize");
			if (LTag != null)
				return System.Convert.ToInt32(LTag.Value);
			else
				return System.Int32.MaxValue;
		}

		private bool GetIsLong(TableVarColumn AColumn, Schema.DataTypes ADataTypes)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.IsLong");
			if (LTag != null)
				return LTag.Value.ToLower() == "true";
			else
				return (GetNativeType(AColumn.DataType, AColumn, ADataTypes) == typeof(byte[]));
		}

		private bool GetIsReadOnly(TableVarColumn AColumn)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.IsReadOnly");
			if (LTag != null)
				return LTag.Value.ToLower() == "true";
			else
				return false;
		}

		private bool GetIsAutoIncrement(TableVarColumn AColumn)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.IsAutoIncrement");
			if (LTag != null)
				return LTag.Value.ToLower() == "true";
			else
				return false;
		}

		private int GetNumericPrecision(TableVarColumn AColumn)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.NumericPrecision");
			if (LTag != null)
				return System.Convert.ToInt32(LTag.Value);
			else
				return CDefaultNumericPrecision;
		}

		private int GetNumericScale(TableVarColumn AColumn)
		{
			DAE.Language.D4.Tag LTag = AColumn.MetaData.Tags.GetTag("DAEDataReader.NumericScale");
			if (LTag != null)
				return System.Convert.ToInt32(LTag.Value);
			else
				return CDefaultNumericScale;
		}

		private DataColumn CreateSchemaColumn(string AColumnName, Type ADataType)
		{
			DataColumn LColumn = new DataColumn(AColumnName, ADataType);
			//We don't need the DataTable to enforce these
			LColumn.AllowDBNull = true;
			return LColumn;
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
			DataTable LResult = new DataTable("SchemaTable");

			LResult.Columns.Add(CreateSchemaColumn("ColumnName", typeof(String)));
			LResult.Columns.Add(CreateSchemaColumn("ColumnOrdinal", typeof(Int32)));
			LResult.Columns.Add(CreateSchemaColumn("ColumnSize", typeof(Int32)));
			LResult.Columns.Add(CreateSchemaColumn("NumericPrecision", typeof(Int32)));
			LResult.Columns.Add(CreateSchemaColumn("NumericScale", typeof(Int32)));
			LResult.Columns.Add(CreateSchemaColumn("DataType", typeof(Type)));
			LResult.Columns.Add(CreateSchemaColumn("ProviderType", typeof(Type)));
			LResult.Columns.Add(CreateSchemaColumn("IsLong", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("AllowDBNull", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("IsReadOnly", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("IsRowVersion", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("IsUnique", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("IsKeyColumn", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("IsAutoIncrement", typeof(Boolean)));
			LResult.Columns.Add(CreateSchemaColumn("BaseSchemaName", typeof(String)));
			LResult.Columns.Add(CreateSchemaColumn("BaseCatalogName", typeof(String)));
			LResult.Columns.Add(CreateSchemaColumn("BaseTableName", typeof(String)));
			LResult.Columns.Add(CreateSchemaColumn("BaseColumnName", typeof(String)));

			TableType LType = (TableType)FCursor.Plan.DataType;
			TableVar LVar = FCursor.Plan.TableVar;
			DataTypes LDataTypes = FCursor.Plan.Process.DataTypes;
			int LOrdinal = 1;
			foreach (TableVarColumn LSourceColumn in LVar.Columns)
			{
				DataRow LTargetRow = LResult.NewRow();
				LTargetRow["ColumnName"] = LSourceColumn.Name;
				LTargetRow["ColumnOrdinal"] = LOrdinal;
				//If only it were so simple.
				LTargetRow["ColumnSize"] = GetColumnSize(LSourceColumn);
				if (LSourceColumn.DataType.Is(FCursor.Plan.Process.DataTypes.SystemDecimal))
				{
					LTargetRow["NumericPrecision"] = GetNumericPrecision(LSourceColumn);
					LTargetRow["NumericScale"] = GetNumericScale(LSourceColumn);
				}
				else
				{
					LTargetRow["NumericPrecision"] = DBNull.Value;
					LTargetRow["NumericScale"] = DBNull.Value;
				}
				Type LNativeType = (Type)FNativeTypes[LOrdinal - 1];
				LTargetRow["DataType"] = LNativeType;
				LTargetRow["ProviderType"] = LSourceColumn.DataType;
				LTargetRow["IsLong"] = GetIsLong(LSourceColumn, LDataTypes);
				// No way to determine whether the column is read-only at this point (read only to Dataphor means that some operator will reject an update)
				LTargetRow["IsReadOnly"] = GetIsReadOnly(LSourceColumn);

				bool LIsUnique = false;
				bool LIsKeyColumn = false;
				foreach (Key LKey in LVar.Keys)
				{
					if (LKey.Columns.Contains(LSourceColumn.Name))
					{
						LIsKeyColumn = true;
						if (LKey.Columns.Count == 1)
						{
							LIsUnique = true;
							break;
						}
					}
				}

				// IsUnique only makes sense at the column level if a key is composed 
				//  of exclusively this column... so look for that.
				LTargetRow["IsUnique"] = LIsUnique;
				// Whatever "row version" means... according to the SqlReader IsRowVersion equals IsUnique.
				LTargetRow["IsRowVersion"] = LIsUnique;
				// Look for a key which contains this column
				LTargetRow["IsKeyColumn"] = LIsKeyColumn;
				// Allow for DAE provider.
				LTargetRow["AllowDBNull"] = !LIsUnique;
				// If only it was so simple  :-)
				LTargetRow["IsAutoIncrement"] = GetIsAutoIncrement(LSourceColumn);
				// Doesn't really map well into Dataphor's metaphor
				LTargetRow["BaseSchemaName"] = DBNull.Value;
				LTargetRow["BaseCatalogName"] = DBNull.Value;
				// Once again these are overly simplistic as this column could come from any mapping into the base tables
				LTargetRow["BaseTableName"] = DBNull.Value;
				LResult.Rows.Add(LTargetRow);
				LOrdinal++;
			}

			return LResult;
		}

		public override bool NextResult()
		{
			// Not supported... as the next "batch" may be a statement not an expression
			return false;
		}

		private DAE.Runtime.Data.Row FInternalRow;

		private DAE.Runtime.Data.Row InternalRow
		{
			get
			{
				if (FInternalRow == null)
					throw new ProviderException(ProviderException.Codes.CursorEOForBOF);
				return FInternalRow;
			}
		}

		private void DisposeRow()
		{
			if (FInternalRow != null)
			{
				FInternalRow.Dispose();
				FInternalRow = null;
			}
		}

		public override bool Read()
		{
			if (FCursor.Next())
			{
				if (FInternalRow == null)
					FInternalRow = new DAE.Runtime.Data.Row(FCursor.Plan.Process.ValueManager, ((TableType)FCursor.Plan.DataType).RowType);
				FCursor.Select(FInternalRow);
				if (!FCursor.EOF())
					FRowCount++;
				return !FCursor.EOF();
			}
			else
				return false;
		}

		// IDataRecord

		public override int FieldCount
		{
			get
			{
				if (FInternalRow == null)
					return FNativeTypes.Count;
				else
					return ((TableType)FCursor.Plan.DataType).Columns.Count;
			}
		}

		public override object this[string AColumnName] { get { return this[GetOrdinal(AColumnName)]; } }

		public object GetNativeValue(int AIndex)
		{
			DAE.Runtime.Data.Row LRow = InternalRow;
			if (!LRow.HasValue(AIndex))
				return DBNull.Value;
			else
				return LRow[AIndex];
		}

		public override object this[int AIndex] { get { return GetNativeValue(AIndex); } }

		public override bool GetBoolean(int AIndex)
		{
			return (bool)InternalRow[AIndex];
		}

		public override byte GetByte(int AIndex)
		{
			return (byte)InternalRow[AIndex];
		}

		public sbyte GetSByte(int AIndex)
		{
			return Convert.ToSByte((byte)InternalRow[AIndex]);
		}

		public override long GetBytes(int AIndex, long ASourceOffset, byte[] ATarget, int ATargetOffset, int ACount)
		{
			Stream LStream = InternalRow.GetValue(AIndex).OpenStream();
			try
			{
				LStream.Position = ASourceOffset;
				return LStream.Read(ATarget, ATargetOffset, ACount);
			}
			finally
			{
				LStream.Close();
			}
		}

		public override char GetChar(int AIndex)
		{
			return ((string)InternalRow[AIndex])[0];
		}

		public override long GetChars(int AIndex, long ASourceOffset, char[] ATarget, int ATargetOffset, int ACount)
		{
			string LValue = (string)InternalRow[AIndex];
			LValue.CopyTo((int)ASourceOffset, ATarget, ATargetOffset, ACount);
			return ATarget.Length;
		}

		public override string GetDataTypeName(int AIndex)
		{
			return GetInternalColumn(AIndex).DataType.Name;
		}

		public override DateTime GetDateTime(int AIndex)
		{
			return (DateTime)InternalRow[AIndex];
		}

		public TimeSpan GetTimeSpan(int AIndex)
		{
			return (TimeSpan)InternalRow[AIndex];
		}

		public override Decimal GetDecimal(int AIndex)
		{
			return (decimal)InternalRow[AIndex];
		}

		public override Double GetDouble(int AIndex)
		{
			return Convert.ToDouble((decimal)InternalRow[AIndex]);
		}

		public override Type GetFieldType(int AIndex)
		{
			return (Type)FNativeTypes[AIndex];
		}

		public override Single GetFloat(int AIndex)
		{
			return Convert.ToSingle((decimal)InternalRow[AIndex]);
		}

		public override Guid GetGuid(int AIndex)
		{
			return (Guid)InternalRow[AIndex];
		}

		public override Int16 GetInt16(int AIndex)
		{
			return (short)InternalRow[AIndex];
		}

		public UInt16 GetUInt16(int AIndex)
		{
			return Convert.ToUInt16((short)InternalRow[AIndex]);
		}

		public override Int32 GetInt32(int AIndex)
		{
			return (int)InternalRow[AIndex];
		}

		public UInt32 GetUInt32(int AIndex)
		{
			return Convert.ToUInt32((int)InternalRow[AIndex]);
		}

		public override Int64 GetInt64(int AIndex)
		{
			return (long)InternalRow[AIndex];
		}

		public UInt64 GetUInt64(int AIndex)
		{
			return Convert.ToUInt64((long)InternalRow[AIndex]);
		}

		public override string GetName(int AIndex)
		{
			return GetInternalColumn(AIndex).Name;
		}

		public override int GetOrdinal(string AName)
		{
			return ((TableType)FCursor.Plan.DataType).Columns.IndexOf(AName);
		}

		public override string GetString(int AIndex)
		{
			return (string)InternalRow[AIndex];
		}

		public override object GetValue(int AIndex)
		{
			return GetNativeValue(AIndex);
		}

		public override int GetValues(object[] AValues)
		{
			int i;
			for (i = 0; i < FieldCount; i++)
				AValues[i] = GetValue(i);
			return i;
		}

		public override bool IsDBNull(int AIndex)
		{
			return !InternalRow.HasValue(AIndex);
		}
	}
}
