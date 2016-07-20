/*
	Dataphor
	© Copyright 2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using CacheClient = InterSystems.Data.CacheClient;

namespace Alphora.Dataphor.DAE.Connection.Cache
{
	public class CacheCommand : DotNetCommand
	{
		public CacheCommand(CacheConnection connection, IDbCommand command)
			: base(connection, command) 
		{
			_useOrdinalBinding = true;
		}

		protected override void PrepareParameters()
		{
			// Prepare parameters
			SQLParameter parameter;
			for (int index = 0; index < _parameterIndexes.Length; index++)
			{
				parameter = Parameters[_parameterIndexes[index]];
				var cacheParameter = (CacheClient.CacheParameter)_command.CreateParameter();
				cacheParameter.ParameterName = String.Format("{0}{1}", parameter.Name, index);
				cacheParameter.IsNullable = true;
				switch (parameter.Direction)
				{
					case SQLDirection.Out : cacheParameter.Direction = ParameterDirection.Output; break;
					case SQLDirection.InOut : cacheParameter.Direction = ParameterDirection.InputOutput; break;
					case SQLDirection.Result : cacheParameter.Direction = ParameterDirection.ReturnValue; break;
					default : cacheParameter.Direction = ParameterDirection.Input; break;
				}

				if (parameter.Type is SQLStringType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.NVarChar;
					cacheParameter.Size = ((SQLStringType)parameter.Type).Length;
				}
				else if (parameter.Type is SQLBooleanType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.Bit;
				}
				else if (parameter.Type is SQLIntegerType)
				{
					switch (((SQLIntegerType)parameter.Type).ByteCount)
					{
						case 1: cacheParameter.CacheDbType = CacheClient.CacheDbType.SmallInt; break;
						case 2: cacheParameter.CacheDbType = CacheClient.CacheDbType.SmallInt; break;
						case 8 :
							cacheParameter.CacheDbType = CacheClient.CacheDbType.Numeric; 
							cacheParameter.Precision = 20;
							cacheParameter.Scale = 0;
							break;
						default: cacheParameter.CacheDbType = CacheClient.CacheDbType.Int; break;
					}
				}
				else if (parameter.Type is SQLNumericType)
				{
					var type = (SQLNumericType)parameter.Type;
					cacheParameter.CacheDbType = CacheClient.CacheDbType.Numeric;
					cacheParameter.Precision = type.Precision;
					cacheParameter.Scale = type.Scale;
				}
				else if (parameter.Type is SQLBinaryType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.VarBinary;
				}
				else if (parameter.Type is SQLTextType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.LongVarChar;
				}
				else if (parameter.Type is SQLDateTimeType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.DateTime;
				}
				else if (parameter.Type is SQLDateType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.Date;
				}
				else if (parameter.Type is SQLTimeType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.Time;
				}
				else if (parameter.Type is SQLGuidType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.UniqueIdentifier;
				}
				else if (parameter.Type is SQLMoneyType)
				{
					cacheParameter.CacheDbType = CacheClient.CacheDbType.Numeric;
				}
				else
					throw new ConnectionException(ConnectionException.Codes.UnknownSQLDataType, parameter.Type.GetType().Name);
				_command.Parameters.Add(cacheParameter);
			}
		}
	}
}