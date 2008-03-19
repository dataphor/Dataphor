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

		private AcceleratorManager FAccelerators = new AcceleratorManager();
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public AcceleratorManager Accelerators
		{
			get { return FAccelerators; }
		}

		// MenuItem

		protected IWindowsBarItem FMenuItem;
		[Browsable(false)]
		public IWindowsBarItem MenuItem
		{
			get { return FMenuItem; }
		}

		private void MenuItemClicked(object ASender, EventArgs AArgs)
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
				return (IWindowsBarContainer)FMenuItem;
			}
		}

		// ActionNode

		protected override void InternalUpdateVisible()
		{
			FMenuItem.Visible = GetVisible();
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
			if ((FMenuItem != null) && (FMenuItem is IWindowsBarButton))
				((IWindowsBarButton)FMenuItem).Enabled = GetEnabled();
		}
		
		private string FAllocatedText;

		protected void DeallocateAccelerator()
		{
			if (FAllocatedText != null)
			{
				if (Parent != null)
					((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(FAllocatedText);
				FAllocatedText = null;
			}
		}

		private void UpdateButtonText()
		{
			DeallocateAccelerator();
			if (FMenuItem is IWindowsBarButton)
			{
				FAllocatedText = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetText(), true);;
				((IWindowsBarButton)FMenuItem).Text = FAllocatedText;
			}
		}

		protected override void InternalUpdateText()
		{
			EnsureMenuItem(Children.Count > 0);
			if (FMenuItem is IWindowsBarButton)
				UpdateButtonText();
			else
				DeallocateAccelerator();
		}

		protected override void InternalSetImage(System.Drawing.Image AImage)
		{
			IWindowsBarButton LButton = FMenuItem as IWindowsBarButton;
			if (LButton != null)
				LButton.Image = AImage;
		}

		// Node

		private void RebuildMenuItem(bool AIsContainer)
		{
			if (!Transitional)
			{
				RemoveMenuItem();
				BuildMenuItem(AIsContainer);
				InternalUpdateEnabled();
				InternalUpdateImage();
				UpdateButtonText();
				InternalUpdateVisible();
			}
		}

		private void EnsureMenuItem(bool AIsContainer)
		{
			if 
			(
				(AIsContainer != (FMenuItem is IWindowsBarContainer))
					|| (!AIsContainer && ((GetText() == "-") != (FMenuItem is IWindowsBarSeparator)))
			)
				RebuildMenuItem(AIsContainer);
		}

		private void RemoveMenuItem()
		{
			if (FMenuItem != null)
			{
				IWindowsMenuHost LWindowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
				if (LWindowsMenuHost != null)
					LWindowsMenuHost.MenuContainer.RemoveBarItem(FMenuItem);
				FMenuItem.Dispose();
			}
		}

		private void BuildMenuItem(bool AIsContainer)
		{
			IWindowsMenuHost LWindowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
			if (LWindowsMenuHost != null)
			{
				if (!AIsContainer)
					if (GetText() == "-")
						FMenuItem = LWindowsMenuHost.MenuContainer.CreateSeparator();
					else
						FMenuItem = LWindowsMenuHost.MenuContainer.CreateMenuItem(new EventHandler(MenuItemClicked));
				else
					FMenuItem = LWindowsMenuHost.MenuContainer.CreateContainer();
				try
				{
					LWindowsMenuHost.MenuContainer.AddBarItem(FMenuItem, new GetPriorityHandler(GetMenuItemPriority));
				}
				catch
				{
					FMenuItem.Dispose();
					FMenuItem = null;
					throw;
				}
			}
		}

		private int GetMenuItemPriority(IWindowsBarItem AItem)
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
		
		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IMenu).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}
    }   
}