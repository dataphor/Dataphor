/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define DEBUG

using System;
using System.Collections;

namespace Alphora.Dataphor
{
	/// <summary> Well performing, in-memory sorted list. </summary>
	/// <remarks> Implementation is a basic B+Tree with two-peer delete merging. 
	/// This structure is not thread-safe. </remarks>
	public class IndexList
	{
		internal const int Fanout = 64;	// Maximum number of items per routing node (must be at least 2)
		internal const int Capacity = 128;	// Maximum number of items per data node
		internal const int InitialDataNodePoolSize = 128;
		internal const int InitialRoutingNodePoolSize = 128;

		public IndexList()
		{
			DataNode newRoot = AcquireDataNode();
			_root = newRoot;
			_head = newRoot;
			_tail = newRoot;
		}

		private Node _root;
		private DataNode _head;
		private DataNode _tail;

		private int _count;
		/// <summary> Total number of entries in the index. </summary>
		public int Count { get { return _count; } }

		#if (DEBUG)

		private int _dataNodeCount;
		/// <summary> Number of data nodes allocated for the entire index. </summary>
		public int DataNodeCount { get { return _dataNodeCount; } }

		private int _routingNodeCount;
		/// <summary> Number of routing nodes allocated for the entire index. </summary>
		public int RoutingNodeCount { get { return _routingNodeCount; } }

		private int _height = 1;
		public int Height { get { return _height; } }

		#endif

		public void Clear()
		{
			_root.Clear(this);
			DataNode newRoot = AcquireDataNode();
			_root = newRoot;
			_head = newRoot;
			_tail = newRoot;
			newRoot._next = null;
			newRoot._prior = null;

			#if (DEBUG)
			_height = 1;
			#endif
		}

		#region Routing and Data Node Pools

		DataNode[] _freeDataNodes = new DataNode[InitialDataNodePoolSize];
		int _freeDataNodeCount = 0;

		private DataNode AcquireDataNode()
		{
			DataNode node;

			#if (DEBUG)
			_dataNodeCount++;
			#endif

			if (_freeDataNodeCount == 0)
				return new DataNode();
			else
			{
				_freeDataNodeCount--;
				node = _freeDataNodes[_freeDataNodeCount];
				node._count = 0;
				return node;
			}
		}

		private void RelinquishDataNode(DataNode node)
		{
			#if (DEBUG)
			_dataNodeCount--;
			#endif

			if (node._next == null)
				_head = node._prior;
			if (node._prior == null)
				_tail = node._next;

			if (_freeDataNodeCount == _freeDataNodes.Length) // we need to grow the array of data nodes
			{
				DataNode[] newValue = new DataNode[_freeDataNodes.Length + (_freeDataNodes.Length / 2)];
				Array.Copy(_freeDataNodes, 0, newValue, 0, _freeDataNodes.Length);
				_freeDataNodes = newValue;
			}
			_freeDataNodes[_freeDataNodeCount] = node;
			_freeDataNodeCount++;
		}

		RoutingNode[] _freeRoutingNodes = new RoutingNode[InitialRoutingNodePoolSize];
		int _freeRoutingNodeCount = 0;

		private RoutingNode AcquireRoutingNode()
		{
			RoutingNode node;

			#if (DEBUG)
			_routingNodeCount++;
			#endif

			if (_freeRoutingNodeCount == 0)
				return new RoutingNode();
			else
			{
				_freeRoutingNodeCount--;
				node = _freeRoutingNodes[_freeRoutingNodeCount];
				node._count = 0;
				return node;
			}
		}

		private void RelinquishRoutingNode(RoutingNode node)
		{
			#if (DEBUG)
			_routingNodeCount--;
			#endif

			if (_freeRoutingNodeCount == _freeRoutingNodes.Length)
			{
				RoutingNode[] newValue = new RoutingNode[_freeRoutingNodes.Length + (_freeRoutingNodes.Length / 2)];
				Array.Copy(_freeRoutingNodes, 0, newValue, 0, _freeRoutingNodes.Length);
				_freeRoutingNodes = newValue;
			}
			_freeRoutingNodes[_freeRoutingNodeCount] = node;
			_freeRoutingNodeCount++;
		}

