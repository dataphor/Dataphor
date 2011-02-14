/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria.Visual;
using Alphora.Dataphor.Frontend.Client.Windows;
using System.Xml.Linq;

namespace Alphora.Dataphor.Dataphoria.Analyzer
{
	public class PlanTree : TreeSurface
	{
		protected override IElementDesigner GetDesigner(object element)
		{
			SimplePlanNodeBox box = new SimplePlanNodeBox((Visual.TreeNode)element);
			box.Click += new EventHandler(DesignerClick);
			box.Disposed += new EventHandler(DesignerDisposed);
			return box;
		}

		public void Set(XElement root)
		{
			BeginUpdate();
			AddNode(Nodes, root).Expand(false);
			EndUpdate();
		}

		public void Clear()
		{
			BeginUpdate();
			Nodes.Clear();
			EndUpdate();
		}

		private Visual.TreeNode AddNode(TreeNodes nodes, XElement element)
		{
			Visual.TreeNode node = new Visual.TreeNode();
			node.IsExpanded = !_expandOnDemand;
			node.Element = element;
			foreach (XElement child in element.Elements())
			{
				if (child.Name.LocalName.IndexOf(".") < 1)
					AddNode(node.Children, child);
			}
			nodes.Add(node);
			return node;
		}

		private bool _expandOnDemand = true;
		public bool ExpandOnDemand
		{
			get { return _expandOnDemand; }
			set
			{
				if (value != _expandOnDemand)
				{
					_expandOnDemand = value;
					if (!value && (Nodes.Count > 0))
						Nodes[0].Expand(true);
				}
			}
		}

		private void ExpandNode(IElementDesigner designer)
		{
			if (designer != null)
			{
				Visual.TreeNode node = (Visual.TreeNode)designer.Element;

				// Collapse all children
				foreach (Visual.TreeNode child in node.Children)
					child.Collapse(false);

				// Expand this node
				node.Expand(false);
			}
		}

		private void DesignerClick(object sender, EventArgs args)
		{
			if (_expandOnDemand)
				ExpandNode(sender as IElementDesigner);
		}

		private void DesignerDisposed(object sender, EventArgs args)
		{
			((Control)sender).Click -= new EventHandler(DesignerClick);
		}
	}

	public class SimplePlanNodeBox : TextDesignerBox, IElementDesigner
	{
		public SimplePlanNodeBox(Visual.TreeNode node)
		{
			_node = node;
			XElement element = (XElement)((Visual.TreeNode)node).Element;

			XAttribute description = element.Attribute("Description");
			if ((description == null) || (description.Value == String.Empty))
				Text = element.Name.LocalName;
			else
				Text = description.Value;

			XAttribute deviceSupported = element.Attribute("DeviceSupported");
			if ((deviceSupported == null) || (deviceSupported.Value.ToLower() != "true"))
				SurfaceColor = Color.FromArgb(200, 230, 200);
			else
				SurfaceColor = Color.FromArgb(230, 200, 200);

			XAttribute category = element.Attribute("Category");
			if (category != null)
				if (category.Value == "Instruction")
					RoundRadius = 10;
				else if (category.Value == "Unknown")
					RoundRadius = 20;
		}

		private Visual.TreeNode _node;
		public Visual.TreeNode Node { get { return _node; } }

		public object Element { get { return _node; } }

		public override void ZoomIn()
		{
			XElement element = (XElement)Node.Element;
			
			DetailedPlanNodeBox box = new DetailedPlanNodeBox(element);
			box.RoundRadius = this.RoundRadius;
			box.SurfaceColor = this.SurfaceColor;
			SingleElementSurface surface = new SingleElementSurface(element, box);

			DesignerControl control = DesignerControl.GetDesigner(this);
			control.Push(surface, Text);
		}

		public override bool CanZoomIn()
		{
			return true;
		}
	}

	public class ElementGroup : ArrayList
	{
		private string _name = String.Empty;
		public string Name
		{
			get { return _name; }
			set { _name = (value == null ? String.Empty : value); }
		}
	}

	public class DetailedPlanNodeBox : FloatingBox, IElementDesigner
	{
		public static Color CTabSurfaceColor = Color.FromArgb(200, 230, 230);

