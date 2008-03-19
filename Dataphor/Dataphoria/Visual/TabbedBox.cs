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
		public const int CTitleVerticalPad = 2;
		public const int CHeaderOffset = 6;

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

		private StringFormat FTitleStringFormat;
		protected virtual StringFormat GetTitleStringFormat()
		{
			if (FTitleStringFormat == null)
			{
				FTitleStringFormat = new StringFormat();
				FTitleStringFormat.Trimming = StringTrimming.EllipsisCharacter;
			}
			return FTitleStringFormat;
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			Rectangle LBounds = base.DisplayRectangle;

			int LHeaderHeight = (CTitleVerticalPad * 2) + Font.Height;
			int LHeaderTextWidth = Math.Min(LBounds.Width - (CHeaderOffset + (LHeaderHeight * 2)), Size.Ceiling(AArgs.Graphics.MeasureString(Text, Font)).Width);
			Point[] LPoints = 
				new Point[] 
				{ 
					new Point(LBounds.X, LBounds.Y + LHeaderHeight), 
					new Point(LBounds.X + CHeaderOffset, LBounds.Y + LHeaderHeight), 
					new Point(LBounds.X + CHeaderOffset + LHeaderHeight, LBounds.Y),
					new Point(LBounds.X + CHeaderOffset + LHeaderHeight + LHeaderTextWidth, LBounds.Y),
					new Point(LBounds.X + CHeaderOffset + LHeaderHeight + LHeaderTextWidth + LHeaderHeight, LBounds.Y + LHeaderHeight),
					new Point(LBounds.Right, LBounds.Y + LHeaderHeight),
					new Point(LBounds.Right, LBounds.Bottom),
					new Point(LBounds.X, LBounds.Bottom),
					new Point(LBounds.X, LBounds.Y + LHeaderHeight)
				};
			using (Brush LBackBrush = new SolidBrush(SurfaceColor))
			{
				AArgs.Graphics.FillPolygon(LBackBrush, LPoints);
			}

			using (Pen LPen = new Pen(ForeColor))
			{
				AArgs.Graphics.DrawPolygon(LPen, LPoints);
			}

			using (Brush LTextBrush = new SolidBrush(ForeColor))
			{
				AArgs.Graphics.DrawString(Text, Font, LTextBrush, LBounds.X + CHeaderOffset + LHeaderHeight, LBounds.Y + CTitleVerticalPad, GetTitleStringFormat());
			}

			LBounds.X += Depth;
			LBounds.Y += Depth + LHeaderHeight;
			LBounds.Height -= LHeaderHeight;
			DrawShadowBox(AArgs.Graphics, LBounds);
		}									

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				int LHeaderHeight = (CTitleVerticalPad * 2) + Font.Height;
				LBounds.Y += LHeaderHeight;
				LBounds.Height -= LHeaderHeight;
				LBounds.Inflate(-1, -1);
				return LBounds;
			}
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);
			if (Controls.Count == 1)
				Controls[0].Bounds = DisplayRectangle;
		}
	}
}
