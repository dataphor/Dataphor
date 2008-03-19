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
			DataRow ARow,
			IDbCommand ACommand,
			StatementType AStatementType,
			DataTableMapping ATableMapping
		) : base(ARow, ACommand, AStatementType, ATableMapping)
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
			DataRow ARow,
			IDbCommand ACommand,
			StatementType AStatementType,
			DataTableMapping ATableMapping
		) : base(ARow, ACommand, AStatementType, ATableMapping)
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
		public const string CDefaultTableName = "Table";

		/// <summary> Initializes a new instance of the DAEDataAdapter class. </summary>
		public DAEDataAdapter() : base() {}

		/// <summary> Initializes a new instance of the DAEDataAdapter class and adds itself to the container. </summary>
		/// <param name="AContainer"> The container to add this component to. </param>
		public DAEDataAdapter(IContainer AContainer)
		{
			if (AContainer != null)
				AContainer.Add(this);
		}

		/// <summary>
		/// Initializes a new instance of the DAEDataAdapter class with the 
		/// specified D4 select statement.
		/// </summary>
		/// <param name="ASelectCommand">D4 select statement.</param>
		public DAEDataAdapter(DAECommand ASelectCommand)
		{
			SelectCommand = ASelectCommand;
		}

		/// <summary>
		/// Initializes a new instance of the DAEDataAdapter class with an D4 select statement and a connection string.
		/// </summary>
		/// <param name="ASelectCommandText">D4 select statement.</param>
		/// <param name="ASelectConnectionString"></param>
		public DAEDataAdapter(string ASelectCommandText, string ASelectConnectionString)
		{
			SelectCommand = new DAECommand(ASelectCommandText, new DAEConnection(ASelectConnectionString));
		}
		
		/// <summary>
		/// Initializes a new instance of the DAEDataAdapter class with an D4 select statement and a DAEConnection object.
		/// </summary>
		/// <param name="ASelectCommandText">D4 select statement.</param>
		/// <param name="AConnection">The DAE connection</param>
		public DAEDataAdapter(string ASelectCommandText, DAEConnection AConnection)
		{
			SelectCommand = new DAECommand(ASelectCommandText, AConnection);
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

		protected override int Update(DataRow[] ADataRows, DataTableMapping ATableMapping)
		{
			int LRowsAffected = 0;
			BeginDataUpdate();
			try
			{
				LRowsAffected = base.Update(ADataRows, ATableMapping);
			}
			finally
			{
				EndDataUpdate();
			}
			return LRowsAffected;
		}

		protected override RowUpdatingEventArgs CreateRowUpdatingEvent
		(
			DataRow ARow, 
			IDbCommand ACommand, 
			StatementType AStatementType,
			DataTableMapping ATableMapping
		)
		{
			return new DAERowUpdatingEventArgs(ARow, ACommand, AStatementType, ATableMapping);
		}

		protected override RowUpdatedEventArgs CreateRowUpdatedEvent
		(
			DataRow ARow,
			IDbCommand ACommand,
			StatementType AStatementType,
			DataTableMapping ATableMapping
		)
		{
			return new DAERowUpdatedEventArgs(ARow, ACommand, AStatementType, ATableMapping);
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
		protected override void OnRowUpdating(RowUpdatingEventArgs AArgs)
		{
			DAERowUpdatingEventHandler LHandler = (DAERowUpdatingEventHandler) Events[EventRowUpdating];
			if ((LHandler != null) && (AArgs is DAERowUpdatingEventArgs)) 
				LHandler(this, (DAERowUpdatingEventArgs)AArgs);
		}

		/// <summary> Raises the RowUpdated event. </summary>
		protected override void OnRowUpdated(RowUpdatedEventArgs AArgs)
		{
			DAERowUpdatedEventHandler LHandler = (DAERowUpdatedEventHandler) Events[EventRowUpdated];
			if ((LHandler != null) && (AArgs is DAERowUpdatedEventArgs)) 
				LHandler(this, (DAERowUpdatedEventArgs)AArgs);
		}

		protected override DataTable[] FillSchema
		(
			System.Data.DataSet ADataSet,
			SchemaType ASchemaType,
			IDbCommand ACommand,
			string ASrcTable,
			CommandBehavior ABehavior
		)
		{
			DataTable[] LTables = base.FillSchema(ADataSet, ASchemaType, ACommand, ASrcTable, ABehavior);
			IServerExpressionPlan LPlan = ((DAECommand)ACommand).FPlan as IServerExpressionPlan;
			if (LPlan != null)
			{
				Schema.Key LMinimumKey = LPlan.TableVar.Keys.MinimumKey(true);
				foreach (DataTable LTable in LTables)
				{
					bool LIsPrimary;
					LTable.Constraints.Clear();
					foreach(Schema.Key LKey in LPlan.TableVar.Keys)
					{
						LIsPrimary = LKey == LMinimumKey;
						LTable.Constraints.Add(LKey.Name, GetDataColumns(LTable, LKey), LIsPrimary);
					}
				}
			}
			return LTables;
		}

		private System.Data.DataColumn[] GetDataColumns(DataTable ATable, Schema.Key AKey)
		{
			System.Data.DataColumn[] LDataColumns = new System.Data.DataColumn[AKey.Columns.Count];
			int i = 0;
			foreach (DAE.Schema.TableVarColumn LColumn in AKey.Columns)
				LDataColumns[i++] = ATable.Columns[LColumn.Name];
			return LDataColumns;
		}
	}
}
