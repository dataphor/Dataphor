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
			_children = new LayoutNodes(this);
		}

		internal LayoutNode _parent;
		public LayoutNode Parent { get { return _parent; } }
		public LayoutNode Root { get { return _parent == null ? this : _parent.Root; } }
		
		private LayoutNodes _children;
		public LayoutNodes Children { get { return _children; } }
	}
	
	#if USETYPEDLIST
	public class LayoutNodes : TypedList
	{
		public LayoutNodes(LayoutNode AContainer) : base(typeof(LayoutNode))
		{
	#else
	public class LayoutNodes : ValidatingBaseList<LayoutNode>
	{
		public LayoutNodes(LayoutNode container) : base()
		{
	#endif
			_container = container;
		}
		
		private LayoutNode _container;
		
		public new LayoutNode this[int index]
		{
			get { return (LayoutNode)base[index]; }
			set { base[index] = value; }
		}
		
		#if USETYPEDLIST
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
		#else
		protected override void Adding(LayoutNode tempValue, int index)
		{
			//base.Adding(AValue, AIndex);
			if (tempValue.Parent != null)
				tempValue.Parent.Children.Remove(tempValue);
			tempValue._parent = _container;
		}
		
		protected override void Removing(LayoutNode tempValue, int index)
		{
			tempValue._parent = null;
			//base.Removing(AItem, AIndex);
		}
		#endif
	}
	
	public class RowNode : LayoutNode {}
	
	public class ColumnNode : LayoutNode {}
	
	public class ElementNode : LayoutNode 
	{
		public ElementNode(Element element) : base()
		{
			_element = element;
		}

		private Element _element;
		public Element Element { get { return _element; } }
	}
}

