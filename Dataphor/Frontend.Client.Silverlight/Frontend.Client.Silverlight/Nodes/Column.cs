using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Column : ContainerElement
	{
		// IVerticalAlignedElement

		private VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					UpdateBinding(FrameworkElement.VerticalAlignmentProperty);
				}
			}
		}

		protected override object UIGetVerticalAlignment()
		{
			return ConvertVerticalAlignment(FVerticalAlignment);
		}
	}
}
