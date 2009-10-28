using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Shapes;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	// TODO: Support the HtmlBox
	public class HtmlBox : Element
	{
		public const int CDefaultWidth = 100;
		public const int CDefaultHeight = 100;

		private int FPixelWidth = CDefaultWidth;
		[DefaultValue(CDefaultWidth)]
		public int PixelWidth
		{
			get { return FPixelWidth; }
			set
			{
				if (FPixelWidth != value)
				{
					FPixelWidth = value;
				}
			}
		}

		private int FPixelHeight = CDefaultHeight;
		[DefaultValue(CDefaultHeight)]
		public int PixelHeight
		{
			get { return FPixelHeight; }
			set
			{
				if (FPixelHeight != value)
				{
					FPixelHeight = value;
				}
			}
		}

		private string FURL = String.Empty;
		[DefaultValue("")]
		public string URL
		{
			get { return FURL; }
			set
			{
				if (FURL != value)
				{
					FURL = value;
				}
			}
		}

		protected override FrameworkElement CreateFrameworkElement()
		{
			return new Rectangle();
		}
	}
}
