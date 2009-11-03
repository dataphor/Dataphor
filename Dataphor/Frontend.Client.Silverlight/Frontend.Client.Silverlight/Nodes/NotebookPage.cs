using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class NotebookPage : ContentElement, INotebookPage, ISilverlightBaseNotebookPage
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new NotebookItem();
		}
		
		public NotebookItem NotebookItem
		{
			get { return (NotebookItem)FrameworkElement; }
		}

		public virtual void Selected() {}

		public virtual void Unselected() {}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(NotebookItem.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		protected string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return FTitle; }
			set
			{
				FTitle = value;
				UpdateBinding(NotebookItem.HeaderProperty);
			}
		}
		
		protected virtual string GetTitle()
		{
			return String.IsNullOrEmpty(FTitle) ? Strings.CDefaultNotebookPageTitle : FTitle;
		}
		
		private object UIGetHeader()
		{
			return GetTitle();
		}

		public override bool IsValidOwner(Type AOwnerType)
		{
			return typeof(INotebook).IsAssignableFrom(AOwnerType);
		}
	}
}
