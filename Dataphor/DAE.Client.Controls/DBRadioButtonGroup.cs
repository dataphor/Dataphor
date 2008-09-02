/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Specialized;
using DAEClient = Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBRadioButtonGroup),"Icons.DBRadioButtonGroup.bmp")]
	public class DBRadioButtonGroup	: RadioButtonGroup, DAEClient.IDataSourceReference, DAEClient.IColumnNameReference, DAEClient.IReadOnly
	{
		public DBRadioButtonGroup()
		{
			FLink = new DAEClient.FieldDataLink();
			FLink.OnFieldChanged += new DAEClient.DataLinkFieldHandler(FieldChanged);
			FLink.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			FLink.OnSaveRequested += new DAEClient.DataLinkHandler(SaveRequested);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FLink != null)
			{
				FLink.Dispose();
				FLink = null;
			}
			base.Dispose(ADisposing);
		}

		private DAEClient.FieldDataLink FLink;
		protected DAEClient.FieldDataLink Link { get { return FLink; } }

		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly 
		{
			get { return FLink.ReadOnly; }
			set { FLink.ReadOnly = value; }
		}

		[Category("Data")]
		[DefaultValue(null)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FLink.ColumnName; }
			set { FLink.ColumnName = value; }
		}

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		public DAEClient.DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value;	 }
		}

		private bool FAutoUpdate = true;
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set { FAutoUpdate = value; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DAEClient.DataField DataField { get { return FLink.DataField; } }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		new public virtual int SelectedIndex
		{
			get { return base.SelectedIndex; }
			set
			{
				if (base.SelectedIndex != value)
				{
					EnsureEdit();
					base.SelectedIndex = value;
					FLink.SaveRequested();
				}
			}
		}

		private void EnsureEdit()
		{
			if (!FLink.Edit())
				throw new ControlsException(ControlsException.Codes.InvalidViewState);
		}

		private bool FEnabled = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		public new bool Enabled
		{
			get { return FEnabled; }
			set 
			{
				if (FEnabled != value)
				{
					FEnabled = value; 
					UpdateEnabled();
				}
			}
		}

		public virtual bool GetEnabled()
		{
			return FEnabled && !FLink.ReadOnly;
		}

		protected virtual void UpdateEnabled()
		{
			base.Enabled = GetEnabled();
		}
		
		protected void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			UpdateEnabled();
		}
		
		private string[] FValues = new string[] {};
		[Category("Data")]
		[Description("Values for the Items.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual string[] Values
		{
			get { return FValues; }
			set
			{
				if (FValues != value)
				{
					if (value == null)
						FValues = new string[] {};
					else
						FValues = value;
					UpdateSelectedIndex();
				}
			}
		}

		private string FDataValue;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string DataValue
		{
			get { return FDataValue; }
			set 
			{ 
				if (FDataValue != value)
				{
					FDataValue = value;
					UpdateSelectedIndex();
				}
			}
		}

		private bool FSettingSelectedIndex;
		protected virtual void UpdateSelectedIndex()
		{
			FSettingSelectedIndex = true;
			try
			{
				if (FDataValue == null)
					base.SelectedIndex = -1;
				else
				{
					int LIndex = -1;
					for (int i = 0; i < FValues.Length; i++)
						if (FValues[i] == FDataValue)
						{
							LIndex = i;
							break;
						}
					base.SelectedIndex = LIndex;
				}
			}
			finally
			{
				FSettingSelectedIndex = false;	
			}
		}

		protected virtual void FieldChanged(DAEClient.DataLink ADataLink, DAEClient.DataSet ADataSet, DAEClient.DataField AField)
		{
			if ((FLink.DataField != null) && FLink.DataField.HasValue())
				DataValue = FLink.DataField.AsString;
			else
				DataValue = null;
		}

		protected virtual void SaveRequested(DAEClient.DataLink ALink, DAEClient.DataSet ADataSet)
		{
			if (FLink.DataField != null)
			{
				if (FDataValue == null)
					FLink.DataField.ClearValue();
				else
					FLink.DataField.AsString = FDataValue;
			}
		}

		protected override void OnSelectedChange(EventArgs AArgs)
		{
			base.OnSelectedChange(AArgs);
			if (!FSettingSelectedIndex && (SelectedIndex >= 0) && (SelectedIndex < FValues.Length))
			{
				FDataValue = FValues[SelectedIndex];
				if (FAutoUpdate)
					FLink.SaveRequested();
			}
		}

		protected override bool CanChange()
		{
			if (FSettingSelectedIndex)
				return true;
			else
				return FLink.Edit();
		}

		protected override bool IsInputKey(Keys AKeyData)
		{
			switch (AKeyData) 
			{
				case Keys.Escape:
					if (FLink.Modified)
						return true;
					break;
			}
			return base.IsInputKey(AKeyData);
		}

		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			base.OnKeyDown(AArgs);
			switch (AArgs.KeyData)
			{
				case Keys.Escape :
					FLink.Reset();
					AArgs.Handled = true;
					break;
			}
		}

		protected override void OnLeave(EventArgs AEventArgs)
		{
			if (FLink != null)
				try
				{
					FLink.SaveRequested();
				}
				catch
				{
					Focus();
					throw;
				}
			base.OnLeave(AEventArgs);
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
				Focus();
		}
	}
}
