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
	public abstract class DataElement : Element, IDataElement
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
					SourceChanged(FSource);
				}
			}
		}
		
		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}

		protected virtual void SourceChanged(ISource AOldSource) {}
		
		// ReadOnly

		private bool FReadOnly;
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set { FReadOnly = value; }
		}
	}

	public abstract class ColumnElement : DataElement, IColumnElement
	{
		// Title		
		
		private string FTitle = String.Empty;
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value == null ? String.Empty : value; }
		}

		// ColumnName

		private string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}

		protected bool IsDataViewActive()
		{
			return 
				(ColumnName != String.Empty) 
					&& (Source != null) 
					&& (Source.DataView != null);
		}

		protected bool IsFieldActive()
		{
			return IsDataViewActive() && !Source.DataView.IsEmpty();
		}

		protected string GetFieldValue(out bool AHasValue)
		{
			if (IsFieldActive())
			{
				DAE.Client.DataField LField = Source.DataView.Fields[ColumnName];
				AHasValue = LField.HasValue();
				if (AHasValue)
					return LField.AsString;
				else
					return String.Empty;
			}
			else
			{
				AHasValue = false;
				return String.Empty;
			}
		}
	}

	public abstract class TitledElement : ColumnElement, ITitledElement
	{
		// VerticalAlignment

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set	{ FVerticalAlignment = value; }
		}

		// MaxWidth

		// TODO: Implement MaxWidth for the web client

		private int FMaxWidth = -1;
		public int MaxWidth
		{
			get { return FMaxWidth; }
			set { FMaxWidth = value; }
		}

		// Width

		private int FWidth;
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}

		// TitleAlignment

		private TitleAlignment FTitleAlignment = TitleAlignment.Top;
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set { FTitleAlignment = value; }
		}

		// IWebElement

		protected abstract void RenderElement(HtmlTextWriter AWriter);

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			switch (FTitleAlignment)
			{
				case TitleAlignment.None : RenderElement(AWriter); break;
				case TitleAlignment.Top :
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(Title)));
					AWriter.Write("<br>");
					RenderElement(AWriter);
					break;
				case TitleAlignment.Left :
					AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Div);
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(Title)));
					RenderElement(AWriter);
					AWriter.RenderEndTag();
					break;
			}
		}
	}

	public abstract class AlignedElement : TitledElement, IAlignedElement
	{
		// TextAlignment

		private HorizontalAlignment FTextAlignment = HorizontalAlignment.Left;
		public HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set { FTextAlignment = value; }
		}
	}

	public class Text : AlignedElement, IText
	{
		// Height (n/a for web client)

		private int FHeight = 1;
		[DefaultValue(1)]
		public int Height
		{
			get { return FHeight; }
			set { FHeight = value; }
		}

		protected override void RenderElement(HtmlTextWriter AWriter)
		{
			switch (TextAlignment)
			{
				case HorizontalAlignment.Left : AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "left"); break;
				case HorizontalAlignment.Center : AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "center"); break;
				case HorizontalAlignment.Right : AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "right"); break;
			}
			AWriter.WriteBeginTag("pre");
			AWriter.WriteAttribute("class", "text");
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.WriteAttribute("title", LHint, true);
			AWriter.Write('>');

			if (IsFieldActive())
			{
				DAE.Client.DataField LField = Source.DataView.Fields[ColumnName];
				if (LField.HasValue())
					AWriter.Write(HttpUtility.HtmlEncode(LField.AsDisplayString));
			}

			AWriter.WriteEndTag("pre");
		}

	}
	
	public class TextBox : AlignedElement, ITextBox, IWebPrehandler
	{
		public TextBox()
		{
			FHasValueID = Session.GenerateID();
		}

		// Height

		private int FHeight = 1;
		public int Height
		{
			get { return FHeight; }
			set { FHeight = value; }
		}


		// MaxLength

		private int FMaxLength = -1;
		public int MaxLength
		{
			get { return FMaxLength; }
			set { FMaxLength = value; }
		}

		// IsPassword

		private bool FIsPassword;
		public bool IsPassword
		{
			get { return FIsPassword; }
			set { FIsPassword = value; }
		}

		// WordWrap

		private bool FWordWrap;
		public bool WordWrap
		{
			get { return FWordWrap; }
			set { FWordWrap = value; }
		}

		// AcceptsReturn

		private bool FAcceptsReturn = true;
		[DefaultValue(true)]
		public bool AcceptsReturn
		{
			get { return FAcceptsReturn; }
			set { FAcceptsReturn = value; }
		}

		// AcceptsTabs

		private bool FAcceptsTabs = false;
		[DefaultValue(false)]
		public bool AcceptsTabs
		{
			get { return FAcceptsTabs; }
			set { FAcceptsTabs = value; }
		}
		
		// NilIfBlank

		private bool FNilIfBlank = true;
		[DefaultValue(true)]
		public bool NilIfBlank
		{
			get { return FNilIfBlank; }
			set { FNilIfBlank = value; }
		}
		
		// AutoUpdate
		
		private bool FAutoUpdate;
		[DefaultValue(false)]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set { FAutoUpdate = value; }
		}

		// AutoUpdateInterval

		// UNSUPPORTED IN WEB
		        
        private int FAutoUpdateInterval = 200;
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set { FAutoUpdateInterval = value; }
		}

		// HasValueID

		private string FHasValueID;
		public string HasValueID { get { return FHasValueID; } }

		// IWebPrehandler

		public virtual void PreprocessRequest(HttpContext AContext)
		{
			if (!ReadOnly && IsDataViewActive())
			{
				string LValue = AContext.Request.Form[ID];
				string LHasValue = AContext.Request.Form[FHasValueID];
				if ((LValue != null) && ((LHasValue == null) || ((LHasValue != null) && (String.Compare(LHasValue, "true", true) == 0))))
				{
					DAE.Client.DataField LField = Source.DataView.Fields[ColumnName];
					if ((LValue == String.Empty) && FNilIfBlank)
						LField.ClearValue();
					else
					{
						Source.DataView.Edit();
						if (LField.AsString != LValue)
							LField.AsString = LValue;
					}
				}
			}
		}

		// IWebElement

		protected override void RenderElement(HtmlTextWriter AWriter)
		{
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Name, ID);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Id, ID);

			bool LHasValue;
			string LValue = GetFieldValue(out LHasValue);

			string LStyle = "";
			string LClass;
			
			if (TextAlignment != HorizontalAlignment.Left)
				LStyle = "text-Alignment: " + TextAlignment.ToString().ToLower() + "; ";
				
			if (ReadOnly)
			{
				LClass = "readonlytextbox";
				LStyle += "width: " + Width.ToString() + "ex;";
			}
			else
			{
				LClass = "textbox";
				if (!LHasValue)
				{
					AWriter.AddAttribute("onkeydown", String.Format("NotNull(this, '{0}', 'textbox')", FHasValueID));
					AWriter.AddAttribute("onchange", String.Format("NotNull(this, '{0}', 'textbox')", FHasValueID));
				}
			}
			if (!LHasValue)
				LClass = LClass + "null";
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, LClass);
			
			if (!String.IsNullOrEmpty(LStyle))
				AWriter.AddAttribute(HtmlTextWriterAttribute.Style, LStyle);
				
			if (ReadOnly)
			{
				// HACK: IE does not recognize foreground colors for disabled controls, so we must use a div rather than a textbox control for read-only
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);
				AWriter.Write(HttpUtility.HtmlEncode(FWordWrap ? LValue.Replace("\r\n", " ") : LValue));
				AWriter.RenderEndTag();
			}
			else
			{
				if (FHeight == 1)
				{
					if (IsPassword)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "password");
					else
						AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Size, Width.ToString());
					AWriter.AddAttribute(HtmlTextWriterAttribute.Value, LValue);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
					AWriter.RenderEndTag();
				}
				else
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Cols, Width.ToString());
					AWriter.AddAttribute(HtmlTextWriterAttribute.Rows, Height.ToString());
					AWriter.AddAttribute(HtmlTextWriterAttribute.Wrap, (FWordWrap ? "soft" : "off"));
					AWriter.RenderBeginTag(HtmlTextWriterTag.Textarea);
					AWriter.Write(HttpUtility.HtmlEncode(LValue));
					AWriter.RenderEndTag();
				}

				if (!LHasValue)
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Name, FHasValueID);
					AWriter.AddAttribute(HtmlTextWriterAttribute.Id, FHasValueID);
					AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Value, "false");
					AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
					AWriter.RenderEndTag();
				}
				
				// TODO: Reenable auto-update when there is a way to reset the focused control when posting (really annoying right now when turned on)
	//			if (FAutoUpdate && !ReadOnly)
	//				AWriter.AddAttribute(HtmlTextWriterAttribute.Onchange, "Submit('" + (string)Session["DefaultPage"] + "',event)");
			}	
		}	 
	}

	public class NumericTextBox : TextBox, INumericTextBox
	{
		// TODO: implement javascript enabled spinbox

		// INumericTextBox

		private int FSmallIncrement = 1;
		public int SmallIncrement
		{
			get { return FSmallIncrement; }
			set { FSmallIncrement = value; }
		}

		private int FLargeIncrement = 10;
		public int LargeIncrement
		{
			get { return FLargeIncrement; }
			set { FLargeIncrement = value; }
		}
	}

	public class DateTimeBox : TextBox, IDateTimeBox
	{
		// TitledElement

		protected override void RenderElement(HtmlTextWriter AWriter)
		{
			base.RenderElement(AWriter);
			if (!ReadOnly)
			{
				AWriter.AddAttribute
				(
					HtmlTextWriterAttribute.Href, 
					String.Format(@"javascript:ShowCalendar('document.getElementById(\'{0}\')', document.getElementById('{0}').value)", ID)
				);
				AWriter.RenderBeginTag(HtmlTextWriterTag.A);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/cal.gif");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "16");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, Strings.Get("DateTimeAlt"));
				AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
				AWriter.RenderEndTag();

				AWriter.RenderEndTag();
			}
		}
	}

    public class TextEditor : TextBox, ITextEditor
    {
        // DocumentType

        public const string CDefaultDocumentType = "Default";
        private string FDocumentType = CDefaultDocumentType;
        [DefaultValue(CDefaultDocumentType)]
        public string DocumentType
        {
            get { return FDocumentType; }
            set
            {
                if (FDocumentType != value)
                {
                    FDocumentType = value != null ? value : "";                   
                }
            }
        }
    }

	public class CheckBox : ColumnElement, ICheckBox, IWebPrehandler
	{
		public CheckBox()
		{
			FHasValueID = Session.GenerateID();
		}

		// Width

		// TODO: Remove Width when common properties are better handled
		// This is only here because it is a common property
		private int FWidth = 0;
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}

		// AutoUpdate

		private bool FAutoUpdate = true;
		[DefaultValue(true)]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set { FAutoUpdate = value; }
		}

		// AutoUpdateInterval

		// UNSUPPORTED IN WEB

        private bool FTrueFirst = true;
        public bool TrueFirst
        {
            get { return FTrueFirst; }
            set { FTrueFirst = value; }
        }
        
        private int FAutoUpdateInterval = 200;
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set { FAutoUpdateInterval = value; }
		}

		// HasValueID

		private string FHasValueID;
		public string HasValueID { get { return FHasValueID; } }

		// IWebPrehandler
		
		public virtual void PreprocessRequest(HttpContext AContext)
		{
			if (!ReadOnly && IsDataViewActive())
			{
				string LValue = AContext.Request.Form[ID];
				string LHasValue = AContext.Request.Form[FHasValueID];
				if ((LHasValue == null) || ((LHasValue != null) && (String.Compare(LHasValue, "true", true) == 0)))
				{
					Source.DataView.Edit();
					bool LBoolValue = (LValue != null);
					DAE.Client.DataField LField = Source.DataView.Fields[ColumnName];
					if (LField.AsBoolean != LBoolValue)
						LField.AsBoolean = LBoolValue;
				}
			}
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Name, ID);
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);

			bool LActive = IsFieldActive();
			DAE.Client.DataField LField;
			if (LActive)
				LField = Source.DataView.Fields[ColumnName];
			else
				LField = null;
			bool LHasValue = LActive && LField.HasValue();
			if (LActive)
			{
				if (LHasValue && LField.AsBoolean)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Checked, null);

				if (!ReadOnly)
				{
					if (LHasValue)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "checkbox");
					else
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onchange, String.Format("NotNull(this, '{0}', 'checkbox')", FHasValueID));
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "checkboxnull");
					}
				}
			}
			if (ReadOnly || !LActive)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "readonlycheckbox");
			}
			// TODO: Reenable auto-update when there is a way to reset the focused control when posting (really annoying right now when turned on)
