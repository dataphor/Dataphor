/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Frontend.Server.Structuring
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.Frontend;
	using Alphora.Dataphor.Frontend.Server;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public abstract class Element : System.Object
	{
		public Element(string name) : base()
		{
			if ((name == null) || (name == String.Empty))
				throw new ServerException(ServerException.Codes.ElementNameRequired);
				
			_name = name;
		}
		
		private string _name;
		public string Name { get { return _name; } }

		public string Title;
		public string Hint;
		public string ElementType;
		public Tags Properties = new Tags();
		public int Priority;
		public Flow Flow;
		public bool FlowBreak;
		public bool Break;
		
		public virtual Element FindElement(string name)
		{
			if (Name == name)
				return this;
			else
				return null;
		}
		
		public virtual bool ContainsMultipleElements()
		{
			return false;
		}
	}
	
	public class ColumnElement : Element
	{
		public ColumnElement(string name) : base(name) {}
		
		public string Source;
		public string ColumnName;
	}
	
	public abstract class ContainerElement : Element
	{
		public ContainerElement(string name) : base(name) {}
		
		private Elements _elements = new Elements();
		public Elements Elements { get { return _elements; } }
		
		public override Element FindElement(string name)
		{
			Element element = base.FindElement(name);
			if (element == null)
				element = _elements.FindElement(name);
			return element;
		}
		
		public override bool ContainsMultipleElements()
		{
			return Elements.Count > 1;
		}
	}
	
	public class GridElement : ContainerElement
	{
		public GridElement(string name) : base(name) {}
		
		public string Source;
		
		public override bool ContainsMultipleElements()
		{
			return true;
		}
	}
	
	public class GridColumnElement : Element
	{
		public GridColumnElement(string name) : base(name) {}
		
		public string ColumnName;
		public int Width;
	}
	
	public class SearchElement : ContainerElement
	{
		public SearchElement(string name) : base(name) {}
		
		public string Source;
		
		public override bool ContainsMultipleElements()
		{
			return true;
		}
	}
	
	public class SearchColumnElement : Element
	{
		public SearchColumnElement(string name) : base(name) {}
		
		public string ColumnName;
		public int Width;
	}
	
	public class GroupElement : ContainerElement
	{
		public GroupElement(string name) : base(name) {}
		
		/// <summary>Determines whether a group will be eliminated if it contains only one element.</summary>
		public bool EliminateGroup = true;

		public override bool ContainsMultipleElements()
		{
			if ((String.Compare(ElementType, "group", true) != 0) || !EliminateGroup)
				return true;
			
			return base.ContainsMultipleElements();
		}
	}
	
	public class LookupColumnElement : GroupElement
	{
		public LookupColumnElement(string name) : base(name) {}
		
		public string Source;		
		public string ColumnName;
		public string LookupColumnName;
		public string LookupDocument;
		public string MasterKeyNames;
		public string DetailKeyNames;
		
		public override bool ContainsMultipleElements()
		{
			return true;
		}
	}

	public class LookupGroupElement : GroupElement
	{
		public LookupGroupElement(string name) : base(name) 
		{
			EliminateGroup = false;
		}

		public string Source;		
		public string ColumnNames;
		public string LookupColumnNames;
		public string LookupDocument;
		public string MasterKeyNames;
		public string DetailKeyNames;
		
		public override bool ContainsMultipleElements()
		{
			return true;
		}
	}
	
	public class Elements : System.Object
	{
		private const int DefaultCapacity = 20;
		private const int DefaultGrowth = 20;
	
		public Elements() : base()
		{
			_elements = new Element[DefaultCapacity];
		}

		private int _count;		
		public int Count { get { return _count; } }

		private Element[] _elements;
		public Element this[int index] 
		{ 
			get 
			{
				if ((index < 0) || index >= _count)
					throw new IndexOutOfRangeException();
				return _elements[index]; 
			} 
		}
		
		public void Add(Element element)
		{
			if (element == null)
				throw new ServerException(ServerException.Codes.ElementRequired);
				
			if (FindElement(element.Name) != null)
				throw new ServerException(ServerException.Codes.DuplicateElementName, element.Name);
				
			EnsureSize(_count + 1);
			bool inserted = false;
			for (int index = 0; index < _count; index++)
				if (_elements[index].Priority > element.Priority)
				{
					InsertAt(element, index);
					inserted = true;
					break;
				}
			if (!inserted)
				InsertAt(element, _count);
			_count++;
		}
		
		public void Remove(Element element)
		{
			RemoveAt(IndexOf(element));
		}
		
		public int IndexOf(Element element)
		{
			for (int index = 0; index < _count; index++)
				if (Object.ReferenceEquals(_elements[index], element))
					return index;
			return -1;
		}
		
		public bool Contains(Element element)
		{
			return IndexOf(element) >= 0;
		}
		
		public Element FindElement(string name)
		{
			Element element;
			for (int index = 0; index < _count; index++)
			{
				element = _elements[index].FindElement(name);
				if (element != null)
					return element;
			}
			return null;
		}
		
		private void InsertAt(Element element, int index)
		{
			for (int localIndex = _count - 1; localIndex >= index; localIndex--)
				_elements[localIndex + 1] = _elements[localIndex];
			_elements[index] = element;
		}
		
		private void RemoveAt(int index)
		{
			for (int localIndex = index; localIndex < _count - 1; localIndex++)
				_elements[localIndex] = _elements[localIndex + 1];
			_elements[_count - 1] = null;
		}
		
		private void EnsureSize(int count)
		{
			if (_elements.Length < count)
			{
				Element[] newElements = new Element[_elements.Length + DefaultGrowth];
				for (int index = 0; index < _count; index++)
					newElements[index] = _elements[index];
				_elements = newElements;
			}
		}
	}
}