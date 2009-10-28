/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;

using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public enum AcceptRejectState {None, True, False};

	[PublishAs("Interface")]
	[ListInDesigner(false)]
	[DesignerImage("Image('Frontend', 'Nodes.Interface')")]
	public class FormInterface : Interface, ISilverlightFormInterface
	{
		// Min and max height are in terms root element size
		public const int CMinWidth = 310;
		public const int CMinHeight = 4;

		public const int CMarginLeft = 4;
		public const int CMarginRight = 4;
		public const int CMarginTop = 6;
		public const int CMarginBottom = 6;

		public FormInterface()
		{
			FBindErrors = new DispatchedReadOnlyCollection<Exception>(FInternalErrors);
		}
		
		#region ISilverlightFormInterface
		
		private ObservableCollection<Exception> FInternalErrors = new ObservableCollection<Exception>();
		
		private DispatchedReadOnlyCollection<Exception> FBindErrors;
		[Browsable(false)]
		public DispatchedReadOnlyCollection<Exception> BindErrors 
		{ 
			get { return FBindErrors; }
		}

		public virtual void EmbedErrors(ErrorList AErrorList)
		{
			foreach (Exception LError in AErrorList)
				FInternalErrors.Add(LError);
		}
		
		public virtual void ClearErrors()
		{
			FInternalErrors.Clear();
		}

		#endregion

		#region Accept / Reject

		// IsAcceptReject

		private bool FForceAcceptReject;
		[DefaultValue(false)]
		public bool ForceAcceptReject
		{
			get { return FForceAcceptReject; }
			set
			{
				if (FForceAcceptReject != value)
				{
					FForceAcceptReject = value;
					UpdateAcceptReject();
				}
			}
		}

		private void UpdateAcceptReject()
		{
			UpdateBinding(FormControl.IsAcceptRejectProperty);
		}
		
		private object UIGetIsAcceptReject()
		{
			return IsAcceptReject;
		}
		
		public bool IsAcceptReject
		{
			get 
			{ 
				return 
					ForceAcceptReject 
						|| (FMode != FormMode.None) 
						|| 
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

		protected override void MainSourceStateChanged(DAE.Client.DataLink ALink, DAE.Client.DataSet ADataSet)
		{
			UpdateAcceptReject();
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

		#endregion

		#region Form actions

		protected DAE.Client.DataView MainView
		{
			get { return (MainSource == null ? null : MainSource.DataView); }
		}

		protected bool MainViewCanNavigate
		{
			get { return (MainView != null) && MainView.Active && (MainView.State == DAE.Client.DataSetState.Browse); }
		}

		public bool CancelForm()
		{
			Close(CloseBehavior.RejectOrClose);
			return true;
		}

		public bool NavigatePrior()
		{
			if (MainViewCanNavigate)
			{
				MainView.Prior();
				return true;
			}
			else
				return false;
		}

		public bool NavigateNext()
		{
			if (MainViewCanNavigate)
			{
				MainView.Next();
				return true;
			}
			else
				return false;
		}

		public bool NavigatePriorPage()
		{
			if (MainViewCanNavigate)
			{
				MainView.MoveBy((MainView.BufferCount - 1) * -1);
				return true;
			}
			else
				return false;
		}

		public bool NavigateNextPage()
		{
			if (MainViewCanNavigate)
			{
				MainView.MoveBy((MainView.BufferCount - 1));
				return true;
			}
			else
				return false;
		}

		public bool NavigateFirst()
		{
			if (MainViewCanNavigate)
			{
				MainView.First();
				return true;
			}
			else
				return false;
		}

		public bool NavigateLast()
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
		
		#region Close
		
		/// <summary> Callback event from the main thread that the user is attempting to close the form. </summary>
		private void FormCloseRequested(object ASender, CloseBehavior ABehavior)
		{
			Session.Invoke((System.Action)(() => { Close(ABehavior); }));
		}

		/// <summary> Requests that the form close. </summary>
		/// <returns> True if the form is closed. </returns>
		public bool Close(CloseBehavior ABehavior)
		{
			if (Closing(ABehavior))
				return Closed(ABehavior);
			else
				return false;
		}

		public override void PostChanges()
		{
			base.PostChanges();
			if ((FMode == FormMode.Delete) && (MainSource != null))
				MainSource.DataView.Delete();
			EnsureSearchControlTimerElapsed(this);
		}

		public override void CancelChanges()
		{
			base.CancelChanges();
			if (((FMode == FormMode.Edit) || (FMode == FormMode.Insert)) && (MainSource != null))
				MainSource.DataView.Cancel();
		}

		public virtual bool Closing(CloseBehavior ABehavior)
		{
			try
			{
				if (ABehavior == CloseBehavior.AcceptOrClose)	
				{
					PostChanges();
					return true;
				}
				else
				{
					CancelChanges();
					return true;
				}
			}
			catch (Exception AException)
			{
				this.HandleException(AException);
				return false;
			}
		}

		public virtual bool Closed(CloseBehavior ABehavior)
		{
			var LSession = (Silverlight.Session)HostNode.Session;
			try
			{
				bool LEndOfStack = true;
				try
				{
					LEndOfStack = LSession.Forms.Remove(this);
				}
				finally
				{
					try
					{
						if (ABehavior == CloseBehavior.AcceptOrClose)
							FormAccepted();
						else
							FormRejected();
					}
					finally
					{
						try
						{
							Session.DispatcherInvoke((System.Action)(() => { if (Form != null) LSession.Close(Form); }));

							if (OnClosed != null)
								OnClosed(this, EventArgs.Empty);
						}
						finally
						{
							EndChildModal();
							LSession.DisposeFormHost(HostNode, LEndOfStack);
						}
					}
				}
				return true;
			}
			catch (Exception AException)
			{
				this.HandleException(AException);
				return false;
			}
		}

		protected void EnsureSearchControlTimerElapsed(INode ANode)
		{
			BroadcastEvent(new ProcessPendingSearchEvent());
		}

		protected virtual void FormAccepted()
		{
			if (FOnAcceptForm != null)
				FOnAcceptForm(this);
		}

		protected virtual void FormRejected()
		{
			if (FOnRejectForm != null)
				FOnRejectForm(this);
		}

		public event EventHandler OnClosed;

		#endregion

		#region Show

		// Accept/Reject is tied to the data edit state and modal state (someone waiting on accept/reject)

		private FormInterfaceHandler FOnCloseForm;
		private FormInterfaceHandler FOnAcceptForm;
		private FormInterfaceHandler FOnRejectForm;

		private FormMode FMode;
		public FormMode Mode { get { return FMode; } }

		/// <remarks> The form interface must be active before calling this. </remarks>
		private void SetMode(FormMode AMode)
		{
			FMode = AMode;
			if ((FMode != FormMode.None) && ((MainSource == null) || (MainSource.DataView == null)))
				throw new ClientException(ClientException.Codes.MainSourceNotSpecified, AMode.ToString());
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
			UpdateAcceptReject();
		}

		public void Show()
		{
			Show(FormMode.None);
		}

		public void Show(FormMode AFormMode)
		{
			Show(null, null, null, AFormMode);
		}

		public void Show(FormInterfaceHandler AOnCloseForm)
		{
			FOnCloseForm = AOnCloseForm;
			try
			{
				SetMode(FormMode.None);
				var LSession = (Silverlight.Session)HostNode.Session;
				LSession.Forms.Add(this);
				if (Form != null)
					LSession.Show(Form, null);
			}
			catch
			{
				FOnCloseForm = null;
				throw;
			}
		}

		public void Show(IFormInterface AParentForm, FormInterfaceHandler AOnAcceptForm, FormInterfaceHandler AOnRejectForm, FormMode AMode)
		{
			FOnAcceptForm = AOnAcceptForm;
			FOnRejectForm = AOnRejectForm;
			try
			{
				SetMode(AMode);
				try
				{
					var LSession = (Silverlight.Session)HostNode.Session;
					if (AParentForm != null)
						LSession.Forms.AddModal(this, AParentForm);
					else
						LSession.Forms.Add(this);
					if (Form != null)
						LSession.Show(Form, AParentForm == null ? null : ((ISilverlightFormInterface)AParentForm).Form);
				}
				catch
				{
					CancelChanges();
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

		#region Enabled / Disabled

		private int FDisableCount;
		
		// Enabled

		public override bool GetEnabled()
		{
			return FDisableCount == 0;
		}
		
		public virtual void Enable()
		{
			FDisableCount = Math.Max(0, FDisableCount - 1);
			if (FDisableCount == 0)
				UpdateBinding(Control.IsEnabledProperty);
		}

		public virtual void Disable(IFormInterface AForm)
		{
			FDisableCount++;
			if (FDisableCount == 1)
				UpdateBinding(Control.IsEnabledProperty);
		}

		#endregion

		#region Element
		
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

		protected override FrameworkElement CreateFrameworkElement()
		{
			return new FormControl();
		}

		public FormControl Form { get { return FrameworkElement as FormControl; } }

		protected override void InitializeFrameworkElement()
		{
			base.InitializeFrameworkElement();
			Form.CloseRequested += FormCloseRequested;
		}
		
		protected override void RegisterBindings()
		{
			base.RegisterBindings();
			AddBinding(FormControl.TitleProperty, new Func<object>(UIGetText));
			AddBinding(FormControl.IsAcceptRejectProperty, new Func<object>(UIGetIsAcceptReject));
		}
		
		private object UIGetText()
		{
			return GetText();
		}

		protected override void UpdateText()
		{
			UpdateBinding(FormControl.TitleProperty);
		}

		#endregion
	}

	public class ProcessPendingSearchEvent : NodeEvent
	{
		public override void Handle(INode ANode)
		{
			//if (ANode is ISilverlightSearch)
			//    ((ISilverlightSearch)ANode).SearchControl.ProcessPending();
		}
	}
}
