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
			_guage = new DepthGuage();
			Controls.Add(_guage);
			ResumeLayout(false);
		}

		protected override void Dispose( bool disposing )
		{
			try
			{
				if (_surfaceStack != null)
				{
					PopAll();
					_surfaceStack = null;
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

		private DepthGuage _guage;

		#region Surfaces

		private ArrayList _surfaceStack = new ArrayList(4);
		
		/// <summary> Zooms in, replacing currently displayed surface with the specified one. </summary>
		public void Push(Surface surface, string title)
		{
			SuspendLayout();
			
			// Hide the old surface
			if (_surfaceStack.Count >= 1)
			{
				Surface localSurface = (Surface)_surfaceStack[_surfaceStack.Count - 1];
				localSurface.RememberActive();
				localSurface.Hide();
			}

			// Show the new surface
			surface.Visible = true;
			Controls.Add(surface);
			surface.BringToFront();
			if (surface.ActiveControl == null)
				surface.SelectNextControl(null, true, true, true, true);
			surface.Focus();
			_surfaceStack.Add(surface);

			// Add the new item to the depth guage
			_guage.Items.Add(title);

			ResumeLayout(true);
		}

		/// <summary> Zooms out, making to previous surface displayed while disposing the current. </summary>
		public void Pop()
		{
			if (_surfaceStack.Count >= 1)
			{
				SuspendLayout();

				// Ready the old surface
				Surface surface;
				if (_surfaceStack.Count > 1)
				{
					surface = (Surface)_surfaceStack[_surfaceStack.Count - 2];
					surface.RecallActive();
					surface.Show();
					surface.Focus();
				}

				// Drop the current surface
				surface = (Surface)_surfaceStack[_surfaceStack.Count - 1];
				_surfaceStack.Remove(surface);
				surface.Dispose();

				// Remove the entry from the guage
				_guage.Items.RemoveAt(_guage.Items.Count - 1);
				
				ResumeLayout(true);
			}
		}

		public void PopAll()
		{
			while (_surfaceStack.Count > 0)
				Pop();
		}

		public void PopAllButTop()
		{
			while (_surfaceStack.Count > 1)
				Pop();
		}

		/// <summary> The currently displayed surface. </summary>
		public Surface ActiveSurface
		{
			get { return (Surface)_surfaceStack[_surfaceStack.Count - 1]; }
		}

		#endregion

		#region Painting

		protected override void OnPaintBackground(PaintEventArgs args)
		{
			if (DesignMode)
				base.OnPaintBackground(args);
			// Optimization: because the background of page will always be 
			// covered by a surface, don't paint it.
		}

		#endregion

		#region Keyboard handling

		protected override bool ProcessDialogChar(char charValue)
		{
			switch (charValue)
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
					return base.ProcessDialogChar(charValue);
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

		private bool _postedWheelMessage;

		protected override void OnMouseWheel(MouseEventArgs args)
		{
			base.OnMouseWheel(args);
			if (_postedWheelMessage)
			{
				try
				{
					if (args.Delta >= 120)
						InternalZoomIn();
					else if (args.Delta <= -120)
						InternalZoomOut();
				}
				finally
				{
					_postedWheelMessage = false;
				}
			}
			else
			{
				if (((args.Delta >= 120) && CanZoomIn()) || ((args.Delta <= -120) && CanZoomOut()))
				{
					_postedWheelMessage = true;
					UnsafeNativeMethods.PostMessage
					(
						Handle, 
						NativeMethods.WM_MOUSEWHEEL, 
						new IntPtr(args.Delta << 16), 
						new IntPtr(args.X | args.Y << 16)
					);
				}
			}
		}

		private IZoomable GetZoomable()
		{
			if (!ContainsFocus)
				return null;
			IntPtr focusedHandle = UnsafeNativeMethods.GetFocus();
			if (focusedHandle == IntPtr.Zero)
				return null;
			return Control.FromChildHandle(focusedHandle) as IZoomable;
		}

		public virtual bool CanZoomIn()
		{
			IZoomable zoomable = GetZoomable();
			return  (zoomable != null) && (zoomable.CanZoomIn());
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
			IZoomable zoomable = GetZoomable();
			return (_surfaceStack.Count > 1) || ((zoomable != null) && (zoomable.CanZoomOut()));
		}

		protected virtual void InternalZoomOut()
		{
			IZoomable zoomable = GetZoomable();
			if ((zoomable != null) && (zoomable.CanZoomOut()))
				zoomable.ZoomOut();
			else
				if (_surfaceStack.Count > 1)
					Pop();
		}

		public void ZoomOut()
		{
			InternalZoomOut();
		}

		#endregion

		#region Utility

		/// <summary> Finds the designer control containing the given control. </summary>
		public static DesignerControl GetDesigner(Control control)
		{
			DesignerControl result;
			do
			{
				result = control as DesignerControl;
				if (result != null)
					return result;
				else
					control = control.Parent;
			} while (control != null);
			return null;
		}

		#endregion
	}

	public class DepthGuage : Control
	{
		public const int HorizontalPad = -5;
		public const int HorizontalSpacing = 4;
		public const int VerticalPadding = 2;
		public const int CurveWidth = 10;
		public const int ItemTextPad = 1;

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
			
			_items = new DepthGuageItems(this);
			_lineColor = Color.Gray;
			
			ResumeLayout(false);
		}

		#region Cosmetic Properties

		private Color _fadeColor = Color.Thistle;
		public Color FadeColor
		{
			get { return _fadeColor; }
			set
			{
				if (_fadeColor != value)
				{
					_fadeColor = value;
					Invalidate(false);
				}
			}
		}

		private Color _itemColor = Color.FromArgb(230, 160, 160);
		public Color ItemColor
		{
			get { return _itemColor; }
			set
			{
				if (_itemColor != value)
				{
					_itemColor = value;
					Invalidate(false);
				}
			}
		}

		private Color _itemFadeColor = Color.FromArgb(255, 190, 190);
		public Color ItemFadeColor
		{
			get { return _itemFadeColor; }
			set
			{
				if (_itemFadeColor != value)
				{
					_itemFadeColor = value;
					Invalidate(false);
				}
			}
		}

		private Color _lineColor;
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				if (_lineColor != value)
				{
					_lineColor = value;
					Invalidate(false);
				}
			}
		}

		#endregion

		private DepthGuageItems _items;
		public DepthGuageItems Items { get { return _items; } }

		private int[] _widths;

		internal void InvalidateWidths()
		{
			_widths = null;
		}

		private void EnsureWidths(Graphics graphics)
		{
			if (_widths == null)
			{
				_widths = new int[_items.Count];
				for (int i = 0; i < _items.Count; i++)
					_widths[i] = Size.Ceiling(graphics.MeasureString(_items[i], Font)).Width;
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);

			EnsureWidths(args.Graphics);

			Rectangle bounds = DisplayRectangle;

			// Fill the background
			using (Brush brush = new LinearGradientBrush(bounds, BackColor, FadeColor, 90))
				args.Graphics.FillRectangle(brush, bounds);

			// Shrink down to the items bounds
			bounds.Inflate(-HorizontalSpacing, -(VerticalPadding - 1));
			args.Graphics.SetClip(bounds);
			bounds.Inflate(0, -1);

			int sum = 0;
			foreach (int width in _widths)
				sum += width;
			sum += (((CurveWidth * 2) + (ItemTextPad * 2)) * _items.Count) + (HorizontalSpacing * (_items.Count - 1));

			int extentX = bounds.Left + Math.Min(0, bounds.Width - sum);
			
			using (Brush fillBrush = new LinearGradientBrush(bounds, ItemColor, ItemFadeColor, 90))
			{
				using (Brush textBrush = new SolidBrush(ForeColor))
				{
					using (Pen pen = new Pen(_lineColor))
					{
						int width;
						for (int i = 0; i < _items.Count; i++)
						{
							using (GraphicsPath path = new GraphicsPath())
							{
								width = _widths[i] + (ItemTextPad * 2);
								path.AddLine(extentX, bounds.Y, extentX + CurveWidth + width, bounds.Y); 
								path.AddBezier(extentX + CurveWidth + width, bounds.Y, extentX + CurveWidth + width + CurveWidth, bounds.Y, extentX + CurveWidth + width + CurveWidth, bounds.Bottom, extentX + CurveWidth + width, bounds.Bottom);
								path.AddLine(extentX + CurveWidth + width, bounds.Bottom, extentX, bounds.Bottom);
								path.AddBezier(extentX, bounds.Bottom, extentX + CurveWidth, bounds.Bottom, extentX + CurveWidth, bounds.Y, extentX, bounds.Y);
								path.CloseFigure();
								args.Graphics.FillPath(fillBrush, path);
								args.Graphics.DrawPath(pen, path);
							}
							args.Graphics.DrawString(_items[i], Font, textBrush, extentX + CurveWidth + ItemTextPad, bounds.Y + ((bounds.Height / 2) - (Font.Height / 2)));
							extentX += _widths[i] + (CurveWidth * 2) + (ItemTextPad * 2) + HorizontalSpacing;
						}
					}
				}
			}
		}
	}

	public class DepthGuageItems : NotifyingBaseList<string>
	{
		public DepthGuageItems(DepthGuage guage) 
		{
			_guage = guage;
		}

		private DepthGuage _guage;

		protected override void Adding(string tempValue, int index)
		{
			base.Adding(tempValue, index);
			_guage.InvalidateWidths();
			_guage.Invalidate(false);
		}

		protected override void Removing(string tempValue, int index)
		{
			base.Removing(tempValue, index);
			_guage.InvalidateWidths();
			_guage.Invalidate(false);
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
