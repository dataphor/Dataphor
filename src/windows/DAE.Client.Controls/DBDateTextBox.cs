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
	public enum DateTimeEditCell { None, Meridian, Second, Minute, Hour, Day, Month, Year }
	public enum DateTimeEditCellPos { None, First, Second, Third, Fourth, Fifth, Sixth, Seventh }

	/// <summary> Represents a data-aware date edit control. </summary>
	[ToolboxItem(true)]
	[System.Drawing.ToolboxBitmap(typeof(DBDateTextBox), "Icons.DBDateTextBox.bmp")]
	public class DBDateTextBox : DBTextBox
	{
		/// <summary> Initializes a new instance of the DBDateTextBox class. </summary>
		public DBDateTextBox() : base()
		{
			_dateTimeFormatInfo = DateTimeFormatInfo.CurrentInfo;
			_separators = new char[] { DateTimeFormatInfo.DateSeparator[0], DateTimeFormatInfo.TimeSeparator[0], ' ', 'T' };
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
		protected string DateTimePattern { get { return ShortDatePattern.ToUpper() + ' ' + LongTimePattern; } }

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
			if (_hideDate || _hideTime)
			{
				return
					dateTime.ToString
					(
						String.Format
						(
							"{0} {1}",
							_hideDate ? String.Empty : ShortDatePattern,
							_hideTime ? String.Empty : LongTimePattern
						).Trim()
					);
			}
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
				case DateTimeEditCell.Meridian:
					return date.AddMeridian(step);
				case DateTimeEditCell.Second:
					return date.AddSeconds((double)step);
				case DateTimeEditCell.Minute:
					return date.AddMinutes((double)step);
				case DateTimeEditCell.Hour:
					return date.AddHours((double)step);
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
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			DateTimeEditCellPos cellPos;
			ParseDateTimeCells(Text, ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			if ((firstCell.Length > 0) && (secondCell.Length == 0))
				cellPos = DateTimeEditCellPos.First;
			else if ((secondCell.Length > 0) && (thirdCell.Length == 0))
				cellPos = DateTimeEditCellPos.Second;
			else if ((thirdCell.Length > 0) && (fourthCell.Length == 0))
				cellPos = DateTimeEditCellPos.Third;
			else if ((fourthCell.Length > 0) && (fifthCell.Length == 0))
				cellPos = DateTimeEditCellPos.Fourth;
			else if ((fifthCell.Length > 0) && (sixthCell.Length == 0))
				cellPos = DateTimeEditCellPos.Fifth;
			else if ((sixthCell.Length > 0) && (seventhCell.Length == 0))
				cellPos = DateTimeEditCellPos.Sixth;
			else if (seventhCell.Length > 0)
				cellPos = DateTimeEditCellPos.Seventh;
			else
				cellPos = DateTimeEditCellPos.None;

			int firstSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator);
			int secondSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator, firstSeparatorIndex + 1);
			int dateAndTimeSeparatorIndex = Text.IndexOfAny(new char[] { ' ', 'T' });
			int firstTimeSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.TimeSeparator, secondSeparatorIndex + 1);
			int secondTimeSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.TimeSeparator, firstTimeSeparatorIndex + 1);
			int meridianSeparatorIndex = Text.IndexOf(' ', Math.Max(firstTimeSeparatorIndex, secondTimeSeparatorIndex) + 1);
			bool hasFirstDateSeparator = firstSeparatorIndex > 0;
			bool hasSecondDateSeparator = secondSeparatorIndex > firstSeparatorIndex;
			bool hasDateAndTimeSeparator = dateAndTimeSeparatorIndex > 0;
			bool hasFirstTimeSeparator = (_hideDate || hasDateAndTimeSeparator) && (firstTimeSeparatorIndex > dateAndTimeSeparatorIndex);
			bool hasSecondTimeSeparator = secondTimeSeparatorIndex > firstTimeSeparatorIndex;
			bool hasMeridianSeparator = hasSecondTimeSeparator && meridianSeparatorIndex > 0;

			return ((cellPos == DateTimeEditCellPos.First) && !_hideDate && !hasFirstDateSeparator)
				|| ((cellPos == DateTimeEditCellPos.Second) && !_hideDate && !hasSecondDateSeparator)
				|| ((cellPos == DateTimeEditCellPos.Third) && !_hideTime && (_hideDate || !hasDateAndTimeSeparator))
				|| ((cellPos == DateTimeEditCellPos.Fourth) && !_hideTime && !hasFirstTimeSeparator)
				|| ((cellPos == DateTimeEditCellPos.Fifth) && !_hideTime && !hasSecondTimeSeparator)
				|| ((cellPos == DateTimeEditCellPos.Sixth) && !_hideTime && !hasMeridianSeparator);
		}

		private void BackSpaceCleanup(KeyEventArgs args)
		{
			if (_autoComplete && (Text.Length > 0) && !CanAddSeparator() && ((Text.LastIndexOfAny(Separators) + DateSeparator.Length) == Text.Length))
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
						if (SelectionLength == Text.Length)
						{
							SelectionStart = 0;
							SelectionLength = 0;
						}
						else
						{
							var editCell = GetFormatCellPos(EditCell);
							switch (editCell)
							{
								case DateTimeEditCellPos.First:
									SelectAll();
									break;
								case DateTimeEditCellPos.Fourth:
									if (_hideDate)
										SelectAll();
									else
										EditCell = GetFormatCellType(editCell - 1);
									break;
								case DateTimeEditCellPos.Second: 
								case DateTimeEditCellPos.Third:
								case DateTimeEditCellPos.Fifth:
								case DateTimeEditCellPos.Sixth:
								case DateTimeEditCellPos.Seventh:
									EditCell = GetFormatCellType(editCell - 1);
									break;
								default:
									editCell = GetFormatCellPos(EditingCell);
									switch (editCell)
									{
										case DateTimeEditCellPos.First:
											if ((SelectionStart <= 0) && (SelectionLength == 0))
												SelectAll();
											else
												EditCell = GetFormatCellType(editCell);
											break;
										case DateTimeEditCellPos.Fourth:
											if (_hideDate && (SelectionStart <= 0) && (SelectionLength == 0))
												SelectAll();
											else
												EditCell = GetFormatCellType(editCell);
											break;
										case DateTimeEditCellPos.Second:
										case DateTimeEditCellPos.Third:
										case DateTimeEditCellPos.Fifth:
										case DateTimeEditCellPos.Sixth:
										case DateTimeEditCellPos.Seventh:
											EditCell = GetFormatCellType(editCell);
											break;
										default:
											if (!_hideTime)
											{
												if (GetFormatCellLength(DateTimeEditCellPos.Seventh) == 0)
													EditCell = GetFormatCellType(DateTimeEditCellPos.Sixth);
												else
													EditCell = GetFormatCellType(DateTimeEditCellPos.Seventh);
											}
											else
											{
												EditCell = GetFormatCellType(DateTimeEditCellPos.Third);
											}
											break;
									}
									break;
							}
						}
						args.Handled = true;
					}
					break;
				case Keys.Right:
					if (_editByCell && (args.Modifiers == Keys.None))
					{
						if (SelectionLength == Text.Length)
						{
							SelectionStart = Text.Length;
							SelectionLength = 0;
						}
						else
						{
							var editCell = GetFormatCellPos(EditCell);
							switch (editCell)
							{
								case DateTimeEditCellPos.First:
								case DateTimeEditCellPos.Second:
								case DateTimeEditCellPos.Fourth:
								case DateTimeEditCellPos.Fifth:
									EditCell = GetFormatCellType(editCell + 1);
									break;
								case DateTimeEditCellPos.Third:
									if (_hideTime)
										SelectAll();
									else
										EditCell = GetFormatCellType(editCell + 1);
									break;
								case DateTimeEditCellPos.Sixth:
									if (GetFormatCellLength(DateTimeEditCellPos.Seventh) == 0)
										SelectAll();
									else
										EditCell = GetFormatCellType(editCell + 1);
									break;
								case DateTimeEditCellPos.Seventh:
									SelectAll();
									break;
								default:
									if ((SelectionStart >= Text.Length) && (SelectionLength == 0))
										SelectAll();
									else
										EditCell = GetFormatCellType(GetFormatCellPos(EditingCell));
									break;
							}
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

		protected override bool IsInputKey(Keys key)
		{
			if (key == Keys.Up || key == Keys.Down)
				return true;
			else
				return base.IsInputKey(key);
		}

		private bool CheckValidKey(char key)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			string currentCell;
			ParseDateTimeCells(Text, ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			
			switch (EditingCell)
			{
				case DateTimeEditCell.Second:
					decimal second;
					currentCell = SelectionLength > 0 ? String.Empty : sixthCell;
					return decimal.TryParse(currentCell + key, out second) && second < 60;

				case DateTimeEditCell.Minute:
					int minute;
					currentCell = SelectionLength > 0 ? String.Empty : fifthCell;
					return int.TryParse(currentCell + key, out minute) && minute < 60;

				case DateTimeEditCell.Hour:
					int hour;
					currentCell = SelectionLength > 0 ? String.Empty : fourthCell;
					return int.TryParse(currentCell + key, out hour) && hour <= 24;

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
			else if (((DateSeparator.Length > 0) && (DateSeparator[0] == args.KeyChar)) 
				|| ((DateAndTimeSeparator.Length > 0) && (DateAndTimeSeparator[0] == args.KeyChar))
				|| ((TimeSeparator.Length > 0) && (TimeSeparator[0] == args.KeyChar)))
				args.Handled = !CanAddSeparator();
			else if (((AMDesignator.Length > 0) && (AMDesignator.IndexOf(args.KeyChar) >= 0))
				|| ((PMDesignator.Length > 0) && (PMDesignator.IndexOf(args.KeyChar) >= 0)))
			{
				if (_editByCell) 
					args.Handled = !(EditingCell == DateTimeEditCell.Meridian);
			}
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

		[Browsable(false)]
		public string TimeSeparator { get { return DateTimeFormatInfo.TimeSeparator; } }

		[Browsable(false)]
		public string DateAndTimeSeparator { get { return " "; } }

		[Browsable(false)]
		public string AMDesignator { get { return DateTimeFormatInfo.AMDesignator; } }

		[Browsable(false)]
		public string PMDesignator { get { return DateTimeFormatInfo.PMDesignator; } }

		private char[] _separators;
		public char[] Separators { get { return _separators; } }

		private void AddDateSeparator()
		{
			InternalSetText(Text + DateSeparator);
			SelectionStart = Text.Length;
		}

		private void AddDateAndTimeSeparator()
		{
			InternalSetText(Text + DateAndTimeSeparator);
			SelectionStart = Text.Length;
		}

		private void AddTimeSeparator()
		{
			InternalSetText(Text + TimeSeparator);
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

		private void ParseTimeCells(string text, ref string fourthCell, ref string fifthCell, ref string sixthCell, ref string seventhCell)
		{
			var indexOfMeridian = text.LastIndexOf(DateAndTimeSeparator);

			if (indexOfMeridian > 0)
			{
				seventhCell = text.Substring(indexOfMeridian).Trim();
				text = text.Substring(0, indexOfMeridian).Trim();
			}
			else
				text = text.Trim(); // Trim the ending separator if there is one

			string[] timeStrings = text.Split(TimeSeparator.ToCharArray());
			if (timeStrings.Length > 0)
				fourthCell = timeStrings[0];
			if (timeStrings.Length > 1)
				fifthCell = timeStrings[1];
			if (timeStrings.Length > 2)
				sixthCell = timeStrings[2];
		}

		private void ParseDateTimeCells(string text, ref string firstCell, ref string secondCell, ref string thirdCell, ref string fourthCell, ref string fifthCell, ref string sixthCell, ref string seventhCell)
		{
			// 1-3 : Date Elements, order specified by ShortDateFormat
			// 4-6 : Time Elements, hour, minute, second
			// 7 : meridian, if present in the LongTimeFormat
			if (_hideDate)
			{
				ParseTimeCells(text, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			}
			else if (_hideTime)
			{
				ParseDateCells(text, ref firstCell, ref secondCell, ref thirdCell);
			}
			else
			{
				var dateAndTimeSeparatorIndex = text.IndexOf(DateAndTimeSeparator);
				var dateText = dateAndTimeSeparatorIndex > 0 ? text.Substring(0, dateAndTimeSeparatorIndex).Trim() : text;
				var timeText = dateAndTimeSeparatorIndex > 0 ? text.Substring(dateAndTimeSeparatorIndex).Trim() : string.Empty;

				ParseDateCells(dateText, ref firstCell, ref secondCell, ref thirdCell);
				ParseTimeCells(timeText, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			}
		}

		private void ParseFormatCells(ref string firstCell, ref string secondCell, ref string thirdCell, ref string fourthCell, ref string fifthCell, ref string sixthCell, ref string seventhCell)
		{
			ParseTimeCells(LongTimePattern, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			ParseDateCells(ShortDatePattern.ToUpper(), ref firstCell, ref secondCell, ref thirdCell);
		}

		private DateTimeEditCellPos GetFormatCellPos(DateTimeEditCell cell)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			ParseFormatCells(ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			switch (cell)
			{
				case DateTimeEditCell.Meridian:
					return seventhCell == String.Empty ? DateTimeEditCellPos.None : DateTimeEditCellPos.Seventh;

				case DateTimeEditCell.Second:
					return DateTimeEditCellPos.Sixth;

				case DateTimeEditCell.Minute:
					return DateTimeEditCellPos.Fifth;

				case DateTimeEditCell.Hour:
					return DateTimeEditCellPos.Fourth;

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
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			ParseFormatCells(ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
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
				case DateTimeEditCellPos.Fourth:
					return DateTimeEditCell.Hour;
				case DateTimeEditCellPos.Fifth:
					return DateTimeEditCell.Minute;
				case DateTimeEditCellPos.Sixth:
					return DateTimeEditCell.Second;
				case DateTimeEditCellPos.Seventh:
					return fourthCell.IndexOf("H") >= 0 ? DateTimeEditCell.None : DateTimeEditCell.Meridian;
				default:
					return DateTimeEditCell.None;
			}
		}

		private int GetFormatCellLength(DateTimeEditCellPos pos)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			ParseFormatCells(ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			switch (GetFormatCellType(pos))
			{
				case DateTimeEditCell.Day:
				case DateTimeEditCell.Month:
				case DateTimeEditCell.Hour:
				case DateTimeEditCell.Minute:
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
						case DateTimeEditCellPos.Fourth:
							return fourthCell.Length;
						case DateTimeEditCellPos.Fifth:
							return fifthCell.Length;
						case DateTimeEditCellPos.Sixth:
							return sixthCell.Length;
						case DateTimeEditCellPos.Seventh:
							return seventhCell.Length;
					}
					return 0;
			}
		}

		private void CheckAddDateSeparator()
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, currentCell, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			ParseDateTimeCells(Text, ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
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
			else if ((thirdCell != String.Empty) && (fourthCell == String.Empty))
			{
				cellPos = DateTimeEditCellPos.Third;
				currentCell = thirdCell;
			}
			else if ((fourthCell != String.Empty) && (fifthCell == String.Empty))
			{
				cellPos = DateTimeEditCellPos.Fourth;
				currentCell = fourthCell;
			}
			else if ((fifthCell != String.Empty) && (sixthCell == String.Empty))
			{
				cellPos = DateTimeEditCellPos.Fifth;
				currentCell = fifthCell;
			}
			else if ((sixthCell != String.Empty) && (seventhCell == String.Empty))
			{
				cellPos = DateTimeEditCellPos.Sixth;
				currentCell = sixthCell;
			}
			else
			{
				cellPos = DateTimeEditCellPos.None;
				currentCell = String.Empty;
			}

			int firstSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator);
			int secondSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.DateSeparator, firstSeparatorIndex + 1);
			int dateAndTimeSeparatorIndex = Text.IndexOfAny(new char[] { ' ', 'T' });
			int firstTimeSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.TimeSeparator, dateAndTimeSeparatorIndex + 1);
			int secondTimeSeparatorIndex = Text.IndexOf(_dateTimeFormatInfo.TimeSeparator, firstTimeSeparatorIndex + 1);
			bool hasFirstDateSeparator = firstSeparatorIndex > 0;
			bool hasSecondDateSeparator = secondSeparatorIndex > firstSeparatorIndex;
			bool hasDateAndTimeSeparator = dateAndTimeSeparatorIndex > 0;
			bool hasFirstTimeSeparator = firstTimeSeparatorIndex > dateAndTimeSeparatorIndex;
			bool hasSecondTimeSeparator = secondTimeSeparatorIndex > firstTimeSeparatorIndex;
			if ((cellPos == DateTimeEditCellPos.First) && hasFirstDateSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos == DateTimeEditCellPos.Second) && hasSecondDateSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos == DateTimeEditCellPos.Third) && hasDateAndTimeSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos == DateTimeEditCellPos.Fourth) && hasFirstTimeSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos == DateTimeEditCellPos.Fifth) && hasSecondTimeSeparator)
				cellPos = DateTimeEditCellPos.None;
			if ((cellPos != DateTimeEditCellPos.None) && (currentCell.Length == GetFormatCellLength(cellPos)))
			{
				switch (cellPos)
				{
					case DateTimeEditCellPos.First:
					case DateTimeEditCellPos.Second:
						AddDateSeparator();
					break;

					case DateTimeEditCellPos.Third:
						if (!_hideTime)
							AddDateAndTimeSeparator();
					break;

					case DateTimeEditCellPos.Fourth:
					case DateTimeEditCellPos.Fifth:
						AddTimeSeparator();
					break;
				}
			}
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

		/// <summary>
		/// Returns the cell in which the cursor is currently positioned.
		/// </summary>
		/// <returns></returns>
		private DateTimeEditCell InternalGetEditingCell()
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			ParseDateTimeCells(Text, ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			int currentLength = firstCell.Length;
			if (!_hideDate)
			{
				if (SelectionStart <= currentLength)
				{
					if ((SelectionLength >= Text.Length) && (currentLength != Text.Length))
						return DateTimeEditCell.None;
					else
						return GetFormatCellType(DateTimeEditCellPos.First);
				}

				currentLength += DateSeparator.Length + secondCell.Length;
				if (SelectionStart <= currentLength)
					return GetFormatCellType(DateTimeEditCellPos.Second);

				currentLength += DateSeparator.Length + thirdCell.Length;
				if (SelectionStart <= currentLength)
					return GetFormatCellType(DateTimeEditCellPos.Third);

				currentLength += DateAndTimeSeparator.Length + fourthCell.Length;
				if (SelectionStart <= currentLength)
					return GetFormatCellType(DateTimeEditCellPos.Fourth);
			}
			else
			{
				currentLength = fourthCell.Length;
				if (SelectionStart <= currentLength)
				{
					if ((SelectionLength >= Text.Length) && (currentLength != Text.Length))
						return DateTimeEditCell.None;
					else
						return GetFormatCellType(DateTimeEditCellPos.Fourth);
				}
			}
			
			currentLength += TimeSeparator.Length + fifthCell.Length;
			if (SelectionStart <= currentLength)
				return GetFormatCellType(DateTimeEditCellPos.Fifth);

			currentLength += TimeSeparator.Length + sixthCell.Length;
			if (SelectionStart <= currentLength)
				return GetFormatCellType(DateTimeEditCellPos.Sixth);

			currentLength += TimeSeparator.Length + seventhCell.Length;
			if (SelectionStart <= currentLength)
				return GetFormatCellType(DateTimeEditCellPos.Seventh);

			return DateTimeEditCell.None;
		}

		private void InternalSetEditCell(DateTimeEditCell value)
		{
			string firstCell = String.Empty, secondCell = String.Empty, thirdCell = String.Empty, fourthCell = String.Empty, fifthCell = String.Empty, sixthCell = String.Empty, seventhCell = String.Empty;
			ParseDateTimeCells(Text, ref firstCell, ref secondCell, ref thirdCell, ref fourthCell, ref fifthCell, ref sixthCell, ref seventhCell);
			if (_hideDate)
			{
				switch (GetFormatCellPos(value))
				{
					case DateTimeEditCellPos.Fourth:
						SelectionStart = 0;
						SelectionLength = fourthCell.Length;
						break;
					case DateTimeEditCellPos.Fifth:
						SelectionStart = fourthCell.Length + TimeSeparator.Length;
						SelectionLength = fifthCell.Length;
						break;
					case DateTimeEditCellPos.Sixth:
						SelectionStart = fourthCell.Length + fifthCell.Length + (2 * TimeSeparator.Length);
						SelectionLength = sixthCell.Length;
						break;
					case DateTimeEditCellPos.Seventh:
						SelectionStart = fourthCell.Length + fifthCell.Length + sixthCell.Length + (3 * TimeSeparator.Length);
						SelectionLength = seventhCell.Length;
						break;
					default:
						SelectionStart = 0;
						SelectionLength = 0;
						break;
				}
			}
			else if (_hideTime)
			{
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
			else
			{
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
					case DateTimeEditCellPos.Fourth:
						SelectionStart = firstCell.Length + secondCell.Length + thirdCell.Length + (3 * DateSeparator.Length); // Assumption is that the length of the date separator and the date/time separator are the same...
						SelectionLength = fourthCell.Length;
						break;
					case DateTimeEditCellPos.Fifth:
						SelectionStart = firstCell.Length + secondCell.Length + thirdCell.Length + (3 * DateSeparator.Length) + fourthCell.Length + TimeSeparator.Length;
						SelectionLength = fifthCell.Length;
						break;
					case DateTimeEditCellPos.Sixth:
						SelectionStart = firstCell.Length + secondCell.Length + thirdCell.Length + (3 * DateSeparator.Length) + fourthCell.Length + fifthCell.Length + (2 * TimeSeparator.Length);
						SelectionLength = sixthCell.Length;
						break;
					case DateTimeEditCellPos.Seventh:
						SelectionStart = firstCell.Length + secondCell.Length + thirdCell.Length + (3 * DateSeparator.Length) + fourthCell.Length + fifthCell.Length + sixthCell.Length + (3 * TimeSeparator.Length);
						SelectionLength = seventhCell.Length;
						break;
					default:
						SelectionStart = 0;
						SelectionLength = 0;
						break;
				}
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
