/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;
using D4 = Alphora.Dataphor.DAE.Language.D4;
using System.Collections.Generic;
using Alphora.Fastore.Client;


namespace Alphora.Dataphor.DAE.Device.Fastore
{ 
	//TODO: FIX NASTY ASSUMPTIONS! (First column is ID column -- This is clearly wrong, but I'm just getting things up and running)
	public class FastoreTable : System.Object
	{
		//Tied Directly to device for time being...
		public FastoreTable(IValueManager manager, Schema.TableVar tableVar, FastoreDevice device)
		{
			TableVar = tableVar;
			Device = device;
			EnsureColumns();
		}

		public FastoreDevice Device;
		public Schema.TableVar TableVar;

		private int[] _columns;
		public int[] Columns { get { return _columns; } }

		protected string MapTypeNames(IDataType dataType)
		{
			if (!(dataType is IScalarType))
				throw new Exception(String.Format("Fastore only supports scalar column types; {0} is not supported.", dataType.Name));

			var scalar = (IScalarType)dataType;

			switch (scalar.NativeType.Name)
			{
				case "System.String": return "String";
				case "System.Integer": return "Int";
				case "System.Boolean": return "Bool";
				case "System.Long": return "Long";
				default:
					throw new Exception(String.Format("Fastore doesn't support scalar type ({0}).", scalar.Name));
			}
		}

		private bool RowIDCandidateType(IDataType dataType)
		{
			return dataType is IScalarType && ((IScalarType)dataType).NativeType.IsValueType;
		}

		protected void EnsureColumns()
		{
            // Pull a list of candidate pods
            Range podQuery = new Range();
            podQuery.ColumnID = Dictionary.PodColumnPodID;
            podQuery.Ascending = true;
            var podIds = Device.Database.GetRange(new[] { Dictionary.PodColumnPodID }, podQuery, int.MaxValue);
            if (podIds.Data.Count == 0)
                throw new Exception("FastoreDevice can't create a new table.  The hive has no pods.  The hive must be initialized first.");

			var minKey = TableVar.Keys.MinimumKey(false, false);
			if (minKey != null && minKey.Columns.Count != 1)
				minKey = null;
			var rowIDColumn = minKey != null && RowIDCandidateType(minKey.Columns[0].DataType) ? minKey.Columns[0] : null;
			var rowIDmappedType = rowIDColumn == null ? "Int" : MapTypeNames(rowIDColumn.DataType);

            // Start distributing columns on a random pod. Otherwise, we will always start on the first pod
            int nextPod = new Random().Next(podIds.Data.Count - 1);

            //This is so we have quick access to all the ids (for queries). Otherwise, we have to iterate the 
            //TableVar Columns and pull the id each time.
            List<int> columnIds = new List<int>();
			for (int i = 0; i < TableVar.Columns.Count; i++)
			{
				var column = TableVar.Columns[i];
				var columnId = 0;
				var combinedName = Schema.Object.Qualify(column.Column.Name, TableVar.Name);

				// Determine or generate a column ID
				var columnIdTag = D4.MetaData.GetTag(column.MetaData, "Storage.ColumnID");
				if (columnIdTag == null)
				{
					// Attempt to find the column by name
					Range query = new Range();
					query.ColumnID = Dictionary.ColumnName;
					query.Start = new RangeBound() { Bound = combinedName, Inclusive = true };
					query.End = new RangeBound() { Bound = combinedName, Inclusive = true };
					var result = Device.Database.GetRange(new int[] { }, query);

					if (result.Data.Count > 0)
						columnId = (int)result.Data[0].ID;
					else
					{
						columnId = (int)Device.IDGenerator.Generate(Dictionary.ColumnID);
						EnsureColumn(column, combinedName, rowIDColumn, rowIDmappedType, columnId, podIds, ref nextPod);
					}

					column.MetaData.Tags.AddOrUpdate("Storage.ColumnID", columnId.ToString());
				}
				else
				{
					columnId = Int32.Parse(columnIdTag.Value);
					EnsureColumn(column, combinedName, rowIDColumn, rowIDmappedType, columnId, podIds, ref nextPod);
				}

				columnIds.Add(columnId);
			}

			_columns = columnIds.ToArray();
		}

