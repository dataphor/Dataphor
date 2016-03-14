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
		public static IDataType NativeScalarTypeNameToDataType(Schema.DataTypes dataTypes, string nativeDataTypeName)
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
				default: 
					if (nativeDataTypeName.EndsWith("[]"))
					{
						// Subtract off ending brackets
						var arrayTypeName = nativeDataTypeName.Substring(0, nativeDataTypeName.Length - 2);
						return new ListType(NativeTypeNameToDataType(dataTypes, arrayTypeName));
					}
					else
						throw new ArgumentException(String.Format("Invalid native data type name: \"{0}\".", nativeDataTypeName));
			}
		}

		public static Columns ParseColumnTypes(Schema.DataTypes dataTypes, string columnDefinitions)
		{
			var results = new Columns();

			var current = 0;
			var colon = columnDefinitions.IndexOf(':');
			while (colon > 0)
			{
				var next = columnDefinitions.IndexOf(',', current);
				if (next < 0)
					next = columnDefinitions.Length;

				results.Add
				(
					new Column
					(
						columnDefinitions.Substring(current, colon - current), 
						NativeTypeNameToDataType(dataTypes, columnDefinitions.Substring(colon + 1, next - colon - 1))
					)
				);

				current = next + 1;
				if (current < columnDefinitions.Length)
					colon = columnDefinitions.IndexOf(':', current);
				else
					colon = -1;
			}

			return results;
		}

		public static IDataType NativeTypeNameToDataType(Schema.DataTypes dataTypes, string nativeDataTypeName)
		{
			if (nativeDataTypeName.ToLower().StartsWith("row"))
			{
				var rowType = new RowType();

				rowType.Columns.AddRange(ParseColumnTypes(dataTypes, nativeDataTypeName.Substring(4, nativeDataTypeName.Length - 5)));

				return rowType;
			}
			else if (nativeDataTypeName.ToLower().StartsWith("table"))
			{
				var tableType = new TableType();

				tableType.Columns.AddRange(ParseColumnTypes(dataTypes, nativeDataTypeName.Substring(6, nativeDataTypeName.Length - 7)));

				return tableType;
			}
			else if (nativeDataTypeName.ToLower().StartsWith("list"))
			{
				var elementType = NativeTypeNameToDataType(dataTypes, nativeDataTypeName.Substring(5, nativeDataTypeName.Length - 6));
				return new ListType(elementType);
			}
			else
			{
				return NativeScalarTypeNameToDataType(dataTypes, nativeDataTypeName);
			}
		}
		
		public static string DataTypeToDataTypeName(Schema.DataTypes dataTypes, IDataType dataType)
		{
			IScalarType scalarType = dataType as IScalarType;
			if (scalarType != null)
				return ScalarTypeToDataTypeName(dataTypes, scalarType);

			ListType listType = dataType as ListType;
			if (listType != null)
				return String.Format("list({0})", DataTypeToDataTypeName(dataTypes, listType.ElementType));

			RowType rowType = dataType as RowType;
			if (rowType != null)
			{
				var sb = new StringBuilder();
				sb.Append("row{");
				bool first = true;
				foreach (Column column in rowType.Columns)
				{
					if (!first)
						sb.Append(",");
					else
						first = false;

					sb.AppendFormat("{0}:{1}", column.Name, DataTypeToDataTypeName(dataTypes, column.DataType));
				}
				sb.Append("}");

				return sb.ToString();
			}

			TableType tableType = dataType as TableType;
			if (tableType != null)
			{
				var sb = new StringBuilder();
				sb.Append("table{");
				bool first = true;
				foreach (Column column in tableType.Columns)
				{
					if (!first)
						sb.Append(",");
					else
						first = false;

					sb.AppendFormat("{0}:{1}", column.Name, DataTypeToDataTypeName(dataTypes, column.DataType));
				}
				sb.Append("}");

				return sb.ToString();
			}
			
			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}
		
		public static string ScalarTypeToDataTypeName(Schema.DataTypes dataTypes, IScalarType scalarType)
		{
			if (scalarType.IsClassType) return scalarType.FromClassDefinition.ClassName;
			if (scalarType.NativeType == NativeAccessors.AsBoolean.NativeType) return "Boolean";
			if (scalarType.NativeType == NativeAccessors.AsByte.NativeType) return "Byte";
			if (scalarType.NativeType == NativeAccessors.AsByteArray.NativeType) return "Byte[]";
			if (scalarType.NativeType == NativeAccessors.AsDateTime.NativeType)
			{
				if (scalarType.Is(dataTypes.SystemDateTime) || scalarType.IsLike(dataTypes.SystemDateTime)) return "DateTime";
				if (scalarType.Is(dataTypes.SystemDate) || scalarType.IsLike(dataTypes.SystemDate)) return "Date";
				if (scalarType.Is(dataTypes.SystemTime) || scalarType.IsLike(dataTypes.SystemTime)) return "Time";
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
				columns.Add(new Column(nativeColumns[index].Name, NativeTypeNameToDataType(dataTypes, nativeColumns[index].DataTypeName)));
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
		
		public static IDataValue NativeValueToDataValue(ServerProcess process, NativeValue nativeValue)
		{
			NativeScalarValue nativeScalar = nativeValue as NativeScalarValue;
			if (nativeScalar != null)
				return new Scalar(process.ValueManager, (IScalarType)NativeTypeNameToDataType(process.DataTypes, nativeScalar.DataTypeName), nativeScalar.Value);
			
			NativeListValue nativeList = nativeValue as NativeListValue;
			if (nativeList != null)
			{
				// Create and fill the list
				ListValue list = new ListValue(process.ValueManager, (ListType)NativeValueToDataType(process, nativeValue), nativeList.Elements == null ? null : new NativeList());
				if (nativeList.Elements != null && nativeList.Elements.Length > 0)
				{
					for (int index = 0; index < nativeList.Elements.Length; index++)
						list.Add(NativeValueToDataValue(process, nativeList.Elements[index]));
				}

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
						row[index] = NativeValueToDataValue(process, nativeRow.Values[index]);
				}
				return row;
			}
			
			NativeTableValue nativeTable = nativeValue as NativeTableValue;
			if (nativeTable != null)
			{
				NativeTable internalTable = new NativeTable(process.ValueManager, NativeTableToTableVar(process, nativeTable));
				TableValue table = new TableValue(process.ValueManager, internalTable.TableType, internalTable); 
				if (nativeTable.Rows == null)
					table.AsNative = null;
				else
				{
					bool[] valueTypes = new bool[internalTable.TableType.Columns.Count];
					for (int index = 0; index < internalTable.TableType.Columns.Count; index++)
						valueTypes[index] = internalTable.TableType.Columns[index].DataType is IScalarType;

					for (int index = 0; index < nativeTable.Rows.Length; index++)
					{
						Row row = new Row(process.ValueManager, internalTable.RowType);
						try
						{
							for (int columnIndex = 0; columnIndex < nativeTable.Rows[index].Length; columnIndex++)
								if (valueTypes[columnIndex])
									row[columnIndex] = nativeTable.Rows[index][columnIndex];
								else
									row[columnIndex] = NativeValueToDataValue(process, (NativeValue)nativeTable.Rows[index][columnIndex]);

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

		private static IDataType NativeValueToDataType(ServerProcess process, NativeValue nativeValue)
		{
			NativeScalarValue nativeScalar = nativeValue as NativeScalarValue;
			if (nativeScalar != null)
				return NativeTypeNameToDataType(process.DataTypes, nativeScalar.DataTypeName);
			
			NativeListValue nativeList = nativeValue as NativeListValue;
			if (nativeList != null)
				return 
					new ListType
					(
						// Use the element type name if given
						!String.IsNullOrEmpty(nativeList.ElementDataTypeName) 
							? NativeTypeNameToDataType(process.DataTypes, nativeList.ElementDataTypeName) 

							// If not, try to use the type of the first element
							: (nativeList.Elements != null && nativeList.Elements.Length > 0 && String.IsNullOrEmpty(nativeList.ElementDataTypeName))
								? NativeValueToDataType(process, nativeList.Elements[0])

								// If not, revert to generic list
								: process.DataTypes.SystemGeneric
					);
			
			NativeRowValue nativeRow = nativeValue as NativeRowValue;
			if (nativeRow != null)
				return new Schema.RowType(NativeColumnsToColumns(process.DataTypes, nativeRow.Columns));
			
			NativeTableValue nativeTable = nativeValue as NativeTableValue;
			if (nativeTable != null)
			{
				TableType tableType = new TableType();
				foreach (Column column in NativeColumnsToColumns(process.DataTypes, nativeTable.Columns))
					tableType.Columns.Add(column);
				return tableType;
			}

			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", nativeTable.GetType().Name));
		}

		public static NativeValue DataTypeToNativeValue(IServerProcess process, IDataType dataType)
		{
			IScalarType scalarType = dataType as IScalarType;
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
		
		public static NativeValue DataValueToNativeValue(IServerProcess process, IDataValue dataValue)
		{
			if (dataValue == null)
				return null;

			IScalar scalar = dataValue as IScalar;
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
			
			IRow row = dataValue as IRow;
			if (row != null)
			{
				NativeRowValue nativeRow = new NativeRowValue();
				nativeRow.Columns = ColumnsToNativeColumns(process.DataTypes, row.DataType.Columns);
					
				if (!row.IsNil)
				{
					nativeRow.Values = new NativeValue[nativeRow.Columns.Length];
					for (int index = 0; index < nativeRow.Values.Length; index++)
						nativeRow.Values[index] = DataValueToNativeValue(process, row.GetValue(index));
				}
				return nativeRow;
			}

			TableValue tableValue = dataValue as TableValue;
			TableValueScan scan = null;
			try
			{
				if (tableValue != null)
				{
					scan = new TableValueScan(tableValue);
					scan.Open();
					dataValue = scan;
				}

				ITable table = dataValue as ITable;
				if (table != null)
				{
					NativeTableValue nativeTable = new NativeTableValue();
					nativeTable.Columns = ColumnsToNativeColumns(process.DataTypes, table.DataType.Columns);
					
					List<object[]> nativeRows = new List<object[]>();

					if (!table.BOF())
						table.First();
					
					bool[] valueTypes = new bool[nativeTable.Columns.Length];
					for (int index = 0; index < nativeTable.Columns.Length; index++)
						valueTypes[index] = table.DataType.Columns[index].DataType is IScalarType;

					while (table.Next())
					{
						using (IRow currentRow = table.Select())
						{
							object[] nativeRow = new object[nativeTable.Columns.Length];
							for (int index = 0; index < nativeTable.Columns.Length; index++)
								if (valueTypes[index])
									nativeRow[index] = currentRow[index];
								else
									nativeRow[index] = DataValueToNativeValue(process, currentRow.GetValue(index));

							nativeRows.Add(nativeRow);
						}
					}
				
					nativeTable.Rows = nativeRows.ToArray();
					return nativeTable;
				}
			}
			finally
			{
				if (scan != null)
					scan.Dispose();
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
			
			IRow currentRow = cursor.Plan.RequestRow();
			try
			{
				bool[] valueTypes = new bool[nativeTable.Columns.Length];
				for (int index = 0; index < nativeTable.Columns.Length; index++)
					valueTypes[index] = currentRow.DataType.Columns[index].DataType is IScalarType;

				while (cursor.Next())
				{
					cursor.Select(currentRow);
					object[] nativeRow = new object[nativeTable.Columns.Length];
					for (int index = 0; index < nativeTable.Columns.Length; index++)
						if (valueTypes[index])
							nativeRow[index] = currentRow[index];
						else
							nativeRow[index] = DataValueToNativeValue(process, currentRow.GetValue(index));

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

		public static NativeValue DataParamToNativeValue(IServerProcess process, DataParam dataParam)
		{
			var scalarType = dataParam.DataType as IScalarType;
			if (scalarType != null)
			{
				NativeScalarValue nativeScalar = new NativeScalarValue();
				nativeScalar.DataTypeName = ScalarTypeToDataTypeName(process.DataTypes, scalarType);
				nativeScalar.Value = dataParam.Value;
				return nativeScalar;
			}

			var listType = dataParam.DataType as IListType;
			if (listType != null)
			{
				var listValue = dataParam.Value as ListValue;
				NativeListValue nativeList = new NativeListValue();
				if (listValue != null)
				{
					nativeList.Elements = new NativeValue[listValue.Count()];
					for (int index = 0; index < listValue.Count(); index++)
						nativeList.Elements[index] = DataValueToNativeValue(process, listValue.GetValue(index));
				}
				return nativeList;
			}

			var rowType = dataParam.DataType as IRowType;
			if (rowType != null)
			{
				var rowValue = dataParam.Value as IRow;
				NativeRowValue nativeRow = new NativeRowValue();
				nativeRow.Columns = ColumnsToNativeColumns(process.DataTypes, rowType.Columns);

				if (rowValue != null)
				{
					nativeRow.Values = new NativeValue[nativeRow.Columns.Length];
					for (int index = 0; index < nativeRow.Values.Length; index++)
						nativeRow.Values[index] = DataValueToNativeValue(process, rowValue.GetValue(index));
				}
				return nativeRow;
			}

			var tableType = dataParam.DataType as ITableType;
			if (tableType != null)
			{
				var tableValue = dataParam.Value as ITable;
				NativeTableValue nativeTable = new NativeTableValue();
				nativeTable.Columns = ColumnsToNativeColumns(process.DataTypes, tableType.Columns);

				List<object[]> nativeRows = new List<object[]>();

				if (!tableValue.BOF())
					tableValue.First();

				var valueTypes = new bool[tableType.Columns.Count];
				for (int index = 0; index < tableType.Columns.Count; index++)
					valueTypes[index] = tableType.Columns[index].DataType is IScalarType;

				while (tableValue.Next())
				{
					using (IRow currentRow = tableValue.Select())
					{
						object[] nativeRow = new object[nativeTable.Columns.Length];
						for (int index = 0; index < nativeTable.Columns.Length; index++)
							if (valueTypes[index])
								nativeRow[index] = currentRow[index];
							else
								nativeRow[index] = DataValueToNativeValue(process, currentRow.GetValue(index));
		
						nativeRows.Add(nativeRow);
					}
				}

				nativeTable.Rows = nativeRows.ToArray();
				return nativeTable;
			}

			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", dataParam.DataType.Name));
		}
		
		public static DataParams NativeParamsToDataParams(ServerProcess process, NativeParam[] nativeParams)
		{
			DataParams dataParams = new DataParams();
			if (nativeParams != null)
				foreach (var nativeParam in nativeParams)
				{
					var value = NativeValueToDataValue(process, nativeParam.Value);
					DataParam dataParam = 
						new DataParam
						(
							nativeParam.Name, 
							value.DataType, 
							NativeCLIUtility.NativeModifierToModifier(nativeParam.Modifier), 
							value.IsNil ? null : (value is IScalar ? value.AsNative : value)
						);
					dataParams.Add(dataParam);
				}
			return dataParams;
		}

		public static void SetNativeOutputParams(IServerProcess process, NativeParam[] nativeParams, DataParams dataParams)
		{
			if (nativeParams != null)
				for (int index = 0; index < nativeParams.Length; index++)
				{
					NativeParam nativeParam = nativeParams[index];
					if ((nativeParam.Modifier == NativeModifier.Var) || (nativeParam.Modifier == NativeModifier.Out))
						nativeParam.Value = DataParamToNativeValue(process, dataParams[index]);
				}
		}
		
		public static NativeParam[] DataParamsToNativeParams(IServerProcess process, DataParams dataParams)
		{
			NativeParam[] nativeParams = new NativeParam[dataParams.Count];
			for (int index = 0; index < dataParams.Count; index++)
			{
				var dataParam = dataParams[index];
				var nativeValue = DataParamToNativeValue(process, dataParam);
				nativeParams[index] =	
					new NativeParam() 
					{
						Name = dataParam.Name, 
						Modifier = NativeCLIUtility.ModifierToNativeModifier(dataParam.Modifier),
						Value = nativeValue
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
				{
					var dataValue = NativeValueToDataValue((ServerProcess)process, nativeParams[index].Value);
					dataParam.Value = dataValue.IsNil ? null : dataValue.AsNative;
				}
			}
		}
	}
}
