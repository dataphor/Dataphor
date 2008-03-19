/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	using System.ComponentModel;

	[ToolboxItem(true)]
	[DefaultProperty("Items")]
	[DefaultEvent("OnSelectedChange")]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.RadioButtonGroup),"Icons.RadioButtonGroup.bmp")]
	public class RadioButtonGroup : GroupBox, IDisposable
	{
		private const int CRadioButtonSize = 12;
		private const int CButtonSpacing = 4;
		private const int CMinimumWidth = CRadioButtonSize + CButtonSpacing + 40;

		public RadioButtonGroup() : base()
		{
			CausesValidation = false;
			SetStyle(ControlStyles.Selectable, true);

			base.TabStop = true;
		}

		private int FMarginWidth = 2;
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("The width of an margin within the radio groups client area. In pixels.")]
		public virtual int MarginWidth
		{
			get { return FMarginWidth; }
			set
			{
				if ((FMarginWidth != value) && (value >= 0))
				{
					FMarginWidth = value;
					PerformLayout();
				}
			}
		}
		
		private int FMarginHeight = 2;
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("The height of an margin within the radio groups client area. In pixels.")]
		public virtual int MarginHeight
		{
			get { return FMarginHeight; }
			set
			{
				if ((FMarginHeight != value) && (value >= 0))
				{
					FMarginHeight = value;
					PerformLayout();
				}
			}
		}
		
		private int FSpacingHeight = 2;
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("The the vertical space between radio buttons. In pixels.")]
		public virtual int SpacingHeight
		{
			get { return FSpacingHeight; }
			set
			{
				if (value < 0)
					value = 0;
				if (FSpacingHeight != value)
				{
					FSpacingHeight = value;
					PerformLayout();
				}
			}
		}
		
		private int FSpacingWidth = 4;
		[DefaultValue(4)]
		[Category("Appearance")]
		[Description("The the horizontal space between radio buttons. In pixels.")]
		public virtual int SpacingWidth
		{
			get { return FSpacingWidth; }
			set
			{
				if (value < 0)
					value = 0;
				if (FSpacingWidth != value)
				{
					FSpacingWidth = value;
					PerformLayout();
				}
			}
		}

		[DefaultValue(true)]
		[Category("Behavior")]
		new public virtual bool TabStop
		{
			get { return base.TabStop; }
			set { base.TabStop = value; }
		}

		private int FSelectedIndex = -1;
		[DefaultValue(-1)]
		[Category("Behavior")]
		[Description("The selected radio button index.")]
		public virtual int SelectedIndex
		{
			get { return FSelectedIndex; }
			set
			{
				if ((FSelectedIndex != value) && !Disposing && CanChange())
				{
					OnSelectedChanging(EventArgs.Empty);
					FSelectedIndex = Math.Max(Math.Min(value, FItems.Length - 1), -1);
					InvalidateButtons();
					OnSelectedChange(EventArgs.Empty);
				}
			}
		}

		public event EventHandler SelectedChange;

		protected virtual void OnSelectedChange(EventArgs AArgs)
		{
			if (SelectedChange != null)
				SelectedChange(this, AArgs);
		}

		public event EventHandler SelectedChanging;

		protected virtual void OnSelectedChanging(EventArgs AArgs)
		{
			if (SelectedChanging != null)
				SelectedChanging(this, AArgs);
		}

		protected virtual bool CanChange()
		{
			return true;
		}

		private string[] FItems = new string[] {};
		/// <summary> Text for each RadioButton in the group. </summary>
		[Category("Appearance")]
		[Description("RadioButtons Text values.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual string[] Items
		{
			get { return FItems; }
			set
			{
				// TODO: the string items in FItems can change without us knowing.  Do something.
				if (FItems != value)
				{
					if (value == null)
						FItems = new string[] {};
					else
						FItems = value;
					PerformLayout();
				}
			}
		}

		private int FColumns = 1;
		/// <summary> Number of RadioButton columns. </summary>
		[DefaultValue(1)]
		[Category("Appearance")]
		[Description("Number of columns to display the RadioButtons in.")]
		public int Columns
		{
			get { return FColumns; }
			set
			{
				if (value < 1)
					value = 1;
				if (FColumns != value)
				{
					FColumns = value;
					PerformLayout();
				}
			}
		}

		protected override bool ShowFocusCues
		{
			get { return true; }
		}

		protected override bool IsInputKey(Keys AKey)
		{
			return 
				(AKey == Keys.Up) 
					|| (AKey == Keys.Down) 
					|| (AKey == Keys.Home) 
					|| (AKey == Keys.End)
					|| ((FColumns > 1) && ((AKey == Keys.Up) || (AKey == Keys.Down)))
					|| base.IsInputKey(AKey);
		}

		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			switch (AArgs.KeyData)
			{
				case Keys.Left : 
				case Keys.Up :
					if (FSelectedIndex < 1)
						SelectedIndex = FItems.Length - 1;
					else
						SelectedIndex--;
					break;
				case Keys.Right :
				case Keys.Down :
					if (FSelectedIndex >= (FItems.Length - 1))
						SelectedIndex = 0;
					else
						SelectedIndex++;
					break;
				case Keys.Home : SelectedIndex = 0; break;
				case Keys.End : SelectedIndex = (FItems.Length - 1); break;
				default : base.OnKeyDown(AArgs); return;
			}
			AArgs.Handled = true;
		}

		protected override bool ProcessMnemonic(char AChar)
		{
			if (CanFocus)
			{
				for (int i = 0; i < FItems.Length; i++)
					if (Control.IsMnemonic(AChar, FItems[i]))
					{
						Focus();
						SelectedIndex = i;
						return true;
					}
				if (Control.IsMnemonic(AChar, Text))
				{
					Focus();
					return true;
				}
			}
			return base.ProcessMnemonic(AChar);
		}

		protected override void OnMouseDown(MouseEventArgs AArgs)
		{
			base.OnMouseDown(AArgs);
			if (AArgs.Button == MouseButtons.Left)
			{
				Focus();
				if (FButtonBounds != null)
				{
					for (int i = 0; i < FButtonBounds.Length; i++)
						if (FButtonBounds[i].Contains(AArgs.X, AArgs.Y))
							SelectedIndex = i;
				}
			}
		}

		private static int Sum(int[] AValues, int AStartIndex, int AEndIndex)
		{
			int LResult = 0;
			for (int i = AStartIndex; i <= AEndIndex; i++)
				LResult += AValues[i];
			return LResult;
		}

		private Rectangle[] FButtonBounds;
		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);
			if (FItems.Length > 0)
			{
				Rectangle LBounds = DisplayRectangle;
				LBounds.Inflate(-(1 + FMarginWidth), -(1 + FMarginHeight));

				// Calculate the number of buttons per column
				int LButtonsPerColumn = (FItems.Length / FColumns) + ((FItems.Length % FColumns) == 0 ? 0 : 1);
				// Actual number of "populated" columns
				int LColumns = Math.Min(FColumns, FItems.Length);

				// Calculate the maximum widths of buttons within each column
				FButtonBounds = new Rectangle[FItems.Length];
				int[] LColumnWidths = new int[FColumns];
				int LWidth;
				int LColumn;
				using (Graphics LGraphics = CreateGraphics())
				{
					for (int i = 0; i < FItems.Length; i++)
					{
						LColumn = i / LButtonsPerColumn;
						LWidth = CRadioButtonSize + CButtonSpacing + Size.Ceiling(LGraphics.MeasureString(FItems[i], Font)).Width;
						if (LColumnWidths[LColumn] < LWidth)
							LColumnWidths[LColumn] = LWidth;
					}
				}

				// Calculate the total desired width of the columns
				int LTotalWidth = Sum(LColumnWidths, 0, LColumnWidths.Length - 1);

				// Subtract the minimal required width for the total (for all columns)
				LTotalWidth = Math.Max(0, LTotalWidth - (LColumns * CMinimumWidth));

				// Calculate the total width available to the columns
				int LUsableWidth = LBounds.Width - ((LColumns - 1) * FSpacingWidth);

				// Subtract the minimal required width for the usable (for all columns)
				LUsableWidth = Math.Max(0, LUsableWidth - (LColumns * CMinimumWidth));

				// Calculate the scaling factor
				double LScalar = (LTotalWidth == 0 ? 1d : (double)LUsableWidth / (double)LTotalWidth);

				// Scale each column's "excess" appropriately
				for (int i = 0; i < LColumns; i++)
					LColumnWidths[i] = CMinimumWidth + (int)Math.Round((double)Math.Max(0, (LColumnWidths[i] - CMinimumWidth) * LScalar));

				// Layout each button
				int LRow;
				int LRowHeight = Math.Max(Font.Height, CRadioButtonSize);
				for (int i = 0; i < FItems.Length; i++)
				{
					LColumn = i / LButtonsPerColumn;
					LRow = i % LButtonsPerColumn;
					FButtonBounds[i] = 
						new Rectangle
						(
							LBounds.Left + Sum(LColumnWidths, 0, LColumn - 1) + (LColumn * FSpacingWidth), 
							LBounds.Top + (LRow * LRowHeight) + (LRow * FSpacingHeight), 
							LColumnWidths[LColumn], 
							LRowHeight
						);
				}
			}
			else
				FButtonBounds = null;

			Invalidate();
		}

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

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			if (Focused)
				ControlPaint.DrawFocusRectangle(AArgs.Graphics, DisplayRectangle);
			if (FButtonBounds != null)
			{
				Rectangle LBounds;
				ButtonState LBaseState = (Enabled ? ButtonState.Normal : ButtonState.Inactive);
				using (Brush LBrush = new SolidBrush(ForeColor))
				{
					using (Pen LPen = new Pen(ForeColor))
					{
						for (int i = 0; i < FItems.Length; i++)
						{
							LBounds = FButtonBounds[i];
							if (AArgs.Graphics.IsVisible(LBounds))
							{
								ControlPaint.DrawRadioButton
								(
									AArgs.Graphics, 
									LBounds.Left, 
									LBounds.Top, 
									CRadioButtonSize, 
									CRadioButtonSize, 
									LBaseState | (i == FSelectedIndex ? ButtonState.Checked : 0)
								);
								LBounds.X += CRadioButtonSize + CButtonSpacing;
								LBounds.Width -= CRadioButtonSize + CButtonSpacing;
								AArgs.Graphics.DrawString
								(
									FItems[i], 
									Font, 
									LBrush, 
									LBounds,
									FFormat
								);
							}
						}
					}
				}
			}
		}

		private void InvalidateButtons()
		{
			Rectangle LBounds = DisplayRectangle;
			LBounds.Inflate(-(1 + FMarginWidth), -(1 + FMarginHeight));
			if (FButtonBounds != null)
			{
				int LButtonsPerColumn = (FItems.Length / FColumns) + ((FItems.Length % FColumns) == 0 ? 0 : 1);
				for (int i = 0; (i < FColumns) && ((i * LButtonsPerColumn) < FItems.Length); i++)
				{
					Invalidate
					(
						new Rectangle
						(
							FButtonBounds[i * LButtonsPerColumn].Left, 
							LBounds.Top, 
							CRadioButtonSize, 
							LBounds.Height
						)
					);
				}
			}
			else
				Invalidate(LBounds);
		}

		private void InvalidateFocusRectangle()
		{
			Rectangle LBounds = DisplayRectangle;
			Region LRegion = new Region(LBounds);
			LBounds.Inflate(-1, -1);
			LRegion.Exclude(LBounds);
			Invalidate(LRegion);
			LRegion.Dispose();
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			InvalidateFocusRectangle();
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
			InvalidateFocusRectangle();
		}

		public Size NaturalSize()
		{
			Size LResult = Size.Empty;
			if (FItems.Length > 0)
			{
				int LButtonsPerColumn = (FItems.Length / FColumns) + ((FItems.Length % FColumns) == 0 ? 0 : 1);
				int LColumns = Math.Min(FColumns, FItems.Length);

				int[] LColumnWidths = new int[FColumns];
				int LWidth;
				int LColumn;
				using (Graphics LGraphics = CreateGraphics())
				{
					for (int i = 0; i < FItems.Length; i++)
					{
						LColumn = i / LButtonsPerColumn;
						LWidth = CRadioButtonSize + CButtonSpacing + Size.Ceiling(LGraphics.MeasureString(FItems[i], Font)).Width;
						if (LColumnWidths[LColumn] < LWidth)
							LColumnWidths[LColumn] = LWidth;
					}
				}
				int LTotalWidth = Sum(LColumnWidths, 0, LColumnWidths.Length - 1);
				int LRowHeight = Math.Max(Font.Height, CRadioButtonSize);
				LResult = 
					new Size
					(
						LTotalWidth + ((LColumns - 1) * FSpacingWidth), 
						(LButtonsPerColumn * LRowHeight) + ((LButtonsPerColumn - 1) * FSpacingHeight)
					);
			}
			return LResult + (Size - DisplayRectangle.Size) + new Size(2 + (FMarginWidth * 2), 2 + (FMarginHeight * 2));
		}
	}
}
