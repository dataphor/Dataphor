using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;


namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplatePart(Name = "AcceptButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "RejectButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "CloseButton", Type = typeof(ButtonBase))]
	[TemplateVisualState(Name = "AcceptReject", GroupName = "AcceptRejectStates")]
	[TemplateVisualState(Name = "Close", GroupName = "AcceptRejectStates")]
	[TemplateVisualState(Name = "Enabled", GroupName = "EnabledStates")]
	[TemplateVisualState(Name = "Disabled", GroupName = "EnabledStates")]
	public class BreadCrumbFormItem : Control
	{
		public BreadCrumbFormItem()
		{
			this.DefaultStyleKey = typeof(BreadCrumbFormItem);
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(BreadCrumbFormItem), new PropertyMetadata(null));
		
		/// <summary> Gets or sets the title of the associated form. </summary>
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty IsAcceptRejectProperty = DependencyProperty.Register("IsAcceptReject", typeof(bool), typeof(BreadCrumbFormItem), new PropertyMetadata(false, new PropertyChangedCallback(IsAcceptRejectChanged)));
		
		/// <summary> Gets or sets whether the associated form is in accept/reject mode. </summary>
		public bool IsAcceptReject
		{
			get { return (bool)GetValue(IsAcceptRejectProperty); }
			set { SetValue(IsAcceptRejectProperty, value); }
		}
		
		private static void IsAcceptRejectChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((BreadCrumbFormItem)sender).UpdateState(true);
		}

		public static readonly DependencyProperty IsFormEnabledProperty = DependencyProperty.Register("IsFormEnabled", typeof(bool), typeof(BreadCrumbFormItem), new PropertyMetadata(false, new PropertyChangedCallback(IsFormEnabledChanged)));
		
		/// <summary> Gets or sets whether the associated form is enabled. </summary>
		public bool IsFormEnabled
		{
			get { return (bool)GetValue(IsFormEnabledProperty); }
			set { SetValue(IsFormEnabledProperty, value); }
		}
		
		private static void IsFormEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((BreadCrumbFormItem)sender).UpdateState(true);
		}

		public static readonly DependencyProperty FormProperty = DependencyProperty.Register("Form", typeof(FormControl), typeof(BreadCrumbFormItem), new PropertyMetadata(null, new PropertyChangedCallback(FormChanged)));
		
		/// <summary> Gets or sets the associated form. </summary>
		public FormControl Form
		{
			get { return (FormControl)GetValue(FormProperty); }
			set { SetValue(FormProperty, value); }
		}
		
		private static void FormChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
		}
		
		private ButtonBase _acceptButton;
		
		public ButtonBase AcceptButton
		{
			get { return _acceptButton; }
			set
			{
				if (_acceptButton != value)
				{
					if (_acceptButton != null)
						_acceptButton.Click -= AcceptButtonClick;
					_acceptButton = value;
					if (_acceptButton != null)
						_acceptButton.Click += AcceptButtonClick;
				}
			}
		}

		private void AcceptButtonClick(object sender, RoutedEventArgs e)
		{
			if (Form != null)
				Form.RequestClose(CloseBehavior.AcceptOrClose);
		}
		
		private ButtonBase _rejectButton;
		
		public ButtonBase RejectButton
		{
			get { return _rejectButton; }
			set
			{
				if (_rejectButton != value)
				{
					if (_rejectButton != null)
						_rejectButton.Click -= RejectButtonClick;
					_rejectButton = value;
					if (_rejectButton != null)
						_rejectButton.Click += RejectButtonClick;
				}
			}
		}

		private void RejectButtonClick(object sender, RoutedEventArgs e)
		{
			if (Form != null)
				Form.RequestClose(CloseBehavior.RejectOrClose);
		}
		
		private ButtonBase _closeButton;
		
		public ButtonBase CloseButton
		{
			get { return _closeButton; }
			set
			{
				if (_closeButton != value)
				{
					if (_closeButton != null)
						_closeButton.Click -= CloseButtonClick;
					_closeButton = value;
					if (_closeButton != null)
						_closeButton.Click += CloseButtonClick;
				}
			}
		}

		private void CloseButtonClick(object sender, RoutedEventArgs e)
		{
			if (Form != null)
				Form.RequestClose(CloseBehavior.AcceptOrClose);
		}
		
		public override void OnApplyTemplate()
		{
			AcceptButton = GetTemplateChild("AcceptButton") as ButtonBase;
			RejectButton = GetTemplateChild("RejectButton") as ButtonBase;
			CloseButton = GetTemplateChild("CloseButton") as ButtonBase;

			base.OnApplyTemplate();
			
			UpdateState(false);
		}

		private void UpdateState(bool useAnimations)
		{
			if (IsAcceptReject)
				VisualStateManager.GoToState(this, "AcceptReject", useAnimations);
			else
				VisualStateManager.GoToState(this, "Close", useAnimations);

			if (IsFormEnabled)
				VisualStateManager.GoToState(this, "Enabled", useAnimations);
			else
				VisualStateManager.GoToState(this, "Disabled", useAnimations);
		}
	}
}
