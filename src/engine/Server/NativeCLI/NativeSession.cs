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
			// Stamped here so that a session which has not been called yet is never already idle.
			_lastActivityTicks = Environment.TickCount64;
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
		
		#region Lifetime Management

		/*
			Strategy: the server stamps this session's clock on its way into and out of every call that names its handle,
			and counts the calls in flight on it. How long the session has been without a call is then what tells an idle
			watcher that nothing is using it - the same job RemoteServerConnection.LastActivityTime does for a client
			connection on the main interface, and the shape here is deliberately that one's. Two differences, both on
			purpose: this clock is monotonic rather than wall-clock (see the field below), and it moves on every operation
			rather than on a ping alone, because the native interface has no connection object for a ping to land on - a
			client that wants to say "still here" has to make a call, which is what NativeCLISession's keep-alive does.

			The three members below are internal rather than public because each of them is only correct under that lock,
			which nothing outside this assembly can take: a caller that counted a call in and not out would leave the
			session looking busy for ever, and one that counted a call out twice would leave a busy session looking idle.
			NativeServer.GetSessionIdleSeconds is the public read, and says there why it is one.
		*/

		/// <summary>When a call last entered or left this session, in milliseconds since the machine started.</summary>
		/// <remarks>
		/// <c>Environment.TickCount64</c>, not <c>DateTime.Now</c>, which is what the main interface's connection clock
		/// (<see cref="Alphora.Dataphor.DAE.Server.RemoteServerConnection.LastActivityTime"/>) uses: a wall clock that
		/// steps - a daylight-saving change, an operator setting the time, a large time-synchronization correction - makes
		/// every stamp look an hour or more old at once, so a watcher reading it reclaims every live session in one sweep.
		/// That is bug-idle-clocks-use-wall-clock-time, filed against the connection clock, and it is not repeated here. A
		/// signed 64-bit millisecond count does not wrap within any machine's lifetime - unlike the 32-bit
		/// <c>Environment.TickCount</c> - so nothing here guards against a machine that has been up a long time. Written
		/// only under NativeServer's session lock, with the count below.
		/// </remarks>
		private long _lastActivityTicks;

		/// <summary>How many calls are in flight on this session.</summary>
		/// <remarks>
		/// A plain int under NativeServer's session lock rather than an interlocked counter, because an idle watcher has to
		/// read this and the session table together, atomically, to decide: a session it has found idle must not be able to
		/// take a call between the read and the removal.
		/// </remarks>
		private int _inUseCount;

		/// <summary>Counts a call as started on this session and stamps its clock; call only under NativeServer's session lock.</summary>
		internal void EnterCall()
		{
			_inUseCount++;
			_lastActivityTicks = Environment.TickCount64;
		}

		/// <summary>Counts a call as finished on this session and stamps its clock again; call only under NativeServer's session lock.</summary>
		/// <remarks>
		/// Stamped on the way out as well as on the way in, so that a call which ran for an hour does not leave an hour-old
		/// stamp behind it and look abandoned the moment it finished.
		/// </remarks>
		internal void ExitCall()
		{
			_inUseCount--;
			_lastActivityTicks = Environment.TickCount64;
		}

		/// <summary>How long this session has been without a call, in seconds; 0 while a call is in flight.</summary>
		/// <remarks>Read only under NativeServer's session lock, which is what the two fields behind it are written under.</remarks>
		internal long IdleSeconds
		{
			get
			{
				if (_inUseCount > 0)
					return 0;
				return (Environment.TickCount64 - _lastActivityTicks) / 1000;
			}
		}

		#endregion

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
