using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Server
{
	/// <summary>
	/// Provides a utility class for translating values to and from 
	/// their Native representations for use in Native CLI calls.
	/// </summary>
	public static class NativeMarshal
	{
		public static ScalarType DataTypeNameToScalarType(IServerProcess AProcess, string ANativeDataTypeName)
		{
			switch (ANativeDataTypeName.ToLower())
			{
				case "byte[]" : return AProcess.DataTypes.SystemBinary;
				case "bool" :
				case "boolean" : return AProcess.DataTypes.SystemBoolean;
				case "byte" : return AProcess.DataTypes.SystemByte;
				case "date" : return AProcess.DataTypes.SystemDate;
				case "datetime" : return AProcess.DataTypes.SystemDateTime;
				case "decimal" : return AProcess.DataTypes.SystemDecimal;
				case "exception" : return AProcess.DataTypes.SystemError;
				case "guid" : return AProcess.DataTypes.SystemGuid;
				case "int32" :
				case "integer" : return AProcess.DataTypes.SystemInteger;
				case "int64" :
				case "long" : return AProcess.DataTypes.SystemLong;
				case "money" : return AProcess.DataTypes.SystemMoney;
				case "int16" :
				case "short" : return AProcess.DataTypes.SystemShort;
				case "string" : return AProcess.DataTypes.SystemString;
				case "time" : return AProcess.DataTypes.SystemTime;
				case "timespan" : return AProcess.DataTypes.SystemTimeSpan;
				default: throw new ArgumentException(String.Format("Invalid native data type name: \"{0}\".", ANativeDataTypeName));
			}
		}
		
		public static string DataTypeToDataTypeName(IServerProcess AProcess, IDataType ADataType)
		{
			ScalarType LScalarType = ADataType as ScalarType;
			if (LScalarType != null)
				return ScalarTypeToDataTypeName(AProcess, LScalarType);
			
			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}
		
		public static string ScalarTypeToDataTypeName(IServerProcess AProcess, ScalarType AScalarType)
		{
			if (AScalarType.NativeType == NativeAccessors.AsBoolean.NativeType) return "Boolean";
			if (AScalarType.NativeType == NativeAccessors.AsByte.NativeType) return "Byte";
			if (AScalarType.NativeType == NativeAccessors.AsByteArray.NativeType) return "Byte[]";
			if (AScalarType.NativeType == NativeAccessors.AsDateTime.NativeType)
			{
				if (AScalarType.Is(AProcess.DataTypes.SystemDateTime)) return "DateTime";
				if (AScalarType.Is(AProcess.DataTypes.SystemDate)) return "Date";
				if (AScalarType.Is(AProcess.DataTypes.SystemTime)) return "Time";
			}
			if (AScalarType.NativeType == NativeAccessors.AsDecimal.NativeType) return "Decimal";
			if (AScalarType.NativeType == NativeAccessors.AsException.NativeType) return "Exception";
			if (AScalarType.NativeType == NativeAccessors.AsGuid.NativeType) return "Guid";
			if (AScalarType.NativeType == NativeAccessors.AsInt16.NativeType) return "Int16";
			if (AScalarType.NativeType == NativeAccessors.AsInt32.NativeType) return "Int32";
			if (AScalarType.NativeType == NativeAccessors.AsInt64.NativeType) return "Int64";
			if (AScalarType.NativeType == NativeAccessors.AsString.NativeType) return "String";
			if (AScalarType.NativeType == NativeAccessors.AsTimeSpan.NativeType) return "TimeSpan";
			throw new ArgumentException(String.Format("Scalar type \"{0}\" has no native type.", AScalarType.Name));
		}
		
		public static NativeColumn[] ColumnsToNativeColumns(IServerProcess AProcess, Columns AColumns)
		{
			NativeColumn[] LColumns = new NativeColumn[AColumns.Count];
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				LColumns[LIndex] = 
					new NativeColumn 
					{ 
						Name = AColumns[LIndex].Name, 
						DataTypeName = DataTypeToDataTypeName(AProcess, AColumns[LIndex].DataType) 
					};
			
			return LColumns;
		}
		
		public static DataValue NativeValueToDataValue(IServerProcess AProcess, string ANativeDataTypeName, object ANativeValue)
		{
			ScalarType LScalarType = DataTypeNameToScalarType(AProcess, ANativeDataTypeName);
			return new Scalar(AProcess, LScalarType, ANativeValue);
		}
		
		public static Columns NativeColumnsToColumns(IServerProcess AProcess, NativeColumn[] ANativeColumns)
		{
			Columns LColumns = new Columns();
			for (int LIndex = 0; LIndex < ANativeColumns.Length; LIndex++)
				LColumns.Add(new Column(ANativeColumns[LIndex].Name, DataTypeNameToScalarType(AProcess, ANativeColumns[LIndex].DataTypeName)));
			return LColumns;
		}
		
		public static TableVar NativeTableToTableVar(ServerProcess AProcess, NativeTableValue ANativeTable)
		{
			TableType LTableType = new TableType();
			foreach (Column LColumn in NativeColumnsToColumns(AProcess, ANativeTable.Columns))
				LTableType.Columns.Add(LColumn);

			BaseTableVar LTableVar = new BaseTableVar(LTableType);
			LTableVar.EnsureTableVarColumns();
			LTableVar.EnsureKey(AProcess.Plan); // TODO: Transport key information for table-valued results?
			return LTableVar;
		}
		
		public static DataValue NativeValueToDataValue(ServerProcess AProcess, NativeValue ANativeValue)
		{
			NativeScalarValue LNativeScalar = ANativeValue as NativeScalarValue;
			if (LNativeScalar != null)
				return NativeValueToDataValue(AProcess, LNativeScalar.DataTypeName, LNativeScalar.Value);
			
			NativeListValue LNativeList = ANativeValue as NativeListValue;
			if (LNativeList != null)
			{
				ListValue LList = new ListValue(AProcess, AProcess.DataTypes.SystemList, LNativeList.Elements == null ? null : new NativeList());
				if (LNativeList.Elements != null)
					for (int LIndex = 0; LIndex < LNativeList.Elements.Length; LIndex++)
						LList.Add(NativeValueToDataValue(AProcess, LNativeList.Elements[LIndex]));
				return LList;
			}
			
			NativeRowValue LNativeRow = ANativeValue as NativeRowValue;
			if (LNativeRow != null)
			{
				Row LRow = new Row(AProcess, new Schema.RowType(NativeColumnsToColumns(AProcess, LNativeRow.Columns)));
				if (LNativeRow.Values == null)
					LRow.AsNative = null;
				else
				{
					for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
						LRow[LIndex] = NativeValueToDataValue(AProcess, LNativeRow.Columns[LIndex].DataTypeName, LNativeRow.Values[LIndex]);
				}
				return LRow;
			}
			
			NativeTableValue LNativeTable = ANativeValue as NativeTableValue;
			if (LNativeTable != null)
			{
				NativeTable LInternalTable = new NativeTable(AProcess, NativeTableToTableVar(AProcess, LNativeTable));
				TableValue LTable = new TableValue(AProcess, LInternalTable); 
				if (LNativeTable.Rows == null)
					LTable.AsNative = null;
				else
				{
					for (int LIndex = 0; LIndex < LNativeTable.Rows.Length; LIndex++)
					{
						Row LRow = new Row(AProcess, LInternalTable.RowType);
						try
						{
							for (int LColumnIndex = 0; LColumnIndex < LNativeTable.Rows[LIndex].Length; LColumnIndex++)
								LRow[LColumnIndex] = NativeValueToDataValue(AProcess, LNativeTable.Columns[LColumnIndex].DataTypeName, LNativeTable.Rows[LIndex][LColumnIndex]);
							LInternalTable.Insert(AProcess, LRow);
						}
						catch (Exception E)
						{
							LRow.Dispose();
							throw;
						}
					}
				}
				return LTable;
			}
			
			throw new NotSupportedException(String.Format("Unknown native value type: \"{0}\".", ANativeValue.GetType().Name));
		}
		
		public static NativeValue DataValueToNativeValue(IServerProcess AProcess, DataValue ADataValue)
		{
			Scalar LScalar = ADataValue as Scalar;
			if (LScalar != null)
			{
				NativeScalarValue LNativeScalar = new NativeScalarValue();
				LNativeScalar.DataTypeName = ScalarTypeToDataTypeName(AProcess, LScalar.DataType);
				LNativeScalar.Value = ADataValue.IsNil ? null : DataValueToNativeValue(AProcess, LScalar);
				return LNativeScalar;
			}
			
			ListValue LList = ADataValue as ListValue;
			if (LList != null)
			{
				NativeListValue LNativeList = new NativeListValue();
				if (!LList.IsNil)
				{
					LNativeList.Elements = new NativeValue[LList.Count()];
					for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
						LNativeList.Elements[LIndex] = DataValueToNativeValue(AProcess, LList[LIndex]);
				}
				return LNativeList;
			}
			
			Row LRow = ADataValue as Row;
			if (LRow != null)
			{
				NativeRowValue LNativeRow = new NativeRowValue();
				LNativeRow.Columns = ColumnsToNativeColumns(AProcess, LRow.DataType.Columns);
					
				if (!LRow.IsNil)
				{
					LNativeRow.Values = new object[LNativeRow.Columns.Length];
					for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
						LNativeRow.Values[LIndex] = LRow.HasValue(LIndex) ? LRow[LIndex].AsNative : null;
				}
			}
			
			Table LTable = ADataValue as Table;
			if (LTable != null)
			{
				NativeTableValue LNativeTable = new NativeTableValue();
				LNativeTable.Columns = ColumnsToNativeColumns(AProcess, LTable.DataType.Columns);
					
				if (!LTable.IsNil)
				{
					List<object[]> LNativeRows = new List<object[]>();

					if (!LTable.BOF())
						LTable.First();
						
					while (LTable.Next())
					{
						using (Row LCurrentRow = LTable.Select())
						{
							object[] LNativeRow = new object[LNativeTable.Columns.Length];
							for (int LIndex = 0; LIndex < LNativeTable.Columns.Length; LIndex++)
								LNativeRow[LIndex] = LCurrentRow.HasValue(LIndex) ? LCurrentRow[LIndex].AsNative : null;
							LNativeRows.Add(LNativeRow);
						}
					}
					
					LNativeTable.Rows = LNativeRows.ToArray();
				}
			}
			
			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", ADataValue.DataType.Name));
		}
		
		public static NativeValue ServerCursorToNativeValue(IServerProcess AProcess, IServerCursor ACursor)
		{
			NativeTableValue LNativeTable = new NativeTableValue();
			ITableType LTableType = ACursor.Plan.TableVar.DataType;
			LNativeTable.Columns = ColumnsToNativeColumns(AProcess, LTableType.Columns);
				
			List<object[]> LNativeRows = new List<object[]>();
			
			Row LCurrentRow = ACursor.Plan.RequestRow();
			try
			{
				while (ACursor.Next())
				{
					ACursor.Select(LCurrentRow);
					object[] LNativeRow = new object[LNativeTable.Columns.Length];
					for (int LIndex = 0; LIndex < LNativeTable.Columns.Length; LIndex++)
						LNativeRow[LIndex] = LCurrentRow.HasValue(LIndex) ? LCurrentRow[LIndex].AsNative : null;
					LNativeRows.Add(LNativeRow);
				}
			}
			finally
			{
				ACursor.Plan.ReleaseRow(LCurrentRow);
			}
			
			LNativeTable.Rows = LNativeRows.ToArray();
			
			return LNativeTable;
		}
		
		public static DataParams NativeParamsToDataParams(IServerProcess AProcess, NativeParam[] ANativeParams)
		{
			DataParams LDataParams = new DataParams();
			for (int LIndex = 0; LIndex < ANativeParams.Length; LIndex++)
			{
				NativeParam LNativeParam = ANativeParams[LIndex];
				DataValue LParamValue = NativeValueToDataValue(AProcess, LNativeParam.DataTypeName, LNativeParam.Value);
				DataParam LDataParam = new DataParam(LNativeParam.Name, LParamValue.DataType, NativeCLIUtility.NativeModifierToModifier(LNativeParam.Modifier), LParamValue);
				LDataParams.Add(LDataParam);
			}
			return LDataParams;
		}
		
		public static void SetNativeOutputParams(IServerProcess AProcess, NativeParam[] ANativeParams, DataParams ADataParams)
		{
			for (int LIndex = 0; LIndex < ANativeParams.Length; LIndex++)
			{
				NativeParam LNativeParam = ANativeParams[LIndex];
				if ((LNativeParam.Modifier == NativeModifier.Var) || (LNativeParam.Modifier == NativeModifier.Out))
					LNativeParam.Value = DataValueToNativeValue(AProcess, ADataParams[LIndex].Value);
			}
		}
		
		public static NativeParam[] DataParamsToNativeParams(IServerProcess AProcess, DataParams ADataParams)
		{
			NativeParam[] LNativeParams = new NativeParam[ADataParams.Count];
			for (int LIndex = 0; LIndex < ADataParams.Count; LIndex++)
			{
				DataParam LDataParam = ADataParams[LIndex];
				LNativeParams[LIndex] =	
					new NativeParam() 
					{
						Name = LDataParam.Name, 
						DataTypeName = DataTypeToDataTypeName(AProcess, LDataParam.DataType),
						Modifier = NativeCLIUtility.ModifierToNativeModifier(LDataParam.Modifier),
						Value = LDataParam.Value == null ? null : DataValueToNativeValue(AProcess, LDataParam.Value)
					};
			}
			return LNativeParams;
		}
		
		public static void SetDataOutputParams(IServerProcess AProcess, DataParams ADataParams, NativeParam[] ANativeParams)
		{
			for (int LIndex = 0; LIndex < ADataParams.Count; LIndex++)
			{
				DataParam LDataParam = ADataParams[LIndex];
				if ((LDataParam.Modifier == Modifier.Var) || (LDataParam.Modifier == Modifier.Out))
					LDataParam.Value = NativeValueToDataValue(AProcess, ANativeParams[LIndex].DataTypeName, ANativeParams[LIndex].Value);
			}
		}
	}
}
