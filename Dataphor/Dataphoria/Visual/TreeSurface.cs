/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public class TreeSurface : Surface
	{
		public const int GutterWidth = 4;
		public const int GutterHeight = 4;

		public TreeSurface()
		{
			SetStyle(ControlStyles.ResizeRedraw, false);
			_nodes = new RootTreeNodes(this);
			SuspendLayout();
			try
			{
				_hScrollBar = new HScrollBar();
				_hScrollBar.SmallChange = 1;
				_hScrollBar.LargeChange = 1;
				_hScrollBar.Minimum = 0;
				_hScrollBar.TabStop = false;
				_hScrollBar.Scroll += new ScrollEventHandler(HScrollBarScrolled);
				Controls.Add(_hScrollBar);

				_vScrollBar = new VScrollBar();
				_vScrollBar.SmallChange = 1;
				_vScrollBar.Minimum = 0;
				_vScrollBar.TabStop = false;
				_vScrollBar.Scroll += new ScrollEventHandler(VScrollBarScrolled);
				Controls.Add(_vScrollBar);
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		#region Cosmetic Properties

		private int _rowHeight = 40;
		public int RowHeight
		{
			get { return _rowHeight; }
			set
			{
				if (value < 1)
					value = 1;
				if (_rowHeight != value)
				{
					_rowHeight = value;
					UpdateSurface();
					PerformLayout();
				}
			}
		}

		private int _indent = 40;
		public int Indent
		{
			get { return _indent; }
			set
			{
				if (value < 1)
					value = 1;
				if (_indent != value)
				{
					_indent = value;
					UpdateSurface();
					PerformLayout();
				}
			}
		}

		#endregion

		#region Scrolling & Layout

		private HScrollBar _hScrollBar;
		private VScrollBar _vScrollBar;
		private int _visibleCount;

		private void HScrollBarScrolled(object sender, ScrollEventArgs args)
		{
			NavigateTo(new Point(args.NewValue, _location.Y));
		}

		private void VScrollBarScrolled(object sender, ScrollEventArgs args)
		{
			SetVerticalScroll(args.NewValue);
		}

		private void SetVerticalScroll(int tempValue)
		{
			tempValue = Math.Max(0, Math.Min(_exposed.Count, tempValue + (_visibleCount - 1)) - (_visibleCount - 1));
			int x = MinDisplayLevel(tempValue);
			if (x == Int32.MaxValue)
				x = _location.X;
			NavigateTo(new Point(x, tempValue));
		}

		/// <summary> Returns the level of the shallowest visible node. </summary>
		private int MinDisplayLevel(int locationY)
		{
			int min = Int32.MaxValue;
			int current;
			int end = Math.Min(_exposed.Count, locationY + _visibleCount);
			for (int i = locationY; i < end; i++) 
			{
				current = ((TreeNode)_exposed[i]).Level();
				if (current < min)
					min = current;
			}
			return min;
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
			{
				case Keys.Left : NavigateTo(new Point(_location.X - 1, _location.Y)); break;
				case Keys.Up : SetVerticalScroll(_location.Y - 1); break;
				case Keys.Right : NavigateTo(new Point(_location.X + 1, _location.Y)); break;
				case Keys.Down : SetVerticalScroll(_location.Y + 1); break;
				case Keys.PageUp : SetVerticalScroll(_location.Y - _visibleCount); break;
				case Keys.PageDown : SetVerticalScroll(_location.Y + _visibleCount); break;
				case Keys.Home : NavigateTo(new Point(0, _location.Y)); break;
				case Keys.End : NavigateTo(new Point(_hScrollBar.Maximum, _location.Y)); break;
				default :
					return base.ProcessDialogKey(key);
			}
			return true;
		}

		private static int Constrain(int tempValue, int min, int max)
		{
			if (tempValue > max)
				tempValue = max;
			if (tempValue < min)
				tempValue = min;
			return tempValue;
		}

		private Point _location;

		private void NavigateTo(Point location)
		{
			location.X = Constrain(location.X, _hScrollBar.Minimum, _hScrollBar.Maximum);
			location.Y = Math.Max(0, Math.Min(_exposed.Count, location.Y + (_visibleCount - 1)) - (_visibleCount - 1));
			if (location != _location)
			{
				Point delta = new Point((_location.X - location.X) * _indent, (_location.Y - location.Y) * _rowHeight);

				_location = location;
				
				_hScrollBar.Value = _location.X;
				_vScrollBar.Value = _location.Y;

				UpdateDesigners(true);	// even if layout weren't supressed, there is no guarantee that a layout will be performed

				RECT rect = UnsafeUtilities.RECTFromRectangle(DisplayRectangle);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, delta.X, delta.Y, ref rect, ref rect, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */);

				PerformLayout();
			}
		}

		protected override void OnResize(EventArgs args)
		{
			if (IsHandleCreated)
			{
				UpdateSurface();
				MinimizeScrolling();
			}
			base.OnResize(args);
		}

		protected override void OnHandleCreated(EventArgs args)
		{
			base.OnHandleCreated(args);
			UpdateSurface();
		}

		private void MinimizeScrolling()
		{
			// Scroll off wasted space at the end
			int newLocationY = Math.Max(0, Math.Min(_exposed.Count, _location.Y + (_visibleCount - 1)) - (_visibleCount - 1));
			if (newLocationY != _location.Y)
				NavigateTo(new Point(_location.X, newLocationY));
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			// Don't call base

			Rectangle bounds = base.DisplayRectangle;

			MinimizeScrolling();

			// Size the scrollbars
			_vScrollBar.Visible = (_vScrollBar.Maximum > 0) && (_vScrollBar.LargeChange <= _vScrollBar.Maximum);
			_hScrollBar.Visible = (_hScrollBar.Maximum > 0);

			_vScrollBar.Left = bounds.Right - _vScrollBar.Width;
			_vScrollBar.Height = bounds.Height - (_hScrollBar.Visible ? _hScrollBar.Height : 0);

			_hScrollBar.Top = bounds.Bottom - _hScrollBar.Height;
			_hScrollBar.Width = bounds.Width - (_vScrollBar.Visible ? _vScrollBar.Width : 0);

			bounds = DisplayRectangle;

			// Position the designers
			IElementDesigner designer;
			TreeNode node;
			int end = Math.Min(_exposed.Count, _location.Y + _visibleCount);
			for (int i = _location.Y; i < end; i++)
			{
				designer = (IElementDesigner)_designers[_exposed[i]];
				node = (TreeNode)designer.Element;
				designer.Bounds =
					new Rectangle
					(
						((node.Level() - _location.X) * _indent) + (GutterWidth / 2),
						((i - _location.Y) * _rowHeight) + (GutterHeight / 2),
						250,
						_rowHeight - GutterHeight
					);
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				if (_vScrollBar.Visible)
					bounds.Width -= _vScrollBar.Width;
				if (_hScrollBar.Visible)
					bounds.Height -= _hScrollBar.Height;
				return bounds;
			}
		}

		/// <summary> Reconciles the set of visible designers with the set of possible exposed nodes. </summary>
		/// <remarks> Called when the active rectangle, or the set of exposed nodes changes. Will only 
		/// cause a layout if a designer is added or removed and ASupressLayout is false. </remarks>
		private void UpdateDesigners(bool supressLayout)
		{
			if (_updateCount == 0)
			{
				bool layoutRequired = false;
				// List of unused designers (initially full)
				ArrayList designed = new ArrayList(_designers.Count);
				designed.AddRange(_designers.Keys);

				SuspendLayout();
				try
				{
					int end = Math.Min(_exposed.Count, _location.Y + _visibleCount);
					IElementDesigner designer;
					for (int i = _location.Y; i < end; i++)
					{
						designer = (IElementDesigner)_designers[_exposed[i]];
						if (designer == null)
						{
							designer = AddDesigner(_exposed[i]);	// Add designers we need but don't have
							layoutRequired = true;
						}
						else
							designed.Remove(designer.Element);	// Remember that this designer is used
						((Control)designer).TabIndex = i;
					}

					// Remove unused designers
					foreach (object node in designed)
					{
						RemoveDesigner(node);
						layoutRequired = true;
					}
				}
				finally
				{
					ResumeLayout(!supressLayout && layoutRequired);
				}
			}
		}

		private void UpdateSurface()
		{
			if (_updateCount == 0)
			{
				_hScrollBar.Maximum = Math.Max(0, Nodes.Depth() - 1);
				_vScrollBar.Maximum = Math.Max(0, (_exposed.Count - 1));

				Rectangle bounds = DisplayRectangle;
				_visibleCount = (bounds.Height / _rowHeight) + (((bounds.Height % _rowHeight) == 0) ? 0 : 1);
				_vScrollBar.LargeChange = Math.Max((_visibleCount - 1), 1);

				UpdateDesigners(true);
			}
		}

		private int _updateCount = 0;

		public void BeginUpdate()
		{
			_updateCount++;
			SuspendLayout();
		}

		public void EndUpdate()
		{
			_updateCount = Math.Max(0, _updateCount - 1);
			if (IsHandleCreated && (_updateCount == 0))
			{
				UpdateSurface();
				Invalidate(false);
				ResumeLayout(true);
			}
			else
				ResumeLayout(false);
		}

		#endregion

		#region Designers

		// List of active designers by their node
		private Hashtable _designers = new Hashtable();

		public event GetDesignerHandler OnGetDesigner;

		protected virtual IElementDesigner GetDesigner(object element)
		{
			if (OnGetDesigner != null)
				return OnGetDesigner(element);
			else
				return null;
		}

		private IElementDesigner AddDesigner(object node)
		{
			IElementDesigner designer = GetDesigner(node);
			if (designer != null)
			{
				try
				{
					_designers.Add(node, designer);
					Controls.Add((Control)designer);
				}
				catch
				{
					designer.Dispose();
					throw;
				}
			}
			return designer;
		}

		private void RemoveDesigner(object node)
		{
			IElementDesigner designer = (IElementDesigner)_designers[node];
			if (designer != null)
			{
				if (ActiveControl == (Control)designer)
					ActiveControl = null;
				Controls.Remove((Control)designer);
				_designers.Remove(node);
			}
		}

		#endregion

		#region Painting

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);

			Rectangle bounds = DisplayRectangle;
			Rectangle lineRect;
			TreeNode node;
			int nodeLevel;
			int end = Math.Min(_exposed.Count, _location.Y + _visibleCount);

			using (Pen pen = new Pen(Color.White))
			{
				using (Pen shadowPen = new Pen(Color.Gray))
				{
					using (Brush brush = new SolidBrush(Color.WhiteSmoke))
					{
						for (int i = _location.Y; i < end; i++)
						{
							node = (TreeNode)_exposed[i];
							nodeLevel = node.Level();
							lineRect =
								new Rectangle
								(
									bounds.X + (-_location.X * _indent),
									bounds.Y + ((i - _location.Y) * _rowHeight),
									(nodeLevel * _indent) + (GutterWidth / 2),
									_rowHeight
								);

							// Draw the lines
							if (args.Graphics.IsVisible(lineRect) && (node.Parent != null))
							{
								DrawVertical(args.Graphics, lineRect, pen, shadowPen, node.Parent);
								
								// Draw the horizontal line leading to the node
								int y = lineRect.Y + (lineRect.Height / 2);
								int x = lineRect.X + ((nodeLevel - 1) * _indent) + (_indent / 2);
								args.Graphics.DrawLine(pen, x, y, lineRect.Right, y);
								args.Graphics.DrawLine(shadowPen, x + 1, y + 1, lineRect.Right, y + 1);

								// Draw the vertical line from the parent to the node (and beyond if there are additional siblings
								if (node.Parent.Children.IndexOf(node) < (node.Parent.Children.Count - 1))
									y = lineRect.Bottom;
								args.Graphics.DrawLine(pen, x, lineRect.Top, x, y - 1);
								args.Graphics.DrawLine(shadowPen, x + 1, lineRect.Top, x + 1, y - 1);
							}

							// Draw the stub
							if (node.Children.Count > 0)
							{
								Rectangle rect = new Rectangle((((nodeLevel - _location.X) * _indent) + (_indent / 2) - (GutterWidth / 2)) - 1, lineRect.Bottom - 5, 7, 6);
								if (args.Graphics.IsVisible(rect))
								{
									args.Graphics.DrawLine(shadowPen, new Point(rect.X + 4, rect.Bottom), new Point(rect.Right, rect.Y + 1));
									args.Graphics.FillPolygon(brush, new Point[] {new Point(rect.X, rect.Y), new Point(rect.X + 3, rect.Bottom - 1), new Point(rect.Right - 1, rect.Y)});
								}
							}
						}
					}
				}
			}
		}

		private void DrawVertical(Graphics graphics, Rectangle bounds, Pen pen, Pen shadowPen, TreeNode node)
		{
			if (node.Parent != null)
			{
				if (node.Parent.Children.IndexOf(node) < (node.Parent.Children.Count - 1))	// if there is another sibling after this node
				{
					int x = bounds.X + (node.Parent.Level() * _indent) + (_indent / 2);
					graphics.DrawLine(pen, x, bounds.Top, x, bounds.Bottom);
					graphics.DrawLine(shadowPen, x + 1, bounds.Top, x + 1, bounds.Bottom);
				}
				DrawVertical(graphics, bounds, pen, shadowPen, node.Parent);
			}
		}		

		#endregion

		#region Nodes

		private RootTreeNodes _nodes;
		public TreeNodes Nodes { get { return _nodes; } }

		private ArrayList _exposed = new ArrayList();

		private void ScrollVisible(int index, int deltaY)
		{
			int visibleY = Math.Max(0, index - _location.Y);
			if (visibleY <= _visibleCount)
			{
				Rectangle visible = DisplayRectangle;
				RECT scroll = UnsafeUtilities.RECTFromLTRB(visible.X, visibleY * _rowHeight, visible.Right, Math.Max(visible.Bottom, visible.Bottom + _rowHeight));
				RECT clip = UnsafeUtilities.RECTFromRectangle(visible);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, 0, deltaY * _rowHeight, ref scroll, ref clip, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */ | 4 /* SW_ERASE */);
			}
		}

		internal void AddExposed(TreeNode node, int index)
		{
			_exposed.Insert(index, node);
			UpdateSurface();
			ScrollVisible(index, 1);
			PerformLayout();
		}

		internal void RemoveExposed(int index)
		{
			_exposed.RemoveAt(index);
			UpdateSurface();
			ScrollVisible(index + 1, -1);
			PerformLayout();
		}

		#endregion
	}

	public class TreeNode
	{
		public TreeNode()
		{
			_children = new TreeNodes(this);
		}

		private TreeSurface _surface;
		public TreeSurface Surface { get { return _surface; } }
		internal void SetSurface(TreeSurface service)
		{
			_surface = service;
			foreach (TreeNode child in Children)
				child.SetSurface(service);
		}

		internal TreeNode _parent;
		public TreeNode Parent 
		{ 
			get { return _parent; }
			set 
			{
				if (_parent != value)
				{
					if (_parent != null)
						_parent.Children.Remove(this);
					if (value != null)
						_parent.Children.Add(this);
				}
			}
		}

		private TreeNodes _children;
		public TreeNodes Children { get { return _children; } }

		private object _element;
		public object Element { get { return _element; } set { _element = value; } }

		/// <summary> A node is "Exposed" if it is added to the list of Exposed (visible) nodes of the surface. </summary>
		private bool _isExposed;

		/// <summary> Adds or removes the node from the list of exposed (not necessarily visible) nodes of the surface. </summary>
		/// <remarks> Recurses to the children if the exposed state of this node changes. </remarks>
		internal void UpdateExposed()
		{
			bool shouldBeExposed = _isVisible && (_surface != null);
			TreeNode node = Parent;
			while ((node != null) && shouldBeExposed)
			{
				shouldBeExposed = shouldBeExposed && node.IsVisible && node.IsExpanded;
				node = node.Parent;
			}
			SetExposed(shouldBeExposed);
		}

		/// <summary> Sets the exposed state of the node (and it's children). </summary>
		internal void SetExposed(bool tempValue)
		{
			if (tempValue != _isExposed)	// This should never change when there is no surface
			{
				_isExposed = tempValue;
				if (tempValue)
					_surface.AddExposed(this, ExposedIndex());
				else
					_surface.RemoveExposed(ExposedIndex());
			}
			if (tempValue)
				UpdateChildrenExposed();
			else
				foreach (TreeNode child in Children)
					child.SetExposed(false);
		}

		/// <summary> Updates the exposed state of the child nodes. </summary>
		private void UpdateChildrenExposed()
		{
			foreach (TreeNode child in Children)
				child.UpdateExposed();
		}

		private bool _isVisible = true;
		public bool IsVisible
		{
			get { return _isVisible; }
			set
			{
				if (_isVisible != value)
				{
					_isVisible = value;
					UpdateExposed();
				}
			}
		}

		private bool _isExpanded;
		public bool IsExpanded 
		{ 
			get { return _isExpanded; }
			set 
			{
				if (_isExpanded != value)
					InternalSetExpanded(value, false);
			}
		}

		private void InternalRecurseExpanded(bool isExpanded)
		{
			_isExpanded = isExpanded;
			foreach (TreeNode child in Children)
				child.InternalRecurseExpanded(isExpanded);
		}

		private void InternalSetExpanded(bool isExpanded, bool recursive)
		{
			if (_surface != null)
				_surface.BeginUpdate();
			try
			{
				bool refreshChildren = isExpanded || (isExpanded != _isExpanded);
				if (recursive)
					InternalRecurseExpanded(isExpanded);
				else
					_isExpanded = isExpanded;
				if (refreshChildren)
					UpdateChildrenExposed();
			}
			finally
			{
				if (_surface != null)
					_surface.EndUpdate();
			}
		}

		public void Expand(bool recursive)
		{
			InternalSetExpanded(true, recursive);
		}

		public void Collapse(bool recursive)
		{
			InternalSetExpanded(false, recursive);
		}

		/// <summary> A count of exposed nodes (including this one and any visible children recursively). </summary>
		private int RecursiveCount()
		{
			int result = 0;
			if (_isExposed)
				result++;
			foreach (TreeNode child in Children)
				result += child.RecursiveCount();
			return result;
		}

		/// <summary> The index of the node relative to the visible ordering of all nodes. </summary>
		public int ExposedIndex()
		{
			if (_parent != null)
			{
				int result = _parent.ExposedIndex();
				if (_parent._isExposed)
					result++;
				for (int i = 0; (i < _parent.Children.Count) && (_parent.Children[i] != this); i++)
					result += _parent.Children[i].RecursiveCount();
				return result;
			}
			else
				return 0;
		}

		public int Level()
		{
			int result = 0;
			TreeNode node = _parent;
			while (node != null)
			{
				result++;
				node = node.Parent;
			}
			return result;
		}

		public int Depth()
		{
			int max = 0;
			if (_isExposed)
			{
				int current;
				foreach (TreeNode child in Children)
				{
					current = child.Depth();
					if (current > max)
						max = current;
				}
				max++;
			}
			return max;
		}
	}

	public class TreeNodes : List
	{
		public TreeNodes(TreeNode node)
		{
			_node = node;
		}

		private TreeNode _node;
		public TreeNode Node { get { return _node; } }

		public virtual TreeSurface GetSurface()
		{
			return _node.Surface;
		}

		public new TreeNode this[int index]
		{
			get { return (TreeNode)base[index]; }
			set { base[index] = value; }
		}

		protected override void Adding(object tempValue, int index)
		{
			base.Adding(tempValue, index);
			TreeNode node = (TreeNode)tempValue;
			node.Parent = null;
			node._parent = _node;
			node.SetSurface(GetSurface());
			node.UpdateExposed();
		}

		protected override void Removing(object tempValue, int index)
		{
			TreeNode node = (TreeNode)tempValue;
			node.SetExposed(false);
			node.SetSurface(null);
			node._parent = null;
			base.Removing(tempValue, index);
		}

		public int Depth()
		{
			int max = 0;
			int current;
			foreach (TreeNode child in this)
			{
				current = child.Depth();
				if (current > max)
					max = current;
			}
			return max;
		}
	}

	public class RootTreeNodes : TreeNodes
	{
		public RootTreeNodes(TreeSurface surface) : base(null)
		{
			_surface = surface;
		}

		private TreeSurface _surface;

		public override TreeSurface GetSurface()
		{
			return _surface;
		}
	}
}
