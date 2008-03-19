/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
    public class Trigger : Element, ITrigger
    {		
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}

		// VerticalAlignment

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set	{ FVerticalAlignment = value; }
		}

		// Action

		protected IAction FAction;
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (FAction != value)
				{
					if (FAction != null)
						FAction.Disposed -= new EventHandler(ActionDisposed);
					FAction = value;
					if (FAction != null)
						FAction.Disposed += new EventHandler(ActionDisposed);
				}
			}
		}

		protected void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}
		
		// ImageWidth

		protected int FImageWidth;
		public int ImageWidth
		{
			get { return FImageWidth; }
			set { FImageWidth = value; }
		}

		// ImageHeight

		protected int FImageHeight;
		public int ImageHeight
		{
			get { return FImageHeight; }
			set { FImageHeight = value; }
		}

		// Text

		private string FText = String.Empty;
		public string Text
		{
			get { return FText; }
			set { FText = value; }
		}

		public virtual string GetText()
		{
			if (FText != String.Empty)
				return FText;
			else 
				if (Action != null)
					return Action.Text;
				else
					return String.Empty;
		}

		// Enabled

		private bool FEnabled = true;
		public bool Enabled
		{
			get { return FEnabled; }
			set { FEnabled = value; }
		}

		public virtual bool GetEnabled()
		{
			return ( FAction == null ? false : FAction.GetEnabled() ) && FEnabled;
		}

		// IWebHandler

		// HACK: fix for IE 5.0 not handling CSS width correctly.
		private bool FStyleHack = false;

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			else
			{
				FStyleHack = AContext.Request.UserAgent.IndexOf("MSIE 5.0") != -1;
				if (Session.IsActionLink(AContext, ID) && GetEnabled())
				{
					Action.Execute();
					return true;
				}
				else
					return false;
			}
		}
		
		// IWebElement

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			// TODO: Render the action's image here somewhere
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "trigger");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "button");
			if (FStyleHack)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Style, "width: 100px;");
			if (!GetEnabled())
				AWriter.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
			switch (FVerticalAlignment)
			{
				case VerticalAlignment.Middle : AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "middle"); break;
				case VerticalAlignment.Bottom : AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom"); break;
			}
			AWriter.AddAttribute(HtmlTextWriterAttribute.Value, Session.RemoveAccellerator(GetText()), true);
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.RenderEndTag();
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((FAction == null) || FAction.Visible);
		}

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
	}
	
	public class ActionNode : Client.ActionNode, IWebHandler
	{
		public ActionNode() : base()
		{
			FID = Session.GenerateID();
		}

		// ID

		private string FID;
		public string ID { get { return FID; } }

		// IWebHandler

		protected virtual void Execute()
		{
			if (Action != null)
				Action.Execute();
		}

		public bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, ID) && GetEnabled())
			{
				Execute();
				return true;
			}
			else
				return false;
		}
	}
	
	public class Exposed : ActionNode, IExposed
	{
		public Exposed()
		{
			FToolBarButton = new ToolBarButton();
			FToolBarButton.OnClick += new EventHandler(ButtonClicked);
		}

		// ToolBarButton

		private ToolBarButton FToolBarButton;
		public ToolBarButton ToolBarButton
		{
			get { return FToolBarButton; }
		}

		private void ButtonClicked(object ASender, EventArgs AArgs)
		{
			if (GetEnabled())
				Action.Execute();
		}

		// Client.ActionNode

		protected override void InternalUpdateEnabled() 
		{
			FToolBarButton.Enabled = GetEnabled();
		}

		protected override void InternalUpdateVisible()
		{
			FToolBarButton.Visible = GetVisible();
		}

		protected override void InternalUpdateText()
		{
			FToolBarButton.Text = GetText();
		}

		protected override void InternalUpdateHint()
		{
			FToolBarButton.Hint = GetHint();
		}

		// TODO: support images for toolbar buttons

		// Node

		protected override void Activate()
		{
			base.Activate();
			// TODO: support ordering of exposed items relating to order of nodes
			((IWebToolbar)FindParent(typeof(IWebToolbar))).ToolBar.Add(FToolBarButton);
		}

		protected override void Deactivate()
		{
			((IWebToolbar)FindParent(typeof(IWebToolbar))).ToolBar.Remove(FToolBarButton);
			base.Deactivate();
		}

	}
}