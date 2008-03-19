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
		public const int CDefaultWidth = 15;
		public const int CBorderWidth = 12;

		protected DAE.Client.Controls.IncrementalSearch SearchControl
		{
			get { return ((IWindowsSearch)Parent).SearchControl; }
		}
		
		private DAE.Client.Controls.IncrementalSearchColumn FColumn;
		protected internal DAE.Client.Controls.IncrementalSearchColumn Column
		{
			get { return FColumn; }
		}

		[DefaultValue(false)]
		[Browsable(false)]
		public int PixelWidth
		{
			get { return CBorderWidth + (FWidth * ((IWindowsSearch)Parent).AverageCharPixelWidth); }
		}
		
		// Hint

		// TODO: Support hint in SearchColumn - stubbed now because it's a derivation common property
		private string FHint = String.Empty;
		[DefaultValue("")]
		[Description("A hint to be displayed for this search column.")]
		public string Hint
		{
			get { return FHint; }
			set { FHint = value; }
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

		private string FTitle = String.Empty;
		[DefaultValue("")]
		[Description("The title of the search column.")]
		public string Title
		{
			get { return FTitle; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (FTitle != value)
				{
					FTitle = value;
					if (Active)
						UpdateColumn();
				}
			}
		}

		// ColumnName
		private string FColumnName = String.Empty;
		[TypeConverter(typeof(ColumnNameConverter))]
		[DefaultValue("")]
		[Description("The name of the column within the data source.")]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (FColumnName != value)
				{
					FColumnName = value;
					if (Active)
						UpdateColumn();
				}
			}
		}

		// Width

		private int FWidth = CDefaultWidth;
        [DefaultValue(CDefaultWidth)]
		[Description("The width in characters of the search column.")]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					FWidth = value;
					if (Active)
						UpdateColumn();
				}
			}
		}
		
		// TextAlignment

		private HorizontalAlignment FTextAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("The alignment of content within the search column.")]
		public HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set
			{
				if (FTextAlignment != value)
				{
					FTextAlignment = value;
					if (Active)
						UpdateColumn();
				}
			}
		}
		
		private void UpdateColumn()
		{
			// TODO: Mnemonics support in the incremental search control
			FColumn.Title = 
				(
					FTitle.Length > 0 
						? 
						(
							FTitle[0] == '~' 
								? FTitle.Substring(1) 
								: TitleUtility.RemoveAccellerators(FTitle)
						) 
						: String.Empty
				);
			FColumn.ColumnName = FColumnName;
			FColumn.ControlWidth = PixelWidth;
			FColumn.TextAlignment = (WinForms.HorizontalAlignment)FTextAlignment;
		}

		// Node

		protected override void Activate()
		{
			FColumn = new DAE.Client.Controls.IncrementalSearchColumn();
			try
			{
				UpdateColumn();
				SearchControl.Columns.Add(Column);
				base.Activate();
			}
			catch
			{
				FColumn = null;
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
				if (FColumn != null)
				{
					SearchControl.Columns.Remove(FColumn);
					FColumn = null;
				}
			}
		}

		public override bool IsValidOwner(Type AOwnerType)
		{
			return typeof(ISearch).IsAssignableFrom(AOwnerType);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.Search')")]
	[DesignerCategory("Data Controls")]
	public class Search : ControlElement, IWindowsSearch
	{
		public const int CNaturalWidth = 40;
		public const int CMinWidth = 20;

		private int FAverageCharPixelWidth;
		[Browsable(false)]
		public int AverageCharPixelWidth
		{
			get { return FAverageCharPixelWidth; }
		}

		// TitleAlignment

		private TitleAlignment FTitleAlignment = TitleAlignment.Left;
		[DefaultValue(TitleAlignment.Left)]
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set
			{
				if (FTitleAlignment != value)
				{
					FTitleAlignment = value;
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
			SearchControl.TitleAlignment = (DAE.Client.Controls.TitleAlignment)FTitleAlignment;
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
			FAverageCharPixelWidth = FormInterface.GetAverageCharPixelWidth(Control);
			InternalUpdateTitleAlignment();
			base.InitializeControl();
		}

		// Node
		
		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(ISearchColumn).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}
		
		// Element

		protected override Size InternalMinSize
		{
			get { return new Size(CMinWidth, SearchControl.NaturalHeight()); }
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
				return new Size(CNaturalWidth, SearchControl.NaturalHeight());
			}
		}
	}
}