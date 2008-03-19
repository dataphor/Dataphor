/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Frontend.Server.Structuring
{
	using System;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.Frontend.Server;
	
	public abstract class LayoutNode : System.Object
	{
		public LayoutNode() : base()
		{
			FChildren = new LayoutNodes(this);
		}

		internal LayoutNode FParent;
		public LayoutNode Parent { get { return FParent; } }
		public LayoutNode Root { get { return FParent == null ? this : FParent.Root; } }
		
		private LayoutNodes FChildren;
		public LayoutNodes Children { get { return FChildren; } }
	}
	
	public class LayoutNodes : TypedList
	{
		public LayoutNodes(LayoutNode AContainer) : base(typeof(LayoutNode))
		{
			FContainer = AContainer;
		}
		
		private LayoutNode FContainer;
		
		public new LayoutNode this[int AIndex]
		{
			get { return (LayoutNode)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Adding(object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			LayoutNode LItem = (LayoutNode)AItem;
			if (LItem.Parent != null)
				LItem.Parent.Children.Remove(AItem);
			LItem.FParent = FContainer;
		}
		
		protected override void Removing(object AItem, int AIndex)
		{
			((LayoutNode)AItem).FParent = null;
			base.Removing(AItem, AIndex);
		}
	}
	
	public class RowNode : LayoutNode {}
	
	public class ColumnNode : LayoutNode {}
	
	public class ElementNode : LayoutNode 
	{
		public ElementNode(Element AElement) : base()
		{
			FElement = AElement;
		}

		private Element FElement;
		public Element Element { get { return FElement; } }
	}
}

