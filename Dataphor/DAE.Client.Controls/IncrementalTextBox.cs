/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[ToolboxItem(false)]
	public class IncrementalTextBox : ExtendedTextBox, IIncrementalControl
	{
		public IncrementalTextBox() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			UpdateBackColor();
		}

		public bool ExtractValue(DAE.Runtime.Data.Row ARow)
		{
			try
			{
				if (HasValue)
					((DAE.Runtime.Data.Scalar)ARow.GetValue(FColumnName)).AsString = Text;
				else
					ARow.ClearValue(FColumnName);
				return true;
			}
			catch
			{
				ConversionFailed = true;
				return false;
			}
		}

		public void InjectValue(DAE.Runtime.Data.Row ARow, bool AOverwrite)
		{
			if (AOverwrite)
			{
				if (ARow.HasValue(ColumnName))
				{
					SetHasValue(true);
					Text = ((DAE.Runtime.Data.Scalar)ARow.GetValue(ColumnName)).AsString;
				}
				else
				{
					SetHasValue(false);
					Text = String.Empty;
				}
			}
			else
			{
				if (ARow.HasValue(ColumnName) && (Text != String.Empty))
				{
					// Show partial match
					// TODO: More intelligent mechanism for displaying partial matches
					string LNewValue = ((DAE.Runtime.Data.Scalar)ARow.GetValue(ColumnName)).AsString;
					if (LNewValue.ToLower().StartsWith(Text.ToLower()))
					{
						int LPreviousTextLength = Text.Length;
						Text = LNewValue;
						Select(LPreviousTextLength, LNewValue.Length - LPreviousTextLength);
					}
				}
			}
		}

		private string FColumnName;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = (value == null ? String.Empty : value); }
		}

		private string FTitle = null;
		public string Title
		{
			get 
			{ 
				if (FTitle == null)
					return ColumnName;
				else
					return FTitle;
			}
		}

		public void Initialize(IncrementalSearchColumn AColumn)
		{
			Width = AColumn.ControlWidth;
			if ((AColumn.Title != null) && (AColumn.Title != String.Empty))
				FTitle = AColumn.Title;
			else
				FTitle = null;
			TextAlign = AColumn.TextAlignment;
		}

		public void UpdateStyle(IncrementalSearch ASearch)
		{
			NoValueBackColor = ASearch.NoValueBackColor;
			InvalidValueBackColor = ASearch.InvalidValueBackColor;
		}

		public event EventHandler OnChanged;
		
		protected override void OnTextChanged(EventArgs AArgs)
		{
			base.OnTextChanged(AArgs);
			ConversionFailed = false;
			FHasValue = true;
			UpdateBackColor();
			if (OnChanged != null)
				OnChanged(this, AArgs);
		}

		private bool FConversionFailed;
		protected bool ConversionFailed
		{
			get { return FConversionFailed; }
			set 
			{
				if (FConversionFailed != value)
				{
					FConversionFailed = value;
					UpdateBackColor();
				}
			}
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			if (HasValue && (SelectionLength == 0))
				SelectAll();
		}

		private Color FInvalidValueBackColor = ControlColor.ConversionFailBackColor;
		public Color InvalidValueBackColor
		{
			get { return FInvalidValueBackColor; }
			set
			{
				if (FInvalidValueBackColor != value)
				{
					FInvalidValueBackColor = value;
					if (FConversionFailed)
						UpdateBackColor();
				}
			}
		}

		protected override void InternalUpdateBackColor()
		{
			base.InternalUpdateBackColor();
			if (FConversionFailed)
				BackColor = FInvalidValueBackColor;
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			// Process backspace as though selection didn't exist
			if ((AKey == Keys.Back) && (SelectionLength > 0) && (SelectionStart > 0))
			{
				int LStart = SelectionStart - 1;
				Text = Text.Remove(LStart, SelectionLength + 1);
				SelectionStart = LStart;
				return true;
			}
			else
				return base.ProcessDialogKey(AKey);
		}

		public void Reset()
		{
			ConversionFailed = false;
			Text = String.Empty;
			SetHasValue(false);
		}

		private bool FHasValue = false;
		public override bool HasValue 
		{	
			get { return FHasValue; } 
		}

		private void SetHasValue(bool AValue)
		{
			if (AValue != FHasValue)
			{
				FHasValue = AValue;
				UpdateBackColor();
			}
		}

	}
}
