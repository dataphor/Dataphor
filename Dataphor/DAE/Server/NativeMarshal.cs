using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Server
{
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.NativeCLI;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Compiling;

	/// <summary>
	/// Provides a utility class for translating values to and from 
	/// their Native representations for use in Native CLI calls.
	/// </summary>
	public static class NativeMarshal
	{
		public static ScalarType DataTypeNameToScalarType(Schema.DataTypes ADataTypes, string ANativeDataTypeName)
		{
			switch (ANativeDataTypeName.ToLower())
			{
				case "byte[]" : return ADataTypes.SystemBinary;
				case "bool" :
				case "boolean" : return ADataTypes.SystemBoolean;
				case "byte" : return ADataTypes.SystemByte;
				case "date" : return ADataTypes.SystemDate;
				case "datetime" : return ADataTypes.SystemDateTime;
				case "decimal" : return ADataTypes.SystemDecimal;
				case "exception" : return ADataTypes.SystemError;
				case "guid" : return ADataTypes.SystemGuid;
				case "int32" :
				case "integer" : return ADataTypes.SystemInteger;
				case "int64" :
				case "long" : return ADataTypes.SystemLong;
				case "money" : return ADataTypes.SystemMoney;
				case "int16" :
				case "short" : return ADataTypes.SystemShort;
				case "string" : return ADataTypes.SystemString;
				case "time" : return ADataTypes.SystemTime;
				case "timespan" : return ADataTypes.SystemTimeSpan;
				default: throw new ArgumentException(String.Format("Invalid native data type name: \"{0}\".", ANativeDataTypeName));
			}
		}
		
		public static string DataTypeToDataTypeName(Schema.DataTypes ADataTypes, IDataType ADataType)
		{
			ScalarType LScalarType = ADataType as ScalarType;
			if (LScalarType != null)
				return ScalarTypeToDataTypeName(ADataTypes, LScalarType);
			
			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}
		
		public static string ScalarTypeToDataTypeName(Schema.DataTypes ADataTypes, ScalarType AScalarType)
		{
			if (AScalarType.NativeType == NativeAccessors.AsBoolean.NativeType) return "Boolean";
			if (AScalarType.NativeType == NativeAccessors.AsByte.NativeType) return "Byte";
			if (AScalarType.NativeType == NativeAccessors.AsByteArray.NativeType) return "Byte[]";
			if (AScalarType.NativeType == NativeAccessors.AsDateTime.NativeType)
			{
				if (AScalarType.Is(ADataTypes.SystemDateTime)) return "DateTime";
				if (AScalarType.Is(ADataTypes.SystemDate)) return "Date";
				if (AScalarType.Is(ADataTypes.SystemTime)) return "Time";
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
		
		public static NativeColumn[] ColumnsToNativeColumns(Schema.DataTypes ADataTypes, Columns AColumns)
		{
			NativeColumn[] LColumns = new NativeColumn[AColumns.Count];
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				LColumns[LIndex] = 
					new NativeColumn 
					{ 
						Name = AColumns[LIndex].Name, 
						DataTypeName = DataTypeToDataTypeName(ADataTypes, AColumns[LIndex].DataType) 
					};
			
			return LColumns;
		}
		
		public static Columns NativeColumnsToColumns(Schema.DataTypes ADataTypes, NativeColumn[] ANativeColumns)
		{
			Columns LColumns = new Columns();
			for (int LIndex = 0; LIndex < ANativeColumns.Length; LIndex++)
				LColumns.Add(new Column(ANativeColumns[LIndex].Name, DataTypeNameToScalarType(ADataTypes, ANativeColumns[LIndex].DataTypeName)));
			return LColumns;
		}
		
		public static TableVar NativeTableToTableVar(ServerProcess AProcess, NativeTableValue ANativeTable)
		{
			using (Plan LPlan = new Plan(AProcess))
			{
				return NativeTableToTableVar(LPlan, ANativeTable);
			}
		}
		
		public static TableVar NativeTableToTableVar(Plan APlan, NativeTableValue ANativeTable)
		{
			TableType LTableType = new TableType();
			foreach (Column LColumn in NativeColumnsToColumns(APlan.DataTypes, ANativeTable.Columns))
				LTableType.Columns.Add(LColumn);

			BaseTableVar LTableVar = new BaseTableVar(LTableType);
			LTableVar.EnsureTableVarColumns();
			if (ANativeTable.Keys != null)
			{
				foreach (NativeKey LNativeKey in ANativeTable.Keys)
				{
					Key LKey = new Key();
					foreach (string LColumnName in LNativeKey.KeyColumns)
						LKey.Columns.Add(LTableVar.Columns[LColumnName]);
					LTableVar.Keys.Add(LKey);
				}
			}
			Compiler.EnsureKey(APlan, LTableVar);
			return LTableVar;
		}
		
		public static DataValue NativeValueToDataValue(ServerProcess AProcess, NativeValue ANativeValue)
		{
			NativeScalarValue LNativeScalar = ANativeValue as NativeScalarValue;
			if (LNativeScalar != null)
				return new Scalar(AProcess.ValueManager, DataTypeNameToScalarType(AProcess.DataTypes, LNativeScalar.DataTypeName), LNativeScalar.Value);
			
			NativeListValue LNativeList = ANativeValue as NativeListValue;
			if (LNativeList != null)
			{
				ListValue LList = new ListValue(AProcess.ValueManager, AProcess.DataTypes.SystemList, LNativeList.Elements == null ? null : new NativeList());
				if (LNativeList.Elements != null)
					for (int LIndex = 0; LIndex < LNativeList.Elements.Length; LIndex++)
						LList.Add(NativeValueToDataValue(AProcess, LNativeList.Elements[LIndex]));
				return LList;
			}
			
			NativeRowValue LNativeRow = ANativeValue as NativeRowValue;
			if (LNativeRow != null)
			{
				Row LRow = new Row(AProcess.ValueManager, new Schema.RowType(NativeColumnsToColumns(AProcess.DataTypes, LNativeRow.Columns)));
				if (LNativeRow.Values == null)
					LRow.AsNative = null;
				else
				{
					for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
						LRow[LIndex] = LNativeRow.Values[LIndex];
				}
				return LRow;
			}
			
			NativeTableValue LNativeTable = ANativeValue as NativeTableValue;
			if (LNativeTable != null)
			{
				NativeTable LInternalTable = new NativeTable(AProcess.ValueManager, NativeTableToTableVar(AProcess, LNativeTable));
				TableValue LTable = new TableValue(AProcess.ValueManager, LInternalTable); 
				if (LNativeTable.Rows == null)
					LTable.AsNative = null;
				else
				{
					for (int LIndex = 0; LIndex < LNativeTable.Rows.Length; LIndex++)
					{
						Row LRow = new Row(AProcess.ValueManager, LInternalTable.RowType);
						try
						{
							for (int LColumnIndex = 0; LColumnIndex < LNativeTable.Rows[LIndex].Length; LColumnIndex++)
								LRow[LColumnIndex] = LNativeTable.Rows[LIndex][LColumnIndex];
							LInternalTable.Insert(AProcess.ValueManager, LRow);
						}
						catch (Exception)
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
		
		public static NativeValue DataTypeToNativeValue(IServerProcess AProcess, IDataType ADataType)
		{
			ScalarType LScalarType = ADataType as ScalarType;
			if (LScalarType != null)
			{
				NativeScalarValue LNativeScalar = new NativeScalarValue();
				LNativeScalar.DataTypeName = ScalarTypeToDataTypeName(AProcess.DataTypes, LScalarType);
				return LNativeScalar;
			}
			
			ListType LListType = ADataType as ListType;
			if (LListType != null)
			{
				NativeListValue LNativeList = new NativeListValue();
				return LNativeList;
			}
			
			RowType LRowType = ADataType as RowType;
			if (LRowType != null)
			{
				NativeRowValue LNativeRow = new NativeRowValue();
				LNativeRow.Columns = ColumnsToNativeColumns(AProcess.DataTypes, LRowType.Columns);
				return LNativeRow;
			}
			
			TableType LTableType = ADataType as TableType;
			if (LTableType != null)
			{
				NativeTableValue LNativeTable = new NativeTableValue();
				LNativeTable.Columns = ColumnsToNativeColumns(AProcess.DataTypes, LTableType.Columns);
				return LNativeTable;
			}
				
			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", ADataType.Name));
		}
		
		public static NativeValue DataValueToNativeValue(IServerProcess AProcess, DataValue ADataValue)
		{
			Scalar LScalar = ADataValue as Scalar;
			if (LScalar != null)
			{
				NativeScalarValue LNativeScalar = new NativeScalarValue();
				LNativeScalar.DataTypeName = ScalarTypeToDataTypeName(AProcess.DataTypes, LScalar.DataType);
				LNativeScalar.Value = ADataValue.IsNil ? null : LScalar.AsNative;
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
						LNativeList.Elements[LIndex] = DataValueToNativeValue(AProcess, LList.GetValue(LIndex));
				}
				return LNativeList;
			}
			
			Row LRow = ADataValue as Row;
			if (LRow != null)
			{
				NativeRowValue LNativeRow = new NativeRowValue();
				LNativeRow.Columns = ColumnsToNativeColumns(AProcess.DataTypes, LRow.DataType.Columns);
					
				if (!LRow.IsNil)
				{
					LNativeRow.Values = new object[LNativeRow.Columns.Length];
					for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
						LNativeRow.Values[LIndex] = LRow[LIndex];
				}
				return LNativeRow;
			}
			
			Table LTable = ADataValue as Table;
			if (LTable != null)
			{
				NativeTableValue LNativeTable = new NativeTableValue();
				LNativeTable.Columns = ColumnsToNativeColumns(AProcess.DataTypes, LTable.DataType.Columns);
					
				List<object[]> LNativeRows = new List<object[]>();

				if (!LTable.BOF())
					LTable.First();
					
				while (LTable.Next())
				{
					using (Row LCurrentRow = LTable.Select())
					{
						object[] LNativeRow = new object[LNativeTable.Columns.Length];
						for (int LIndex = 0; LIndex < LNativeTable.Columns.Length; LIndex++)
							LNativeRow[LIndex] = LCurrentRow[LIndex];
						LNativeRows.Add(LNativeRow);
					}
				}
				
				LNativeTable.Rows = LNativeRows.ToArray();
				return LNativeTable;
			}
			
			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", ADataValue.DataType.Name));
		}
		
		public static NativeTableValue TableVarToNativeTableValue(IServerProcess AProcess, TableVar ATableVar)
		{
			NativeTableValue LNativeTable = new NativeTableValue();
			LNativeTable.Columns = ColumnsToNativeColumns(AProcess.DataTypes, ATableVar.DataType.Columns);
			LNativeTable.Keys = new NativeKey[ATableVar.Keys.Count];
			for (int LIndex = 0; LIndex < ATableVar.Keys.Count; LIndex++)
			{
				LNativeTable.Keys[LIndex] = new NativeKey();
				LNativeTable.Keys[LIndex].KeyColumns = ATableVar.Keys[LIndex].Columns.ColumnNames;
			}
			
			return LNativeTable;
		}
		
		public static NativeValue ServerCursorToNativeValue(IServerProcess AProcess, IServerCursor ACursor)
		{
			NativeTableValue LNativeTable = TableVarToNativeTableValue(AProcess, ACursor.Plan.TableVar);
				
			List<object[]> LNativeRows = new List<object[]>();
			
			Row LCurrentRow = ACursor.Plan.RequestRow();
			try
			{
				while (ACursor.Next())
				{
					ACursor.Select(LCurrentRow);
					object[] LNativeRow = new object[LNativeTable.Columns.Length];
					for (int LIndex = 0; LIndex < LNativeTable.Columns.Length; LIndex++)
						LNativeRow[LIndex] = LCurrentRow[LIndex];
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
				DataParam LDataParam = 
					new DataParam
					(
						LNativeParam.Name, 
						DataTypeNameToScalarType(AProcess.DataTypes, LNativeParam.DataTypeName), 
						NativeCLIUtility.NativeModifierToModifier(LNativeParam.Modifier), 
						LNativeParam.Value
					);
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
				{
					DataParam LDataParam = ADataParams[LIndex];
					LNativeParam.Value = LDataParam.Value;
				}
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
						DataTypeName = DataTypeToDataTypeName(AProcess.DataTypes, LDataParam.DataType),
						Modifier = NativeCLIUtility.ModifierToNativeModifier(LDataParam.Modifier),
						Value = LDataParam.Value
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
					LDataParam.Value = ANativeParams[LIndex].Value;
			}
		}
	}
}
