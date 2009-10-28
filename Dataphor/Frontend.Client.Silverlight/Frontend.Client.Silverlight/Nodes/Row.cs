using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Row : ContainerElement, IRow
	{
		// IHorizontalAlignedElement

		private HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
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
