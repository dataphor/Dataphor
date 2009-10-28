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
	public abstract class Interface : Element, IInterface, ISilverlightContainerElement
	{
		protected override void Dispose(bool ADisposed)
		{
			try
			{
				base.Dispose(ADisposed);
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

		// RootElement

		private ISilverlightElement FRootElement;
		
		protected ISilverlightElement RootElement
		{
			get { return FRootElement; }
		}

		// Text

		private string FText = String.Empty;
		[DefaultValue("")]
		public string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					UpdateText();
				}
			}
		}

		public virtual string GetText()
		{
			return FText;
		}

		protected virtual void UpdateText() 
		{
		}

		// UserState

		private IndexedDictionary<string, object> FUserState = new IndexedDictionary<string, object>();
		[Browsable(false)]
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
					{
						FMainSource.Disposed -= new EventHandler(MainSourceDisposed);
						FMainSource.StateChanged -= new DAE.Client.DataLinkHandler(MainSourceStateChanged);
					}
					FMainSource = value;
					if (FMainSource != null)
					{
						FMainSource.Disposed += new EventHandler(MainSourceDisposed);
						FMainSource.StateChanged += new DAE.Client.DataLinkHandler(MainSourceStateChanged);
					}
				}
			}
		}

		private void MainSourceDisposed(object ASender, EventArgs AArgs)
		{
			MainSource = null;
		}

		protected virtual void MainSourceStateChanged(DAE.Client.DataLink ALink, DAE.Client.DataSet ADataSet) {}

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
					DefaultChanged();
				}
			}
		}

		protected virtual void DefaultChanged()
		{
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
						FOnShown.Disposed -= new EventHandler(OnShownActionDisposed);
					FOnShown = value;
					if (FOnShown != null)
						FOnShown.Disposed += new EventHandler(OnShownActionDisposed);
				}
			}
		}

		private void OnShownActionDisposed(object ASender, EventArgs AArgs)
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

		// Node

		public override void HandleEvent(NodeEvent AEvent)
		{
			base.HandleEvent(AEvent);
			if (!AEvent.IsHandled)
				if (AEvent is ViewActionEvent)
				{
					switch (((ViewActionEvent)AEvent).Action)
					{
						case (SourceActions.Post) :
							if (FOnPost != null)
								FOnPost.Execute(this, new EventParams());
							break;
						case (SourceActions.Cancel) :
							if (FOnCancel != null)
								FOnCancel.Execute(this, new EventParams());
							break;
                        case (SourceActions.PostIfModified):
                            if (FOnPost != null)
                                FOnPost.Execute(this, new EventParams());
                            break;
					}
				}
				else if ((AEvent is FormShownEvent) && (FOnShown != null))
					FOnShown.Execute();
		}

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(ISilverlightElement).IsAssignableFrom(AChildType))
				return FRootElement == null;
			return true;
		}

		protected internal override void InvalidChildError(INode AChild) 
		{
			throw new ClientException(ClientException.Codes.UseSingleElementNode);
		}

		protected internal override void AddChild(INode AChild)
		{
			base.AddChild(AChild);
			if (AChild is ISilverlightElement)
				FRootElement = (ISilverlightElement)AChild;
		}
		
		protected internal override void RemoveChild(INode AChild)
		{
			base.RemoveChild(AChild);
			if (AChild == FRootElement)
				FRootElement = null;
		}

		protected override void Activate()
		{
			try
			{
				if (OnActivate != null)
					OnActivate.Execute(this, new EventParams());
			}
			catch (Exception LException)
			{
				this.HandleException(LException);
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
			catch (Exception LException)
			{
				this.HandleException(LException);
			}
		}

		protected internal override void BeforeDeactivate()
		{
			try
			{
				if (OnBeforeDeactivate != null)
					OnBeforeDeactivate.Execute(this, new EventParams());
			}
			catch (Exception LException)
			{
				this.HandleException(LException);
			}
			base.BeforeDeactivate();
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

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		protected ContentControl ContentControl
		{
			get { return FrameworkElement as ContentControl; }
		}
		
		/// <remarks> This method is invoked on the main thread. </remarks>
		public virtual void InsertChild(int AIndex, FrameworkElement AChild)
		{
			ContentControl.Content = AChild;
		}
	}
}