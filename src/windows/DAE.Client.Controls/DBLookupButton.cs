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
			_link = new DAEClient.FieldDataLink();
			_link.OnDataChanged += new DAEClient.DataLinkHandler(SyncEnabled);
			_link.OnActiveChanged += new DAEClient.DataLinkHandler(SyncEnabled);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			Size = new System.Drawing.Size (21, 19);
			Image = ResourceBitmap(typeof(DBLookupButton), "Alphora.Dataphor.DAE.Client.Controls.Images.Lookup.bmp");
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				_link.OnDataChanged -= new DAEClient.DataLinkHandler(SyncEnabled);
				_link.OnActiveChanged -= new DAEClient.DataLinkHandler(SyncEnabled);
				_link.Dispose();
				_link = null;
			}
			base.Dispose(disposing);
		}

		private DAEClient.FieldDataLink _link;
		protected DAEClient.FieldDataLink Link
		{
			get { return _link;	}
		}

		[DefaultValue(false)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return _link.ReadOnly; }
			set	{ _link.ReadOnly = value; }
		}

		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set { _link.ColumnName = value; }
		}

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		public DAEClient.DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value;	}
		}

		[Browsable(false)]
		public DAEClient.DataField DataField
		{
			get { return _link.DataField; }
		}

		private bool _autoNextControl;
		[DefaultValue(false)]
		[Category("Behavior")]
		public virtual bool AutoNextControl
		{
			get { return _autoNextControl; }
			set	
			{
				if (_autoNextControl != value)
					_autoNextControl = value;
			}
		}

		protected virtual void SyncEnabled(DAEClient.DataLink dataLink, DAEClient.DataSet dataSet)
		{
			if (!DesignMode)
				Enabled = _link.Active && _link.CanModify();
		}

		protected Form ParentForm
		{
			get
			{
				Control parent = Parent;
				while ((parent != null) && !(parent is Form))
					parent = parent.Parent;
				return (Form)parent;
			}
		}

		protected override void OnClick(EventArgs eventArgs)
		{
			
			Lookup();
			base.OnClick(eventArgs);
			if (_autoNextControl)
			{
				if (ParentForm != null)
				{
					Control nextControl = ParentForm.GetNextControl(this, true);
					if (nextControl != null)
						nextControl.Focus();
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

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			if (field == DataField)
				Focus();
		}
	}
}
