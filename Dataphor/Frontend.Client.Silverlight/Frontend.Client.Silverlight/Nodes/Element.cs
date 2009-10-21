/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Controls;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Base node for all visible nodes. </summary>
	public abstract class Element : Node, IElement, INotifyPropertyChanged
	{
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

		#region Framework Element
		
		private FrameworkElement FFrameworkElement;
		
		public FrameworkElement FrameworkElement
		{
			get { return FFrameworkElement; }
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		protected virtual void InitializeFrameworkElement()
		{
			var LBinding = new Binding("BindMargin");
			LBinding.Source = this;
			FFrameworkElement.SetBinding(FrameworkElement.MarginProperty, LBinding);
			
			LBinding = new Binding("BindVisibility");
			LBinding.Source = this;
			FFrameworkElement.SetBinding(FrameworkElement.VisibilityProperty, LBinding);
			
			LBinding = new Binding("BindToolTip");
			LBinding.Source = this;
			FFrameworkElement.SetBinding(ToolTipService.ToolTipProperty, LBinding);
			
			var LControl = FFrameworkElement as Control;
			if (LControl != null)
			{
				LBinding = new Binding("BindIsTabStop");
				LBinding.Source = this;
				LControl.SetBinding(Control.IsTabStopProperty, LBinding);
				
				LBinding = new Binding("BindStyle");
				LBinding.Source = this;
				LControl.SetBinding(Control.StyleProperty, LBinding);
			}
			
			if (Parent != null)
			{
				var LParentContainer = Parent as ISilverlightContainerElement;
				if (LParentContainer != null)
					LParentContainer.AddChild(FFrameworkElement);
			}
		}
		
		protected abstract FrameworkElement CreateFrameworkElement();

		#endregion
		
		#region Styles
		
		private string FStyle = "";
		
		[DefaultValue("")]
		public string Style
		{
			get { return FStyle; }
			set
			{
				if (FStyle != value)
				{
					FStyle = value;
					UpdateStyle();
				}
			}
		}
		
		protected virtual string GetStyle()
		{
			if (!String.IsNullOrEmpty(FStyle))
				return FStyle;
			return GetDefaultStyle();
		}

		protected virtual string GetDefaultStyle()
		{
			return null;
		}
		
		protected void UpdateStyle()
		{
			var LStyleName = GetStyle();
			if (String.IsNullOrEmpty(LStyleName))
				BindStyle = null;
			else
				Session.DispatcherInvoke((System.Action)(() => { BindStyle = Application.Current.Resources[LStyleName] as Style; }));
		}
		
		private Style FBindStyle;
		
		public Style BindStyle
		{
			get { return FBindStyle; }
			private set
			{
				if (FBindStyle != value)
				{
					FBindStyle = value;
					NotifyPropertyChanged("BindStyle");
				}
			}
		}
		
		#endregion
		
		#region Binding
		
		public event PropertyChangedEventHandler PropertyChanged;
	
		/// <summary> Invokes a property changes notification on the main (UI) thread. </summary>
		protected void NotifyPropertyChanged(string AName)
		{
			if (PropertyChanged != null)
			{
				var LDispatcher = Session.CheckedDispatcher;
				if (LDispatcher.CheckAccess())
					PropertyChanged(this, new PropertyChangedEventArgs(AName));
				else
					Session.DispatcherInvoke(PropertyChanged, this, new PropertyChangedEventArgs(AName));
			}
		}

		#endregion
		
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
					NotifyPropertyChanged("BindVisibility");
				}
			}
		}
		
		public bool GetVisible()
		{
			return false;	// unused, part of IVisible interface for other clients
		}
		
		public Visibility BindVisibility
		{
			get { return FVisible ? Visibility.Visible : Visibility.Collapsed; }
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
					UpdateTabStop();
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

		protected void UpdateTabStop() 
		{
			BindIsTabStop = GetTabStop();
		}
		
		private bool FBindIsTabStop;
		
		public bool BindIsTabStop
		{
			get { return FBindIsTabStop; }
			private set
			{
				if (FBindIsTabStop != value)
				{
					FBindIsTabStop = value;
					NotifyPropertyChanged("BindIsTabStop");
				}
			}
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

		protected void UpdateToolTip()
		{
			BindToolTip = GetHint();
		}

		private string FBindToolTip;
		
		public string BindToolTip
		{
			get { return FBindToolTip; }
			private set
			{
				if (FBindToolTip != value)
				{
					FBindToolTip = value;
					NotifyPropertyChanged("BindToolTip");
				}
			}
		}

		#endregion

		#region Help

		// TODO: Support for help
		
		private string FHelpKeyword = String.Empty;
		[Description("The help keyword to navigate to when the user activates help.")]
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return FHelpKeyword; }
			set { FHelpKeyword = (value == null ? String.Empty : value); }
		}
		
		private HelpKeywordBehavior FHelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		[Description("Determines the method used to navigate the the help identified by the HelpKeyword property.")]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return FHelpKeywordBehavior; }
			set { FHelpKeywordBehavior = value; }
		}

		private string FHelpString = String.Empty;
		[Description("Specifies the help text to display to the user when keyword navigation is not used.")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("txt")]
		public string HelpString
		{ 
			get { return FHelpString; }
			set { FHelpString = (value == null ? String.Empty : value); }
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
					UpdateMargins();
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
					UpdateMargins();
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
					UpdateMargins();
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
					UpdateMargins();
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
					UpdateMargins();
				}
			}
		}

		public virtual int GetDefaultMarginBottom()
		{
			return CDefaultMarginBottom;
		}

		// BindMargin
				
		private Thickness FBindMargin;
		
		public Thickness BindMargin
		{
			get { return FBindMargin; }
			private set
			{
				if (FBindMargin != value)
				{
					FBindMargin = value;
					NotifyPropertyChanged("BindMargin");
				}
			}
		}

		private void UpdateMargins()
		{
			BindMargin =
				FSuppressMargins
					? new Thickness(0)
					: new Thickness(FMarginLeft, FMarginTop, FMarginRight, FMarginBottom); 
		}

		#endregion

		#region Node

		protected override void Activate()
		{
			UpdateToolTip();
			UpdateTabStop();
			UpdateMargins();
			UpdateStyle();
			Session.DispatchAndWait
			(
				(System.Action)
				(
					() =>
					{
						FFrameworkElement = CreateFrameworkElement();
						InitializeFrameworkElement();
					}
				)
			);
			base.Activate();
		}

		#endregion
	}
}
