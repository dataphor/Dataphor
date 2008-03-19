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

		public override bool IsValidChild(Type AChildType)
		{
			return typeof(IWebElement).IsAssignableFrom(AChildType) || base.IsValidChild(AChildType);
		}

		protected bool HasVisibleChild()
		{
			foreach (IElement LChild in Children)
				if (LChild.GetVisible())
					return true;
			return false;
		}
	}

	public class Column : ContainerElement, IColumn
    {
		// IColumn

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set	{ FVerticalAlignment = value; }
		}
		
		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			if (HasVisibleChild())
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				string LHint = GetHint();
				if (LHint != String.Empty)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
				switch (FVerticalAlignment)
				{
					case VerticalAlignment.Middle : AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "middle"); break;
					case VerticalAlignment.Bottom : AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom"); break;
				}
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

				foreach (IWebElement LElement in Children)
				{
					if (LElement.GetVisible())
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
						LElement.Render(AWriter);
						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
					}
				}

				AWriter.RenderEndTag();
			}
		}
    }
    
    public class Row : ContainerElement, IRow
    {		
		// IRow

		protected HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set { FHorizontalAlignment = value; }
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			if (HasVisibleChild())
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
				string LHint = GetHint();
				if (LHint != String.Empty)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
				switch (FHorizontalAlignment)
				{
					case HorizontalAlignment.Center : AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "center"); break;
					case HorizontalAlignment.Right : AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "right"); break;
				}
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);
				AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

				foreach (IWebElement LElement in Children)
				{
					if (LElement.GetVisible())
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
						LElement.Render(AWriter);
						AWriter.RenderEndTag();
					}
				}

				AWriter.RenderEndTag();
				AWriter.RenderEndTag();
			}
		}
    }
    
	public abstract class SingleElementContainer : ContainerElement
	{
		private IWebElement FChild;
		protected IWebElement Child { get { return FChild; } }

		// Element

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			if ((FChild != null) && FChild.GetVisible())
				FChild.Render(AWriter);
		}

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			return 
				(typeof(IWebElement).IsAssignableFrom(AChildType) && (FChild == null)) ||
				base.IsValidChild(AChildType);
		}

		protected override void InvalidChildError(INode AChild) 
		{
			throw new ClientException(ClientException.Codes.UseSingleElementNode);
		}
		
		protected override void AddChild(INode AChild)
		{
			base.AddChild(AChild);
			if (AChild is IWebElement)
				FChild = (IWebElement)AChild;
		}

		protected override void RemoveChild(INode AChild)
		{
			if (AChild == FChild)
				FChild = null;
			base.RemoveChild(AChild);
		}
	}

	public class Group : SingleElementContainer, IGroup
    {
		// Title

		protected string FTitle = String.Empty;
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value; }
		}

		public virtual string GetTitle()
		{
			return Session.RemoveAccellerator(FTitle);
		}

		// IWebElement
		
		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "group");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "1");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "7");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
			string LTemp = GetHint();
			if (LTemp != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LTemp, true);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			LTemp = GetTitle();
			if (LTemp != String.Empty)
			{
				AWriter.RenderBeginTag(HtmlTextWriterTag.Caption);
				AWriter.Write(HttpUtility.HtmlEncode(LTemp));
				AWriter.RenderEndTag();
			}

			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			base.InternalRender(AWriter);

			AWriter.RenderEndTag();	// TD
			AWriter.RenderEndTag();	// TR
			AWriter.RenderEndTag();	// TABLE
		}
    }
}
