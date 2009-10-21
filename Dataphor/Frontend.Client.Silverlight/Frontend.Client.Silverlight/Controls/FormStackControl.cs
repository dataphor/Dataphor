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
		
		public static readonly DependencyProperty FormStackProperty = DependencyProperty.Register("FormStack", typeof(FormStack), typeof(FormStackControl), new PropertyMetadata(null, new PropertyChangedCallback(FormStackChanged)));
		
		/// <summary> Gets or sets the forms of this stack. </summary>
		public FormStack FormStack
		{
			get { return (FormStack)GetValue(FormStackProperty); }
			set { SetValue(FormStackProperty, value); }
		}
		
		private static void FormStackChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			var LSender = (FormStackControl)ASender;
			//LSender.UpdateSelectedForm();
			LSender.UpdateFormStack();
		}

		private void UpdateFormStack()
		{
			//if (FormStackView == null)
			//    FormStackView = new CollectionViewSource();
			// TODO: is this detach/reattach necessary?
			//FormStackView.View.CurrentChanged -= new EventHandler(CurrentFormStackChanged);
			//FormStackView.Source = FormStack;
			//FormStackView.View.MoveCurrentToFirst();
			//FormStackView.View.CurrentChanged += new EventHandler(CurrentFormStackChanged);
		}

		//private void CurrentFormStackChanged(object ASender, EventArgs AArgs)
		//{
			
		//}
		
		//public static readonly DependencyProperty FormStackViewProperty = DependencyProperty.Register("FormStackView", typeof(CollectionViewSource), typeof(FormStackControl), new PropertyMetadata(null));
		
		///// <summary> Gets or sets the collection view source that wraps the form stack. </summary>
		//public CollectionViewSource FormStackView
		//{
		//    get { return (CollectionViewSource)GetValue(FormStackViewProperty); }
		//    set { SetValue(FormStackViewProperty, value); }
		//}
		
		//public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(FormStackControl), new PropertyMetadata(-1, new PropertyChangedCallback(SelectedIndexChanged)));
		
		///// <summary> Gets or sets the index of the selected form in this stack. </summary>
		//public int SelectedIndex
		//{
		//    get { return (int)GetValue(SelectedIndexProperty); }
		//    set { SetValue(SelectedIndexProperty, value); }
		//}
		
		//private static void SelectedIndexChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		//{
		//    ((FormStackControl)ASender).UpdateSelectedForm();
		//}
		
		//public static readonly DependencyProperty SelectedFormProperty = DependencyProperty.Register("SelectedForm", typeof(FormControl), typeof(FormStackControl), new PropertyMetadata(null));
		
		///// <summary> Gets or sets the index of the selected form in this stack. </summary>
		//public FormControl SelectedForm
		//{
		//    get { return (FormControl)GetValue(SelectedFormProperty); }
		//    set { SetValue(SelectedFormProperty, value); }
		//}

		//public void UpdateSelectedForm()
		//{
		//    SelectedForm = 
		//        (FormStack == null || SelectedIndex < 0 || SelectedIndex >= FormStack.Count) 
		//            ? null 
		//            : FormStack[SelectedIndex]; 
		//}
	}

	public class FormStack : ObservableCollectionView<FormControl>
	{
		public void Push(FormControl AForm)
		{
			Add(AForm);
			MoveCurrentToLast();
		}
		
		public FormControl Pop()
		{
			if (Count > 0)
			{
				var LPopped = this[Count - 1];
				if (CurrentPosition == Count - 1)
					MoveCurrentToPrevious();
				RemoveAt(Count - 1);
				return LPopped;
			}
			else
				return null;
		}

		public bool TopMatches(FormControl AFormControl)
		{
			return Count > 0 && this[Count - 1] == AFormControl;
		}
	}
}
