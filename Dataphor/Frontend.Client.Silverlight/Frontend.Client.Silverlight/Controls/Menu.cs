using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplatePart(Name = "Protector", Type = typeof(PopupProtector))]
	public class Menu : ItemsControl
	{
		public Menu()
		{
			this.DefaultStyleKey = typeof(Menu);
		}

		/// <summary> Gets or sets whether the menu is active, which means that it 
		/// has focus, or that a submenu has been expanded. </summary>
		public bool IsActive
		{
			get { return (bool)GetValue(IsActiveProperty); }
			set { SetValue(IsActiveProperty, value); }
		}
		
		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register("IsActive", typeof(bool), typeof(Menu), new PropertyMetadata(false, new PropertyChangedCallback(OnIsActiveChanged)));

		private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Menu)d).OnIsActiveChanged(e);
		}

		protected virtual void OnIsActiveChanged(DependencyPropertyChangedEventArgs e)
		{
			if (!(bool)e.NewValue)
				UpdateItemsExpansion(null);
		}

        public static DependencyProperty ItemContainerStyleProperty = 
			DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(Menu), new PropertyMetadata(null, new PropertyChangedCallback(OnItemContainerStyleChanged)));

        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

		private static void OnItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Menu)d).OnItemContainerStyleChanged((Style)e.OldValue, (Style)e.NewValue);
		}
 
		private void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)
		{
			foreach (MenuItem item in Items)
				ApplyItemContainerStyle(item);
		}
 
		private void ApplyItemContainerStyle(MenuItem item)
		{
			if
			(
				item != null
					&&
					(
						item.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue
							|| item.IsStyleSetFromItemsContainer
					)
			)
			{
				if (ItemContainerStyle != null)
				{
					item.Style = ItemContainerStyle;
					item.IsStyleSetFromItemsContainer = true;
				}
				else
				{
					item.ClearValue(FrameworkElement.StyleProperty);
					item.IsStyleSetFromItemsContainer = false;
				}
			}
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new MenuItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is MenuItem;
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
 			base.PrepareContainerForItemOverride(element, item);
 			var menuItem = element as MenuItem;
 			if (menuItem != null)
 			{
 				menuItem.Highlighted += new RoutedEventHandler(SubMenuItemHighlighted);
				menuItem.ItemClicked += SubMenuItemItemClicked;
				menuItem.Clicked += MenuItemClicked;
				ApplyItemContainerStyle(menuItem);
			}
		}
		
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
 			base.ClearContainerForItemOverride(element, item);
 			var menuItem = element as MenuItem;
 			if (menuItem != null)
 			{
 				menuItem.Highlighted -= new RoutedEventHandler(SubMenuItemHighlighted);
				menuItem.ItemClicked -= SubMenuItemItemClicked;
				menuItem.Clicked -= MenuItemClicked;
 			}
		}

		private PopupProtector _protectorControl;

		protected PopupProtector ProtectorControl
		{
			get { return _protectorControl; }
			set
			{
				if (_protectorControl != value)
				{
					if (_protectorControl != null)
						_protectorControl.Clicked -= new RoutedEventHandler(ProtectorControlClicked);
					_protectorControl = value;
					if (_protectorControl != null)
						_protectorControl.Clicked += new RoutedEventHandler(ProtectorControlClicked);
				}
			}
		}

		private void ProtectorControlClicked(object sender, RoutedEventArgs e)
		{
			IsActive = false;
		}

		public override void OnApplyTemplate()
		{
			ProtectorControl = GetTemplateChild("Protector") as PopupProtector;
			base.OnApplyTemplate();
		}

		public event MenuItemClickedHandler ItemClicked;
		
		private void SubMenuItemItemClicked(MenuItem sender)
		{
			if (sender.Items.Count == 0)
				IsActive = false;
			OnItemClicked(sender);
		}

		protected virtual void OnItemClicked(MenuItem sender)
		{
			if (ItemClicked != null)
				ItemClicked(sender);
		}
		
		private void MenuItemClicked(MenuItem sender)
		{
			IsActive = !IsActive && sender.Items.Count > 0;
			UpdateItemsExpansion(IsActive ? sender : null);
			OnItemClicked(sender);
		}

		private void SubMenuItemHighlighted(object sender, RoutedEventArgs args)
		{
			UpdateItemsExpansion(IsActive ? (MenuItem)sender : null);
		}

		private void UpdateItemsExpansion(MenuItem active)
		{
			foreach (MenuItem item in Items)
				item.IsExpanded = item.Items.Count > 0 && Object.ReferenceEquals(item, active);
		}
	}
}