		#endregion

		#region Insert

		public void Insert(IComparable key, object data)
		{
			for (;;)
			{
				if (!_root.Insert(key, data, this))
				{
					RoutingNode newRoot = AcquireRoutingNode();
					Node newChild = SplitNode(_root);
					newRoot.InsertNode(_root._keys[0], _root, 0);
					newRoot.InsertNode(newChild._keys[0], newChild, 1);
					_root = newRoot;

					#if (DEBUG)
					_height++;
					#endif

					continue;
				}
				break;
			}
			_count++;
		}

		private Node SplitNode(Node child)
		{
			DataNode childDataNode = child as DataNode;
			if (childDataNode != null)
			{
				// Prepare new node
				DataNode newDataNode = AcquireDataNode();
				newDataNode._prior = childDataNode;
				newDataNode._next = childDataNode._next;
				newDataNode.CopyHalf(childDataNode);

				// Update child node
				if (childDataNode._next != null)
					childDataNode._next._prior = newDataNode;
				childDataNode._next = newDataNode;

				return newDataNode;
			}
			else
			{
				RoutingNode newRoutingNode = AcquireRoutingNode();
				newRoutingNode.CopyHalf((RoutingNode)child);
				return newRoutingNode;
			}
		}

		#endregion

		#region Delete

		public bool Delete(IComparable key)
		{
			bool result = _root.Delete(key, this);
			if (result)
			{
				RoutingNode routingRoot = _root as RoutingNode;
				if ((routingRoot != null) && (routingRoot._count == 1))
				{
					// Collapse the tree if there is only one node in the root (routing node)
					_root = routingRoot._nodes[0];
					RelinquishRoutingNode(routingRoot);

					#if (DEBUG)
					_height--;
					#endif
				}
				_count--;
			}
			return result;
		}

		#endregion

		/// <summary> Finds the nearest entry to the given key (in the specified direction). </summary>
		/// <returns> True if an exact match was found. </returns>
		public bool Find(IComparable key, bool forward, out object nearest)
		{
			return _root.Find(key, forward, out nearest);
		}

		internal abstract class Node
		{
			public IComparable[] _keys;
			public int _count;

			/// <summary>
			/// Performs a binary search among the entries in this node for the given key.  Will always return an
			/// entry index in AIndex, which is the index of the entry that was found if the method returns true,
			/// otherwise it is the index where the key should be inserted if the method returns false.
			/// </summary>
			public bool Search(int initialLow, IComparable key, out int index)
			{
				int lo = initialLow;
				int hi = _count - 1;
				int localIndex = 0;
				int result = -1;
				
				while (lo <= hi)
				{
					localIndex = (lo + hi) / 2;
					result = _keys[localIndex].CompareTo(key);
					if (result == 0)
						break;
					else if (result > 0)
						hi = localIndex - 1;
					else 
						lo = localIndex + 1;
				}
				
				if (result == 0)
					index = localIndex;
				else
					index = lo;
					
				return result == 0;
			}

			public abstract bool Find(IComparable key, bool forward, out object nearest);

			public abstract bool Insert(IComparable key, object data, IndexList list);

			public abstract bool Delete(IComparable key, IndexList list);

			/// <summary> Quickly relinquishes all of the nodes (recursively). </summary>
			public abstract void Clear(IndexList list);

			/// <summary> Adds the first ACount nodes from this node to the end of the given node. </summary>
			/// <remarks> No range checking is done. </remarks>
			public abstract void AppendTo(Node node, int count);

			/// <summary> Adds the nodes from this node to the beginning on the given node. </summary>
			public abstract void PrependTo(Node node);
		}

		internal class RoutingNode : Node
		{
			public RoutingNode()
			{
				_keys = new IComparable[Fanout];
				_nodes = new Node[Fanout];
			}

			public Node[] _nodes;

