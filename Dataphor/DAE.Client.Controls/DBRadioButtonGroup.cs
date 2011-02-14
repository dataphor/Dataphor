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
			_link = new DAEClient.FieldDataLink();
			_link.OnFieldChanged += new DAEClient.DataLinkFieldHandler(FieldChanged);
			_link.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			_link.OnSaveRequested += new DAEClient.DataLinkHandler(SaveRequested);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			_autoUpdateInterval = 200;
			_autoUpdateTimer = new System.Windows.Forms.Timer();
			_autoUpdateTimer.Interval = _autoUpdateInterval;
			_autoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			_autoUpdateTimer.Enabled = false;
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				_autoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
				_autoUpdateTimer.Dispose();
				_autoUpdateTimer = null;
			}
			finally
			{
				if (_link != null)
				{
					_link.Dispose();
					_link = null;
				}
			}
			base.Dispose(disposing);
		}

		private DAEClient.FieldDataLink _link;
		protected DAEClient.FieldDataLink Link { get { return _link; } }

		private int _autoUpdateInterval;
		/// <summary> Determines the amount of time to wait before updating a DataField's value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBRadioButtonGroup.dxd"/>
		[DefaultValue(200)]
		[Category("Behavior")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				if (_autoUpdateInterval != value)
					_autoUpdateInterval = value;
			}
		}
		
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly 
		{
			get { return _link.ReadOnly; }
			set { _link.ReadOnly = value; }
		}
		
		private System.Windows.Forms.Timer _autoUpdateTimer;

		private bool _autoUpdate;
		/// <summary> Determines if the control should automatically update the DataField's value on a given interval. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBRadioButtonGroup.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				if (_autoUpdate != value)
					_autoUpdate = value;
			}
		}

		[Category("Data")]
		[DefaultValue(null)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set { _link.ColumnName = value; }
		}

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		public DAEClient.DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value;	 }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DAEClient.DataField DataField { get { return _link.DataField; } }

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
					_link.SaveRequested();
				}
			}
		}

		private void EnsureEdit()
		{
			if (!_link.Edit())
				throw new ControlsException(ControlsException.Codes.InvalidViewState);
		}

		private bool _enabled = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		public new bool Enabled
		{
			get { return _enabled; }
			set 
			{
				if (_enabled != value)
				{
					_enabled = value; 
					UpdateEnabled();
				}
			}
		}

		public virtual bool GetEnabled()
		{
			return _enabled && !_link.ReadOnly;
		}

		protected virtual void UpdateEnabled()
		{
			base.Enabled = GetEnabled();
		}
		
		protected void UpdateReadOnly(object sender, EventArgs args)
		{
			UpdateEnabled();
		}
		
		private string[] _values = new string[] {};
		[Category("Data")]
		[Description("Values for the Items.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual string[] Values
		{
			get { return _values; }
			set
			{
				if (_values != value)
				{
					if (value == null)
						_values = new string[] {};
					else
						_values = value;
					UpdateSelectedIndex();
				}
			}
		}

		private string _dataValue;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string DataValue
		{
			get { return _dataValue; }
			set 
			{ 
				if (_dataValue != value)
				{
					_dataValue = value;
					UpdateSelectedIndex();
				}
			}
		}

		private bool _settingSelectedIndex;
		protected virtual void UpdateSelectedIndex()
		{
			_settingSelectedIndex = true;
			try
			{
				if (_dataValue == null)
					base.SelectedIndex = -1;
				else
				{
					int index = -1;
					for (int i = 0; i < _values.Length; i++)
						if (_values[i] == _dataValue)
						{
							index = i;
							break;
						}
					base.SelectedIndex = index;
				}
			}
			finally
			{
				_settingSelectedIndex = false;	
			}
		}

		protected virtual void FieldChanged(DAEClient.DataLink dataLink, DAEClient.DataSet dataSet, DAEClient.DataField field)
		{
			if ((_link.DataField != null) && _link.DataField.HasValue())
				DataValue = _link.DataField.AsString;
			else
				DataValue = null;
		}

		protected virtual void SaveRequested(DAEClient.DataLink link, DAEClient.DataSet dataSet)
		{
			if (_link.DataField != null)
			{
				if (_dataValue == null)
					_link.DataField.ClearValue();
				else
					_link.DataField.AsString = _dataValue;
			}
		}

		/// <summary> Called when the AutoUpdateTimer has elapsed. </summary>
		/// <param name="sender"> The object whose delegate is called. </param>
		/// <param name="args"> An EventArgs that contains data related to this event. </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBRadioButtonGroup.dxd"/>
		protected virtual void AutoUpdateElapsed(object sender, EventArgs args)
		{
			_autoUpdateTimer.Stop();
			_link.SaveRequested();
		}
		
		protected override void OnSelectedChange(EventArgs args)
		{
			base.OnSelectedChange(args);
			if (!_settingSelectedIndex && (SelectedIndex >= 0) && (SelectedIndex < _values.Length))
			{
				_dataValue = _values[SelectedIndex];
				if (_autoUpdate)
				{
					if (_autoUpdateInterval <= 0)
						_link.SaveRequested();
					else
					{
						_autoUpdateTimer.Interval = _autoUpdateInterval;
						_autoUpdateTimer.Start();
					}
				}
			}
		}

		protected override bool CanChange()
		{
			if (_settingSelectedIndex)
				return true;
			else
				return _link.Edit();
		}

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData) 
			{
				case Keys.Escape:
					if (_link.Modified)
						return true;
					break;
			}
			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			base.OnKeyDown(args);
			switch (args.KeyData)
			{
				case Keys.Escape :
					_link.Reset();
					args.Handled = true;
					break;
			}
		}

		protected override void OnLeave(EventArgs eventArgs)
		{
			if (_link != null)
				try
				{
					_link.SaveRequested();
				}
				catch
				{
					Focus();
					throw;
				}
			base.OnLeave(eventArgs);
		}

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			if (field == DataField)
				Focus();
		}
	}
}
