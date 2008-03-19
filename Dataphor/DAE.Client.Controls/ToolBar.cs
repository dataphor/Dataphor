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
		public const int CWidthPadding = 10;
		public const int CImagePadding = 0;

		public ToolBarButton() : base()
		{
			InitializeControl();
		}

		public ToolBarButton(string AText, EventHandler AClick) : base()
		{
			InitializeControl();
			Text = AText;
			Click += AClick;
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
				using (Graphics LGraphics = this.CreateGraphics())
					Width = CWidthPadding + (Image == null ? 0 : Image.Width + CImagePadding) + Size.Ceiling(LGraphics.MeasureString(Text, Font)).Width;
			}
		}

		protected override void OnHandleCreated(EventArgs AArgs)
		{
			base.OnHandleCreated(AArgs);
			UpdateSize();
		}

		protected override void OnTextChanged(EventArgs AArgs)
		{
			base.OnTextChanged(AArgs);
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

		protected override bool ProcessMnemonic(char ACharCode)
		{
			if (base.ProcessMnemonic(ACharCode))
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

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			//base.OnLayout();
			int LOffset = 0;
			if ((Dock == DockStyle.Left) || (Dock == DockStyle.Right))
			{
				foreach (Control LChild in Controls)
				{
					if (LChild.Visible)
					{
						LChild.Top = LOffset;
						LChild.Width = ClientSize.Width;
						LOffset += LChild.Height;
					}
				}
			}
			else
			{
				foreach (Control LChild in Controls)
				{
					if (LChild.Visible)
					{
						LChild.Left = LOffset;
						LChild.Height = ClientSize.Height;
						LOffset += LChild.Width;
					}
				}
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			using (Pen LPen = new Pen(Color.White, 1))
			{
				AArgs.Graphics.DrawLine(LPen, 0, 0, ClientSize.Width, 0);
				LPen.Color = Color.Gray;
				AArgs.Graphics.DrawLine(LPen, 0, ClientSize.Height - 1, ClientSize.Width, ClientSize.Height - 1);
			}
		}
	}
}