			public override bool Find(IComparable key, bool forward, out object nearest)
			{
				int closestIndex;
				bool match = Search(0, key, out closestIndex);
				if (!match && closestIndex > 0)	// added the guard
					closestIndex--;
				return _nodes[closestIndex].Find(key, forward, out nearest);
			}

			public override bool Insert(IComparable key, object data, IndexList list)
			{
				for (;;)
				{
					int AClosestIndex;
					bool match = Search(0, key, out AClosestIndex);
					if (!match && AClosestIndex > 0)
						AClosestIndex--;
					// Do not throw if there is an exact match.  An exact match in a routing node does not mean that a data node with that key exists (deletes do not update the index nodes)
					Node child = _nodes[AClosestIndex];
					if (!child.Insert(key, data, list))
					{
						// Make sure that there is room to store a new child for the split
						if (_count == Fanout)
							return false;

						// Split the child
						Node newValue = list.SplitNode(child);
						InsertNode(newValue._keys[0], newValue, AClosestIndex + 1);
						continue;	// retry the insert
					}
					return true;
				}
			}

			public void InsertNode(IComparable key, Node node, int index)
			{
				// Slide all entries above the insert index
				Array.Copy(_keys, index, _keys, index + 1, _count - index);
				Array.Copy(_nodes, index, _nodes, index + 1, _count - index);

				// Set the new entry data			
				_keys[index] = key;
				_nodes[index] = node;

				// Increment entry count			
				_count++;
			}

			/// <summary> Initializes this node to contain the upper half of nodes from the given node (and removes them from the given one). </summary>
			public void CopyHalf(RoutingNode node)
			{
				_count = node._count / 2;
				node._count -= _count;
				Array.Copy(_keys, 0, node._keys, node._count, _count);
				Array.Copy(_nodes, 0, node._nodes, node._count, _count);
			}

			public override void PrependTo(Node node)
			{
				RoutingNode target = node as RoutingNode;
				for (int i = 0; i < _count; i++)
					target.InsertNode(_keys[i], _nodes[i], i);
			}

			public override void AppendTo(Node node, int count)
			{
				RoutingNode target = node as RoutingNode;
				for (int i = 0; i < count; i++)
					target.InsertNode(_keys[i], _nodes[i], target._count);
				Array.Copy(_keys, count, _keys, 0, _count - count);
				Array.Copy(_nodes, count, _nodes, 0, _count - count);
				_count -= count;
			}

			public void DeleteNode(int index)
			{
				// Slide all entries above the delete index down over the item to be deleted. 
				Array.Copy(_keys, index + 1, _keys, index, (_count - index) - 1);
				Array.Copy(_nodes, index + 1, _nodes, index, (_count - index) - 1);
				
				// Decrement entry count
				_count--;
			}

			public override bool Delete(IComparable key, IndexList list)
			{
				int AClosestIndex;
				bool match = Search(0, key, out AClosestIndex);
				if (!match && AClosestIndex > 0)
					AClosestIndex--;
				Node child = _nodes[AClosestIndex];
				match = child.Delete(key, list);
				if (match)
				{
					int childCapacity = child._keys.Length;

					// A Delete occurred, check for a possible merge
					if (child._count < (childCapacity / 3))	// if less than a third full
					{
						Node prior = (AClosestIndex == 0 ? null : _nodes[AClosestIndex - 1]);
						Node next = (AClosestIndex == (_count - 1) ? null : _nodes[AClosestIndex + 1]);
						if
						(									// if there is enough room in the adjacent node(s) to handle this node's entries
							(childCapacity * 2)
								- ((prior == null ? 0 : prior._count) + (next == null ? 0 : next._count))
								>= child._count
						)
						{
							// Merge with adjacent nodes
							if (prior != null)
							{
								int needed = (child._count / 2) + (child._count % 2);	// Assume half rounded up
								child.AppendTo
								(
									prior, 
									Math.Min	// Append the lesser of the number of slots available in the prior node, and half of the items to allocate plus the number that the next will not be able to handle of its half
									(
										childCapacity - prior._count, 
										needed + (next == null ? 0 : Math.Max(0, needed - (childCapacity - next._count)))
									)
								);
							}

							if (next != null)
								child.PrependTo(next);

							DeleteNode(AClosestIndex);
							RoutingNode routingChild = child as RoutingNode;
							if (routingChild != null)
								list.RelinquishRoutingNode(routingChild);
							else
								list.RelinquishDataNode((DataNode)child);
						}
					}
				}
				return match;
			}

