using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplatePart(Name = "Popup", Type = typeof(Popup))]
	[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Highlighted", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Pressed", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Expanded", GroupName = "ExpandedStates")]
	[TemplateVisualState(Name = "Collapsed", GroupName = "ExpandedStates")]
	[TemplateVisualState(Name = "HasItems", GroupName = "HastItemsStates")]
	[TemplateVisualState(Name = "NoItems", GroupName = "HastItemsStates")]
	public class MenuItem : HeaderedItemsControl
	{
		public MenuItem()
		{
			this.DefaultStyleKey = typeof(MenuItem);
			
			this.IsEnabledChanged += new DependencyPropertyChangedEventHandler(OnEnabledChanged);
		}
		
		protected virtual void OnEnabledChanged(object ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			UpdateVisualStates(true);
		}

		public override void OnApplyTemplate()
		{
			SubMenuPopup = GetTemplateChild("Popup") as Popup;
			base.OnApplyTemplate();
			UpdateVisualStates(false);
		}
		
		protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			UpdateVisualStates(true);
		}

		protected override void PrepareContainerForItemOverride(DependencyObject AElement, object AItem)
		{
 			base.PrepareContainerForItemOverride(AElement, AItem);
 			var LMenuItem = AElement as MenuItem;
 			if (LMenuItem != null)
 			{
 				LMenuItem.Highlighted += SubMenuItemHighlighted;
				LMenuItem.ItemClicked += SubMenuItemItemClicked;
				LMenuItem.Clicked += SubMenuItemClicked;
 			}
		}

		protected override void ClearContainerForItemOverride(DependencyObject AElement, object AItem)
		{
 			base.ClearContainerForItemOverride(AElement, AItem);
 			var LMenuItem = AElement as MenuItem;
 			if (LMenuItem != null)
 			{
 				LMenuItem.Highlighted -= SubMenuItemHighlighted;
				LMenuItem.ItemClicked -= SubMenuItemItemClicked;
				LMenuItem.Clicked -= SubMenuItemClicked;
			}
		}
		
		public event MenuItemClickedHandler ItemClicked;
		
		private void SubMenuItemItemClicked(MenuItem ASender)
		{
			OnItemClicked(ASender);
		}

		private void SubMenuItemClicked(MenuItem ASender)
		{
			OnItemClicked(ASender);
		}

		protected virtual void OnItemClicked(MenuItem ASender)
		{
			if (ItemClicked != null)
				ItemClicked(ASender);
		}
		
		private void UpdateItemsExpansion(MenuItem AActive)
		{
			foreach (MenuItem LItem in Items)
				LItem.IsExpanded = LItem.Items.Count > 0 && Object.ReferenceEquals(LItem, AActive);
		}
		
		private void SubMenuItemHighlighted(object ASender, RoutedEventArgs AArgs)
		{
			UpdateItemsExpansion((MenuItem)ASender);
		}
		
		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register("IsExpanded", typeof(bool), typeof(MenuItem), new PropertyMetadata(false, new PropertyChangedCallback(OnIsExpandedChanged)));

		/// <summary> True if the menu item is expanded. </summary>
		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}

		private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MenuItem)d).OnIsExpandedChanged(e);
		}

		protected virtual void OnIsExpandedChanged(DependencyPropertyChangedEventArgs e)
		{
			// Ensure that no sub-menus are expanded if this menu is no longer expanded
			if (!IsExpanded)
			{
				foreach (MenuItem LItem in Items)
					LItem.IsExpanded = false;
			}
			UpdateVisualStates(true);
		}

		private Popup FSubMenuPopup;
		
		public Popup SubMenuPopup
		{
			get { return FSubMenuPopup; }
			set { FSubMenuPopup = value; }
		}

		public static readonly DependencyProperty IsHighlightedProperty =
			DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(MenuItem), new PropertyMetadata(false, new PropertyChangedCallback(OnIsHighlightedChanged)));

		/// <summary> True if the mouse is over the header item. </summary>
		public bool IsHighlighted
		{
			get { return (bool)GetValue(IsHighlightedProperty); }
			set { SetValue(IsHighlightedProperty, value); }
		}

		private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MenuItem)d).OnIsHighlightedChanged(e);
		}

		public event RoutedEventHandler Highlighted;
		
		protected virtual void OnIsHighlightedChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateVisualStates(true);
			
			if (Highlighted != null)
				Highlighted(this, new RoutedEventArgs());
		}

		private void UpdateHighlighted()
		{
			IsHighlighted = FIsFocused || FIsMouseOver;
		}
		
		private bool FIsFocused;
		private bool FIsMouseOver;

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			FIsFocused = true;
			UpdateHighlighted();
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			FIsFocused = false;
			UpdateHighlighted();
		}

		protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			FIsMouseOver = true;
			UpdateHighlighted();
		}

		protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			FIsMouseOver = false;
			UpdateHighlighted();
		}

		protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			IsPressed = true;
		}

		protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonUp(e);
			IsPressed = false;
			OnClicked();
		}

		public event MenuItemClickedHandler Clicked;

		protected virtual void OnClicked()
		{
			if (Clicked != null)
				Clicked(this);
		}
		
		public static readonly DependencyProperty IsPressedProperty =
			DependencyProperty.Register("IsPressed", typeof(bool), typeof(MenuItem), new PropertyMetadata(false, new PropertyChangedCallback(OnIsPressedChanged)));

		/// <summary> True if the header item is pressed. </summary>
		public bool IsPressed
		{
			get { return (bool)GetValue(IsPressedProperty); }
			set { SetValue(IsPressedProperty, value); }
		}

		private static void OnIsPressedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MenuItem)d).OnIsPressedChanged(e);
		}

		protected virtual void OnIsPressedChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateVisualStates(true);
		}

		public static readonly DependencyProperty SubMenuPlacementProperty = 
			DependencyProperty.Register("SubMenuPlacement", typeof(PlacementMode), typeof(MenuItem), new PropertyMetadata(PlacementMode.Right, new PropertyChangedCallback(OnSubMenuPlacementChanged)));
		    
		/// <summary> Gets or sets the placement of the sub-menu items. </summary>
		public PlacementMode SubMenuPlacement
		{
			get { return (PlacementMode)GetValue(SubMenuPlacementProperty); }
			set { SetValue(SubMenuPlacementProperty, value); }
		}

		private static void OnSubMenuPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MenuItem)d).OnSubMenuPlacementChanged(e);
		}

		protected virtual void OnSubMenuPlacementChanged(DependencyPropertyChangedEventArgs e)
		{
		}
		
		private void UpdateVisualStates(bool AUseTransitions)
		{
			if (!IsEnabled)
				VisualStateManager.GoToState(this, "Disabled", AUseTransitions);
			else if (IsPressed)
				VisualStateManager.GoToState(this, "Pressed", AUseTransitions);
			else if (IsHighlighted)				
				VisualStateManager.GoToState(this, "Highlighted", AUseTransitions);
			else
				VisualStateManager.GoToState(this, "Normal", AUseTransitions);
				
			if (IsExpanded)
				VisualStateManager.GoToState(this, "Expanded", AUseTransitions);
			else
				VisualStateManager.GoToState(this, "Collapsed", AUseTransitions);
				
			if (Items.Count > 0)
				VisualStateManager.GoToState(this, "HasItems", AUseTransitions);
			else
				VisualStateManager.GoToState(this, "NoItems", AUseTransitions);
		}

		internal bool IsStyleSetFromItemsContainer { get; set; }

		public static readonly DependencyProperty ImageSourceProperty =
			DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(MenuItem), new PropertyMetadata(null));

		public ImageSource ImageSource
		{
			get { return (ImageSource)GetValue(ImageSourceProperty); }
			set { SetValue(ImageSourceProperty, value); }
		}
	}
	
	public delegate void MenuItemClickedHandler(MenuItem AItem);
}