		private void EnsureColumn(TableVarColumn column, string combinedName, TableVarColumn rowIDColumn, string rowIDmappedType, int columnId, RangeSet podIds, ref int nextPod)
		{
			// Attempt to find the column by ID, determine if it is already created
			Range query = new Range();
			query.ColumnID = Dictionary.ColumnID;
			query.Start = new RangeBound() { Bound = columnId, Inclusive = true };
			query.End = new RangeBound() { Bound = columnId, Inclusive = true };
			var result = Device.Database.GetRange(new int[] { }, query, int.MaxValue);

			if (result.Data.Count == 0)
			{
				// Determine the storage pod - default, but let the user override
				var defaultPodID = podIds.Data[nextPod++ % podIds.Data.Count].Values[0];
				var podIDs = 
				(
					from p in D4.MetaData.GetTag(column.MetaData, "Storage.PodIDs", defaultPodID.ToString()).Split(',') 
						select Int32.Parse(p)
				).ToArray();
				if (podIDs.Length == 0)
					throw new Exception(String.Format("No Pod ID(s) given for column {0} of table {1}.", column.DisplayName, column.TableVar.DisplayName));

				var mappedName = MapTypeNames(column.DataType);

				using (var transaction = Device.Database.Begin(true, true))
				{
					transaction.Include
					(
						Dictionary.ColumnColumns,
						columnId,
						new object[] 
						{ 
							columnId, 
							combinedName,
							mappedName, 
							rowIDmappedType,
							(rowIDColumn == column) ? BufferType.Identity
								: column.TableVar.Keys.Any(k => k.Columns.Count == 1 && k.Columns[0] == column) ? BufferType.Unique
								: BufferType.Multi
						}
					);

					foreach (var pod in podIDs)
						transaction.Include
						(
							Dictionary.PodColumnColumns,
							Device.IDGenerator.Generate(Dictionary.PodColumnPodID),
							new object[] { pod, columnId }
						);

					transaction.Commit();
				}
			}
		}

		public void Insert(IValueManager manager, IRow row, Database db)
		{
			if (row.HasValue(0))
			{
				object[] items = ((NativeRow)row.AsNative).Values;

				db.Include(_columns, items[0], items);
			}
		}

		public void Update(IValueManager manager, IRow oldRow, IRow newRow, Database db)
		{
			if (oldRow.HasValue(0))
			{
				var id = oldRow[0];
				for (int i = 0; i < _columns.Length; i++)
				{
					db.Exclude(_columns, id);
				}
			}

			if (newRow.HasValue(0))
			{
				object[] items = ((NativeRow)newRow.AsNative).Values;
				db.Include(_columns, items[0], items);
			}
		}

		public void Delete(IValueManager manager, IRow row, Database db)
		{
			//If no value at zero, no ID (Based on our wrong assumptions)
			if (row.HasValue(0))
			{
				db.Exclude(_columns, row[0]);
			}
		}

		public void Drop(IValueManager manager)
		{
			foreach (var col in _columns)
			{
                // Pull a list of the current repos so we can drop them all.
                Range repoQuery = new Range();
                repoQuery.ColumnID = Dictionary.PodColumnColumnID;
                repoQuery.Ascending = true;
                repoQuery.Start = new RangeBound() { Bound = col, Inclusive = true };
                repoQuery.End = new RangeBound() { Bound = col, Inclusive = true };

				var repoIds = Device.Database.GetRange(new int[] { Dictionary.PodColumnColumnID }, repoQuery, int.MaxValue);

                for (int i = 0; i < repoIds.Data.Count; i++)
                {
                    Device.Database.Exclude(Dictionary.PodColumnColumns, repoIds.Data[i].ID);
                }

				Range query = new Range();
                query.ColumnID = 0;
                query.Start = new RangeBound() { Bound = col, Inclusive = true };
                query.End = new RangeBound() { Bound = col, Inclusive = true };

                var columnExists = Device.Database.GetRange(new int[] { Dictionary.ColumnID }, query, int.MaxValue);

                if (columnExists.Data.Count > 0)
                {
                    Device.Database.Exclude(Dictionary.ColumnColumns, col);
                }
			}
		}
	}
}
