/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using DAE = Alphora.Dataphor.DAE;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.Tree')")]
	[DesignerCategory("Data Controls")]
	public class Tree : ControlElement, ITree
	{
		public const int CMinWidth = 50;
		public const int CMinHeight = 100;
		public const int CNaturalWidth = 120;
		public const int CNaturalHeight = 180;
		
		// TreeViewControl

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DAE.Client.Controls.DBTreeView TreeViewControl
		{
			get { return (DAE.Client.Controls.DBTreeView)Control; }
		}

		// DataElement

		// ColumnName

		private string FColumnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The name of the column which will be show in the tree.")]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FColumnName != value)
				{
					FColumnName = value;
					if (Active)
						InternalUpdateColumnName();
				}
			}
		}

		protected virtual void InternalUpdateColumnName()
		{
			((DAE.Client.IColumnNameReference)Control).ColumnName = ColumnName;
		}

		protected override void InternalUpdateReadOnly()
		{
			TreeViewControl.ReadOnly = GetReadOnly();
		}
		
		// RootExpression
		private string FRootExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression defining the root set of nodes to display. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.  The master key and other parameters of the associated DataView are available as variables such as AMasterXXX (where XXX is the name of the detail column with '.'s changed to '_'s).")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string RootExpression
		{
			get { return FRootExpression; }
			set
			{
				if (FRootExpression != value)
				{
					FRootExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ChildExpression
		private string FChildExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression defining the set of child nodes for a given parent node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ChildExpression
		{
			get { return FChildExpression; }
			set
			{
				if (FChildExpression != value)
				{
					FChildExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ParentExpression
		private string FParentExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression defining the parent node for a given child node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName. If this result returns more than one row, only the first row will be used.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ParentExpression
		{
			get { return FParentExpression; }
			set
			{
				if (FParentExpression != value)
				{
					FParentExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		protected virtual void InternalUpdateTreeView()
		{
			TreeViewControl.RootExpression = FRootExpression;
			TreeViewControl.ChildExpression = FChildExpression;
			TreeViewControl.ParentExpression = FParentExpression;
		}

		// Width

		private int FWidth = 25;
		[DefaultValue(25)]
		[Description("Approximate width (in characters) of the control.")]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					FWidth = value;
					UpdateLayout();
				}
			}
		}

		// Height

		private int FHeight = 20;
		[DefaultValue(20)]
		[Description("Height (in rows) of the control.")]
		public int Height
		{
			get { return FHeight; }
			set
			{
				if (FHeight != value)
				{
					FHeight = value;
					UpdateLayout();
				}
			}
		}

		// ControlElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBTreeView();
		}

		private int FAverageCharPixelWidth;

		protected override void InitializeControl()
		{
			Control.BackColor = ((Session)HostNode.Session).Theme.TreeColor;
			InternalUpdateTreeView();
			InternalUpdateColumnName();
			FAverageCharPixelWidth = Element.GetAverageCharPixelWidth(Control);
			base.InitializeControl();
		}

		// Element

		protected override Size InternalMinSize
		{
			get { return new Size(CMinWidth, CMinHeight); }
		}
		
		protected override Size InternalMaxSize
		{
			get { return WinForms.Screen.FromControl(Control).WorkingArea.Size; }
		}

		protected override Size InternalNaturalSize
		{
			get 
			{ 
				Size LClientSize = Control.ClientSize;
				return
					new Size
					(
						(FAverageCharPixelWidth * FWidth) + (Control.Width - LClientSize.Width) + WinForms.SystemInformation.VerticalScrollBarWidth, 
						(TreeViewControl.ItemHeight * FHeight) + (Control.Width - LClientSize.Width) + WinForms.SystemInformation.HorizontalScrollBarHeight
					); 
			}
		}
	}
}