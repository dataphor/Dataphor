using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Trigger : Element, ITrigger 
    {		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}
		
		// IVerticalAlignedElement

		private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					UpdateBinding(FrameworkElement.VerticalAlignmentProperty);
				}
			}
		}

		protected override object UIGetVerticalAlignment()
		{
			return ConvertVerticalAlignment(_verticalAlignment);
		}

		// Button

		protected ButtonBase Button
		{
			get { return FrameworkElement as ButtonBase; }
		}
		
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new TriggerControl();
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();

			AddBinding(ContentControl.ContentProperty, new Func<object>(UIGetContent));
			AddBinding(TriggerControl.ImageProperty, new Func<object>(UIGetImage));
			AddBinding(TriggerControl.ImageWidthProperty, new Func<object>(UIGetImageWidth));			
			AddBinding(TriggerControl.ImageHeightProperty, new Func<object>(UIGetImageHeight));			
		}
		
		protected override void InitializeFrameworkElement()
		{
			base.InitializeFrameworkElement();

			Button.Click += new RoutedEventHandler(ButtonClick);
		}

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			Session.Invoke
			(
				(System.Action)
				(
					() =>
					{
						try
						{
							if (Action != null && GetEnabled())
								Action.Execute(this, new EventParams());
						}
						catch (Exception AException)
						{
							this.HandleException(AException);	//don't re-throw
						}
					}
				)
			);
		}
		
		// Action

		protected IAction _action;
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (_action != value)
				{
					if (_action != null)
					{
						_action.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged -= new EventHandler(ActionTextChanged);
						_action.OnImageChanged -= new EventHandler(ActionImageChanged);
						_action.OnHintChanged -= new EventHandler(ActionHintChanged);
						_action.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						_action.Disposed -= new EventHandler(ActionDisposed);
					}
					_action = value;
					if (_action != null)
					{
						_action.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged += new EventHandler(ActionTextChanged);
						_action.OnImageChanged += new EventHandler(ActionImageChanged);
						_action.OnHintChanged += new EventHandler(ActionHintChanged);
						_action.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						_action.Disposed += new EventHandler(ActionDisposed);
					}
					if (Active)
					{
						UpdateBindings
						(
							new DependencyProperty[]
							{
								FrameworkElement.VisibilityProperty,
								TriggerControl.ImageProperty,
								ContentControl.ContentProperty,
								Control.IsEnabledProperty,
								ToolTipService.ToolTipProperty
							}
						);
					}
				}
			}
		}
		
		protected void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}
		
		// Image

		private void ActionImageChanged(object sender, EventArgs args)
		{
			UpdateBinding(TriggerControl.ImageProperty);
		}
		
		private object UIGetImage()
		{
			return (Action == null ? null : ((Action)Action).LoadedImage);
		}
		
		// ImageWidth
		
		protected int _imageWidth = 0;
		[DefaultValue(0)]
		public int ImageWidth
		{
			get { return _imageWidth; }
			set
			{
				if (_imageWidth != value)
				{
					_imageWidth = value;
					UpdateBinding(TriggerControl.ImageWidthProperty);
				}
			}
		}

		private object UIGetImageWidth()
		{
			return (double)_imageWidth;
		}
		
		protected int _imageHeight = 0;
		[DefaultValue(0)]
		public int ImageHeight
		{
			get { return _imageHeight; }
			set
			{
				if (_imageHeight != value)
				{
					_imageHeight = value;
					UpdateBinding(TriggerControl.ImageHeightProperty);
				}
			}
		}

		private object UIGetImageHeight()
		{
			return (double)_imageHeight;
		}
		
		// Text

		private string _text = String.Empty;
		[DefaultValue("")]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					UpdateBinding(ContentControl.ContentProperty);
				}
			}
		}

		private void ActionTextChanged(object sender, EventArgs args)
		{
			if (_text == String.Empty)
				UpdateBinding(ContentControl.ContentProperty);
		}

		public virtual string GetText()
		{
			return 
				(Action != null) && (_text == String.Empty)
					? Action.Text
					: _text;
		}

		private object UIGetContent()
		{
			return GetText();
		}
		
		// Enabled

		private bool _enabled = true;
		[DefaultValue(true)]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					UpdateBinding(Control.IsEnabledProperty);
				}
			}
		}
		
		/// <summary> Gets whether the node is actually enabled (accounting for action). </summary>
		/// <remarks>
		///		The enabled state of the node is the most restrictive between 
		///		the action and the Enabled property.
		///	</remarks>
		public override bool GetEnabled()
		{
			return ( Action == null ? false : Action.GetEnabled() ) && _enabled && base.GetEnabled();
		}

		private void ActionEnabledChanged(object sender, EventArgs args)
		{
			UpdateBinding(Control.IsEnabledProperty);
		}

		// Hint/Tooltip

		public override string GetHint()
		{
			if (Hint != String.Empty)
				return base.GetHint();
			else if (Action != null)
				return Action.Hint;
			else
				return String.Empty;
		}

		private void ActionHintChanged(object sender, EventArgs args)
		{
			UpdateBinding(ToolTipService.ToolTipProperty);
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((Action == null) || Action.Visible);
		}

		private void ActionVisibleChanged(object sender, EventArgs args)
		{
			UpdateBinding(FrameworkElement.VisibilityProperty);
		}

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
	}
}
