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
		public const int MinWidth = 50;
		public const int MinHeight = 100;
		public const int NaturalWidth = 120;
		public const int NaturalHeight = 180;
		
		// TreeViewControl

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DAE.Client.Controls.DBTreeView TreeViewControl
		{
			get { return (DAE.Client.Controls.DBTreeView)Control; }
		}

		// DataElement

		// ColumnName

		private string _columnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The name of the column which will be show in the tree.")]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (_columnName != value)
				{
					_columnName = value;
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
		private string _rootExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression defining the root set of nodes to display. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.  The master key and other parameters of the associated DataView are available as variables such as AMasterXXX (where XXX is the name of the detail column with '.'s changed to '_'s).")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string RootExpression
		{
			get { return _rootExpression; }
			set
			{
				if (_rootExpression != value)
				{
					_rootExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ChildExpression
		private string _childExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression defining the set of child nodes for a given parent node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ChildExpression
		{
			get { return _childExpression; }
			set
			{
				if (_childExpression != value)
				{
					_childExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ParentExpression
		private string _parentExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression defining the parent node for a given child node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName. If this result returns more than one row, only the first row will be used.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ParentExpression
		{
			get { return _parentExpression; }
			set
			{
				if (_parentExpression != value)
				{
					_parentExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		protected virtual void InternalUpdateTreeView()
		{
			TreeViewControl.RootExpression = _rootExpression;
			TreeViewControl.ChildExpression = _childExpression;
			TreeViewControl.ParentExpression = _parentExpression;
		}

		// Width

		private int _width = 25;
		[DefaultValue(25)]
		[Description("Approximate width (in characters) of the control.")]
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					_width = value;
					UpdateLayout();
				}
			}
		}

		// Height

		private int _height = 20;
		[DefaultValue(20)]
		[Description("Height (in rows) of the control.")]
		public int Height
		{
			get { return _height; }
			set
			{
				if (_height != value)
				{
					_height = value;
					UpdateLayout();
				}
			}
		}

		// ControlElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBTreeView();
		}

		private int _averageCharPixelWidth;

		protected override void InitializeControl()
		{
			Control.BackColor = ((Session)HostNode.Session).Theme.TreeColor;
			InternalUpdateTreeView();
			InternalUpdateColumnName();
			_averageCharPixelWidth = Element.GetAverageCharPixelWidth(Control);
			base.InitializeControl();
		}

		// Element

		protected override Size InternalMinSize
		{
			get { return new Size(MinWidth, MinHeight); }
		}
		
		protected override Size InternalMaxSize
		{
			get { return WinForms.Screen.FromControl(Control).WorkingArea.Size; }
		}

		protected override Size InternalNaturalSize
		{
			get 
			{ 
				Size clientSize = Control.ClientSize;
				return
					new Size
					(
						(_averageCharPixelWidth * _width) + (Control.Width - clientSize.Width) + WinForms.SystemInformation.VerticalScrollBarWidth, 
						(TreeViewControl.ItemHeight * _height) + (Control.Width - clientSize.Width) + WinForms.SystemInformation.HorizontalScrollBarHeight
					); 
			}
		}
	}
}