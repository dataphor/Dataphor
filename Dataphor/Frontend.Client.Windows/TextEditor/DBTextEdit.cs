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

		protected override void Dispose(bool ADisposing)
		{
			DeinitializeDataLink();
			base.Dispose(ADisposing);
		}

		#region Data Link

		private FieldDataLink FLink;
		protected FieldDataLink Link { get { return FLink; } }

		private void InitializeDataLink()
		{
			FLink = new FieldDataLink();
			FLink.OnSaveRequested += new DataLinkHandler(SaveRequested);
			FLink.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			FLink.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
		}

		private void DeinitializeDataLink()
		{
			if (FLink != null)
			{
				FLink.Dispose();
				FLink = null;
			}
		}

		/// <summary> Gets or sets a value indicating whether to allow the user to modify the file. </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return FLink.ReadOnly; }
			set { FLink.ReadOnly = value; }
		}

		internal protected bool InternalGetReadOnly()
		{
			return FLink.ReadOnly || !FLink.Active;
		}

		private void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			InternalUpdateTabStop();
		}

		/// <summary> Gets or sets a value indicating the column in the DataView to link to. </summary>
		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FLink.ColumnName; }
			set { FLink.ColumnName = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value; }
		}

		[Browsable(false)]
		public DataField DataField
		{
			get { return FLink == null ? null : FLink.DataField; }
		}

		#endregion

		#region Value Synchronization
		
		private bool FHasValue;
		public bool HasValue { get { return FHasValue; } }
		private void SetHasValue(bool AValue)
		{
			FHasValue = AValue;
			//UpdateBackColor();
		}

		private void FieldChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			SetHasValue((DataField != null) && DataField.HasValue());
			SetText(HasValue ? DataField.AsString : "");
		}

		protected override void DoDocumentChanged(object ASender, ICSharpCode.TextEditor.Document.DocumentEventArgs AArgs)
		{
			base.DoDocumentChanged(ASender, AArgs);
			SetHasValue((Document.TextContent != String.Empty) || !NilIfBlank);
			EnsureEdit();
		}

		private void EnsureEdit()
		{
			if (!FLink.Edit())
				throw new DAE.Client.Controls.ControlsException(DAE.Client.Controls.ControlsException.Codes.InvalidViewState);
		}

		protected void Reset()
		{
			FLink.Reset();
			//SelectAll();
		}

		private void UpdateFieldValue()
		{
			if (!Disposing && !IsDisposed)
			{
				try
				{
					FLink.SaveRequested();
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

		private void SaveRequested(DataLink ALink, DataSet ADataSet)
		{
			if (FLink.DataField != null)
			{
				if (!HasValue)
					DataField.ClearValue();
				else
					DataField.AsString = Document.TextContent;
			}
		}

		#endregion
		
		#region Keyboard

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
			switch (AArgs.KeyData)
			{
				case Keys.Escape:
					Reset();
					AArgs.Handled = true;
					break;
			}
			base.OnKeyDown(AArgs);
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			switch (AKey)
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
			return base.ProcessDialogKey(AKey);
		}

		#endregion

		#region Focus
		
		protected override void OnLeave(EventArgs AEventArgs)
		{
			UpdateFieldValue();
			base.OnLeave(AEventArgs);
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
				Focus();
		}

		#endregion		

		#region TabStop

		private bool FTabStop = true;
		[DefaultValue(true)]
		public new bool TabStop
		{
			get { return FTabStop; }
			set
			{
				if (FTabStop != value)
				{
					FTabStop = value;
					InternalUpdateTabStop();
				}
			}
		}

		private bool InternalGetTabStop()
		{
			return FTabStop && !InternalGetReadOnly();
		}

		private void InternalUpdateTabStop()
		{
			base.TabStop = InternalGetTabStop();
		}

		#endregion

		// DocumentType

		public const string CDefaultDocumentType = "Default";
		private string FDocumentType = CDefaultDocumentType;
		[DefaultValue(CDefaultDocumentType)]
		public string DocumentType
		{
			get { return FDocumentType; }
			set
			{
				if (FDocumentType != value)
				{
					FDocumentType = value != null ? value : "";
					Document.HighlightingStrategy = SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy(FDocumentType);
				}
			}
		}

		// NilIfBlank
		
		private bool FNilIfBlank = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When true, a '' value entered is considered nil")]
		public bool NilIfBlank
		{
			get { return FNilIfBlank; }
			set { FNilIfBlank = value; }
		}

	}
}
