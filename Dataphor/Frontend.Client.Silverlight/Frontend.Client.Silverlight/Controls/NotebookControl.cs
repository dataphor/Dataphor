using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Tabbed notebook control. </summary>
	public class NotebookControl : ListBox
	{
		public NotebookControl()
		{
			this.DefaultStyleKey = typeof(NotebookControl);

			this.SelectionChanged += new SelectionChangedEventHandler(OnSelectionChanged);
		}
		
		protected override DependencyObject GetContainerForItemOverride()
		{
			var LItem = new NotebookItem();
			if (ItemContainerStyle != null)
				LItem.Style = ItemContainerStyle;
			return LItem;
		}

		protected override bool IsItemItsOwnContainerOverride(object AItem)
		{
			return AItem is NotebookItem;
		}

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			UpdateSelectedFromTarget();
		}
		
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateSelection();
		}

		/// <summary> Handles selection change and sets the current content property to the selected item container's content. </summary>
		private void OnSelectionChanged(object ASender, SelectionChangedEventArgs AArgs)
		{
			TargetSelectedIndex = SelectedIndex;
			UpdateSelection();
		}
		
		private void UpdateSelection()
		{
			if (Items.Count > 0 && SelectedIndex >= 0 && SelectedIndex < Items.Count)
			{
				var LContainer = Items[SelectedIndex] as NotebookItem;
				var LNewContent = LContainer != null ? LContainer.Content : null;
				ClearValue(SelectedContentProperty);
				if (LContainer != null)
				{
					var LBinding = new Binding("Content");
					LBinding.Source = LContainer;
					SetBinding(SelectedContentProperty, LBinding);
				}
			}
			else
				ClearValue(SelectedContentProperty);
		}

		public static readonly DependencyProperty SelectedContentProperty =
			DependencyProperty.Register("SelectedContent", typeof(object), typeof(NotebookControl), new PropertyMetadata(null));

		/// <summary> The content of the currently selected notebook item. </summary>
		public object SelectedContent
		{
			get { return (object)GetValue(SelectedContentProperty); }
			internal set { SetValue(SelectedContentProperty, value); }
		}

		public static readonly DependencyProperty TargetSelectedIndexProperty =
			DependencyProperty.Register("TargetSelectedIndex", typeof(int), typeof(NotebookControl), new PropertyMetadata(0, new PropertyChangedCallback(OnTargetSelectedIndexChanged)));

		/// <summary> The desired selected index.  </summary>
		/// <remarks> Setting this will not result in an error if the specified target is outside the valid range. </remarks>
		public int TargetSelectedIndex
		{
			get { return (int)GetValue(TargetSelectedIndexProperty); }
			set { SetValue(TargetSelectedIndexProperty, value); }
		}

		private static void OnTargetSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((NotebookControl)d).OnTargetSelectedIndexChanged(e);
		}

		protected virtual void OnTargetSelectedIndexChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateSelectedFromTarget();
		}

		private void UpdateSelectedFromTarget()
		{
			if (TargetSelectedIndex != SelectedIndex && TargetSelectedIndex >= -1 && TargetSelectedIndex < Items.Count)
				SelectedIndex = TargetSelectedIndex;
		}
	}
}
