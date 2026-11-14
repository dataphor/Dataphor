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
	public class IncrementalCheckBox : CheckBox, IIncrementalControl
	{
		public IncrementalCheckBox() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Color.Transparent;
			Text = String.Empty;
			base.ThreeState = false;
			base.CheckState = CheckState.Indeterminate;
			Width = SystemInformation.SmallIconSize.Width + 7;
			Height = Width;
			this.CheckAlign = ContentAlignment.MiddleCenter;
		}

		public bool ExtractValue(DAE.Runtime.Data.IRow row)
		{
			try
			{
				if (HasValue)
					((DAE.Runtime.Data.Scalar)row.GetValue(_columnName)).AsBoolean = (base.CheckState == CheckState.Checked);
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
					if (((DAE.Runtime.Data.Scalar)row.GetValue(ColumnName)).AsBoolean)
						base.CheckState = CheckState.Checked;
					else
						base.CheckState = CheckState.Unchecked;
				else
					base.CheckState = CheckState.Indeterminate;
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
			if ((column.Title != null) && (column.Title != String.Empty))
				_title = column.Title;
			else
				_title = null;
		}

		public void UpdateStyle(IncrementalSearch search)
		{
			// Nothing
		}

		public virtual void Reset()
		{
			ConversionFailed = false;
			base.CheckState = CheckState.Indeterminate;
		}

		public bool HasValue { get { return base.CheckState != CheckState.Indeterminate; } }

		public event EventHandler OnChanged;

		protected override void OnCheckStateChanged(EventArgs args)
		{
			base.OnCheckStateChanged(args);
			ConversionFailed = false;
			if (OnChanged != null)
				OnChanged(this, args);
		}

		private bool _conversionFailed;
		public bool ConversionFailed
		{
			get { return _conversionFailed; }
			set 
			{
				if (_conversionFailed != value)
				{
					_conversionFailed = value;
					// TODO: Visually indicate failed conversion for IncrementalCheckBox
				}
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			if (Focused)
				ControlPaint.DrawFocusRectangle(args.Graphics, DisplayRectangle);
		}
	}
}
