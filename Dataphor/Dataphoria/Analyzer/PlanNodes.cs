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

namespace Alphora.Dataphor.Dataphoria.Analyzer
{
	public class PlanTree : TreeSurface
	{
		protected override IElementDesigner GetDesigner(object AElement)
		{
			SimplePlanNodeBox LBox = new SimplePlanNodeBox((Visual.TreeNode)AElement);
			LBox.Click += new EventHandler(DesignerClick);
			LBox.Disposed += new EventHandler(DesignerDisposed);
			return LBox;
		}

		public void Set(XmlElement ARoot)
		{
			BeginUpdate();
			AddNode(Nodes, ARoot).Expand(false);
			EndUpdate();
		}

		public void Clear()
		{
			BeginUpdate();
			Nodes.Clear();
			EndUpdate();
		}

		private Visual.TreeNode AddNode(TreeNodes ANodes, XmlElement AElement)
		{
			Visual.TreeNode LNode = new Visual.TreeNode();
			LNode.IsExpanded = !FExpandOnDemand;
			LNode.Element = AElement;
			XmlElement LChild;
			foreach (XmlNode LXmlNode in AElement.ChildNodes)
			{
				LChild = LXmlNode as XmlElement;
				if ((LChild != null) && (LChild.LocalName.IndexOf(".") < 1))
					AddNode(LNode.Children, LChild);
			}
			ANodes.Add(LNode);
			return LNode;
		}

		private bool FExpandOnDemand = true;
		public bool ExpandOnDemand
		{
			get { return FExpandOnDemand; }
			set
			{
				if (value != FExpandOnDemand)
				{
					FExpandOnDemand = value;
					if (!value && (Nodes.Count > 0))
						Nodes[0].Expand(true);
				}
			}
		}

		private void ExpandNode(IElementDesigner ADesigner)
		{
			if (ADesigner != null)
			{
				Visual.TreeNode LNode = (Visual.TreeNode)ADesigner.Element;

				// Collapse all children
				foreach (Visual.TreeNode LChild in LNode.Children)
					LChild.Collapse(false);

				// Expand this node
				LNode.Expand(false);
			}
		}

		private void DesignerClick(object ASender, EventArgs AArgs)
		{
			if (FExpandOnDemand)
				ExpandNode(ASender as IElementDesigner);
		}

		private void DesignerDisposed(object ASender, EventArgs AArgs)
		{
			((Control)ASender).Click -= new EventHandler(DesignerClick);
		}
	}

	public class SimplePlanNodeBox : TextDesignerBox, IElementDesigner
	{
		public SimplePlanNodeBox(Visual.TreeNode ANode)
		{
			FNode = ANode;
			XmlElement LElement = (XmlElement)((Visual.TreeNode)ANode).Element;

			XmlAttribute LDescription = LElement.Attributes["Description"];
			if ((LDescription == null) || (LDescription.Value == String.Empty))
				Text = LElement.LocalName;
			else
				Text = LDescription.Value;

			XmlAttribute LDeviceSupported = LElement.Attributes["DeviceSupported"];
			if ((LDeviceSupported == null) || (LDeviceSupported.Value.ToLower() != "true"))
				SurfaceColor = Color.FromArgb(200, 230, 200);
			else
				SurfaceColor = Color.FromArgb(230, 200, 200);

			XmlAttribute LCategory = LElement.Attributes["Category"];
			if (LCategory != null)
				if (LCategory.Value == "Instruction")
					RoundRadius = 10;
				else if (LCategory.Value == "Unknown")
					RoundRadius = 20;
		}

		private Visual.TreeNode FNode;
		public Visual.TreeNode Node { get { return FNode; } }

		public object Element { get { return FNode; } }

		public override void ZoomIn()
		{
			XmlElement LElement = (XmlElement)Node.Element;
			
			DetailedPlanNodeBox LBox = new DetailedPlanNodeBox(LElement);
			LBox.RoundRadius = this.RoundRadius;
			LBox.SurfaceColor = this.SurfaceColor;
			SingleElementSurface LSurface = new SingleElementSurface(LElement, LBox);

			DesignerControl LControl = DesignerControl.GetDesigner(this);
			LControl.Push(LSurface, Text);
		}

