using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Collections.ObjectModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class SessionControl : ContentControl
	{
		public SessionControl()
		{
			DefaultStyleKey = typeof(SessionControl);
		}
		
		public static readonly DependencyProperty MainFormTitleProperty = DependencyProperty.Register("MainFormTitle", typeof(string), typeof(SessionControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets the title of the main form for this session. </summary>
		public string MainFormTitle
		{
			get { return (string)GetValue(MainFormTitleProperty); }
			set { SetValue(MainFormTitleProperty, value); }
		}

		public static readonly DependencyProperty MainFormSelectedProperty = DependencyProperty.Register("MainFormSelected", typeof(bool), typeof(SessionControl), new PropertyMetadata(false));
		
		/// <summary> Gets or sets whether the main form is selected. </summary>
		public bool MainFormSelected
		{
			get { return (bool)GetValue(MainFormSelectedProperty); }
			set { SetValue(MainFormSelectedProperty, value); }
		}

		public static readonly DependencyProperty FormStacksProperty = DependencyProperty.Register("FormStacks", typeof(FormStacks), typeof(SessionControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets whether the main form is selected. </summary>
		public FormStacks FormStacks
		{
			get { return (FormStacks)GetValue(FormStacksProperty); }
			set { SetValue(FormStacksProperty, value); }
		}
	}
	
	public class FormStacks : ObservableCollection<FormStack>
	{
	}
}
