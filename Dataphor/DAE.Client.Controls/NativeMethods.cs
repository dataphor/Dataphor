/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Drawing;
	using System.Runtime.InteropServices;

	internal class NativeMethods
	{
		internal const int
			WM_CLEAR = 0x0303,
			WM_CONTEXTMENU = 0x007B,
			WM_COPY = 0x0301,
			WM_CUT = 0x0300,
			WM_HSCROLL = 0x0114,
			WM_KEYDOWN = 0x0100,
			WM_LBUTTONUP = 0x0202,
			WM_MOUSEMOVE = 0x0200,
			WM_LBUTTONDBLCLK = 0x0203,
			WM_PASTE = 0x0302,
			WM_SETREDRAW = 0x000B,
			WM_SIZE = 0x0005,
			WM_UNDO = 0x0304,
			WM_VSCROLL = 0x0115,
			WM_SETFOCUS = 0x0007,
			WM_KILLFOCUS = 0x0008,
			WM_PAINT = 0x000F;

		internal const int
			WS_EX_CLIENTEDGE = 0x200,
			WS_BORDER = 0x800000,
			WS_HSCROLL = 0x00100000,
			WS_VSCROLL = 0x00200000,
			WS_POPUP = unchecked((int)0x80000000);

		internal const int
			SB_HORZ	= 0,
			SB_VERT	= 1,
			SB_CTL = 2,
			SB_BOTH	= 3;

		internal const int
			SIF_RANGE = 0x0001,
			SIF_PAGE = 0x0002,
			SIF_POS	= 0x0004,
			SIF_DISABLENOSCROLL	= 0x0008,
			SIF_TRACKPOS = 0x0010;

		internal const int
			SB_LINEUP = 0,
			SB_LINELEFT	= 0,
			SB_LINEDOWN	= 1,
			SB_LINERIGHT = 1,
			SB_PAGEUP = 2,
			SB_PAGELEFT	= 2,
			SB_PAGEDOWN	= 3,
			SB_PAGERIGHT = 3,
			SB_THUMBPOSITION = 4,
			SB_THUMBTRACK = 5,
			SB_TOP = 6,
			SB_LEFT	= 6,
			SB_BOTTOM = 7,
			SB_RIGHT = 7,
			SB_ENDSCROLL = 8;

		internal const int
			MK_LBUTTON = 0x0001,
			MK_RBUTTON = 0x0002,
			MK_SHIFT = 0x0004,
			MK_CONTROL = 0x0008,
			MK_MBUTTON = 0x0010;

		internal const int
			S_DBLCLKS = 0x0008;

		internal const int
			SW_SCROLLCHILDREN = 0x0001,
            SW_INVALIDATE = 0x0002,
			SW_ERASE = 0x0004,
            SW_SMOOTHSCROLL = 0x0010;

		[StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		internal struct RECT 
		{
			internal int left;
			internal int top;
			internal int right;
			internal int bottom;

			internal RECT(int left, int top, int right, int bottom) 
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}

			internal static RECT EmptyRECT()
			{
				return new RECT(0, 0, 0, 0);
			}

			internal static RECT FromXYWH(int x, int y, int width, int height) 
			{
				return new RECT(x, y, x + width, y + height);
			}

			internal static RECT FromRectangle(Rectangle ARectangle) 
			{
				return new RECT(ARectangle.X, ARectangle.Y, ARectangle.Right, ARectangle.Bottom);
			}

			internal static Rectangle ToRectangle(RECT ARect)
			{
				return new Rectangle(ARect.left, ARect.top, ARect.right - ARect.left, ARect.bottom - ARect.top);
			}
		}

		[StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] 
		internal class SCROLLINFO
		{
			public int cbSize;
			public int fMask;
			public int nMin;
			public int nMax;
			public int nPage;
			public int nPos;
			public int nTrackPos;

			public SCROLLINFO()
			{
				cbSize = 28;
			}

			public SCROLLINFO(int mask, int min, int max, int page, int pos)
			{
				cbSize = 28;
				fMask = mask;
				nMin = min;
				nMax = max;
				nPage = page;
				nPos = pos;
				nTrackPos = 0;
			}
		}

		[System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
		internal class Utilities
		{
			internal static int MakeLong(int low, int high) 
			{
				return (high << 16) | (low & 0xffff);
			}
    
			internal static IntPtr MakeLParam(int low, int high) 
			{
				return (IntPtr) ((high << 16) | (low & 0xffff));
			}
    
			internal static int HiWord(int value) 
			{
				return (value >> 16) & 0xffff;
			}
    
			internal static int HiWord(IntPtr value) 
			{
				return HiWord((int)value);
			}
    
			internal static int LowWord(int value) 
			{
				return value & 0xffff;
			}
    
			internal static int LowWord(IntPtr value) 
			{
				return LowWord((int)value);
			}
    
		}
	}
}
