using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Notebook : Element, INotebook
	{
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				OnActivePageChange = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		protected override FrameworkElement CreateFrameworkElement()
		{
			return new System.Windows.Controls.TabControl();
		}

		protected override void InitializeFrameworkElement()
		{
			base.InitializeFrameworkElement();
			TabControl.SelectionChanged += new SelectionChangedEventHandler(SelectionChanged);
		}

		public TabControl TabControl
		{
			get { return (TabControl)FrameworkElement; }
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(TabControl.SelectedItemProperty, new Func<object>(UIGetSelectedItem));
		}
		
		/// <remarks> This callback method is invoked on the main thread. </remarks>
		private void SelectionChanged(object ASender, SelectionChangedEventArgs AArgs)
		{
			var LNewItem = AArgs.AddedItems.Count == 0 ? null : (TabItem)AArgs.AddedItems[0];
			Session.Invoke
			(
				(System.Action)
				(
					() =>
					{
						if (Active)
						{
							SetActive(FindPage(LNewItem));
							if (OnActivePageChange != null)
								OnActivePageChange.Execute(this, new EventParams());
						}
					}
				)
			);
		}

		private IBaseNotebookPage FindPage(TabItem AItem)
		{
			foreach (ISilverlightBaseNotebookPage LPage in Children)
				if (LPage.TabItem == AItem)
					return LPage;
			return null;
		}

		private void SetActive(IBaseNotebookPage APage)
		{
			if (APage != FActivePage)
			{
				IBaseNotebookPage LOldPage = FActivePage;
				FActivePage = APage;

				if (LOldPage != null)
					((ISilverlightBaseNotebookPage)LOldPage).Unselected();
				if (FActivePage != null)
					((ISilverlightBaseNotebookPage)FActivePage).Selected();
			}
		}

		// ActivePage

		private IBaseNotebookPage FActivePage;
		public IBaseNotebookPage ActivePage
		{
			get { return FActivePage; }
			set
			{
				if (FActivePage != value)
				{
					if ((value != null) && (!IsChildNode(value)))
						throw new ClientException(ClientException.Codes.InvalidActivePage);
					FActivePage = value;
					UpdateBinding(TabControl.SelectedItemProperty);
				}
			}
		}

		private object UIGetSelectedItem()
		{
			return FActivePage == null ? null : ((ISilverlightBaseNotebookPage)FActivePage).TabItem;
		}

		private bool IsChildNode(INode ANode)
		{
			foreach (INode LNode in Children)
				if (LNode == ANode)
					return true;
			return false;
		}

		// OnActivePageChange

		private IAction FOnActivePageChange;
		public IAction OnActivePageChange
		{
			get { return FOnActivePageChange; }
			set
			{
				if (FOnActivePageChange != null)
					FOnActivePageChange.Disposed -= new EventHandler(OnActivePageChangeDisposed);
				FOnActivePageChange = value;
				if (FOnActivePageChange != null)
					FOnActivePageChange.Disposed += new EventHandler(OnActivePageChangeDisposed);
			}
		}

		private void OnActivePageChangeDisposed(object ASender, EventArgs AArgs)
		{
			OnActivePageChange = null;
		}

		// Node

		protected override void Activate()
		{
			// Use the first child if there is not an explicit active page set (do this before calling base so the child will know that it will be active)
			if ((FActivePage == null) && (Children.Count > 0))
				FActivePage = (IBaseNotebookPage)Children[0];

			base.Activate();

			if (FActivePage != null)
				((ISilverlightBaseNotebookPage)FActivePage).Selected();
		}

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(ISilverlightBaseNotebookPage).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		internal protected override void ChildrenChanged()
		{
			base.ChildrenChanged();
			if ((FActivePage == null) && (Children.Count > 0))
				ActivePage = (IBaseNotebookPage)Children[0];
		}


		// Element

		public override bool GetDefaultTabStop()
		{
			return true;
		}
	}
}
