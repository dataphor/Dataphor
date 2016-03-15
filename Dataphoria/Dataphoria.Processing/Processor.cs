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

		public object Evaluate(string statement, Dictionary<string, object> args)
		{
			CheckActive();

			var paramsValue = 
				args != null 
					? (from e in args select new NativeParam { Name = e.Key, Value = ValueToNativeValue(e.Value), Modifier = NativeModifier.In }).ToArray()
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

		private string GetDataTypeName(NativeValue value)
		{
			NativeScalarValue scalarValue = value as NativeScalarValue;
			if (scalarValue != null)
				return scalarValue.DataTypeName;

			NativeListValue listValue = value as NativeListValue;
			if (listValue != null)
				return String.Format("list({0})", listValue.ElementDataTypeName);

			NativeRowValue rowValue = value as NativeRowValue;
			if (rowValue != null)
			{
				var sb = new StringBuilder();
				sb.Append("row{");
				bool first = true;
				foreach (NativeColumn column in rowValue.Columns)
				{
					if (!first)
						sb.Append(",");
					else
						first = false;
					sb.AppendFormat("{0}:{1}", column.Name, column.DataTypeName);
				}
				sb.Append("}");
				return sb.ToString();
			}

			NativeTableValue tableValue = value as NativeTableValue;
			if (tableValue != null)
			{
				var sb = new StringBuilder();
				sb.Append("table{");
				bool first = true;
				foreach (NativeColumn column in tableValue.Columns)
				{
					if (!first)
						sb.Append(",");
					else
						first = false;

					sb.AppendFormat("{0}:{1}", column.Name, column.DataTypeName);
				}
				sb.Append("}");
				return sb.ToString();
			}
			
			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}

		private NativeValue ValueToNativeValue(object value)
		{
			// If it's a value type or String, send it through as a scalar
			if (value == null || value.GetType().IsValueType || value.GetType() == typeof(String))
				return new NativeScalarValue { Value = value, DataTypeName = value != null ? value.GetType().Name : "Object" };

			// If it's a JObject, send it through as a row
			// I really don't want to do this (JSON is part of the communication layer, shouldn't be part of the processor here) but I don't want to build yet another pass-through structure to enable it when JObject is already that...
			var jObject = value as JObject;
			if (jObject != null)
			{
				var columns = new List<NativeColumn>();
				var values = new List<NativeValue>();
				foreach (var property in jObject)
				{
					var propertyValue = ValueToNativeValue(property.Value);
					columns.Add(new NativeColumn { Name = property.Key, DataTypeName = GetDataTypeName(propertyValue) });
					values.Add(propertyValue);
				}

				return new NativeRowValue { Columns = columns.ToArray(), Values = values.ToArray() };
			}

			var jValue = value as JValue;
			if (jValue != null)
			{
				return ValueToNativeValue(jValue.Value);
			}

			// If it's an array or collection, send it through as a NativeList or NativeTable...
			if (value.GetType().IsArray || value is ICollection)
				// TODO: Handle list and table parameters
				throw new NotSupportedException("List parameters are not yet supported.");

			// Otherwise, send it through as a row
			return ObjectToNativeValue(value);
		}

		private NativeValue ObjectToNativeValue(object value)
		{
			var columns = new List<NativeColumn>();
			var values = new List<NativeValue>();
			foreach (var property in value.GetType().GetProperties())
			{
				if (property.CanRead)
				{
					var propertyValue = ValueToNativeValue(property.GetValue(value, null));
					columns.Add(new NativeColumn { Name = property.Name, DataTypeName = GetDataTypeName(propertyValue) });
					values.Add(propertyValue);
				}
			}

			return new NativeRowValue { Columns = columns.ToArray(), Values = values.ToArray() };
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
