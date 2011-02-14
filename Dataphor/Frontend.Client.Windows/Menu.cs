/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Creates and maintains an interface menu. </summary>
    [Description("Option on Menubar.  Can be embedded within itself to create submenus.")]
	[DesignerImage("Image('Frontend', 'Nodes.Menu')")]
	[DesignerCategory("Non Visual")]
    public class Menu : ActionNode, IMenu, IWindowsMenuHost, IAccelerates
    {
		// TODO: Tooltips on menu items

		// IAccelerates

		private AcceleratorManager _accelerators = new AcceleratorManager();
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public AcceleratorManager Accelerators
		{
			get { return _accelerators; }
		}

		// MenuItem

		protected IWindowsBarItem _menuItem;
		[Browsable(false)]
		public IWindowsBarItem MenuItem
		{
			get { return _menuItem; }
		}

		private void MenuItemClicked(object sender, EventArgs args)
		{
			try
			{
				if (Action != null)
					Action.Execute(this, new EventParams());
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);	// Don't rethrow
			}
		}

		// IWindowsMenuHost

		[Browsable(false)]
		public IWindowsBarContainer MenuContainer
		{
			get 
			{
				// A child is requesting our container, we better make sure we are one
				EnsureMenuItem(true);
				return (IWindowsBarContainer)_menuItem;
			}
		}

		// ActionNode

		protected override void InternalUpdateVisible()
		{
			_menuItem.Visible = GetVisible();
		}
		
		public override bool GetEnabled()
		{
			if (Children.Count > 0)
				return Enabled;
			else
				return base.GetEnabled();
		}

		protected override void ChildrenChanged()
		{
			if (Active)
			{
				InternalUpdateEnabled();
				EnsureMenuItem(Children.Count > 0);
			}
			base.ChildrenChanged();
		}

		protected override void InternalUpdateEnabled()
		{
			if ((_menuItem != null) && (_menuItem is IWindowsBarButton))
				((IWindowsBarButton)_menuItem).Enabled = GetEnabled();
		}
		
		private string _allocatedText;

		protected void DeallocateAccelerator()
		{
			if (_allocatedText != null)
			{
				if (Parent != null)
					((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(_allocatedText);
				_allocatedText = null;
			}
		}

		private void UpdateButtonText()
		{
			DeallocateAccelerator();
			if (_menuItem is IWindowsBarButton)
			{
				_allocatedText = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetText(), true);;
				((IWindowsBarButton)_menuItem).Text = _allocatedText;
			}
		}

		protected override void InternalUpdateText()
		{
			EnsureMenuItem(Children.Count > 0);
			if (_menuItem is IWindowsBarButton)
				UpdateButtonText();
			else
				DeallocateAccelerator();
		}

		protected override void InternalSetImage(System.Drawing.Image image)
		{
			IWindowsBarButton button = _menuItem as IWindowsBarButton;
			if (button != null)
				button.Image = image;
		}

		// Node

		private void RebuildMenuItem(bool isContainer)
		{
			if (!Transitional)
			{
				RemoveMenuItem();
				BuildMenuItem(isContainer);
				InternalUpdateEnabled();
				InternalUpdateImage();
				UpdateButtonText();
				InternalUpdateVisible();
			}
		}

		private void EnsureMenuItem(bool isContainer)
		{
			if 
			(
				(isContainer != (_menuItem is IWindowsBarContainer))
					|| (!isContainer && ((GetText() == "-") != (_menuItem is IWindowsBarSeparator)))
			)
				RebuildMenuItem(isContainer);
		}

		private void RemoveMenuItem()
		{
			if (_menuItem != null)
			{
				IWindowsMenuHost windowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
				if (windowsMenuHost != null)
					windowsMenuHost.MenuContainer.RemoveBarItem(_menuItem);
				_menuItem.Dispose();
			}
		}

		private void BuildMenuItem(bool isContainer)
		{
			IWindowsMenuHost windowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
			if (windowsMenuHost != null)
			{
				if (!isContainer)
					if (GetText() == "-")
						_menuItem = windowsMenuHost.MenuContainer.CreateSeparator();
					else
						_menuItem = windowsMenuHost.MenuContainer.CreateMenuItem(new EventHandler(MenuItemClicked));
				else
					_menuItem = windowsMenuHost.MenuContainer.CreateContainer();
				try
				{
					windowsMenuHost.MenuContainer.AddBarItem(_menuItem, new GetPriorityHandler(GetMenuItemPriority));
				}
				catch
				{
					_menuItem.Dispose();
					_menuItem = null;
					throw;
				}
			}
		}

		private int GetMenuItemPriority(IWindowsBarItem item)
		{
			return Parent.Children.IndexOf(this);
		}

		protected override void Activate()
		{
			try
			{
				BuildMenuItem(Children.Count > 0);
				base.Activate();
			}
			catch
			{
				RemoveMenuItem();
				throw;
			}
		}
		
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				try
				{
					DeallocateAccelerator();
				}
				finally
				{
					RemoveMenuItem();
				}
			}
		}
		
		public override bool IsValidChild(Type childType)
		{
			if (typeof(IMenu).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}
    }   
}