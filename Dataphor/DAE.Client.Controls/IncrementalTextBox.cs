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

		public bool ExtractValue(DAE.Runtime.Data.IRow row)
		{
			try
			{
				if (HasValue)
					((DAE.Runtime.Data.Scalar)row.GetValue(_columnName)).AsString = Text;
				else
					row.ClearValue(_columnName);
				return true;
			}
			catch
			{
				ConversionFailed = true;
				return false;
			}
		}

		public void InjectValue(DAE.Runtime.Data.IRow row, bool overwrite)
		{
			if (overwrite)
			{
				if (row.HasValue(ColumnName))
				{
					SetHasValue(true);
					Text = ((DAE.Runtime.Data.Scalar)row.GetValue(ColumnName)).AsString;
				}
				else
				{
					SetHasValue(false);
					Text = String.Empty;
				}
			}
			else
			{
				if (row.HasValue(ColumnName) && (Text != String.Empty))
				{
					// Show partial match
					// TODO: More intelligent mechanism for displaying partial matches
					string newValue = ((DAE.Runtime.Data.Scalar)row.GetValue(ColumnName)).AsString;
					if (newValue.ToLower().StartsWith(Text.ToLower()))
					{
						int previousTextLength = Text.Length;
						Text = newValue;
						Select(previousTextLength, newValue.Length - previousTextLength);
					}
				}
			}
		}

		private string _columnName;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = (value == null ? String.Empty : value); }
		}

		private string _title = null;
		public string Title
		{
			get 
			{ 
				if (_title == null)
					return ColumnName;
				else
					return _title;
			}
		}

		public void Initialize(IncrementalSearchColumn column)
		{
			Width = column.ControlWidth;
			if ((column.Title != null) && (column.Title != String.Empty))
				_title = column.Title;
			else
				_title = null;
			TextAlign = column.TextAlignment;
		}

		public void UpdateStyle(IncrementalSearch search)
		{
			NoValueBackColor = search.NoValueBackColor;
			InvalidValueBackColor = search.InvalidValueBackColor;
		}

		public event EventHandler OnChanged;
		
		protected override void OnTextChanged(EventArgs args)
		{
			base.OnTextChanged(args);
			ConversionFailed = false;
			_hasValue = true;
			UpdateBackColor();
			if (OnChanged != null)
				OnChanged(this, args);
		}

		private bool _conversionFailed;
		protected bool ConversionFailed
		{
			get { return _conversionFailed; }
			set 
			{
				if (_conversionFailed != value)
				{
					_conversionFailed = value;
					UpdateBackColor();
				}
			}
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			if (HasValue && (SelectionLength == 0))
				SelectAll();
		}

		private Color _invalidValueBackColor = ControlColor.ConversionFailBackColor;
		public Color InvalidValueBackColor
		{
			get { return _invalidValueBackColor; }
			set
			{
				if (_invalidValueBackColor != value)
				{
					_invalidValueBackColor = value;
					if (_conversionFailed)
						UpdateBackColor();
				}
			}
		}

		protected override void InternalUpdateBackColor()
		{
			base.InternalUpdateBackColor();
			if (_conversionFailed)
				BackColor = _invalidValueBackColor;
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			// Process backspace as though selection didn't exist
			if ((key == Keys.Back) && (SelectionLength > 0) && (SelectionStart > 0))
			{
				int start = SelectionStart - 1;
				Text = Text.Remove(start, SelectionLength + 1);
				SelectionStart = start;
				return true;
			}
			else
				return base.ProcessDialogKey(key);
		}

		public void Reset()
		{
			ConversionFailed = false;
			Text = String.Empty;
			SetHasValue(false);
		}

		private bool _hasValue = false;
		public override bool HasValue 
		{	
			get { return _hasValue; } 
		}

		private void SetHasValue(bool value)
		{
			if (value != _hasValue)
			{
				_hasValue = value;
				UpdateBackColor();
			}
		}

	}
}
