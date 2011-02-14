/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Abstract class the implements functionality common to container elements not associated with a control. </summary>
	public abstract class ContainerElement : Element, IWindowsContainerElement
    {
		// IWindowsContainerElement

		/// <value> The control that children are to use as a parent. </value>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual WinForms.Control Control
		{
			get { return ((IWindowsContainerElement)Parent).Control; }
		}
	
		// Element

		public override int GetDefaultMarginLeft()
		{
			return 0;
		}

		public override int GetDefaultMarginRight()
		{
			return 0;
		}

		public override int GetDefaultMarginTop()
		{
			return 0;
		}

		public override int GetDefaultMarginBottom()
		{
			return 0;
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IElement).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		// Utility

		protected class ChildDetail
		{
			public ChildDetail(int min, int max, int natural)
			{
				Min = min;
				Max = max;
				Natural = natural;
			}

			public int Min;
			public int Max;
			public int Natural;
			public int Current;
			public IWindowsElement Child;
		}

		protected static int AllocateProportionally(ChildDetail[] items, int delta, int naturalSum)
		{
			int oldDelta;
			int itemDelta;
			// Keep handing portions of the delta until delta doesn't change
			do
			{
				oldDelta = delta;
				foreach (ChildDetail detail in items)
				{
					if (detail != null) 
					{
						itemDelta = (int)(((double)detail.Natural / (double)naturalSum) * oldDelta);
						if (detail.Current + itemDelta > detail.Max)
							itemDelta += detail.Max - (detail.Current + itemDelta);
						if (detail.Current + itemDelta < detail.Min)
							itemDelta += detail.Min - (detail.Current + itemDelta);
						detail.Current += itemDelta;
						delta -= itemDelta;
					}
				}
			} while (oldDelta != delta);
			return delta;
		}
	}
    
	[DesignerImage("Image('Frontend', 'Nodes.Column')")]
	[DesignerCategory("Static Controls")]
	public class Column : ContainerElement, IColumn
    {
		// IColumn

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					UpdateLayout();
				}
			}
		}

		// Element

		/// <remarks> Arranges the column controls using standard column arrangement. </remarks>
		protected override void InternalLayout(Rectangle bounds)
		{
			// Read the Min, Max and Natural sizes for the children (for perf reasons)
			ChildDetail[] childDetails = new ChildDetail[Children.Count];
			int naturalHeightSum = 0;
			int childCount = 0;
			foreach (IWindowsElement child in Children)
			{
				if (child.GetVisible()) 
				{
					childDetails[childCount] = new ChildDetail(child.MinSize.Height, child.MaxSize.Height, child.NaturalSize.Height);
					childDetails[childCount].Current = 0;
					childDetails[childCount].Child = child;
					naturalHeightSum += childDetails[childCount].Natural;
					childCount++;
				}
			}

			int deltaHeight = 0;
			if (naturalHeightSum != 0)
				deltaHeight = AllocateProportionally(childDetails, bounds.Height, naturalHeightSum);

			// adjust for vertical alignment
			if (_verticalAlignment != VerticalAlignment.Top)
			{
				if (_verticalAlignment == VerticalAlignment.Middle)
					bounds.Y += deltaHeight / 2;
				else // bottom
					bounds.Y += deltaHeight;
			}

			// Optionally we could force the controls to shrink or grow to take the remaining space (LDeltaHeight)

			// Now call SetSize for the children based on our calculations
			for (int i = 0; i < childCount; i++)
			{
				ChildDetail detail = childDetails[i];
				detail.Child.Layout(new Rectangle(bounds.Location, new Size(bounds.Width, detail.Current)));
				bounds.Y += detail.Current;
			}
		}
		
		/// <remarks> The sum of the min heights.  The greatest of the min widths. </remarks>
		protected override Size InternalMinSize
		{
			get
			{
				Size result = Size.Empty;
				Size childMinSize;
				foreach (IWindowsElement child in Children)
				{
					if (child.GetVisible()) 
					{
						childMinSize = child.MinSize;
						result.Height += childMinSize.Height;
						ConstrainMinWidth(ref result, childMinSize.Width);
					}
				}
				return result;
			}
		}
		
		/// <remarks> The sum of the max heights.  The greatest of the max widths. </remarks>
		protected override Size InternalMaxSize
		{
			get
			{
				Size result = Size.Empty;
				Size childMaxSize;
				foreach (IWindowsElement child in Children)
				{
					if (child.GetVisible()) 
					{
						childMaxSize = child.MaxSize;
						result.Height += childMaxSize.Height;
						ConstrainMinWidth(ref result, childMaxSize.Width);
					}
				}
				return result;
			}
		}
		
		/// <remarks>
		///		The sum of the natural heights.  The max of the natural widths 
		///		constrained first to the max width then to the min width.
		///	</remarks>
		protected override Size InternalNaturalSize
		{
			get
			{
				Size result = Size.Empty;

				Size natural;
				foreach (IWindowsElement child in Children)
				{
					if (child.GetVisible()) 
					{
						natural = child.NaturalSize;
                        result.Height += natural.Height + child.MarginTop + child.MarginBottom + 2;
						ConstrainMinWidth(ref result, natural.Width);
					}
				}
				
				ConstrainMaxWidth(ref result, MaxSize.Width);
				ConstrainMinWidth(ref result, MinSize.Width);

				return result;
			}
		}
    }
    
	[DesignerImage("Image('Frontend', 'Nodes.Row')")]
	[DesignerCategory("Static Controls")]
	public class Row : ContainerElement, IRow
    {		
		// IRow

		protected HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set
			{
				if (_horizontalAlignment != value)
				{
					_horizontalAlignment = value;
					UpdateLayout();
				}
			}
		}
		
		// Element

		protected override void InternalLayout(Rectangle bounds) 
		{
			// Read the Min, Max and Natural sizes for the children (for perf reasons)
			ChildDetail[] childDetails = new ChildDetail[Children.Count];
			int naturalWidthSum = 0;
			int childCount = 0;
			foreach (IWindowsElement child in Children)
			{
				if (child.GetVisible()) 
				{
					childDetails[childCount] = new ChildDetail(child.MinSize.Width, child.MaxSize.Width, child.NaturalSize.Width);
					childDetails[childCount].Current = 0;
					childDetails[childCount].Child = child;
					naturalWidthSum += childDetails[childCount].Natural;
					childCount++;
				}
			}

			int deltaWidth = 0;
			if (naturalWidthSum != 0)
				deltaWidth = AllocateProportionally(childDetails, bounds.Width, naturalWidthSum);

			// adjust for horizontal alignment
			if (_horizontalAlignment != HorizontalAlignment.Left)
			{
				if (_horizontalAlignment == HorizontalAlignment.Center)
					bounds.X += deltaWidth / 2;
				else // Right
					bounds.X += deltaWidth;
			}

			// Optionally we could force the controls to shrink or grow to take the remaining space (LDeltaWidth)

			// Now layout the children based on our calculations
			for (int i = 0; i < childCount; i++)
			{
				ChildDetail detail = childDetails[i];
				detail.Child.Layout(new Rectangle(bounds.Location, new Size(detail.Current, bounds.Height)));
				bounds.X += detail.Current;
			}
		}
		
		/// <remarks> The sum of the min widths.  The greatest of the min heights. </remarks>
		protected override Size InternalMinSize
		{
			get
			{
				Size result = Size.Empty;
				Size childMinSize;
				foreach (IWindowsElement child in Children)
				{
					if (child.GetVisible()) 
					{
						childMinSize = child.MinSize;
						result.Width += childMinSize.Width;
						ConstrainMinHeight(ref result, childMinSize.Height);
					}
				}
				return result;
			}
		}
		
		/// <remarks> The sum of the max widths.  The greatest of the max heights. </remarks>
		protected override Size InternalMaxSize
		{
			get
			{
				Size result = Size.Empty;
				Size childMaxSize;
				foreach (IWindowsElement child in Children)
				{
					if (child.GetVisible()) 
					{
						childMaxSize = child.MaxSize;
						result.Width += childMaxSize.Width;
						ConstrainMinHeight(ref result, childMaxSize.Height);
					}
				}
				return result;
			}
		}
		
		/// <remarks>
		///		The sum of the natural widths.  The average of the natural heights 
		///		constrained first to the max height then to the min height.
		///	</remarks>
		protected override Size InternalNaturalSize
		{
			get
			{
				Size result = Size.Empty;

				Size natural;
				foreach (IWindowsElement child in Children)
				{
					if (child.GetVisible()) 
					{
						natural = child.NaturalSize;
						result.Width += natural.Width;
						ConstrainMinHeight(ref result, natural.Height);
					}
				}
				
				ConstrainMaxHeight(ref result, MaxSize.Height);
				ConstrainMinHeight(ref result, MinSize.Height);

				return result;
			}
		}
    }

    /// <summary> Abstract class the implements functionality common to container elements that are associated with a control. </summary>
	public abstract class ControlContainer : Element, IWindowsContainerElement, IWindowsControlElement
	{
		// Control

		private WinForms.Control _control;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual WinForms.Control Control
		{
			get { return _control; }
			set 
			{ 
				if (_control != null)
					((Session)HostNode.Session).UnregisterControlHelp(_control);
				_control = value; 
				if (_control != null)
					((Session)HostNode.Session).RegisterControlHelp(_control, this);
			}
		}

		protected abstract WinForms.Control CreateControl();

		protected virtual void InitializeControl() { }

		// Text

		protected string _title = String.Empty;
		[DefaultValue("")]
		[Description("Title used as a title for the group.")]
		public string Title
		{
			get	{ return _title; }
			set
			{
				_title = value;
				if (Active)
					InternalUpdateTitle();
			}
		}

		public virtual string GetTitle()
		{
			return _title;
		}

		private string _allocatedTitle;

		protected void DeallocateAccelerator()
		{
			if (_allocatedTitle != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(_allocatedTitle);
				_allocatedTitle = null;
			}
		}

		protected virtual void SetControlText(string title)
		{
			Control.Text = title;
		}

		protected virtual bool AlwaysAccellerate()
		{
			return true;
		}

		protected virtual void InternalUpdateTitle()
		{
			DeallocateAccelerator();
			_allocatedTitle = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetTitle(), AlwaysAccellerate());
			SetControlText(_allocatedTitle);
		}

		// Parent

		protected virtual void SetParent()
		{
			_control.Parent = ((IWindowsContainerElement)Parent).Control;
		}

		// Node

		protected IWindowsElement _child;

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IWindowsElement).IsAssignableFrom(childType))
				return (_child == null);
			return base.IsValidChild(childType);
		}

		protected override void AddChild(INode child)
		{
			base.AddChild(child);
			_child = (IWindowsElement)child;
		}

		protected override void RemoveChild(INode child)
		{
			_child = null;
			base.RemoveChild(child);
		}

		protected virtual void UpdateColor()
		{
			Control.BackColor = ((Session)HostNode.Session).Theme.ContainerColor;
		}

		protected override void Activate()
		{
			Control = CreateControl();
			try
			{
				UpdateColor();
				SetParent();
				InternalUpdateTitle();
				InitializeControl();
				base.Activate();
			}
			catch
			{
				if (_control != null)
				{
					_control.Dispose();
					_control = null;
				}
				throw;
			}
		}
		
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				try
				{
					DeallocateAccelerator();
				}
				finally
				{
					if (Control != null)
					{
						Control.Dispose();
						Control = null;
					}
				}
			}
		}

		// Element

		protected override void InternalUpdateVisible() 
		{
			Control.Visible = GetVisible();
			base.InternalUpdateVisible();
		}

		protected override void InternalUpdateTabStop()
		{
			Control.TabStop = GetTabStop();
		}

		public override Size GetOverhead()
		{
			return base.GetOverhead() + (Control.Size - Control.DisplayRectangle.Size);
		}

		protected virtual void LayoutControl(Rectangle bounds)
		{
			Control.Bounds = bounds;
		}

		protected virtual void LayoutChild(IWindowsElement child, Rectangle bounds)
		{
			if ((child != null) && child.GetVisible())
				child.Layout(Control.DisplayRectangle);
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			LayoutControl(bounds);
			LayoutChild(_child, bounds);
		}
		
		protected override Size InternalMinSize
		{
			get
			{
				if ((_child != null) && _child.GetVisible())
					return _child.MinSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				if ((_child != null) && _child.GetVisible())
					return _child.MaxSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				if ((_child != null) && _child.GetVisible())
					return _child.NaturalSize;
				else
					return Size.Empty;
			}
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.Group')")]
	[DesignerCategory("Static Controls")]
	public class Group : ControlContainer, IGroup
    {
		public const int BottomPadding = 2;

		// ControlContainer

		protected override WinForms.Control CreateControl()
		{
			return new ExtendedGroupBox();
		}

		public override Size GetOverhead()
		{
			return base.GetOverhead() + new Size(0, BottomPadding);
		}

        protected override Size InternalMinSize
        {
            get
            {
                Size result = new Size((Size.Ceiling(Control.CreateGraphics().MeasureString(Title, Control.Font)).Width + GetOverhead().Width), Control.Font.Height);
                if ((_child != null) && _child.GetVisible())
                {
                    ConstrainMinWidth(ref result, _child.MinSize.Width);
                    ConstrainMinHeight(ref result, _child.MinSize.Height);
                }
                return result;         
            }
        }

        protected override Size InternalNaturalSize
        {
            get
            {
                Size result = new Size((Size.Ceiling(Control.CreateGraphics().MeasureString(Title, Control.Font)).Width + GetOverhead().Width), Control.Font.Height);
                if ((_child != null) && _child.GetVisible())
                {
                    ConstrainMinWidth(ref result, _child.NaturalSize.Width);
                    ConstrainMinHeight(ref result, _child.NaturalSize.Height);
                }
                return result;
            }
        }
	}

	[ToolboxItem(false)]
	public class ExtendedGroupBox : WinForms.GroupBox
	{
		public ExtendedGroupBox()
		{
			CausesValidation = false;
		}
	}

}
