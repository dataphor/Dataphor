using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class StaticText : Element, IStaticText
	{
		public const int CDefaultWidth = 40;
				
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new TextBlock();
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(TextBlock.TextProperty, new Func<object>(UIGetText));
			AddBinding(FrameworkElement.WidthProperty, new Func<object>(UIGetWidth));
		}

		protected override string GetDefaultStyle()
		{
			return "StaticTextStyle";
		}

		private string FText = "";
		[DefaultValue("")]
		public string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					UpdateBinding(TextBlock.TextProperty);
				}
			}
		}

		private object UIGetText()
		{
			return Text;
		}

		private int FWidth = CDefaultWidth;
		[DefaultValue(CDefaultWidth)]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					if (FWidth < 1)
						throw new ClientException(ClientException.Codes.CharsPerLineInvalid);
					FWidth = value;
					UpdateBinding(FrameworkElement.WidthProperty);
				}
			}
		}

		private object UIGetWidth()
		{
			return FWidth * Silverlight.Session.AverageCharacterWidth;
		}
	}
}
