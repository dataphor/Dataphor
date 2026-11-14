/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary>
	/// Introduces a different background color when the control has no value.
	/// <see cref="ImageAspect"/>
	/// </summary>
	[ToolboxItem(false)]
	public abstract class ExtendedImageAspect : ImageAspect
	{
		public ExtendedImageAspect() : base()
		{
			_valueBackColor = BackColor;
		}

		/// <summary> Determines whether the control has a value. </summary>
		/// <returns> True if the control holds a valid value otherwise false. </returns>
		public abstract bool HasValue { get; }

		private Color _noValueBackColor = ControlColor.NoValueBackColor;
		[Category("Colors")]
		[Description("Background color of the control when it has no value.")]
		public Color NoValueBackColor
		{
			get { return _noValueBackColor; }
			set
			{
				if (_noValueBackColor != value)
				{
					_noValueBackColor = value;
					UpdateBackColor();
				}
			}
		}

		private Color _valueBackColor;
		protected Color ValueBackColor { get { return _valueBackColor; } }

		private bool _updatingBackColor;
		protected bool UpdatingBackColor
		{
			get { return _updatingBackColor; }
		}

		protected virtual void InternalUpdateBackColor()
		{
			if (!DesignMode)
				BackColor = HasValue ? _valueBackColor : NoValueBackColor;
		}

		/// <summary> Updates the <c>BackColor</c> based on whether or not the <c>DataField</c> has a value. </summary>
		protected void UpdateBackColor()
		{
			_updatingBackColor = true;
			try
			{
				InternalUpdateBackColor();
			}
			finally
			{
				_updatingBackColor = false;
			}
		}

		protected override void OnBackColorChanged(EventArgs args)
		{
			base.OnBackColorChanged(args);
			if (!UpdatingBackColor)
				_valueBackColor = BackColor;
		}

	}

	public delegate void RequestImageHandler();

	/// <summary> Data-aware image control. </summary>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBImageAspect),"Icons.DBImageAspect.bmp")]
	public class DBImageAspect : ExtendedImageAspect, IDataSourceReference, IColumnNameReference, IReadOnly
	{
		public const int DefaultDelay = 1000;
		public DBImageAspect()
		{
			_delayTimer = new System.Windows.Forms.Timer();
			_delayTimer.Interval = DefaultDelay;
			_delayTimer.Enabled = false;
			_delayTimer.Tick += new EventHandler(DelayTimerTick);
			_link = new FieldDataLink();
			_link.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			_link.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			_link.OnSaveRequested += new DataLinkHandler(SaveRequested);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				_delayTimer.Tick -= new EventHandler(DelayTimerTick);
				_delayTimer.Dispose();
				_delayTimer = null;
				_link.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
				_link.OnUpdateReadOnly -= new EventHandler(UpdateReadOnly);
				_link.OnSaveRequested -= new DataLinkHandler(SaveRequested);
				_link.Dispose();
				_link = null;
			}
			base.Dispose(disposing);
		}

		private FieldDataLink _link;
		protected FieldDataLink Link { get { return _link; } }

		[DefaultValue(false)]
		[Category("Behavior")]
		public override bool ReadOnly 
		{
			get { return _link.ReadOnly || base.ReadOnly; }
			set
			{
				_link.ReadOnly = value;
				base.ReadOnly = value;
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
		public DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value;	}
		}

		private bool _autoDisplay = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When false LoadImage should be called at runtime, else the control loads the image.")]
		public bool AutoDisplay
		{
			get { return _autoDisplay; }
			set { _autoDisplay = value;	}
		}

		private System.Windows.Forms.Timer _delayTimer;
		protected System.Windows.Forms.Timer DelayTimer
		{
			get { return _delayTimer; }
		}

		[DefaultValue(DefaultDelay)]
		[Category("Behavior")]
		[Description("Milliseconds to delay before loading the image.")]
		public int DisplayDelay
		{
			get { return _delayTimer.Interval; }
			set
			{
				if (_delayTimer.Interval != value)
				{
					bool saveEnabled = _delayTimer.Enabled;
					_delayTimer.Enabled = false;
					_delayTimer.Interval = value;
					if (_delayTimer.Interval > 0)
						_delayTimer.Enabled = saveEnabled;
				}
			}
		}
		
		[Browsable(false)]
		public DataField DataField { get { return _link.DataField; } }

		/// <summary> Controls the internal getting and setting of the DataField's value. </summary>
		protected virtual Image FieldValue
		{
			get 
			{ 
				if ((DataField != null) && DataField.HasValue())
				{
					using (Stream stream = DataField.Value.OpenStream())
					{
						MemoryStream copyStream = new MemoryStream();
						StreamUtility.CopyStream(stream, copyStream);
						return Image.FromStream(copyStream);
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					using (DAE.Runtime.Data.Scalar newValue = new DAE.Runtime.Data.Scalar(Link.DataSet.Process.ValueManager, Link.DataSet.Process.DataTypes.SystemGraphic))
					{
						using (Stream stream = newValue.OpenStream())
						{
							value.Save(stream, value.RawFormat);
						}
						DataField.Value = newValue;
					}
				}
				else
					DataField.ClearValue();
			}
		}

		private void EnsureEdit()
		{
			if (!_link.Edit())
				throw new ControlsException(ControlsException.Codes.InvalidViewState);
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override Image Image
		{
			get { return base.Image; }
			set
			{
				if (Image != value)
				{
					EnsureEdit();
					base.Image = value;
					_link.SaveRequested();
				}
			}
		}

		[DefaultValue(false)]
		[Category("Behavior")]
		public new bool Enabled
		{
			get { return base.Enabled; }
			set { base.Enabled = value; }
		}

		protected virtual void UpdateReadOnly(object sender, EventArgs args)
		{
			if (!DesignMode)
			{
				base.ReadOnly = !_link.Active || _link.ReadOnly;
				base.Enabled = _link.Active && !_link.ReadOnly;
			}
		}

		private bool _haveValue;
		public override bool HasValue {	get { return _haveValue; } }
		private void SetHaveValue(bool value)
		{
			_haveValue = value;
			UpdateBackColor();
		}

		protected override void InternalUpdateBackColor()
		{
			if (!_link.Active)
				BackColor = ValueBackColor;
			else
				base.InternalUpdateBackColor();
		}

		protected override void OnImageChanged(EventArgs args)
		{
			base.OnImageChanged(args);
			SetHaveValue(DataField != null);
		}

		/// <summary> Loads the image from the view's data field. </summary>
		public virtual void LoadImage()
		{
			base.Image = FieldValue;
			SetHaveValue((DataField != null) && DataField.HasValue());
		}

		private void DelayTimerTick(object sender, EventArgs args)
		{
			_delayTimer.Enabled = false;
			LoadImage();
		}

		protected virtual void FieldChanged(DataLink link, DataSet dataSet, DataField field)
		{
			_delayTimer.Enabled = false;
			if (_autoDisplay && !_link.Modified)
			{
				if (_delayTimer.Interval > 0)
					_delayTimer.Enabled = true;
				else
					LoadImage();
			}
		}

		protected virtual void SaveRequested(DataLink link, DataSet dataSet)
		{
			if (_link.DataField != null)
				FieldValue = Image;
		}

		protected void Reset()
		{
			_link.Reset();
		}
		
		protected override void OnLeave(EventArgs eventArgs)
		{
			base.OnLeave(eventArgs);
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

		public event RequestImageHandler OnImageRequested;
		private void CaptureClicked(object sender, EventArgs args)
		{
			if (OnImageRequested != null)
			{
				_link.Edit();
				OnImageRequested();
			}
		}   
		
		protected override void OnKeyDown(KeyEventArgs args)
		{
			switch (args.KeyData)
			{
				case System.Windows.Forms.Keys.Escape :
					if (_link.Modified)
					{
						Reset();
						args.Handled = true;
					}
					break;
				case Keys.Delete:
					_link.Edit();
					Image = null;
					args.Handled = true;
					break;
				case Keys.Control | Keys.H :
				case Keys.Control | Keys.V :
				case Keys.Control | Keys.X :
					_link.Edit();
					break;
			}
			base.OnKeyDown(args);
		}

		const int WM_CONTEXTMENU = 0x007B;
		protected override void WndProc(ref Message message)
		{
			switch (message.Msg)
			{
				case NativeMethods.WM_CUT:
				case NativeMethods.WM_PASTE:
				case NativeMethods.WM_UNDO:
					EnsureEdit();
					break;
				case NativeMethods.WM_CLEAR:
					_link.Edit();
					Image = null;                  
					break;   					
				case NativeMethods.WM_CONTEXTMENU:
					BuildContextMenu();
					break;
			}                         
			base.WndProc(ref message);	
		}

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			if (field == DataField)
				Focus();
		}

		#region Menu

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

		private void SendWndProc(int NativeMethod)
		{             
			Message message = new Message();
			message.Msg = NativeMethod;
			WndProc(ref message);
		}

		protected void PasteClicked(object sender, EventArgs args)
		{
			SendWndProc(NativeMethods.WM_PASTE);
		}

		protected void CopyClicked(object sender, EventArgs args)
		{
			SendWndProc(NativeMethods.WM_COPY);
		}

		protected void ClearClicked(object sender, EventArgs args)
		{
			SendWndProc(NativeMethods.WM_CLEAR);
		}

		protected void LoadClicked(object sender, EventArgs args)
		{               
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = Strings.Get("DBImageAspect.OpenFileDialog.Filter"); 
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				using (FileStream imageFile = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
				{
					MemoryStream copy = new MemoryStream();
					StreamUtility.CopyStream(imageFile, copy);
					copy.Position = 0;
					Image = System.Drawing.Image.FromStream(copy);
				}
			}                                                           
		}

		protected void SaveAsClicked(object sender, EventArgs args)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = Strings.Get("DBImageAspect.SaveFileDialog.Filter");
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				Image.Save(saveFileDialog.FileName);
			} 
		}

		protected virtual void InternalBuildContextMenu()
		{
			DBImageAspectMenuItem item;

			// Capture...
			item = new DBImageAspectMenuItem(this, false, true);
			item.Text = Strings.Get("DBImageAspect.Menu.CaptureText");
			item.Click += new EventHandler(CaptureClicked);
			item.DefaultItem = true;
			ContextMenu.MenuItems.Add(item);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));

			//  Load...
			item = new DBImageAspectMenuItem(this, false, true);
			item.Text = Strings.Get("DBImageAspect.Menu.LoadText");
			item.Click += new EventHandler(LoadClicked);	 			
			ContextMenu.MenuItems.Add(item); 
		   
			// Save As...
			item = new DBImageAspectMenuItem(this, true, false);
			item.Text = Strings.Get("DBImageAspect.Menu.SaveAsText");
			item.Click += new EventHandler(SaveAsClicked);
			ContextMenu.MenuItems.Add(item);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));   
					
			// Copy
			item = new DBImageAspectMenuItem(this, true, false);
			item.Text = Strings.Get("DBImageAspect.Menu.CopyText");
			item.Click += new EventHandler(CopyClicked);
			item.Shortcut = Shortcut.CtrlC;
			ContextMenu.MenuItems.Add(item);

			// Paste
			item = new DBImageAspectMenuItem(this, false, true);
			item.Text = Strings.Get("DBImageAspect.Menu.PasteText");
			item.Click += new EventHandler(PasteClicked);
			item.Shortcut = Shortcut.CtrlV;
			ContextMenu.MenuItems.Add(item);

			// Clear
			item = new DBImageAspectMenuItem(this, true, false);
			item.Text = Strings.Get("DBImageAspect.Menu.ClearText");
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
					DBImageAspectMenuItem fileItem = item as DBImageAspectMenuItem;
					if (fileItem != null)
						fileItem.UpdateEnabled();
				}

				if (OnUpdateMenuItems != null)
					OnUpdateMenuItems(this, EventArgs.Empty);
			}
		}

		#endregion           
	}

	public class DBImageAspectMenuItem : MenuItem
	{
		public DBImageAspectMenuItem(DBImageAspect fileControl, bool readsData, bool writesData)
		{
			_fileControl = fileControl;
			_readsData = readsData;
			_writesData = writesData;
		}

		private DBImageAspect _fileControl;
		public DBImageAspect FileControl
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
				(!_writesData || !_fileControl.ReadOnly)
					&& (!_readsData || _fileControl.HasValue);
		}
	}
}

