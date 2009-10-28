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
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				OnActivePageChange = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		[Browsable(false)]
		[Publish(PublishMethod.None)]
		public EnhancedTabControl TabControl
		{
			get { return (EnhancedTabControl)Control; }
		}

		private void SelectionChanged(object ASender, EventArgs AArgs)
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

		private void SetActive(IBaseNotebookPage APage)
		{
			if (APage != FActivePage)
			{
				IBaseNotebookPage LOldPage = FActivePage;
				FActivePage = APage;

				BeginUpdate();
				try
				{
					if (LOldPage != null)
						((BaseNotebookPage)LOldPage).Unselected();
					if (FActivePage != null)
						((BaseNotebookPage)FActivePage).Selected();
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

		private IBaseNotebookPage FActivePage;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("The currently active notebook page.")]
		public IBaseNotebookPage ActivePage
		{
			get { return FActivePage; }
			set
			{
				if (FActivePage != value)
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
						FActivePage = value;
				}
			}
		}

		private bool IsChildNode(INode ANode)
		{
			foreach (INode LNode in Children)
				if (LNode == ANode)
					return true;
			return false;
		}

		// OnActivePageChange

		private IAction FOnActivePageChange;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Action triggered when the active page changes.")]
		public IAction OnActivePageChange
		{
			get { return FOnActivePageChange; }
			set
			{
				if (FOnActivePageChange != null)
					FOnActivePageChange.Disposed -= new EventHandler(OnActivePageChangeDisposed);
				FOnActivePageChange = value;
				if (FOnActivePageChange != null)
					FOnActivePageChange.Disposed += new EventHandler(OnActivePageChangeDisposed);
			}
		}

		private void OnActivePageChangeDisposed(object ASender, EventArgs AArgs)
		{
			OnActivePageChange = null;
		}

		// Node

		protected override void Activate()
		{
			// Use the first child if there is not an explicit active page set (do this before calling base so the child will know that it will be active)
			if ((FActivePage == null) && (Children.Count > 0))
				FActivePage = (IBaseNotebookPage)Children[0];

			base.Activate();

			if (FActivePage != null)
			{
				((BaseNotebookPage)FActivePage).Selected();
				TabControl.Selected = ((BaseNotebookPage)FActivePage).TabPageControl;
			}
		}

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(BaseNotebookPage).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		protected override void ChildrenChanged()
		{
			base.ChildrenChanged();
			if ((FActivePage == null) && (Children.Count > 0))
				FActivePage = (IBaseNotebookPage)Children[0];
		}


		// Element

		public override bool GetDefaultTabStop()
		{
			return true;
		}

		internal protected void LayoutPage(BaseNotebookPage APage)
		{
			LayoutChild(APage, Rectangle.Empty);
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			LayoutControl(ABounds);

			foreach (BaseNotebookPage LPage in Children)
			{
				if (LPage == FActivePage)
					LayoutPage(LPage);
				else
					((BaseNotebookPage)LPage).InvalidateLaidOut();
			}
		}

		/// <remarks> The maximum of all minimum child sizes. </remarks>
		protected override Size InternalMinSize
		{
			get
			{
				Size LMinSize = Size.Empty;
				foreach (BaseNotebookPage LPage in Children)
					ConstrainMin(ref LMinSize, LPage.MinSize);
				return LMinSize;
			}
		}

		/// <remarks> The maximum of all maximum child sizes. </remarks>
		protected override Size InternalMaxSize
		{
			get
			{
				Size LMaxSize = Size.Empty;
				foreach (BaseNotebookPage LPage in Children)
					ConstrainMin(ref LMaxSize, LPage.MaxSize);
				return LMaxSize;
			}
		}

		/// <remarks> The maximum of all natural child sizes. </remarks>
		protected override Size InternalNaturalSize
		{
			get
			{
				Size LNaturalSize = Size.Empty;
				foreach (BaseNotebookPage LPage in Children)
					ConstrainMin(ref LNaturalSize, LPage.NaturalSize);
				return LNaturalSize;
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
			Theme LTheme = ((Session)HostNode.Session).Theme;
			TabControl.TabColor = LTheme.TabColor;
			TabControl.TabOriginColor = LTheme.TabOriginColor;
			TabControl.LineColor = LTheme.TabLineColor;
			TabControl.BodyColor = LTheme.ContainerColor;
			TabControl.OnSelectionChanged += new EventHandler(SelectionChanged);
		}
	}

	public abstract class BaseNotebookPage : ControlContainer
	{
		protected bool FLaidOut;

		internal protected virtual void InvalidateLaidOut() 
		{
			FLaidOut = false;
		}

		public virtual void Selected() 
		{
			if (!FLaidOut)
				((Notebook)Parent).LayoutPage(this);
		}

		public virtual void Unselected() {}

		// ControlContainer

		protected override void LayoutControl(Rectangle ABounds)
		{
			// Do nothing... tab pages auto-size themselves within the tab notebook
		}

		/// <remarks> Ensure that the pages are in the same order as the nodes. </remarks>
		protected override void SetParent()
		{
			EnhancedTabControl LParent = ((Notebook)Parent).TabControl;
			
			int LThisIndex = 0;
			for (int i = 0; i < Parent.Children.Count; i++)
				if (Parent.Children[i] == this)
					break;
				else
					if (((IElement)Parent.Children[i]).Visible)	// Don't use GetVisible() - we don't care if the page's parent is visible for inclusion purposes
						LThisIndex++;

			if (!LParent.Pages.Contains(TabPageControl))
				LParent.Pages.Insert(LThisIndex, TabPageControl);
		}

		protected override System.Windows.Forms.Control CreateControl()
		{
			return new EnhancedTabPage();
		}

		protected override void InitializeControl()
		{
			TabPageControl.Tag = this;
		}

		protected override void SetControlText(string ATitle)
		{
			TabPageControl.Text = ATitle;
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			if (FTitle == String.Empty)
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

		protected override void InternalLayout(Rectangle ABounds)
		{
			base.InternalLayout(ABounds);
			FLaidOut = true;
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

		public override bool IsValidOwner(Type AOwnerType)
		{
			return typeof(INotebook).IsAssignableFrom(AOwnerType);
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

		public NotebookFramePage([PublishSource("SourceLinkType")] SourceLinkType ASourceLinkType): base()
		{
			SourceLinkType = ASourceLinkType;
		}

		protected override void Dispose(bool ADisposing)
		{
			BeforeCloseEmbedded = null;
			base.Dispose(ADisposing);
		}

		// this link must be set first when deserializing.
		// which is why it is set in the constructor
		private SourceLinkType FSourceLinkType;
		[DefaultValue(SourceLinkType.None)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("Determines the data relationship between this document one that will be shown.")]
		public SourceLinkType SourceLinkType
		{
			get { return FSourceLinkType; }
			set
			{
				if (FSourceLinkType != value)
				{
					if (FSourceLink != null)
						FSourceLink.Dispose();
					FSourceLinkType = value;
					if (FSourceLinkType == SourceLinkType.None)
						FSourceLink = null;
					else 
					{
						if (FSourceLinkType == SourceLinkType.Surrogate)
							FSourceLink = new SurrogateSourceLink(this);
						else if (FSourceLinkType == SourceLinkType.Detail)
							FSourceLink = new DetailSourceLink(this);
						if (FFrameInterfaceNode != null)
							FSourceLink.TargetSource = FFrameInterfaceNode.MainSource;
					}
				}
			}
		}

		private SourceLink FSourceLink;
		[BOP.Publish(BOP.PublishMethod.Inline)]
		[Description("Contains the specific settings based on the SourceLinkType.")]
		public SourceLink SourceLink
		{
			get { return FSourceLink; }
		}

		// PostBeforeClosingEmbedded

		private bool FPostBeforeClosingEmbedded = true;
		[DefaultValue(true)]
		[Description("Determines whether the frame will automatically request a post of an embedded interface before closing it.")]
		public bool PostBeforeClosingEmbedded
		{
			get { return FPostBeforeClosingEmbedded; }
			set { FPostBeforeClosingEmbedded = value; }
		}

		// BeforeCloseEmbedded

		private IAction FBeforeCloseEmbedded;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed before closing an embedded interface (AInterface).")]
		public IAction BeforeCloseEmbedded
		{
			get { return FBeforeCloseEmbedded; }
			set
			{
				if (FBeforeCloseEmbedded != value)
				{
					if (FBeforeCloseEmbedded != null)
						FBeforeCloseEmbedded.Disposed -= new EventHandler(BeforeCloseEmbeddedActionDisposed);
					FBeforeCloseEmbedded = value;
					if (FBeforeCloseEmbedded != null)
						FBeforeCloseEmbedded.Disposed += new EventHandler(BeforeCloseEmbeddedActionDisposed);
				}
			}
		}

		private void BeforeCloseEmbeddedActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeCloseEmbedded = null;
		}

		// Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
		[Description("Specifies the Document of the form interface to embed.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return FDocument; }
			set
			{
				if (FDocument != value)
				{
					FDocument = value;
					UpdateFrameInterface(true);
				}
			}
		}

		// FrameInterface

		private FrameInterface FFrameInterfaceNode;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IFrameInterface FrameInterfaceNode
		{
			get { return FFrameInterfaceNode; }
		}

		private void UpdateFrameInterface(bool AForce)
		{
			// If the frame should be loaded and it is not, or vise versa... then fix it
			if 
			(
				Active && 
				(
					(ShouldLoad() == (FFrameInterfaceNode == null)) || 
					AForce
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
			return (FDocument != String.Empty) && (IsSelected() || !FLoadAsSelected);
		}

		// This is based the Frame code
		private void ResetFrameInterfaceNode(bool ABuild)
		{
			BeginUpdate();
			try
			{
				// Clean up the old frame if there is one
				if (FFrameInterfaceNode != null)
				{
					// Optionally post the data changes
					if (FPostBeforeClosingEmbedded)
						FFrameInterfaceNode.PostChanges();

					// Invoke the before close embedded handler
					if (FBeforeCloseEmbedded != null)
						FBeforeCloseEmbedded.Execute(this, new EventParams("AInterface", FFrameInterfaceNode));

					try
					{
						FFrameInterfaceNode.HostNode.BroadcastEvent(new DisableSourceEvent());
						if (FSourceLink != null)
							FSourceLink.TargetSource = null;
					}
					finally
					{
						try
						{
							FFrameInterfaceNode.HostNode.Dispose();
						}
						finally
						{
							FFrameInterfaceNode = null;
							RemoveMenu();
						}
						InvalidateLaidOut();
					}
				}
				// Create the new frame
				if 
				(
					ABuild && 
					ShouldLoad()
				)
				{
					IHost LHost = HostNode.Session.CreateHost();
					try
					{
						FFrameInterfaceNode = new FrameInterface(this);
						try
						{
							LHost.Load(FDocument, FFrameInterfaceNode);
							if (FSourceLink != null)
								FSourceLink.TargetSource = FFrameInterfaceNode.MainSource;
							LHost.Open(!Active);
							if (Active)
								BroadcastEvent(new FormShownEvent());
						}
						catch
						{
							FFrameInterfaceNode.Dispose();
							FFrameInterfaceNode = null;
							throw;
						}
					}
					catch
					{
						LHost.Dispose();
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

		private IWindowsBarContainer FMenuContainer;
		[Browsable(false)]
		public IWindowsBarContainer MenuContainer
		{
			get 
			{ 
				EnsureMenu();
				return FMenuContainer; 
			}
		}

		private void EnsureMenu()
		{
			if (FMenuContainer == null)
			{
				IWindowsMenuHost LWindowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
				if (LWindowsMenuHost != null)
				{
					FMenuContainer = LWindowsMenuHost.MenuContainer.CreateContainer();
					((IWindowsBarButton)FMenuContainer).Text = GetMenuText();
					FMenuContainer.Visible = IsSelected();
					LWindowsMenuHost.MenuContainer.AddBarItem(FMenuContainer, null);
				}
			}
		}

		private void RemoveMenu()
		{
			if (FMenuContainer != null)
			{
				IWindowsMenuHost LWindowsMenuHost = (IWindowsMenuHost)FindParent(typeof(IWindowsMenuHost));
				if (LWindowsMenuHost != null)
					LWindowsMenuHost.MenuContainer.RemoveBarItem(FMenuContainer);
				FMenuContainer.Dispose();
				FMenuContainer = null;
			}
		}

		// MenuText

		private string FMenuText = String.Empty;
		[DefaultValue("")]
		[Description("The menu name under which the frames' menus will be available.")]
		public string MenuText
		{
			get { return FMenuText; }
			set
			{
				FMenuText = value;
				UpdateMenuText();
			}
		}

		public string GetMenuText()
		{
			if (FMenuText == String.Empty)
				if (Title != String.Empty)
					return Title;
				else
					return (new System.Resources.ResourceManager("Alphora.Dataphor.Frontend.Client.Windows.Strings", typeof(Frame).Assembly).GetString("CFrameDefaultMenuText"));
			else
				return FMenuText;
		}

		public void UpdateMenuText()
		{
			if (Active && (FMenuContainer != null))
				((IWindowsBarButton)FMenuContainer).Text = GetMenuText();
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			UpdateMenuText();
		}

		// LoadAsSelected

		private bool FLoadAsSelected = true;
		[DefaultValue(true)]
		[Description("When true, the frame will only be loaded when the tab is selected; otherwise the frame is loaded when activated.")]
		public bool LoadAsSelected
		{
			get { return FLoadAsSelected; }
			set
			{
				if (FLoadAsSelected != value)
				{
					try
					{
						// Set the property before updating so the update function appropriately
						FLoadAsSelected = value;
						UpdateFrameInterface(false);
					}
					catch
					{
						FLoadAsSelected = !value;
						throw;
					}
				}
			}
		}

		// BaseNotebookPage

		public override void Selected()
		{
			UpdateFrameInterface(false);
			if (FMenuContainer != null)
				FMenuContainer.Visible = GetVisible();
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
				if (FMenuContainer != null)
					FMenuContainer.Visible = false;
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
			if (FFrameInterfaceNode != null)
				FFrameInterfaceNode.HostNode.AfterOpen();
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

		public override void BroadcastEvent(NodeEvent AEvent)
		{
			if (FrameInterfaceNode != null)
				FrameInterfaceNode.BroadcastEvent(AEvent);
		}

		// Element

		protected override void InternalUpdateVisible() 
		{
			base.InternalUpdateVisible();
			if (FMenuContainer != null)
				FMenuContainer.Visible = (GetVisible() && IsSelected());
		}

		public override void VisibleChanged()
		{
			base.VisibleChanged();
			if (FFrameInterfaceNode != null)
				FFrameInterfaceNode.VisibleChanged();
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			LayoutChild(FFrameInterfaceNode, Rectangle.Empty);
		}

		private Size FPriorMinSize = Size.Empty;
		
		protected override Size InternalMinSize
		{
			get
			{
				if (FFrameInterfaceNode != null)
					FPriorMinSize = FFrameInterfaceNode.MinSize;
				
				return FPriorMinSize;
			}
		}
		
		private Size FPriorMaxSize = Size.Empty;
		
		protected override Size InternalMaxSize
		{
			get
			{
				if (FFrameInterfaceNode != null)
					FPriorMaxSize = FFrameInterfaceNode.MaxSize;

				return FPriorMaxSize;
			}
		}

		// Remember the natural size so that if we are not loaded we can use what the size was when we were loaded
		private Size FPriorNaturalSize = Size.Empty;

		protected override Size InternalNaturalSize
		{
			get
			{
				if (FFrameInterfaceNode != null)
					FPriorNaturalSize = FFrameInterfaceNode.NaturalSize;

				return FPriorNaturalSize;
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