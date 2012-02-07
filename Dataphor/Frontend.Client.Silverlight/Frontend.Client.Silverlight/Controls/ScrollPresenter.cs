using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class ScrollPresenter : ContentPresenter
	{
		public static readonly DependencyProperty CanHorizontallyScrollProperty = DependencyProperty.Register("CanHorizontallyScroll", typeof(bool), typeof(ScrollPresenter), new PropertyMetadata(true, new PropertyChangedCallback(CanScrollPropertyChanged)));
		
		/// <summary> Gets or sets whether the control can scroll horizontally. </summary>
		public bool CanHorizontallyScroll
		{
			get { return (bool)GetValue(CanHorizontallyScrollProperty); }
			set { SetValue(CanHorizontallyScrollProperty, value); }
		}

		public static readonly DependencyProperty CanVerticallyScrollProperty = DependencyProperty.Register("CanVerticallyScroll", typeof(bool), typeof(ScrollPresenter), new PropertyMetadata(true, new PropertyChangedCallback(CanScrollPropertyChanged)));
		
		/// <summary> Gets or sets whether the control can scroll vertically. </summary>
		public bool CanVerticallyScroll
		{
			get { return (bool)GetValue(CanVerticallyScrollProperty); }
			set { SetValue(CanVerticallyScrollProperty, value); }
		}

		private static void CanScrollPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScrollPresenter)sender).InvalidateMeasure();
		}
				
		private Size _extent = new Size(0d, 0d);
		
		public double ExtentHeight
		{
			get { return _extent.Height; }
		}
		
		public double ExtentWidth
		{
			get { return _extent.Width; }
		}
		
		private Size _viewport = new Size(0d, 0d);

		public double ViewportHeight
		{
			get { return _viewport.Height; }
		}
		
		public double ViewportWidth
		{
			get { return _viewport.Width; }
		}

		/// <summary> Raised when the extent or viewport changes. </summary>
		public event EventHandler MeasurementsChanged;
		
		protected override Size MeasureOverride(Size availableSize)
		{
			var inner = availableSize;
			if (CanHorizontallyScroll)
				inner.Width = Double.PositiveInfinity;
			if (CanVerticallyScroll)
				inner.Height = Double.PositiveInfinity;
			
			UIElement content = (VisualTreeHelper.GetChildrenCount(this) == 0) ? null : (VisualTreeHelper.GetChild(this, 0) as UIElement);
			if (content != null)
				content.Measure(inner);
			
			var extent = content == null ? new Size(0d, 0d) : content.DesiredSize;
			var viewport = availableSize;
			if (extent != _extent || viewport != _viewport)
			{
				_extent = extent;
				_viewport = viewport;
				HorizontalOffset = Math.Max(0d, Math.Min(_extent.Width - _viewport.Width, HorizontalOffset));
				VerticalOffset = Math.Max(0d, Math.Min(_extent.Height - _viewport.Height, VerticalOffset));
				if (MeasurementsChanged != null)
					MeasurementsChanged(this, EventArgs.Empty);
			}
			
			return new Size(Math.Min(availableSize.Width, _extent.Width), Math.Min(availableSize.Height, _extent.Height));
		}

		public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register("HorizontalOffset", typeof(double), typeof(ScrollPresenter), new PropertyMetadata(0d, new PropertyChangedCallback(OffsetPropertyChanged)));
		
		/// <summary> Gets or sets the horizontal offset. </summary>
		public double HorizontalOffset
		{
			get { return (double)GetValue(HorizontalOffsetProperty); }
			set { SetValue(HorizontalOffsetProperty, value); }
		}

		public void ScrollToHorizontalOffset(double target)
		{
			HorizontalOffset = Math.Max(0d, Math.Min(target, _extent.Width - _viewport.Width));
		}
		
		public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register("VerticalOffset", typeof(double), typeof(ScrollPresenter), new PropertyMetadata(0d, new PropertyChangedCallback(OffsetPropertyChanged)));
		
		/// <summary> Gets or sets the Vertical offset. </summary>
		public double VerticalOffset
		{
			get { return (double)GetValue(VerticalOffsetProperty); }
			set { SetValue(VerticalOffsetProperty, value); }
		}

		public void ScrollToVerticalOffset(double target)
		{
			VerticalOffset = Math.Max(0d, Math.Min(target, _extent.Height - _viewport.Height));
		}
		
		public event EventHandler OffsetsChanged;
		
		protected virtual void OnOffsetsChanged()
		{
			if (OffsetsChanged != null)
				OffsetsChanged(this, EventArgs.Empty);
		}
		
		private static void OffsetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var localSender = (ScrollPresenter)sender;
			localSender.InvalidateArrange();
			localSender.OnOffsetsChanged();
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
		    UpdateClip(finalSize);
		    UIElement content = (VisualTreeHelper.GetChildrenCount(this) == 0) ? null : (VisualTreeHelper.GetChild(this, 0) as UIElement);
		    if (content != null)
		        content.Arrange
		        (
		            new Rect
		            (
		                -HorizontalOffset,
		                -VerticalOffset,
		                Math.Max(content.DesiredSize.Width, finalSize.Width),
		                Math.Max(content.DesiredSize.Height, finalSize.Height)
		            )
		        );
		    return finalSize;
		}

		private RectangleGeometry _clippingRectangle;
		
		private void UpdateClip(Size arrangeSize)
		{
			if (_clippingRectangle == null)
			{
				_clippingRectangle = new RectangleGeometry();
				Clip = _clippingRectangle;
			}
			_clippingRectangle.Rect = new Rect(0d, 0d, arrangeSize.Width, arrangeSize.Height);
		}
	}
}
