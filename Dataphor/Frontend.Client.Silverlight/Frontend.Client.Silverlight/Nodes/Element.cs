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
	public abstract class Element : Node, ISilverlightElement
	{
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

		#region Framework Element
		
		private FrameworkElement _frameworkElement;
		
		public FrameworkElement FrameworkElement
		{
			get { return _frameworkElement; }
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		protected abstract FrameworkElement CreateFrameworkElement();

		/// <remarks> This method is invoked on the main thread while the session thread is waiting. </remarks>
		protected virtual void InitializeFrameworkElement()
		{
			if (Parent != null)
			{
				var parentContainer = Parent as ISilverlightContainerElement;
				var index = Parent.Children.IndexOf(this);
				if (parentContainer != null)
					parentContainer.InsertChild(index, _frameworkElement);
			}
		}
		
		/// <remarks> This method is invoked on the main thread while the session thread is waiting. </remarks>
		protected virtual void DeinitializeFrameworkElement()
		{
			// Remove this control from the parent if 
			if (Parent != null)
			{
				var parentContainer = Parent as ISilverlightContainerElement;
				if (parentContainer != null && Parent.Active)
					parentContainer.RemoveChild(_frameworkElement);
			}
		}
		
		#endregion
		
		#region Binding
		
		private Dictionary<DependencyProperty, Func<object>> _bindings = new Dictionary<DependencyProperty, Func<object>>();
		
		protected void AddBinding(DependencyProperty property, Func<object> getter)
		{
			_bindings.Add(property, getter);
			UpdateBinding(property);
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		private void InternalRemoveBinding(FrameworkElement element, DependencyProperty property)
		{
			element.ClearValue(property);
		}
		
		protected void RemoveBinding(DependencyProperty property)
		{
			_bindings.Remove(property);
			if (Active)
				Session.DispatcherInvoke(new Action<FrameworkElement, DependencyProperty>(InternalRemoveBinding), FrameworkElement, property);
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		private void InternalUpdateBinding(FrameworkElement element, DependencyProperty property, object tempValue)
		{
			element.SetValue(property, tempValue);
		}
		
		protected void UpdateBinding(DependencyProperty property)
		{
			Func<object> getter;
			if (Active && _bindings.TryGetValue(property, out getter))
				Session.DispatcherInvoke(new Action<FrameworkElement, DependencyProperty, object>(InternalUpdateBinding), FrameworkElement, property, getter());
		}
		
		protected void UpdateBindings(DependencyProperty[] properties)
		{
			var element = FrameworkElement;
			if (element != null)
			{
				object[] values = new object[properties.Length];
				for (int i = 0; i < properties.Length; i++)
					values[i] = _bindings[properties[i]]();
					
				InternalUpdateBindings(element, properties, values);
			}
		}
		
		protected void UpdateAllBindings()
		{
			var element = FrameworkElement;
			if (element != null)
			{
				// Snapshot the properties and values on the main thread to carry across threads
				DependencyProperty[] properties = new DependencyProperty[_bindings.Count];
				object[] values = new object[_bindings.Count];
				int index = 0;
				foreach (KeyValuePair<DependencyProperty, Func<object>> entry in _bindings)
				{
					properties[index] = entry.Key;
					values[index] = entry.Value();
					index++;
				}

				InternalUpdateBindings(element, properties, values);
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
			if (_frameworkElement is Control)
			{
				AddBinding(Control.IsTabStopProperty, new Func<object>(UIGetIsTabStop));
				AddBinding(Control.IsEnabledProperty, new Func<object>(UIGetIsEnabled));
			}
		}
		
		#endregion
		
		#region Styles
		
		private string _style = "";
		[DefaultValue("")]
		public string Style
		{
			get { return _style; }
			set
			{
				if (_style != value)
				{
					_style = value;
					UpdateBinding(FrameworkElement.StyleProperty);
				}
			}
		}
		
		protected virtual string GetStyle()
		{
			if (!String.IsNullOrEmpty(_style))
				return _style;
			return GetDefaultStyle();
		}

		protected virtual string GetDefaultStyle()
		{
			return null;
		}
		
		private object UIGetStyle()
		{
			var styleName = GetStyle();
			if (String.IsNullOrEmpty(styleName))
				return null;
			else
				return Session.DispatchAndWait((System.Func<object>)(() => { return Application.Current.Resources[styleName]; }));
		}
		
		#endregion
		
		#region Alignment

		protected System.Windows.VerticalAlignment ConvertVerticalAlignment(VerticalAlignment tempValue)
		{
			switch (tempValue)
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
		
		protected System.Windows.HorizontalAlignment ConvertHorizontalAlignment(HorizontalAlignment tempValue)
		{
			switch (tempValue)
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

		private bool _visible = true;
		[DefaultValue(true)]
		public bool Visible
		{
			get { return _visible; }
			set
			{
				if (_visible != value)
				{
					_visible = value;
					UpdateBinding(FrameworkElement.VisibilityProperty);
				}
			}
		}
		
		public virtual bool GetVisible()
		{
			return _visible;
		}
		
		private object UIGetVisibility()
		{
			return GetVisible() ? Visibility.Visible : Visibility.Collapsed;
		}
		
		#endregion

		#region TabStop

		private bool _tabStop;
		[DefaultValueMember("GetDefaultTabStop")]
		public bool TabStop
		{
			get { return _tabStop; }
			set 
			{ 
				if (_tabStop != value)
				{
					_tabStop = value;
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
			return _tabStop;
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
			Element parent = Parent as Element;
			if (parent != null)
				return parent.GetEnabled();
			else
				return true;
		}

		private object UIGetIsEnabled()
		{
			return GetEnabled();
		}

		#endregion

		#region Hint

		private string _hint = String.Empty;
		[DefaultValue("")]
		public string Hint
		{
			get { return _hint; }
			set
			{
				if (_hint != value)
				{
					_hint = value;
					UpdateBinding(ToolTipService.ToolTipProperty);
				}
			}
		}

		public virtual string GetHint()
		{
			return _hint;
		}
		
		private object UIGetToolTip()
		{
			var hint = GetHint();
			return String.IsNullOrEmpty(hint) ? null : hint;
		}

		#endregion

		#region Help

		// TODO: Support for help
		
		private string _helpKeyword = String.Empty;
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return _helpKeyword; }
			set { _helpKeyword = (value == null ? String.Empty : value); }
		}
		
		private HelpKeywordBehavior _helpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return _helpKeywordBehavior; }
			set { _helpKeywordBehavior = value; }
		}

		private string _helpString = String.Empty;
		[DefaultValue("")]
		public string HelpString
		{ 
			get { return _helpString; }
			set { _helpString = (value == null ? String.Empty : value); }
		}

		#endregion

		#region Margins

		// SuppressMargins

		private bool _suppressMargins;
		[Publish(PublishMethod.None)]
		public bool SuppressMargins
		{
			get { return _suppressMargins; }
			set 
			{
				if (_suppressMargins != value)
				{
					_suppressMargins = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		// MarginLeft
		
		private int _marginLeft;
		[DefaultValueMember("GetDefaultMarginLeft")]
		public int MarginLeft
		{
			get { return _marginLeft; }
			set
			{
				if (_marginLeft != value)
				{
					_marginLeft = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginLeft()
		{
			return DefaultMarginLeft;
		}
		
		// MarginRight
		
		private int _marginRight;
		[DefaultValueMember("GetDefaultMarginRight")]
		public int MarginRight
		{
			get { return _marginRight; }
			set
			{
				if (_marginRight != value)
				{
					_marginRight = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginRight()
		{
			return DefaultMarginRight;
		}
		
		// MarginTop
		
		private int _marginTop;
		[DefaultValueMember("GetDefaultMarginTop")]
		public int MarginTop
		{
			get { return _marginTop; }
			set
			{
				if (_marginTop != value)
				{
					_marginTop = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginTop()
		{
			return DefaultMarginTop;
		}
		
		// MarginBottom
		
		private int _marginBottom;
		[DefaultValueMember("GetDefaultMarginBottom")]
		public int MarginBottom
		{
			get { return _marginBottom; }
			set
			{
				if (_marginBottom != value)
				{
					_marginBottom = value;
					UpdateBinding(FrameworkElement.MarginProperty);
				}
			}
		}

		public virtual int GetDefaultMarginBottom()
		{
			return DefaultMarginBottom;
		}

		private object UIGetMargin()
		{
			return
				_suppressMargins
					? new Thickness(0)
					: new Thickness(_marginLeft, _marginTop, _marginRight, _marginBottom); 
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
						_frameworkElement = CreateFrameworkElement();
						InitializeFrameworkElement();
					}
				)
			);
			RegisterBindings();
			UpdateAllBindings();
			base.Activate();
		}

		protected override void Deactivate()
		{
			base.Deactivate();
			Session.DispatchAndWait(new System.Action(DeinitializeFrameworkElement));
		}

		#endregion
	}
}
