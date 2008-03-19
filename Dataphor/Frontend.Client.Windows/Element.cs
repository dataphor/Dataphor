/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing.Design;
using System.ComponentModel;
using System.Drawing;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Base node for all visible nodes. </summary>
	public abstract class Element : Node, IWindowsElement
	{
		public const string CAverageChars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		
		public const int CDefaultMarginLeft = 2;
		public const int CDefaultMarginRight = 2;
		public const int CDefaultMarginTop = 2;
		public const int CDefaultMarginBottom = 2;

		public Element() : base()
		{
			FMarginLeft = GetDefaultMarginLeft();
			FMarginRight = GetDefaultMarginRight();
			FMarginTop = GetDefaultMarginTop();
			FMarginBottom = GetDefaultMarginBottom();
			FTabStop = GetDefaultTabStop();
		}

		#region Visible

		private bool FVisible = true;
		[DefaultValue(true)]
		[Description("When set to false the control will not be shown.")]
		public bool Visible
		{
			get { return FVisible; }
			set
			{
				if (FVisible != value)
				{
					FVisible = value;
					VisibleChanged();
				}
			}
		}

		public virtual bool GetVisible() 
		{
			return FVisible && ( Parent is IElement ? ((IElement)Parent).GetVisible() : true );
		}

		/// <remarks> Updates all visual children's visibility. </remarks>
		protected virtual void InternalUpdateVisible() {}

		public virtual void VisibleChanged()
		{
			if (Active)
			{
				BeginUpdate();
				try
				{
					InternalUpdateVisible();
					IWindowsElement LVisualChild;
					foreach (INode LChild in Children)
					{
						LVisualChild = LChild as IWindowsElement;
						if (LVisualChild != null)
							LVisualChild.VisibleChanged();
					}
				}
				finally
				{
					EndUpdate(false);
					UpdateLayout();
				}
			}
		}

		#endregion

		#region TabStop

		private bool FTabStop;
		[Description("Determines if the control can be focused through tab key navigation.")]
		[DefaultValueMember("GetDefaultTabStop")]
		public bool TabStop
		{
			get { return FTabStop; }
			set 
			{ 
				if (FTabStop != value)
				{
					FTabStop = value;
					TabStopChanged();
				}
			}
		}

		public virtual bool GetDefaultTabStop()
		{
			return true;
		}

		public virtual bool GetTabStop()
		{
			return FTabStop;
		}

		protected virtual void InternalUpdateTabStop() { }

		protected virtual void TabStopChanged() 
		{
			if (Active)
				InternalUpdateTabStop();
		}

		#endregion

		#region Hint

		private string FHint = String.Empty;
		[Browsable(true)]
		[DefaultValue("")]
		[Description("Text which describes the element to the end-user.")]
		public string Hint
		{
			get { return FHint; }
			set
			{
				if (FHint != value)
				{
					FHint = value;
					UpdateToolTip();
				}
			}
		}

		public virtual string GetHint()
		{
			return FHint;
		}

		protected virtual void InternalUpdateToolTip()
		{
			// abstract
		}

		protected void UpdateToolTip()
		{
			if (Active)
				InternalUpdateToolTip();
		}

		protected void SetToolTip(WinForms.Control AControl)
		{
			Client.IHost LHost = HostNode;
			if ((LHost != null) && (LHost.Session != null))
				((Windows.Session)LHost.Session).ToolTip.SetToolTip(AControl, GetHint());
		}

		#endregion

		#region Help

		protected virtual void InternalUpdateHelp() {}

		protected virtual void UpdateHelp()
		{
			if (Active)
				InternalUpdateHelp();
		}

		private string FHelpKeyword = String.Empty;
		[Description("The help keyword to navigate to when the user activates help.")]
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return FHelpKeyword; }
			set 
			{ 
				if (value != FHelpKeyword)
				{
					FHelpKeyword = (value == null ? String.Empty : value);
					UpdateHelp();
				}
			}
		}
		
		private HelpKeywordBehavior FHelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		[Description("Determines the method used to navigate the the help identified by the HelpKeyword property.")]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return FHelpKeywordBehavior; }
			set 
			{ 
				if (value != FHelpKeywordBehavior)
				{
					FHelpKeywordBehavior = value; 
					UpdateHelp();
				}
			}
		}

		private string FHelpString = String.Empty;
		[Description("Specifies the help text to display to the user when keyword navigation is not used.")]
		[DefaultValue("")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("txt")]
		public string HelpString
		{ 
			get { return FHelpString; }
			set 
			{ 
				if (value != FHelpString)
				{
					FHelpString = (value == null ? String.Empty : value); 
					UpdateHelp();
				}
			}
		}

		#endregion

		#region Margins

		// SuppressMargins

		private bool FSuppressMargins;
		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public bool SuppressMargins
		{
			get { return FSuppressMargins; }
			set 
			{
				if (FSuppressMargins != value)
				{
					FSuppressMargins = value;
					UpdateLayout();
				}
			}
		}

		// MarginLeft
		
		private int FMarginLeft;
		[Description("The left margin for the control")]
		[DefaultValueMember("GetDefaultMarginLeft")]
		public int MarginLeft
		{
			get { return FMarginLeft; }
			set
			{
				if (FMarginLeft != value)
				{
					FMarginLeft = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginLeft()
		{
			return CDefaultMarginLeft;
		}
		
		// MarginRight
		
		private int FMarginRight;
		[Description("The right margin for the control")]
		[DefaultValueMember("GetDefaultMarginRight")]
		public int MarginRight
		{
			get { return FMarginRight; }
			set
			{
				if (FMarginRight != value)
				{
					FMarginRight = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginRight()
		{
			return CDefaultMarginRight;
		}
		
		// MarginTop
		
		private int FMarginTop;
		[Description("The top margin for the control")]
		[DefaultValueMember("GetDefaultMarginTop")]
		public int MarginTop
		{
			get { return FMarginTop; }
			set
			{
				if (FMarginTop != value)
				{
					FMarginTop = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginTop()
		{
			return CDefaultMarginTop;
		}
		
		// MarginBottom
		
		private int FMarginBottom;
		[Description("The bottom margin for the control")]
		[DefaultValueMember("GetDefaultMarginBottom")]
		public int MarginBottom
		{
			get { return FMarginBottom; }
			set
			{
				if (FMarginBottom != value)
				{
					FMarginBottom = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginBottom()
		{
			return CDefaultMarginBottom;
		}
		
		#endregion

		#region Layout

		/// <summary> Call anytime subsequent layouts should recalcuate the sizes. </summary>
		protected virtual void InvalidateLayoutCache()
		{
			FMinSize = Size.Empty;
			FMaxSize = Size.Empty;
			FNaturalSize = Size.Empty;
		}

		/// <summary> Call to update the appearance and size of the element or its children. </summary>
		/// <remarks> Clears the layout cache and sends the call to the root element for propigation to <see cref="Layout"/>. </remarks>
		public void UpdateLayout()
		{
			if (Active)
			{
				InvalidateLayoutCache();

				// Relayout to the root
				if (this is IWindowsFormInterface)
					((IWindowsFormInterface)this).RootLayout();
				else
					((IWindowsElement)Parent).UpdateLayout();
			}
		}

		protected override void ChildrenChanged()
		{
			base.ChildrenChanged();
			UpdateLayout();
		}

		public virtual void BeginUpdate()
		{
			IUpdateHandler LHandler = (IUpdateHandler)FindParent(typeof(IUpdateHandler));
			if (LHandler != null)
				LHandler.BeginUpdate();
		}

		public virtual void EndUpdate(bool APerformLayout)
		{
			IUpdateHandler LHandler = (IUpdateHandler)FindParent(typeof(IUpdateHandler));
			if (LHandler != null)
				LHandler.EndUpdate(APerformLayout);
		}

		// GetOverhead()

		public virtual Size GetOverhead()
		{
			if (FSuppressMargins)
				return Size.Empty;
			else
				return new Size(FMarginLeft + FMarginRight, FMarginTop + FMarginBottom);
		}

		// InternalLayout

		public void Layout(Rectangle ABounds) 
		{
			if (!FSuppressMargins)
			{
				ABounds.X += FMarginLeft;
				ABounds.Y += FMarginTop;
				ABounds.Width -= (FMarginLeft + FMarginRight);
				ABounds.Height -= (FMarginTop + FMarginBottom);
			}
			InternalLayout(ABounds);
		}

		protected abstract void InternalLayout(Rectangle ABounds);

		#endregion

		#region MinSize

		// Cached minimum useful size
		private Size FMinSize = Size.Empty;

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual Size MinSize
		{ 
			get 
			{ 
				if (FMinSize == Size.Empty)
					FMinSize = InternalMinSize + GetOverhead();
				return FMinSize;
			}
		}
		
		protected virtual Size InternalMinSize
		{
			get { return InternalNaturalSize; }
		}

		#endregion

		#region MaxSize

		// Cached maximum useful size
		private Size FMaxSize = Size.Empty;

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual Size MaxSize
		{
			get 
			{
				if (FMaxSize == Size.Empty)
					FMaxSize = InternalMaxSize + GetOverhead();
				return FMaxSize;
			}
		}

		protected virtual Size InternalMaxSize
		{
			get { return InternalNaturalSize; }
		}

		#endregion

		#region NaturalSize

		// Cached natural size
		private Size FNaturalSize = Size.Empty;

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual Size NaturalSize
		{
			get 
			{ 
				if (FNaturalSize == Size.Empty)
					FNaturalSize = InternalNaturalSize + GetOverhead();
				return FNaturalSize;
			}
		}
		
		protected virtual Size InternalNaturalSize
		{
			get { return new Size(0, 0); }
		}

		#endregion

		#region Node

		protected override void Activate()
		{
			InternalUpdateToolTip();
			InternalUpdateVisible();
			InternalUpdateTabStop();
			InternalUpdateHelp();
			base.Activate();
		}

		#endregion

		#region Static Layout Utilities

		// <summary> Constrains a size to within specified size. </summary>
		public static void ConstrainMax(ref Size ASize, Size AMaxSize)
		{
			ConstrainMaxWidth(ref ASize, AMaxSize.Width);
			ConstrainMaxHeight(ref ASize, AMaxSize.Height);
		}

		// <summary> Grows a size to at least specified size. </summary>
		public static void ConstrainMin(ref Size ASize, Size AMinSize)
		{
			ConstrainMinWidth(ref ASize, AMinSize.Width);
			ConstrainMinHeight(ref ASize, AMinSize.Height);
		}

		public static void ConstrainMaxHeight(ref Size ASize, int AMaxValue)
		{
			if (ASize.Height > AMaxValue)
				ASize.Height = AMaxValue;
		}

		public static void ConstrainMinHeight(ref Size ASize, int AMinValue)
		{
			if (ASize.Height < AMinValue)
				ASize.Height = AMinValue;
		}

		public static void ConstrainMaxWidth(ref Size ASize, int AMaxValue)
		{
			if (ASize.Width > AMaxValue)
				ASize.Width = AMaxValue;
		}

		public static void ConstrainMinWidth(ref Size ASize, int AMinValue)
		{
			if (ASize.Width < AMinValue)
				ASize.Width = AMinValue;
		}

		// TODO: Localize this!

		/// <summary> Calculates the average character width for the font of a given control. </summary>
		public static int GetAverageCharPixelWidth(WinForms.Control AControl)
		{
			using (Graphics LGraphics = AControl.CreateGraphics())
				return (int)(LGraphics.MeasureString(CAverageChars, AControl.Font).Width / (float)CAverageChars.Length);
		}

		/// <summary> Calculates the Height for the font of a given control. </summary>
		public static int GetPixelHeight(WinForms.Control AControl)
		{
			return AControl.Font.Height;
		}

		#endregion
	}
}
