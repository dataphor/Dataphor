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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}

		// VerticalAlignment

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set	{ _verticalAlignment = value; }
		}

		// Action

		protected IAction _action;
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (_action != value)
				{
					if (_action != null)
						_action.Disposed -= new EventHandler(ActionDisposed);
					_action = value;
					if (_action != null)
						_action.Disposed += new EventHandler(ActionDisposed);
				}
			}
		}

		protected void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}
		
		// ImageWidth

		protected int _imageWidth;
		public int ImageWidth
		{
			get { return _imageWidth; }
			set { _imageWidth = value; }
		}

		// ImageHeight

		protected int _imageHeight;
		public int ImageHeight
		{
			get { return _imageHeight; }
			set { _imageHeight = value; }
		}

		// Text

		private string _text = String.Empty;
		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		public virtual string GetText()
		{
			if (_text != String.Empty)
				return _text;
			else 
				if (Action != null)
					return Action.Text;
				else
					return String.Empty;
		}

		// Enabled

		private bool _enabled = true;
		public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}

		public virtual bool GetEnabled()
		{
			return ( _action == null ? false : _action.GetEnabled() ) && _enabled;
		}

		// IWebHandler

		// HACK: fix for IE 5.0 not handling CSS width correctly.
		private bool _styleHack = false;

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			else
			{
				_styleHack = context.Request.UserAgent.IndexOf("MSIE 5.0") != -1;
				if (Session.IsActionLink(context, ID) && GetEnabled())
				{
					Action.Execute();
					return true;
				}
				else
					return false;
			}
		}
		
		// IWebElement

		protected override void InternalRender(HtmlTextWriter writer)
		{
			// TODO: Render the action's image here somewhere
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "trigger");
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
			if (_styleHack)
				writer.AddAttribute(HtmlTextWriterAttribute.Style, "width: 100px;");
			if (!GetEnabled())
				writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
			switch (_verticalAlignment)
			{
				case VerticalAlignment.Middle : writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle"); break;
				case VerticalAlignment.Bottom : writer.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom"); break;
			}
			writer.AddAttribute(HtmlTextWriterAttribute.Value, Session.RemoveAccellerator(GetText()), true);
			string hint = GetHint();
			if (hint != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
			writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID));
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((_action == null) || _action.Visible);
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
			_iD = Session.GenerateID();
		}

		// ID

		private string _iD;
		public string ID { get { return _iD; } }

		// IWebHandler

		protected virtual void Execute()
		{
			if (Action != null)
				Action.Execute();
		}

		public bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, ID) && GetEnabled())
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
			_toolBarButton = new ToolBarButton();
			_toolBarButton.OnClick += new EventHandler(ButtonClicked);
		}

		// ToolBarButton

		private ToolBarButton _toolBarButton;
		public ToolBarButton ToolBarButton
		{
			get { return _toolBarButton; }
		}

		private void ButtonClicked(object sender, EventArgs args)
		{
			if (GetEnabled())
				Action.Execute();
		}

		// Client.ActionNode

		protected override void InternalUpdateEnabled() 
		{
			_toolBarButton.Enabled = GetEnabled();
		}

		protected override void InternalUpdateVisible()
		{
			_toolBarButton.Visible = GetVisible();
		}

		protected override void InternalUpdateText()
		{
			_toolBarButton.Text = GetText();
		}

		protected override void InternalUpdateHint()
		{
			_toolBarButton.Hint = GetHint();
		}

		// TODO: support images for toolbar buttons

		// Node

		protected override void Activate()
		{
			base.Activate();
			// TODO: support ordering of exposed items relating to order of nodes
			((IWebToolbar)FindParent(typeof(IWebToolbar))).ToolBar.Add(_toolBarButton);
		}

		protected override void Deactivate()
		{
			((IWebToolbar)FindParent(typeof(IWebToolbar))).ToolBar.Remove(_toolBarButton);
			base.Deactivate();
		}

	}
}