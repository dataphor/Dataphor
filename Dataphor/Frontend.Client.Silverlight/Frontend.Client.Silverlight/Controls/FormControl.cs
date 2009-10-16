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
		
		/// <summary> Gets or sets whether the main form is selected. </summary>
		public bool IsAcceptReject
		{
			get { return (bool)GetValue(IsAcceptRejectProperty); }
			set { SetValue(IsAcceptRejectProperty, value); }
		}

		public event CloseHandler CloseRequested;
		
		public void RequestClose(CloseBehavior ABehavior)
		{
			if (CloseRequested != null)
				CloseRequested(this, ABehavior);
		}

		public event CloseHandler Closed;
		
		public void Close(CloseBehavior ABehavior)
		{
			if (Closed != null)
				Closed(this, ABehavior);
		}

		public void Show()
		{
			throw new NotImplementedException();
		}

		internal void Show(FormControl AParent)
		{
			throw new NotImplementedException();
		}
	}
	
	public delegate void CloseHandler(object ASender, CloseBehavior ABehavior);
}
