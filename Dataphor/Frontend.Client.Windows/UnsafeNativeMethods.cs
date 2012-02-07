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
		public const string DLLUSER32 = "user32.dll";

		public const string EPPostMessage = "PostMessage";
		public const string EPSendMessage = "SendMessage";
		public const string EPEnableWindow = "EnableWindow";
		public const string EPGetSystemMenu = "GetSystemMenu";
		public const string EPEnableMenuItem = "EnableMenuItem";
		public const string EPInsertMenuItem = "InsertMenuItem";
		public const string EPDrawMenuBar = "DrawMenuBar";
		public const string EPGetFocus = "GetFocus";

		[DllImport(DLLUSER32, EntryPoint = EPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPEnableWindow, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int EnableWindow(IntPtr hWnd, bool bEnable);

		[DllImport(DLLUSER32, EntryPoint = EPDrawMenuBar, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool DrawMenuBar(IntPtr hwnd);

		[DllImport(DLLUSER32, EntryPoint = EPInsertMenuItem, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool InsertMenuItem(IntPtr hMenu, int uItem, bool fByPosition, ref NativeMethods.MenuItemInfo lpmii);

		[DllImport(DLLUSER32, EntryPoint = EPEnableMenuItem, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool EnableMenuItem(IntPtr hMenu, int uID, int uEnabled);

		[DllImport(DLLUSER32, EntryPoint = EPGetSystemMenu, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport(DLLUSER32, EntryPoint = EPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPGetFocus, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr GetFocus();
	}
}
