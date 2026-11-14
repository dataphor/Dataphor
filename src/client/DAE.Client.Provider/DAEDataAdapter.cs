/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Drawing;
using System.Data.Common;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	public delegate void DAERowUpdatingEventHandler(object ASender, DAERowUpdatingEventArgs AArgs);
	public delegate void DAERowUpdatedEventHandler(object ASender, DAERowUpdatedEventArgs AArgs);

	public class DAERowUpdatingEventArgs : RowUpdatingEventArgs
	{
		public DAERowUpdatingEventArgs
		(
			DataRow row,
			IDbCommand command,
			StatementType statementType,
			DataTableMapping tableMapping
		) : base(row, command, statementType, tableMapping)
		{}

		new public DAECommand Command
		{
			get { return (DAECommand)base.Command; }
			set { base.Command = value; }
		}
	}

	public class DAERowUpdatedEventArgs : RowUpdatedEventArgs
	{
		public DAERowUpdatedEventArgs
		(
			DataRow row,
			IDbCommand command,
			StatementType statementType,
			DataTableMapping tableMapping
		) : base(row, command, statementType, tableMapping)
		{}
		public new DAECommand Command { get { return (DAECommand)base.Command; } }
	}

	/// <summary> DAE Data Adapter. </summary>
	/// <remarks>
	/// To create a typed DataSet do the following:
	/// Export the schema for the table you want to add using Dataphoria.
	/// Highlight the exported schema and copy it to the clipboard.
	/// In your project add a new "DataSet" item by right clicking on your project and Add/Add New Item...
	/// Select DataSet.xsd give it the name of the table you have schema for.
	/// Doubleclick on the new dataset after adding it to the project.
	/// Select the xml tab in the designer, replace the existing xml with the clipboards(the tables schema.)
	/// Now you can drop a DataSet component on your form and relate it to the typed dataset.
	/// </remarks>
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Provider.DAEDataAdapter),"Icons.DAEDataAdapter.bmp")]
	public class DAEDataAdapter : DbDataAdapter, IDbDataAdapter
	{
		public const string DefaultTableName = "Table";

		/// <summary> Initializes a new instance of the DAEDataAdapter class. </summary>
		public DAEDataAdapter() : base() {}

		/// <summary> Initializes a new instance of the DAEDataAdapter class and adds itself to the container. </summary>
		/// <param name="container"> The container to add this component to. </param>
		public DAEDataAdapter(IContainer container)
		{
			if (container != null)
				container.Add(this);
		}

		/// <summary>
		/// Initializes a new instance of the DAEDataAdapter class with the 
		/// specified D4 select statement.
		/// </summary>
		/// <param name="selectCommand">D4 select statement.</param>
		public DAEDataAdapter(DAECommand selectCommand)
		{
			SelectCommand = selectCommand;
		}

		/// <summary>
		/// Initializes a new instance of the DAEDataAdapter class with an D4 select statement and a connection string.
		/// </summary>
		/// <param name="selectCommandText">D4 select statement.</param>
		/// <param name="selectConnectionString"></param>
		public DAEDataAdapter(string selectCommandText, string selectConnectionString)
		{
			SelectCommand = new DAECommand(selectCommandText, new DAEConnection(selectConnectionString));
		}
		
		/// <summary>
		/// Initializes a new instance of the DAEDataAdapter class with an D4 select statement and a DAEConnection object.
		/// </summary>
		/// <param name="selectCommandText">D4 select statement.</param>
		/// <param name="connection">The DAE connection</param>
		public DAEDataAdapter(string selectCommandText, DAEConnection connection)
		{
			SelectCommand = new DAECommand(selectCommandText, connection);
		}

		public override IDataParameter[] GetFillParameters()
		{
			return base.GetFillParameters();
		}

		protected void BeginDataUpdate()
		{
			if (SelectCommand != null)
				((DAECommand)SelectCommand).DataAdapterInUpdate = true;
			if (InsertCommand != null)
				((DAECommand)InsertCommand).DataAdapterInUpdate = true;
			if (UpdateCommand != null)
				((DAECommand)UpdateCommand).DataAdapterInUpdate = true;
			if (DeleteCommand != null)
				((DAECommand)DeleteCommand).DataAdapterInUpdate = true;
		}

		protected void EndDataUpdate()
		{
			if (SelectCommand != null)
				((DAECommand)SelectCommand).DataAdapterInUpdate = false;
			if (InsertCommand != null)
				((DAECommand)InsertCommand).DataAdapterInUpdate = false;
			if (UpdateCommand != null)
				((DAECommand)UpdateCommand).DataAdapterInUpdate = false;
			if (DeleteCommand != null)
				((DAECommand)DeleteCommand).DataAdapterInUpdate = false;
		}

		protected override int Update(DataRow[] dataRows, DataTableMapping tableMapping)
		{
			int rowsAffected = 0;
			BeginDataUpdate();
			try
			{
				rowsAffected = base.Update(dataRows, tableMapping);
			}
			finally
			{
				EndDataUpdate();
			}
			return rowsAffected;
		}

		protected override RowUpdatingEventArgs CreateRowUpdatingEvent
		(
			DataRow row, 
			IDbCommand command, 
			StatementType statementType,
			DataTableMapping tableMapping
		)
		{
			return new DAERowUpdatingEventArgs(row, command, statementType, tableMapping);
		}

		protected override RowUpdatedEventArgs CreateRowUpdatedEvent
		(
			DataRow row,
			IDbCommand command,
			StatementType statementType,
			DataTableMapping tableMapping
		)
		{
			return new DAERowUpdatedEventArgs(row, command, statementType, tableMapping);
		}

		/*
		* Inherit from Component through DbDataAdapter. The event
		* mechanism is designed to work with the Component.Events
		* property. These variables are the keys used to find the
		* events in the components list of events.
		*/
		private readonly object EventRowUpdated = new object(); 
		private readonly object EventRowUpdating = new object(); 

		public event DAERowUpdatingEventHandler RowUpdating
		{
			add { Events.AddHandler(EventRowUpdating, value); }
			remove { Events.AddHandler(EventRowUpdating, value); }
		}

		public event DAERowUpdatedEventHandler RowUpdated
		{
			add { Events.AddHandler(EventRowUpdated, value); }
			remove { Events.RemoveHandler(EventRowUpdated, value); }
		}

		/// <summary> Raises the RowUpdating event. </summary>
		protected override void OnRowUpdating(RowUpdatingEventArgs args)
		{
			DAERowUpdatingEventHandler handler = (DAERowUpdatingEventHandler) Events[EventRowUpdating];
			if ((handler != null) && (args is DAERowUpdatingEventArgs)) 
				handler(this, (DAERowUpdatingEventArgs)args);
		}

		/// <summary> Raises the RowUpdated event. </summary>
		protected override void OnRowUpdated(RowUpdatedEventArgs args)
		{
			DAERowUpdatedEventHandler handler = (DAERowUpdatedEventHandler) Events[EventRowUpdated];
			if ((handler != null) && (args is DAERowUpdatedEventArgs)) 
				handler(this, (DAERowUpdatedEventArgs)args);
		}

		protected override DataTable[] FillSchema
		(
			System.Data.DataSet dataSet,
			SchemaType schemaType,
			IDbCommand command,
			string srcTable,
			CommandBehavior behavior
		)
		{
			DataTable[] tables = base.FillSchema(dataSet, schemaType, command, srcTable, behavior);
			IServerExpressionPlan plan = ((DAECommand)command)._plan as IServerExpressionPlan;
			if (plan != null)
			{
				Schema.Key minimumKey = plan.TableVar.Keys.MinimumKey(true);
				foreach (DataTable table in tables)
				{
					bool isPrimary;
					table.Constraints.Clear();
					foreach(Schema.Key key in plan.TableVar.Keys)
					{
						isPrimary = key == minimumKey;
						table.Constraints.Add(key.Name, GetDataColumns(table, key), isPrimary);
					}
				}
			}
			return tables;
		}

		private System.Data.DataColumn[] GetDataColumns(DataTable table, Schema.Key key)
		{
			System.Data.DataColumn[] dataColumns = new System.Data.DataColumn[key.Columns.Count];
			int i = 0;
			foreach (DAE.Schema.TableVarColumn column in key.Columns)
				dataColumns[i++] = table.Columns[column.Name];
			return dataColumns;
		}
	}
}
