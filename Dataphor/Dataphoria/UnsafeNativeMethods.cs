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
		public const string CDLLUSER32 = "user32.dll";

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int GetScrollPos(IntPtr hWnd, int nBar);

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref RECT prcScroll, ref RECT prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, UInt32 flags);

		[DllImport(CDLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr GetFocus();
	}

	internal class UnsafeUtilities
	{
		public static RECT RECTFromRectangle(Rectangle ARectangle)
		{
			RECT LResult = new RECT();
			LResult.left = ARectangle.Left;
			LResult.top = ARectangle.Top;
			LResult.right = ARectangle.Right;
			LResult.bottom = ARectangle.Bottom;
			return LResult;
		}

		public static RECT RECTFromLTRB(int ALeft, int ATop, int ARight, int ABottom)
		{
			RECT LResult = new RECT();
			LResult.left = ALeft;
			LResult.top = ATop;
			LResult.right = ARight;
			LResult.bottom = ABottom;
			return LResult;
		}
	}
}
