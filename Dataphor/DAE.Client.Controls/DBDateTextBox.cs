/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;
using System.Globalization;
using System.ComponentModel;
using Alphora.Dataphor.DAE.Client;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	public enum DateTimeEditCell {None, Day, Month, Year}
	public enum DateTimeEditCellPos {None, First, Second, Third}

	/// <summary> Represents a data-aware date edit control. </summary>
	[ToolboxItem(true)]
	[System.Drawing.ToolboxBitmap(typeof(DBDateTextBox), "Icons.DBDateTextBox.bmp")]
	public class DBDateTextBox : DBTextBox
	{
		/// <summary> Initializes a new instance of the DBDateTextBox class. </summary>
		public DBDateTextBox() : base()
		{
			_dateTimeFormatInfo = DateTimeFormatInfo.CurrentInfo;
			Link.OnActiveChanged += new DataLinkHandler(ActiveChanged);
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
				Link.OnActiveChanged -= new DataLinkHandler(ActiveChanged);
			base.Dispose(disposing);
		}

		protected override string FieldValue
		{
			get { return ((DataField == null) || (!DataField.HasValue())) ? String.Empty : InternalConvertDateTimeToString(DataField.AsDateTime); }
			set { DataField.AsDateTime = Convert.ToDateTime(value); }
		}

		private DateTimeFormatInfo _dateTimeFormatInfo;
		protected DateTimeFormatInfo DateTimeFormatInfo
		{
			get { return _dateTimeFormatInfo; }
		}

		protected string ShortDatePattern { get { return _dateTimeFormatInfo.ShortDatePattern; } }
		protected string LongTimePattern { get { return _dateTimeFormatInfo.LongTimePattern; } }

		private bool _editByCell = true;
		/// <summary> Validate individual month, day, and year input. </summary>
		/// <remarks> Month, day, and year order is determined by culture settings. </remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Validate individual month, day, and year input.")]
		public bool EditByCell
		{
			get { return _editByCell; }
			set { _editByCell = value; }
		}

		private bool _autoComplete = true;
		/// <summary> Complete user input when applicable. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Complete user input add date separators when applicable.")]
		public bool AutoComplete
		{
			get { return _autoComplete; }
			set
			{
				if (_autoComplete != value)
					_autoComplete = value;
			}
		}

		/// <summary> The Text property as DateTime value. </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual DateTime Date
		{
			get { return Convert.ToDateTime(Text); }
			set { Text = InternalConvertDateTimeToString(value); }
		}

        private bool _hideDate = false;
        /// <summary> Do not display the date part. </summary>
        /// <remarks> Could result in nothing being displayed. </remarks>
        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Do not display the date part.")]
        public bool HideDate
        {
            get { return _hideDate; }
            set { _hideDate = value; }
        }

        private bool _hideTime = true;
        /// <summary> Do not display the time part. </summary>
        /// <remarks> Could result in nothing being displayed. </remarks>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Do not display the time part.")]
        public bool HideTime
        {
            get { return _hideTime; }
            set { _hideTime = value; }
        }

		private string InternalConvertDateTimeToString(DateTime dateTime)
		{
			if (EditByCell)
				return 
                    dateTime.TimeOfDay.Ticks == 0 ? 
                        _hideDate ? String.Empty : dateTime.ToString(ShortDatePattern) 
                        : dateTime.ToString
                        (
                            String.Format
                            (
                                "{0} {1}", 
                                _hideDate ? String.Empty : ShortDatePattern, 
                                _hideTime ? String.Empty : LongTimePattern
                            ).Trim()
                        );
			else
				return dateTime.ToString("G");
		}

		protected void InternalSetDate(DateTime dateTime)
		{
			InternalSetText(InternalConvertDateTimeToString(dateTime));
		}

		private void ActiveChanged(DataLink link, DataSet dataSet)
		{
			// BTR -> This control should work with any data type that exposes an AsDateTime representation.
			//if ((DataField != null) && !(DataField.DataType.Is(AView.Process.DataTypes.SystemDateTime))&& !(DataField.DataType.Is(AView.Process.DataTypes.SystemDate)))
			//	throw new ControlsException(ControlsException.Codes.InvalidColumn, ColumnName);
		}

		private DateTime IncEditCell(DateTime date, DateTimeEditCell cell, int step)
		{
			switch (cell)
			{
				case DateTimeEditCell.Day:
					return date.AddDays((double)step);
				case DateTimeEditCell.Month:
					return date.AddMonths(step);
				case DateTimeEditCell.Year:
					return date.AddYears(step);
				default:
					return date;
			}
		}

		private bool CanAddSeparator()
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			DateTimeEditCellPos cellPos;
			ParseDateCells(Text, ref firstCell, ref secondCell, ref thirdCell);
			if ((firstCell.Length > 0) && (secondCell.Length == 0))
				cellPos = DateTimeEditCellPos.First;
			else if ((secondCell.Length > 0) && (thirdCell.Length == 0))
				cellPos = DateTimeEditCellPos.Second;
			else
				cellPos = DateTimeEditCellPos.None;

			int firstSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator);
			int secondSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator, firstSeparatorIndex + 1);
			bool hasFirstDateSeparator = firstSeparatorIndex > 0;
			bool hasSecondDateSeparator = secondSeparatorIndex > firstSeparatorIndex;

			return ((cellPos == DateTimeEditCellPos.First) && !hasFirstDateSeparator)
				|| ((cellPos == DateTimeEditCellPos.Second) && !hasSecondDateSeparator);
		}

		private void BackSpaceCleanup(KeyEventArgs args)
		{
			if (_autoComplete && (Text.Length > 0) && !CanAddSeparator() && ((Text.LastIndexOf(DateSeparator) + DateSeparator.Length) == Text.Length))
			{
				int saveSelectionStart = SelectionStart;
				_internalDisableAutoComplete = true;
				try
				{
					InternalSetText(Text.Substring(0, Text.Length - DateSeparator.Length));
				}
				finally
				{
					_internalDisableAutoComplete = false;
					SelectionStart = saveSelectionStart;
				}
			}	
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			base.OnKeyDown(args);
			switch (args.KeyData)
			{
				case Keys.Left:
					if (_editByCell && (args.Modifiers == Keys.None))
					{
						switch (GetFormatCellPos(EditCell))
						{
							case DateTimeEditCellPos.First:
								SelectAll();
								break;
							case DateTimeEditCellPos.Second: 
								EditCell = GetFormatCellType(DateTimeEditCellPos.First);
								break;
							case DateTimeEditCellPos.Third:
								EditCell = GetFormatCellType(DateTimeEditCellPos.Second);
								break;
							default:
								switch (GetFormatCellPos(EditingCell))
								{
									case DateTimeEditCellPos.First:
										if ((SelectionStart <= 0) && (SelectionLength == 0))
											SelectAll();
										else
											EditCell = GetFormatCellType(DateTimeEditCellPos.First);
										break;
									case DateTimeEditCellPos.Second:
										EditCell = GetFormatCellType(DateTimeEditCellPos.Second);
										break;
									case DateTimeEditCellPos.Third:
										EditCell = GetFormatCellType(DateTimeEditCellPos.Third);
										break;
									default:
										EditCell = GetFormatCellType(DateTimeEditCellPos.Third);
										break;
								}
								break;
						}
						args.Handled = true;
					}
					break;
				case Keys.Right:
					if (_editByCell && (args.Modifiers == Keys.None))
					{
						switch (GetFormatCellPos(EditCell))
						{
							case DateTimeEditCellPos.First:
								EditCell = GetFormatCellType(DateTimeEditCellPos.Second);
								break;
							case DateTimeEditCellPos.Second:
								EditCell = GetFormatCellType(DateTimeEditCellPos.Third);
								break;
							case DateTimeEditCellPos.Third:
								SelectAll();
								break;
							default:
								EditCell = GetFormatCellType(GetFormatCellPos(EditingCell));
								break;
						}
						args.Handled = true;
					}
					break;
				case Keys.Up:
				case Keys.Down:
					if (_editByCell && (args.Modifiers == Keys.None) && Link.Active)
					{
						Link.Edit();
						DateTimeEditCell saveEditCell = EditCell;
						try
						{
							if (args.KeyData == Keys.Up)
								InternalSetDate(IncEditCell(Date, EditCell, 1));
							else
								InternalSetDate(IncEditCell(Date, EditCell, -1));
						}
						finally
						{
							EditCell = saveEditCell;
						}
						args.Handled = true;
					}
					break;
				case Keys.H:
					if (args.Control)
						BackSpaceCleanup(args);
					break;
				case Keys.Back :
					BackSpaceCleanup(args);
					break;
			}
		}

		private bool CheckValidKey(char key)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			string currentCell;
			ParseDateCells(Text, ref firstCell, ref secondCell, ref thirdCell);
			
			switch (EditingCell)
			{
				case DateTimeEditCell.Day:
					int month = 0;
					switch (GetFormatCellPos(DateTimeEditCell.Month))
					{
						case DateTimeEditCellPos.First:
							if (firstCell != String.Empty)
								month = Convert.ToInt32(firstCell);
							break;
						case DateTimeEditCellPos.Second:
							if (secondCell != String.Empty)
								month = Convert.ToInt32(secondCell);
							break;
						case DateTimeEditCellPos.Third:
							if (thirdCell != String.Empty)
								month = Convert.ToInt32(thirdCell);
							break;
					}

					int year = 0;
					switch (GetFormatCellPos(DateTimeEditCell.Year))
					{
						case DateTimeEditCellPos.First:
							if (firstCell != String.Empty)
								year = Convert.ToInt32(firstCell);
							break;
						case DateTimeEditCellPos.Second:
							if (secondCell != String.Empty)
								year = Convert.ToInt32(secondCell);
							break;
						case DateTimeEditCellPos.Third:
							if (thirdCell != String.Empty)
								year = Convert.ToInt32(thirdCell);
							break;
					}

					//Thirty days hath September, April, June and November
					//all the rest have thirty one except for grandma she drives a Buick.
					
					int minDay = 1, maxDay = 31;
					switch (month)
					{
						case 2:
							if ((year == 0) || DateTime.IsLeapYear(year))
								maxDay = 29;
							else
								maxDay = 28;
							break;
						case 4:
						case 6:
						case 9:
						case 11:
							maxDay = 30;
							break;
					}

					switch (GetFormatCellPos(DateTimeEditCell.Day))
					{
						case DateTimeEditCellPos.First : currentCell = firstCell;
							break;
						case DateTimeEditCellPos.Second : currentCell = secondCell;
							break;
						case DateTimeEditCellPos.Third : currentCell = thirdCell;
							break;
						default : currentCell = String.Empty;
							break;
					}

					if (SelectionLength > 0)
						currentCell = String.Empty;
					if (currentCell == String.Empty)
						minDay = 0;
					int dayToTest = Convert.ToInt32(currentCell + key);
					return (dayToTest >= minDay) && (dayToTest <= maxDay);
				case DateTimeEditCell.Month:
					switch (GetFormatCellPos(DateTimeEditCell.Month))
					{
						case DateTimeEditCellPos.First : currentCell = firstCell;
							break;
						case DateTimeEditCellPos.Second : currentCell = secondCell;
							break;
						case DateTimeEditCellPos.Third : currentCell = thirdCell;
							break;
						default : currentCell = String.Empty;
							break;
					}

					int minMonth = 1, maxMonth = 12;

					if (SelectionLength > 0)
						currentCell = String.Empty;
					if (currentCell == String.Empty)
						minMonth = 0;
					int monthToTest = Convert.ToInt32(currentCell + key);
					return (monthToTest >= minMonth) && (monthToTest <= maxMonth);
				default:
					return true;
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs args)
		{
			base.OnKeyPress(args);
			if ((args.KeyChar == '+') || (args.KeyChar == '-'))
			{
				Link.Edit();
				if (args.KeyChar == '+')
					InternalSetDate(Date.AddDays(1));
				else
					InternalSetDate(Date.AddDays(-1));
				args.Handled = true;
			}
			else if ((DateSeparator.Length > 0) && (DateSeparator[0] == args.KeyChar))
				args.Handled = !CanAddSeparator();
			else if ((args.KeyChar >= (char)32) && (args.KeyChar <= (char)255))
				if (_editByCell)
					args.Handled = !CheckValidKey(args.KeyChar);
		}

		private bool _internalDisableAutoComplete;
		protected bool InternalDisableAutoComplete
		{
			get { return _internalDisableAutoComplete; }
			set { _internalDisableAutoComplete = value; }
		}

		protected override void InternalAutoUpdate()
		{
			if (!_autoComplete || (!InternalDisableAutoComplete && (GetFormatCellPos(EditingCell) == DateTimeEditCellPos.Third)))
				base.InternalAutoUpdate();
		}
		
		protected override void OnTextChanged(EventArgs args)
		{
			base.OnTextChanged(args);
			if (_autoComplete && !InternalDisableAutoComplete)
				CheckAddDateSeparator();
		}

		/// <summary> The character(s) that comprise the date separator. </summary>
		/// <remarks> DateSeparator is Defined by culture information. </remarks>
		[Browsable(false)]
		public string DateSeparator { get { return DateTimeFormatInfo.DateSeparator; } }

		private void AddDateSeparator()
		{
			InternalSetText(Text + DateSeparator);
			SelectionStart = Text.Length;
		}

		private void ParseDateCells(string text, ref string firstCell, ref string secondCell, ref string thirdCell)
		{
			string[] dateStrings = text.Split(DateSeparator.ToCharArray());
			firstCell = dateStrings[0];
			if (dateStrings.Length > 1)
				secondCell = dateStrings[1];
			if (dateStrings.Length > 2)
				thirdCell = dateStrings[2];
		}

		private DateTimeEditCellPos GetFormatCellPos(DateTimeEditCell cell)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			ParseDateCells(ShortDatePattern.ToUpper(), ref firstCell, ref secondCell, ref thirdCell);
			switch (cell)
			{
				case DateTimeEditCell.Day:
					if (firstCell.IndexOf("D") >= 0)
						return DateTimeEditCellPos.First;
					if (secondCell.IndexOf("D") >= 0)
						return DateTimeEditCellPos.Second;
					if (thirdCell.IndexOf("D") >= 0)
						return DateTimeEditCellPos.Third;
					return DateTimeEditCellPos.None;
				case DateTimeEditCell.Month:
					if (firstCell.IndexOf("M") >= 0)
						return DateTimeEditCellPos.First;
					if (secondCell.IndexOf("M") >= 0)
						return DateTimeEditCellPos.Second;
					if (thirdCell.IndexOf("M") >= 0)
						return DateTimeEditCellPos.Third;
					return DateTimeEditCellPos.None;
				case DateTimeEditCell.Year:
					if (firstCell.IndexOf("Y") >= 0)
						return DateTimeEditCellPos.First;
					if (secondCell.IndexOf("Y") >= 0)
						return DateTimeEditCellPos.Second;
					if (thirdCell.IndexOf("Y") >= 0)
						return DateTimeEditCellPos.Third;
					return DateTimeEditCellPos.None;
				default:
					return DateTimeEditCellPos.None;
			}
		}

		private DateTimeEditCell GetFormatCellType(DateTimeEditCellPos pos)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			ParseDateCells(ShortDatePattern.ToUpper(), ref firstCell, ref secondCell, ref thirdCell);
			switch (pos)
			{
				case DateTimeEditCellPos.First:
					if (firstCell.IndexOf("D") >= 0)
						return DateTimeEditCell.Day;
					if (firstCell.IndexOf("M") >= 0)
						return DateTimeEditCell.Month;
					if (firstCell.IndexOf("Y") >= 0)
						return DateTimeEditCell.Year;
					return DateTimeEditCell.None;
				case DateTimeEditCellPos.Second:
					if (secondCell.IndexOf("D") >= 0)
						return DateTimeEditCell.Day;
					if (secondCell.IndexOf("M") >= 0)
						return DateTimeEditCell.Month;
					if (secondCell.IndexOf("Y") >= 0)
						return DateTimeEditCell.Year;
					return DateTimeEditCell.None;
				case DateTimeEditCellPos.Third:
					if (thirdCell.IndexOf("D") >= 0)
						return DateTimeEditCell.Day;
					if (thirdCell.IndexOf("M") >= 0)
						return DateTimeEditCell.Month;
					if (thirdCell.IndexOf("Y") >= 0)
						return DateTimeEditCell.Year;
					return DateTimeEditCell.None;
				default:
					return DateTimeEditCell.None;
			}
		}

		private int GetFormatCellLength(DateTimeEditCellPos pos)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			ParseDateCells(ShortDatePattern.ToUpper(), ref firstCell, ref secondCell, ref thirdCell);
			switch (GetFormatCellType(pos))
			{
				case DateTimeEditCell.Day:
				case DateTimeEditCell.Month:
					return 2;
				default:
					switch (pos)
					{
						case DateTimeEditCellPos.First:
							return firstCell.Length;
						case DateTimeEditCellPos.Second:
							return secondCell.Length;
						case DateTimeEditCellPos.Third:
							return thirdCell.Length;
					}
					return 0;
			}
		}

		private void CheckAddDateSeparator()
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, currentCell;
			ParseDateCells(Text, ref firstCell, ref secondCell, ref thirdCell);
			DateTimeEditCellPos cellPos;
			if ((firstCell != String.Empty) && (secondCell == String.Empty))
			{
				cellPos = DateTimeEditCellPos.First;
				currentCell = firstCell;
			}
			else if ((secondCell != String.Empty) && (thirdCell == String.Empty))
			{
				cellPos = DateTimeEditCellPos.Second;
				currentCell = secondCell;
			}
			else
			{
				cellPos = DateTimeEditCellPos.None;
				currentCell = String.Empty;
			}

			int firstSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator);
			int secondSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator, firstSeparatorIndex + 1);
			bool hasFirstDateSeparator = firstSeparatorIndex > 0;
			bool hasSecondDateSeparator = secondSeparatorIndex > firstSeparatorIndex;
			if ((cellPos == DateTimeEditCellPos.First) && hasFirstDateSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos == DateTimeEditCellPos.Second) && hasSecondDateSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos != DateTimeEditCellPos.None) && (currentCell.Length == GetFormatCellLength(cellPos)))
				AddDateSeparator();
			else
			{
				switch (GetFormatCellType(cellPos))
				{
					case DateTimeEditCell.Day:
						int day = Convert.ToInt32(currentCell);
						if ((day >= 4) && (day <= 31))
							AddDateSeparator();
						break;
					case DateTimeEditCell.Month:
						int month = Convert.ToInt32(currentCell);
						if ((month >= 2) && (month <= 12))
							AddDateSeparator();
						break;
				}
			}
		}

		private DateTimeEditCell InternalGetEditingCell()
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			ParseDateCells(Text, ref firstCell, ref secondCell, ref thirdCell);
			if (SelectionStart <= firstCell.Length)
			{
				if ((SelectionLength == Text.Length) && (firstCell.Length != Text.Length))
					return DateTimeEditCell.None;
				else
					return GetFormatCellType(DateTimeEditCellPos.First);
			}
			else if (SelectionStart <= (firstCell.Length + secondCell.Length + DateSeparator.Length))
				return GetFormatCellType(DateTimeEditCellPos.Second);
			else if (SelectionStart <= Text.Length)
				return GetFormatCellType(DateTimeEditCellPos.Third);
			else
				return DateTimeEditCell.None;
		}

		private void InternalSetEditCell(DateTimeEditCell value)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty;
			ParseDateCells(Text, ref firstCell, ref secondCell, ref thirdCell);
			switch (GetFormatCellPos(value))
			{
				case DateTimeEditCellPos.First:
					SelectionStart = 0;
					SelectionLength = firstCell.Length;
					break;
				case DateTimeEditCellPos.Second:
					SelectionStart = firstCell.Length + DateSeparator.Length;
					SelectionLength = secondCell.Length;
					break;
				case DateTimeEditCellPos.Third:
					SelectionStart = firstCell.Length + secondCell.Length + (2 * DateSeparator.Length);
					SelectionLength = thirdCell.Length;
					break;
				default:
					SelectionStart = 0;
					SelectionLength = 0;
					break;
			}
		}
		
		protected DateTimeEditCell EditCell
		{
			get
			{
				if (SelectionLength == 0)
					return DateTimeEditCell.None;
				else
					return EditingCell;
			}
			set { InternalSetEditCell(value); }
		}
		
		protected DateTimeEditCell EditingCell { get { return InternalGetEditingCell();	} }

	}
}
