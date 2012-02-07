/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;

	[System.Security.SuppressUnmanagedCodeSecurity()]
	internal class UnsafeNativeMethods
	{
		public const string DLLUSER32 = "user32.dll";
		public const string EPSetScrollInfo = "SetScrollInfo";
		public const string EPSendMessage = "SendMessage";
		public const string EPPostMessage = "PostMessage";
		public const string EPScrollWindowEx = "ScrollWindowEx";
		public const string EPMessageBeep = "MessageBeep";
		public const string EPUpdateWindow = "UpdateWindow";
		public const string EPGetUpdateRect = "GetUpdateRect";

		[DllImport(DLLUSER32, EntryPoint = EPSetScrollInfo, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SetScrollInfo(IntPtr handle, int fnBar, NativeMethods.SCROLLINFO scrollInfo, bool redraw);

		[DllImport(DLLUSER32, EntryPoint = EPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(DLLUSER32, EntryPoint = EPScrollWindowEx, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref NativeMethods.RECT lprcScroll, ref NativeMethods.RECT lprcClip, IntPtr hrgnUpdate, ref NativeMethods.RECT lprcUpdate, uint flags);

		[DllImport(DLLUSER32, EntryPoint = EPScrollWindowEx, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref NativeMethods.RECT lprcScroll, ref NativeMethods.RECT lprcClip, IntPtr hrgnUpdate, IntPtr lprcUpdate, uint flags);

		[DllImport(DLLUSER32, EntryPoint = EPMessageBeep, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool MessageBeep(int uType);

		[DllImport(DLLUSER32, EntryPoint = EPUpdateWindow, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int UpdateWindow(IntPtr hWnd);

		[DllImport(DLLUSER32, EntryPoint = EPGetUpdateRect, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int GetUpdateRect(IntPtr hWnd, ref NativeMethods.RECT lprcUpdate, bool bErase);
	}
}
