using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Abstract class the implements functionality common panel based container elements. </summary>
	public abstract class ContainerElement : Element, ISilverlightContainerElement
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new StackPanel();
		}

		protected Panel Panel
		{
			get { return (Panel)FrameworkElement; }
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		public void InsertChild(int index, FrameworkElement child)
		{
			Panel.Children.Insert(Math.Min(index, Panel.Children.Count), child);
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		public void RemoveChild(FrameworkElement child)
		{
			Panel.Children.Remove(child);
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

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IElement).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}
	}
}
