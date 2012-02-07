using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> When activeated, surrounds the content with a transparent popup. </summary>
	/// <remarks> If no content is given, this control covers the entire host with a transparent popup.  
	/// <p>When the popup is clicked (except for the content area), the Clicked event is invoked.</p> </remarks>
	public class PopupProtector : ContentControl
	{
		public PopupProtector()
		{
			this.DefaultStyleKey = typeof(PopupProtector);
		}

		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register("IsActive", typeof(bool), typeof(PopupProtector), new PropertyMetadata(false, new PropertyChangedCallback(OnIsActiveChanged)));

		/// <summary> If true, the popup protector is activated. </summary>
		public bool IsActive
		{
			get { return (bool)GetValue(IsActiveProperty); }
			set { SetValue(IsActiveProperty, value); }
		}

		private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((PopupProtector)d).OnIsActiveChanged((bool)e.OldValue, (bool)e.NewValue);
		}

		public static readonly DependencyProperty ProtectorBrushProperty =
			DependencyProperty.Register("ProtectorBrush", typeof(Brush), typeof(PopupProtector), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), null));

		/// <summary> Gets or sets the brush used for the protector popup area surrounding the content.  </summary>
		/// <remarks> Defaults to Transparent. </remarks>
		public Brush ProtectorBrush
		{
			get { return (Brush)GetValue(ProtectorBrushProperty); }
			set { SetValue(ProtectorBrushProperty, value); }
		}

		private Popup _backgroundPopup;
		private Canvas _backgroundCanvas;
		
		protected virtual void OnIsActiveChanged(bool oldValue, bool newValue)
		{
			if (newValue)
			{
				ApplyTemplate();

				if (_backgroundPopup == null)
				{
					_backgroundPopup = new Popup();
					_backgroundCanvas = new Canvas();
					_backgroundPopup.Child = _backgroundCanvas;
					_backgroundCanvas.MouseLeftButtonDown += new MouseButtonEventHandler(BackgroundClicked);
				}

				// Listen to content size changes
				var content = GetContent();
				if (content != null)
					content.SizeChanged += new SizeChangedEventHandler(ContentSizeChanged);

				// Listen to host size changes
				Application.Current.Host.Content.Resized += new EventHandler(PageResized);

				PrepareBackgroundCanvas();

				_backgroundPopup.IsOpen = true;
			}
			else
			{
				UnprepareBackgroundCanvas();

				// Stop listening to content size changes
				var content = GetContent();
				if (content != null)
					content.SizeChanged -= new SizeChangedEventHandler(ContentSizeChanged);

				// Stop listening to host size changes
				Application.Current.Host.Content.Resized -= new EventHandler(PageResized);

				if (_backgroundPopup != null)
					_backgroundPopup.IsOpen = false;
			}
		}

		private void PrepareBackgroundCanvas()
		{
			var size = GetBackgroundCanvasSize();

			var content = GetContent();
			if (content != null)
			{
				// Build covering canvas' surrounding the content
				var group = new TransformGroup();
				group.Children.Add(content.TransformToVisual(null) as Transform);
				if (Application.Current.Host.Settings.EnableAutoZoom)
				{
					var scale = 1d / Application.Current.Host.Content.ZoomFactor;
					group.Children.Add(new ScaleTransform { ScaleX = scale, ScaleY = scale });
				}
				Rect bounds = group.TransformBounds(new Rect(0d, 0d, content.ActualWidth, content.ActualHeight));

				AddCanvas(0d, bounds.Top, bounds.Left, bounds.Height);	// Side left
				AddCanvas(0d, 0d, size.Width, bounds.Top);	// Entire top
				AddCanvas(bounds.Right, bounds.Top, size.Width - bounds.Right, bounds.Height);	// Side right
				AddCanvas(0d, bounds.Bottom, size.Width, size.Height - bounds.Bottom);	// Entire bottom
			}
			else
				// No content, so cover entire host area
				AddCanvas(0d, 0d, size.Width, size.Height);

			_backgroundCanvas.Width = size.Width;
			_backgroundCanvas.Height = size.Height;
		}

		private FrameworkElement GetContent()
		{
			return (VisualTreeHelper.GetChildrenCount(this) == 0) ? null : (VisualTreeHelper.GetChild(this, 0) as FrameworkElement);
		}

		private void AddCanvas(double left, double top, double width, double height)
		{
			if (width > 0d && height > 0d)
			{
				var rect = new System.Windows.Shapes.Rectangle();

				var backgroundBinding = new Binding("ProtectorBrush");
				backgroundBinding.Source = this;
				rect.SetBinding(System.Windows.Shapes.Rectangle.FillProperty, backgroundBinding);
			
				Canvas.SetLeft(rect, left);
				Canvas.SetTop(rect, top);
				rect.Width = width;
				rect.Height = height;
			
				_backgroundCanvas.Children.Add(rect);
			}
		}

		private void UnprepareBackgroundCanvas()
		{
			// Empty background
			while (_backgroundCanvas.Children.Count > 0)
				_backgroundCanvas.Children.RemoveAt(0);
		}

		private void ContentSizeChanged(object sender, SizeChangedEventArgs e)
		{
			ReprepareBackground();
		}

		private void ReprepareBackground()
		{
			UnprepareBackgroundCanvas();
			PrepareBackgroundCanvas();
		}
		
		private Size GetBackgroundCanvasSize()
		{
			var size = new Size(Application.Current.Host.Content.ActualWidth, Application.Current.Host.Content.ActualHeight);
			if (Application.Current.Host.Settings.EnableAutoZoom)
			{
				double zoomFactor = Application.Current.Host.Content.ZoomFactor;
				if (zoomFactor != 0.0)
				{
					size.Height /= zoomFactor;
					size.Width /= zoomFactor;
				}
			}
			return size;
		}

		private void PageResized(object sender, EventArgs args)
		{
			ReprepareBackground();
		}

		private void BackgroundClicked(object sender, MouseButtonEventArgs e)
		{
 			OnClicked();
		}
		
		public event RoutedEventHandler Clicked;
		
		protected virtual void OnClicked()
		{
			if (Clicked != null)
				Clicked(this, new RoutedEventArgs());
		}
	}
}