		public override bool CanZoomIn()
		{
			return true;
		}
	}

	public class ElementGroup : ArrayList
	{
		private string FName = String.Empty;
		public string Name
		{
			get { return FName; }
			set { FName = (value == null ? String.Empty : value); }
		}
	}

	public class DetailedPlanNodeBox : FloatingBox, IElementDesigner
	{
		public static Color CTabSurfaceColor = Color.FromArgb(200, 230, 230);

		public DetailedPlanNodeBox(XmlElement AElement)
		{
			SuspendLayout();

			TabStop = false;
			SetStyle(ControlStyles.Selectable, false);
			NormalDepth = MaxDepth;
			SurfaceColor = Color.FromArgb(190, 190, 220);

			FElement = AElement;

			FGroups = GetChildGroups();

			FProperties = new Table();
			FProperties.BeginUpdate();
			FProperties.BackColor = SurfaceColor;
			FProperties.ForeColor = ForeColor;
			TableColumn LColumn = new TableColumn(FProperties);
			LColumn.Name = "Attribute";
			LColumn.Title = Strings.Analyzer_AttributeTable_AttributeTitle;
			FProperties.Columns.Add(LColumn);
			LColumn = new TableColumn(FProperties);
			LColumn.Name = "Value";
			LColumn.Title = Strings.Analyzer_AttributeTable_ValueTitle;
			FProperties.Columns.Add(LColumn);
			FProperties.OnGetValue += new GetTableValueHandler(PropertiesGetValue);
			FProperties.OnGetDesignerRequired += new GetTableDesignerRequiredHandler(PropertiesGetDesignerRequired);
			FProperties.OnGetDesigner += new GetTableDesignerHandler(PropertiesGetDesigner);
			Controls.Add(FProperties);
			foreach (XmlAttribute LAttribute in FElement.Attributes)
				FProperties.Rows.Add(LAttribute);
			FProperties.EndUpdate();

			if (FGroups.Count > 0)
			{
				FNotebook = new DAE.Client.Controls.Notebook();
				Controls.Add(FNotebook);
			}
		
			// Place a table for each group
			foreach (DictionaryEntry LEntry in FGroups)
			{
				DAE.Client.Controls.NotebookPage LPage = new DAE.Client.Controls.NotebookPage();
				LPage.BackColor = CTabSurfaceColor;
				LPage.Text = (string)LEntry.Key;
				LPage.Controls.Add(CreateGroupTable((ElementGroup)LEntry.Value));
				LPage.Controls[0].Dock = DockStyle.Fill;
				FNotebook.Pages.Add(LPage);
			}

			ResumeLayout(false);
		}

		private XmlElement FElement;
		public object Element { get { return FElement; } }

		#region Properties

		private Table FProperties;
		public Table Properties { get { return FProperties; } }

		protected override void SurfaceColorChanged()
		{
			base.SurfaceColorChanged();
			if (FProperties != null)
				FProperties.BackColor = SurfaceColor;
		}

		private string PropertiesGetValue(Table ATable, Point ACell)
		{
			XmlAttribute LAttribute = (XmlAttribute)FProperties.Rows[ACell.Y];
			if (ACell.X == 0)
				return LAttribute.LocalName;
			else
				return LAttribute.Value;
		}

		private bool PropertiesGetDesignerRequired(Table ATable, Point ACell, Graphics AGraphics, Rectangle ABounds)
		{
			Size LBounds = Size.Ceiling(AGraphics.MeasureString(PropertiesGetValue(ATable, ACell), ATable.Font));
			return (LBounds.Width > ABounds.Width) || (LBounds.Height > ABounds.Height);
		}

		private IElementDesigner PropertiesGetDesigner(Table ATable, Point ACell)
		{
			CellDesignerBox LBox = new CellDesignerBox();
			LBox.TextHAlign = HorizontalAlignment.Left;
			LBox.Text = PropertiesGetValue(ATable, ACell);
			return LBox;
		}

