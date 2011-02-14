/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
#if SILVERLIGHT
using Image=System.Windows.Media.ImageSource;
#else
using System.Drawing;
#endif

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	public abstract class ActionNode : Node, IActionNode
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}

		// Action

		protected IAction _action;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("Associated Action node.")]
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (_action != value)
				{
					if (_action != null)
					{
						_action.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged -= new EventHandler(ActionTextChanged);
						_action.OnImageChanged -= new EventHandler(ActionImageChanged);
						_action.OnHintChanged -= new EventHandler(ActionHintChanged);
						_action.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						_action.Disposed -= new EventHandler(ActionDisposed);
					}
					_action = value;
					if (_action != null)
					{
						_action.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged += new EventHandler(ActionTextChanged);
						_action.OnImageChanged += new EventHandler(ActionImageChanged);
						_action.OnHintChanged += new EventHandler(ActionHintChanged);
						_action.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						_action.Disposed += new EventHandler(ActionDisposed);
					}
					if (Active)
					{
						InternalUpdateEnabled();
						InternalUpdateText();
						InternalUpdateImage();
					}
				}
			}
		}
		
		private void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}
		
		// Enabled

		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("When false, the node is disabled.")]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					if (Active)
						InternalUpdateEnabled();
				}
			}
		}

		public virtual bool GetEnabled()
		{
			return ( _action == null ? false : _action.GetEnabled() ) && _enabled;
		}
		
		private void ActionEnabledChanged(object sender, EventArgs args)
		{
			if (Active)
				InternalUpdateEnabled();
		}
		
		protected virtual void InternalUpdateEnabled() {}

		// Visible

		private bool _visible = true;
		[DefaultValue(true)]
		[Description("When set to false the menu will not be shown.")]
		public bool Visible
		{
			get { return _visible; }
			set
			{
				if (_visible != value)
				{
					_visible = value;
					if (Active)
						InternalUpdateVisible();
				}
			}
		}

		public virtual bool GetVisible() 
		{
			return _visible && ( Parent is IVisual ? ((IVisual)Parent).GetVisible() : true ) && ((_action == null) || _action.Visible);
		}

		protected virtual void InternalUpdateVisible() {}

		public void UpdateVisible()
		{
			if (Active)
				InternalUpdateVisible();
		}

		private void ActionVisibleChanged(object sender, EventArgs e)
		{
			UpdateVisible();
		}

		// Text

		private string _text = String.Empty;
		[DefaultValue("")]
		[Description("A text string that will be used by this node.  If this is not set the text property of the action will be used.")]
		public virtual string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					if (Active)
						InternalUpdateText();
				}
			}
		}

		private void ActionTextChanged(object sender, EventArgs args)
		{
			if ((_text == String.Empty) && Active)
				InternalUpdateText();
		}

		public virtual string GetText()
		{
			if (Text != String.Empty)
				return Text;
			else
				return ( Action != null ? Action.Text : String.Empty );
		}

		protected virtual void InternalUpdateText() {}

		// Image

		private void ActionImageChanged(object sender, EventArgs args)
		{
			if (Active)
				InternalUpdateImage();
		}

		protected virtual void InternalUpdateImage()
		{
			if (Action != null)
				InternalSetImage(((Action)Action).LoadedImage);
			else
				InternalSetImage(null);
		}

		protected virtual void InternalSetImage(Image image) {}

		// Hint

		protected virtual string GetHint()
		{
			if (Action != null)
				return Action.Hint;
			else
				return String.Empty;
		}

		private void ActionHintChanged(object sender, EventArgs args)
		{
			if (Active)
				InternalUpdateHint();
		}

		protected virtual void InternalUpdateHint() {}

		// Node

		protected override void Activate()
		{
			InternalUpdateEnabled();
			InternalUpdateText();
			InternalUpdateVisible();
			base.Activate();
		}

		protected internal override void AfterActivate()
		{
			InternalUpdateImage();
			base.AfterActivate();
		}
	}
}
