/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public class NativeSession
	{
		public NativeSession(NativeSessionInfo nativeSessionInfo)
		{
			_iD = Guid.NewGuid();
			_nativeSessionInfo = nativeSessionInfo;
		}
		
		private Guid _iD;
		public Guid ID { get { return _iD; } }
		
		private NativeSessionInfo _nativeSessionInfo;
		public NativeSessionInfo NativeSessionInfo { get { return _nativeSessionInfo; } }
		
		private SessionInfo _sessionInfo;
		public SessionInfo SessionInfo
		{
			get
			{
				if (_sessionInfo == null)
					_sessionInfo = NativeCLIUtility.NativeSessionInfoToSessionInfo(_nativeSessionInfo);
				return _sessionInfo;
			}
		}
		
		private IServerSession _session;
		public IServerSession Session 
		{ 
			get { return _session; } 
			set { _session = value; }
		}
		
		private IServerProcess _process;
		public IServerProcess Process 
		{ 
			get { return _process; } 
			set { _process = value; }
		}
		
		public NativeResult Execute(string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			IServerScript script = _process.PrepareScript(statement);
			try
			{
				if (script.Batches.Count != 1)
					throw new ArgumentException("Execution statement must contain one, and only one, batch.");
					
				IServerBatch batch = script.Batches[0];
				DataParams dataParams = NativeMarshal.NativeParamsToDataParams((Server.ServerProcess)_process, paramsValue);
				NativeResult result = new NativeResult();
				result.Params = paramsValue;

				if (batch.IsExpression())
				{
					IServerExpressionPlan expressionPlan = batch.PrepareExpression(dataParams);
					try
					{
						if (expressionPlan.DataType is Schema.TableType)
						{
							if (options != NativeExecutionOptions.SchemaOnly)
							{
								IServerCursor cursor = expressionPlan.Open(dataParams);
								try
								{
									result.Value = NativeMarshal.ServerCursorToNativeValue(_process, cursor);
								}
								finally
								{
									expressionPlan.Close(cursor);
								}
							}
							else
							{
								result.Value = NativeMarshal.TableVarToNativeTableValue(_process, expressionPlan.TableVar);
							}
						}
						else
						{
							if (options != NativeExecutionOptions.SchemaOnly)
							{
								using (IDataValue tempValue = expressionPlan.Evaluate(dataParams))
								{
									result.Value = NativeMarshal.DataValueToNativeValue(_process, tempValue);
								}
							}
							else
							{
								result.Value = NativeMarshal.DataTypeToNativeValue(_process, expressionPlan.DataType);
							}
						}
					}
					finally
					{
						batch.UnprepareExpression(expressionPlan);
					}
				}
				else
				{
					IServerStatementPlan statementPlan = batch.PrepareStatement(dataParams);
					try
					{
						if (options != NativeExecutionOptions.SchemaOnly)
							statementPlan.Execute(dataParams);
					}
					finally
					{
						batch.UnprepareStatement(statementPlan);
					}
				}

				if (options != NativeExecutionOptions.SchemaOnly)
					NativeMarshal.SetNativeOutputParams(_process, result.Params, dataParams);
				return result;
			}
			finally
			{
				_process.UnprepareScript(script);
			}
		}
		
		public NativeResult[] Execute(NativeExecuteOperation[] operations)
		{
			NativeResult[] results = new NativeResult[operations.Length];
			for (int index = 0; index < operations.Length; index++)
				results[index] = Execute(operations[index].Statement, operations[index].Params, operations[index].Options);
				
			return results;
		}
	}
}
