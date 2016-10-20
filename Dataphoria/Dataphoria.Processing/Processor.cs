using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

		/* Change this processor to not return anything NativeXXX, but to just return a c# primitives (lists, etc)
		 * Change this processor to be "kind of" stateful, by returning an id or something to mark the current page
		 *		... and then to take that id in the next call, and have a ExecuteNext() and ExecutePrior() to page back and forth
		 * Change the DataController to then take those primitives and convert them to json or whatever, without using NativeXXX either
		 * Change the DataController to also return RESTfulCLI objects?  Maybe I'm confused on this part
		*/
		public object OldEvaluate(string statement, IEnumerable<KeyValuePair<string, object>> args)
		{
			var nativeServer = new NativeServer(_server);
			CheckActive();

			var paramsValue =
				args != null
					? (from e in args select new NativeParam { Name = e.Key, Value = NativeCLIHelper.ValueToNativeValue(e.Value), Modifier = NativeModifier.In }).ToArray()
					: null;

			// Use the NativeCLI here to wrap the result in a NativeResult
			// The Web service layer will then convert that to pure Json.
			return nativeServer.Execute(new NativeSessionInfo(), statement, paramsValue, NativeExecutionOptions.Default);
		}

		public object Evaluate(string statement, IEnumerable<KeyValuePair<string, object>> args)
		{
			var oldObject = OldEvaluate(statement, args);

			CheckActive();
			var session = _server.Connect(new SessionInfo());
			var process = session.StartProcess(new ProcessInfo());

			var paramsValue = 
				args != null 
					? (from e in args select new DataParam
						(
							e.Key,
							ValueToDataValue((ServerProcess)process, e.Value).DataType,
							DAE.Language.Modifier.In
						)
					  ).ToArray()
					: null;
			
			var result = Execute(process, statement, paramsValue);

			return null;
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

		private object Execute(IServerProcess process, string statement, DataParam[] paramsValue)
		{
			IServerScript script = process.PrepareScript(statement);
			try
			{
				if (script.Batches.Count != 1)
				{
					throw new ArgumentException("Execution statement must contain one, and only one, batch.");
				}

				IServerBatch batch = script.Batches[0];
				DataParams dataParams = ParamsArrayToDataParams(paramsValue);
				Result result = new Result();
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
								result.Value = ServerCursorToValue(process, cursor);
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
								result.Value = DataValueToValue(process, tempValue);
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

				NativeMarshal.SetNativeOutputParams(process, result.Params, dataParams);
				return result;
			}
			finally
			{
				process.UnprepareScript(script);
			}
		}

		private DataParams ParamsArrayToDataParams(DataParam[] paramsValue)
		{
			throw new NotImplementedException();
		}

		private IDataValue ValueToDataValue(ServerProcess process, object value)
		{
			// If it's a value type or String, send it through as a scalar
			if (value == null || value.GetType().IsValueType || value.GetType() == typeof(String))
				return new Scalar(process.ValueManager, process.ValueManager.DataTypes.SystemScalar, value);

			// If it's a JObject, send it through as a row
			// I really don't want to do this (JSON is part of the communication layer, shouldn't be part of the processor here) but I don't want to build yet another pass-through structure to enable it when JObject is already that...
			var jObject = value as JObject;
			if (jObject != null)
			{
				var columns = new Columns();
				var values = new List<IDataValue>();
				foreach (var property in jObject)
				{
					var propertyValue = ValueToDataValue(process, property.Value);
					columns.Add(new Column(property.Key, GetDataType(propertyValue)));
					values.Add(propertyValue);
				}

				return (IRow)columns;
			}

			var jValue = value as JValue;
			if (jValue != null)
			{
				return ValueToDataValue(process, jValue.Value);
			}

			// If it's an array or collection, send it through as a List...
			if (value.GetType().IsArray || value is ICollection)
				// TODO: Handle list and table parameters
				throw new NotSupportedException("List parameters are not yet supported.");

			// Otherwise, send it through as a row
			return new Row(process.ValueManager, process.ValueManager.DataTypes.SystemRow);
		}

		private IDataType GetDataType(object value)
		{
			Scalar scalarValue = value as Scalar;
			if (scalarValue != null)
				return scalarValue.DataType;

			ListValue listValue = value as ListValue;
			if (listValue != null)
				return listValue.DataType;

			Row rowValue = value as Row;
			if (rowValue != null)
				return rowValue.DataType;

			TableValue tableValue = value as TableValue;
			if (tableValue != null)
				return tableValue.DataType;

			throw new NotSupportedException("Non-scalar-valued attributes are not supported.");
		}

		public static NativeValue ServerCursorToValue(IServerProcess process, IServerCursor cursor)
		{
			NativeTableValue nativeTable = TableVarToNativeTableValue(process, cursor.Plan.TableVar);

			List<object[]> nativeRows = new List<object[]>();

			IRow currentRow = cursor.Plan.RequestRow();
			try
			{
				bool[] valueTypes = new bool[nativeTable.Columns.Length];
				for (int index = 0; index < nativeTable.Columns.Length; index++)
					valueTypes[index] = currentRow.DataType.Columns[index].DataType is IScalarType;

				while (cursor.Next())
				{
					cursor.Select(currentRow);
					object[] nativeRow = new object[nativeTable.Columns.Length];
					for (int index = 0; index < nativeTable.Columns.Length; index++)
						if (valueTypes[index])
							nativeRow[index] = currentRow[index];
						else
							nativeRow[index] = DataValueToValue(process, currentRow.GetValue(index));

					nativeRows.Add(nativeRow);
				}
			}
			finally
			{
				cursor.Plan.ReleaseRow(currentRow);
			}

			nativeTable.Rows = nativeRows.ToArray();

			return nativeTable;
		}

		public static NativeValue DataValueToValue(IServerProcess process, IDataValue dataValue)
		{
			if (dataValue == null)
				return null;

			IScalar scalar = dataValue as IScalar;
			if (scalar != null)
			{
				NativeScalarValue nativeScalar = new NativeScalarValue();
				nativeScalar.DataTypeName = ScalarTypeToDataTypeName(process.DataTypes, scalar.DataType);
				nativeScalar.Value = dataValue.IsNil ? null : scalar.AsNative;
				return nativeScalar;
			}

			ListValue list = dataValue as ListValue;
			if (list != null)
			{
				NativeListValue nativeList = new NativeListValue();
				if (!list.IsNil)
				{
					nativeList.Elements = new NativeValue[list.Count()];
					for (int index = 0; index < list.Count(); index++)
						nativeList.Elements[index] = DataValueToValue(process, list.GetValue(index));
				}
				return nativeList;
			}

			IRow row = dataValue as IRow;
			if (row != null)
			{
				NativeRowValue nativeRow = new NativeRowValue();
				nativeRow.Columns = ColumnsToNativeColumns(process.DataTypes, row.DataType.Columns);

				if (!row.IsNil)
				{
					nativeRow.Values = new NativeValue[nativeRow.Columns.Length];
					for (int index = 0; index < nativeRow.Values.Length; index++)
						nativeRow.Values[index] = DataValueToValue(process, row.GetValue(index));
				}
				return nativeRow;
			}

			TableValue tableValue = dataValue as TableValue;
			TableValueScan scan = null;
			try
			{
				if (tableValue != null)
				{
					scan = new TableValueScan(tableValue);
					scan.Open();
					dataValue = scan;
				}

				ITable table = dataValue as ITable;
				if (table != null)
				{
					NativeTableValue nativeTable = new NativeTableValue();
					nativeTable.Columns = ColumnsToNativeColumns(process.DataTypes, table.DataType.Columns);

					List<object[]> nativeRows = new List<object[]>();

					if (!table.BOF())
						table.First();

					bool[] valueTypes = new bool[nativeTable.Columns.Length];
					for (int index = 0; index < nativeTable.Columns.Length; index++)
						valueTypes[index] = table.DataType.Columns[index].DataType is IScalarType;

					while (table.Next())
					{
						using (IRow currentRow = table.Select())
						{
							object[] nativeRow = new object[nativeTable.Columns.Length];
							for (int index = 0; index < nativeTable.Columns.Length; index++)
								if (valueTypes[index])
									nativeRow[index] = currentRow[index];
								else
									nativeRow[index] = DataValueToValue(process, currentRow.GetValue(index));

							nativeRows.Add(nativeRow);
						}
					}

					nativeTable.Rows = nativeRows.ToArray();
					return nativeTable;
				}
			}
			finally
			{
				if (scan != null)
					scan.Dispose();
			}

			throw new NotSupportedException(String.Format("Values of type \"{0}\" are not supported.", dataValue.DataType.Name));
		}
	}

	internal class Result
	{
		public Result()
		{
		}

		public DataParam[] Params { get; internal set; }

		public NativeValue Value { get; internal set; }
	}
}
