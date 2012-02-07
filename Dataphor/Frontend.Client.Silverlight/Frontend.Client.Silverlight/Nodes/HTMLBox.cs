using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Shapes;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	// TODO: Support the HtmlBox
	public class HtmlBox : Element, IHtmlBox
	{
		public const int DefaultWidth = 100;
		public const int DefaultHeight = 100;

		private int _pixelWidth = DefaultWidth;
		[DefaultValue(DefaultWidth)]
		public int PixelWidth
		{
			get { return _pixelWidth; }
			set
			{
				if (_pixelWidth != value)
				{
					_pixelWidth = value;
				}
			}
		}

		private int _pixelHeight = DefaultHeight;
		[DefaultValue(DefaultHeight)]
		public int PixelHeight
		{
			get { return _pixelHeight; }
			set
			{
				if (_pixelHeight != value)
				{
					_pixelHeight = value;
				}
			}
		}

		private string _uRL = String.Empty;
		[DefaultValue("")]
		public string URL
		{
			get { return _uRL; }
			set
			{
				if (_uRL != value)
				{
					_uRL = value;
				}
			}
		}

		protected override FrameworkElement CreateFrameworkElement()
		{
			return new Rectangle();
		}
	}
}
