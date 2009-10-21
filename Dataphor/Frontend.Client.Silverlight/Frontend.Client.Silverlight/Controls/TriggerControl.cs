using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class TriggerControl : Button
	{
		public TriggerControl()
		{
			this.DefaultStyleKey = typeof(TriggerControl);
		}
		
		public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(ImageSource), typeof(TriggerControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets the image to display on this button. </summary>
		public ImageSource Image
		{
			get { return (ImageSource)GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}

		public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(double), typeof(TriggerControl), new PropertyMetadata(0d));
		
		/// <summary> Gets or sets the width of the image to display on this button. </summary>
		public double ImageWidth
		{
			get { return (double)GetValue(ImageWidthProperty); }
			set { SetValue(ImageWidthProperty, value); }
		}

		public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof(double), typeof(TriggerControl), new PropertyMetadata(0d));
		
		/// <summary> Gets or sets the Height of the image to display on this button. </summary>
		public double ImageHeight
		{
			get { return (double)GetValue(ImageHeightProperty); }
			set { SetValue(ImageHeightProperty, value); }
		}
	}
}
