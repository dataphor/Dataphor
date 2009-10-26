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
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}
		
		// IVerticalAlignedElement

		private VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
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
					UpdateBinding(FrameworkElement.VerticalAlignmentProperty);
				}
			}
		}

		protected override object UIGetVerticalAlignment()
		{
			return ConvertVerticalAlignment(FVerticalAlignment);
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

		protected IAction FAction;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (FAction != value)
				{
					if (FAction != null)
					{
						FAction.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged -= new EventHandler(ActionTextChanged);
						FAction.OnImageChanged -= new EventHandler(ActionImageChanged);
						FAction.OnHintChanged -= new EventHandler(ActionHintChanged);
						FAction.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						FAction.Disposed -= new EventHandler(ActionDisposed);
					}
					FAction = value;
					if (FAction != null)
					{
						FAction.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged += new EventHandler(ActionTextChanged);
						FAction.OnImageChanged += new EventHandler(ActionImageChanged);
						FAction.OnHintChanged += new EventHandler(ActionHintChanged);
						FAction.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						FAction.Disposed += new EventHandler(ActionDisposed);
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
		
		protected void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}
		
		// Image

		private void ActionImageChanged(object ASender, EventArgs AArgs)
		{
			UpdateBinding(TriggerControl.ImageProperty);
		}
		
		private object UIGetImage()
		{
			return (Action == null ? null : ((Action)Action).LoadedImage);
		}
		
		// ImageWidth
		
		protected int FImageWidth = 0;
		[DefaultValue(0)]
		public int ImageWidth
		{
			get { return FImageWidth; }
			set
			{
				if (FImageWidth != value)
				{
					FImageWidth = value;
					UpdateBinding(TriggerControl.ImageWidthProperty);
				}
			}
		}

		private object UIGetImageWidth()
		{
			return (double)FImageWidth;
		}
		
		protected int FImageHeight = 0;
		[DefaultValue(0)]
		public int ImageHeight
		{
			get { return FImageHeight; }
			set
			{
				if (FImageHeight != value)
				{
					FImageHeight = value;
					UpdateBinding(TriggerControl.ImageHeightProperty);
				}
			}
		}

		private object UIGetImageHeight()
		{
			return (double)FImageHeight;
		}
		
		// Text

		private string FText = String.Empty;
		[DefaultValue("")]
		public string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					UpdateBinding(ContentControl.ContentProperty);
				}
			}
		}

		private void ActionTextChanged(object ASender, EventArgs AArgs)
		{
			if (FText == String.Empty)
				UpdateBinding(ContentControl.ContentProperty);
		}

		public virtual string GetText()
		{
			return 
				(Action != null) && (FText == String.Empty)
					? Action.Text
					: FText;
		}

		private object UIGetContent()
		{
			return GetText();
		}
		
		// Enabled

		private bool FEnabled = true;
		[DefaultValue(true)]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (FEnabled != value)
				{
					FEnabled = value;
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
			return ( Action == null ? false : Action.GetEnabled() ) && FEnabled && base.GetEnabled();
		}

		private void ActionEnabledChanged(object ASender, EventArgs AArgs)
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

		private void ActionHintChanged(object ASender, EventArgs AArgs)
		{
			UpdateBinding(ToolTipService.ToolTipProperty);
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((Action == null) || Action.Visible);
		}

		private void ActionVisibleChanged(object ASender, EventArgs AArgs)
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
