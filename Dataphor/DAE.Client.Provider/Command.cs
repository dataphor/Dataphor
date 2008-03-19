/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	/// <summary> Dataphor DAE Command class. </summary>
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Provider.DAECommand),"Icons.DAECommand.bmp")]
	public class DAECommand : DbCommand, ICloneable
	{
		public DAECommand()
		{
			Initialize(String.Empty, null, null);
		}

		public DAECommand(IContainer AContainer)
		{
			Initialize(String.Empty, null, null);
			if (AContainer != null)
				AContainer.Add(this);
		}

		public DAECommand(string ACommandText)
		{
			Initialize(ACommandText, null, null);
		}

		public DAECommand(string ACommandText, DAEConnection AConnection)
		{
			Initialize(ACommandText, AConnection, null);
		}

		public DAECommand(string ACommandText, DAEConnection AConnection, DAETransaction ATransaction)
		{
			Initialize(ACommandText, AConnection, ATransaction);
		}

		private void Initialize(string ACommandText, DAEConnection AConnection, DAETransaction ATransaction)
		{
			FCommandText = ACommandText;
			Connection = AConnection;
			FTransaction = ATransaction;
			FParameters = new DAEParameterCollection(this);
			FCommandType = CommandType.Text;
			FUpdatedRowSource = UpdateRowSource.Both;
		}

		protected override void Dispose(bool ADisposing)
		{
			Cancel();
			base.Dispose(ADisposing);
		}

		private bool FDesignTimeVisible;
		public override bool DesignTimeVisible
		{
			get { return FDesignTimeVisible; }
			set { FDesignTimeVisible = value; }
		}

		private bool FDataAdapterInUpdate;
		internal protected bool DataAdapterInUpdate
		{
			get { return FDataAdapterInUpdate; }
			set 
			{
				if (FDataAdapterInUpdate != value)
					FDataAdapterInUpdate = value;
			}
		}

		private string FPreparedCommandText = String.Empty;
		protected string PreparedCommandText
		{
			get { return FPreparedCommandText; }
			set { FPreparedCommandText = value; }
		}

		private string FCommandText;
		[Category("Command")]
		public override string CommandText
		{
			get { return FCommandText; }
			set
			{ 
				CheckNotPrepared();
				FCommandText = value;
			}
		}

		private int FCommandTimeout;
		[Description("Not implemented")]
		[Category("Command")]
		[Browsable(false)]
		public override int CommandTimeout
		{
			get { return FCommandTimeout; }
			set
			{
				CheckNotPrepared();
				FCommandTimeout = value;
			}
		}

		private CommandType FCommandType;
		/// <summary>
		/// When CommandType is TableDirect multiple tables are not supported.
		/// CommandType StoredProcedure is not supported, use CommandType Text instead.
		/// </summary>
		[Category("Command")]
		[DefaultValue(CommandType.Text)]
		public override CommandType CommandType
		{
			get { return FCommandType; }
			set
			{
				CheckNotPrepared();
				if ((value != CommandType.Text) && (value != CommandType.TableDirect))
					throw new ProviderException(ProviderException.Codes.UnsupportedCommandType, CommandType.ToString());
				FCommandType = value;
			}
		}

		protected void ConnectionClose(object ASender, EventArgs AArgs)
		{
			Cancel();
		}

		private DAEConnection FConnection;
		[Category("Command")]
		protected override DbConnection DbConnection
		{
			get { return FConnection; }
			set
			{
				CheckNotPrepared();
				if (FConnection != null)
					FConnection.BeforeClose -= new EventHandler(ConnectionClose);
				if (value != null)
				{
					FConnection = (DAEConnection)value;
					FConnection.BeforeClose += new EventHandler(ConnectionClose);
				}
				else
					FConnection = null;
			}
		}

		private DAEParameterCollection FParameters;

		[Category("Command")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.DAEParameterCollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public DAEParameterCollection DAEParameters
		{
			get { return FParameters; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override DbParameterCollection DbParameterCollection
		{
			get { return FParameters; }
		}

		private DAETransaction FTransaction;
		[Category("Command")]
		protected override DbTransaction DbTransaction
		{
			get { return FTransaction; }
			set
			{ 
				CheckNotPrepared();
				FTransaction = (DAETransaction)value;
			}
		}

		private UpdateRowSource FUpdatedRowSource = UpdateRowSource.None;
		[Category("Command")]
		[DefaultValue(UpdateRowSource.Both)]
		public override UpdateRowSource UpdatedRowSource
		{
			get { return FUpdatedRowSource; }
			set
			{
				CheckNotPrepared();
				FUpdatedRowSource = value;
			}
		}

		private void CheckNotPrepared()
		{
			if (IsPrepared)
				throw new ProviderException(ProviderException.Codes.CommandPrepared);
		}

		[Browsable(false)]
		public bool IsPrepared
		{
			get { return FPreparedCommandText != String.Empty; }
		}

		public override void Cancel()
		{
			UnPrepare();
		}

		protected override DbParameter CreateDbParameter()
		{
			return new DAEParameter();
		}

		private DataParams FDAERuntimeParams = null;
		internal protected DataParams DAERuntimeParams
		{
			get { return FDAERuntimeParams; }
		}

		internal protected IServerPlan FPlan;

		[Browsable(false)]
		public Schema.TableType TableType
		{
			get
			{
				if (FPlan == null)
				{
					Prepare();
					FPlan = FConnection.ServerProcess.PrepareExpression(PreparedCommandText, FDAERuntimeParams);
				}
				return (FPlan is IServerExpressionPlan) ? (Schema.TableType)((IServerExpressionPlan)FPlan).DataType : null;
			}
		}

		public override int ExecuteNonQuery()
		{
			Prepare();
			try
			{
				if (FPlan == null)
					FPlan = FConnection.ServerProcess.PrepareStatement(PreparedCommandText, FDAERuntimeParams);
			}
			catch (Exception AException)
			{
				throw new Exception(String.Format("Error preparing statement {0}. Details ({1})", PreparedCommandText, ExceptionUtility.BriefDescription(AException)), AException);
			}
			
			try
			{
				((IServerStatementPlan)FPlan).Execute(FDAERuntimeParams);
			}
			catch (Exception AException)
			{
				throw new Exception(String.Format("Error Executing statement {0}. Details ({1})", PreparedCommandText, ExceptionUtility.BriefDescription(AException)), AException);
			}

			FParameters.UpdateParams(FDAERuntimeParams);
			return 0;	// Return 0 because Dataphor doesn't report modified rows
		}

		private CommandBehavior FBehavior;
		[Browsable(false)]
		public CommandBehavior Behavior
		{
			get { return FBehavior; }
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior ABehavior)
		{
			//This if statement fixes a problem in DbDataAdapter Update abstraction. 
			//The ExecuteReader method calls ExecuteNonQuery if the the DAEDataAdapter is performing an Update.
			//It also returns a special DataReader where RecordsAffected is 1.
			//This satisfies the DbDataAdapter.Update abstraction requirements.
			//Without this if statement there is a lot of code to reproduce.
			//To remove this if statement, override DbDataAdapter.Update(DataRow[] ADataRows, DataTableMapping ATableMapping).
			if (FDataAdapterInUpdate)
			{
				ExecuteNonQuery();
				return new DAEDataReader(this);
			}
			Prepare();
			FBehavior = ABehavior;
			if (FPlan == null)
				FPlan = FConnection.ServerProcess.PrepareExpression(PreparedCommandText, FDAERuntimeParams);
			try
			{
				IServerCursor LCursor = ((IServerExpressionPlan)FPlan).Open(FDAERuntimeParams);
				try
				{
					FParameters.UpdateParams(FDAERuntimeParams);
					return new DAEDataReader(LCursor, this);
				}
				catch
				{
					((IServerExpressionPlan)FPlan).Close(LCursor);
					throw;
				}
			}
			catch
			{
				if (FPlan != null)
					FConnection.ServerProcess.UnprepareExpression((IServerExpressionPlan)FPlan);
				throw;
			}
		}

		public override object ExecuteScalar()
		{
			using (IDataReader LReader = ExecuteReader())
			{
				if (!LReader.Read())
					return DBNull.Value;
				else
					return LReader.GetValue(0);
			}
		}

		protected string PrepareCommand(string ACommand)
		{
			string LResult = ACommand;
			if (FParameters.Count > 0)
			{
				if (FDAERuntimeParams == null)
					FDAERuntimeParams = new DataParams();
				foreach (DAEParameter LParameter in FParameters)
					LResult = LParameter.Prepare(LResult, this);
			}
			else
				FDAERuntimeParams = null;

			return LResult;
		}

		public void UnPrepare()
		{
			FPreparedCommandText = String.Empty;
			FDAERuntimeParams = null;
			if (FPlan != null)
			{
				IServerProcess LPlanProcess = FPlan.Process;
				if (LPlanProcess != null)
				{
					if (FPlan is IServerExpressionPlan)
						LPlanProcess.UnprepareExpression((IServerExpressionPlan)FPlan);
					else
						LPlanProcess.UnprepareStatement((IServerStatementPlan)FPlan);
				}
				FPlan = null;
			}
		}

		public override void Prepare()
		{
			string LCommand = FCommandText;
			if (FPreparedCommandText == String.Empty)
			{
				if (FConnection == null)
					throw new ProviderException(ProviderException.Codes.ConnectionRequired);

				if (CommandType == CommandType.TableDirect)
					LCommand = "select " + FCommandText + ';';
			}
			FPreparedCommandText = PrepareCommand(LCommand);
		}

		public virtual object Clone()
		{
			DAECommand LNewInstance = new DAECommand();
			LNewInstance.CommandText = FCommandText;
			LNewInstance.CommandType = FCommandType;
			LNewInstance.UpdatedRowSource = FUpdatedRowSource;
			foreach(DAEParameter LParam in FParameters)
				LNewInstance.Parameters.Add(LParam.Clone());
			LNewInstance.Connection = FConnection;
			LNewInstance.Transaction = FTransaction;
			LNewInstance.CommandTimeout = FCommandTimeout;
			return LNewInstance;
		}
	}

	public class DAEParameterCollection : DbParameterCollection, IDataParameterCollection
	{
		public DAEParameterCollection(DAECommand ACommand) : base()
		{
			FCommand = ACommand;
			FItems = new List<DAEParameter>();
		}

		private List<DAEParameter> FItems;

		private DAECommand FCommand;

		public DAEParameter[] All
		{
			get
			{
				DAEParameter[] LParameters = new DAEParameter[Count];
				for(int i = 0; i < Count; i++)
					LParameters[i] = FItems[i];
				return LParameters;
			}
		}

		public override bool Contains(string AParameterName)
		{
			return IndexOf(AParameterName) > -1;
		}

		public override int IndexOf(string AParameterName)
		{
			int index = 0;
			foreach(DAEParameter LParameter in this) 
			{
				if (0 == CultureAwareCompare(LParameter.ParameterName, AParameterName))
					return index;
				index++;
			}
			return -1;
		}

		public override int Count { get { return FItems.Count; } }
		public override bool IsFixedSize { get { return false; } }
		public override bool IsReadOnly { get { return false; } }
		public override bool IsSynchronized { get { return false; } }
		public override object SyncRoot { get { return this; } }
		public override int Add(object AValue)
		{
			DAEParameter LValue = (DAEParameter)AValue;
			FItems.Add(LValue);
			return IndexOf(LValue);
		}
		public override void AddRange(Array AValues)
		{
			foreach (DAEParameter LParam in AValues)
				Add(LParam);
		}
		public override void Clear()
		{
			FItems.Clear();
		}
		public override bool Contains(object AValue)
		{
			return FItems.Contains((DAEParameter)AValue);
		}
		public override void CopyTo(Array AArray, int AIndex)
		{
			foreach (DAEParameter LParam in FItems)
			{
				AArray.SetValue(LParam, AIndex);
				AIndex++;
			}
		}
		public override IEnumerator GetEnumerator()
		{
			return FItems.GetEnumerator();
		}
		protected override DbParameter GetParameter(int AIndex)
		{
			return FItems[AIndex];
		}
		protected override DbParameter GetParameter(string AName)
		{
			return FItems[IndexOf(AName)];
		}
		public override int IndexOf(object AValue)
		{
			return FItems.IndexOf((DAEParameter)AValue);
		}
		public override void Insert(int AIndex, object AValue)
		{
			FItems.Insert(AIndex, (DAEParameter)AValue);
		}
		public override void Remove(object AValue)
		{
			FItems.Remove((DAEParameter)AValue);
		}
		public override void RemoveAt(int AIndex)
		{
			FItems.RemoveAt(AIndex);
		}
		public override void RemoveAt(string AName)
		{
			FItems.RemoveAt(IndexOf(AName));
		}
		protected override void SetParameter(int AIndex, DbParameter AValue)
		{
			FItems[AIndex] = (DAEParameter)AValue;
		}
		protected override void SetParameter(string AName, DbParameter AValue)
		{
			int LIndex = IndexOf(AName);
			if (LIndex >= 0)
				FItems.RemoveAt(LIndex);
			FItems.Add((DAEParameter)AValue);
		}

		public int Add(string AParameterName, DAEDbType AType)
		{
			return Add(new DAEParameter(AParameterName, AType));
		}

		public int Add(string AParameterName, object AValue)
		{
			return Add(new DAEParameter(AParameterName, AValue));
		}

		public int Add(string AParameterName, DAEDbType AType, string ASourceColumnName)
		{
			return Add(new DAEParameter(AParameterName, AType, ASourceColumnName));
		}

		public void UpdateParams(DataParams ADAERuntimeParams)
		{
			if (ADAERuntimeParams == null)
				return;
			foreach (DataParam LDAERuntimeParam in ADAERuntimeParams)
			{
				if ((LDAERuntimeParam.Modifier == Language.Modifier.Out) || (LDAERuntimeParam.Modifier == Language.Modifier.Var))
					((DAEParameter)this[LDAERuntimeParam.Name]).Value = DAEParameter.NativeValue(LDAERuntimeParam.Value, (DAEConnection)FCommand.Connection);
			}
		}

		private static int CultureAwareCompare(string AStringA, string AStringB)
		{
			return CultureInfo.CurrentCulture.CompareInfo.Compare(AStringA, AStringB, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase);
		}
	}

	public enum DAEDbType 
	{
		Raw, 
		Boolean, 
		Byte, 
		Short, 
		Integer, 
		Long, 
		#if UseUnsignedIntegers
		PByte, 
		PShort,
		PInteger,
		PLong,
		SByte, 
		UShort, 
		UInteger, 
		ULong, 
		#endif
		Decimal, 
		String, 
		TimeSpan, 
		DateTime, 
		Money, 
		GUID
	}

	public class DAEParameterConverter : TypeConverter
	{
		public DAEParameterConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext AContext, Type ADestinationType)
		{
			if (ADestinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(AContext, ADestinationType);
		}

		protected virtual int ShouldSerializeSignature(DAEParameter AParameter)
		{
			PropertyDescriptorCollection LPropertyDescriptors = TypeDescriptor.GetProperties(AParameter);
			int LSerializeSignature = 0;
			if (LPropertyDescriptors["ParameterName"].ShouldSerializeValue(AParameter))
				LSerializeSignature |= 1;

			if (LPropertyDescriptors["DAEDbType"].ShouldSerializeValue(AParameter))
				LSerializeSignature |= 2;
			
			if (LPropertyDescriptors["SourceColumn"].ShouldSerializeValue(AParameter))
				LSerializeSignature |= 4;

			if 
				(
				LPropertyDescriptors["Size"].ShouldSerializeValue(AParameter) ||
				LPropertyDescriptors["Direction"].ShouldSerializeValue(AParameter) ||
				LPropertyDescriptors["IsNullable"].ShouldSerializeValue(AParameter) ||
				LPropertyDescriptors["Precision"].ShouldSerializeValue(AParameter) ||
				LPropertyDescriptors["Scale"].ShouldSerializeValue(AParameter) ||
				LPropertyDescriptors["SourceVersion"].ShouldSerializeValue(AParameter)
				)
				LSerializeSignature |= 8;

			return LSerializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(DAEParameter AParameter)
		{
			ConstructorInfo LInfo;
			Type LType = AParameter.GetType();
			switch (ShouldSerializeSignature(AParameter))
			{
				case 0:
				case 1:
				case 2:
				case 3:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(DAEDbType)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AParameter.ParameterName,
								AParameter.DAEDbType
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(DAEDbType),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AParameter.ParameterName,
								AParameter.DAEDbType,
								AParameter.SourceColumn
							}
						);
				default:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(DAEDbType),
								typeof(int),
								typeof(ParameterDirection),
								typeof(bool),
								typeof(byte),
								typeof(byte),
								typeof(string),
								typeof(DataRowVersion),
								typeof(object)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AParameter.ParameterName,
								AParameter.DAEDbType,
								AParameter.Size,
								AParameter.Direction,
								AParameter.IsNullable,
								AParameter.Precision,
								AParameter.Scale,
								AParameter.SourceColumn,
								AParameter.SourceVersion,
								AParameter.Value
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, System.Globalization.CultureInfo ACulture, object AValue, Type ADestinationType)
		{
			DAEParameter LParameter = AValue as DAEParameter;
			if 
				(
				(ADestinationType == typeof(InstanceDescriptor)) &&
				(LParameter != null)
				)
				return GetInstanceDescriptor(LParameter);	
			return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Provider.DAEParameterConverter))]
	public class DAEParameter : DbParameter, IDbDataParameter, IDataParameter, ICloneable
	{
		public DAEParameter()
		{
			Initialize(String.Empty, DAEDbType.String, ParameterDirection.Input, true, "");
		}

		public DAEParameter(string AName, DAEDbType AType)
		{
			Initialize(AName, AType, ParameterDirection.Input, true, String.Empty);
		}

		public DAEParameter(string AName, DAEDbType AType, string ASourceColumnName)
		{
			Initialize(AName, AType, ParameterDirection.Input, true, ASourceColumnName);
		}

		public DAEParameter(string AName, DAEDbType AType, ParameterDirection ADirection)
		{
			Initialize(AName, AType, ADirection, true, String.Empty);
		}

		public DAEParameter(string AName, object AValue)
		{
			FName = AName;
			FValue = AValue;
		}

		public DAEParameter
			(
			string AName,
			DAEDbType AType,
			int ASize,
			ParameterDirection ADirection,
			bool AIsNullable,
			byte APrecision,
			byte AScale,
			string ASourceColumnName,
			DataRowVersion ASrcVersion,
			object AValue
			)
		{
			FName = AName;
			DAEDbType = AType;
			FSize = ASize;
			FDirection = ADirection;
			FIsNullable = AIsNullable;
			FPrecision = APrecision;
			FScale = AScale;
			SourceColumn = ASourceColumnName;
			FSourceVersion = ASrcVersion;
			FValue = AValue;
		}

		private void Initialize(string AName, DAEDbType AType, ParameterDirection ADirection, bool AIsNullable, string ASourceColumn)
		{
			FName = AName;
			DAEDbType = AType;
			FDirection = ADirection;
			FIsNullable = AIsNullable;
			SourceColumn = ASourceColumn;
		}

		/// <summary> Maps a DAEDbType to an Dataphor Schema.DataType </summary>
		public static Schema.IDataType DataTypeFromDAEDbType(DAEDbType ADAEDbType, DAEConnection AConnection)
		{
			switch (ADAEDbType)
			{
				case DAEDbType.String : return AConnection.ServerProcess.DataTypes.SystemString;
				case DAEDbType.Boolean : return AConnection.ServerProcess.DataTypes.SystemBoolean;
				case DAEDbType.Byte : return AConnection.ServerProcess.DataTypes.SystemByte;
				case DAEDbType.Short : return AConnection.ServerProcess.DataTypes.SystemShort;
				case DAEDbType.Integer : return AConnection.ServerProcess.DataTypes.SystemInteger;
				case DAEDbType.Long : return AConnection.ServerProcess.DataTypes.SystemLong;
				case DAEDbType.Decimal : return AConnection.ServerProcess.DataTypes.SystemDecimal;
				case DAEDbType.GUID : return AConnection.ServerProcess.DataTypes.SystemGuid;
				case DAEDbType.Money : return AConnection.ServerProcess.DataTypes.SystemMoney;
				#if UseUnsignedIntegers
				case DAEDbType.PByte : return AConnection.ServerProcess.DataTypes.SystemPByte;
				case DAEDbType.PShort : return AConnection.ServerProcess.DataTypes.SystemPShort;
				case DAEDbType.PInteger : return AConnection.ServerProcess.DataTypes.SystemPInteger;
				case DAEDbType.PLong : return AConnection.ServerProcess.DataTypes.SystemPLong
				case DAEDbType.SByte : return AConnection.ServerProcess.DataTypes.SystemSByte;
				case DAEDbType.UInteger : return AConnection.ServerProcess.DataTypes.SystemUInteger;
				case DAEDbType.ULong : return AConnection.ServerProcess.DataTypes.SystemULong;
				case DAEDbType.UShort : return AConnection.ServerProcess.DataTypes.SystemUShort;
				#endif
				case DAEDbType.TimeSpan : return AConnection.ServerProcess.DataTypes.SystemTimeSpan;
				case DAEDbType.DateTime : return AConnection.ServerProcess.DataTypes.SystemDateTime;
				case DAEDbType.Raw : return AConnection.ServerProcess.DataTypes.SystemBinary;
				default : return AConnection.ServerProcess.DataTypes.SystemBinary;
			}
		}

		/// <summary> Maps a DAEDbType to a DBType. </summary>
		private DbType DbTypeFromDAEDbType(DAEDbType ADAEDbType)
		{
			switch (ADAEDbType)
			{
				case DAEDbType.Boolean : return DbType.Boolean;
				case DAEDbType.Byte : return DbType.Byte;
				case DAEDbType.DateTime : return DbType.DateTime;
				case DAEDbType.Decimal : return DbType.Decimal;
				//Double is not supported by the DAE, doubles are treated as decimals.
				case DAEDbType.GUID : return DbType.Guid;
				case DAEDbType.Integer : return DbType.Int32;
				case DAEDbType.Long : return DbType.Int64;
				case DAEDbType.Short : return DbType.Int16;
				case DAEDbType.Money : return DbType.Currency;
				#if UseUnsignedIntegers
				case DAEDbType.PByte : return DbType.Byte;
				case DAEDbType.PShort : return DbType.UInt16;
				case DAEDbType.PInteger : return DbType.UInt32;
				case DAEDbType.PLong : return DbType.UInt64;
				case DAEDbType.SByte : return DbType.SByte;
				case DAEDbType.UShort : return DbType.UInt16;
				case DAEDbType.UInteger : return DbType.UInt32;
				case DAEDbType.ULong : return DbType.UInt64;
				#endif
				case DAEDbType.String : return DbType.String;
				case DAEDbType.TimeSpan : return DbType.Int64;
				case DAEDbType.Raw : return DbType.Binary;
				default : return DbType.Binary;
			}
		}

		private DAEDbType DAEDbTypeFromDbType(DbType ADbType)
		{

			switch (ADbType)
			{
				case DbType.String : 
				case DbType.StringFixedLength :
				case DbType.AnsiString :
				case DbType.AnsiStringFixedLength : return DAEDbType.String;
				case DbType.Boolean : return DAEDbType.Boolean;
				case DbType.Byte : return DAEDbType.Byte;
				case DbType.Currency : return DAEDbType.Money;
				case DbType.Date : 
				case DbType.Time :
				case DbType.DateTime : return DAEDbType.DateTime;
					//Double is not supported by the DAE, doubles are treated as decimals.
				case DbType.Double :
				case DbType.Decimal : return DAEDbType.Decimal;
				case DbType.Guid : return DAEDbType.GUID;
				case DbType.Int16 : return DAEDbType.Short;
				case DbType.Int32 : return DAEDbType.Integer;
				case DbType.Int64 : return DAEDbType.Long;
				case DbType.Single : return DAEDbType.Decimal;
				#if UseUnsignedIntegers
				case DbType.SByte : return DAEDbType.SByte;
				case DbType.UInt16 : return DAEDbType.UShort;
				case DbType.UInt32 : return DAEDbType.UInteger;
				case DbType.UInt64 : return DAEDbType.ULong;
				#else
				case DbType.SByte : return DAEDbType.Short;
				case DbType.UInt16 : return DAEDbType.Integer;
				case DbType.UInt32 : return DAEDbType.Long;
				case DbType.UInt64 : return DAEDbType.Decimal;
				#endif
				case DbType.Binary :
				case DbType.Object :
				case DbType.VarNumeric : return DAEDbType.Raw;
				default : return DAEDbType.Raw;
			}
		}

		private DAEDbType FType = DAEDbType.String;
		[DefaultValue(DAEDbType.String)]
		[Description("The Dataphor DAE type.")]
		[Category("Data")]
		[RefreshProperties(RefreshProperties.All)]
		public DAEDbType DAEDbType
		{
			get { return FType; }
			set
			{
				if (FType != value)
				{
					FDbType = DbTypeFromDAEDbType(value);
					FType = value;
				}
			}
		}

		private DbType FDbType = DbType.String;
		public override DbType DbType
		{
			get { return FDbType; }
			set
			{
				if (FDbType != value)
				{
					FType = DAEDbTypeFromDbType(value);
					FDbType = value;
				}
			}
		}

		public override void ResetDbType()
		{
			DbType = DbType.String;
		}

		private ParameterDirection FDirection = ParameterDirection.Input;
		public override ParameterDirection Direction
		{
			get { return FDirection; }
			set { FDirection = value; }
		}

		private bool FIsNullable;
		public override bool IsNullable
		{
			get { return FIsNullable; }
			set { FIsNullable = value; }
		}

		private bool FSourceColumnNullMapping;
		public override bool SourceColumnNullMapping 
		{
			get { return FSourceColumnNullMapping; }
			set { FSourceColumnNullMapping = value; }
		}

		private string FName;
		public override string ParameterName
		{
			get { return FName; }
			set { FName = value; }
		}

		private string FSourceColumn;
		public override string SourceColumn
		{
			get { return FSourceColumn; }
			set { FSourceColumn = value; }
		}

		private DataRowVersion FSourceVersion = DataRowVersion.Current;
		public override DataRowVersion SourceVersion
		{
			get { return FSourceVersion; }
			set { FSourceVersion = value; }
		}

		private object FValue;
		public override object Value
		{
			get { return FValue; }
			set { FValue = value; }
		}

		private byte FPrecision = 0;
		[DefaultValue(0)]
		[Category("Data")]
		public byte Precision
		{
			get { return FPrecision; }
			set { FPrecision = value; }
		}

		private byte FScale = 0;
		[DefaultValue(0)]
		[Category("Data")]
		public byte Scale
		{
			get { return FScale; }
			set { FScale = value; }
		}

		private int FSize;
		/// <summary>
		/// Size is no-op for DAE.
		/// </summary>
		[DefaultValue(0)]
		public override int Size
		{
			get { return FSize; }
			set { FSize = value; }
		}

		public static Language.Modifier Modifier(ParameterDirection ADirection)
		{
			switch (ADirection)
			{
				case ParameterDirection.Input : return Language.Modifier.In;
				case ParameterDirection.InputOutput : return Language.Modifier.Var;
				case ParameterDirection.Output : return Language.Modifier.Out;
				case ParameterDirection.ReturnValue : return Language.Modifier.Out; //Return value not supported.
				default : return Language.Modifier.In;
			}
		}

		public static object NativeValue(Alphora.Dataphor.DAE.Runtime.Data.DataValue AValue, DAEConnection AConnection)
		{
			return AValue.AsNative;
		}

		public static Alphora.Dataphor.DAE.Runtime.Data.DataValue DataValue(Schema.IDataType AType, object AValue, DAECommand ACommand)
		{
			if (AValue == null)
				return null;
			return Alphora.Dataphor.DAE.Runtime.Data.DataValue.FromNative(((DAEConnection)ACommand.Connection).ServerProcess, AType, AValue);
		}

		internal string Prepare(string ACommandText, DAECommand ACommand)
		{
			if (ACommandText.IndexOf(ParameterName) > -1)
			{
				DataParam LRuntimeParam = null;
				if (ACommand.DAERuntimeParams.IndexOf(ParameterName) < 0)
				{
					if ((Direction == ParameterDirection.Output) || (Direction == ParameterDirection.ReturnValue))
						LRuntimeParam = new DataParam(ParameterName, DataTypeFromDAEDbType(FType, (DAEConnection)ACommand.Connection), Modifier(Direction));
					else
						LRuntimeParam = new DataParam(ParameterName, DataTypeFromDAEDbType(FType, (DAEConnection)ACommand.Connection), Modifier(Direction), DataValue(DataTypeFromDAEDbType(FType, (DAEConnection)ACommand.Connection), FValue, ACommand));
					ACommand.DAERuntimeParams.Add(LRuntimeParam);
				}
				else
				{
					LRuntimeParam = ACommand.DAERuntimeParams[ParameterName];
					if ((Direction != ParameterDirection.Output) && (Direction != ParameterDirection.ReturnValue))
						LRuntimeParam.Value = DataValue(DataTypeFromDAEDbType(FType, (DAEConnection)ACommand.Connection), FValue, ACommand);
				}
			}
			return ACommandText;
		}

		public virtual object Clone()
		{
			return new DAEParameter(FName, FType, FSize, FDirection, FIsNullable, FPrecision, FScale, FSourceColumn, FSourceVersion, FValue);
		}
	}
}
