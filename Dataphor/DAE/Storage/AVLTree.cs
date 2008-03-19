/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE.Storage
{
	/// <summary> Interface for an item participating in a doubly linked list. </summary>
	public interface IDoubleLinkedItem
	{
		IDoubleLinkedItem Prior { get; set; }
		IDoubleLinkedItem Next { get; set; }
	}

	/// <summary> Bucketed AVL tree node. </summary>
	/// <remarks>
	///		<p>The contained value is common to the items in the bucket,
	///		and also provides a basis for comparison within the tree.</p>
	///		<p>Items in the bucket are maintined as a doubly-linked list.</p>
	///	</remarks>
	public class AVLTreeNode
	{
		public AVLTreeNode(IComparable AValue)
		{
			FValue = AValue;
		}

		/*
			Balancing Flag
			When:
				Flag == 0	tree balanced below node
				Flag < 0	tree taller on left
				Flag > 0	tree taller on right
		*/
		internal sbyte FFlag;

		// Node data
		internal IComparable FValue;

		// Node item bucket
		internal IDoubleLinkedItem FHead;
		internal IDoubleLinkedItem FTail;
		
		// Child nodes
		internal AVLTreeNode FLeft;
		internal AVLTreeNode FRight;

		/// <summary> Creates a new tree node and adds an item to it's bucket. </summary>
		/// <remarks>
		///		The node is assumed to have no items, so the item becomes both the 
		///		head and tail of the bucket's linked list.  
		///	</remarks>
		/// <param name="AValue"> Value to initialize the new node. </param>
		/// <param name="AItem"> Initial item for the new node. </param>
		/// <returns> The newly created tree node. </returns>
		internal static AVLTreeNode CreateNodeWithItem(IComparable AValue, IDoubleLinkedItem AItem)
		{
			AVLTreeNode LNode = new AVLTreeNode(AValue);
			LNode.FHead = AItem;
			LNode.FTail = AItem;
			AItem.Next = null;
			AItem.Prior = null;
			return LNode;
		}

		private static AVLTreeNode RotateRight(AVLTreeNode AChild)
		{
			AVLTreeNode LResult = AChild;
			LResult = LResult.FLeft;
			AChild.FLeft = LResult.FRight;
			LResult.FRight = AChild;
			AChild.FFlag -= 2;
			LResult.FFlag--;
			return LResult;
		}

		private static AVLTreeNode RotateLeft(AVLTreeNode AChild)
		{
			AVLTreeNode LResult = AChild;
			LResult = LResult.FRight;
			AChild.FRight = LResult.FLeft;
			LResult.FLeft = AChild;
			AChild.FFlag += 2;
			LResult.FFlag++;
			return LResult;
		}

		/// <summary> Rebalances the node branch by rotating nodes left or right. </summary>
		/// <param name="AChild"> The top node of the rotation. </param>
		/// <returns> Returns the new top node in the rotated subtree. </returns>
		internal AVLTreeNode Balance()
		{
			if ((FFlag >= -1) && (FFlag <= 1))
				return this;						// No Rotation necessary
			else
			{
				if (FFlag > 1)				//Tree is tall on the left
				{
					if (FLeft.FFlag > 0)		// LL Rotation
						return RotateRight(this);
					else							// LR Rotation
					{
						FLeft = RotateLeft(FLeft);
						return RotateRight(this);
					}
				}
				else /* (FFlag < 1) */		// Tree is tall on the right
				{
					if (FRight.FFlag < 0)	// RR Rotation
						return RotateLeft(this);
					else							// RL Rotation
					{
						FRight = RotateRight(FRight);
						return RotateLeft(this);
					}
				}
			}
		}

		/// <summary> Adds an item to the head of the bucket for the applicable node (determined by value). </summary>
		/// <param name="AValue"> The value used to determine location within the tree. </param>
		/// <param name="AItem">
		///		The item to add to the head of the linked list bucket. AItem.Next 
		///		is assumed to be initialized to null.
		///	</param>
		/// <returns> Returns whether or not a balance check is necessary for the caller. </returns>
		internal bool AddHeadItem(IComparable AValue, IDoubleLinkedItem AItem)
		{
			int LCompare = AValue.CompareTo(FValue);
			if (LCompare == 0)
			{
				AItem.Prior = FHead;
				if (FHead != null)
					FHead.Next = AItem;
				else
					FTail = AItem;	// Only item (both head and tail)
				FHead = AItem;
				return false;
			}
			else if (LCompare > 0)
			{
				FFlag++;
				if (FRight == null)
					FRight = CreateNodeWithItem(AValue, AItem);
				else
				{
					if (FRight.AddHeadItem(AValue, AItem))
					{
						AVLTreeNode LOldRight = FRight;
						return (FRight = FRight.Balance()) != LOldRight;
					}
					else
						return false;
				}
			}
			else /* (LCompare < 0) */
			{
				FFlag--;
				if (FLeft == null)
					FLeft = CreateNodeWithItem(AValue, AItem);
				else
				{
					if (FLeft.AddHeadItem(AValue, AItem))
					{
						AVLTreeNode LOldLeft = FLeft;
						return (FLeft = FLeft.Balance()) != LOldLeft;
					}
					else
						return false;
				}
			}
			return true;
		}

		/// <summary> Removes the item from the tail of the bucket. </summary>
		/// <param name="AItem"> The item that was removed from the tail of the bucket. </param>
		/// <returns> True if there are no more items in this node's bucket. </returns>
		private bool RemoveTailItem(out IDoubleLinkedItem AItem)
		{
			AItem = FTail;
			if (AItem.Prior != null)
			{
				AItem.Prior.Next = null;
				FTail = AItem.Prior;
				return false;
			}
			else	/* No more items - remove the node */
				return true;
		}

		/// <summary> Locates and removes the leftmost tail item. </summary>
		/// <param name="AItem"> The located, leftmost tail item. </param>
		/// <returns> True if there are no more items in this node's bucket. </returns>
		internal bool FindAndRemoveLeftMostTailItem(out IDoubleLinkedItem AItem)
		{
			if (FLeft == null)
				return RemoveTailItem(out AItem);
			else
			{
				if (FLeft.FindAndRemoveLeftMostTailItem(out AItem))
				{
					FLeft = FLeft.RemoveNode();
					FFlag++;
				}
				else
					FLeft = FLeft.Balance();
				return false;
			}
		}

		/// <summary> Used internally to remove an empty node. </summary>
		/// <remarks> No checks are made to ensure that the node is empty. </remarks>
		/// <returns>
		///		<p> The new node (if any) which replaces the current. </p>
		///		<p>
		///			If no children exist, null is returned (no replacement for node).
		///			If one child exists (either left or right), that child is returned.
		///			If two children exist, the predicessor is found, removed and this
		///			nodes values are swapped with it.  This same node is returned with
		///			the values from the preceeding node.
		///		</p>
		///	</returns>
		internal AVLTreeNode RemoveNode()
		{
			AVLTreeNode LResult = this;
			if (FLeft == null)
			{
				if (FRight == null)
					return null;
				else
					return FRight;
			}
			else
			{
				if (FRight == null)
					return FLeft;
				else
				{
					AVLTreeNode LIterator = FLeft;
					FFlag--;
					if (LIterator.FRight == null)
						FLeft = LIterator.RemoveNode();
					else
					{
						for (;;)
						{
							LIterator.FFlag++;
							if (LIterator.FRight.FRight == null)
							{
								AVLTreeNode LPredicessor = LIterator.FRight;
								LIterator.FRight = LIterator.FRight.RemoveNode();
								LIterator = LPredicessor;
								break;
							}
							LIterator = LIterator.FRight;
						};
					}
					//Swap our value with that of the preceeding (by value) node's value
					FValue = LIterator.FValue;
					FHead = LIterator.FHead;
					FTail = LIterator.FTail;
					return this;
				}
			}
		}

		/// <summary> Removes an item from the appropriate bucket. </summary>
		/// <param name="AValue"> The value indicating the appropriate bucket to remove from. </param>
		/// <param name="AItem"> The item to remove from the specified bucket. </param>
		/// <returns> True if there are no more items in this node's bucket. </returns>
		internal bool RemoveItem(IComparable AValue, IDoubleLinkedItem AItem)
		{
			int LComparison = AValue.CompareTo(FValue);
			if (LComparison == 0)
			{
				if (AItem.Next == null)
					FHead = AItem.Prior;
				else
					AItem.Next.Prior = AItem.Prior;
				if (AItem.Prior == null)
					FTail = AItem.Next;
				else
					AItem.Prior.Next = AItem.Next;
				return (FTail == null);
			} 
			else
			{
				if (LComparison < 0)			// Right of
				{
					if (FLeft.RemoveItem(AValue, AItem))
					{
						FLeft = FLeft.RemoveNode();
						FFlag--;
					}
					else
						FLeft = FLeft.Balance();
				}
				else /* (LComparison > 0) */	// Left of
				{
					if (FRight.RemoveItem(AValue, AItem))
					{
						FRight = FRight.RemoveNode();
						FFlag++;
					}
					else
						FRight = FRight.Balance();
				}
				return false;
			}
		}


	}

	/// <summary> Bucketed AVL balanced tree class. </summary>
	/// <remarks> This AVL tree is optimized for inserts and finding/deleting extremes. </remarks>
	public class AVLTree
	{
		private AVLTreeNode FRoot;

		private int FCount;
		/// <summary> Count of items in the tree. </summary>
		/// <remarks> Relatively slow and stack intensive. Recursively iterates trough items. </remarks>
		public int Count
		{
			get { return FCount; }
		}

		/// <summary> Adds an item to the head of the appropriate bucket.</summary>
		/// <param name="AValue">
		///		The value of the bucket in which the item is to be added.  The 
		///		bucket will be created if necessary.
		///	</param>
		/// <param name="AItem"> The item to add to the head of the appropriate bucket. </param>
		public void AddHeadItem(IComparable AValue, IDoubleLinkedItem AItem)
		{
			if (FRoot == null)
				FRoot = AVLTreeNode.CreateNodeWithItem(AValue, AItem);
			else
			{
				if (FRoot.AddHeadItem(AValue, AItem))
					FRoot = FRoot.Balance();
			}
			FCount++;
		}

		/// <summary> Removes the given item from the tree. </summary>
		/// <param name="AValue"> The bucket in which the item can be found. </param>
		/// <param name="AItem"> The item to remove. </param>
		public void RemoveItem(IComparable AValue, IDoubleLinkedItem AItem)
		{
			if (FRoot == null)
				throw new Exception(CCannotRemoveFromEmpty);
			else
				if (FRoot.RemoveItem(AValue, AItem))
					FRoot = FRoot.RemoveNode();
			FCount--;
		}

		/// <summary> Finds and returns the tale item in the leftmost value bucket. </summary>
		/// <remarks>
		///		This method does not use recursion and directly manipulates the nodes.  It's
		///		tasks could be abstracted, but are not to maximize performance.
		///	</remarks>
		/// <returns> Found item, or null if not found. </returns>
		public IDoubleLinkedItem FindAndRemoveLeftmostTailItem()
		{
			if (FRoot == null)
				return null;
			else
			{
				IDoubleLinkedItem LResult;
				if (FRoot.FindAndRemoveLeftMostTailItem(out LResult))
					FRoot = FRoot.RemoveNode();
				FCount--;
				return LResult;
			}
		}
		
	}
}
