/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Embeds a FrameInterface. </summary>
	[Description("Embeds a seperate form inside of the current form.")]
	[DesignerImage("Image('Frontend', 'Nodes.Frame')")]
	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType,Alphora.Dataphor.Frontend.Client")]
	[DesignerCategory("Static Controls")]
	public class Frame : Element, IWindowsMenuHost, IFrame, IWindowsContainerElement
	{
		public Frame() {}

		public Frame([PublishSource("SourceLinkType")] SourceLinkType ASourceLinkType): base()
		{
			SourceLinkType = ASourceLinkType;
			// No margin (by default)... the frame interface root node should have its own margin
			MarginLeft = 0;
			MarginRight = 0;
			MarginTop = 0;
			MarginBottom = 0;
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
			set { FSourceLink = value; }
		}

		// Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
		[Description("A form interface Document to load inside of the frame.")]
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
					UpdateFrameInterface();
				}
			}
		}

		// PostBeforeClosingEmbedded

		private bool FPostBeforeClosingEmbedded;
		[DefaultValue(false)]
		[Description("Determines whether the frame will automatically request a post of an embedded interface before closing it")]
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

		// FrameInterface

		private FrameInterface FFrameInterfaceNode;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IFrameInterface FrameInterfaceNode
		{
			get { return FFrameInterfaceNode; }
		}

		private void UpdateFrameInterface()
		{
			ResetFrameInterfaceNode(Active);
		}

		private void ResetFrameInterfaceNode(bool ABuild)
		{
			BeginUpdate();
			try
			{
				// Clean up the old frame if there is one
				if (FFrameInterfaceNode != null)
					EnsureFrameInterfaceClosed();
				if (ABuild && (FDocument != String.Empty))
					LoadFrameInterface();
			}
			finally
			{
				if (Active)
					UpdateLayout();
				EndUpdate(Active);
			}
		}

		private void LoadFrameInterface()
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

		private void EnsureFrameInterfaceClosed()
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
				if (Name != String.Empty)
					return Name;
				else
					return Strings.CFrameDefaultMenuText;
			else
				return FMenuText;
		}

		public void UpdateMenuText()
		{
			if (Active && (FMenuContainer != null))
				((IWindowsBarButton)FMenuContainer).Text = GetMenuText();
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

		// IWindowsContainerElement

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public WinForms.Control Control
		{
			get { return ((IWindowsContainerElement)Parent).Control; }
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

		protected override void InternalUpdateVisible() 
		{
			base.InternalUpdateVisible();
			if (FMenuContainer != null)
				FMenuContainer.Visible = GetVisible();
		}

		public override void VisibleChanged()
		{
			base.VisibleChanged();
			if (FFrameInterfaceNode != null)
				FFrameInterfaceNode.VisibleChanged();
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			if (FFrameInterfaceNode != null)
				FFrameInterfaceNode.Layout(ABounds);
		}

		protected override Size InternalMinSize
		{
			get
			{
				if (FFrameInterfaceNode != null)
					return FFrameInterfaceNode.MinSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				if (FFrameInterfaceNode != null)
					return FFrameInterfaceNode.MaxSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				if (FFrameInterfaceNode != null)
					return FFrameInterfaceNode.NaturalSize;
				else
					return Size.Empty;
			}
		}

	}

	/// <summary> Embedded Interface node. </summary>
	[PublishAs("Interface")]
	public class FrameInterface : Interface, IWindowsContainerElement, IFrameInterface
	{
		public FrameInterface(IFrame AFrame) : base()
		{
			FFrame = AFrame;
		}

		// Frame

		private IFrame FFrame;
		
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IFrame Frame
		{
			get { return FFrame; }
		}

		// BackgroundImage

		private string FBackgroundImage = String.Empty;
		
		/// <summary> Frame does nothing with the BackgroundImage, but it's value is preserved. </summary>
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string BackgroundImage
		{
			get { return FBackgroundImage; }
			set { FBackgroundImage = value; }
		}

		// IconImage

		private string FIconImage = String.Empty;

		/// <remarks> Frame does nothing with the IconImage, but it's value is preserved. </remarks>
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string IconImage
		{
			get { return FIconImage; }
			set { FIconImage = value; }
		}

		/// <remarks> Frame does nothing with the ForceAcceptReject. Vestigial of IInterface ancestry. </remarks>
		public bool ForceAcceptReject { get; set; }
		
		// Default Action

		public override void PerformDefaultAction()
		{
			if (OnDefault != null)
				OnDefault.Execute(this, new EventParams());
			else
				((Interface)FindParent(typeof(IInterface))).PerformDefaultAction();
		}

		// Node

		public override INode Parent
		{
			get { return FFrame; }
		}

		// IWindowsContainerElement

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public WinForms.Control Control
		{
			get { return ((IWindowsContainerElement)FFrame).Control; }
		}
	}
}
