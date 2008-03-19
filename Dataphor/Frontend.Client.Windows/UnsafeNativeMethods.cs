/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[System.Security.SuppressUnmanagedCodeSecurity()]
	internal class UnsafeNativeMethods
	{
		public const string CDLLUSER32 = "user32.dll";

		public const string CEPPostMessage = "PostMessage";
		public const string CEPSendMessage = "SendMessage";
		public const string CEPEnableWindow = "EnableWindow";
		public const string CEPGetSystemMenu = "GetSystemMenu";
		public const string CEPEnableMenuItem = "EnableMenuItem";
		public const string CEPInsertMenuItem = "InsertMenuItem";
		public const string CEPDrawMenuBar = "DrawMenuBar";
		public const string CEPGetFocus = "GetFocus";

		[DllImport(CDLLUSER32, EntryPoint = CEPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPEnableWindow, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int EnableWindow(IntPtr hWnd, bool bEnable);

		[DllImport(CDLLUSER32, EntryPoint = CEPDrawMenuBar, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool DrawMenuBar(IntPtr hwnd);

		[DllImport(CDLLUSER32, EntryPoint = CEPInsertMenuItem, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool InsertMenuItem(IntPtr hMenu, int uItem, bool fByPosition, ref NativeMethods.MenuItemInfo lpmii);

		[DllImport(CDLLUSER32, EntryPoint = CEPEnableMenuItem, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool EnableMenuItem(IntPtr hMenu, int uID, int uEnabled);

		[DllImport(CDLLUSER32, EntryPoint = CEPGetSystemMenu, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport(CDLLUSER32, EntryPoint = CEPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPGetFocus, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr GetFocus();
	}
}
