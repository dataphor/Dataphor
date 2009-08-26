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
		public const int CDefaultFanout = 100;
		public const int CDefaultCapacity = 100;

		public NativeTable(IValueManager AManager, Schema.TableVar ATableVar) : base()
		{
			TableVar = ATableVar;
			FFanout = CDefaultFanout;
			FCapacity = CDefaultCapacity;
			Create(AManager);
		}
		
		public NativeTable(IValueManager AManager, Schema.TableVar ATableVar, int AFanout, int ACapacity) : base()
		{
			TableVar = ATableVar;
			FFanout = AFanout;
			FCapacity = ACapacity;
			Create(AManager);
		}
		
		public Schema.TableVar TableVar;
		
		public Schema.ITableType TableType;
		
		public Schema.IRowType RowType;
		
		public NativeRowTree ClusteredIndex;
		
		public NativeRowTreeList NonClusteredIndexes = new NativeRowTreeList();

		private int FFanout;
		public int Fanout { get { return FFanout; } }

		private int FCapacity;
		public int Capacity { get { return FCapacity; } }
		
		private int FRowCount = 0;
		public int RowCount { get { return FRowCount; } }
		
		#if USEINTERNALID
		public const string CInternalIDColumnName = @"__InternalID";

		private Schema.TableVarColumn FInternalIDColumn;
		#endif
		
		// TODO: Compile row types for each index, saving column indexes to prevent the need for lookup during insert, update, and delete.		
		private void Create(IValueManager AManager)
		{
			TableType = TableVar.DataType;
			RowType = TableType.RowType;

			Schema.RowType LKeyRowType;
			Schema.RowType LDataRowType;

			// Create the indexes required to store data as described by the given table variable
			// Determine Fanout, Capacity, Clustering Key
			Schema.Order LClusteringKey = AManager.FindClusteringOrder(TableVar);
			LKeyRowType = new Schema.RowType(LClusteringKey.Columns);
			LDataRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in TableVar.DataType.Columns)
				if (!LClusteringKey.Columns.Contains(LColumn.Name))
					LDataRowType.Columns.Add(new Schema.Column(LColumn.Name, LColumn.DataType));
				
			// Add an internal identifier for uniqueness of keys in nonunique indexes
			#if USEINTERNALID
			FInternalIDColumn = new Schema.TableVarColumn(new Schema.Column(CInternalIDColumnName, AManager.DataTypes.SystemGuid), Schema.TableVarColumnType.InternalID);
			LDataRowType.Columns.Add(FInternalIDColumn.Column);
			#endif
					
			// Create the Clustered index
			ClusteredIndex = 
				new NativeRowTree
				(
					LClusteringKey,
					LKeyRowType,
					LDataRowType,
					FFanout,
					FCapacity,
					true
				);

			// DataLength and DataColumns for all non clustered indexes is the key length and columns of the clustered key				
			LDataRowType = LKeyRowType;
				
			// Create non clustered indexes for each key and order (unique sets)
			Schema.Order LKey;
			foreach (Schema.Key LNonClusteredKey in TableVar.Keys)
				if (!LNonClusteredKey.IsSparse && LNonClusteredKey.Enforced)
				{
					if (!AManager.OrderIncludesKey(ClusteredIndex.Key, LNonClusteredKey))
					{
						LKey = AManager.OrderFromKey(LNonClusteredKey);
						if (!NonClusteredIndexes.Contains(LKey))
						{
							LKeyRowType = new Schema.RowType(LKey.Columns);
								
							NonClusteredIndexes.Add
							(
								new NativeRowTree
								(
									LKey,
									LKeyRowType,
									LDataRowType,
									FFanout,
									FCapacity,
									false
								)
							);						
						}
					}
				}
				else
				{
					// This is a potentially non-unique index, so add a GUID to ensure uniqueness of the key in the BTree
					LKey = AManager.OrderFromKey(LNonClusteredKey);
					#if USEINTERNALID
					Schema.OrderColumn LUniqueColumn = new Schema.OrderColumn(FInternalIDColumn, LKey.IsAscending);
					LUniqueColumn.Sort = AManager.GetUniqueSort(LUniqueColumn.Column.DataType);
					LKey.Columns.Add(LUniqueColumn);
					#endif

					if (!NonClusteredIndexes.Contains(LKey))
					{
						LKeyRowType = new Schema.RowType(LKey.Columns);
										  
						NonClusteredIndexes.Add
						(
							new NativeRowTree
							(
								LKey,
								LKeyRowType,
								LDataRowType,
								FFanout,
								FCapacity,
								false
							)
						);
					}
				}

			foreach (Schema.Order LOrder in TableVar.Orders)
			{
				// This is a potentially non-unique index, so add a GUID to ensure uniqueness of the key in the BTree
				LKey = new Schema.Order(LOrder);
				#if USEINTERNALID
				if (!AManager.OrderIncludesOrder(LKey, LClusteringKey))
				{
					Schema.OrderColumn LUniqueColumn = new Schema.OrderColumn(FInternalIDColumn, LOrder.IsAscending);
					LUniqueColumn.Sort = AManager.GetUniqueSort(LUniqueColumn.Column.DataType);
					LKey.Columns.Add(LUniqueColumn);
				}
				#endif

				if (!NonClusteredIndexes.Contains(LKey))
				{
					LKeyRowType = new Schema.RowType(LKey.Columns);
									  
					NonClusteredIndexes.Add
					(
						new NativeRowTree
						(
							LKey,
							LKeyRowType,
							LDataRowType,
							FFanout,
							FCapacity,
							false
						)
					);
				}
			}
		}
		
		public void Drop(IValueManager AManager)
		{
			for (int LIndex = NonClusteredIndexes.Count - 1; LIndex >= 0; LIndex--)
			{
				NonClusteredIndexes[LIndex].Drop(AManager);
				NonClusteredIndexes.RemoveAt(LIndex);
			}
			
			ClusteredIndex.Drop(AManager);
			
			FRowCount = 0;
		}
		
		#if USEINTERNALID
		private void IndexInsert(IValueManager AManager, NativeRowTree AIndex, Row ARow, Guid AInternalID)
		#else
		private void IndexInsert(IValueManager AManager, NativeRowTree AIndex, Row ARow)
		#endif
		{
			int LColumnIndex;
			Row LKey = new Row(AManager, AIndex.KeyRowType);
			try
			{
				Row LData = new Row(AManager, AIndex.DataRowType);
				try
				{
					LKey.ValuesOwned = false;
					LData.ValuesOwned = false;

					for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
					{
						#if USEINTERNALID
						if (LKey.DataType.Columns[LIndex].Name == CInternalIDColumnName)
							LKey[LIndex] = AInternalID;
						else
						#endif
						{
							LColumnIndex = ARow.DataType.Columns.GetIndexOfColumn(LKey.DataType.Columns[LIndex].Name);
							if (ARow.HasValue(LColumnIndex))
								LKey[LIndex] = ARow[LColumnIndex];
							else
								LKey.ClearValue(LIndex);
						}
					}

					for (int LIndex = 0; LIndex < LData.DataType.Columns.Count; LIndex++)
					{
						#if USEINTERNALID
						if (LData.DataType.Columns[LIndex].Name == CInternalIDColumnName)
							LData[LIndex] = AInternalID;
						else
						#endif
						{
							LColumnIndex = ARow.DataType.Columns.GetIndexOfColumn(LData.DataType.Columns[LIndex].Name);
							if (ARow.HasValue(LColumnIndex))
								LData[LIndex] = ARow[LColumnIndex];
							else
								LData.ClearValue(LIndex);
						}
					}

					AIndex.Insert(AManager, (NativeRow)LKey.AsNative, (NativeRow)LData.AsNative);
				}
				finally
				{
					LData.Dispose();
				}
			}
			finally
			{
				LKey.Dispose();
			}
		}
		
		/// <summary>Inserts the given row into all the indexes of the table value.</summary>
		/// <param name="ARow">The given row must conform to the structure of the table value.</param>
		public void Insert(IValueManager AManager, Row ARow)
		{
			// Insert the row into all indexes
			#if USEINTERNALID
			Guid LInternalID = Guid.NewGuid();
			IndexInsert(AManager, ClusteredIndex, ARow, LInternalID);
			foreach (NativeRowTree LIndex in NonClusteredIndexes)
				IndexInsert(AManager, LIndex, ARow, LInternalID);
			#else
			IndexInsert(AManager, ClusteredIndex, ARow);
			foreach (NativeRowTree LIndex in NonClusteredIndexes)
				IndexInsert(AManager, LIndex, ARow);
			#endif
			
			FRowCount++;
		}
		
		private Row GetIndexData(IValueManager AManager, Schema.RowType ARowType, Row[] ASourceRows)
		{
			Row LRow = new Row(AManager, ARowType);
			try
			{
				int LColumnIndex;
				bool LFound;
				for (int LIndex = 0; LIndex < LRow.DataType.Columns.Count; LIndex++)
				{
					LFound = false;
					foreach (Row LSourceRow in ASourceRows)
					{
						LColumnIndex = LSourceRow.DataType.Columns.IndexOfName(LRow.DataType.Columns[LIndex].Name);
						if (LColumnIndex >= 0)
						{
							if (LSourceRow.HasValue(LColumnIndex))
								LRow[LIndex] = LSourceRow[LColumnIndex];
							else
								LRow.ClearValue(LIndex);
							LFound = true;
							break;
						}
					}
					if (LFound)
						continue;
					
					throw new RuntimeException(RuntimeException.Codes.UnableToConstructIndexKey);
				}
				return LRow;
			}
			catch
			{
				LRow.Dispose();
				throw;
			}
		}
		
		private bool GetIsIndexAffected(Schema.RowType ARowType, Row ARow)
		{
			foreach (Schema.Column LColumn in ARowType.Columns)
				if (ARow.DataType.Columns.ContainsName(LColumn.Name))
					return true;
			return false;
		}
		
		public bool HasRow(IValueManager AManager, Row ARow)
		{
			Row LKey = GetIndexData(AManager, ClusteredIndex.KeyRowType, new Row[]{ARow});
			try
			{
				using (RowTreeSearchPath LSearchPath = new RowTreeSearchPath())
				{
					int LEntryNumber;
					return ClusteredIndex.FindKey(AManager, ClusteredIndex.KeyRowType, (NativeRow)LKey.AsNative, LSearchPath, out LEntryNumber);
				}
			}
			finally
			{
				LKey.Dispose();
			}
		}
		
		public void Update(IValueManager AManager, Row AOldRow, Row ANewRow)
		{
			// AOldRow must have at least the columns of the clustered index key
			Row LOldClusteredKey = GetIndexData(AManager, ClusteredIndex.KeyRowType, new Row[]{AOldRow});
			try
			{
				bool LIsClusteredIndexKeyAffected = GetIsIndexAffected(ClusteredIndex.KeyRowType, ANewRow);
				bool LIsClusteredIndexDataAffected = GetIsIndexAffected(ClusteredIndex.DataRowType, ANewRow);

				Row LNewClusteredKey = null;
				Row LNewClusteredData = null;
				try
				{					
					// Update the row in each index
					using (RowTreeSearchPath LSearchPath = new RowTreeSearchPath())
					{
						int LEntryNumber;
						bool LResult = ClusteredIndex.FindKey(AManager, ClusteredIndex.KeyRowType, (NativeRow)LOldClusteredKey.AsNative, LSearchPath, out LEntryNumber);
						if (!LResult)
							throw new IndexException(IndexException.Codes.KeyNotFound);
							
						Row LOldClusteredData = new Row(AManager, ClusteredIndex.DataRowType, LSearchPath.DataNode.DataNode.Rows[LEntryNumber]);
						try
						{
							bool LIsIndexAffected;
							foreach (NativeRowTree LTree in NonClusteredIndexes)
							{
								LIsIndexAffected = GetIsIndexAffected(LTree.KeyRowType, ANewRow);
								
								if (LIsClusteredIndexKeyAffected || LIsIndexAffected)
								{
									Row LOldIndexKey = GetIndexData(AManager, LTree.KeyRowType, new Row[]{LOldClusteredKey, LOldClusteredData});
									try
									{
										Row LNewIndexKey = null;
										Row LNewIndexData = null;
										try
										{
											if (LIsIndexAffected)
												LNewIndexKey = GetIndexData(AManager, LTree.KeyRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
												
											if (LIsClusteredIndexKeyAffected)
												LNewIndexData = GetIndexData(AManager, LTree.DataRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
												
											if (LIsIndexAffected && LIsClusteredIndexKeyAffected)
											{
												LTree.Update(AManager, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LNewIndexKey.AsNative, (NativeRow)LNewIndexData.AsNative);
												LNewIndexKey.ValuesOwned = false;
												LNewIndexData.ValuesOwned = false;
											}
											else if (LIsIndexAffected)
											{
												LTree.Update(AManager, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LNewIndexKey.AsNative);
												LNewIndexKey.ValuesOwned = false;
											}
											else if (LIsClusteredIndexKeyAffected)
											{
												LTree.Update(AManager, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LNewIndexData.AsNative);
												LNewIndexData.ValuesOwned = false;
											}
										}
										finally
										{
											if (LNewIndexKey != null)
												LNewIndexKey.Dispose();
												
											if (LNewIndexData != null)
												LNewIndexData.Dispose();
										}
									}
									finally
									{
										LOldIndexKey.Dispose();
									}
								}
							}
							
							if (LIsClusteredIndexKeyAffected)
								LNewClusteredKey = GetIndexData(AManager, ClusteredIndex.KeyRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
								
							if (LIsClusteredIndexDataAffected)
								LNewClusteredData = GetIndexData(AManager, ClusteredIndex.DataRowType, new Row[]{ANewRow, LOldClusteredData});
						}
						finally
						{
							LOldClusteredData.Dispose();
						}
					}

					if (LIsClusteredIndexKeyAffected && LIsClusteredIndexDataAffected)
					{
						ClusteredIndex.Update(AManager, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LNewClusteredKey.AsNative, (NativeRow)LNewClusteredData.AsNative);
						LNewClusteredKey.ValuesOwned = false;
						LNewClusteredData.ValuesOwned = false;
					}				
					else if (LIsClusteredIndexKeyAffected)
					{
						ClusteredIndex.Update(AManager, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LNewClusteredKey.AsNative);
						LNewClusteredKey.ValuesOwned = false;
					}
					else if (LIsClusteredIndexDataAffected)
					{
						ClusteredIndex.Update(AManager, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LNewClusteredData.AsNative);
						LNewClusteredData.ValuesOwned = false;
					}
				}
				finally
				{
					if (LNewClusteredKey != null)
						LNewClusteredKey.Dispose();
						
					if (LNewClusteredData != null)
						LNewClusteredData.Dispose();
				}
			}
			finally
			{
				LOldClusteredKey.Dispose();
			}
		}

		public void Delete(IValueManager AManager, Row ARow)
		{
			// Delete the row from all indexes
			Row LClusteredKey = GetIndexData(AManager, ClusteredIndex.KeyRowType, new Row[]{ARow});
			try
			{
				using (RowTreeSearchPath LSearchPath = new RowTreeSearchPath())
				{
					int LEntryNumber;
					bool LResult = ClusteredIndex.FindKey(AManager, ClusteredIndex.KeyRowType, (NativeRow)LClusteredKey.AsNative, LSearchPath, out LEntryNumber);
					if (!LResult)
						throw new IndexException(IndexException.Codes.KeyNotFound);
						
					Row LClusteredData = new Row(AManager, ClusteredIndex.DataRowType, LSearchPath.DataNode.DataNode.Rows[LEntryNumber]);
					try
					{
						foreach (NativeRowTree LBufferIndex in NonClusteredIndexes)
						{
							Row LKey = GetIndexData(AManager, LBufferIndex.KeyRowType, new Row[]{LClusteredKey, LClusteredData});
							try
							{
								LBufferIndex.Delete(AManager, (NativeRow)LKey.AsNative);
							}
							finally
							{
								LKey.Dispose();
							}
						}
					}
					finally
					{
						LClusteredData.Dispose();
					}
				}

				ClusteredIndex.Delete(AManager, (NativeRow)LClusteredKey.AsNative);
			}
			finally
			{	
				LClusteredKey.Dispose();
			}
			
			FRowCount--;
		}
		
		public void Truncate(IValueManager AManager)
		{
			Drop(AManager);
			Create(AManager);
		}
	}

	public class NativeTables : List
	{
		public NativeTables() : base(){}
		
		public new NativeTable this[int AIndex]
		{
			get { lock (this) { return (NativeTable)base[AIndex]; } } 
			set { lock (this) { base[AIndex] = value; } }
		}

		public int IndexOf(Schema.TableVar ATableVar)
		{
			lock (this)
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (this[LIndex].TableVar == ATableVar)
						return LIndex;
				return -1;
			}
		}
		
		public bool Contains(Schema.TableVar ATableVar)
		{
			return IndexOf(ATableVar) >= 0;
		}
		
		public NativeTable this[Schema.TableVar ATableVar]
		{
			get
			{
				lock (this)
				{
					int LIndex = IndexOf(ATableVar);
					if (LIndex < 0)
						throw new RuntimeException(RuntimeException.Codes.NativeTableNotFound, ATableVar.DisplayName);
					return this[LIndex];
				}
			}
		}
	}
}
