using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class NotebookItem : ListBoxItem
	{
		public NotebookItem()
		{
			this.DefaultStyleKey = typeof(NotebookItem);
		}

		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register("Header", typeof(object), typeof(NotebookItem), new PropertyMetadata(null));

		/// <summary> The header content used to display on the notebook item's tab. </summary>
		public object Header
		{
			get { return (object)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}

		public static readonly DependencyProperty HeaderPaddingProperty =
			DependencyProperty.Register("HeaderPadding", typeof(Thickness), typeof(NotebookItem), new PropertyMetadata(new Thickness(6,2,6,2)));

		/// <summary> The padding around the header. </summary>
		public Thickness HeaderPadding
		{
			get { return (Thickness)GetValue(HeaderPaddingProperty); }
			set { SetValue(HeaderPaddingProperty, value); }
		}

		public static readonly DependencyProperty HeaderTemplateProperty =
			DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(NotebookItem), new PropertyMetadata(null));

		/// <summary> The data template for the header. </summary>
		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
			set { SetValue(HeaderTemplateProperty, value); }
		}
	}
}
