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
		public const int CDragOverInterval = 500;
		public const int CHoverScrollInterval = 50;
		public const int CHoverScrollMargin = 5;

		protected override void Dispose(bool ADisposing)
		{
			ClearTimer();
			base.Dispose(ADisposing);
		}

		#region Custom Painting

		protected override void WndProc(ref Message AMessage)
		{
			base.WndProc(ref AMessage);
			if (AMessage.Msg == (int)NativeMethods.OCM_NOTIFY)
			{
				NMHDR LHeader = (NMHDR)AMessage.GetLParam(typeof(NMHDR));	
				if (LHeader.code == (int)NotificationMessages.NM_CUSTOMDRAW)
					NotifyTreeCustomDraw(ref AMessage);
			}
		}

		private bool NotifyTreeCustomDraw(ref Message AMessage)
		{
			AMessage.Result = (IntPtr)CustomDrawReturnFlags.CDRF_DODEFAULT;
			NMTVCUSTOMDRAW LCustomDrawInfo = (NMTVCUSTOMDRAW)AMessage.GetLParam(typeof(NMTVCUSTOMDRAW));
			IntPtr LThisHandle = Handle;
			
			if (LCustomDrawInfo.nmcd.hdr.hwndFrom == Handle)
			{
				switch (LCustomDrawInfo.nmcd.dwDrawStage)
				{
					case (int)CustomDrawDrawStateFlags.CDDS_PREPAINT:
						AMessage.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYITEMDRAW;
						break;
					case (int)CustomDrawDrawStateFlags.CDDS_ITEMPREPAINT:
						AMessage.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYPOSTPAINT;
						PaintTreeItemEventArgs LArgs = GetPaintTreeItemEventArgs(LCustomDrawInfo);
						try
						{
							LArgs.ForeColor = SystemColors.WindowText;
							LArgs.BackColor = SystemColors.Window;
							if (DoPrePaintItem(LArgs))
							{
								DoPostPaintItem(LArgs);
								AMessage.Result = (IntPtr)CustomDrawReturnFlags.CDRF_SKIPDEFAULT;
							}
							LCustomDrawInfo.clrTextBk = 
								RGB
								(
									LArgs.BackColor.R,
									LArgs.BackColor.G, 
									LArgs.BackColor.B
								);
							LCustomDrawInfo.clrText =
								RGB
								(
									LArgs.ForeColor.R,
									LArgs.ForeColor.G, 
									LArgs.ForeColor.B
								);
							Marshal.StructureToPtr(LCustomDrawInfo, AMessage.LParam, true);
						}
						finally
						{
							LArgs.Graphics.Dispose();
						}
						break;
					case (int)CustomDrawDrawStateFlags.CDDS_ITEMPOSTPAINT:
						LArgs = GetPaintTreeItemEventArgs(LCustomDrawInfo);
						try
						{
							DoPostPaintItem(LArgs);
						}
						finally
						{
							LArgs.Graphics.Dispose();
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

		private PaintTreeItemEventArgs GetPaintTreeItemEventArgs(NMTVCUSTOMDRAW ACustomDrawInfo)
		{
			TreeNode LNode = TreeNode.FromHandle(this, (IntPtr)ACustomDrawInfo.nmcd.dwItemSpec);
			return
				new PaintTreeItemEventArgs
				(
					LNode,
					Graphics.FromHdc(ACustomDrawInfo.nmcd.hdc),
					(ACustomDrawInfo.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_FOCUS) != 0,
					(ACustomDrawInfo.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_SELECTED) != 0,
					Rectangle.FromLTRB(ACustomDrawInfo.nmcd.rc.left, ACustomDrawInfo.nmcd.rc.top, ACustomDrawInfo.nmcd.rc.right, ACustomDrawInfo.nmcd.rc.bottom)
				);
		}

		public event PaintTreeItemEventHandler OnPrePaintItem;

		/// <returns> True if painting is handled (should skip default). </returns>
		protected virtual bool DoPrePaintItem(PaintTreeItemEventArgs AArgs)
		{
			if (AArgs.Node != null)
			{
				using (Brush LBrush = new SolidBrush(SystemColors.Window))
				{
					AArgs.Graphics.FillRectangle(LBrush, AArgs.Bounds);
				}
				if (OnPrePaintItem != null)
					OnPrePaintItem(this, AArgs);
			}
			return false;
		}

		public event PaintTreeItemEventHandler OnPostPaintItem;

		protected virtual void DoPostPaintItem(PaintTreeItemEventArgs AArgs)
		{
			if (AArgs.Node != null)
			{
				Rectangle LBounds = AArgs.Node.Bounds;
				LBounds.Height -= 1;
				if (AArgs.Focused)
					using (Brush LBrush = new SolidBrush(Color.FromArgb(70, SystemColors.Highlight)))
					{
						AArgs.Graphics.FillRectangle(LBrush, LBounds);
					}
				if (AArgs.Selected)
					AArgs.Graphics.DrawRectangle(SystemPens.Highlight, LBounds);
				if (OnPostPaintItem != null)
					OnPostPaintItem(this, AArgs);
			}
		}

		#endregion

		#region Drag & Drop and hover

		private HoverTimer FHoverTimer;

		private void ClearTimer()
		{
			if (FHoverTimer != null)
			{
				FHoverTimer.Dispose();
				FHoverTimer = null;
			}
		}

		protected override void OnDragOver(DragEventArgs AArgs)
		{
			base.OnDragOver(AArgs);
			HoverAction LNewAction;
			TreeNode LNewNode = null;
			Point LLocalPoint = PointToClient(new Point(AArgs.X, AArgs.Y));
			if (LLocalPoint.Y < CHoverScrollMargin)
				LNewAction = HoverAction.ScrollUp;
			else if (LLocalPoint.Y > (ClientSize.Height - CHoverScrollMargin))
				LNewAction = HoverAction.ScrollDown;
			else
			{
				LNewAction = HoverAction.OverNode;
				LNewNode = GetNodeAt(LLocalPoint.X, LLocalPoint.Y);
			}

			if (FHoverTimer != null)
			{
				if ((FHoverTimer.Action != LNewAction) || (FHoverTimer.Node != LNewNode))
					ClearTimer();
				else
					return;		// do not reset the timer if nothing has changed
			}

			if ((LNewAction != HoverAction.OverNode) || (LNewNode != null))
			{
				FHoverTimer = new HoverTimer(LNewAction, LNewNode);
				switch (LNewAction)
				{
					case HoverAction.OverNode :
						FHoverTimer.Interval = CDragOverInterval;
						FHoverTimer.Tick += new EventHandler(HoverExpand);
						break;
					case HoverAction.ScrollUp :
					case HoverAction.ScrollDown :
						FHoverTimer.Interval = CHoverScrollInterval;
						FHoverTimer.Tick += new EventHandler(HoverScroll);
						break;
				}
				FHoverTimer.Enabled = true;
			}
		}

		protected override void OnDragLeave(EventArgs AArgs)
		{
			base.OnDragLeave(AArgs);
			ClearTimer();
		}

		private void ScrollTree(int ADelta)
		{
			int LPosition = UnsafeNativeMethods.GetScrollPos(Handle, 1);
			UnsafeNativeMethods.SetScrollPos(Handle, 1, LPosition + ADelta, true);
			UnsafeNativeMethods.PostMessage(Handle, NativeMethods.WM_SETREDRAW, (IntPtr)1, (IntPtr)0);
		}

		private void HoverScroll(object ASender, EventArgs AArgs)
		{
			HoverTimer LTimer = (HoverTimer)ASender;
			if (LTimer.Action == HoverAction.ScrollUp)
				ScrollTree(-2);
			else
				ScrollTree(2);
		}

		private void HoverExpand(object ASender, EventArgs AArgs)
		{
			HoverTimer LTimer = (HoverTimer)ASender;
			if (LTimer.Node != null)
				LTimer.Node.Expand();
			LTimer.Enabled = false;
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
		public HoverTimer(HoverAction AAction, TreeNode ANode)
		{
			FAction = AAction;
			FNode = ANode;
		}

		private HoverAction FAction;
		public HoverAction Action
		{
			get { return FAction; }
		}

		private TreeNode FNode;
		public TreeNode Node
		{
			get { return FNode; }
		}
	}

	public class PaintTreeItemEventArgs
	{
		public PaintTreeItemEventArgs(TreeNode ANode, Graphics AGraphics, bool AFocused, bool ASelected, Rectangle ABounds)
		{
			FNode = ANode;
			FGraphics = AGraphics;
			FFocused = AFocused;
			FSelected = ASelected;
			FBounds = ABounds;
		}

		private TreeNode FNode;
		public TreeNode Node
		{
			get { return FNode; }
		}

		private Graphics FGraphics;
		public Graphics Graphics
		{
			get { return FGraphics; }
		}

		private Rectangle FBounds;
		public Rectangle Bounds
		{
			get { return FBounds; }
			set { FBounds = value; }
		}

		private bool FFocused;
		public bool Focused
		{
			get { return FFocused; }
			set { FFocused = value; }
		}

		private bool FSelected;
		public bool Selected
		{
			get { return FSelected; }
			set { FSelected = value; }
		}

		private Color FBackColor;
		public Color BackColor
		{
			get { return FBackColor; }
			set { FBackColor = value; }
		}

		private Color FForeColor;
		public Color ForeColor
		{
			get { return FForeColor; }
			set { FForeColor = value; }
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