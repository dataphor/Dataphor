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
		public const int FadeInterval = 50;
		public const int DefaultNormalDepth = 2;
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
			_surfaceColor = Color.White;
			_normalDepth = DefaultNormalDepth;
			_depth = _normalDepth;
			_targetDepth = _depth;

			ResumeLayout(false);
		}

		private Color _surfaceColor;
		public Color SurfaceColor
		{
			get { return _surfaceColor; }
			set
			{
				if (value != _surfaceColor)
				{
					_surfaceColor = value;
					Invalidate();
					SurfaceColorChanged();
				}
			}
		}

		protected virtual void SurfaceColorChanged() {}

		private System.Windows.Forms.Timer _fadeTimer;
		
		private int _depth;
		public int Depth { get { return _depth; } }

		private int _targetDepth;

		private int _maxDepth = 5;
		public int MaxDepth
		{
			get { return _maxDepth; }
			set
			{
				if (_maxDepth != value)
				{
					_maxDepth = value;
					PerformLayout();
					Invalidate();
				}
			}
		}

		private int _normalDepth;
		public int NormalDepth
		{
			get { return _normalDepth; }
			set
			{
				if (value > _maxDepth)
					value = _maxDepth;
				if (_depth == _normalDepth)
					MakeDepth(value);
				_normalDepth = value;
			}
		}

		public void MakeDepth(int depth)		//I've always wanted to say that!
		{
			if (depth != _targetDepth)
			{
				_targetDepth = depth;
				if (!IsHandleCreated)
					_depth = _targetDepth;
				else
				{
					StartFadeTimer();
					FadeStep();
				}
			}
		}

		private void StartFadeTimer()
		{
			if (_fadeTimer == null)
			{
				_fadeTimer = new System.Windows.Forms.Timer();
				_fadeTimer.Interval = FadeInterval;
				_fadeTimer.Enabled = true;
				_fadeTimer.Tick += new EventHandler(FadeTimerTick);
			}
		}

		private void FadeStep()
		{
			if (_targetDepth > _depth)
				_depth++;
			else
				_depth--;

			if (_depth == _targetDepth)
			{
				_fadeTimer.Dispose();
				_fadeTimer = null;
			}

			Invalidate(false);
			Update();
		}

		private void FadeTimerTick(object sender, EventArgs args)
		{
			FadeStep();
		}

		protected void DrawShadowBox(Graphics graphics, Rectangle LBounds)
		{
			if (_depth > 0)
			{
				using (Pen pen = new Pen(CShadowColor))
				{
					Point corner = new Point(LBounds.Right, LBounds.Bottom);
					// Draw shadow
					for (int fade = 1; fade <= _depth; fade++)
					{
						pen.Color = Color.FromArgb(170 - (150 / fade), CShadowColor);
						graphics.DrawLines(pen, new Point[] {new Point(corner.X, LBounds.Top), corner, new Point(LBounds.Left, corner.Y)});
						corner.X--;
						corner.Y--;
					}
				}
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				bounds.Width -= _maxDepth;
				bounds.Height -= _maxDepth;
				return bounds;
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
			_highlightColor = Color.Blue;
			ResumeLayout(false);
		}

		private Color _highlightColor;
		public Color HighlightColor
		{
			get { return _highlightColor; }
			set
			{
				_highlightColor = value;
				if (_highlight)
					Invalidate();
			}
		}

		private float _roundRadius = 0f;
		public float RoundRadius
		{
			get { return _roundRadius; }
			set
			{
				if (_roundRadius != value)
				{
					_roundRadius = value;
					Invalidate();
				}
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				bounds.Inflate(-1, -1);	// Account for border
				return bounds;
			}
		}

		private bool _highlight;
		public bool Highlight
		{
			get { return _highlight; }
			set
			{
				if (_highlight != value)
				{
					_highlight = value;
					Invalidate(false);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			Rectangle rect = base.DisplayRectangle;

			Rectangle shadowRect = rect;
			shadowRect.X += Depth;
			shadowRect.Y += Depth;

			using (Pen pen = new Pen(CShadowColor))
			{
				using (SolidBrush brush = new SolidBrush(SurfaceColor))
				{
					if (_roundRadius > 0)
					{
						// Draw shadow
						brush.Color = Color.FromArgb(((MaxDepth - Depth) * (100 / MaxDepth)) + 127, CShadowColor);
						using (GraphicsPath path = GraphicsUtility.GetRoundedRectPath(shadowRect, _roundRadius))
							args.Graphics.FillPath(brush, path);

						brush.Color = SurfaceColor;

						using (GraphicsPath path = GraphicsUtility.GetRoundedRectPath(rect, _roundRadius))
						{
							// Fill the inside
							args.Graphics.FillPath(brush, path);

							// Draw outline
							if (_highlight)
								pen.Color = _highlightColor;
							else
								pen.Color = ForeColor;
							args.Graphics.DrawPath(pen, path);
						}
					}
					else
					{
						DrawShadowBox(args.Graphics, shadowRect);

						// Fill the inside
						args.Graphics.FillRectangle(brush, rect);

						// Draw outline
						if (_highlight)
							pen.Color = _highlightColor;
						else
							pen.Color = ForeColor;
						args.Graphics.DrawRectangle(pen, rect);
					}
				}
			}
			// Adjust the clipping rect for descendant rendering

			rect.Inflate(-1, -1);
			args.Graphics.SetClip(rect, System.Drawing.Drawing2D.CombineMode.Intersect);
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

		protected override void OnDoubleClick(EventArgs args)
		{
			base.OnDoubleClick(args);
			if (CanZoomIn())
				ZoomIn();
		}

		#endregion
	}

	public class DesignerBox : FloatingBox
	{
		protected override void OnEnter(EventArgs args)
		{
			base.OnEnter(args);
			MakeDepth(MaxDepth);
		}

		protected override void OnLeave(EventArgs args)
		{
			base.OnLeave(args);
			MakeDepth(NormalDepth);
		}

		protected override void OnClick(EventArgs args)
		{
			base.OnClick(args);
			Focus();
		}

		protected override void OnMouseEnter(EventArgs args)
		{
			base.OnMouseEnter(args);
			Highlight = true;
		}

		protected override void OnMouseLeave(EventArgs args)
		{
			base.OnMouseLeave(args);
			Highlight = false;
		}
	}

	public class TextDesignerBox : DesignerBox
	{
		public const int DefaultTextHPadding = 4;
		public const int DefaultTextVPadding = 2;

		public TextDesignerBox()
		{
			SetStyle(ControlStyles.CacheText, true);
		}

		private VerticalAlignment _textVAlign = VerticalAlignment.Middle;
		public VerticalAlignment TextVAlign
		{
			get { return _textVAlign; }
			set
			{
				if (_textVAlign != value)
				{
					_textVAlign = value;
					Invalidate();
				}
			}
		}

		private int _textVPadding = DefaultTextVPadding;
		public int TextVPadding
		{
			get { return _textVPadding; }
			set
			{
				if (_textVPadding != value)
				{
					_textVPadding = value;
					Invalidate();
				}
			}
		}

		private HorizontalAlignment _textHAlign = HorizontalAlignment.Center;
		public HorizontalAlignment TextHAlign
		{
			get { return _textHAlign; }
			set
			{
				if (_textHAlign != value)
				{
					_textHAlign = value;
					Invalidate();
				}
			}
		}

		private int _textHPadding = DefaultTextHPadding;
		public int TextHPadding
		{
			get { return _textHPadding; }
			set
			{
				if (_textHPadding != value)
				{
					_textHPadding = value;
					Invalidate();
				}
			}
		}

		/// <summary> Returns the total, padded area for the text. </summary>
		protected virtual Rectangle GetTextBounds()
		{
			Rectangle bounds = DisplayRectangle;
			switch (_textVAlign)
			{
				case VerticalAlignment.Top : return new Rectangle(bounds.X, bounds.Y + _textVPadding, bounds.Width, Font.Height);
				case VerticalAlignment.Middle : return new Rectangle(bounds.X, bounds.Y + (bounds.Height / 2) - (Font.Height / 2), bounds.Width, Font.Height);
				default : return new Rectangle(bounds.X, bounds.Bottom - _textVPadding - Font.Height, bounds.Width, Font.Height);
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);

			using (SolidBrush brush = new SolidBrush(Highlight ? HighlightColor : ForeColor))
			{
				args.Graphics.DrawString
				(
					Text, 
					Font, 
					brush,
					GetTextBounds(),
					GetStringFormat()
				);
			}
		}

		private StringFormat _stringFormat;
		protected virtual StringFormat GetStringFormat()
		{
			if (_stringFormat == null)
			{
				_stringFormat = new StringFormat();
				_stringFormat.Trimming = StringTrimming.EllipsisCharacter;
			}
			switch (_textHAlign)
			{
				case HorizontalAlignment.Left : _stringFormat.Alignment = StringAlignment.Near; break;
				case HorizontalAlignment.Center : _stringFormat.Alignment = StringAlignment.Center; break;
				case HorizontalAlignment.Right : _stringFormat.Alignment = StringAlignment.Far; break;
			}
			return _stringFormat;
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
		public static GraphicsPath GetRoundedRectPath(Rectangle bounds, float radius)
		{
			GraphicsPath result;
			if (radius >= (Math.Min(bounds.Width, bounds.Height) / 2))
				result = GetCapsulePath(bounds);
			else
			{
				result = new GraphicsPath();
				if (radius <= 0f)
					result.AddRectangle(bounds);
				else
				{
					float diameter = radius * 2f;
					RectangleF arcRect = new RectangleF((PointF)bounds.Location, new SizeF(diameter, diameter));
				
					result.AddArc(arcRect, 180, 90);
					arcRect.X = bounds.Right - diameter;
					result.AddArc(arcRect, 270, 90);
					arcRect.Y = bounds.Bottom - diameter;
					result.AddArc(arcRect, 0, 90);
					arcRect.X = bounds.X;
					result.AddArc(arcRect, 90, 90);

				}
				result.CloseFigure();
			}
			return result;
		}

		private static GraphicsPath GetCapsulePath(Rectangle bounds)
		{
			GraphicsPath result = new GraphicsPath();
			if (bounds.Width > bounds.Height)
			{
				float diameter = bounds.Height;
				RectangleF arcRect = new RectangleF((PointF)bounds.Location, new SizeF(diameter, diameter));
				result.AddArc(arcRect, 90, 180);
				arcRect.X = bounds.Right - diameter;
				result.AddArc(arcRect, 270, 180);
			}
			else if (bounds.Height < bounds.Width)
			{
				float diameter = bounds.Width;
				RectangleF arcRect = new RectangleF((PointF)bounds.Location, new SizeF(diameter, diameter));
				result.AddArc(arcRect, 180, 180);
				arcRect.Y = bounds.Bottom - diameter;
				result.AddArc(arcRect, 0, 180);
			}
			else
			{
				result.AddEllipse(bounds);
			}
			result.CloseFigure();
			return result;
		}
	}
}
