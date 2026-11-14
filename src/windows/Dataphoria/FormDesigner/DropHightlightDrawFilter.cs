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
			_tree = LTree;
			_tree.OnPrePaintItem += new PaintTreeItemEventHandler(TreePrePaintItem);
			_tree.OnPostPaintItem += new PaintTreeItemEventHandler(TreePostPaintItem);
			_dropHighlightNode = null;
			_dropLinePosition = DropLinePosition.None;
			_dropHighlightBackColor = System.Drawing.SystemColors.Highlight;
			_dropHighlightForeColor = System.Drawing.SystemColors.HighlightText;
			_dropLineColor = System.Drawing.SystemColors.ControlText;
			_dropLineWidth = 2;
		} 

		private TreeView _tree;
		public TreeView Tree
		{
			get { return _tree; }
		}

		private QueryAllowedPositionsHandler _queryAllowedPositions;
		public QueryAllowedPositionsHandler QueryAllowedPositions
		{
			get { return _queryAllowedPositions; }
			set { _queryAllowedPositions = value; }
		}

		private TreeNode _dropHighlightNode;
		public TreeNode DropHighlightNode
		{
			get { return _dropHighlightNode; }
		}

		private DropLinePosition _dropLinePosition;
		public DropLinePosition DropLinePosition
		{
			get { return _dropLinePosition; }
		}

		private DropOperation _dropOperation;
		public DropOperation DropOperation
		{
			get { return _dropOperation; }
		}

		private int _dropLineWidth;
		public int DropLineWidth
		{
			get { return _dropLineWidth; }
			set { _dropLineWidth = value; }
		}

		private System.Drawing.Color _dropHighlightBackColor; 
		/// <remarks> This only affect the node when it is being dropped On. Not Above or Below. </remarks>
		public System.Drawing.Color DropHighlightBackColor
		{
			get { return _dropHighlightBackColor; }
			set { _dropHighlightBackColor = value; }
		}

		private System.Drawing.Color  _dropHighlightForeColor;
		/// <remarks> This only affect the node when it is being dropped On. Not Above or Below. </remarks>
		public System.Drawing.Color  DropHighlightForeColor
		{
			get { return _dropHighlightForeColor; }
			set { _dropHighlightForeColor = value; }
		}

		private System.Drawing.Color _dropLineColor;
		public System.Drawing.Color DropLineColor
		{
			get { return _dropLineColor; }
			set { _dropLineColor = value; }
		}

		private void PositionChanged()
		{
			_tree.Invalidate();
		}

		public void ClearDropHighlight()
		{
			_dropOperation = DropOperation.None;
			SetDropHighlightNode(null, DropLinePosition.None);
		}

		/// <summary> Call this proc every time the DragOver event of the Tree fires. </summary>
		/// <remarks> The point passed in is in Tree coords. </remarks>
		public void SetDropHighlight(System.Drawing.Point pointInTreeCoords, object source, DropOperation requestedOperation)
		{
			TreeNode targetNode = _tree.GetNodeAt(pointInTreeCoords.X, pointInTreeCoords.Y);
			if (targetNode != null)
			{
				Rectangle targetNodeBounds = targetNode.Bounds;

				// Determine the allowed positions
				QueryAllowedPositionsEventArgs positionsArgs = new QueryAllowedPositionsEventArgs();
				positionsArgs.TargetNode = targetNode;
				positionsArgs.Source = source;
				positionsArgs.AllowedMovePositions = DropLinePosition.None;
				positionsArgs.AllowedCopyPositions = DropLinePosition.None;
				if (QueryAllowedPositions != null)
					QueryAllowedPositions(this, positionsArgs);

				// Determine the distance from edge offset based on the allowed positions
				int distanceFromEdge;
				if (((positionsArgs.AllowedMovePositions | positionsArgs.AllowedCopyPositions) & (DropLinePosition.AboveNode | DropLinePosition.BelowNode)) != 0)
					if (((positionsArgs.AllowedMovePositions | positionsArgs.AllowedCopyPositions) & DropLinePosition.OnNode) != 0)
						distanceFromEdge = targetNodeBounds.Height / 3;
					else
						distanceFromEdge = targetNodeBounds.Height / 2;
				else
					distanceFromEdge = 0;
 
				DropLinePosition newDropLinePosition;

				if (pointInTreeCoords.Y < (targetNodeBounds.Top + distanceFromEdge))
					newDropLinePosition = DropLinePosition.AboveNode;
				else
				{
					if (pointInTreeCoords.Y > (targetNodeBounds.Bottom - distanceFromEdge))
						newDropLinePosition = DropLinePosition.BelowNode;
					else
						newDropLinePosition = DropLinePosition.OnNode;
				}

				_dropOperation = DropOperation.None;
				if (((requestedOperation & DropOperation.Move) != 0) && ((newDropLinePosition & positionsArgs.AllowedMovePositions) != 0))
					_dropOperation |= DropOperation.Move;
				if (((requestedOperation & DropOperation.Copy) != 0) && ((newDropLinePosition & positionsArgs.AllowedCopyPositions) != 0))
					_dropOperation |= DropOperation.Copy;
				if (_dropOperation != DropOperation.None)
				{
					SetDropHighlightNode(targetNode, newDropLinePosition);
					return;
				}
			}
			ClearDropHighlight();
		}

		private void SetDropHighlightNode(TreeNode node, DropLinePosition dropLinePosition)
		{
			bool isPositionChanged = false;

			isPositionChanged = 
				!
				(
					Object.ReferenceEquals(_dropHighlightNode, node) && 
					(_dropLinePosition == dropLinePosition)
				);

			//Set both properties without calling PositionChanged
			_dropHighlightNode = node;
			_dropLinePosition = dropLinePosition;

			if (isPositionChanged)
				PositionChanged();
		}

		private void TreePrePaintItem(object sender, PaintTreeItemEventArgs args)
		{
			if ((_dropHighlightNode != null) && (_dropLinePosition == DropLinePosition.OnNode))
			{
				if (args.Node.Equals(_dropHighlightNode))
				{
					args.BackColor = _dropHighlightBackColor;
					args.ForeColor = _dropHighlightForeColor;
				}
			}
		}

		private void TreePostPaintItem(object sender, PaintTreeItemEventArgs args)
		{
			if ((_dropHighlightNode != null) && (_dropHighlightNode == args.Node) && (_dropLinePosition != DropLinePosition.None))
			{
				using (System.Drawing.Pen pen = new System.Drawing.Pen(_dropLineColor, _dropLineWidth))
				{
					int leftEdge = args.Node.Bounds.Left - 4;
					if (args.Node.ImageIndex >= 0)
						leftEdge -= 20;

					System.Windows.Forms.TreeView tree = args.Node.TreeView; 
					int rightEdge = tree.DisplayRectangle.Right - 4;

					int lineVPosition;

					if ((_dropLinePosition & DropLinePosition.AboveNode) == DropLinePosition.AboveNode)
					{
						lineVPosition = _dropHighlightNode.Bounds.Top;
						args.Graphics.DrawLine(pen, leftEdge, lineVPosition, rightEdge, lineVPosition);
						pen.Width = 1;
						args.Graphics.DrawLine(pen, leftEdge, lineVPosition - 3, leftEdge, lineVPosition + 2);
						args.Graphics.DrawLine(pen, leftEdge + 1, lineVPosition - 2, leftEdge + 1, lineVPosition + 1);
						args.Graphics.DrawLine(pen, rightEdge, lineVPosition - 3, rightEdge, lineVPosition + 2);
						args.Graphics.DrawLine(pen, rightEdge - 1, lineVPosition - 2, rightEdge - 1, lineVPosition + 1);
					}
					if ((_dropLinePosition & DropLinePosition.BelowNode) == DropLinePosition.BelowNode)
					{
						lineVPosition = _dropHighlightNode.Bounds.Bottom;
						args.Graphics.DrawLine(pen, leftEdge, lineVPosition, rightEdge, lineVPosition);
						pen.Width = 1;
						args.Graphics.DrawLine(pen, leftEdge, lineVPosition - 3, leftEdge, lineVPosition + 2);
						args.Graphics.DrawLine(pen, leftEdge + 1, lineVPosition - 2, leftEdge + 1, lineVPosition + 1);
						args.Graphics.DrawLine(pen, rightEdge, lineVPosition - 3, rightEdge, lineVPosition + 2);
						args.Graphics.DrawLine(pen, rightEdge - 1, lineVPosition - 2, rightEdge - 1, lineVPosition + 1);
					}
				}
			}
		}
	}
}