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
			FValueBackColor = BackColor;
		}

		/// <summary> Determines whether the control has a value. </summary>
		/// <returns> True if the control holds a valid value otherwise false. </returns>
		public abstract bool HasValue { get; }

		private Color FNoValueBackColor = ControlColor.NoValueBackColor;
		[Category("Colors")]
		[Description("Background color of the control when it has no value.")]
		public Color NoValueBackColor
		{
			get { return FNoValueBackColor; }
			set
			{
				if (FNoValueBackColor != value)
				{
					FNoValueBackColor = value;
					UpdateBackColor();
				}
			}
		}

		private Color FValueBackColor;
		protected Color ValueBackColor { get { return FValueBackColor; } }

		private bool FUpdatingBackColor;
		protected bool UpdatingBackColor
		{
			get { return FUpdatingBackColor; }
		}

		protected virtual void InternalUpdateBackColor()
		{
			if (!DesignMode)
				BackColor = HasValue ? FValueBackColor : NoValueBackColor;
		}

		/// <summary> Updates the <c>BackColor</c> based on whether or not the <c>DataField</c> has a value. </summary>
		protected void UpdateBackColor()
		{
			FUpdatingBackColor = true;
			try
			{
				InternalUpdateBackColor();
			}
			finally
			{
				FUpdatingBackColor = false;
			}
		}

		protected override void OnBackColorChanged(EventArgs AArgs)
		{
			base.OnBackColorChanged(AArgs);
			if (!UpdatingBackColor)
				FValueBackColor = BackColor;
		}

	}

	public delegate void RequestImageHandler();

	/// <summary> Data-aware image control. </summary>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBImageAspect),"Icons.DBImageAspect.bmp")]
	public class DBImageAspect : ExtendedImageAspect, IDataSourceReference, IColumnNameReference, IReadOnly
	{
		public const int CDefaultDelay = 1000;
		public DBImageAspect()
		{
			FDelayTimer = new System.Windows.Forms.Timer();
			FDelayTimer.Interval = CDefaultDelay;
			FDelayTimer.Enabled = false;
			FDelayTimer.Tick += new EventHandler(DelayTimerTick);
			FLink = new FieldDataLink();
			FLink.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			FLink.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			FLink.OnSaveRequested += new DataLinkHandler(SaveRequested);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				FDelayTimer.Tick -= new EventHandler(DelayTimerTick);
				FDelayTimer.Dispose();
				FDelayTimer = null;
				FLink.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
				FLink.OnUpdateReadOnly -= new EventHandler(UpdateReadOnly);
				FLink.OnSaveRequested -= new DataLinkHandler(SaveRequested);
				FLink.Dispose();
				FLink = null;
			}
			base.Dispose(ADisposing);
		}

		private FieldDataLink FLink;
		protected FieldDataLink Link { get { return FLink; } }

		[DefaultValue(false)]
		[Category("Behavior")]
		public override bool ReadOnly 
		{
			get { return FLink.ReadOnly || base.ReadOnly; }
			set
			{
				FLink.ReadOnly = value;
				base.ReadOnly = value;
			}
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
		public DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value;	}
		}

		private bool FAutoDisplay = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When false LoadImage should be called at runtime, else the control loads the image.")]
		public bool AutoDisplay
		{
			get { return FAutoDisplay; }
			set { FAutoDisplay = value;	}
		}

		private System.Windows.Forms.Timer FDelayTimer;
		protected System.Windows.Forms.Timer DelayTimer
		{
			get { return FDelayTimer; }
		}

		[DefaultValue(CDefaultDelay)]
		[Category("Behavior")]
		[Description("Milliseconds to delay before loading the image.")]
		public int DisplayDelay
		{
			get { return FDelayTimer.Interval; }
			set
			{
				if (FDelayTimer.Interval != value)
				{
					bool LSaveEnabled = FDelayTimer.Enabled;
					FDelayTimer.Enabled = false;
					FDelayTimer.Interval = value;
					if (FDelayTimer.Interval > 0)
						FDelayTimer.Enabled = LSaveEnabled;
				}
			}
		}
		
		[Browsable(false)]
		public DataField DataField { get { return FLink.DataField; } }

		/// <summary> Controls the internal getting and setting of the DataField's value. </summary>
		protected virtual Image FieldValue
		{
			get 
			{ 
				if ((DataField != null) && DataField.HasValue())
				{
					using (Stream LStream = DataField.Value.OpenStream())
					{
						MemoryStream LCopyStream = new MemoryStream();
						StreamUtility.CopyStream(LStream, LCopyStream);
						return Image.FromStream(LCopyStream);
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					using (DAE.Runtime.Data.Scalar LNewValue = new DAE.Runtime.Data.Scalar(Link.DataSet.Process.ValueManager, Link.DataSet.Process.DataTypes.SystemGraphic))
					{
						using (Stream LStream = LNewValue.OpenStream())
						{
							value.Save(LStream, value.RawFormat);
						}
						DataField.Value = LNewValue;
					}
				}
				else
					DataField.ClearValue();
			}
		}

		private void EnsureEdit()
		{
			if (!FLink.Edit())
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
					FLink.SaveRequested();
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

		protected virtual void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			if (!DesignMode)
			{
				base.ReadOnly = !FLink.Active || FLink.ReadOnly;
				base.Enabled = FLink.Active && !FLink.ReadOnly;
			}
		}

		private bool FHaveValue;
		public override bool HasValue {	get { return FHaveValue; } }
		private void SetHaveValue(bool AValue)
		{
			FHaveValue = AValue;
			UpdateBackColor();
		}

		protected override void InternalUpdateBackColor()
		{
			if (!FLink.Active)
				BackColor = ValueBackColor;
			else
				base.InternalUpdateBackColor();
		}

		protected override void OnImageChanged(EventArgs AArgs)
		{
			base.OnImageChanged(AArgs);
			SetHaveValue(DataField != null);
		}

		/// <summary> Loads the image from the view's data field. </summary>
		public virtual void LoadImage()
		{
			base.Image = FieldValue;
			SetHaveValue((DataField != null) && DataField.HasValue());
		}

		private void DelayTimerTick(object ASender, EventArgs AArgs)
		{
			FDelayTimer.Enabled = false;
			LoadImage();
		}

		protected virtual void FieldChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			FDelayTimer.Enabled = false;
			if (FAutoDisplay && !FLink.Modified)
			{
				if (FDelayTimer.Interval > 0)
					FDelayTimer.Enabled = true;
				else
					LoadImage();
			}
		}

		protected virtual void SaveRequested(DataLink ALink, DataSet ADataSet)
		{
			if (FLink.DataField != null)
				FieldValue = Image;
		}

		protected void Reset()
		{
			FLink.Reset();
		}
		
		protected override void OnLeave(EventArgs AEventArgs)
		{
			base.OnLeave(AEventArgs);
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

		public event RequestImageHandler OnCaptureRequested;
		
		private void CaptureClicked(object ASender, EventArgs AArgs)
		{
			if (OnCaptureRequested != null)
			{
				FLink.Edit();
				OnCaptureRequested();
			}
		}

        public event RequestImageHandler OnScanRequested;
        
        private void ScanClicked(object ASender, EventArgs AArgs)
        {
            if (OnScanRequested != null)
            {
                FLink.Edit();
                OnScanRequested();
            }
        }   
		
		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			switch (AArgs.KeyData)
			{
				case System.Windows.Forms.Keys.Escape :
					if (FLink.Modified)
					{
						Reset();
						AArgs.Handled = true;
					}
					break;
				case Keys.Delete:
					FLink.Edit();
					Image = null;
					AArgs.Handled = true;
					break;
				case Keys.Control | Keys.H :
				case Keys.Control | Keys.V :
				case Keys.Control | Keys.X :
					FLink.Edit();
					break;
			}
			base.OnKeyDown(AArgs);
		}

		const int WM_CONTEXTMENU = 0x007B;
		protected override void WndProc(ref Message AMessage)
		{
			switch (AMessage.Msg)
			{
				case NativeMethods.WM_CUT:
				case NativeMethods.WM_PASTE:
				case NativeMethods.WM_UNDO:
					EnsureEdit();
					break;
				case NativeMethods.WM_CLEAR:
					FLink.Edit();
					Image = null;                  
					break;   					
				case NativeMethods.WM_CONTEXTMENU:
					BuildContextMenu();
					break;
			}                         
			base.WndProc(ref AMessage);	
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
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
			Message LMessage = new Message();
			LMessage.Msg = NativeMethod;
			WndProc(ref LMessage);
		}

		protected void PasteClicked(object ASender, EventArgs AArgs)
		{
			SendWndProc(NativeMethods.WM_PASTE);
		}

		protected void CopyClicked(object ASender, EventArgs AArgs)
		{
			SendWndProc(NativeMethods.WM_COPY);
		}

		protected void ClearClicked(object ASender, EventArgs AArgs)
		{
			SendWndProc(NativeMethods.WM_CLEAR);
		}

		protected void LoadClicked(object sender, EventArgs AArgs)
		{               
			OpenFileDialog LOpenFileDialog = new OpenFileDialog();
			LOpenFileDialog.Filter = Strings.Get("DBImageAspect.OpenFileDialog.Filter"); 
			if (LOpenFileDialog.ShowDialog() == DialogResult.OK)
			{
				using (FileStream LImageFile = new FileStream(LOpenFileDialog.FileName, FileMode.Open, FileAccess.Read))
				{
					MemoryStream LCopy = new MemoryStream();
					StreamUtility.CopyStream(LImageFile, LCopy);
					LCopy.Position = 0;
					Image = System.Drawing.Image.FromStream(LCopy);
				}
			}                                                           
		}

		protected void SaveAsClicked(object sender, EventArgs AArgs)
		{
			SaveFileDialog LSaveFileDialog = new SaveFileDialog();
			LSaveFileDialog.Filter = Strings.Get("DBImageAspect.SaveFileDialog.Filter");
			if (LSaveFileDialog.ShowDialog() == DialogResult.OK)
			{
				Image.Save(LSaveFileDialog.FileName);
			} 
		}

		protected virtual void InternalBuildContextMenu()
		{
			DBImageAspectMenuItem LItem;

            // Scan...
            LItem = new DBImageAspectMenuItem(this, false, true);
            LItem.Text = Strings.Get("DBImageAspect.Menu.ScanText");
            LItem.Click += new EventHandler(ScanClicked);
            LItem.DefaultItem = true;
            ContextMenu.MenuItems.Add(LItem);
            
            // Capture...
			LItem = new DBImageAspectMenuItem(this, false, true);
			LItem.Text = Strings.Get("DBImageAspect.Menu.CaptureText");
			LItem.Click += new EventHandler(CaptureClicked);
            LItem.DefaultItem = false;
			ContextMenu.MenuItems.Add(LItem);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));

			//  Load...
			LItem = new DBImageAspectMenuItem(this, false, true);
			LItem.Text = Strings.Get("DBImageAspect.Menu.LoadText");
			LItem.Click += new EventHandler(LoadClicked);	 			
			ContextMenu.MenuItems.Add(LItem); 
		   
			// Save As...
			LItem = new DBImageAspectMenuItem(this, true, false);
			LItem.Text = Strings.Get("DBImageAspect.Menu.SaveAsText");
			LItem.Click += new EventHandler(SaveAsClicked);
			ContextMenu.MenuItems.Add(LItem);

			// -
			ContextMenu.MenuItems.Add(new MenuItem("-"));   
					
			// Copy
			LItem = new DBImageAspectMenuItem(this, true, false);
			LItem.Text = Strings.Get("DBImageAspect.Menu.CopyText");
			LItem.Click += new EventHandler(CopyClicked);
			LItem.Shortcut = Shortcut.CtrlC;
			ContextMenu.MenuItems.Add(LItem);

			// Paste
			LItem = new DBImageAspectMenuItem(this, false, true);
			LItem.Text = Strings.Get("DBImageAspect.Menu.PasteText");
			LItem.Click += new EventHandler(PasteClicked);
			LItem.Shortcut = Shortcut.CtrlV;
			ContextMenu.MenuItems.Add(LItem);

			// Clear
			LItem = new DBImageAspectMenuItem(this, true, false);
			LItem.Text = Strings.Get("DBImageAspect.Menu.ClearText");
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
					DBImageAspectMenuItem LFileItem = LItem as DBImageAspectMenuItem;
					if (LFileItem != null)
						LFileItem.UpdateEnabled();
				}

				if (OnUpdateMenuItems != null)
					OnUpdateMenuItems(this, EventArgs.Empty);
			}
		}

		#endregion           
	}

	public class DBImageAspectMenuItem : MenuItem
	{
		public DBImageAspectMenuItem(DBImageAspect AFileControl, bool AReadsData, bool AWritesData)
		{
			FFileControl = AFileControl;
			FReadsData = AReadsData;
			FWritesData = AWritesData;
		}

		private DBImageAspect FFileControl;
		public DBImageAspect FileControl
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
				(!FWritesData || !FFileControl.ReadOnly)
					&& (!FReadsData || FFileControl.HasValue);
		}
	}
}

