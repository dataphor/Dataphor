/*
	Alphora Dataphor
	© Copyright 2000-2018 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR
{
	/// <summary>
	/// Provides a utility class for translating values to and from 
	/// their C#/FHIR representations for use in FHIR API calls.
	/// </summary>
	public static class DataFhirMarshal
	{
		public static DataParams ToDataParams(IServerProcess process, IEnumerable<KeyValuePair<string, object>> args)
		{
			var dataParams = new DataParams();
			if (args != null)
			{
				foreach (var e in args)
				{
					var value = ValueToDataValue((ServerProcess)process, e.Value);
					dataParams.Add
					(
						new DataParam
						(
							e.Key,
							value.DataType,
							DAE.Language.Modifier.In,
							value.IsNil ? null : (value is IScalar ? value.AsNative : value)
						)
					);
				}
			}
			return dataParams;
		}

		public static DataParam[] ArgsToDataParams(IServerProcess process, IEnumerable<KeyValuePair<string, object>> args)
		{
			DataParam[] paramsValue = null;
			if (args != null)
			{
				var paramsList = new List<DataParam>();
				foreach (var e in args)
				{
					var value = ValueToDataValue((ServerProcess)process, e.Value);

					paramsList.Add(new DataParam
					(
						e.Key,
						value.DataType,
						DAE.Language.Modifier.In,
						value.IsNil ? null : (value is IScalar ? value.AsNative : value)
					));
				}
				paramsValue = paramsList.ToArray();
			}

			return paramsValue;
		}

		public static DataParams ParamsArrayToDataParams(IServerProcess process, DataParam[] paramsValue)
		{
			DataParams dataParams = new DataParams();
			if (paramsValue != null)
			{
				foreach (var dataParam in paramsValue)
				{
					dataParams.Add(dataParam);
				}
			}
			return dataParams;
		}

		public static IDataValue ValueToDataValue(ServerProcess process, object value)
		{
			// If it's a value type or String, send it through as a scalar
			if (value == null || value.GetType().IsValueType || value.GetType() == typeof(string))
			{
				return new Scalar(process.ValueManager, (IScalarType)ScalarTypeNameToDataType(process.DataTypes, value.GetType().Name), value);
			}

			// If it's a JObject, send it through as a row
			// I really don't want to do this (JSON is part of the communication layer, shouldn't be part of the processor here) but I don't want to build yet another pass-through structure to enable it when JObject is already that...
			var jObject = value as JObject;
			if (jObject != null)
			{
				var columns = new Columns();
				var values = new List<IDataValue>();
				foreach (var property in jObject)
				{
					var propertyValue = ValueToDataValue(process, property.Value);
					columns.Add(new Column(property.Key, GetDataType(propertyValue)));
					values.Add(propertyValue);
				}

				return new Row(process.ValueManager, new RowType(columns));
			}

			var jValue = value as JValue;
			if (jValue != null)
			{
				return ValueToDataValue(process, jValue.Value);
			}

			// If it's an array or collection, send it through as a List...
			if (value.GetType().IsArray || value is ICollection)
				// TODO: Handle list and table parameters
				throw new NotSupportedException("List parameters are not yet supported.");

			// Otherwise, send it through as a row
			return new Row(process.ValueManager, process.ValueManager.DataTypes.SystemRow);
		}

		public static IDataType GetDataType(object value)
		{
			Scalar scalarValue = value as Scalar;
			if (scalarValue != null)
				return scalarValue.DataType;

			ListValue listValue = value as ListValue;
			if (listValue != null)
				return listValue.DataType;

			Row rowValue = value as Row;
			if (rowValue != null)
				return rowValue.DataType;

			TableValue tableValue = value as TableValue;
			if (tableValue != null)
				return tableValue.DataType;

			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}

		public static IEnumerable<object> ServerCursorToValue(IServerProcess process, IServerCursor cursor)
		{
			var table = new List<object>();

			IRow currentRow = cursor.Plan.RequestRow();
			try
			{
				bool[] valueTypes = new bool[currentRow.DataType.Columns.Count];
				for (int index = 0; index < currentRow.DataType.Columns.Count; index++)
					valueTypes[index] = currentRow.DataType.Columns[index].DataType is IScalarType;

				while (cursor.Next())
				{
					cursor.Select(currentRow);
					var row = new Dictionary<string, object>();
					for (int index = 0; index < currentRow.DataType.Columns.Count; index++)
						if (valueTypes[index])
							row.Add(currentRow.DataType.Columns[index].Name, currentRow[index]);
						else
							row.Add(currentRow.DataType.Columns[index].Name, DataValueToValue(process, currentRow.GetValue(index)));

					table.Add(row);
				}
			}
			finally
			{
				cursor.Plan.ReleaseRow(currentRow);
			}

			return table;
		}

		public static object DataValueToValue(IServerProcess process, IDataValue dataValue)
		{
			if (dataValue == null)
				return null;

			IScalar scalar = dataValue as IScalar;
			if (scalar != null)
			{
				return ScalarTypeNameToValue(dataValue.DataType.Name, scalar);
			}

			ListValue list = dataValue as ListValue;
			if (list != null)
			{
				var listValue = new List<object>();
				if (!list.IsNil)
				{
					for (int index = 0; index < list.Count(); index++)
					{
						listValue.Add(DataValueToValue(process, list.GetValue(index)));
					}
				}
				return listValue;
			}

			IRow row = dataValue as IRow;
			if (row != null)
			{
				var rowValue = new Dictionary<string, object>();

				if (!row.IsNil)
				{
					for (int index = 0; index < row.DataType.Columns.Count; index++)
					{
						var data = row.GetValue(index);
						data.DataType.Name = DataTypeToDataTypeName(process.DataTypes, row.DataType.Columns[index].DataType);
						rowValue.Add(row.DataType.Columns[index].Name, DataValueToValue(process, data));
					}
				}
				return rowValue;
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
					var resultTable = new List<object>();

					if (!table.BOF())
						table.First();

					bool[] valueTypes = new bool[table.DataType.Columns.Count];
					for (int index = 0; index < table.DataType.Columns.Count; index++)
						valueTypes[index] = table.DataType.Columns[index].DataType is IScalarType;

					while (table.Next())
					{
						using (IRow currentRow = table.Select())
						{
							object[] nativeRow = new object[table.DataType.Columns.Count];
							for (int index = 0; index < table.DataType.Columns.Count; index++)
								if (valueTypes[index])
									resultTable.Add(currentRow[index]);
								else
									resultTable.Add(DataValueToValue(process, currentRow.GetValue(index)));
						}
					}

					return resultTable;
				}
			}
			finally
			{
				if (scan != null)
					scan.Dispose();
			}

			throw new NotSupportedException(string.Format("Values of type \"{0}\" are not supported.", dataValue.DataType.Name));
		}

		public static string DataTypeToDataTypeName(DataTypes dataTypes, IDataType dataType)
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

		public static string ScalarTypeToDataTypeName(DataTypes dataTypes, IScalarType scalarType)
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

		public static IDataType ScalarTypeNameToDataType(DataTypes dataTypes, string dataTypeName)
		{
			switch (dataTypeName.ToLower())
			{
				case "byte[]": return dataTypes.SystemBinary;
				case "bool":
				case "boolean": return dataTypes.SystemBoolean;
				case "byte": return dataTypes.SystemByte;
				case "date": return dataTypes.SystemDate;
				case "datetime": return dataTypes.SystemDateTime;
				case "decimal": return dataTypes.SystemDecimal;
				case "exception": return dataTypes.SystemError;
				case "guid": return dataTypes.SystemGuid;
				case "int32":
				case "integer": return dataTypes.SystemInteger;
				case "int64":
				case "long": return dataTypes.SystemLong;
				case "money": return dataTypes.SystemMoney;
				case "int16":
				case "short": return dataTypes.SystemShort;
				case "string": return dataTypes.SystemString;
				case "time": return dataTypes.SystemTime;
				case "timespan": return dataTypes.SystemTimeSpan;
				default:
					throw new ArgumentException(string.Format("Invalid native data type name: \"{0}\".", dataTypeName));
			}
		}

		public static object ScalarTypeNameToValue(string dataTypeName, IScalar dataTypeValue)
		{
			switch (dataTypeName.ToLower())
			{
				case "byte[]": return dataTypeValue.AsByte;
				case "bool":
				case "boolean": return dataTypeValue.AsBoolean;
				case "byte": return dataTypeValue.AsByte;
				case "date": return dataTypeValue.AsDateTime;
				case "datetime": return dataTypeValue.AsDateTime;
				case "decimal": return dataTypeValue.AsDecimal;
				case "exception": return new ArgumentException(string.Format("Exception: \"{0}\".", dataTypeName));
				case "guid": return dataTypeValue.AsGuid;
				case "int32":
				case "integer": return dataTypeValue.AsInt32;
				case "int64":
				case "long": return dataTypeValue.AsInt64;
				case "money": return dataTypeValue.AsDecimal;
				case "int16":
				case "short": return dataTypeValue.AsInt16;
				case "string": return dataTypeValue.IsNil ? null : dataTypeValue.AsString;
				case "time": return dataTypeValue.AsDateTime;
				case "timespan": return dataTypeValue.AsTimeSpan;
				default:
					throw new ArgumentException(string.Format("Invalid native data type name: \"{0}\".", dataTypeName));
			}
		}
	}
}
