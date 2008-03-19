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
			FDateTimeFormatInfo = DateTimeFormatInfo.CurrentInfo;
			Link.OnActiveChanged += new DataLinkHandler(ActiveChanged);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
				Link.OnActiveChanged -= new DataLinkHandler(ActiveChanged);
			base.Dispose(ADisposing);
		}

		protected override string FieldValue
		{
			get { return ((DataField == null) || (!DataField.HasValue())) ? String.Empty : InternalConvertDateTimeToString(DataField.AsDateTime); }
			set { DataField.AsDateTime = Convert.ToDateTime(value); }
		}

		private DateTimeFormatInfo FDateTimeFormatInfo;
		protected DateTimeFormatInfo DateTimeFormatInfo
		{
			get { return FDateTimeFormatInfo; }
		}

		protected string ShortDatePattern { get { return FDateTimeFormatInfo.ShortDatePattern; } }
		protected string LongTimePattern { get { return FDateTimeFormatInfo.LongTimePattern; } }

		private bool FEditByCell = true;
		/// <summary> Validate individual month, day, and year input. </summary>
		/// <remarks> Month, day, and year order is determined by culture settings. </remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Validate individual month, day, and year input.")]
		public bool EditByCell
		{
			get { return FEditByCell; }
			set { FEditByCell = value; }
		}

		private bool FAutoComplete = true;
		/// <summary> Complete user input when applicable. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Complete user input add date separators when applicable.")]
		public bool AutoComplete
		{
			get { return FAutoComplete; }
			set
			{
				if (FAutoComplete != value)
					FAutoComplete = value;
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

		private string InternalConvertDateTimeToString(DateTime ADateTime)
		{
			if (EditByCell)
				return ADateTime.TimeOfDay.Ticks == 0 ? ADateTime.ToString(ShortDatePattern) : ADateTime.ToString(String.Format("{0} {1}", ShortDatePattern, LongTimePattern));
			else
				return ADateTime.ToString("G");
		}

		protected void InternalSetDate(DateTime ADateTime)
		{
			InternalSetText(InternalConvertDateTimeToString(ADateTime));
		}

		private void ActiveChanged(DataLink ALink, DataSet ADataSet)
		{
			// BTR -> This control should work with any data type that exposes an AsDateTime representation.
			//if ((DataField != null) && !(DataField.DataType.Is(AView.Process.DataTypes.SystemDateTime))&& !(DataField.DataType.Is(AView.Process.DataTypes.SystemDate)))
			//	throw new ControlsException(ControlsException.Codes.InvalidColumn, ColumnName);
		}

		private DateTime IncEditCell(DateTime ADate, DateTimeEditCell ACell, int AStep)
		{
			switch (ACell)
			{
				case DateTimeEditCell.Day:
					return ADate.AddDays((double)AStep);
				case DateTimeEditCell.Month:
					return ADate.AddMonths(AStep);
				case DateTimeEditCell.Year:
					return ADate.AddYears(AStep);
				default:
					return ADate;
			}
		}

		private bool CanAddSeparator()
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			DateTimeEditCellPos LCellPos;
			ParseDateCells(Text, ref LFirstCell, ref LSecondCell, ref LThirdCell);
			if ((LFirstCell.Length > 0) && (LSecondCell.Length == 0))
				LCellPos = DateTimeEditCellPos.First;
			else if ((LSecondCell.Length > 0) && (LThirdCell.Length == 0))
				LCellPos = DateTimeEditCellPos.Second;
			else
				LCellPos = DateTimeEditCellPos.None;

			int LFirstSeparatorIndex = Text.IndexOf(FDateTimeFormatInfo.DateSeparator);
			int LSecondSeparatorIndex = Text.IndexOf(FDateTimeFormatInfo.DateSeparator, LFirstSeparatorIndex + 1);
			bool LHasFirstDateSeparator = LFirstSeparatorIndex > 0;
			bool LHasSecondDateSeparator = LSecondSeparatorIndex > LFirstSeparatorIndex;

			return ((LCellPos == DateTimeEditCellPos.First) && !LHasFirstDateSeparator)
				|| ((LCellPos == DateTimeEditCellPos.Second) && !LHasSecondDateSeparator);
		}

		private void BackSpaceCleanup(KeyEventArgs AArgs)
		{
			if (FAutoComplete && (Text.Length > 0) && !CanAddSeparator() && ((Text.LastIndexOf(DateSeparator) + DateSeparator.Length) == Text.Length))
			{
				int LSaveSelectionStart = SelectionStart;
				FInternalDisableAutoComplete = true;
				try
				{
					InternalSetText(Text.Substring(0, Text.Length - DateSeparator.Length));
				}
				finally
				{
					FInternalDisableAutoComplete = false;
					SelectionStart = LSaveSelectionStart;
				}
			}	
		}

		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			base.OnKeyDown(AArgs);
			switch (AArgs.KeyData)
			{
				case Keys.Left:
					if (FEditByCell && (AArgs.Modifiers == Keys.None))
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
						AArgs.Handled = true;
					}
					break;
				case Keys.Right:
					if (FEditByCell && (AArgs.Modifiers == Keys.None))
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
						AArgs.Handled = true;
					}
					break;
				case Keys.Up:
				case Keys.Down:
					if (FEditByCell && (AArgs.Modifiers == Keys.None) && Link.Active)
					{
						Link.Edit();
						DateTimeEditCell LSaveEditCell = EditCell;
						try
						{
							if (AArgs.KeyData == Keys.Up)
								InternalSetDate(IncEditCell(Date, EditCell, 1));
							else
								InternalSetDate(IncEditCell(Date, EditCell, -1));
						}
						finally
						{
							EditCell = LSaveEditCell;
						}
						AArgs.Handled = true;
					}
					break;
				case Keys.H:
					if (AArgs.Control)
						BackSpaceCleanup(AArgs);
					break;
				case Keys.Back :
					BackSpaceCleanup(AArgs);
					break;
			}
		}

		private bool CheckValidKey(char AKey)
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			string LCurrentCell;
			ParseDateCells(Text, ref LFirstCell, ref LSecondCell, ref LThirdCell);
			
			switch (EditingCell)
			{
				case DateTimeEditCell.Day:
					int LMonth = 0;
					switch (GetFormatCellPos(DateTimeEditCell.Month))
					{
						case DateTimeEditCellPos.First:
							if (LFirstCell != String.Empty)
								LMonth = Convert.ToInt32(LFirstCell);
							break;
						case DateTimeEditCellPos.Second:
							if (LSecondCell != String.Empty)
								LMonth = Convert.ToInt32(LSecondCell);
							break;
						case DateTimeEditCellPos.Third:
							if (LThirdCell != String.Empty)
								LMonth = Convert.ToInt32(LThirdCell);
							break;
					}

					int LYear = 0;
					switch (GetFormatCellPos(DateTimeEditCell.Year))
					{
						case DateTimeEditCellPos.First:
							if (LFirstCell != String.Empty)
								LYear = Convert.ToInt32(LFirstCell);
							break;
						case DateTimeEditCellPos.Second:
							if (LSecondCell != String.Empty)
								LYear = Convert.ToInt32(LSecondCell);
							break;
						case DateTimeEditCellPos.Third:
							if (LThirdCell != String.Empty)
								LYear = Convert.ToInt32(LThirdCell);
							break;
					}

					//Thirty days hath September, April, June and November
					//all the rest have thirty one except for grandma she drives a Buick.
					
					int LMinDay = 1, LMaxDay = 31;
					switch (LMonth)
					{
						case 2:
							if ((LYear == 0) || DateTime.IsLeapYear(LYear))
								LMaxDay = 29;
							else
								LMaxDay = 28;
							break;
						case 4:
						case 6:
						case 9:
						case 11:
							LMaxDay = 30;
							break;
					}

					switch (GetFormatCellPos(DateTimeEditCell.Day))
					{
						case DateTimeEditCellPos.First : LCurrentCell = LFirstCell;
							break;
						case DateTimeEditCellPos.Second : LCurrentCell = LSecondCell;
							break;
						case DateTimeEditCellPos.Third : LCurrentCell = LThirdCell;
							break;
						default : LCurrentCell = String.Empty;
							break;
					}

					if (SelectionLength > 0)
						LCurrentCell = String.Empty;
					if (LCurrentCell == String.Empty)
						LMinDay = 0;
					int LDayToTest = Convert.ToInt32(LCurrentCell + AKey);
					return (LDayToTest >= LMinDay) && (LDayToTest <= LMaxDay);
				case DateTimeEditCell.Month:
					switch (GetFormatCellPos(DateTimeEditCell.Month))
					{
						case DateTimeEditCellPos.First : LCurrentCell = LFirstCell;
							break;
						case DateTimeEditCellPos.Second : LCurrentCell = LSecondCell;
							break;
						case DateTimeEditCellPos.Third : LCurrentCell = LThirdCell;
							break;
						default : LCurrentCell = String.Empty;
							break;
					}

					int LMinMonth = 1, LMaxMonth = 12;

					if (SelectionLength > 0)
						LCurrentCell = String.Empty;
					if (LCurrentCell == String.Empty)
						LMinMonth = 0;
					int LMonthToTest = Convert.ToInt32(LCurrentCell + AKey);
					return (LMonthToTest >= LMinMonth) && (LMonthToTest <= LMaxMonth);
				default:
					return true;
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs AArgs)
		{
			base.OnKeyPress(AArgs);
			if ((AArgs.KeyChar == '+') || (AArgs.KeyChar == '-'))
			{
				Link.Edit();
				if (AArgs.KeyChar == '+')
					InternalSetDate(Date.AddDays(1));
				else
					InternalSetDate(Date.AddDays(-1));
				AArgs.Handled = true;
			}
			else if ((DateSeparator.Length > 0) && (DateSeparator[0] == AArgs.KeyChar))
				AArgs.Handled = !CanAddSeparator();
			else if ((AArgs.KeyChar >= (char)32) && (AArgs.KeyChar <= (char)255))
				if (FEditByCell)
					AArgs.Handled = !CheckValidKey(AArgs.KeyChar);
		}

		private bool FInternalDisableAutoComplete;
		protected bool InternalDisableAutoComplete
		{
			get { return FInternalDisableAutoComplete; }
			set { FInternalDisableAutoComplete = value; }
		}

		protected override void OnTextChanged(EventArgs AArgs)
		{
			base.OnTextChanged(AArgs);
			if (FAutoComplete && !InternalDisableAutoComplete)
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

		private void ParseDateCells(string AText, ref string AFirstCell, ref string ASecondCell, ref string AThirdCell)
		{
			string[] LDateStrings = AText.Split(DateSeparator.ToCharArray());
			AFirstCell = LDateStrings[0];
			if (LDateStrings.Length > 1)
				ASecondCell = LDateStrings[1];
			if (LDateStrings.Length > 2)
				AThirdCell = LDateStrings[2];
		}

		private DateTimeEditCellPos GetFormatCellPos(DateTimeEditCell ACell)
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			ParseDateCells(ShortDatePattern.ToUpper(), ref LFirstCell, ref LSecondCell, ref LThirdCell);
			switch (ACell)
			{
				case DateTimeEditCell.Day:
					if (LFirstCell.IndexOf("D") >= 0)
						return DateTimeEditCellPos.First;
					if (LSecondCell.IndexOf("D") >= 0)
						return DateTimeEditCellPos.Second;
					if (LThirdCell.IndexOf("D") >= 0)
						return DateTimeEditCellPos.Third;
					return DateTimeEditCellPos.None;
				case DateTimeEditCell.Month:
					if (LFirstCell.IndexOf("M") >= 0)
						return DateTimeEditCellPos.First;
					if (LSecondCell.IndexOf("M") >= 0)
						return DateTimeEditCellPos.Second;
					if (LThirdCell.IndexOf("M") >= 0)
						return DateTimeEditCellPos.Third;
					return DateTimeEditCellPos.None;
				case DateTimeEditCell.Year:
					if (LFirstCell.IndexOf("Y") >= 0)
						return DateTimeEditCellPos.First;
					if (LSecondCell.IndexOf("Y") >= 0)
						return DateTimeEditCellPos.Second;
					if (LThirdCell.IndexOf("Y") >= 0)
						return DateTimeEditCellPos.Third;
					return DateTimeEditCellPos.None;
				default:
					return DateTimeEditCellPos.None;
			}
		}

		private DateTimeEditCell GetFormatCellType(DateTimeEditCellPos APos)
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			ParseDateCells(ShortDatePattern.ToUpper(), ref LFirstCell, ref LSecondCell, ref LThirdCell);
			switch (APos)
			{
				case DateTimeEditCellPos.First:
					if (LFirstCell.IndexOf("D") >= 0)
						return DateTimeEditCell.Day;
					if (LFirstCell.IndexOf("M") >= 0)
						return DateTimeEditCell.Month;
					if (LFirstCell.IndexOf("Y") >= 0)
						return DateTimeEditCell.Year;
					return DateTimeEditCell.None;
				case DateTimeEditCellPos.Second:
					if (LSecondCell.IndexOf("D") >= 0)
						return DateTimeEditCell.Day;
					if (LSecondCell.IndexOf("M") >= 0)
						return DateTimeEditCell.Month;
					if (LSecondCell.IndexOf("Y") >= 0)
						return DateTimeEditCell.Year;
					return DateTimeEditCell.None;
				case DateTimeEditCellPos.Third:
					if (LThirdCell.IndexOf("D") >= 0)
						return DateTimeEditCell.Day;
					if (LThirdCell.IndexOf("M") >= 0)
						return DateTimeEditCell.Month;
					if (LThirdCell.IndexOf("Y") >= 0)
						return DateTimeEditCell.Year;
					return DateTimeEditCell.None;
				default:
					return DateTimeEditCell.None;
			}
		}

		private int GetFormatCellLength(DateTimeEditCellPos APos)
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			ParseDateCells(ShortDatePattern.ToUpper(), ref LFirstCell, ref LSecondCell, ref LThirdCell);
			switch (GetFormatCellType(APos))
			{
				case DateTimeEditCell.Day:
				case DateTimeEditCell.Month:
					return 2;
				default:
					switch (APos)
					{
						case DateTimeEditCellPos.First:
							return LFirstCell.Length;
						case DateTimeEditCellPos.Second:
							return LSecondCell.Length;
						case DateTimeEditCellPos.Third:
							return LThirdCell.Length;
					}
					return 0;
			}
		}

		private void CheckAddDateSeparator()
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty, LCurrentCell;
			ParseDateCells(Text, ref LFirstCell, ref LSecondCell, ref LThirdCell);
			DateTimeEditCellPos LCellPos;
			if ((LFirstCell != String.Empty) && (LSecondCell == String.Empty))
			{
				LCellPos = DateTimeEditCellPos.First;
				LCurrentCell = LFirstCell;
			}
			else if ((LSecondCell != String.Empty) && (LThirdCell == String.Empty))
			{
				LCellPos = DateTimeEditCellPos.Second;
				LCurrentCell = LSecondCell;
			}
			else
			{
				LCellPos = DateTimeEditCellPos.None;
				LCurrentCell = String.Empty;
			}

			int LFirstSeparatorIndex = Text.IndexOf(FDateTimeFormatInfo.DateSeparator);
			int LSecondSeparatorIndex = Text.IndexOf(FDateTimeFormatInfo.DateSeparator, LFirstSeparatorIndex + 1);
			bool LHasFirstDateSeparator = LFirstSeparatorIndex > 0;
			bool LHasSecondDateSeparator = LSecondSeparatorIndex > LFirstSeparatorIndex;
			if ((LCellPos == DateTimeEditCellPos.First) && LHasFirstDateSeparator)
				LCellPos = DateTimeEditCellPos.None;
			if ((LCellPos == DateTimeEditCellPos.Second) && LHasSecondDateSeparator)
				LCellPos = DateTimeEditCellPos.None;
			if ((LCellPos != DateTimeEditCellPos.None) && (LCurrentCell.Length == GetFormatCellLength(LCellPos)))
				AddDateSeparator();
			else
			{
				switch (GetFormatCellType(LCellPos))
				{
					case DateTimeEditCell.Day:
						int LDay = Convert.ToInt32(LCurrentCell);
						if ((LDay >= 4) && (LDay <= 31))
							AddDateSeparator();
						break;
					case DateTimeEditCell.Month:
						int LMonth = Convert.ToInt32(LCurrentCell);
						if ((LMonth >= 2) && (LMonth <= 12))
							AddDateSeparator();
						break;
				}
			}
		}

		private DateTimeEditCell InternalGetEditingCell()
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			ParseDateCells(Text, ref LFirstCell, ref LSecondCell, ref LThirdCell);
			if (SelectionStart <= LFirstCell.Length)
			{
				if ((SelectionLength == Text.Length) && (LFirstCell.Length != Text.Length))
					return DateTimeEditCell.None;
				else
					return GetFormatCellType(DateTimeEditCellPos.First);
			}
			else if (SelectionStart <= (LFirstCell.Length + LSecondCell.Length + DateSeparator.Length))
				return GetFormatCellType(DateTimeEditCellPos.Second);
			else if (SelectionStart <= Text.Length)
				return GetFormatCellType(DateTimeEditCellPos.Third);
			else
				return DateTimeEditCell.None;
		}

		private void InternalSetEditCell(DateTimeEditCell AValue)
		{
			string LFirstCell = String.Empty, LSecondCell = String.Empty, LThirdCell = String.Empty;
			ParseDateCells(Text, ref LFirstCell, ref LSecondCell, ref LThirdCell);
			switch (GetFormatCellPos(AValue))
			{
				case DateTimeEditCellPos.First:
					SelectionStart = 0;
					SelectionLength = LFirstCell.Length;
					break;
				case DateTimeEditCellPos.Second:
					SelectionStart = LFirstCell.Length + DateSeparator.Length;
					SelectionLength = LSecondCell.Length;
					break;
				case DateTimeEditCellPos.Third:
					SelectionStart = LFirstCell.Length + LSecondCell.Length + (2 * DateSeparator.Length);
					SelectionLength = LThirdCell.Length;
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
