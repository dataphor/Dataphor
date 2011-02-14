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
		
		private ScrollPresenter _scrollPresenter;
		
		public ScrollPresenter ScrollPresenter
		{
			get { return _scrollPresenter; }
			set
			{
				if (_scrollPresenter != value)
				{
					if (_scrollPresenter != null)
					{
						_scrollPresenter.MeasurementsChanged -= ScrollPresenterMeasurementsOrOffsetsChanged;
						_scrollPresenter.OffsetsChanged -= ScrollPresenterMeasurementsOrOffsetsChanged;
					}
					_scrollPresenter = value;
					if (_scrollPresenter != null)
					{
						_scrollPresenter.MeasurementsChanged += ScrollPresenterMeasurementsOrOffsetsChanged;
						_scrollPresenter.OffsetsChanged += ScrollPresenterMeasurementsOrOffsetsChanged;
					}
				}
			}
		}

		private void ScrollPresenterSizeChanged(object sender, SizeChangedEventArgs e)
		{
			InvalidateMeasure();
		}
		
		private void ScrollPresenterMeasurementsOrOffsetsChanged(object sender, EventArgs args)
		{
			InvalidateMeasure();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var result = base.MeasureOverride(availableSize);
			UpdateStates(false);
			return result;			
		}
		
		protected void UpdateStates(bool useTransitions)
		{
			if (_scrollPresenter != null)
			{
				if (_scrollPresenter.ExtentWidth - _scrollPresenter.HorizontalOffset > _scrollPresenter.ViewportWidth)
					VisualStateManager.GoToState(this, "RightEnabled", useTransitions);
				else
					VisualStateManager.GoToState(this, "RightDisabled", useTransitions);

				if (_scrollPresenter.HorizontalOffset > 0)
					VisualStateManager.GoToState(this, "LeftEnabled", useTransitions);
				else
					VisualStateManager.GoToState(this, "LeftDisabled", useTransitions);
			}
		}

		private ButtonBase _leftButton;
		
		public ButtonBase LeftButton
		{
			get { return _leftButton; }
			set
			{
				if (_leftButton != value)
				{
					if (_leftButton != null)
						_leftButton.Click -= LeftButtonClick;
					_leftButton = value;
					if (_leftButton != null)
						_leftButton.Click += LeftButtonClick;
				}
			}
		}
		
		private void LeftButtonClick(object sender, RoutedEventArgs args)
		{
			if (_scrollPresenter != null)
				_scrollPresenter.ScrollToHorizontalOffset(_scrollPresenter.HorizontalOffset - 16);
		}

		private ButtonBase _rightButton;
		
		public ButtonBase RightButton
		{
			get { return _rightButton; }
			set
			{
				if (_rightButton != value)
				{
					if (_rightButton != null)
						_rightButton.Click -= RightButtonClick;
					_rightButton = value;
					if (_rightButton != null)
						_rightButton.Click += RightButtonClick;
				}
			}
		}
		
		private void RightButtonClick(object sender, RoutedEventArgs args)
		{
			if (_scrollPresenter != null)
				_scrollPresenter.ScrollToHorizontalOffset(_scrollPresenter.HorizontalOffset + 10);
		}
	}
}
