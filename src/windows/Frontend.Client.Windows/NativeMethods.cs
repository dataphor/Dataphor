/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	internal class NativeMethods
	{
		internal const int
			WM_KEYDOWN = 0x0100,
			WM_SYSCOMMAND = 0x0112,
			WM_SETREDRAW = 0x000B,
			WM_CLOSE = 0x0010,
			SC_NATURALSIZE = 0x0001;

		internal const int
			MIIM_STATE = 0x0001,
			MIIM_ID = 0x0002,
			MIIM_SUBMENU = 0x0004,
			MIIM_CHECKMARKS = 0x0008,
			MIIM_TYPE = 0x0010,
			MIIM_DATA = 0x0020,
			MIIM_STRING = 0x0040,
			MIIM_BITMAP = 0x0080,
			MIIM_FTYPE = 0x0100;

		/* Menu flags */
		internal const int
			MF_INSERT = 0x0000,
			MF_CHANGE = 0x0080,
			MF_APPEND = 0x0100,
			MF_DELETE = 0x0200,
			MF_REMOVE = 0x1000,
			MF_BYCOMMAND = 0x0000,
			MF_BYPOSITION = 0x0400,
			MF_SEPARATOR = 0x0800,
			MF_ENABLED = 0x0000,
			MF_GRAYED = 0x0001,
			MF_DISABLED = 0x0002,
			MF_UNCHECKED = 0x0000,
			MF_CHECKED = 0x0008,
			MF_USECHECKBITMAPS = 0x0200,
			MF_STRING = 0x0000,
			MF_BITMAP = 0x0004,
			MF_OWNERDRAW = 0x0100,
			MF_POPUP = 0x10,
			MF_MENUBARBREAK = 0x0020,
			MF_MENUBREAK = 0x0040,
			MF_UNHILITE = 0x0000,
			MF_HILITE = 0x0080,
			MF_DEFAULT = 0x1000,
			MF_SYSMENU = 0x2000,
			MF_HELP = 0x4000,
			MF_RIGHTJUSTIFY = 0x4000,
			MF_MOUSESELECT = 0x8000,
			MF_END = 0x0080;

		internal const int
			MFT_STRING = MF_STRING,
			MFT_BITMAP = MF_BITMAP,
			MFT_MENUBARBREAK = MF_MENUBARBREAK,
			MFT_MENUBREAK = MF_MENUBREAK,
			MFT_OWNERDRAW = MF_OWNERDRAW,
			MFT_RADIOCHECK = 0x0200,
			MFT_SEPARATOR = MF_SEPARATOR,
			MFT_RIGHTORDER = 0x2000,
			MFT_RIGHTJUSTIFY = MF_RIGHTJUSTIFY;

		/* Menu state flags */
		internal const int
			MFS_GRAYED = 0x0003,
			MFS_DISABLED = MFS_GRAYED,
			MFS_CHECKED = MF_CHECKED,
			MFS_HILITE = MF_HILITE,
			MFS_ENABLED = MF_ENABLED,
			MFS_UNCHECKED = MF_UNCHECKED,
			MFS_UNHILITE = MF_UNHILITE,
			MFS_DEFAULT = MF_DEFAULT;

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		internal struct MenuItemInfo
		{
			public int cbSize;				//Sizeof this structure in bytes.
			public int fMask;
			public int fType;				
			public int fState;				
			public int wID;					
			public IntPtr hSubMenu;			
			public IntPtr hbmpChecked;		
			public IntPtr hbmpUnchecked;	
			public int dwItemData;			
			public string dwTypeData;		
			public int cch;					
		}

	}
}
