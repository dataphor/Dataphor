/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Web;
using System.Web.UI;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public abstract class Element : Node, IWebElement, IWebHandler
	{
		public const int CDefaultMarginLeft = 2;
		public const int CDefaultMarginRight = 2;
		public const int CDefaultMarginTop = 2;
		public const int CDefaultMarginBottom = 2;

		public Element()
		{
			FMarginLeft = GetDefaultMarginLeft();
			FMarginRight = GetDefaultMarginRight();
			FMarginTop = GetDefaultMarginTop();
			FMarginBottom = GetDefaultMarginBottom();
			FTabStop = GetDefaultTabStop();
			FHelpID = Session.GenerateID();
		}

		// TODO: Implement styles in web client
		[DefaultValue("")]
		public string Style { get; set; }
		
		// MarginLeft
		
		private int FMarginLeft;
		[DefaultValueMember("GetDefaultMarginLeft")]
		public int MarginLeft
		{
			get { return FMarginLeft; }
			set { FMarginLeft = value; }
		}

		public virtual int GetDefaultMarginLeft()
		{
			return CDefaultMarginLeft;
		}
		
		// MarginRight
		
		private int FMarginRight;
		[DefaultValueMember("GetDefaultMarginRight")]
		public int MarginRight
		{
			get { return FMarginRight; }
			set { FMarginRight = value; }
		}

		public virtual int GetDefaultMarginRight()
		{
			return CDefaultMarginRight;
		}
		
		// MarginTop
		
		private int FMarginTop;
		[DefaultValueMember("GetDefaultMarginTop")]
		public int MarginTop
		{
			get { return FMarginTop; }
			set { FMarginTop = value; }
		}

		public virtual int GetDefaultMarginTop()
		{
			return CDefaultMarginTop;
		}
		
		// MarginBottom
		
		private int FMarginBottom;
		[DefaultValueMember("GetDefaultMarginBottom")]
		public int MarginBottom
		{
			get { return FMarginBottom; }
			set { FMarginBottom = value; }
		}

		public virtual int GetDefaultMarginBottom()
		{
			return CDefaultMarginBottom;
		}
		
		// Hint

		private string FHint = String.Empty;
		[DefaultValue("")]
		public string Hint { get { return FHint; } set { FHint = value; } }

		/// <summary> Gets the actual hint to use for this node. </summary>
		public virtual string GetHint()
		{
			return FHint;
		}
		
		// Help

		// TODO: Support for context help in the web

		private string FHelpKeyword = String.Empty;
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return FHelpKeyword; }
			set { FHelpKeyword = (value == null ? String.Empty : value); }
		}
		
		private HelpKeywordBehavior FHelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return FHelpKeywordBehavior; }
			set { FHelpKeywordBehavior = value; }
		}

		private string FHelpString = String.Empty;
		[DefaultValue("")]
		public string HelpString
		{ 
			get { return FHelpString; }
			set { FHelpString = (value == null ? String.Empty : value); }
		}

		private string FHelpID;
		private bool FRenderHelp = false;

		public virtual void RenderHelp(HtmlTextWriter AWriter)
		{
			if (FRenderHelp)
			{
				AWriter.Write("<br>");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "elementhelp");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Font);
				AWriter.Write(HttpUtility.HtmlEncode(FHelpString));
				AWriter.RenderEndTag();
				FRenderHelp = false;
			}
			else
			{
				if (FHelpString != String.Empty)
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "image");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Name, FHelpID);
					AWriter.AddAttribute(HtmlTextWriterAttribute.Src, @"images\help.gif");
					AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
					AWriter.RenderEndTag();
				}
			}
		}

		// TabStop

		// TODO: TabStop support in the web (there is support for TabIndex, but there doesn't seem to be support for Tab Stop toggle)

		private bool FTabStop;
		[DefaultValueMember("GetDefaultTabStop")]
		public bool TabStop
		{
			get { return FTabStop; }
			set { FTabStop = value; }
		}

		protected virtual bool GetDefaultTabStop()
		{
			return true;
		}

		public virtual bool GetTabStop()
		{
			return FTabStop;
		}

		// Visible

		private bool FVisible = true;
		[DefaultValue(true)]
		public bool Visible { get { return FVisible; } set { FVisible = value; } }

		public virtual bool GetVisible()
		{
			return FVisible;
		}

		// ErrorMessage

		private string FErrorMessage;
		/// <summary> Constraint violation or other field level errors to be shown at next field rendering. </summary>
		public string ErrorMessage { get { return FErrorMessage; } set { FErrorMessage = value; } }

		protected virtual void RenderErrorMessage(HtmlTextWriter AWriter)
		{
			if (FErrorMessage != null)
			{
				AWriter.Write("<br>");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "elementerror");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Font);
				AWriter.Write(HttpUtility.HtmlEncode(FErrorMessage));
				AWriter.RenderEndTag();
				FErrorMessage = null;
			}
		}

		// Element exceptions

		public virtual void HandleElementException(Exception AException)
		{
			if (FErrorMessage == null)
				FErrorMessage = AException.Message;
			else
				FErrorMessage = FErrorMessage + "\r\n" + AException.Message;
		}

		// IWebElement

		public virtual void Render(HtmlTextWriter AWriter)
		{
			if (GetVisible())
			{
				InternalRender(AWriter);
				RenderHelp(AWriter);
				RenderErrorMessage(AWriter);
			}
		}

		public virtual bool ProcessRequest(HttpContext AContext)
		{
			if (AContext.Request.Form[FHelpID + ".x"] != null)
			{
				FRenderHelp = true;
				return true;
			}
			else
				return false;
		}

		protected abstract void InternalRender(HtmlTextWriter AWriter);
	}
}
