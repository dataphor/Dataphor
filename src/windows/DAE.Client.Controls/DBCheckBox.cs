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
	using Alphora.Dataphor.DAE.Client;

	/// <summary> Data-aware checkbox control. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBCheckBox),"Icons.DBCheckBox.bmp")]
	public class DBCheckBox : CheckBox, IDataSourceReference, IColumnNameReference, IReadOnly
	{
		/// <summary> Initializes a new instance of the <c>DBCheckBox</c> class. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		public DBCheckBox() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			CausesValidation = false;
			_link = new FieldDataLink();
			_link.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			_link.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			_link.OnSaveRequested += new DataLinkHandler(SaveRequested);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			_autoUpdateInterval = 200;
			base.ThreeState = false;
			base.CheckState = CheckState.Indeterminate;
			base.AutoCheck = false;
			_autoUpdateTimer = new System.Windows.Forms.Timer();
			_autoUpdateTimer.Interval = _autoUpdateInterval;
			_autoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			_autoUpdateTimer.Enabled = false;
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				try
				{
					_autoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
					_autoUpdateTimer.Dispose();
					_autoUpdateTimer = null;
				}
				finally
				{
					_link.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
					_link.OnUpdateReadOnly -= new EventHandler(UpdateReadOnly);
					_link.OnSaveRequested -= new DataLinkHandler(SaveRequested);
					_link.Dispose();
					_link = null;
				}
			}
			base.Dispose(disposing);
		}

		private FieldDataLink _link;
		/// <summary> Links this control to a view's DataField. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		protected FieldDataLink Link
		{
			get { return _link; }
		}

		private System.Windows.Forms.Timer _autoUpdateTimer;

		private bool _autoUpdate;
		/// <summary> Determines if the control should automatically update the DataField's value on a given interval. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
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

        private bool _trueFirst = true;
        /// <summary> Determines the CheckState transition sequence for CheckBoxes with three-states. </summary>
        /// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
        [DefaultValue(true)]
        [Category("Behavior")]
        public bool TrueFirst
        {
            get { return _trueFirst; }
            set { _trueFirst = value; }
        }
        
        private int _autoUpdateInterval;
		/// <summary> Determines the amount of time to wait before updating a DataField's value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
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

		/// <summary> Gets or sets a value indicating whether the CheckBox is read-only. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[DefaultValue(false)]
		[Category("Data")]
		public bool ReadOnly 
		{
			get { return _link.ReadOnly; }
			set	{ _link.ReadOnly = value; }
		}

		/// <summary> Gets or sets a value indicating the column name from which the CheckBox control displays data. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[DefaultValue(null)]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set { _link.ColumnName = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the CheckBox is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		public DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value;	}
		}

		/// <summary> Gets the DataField the CheckBox represents. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[Browsable(false)]
		public DataField DataField
		{
			get { return _link.DataField; }
		}

		private void EnsureEdit()
		{
			if (!_link.Edit())
				throw new ControlsException(ControlsException.Codes.InvalidViewState);
		}

		[DefaultValue(false)]
		public new bool Enabled
		{
			get { return base.Enabled; }
			set { base.Enabled = value; }
		}

		private void UpdateReadOnly(object sender, EventArgs args)
		{
			if (!DesignMode)
			{
				SetStyle(ControlStyles.Selectable, _link.Active && !_link.ReadOnly);
				base.Enabled = _link.Active && !_link.ReadOnly;
			}
		}

		private void FieldChanged(DataLink link, DataSet dataSet, DataField field)
		{
			base.CheckState = GetCheckState();
		}

		private void SaveRequested(DataLink link, DataSet dataSet)
		{
			if (_link.DataField != null)
			{
				if (CheckState == CheckState.Indeterminate)
					_link.DataField.ClearValue();
				else
					_link.DataField.AsBoolean = (CheckState == CheckState.Checked);
			}
		}

		/// <summary> Returns the CheckState based on the field value. </summary>
		private CheckState GetCheckState()
		{
			if ((DataField != null) && DataField.HasValue())
			{
				if (Convert.ToBoolean(DataField))
					return CheckState.Checked;
				else
					return CheckState.Unchecked;
			}
			else
				return CheckState.Indeterminate;
		}

		protected override void OnClick(EventArgs eventArgs)
		{
			base.OnClick(eventArgs);
			if (_link.Edit())
			{
				switch (base.CheckState)
				{
					case CheckState.Indeterminate :
						base.CheckState = _trueFirst ? CheckState.Checked : CheckState.Unchecked;
						break; 
					case CheckState.Checked :
						if (base.ThreeState && _trueFirst)
							base.CheckState = CheckState.Indeterminate;
						else
							base.CheckState = CheckState.Unchecked;
						break;
					case CheckState.Unchecked :
                        if (base.ThreeState && !_trueFirst)
                            base.CheckState = CheckState.Indeterminate;
                        else
                            base.CheckState = CheckState.Checked;
						break;
				}
			}
		}

		/// <summary> Called when the AutoUpdateTimer has elapsed. </summary>
		/// <param name="sender"> The object whose delegate is called. </param>
		/// <param name="args"> An EventArgs that contains data related to this event. </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		protected virtual void AutoUpdateElapsed(object sender, EventArgs args)
		{
			_autoUpdateTimer.Stop();
			_link.SaveRequested();
		}

		protected override void OnCheckStateChanged(EventArgs eventArgs)
		{
			base.OnCheckStateChanged(eventArgs);
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

		/// <summary> Gets a value indicating whether the Checked or CheckState values and the check box's appearance are automatically changed when the check box is clicked. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[Browsable(false)]
		new public bool AutoCheck
		{
			get { return base.AutoCheck; }
		}

		/// <summary> Gets a value indicating whether the check box will allow three check states rather than two. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[Browsable(false)]
		new public bool ThreeState
		{
			get { return base.ThreeState; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		new public virtual CheckState CheckState 
		{
			get { return base.CheckState; }
			set
			{
				if (base.CheckState != value)
				{
					EnsureEdit();
					base.CheckState = value;
					_link.SaveRequested();
				}
			}
		}

		/// <summary> Checked is defined by a field value not serialized value. </summary>
		/// <returns>false</returns>
		protected bool ShouldSerializeChecked() { return false;	}

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData) 
			{
				case Keys.Escape:
					if(_link.Modified)
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
				case Keys.Space :
					_link.Edit();
					break;
			}
		}

		protected override void OnLeave(EventArgs eventArgs)
		{
			if (!Disposing)
			{
				try
				{
					if (_link != null)
						_link.SaveRequested();
				}
				catch
				{
					Focus();
					throw;
				}
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