//			if (FAutoUpdate && !ReadOnly)
//				AWriter.AddAttribute(HtmlTextWriterAttribute.Onchange, "Submit('" + (string)Session["DefaultPage"] + "',event)");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Value, "true");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.Write("&nbsp;");
			AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(Title)));
			AWriter.Write("&nbsp;");
			AWriter.RenderEndTag();

			if (!LHasValue && !ReadOnly)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Name, FHasValueID);
				AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Value, "false");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
				AWriter.RenderEndTag();
			}
		}
	}

	public class Choice : ColumnElement, IChoice, IWebPrehandler
	{
		// Items

		private string FItems = String.Empty;
		public string Items
		{
			get { return FItems; }
			set { FItems = value; }
		}

		// AutoUpdate

		// TODO: Support auto update (and interval) by posting back

		private bool FAutoUpdate;
		[DefaultValue(false)]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set { FAutoUpdate = value; }
		}

		// AutoUpdateInterval

		// UNSUPPORTED IN WEB
		        
        private int FAutoUpdateInterval = 200;
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set { FAutoUpdateInterval = value; }
		}

		// Width

		// TODO: Width in Choice because it is a common derivation prop.  Remove w/ better derivation system.
		private int FWidth = 0;
		[DefaultValue(0)]
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}

		// Columns

		// TODO: Support RadioGroup Columns in Web
		private int FColumns = 1;
		[DefaultValue(1)]
		public int Columns
		{
			get { return FColumns; }
			set { FColumns = Math.Max(1, value); }
		}


		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "group");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "3");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Caption);
			AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(Title)));
			AWriter.RenderEndTag();

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			if (FColumns > 1)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			}

			bool LHasValue;
			string LValue = GetFieldValue(out LHasValue);
			string[] LItems = FItems.Split(new char[] {';', ','});
			string LItemName;
			string LItemValue;
			int LPos;
			string LItem;
			bool LFirst = true;
			int LCurrentColumn = 0;
			int LButtonsPerColumn = (LItems.Length / FColumns) + ((LItems.Length % FColumns) == 0 ? 0 : 1);
			for (int i = 0; i < LItems.Length; i++)
			{
				LItem = LItems[i];
				LPos = LItem.IndexOf("=");
				if (LPos > 0)
				{
					// Start a new column if necessary
					if (LCurrentColumn != (i / LButtonsPerColumn))
					{
						AWriter.RenderEndTag();
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
						LFirst = true;
					}
					LItemName = LItem.Substring(0, LPos).Trim();
					LItemValue = LItem.Substring(LPos + 1).Trim();

					if (LFirst)
						LFirst = false;
					else
						AWriter.Write("<br>");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Name, ID);
					AWriter.AddAttribute(HtmlTextWriterAttribute.Value, LItemValue, true);
					if (ReadOnly)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Disabled, null);
					if (LHasValue && (LValue == LItemValue))
						AWriter.AddAttribute(HtmlTextWriterAttribute.Checked, null);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Input);

					Session.RenderDummyImage(AWriter, "4", "1");
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(LItemName)));
					Session.RenderDummyImage(AWriter, "6", "1");

					AWriter.RenderEndTag();
				}
			}

			if (FColumns > 1)
			{
				AWriter.RenderEndTag();	// TD
				AWriter.RenderEndTag(); // TR
				AWriter.RenderEndTag(); // TABLE
			}
			AWriter.RenderEndTag();	// TD
			AWriter.RenderEndTag(); // TR
			AWriter.RenderEndTag(); // TABLE
		}

		// IWebPrehandler

		public virtual void PreprocessRequest(HttpContext AContext)
		{
			if (!ReadOnly && IsDataViewActive())
			{
				string LValue = AContext.Request.Form[ID];
				if ((LValue != null) && (LValue != String.Empty))
				{
					Source.DataView.Edit();
					DAE.Client.DataField LField = Source.DataView.Fields[ColumnName];
					if (LField.AsString != LValue)
						LField.AsString = LValue;
				}
			}
		}
	}

	public class Image : TitledElement, IImage
	{
		public const int CMinImageWidth = 20;
		public const int CImageHeight = 90;
		
		// ImageWidth

		private int FImageWidth = -1;
		[DefaultValue(-1)]
		public int ImageWidth
		{
			get { return FImageWidth; }
			set { FImageWidth = value; }
		}

		// ImageHeight

		private int FImageHeight = -1;
		[DefaultValue(-1)]
		public int ImageHeight
		{
			get { return FImageHeight; }
			set { FImageHeight = value; }
		}

		// StretchStyle
		
		private StretchStyles FStretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		public StretchStyles StretchStyle
		{
			get { return FStretchStyle; }
			set { FStretchStyle = value; }
		}

		// Center
		
		private bool FCenter = true;
		[DefaultValue(true)]
		public bool Center
		{
			get { return FCenter; }
			set { FCenter = value; }
		}

		protected DAE.Client.DataField DataField 
		{
			get
			{
				if ((Source != null) && (Source.DataView != null))
					return Source.DataView.Fields[ColumnName];
				else
					return null;
			}
		}

		// TitledElement

		protected override void RenderElement(HtmlTextWriter AWriter)
		{
			if (IsFieldActive())
			{
				if (FCenter)
				{
					AWriter.AddAttribute("Alignment", "center", false);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Div);
				}

				string LHint = GetHint();
				if (LHint != String.Empty)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?HandlerID=" + FImageID);
				StaticImage.RenderStretchAttributes(AWriter, FStretchStyle, FImageWidth, FImageHeight);
				AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, FVerticalAlignment.ToString().ToLower());
				if (ReadOnly)
					AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
				else
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "image");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Name, ID);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
				}
				AWriter.RenderEndTag();

				if (FCenter)
					AWriter.RenderEndTag();
			}
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			else
			{
				if (AContext.Request.Form[ID + ".x"] != null)
				{
					IHost LHost = HostNode.Session.CreateHost();
					try
					{
						SetImageForm LForm = new SetImageForm(Title);
						try
						{
							LHost.Children.Add(LForm);
							LHost.Open();
							LForm.Show((IFormInterface)FindParent(typeof(IFormInterface)), new FormInterfaceHandler(SetImageAccepted), null, FormMode.Edit);
						}
						catch
						{
							LForm.Dispose();
							throw;
						}
					}
					catch
					{
						LHost.Dispose();
						throw;
					}
					return true;
				}
				else
					return false;
			}
		}

		private IImageSource FImageSource;
		public IImageSource ImageSource
		{
			get { return FImageSource; }
		}

		public void SetImageAccepted(IFormInterface AForm)
		{
			FImageSource = (IImageSource)AForm;
			if (FImageSource.Stream == null)
				DataField.ClearValue();
			else
			{
				using (DAE.Runtime.Data.Scalar LNewValue = new DAE.Runtime.Data.Scalar(Source.DataView.Process.ValueManager, Source.DataView.Process.DataTypes.SystemGraphic))
				{
					Stream LStream = LNewValue.OpenStream();
					try
					{
						FImageSource.Stream.Position = 0;
						StreamUtility.CopyStream(FImageSource.Stream, LStream);
					}
					finally
					{
						LStream.Close();
					}
					DataField.Value = LNewValue;
				}
			}
		}

		private string FImageID = String.Empty;
		public string ImageID { get { return FImageID; } }

		private void GetImage(HttpContext AContext, string AID, Stream AStream)
		{
			if (IsFieldActive() && DataField.HasValue())
			{
				Stream LStream = DataField.Value.OpenStream();
				try
				{
					StreamUtility.CopyStream(LStream, AStream);
				}
				finally
				{
					LStream.Close();
				}
			}
		}

		protected override void Activate()
		{
			base.Activate();
			FImageID = WebSession.ImageCache.RegisterImageHandler(new LoadImageHandler(GetImage));
		}

		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				WebSession.ImageCache.UnregisterImageHandler(FImageID);
			}
		}
	}

	public class SetImageForm : FormInterface, IWebPrehandler, IImageSource
	{
		public SetImageForm(string ATitle)
		{
			Text = String.Format(Strings.Get("SetImageFormTitle"), ATitle);
		}

		// IImageSource

		private Stream FStream;
		public Stream Stream { get { return FStream; } }

		private bool FLoading;
		public bool Loading
		{
			get { return FLoading; }
		}

		private HtmlTextWriter FWriter;
		public void LoadImage()
		{
			FWriter.WriteLine(HttpUtility.HtmlEncode(Strings.Get("ImageUpload")));
			FWriter.RenderBeginTag(HtmlTextWriterTag.Br);
			FWriter.RenderEndTag();

			FWriter.AddAttribute(HtmlTextWriterAttribute.Type, "file");
			FWriter.AddAttribute(HtmlTextWriterAttribute.Name, "InputFile");
			FWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			FWriter.RenderEndTag();
		}

		// IWebPrehandler

		public override void PreprocessRequest(HttpContext AContext)
		{
			if (AContext.Request.Files.Count > 0)
			{
				FLoading = true;
				Stream LStream = new MemoryStream();
				StreamUtility.CopyStream(AContext.Request.Files[0].InputStream, LStream);
				FStream = LStream;
			}
			base.ProcessRequest(AContext);
		}

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			base.InternalRender(AWriter);
			FWriter = AWriter;
			LoadImage();
		} 
	}
}
