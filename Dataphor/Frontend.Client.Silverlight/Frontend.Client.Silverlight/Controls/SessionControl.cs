using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class SessionControl : Control
	{
		public SessionControl()
		{
			DefaultStyleKey = typeof(SessionControl);
			
			FormStacks = new FormStacks();
		}
		
		public static readonly DependencyProperty FormStacksProperty = DependencyProperty.Register("FormStacks", typeof(FormStacks), typeof(SessionControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets whether the main form is selected. </summary>
		public FormStacks FormStacks
		{
			get { return (FormStacks)GetValue(FormStacksProperty); }
			set { SetValue(FormStacksProperty, value); }
		}


		public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool), typeof(SessionControl), new PropertyMetadata(false));

		/// <summary> Gets or sets whether or not the session is busy processing. </summary>
		public bool IsBusy
		{
			get { return (bool)GetValue(IsBusyProperty); }
			set { SetValue(IsBusyProperty, value); }
		}
	}
	
	public class FormStacks : ObservableCollectionView<FormStackControl>
	{
		public virtual FormStackControl Create()
		{
			var LStack = new FormStackControl();
			InitializeFormStackControl(LStack);
			Add(LStack);
			MoveCurrentToPosition(Count - 1);
			return LStack;
		}

		public FormStackControl Find(FormControl AForm)
		{
			foreach (var LItem in this)
				if (LItem.FormStack.TopMatches(AForm))
					return LItem;
			return null;
		}

		protected virtual void InitializeFormStackControl(FormStackControl AStack)
		{
			// pure virtual
		}
	}
}
