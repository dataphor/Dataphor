/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.LookupPanel),"Icons.LookupPanel.bmp")]
	public class LookupPanel : LookupBase
	{
		public const int CTextOffset = 8;
		public const int CTextMargin = 2;

		public LookupPanel() : base()
		{
			SuspendLayout();

			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.CacheText, true);
			SetStyle(ControlStyles.StandardClick, true);

			TabStop = true;
			Size = new Size(120, 60);

			ResumeLayout(false);
		}

		public override bool FocusControl()
		{
			Focus();
			return Focused;
		}

		public event EventHandler ClearValue;
		
		protected virtual void OnClearValue(EventArgs AArgs)
		{
			if (ClearValue != null)
				ClearValue(this, AArgs);
		}
		
		protected void PerformClearValue()
		{
			OnClearValue(EventArgs.Empty);
		}
		
		#region Keyboard & Mouse

		protected override void OnClick(EventArgs AArgs)
		{
			base.OnClick(AArgs);
			Focus();
		}

		protected override bool ProcessMnemonic(char AChar)
		{
			if (Control.IsMnemonic(AChar, Text))
			{
				Focus();
				return true;
			}
			else
				return base.ProcessMnemonic(AChar);
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if (AKey == (Keys.Control | Keys.Back))
			{
				PerformClearValue();
				return true;
			}
			else
				return base.ProcessDialogKey(AKey);
		}

		protected override bool ProcessKeyPreview(ref Message AMessage)
		{
			const int WM_KEYUP = 0x101;
			if (AMessage.Msg == WM_KEYUP && (((Keys)((int)((long)AMessage.WParam))) | ModifierKeys) == (Keys.Control | Keys.Back))
			{
				PerformClearValue();
				return true;
			}
			else
				return base.ProcessKeyPreview(ref AMessage);
		}

		#endregion

		#region Layout

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);

			// Layout the button
			Rectangle LBounds = base.DisplayRectangle;
			int LDeltaY = (Text != String.Empty) ? (Font.Height / 2) + 1 : 0;
			Button.Bounds =
				new Rectangle
				(
					LBounds.Right - (1 + Button.Width),
					LBounds.Y + LDeltaY,
					Button.Width,
					LBounds.Height - (LDeltaY + 1)
				);
		}

		[Browsable(false)]
		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				LBounds.Inflate(-2, -2);
				LBounds.Width -= Button.Width;
				if (Text != String.Empty)
				{
					LBounds.Y += Font.Height;
					LBounds.Height -= Font.Height;
				}
				return LBounds;
			}
		}

		#endregion

		#region Painting

		private static StringFormat FFormat;
		private static StringFormat GetFormat()
		{
			if (FFormat == null)
			{
				FFormat = new StringFormat(StringFormat.GenericDefault);
				FFormat.Trimming = StringTrimming.EllipsisWord;
				FFormat.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
			}
			return FFormat;
		}

		protected override void OnTextChanged(EventArgs AArgs)
		{
			base.OnTextChanged(AArgs);
			PerformLayout(this, "Height");
			Invalidate();
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
			InvalidateFocusBorder();
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			InvalidateFocusBorder();
		}

		protected virtual void InvalidateFocusBorder()
		{
			Rectangle LBounds = DisplayRectangle;
			LBounds.Inflate(1, 1);
			Region LRegion = new Region(LBounds);
			LBounds.Inflate(-1, -1);
			LRegion.Exclude(LBounds);
			Invalidate(LRegion);
			LRegion.Dispose();
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			Rectangle LBounds = base.DisplayRectangle;
			LBounds.Height--;
			LBounds.Width--;

			int LTextWidth = 0;

			// Paint title
			if (Text != String.Empty)
			{
				// Measure the title
				StringFormat LFormat = GetFormat();
				LTextWidth = Size.Ceiling(AArgs.Graphics.MeasureString(Text, Font, LBounds.Width - (CTextOffset * 2), GetFormat())).Width;

				// Paint the title
				using (Brush LBrush = new SolidBrush(Enabled ? SystemColors.ControlText : SystemColors.GrayText))
				{
					AArgs.Graphics.DrawString
					(
						Text,
						Font,
						LBrush,
						new Rectangle
						(
							CTextOffset,
							0,
							LTextWidth,
							Font.Height
						),
						LFormat
					);
				}
			}

			// Paint the border
			using (Pen LPen = new Pen(Enabled ? SystemColors.ControlText : SystemColors.InactiveBorder))
			{
				if (Text != String.Empty)
				{
					int LTopLineY = LBounds.Y + (Font.Height / 2);
					AArgs.Graphics.DrawLines
					(
						LPen,
						new Point[] 
						{
							new Point(CTextOffset - CTextMargin, LTopLineY),
							new Point(LBounds.Left, LTopLineY),
							new Point(LBounds.Left, LBounds.Bottom),
							new Point(LBounds.Right, LBounds.Bottom),
							new Point(LBounds.Right, LTopLineY),
							new Point(LBounds.Left + CTextOffset + LTextWidth + CTextMargin, LTopLineY)
						}
					);
				}
				else
					AArgs.Graphics.DrawRectangle(LPen, LBounds);
			}

			// Paint the focus rectangle
			if (Focused)
			{
				LBounds = DisplayRectangle;
				LBounds.Inflate(1, 1);
				ControlPaint.DrawFocusRectangle(AArgs.Graphics, LBounds);
			}
		}

		#endregion
	}

}
