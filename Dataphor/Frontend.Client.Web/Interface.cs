/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Drawing;

using Alphora.Dataphor.Frontend.Client;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public abstract class Interface : SingleElementContainer, IInterface
	{
		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);
			}
			finally
			{
				MainSource = null;
				OnDefault = null;
				OnShown = null;
				OnPost = null;
				OnCancel = null;
				OnActivate = null;
				OnAfterActivate = null;
				OnBeforeDeactivate = null;
			}
		}

		// Text

		private string _text = String.Empty;
		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		public virtual string GetText()
		{
			return _text;
		}

		// UserState

		private IndexedDictionary<string, object> _userState = new IndexedDictionary<string, object>();
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
						_mainSource.Disposed -= new EventHandler(MainSourceDisposed);
					_mainSource = value;
					if (_mainSource != null)
						_mainSource.Disposed += new EventHandler(MainSourceDisposed);
				}
			}
		}

		private void MainSourceDisposed(object sender, EventArgs args)
		{
			MainSource = null;
		}

		// CheckMainSource

		public void CheckMainSource()
		{
			if (MainSource == null)
				throw new ClientException(ClientException.Codes.MainSourceRequired, HostNode.Document);
		}

		// BackgroundImage

		private string _backgroundImage = String.Empty;
		public string BackgroundImage
		{
			get { return _backgroundImage; }
			set 
			{ 
				_backgroundImage = value;
				UpdateBackgroundImage();
			}
		}

		private void UpdateBackgroundImage()
		{
			ImageCache cache = WebSession.ImageCache;
			cache.Deallocate(_backgroundImageID);
			if (Active)
				_backgroundImageID = cache.Allocate(_backgroundImage);
			else
				_backgroundImageID = String.Empty;
		}

		private string _backgroundImageID = String.Empty;
		public string BackgroundImageID
		{
			get { return _backgroundImageID; }
		}

		// IconImage

		protected string _iconImage = String.Empty;
		public string IconImage
		{
			get { return _iconImage; }
			set 
			{ 
				_iconImage = value; 
				UpdateIconImage();
			}
		}

		private void UpdateIconImage()
		{
			if (Active)
			{
				ImageCache cache = WebSession.ImageCache;
				cache.Deallocate(_iconImageID);
				if (Active)
					_iconImageID = cache.Allocate(_iconImage);
				else
					_iconImageID = String.Empty;
			}
		}

		private string _iconImageID = String.Empty;
		public string IconImageID
		{
			get { return _iconImageID; }
		}

		// PerformDefaultAction

		public virtual void PerformDefaultAction()
		{
			// TODO: Default action handling
		}

		// OnDefault

		// TODO: Utilize the default action for enter key

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
				}
			}
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
						_onShown.Disposed -= new EventHandler(ShownActionDisposed);
					_onShown = value;
					if (_onShown != null)
						_onShown.Disposed += new EventHandler(ShownActionDisposed);
				}
			}
		}

		private void ShownActionDisposed(object sender, EventArgs args)
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

		public void PostChanges()
		{
			BroadcastEvent(new ViewActionEvent(SourceActions.Post));
		}

        public void PostChangesIfModified()
        {
            BroadcastEvent(new ViewActionEvent(SourceActions.PostIfModified));
        }

		public void CancelChanges()
		{
			BroadcastEvent(new ViewActionEvent(SourceActions.Cancel));
		}

		// RootElement

		public IWebElement RootElement
		{
			get { return Child; }
		}

		// Element

		public override int GetDefaultMarginLeft()
		{
			return 0;
		}

		public override int GetDefaultMarginRight()
		{
			return 0;
		}

		public override int GetDefaultMarginTop()
		{
			return 0;
		}

		public override int GetDefaultMarginBottom()
		{
			return 0;
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			return !typeof(IWebElement).IsAssignableFrom(childType) || (RootElement == null);
		}

		public override void HandleEvent(NodeEvent eventValue)
		{
			if (eventValue is ViewActionEvent)
			{
				switch (((ViewActionEvent)eventValue).Action)
				{
					case (SourceActions.Post) :
						if (OnPost != null)
							OnPost.Execute();
						break;
					case (SourceActions.Cancel) :
						if (OnCancel != null)
							OnCancel.Execute();
						break;
				}
			}
			base.HandleEvent(eventValue);
		}

		protected override void Activate()
		{
			base.Activate();
			UpdateIconImage();
			UpdateBackgroundImage();
			try
			{
				if (OnActivate != null)
					OnActivate.Execute();
			}
			catch (Exception exception)
			{
				WebSession.ErrorList.Add(exception);
			}
		}

		protected override void AfterActivate()
		{
			base.AfterActivate();
			try
			{
				if (OnAfterActivate != null)
					OnAfterActivate.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				WebSession.ErrorList.Add(exception);
			}
		}

		protected override void BeforeDeactivate()
		{
			try
			{
				if (OnBeforeDeactivate != null)
					OnBeforeDeactivate.Execute(this, new EventParams());
			}
			catch (Exception exception)
			{
				WebSession.ErrorList.Add(exception);
			}
			base.BeforeDeactivate();
		}

		protected override void Deactivate()
		{
			try
			{
				UpdateIconImage();
				UpdateBackgroundImage();
			}
			finally
			{
				base.Deactivate();
			}
		}
	}
}
