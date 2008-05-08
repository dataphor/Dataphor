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
		public GenericDotNetConnection(string AProviderInvariantName, string AConnectionString) : base()
		{ 
			ProviderInvariantName = AProviderInvariantName;
			InternalConnect(AConnectionString);
		}
		
		private string FProviderInvariantName;
		/// <summary>
		/// Gets or sets the provider invariant name used to obtain the DbProviderFactory instance for this connection.
		/// </summary>
		/// <remarks>
		/// The provider invariant name cannot be set once the provider factory has been built for this connection.
		/// </remarks>
		public string ProviderInvariantName
		{
			get { return FProviderInvariantName; }
			set 
			{ 
				if (FProviderInvariantName != value)
				{
					if (FProviderFactory != null)
						throw new ConnectionException(ConnectionException.Codes.ProviderFactoryAlreadyConstructed);
						
					if (String.IsNullOrEmpty(value))
						throw new ConnectionException(ConnectionException.Codes.ProviderInvariantNameRequired);
						
					FProviderInvariantName = value; 
				}
			}
		}
		
		private DbProviderFactory FProviderFactory;
		/// <summary>
		/// Gets the provider factory used for this connection.
		/// </summary>
		protected internal DbProviderFactory ProviderFactory
		{
			get
			{
				if (FProviderFactory == null)
					FProviderFactory = DbProviderFactories.GetFactory(FProviderInvariantName);
				return FProviderFactory;
			}
		}
		
		protected override IDbConnection CreateDbConnection(string AConnectionString)
		{
			IDbConnection LConnection = ProviderFactory.CreateConnection();
			LConnection.ConnectionString = AConnectionString;
			return LConnection;
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
		public GenericDotNetCommand(GenericDotNetConnection AConnection, IDbCommand ACommand) : base(AConnection, ACommand) 
		{
			FUseOrdinalBinding = true;
		}
		
		public new GenericDotNetConnection Connection { get { return (GenericDotNetConnection)base.Connection; } }
		
		protected override void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter LParameter;
			for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
			{
				LParameter = Parameters[FParameterIndexes[LIndex]];
				DbParameter LDBParameter = Connection.ProviderFactory.CreateParameter();
				LDBParameter.ParameterName = String.Format("@{0}", LParameter.Name);
				switch (LParameter.Direction)
				{
					case SQLDirection.Out : LDBParameter.Direction = System.Data.ParameterDirection.Output; break;
					case SQLDirection.InOut : LDBParameter.Direction = System.Data.ParameterDirection.InputOutput; break;
					case SQLDirection.Result : LDBParameter.Direction = System.Data.ParameterDirection.ReturnValue; break;
					default : LDBParameter.Direction = System.Data.ParameterDirection.Input; break;
				}

				if (LParameter.Type is SQLStringType)
				{
					LDBParameter.DbType = DbType.String;
					LDBParameter.Size = ((SQLStringType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLBooleanType)
				{
					LDBParameter.DbType = DbType.Boolean;
				}
				else if (LParameter.Type is SQLByteArrayType)
				{
					LDBParameter.DbType = DbType.Binary;
					LDBParameter.Size = ((SQLByteArrayType)LParameter.Type).Length;
				}
				else if (LParameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)LParameter.Type).ByteCount)
					{
						case 1 : LDBParameter.DbType = DbType.Byte; break;
						case 2 : LDBParameter.DbType = DbType.Int16; break;
						case 8 : LDBParameter.DbType = DbType.Int64; break;
						default : LDBParameter.DbType = DbType.Int32; break;
					}
				}
				else if (LParameter.Type is SQLNumericType)
				{
					SQLNumericType LType = (SQLNumericType)LParameter.Type;
					LDBParameter.DbType = DbType.Decimal;
					// No ability to specify scale and precision generically. Gracias.
					//LDBParameter.Scale = LType.Scale;
					//LDBParameter.Precision = LType.Precision;
				}
				else if (LParameter.Type is SQLFloatType)
				{
					SQLFloatType LType = (SQLFloatType)LParameter.Type;
					if (LType.Width == 1)
						LDBParameter.DbType = DbType.Single;
					else
						LDBParameter.DbType = DbType.Double;
				}
				else if (LParameter.Type is SQLBinaryType)
				{
					LDBParameter.DbType = DbType.Binary;
				}
				else if (LParameter.Type is SQLTextType)
				{
					LDBParameter.DbType = DbType.String; // Hmmmm.... seems like this mapping is wrong. Can't find a clob type in the DbType enumeration.
				}
				else if (LParameter.Type is SQLDateType)
				{
					LDBParameter.DbType = DbType.Date;
				}
				else if (LParameter.Type is SQLTimeType)
				{
					LDBParameter.DbType = DbType.Time;
				}
				else if (LParameter.Type is SQLDateTimeType)
				{
					LDBParameter.DbType = DbType.DateTime;
				}
				else if (LParameter.Type is SQLGuidType)
				{
					LDBParameter.DbType = DbType.Guid;
				}
				else if (LParameter.Type is SQLMoneyType)
				{
					LDBParameter.DbType = DbType.Currency;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, LParameter.Type.GetType().Name);
				FCommand.Parameters.Add(LDBParameter);
			}
		}
	}
}
