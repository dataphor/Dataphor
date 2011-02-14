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
		
		protected string _title = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return _title; }
			set
			{
				_title = value;
				UpdateBinding(NotebookItem.HeaderProperty);
			}
		}
		
		protected virtual string GetTitle()
		{
			return String.IsNullOrEmpty(_title) ? Strings.CDefaultNotebookPageTitle : _title;
		}
		
		private object UIGetHeader()
		{
			return GetTitle();
		}

		public override bool IsValidOwner(Type ownerType)
		{
			return typeof(INotebook).IsAssignableFrom(ownerType);
		}
	}
}
