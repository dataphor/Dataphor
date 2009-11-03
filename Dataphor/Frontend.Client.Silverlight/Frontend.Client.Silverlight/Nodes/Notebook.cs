using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Notebook : Element, INotebook, ISilverlightContainerElement
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

		public NotebookControl NotebookControl
		{
			get { return (NotebookControl)FrameworkElement; }
		}

		/// <remarks> This callback method is invoked on the main thread. </remarks>
		private void SelectionChanged(object ASender, SelectionChangedEventArgs AArgs)
		{
			var LNewItem = AArgs.AddedItems.Count == 0 ? null : AArgs.AddedItems[0];
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

		private IBaseNotebookPage FindPage(object AItem)
		{
			foreach (ISilverlightBaseNotebookPage LPage in Children)
				if (LPage.ContentControl == AItem)
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
					UpdateBinding(NotebookControl.TargetSelectedIndexProperty);
				}
			}
		}

		private object UIGetTargetSelectedIndex()
		{
			return FActivePage == null ? -1 : Children.IndexOf(FActivePage);;
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

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void InsertChild(int AIndex, FrameworkElement AChild)
		{
			NotebookControl.Items.Insert(Math.Min(AIndex, NotebookControl.Items.Count), AChild);
		}

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void RemoveChild(FrameworkElement AChild)
		{
			NotebookControl.Items.Remove(AChild);
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
