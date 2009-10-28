using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class Group : Element, IGroup, ISilverlightContainerElement
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new GroupBox();
		}

		protected ContentControl ContentControl
		{
			get { return (ContentControl)FrameworkElement; }
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(GroupBox.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		private string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get { return FTitle; }
			set
			{
				if (FTitle != value)
				{
					FTitle = value;
					UpdateBinding(GroupBox.HeaderProperty);
				}
			}
		}
		
		private object UIGetHeader()
		{
			if (String.IsNullOrEmpty(FTitle))
				return null;
			else
				return FTitle;
		}

		/// <remarks> This method is invoked on the main thread. </remarks>
		public void InsertChild(int AIndex, FrameworkElement AChild)
		{
			ContentControl.Content = AChild;
		}

		// Element

		public override int GetDefaultMarginLeft()
		{
			return 0;
		}

		public override int GetDefaultMarginRight()
		{
			return 0;
		}

		public override int GetDefaultMarginTop()
		{
			return 0;
		}

		public override int GetDefaultMarginBottom()
		{
			return 0;
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IElement).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}
	}
}
