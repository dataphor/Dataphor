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

		public NativeTable(ServerProcess AProcess, Schema.TableVar ATableVar) : base()
		{
			TableVar = ATableVar;
			FFanout = CDefaultFanout;
			FCapacity = CDefaultCapacity;
			Create(AProcess);
		}
		
		public NativeTable(ServerProcess AProcess, Schema.TableVar ATableVar, int AFanout, int ACapacity) : base()
		{
			TableVar = ATableVar;
			FFanout = AFanout;
			FCapacity = ACapacity;
			Create(AProcess);
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
		private void Create(ServerProcess AProcess)
		{
			TableType = TableVar.DataType;
			RowType = TableType.RowType;

			Schema.RowType LKeyRowType;
			Schema.RowType LDataRowType;

			// Create the indexes required to store data as described by the given table variable
			// Determine Fanout, Capacity, Clustering Key
			Schema.Order LClusteringKey = TableVar.FindClusteringOrder(AProcess.Plan);
			LKeyRowType = new Schema.RowType(LClusteringKey.Columns);
			LDataRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in TableVar.DataType.Columns)
				if (!LClusteringKey.Columns.Contains(LColumn.Name))
					LDataRowType.Columns.Add(new Schema.Column(LColumn.Name, LColumn.DataType));
				
			// Add an internal identifier for uniqueness of keys in nonunique indexes
			#if USEINTERNALID
			FInternalIDColumn = new Schema.TableVarColumn(new Schema.Column(CInternalIDColumnName, AProcess.DataTypes.SystemGuid), Schema.TableVarColumnType.InternalID);
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
				if (!LNonClusteredKey.IsSparse)
				{
					if (!ClusteredIndex.Key.Includes(AProcess.Plan, LNonClusteredKey))
					{
						LKey = new Schema.Order(LNonClusteredKey, AProcess.Plan);
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
					LKey = new Schema.Order(LNonClusteredKey, AProcess.Plan);
					#if USEINTERNALID
					Schema.OrderColumn LUniqueColumn = new Schema.OrderColumn(FInternalIDColumn, LKey.IsAscending);
					LUniqueColumn.Sort = ((Schema.ScalarType)LUniqueColumn.Column.DataType).GetUniqueSort(AProcess.Plan);
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
				if (!LKey.Includes(AProcess.Plan, LClusteringKey))
				{
					Schema.OrderColumn LUniqueColumn = new Schema.OrderColumn(FInternalIDColumn, LOrder.IsAscending);
					LUniqueColumn.Sort = ((Schema.ScalarType)LUniqueColumn.Column.DataType).GetUniqueSort(AProcess.Plan);
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
		
		public void Drop(ServerProcess AProcess)
		{
			for (int LIndex = NonClusteredIndexes.Count - 1; LIndex >= 0; LIndex--)
			{
				NonClusteredIndexes[LIndex].Drop(AProcess);
				NonClusteredIndexes.RemoveAt(LIndex);
			}
			
			ClusteredIndex.Drop(AProcess);
			
			FRowCount = 0;
		}
		
		#if USEINTERNALID
		private void IndexInsert(ServerProcess AProcess, NativeRowTree AIndex, Row ARow, Scalar AInternalID)
		#else
		private void IndexInsert(ServerProcess AProcess, NativeRowTree AIndex, Row ARow)
		#endif
		{
			int LColumnIndex;
			Row LKey = new Row(AProcess, AIndex.KeyRowType);
			try
			{
				Row LData = new Row(AProcess, AIndex.DataRowType);
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
							LColumnIndex = ARow.GetIndexOfColumn(LKey.DataType.Columns[LIndex].Name);
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
							LColumnIndex = ARow.GetIndexOfColumn(LData.DataType.Columns[LIndex].Name);
							if (ARow.HasValue(LColumnIndex))
								LData[LIndex] = ARow[LColumnIndex];
							else
								LData.ClearValue(LIndex);
						}
					}

					AIndex.Insert(AProcess, (NativeRow)LKey.AsNative, (NativeRow)LData.AsNative);
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
		public void Insert(ServerProcess AProcess, Row ARow)
		{
			// Insert the row into all indexes
			#if USEINTERNALID
			Scalar LInternalID = new Scalar(AProcess, (Schema.IScalarType)FInternalIDColumn.DataType, Guid.NewGuid());
			IndexInsert(AProcess, ClusteredIndex, ARow, LInternalID);
			foreach (NativeRowTree LIndex in NonClusteredIndexes)
				IndexInsert(AProcess, LIndex, ARow, LInternalID);
			#else
			IndexInsert(AProcess, ClusteredIndex, ARow);
			foreach (NativeRowTree LIndex in NonClusteredIndexes)
				IndexInsert(AProcess, LIndex, ARow);
			#endif
			
			FRowCount++;
		}
		
		private Row GetIndexData(ServerProcess AProcess, Schema.RowType ARowType, Row[] ASourceRows)
		{
			Row LRow = new Row(AProcess, ARowType);
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
		
		public bool HasRow(ServerProcess AProcess, Row ARow)
		{
			Row LKey = GetIndexData(AProcess, ClusteredIndex.KeyRowType, new Row[]{ARow});
			try
			{
				using (RowTreeSearchPath LSearchPath = new RowTreeSearchPath())
				{
					int LEntryNumber;
					return ClusteredIndex.FindKey(AProcess, ClusteredIndex.KeyRowType, (NativeRow)LKey.AsNative, LSearchPath, out LEntryNumber);
				}
			}
			finally
			{
				LKey.Dispose();
			}
		}
		
		public void Update(ServerProcess AProcess, Row AOldRow, Row ANewRow)
		{
			// AOldRow must have at least the columns of the clustered index key
			Row LOldClusteredKey = GetIndexData(AProcess, ClusteredIndex.KeyRowType, new Row[]{AOldRow});
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
						bool LResult = ClusteredIndex.FindKey(AProcess, ClusteredIndex.KeyRowType, (NativeRow)LOldClusteredKey.AsNative, LSearchPath, out LEntryNumber);
						if (!LResult)
							throw new IndexException(IndexException.Codes.KeyNotFound);
							
						Row LOldClusteredData = new Row(AProcess, ClusteredIndex.DataRowType, LSearchPath.DataNode.DataNode.Rows[LEntryNumber]);
						try
						{
							bool LIsIndexAffected;
							foreach (NativeRowTree LTree in NonClusteredIndexes)
							{
								LIsIndexAffected = GetIsIndexAffected(LTree.KeyRowType, ANewRow);
								
								if (LIsClusteredIndexKeyAffected || LIsIndexAffected)
								{
									Row LOldIndexKey = GetIndexData(AProcess, LTree.KeyRowType, new Row[]{LOldClusteredKey, LOldClusteredData});
									try
									{
										Row LNewIndexKey = null;
										Row LNewIndexData = null;
										try
										{
											if (LIsIndexAffected)
												LNewIndexKey = GetIndexData(AProcess, LTree.KeyRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
												
											if (LIsClusteredIndexKeyAffected)
												LNewIndexData = GetIndexData(AProcess, LTree.DataRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
												
											if (LIsIndexAffected && LIsClusteredIndexKeyAffected)
											{
												LTree.Update(AProcess, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LNewIndexKey.AsNative, (NativeRow)LNewIndexData.AsNative);
												LNewIndexKey.ValuesOwned = false;
												LNewIndexData.ValuesOwned = false;
											}
											else if (LIsIndexAffected)
											{
												LTree.Update(AProcess, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LNewIndexKey.AsNative);
												LNewIndexKey.ValuesOwned = false;
											}
											else if (LIsClusteredIndexKeyAffected)
											{
												LTree.Update(AProcess, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LOldIndexKey.AsNative, (NativeRow)LNewIndexData.AsNative);
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
								LNewClusteredKey = GetIndexData(AProcess, ClusteredIndex.KeyRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
								
							if (LIsClusteredIndexDataAffected)
								LNewClusteredData = GetIndexData(AProcess, ClusteredIndex.DataRowType, new Row[]{ANewRow, LOldClusteredData});
						}
						finally
						{
							LOldClusteredData.Dispose();
						}
					}

					if (LIsClusteredIndexKeyAffected && LIsClusteredIndexDataAffected)
					{
						ClusteredIndex.Update(AProcess, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LNewClusteredKey.AsNative, (NativeRow)LNewClusteredData.AsNative);
						LNewClusteredKey.ValuesOwned = false;
						LNewClusteredData.ValuesOwned = false;
					}				
					else if (LIsClusteredIndexKeyAffected)
					{
						ClusteredIndex.Update(AProcess, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LNewClusteredKey.AsNative);
						LNewClusteredKey.ValuesOwned = false;
					}
					else if (LIsClusteredIndexDataAffected)
					{
						ClusteredIndex.Update(AProcess, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LOldClusteredKey.AsNative, (NativeRow)LNewClusteredData.AsNative);
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

		public void Delete(ServerProcess AProcess, Row ARow)
		{
			// Delete the row from all indexes
			Row LClusteredKey = GetIndexData(AProcess, ClusteredIndex.KeyRowType, new Row[]{ARow});
			try
			{
				using (RowTreeSearchPath LSearchPath = new RowTreeSearchPath())
				{
					int LEntryNumber;
					bool LResult = ClusteredIndex.FindKey(AProcess, ClusteredIndex.KeyRowType, (NativeRow)LClusteredKey.AsNative, LSearchPath, out LEntryNumber);
					if (!LResult)
						throw new IndexException(IndexException.Codes.KeyNotFound);
						
					Row LClusteredData = new Row(AProcess, ClusteredIndex.DataRowType, LSearchPath.DataNode.DataNode.Rows[LEntryNumber]);
					try
					{
						foreach (NativeRowTree LBufferIndex in NonClusteredIndexes)
						{
							Row LKey = GetIndexData(AProcess, LBufferIndex.KeyRowType, new Row[]{LClusteredKey, LClusteredData});
							try
							{
								LBufferIndex.Delete(AProcess, (NativeRow)LKey.AsNative);
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

				ClusteredIndex.Delete(AProcess, (NativeRow)LClusteredKey.AsNative);
			}
			finally
			{	
				LClusteredKey.Dispose();
			}
			
			FRowCount--;
		}
		
		public void Truncate(ServerProcess AProcess)
		{
			Drop(AProcess);
			Create(AProcess);
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
