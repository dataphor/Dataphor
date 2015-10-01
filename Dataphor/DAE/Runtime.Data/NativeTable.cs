/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define USEINTERNALID // This allows the internal index to store duplicates of logical values for non-unique indexes

using System;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class NativeTable : System.Object
	{
		public const int DefaultFanout = 100;
		public const int DefaultCapacity = 100;

		public NativeTable(IValueManager manager, Schema.TableVar tableVar) : base()
		{
			TableVar = tableVar;
			_fanout = DefaultFanout;
			_capacity = DefaultCapacity;
			Create(manager);
		}
		
		public NativeTable(IValueManager manager, Schema.TableVar tableVar, int fanout, int capacity) : base()
		{
			TableVar = tableVar;
			_fanout = fanout;
			_capacity = capacity;
			Create(manager);
		}
		
		public Schema.TableVar TableVar;
		
		public Schema.ITableType TableType;
		
		public Schema.IRowType RowType;
		
		public NativeRowTree ClusteredIndex;
		
		public NativeRowTreeList NonClusteredIndexes = new NativeRowTreeList();

		private int _fanout;
		public int Fanout { get { return _fanout; } }

		private int _capacity;
		public int Capacity { get { return _capacity; } }
		
		private int _rowCount = 0;
		public int RowCount { get { return _rowCount; } }
		
		#if USEINTERNALID
		public const string InternalIDColumnName = @"__InternalID";

		private Schema.TableVarColumn _internalIDColumn;
		#endif
		
		// TODO: Compile row types for each index, saving column indexes to prevent the need for lookup during insert, update, and delete.		
		private void Create(IValueManager manager)
		{
			TableType = TableVar.DataType;
			RowType = TableType.RowType;

			Schema.RowType keyRowType;
			Schema.RowType dataRowType;

			// Create the indexes required to store data as described by the given table variable
			// Determine Fanout, Capacity, Clustering Key
			Schema.Order clusteringKey = manager.FindClusteringOrder(TableVar);
			keyRowType = new Schema.RowType(clusteringKey.Columns);
			dataRowType = new Schema.RowType();
			foreach (Schema.Column column in TableVar.DataType.Columns)
				if (!clusteringKey.Columns.Contains(column.Name))
					dataRowType.Columns.Add(new Schema.Column(column.Name, column.DataType));
				
			// Add an internal identifier for uniqueness of keys in nonunique indexes
			#if USEINTERNALID
			_internalIDColumn = new Schema.TableVarColumn(new Schema.Column(InternalIDColumnName, manager.DataTypes.SystemGuid), Schema.TableVarColumnType.InternalID);
			dataRowType.Columns.Add(_internalIDColumn.Column);
			#endif
					
			// Create the Clustered index
			ClusteredIndex = 
				new NativeRowTree
				(
					clusteringKey,
					keyRowType,
					dataRowType,
					_fanout,
					_capacity,
					true
				);

			// DataLength and DataColumns for all non clustered indexes is the key length and columns of the clustered key				
			dataRowType = keyRowType;
				
			// Create non clustered indexes for each key and order (unique sets)
			Schema.Order key;
			foreach (Schema.Key nonClusteredKey in TableVar.Keys)
				if (!nonClusteredKey.IsSparse && nonClusteredKey.Enforced)
				{
					if (!manager.OrderIncludesKey(ClusteredIndex.Key, nonClusteredKey))
					{
						key = manager.OrderFromKey(nonClusteredKey);
						if (!NonClusteredIndexes.Contains(key))
						{
							keyRowType = new Schema.RowType(key.Columns);
								
							NonClusteredIndexes.Add
							(
								new NativeRowTree
								(
									key,
									keyRowType,
									dataRowType,
									_fanout,
									_capacity,
									false
								)
							);						
						}
					}
				}
				else
				{
					// This is a potentially non-unique index, so add a GUID to ensure uniqueness of the key in the BTree
					key = manager.OrderFromKey(nonClusteredKey);
					#if USEINTERNALID
					Schema.OrderColumn uniqueColumn = new Schema.OrderColumn(_internalIDColumn, key.IsAscending);
					uniqueColumn.Sort = manager.GetUniqueSort(uniqueColumn.Column.DataType);
					key.Columns.Add(uniqueColumn);
					#endif

					if (!NonClusteredIndexes.Contains(key))
					{
						keyRowType = new Schema.RowType(key.Columns);
										  
						NonClusteredIndexes.Add
						(
							new NativeRowTree
							(
								key,
								keyRowType,
								dataRowType,
								_fanout,
								_capacity,
								false
							)
						);
					}
				}

			foreach (Schema.Order order in TableVar.Orders)
			{
				// This is a potentially non-unique index, so add a GUID to ensure uniqueness of the key in the BTree
				key = new Schema.Order(order);
				#if USEINTERNALID
				if (!manager.OrderIncludesOrder(key, clusteringKey))
				{
					Schema.OrderColumn uniqueColumn = new Schema.OrderColumn(_internalIDColumn, order.IsAscending);
					uniqueColumn.Sort = manager.GetUniqueSort(uniqueColumn.Column.DataType);
					key.Columns.Add(uniqueColumn);
				}
				#endif

				if (!NonClusteredIndexes.Contains(key))
				{
					keyRowType = new Schema.RowType(key.Columns);
									  
					NonClusteredIndexes.Add
					(
						new NativeRowTree
						(
							key,
							keyRowType,
							dataRowType,
							_fanout,
							_capacity,
							false
						)
					);
				}
			}
		}
		
		public void Drop(IValueManager manager)
		{
			for (int index = NonClusteredIndexes.Count - 1; index >= 0; index--)
			{
				NonClusteredIndexes[index].Drop(manager);
				NonClusteredIndexes.RemoveAt(index);
			}
			
			ClusteredIndex.Drop(manager);
			
			_rowCount = 0;
		}
		
		#if USEINTERNALID
		private void IndexInsert(IValueManager manager, NativeRowTree index, IRow row, Guid internalID)
		#else
		private void IndexInsert(IValueManager manager, NativeRowTree index, IRow row)
		#endif
		{
			int columnIndex;
			Row key = new Row(manager, index.KeyRowType);
			try
			{
				Row data = new Row(manager, index.DataRowType);
				try
				{
					key.ValuesOwned = false;
					data.ValuesOwned = false;

					for (int localIndex = 0; localIndex < key.DataType.Columns.Count; localIndex++)
					{
						#if USEINTERNALID
						if (key.DataType.Columns[localIndex].Name == InternalIDColumnName)
							key[localIndex] = internalID;
						else
						#endif
						{
							columnIndex = row.DataType.Columns.GetIndexOfColumn(key.DataType.Columns[localIndex].Name);
							if (row.HasValue(columnIndex))
								key[localIndex] = row.GetNativeValue(columnIndex);
							else
								key.ClearValue(localIndex);
						}
					}

					for (int localIndex = 0; localIndex < data.DataType.Columns.Count; localIndex++)
					{
						#if USEINTERNALID
						if (data.DataType.Columns[localIndex].Name == InternalIDColumnName)
							data[localIndex] = internalID;
						else
						#endif
						{
							columnIndex = row.DataType.Columns.GetIndexOfColumn(data.DataType.Columns[localIndex].Name);
							if (row.HasValue(columnIndex))
								data[localIndex] = row.GetNativeValue(columnIndex);
							else
								data.ClearValue(localIndex);
						}
					}

					index.Insert(manager, (NativeRow)key.AsNative, (NativeRow)data.AsNative);
				}
				finally
				{
					data.Dispose();
				}
			}
			finally
			{
				key.Dispose();
			}
		}
		
		/// <summary>Inserts the given row into all the indexes of the table value.</summary>
		/// <param name="row">The given row must conform to the structure of the table value.</param>
		public void Insert(IValueManager manager, IRow row)
		{
			// Insert the row into all indexes
			#if USEINTERNALID
			Guid internalID = Guid.NewGuid();
			IndexInsert(manager, ClusteredIndex, row, internalID);
			foreach (NativeRowTree index in NonClusteredIndexes)
				IndexInsert(manager, index, row, internalID);
			#else
			IndexInsert(manager, ClusteredIndex, row);
			foreach (NativeRowTree index in NonClusteredIndexes)
				IndexInsert(manager, index, row);
			#endif
			
			_rowCount++;
		}
		
		private Row GetIndexData(IValueManager manager, Schema.IRowType rowType, IRow[] sourceRows)
		{
			Row row = new Row(manager, rowType);
			try
			{
				int columnIndex;
				bool found;
				for (int index = 0; index < row.DataType.Columns.Count; index++)
				{
					found = false;
					foreach (Row sourceRow in sourceRows)
					{
						columnIndex = sourceRow.DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
						if (columnIndex >= 0)
						{
							if (sourceRow.HasValue(columnIndex))
								row[index] = sourceRow.GetNativeValue(columnIndex);
							else
								row.ClearValue(index);
							found = true;
							break;
						}
					}
					if (found)
						continue;
					
					throw new RuntimeException(RuntimeException.Codes.UnableToConstructIndexKey);
				}
				return row;
			}
			catch
			{
				row.Dispose();
				throw;
			}
		}
		
		private bool GetIsIndexAffected(Schema.IRowType rowType, IRow row)
		{
			foreach (Schema.Column column in rowType.Columns)
				if (row.DataType.Columns.ContainsName(column.Name))
					return true;
			return false;
		}
		
		public bool HasRow(IValueManager manager, IRow row)
		{
			IRow key = GetIndexData(manager, ClusteredIndex.KeyRowType, new IRow[]{row});
			try
			{
				using (RowTreeSearchPath searchPath = new RowTreeSearchPath())
				{
					int entryNumber;
					return ClusteredIndex.FindKey(manager, ClusteredIndex.KeyRowType, (NativeRow)key.AsNative, searchPath, out entryNumber);
				}
			}
			finally
			{
				key.Dispose();
			}
		}
		
		public void Update(IValueManager manager, IRow oldRow, IRow newRow)
		{
			// AOldRow must have at least the columns of the clustered index key
			Row oldClusteredKey = GetIndexData(manager, ClusteredIndex.KeyRowType, new IRow[]{oldRow});
			try
			{
				bool isClusteredIndexKeyAffected = GetIsIndexAffected(ClusteredIndex.KeyRowType, newRow);
				bool isClusteredIndexDataAffected = GetIsIndexAffected(ClusteredIndex.DataRowType, newRow);

				Row newClusteredKey = null;
				Row newClusteredData = null;
				try
				{					
					// Update the row in each index
					using (RowTreeSearchPath searchPath = new RowTreeSearchPath())
					{
						int entryNumber;
						bool result = ClusteredIndex.FindKey(manager, ClusteredIndex.KeyRowType, (NativeRow)oldClusteredKey.AsNative, searchPath, out entryNumber);
						if (!result)
							throw new IndexException(IndexException.Codes.KeyNotFound);
							
						Row oldClusteredData = new Row(manager, ClusteredIndex.DataRowType, searchPath.DataNode.DataNode.Rows[entryNumber]);
						try
						{
							bool isIndexAffected;
							foreach (NativeRowTree tree in NonClusteredIndexes)
							{
								isIndexAffected = GetIsIndexAffected(tree.KeyRowType, newRow);
								
								if (isClusteredIndexKeyAffected || isIndexAffected)
								{
									Row oldIndexKey = GetIndexData(manager, tree.KeyRowType, new Row[]{oldClusteredKey, oldClusteredData});
									try
									{
										Row newIndexKey = null;
										Row newIndexData = null;
										try
										{
											if (isIndexAffected)
												newIndexKey = GetIndexData(manager, tree.KeyRowType, new IRow[]{newRow, oldClusteredKey, oldClusteredData});
												
											if (isClusteredIndexKeyAffected)
												newIndexData = GetIndexData(manager, tree.DataRowType, new IRow[]{newRow, oldClusteredKey, oldClusteredData});
												
											if (isIndexAffected && isClusteredIndexKeyAffected)
											{
												tree.Update(manager, (NativeRow)oldIndexKey.AsNative, (NativeRow)newIndexKey.AsNative, (NativeRow)newIndexData.AsNative);
												newIndexKey.ValuesOwned = false;
												newIndexData.ValuesOwned = false;
											}
											else if (isIndexAffected)
											{
												tree.Update(manager, (NativeRow)oldIndexKey.AsNative, (NativeRow)newIndexKey.AsNative);
												newIndexKey.ValuesOwned = false;
											}
											else if (isClusteredIndexKeyAffected)
											{
												tree.Update(manager, (NativeRow)oldIndexKey.AsNative, (NativeRow)oldIndexKey.AsNative, (NativeRow)newIndexData.AsNative);
												newIndexData.ValuesOwned = false;
											}
										}
										finally
										{
											if (newIndexKey != null)
												newIndexKey.Dispose();
												
											if (newIndexData != null)
												newIndexData.Dispose();
										}
									}
									finally
									{
										oldIndexKey.Dispose();
									}
								}
							}
							
							if (isClusteredIndexKeyAffected)
								newClusteredKey = GetIndexData(manager, ClusteredIndex.KeyRowType, new IRow[]{newRow, oldClusteredKey, oldClusteredData});
								
							if (isClusteredIndexDataAffected)
								newClusteredData = GetIndexData(manager, ClusteredIndex.DataRowType, new IRow[]{newRow, oldClusteredData});
						}
						finally
						{
							oldClusteredData.Dispose();
						}
					}

					if (isClusteredIndexKeyAffected && isClusteredIndexDataAffected)
					{
						ClusteredIndex.Update(manager, (NativeRow)oldClusteredKey.AsNative, (NativeRow)newClusteredKey.AsNative, (NativeRow)newClusteredData.AsNative);
						newClusteredKey.ValuesOwned = false;
						newClusteredData.ValuesOwned = false;
					}				
					else if (isClusteredIndexKeyAffected)
					{
						ClusteredIndex.Update(manager, (NativeRow)oldClusteredKey.AsNative, (NativeRow)newClusteredKey.AsNative);
						newClusteredKey.ValuesOwned = false;
					}
					else if (isClusteredIndexDataAffected)
					{
						ClusteredIndex.Update(manager, (NativeRow)oldClusteredKey.AsNative, (NativeRow)oldClusteredKey.AsNative, (NativeRow)newClusteredData.AsNative);
						newClusteredData.ValuesOwned = false;
					}
				}
				finally
				{
					if (newClusteredKey != null)
						newClusteredKey.Dispose();
						
					if (newClusteredData != null)
						newClusteredData.Dispose();
				}
			}
			finally
			{
				oldClusteredKey.Dispose();
			}
		}

		public void Delete(IValueManager manager, IRow row)
		{
			// Delete the row from all indexes
			Row clusteredKey = GetIndexData(manager, ClusteredIndex.KeyRowType, new IRow[]{row});
			try
			{
				using (RowTreeSearchPath searchPath = new RowTreeSearchPath())
				{
					int entryNumber;
					bool result = ClusteredIndex.FindKey(manager, ClusteredIndex.KeyRowType, (NativeRow)clusteredKey.AsNative, searchPath, out entryNumber);
					if (!result)
						throw new IndexException(IndexException.Codes.KeyNotFound);
						
					Row clusteredData = new Row(manager, ClusteredIndex.DataRowType, searchPath.DataNode.DataNode.Rows[entryNumber]);
					try
					{
						foreach (NativeRowTree bufferIndex in NonClusteredIndexes)
						{
							Row key = GetIndexData(manager, bufferIndex.KeyRowType, new IRow[]{clusteredKey, clusteredData});
							try
							{
								bufferIndex.Delete(manager, (NativeRow)key.AsNative);
							}
							finally
							{
								key.Dispose();
							}
						}
					}
					finally
					{
						clusteredData.Dispose();
					}
				}

				ClusteredIndex.Delete(manager, (NativeRow)clusteredKey.AsNative);
			}
			finally
			{	
				clusteredKey.Dispose();
			}
			
			_rowCount--;
		}
		
		public void Truncate(IValueManager manager)
		{
			Drop(manager);
			Create(manager);
		}
	}

	public class NativeTables : List
	{
		public NativeTables() : base(){}
		
		public new NativeTable this[int index]
		{
			get { lock (this) { return (NativeTable)base[index]; } } 
			set { lock (this) { base[index] = value; } }
		}

		public int IndexOf(Schema.TableVar tableVar)
		{
			lock (this)
			{
				for (int index = 0; index < Count; index++)
					if (this[index].TableVar == tableVar)
						return index;
				return -1;
			}
		}
		
		public bool Contains(Schema.TableVar tableVar)
		{
			return IndexOf(tableVar) >= 0;
		}
		
		public NativeTable this[Schema.TableVar tableVar]
		{
			get
			{
				lock (this)
				{
					int index = IndexOf(tableVar);
					if (index < 0)
						throw new RuntimeException(RuntimeException.Codes.NativeTableNotFound, tableVar.DisplayName);
					return this[index];
				}
			}
		}
	}
}
