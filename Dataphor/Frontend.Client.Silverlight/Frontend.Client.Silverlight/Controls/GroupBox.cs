using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <remarks> This class is named GroupBox to purposely collide with the WPF framework's version
	///	so that if MS introduces this control into SL less will have to change.	 
	///	Props to: http://leeontech.wordpress.com/2008/04/10/groupbox/
	/// </remarks>
	[TemplateVisualState(Name = "Enabled", GroupName = "EnabledGroup")]
	[TemplateVisualState(Name = "Disabled", GroupName = "EnabledGroup")]
	[TemplateVisualState(Name = "HeaderNull", GroupName = "HeaderGroup")]
	[TemplateVisualState(Name = "HeaderNotNull", GroupName = "HeaderGroup")]
	public class GroupBox : ContentControl
	{
		public GroupBox()
		{
			DefaultStyleKey = typeof(GroupBox);
			
			IsEnabledChanged += new DependencyPropertyChangedEventHandler(BaseIsEnabledChanged);
		}

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(object), typeof(GroupBox), null);

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(GroupBox), null);

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

		private void BaseIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStates(true);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateVisualStates(false);
		}

		private void UpdateVisualStates(bool useTransitions)
		{
			if (IsEnabled)
				VisualStateManager.GoToState(this, "Enabled", useTransitions);
			else
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			
			if (Header == null)
				VisualStateManager.GoToState(this, "HeaderNull", useTransitions);
			else
				VisualStateManager.GoToState(this, "HeaderNotNull", useTransitions);
		}
		
	}
}
