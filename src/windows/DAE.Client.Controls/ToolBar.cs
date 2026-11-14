/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Drawing;
	using System.Reflection;
	using System.Windows.Forms;

	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.ToolBarButton), "Icons.ToolBarButton.bmp")]
	public class ToolBarButton : Button
	{
		public const int WidthPadding = 10;
		public const int ImagePadding = 0;

		public ToolBarButton() : base()
		{
			InitializeControl();
		}

		public ToolBarButton(string text, EventHandler click) : base()
		{
			InitializeControl();
			Text = text;
			Click += click;
		}

		private void InitializeControl()
		{
			CausesValidation = false;
			ImageAlign = ContentAlignment.MiddleLeft;
			TextAlign = ContentAlignment.MiddleRight;
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Selectable, false);
			UpdateSize();
		}

		protected void UpdateSize()
		{
			if (IsHandleCreated)
			{
				using (Graphics graphics = this.CreateGraphics())
					Width = WidthPadding + (Image == null ? 0 : Image.Width + ImagePadding) + Size.Ceiling(graphics.MeasureString(Text, Font)).Width;
			}
		}

		protected override void OnHandleCreated(EventArgs args)
		{
			base.OnHandleCreated(args);
			UpdateSize();
		}

		protected override void OnTextChanged(EventArgs args)
		{
			base.OnTextChanged(args);
			UpdateSize();
		}

		public new Image Image
		{
			get { return base.Image; }
			set
			{
				base.Image = value;
				UpdateSize();
			}
		}

		// TODO: Update size after image is changed (no OnImageChanged and Image prop not virtual)

		protected override bool ProcessMnemonic(char charCode)
		{
			if (base.ProcessMnemonic(charCode))
			{
				OnClick(EventArgs.Empty);
				return true;
			}
			return false;
		}
	}

	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.ToolBar),"Icons.ToolBar.bmp")]
	public class ToolBar : Control
	{
		public ToolBar() : base()
		{
			CausesValidation = false;
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.Selectable, false);
			SetStyle(ControlStyles.ContainerControl, true);
			Height = 24;
			Dock = DockStyle.Top;
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			//base.OnLayout();
			int offset = 0;
			if ((Dock == DockStyle.Left) || (Dock == DockStyle.Right))
			{
				foreach (Control child in Controls)
				{
					if (child.Visible)
					{
						child.Top = offset;
						child.Width = ClientSize.Width;
						offset += child.Height;
					}
				}
			}
			else
			{
				foreach (Control child in Controls)
				{
					if (child.Visible)
					{
						child.Left = offset;
						child.Height = ClientSize.Height;
						offset += child.Width;
					}
				}
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			using (Pen pen = new Pen(Color.White, 1))
			{
				args.Graphics.DrawLine(pen, 0, 0, ClientSize.Width, 0);
				pen.Color = Color.Gray;
				args.Graphics.DrawLine(pen, 0, ClientSize.Height - 1, ClientSize.Width, ClientSize.Height - 1);
			}
		}
	}
}
