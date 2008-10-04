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

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IElement).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		// Utility

		protected class ChildDetail
		{
			public ChildDetail(int AMin, int AMax, int ANatural)
			{
				Min = AMin;
				Max = AMax;
				Natural = ANatural;
			}

			public int Min;
			public int Max;
			public int Natural;
			public int Current;
			public IWindowsElement Child;
		}

		protected static int AllocateProportionally(ChildDetail[] AItems, int ADelta, int ANaturalSum)
		{
			int LOldDelta;
			int LItemDelta;
			// Keep handing portions of the delta until delta doesn't change
			do
			{
				LOldDelta = ADelta;
				foreach (ChildDetail LDetail in AItems)
				{
					if (LDetail != null) 
					{
						LItemDelta = (int)(((double)LDetail.Natural / (double)ANaturalSum) * LOldDelta);
						if (LDetail.Current + LItemDelta > LDetail.Max)
							LItemDelta += LDetail.Max - (LDetail.Current + LItemDelta);
						if (LDetail.Current + LItemDelta < LDetail.Min)
							LItemDelta += LDetail.Min - (LDetail.Current + LItemDelta);
						LDetail.Current += LItemDelta;
						ADelta -= LItemDelta;
					}
				}
			} while (LOldDelta != ADelta);
			return ADelta;
		}
	}
    
	[DesignerImage("Image('Frontend', 'Nodes.Column')")]
	[DesignerCategory("Static Controls")]
	public class Column : ContainerElement, IColumn
    {
		// IColumn

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					UpdateLayout();
				}
			}
		}

		// Element

		/// <remarks> Arranges the column controls using standard column arrangement. </remarks>
		protected override void InternalLayout(Rectangle ABounds)
		{
			// Read the Min, Max and Natural sizes for the children (for perf reasons)
			ChildDetail[] LChildDetails = new ChildDetail[Children.Count];
			int LNaturalHeightSum = 0;
			int LChildCount = 0;
			foreach (IWindowsElement LChild in Children)
			{
				if (LChild.GetVisible()) 
				{
					LChildDetails[LChildCount] = new ChildDetail(LChild.MinSize.Height, LChild.MaxSize.Height, LChild.NaturalSize.Height);
					LChildDetails[LChildCount].Current = 0;
					LChildDetails[LChildCount].Child = LChild;
					LNaturalHeightSum += LChildDetails[LChildCount].Natural;
					LChildCount++;
				}
			}

			int LDeltaHeight = 0;
			if (LNaturalHeightSum != 0)
				LDeltaHeight = AllocateProportionally(LChildDetails, ABounds.Height, LNaturalHeightSum);

			// adjust for vertical alignment
			if (FVerticalAlignment != VerticalAlignment.Top)
			{
				if (FVerticalAlignment == VerticalAlignment.Middle)
					ABounds.Y += LDeltaHeight / 2;
				else // bottom
					ABounds.Y += LDeltaHeight;
			}

			// Optionally we could force the controls to shrink or grow to take the remaining space (LDeltaHeight)

			// Now call SetSize for the children based on our calculations
			for (int i = 0; i < LChildCount; i++)
			{
				ChildDetail LDetail = LChildDetails[i];
				LDetail.Child.Layout(new Rectangle(ABounds.Location, new Size(ABounds.Width, LDetail.Current)));
				ABounds.Y += LDetail.Current;
			}
		}
		
		/// <remarks> The sum of the min heights.  The greatest of the min widths. </remarks>
		protected override Size InternalMinSize
		{
			get
			{
				Size LResult = Size.Empty;
				Size LChildMinSize;
				foreach (IWindowsElement LChild in Children)
				{
					if (LChild.GetVisible()) 
					{
						LChildMinSize = LChild.MinSize;
						LResult.Height += LChildMinSize.Height;
						ConstrainMinWidth(ref LResult, LChildMinSize.Width);
					}
				}
				return LResult;
			}
		}
		
		/// <remarks> The sum of the max heights.  The greatest of the max widths. </remarks>
		protected override Size InternalMaxSize
		{
			get
			{
				Size LResult = Size.Empty;
				Size LChildMaxSize;
				foreach (IWindowsElement LChild in Children)
				{
					if (LChild.GetVisible()) 
					{
						LChildMaxSize = LChild.MaxSize;
						LResult.Height += LChildMaxSize.Height;
						ConstrainMinWidth(ref LResult, LChildMaxSize.Width);
					}
				}
				return LResult;
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
				Size LResult = Size.Empty;

				Size LNatural;
				foreach (IWindowsElement LChild in Children)
				{
					if (LChild.GetVisible()) 
					{
						LNatural = LChild.NaturalSize;
                        LResult.Height += LNatural.Height + LChild.MarginTop + LChild.MarginBottom + 2;
						ConstrainMinWidth(ref LResult, LNatural.Width);
					}
				}
				
				ConstrainMaxWidth(ref LResult, MaxSize.Width);
				ConstrainMinWidth(ref LResult, MinSize.Width);

				return LResult;
			}
		}
    }
    
	[DesignerImage("Image('Frontend', 'Nodes.Row')")]
	[DesignerCategory("Static Controls")]
	public class Row : ContainerElement, IRow
    {		
		// IRow

		protected HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set
			{
				if (FHorizontalAlignment != value)
				{
					FHorizontalAlignment = value;
					UpdateLayout();
				}
			}
		}
		
		// Element

		protected override void InternalLayout(Rectangle ABounds) 
		{
			// Read the Min, Max and Natural sizes for the children (for perf reasons)
			ChildDetail[] LChildDetails = new ChildDetail[Children.Count];
			int LNaturalWidthSum = 0;
			int LChildCount = 0;
			foreach (IWindowsElement LChild in Children)
			{
				if (LChild.GetVisible()) 
				{
					LChildDetails[LChildCount] = new ChildDetail(LChild.MinSize.Width, LChild.MaxSize.Width, LChild.NaturalSize.Width);
					LChildDetails[LChildCount].Current = 0;
					LChildDetails[LChildCount].Child = LChild;
					LNaturalWidthSum += LChildDetails[LChildCount].Natural;
					LChildCount++;
				}
			}

			int LDeltaWidth = 0;
			if (LNaturalWidthSum != 0)
				LDeltaWidth = AllocateProportionally(LChildDetails, ABounds.Width, LNaturalWidthSum);

			// adjust for horizontal alignment
			if (FHorizontalAlignment != HorizontalAlignment.Left)
			{
				if (FHorizontalAlignment == HorizontalAlignment.Center)
					ABounds.X += LDeltaWidth / 2;
				else // Right
					ABounds.X += LDeltaWidth;
			}

			// Optionally we could force the controls to shrink or grow to take the remaining space (LDeltaWidth)

			// Now layout the children based on our calculations
			for (int i = 0; i < LChildCount; i++)
			{
				ChildDetail LDetail = LChildDetails[i];
				LDetail.Child.Layout(new Rectangle(ABounds.Location, new Size(LDetail.Current, ABounds.Height)));
				ABounds.X += LDetail.Current;
			}
		}
		
		/// <remarks> The sum of the min widths.  The greatest of the min heights. </remarks>
		protected override Size InternalMinSize
		{
			get
			{
				Size LResult = Size.Empty;
				Size LChildMinSize;
				foreach (IWindowsElement LChild in Children)
				{
					if (LChild.GetVisible()) 
					{
						LChildMinSize = LChild.MinSize;
						LResult.Width += LChildMinSize.Width;
						ConstrainMinHeight(ref LResult, LChildMinSize.Height);
					}
				}
				return LResult;
			}
		}
		
		/// <remarks> The sum of the max widths.  The greatest of the max heights. </remarks>
		protected override Size InternalMaxSize
		{
			get
			{
				Size LResult = Size.Empty;
				Size LChildMaxSize;
				foreach (IWindowsElement LChild in Children)
				{
					if (LChild.GetVisible()) 
					{
						LChildMaxSize = LChild.MaxSize;
						LResult.Width += LChildMaxSize.Width;
						ConstrainMinHeight(ref LResult, LChildMaxSize.Height);
					}
				}
				return LResult;
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
				Size LResult = Size.Empty;

				Size LNatural;
				foreach (IWindowsElement LChild in Children)
				{
					if (LChild.GetVisible()) 
					{
						LNatural = LChild.NaturalSize;
						LResult.Width += LNatural.Width;
						ConstrainMinHeight(ref LResult, LNatural.Height + LChild.MarginBottom + LChild.MarginTop + 2);
					}
				}
				
				ConstrainMaxHeight(ref LResult, MaxSize.Height);
				ConstrainMinHeight(ref LResult, MinSize.Height);

				return LResult;
			}
		}
    }

    /// <summary> Abstract class the implements functionality common to container elements that are associated with a control. </summary>
	public abstract class ControlContainer : Element, IWindowsContainerElement, IWindowsControlElement
	{
		// Control

		private WinForms.Control FControl;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual WinForms.Control Control
		{
			get { return FControl; }
			set 
			{ 
				if (FControl != null)
					((Session)HostNode.Session).UnregisterControlHelp(FControl);
				FControl = value; 
				if (FControl != null)
					((Session)HostNode.Session).RegisterControlHelp(FControl, this);
			}
		}

		protected abstract WinForms.Control CreateControl();

		protected virtual void InitializeControl() { }

		// Text

		protected string FTitle = String.Empty;
		[DefaultValue("")]
		[Description("Title used as a title for the group.")]
		public string Title
		{
			get	{ return FTitle; }
			set
			{
				FTitle = value;
				if (Active)
					InternalUpdateTitle();
			}
		}

		public virtual string GetTitle()
		{
			return FTitle;
		}

		private string FAllocatedTitle;

		protected void DeallocateAccelerator()
		{
			if (FAllocatedTitle != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(FAllocatedTitle);
				FAllocatedTitle = null;
			}
		}

		protected virtual void SetControlText(string ATitle)
		{
			Control.Text = ATitle;
		}

		protected virtual bool AlwaysAccellerate()
		{
			return true;
		}

		protected virtual void InternalUpdateTitle()
		{
			DeallocateAccelerator();
			FAllocatedTitle = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetTitle(), AlwaysAccellerate());
			SetControlText(FAllocatedTitle);
		}

		// Parent

		protected virtual void SetParent()
		{
			FControl.Parent = ((IWindowsContainerElement)Parent).Control;
		}

		// Node

		protected IWindowsElement FChild;

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IWindowsElement).IsAssignableFrom(AChildType))
				return (FChild == null);
			return base.IsValidChild(AChildType);
		}

		protected override void AddChild(INode AChild)
		{
			base.AddChild(AChild);
			FChild = (IWindowsElement)AChild;
		}

		protected override void RemoveChild(INode AChild)
		{
			FChild = null;
			base.RemoveChild(AChild);
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
				if (FControl != null)
				{
					FControl.Dispose();
					FControl = null;
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

		protected virtual void LayoutControl(Rectangle ABounds)
		{
			Control.Bounds = ABounds;
		}

		protected virtual void LayoutChild(IWindowsElement AChild, Rectangle ABounds)
		{
			if ((AChild != null) && AChild.GetVisible())
				AChild.Layout(Control.DisplayRectangle);
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			LayoutControl(ABounds);
			LayoutChild(FChild, ABounds);
		}
		
		protected override Size InternalMinSize
		{
			get
			{
				if ((FChild != null) && FChild.GetVisible())
					return FChild.MinSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				if ((FChild != null) && FChild.GetVisible())
					return FChild.MaxSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				if ((FChild != null) && FChild.GetVisible())
					return FChild.NaturalSize;
				else
					return Size.Empty;
			}
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.Group')")]
	[DesignerCategory("Static Controls")]
	public class Group : ControlContainer, IGroup
    {
		public const int CBottomPadding = 2;

		// ControlContainer

		protected override WinForms.Control CreateControl()
		{
			return new ExtendedGroupBox();
		}

		public override Size GetOverhead()
		{
			return base.GetOverhead() + new Size(0, CBottomPadding);
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