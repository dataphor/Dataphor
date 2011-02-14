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
		public const int MinWidth = 310;
		public const int MinHeight = 4;

		public const int MarginLeft = 4;
		public const int MarginRight = 4;
		public const int MarginTop = 6;
		public const int MarginBottom = 6;

		public FormInterface()
		{
			_bindErrors = new DispatchedReadOnlyCollection<Exception>(_internalErrors);
		}
		
		#region ISilverlightFormInterface
		
		private ObservableCollection<Exception> _internalErrors = new ObservableCollection<Exception>();
		
		private DispatchedReadOnlyCollection<Exception> _bindErrors;
		[Browsable(false)]
		public DispatchedReadOnlyCollection<Exception> BindErrors 
		{ 
			get { return _bindErrors; }
		}

		public virtual void EmbedErrors(ErrorList errorList)
		{
			foreach (Exception error in errorList)
				_internalErrors.Add(error);
		}
		
		public virtual void ClearErrors()
		{
			_internalErrors.Clear();
		}

		#endregion

		#region Accept / Reject

		// IsAcceptReject

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
						|| (_mode != FormMode.None) 
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

		protected override void MainSourceStateChanged(DAE.Client.DataLink link, DAE.Client.DataSet dataSet)
		{
			UpdateAcceptReject();
		}
		
		private bool _acceptEnabled = true;
		public bool AcceptEnabled
		{
			get { return _acceptEnabled; }
			set { _acceptEnabled = value; }
		}

		// OnBeforeAccept

		private IAction _onBeforeAccept;
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

		protected void BeforeAccept()
		{
			try
			{
				if (OnBeforeAccept != null)
					OnBeforeAccept.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				this.HandleException(exception);
			}
		}

		#endregion

		#region Default Actions
		
		public override void PerformDefaultAction()
		{
			if ((_mode == FormMode.None) && (OnDefault != null))
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
		private void FormCloseRequested(object sender, CloseBehavior behavior)
		{
			Session.Invoke((System.Action)(() => { Close(behavior); }));
		}

		public event EventHandler Accepting; 
		/// <summary> Requests that the form close. </summary>
		/// <returns> True if the form is closed. </returns>
		public bool Close(CloseBehavior behavior)
		{
			try
			{
				if (Accepting != null)
					Accepting(this, EventArgs.Empty);
			}
			catch (Exception AException)
			{
				this.HandleException(AException);
				return false;
			}
			if (AcceptEnabled)
				if (Closing(behavior))
					return Closed(behavior);
				else
					return false;
			else
				return false;
		}

		public override void PostChanges()
		{
			base.PostChanges();
			if ((_mode == FormMode.Delete) && (MainSource != null))
				MainSource.DataView.Delete();
			EnsureSearchControlTimerElapsed(this);
		}

		public override void CancelChanges()
		{
			base.CancelChanges();
			if (((_mode == FormMode.Edit) || (_mode == FormMode.Insert)) && (MainSource != null))
				MainSource.DataView.Cancel();
		}

		public virtual bool Closing(CloseBehavior behavior)
		{
			try
			{
				if (behavior == CloseBehavior.AcceptOrClose)	
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

		public virtual bool Closed(CloseBehavior behavior)
		{
			var session = (Silverlight.Session)HostNode.Session;
			try
			{
				bool endOfStack = true;
				try
				{
					endOfStack = session.Forms.Remove(this);
				}
				finally
				{
					try
					{
						if (behavior == CloseBehavior.AcceptOrClose)
							FormAccepted();
						else
							FormRejected();
					}
					finally
					{
						try
						{
							Session.DispatcherInvoke((System.Action)(() => { if (Form != null) session.Close(Form); }));

							if (OnClosed != null)
								OnClosed(this, EventArgs.Empty);
						}
						finally
						{
							EndChildModal();
							session.DisposeFormHost(HostNode, endOfStack);
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

		protected void EnsureSearchControlTimerElapsed(INode node)
		{
			BroadcastEvent(new ProcessPendingSearchEvent());
		}

		protected virtual void FormAccepted()
		{
			if (_onAcceptForm != null)
				_onAcceptForm(this);
		}

		protected virtual void FormRejected()
		{
			if (_onRejectForm != null)
				_onRejectForm(this);
		}

		public event EventHandler OnClosed;

		#endregion

		#region Show

		// Accept/Reject is tied to the data edit state and modal state (someone waiting on accept/reject)

		private FormInterfaceHandler _onCloseForm;
		private FormInterfaceHandler _onAcceptForm;
		private FormInterfaceHandler _onRejectForm;

		private FormMode _mode;
		public FormMode Mode { get { return _mode; } }

		/// <remarks> The form interface must be active before calling this. </remarks>
		private void SetMode(FormMode mode)
		{
			_mode = mode;
			if ((_mode != FormMode.None) && ((MainSource == null) || (MainSource.DataView == null)))
				throw new ClientException(ClientException.Codes.MainSourceNotSpecified, mode.ToString());
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
			UpdateAcceptReject();
		}

		public void Show()
		{
			Show(FormMode.None);
		}

		public void Show(FormMode formMode)
		{
			Show(null, null, null, formMode);
		}

		public void Show(FormInterfaceHandler onCloseForm)
		{
			_onCloseForm = onCloseForm;
			try
			{
				SetMode(FormMode.None);
				var session = (Silverlight.Session)HostNode.Session;
				session.Forms.Add(this);
				if (Form != null)
					session.Show(Form, null);
			}
			catch
			{
				_onCloseForm = null;
				throw;
			}
		}

		public void Show(IFormInterface parentForm, FormInterfaceHandler onAcceptForm, FormInterfaceHandler onRejectForm, FormMode mode)
		{
			_onAcceptForm = onAcceptForm;
			_onRejectForm = onRejectForm;
			try
			{
				SetMode(mode);
				try
				{
					var session = (Silverlight.Session)HostNode.Session;
					if (parentForm != null)
						session.Forms.AddModal(this, parentForm);
					else
						session.Forms.Add(this);
					if (Form != null)
						session.Show(Form, parentForm == null ? null : ((ISilverlightFormInterface)parentForm).Form);
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
			_onAcceptForm = null;
			_onRejectForm = null;
			_mode = FormMode.None;
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
			// TODO: implement top-most
		}

		#endregion

		#region Enabled / Disabled

		private int _disableCount;
		
		// Enabled

		public override bool GetEnabled()
		{
			return _disableCount == 0;
		}
		
		public virtual void Enable()
		{
			_disableCount = Math.Max(0, _disableCount - 1);
			if (_disableCount == 0)
				UpdateBinding(Control.IsEnabledProperty);
		}

		public virtual void Disable(IFormInterface form)
		{
			_disableCount++;
			if (_disableCount == 1)
				UpdateBinding(Control.IsEnabledProperty);
		}

		#endregion

		#region Element
		
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
		public override void Handle(INode node)
		{
			//if (ANode is ISilverlightSearch)
			//    ((ISilverlightSearch)ANode).SearchControl.ProcessPending();
		}
	}
}