		public DetailedPlanNodeBox(XElement element)
		{
			SuspendLayout();

			TabStop = false;
			SetStyle(ControlStyles.Selectable, false);
			NormalDepth = MaxDepth;
			SurfaceColor = Color.FromArgb(190, 190, 220);

			_element = element;

			_groups = GetChildGroups();

			_properties = new Table();
			_properties.BeginUpdate();
			_properties.BackColor = SurfaceColor;
			_properties.ForeColor = ForeColor;
			TableColumn column = new TableColumn(_properties);
			column.Name = "Attribute";
			column.Title = Strings.Analyzer_AttributeTable_AttributeTitle;
			_properties.Columns.Add(column);
			column = new TableColumn(_properties);
			column.Name = "Value";
			column.Title = Strings.Analyzer_AttributeTable_ValueTitle;
			_properties.Columns.Add(column);
			_properties.OnGetValue += new GetTableValueHandler(PropertiesGetValue);
			_properties.OnGetDesignerRequired += new GetTableDesignerRequiredHandler(PropertiesGetDesignerRequired);
			_properties.OnGetDesigner += new GetTableDesignerHandler(PropertiesGetDesigner);
			Controls.Add(_properties);
			foreach (XAttribute attribute in _element.Attributes())
				_properties.Rows.Add(attribute);
			_properties.EndUpdate();

			if (_groups.Count > 0)
			{
				_notebook = new DAE.Client.Controls.Notebook();
				Controls.Add(_notebook);
			}
		
			// Place a table for each group
			foreach (DictionaryEntry entry in _groups)
			{
				DAE.Client.Controls.NotebookPage page = new DAE.Client.Controls.NotebookPage();
				page.BackColor = CTabSurfaceColor;
				page.Text = (string)entry.Key;
				page.Controls.Add(CreateGroupTable((ElementGroup)entry.Value));
				page.Controls[0].Dock = DockStyle.Fill;
				_notebook.Pages.Add(page);
			}

			ResumeLayout(false);
		}

		private XElement _element;
		public object Element { get { return _element; } }

		#region Properties

		private Table _properties;
		public Table Properties { get { return _properties; } }

		protected override void SurfaceColorChanged()
		{
			base.SurfaceColorChanged();
			if (_properties != null)
				_properties.BackColor = SurfaceColor;
		}

		private string PropertiesGetValue(Table table, Point cell)
		{
			XAttribute attribute = (XAttribute)_properties.Rows[cell.Y];
			if (cell.X == 0)
				return attribute.Name.LocalName;
			else
				return attribute.Value;
		}

		private bool PropertiesGetDesignerRequired(Table table, Point cell, Graphics graphics, Rectangle bounds)
		{
			Size localBounds = Size.Ceiling(graphics.MeasureString(PropertiesGetValue(table, cell), table.Font));
			return (localBounds.Width > bounds.Width) || (localBounds.Height > bounds.Height);
		}

		private IElementDesigner PropertiesGetDesigner(Table table, Point cell)
		{
			CellDesignerBox box = new CellDesignerBox();
			box.TextHAlign = HorizontalAlignment.Left;
			box.Text = PropertiesGetValue(table, cell);
			return box;
		}

		#endregion

		#region Child Groups

		private DAE.Client.Controls.Notebook _notebook;
		
		private IDictionary _groups;
		public IDictionary Groups { get { return _groups; } }

		/// <summary> Get the set of child elements grouped by their qualifiers. </summary>
		private IDictionary GetChildGroups()
		{
			HybridDictionary groups = new HybridDictionary();
			string qualifierName;
			ElementGroup group;
			foreach (XElement element in _element.Elements())
			{
				if (element.Name.LocalName.IndexOf(".") >= 0)
				{
					qualifierName = element.Name.LocalName.Substring(0, element.Name.LocalName.IndexOf("."));
					group = groups[qualifierName] as ElementGroup;
					if (group != null)
						group.Add(element);
					else
					{
						group = new ElementGroup();
						group.Name = qualifierName;
						group.Add(element);
						groups.Add(qualifierName, group);
					}
				}
			}
			return groups;
		}

		private Table CreateGroupTable(ElementGroup group)
		{
			Table groupTable = new Table();
			groupTable.BackColor = CTabSurfaceColor;
			groupTable.Tag = group;
			groupTable.OnGetValue += new GetTableValueHandler(GroupsGetValue);
			groupTable.OnGetDesignerRequired += new GetTableDesignerRequiredHandler(GroupsGetDesignerRequired);
			groupTable.OnGetDesigner += new GetTableDesignerHandler(GroupsGetDesigner);
			groupTable.BeginUpdate();
			TableColumn column;
			foreach (XElement element in group)
			{
				// Ensure that there are columns for each attribute
				foreach (XAttribute attribute in element.Attributes())
				{
					column = groupTable.Columns[attribute.Name.LocalName];
					if (column == null)
					{
						column = new TableColumn(groupTable);
						column.Name = attribute.Name.LocalName;
						column.Title = attribute.Name.LocalName;
						groupTable.Columns.Add(column);
					}
				}

				// Ensure that there is a 'Details' column if there are any child elements
				if (element.HasElements)
				{
					column = groupTable.Columns["InternalDetails"];
					if (column == null)
					{
						column = new TableColumn(groupTable);
						column.Name = "InternalDetails";
						column.Title = Strings.Analyzer_DetailsColumnTitle;
						groupTable.Columns.Add(column);
					}
				}

				// Add a row for each element
				groupTable.Rows.Add(element);
			}
			groupTable.EndUpdate();
			return groupTable;
		}
		
