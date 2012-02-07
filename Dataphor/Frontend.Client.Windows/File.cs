/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.DAE.Client.Controls;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.File')")]
	[DesignerCategory("Data Controls")]
	public class File : TitledElement, IFile
	{
		public DBFile FileControl { get { return (DBFile)Control; } }

		// ExtensionColumnName

		private string _extensionColumnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The name of the column in the data source containing the extension of the file that this controls is associated with.")]
		public string ExtensionColumnName
		{
			get { return _extensionColumnName; }
			set
			{
				if (_extensionColumnName != value)
				{
					_extensionColumnName = value;
					if (Active)
						InternalUpdateExtensionColumnName();
				}
			}
		}

		protected virtual void InternalUpdateExtensionColumnName()
		{
			FileControl.ExtensionColumnName = _extensionColumnName;
		}

		// NameColumnName

		private string _nameColumnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The name of the column in the data source containing the name of the file that this controls is associated with.")]
		public string NameColumnName
		{
			get { return _nameColumnName; }
			set
			{
				if (_nameColumnName != value)
				{
					_nameColumnName = value;
					if (Active)
						InternalUpdateNameColumnName();
				}
			}
		}

		protected virtual void InternalUpdateNameColumnName()
		{
			FileControl.NameColumnName = _nameColumnName;
		}

		// MaximumContentLength

        private long _maximumContentLength = DBFileForm.DefaultMaximumContentLength;
		/// <summary> Maximum size in bytes for documents to be loaded into this control. </summary>
        [DefaultValue(DBFileForm.DefaultMaximumContentLength)]
		[Description("Maximum size in bytes for documents to be loaded into this control.")]
		public long MaximumContentLength
		{
			get { return _maximumContentLength; }
			set
			{
				if (_maximumContentLength != value)
				{
					_maximumContentLength = value;
					if (Active)
						InternalUpdateMaximumContentLength();
				}
			}
		}
		
		protected virtual void InternalUpdateMaximumContentLength()
		{
			FileControl.MaximumContentLength = _maximumContentLength;
		}
		
		// ControlElement

		protected override System.Windows.Forms.Control CreateControl()
		{
			return new DBFile();
		}

		protected override void InitializeControl()
		{
			base.InitializeControl();
			InternalUpdateExtensionColumnName();
			InternalUpdateNameColumnName();
			InternalUpdateMaximumContentLength();
		}

		protected override void LayoutControl(System.Drawing.Rectangle bounds)
		{
			Control.Location = bounds.Location;
		}

		// TitledElement

		protected override bool EnforceMaxHeight()
		{
			return true;
		}

		protected override int GetControlNaturalWidth()
		{
			return Control.Width;
		}
		
		protected override int GetControlMaxWidth()
		{
			return Control.Width;
		}
		
		protected override int GetControlMinWidth()
		{
			return Control.Width;
		}
		
		protected override int GetControlNaturalHeight()
		{
			return Control.Height;
		}
		
		protected override int GetControlMaxHeight()
		{
			return Control.Height;
		}
		
		protected override int GetControlMinHeight()
		{
			return Control.Height;
		}
	}
}
