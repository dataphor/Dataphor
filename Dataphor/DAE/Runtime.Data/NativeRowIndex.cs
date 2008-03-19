/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	/// <remarks>
	/// Provides a callback to notify users of the index when a set of rows has moved.
	/// </remarks>
	public delegate void NativeRowTreeRowsMovedHandler(NativeRowTree ATree, NativeRowTreeNode AOldNode, int AOldEntryNumberMin, int AOldEntryNumberMax, NativeRowTreeNode ANewNode, int AEntryNumberDelta);

	/// <remarks>
	/// Provides a callback to notify users of the index when a row is deleted.
	/// </remarks>
	public delegate void NativeRowTreeRowDeletedHandler(NativeRowTree ATree, NativeRowTreeNode ANode, int AEntryNumber);
	
/*
	public abstract class NativeRowIndex : System.Object {}
	
	public class NativeRowIndexList : TypedList
	{
		public NativeRowIndexList() : base(typeof(NativeRowIndex)) {}
		
		public new NativeRowIndex this[int AIndex]
		{
			get { return (NativeRowIndex)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
*/
	
	public class NativeRowTreeList : TypedList
	{
		public NativeRowTreeList() : base(typeof(NativeRowTree)) {}
		
		public new NativeRowTree this[int AIndex]
		{
			get { return (NativeRowTree)base[AIndex]; }
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
		
		public NativeRowTree this[Schema.Order AKey] { get { return this[IndexOf(AKey)]; } }
	}
	
	/// <remarks>
	/// Provides a storage structure for the search path followed by the find key in terms of index nodes.
	/// See the description of the FindKey method for the Index class for more information.
	/// </remarks>
	public class RowTreeSearchPath : DisposableTypedList
	{
		public RowTreeSearchPath() : base(typeof(RowTreeNode), true, false){}
		
		public new RowTreeNode this[int AIndex]
		{
			get { return (RowTreeNode)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public RowTreeNode DataNode { get { return this[Count - 1]; } }
		
		public new RowTreeNode RemoveAt(int AIndex)
		{
			return (RowTreeNode)base.RemoveItemAt(AIndex);
		}
		
		public new RowTreeNode DisownAt(int AIndex)
		{
			return (RowTreeNode)base.DisownAt(AIndex);
		}
	}
	
	public class RowTreeNode : Disposable
	{
		public RowTreeNode(ServerProcess AProcess, NativeRowTree ATree, NativeRowTreeNode ANode, LockMode ALockMode)
		{
			Process = AProcess;
			Tree = ATree;
			Node = ANode;
			DataNode = Node as NativeRowTreeDataNode;
			RoutingNode = Node as NativeRowTreeRoutingNode;
			#if LOCKROWTREE
			Process.Lock(Node.LockID, ALockMode);
			#endif
		}
		
		public ServerProcess Process;
		public NativeRowTree Tree;
		
		public NativeRowTreeNode Node;
		public NativeRowTreeDataNode DataNode;
		public NativeRowTreeRoutingNode RoutingNode;
		
		protected override void Dispose(bool ADisposing)
		{
			if (Process != null)
			{
				#if LOCKROWTREE
				Process.Unlock(Node.LockID);
				#endif
				Process = null;
			}
			base.Dispose(ADisposing);
		}

		/// <summary>
		/// Performs a binary search among the entries in this node for the given key.  Will always return an
		/// entry index in AEntryNumber, which is the index of the entry that was found if the method returns true,
		/// otherwise it is the index where the key should be inserted if the method returns false.
		/// </summary>
		public bool NodeSearch(Schema.IRowType AKeyRowType, NativeRow AKey, out int AEntryNumber)
		{
			int LLo = (Node.NodeType == NativeRowTreeNodeType.Routing ? 1 : 0);
			int LHi = Node.EntryCount - 1;
			int LIndex = 0;
			int LResult = -1;
			
			while (LLo <= LHi)
			{
				LIndex = (LLo + LHi) / 2;
				LResult = Tree.Compare(Process, Tree.KeyRowType, Node.Keys[LIndex], AKeyRowType, AKey);
				if (LResult == 0)
					break;
				else if (LResult > 0)
					LHi = LIndex - 1;
				else // if (LResult < 0) unnecessary
					LLo = LIndex + 1;
			}
			
			if (LResult == 0)
				AEntryNumber = LIndex;
			else
				AEntryNumber = LLo;
				
			return LResult == 0;
		}

		/// <summary>
		/// The recursive portion of the find key algorithm invoked by the FindKey method of the parent Index.
		/// </summary>
		public bool FindKey(Schema.IRowType AKeyRowType, NativeRow AKey, RowTreeSearchPath ARowTreeSearchPath, out int AEntryNumber)
		{
			ARowTreeSearchPath.Add(this);
			if (Node.NodeType == NativeRowTreeNodeType.Routing)
			{
				// Perform a binary search among the keys in this node to determine which streamid to follow for the next node
				bool LResult = NodeSearch(AKeyRowType, AKey, out AEntryNumber);

				// If the key was found, use the given entry number, otherwise, use the one before the given entry
				AEntryNumber = LResult ? AEntryNumber : (AEntryNumber - 1);
				return
					new RowTreeNode
					(
						Process,
						Tree, 
						RoutingNode.Nodes[AEntryNumber],
						LockMode.Shared
					).FindKey
					(
						AKeyRowType,
						AKey, 
						ARowTreeSearchPath, 
						out AEntryNumber
					);
			}
			else
			{
				// Perform a binary search among the keys in this node to determine which entry, if any, is equal to the given key
				return NodeSearch(AKeyRowType, AKey, out AEntryNumber);
			}
		}
		
		/// <summary>Inserts the given Key and Data streams into this node at the given index.</summary>
		public void InsertData(NativeRow AKey, NativeRow AData, int AEntryNumber)
		{
			DataNode.Insert(AKey, AData, AEntryNumber);
			
			Tree.RowsMoved(Node, AEntryNumber, Node.EntryCount - 2, Node, 1);
		}
		
		public void InsertRouting(NativeRow AKey, NativeRowTreeNode ANode, int AEntryNumber)
		{
			RoutingNode.Insert(AKey, ANode, AEntryNumber);
			
			Tree.RowsMoved(Node, AEntryNumber, Node.EntryCount - 2, Node, 1);
		}
		
		public void UpdateData(NativeRow AData, int AEntryNumber)
		{
			Tree.DisposeData(Process, DataNode.Rows[AEntryNumber]);
			DataNode.Rows[AEntryNumber] = AData;
		}
		
		public void UpdateRouting(NativeRowTreeNode ANode, int AEntryNumber)
		{
			RoutingNode.Nodes[AEntryNumber] = ANode;
		}
		
		public void DeleteData(int AEntryNumber)
		{
			Tree.DisposeData(Process, DataNode.Rows[AEntryNumber]);
			Tree.DisposeKey(Process, DataNode.Keys[AEntryNumber]);
			
			DataNode.Delete(AEntryNumber);
			
			Tree.RowDeleted(Node, AEntryNumber);
			Tree.RowsMoved(Node, AEntryNumber + 1, Node.EntryCount, Node, -1);
		}
		
		public void DeleteRouting(int AEntryNumber)
		{
			Tree.DisposeKey(Process, RoutingNode.Keys[AEntryNumber]);
			
			RoutingNode.Delete(AEntryNumber);
			
			Tree.RowDeleted(Node, AEntryNumber);
			Tree.RowsMoved(Node, AEntryNumber + 1, Node.EntryCount, Node, -1);
		}
	}
	
	/// <remarks>
	///	Provides a generic implementation of a B+Tree structure.
	/// The main characteristics of this structure are Fanout, and Capacity.
	///	Each node in the index is a pair of lists of rows. 
	/// </remarks>	
	public class NativeRowTree : System.Object //NativeRowIndex
	{
		public const int MinimumFanout = 2;
		
		public NativeRowTree
		(
			Schema.Order AKey,
			Schema.RowType AKeyRowType,
			Schema.RowType ADataRowType,
			int AFanout,
			int ACapacity,
			bool AIsClustered
		) : base()
		{
			FKey = AKey;
			#if DEBUG
			for (int LIndex = 0; LIndex < FKey.Columns.Count; LIndex++)
				if (FKey.Columns[LIndex].Sort == null)
					Error.Fail("Sort is null");
			#endif
			
			FKeyRowType = AKeyRowType;
			FDataRowType = ADataRowType;
			FFanout = AFanout < MinimumFanout ? MinimumFanout : AFanout;
			FCapacity = ACapacity;
			IsClustered = AIsClustered;
			Root = new NativeRowTreeDataNode(this);
			Head = Root;
			Tail = Root;
			Height = 1;
		}
		
		/// <summary>The root node in the tree.</summary>
		public NativeRowTreeNode Root;
		
		/// <summary>The head node of the tree.</summary>
		public NativeRowTreeNode Head;
		
		/// <summary>The tail node of the tree.</summary>
		public NativeRowTreeNode Tail;
		
		/// <summary>The height of the tree.</summary>
		public int Height;
		
		private int FFanout;		
		/// <summary>The number of entries per routing node in the tree.</summary>		
		public int Fanout { get { return FFanout; } }

		private int FCapacity;
		/// <summary>The number of entries per data node in the tree.</summary>		
		public int Capacity { get { return FCapacity; } }
		
		private Schema.Order FKey;
		/// <summary>The description of the order for the index.</summary>
		public Schema.Order Key { get { return FKey; } }

		private Schema.RowType FKeyRowType;
		/// <summary>The row type of the key for the index.</summary>
		public Schema.RowType KeyRowType { get { return FKeyRowType; } }
		
		private Schema.RowType FDataRowType;
		/// <summary>The row type for data for the index.</summary>
		public Schema.RowType DataRowType { get { return FDataRowType; } }
		
		public bool IsClustered;

		public void Drop(ServerProcess AProcess)
		{
			// Deallocate all nodes in the tree
			DeallocateNode(AProcess, Root);
			Root = null;
			Tail = null;
			Head = null;
			Height = 0;
		}
		
		protected RowTreeNode AllocateNode(ServerProcess AProcess, NativeRowTreeNodeType ANodeType)
		{
			return new RowTreeNode(AProcess, this, ANodeType == NativeRowTreeNodeType.Routing ? (NativeRowTreeNode)new NativeRowTreeRoutingNode(this) : new NativeRowTreeDataNode(this), LockMode.Exclusive);
		}
		
		protected void DeallocateNode(ServerProcess AProcess, NativeRowTreeNode ANode)
		{
			using (RowTreeNode LNode = new RowTreeNode(AProcess, this, ANode, LockMode.Exclusive))
			{
				NativeRowTreeDataNode LDataNode = ANode as NativeRowTreeDataNode;
				NativeRowTreeRoutingNode LRoutingNode = ANode as NativeRowTreeRoutingNode;
				for (int LEntryIndex = 0; LEntryIndex < ANode.EntryCount; LEntryIndex++)
				{
					if (ANode.NodeType == NativeRowTreeNodeType.Routing)
					{
						if (LEntryIndex > 0)
							DisposeKey(AProcess, ANode.Keys[LEntryIndex]);
						DeallocateNode(AProcess, LRoutingNode.Nodes[LEntryIndex]);
					}
					else
					{
						DisposeKey(AProcess, LDataNode.Keys[LEntryIndex]);
						DisposeData(AProcess, LDataNode.Rows[LEntryIndex]);
					}
				}
				
				if (ANode.NextNode == null)
					Tail = ANode.PriorNode;
				else
				{
					using (RowTreeNode LNextNode = new RowTreeNode(AProcess, this, ANode.NextNode, LockMode.Exclusive))
					{
						LNextNode.Node.PriorNode = ANode.PriorNode;
					}
				}
					
				if (ANode.PriorNode == null)
					Head = ANode.NextNode;
				else
				{
					using (RowTreeNode LPriorNode = new RowTreeNode(AProcess, this, ANode.PriorNode, LockMode.Exclusive))
					{
						LPriorNode.Node.NextNode = ANode.NextNode;
					}
				}
			}
		}
		
		/// <summary>
		/// The given streams are copied into the index, so references within the streams 
		/// are considered owned by the index after the insert.
		/// </summary>
		public void Insert(ServerProcess AProcess, NativeRow AKey, NativeRow AData)
		{
			int LEntryNumber;
			using (RowTreeSearchPath LRowTreeSearchPath = new RowTreeSearchPath())
			{
				bool LResult = FindKey(AProcess, KeyRowType, AKey, LRowTreeSearchPath, out LEntryNumber);
				if (LResult)
					throw new IndexException(IndexException.Codes.DuplicateKey);
					
				InternalInsert(AProcess, LRowTreeSearchPath, LEntryNumber, AKey, AData);
			}
		}
		
		private int Split(NativeRowTreeNode ASourceNode, NativeRowTreeNode ATargetNode)
		{
			int LEntryCount = ASourceNode.EntryCount;
			int LEntryPivot = LEntryCount / 2;
			if (ASourceNode.NodeType == NativeRowTreeNodeType.Data)
			{
				NativeRowTreeDataNode LSourceDataNode = (NativeRowTreeDataNode)ASourceNode;
				NativeRowTreeDataNode LTargetDataNode = (NativeRowTreeDataNode)ATargetNode;

				// Insert the upper half of the entries from ASourceNode into ATargetNode
				for (int LEntryIndex = LEntryPivot; LEntryIndex < LEntryCount; LEntryIndex++)
					LTargetDataNode.Insert(LSourceDataNode.Keys[LEntryIndex], LSourceDataNode.Rows[LEntryIndex], LEntryIndex - LEntryPivot);

				// Remove the upper half of the entries from ASourceNode					
				for (int LEntryIndex = LEntryCount - 1; LEntryIndex >= LEntryPivot; LEntryIndex--)
					LSourceDataNode.Delete(LEntryIndex); // Don't dispose the values here, this is a move
			}
			else
			{
				NativeRowTreeRoutingNode LSourceRoutingNode = (NativeRowTreeRoutingNode)ASourceNode;
				NativeRowTreeRoutingNode LTargetRoutingNode = (NativeRowTreeRoutingNode)ATargetNode;
	
				// Insert the upper half of the entries from ASourceNode into ATargetNode
				for (int LEntryIndex = LEntryPivot; LEntryIndex < LEntryCount; LEntryIndex++)
					LTargetRoutingNode.Insert(LSourceRoutingNode.Keys[LEntryIndex], LSourceRoutingNode.Nodes[LEntryIndex], LEntryIndex - LEntryPivot);
					
				// Remove the upper half of the entries from ASourceNode					
				for (int LEntryIndex = LEntryCount - 1; LEntryIndex >= LEntryPivot; LEntryIndex--)
					LSourceRoutingNode.Delete(LEntryIndex);
			}

			// Notify index clients of the data change
			RowsMoved(ASourceNode, LEntryPivot, LEntryCount - 1, ATargetNode, -LEntryPivot);
			
			return LEntryPivot;
		}
		
		private void InternalInsert(ServerProcess AProcess, RowTreeSearchPath ARowTreeSearchPath, int AEntryNumber, NativeRow AKey, NativeRow AData)
		{
			// Walk back up the search path, inserting data and splitting pages as necessary
			RowTreeNode LNewRowTreeNode;
			NativeRowTreeNode LSplitNode = null;
			for (int LIndex = ARowTreeSearchPath.Count - 1; LIndex >= 0; LIndex--)
			{
				if (ARowTreeSearchPath[LIndex].Node.EntryCount >= Capacity)
				{
					// Allocate a new node
					using (LNewRowTreeNode = AllocateNode(AProcess, ARowTreeSearchPath[LIndex].Node.NodeType))
					{
						// Thread it into the list of leaves, if necessary
						if (LNewRowTreeNode.Node.NodeType == NativeRowTreeNodeType.Data)
						{
							LNewRowTreeNode.Node.PriorNode = ARowTreeSearchPath[LIndex].Node;
							LNewRowTreeNode.Node.NextNode = ARowTreeSearchPath[LIndex].Node.NextNode;
							ARowTreeSearchPath[LIndex].Node.NextNode = LNewRowTreeNode.Node;
							if (LNewRowTreeNode.Node.NextNode == null)
								Tail = LNewRowTreeNode.Node;
							else
							{
								using (RowTreeNode LNextRowTreeNode = new RowTreeNode(AProcess, this, LNewRowTreeNode.Node.NextNode, LockMode.Exclusive))
								{
									LNextRowTreeNode.Node.PriorNode = LNewRowTreeNode.Node;
								}
							}
						}
						
						int LEntryPivot = Split(ARowTreeSearchPath[LIndex].Node, LNewRowTreeNode.Node);
						
						// Insert the new entry into the appropriate node
						if (AEntryNumber >= LEntryPivot)
							if (LNewRowTreeNode.Node.NodeType == NativeRowTreeNodeType.Data)
								LNewRowTreeNode.InsertData(AKey, AData, AEntryNumber - LEntryPivot);
							else
								LNewRowTreeNode.InsertRouting(AKey, LSplitNode, AEntryNumber - LEntryPivot);
						else
							if (LNewRowTreeNode.Node.NodeType == NativeRowTreeNodeType.Data)
								ARowTreeSearchPath[LIndex].InsertData(AKey, AData, AEntryNumber);
							else
								ARowTreeSearchPath[LIndex].InsertRouting(AKey, LSplitNode, AEntryNumber);
							
						// Reset the AKey for the next round
						// The key for the entry one level up is the first key for the newly allocated node
						AKey = CopyKey(AProcess, LNewRowTreeNode.Node.Keys[0]);
						
						// Set LSplitNode to the newly allocated node
						LSplitNode = LNewRowTreeNode.Node;
					}

					if (LIndex == 0)
					{
						// Allocate a new root node and grow the height of the tree by 1
						using (LNewRowTreeNode = AllocateNode(AProcess, NativeRowTreeNodeType.Routing))
						{
							LNewRowTreeNode.InsertRouting(null, ARowTreeSearchPath[LIndex].Node, 0); // 1st key of a routing node is not used
							LNewRowTreeNode.InsertRouting(AKey, LSplitNode, 1);
							Root = LNewRowTreeNode.Node;
							Height++;
						}
					}
					else
					{
						// reset AEntryNumber for the next round
						bool LResult = ARowTreeSearchPath[LIndex - 1].NodeSearch(KeyRowType, AKey, out AEntryNumber);

						// At this point we should be guaranteed to have a routing key which does not exist in the parent node
						if (LResult)
							throw new IndexException(IndexException.Codes.DuplicateRoutingKey);
					}
				}
				else
				{
					if (ARowTreeSearchPath[LIndex].Node.NodeType == NativeRowTreeNodeType.Data)
						ARowTreeSearchPath[LIndex].InsertData(AKey, AData, AEntryNumber);
					else
						ARowTreeSearchPath[LIndex].InsertRouting(AKey, LSplitNode, AEntryNumber);
					break;
				}
			}
		}
		
		/// <summary>Updates the entry given by AOldKey to the stream given by ANewKey.  The data for the entry is moved to the new location.</summary>
		public void Update(ServerProcess AProcess, NativeRow AOldKey, NativeRow ANewKey)
		{
			Update(AProcess, AOldKey, ANewKey, null);
		}
		
		/// <summary>Updates the entry given by AOldKey to the entry given by ANewKey and ANewData.  If AOldKey == ANewKey, the data for the entry is updated in place, otherwise it is moved to the location given by ANewKey.</summary>
		public void Update(ServerProcess AProcess, NativeRow AOldKey, NativeRow ANewKey, NativeRow ANewData)
		{
			int LEntryNumber;
			using (RowTreeSearchPath LRowTreeSearchPath = new RowTreeSearchPath())
			{
				bool LResult = FindKey(AProcess, KeyRowType, AOldKey, LRowTreeSearchPath, out LEntryNumber);
				if (!LResult)
					throw new IndexException(IndexException.Codes.KeyNotFound);
					
				if (Compare(AProcess, KeyRowType, AOldKey, KeyRowType, ANewKey) == 0)
				{
					if (ANewData != null)
						LRowTreeSearchPath.DataNode.UpdateData(ANewData, LEntryNumber);
				}
				else
				{
					if (ANewData == null)
					{
						ANewData = LRowTreeSearchPath.DataNode.DataNode.Rows[LEntryNumber];
						LRowTreeSearchPath.DataNode.DataNode.Delete(LEntryNumber); // Don't dispose here this is a move
					}
					else
						InternalDelete(AProcess, LRowTreeSearchPath, LEntryNumber); // Dispose here this is not a move

					LRowTreeSearchPath.Dispose();
					LResult = FindKey(AProcess, KeyRowType, ANewKey, LRowTreeSearchPath, out LEntryNumber);
					if (LResult)
						throw new IndexException(IndexException.Codes.DuplicateKey);
						
					InternalInsert(AProcess, LRowTreeSearchPath, LEntryNumber, ANewKey, ANewData);
				}
			}
		}
		
		private void InternalDelete(ServerProcess AProcess, RowTreeSearchPath ARowTreeSearchPath, int AEntryNumber)
		{
			ARowTreeSearchPath.DataNode.DeleteData(AEntryNumber);
		}
		
		// TODO: Asynchronous collapsed node recovery
		/// <summary>Deletes the entry given by AKey.  The streams are disposed through the DisposeKey event, so it is the responsibility of the index user to dispose references within the streams.</summary>
		public void Delete(ServerProcess AProcess, NativeRow AKey)
		{
			int LEntryNumber;
			using (RowTreeSearchPath LRowTreeSearchPath = new RowTreeSearchPath())
			{
				bool LResult = FindKey(AProcess, KeyRowType, AKey, LRowTreeSearchPath, out LEntryNumber);
				if (!LResult)
					throw new IndexException(IndexException.Codes.KeyNotFound);
					
				InternalDelete(AProcess, LRowTreeSearchPath, LEntryNumber);
			}
		}

		/// <summary>
		/// Searches for the given key within the index.  ARowTreeSearchPath and AEntryNumber together give the 
		/// location of the key in the index.  If the search is successful, the entry exists, otherwise 
		/// the EntryNumber indicates where the entry should be placed for an insert.
		/// </summary>
		/// <param name="AKey">The key to be found.</param>
		/// <param name="ARowTreeSearchPath">A <see cref="RowTreeSearchPath"/> which will contain the set of nodes along the search path to the key.</param>
		/// <param name="AEntryNumber">The EntryNumber where the key either is, or should be, depending on the result of the find.</param>
		/// <returns>A boolean value indicating the success or failure of the find.</returns>
		public bool FindKey(ServerProcess AProcess, Schema.IRowType AKeyRowType, NativeRow AKey, RowTreeSearchPath ARowTreeSearchPath, out int AEntryNumber)
		{
			return new RowTreeNode(AProcess, this, Root, LockMode.Shared).FindKey(AKeyRowType, AKey, ARowTreeSearchPath, out AEntryNumber);
		}
		
		public int Compare(ServerProcess AProcess, Schema.IRowType AIndexKeyRowType, NativeRow AIndexKey, Schema.IRowType ACompareKeyRowType, NativeRow ACompareKey)
		{
			// If AIndexKeyRowType is null, the index key must have the structure of an index key,
			// Otherwise, the IndexKey row could be a subset of the actual index key.
			// In that case, AIndexKeyRowType is the RowType for the IndexKey row.
			// It is the caller's responsibility to ensure that the passed IndexKey RowType 
			// is a subset of the actual IndexKey with order intact.
			//Row LIndexKey = new Row(AProcess, AIndexKeyRowType, AIndexKey);
				
			// If ACompareContext is null, the compare key must have the structure of an index key,
			// Otherwise the CompareKey could be a subset of the actual index key.
			// In that case, ACompareContext is the RowType for the CompareKey row.
			// It is the caller's responsibility to ensure that the passed CompareKey RowType 
			// is a subset of the IndexKey with order intact.
			//Row LCompareKey = new Row(AProcess, ACompareKeyRowType, ACompareKey);
				
			int LResult = 0;
			for (int LIndex = 0; LIndex < AIndexKeyRowType.Columns.Count; LIndex++)
			{
				if (LIndex >= ACompareKeyRowType.Columns.Count)
					break;
					
				if ((AIndexKey.Values[LIndex] != null) && (ACompareKey.Values[LIndex] != null))
				{
					/*
					if ((AIndexKey.Values[LIndex] is IComparable) && (ACompareKey.Values[LIndex] is IComparable))
						LResult = Key.Columns[LIndex].Ascending ? ((IComparable)AIndexKey.Values[LIndex]).CompareTo(ACompareKey.Values[LIndex]) : -((IComparable)AIndexKey.Values[LIndex]).CompareTo(ACompareKey.Values[LIndex]);
					else
					{
					*/
						AProcess.Context.Push(new DataVar(AIndexKeyRowType.Columns[LIndex].DataType, DataValue.FromNativeRow(AProcess, AIndexKeyRowType, AIndexKey, LIndex)));
						AProcess.Context.Push(new DataVar(ACompareKeyRowType.Columns[LIndex].DataType, DataValue.FromNativeRow(AProcess, ACompareKeyRowType, ACompareKey, LIndex)));
						LResult = Key.Columns[LIndex].Ascending ? 
							Key.Columns[LIndex].Sort.CompareNode.Execute(AProcess).Value.AsInt32 : 
							-(Key.Columns[LIndex].Sort.CompareNode.Execute(AProcess).Value.AsInt32);
						AProcess.Context.Pop();
						AProcess.Context.Pop();
					//} 
				}
				else if (AIndexKey.Values[LIndex] != null)
				{
					LResult = Key.Columns[LIndex].Ascending ? 1 : -1;
				}
				else if (ACompareKey.Values[LIndex] != null)
				{
					LResult = Key.Columns[LIndex].Ascending ? -1 : 1;
				}
				else
				{
					LResult = 0;
				}
				
				if (LResult != 0)
					break;
			}
			
			//LIndexKey.Dispose();
			//LCompareKey.Dispose();
			return LResult;
		}
		
		public NativeRow CopyKey(ServerProcess AProcess, NativeRow ASourceKey)
		{
			return (NativeRow)DataValue.CopyNative(AProcess, KeyRowType, ASourceKey);
		}
		
		public NativeRow CopyData(ServerProcess AProcess, NativeRow ASourceData)
		{
			return (NativeRow)DataValue.CopyNative(AProcess, DataRowType, ASourceData);
		}
		
		public void DisposeKey(ServerProcess AProcess, NativeRow AKey)
		{
			DataValue.DisposeNative(AProcess, KeyRowType, AKey);
		}
		
		public void DisposeData(ServerProcess AProcess, NativeRow AData)
		{
			DataValue.DisposeNative(AProcess, DataRowType, AData);
		}

		public event NativeRowTreeRowsMovedHandler OnRowsMoved;
		public void RowsMoved(NativeRowTreeNode AOldNode, int AOldEntryNumberMin, int AOldEntryNumberMax, NativeRowTreeNode ANewNode, int AEntryNumberDelta)
		{
			if (OnRowsMoved != null)
				OnRowsMoved(this, AOldNode, AOldEntryNumberMin, AOldEntryNumberMax, ANewNode, AEntryNumberDelta);
		}
		
		public event NativeRowTreeRowDeletedHandler OnRowDeleted;
		public void RowDeleted(NativeRowTreeNode ANode, int AEntryNumber)
		{
			if (OnRowDeleted != null)
				OnRowDeleted(this, ANode, AEntryNumber);
		}
	}

	public enum NativeRowTreeNodeType {Routing, Data}
	
	public class NativeRowTreeNode : System.Object
	{
		public NativeRowTreeNode(NativeRowTree ANativeRowTree)
		{
			FNativeRowTree = ANativeRowTree;
			#if LOCKROWTREE
			LockID = new LockID(Server.Server.CStreamManagerID, GetHashCode().ToString());
			#endif
		}
		
		protected NativeRowTree FNativeRowTree;
		public NativeRowTree NativeRowTree { get { return FNativeRowTree; } }
		
		protected NativeRowTreeNodeType FNodeType;
		public NativeRowTreeNodeType NodeType { get { return FNodeType; } }
		
		protected NativeRow[] FKeys;
		public NativeRow[] Keys { get { return FKeys; } }
		
		protected int FEntryCount = 0;
		public int EntryCount { get { return FEntryCount; } }
		
		public NativeRowTreeNode PriorNode;

		public NativeRowTreeNode NextNode;
		
		#if LOCKROWTREE
		public LockID LockID;
		#endif
	}
	
	public class NativeRowTreeRoutingNode : NativeRowTreeNode
	{
		public NativeRowTreeRoutingNode(NativeRowTree ANativeRowTree) : base(ANativeRowTree)
		{
			FNodeType = NativeRowTreeNodeType.Routing;
			FKeys = new NativeRow[FNativeRowTree.Fanout];
			FNodes = new NativeRowTreeNode[FNativeRowTree.Fanout];
		}
		
		private NativeRowTreeNode[] FNodes;
		public NativeRowTreeNode[] Nodes { get { return FNodes; } }
		
		public void Insert(NativeRow AKey, NativeRowTreeNode ANode, int AEntryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(FKeys, AEntryNumber, FKeys, AEntryNumber + 1, FEntryCount - AEntryNumber);
			Array.Copy(FNodes, AEntryNumber, FNodes, AEntryNumber + 1, FEntryCount - AEntryNumber);

			// Set the new entry data			
			FKeys[AEntryNumber] = AKey;
			FNodes[AEntryNumber] = ANode;

			// Increment entry count			
			FEntryCount++;
		}
		
		public void Delete(int AEntryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(FKeys, AEntryNumber + 1, FKeys, AEntryNumber, FEntryCount - AEntryNumber - 1);
			Array.Copy(FNodes, AEntryNumber + 1, FNodes, AEntryNumber, FEntryCount - AEntryNumber - 1);
			
			// Decrement EntryCount
			FEntryCount--;
		}
	}
	
	public class NativeRowTreeDataNode : NativeRowTreeNode
	{
		public NativeRowTreeDataNode(NativeRowTree ANativeRowTree) : base(ANativeRowTree)
		{
			FNodeType = NativeRowTreeNodeType.Data;
			FKeys = new NativeRow[FNativeRowTree.Capacity];
			FRows = new NativeRow[FNativeRowTree.Capacity];
		}
		
		private NativeRow[] FRows;
		public NativeRow[] Rows { get { return FRows; } }

		public void Insert(NativeRow AKey, NativeRow ARow, int AEntryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(FKeys, AEntryNumber, FKeys, AEntryNumber + 1, FEntryCount - AEntryNumber);
			Array.Copy(FRows, AEntryNumber, FRows, AEntryNumber + 1, FEntryCount - AEntryNumber);

			// Set the new entry data			
			FKeys[AEntryNumber] = AKey;
			FRows[AEntryNumber] = ARow;

			// Increment entry count			
			FEntryCount++;
		}
		
		public void Delete(int AEntryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(FKeys, AEntryNumber + 1, FKeys, AEntryNumber, FEntryCount - AEntryNumber - 1);
			Array.Copy(FRows, AEntryNumber + 1, FRows, AEntryNumber, FEntryCount - AEntryNumber - 1);
			
			// Decrement EntryCount
			FEntryCount--;
		}
	}
	
/*
	public class NativeRowHashTable : NativeRowIndex
	{
		private class NativeRowHashTableHashCodeProvider : IHashCodeProvider
		{
			public NativeRowHashTableHashCodeProvider(NativeRowHashTable AHashTable) : base()
			{
				FHashtable = AHashtable;
			}
			
			private NativeRowHashTable FHashtable;
			
			#region IHashCodeProvider Members

			public int GetHashCode(object AObject)
			{
				// TODO:  Add NativeRowHashTableHashCodeProvider.GetHashCode implementation
				return 0;
			}

			#endregion
		}
		
		private class NativeRowHashTableComparer : IComparer
		{
			public NativeRowHashTableComparer(NativeRowHashTable AHashTable) : base()
			{
				FHashtable = AHashtable;
			}
			
			private NativeRowHashTable FHashtable;
			
			#region IComparer Members

			public int Compare(object ALeftValue, object ARightValue)
			{
				// TODO:  Add NativeRowHashTableComparer.Compare implementation
				return 0;
			}

			#endregion
		}

		public NativeRowHashTable() : base()
		{	
			FHashCodeProvider = new NativeRowHashTableHashCodeProvider(this);
			FComparer = new NativeRowHashTableComparer(this);
			FRows = new Hashtable(FHashCodeProvider, FComparer);
		}
		
		private Hashtable FRows;
		
		internal IServerProcess FProcess; // Only used during the Add and Remove calls
		
		private NativeRowHashTableHashCodeProvider FHashCodeProvider;
		
		private NativeRowHashTableComparer FComparer;
		
		public void Add(IServerProcess AProcess, NativeRow AKey, NativeRow AData)
		{
			lock (this)
			{
				FProcess = AProcess;
				try
				{
					FRows.Add(AKey, AData);
				}
				finally
				{
					FProcess = null;
				}
			}
		}
		
		public void Remove(IServerProcess AProcess, NativeRow AKey)
		{
			lock (this)
			{
				FProcess = AProcess;
				try
				{
					FRows.Remove(AKey);
				}
				finally
				{
					FProcess = null;
				}
			}
		}
	}
*/
}
