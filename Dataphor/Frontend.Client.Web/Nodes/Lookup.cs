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
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Source = null;
		}

		// Source

		private ISource FSource;
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
				{
					if (FSource != null)
						FSource.Disposed -= new EventHandler(SourceDisposed);
					FSource = value;
					if (FSource != null)
						FSource.Disposed += new EventHandler(SourceDisposed);
				}
			}
		}
		
		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}

		// ReadOnly

		private bool FReadOnly;
		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set { FReadOnly = value; }
		}

		// Title		

		private string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value == null ? String.Empty : value; }
		}

		public virtual string GetTitle()
		{
			return FTitle;
		}

		// Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
		public string Document
		{
			get { return FDocument; }
			set { FDocument = value; }
		}

		// MasterKeyNames

		private string FMasterKeyNames = String.Empty;
		[DefaultValue("")]
		public string MasterKeyNames
		{
			get { return FMasterKeyNames; }
			set { FMasterKeyNames = value == null ? String.Empty : value; }
		}

		// DetailKeyNames

		private string FDetailKeyNames = String.Empty;
		[DefaultValue("")]
		public string DetailKeyNames
		{
			get { return FDetailKeyNames; }
			set { FDetailKeyNames = value == null ? String.Empty : value; }
		}

		// AutoLookup - NOT SUPPORTED by the web client

		private bool FAutoLookup = false;
		[DefaultValue(false)]
		public bool AutoLookup
		{
			get { return FAutoLookup; }
			set { FAutoLookup = value; }
		}

		private bool FAutoLookupWhenNotNil;
		[DefaultValue(false)]
		public bool AutoLookupWhenNotNil
		{
			get { return FAutoLookupWhenNotNil; }
			set { FAutoLookupWhenNotNil = value; }
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

		private void LookupAccepted(IFormInterface AForm) 
		{
			if 
			(
				!ReadOnly
					&& (Source != null) 
					&& (Source.DataView != null) 
					&& (AForm.MainSource != null) 
					&& (AForm.MainSource.DataView != null) 
					&& !AForm.MainSource.DataView.IsEmpty()
			)
			{
				int LIndex = -1;
				string[] LTargetColumns = GetColumnNames().Split(DAE.Client.DataView.CColumnNameDelimiters);
				foreach (string LSourceColumnName in GetLookupColumnNames().Split(DAE.Client.DataView.CColumnNameDelimiters))
				{
					++LIndex;
					DAE.Client.DataField LSource = AForm.MainSource.DataView.Fields[LSourceColumnName.Trim()];
					DAE.Client.DataField LTarget = Source.DataSource.DataSet.Fields[LTargetColumns[LIndex].Trim()];
					if (!LSource.HasValue())
						LTarget.ClearValue();
					else
						LTarget.Value = LSource.Value;
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

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, ID) && IsDataViewActive() && !ReadOnly)
			{
				Lookup();
				return true;
			}
			else
				return base.ProcessRequest(AContext);
		}

		// ILookup

		void ILookup.LookupFormInitialize(IFormInterface AForm) 
		{
			// Nadda
		}

	}

	public class QuickLookup : LookupBase, IQuickLookup
	{
		#region LookupColumnName

		private string FLookupColumnName = String.Empty;
		[DefaultValue("")]
		public string LookupColumnName
		{
			get { return FLookupColumnName; }
			set { FLookupColumnName = (value == null ? String.Empty : value); }
		}
		
		public override string GetLookupColumnNames()
		{
			return FLookupColumnName;
		}

		#endregion

		#region ColumnName

		private string FColumnName = String.Empty;
		[DefaultValue("")]
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = (value == null ? String.Empty : value); }
		}
		
		public override string GetColumnNames()
		{
			return FColumnName;
		}

		#endregion

		#region VerticalAlignment

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set { FVerticalAlignment = value; }
		}

		#endregion

		#region TitleAlignment		

		private TitleAlignment FTitleAlignment = TitleAlignment.Top;
		[DefaultValue(TitleAlignment.Top)]
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set { FTitleAlignment = value; }
		}

		#endregion

		// TitledElement

		protected virtual void RenderElement(HtmlTextWriter AWriter)
		{
			base.InternalRender(AWriter);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/lookup.png");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "15");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, Strings.Get("LookupButtonAlt"));
			if (!ReadOnly && IsDataViewActive())
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();
		}

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			switch (FTitleAlignment)
			{
				case TitleAlignment.None : RenderElement(AWriter); break;
				case TitleAlignment.Top :
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(GetTitle())));
					AWriter.Write("<br>");
					RenderElement(AWriter);
					break;
				case TitleAlignment.Left :
					AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Div);
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(GetTitle())));
					RenderElement(AWriter);
					AWriter.RenderEndTag();
					break;
			}
		}
	}

	public class FullLookup : LookupBase, IFullLookup
	{
		//ColumnNames

		private string FColumnNames = String.Empty;
		[DefaultValue("")]
		public string ColumnNames
		{
			get { return FColumnNames; }
			set { FColumnNames = value == null ? String.Empty : value; }
		}

		public override string GetColumnNames()
		{
			return FColumnNames;
		}

		// LookupColumnNames

		private string FLookupColumnNames = String.Empty;
		[DefaultValue("")]
		public string LookupColumnNames
		{
			get { return FLookupColumnNames; }
			set { FLookupColumnNames = value == null ? String.Empty : value; }
		}

		public override string GetLookupColumnNames()
		{
			return FLookupColumnNames;
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

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			base.InternalRender(AWriter);
			AWriter.RenderEndTag();

			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/lookup.png");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "15");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, Strings.Get("LookupButtonAlt"));
			if (!ReadOnly && IsDataViewActive())
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TD
			AWriter.RenderEndTag();	// TR
			AWriter.RenderEndTag(); // TABLE
		}
	}
}
