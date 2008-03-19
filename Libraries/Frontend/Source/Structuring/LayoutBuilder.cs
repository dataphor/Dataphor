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
		protected virtual LayoutNode PrepareLayout(LayoutNode ACurrentNode, Flow AFlow)
		{
			// Ensures that the node is ready to accept a new node, and returns the parent into which the new node should be inserted
			LayoutNode LParent = ACurrentNode.Parent;
			switch (AFlow)
			{
				case Flow.Horizontal :
					if (!(LParent is RowNode))
					{
						RowNode LRowNode = new RowNode();
						LRowNode.Children.Add(ACurrentNode);
						if (LParent != null)
							LParent.Children.Add(LRowNode);
						return LRowNode;
					}
					return LParent;
				
				default :
					if (!(LParent is ColumnNode))
					{
						ColumnNode LColumnNode = new ColumnNode();
						LColumnNode.Children.Add(ACurrentNode);
						if (LParent != null)
							LParent.Children.Add(LColumnNode);
						return LColumnNode;
					}
					return LParent;
			}
		}
		
		protected Flow ReverseFlow(Flow AFlow)
		{
			return ((AFlow == Flow.Horizontal) ? Flow.Vertical : Flow.Horizontal);
		}
		
		protected virtual LayoutNode LayoutElement(LayoutNode ACurrentNode, Element AElement)
		{
			// Eliminate empty groups, and groups with only one element
			GroupElement LGroupElement = AElement as GroupElement;
			if (LGroupElement != null)
			{
				if (LGroupElement.Elements.Count == 0)
					return ACurrentNode;
					
				if (!LGroupElement.ContainsMultipleElements())
					return LayoutElement(ACurrentNode, LGroupElement.Elements[0]);
			}
			
			ElementNode LElementNode = new ElementNode(AElement);

			if ((AElement is GroupElement) && (((GroupElement)AElement).Elements.Count > 0))
			{
				LayoutNode LNewNode = new LayoutBuilder().Layout(((GroupElement)AElement).Elements);
				if (LNewNode != null)
					LElementNode.Children.Add(LNewNode);
			}
			
			if (ACurrentNode != null)
			{
				LayoutNode LParent;
				
				if (FFlowBreak || FReturn)
					LParent = PrepareLayout(ACurrentNode, ReverseFlow(FFlow));
				else
					LParent = PrepareLayout(ACurrentNode, FFlow);
				
				if (FBreak)
					PrepareLayout(LParent, ReverseFlow(FFlow)).Children.Add(PrepareLayout(LElementNode, FFlow));
				else if (FReturn)
					PrepareLayout(LParent, FFlow).Children.Add(LElementNode);
				else
					LParent.Children.Add(LElementNode);
			}
			
			switch (AElement.Flow)
			{
				case Flow.Horizontal:
				case Flow.Vertical : FFlow = AElement.Flow; break;
			}
			
			FReturn = FFlowBreak && !AElement.FlowBreak;
			FBreak = AElement.Break;
			FFlowBreak = AElement.FlowBreak;
			
			return LElementNode;
		}
		
		public virtual LayoutNode Layout(Elements AElements)
		{
			FFlow = Flow.Vertical;
			FFlowBreak = false;
			FBreak = false;
			FReturn = false;
			LayoutNode LCurrentNode = null;
			
			for (int LIndex = 0; LIndex < AElements.Count; LIndex++)
				LCurrentNode = LayoutElement(LCurrentNode, AElements[LIndex]);
				
			return LCurrentNode != null ? LCurrentNode.Root : null;
		}
		
		protected Flow FFlow;
		protected bool FFlowBreak;
		protected bool FBreak;
		protected bool FReturn;
	}
}

