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

		protected override void InitializeFrameworkElement()
		{
			base.InitializeFrameworkElement();
			
			var LBinding = new Binding("BindText");
			LBinding.Source = this;
			FrameworkElement.SetBinding(TextBlock.TextProperty, LBinding);
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
					NotifyPropertyChanged("BindText");
				}
			}
		}

		public string BindText
		{
			get { return FText; }
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
					UpdateWidth();
				}
			}
		}

		protected void UpdateWidth()
		{
			BindWidth = FWidth * Silverlight.Session.AverageCharacterWidth;
		}
		
		private double FBindWidth;
		
		public double BindWidth
		{
			get { return FBindWidth; }
			set
			{
				if (FBindWidth != value)
				{
					FBindWidth = value;
					NotifyPropertyChanged("BindWidth");
				}
			}
		}

		protected override void Activate()
		{
			UpdateWidth();
			base.Activate();
		}
	}
}
