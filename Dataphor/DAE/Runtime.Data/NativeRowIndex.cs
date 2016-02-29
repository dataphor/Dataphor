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
	
	#if USETYPEDLIST
	public class NativeRowIndexList : TypedList
	{
		public NativeRowIndexList() : base(typeof(NativeRowIndex)) {}
		
		public new NativeRowIndex this[int AIndex]
		{
			get { return (NativeRowIndex)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class NativeRowIndexList : BaseList<NativeRowIndex> { }
	#endif
*/
	
	#if USETYPEDLIST
	public class NativeRowTreeList : TypedList
	{
		public NativeRowTreeList() : base(typeof(NativeRowTree)) {}
		
		public new NativeRowTree this[int AIndex]
		{
			get { return (NativeRowTree)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class NativeRowTreeList : BaseList<NativeRowTree>
	{
	#endif
		public int IndexOf(Schema.Order key)
		{
			for (int index = 0; index < Count; index++)
				if (key.Equivalent(this[index].Key))
					return index;
			return -1;
		}
		
		public bool Contains(Schema.Order key)
		{
			return IndexOf(key) >= 0;
		}
		
		public NativeRowTree this[Schema.Order key] { get { return this[IndexOf(key)]; } }
	}
	
	/// <remarks>
	/// Provides a storage structure for the search path followed by the find key in terms of index nodes.
	/// See the description of the FindKey method for the Index class for more information.
	/// </remarks>
	#if USETYPEDLIST
	public class RowTreeSearchPath : DisposableTypedList
	{
		public RowTreeSearchPath() : base(typeof(RowTreeNode), true, false){}
		
		public new RowTreeNode this[int AIndex]
		{
			get { return (RowTreeNode)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	
	#else
	public class RowTreeSearchPath : DisposableList<RowTreeNode>
	{
	#endif
		public RowTreeNode DataNode { get { return this[Count - 1]; } }
		
		#if USETYPEDLIST
		public new RowTreeNode RemoveAt(int AIndex)
		{
			return (RowTreeNode)base.RemoveItemAt(AIndex);
		}
		
		public new RowTreeNode DisownAt(int AIndex)
		{
			return (RowTreeNode)base.DisownAt(AIndex);
		}
		#endif
	}
	
	public class RowTreeNode : Disposable
	{
		public RowTreeNode(IValueManager manager, NativeRowTree tree, NativeRowTreeNode node, LockMode lockMode)
		{
			Manager = manager;
			Tree = tree;
			Node = node;
			DataNode = Node as NativeRowTreeDataNode;
			RoutingNode = Node as NativeRowTreeRoutingNode;
			#if LOCKROWTREE
			Manager.Lock(Node.LockID, ALockMode);
			#endif
		}
		
		public IValueManager Manager;
		public NativeRowTree Tree;
		
		public NativeRowTreeNode Node;
		public NativeRowTreeDataNode DataNode;
		public NativeRowTreeRoutingNode RoutingNode;
		
		protected override void Dispose(bool disposing)
		{
			if (Manager != null)
			{
				#if LOCKROWTREE
				Manager.Unlock(Node.LockID);
				#endif
				Manager = null;
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Performs a binary search among the entries in this node for the given key.  Will always return an
		/// entry index in AEntryNumber, which is the index of the entry that was found if the method returns true,
		/// otherwise it is the index where the key should be inserted if the method returns false.
		/// </summary>
		public bool NodeSearch(Schema.IRowType keyRowType, NativeRow key, out int entryNumber)
		{
			int lo = (Node.NodeType == NativeRowTreeNodeType.Routing ? 1 : 0);
			int hi = Node.EntryCount - 1;
			int index = 0;
			int result = -1;
			
			while (lo <= hi)
			{
				index = (lo + hi) / 2;
				result = Tree.Compare(Manager, Tree.KeyRowType, Node.Keys[index], keyRowType, key);
				if (result == 0)
					break;
				else if (result > 0)
					hi = index - 1;
				else // if (LResult < 0) unnecessary
					lo = index + 1;
			}
			
			if (result == 0)
				entryNumber = index;
			else
				entryNumber = lo;
				
			return result == 0;
		}

		/// <summary>
		/// The recursive portion of the find key algorithm invoked by the FindKey method of the parent Index.
		/// </summary>
		public bool FindKey(Schema.IRowType keyRowType, NativeRow key, RowTreeSearchPath rowTreeSearchPath, out int entryNumber)
		{
			rowTreeSearchPath.Add(this);
			if (Node.NodeType == NativeRowTreeNodeType.Routing)
			{
				// Perform a binary search among the keys in this node to determine which streamid to follow for the next node
				bool result = NodeSearch(keyRowType, key, out entryNumber);

				// If the key was found, use the given entry number, otherwise, use the one before the given entry
				entryNumber = result ? entryNumber : (entryNumber - 1);
				return
					new RowTreeNode
					(
						Manager,
						Tree, 
						RoutingNode.Nodes[entryNumber],
						LockMode.Shared
					).FindKey
					(
						keyRowType,
						key, 
						rowTreeSearchPath, 
						out entryNumber
					);
			}
			else
			{
				// Perform a binary search among the keys in this node to determine which entry, if any, is equal to the given key
				return NodeSearch(keyRowType, key, out entryNumber);
			}
		}
		
		/// <summary>Inserts the given Key and Data streams into this node at the given index.</summary>
		public void InsertData(NativeRow key, NativeRow data, int entryNumber)
		{
			DataNode.Insert(key, data, entryNumber);
			
			Tree.RowsMoved(Node, entryNumber, Node.EntryCount - 2, Node, 1);
		}
		
		public void InsertRouting(NativeRow key, NativeRowTreeNode node, int entryNumber)
		{
			RoutingNode.Insert(key, node, entryNumber);
			
			Tree.RowsMoved(Node, entryNumber, Node.EntryCount - 2, Node, 1);
		}
		
		public void UpdateData(NativeRow data, int entryNumber)
		{
			Tree.DisposeData(Manager, DataNode.Rows[entryNumber]);
			DataNode.Rows[entryNumber] = data;
		}
		
		public void UpdateRouting(NativeRowTreeNode node, int entryNumber)
		{
			RoutingNode.Nodes[entryNumber] = node;
		}
		
		public void DeleteData(int entryNumber)
		{
			Tree.DisposeData(Manager, DataNode.Rows[entryNumber]);
			Tree.DisposeKey(Manager, DataNode.Keys[entryNumber]);
			
			DataNode.Delete(entryNumber);
			
			Tree.RowDeleted(Node, entryNumber);
			Tree.RowsMoved(Node, entryNumber + 1, Node.EntryCount, Node, -1);
		}
		
		public void DeleteRouting(int entryNumber)
		{
			Tree.DisposeKey(Manager, RoutingNode.Keys[entryNumber]);
			
			RoutingNode.Delete(entryNumber);
			
			Tree.RowDeleted(Node, entryNumber);
			Tree.RowsMoved(Node, entryNumber + 1, Node.EntryCount, Node, -1);
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
			Schema.Order key,
			Schema.RowType keyRowType,
			Schema.RowType dataRowType,
			int fanout,
			int capacity,
			bool isClustered
		) : base()
		{
			_key = key;
			#if DEBUG
			for (int index = 0; index < _key.Columns.Count; index++)
				if (_key.Columns[index].Sort == null)
					Error.Fail("Sort is null");
			#endif
			
			_keyRowType = keyRowType;
			_dataRowType = dataRowType;
			_fanout = fanout < MinimumFanout ? MinimumFanout : fanout;
			_capacity = capacity;
			IsClustered = isClustered;
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
		
		private int _fanout;		
		/// <summary>The number of entries per routing node in the tree.</summary>		
		public int Fanout { get { return _fanout; } }

		private int _capacity;
		/// <summary>The number of entries per data node in the tree.</summary>		
		public int Capacity { get { return _capacity; } }
		
		private Schema.Order _key;
		/// <summary>The description of the order for the index.</summary>
		public Schema.Order Key { get { return _key; } }

		private Schema.RowType _keyRowType;
		/// <summary>The row type of the key for the index.</summary>
		public Schema.RowType KeyRowType { get { return _keyRowType; } }
		
		private Schema.RowType _dataRowType;
		/// <summary>The row type for data for the index.</summary>
		public Schema.RowType DataRowType { get { return _dataRowType; } }
		
		public bool IsClustered;

		public void Drop(IValueManager manager)
		{
			// Deallocate all nodes in the tree
			DeallocateNode(manager, Root);
			Root = null;
			Tail = null;
			Head = null;
			Height = 0;
		}
		
		protected RowTreeNode AllocateNode(IValueManager manager, NativeRowTreeNodeType nodeType)
		{
			return new RowTreeNode(manager, this, nodeType == NativeRowTreeNodeType.Routing ? (NativeRowTreeNode)new NativeRowTreeRoutingNode(this) : new NativeRowTreeDataNode(this), LockMode.Exclusive);
		}
		
		protected void DeallocateNode(IValueManager manager, NativeRowTreeNode node)
		{
			using (RowTreeNode localNode = new RowTreeNode(manager, this, node, LockMode.Exclusive))
			{
				NativeRowTreeDataNode dataNode = node as NativeRowTreeDataNode;
				NativeRowTreeRoutingNode routingNode = node as NativeRowTreeRoutingNode;
				for (int entryIndex = 0; entryIndex < node.EntryCount; entryIndex++)
				{
					if (node.NodeType == NativeRowTreeNodeType.Routing)
					{
						if (entryIndex > 0)
							DisposeKey(manager, node.Keys[entryIndex]);
						DeallocateNode(manager, routingNode.Nodes[entryIndex]);
					}
					else
					{
						DisposeKey(manager, dataNode.Keys[entryIndex]);
						DisposeData(manager, dataNode.Rows[entryIndex]);
					}
				}
				
				if (node.NextNode == null)
					Tail = node.PriorNode;
				else
				{
					using (RowTreeNode nextNode = new RowTreeNode(manager, this, node.NextNode, LockMode.Exclusive))
					{
						nextNode.Node.PriorNode = node.PriorNode;
					}
				}
					
				if (node.PriorNode == null)
					Head = node.NextNode;
				else
				{
					using (RowTreeNode priorNode = new RowTreeNode(manager, this, node.PriorNode, LockMode.Exclusive))
					{
						priorNode.Node.NextNode = node.NextNode;
					}
				}
			}
		}
		
		/// <summary>
		/// The given streams are copied into the index, so references within the streams 
		/// are considered owned by the index after the insert.
		/// </summary>
		public void Insert(IValueManager manager, NativeRow key, NativeRow data)
		{
			int entryNumber;
			using (RowTreeSearchPath rowTreeSearchPath = new RowTreeSearchPath())
			{
				bool result = FindKey(manager, KeyRowType, key, rowTreeSearchPath, out entryNumber);
				if (result)
					throw new IndexException(IndexException.Codes.DuplicateKey);
					
				InternalInsert(manager, rowTreeSearchPath, entryNumber, key, data);
			}
		}
		
		private int Split(NativeRowTreeNode sourceNode, NativeRowTreeNode targetNode)
		{
			int entryCount = sourceNode.EntryCount;
			int entryPivot = entryCount / 2;
			if (sourceNode.NodeType == NativeRowTreeNodeType.Data)
			{
				NativeRowTreeDataNode sourceDataNode = (NativeRowTreeDataNode)sourceNode;
				NativeRowTreeDataNode targetDataNode = (NativeRowTreeDataNode)targetNode;

				// Insert the upper half of the entries from ASourceNode into ATargetNode
				for (int entryIndex = entryPivot; entryIndex < entryCount; entryIndex++)
					targetDataNode.Insert(sourceDataNode.Keys[entryIndex], sourceDataNode.Rows[entryIndex], entryIndex - entryPivot);

				// Remove the upper half of the entries from ASourceNode					
				for (int entryIndex = entryCount - 1; entryIndex >= entryPivot; entryIndex--)
					sourceDataNode.Delete(entryIndex); // Don't dispose the values here, this is a move
			}
			else
			{
				NativeRowTreeRoutingNode sourceRoutingNode = (NativeRowTreeRoutingNode)sourceNode;
				NativeRowTreeRoutingNode targetRoutingNode = (NativeRowTreeRoutingNode)targetNode;
	
				// Insert the upper half of the entries from ASourceNode into ATargetNode
				for (int entryIndex = entryPivot; entryIndex < entryCount; entryIndex++)
					targetRoutingNode.Insert(sourceRoutingNode.Keys[entryIndex], sourceRoutingNode.Nodes[entryIndex], entryIndex - entryPivot);
					
				// Remove the upper half of the entries from ASourceNode					
				for (int entryIndex = entryCount - 1; entryIndex >= entryPivot; entryIndex--)
					sourceRoutingNode.Delete(entryIndex);
			}

			// Notify index clients of the data change
			RowsMoved(sourceNode, entryPivot, entryCount - 1, targetNode, -entryPivot);
			
			return entryPivot;
		}
		
		private void InternalInsert(IValueManager manager, RowTreeSearchPath rowTreeSearchPath, int entryNumber, NativeRow key, NativeRow data)
		{
			// Walk back up the search path, inserting data and splitting pages as necessary
			RowTreeNode newRowTreeNode;
			NativeRowTreeNode splitNode = null;
			for (int index = rowTreeSearchPath.Count - 1; index >= 0; index--)
			{
				if (rowTreeSearchPath[index].Node.EntryCount >= Capacity)
				{
					// Allocate a new node
					using (newRowTreeNode = AllocateNode(manager, rowTreeSearchPath[index].Node.NodeType))
					{
						// Thread it into the list of leaves, if necessary
						if (newRowTreeNode.Node.NodeType == NativeRowTreeNodeType.Data)
						{
							newRowTreeNode.Node.PriorNode = rowTreeSearchPath[index].Node;
							newRowTreeNode.Node.NextNode = rowTreeSearchPath[index].Node.NextNode;
							rowTreeSearchPath[index].Node.NextNode = newRowTreeNode.Node;
							if (newRowTreeNode.Node.NextNode == null)
								Tail = newRowTreeNode.Node;
							else
							{
								using (RowTreeNode nextRowTreeNode = new RowTreeNode(manager, this, newRowTreeNode.Node.NextNode, LockMode.Exclusive))
								{
									nextRowTreeNode.Node.PriorNode = newRowTreeNode.Node;
								}
							}
						}
						
						int entryPivot = Split(rowTreeSearchPath[index].Node, newRowTreeNode.Node);
						
						// Insert the new entry into the appropriate node
						if (entryNumber >= entryPivot)
							if (newRowTreeNode.Node.NodeType == NativeRowTreeNodeType.Data)
								newRowTreeNode.InsertData(key, data, entryNumber - entryPivot);
							else
								newRowTreeNode.InsertRouting(key, splitNode, entryNumber - entryPivot);
						else
							if (newRowTreeNode.Node.NodeType == NativeRowTreeNodeType.Data)
								rowTreeSearchPath[index].InsertData(key, data, entryNumber);
							else
								rowTreeSearchPath[index].InsertRouting(key, splitNode, entryNumber);
							
						// Reset the AKey for the next round
						// The key for the entry one level up is the first key for the newly allocated node
						key = CopyKey(manager, newRowTreeNode.Node.Keys[0]);
						
						// Set LSplitNode to the newly allocated node
						splitNode = newRowTreeNode.Node;
					}

					if (index == 0)
					{
						// Allocate a new root node and grow the height of the tree by 1
						using (newRowTreeNode = AllocateNode(manager, NativeRowTreeNodeType.Routing))
						{
							newRowTreeNode.InsertRouting(null, rowTreeSearchPath[index].Node, 0); // 1st key of a routing node is not used
							newRowTreeNode.InsertRouting(key, splitNode, 1);
							Root = newRowTreeNode.Node;
							Height++;
						}
					}
					else
					{
						// reset AEntryNumber for the next round
						bool result = rowTreeSearchPath[index - 1].NodeSearch(KeyRowType, key, out entryNumber);

						// At this point we should be guaranteed to have a routing key which does not exist in the parent node
						if (result)
							throw new IndexException(IndexException.Codes.DuplicateRoutingKey);
					}
				}
				else
				{
					if (rowTreeSearchPath[index].Node.NodeType == NativeRowTreeNodeType.Data)
						rowTreeSearchPath[index].InsertData(key, data, entryNumber);
					else
						rowTreeSearchPath[index].InsertRouting(key, splitNode, entryNumber);
					break;
				}
			}
		}
		
		/// <summary>Updates the entry given by AOldKey to the stream given by ANewKey.  The data for the entry is moved to the new location.</summary>
		public void Update(IValueManager manager, NativeRow oldKey, NativeRow newKey)
		{
			Update(manager, oldKey, newKey, null);
		}
		
		/// <summary>Updates the entry given by AOldKey to the entry given by ANewKey and ANewData.  If AOldKey == ANewKey, the data for the entry is updated in place, otherwise it is moved to the location given by ANewKey.</summary>
		public void Update(IValueManager manager, NativeRow oldKey, NativeRow newKey, NativeRow newData)
		{
			int entryNumber;
			using (RowTreeSearchPath rowTreeSearchPath = new RowTreeSearchPath())
			{
				bool result = FindKey(manager, KeyRowType, oldKey, rowTreeSearchPath, out entryNumber);
				if (!result)
					throw new IndexException(IndexException.Codes.KeyNotFound);
					
				if (Compare(manager, KeyRowType, oldKey, KeyRowType, newKey) == 0)
				{
					if (newData != null)
						rowTreeSearchPath.DataNode.UpdateData(newData, entryNumber);
				}
				else
				{
					if (newData == null)
					{
						newData = rowTreeSearchPath.DataNode.DataNode.Rows[entryNumber];
						rowTreeSearchPath.DataNode.DataNode.Delete(entryNumber); // Don't dispose here this is a move
					}
					else
						InternalDelete(manager, rowTreeSearchPath, entryNumber); // Dispose here this is not a move

					rowTreeSearchPath.Dispose();
					result = FindKey(manager, KeyRowType, newKey, rowTreeSearchPath, out entryNumber);
					if (result)
						throw new IndexException(IndexException.Codes.DuplicateKey);
						
					InternalInsert(manager, rowTreeSearchPath, entryNumber, newKey, newData);
				}
			}
		}
		
		private void InternalDelete(IValueManager manager, RowTreeSearchPath rowTreeSearchPath, int entryNumber)
		{
			rowTreeSearchPath.DataNode.DeleteData(entryNumber);
		}
		
		// TODO: Asynchronous collapsed node recovery
		/// <summary>Deletes the entry given by AKey.  The streams are disposed through the DisposeKey event, so it is the responsibility of the index user to dispose references within the streams.</summary>
		public void Delete(IValueManager manager, NativeRow key)
		{
			int entryNumber;
			using (RowTreeSearchPath rowTreeSearchPath = new RowTreeSearchPath())
			{
				bool result = FindKey(manager, KeyRowType, key, rowTreeSearchPath, out entryNumber);
				if (!result)
					throw new IndexException(IndexException.Codes.KeyNotFound);
					
				InternalDelete(manager, rowTreeSearchPath, entryNumber);
			}
		}

		/// <summary>
		/// Searches for the given key within the index.  ARowTreeSearchPath and AEntryNumber together give the 
		/// location of the key in the index.  If the search is successful, the entry exists, otherwise 
		/// the EntryNumber indicates where the entry should be placed for an insert.
		/// </summary>
		/// <param name="key">The key to be found.</param>
		/// <param name="rowTreeSearchPath">A <see cref="RowTreeSearchPath"/> which will contain the set of nodes along the search path to the key.</param>
		/// <param name="entryNumber">The EntryNumber where the key either is, or should be, depending on the result of the find.</param>
		/// <returns>A boolean value indicating the success or failure of the find.</returns>
		public bool FindKey(IValueManager manager, Schema.IRowType keyRowType, NativeRow key, RowTreeSearchPath rowTreeSearchPath, out int entryNumber)
		{
			return new RowTreeNode(manager, this, Root, LockMode.Shared).FindKey(keyRowType, key, rowTreeSearchPath, out entryNumber);
		}
		
		public int Compare(IValueManager manager, Schema.IRowType indexKeyRowType, NativeRow indexKey, Schema.IRowType compareKeyRowType, NativeRow compareKey)
		{
			// If AIndexKeyRowType is null, the index key must have the structure of an index key,
			// Otherwise, the IndexKey row could be a subset of the actual index key.
			// In that case, AIndexKeyRowType is the RowType for the IndexKey row.
			// It is the caller's responsibility to ensure that the passed IndexKey RowType 
			// is a subset of the actual IndexKey with order intact.
			//Row LIndexKey = new Row(AManager, AIndexKeyRowType, AIndexKey);
				
			// If ACompareContext is null, the compare key must have the structure of an index key,
			// Otherwise the CompareKey could be a subset of the actual index key.
			// In that case, ACompareContext is the RowType for the CompareKey row.
			// It is the caller's responsibility to ensure that the passed CompareKey RowType 
			// is a subset of the IndexKey with order intact.
			//Row LCompareKey = new Row(AManager, ACompareKeyRowType, ACompareKey);
				
			int result = 0;
			for (int index = 0; index < indexKeyRowType.Columns.Count; index++)
			{
				if (index >= compareKeyRowType.Columns.Count)
					break;
					
				if ((indexKey.Values[index] != null) && (compareKey.Values[index] != null))
				{
					if (indexKeyRowType.Columns[index].DataType is Schema.ScalarType)
						result = manager.EvaluateSort(Key.Columns[index], indexKey.Values[index], compareKey.Values[index]);
					else
					{
						using (var indexValue = DataValue.FromNative(manager, indexKey.DataTypes[index], indexKey.Values[index]))
						{
							using (var compareValue = DataValue.FromNative(manager, compareKey.DataTypes[index], compareKey.Values[index]))
							{
								result = manager.EvaluateSort(Key.Columns[index], indexValue, compareValue);
							}
						}
					}
				}
				else if (indexKey.Values[index] != null)
				{
					result = Key.Columns[index].Ascending ? 1 : -1;
				}
				else if (compareKey.Values[index] != null)
				{
					result = Key.Columns[index].Ascending ? -1 : 1;
				}
				else
				{
					result = 0;
				}
				
				if (result != 0)
					break;
			}
			
			//LIndexKey.Dispose();
			//LCompareKey.Dispose();
			return result;
		}
		
		public NativeRow CopyKey(IValueManager manager, NativeRow sourceKey)
		{
			return (NativeRow)DataValue.CopyNative(manager, KeyRowType, sourceKey);
		}
		
		public NativeRow CopyData(IValueManager manager, NativeRow sourceData)
		{
			return (NativeRow)DataValue.CopyNative(manager, DataRowType, sourceData);
		}
		
		public void DisposeKey(IValueManager manager, NativeRow key)
		{
			DataValue.DisposeNative(manager, KeyRowType, key);
		}
		
		public void DisposeData(IValueManager manager, NativeRow data)
		{
			DataValue.DisposeNative(manager, DataRowType, data);
		}

		public event NativeRowTreeRowsMovedHandler OnRowsMoved;
		public void RowsMoved(NativeRowTreeNode oldNode, int oldEntryNumberMin, int oldEntryNumberMax, NativeRowTreeNode newNode, int entryNumberDelta)
		{
			if (OnRowsMoved != null)
				OnRowsMoved(this, oldNode, oldEntryNumberMin, oldEntryNumberMax, newNode, entryNumberDelta);
		}
		
		public event NativeRowTreeRowDeletedHandler OnRowDeleted;
		public void RowDeleted(NativeRowTreeNode node, int entryNumber)
		{
			if (OnRowDeleted != null)
				OnRowDeleted(this, node, entryNumber);
		}
	}

	public enum NativeRowTreeNodeType {Routing, Data}
	
	public class NativeRowTreeNode : System.Object
	{
		public NativeRowTreeNode(NativeRowTree nativeRowTree)
		{
			_nativeRowTree = nativeRowTree;
			#if LOCKROWTREE
			LockID = new LockID(Server.Server.CStreamManagerID, GetHashCode().ToString());
			#endif
		}
		
		protected NativeRowTree _nativeRowTree;
		public NativeRowTree NativeRowTree { get { return _nativeRowTree; } }
		
		protected NativeRowTreeNodeType _nodeType;
		public NativeRowTreeNodeType NodeType { get { return _nodeType; } }
		
		protected NativeRow[] _keys;
		public NativeRow[] Keys { get { return _keys; } }
		
		protected int _entryCount = 0;
		public int EntryCount { get { return _entryCount; } }
		
		public NativeRowTreeNode PriorNode;

		public NativeRowTreeNode NextNode;
		
		#if LOCKROWTREE
		public LockID LockID;
		#endif
	}
	
	public class NativeRowTreeRoutingNode : NativeRowTreeNode
	{
		public NativeRowTreeRoutingNode(NativeRowTree nativeRowTree) : base(nativeRowTree)
		{
			_nodeType = NativeRowTreeNodeType.Routing;
			_keys = new NativeRow[_nativeRowTree.Fanout];
			_nodes = new NativeRowTreeNode[_nativeRowTree.Fanout];
		}
		
		private NativeRowTreeNode[] _nodes;
		public NativeRowTreeNode[] Nodes { get { return _nodes; } }
		
		public void Insert(NativeRow key, NativeRowTreeNode node, int entryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(_keys, entryNumber, _keys, entryNumber + 1, _entryCount - entryNumber);
			Array.Copy(_nodes, entryNumber, _nodes, entryNumber + 1, _entryCount - entryNumber);

			// Set the new entry data			
			_keys[entryNumber] = key;
			_nodes[entryNumber] = node;

			// Increment entry count			
			_entryCount++;
		}
		
		public void Delete(int entryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(_keys, entryNumber + 1, _keys, entryNumber, _entryCount - entryNumber - 1);
			Array.Copy(_nodes, entryNumber + 1, _nodes, entryNumber, _entryCount - entryNumber - 1);
			
			// Decrement EntryCount
			_entryCount--;
		}
	}
	
	public class NativeRowTreeDataNode : NativeRowTreeNode
	{
		public NativeRowTreeDataNode(NativeRowTree nativeRowTree) : base(nativeRowTree)
		{
			_nodeType = NativeRowTreeNodeType.Data;
			_keys = new NativeRow[_nativeRowTree.Capacity];
			_rows = new NativeRow[_nativeRowTree.Capacity];
		}
		
		private NativeRow[] _rows;
		public NativeRow[] Rows { get { return _rows; } }

		public void Insert(NativeRow key, NativeRow row, int entryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(_keys, entryNumber, _keys, entryNumber + 1, _entryCount - entryNumber);
			Array.Copy(_rows, entryNumber, _rows, entryNumber + 1, _entryCount - entryNumber);

			// Set the new entry data			
			_keys[entryNumber] = key;
			_rows[entryNumber] = row;

			// Increment entry count			
			_entryCount++;
		}
		
		public void Delete(int entryNumber)
		{
			// Slide all entries above the insert index
			Array.Copy(_keys, entryNumber + 1, _keys, entryNumber, _entryCount - entryNumber - 1);
			Array.Copy(_rows, entryNumber + 1, _rows, entryNumber, _entryCount - entryNumber - 1);
			
			// Decrement EntryCount
			_entryCount--;
		}
	}
	
/*
	Incomplete native row hashtable implementation:

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
			FRows = new Dictionary<NativeRow, NativeRow>(FHashCodeProvider, FComparer);
		}
		
		private Dictionary<NativeRow, NativeRow> FRows;
		
		internal IManager FManager; // Only used during the Add and Remove calls
		
		private NativeRowHashTableHashCodeProvider FHashCodeProvider;
		
		private NativeRowHashTableComparer FComparer;
		
		public void Add(IIValueManager AManager, NativeRow AKey, NativeRow AData)
		{
			lock (this)
			{
				FManager = AManager;
				try
				{
					FRows.Add(AKey, AData);
				}
				finally
				{
					FManager = null;
				}
			}
		}
		
		public void Remove(IIValueManager AManager, NativeRow AKey)
		{
			lock (this)
			{
				FManager = AManager;
				try
				{
					FRows.Remove(AKey);
				}
				finally
				{
					FManager = null;
				}
			}
		}
	}
*/
}
