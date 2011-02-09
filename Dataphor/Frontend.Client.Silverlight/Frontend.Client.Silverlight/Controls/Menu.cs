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
			foreach (MenuItem LItem in Items)
				ApplyItemContainerStyle(LItem);
		}
 
		private void ApplyItemContainerStyle(MenuItem AItem)
		{
			if
			(
				AItem != null
					&&
					(
						AItem.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue
							|| AItem.IsStyleSetFromItemsContainer
					)
			)
			{
				if (ItemContainerStyle != null)
				{
					AItem.Style = ItemContainerStyle;
					AItem.IsStyleSetFromItemsContainer = true;
				}
				else
				{
					AItem.ClearValue(FrameworkElement.StyleProperty);
					AItem.IsStyleSetFromItemsContainer = false;
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

		protected override void PrepareContainerForItemOverride(DependencyObject AElement, object AItem)
		{
 			base.PrepareContainerForItemOverride(AElement, AItem);
 			var LMenuItem = AElement as MenuItem;
 			if (LMenuItem != null)
 			{
 				LMenuItem.Highlighted += new RoutedEventHandler(SubMenuItemHighlighted);
				LMenuItem.ItemClicked += SubMenuItemItemClicked;
				LMenuItem.Clicked += MenuItemClicked;
				ApplyItemContainerStyle(LMenuItem);
			}
		}
		
		protected override void ClearContainerForItemOverride(DependencyObject AElement, object AItem)
		{
 			base.ClearContainerForItemOverride(AElement, AItem);
 			var LMenuItem = AElement as MenuItem;
 			if (LMenuItem != null)
 			{
 				LMenuItem.Highlighted -= new RoutedEventHandler(SubMenuItemHighlighted);
				LMenuItem.ItemClicked -= SubMenuItemItemClicked;
				LMenuItem.Clicked -= MenuItemClicked;
 			}
		}

		private PopupProtector FProtectorControl;

		protected PopupProtector ProtectorControl
		{
			get { return FProtectorControl; }
			set
			{
				if (FProtectorControl != value)
				{
					if (FProtectorControl != null)
						FProtectorControl.Clicked -= new RoutedEventHandler(ProtectorControlClicked);
					FProtectorControl = value;
					if (FProtectorControl != null)
						FProtectorControl.Clicked += new RoutedEventHandler(ProtectorControlClicked);
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
		
		private void SubMenuItemItemClicked(MenuItem ASender)
		{
			if (ASender.Items.Count == 0)
				IsActive = false;
			OnItemClicked(ASender);
		}

		protected virtual void OnItemClicked(MenuItem ASender)
		{
			if (ItemClicked != null)
				ItemClicked(ASender);
		}
		
		private void MenuItemClicked(MenuItem ASender)
		{
			IsActive = !IsActive && ASender.Items.Count > 0;
			UpdateItemsExpansion(IsActive ? ASender : null);
			OnItemClicked(ASender);
		}

		private void SubMenuItemHighlighted(object ASender, RoutedEventArgs AArgs)
		{
			UpdateItemsExpansion(IsActive ? (MenuItem)ASender : null);
		}

		private void UpdateItemsExpansion(MenuItem AActive)
		{
			foreach (MenuItem LItem in Items)
				LItem.IsExpanded = LItem.Items.Count > 0 && Object.ReferenceEquals(LItem, AActive);
		}
	}
}
