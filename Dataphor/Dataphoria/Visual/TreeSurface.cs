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
		public const int CGutterWidth = 4;
		public const int CGutterHeight = 4;

		public TreeSurface()
		{
			SetStyle(ControlStyles.ResizeRedraw, false);
			FNodes = new RootTreeNodes(this);
			SuspendLayout();
			try
			{
				FHScrollBar = new HScrollBar();
				FHScrollBar.SmallChange = 1;
				FHScrollBar.LargeChange = 1;
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
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		#region Cosmetic Properties

		private int FRowHeight = 40;
		public int RowHeight
		{
			get { return FRowHeight; }
			set
			{
				if (value < 1)
					value = 1;
				if (FRowHeight != value)
				{
					FRowHeight = value;
					UpdateSurface();
					PerformLayout();
				}
			}
		}

		private int FIndent = 40;
		public int Indent
		{
			get { return FIndent; }
			set
			{
				if (value < 1)
					value = 1;
				if (FIndent != value)
				{
					FIndent = value;
					UpdateSurface();
					PerformLayout();
				}
			}
		}

		#endregion

		#region Scrolling & Layout

		private HScrollBar FHScrollBar;
		private VScrollBar FVScrollBar;
		private int FVisibleCount;

		private void HScrollBarScrolled(object ASender, ScrollEventArgs AArgs)
		{
			NavigateTo(new Point(AArgs.NewValue, FLocation.Y));
		}

		private void VScrollBarScrolled(object ASender, ScrollEventArgs AArgs)
		{
			SetVerticalScroll(AArgs.NewValue);
		}

		private void SetVerticalScroll(int AValue)
		{
			AValue = Math.Max(0, Math.Min(FExposed.Count, AValue + (FVisibleCount - 1)) - (FVisibleCount - 1));
			int LX = MinDisplayLevel(AValue);
			if (LX == Int32.MaxValue)
				LX = FLocation.X;
			NavigateTo(new Point(LX, AValue));
		}

		/// <summary> Returns the level of the shallowest visible node. </summary>
		private int MinDisplayLevel(int ALocationY)
		{
			int LMin = Int32.MaxValue;
			int LCurrent;
			int LEnd = Math.Min(FExposed.Count, ALocationY + FVisibleCount);
			for (int i = ALocationY; i < LEnd; i++) 
			{
				LCurrent = ((TreeNode)FExposed[i]).Level();
				if (LCurrent < LMin)
					LMin = LCurrent;
			}
			return LMin;
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			switch (AKey)
			{
				case Keys.Left : NavigateTo(new Point(FLocation.X - 1, FLocation.Y)); break;
				case Keys.Up : SetVerticalScroll(FLocation.Y - 1); break;
				case Keys.Right : NavigateTo(new Point(FLocation.X + 1, FLocation.Y)); break;
				case Keys.Down : SetVerticalScroll(FLocation.Y + 1); break;
				case Keys.PageUp : SetVerticalScroll(FLocation.Y - FVisibleCount); break;
				case Keys.PageDown : SetVerticalScroll(FLocation.Y + FVisibleCount); break;
				case Keys.Home : NavigateTo(new Point(0, FLocation.Y)); break;
				case Keys.End : NavigateTo(new Point(FHScrollBar.Maximum, FLocation.Y)); break;
				default :
					return base.ProcessDialogKey(AKey);
			}
			return true;
		}

		private static int Constrain(int AValue, int AMin, int AMax)
		{
			if (AValue > AMax)
				AValue = AMax;
			if (AValue < AMin)
				AValue = AMin;
			return AValue;
		}

		private Point FLocation;

		private void NavigateTo(Point ALocation)
		{
			ALocation.X = Constrain(ALocation.X, FHScrollBar.Minimum, FHScrollBar.Maximum);
			ALocation.Y = Math.Max(0, Math.Min(FExposed.Count, ALocation.Y + (FVisibleCount - 1)) - (FVisibleCount - 1));
			if (ALocation != FLocation)
			{
				Point LDelta = new Point((FLocation.X - ALocation.X) * FIndent, (FLocation.Y - ALocation.Y) * FRowHeight);

				FLocation = ALocation;
				
				FHScrollBar.Value = FLocation.X;
				FVScrollBar.Value = FLocation.Y;

				UpdateDesigners(true);	// even if layout weren't supressed, there is no guarantee that a layout will be performed

				RECT LRect = UnsafeUtilities.RECTFromRectangle(DisplayRectangle);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, LDelta.X, LDelta.Y, ref LRect, ref LRect, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */);

				PerformLayout();
			}
		}

		protected override void OnResize(EventArgs AArgs)
		{
			if (IsHandleCreated)
			{
				UpdateSurface();
				MinimizeScrolling();
			}
			base.OnResize(AArgs);
		}

		protected override void OnHandleCreated(EventArgs AArgs)
		{
			base.OnHandleCreated(AArgs);
			UpdateSurface();
		}

		private void MinimizeScrolling()
		{
			// Scroll off wasted space at the end
			int LNewLocationY = Math.Max(0, Math.Min(FExposed.Count, FLocation.Y + (FVisibleCount - 1)) - (FVisibleCount - 1));
			if (LNewLocationY != FLocation.Y)
				NavigateTo(new Point(FLocation.X, LNewLocationY));
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			// Don't call base

			Rectangle LBounds = base.DisplayRectangle;

			MinimizeScrolling();

			// Size the scrollbars
			FVScrollBar.Visible = (FVScrollBar.Maximum > 0) && (FVScrollBar.LargeChange <= FVScrollBar.Maximum);
			FHScrollBar.Visible = (FHScrollBar.Maximum > 0);

			FVScrollBar.Left = LBounds.Right - FVScrollBar.Width;
			FVScrollBar.Height = LBounds.Height - (FHScrollBar.Visible ? FHScrollBar.Height : 0);

			FHScrollBar.Top = LBounds.Bottom - FHScrollBar.Height;
			FHScrollBar.Width = LBounds.Width - (FVScrollBar.Visible ? FVScrollBar.Width : 0);

			LBounds = DisplayRectangle;

			// Position the designers
			IElementDesigner LDesigner;
			TreeNode LNode;
			int LEnd = Math.Min(FExposed.Count, FLocation.Y + FVisibleCount);
			for (int i = FLocation.Y; i < LEnd; i++)
			{
				LDesigner = (IElementDesigner)FDesigners[FExposed[i]];
				LNode = (TreeNode)LDesigner.Element;
				LDesigner.Bounds =
					new Rectangle
					(
						((LNode.Level() - FLocation.X) * FIndent) + (CGutterWidth / 2),
						((i - FLocation.Y) * FRowHeight) + (CGutterHeight / 2),
						250,
						FRowHeight - CGutterHeight
					);
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				if (FVScrollBar.Visible)
					LBounds.Width -= FVScrollBar.Width;
				if (FHScrollBar.Visible)
					LBounds.Height -= FHScrollBar.Height;
				return LBounds;
			}
		}

		/// <summary> Reconciles the set of visible designers with the set of possible exposed nodes. </summary>
		/// <remarks> Called when the active rectangle, or the set of exposed nodes changes. Will only 
		/// cause a layout if a designer is added or removed and ASupressLayout is false. </remarks>
		private void UpdateDesigners(bool ASupressLayout)
		{
			if (FUpdateCount == 0)
			{
				bool LLayoutRequired = false;
				// List of unused designers (initially full)
				ArrayList LDesigned = new ArrayList(FDesigners.Count);
				LDesigned.AddRange(FDesigners.Keys);

				SuspendLayout();
				try
				{
					int LEnd = Math.Min(FExposed.Count, FLocation.Y + FVisibleCount);
					IElementDesigner LDesigner;
					for (int i = FLocation.Y; i < LEnd; i++)
					{
						LDesigner = (IElementDesigner)FDesigners[FExposed[i]];
						if (LDesigner == null)
						{
							LDesigner = AddDesigner(FExposed[i]);	// Add designers we need but don't have
							LLayoutRequired = true;
						}
						else
							LDesigned.Remove(LDesigner.Element);	// Remember that this designer is used
						((Control)LDesigner).TabIndex = i;
					}

					// Remove unused designers
					foreach (object LNode in LDesigned)
					{
						RemoveDesigner(LNode);
						LLayoutRequired = true;
					}
				}
				finally
				{
					ResumeLayout(!ASupressLayout && LLayoutRequired);
				}
			}
		}

		private void UpdateSurface()
		{
			if (FUpdateCount == 0)
			{
				FHScrollBar.Maximum = Math.Max(0, Nodes.Depth() - 1);
				FVScrollBar.Maximum = Math.Max(0, (FExposed.Count - 1));

				Rectangle LBounds = DisplayRectangle;
				FVisibleCount = (LBounds.Height / FRowHeight) + (((LBounds.Height % FRowHeight) == 0) ? 0 : 1);
				FVScrollBar.LargeChange = Math.Max((FVisibleCount - 1), 1);

				UpdateDesigners(true);
			}
		}

		private int FUpdateCount = 0;

		public void BeginUpdate()
		{
			FUpdateCount++;
			SuspendLayout();
		}

		public void EndUpdate()
		{
			FUpdateCount = Math.Max(0, FUpdateCount - 1);
			if (IsHandleCreated && (FUpdateCount == 0))
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
		private Hashtable FDesigners = new Hashtable();

		public event GetDesignerHandler OnGetDesigner;

		protected virtual IElementDesigner GetDesigner(object AElement)
		{
			if (OnGetDesigner != null)
				return OnGetDesigner(AElement);
			else
				return null;
		}

		private IElementDesigner AddDesigner(object ANode)
		{
			IElementDesigner LDesigner = GetDesigner(ANode);
			if (LDesigner != null)
			{
				try
				{
					FDesigners.Add(ANode, LDesigner);
					Controls.Add((Control)LDesigner);
				}
				catch
				{
					LDesigner.Dispose();
					throw;
				}
			}
			return LDesigner;
		}

		private void RemoveDesigner(object ANode)
		{
			IElementDesigner LDesigner = (IElementDesigner)FDesigners[ANode];
			if (LDesigner != null)
			{
				if (ActiveControl == (Control)LDesigner)
					ActiveControl = null;
				Controls.Remove((Control)LDesigner);
				FDesigners.Remove(ANode);
			}
		}

		#endregion

		#region Painting

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);

			Rectangle LBounds = DisplayRectangle;
			Rectangle LLineRect;
			TreeNode LNode;
			int LNodeLevel;
			int LEnd = Math.Min(FExposed.Count, FLocation.Y + FVisibleCount);

			using (Pen LPen = new Pen(Color.White))
			{
				using (Pen LShadowPen = new Pen(Color.Gray))
				{
					using (Brush LBrush = new SolidBrush(Color.WhiteSmoke))
					{
						for (int i = FLocation.Y; i < LEnd; i++)
						{
							LNode = (TreeNode)FExposed[i];
							LNodeLevel = LNode.Level();
							LLineRect =
								new Rectangle
								(
									LBounds.X + (-FLocation.X * FIndent),
									LBounds.Y + ((i - FLocation.Y) * FRowHeight),
									(LNodeLevel * FIndent) + (CGutterWidth / 2),
									FRowHeight
								);

							// Draw the lines
							if (AArgs.Graphics.IsVisible(LLineRect) && (LNode.Parent != null))
							{
								DrawVertical(AArgs.Graphics, LLineRect, LPen, LShadowPen, LNode.Parent);
								
								// Draw the horizontal line leading to the node
								int LY = LLineRect.Y + (LLineRect.Height / 2);
								int LX = LLineRect.X + ((LNodeLevel - 1) * FIndent) + (FIndent / 2);
								AArgs.Graphics.DrawLine(LPen, LX, LY, LLineRect.Right, LY);
								AArgs.Graphics.DrawLine(LShadowPen, LX + 1, LY + 1, LLineRect.Right, LY + 1);

								// Draw the vertical line from the parent to the node (and beyond if there are additional siblings
								if (LNode.Parent.Children.IndexOf(LNode) < (LNode.Parent.Children.Count - 1))
									LY = LLineRect.Bottom;
								AArgs.Graphics.DrawLine(LPen, LX, LLineRect.Top, LX, LY - 1);
								AArgs.Graphics.DrawLine(LShadowPen, LX + 1, LLineRect.Top, LX + 1, LY - 1);
							}

							// Draw the stub
							if (LNode.Children.Count > 0)
							{
								Rectangle LRect = new Rectangle((((LNodeLevel - FLocation.X) * FIndent) + (FIndent / 2) - (CGutterWidth / 2)) - 1, LLineRect.Bottom - 5, 7, 6);
								if (AArgs.Graphics.IsVisible(LRect))
								{
									AArgs.Graphics.DrawLine(LShadowPen, new Point(LRect.X + 4, LRect.Bottom), new Point(LRect.Right, LRect.Y + 1));
									AArgs.Graphics.FillPolygon(LBrush, new Point[] {new Point(LRect.X, LRect.Y), new Point(LRect.X + 3, LRect.Bottom - 1), new Point(LRect.Right - 1, LRect.Y)});
								}
							}
						}
					}
				}
			}
		}

		private void DrawVertical(Graphics AGraphics, Rectangle ABounds, Pen APen, Pen AShadowPen, TreeNode ANode)
		{
			if (ANode.Parent != null)
			{
				if (ANode.Parent.Children.IndexOf(ANode) < (ANode.Parent.Children.Count - 1))	// if there is another sibling after this node
				{
					int LX = ABounds.X + (ANode.Parent.Level() * FIndent) + (FIndent / 2);
					AGraphics.DrawLine(APen, LX, ABounds.Top, LX, ABounds.Bottom);
					AGraphics.DrawLine(AShadowPen, LX + 1, ABounds.Top, LX + 1, ABounds.Bottom);
				}
				DrawVertical(AGraphics, ABounds, APen, AShadowPen, ANode.Parent);
			}
		}		

		#endregion

		#region Nodes

		private RootTreeNodes FNodes;
		public TreeNodes Nodes { get { return FNodes; } }

		private ArrayList FExposed = new ArrayList();

		private void ScrollVisible(int AIndex, int ADeltaY)
		{
			int LVisibleY = Math.Max(0, AIndex - FLocation.Y);
			if (LVisibleY <= FVisibleCount)
			{
				Rectangle LVisible = DisplayRectangle;
				RECT LScroll = UnsafeUtilities.RECTFromLTRB(LVisible.X, LVisibleY * FRowHeight, LVisible.Right, Math.Max(LVisible.Bottom, LVisible.Bottom + FRowHeight));
				RECT LClip = UnsafeUtilities.RECTFromRectangle(LVisible);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, 0, ADeltaY * FRowHeight, ref LScroll, ref LClip, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */ | 4 /* SW_ERASE */);
			}
		}

		internal void AddExposed(TreeNode ANode, int AIndex)
		{
			FExposed.Insert(AIndex, ANode);
			UpdateSurface();
			ScrollVisible(AIndex, 1);
			PerformLayout();
		}

		internal void RemoveExposed(int AIndex)
		{
			FExposed.RemoveAt(AIndex);
			UpdateSurface();
			ScrollVisible(AIndex + 1, -1);
			PerformLayout();
		}

		#endregion
	}

	public class TreeNode
	{
		public TreeNode()
		{
			FChildren = new TreeNodes(this);
		}

		private TreeSurface FSurface;
		public TreeSurface Surface { get { return FSurface; } }
		internal void SetSurface(TreeSurface AService)
		{
			FSurface = AService;
			foreach (TreeNode LChild in Children)
				LChild.SetSurface(AService);
		}

		internal TreeNode FParent;
		public TreeNode Parent 
		{ 
			get { return FParent; }
			set 
			{
				if (FParent != value)
				{
					if (FParent != null)
						FParent.Children.Remove(this);
					if (value != null)
						FParent.Children.Add(this);
				}
			}
		}

		private TreeNodes FChildren;
		public TreeNodes Children { get { return FChildren; } }

		private object FElement;
		public object Element { get { return FElement; } set { FElement = value; } }

		/// <summary> A node is "Exposed" if it is added to the list of Exposed (visible) nodes of the surface. </summary>
		private bool FIsExposed;

		/// <summary> Adds or removes the node from the list of exposed (not necessarily visible) nodes of the surface. </summary>
		/// <remarks> Recurses to the children if the exposed state of this node changes. </remarks>
		internal void UpdateExposed()
		{
			bool LShouldBeExposed = FIsVisible && (FSurface != null);
			TreeNode LNode = Parent;
			while ((LNode != null) && LShouldBeExposed)
			{
				LShouldBeExposed = LShouldBeExposed && LNode.IsVisible && LNode.IsExpanded;
				LNode = LNode.Parent;
			}
			SetExposed(LShouldBeExposed);
		}

		/// <summary> Sets the exposed state of the node (and it's children). </summary>
		internal void SetExposed(bool AValue)
		{
			if (AValue != FIsExposed)	// This should never change when there is no surface
			{
				FIsExposed = AValue;
				if (AValue)
					FSurface.AddExposed(this, ExposedIndex());
				else
					FSurface.RemoveExposed(ExposedIndex());
			}
			if (AValue)
				UpdateChildrenExposed();
			else
				foreach (TreeNode LChild in Children)
					LChild.SetExposed(false);
		}

		/// <summary> Updates the exposed state of the child nodes. </summary>
		private void UpdateChildrenExposed()
		{
			foreach (TreeNode LChild in Children)
				LChild.UpdateExposed();
		}

		private bool FIsVisible = true;
		public bool IsVisible
		{
			get { return FIsVisible; }
			set
			{
				if (FIsVisible != value)
				{
					FIsVisible = value;
					UpdateExposed();
				}
			}
		}

		private bool FIsExpanded;
		public bool IsExpanded 
		{ 
			get { return FIsExpanded; }
			set 
			{
				if (FIsExpanded != value)
					InternalSetExpanded(value, false);
			}
		}

		private void InternalRecurseExpanded(bool AIsExpanded)
		{
			FIsExpanded = AIsExpanded;
			foreach (TreeNode LChild in Children)
				LChild.InternalRecurseExpanded(AIsExpanded);
		}

		private void InternalSetExpanded(bool AIsExpanded, bool ARecursive)
		{
			if (FSurface != null)
				FSurface.BeginUpdate();
			try
			{
				bool LRefreshChildren = AIsExpanded || (AIsExpanded != FIsExpanded);
				if (ARecursive)
					InternalRecurseExpanded(AIsExpanded);
				else
					FIsExpanded = AIsExpanded;
				if (LRefreshChildren)
					UpdateChildrenExposed();
			}
			finally
			{
				if (FSurface != null)
					FSurface.EndUpdate();
			}
		}

		public void Expand(bool ARecursive)
		{
			InternalSetExpanded(true, ARecursive);
		}

		public void Collapse(bool ARecursive)
		{
			InternalSetExpanded(false, ARecursive);
		}

		/// <summary> A count of exposed nodes (including this one and any visible children recursively). </summary>
		private int RecursiveCount()
		{
			int LResult = 0;
			if (FIsExposed)
				LResult++;
			foreach (TreeNode LChild in Children)
				LResult += LChild.RecursiveCount();
			return LResult;
		}

		/// <summary> The index of the node relative to the visible ordering of all nodes. </summary>
		public int ExposedIndex()
		{
			if (FParent != null)
			{
				int LResult = FParent.ExposedIndex();
				if (FParent.FIsExposed)
					LResult++;
				for (int i = 0; (i < FParent.Children.Count) && (FParent.Children[i] != this); i++)
					LResult += FParent.Children[i].RecursiveCount();
				return LResult;
			}
			else
				return 0;
		}

		public int Level()
		{
			int LResult = 0;
			TreeNode LNode = FParent;
			while (LNode != null)
			{
				LResult++;
				LNode = LNode.Parent;
			}
			return LResult;
		}

		public int Depth()
		{
			int LMax = 0;
			if (FIsExposed)
			{
				int LCurrent;
				foreach (TreeNode LChild in Children)
				{
					LCurrent = LChild.Depth();
					if (LCurrent > LMax)
						LMax = LCurrent;
				}
				LMax++;
			}
			return LMax;
		}
	}

	public class TreeNodes : List
	{
		public TreeNodes(TreeNode ANode)
		{
			FNode = ANode;
		}

		private TreeNode FNode;
		public TreeNode Node { get { return FNode; } }

		public virtual TreeSurface GetSurface()
		{
			return FNode.Surface;
		}

		public new TreeNode this[int AIndex]
		{
			get { return (TreeNode)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			TreeNode LNode = (TreeNode)AValue;
			LNode.Parent = null;
			LNode.FParent = FNode;
			LNode.SetSurface(GetSurface());
			LNode.UpdateExposed();
		}

		protected override void Removing(object AValue, int AIndex)
		{
			TreeNode LNode = (TreeNode)AValue;
			LNode.SetExposed(false);
			LNode.SetSurface(null);
			LNode.FParent = null;
			base.Removing(AValue, AIndex);
		}

		public int Depth()
		{
			int LMax = 0;
			int LCurrent;
			foreach (TreeNode LChild in this)
			{
				LCurrent = LChild.Depth();
				if (LCurrent > LMax)
					LMax = LCurrent;
			}
			return LMax;
		}
	}

	public class RootTreeNodes : TreeNodes
	{
		public RootTreeNodes(TreeSurface ASurface) : base(null)
		{
			FSurface = ASurface;
		}

		private TreeSurface FSurface;

		public override TreeSurface GetSurface()
		{
			return FSurface;
		}
	}
}
