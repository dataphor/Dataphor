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
		public const int DefaultMarginLeft = 2;
		public const int DefaultMarginRight = 2;
		public const int DefaultMarginTop = 2;
		public const int DefaultMarginBottom = 2;

		public Element()
		{
			_marginLeft = GetDefaultMarginLeft();
			_marginRight = GetDefaultMarginRight();
			_marginTop = GetDefaultMarginTop();
			_marginBottom = GetDefaultMarginBottom();
			_tabStop = GetDefaultTabStop();
			_helpID = Session.GenerateID();
		}

		// TODO: Implement styles in web client
		[DefaultValue("")]
		public string Style { get; set; }
		
		// MarginLeft
		
		private int _marginLeft;
		[DefaultValueMember("GetDefaultMarginLeft")]
		public int MarginLeft
		{
			get { return _marginLeft; }
			set { _marginLeft = value; }
		}

		public virtual int GetDefaultMarginLeft()
		{
			return DefaultMarginLeft;
		}
		
		// MarginRight
		
		private int _marginRight;
		[DefaultValueMember("GetDefaultMarginRight")]
		public int MarginRight
		{
			get { return _marginRight; }
			set { _marginRight = value; }
		}

		public virtual int GetDefaultMarginRight()
		{
			return DefaultMarginRight;
		}
		
		// MarginTop
		
		private int _marginTop;
		[DefaultValueMember("GetDefaultMarginTop")]
		public int MarginTop
		{
			get { return _marginTop; }
			set { _marginTop = value; }
		}

		public virtual int GetDefaultMarginTop()
		{
			return DefaultMarginTop;
		}
		
		// MarginBottom
		
		private int _marginBottom;
		[DefaultValueMember("GetDefaultMarginBottom")]
		public int MarginBottom
		{
			get { return _marginBottom; }
			set { _marginBottom = value; }
		}

		public virtual int GetDefaultMarginBottom()
		{
			return DefaultMarginBottom;
		}
		
		// Hint

		private string _hint = String.Empty;
		[DefaultValue("")]
		public string Hint { get { return _hint; } set { _hint = value; } }

		/// <summary> Gets the actual hint to use for this node. </summary>
		public virtual string GetHint()
		{
			return _hint;
		}
		
		// Help

		// TODO: Support for context help in the web

		private string _helpKeyword = String.Empty;
		[DefaultValue("")]
		public string HelpKeyword 
		{ 
			get { return _helpKeyword; }
			set { _helpKeyword = (value == null ? String.Empty : value); }
		}
		
		private HelpKeywordBehavior _helpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return _helpKeywordBehavior; }
			set { _helpKeywordBehavior = value; }
		}

		private string _helpString = String.Empty;
		[DefaultValue("")]
		public string HelpString
		{ 
			get { return _helpString; }
			set { _helpString = (value == null ? String.Empty : value); }
		}

		private string _helpID;
		private bool _renderHelp = false;

		public virtual void RenderHelp(HtmlTextWriter writer)
		{
			if (_renderHelp)
			{
				writer.Write("<br>");
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "elementhelp");
				writer.RenderBeginTag(HtmlTextWriterTag.Font);
				writer.Write(HttpUtility.HtmlEncode(_helpString));
				writer.RenderEndTag();
				_renderHelp = false;
			}
			else
			{
				if (_helpString != String.Empty)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Type, "image");
					writer.AddAttribute(HtmlTextWriterAttribute.Name, _helpID);
					writer.AddAttribute(HtmlTextWriterAttribute.Src, @"images\help.gif");
					writer.RenderBeginTag(HtmlTextWriterTag.Input);
					writer.RenderEndTag();
				}
			}
		}

		// TabStop

		// TODO: TabStop support in the web (there is support for TabIndex, but there doesn't seem to be support for Tab Stop toggle)

		private bool _tabStop;
		[DefaultValueMember("GetDefaultTabStop")]
		public bool TabStop
		{
			get { return _tabStop; }
			set { _tabStop = value; }
		}

		protected virtual bool GetDefaultTabStop()
		{
			return true;
		}

		public virtual bool GetTabStop()
		{
			return _tabStop;
		}

		// Visible

		private bool _visible = true;
		[DefaultValue(true)]
		public bool Visible { get { return _visible; } set { _visible = value; } }

		public virtual bool GetVisible()
		{
			return _visible;
		}

		// ErrorMessage

		private string _errorMessage;
		/// <summary> Constraint violation or other field level errors to be shown at next field rendering. </summary>
		public string ErrorMessage { get { return _errorMessage; } set { _errorMessage = value; } }

		protected virtual void RenderErrorMessage(HtmlTextWriter writer)
		{
			if (_errorMessage != null)
			{
				writer.Write("<br>");
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "elementerror");
				writer.RenderBeginTag(HtmlTextWriterTag.Font);
				writer.Write(HttpUtility.HtmlEncode(_errorMessage));
				writer.RenderEndTag();
				_errorMessage = null;
			}
		}

		// Element exceptions

		public virtual void HandleElementException(Exception exception)
		{
			if (_errorMessage == null)
				_errorMessage = exception.Message;
			else
				_errorMessage = _errorMessage + "\r\n" + exception.Message;
		}

		// IWebElement

		public virtual void Render(HtmlTextWriter writer)
		{
			if (GetVisible())
			{
				InternalRender(writer);
				RenderHelp(writer);
				RenderErrorMessage(writer);
			}
		}

		public virtual bool ProcessRequest(HttpContext context)
		{
			if (context.Request.Form[_helpID + ".x"] != null)
			{
				_renderHelp = true;
				return true;
			}
			else
				return false;
		}

		protected abstract void InternalRender(HtmlTextWriter writer);
	}
}
