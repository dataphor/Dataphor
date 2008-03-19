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
			FLink = new FieldDataLink();
			FLink.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			FLink.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			FLink.OnSaveRequested += new DataLinkHandler(SaveRequested);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			FAutoUpdateInterval = 200;
			base.ThreeState = false;
			base.CheckState = CheckState.Indeterminate;
			base.AutoCheck = false;
			FAutoUpdateTimer = new System.Windows.Forms.Timer();
			FAutoUpdateTimer.Interval = FAutoUpdateInterval;
			FAutoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			FAutoUpdateTimer.Enabled = false;
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				try
				{
					FAutoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
					FAutoUpdateTimer.Dispose();
					FAutoUpdateTimer = null;
				}
				finally
				{
					FLink.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
					FLink.OnUpdateReadOnly -= new EventHandler(UpdateReadOnly);
					FLink.OnSaveRequested -= new DataLinkHandler(SaveRequested);
					FLink.Dispose();
					FLink = null;
				}
			}
			base.Dispose(ADisposing);
		}

		private FieldDataLink FLink;
		/// <summary> Links this control to a view's DataField. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		protected FieldDataLink Link
		{
			get { return FLink; }
		}

		private System.Windows.Forms.Timer FAutoUpdateTimer;

		private bool FAutoUpdate;
		/// <summary> Determines if the control should automatically update the DataField's value on a given interval. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				if (FAutoUpdate != value)
					FAutoUpdate = value;
			}
		}

		private int FAutoUpdateInterval;
		/// <summary> Determines the amount of time to wait before updating a DataField's value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[DefaultValue(200)]
		[Category("Behavior")]
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set
			{
				if (FAutoUpdateInterval != value)
					FAutoUpdateInterval = value;
			}
		}

		/// <summary> Gets or sets a value indicating whether the CheckBox is read-only. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[DefaultValue(false)]
		[Category("Data")]
		public bool ReadOnly 
		{
			get { return FLink.ReadOnly; }
			set	{ FLink.ReadOnly = value; }
		}

		/// <summary> Gets or sets a value indicating the column name from which the CheckBox control displays data. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[DefaultValue(null)]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FLink.ColumnName; }
			set { FLink.ColumnName = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the CheckBox is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		public DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value;	}
		}

		/// <summary> Gets the DataField the CheckBox represents. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		[Browsable(false)]
		public DataField DataField
		{
			get { return FLink.DataField; }
		}

		private void EnsureEdit()
		{
			if (!FLink.Edit())
				throw new ControlsException(ControlsException.Codes.InvalidViewState);
		}

		[DefaultValue(false)]
		public new bool Enabled
		{
			get { return base.Enabled; }
			set { base.Enabled = value; }
		}

		private void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			if (!DesignMode)
			{
				SetStyle(ControlStyles.Selectable, FLink.Active && !FLink.ReadOnly);
				base.Enabled = FLink.Active && !FLink.ReadOnly;
			}
		}

		private void FieldChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			base.CheckState = GetCheckState();
		}

		private void SaveRequested(DataLink ALink, DataSet ADataSet)
		{
			if (FLink.DataField != null)
			{
				if (CheckState == CheckState.Indeterminate)
					FLink.DataField.ClearValue();
				else
					FLink.DataField.AsBoolean = (CheckState == CheckState.Checked);
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

		protected override void OnClick(EventArgs AEventArgs)
		{
			base.OnClick(AEventArgs);
			if (FLink.Edit())
			{
				switch (base.CheckState)
				{
					case CheckState.Indeterminate :
						base.CheckState = CheckState.Checked;
						break; 
					case CheckState.Checked :
						if (base.ThreeState)
							base.CheckState = CheckState.Indeterminate;
						else
							base.CheckState = CheckState.Unchecked;
						break;
					case CheckState.Unchecked :
						base.CheckState = CheckState.Checked;
						break;
				}
			}
		}

		/// <summary> Called when the AutoUpdateTimer has elapsed. </summary>
		/// <param name="ASender"> The object whose delegate is called. </param>
		/// <param name="AArgs"> An EventArgs that contains data related to this event. </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBCheckBox.dxd"/>
		protected virtual void AutoUpdateElapsed(object ASender, EventArgs AArgs)
		{
			FAutoUpdateTimer.Stop();
			FLink.SaveRequested();
		}

		protected override void OnCheckStateChanged(EventArgs AEventArgs)
		{
			base.OnCheckStateChanged(AEventArgs);
			if (FAutoUpdate)
			{
				if (FAutoUpdateInterval <= 0)
					FLink.SaveRequested();
				else
				{
					FAutoUpdateTimer.Interval = FAutoUpdateInterval;
					FAutoUpdateTimer.Start();
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
					FLink.SaveRequested();
				}
			}
		}

		/// <summary> Checked is defined by a field value not serialized value. </summary>
		/// <returns>false</returns>
		protected bool ShouldSerializeChecked() { return false;	}

		protected override bool IsInputKey(Keys AKeyData)
		{
			switch (AKeyData) 
			{
				case Keys.Escape:
					if(FLink.Modified)
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
				case Keys.Space :
					FLink.Edit();
					break;
			}
		}

		protected override void OnLeave(EventArgs AEventArgs)
		{
			if (!Disposing)
			{
				try
				{
					if (FLink != null)
						FLink.SaveRequested();
				}
				catch
				{
					Focus();
					throw;
				}
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
