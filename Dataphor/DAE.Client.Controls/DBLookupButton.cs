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
using System.Runtime.InteropServices;
using DAEClient = Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBLookupButton),"Icons.DBLookup.bmp")]
	public class DBLookupButton	: BitmapButton, DAEClient.IDataSourceReference, DAEClient.IColumnNameReference
	{
		/// <summary> Initializes a new instance of DBLookupButton. </summary>
		public DBLookupButton()	: base()
		{
			FLink = new DAEClient.FieldDataLink();
			FLink.OnDataChanged += new DAEClient.DataLinkHandler(SyncEnabled);
			FLink.OnActiveChanged += new DAEClient.DataLinkHandler(SyncEnabled);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			Size = new System.Drawing.Size (21, 19);
			Image = ResourceBitmap(typeof(DBLookupButton), "Alphora.Dataphor.DAE.Client.Controls.Images.Lookup.bmp");
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				FLink.OnDataChanged -= new DAEClient.DataLinkHandler(SyncEnabled);
				FLink.OnActiveChanged -= new DAEClient.DataLinkHandler(SyncEnabled);
				FLink.Dispose();
				FLink = null;
			}
			base.Dispose(ADisposing);
		}

		private DAEClient.FieldDataLink FLink;
		protected DAEClient.FieldDataLink Link
		{
			get { return FLink;	}
		}

		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return FLink.ReadOnly; }
			set	{ FLink.ReadOnly = value; }
		}

		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FLink.ColumnName; }
			set { FLink.ColumnName = value; }
		}

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		public DAEClient.DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value;	}
		}

		[Browsable(false)]
		public DAEClient.DataField DataField
		{
			get { return FLink.DataField; }
		}

		private bool FAutoNextControl;
		[DefaultValue(false)]
		[Category("Behavior")]
		public virtual bool AutoNextControl
		{
			get { return FAutoNextControl; }
			set	
			{
				if (FAutoNextControl != value)
					FAutoNextControl = value;
			}
		}

		protected virtual void SyncEnabled(DAEClient.DataLink ADataLink, DAEClient.DataSet ADataSet)
		{
			if (!DesignMode)
				Enabled = FLink.Active && FLink.CanModify();
		}

		protected Form ParentForm
		{
			get
			{
				Control LParent = Parent;
				while ((LParent != null) && !(LParent is Form))
					LParent = LParent.Parent;
				return (Form)LParent;
			}
		}

		protected override void OnClick(EventArgs AEventArgs)
		{
			
			Lookup();
			base.OnClick(AEventArgs);
			if (FAutoNextControl)
			{
				if (ParentForm != null)
				{
					Control LNextControl = ParentForm.GetNextControl(this, true);
					if (LNextControl != null)
						LNextControl.Focus();
				}
			}
		}

		[Category("Data")]
		[Description("Triggered when the lookup button is clicked.")]
		public event EventHandler OnLookup;
		protected virtual void Lookup()
		{
			if (OnLookup != null)
				OnLookup(this, EventArgs.Empty);
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
				Focus();
		}
	}
}
