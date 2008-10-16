/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Web.UI;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	/// <summary> FormInterface element node. </summary>
	/// <remarks> A host for a root interface node. </remarks>
	[PublishAs("Interface")]
	public class FormInterface : Interface, IWebFormInterface, IWebMenu, IWebToolbar
	{
		// TODO: Move these constants into Frontend.Client
		public const int CMarginLeft = 4;
		public const int CMarginRight = 4;
		public const int CMarginTop = 6;
		public const int CMarginBottom = 6;

		public FormInterface() : base()
		{
			FToolBar = new ToolBar();
			FMainMenu = new MainMenu();

			FFormMenuItem = new MenuItem();
			FFormMenuItem.Text = Strings.Get("FormMenuItemText");
			FMainMenu.Items.Insert(0, FFormMenuItem);

			// TODO: Images for accept/reject/close like win client

			FAcceptMenuItem = new MenuItem();
			FAcceptMenuItem.OnClick += new EventHandler(AcceptClick);
			FAcceptMenuItem.Text = Strings.Get("AcceptText");
			FAcceptMenuItem.Hint = Strings.Get("AcceptHint");

			FRejectMenuItem = new MenuItem();
			FRejectMenuItem.OnClick += new EventHandler(RejectClick);
			FRejectMenuItem.Text = Strings.Get("RejectText");
			FRejectMenuItem.Hint = Strings.Get("RejectHint");

			FAcceptButton = new ToolBarButton();
			FAcceptButton.OnClick += new EventHandler(AcceptClick);
			FAcceptButton.Text = FAcceptMenuItem.Text;
			FAcceptButton.Hint = FAcceptMenuItem.Hint;

			FRejectButton = new ToolBarButton();
			FRejectButton.OnClick += new EventHandler(RejectClick);
			FRejectButton.Text = FRejectMenuItem.Text;
			FRejectButton.Hint = FRejectMenuItem.Hint;

			FCloseMenuItem = new MenuItem();
			FCloseMenuItem.OnClick += new EventHandler(CloseClick);
			FCloseMenuItem.Text = Strings.Get("CloseText");
			FCloseMenuItem.Hint = Strings.Get("CloseHint");

			FCloseButton = new ToolBarButton();
			FCloseButton.OnClick += new EventHandler(CloseClick);
			FCloseButton.Text = FCloseMenuItem.Text;
			FCloseButton.Hint = FCloseMenuItem.Hint;
		}

		// ToolBar

		private ToolBar FToolBar = new ToolBar();
		public ToolBar ToolBar
		{
			get { return FToolBar; }
		}

		// MainMenu

		private MainMenu FMainMenu = new MainMenu();
		public MainMenu MainMenu
		{
			get { return FMainMenu; }
		}

		// IWebMenu

		public MenuItemList Items
		{
			get { return FMainMenu.Items; }
		}

		// IsAcceptReject

		public bool IsAcceptReject
		{
			get 
			{ 
				return
					(FMode != FormMode.None) ||
					(
						(MainSource != null) &&
						(
							(MainSource.DataView.State == DAE.Client.DataSetState.Edit) || 
							(MainSource.DataView.State == DAE.Client.DataSetState.Insert)
						)
					);
			}
		}

		private void CloseClick(object ASender, EventArgs AArgs)
		{
			Close(CloseBehavior.AcceptOrClose);
		}
		
		private void AcceptClick(object ASender, EventArgs AArgs)
		{
			Close(CloseBehavior.AcceptOrClose);
		}
		
		private void RejectClick(object ASender, EventArgs AArgs)
		{
			Close(CloseBehavior.RejectOrClose);
		}
		
		// IFormInterface

		private FormInterfaceHandler FOnAcceptForm;
		private FormInterfaceHandler FOnRejectForm;

		private FormMode FMode;
		public FormMode Mode { get { return FMode; } }

		public void Show()
		{
			Show(FormMode.None);
		}

		public void Show(FormMode AMode)
		{
			FMode = AMode;
			HostNode.Session.Forms.Add(this);
			BroadcastEvent(new FormShownEvent());
		}

		public void Show(IFormInterface AParentForm, FormInterfaceHandler AOnAcceptForm, FormInterfaceHandler AOnRejectForm, FormMode AMode)
		{
			if ((FOnAcceptForm != null) || (FOnRejectForm != null))
				throw new ClientException(ClientException.Codes.FormAlreadyModal);
			FOnAcceptForm = AOnAcceptForm;
			FOnRejectForm = AOnRejectForm;
			FMode = AMode;
			try
			{
				HostNode.Session.Forms.AddModal(this, AParentForm);
			}
			catch
			{
				EndModal();
				throw;
			}
			BroadcastEvent(new FormShownEvent());
		}

		// ErrorList - DFD loading errors

		private ErrorList FErrorList;
		public ErrorList ErrorList { get { return FErrorList; } }

		public virtual void EmbedErrors(ErrorList AErrorList)
		{
			FErrorList = AErrorList;
		}

		// Closing

		public event CancelEventHandler OnClosing;	// Named to be consistent with the Windows forms event names

		public virtual void Closing(CancelEventArgs AArgs)
		{
			if (OnClosing != null)
				OnClosing(this, AArgs);
		}

		// Closed

		public event EventHandler OnClosed;

		public virtual void Closed(EventArgs AArgs)
		{
			if (OnClosed != null)
				OnClosed(this, AArgs);
		}

		// Close

		public bool Close(CloseBehavior ABehavior)
		{
			CancelEventArgs LArgs = new CancelEventArgs(false);
			Closing(LArgs);
			if (LArgs.Cancel)
				return false;
	
			if (IsAcceptReject)
				if (ABehavior == CloseBehavior.AcceptOrClose)
					PostChanges();
				else
					CancelChanges();

			HostNode.Session.Forms.Remove(this);

			try
			{
				if (IsAcceptReject)
				{
					if (ABehavior == CloseBehavior.AcceptOrClose)
						AcceptForm();
					else
						RejectForm();
				}
			}
			finally
			{
				if (ABehavior == CloseBehavior.AcceptOrClose)
				{
					if (FOnAcceptForm != null)
						FOnAcceptForm(this);
				}
				else
				{
					if (FOnRejectForm != null)
						FOnRejectForm(this);
				}
				EndModal();
			}

			Closed(EventArgs.Empty);

			if ((HostNode != null) && (HostNode.NextRequest == null))
				HostNode.Dispose();
			
			return true;
		}

		private void EndModal()
		{
			FMode = FormMode.None;
			FOnAcceptForm = null;
			FOnRejectForm = null;
		}

		private void AcceptForm()
		{
			if ((FMode == FormMode.Delete) && (MainSource != null))
				MainSource.DataView.Delete();
		}

		private void RejectForm()
		{
			try
			{
				if (((FMode == FormMode.Edit) || (FMode == FormMode.Insert)) && (MainSource != null))
					MainSource.DataView.Cancel();
			}
			catch (Exception LException)
			{
				WebSession.ErrorList.Add(LException);
				// don't rethrow
			}
		}
		
		// Enable / Disable

		private int FDisableCount;

		public virtual void Enable()
		{
			FDisableCount--;
		}

		public virtual void Disable(IFormInterface AForm)
		{
			FDisableCount++;
		}

		public virtual bool GetEnabled()
		{
			return FDisableCount <= 0;
		}

		// LogicalClock

		private int FLogicalClock = 1;
		public int LogicalClock
		{
			get { return FLogicalClock; }
		}

		public void IncrementLogicalClock()
		{
			FLogicalClock++;
		}

		private int GetContextClock(HttpContext AContext)
		{
			string LLogicalClockString = AContext.Request["LogicalClock"];
			if ((LLogicalClockString == null) || (LLogicalClockString == String.Empty))
				return FLogicalClock - 1;
			else
			{
				try
				{
					return Int32.Parse(LLogicalClockString);
				}
				catch
				{
					return FLogicalClock - 1;
				}
			}
		}

		public virtual void PreprocessRequest(HttpContext AContext)	// this is not part of an IWebPrehandler interface
		{
			IncrementLogicalClock();
			if (GetContextClock(AContext) == (FLogicalClock - 1))	// If logical clock was missing or incorrect, do not apply request
			{
				PreprocessRequestEvent LEvent = new PreprocessRequestEvent(AContext);
				BroadcastEvent(LEvent);
				if (LEvent.AnyErrors)
					throw new AbortException();
			}
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext AContext)
		{
			return 
				base.ProcessRequest(AContext)
					|| FMainMenu.ProcessRequest(AContext) 
					|| FToolBar.ProcessRequest(AContext);
		}

		// Element

		public override int GetDefaultMarginLeft()
		{
			return CMarginLeft;
		}

		public override int GetDefaultMarginRight()
		{
			return CMarginRight;
		}

		public override int GetDefaultMarginTop()
		{
			return CMarginTop;
		}

		public override int GetDefaultMarginBottom()
		{
			return CMarginBottom;
		}

		// IWebElement

		private MenuItem FAcceptMenuItem;
		private MenuItem FRejectMenuItem;
		private MenuItem FCloseMenuItem;

		private ToolBarButton FAcceptButton;
		private ToolBarButton FRejectButton;
		private ToolBarButton FCloseButton;

		private MenuItem FFormMenuItem;
		public MenuItem FormMenuItem { get { return FFormMenuItem; } }

		private void DestroyButton(ToolBarButton AButton)
		{
		}

		private void DestroyMenu(Menu AMenu)
		{
			Children.Remove(AMenu);
			AMenu.Dispose();
		}

		protected virtual void UpdateAcceptReject()
		{
			if (IsAcceptReject)
			{
				if (!FToolBar.Contains(FAcceptButton))
				{
					if (FToolBar.Contains(FCloseButton))
						FToolBar.Remove(FCloseButton);
					if (FMainMenu.Items.Contains(FCloseMenuItem))
						FMainMenu.Items.Remove(FCloseMenuItem);

					FFormMenuItem.Items.Insert(0, FAcceptMenuItem);
					FFormMenuItem.Items.Insert(1, FRejectMenuItem);
					FToolBar.Insert(0, FAcceptButton);
					FToolBar.Insert(1, FRejectButton);
				}
			}
			else
			{
				if (!FToolBar.Contains(FCloseButton))
				{
					if (FToolBar.Contains(FAcceptButton))
						FToolBar.Remove(FAcceptButton);
					if (FToolBar.Contains(FRejectButton))
						FToolBar.Remove(FRejectButton);
					if (FMainMenu.Items.Contains(FAcceptMenuItem))
						FMainMenu.Items.Remove(FAcceptMenuItem);
					if (FMainMenu.Items.Contains(FRejectMenuItem))
						FMainMenu.Items.Remove(FRejectMenuItem);

					FFormMenuItem.Items.Insert(0, FCloseMenuItem);
					FToolBar.Insert(0, FCloseButton);
				}
			}
		}

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			HttpContext LContext = Session.Get(this).Context;

			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Name, "LogicalClock");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Value, FLogicalClock.ToString());
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.RenderEndTag();

			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Name, "FormID");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Value, ID);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.RenderEndTag();

			UpdateAcceptReject();

			FMainMenu.Render(LContext, AWriter);
			FToolBar.Render(LContext, AWriter);

			base.InternalRender(AWriter);
		}
	}

	public class ToolBarButton
	{
		public ToolBarButton() 
		{
			FID = Session.GenerateID();
		}

		// ID

		private string FID;
		public string ID { get { return FID; } }

		// Text

		private string FText = String.Empty;
		public string Text { get { return FText; } set { FText = value; } }

		// Hint

		private string FHint = String.Empty;
		public string Hint { get { return FHint; } set { FHint = value; } }

		// Enabled

		private bool FEnabled = true;
		public bool Enabled { get { return FEnabled; } set { FEnabled = value; } }

		// Visible

		private bool FVisible = true;
		public bool Visible { get { return FVisible; } set { FVisible = value; } }

		// OnClick

		public event EventHandler OnClick;

		public void Render(HttpContext AContext, HtmlTextWriter AWriter)
		{
			if (FVisible)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "toolbarbutton");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "button");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(AContext, FID));
				AWriter.AddAttribute(HtmlTextWriterAttribute.Value, Session.RemoveAccellerator(FText), true);
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, FHint, true);
				if (!FEnabled)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
				AWriter.RenderEndTag();

				AWriter.RenderEndTag();	// TD
			}
		}

		public bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, FID) && FEnabled && (OnClick != null))
			{
				OnClick(this, EventArgs.Empty);
				return true;
			}
			else
				return false;
		}

	}

	public class ToolBar : List
	{
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			if (!(AValue is ToolBarButton))
				throw new WebClientException(WebClientException.Codes.InvalidToolbarObject);
		}

		public new ToolBarButton this[int AIndex]
		{
			get { return (ToolBarButton)base[AIndex]; }
		}

		public void Render(HttpContext AContext, HtmlTextWriter AWriter) 
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "toolbar");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			foreach (ToolBarButton LButton in this)
				LButton.Render(AContext, AWriter);

			AWriter.RenderEndTag();
			AWriter.RenderEndTag();
		}

		public bool ProcessRequest(HttpContext AContext)
		{
			foreach (ToolBarButton LButton in this)
				if (LButton.ProcessRequest(AContext))
					return true;
			return false;
		}
	}

	public class ProcessRequestEvent : NodeEvent
	{
		public ProcessRequestEvent(HttpContext AContext) : base()
		{
			FContext = AContext;
		}

		private HttpContext FContext;
		public HttpContext Context { get { return FContext; } }

		public override void Handle(INode ANode)
		{
			IWebHandler LHandler = ANode as IWebHandler;
			if (LHandler != null)
				IsHandled = IsHandled || LHandler.ProcessRequest(FContext);
		}
	}			

	public class PreprocessRequestEvent : NodeEvent
	{
		public PreprocessRequestEvent(HttpContext AContext) : base()
		{
			FContext = AContext;
		}

		private HttpContext FContext;
		public HttpContext Context { get { return FContext; } }

		private bool FAnyErrors;
		public bool AnyErrors { get { return FAnyErrors; } }

		public override void Handle(INode ANode)
		{
			IWebPrehandler LHandler = ANode as IWebPrehandler;
			if (LHandler != null)
			{
				try
				{
					LHandler.PreprocessRequest(FContext);
				}
				catch (Exception LException)
				{
					FAnyErrors = true;
					IWebElement LWebElement = ANode as IWebElement;
					if (LWebElement == null)
						throw;
					else
						LWebElement.HandleElementException(LException);
				}
			}				
		}
	}			

	public class ShowLinkAction : Client.ShowLinkAction
	{
		protected override void InternalExecute(Alphora.Dataphor.Frontend.Client.INode ASender, Alphora.Dataphor.Frontend.Client.EventParams AParams)
		{
			((Web.Session)HostNode.Session).OnBeforeRenderBody += new RenderHandler(SessionBeforeRender);
		}

		private void SessionBeforeRender(HttpContext AContext)
		{
			((Web.Session)HostNode.Session).OnBeforeRenderBody -= new RenderHandler(SessionBeforeRender);
			HtmlTextWriter LWriter = new HtmlTextWriter(AContext.Response.Output);
			try
			{
				LWriter.AddAttribute("language", "JavaScript");
				LWriter.RenderBeginTag(HtmlTextWriterTag.Script);
				LWriter.Write
				(
					String.Format
					(
						@"open(""{0}"", ""_blank"");",
						URL
					)
				);
				LWriter.RenderEndTag();
			}
			finally
			{
				LWriter.Close();
			}
		}
	}

}
