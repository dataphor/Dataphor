using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Server;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Dataphoria.Processing
{
	public class Processor : IDisposable
	{
		public const string DefaultProcessorInstanceName = "Processor";

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

			_nativeServer = new NativeServer(_server);
		}

		private string _instanceName;
		public string InstanceName { get { return _instanceName; } }

		private ServerConfiguration _configuration;
		private Server _server;
		private NativeServer _nativeServer;

		#region IDisposable Members

		public void Dispose()
		{
			if (_server != null)
			{
				_server.Stop();
				_server = null;
			}

			_nativeServer = null;
			_configuration = null;
		}

		#endregion

		public object Evaluate(string statement, IEnumerable<KeyValuePair<string, object>> args)
		{
			CheckActive();

			var paramsValue = 
				args != null 
					? (from e in args select new NativeParam { Name = e.Key, Value = NativeCLIHelper.ValueToNativeValue(e.Value), Modifier = NativeModifier.In }).ToArray()
					: null;

			// Use the NativeCLI here to wrap the result in a NativeResult
			// The Web service layer will then convert that to pure Json.
			return _nativeServer.Execute(new NativeSessionInfo(), statement, paramsValue, NativeExecutionOptions.Default);
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
				result += String.Format(" and {0} = {1}", parameter.Key.Substring(1), parameter.Key);
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
	}
}
