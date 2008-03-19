/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;
using System.Collections;
using System.ComponentModel;

using Crownwood.Magic.Controls;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public class DesignerControl : ContainerControl
	{
		public DesignerControl(string AScript)
		{
			FServer = new ServerSchema(AScript);

			FDesignStatusPanel = new StatusBarPanel();
			FDesignStatusPanel.AutoSize = StatusBarPanelAutoSize.Spring;

			FDesignSelectionPanel = new StatusBarPanel();
			FDesignSelectionPanel.Alignment = HorizontalAlignment.Right;
			FDesignSelectionPanel.AutoSize = StatusBarPanelAutoSize.Contents;

			FDesignPositionPanel = new StatusBarPanel();
			FDesignPositionPanel.Alignment = HorizontalAlignment.Right;
			FDesignPositionPanel.AutoSize = StatusBarPanelAutoSize.Contents;

			FDesignStatusBar = new StatusBar();
			FDesignStatusBar.ShowPanels = true;
			FDesignStatusBar.SizingGrip = false;
			FDesignStatusBar.Panels.AddRange(new StatusBarPanel[] {FDesignStatusPanel, FDesignSelectionPanel, FDesignPositionPanel});

			SuspendLayout();

			Dock = DockStyle.Fill;
			Controls.Add(FDesignStatusBar);

			// Prepare designer
			Push(FDatabaseSurface = new DatabaseSurface(FServer.GetCatalog(), this));

			ResumeLayout(false);
		}

		protected override void Dispose( bool disposing )
		{
			try
			{
				try
				{
					if (FSurfaceStack != null)
					{
						while (FSurfaceStack.Count > 0)
							Pop();
						FDatabaseSurface = null;
						FSurfaceStack = null;
					}
				}
				finally
				{
					if (FServer != null)
					{
						FServer.Dispose();
						FServer = null;
					}
				}
			}
			finally
			{
				base.Dispose( disposing );
			}
		}

		private StatusBar FDesignStatusBar;
		private StatusBarPanel FDesignStatusPanel;
		private StatusBarPanel FDesignSelectionPanel;
		private StatusBarPanel FDesignPositionPanel;

		// Server

		private ServerSchema FServer;
		public ServerSchema Server
		{
			get { return FServer; }
		}

		// Script

		public string Script
		{
			get { return FServer.GetScript(); }
		}

		// DatabaseSurface

		private DatabaseSurface FDatabaseSurface;
		public DatabaseSurface DatabaseSurface
		{
			get { return FDatabaseSurface; }
		}

		// Modified

		public event EventHandler OnModified;

		public void Modified()
		{
			if (OnModified != null)
				OnModified(this, EventArgs.Empty);
		}

		// Surfaces

		private ArrayList FSurfaceStack = new ArrayList(4);
		
		/// <summary> Zooms in, replacing currently displayed surface with the specified one. </summary>
		public void Push(Surface ASurface)
		{
			if (FSurfaceStack.Count >= 1)
			{
				Surface LSurface = (Surface)FSurfaceStack[FSurfaceStack.Count - 1];
				LSurface.RememberActive();
				LSurface.Hide();
			}

			ASurface.Size = ClientSize;
			ASurface.Visible = true;
			Controls.Add(ASurface);
			ASurface.BringToFront();
			if (ASurface.ActiveControl == null)
				ASurface.SelectNextControl(null, true, true, true, true);
			ASurface.Focus();
			FSurfaceStack.Add(ASurface);
		}

		/// <summary> Zooms out, making to previous surface displayed while disposing the current. </summary>
		public void Pop()
		{
			if (FSurfaceStack.Count >= 1)
			{
				Surface LSurface;
				if (FSurfaceStack.Count > 1)
				{
					LSurface = (Surface)FSurfaceStack[FSurfaceStack.Count - 2];
					LSurface.RecallActive();
					LSurface.Show();
					LSurface.Focus();
				}
				LSurface = (Surface)FSurfaceStack[FSurfaceStack.Count - 1];
				FSurfaceStack.Remove(LSurface);
				LSurface.Dispose();
			}
		}

		/// <summary> The currently displayed surface. </summary>
		public Surface ActiveSurface
		{
			get { return (Surface)FSurfaceStack[FSurfaceStack.Count - 1]; }
		}

		/// <remarks> When passed null, will zoom in on the selected item. </remarks>
		public void ZoomIn(Control AControl)
		{
			ActiveSurface.Details(AControl);
		}

		public void ZoomOut()
		{
			if (FSurfaceStack.Count > 1)
				Pop();
		}

		// Painting

		protected override void OnPaintBackground(PaintEventArgs AArgs)
		{
			// Optimization: because the background of page will always be 
			// covered by a surface, don't paint it.
		}

		// Mouse handling

		// HACK: This method of handling the mouse wheel 
		// is a result of problems that arose when the control over which the 
		// mouse sits become disposed while still in it's message handling.
		// This method ensures that the mouse wheel message is coming directly 
		// from Windows, and not a nested default handling procedure of a child 
		// control

		private bool FPostedWheelMessage;

		protected override void OnMouseWheel(MouseEventArgs AArgs)
		{
			base.OnMouseWheel(AArgs);
			if (FPostedWheelMessage)
			{
				if (ActiveSurface != null)
				{
					if (AArgs.Delta >= 120)
						ZoomIn(null);
					else if (AArgs.Delta <= -120)
						ZoomOut();
				}
				FPostedWheelMessage = false;
			}
			else
			{
				FPostedWheelMessage = true;
				UnsafeNativeMethods.PostMessage
				(
					Handle, 
					NativeMethods.WM_MOUSEWHEEL, 
					new IntPtr(AArgs.Delta << 16), 
					new IntPtr(AArgs.X | AArgs.Y << 16)
				);
			}
		}

		// Keyboard handling

		protected override bool ProcessDialogChar(char AChar)
		{
			switch (AChar)
			{
				case '-':
					ZoomOut();
					return true;
				case '=' :
				case '+' :
					ZoomIn(null);
					return true;
				default :
					return base.ProcessDialogChar(AChar);
			}
		}
	}
}
