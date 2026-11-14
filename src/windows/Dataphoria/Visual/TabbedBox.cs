/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public class TabbedBox : ShadowBox
	{
		public const int TitleVerticalPad = 2;
		public const int HeaderOffset = 6;

		public TabbedBox()
		{
			SuspendLayout();
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.FixedHeight, false);
			SetStyle(ControlStyles.FixedWidth, false);
			SetStyle(ControlStyles.ResizeRedraw, false);
			SetStyle(ControlStyles.Selectable, false);
			SetStyle(ControlStyles.StandardClick, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.CacheText, true);
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			Size = new Size(400, 60);
			
			ResumeLayout(false);
		}

		private StringFormat _titleStringFormat;
		protected virtual StringFormat GetTitleStringFormat()
		{
			if (_titleStringFormat == null)
			{
				_titleStringFormat = new StringFormat();
				_titleStringFormat.Trimming = StringTrimming.EllipsisCharacter;
			}
			return _titleStringFormat;
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			Rectangle bounds = base.DisplayRectangle;

			int headerHeight = (TitleVerticalPad * 2) + Font.Height;
			int headerTextWidth = Math.Min(bounds.Width - (HeaderOffset + (headerHeight * 2)), Size.Ceiling(args.Graphics.MeasureString(Text, Font)).Width);
			Point[] points = 
				new Point[] 
				{ 
					new Point(bounds.X, bounds.Y + headerHeight), 
					new Point(bounds.X + HeaderOffset, bounds.Y + headerHeight), 
					new Point(bounds.X + HeaderOffset + headerHeight, bounds.Y),
					new Point(bounds.X + HeaderOffset + headerHeight + headerTextWidth, bounds.Y),
					new Point(bounds.X + HeaderOffset + headerHeight + headerTextWidth + headerHeight, bounds.Y + headerHeight),
					new Point(bounds.Right, bounds.Y + headerHeight),
					new Point(bounds.Right, bounds.Bottom),
					new Point(bounds.X, bounds.Bottom),
					new Point(bounds.X, bounds.Y + headerHeight)
				};
			using (Brush backBrush = new SolidBrush(SurfaceColor))
			{
				args.Graphics.FillPolygon(backBrush, points);
			}

			using (Pen pen = new Pen(ForeColor))
			{
				args.Graphics.DrawPolygon(pen, points);
			}

			using (Brush textBrush = new SolidBrush(ForeColor))
			{
				args.Graphics.DrawString(Text, Font, textBrush, bounds.X + HeaderOffset + headerHeight, bounds.Y + TitleVerticalPad, GetTitleStringFormat());
			}

			bounds.X += Depth;
			bounds.Y += Depth + headerHeight;
			bounds.Height -= headerHeight;
			DrawShadowBox(args.Graphics, bounds);
		}									

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				int headerHeight = (TitleVerticalPad * 2) + Font.Height;
				bounds.Y += headerHeight;
				bounds.Height -= headerHeight;
				bounds.Inflate(-1, -1);
				return bounds;
			}
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);
			if (Controls.Count == 1)
				Controls[0].Bounds = DisplayRectangle;
		}
	}
}