		#endregion

		#region Child Groups

		private DAE.Client.Controls.Notebook FNotebook;
		
		private IDictionary FGroups;
		public IDictionary Groups { get { return FGroups; } }

		/// <summary> Get the set of child elements grouped by their qualifiers. </summary>
		private IDictionary GetChildGroups()
		{
			HybridDictionary LGroups = new HybridDictionary();
			XmlElement LElement;
			string LQualifierName;
			ElementGroup LGroup;
			foreach (XmlNode LNode in FElement.ChildNodes)
			{
				LElement = LNode as XmlElement;
				if ((LElement != null) && (LElement.LocalName.IndexOf(".") >= 0))
				{
					LQualifierName = LElement.LocalName.Substring(0, LElement.LocalName.IndexOf("."));
					LGroup = LGroups[LQualifierName] as ElementGroup;
					if (LGroup != null)
						LGroup.Add(LElement);
					else
					{
						LGroup = new ElementGroup();
						LGroup.Name = LQualifierName;
						LGroup.Add(LElement);
						LGroups.Add(LQualifierName, LGroup);
					}
				}
			}
			return LGroups;
		}

		private bool HasChildElement(XmlElement AElement)
		{
			foreach (XmlNode LNode in AElement.ChildNodes)
				if (LNode is XmlElement)
					return true;
			return false;
		}

		private Table CreateGroupTable(ElementGroup AGroup)
		{
			Table LGroupTable = new Table();
			LGroupTable.BackColor = CTabSurfaceColor;
			LGroupTable.Tag = AGroup;
			LGroupTable.OnGetValue += new GetTableValueHandler(GroupsGetValue);
			LGroupTable.OnGetDesignerRequired += new GetTableDesignerRequiredHandler(GroupsGetDesignerRequired);
			LGroupTable.OnGetDesigner += new GetTableDesignerHandler(GroupsGetDesigner);
			LGroupTable.BeginUpdate();
			TableColumn LColumn;
			foreach (XmlElement LElement in AGroup)
			{
				// Ensure that there are columns for each attribute
				foreach (XmlAttribute LAttribute in LElement.Attributes)
				{
					LColumn = LGroupTable.Columns[LAttribute.LocalName];
					if (LColumn == null)
					{
						LColumn = new TableColumn(LGroupTable);
						LColumn.Name = LAttribute.LocalName;
						LColumn.Title = LAttribute.LocalName;
						LGroupTable.Columns.Add(LColumn);
					}
				}

				// Ensure that there is a 'Details' column if there are any child elements
				if (HasChildElement(LElement))
				{
					LColumn = LGroupTable.Columns["InternalDetails"];
					if (LColumn == null)
					{
						LColumn = new TableColumn(LGroupTable);
						LColumn.Name = "InternalDetails";
						LColumn.Title = Strings.Analyzer_DetailsColumnTitle;
						LGroupTable.Columns.Add(LColumn);
					}
				}

				// Add a row for each element
				LGroupTable.Rows.Add(LElement);
			}
			LGroupTable.EndUpdate();
			return LGroupTable;
		}
		
		private string GroupsGetValue(Table ATable, Point ACell)
		{
			string LColumnName = ATable.Columns[ACell.X].Name;
			XmlElement LElement = (XmlElement)((ElementGroup)ATable.Tag)[ACell.Y];

			if (LColumnName == "InternalDetails")
				return String.Empty;
			else
			{
				XmlAttribute LAttribute = LElement.Attributes[LColumnName];
				if (LAttribute == null)
					return String.Empty;
				else
					return LAttribute.Value;
			}
		}

		private bool GroupsGetDesignerRequired(Table ATable, Point ACell, Graphics AGraphics, Rectangle ABounds)
		{
			string LColumnName = ATable.Columns[ACell.X].Name;

			if (LColumnName == "InternalDetails")
				return true;
			else
			{
				Size LBounds = Size.Ceiling(AGraphics.MeasureString(GroupsGetValue(ATable, ACell), ATable.Font));
				return (LBounds.Width > ABounds.Width) || (LBounds.Height > ABounds.Height);
			}
		}

