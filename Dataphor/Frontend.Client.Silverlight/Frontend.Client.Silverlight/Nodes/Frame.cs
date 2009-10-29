using System;
using System.Windows;
using System.Windows.Controls;
using Alphora.Dataphor.BOP;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType")]
	public class Frame : ContentElement, IFrame
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

		// this link must be set first when deserializing.
		// which is why it is set in the constructor
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

		private SourceLink FSourceLink;
		public SourceLink SourceLink
		{
			get { return FSourceLink; }
			set { FSourceLink = value; }
		}

		// Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
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

		// PostBeforeClosingEmbedded

		private bool FPostBeforeClosingEmbedded;
		[DefaultValue(false)]
		public bool PostBeforeClosingEmbedded
		{
			get { return FPostBeforeClosingEmbedded; }
			set { FPostBeforeClosingEmbedded = value; }
		}

		// BeforeCloseEmbedded

		private IAction FBeforeCloseEmbedded;
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
		public IFrameInterface FrameInterfaceNode
		{
			get { return FFrameInterfaceNode; }
		}

		protected virtual void UpdateFrameInterface(bool AForce)
		{
			ResetFrameInterfaceNode(Active);
		}

		protected void ResetFrameInterfaceNode(bool ABuild)
		{
			// Clean up the old frame if there is one
			if (FFrameInterfaceNode != null)
				EnsureFrameInterfaceClosed();
			if (ABuild && (FDocument != String.Empty))
				LoadFrameInterface();
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
				}
			}
		}

		// MenuText

		private string FMenuText = String.Empty;
		[DefaultValue("")]
		public string MenuText
		{
			get { return FMenuText; }
			set
			{
				FMenuText = value;
			}
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			ResetFrameInterfaceNode(true);
		}

		internal protected override void AfterActivate()
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
	}
}