			public override void Clear(IndexList list)
			{
				for (int i = 0; i < _count; i++)
				{
					Node node = _nodes[i];
					node.Clear(list);
					DataNode dataNode = node as DataNode;
					if (node != null)
						list.RelinquishDataNode(dataNode);
					else
						list.RelinquishRoutingNode((RoutingNode)node);
				}
			}
		}

		internal class DataNode : Node
		{
			public DataNode()
			{
				_keys = new IComparable[Capacity];
				_entries = new object[Capacity];
			}

			public DataNode _next;
			public DataNode _prior;

			public object[] _entries;

			public override bool Find(IComparable key, bool forward, out object nearest)
			{
				int closestIndex;
				bool match = Search(0, key, out closestIndex);
				if (!match)
				{
					if (forward)
					{
						if (closestIndex >= _count)
							if (_next != null)
								return _next.Find(key, forward, out nearest);
							else
							{
								nearest = null;
								return false;
							}
					}
					else
					{
						closestIndex--;
						if (closestIndex < 0)
							if (_prior != null)
								return _prior.Find(key, forward, out nearest);
							else
							{
								nearest = null;
								return false;
							}
					}
				}
				nearest = _entries[closestIndex];
				return match;
			}

			public override bool Insert(IComparable key, object data, IndexList list)
			{
				int AClosestIndex;

				// Check for a split condition before the search.  In the case of a duplicate, this may cause an unnecessary split, but we'll optimize for the non-duplicate insert case.
				if (_count == Capacity)
					return false;

				// Perform the search
				bool match = Search(0, key, out AClosestIndex); // start searching at AIndex = 0
				if (match)
					throw new BaseException(BaseException.Codes.Duplicate, key.ToString());

				// Insert the entry
				InsertEntry(key, data, AClosestIndex);

				return true;
			}

			/// <summary> Initializes this node to contain the upper half of entires from the given node (and removes them from the given one). </summary>
			public void CopyHalf(DataNode node)
			{
				_count = node._count / 2;
				node._count -= _count;
				Array.Copy(_keys, 0, node._keys, node._count, _count);
				Array.Copy(_entries, 0, node._entries, node._count, _count);
			}

			public override void PrependTo(Node node)
			{
				DataNode target = node as DataNode;
				for (int i = 0; i < _count; i++)
					target.InsertEntry(_keys[i], _entries[i], i);
			}

			public override void AppendTo(Node node, int count)
			{
				DataNode target = node as DataNode;
				for (int i = 0; i < count; i++)
					target.InsertEntry(_keys[i], _entries[i], target._count);
				Array.Copy(_keys, count, _keys, 0, _count - count);
				Array.Copy(_entries, count, _entries, 0, _count - count);
				_count -= count;
			}

			public void InsertEntry(IComparable key, object entry, int index)
			{
				// Slide all entries above the insert index
				Array.Copy(_keys, index, _keys, index + 1, _count - index);
				Array.Copy(_entries, index, _entries, index + 1, _count - index);

				// Set the new entry data			
				_keys[index] = key;
				_entries[index] = entry;

				// Increment entry count			
				_count++;
			}

			public override bool Delete(IComparable key, IndexList list)
			{
				int AClosestIndex;
				bool match = Search(0, key, out AClosestIndex);
				if (match)
					DeleteEntry(AClosestIndex);
				return match;
			}

			public void DeleteEntry(int index)
			{
				// Slide all entries above the delete index down over the item to be deleted.
				Array.Copy(_keys, index + 1, _keys, index, (_count - index) - 1);
				Array.Copy(_entries, index + 1, _entries, index, (_count - index) - 1);
				
				// Decrement entry count
				_count--;
			}

			public override void Clear(IndexList list)
			{
				// Nothing
			}
		}
	}
}