		private IElementDesigner GroupsGetDesigner(Table ATable, Point ACell)
		{
			string LColumnName = ATable.Columns[ACell.X].Name;
			if (LColumnName == "InternalDetails")
				return new DetailsCellDesignerBox((XmlElement)((ElementGroup)ATable.Tag)[ACell.Y]);
			else
			{
				CellDesignerBox LBox = new CellDesignerBox();
				LBox.TextHAlign = HorizontalAlignment.Left;
				LBox.Text = GroupsGetValue(ATable, ACell);
				return LBox;
			}
		}

		#endregion

		protected override void OnLayout(System.Windows.Forms.LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);

			Rectangle LBounds = DisplayRectangle;
			LBounds.Inflate(-5, -5);

			// Give a quarter of the area to the properties table
			FProperties.Bounds = new Rectangle(LBounds.X, LBounds.Y, LBounds.Width, LBounds.Height / 4);

			// Distribute the remaining space evenly to the group tables
			if (Controls.Count > 1)
			{
				LBounds.Y += LBounds.Height / 4;
				LBounds.Height -= LBounds.Height / 4;
				int LTableHeight = LBounds.Height / (Controls.Count - 1);
				for (int i = 1; i < Controls.Count; i++)
				{
					Controls[i].Bounds = 
						new Rectangle
						(
							LBounds.X, 
							LBounds.Y + ((i - 1) * LTableHeight),
							LBounds.Width,
							LTableHeight
						);
				}
			}
		}
	}

	public class DetailsCellDesignerBox : TextDesignerBox, IElementDesigner
	{
		public DetailsCellDesignerBox(XmlElement AElement)
		{
			SuspendLayout();
			FElement = AElement;
			Text = Strings.Analyzer_DetailsColumnTitle;
			MaxDepth = 3;
			NormalDepth = 1;
			ForeColor = Color.White;
			SurfaceColor = Color.FromArgb(100, 100, 140);
			HighlightColor = Color.Yellow;
			ResumeLayout(false);
		}

		private XmlElement FElement;
		public object Element { get { return FElement; } }

		private bool HasUnqualifiedChildElement(XmlElement AElement)
		{
			XmlElement LChild;
			foreach (XmlNode LNode in AElement.ChildNodes)
			{
				LChild = LNode as XmlElement;
				if ((LChild != null) && (LChild.LocalName.IndexOf(".") < 0))
					return true;
			}
			return false;
		}

		public override void ZoomIn()
		{
			if (HasUnqualifiedChildElement(FElement))
			{
				PlanTree LSurface = new PlanTree();
				LSurface.Set(FElement);
				DesignerControl.GetDesigner(this).Push(LSurface, String.Format(Strings.Analyzer_PlanNodeTitle, FElement.LocalName));
			}
			else
				DesignerControl.GetDesigner(this).Push(new SingleElementSurface(FElement, new DetailedPlanNodeBox(FElement)), String.Format(Strings.Analyzer_DetailNodeTitle, FElement.LocalName));
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
			CellDetailBox LBox = new CellDetailBox(Text);
			SingleElementSurface LSurface = new SingleElementSurface(null, LBox);
			DesignerControl LControl = DesignerControl.GetDesigner(this);
			LControl.Push(LSurface, Strings.Analyzer_TextDetailTitle);
		}

		public override bool CanZoomIn()
		{
			return true;
		}
	}

	public class CellDetailBox : FloatingBox, IElementDesigner
	{
		public CellDetailBox(string AText)
		{
			SuspendLayout();

			NormalDepth = MaxDepth;
			
			FTextEdit = new Alphora.Dataphor.Dataphoria.TextEditor.ResultPanel();
			FTextEdit.SetText(AText);
			Controls.Add(FTextEdit);

			ResumeLayout(false);
		}

		private TextEdit FTextEdit;
		public TextEdit TextEdit
		{
			get { return FTextEdit; }
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);
			FTextEdit.Bounds = DisplayRectangle;
		}

		public object Element
		{
			get { return FTextEdit.Document.TextContent; }
		}
	}
}
