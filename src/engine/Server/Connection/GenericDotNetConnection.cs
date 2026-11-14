/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Alphora.Dataphor.DAE.Connection
{
	/// <summary>
	/// Implements a generic crap wrapper that utilizes the ADO.NET 2.0 DbProviderFactory infrastructure to establish connectivity.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use of this connection is NOT RECOMMENDED for anything, ever. There are too many behaviors that are simply ignored
	/// by the DbProviderFactory implementation that are required for proper functioning of a connectivity implementation within
	/// the DAE. It is STRONGLY RECOMMENDED that a specific connectivity implementation be built that can establish these behaviors
	/// with respect to the system to which connectivity is being established.
	/// </para>
	/// <para>
	/// At least the following problems are identified with this connection:
	///		No mechanism for indicating whether or not an exception resulted in a transaction failure. Pretty important.
	///		No mechanism for determining the serverity of an exception. Again, pretty important.
	///		There seems to be no type mapping for BCD numerics, or CLOB data in the generic DbType enumeration.
	/// </para>
	/// </remarks>
	public class GenericDotNetConnection : DotNetConnection
	{
		public GenericDotNetConnection(string providerInvariantName, string connectionString) : base()
		{ 
			ProviderInvariantName = providerInvariantName;
			InternalConnect(connectionString);
		}
		
		private string _providerInvariantName;
		/// <summary>
		/// Gets or sets the provider invariant name used to obtain the DbProviderFactory instance for this connection.
		/// </summary>
		/// <remarks>
		/// The provider invariant name cannot be set once the provider factory has been built for this connection.
		/// </remarks>
		public string ProviderInvariantName
		{
			get { return _providerInvariantName; }
			set 
			{ 
				if (_providerInvariantName != value)
				{
					if (_providerFactory != null)
						throw new ConnectionException(ConnectionException.Codes.ProviderFactoryAlreadyConstructed);
						
					if (String.IsNullOrEmpty(value))
						throw new ConnectionException(ConnectionException.Codes.ProviderInvariantNameRequired);
						
					_providerInvariantName = value; 
				}
			}
		}
		
		private DbProviderFactory _providerFactory;
		/// <summary>
		/// Gets the provider factory used for this connection.
		/// </summary>
		protected internal DbProviderFactory ProviderFactory
		{
			get
			{
				if (_providerFactory == null)
					_providerFactory = DbProviderFactories.GetFactory(_providerInvariantName);
				return _providerFactory;
			}
		}
		
		protected override IDbConnection CreateDbConnection(string connectionString)
		{
			IDbConnection connection = ProviderFactory.CreateConnection();
			connection.ConnectionString = connectionString;
			return connection;
		}
		
		protected override SQLCommand InternalCreateCommand()
		{
			return new GenericDotNetCommand(this, CreateDbCommand());
		}

		// TODO: Generic mechanism for whether or not an exception indicates a transaction failure? Not likely...
		// TODO: Generic mechanism for determining the severity of an exception? Again, not likely...
	}
	
	public class GenericDotNetCommand : DotNetCommand
	{
		public GenericDotNetCommand(GenericDotNetConnection connection, IDbCommand command) : base(connection, command) 
		{
			_useOrdinalBinding = true;
		}
		
		public new GenericDotNetConnection Connection { get { return (GenericDotNetConnection)base.Connection; } }
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter parameter;
			for (int index = 0; index < _parameterIndexes.Length; index++)
			{
				parameter = Parameters[_parameterIndexes[index]];
				DbParameter dBParameter = Connection.ProviderFactory.CreateParameter();
				dBParameter.ParameterName = String.Format("@{0}", parameter.Name);
				switch (parameter.Direction)
				{
					case SQLDirection.Out : dBParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : dBParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : dBParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : dBParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					dBParameter.DbType = DbType.String;
					dBParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					dBParameter.DbType = DbType.Boolean;
				}
				else if (parameter.Type is SQLByteArrayType)
				{
					dBParameter.DbType = DbType.Binary;
					dBParameter.Size = ((SQLByteArrayType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1 : dBParameter.DbType = DbType.Byte; break;
						case 2 : dBParameter.DbType = DbType.Int16; break;
						case 8 : dBParameter.DbType = DbType.Int64; break;
						default : dBParameter.DbType = DbType.Int32; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					SQLNumericType type = (SQLNumericType)parameter.Type;
					dBParameter.DbType = DbType.Decimal;
					// No ability to specify scale and precision generically. Gracias.
					//LDBParameter.Scale = LType.Scale;
					//LDBParameter.Precision = LType.Precision;
				}
				else if (parameter.Type is SQLFloatType)
				{
					SQLFloatType type = (SQLFloatType)parameter.Type;
					if (type.Width == 1)
						dBParameter.DbType = DbType.Single;
					else
						dBParameter.DbType = DbType.Double;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					dBParameter.DbType = DbType.Binary;
				}
				else if (parameter.Type is SQLTextType)
				{
					dBParameter.DbType = DbType.String; // Hmmmm.... seems like this mapping is wrong. Can't find a clob type in the DbType enumeration.
				}
				else if (parameter.Type is SQLDateType)
				{
					dBParameter.DbType = DbType.Date;
				}
				else if (parameter.Type is SQLTimeType)
				{
					dBParameter.DbType = DbType.Time;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					dBParameter.DbType = DbType.DateTime;
				}
				else if (parameter.Type is SQLGuidType)
				{
					dBParameter.DbType = DbType.Guid;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					dBParameter.DbType = DbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(dBParameter);
			}
		}
	}
}
