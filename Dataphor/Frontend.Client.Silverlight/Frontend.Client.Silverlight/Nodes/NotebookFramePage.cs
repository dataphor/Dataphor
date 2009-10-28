using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class NotebookFramePage : Frame, INotebookFramePage, ISilverlightBaseNotebookPage
	{
		protected override FrameworkElement CreateFrameworkElement()
		{
			return new TabItem();
		}
		
		public TabItem TabItem
		{
			get { return (TabItem)FrameworkElement; }
		}

		private bool FIsSelected;
		
		public virtual void Selected()
		{
			FIsSelected = true;
			UpdateFrameInterface(false);
		}

		public virtual void Unselected()
		{
			FIsSelected = false;
			UpdateFrameInterface(false);
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(HeaderedItemsControl.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		protected string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return FTitle; }
			set
			{
				FTitle = value;
				UpdateBinding(HeaderedItemsControl.HeaderProperty);
			}
		}
		
		private object UIGetHeader()
		{
			return String.IsNullOrEmpty(FTitle) ? null : FTitle;
		}

		public override bool IsValidOwner(Type AOwnerType)
		{
			return typeof(INotebook).IsAssignableFrom(AOwnerType);
		}

		protected override void UpdateFrameInterface(bool AForce)
		{
			// If the frame should be loaded and it is not, or vise versa... then fix it
			if 
			(
				Active && 
				(
					(ShouldLoad() == (FrameInterfaceNode == null)) || 
					AForce
				)
			)
				ResetFrameInterfaceNode(Active && ShouldLoad());
		}

		private bool ShouldLoad()
		{
			return (Document != String.Empty) && (FIsSelected || !FLoadAsSelected);
		}

		// LoadAsSelected

		private bool FLoadAsSelected = true;
		[DefaultValue(true)]
		public bool LoadAsSelected
		{
			get { return FLoadAsSelected; }
			set
			{
				if (FLoadAsSelected != value)
				{
					try
					{
						// Set the property before updating so the update function appropriately
						FLoadAsSelected = value;
						UpdateFrameInterface(false);
					}
					catch
					{
						FLoadAsSelected = !value;
						throw;
					}
				}
			}
		}
	}
}
