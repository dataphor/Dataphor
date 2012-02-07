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

		public DAECommand(IContainer container)
		{
			Initialize(String.Empty, null, null);
			if (container != null)
				container.Add(this);
		}

		public DAECommand(string commandText)
		{
			Initialize(commandText, null, null);
		}

		public DAECommand(string commandText, DAEConnection connection)
		{
			Initialize(commandText, connection, null);
		}

		public DAECommand(string commandText, DAEConnection connection, DAETransaction transaction)
		{
			Initialize(commandText, connection, transaction);
		}

		private void Initialize(string commandText, DAEConnection connection, DAETransaction transaction)
		{
			_commandText = commandText;
			Connection = connection;
			_transaction = transaction;
			_parameters = new DAEParameterCollection(this);
			_commandType = CommandType.Text;
			_updatedRowSource = UpdateRowSource.Both;
		}

		protected override void Dispose(bool disposing)
		{
			Cancel();
			base.Dispose(disposing);
		}

		private bool _designTimeVisible;
		public override bool DesignTimeVisible
		{
			get { return _designTimeVisible; }
			set { _designTimeVisible = value; }
		}

		private bool _dataAdapterInUpdate;
		internal protected bool DataAdapterInUpdate
		{
			get { return _dataAdapterInUpdate; }
			set 
			{
				if (_dataAdapterInUpdate != value)
					_dataAdapterInUpdate = value;
			}
		}

		private string _preparedCommandText = String.Empty;
		protected string PreparedCommandText
		{
			get { return _preparedCommandText; }
			set { _preparedCommandText = value; }
		}

		private string _commandText;
		[Category("Command")]
		public override string CommandText
		{
			get { return _commandText; }
			set
			{ 
				CheckNotPrepared();
				_commandText = value;
			}
		}

		private int _commandTimeout;
		[Description("Not implemented")]
		[Category("Command")]
		[Browsable(false)]
		public override int CommandTimeout
		{
			get { return _commandTimeout; }
			set
			{
				CheckNotPrepared();
				_commandTimeout = value;
			}
		}

		private CommandType _commandType;
		/// <summary>
		/// When CommandType is TableDirect multiple tables are not supported.
		/// CommandType StoredProcedure is not supported, use CommandType Text instead.
		/// </summary>
		[Category("Command")]
		[DefaultValue(CommandType.Text)]
		public override CommandType CommandType
		{
			get { return _commandType; }
			set
			{
				CheckNotPrepared();
				if ((value != CommandType.Text) && (value != CommandType.TableDirect))
					throw new ProviderException(ProviderException.Codes.UnsupportedCommandType, CommandType.ToString());
				_commandType = value;
			}
		}

		protected void ConnectionClose(object sender, EventArgs args)
		{
			Cancel();
		}

		private DAEConnection _connection;
		[Category("Command")]
		protected override DbConnection DbConnection
		{
			get { return _connection; }
			set
			{
				CheckNotPrepared();
				if (_connection != null)
					_connection.BeforeClose -= new EventHandler(ConnectionClose);
				if (value != null)
				{
					_connection = (DAEConnection)value;
					_connection.BeforeClose += new EventHandler(ConnectionClose);
				}
				else
					_connection = null;
			}
		}

		private DAEParameterCollection _parameters;

		[Category("Command")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.DAEParameterCollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public DAEParameterCollection DAEParameters
		{
			get { return _parameters; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override DbParameterCollection DbParameterCollection
		{
			get { return _parameters; }
		}

		private DAETransaction _transaction;
		[Category("Command")]
		protected override DbTransaction DbTransaction
		{
			get { return _transaction; }
			set
			{ 
				CheckNotPrepared();
				_transaction = (DAETransaction)value;
			}
		}

		private UpdateRowSource _updatedRowSource = UpdateRowSource.None;
		[Category("Command")]
		[DefaultValue(UpdateRowSource.Both)]
		public override UpdateRowSource UpdatedRowSource
		{
			get { return _updatedRowSource; }
			set
			{
				CheckNotPrepared();
				_updatedRowSource = value;
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
			get { return _preparedCommandText != String.Empty; }
		}

		public override void Cancel()
		{
			UnPrepare();
		}

		protected override DbParameter CreateDbParameter()
		{
			return new DAEParameter();
		}

		private DataParams _dAERuntimeParams = null;
		internal protected DataParams DAERuntimeParams
		{
			get { return _dAERuntimeParams; }
		}

		internal protected IServerPlan _plan;

		[Browsable(false)]
		public Schema.TableType TableType
		{
			get
			{
				if (_plan == null)
				{
					Prepare();
					_plan = _connection.ServerProcess.PrepareExpression(PreparedCommandText, _dAERuntimeParams);
				}
				return (_plan is IServerExpressionPlan) ? (Schema.TableType)((IServerExpressionPlan)_plan).DataType : null;
			}
		}

		public override int ExecuteNonQuery()
		{
			Prepare();
			try
			{
				if (_plan == null)
					_plan = _connection.ServerProcess.PrepareStatement(PreparedCommandText, _dAERuntimeParams);
			}
			catch (Exception AException)
			{
				throw new Exception(String.Format("Error preparing statement {0}. Details ({1})", PreparedCommandText, ExceptionUtility.BriefDescription(AException)), AException);
			}
			
			try
			{
				((IServerStatementPlan)_plan).Execute(_dAERuntimeParams);
			}
			catch (Exception AException)
			{
				throw new Exception(String.Format("Error Executing statement {0}. Details ({1})", PreparedCommandText, ExceptionUtility.BriefDescription(AException)), AException);
			}

			_parameters.UpdateParams(_dAERuntimeParams);
			return 0;	// Return 0 because Dataphor doesn't report modified rows
		}

		private CommandBehavior _behavior;
		[Browsable(false)]
		public CommandBehavior Behavior
		{
			get { return _behavior; }
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			//This if statement fixes a problem in DbDataAdapter Update abstraction. 
			//The ExecuteReader method calls ExecuteNonQuery if the the DAEDataAdapter is performing an Update.
			//It also returns a special DataReader where RecordsAffected is 1.
			//This satisfies the DbDataAdapter.Update abstraction requirements.
			//Without this if statement there is a lot of code to reproduce.
			//To remove this if statement, override DbDataAdapter.Update(DataRow[] ADataRows, DataTableMapping ATableMapping).
			if (_dataAdapterInUpdate)
			{
				ExecuteNonQuery();
				return new DAEDataReader(this);
			}
			Prepare();
			_behavior = behavior;
			if (_plan == null)
				_plan = _connection.ServerProcess.PrepareExpression(PreparedCommandText, _dAERuntimeParams);
			try
			{
				IServerCursor cursor = ((IServerExpressionPlan)_plan).Open(_dAERuntimeParams);
				try
				{
					_parameters.UpdateParams(_dAERuntimeParams);
					return new DAEDataReader(cursor, this);
				}
				catch
				{
					((IServerExpressionPlan)_plan).Close(cursor);
					throw;
				}
			}
			catch
			{
				if (_plan != null)
					_connection.ServerProcess.UnprepareExpression((IServerExpressionPlan)_plan);
				throw;
			}
		}

		public override object ExecuteScalar()
		{
			using (IDataReader reader = ExecuteReader())
			{
				if (!reader.Read())
					return DBNull.Value;
				else
					return reader.GetValue(0);
			}
		}

		protected string PrepareCommand(string command)
		{
			string result = command;
			if (_parameters.Count > 0)
			{
				if (_dAERuntimeParams == null)
					_dAERuntimeParams = new DataParams();
				foreach (DAEParameter parameter in _parameters)
					result = parameter.Prepare(result, this);
			}
			else
				_dAERuntimeParams = null;

			return result;
		}

		public void UnPrepare()
		{
			_preparedCommandText = String.Empty;
			_dAERuntimeParams = null;
			if (_plan != null)
			{
				IServerProcess planProcess = _plan.Process;
				if (planProcess != null)
				{
					if (_plan is IServerExpressionPlan)
						planProcess.UnprepareExpression((IServerExpressionPlan)_plan);
					else
						planProcess.UnprepareStatement((IServerStatementPlan)_plan);
				}
				_plan = null;
			}
		}

		public override void Prepare()
		{
			string command = _commandText;
			if (_preparedCommandText == String.Empty)
			{
				if (_connection == null)
					throw new ProviderException(ProviderException.Codes.ConnectionRequired);

				if (CommandType == CommandType.TableDirect)
					command = "select " + _commandText + ';';
			}
			_preparedCommandText = PrepareCommand(command);
		}

		public virtual object Clone()
		{
			DAECommand newInstance = new DAECommand();
			newInstance.CommandText = _commandText;
			newInstance.CommandType = _commandType;
			newInstance.UpdatedRowSource = _updatedRowSource;
			foreach(DAEParameter param in _parameters)
				newInstance.Parameters.Add(param.Clone());
			newInstance.Connection = _connection;
			newInstance.Transaction = _transaction;
			newInstance.CommandTimeout = _commandTimeout;
			return newInstance;
		}
	}

	public class DAEParameterCollection : DbParameterCollection, IDataParameterCollection
	{
		public DAEParameterCollection(DAECommand command) : base()
		{
			_command = command;
			_items = new List<DAEParameter>();
		}

		private List<DAEParameter> _items;

		private DAECommand _command;

		public DAEParameter[] All
		{
			get
			{
				DAEParameter[] parameters = new DAEParameter[Count];
				for(int i = 0; i < Count; i++)
					parameters[i] = _items[i];
				return parameters;
			}
		}

		public override bool Contains(string parameterName)
		{
			return IndexOf(parameterName) > -1;
		}

		public override int IndexOf(string parameterName)
		{
			int index = 0;
			foreach(DAEParameter parameter in this) 
			{
				if (0 == CultureAwareCompare(parameter.ParameterName, parameterName))
					return index;
				index++;
			}
			return -1;
		}

		public override int Count { get { return _items.Count; } }
		public override bool IsFixedSize { get { return false; } }
		public override bool IsReadOnly { get { return false; } }
		public override bool IsSynchronized { get { return false; } }
		public override object SyncRoot { get { return this; } }
		public override int Add(object value)
		{
			DAEParameter localValue = (DAEParameter)value;
			_items.Add(localValue);
			return IndexOf(localValue);
		}
		public override void AddRange(Array values)
		{
			foreach (DAEParameter param in values)
				Add(param);
		}
		public override void Clear()
		{
			_items.Clear();
		}
		public override bool Contains(object value)
		{
			return _items.Contains((DAEParameter)value);
		}
		public override void CopyTo(Array array, int index)
		{
			foreach (DAEParameter param in _items)
			{
				array.SetValue(param, index);
				index++;
			}
		}
		public override IEnumerator GetEnumerator()
		{
			return _items.GetEnumerator();
		}
		protected override DbParameter GetParameter(int index)
		{
			return _items[index];
		}
		protected override DbParameter GetParameter(string name)
		{
			return _items[IndexOf(name)];
		}
		public override int IndexOf(object value)
		{
			return _items.IndexOf((DAEParameter)value);
		}
		public override void Insert(int index, object value)
		{
			_items.Insert(index, (DAEParameter)value);
		}
		public override void Remove(object value)
		{
			_items.Remove((DAEParameter)value);
		}
		public override void RemoveAt(int index)
		{
			_items.RemoveAt(index);
		}
		public override void RemoveAt(string name)
		{
			_items.RemoveAt(IndexOf(name));
		}
		protected override void SetParameter(int index, DbParameter value)
		{
			_items[index] = (DAEParameter)value;
		}
		protected override void SetParameter(string name, DbParameter value)
		{
			int index = IndexOf(name);
			if (index >= 0)
				_items.RemoveAt(index);
			_items.Add((DAEParameter)value);
		}

		public int Add(string parameterName, DAEDbType type)
		{
			return Add(new DAEParameter(parameterName, type));
		}

		public int Add(string parameterName, object value)
		{
			return Add(new DAEParameter(parameterName, value));
		}

		public int Add(string parameterName, DAEDbType type, string sourceColumnName)
		{
			return Add(new DAEParameter(parameterName, type, sourceColumnName));
		}

		public void UpdateParams(DataParams dAERuntimeParams)
		{
			if (dAERuntimeParams == null)
				return;
			foreach (DataParam dAERuntimeParam in dAERuntimeParams)
			{
				if ((dAERuntimeParam.Modifier == Language.Modifier.Out) || (dAERuntimeParam.Modifier == Language.Modifier.Var))
					((DAEParameter)this[dAERuntimeParam.Name]).Value = dAERuntimeParam.Value;
			}
		}

		private static int CultureAwareCompare(string stringA, string stringB)
		{
			return CultureInfo.CurrentCulture.CompareInfo.Compare(stringA, stringB, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase);
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
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(context, destinationType);
		}

		protected virtual int ShouldSerializeSignature(DAEParameter parameter)
		{
			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(parameter);
			int serializeSignature = 0;
			if (propertyDescriptors["ParameterName"].ShouldSerializeValue(parameter))
				serializeSignature |= 1;

			if (propertyDescriptors["DAEDbType"].ShouldSerializeValue(parameter))
				serializeSignature |= 2;
			
			if (propertyDescriptors["SourceColumn"].ShouldSerializeValue(parameter))
				serializeSignature |= 4;

			if 
				(
				propertyDescriptors["Size"].ShouldSerializeValue(parameter) ||
				propertyDescriptors["Direction"].ShouldSerializeValue(parameter) ||
				propertyDescriptors["IsNullable"].ShouldSerializeValue(parameter) ||
				propertyDescriptors["Precision"].ShouldSerializeValue(parameter) ||
				propertyDescriptors["Scale"].ShouldSerializeValue(parameter) ||
				propertyDescriptors["SourceVersion"].ShouldSerializeValue(parameter)
				)
				serializeSignature |= 8;

			return serializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(DAEParameter parameter)
		{
			ConstructorInfo info;
			Type type = parameter.GetType();
			switch (ShouldSerializeSignature(parameter))
			{
				case 0:
				case 1:
				case 2:
				case 3:
					info = type.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(DAEDbType)
							}
						);
					return new InstanceDescriptor
						(
						info,
						new object[]
							{
								parameter.ParameterName,
								parameter.DAEDbType
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								parameter.ParameterName,
								parameter.DAEDbType,
								parameter.SourceColumn
							}
						);
				default:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								parameter.ParameterName,
								parameter.DAEDbType,
								parameter.Size,
								parameter.Direction,
								parameter.IsNullable,
								parameter.Precision,
								parameter.Scale,
								parameter.SourceColumn,
								parameter.SourceVersion,
								parameter.Value
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			DAEParameter parameter = value as DAEParameter;
			if 
				(
				(destinationType == typeof(InstanceDescriptor)) &&
				(parameter != null)
				)
				return GetInstanceDescriptor(parameter);	
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Provider.DAEParameterConverter))]
	public class DAEParameter : DbParameter, IDbDataParameter, IDataParameter, ICloneable
	{
		public DAEParameter()
		{
			Initialize(String.Empty, DAEDbType.String, ParameterDirection.Input, true, "");
		}

		public DAEParameter(string name, DAEDbType type)
		{
			Initialize(name, type, ParameterDirection.Input, true, String.Empty);
		}

		public DAEParameter(string name, DAEDbType type, string sourceColumnName)
		{
			Initialize(name, type, ParameterDirection.Input, true, sourceColumnName);
		}

		public DAEParameter(string name, DAEDbType type, ParameterDirection direction)
		{
			Initialize(name, type, direction, true, String.Empty);
		}

		public DAEParameter(string name, object value)
		{
			_name = name;
			_value = value;
		}

		public DAEParameter
			(
			string name,
			DAEDbType type,
			int size,
			ParameterDirection direction,
			bool isNullable,
			byte precision,
			byte scale,
			string sourceColumnName,
			DataRowVersion srcVersion,
			object value
			)
		{
			_name = name;
			DAEDbType = type;
			_size = size;
			_direction = direction;
			_isNullable = isNullable;
			_precision = precision;
			_scale = scale;
			SourceColumn = sourceColumnName;
			_sourceVersion = srcVersion;
			_value = value;
		}

		private void Initialize(string name, DAEDbType type, ParameterDirection direction, bool isNullable, string sourceColumn)
		{
			_name = name;
			DAEDbType = type;
			_direction = direction;
			_isNullable = isNullable;
			SourceColumn = sourceColumn;
		}

		/// <summary> Maps a DAEDbType to an Dataphor Schema.DataType </summary>
		public static Schema.IDataType DataTypeFromDAEDbType(DAEDbType dAEDbType, DAEConnection connection)
		{
			switch (dAEDbType)
			{
				case DAEDbType.String : return connection.ServerProcess.DataTypes.SystemString;
				case DAEDbType.Boolean : return connection.ServerProcess.DataTypes.SystemBoolean;
				case DAEDbType.Byte : return connection.ServerProcess.DataTypes.SystemByte;
				case DAEDbType.Short : return connection.ServerProcess.DataTypes.SystemShort;
				case DAEDbType.Integer : return connection.ServerProcess.DataTypes.SystemInteger;
				case DAEDbType.Long : return connection.ServerProcess.DataTypes.SystemLong;
				case DAEDbType.Decimal : return connection.ServerProcess.DataTypes.SystemDecimal;
				case DAEDbType.GUID : return connection.ServerProcess.DataTypes.SystemGuid;
				case DAEDbType.Money : return connection.ServerProcess.DataTypes.SystemMoney;
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
				case DAEDbType.TimeSpan : return connection.ServerProcess.DataTypes.SystemTimeSpan;
				case DAEDbType.DateTime : return connection.ServerProcess.DataTypes.SystemDateTime;
				case DAEDbType.Raw : return connection.ServerProcess.DataTypes.SystemBinary;
				default : return connection.ServerProcess.DataTypes.SystemBinary;
			}
		}

		/// <summary> Maps a DAEDbType to a DBType. </summary>
		private DbType DbTypeFromDAEDbType(DAEDbType dAEDbType)
		{
			switch (dAEDbType)
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

		private DAEDbType DAEDbTypeFromDbType(DbType dbType)
		{

			switch (dbType)
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

		private DAEDbType _type = DAEDbType.String;
		[DefaultValue(DAEDbType.String)]
		[Description("The Dataphor DAE type.")]
		[Category("Data")]
		[RefreshProperties(RefreshProperties.All)]
		public DAEDbType DAEDbType
		{
			get { return _type; }
			set
			{
				if (_type != value)
				{
					_dbType = DbTypeFromDAEDbType(value);
					_type = value;
				}
			}
		}

		private DbType _dbType = DbType.String;
		public override DbType DbType
		{
			get { return _dbType; }
			set
			{
				if (_dbType != value)
				{
					_type = DAEDbTypeFromDbType(value);
					_dbType = value;
				}
			}
		}

		public override void ResetDbType()
		{
			DbType = DbType.String;
		}

		private ParameterDirection _direction = ParameterDirection.Input;
		public override ParameterDirection Direction
		{
			get { return _direction; }
			set { _direction = value; }
		}

		private bool _isNullable;
		public override bool IsNullable
		{
			get { return _isNullable; }
			set { _isNullable = value; }
		}

		private bool _sourceColumnNullMapping;
		public override bool SourceColumnNullMapping 
		{
			get { return _sourceColumnNullMapping; }
			set { _sourceColumnNullMapping = value; }
		}

		private string _name;
		public override string ParameterName
		{
			get { return _name; }
			set { _name = value; }
		}

		private string _sourceColumn;
		public override string SourceColumn
		{
			get { return _sourceColumn; }
			set { _sourceColumn = value; }
		}

		private DataRowVersion _sourceVersion = DataRowVersion.Current;
		public override DataRowVersion SourceVersion
		{
			get { return _sourceVersion; }
			set { _sourceVersion = value; }
		}

		private object _value;
		public override object Value
		{
			get { return _value; }
			set { _value = value; }
		}

		private byte _precision = 0;
		[DefaultValue(0)]
		[Category("Data")]
		public byte Precision
		{
			get { return _precision; }
			set { _precision = value; }
		}

		private byte _scale = 0;
		[DefaultValue(0)]
		[Category("Data")]
		public byte Scale
		{
			get { return _scale; }
			set { _scale = value; }
		}

		private int _size;
		/// <summary>
		/// Size is no-op for DAE.
		/// </summary>
		[DefaultValue(0)]
		public override int Size
		{
			get { return _size; }
			set { _size = value; }
		}

		public static Language.Modifier Modifier(ParameterDirection direction)
		{
			switch (direction)
			{
				case ParameterDirection.Input : return Language.Modifier.In;
				case ParameterDirection.InputOutput : return Language.Modifier.Var;
				case ParameterDirection.Output : return Language.Modifier.Out;
				case ParameterDirection.ReturnValue : return Language.Modifier.Out; //Return value not supported.
				default : return Language.Modifier.In;
			}
		}

		public static object NativeValue(Alphora.Dataphor.DAE.Runtime.Data.DataValue value, DAEConnection connection)
		{
			return value.AsNative;
		}

/*
 * Params use native values now, this is unnecessary
		public static Alphora.Dataphor.DAE.Runtime.Data.DataValue DataValue(Schema.IDataType AType, object AValue, DAECommand ACommand)
		{
			if (AValue == null)
				return null;
			return Alphora.Dataphor.DAE.Runtime.Data.DataValue.FromNative(((DAEConnection)ACommand.Connection).ServerProcess, AType, AValue);
		}
*/

		internal string Prepare(string commandText, DAECommand command)
		{
			if (commandText.IndexOf(ParameterName) > -1)
			{
				DataParam runtimeParam = null;
				if (command.DAERuntimeParams.IndexOf(ParameterName) < 0)
				{
					if ((Direction == ParameterDirection.Output) || (Direction == ParameterDirection.ReturnValue))
						runtimeParam = new DataParam(ParameterName, DataTypeFromDAEDbType(_type, (DAEConnection)command.Connection), Modifier(Direction));
					else
						runtimeParam = new DataParam(ParameterName, DataTypeFromDAEDbType(_type, (DAEConnection)command.Connection), Modifier(Direction), _value);
					command.DAERuntimeParams.Add(runtimeParam);
				}
				else
				{
					runtimeParam = command.DAERuntimeParams[ParameterName];
					if ((Direction != ParameterDirection.Output) && (Direction != ParameterDirection.ReturnValue))
						runtimeParam.Value = _value;
				}
			}
			return commandText;
		}

		public virtual object Clone()
		{
			return new DAEParameter(_name, _type, _size, _direction, _isNullable, _precision, _scale, _sourceColumn, _sourceVersion, _value);
		}
	}
}
