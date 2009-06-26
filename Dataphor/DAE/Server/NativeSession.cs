using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	public class NativeSession
	{
		public NativeSession(NativeSessionInfo ANativeSessionInfo)
		{
			FID = Guid.NewGuid();
			FNativeSessionInfo = ANativeSessionInfo;
		}
		
		private Guid FID;
		public Guid ID { get { return FID; } }
		
		private NativeSessionInfo FNativeSessionInfo;
		public NativeSessionInfo NativeSessionInfo { get { return FNativeSessionInfo; } }
		
		private SessionInfo FSessionInfo;
		public SessionInfo SessionInfo
		{
			get
			{
				if (FSessionInfo == null)
					FSessionInfo = NativeCLIUtility.NativeSessionInfoToSessionInfo(FNativeSessionInfo);
				return FSessionInfo;
			}
		}
		
		private IServerSession FSession;
		public IServerSession Session 
		{ 
			get { return FSession; } 
			set { FSession = value; }
		}
		
		private IServerProcess FProcess;
		public IServerProcess Process 
		{ 
			get { return FProcess; } 
			set { FProcess = value; }
		}
		
		private ScalarType DataTypeNameToScalarType(string ANativeDataTypeName)
		{
			switch (ANativeDataTypeName.ToLower())
			{
				case "byte[]" : return Process.DataTypes.SystemBinary;
				case "bool" :
				case "boolean" : return Process.DataTypes.SystemBoolean;
				case "byte" : return Process.DataTypes.SystemByte;
				case "date" : return Process.DataTypes.SystemDate;
				case "datetime" : return Process.DataTypes.SystemDateTime;
				case "decimal" : return Process.DataTypes.SystemDecimal;
				case "exception" : return Process.DataTypes.SystemError;
				case "guid" : return Process.DataTypes.SystemGuid;
				case "int32" :
				case "integer" : return Process.DataTypes.SystemInteger;
				case "int64" :
				case "long" : return Process.DataTypes.SystemLong;
				case "money" : return Process.DataTypes.SystemMoney;
				case "int16" :
				case "short" : return Process.DataTypes.SystemShort;
				case "string" : return Process.DataTypes.SystemString;
				case "time" : return Process.DataTypes.SystemTime;
				case "timespan" : return Process.DataTypes.SystemTimeSpan;
				default: throw new ArgumentException(String.Format("Invalid native data type name: \"{0}\".", ANativeDataTypeName));
			}
		}
		
		private string DataTypeToDataTypeName(IDataType ADataType)
		{
			ScalarType LScalarType = ADataType as ScalarType;
			if (LScalarType != null)
				return ScalarTypeToDataTypeName(LScalarType);
			
			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}
		
		private string ScalarTypeToDataTypeName(ScalarType AScalarType)
		{
			if (AScalarType.NativeType == NativeAccessors.AsBoolean.NativeType) return "Boolean";
			if (AScalarType.NativeType == NativeAccessors.AsByte.NativeType) return "Byte";
			if (AScalarType.NativeType == NativeAccessors.AsByteArray.NativeType) return "Byte[]";
			if (AScalarType.NativeType == NativeAccessors.AsDateTime.NativeType)
			{
				if (AScalarType.Is(Process.DataTypes.SystemDateTime)) return "DateTime";
				if (AScalarType.Is(Process.DataTypes.SystemDate)) return "Date";
				if (AScalarType.Is(Process.DataTypes.SystemTime)) return "Time";
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
		
		private DataValue NativeValueToDataValue(string ANativeDataTypeName, object ANativeValue)
		{
			ScalarType LScalarType = DataTypeNameToScalarType(ANativeDataTypeName);
			return new Scalar(Process, LScalarType, ANativeValue);
		}
		
		private NativeValue DataValueToNativeValue(DataValue ADataValue)
		{
			Scalar LScalar = ADataValue as Scalar;
			if (LScalar != null)
			{
				NativeScalarValue LNativeScalar = new NativeScalarValue();
				LNativeScalar.DataTypeName = ScalarTypeToDataTypeName(LScalar.DataType);
				LNativeScalar.Value = ADataValue.IsNil ? null : DataValueToNativeValue(LScalar);
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
						LNativeList.Elements[LIndex] = DataValueToNativeValue(LList[LIndex]);
				}
				return LNativeList;
			}
			
			Row LRow = ADataValue as Row;
			if (LRow != null)
			{
				NativeRowValue LNativeRow = new NativeRowValue();
				LNativeRow.Columns = new NativeColumn[LRow.DataType.Columns.Count];
				for (int LIndex = 0; LIndex < LRow.DataType.Columns.Count; LIndex++)
					LNativeRow.Columns[LIndex] = new NativeColumn { Name = LRow.DataType.Columns[LIndex].Name, DataTypeName = DataTypeToDataTypeName(LRow.DataType.Columns[LIndex].DataType) };
					
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
				LNativeTable.Columns = new NativeColumn[LTable.DataType.Columns.Count];
				for (int LIndex = 0; LIndex < LTable.DataType.Columns.Count; LIndex++)
					LNativeTable.Columns[LIndex] = new NativeColumn { Name = LTable.DataType.Columns[LIndex].Name, DataTypeName = DataTypeToDataTypeName(LTable.DataType.Columns[LIndex].DataType) };
					
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
		
		private NativeValue ServerCursorToNativeValue(IServerCursor ACursor)
		{
			NativeTableValue LNativeTable = new NativeTableValue();
			ITableType LTableType = ACursor.Plan.TableVar.DataType;
			LNativeTable.Columns = new NativeColumn[LTableType.Columns.Count];
			for (int LIndex = 0; LIndex < LTableType.Columns.Count; LIndex++)
				LNativeTable.Columns[LIndex] = new NativeColumn { Name = LTableType.Columns[LIndex].Name, DataTypeName = DataTypeToDataTypeName(LTableType.Columns[LIndex].DataType) };
				
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
				}
			}
			finally
			{
				ACursor.Plan.ReleaseRow(LCurrentRow);
			}
			
			LNativeTable.Rows = LNativeRows.ToArray();
			
			return LNativeTable;
		}
		
		private DataParams NativeParamsToDataParams(NativeParam[] ANativeParams)
		{
			DataParams LDataParams = new DataParams();
			for (int LIndex = 0; LIndex < ANativeParams.Length; LIndex++)
			{
				NativeParam LNativeParam = ANativeParams[LIndex];
				DataValue LParamValue = NativeValueToDataValue(LNativeParam.DataTypeName, LNativeParam.Value);
				DataParam LDataParam = new DataParam(LNativeParam.Name, LParamValue.DataType, NativeCLIUtility.NativeModifierToModifier(LNativeParam.Modifier), LParamValue);
				LDataParams.Add(LDataParam);
			}
			return LDataParams;
		}
		
		private void SetOutputParams(NativeParam[] ANativeParams, DataParams ADataParams)
		{
			for (int LIndex = 0; LIndex < ANativeParams.Length; LIndex++)
			{
				NativeParam LNativeParam = ANativeParams[LIndex];
				if ((LNativeParam.Modifier == NativeModifier.Var) || (LNativeParam.Modifier == NativeModifier.Out))
					LNativeParam.Value = DataValueToNativeValue(ADataParams[LIndex].Value);
			}
		}
		
		public NativeResult Execute(string AStatement, NativeParam[] AParams)
		{
			IServerScript LScript = FProcess.PrepareScript(AStatement);
			try
			{
				if (LScript.Batches.Count != 1)
					throw new ArgumentException("Execution statement must contain one, and only one, batch.");
					
				IServerBatch LBatch = LScript.Batches[0];
				DataParams LDataParams = NativeParamsToDataParams(AParams);
				NativeResult LResult = new NativeResult();
				LResult.Params = AParams;

				if (LBatch.IsExpression())
				{
					IServerExpressionPlan LExpressionPlan = LBatch.PrepareExpression(LDataParams);
					try
					{
						if (LExpressionPlan.DataType is Schema.TableType)
						{
							IServerCursor LCursor = LExpressionPlan.Open(LDataParams);
							try
							{
								LResult.Value = ServerCursorToNativeValue(LCursor);
							}
							finally
							{
								LExpressionPlan.Close(LCursor);
							}
						}
						else
						{
							LResult.Value = DataValueToNativeValue(LExpressionPlan.Evaluate(LDataParams));
						}
					}
					finally
					{
						LBatch.UnprepareExpression(LExpressionPlan);
					}
				}
				else
				{
					IServerStatementPlan LStatementPlan = LBatch.PrepareStatement(LDataParams);
					try
					{
						LStatementPlan.Execute(LDataParams);
					}
					finally
					{
						LBatch.UnprepareStatement(LStatementPlan);
					}
				}

				SetOutputParams(LResult.Params, LDataParams);
				return LResult;
			}
			finally
			{
				FProcess.UnprepareScript(LScript);
			}
		}
		
		public NativeResult[] Execute(NativeExecuteOperation[] AOperations)
		{
			NativeResult[] LResults = new NativeResult[AOperations.Length];
			for (int LIndex = 0; LIndex < AOperations.Length; LIndex++)
				LResults[LIndex] = Execute(AOperations[LIndex].Statement, AOperations[LIndex].Params);
				
			return LResults;
		}
	}
}
