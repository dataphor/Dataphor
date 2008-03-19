/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Web.UI;
using System.ComponentModel;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class Notebook : ContainerElement, INotebook
	{
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				OnActivePageChange = null;
				ActivePage = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		// Title

		protected string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return FTitle; }
			set { FTitle = (value == null ? String.Empty : value); }
		}

		// ActivePage

		private IBaseNotebookPage FActivePage;
		
		public IBaseNotebookPage ActivePage
		{
			get { return FActivePage; }
			set
			{
				if (FActivePage != value)
				{
					if ((FActivePage != null) && Active)
						((IWebNotebookPage)FActivePage).Unselected();
					FActivePage = value;
					if (Active)
					{
						if (FActivePage != null)
							((IWebNotebookPage)FActivePage).Selected();
						if (OnActivePageChange != null)
							OnActivePageChange.Execute();
					}
				}
			}
		}

		// OnActivePageChange

		private IAction FOnActivePageChange;
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
			base.Activate();

			// if no explicit active page, then activate the first
			if ((FActivePage == null) && (Children.Count != 0))
				FActivePage = (IBaseNotebookPage)Children[0];
			
			if (FActivePage != null)
				((IWebNotebookPage)FActivePage).Selected();
		}

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IWebNotebookPage).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		// Element

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "notebook");
			if (FTitle != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, FTitle);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "3");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabspacer");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(AWriter, "1", "1");
			AWriter.RenderEndTag();

			int LVisiblePageCount = 0;
			foreach (IWebNotebookPage LPage in Children)
			{
				if (LPage.Visible)
				{
					if (LPage == FActivePage)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabactive");
					else
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktab");
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, LPage.ID));
					}
					AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

					AWriter.Write(HttpUtility.HtmlEncode(GetPageTitle(LPage)));

					AWriter.RenderEndTag();

					LVisiblePageCount++;
				}
			}

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabspacerright");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(AWriter, "1", "1");
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TR

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabcontents");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Colspan, (LVisiblePageCount + 2).ToString());
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			if (FActivePage != null)
				((IWebNotebookPage)FActivePage).Render(AWriter);
			else
				Session.RenderDummyImage(AWriter, "1", "1");
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TR
			AWriter.RenderEndTag();	// TABLE
		}

		protected virtual string GetPageTitle(IWebNotebookPage APage)
		{
			if (APage.Title != String.Empty)
				return APage.Title;
			else
				return Strings.Get("UntitledPage");
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			else
			{
				foreach (IWebElement LPage in Children)
					if (Session.IsActionLink(AContext, LPage.ID))
					{
						ActivePage = (IBaseNotebookPage)LPage;
						return true;
					}
				return false;
			}
		}
	}

	public class NotebookPage : SingleElementContainer, IWebNotebookPage, INotebookPage
	{
		// Title

		protected string FTitle = String.Empty;
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value; }
		}

		public virtual void Selected() {}
		public virtual void Unselected() {}
	}

	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType,Alphora.Dataphor.Frontend.Client")]
	public class NotebookFramePage : Frame, IWebNotebookPage, INotebookFramePage
	{
		public NotebookFramePage() {}

		public NotebookFramePage([PublishSource("SourceLinkType")] SourceLinkType ASourceLinkType): base(ASourceLinkType) {}
			
		// Title

		protected string FTitle = String.Empty;
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value; }
		}

		// LoadAsSelected

		// TODO: Have LoadAsSelected function in the Web Client (otherwise may cause problem with code that expects LoadAsSelected behavior)
		private bool FLoadAsSelected = true;
		[DefaultValue(true)]
		public bool LoadAsSelected
		{
			get { return FLoadAsSelected; }
			set { FLoadAsSelected = value; }
		}

		// IWebNodebookPage

		public virtual void Selected()
		{
			if (FrameInterfaceNode == null) 
				UpdateFrameInterfaceNode(true);
		}

		public virtual void Unselected()
		{
			try
			{
				UpdateFrameInterfaceNode(false);
			}
			finally
			{
				ReleaseMenu();
			}
		}

		protected override bool LoadOnActivate()
		{
			return false;
		}
	}
}