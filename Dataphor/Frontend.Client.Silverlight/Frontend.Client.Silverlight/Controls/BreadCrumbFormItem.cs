using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplateVisualState(Name = "AcceptReject", GroupName = "AcceptRejectStates")]
	[TemplateVisualState(Name = "Close", GroupName = "AcceptRejectStates")]
	public class BreadCrumbFormItem : ContentControl
	{
		public BreadCrumbFormItem()
		{
			this.DefaultStyleKey = typeof(BreadCrumbFormItem);
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(FormControl), new PropertyMetadata(null));
		
		/// <summary> Gets or sets the title of the main form for this session. </summary>
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty IsAcceptRejectProperty = DependencyProperty.Register("IsAcceptReject", typeof(bool), typeof(FormControl), new PropertyMetadata(false, new PropertyChangedCallback(IsAcceptRejectChanged)));
		
		/// <summary> Gets or sets whether the main form is selected. </summary>
		public bool IsAcceptReject
		{
			get { return (bool)GetValue(IsAcceptRejectProperty); }
			set { SetValue(IsAcceptRejectProperty, value); }
		}
		
		private static void IsAcceptRejectChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((BreadCrumbFormItem)ASender).UpdateState(true);
		}

		public static readonly DependencyProperty FormProperty = DependencyProperty.Register("Form", typeof(FormControl), typeof(BreadCrumbFormItem), new PropertyMetadata(false));
		
		/// <summary> Gets or sets whether the main form is selected. </summary>
		public FormControl Form
		{
			get { return (FormControl)GetValue(FormProperty); }
			set { SetValue(FormProperty, value); }
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateState(false);
		}

		private void UpdateState(bool AUseAnimations)
		{
			if (IsAcceptReject)
				VisualStateManager.GoToState(this, "AcceptReject", AUseAnimations);
			else
				VisualStateManager.GoToState(this, "Close", AUseAnimations);
		}
	}
}
