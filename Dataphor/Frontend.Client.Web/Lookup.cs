/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Web.UI;
using System.Drawing;
using System.ComponentModel;
using System.IO;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public abstract class LookupBase : SingleElementContainer, ILookupElement
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}

		// Source

		private ISource _source;
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					if (_source != null)
						_source.Disposed -= new EventHandler(SourceDisposed);
					_source = value;
					if (_source != null)
						_source.Disposed += new EventHandler(SourceDisposed);
				}
			}
		}
		
		protected virtual void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}

		// ReadOnly

		private bool _readOnly;
		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set { _readOnly = value; }
		}

		// Title		

		private string _title = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get { return _title; }
			set { _title = value == null ? String.Empty : value; }
		}

		public virtual string GetTitle()
		{
			return _title;
		}

		// Document

		private string _document = String.Empty;
		[DefaultValue("")]
		public string Document
		{
			get { return _document; }
			set { _document = value; }
		}

		// MasterKeyNames

		private string _masterKeyNames = String.Empty;
		[DefaultValue("")]
		public string MasterKeyNames
		{
			get { return _masterKeyNames; }
			set { _masterKeyNames = value == null ? String.Empty : value; }
		}

		// DetailKeyNames

		private string _detailKeyNames = String.Empty;
		[DefaultValue("")]
		public string DetailKeyNames
		{
			get { return _detailKeyNames; }
			set { _detailKeyNames = value == null ? String.Empty : value; }
		}

		// AutoLookup - NOT SUPPORTED by the web client

		private bool _autoLookup = false;
		[DefaultValue(false)]
		public bool AutoLookup
		{
			get { return _autoLookup; }
			set { _autoLookup = value; }
		}

		private bool _autoLookupWhenNotNil;
		[DefaultValue(false)]
		public bool AutoLookupWhenNotNil
		{
			get { return _autoLookupWhenNotNil; }
			set { _autoLookupWhenNotNil = value; }
		}

		public void ResetAutoLookup() {}

		#region Lookup

		protected void Lookup()
		{
			LookupUtility.DoLookup
			(
				this,
				new FormInterfaceHandler(LookupAccepted),
				null,
				null
			);
		}

		private void LookupAccepted(IFormInterface form) 
		{
			if 
			(
				!ReadOnly
					&& (Source != null) 
					&& (Source.DataView != null) 
					&& (form.MainSource != null) 
					&& (form.MainSource.DataView != null) 
					&& !form.MainSource.DataView.IsEmpty()
			)
			{
				int index = -1;
				string[] targetColumns = GetColumnNames().Split(DAE.Client.DataView.ColumnNameDelimiters);
				foreach (string sourceColumnName in GetLookupColumnNames().Split(DAE.Client.DataView.ColumnNameDelimiters))
				{
					++index;
					DAE.Client.DataField source = form.MainSource.DataView.Fields[sourceColumnName.Trim()];
					DAE.Client.DataField target = Source.DataSource.DataSet.Fields[targetColumns[index].Trim()];
					if (!source.HasValue())
						target.ClearValue();
					else
						target.Value = source.Value;
				}
			}
		}
		
		#endregion

		public abstract string GetColumnNames();

		public abstract string GetLookupColumnNames();

		// IWebHandler

		protected bool IsDataViewActive()
		{
			return 
				(GetColumnNames() != String.Empty) 
					&& (Source != null) 
					&& (Source.DataView != null);
		}

		public override bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, ID) && IsDataViewActive() && !ReadOnly)
			{
				Lookup();
				return true;
			}
			else
				return base.ProcessRequest(context);
		}

		// ILookup

		void ILookup.LookupFormInitialize(IFormInterface form) 
		{
			// Nadda
		}

	}

	public class QuickLookup : LookupBase, IQuickLookup
	{
		#region LookupColumnName

		private string _lookupColumnName = String.Empty;
		[DefaultValue("")]
		public string LookupColumnName
		{
			get { return _lookupColumnName; }
			set { _lookupColumnName = (value == null ? String.Empty : value); }
		}
		
		public override string GetLookupColumnNames()
		{
			return _lookupColumnName;
		}

		#endregion

		#region ColumnName

		private string _columnName = String.Empty;
		[DefaultValue("")]
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = (value == null ? String.Empty : value); }
		}
		
		public override string GetColumnNames()
		{
			return _columnName;
		}

		#endregion

		#region VerticalAlignment

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set { _verticalAlignment = value; }
		}

		#endregion

		#region TitleAlignment		

		private TitleAlignment _titleAlignment = TitleAlignment.Top;
		[DefaultValue(TitleAlignment.Top)]
		public TitleAlignment TitleAlignment
		{
			get { return _titleAlignment; }
			set { _titleAlignment = value; }
		}

		#endregion

		// TitledElement

		protected virtual void RenderElement(HtmlTextWriter writer)
		{
			base.InternalRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "lookup");
			writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/lookup.png");
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			writer.AddAttribute(HtmlTextWriterAttribute.Height, "15");
			writer.AddAttribute(HtmlTextWriterAttribute.Alt, Strings.Get("LookupButtonAlt"));
			if (!ReadOnly && IsDataViewActive())
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID));
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();
		}

		protected override void InternalRender(HtmlTextWriter writer)
		{
			switch (_titleAlignment)
			{
				case TitleAlignment.None : RenderElement(writer); break;
				case TitleAlignment.Top :
					writer.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(GetTitle())));
					writer.Write("<br>");
					RenderElement(writer);
					break;
				case TitleAlignment.Left :
					writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					writer.RenderBeginTag(HtmlTextWriterTag.Div);
					writer.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(GetTitle())));
					RenderElement(writer);
					writer.RenderEndTag();
					break;
			}
		}
	}

	public class FullLookup : LookupBase, IFullLookup
	{
		//ColumnNames

		private string _columnNames = String.Empty;
		[DefaultValue("")]
		public string ColumnNames
		{
			get { return _columnNames; }
			set { _columnNames = value == null ? String.Empty : value; }
		}

		public override string GetColumnNames()
		{
			return _columnNames;
		}

		// LookupColumnNames

		private string _lookupColumnNames = String.Empty;
		[DefaultValue("")]
		public string LookupColumnNames
		{
			get { return _lookupColumnNames; }
			set { _lookupColumnNames = value == null ? String.Empty : value; }
		}

		public override string GetLookupColumnNames()
		{
			return _lookupColumnNames;
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

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			base.InternalRender(writer);
			writer.RenderEndTag();

			writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "lookup");
			writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/lookup.png");
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			writer.AddAttribute(HtmlTextWriterAttribute.Height, "15");
			writer.AddAttribute(HtmlTextWriterAttribute.Alt, Strings.Get("LookupButtonAlt"));
			if (!ReadOnly && IsDataViewActive())
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID));
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			writer.RenderEndTag();	// TD
			writer.RenderEndTag();	// TR
			writer.RenderEndTag(); // TABLE
		}
	}
}
