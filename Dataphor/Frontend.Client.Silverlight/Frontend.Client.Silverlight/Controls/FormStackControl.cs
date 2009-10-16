using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Collections.ObjectModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class FormStackControl : Control
	{
		public FormStackControl()
		{
			DefaultStyleKey = typeof(FormStackControl);
		}
		
		public static readonly DependencyProperty FormStackProperty = DependencyProperty.Register("FormStack", typeof(FormStack), typeof(FormStackControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets the forms of this stack. </summary>
		public FormStack FormStack
		{
			get { return (FormStack)GetValue(FormStackProperty); }
			set { SetValue(FormStackProperty, value); }
		}
	}

	public class FormStack : ObservableCollection<FormControl>
	{
	}
}
