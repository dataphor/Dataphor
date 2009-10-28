using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class NotebookPage : Element, INotebookPage, ISilverlightBaseNotebookPage
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new TabItem();
		}
		
		public TabItem TabItem
		{
			get { return (TabItem)FrameworkElement; }
		}

		public virtual void Selected() {}

		public virtual void Unselected() {}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(HeaderedItemsControl.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		protected string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return FTitle; }
			set
			{
				FTitle = value;
				UpdateBinding(HeaderedItemsControl.HeaderProperty);
			}
		}
		
		private object UIGetHeader()
		{
			return String.IsNullOrEmpty(FTitle) ? null : FTitle;
		}

		public override bool IsValidOwner(Type AOwnerType)
		{
			return typeof(INotebook).IsAssignableFrom(AOwnerType);
		}
	}
}
