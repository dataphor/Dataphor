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
			return new NotebookItem();
		}
		
		public NotebookItem NotebookItem
		{
			get { return (NotebookItem)FrameworkElement; }
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
			AddBinding(NotebookItem.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		protected string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return FTitle; }
			set
			{
				FTitle = value;
				UpdateBinding(NotebookItem.HeaderProperty);
			}
		}
		
		protected virtual string GetTitle()
		{
			return String.IsNullOrEmpty(FTitle) ? Strings.CDefaultNotebookPageTitle : FTitle;
		}
		
		private object UIGetHeader()
		{
			return GetTitle();
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
				ResetFrameInterfaceNode(Active);
		}

		protected override bool ShouldLoad()
		{
			return base.ShouldLoad() && (FIsSelected || !FLoadAsSelected);
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
