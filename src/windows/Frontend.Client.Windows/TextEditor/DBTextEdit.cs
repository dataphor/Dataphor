/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

using Alphora.Dataphor.DAE.Client;

using SD = ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class DBTextEdit : TextEdit, IColumnNameReference, IDataSourceReference
	{
		public DBTextEdit()
		{
			InitializeDataLink();
		}

		protected override void Dispose(bool disposing)
		{
			DeinitializeDataLink();
			base.Dispose(disposing);
		}

		#region Data Link

		private FieldDataLink _link;
		protected FieldDataLink Link { get { return _link; } }

		private void InitializeDataLink()
		{
			_link = new FieldDataLink();
			_link.OnSaveRequested += new DataLinkHandler(SaveRequested);
			_link.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			_link.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
		}

		private void DeinitializeDataLink()
		{
			if (_link != null)
			{
				_link.Dispose();
				_link = null;
			}
		}

		/// <summary> Gets or sets a value indicating whether to allow the user to modify the file. </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return _link.ReadOnly; }
			set { _link.ReadOnly = value; }
		}

		internal protected bool InternalGetReadOnly()
		{
			return _link.ReadOnly || !_link.Active;
		}

		private void UpdateReadOnly(object sender, EventArgs args)
		{
			InternalUpdateTabStop();
		}

		/// <summary> Gets or sets a value indicating the column in the DataView to link to. </summary>
		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set { _link.ColumnName = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value; }
		}

		[Browsable(false)]
		public DataField DataField
		{
			get { return _link == null ? null : _link.DataField; }
		}

		#endregion

		#region Value Synchronization
		
		private bool _hasValue;
		public bool HasValue { get { return _hasValue; } }
		private void SetHasValue(bool tempValue)
		{
			_hasValue = tempValue;
			//UpdateBackColor();
		}

		private void FieldChanged(DataLink link, DataSet dataSet, DataField field)
		{
			SetHasValue((DataField != null) && DataField.HasValue());
			SetText(HasValue ? DataField.AsString : "");
		}

		protected override void DoDocumentChanged(object sender, ICSharpCode.TextEditor.Document.DocumentEventArgs args)
		{
			base.DoDocumentChanged(sender, args);
			SetHasValue((Document.TextContent != String.Empty) || !NilIfBlank);
			EnsureEdit();
		}

		private void EnsureEdit()
		{
			if (!_link.Edit())
				throw new DAE.Client.Controls.ControlsException(DAE.Client.Controls.ControlsException.Codes.InvalidViewState);
		}

		protected void Reset()
		{
			_link.Reset();
			//SelectAll();
		}

		private void UpdateFieldValue()
		{
			if (!Disposing && !IsDisposed)
			{
				try
				{
					_link.SaveRequested();
				}
				catch
				{
					//SelectAll();
					Focus();
					throw;
				}
				//ResetCursor();
			}
		}

		private void SaveRequested(DataLink link, DataSet dataSet)
		{
			if (_link.DataField != null)
			{
				if (!HasValue)
					DataField.ClearValue();
				else
					DataField.AsString = Document.TextContent;
			}
		}

		#endregion
		
		#region Keyboard

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
			switch (args.KeyData)
			{
				case Keys.Escape:
					Reset();
					args.Handled = true;
					break;
			}
			base.OnKeyDown(args);
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
			{
				case Keys.Control | Keys.Back:
					if (HasValue && !InternalGetReadOnly())
					{
						EnsureEdit();
						SetText(String.Empty);
						SetHasValue(false);
					}
					else
						System.Media.SystemSounds.Beep.Play();
					return true;
				case Keys.Control | Keys.Space:
					if (!InternalGetReadOnly())
					{
						EnsureEdit();
						SetText(String.Empty);
						SetHasValue(true);
					}
					else
						System.Media.SystemSounds.Beep.Play();
					return true;
			}
			return base.ProcessDialogKey(key);
		}

		#endregion

		#region Focus
		
		protected override void OnLeave(EventArgs eventArgs)
		{
			UpdateFieldValue();
			base.OnLeave(eventArgs);
		}

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			if (field == DataField)
				Focus();
		}

		#endregion		

		#region TabStop

		private bool _tabStop = true;
		[DefaultValue(true)]
		public new bool TabStop
		{
			get { return _tabStop; }
			set
			{
				if (_tabStop != value)
				{
					_tabStop = value;
					InternalUpdateTabStop();
				}
			}
		}

		private bool InternalGetTabStop()
		{
			return _tabStop && !InternalGetReadOnly();
		}

		private void InternalUpdateTabStop()
		{
			base.TabStop = InternalGetTabStop();
		}

		#endregion

		// DocumentType

		public const string DefaultDocumentType = "Default";
		private string _documentType = DefaultDocumentType;
		[DefaultValue(DefaultDocumentType)]
		public string DocumentType
		{
			get { return _documentType; }
			set
			{
				if (_documentType != value)
				{
					_documentType = value != null ? value : "";
					Document.HighlightingStrategy = SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy(_documentType);
				}
			}
		}

		// NilIfBlank
		
		private bool _nilIfBlank = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When true, a '' value entered is considered nil")]
		public bool NilIfBlank
		{
			get { return _nilIfBlank; }
			set { _nilIfBlank = value; }
		}

	}
}
