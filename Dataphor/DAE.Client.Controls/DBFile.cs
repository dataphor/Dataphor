/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Data aware file control. </summary>
	[ToolboxItem(true)]
	public class DBFile : Control, IReadOnly, IColumnNameReference, IDataSourceReference
	{
		public DBFile()
		{
			SetStyle(ControlStyles.FixedHeight, true);
			SetStyle(ControlStyles.FixedWidth, true);
			SetStyle(ControlStyles.ContainerControl, false);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.StandardClick, true);
			SetStyle(ControlStyles.StandardDoubleClick, true);

			BackColor = Color.Transparent;
			Width = 29;
			Height = 34;

			InitializeLinks();
			InitializeImages();
		}

		protected override void Dispose(bool ADisposing)
		{
			DeinitializeDBFileForm();
			DeinitializeLinks();
			DeinitializeImages();
			base.Dispose(ADisposing);
		}

		#region MaximumContentLength

		private long FMaximumContentLength = DBFileForm.CDefaultMaximumContentLength;
		/// <summary> Maximum size in bytes for documents to be loaded into this control. </summary>
		[DefaultValue(DBFileForm.CDefaultMaximumContentLength)]
		[Description("Maximum size in bytes for documents to be loaded into this control.")]
		public long MaximumContentLength
		{
			get { return FMaximumContentLength; }
			set { FMaximumContentLength = Math.Max(0, value); }
		}

		#endregion

		#region Data ContentLink

		private void InitializeLinks()
		{
			FLink = new FieldDataLink();
			FLink.OnFieldChanged += new DataLinkFieldHandler(ContentChanged);
			FLink.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);

			FExtensionLink = new FieldDataLink();
			FExtensionLink.OnFieldChanged += new DataLinkFieldHandler(ExtensionChanged);

			FNameLink = new FieldDataLink();
		}

		private void DeinitializeLinks()
		{
			if (FNameLink != null)
			{
				FNameLink.Dispose();
				FNameLink = null;
			}
			if (FExtensionLink != null)
			{
				FExtensionLink.Dispose();
				FExtensionLink = null;
			}
			if (FLink != null)
			{
				FLink.Dispose();
				FLink = null;
			}
		}

		private FieldDataLink FExtensionLink;
		internal protected FieldDataLink ExtensionLink
		{
			get { return FExtensionLink; }
		}

		private FieldDataLink FNameLink;
		internal protected FieldDataLink NameLink
		{
			get { return FNameLink; }
		}

		private FieldDataLink FLink;
		internal protected FieldDataLink ContentLink
		{
			get { return FLink; }
		}

		/// <summary> Gets or sets a value indicating whether to allow the user to modify the file. </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return FLink.ReadOnly; }
			set { FLink.ReadOnly = value; }
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

		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ExtensionColumnName
		{
			get { return FExtensionLink.ColumnName; }
			set { FExtensionLink.ColumnName = value; }
		}

		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string NameColumnName
		{
			get { return FNameLink.ColumnName; }
			set { FNameLink.ColumnName = value; }
		}

		private bool FAutoSetNameOnLoad = true;
		/// <summary> Indicates whether of not to set the values of the Name and/or Extension columns when a file is loaded. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool AutoSetNameOnLoad
		{
			get { return FAutoSetNameOnLoad; }
			set { FAutoSetNameOnLoad = value; }
		}

		private bool FAutoRenameOnOpen = true;
		/// <summary> Indicates whether of not to rename the temporary file if a file with that name already exists. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool AutoRenameOnOpen
		{
			get { return FAutoRenameOnOpen; }
			set { FAutoRenameOnOpen = value; }
		}

		private int FWaitForProcessInterval = DBFileForm.CDefaultWaitForProcessInterval;
		/// <summary> Number of seconds to wait for the spawned process to load. </summary>
		[DefaultValue(DBFileForm.CDefaultWaitForProcessInterval)]
		[Category("Behavior")]
		public int WaitForProcessInterval
		{
			get { return FWaitForProcessInterval; }
			set { FWaitForProcessInterval = value; }
		}

		private int FPollInterval = DBFileForm.CDefaultPollInterval;
		/// <summary> Interval to poll the temporary file. </summary>
		[DefaultValue(DBFileForm.CDefaultPollInterval)]
		[Category("Behavior")]
		public int PollInterval
		{
			get { return FPollInterval; }
			set { FPollInterval = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return FLink.Source; }
			set
			{
				FLink.Source = value;
				FNameLink.Source = value;
				FExtensionLink.Source = value;
			}
		}

		[Browsable(false)]
		public DataField DataField
		{
			get { return FLink == null ? null : FLink.DataField; }
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
				Focus();
		}

		private void ExtensionChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			UpdateMenuItems();
		}

		internal protected bool InternalGetReadOnly()
		{
			return FLink.ReadOnly || !FLink.Active;
		}

		private void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			if (!DesignMode)
			{
				UpdateMenuItems();
				InternalUpdateTabStop();
			}
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

		#region HasValue

		private void ContentChanged(DataLink ADataLink, DataSet ADataSet, DataField AField)
		{
			SetHasValue((DataField != null) && DataField.HasValue());
		}

		private bool FHasValue;
		internal protected bool HasValue
		{
			get { return FHasValue; }
		}

		private void SetHasValue(bool AHasValue)
		{
			if (FHasValue != AHasValue)
			{
				FHasValue = AHasValue;
				UpdateMenuItems();
				Invalidate();
			}
		}

		#endregion

		#region Painting

		private Image FInactiveImage;
		private Image FActiveImage;

		private void DeinitializeDBFileForm()
		{
			if (FDBFileForm != null)
			{
				if (FDBFileForm.FileOpened && !FDBFileForm.FileProcessed)
					FDBFileForm.RecoverFile();
				FDBFileForm.Dispose();
				FDBFileForm = null;
			}
		}

		private void InitializeImages()
		{
			FInactiveImage = new Bitmap(GetType().Assembly.GetManifestResourceStream(@"Alphora.Dataphor.DAE.Client.Controls.Images.File.png"));
			FActiveImage = new Bitmap(GetType().Assembly.GetManifestResourceStream(@"Alphora.Dataphor.DAE.Client.Controls.Images.FileActive.png"));
		}

		private void DeinitializeImages()
		{
			if (FInactiveImage != null)
			{
				FInactiveImage.Dispose();
				FInactiveImage = null;
			}
			if (FActiveImage != null)
			{
				FActiveImage.Dispose();
				FActiveImage = null;
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			if (Focused)
				ControlPaint.DrawFocusRectangle(AArgs.Graphics, new Rectangle(0, 0, Width - 1, Height - 1));
			Image LImage = FHasValue ? FActiveImage : FInactiveImage;
			AArgs.Graphics.DrawImage(LImage, 1, 1, LImage.Width - 1, LImage.Height - 1);
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			Invalidate();
			BuildContextMenu();
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
			Invalidate();
		}

		#endregion

		#region Menu

		const int WM_CONTEXTMENU = 0x007B;

		protected override void WndProc(ref Message AMessage)
		{
			// Don't build the context menu until it is requested
			if (AMessage.Msg == WM_CONTEXTMENU)
				BuildContextMenu();

			base.WndProc(ref AMessage);
		}

		private void BuildContextMenu()
		{
			if (ContextMenu == null)
			{
				ContextMenu = new ContextMenu();

				InternalBuildContextMenu();

				UpdateMenuItems();
			}
		}

		public event EventHandler OnBuildContextMenu;

		protected virtual void InternalBuildContextMenu()
		{
			DBFileMenuItem LItem;

			// Open
			LItem = new DBFileMenuItem(this, true, false);
			LItem.Text = Strings.Get("DBFile.Menu.OpenText");
			LItem.Click += new EventHandler(OpenClicked);
			LItem.DefaultItem = true;
			ContextMenu.MenuItems.Add(LItem);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));

			// Save As...
			LItem = new DBFileMenuItem(this, true, false);
			LItem.Text = Strings.Get("DBFile.Menu.SaveAsText");
			LItem.Click += new EventHandler(SaveAsClicked);
			ContextMenu.MenuItems.Add(LItem);

			// Load...
			LItem = new DBFileMenuItem(this, false, true);
			LItem.Text = Strings.Get("DBFile.Menu.LoadText");
			LItem.Click += new EventHandler(LoadClicked);
			ContextMenu.MenuItems.Add(LItem);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));

			// TODO: Copy and paste commands

			//// Copy
			//LItem = new DBFileMenuItem(this, true, false);
			//LItem.Text = Strings.Get("DBFile.Menu.CopyText");
			//LItem.Click += new EventHandler(CopyClicked);
			//LItem.Shortcut = Shortcut.CtrlC;
			//ContextMenu.MenuItems.Add(LItem);

			//// Paste
			//LItem = new DBFileMenuItem(this, false, true);
			//LItem.Text = Strings.Get("DBFile.Menu.PasteText");
			//LItem.Click += new EventHandler(PasteClicked);
			//LItem.Shortcut = Shortcut.CtrlV;
			//ContextMenu.MenuItems.Add(LItem);

			// Clear
			LItem = new DBFileMenuItem(this, true, true);
			LItem.Text = Strings.Get("DBFile.Menu.ClearText");
			LItem.Click += new EventHandler(ClearClicked);
			ContextMenu.MenuItems.Add(LItem);

			if (OnBuildContextMenu != null)
				OnBuildContextMenu(this, EventArgs.Empty);
		}

		public event EventHandler OnUpdateMenuItems;

		private void UpdateMenuItems()
		{
			if (ContextMenu != null)
			{
				foreach (MenuItem LItem in ContextMenu.MenuItems)
				{
					DBFileMenuItem LFileItem = LItem as DBFileMenuItem;
					if (LFileItem != null)
						LFileItem.UpdateEnabled();
				}

				if (OnUpdateMenuItems != null)
					OnUpdateMenuItems(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Keyboard and Mouse

		protected override bool IsInputKey(Keys AKeyData)
		{
			switch (AKeyData)
			{
				case Keys.Insert:
				case Keys.L:
				case Keys.S:
				case Keys.Space:
					return true;
			}
			return base.IsInputKey(AKeyData);
		}

		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			BuildContextMenu();
			switch (AArgs.KeyData)
			{
				case Keys.Insert:
					ContextMenu.Show(this, new Point(Width / 2, Height / 2));
					AArgs.Handled = true;
					break;
				case Keys.L:
					LoadClicked(this, EventArgs.Empty);
					AArgs.Handled = true;
					break;
				case Keys.S:
					SaveAsClicked(this, EventArgs.Empty);
					AArgs.Handled = true;
					break;
				case Keys.Space:
					OpenClicked(this, EventArgs.Empty);
					AArgs.Handled = true;
					break;
			}
			base.OnKeyDown(AArgs);
		}

		protected override void OnDoubleClick(EventArgs AArgs)
		{
			OpenClicked(this, AArgs);
			base.OnDoubleClick(AArgs);
		}

		protected override void OnClick(EventArgs AArgs)
		{
			Focus();
			base.OnClick(AArgs);
		}

		#endregion

		#region Actions

		private DBFileForm FDBFileForm;
		private DBFileForm DBFileForm
		{
			get
			{
				if (FDBFileForm == null)
					FDBFileForm = new DBFileForm(FLink, FNameLink, FExtensionLink);

				FDBFileForm.MaximumContentLength = FMaximumContentLength;
				FDBFileForm.PollInterval = FPollInterval;
				FDBFileForm.WaitForProcessInterval = FWaitForProcessInterval;
				FDBFileForm.AutoRenameOnOpen = FAutoRenameOnOpen;
				return FDBFileForm;
			}
		}

		private void CopyClicked(object ASender, EventArgs AArgs)
		{
			// TODO	
		}

		private void PasteClicked(object ASender, EventArgs AArgs)
		{
			if (DataField != null)
			{
				// TODO
			}
		}

		private void SaveAsClicked(object ASender, EventArgs AArgs)
		{
			if ((DataField != null) && DataField.HasValue())
				DBFileForm.SaveToFile();
		}

		private string ExtensionWithoutDot(string AExtension)
		{
			return (AExtension == String.Empty ? String.Empty : AExtension.Substring(1));
		}

		private void LoadClicked(object ASender, EventArgs AArgs)
		{
			if (FLink.DataSet != null)
			{
				FLink.DataSet.Edit();
				if (DataField != null)
				{
					DBFileForm.LoadFromFile();
					if (AutoSetNameOnLoad)
					{
						if ((FNameLink.DataField != null) && FNameLink.DataField.IsNil)
							FNameLink.DataField.AsString = Path.GetFileNameWithoutExtension(DBFileForm.FileName);
						if ((FExtensionLink.DataField != null) && FExtensionLink.DataField.IsNil)
							FExtensionLink.DataField.AsString = ExtensionWithoutDot(Path.GetExtension(DBFileForm.FileName));
					}
				}
			}
		}

		private void ClearClicked(object ASender, EventArgs AArgs)
		{
			if ((DataField != null) && !DataField.IsNil)
				DataField.ClearValue();
		}

		private void OpenClicked(object ASender, EventArgs AArgs)
		{
			if ((DataField != null) && DataField.HasValue())
			{
				if (FDBFileForm != null)
				{
					FDBFileForm.Dispose();
					FDBFileForm = null;
				}
				
				DBFileForm.OpenFile();
			}
		}
		#endregion
	}

	public class DBFileMenuItem : MenuItem
	{
		public DBFileMenuItem(DBFile AFileControl, bool AReadsData, bool AWritesData)
		{
			FFileControl = AFileControl;
			FReadsData = AReadsData;
			FWritesData = AWritesData;
		}

		private DBFile FFileControl;
		public DBFile FileControl
		{
			get { return FFileControl; }
		}

		private bool FReadsData;
		public bool ReadsData
		{
			get { return FReadsData; }
		}

		private bool FWritesData;
		public bool WritesData
		{
			get { return FWritesData; }
		}

		internal protected virtual void UpdateEnabled()
		{
			Enabled =
				(!FWritesData || !FFileControl.InternalGetReadOnly())
					&& (!FReadsData || FFileControl.HasValue);
		}
	}
}
