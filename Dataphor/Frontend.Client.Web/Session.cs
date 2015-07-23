/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Collections;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Net;
using System.IO;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	/// <summary> Session represents the client session with the application server. </summary>
	public class Session : Client.Session
	{
		public const string ImageCachePath = @"DataphorWebClientImageCache";

		public Session(DataSession session, bool ownsConnection) : base(session, ownsConnection)
		{
			_imageCache = new ImageCache(Path.GetTempPath() + ImageCachePath, this);
		}

		protected override void Dispose(bool disposed)
		{
			try
			{
				CloseAllForms();
			}
			finally
			{
				try
				{
					base.Dispose(disposed);
				}
				finally
				{
					if (_imageCache != null)
					{
						_imageCache.Dispose();
						_imageCache = null;
					}
				}
			}
		}

		~Session()
		{
			Dispose(false);
		}

		// AreImagesLoaded
		
		public override bool AreImagesLoaded()
		{
			return false;
		}

		// Context

		private HttpContext _context;
		public HttpContext Context { get { return _context; } }

		// ImageCache

		private ImageCache _imageCache;

		public ImageCache ImageCache { get { return _imageCache; } }

		public static void AddImageLinkAttribute(HtmlTextWriter writer, string attributeName, string imageID)
		{
			if (imageID != String.Empty)
				writer.AddAttribute(attributeName, "ViewImage.aspx?ImageID=" + imageID, false);
		}

		public static void AddImageLinkAttribute(HtmlTextWriter writer, HtmlTextWriterAttribute attribute, string imageID)
		{
			if (imageID != String.Empty)
				writer.AddAttribute(attribute, "ViewImage.aspx?ImageID=" + imageID, false);
		}

		// Forms

		protected override void FormAdded(IFormInterface form, bool stack)
		{
			base.FormAdded(form, stack);
			_formsByID.Add(((IWebElement)form).ID, form);
		}

		protected override void FormRemoved(IFormInterface form, bool stack)
		{
			base.FormRemoved(form, stack);
			_formsByID.Remove(((IWebElement)form).ID);
		}
		
		private Hashtable _formsByID = new Hashtable();

		public IFormInterface GetForm(string formID)
		{
			return (IFormInterface)_formsByID[formID];
		}

		/// <summary> Closes the session's forms and returns true if it is okay to close the session. </summary>
		/// <remarks> Closes all of the forms opened in this session (except for the root form). </remarks>
		public bool PrepareToClose(CloseBehavior behavior)
		{
			while (_formsByID.Count > 1)
			{
				foreach (IFormInterface form in Forms)
				{
					if (((_rootFormHost == null) || (_rootFormHost.Children.Count == 0) || (form != _rootFormHost.Children[0])) && !form.Close(behavior))
						return false;
				}
			}
			return true;
		}

		/// <summary> Attempts to close or reject all forms. </summary>
		/// <returns> True if all forms were closed. </returns>
		public bool CloseAllForms()
		{
			if (!PrepareToClose(CloseBehavior.RejectOrClose))
				return false;
			return 
				(
					(_rootFormHost == null) 
						|| (_rootFormHost.Children.Count == 0)
						|| ((IFormInterface)_rootFormHost.Children[0]).Close(CloseBehavior.RejectOrClose)
				);
		}

		public string GetTitle()
		{
			if (Forms.IsEmpty())
				return String.Empty;
			else
				return Forms.GetTopmostForm().Text;
		}

		// ProcessRequest

		public virtual void ProcessRequest(HttpContext context)
		{
			_context = context;
			try
			{
				ProcessShowFormWarning(context);
				try
				{
					PreprocessFormRequest(context);
					ProcessFormRequest(context);	// this must come before ProcessFormChange()
				}
				finally
				{
					ProcessFormChange(context);
				}
			}
			catch (Exception exception)
			{
				ErrorList.Add(exception);
			}
			_context = null;
		}

		/// <summary> Process this request with the appropriate form. </summary>
		protected virtual void ProcessFormRequest(HttpContext context)
		{
			// Propigate the request to the appropriate form
			string formIDString = context.Request.Form["FormID"];
			if ((formIDString != null) && (formIDString != String.Empty))
			{
				IFormInterface form = GetForm(formIDString);
				if (form != null)
					form.BroadcastEvent(new ProcessRequestEvent(context));
			}
		}

		protected virtual void PreprocessFormRequest(HttpContext context)
		{
			// Propigate the request to the appropriate form
			string formIDString = context.Request.Form["FormID"];
			if ((formIDString != null) && (formIDString != String.Empty))
			{
				IWebFormInterface form = (IWebFormInterface)GetForm(formIDString);
				if (form != null)
					form.PreprocessRequest(context);
			}
		}

		/// <summary> Change frontmost form if requested to do so. </summary>
		protected virtual void ProcessFormChange(HttpContext context)
		{
			string formIDString = context.Request.QueryString["FormID"];
			if ((formIDString != null) && (formIDString != String.Empty))
			{
				IFormInterface form = GetForm(formIDString);
				if (form != null)
					Forms.BringToFront(form);
			}
		}

		protected virtual void ProcessShowFormWarning(HttpContext context)
		{
			string formIDString = context.Request.QueryString["WarningForFormID"];
			if ((formIDString != null) && (formIDString != String.Empty))
			{
				IWebFormInterface form = GetForm(formIDString) as IWebFormInterface;
				if ((form != null) && (form.ErrorList != null))
					ShowFormWarnings(form.ErrorList);
			}
		}

		// Start

		public void SetApplication(string applicationID)
		{
			Start(SetApplication(applicationID, "Web"));
		}

		private IHost _rootFormHost;
		public IHost RootFormHost { get { return _rootFormHost; } }

		private void RootFormHostDisposed(object sender, EventArgs args)
		{
			_rootFormHost = null;
		}

		public void Start(string document)
		{
			// Close any existing forms
			if ((_rootFormHost != null) && (!CloseAllForms()))
				throw new AbortException();

			_rootFormHost = CreateHost();
			try
			{		
				_rootFormHost.Disposed += new EventHandler(RootFormHostDisposed);				   
				_rootFormHost.NextRequest = new Request(document);
				LoadNextForm();
			}
			catch
			{
				_rootFormHost.Dispose();
				_rootFormHost = null;
				throw;
			}
		}

		private void LoadNextForm()
		{
			if ((_rootFormHost != null) && (_rootFormHost.NextRequest != null))
			{
				IWebFormInterface newForm = (IWebFormInterface)NodeTypeTable.CreateInstance("FormInterface");
				newForm.OnClosing += new CancelEventHandler(MainFormClosing);
				newForm.OnClosed += new EventHandler(MainFormClosed);
				newForm = (IWebFormInterface)_rootFormHost.LoadNext(newForm);
				_rootFormHost.Open();
				newForm.Show();
			}
		}

		private void MainFormClosed(object sender, EventArgs args)
		{
			LoadNextForm();
		}

		private void MainFormClosing(object sender, CancelEventArgs args)
		{
			args.Cancel = args.Cancel || !PrepareToClose(CloseBehavior.AcceptOrClose);
		}

		// Client.Session

		public override IHost CreateHost()
		{
			Host host = new Host(this);
			host.OnDeserializationErrors += new DeserializationErrorsHandler(DeserializationErrors);
			return host;
		}

		private void DeserializationErrors(IHost host, ErrorList errorList)
		{
			if ((host != null) && (host.Children.Count > 0))
			{
				IFormInterface formInterface = host.Children[0] as IFormInterface;
				if (formInterface == null)
					formInterface = (IFormInterface)host.Children[0].FindParent(typeof(IFormInterface));

				if (formInterface != null)
				{
					formInterface.EmbedErrors(errorList);
					return;
				}
			}
			ShowFormWarnings(errorList);
		}

		public void ShowFormWarnings(ErrorList errors)
		{
			IHost host = CreateHost();
			try
			{
				ShowErrorsForm form = new ShowErrorsForm(Strings.Get("CFormLoadWarningsCaption"));
				try
				{
					host.Children.Add(form);
					form.Errors = errors;
					host.Open();
					form.Show();
				}
				catch
				{
					form.Dispose();
					throw;
				}
			}
			catch
			{
				host.Dispose();
				throw;
			}
		}

		// ErrorList

		private ErrorList _errorList = new ErrorList();
		public ErrorList ErrorList
		{
			get { return _errorList; }
		}

		// Render

		/// <summary> Renders a on pixel table dimension. </summary>
		protected void RenderDummyDimension(HtmlTextWriter writer, string color)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "1");
			writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, color);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			writer.AddAttribute(HtmlTextWriterAttribute.Width, "1");
			writer.AddAttribute(HtmlTextWriterAttribute.Height, "1");
			writer.AddAttribute(HtmlTextWriterAttribute.Alt, "");
			writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/pixel.gif");
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			writer.RenderEndTag();
		}

		protected virtual void RenderFormList(HtmlTextWriter writer)
		{
			if ((Forms.First != null) && (Forms.First.Next != null))	// only show if there are at least two forms
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				writer.RenderBeginTag(HtmlTextWriterTag.Table);

				writer.RenderBeginTag(HtmlTextWriterTag.Tr);

				foreach (IFormInterface form in Forms)
				{
					RenderDummyDimension(writer, "#FFFFFF");

					if (form == Forms.GetTopmostForm())
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "selectedformtitle");
						writer.RenderBeginTag(HtmlTextWriterTag.Td);

						writer.RenderBeginTag(HtmlTextWriterTag.Center);

						writer.AddAttribute(HtmlTextWriterAttribute.Class, "selectedformtitle");
						writer.RenderBeginTag(HtmlTextWriterTag.Font);

						writer.RenderBeginTag(HtmlTextWriterTag.B);

						writer.Write("&nbsp;" + HttpUtility.HtmlEncode(TruncateTitle(GetFormTitle(form), 20)) + "&nbsp;");

						writer.RenderEndTag();
						writer.RenderEndTag();
						writer.RenderEndTag();
						writer.RenderEndTag();
					}
					else
					{
						string linkScript = "Submit('" + GetFormChangeUri(Context, form) + "',event);";

						writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "formtitle");
						writer.AddAttribute(HtmlTextWriterAttribute.Onclick, linkScript);
						writer.AddAttribute(HtmlTextWriterAttribute.Alt, HttpUtility.HtmlEncode(GetFormTitle(form)));
						writer.RenderBeginTag(HtmlTextWriterTag.Td);

						writer.RenderBeginTag(HtmlTextWriterTag.Center);

						writer.AddAttribute(HtmlTextWriterAttribute.Class, "formtitle");
						writer.RenderBeginTag(HtmlTextWriterTag.Font);
						
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "formtitle");
						writer.AddAttribute(HtmlTextWriterAttribute.Onclick, linkScript);
						writer.RenderBeginTag(HtmlTextWriterTag.Div);

						writer.Write("&nbsp;" + HttpUtility.HtmlEncode(TruncateTitle(GetFormTitle(form), 20)) + "&nbsp;");

						writer.RenderEndTag();
						writer.RenderEndTag();
						writer.RenderEndTag();
						writer.RenderEndTag();
					}

					RenderDummyDimension(writer, "#808080");
				}

				writer.RenderEndTag();
				writer.RenderEndTag();
			}
		}

		protected virtual void RenderDepthGauge(HtmlTextWriter writer)
		{
			if (Forms.First != null)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "depthgauge");
				writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				if (Forms.First != null)
					writer.AddAttribute(HtmlTextWriterAttribute.Alt, HttpUtility.HtmlEncode(GetFormTitle(Forms.GetTopmostForm())));
				writer.RenderBeginTag(HtmlTextWriterTag.Td);

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "depthgauge");
				writer.RenderBeginTag(HtmlTextWriterTag.Font);

				int truncateAt = 20;
				for (int i = 0; i < Forms.First.Forms.Count; i++)
				{
					if (i > 0)
						writer.Write("&nbsp;&gt;&nbsp;");
					if (i == Forms.First.Forms.Count - 1)
					{
						truncateAt = 80;
						writer.RenderBeginTag(HtmlTextWriterTag.B);
					}
					writer.Write(HttpUtility.HtmlEncode(TruncateTitle(GetFormTitle(Forms.First.Forms[i]), truncateAt)));
				}
				// Assumption: there will never be 0 forms in a form stack
				writer.RenderEndTag();	// B

				writer.RenderEndTag();	// font
				writer.RenderEndTag();	// td
			}
		}

		protected virtual void RenderStatusButton(HtmlTextWriter writer, string altText, string imageUri, string clickCode)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "statusbutton");
			writer.AddAttribute(HtmlTextWriterAttribute.Src, imageUri);
			writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clickCode);
			writer.AddAttribute(HtmlTextWriterAttribute.Alt, altText);
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			writer.AddAttribute(HtmlTextWriterAttribute.Height, "16");
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();
		}

		protected virtual void RenderWarningsButton(HtmlTextWriter writer)
		{
			IWebFormInterface form = Forms.GetTopmostForm() as IWebFormInterface;
			if ((form != null) && (form.ErrorList != null) && HasNonAborts(form.ErrorList))
				RenderStatusButton(writer, Strings.Get("FormLoadWarningsButtonAlt"), @"images/formwarning.png", String.Format(@"Submit('{0}?WarningForFormID={1}',event)", (string)Context.Session["DefaultPage"], ((IWebElement)form).ID));
		}

		protected virtual void RenderUpdateButton(HtmlTextWriter writer)
		{
			RenderStatusButton(writer, Strings.Get("UpdateButtonAlt"), @"images/updateform.png", "Submit('" + (string)Context.Session["DefaultPage"] + "',event)");
		}

		protected virtual void RenderStatusButtons(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
			writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			RenderWarningsButton(writer);
			RenderUpdateButton(writer);

			writer.RenderEndTag();	// td
		}

		protected virtual void RenderStatusPanel(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "statuspanel");
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			RenderDepthGauge(writer);
			RenderStatusButtons(writer);

			writer.RenderEndTag();	// TR
			writer.RenderEndTag(); // TABLE
		}

		/// <summary> Returns true if there are no items in the list, or every item in the list is an abort. </summary>
		private static bool HasNonAborts(ErrorList list)
		{
			foreach (Exception exception in list)
				if (!(exception is AbortException))
					return true;
			return false;
		}

		public virtual void RenderErrorsList(HtmlTextWriter writer)
		{
			if (HasNonAborts(_errorList))
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "errorlist");
				writer.AddAttribute(HtmlTextWriterAttribute.Id, "errorlist");
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "HideError(this)");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);


				bool first = true;
				foreach (Exception exception in _errorList)
				{
					if (!(exception is AbortException))
					{
						if (first)
							first = false;
						else
							writer.Write("<br>");
						ShowErrorsForm.RenderError(writer, exception);
					}
				}

				writer.RenderEndTag();

				_errorList.Clear();
			}
		}

		public virtual void RenderRenderError(HtmlTextWriter writer, Exception exception)
		{
			writer.RenderBeginTag(HtmlTextWriterTag.Br);
			writer.RenderEndTag();

			writer.Write(HttpUtility.HtmlEncode(Strings.Get("RenderErrorIntro")));
			ShowErrorsForm.RenderError(writer, exception);

			writer.RenderBeginTag(HtmlTextWriterTag.Br);
			writer.RenderEndTag();

			_errorList.Clear();
		}

		public event RenderHandler OnBeforeRenderBody;

		protected virtual void BeforeRenderBody(HttpContext context)
		{
			if (OnBeforeRenderBody != null)
				OnBeforeRenderBody(context);
		}

		public event RenderHandler OnAfterRenderBody;

		protected virtual void AfterRenderBody(HttpContext context)
		{
			if (OnAfterRenderBody != null)
				OnAfterRenderBody(context);
		}

		public virtual void Render(HttpContext context)
		{
			_context = context;
			try
			{
				HtmlTextWriter writer = new HtmlTextWriter(context.Response.Output);
				try
				{
					try
					{
						BeforeRenderBody(context);
						RenderFormList(writer);
						RenderStatusPanel(writer);
						RenderErrorsList(writer);
					
						IFormInterface form = Forms.GetTopmostForm();
						if (form != null)
							((IWebElement)form).Render(writer);
						AfterRenderBody(context);
					}
					catch (Exception exception)
					{
						RenderRenderError(writer, exception);
					}
				}
				finally
				{
					writer.Close();
				}
			}
			finally
			{
				_context = null;
			}
		}

		// Help

		//  TODO: Context help support for the web client.  Could possibly create links such as (ms-its:http://www.helpware.net/htmlhelp/help.chm::/masterfile.htm)

		public override void InvokeHelp(INode sender, string helpKeyword, HelpKeywordBehavior helpKeywordBehavior, string helpString)
		{
			throw new WebClientException(WebClientException.Codes.ContextHelpNotSupported);
		}

		// Utility routines

		public static void RenderDummyImage(HtmlTextWriter writer, string width, string height)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Height, height);
			writer.AddAttribute(HtmlTextWriterAttribute.Width, width);
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();
		}

		public static Session Get(INode node)
		{
			return (Session)node.HostNode.Session;
		}

		/// <summary> Gets a (relative) link URL for an action-based post-back. </summary>
		public static string GetActionLink(HttpContext context, string iD)
		{
			// TODO: find way to avoid hard-coding this
			return String.Format("Submit('{0}?ActionID={1}',event)", (string)context.Session["DefaultPage"], iD);
		}

		/// <summary> Returns true if the request is an action for the given ID. </summary>
		public static bool IsActionLink(HttpContext context, string iD)
		{
			string actionString = context.Request.QueryString["ActionID"];
			return (actionString != null) && (actionString == iD);
		}

		private static int _nextID = 0;

		public static string GenerateID()
		{
			_nextID++;
			return _nextID.GetHashCode().ToString();
		}

		/// <summary> Truncates the given text with an "..." if it exceeds the specified max length. </summary>
		public static string TruncateTitle(string title, int maxLength)
		{
			if (title.Length > maxLength)
				return title.Substring(0, maxLength - 3) + "...";
			else
				return title;
		}

		/// <summary> Gets some title for a form (even if the form's text is blank). </summary>
		public static string GetFormTitle(IFormInterface form)
		{
			if (form.Text == String.Empty)
				return Strings.Get("EmptyTitle");
			else
				return RemoveAccellerator(form.Text);
		}

		/// <summary> Removes "&amp;" from text, which are used in the windows client to show hotkeys. </summary>
		public static string RemoveAccellerator(string text)
		{
			return 
				(
					text.Length > 0 
						? 
						(
							text[0] == '~' 
								? text.Substring(1) 
								: TitleUtility.RemoveAccellerators(text)
						)
						: String.Empty
				);
		}

		public static string GetFormChangeUri(HttpContext context, IFormInterface form)
		{
			return (string)context.Session["DefaultPage"] + "?FormID=" + ((IWebElement)form).ID;
		}
	}

	public delegate void RenderHandler(HttpContext AContext);

	public class ShowErrorsForm : FormInterface
	{
		public ShowErrorsForm(string message)
		{
			Text = message;
		}

		private ErrorList _errors;
		public ErrorList Errors
		{
			get { return _errors; }
			set { _errors = value; }
		}

		public static void RenderError(HtmlTextWriter writer, Exception exception)
		{
			if (!(exception is AbortException))
			{
				// Render hidden verbose error
				writer.AddAttribute(HtmlTextWriterAttribute.Style, "display: none;");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "error");
				writer.RenderBeginTag(HtmlTextWriterTag.Font);

				writer.RenderBeginTag(HtmlTextWriterTag.Pre);
				writer.Write(HttpUtility.HtmlEncode(ExceptionUtility.DetailedDescription(exception)));
				writer.RenderEndTag();

				writer.RenderEndTag();	// FONT
				writer.RenderEndTag(); // DIV

				// Render visible concise error
				writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				writer.RenderBeginTag(HtmlTextWriterTag.Table);

				writer.RenderBeginTag(HtmlTextWriterTag.Tr);

				writer.RenderBeginTag(HtmlTextWriterTag.Td);

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "error");
				writer.RenderBeginTag(HtmlTextWriterTag.Font);

				writer.RenderBeginTag(HtmlTextWriterTag.Pre);
				writer.Write(HttpUtility.HtmlEncode(ExceptionUtility.BriefDescription(exception)));
				writer.RenderEndTag();

				writer.RenderEndTag();	// FONT
				writer.RenderEndTag();	// TD

				writer.RenderBeginTag(HtmlTextWriterTag.Td);

				writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "button");
				writer.AddAttribute(HtmlTextWriterAttribute.Value, Strings.Get("ErrorDetailsButtonText"), true);
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "ShowErrorDetail(GetParentTable(this))");
				writer.RenderBeginTag(HtmlTextWriterTag.Input);
				writer.RenderEndTag();
				
				writer.RenderEndTag();	// TD
				writer.RenderEndTag();	// TR
				writer.RenderEndTag();	// TABLE
			}
		}

		protected override void InternalRender(HtmlTextWriter writer)
		{
			base.InternalRender(writer);
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "error");
			writer.RenderBeginTag(HtmlTextWriterTag.Font);

			foreach (Exception error in _errors)
			{
				RenderError(writer, error);
				writer.RenderBeginTag(HtmlTextWriterTag.Br);
				writer.RenderEndTag();
			}

			writer.RenderEndTag();
		}
	}

}
