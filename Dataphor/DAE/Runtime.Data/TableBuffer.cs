/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.IO;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	/*
		TableBufferIndex
		TableBuffer
	*/

	/// <remarks>
	/// Provides the base class for an implementation of a storage level index structure.
	///	Used by the TableBuffer class to represent a single index in the table data.
	/// Also implements the call back from the actual Index class.
	/// </remarks>	
	public class TableBufferIndex : Index
	{
		public TableBufferIndex
		(
			ServerProcess AProcess,
			Schema.Order AKey,
			Schema.RowType AKeyRowType,
			Schema.RowType ADataRowType,
			bool AIsClustered, 
			int AFanout, 
			int ACapacity
		) : base(AFanout, ACapacity, AKeyRowType.StaticByteSize, ADataRowType.StaticByteSize)
		{
			FKey = AKey;
			#if DEBUG
			for (int LIndex = 0; LIndex < FKey.Columns.Count; LIndex++)
				if (FKey.Columns[LIndex].Sort == null)
					throw new Exception("Sort is null");
			#endif
			
			FKeyRowType = AKeyRowType;
			FDataRowType = ADataRowType;
			FIsClustered = AIsClustered;
			Create(AProcess);
		}
		
		private Schema.Order FKey;
		public Schema.Order Key { get { return FKey; } }

		protected Schema.RowType FKeyRowType;
		public Schema.RowType KeyRowType { get { return FKeyRowType; } }
		
		protected Schema.RowType FDataRowType;
		public Schema.RowType DataRowType { get { return FDataRowType; } }
		
		private bool FIsClustered;
		public bool IsClustered { get { return FIsClustered; } }

		public override int Compare(ServerProcess AProcess, Stream AIndexKey, object AIndexContext, Stream ACompareKey, object ACompareContext)
		{
			// If AIndexContext is null, the index stream will have the structure of an index key,
			// Otherwise, the IndexKey stream could be a subset of the actual index key.
			// In that case, AIndexContext will be the RowType for the IndexKey stream
			// It is the caller's responsibility to ensure that the passed IndexKey RowType 
			// is a subset of the actual IndexKey with order intact.
			Row LIndexKey;
			if (AIndexContext == null)
				LIndexKey = AProcess.RowManager.RequestRow(AProcess, FKeyRowType, AIndexKey);
			else
				LIndexKey = AProcess.RowManager.RequestRow(AProcess, (Schema.RowType)AIndexContext, AIndexKey);
				
			// If ACompareContext is null, the compare stream will have the structure of an index key,
			// Otherwise the CompareKey could be a subset of the actual index key.
			// In that case, ACompareContext will be the RowType for the CompareKey stream
			// It is the caller's responsibility to ensure that the passed CompareKey RowType 
			// is a subset of the IndexKey with order intact.
			Row LCompareKey;
			if (ACompareContext == null)
				LCompareKey = AProcess.RowManager.RequestRow(AProcess, FKeyRowType, ACompareKey);
			else
				LCompareKey = AProcess.RowManager.RequestRow(AProcess, (Schema.RowType)ACompareContext, ACompareKey);
				
			int LResult = 0;
			for (int LIndex = 0; LIndex < LIndexKey.DataType.Columns.Count; LIndex++)
			{
				if (LIndex >= LCompareKey.DataType.Columns.Count)
					break;

				if (LIndexKey.HasValue(LIndex) && LCompareKey.HasValue(LIndex))
				{
					Scalar LIndexValue = LIndexKey[LIndex];
					Scalar LCompareValue = LCompareKey[LIndex];
					AProcess.Context.Push(new DataVar(LIndexValue.DataType, LIndexValue));
					AProcess.Context.Push(new DataVar(LCompareValue.DataType, LCompareValue));
					DataVar LResultVar = FKey.Columns[LIndex].Sort.CompareNode.Execute(AProcess);
					LResult = FKey.Columns[LIndex].Ascending ? ((Scalar)LResultVar.Value).ToInt32() : -((Scalar)LResultVar.Value).ToInt32();
					AProcess.Context.Pop();
					AProcess.Context.Pop();
				}
				else if (LIndexKey.HasValue(LIndex))
				{
					// Index Key Has A Value
					LResult = 1;
				}
				else if (LCompareKey.HasValue(LIndex))
				{
					// Compare Key Has A Value
					LResult = -1;
				}
				else
				{
					// Neither key has a value
					LResult = 0;
				}
				
				if (LResult != 0)
					break;
			}
			
			AProcess.RowManager.ReleaseRow(LIndexKey);
			AProcess.RowManager.ReleaseRow(LCompareKey);
			return LResult;
		}
		
		public override void CopyKey(ServerProcess AProcess, Stream ASourceKey, Stream ATargetKey)
		{
			Row LSourceKey = AProcess.RowManager.RequestRow(AProcess, FKeyRowType, ASourceKey);
			try
			{
				Row LTargetKey = AProcess.RowManager.RequestRow(AProcess, FKeyRowType, ATargetKey);
				try
				{
					LSourceKey.CopyTo(LTargetKey);
				}
				finally
				{
					AProcess.RowManager.ReleaseRow(LTargetKey);
				}
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LSourceKey);
			}
		}
		
		public override void CopyData(ServerProcess AProcess, Stream ASourceData, Stream ATargetData)
		{
			Row LSourceRow = AProcess.RowManager.RequestRow(AProcess, FDataRowType, ASourceData);
			try
			{
				Row LTargetRow = AProcess.RowManager.RequestRow(AProcess, FDataRowType, ATargetData);
				try
				{
					LSourceRow.CopyTo(LTargetRow);
				}
				finally
				{
					AProcess.RowManager.ReleaseRow(LTargetRow);
				}
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LSourceRow);
			}
		}
		
		public override void DisposeKey(ServerProcess AProcess, Stream AKey)
		{
			Row LKey = AProcess.RowManager.RequestRow(AProcess, FKeyRowType, AKey);
			try
			{
				LKey.ValuesOwned = true;
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LKey);
			}
		}
		
		public override void DisposeData(ServerProcess AProcess, Stream AData)
		{
			Row ARow = AProcess.RowManager.RequestRow(AProcess, FDataRowType, AData);
			try
			{
				ARow.ValuesOwned = true;
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(ARow);
			}
		}
	}
	
	public class TableBufferIndexes	: DisposableList
	{
		public TableBufferIndexes() : base(){}

		public new TableBufferIndex this[int AIndex]
		{
			get { return (TableBufferIndex)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(Schema.Order AKey)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AKey.Equivalent(this[LIndex].Key))
					return LIndex;
			return -1;
		}
		
		public bool Contains(Schema.Order AKey)
		{
			return IndexOf(AKey) >= 0;
		}
		
		public TableBufferIndex this[Schema.Order AKey] { get { return this[IndexOf(AKey)]; } }
	}

	/// <remarks>
	/// Provides the base class for a TableBuffer, the grouping of index structures together to store the data for
	///	a single table or presentation variable.
	/// </remarks>
	public class TableBuffer : Disposable
	{
		public const int CDefaultClusteredFanout = 100;
		public const int CDefaultClusteredCapacity = 100;
		public const int CDefaultNonClusteredFanout = 100;
		public const int CDefaultNonClusteredCapacity = 100;
		
		#if USEINTERNALID
		public const string CInternalIDColumnName = @"__InternalID";
		#endif
		
		public TableBuffer(ServerProcess AProcess, Schema.TableVar ATableVar, int AFanout, int ACapacity)
		{
			FTableVar = ATableVar;
			FFanout = AFanout;
			FCapacity = ACapacity;
			InternalInitialize(AProcess);
		}
		
		public TableBuffer(ServerProcess AProcess, Schema.TableVar ATableVar) : base()
		{
			FTableVar = ATableVar;
			FFanout = CDefaultClusteredFanout;
			FCapacity = CDefaultClusteredCapacity;
			InternalInitialize(AProcess);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FIndexes != null)
			{
				FIndexes.Dispose();
				FIndexes = null;
			}
			
			FClusteredIndex = null;
			FTableVar = null;
			
			base.Dispose(ADisposing);
		}
		
		// TODO: Compile row types for each index, saving column indexes to prevent the need for lookup during insert, update, and delete.		
		private void InternalInitialize(ServerProcess AProcess)
		{
			Schema.RowType LKeyRowType;
			Schema.RowType LDataRowType;

			// Create the indexes required to store data as described by the given table type
			// Determine Fanout, Capacity, Clustering Key, KeyLength, DataLength
			FClusteringKey = TableVar.FindClusteringOrder(AProcess.Plan);
			//Schema.Key LClusteringKey = TableVar.FindClusteringKey();
			//FClusteringKey = new Schema.Order(LClusteringKey, AProcess.Plan);
			LKeyRowType = new Schema.RowType(FClusteringKey.Columns);
			LDataRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in TableVar.DataType.Columns)
				if (!FClusteringKey.Columns.Contains(LColumn.Name))
					LDataRowType.Columns.Add(new Schema.Column(LColumn.Name, LColumn.DataType));
				
			// Add an internal identifier for uniqueness of keys in nonunique indexes
			#if USEINTERNALID
			FInternalIDColumn = new Schema.TableVarColumn(new Schema.Column(CInternalIDColumnName, AProcess.Plan.Catalog.DataTypes.SystemGuid), Schema.TableVarColumnType.InternalID);
			LDataRowType.Columns.Add(FInternalIDColumn.Column);
			#endif
					
			// Create the Clustered index
			FClusteredIndex = 
				new TableBufferIndex
				(
					AProcess,
					FClusteringKey,
					LKeyRowType,
					LDataRowType,
					true,
					FFanout,
					FCapacity
				);
			Indexes.Add(FClusteredIndex);

			// DataLength and DataColumns for all non clustered indexes is the key length and columns of the clustered key				
			LDataRowType = LKeyRowType;
				
			// Create non clustered indexes for each key and order (unique sets)
			Schema.Order LKey;
			foreach (Schema.Key LNonClusteredKey in TableVar.Keys)
				if (!FClusteringKey.Includes(LNonClusteredKey))
				{
					LKey = new Schema.Order(LNonClusteredKey, AProcess.Plan);
					if (!Indexes.Contains(LKey))
					{
						LKeyRowType = new Schema.RowType(LKey.Columns);
							
						Indexes.Add
						(
							new TableBufferIndex
							(
								AProcess,
								LKey,
								LKeyRowType,
								LDataRowType,
								false,
								FFanout,
								FCapacity
							)
						);						
					}
				}

			foreach (Schema.Order LOrder in TableVar.Orders)
			{
				// This is a potentially non-unique index, so add a GUID to ensure uniqueness of the key in the BTree
				LKey = new Schema.Order(LOrder);
				#if USEINTERNALID
				if (!LKey.Includes(LClusteringKey))
				{
					Schema.OrderColumn LUniqueColumn = new Schema.OrderColumn(FInternalIDColumn, true);
					LUniqueColumn.Sort = ((Schema.ScalarType)LUniqueColumn.Column.DataType).GetUniqueSort(AProcess.Plan);
					LKey.Columns.Add(LUniqueColumn);
				}
				#endif

				if (!Indexes.Contains(LKey))
				{
					LKeyRowType = new Schema.RowType(LKey.Columns);
									  
					Indexes.Add
					(
						new TableBufferIndex
						(
							AProcess,
							LKey,
							LKeyRowType,
							LDataRowType,
							false,
							FFanout,
							FCapacity
						)
					);
				}
			}
		}
		
		public void Drop(ServerProcess AProcess)
		{
			for (int LIndex = FIndexes.Count - 1; LIndex >= 0; LIndex--)
			{
				#if !FINDLEAKS
				try
				{
				#endif
					((TableBufferIndex)FIndexes[LIndex]).Drop(AProcess);
				#if !FINDLEAKS
				}
				catch
				{
					// This should not prevent the table buffer from dropping
					// TODO: Log this error
				}
				#endif
				FIndexes.RemoveAt(LIndex);
			}
		}
		
		private int FFanout;
		private int FCapacity;
		
		private Schema.TableVar FTableVar;
		public Schema.TableVar TableVar { get { return FTableVar; } }
		
		private TableBufferIndexes FIndexes = new TableBufferIndexes();
		public TableBufferIndexes Indexes { get { return FIndexes; } }
		
		private TableBufferIndex FClusteredIndex;
		public TableBufferIndex ClusteredIndex { get { return FClusteredIndex; } }
		
		private Schema.Order FClusteringKey;
		#if USEINTERNALID
		private Schema.TableVarColumn FInternalIDColumn;
		#endif
		
		/// <summary>Inserts the given row into all the indexes of the TableBuffer.</summary>
		/// <param name="ARow">The given row must conform to the structure of the TableBuffer.</param>
		public void Insert(ServerProcess AProcess, Row ARow)
		{
			// Insert the row into all indexes
			Row LKey;
			Row LData;
			int LColumnIndex;
			#if USEINTERNALID
			Scalar LScalar = new Scalar(AProcess, (Schema.IScalarType)FInternalIDColumn.DataType, Guid.NewGuid());
			#endif
			foreach (TableBufferIndex LBufferIndex in Indexes)
			{
				LKey = AProcess.RowManager.RequestRow(AProcess, LBufferIndex.KeyRowType);
				try
				{
					LData = AProcess.RowManager.RequestRow(AProcess, LBufferIndex.DataRowType);
					try
					{
						LKey.ValuesOwned = false;
						LData.ValuesOwned = false;

						for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
							#if USEINTERNALID
							if (LKey.DataType.Columns[LIndex].Name == CInternalIDColumnName)
								LKey[LIndex] = LScalar;
							else
							#endif
							{
								LColumnIndex = ARow.DataType.Columns.IndexOf(LKey.DataType.Columns[LIndex].Name);
								if (((Row)ARow).HasValue(LColumnIndex))
									LKey[LIndex] = ARow[LColumnIndex];
								else
									LKey.ClearValue(LIndex);
							}

						for (int LIndex = 0; LIndex < LData.DataType.Columns.Count; LIndex++)
							#if USEINTERNALID
							if (LData.DataType.Columns[LIndex].Name == CInternalIDColumnName)
								LData[LIndex] = LScalar;
							else
							#endif
							{
								LColumnIndex = ARow.DataType.Columns.IndexOf(LData.DataType.Columns[LIndex].Name);
								if (((Row)ARow).HasValue(LColumnIndex))
									LData[LIndex] = ARow[LColumnIndex];
								else
									LData.ClearValue(LIndex);
							}

						LBufferIndex.Insert(AProcess, LKey.Stream, LData.Stream);
					}
					finally
					{
						AProcess.RowManager.ReleaseRow(LData);
					}
				}
				finally
				{
					AProcess.RowManager.ReleaseRow(LKey);
				}
			}
		}
		
		private Row GetIndexData(ServerProcess AProcess, Schema.RowType ARowType, Row[] ASourceRows)
		{
			Row LRow = AProcess.RowManager.RequestRow(AProcess, ARowType);
			try
			{
				int LColumnIndex;
				bool LFound;
				for (int LIndex = 0; LIndex < LRow.DataType.Columns.Count; LIndex++)
				{
					LFound = false;
					foreach (Row LSourceRow in ASourceRows)
					{
						LColumnIndex = LSourceRow.DataType.Columns.IndexOf(LRow.DataType.Columns[LIndex].Name);
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
				AProcess.RowManager.ReleaseRow(LRow);
				throw;
			}
		}
		
		private bool GetIsIndexAffected(Schema.RowType ARowType, Row ARow)
		{
			foreach (Schema.Column LColumn in ARowType.Columns)
				if (ARow.DataType.Columns.Contains(LColumn.Name))
					return true;
			return false;
		}
		
		public bool HasRow(ServerProcess AProcess, Row ARow)
		{
			Row LKey = GetIndexData(AProcess, FClusteredIndex.KeyRowType, new Row[]{ARow});
			try
			{
				using (SearchPath LSearchPath = new SearchPath())
				{
					int LEntryNumber;
					return FClusteredIndex.FindKey(AProcess, LKey.Stream, null, LSearchPath, out LEntryNumber);
				}
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LKey);
			}
		}
		
		public void Update(ServerProcess AProcess, Row AOldRow, Row ANewRow)
		{
			// AOldRow must have at least the columns of the clustered index key
			Row LOldClusteredKey = GetIndexData(AProcess, FClusteredIndex.KeyRowType, new Row[]{AOldRow});
			try
			{
				bool LIsClusteredIndexKeyAffected = GetIsIndexAffected(FClusteredIndex.KeyRowType, ANewRow);
				bool LIsClusteredIndexDataAffected = GetIsIndexAffected(FClusteredIndex.DataRowType, ANewRow);

				Row LNewClusteredKey = null;
				Row LNewClusteredData = null;
				try
				{					
					// Update the row in each index
					using (SearchPath LSearchPath = new SearchPath())
					{
						int LEntryNumber;
						bool LResult = FClusteredIndex.FindKey(AProcess, LOldClusteredKey.Stream, null, LSearchPath, out LEntryNumber);
						if (!LResult)
							throw new IndexException(IndexException.Codes.KeyNotFound);
							
						Row LOldClusteredData = AProcess.RowManager.RequestRow(AProcess, FClusteredIndex.DataRowType, LSearchPath.DataNode.Data(LEntryNumber));
						try
						{
							bool LIsIndexAffected;
							foreach (TableBufferIndex LBufferIndex in Indexes)
							{
								if (LBufferIndex != FClusteredIndex)
								{
									LIsIndexAffected = GetIsIndexAffected(LBufferIndex.KeyRowType, ANewRow);
									
									if (LIsClusteredIndexKeyAffected || LIsIndexAffected)
									{
										Row LOldIndexKey = GetIndexData(AProcess, LBufferIndex.KeyRowType, new Row[]{LOldClusteredKey, LOldClusteredData});
										try
										{
											Row LNewIndexKey = null;
											Row LNewIndexData = null;
											try
											{
												if (LIsIndexAffected)
													LNewIndexKey = GetIndexData(AProcess, LBufferIndex.KeyRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
													
												if (LIsClusteredIndexKeyAffected)
													LNewIndexData = GetIndexData(AProcess, LBufferIndex.DataRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
													
												if (LIsIndexAffected && LIsClusteredIndexKeyAffected)
												{
													LBufferIndex.Update(AProcess, LOldIndexKey.Stream, LNewIndexKey.Stream, LNewIndexData.Stream);
													LNewIndexKey.ValuesOwned = false;
													LNewIndexData.ValuesOwned = false;
												}
												else if (LIsIndexAffected)
												{
													LBufferIndex.Update(AProcess, LOldIndexKey.Stream, LNewIndexKey.Stream);
													LNewIndexKey.ValuesOwned = false;
												}
												else if (LIsClusteredIndexKeyAffected)
												{
													LBufferIndex.Update(AProcess, LOldIndexKey.Stream, LOldIndexKey.Stream, LNewIndexData.Stream);
													LNewIndexData.ValuesOwned = false;
												}
											}
											finally
											{
												if (LNewIndexKey != null)
													AProcess.RowManager.ReleaseRow(LNewIndexKey);
													
												if (LNewIndexData != null)
													AProcess.RowManager.ReleaseRow(LNewIndexData);
											}
										}
										finally
										{
											AProcess.RowManager.ReleaseRow(LOldIndexKey);
										}
									}
								}
							}
							
							if (LIsClusteredIndexKeyAffected)
								LNewClusteredKey = GetIndexData(AProcess, FClusteredIndex.KeyRowType, new Row[]{ANewRow, LOldClusteredKey, LOldClusteredData});
								
							if (LIsClusteredIndexDataAffected)
								LNewClusteredData = GetIndexData(AProcess, FClusteredIndex.DataRowType, new Row[]{ANewRow, LOldClusteredData});
						}
						finally
						{
							AProcess.RowManager.ReleaseRow(LOldClusteredData);
						}
					}

					if (LIsClusteredIndexKeyAffected && LIsClusteredIndexDataAffected)
					{
						FClusteredIndex.Update(AProcess, LOldClusteredKey.Stream, LNewClusteredKey.Stream, LNewClusteredData.Stream);
						LNewClusteredKey.ValuesOwned = false;
						LNewClusteredData.ValuesOwned = false;
					}				
					else if (LIsClusteredIndexKeyAffected)
					{
						FClusteredIndex.Update(AProcess, LOldClusteredKey.Stream, LNewClusteredKey.Stream);
						LNewClusteredKey.ValuesOwned = false;
					}
					else if (LIsClusteredIndexDataAffected)
					{
						FClusteredIndex.Update(AProcess, LOldClusteredKey.Stream, LOldClusteredKey.Stream, LNewClusteredData.Stream);
						LNewClusteredData.ValuesOwned = false;
					}
				}
				finally
				{
					if (LNewClusteredKey != null)
						AProcess.RowManager.ReleaseRow(LNewClusteredKey);
						
					if (LNewClusteredData != null)
						AProcess.RowManager.ReleaseRow(LNewClusteredData);
				}
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LOldClusteredKey);
			}
		}

		public void Delete(ServerProcess AProcess, Row ARow)
		{
			// Delete the row from all indexes
			Row LClusteredKey = GetIndexData(AProcess, FClusteredIndex.KeyRowType, new Row[]{ARow});
			try
			{
				using (SearchPath LSearchPath = new SearchPath())
				{
					int LEntryNumber;
					bool LResult = FClusteredIndex.FindKey(AProcess, LClusteredKey.Stream, null, LSearchPath, out LEntryNumber);
					if (!LResult)
						throw new IndexException(IndexException.Codes.KeyNotFound);
						
					Row LClusteredData = AProcess.RowManager.RequestRow(AProcess, FClusteredIndex.DataRowType, LSearchPath.DataNode.Data(LEntryNumber));
					try
					{
						foreach (TableBufferIndex LBufferIndex in Indexes)
						{
							if (LBufferIndex != FClusteredIndex)
							{
								Row LKey = GetIndexData(AProcess, LBufferIndex.KeyRowType, new Row[]{LClusteredKey, LClusteredData});
								try
								{
									LBufferIndex.Delete(AProcess, LKey.Stream);
								}
								finally
								{
									AProcess.RowManager.ReleaseRow(LKey);
								}
							}
						}
					}
					finally
					{
						AProcess.RowManager.ReleaseRow(LClusteredData);
					}
				}

				FClusteredIndex.Delete(AProcess, LClusteredKey.Stream);
			}
			finally
			{	
				AProcess.RowManager.ReleaseRow(LClusteredKey);
			}
		}
		
		public void Truncate(ServerProcess AProcess)
		{
			// This probably wont work due to locking...
			using (Scan LScan = new Scan(AProcess, this, this.ClusteredIndex, ScanDirection.Forward, null, null))
			{
				LScan.Open();
				LScan.Next();
				while (!LScan.EOF())
				{
					Row LKey = LScan.GetKey();
					try
					{
						this.Delete(AProcess, LKey);
					}
					finally
					{
						AProcess.RowManager.ReleaseRow(LKey);
					}
				}
			}
		}
	}
	
	public class TableBuffers : DisposableList
	{
		public TableBuffers() : base(){}
		
		public new TableBuffer this[int AIndex]
		{
			get { return (TableBuffer)base[AIndex]; } 
			set { base[AIndex] = value; }
		}

		public int IndexOf(Schema.TableVar ATableVar)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].TableVar == ATableVar)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Schema.TableVar ATableVar)
		{
			return IndexOf(ATableVar) >= 0;
		}
		
		public TableBuffer this[Schema.TableVar ATableVar]
		{
			get
			{
				int LIndex = IndexOf(ATableVar);
				if (LIndex < 0)
					throw new RuntimeException(RuntimeException.Codes.TableBufferNotFound, ATableVar.Name);
				return this[LIndex];
			}
		}
	}
}

