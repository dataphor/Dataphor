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

		public bool ExtractValue(DAE.Runtime.Data.Row ARow)
		{
			try
			{
				if (HasValue)
					((DAE.Runtime.Data.Scalar)ARow.GetValue(FColumnName)).AsBoolean = (base.CheckState == CheckState.Checked);
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
					if (((DAE.Runtime.Data.Scalar)ARow.GetValue(ColumnName)).AsBoolean)
						base.CheckState = CheckState.Checked;
					else
						base.CheckState = CheckState.Unchecked;
				else
					base.CheckState = CheckState.Indeterminate;
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
			if ((AColumn.Title != null) && (AColumn.Title != String.Empty))
				FTitle = AColumn.Title;
			else
				FTitle = null;
		}

		public void UpdateStyle(IncrementalSearch ASearch)
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

		protected override void OnCheckStateChanged(EventArgs AArgs)
		{
			base.OnCheckStateChanged(AArgs);
			ConversionFailed = false;
			if (OnChanged != null)
				OnChanged(this, AArgs);
		}

		private bool FConversionFailed;
		public bool ConversionFailed
		{
			get { return FConversionFailed; }
			set 
			{
				if (FConversionFailed != value)
				{
					FConversionFailed = value;
					// TODO: Visually indicate failed conversion for IncrementalCheckBox
				}
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			if (Focused)
				ControlPaint.DrawFocusRectangle(AArgs.Graphics, DisplayRectangle);
		}
	}
}
