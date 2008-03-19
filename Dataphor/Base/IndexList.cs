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
		internal const int CFanout = 64;	// Maximum number of items per routing node (must be at least 2)
		internal const int CCapacity = 128;	// Maximum number of items per data node
		internal const int CInitialDataNodePoolSize = 128;
		internal const int CInitialRoutingNodePoolSize = 128;

		public IndexList()
		{
			DataNode LNewRoot = AcquireDataNode();
			FRoot = LNewRoot;
			FHead = LNewRoot;
			FTail = LNewRoot;
		}

		private Node FRoot;
		private DataNode FHead;
		private DataNode FTail;

		private int FCount;
		/// <summary> Total number of entries in the index. </summary>
		public int Count { get { return FCount; } }

		#if (DEBUG)

		private int FDataNodeCount;
		/// <summary> Number of data nodes allocated for the entire index. </summary>
		public int DataNodeCount { get { return FDataNodeCount; } }

		private int FRoutingNodeCount;
		/// <summary> Number of routing nodes allocated for the entire index. </summary>
		public int RoutingNodeCount { get { return FRoutingNodeCount; } }

		private int FHeight = 1;
		public int Height { get { return FHeight; } }

		#endif

		public void Clear()
		{
			FRoot.Clear(this);
			DataNode LNewRoot = AcquireDataNode();
			FRoot = LNewRoot;
			FHead = LNewRoot;
			FTail = LNewRoot;
			LNewRoot.FNext = null;
			LNewRoot.FPrior = null;

			#if (DEBUG)
			FHeight = 1;
			#endif
		}

		#region Routing and Data Node Pools

		DataNode[] FFreeDataNodes = new DataNode[CInitialDataNodePoolSize];
		int FFreeDataNodeCount = 0;

		private DataNode AcquireDataNode()
		{
			DataNode LNode;

			#if (DEBUG)
			FDataNodeCount++;
			#endif

			if (FFreeDataNodeCount == 0)
				return new DataNode();
			else
			{
				FFreeDataNodeCount--;
				LNode = FFreeDataNodes[FFreeDataNodeCount];
				LNode.FCount = 0;
				return LNode;
			}
		}

		private void RelinquishDataNode(DataNode ANode)
		{
			#if (DEBUG)
			FDataNodeCount--;
			#endif

			if (ANode.FNext == null)
				FHead = ANode.FPrior;
			if (ANode.FPrior == null)
				FTail = ANode.FNext;

			if (FFreeDataNodeCount == FFreeDataNodes.Length) // we need to grow the array of data nodes
			{
				DataNode[] LNew = new DataNode[FFreeDataNodes.Length + (FFreeDataNodes.Length / 2)];
				Array.Copy(FFreeDataNodes, 0, LNew, 0, FFreeDataNodes.Length);
				FFreeDataNodes = LNew;
			}
			FFreeDataNodes[FFreeDataNodeCount] = ANode;
			FFreeDataNodeCount++;
		}

		RoutingNode[] FFreeRoutingNodes = new RoutingNode[CInitialRoutingNodePoolSize];
		int FFreeRoutingNodeCount = 0;

		private RoutingNode AcquireRoutingNode()
		{
			RoutingNode LNode;

			#if (DEBUG)
			FRoutingNodeCount++;
			#endif

			if (FFreeRoutingNodeCount == 0)
				return new RoutingNode();
			else
			{
				FFreeRoutingNodeCount--;
				LNode = FFreeRoutingNodes[FFreeRoutingNodeCount];
				LNode.FCount = 0;
				return LNode;
			}
		}

		private void RelinquishRoutingNode(RoutingNode ANode)
		{
			#if (DEBUG)
			FRoutingNodeCount--;
			#endif

			if (FFreeRoutingNodeCount == FFreeRoutingNodes.Length)
			{
				RoutingNode[] LNew = new RoutingNode[FFreeRoutingNodes.Length + (FFreeRoutingNodes.Length / 2)];
				Array.Copy(FFreeRoutingNodes, 0, LNew, 0, FFreeRoutingNodes.Length);
				FFreeRoutingNodes = LNew;
			}
			FFreeRoutingNodes[FFreeRoutingNodeCount] = ANode;
			FFreeRoutingNodeCount++;
		}

		#endregion

		#region Insert

		public void Insert(IComparable AKey, object AData)
		{
			for (;;)
			{
				if (!FRoot.Insert(AKey, AData, this))
				{
					RoutingNode LNewRoot = AcquireRoutingNode();
					Node LNewChild = SplitNode(FRoot);
					LNewRoot.InsertNode(FRoot.FKeys[0], FRoot, 0);
					LNewRoot.InsertNode(LNewChild.FKeys[0], LNewChild, 1);
					FRoot = LNewRoot;

					#if (DEBUG)
					FHeight++;
					#endif

					continue;
				}
				break;
			}
			FCount++;
		}

		private Node SplitNode(Node AChild)
		{
			DataNode LChildDataNode = AChild as DataNode;
			if (LChildDataNode != null)
			{
				// Prepare new node
				DataNode LNewDataNode = AcquireDataNode();
				LNewDataNode.FPrior = LChildDataNode;
				LNewDataNode.FNext = LChildDataNode.FNext;
				LNewDataNode.CopyHalf(LChildDataNode);

				// Update child node
				if (LChildDataNode.FNext != null)
					LChildDataNode.FNext.FPrior = LNewDataNode;
				LChildDataNode.FNext = LNewDataNode;

				return LNewDataNode;
			}
			else
			{
				RoutingNode LNewRoutingNode = AcquireRoutingNode();
				LNewRoutingNode.CopyHalf((RoutingNode)AChild);
				return LNewRoutingNode;
			}
		}

		#endregion

		#region Delete

		public bool Delete(IComparable AKey)
		{
			bool LResult = FRoot.Delete(AKey, this);
			if (LResult)
			{
				RoutingNode LRoutingRoot = FRoot as RoutingNode;
				if ((LRoutingRoot != null) && (LRoutingRoot.FCount == 1))
				{
					// Collapse the tree if there is only one node in the root (routing node)
					FRoot = LRoutingRoot.FNodes[0];
					RelinquishRoutingNode(LRoutingRoot);

					#if (DEBUG)
					FHeight--;
					#endif
				}
				FCount--;
			}
			return LResult;
		}

		#endregion

		/// <summary> Finds the nearest entry to the given key (in the specified direction). </summary>
		/// <returns> True if an exact match was found. </returns>
		public bool Find(IComparable AKey, bool AForward, out object ANearest)
		{
			return FRoot.Find(AKey, AForward, out ANearest);
		}

		internal abstract class Node
		{
			public IComparable[] FKeys;
			public int FCount;

			/// <summary>
			/// Performs a binary search among the entries in this node for the given key.  Will always return an
			/// entry index in AIndex, which is the index of the entry that was found if the method returns true,
			/// otherwise it is the index where the key should be inserted if the method returns false.
			/// </summary>
			public bool Search(int AInitialLow, IComparable AKey, out int AIndex)
			{
				int LLo = AInitialLow;
				int LHi = FCount - 1;
				int LIndex = 0;
				int LResult = -1;
				
				while (LLo <= LHi)
				{
					LIndex = (LLo + LHi) / 2;
					LResult = FKeys[LIndex].CompareTo(AKey);
					if (LResult == 0)
						break;
					else if (LResult > 0)
						LHi = LIndex - 1;
					else 
						LLo = LIndex + 1;
				}
				
				if (LResult == 0)
					AIndex = LIndex;
				else
					AIndex = LLo;
					
				return LResult == 0;
			}

			public abstract bool Find(IComparable AKey, bool AForward, out object ANearest);

			public abstract bool Insert(IComparable AKey, object AData, IndexList AList);

			public abstract bool Delete(IComparable AKey, IndexList AList);

			/// <summary> Quickly relinquishes all of the nodes (recursively). </summary>
			public abstract void Clear(IndexList AList);

			/// <summary> Adds the first ACount nodes from this node to the end of the given node. </summary>
			/// <remarks> No range checking is done. </remarks>
			public abstract void AppendTo(Node ANode, int ACount);

			/// <summary> Adds the nodes from this node to the beginning on the given node. </summary>
			public abstract void PrependTo(Node ANode);
		}

		internal class RoutingNode : Node
		{
			public RoutingNode()
			{
				FKeys = new IComparable[CFanout];
				FNodes = new Node[CFanout];
			}

			public Node[] FNodes;

			public override bool Find(IComparable AKey, bool AForward, out object ANearest)
			{
				int LClosestIndex;
				bool LMatch = Search(0, AKey, out LClosestIndex);
				if (!LMatch && LClosestIndex > 0)	// added the guard
					LClosestIndex--;
				return FNodes[LClosestIndex].Find(AKey, AForward, out ANearest);
			}

			public override bool Insert(IComparable AKey, object AData, IndexList AList)
			{
				for (;;)
				{
					int AClosestIndex;
					bool LMatch = Search(0, AKey, out AClosestIndex);
					if (!LMatch && AClosestIndex > 0)
						AClosestIndex--;
					// Do not throw if there is an exact match.  An exact match in a routing node does not mean that a data node with that key exists (deletes do not update the index nodes)
					Node LChild = FNodes[AClosestIndex];
					if (!LChild.Insert(AKey, AData, AList))
					{
						// Make sure that there is room to store a new child for the split
						if (FCount == CFanout)
							return false;

						// Split the child
						Node LNew = AList.SplitNode(LChild);
						InsertNode(LNew.FKeys[0], LNew, AClosestIndex + 1);
						continue;	// retry the insert
					}
					return true;
				}
			}

			public void InsertNode(IComparable AKey, Node ANode, int AIndex)
			{
				// Slide all entries above the insert index
				Array.Copy(FKeys, AIndex, FKeys, AIndex + 1, FCount - AIndex);
				Array.Copy(FNodes, AIndex, FNodes, AIndex + 1, FCount - AIndex);

				// Set the new entry data			
				FKeys[AIndex] = AKey;
				FNodes[AIndex] = ANode;

				// Increment entry count			
				FCount++;
			}

			/// <summary> Initializes this node to contain the upper half of nodes from the given node (and removes them from the given one). </summary>
			public void CopyHalf(RoutingNode ANode)
			{
				FCount = ANode.FCount / 2;
				ANode.FCount -= FCount;
				Array.Copy(FKeys, 0, ANode.FKeys, ANode.FCount, FCount);
				Array.Copy(FNodes, 0, ANode.FNodes, ANode.FCount, FCount);
			}

			public override void PrependTo(Node ANode)
			{
				RoutingNode LTarget = ANode as RoutingNode;
				for (int i = 0; i < FCount; i++)
					LTarget.InsertNode(FKeys[i], FNodes[i], i);
			}

			public override void AppendTo(Node ANode, int ACount)
			{
				RoutingNode LTarget = ANode as RoutingNode;
				for (int i = 0; i < ACount; i++)
					LTarget.InsertNode(FKeys[i], FNodes[i], LTarget.FCount);
				Array.Copy(FKeys, ACount, FKeys, 0, FCount - ACount);
				Array.Copy(FNodes, ACount, FNodes, 0, FCount - ACount);
				FCount -= ACount;
			}

			public void DeleteNode(int AIndex)
			{
				// Slide all entries above the delete index down over the item to be deleted. 
				Array.Copy(FKeys, AIndex + 1, FKeys, AIndex, (FCount - AIndex) - 1);
				Array.Copy(FNodes, AIndex + 1, FNodes, AIndex, (FCount - AIndex) - 1);
				
				// Decrement entry count
				FCount--;
			}

			public override bool Delete(IComparable AKey, IndexList AList)
			{
				int AClosestIndex;
				bool LMatch = Search(0, AKey, out AClosestIndex);
				if (!LMatch && AClosestIndex > 0)
					AClosestIndex--;
				Node LChild = FNodes[AClosestIndex];
				LMatch = LChild.Delete(AKey, AList);
				if (LMatch)
				{
					int LChildCapacity = LChild.FKeys.Length;

					// A Delete occurred, check for a possible merge
					if (LChild.FCount < (LChildCapacity / 3))	// if less than a third full
					{
						Node LPrior = (AClosestIndex == 0 ? null : FNodes[AClosestIndex - 1]);
						Node LNext = (AClosestIndex == (FCount - 1) ? null : FNodes[AClosestIndex + 1]);
						if
						(									// if there is enough room in the adjacent node(s) to handle this node's entries
							(LChildCapacity * 2)
								- ((LPrior == null ? 0 : LPrior.FCount) + (LNext == null ? 0 : LNext.FCount))
								>= LChild.FCount
						)
						{
							// Merge with adjacent nodes
							if (LPrior != null)
							{
								int LNeeded = (LChild.FCount / 2) + (LChild.FCount % 2);	// Assume half rounded up
								LChild.AppendTo
								(
									LPrior, 
									Math.Min	// Append the lesser of the number of slots available in the prior node, and half of the items to allocate plus the number that the next will not be able to handle of its half
									(
										LChildCapacity - LPrior.FCount, 
										LNeeded + (LNext == null ? 0 : Math.Max(0, LNeeded - (LChildCapacity - LNext.FCount)))
									)
								);
							}

							if (LNext != null)
								LChild.PrependTo(LNext);

							DeleteNode(AClosestIndex);
							RoutingNode LRoutingChild = LChild as RoutingNode;
							if (LRoutingChild != null)
								AList.RelinquishRoutingNode(LRoutingChild);
							else
								AList.RelinquishDataNode((DataNode)LChild);
						}
					}
				}
				return LMatch;
			}

			public override void Clear(IndexList AList)
			{
				for (int i = 0; i < FCount; i++)
				{
					Node LNode = FNodes[i];
					LNode.Clear(AList);
					DataNode LDataNode = LNode as DataNode;
					if (LNode != null)
						AList.RelinquishDataNode(LDataNode);
					else
						AList.RelinquishRoutingNode((RoutingNode)LNode);
				}
			}
		}

		internal class DataNode : Node
		{
			public DataNode()
			{
				FKeys = new IComparable[CCapacity];
				FEntries = new object[CCapacity];
			}

			public DataNode FNext;
			public DataNode FPrior;

			public object[] FEntries;

			public override bool Find(IComparable AKey, bool AForward, out object ANearest)
			{
				int LClosestIndex;
				bool LMatch = Search(0, AKey, out LClosestIndex);
				if (!LMatch)
				{
					if (AForward)
					{
						if (LClosestIndex >= FCount)
							if (FNext != null)
								return FNext.Find(AKey, AForward, out ANearest);
							else
							{
								ANearest = null;
								return false;
							}
					}
					else
					{
						LClosestIndex--;
						if (LClosestIndex < 0)
							if (FPrior != null)
								return FPrior.Find(AKey, AForward, out ANearest);
							else
							{
								ANearest = null;
								return false;
							}
					}
				}
				ANearest = FEntries[LClosestIndex];
				return LMatch;
			}

			public override bool Insert(IComparable AKey, object AData, IndexList AList)
			{
				int AClosestIndex;

				// Check for a split condition before the search.  In the case of a duplicate, this may cause an unnecessary split, but we'll optimize for the non-duplicate insert case.
				if (FCount == CCapacity)
					return false;

				// Perform the search
				bool LMatch = Search(0, AKey, out AClosestIndex); // start searching at AIndex = 0
				if (LMatch)
					throw new BaseException(BaseException.Codes.Duplicate, AKey.ToString());

				// Insert the entry
				InsertEntry(AKey, AData, AClosestIndex);

				return true;
			}

			/// <summary> Initializes this node to contain the upper half of entires from the given node (and removes them from the given one). </summary>
			public void CopyHalf(DataNode ANode)
			{
				FCount = ANode.FCount / 2;
				ANode.FCount -= FCount;
				Array.Copy(FKeys, 0, ANode.FKeys, ANode.FCount, FCount);
				Array.Copy(FEntries, 0, ANode.FEntries, ANode.FCount, FCount);
			}

			public override void PrependTo(Node ANode)
			{
				DataNode LTarget = ANode as DataNode;
				for (int i = 0; i < FCount; i++)
					LTarget.InsertEntry(FKeys[i], FEntries[i], i);
			}

			public override void AppendTo(Node ANode, int ACount)
			{
				DataNode LTarget = ANode as DataNode;
				for (int i = 0; i < ACount; i++)
					LTarget.InsertEntry(FKeys[i], FEntries[i], LTarget.FCount);
				Array.Copy(FKeys, ACount, FKeys, 0, FCount - ACount);
				Array.Copy(FEntries, ACount, FEntries, 0, FCount - ACount);
				FCount -= ACount;
			}

			public void InsertEntry(IComparable AKey, object AEntry, int AIndex)
			{
				// Slide all entries above the insert index
				Array.Copy(FKeys, AIndex, FKeys, AIndex + 1, FCount - AIndex);
				Array.Copy(FEntries, AIndex, FEntries, AIndex + 1, FCount - AIndex);

				// Set the new entry data			
				FKeys[AIndex] = AKey;
				FEntries[AIndex] = AEntry;

				// Increment entry count			
				FCount++;
			}

			public override bool Delete(IComparable AKey, IndexList AList)
			{
				int AClosestIndex;
				bool LMatch = Search(0, AKey, out AClosestIndex);
				if (LMatch)
					DeleteEntry(AClosestIndex);
				return LMatch;
			}

			public void DeleteEntry(int AIndex)
			{
				// Slide all entries above the delete index down over the item to be deleted.
				Array.Copy(FKeys, AIndex + 1, FKeys, AIndex, (FCount - AIndex) - 1);
				Array.Copy(FEntries, AIndex + 1, FEntries, AIndex, (FCount - AIndex) - 1);
				
				// Decrement entry count
				FCount--;
			}

			public override void Clear(IndexList AList)
			{
				// Nothing
			}
		}
	}
}
