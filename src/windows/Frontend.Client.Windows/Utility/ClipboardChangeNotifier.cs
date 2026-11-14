/* From: http://www.vbaccelerator.com/home/net/code/libraries/Windows_Messages/Responding_to_Clipboard_Change_Notifications/article.asp */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

//using vbAccelerator.Components.Win32;

// Requires unmanaged code
[assembly:SecurityPermissionAttribute(SecurityAction.RequestMinimum, UnmanagedCode=true)]
// Requires all clipboard access
[assembly:UIPermissionAttribute(SecurityAction.RequestMinimum, Clipboard=UIPermissionClipboard.AllClipboard)]

namespace vbAccelerator.Components.Clipboard
{
	/// <summary>
	/// Provides a way to receive notifications of changes to the 
	/// content of the clipboard using the Windows API.  
	/// </summary>
	/// <remarks>
	/// To be a part of the change notification you need to register a 
	/// window in the Clipboard Viewer Chain.  This provides
	/// notification messages to the window whenever the 
	/// clipboard changes, and also messages associated with managing
	/// the chain.  This class manages the detail of keeping the
	/// chain intact and ensuring that the application is removed
	/// from the chain at the right point.
	/// 
	/// Use the <see cref="System.Windows.Forms.NativeWindow.AssignHandle"/> method 
	/// to connect this class up to a form to begin receiving notifications.
	/// Note that a Form can change its <see cref="System.Windows.Forms.Form.Handle"/>	
	/// under certain circumstances; for example, if you change the 
	/// <see cref="System.Windows.Forms.Form.ShowInTaskbar"/> property the Framework
	/// must re-create the form from scratch since Windows ignores changes to 
	/// that style unless they are in place when the Window is created.
	/// (As a consequence, you should try to set as many high-level Window 
	/// style details as possible prior to creating the Window, or at least,
	/// prior to making it visible).  The <see cref="OnHandleChanged"/> 
	/// method of this class is called when this happens.  You need to
	/// re-assign the handle again whenever this occurs.  Unfortunately
	/// <see cref="OnHandleChanged"/> is not a useful event in which to
	/// do anything since the window handle at that point contains neither
	/// a valid old window or a valid new one.  Therefore you need to
	/// make the call to re-assign at a point when you know the window
	/// is valid, for example, after styles have been changed, or 
	/// by overriding <see cref="System.Windows.Forms.Form.OnHandleCreated"/>.
	/// </remarks>		
	public class ClipboardChangeNotifier : NativeWindow, IDisposable
	{
		#region Unmanaged Code
		[DllImport("user32")]
		private extern static IntPtr SetClipboardViewer (
			IntPtr hWnd);
		[DllImport("user32")]
		private extern static int ChangeClipboardChain (
			IntPtr hWnd, 
			IntPtr hWndNext);
		[DllImport("user32", CharSet=CharSet.Auto)]
		private extern static int SendMessage (
			IntPtr hWnd, 
			int wMsg, 
			IntPtr wParam, 
			IntPtr lParam);

		private const int WM_DESTROY = 0x0002;
		private const int WM_DRAWCLIPBOARD = 0x308;
		private const int WM_CHANGECBCHAIN = 0x30D;
		#endregion

		#region Member Variables
		/// <summary>
		/// The next handle in the clipboard viewer chain when the 
		/// clipboard notification is installed, otherwise <see cref="IntPtr.Zero"/>
		/// </summary>
		protected IntPtr nextViewerHandle = IntPtr.Zero;
		/// <summary>
		/// Whether this class has been disposed or not.
		/// </summary>
		protected bool disposed = false;
		/// <summary>
		/// The Window clipboard change notification was installed for.
		/// </summary>
		protected IntPtr installedHandle = IntPtr.Zero;
		#endregion

		#region Events
		/// <summary>
		/// Notifies of a change to the clipboard's content.
		/// </summary>
		public event System.EventHandler ClipboardChanged;
		#endregion

		/// <summary>
		/// Provides default WndProc processing and responds to
		/// clipboard change notifications.
		/// </summary>
		/// <param name="e"></param>
		protected override void WndProc(ref Message e)
		{
			// if the message is a clipboard change notification
			switch (e.Msg)
			{
				case WM_CHANGECBCHAIN:
					// Store the changed handle of the next item 
					// in the clipboard chain:
					this.nextViewerHandle = e.LParam;

					if (!this.nextViewerHandle.Equals(IntPtr.Zero))
					{
						// pass the message on:
						SendMessage(this.nextViewerHandle, e.Msg, e.WParam, e.LParam);
					}

					// We have processed this message:
					e.Result = IntPtr.Zero;
					break;

				case WM_DRAWCLIPBOARD:
					// content of clipboard has changed:
					EventArgs clipChange = new EventArgs();
					OnClipboardChanged(clipChange);
					
					// pass the message on:
					if (!this.nextViewerHandle.Equals(IntPtr.Zero))
					{
						SendMessage(this.nextViewerHandle, e.Msg, e.WParam, e.LParam);
					}

					// We have processed this message:
					e.Result = IntPtr.Zero;
					break;

				case WM_DESTROY:
					// Very important: ensure we are uninstalled.
					Uninstall();
					// And call the superclass:
					base.WndProc(ref e);
					break;

				default:
					// call the superclass implementation:
					base.WndProc(ref e);
					break;
					
			}


		}

		/// <summary>
		/// Responds to Window Handle change events and uninstalls
		/// the clipboard change notification if it is installed.
		/// </summary>
		protected override void OnHandleChange()
		{
			// If we did get to this point, and we're still
			// installed then the chain will be broken.
			// The response to the WM_TERMINATE message should
			// prevent this.
			Uninstall();
			base.OnHandleChange();
		}

		/// <summary>
		/// Installs clipboard change notification.  The
		/// <see cref="AssignHandle"/> method of this class
		/// must have been called first.
		/// </summary>
		public void Install()
		{
			this.Uninstall();
			if (!this.Handle.Equals(IntPtr.Zero))
			{
				this.nextViewerHandle = SetClipboardViewer(this.Handle);
				this.installedHandle = this.Handle;
			}
		}

		/// <summary>
		/// Uninstalls clipboard change notification.
		/// </summary>
		public void Uninstall()
		{			
			if (!this.installedHandle.Equals(IntPtr.Zero))
			{
				ChangeClipboardChain(this.installedHandle, this.nextViewerHandle);
				int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
				//Debug.Assert(error==0, 
				//    String.Format("{0} Failed to uninstall from Clipboard Chain", this),
				//    Win32Error.ErrorMessage(error));
				this.nextViewerHandle = IntPtr.Zero;
				this.installedHandle = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Raises the <c>ClipboardChanged</c> event.
		/// </summary>
		/// <param name="e">Blank event arguments.</param>
		protected virtual void OnClipboardChanged(EventArgs e)
		{
			if (this.ClipboardChanged != null)
			{
				this.ClipboardChanged(this, e);
			}
		}

		/// <summary>
		/// Uninstalls clipboard event notifications if necessary
		/// during dispose of this object.
		/// </summary>
		public void Dispose()
		{
			if (!this.disposed)
			{
				Uninstall();
				this.disposed = true;
			}			
		}

		/// <summary>
		/// Constructs a new instance of this class.
		/// </summary>
		public ClipboardChangeNotifier() : base()
		{
			// intentionally blank
		}

	}
}
