/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define LOGDDLINSTRUCTIONS

using System;
using System.IO;
using System.Data;
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
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Connection;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDeviceTable : Table
	{
		public CatalogDeviceTable(CatalogDevicePlanNode ADevicePlanNode, ServerProcess AProcess, CatalogDeviceSession ASession) : base(ADevicePlanNode.Node as TableNode, AProcess)
		{
			FDevicePlanNode = ADevicePlanNode;
			FSession = ASession;
		}
		
		private CatalogDeviceSession FSession; 
		public CatalogDeviceSession Session { get { return FSession; } }
		
		private CatalogDevicePlanNode FDevicePlanNode;
		public CatalogDevicePlanNode DevicePlanNode { get { return FDevicePlanNode; } }
		
		private SQLConnection FConnection;
		public SQLConnection Connection { get { return FConnection; } }
		
		private SQLCommand FCommand;
		public SQLCommand Command { get { return FCommand; } }
		
		private SQLCursor FCursor;
		public SQLCursor Cursor { get { return FCursor; } }

		protected override void InternalOpen()
		{
			// Connect to the Catalog Store
			FConnection = Session.Device.Store.GetSQLConnection();
			FConnection.BeginTransaction(SQLIsolationLevel.ReadUncommitted);

			// Create a command using DevicePlanNode.Statement
			FCommand = FConnection.CreateCommand(true);
			FCommand.CursorLocation = SQLCursorLocation.Server;
			FCommand.CommandBehavior = SQLCommandBehavior.Default;
			FCommand.CommandType = SQLCommandType.Statement;
			FCommand.LockType = SQLLockType.ReadOnly;
			FCommand.Statement = FDevicePlanNode.Statement.ToString();
			
			// Set the parameter values
			foreach (CatalogPlanParameter LPlanParameter in FDevicePlanNode.PlanParameters)
			{
				FCommand.Parameters.Add(LPlanParameter.SQLParameter);
				LPlanParameter.SQLParameter.Value = GetSQLValue(LPlanParameter.PlanNode.DataType, LPlanParameter.PlanNode.Execute(Session.ServerProcess));
			}

			// Open a cursor from the command
			FCursor = FCommand.Open(SQLCursorType.Dynamic, SQLIsolationLevel.ReadUncommitted);
			
			FBOF = true;
			FEOF = !FCursor.Next();
		}

		protected override void InternalClose()
		{
			// Dispose the cursor
			if (FCursor != null)
			{
				FCursor.Dispose();
				FCursor = null;
			}
			// Dispose the command
			if (FCommand != null)
			{
				FCommand.Dispose();
				FCommand = null;
			}
			
			// Dispose the connection
			if (FConnection != null)
			{
				FConnection.Dispose();
				FConnection = null;
			}
		}
		
		private object GetSQLValue(Schema.IDataType ADataType, object AValue)
		{
			if (AValue == null)
				return null;
			
			if (ADataType.Is(Session.Catalog.DataTypes.SystemBoolean))
				return (bool)AValue ? 1 : 0;
				
			return AValue;
		}
		
		private object GetNativeValue(object AValue)
		{
			// If this is a byte, then it must be translated as a bool
			if (AValue is byte)
				return (byte)AValue == 1;
			// If this is a DBNull, then it must be translated as a null
			if (AValue is DBNull)
				return null;
			return AValue;
		}

		protected override void InternalSelect(Row ARow)
		{
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
			{
				int LColumnIndex = Node.DataType.Columns.IndexOfName(ARow.DataType.Columns[LIndex].Name);
				if (LColumnIndex >= 0)
					ARow[LIndex] = GetNativeValue(FCursor[LColumnIndex]);
			}
		}

		protected override bool InternalNext()
		{
			if (FBOF)
				FBOF = false;
			else
			{
				if (!FEOF)
					FEOF = !FCursor.Next();
			}

			return !FEOF;
		}
		
		private bool FBOF;
		private bool FEOF;

		protected override bool InternalBOF()
		{
			return FBOF;
		}

		protected override bool InternalEOF()
		{
			return FEOF;
		}
	}
}
