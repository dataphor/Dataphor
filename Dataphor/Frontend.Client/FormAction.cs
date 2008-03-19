/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.ComponentModel;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Client
{
	public class ExecuteModuleAction : Action, IExecuteModuleAction
	{
		public ExecuteModuleAction() {}
		
		protected override void Dispose(bool ADisposing)
		{
			EnsureModuleHostDisposed();
			base.Dispose(ADisposing);
		}

		// ModuleExpression
		private string FModuleExpression = String.Empty;		
		[Description("A D4 expression evaluating to a DIL document containing the module to be loaded.")]
		[DefaultValue("")]
		public string ModuleExpression
		{
			get { return FModuleExpression; }
			set 
			{ 
				if (FModuleExpression != value)
				{
					FModuleExpression = value == null ? String.Empty : value; 
					EnabledChanged();
				}
			}
		}
		
		// ActionName
		private string FActionName = String.Empty;
		[Description("The name of the action to be executed. An action with this name must be present in the module being loaded.")]
		[DefaultValue("")]
		public string ActionName
		{
			get { return FActionName; }
			set 
			{ 
				if (FActionName != value)
				{
					FActionName = value == null ? String.Empty : value; 
					EnabledChanged();
				}
			}
		}

		public override bool GetEnabled()
		{
			return base.GetEnabled() && (FModuleExpression != String.Empty) && (FActionName != String.Empty);
		}

		private IHost FModuleHost;
		private void EnsureModuleHostDisposed()
		{
			if (FModuleHost != null)
			{
				FModuleHost.Dispose();
				FModuleHost = null;
			}
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			EnsureModuleHostDisposed();
			
			FModuleHost = HostNode.Session.CreateHost();
			try
			{
				FModuleHost.Load(ModuleExpression, null);
				FModuleHost.Open();
				((IAction)FModuleHost.FindNode(FActionName)).Execute(this, AParams);
			}
			catch
			{
				EnsureModuleHostDisposed();
				throw;
			}
		}
	}

	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType,Alphora.Dataphor.Frontend.Client")]
	public class ShowFormAction : Action, IShowFormAction
	{
		public ShowFormAction() {}

		public ShowFormAction([PublishSource("SourceLinkType")] SourceLinkType ASourceLinkType): base()
		{
			SourceLinkType = ASourceLinkType;
		}
		
		protected override void Dispose(bool ADisposed)
		{
			try
			{
				OnFormClose = null;
				OnFormAccepted = null;
				OnFormRejected = null;
				BeforeFormActivated = null;
				AfterFormActivated = null;
			}
			finally
			{
				base.Dispose(ADisposed);
			}
		}

		// SourceLinkType

		// this link must be set first when deserializing.
		// which is why it is set in the constructor
		private SourceLinkType FSourceLinkType;
		[DefaultValue(SourceLinkType.None)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("Determines the data relationship between this document one that will be shown.")]
		public SourceLinkType SourceLinkType
		{
			get { return FSourceLinkType; }
			set
			{
				if (FSourceLinkType != value)
				{
					if (FSourceLink != null)
					{
						FSourceLink.OnSourceDataChanged -= new EventHandler(SourceLinkSourceDataChanged);
						FSourceLink.Dispose();
					}
					FSourceLinkType = value;
					if (FSourceLinkType == SourceLinkType.None)
						FSourceLink = null;
					else 
					{
						if (FSourceLinkType == SourceLinkType.Surrogate)
							FSourceLink = new SurrogateSourceLink(this);
						else if (FSourceLinkType == SourceLinkType.Detail)
							FSourceLink = new DetailSourceLink(this);
						FSourceLink.OnSourceDataChanged += new EventHandler(SourceLinkSourceDataChanged);
					}
				}
			}
		}

		private void SourceLinkSourceDataChanged(object ASender, EventArgs AArgs)
		{
			EnabledChanged();
		}

		// SourceLink

		private SourceLink FSourceLink;
		[BOP.Publish(BOP.PublishMethod.Inline)]
		[Description("Contains the specific settings based on the SourceLinkType.")]
		public SourceLink SourceLink
		{
			get { return FSourceLink; }
			set { FSourceLink = value; }
		}

		// SourceLinkRefresh

		private bool FSourceLinkRefresh = true;
		[DefaultValue(true)]
		[Description("If true and a sourcelink is set, the source is refreshed after execution.")]
		public bool SourceLinkRefresh
		{
			get { return FSourceLinkRefresh; }
			set { FSourceLinkRefresh = value; }
		}
		
		// SourceLinkRefreshKeyNames
		
		private string FSourceLinkRefreshKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("When refreshing the source link, determines the set of columns to be used to perform the refresh in the source.")]
		public string SourceLinkRefreshKeyNames
		{
			get { return FSourceLinkRefreshKeyNames; }
			set { FSourceLinkRefreshKeyNames = value == null ? String.Empty : value; }
		}
		
		// RefreshKeyNames
		
		private string FRefreshKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("When refreshing the source link, determines the set of columns to be used to retrieve the refresh information from the displayed form.")]
		public string RefreshKeyNames
		{
			get { return FRefreshKeyNames; }
			set { FRefreshKeyNames = value == null ? String.Empty : value; }
		}
		
		// OnFormClose

		protected IAction FOnFormClose;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the form has been closed.")]
		public IAction OnFormClose
		{
			get { return FOnFormClose; }
			set	
			{ 
				if (FOnFormClose != null)
					FOnFormClose.Disposed -= new EventHandler(FormCloseActionDisposed);
				FOnFormClose = value;	
				if (FOnFormClose != null)
					FOnFormClose.Disposed += new EventHandler(FormCloseActionDisposed);
			}
		}

		private void FormCloseActionDisposed(object ASender, EventArgs AArgs)
		{
			OnFormClose = null;
		}

		// OnFormAccepted

		protected IAction FOnFormAccepted;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute when the form is accepted.  Only called for forms that are shown modally.")]
		public IAction OnFormAccepted
		{
			get { return FOnFormAccepted; }
			set	
			{ 
				if (FOnFormAccepted != null)
					FOnFormAccepted.Disposed -= new EventHandler(FormAcceptedActionDisposed);
				FOnFormAccepted = value;	
				if (FOnFormAccepted != null)
					FOnFormAccepted.Disposed += new EventHandler(FormAcceptedActionDisposed);
			}
		}

		private void FormAcceptedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnFormAccepted = null;
		}

		// OnFormRejected

		protected IAction FOnFormRejected;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute when the form is rejected.  Only called for forms that are shown modally.")]
		public IAction OnFormRejected
		{
			get { return FOnFormRejected; }
			set	
			{ 
				if (FOnFormRejected != null)
					FOnFormRejected.Disposed -= new EventHandler(FormRejectedActionDisposed);
				FOnFormRejected = value;	
				if (FOnFormRejected != null)
					FOnFormRejected.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void FormRejectedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnFormRejected = null;
		}
		
		// BeforeFormActivated

		protected IAction FBeforeFormActivated;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the form is created, but before it is activated.")]
		public IAction BeforeFormActivated
		{
			get { return FBeforeFormActivated; }
			set	
			{ 
				if (FBeforeFormActivated != null)
					FBeforeFormActivated.Disposed -= new EventHandler(FormRejectedActionDisposed);
				FBeforeFormActivated = value;	
				if (FBeforeFormActivated != null)
					FBeforeFormActivated.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void BeforeFormActivatedDisposed(object ASender, EventArgs AArgs)
		{
			BeforeFormActivated = null;
		}

		// AfterFormActivated

		protected IAction FAfterFormActivated;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the form is activated, but before it is shown.")]
		public IAction AfterFormActivated
		{
			get { return FAfterFormActivated; }
			set	
			{ 
				if (FAfterFormActivated != null)
					FAfterFormActivated.Disposed -= new EventHandler(FormRejectedActionDisposed);
				FAfterFormActivated = value;	
				if (FAfterFormActivated != null)
					FAfterFormActivated.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void AfterFormActivatedDisposed(object ASender, EventArgs AArgs)
		{
			AfterFormActivated = null;
		}

		// Document

		private string FDocument = String.Empty;
		[Description("A Document expression returning a form interface to be shown.")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return FDocument; }
			set
			{
				if (value != FDocument)
				{
					FDocument = value;
					EnabledChanged();
				}
			}
		}

		// Filter

		private string FFilter = String.Empty;
		[DefaultValue("")]
		[Description("Filter to apply to the main source of the target form.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Filter
		{
			get { return FFilter; }
			set { FFilter = value; }
		}

		// Mode

		private FormMode FMode;
		[DefaultValue(FormMode.None)]
		[Description("FormMode used when opening the interface.")]
		public FormMode Mode
		{
			get { return FMode; }
			set
			{
				if (value != FMode)
				{
					FMode = value;
					EnabledChanged();
				}
			}
		}

		// AutoAcceptAfterInsertOnQuery

		private bool FAutoAcceptAfterInsertOnQuery = true;
		[DefaultValue(true)]
		[Description("When true, if the current form is being queried (typically as a lookup) and the shown form is in insert mode and is accepted, the current form will automatically be accepted.")]
		public bool AutoAcceptAfterInsertOnQuery
		{
			get { return FAutoAcceptAfterInsertOnQuery; }
			set { FAutoAcceptAfterInsertOnQuery = value; }
		}

		// Action

		public override bool GetEnabled()
		{
			return
				base.GetEnabled() 
					&& (FDocument != String.Empty) 
					&&
					(
						(SourceLinkType == SourceLinkType.None) 
							|| (FMode == FormMode.Insert) 
							|| 
							(
								(SourceLink.Source != null) 
									&& (SourceLink.Source.DataView != null)
									&& SourceLink.Source.DataView.Active
									&&
									(
										(!SourceLink.Source.DataView.IsEmpty())
										||
										(
											(SourceLink is DetailSourceLink)
												&& ((DetailSourceLink)SourceLink).AttachMaster
												&& SourceLink.Source.DataView.IsMasterValid()
										)
									)
							)
					);
		}

		// ConfirmDelete
				
		private bool FConfirmDelete = true;
		[Description("Indicates whether a confirm form will be displayed.")]
		[DefaultValue(true)]
		public bool ConfirmDelete
		{
			get { return FConfirmDelete; }
			set { FConfirmDelete = value; }
		}

		// UseOpenState
		
		private bool FUseOpenState = true;
		[Description("Determines whether or not to set the OpenState property of the main Source of the form being shown.")]
		[DefaultValue(true)]
		public bool UseOpenState
		{
			get { return FUseOpenState; }
			set { FUseOpenState = value; }
		}

		// ManageWriteOnly
		
		private bool FManageWriteOnly = true;
		[Description("Determines whether or not to set the IsWriteOnly property of the main Source of the form being shown.")]
		[DefaultValue(true)]
		public bool ManageWriteOnly
		{
			get { return FManageWriteOnly; }
			set { FManageWriteOnly = value; }
		}

		// ManageRefreshAfterPost
		
		private bool FManageRefreshAfterPost = true;
		[Description("Determines whether or not to set the RefreshAfterPost property of the main Source of the form being shown.")]
		[DefaultValue(true)]
		public bool ManageRefreshAfterPost
		{
			get { return FManageRefreshAfterPost; }
			set { FManageRefreshAfterPost = value; }
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			if (FDocument != String.Empty)
			{
				if ((FMode == FormMode.Delete) && !ConfirmDelete)
				{
					SourceLink.Source.DataView.Delete();
				}
				else
				{
					IFormInterface LForm = HostNode.Session.LoadForm(this, FDocument, new FormInterfaceHandler(InternalBeforeActivateForm));
					try
					{
						LForm.OnClosed += new EventHandler(OnClosedHandler);
						InternalAfterActivateForm(LForm);
						if ((FMode != FormMode.None) || (SourceLinkType != SourceLinkType.None) || (FOnFormAccepted != null) || (FOnFormRejected != null))
						{
							LForm.Show
							(
								(IFormInterface)FindParent(typeof(IFormInterface)),
								new FormInterfaceHandler(FormAccepted),
								new FormInterfaceHandler(FormRejected),
								FMode
							);
						}
						else
							LForm.Show(FMode);
					}
					catch
					{
						LForm.Dispose();
						throw;
					}
				}
			}
		}

		protected void FormRejected(IFormInterface AForm)
		{
			if (FOnFormRejected != null)
				FOnFormRejected.Execute(this, new EventParams("AForm", AForm));
		}

		protected void FormAccepted(IFormInterface AForm)
		{
			if 
			(
				FSourceLinkRefresh && 
				(FSourceLink != null) && 
				(FSourceLink.Source != null) && 
				(FSourceLink.Source.DataView.State == DataSetState.Browse) && 
				(AForm.MainSource != null) //&&
				//!Object.ReferenceEquals(FSourceLink.Source.DataView, AForm.MainSource.DataView) // Do not refresh if this is a surrogate
			)
			{
				switch (Mode)
				{
					case FormMode.Delete:
						AForm.MainSource.DataView.Close(); // Close the data view first to prevent the following refresh from causing an unnecessary requery
						FSourceLink.Source.DataView.Refresh();
					break;

					case FormMode.Insert:
					case FormMode.Edit:
						// Find the nearest row in current set
						DataView LSourceView = FSourceLink.Source.DataView;
						DataView LTargetView = AForm.MainSource.DataView;
						
						// Determine RefreshSourceKey
						// Determine RefreshKey
						
						// if SourceLinkRefreshKeyNames and RefreshKeyNames are specified, use them, otherwise
						// if the current order of the source link data view is a subset of the columns in the detail view, use it, otherwise
						// find the minimum key in the source link data view that is a subset of the columns in the detail view and use it
						
						Schema.Order LSourceKey = null;
						Schema.Order LTargetKey = null;
						
						if ((SourceLinkRefreshKeyNames != "") && (RefreshKeyNames != ""))
						{
							string[] LSourceKeyNames = SourceLinkRefreshKeyNames.Split(new char[]{';', ','});
							string[] LTargetKeyNames = RefreshKeyNames.Split(new char[]{';', ','});
							if (LSourceKeyNames.Length == LTargetKeyNames.Length)
							{
								LSourceKey = new Schema.Order();
								LTargetKey = new Schema.Order();
								for (int LIndex = 0; LIndex < LSourceKeyNames.Length; LIndex++)
								{
									LSourceKey.Columns.Add(new Schema.OrderColumn(LSourceView.TableVar.Columns[LSourceKeyNames[LIndex]], true));
									LTargetKey.Columns.Add(new Schema.OrderColumn(LTargetView.TableVar.Columns[LTargetKeyNames[LIndex]], true));
								}
							}
						}
						
						if (LSourceKey == null)
						{
							if ((LSourceView.Order != null) && LSourceView.Order.Columns.IsSubsetOf(LTargetView.TableVar.Columns))
							{
								LSourceKey = LSourceView.Order;
								LTargetKey = LSourceView.Order;
							}
							else
							{
								Schema.Key LMinimumKey = LSourceView.TableVar.Keys.MinimumSubsetKey(LTargetView.TableVar.Columns);
								if (LMinimumKey != null)
								{
									LSourceKey = new Schema.Order(LMinimumKey);
									LTargetKey = LSourceKey;
								}
							}
						}

						if (LSourceKey != null)
						{						
							using (Row LRow = new Row(LSourceView.Process, new Schema.RowType(LSourceKey.Columns)))
							{
								for (int LIndex = 0; LIndex < LSourceKey.Columns.Count; LIndex++)
								{
									DataField LTargetField = LTargetView[LTargetKey.Columns[LIndex].Column.Name];
									if (LTargetField.HasValue())
										LRow[LIndex] = LTargetField.Value;
									else
										LRow.ClearValue(LIndex);
								}
								
								LTargetView.Close(); // to prevent unnecessary requery

								string LSaveOrder = String.Empty;								
								if (!LSourceView.Order.Equals(LSourceKey))
								{
									LSaveOrder = LSourceView.OrderString;
									try
									{
										LSourceView.Order = LSourceKey;
									}
									catch (Exception LException)
									{
										if (LSourceView.OrderString != LSaveOrder)
											LSourceView.OrderString = LSaveOrder;
										else
											LSourceView.Refresh();
										throw new ClientException(ClientException.Codes.UnableToFindModifiedRow, LException);
									}
								}
								try
								{
									LSourceView.Refresh(LRow);
								}
								finally
								{
									if ((LSaveOrder != String.Empty) && (LSourceView.OrderString != LSaveOrder))
										LSourceView.OrderString = LSaveOrder;
								}
							}
						}
						else
						{
							LTargetView.Close();
							LSourceView.Refresh();
						}
					break;
				}
			}

			if ((Mode == FormMode.Insert) && FAutoAcceptAfterInsertOnQuery)
			{
				IFormInterface LForm = (IFormInterface)FindParent(typeof(IFormInterface));
				if (LForm.Mode == FormMode.Query)
					LForm.Close(CloseBehavior.AcceptOrClose);
			}
			
			if (FOnFormAccepted != null)
				FOnFormAccepted.Execute(this, new EventParams("AForm", AForm));
		}

		protected virtual void OnClosedHandler(object sender, EventArgs e)
		{
			if (FSourceLink != null)
				FSourceLink.TargetSource = null;
			if (FOnFormClose != null)
				FOnFormClose.Execute(this, new EventParams("AForm", sender));
		}

		// Node

		protected internal override void AfterActivate()
		{
			EnabledChanged();
			base.AfterActivate();
		}

		protected void InternalBeforeActivateForm(IFormInterface AForm)
		{
			if (AForm.MainSource != null)
				AForm.MainSource.Filter = FFilter;

			if (FSourceLink != null) 
				FSourceLink.TargetSource = AForm.MainSource;
				
			switch (FMode)
			{
				case FormMode.Insert : 
					if (FUseOpenState || FManageRefreshAfterPost || FManageWriteOnly)
					{
						AForm.CheckMainSource();
						if (FUseOpenState)
							AForm.MainSource.OpenState = DAE.Client.DataSetState.Insert;
						if (FManageRefreshAfterPost)
							AForm.MainSource.RefreshAfterPost = false;
						if (FManageWriteOnly)
							AForm.MainSource.IsWriteOnly = true;
					}
					break;

				case FormMode.Edit : 
					if (FUseOpenState || FManageRefreshAfterPost)
					{
						AForm.CheckMainSource();
						if (FUseOpenState)
							AForm.MainSource.OpenState = DAE.Client.DataSetState.Edit; 
						if (FManageRefreshAfterPost)
							AForm.MainSource.RefreshAfterPost = false;
					}
					break;
			}

			if (FBeforeFormActivated != null)
				FBeforeFormActivated.Execute(this, new EventParams("AForm", AForm));
		}

		protected void InternalAfterActivateForm(IFormInterface AForm)
		{
			if (FAfterFormActivated != null)
				FAfterFormActivated.Execute(this, new EventParams("AForm", AForm));
		}
	}

	public class FormAction : Action, IFormAction
	{
		// Behavior

		private CloseBehavior FBehavior = CloseBehavior.RejectOrClose;
		[DefaultValue(CloseBehavior.RejectOrClose)]
		[Description("The behavior of the FormAction.")]
		public CloseBehavior Behavior
		{
			get { return FBehavior; }
			set { FBehavior = value; }
		}

		// Action

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			((IFormInterface)FindParent(typeof(IFormInterface))).Close(FBehavior);
		}
	}

	public class SetNextRequestAction : Action, ISetNextRequestAction
	{
		// Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
		[Description("The Document of the next user interface document to be loaded.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return FDocument; }
			set { FDocument = value; }
		}

		// Action

		/// <remarks> Sets the next request property of the hostnode.</remarks>
		/// <seealso cref="IHost.NextRequest"/>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			Request LRequest = HostNode.NextRequest;
			if (LRequest == null)
				HostNode.NextRequest = new Request(Document);
			else
				LRequest.Document = Document;
		}

	}

	public class ClearNextRequestAction : Action, IClearNextRequestAction
	{
		// Action

		/// <remarks> Clears the next request property of the hostnode. </remarks>
        /// <seealso cref="IHost.NextRequest"/>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			HostNode.NextRequest = null;
		}
	}

	public class FormShownEvent : NodeEvent { }
}