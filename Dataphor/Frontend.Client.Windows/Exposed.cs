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

		private void ControlClicked(object sender, EventArgs args)
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

		private IWindowsBarButton _control;
		protected IWindowsBarButton Control
		{
			get	{ return _control; }
		}

		// ActionNode

		protected override void InternalUpdateVisible()
		{
			_control.Visible = GetVisible();
		}

		protected override void InternalUpdateEnabled()
		{
			_control.Enabled = GetEnabled();
		}

		protected override void InternalSetImage(System.Drawing.Image image)
		{
			_control.Image = image;
		}

		private string _allocatedText;

		protected void DeallocateAccelerator()
		{
			if (_allocatedText != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(_allocatedText);
				_allocatedText = null;
			}
		}

		protected override void InternalUpdateText()
		{
			DeallocateAccelerator();
			_allocatedText = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetText(), true);
			_control.Text = _allocatedText;
		}

		// Node

		protected override void Activate()
		{
			IWindowsExposedHost host = (IWindowsExposedHost)FindParent(typeof(IWindowsExposedHost));
			if (host != null)
			{
				_control = host.ExposedContainer.CreateMenuItem(new EventHandler(ControlClicked));
				host.ExposedContainer.AddBarItem(_control, null);
			}
			try
			{
				base.Activate();
			}
			catch
			{
				_control.Dispose();
				_control = null;
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
					if (_control != null)
					{
						_control.Dispose();
						_control = null;
					}
				}
			}
		}
	}
}
