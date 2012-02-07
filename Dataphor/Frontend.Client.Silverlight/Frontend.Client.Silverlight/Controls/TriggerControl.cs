using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplateVisualState(Name = "HasImage", GroupName = "Image")]
	[TemplateVisualState(Name = "NoImage", GroupName = "Image")]
	[TemplateVisualState(Name = "OnlyImage", GroupName = "Image")]
	public class TriggerControl : Button
	{
		public TriggerControl()
		{
			this.DefaultStyleKey = typeof(TriggerControl);
		}
		
		public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(ImageSource), typeof(TriggerControl), new PropertyMetadata(null, new PropertyChangedCallback(ImagePropertyChanged)));
		
		/// <summary> Gets or sets the image to display on this button. </summary>
		public ImageSource Image
		{
			get { return (ImageSource)GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}
		
		public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(double), typeof(TriggerControl), new PropertyMetadata(0d, new PropertyChangedCallback(ImagePropertyChanged)));
		
		/// <summary> Gets or sets the width of the image to display on this button. </summary>
		public double ImageWidth
		{
			get { return (double)GetValue(ImageWidthProperty); }
			set { SetValue(ImageWidthProperty, value); }
		}

		public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof(double), typeof(TriggerControl), new PropertyMetadata(0d, new PropertyChangedCallback(ImagePropertyChanged)));
		
		/// <summary> Gets or sets the Height of the image to display on this button. </summary>
		public double ImageHeight
		{
			get { return (double)GetValue(ImageHeightProperty); }
			set { SetValue(ImageHeightProperty, value); }
		}

		private static void ImagePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((TriggerControl)sender).UpdateImage();
		}

		private void UpdateImage()
		{
			var bitmap = Image as BitmapImage;
			if (bitmap != null)
			{
				ImageWidth = bitmap.PixelWidth;
				ImageHeight = bitmap.PixelHeight;
			}
			UpdateState(true);
		}
		
		private void UpdateState(bool useTransitions)
		{
			if (Image != null || (ImageWidth > 0 && ImageHeight > 0))
			{
				if (Content == null)
					VisualStateManager.GoToState(this, "OnlyImage", useTransitions);
				else
					VisualStateManager.GoToState(this, "HasImage", useTransitions);
			}
			else
				VisualStateManager.GoToState(this, "NoImage", useTransitions);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateState(false);
		}
	}
}
