using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class FormStackControl : Control
	{
		public FormStackControl()
		{
			DefaultStyleKey = typeof(FormStackControl);
			
			FormStack = new FormStack();
		}
		
		public static readonly DependencyProperty FormStackProperty = DependencyProperty.Register("FormStack", typeof(FormStack), typeof(FormStackControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets the forms of this stack. </summary>
		public FormStack FormStack
		{
			get { return (FormStack)GetValue(FormStackProperty); }
			set { SetValue(FormStackProperty, value); }
		}
	}

	public class FormStack : ObservableCollectionView<FormControl>
	{
		public void Push(FormControl form)
		{
			Add(form);
			MoveCurrentToLast();
		}
		
		public FormControl Pop()
		{
			if (Count > 0)
			{
				var popped = this[Count - 1];
				if (CurrentPosition == Count - 1)
					MoveCurrentToPrevious();
				RemoveAt(Count - 1);
				return popped;
			}
			else
				return null;
		}

		public bool TopMatches(FormControl formControl)
		{
			return Count > 0 && this[Count - 1] == formControl;
		}
	}
}
