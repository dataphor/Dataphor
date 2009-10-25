using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Row : ContainerElement
	{
		// IHorizontalAlignedElement

		private HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set
			{
				if (FHorizontalAlignment != value)
				{
					FHorizontalAlignment = value;
					UpdateBinding(FrameworkElement.HorizontalAlignmentProperty);
				}
			}
		}

		protected override object UIGetHorizontalAlignment()
		{
			return ConvertHorizontalAlignment(FHorizontalAlignment);
		}

		protected override string GetDefaultStyle()
		{
			return "RowStyle";
		}
	}
}
