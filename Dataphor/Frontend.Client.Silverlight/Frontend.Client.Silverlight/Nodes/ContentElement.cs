using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class ContentElement : Element, ISilverlightContainerElement
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new ContentControl();
		}

		public ContentControl ContentControl
		{
			get { return (ContentControl)FrameworkElement; }
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		public void InsertChild(int index, FrameworkElement child)
		{
			ContentControl.Content = child;
		}

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void RemoveChild(FrameworkElement child)
		{
			ContentControl.Content = null;
		}

		// RootElement

		private ISilverlightElement _rootElement;
		
		protected ISilverlightElement RootElement
		{
			get { return _rootElement; }
		}

		public override bool IsValidChild(Type childType)
		{
			if (typeof(ISilverlightElement).IsAssignableFrom(childType))
				return _rootElement == null;
			return base.IsValidChild(childType);
		}

		protected internal override void InvalidChildError(INode child) 
		{
			throw new ClientException(ClientException.Codes.UseSingleElementNode);
		}

		protected internal override void AddChild(INode child)
		{
			base.AddChild(child);
			if (child is ISilverlightElement)
				_rootElement = (ISilverlightElement)child;
		}
		
		protected internal override void RemoveChild(INode child)
		{
			base.RemoveChild(child);
			if (child == _rootElement)
				_rootElement = null;
		}

		// Element

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

		public override bool GetDefaultTabStop()
		{
			return false;
		}
	}
}
