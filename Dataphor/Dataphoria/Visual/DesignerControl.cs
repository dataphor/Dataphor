/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Windows.Forms;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public class DesignerControl : ContainerControl
	{
		public DesignerControl()
		{
			SuspendLayout();
			Dock = DockStyle.Fill;
			FGuage = new DepthGuage();
			Controls.Add(FGuage);
			ResumeLayout(false);
		}

		protected override void Dispose( bool disposing )
		{
			try
			{
				if (FSurfaceStack != null)
				{
					PopAll();
					FSurfaceStack = null;
				}
			}
			finally
			{
				base.Dispose( disposing );
			}
		}

		// Modified

		public event EventHandler OnModified;

		public void Modified()
		{
			if (OnModified != null)
				OnModified(this, EventArgs.Empty);
		}

		private DepthGuage FGuage;

		#region Surfaces

		private ArrayList FSurfaceStack = new ArrayList(4);
		
		/// <summary> Zooms in, replacing currently displayed surface with the specified one. </summary>
		public void Push(Surface ASurface, string ATitle)
		{
			SuspendLayout();
			
			// Hide the old surface
			if (FSurfaceStack.Count >= 1)
			{
				Surface LSurface = (Surface)FSurfaceStack[FSurfaceStack.Count - 1];
				LSurface.RememberActive();
				LSurface.Hide();
			}

			// Show the new surface
			ASurface.Visible = true;
			Controls.Add(ASurface);
			ASurface.BringToFront();
			if (ASurface.ActiveControl == null)
				ASurface.SelectNextControl(null, true, true, true, true);
			ASurface.Focus();
			FSurfaceStack.Add(ASurface);

			// Add the new item to the depth guage
			FGuage.Items.Add(ATitle);

			ResumeLayout(true);
		}

		/// <summary> Zooms out, making to previous surface displayed while disposing the current. </summary>
		public void Pop()
		{
			if (FSurfaceStack.Count >= 1)
			{
				SuspendLayout();

				// Ready the old surface
				Surface LSurface;
				if (FSurfaceStack.Count > 1)
				{
					LSurface = (Surface)FSurfaceStack[FSurfaceStack.Count - 2];
					LSurface.RecallActive();
					LSurface.Show();
					LSurface.Focus();
				}

				// Drop the current surface
				LSurface = (Surface)FSurfaceStack[FSurfaceStack.Count - 1];
				FSurfaceStack.Remove(LSurface);
				LSurface.Dispose();

				// Remove the entry from the guage
				FGuage.Items.RemoveAt(FGuage.Items.Count - 1);
				
				ResumeLayout(true);
			}
		}

		public void PopAll()
		{
			while (FSurfaceStack.Count > 0)
				Pop();
		}

		public void PopAllButTop()
		{
			while (FSurfaceStack.Count > 1)
				Pop();
		}

		/// <summary> The currently displayed surface. </summary>
		public Surface ActiveSurface
		{
			get { return (Surface)FSurfaceStack[FSurfaceStack.Count - 1]; }
		}

		#endregion

		#region Painting

		protected override void OnPaintBackground(PaintEventArgs AArgs)
		{
			if (DesignMode)
				base.OnPaintBackground(AArgs);
			// Optimization: because the background of page will always be 
			// covered by a surface, don't paint it.
		}

		#endregion

		#region Keyboard handling

		protected override bool ProcessDialogChar(char AChar)
		{
			switch (AChar)
			{
				case '-':
					if (CanZoomOut())
					{
						ZoomOut();
						return true;
					}
					else
						return false;
				case '=' :
				case '+' :
					if (CanZoomIn())
					{
						ZoomIn();
						return true;
					}
					else
						return false;
				default :
					return base.ProcessDialogChar(AChar);
			}
		}

		#endregion

		#region ZoomIn & ZoomOut

		// HACK: This method of handling the mouse wheel 
		// is a result of problems that arose when the control over which the 
		// mouse sits become disposed while still in it's message handling.
		// This method ensures that the mouse wheel message is coming directly 
		// from Windows, and not a nested default handling procedure of a child 
		// control

		private bool FPostedWheelMessage;

		protected override void OnMouseWheel(MouseEventArgs AArgs)
		{
			base.OnMouseWheel(AArgs);
			if (FPostedWheelMessage)
			{
				try
				{
					if (AArgs.Delta >= 120)
						InternalZoomIn();
					else if (AArgs.Delta <= -120)
						InternalZoomOut();
				}
				finally
				{
					FPostedWheelMessage = false;
				}
			}
			else
			{
				if (((AArgs.Delta >= 120) && CanZoomIn()) || ((AArgs.Delta <= -120) && CanZoomOut()))
				{
					FPostedWheelMessage = true;
					UnsafeNativeMethods.PostMessage
					(
						Handle, 
						NativeMethods.WM_MOUSEWHEEL, 
						new IntPtr(AArgs.Delta << 16), 
						new IntPtr(AArgs.X | AArgs.Y << 16)
					);
				}
			}
		}

		private IZoomable GetZoomable()
		{
			if (!ContainsFocus)
				return null;
			IntPtr LFocusedHandle = UnsafeNativeMethods.GetFocus();
			if (LFocusedHandle == IntPtr.Zero)
				return null;
			return Control.FromChildHandle(LFocusedHandle) as IZoomable;
		}

		public virtual bool CanZoomIn()
		{
			IZoomable LZoomable = GetZoomable();
			return  (LZoomable != null) && (LZoomable.CanZoomIn());
		}

		protected virtual void InternalZoomIn()
		{
			GetZoomable().ZoomIn();
		}

		public void ZoomIn()
		{
			if (CanZoomIn())
				InternalZoomIn();
		}

		public virtual bool CanZoomOut()
		{
			IZoomable LZoomable = GetZoomable();
			return (FSurfaceStack.Count > 1) || ((LZoomable != null) && (LZoomable.CanZoomOut()));
		}

		protected virtual void InternalZoomOut()
		{
			IZoomable LZoomable = GetZoomable();
			if ((LZoomable != null) && (LZoomable.CanZoomOut()))
				LZoomable.ZoomOut();
			else
				if (FSurfaceStack.Count > 1)
					Pop();
		}

		public void ZoomOut()
		{
			InternalZoomOut();
		}

		#endregion

		#region Utility

		/// <summary> Finds the designer control containing the given control. </summary>
		public static DesignerControl GetDesigner(Control AControl)
		{
			DesignerControl LResult;
			do
			{
				LResult = AControl as DesignerControl;
				if (LResult != null)
					return LResult;
				else
					AControl = AControl.Parent;
			} while (AControl != null);
			return null;
		}

		#endregion
	}

	public class DepthGuage : Control
	{
		public const int CHorizontalPad = -5;
		public const int CHorizontalSpacing = 4;
		public const int CVerticalPadding = 2;
		public const int CCurveWidth = 10;
		public const int CItemTextPad = 1;

		public DepthGuage()
		{
			SuspendLayout();
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.FixedHeight, false);
			SetStyle(ControlStyles.FixedWidth, false);
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.Selectable, false);
			SetStyle(ControlStyles.StandardClick, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			
			Size = new Size(400, 20);
			Dock = DockStyle.Top;
			
			FItems = new DepthGuageItems(this);
			FLineColor = Color.Gray;
			
			ResumeLayout(false);
		}

		#region Cosmetic Properties

		private Color FFadeColor = Color.Thistle;
		public Color FadeColor
		{
			get { return FFadeColor; }
			set
			{
				if (FFadeColor != value)
				{
					FFadeColor = value;
					Invalidate(false);
				}
			}
		}

		private Color FItemColor = Color.FromArgb(230, 160, 160);
		public Color ItemColor
		{
			get { return FItemColor; }
			set
			{
				if (FItemColor != value)
				{
					FItemColor = value;
					Invalidate(false);
				}
			}
		}

		private Color FItemFadeColor = Color.FromArgb(255, 190, 190);
		public Color ItemFadeColor
		{
			get { return FItemFadeColor; }
			set
			{
				if (FItemFadeColor != value)
				{
					FItemFadeColor = value;
					Invalidate(false);
				}
			}
		}

		private Color FLineColor;
		public Color LineColor
		{
			get { return FLineColor; }
			set
			{
				if (FLineColor != value)
				{
					FLineColor = value;
					Invalidate(false);
				}
			}
		}

		#endregion

		private DepthGuageItems FItems;
		public DepthGuageItems Items { get { return FItems; } }

		private int[] FWidths;

		internal void InvalidateWidths()
		{
			FWidths = null;
		}

		private void EnsureWidths(Graphics AGraphics)
		{
			if (FWidths == null)
			{
				FWidths = new int[FItems.Count];
				for (int i = 0; i < FItems.Count; i++)
					FWidths[i] = Size.Ceiling(AGraphics.MeasureString(FItems[i], Font)).Width;
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);

			EnsureWidths(AArgs.Graphics);

			Rectangle LBounds = DisplayRectangle;

			// Fill the background
			using (Brush LBrush = new LinearGradientBrush(LBounds, BackColor, FadeColor, 90))
				AArgs.Graphics.FillRectangle(LBrush, LBounds);

			// Shrink down to the items bounds
			LBounds.Inflate(-CHorizontalSpacing, -(CVerticalPadding - 1));
			AArgs.Graphics.SetClip(LBounds);
			LBounds.Inflate(0, -1);

			int LSum = 0;
			foreach (int LWidth in FWidths)
				LSum += LWidth;
			LSum += (((CCurveWidth * 2) + (CItemTextPad * 2)) * FItems.Count) + (CHorizontalSpacing * (FItems.Count - 1));

			int LExtentX = LBounds.Left + Math.Min(0, LBounds.Width - LSum);
			
			using (Brush LFillBrush = new LinearGradientBrush(LBounds, ItemColor, ItemFadeColor, 90))
			{
				using (Brush LTextBrush = new SolidBrush(ForeColor))
				{
					using (Pen LPen = new Pen(FLineColor))
					{
						int LWidth;
						for (int i = 0; i < FItems.Count; i++)
						{
							using (GraphicsPath LPath = new GraphicsPath())
							{
								LWidth = FWidths[i] + (CItemTextPad * 2);
								LPath.AddLine(LExtentX, LBounds.Y, LExtentX + CCurveWidth + LWidth, LBounds.Y); 
								LPath.AddBezier(LExtentX + CCurveWidth + LWidth, LBounds.Y, LExtentX + CCurveWidth + LWidth + CCurveWidth, LBounds.Y, LExtentX + CCurveWidth + LWidth + CCurveWidth, LBounds.Bottom, LExtentX + CCurveWidth + LWidth, LBounds.Bottom);
								LPath.AddLine(LExtentX + CCurveWidth + LWidth, LBounds.Bottom, LExtentX, LBounds.Bottom);
								LPath.AddBezier(LExtentX, LBounds.Bottom, LExtentX + CCurveWidth, LBounds.Bottom, LExtentX + CCurveWidth, LBounds.Y, LExtentX, LBounds.Y);
								LPath.CloseFigure();
								AArgs.Graphics.FillPath(LFillBrush, LPath);
								AArgs.Graphics.DrawPath(LPen, LPath);
							}
							AArgs.Graphics.DrawString(FItems[i], Font, LTextBrush, LExtentX + CCurveWidth + CItemTextPad, LBounds.Y + ((LBounds.Height / 2) - (Font.Height / 2)));
							LExtentX += FWidths[i] + (CCurveWidth * 2) + (CItemTextPad * 2) + CHorizontalSpacing;
						}
					}
				}
			}
		}
	}

	public class DepthGuageItems : NotifyingBaseList<string>
	{
		public DepthGuageItems(DepthGuage AGuage) 
		{
			FGuage = AGuage;
		}

		private DepthGuage FGuage;

		protected override void Adding(string AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			FGuage.InvalidateWidths();
			FGuage.Invalidate(false);
		}

		protected override void Removing(string AValue, int AIndex)
		{
			base.Removing(AValue, AIndex);
			FGuage.InvalidateWidths();
			FGuage.Invalidate(false);
		}
	}

	public interface IZoomable
	{
		bool CanZoomIn();
		void ZoomIn();
		bool CanZoomOut();
		void ZoomOut();
	}
}
