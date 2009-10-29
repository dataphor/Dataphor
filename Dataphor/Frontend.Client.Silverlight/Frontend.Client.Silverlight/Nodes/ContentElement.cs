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

		protected ContentControl ContentControl
		{
			get { return (ContentControl)FrameworkElement; }
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		public void InsertChild(int AIndex, FrameworkElement AChild)
		{
			ContentControl.Content = AChild;
		}

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void RemoveChild(FrameworkElement AChild)
		{
			ContentControl.Content = null;
		}

		// RootElement

		private ISilverlightElement FRootElement;
		
		protected ISilverlightElement RootElement
		{
			get { return FRootElement; }
		}

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(ISilverlightElement).IsAssignableFrom(AChildType))
				return FRootElement == null;
			return base.IsValidChild(AChildType);
		}

		protected internal override void InvalidChildError(INode AChild) 
		{
			throw new ClientException(ClientException.Codes.UseSingleElementNode);
		}

		protected internal override void AddChild(INode AChild)
		{
			base.AddChild(AChild);
			if (AChild is ISilverlightElement)
				FRootElement = (ISilverlightElement)AChild;
		}
		
		protected internal override void RemoveChild(INode AChild)
		{
			base.RemoveChild(AChild);
			if (AChild == FRootElement)
				FRootElement = null;
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
