/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Language;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.Frontend.Client
{
	public class ExecuteModuleAction : Action, IExecuteModuleAction
	{
		public ExecuteModuleAction() {}
		
		protected override void Dispose(bool disposing)
		{
			EnsureModuleHostDisposed();
			base.Dispose(disposing);
		}

		// ModuleExpression
		private string _moduleExpression = String.Empty;		
		[Description("A D4 expression evaluating to a DIL document containing the module to be loaded.")]
		[DefaultValue("")]
		public string ModuleExpression
		{
			get { return _moduleExpression; }
			set 
			{ 
				if (_moduleExpression != value)
				{
					_moduleExpression = value == null ? String.Empty : value; 
					EnabledChanged();
				}
			}
		}
		
		// ActionName
		private string _actionName = String.Empty;
		[Description("The name of the action to be executed. An action with this name must be present in the module being loaded.")]
		[DefaultValue("")]
		public string ActionName
		{
			get { return _actionName; }
			set 
			{ 
				if (_actionName != value)
				{
					_actionName = value == null ? String.Empty : value; 
					EnabledChanged();
				}
			}
		}

		public override bool GetEnabled()
		{
			return base.GetEnabled() && (_moduleExpression != String.Empty) && (_actionName != String.Empty);
		}

		private IHost _moduleHost;
		private void EnsureModuleHostDisposed()
		{
			if (_moduleHost != null)
			{
				_moduleHost.Dispose();
				_moduleHost = null;
			}
		}

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			EnsureModuleHostDisposed();
			
			_moduleHost = HostNode.Session.CreateHost();
			try
			{
				_moduleHost.Load(ModuleExpression, null);
				_moduleHost.Open();
				((IAction)_moduleHost.FindNode(_actionName)).Execute(this, paramsValue);
			}
			catch
			{
				EnsureModuleHostDisposed();
				throw;
			}
		}
	}

	[PublishDefaultConstructor("Alphora.Dataphor.Frontend.Client.SourceLinkType")]
	public class ShowFormAction : Action, IShowFormAction
	{
		public ShowFormAction() {}

		public ShowFormAction([PublishSource("SourceLinkType")] SourceLinkType sourceLinkType): base()
		{
			SourceLinkType = sourceLinkType;
		}
		
		protected override void Dispose(bool disposed)
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
				base.Dispose(disposed);
			}
		}

		// SourceLinkType

		// this link must be set first when deserializing.
		// which is why it is set in the constructor
		private SourceLinkType _sourceLinkType;
		[DefaultValue(SourceLinkType.None)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("Determines the data relationship between this document one that will be shown.")]
		public SourceLinkType SourceLinkType
		{
			get { return _sourceLinkType; }
			set
			{
				if (_sourceLinkType != value)
				{
					if (_sourceLink != null)
					{
						_sourceLink.OnSourceDataChanged -= new EventHandler(SourceLinkSourceDataChanged);
						_sourceLink.Dispose();
					}
					_sourceLinkType = value;
					if (_sourceLinkType == SourceLinkType.None)
						_sourceLink = null;
					else 
					{
						if (_sourceLinkType == SourceLinkType.Surrogate)
							_sourceLink = new SurrogateSourceLink(this);
						else if (_sourceLinkType == SourceLinkType.Detail)
							_sourceLink = new DetailSourceLink(this);
						_sourceLink.OnSourceDataChanged += new EventHandler(SourceLinkSourceDataChanged);
					}
				}
			}
		}

		private void SourceLinkSourceDataChanged(object sender, EventArgs args)
		{
			EnabledChanged();
		}

		// SourceLink

		private SourceLink _sourceLink;
		[BOP.Publish(BOP.PublishMethod.Inline)]
		[Description("Contains the specific settings based on the SourceLinkType.")]
		public SourceLink SourceLink
		{
			get { return _sourceLink; }
			set { _sourceLink = value; }
		}

		// SourceLinkRefresh

		private bool _sourceLinkRefresh = true;
		[DefaultValue(true)]
		[Description("If true and a sourcelink is set, the source is refreshed after execution.")]
		public bool SourceLinkRefresh
		{
			get { return _sourceLinkRefresh; }
			set { _sourceLinkRefresh = value; }
		}
		
		// SourceLinkRefreshKeyNames
		
		private string _sourceLinkRefreshKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("When refreshing the source link, determines the set of columns to be used to perform the refresh in the source.")]
		public string SourceLinkRefreshKeyNames
		{
			get { return _sourceLinkRefreshKeyNames; }
			set { _sourceLinkRefreshKeyNames = value == null ? String.Empty : value; }
		}
		
		// RefreshKeyNames
		
		private string _refreshKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("When refreshing the source link, determines the set of columns to be used to retrieve the refresh information from the displayed form.")]
		public string RefreshKeyNames
		{
			get { return _refreshKeyNames; }
			set { _refreshKeyNames = value == null ? String.Empty : value; }
		}
		
		// OnFormClose

		protected IAction _onFormClose;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the form has been closed.")]
		public IAction OnFormClose
		{
			get { return _onFormClose; }
			set	
			{ 
				if (_onFormClose != null)
					_onFormClose.Disposed -= new EventHandler(FormCloseActionDisposed);
				_onFormClose = value;	
				if (_onFormClose != null)
					_onFormClose.Disposed += new EventHandler(FormCloseActionDisposed);
			}
		}

		private void FormCloseActionDisposed(object sender, EventArgs args)
		{
			OnFormClose = null;
		}

		// OnFormAccepted

		protected IAction _onFormAccepted;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute when the form is accepted.  Only called for forms that are shown modally.")]
		public IAction OnFormAccepted
		{
			get { return _onFormAccepted; }
			set	
			{ 
				if (_onFormAccepted != null)
					_onFormAccepted.Disposed -= new EventHandler(FormAcceptedActionDisposed);
				_onFormAccepted = value;	
				if (_onFormAccepted != null)
					_onFormAccepted.Disposed += new EventHandler(FormAcceptedActionDisposed);
			}
		}

		private void FormAcceptedActionDisposed(object sender, EventArgs args)
		{
			OnFormAccepted = null;
		}

		// OnFormRejected

		protected IAction _onFormRejected;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute when the form is rejected.  Only called for forms that are shown modally.")]
		public IAction OnFormRejected
		{
			get { return _onFormRejected; }
			set	
			{ 
				if (_onFormRejected != null)
					_onFormRejected.Disposed -= new EventHandler(FormRejectedActionDisposed);
				_onFormRejected = value;	
				if (_onFormRejected != null)
					_onFormRejected.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void FormRejectedActionDisposed(object sender, EventArgs args)
		{
			OnFormRejected = null;
		}
		
		// BeforeFormActivated

		protected IAction _beforeFormActivated;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the form is created, but before it is activated.")]
		public IAction BeforeFormActivated
		{
			get { return _beforeFormActivated; }
			set	
			{ 
				if (_beforeFormActivated != null)
					_beforeFormActivated.Disposed -= new EventHandler(FormRejectedActionDisposed);
				_beforeFormActivated = value;	
				if (_beforeFormActivated != null)
					_beforeFormActivated.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void BeforeFormActivatedDisposed(object sender, EventArgs args)
		{
			BeforeFormActivated = null;
		}

		// AfterFormActivated

		protected IAction _afterFormActivated;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the form is activated, but before it is shown.")]
		public IAction AfterFormActivated
		{
			get { return _afterFormActivated; }
			set	
			{ 
				if (_afterFormActivated != null)
					_afterFormActivated.Disposed -= new EventHandler(FormRejectedActionDisposed);
				_afterFormActivated = value;	
				if (_afterFormActivated != null)
					_afterFormActivated.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void AfterFormActivatedDisposed(object sender, EventArgs args)
		{
			AfterFormActivated = null;
		}

		// Document

		private string _document = String.Empty;
		[Description("A Document expression returning a form interface to be shown.")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return _document; }
			set
			{
				if (value != _document)
				{
					_document = value;
					EnabledChanged();
				}
			}
		}

		// Filter

		private string _filter = String.Empty;
		[DefaultValue("")]
		[Description("Filter to apply to the main source of the target form.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Filter
		{
			get { return _filter; }
			set { _filter = value; }
		}
		
		// Mode

		private FormMode _mode;
		[DefaultValue(FormMode.None)]
		[Description("FormMode used when opening the interface.")]
		public FormMode Mode
		{
			get { return _mode; }
			set
			{
				if (value != _mode)
				{
					_mode = value;
					EnabledChanged();
				}
			}
		}

		// AutoAcceptAfterInsertOnQuery

		private bool _autoAcceptAfterInsertOnQuery = true;
		[DefaultValue(true)]
		[Description("When true, if the current form is being queried (typically as a lookup) and the shown form is in insert mode and is accepted, the current form will automatically be accepted.")]
		public bool AutoAcceptAfterInsertOnQuery
		{
			get { return _autoAcceptAfterInsertOnQuery; }
			set { _autoAcceptAfterInsertOnQuery = value; }
		}

		// TopMost

		private bool _topMost;
		[DefaultValue(false)]
		[Description("The TopMost setting of the shown form.")]
		public bool TopMost
		{
			get { return _topMost; }
			set { _topMost = value; }
		}

		// Action

		public override bool GetEnabled()
		{
			return
				base.GetEnabled() 
					&& (_document != String.Empty) 
					&&
					(
						(SourceLinkType == SourceLinkType.None) 
							|| (_mode == FormMode.Insert) 
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
				
		private bool _confirmDelete = true;
		[Description("Indicates whether a confirm form will be displayed.")]
		[DefaultValue(true)]
		public bool ConfirmDelete
		{
			get { return _confirmDelete; }
			set { _confirmDelete = value; }
		}

		// UseOpenState
		
		private bool _useOpenState = true;
		[Description("Determines whether or not to set the OpenState property of the main Source of the form being shown.")]
		[DefaultValue(true)]
		public bool UseOpenState
		{
			get { return _useOpenState; }
			set { _useOpenState = value; }
		}

		// ManageWriteOnly
		
		private bool _manageWriteOnly = true;
		[Description("Determines whether or not to set the IsWriteOnly property of the main Source of the form being shown.")]
		[DefaultValue(true)]
		public bool ManageWriteOnly
		{
			get { return _manageWriteOnly; }
			set { _manageWriteOnly = value; }
		}

		// ManageRefreshAfterPost
		
		private bool _manageRefreshAfterPost = true;
		[Description("Determines whether or not to set the RefreshAfterPost property of the main Source of the form being shown.")]
		[DefaultValue(true)]
		public bool ManageRefreshAfterPost
		{
			get { return _manageRefreshAfterPost; }
			set { _manageRefreshAfterPost = value; }
		}
		
		// OnCompleted (IBlockable)
		public event NodeEventHandler OnCompleted;
		private void DoCompleted(EventParams paramsValue)
		{
			if (OnCompleted != null)
				OnCompleted(this, paramsValue);
		}

		// These hooks are provided so that the ShowFormAction can be used in the eventing system
		public event FormInterfaceHandler OnFormAcceptedEvent;
		public event FormInterfaceHandler OnFormRejectedEvent;
		private DataParams _mainSourceParams;
		private EventParams _params;

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			if (_document != String.Empty)
			{
				_params = paramsValue;
				if ((_mode == FormMode.Delete) && !ConfirmDelete)
				{
					SourceLink.Source.DataView.Delete();
				}
				else
				{
					_mainSourceParams = paramsValue["AParams"] as DataParams;
					try
					{
						IFormInterface form = HostNode.Session.LoadForm(this, _document, new FormInterfaceHandler(InternalBeforeActivateForm));
						try
						{
							form.OnClosed += new EventHandler(OnClosedHandler);
							form.TopMost = TopMost;
							InternalAfterActivateForm(form);
							bool forceAcceptReject = (_onFormAccepted != null) || (_onFormRejected != null) || (OnFormAcceptedEvent != null) || (OnFormRejectedEvent != null);
							if ((_mode != FormMode.None) || (SourceLinkType != SourceLinkType.None) || forceAcceptReject)
							{
								if (forceAcceptReject)
									form.ForceAcceptReject = true;
								form.Show
								(
									(IFormInterface)FindParent(typeof(IFormInterface)),
									new FormInterfaceHandler(FormAccepted),
									new FormInterfaceHandler(FormRejected),
									_mode
								);
							}
							else
								form.Show(_mode);
						}
						catch
						{
							form.Dispose();
							throw;
						}
					}
					finally
					{
						_mainSourceParams = null;
					}
				}
			}
		}

		protected void FormRejected(IFormInterface form)
		{
			if (_onFormRejected != null)
				_onFormRejected.Execute(this, new EventParams("AForm", form));
				
			if (OnFormRejectedEvent != null)
				OnFormRejectedEvent(form);
		}
		
		protected void FormAccepted(IFormInterface form)
		{
			if 
			(
				_sourceLinkRefresh && 
				(_sourceLink != null) && 
				(_sourceLink.Source != null) && 
				(_sourceLink.Source.DataView.State == DataSetState.Browse) && 
				(form.MainSource != null) //&&
				//!Object.ReferenceEquals(FSourceLink.Source.DataView, AForm.MainSource.DataView) // Do not refresh if this is a surrogate
			)
			{
				switch (Mode)
				{
					case FormMode.Delete:
						form.MainSource.DataView.Close(); // Close the data view first to prevent the following refresh from causing an unnecessary requery
						_sourceLink.Source.DataView.Refresh();
					break;

					case FormMode.Insert:
					case FormMode.Edit:
						// Find the nearest row in current set
						DataView sourceView = _sourceLink.Source.DataView;
						DataView targetView = form.MainSource.DataView;
						
						if (sourceView != targetView)
						{
							// Determine RefreshSourceKey
							// Determine RefreshKey
							
							// if SourceLinkRefreshKeyNames and RefreshKeyNames are specified, use them, otherwise
							// if the current order of the source link data view is a subset of the columns in the detail view, use it, otherwise
							// find the minimum key in the source link data view that is a subset of the columns in the detail view and use it
							
							Schema.Order sourceKey = null;
							Schema.Order targetKey = null;
							
							if ((SourceLinkRefreshKeyNames != "") && (RefreshKeyNames != ""))
							{
								string[] sourceKeyNames = SourceLinkRefreshKeyNames.Split(new char[]{';', ','});
								string[] targetKeyNames = RefreshKeyNames.Split(new char[]{';', ','});
								if (sourceKeyNames.Length == targetKeyNames.Length)
								{
									sourceKey = new Schema.Order();
									targetKey = new Schema.Order();
									for (int index = 0; index < sourceKeyNames.Length; index++)
									{
										sourceKey.Columns.Add(new Schema.OrderColumn(sourceView.TableVar.Columns[sourceKeyNames[index]], true));
										targetKey.Columns.Add(new Schema.OrderColumn(targetView.TableVar.Columns[targetKeyNames[index]], true));
									}
								}
							}
							
							if (sourceKey == null)
							{
								if ((sourceView.Order != null) && sourceView.Order.Columns.IsSubsetOf(targetView.TableVar.Columns))
								{
									sourceKey = sourceView.Order;
									targetKey = sourceView.Order;
								}
								else
								{
									Schema.Key minimumKey = sourceView.TableVar.Keys.MinimumSubsetKey(targetView.TableVar.Columns);
									if (minimumKey != null)
									{
										sourceKey = new Schema.Order(minimumKey);
										targetKey = sourceKey;
									}
								}
							}

							if (sourceKey != null)
							{						
								using (Row row = new Row(sourceView.Process.ValueManager, new Schema.RowType(sourceKey.Columns)))
								{
									for (int index = 0; index < sourceKey.Columns.Count; index++)
									{
										DataField targetField = targetView[targetKey.Columns[index].Column.Name];
										if (targetField.HasValue())
											row[index] = targetField.Value;
										else
											row.ClearValue(index);
									}
									
									targetView.Close(); // to prevent unnecessary requery

									string saveOrder = String.Empty;								
									if (!sourceView.Order.Equals(sourceKey))
									{
										saveOrder = sourceView.OrderString;
										try
										{
											sourceView.Order = sourceKey;
										}
										catch (Exception exception)
										{
											if (sourceView.OrderString != saveOrder)
												sourceView.OrderString = saveOrder;
											else
												sourceView.Refresh();
											throw new ClientException(ClientException.Codes.UnableToFindModifiedRow, exception);
										}
									}
									try
									{
										sourceView.Refresh(row);
									}
									finally
									{
										if ((saveOrder != String.Empty) && (sourceView.OrderString != saveOrder))
											sourceView.OrderString = saveOrder;
									}
								}
							}
							else
							{
								targetView.Close();
								sourceView.Refresh();
							}
						}
					break;
				}
			}

			if ((Mode == FormMode.Insert) && _autoAcceptAfterInsertOnQuery)
			{
				IFormInterface localForm = (IFormInterface)FindParent(typeof(IFormInterface));
				if (localForm.Mode == FormMode.Query)
					localForm.Close(CloseBehavior.AcceptOrClose);
			}
			
			if (_onFormAccepted != null)
				_onFormAccepted.Execute(this, new EventParams("AForm", form));
				
			if (OnFormAcceptedEvent != null)
				OnFormAcceptedEvent(form);
		}

		protected virtual void OnClosedHandler(object sender, EventArgs e)
		{
			if (_onFormClose != null)
				_onFormClose.Execute(this, new EventParams("AForm", sender));

			// Disable the source(s) first before disestablishing the link
			((IFormInterface)sender).BroadcastEvent(new DisableSourceEvent());
			
			if (_sourceLink != null)
				_sourceLink.TargetSource = null;
				
			DoCompleted(_params);

			_params = null;
		}

		// Node

		protected internal override void AfterActivate()
		{
			EnabledChanged();
			base.AfterActivate();
		}

		protected void InternalBeforeActivateForm(IFormInterface form)
		{
			if (form.MainSource != null)
			{
                if (!String.IsNullOrEmpty(_filter))
				    form.MainSource.Filter = _filter;
                if (_mainSourceParams != null)
				    form.MainSource.Params = _mainSourceParams;
				form.MainSource.Default += new DataLinkHandler(DefaultData);
			}

			if (_sourceLink != null) 
				_sourceLink.TargetSource = form.MainSource;
				
			switch (_mode)
			{
				case FormMode.Insert : 
					if (_useOpenState || _manageRefreshAfterPost || _manageWriteOnly)
					{
						form.CheckMainSource();
						if (_useOpenState)
							form.MainSource.OpenState = DAE.Client.DataSetState.Insert;
						if (_manageRefreshAfterPost)
							form.MainSource.RefreshAfterPost = false;
						if (_manageWriteOnly)
							form.MainSource.IsWriteOnly = true;
					}
					break;

				case FormMode.Edit : 
					if (_useOpenState || _manageRefreshAfterPost)
					{
						form.CheckMainSource();
						if (_useOpenState)
							form.MainSource.OpenState = DAE.Client.DataSetState.Edit; 
						if (_manageRefreshAfterPost)
							form.MainSource.RefreshAfterPost = false;
					}
					break;
			}

			if (_beforeFormActivated != null)
				_beforeFormActivated.Execute(this, new EventParams("AForm", form));
		}

		protected virtual void DefaultData(DataLink link, DataSet dataSet)
		{
			foreach (Node child in Children)
			{
				var defaultValue = child as DataDefault;
				if (defaultValue != null)
					defaultValue.PerformDefault(link, _params);
			}
		}

		protected void InternalAfterActivateForm(IFormInterface form)
		{
			if (_afterFormActivated != null)
				_afterFormActivated.Execute(this, new EventParams("AForm", form));
		}

		public override bool IsValidChild(Type childType)
		{
			return typeof(DataDefault).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.DataArgument')")]
	[DesignerCategory("Non Visual")]
	public abstract class DataDefault : Node, IDataDefault
	{
		// TargetColumn

		private string _targetColumns = String.Empty;
		[DefaultValue("")]
		[Description("The comma or semicolon separated list of columns in the Target source that are to be defaulted.")]
		public string TargetColumns
		{
			get { return _targetColumns; }
			set { _targetColumns = value; }
		}
		
		// Enabled
		
		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("The default will only be performed if Enabled is true.")]
		public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}
		
		protected virtual bool GetEnabled()
		{
			return _enabled && !String.IsNullOrEmpty(_targetColumns);
		}
		
		protected internal void PerformDefault(DataLink link, EventParams paramsValue)
		{
			if (_enabled && link.Active)
				InternalPerformDefault(link, _targetColumns.Split(';', ','), paramsValue);
		}

		protected abstract void InternalPerformDefault(DataLink link, string[] targetColumns, EventParams paramsValue);
	}

	/// <summary> Defaults data from a data source. </summary>
	public class DataSourceDefault : DataDefault, ISourceReference, IDataSourceDefault
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}

		// Source

		private ISource _source;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The source from which to pull the data.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					if (_source != null)
						_source.Disposed -= new EventHandler(SourceDisposed);
					_source = value;
					if (_source != null)
						_source.Disposed += new EventHandler(SourceDisposed);
				}
			}
		}

		private void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}

		// SourceColumns

		private string _sourceColumns = String.Empty;
		[DefaultValue("")]
		[Description("The columns in the Source source that are to be used to default from.")]
		public string SourceColumns
		{
			get { return _sourceColumns; }
			set { _sourceColumns = value; }
		}

		protected override bool GetEnabled()
		{
			return (_source != null) && (_source.DataView != null) && _source.DataView.Active && !String.IsNullOrEmpty(_sourceColumns) && base.GetEnabled();
		}

		protected override void InternalPerformDefault(DataLink link, string[] targetColumns, EventParams paramsValue)
		{
			var sourceColumns = _sourceColumns.Split(',', ';');
			for (int i = 0; i < Math.Min(sourceColumns.Length, targetColumns.Length); i++)
			{
				var sourceField = _source[sourceColumns[i].Trim()];
				var targetField = link.DataSet[targetColumns[i].Trim()];
				if (sourceField.HasValue())
					targetField.AsNative = sourceField.AsNative;
				else
					targetField.ClearValue();
			}
		}
	}
	
	/// <summary> Defaults data from a set of specified values. </summary>
	public class DataValueDefault : DataDefault, IDataValueDefault
	{
		// SourceValues

		private string _sourceValues = String.Empty;
		[Description("Source values in D4 list literal format (e.g. 'String value', nil, 5 )")]
		public string SourceValues
		{
			get { return _sourceValues; }
			set 
			{
				value = value == null ? String.Empty : value;
				var expressions = new Alphora.Dataphor.DAE.Language.D4.Parser().ParseExpressionList(value);
				// validate that all expressions are literals
				foreach (Expression expression in expressions)
					if (!(expression is ValueExpression))
						throw new ClientException(ClientException.Codes.ValueExpressionExpected);
				_sourceValueExpressions = expressions;
				_sourceValues = value; 
			}
		}

		private List<Expression> _sourceValueExpressions;

		protected override void InternalPerformDefault(DataLink link, string[] targetColumns, EventParams paramsValue)
		{
			if (_sourceValueExpressions != null)
				for (int i = 0; i < Math.Min(_sourceValueExpressions.Count, targetColumns.Length); i++)
				{
					var targetField = link.DataSet[targetColumns[i].Trim()];
					var source = (ValueExpression)_sourceValueExpressions[i];
					switch (source.Token)
					{
						case TokenType.Boolean : targetField.AsBoolean = (bool)source.Value; break;
						case TokenType.Decimal : targetField.AsDecimal = (decimal)source.Value; break;
						case TokenType.Integer : targetField.AsInt32 = (int)source.Value; break;
						case TokenType.Money : targetField.AsDecimal = (decimal)source.Value; break;
						case TokenType.Nil : targetField.ClearValue(); break;
						case TokenType.String : targetField.AsString = (string)source.Value; break;
					}
				}
		}
	}

	/// <summary> Defaults data from given parameters. </summary>
	public class DataParamDefault : DataDefault
	{
		// SourceParams

		private string _sourceParams = String.Empty;
		[DefaultValue("")]
		[Description("Optional comma or semicolon delimited list of Params in the Source source that are to be used to default from.  If left empty, all given parameters are used.")]
		public string SourceParams
		{
			get { return _sourceParams; }
			set { _sourceParams = value; }
		}

		protected override void InternalPerformDefault(DataLink link, string[] targetColumns, EventParams paramsValue)
		{
			if (paramsValue != null)
			{
				// Determine the list of source parameters to use
				string[] sourceNames;
				if (String.IsNullOrEmpty(_sourceParams))
				{
					sourceNames = new string[paramsValue.Count];
					var i = 0;
					foreach (KeyValuePair<string, object> entry in paramsValue)
					{
						sourceNames[i] = entry.Key;
						i++;
					}
				}
				else
					sourceNames = _sourceParams.Split(',', ';');
				
				// Copy the parameter values
				for (int i = 0; i < Math.Min(sourceNames.Length, targetColumns.Length); i++)
				{
					var sourceValue = paramsValue[sourceNames[i].Trim()];
					var targetField = link.DataSet[targetColumns[i].Trim()];
					if (sourceValue != null)
						targetField.AsNative = sourceValue;
					else
						targetField.ClearValue();
				}
			}
		}
	}


	public class FormAction : Action, IFormAction
	{
		// Behavior

		private CloseBehavior _behavior = CloseBehavior.RejectOrClose;
		[DefaultValue(CloseBehavior.RejectOrClose)]
		[Description("The behavior of the FormAction.")]
		public CloseBehavior Behavior
		{
			get { return _behavior; }
			set { _behavior = value; }
		}

		// Action

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			((IFormInterface)FindParent(typeof(IFormInterface))).Close(_behavior);
		}
	}

	public class SetNextRequestAction : Action, ISetNextRequestAction
	{
		// Document

		private string _document = String.Empty;
		[DefaultValue("")]
		[Description("The Document of the next user interface document to be loaded.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return _document; }
			set { _document = value; }
		}

		// Action

		/// <remarks> Sets the next request property of the hostnode.</remarks>
		/// <seealso cref="IHost.NextRequest"/>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			Request request = HostNode.NextRequest;
			if (request == null)
				HostNode.NextRequest = new Request(Document);
			else
				request.Document = Document;
		}

	}

	public class ClearNextRequestAction : Action, IClearNextRequestAction
	{
		// Action

		/// <remarks> Clears the next request property of the hostnode. </remarks>
        /// <seealso cref="IHost.NextRequest"/>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			HostNode.NextRequest = null;
		}
	}

	public class FormShownEvent : NodeEvent { }
}