/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using WinForms = System.Windows.Forms;

using DAE = Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.Frontend.Client;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.SearchColumn')")]
	[DesignerCategory("Data Controls")]
	public class SearchColumn : Node, IWindowsSearchColumn
	{
		public const int DefaultWidth = 15;
		public const int BorderWidth = 12;

		protected DAE.Client.Controls.IncrementalSearch SearchControl
		{
			get { return ((IWindowsSearch)Parent).SearchControl; }
		}
		
		private DAE.Client.Controls.IncrementalSearchColumn _column;
		protected internal DAE.Client.Controls.IncrementalSearchColumn Column
		{
			get { return _column; }
		}

		[DefaultValue(false)]
		[Browsable(false)]
		public int PixelWidth
		{
			get { return BorderWidth + (_width * ((IWindowsSearch)Parent).AverageCharPixelWidth); }
		}
		
		// Hint

		// TODO: Support hint in SearchColumn - stubbed now because it's a derivation common property
		private string _hint = String.Empty;
		[DefaultValue("")]
		[Description("A hint to be displayed for this search column.")]
		public string Hint
		{
			get { return _hint; }
			set { _hint = value; }
		}
		
		// ReadOnly

		// The following property has no meaning, but has to be here and be settable cuz it's a derivation common property.
		[DefaultValue(false)]
		[Browsable(false)]
		public bool ReadOnly
		{
			get { return false; }
			set { }
		}

		// Title

		private string _title = String.Empty;
		[DefaultValue("")]
		[Description("The title of the search column.")]
		public string Title
		{
			get { return _title; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (_title != value)
				{
					_title = value;
					if (Active)
						UpdateColumn();
				}
			}
		}

		// ColumnName
		private string _columnName = String.Empty;
		[TypeConverter(typeof(ColumnNameConverter))]
		[DefaultValue("")]
		[Description("The name of the column within the data source.")]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (_columnName != value)
				{
					_columnName = value;
					if (Active)
						UpdateColumn();
				}
			}
		}

		// Width

		private int _width = DefaultWidth;
        [DefaultValue(DefaultWidth)]
		[Description("The width in characters of the search column.")]
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					_width = value;
					if (Active)
						UpdateColumn();
				}
			}
		}
		
		// TextAlignment

		private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("The alignment of content within the search column.")]
		public HorizontalAlignment TextAlignment
		{
			get { return _textAlignment; }
			set
			{
				if (_textAlignment != value)
				{
					_textAlignment = value;
					if (Active)
						UpdateColumn();
				}
			}
		}
		
		private void UpdateColumn()
		{
			// TODO: Mnemonics support in the incremental search control
			_column.Title = 
				(
					_title.Length > 0 
						? 
						(
							_title[0] == '~' 
								? _title.Substring(1) 
								: TitleUtility.RemoveAccellerators(_title)
						) 
						: String.Empty
				);
			_column.ColumnName = _columnName;
			_column.ControlWidth = PixelWidth;
			_column.TextAlignment = (WinForms.HorizontalAlignment)_textAlignment;
		}

		// Node

		protected override void Activate()
		{
			_column = new DAE.Client.Controls.IncrementalSearchColumn();
			try
			{
				UpdateColumn();
				SearchControl.Columns.Add(Column);
				base.Activate();
			}
			catch
			{
				_column = null;
				throw;
			}
		}
		
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				if (_column != null)
				{
					SearchControl.Columns.Remove(_column);
					_column = null;
				}
			}
		}

		public override bool IsValidOwner(Type ownerType)
		{
			return typeof(ISearch).IsAssignableFrom(ownerType);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.Search')")]
	[DesignerCategory("Data Controls")]
	public class Search : ControlElement, IWindowsSearch
	{
		public const int NaturalWidth = 40;
		public const int MinWidth = 20;

		private int _averageCharPixelWidth;
		[Browsable(false)]
		public int AverageCharPixelWidth
		{
			get { return _averageCharPixelWidth; }
		}

		// TitleAlignment

		private TitleAlignment _titleAlignment = TitleAlignment.Left;
		[DefaultValue(TitleAlignment.Left)]
		public TitleAlignment TitleAlignment
		{
			get { return _titleAlignment; }
			set
			{
				if (_titleAlignment != value)
				{
					_titleAlignment = value;
					if (Active)
					{
						InternalUpdateTitleAlignment();
						UpdateLayout();
					}
				}
			}
		}

		protected void InternalUpdateTitleAlignment()
		{
			SearchControl.TitleAlignment = (DAE.Client.Controls.TitleAlignment)_titleAlignment;
		}

		// SearchControl

		[DefaultValue(false)]
		[Browsable(false)]
		public DAE.Client.Controls.IncrementalSearch SearchControl
		{
			get { return (DAE.Client.Controls.IncrementalSearch)Control; }
		}
		
		// DataElement

		protected override void InternalUpdateReadOnly()
		{
			// Don't need readonly for search control
		}

		// ControlElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.IncrementalSearch();
		}

		protected override void InitializeControl()
		{
			SearchControl.BackColor = ((Session)HostNode.Session).Theme.ContainerColor;
			SearchControl.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			SearchControl.InvalidValueBackColor = ((Session)HostNode.Session).Theme.InvalidValueBackColor;
			_averageCharPixelWidth = FormInterface.GetAverageCharPixelWidth(Control);
			InternalUpdateTitleAlignment();
			base.InitializeControl();
		}

		// Node
		
		public override bool IsValidChild(Type childType)
		{
			if (typeof(ISearchColumn).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}
		
		// Element

		protected override Size InternalMinSize
		{
			get { return new Size(MinWidth, SearchControl.NaturalHeight()); }
		}
		
		protected override Size InternalMaxSize
		{
			get { return new Size(WinForms.Screen.FromControl(Control).WorkingArea.Width, SearchControl.NaturalHeight()); }
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				// In order for this to be based on the actual widths of the displayed search controls, it
				// would be necessary re-layout when the search criteria changes (probably not a good thing).
				return new Size(NaturalWidth, SearchControl.NaturalHeight());
			}
		}
	}
}