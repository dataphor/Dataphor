using System;
using System.Windows.Controls;
using System.Windows;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> Panel that lays out light a horizontal stack panel, but proportionally sizes the children to take up all available space. </summary>
	public class FillPanel : Panel
	{
		protected override System.Windows.Size MeasureOverride(System.Windows.Size AAvailableSize)
		{
			var LResult = new Size(0d, 0d);
			foreach (UIElement LChild in Children)
			{
				LChild.Measure
				(
					new Size
					(
						AAvailableSize.Width - LResult.Width,
						AAvailableSize.Height
					)
				);
				LResult.Width += LChild.DesiredSize.Width;
				LResult.Height = Math.Max(LChild.DesiredSize.Height, LResult.Height);
			}

			return LResult;
		}

		protected override Size ArrangeOverride(Size AFinalSize)
		{
			var LDesiredWidth = 0d;
			foreach (UIElement LChild in Children)
				LDesiredWidth += LChild.DesiredSize.Width;
			var LScalingFactor = LDesiredWidth == 0d ? 0d : (AFinalSize.Width / LDesiredWidth);
				
			var LOffsetX = 0d;
			foreach (UIElement LChild in Children)
			{
				var LWidth = LChild.DesiredSize.Width * LScalingFactor;
				LChild.Arrange
				(
					new Rect
					(
						LOffsetX,
						0,
						(SnapsToDevicePixels ? Math.Floor(LWidth) : LWidth),
						AFinalSize.Height
					)
				);
				LOffsetX += LWidth;
			}
			return new Size(LOffsetX, AFinalSize.Height);
		}
	}
}
