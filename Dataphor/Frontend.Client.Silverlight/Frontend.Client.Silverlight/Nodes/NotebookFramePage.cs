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

		private bool _isSelected;
		
		public virtual void Selected()
		{
			_isSelected = true;
			UpdateFrameInterface(false);
		}

		public virtual void Unselected()
		{
			_isSelected = false;
			UpdateFrameInterface(false);
		}

		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(NotebookItem.HeaderProperty, new Func<object>(UIGetHeader));
		}
		
		protected string _title = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return _title; }
			set
			{
				_title = value;
				UpdateBinding(NotebookItem.HeaderProperty);
			}
		}
		
		protected virtual string GetTitle()
		{
			return String.IsNullOrEmpty(_title) ? Strings.CDefaultNotebookPageTitle : _title;
		}
		
		private object UIGetHeader()
		{
			return GetTitle();
		}

		public override bool IsValidOwner(Type ownerType)
		{
			return typeof(INotebook).IsAssignableFrom(ownerType);
		}

		protected override void UpdateFrameInterface(bool force)
		{
			// If the frame should be loaded and it is not, or vise versa... then fix it
			if 
			(
				Active && 
				(
					(ShouldLoad() == (FrameInterfaceNode == null)) || 
					force
				)
			)
				ResetFrameInterfaceNode(Active);
		}

		protected override bool ShouldLoad()
		{
			return base.ShouldLoad() && (_isSelected || !_loadAsSelected);
		}

		// LoadAsSelected

		private bool _loadAsSelected = true;
		[DefaultValue(true)]
		public bool LoadAsSelected
		{
			get { return _loadAsSelected; }
			set
			{
				if (_loadAsSelected != value)
				{
					try
					{
						// Set the property before updating so the update function appropriately
						_loadAsSelected = value;
						UpdateFrameInterface(false);
					}
					catch
					{
						_loadAsSelected = !value;
						throw;
					}
				}
			}
		}
	}
}
