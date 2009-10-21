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
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				base.Dispose(ADisposing);
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

		private string FText = String.Empty;
		public string Text
		{
			get { return FText; }
			set { FText = value; }
		}

		public virtual string GetText()
		{
			return FText;
		}

		// UserState

		private IndexedDictionary<string, object> FUserState = new IndexedDictionary<string, object>();
		public IndexedDictionary<string, object> UserState
		{
			get { return FUserState; }
		}

		// MainSource

		private ISource FMainSource;
		/// <summary> The source for the primary DataView of this interface. </summary>
		public ISource MainSource
		{
			get { return FMainSource; }
			set
			{
				if (FMainSource != value)
				{
					if (FMainSource != null)
						FMainSource.Disposed -= new EventHandler(MainSourceDisposed);
					FMainSource = value;
					if (FMainSource != null)
						FMainSource.Disposed += new EventHandler(MainSourceDisposed);
				}
			}
		}

		private void MainSourceDisposed(object ASender, EventArgs AArgs)
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

		private string FBackgroundImage = String.Empty;
		public string BackgroundImage
		{
			get { return FBackgroundImage; }
			set 
			{ 
				FBackgroundImage = value;
				UpdateBackgroundImage();
			}
		}

		private void UpdateBackgroundImage()
		{
			ImageCache LCache = WebSession.ImageCache;
			LCache.Deallocate(FBackgroundImageID);
			if (Active)
				FBackgroundImageID = LCache.Allocate(FBackgroundImage);
			else
				FBackgroundImageID = String.Empty;
		}

		private string FBackgroundImageID = String.Empty;
		public string BackgroundImageID
		{
			get { return FBackgroundImageID; }
		}

		// IconImage

		protected string FIconImage = String.Empty;
		public string IconImage
		{
			get { return FIconImage; }
			set 
			{ 
				FIconImage = value; 
				UpdateIconImage();
			}
		}

		private void UpdateIconImage()
		{
			if (Active)
			{
				ImageCache LCache = WebSession.ImageCache;
				LCache.Deallocate(FIconImageID);
				if (Active)
					FIconImageID = LCache.Allocate(FIconImage);
				else
					FIconImageID = String.Empty;
			}
		}

		private string FIconImageID = String.Empty;
		public string IconImageID
		{
			get { return FIconImageID; }
		}

		// PerformDefaultAction

		public virtual void PerformDefaultAction()
		{
			// TODO: Default action handling
		}

		// OnDefault

		// TODO: Utilize the default action for enter key

		private IAction FOnDefault;
		public IAction OnDefault 
		{ 
			get { return FOnDefault; }
			set
			{
				if (FOnDefault != value)
				{
					if (FOnDefault != null)
						FOnDefault.Disposed -= new EventHandler(DefaultActionDisposed);
					FOnDefault = value;
					if (FOnDefault != null)
						FOnDefault.Disposed += new EventHandler(DefaultActionDisposed);
				}
			}
		}

		private void DefaultActionDisposed(object ASender, EventArgs AArgs)
		{
			OnDefault = null;
		}

		// OnShown

		private IAction FOnShown;
		public IAction OnShown
		{
			get { return FOnShown; }
			set
			{
				if (FOnShown != value)
				{
					if (FOnShown != null)
						FOnShown.Disposed -= new EventHandler(ShownActionDisposed);
					FOnShown = value;
					if (FOnShown != null)
						FOnShown.Disposed += new EventHandler(ShownActionDisposed);
				}
			}
		}

		private void ShownActionDisposed(object ASender, EventArgs AArgs)
		{
			OnShown = null;
		}

		// OnPost

		private IAction FOnPost;
		public IAction OnPost
		{
			get { return FOnPost; }
			set
			{
				if (FOnPost != value)
				{
					if (FOnPost != null)
						FOnPost.Disposed -= new EventHandler(OnPostDisposed);
					FOnPost = value;
					if (FOnPost != null)
						FOnPost.Disposed += new EventHandler(OnPostDisposed);
				}
			}
		}

		private void OnPostDisposed(object ASender, EventArgs AArgs)
		{
			OnPost = null;
		}

		// OnCancel

		private IAction FOnCancel;
		public IAction OnCancel
		{
			get { return FOnCancel; }
			set
			{
				if (FOnCancel != value)
				{
					if (FOnCancel != null)
						FOnCancel.Disposed -= new EventHandler(OnCancelDisposed);
					FOnCancel = value;
					if (FOnCancel != null)
						FOnCancel.Disposed += new EventHandler(OnCancelDisposed);
				}
			}
		}

		private void OnCancelDisposed(object ASender, EventArgs AArgs)
		{
			OnCancel = null;
		}

		// OnActivate

		private IAction FOnActivate;
		public IAction OnActivate
		{
			get { return FOnActivate; }
			set
			{
				if (FOnActivate != value)
				{
					if (FOnActivate != null)
						FOnActivate.Disposed -= new EventHandler(OnActivateDisposed);
					FOnActivate = value;
					if (FOnActivate != null)
						FOnActivate.Disposed += new EventHandler(OnActivateDisposed);
				}
			}
		}

		private void OnActivateDisposed(object ASender, EventArgs AArgs)
		{
			OnActivate = null;
		}

		// OnAfterActivate

		private IAction FOnAfterActivate;
		public IAction OnAfterActivate
		{
			get { return FOnAfterActivate; }
			set
			{
				if (FOnAfterActivate != value)
				{
					if (FOnAfterActivate != null)
						FOnAfterActivate.Disposed -= new EventHandler(OnAfterActivateDisposed);
					FOnAfterActivate = value;
					if (FOnAfterActivate != null)
						FOnAfterActivate.Disposed += new EventHandler(OnAfterActivateDisposed);
				}
			}
		}

		private void OnAfterActivateDisposed(object ASender, EventArgs AArgs)
		{
			OnAfterActivate = null;
		}

		// OnBeforeDeactivate

		private IAction FOnBeforeDeactivate;
		public IAction OnBeforeDeactivate
		{
			get { return FOnBeforeDeactivate; }
			set
			{
				if (FOnBeforeDeactivate != value)
				{
					if (FOnBeforeDeactivate != null)
						FOnBeforeDeactivate.Disposed -= new EventHandler(OnBeforeDeactivateDisposed);
					FOnBeforeDeactivate = value;
					if (FOnBeforeDeactivate != null)
						FOnBeforeDeactivate.Disposed += new EventHandler(OnBeforeDeactivateDisposed);
				}
			}
		}

		private void OnBeforeDeactivateDisposed(object ASender, EventArgs AArgs)
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

		public override bool IsValidChild(Type AChildType)
		{
			return !typeof(IWebElement).IsAssignableFrom(AChildType) || (RootElement == null);
		}

		public override void HandleEvent(NodeEvent AEvent)
		{
			if (AEvent is ViewActionEvent)
			{
				switch (((ViewActionEvent)AEvent).Action)
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
			base.HandleEvent(AEvent);
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
			catch (Exception LException)
			{
				WebSession.ErrorList.Add(LException);
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
			catch (Exception LException)
			{
				WebSession.ErrorList.Add(LException);
			}
		}

		protected override void BeforeDeactivate()
		{
			try
			{
				if (OnBeforeDeactivate != null)
					OnBeforeDeactivate.Execute(this, new EventParams());
			}
			catch (Exception LException)
			{
				WebSession.ErrorList.Add(LException);
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
