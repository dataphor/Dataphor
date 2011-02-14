using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Notebook : Element, INotebook, ISilverlightContainerElement
	{
		protected override void Dispose(bool disposing)
		{
			try
			{
				OnActivePageChange = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public NotebookControl NotebookControl
		{
			get { return (NotebookControl)FrameworkElement; }
		}

		/// <remarks> This callback method is invoked on the main thread. </remarks>
		private void SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			var newItem = args.AddedItems.Count == 0 ? null : args.AddedItems[0];
			Session.Invoke
			(
				(System.Action)
				(
					() =>
					{
						if (Active)
						{
							SetActive(FindPage(newItem));
							if (OnActivePageChange != null)
								OnActivePageChange.Execute(this, new EventParams());
						}
					}
				)
			);
		}

		private IBaseNotebookPage FindPage(object item)
		{
			foreach (ISilverlightBaseNotebookPage page in Children)
				if (page.ContentControl == item)
					return page;
			return null;
		}

		private void SetActive(IBaseNotebookPage page)
		{
			if (page != _activePage)
			{
				IBaseNotebookPage oldPage = _activePage;
				_activePage = page;

				if (oldPage != null)
					((ISilverlightBaseNotebookPage)oldPage).Unselected();
				if (_activePage != null)
					((ISilverlightBaseNotebookPage)_activePage).Selected();
			}
		}

		// ActivePage

		private IBaseNotebookPage _activePage;
		public IBaseNotebookPage ActivePage
		{
			get { return _activePage; }
			set
			{
				if (_activePage != value)
				{
					if ((value != null) && (!IsChildNode(value)))
						throw new ClientException(ClientException.Codes.InvalidActivePage);
					_activePage = value;
					UpdateBinding(NotebookControl.TargetSelectedIndexProperty);
				}
			}
		}

		private object UIGetTargetSelectedIndex()
		{
			return _activePage == null ? -1 : Children.IndexOf(_activePage);;
		}

		private bool IsChildNode(INode node)
		{
			foreach (INode localNode in Children)
				if (localNode == node)
					return true;
			return false;
		}

		// OnActivePageChange

		private IAction _onActivePageChange;
		public IAction OnActivePageChange
		{
			get { return _onActivePageChange; }
			set
			{
				if (_onActivePageChange != null)
					_onActivePageChange.Disposed -= new EventHandler(OnActivePageChangeDisposed);
				_onActivePageChange = value;
				if (_onActivePageChange != null)
					_onActivePageChange.Disposed += new EventHandler(OnActivePageChangeDisposed);
			}
		}

		private void OnActivePageChangeDisposed(object sender, EventArgs args)
		{
			OnActivePageChange = null;
		}

		// Node

		protected override void Activate()
		{
			// Use the first child if there is not an explicit active page set (do this before calling base so the child will know that it will be active)
			if ((_activePage == null) && (Children.Count > 0))
				_activePage = (IBaseNotebookPage)Children[0];

			base.Activate();

			if (_activePage != null)
				((ISilverlightBaseNotebookPage)_activePage).Selected();
		}

		public override bool IsValidChild(Type childType)
		{
			if (typeof(ISilverlightBaseNotebookPage).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		internal protected override void ChildrenChanged()
		{
			base.ChildrenChanged();
			if ((_activePage == null) && (Children.Count > 0))
				ActivePage = (IBaseNotebookPage)Children[0];
		}

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void InsertChild(int index, FrameworkElement child)
		{
			NotebookControl.Items.Insert(Math.Min(index, NotebookControl.Items.Count), child);
		}

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void RemoveChild(FrameworkElement child)
		{
			NotebookControl.Items.Remove(child);
		}

		// Element

		protected override FrameworkElement CreateFrameworkElement()
		{
			return new NotebookControl();
		}

		protected override void InitializeFrameworkElement()
		{
			base.InitializeFrameworkElement();
			NotebookControl.SelectionChanged += new SelectionChangedEventHandler(SelectionChanged);
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(NotebookControl.TargetSelectedIndexProperty, new Func<object>(UIGetTargetSelectedIndex));
		}
		
		public override bool GetDefaultTabStop()
		{
			return true;
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
