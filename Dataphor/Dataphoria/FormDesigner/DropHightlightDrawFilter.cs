/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
	[System.Flags] 
	public enum DropLinePosition
	{
		None = 0,
		OnNode = 1,
		AboveNode = 2,
		BelowNode = 4,
		All = OnNode | AboveNode | BelowNode
	}

	[System.Flags]
	public enum DropOperation
	{
		None = 0,
		Copy = 1,
		Move = 2,
		Both = Copy | Move
	}

	public class QueryAllowedPositionsEventArgs : System.EventArgs
	{
		public TreeNode TargetNode;
		public object Source;
		public DropLinePosition AllowedMovePositions;
		public DropLinePosition AllowedCopyPositions;
	}

	public delegate void QueryAllowedPositionsHandler(object ASender, QueryAllowedPositionsEventArgs AArgs);

	public class DropHighlightDrawFilter
	{
		public DropHighlightDrawFilter(TreeView LTree)
		{
			FTree = LTree;
			FTree.OnPrePaintItem += new PaintTreeItemEventHandler(TreePrePaintItem);
			FTree.OnPostPaintItem += new PaintTreeItemEventHandler(TreePostPaintItem);
			FDropHighlightNode = null;
			FDropLinePosition = DropLinePosition.None;
			FDropHighlightBackColor = System.Drawing.SystemColors.Highlight;
			FDropHighlightForeColor = System.Drawing.SystemColors.HighlightText;
			FDropLineColor = System.Drawing.SystemColors.ControlText;
			FDropLineWidth = 2;
		} 

		private TreeView FTree;
		public TreeView Tree
		{
			get { return FTree; }
		}

		private QueryAllowedPositionsHandler FQueryAllowedPositions;
		public QueryAllowedPositionsHandler QueryAllowedPositions
		{
			get { return FQueryAllowedPositions; }
			set { FQueryAllowedPositions = value; }
		}

		private TreeNode FDropHighlightNode;
		public TreeNode DropHighlightNode
		{
			get { return FDropHighlightNode; }
		}

		private DropLinePosition FDropLinePosition;
		public DropLinePosition DropLinePosition
		{
			get { return FDropLinePosition; }
		}

		private DropOperation FDropOperation;
		public DropOperation DropOperation
		{
			get { return FDropOperation; }
		}

		private int FDropLineWidth;
		public int DropLineWidth
		{
			get { return FDropLineWidth; }
			set { FDropLineWidth = value; }
		}

		private System.Drawing.Color FDropHighlightBackColor; 
		/// <remarks> This only affect the node when it is being dropped On. Not Above or Below. </remarks>
		public System.Drawing.Color DropHighlightBackColor
		{
			get { return FDropHighlightBackColor; }
			set { FDropHighlightBackColor = value; }
		}

		private System.Drawing.Color  FDropHighlightForeColor;
		/// <remarks> This only affect the node when it is being dropped On. Not Above or Below. </remarks>
		public System.Drawing.Color  DropHighlightForeColor
		{
			get { return FDropHighlightForeColor; }
			set { FDropHighlightForeColor = value; }
		}

		private System.Drawing.Color FDropLineColor;
		public System.Drawing.Color DropLineColor
		{
			get { return FDropLineColor; }
			set { FDropLineColor = value; }
		}

		private void PositionChanged()
		{
			FTree.Invalidate();
		}

		public void ClearDropHighlight()
		{
			FDropOperation = DropOperation.None;
			SetDropHighlightNode(null, DropLinePosition.None);
		}

		/// <summary> Call this proc every time the DragOver event of the Tree fires. </summary>
		/// <remarks> The point passed in is in Tree coords. </remarks>
		public void SetDropHighlight(System.Drawing.Point APointInTreeCoords, object ASource, DropOperation ARequestedOperation)
		{
			TreeNode LTargetNode = FTree.GetNodeAt(APointInTreeCoords.X, APointInTreeCoords.Y);
			if (LTargetNode != null)
			{
				Rectangle LTargetNodeBounds = LTargetNode.Bounds;

				// Determine the allowed positions
				QueryAllowedPositionsEventArgs LPositionsArgs = new QueryAllowedPositionsEventArgs();
				LPositionsArgs.TargetNode = LTargetNode;
				LPositionsArgs.Source = ASource;
				LPositionsArgs.AllowedMovePositions = DropLinePosition.None;
				LPositionsArgs.AllowedCopyPositions = DropLinePosition.None;
				if (QueryAllowedPositions != null)
					QueryAllowedPositions(this, LPositionsArgs);

				// Determine the distance from edge offset based on the allowed positions
				int LDistanceFromEdge;
				if (((LPositionsArgs.AllowedMovePositions | LPositionsArgs.AllowedCopyPositions) & (DropLinePosition.AboveNode | DropLinePosition.BelowNode)) != 0)
					if (((LPositionsArgs.AllowedMovePositions | LPositionsArgs.AllowedCopyPositions) & DropLinePosition.OnNode) != 0)
						LDistanceFromEdge = LTargetNodeBounds.Height / 3;
					else
						LDistanceFromEdge = LTargetNodeBounds.Height / 2;
				else
					LDistanceFromEdge = 0;
 
				DropLinePosition LNewDropLinePosition;

				if (APointInTreeCoords.Y < (LTargetNodeBounds.Top + LDistanceFromEdge))
					LNewDropLinePosition = DropLinePosition.AboveNode;
				else
				{
					if (APointInTreeCoords.Y > (LTargetNodeBounds.Bottom - LDistanceFromEdge))
						LNewDropLinePosition = DropLinePosition.BelowNode;
					else
						LNewDropLinePosition = DropLinePosition.OnNode;
				}

				FDropOperation = DropOperation.None;
				if (((ARequestedOperation & DropOperation.Move) != 0) && ((LNewDropLinePosition & LPositionsArgs.AllowedMovePositions) != 0))
					FDropOperation |= DropOperation.Move;
				if (((ARequestedOperation & DropOperation.Copy) != 0) && ((LNewDropLinePosition & LPositionsArgs.AllowedCopyPositions) != 0))
					FDropOperation |= DropOperation.Copy;
				if (FDropOperation != DropOperation.None)
				{
					SetDropHighlightNode(LTargetNode, LNewDropLinePosition);
					return;
				}
			}
			ClearDropHighlight();
		}

		private void SetDropHighlightNode(TreeNode ANode, DropLinePosition ADropLinePosition)
		{
			bool LIsPositionChanged = false;

			LIsPositionChanged = 
				!
				(
					Object.ReferenceEquals(FDropHighlightNode, ANode) && 
					(FDropLinePosition == ADropLinePosition)
				);

			//Set both properties without calling PositionChanged
			FDropHighlightNode = ANode;
			FDropLinePosition = ADropLinePosition;

			if (LIsPositionChanged)
				PositionChanged();
		}

		private void TreePrePaintItem(object ASender, PaintTreeItemEventArgs AArgs)
		{
			if ((FDropHighlightNode != null) && (FDropLinePosition == DropLinePosition.OnNode))
			{
				if (AArgs.Node.Equals(FDropHighlightNode))
				{
					AArgs.BackColor = FDropHighlightBackColor;
					AArgs.ForeColor = FDropHighlightForeColor;
				}
			}
		}

		private void TreePostPaintItem(object ASender, PaintTreeItemEventArgs AArgs)
		{
			if ((FDropHighlightNode != null) && (FDropHighlightNode == AArgs.Node) && (FDropLinePosition != DropLinePosition.None))
			{
				using (System.Drawing.Pen LPen = new System.Drawing.Pen(FDropLineColor, FDropLineWidth))
				{
					int LLeftEdge = AArgs.Node.Bounds.Left - 4;
					if (AArgs.Node.ImageIndex >= 0)
						LLeftEdge -= 20;

					System.Windows.Forms.TreeView LTree = AArgs.Node.TreeView; 
					int LRightEdge = LTree.DisplayRectangle.Right - 4;

					int LLineVPosition;

					if ((FDropLinePosition & DropLinePosition.AboveNode) == DropLinePosition.AboveNode)
					{
						LLineVPosition = FDropHighlightNode.Bounds.Top;
						AArgs.Graphics.DrawLine(LPen, LLeftEdge, LLineVPosition, LRightEdge, LLineVPosition);
						LPen.Width = 1;
						AArgs.Graphics.DrawLine(LPen, LLeftEdge, LLineVPosition - 3, LLeftEdge, LLineVPosition + 2);
						AArgs.Graphics.DrawLine(LPen, LLeftEdge + 1, LLineVPosition - 2, LLeftEdge + 1, LLineVPosition + 1);
						AArgs.Graphics.DrawLine(LPen, LRightEdge, LLineVPosition - 3, LRightEdge, LLineVPosition + 2);
						AArgs.Graphics.DrawLine(LPen, LRightEdge - 1, LLineVPosition - 2, LRightEdge - 1, LLineVPosition + 1);
					}
					if ((FDropLinePosition & DropLinePosition.BelowNode) == DropLinePosition.BelowNode)
					{
						LLineVPosition = FDropHighlightNode.Bounds.Bottom;
						AArgs.Graphics.DrawLine(LPen, LLeftEdge, LLineVPosition, LRightEdge, LLineVPosition);
						LPen.Width = 1;
						AArgs.Graphics.DrawLine(LPen, LLeftEdge, LLineVPosition - 3, LLeftEdge, LLineVPosition + 2);
						AArgs.Graphics.DrawLine(LPen, LLeftEdge + 1, LLineVPosition - 2, LLeftEdge + 1, LLineVPosition + 1);
						AArgs.Graphics.DrawLine(LPen, LRightEdge, LLineVPosition - 3, LRightEdge, LLineVPosition + 2);
						AArgs.Graphics.DrawLine(LPen, LRightEdge - 1, LLineVPosition - 2, LRightEdge - 1, LLineVPosition + 1);
					}
				}
			}
		}
	}
}