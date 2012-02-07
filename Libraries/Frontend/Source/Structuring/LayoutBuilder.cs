/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Frontend.Server.Structuring
{
	public class LayoutBuilder : System.Object
	{
		/// <summary> Puts the specified node into a properly flowing container (if it isn't already). </summary>
		protected virtual LayoutNode PrepareLayout(LayoutNode currentNode, Flow flow)
		{
			// Ensures that the node is ready to accept a new node, and returns the parent into which the new node should be inserted
			LayoutNode parent = currentNode.Parent;
			switch (flow)
			{
				case Flow.Horizontal :
					if (!(parent is RowNode))
					{
						RowNode rowNode = new RowNode();
						rowNode.Children.Add(currentNode);
						if (parent != null)
							parent.Children.Add(rowNode);
						return rowNode;
					}
					return parent;
				
				default :
					if (!(parent is ColumnNode))
					{
						ColumnNode columnNode = new ColumnNode();
						columnNode.Children.Add(currentNode);
						if (parent != null)
							parent.Children.Add(columnNode);
						return columnNode;
					}
					return parent;
			}
		}
		
		protected Flow ReverseFlow(Flow flow)
		{
			return ((flow == Flow.Horizontal) ? Flow.Vertical : Flow.Horizontal);
		}
		
		protected virtual LayoutNode LayoutElement(LayoutNode currentNode, Element element)
		{
			// Eliminate empty groups, and groups with only one element
			GroupElement groupElement = element as GroupElement;
			if (groupElement != null)
			{
				if (groupElement.Elements.Count == 0)
					return currentNode;
					
				if (!groupElement.ContainsMultipleElements())
					return LayoutElement(currentNode, groupElement.Elements[0]);
			}
			
			ElementNode elementNode = new ElementNode(element);

			if ((element is GroupElement) && (((GroupElement)element).Elements.Count > 0))
			{
				LayoutNode newNode = new LayoutBuilder().Layout(((GroupElement)element).Elements);
				if (newNode != null)
					elementNode.Children.Add(newNode);
			}
			
			if (currentNode != null)
			{
				LayoutNode parent;
				
				if (_flowBreak || _return)
					parent = PrepareLayout(currentNode, ReverseFlow(_flow));
				else
					parent = PrepareLayout(currentNode, _flow);
				
				if (_break)
					PrepareLayout(parent, ReverseFlow(_flow)).Children.Add(PrepareLayout(elementNode, _flow));
				else if (_return)
					PrepareLayout(parent, _flow).Children.Add(elementNode);
				else
					parent.Children.Add(elementNode);
			}
			
			switch (element.Flow)
			{
				case Flow.Horizontal:
				case Flow.Vertical : _flow = element.Flow; break;
			}
			
			_return = _flowBreak && !element.FlowBreak;
			_break = element.Break;
			_flowBreak = element.FlowBreak;
			
			return elementNode;
		}
		
		public virtual LayoutNode Layout(Elements elements)
		{
			_flow = Flow.Vertical;
			_flowBreak = false;
			_break = false;
			_return = false;
			LayoutNode currentNode = null;
			
			for (int index = 0; index < elements.Count; index++)
				currentNode = LayoutElement(currentNode, elements[index]);
				
			return currentNode != null ? currentNode.Root : null;
		}
		
		protected Flow _flow;
		protected bool _flowBreak;
		protected bool _break;
		protected bool _return;
	}
}

