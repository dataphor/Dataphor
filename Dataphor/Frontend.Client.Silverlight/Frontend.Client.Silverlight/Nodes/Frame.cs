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

		public Frame([PublishSource("SourceLinkType")] SourceLinkType sourceLinkType): base()
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
		public SourceLink SourceLink
		{
			get { return _sourceLink; }
			set { _sourceLink = value; }
		}

		// Document

		private string _document = String.Empty;
		[DefaultValue("")]
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

		// PostBeforeClosingEmbedded

		private bool _postBeforeClosingEmbedded;
		[DefaultValue(false)]
		public bool PostBeforeClosingEmbedded
		{
			get { return _postBeforeClosingEmbedded; }
			set { _postBeforeClosingEmbedded = value; }
		}

		// BeforeCloseEmbedded

		private IAction _beforeCloseEmbedded;
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
		public IFrameInterface FrameInterfaceNode
		{
			get { return _frameInterfaceNode; }
		}

		protected virtual void UpdateFrameInterface(bool force)
		{
			ResetFrameInterfaceNode(Active);
		}

		protected virtual bool ShouldLoad()
		{
			return Document != String.Empty;
		}

		protected void ResetFrameInterfaceNode(bool build)
		{
			// Clean up the old frame if there is one
			if (_frameInterfaceNode != null)
				EnsureFrameInterfaceClosed();
			if (build && ShouldLoad())
				LoadFrameInterface();
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
				}
			}
		}

		// MenuText

		private string _menuText = String.Empty;
		[DefaultValue("")]
		public string MenuText
		{
			get { return _menuText; }
			set
			{
				_menuText = value;
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
	}
}
