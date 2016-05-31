/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Drawing;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType,Alphora.Dataphor.Frontend.Client")]
	public class Frame : Element, IFrame, IWebMenu
	{
		public Frame() {}

		public Frame([PublishSource("SourceLinkType")] SourceLinkType sourceLinkType): base()
		{
			SourceLinkType = sourceLinkType;
		}

		protected override void Dispose(bool disposing)
		{
			BeforeCloseEmbedded = null;
			base.Dispose(disposing);
		}

		// SourceLinkType - This link must be set first when deserializing, which is why it is set in the constructor.

		private SourceLinkType _sourceLinkType;
		[DefaultValue(SourceLinkType.None)]
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

		// SourceLink

		private SourceLink _sourceLink;

		public SourceLink SourceLink
		{
			get { return _sourceLink; }
			set { _sourceLink = value; }
		}
		
		// FrameInterface

		private IFrameInterface _frameInterfaceNode;

		public IFrameInterface FrameInterfaceNode
		{
			get { return _frameInterfaceNode; }
		}

		// MenuText

		private string _menuText;
		
		public string MenuText
		{
			get { return _menuText; }
			set 
			{
				if (_menuText != value)
				{
					_menuText = value; 
					UpdateMenuText();
				}
			}
		}

		public string GetMenuText()
		{
			if (_menuText == String.Empty)
				if (Name != String.Empty)
					return Name;
				else
					return Strings.Get("FrameDefaultMenuText");
			else
				return _menuText;
		}

		public void UpdateMenuText()
		{
			if (Active && (_menuItem != null))
				_menuItem.Text = GetMenuText();
		}

		// MenuItem

		private MenuItem _menuItem;
		public MenuItem MenuItem { get { return _menuItem; } }

		private void EnsureMenuItem()
		{
			if (_menuItem == null)
			{
				_menuItem = new MenuItem();
				_menuItem.Text = GetMenuText();
				((IWebMenu)FindParent(typeof(IWebMenu))).Items.Add(_menuItem);
			}
		}

		// IWebMenu

		public MenuItemList Items
		{
			get
			{
				EnsureMenuItem();
				return _menuItem.Items;
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

		// Document

		private string _document = String.Empty;
		
		public string Document
		{
			get { return _document; }
			set
			{
				if (_document != value)
				{
					_document = value;
					UpdateFrameInterfaceNode(Active);
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
					UpdateFrameInterfaceNode(Active);
				}
			}
		}

		protected void UpdateFrameInterfaceNode(bool build)
		{
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
					_frameInterfaceNode.HostNode.BroadcastEvent(new Frontend.Client.DisableSourceEvent());
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
					}
				}
			}
			if (build && (_document != String.Empty))
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

		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			if (_frameInterfaceNode != null) 
				((IWebElement)_frameInterfaceNode).Render(writer);
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

		public override void BroadcastEvent(NodeEvent eventValue)
		{
			if (_frameInterfaceNode != null)
				_frameInterfaceNode.BroadcastEvent(eventValue);
		}

		protected virtual bool LoadOnActivate()
		{
			return true;
		}

		protected override void Activate()
		{
			if (LoadOnActivate())
				UpdateFrameInterfaceNode(true);
			base.Activate();
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
				UpdateFrameInterfaceNode(false);
			}
			finally
			{
				try
				{
					ReleaseMenu();
				}
				finally
				{
					base.Deactivate();
				}
			}
		}

		protected void ReleaseMenu()
		{
			if (_menuItem != null)
			{
				((IWebMenu)Parent.FindParent(typeof(IWebMenu))).Items.Remove(_menuItem);
				_menuItem = null;
			}
		}
	}

	[PublishAs("Interface")]
	public class FrameInterface : Interface, IFrameInterface
	{
		public FrameInterface(IFrame frame) : base()
		{
			_frame = frame;
		}

		// Frame

		private IFrame _frame;
		public IFrame Frame
		{
			get { return _frame; }
		}

		// Node

		public override INode Parent
		{
			get { return _frame; }
		}
	}
}