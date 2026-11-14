/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public class DatabaseSurface : Surface
	{
		public const int CCellWidth = 150;
		public const int CCellHeight = 78;
		public const int CGutterWidth = 30;
		public const int CGutterHeight = 30;

		public DatabaseSurface(CatalogSchema ACatalog, DesignerControl ADesigner) : base(ADesigner)
		{
			FCatalog = ACatalog;
			AllowDrop = true;

			SuspendLayout();
			try
			{
				FHScrollBar = new HScrollBar();
				FHScrollBar.SmallChange = 1;
				FHScrollBar.Minimum = 0;
				FHScrollBar.TabStop = false;
				FHScrollBar.Scroll += new ScrollEventHandler(HScrollBarScrolled);
				Controls.Add(FHScrollBar);

				FVScrollBar = new VScrollBar();
				FVScrollBar.SmallChange = 1;
				FVScrollBar.Minimum = 0;
				FVScrollBar.TabStop = false;
				FVScrollBar.Scroll += new ScrollEventHandler(VScrollBarScrolled);
				Controls.Add(FVScrollBar);

				foreach (ObjectSchema LObject in FCatalog)
					if (LObject != null)
						InternalAddObject(LObject);

				FInitialLocation = true;
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		private CatalogSchema FCatalog;
		public CatalogSchema Catalog
		{
			get { return FCatalog; }
		}

		private SparseGrid FGrid = new SparseGrid();
		public SparseGrid Grid
		{
			get { return FGrid; }
		}

		// Sizing + location

		private bool FInitialLocation;
		private Rectangle FActiveRect = Rectangle.Empty;
		private Rectangle FRange;
		private HScrollBar FHScrollBar;
		private VScrollBar FVScrollBar;

		private void HScrollBarScrolled(object ASender, ScrollEventArgs AArgs)
		{
			NavigateTo(new Point(AArgs.NewValue + FRange.X, FActiveRect.Y), false);
		}

		private void VScrollBarScrolled(object ASender, ScrollEventArgs AArgs)
		{
			NavigateTo(new Point(FActiveRect.X, AArgs.NewValue + FRange.Y), false);
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if ((AKey & Keys.Modifiers) == Keys.Alt)
			{
				switch (AKey & Keys.KeyCode)
				{
					case Keys.Left :
						NavigateTo(new Point(Math.Max(FRange.X, FActiveRect.X - 1), FActiveRect.Y), false);
						break;
					case Keys.Up :
						NavigateTo(new Point(FActiveRect.X, Math.Max(FRange.Y, FActiveRect.Y - 1)), false);
						break;
					case Keys.Right :
						NavigateTo(new Point(Math.Min(FRange.Right - FActiveRect.Width, FActiveRect.X + 1), FActiveRect.Y), false);
						break;
					case Keys.Down :
						NavigateTo(new Point(FActiveRect.X, Math.Min(FRange.Bottom - FActiveRect.Height, FActiveRect.Y + 1)), false);
						break;
					default :
						goto regular;
				}
				return true;
			}
			regular:
			return base.ProcessDialogKey(AKey);
		}

		private static int Constrain(int AValue, int AMin, int AMax)
		{
			if (AValue > AMax)
				AValue = AMax;
			if (AValue < AMin)
				AValue = AMin;
			return AValue;
		}

		private void NavigateTo(Point ALocation, bool ACenter)
		{
			if (ALocation != FActiveRect.Location)
			{
				FInitialLocation = false;
				// Translate to center
				if (ACenter)
				{
					ALocation.X -= (FActiveRect.Width / 2);
					ALocation.Y -= (FActiveRect.Height / 2);
				}
				FActiveRect.Location = ALocation;
				UpdateDisplay(true);	// even if layout weren't supressed, there is no guarantee that a layout will be performed
				UpdateScrollBars();
				PerformLayout();
			}
		}

		private Point ClientToGrid(Point ALocation)
		{
			return new Point((ALocation.X / CCellWidth) + FActiveRect.X, (ALocation.Y / CCellHeight) + FActiveRect.Y);
		}

		private Point GridToClient(Point ALocation)
		{
			return new Point((ALocation.X - FActiveRect.X) * CCellWidth, (ALocation.Y - FActiveRect.Y) * CCellHeight);
		}

		protected override void OnResize(EventArgs AArgs)
		{
			base.OnResize(AArgs);
			if (IsHandleCreated)
				UpdateSurface();
		}

		protected override void OnHandleCreated(EventArgs AArgs)
		{
			base.OnHandleCreated(AArgs);
			UpdateSurface();
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			// Don't call base

			if (IsHandleCreated)
			{
				if (FInitialLocation)
				{
					UpdateActiveSize();
					UpdateRange();
					FActiveRect.Location = 
						new Point
						(
							FRange.X + (FRange.Width / 2) - (FActiveRect.Width / 2), 
							FRange.Y + (FRange.Height / 2) - (FActiveRect.Height / 2)
						);
					UpdateDisplay(true);
					UpdateScrollBars();
				}

				foreach (ObjectDesigner LDesigner in FDesigners.Values)
				{
					LDesigner.Bounds = 
						new Rectangle
						(
							GridToClient(FGrid.GetPosition(LDesigner.Object)) + 
								new Size(CGutterWidth / 2, CGutterHeight / 2),
							new Size(CCellWidth - CGutterWidth, CCellHeight - CGutterHeight)
						);
				}
				Size LClientSize = ClientSize;
				FVScrollBar.Left = LClientSize.Width - FVScrollBar.Width;
				FVScrollBar.Height = LClientSize.Height - FHScrollBar.Height;
				FHScrollBar.Top = LClientSize.Height - FHScrollBar.Height;
				FHScrollBar.Width = LClientSize.Width - FVScrollBar.Width;
			}
		}

		/// <summary> Determines the currently visible portion of the grid. </summary>
		/// <remarks> Should be called any time the ClientSize changes. </remarks>
		private void UpdateActiveSize()
		{
			Size LClientSize = ClientSize - new Size(FVScrollBar.Width, FHScrollBar.Height);
			FActiveRect.Width = ((LClientSize.Width - 1) / CCellWidth) + 1;
			FActiveRect.Height = ((LClientSize.Height - 1) / CCellHeight) + 1;
			FHScrollBar.LargeChange = Math.Max(FActiveRect.Width, 1);
			FVScrollBar.LargeChange = Math.Max(FActiveRect.Height, 1);
		}

		/// <summary> Updates the overall grid range. </summary>
		/// <remarks> Called when the grid extremes, or the Active Size changes. </remarks>
		private void UpdateRange()
		{
			FRange = FGrid.GetExtremes();
			FRange.Inflate(new Size(FActiveRect.Width - 1, FActiveRect.Height - 1));	// Pad by visible size on all sides
			
			FHScrollBar.Maximum = FRange.Width;
			FVScrollBar.Maximum = FRange.Height;
		}

		private void UpdateScrollBars()
		{
			FHScrollBar.Value = Constrain(FActiveRect.X - FRange.X, FHScrollBar.Minimum, FHScrollBar.Maximum);
			FVScrollBar.Value = Constrain(FActiveRect.Y - FRange.Y, FVScrollBar.Minimum, FVScrollBar.Maximum);
		}

		/// <summary> Updates the visible elements from the grid. </summary>
		/// <remarks>
		///		Called when the active rectangle, or the grid itself changes.
		///		Will only cause a layout if a designer is added or removed and
		///		  ASupressLayout is false.
		/// </remarks>
		private void UpdateDisplay(bool ASupressLayout)
		{
			object[] LDisplayObjects = (object[])FGrid.QueryRect(FActiveRect);
			object[] LDesigned = new object[FDesigners.Count];
			FDesigners.Keys.CopyTo(LDesigned, 0);	// Copy designers list for enumeration
			bool LLayoutRequired = false;

			SuspendLayout();
			try
			{
				// Remove designers that are no longer visible
				foreach (ObjectSchema LObject in LDesigned)
					if (!Sets.In(LDisplayObjects, LObject))
					{
						RemoveDesigner(LObject);
						LLayoutRequired = true;
					}
				
				// Add designers that have come into view
				foreach (ObjectSchema LObject in LDisplayObjects)
					if (!FDesigners.Contains(LObject))
					{
						AddDesigner(LObject);
						LLayoutRequired = true;
					}
			}
			finally
			{
				ResumeLayout(!ASupressLayout && LLayoutRequired);
			}
		}

		private void UpdateSurface()
		{
			UpdateActiveSize();
			UpdateRange();
			UpdateDisplay(true);
			UpdateScrollBars();
		}

		private Point InternalAddObject(ObjectSchema AObject)
		{
			// TODO: Better schema object placement (vacancy detection to the SparseGrid FindNearestVacancy(x,y))
			Point LPoint = Point.Empty;
			while (FGrid[LPoint] != null)
			{
				if (LPoint.Y > 5)
				{
					LPoint.Y = 0;
					LPoint.X++;
				}
				else
					LPoint.Y++;
			}
			FGrid[LPoint] = AObject;
			AObject.OnDeleted += new SchemaHandler(ObjectDeleted);
			return LPoint;
		}

		public void AddObject(ObjectSchema AObject)
		{
			if (FActiveRect.Contains(InternalAddObject(AObject)))
				AddDesigner(AObject);
		}

		private void InternalRemoveObject(ObjectSchema AObject)
		{
			AObject.OnDeleted -= new SchemaHandler(ObjectDeleted);
			FGrid.Remove(AObject);
		}

		public void RemoveObject(ObjectSchema AObject)
		{
			InternalRemoveObject(AObject);
			RemoveDesigner(AObject);
		}

		private void ObjectDeleted(BaseSchema ASchema)
		{
			InternalRemoveObject((ObjectSchema)ASchema);	// The designer will go away on it's own
		}

		// List of active designers by their schema object
		private Hashtable FDesigners = new Hashtable();

		private void AddDesigner(ObjectSchema AObject)
		{
			ObjectDesigner LDesigner;
			if (AObject is TableSchema)
				LDesigner = new TableDesigner(AObject, DesignerControl);
			else if (AObject is ViewSchema)
				LDesigner = new ViewDesigner(AObject, DesignerControl);
			else if (AObject is OperatorSchema)
				LDesigner = new OperatorDesigner(AObject, DesignerControl);
			else
				return;
			try
			{
				FDesigners.Add(AObject, LDesigner);
				LDesigner.Disposed += new EventHandler(DesignerDisposed);
				Controls.Add(LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void RemoveDesigner(ObjectSchema AObject)
		{
			ObjectDesigner LDesigner = (ObjectDesigner)FDesigners[AObject];
			if (LDesigner != null)
			{
				Controls.Remove(LDesigner);
				FDesigners.Remove(AObject);
				LDesigner.Disposed -= new EventHandler(DesignerDisposed);
			}
		}

		private void DesignerDisposed(object ASender, EventArgs AArgs)
		{
			FDesigners.Remove(ASender);
			((Control)ASender).Disposed -= new EventHandler(DesignerDisposed);
		}

		public override void Details(Control AControl)
		{
			if (AControl == null)
				AControl = ActiveControl;
			if ((AControl != null) && (AControl is ObjectDesigner))
				((ObjectDesigner)AControl).Details();
		}

		// Drag / Drop

		private bool FIsDragTarget;
		private Point FDragLocation;	// In grid coords

		private void SetDragState(bool AIsDragTarget, Point ADragLocation)
		{
			if ((AIsDragTarget != FIsDragTarget) || (FIsDragTarget && (FDragLocation != ADragLocation)))
			{
				if (FIsDragTarget)
					Invalidate(GetCellRect(FDragLocation));
				FIsDragTarget = AIsDragTarget;
				if (FIsDragTarget)
				{
					FDragLocation = ADragLocation;
					Invalidate(GetCellRect(FDragLocation));
				}
			}
		}

		private void GetDragState(ref bool AIsDragTarget, ref Point ADragLocation, DragEventArgs AArgs)
		{
			ObjectDesignerData LData = AArgs.Data as ObjectDesignerData;
			if (LData != null)
			{
				ObjectSchema LObject = (ObjectSchema)FGrid
				[
					ClientToGrid(PointToClient(new Point(AArgs.X, AArgs.Y)))
				];
				if (LObject == null)
				{
					ADragLocation = ClientToGrid(PointToClient(new Point(AArgs.X, AArgs.Y)));
					AIsDragTarget = true;
				}
			}
		}

		protected override void OnDragOver(DragEventArgs AArgs)
		{
			base.OnDragEnter(AArgs);

			bool LIsDragTarget = false;
			Point LDragLocation = Point.Empty;
			GetDragState(ref LIsDragTarget, ref LDragLocation, AArgs);
			if (LIsDragTarget)
				AArgs.Effect = DragDropEffects.Move;
			else
				AArgs.Effect = DragDropEffects.None;
			SetDragState(LIsDragTarget, LDragLocation);
		}

		protected override void OnDragLeave(EventArgs AArgs)
		{
			base.OnDragLeave(AArgs);

			SetDragState(false, Point.Empty);
		}

		protected override void OnDragDrop(DragEventArgs AArgs)
		{
			base.OnDragDrop(AArgs);

			SetDragState(false, Point.Empty);

			bool LIsDragTarget = false;
			Point LDragLocation = Point.Empty;
			GetDragState(ref LIsDragTarget, ref LDragLocation, AArgs);
			if (LIsDragTarget)
			{
				ObjectDesignerData LData = AArgs.Data as ObjectDesignerData;
				if (LData != null)
				{
					FGrid[FGrid.GetPosition(LData.Object)] = null;
					FGrid[LDragLocation] = LData.Object;
					UpdateRange();
					UpdateDisplay(true);
					UpdateScrollBars();
					PerformLayout();
				}
			}
		}

		// Painting

		private Rectangle GetCellRect(Point ALocation)
		{
			return new Rectangle(GridToClient(ALocation), new Size(CCellWidth, CCellHeight));
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			if (FIsDragTarget)
			{
				Rectangle LRect = GetCellRect(FDragLocation);
				if (AArgs.Graphics.IsVisible(LRect))
				{
					LRect.Inflate(-(CGutterWidth / 2), -(CGutterHeight / 2));
					using (SolidBrush LBrush = new SolidBrush(Color.FromArgb(70, Color.Gray)))
					{
						AArgs.Graphics.FillRectangle(LBrush, LRect);
					}
					using (Pen LPen = new Pen(Color.Black))
					{
						AArgs.Graphics.DrawRectangle(LPen, LRect);
					}
				}
			}
		}
	}

	/*
		The object grid must:
			-Perform 2-dimensional range queries quickly (without order(n) search)
			-Provide a virtual (sparse) grid, with little to no penalty based on
			 the location of the nodes relative to the origin of the cartesian plane
			-Quickly find the position of a given node.
			
		This is accomplished by:
			-Providing a 2-dimensional tree for positional queries.
			-Provide a secondary index to find nodes by object
	*/
	public class SparseGrid
	{
		/// <summary> Get or set the object at a specific point. </summary>
		public object this[Point APoint]
		{
			get 
			{
				KDTree.Node LNode = FTree.Query(APoint);
				if (LNode != null)
					return LNode.Object;
				else
					return null;
			}
			set 
			{
				// Remove any item at this spot
				KDTree.Node LNode = FTree.Query(APoint);
				if (LNode != null)
				{
					FTree.Remove(LNode);
					FNodes.Remove(LNode.Object);
				}
								
				// Add the new item at the spot
				if (value != null)
				{
					LNode = new KDTree.Node(APoint, value);
					FTree.Add(LNode);
					FNodes.Add(value, LNode);
				}
			}
		}

		/// <summary> Get all objects within a given range of the plane. </summary>
		public object[] QueryRect(Rectangle ARect)
		{
			ArrayList LNodes = FTree.Query(ARect);
			object[] LResult = new object[LNodes.Count];
			for (int i = 0; i < LResult.Length; i++)
				LResult[i] = ((KDTree.Node)LNodes[i]).Object;
			return LResult;
		}

		/// <summary> Get the position of a given object. </summary>
		public Point GetPosition(object AObject)
		{
			return ((KDTree.Node)FNodes[AObject]).Location;
		}

		/// <summary> Removes the object at a given point. </summary>
		public void Remove(object AObject)
		{
			KDTree.Node LNode = (KDTree.Node)FNodes[AObject];
			FTree.Remove(LNode);
			FNodes.Remove(AObject);
		}

		private KDTree FTree = new KDTree();
		private Hashtable FNodes = new Hashtable(10);

		public Rectangle GetExtremes()
		{
			return FTree.GetExtremes();
		}
	}

	/// <summary> A 2-Dimensional Tree. </summary>
	/// <remarks> This tree is bucketless so no two nodes can occupy the same point. </remarks>
	public class KDTree
	{
		public const int CDimensions = 2;

		public bool Add(Node ANode)
		{
			if (FRoot == null)
			{
				FRoot = ANode;
				return true;
			}
			else
			{
				Node LNode = FRoot;
				int LDimension = 0;
				for (;;)
				{
					if (ANode.FLocation[LDimension] < LNode.FLocation[LDimension])
						if (LNode.FLeft == null)
						{
							LNode.FLeft = ANode;
							return true;
						}
						else
							LNode = LNode.FLeft;
					else if (ANode.FLocation[LDimension] >= LNode.FLocation[LDimension])
					{
						if ((ANode.FLocation[0] == LNode.FLocation[0]) && (ANode.FLocation[1] == LNode.FLocation[1]))
							return false;
						else
							if (LNode.FRight == null)
							{
								LNode.FRight = ANode;
								return true;
							}
							else
								LNode = LNode.FRight;
					}
					LDimension = (LDimension + 1) % CDimensions;
				}
			}
		}

		/// <summary> Re-add broken branch of the tree. </summary>
		private void Readd(Node ANode)
		{
			if (ANode != null)
			{
				Node LLeft = ANode.FLeft;
				ANode.FLeft = null;
				Node LRight = ANode.FRight;
				ANode.FRight = null;
				Add(ANode);
				Readd(LLeft);
				Readd(LRight);
			}
		}

		/// <remarks> If the node is null or not found, nothing happens. </remarks>
		public void Remove(Node ANode)
		{
			if (ANode != null)
			{
				if (FRoot == ANode)
				{
					FRoot = null;
					Readd(ANode.FLeft);
					Readd(ANode.FRight);
				}
				else
				{
					Node LNode = FRoot;
					int LDimension = 0;
					while (LNode != null)
					{
						if (ANode == LNode.FLeft)
						{
							LNode.FLeft = null;
							Readd(ANode.FLeft);
							Readd(ANode.FRight);
							return;
						}
						if (ANode == LNode.FRight)
						{
							LNode.FRight = null;
							Readd(ANode.FLeft);
							Readd(ANode.FRight);
							return;
						}
						if (ANode.FLocation[LDimension] < LNode.FLocation[LDimension])
							LNode = LNode.FLeft;
						else
							LNode = LNode.FRight;
						LDimension = (LDimension + 1) % CDimensions;
					}
				}
			}
		}

		public Node Query(Point APoint)
		{
			if (FRoot == null)
				return null;
			else
			{
				Node LNode = FRoot;
				int[] LPoint = new int[2] {APoint.X, APoint.Y};
				int LDimension = 0;
				while (LNode != null)
				{
					if (LPoint[LDimension] < LNode.FLocation[LDimension])
						LNode = LNode.FLeft;
					else if (LPoint[LDimension] >= LNode.FLocation[LDimension])
					{
						if ((LPoint[0] == LNode.FLocation[0]) && (LPoint[1] == LNode.FLocation[1]))
							return LNode;
						LNode = LNode.FRight;
					}
					LDimension = (LDimension + 1) % CDimensions;
				}
				return null;
			}
		}

		/// <remarks> Area must not be inverted. </remarks>
		public ArrayList Query(Rectangle ARect)
		{
			ArrayList LResult = new ArrayList();
			if (FRoot != null)
			{
				Query
				(
					FRoot,
					LResult,
					new int[2] {ARect.X, ARect.Y},
					new int[2] {ARect.Right - 1, ARect.Bottom - 1},
					0
				);
			}
			return LResult;
		}

		private void Query(Node ANode, ArrayList AList, int[] ALow, int[] AHigh, int ADimension)
		{
			if (ANode != null)
			{
				int LNewDimension = (ADimension + 1) % CDimensions;
				if		// current node is in range
				(
					(ALow[ADimension] <= ANode.FLocation[ADimension]) && 
					(AHigh[ADimension] >= ANode.FLocation[ADimension])
				)
				{
					if 
					(
						(ALow[LNewDimension] <= ANode.FLocation[LNewDimension]) &&
						(AHigh[LNewDimension] >= ANode.FLocation[LNewDimension])
					)
						AList.Add(ANode);
					Query(ANode.FLeft, AList, ALow, AHigh, LNewDimension);
					Query(ANode.FRight, AList, ALow, AHigh, LNewDimension);
				}
				else if (AHigh[ADimension] < ANode.FLocation[ADimension])
					Query(ANode.FLeft, AList, ALow, AHigh, LNewDimension);
				else
					Query(ANode.FRight, AList, ALow, AHigh, LNewDimension);
			}
		}

		public Rectangle GetExtremes()
		{
			if (FRoot != null)
			{
				Rectangle LResult = Rectangle.Empty;
				
				int i = Int32.MaxValue;
				ChaseMin(FRoot, 0, 0, ref i);
				LResult.X = i;

				i = Int32.MaxValue;
				ChaseMin(FRoot, 1, 0, ref i);
				LResult.Y = i;

				i = Int32.MinValue;
				ChaseMax(FRoot, 0, 0, ref i);
				LResult.Width = (i - LResult.Left) + 1;

				i = Int32.MinValue;
				ChaseMax(FRoot, 1, 0, ref i);
				LResult.Height = (i - LResult.Top) + 1;

				return LResult;
			}
			else
				return Rectangle.Empty;
		}
	
		internal void ChaseMin(Node ANode, int ADimension, int ALevel, ref int AMin)
		{
			if (ANode != null)
			{
				if (ANode.FLocation[ADimension] < AMin)
					AMin = ANode.FLocation[ADimension];
				if ((ALevel % CDimensions) == ADimension)
					ChaseMin(ANode.FLeft, ADimension, ALevel + 1, ref AMin);
				else
				{
					ChaseMin(ANode.FLeft, ADimension, ALevel + 1, ref AMin);
					ChaseMin(ANode.FRight, ADimension, ALevel + 1, ref AMin);
				}
			}
		}

		internal void ChaseMax(Node ANode, int ADimension, int ALevel, ref int AMax)
		{
			if (ANode != null)
			{
				if (ANode.FLocation[ADimension] > AMax)
					AMax = ANode.FLocation[ADimension];
				if ((ALevel % CDimensions) == ADimension)
					ChaseMax(ANode.FRight, ADimension, ALevel + 1, ref AMax);
				else
				{
					ChaseMax(ANode.FLeft, ADimension, ALevel + 1, ref AMax);
					ChaseMax(ANode.FRight, ADimension, ALevel + 1, ref AMax);
				}
			}
		}

		private Node FRoot;

		public class Node
		{
			public Node(Point ALocation, object AObject)
			{
				FLocation = new int[2] {ALocation.X, ALocation.Y};
				FObject = AObject;
			}

			internal int[] FLocation;

			public Point Location
			{
				get { return new Point(FLocation[0], FLocation[1]); }
			}

			private object FObject;
			public object Object
			{
				get { return FObject; }
			}

			internal Node FLeft;
			internal Node FRight;

		}

	}

	public sealed class Sets
	{
		public static bool In(object[] ASearchList, object AValue)
		{
			for (int i = 0; i < ASearchList.Length; i++)
				if (ASearchList[i] == AValue)
					return true;
			return false;
		}
	}
}
