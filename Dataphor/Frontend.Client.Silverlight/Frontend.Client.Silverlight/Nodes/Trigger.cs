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
					UpdateVerticalAlignment();
				}
			}
		}

		protected override void UpdateVerticalAlignment()
		{
			BindVerticalAlignment = ConvertVerticalAlignment(FVerticalAlignment);
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

		protected override void InitializeFrameworkElement()
		{
			base.InitializeFrameworkElement();

			Button.Click += new RoutedEventHandler(ButtonClick);
			
			var LBinding = new Binding("BindText");
			LBinding.Source = this;
			FrameworkElement.SetBinding(ContentControl.ContentProperty, LBinding);

			LBinding = new Binding("BindIsEnabled");
			LBinding.Source = this;
			FrameworkElement.SetBinding(Control.IsEnabledProperty, LBinding);
			
			LBinding = new Binding("BindImage");
			LBinding.Source = this;
			FrameworkElement.SetBinding(TriggerControl.ImageProperty, LBinding);
			
			LBinding = new Binding("BindImageWidth");
			LBinding.Source = this;
			FrameworkElement.SetBinding(TriggerControl.ImageWidthProperty, LBinding);			

			LBinding = new Binding("BindImageHeight");
			LBinding.Source = this;
			FrameworkElement.SetBinding(TriggerControl.ImageHeightProperty, LBinding);			
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
							if (Action != null)
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
						UpdateEnabled();
						UpdateImage();
						UpdateText();
						UpdateToolTip();
						UpdateVisible();
					}
				}
			}
		}
		
		protected void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}
		
		// Image

		private ImageSource FBindImage;
		
		public ImageSource BindImage
		{
			get { return FBindImage; }
			private set
			{
				if (FBindImage != value)
				{
					FBindImage = value;
					NotifyPropertyChanged("BindImage");
				}
			}
		}
		
		private void ActionImageChanged(object ASender, EventArgs AArgs)
		{
			if (Active)
				UpdateImage();
		}
		
		protected void UpdateImage()
		{
			BindImage = (Action == null ? null : ((Action)Action).LoadedImage);
		}

		private double FBindImageWidth;
		
		public double BindImageWidth
		{
			get { return FBindImageWidth; }
			private set
			{
				if (FBindImageWidth != value)
				{
					FBindImageWidth = value;
					NotifyPropertyChanged("BindImageWidth");
				}
			}
		}
		
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
					BindImageWidth = FImageWidth;
				}
			}
		}

		private double FBindImageHeight;
		
		public double BindImageHeight
		{
			get { return FBindImageHeight; }
			private set
			{
				if (FBindImageHeight != value)
				{
					FBindImageHeight = value;
					NotifyPropertyChanged("BindImageHeight");
				}
			}
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
					BindImageHeight = FImageHeight;
				}
			}
		}
		
		// Text

		private string FBindText;
		
		public string BindText
		{
			get { return FBindText; }
			private set
			{
				if (FBindText != value)
				{
					FBindText = value;
					NotifyPropertyChanged("BindText");
				}
			}
		}
		
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
					UpdateText();
				}
			}
		}

		private void ActionTextChanged(object ASender, EventArgs AArgs)
		{
			if (FText == String.Empty)
				UpdateText();
		}

		public virtual string GetText()
		{
			return 
				(Action != null) && (FText == String.Empty)
					? Action.Text
					: FText;
		}

		protected void UpdateText()
		{
			BindText = GetText();
		}
		
		// Enabled

		private bool FBindIsEnabled;
		
		public bool BindIsEnabled
		{
			get { return FBindIsEnabled; }
			private set
			{
				if (FBindIsEnabled != value)
				{
					FBindIsEnabled = value;
					NotifyPropertyChanged("BindIsEnabled");
				}
			}
		}
		
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
					UpdateEnabled();
				}
			}
		}

		/// <summary> Gets whether the node is actuall enabled (accounting for action). </summary>
		/// <remarks>
		///		The enabled state of the node is the most restrictive between 
		///		the action and the Enabled property.
		///	</remarks>
		public virtual bool GetEnabled()
		{
			return ( Action == null ? false : Action.GetEnabled() ) && FEnabled;
		}

		private void ActionEnabledChanged(object ASender, EventArgs AArgs)
		{
			UpdateEnabled();
		}

		protected void UpdateEnabled()
		{
			BindIsEnabled = GetEnabled();
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
			UpdateToolTip();
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((Action == null) || Action.Visible);
		}

		private void ActionVisibleChanged(object ASender, EventArgs AArgs)
		{
			UpdateVisible();
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

		// Node

		protected override void Activate()
		{
			UpdateText();
			UpdateImage();
			UpdateEnabled();
			base.Activate();
		}
	}
}
