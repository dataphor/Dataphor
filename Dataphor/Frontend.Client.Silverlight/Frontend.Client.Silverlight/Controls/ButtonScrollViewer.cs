using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplatePart(Name = "ScrollPresenter", Type = typeof(ScrollContentPresenter))]
	[TemplatePart(Name = "LeftButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "RightButton", Type = typeof(ButtonBase))]
	[TemplateVisualState(GroupName = "Left", Name = "LeftDisabled")]
	[TemplateVisualState(GroupName = "Left", Name = "LeftEnabled")]
	[TemplateVisualState(GroupName = "Right", Name = "RightDisabled")]
	[TemplateVisualState(GroupName = "Right", Name = "RightEnabled")]
	public class ButtonScrollViewer : ContentControl
	{
		public ButtonScrollViewer()
		{
			this.DefaultStyleKey = typeof(ButtonScrollViewer);
		}

		public override void OnApplyTemplate()
		{
			ScrollPresenter = GetTemplateChild("ScrollPresenter") as ScrollContentPresenter;
			LeftButton = GetTemplateChild("LeftButton") as ButtonBase;
			RightButton = GetTemplateChild("RightButton") as ButtonBase;
			
			base.OnApplyTemplate();
			
			UpdateStates(false);
		}
		
		private ScrollContentPresenter FScrollPresenter;
		
		public ScrollContentPresenter ScrollPresenter
		{
			get { return FScrollPresenter; }
			set
			{
				if (FScrollPresenter != value)
				{
					if (FScrollPresenter != null)
						FScrollPresenter.LayoutUpdated -= ScrollPresenterLayoutUpdated;
					FScrollPresenter = value;
					if (FScrollPresenter != null)
						FScrollPresenter.LayoutUpdated += ScrollPresenterLayoutUpdated;
				}
			}
		}
		
		private void ScrollPresenterLayoutUpdated(object ASender, EventArgs AArgs)
		{
			UpdateStates(true);
		}

		protected void UpdateStates(bool AUseTransitions)
		{
			if (FScrollPresenter != null)
			{
				if (FScrollPresenter.ExtentWidth - FScrollPresenter.HorizontalOffset > FScrollPresenter.ViewportWidth)
					VisualStateManager.GoToState(this, "RightEnabled", AUseTransitions);
				else
					VisualStateManager.GoToState(this, "RightDisabled", AUseTransitions);

				if (FScrollPresenter.HorizontalOffset > 0)
					VisualStateManager.GoToState(this, "LeftEnabled", AUseTransitions);
				else
					VisualStateManager.GoToState(this, "LeftDisabled", AUseTransitions);
			}
		}

		private ButtonBase FLeftButton;
		
		public ButtonBase LeftButton
		{
			get { return FLeftButton; }
			set
			{
				if (FLeftButton != value)
				{
					if (FLeftButton != null)
						FLeftButton.Click -= LeftButtonClick;
					FLeftButton = value;
					if (FLeftButton != null)
						FLeftButton.Click += LeftButtonClick;
				}
			}
		}
		
		private void LeftButtonClick(object ASender, RoutedEventArgs AArgs)
		{
			if (FScrollPresenter != null)
				FScrollPresenter.LineLeft();
		}

		private ButtonBase FRightButton;
		
		public ButtonBase RightButton
		{
			get { return FRightButton; }
			set
			{
				if (FRightButton != value)
				{
					if (FRightButton != null)
						FRightButton.Click -= RightButtonClick;
					FRightButton = value;
					if (FRightButton != null)
						FRightButton.Click += RightButtonClick;
				}
			}
		}
		
		private void RightButtonClick(object ASender, RoutedEventArgs AArgs)
		{
			if (FScrollPresenter != null)
				FScrollPresenter.LineRight();
		}
	}
}
