/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <remarks> Some parent of the exposed node must implement IWindowsExposedHost. </remarks>
	[DesignerImage("Image('Frontend', 'Nodes.Exposed')")]
	[DesignerCategory("Non Visual")]
	public class Exposed : ActionNode, IExposed
	{
		// TODO: Tooltips on exposed

		private void ControlClicked(object ASender, EventArgs AArgs)
		{
			try
			{
				if (Action != null)
					Action.Execute(this, new EventParams());
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);
			}
		}
		
		// Control

		private IWindowsBarButton FControl;
		protected IWindowsBarButton Control
		{
			get	{ return FControl; }
		}

		// ActionNode

		protected override void InternalUpdateVisible()
		{
			FControl.Visible = GetVisible();
		}

		protected override void InternalUpdateEnabled()
		{
			FControl.Enabled = GetEnabled();
		}

		protected override void InternalSetImage(System.Drawing.Image AImage)
		{
			FControl.Image = AImage;
		}

		private string FAllocatedText;

		protected void DeallocateAccelerator()
		{
			if (FAllocatedText != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(FAllocatedText);
				FAllocatedText = null;
			}
		}

		protected override void InternalUpdateText()
		{
			DeallocateAccelerator();
			FAllocatedText = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetText(), true);
			FControl.Text = FAllocatedText;
		}

		// Node

		protected override void Activate()
		{
			IWindowsExposedHost LHost = (IWindowsExposedHost)FindParent(typeof(IWindowsExposedHost));
			if (LHost != null)
			{
				FControl = LHost.ExposedContainer.CreateMenuItem(new EventHandler(ControlClicked));
				LHost.ExposedContainer.AddBarItem(FControl, null);
			}
			try
			{
				base.Activate();
			}
			catch
			{
				FControl.Dispose();
				FControl = null;
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
					if (FControl != null)
					{
						FControl.Dispose();
						FControl = null;
					}
				}
			}
		}
	}
}
