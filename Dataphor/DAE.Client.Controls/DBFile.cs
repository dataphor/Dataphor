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

		protected override void Dispose(bool disposing)
		{
			DeinitializeDBFileForm();
			DeinitializeLinks();
			DeinitializeImages();
			base.Dispose(disposing);
		}

		#region MaximumContentLength

		private long _maximumContentLength = DBFileForm.DefaultMaximumContentLength;
		/// <summary> Maximum size in bytes for documents to be loaded into this control. </summary>
		[DefaultValue(DBFileForm.DefaultMaximumContentLength)]
		[Description("Maximum size in bytes for documents to be loaded into this control.")]
		public long MaximumContentLength
		{
			get { return _maximumContentLength; }
			set { _maximumContentLength = Math.Max(0, value); }
		}

		#endregion

		#region Data ContentLink

		private void InitializeLinks()
		{
			_link = new FieldDataLink();
			_link.OnFieldChanged += new DataLinkFieldHandler(ContentChanged);
			_link.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);

			_extensionLink = new FieldDataLink();
			_extensionLink.OnFieldChanged += new DataLinkFieldHandler(ExtensionChanged);

			_nameLink = new FieldDataLink();
		}

		private void DeinitializeLinks()
		{
			if (_nameLink != null)
			{
				_nameLink.Dispose();
				_nameLink = null;
			}
			if (_extensionLink != null)
			{
				_extensionLink.Dispose();
				_extensionLink = null;
			}
			if (_link != null)
			{
				_link.Dispose();
				_link = null;
			}
		}

		private FieldDataLink _extensionLink;
		internal protected FieldDataLink ExtensionLink
		{
			get { return _extensionLink; }
		}

		private FieldDataLink _nameLink;
		internal protected FieldDataLink NameLink
		{
			get { return _nameLink; }
		}

		private FieldDataLink _link;
		internal protected FieldDataLink ContentLink
		{
			get { return _link; }
		}

		/// <summary> Gets or sets a value indicating whether to allow the user to modify the file. </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return _link.ReadOnly; }
			set { _link.ReadOnly = value; }
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

		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ExtensionColumnName
		{
			get { return _extensionLink.ColumnName; }
			set { _extensionLink.ColumnName = value; }
		}

		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string NameColumnName
		{
			get { return _nameLink.ColumnName; }
			set { _nameLink.ColumnName = value; }
		}

		private bool _autoSetNameOnLoad = true;
		/// <summary> Indicates whether of not to set the values of the Name and/or Extension columns when a file is loaded. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool AutoSetNameOnLoad
		{
			get { return _autoSetNameOnLoad; }
			set { _autoSetNameOnLoad = value; }
		}

		private bool _autoRenameOnOpen = true;
		/// <summary> Indicates whether of not to rename the temporary file if a file with that name already exists. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool AutoRenameOnOpen
		{
			get { return _autoRenameOnOpen; }
			set { _autoRenameOnOpen = value; }
		}

		private int _waitForProcessInterval = DBFileForm.DefaultWaitForProcessInterval;
		/// <summary> Number of seconds to wait for the spawned process to load. </summary>
		[DefaultValue(DBFileForm.DefaultWaitForProcessInterval)]
		[Category("Behavior")]
		public int WaitForProcessInterval
		{
			get { return _waitForProcessInterval; }
			set { _waitForProcessInterval = value; }
		}

		private int _pollInterval = DBFileForm.DefaultPollInterval;
		/// <summary> Interval to poll the temporary file. </summary>
		[DefaultValue(DBFileForm.DefaultPollInterval)]
		[Category("Behavior")]
		public int PollInterval
		{
			get { return _pollInterval; }
			set { _pollInterval = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return _link.Source; }
			set
			{
				_link.Source = value;
				_nameLink.Source = value;
				_extensionLink.Source = value;
			}
		}

		[Browsable(false)]
		public DataField DataField
		{
			get { return _link == null ? null : _link.DataField; }
		}

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			if (field == DataField)
				Focus();
		}

		private void ExtensionChanged(DataLink link, DataSet dataSet, DataField field)
		{
			UpdateMenuItems();
		}

		internal protected bool InternalGetReadOnly()
		{
			return _link.ReadOnly || !_link.Active;
		}

		private void UpdateReadOnly(object sender, EventArgs args)
		{
			if (!DesignMode)
			{
				UpdateMenuItems();
				InternalUpdateTabStop();
			}
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

		#region HasValue

		private void ContentChanged(DataLink dataLink, DataSet dataSet, DataField field)
		{
			SetHasValue((DataField != null) && DataField.HasValue());
		}

		private bool _hasValue;
		internal protected bool HasValue
		{
			get { return _hasValue; }
		}

		private void SetHasValue(bool hasValue)
		{
			if (_hasValue != hasValue)
			{
				_hasValue = hasValue;
				UpdateMenuItems();
				Invalidate();
			}
		}

		#endregion

		#region Painting

		protected Image InactiveImage
		{
			get;
			set;
		}
		protected Image ActiveImage
		{
			get;
			set;
		}

		private void DeinitializeDBFileForm()
		{
			if (_dBFileForm != null)
			{
				if (_dBFileForm.FileOpened && !_dBFileForm.FileProcessed)
					_dBFileForm.RecoverFile();
				_dBFileForm.Dispose();
				_dBFileForm = null;
			}
		}

		protected virtual void InitializeImages()
		{
			InactiveImage = new Bitmap(GetType().Assembly.GetManifestResourceStream(@"Alphora.Dataphor.DAE.Client.Controls.Images.File.png"));
			ActiveImage = new Bitmap(GetType().Assembly.GetManifestResourceStream(@"Alphora.Dataphor.DAE.Client.Controls.Images.FileActive.png"));			
		}

		private void DeinitializeImages()
		{
			if (InactiveImage != null)
			{
				InactiveImage.Dispose();
				InactiveImage = null;
			}
			if (ActiveImage != null)
			{
				ActiveImage.Dispose();
				ActiveImage = null;
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			if (Focused)
				ControlPaint.DrawFocusRectangle(args.Graphics, new Rectangle(0, 0, Width - 1, Height - 1));
			Image image = _hasValue ? ActiveImage : InactiveImage;
			args.Graphics.DrawImage(image, 1, 1, image.Width - 1, image.Height - 1);
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			Invalidate();
			BuildContextMenu();
		}

		protected override void OnLostFocus(EventArgs args)
		{
			base.OnLostFocus(args);
			Invalidate();
		}

		#endregion

		#region Menu

		const int WM_CONTEXTMENU = 0x007B;

		protected override void WndProc(ref Message message)
		{
			// Don't build the context menu until it is requested
			if (message.Msg == WM_CONTEXTMENU)
				BuildContextMenu();

			base.WndProc(ref message);
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
			DBFileMenuItem item;

			// Open
			item = new DBFileMenuItem(this, true, false);
			item.Text = Strings.Get("DBFile.Menu.OpenText");
			item.Click += new EventHandler(OpenClicked);
			item.DefaultItem = true;
			ContextMenu.MenuItems.Add(item);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));

			// Save As...
			item = new DBFileMenuItem(this, true, false);
			item.Text = Strings.Get("DBFile.Menu.SaveAsText");
			item.Click += new EventHandler(SaveAsClicked);
			ContextMenu.MenuItems.Add(item);

			// Load...
			item = new DBFileMenuItem(this, false, true);
			item.Text = Strings.Get("DBFile.Menu.LoadText");
			item.Click += new EventHandler(LoadClicked);
			ContextMenu.MenuItems.Add(item);

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
			item = new DBFileMenuItem(this, true, true);
			item.Text = Strings.Get("DBFile.Menu.ClearText");
			item.Click += new EventHandler(ClearClicked);
			ContextMenu.MenuItems.Add(item);

			if (OnBuildContextMenu != null)
				OnBuildContextMenu(this, EventArgs.Empty);
		}

		public event EventHandler OnUpdateMenuItems;

		private void UpdateMenuItems()
		{
			if (ContextMenu != null)
			{
				foreach (MenuItem item in ContextMenu.MenuItems)
				{
					DBFileMenuItem fileItem = item as DBFileMenuItem;
					if (fileItem != null)
						fileItem.UpdateEnabled();
				}

				if (OnUpdateMenuItems != null)
					OnUpdateMenuItems(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Keyboard and Mouse

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Insert:
				case Keys.L:
				case Keys.S:
				case Keys.Space:
					return true;
			}
			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			BuildContextMenu();
			switch (args.KeyData)
			{
				case Keys.Insert:
					ContextMenu.Show(this, new Point(Width / 2, Height / 2));
					args.Handled = true;
					break;
				case Keys.L:
					LoadClicked(this, EventArgs.Empty);
					args.Handled = true;
					break;
				case Keys.S:
					SaveAsClicked(this, EventArgs.Empty);
					args.Handled = true;
					break;
				case Keys.Space:
					OpenClicked(this, EventArgs.Empty);
					args.Handled = true;
					break;
			}
			base.OnKeyDown(args);
		}

		protected override void OnDoubleClick(EventArgs args)
		{
			OpenClicked(this, args);
			base.OnDoubleClick(args);
		}

		protected override void OnClick(EventArgs args)
		{
			Focus();
			base.OnClick(args);
		}

		#endregion

		#region Actions

		private DBFileForm _dBFileForm;
		private DBFileForm DBFileForm
		{
			get
			{
				if (_dBFileForm == null)
					_dBFileForm = new DBFileForm(_link, _nameLink, _extensionLink);

				_dBFileForm.MaximumContentLength = _maximumContentLength;
				_dBFileForm.PollInterval = _pollInterval;
				_dBFileForm.WaitForProcessInterval = _waitForProcessInterval;
				_dBFileForm.AutoRenameOnOpen = _autoRenameOnOpen;
				return _dBFileForm;
			}
		}

		private void CopyClicked(object sender, EventArgs args)
		{
			// TODO	
		}

		private void PasteClicked(object sender, EventArgs args)
		{
			if (DataField != null)
			{
				// TODO
			}
		}

		private void SaveAsClicked(object sender, EventArgs args)
		{
			if ((DataField != null) && DataField.HasValue())
				DBFileForm.SaveToFile();
		}

		private string ExtensionWithoutDot(string extension)
		{
			return (extension == String.Empty ? String.Empty : extension.Substring(1));
		}

		private void LoadClicked(object sender, EventArgs args)
		{
			if (_link.DataSet != null)
			{
				_link.DataSet.Edit();
				if (DataField != null)
				{
					DBFileForm.LoadFromFile();
					if (AutoSetNameOnLoad)
					{
						if ((_nameLink.DataField != null) && _nameLink.DataField.IsNil)
							_nameLink.DataField.AsString = Path.GetFileNameWithoutExtension(DBFileForm.FileName);
						if ((_extensionLink.DataField != null) && _extensionLink.DataField.IsNil)
							_extensionLink.DataField.AsString = ExtensionWithoutDot(Path.GetExtension(DBFileForm.FileName));
					}
				}
			}
		}

		private void ClearClicked(object sender, EventArgs args)
		{
			if ((DataField != null) && !DataField.IsNil)
				DataField.ClearValue();
		}

		private void OpenClicked(object sender, EventArgs args)
		{
			if ((DataField != null) && DataField.HasValue())
			{
				if (_dBFileForm != null)
				{
					_dBFileForm.Dispose();
					_dBFileForm = null;
				}
				
				DBFileForm.OpenFile();
			}
		}
		#endregion
	}

	public class DBFileMenuItem : MenuItem
	{
		public DBFileMenuItem(DBFile fileControl, bool readsData, bool writesData)
		{
			_fileControl = fileControl;
			_readsData = readsData;
			_writesData = writesData;
		}

		private DBFile _fileControl;
		public DBFile FileControl
		{
			get { return _fileControl; }
		}

		private bool _readsData;
		public bool ReadsData
		{
			get { return _readsData; }
		}

		private bool _writesData;
		public bool WritesData
		{
			get { return _writesData; }
		}

		internal protected virtual void UpdateEnabled()
		{
			Enabled =
				(!_writesData || !_fileControl.InternalGetReadOnly())
					&& (!_readsData || _fileControl.HasValue);
		}
	}
}
