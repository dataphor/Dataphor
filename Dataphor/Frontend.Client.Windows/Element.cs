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
		public const string AverageChars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		
		public const int DefaultMarginLeft = 2;
		public const int DefaultMarginRight = 2;
		public const int DefaultMarginTop = 2;
		public const int DefaultMarginBottom = 2;

		public Element() : base()
		{
			_marginLeft = GetDefaultMarginLeft();
			_marginRight = GetDefaultMarginRight();
			_marginTop = GetDefaultMarginTop();
			_marginBottom = GetDefaultMarginBottom();
			_tabStop = GetDefaultTabStop();
		}

		#region Visible

		private bool _visible = true;
		[DefaultValue(true)]
		[Description("When set to false the control will not be shown.")]
		public bool Visible
		{
			get { return _visible; }
			set
			{
				if (_visible != value)
				{
					_visible = value;
					VisibleChanged();
				}
			}
		}

		public virtual bool GetVisible() 
		{
			return _visible && ( Parent is IElement ? ((IElement)Parent).GetVisible() : true );
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
					IWindowsElement visualChild;
					foreach (INode child in Children)
					{
						visualChild = child as IWindowsElement;
						if (visualChild != null)
							visualChild.VisibleChanged();
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

		private bool _tabStop;
		[Description("Determines if the control can be focused through tab key navigation.")]
		[DefaultValueMember("GetDefaultTabStop")]
		public bool TabStop
		{
			get { return _tabStop; }
			set 
			{ 
				if (_tabStop != value)
				{
					_tabStop = value;
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
			return _tabStop;
		}

		protected virtual void InternalUpdateTabStop() { }

		protected virtual void TabStopChanged() 
		{
			if (Active)
				InternalUpdateTabStop();
		}

		#endregion

		#region Hint

		private string _hint = String.Empty;
		[Browsable(true)]
		[DefaultValue("")]
		[Description("Text which describes the element to the end-user.")]
		public string Hint
		{
			get { return _hint; }
			set
			{
				if (_hint != value)
				{
					_hint = value;
					UpdateToolTip();
				}
			}
		}

		public virtual string GetHint()
		{
			return _hint;
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

		protected void SetToolTip(WinForms.Control control)
		{
			Client.IHost host = HostNode;
			if ((host != null) && (host.Session != null))
				((Windows.Session)host.Session).ToolTip.SetToolTip(control, GetHint());
		}

		#endregion

		#region Help

		protected virtual void InternalUpdateHelp() {}

		protected virtual void UpdateHelp()
		{
			if (Active)
				InternalUpdateHelp();
		}

		private string _helpKeyword = String.Empty;
		[Description("The help keyword to navigate to when the user activates help.")]
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return _helpKeyword; }
			set 
			{ 
				if (value != _helpKeyword)
				{
					_helpKeyword = (value == null ? String.Empty : value);
					UpdateHelp();
				}
			}
		}
		
		private HelpKeywordBehavior _helpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		[Description("Determines the method used to navigate the the help identified by the HelpKeyword property.")]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return _helpKeywordBehavior; }
			set 
			{ 
				if (value != _helpKeywordBehavior)
				{
					_helpKeywordBehavior = value; 
					UpdateHelp();
				}
			}
		}

		private string _helpString = String.Empty;
		[Description("Specifies the help text to display to the user when keyword navigation is not used.")]
		[DefaultValue("")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("txt")]
		public string HelpString
		{ 
			get { return _helpString; }
			set 
			{ 
				if (value != _helpString)
				{
					_helpString = (value == null ? String.Empty : value); 
					UpdateHelp();
				}
			}
		}

		#endregion
		
		#region Styles
		
		private string _style = "";
		
		// TODO: Implement styles in the Windows Client
		[DefaultValue("")]
		public string Style
		{
			get { return _style; }
			set
			{
				if (_style != value)
				{
					_style = value;
				}
			}
		}
		
		#endregion

		#region Margins

		// SuppressMargins

		private bool _suppressMargins;
		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public bool SuppressMargins
		{
			get { return _suppressMargins; }
			set 
			{
				if (_suppressMargins != value)
				{
					_suppressMargins = value;
					UpdateLayout();
				}
			}
		}

		// MarginLeft
		
		private int _marginLeft;
		[Description("The left margin for the control")]
		[DefaultValueMember("GetDefaultMarginLeft")]
		public int MarginLeft
		{
			get { return _marginLeft; }
			set
			{
				if (_marginLeft != value)
				{
					_marginLeft = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginLeft()
		{
			return DefaultMarginLeft;
		}
		
		// MarginRight
		
		private int _marginRight;
		[Description("The right margin for the control")]
		[DefaultValueMember("GetDefaultMarginRight")]
		public int MarginRight
		{
			get { return _marginRight; }
			set
			{
				if (_marginRight != value)
				{
					_marginRight = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginRight()
		{
			return DefaultMarginRight;
		}
		
		// MarginTop
		
		private int _marginTop;
		[Description("The top margin for the control")]
		[DefaultValueMember("GetDefaultMarginTop")]
		public int MarginTop
		{
			get { return _marginTop; }
			set
			{
				if (_marginTop != value)
				{
					_marginTop = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginTop()
		{
			return DefaultMarginTop;
		}
		
		// MarginBottom
		
		private int _marginBottom;
		[Description("The bottom margin for the control")]
		[DefaultValueMember("GetDefaultMarginBottom")]
		public int MarginBottom
		{
			get { return _marginBottom; }
			set
			{
				if (_marginBottom != value)
				{
					_marginBottom = value;
					UpdateLayout();
				}
			}
		}

		public virtual int GetDefaultMarginBottom()
		{
			return DefaultMarginBottom;
		}
		
		#endregion

		#region Layout

		/// <summary> Call anytime subsequent layouts should recalcuate the sizes. </summary>
		protected virtual void InvalidateLayoutCache()
		{
			_minSize = Size.Empty;
			_maxSize = Size.Empty;
			_naturalSize = Size.Empty;
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
			IUpdateHandler handler = (IUpdateHandler)FindParent(typeof(IUpdateHandler));
			if (handler != null)
				handler.BeginUpdate();
		}

		public virtual void EndUpdate(bool performLayout)
		{
			IUpdateHandler handler = (IUpdateHandler)FindParent(typeof(IUpdateHandler));
			if (handler != null)
				handler.EndUpdate(performLayout);
		}

		// GetOverhead()

		public virtual Size GetOverhead()
		{
			if (_suppressMargins)
				return Size.Empty;
			else
				return new Size(_marginLeft + _marginRight, _marginTop + _marginBottom);
		}

		// InternalLayout

		public void Layout(Rectangle bounds) 
		{
			if (!_suppressMargins)
			{
				bounds.X += _marginLeft;
				bounds.Y += _marginTop;
				bounds.Width -= (_marginLeft + _marginRight);
				bounds.Height -= (_marginTop + _marginBottom);
			}
			InternalLayout(bounds);
		}

		protected abstract void InternalLayout(Rectangle bounds);

		#endregion

		#region MinSize

		// Cached minimum useful size
		private Size _minSize = Size.Empty;

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual Size MinSize
		{ 
			get 
			{ 
				if (_minSize == Size.Empty)
					_minSize = InternalMinSize + GetOverhead();
				return _minSize;
			}
		}
		
		protected virtual Size InternalMinSize
		{
			get { return InternalNaturalSize; }
		}

		#endregion

		#region MaxSize

		// Cached maximum useful size
		private Size _maxSize = Size.Empty;

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual Size MaxSize
		{
			get 
			{
				if (_maxSize == Size.Empty)
					_maxSize = InternalMaxSize + GetOverhead();
				return _maxSize;
			}
		}

		protected virtual Size InternalMaxSize
		{
			get { return InternalNaturalSize; }
		}

		#endregion

		#region NaturalSize

		// Cached natural size
		private Size _naturalSize = Size.Empty;

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual Size NaturalSize
		{
			get 
			{ 
				if (_naturalSize == Size.Empty)
					_naturalSize = InternalNaturalSize + GetOverhead();
				return _naturalSize;
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
		public static void ConstrainMax(ref Size size, Size maxSize)
		{
			ConstrainMaxWidth(ref size, maxSize.Width);
			ConstrainMaxHeight(ref size, maxSize.Height);
		}

		// <summary> Grows a size to at least specified size. </summary>
		public static void ConstrainMin(ref Size size, Size minSize)
		{
			ConstrainMinWidth(ref size, minSize.Width);
			ConstrainMinHeight(ref size, minSize.Height);
		}

		public static void ConstrainMaxHeight(ref Size size, int maxValue)
		{
			if (size.Height > maxValue)
				size.Height = maxValue;
		}

		public static void ConstrainMinHeight(ref Size size, int minValue)
		{
			if (size.Height < minValue)
				size.Height = minValue;
		}

		public static void ConstrainMaxWidth(ref Size size, int maxValue)
		{
			if (size.Width > maxValue)
				size.Width = maxValue;
		}

		public static void ConstrainMinWidth(ref Size size, int minValue)
		{
			if (size.Width < minValue)
				size.Width = minValue;
		}

		// TODO: Localize this!

		/// <summary> Calculates the average character width for the font of a given control. </summary>
		public static int GetAverageCharPixelWidth(WinForms.Control control)
		{
			using (Graphics graphics = control.CreateGraphics())
				return (int)(graphics.MeasureString(AverageChars, control.Font).Width / (float)AverageChars.Length);
		}

		/// <summary> Calculates the Height for the font of a given control. </summary>
		public static int GetPixelHeight(WinForms.Control control)
		{
			return control.Font.Height;
		}

		#endregion
	}
}
