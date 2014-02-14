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
		public const int MarginLeft = 4;
		public const int MarginRight = 4;
		public const int MarginTop = 6;
		public const int MarginBottom = 6;

		public FormInterface() : base()
		{
			_toolBar = new ToolBar();
			_mainMenu = new MainMenu();

			_formMenuItem = new MenuItem();
			_formMenuItem.Text = Strings.Get("FormMenuItemText");
			_mainMenu.Items.Insert(0, _formMenuItem);

			// TODO: Images for accept/reject/close like win client

			_acceptMenuItem = new MenuItem();
			_acceptMenuItem.OnClick += new EventHandler(AcceptClick);
			_acceptMenuItem.Text = Strings.Get("AcceptText");
			_acceptMenuItem.Hint = Strings.Get("AcceptHint");

			_rejectMenuItem = new MenuItem();
			_rejectMenuItem.OnClick += new EventHandler(RejectClick);
			_rejectMenuItem.Text = Strings.Get("RejectText");
			_rejectMenuItem.Hint = Strings.Get("RejectHint");

			_acceptButton = new ToolBarButton();
			_acceptButton.OnClick += new EventHandler(AcceptClick);
			_acceptButton.Text = _acceptMenuItem.Text;
			_acceptButton.Hint = _acceptMenuItem.Hint;

			_rejectButton = new ToolBarButton();
			_rejectButton.OnClick += new EventHandler(RejectClick);
			_rejectButton.Text = _rejectMenuItem.Text;
			_rejectButton.Hint = _rejectMenuItem.Hint;

			_closeMenuItem = new MenuItem();
			_closeMenuItem.OnClick += new EventHandler(CloseClick);
			_closeMenuItem.Text = Strings.Get("CloseText");
			_closeMenuItem.Hint = Strings.Get("CloseHint");

			_closeButton = new ToolBarButton();
			_closeButton.OnClick += new EventHandler(CloseClick);
			_closeButton.Text = _closeMenuItem.Text;
			_closeButton.Hint = _closeMenuItem.Hint;
		}

		// ToolBar

		private ToolBar _toolBar = new ToolBar();
		public ToolBar ToolBar
		{
			get { return _toolBar; }
		}

		// MainMenu

		private MainMenu _mainMenu = new MainMenu();
		public MainMenu MainMenu
		{
			get { return _mainMenu; }
		}

		// IWebMenu

		public MenuItemList Items
		{
			get { return _mainMenu.Items; }
		}

		private bool _forceAcceptReject;

		public bool ForceAcceptReject
		{
			get { return _forceAcceptReject; }
			set { _forceAcceptReject = value; }
		}

		// IsAcceptReject

		public bool IsAcceptReject
		{
			get 
			{
				return
					AcceptEnabled
					&&
					(
						ForceAcceptReject
						|| (_mode != FormMode.None)
						||
						(
							(MainSource != null) &&
							(MainSource.DataView != null) &&
							(
								(MainSource.DataView.State == DAE.Client.DataSetState.Edit) ||
								(MainSource.DataView.State == DAE.Client.DataSetState.Insert)
							)
						)
					);
			}
		}

		private bool _acceptEnabled = true;
		public bool AcceptEnabled
		{
			get { return _acceptEnabled; }
			set { _acceptEnabled = value; }
		}

		// OnBeforeAccept

		private IAction _onBeforeAccept;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed before the form is accepted.")]
		public IAction OnBeforeAccept
		{
			get { return _onBeforeAccept; }
			set
			{
				if (_onBeforeAccept != value)
				{
					if (_onBeforeAccept != null)
						_onBeforeAccept.Disposed -= new EventHandler(OnBeforeAcceptDisposed);
					_onBeforeAccept = value;
					if (_onBeforeAccept != null)
						_onBeforeAccept.Disposed += new EventHandler(OnBeforeAcceptDisposed);
				}
			}
		}

		private void OnBeforeAcceptDisposed(object sender, EventArgs args)
		{
			_onBeforeAccept = null;
		}

		protected virtual void BeforeAccept()
		{
			try
			{
				if (OnBeforeAccept != null)
					OnBeforeAccept.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				WebSession.ErrorList.Add(exception);
			}
		}

		private void CloseClick(object sender, EventArgs args)
		{
			Close(CloseBehavior.AcceptOrClose);
		}
											 
		public event EventHandler Accepting; 
		private void AcceptClick(object sender, EventArgs args)
		{
			try
			{
				if (Accepting != null)
					Accepting(this, args);
			}
			catch (Exception exception)
			{
				WebSession.ErrorList.Add(exception);
				// don't rethrow
			}
			if (AcceptEnabled) 
				Close(CloseBehavior.AcceptOrClose);
		}
		
		private void RejectClick(object sender, EventArgs args)
		{
			Close(CloseBehavior.RejectOrClose);
		}
		
		// IFormInterface

		private FormInterfaceHandler _onAcceptForm;
		private FormInterfaceHandler _onRejectForm;

		private FormMode _mode;
		public FormMode Mode { get { return _mode; } }

		public void Show()
		{
			Show(FormMode.None);
		}

		public void Show(FormMode mode)
		{
			_mode = mode;
			HostNode.Session.Forms.Add(this);
			BroadcastEvent(new FormShownEvent());
		}

		public void Show(IFormInterface parentForm, FormInterfaceHandler onAcceptForm, FormInterfaceHandler onRejectForm, FormMode mode)
		{
			if ((_onAcceptForm != null) || (_onRejectForm != null))
				throw new ClientException(ClientException.Codes.FormAlreadyModal);
			_onAcceptForm = onAcceptForm;
			_onRejectForm = onRejectForm;
			_mode = mode;
			try
			{
				HostNode.Session.Forms.AddModal(this, parentForm);
			}
			catch
			{
				EndModal();
				throw;
			}
			BroadcastEvent(new FormShownEvent());
		}

		// TopMost

		private bool _topMost;
		[DefaultValue(false)]
		[Publish(PublishMethod.None)]
		public bool TopMost
		{
			get { return _topMost; }
			set { _topMost = value; }
		}

		// ErrorList - DFD loading errors

		private ErrorList _errorList;
		public ErrorList ErrorList { get { return _errorList; } }

		public virtual void EmbedErrors(ErrorList errorList)
		{
			_errorList = errorList;
		}

		// Closing

		public event CancelEventHandler OnClosing;	// Named to be consistent with the Windows forms event names

		public virtual void Closing(CancelEventArgs args)
		{
			if (OnClosing != null)
				OnClosing(this, args);
		}

		// Closed

		public event EventHandler OnClosed;

		public virtual void Closed(EventArgs args)
		{
			if (OnClosed != null)
				OnClosed(this, args);
		}

		// Close

		public bool Close(CloseBehavior behavior)
		{
			CancelEventArgs args = new CancelEventArgs(false);
			Closing(args);
			if (args.Cancel)
				return false;
	
			if (IsAcceptReject)
				if (behavior == CloseBehavior.AcceptOrClose)
					PostChanges();
				else
					CancelChanges();

			HostNode.Session.Forms.Remove(this);

			try
			{
				if (IsAcceptReject)
				{
					if (behavior == CloseBehavior.AcceptOrClose)
						AcceptForm();
					else
						RejectForm();
				}
			}
			finally
			{
				if (behavior == CloseBehavior.AcceptOrClose)
				{
					if (_onAcceptForm != null)
						_onAcceptForm(this);
				}
				else
				{
					if (_onRejectForm != null)
						_onRejectForm(this);
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
			_mode = FormMode.None;
			_onAcceptForm = null;
			_onRejectForm = null;
		}

		private void AcceptForm()
		{
			if ((_mode == FormMode.Delete) && (MainSource != null))
				MainSource.DataView.Delete();
		}

		private void RejectForm()
		{
			try
			{
				if (((_mode == FormMode.Edit) || (_mode == FormMode.Insert)) && (MainSource != null))
					MainSource.DataView.Cancel();
			}
			catch (Exception exception)
			{
				WebSession.ErrorList.Add(exception);
				// don't rethrow
			}
		}
		
		// Enable / Disable

		private int _disableCount;

		public virtual void Enable()
		{
			_disableCount--;
		}

		public virtual void Disable(IFormInterface form)
		{
			_disableCount++;
		}

		public virtual bool GetEnabled()
		{
			return _disableCount <= 0;
		}

		// LogicalClock

		private int _logicalClock = 1;
		public int LogicalClock
		{
			get { return _logicalClock; }
		}

		public void IncrementLogicalClock()
		{
			_logicalClock++;
		}

		private int GetContextClock(HttpContext context)
		{
			string logicalClockString = context.Request["LogicalClock"];
			if ((logicalClockString == null) || (logicalClockString == String.Empty))
				return _logicalClock - 1;
			else
			{
				try
				{
					return Int32.Parse(logicalClockString);
				}
				catch
				{
					return _logicalClock - 1;
				}
			}
		}

		public virtual void PreprocessRequest(HttpContext context)	// this is not part of an IWebPrehandler interface
		{
			IncrementLogicalClock();
			if (GetContextClock(context) == (_logicalClock - 1))	// If logical clock was missing or incorrect, do not apply request
			{
				PreprocessRequestEvent eventValue = new PreprocessRequestEvent(context);
				BroadcastEvent(eventValue);
				if (eventValue.AnyErrors)
					throw new AbortException();
			}
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext context)
		{
			return 
				base.ProcessRequest(context)
					|| _mainMenu.ProcessRequest(context) 
					|| _toolBar.ProcessRequest(context);
		}

		// Element

		public override int GetDefaultMarginLeft()
		{
			return MarginLeft;
		}

		public override int GetDefaultMarginRight()
		{
			return MarginRight;
		}

		public override int GetDefaultMarginTop()
		{
			return MarginTop;
		}

		public override int GetDefaultMarginBottom()
		{
			return MarginBottom;
		}

		// IWebElement

		private MenuItem _acceptMenuItem;
		private MenuItem _rejectMenuItem;
		private MenuItem _closeMenuItem;

		private ToolBarButton _acceptButton;
		private ToolBarButton _rejectButton;
		private ToolBarButton _closeButton;

		private MenuItem _formMenuItem;
		public MenuItem FormMenuItem { get { return _formMenuItem; } }

		private void DestroyButton(ToolBarButton button)
		{
		}

		private void DestroyMenu(Menu menu)
		{
			Children.Remove(menu);
			menu.Dispose();
		}

		protected virtual void UpdateAcceptReject()
		{
			if (IsAcceptReject)
			{
				if (!_toolBar.Contains(_acceptButton))
				{
					if (_toolBar.Contains(_closeButton))
						_toolBar.Remove(_closeButton);
					if (_mainMenu.Items.Contains(_closeMenuItem))
						_mainMenu.Items.Remove(_closeMenuItem);

					_formMenuItem.Items.Insert(0, _acceptMenuItem);
					_formMenuItem.Items.Insert(1, _rejectMenuItem);
					_toolBar.Insert(0, _acceptButton);
					_toolBar.Insert(1, _rejectButton);
				}
			}
			else
			{
				if (!_toolBar.Contains(_closeButton))
				{
					if (_toolBar.Contains(_acceptButton))
						_toolBar.Remove(_acceptButton);
					if (_toolBar.Contains(_rejectButton))
						_toolBar.Remove(_rejectButton);
					if (_mainMenu.Items.Contains(_acceptMenuItem))
						_mainMenu.Items.Remove(_acceptMenuItem);
					if (_mainMenu.Items.Contains(_rejectMenuItem))
						_mainMenu.Items.Remove(_rejectMenuItem);

					_formMenuItem.Items.Insert(0, _closeMenuItem);
					_toolBar.Insert(0, _closeButton);
				}
			}
		}

		protected override void InternalRender(HtmlTextWriter writer)
		{
			HttpContext context = Session.Get(this).Context;

			writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
			writer.AddAttribute(HtmlTextWriterAttribute.Name, "LogicalClock");
			writer.AddAttribute(HtmlTextWriterAttribute.Value, _logicalClock.ToString());
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();

			writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
			writer.AddAttribute(HtmlTextWriterAttribute.Name, "FormID");
			writer.AddAttribute(HtmlTextWriterAttribute.Value, ID);
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();

			UpdateAcceptReject();

			_mainMenu.Render(context, writer);

			base.InternalRender(writer);
			_toolBar.Render(context, writer);
		}
	}

	public class ToolBarButton
	{
		public ToolBarButton() 
		{
			_iD = Session.GenerateID();
		}

		// ID

		private string _iD;
		public string ID { get { return _iD; } }

		// Text

		private string _text = String.Empty;
		public string Text { get { return _text; } set { _text = value; } }

		// Hint

		private string _hint = String.Empty;
		public string Hint { get { return _hint; } set { _hint = value; } }

		// Enabled

		private bool _enabled = true;
		public bool Enabled { get { return _enabled; } set { _enabled = value; } }

		// Visible

		private bool _visible = true;
		public bool Visible { get { return _visible; } set { _visible = value; } }

		// OnClick

		public event EventHandler OnClick;

		public void Render(HttpContext context, HtmlTextWriter writer)
		{
			if (_visible)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				writer.RenderBeginTag(HtmlTextWriterTag.Td);

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "toolbarbutton");
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(context, _iD));
				writer.AddAttribute(HtmlTextWriterAttribute.Value, Session.RemoveAccellerator(_text), true);
				writer.AddAttribute(HtmlTextWriterAttribute.Title, _hint, true);
				if (!_enabled)
					writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
				writer.RenderBeginTag(HtmlTextWriterTag.Input);
				writer.RenderEndTag();

				writer.RenderEndTag();	// TD
			}
		}

		public bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, _iD) && _enabled && (OnClick != null))
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
		protected override void Validate(object tempValue)
		{
			base.Validate(tempValue);
			if (!(tempValue is ToolBarButton))
				throw new WebClientException(WebClientException.Codes.InvalidToolbarObject);
		}

		public new ToolBarButton this[int index]
		{
			get { return (ToolBarButton)base[index]; }
		}

		public void Render(HttpContext context, HtmlTextWriter writer) 
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "toolbar");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			foreach (ToolBarButton button in this)
				button.Render(context, writer);

			writer.RenderEndTag();
			writer.RenderEndTag();
		}

		public bool ProcessRequest(HttpContext context)
		{
			foreach (ToolBarButton button in this)
				if (button.ProcessRequest(context))
					return true;
			return false;
		}
	}

	public class ProcessRequestEvent : NodeEvent
	{
		public ProcessRequestEvent(HttpContext context) : base()
		{
			_context = context;
		}

		private HttpContext _context;
		public HttpContext Context { get { return _context; } }

		public override void Handle(INode node)
		{
			IWebHandler handler = node as IWebHandler;
			if (handler != null)
				IsHandled = IsHandled || handler.ProcessRequest(_context);
		}
	}			

	public class PreprocessRequestEvent : NodeEvent
	{
		public PreprocessRequestEvent(HttpContext context) : base()
		{
			_context = context;
		}

		private HttpContext _context;
		public HttpContext Context { get { return _context; } }

		private bool _anyErrors;
		public bool AnyErrors { get { return _anyErrors; } }

		public override void Handle(INode node)
		{
			IWebPrehandler handler = node as IWebPrehandler;
			if (handler != null)
			{
				try
				{
					handler.PreprocessRequest(_context);
				}
				catch (Exception exception)
				{
					_anyErrors = true;
					IWebElement webElement = node as IWebElement;
					if (webElement == null)
						throw;
					else
						webElement.HandleElementException(exception);
				}
			}				
		}
	}			

	public class ShowLinkAction : Client.ShowLinkAction
	{
		protected override void InternalExecute(Alphora.Dataphor.Frontend.Client.INode sender, Alphora.Dataphor.Frontend.Client.EventParams paramsValue)
		{
			((Web.Session)HostNode.Session).OnBeforeRenderBody += new RenderHandler(SessionBeforeRender);
		}

		private void SessionBeforeRender(HttpContext context)
		{
			((Web.Session)HostNode.Session).OnBeforeRenderBody -= new RenderHandler(SessionBeforeRender);
			HtmlTextWriter writer = new HtmlTextWriter(context.Response.Output);
			try
			{
				writer.AddAttribute("language", "JavaScript");
				writer.RenderBeginTag(HtmlTextWriterTag.Script);
				writer.Write
				(
					String.Format
					(
						@"open(""{0}"", ""_blank"");",
						URL
					)
				);
				writer.RenderEndTag();
			}
			finally
			{
				writer.Close();
			}
		}
	}

}
