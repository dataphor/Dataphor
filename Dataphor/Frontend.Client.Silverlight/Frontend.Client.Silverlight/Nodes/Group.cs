using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Group : ContentElement, IGroup
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new GroupBox();
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(GroupBox.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		private string _title = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get { return _title; }
			set
			{
				if (_title != value)
				{
					_title = value;
					UpdateBinding(GroupBox.HeaderProperty);
				}
			}
		}
		
		private object UIGetHeader()
		{
			if (String.IsNullOrEmpty(_title))
				return null;
			else
				return _title;
		}
	}
}
