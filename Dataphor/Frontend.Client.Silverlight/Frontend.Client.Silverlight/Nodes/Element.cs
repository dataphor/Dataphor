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
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Base node for all visible nodes. </summary>
	public abstract class Element : Node, IElement
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
		protected abstract FrameworkElement CreateFrameworkElement();

		/// <remarks> This method is invoked on the main thread. </remarks>
		protected virtual void InitializeFrameworkElement()
		{
			if (Parent != null)
			{
				var LParentContainer = Parent as ISilverlightContainerElement;
				var LIndex = Parent.Children.IndexOf(this);
				if (LParentContainer != null)
					LParentContainer.InsertChild(LIndex, FFrameworkElement);
			}
		}
		
		#endregion
		
		#region Binding
		
		private Dictionary<DependencyProperty, Func<object>> FBindings = new Dictionary<DependencyProperty, Func<object>>();
		
		protected void AddBinding(DependencyProperty AProperty, Func<object> AGetter)
		{
			FBindings.Add(AProperty, AGetter);
			UpdateBinding(AProperty);
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		private void InternalRemoveBinding(FrameworkElement AElement, DependencyProperty AProperty)
		{
			AElement.ClearValue(AProperty);
		}
		
		protected void RemoveBinding(DependencyProperty AProperty)
		{
			FBindings.Remove(AProperty);
			if (Active)
				Session.DispatcherInvoke(new Action<FrameworkElement, DependencyProperty>(InternalRemoveBinding), FrameworkElement, AProperty);
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		private void InternalUpdateBinding(FrameworkElement AElement, DependencyProperty AProperty, object AValue)
		{
			AElement.SetValue(AProperty, AValue);
		}
		
		protected void UpdateBinding(DependencyProperty AProperty)
		{
			Func<object> LGetter;
			if (Active && FBindings.TryGetValue(AProperty, out LGetter))
				Session.DispatcherInvoke(new Action<FrameworkElement, DependencyProperty, object>(InternalUpdateBinding), FrameworkElement, AProperty, LGetter());
		}
		
		protected void UpdateBindings(DependencyProperty[] AProperties)
		{
			var LElement = FrameworkElement;
			if (LElement != null)
			{
				object[] LValues = new object[AProperties.Length];
				for (int i = 0; i < AProperties.Length; i++)
					LValues[i] = FBindings[AProperties[i]]();
					
				InternalUpdateBindings(LElement, AProperties, LValues);
			}
		}
		
		protected void UpdateAllBindings()
		{
			var LElement = FrameworkElement;
			if (LElement != null)
			{
				// Snapshot the properties and values on the main thread to carry across threads
				DependencyProperty[] LProperties = new DependencyProperty[FBindings.Count];
				object[] LValues = new object[FBindings.Count];
				int LIndex = 0;
				foreach (KeyValuePair<DependencyProperty, Func<object>> LEntry in FBindings)
				{
					LProperties[LIndex] = LEntry.Key;
					LValues[LIndex] = LEntry.Value();
					LIndex++;
				}

				InternalUpdateBindings(LElement, LProperties, LValues);
			}
		}

		private void InternalUpdateBindings(FrameworkElement LElement, DependencyProperty[] LProperties, object[] LValues)
		{
			Session.DispatcherInvoke
			(
				(System.Action)
				(
					() =>
					{
						for (int i = 0; i < LProperties.Length; i++)
							InternalUpdateBinding(LElement, LProperties[i], LValues[i]);
					}
				)
			);
		}
		
		/// <remarks> This method is invoked on the session thread. </remarks>
		protected virtual void RegisterBindings()
		{
			AddBinding(FrameworkElement.MarginProperty, new Func<object>(UIGetMargin));
			AddBinding(FrameworkElement.VisibilityProperty, new Func<object>(UIGetVisibility));
			AddBinding(ToolTipService.ToolTipProperty, new Func<object>(UIGetToolTip));
			AddBinding(FrameworkElement.VerticalAlignmentProperty, new Func<object>(UIGetVerticalAlignment));
			AddBinding(FrameworkElement.HorizontalAlignmentProperty, new Func<object>(UIGetHorizontalAlignment));
			AddBinding(FrameworkElement.StyleProperty, new Func<object>(UIGetStyle));
			if (FFrameworkElement is Control)
			{
				AddBinding(Control.IsTabStopProperty, new Func<object>(UIGetIsTabStop));
				AddBinding(Control.IsEnabledProperty, new Func<object>(UIGetIsEnabled));
			}
		}
		
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
					UpdateBinding(FrameworkElement.StyleProperty);
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
		
		private object UIGetStyle()
		{
			var LStyleName = GetStyle();
			if (String.IsNullOrEmpty(LStyleName))
				return null;
			else
				return Session.DispatchAndWait((System.Func<object>)(() => { return Application.Current.Resources[LStyleName]; }));
		}
		
		#endregion
		
		#region Alignment

		protected System.Windows.VerticalAlignment ConvertVerticalAlignment(VerticalAlignment AValue)
		{
			switch (AValue)
			{
				case VerticalAlignment.Bottom : return System.Windows.VerticalAlignment.Bottom;
				case VerticalAlignment.Top : return System.Windows.VerticalAlignment.Top;
				default : /* case VerticalAlignment.Middle : */ return System.Windows.VerticalAlignment.Center;
			}
		}
		
		protected virtual object UIGetVerticalAlignment()
		{
			return System.Windows.VerticalAlignment.Stretch;
		}
		
		protected System.Windows.HorizontalAlignment ConvertHorizontalAlignment(HorizontalAlignment AValue)
		{
			switch (AValue)
			{
				case HorizontalAlignment.Left : return System.Windows.HorizontalAlignment.Left;
				case HorizontalAlignment.Right : return System.Windows.HorizontalAlignment.Right;
				default : /* case HorizontalAlignment.Middle : */ return System.Windows.HorizontalAlignment.Center;
			}
		}
		
		protected virtual object UIGetHorizontalAlignment()
		{
			return System.Windows.HorizontalAlignment.Stretch;
		}
		

		#endregion
		
		#region Visible

		private bool FVisible = true;
		[DefaultValue(true)]
		public bool Visible
		{
			get { return FVisible; }
			set
			{
				if (FVisible != value)
				{
					FVisible = value;
					UpdateBinding(FrameworkElement.VisibilityProperty);
				}
			}
		}
		
		public virtual bool GetVisible()
		{
			return FVisible;
		}
		
		private object UIGetVisibility()
		{
			return GetVisible() ? Visibility.Visible : Visibility.Collapsed;
		}
		
		#endregion

		#region TabStop

		private bool FTabStop;
		[DefaultValueMember("GetDefaultTabStop")]
		public bool TabStop
		{
			get { return FTabStop; }
			set 
			{ 
				if (FTabStop != value)
				{
					FTabStop = value;
					UpdateBinding(Control.IsTabStopProperty);
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
		
		private object UIGetIsTabStop()
		{
			return GetTabStop();
		}

		#endregion
		
		#region Enabled
		
		/// <summary> Gets whether the node is actually enabled. </summary>
		public virtual bool GetEnabled()
		{
			Element LParent = Parent as Element;
			if (LParent != null)
				return LParent.GetEnabled();
			else
				return true;
		}

		private object UIGetIsEnabled()
		{
			return GetEnabled();
		}

		#endregion

		#region Hint

		private string FHint = String.Empty;
		[DefaultValue("")]
		public string Hint
		{
			get { return FHint; }
			set
			{
				if (FHint != value)
				{
					FHint = value;
					UpdateBinding(ToolTipService.ToolTipProperty);
				}
			}
		}

		public virtual string GetHint()
		{
			return FHint;
		}
		
		private object UIGetToolTip()
		{
			var LHint = GetHint();
			return String.IsNullOrEmpty(LHint) ? null : LHint;
		}

		#endregion

		#region Help

		// TODO: Support for help
		
		private string FHelpKeyword = String.Empty;
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return FHelpKeyword; }
			set { FHelpKeyword = (value == null ? String.Empty : value); }
		}
		
		private HelpKeywordBehavior FHelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return FHelpKeywordBehavior; }
			set { FHelpKeywordBehavior = value; }
		}

		private string FHelpString = String.Empty;
		[DefaultValue("")]
		public string HelpString
		{ 
			get { return FHelpString; }
			set { FHelpString = (value == null ? String.Empty : value); }
		}

		#endregion

		#region Margins

		// SuppressMargins

		private bool FSuppressMargins;
		[Publish(PublishMethod.None)]
		public bool SuppressMargins
		{
			get { return FSuppressMargins; }
			set 
			{
				if (FSuppressMargins != value)
				{
					FSuppressMargins = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		// MarginLeft
		
		private int FMarginLeft;
		[DefaultValueMember("GetDefaultMarginLeft")]
		public int MarginLeft
		{
			get { return FMarginLeft; }
			set
			{
				if (FMarginLeft != value)
				{
					FMarginLeft = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginLeft()
		{
			return CDefaultMarginLeft;
		}
		
		// MarginRight
		
		private int FMarginRight;
		[DefaultValueMember("GetDefaultMarginRight")]
		public int MarginRight
		{
			get { return FMarginRight; }
			set
			{
				if (FMarginRight != value)
				{
					FMarginRight = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginRight()
		{
			return CDefaultMarginRight;
		}
		
		// MarginTop
		
		private int FMarginTop;
		[DefaultValueMember("GetDefaultMarginTop")]
		public int MarginTop
		{
			get { return FMarginTop; }
			set
			{
				if (FMarginTop != value)
				{
					FMarginTop = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginTop()
		{
			return CDefaultMarginTop;
		}
		
		// MarginBottom
		
		private int FMarginBottom;
		[DefaultValueMember("GetDefaultMarginBottom")]
		public int MarginBottom
		{
			get { return FMarginBottom; }
			set
			{
				if (FMarginBottom != value)
				{
					FMarginBottom = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginBottom()
		{
			return CDefaultMarginBottom;
		}

		private object UIGetMargin()
		{
			return
				FSuppressMargins
					? new Thickness(0)
					: new Thickness(FMarginLeft, FMarginTop, FMarginRight, FMarginBottom); 
		}

		#endregion

		#region Node

		protected override void Activate()
		{
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
			RegisterBindings();
			UpdateAllBindings();
			base.Activate();
		}

		#endregion
	}
}