		private string GroupsGetValue(Table table, Point cell)
		{
			string columnName = table.Columns[cell.X].Name;
			XElement element = (XElement)((ElementGroup)table.Tag)[cell.Y];

			if (columnName == "InternalDetails")
				return String.Empty;
			else
			{
				XAttribute attribute = element.Attribute(columnName);
				if (attribute == null)
					return String.Empty;
				else
					return attribute.Value;
			}
		}

		private bool GroupsGetDesignerRequired(Table table, Point cell, Graphics graphics, Rectangle bounds)
		{
			string columnName = table.Columns[cell.X].Name;

			if (columnName == "InternalDetails")
				return true;
			else
			{
				Size localBounds = Size.Ceiling(graphics.MeasureString(GroupsGetValue(table, cell), table.Font));
				return (localBounds.Width > bounds.Width) || (localBounds.Height > bounds.Height);
			}
		}

		private IElementDesigner GroupsGetDesigner(Table table, Point cell)
		{
			string columnName = table.Columns[cell.X].Name;
			if (columnName == "InternalDetails")
				return new DetailsCellDesignerBox((XElement)((ElementGroup)table.Tag)[cell.Y]);
			else
			{
				CellDesignerBox box = new CellDesignerBox();
				box.TextHAlign = HorizontalAlignment.Left;
				box.Text = GroupsGetValue(table, cell);
				return box;
			}
		}

		#endregion

		protected override void OnLayout(System.Windows.Forms.LayoutEventArgs args)
		{
			base.OnLayout(args);

			Rectangle bounds = DisplayRectangle;
			bounds.Inflate(-5, -5);

			// Give a quarter of the area to the properties table
			_properties.Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height / 4);

			// Distribute the remaining space evenly to the group tables
			if (Controls.Count > 1)
			{
				bounds.Y += bounds.Height / 4;
				bounds.Height -= bounds.Height / 4;
				int tableHeight = bounds.Height / (Controls.Count - 1);
				for (int i = 1; i < Controls.Count; i++)
				{
					Controls[i].Bounds = 
						new Rectangle
						(
							bounds.X, 
							bounds.Y + ((i - 1) * tableHeight),
							bounds.Width,
							tableHeight
						);
				}
			}
		}
	}

	public class DetailsCellDesignerBox : TextDesignerBox, IElementDesigner
	{
		public DetailsCellDesignerBox(XElement element)
		{
			SuspendLayout();
			_element = element;
			Text = Strings.Analyzer_DetailsColumnTitle;
			MaxDepth = 3;
			NormalDepth = 1;
			ForeColor = Color.White;
			SurfaceColor = Color.FromArgb(100, 100, 140);
			HighlightColor = Color.Yellow;
			ResumeLayout(false);
		}

		private XElement _element;
		public object Element { get { return _element; } }

		private bool HasUnqualifiedChildElement(XElement element)
		{
			foreach (XElement child in element.Elements())
			{
				if (child.Name.LocalName.IndexOf(".") < 0)
					return true;
			}
			return false;
		}

		public override void ZoomIn()
		{
			if (HasUnqualifiedChildElement(_element))
			{
				PlanTree surface = new PlanTree();
				surface.Set(_element);
				DesignerControl.GetDesigner(this).Push(surface, String.Format(Strings.Analyzer_PlanNodeTitle, _element.Name.LocalName));
			}
			else
				DesignerControl.GetDesigner(this).Push(new SingleElementSurface(_element, new DetailedPlanNodeBox(_element)), String.Format(Strings.Analyzer_DetailNodeTitle, _element.Name.LocalName));
		}

		public override bool CanZoomIn()
		{
			return true;
		}
	}

	public class CellDesignerBox : TextDesignerBox, IElementDesigner
	{
		public CellDesignerBox()
		{
			SuspendLayout();
			MaxDepth = 3;
			NormalDepth = 1;
			SurfaceColor = SystemColors.Control;
			ResumeLayout(false);
		}

		public object Element { get { return null; } }

		public override void ZoomIn()
		{
			CellDetailBox box = new CellDetailBox(Text);
			SingleElementSurface surface = new SingleElementSurface(null, box);
			DesignerControl control = DesignerControl.GetDesigner(this);
			control.Push(surface, Strings.Analyzer_TextDetailTitle);
		}

		public override bool CanZoomIn()
		{
			return true;
		}
	}

	public class CellDetailBox : FloatingBox, IElementDesigner
	{
		public CellDetailBox(string text)
		{
			SuspendLayout();

			NormalDepth = MaxDepth;
			
			_textEdit = new Alphora.Dataphor.Dataphoria.TextEditor.ResultPanel();
			_textEdit.SetText(text);
			Controls.Add(_textEdit);

			ResumeLayout(false);
		}

		private TextEdit _textEdit;
		public TextEdit TextEdit
		{
			get { return _textEdit; }
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);
			_textEdit.Bounds = DisplayRectangle;
		}

		public object Element
		{
			get { return _textEdit.Document.TextContent; }
		}
	}
}
