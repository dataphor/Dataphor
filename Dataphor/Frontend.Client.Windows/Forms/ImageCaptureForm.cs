/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Captures an image from a windows device </summary>
	public partial class ImageCaptureForm : DialogForm, IImageSource 
	{
		public ImageCaptureForm()
		{
			InitializeComponent();
			SuspendLayout();
			try
			{                    
				SetAcceptReject(true, false);                
			}
			finally
			{
				ResumeLayout(false);
			}
			InitializeDrivers();
		}

		private bool _loading;
		public bool Loading
		{
			get { return _loading; }
		}

		public void LoadImage()
		{
			_loading = true;
			ShowDialog();
		}

		[DllImport("avicap32.dll")]
		protected static extern bool capGetDriverDescriptionA
		(
			short wDriverIndex,
			[MarshalAs(UnmanagedType.VBByRefStr)]ref String lpszName,
			int cbName,
			[MarshalAs(UnmanagedType.VBByRefStr)] ref String lpszVer,
			int cbVer
		);

		public const int CapabilityNameSize = 100;
		public const int CapabilityVersionSize = 100;
		private void InitializeDrivers()
		{
			string name = "".PadRight(100);
			string version = "".PadRight(100);
			short driverIndex = 0;
			while (capGetDriverDescriptionA(driverIndex++, ref name, CapabilityNameSize, ref version, CapabilityVersionSize))
				FDevices.Items.Add(name.Trim());
			if (FDevices.Items.Count == 1)
			{
				FDevices.SelectedIndex = 0;
				FDevices_SelectedIndexChanged(this, null);
			}
		}

		[DllImport("avicap32.dll")]
		protected static extern int capCreateCaptureWindowA
		(
			[MarshalAs(UnmanagedType.VBByRefStr)] ref string lpszWindowName,
			int dwStyle,
			int x,
			int y,
			int nWidth,
			int nHeight,
			int hWndParent,
			int nID
		);

		[DllImport("user32", EntryPoint = "SendMessageA")]
		protected static extern int SendMessage(int hwnd, int wMsg, int wParam, [MarshalAs(UnmanagedType.AsAny)] object lParam);

		[DllImport("user32")]
		protected static extern int SetWindowPos(int hwnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

		private const string CaptureWindowName = "Image Capture Picture Box";
		private const int CaptureWindowX = 0;
		private const int CaptureWindowY = 0;
		private const int CaptureWindowID = 0;
		private const int SendMessagenNullParameter = 0;        
		private const int PreviewRate = 0x42;
		private const int SetWindowFlag = 6;
		private const int SetWindowInsertAfter = 1;
		private const int SetWindowX = 0;
		private const int SetWindowY = 0;
		private const int WS_VISIBLE = 0x10000000;
		private const int WS_CHILD = 0x40000000;
		private const int WM_CAP_DRIVER_CONNECT = 0x40a;
		private const int WM_CAP_SET_SCALE = 0x435;
		private const int WM_CAP_SET_PREVIEW = 0x432;
		private const int WM_CAP_SET_PREVIEWRATE = 0x434;

		private int _videoHandle = Int32.MinValue;
		private int _currentDevice = Int32.MinValue;
		private string _windowName = CaptureWindowName;            
		private void FDevices_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (FDevices.SelectedIndex > -1)
			{
				Disconnect();
				_videoHandle =
					capCreateCaptureWindowA
					(
						ref _windowName,
						WS_VISIBLE | WS_CHILD,
						CaptureWindowX,
						CaptureWindowY,
						FPreviewImage.Width,
						FPreviewImage.Height,
						FPreviewImage.Handle.ToInt32(),
						CaptureWindowID
					 );

				if (SendMessage(_videoHandle, WM_CAP_DRIVER_CONNECT, FDevices.SelectedIndex, SendMessagenNullParameter) > 0)
				{
					_currentDevice = FDevices.SelectedIndex;
					SendMessage(_videoHandle, WM_CAP_SET_SCALE, -1, SendMessagenNullParameter);
					SendMessage(_videoHandle, WM_CAP_SET_PREVIEWRATE, PreviewRate, SendMessagenNullParameter);
					SendMessage(_videoHandle, WM_CAP_SET_PREVIEW, -1, SendMessagenNullParameter);
					SetWindowPos(_videoHandle, SetWindowInsertAfter, SetWindowX, SetWindowY, FPreviewImage.Width, FPreviewImage.Height, SetWindowFlag);
					FCaptureFrame.Enabled = true;
					FSettings.Enabled = true;
					SetHintText(Strings.CConnectedText);
				}
				else
				{
					Disconnect();
					SetHintText(Strings.CConnectionErrorText);
					FDevices.SelectedIndex = -1;
				}
			}
		}

		private MemoryStream _stream;
		public Stream Stream
		{
			get	 { return _stream; }
		}

		private const int WM_CAP_FILE_SAVEDIB = 0x419;        
		private void FCapture_Click(object sender, EventArgs e)
		{
			String tempFileName = Path.Combine(Application.LocalUserAppDataPath, Strings.CTempFileName);
			SendMessage(_videoHandle, WM_CAP_FILE_SAVEDIB, SendMessagenNullParameter, tempFileName);
			using (FileStream imageFile = new FileStream(tempFileName, FileMode.Open, FileAccess.Read))
			{
				if (_stream != null)
				{
					_stream.Close();
					_stream = null;
				}
				_stream = new MemoryStream();
				StreamUtility.CopyStream(imageFile, _stream);
				_stream.Position = 0;
				FCaptureImage.Image = System.Drawing.Image.FromStream(_stream);
			}                
		}

		const int WM_CAP_DLG_VIDEOSOURCE = 0x42a;
		private void FSettings_Click(object sender, EventArgs e)
		{
			SendMessage(_videoHandle, WM_CAP_DLG_VIDEOSOURCE, SendMessagenNullParameter, SendMessagenNullParameter);
		} 

		[DllImport("user32")]
		protected static extern bool DestroyWindow(int hwnd);    
		private const int WM_CAP_DRIVER_DISCONNECT = 0x40b;
		private void Disconnect()
		{
			if (_currentDevice != Int32.MinValue)
			{                
				try
				{
					SendMessage(_videoHandle, WM_CAP_DRIVER_DISCONNECT, _currentDevice, SendMessagenNullParameter);                     
				}
				finally
				{
					_currentDevice = Int32.MinValue;
				}
			}
			if (_videoHandle != Int32.MinValue)
			{
				try
				{
					DestroyWindow(_videoHandle);
				}
				finally
				{
					_videoHandle = Int32.MinValue;
				}
			}
			SetHintText(String.Empty);
			FSettings.Enabled = false;
			FCaptureFrame.Enabled = false;
		} 

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			args.Cancel = false;
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					try
					{
						PostChanges();
					}
					catch
					{
						args.Cancel = true;
						throw;
					}
				}
				else
					CancelChanges();
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);
			}
			finally
			{
				Disconnect();
				_loading = false;			
			}
		}

		private void CancelChanges()
		{
			FCaptureImage.Image = null;            
		}

		private void PostChanges()
		{             
			if (FCaptureImage.Image == null)
			{
				System.Media.SystemSounds.Beep.Play();
				throw new AbortException();
			}                              
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// HACK: The first control doesn't have focus when this form is shown (???) so we have to explicitly focus it.
			FDevices.Focus();
		}                             
	}
}