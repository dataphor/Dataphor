/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;

using Alphora.Dataphor.BOP;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Abstract class for interfaces. </summary>
	public abstract class Interface : ContentElement, IInterface
	{
		protected override void Dispose(bool disposed)
		{
			try
			{
				base.Dispose(disposed);
			}
			finally
			{
				OnDefault = null;
				OnShown = null;
				OnActivate = null;
				OnAfterActivate = null;
				OnBeforeDeactivate = null;
				OnCancel = null;
				OnPost = null;
				MainSource = null;
			}
		}

		// Text

		private string _text = String.Empty;
		[DefaultValue("")]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					UpdateText();
				}
			}
		}

		public virtual string GetText()
		{
			return _text;
		}

		protected virtual void UpdateText() 
		{
		}

		// UserState

		private IndexedDictionary<string, object> _userState = new IndexedDictionary<string, object>();
		[Browsable(false)]
		public IndexedDictionary<string, object> UserState
		{
			get { return _userState; }
		}

		// MainSource

		private ISource _mainSource;
		/// <summary> The source for the primary DataView of this interface. </summary>
		public ISource MainSource
		{
			get { return _mainSource; }
			set
			{
				if (_mainSource != value)
				{
					if (_mainSource != null)
					{
						_mainSource.Disposed -= new EventHandler(MainSourceDisposed);
						_mainSource.StateChanged -= new DAE.Client.DataLinkHandler(MainSourceStateChanged);
					}
					_mainSource = value;
					if (_mainSource != null)
					{
						_mainSource.Disposed += new EventHandler(MainSourceDisposed);
						_mainSource.StateChanged += new DAE.Client.DataLinkHandler(MainSourceStateChanged);
					}
				}
			}
		}

		private void MainSourceDisposed(object sender, EventArgs args)
		{
			MainSource = null;
		}

		protected virtual void MainSourceStateChanged(DAE.Client.DataLink link, DAE.Client.DataSet dataSet) {}

		/// <summary> Use to ensure the existence of a MainSource. </summary>
		public void CheckMainSource()
		{
			if (MainSource == null)
				throw new ClientException(ClientException.Codes.MainSourceRequired, HostNode.Document);
		}

		public virtual void PostChanges()
		{
			BroadcastEvent(new ViewActionEvent(SourceActions.Post));
		}

        public virtual void PostChangesIfModified()
        {
            BroadcastEvent(new ViewActionEvent(SourceActions.PostIfModified));
        }

		public virtual void CancelChanges()
		{
			BroadcastEvent(new ViewActionEvent(SourceActions.Cancel));
		}


		// PerformDefaultAction

		public abstract void PerformDefaultAction();

		// OnDefault

		private IAction _onDefault;
		public IAction OnDefault 
		{ 
			get { return _onDefault; }
			set
			{
				if (_onDefault != value)
				{
					if (_onDefault != null)
						_onDefault.Disposed -= new EventHandler(DefaultActionDisposed);
					_onDefault = value;
					if (_onDefault != null)
						_onDefault.Disposed += new EventHandler(DefaultActionDisposed);
					DefaultChanged();
				}
			}
		}

		protected virtual void DefaultChanged()
		{
		}

		private void DefaultActionDisposed(object sender, EventArgs args)
		{
			OnDefault = null;
		}

		// OnShown

		private IAction _onShown;
		public IAction OnShown
		{
			get { return _onShown; }
			set
			{
				if (_onShown != value)
				{
					if (_onShown != null)
						_onShown.Disposed -= new EventHandler(OnShownActionDisposed);
					_onShown = value;
					if (_onShown != null)
						_onShown.Disposed += new EventHandler(OnShownActionDisposed);
				}
			}
		}

		private void OnShownActionDisposed(object sender, EventArgs args)
		{
			OnShown = null;
		}

		// OnPost

		private IAction _onPost;
		public IAction OnPost
		{
			get { return _onPost; }
			set
			{
				if (_onPost != value)
				{
					if (_onPost != null)
						_onPost.Disposed -= new EventHandler(OnPostDisposed);
					_onPost = value;
					if (_onPost != null)
						_onPost.Disposed += new EventHandler(OnPostDisposed);
				}
			}
		}

		private void OnPostDisposed(object sender, EventArgs args)
		{
			OnPost = null;
		}

		// OnCancel

		private IAction _onCancel;
		public IAction OnCancel
		{
			get { return _onCancel; }
			set
			{
				if (_onCancel != value)
				{
					if (_onCancel != null)
						_onCancel.Disposed -= new EventHandler(OnCancelDisposed);
					_onCancel = value;
					if (_onCancel != null)
						_onCancel.Disposed += new EventHandler(OnCancelDisposed);
				}
			}
		}

		private void OnCancelDisposed(object sender, EventArgs args)
		{
			OnCancel = null;
		}

		// OnActivate

		private IAction _onActivate;
		public IAction OnActivate
		{
			get { return _onActivate; }
			set
			{
				if (_onActivate != value)
				{
					if (_onActivate != null)
						_onActivate.Disposed -= new EventHandler(OnActivateDisposed);
					_onActivate = value;
					if (_onActivate != null)
						_onActivate.Disposed += new EventHandler(OnActivateDisposed);
				}
			}
		}

		private void OnActivateDisposed(object sender, EventArgs args)
		{
			OnActivate = null;
		}

		// OnAfterActivate

		private IAction _onAfterActivate;
		public IAction OnAfterActivate
		{
			get { return _onAfterActivate; }
			set
			{
				if (_onAfterActivate != value)
				{
					if (_onAfterActivate != null)
						_onAfterActivate.Disposed -= new EventHandler(OnAfterActivateDisposed);
					_onAfterActivate = value;
					if (_onAfterActivate != null)
						_onAfterActivate.Disposed += new EventHandler(OnAfterActivateDisposed);
				}
			}
		}

		private void OnAfterActivateDisposed(object sender, EventArgs args)
		{
			OnAfterActivate = null;
		}

		// OnBeforeDeactivate

		private IAction _onBeforeDeactivate;
		public IAction OnBeforeDeactivate
		{
			get { return _onBeforeDeactivate; }
			set
			{
				if (_onBeforeDeactivate != value)
				{
					if (_onBeforeDeactivate != null)
						_onBeforeDeactivate.Disposed -= new EventHandler(OnBeforeDeactivateDisposed);
					_onBeforeDeactivate = value;
					if (_onBeforeDeactivate != null)
						_onBeforeDeactivate.Disposed += new EventHandler(OnBeforeDeactivateDisposed);
				}
			}
		}

		private void OnBeforeDeactivateDisposed(object sender, EventArgs args)
		{
			OnBeforeDeactivate = null;
		}

		// Node

		public override void HandleEvent(NodeEvent eventValue)
		{
			base.HandleEvent(eventValue);
			if (!eventValue.IsHandled)
				if (eventValue is ViewActionEvent)
				{
					switch (((ViewActionEvent)eventValue).Action)
					{
						case (SourceActions.Post) :
							if (_onPost != null)
								_onPost.Execute(this, new EventParams());
							break;
						case (SourceActions.Cancel) :
							if (_onCancel != null)
								_onCancel.Execute(this, new EventParams());
							break;
                        case (SourceActions.PostIfModified):
                            if (_onPost != null)
                                _onPost.Execute(this, new EventParams());
                            break;
					}
				}
				else if ((eventValue is FormShownEvent) && (_onShown != null))
					_onShown.Execute();
		}

		protected override void Activate()
		{
			try
			{
				if (OnActivate != null)
					OnActivate.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				this.HandleException(exception);
			}
			UpdateText();
			base.Activate();
		}

		protected internal override void AfterActivate()
		{
			base.AfterActivate();
			try
			{
				if (OnAfterActivate != null)
					OnAfterActivate.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				this.HandleException(exception);
			}
		}

		protected internal override void BeforeDeactivate()
		{
			try
			{
				if (OnBeforeDeactivate != null)
					OnBeforeDeactivate.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				this.HandleException(exception);
			}
			base.BeforeDeactivate();
		}

		public override bool IsValidChild(Type childType)
		{
			if (typeof(ISilverlightElement).IsAssignableFrom(childType))
				return RootElement == null;
			return true;
		}
	}
}