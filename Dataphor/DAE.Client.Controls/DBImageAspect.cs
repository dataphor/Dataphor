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
					using (DAE.Runtime.Data.Scalar LNewValue = new DAE.Runtime.Data.Scalar(Link.DataSet.Process, Link.DataSet.Process.DataTypes.SystemGraphic))
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
				case Keys.Control | Keys.H :
				case Keys.Control | Keys.V :
				case Keys.Control | Keys.X :
					FLink.Edit();
					break;
			}
			base.OnKeyDown(AArgs);
		}

		protected override void WndProc(ref Message AMessage)
		{
			switch (AMessage.Msg)
			{
				case NativeMethods.WM_CUT:
				case NativeMethods.WM_PASTE:
				case NativeMethods.WM_UNDO:
				case NativeMethods.WM_CLEAR:
					EnsureEdit();
					break;
			}
			base.WndProc(ref AMessage);	
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
				Focus();
		}
	}
}

