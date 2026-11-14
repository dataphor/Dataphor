using System;
using System.Windows.Controls;
using System.Windows;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> Panel that lays out light a horizontal stack panel, but proportionally sizes the children to take up all available space. </summary>
	public class FillPanel : Panel
	{
		protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
		{
			var result = new Size(0d, 0d);
			foreach (UIElement child in Children)
			{
				child.Measure
				(
					new Size
					(
						availableSize.Width - result.Width,
						availableSize.Height
					)
				);
				result.Width += child.DesiredSize.Width;
				result.Height = Math.Max(child.DesiredSize.Height, result.Height);
			}

			return result;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var desiredWidth = 0d;
			foreach (UIElement child in Children)
				desiredWidth += child.DesiredSize.Width;
			var scalingFactor = desiredWidth == 0d ? 0d : (finalSize.Width / desiredWidth);
				
			var offsetX = 0d;
			foreach (UIElement child in Children)
			{
				var width = child.DesiredSize.Width * scalingFactor;
				child.Arrange
				(
					new Rect
					(
						offsetX,
						0,
						(SnapsToDevicePixels ? Math.Floor(width) : width),
						finalSize.Height
					)
				);
				offsetX += width;
			}
			return new Size(offsetX, finalSize.Height);
		}
	}
}
