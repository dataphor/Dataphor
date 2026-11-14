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
		public const int TextOffset = 8;
		public const int TextMargin = 2;

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
		
		protected virtual void OnClearValue(EventArgs args)
		{
			if (ClearValue != null)
				ClearValue(this, args);
		}
		
		protected void PerformClearValue()
		{
			OnClearValue(EventArgs.Empty);
		}
		
		#region Keyboard & Mouse

		protected override void OnClick(EventArgs args)
		{
			base.OnClick(args);
			Focus();
		}

		protected override bool ProcessMnemonic(char charValue)
		{
			if (Control.IsMnemonic(charValue, Text))
			{
				Focus();
				return true;
			}
			else
				return base.ProcessMnemonic(charValue);
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			if (key == (Keys.Control | Keys.Back))
			{
				PerformClearValue();
				return true;
			}
			else
				return base.ProcessDialogKey(key);
		}

		protected override bool ProcessKeyPreview(ref Message message)
		{
			const int WM_KEYUP = 0x101;
			if (message.Msg == WM_KEYUP && (((Keys)((int)((long)message.WParam))) | ModifierKeys) == (Keys.Control | Keys.Back))
			{
				PerformClearValue();
				return true;
			}
			else
				return base.ProcessKeyPreview(ref message);
		}

		#endregion

		#region Layout

		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);

			// Layout the button
			Rectangle bounds = base.DisplayRectangle;
			int deltaY = (Text != String.Empty) ? (Font.Height / 2) + 1 : 0;
			Button.Bounds =
				new Rectangle
				(
					bounds.Right - (1 + Button.Width),
					bounds.Y + deltaY,
					Button.Width,
					bounds.Height - (deltaY + 1)
				);
		}

		[Browsable(false)]
		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				bounds.Inflate(-2, -2);
				bounds.Width -= Button.Width;
				if (Text != String.Empty)
				{
					bounds.Y += Font.Height;
					bounds.Height -= Font.Height;
				}
				return bounds;
			}
		}

		#endregion

		#region Painting

		private static StringFormat _format;
		private static StringFormat GetFormat()
		{
			if (_format == null)
			{
				_format = new StringFormat(StringFormat.GenericDefault);
				_format.Trimming = StringTrimming.EllipsisWord;
				_format.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
			}
			return _format;
		}

		protected override void OnTextChanged(EventArgs args)
		{
			base.OnTextChanged(args);
			PerformLayout(this, "Height");
			Invalidate();
		}

		protected override void OnLostFocus(EventArgs args)
		{
			base.OnLostFocus(args);
			InvalidateFocusBorder();
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			InvalidateFocusBorder();
		}

		protected virtual void InvalidateFocusBorder()
		{
			Rectangle bounds = DisplayRectangle;
			bounds.Inflate(1, 1);
			Region region = new Region(bounds);
			bounds.Inflate(-1, -1);
			region.Exclude(bounds);
			Invalidate(region);
			region.Dispose();
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			Rectangle bounds = base.DisplayRectangle;
			bounds.Height--;
			bounds.Width--;

			int textWidth = 0;

			// Paint title
			if (Text != String.Empty)
			{
				// Measure the title
				StringFormat format = GetFormat();
				textWidth = Size.Ceiling(args.Graphics.MeasureString(Text, Font, bounds.Width - (TextOffset * 2), GetFormat())).Width;

				// Paint the title
				using (Brush brush = new SolidBrush(Enabled ? SystemColors.ControlText : SystemColors.GrayText))
				{
					args.Graphics.DrawString
					(
						Text,
						Font,
						brush,
						new Rectangle
						(
							TextOffset,
							0,
							textWidth,
							Font.Height
						),
						format
					);
				}
			}

			// Paint the border
			using (Pen pen = new Pen(Enabled ? SystemColors.ControlText : SystemColors.InactiveBorder))
			{
				if (Text != String.Empty)
				{
					int topLineY = bounds.Y + (Font.Height / 2);
					args.Graphics.DrawLines
					(
						pen,
						new Point[] 
						{
							new Point(TextOffset - TextMargin, topLineY),
							new Point(bounds.Left, topLineY),
							new Point(bounds.Left, bounds.Bottom),
							new Point(bounds.Right, bounds.Bottom),
							new Point(bounds.Right, topLineY),
							new Point(bounds.Left + TextOffset + textWidth + TextMargin, topLineY)
						}
					);
				}
				else
					args.Graphics.DrawRectangle(pen, bounds);
			}

			// Paint the focus rectangle
			if (Focused)
			{
				bounds = DisplayRectangle;
				bounds.Inflate(1, 1);
				ControlPaint.DrawFocusRectangle(args.Graphics, bounds);
			}
		}

		#endregion
	}

}
