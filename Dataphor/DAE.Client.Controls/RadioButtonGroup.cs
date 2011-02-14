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
		private const int RadioButtonSize = 12;
		private const int ButtonSpacing = 4;
        private const int MinimumWidth = RadioButtonSize + ButtonSpacing;
        // Right Buffer is needed to adjust for differing behavior between Windows themes
        private const int TitleBuffer = 10;
		
		public RadioButtonGroup() : base()
		{
			CausesValidation = false;
			SetStyle(ControlStyles.Selectable, true);

			base.TabStop = true;
		}

		private int _marginWidth = 2;
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("The width of an margin within the radio groups client area. In pixels.")]
		public virtual int MarginWidth
		{
			get { return _marginWidth; }
			set
			{
				if ((_marginWidth != value) && (value >= 0))
				{
					_marginWidth = value;
					PerformLayout();
				}
			}
		}
		
		private int _marginHeight = 2;
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("The height of an margin within the radio groups client area. In pixels.")]
		public virtual int MarginHeight
		{
			get { return _marginHeight; }
			set
			{
				if ((_marginHeight != value) && (value >= 0))
				{
					_marginHeight = value;
					PerformLayout();
				}
			}
		}
		
		private int _spacingHeight = 2;
		[DefaultValue(2)]
		[Category("Appearance")]
		[Description("The the vertical space between radio buttons. In pixels.")]
		public virtual int SpacingHeight
		{
			get { return _spacingHeight; }
			set
			{
				if (value < 0)
					value = 0;
				if (_spacingHeight != value)
				{
					_spacingHeight = value;
					PerformLayout();
				}
			}
		}
		
		private int _spacingWidth = 4;
		[DefaultValue(4)]
		[Category("Appearance")]
		[Description("The the horizontal space between radio buttons. In pixels.")]
		public virtual int SpacingWidth
		{
			get { return _spacingWidth; }
			set
			{
				if (value < 0)
					value = 0;
				if (_spacingWidth != value)
				{
					_spacingWidth = value;
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

		private int _selectedIndex = -1;
		[DefaultValue(-1)]
		[Category("Behavior")]
		[Description("The selected radio button index.")]
		public virtual int SelectedIndex
		{
			get { return _selectedIndex; }
			set
			{
				if ((_selectedIndex != value) && !Disposing && CanChange())
				{
					OnSelectedChanging(EventArgs.Empty);
					_selectedIndex = Math.Max(Math.Min(value, _items.Length - 1), -1);
					InvalidateButtons();
					OnSelectedChange(EventArgs.Empty);
				}
			}
		}

		public event EventHandler SelectedChange;

		protected virtual void OnSelectedChange(EventArgs args)
		{
			if (SelectedChange != null)
				SelectedChange(this, args);
		}

		public event EventHandler SelectedChanging;

		protected virtual void OnSelectedChanging(EventArgs args)
		{
			if (SelectedChanging != null)
				SelectedChanging(this, args);
		}

		protected virtual bool CanChange()
		{
			return true;
		}

		private string[] _items = new string[] {};
		/// <summary> Text for each RadioButton in the group. </summary>
		[Category("Appearance")]
		[Description("RadioButtons Text values.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual string[] Items
		{
			get { return _items; }
			set
			{
				// TODO: the string items in FItems can change without us knowing.  Do something.
				if (_items != value)
				{
					if (value == null)
						_items = new string[] {};
					else
						_items = value;
					PerformLayout();
				}
			}
		}

		private int _columns = 1;
		/// <summary> Number of RadioButton columns. </summary>
		[DefaultValue(1)]
		[Category("Appearance")]
		[Description("Number of columns to display the RadioButtons in.")]
		public int Columns
		{
			get { return _columns; }
			set
			{
				if (value < 1)
					value = 1;
				if (_columns != value)
				{
					_columns = value;
					PerformLayout();
				}
			}
		}

		protected override bool ShowFocusCues
		{
			get { return true; }
		}

		protected override bool IsInputKey(Keys key)
		{
			return 
				(key == Keys.Up) 
					|| (key == Keys.Down) 
					|| (key == Keys.Home) 
					|| (key == Keys.End)
					|| ((_columns > 1) && ((key == Keys.Up) || (key == Keys.Down)))
					|| base.IsInputKey(key);
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			switch (args.KeyData)
			{
				case Keys.Left : 
				case Keys.Up :
					if (_selectedIndex < 1)
						SelectedIndex = _items.Length - 1;
					else
						SelectedIndex--;
					break;
				case Keys.Right :
				case Keys.Down :
					if (_selectedIndex >= (_items.Length - 1))
						SelectedIndex = 0;
					else
						SelectedIndex++;
					break;
				case Keys.Home : SelectedIndex = 0; break;
				case Keys.End : SelectedIndex = (_items.Length - 1); break;
				default : base.OnKeyDown(args); return;
			}
			args.Handled = true;
		}

		protected override bool ProcessMnemonic(char charValue)
		{
			if (CanFocus)
			{
				for (int i = 0; i < _items.Length; i++)
					if (Control.IsMnemonic(charValue, _items[i]))
					{
						Focus();
						SelectedIndex = i;
						return true;
					}
				if (Control.IsMnemonic(charValue, Text))
				{
					Focus();
					return true;
				}
			}
			return base.ProcessMnemonic(charValue);
		}

		protected override void OnMouseDown(MouseEventArgs args)
		{
			base.OnMouseDown(args);
			if (args.Button == MouseButtons.Left)
			{
				Focus();
				if (_buttonBounds != null)
				{
					for (int i = 0; i < _buttonBounds.Length; i++)
						if (_buttonBounds[i].Contains(args.X, args.Y))
							SelectedIndex = i;
				}
			}
		}

		private static int Sum(int[] values, int startIndex, int endIndex)
		{
			int result = 0;
			for (int i = startIndex; i <= endIndex; i++)
				result += values[i];
			return result;
		}

		private Rectangle[] _buttonBounds;
		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);
			if (_items.Length > 0)
			{
				Rectangle bounds = DisplayRectangle;
				bounds.Inflate(-(1 + _marginWidth), -(1 + _marginHeight));

				// Calculate the number of buttons per column
				int buttonsPerColumn = (_items.Length / _columns) + ((_items.Length % _columns) == 0 ? 0 : 1);
				// Actual number of "populated" columns
				int columns = Math.Min(_columns, _items.Length);

                // Get desired width of columns
			    int[] columnWidths = GetColumnWidths(buttonsPerColumn);                

                // Calculate the total desired width of the columns
				int totalWidth = Sum(columnWidths, 0, columnWidths.Length - 1);             

				// Calculate the total width available to the columns
				int usableWidth = bounds.Width - ((columns - 1) * _spacingWidth);

                // Calculate the excess 
                int excess = usableWidth - totalWidth;
                           
				// Add the excess to column's width.  If the excess is positive, split it evenly, othewise distribute it to largest columns
                if (excess >= 0)
                {
                    int columnExcess = (int)Math.Round((double)excess / columns);                  
                    for (int i = 0; i < columns; i++)                   
                        columnWidths[i] = columnWidths[i] + columnExcess;
                }
                else
                {
                    int maxWidth = 0;
                    for (int i = 0; i < columns; i++)
                        maxWidth = Math.Max(maxWidth, columnWidths[i]);
                    while ((maxWidth >= MinimumWidth) && (excess < 0))
                    {
                        for (int i = 0; i < columns; i++)
                        {
                            if (excess == 0)
                                break;
                            if (columnWidths[i] == maxWidth)
                            {
                                columnWidths[i] = columnWidths[i] - 1;
                                excess++;
                            }
                        }
                        maxWidth--;
                    }
                }
                      
				// Layout each button
				int row;
                int column;
				int rowHeight = Math.Max(Font.Height, RadioButtonSize);
                _buttonBounds = new Rectangle[_items.Length];
				for (int i = 0; i < _items.Length; i++)
				{
					column = i / buttonsPerColumn;
					row = i % buttonsPerColumn;
					_buttonBounds[i] = 
						new Rectangle
						(
							bounds.Left + Sum(columnWidths, 0, column - 1) + (column * _spacingWidth), 
							bounds.Top + (row * rowHeight) + (row * _spacingHeight), 
							columnWidths[column], 
							rowHeight
						);
				}
			}
			else
				_buttonBounds = null;

			Invalidate();
		}
      
		private static StringFormat _format;
		private static StringFormat Format
		{
            get
            {
                if (_format == null)
                {
                    _format = new StringFormat();
                    _format.Trimming = StringTrimming.EllipsisCharacter;
                    _format.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
                }
                return _format;
            }
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			if (Focused)
				ControlPaint.DrawFocusRectangle(args.Graphics, DisplayRectangle);
			if (_buttonBounds != null)
			{
				Rectangle bounds;              
				ButtonState baseState = (Enabled ? ButtonState.Normal : ButtonState.Inactive);
				using (Brush brush = new SolidBrush(ForeColor))
				{
					using (Pen pen = new Pen(ForeColor))
					{
						for (int i = 0; i < _items.Length; i++)
						{
							bounds = _buttonBounds[i];
							if (args.Graphics.IsVisible(bounds))
							{
								ControlPaint.DrawRadioButton
								(
									args.Graphics, 
									bounds.Left, 
									bounds.Top, 
									RadioButtonSize, 
									RadioButtonSize, 
									baseState | (i == _selectedIndex ? ButtonState.Checked : 0)
								);
								bounds.X += RadioButtonSize + ButtonSpacing;
								bounds.Width -= RadioButtonSize + ButtonSpacing;
								args.Graphics.DrawString
								(
									_items[i], 
									Font, 
									brush, 
									bounds,
                                    Format 
								);
							}
						}
					}
				}
			}
		}

		private void InvalidateButtons()
		{
			Rectangle bounds = DisplayRectangle;
			bounds.Inflate(-(1 + _marginWidth), -(1 + _marginHeight));
			if (_buttonBounds != null)
			{
				int buttonsPerColumn = (_items.Length / _columns) + ((_items.Length % _columns) == 0 ? 0 : 1);
				for (int i = 0; (i < _columns) && ((i * buttonsPerColumn) < _items.Length); i++)
				{
					Invalidate
					(
						new Rectangle
						(
							_buttonBounds[i * buttonsPerColumn].Left, 
							bounds.Top, 
							RadioButtonSize, 
							bounds.Height
						)
					);
				}
			}
			else
				Invalidate(bounds);
		}

		private void InvalidateFocusRectangle()
		{
			Rectangle bounds = DisplayRectangle;
			Region region = new Region(bounds);
			bounds.Inflate(-1, -1);
			region.Exclude(bounds);
			Invalidate(region);
			region.Dispose();
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			InvalidateFocusRectangle();
		}

		protected override void OnLostFocus(EventArgs args)
		{
			base.OnLostFocus(args);
			InvalidateFocusRectangle();
		}

        private int[] GetColumnWidths(int buttonsPerColumn)
        {
            int[] columnWidths = new int[_columns];
            int width;
            int column;
            
            using (Graphics graphics = CreateGraphics())
            {
                for (int i = 0; i < _items.Length; i++)
                {
                    column = i / buttonsPerColumn;
                    width = MinimumWidth + Size.Ceiling(graphics.MeasureString(_items[i], Font)).Width;                    
                    if (columnWidths[column] < width)
                        columnWidths[column] = width;
                }
            }
            return columnWidths;
        }

        private Size _minimumSize;
        public override Size MinimumSize
        {
            get
            {
                if (!_minimumSize.IsEmpty)
                    return _minimumSize;

                Size result = Size.Empty;
                if (_items.Length > 0)
                {
                    int buttonsPerColumn = (_items.Length / _columns) + ((_items.Length % _columns) == 0 ? 0 : 1);
                    int columns = Math.Min(_columns, _items.Length);
                    int totalWidth = columns * MinimumWidth;
                    int rowHeight = Math.Max(Font.Height, RadioButtonSize);
                    result =
                        new Size
                        (
                            totalWidth + ((columns - 1) * _spacingWidth),
                            (buttonsPerColumn * rowHeight) + ((buttonsPerColumn - 1) * _spacingHeight)
                        );
                }
                return result + (Size - DisplayRectangle.Size) + new Size(2 + (_marginWidth * 2), 2 + (_marginHeight * 2));               
            }
            set
            {
                _minimumSize = value;
            }
        }
               
		public Size NaturalSize()
		{
			Size result = Size.Empty;
            int totalWidth = 0;
            int rowHeight = 0;
			if (_items.Length > 0)
			{
				int buttonsPerColumn = (_items.Length / _columns) + ((_items.Length % _columns) == 0 ? 0 : 1);
				int columns = Math.Min(_columns, _items.Length);
                int[] columnWidths = GetColumnWidths(buttonsPerColumn);
				
				totalWidth = Sum(columnWidths, 0, columnWidths.Length - 1);
                totalWidth += ((columns - 1) * _spacingWidth);
				rowHeight = Math.Max(Font.Height, RadioButtonSize);
                rowHeight = (buttonsPerColumn * rowHeight) + ((buttonsPerColumn - 1) * _spacingHeight);			    
			}            
            using (Graphics graphics = CreateGraphics())
            {
                result = new Size(Math.Max(totalWidth, (Size.Ceiling(graphics.MeasureString(Text, Font)).Width + TitleBuffer)), rowHeight);
            }
            return result + (Size - DisplayRectangle.Size) + new Size(2 + (_marginWidth * 2), 2 + (_marginHeight * 2));
		}
	}
}
