using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class FormControl : ContentControl
	{
		public FormControl()
		{
			DefaultStyleKey = typeof(FormControl);
		}
		
		public static readonly DependencyProperty IsAcceptRejectProperty = DependencyProperty.Register("IsAcceptReject", typeof(bool), typeof(FormControl), new PropertyMetadata(false));
		
		/// <summary> Gets or sets whether the form is in accept/reject mode. </summary>
		public bool IsAcceptReject
		{
			get { return (bool)GetValue(IsAcceptRejectProperty); }
			set { SetValue(IsAcceptRejectProperty, value); }
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(FormControl), new PropertyMetadata(null));
				
		/// <summary> Gets or sets the form's title. </summary>
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}
		
		public event CloseHandler CloseRequested;
		
		public void RequestClose(CloseBehavior ABehavior)
		{
			if (CloseRequested != null)
				CloseRequested(this, ABehavior);
		}
	}
	
	public delegate void CloseHandler(object ASender, CloseBehavior ABehavior);
}
