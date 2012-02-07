using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class StaticText : Element, IStaticText
	{
		public const int DefaultWidth = 40;
				
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

		private string _text = "";
		[DefaultValue("")]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					UpdateBinding(TextBlock.TextProperty);
				}
			}
		}

		private object UIGetText()
		{
			return Text;
		}

		private int _width = DefaultWidth;
		[DefaultValue(DefaultWidth)]
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					if (_width < 1)
						throw new ClientException(ClientException.Codes.CharsPerLineInvalid);
					_width = value;
					UpdateBinding(FrameworkElement.WidthProperty);
				}
			}
		}

		private object UIGetWidth()
		{
			return _width * Silverlight.Session.AverageCharacterWidth;
		}
	}
}
