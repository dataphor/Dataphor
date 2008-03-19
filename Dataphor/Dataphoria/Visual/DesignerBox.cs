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
	public abstract class ShadowBox : Control
	{
		public const int CFadeInterval = 50;
		public const int CDefaultNormalDepth = 2;
		public static Color CShadowColor = Color.Black;

		public ShadowBox()
		{
			SuspendLayout();

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.ContainerControl, true);
			BackColor = Color.Transparent;
			FSurfaceColor = Color.White;
			FNormalDepth = CDefaultNormalDepth;
			FDepth = FNormalDepth;
			FTargetDepth = FDepth;

			ResumeLayout(false);
		}

		private Color FSurfaceColor;
		public Color SurfaceColor
		{
			get { return FSurfaceColor; }
			set
			{
				if (value != FSurfaceColor)
				{
					FSurfaceColor = value;
					Invalidate();
					SurfaceColorChanged();
				}
			}
		}

		protected virtual void SurfaceColorChanged() {}

		private System.Windows.Forms.Timer FFadeTimer;
		
		private int FDepth;
		public int Depth { get { return FDepth; } }

		private int FTargetDepth;

		private int FMaxDepth = 5;
		public int MaxDepth
		{
			get { return FMaxDepth; }
			set
			{
				if (FMaxDepth != value)
				{
					FMaxDepth = value;
					PerformLayout();
					Invalidate();
				}
			}
		}

		private int FNormalDepth;
		public int NormalDepth
		{
			get { return FNormalDepth; }
			set
			{
				if (value > FMaxDepth)
					value = FMaxDepth;
				if (FDepth == FNormalDepth)
					MakeDepth(value);
				FNormalDepth = value;
			}
		}

		public void MakeDepth(int ADepth)		//I've always wanted to say that!
		{
			if (ADepth != FTargetDepth)
			{
				FTargetDepth = ADepth;
				if (!IsHandleCreated)
					FDepth = FTargetDepth;
				else
				{
					StartFadeTimer();
					FadeStep();
				}
			}
		}

		private void StartFadeTimer()
		{
			if (FFadeTimer == null)
			{
				FFadeTimer = new System.Windows.Forms.Timer();
				FFadeTimer.Interval = CFadeInterval;
				FFadeTimer.Enabled = true;
				FFadeTimer.Tick += new EventHandler(FadeTimerTick);
			}
		}

		private void FadeStep()
		{
			if (FTargetDepth > FDepth)
				FDepth++;
			else
				FDepth--;

			if (FDepth == FTargetDepth)
			{
				FFadeTimer.Dispose();
				FFadeTimer = null;
			}

			Invalidate(false);
			Update();
		}

		private void FadeTimerTick(object ASender, EventArgs AArgs)
		{
			FadeStep();
		}

		protected void DrawShadowBox(Graphics AGraphics, Rectangle LBounds)
		{
			if (FDepth > 0)
			{
				using (Pen LPen = new Pen(CShadowColor))
				{
					Point LCorner = new Point(LBounds.Right, LBounds.Bottom);
					// Draw shadow
					for (int LFade = 1; LFade <= FDepth; LFade++)
					{
						LPen.Color = Color.FromArgb(170 - (150 / LFade), CShadowColor);
						AGraphics.DrawLines(LPen, new Point[] {new Point(LCorner.X, LBounds.Top), LCorner, new Point(LBounds.Left, LCorner.Y)});
						LCorner.X--;
						LCorner.Y--;
					}
				}
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				LBounds.Width -= FMaxDepth;
				LBounds.Height -= FMaxDepth;
				return LBounds;
			}
		}
	}
	
	public class FloatingBox : ShadowBox, IZoomable
	{
		public FloatingBox()
		{
			SuspendLayout();
			Size = new Size(120, 25);
			SetStyle(ControlStyles.FixedHeight, true);
			SetStyle(ControlStyles.FixedWidth, true);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			ForeColor = Color.Navy;
			FHighlightColor = Color.Blue;
			ResumeLayout(false);
		}

		private Color FHighlightColor;
		public Color HighlightColor
		{
			get { return FHighlightColor; }
			set
			{
				FHighlightColor = value;
				if (FHighlight)
					Invalidate();
			}
		}

		private float FRoundRadius = 0f;
		public float RoundRadius
		{
			get { return FRoundRadius; }
			set
			{
				if (FRoundRadius != value)
				{
					FRoundRadius = value;
					Invalidate();
				}
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				LBounds.Inflate(-1, -1);	// Account for border
				return LBounds;
			}
		}

		private bool FHighlight;
		public bool Highlight
		{
			get { return FHighlight; }
			set
			{
				if (FHighlight != value)
				{
					FHighlight = value;
					Invalidate(false);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			Rectangle LRect = base.DisplayRectangle;

			Rectangle LShadowRect = LRect;
			LShadowRect.X += Depth;
			LShadowRect.Y += Depth;

			using (Pen LPen = new Pen(CShadowColor))
			{
				using (SolidBrush LBrush = new SolidBrush(SurfaceColor))
				{
					if (FRoundRadius > 0)
					{
						// Draw shadow
						LBrush.Color = Color.FromArgb(((MaxDepth - Depth) * (100 / MaxDepth)) + 127, CShadowColor);
						using (GraphicsPath LPath = GraphicsUtility.GetRoundedRectPath(LShadowRect, FRoundRadius))
							AArgs.Graphics.FillPath(LBrush, LPath);

						LBrush.Color = SurfaceColor;

						using (GraphicsPath LPath = GraphicsUtility.GetRoundedRectPath(LRect, FRoundRadius))
						{
							// Fill the inside
							AArgs.Graphics.FillPath(LBrush, LPath);

							// Draw outline
							if (FHighlight)
								LPen.Color = FHighlightColor;
							else
								LPen.Color = ForeColor;
							AArgs.Graphics.DrawPath(LPen, LPath);
						}
					}
					else
					{
						DrawShadowBox(AArgs.Graphics, LShadowRect);

						// Fill the inside
						AArgs.Graphics.FillRectangle(LBrush, LRect);

						// Draw outline
						if (FHighlight)
							LPen.Color = FHighlightColor;
						else
							LPen.Color = ForeColor;
						AArgs.Graphics.DrawRectangle(LPen, LRect);
					}
				}
			}
			// Adjust the clipping rect for descendant rendering

			LRect.Inflate(-1, -1);
			AArgs.Graphics.SetClip(LRect, System.Drawing.Drawing2D.CombineMode.Intersect);
		}


		#region ZoomIn & ZoomOut

		public virtual bool CanZoomIn()
		{
			return false;
		}

		public virtual void ZoomIn()
		{
		}

		public virtual bool CanZoomOut()
		{
			return false;
		}

		public virtual void ZoomOut()
		{
		}

		protected override void OnDoubleClick(EventArgs AArgs)
		{
			base.OnDoubleClick(AArgs);
			if (CanZoomIn())
				ZoomIn();
		}

		#endregion
	}

	public class DesignerBox : FloatingBox
	{
		protected override void OnEnter(EventArgs AArgs)
		{
			base.OnEnter(AArgs);
			MakeDepth(MaxDepth);
		}

		protected override void OnLeave(EventArgs AArgs)
		{
			base.OnLeave(AArgs);
			MakeDepth(NormalDepth);
		}

		protected override void OnClick(EventArgs AArgs)
		{
			base.OnClick(AArgs);
			Focus();
		}

		protected override void OnMouseEnter(EventArgs AArgs)
		{
			base.OnMouseEnter(AArgs);
			Highlight = true;
		}

		protected override void OnMouseLeave(EventArgs AArgs)
		{
			base.OnMouseLeave(AArgs);
			Highlight = false;
		}
	}

	public class TextDesignerBox : DesignerBox
	{
		public const int CDefaultTextHPadding = 4;
		public const int CDefaultTextVPadding = 2;

		public TextDesignerBox()
		{
			SetStyle(ControlStyles.CacheText, true);
		}

		private VerticalAlignment FTextVAlign = VerticalAlignment.Middle;
		public VerticalAlignment TextVAlign
		{
			get { return FTextVAlign; }
			set
			{
				if (FTextVAlign != value)
				{
					FTextVAlign = value;
					Invalidate();
				}
			}
		}

		private int FTextVPadding = CDefaultTextVPadding;
		public int TextVPadding
		{
			get { return FTextVPadding; }
			set
			{
				if (FTextVPadding != value)
				{
					FTextVPadding = value;
					Invalidate();
				}
			}
		}

		private HorizontalAlignment FTextHAlign = HorizontalAlignment.Center;
		public HorizontalAlignment TextHAlign
		{
			get { return FTextHAlign; }
			set
			{
				if (FTextHAlign != value)
				{
					FTextHAlign = value;
					Invalidate();
				}
			}
		}

		private int FTextHPadding = CDefaultTextHPadding;
		public int TextHPadding
		{
			get { return FTextHPadding; }
			set
			{
				if (FTextHPadding != value)
				{
					FTextHPadding = value;
					Invalidate();
				}
			}
		}

		/// <summary> Returns the total, padded area for the text. </summary>
		protected virtual Rectangle GetTextBounds()
		{
			Rectangle LBounds = DisplayRectangle;
			switch (FTextVAlign)
			{
				case VerticalAlignment.Top : return new Rectangle(LBounds.X, LBounds.Y + FTextVPadding, LBounds.Width, Font.Height);
				case VerticalAlignment.Middle : return new Rectangle(LBounds.X, LBounds.Y + (LBounds.Height / 2) - (Font.Height / 2), LBounds.Width, Font.Height);
				default : return new Rectangle(LBounds.X, LBounds.Bottom - FTextVPadding - Font.Height, LBounds.Width, Font.Height);
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);

			using (SolidBrush LBrush = new SolidBrush(Highlight ? HighlightColor : ForeColor))
			{
				AArgs.Graphics.DrawString
				(
					Text, 
					Font, 
					LBrush,
					GetTextBounds(),
					GetStringFormat()
				);
			}
		}

		private StringFormat FStringFormat;
		protected virtual StringFormat GetStringFormat()
		{
			if (FStringFormat == null)
			{
				FStringFormat = new StringFormat();
				FStringFormat.Trimming = StringTrimming.EllipsisCharacter;
			}
			switch (FTextHAlign)
			{
				case HorizontalAlignment.Left : FStringFormat.Alignment = StringAlignment.Near; break;
				case HorizontalAlignment.Center : FStringFormat.Alignment = StringAlignment.Center; break;
				case HorizontalAlignment.Right : FStringFormat.Alignment = StringAlignment.Far; break;
			}
			return FStringFormat;
		}
	}

	public enum VerticalAlignment
	{
		Top,
		Middle,
		Bottom
	}

	public sealed class GraphicsUtility
	{
		public static GraphicsPath GetRoundedRectPath(Rectangle ABounds, float ARadius)
		{
			GraphicsPath LResult;
			if (ARadius >= (Math.Min(ABounds.Width, ABounds.Height) / 2))
				LResult = GetCapsulePath(ABounds);
			else
			{
				LResult = new GraphicsPath();
				if (ARadius <= 0f)
					LResult.AddRectangle(ABounds);
				else
				{
					float LDiameter = ARadius * 2f;
					RectangleF LArcRect = new RectangleF((PointF)ABounds.Location, new SizeF(LDiameter, LDiameter));
				
					LResult.AddArc(LArcRect, 180, 90);
					LArcRect.X = ABounds.Right - LDiameter;
					LResult.AddArc(LArcRect, 270, 90);
					LArcRect.Y = ABounds.Bottom - LDiameter;
					LResult.AddArc(LArcRect, 0, 90);
					LArcRect.X = ABounds.X;
					LResult.AddArc(LArcRect, 90, 90);

				}
				LResult.CloseFigure();
			}
			return LResult;
		}

		private static GraphicsPath GetCapsulePath(Rectangle ABounds)
		{
			GraphicsPath LResult = new GraphicsPath();
			if (ABounds.Width > ABounds.Height)
			{
				float LDiameter = ABounds.Height;
				RectangleF LArcRect = new RectangleF((PointF)ABounds.Location, new SizeF(LDiameter, LDiameter));
				LResult.AddArc(LArcRect, 90, 180);
				LArcRect.X = ABounds.Right - LDiameter;
				LResult.AddArc(LArcRect, 270, 180);
			}
			else if (ABounds.Height < ABounds.Width)
			{
				float LDiameter = ABounds.Width;
				RectangleF LArcRect = new RectangleF((PointF)ABounds.Location, new SizeF(LDiameter, LDiameter));
				LResult.AddArc(LArcRect, 180, 180);
				LArcRect.Y = ABounds.Bottom - LDiameter;
				LResult.AddArc(LArcRect, 0, 180);
			}
			else
			{
				LResult.AddEllipse(ABounds);
			}
			LResult.CloseFigure();
			return LResult;
		}
	}
}
