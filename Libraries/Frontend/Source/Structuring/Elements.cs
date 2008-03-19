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
		public Element(string AName) : base()
		{
			if ((AName == null) || (AName == String.Empty))
				throw new ServerException(ServerException.Codes.ElementNameRequired);
				
			FName = AName;
		}
		
		private string FName;
		public string Name { get { return FName; } }

		public string Title;
		public string Hint;
		public string ElementType;
		public Tags Properties = new Tags();
		public int Priority;
		public Flow Flow;
		public bool FlowBreak;
		public bool Break;
		
		public virtual Element FindElement(string AName)
		{
			if (Name == AName)
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
		public ColumnElement(string AName) : base(AName) {}
		
		public string Source;
		public string ColumnName;
	}
	
	public abstract class ContainerElement : Element
	{
		public ContainerElement(string AName) : base(AName) {}
		
		private Elements FElements = new Elements();
		public Elements Elements { get { return FElements; } }
		
		public override Element FindElement(string AName)
		{
			Element LElement = base.FindElement(AName);
			if (LElement == null)
				LElement = FElements.FindElement(AName);
			return LElement;
		}
		
		public override bool ContainsMultipleElements()
		{
			return Elements.Count > 1;
		}
	}
	
	public class GridElement : ContainerElement
	{
		public GridElement(string AName) : base(AName) {}
		
		public string Source;
		
		public override bool ContainsMultipleElements()
		{
			return true;
		}
	}
	
	public class GridColumnElement : Element
	{
		public GridColumnElement(string AName) : base(AName) {}
		
		public string ColumnName;
		public int Width;
	}
	
	public class SearchElement : ContainerElement
	{
		public SearchElement(string AName) : base(AName) {}
		
		public string Source;
		
		public override bool ContainsMultipleElements()
		{
			return true;
		}
	}
	
	public class SearchColumnElement : Element
	{
		public SearchColumnElement(string AName) : base(AName) {}
		
		public string ColumnName;
		public int Width;
	}
	
	public class GroupElement : ContainerElement
	{
		public GroupElement(string AName) : base(AName) {}
		
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
		public LookupColumnElement(string AName) : base(AName) {}
		
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
		public LookupGroupElement(string AName) : base(AName) 
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
		private const int CDefaultCapacity = 20;
		private const int CDefaultGrowth = 20;
	
		public Elements() : base()
		{
			FElements = new Element[CDefaultCapacity];
		}

		private int FCount;		
		public int Count { get { return FCount; } }

		private Element[] FElements;
		public Element this[int AIndex] 
		{ 
			get 
			{
				if ((AIndex < 0) || AIndex >= FCount)
					throw new IndexOutOfRangeException();
				return FElements[AIndex]; 
			} 
		}
		
		public void Add(Element AElement)
		{
			if (AElement == null)
				throw new ServerException(ServerException.Codes.ElementRequired);
				
			if (FindElement(AElement.Name) != null)
				throw new ServerException(ServerException.Codes.DuplicateElementName, AElement.Name);
				
			EnsureSize(FCount + 1);
			bool LInserted = false;
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (FElements[LIndex].Priority > AElement.Priority)
				{
					InsertAt(AElement, LIndex);
					LInserted = true;
					break;
				}
			if (!LInserted)
				InsertAt(AElement, FCount);
			FCount++;
		}
		
		public void Remove(Element AElement)
		{
			RemoveAt(IndexOf(AElement));
		}
		
		public int IndexOf(Element AElement)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (Object.ReferenceEquals(FElements[LIndex], AElement))
					return LIndex;
			return -1;
		}
		
		public bool Contains(Element AElement)
		{
			return IndexOf(AElement) >= 0;
		}
		
		public Element FindElement(string AName)
		{
			Element LElement;
			for (int LIndex = 0; LIndex < FCount; LIndex++)
			{
				LElement = FElements[LIndex].FindElement(AName);
				if (LElement != null)
					return LElement;
			}
			return null;
		}
		
		private void InsertAt(Element AElement, int AIndex)
		{
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FElements[LIndex + 1] = FElements[LIndex];
			FElements[AIndex] = AElement;
		}
		
		private void RemoveAt(int AIndex)
		{
			for (int LIndex = AIndex; LIndex < FCount - 1; LIndex++)
				FElements[LIndex] = FElements[LIndex + 1];
			FElements[FCount - 1] = null;
		}
		
		private void EnsureSize(int ACount)
		{
			if (FElements.Length < ACount)
			{
				Element[] LNewElements = new Element[FElements.Length + CDefaultGrowth];
				for (int LIndex = 0; LIndex < FCount; LIndex++)
					LNewElements[LIndex] = FElements[LIndex];
				FElements = LNewElements;
			}
		}
	}
}