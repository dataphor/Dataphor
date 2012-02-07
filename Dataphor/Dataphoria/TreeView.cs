/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace Alphora.Dataphor.Dataphoria
{
	public class TreeView : System.Windows.Forms.TreeView
	{
		public const int DragOverInterval = 500;
		public const int HoverScrollInterval = 50;
		public const int HoverScrollMargin = 5;

		protected override void Dispose(bool disposing)
		{
			ClearTimer();
			base.Dispose(disposing);
		}

		#region Custom Painting

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);
			if (message.Msg == (int)NativeMethods.OCM_NOTIFY)
			{
				NMHDR header = (NMHDR)message.GetLParam(typeof(NMHDR));	
				if (header.code == (int)NotificationMessages.NM_CUSTOMDRAW)
					NotifyTreeCustomDraw(ref message);
			}
		}

		private bool NotifyTreeCustomDraw(ref Message message)
		{
			message.Result = (IntPtr)CustomDrawReturnFlags.CDRF_DODEFAULT;
			NMTVCUSTOMDRAW customDrawInfo = (NMTVCUSTOMDRAW)message.GetLParam(typeof(NMTVCUSTOMDRAW));
			IntPtr thisHandle = Handle;
			
			if (customDrawInfo.nmcd.hdr.hwndFrom == Handle)
			{
				switch (customDrawInfo.nmcd.dwDrawStage)
				{
					case (int)CustomDrawDrawStateFlags.CDDS_PREPAINT:
						message.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYITEMDRAW;
						break;
					case (int)CustomDrawDrawStateFlags.CDDS_ITEMPREPAINT:
						message.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYPOSTPAINT;
						PaintTreeItemEventArgs args = GetPaintTreeItemEventArgs(customDrawInfo);
						try
						{
							args.ForeColor = SystemColors.WindowText;
							args.BackColor = SystemColors.Window;
							if (DoPrePaintItem(args))
							{
								DoPostPaintItem(args);
								message.Result = (IntPtr)CustomDrawReturnFlags.CDRF_SKIPDEFAULT;
							}
							customDrawInfo.clrTextBk = 
								RGB
								(
									args.BackColor.R,
									args.BackColor.G, 
									args.BackColor.B
								);
							customDrawInfo.clrText =
								RGB
								(
									args.ForeColor.R,
									args.ForeColor.G, 
									args.ForeColor.B
								);
							Marshal.StructureToPtr(customDrawInfo, message.LParam, true);
						}
						finally
						{
							args.Graphics.Dispose();
						}
						break;
					case (int)CustomDrawDrawStateFlags.CDDS_ITEMPOSTPAINT:
						args = GetPaintTreeItemEventArgs(customDrawInfo);
						try
						{
							DoPostPaintItem(args);
						}
						finally
						{
							args.Graphics.Dispose();
						}
						break;
				}
			}
			return false;
		}

		static private uint RGB(int r, int g, int b)
		{
			return ((uint)(((byte)(r)|((short)((byte)(g))<<8))|(((short)(byte)(b))<<16)));
		}

		private PaintTreeItemEventArgs GetPaintTreeItemEventArgs(NMTVCUSTOMDRAW customDrawInfo)
		{
			TreeNode node = TreeNode.FromHandle(this, (IntPtr)customDrawInfo.nmcd.dwItemSpec);
			return
				new PaintTreeItemEventArgs
				(
					node,
					Graphics.FromHdc(customDrawInfo.nmcd.hdc),
					(customDrawInfo.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_FOCUS) != 0,
					(customDrawInfo.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_SELECTED) != 0,
					Rectangle.FromLTRB(customDrawInfo.nmcd.rc.left, customDrawInfo.nmcd.rc.top, customDrawInfo.nmcd.rc.right, customDrawInfo.nmcd.rc.bottom)
				);
		}

		public event PaintTreeItemEventHandler OnPrePaintItem;

		/// <returns> True if painting is handled (should skip default). </returns>
		protected virtual bool DoPrePaintItem(PaintTreeItemEventArgs args)
		{
			if (args.Node != null)
			{
				using (Brush brush = new SolidBrush(SystemColors.Window))
				{
					args.Graphics.FillRectangle(brush, args.Bounds);
				}
				if (OnPrePaintItem != null)
					OnPrePaintItem(this, args);
			}
			return false;
		}

		public event PaintTreeItemEventHandler OnPostPaintItem;

		protected virtual void DoPostPaintItem(PaintTreeItemEventArgs args)
		{
			if (args.Node != null)
			{
				Rectangle bounds = args.Node.Bounds;
				bounds.Height -= 1;
				if (args.Focused)
					using (Brush brush = new SolidBrush(Color.FromArgb(70, SystemColors.Highlight)))
					{
						args.Graphics.FillRectangle(brush, bounds);
					}
				if (args.Selected)
					args.Graphics.DrawRectangle(SystemPens.Highlight, bounds);
				if (OnPostPaintItem != null)
					OnPostPaintItem(this, args);
			}
		}

		#endregion

		#region Drag & Drop and hover

		private HoverTimer _hoverTimer;

		private void ClearTimer()
		{
			if (_hoverTimer != null)
			{
				_hoverTimer.Dispose();
				_hoverTimer = null;
			}
		}

		protected override void OnDragOver(DragEventArgs args)
		{
			base.OnDragOver(args);
			HoverAction newAction;
			TreeNode newNode = null;
			Point localPoint = PointToClient(new Point(args.X, args.Y));
			if (localPoint.Y < HoverScrollMargin)
				newAction = HoverAction.ScrollUp;
			else if (localPoint.Y > (ClientSize.Height - HoverScrollMargin))
				newAction = HoverAction.ScrollDown;
			else
			{
				newAction = HoverAction.OverNode;
				newNode = GetNodeAt(localPoint.X, localPoint.Y);
			}

			if (_hoverTimer != null)
			{
				if ((_hoverTimer.Action != newAction) || (_hoverTimer.Node != newNode))
					ClearTimer();
				else
					return;		// do not reset the timer if nothing has changed
			}

			if ((newAction != HoverAction.OverNode) || (newNode != null))
			{
				_hoverTimer = new HoverTimer(newAction, newNode);
				switch (newAction)
				{
					case HoverAction.OverNode :
						_hoverTimer.Interval = DragOverInterval;
						_hoverTimer.Tick += new EventHandler(HoverExpand);
						break;
					case HoverAction.ScrollUp :
					case HoverAction.ScrollDown :
						_hoverTimer.Interval = HoverScrollInterval;
						_hoverTimer.Tick += new EventHandler(HoverScroll);
						break;
				}
				_hoverTimer.Enabled = true;
			}
		}

		protected override void OnDragLeave(EventArgs args)
		{
			base.OnDragLeave(args);
			ClearTimer();
		}

		private void ScrollTree(int delta)
		{
			int position = UnsafeNativeMethods.GetScrollPos(Handle, 1);
			UnsafeNativeMethods.SetScrollPos(Handle, 1, position + delta, true);
			UnsafeNativeMethods.PostMessage(Handle, NativeMethods.WM_SETREDRAW, (IntPtr)1, (IntPtr)0);
		}

		private void HoverScroll(object sender, EventArgs args)
		{
			HoverTimer timer = (HoverTimer)sender;
			if (timer.Action == HoverAction.ScrollUp)
				ScrollTree(-2);
			else
				ScrollTree(2);
		}

		private void HoverExpand(object sender, EventArgs args)
		{
			HoverTimer timer = (HoverTimer)sender;
			if (timer.Node != null)
				timer.Node.Expand();
			timer.Enabled = false;
		}

		#endregion
	}

	public enum HoverAction 
	{
		ScrollUp,
		ScrollDown,
		OverNode
	}

	public class HoverTimer : Timer
	{
		public HoverTimer(HoverAction action, TreeNode node)
		{
			_action = action;
			_node = node;
		}

		private HoverAction _action;
		public HoverAction Action
		{
			get { return _action; }
		}

		private TreeNode _node;
		public TreeNode Node
		{
			get { return _node; }
		}
	}

	public class PaintTreeItemEventArgs
	{
		public PaintTreeItemEventArgs(TreeNode node, Graphics graphics, bool focused, bool selected, Rectangle bounds)
		{
			_node = node;
			_graphics = graphics;
			_focused = focused;
			_selected = selected;
			_bounds = bounds;
		}

		private TreeNode _node;
		public TreeNode Node
		{
			get { return _node; }
		}

		private Graphics _graphics;
		public Graphics Graphics
		{
			get { return _graphics; }
		}

		private Rectangle _bounds;
		public Rectangle Bounds
		{
			get { return _bounds; }
			set { _bounds = value; }
		}

		private bool _focused;
		public bool Focused
		{
			get { return _focused; }
			set { _focused = value; }
		}

		private bool _selected;
		public bool Selected
		{
			get { return _selected; }
			set { _selected = value; }
		}

		private Color _backColor;
		public Color BackColor
		{
			get { return _backColor; }
			set { _backColor = value; }
		}

		private Color _foreColor;
		public Color ForeColor
		{
			get { return _foreColor; }
			set { _foreColor = value; }
		}

	}

	public delegate void PaintTreeItemEventHandler(object ASender, PaintTreeItemEventArgs AArgs);

	public enum NotificationMessages
	{
		NM_FIRST      = 0,
		NM_CUSTOMDRAW = (NM_FIRST - 12),
		NM_NCHITTEST  = (NM_FIRST - 14) 
	}

	public enum TreeViewMessages
	{
		TV_FIRST        =  0x1100,
		TVM_GETITEMRECT = (TV_FIRST + 4),
		TVM_GETITEMW    = (TV_FIRST + 62)
	}

	public enum TreeViewItemFlags
	{
		TVIF_TEXT               = 0x0001,
		TVIF_IMAGE              = 0x0002,
		TVIF_PARAM              = 0x0004,
		TVIF_STATE              = 0x0008,
		TVIF_HANDLE             = 0x0010,
		TVIF_SELECTEDIMAGE      = 0x0020,
		TVIF_CHILDREN           = 0x0040,
		TVIF_INTEGRAL           = 0x0080
	}

	public enum CustomDrawItemStateFlags
	{
		CDIS_SELECTED       = 0x0001,
		CDIS_GRAYED         = 0x0002,
		CDIS_DISABLED       = 0x0004,
		CDIS_CHECKED        = 0x0008,
		CDIS_FOCUS          = 0x0010,
		CDIS_DEFAULT        = 0x0020,
		CDIS_HOT            = 0x0040,
		CDIS_MARKED         = 0x0080,
		CDIS_INDETERMINATE  = 0x0100
	}

	public enum CustomDrawDrawStateFlags
	{
		CDDS_PREPAINT           = 0x00000001,
		CDDS_POSTPAINT          = 0x00000002,
		CDDS_PREERASE           = 0x00000003,
		CDDS_POSTERASE          = 0x00000004,
		CDDS_ITEM               = 0x00010000,
		CDDS_ITEMPREPAINT       = (CDDS_ITEM | CDDS_PREPAINT),
		CDDS_ITEMPOSTPAINT      = (CDDS_ITEM | CDDS_POSTPAINT),
		CDDS_ITEMPREERASE       = (CDDS_ITEM | CDDS_PREERASE),
		CDDS_ITEMPOSTERASE      = (CDDS_ITEM | CDDS_POSTERASE),
		CDDS_SUBITEM            = 0x00020000
	}

	public enum CustomDrawReturnFlags
	{
		CDRF_DODEFAULT          = 0x00000000,
		CDRF_NEWFONT            = 0x00000002,
		CDRF_SKIPDEFAULT        = 0x00000004,
		CDRF_NOTIFYPOSTPAINT    = 0x00000010,
		CDRF_NOTIFYITEMDRAW     = 0x00000020,
		CDRF_NOTIFYSUBITEMDRAW  = 0x00000020, 
		CDRF_NOTIFYPOSTERASE    = 0x00000040
	}

	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
	public struct TVITEM 
	{
		public	uint      mask;
		public	IntPtr    hItem;
		public	uint      state;
		public	uint      stateMask;
		public	IntPtr    pszText;
		public	int       cchTextMax;
		public	int       iImage;
		public	int       iSelectedImage;
		public	int       cChildren;
		public	int       lParam;
	} 

	[StructLayout(LayoutKind.Sequential)]
	public struct NMHDR
	{
		public IntPtr hwndFrom;
		public int idFrom;
		public int code;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct NMCUSTOMDRAW
	{
		public NMHDR hdr;
		public int dwDrawStage;
		public IntPtr hdc;
		public RECT rc;
		public int dwItemSpec;
		public int uItemState;
		public int lItemlParam;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct NMTVCUSTOMDRAW 
	{
		public NMCUSTOMDRAW nmcd;
		public uint clrText;
		public uint clrTextBk;
		public int iLevel;
	}

}