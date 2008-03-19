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

		public Frame([PublishSource("SourceLinkType")] SourceLinkType ASourceLinkType): base()
		{
			SourceLinkType = ASourceLinkType;
		}

		protected override void Dispose(bool ADisposing)
		{
			BeforeCloseEmbedded = null;
			base.Dispose(ADisposing);
		}

		// SourceLinkType - This link must be set first when deserializing, which is why it is set in the constructor.

		private SourceLinkType FSourceLinkType;
		[DefaultValue(SourceLinkType.None)]
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

		// SourceLink

		private SourceLink FSourceLink;

		public SourceLink SourceLink
		{
			get { return FSourceLink; }
			set { FSourceLink = value; }
		}
		
		// FrameInterface

		private IFrameInterface FFrameInterfaceNode;

		public IFrameInterface FrameInterfaceNode
		{
			get { return FFrameInterfaceNode; }
		}

		// MenuText

		private string FMenuText;
		
		public string MenuText
		{
			get { return FMenuText; }
			set 
			{
				if (FMenuText != value)
				{
					FMenuText = value; 
					UpdateMenuText();
				}
			}
		}

		public string GetMenuText()
		{
			if (FMenuText == String.Empty)
				if (Name != String.Empty)
					return Name;
				else
					return Strings.Get("FrameDefaultMenuText");
			else
				return FMenuText;
		}

		public void UpdateMenuText()
		{
			if (Active && (FMenuItem != null))
				FMenuItem.Text = GetMenuText();
		}

		// MenuItem

		private MenuItem FMenuItem;
		public MenuItem MenuItem { get { return FMenuItem; } }

		private void EnsureMenuItem()
		{
			if (FMenuItem == null)
			{
				FMenuItem = new MenuItem();
				FMenuItem.Text = GetMenuText();
				((IWebMenu)FindParent(typeof(IWebMenu))).Items.Add(FMenuItem);
			}
		}

		// IWebMenu

		public MenuItemList Items
		{
			get
			{
				EnsureMenuItem();
				return FMenuItem.Items;
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

		// Document

		private string FDocument = String.Empty;
		
		public string Document
		{
			get { return FDocument; }
			set
			{
				if (FDocument != value)
				{
					FDocument = value;
					UpdateFrameInterfaceNode(Active);
				}
			}
		}

		protected void UpdateFrameInterfaceNode(bool ABuild)
		{
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
					FFrameInterfaceNode.HostNode.BroadcastEvent(new Frontend.Client.DisableSourceEvent());
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
					}
				}
			}
			if (ABuild && (FDocument != String.Empty))
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

		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			if (FFrameInterfaceNode != null) 
				((IWebElement)FFrameInterfaceNode).Render(AWriter);
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

		public override void BroadcastEvent(NodeEvent AEvent)
		{
			if (FFrameInterfaceNode != null)
				FFrameInterfaceNode.BroadcastEvent(AEvent);
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
			if (FFrameInterfaceNode != null)
				FFrameInterfaceNode.HostNode.AfterOpen();
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
			if (FMenuItem != null)
			{
				((IWebMenu)Parent.FindParent(typeof(IWebMenu))).Items.Remove(FMenuItem);
				FMenuItem = null;
			}
		}
	}

	[PublishAs("Interface")]
	public class FrameInterface : Interface, IFrameInterface
	{
		public FrameInterface(IFrame AFrame) : base()
		{
			FFrame = AFrame;
		}

		// Frame

		private IFrame FFrame;
		public IFrame Frame
		{
			get { return FFrame; }
		}

		// Node

		public override INode Parent
		{
			get { return FFrame; }
		}
	}
}