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
	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType")]
	[DesignerCategory("Static Controls")]
	public class Frame : Element, IWindowsMenuHost, IFrame, IWindowsContainerElement
	{
		public Frame() {}

		public Frame([PublishSource("SourceLinkType")] SourceLinkType sourceLinkType): base()
		{
			SourceLinkType = sourceLinkType;
			// No margin (by default)... the frame interface root node should have its own margin
			MarginLeft = 0;
			MarginRight = 0;
			MarginTop = 0;
			MarginBottom = 0;
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
		[Description("Determines the data relationship between this document and the one that will be shown.")]
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
			set { _sourceLink = value; }
		}

		// Document

		private string _document = String.Empty;
		[DefaultValue("")]
		[Description("A form interface Document to load inside of the frame.")]
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
					UpdateFrameInterface();
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
					UpdateFrameInterface();
				}
			}
		}

		// PostBeforeClosingEmbedded

		private bool _postBeforeClosingEmbedded;
		[DefaultValue(false)]
		[Description("Determines whether the frame will automatically request a post of an embedded interface before closing it")]
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

		// FrameInterface

		private FrameInterface _frameInterfaceNode;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IFrameInterface FrameInterfaceNode
		{
			get { return _frameInterfaceNode; }
		}

		private void UpdateFrameInterface()
		{
			ResetFrameInterfaceNode(Active);
		}

		private void ResetFrameInterfaceNode(bool build)
		{
			BeginUpdate();
			try
			{
				// Clean up the old frame if there is one
				if (_frameInterfaceNode != null)
					EnsureFrameInterfaceClosed();
				if (build && (_document != String.Empty))
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

		private void EnsureFrameInterfaceClosed()
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
				if (Name != String.Empty)
					return Name;
				else
					return Strings.CFrameDefaultMenuText;
			else
				return _menuText;
		}

		public void UpdateMenuText()
		{
			if (Active && (_menuContainer != null))
				((IWindowsBarButton)_menuContainer).Text = GetMenuText();
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
			if (_menuContainer != null)
				_menuContainer.Visible = GetVisible();
		}

		public override void VisibleChanged()
		{
			base.VisibleChanged();
			if (_frameInterfaceNode != null)
				_frameInterfaceNode.VisibleChanged();
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			if (_frameInterfaceNode != null)
				_frameInterfaceNode.Layout(bounds);
		}

		protected override Size InternalMinSize
		{
			get
			{
				if (_frameInterfaceNode != null)
					return _frameInterfaceNode.MinSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				if (_frameInterfaceNode != null)
					return _frameInterfaceNode.MaxSize;
				else
					return Size.Empty;
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				if (_frameInterfaceNode != null)
					return _frameInterfaceNode.NaturalSize;
				else
					return Size.Empty;
			}
		}

	}

	/// <summary> Embedded Interface node. </summary>
	[PublishAs("Interface")]
	public class FrameInterface : Interface, IWindowsContainerElement, IFrameInterface
	{
		public FrameInterface(IFrame frame) : base()
		{
			_frame = frame;
		}

		// Frame

		private IFrame _frame;
		
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public IFrame Frame
		{
			get { return _frame; }
		}

		// BackgroundImage

		private string _backgroundImage = String.Empty;
		
		/// <summary> Frame does nothing with the BackgroundImage, but it's value is preserved. </summary>
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string BackgroundImage
		{
			get { return _backgroundImage; }
			set { _backgroundImage = value; }
		}

		// IconImage

		private string _iconImage = String.Empty;

		/// <remarks> Frame does nothing with the IconImage, but it's value is preserved. </remarks>
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public override string IconImage
		{
			get { return _iconImage; }
			set { _iconImage = value; }
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
			get { return _frame; }
		}

		// IWindowsContainerElement

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public WinForms.Control Control
		{
			get { return ((IWindowsContainerElement)_frame).Control; }
		}
	}
}
