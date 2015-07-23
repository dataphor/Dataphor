/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
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
		protected override void Dispose(bool disposing)
		{
			try
			{
				OnActivePageChange = null;
				ActivePage = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		// Title

		protected string _title = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get	{ return _title; }
			set { _title = (value == null ? String.Empty : value); }
		}

		// ActivePage

		private IBaseNotebookPage _activePage;
		
		public IBaseNotebookPage ActivePage
		{
			get { return _activePage; }
			set
			{
				if (_activePage != value)
				{
					if ((_activePage != null) && Active)
						((IWebNotebookPage)_activePage).Unselected();
					_activePage = value;
					if (Active)
					{
						if (_activePage != null)
							((IWebNotebookPage)_activePage).Selected();
						if (OnActivePageChange != null)
							OnActivePageChange.Execute();
					}
				}
			}
		}

		// OnActivePageChange

		private IAction _onActivePageChange;
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
			base.Activate();

			// if no explicit active page, then activate the first
			if ((_activePage == null) && (Children.Count != 0))
				_activePage = (IBaseNotebookPage)Children[0];
			
			if (_activePage != null)
				((IWebNotebookPage)_activePage).Selected();
		}

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IWebNotebookPage).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		// Element

		protected override void InternalRender(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "notebook");
			if (_title != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, _title);
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "3");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabspacer");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(writer, "1", "1");
			writer.RenderEndTag();

			int visiblePageCount = 0;
			foreach (IWebNotebookPage page in Children)
			{
				if (page.Visible)
				{
					if (page == _activePage)
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabactive");
					else
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktab");
						writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, page.ID));
					}
					writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					writer.RenderBeginTag(HtmlTextWriterTag.Td);

					writer.Write(HttpUtility.HtmlEncode(GetPageTitle(page)));

					writer.RenderEndTag();

					visiblePageCount++;
				}
			}

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabspacerright");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(writer, "1", "1");
			writer.RenderEndTag();

			writer.RenderEndTag();	// TR

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "notebooktabcontents");
			writer.AddAttribute(HtmlTextWriterAttribute.Colspan, (visiblePageCount + 2).ToString());
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			if (_activePage != null)
				((IWebNotebookPage)_activePage).Render(writer);
			else
				Session.RenderDummyImage(writer, "1", "1");
			writer.RenderEndTag();

			writer.RenderEndTag();	// TR
			writer.RenderEndTag();	// TABLE
		}

		protected virtual string GetPageTitle(IWebNotebookPage page)
		{
			if (page.Title != String.Empty)
				return page.Title;
			else
				return Strings.Get("UntitledPage");
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			else
			{
				foreach (IWebElement page in Children)
					if (Session.IsActionLink(context, page.ID))
					{
						ActivePage = (IBaseNotebookPage)page;
						return true;
					}
				return false;
			}
		}
	}

	public class NotebookPage : SingleElementContainer, IWebNotebookPage, INotebookPage
	{
		// Title

		protected string _title = String.Empty;
		public string Title
		{
			get { return _title; }
			set { _title = value; }
		}

		public virtual void Selected() {}
		public virtual void Unselected() {}
	}

	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType,Alphora.Dataphor.Frontend.Client")]
	public class NotebookFramePage : Frame, IWebNotebookPage, INotebookFramePage
	{
		public NotebookFramePage() {}

		public NotebookFramePage([PublishSource("SourceLinkType")] SourceLinkType sourceLinkType): base(sourceLinkType) {}
			
		// Title

		protected string _title = String.Empty;
		public string Title
		{
			get { return _title; }
			set { _title = value; }
		}

		// LoadAsSelected

		// TODO: Have LoadAsSelected function in the Web Client (otherwise may cause problem with code that expects LoadAsSelected behavior)
		private bool _loadAsSelected = true;
		[DefaultValue(true)]
		public bool LoadAsSelected
		{
			get { return _loadAsSelected; }
			set { _loadAsSelected = value; }
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