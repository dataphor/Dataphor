/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	public abstract class ActionNode : Node, IActionNode
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}

		// Action

		protected IAction FAction;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Associated Action node.")]
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (FAction != value)
				{
					if (FAction != null)
					{
						FAction.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged -= new EventHandler(ActionTextChanged);
						FAction.OnImageChanged -= new EventHandler(ActionImageChanged);
						FAction.OnHintChanged -= new EventHandler(ActionHintChanged);
						FAction.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						FAction.Disposed -= new EventHandler(ActionDisposed);
					}
					FAction = value;
					if (FAction != null)
					{
						FAction.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged += new EventHandler(ActionTextChanged);
						FAction.OnImageChanged += new EventHandler(ActionImageChanged);
						FAction.OnHintChanged += new EventHandler(ActionHintChanged);
						FAction.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						FAction.Disposed += new EventHandler(ActionDisposed);
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
		
		private void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}
		
		// Enabled

		private bool FEnabled = true;
		[DefaultValue(true)]
		[Description("When false, the node is disabled.")]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (FEnabled != value)
				{
					FEnabled = value;
					if (Active)
						InternalUpdateEnabled();
				}
			}
		}

		public virtual bool GetEnabled()
		{
			return ( FAction == null ? false : FAction.GetEnabled() ) && FEnabled;
		}
		
		private void ActionEnabledChanged(object ASender, EventArgs AArgs)
		{
			if (Active)
				InternalUpdateEnabled();
		}
		
		protected virtual void InternalUpdateEnabled() {}

		// Visible

		private bool FVisible = true;
		[DefaultValue(true)]
		[Description("When set to false the menu will not be shown.")]
		public bool Visible
		{
			get { return FVisible; }
			set
			{
				if (FVisible != value)
				{
					FVisible = value;
					if (Active)
						InternalUpdateVisible();
				}
			}
		}

		public virtual bool GetVisible() 
		{
			return FVisible && ( Parent is IVisual ? ((IVisual)Parent).GetVisible() : true ) && ((FAction == null) || FAction.Visible);
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

		private string FText = String.Empty;
		[DefaultValue("")]
		[Description("A text string that will be used by this node.  If this is not set the text property of the action will be used.")]
		public virtual string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					if (Active)
						InternalUpdateText();
				}
			}
		}

		private void ActionTextChanged(object ASender, EventArgs AArgs)
		{
			if ((FText == String.Empty) && Active)
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

		private void ActionImageChanged(object ASender, EventArgs AArgs)
		{
			if (Active)
				InternalUpdateImage();
		}

		protected virtual void InternalUpdateImage()
		{
			if (Action != null)
				InternalSetImage(Action.LoadedImage);
			else
				InternalSetImage(null);
		}

		protected virtual void InternalSetImage(System.Drawing.Image AImage) {}

		// Hint

		protected virtual string GetHint()
		{
			if (Action != null)
				return Action.Hint;
			else
				return String.Empty;
		}

		private void ActionHintChanged(object ASender, EventArgs AArgs)
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
