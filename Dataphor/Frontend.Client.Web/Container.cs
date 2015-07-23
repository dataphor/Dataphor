/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Web.UI;
using System.Drawing;
using System.Collections;
using System.ComponentModel;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public abstract class ContainerElement : Element
	{
		// Element

		public override int GetDefaultMarginLeft()
		{
			return 0;
		}

		public override int GetDefaultMarginRight()
		{
			return 0;
		}

		public override int GetDefaultMarginTop()
		{
			return 0;
		}

		public override int GetDefaultMarginBottom()
		{
			return 0;
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			return typeof(IWebElement).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}

		protected bool HasVisibleChild()
		{
			foreach (IElement child in Children)
				if (child.GetVisible())
					return true;
			return false;
		}
	}

	public class Column : ContainerElement, IColumn
    {
		// IColumn

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set	{ _verticalAlignment = value; }
		}
		
		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			if (HasVisibleChild())
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				string hint = GetHint();
				if (hint != String.Empty)
					writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
				switch (_verticalAlignment)
				{
					case VerticalAlignment.Middle : writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle"); break;
					case VerticalAlignment.Bottom : writer.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom"); break;
				}
				writer.RenderBeginTag(HtmlTextWriterTag.Table);

				foreach (IWebElement element in Children)
				{
					if (element.GetVisible())
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
						writer.RenderBeginTag(HtmlTextWriterTag.Tr);
						writer.RenderBeginTag(HtmlTextWriterTag.Td);
						element.Render(writer);
						writer.RenderEndTag();
						writer.RenderEndTag();
					}
				}

				writer.RenderEndTag();
			}
		}
    }
    
    public class Row : ContainerElement, IRow
    {		
		// IRow

		protected HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set { _horizontalAlignment = value; }
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			if (HasVisibleChild())
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
				string hint = GetHint();
				if (hint != String.Empty)
					writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
				switch (_horizontalAlignment)
				{
					case HorizontalAlignment.Center : writer.AddAttribute(HtmlTextWriterAttribute.Align, "center"); break;
					case HorizontalAlignment.Right : writer.AddAttribute(HtmlTextWriterAttribute.Align, "right"); break;
				}
				writer.RenderBeginTag(HtmlTextWriterTag.Table);
				writer.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
				writer.RenderBeginTag(HtmlTextWriterTag.Tr);

				foreach (IWebElement element in Children)
				{
					if (element.GetVisible())
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
						writer.RenderBeginTag(HtmlTextWriterTag.Td);
						element.Render(writer);
						writer.RenderEndTag();
					}
				}

				writer.RenderEndTag();
				writer.RenderEndTag();
			}
		}
    }
    
	public abstract class SingleElementContainer : ContainerElement
	{
		private IWebElement _child;
		protected IWebElement Child { get { return _child; } }

		// Element

		protected override void InternalRender(HtmlTextWriter writer)
		{
			if ((_child != null) && _child.GetVisible())
				_child.Render(writer);
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			return 
				(typeof(IWebElement).IsAssignableFrom(childType) && (_child == null)) ||
				base.IsValidChild(childType);
		}

		protected override void InvalidChildError(INode child) 
		{
			throw new ClientException(ClientException.Codes.UseSingleElementNode);
		}
		
		protected override void AddChild(INode child)
		{
			base.AddChild(child);
			if (child is IWebElement)
				_child = (IWebElement)child;
		}

		protected override void RemoveChild(INode child)
		{
			if (child == _child)
				_child = null;
			base.RemoveChild(child);
		}
	}

	public class Group : SingleElementContainer, IGroup
    {
		// Title

		protected string _title = String.Empty;
		public string Title
		{
			get { return _title; }
			set { _title = value; }
		}

		public virtual string GetTitle()
		{
			return Session.RemoveAccellerator(_title);
		}

		// IWebElement
		
		protected override void InternalRender(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "group");
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "7");
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
			string temp = GetHint();
			if (temp != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, temp, true);
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			temp = GetTitle();
			if (temp != String.Empty)
			{
				writer.RenderBeginTag(HtmlTextWriterTag.Caption);
				writer.Write(HttpUtility.HtmlEncode(temp));
				writer.RenderEndTag();
			}

			writer.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
			writer.RenderBeginTag(HtmlTextWriterTag.Tr);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			base.InternalRender(writer);

			writer.RenderEndTag();	// TD
			writer.RenderEndTag();	// TR
			writer.RenderEndTag();	// TABLE
		}
    }
}
