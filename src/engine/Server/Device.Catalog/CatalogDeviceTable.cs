/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define LOGDDLINSTRUCTIONS

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Connection;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDeviceTable : Table
	{
		public CatalogDeviceTable(CatalogDevicePlanNode devicePlanNode, Program program, ServerCatalogDeviceSession session) : base(devicePlanNode.Node as TableNode, program)
		{
			_devicePlanNode = devicePlanNode;
			_session = session;
		}
		
		private ServerCatalogDeviceSession _session; 
		public ServerCatalogDeviceSession Session { get { return _session; } }
		
		private CatalogDevicePlanNode _devicePlanNode;
		public CatalogDevicePlanNode DevicePlanNode { get { return _devicePlanNode; } }
		
		private SQLConnection _connection;
		public SQLConnection Connection { get { return _connection; } }
		
		private SQLCommand _command;
		public SQLCommand Command { get { return _command; } }
		
		private SQLCursor _cursor;
		public SQLCursor Cursor { get { return _cursor; } }

		protected override void InternalOpen()
		{
			// Connect to the Catalog Store
			_connection = Session.Device.Store.GetSQLConnection();
			_connection.BeginTransaction(SQLIsolationLevel.ReadUncommitted);

			// Create a command using DevicePlanNode.Statement
			_command = _connection.CreateCommand(true);
			_command.CursorLocation = SQLCursorLocation.Server;
			_command.CommandBehavior = SQLCommandBehavior.Default;
			_command.CommandType = SQLCommandType.Statement;
			_command.LockType = SQLLockType.ReadOnly;
			_command.Statement = _devicePlanNode.Statement.ToString();
			
			// Set the parameter values
			foreach (CatalogPlanParameter planParameter in _devicePlanNode.PlanParameters)
			{
				_command.Parameters.Add(planParameter.SQLParameter);
				planParameter.SQLParameter.Value = GetSQLValue(planParameter.PlanNode.DataType, planParameter.PlanNode.Execute(Program));
			}

			// Open a cursor from the command
			_cursor = _command.Open(SQLCursorType.Dynamic, SQLIsolationLevel.ReadUncommitted);
			
			_bOF = true;
			_eOF = !_cursor.Next();
		}

		protected override void InternalClose()
		{
			// Dispose the cursor
			if (_cursor != null)
			{
				_cursor.Dispose();
				_cursor = null;
			}
			// Dispose the command
			if (_command != null)
			{
				_command.Dispose();
				_command = null;
			}
			
			// Dispose the connection
			if (_connection != null)
			{
				_connection.Dispose();
				_connection = null;
			}
		}
		
		private object GetSQLValue(Schema.IDataType dataType, object tempValue)
		{
			if (tempValue == null)
				return null;
			
			if (dataType.Is(Session.Catalog.DataTypes.SystemBoolean))
				return (bool)tempValue ? 1 : 0;
				
			return tempValue;
		}
		
		private object GetNativeValue(object tempValue)
		{
			// If this is a byte, then it must be translated as a bool
			if (tempValue is byte)
				return (byte)tempValue == 1;
			// If this is a DBNull, then it must be translated as a null
			if (tempValue is DBNull)
				return null;
			return tempValue;
		}

		protected override void InternalSelect(IRow row)
		{
			for (int index = 0; index < row.DataType.Columns.Count; index++)
			{
				int columnIndex = Node.DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
				if (columnIndex >= 0)
					row[index] = GetNativeValue(_cursor[columnIndex]);
			}
		}

		protected override bool InternalNext()
		{
			if (_bOF)
				_bOF = false;
			else
			{
				if (!_eOF)
					_eOF = !_cursor.Next();
			}

			return !_eOF;
		}
		
		private bool _bOF;
		private bool _eOF;

		protected override bool InternalBOF()
		{
			return _bOF;
		}

		protected override bool InternalEOF()
		{
			return _eOF;
		}
	}
}
