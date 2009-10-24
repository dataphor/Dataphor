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

		private static void CanScrollPropertyChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((ScrollPresenter)ASender).InvalidateMeasure();
		}
				
		private Size FExtent = new Size(0d, 0d);
		
		public double ExtentHeight
		{
			get { return FExtent.Height; }
		}
		
		public double ExtentWidth
		{
			get { return FExtent.Width; }
		}
		
		private Size FViewport = new Size(0d, 0d);

		public double ViewportHeight
		{
			get { return FViewport.Height; }
		}
		
		public double ViewportWidth
		{
			get { return FViewport.Width; }
		}

		/// <summary> Raised when the extent or viewport changes. </summary>
		public event EventHandler MeasurementsChanged;
		
		protected override Size MeasureOverride(Size AAvailableSize)
		{
			var LInner = AAvailableSize;
			if (CanHorizontallyScroll)
				LInner.Width = Double.PositiveInfinity;
			if (CanVerticallyScroll)
				LInner.Height = Double.PositiveInfinity;
			
			UIElement LContent = (VisualTreeHelper.GetChildrenCount(this) == 0) ? null : (VisualTreeHelper.GetChild(this, 0) as UIElement);
			if (LContent != null)
				LContent.Measure(LInner);
			
			var LExtent = LContent == null ? new Size(0d, 0d) : LContent.DesiredSize;
			var LViewport = AAvailableSize;
			if (LExtent != FExtent || LViewport != FViewport)
			{
				FExtent = LExtent;
				FViewport = LViewport;
				HorizontalOffset = Math.Max(0d, Math.Min(FExtent.Width - FViewport.Width, HorizontalOffset));
				VerticalOffset = Math.Max(0d, Math.Min(FExtent.Height - FViewport.Height, VerticalOffset));
				if (MeasurementsChanged != null)
					MeasurementsChanged(this, EventArgs.Empty);
			}
			
			return new Size(Math.Min(AAvailableSize.Width, FExtent.Width), Math.Min(AAvailableSize.Height, FExtent.Height));
		}

		public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register("HorizontalOffset", typeof(double), typeof(ScrollPresenter), new PropertyMetadata(0d, new PropertyChangedCallback(OffsetPropertyChanged)));
		
		/// <summary> Gets or sets the horizontal offset. </summary>
		public double HorizontalOffset
		{
			get { return (double)GetValue(HorizontalOffsetProperty); }
			set { SetValue(HorizontalOffsetProperty, value); }
		}

		public void ScrollToHorizontalOffset(double ATarget)
		{
			HorizontalOffset = Math.Max(0d, Math.Min(ATarget, FExtent.Width - FViewport.Width));
		}
		
		public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register("VerticalOffset", typeof(double), typeof(ScrollPresenter), new PropertyMetadata(0d, new PropertyChangedCallback(OffsetPropertyChanged)));
		
		/// <summary> Gets or sets the Vertical offset. </summary>
		public double VerticalOffset
		{
			get { return (double)GetValue(VerticalOffsetProperty); }
			set { SetValue(VerticalOffsetProperty, value); }
		}

		public void ScrollToVerticalOffset(double ATarget)
		{
			VerticalOffset = Math.Max(0d, Math.Min(ATarget, FExtent.Height - FViewport.Height));
		}
		
		public event EventHandler OffsetsChanged;
		
		protected virtual void OnOffsetsChanged()
		{
			if (OffsetsChanged != null)
				OffsetsChanged(this, EventArgs.Empty);
		}
		
		private static void OffsetPropertyChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			var LSender = (ScrollPresenter)ASender;
			LSender.InvalidateArrange();
			LSender.OnOffsetsChanged();
		}

		protected override Size ArrangeOverride(Size AFinalSize)
		{
		    UpdateClip(AFinalSize);
		    UIElement LContent = (VisualTreeHelper.GetChildrenCount(this) == 0) ? null : (VisualTreeHelper.GetChild(this, 0) as UIElement);
		    if (LContent != null)
		        LContent.Arrange
		        (
		            new Rect
		            (
		                -HorizontalOffset,
		                -VerticalOffset,
		                Math.Max(LContent.DesiredSize.Width, AFinalSize.Width),
		                Math.Max(LContent.DesiredSize.Height, AFinalSize.Height)
		            )
		        );
		    return AFinalSize;
		}

		private RectangleGeometry FClippingRectangle;
		
		private void UpdateClip(Size AArrangeSize)
		{
			if (FClippingRectangle == null)
			{
				FClippingRectangle = new RectangleGeometry();
				Clip = FClippingRectangle;
			}
			FClippingRectangle.Rect = new Rect(0d, 0d, AArrangeSize.Width, AArrangeSize.Height);
		}
	}
}
