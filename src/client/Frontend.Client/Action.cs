/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client
{
	public enum NotifyIcon
	{
		None,
		Info,
		Warning,
		Error
	}
		
	public class EventParams : IndexedDictionary<string, object>
	{
		public EventParams() : base() {}
		
		public EventParams(params object[] parameters) : base()
		{
			for (int index = 0; index < parameters.Length; index++)
				if ((index % 2) != 0)
					Add((string)parameters[index - 1], parameters[index]);
		}
	}
	
	[DesignerImage("Image('Frontend', 'Nodes.Action')")]
	[DesignerCategory("Actions")]
	public abstract partial class Action : Node, IAction
    {
		protected override void Dispose(bool disposing)
		{
			try
			{
				AfterExecute = null;
				BeforeExecute = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		public void Execute()
		{
			Execute(this, new EventParams());
		}
		
		public void Execute(INode sender, EventParams paramsValue)
		{
			if (GetEnabled()) 
			{
				ILayoutDisableable layoutDisableable = (ILayoutDisableable)FindParent(typeof(ILayoutDisableable));
				if (layoutDisableable != null)
					layoutDisableable.DisableLayout();
				try 
				{
					if (DoBeforeExecute(sender, paramsValue))
						FinishExecute(sender, paramsValue);
				}
				finally
				{
					if (layoutDisableable != null)
						layoutDisableable.EnableLayout();
				}
			}
		}

		protected abstract void InternalExecute(INode sender, EventParams paramsValue);

		// BeforeExecute		
		private IAction _beforeExecute;
		[Description("Action to be called before the execute.")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		public IAction BeforeExecute
		{
			get { return _beforeExecute; }
			set
			{
				if (_beforeExecute != value)
				{
					if (_beforeExecute != null)
						_beforeExecute.Disposed -= new EventHandler(BeforeExecuteDisposed);
					_beforeExecute = value;
					if (_beforeExecute != null)
						_beforeExecute.Disposed += new EventHandler(BeforeExecuteDisposed);
				}
			}
		}

		private void BeforeExecuteDisposed(object sender, EventArgs args)
		{
			BeforeExecute = null;
		}
		
		/// <summary>
		/// Returns true if the before execute action is not a Blockable node, meaning that the FinishExecute should be called immediately.
		/// </summary>
		/// <param name="paramsValue"></param>
		/// <returns></returns>
		private bool DoBeforeExecute(INode sender, EventParams paramsValue)
		{
			if (_beforeExecute != null)
			{
				IBlockable blockable = _beforeExecute as IBlockable;
				if (blockable != null)
					blockable.OnCompleted += new NodeEventHandler(BeforeExecuteCompleted);

				_beforeExecute.Execute(this, paramsValue);
				
				// return true to indicate the FinishExecute should be called immediately
				// return false to indicate the FinishExecute should be called on the ExecuteCompleted of the BeforeExecute action
				return blockable == null;
			}
			
			return true;
		}

		private void BeforeExecuteCompleted(INode sender, EventParams paramsValue)
		{
			IBlockable blockable = _beforeExecute as IBlockable;
			if (blockable != null)
				blockable.OnCompleted -= new NodeEventHandler(BeforeExecuteCompleted);
			FinishExecute(sender, paramsValue);
		}

		private void FinishExecute(INode sender, EventParams paramsValue)
		{
			InternalExecute(sender, paramsValue);
			DoAfterExecute(paramsValue);
		}

		// AfterExecute		
		private IAction _afterExecute;
		[Description("Action to be called after the execute.")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		public IAction AfterExecute
		{
			get { return _afterExecute; }
			set
			{
				if (_afterExecute != value)
				{
					if (_afterExecute != null)
						_afterExecute.Disposed -= new EventHandler(AfterExecuteDisposed);
					_afterExecute = value;
					if (_afterExecute != null)
						_afterExecute.Disposed += new EventHandler(AfterExecuteDisposed);
				}
			}
		}

		private void AfterExecuteDisposed(object sender, EventArgs args)
		{
			AfterExecute = null;
		}
		
		private void DoAfterExecute(EventParams paramsValue)
		{
			if (_afterExecute != null)
				_afterExecute.Execute(this, paramsValue);
		}

		// Text

		public event EventHandler OnTextChanged;

		private string _text = String.Empty;
		[DefaultValue("")]
		[Description("Text that can be used by visible controls that hook to this action.")]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					TextChanged();
				}
			}
		}

		public virtual string GetText()
		{
			return _text;
		}

		public virtual string GetDescription()
		{
			return _text.Replace(".", "").Replace("&", "");
		}

		protected virtual void TextChanged()
		{
			if (OnTextChanged != null)
				OnTextChanged(this, EventArgs.Empty);
		}
		
		// Enabled

		public event EventHandler OnEnabledChanged;
		
		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("When this is false the action should not be executed and any controls hooking to this action should be disabled.")]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					EnabledChanged();
				}
			}
		}

		/// <summary> Remembers the value of GetEnabled so we know whether it has actually changed. </summary>
		private bool _actualEnabled;

		/// <summary> Called when the value of GetEnabled() may have changed. </summary>
		protected virtual void EnabledChanged()
		{
			bool enabled = GetEnabled();
			if (_actualEnabled != enabled)
			{
				_actualEnabled = enabled;
				if (OnEnabledChanged != null)
					OnEnabledChanged(this, EventArgs.Empty);
			}
		}

		public virtual bool GetEnabled()
		{
			return _enabled;
		}

		// Hint

		public event EventHandler OnHintChanged;

		private string _hint = String.Empty;
		[DefaultValue("")]
		[Description("A text string that will be shown in tooltips, etc.")]
		public string Hint
		{
			get { return _hint; }
			set
			{
				if (_hint != value)
				{
					_hint = value;
					if (OnHintChanged != null)
						OnHintChanged(this, EventArgs.Empty);
				}
			}
		}

		// Visible

		private bool _visible = true;
		[Description("Determines whether the controls that are associated with this action are visible.")]
		[DefaultValue(true)]
		public bool Visible
		{
			get { return _visible; }
			set 
			{ 
				if (_visible != value)
				{
					_visible = value; 
					if (OnVisibleChanged != null)
						OnVisibleChanged(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler OnVisibleChanged;

		// Node

		protected override void Activate()
		{
			base.Activate();
			_actualEnabled = GetEnabled();
		}

		/// <remarks> Clears the image. </remarks>
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				SetImage(null);
			}
		}

		protected internal override void BeforeDeactivate()
		{
			base.BeforeDeactivate();
			CancelImageRequest();
		}

		protected internal override void AfterActivate()
		{
			InternalUpdateImage();
			base.AfterActivate();
		}

	}
	
	/// <summary>
	/// Default implementation of a BlockableAction
	/// </summary>
	public abstract class BlockableAction : Action, IBlockable
	{
		public event NodeEventHandler OnCompleted;
		
		protected void DoCompleted(EventParams paramsValue)
		{
			if (OnCompleted != null)
				OnCompleted(this, paramsValue);
		}
		
		/// <summary>
		/// Executes the given action, hooking the OnCompleted if it is a Blockable action.
		/// </summary>
		/// <param name="action"></param>
		protected void BlockableExecute(IAction action, EventParams paramsValue)
		{
			IBlockable blockable = action as IBlockable;
			if (blockable != null)
			{
				blockable.OnCompleted += new NodeEventHandler(BlockableCompleted);
				blockable.Disposed += new EventHandler(BlockableDisposed);
			}
				
			action.Execute(this, paramsValue);
			
			if (blockable == null)
				DoCompleted(paramsValue);
		}
		
		protected void DetachBlockable(IBlockable blockable)
		{
			if (blockable != null)
			{
				blockable.OnCompleted -= new NodeEventHandler(BlockableCompleted);
				blockable.Disposed -= new EventHandler(BlockableDisposed);
			}
		}
		
		private void BlockableCompleted(INode sender, EventParams paramsValue)
		{
			DetachBlockable(sender as IBlockable);
			DoCompleted(paramsValue);
		}
		
		private void BlockableDisposed(object sender, EventArgs args)
		{
			DetachBlockable(sender as IBlockable);
		}
	}
    
	[Description("Executes a list of actions sequentially, like a begin...end block.")]
	public class BlockAction : Action, IBlockAction
	{
		/// <remarks> Only actions are allowed as children. </remarks>
		public override bool IsValidChild(Type childType)
		{
			if (typeof(IAction).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		/// <summary> Executes each child action sequentially. </summary>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			// Don't use a foreach here to avoid the possibility that the action changes the form and throws the enumerator changed error
			for(int count = 0; count < (Children != null ? Children.Count : 0); count++) 
				((IAction)Children[count]).Execute(this, paramsValue);
		}
	}

	[Description("Executes the specified action node when executed.")]
	public class CallAction : Action, ICallAction
	{
		/// <remarks> Dereferences the referenced action. </remarks>
		protected override void Dispose(bool disposing)
		{
			try
			{
				Action = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private IAction _action;
		[Description("Action to be called upon executing.")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (value == this)
					throw new ClientException(ClientException.Codes.RecursiveActionReference, ToString());
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

		private void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}

		/// <summary> Calls the other action. </summary>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			if (_action != null)
				_action.Execute(this, paramsValue);
		}
	}

	public class HelpAction : Action, IHelpAction
	{
		private string _helpKeyword = String.Empty;
		[DefaultValue("")]
		[Description("The keyword to use to navigate within the help.")]
		public string HelpKeyword
		{
			get { return _helpKeyword; }
			set { _helpKeyword = (value == null ? String.Empty : value); }
		}

		private HelpKeywordBehavior _helpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		[Description("Specifies the type of initial help navigation to perform based on the HelpKeyword.")]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return _helpKeywordBehavior; }
			set { _helpKeywordBehavior = value; }
		}

		private string _helpString = String.Empty;
		[DefaultValue("")]
		[Description("The help text to display (if HelpKeyword is not specified).")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("txt")]
		public string HelpString
		{
			get { return _helpString; }
			set { _helpString = (value == null ? String.Empty : value); }
		}

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			HostNode.Session.InvokeHelp((sender == null ? this : sender), _helpKeyword, _helpKeywordBehavior, _helpString);
		}
	}

	public abstract class BaseConditionalAction : Action, IConditionalAction
	{
		// Condition
		private string _condition = String.Empty;
		public virtual string Condition
		{
			get { return _condition; }
			set
			{
				if (_condition != value)
				{
					_condition = (value == null ? String.Empty : value);
				}
			}
		}

		/// <summary> Executes the IAction child conditionally. </summary>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			foreach (Node localNode in Children)
				if (localNode is IAction && EvaluateCondition())
					((IAction)localNode).Execute(this, paramsValue);
		}

		protected abstract bool EvaluateCondition(); 		
	}	
}

