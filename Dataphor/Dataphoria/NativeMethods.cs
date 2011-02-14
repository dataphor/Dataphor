/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.Dataphoria
{
	internal class NativeMethods
	{
		public const string DLLUSER32 = "user32.dll";

		public static int WM_MOUSEWHEEL = 0x020A;
		public static int WM_CLOSE = 0x0010;
		public static int WM_USER = 0x0400;
		public static int WM_NOTIFY = 0x004E;
		public static int WM_SETREDRAW = 0x000B;
		public static int WM_LBUTTONDOWN = 0x0201;

		public static int OCM__BASE	= (NativeMethods.WM_USER + 0x1C00);
		public static int OCM_NOTIFY = (OCM__BASE + NativeMethods.WM_NOTIFY);

		[DllImport(DLLUSER32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern int RegisterWindowMessage(string specifier);
	}
}
