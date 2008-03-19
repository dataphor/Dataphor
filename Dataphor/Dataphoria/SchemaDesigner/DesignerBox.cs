/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public class DesignerBox : Control
	{
		public const int CMaxDepth = 5;
		public const int CFadeInterval = 50;
		public const int CDefaultNormalDepth = 2;
		public static Color CShadowColor = Color.Black;

		public DesignerBox()
		{
			Size = new Size(120, 25);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.FixedHeight, true);
			SetStyle(ControlStyles.FixedWidth, true);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BackColor = Color.Transparent;
			ForeColor = Color.Navy;
			FSurfaceColor = Color.White;
			FHighlightColor = Color.Blue;
			FNormalDepth = CDefaultNormalDepth;
			FDepth = FNormalDepth;
			FTargetDepth = FDepth;
		}

		private Color FSurfaceColor;
		public Color SurfaceColor
		{
			get { return FSurfaceColor; }
			set
			{
				FSurfaceColor = value;
				Invalidate();
			}
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

		/// <summary> Essentially gets the client area, though the Windows client area mechanism is not used. </summary>
		protected Rectangle GetInnerBounds()
		{
			Size LClientSize = ClientSize;
			return new Rectangle
			(
				1,
				1,
				LClientSize.Width - (CMaxDepth + 2),
				LClientSize.Height - (CMaxDepth + 2)
			);
		}

		protected bool FHighlight;

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			Rectangle LRect = new Rectangle(Point.Empty, ClientSize);
			LRect.Width -= CMaxDepth;
			LRect.Height -= CMaxDepth;

			Rectangle LShadowRect = LRect;
			LShadowRect.X += FDepth;
			LShadowRect.Y += FDepth;

			using (Pen LPen = new Pen(Color.Black))
			{
				// TODO: Optimize painting for clipping

				using (SolidBrush LBrush = new SolidBrush(FSurfaceColor))
				{
					if (FRoundRadius > 0)
					{
						using (GraphicsPath LPath = GraphicsUtility.GetRoundedRectPath(new Rectangle(Point.Empty, LShadowRect.Size), FRoundRadius))
						{
							if (FDepth > 0)
							{
								// TODO: The right/bottom of the shadow is being clipped

								// Draw shadow
								LBrush.Color = Color.FromArgb(((CMaxDepth - FDepth) * (100 / CMaxDepth)) + 127, CShadowColor);
								GraphicsState LState = AArgs.Graphics.Save();
								AArgs.Graphics.TranslateTransform(FDepth, FDepth);
								AArgs.Graphics.FillPath(LBrush, LPath);
								AArgs.Graphics.Restore(LState);
								LBrush.Color = FSurfaceColor;
							}

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
						if (FDepth > 0)
						{
							// Draw shadow
							for (int LFade = 1; LFade <= FDepth; LFade++)
							{
								LPen.Color = Color.FromArgb(170 - (150 / LFade), CShadowColor);
								AArgs.Graphics.DrawRectangle(LPen, LShadowRect);
								LShadowRect.Inflate(-1, -1);
							}
						}

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

		private System.Windows.Forms.Timer FFadeTimer;
		private int FDepth;
		private int FTargetDepth;

		private int FNormalDepth;
		public int NormalDepth
		{
			get { return FNormalDepth; }
			set
			{
				if (value > CMaxDepth)
					value = CMaxDepth;
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
				StartFadeTimer();
				FadeStep();
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

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			MakeDepth(CMaxDepth);
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
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
			FHighlight = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs AArgs)
		{
			base.OnMouseLeave(AArgs);
			FHighlight = false;
			Invalidate();
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
		protected Rectangle GetTextBounds()
		{
			Rectangle LBounds = GetInnerBounds();
			int LTextHeight = Font.Height + (FTextVPadding * 2);
			switch (FTextVAlign)
			{
				case VerticalAlignment.Middle :
					LBounds.Y += (LBounds.Height / 2) - (LTextHeight / 2);
					break;
				case VerticalAlignment.Bottom :
					LBounds.Y += LBounds.Height - LTextHeight;
					break;
			}
			LBounds.Height = LTextHeight;
			return LBounds;
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);

			GraphicsState LState = AArgs.Graphics.Save();

			using (SolidBrush LBrush = new SolidBrush(FHighlight ? HighlightColor : ForeColor))
			{
				Rectangle LRect = GetTextBounds();
				LRect.X += FTextHPadding;
				LRect.Y += FTextVPadding;
				LRect.Width -= FTextHPadding * 2;
				LRect.Height -= FTextVPadding * 2;
				AArgs.Graphics.SetClip(LRect, CombineMode.Intersect);

				string LDrawText = Text;	// TODO: Show elipsis when text overflows size
				
				Size LDrawTextSize = AArgs.Graphics.MeasureString(LDrawText, Font).ToSize();
				switch (FTextHAlign)
				{
					case HorizontalAlignment.Center :
						LRect.X += (LRect.Width / 2) - (LDrawTextSize.Width / 2);
						break;
					case HorizontalAlignment.Right :
						LRect.X += LRect.Width - LDrawTextSize.Width;
						break;
				}
				if (AArgs.Graphics.Clip.IsVisible(new Rectangle(LRect.Location, LDrawTextSize)))
					AArgs.Graphics.DrawString
					(
						LDrawText, 
						Font, 
						LBrush, 
						LRect.X, 
						LRect.Y
					);
			}

			AArgs.Graphics.Restore(LState);
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
