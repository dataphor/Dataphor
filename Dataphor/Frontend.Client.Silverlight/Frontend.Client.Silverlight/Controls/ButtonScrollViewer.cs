using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[TemplatePart(Name = "ScrollPresenter", Type = typeof(ScrollPresenter))]
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
			ScrollPresenter = GetTemplateChild("ScrollPresenter") as ScrollPresenter;
			LeftButton = GetTemplateChild("LeftButton") as ButtonBase;
			RightButton = GetTemplateChild("RightButton") as ButtonBase;
			
			base.OnApplyTemplate();
			
			UpdateStates(false);
		}
		
		private ScrollPresenter FScrollPresenter;
		
		public ScrollPresenter ScrollPresenter
		{
			get { return FScrollPresenter; }
			set
			{
				if (FScrollPresenter != value)
				{
					if (FScrollPresenter != null)
					{
						FScrollPresenter.MeasurementsChanged -= ScrollPresenterMeasurementsOrOffsetsChanged;
						FScrollPresenter.OffsetsChanged -= ScrollPresenterMeasurementsOrOffsetsChanged;
					}
					FScrollPresenter = value;
					if (FScrollPresenter != null)
					{
						FScrollPresenter.MeasurementsChanged += ScrollPresenterMeasurementsOrOffsetsChanged;
						FScrollPresenter.OffsetsChanged += ScrollPresenterMeasurementsOrOffsetsChanged;
					}
				}
			}
		}

		private void ScrollPresenterSizeChanged(object sender, SizeChangedEventArgs e)
		{
			InvalidateMeasure();
		}
		
		private void ScrollPresenterMeasurementsOrOffsetsChanged(object ASender, EventArgs AArgs)
		{
			InvalidateMeasure();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var LResult = base.MeasureOverride(availableSize);
			UpdateStates(false);
			return LResult;			
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
				FScrollPresenter.ScrollToHorizontalOffset(FScrollPresenter.HorizontalOffset - 16);
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
				FScrollPresenter.ScrollToHorizontalOffset(FScrollPresenter.HorizontalOffset + 10);
		}
	}
}
