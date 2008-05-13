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
		public const int CMinWidth = 310;
		public const int CMinHeight = 4;

		public const int CMarginLeft = 4;
		public const int CMarginRight = 4;
		public const int CMarginTop = 6;
		public const int CMarginBottom = 6;

		public const int WM_NEXTDLGCTL = 0x0028;
	
		#region IAccelerates

		private AcceleratorManager FAccelerators = new AcceleratorManager();
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public AcceleratorManager Accelerators
		{
			get { return FAccelerators; }
		}

		#endregion

		#region IWindowsMenuHost

		[Browsable(false)]
		public IWindowsBarContainer MenuContainer
		{
			get { return (FForm == null ? null : FForm.MenuContainer); }
		}

		#endregion

		#region IWindowsInterface

		private Dictionary<object, FormInterfaceHandler> FCustomActionHandlers = new Dictionary<object, FormInterfaceHandler>();

		public virtual object AddCustomAction(string AText, System.Drawing.Image AImage, FormInterfaceHandler AHandler)
		{
			object LResult = 
				FForm.AddCustomAction
				(
					AText, 
					AImage, 
					new EventHandler
					(
						delegate(object ASender, EventArgs AArgs) 
						{
							FCustomActionHandlers[ASender](this);
						}
					)
				);
			FCustomActionHandlers.Add(LResult, AHandler);
			return LResult;
		}

		public virtual void RemoveCustomAction(object AAction)
		{
			FForm.RemoveCustomAction(AAction);
			FCustomActionHandlers.Remove(AAction);
		}

		public virtual void ClearCustomActions()
		{
			FForm.ClearCustomActions();
			FCustomActionHandlers.Clear();
		}

		private ErrorList FErrorList;
		[Browsable(false)]
		public ErrorList ErrorList { get { return FErrorList; }	}

		public virtual void EmbedErrors(ErrorList AErrorList)
		{
			FErrorList = AErrorList;
			if (Active)
				FForm.EmbedErrors(AErrorList);
		}

		#endregion

		#region IWindowsExposedHost

		[Browsable(false)]
		public IWindowsBarContainer ExposedContainer
		{
			get { return (FForm == null ? null : FForm.ExposedContainer); }
		}

		#endregion

		#region Appearance Properties

		// BackgroundImage

		private string FBackgroundImage = String.Empty;

		/// <summary> A URL referring to an image resource which is used as a background for the Form. </summary>
		[DefaultValue("")]
		[Description("A URL referring to an image resource which is used as a background for the Form.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string BackgroundImage
		{
			get { return FBackgroundImage; }
			set
			{
				if (FBackgroundImage != value)
				{
					FBackgroundImage = value;
					if (Active)
						UpdateBackgroundImage();
				}
			}
		}
	
		private AsyncImageRequest FBackgroundImageRequest = null;

		private void UpdateBackgroundImage()
		{
			if (BackgroundImage == String.Empty)
				ClearBackgroundImage();
			else
			{
				FBackgroundImageRequest = new AsyncImageRequest(this, FBackgroundImage, new EventHandler(BackgroundLoaded));
			}
		}

		private void BackgroundLoaded(object ASender, EventArgs AArgs)
		{
			FForm.BackgroundImage = ((AsyncImageRequest)ASender).Image;
		}

		private void ClearBackgroundImage()
		{
			if (FForm.BackgroundImage != null)
			{
				FForm.BackgroundImage.Dispose();
				FForm.BackgroundImage = null;
			}
		}

		// IconImage

		private string FIconImage = String.Empty;

		/// <summary> The image be used as an icon for this form. </summary>
		[DefaultValue("")]
		[Description("The image be used as an icon for this form.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string IconImage
		{
			get { return FIconImage; }
			set
			{
				if (FIconImage != value)
				{
					FIconImage = value;
					if (Active)
						UpdateImage();
				}
			}
		}
	
		private AsyncImageRequest FImageRequest = null;

		private void UpdateImage()
		{
			if (FIconImage == String.Empty)
				ClearImage();
			else
			{
				FImageRequest = new AsyncImageRequest(this, FIconImage, new EventHandler(ImageLoaded));
			}
		}

		private void ImageLoaded(object ASender, EventArgs AArgs)
		{
			if (FImageRequest.Image is Bitmap)
				FForm.Icon = Icon.FromHandle(((Bitmap)FImageRequest.Image).GetHicon());
		}

		private void ClearImage()
		{
			Icon LDefaultIcon = ((Session)HostNode.Session).DefaultIcon;
			if 
			(
				(FForm.Icon != null) && 
				(
					(LDefaultIcon == null) || 
					((LDefaultIcon != null) && (FForm.Icon != LDefaultIcon))
				)
			)
				FForm.Icon.Dispose();
			if (LDefaultIcon != null)
				FForm.Icon = LDefaultIcon;
			else
				FForm.ResetIcon();
		}

		#endregion

		#region Accept / Reject

		// IsAcceptReject

		private bool FSupressCloseButton;
		/// <summary> Supresses the Close button on the form. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public bool SupressCloseButton
		{
			get { return FSupressCloseButton; }
			set 
			{ 
				if (FSupressCloseButton != value)
				{
					FSupressCloseButton = value; 
					if (Active)
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
					(FMode != FormMode.None) ||
					(
						(MainSource != null) &&
						(MainSource.DataView != null) &&
						(
							(MainSource.DataView.State == DAE.Client.DataSetState.Edit) || 
							(MainSource.DataView.State == DAE.Client.DataSetState.Insert)
						)
					);
			}
		}

		private AcceptRejectState FAcceptRejectState;

		protected void AcceptRejectChanged()
		{
			bool LIsAcceptReject = IsAcceptReject;
			if 
			(
				(FAcceptRejectState == AcceptRejectState.None) ||
				(LIsAcceptReject != (FAcceptRejectState == AcceptRejectState.True))
			)
			{
				if (LIsAcceptReject)
					FAcceptRejectState = AcceptRejectState.True;
				else
					FAcceptRejectState = AcceptRejectState.False;
				FForm.SetAcceptReject(LIsAcceptReject, FSupressCloseButton);
			}
		}

		protected override void MainSourceStateChanged(DAE.Client.DataLink ALink, DAE.Client.DataSet ADataSet)
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
					FForm.SuspendLayout();
					try
					{
						FAcceptRejectState = AcceptRejectState.None;

						FAccelerators.Reset();
						FAccelerators.Allocate('C');	// Close / Accept
						FAccelerators.Allocate('F');	// Form
						FAccelerators.Allocate('R');	// Reject (must allocate now, cuz we won't be able to re-claim it if we go from Close to Accept/Reject)

						if (FErrorList != null)
							FForm.EmbedErrors(FErrorList);
						FForm.ForeColor = ((Session)HostNode.Session).Theme.ForeColor;

						base.Activate();
						InternalUpdateEnabled();
						InternalUpdateAutoSize();
						InternalUpdateIsLookup();
					}
					catch
					{
						FForm.ResumeLayout(false);
						throw;
					}
				}
				catch
				{
					FForm.Dispose();
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
			FForm.SuspendLayout();
			try
			{
				base.BeforeDeactivate();
			}
			catch
			{
				FForm.ResumeLayout(false);
				throw;
			}
		}

		protected override void Deactivate()
		{
			try
			{
				if (FForm != null)
				{
					FForm.Hide();
					base.Deactivate();
				}
				else
					base.Deactivate();
			}
			finally
			{
				if (FForm != null)
				{
					// Don't bother resuming layout and ending update.
					ClearBackgroundImage();
					FForm.Dispose();
					SetForm(null);
				}
			}
		}

		protected override void AfterActivate()
		{
			WinForms.Cursor LOldCursor = WinForms.Cursor.Current;
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
					}
					finally
					{
						base.AfterActivate();
					}
				}
				finally
				{
					FForm.ResumeLayout(false);
				}
			}
			finally
			{
				WinForms.Cursor.Current = LOldCursor;
			}
			((Session)HostNode.Session).DoAfterFormActivate(this);
		}

		public override void HandleEvent(NodeEvent AEvent)
		{
			if (AEvent is FocusChangedEvent)
				UpdateActiveControl(((FocusChangedEvent)AEvent).Node);
			else if (AEvent is AdvanceFocusEvent)
				Form.AdvanceFocus(((AdvanceFocusEvent)AEvent).Forward);
			else
			{
				base.HandleEvent(AEvent);
				return;
			}
			AEvent.IsHandled = true;
		}

		#endregion

		#region Default Actions
		
		public override void PerformDefaultAction()
		{
			if ((FMode == FormMode.None) && (OnDefault != null))
				OnDefault.Execute(this, new EventParams());
			else
				Close(CloseBehavior.AcceptOrClose);
		}

		public string GetDefaultActionDescription()
		{
			if ((FMode == FormMode.None) && (OnDefault != null))
				return OnDefault.GetDescription();
			else
				return Strings.Close;
		}

		protected override void DefaultChanged()
		{
			if (Active)
				FForm.UpdateStatusText();
		}

		#endregion

		#region Form

		private IBaseForm FForm;
		/// <summary> Represents for windows Form object which is maintained by this node. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IBaseForm Form
		{
			get { return FForm; }
		}

		private void SetForm(IBaseForm AForm)
		{
			if (FForm != AForm)
			{
				if (FForm != null)
				{
					FForm.Closing -= new CancelEventHandler(FormClosing);
					FForm.Closed -= new EventHandler(FormClosed);
					FForm.PaintBackground -= new PaintHandledEventHandler(FormPaintBackground);
					FForm.LayoutContents -= new EventHandler(FormLayoutContents);
					FForm.GetNaturalSize -= new GetSizeHandler(FormGetNaturalSize);
					FForm.DefaultAction -= new EventHandler(FormDefaultAction);
					FForm.Shown -= new EventHandler(FormShown);
					FForm.GetDefaultActionDescription -= new GetStringHandler(FormGetDefaultActionDescription);

					((Session)HostNode.Session).UnregisterControlHelp((WinForms.Control)FForm);
				}
				FForm = AForm;
				if (FForm != null)
				{
					FForm.Closing += new CancelEventHandler(FormClosing);
					FForm.Closed += new EventHandler(FormClosed);
					FForm.PaintBackground += new PaintHandledEventHandler(FormPaintBackground);
					FForm.LayoutContents += new EventHandler(FormLayoutContents);
					FForm.GetNaturalSize += new GetSizeHandler(FormGetNaturalSize);
					FForm.DefaultAction += new EventHandler(FormDefaultAction);
					FForm.Shown += new EventHandler(FormShown);
					FForm.GetDefaultActionDescription += new GetStringHandler(FormGetDefaultActionDescription);

					FForm.HelpButton = ((Session)HostNode.Session).IsContextHelpAvailable();
					((Session)HostNode.Session).RegisterControlHelp((WinForms.Control)FForm, this);
					MapDialogKeys(FForm);
				}
			}
		}

		private void FormShown(object ASender, EventArgs AArgs)
		{
			BroadcastEvent(new FormShownEvent());
		}

		/// <summary> Descendants can override this method to provide a custom form instance. </summary>
		protected virtual IBaseForm CreateForm()
		{
			return new BaseForm();
		}

		private void FormPaintBackground(object ASender, WinForms.PaintEventArgs AArgs, out bool AHandled)
		{
			AHandled = (FForm.BackgroundImage != null);
			if (!AHandled)
				AHandled = ((Session)HostNode.Session).Theme.PaintBackground((WinForms.Control)FForm, AArgs);
		}

		private void FormLayoutContents(object ASender, EventArgs AArgs)
		{
			Rectangle LDisplayRectangle = FForm.ContentPanel.Bounds;

			// Enforce minimums and compensate for any area lost to scroll bars
			Size LMinSize = MinSize;
			if (LDisplayRectangle.Width < LMinSize.Width)
			{
				LDisplayRectangle.Width = LMinSize.Width;
				LDisplayRectangle.Height = Math.Max(LMinSize.Height, LDisplayRectangle.Height - WinForms.SystemInformation.HorizontalScrollBarHeight);
			}
			if (LDisplayRectangle.Height < LMinSize.Height)
			{
				LDisplayRectangle.Height = LMinSize.Height;
				LDisplayRectangle.Width = Math.Max(LMinSize.Width, LDisplayRectangle.Width - WinForms.SystemInformation.VerticalScrollBarWidth);
			}

			// Perform contents layout
			Layout(new Rectangle(Point.Empty, LDisplayRectangle.Size));
		}

		private Size FormGetNaturalSize(object ASender)
		{
			return NaturalSize;
		}

		private void FormDefaultAction(object ASender, EventArgs AArgs)
		{
			PerformDefaultAction();
		}

		private string FormGetDefaultActionDescription(object ASender)
		{
			return GetDefaultActionDescription();
		}

		private void FormClosing(object ASender, CancelEventArgs AArgs)
		{
			AArgs.Cancel = FormClosing();
		}

		#endregion

		#region Form Key Mappings

		private void MapDialogKeys(IBaseForm AForm)
		{
			AForm.DialogKeys[WinForms.Keys.Up] = new DialogKeyHandler(NavigatePrior);
			AForm.DialogKeys[WinForms.Keys.Down] = new DialogKeyHandler(NavigateNext);
			AForm.DialogKeys[WinForms.Keys.PageUp] = new DialogKeyHandler(NavigatePriorPage);
			AForm.DialogKeys[WinForms.Keys.PageDown] = new DialogKeyHandler(NavigateNextPage);
			AForm.DialogKeys[WinForms.Keys.Home | WinForms.Keys.Control] = new DialogKeyHandler(NavigateFirst);
			AForm.DialogKeys[WinForms.Keys.End | WinForms.Keys.Control] = new DialogKeyHandler(NavigateLast);
			AForm.DialogKeys[WinForms.Keys.Escape] = new DialogKeyHandler(CancelForm);
		}

		private bool CancelForm(WinForms.Form AForm, WinForms.Keys AKey)
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

		private bool NavigatePrior(WinForms.Form AForm, WinForms.Keys AKey)
		{
			if (MainViewCanNavigate)
			{
				MainView.Prior();
				return true;
			}
			else
				return false;
		}

		private bool NavigateNext(WinForms.Form AForm, WinForms.Keys AKey)
		{
			if (MainViewCanNavigate)
			{
				MainView.Next();
				return true;
			}
			else
				return false;
		}

		private bool NavigatePriorPage(WinForms.Form AForm, WinForms.Keys AKey)
		{
			if (MainViewCanNavigate)
			{
				MainView.MoveBy((MainView.BufferCount - 1) * -1);
				return true;
			}
			else
				return false;
		}

		private bool NavigateNextPage(WinForms.Form AForm, WinForms.Keys AKey)
		{
			if (MainViewCanNavigate)
			{
				MainView.MoveBy((MainView.BufferCount - 1));
				return true;
			}
			else
				return false;
		}

		private bool NavigateFirst(WinForms.Form AForm, WinForms.Keys AKey)
		{
			if (MainViewCanNavigate)
			{
				MainView.First();
				return true;
			}
			else
				return false;
		}

		private bool NavigateLast(WinForms.Form AForm, WinForms.Keys AKey)
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

		private bool FAutoSize = true;
		/// <summary> Specifies whether the form should initially auto-size itself. </summary>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public bool AutoSize 
		{ 
			get { return FAutoSize; }
			set 
			{
				if (FAutoSize != value)
				{
					FAutoSize = value;
					if (Active)
						InternalUpdateAutoSize();
				}
			}
		}

		protected virtual void InternalUpdateAutoSize()
		{
			FForm.AutoResize = FAutoSize;
		}
	
		public void RootLayout()
		{
			if (Active)
				FForm.PerformLayout();
		}



		#endregion

		#region Close

		public virtual bool FormClosing()
		{
			bool LCancel = false;
			try
			{
				if (FForm != null)
				{
					if (FForm.DialogResult == WinForms.DialogResult.OK)	
					{
						try
						{
							PostChanges();
						}
						catch
						{
							LCancel = true;
							FForm.DialogResult = WinForms.DialogResult.None;
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
			return LCancel;
		}

		private void FormClosed(object ASender, EventArgs AArgs)
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
						if (FForm.DialogResult == WinForms.DialogResult.OK)
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
								if (FOnCloseForm != null)
								{
									FOnCloseForm(this);
									FOnCloseForm = null;
								}
							}
							finally
							{
								if ((FForm == null) || !FForm.Modal)  // Some forms (such as the main form) may be truly modal.
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
				if (FForm != null)
					FForm.DialogResult = WinForms.DialogResult.Cancel;
			}
		}

		protected void EnsureSearchControlTimerElapsed(INode ANode)
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
					if ((FMode == FormMode.Delete) && (MainSource != null))
						MainSource.DataView.Delete();
				}
				finally
				{
					if (FOnAcceptForm != null)
						FOnAcceptForm(this);
				}
			}
		}

		protected virtual void FormRejected()
		{
			try
			{
				if (((FMode == FormMode.Edit) || (FMode == FormMode.Insert)) && (MainSource != null))
					MainSource.DataView.Cancel();
			}
			finally
			{
				if (FOnRejectForm != null)
					FOnRejectForm(this);
			}
		}

		private void UpdateActiveControl(IWindowsElement ANode)
		{
			FForm.SetHintText((ANode != null) ? ANode.GetHint() : String.Empty);
			FForm.UpdateStatusText();
		}

		public event EventHandler OnClosed;

		public bool Close(CloseBehavior ABehavior)
		{
			if (FForm != null)
				FForm.Close(ABehavior);
			return FForm == null;	// will be null of close succussfully completed (this should never be true for a windows form because closing will not happen until the next message loop iteration)
		}

		#endregion

		#region Show

		public bool IsChildModal()
		{
			return (FOnAcceptForm != null) || (FOnRejectForm != null);
		}

		private void CheckNotChildModal()
		{
			if (IsChildModal())
				throw new ClientException(ClientException.Codes.FormAlreadyModal);
		}

		// Accept/Reject is tied to the data edit state and modal state (someone waiting on accept/reject)

		private FormMode FMode;
		[Browsable(false)]
		public FormMode Mode { get { return FMode; } }

		/// <remarks> The form interface must be active before calling this. </remarks>
		private void SetMode(FormMode AMode)
		{
			Form.SuspendLayout();
			try
			{
				FMode = AMode;
				if ((FMode != FormMode.None) && ((MainSource == null) || (MainSource.DataView == null)))
					throw new ClientException(ClientException.Codes.MainSourceNotSpecified, AMode.ToString());
				FForm.EnterNavigates = (FMode == FormMode.Insert) || (FMode == FormMode.Edit);
				switch (FMode)
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

		public void Show(FormMode AFormMode)
		{
			Show(null, null, null, AFormMode);
		}

		public WinForms.DialogResult ShowModal(FormMode AMode)
		{
			CheckNotChildModal();
			SetMode(AMode);
			try
			{			
				AcceptRejectChanged();
				HostNode.Session.Forms.Add(this);
				return FForm.ShowDialog();
			}
			catch
			{
				if ((FMode == FormMode.Edit) || (FMode == FormMode.Insert))
					MainSource.DataView.Cancel();
				throw;
			}
		}

		public void Show(FormInterfaceHandler AOnCloseForm)
		{
			CheckNotChildModal();
			FOnCloseForm = AOnCloseForm;
			try
			{
				SetMode(FormMode.None);
				HostNode.Session.Forms.Add(this);
				FForm.Show();
			}
			catch
			{
				FOnCloseForm = null;
				throw;
			}
		}

		private FormInterfaceHandler FOnCloseForm;
		private FormInterfaceHandler FOnAcceptForm;
		private FormInterfaceHandler FOnRejectForm;

		public void Show(IFormInterface AParentForm, FormInterfaceHandler AOnAcceptForm, FormInterfaceHandler AOnRejectForm, FormMode AMode)
		{
			CheckNotChildModal();
			FOnAcceptForm = AOnAcceptForm;
			FOnRejectForm = AOnRejectForm;
			try
			{
				SetMode(AMode);
				try
				{
					if (AParentForm != null)
						HostNode.Session.Forms.AddModal(this, AParentForm);
					else
						HostNode.Session.Forms.Add(this);
					AcceptRejectChanged();
					FForm.Show();
				}
				catch
				{
					if ((FMode == FormMode.Edit) || (FMode == FormMode.Insert))
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
			FOnAcceptForm = null;
			FOnRejectForm = null;
			FMode = FormMode.None;
		}

		#endregion

		#region IsLookup

		private bool FIsLookup;
		
		/// <remarks> Only effectual on show. </remarks>
		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public bool IsLookup
		{
			get { return FIsLookup; }
			set 
			{ 
				if (FIsLookup != value)
				{
					FIsLookup = value;
					if (Active) 
						InternalUpdateIsLookup();
				}
			}
		}
		
		private void InternalUpdateIsLookup()
		{
			FForm.IsLookup = FIsLookup;
		}
		
		#endregion
		
		#region Enabled / Disabled

		private int FDisableCount;

		public virtual void Enable()
		{
			FDisableCount--;
			if (Active)
				InternalUpdateEnabled();
		}

		public virtual void Disable(IFormInterface AForm)
		{
			FDisableCount++;
			if (Active)
				InternalUpdateEnabled();

			IWindowsFormInterface LForm = AForm as IWindowsFormInterface;
			if (LForm != null)
			{
				LForm.Form.Owner = (WinForms.Form)Form;
				// LForm.Form.ShowInTaskbar = false;	this would be okay, except that alt-tab doesn't seem to work unless the owned form is also in the task bar
			}
		}

		public virtual bool GetEnabled()
		{
			return FDisableCount <= 0;
		}

		protected virtual void InternalUpdateEnabled()
		{
			bool LIsEnabled = GetEnabled();
			// WinForms form handling seems to re-create the handle when the ShowInTaskbar property is toggled
			// Setting the taskbar before the enabled made the form disable properly.  -- Bryan
			// disabled again, besides the proplem above there is a huge amount of eyesore flickering happening.
//			Form.ShowInTaskbar = LIsEnabled;
			Form.Enabled = LIsEnabled;
		}

		#endregion

		#region IUpdateHandler

		public override void BeginUpdate()
		{
			if (Active)
			{
				FForm.BeginUpdate();
				FForm.SuspendLayout();
			}
		}

		public void EndUpdate()
		{
			EndUpdate(true);
		}

		public override void EndUpdate(bool APerformLayout)
		{
			if (Active)
			{
				FForm.EndUpdate();
				FForm.ResumeLayout(APerformLayout);
			}
		}

		#endregion

		#region IWindowsContainerElement

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public virtual WinForms.Control Control
		{
			get { return FForm.ContentPanel; }
		}

		#endregion
	
		#region Element

		// Hint / ToolTip

		protected override void InternalUpdateToolTip()
		{
			SetToolTip((WinForms.Control)FForm);
		}

		// Setting visible on the form is equivilant to showing which we handle differently
//		protected override void InternalUpdateVisible() 

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

		protected override Size InternalMinSize
		{
			get
			{
				Size LResult = base.InternalMinSize;
				// constrain to the minimum required of the form.
				ConstrainMin(ref LResult, new Size(CMinWidth, CMinHeight));
				return LResult;
			}
		}

		protected override Size InternalNaturalSize
		{
			get
			{
				Size LNatural = base.InternalNaturalSize;
				Size LMaxSize = InternalMaxSize;
				Size LMinSize = InternalMinSize;
				ConstrainMin(ref LMaxSize, LMinSize);	// make sure we don't allow the max to be below the min
				ConstrainMin(ref LNatural, LMinSize);
				ConstrainMax(ref LNatural, LMaxSize);
				return LNatural;
			}
		}

		/// <summary> Text becomes the caption of the Form. </summary>
		protected override void InternalUpdateText()
		{
			FForm.Text = GetText();
		}

		/// <summary> Estemates the Minimum size of the overall form. </summary>
		/// <remarks> This is only an estimate because actually resizing the form may cause menu wrapping and cause the size to change. </remarks>
		public virtual Size FormMinSize()
		{
			CheckActive();
			return MinSize + FForm.GetBorderSize();
		}

		/// <summary> Estemates the Natural size of the overall form. </summary>
		/// <remarks> This is only an estimate because actually resizing the form may cause menu wrapping and cause the size to change. </remarks>
		public virtual Size FormNaturalSize()
		{
			CheckActive();
			return NaturalSize + FForm.GetBorderSize();
		}

		/// <summary> Estemates the Max size of the overall form. </summary>
		/// <remarks> This is only an estimate because actually resizing the form may cause menu wrapping and cause the size to change. </remarks>
		public virtual Size FormMaxSize()
		{
			CheckActive();
			return MaxSize + FForm.GetBorderSize();
		}

		#endregion

		#region IErrorSource Members

		public void ErrorHighlighted(Exception AException)
		{
			// Nothing
		}

		public void ErrorSelected(Exception AException)
		{
			if (FForm != null)
				FForm.Activate();
		}

		#endregion
	}

	public class AcceleratorManager
	{
		private const int CFirstAcceleratorOffset = 0x30;
		private const int CAcceleratorRange = 0x4A;

		public AcceleratorManager()
		{
			FAccelerators = new System.Collections.BitArray(CAcceleratorRange, false);
		}

		private System.Collections.BitArray FAccelerators;

		public void Reset()
		{
			FAccelerators.SetAll(false);
		}

		internal bool Allocate(char AChar)
		{
			int LIndex = (int)Char.ToUpper(AChar) - CFirstAcceleratorOffset;
			if ((LIndex > 0) && (LIndex < CAcceleratorRange) && !FAccelerators[LIndex])
			{
				FAccelerators[LIndex] = true;
				return true;
			}
			else
				return false;
		}

		// This Method will try to allocate an accelorator if it can. (for use internally and with Groupers and Pushers)
		private string AllocateTry(string AText)
		{
			if (AText != String.Empty)
			{
				int LPos;
				System.Text.StringBuilder LResult = new System.Text.StringBuilder(AText.Length);
				bool LSatisfied = false;

				// First look to see if the "desired" accellerator is available
				for (LPos = 0; LPos < AText.Length; LPos++)
				{
					if (AText[LPos] == '&') 
						if (LPos < (AText.Length - 1))
						{
							if (AText[LPos + 1] == '&')	// skip escaped ampersands
							{
								LResult.Append("&");
								LPos++;
							}
							else
								if (!LSatisfied && Allocate(AText[LPos + 1]))
									LSatisfied = true;
								else
									continue;
						}
						else
							continue;
					LResult.Append(AText[LPos]);
				}
				if (LSatisfied)
					return LResult.ToString();
				else
					AText = LResult.ToString();

				// Step through the characters until we find an elligable accelerator
				for (LPos = 0; LPos < AText.Length; LPos++)
				{
					if (Allocate(AText[LPos]))
						return AText.Insert(LPos, "&");
				}

				// No accelerators were found, try to use an appended number
				for (LPos = 0; LPos < 10; LPos++)
					if (Allocate((char)(LPos + CFirstAcceleratorOffset)))
						return String.Format("{0} &{1}", AText, (char)(LPos + CFirstAcceleratorOffset));
			}

			// Unable to accelerate, use plain text
			return AText;
		}

		/// <summary> Attempts to allocate accellerated text based on the given string. </summary>
		/// <param name="AText"> This string may contain one or more ampersands followed by alphanumerics.  
		/// Each ampersand preceeded character will be tested for accellerator availability.  If the 
		/// character is not available or an accellerator has already been successfully allocated, the 
		/// ampersand will be stripped off.  Literal ampersands can be specified using a double ampersand 
		/// escape.</param>
		/// <param name="ATryIfNotRequested"> If true, then an attempt will be made to accellerate the 
		/// text regardless of whether or not the text contains a requested accellerator. </param>
		/// <returns> Text with any successfully allocated accellerator, and all others stripped off. </returns>
		public string Allocate(string AText, bool ATryIfNotRequested)
		{
			if ((AText.Length > 0) && (AText[0] == '~'))
				return AText.Substring(1).Replace("&", "&&");
			else
			{
				if (ATryIfNotRequested)
					return AllocateTry(AText);
				else
				{
					for (int i = 0; i < AText.Length; i++)
						if ((AText[i] == '&') && ((i == (AText.Length - 1)) || (AText[i + 1] != '&')))
							return AllocateTry(AText);
					return AText;	// no accellerators requested
				}
			}
		}

		internal void Deallocate(char AChar)
		{
			FAccelerators[(int)Char.ToUpper(AChar) - CFirstAcceleratorOffset] = false;
		}

		public void Deallocate(string AText)
		{
			for (int i = 0; i < AText.Length; i++)
				if ((AText[i] == '&') && ((i == (AText.Length - 1)) || (AText[i + 1] != '&')))
				{
					Deallocate(AText[i + 1]);
					return;
				}
		}
	}

	public class ProcessPendingSearchEvent : NodeEvent
	{
		public override void Handle(INode ANode)
		{
			if (ANode is IWindowsSearch)
				((IWindowsSearch)ANode).SearchControl.ProcessPending();
		}
	}
}
