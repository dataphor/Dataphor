/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Client
{
	public class EventParams : HybridDictionary 
	{
		public EventParams() : base() {}
		
		public EventParams(params object[] AParameters) : base()
		{
			for (int LIndex = 0; LIndex < AParameters.Length; LIndex++)
				if ((LIndex % 2) != 0)
					Add(AParameters[LIndex - 1], AParameters[LIndex]);
		}
	}
	
	[DesignerImage("Image('Frontend', 'Nodes.Action')")]
	[DesignerCategory("Actions")]
	public abstract class Action : Node, IAction
    {
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				AfterExecute = null;
				BeforeExecute = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		public void Execute()
		{
			Execute(this, new EventParams());
		}

		public void Execute(INode ASender, EventParams AParams)
		{
			if (GetEnabled()) 
			{
				ILayoutDisableable LLayoutDisableable = (ILayoutDisableable)FindParent(typeof(ILayoutDisableable));
				if (LLayoutDisableable != null)
					LLayoutDisableable.DisableLayout();
				try 
				{
					DoBeforeExecute(AParams);
					InternalExecute(ASender, AParams);
					DoAfterExecute(AParams);
				}
				finally
				{
					if (LLayoutDisableable != null)
						LLayoutDisableable.EnableLayout();
				}
			}
		}

		protected abstract void InternalExecute(INode ASender, EventParams AParams);

		// BeforeExecute		
		private IAction FBeforeExecute;
		[Description("Action to be called before the execute.")]
		[TypeConverter(typeof(NodeReferenceConverter))]
		public IAction BeforeExecute
		{
			get { return FBeforeExecute; }
			set
			{
				if (FBeforeExecute != value)
				{
					if (FBeforeExecute != null)
						FBeforeExecute.Disposed -= new EventHandler(BeforeExecuteDisposed);
					FBeforeExecute = value;
					if (FBeforeExecute != null)
						FBeforeExecute.Disposed += new EventHandler(BeforeExecuteDisposed);
				}
			}
		}

		private void BeforeExecuteDisposed(object ASender, EventArgs AArgs)
		{
			BeforeExecute = null;
		}
		
		private void DoBeforeExecute(EventParams AParams)
		{
			if (FBeforeExecute != null)
				FBeforeExecute.Execute(this, AParams);
		}

		// AfterExecute		
		private IAction FAfterExecute;
		[Description("Action to be called after the execute.")]
		[TypeConverter(typeof(NodeReferenceConverter))]
		public IAction AfterExecute
		{
			get { return FAfterExecute; }
			set
			{
				if (FAfterExecute != value)
				{
					if (FAfterExecute != null)
						FAfterExecute.Disposed -= new EventHandler(AfterExecuteDisposed);
					FAfterExecute = value;
					if (FAfterExecute != null)
						FAfterExecute.Disposed += new EventHandler(AfterExecuteDisposed);
				}
			}
		}

		private void AfterExecuteDisposed(object ASender, EventArgs AArgs)
		{
			AfterExecute = null;
		}
		
		private void DoAfterExecute(EventParams AParams)
		{
			if (FAfterExecute != null)
				FAfterExecute.Execute(this, AParams);
		}

		// Text

		public event EventHandler OnTextChanged;

		private string FText = String.Empty;
		[DefaultValue("")]
		[Description("Text that can be used by visible controls that hook to this action.")]
		public string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					TextChanged();
				}
			}
		}

		public virtual string GetText()
		{
			return FText;
		}

		public virtual string GetDescription()
		{
			return FText.Replace(".", "").Replace("&", "");
		}

		protected virtual void TextChanged()
		{
			if (OnTextChanged != null)
				OnTextChanged(this, EventArgs.Empty);
		}
		
		// Enabled

		public event EventHandler OnEnabledChanged;
		
		private bool FEnabled = true;
		[DefaultValue(true)]
		[Description("When this is false the action should not be executed and any controls hooking to this action should be disabled.")]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (FEnabled != value)
				{
					FEnabled = value;
					EnabledChanged();
				}
			}
		}

		/// <summary> Remembers the value of GetEnabled so we know whether it has actually changed. </summary>
		private bool FActualEnabled;

		/// <summary> Called when the value of GetEnabled() may have changed. </summary>
		protected virtual void EnabledChanged()
		{
			bool LEnabled = GetEnabled();
			if (FActualEnabled != LEnabled)
			{
				FActualEnabled = LEnabled;
				if (OnEnabledChanged != null)
					OnEnabledChanged(this, EventArgs.Empty);
			}
		}

		public virtual bool GetEnabled()
		{
			return FEnabled;
		}

		// Hint

		public event EventHandler OnHintChanged;

		private string FHint = String.Empty;
		[DefaultValue("")]
		[Description("A text string that will be shown in tooltips, etc.")]
		public string Hint
		{
			get { return FHint; }
			set
			{
				if (FHint != value)
				{
					FHint = value;
					if (OnHintChanged != null)
						OnHintChanged(this, EventArgs.Empty);
				}
			}
		}

		// Image

		public event EventHandler OnImageChanged;

		private string FImage = String.Empty;
		[DefaultValue("")]
		[Description("An image used by this action's controls as an icon.")]
		public string Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					if (Active)
						InternalUpdateImage();
				}
			}
		}

		private System.Drawing.Image FLoadedImage;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public System.Drawing.Image LoadedImage
		{
			get { return FLoadedImage; }
		}

		private void SetImage(System.Drawing.Image AValue)
		{
			if (FLoadedImage != null)
				FLoadedImage.Dispose();
			FLoadedImage = AValue;
			if (OnImageChanged != null)
				OnImageChanged(this, EventArgs.Empty);
		}

		private PipeRequest FImageRequest;

		private void CancelImageRequest()
		{
			if (FImageRequest != null)
			{
				HostNode.Pipe.CancelRequest(FImageRequest);
				FImageRequest = null;
			}
		}

		// TODO: change this to use the AsyncImageRequest class

		/// <summary> Make sure that the image is loaded or creates an async request for it. </summary>
		private void InternalUpdateImage()
		{
			if (HostNode.Session.AreImagesLoaded())
			{
				CancelImageRequest();
				if (Image == String.Empty)
					SetImage(null);
				else
				{
					// Queue up an asynchronous request
					FImageRequest = new PipeRequest(Image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
					HostNode.Pipe.QueueRequest(FImageRequest);
				}
			}
			else
				SetImage(null);
		}

		private void ImageRead(PipeRequest ARequest, Pipe APipe)
		{
			if (Active)
			{
				FImageRequest = null;
				try
				{
					if (ARequest.Result.IsNative)
					{
						byte[] LResultBytes = ARequest.Result.AsByteArray;
						SetImage(System.Drawing.Image.FromStream(new MemoryStream(LResultBytes, 0, LResultBytes.Length, false, true)));
					}
					else
					{
						using (Stream LStream = ARequest.Result.OpenStream())
						{
							MemoryStream LCopyStream = new MemoryStream();
							StreamUtility.CopyStream(LStream, LCopyStream);
							SetImage(System.Drawing.Image.FromStream(LCopyStream));
						}
					}
				}
				catch
				{
					SetImage(ImageUtility.GetErrorImage());
				}
			}
		}

		private void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			FImageRequest = null;
			SetImage(ImageUtility.GetErrorImage());
		}

		// Visible

		private bool FVisible = true;
		[Description("Determines whether the controls that are associated with this action are visible.")]
		[DefaultValue(true)]
		public bool Visible
		{
			get { return FVisible; }
			set 
			{ 
				if (FVisible != value)
				{
					FVisible = value; 
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
			FActualEnabled = GetEnabled();
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
    
	[Description("Executes a list of actions sequentially, like a begin...end block.")]
	public class BlockAction : Action, IBlockAction
	{
		/// <remarks> Only actions are allowed as children. </remarks>
		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IAction).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		/// <summary> Executes each child action sequentially. </summary>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			// Don't use a foreach here to avoid the possibility that the action changes the form and throws the enumerator changed error
			for(int LCount = 0; LCount < (Children != null ? Children.Count : 0); LCount++) 
				((IAction)Children[LCount]).Execute(this, AParams);
		}
	}

	[Description("Executes the specified action node when executed.")]
	public class CallAction : Action, ICallAction
	{
		/// <remarks> Dereferences the referenced action. </remarks>
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Action = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		private IAction FAction;
		[Description("Action to be called upon executing.")]
		[TypeConverter(typeof(NodeReferenceConverter))]
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (value == this)
					throw new ClientException(ClientException.Codes.RecursiveActionReference, ToString());
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

		private void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}

		/// <summary> Calls the other action. </summary>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			if (FAction != null)
				FAction.Execute(this, AParams);
		}
	}

	public class HelpAction : Action, IHelpAction
	{
		private string FHelpKeyword = String.Empty;
		[DefaultValue("")]
		[Description("The keyword to use to navigate within the help.")]
		public string HelpKeyword
		{
			get { return FHelpKeyword; }
			set { FHelpKeyword = (value == null ? String.Empty : value); }
		}

		private HelpKeywordBehavior FHelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
		[DefaultValue(HelpKeywordBehavior.KeywordIndex)]
		[Description("Specifies the type of initial help navigation to perform based on the HelpKeyword.")]
		public HelpKeywordBehavior HelpKeywordBehavior
		{
			get { return FHelpKeywordBehavior; }
			set { FHelpKeywordBehavior = value; }
		}

		private string FHelpString = String.Empty;
		[DefaultValue("")]
		[Description("The help text to display (if HelpKeyword is not specified).")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("txt")]
		public string HelpString
		{
			get { return FHelpString; }
			set { FHelpString = (value == null ? String.Empty : value); }
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			HostNode.Session.InvokeHelp((ASender == null ? this : ASender), FHelpKeyword, FHelpKeywordBehavior, FHelpString);
		}
	}
}
