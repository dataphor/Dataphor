/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Abstract class for interfaces. </summary>
	[DesignRoot()]
	[ListInDesigner(false)]
	[DesignerImage("Image('Frontend', 'Nodes.Interface')")]
	public abstract class Interface : Element, IInterface
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

		// RootElement

		private IWindowsElement _rootElement;
		protected IWindowsElement RootElement
		{
			get { return _rootElement; }
		}

		// Text

		private string _text = String.Empty;
		[DefaultValue("")]
		[Description("The text to show as the title of the interface.")]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					if (Active)
						InternalUpdateText();
				}
			}
		}

		public virtual string GetText()
		{
			return _text;
		}

		protected virtual void InternalUpdateText() {}

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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("The source for the primary DataView of this interface.")]
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


		// BackgroundImage

		public abstract string BackgroundImage { get; set; }

		// IconImage

		public abstract string IconImage { get; set; }

		// PerformDefaultAction

		public abstract void PerformDefaultAction();

		// OnDefault

		private IAction _onDefault;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to invoke as the default for the form.")]
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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to invoke when the interface is shown.")]
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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that executes when the form is posted.")]
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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will get executed when the form is canceled.")]
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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed while form is initially activating.")]
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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed after the form is initially activated.")]
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
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed before the form is deactivated.")]
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

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IWindowsElement).IsAssignableFrom(childType))
				return _rootElement == null;
			return true;
		}

		protected override void InvalidChildError(INode child) 
		{
			throw new ClientException(ClientException.Codes.UseSingleElementNode);
		}

		protected override void AddChild(INode child)
		{
			base.AddChild(child);
			if (child is IWindowsElement)
				_rootElement = (IWindowsElement)child;
		}
		
		protected override void RemoveChild(INode child)
		{
			base.RemoveChild(child);
			if (child == _rootElement)
				_rootElement = null;
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
				Session.HandleException(exception);
			}
			InternalUpdateText();
			base.Activate();
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
				Session.HandleException(exception);
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
				Session.HandleException(exception);
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

		protected override void InternalLayout(Rectangle bounds)
		{
			if (_rootElement != null)
				_rootElement.Layout(bounds);
		}
		
		protected override Size InternalMinSize
		{
			get
			{
				if (_rootElement != null)
					return _rootElement.MinSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				if (RootElement != null)
					return RootElement.MaxSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				if (RootElement != null)
					return RootElement.NaturalSize;
				else
					return Size.Empty;
			}
		}
	}
}