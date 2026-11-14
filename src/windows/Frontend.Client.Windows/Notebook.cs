/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Drawing;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{

	/// <remarks> 
	///		The Notebook class depends on a BaseNotebookPage descendant 
	///		class as a child.  This class does not only depend on the interfaces of the children.
	///	</remarks>
	[DesignerImage("Image('Frontend', 'Nodes.Notebook')")]
	[DesignerCategory("Static Controls")]
	public class Notebook : ControlContainer, INotebook
	{
		protected override void Dispose(bool disposing)
		{
			try
			{
				OnActivePageChange = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public EnhancedTabControl TabControl
		{
			get { return (EnhancedTabControl)Control; }
		}

		private void SelectionChanged(object sender, EventArgs args)
		{
			if (Active)
			{
				if (TabControl.Selected != null)
					SetActive((IBaseNotebookPage)TabControl.Selected.Tag);
				else
					SetActive(null);
				if (OnActivePageChange != null)
					OnActivePageChange.Execute(this, new EventParams());
			}
		}

		private void SetActive(IBaseNotebookPage page)
		{
			if (page != _activePage)
			{
				IBaseNotebookPage oldPage = _activePage;
				_activePage = page;

				BeginUpdate();
				try
				{
					if (oldPage != null)
						((BaseNotebookPage)oldPage).Unselected();
					if (_activePage != null)
						((BaseNotebookPage)_activePage).Selected();
				}
				finally
				{
					if (Active)
						UpdateLayout();
					EndUpdate(true);
				}
			}
		}

		// ActivePage

		private IBaseNotebookPage _activePage;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("The currently active notebook page.")]
		public IBaseNotebookPage ActivePage
		{
			get { return _activePage; }
			set
			{
				if (_activePage != value)
				{
					if ((value != null) && (!IsChildNode(value)))
						throw new ClientException(ClientException.Codes.InvalidActivePage);
					if (Active)
					{
						if (value == null)
							TabControl.Selected = null;
						else
							TabControl.Selected = ((BaseNotebookPage)value).TabPageControl;
					}
					else
						_activePage = value;
				}
			}
		}

		private bool IsChildNode(INode node)
		{
			foreach (INode localNode in Children)
				if (localNode == node)
					return true;
			return false;
		}

		// OnActivePageChange

		private IAction _onActivePageChange;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Action triggered when the active page changes.")]
		public IAction OnActivePageChange
		{
			get { return _onActivePageChange; }
			set
			{
				if (_onActivePageChange != null)
					_onActivePageChange.Disposed -= new EventHandler(OnActivePageChangeDisposed);
				_onActivePageChange = value;
				if (_onActivePageChange != null)
					_onActivePageChange.Disposed += new EventHandler(OnActivePageChangeDisposed);
			}
		}

		private void OnActivePageChangeDisposed(object sender, EventArgs args)
		{
			OnActivePageChange = null;
		}

		// Node

		protected override void Activate()
		{
			// Use the first child if there is not an explicit active page set (do this before calling base so the child will know that it will be active)
			if ((_activePage == null) && (Children.Count > 0))
				_activePage = (IBaseNotebookPage)Children[0];

			base.Activate();

			if (_activePage != null)
			{
				((BaseNotebookPage)_activePage).Selected();
				TabControl.Selected = ((BaseNotebookPage)_activePage).TabPageControl;
			}
		}

		public override bool IsValidChild(Type childType)
		{
			if (typeof(BaseNotebookPage).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		protected override void ChildrenChanged()
		{
			base.ChildrenChanged();
			if ((_activePage == null) && (Children.Count > 0))
				_activePage = (IBaseNotebookPage)Children[0];
		}


		// Element

		public override bool GetDefaultTabStop()
		{
			return true;
		}

		internal protected void LayoutPage(BaseNotebookPage page)
		{
			LayoutChild(page, Rectangle.Empty);
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			LayoutControl(bounds);

			foreach (BaseNotebookPage page in Children)
			{
				if (page == _activePage)
					LayoutPage(page);
				else
					((BaseNotebookPage)page).InvalidateLaidOut();
			}
		}

		/// <remarks> The maximum of all minimum child sizes. </remarks>
		protected override Size InternalMinSize
		{
			get
			{
				Size minSize = Size.Empty;
				foreach (BaseNotebookPage page in Children)
					ConstrainMin(ref minSize, page.MinSize);
				return minSize;
			}
		}

		/// <remarks> The maximum of all maximum child sizes. </remarks>
		protected override Size InternalMaxSize
		{
			get
			{
				Size maxSize = Size.Empty;
				foreach (BaseNotebookPage page in Children)
					ConstrainMin(ref maxSize, page.MaxSize);
				return maxSize;
			}
		}

		/// <remarks> The maximum of all natural child sizes. </remarks>
		protected override Size InternalNaturalSize
		{
			get
			{
				Size naturalSize = Size.Empty;
				foreach (BaseNotebookPage page in Children)
					ConstrainMin(ref naturalSize, page.NaturalSize);
				return naturalSize;
			}
		}

		// ContainerElement

		protected override void UpdateColor()
		{
			// TODO: support for transparent colors
		}

		// ControlContainer

		protected override System.Windows.Forms.Control CreateControl()
		{
			return new EnhancedTabControl();
		}

		protected override void InitializeControl()
		{
			Theme theme = ((Session)HostNode.Session).Theme;
			TabControl.TabColor = theme.TabColor;
			TabControl.TabOriginColor = theme.TabOriginColor;
			TabControl.LineColor = theme.TabLineColor;
			TabControl.BodyColor = theme.ContainerColor;
			TabControl.OnSelectionChanged += new EventHandler(SelectionChanged);
		}
	}

	public abstract class BaseNotebookPage : ControlContainer
	{
		protected bool _laidOut;

		internal protected virtual void InvalidateLaidOut() 
		{
			_laidOut = false;
		}

		public virtual void Selected() 
		{
			if (!_laidOut)
				((Notebook)Parent).LayoutPage(this);
		}

		public virtual void Unselected() {}

		// ControlContainer

		protected override void LayoutControl(Rectangle bounds)
		{
			// Do nothing... tab pages auto-size themselves within the tab notebook
		}

		/// <remarks> Ensure that the pages are in the same order as the nodes. </remarks>
		protected override void SetParent()
		{
			EnhancedTabControl parent = ((Notebook)Parent).TabControl;
			
			int thisIndex = 0;
			for (int i = 0; i < Parent.Children.Count; i++)
				if (Parent.Children[i] == this)
					break;
				else
					if (((IElement)Parent.Children[i]).Visible)	// Don't use GetVisible() - we don't care if the page's parent is visible for inclusion purposes
						thisIndex++;

			if (!parent.Pages.Contains(TabPageControl))
				parent.Pages.Insert(thisIndex, TabPageControl);
		}

		protected override System.Windows.Forms.Control CreateControl()
		{
			return new EnhancedTabPage();
		}

		protected override void InitializeControl()
		{
			TabPageControl.Tag = this;
		}

		protected override void SetControlText(string title)
		{
			TabPageControl.Text = title;
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			if (_title == String.Empty)
				TabPageControl.Text = Strings.CDefaultNotebookPageTitle;
		}

		protected override void InternalUpdateVisible()
		{
			if (Visible)		// Don't use GetVisible() - we don't care if the page's parent is visible for inclusion purposes
				SetParent();
			else
				Control.Parent = null;
		}

		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public EnhancedTabPage TabPageControl
		{
			get { return (EnhancedTabPage)Control; }
		}

		// Element

		protected override void InternalLayout(Rectangle bounds)
		{
			base.InternalLayout(bounds);
			_laidOut = true;
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			InvalidateLaidOut();
		}

		protected override void Deactivate()
		{
			base.Deactivate();
		}

		public override bool IsValidOwner(Type ownerType)
		{
			return typeof(INotebook).IsAssignableFrom(ownerType);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.NotebookPage')")]
	[DesignerCategory("Static Controls")]
	public class NotebookPage : BaseNotebookPage, INotebookPage
	{
	}

	[DesignerImage("Image('Frontend', 'Nodes.NotebookFramePage')")]
	[DesignerCategory("Static Controls")]
	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType")]
	public class NotebookFramePage : BaseNotebookPage, IFrame, IWindowsMenuHost, INotebookFramePage
	{
		public NotebookFramePage() {}

		public NotebookFramePage([PublishSource("SourceLinkType")] SourceLinkType sourceLinkType): base()
		{
			SourceLinkType = sourceLinkType;
		}

		protected override void Dispose(bool disposing)
		{
			BeforeCloseEmbedded = null;
			base.Dispose(disposing);
		}

		// this link must be set first when deserializing.
		// which is why it is set in the constructor
		private SourceLinkType _sourceLinkType;
		[DefaultValue(SourceLinkType.None)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("Determines the data relationship between this document one that will be shown.")]
		public SourceLinkType SourceLinkType
		{
			get { return _sourceLinkType; }
			set
			{
				if (_sourceLinkType != value)
				{
					if (_sourceLink != null)
						_sourceLink.Dispose();
					_sourceLinkType = value;
					if (_sourceLinkType == SourceLinkType.None)
						_sourceLink = null;
					else 
					{
						if (_sourceLinkType == SourceLinkType.Surrogate)
							_sourceLink = new SurrogateSourceLink(this);
						else if (_sourceLinkType == SourceLinkType.Detail)
							_sourceLink = new DetailSourceLink(this);
						if (_frameInterfaceNode != null)
							_sourceLink.TargetSource = _frameInterfaceNode.MainSource;
					}
				}
			}
		}

		private SourceLink _sourceLink;
		[BOP.Publish(BOP.PublishMethod.Inline)]
		[Description("Contains the specific settings based on the SourceLinkType.")]
		public SourceLink SourceLink
		{
			get { return _sourceLink; }
		}

		// PostBeforeClosingEmbedded

		private bool _postBeforeClosingEmbedded = true;
		[DefaultValue(true)]
		[Description("Determines whether the frame will automatically request a post of an embedded interface before closing it.")]
		public bool PostBeforeClosingEmbedded
		{
			get { return _postBeforeClosingEmbedded; }
			set { _postBeforeClosingEmbedded = value; }
		}

		// BeforeCloseEmbedded

		private IAction _beforeCloseEmbedded;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed before closing an embedded interface (AInterface).")]
		public IAction BeforeCloseEmbedded
		{
			get { return _beforeCloseEmbedded; }
			set
			{
				if (_beforeCloseEmbedded != value)
				{
					if (_beforeCloseEmbedded != null)
						_beforeCloseEmbedded.Disposed -= new EventHandler(BeforeCloseEmbeddedActionDisposed);
					_beforeCloseEmbedded = value;
					if (_beforeCloseEmbedded != null)
						_beforeCloseEmbedded.Disposed += new EventHandler(BeforeCloseEmbeddedActionDisposed);
				}
			}
		}

		private void BeforeCloseEmbeddedActionDisposed(object sender, EventArgs args)
		{
			BeforeCloseEmbedded = null;
		}

		// Document

		private string _document = String.Empty;
		[DefaultValue("")]
		[Description("Specifies the Document of the form interface to embed.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return _document; }
			set
			{
				if (_document != value)
				{
					_document = value;
					UpdateFrameInterface(true);
				}
			}
		}

        // Filter

        private string _filter = String.Empty;
        [DefaultValue("")]
        [Description("Filter to apply to the main source of the target frame.")]
        [Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor", "System.Drawing.Design.UITypeEditor,System.Drawing")]
        [DAE.Client.Design.EditorDocumentType("d4")]
        public string Filter
        {
            get { return _filter; }
			set
			{
				if (_filter != value)
				{
					_filter = value;
					UpdateFrameInterface(true);
				}
			}
		}

		// FrameInterface

		private FrameInterface _frameInterfaceNode;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IFrameInterface FrameInterfaceNode
		{
			get { return _frameInterfaceNode; }
		}

		private void UpdateFrameInterface(bool force)
		{
			// If the frame should be loaded and it is not, or vise versa... then fix it
			if 
			(
				Active && 
				(
					(ShouldLoad() == (_frameInterfaceNode == null)) || 
					force
				)
			)
				ResetFrameInterfaceNode(Active);
		}

		private bool IsSelected()
		{
			return ((Notebook)Parent).ActivePage == this;
		}

		private bool ShouldLoad()
		{
			return (_document != String.Empty) && (IsSelected() || !_loadAsSelected);
		}

		// This is based the Frame code
		private void ResetFrameInterfaceNode(bool build)
		{
			BeginUpdate();
			try
			{
				// Clean up the old frame if there is one
				if (_frameInterfaceNode != null)
				{
					// Optionally post the data changes
					if (_postBeforeClosingEmbedded)
						_frameInterfaceNode.PostChanges();

					// Invoke the before close embedded handler
					if (_beforeCloseEmbedded != null)
						_beforeCloseEmbedded.Execute(this, new EventParams("AInterface", _frameInterfaceNode));

					try
					{
						_frameInterfaceNode.HostNode.BroadcastEvent(new DisableSourceEvent());
						if (_sourceLink != null)
							_sourceLink.TargetSource = null;
					}
					finally
					{
						try
						{
							_frameInterfaceNode.HostNode.Dispose();
						}
						finally
						{
							_frameInterfaceNode = null;
							RemoveMenu();
						}
						InvalidateLaidOut();
					}
				}
				// Create the new frame
				if 
				(
					build && 
					ShouldLoad()
				)
				{
					IHost host = HostNode.Session.CreateHost();
					try
					{
						_frameInterfaceNode = new FrameInterface(this);
						try
						{
							host.Load(_document, _frameInterfaceNode);
							if (_sourceLink != null)
								_sourceLink.TargetSource = _frameInterfaceNode.MainSource;
							if (_frameInterfaceNode.MainSource != null && !String.IsNullOrEmpty(_filter))
								_frameInterfaceNode.MainSource.Filter = _filter;
							host.Open(!Active);
							if (Active)
								BroadcastEvent(new FormShownEvent());
						}
						catch
						{
							_frameInterfaceNode.Dispose();
							_frameInterfaceNode = null;
							throw;
						}
					}
					catch
					{
						host.Dispose();
						throw;
					}
				}
			}
			finally
			{
				if (Active)
					UpdateLayout();
				EndUpdate(Active);
			}
		}

		// IWindowsMenuHost

		private IWindowsBarContainer _menuContainer;
		[Browsable(false)]
		public IWindowsBarContainer MenuContainer
		{
			get 
			{ 
				EnsureMenu();
				return _menuContainer; 
			}
		}

		private void EnsureMenu()
		{
			if (_menuContainer == null)
			{
				IWindowsMenuHost windowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
				if (windowsMenuHost != null)
				{
					_menuContainer = windowsMenuHost.MenuContainer.CreateContainer();
					((IWindowsBarButton)_menuContainer).Text = GetMenuText();
					_menuContainer.Visible = IsSelected();
					windowsMenuHost.MenuContainer.AddBarItem(_menuContainer, null);
				}
			}
		}

		private void RemoveMenu()
		{
			if (_menuContainer != null)
			{
				IWindowsMenuHost windowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
				if (windowsMenuHost != null)
					windowsMenuHost.MenuContainer.RemoveBarItem(_menuContainer);
				_menuContainer.Dispose();
				_menuContainer = null;
			}
		}

		// MenuText

		private string _menuText = String.Empty;
		[DefaultValue("")]
		[Description("The menu name under which the frames' menus will be available.")]
		public string MenuText
		{
			get { return _menuText; }
			set
			{
				_menuText = value;
				UpdateMenuText();
			}
		}

		public string GetMenuText()
		{
			if (_menuText == String.Empty)
				if (Title != String.Empty)
					return Title;
				else
					return (new System.Resources.ResourceManager("Alphora.Dataphor.Frontend.Client.Windows.Strings", typeof(Frame).Assembly).GetString("CFrameDefaultMenuText"));
			else
				return _menuText;
		}

		public void UpdateMenuText()
		{
			if (Active && (_menuContainer != null))
				((IWindowsBarButton)_menuContainer).Text = GetMenuText();
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			UpdateMenuText();
		}

		// LoadAsSelected

		private bool _loadAsSelected = true;
		[DefaultValue(true)]
		[Description("When true, the frame will only be loaded when the tab is selected; otherwise the frame is loaded when activated.")]
		public bool LoadAsSelected
		{
			get { return _loadAsSelected; }
			set
			{
				if (_loadAsSelected != value)
				{
					try
					{
						// Set the property before updating so the update function appropriately
						_loadAsSelected = value;
						UpdateFrameInterface(false);
					}
					catch
					{
						_loadAsSelected = !value;
						throw;
					}
				}
			}
		}

		// BaseNotebookPage

		public override void Selected()
		{
			UpdateFrameInterface(false);
			if (_menuContainer != null)
				_menuContainer.Visible = GetVisible();
			base.Selected();
		}

		public override void Unselected()
		{
			// no need to call base
			try
			{
				UpdateFrameInterface(false);
			}
			finally
			{
				if (_menuContainer != null)
					_menuContainer.Visible = false;
			}
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			ResetFrameInterfaceNode(true);
		}

		protected override void AfterActivate()
		{
			if (_frameInterfaceNode != null)
				_frameInterfaceNode.HostNode.AfterOpen();
			base.AfterActivate();
		}

		protected override void Deactivate()
		{
			try
			{
				ResetFrameInterfaceNode(false);
			}
			finally
			{
				base.Deactivate();
			}
		}

		public override void BroadcastEvent(NodeEvent eventValue)
		{
			if (FrameInterfaceNode != null)
				FrameInterfaceNode.BroadcastEvent(eventValue);
		}

		// Element

		protected override void InternalUpdateVisible() 
		{
			base.InternalUpdateVisible();
			if (_menuContainer != null)
				_menuContainer.Visible = (GetVisible() && IsSelected());
		}

		public override void VisibleChanged()
		{
			base.VisibleChanged();
			if (_frameInterfaceNode != null)
				_frameInterfaceNode.VisibleChanged();
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			LayoutChild(_frameInterfaceNode, Rectangle.Empty);
		}

		private Size _priorMinSize = Size.Empty;
		
		protected override Size InternalMinSize
		{
			get
			{
				if (_frameInterfaceNode != null)
					_priorMinSize = _frameInterfaceNode.MinSize;
				
				return _priorMinSize;
			}
		}
		
		private Size _priorMaxSize = Size.Empty;
		
		protected override Size InternalMaxSize
		{
			get
			{
				if (_frameInterfaceNode != null)
					_priorMaxSize = _frameInterfaceNode.MaxSize;

				return _priorMaxSize;
			}
		}

		// Remember the natural size so that if we are not loaded we can use what the size was when we were loaded
		private Size _priorNaturalSize = Size.Empty;

		protected override Size InternalNaturalSize
		{
			get
			{
				if (_frameInterfaceNode != null)
					_priorNaturalSize = _frameInterfaceNode.NaturalSize;

				return _priorNaturalSize;
			}
		}
	}

	public class EnhancedTabControl : Alphora.Dataphor.DAE.Client.Controls.Notebook
	{
		public EnhancedTabControl()
		{
		}
	}

	public class EnhancedTabPage : Alphora.Dataphor.DAE.Client.Controls.NotebookPage
	{
		public EnhancedTabPage()
		{
		}
	}
}