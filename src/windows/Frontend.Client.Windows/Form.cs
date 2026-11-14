/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Drawing;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public enum AcceptRejectState {None, True, False};

	[PublishAs("Interface")]
	[ListInDesigner(false)]
	[DesignerImage("Image('Frontend', 'Nodes.Interface')")]
	public class FormInterface : Interface, IWindowsFormInterface, IWindowsMenuHost, IAccelerates, IWindowsExposedHost, IWindowsContainerElement, IUpdateHandler, IErrorSource
	{
		// Min and max height are in terms root element size
		public const int MinWidth = 310;
		public const int MinHeight = 4;

		public const int MarginLeft = 4;
		public const int MarginRight = 4;
		public const int MarginTop = 6;
		public const int MarginBottom = 6;

		public const int WM_NEXTDLGCTL = 0x0028;

		protected override void Dispose(bool disposed)
		{
			try
			{
				base.Dispose(disposed);
			}
			finally
			{
				OnBeforeAccept = null;
			}
		}
	
		#region IAccelerates

		private AcceleratorManager _accelerators = new AcceleratorManager();
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public AcceleratorManager Accelerators
		{
			get { return _accelerators; }
		}

		#endregion

		#region IWindowsMenuHost

		[Browsable(false)]
		public IWindowsBarContainer MenuContainer
		{
			get { return (_form == null ? null : _form.MenuContainer); }
		}

		#endregion

		#region IWindowsInterface

		private Dictionary<object, FormInterfaceHandler> _customActionHandlers = new Dictionary<object, FormInterfaceHandler>();

		public virtual object AddCustomAction(string text, System.Drawing.Image image, FormInterfaceHandler handler)
		{
			object result = 
				_form.AddCustomAction
				(
					text, 
					image, 
					new EventHandler
					(
						delegate(object ASender, EventArgs AArgs) 
						{
							_customActionHandlers[ASender](this);
						}
					)
				);
			_customActionHandlers.Add(result, handler);
			return result;
		}

		public virtual void RemoveCustomAction(object action)
		{
			_form.RemoveCustomAction(action);
			_customActionHandlers.Remove(action);
		}

		public virtual void ClearCustomActions()
		{
			_form.ClearCustomActions();
			_customActionHandlers.Clear();
		}

		private ErrorList _errorList;
		[Browsable(false)]
		public ErrorList ErrorList { get { return _errorList; }	}

		public virtual void EmbedErrors(ErrorList errorList)
		{
			_errorList = errorList;
			if (Active)
				_form.EmbedErrors(errorList);
		}

		#endregion

		#region IWindowsExposedHost

		[Browsable(false)]
		public IWindowsBarContainer ExposedContainer
		{
			get { return (_form == null ? null : _form.ExposedContainer); }
		}

		#endregion

		#region Appearance Properties

		// BackgroundImage

		private string _backgroundImage = String.Empty;

		/// <summary> A URL referring to an image resource which is used as a background for the Form. </summary>
		[DefaultValue("")]
		[Description("A URL referring to an image resource which is used as a background for the Form.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string BackgroundImage
		{
			get { return _backgroundImage; }
			set
			{
				if (_backgroundImage != value)
				{
					_backgroundImage = value;
					if (Active)
						UpdateBackgroundImage();
				}
			}
		}
	
		private AsyncImageRequest _backgroundImageRequest = null;

		private void UpdateBackgroundImage()
		{
			if (BackgroundImage == String.Empty)
				ClearBackgroundImage();
			else
			{
				_backgroundImageRequest = new AsyncImageRequest(this, _backgroundImage, new EventHandler(BackgroundLoaded));
			}
		}

		private void BackgroundLoaded(object sender, EventArgs args)
		{
			_form.BackgroundImage = ((AsyncImageRequest)sender).Image;
		}

		private void ClearBackgroundImage()
		{
			if (_form.BackgroundImage != null)
			{
				_form.BackgroundImage.Dispose();
				_form.BackgroundImage = null;
			}
		}

		// IconImage

		private string _iconImage = String.Empty;

		/// <summary> The image be used as an icon for this form. </summary>
		[DefaultValue("")]
		[Description("The image be used as an icon for this form.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string IconImage
		{
			get { return _iconImage; }
			set
			{
				if (_iconImage != value)
				{
					_iconImage = value;
					if (Active)
						UpdateImage();
				}
			}
		}
	
		private AsyncImageRequest _imageRequest = null;

		private void UpdateImage()
		{
			if (_iconImage == String.Empty)
				ClearImage();
			else
			{
				_imageRequest = new AsyncImageRequest(this, _iconImage, new EventHandler(ImageLoaded));
			}
		}

		private void ImageLoaded(object sender, EventArgs args)
		{
			if (_imageRequest.Image is Bitmap)
				_form.Icon = Icon.FromHandle(((Bitmap)_imageRequest.Image).GetHicon());
		}

		private void ClearImage()
		{
			Icon defaultIcon = ((Session)HostNode.Session).DefaultIcon;
			if 
			(
				(_form.Icon != null) && 
				(
					(defaultIcon == null) || 
					((defaultIcon != null) && (_form.Icon != defaultIcon))
				)
			)
				_form.Icon.Dispose();
			if (defaultIcon != null)
				_form.Icon = defaultIcon;
			else
				_form.ResetIcon();
		}

		// TopMost
		
		private bool _topMost;
		[DefaultValue(false)]
		[Publish(PublishMethod.None)]
		public bool TopMost 
		{ 
			get { return _topMost; }
			set 
			{
				if (_topMost != value)
				{
					_topMost = value;
					if (Active)
						UpdateTopMost();
				}
			}
		}

		private void UpdateTopMost()
		{
			// HACK: if the form's TopMost property is set to false (even though it already is false) the first control is not focused.
			if (_topMost != Form.TopMost)
				Form.TopMost = _topMost;
		}

		#endregion

		#region Accept / Reject

		// IsAcceptReject

		private bool _supressCloseButton;
		/// <summary> Supresses the Close button on the form. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public bool SupressCloseButton
		{
			get { return _supressCloseButton; }
			set 
			{ 
				if (_supressCloseButton != value)
				{
					_supressCloseButton = value; 
					if (Active)
						AcceptRejectChanged();
				}
			}
		}

		private bool _forceAcceptReject;
		[DefaultValue(false)]
		public bool ForceAcceptReject
		{
			get { return _forceAcceptReject; }
			set
			{
				if (_forceAcceptReject != value)
				{
					_forceAcceptReject = value;
					AcceptRejectChanged();
				}
			}
		}
		
		[Publish(PublishMethod.None)]
		[Browsable(false)]
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
		[DefaultValue(true)]
		public bool AcceptEnabled
		{
			get { return _acceptEnabled; }
			set
			{
				if (_acceptEnabled != value)
				{
					_acceptEnabled = value;
					AcceptRejectChanged();
				}
			}
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
				Session.HandleException(exception);
			}
		} 		
				
		private AcceptRejectState _acceptRejectState;

		protected void AcceptRejectChanged()
		{
			bool isAcceptReject = IsAcceptReject;
			if 
			(
				(_acceptRejectState == AcceptRejectState.None) ||
				(isAcceptReject != (_acceptRejectState == AcceptRejectState.True))
			)
			{
				if (isAcceptReject)
					_acceptRejectState = AcceptRejectState.True;
				else
					_acceptRejectState = AcceptRejectState.False;

				if (_form != null)
					_form.SetAcceptReject(isAcceptReject, _supressCloseButton);
			}
		}

		protected override void MainSourceStateChanged(DAE.Client.DataLink link, DAE.Client.DataSet dataSet)
		{
			if (Active)
				AcceptRejectChanged();
		}

		#endregion

		#region Node

		protected override void Activate()
		{
			WinForms.Application.UseWaitCursor = true;
			try
			{
				SetForm(CreateForm());
				try
				{
					_form.SuspendLayout();
					try
					{
						_acceptRejectState = AcceptRejectState.None;

						_accelerators.Reset();
						_accelerators.Allocate('C');	// Close / Accept
						_accelerators.Allocate('F');	// Form
						_accelerators.Allocate('R');	// Reject (must allocate now, cuz we won't be able to re-claim it if we go from Close to Accept/Reject)

						if (_errorList != null)
							_form.EmbedErrors(_errorList);
						_form.ForeColor = ((Session)HostNode.Session).Theme.ForeColor;

						base.Activate();
						InternalUpdateEnabled();
						InternalUpdateAutoSize();
						InternalUpdateIsLookup();
					}
					catch
					{
						_form.ResumeLayout(false);
						throw;
					}
				}
				catch
				{
					_form.Dispose();
					SetForm(null);
					throw;
				}
			}
			finally
			{
				WinForms.Application.UseWaitCursor = false;
			}
		}

		protected override void BeforeDeactivate()
		{
			// Don't BeginUpdate() here, this causes the screen to not be refreshed where the form was.
			_form.SuspendLayout();
			try
			{
				base.BeforeDeactivate();
			}
			catch
			{
				_form.ResumeLayout(false);
				throw;
			}
		}

		protected override void Deactivate()
		{
			try
			{
				if (_form != null)
				{
					_form.Hide();
					base.Deactivate();
				}
				else
					base.Deactivate();
			}
			finally
			{
				if (_form != null)
				{
					// Don't bother resuming layout and ending update.
					ClearBackgroundImage();
					_form.Dispose();
					SetForm(null);
				}
			}
		}

		protected override void AfterActivate()
		{
			WinForms.Cursor oldCursor = WinForms.Cursor.Current;
			WinForms.Cursor.Current = WinForms.Cursors.WaitCursor;
			try
			{
				try
				{
					try
					{
						UpdateActiveControl(null);
						UpdateBackgroundImage();
						UpdateImage();
						UpdateTopMost();
					}
					finally
					{
						base.AfterActivate();
					}
				}
				finally
				{
					_form.ResumeLayout(false);
				}
			}
			finally
			{
				WinForms.Cursor.Current = oldCursor;
			}
			((Session)HostNode.Session).DoAfterFormActivate(this);
		}

		public override void HandleEvent(NodeEvent eventValue)
		{
			if (eventValue is FocusChangedEvent)
				UpdateActiveControl(((FocusChangedEvent)eventValue).Node);
			else if (eventValue is AdvanceFocusEvent)
				Form.AdvanceFocus(((AdvanceFocusEvent)eventValue).Forward);
			else
			{
				base.HandleEvent(eventValue);
				return;
			}
			eventValue.IsHandled = true;
		}

		#endregion

		#region Default Actions
		
		public override void PerformDefaultAction()
		{
			if (_mode == FormMode.None) 
			{
				if (OnDefault != null)
					OnDefault.Execute(this, new EventParams());
			}
			else
				Close(CloseBehavior.AcceptOrClose);
		}

		public string GetDefaultActionDescription()
		{
			if (_mode == FormMode.None)
				if (OnDefault != null)
					return OnDefault.GetDescription();
				else
					return String.Empty;
			else
				return Strings.Close;
		}

		protected override void DefaultChanged()
		{
			if (Active)
				_form.UpdateStatusText();
		}

		#endregion

		#region Form

		private IBaseForm _form;
		/// <summary> Represents for windows Form object which is maintained by this node. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IBaseForm Form
		{
			get { return _form; }
		}

		private void SetForm(IBaseForm form)
		{
			if (_form != form)
			{
				if (_form != null)
				{
					_form.Accepting -= new EventHandler(FormAccepting);
					_form.Closing -= new CancelEventHandler(FormClosing);
					_form.Closed -= new EventHandler(FormClosed);
					_form.PaintBackground -= new PaintHandledEventHandler(FormPaintBackground);
					_form.LayoutContents -= new EventHandler(FormLayoutContents);
					_form.GetNaturalSize -= new GetSizeHandler(FormGetNaturalSize);
					_form.DefaultAction -= new EventHandler(FormDefaultAction);
					_form.Shown -= new EventHandler(FormShown);
					_form.GetDefaultActionDescription -= new GetStringHandler(FormGetDefaultActionDescription);

					((Session)HostNode.Session).UnregisterControlHelp((WinForms.Control)_form);
				}
				_form = form;
				if (_form != null)
				{
					_form.Accepting += new EventHandler(FormAccepting);
					_form.Closing += new CancelEventHandler(FormClosing);
					_form.Closed += new EventHandler(FormClosed);
					_form.PaintBackground += new PaintHandledEventHandler(FormPaintBackground);
					_form.LayoutContents += new EventHandler(FormLayoutContents);
					_form.GetNaturalSize += new GetSizeHandler(FormGetNaturalSize);
					_form.DefaultAction += new EventHandler(FormDefaultAction);
					_form.Shown += new EventHandler(FormShown);
					_form.GetDefaultActionDescription += new GetStringHandler(FormGetDefaultActionDescription);

					_form.HelpButton = ((Session)HostNode.Session).IsContextHelpAvailable();
					((Session)HostNode.Session).RegisterControlHelp((WinForms.Control)_form, this);
					MapDialogKeys(_form);
				}
			}
		}

		private void FormShown(object sender, EventArgs args)
		{
			BroadcastEvent(new FormShownEvent());
		}

		/// <summary> Descendants can override this method to provide a custom form instance. </summary>
		protected virtual IBaseForm CreateForm()
		{
			return new BaseForm();
		}

		private void FormPaintBackground(object sender, WinForms.PaintEventArgs args, out bool handled)
		{
			handled = (_form.BackgroundImage != null);
			if (!handled)
				handled = ((Session)HostNode.Session).Theme.PaintBackground((WinForms.Control)_form, args);
		}

		private void FormLayoutContents(object sender, EventArgs args)
		{
			Rectangle displayRectangle = _form.ContentPanel.Bounds;

			// Enforce minimums and compensate for any area lost to scroll bars
			Size minSize = MinSize;
			if (displayRectangle.Width < minSize.Width)
			{
				displayRectangle.Width = minSize.Width;
				displayRectangle.Height = Math.Max(minSize.Height, displayRectangle.Height - WinForms.SystemInformation.HorizontalScrollBarHeight);
			}
			if (displayRectangle.Height < minSize.Height)
			{
				displayRectangle.Height = minSize.Height;
				displayRectangle.Width = Math.Max(minSize.Width, displayRectangle.Width - WinForms.SystemInformation.VerticalScrollBarWidth);
			}

			// Perform contents layout
			Layout(new Rectangle(Point.Empty, displayRectangle.Size));
		}

		private Size FormGetNaturalSize(object sender)
		{
			return NaturalSize;
		}

		private void FormDefaultAction(object sender, EventArgs args)
		{
			PerformDefaultAction();
		}

		private string FormGetDefaultActionDescription(object sender)
		{
			return GetDefaultActionDescription();
		}

		private void FormAccepting(object sender, EventArgs args)
		{
			BeforeAccept();
		}
		
		private void FormClosing(object sender, CancelEventArgs args)
		{
			args.Cancel = FormClosing();
		}

		#endregion

		#region Form Key Mappings

		private void MapDialogKeys(IBaseForm form)
		{
			form.DialogKeys[WinForms.Keys.Up] = new DialogKeyHandler(NavigatePrior);
			form.DialogKeys[WinForms.Keys.Down] = new DialogKeyHandler(NavigateNext);
			form.DialogKeys[WinForms.Keys.PageUp] = new DialogKeyHandler(NavigatePriorPage);
			form.DialogKeys[WinForms.Keys.PageDown] = new DialogKeyHandler(NavigateNextPage);
			form.DialogKeys[WinForms.Keys.Home | WinForms.Keys.Control] = new DialogKeyHandler(NavigateFirst);
			form.DialogKeys[WinForms.Keys.End | WinForms.Keys.Control] = new DialogKeyHandler(NavigateLast);
			form.DialogKeys[WinForms.Keys.Escape] = new DialogKeyHandler(CancelForm);
		}

		private bool CancelForm(WinForms.Form form, WinForms.Keys key)
		{
			if (!SupressCloseButton)	// Don't close with escape key if supressclosebutton
			{
				Close(CloseBehavior.RejectOrClose);
				return true;
			}
			else
				return false;
		}

		protected DAE.Client.DataView MainView
		{
			get { return (MainSource == null ? null : MainSource.DataView); }
		}

		protected bool MainViewCanNavigate
		{
			get { return (MainView != null) && MainView.Active && (MainView.State == DAE.Client.DataSetState.Browse); }
		}

		private bool NavigatePrior(WinForms.Form form, WinForms.Keys key)
		{
			if (MainViewCanNavigate)
			{
				MainView.Prior();
				return true;
			}
			else
				return false;
		}

		private bool NavigateNext(WinForms.Form form, WinForms.Keys key)
		{
			if (MainViewCanNavigate)
			{
				MainView.Next();
				return true;
			}
			else
				return false;
		}

		private bool NavigatePriorPage(WinForms.Form form, WinForms.Keys key)
		{
			if (MainViewCanNavigate)
			{
				MainView.MoveBy((MainView.BufferCount - 1) * -1);
				return true;
			}
			else
				return false;
		}

		private bool NavigateNextPage(WinForms.Form form, WinForms.Keys key)
		{
			if (MainViewCanNavigate)
			{
				MainView.MoveBy((MainView.BufferCount - 1));
				return true;
			}
			else
				return false;
		}

		private bool NavigateFirst(WinForms.Form form, WinForms.Keys key)
		{
			if (MainViewCanNavigate)
			{
				MainView.First();
				return true;
			}
			else
				return false;
		}

		private bool NavigateLast(WinForms.Form form, WinForms.Keys key)
		{
			if (MainViewCanNavigate)
			{
				MainView.Last();
				return true;
			}
			else
				return false;
		}

		#endregion

		#region Layout & Sizing

		private bool _autoSize = true;
		/// <summary> Specifies whether the form should initially auto-size itself. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public bool AutoSize 
		{ 
			get { return _autoSize; }
			set 
			{
				if (_autoSize != value)
				{
					_autoSize = value;
					if (Active)
						InternalUpdateAutoSize();
				}
			}
		}

		protected virtual void InternalUpdateAutoSize()
		{
			_form.AutoResize = _autoSize;
		}
	
		public void RootLayout()
		{
			if (Active)
				_form.PerformLayout();
		}



		#endregion

		#region Close

		public virtual bool FormClosing()
		{
			bool cancel = false;
			try
			{
				if (_form != null)
				{
					if (_form.DialogResult == WinForms.DialogResult.OK)	
					{
						try
						{
							PostChanges();
						}
						catch
						{
							cancel = true;
							_form.DialogResult = WinForms.DialogResult.None;
							throw;
						}
					}
					else
						CancelChanges();
				}
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);
			}
			return cancel;
		}

		private void FormClosed(object sender, EventArgs args)
		{
			FormClosed();
		}

		public void FormClosed()
		{
			try
			{
				try
				{
					HostNode.Session.Forms.Remove(this);
				}
				finally
				{
					try
					{
						if (_form.DialogResult == WinForms.DialogResult.OK)
							FormAccepted();
						else
							FormRejected();
					}
					finally
					{
						try
						{
							if (OnClosed != null)
								OnClosed(this, null);
						}
						finally
						{
							try
							{
								if (_onCloseForm != null)
								{
									_onCloseForm(this);
									_onCloseForm = null;
								}
							}
							finally
							{
								if ((_form == null) || !_form.Modal)  // Some forms (such as the main form) may be truly modal.
								{
									EndChildModal();
									if ((HostNode != null) && (HostNode.NextRequest == null))
										HostNode.Dispose();
								}
							}
						}
					}
				}
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);
				if (_form != null)
					_form.DialogResult = WinForms.DialogResult.Cancel;
			}
		}

		protected void EnsureSearchControlTimerElapsed(INode node)
		{
			BroadcastEvent(new ProcessPendingSearchEvent());
		}

		protected virtual void FormAccepted()
		{
			try
			{
				EnsureSearchControlTimerElapsed(this);
			}
			finally
			{
				try
				{
					if ((_mode == FormMode.Delete) && (MainSource != null))
						MainSource.DataView.Delete();
				}
				finally
				{
					if (_onAcceptForm != null)
						_onAcceptForm(this);
				}
			}
		}

		protected virtual void FormRejected()
		{
			try
			{
				if (((_mode == FormMode.Edit) || (_mode == FormMode.Insert)) && (MainSource != null))
					MainSource.DataView.Cancel();
			}
			finally
			{
				if (_onRejectForm != null)
					_onRejectForm(this);
			}
		}
		
		private void UpdateActiveControl(IWindowsElement node)
		{
			_form.SetHintText((node != null) ? node.GetHint() : String.Empty);
			_form.UpdateStatusText();
		}

		public event EventHandler OnClosed;

		public bool Close(CloseBehavior behavior)
		{
			if (_form != null)
				_form.Close(behavior);
			return _form == null;	// will be null of close succussfully completed (this should never be true for a windows form because closing will not happen until the next message loop iteration)
		}

		#endregion

		#region Show

		public bool IsChildModal()
		{
			return (_onAcceptForm != null) || (_onRejectForm != null);
		}

		private void CheckNotChildModal()
		{
			if (IsChildModal())
				throw new ClientException(ClientException.Codes.FormAlreadyModal);
		}

		// Accept/Reject is tied to the data edit state and modal state (someone waiting on accept/reject)

		private FormMode _mode;
		[Browsable(false)]
		public FormMode Mode { get { return _mode; } }

		/// <remarks> The form interface must be active before calling this. </remarks>
		private void SetMode(FormMode mode)
		{
			Form.SuspendLayout();
			try
			{
				_mode = mode;
				if ((_mode != FormMode.None) && ((MainSource == null) || (MainSource.DataView == null)))
					throw new ClientException(ClientException.Codes.MainSourceNotSpecified, _errorList != null ? _errorList.ToException() : null, mode.ToString());

				_form.EnterNavigates = (_mode == FormMode.Insert) || (_mode == FormMode.Edit);
				switch (_mode)
				{
					case FormMode.Insert :
						if (MainSource.DataView.State != DAE.Client.DataSetState.Insert)
							MainSource.DataView.Insert();
						break;
					case FormMode.Edit :
						if (MainSource.DataView.State != DAE.Client.DataSetState.Edit)
							MainSource.DataView.Edit();
						break;
				}
				AcceptRejectChanged();
			}
			finally
			{
				Form.ResumeLayout(false);
			}
		}

		public void Show()
		{
			Show(FormMode.None);
		}

		public void Show(FormMode formMode)
		{
			Show(null, null, null, formMode);
		}

		public WinForms.DialogResult ShowModal(FormMode mode)
		{
			CheckNotChildModal();
			SetMode(mode);
			try
			{			
				AcceptRejectChanged();
				HostNode.Session.Forms.Add(this);
				return _form.ShowDialog();
			}
			catch
			{
				if ((_mode == FormMode.Edit) || (_mode == FormMode.Insert))
					MainSource.DataView.Cancel();
				throw;
			}
		}

		public void Show(FormInterfaceHandler onCloseForm)
		{
			CheckNotChildModal();
			_onCloseForm = onCloseForm;
			try
			{
				SetMode(FormMode.None);
				HostNode.Session.Forms.Add(this);
				_form.Show(null);
			}
			catch
			{
				_onCloseForm = null;
				throw;
			}
		}

		private FormInterfaceHandler _onCloseForm;
		private FormInterfaceHandler _onAcceptForm;
		private FormInterfaceHandler _onRejectForm;

		public void Show(IFormInterface parentForm, FormInterfaceHandler onAcceptForm, FormInterfaceHandler onRejectForm, FormMode mode)
		{
			CheckNotChildModal();
			_onAcceptForm = onAcceptForm;
			_onRejectForm = onRejectForm;
			try
			{
				SetMode(mode);
				try
				{
					if (parentForm != null)
						HostNode.Session.Forms.AddModal(this, parentForm);
					else
						HostNode.Session.Forms.Add(this);
					AcceptRejectChanged();
					_form.Show(parentForm);
				}
				catch
				{
					if ((_mode == FormMode.Edit) || (_mode == FormMode.Insert))
						MainSource.DataView.Cancel();
					throw;
				}
			}
			catch
			{
				EndChildModal();
				throw;
			}

		}

		private void EndChildModal()
		{
			_onAcceptForm = null;
			_onRejectForm = null;
			_mode = FormMode.None;
		}

		#endregion

		#region IsLookup

		private bool _isLookup;
		
		/// <remarks> Only effectual on show. </remarks>
		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public bool IsLookup
		{
			get { return _isLookup; }
			set 
			{ 
				if (_isLookup != value)
				{
					_isLookup = value;
					if (Active) 
						InternalUpdateIsLookup();
				}
			}
		}
		
		private void InternalUpdateIsLookup()
		{
			_form.IsLookup = _isLookup;
		}
		
		#endregion
		
		#region Enabled / Disabled

		private int _disableCount;

		public virtual void Enable()
		{
			_disableCount--;
			if (Active)
				InternalUpdateEnabled();
		}

		public virtual void Disable(IFormInterface form)
		{
			_disableCount++;
			if (Active)
				InternalUpdateEnabled();

			IWindowsFormInterface localForm = form as IWindowsFormInterface;
			if (localForm != null)
			{
				localForm.Form.Owner = (WinForms.Form)Form;
				// LForm.Form.ShowInTaskbar = false;	this would be okay, except that alt-tab doesn't seem to work unless the owned form is also in the task bar
			}
		}

		public virtual bool GetEnabled()
		{
			return _disableCount <= 0;
		}

		protected virtual void InternalUpdateEnabled()
		{
			bool isEnabled = GetEnabled();
			// WinForms form handling seems to re-create the handle when the ShowInTaskbar property is toggled
			// Setting the taskbar before the enabled made the form disable properly.  -- Bryan
			// disabled again, besides the proplem above there is a huge amount of eyesore flickering happening.
//			Form.ShowInTaskbar = LIsEnabled;
			Form.Enabled = isEnabled;
		}

		#endregion

		#region IUpdateHandler

		public override void BeginUpdate()
		{
			if (Active)
			{
				_form.BeginUpdate();
				_form.SuspendLayout();
			}
		}

		public void EndUpdate()
		{
			EndUpdate(true);
		}

		public override void EndUpdate(bool performLayout)
		{
			if (Active)
			{
				_form.EndUpdate();
				_form.ResumeLayout(performLayout);
			}
		}

		#endregion

		#region IWindowsContainerElement

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual WinForms.Control Control
		{
			get { return _form.ContentPanel; }
		}

		#endregion
	
		#region Element

		// Hint / ToolTip

		protected override void InternalUpdateToolTip()
		{
			SetToolTip((WinForms.Control)_form);
		}

		// Setting visible on the form is equivilant to showing which we handle differently
//		protected override void InternalUpdateVisible() 

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

		protected override Size InternalMinSize
		{
			get
			{
				Size result = base.InternalMinSize;
				// constrain to the minimum required of the form.
				ConstrainMin(ref result, new Size(MinWidth, MinHeight));
				return result;
			}
		}

		protected override Size InternalNaturalSize
		{
			get
			{
				Size natural = base.InternalNaturalSize;
				Size maxSize = InternalMaxSize;
				Size minSize = InternalMinSize;
				ConstrainMin(ref maxSize, minSize);	// make sure we don't allow the max to be below the min
				ConstrainMin(ref natural, minSize);
				ConstrainMax(ref natural, maxSize);
				return natural;
			}
		}

		/// <summary> Text becomes the caption of the Form. </summary>
		protected override void InternalUpdateText()
		{
			_form.Text = GetText();
		}

		/// <summary> Estemates the Minimum size of the overall form. </summary>
		/// <remarks> This is only an estimate because actually resizing the form may cause menu wrapping and cause the size to change. </remarks>
		public virtual Size FormMinSize()
		{
			CheckActive();
			return MinSize + _form.GetBorderSize();
		}

		/// <summary> Estemates the Natural size of the overall form. </summary>
		/// <remarks> This is only an estimate because actually resizing the form may cause menu wrapping and cause the size to change. </remarks>
		public virtual Size FormNaturalSize()
		{
			CheckActive();
			return NaturalSize + _form.GetBorderSize();
		}

		/// <summary> Estemates the Max size of the overall form. </summary>
		/// <remarks> This is only an estimate because actually resizing the form may cause menu wrapping and cause the size to change. </remarks>
		public virtual Size FormMaxSize()
		{
			CheckActive();
			return MaxSize + _form.GetBorderSize();
		}

		#endregion

		#region IErrorSource Members

		public void ErrorHighlighted(Exception exception)
		{
			// Nothing
		}

		public void ErrorSelected(Exception exception)
		{
			if (_form != null)
				_form.Activate();
		}

		#endregion
	}

	public class AcceleratorManager
	{
		private const int FirstAcceleratorOffset = 0x30;
		private const int AcceleratorRange = 0x4A;

		public AcceleratorManager()
		{
			_accelerators = new System.Collections.BitArray(AcceleratorRange, false);
		}

		private System.Collections.BitArray _accelerators;

		public void Reset()
		{
			_accelerators.SetAll(false);
		}

		internal bool Allocate(char charValue)
		{
			int index = (int)Char.ToUpper(charValue) - FirstAcceleratorOffset;
			if ((index > 0) && (index < AcceleratorRange) && !_accelerators[index])
			{
				_accelerators[index] = true;
				return true;
			}
			else
				return false;
		}

		// This Method will try to allocate an accelorator if it can. (for use internally and with Groupers and Pushers)
		private string AllocateTry(string text)
		{
			if (text != String.Empty)
			{
				int pos;
				System.Text.StringBuilder result = new System.Text.StringBuilder(text.Length);
				bool satisfied = false;

				// First look to see if the "desired" accellerator is available
				for (pos = 0; pos < text.Length; pos++)
				{
					if (text[pos] == '&') 
						if (pos < (text.Length - 1))
						{
							if (text[pos + 1] == '&')	// skip escaped ampersands
							{
								result.Append("&");
								pos++;
							}
							else
								if (!satisfied && Allocate(text[pos + 1]))
									satisfied = true;
								else
									continue;
						}
						else
							continue;
					result.Append(text[pos]);
				}
				if (satisfied)
					return result.ToString();
				else
					text = result.ToString();

				// Step through the characters until we find an elligable accelerator
				for (pos = 0; pos < text.Length; pos++)
				{
					if (Allocate(text[pos]))
						return text.Insert(pos, "&");
				}

				// No accelerators were found, try to use an appended number
				// Disabling this functionality, it confuses users....
				//for (pos = 0; pos < 10; pos++)
				//	if (Allocate((char)(pos + FirstAcceleratorOffset)))
				//		return String.Format("{0} &{1}", text, (char)(pos + FirstAcceleratorOffset));
			}

			// Unable to accelerate, use plain text
			return text;
		}

		/// <summary> Attempts to allocate accellerated text based on the given string. </summary>
		/// <param name="text"> This string may contain one or more ampersands followed by alphanumerics.  
		/// Each ampersand preceeded character will be tested for accellerator availability.  If the 
		/// character is not available or an accellerator has already been successfully allocated, the 
		/// ampersand will be stripped off.  Literal ampersands can be specified using a double ampersand 
		/// escape.</param>
		/// <param name="tryIfNotRequested"> If true, then an attempt will be made to accellerate the 
		/// text regardless of whether or not the text contains a requested accellerator. </param>
		/// <returns> Text with any successfully allocated accellerator, and all others stripped off. </returns>
		public string Allocate(string text, bool tryIfNotRequested)
		{
			if ((text.Length > 0) && (text[0] == '~'))
				return text.Substring(1).Replace("&", "&&");
			else
			{
				if (tryIfNotRequested)
					return AllocateTry(text);
				else
				{
					for (int i = 0; i < text.Length; i++)
						if ((text[i] == '&') && ((i == (text.Length - 1)) || (text[i + 1] != '&')))
							return AllocateTry(text);
					return text;	// no accellerators requested
				}
			}
		}

		internal void Deallocate(char charValue)
		{
			_accelerators[(int)Char.ToUpper(charValue) - FirstAcceleratorOffset] = false;
		}

		public void Deallocate(string text)
		{
			for (int i = 0; i < text.Length; i++)
				if ((text[i] == '&') && ((i == (text.Length - 1)) || (text[i + 1] != '&')))
				{
					Deallocate(text[i + 1]);
					return;
				}
		}
	}

	public class ProcessPendingSearchEvent : NodeEvent
	{
		public override void Handle(INode node)
		{
			if (node is IWindowsSearch)
				((IWindowsSearch)node).SearchControl.ProcessPending();
		}
	}
}
