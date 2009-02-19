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

		private bool FLoading;
		public bool Loading
		{
			get { return FLoading; }
		}

		public void LoadImage()
		{
			FLoading = true;
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

		public const int CCapabilityNameSize = 100;
		public const int CCapabilityVersionSize = 100;
		private void InitializeDrivers()
		{
			string LName = "".PadRight(100);
			string LVersion = "".PadRight(100);
			short LDriverIndex = 0;
			while (capGetDriverDescriptionA(LDriverIndex++, ref LName, CCapabilityNameSize, ref LVersion, CCapabilityVersionSize))
				FDevices.Items.Add(LName.Trim());
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

		private const string CCaptureWindowName = "Image Capture Picture Box";
		private const int CCaptureWindowX = 0;
		private const int CCaptureWindowY = 0;
		private const int CCaptureWindowID = 0;
		private const int CSendMessagenNullParameter = 0;        
		private const int CPreviewRate = 0x42;
		private const int CSetWindowFlag = 6;
		private const int CSetWindowInsertAfter = 1;
		private const int CSetWindowX = 0;
		private const int CSetWindowY = 0;
		private const int WS_VISIBLE = 0x10000000;
		private const int WS_CHILD = 0x40000000;
		private const int WM_CAP_DRIVER_CONNECT = 0x40a;
		private const int WM_CAP_SET_SCALE = 0x435;
		private const int WM_CAP_SET_PREVIEW = 0x432;
		private const int WM_CAP_SET_PREVIEWRATE = 0x434;

		private int FVideoHandle = Int32.MinValue;
		private int FCurrentDevice = Int32.MinValue;
		private string FWindowName = CCaptureWindowName;            
		private void FDevices_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (FDevices.SelectedIndex > -1)
			{
				Disconnect();
				FVideoHandle =
					capCreateCaptureWindowA
					(
						ref FWindowName,
						WS_VISIBLE | WS_CHILD,
						CCaptureWindowX,
						CCaptureWindowY,
						FPreviewImage.Width,
						FPreviewImage.Height,
						FPreviewImage.Handle.ToInt32(),
						CCaptureWindowID
					 );

				if (SendMessage(FVideoHandle, WM_CAP_DRIVER_CONNECT, FDevices.SelectedIndex, CSendMessagenNullParameter) > 0)
				{
					FCurrentDevice = FDevices.SelectedIndex;
					SendMessage(FVideoHandle, WM_CAP_SET_SCALE, -1, CSendMessagenNullParameter);
					SendMessage(FVideoHandle, WM_CAP_SET_PREVIEWRATE, CPreviewRate, CSendMessagenNullParameter);
					SendMessage(FVideoHandle, WM_CAP_SET_PREVIEW, -1, CSendMessagenNullParameter);
					SetWindowPos(FVideoHandle, CSetWindowInsertAfter, CSetWindowX, CSetWindowY, FPreviewImage.Width, FPreviewImage.Height, CSetWindowFlag);
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

		private MemoryStream FStream;
		public Stream Stream
		{
			get	 { return FStream; }
		}

		private const int WM_CAP_FILE_SAVEDIB = 0x419;        
		private void FCapture_Click(object sender, EventArgs e)
		{
			String LTempFileName = Application.LocalUserAppDataPath + @"\" + Strings.CTempFileName;
			SendMessage(FVideoHandle, WM_CAP_FILE_SAVEDIB, CSendMessagenNullParameter, LTempFileName);
			using (FileStream LImageFile = new FileStream(LTempFileName, FileMode.Open, FileAccess.Read))
			{
				if (FStream != null)
				{
					FStream.Close();
					FStream = null;
				}
				FStream = new MemoryStream();
				StreamUtility.CopyStream(LImageFile, FStream);
				FStream.Position = 0;
				FCaptureImage.Image = System.Drawing.Image.FromStream(FStream);
			}                
		}

		const int WM_CAP_DLG_VIDEOSOURCE = 0x42a;
		private void FSettings_Click(object sender, EventArgs e)
		{
			SendMessage(FVideoHandle, WM_CAP_DLG_VIDEOSOURCE, CSendMessagenNullParameter, CSendMessagenNullParameter);
		} 

		[DllImport("user32")]
		protected static extern bool DestroyWindow(int hwnd);    
		private const int WM_CAP_DRIVER_DISCONNECT = 0x40b;
		private void Disconnect()
		{
			if (FCurrentDevice != Int32.MinValue)
			{                
				try
				{
					SendMessage(FVideoHandle, WM_CAP_DRIVER_DISCONNECT, FCurrentDevice, CSendMessagenNullParameter);                     
				}
				finally
				{
					FCurrentDevice = Int32.MinValue;
				}
			}
			if (FVideoHandle != Int32.MinValue)
			{
				try
				{
					DestroyWindow(FVideoHandle);
				}
				finally
				{
					FVideoHandle = Int32.MinValue;
				}
			}
			SetHintText(String.Empty);
			FSettings.Enabled = false;
			FCaptureFrame.Enabled = false;
		} 

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			AArgs.Cancel = false;
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
						AArgs.Cancel = true;
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
				FLoading = false;			
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