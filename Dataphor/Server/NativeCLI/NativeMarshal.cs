/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;

	/// <summary>
	/// Provides a utility class for translating values to and from 
	/// their Native representations for use in Native CLI calls.
	/// </summary>
	public static class NativeMarshal
	{
		public static ScalarType DataTypeNameToScalarType(Schema.DataTypes dataTypes, string nativeDataTypeName)
		{
			switch (nativeDataTypeName.ToLower())
			{
				case "byte[]" : return dataTypes.SystemBinary;
				case "bool" :
				case "boolean" : return dataTypes.SystemBoolean;
				case "byte" : return dataTypes.SystemByte;
				case "date" : return dataTypes.SystemDate;
				case "datetime" : return dataTypes.SystemDateTime;
				case "decimal" : return dataTypes.SystemDecimal;
				case "exception" : return dataTypes.SystemError;
				case "guid" : return dataTypes.SystemGuid;
				case "int32" :
				case "integer" : return dataTypes.SystemInteger;
				case "int64" :
				case "long" : return dataTypes.SystemLong;
				case "money" : return dataTypes.SystemMoney;
				case "int16" :
				case "short" : return dataTypes.SystemShort;
				case "string" : return dataTypes.SystemString;
				case "time" : return dataTypes.SystemTime;
				case "timespan" : return dataTypes.SystemTimeSpan;
				default: throw new ArgumentException(String.Format("Invalid native data type name: \"{0}\".", nativeDataTypeName));
			}
		}
		
		public static string DataTypeToDataTypeName(Schema.DataTypes dataTypes, IDataType dataType)
		{
			ScalarType scalarType = dataType as ScalarType;
			if (scalarType != null)
				return ScalarTypeToDataTypeName(dataTypes, scalarType);
			
			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}
		
		public static string ScalarTypeToDataTypeName(Schema.DataTypes dataTypes, ScalarType scalarType)
		{
			if (scalarType.NativeType == NativeAccessors.AsBoolean.NativeType) return "Boolean";
			if (scalarType.NativeType == NativeAccessors.AsByte.NativeType) return "Byte";
			if (scalarType.NativeType == NativeAccessors.AsByteArray.NativeType) return "Byte[]";
			if (scalarType.NativeType == NativeAccessors.AsDateTime.NativeType)
			{
				if (scalarType.Is(dataTypes.SystemDateTime)) return "DateTime";
				if (scalarType.Is(dataTypes.SystemDate)) return "Date";
				if (scalarType.Is(dataTypes.SystemTime)) return "Time";
			}
			if (scalarType.NativeType == NativeAccessors.AsDecimal.NativeType) return "Decimal";
			if (scalarType.NativeType == NativeAccessors.AsException.NativeType) return "Exception";
			if (scalarType.NativeType == NativeAccessors.AsGuid.NativeType) return "Guid";
			if (scalarType.NativeType == NativeAccessors.AsInt16.NativeType) return "Int16";
			if (scalarType.NativeType == NativeAccessors.AsInt32.NativeType) return "Int32";
			if (scalarType.NativeType == NativeAccessors.AsInt64.NativeType) return "Int64";
			if (scalarType.NativeType == NativeAccessors.AsString.NativeType) return "String";
			if (scalarType.NativeType == NativeAccessors.AsTimeSpan.NativeType) return "TimeSpan";
			throw new ArgumentException(String.Format("Scalar type \"{0}\" has no native type.", scalarType.Name));
		}
		
		public static NativeColumn[] ColumnsToNativeColumns(Schema.DataTypes dataTypes, Columns columns)
		{
			NativeColumn[] localColumns = new NativeColumn[columns.Count];
			for (int index = 0; index < columns.Count; index++)
				localColumns[index] = 
					new NativeColumn 
					{ 
						Name = columns[index].Name, 
						DataTypeName = DataTypeToDataTypeName(dataTypes, columns[index].DataType) 
					};
			
			return localColumns;
		}
		
		public static Columns NativeColumnsToColumns(Schema.DataTypes dataTypes, NativeColumn[] nativeColumns)
		{
			Columns columns = new Columns();
			for (int index = 0; index < nativeColumns.Length; index++)
				columns.Add(new Column(nativeColumns[index].Name, DataTypeNameToScalarType(dataTypes, nativeColumns[index].DataTypeName)));
			return columns;
		}
		
		public static TableVar NativeTableToTableVar(ServerProcess process, NativeTableValue nativeTable)
		{
			using (Plan plan = new Plan(process))
			{
				return NativeTableToTableVar(plan, nativeTable);
			}
		}
		
		public static TableVar NativeTableToTableVar(Plan plan, NativeTableValue nativeTable)
		{
			TableType tableType = new TableType();
			foreach (Column column in NativeColumnsToColumns(plan.DataTypes, nativeTable.Columns))
				tableType.Columns.Add(column);

			BaseTableVar tableVar = new BaseTableVar(tableType);
			tableVar.EnsureTableVarColumns();
			if (nativeTable.Keys != null)
			{
				foreach (NativeKey nativeKey in nativeTable.Keys)
				{
					Key key = new Key();
					foreach (string columnName in nativeKey.KeyColumns)
						key.Columns.Add(tableVar.Columns[columnName]);
					tableVar.Keys.Add(key);
				}
			}
			Compiler.EnsureKey(plan, tableVar);
			return tableVar;
		}
		
		public static DataValue NativeValueToDataValue(ServerProcess process, NativeValue nativeValue)
		{
			NativeScalarValue nativeScalar = nativeValue as NativeScalarValue;
			if (nativeScalar != null)
				return new Scalar(process.ValueManager, DataTypeNameToScalarType(process.DataTypes, nativeScalar.DataTypeName), nativeScalar.Value);
			
			NativeListValue nativeList = nativeValue as NativeListValue;
			if (nativeList != null)
			{
				ListValue list = new ListValue(process.ValueManager, process.DataTypes.SystemList, nativeList.Elements == null ? null : new NativeList());
				if (nativeList.Elements != null)
					for (int index = 0; index < nativeList.Elements.Length; index++)
						list.Add(NativeValueToDataValue(process, nativeList.Elements[index]));
				return list;
			}
			
			NativeRowValue nativeRow = nativeValue as NativeRowValue;
			if (nativeRow != null)
			{
				Row row = new Row(process.ValueManager, new Schema.RowType(NativeColumnsToColumns(process.DataTypes, nativeRow.Columns)));
				if (nativeRow.Values == null)
					row.AsNative = null;
				else
				{
					for (int index = 0; index < nativeRow.Values.Length; index++)
						row[index] = nativeRow.Values[index];
				}
				return row;
			}
			
			NativeTableValue nativeTable = nativeValue as NativeTableValue;
			if (nativeTable != null)
			{
				NativeTable internalTable = new NativeTable(process.ValueManager, NativeTableToTableVar(process, nativeTable));
				TableValue table = new TableValue(process.ValueManager, internalTable); 
				if (nativeTable.Rows == null)
					table.AsNative = null;
				else
				{
					for (int index = 0; index < nativeTable.Rows.Length; index++)
					{
						Row row = new Row(process.ValueManager, internalTable.RowType);
						try
						{
							for (int columnIndex = 0; columnIndex < nativeTable.Rows[index].Length; columnIndex++)
								row[columnIndex] = nativeTable.Rows[index][columnIndex];
							internalTable.Insert(process.ValueManager, row);
						}
						catch (Exception)
						{
							row.Dispose();
							throw;
						}
					}
				}
				return table;
			}
			
			throw new NotSupportedException(String.Format("Unknown native value type: \"{0}\".", nativeValue.GetType().Name));
		}
		
		public static NativeValue DataTypeToNativeValue(IServerProcess process, IDataType dataType)
		{
			ScalarType scalarType = dataType as ScalarType;
			if (scalarType != null)
			{
				NativeScalarValue nativeScalar = new NativeScalarValue();
				nativeScalar.DataTypeName = ScalarTypeToDataTypeName(process.DataTypes, scalarType);
				return nativeScalar;
			}
			
			ListType listType = dataType as ListType;
			if (listType != null)
			{
				NativeListValue nativeList = new NativeListValue();
				return nativeList;
			}
			
			RowType rowType = dataType as RowType;
			if (rowType != null)
			{
				NativeRowValue nativeRow = new NativeRowValue();
				nativeRow.Columns = ColumnsToNativeColumns(process.DataTypes, rowType.Columns);
				return nativeRow;
			}
			
			TableType tableType = dataType as TableType;
			if (tableType != null)
			{
				NativeTableValue nativeTable = new NativeTableValue();
				nativeTable.Columns = ColumnsToNativeColumns(process.DataTypes, tableType.Columns);
				return nativeTable;
			}
				
			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", dataType.Name));
		}
		
		public static NativeValue DataValueToNativeValue(IServerProcess process, DataValue dataValue)
		{
			Scalar scalar = dataValue as Scalar;
			if (scalar != null)
			{
				NativeScalarValue nativeScalar = new NativeScalarValue();
				nativeScalar.DataTypeName = ScalarTypeToDataTypeName(process.DataTypes, scalar.DataType);
				nativeScalar.Value = dataValue.IsNil ? null : scalar.AsNative;
				return nativeScalar;
			}
			
			ListValue list = dataValue as ListValue;
			if (list != null)
			{
				NativeListValue nativeList = new NativeListValue();
				if (!list.IsNil)
				{
					nativeList.Elements = new NativeValue[list.Count()];
					for (int index = 0; index < list.Count(); index++)
						nativeList.Elements[index] = DataValueToNativeValue(process, list.GetValue(index));
				}
				return nativeList;
			}
			
			Row row = dataValue as Row;
			if (row != null)
			{
				NativeRowValue nativeRow = new NativeRowValue();
				nativeRow.Columns = ColumnsToNativeColumns(process.DataTypes, row.DataType.Columns);
					
				if (!row.IsNil)
				{
					nativeRow.Values = new object[nativeRow.Columns.Length];
					for (int index = 0; index < nativeRow.Values.Length; index++)
						nativeRow.Values[index] = row[index];
				}
				return nativeRow;
			}
			
			Table table = dataValue as Table;
			if (table != null)
			{
				NativeTableValue nativeTable = new NativeTableValue();
				nativeTable.Columns = ColumnsToNativeColumns(process.DataTypes, table.DataType.Columns);
					
				List<object[]> nativeRows = new List<object[]>();

				if (!table.BOF())
					table.First();
					
				while (table.Next())
				{
					using (Row currentRow = table.Select())
					{
						object[] nativeRow = new object[nativeTable.Columns.Length];
						for (int index = 0; index < nativeTable.Columns.Length; index++)
							nativeRow[index] = currentRow[index];
						nativeRows.Add(nativeRow);
					}
				}
				
				nativeTable.Rows = nativeRows.ToArray();
				return nativeTable;
			}
			
			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", dataValue.DataType.Name));
		}
		
		public static NativeTableValue TableVarToNativeTableValue(IServerProcess process, TableVar tableVar)
		{
			NativeTableValue nativeTable = new NativeTableValue();
			nativeTable.Columns = ColumnsToNativeColumns(process.DataTypes, tableVar.DataType.Columns);
			nativeTable.Keys = new NativeKey[tableVar.Keys.Count];
			for (int index = 0; index < tableVar.Keys.Count; index++)
			{
				nativeTable.Keys[index] = new NativeKey();
				nativeTable.Keys[index].KeyColumns = tableVar.Keys[index].Columns.ColumnNames;
			}
			
			return nativeTable;
		}
		
		public static NativeValue ServerCursorToNativeValue(IServerProcess process, IServerCursor cursor)
		{
			NativeTableValue nativeTable = TableVarToNativeTableValue(process, cursor.Plan.TableVar);
				
			List<object[]> nativeRows = new List<object[]>();
			
			Row currentRow = cursor.Plan.RequestRow();
			try
			{
				while (cursor.Next())
				{
					cursor.Select(currentRow);
					object[] nativeRow = new object[nativeTable.Columns.Length];
					for (int index = 0; index < nativeTable.Columns.Length; index++)
						nativeRow[index] = currentRow[index];
					nativeRows.Add(nativeRow);
				}
			}
			finally
			{
				cursor.Plan.ReleaseRow(currentRow);
			}
			
			nativeTable.Rows = nativeRows.ToArray();
			
			return nativeTable;
		}
		
		public static DataParams NativeParamsToDataParams(IServerProcess process, NativeParam[] nativeParams)
		{
			DataParams dataParams = new DataParams();
			for (int index = 0; index < nativeParams.Length; index++)
			{
				NativeParam nativeParam = nativeParams[index];
				DataParam dataParam = 
					new DataParam
					(
						nativeParam.Name, 
						DataTypeNameToScalarType(process.DataTypes, nativeParam.DataTypeName), 
						NativeCLIUtility.NativeModifierToModifier(nativeParam.Modifier), 
						nativeParam.Value
					);
				dataParams.Add(dataParam);
			}
			return dataParams;
		}
		
		public static void SetNativeOutputParams(IServerProcess process, NativeParam[] nativeParams, DataParams dataParams)
		{
			for (int index = 0; index < nativeParams.Length; index++)
			{
				NativeParam nativeParam = nativeParams[index];
				if ((nativeParam.Modifier == NativeModifier.Var) || (nativeParam.Modifier == NativeModifier.Out))
				{
					DataParam dataParam = dataParams[index];
					nativeParam.Value = dataParam.Value;
				}
			}
		}
		
		public static NativeParam[] DataParamsToNativeParams(IServerProcess process, DataParams dataParams)
		{
			NativeParam[] nativeParams = new NativeParam[dataParams.Count];
			for (int index = 0; index < dataParams.Count; index++)
			{
				DataParam dataParam = dataParams[index];
				nativeParams[index] =	
					new NativeParam() 
					{
						Name = dataParam.Name, 
						DataTypeName = DataTypeToDataTypeName(process.DataTypes, dataParam.DataType),
						Modifier = NativeCLIUtility.ModifierToNativeModifier(dataParam.Modifier),
						Value = dataParam.Value
					};
			}
			return nativeParams;
		}
		
		public static void SetDataOutputParams(IServerProcess process, DataParams dataParams, NativeParam[] nativeParams)
		{
			for (int index = 0; index < dataParams.Count; index++)
			{
				DataParam dataParam = dataParams[index];
				if ((dataParam.Modifier == Modifier.Var) || (dataParam.Modifier == Modifier.Out))
					dataParam.Value = nativeParams[index].Value;
			}
		}
	}
}
