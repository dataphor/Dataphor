/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.Dataphoria
{
	[System.Security.SuppressUnmanagedCodeSecurity()]
	internal class UnsafeNativeMethods
	{
		public const string DLLUSER32 = "user32.dll";

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int GetScrollPos(IntPtr hWnd, int nBar);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref RECT prcScroll, ref RECT prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, UInt32 flags);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr GetFocus();
	}

	internal class UnsafeUtilities
	{
		public static RECT RECTFromRectangle(Rectangle rectangle)
		{
			RECT result = new RECT();
			result.left = rectangle.Left;
			result.top = rectangle.Top;
			result.right = rectangle.Right;
			result.bottom = rectangle.Bottom;
			return result;
		}

		public static RECT RECTFromLTRB(int left, int top, int right, int bottom)
		{
			RECT result = new RECT();
			result.left = left;
			result.top = top;
			result.right = right;
			result.bottom = bottom;
			return result;
		}
	}
}
