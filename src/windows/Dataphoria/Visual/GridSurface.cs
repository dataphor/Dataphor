/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public class GridSurface : Surface
	{
		public const int CellWidth = 150;
		public const int CellHeight = 78;
		public const int GutterWidth = 30;
		public const int GutterHeight = 30;

		public GridSurface()
		{
			AllowDrop = true;

			SuspendLayout();
			try
			{
				_hScrollBar = new HScrollBar();
				_hScrollBar.SmallChange = 1;
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

				_initialLocation = true;
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		#region Sizing & Scrolling

		private bool _initialLocation;
		private Rectangle _activeRect = Rectangle.Empty;
		private Rectangle _range;
		private HScrollBar _hScrollBar;
		private VScrollBar _vScrollBar;

		private void HScrollBarScrolled(object sender, ScrollEventArgs args)
		{
			NavigateTo(new Point(args.NewValue + _range.X, _activeRect.Y), false);
		}

		private void VScrollBarScrolled(object sender, ScrollEventArgs args)
		{
			NavigateTo(new Point(_activeRect.X, args.NewValue + _range.Y), false);
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
			{
				case Keys.Alt | Keys.Left :
					NavigateTo(new Point(Math.Max(_range.X, _activeRect.X - 1), _activeRect.Y), false);
					break;
				case Keys.Alt | Keys.Up :
					NavigateTo(new Point(_activeRect.X, Math.Max(_range.Y, _activeRect.Y - 1)), false);
					break;
				case Keys.Alt | Keys.Right :
					NavigateTo(new Point(Math.Min(_range.Right - _activeRect.Width, _activeRect.X + 1), _activeRect.Y), false);
					break;
				case Keys.Alt | Keys.Down :
					NavigateTo(new Point(_activeRect.X, Math.Min(_range.Bottom - _activeRect.Height, _activeRect.Y + 1)), false);
					break;
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

		private void NavigateTo(Point location, bool center)
		{
			if (location != _activeRect.Location)
			{
				_initialLocation = false;
				// Translate to center
				if (center)
				{
					location.X -= (_activeRect.Width / 2);
					location.Y -= (_activeRect.Height / 2);
				}
				_activeRect.Location = location;
				UpdateDesigners(true);	// even if layout weren't supressed, there is no guarantee that a layout will be performed
				UpdateScrollBars();
				PerformLayout();
			}
		}

		private Point ClientToGrid(Point location)
		{
			return new Point((location.X / CellWidth) + _activeRect.X, (location.Y / CellHeight) + _activeRect.Y);
		}

		private Point GridToClient(Point location)
		{
			return new Point((location.X - _activeRect.X) * CellWidth, (location.Y - _activeRect.Y) * CellHeight);
		}

		protected override void OnResize(EventArgs args)
		{
			if (IsHandleCreated)
				UpdateSurface();
			base.OnResize(args);
		}

		protected override void OnHandleCreated(EventArgs args)
		{
			base.OnHandleCreated(args);
			UpdateSurface();
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			// Don't call base

			if (_initialLocation)
			{
				UpdateActiveSize();
				UpdateRange();
				_activeRect.Location = 
					new Point
					(
						_range.X + (_range.Width / 2) - (_activeRect.Width / 2), 
						_range.Y + (_range.Height / 2) - (_activeRect.Height / 2)
					);
				UpdateDesigners(true);
				UpdateScrollBars();
			}

			IElementDesigner designer;
			foreach (DictionaryEntry entry in _designers)
			{
				designer = (IElementDesigner)entry.Value;
				designer.Bounds = 
					new Rectangle
					(
						GridToClient(_grid.GetPosition(designer.Element)) + 
							new Size(GutterWidth / 2, GutterHeight / 2),
						new Size(CellWidth - GutterWidth, CellHeight - GutterHeight)
					);
			}
			Size clientSize = ClientSize;
			_vScrollBar.Left = clientSize.Width - _vScrollBar.Width;
			_vScrollBar.Height = clientSize.Height - _hScrollBar.Height;
			_hScrollBar.Top = clientSize.Height - _hScrollBar.Height;
			_hScrollBar.Width = clientSize.Width - _vScrollBar.Width;
		}

		/// <summary> Determines the currently visible portion of the grid. </summary>
		/// <remarks> Should be called any time the ClientSize changes. </remarks>
		private void UpdateActiveSize()
		{
			Size clientSize = ClientSize - new Size(_vScrollBar.Width, _hScrollBar.Height);
			_activeRect.Width = ((clientSize.Width - 1) / CellWidth) + 1;
			_activeRect.Height = ((clientSize.Height - 1) / CellHeight) + 1;
			_hScrollBar.LargeChange = Math.Max(_activeRect.Width, 1);
			_vScrollBar.LargeChange = Math.Max(_activeRect.Height, 1);
		}

		/// <summary> Updates the overall grid range. </summary>
		/// <remarks> Called when the grid extremes, or the Active Size changes. </remarks>
		private void UpdateRange()
		{
			_range = _grid.GetExtremes();
			_range.Inflate(new Size(_activeRect.Width - 1, _activeRect.Height - 1));	// Pad by visible size on all sides
			
			_hScrollBar.Maximum = _range.Width;
			_vScrollBar.Maximum = _range.Height;
		}

		private void UpdateScrollBars()
		{
			_hScrollBar.Value = Constrain(_activeRect.X - _range.X, _hScrollBar.Minimum, _hScrollBar.Maximum);
			_vScrollBar.Value = Constrain(_activeRect.Y - _range.Y, _vScrollBar.Minimum, _vScrollBar.Maximum);
		}

		/// <summary> Updates the visible elements from the grid. </summary>
		/// <remarks>
		///		Called when the active rectangle, or the grid itself changes.
		///		Will only cause a layout if a designer is added or removed and
		///		  ASupressLayout is false.
		/// </remarks>
		private void UpdateDesigners(bool supressLayout)
		{
			object[] displayObjects = (object[])_grid.QueryRect(_activeRect);
			object[] designed = new object[_designers.Count];
			_designers.Keys.CopyTo(designed, 0);	// Copy designers list for enumeration
			bool layoutRequired = false;

			SuspendLayout();
			try
			{
				// Remove designers that are no longer visible
				foreach (object element in designed)
					if (Array.IndexOf(displayObjects, element) < 0)
					{
						RemoveDesigner(element);
						layoutRequired = true;
					}
				
				// Add designers that have come into view
				foreach (object element in displayObjects)
					if (!_designers.Contains(element))
					{
						AddDesigner(element);
						layoutRequired = true;
					}
			}
			finally
			{
				ResumeLayout(!supressLayout && layoutRequired);
			}
		}

		private void UpdateSurface()
		{
			UpdateActiveSize();
			UpdateRange();
			UpdateDesigners(true);
			UpdateScrollBars();
		}

		#endregion

		#region Element Maintenance

		private SparseGrid _grid = new SparseGrid();
		public SparseGrid Grid
		{
			get { return _grid; }
		}

		private Point InternalPlaceElement(object element)
		{
			// TODO: Better element placement (vacancy detection to the SparseGrid FindNearestVacancy(x,y))
			Point point = Point.Empty;
			while (_grid[point] != null)
			{
				if (point.Y > 5)
				{
					point.Y = 0;
					point.X++;
				}
				else
					point.Y++;
			}
			InternalAddElement(element, point);
			return point;
		}

		private void InternalAddElement(object element, Point point)
		{
			Error.DebugAssertFail(_grid[point] == null, "Attempting to add an Element to a non-vacant space");
			_grid[point] = element;
		}

		public void PlaceElement(object element)
		{
			if (_activeRect.Contains(InternalPlaceElement(element)))
				AddDesigner(element);
		}

		public void AddElement(object element, Point point)
		{
			InternalAddElement(element, point);
			if (_activeRect.Contains(point))
				AddDesigner(element);
		}

		private void InternalRemoveElement(object element)
		{
			_grid.Remove(element);
		}

		public void RemoveElement(object element)
		{
			InternalRemoveElement(element);
			RemoveDesigner(element);
		}

		#endregion

		#region Designers

		// List of active designers by their element
		private Hashtable _designers = new Hashtable();

		public event GetDesignerHandler OnGetDesigner;

		protected virtual IElementDesigner GetDesigner(object element)
		{
			if (OnGetDesigner != null)
				return OnGetDesigner(element);
			else
				return null;
		}

		private void AddDesigner(object element)
		{
			IElementDesigner designer = GetDesigner(element);
			if (designer != null)
			{
				try
				{
					_designers.Add(element, designer);
					designer.Disposed += new EventHandler(DesignerDisposed);
					Controls.Add((Control)designer);
				}
				catch
				{
					designer.Dispose();
					throw;
				}
			}
		}

		private void RemoveDesigner(object element)
		{
			IElementDesigner designer = (IElementDesigner)_designers[element];
			if (designer != null)
			{
				Controls.Remove((Control)designer);
				_designers.Remove(element);
				designer.Disposed -= new EventHandler(DesignerDisposed);
			}
		}

		private void DesignerDisposed(object sender, EventArgs args)
		{
			_designers.Remove(sender);
			((IElementDesigner)sender).Disposed -= new EventHandler(DesignerDisposed);
		}

		#endregion

		#region Drag & Drop

		private bool _isDragTarget;
		private Point _dragLocation;	// In grid coords

		private void SetDragState(bool isDragTarget, Point dragLocation)
		{
			if ((isDragTarget != _isDragTarget) || (_isDragTarget && (_dragLocation != dragLocation)))
			{
				if (_isDragTarget)
					Invalidate(GetCellRect(_dragLocation));
				_isDragTarget = isDragTarget;
				if (_isDragTarget)
				{
					_dragLocation = dragLocation;
					Invalidate(GetCellRect(_dragLocation));
				}
			}
		}

		private void GetDragState(ref bool isDragTarget, ref Point dragLocation, DragEventArgs args)
		{
			ElementDesignerData data = args.Data as ElementDesignerData;
			if (data != null)
			{
				object element = 
					_grid
					[
						ClientToGrid(PointToClient(new Point(args.X, args.Y)))
					];
				if (element == null)
				{
					dragLocation = ClientToGrid(PointToClient(new Point(args.X, args.Y)));
					isDragTarget = true;
				}
			}
		}

		protected override void OnDragOver(DragEventArgs args)
		{
			base.OnDragEnter(args);

			bool isDragTarget = false;
			Point dragLocation = Point.Empty;
			GetDragState(ref isDragTarget, ref dragLocation, args);
			if (isDragTarget)
				args.Effect = DragDropEffects.Move;
			else
				args.Effect = DragDropEffects.None;
			SetDragState(isDragTarget, dragLocation);
		}

		protected override void OnDragLeave(EventArgs args)
		{
			base.OnDragLeave(args);

			SetDragState(false, Point.Empty);
		}

		protected override void OnDragDrop(DragEventArgs args)
		{
			base.OnDragDrop(args);

			SetDragState(false, Point.Empty);

			bool isDragTarget = false;
			Point dragLocation = Point.Empty;
			GetDragState(ref isDragTarget, ref dragLocation, args);
			if (isDragTarget)
			{
				ElementDesignerData data = args.Data as ElementDesignerData;
				if (data != null)
				{
					_grid[_grid.GetPosition(data.Element)] = null;
					_grid[dragLocation] = data.Element;
					UpdateRange();
					UpdateDesigners(true);
					UpdateScrollBars();
					PerformLayout();
				}
			}
		}

		#endregion

		#region Painting

		private Rectangle GetCellRect(Point location)
		{
			return new Rectangle(GridToClient(location), new Size(CellWidth, CellHeight));
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			if (_isDragTarget)
			{
				Rectangle rect = GetCellRect(_dragLocation);
				if (args.Graphics.IsVisible(rect))
				{
					rect.Inflate(-(GutterWidth / 2), -(GutterHeight / 2));
					using (SolidBrush brush = new SolidBrush(Color.FromArgb(70, Color.Gray)))
					{
						args.Graphics.FillRectangle(brush, rect);
					}
					using (Pen pen = new Pen(Color.Black))
					{
						args.Graphics.DrawRectangle(pen, rect);
					}
				}
			}
		}

		#endregion
	}

	public delegate IElementDesigner GetDesignerHandler(object AElement);

	/*
		The sparse grid must:
			-Perform 2-dimensional range queries quickly (without O(n) search)
			-Provide a virtual (sparse) grid, with little to no penalty based on
			 the location of the nodes relative to the origin of the cartesian plane
			-Quickly find the position of a given node.
			
		This is accomplished by:
			-Providing a 2-dimensional tree for positional queries.
			-Provide a secondary index to find nodes by object.
	*/
	public class SparseGrid
	{
		/// <summary> Get or set the element at a specific point. </summary>
		public object this[Point point]
		{
			get 
			{
				KDTree.Node node = _tree.Query(point);
				if (node != null)
					return node.Object;
				else
					return null;
			}
			set 
			{
				// Remove any item at this spot
				KDTree.Node node = _tree.Query(point);
				if (node != null)
				{
					_tree.Remove(node);
					_nodes.Remove(node.Object);
				}
								
				// Add the new item at the spot
				if (value != null)
				{
					node = new KDTree.Node(point, value);
					_tree.Add(node);
					_nodes.Add(value, node);
				}
			}
		}

		/// <summary> Get all objects within a given range of the plane. </summary>
		public object[] QueryRect(Rectangle rect)
		{
			ArrayList nodes = _tree.Query(rect);
			object[] result = new object[nodes.Count];
			for (int i = 0; i < result.Length; i++)
				result[i] = ((KDTree.Node)nodes[i]).Object;
			return result;
		}

		/// <summary> Get the position of a given object. </summary>
		public Point GetPosition(object element)
		{
			return ((KDTree.Node)_nodes[element]).Location;
		}

		/// <summary> Removes the object at a given point. </summary>
		public void Remove(object element)
		{
			KDTree.Node node = (KDTree.Node)_nodes[element];
			_tree.Remove(node);
			_nodes.Remove(element);
		}

		private KDTree _tree = new KDTree();
		private Hashtable _nodes = new Hashtable(10);

		public Rectangle GetExtremes()
		{
			return _tree.GetExtremes();
		}
	}

	/// <summary> A 2-Dimensional Tree. </summary>
	/// <remarks> This tree is bucketless so no two nodes can occupy the same point. </remarks>
	public class KDTree
	{
		public const int Dimensions = 2;

		public bool Add(Node node)
		{
			if (_root == null)
			{
				_root = node;
				return true;
			}
			else
			{
				Node localNode = _root;
				int dimension = 0;
				for (;;)
				{
					if (node._location[dimension] < localNode._location[dimension])
						if (localNode._left == null)
						{
							localNode._left = node;
							return true;
						}
						else
							localNode = localNode._left;
					else if (node._location[dimension] >= localNode._location[dimension])
					{
						if ((node._location[0] == localNode._location[0]) && (node._location[1] == localNode._location[1]))
							return false;
						else
							if (localNode._right == null)
							{
								localNode._right = node;
								return true;
							}
							else
								localNode = localNode._right;
					}
					dimension = (dimension + 1) % Dimensions;
				}
			}
		}

		/// <summary> Re-add broken branch of the tree. </summary>
		private void Readd(Node node)
		{
			if (node != null)
			{
				Node left = node._left;
				node._left = null;
				Node right = node._right;
				node._right = null;
				Add(node);
				Readd(left);
				Readd(right);
			}
		}

		/// <remarks> If the node is null or not found, nothing happens. </remarks>
		public void Remove(Node node)
		{
			if (node != null)
			{
				if (_root == node)
				{
					_root = null;
					Readd(node._left);
					Readd(node._right);
				}
				else
				{
					Node localNode = _root;
					int dimension = 0;
					while (localNode != null)
					{
						if (node == localNode._left)
						{
							localNode._left = null;
							Readd(node._left);
							Readd(node._right);
							return;
						}
						if (node == localNode._right)
						{
							localNode._right = null;
							Readd(node._left);
							Readd(node._right);
							return;
						}
						if (node._location[dimension] < localNode._location[dimension])
							localNode = localNode._left;
						else
							localNode = localNode._right;
						dimension = (dimension + 1) % Dimensions;
					}
				}
			}
		}

		public Node Query(Point point)
		{
			if (_root == null)
				return null;
			else
			{
				Node node = _root;
				int[] localPoint = new int[2] {point.X, point.Y};
				int dimension = 0;
				while (node != null)
				{
					if (localPoint[dimension] < node._location[dimension])
						node = node._left;
					else if (localPoint[dimension] >= node._location[dimension])
					{
						if ((localPoint[0] == node._location[0]) && (localPoint[1] == node._location[1]))
							return node;
						node = node._right;
					}
					dimension = (dimension + 1) % Dimensions;
				}
				return null;
			}
		}

		/// <remarks> Area must not be inverted. </remarks>
		public ArrayList Query(Rectangle rect)
		{
			ArrayList result = new ArrayList();
			if (_root != null)
			{
				Query
				(
					_root,
					result,
					new int[2] {rect.X, rect.Y},
					new int[2] {rect.Right - 1, rect.Bottom - 1},
					0
				);
			}
			return result;
		}

		private void Query(Node node, ArrayList list, int[] low, int[] high, int dimension)
		{
			if (node != null)
			{
				int newDimension = (dimension + 1) % Dimensions;
				if		// current node is in range
				(
					(low[dimension] <= node._location[dimension]) && 
					(high[dimension] >= node._location[dimension])
				)
				{
					if 
					(
						(low[newDimension] <= node._location[newDimension]) &&
						(high[newDimension] >= node._location[newDimension])
					)
						list.Add(node);
					Query(node._left, list, low, high, newDimension);
					Query(node._right, list, low, high, newDimension);
				}
				else if (high[dimension] < node._location[dimension])
					Query(node._left, list, low, high, newDimension);
				else
					Query(node._right, list, low, high, newDimension);
			}
		}

		public Rectangle GetExtremes()
		{
			if (_root != null)
			{
				Rectangle result = Rectangle.Empty;
				
				int i = Int32.MaxValue;
				ChaseMin(_root, 0, 0, ref i);
				result.X = i;

				i = Int32.MaxValue;
				ChaseMin(_root, 1, 0, ref i);
				result.Y = i;

				i = Int32.MinValue;
				ChaseMax(_root, 0, 0, ref i);
				result.Width = (i - result.Left) + 1;

				i = Int32.MinValue;
				ChaseMax(_root, 1, 0, ref i);
				result.Height = (i - result.Top) + 1;

				return result;
			}
			else
				return Rectangle.Empty;
		}
	
		internal void ChaseMin(Node node, int dimension, int level, ref int min)
		{
			if (node != null)
			{
				if (node._location[dimension] < min)
					min = node._location[dimension];
				if ((level % Dimensions) == dimension)
					ChaseMin(node._left, dimension, level + 1, ref min);
				else
				{
					ChaseMin(node._left, dimension, level + 1, ref min);
					ChaseMin(node._right, dimension, level + 1, ref min);
				}
			}
		}

		internal void ChaseMax(Node node, int dimension, int level, ref int max)
		{
			if (node != null)
			{
				if (node._location[dimension] > max)
					max = node._location[dimension];
				if ((level % Dimensions) == dimension)
					ChaseMax(node._right, dimension, level + 1, ref max);
				else
				{
					ChaseMax(node._left, dimension, level + 1, ref max);
					ChaseMax(node._right, dimension, level + 1, ref max);
				}
			}
		}

		private Node _root;

		public class Node
		{
			public Node(Point location, object element)
			{
				_location = new int[2] {location.X, location.Y};
				_object = element;
			}

			internal int[] _location;

			public Point Location
			{
				get { return new Point(_location[0], _location[1]); }
			}

			private object _object;
			public object Object
			{
				get { return _object; }
			}

			internal Node _left;
			internal Node _right;

		}

	}
}
