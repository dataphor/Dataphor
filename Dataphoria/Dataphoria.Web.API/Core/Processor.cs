using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.REST;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.Dataphoria.Processing
{
	public class Processor : IDisposable
	{
		public const string DefaultProcessorInstanceName = "Processor";
		private ServerConfiguration _configuration;
		private Server _server;

		private string _instanceName;
		public string InstanceName { get { return _instanceName; } }
		
		public Processor() : this(DefaultProcessorInstanceName)
		{
		}

		public Processor(string instanceName)
		{
			if (String.IsNullOrEmpty(instanceName))
			{
				throw new ArgumentNullException("instanceName");
			}

			_instanceName = instanceName;
			_configuration = InstanceManager.GetInstance(instanceName); // Don't necessarily need this to come from the instance manager, but this is the easiest solution for now
			_server = new Server();
			_configuration.ApplyTo(_server);
			_server.Start();
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (_server != null)
			{
				_server.Stop();
				_server = null;
			}
			
			_configuration = null;
		}

		#endregion
		
		public object Evaluate(string statement, IEnumerable<KeyValuePair<string, object>> args)
		{
			CheckActive();
            RESTResult result = null;
            var session = _server.Connect(new SessionInfo());
            try
            {
                var process = session.StartProcess(new ProcessInfo());
                try
                {
                    DataParam[] paramsValue = RESTMarshal.ArgsToDataParams(process, args);
                    result = Execute(process, statement, paramsValue);
                }
                finally
                {
                    session.StopProcess(process);
                }
            }
            finally
            {
                _server.Disconnect(session);
            }

            return result;
		}

		public object HostEvaluate(string statement)
		{
			CheckActive();

			var session = _server.Connect(new SessionInfo());
			try
			{
				var process = session.StartProcess(new ProcessInfo());
				try
				{
					return process.Evaluate(statement, null);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				_server.Disconnect(session);
			}
		}

		public string BuildWhereClause(IEnumerable<KeyValuePair<string, object>> paramaters)
		{
			var result = "where 1 = 1";

			foreach (var parameter in paramaters)
			{
				result += string.Format(" and {0} = {1}", parameter.Key.Substring(1), parameter.Key);
			}

			return result;
		}

		private void CheckActive()
		{  
			if ((_server == null) || (_server.State != ServerState.Started))
			{
				throw new InvalidOperationException("Instance is not active.");
			}
		}

		private RESTResult Execute(IServerProcess process, string statement, DataParam[] paramsValue)
		{
			IServerScript script = process.PrepareScript(statement);
			try
			{
				if (script.Batches.Count != 1)
				{
					throw new ArgumentException("Execution statement must contain one, and only one, batch.");
				}

				IServerBatch batch = script.Batches[0];
				DataParams dataParams = RESTMarshal.ParamsArrayToDataParams(process, paramsValue);
				RESTResult result = new RESTResult();
				result.Params = paramsValue;

				if (batch.IsExpression())
				{
					IServerExpressionPlan expressionPlan = batch.PrepareExpression(dataParams);
					try
					{
						if (expressionPlan.DataType is TableType)
						{
							IServerCursor cursor = expressionPlan.Open(dataParams);
							try
							{
								result.Value = RESTMarshal.ServerCursorToValue(process, cursor);
							}
							finally
							{
								expressionPlan.Close(cursor);
							}
						}
						else
						{
							using (IDataValue tempValue = expressionPlan.Evaluate(dataParams))
							{
								result.Value = RESTMarshal.DataValueToValue(process, tempValue);
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
						statementPlan.Execute(dataParams);
					}
					finally
					{
						batch.UnprepareStatement(statementPlan);
					}
				}

				//NativeMarshal.SetNativeOutputParams(process, result.Params, dataParams);
				return result;
			}
			finally
			{
				process.UnprepareScript(script);
			}
		}
	}
}
