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
		public const string CDLLUSER32 = "user32.dll";
		public const string CEPSetScrollInfo = "SetScrollInfo";
		public const string CEPSendMessage = "SendMessage";
		public const string CEPPostMessage = "PostMessage";
		public const string CEPScrollWindowEx = "ScrollWindowEx";
		public const string CEPMessageBeep = "MessageBeep";
		public const string CEPUpdateWindow = "UpdateWindow";
		public const string CEPGetUpdateRect = "GetUpdateRect";

		[DllImport(CDLLUSER32, EntryPoint = CEPSetScrollInfo, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SetScrollInfo(IntPtr AHandle, int fnBar, NativeMethods.SCROLLINFO AScrollInfo, bool ARedraw);

		[DllImport(CDLLUSER32, EntryPoint = CEPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPPostMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPSendMessage, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, EntryPoint = CEPScrollWindowEx, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref NativeMethods.RECT lprcScroll, ref NativeMethods.RECT lprcClip, IntPtr hrgnUpdate, ref NativeMethods.RECT lprcUpdate, uint flags);

		[DllImport(CDLLUSER32, EntryPoint = CEPScrollWindowEx, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref NativeMethods.RECT lprcScroll, ref NativeMethods.RECT lprcClip, IntPtr hrgnUpdate, IntPtr lprcUpdate, uint flags);

		[DllImport(CDLLUSER32, EntryPoint = CEPMessageBeep, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool MessageBeep(int uType);

		[DllImport(CDLLUSER32, EntryPoint = CEPUpdateWindow, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int UpdateWindow(IntPtr hWnd);

		[DllImport(CDLLUSER32, EntryPoint = CEPGetUpdateRect, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int GetUpdateRect(IntPtr hWnd, ref NativeMethods.RECT lprcUpdate, bool bErase);
	}
}
