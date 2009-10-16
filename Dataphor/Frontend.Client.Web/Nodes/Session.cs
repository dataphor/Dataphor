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
		public const string CImageCachePath = @"DataphorWebClientImageCache";

		public Session(DataSession ASession, bool AOwnsConnection) : base(ASession, AOwnsConnection)
		{
			FImageCache = new ImageCache(Path.GetTempPath() + CImageCachePath, this);
			Forms.OnAdded += new FormsHandler(FormAdded);
			Forms.OnRemoved += new FormsHandler(FormRemoved);
		}

		protected override void Dispose(bool ADisposed)
		{
			try
			{
				CloseAllForms();
			}
			finally
			{
				try
				{
					base.Dispose(ADisposed);
				}
				finally
				{
					if (FImageCache != null)
					{
						FImageCache.Dispose();
						FImageCache = null;
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

		private HttpContext FContext;
		public HttpContext Context { get { return FContext; } }

		// ImageCache

		private ImageCache FImageCache;

		public ImageCache ImageCache { get { return FImageCache; } }

		public static void AddImageLinkAttribute(HtmlTextWriter AWriter, string AAttributeName, string AImageID)
		{
			if (AImageID != String.Empty)
				AWriter.AddAttribute(AAttributeName, "ViewImage.aspx?ImageID=" + AImageID, false);
		}

		public static void AddImageLinkAttribute(HtmlTextWriter AWriter, HtmlTextWriterAttribute AAttribute, string AImageID)
		{
			if (AImageID != String.Empty)
				AWriter.AddAttribute(AAttribute, "ViewImage.aspx?ImageID=" + AImageID, false);
		}

		// Forms

		private void FormAdded(IFormInterface AForm)
		{
			FFormsByID.Add(((IWebElement)AForm).ID, AForm);
		}

		private void FormRemoved(IFormInterface AForm)
		{
			FFormsByID.Remove(((IWebElement)AForm).ID);
		}

		private Hashtable FFormsByID = new Hashtable();

		public IFormInterface GetForm(string AFormID)
		{
			return (IFormInterface)FFormsByID[AFormID];
		}

		/// <summary> Closes the session's forms and returns true if it is okay to close the session. </summary>
		/// <remarks> Closes all of the forms opened in this session (except for the root form). </remarks>
		public bool PrepareToClose(CloseBehavior ABehavior)
		{
			while (FFormsByID.Count > 1)
			{
				foreach (IFormInterface LForm in Forms)
				{
					if (((FRootFormHost == null) || (FRootFormHost.Children.Count == 0) || (LForm != FRootFormHost.Children[0])) && !LForm.Close(ABehavior))
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
					(FRootFormHost == null) 
						|| (FRootFormHost.Children.Count == 0)
						|| ((IFormInterface)FRootFormHost.Children[0]).Close(CloseBehavior.RejectOrClose)
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

		public virtual void ProcessRequest(HttpContext AContext)
		{
			FContext = AContext;
			try
			{
				ProcessShowFormWarning(AContext);
				try
				{
					PreprocessFormRequest(AContext);
					ProcessFormRequest(AContext);	// this must come before ProcessFormChange()
				}
				finally
				{
					ProcessFormChange(AContext);
				}
			}
			catch (Exception LException)
			{
				ErrorList.Add(LException);
			}
			FContext = null;
		}

		/// <summary> Process this request with the appropriate form. </summary>
		protected virtual void ProcessFormRequest(HttpContext AContext)
		{
			// Propigate the request to the appropriate form
			string LFormIDString = AContext.Request.Form["FormID"];
			if ((LFormIDString != null) && (LFormIDString != String.Empty))
			{
				IFormInterface LForm = GetForm(LFormIDString);
				if (LForm != null)
					LForm.BroadcastEvent(new ProcessRequestEvent(AContext));
			}
		}

		protected virtual void PreprocessFormRequest(HttpContext AContext)
		{
			// Propigate the request to the appropriate form
			string LFormIDString = AContext.Request.Form["FormID"];
			if ((LFormIDString != null) && (LFormIDString != String.Empty))
			{
				IWebFormInterface LForm = (IWebFormInterface)GetForm(LFormIDString);
				if (LForm != null)
					LForm.PreprocessRequest(AContext);
			}
		}

		/// <summary> Change frontmost form if requested to do so. </summary>
		protected virtual void ProcessFormChange(HttpContext AContext)
		{
			string LFormIDString = AContext.Request.QueryString["FormID"];
			if ((LFormIDString != null) && (LFormIDString != String.Empty))
			{
				IFormInterface LForm = GetForm(LFormIDString);
				if (LForm != null)
					Forms.BringToFront(LForm);
			}
		}

		protected virtual void ProcessShowFormWarning(HttpContext AContext)
		{
			string LFormIDString = AContext.Request.QueryString["WarningForFormID"];
			if ((LFormIDString != null) && (LFormIDString != String.Empty))
			{
				IWebFormInterface LForm = GetForm(LFormIDString) as IWebFormInterface;
				if ((LForm != null) && (LForm.ErrorList != null))
					ShowFormWarnings(LForm.ErrorList);
			}
		}

		// Start

		public void SetApplication(string AApplicationID)
		{
			Start(SetApplication(AApplicationID, "Web"));
		}

		private IHost FRootFormHost;
		public IHost RootFormHost { get { return FRootFormHost; } }

		private void RootFormHostDisposed(object ASender, EventArgs AArgs)
		{
			FRootFormHost = null;
		}

		public void Start(string ADocument)
		{
			// Close any existing forms
			if ((FRootFormHost != null) && (!CloseAllForms()))
				throw new AbortException();

			FRootFormHost = CreateHost();
			try
			{		
				FRootFormHost.Disposed += new EventHandler(RootFormHostDisposed);				   
				FRootFormHost.NextRequest = new Request(ADocument);
				LoadNextForm();
			}
			catch
			{
				FRootFormHost.Dispose();
				FRootFormHost = null;
				throw;
			}
		}

		private void LoadNextForm()
		{
			if ((FRootFormHost != null) && (FRootFormHost.NextRequest != null))
			{
				IWebFormInterface LNewForm = (IWebFormInterface)NodeTypeTable.CreateInstance("FormInterface");
				LNewForm.OnClosing += new CancelEventHandler(MainFormClosing);
				LNewForm.OnClosed += new EventHandler(MainFormClosed);
				LNewForm = (IWebFormInterface)FRootFormHost.LoadNext(LNewForm);
				FRootFormHost.Open();
				LNewForm.Show();
			}
		}

		private void MainFormClosed(object ASender, EventArgs AArgs)
		{
			LoadNextForm();
		}

		private void MainFormClosing(object ASender, CancelEventArgs AArgs)
		{
			AArgs.Cancel = AArgs.Cancel || !PrepareToClose(CloseBehavior.AcceptOrClose);
		}

		// Client.Session

		public override IHost CreateHost()
		{
			Host LHost = new Host(this);
			LHost.OnDeserializationErrors += new DeserializationErrorsHandler(DeserializationErrors);
			return LHost;
		}

		private void DeserializationErrors(IHost AHost, ErrorList AErrorList)
		{
			if ((AHost != null) && (AHost.Children.Count > 0))
			{
				IFormInterface LFormInterface = AHost.Children[0] as IFormInterface;
				if (LFormInterface == null)
					LFormInterface = (IFormInterface)AHost.Children[0].FindParent(typeof(IFormInterface));

				if (LFormInterface != null)
				{
					LFormInterface.EmbedErrors(AErrorList);
					return;
				}
			}
			ShowFormWarnings(AErrorList);
		}

		public void ShowFormWarnings(ErrorList AErrors)
		{
			IHost LHost = CreateHost();
			try
			{
				ShowErrorsForm LForm = new ShowErrorsForm(Strings.Get("CFormLoadWarningsCaption"));
				try
				{
					LHost.Children.Add(LForm);
					LForm.Errors = AErrors;
					LHost.Open();
					LForm.Show();
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
		}

		// ErrorList

		private ErrorList FErrorList = new ErrorList();
		public ErrorList ErrorList
		{
			get { return FErrorList; }
		}

		// Render

		/// <summary> Renders a on pixel table dimension. </summary>
		protected void RenderDummyDimension(HtmlTextWriter AWriter, string AColor)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "1");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Bgcolor, AColor);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "1");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "1");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, "");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/pixel.gif");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();
		}

		protected virtual void RenderFormList(HtmlTextWriter AWriter)
		{
			if ((Forms.First != null) && (Forms.First.Next != null))	// only show if there are at least two forms
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

				AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

				foreach (IFormInterface LForm in Forms)
				{
					RenderDummyDimension(AWriter, "#FFFFFF");

					if (LForm == Forms.GetTopmostForm())
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "selectedformtitle");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

						AWriter.RenderBeginTag(HtmlTextWriterTag.Center);

						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "selectedformtitle");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Font);

						AWriter.RenderBeginTag(HtmlTextWriterTag.B);

						AWriter.Write("&nbsp;" + HttpUtility.HtmlEncode(TruncateTitle(GetFormTitle(LForm), 20)) + "&nbsp;");

						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
					}
					else
					{
						string LLinkScript = "Submit('" + GetFormChangeUri(Context, LForm) + "',event);";

						AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "formtitle");
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, LLinkScript);
						AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, HttpUtility.HtmlEncode(GetFormTitle(LForm)));
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

						AWriter.RenderBeginTag(HtmlTextWriterTag.Center);

						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "formtitle");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Font);

						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "formtitle");
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, LLinkScript);
						AWriter.RenderBeginTag(HtmlTextWriterTag.Div);

						AWriter.Write("&nbsp;" + HttpUtility.HtmlEncode(TruncateTitle(GetFormTitle(LForm), 20)) + "&nbsp;");

						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
						AWriter.RenderEndTag();
					}

					RenderDummyDimension(AWriter, "#808080");
				}

				AWriter.RenderEndTag();
				AWriter.RenderEndTag();
			}
		}

		protected virtual void RenderDepthGauge(HtmlTextWriter AWriter)
		{
			if (Forms.First != null)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "depthgauge");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				if (Forms.First != null)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, HttpUtility.HtmlEncode(GetFormTitle(Forms.GetTopmostForm())));
				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "depthgauge");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Font);

				int LTruncateAt = 20;
				for (int i = 0; i < Forms.First.Forms.Count; i++)
				{
					if (i > 0)
						AWriter.Write("&nbsp;&gt;&nbsp;");
					if (i == Forms.First.Forms.Count - 1)
					{
						LTruncateAt = 80;
						AWriter.RenderBeginTag(HtmlTextWriterTag.B);
					}
					AWriter.Write(HttpUtility.HtmlEncode(TruncateTitle(GetFormTitle(Forms.First.Forms[i]), LTruncateAt)));
				}
				// Assumption: there will never be 0 forms in a form stack
				AWriter.RenderEndTag();	// B

				AWriter.RenderEndTag();	// font
				AWriter.RenderEndTag();	// td
			}
		}

		protected virtual void RenderStatusButton(HtmlTextWriter AWriter, string AAltText, string AImageUri, string AClickCode)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "statusbutton");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, AImageUri);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, AClickCode);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Alt, AAltText);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "16");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();
		}

		protected virtual void RenderWarningsButton(HtmlTextWriter AWriter)
		{
			IWebFormInterface LForm = Forms.GetTopmostForm() as IWebFormInterface;
			if ((LForm != null) && (LForm.ErrorList != null) && HasNonAborts(LForm.ErrorList))
				RenderStatusButton(AWriter, Strings.Get("FormLoadWarningsButtonAlt"), @"images/formwarning.png", String.Format(@"Submit('{0}?WarningForFormID={1}',event)", (string)Context.Session["DefaultPage"], ((IWebElement)LForm).ID));
		}

		protected virtual void RenderUpdateButton(HtmlTextWriter AWriter)
		{
			RenderStatusButton(AWriter, Strings.Get("UpdateButtonAlt"), @"images/updateform.png", "Submit('" + (string)Context.Session["DefaultPage"] + "',event)");
		}

		protected virtual void RenderStatusButtons(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "right");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			RenderWarningsButton(AWriter);
			RenderUpdateButton(AWriter);

			AWriter.RenderEndTag();	// td
		}

		protected virtual void RenderStatusPanel(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "statuspanel");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			RenderDepthGauge(AWriter);
			RenderStatusButtons(AWriter);

			AWriter.RenderEndTag();	// TR
			AWriter.RenderEndTag(); // TABLE
		}

		/// <summary> Returns true if there are no items in the list, or every item in the list is an abort. </summary>
		private static bool HasNonAborts(ErrorList AList)
		{
			foreach (Exception LException in AList)
				if (!(LException is AbortException))
					return true;
			return false;
		}

		public virtual void RenderErrorsList(HtmlTextWriter AWriter)
		{
			if (HasNonAborts(FErrorList))
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "errorlist");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);

				bool LFirst = true;
				foreach (Exception LException in FErrorList)
				{
					if (!(LException is AbortException))
					{
						if (LFirst)
							LFirst = false;
						else
							AWriter.Write("<br>");
						ShowErrorsForm.RenderError(AWriter, LException);
					}
				}

				AWriter.RenderEndTag();

				FErrorList.Clear();
			}
		}

		public virtual void RenderRenderError(HtmlTextWriter AWriter, Exception AException)
		{
			AWriter.RenderBeginTag(HtmlTextWriterTag.Br);
			AWriter.RenderEndTag();

			AWriter.Write(HttpUtility.HtmlEncode(Strings.Get("RenderErrorIntro")));
			ShowErrorsForm.RenderError(AWriter, AException);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Br);
			AWriter.RenderEndTag();

			FErrorList.Clear();
		}

		public event RenderHandler OnBeforeRenderBody;

		protected virtual void BeforeRenderBody(HttpContext AContext)
		{
			if (OnBeforeRenderBody != null)
				OnBeforeRenderBody(AContext);
		}

		public event RenderHandler OnAfterRenderBody;

		protected virtual void AfterRenderBody(HttpContext AContext)
		{
			if (OnAfterRenderBody != null)
				OnAfterRenderBody(AContext);
		}

		public virtual void Render(HttpContext AContext)
		{
			FContext = AContext;
			try
			{
				HtmlTextWriter LWriter = new HtmlTextWriter(AContext.Response.Output);
				try
				{
					try
					{
						BeforeRenderBody(AContext);
						RenderFormList(LWriter);
						RenderStatusPanel(LWriter);
						RenderErrorsList(LWriter);
					
						IFormInterface LForm = Forms.GetTopmostForm();
						if (LForm != null)
							((IWebElement)LForm).Render(LWriter);
						AfterRenderBody(AContext);
					}
					catch (Exception LException)
					{
						RenderRenderError(LWriter, LException);
					}
				}
				finally
				{
					LWriter.Close();
				}
			}
			finally
			{
				FContext = null;
			}
		}

		// Help

		//  TODO: Context help support for the web client.  Could possibly create links such as (ms-its:http://www.helpware.net/htmlhelp/help.chm::/masterfile.htm)

		public override void InvokeHelp(INode ASender, string AHelpKeyword, HelpKeywordBehavior AHelpKeywordBehavior, string AHelpString)
		{
			throw new WebClientException(WebClientException.Codes.ContextHelpNotSupported);
		}

		// Utility routines

		public static void RenderDummyImage(HtmlTextWriter AWriter, string AWidth, string AHeight)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, AHeight);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, AWidth);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();
		}

		public static Session Get(INode ANode)
		{
			return (Session)ANode.HostNode.Session;
		}

		/// <summary> Gets a (relative) link URL for an action-based post-back. </summary>
		public static string GetActionLink(HttpContext AContext, string AID)
		{
			// TODO: find way to avoid hard-coding this
			return String.Format("Submit('{0}?ActionID={1}',event)", (string)AContext.Session["DefaultPage"], AID);
		}

		/// <summary> Returns true if the request is an action for the given ID. </summary>
		public static bool IsActionLink(HttpContext AContext, string AID)
		{
			string LActionString = AContext.Request.QueryString["ActionID"];
			return (LActionString != null) && (LActionString == AID);
		}

		private static int FNextID = 0;

		public static string GenerateID()
		{
			FNextID++;
			return FNextID.GetHashCode().ToString();
		}

		/// <summary> Truncates the given text with an "..." if it exceeds the specified max length. </summary>
		public static string TruncateTitle(string ATitle, int AMaxLength)
		{
			if (ATitle.Length > AMaxLength)
				return ATitle.Substring(0, AMaxLength - 3) + "...";
			else
				return ATitle;
		}

		/// <summary> Gets some title for a form (even if the form's text is blank). </summary>
		public static string GetFormTitle(IFormInterface AForm)
		{
			if (AForm.Text == String.Empty)
				return Strings.Get("EmptyTitle");
			else
				return RemoveAccellerator(AForm.Text);
		}

		/// <summary> Removes "&amp;" from text, which are used in the windows client to show hotkeys. </summary>
		public static string RemoveAccellerator(string AText)
		{
			return 
				(
					AText.Length > 0 
						? 
						(
							AText[0] == '~' 
								? AText.Substring(1) 
								: TitleUtility.RemoveAccellerators(AText)
						)
						: String.Empty
				);
		}

		public static string GetFormChangeUri(HttpContext AContext, IFormInterface AForm)
		{
			return (string)AContext.Session["DefaultPage"] + "?FormID=" + ((IWebElement)AForm).ID;
		}
	}

	public delegate void RenderHandler(HttpContext AContext);

	public class ShowErrorsForm : FormInterface
	{
		public ShowErrorsForm(string AMessage)
		{
			Text = AMessage;
		}

		private ErrorList FErrors;
		public ErrorList Errors
		{
			get { return FErrors; }
			set { FErrors = value; }
		}

		public static void RenderError(HtmlTextWriter AWriter, Exception AException)
		{
			if (!(AException is AbortException))
			{
				// Render hidden verbose error
				AWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none;");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "error");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Font);

				AWriter.RenderBeginTag(HtmlTextWriterTag.Pre);
				AWriter.Write(HttpUtility.HtmlEncode(ExceptionUtility.DetailedDescription(AException)));
				AWriter.RenderEndTag();

				AWriter.RenderEndTag();	// FONT
				AWriter.RenderEndTag(); // DIV

				// Render visible concise error
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

				AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "error");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Font);

				AWriter.RenderBeginTag(HtmlTextWriterTag.Pre);
				AWriter.Write(HttpUtility.HtmlEncode(ExceptionUtility.BriefDescription(AException)));
				AWriter.RenderEndTag();

				AWriter.RenderEndTag();	// FONT
				AWriter.RenderEndTag();	// TD

				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "button");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Value, Strings.Get("ErrorDetailsButtonText"), true);
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "ShowErrorDetail(GetParentTable(this))");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
				AWriter.RenderEndTag();
				
				AWriter.RenderEndTag();	// TD
				AWriter.RenderEndTag();	// TR
				AWriter.RenderEndTag();	// TABLE
			}
		}

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			base.InternalRender(AWriter);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "error");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Font);

			foreach (Exception LError in FErrors)
			{
				RenderError(AWriter, LError);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Br);
				AWriter.RenderEndTag();
			}

			AWriter.RenderEndTag();
		}
	}

}
