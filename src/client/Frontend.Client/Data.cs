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
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client
{
	[DesignerImage("Image('Frontend', 'Nodes.Source')")]
	[DesignerCategory("Non Visual")]
	public class Source : Node, ISource
	{
		/// <summary>  Creates DataSource and DataLink objects and hooks the event handlers of the DataLink. </summary>
		public Source() : base()
		{
			_cursorType = DAE.CursorType.Dynamic;
			_requestedIsolation = DAE.CursorIsolation.Browse;
			_requestedCapabilities = DAE.CursorCapability.Navigable | DAE.CursorCapability.BackwardsNavigable | DAE.CursorCapability.Bookmarkable | DAE.CursorCapability.Searchable | DAE.CursorCapability.Updateable;
			_dataSource = new DataSource();
			try
			{
				_dataLink = new DataLink();
				try
				{
					_dataLink.Source = _dataSource;
					_dataLink.OnActiveChanged += new DataLinkHandler(ActiveChangedAction);
					_dataLink.OnStateChanged += new DataLinkHandler(StateChangedAction);
					_dataLink.OnDataChanged += new DataLinkHandler(DataChangedAction);
					_dataLink.OnRowChanging += new DataLinkFieldHandler(RowChangingAction);
					_dataLink.OnRowChanged += new DataLinkFieldHandler(RowChangedAction);
				}
				catch
				{
					_dataLink.Dispose();
					_dataLink = null;
					throw;
				}
			}
			catch
			{
				_dataSource.Dispose();
				_dataSource = null;
				throw;
			}
		}

		/// <remarks> Removes the DataSource and DataLink objects and unhooks the event handlers of the DataLink. </remarks>
		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);
				Surrogate = null;
				Master = null;
				OnChange = null;
				OnActiveChange = null;
				OnStateChange = null;
				OnRowChange = null;
				OnValidate = null;
				OnDefault = null;
				BeforeOpen = null;
				AfterOpen = null;
				BeforeClose = null;
				AfterClose = null;
				BeforeInsert = null;
				AfterInsert = null;
				BeforeEdit = null;
				AfterEdit = null;
				BeforeDelete = null;
				AfterDelete = null;
				BeforePost = null;
				AfterPost = null;
				BeforeCancel = null;
				AfterCancel = null;
			}
			finally
			{
				try
				{
					if (_dataLink != null)
					{
						_dataLink.OnActiveChanged -= new DataLinkHandler(ActiveChangedAction);
						_dataLink.OnStateChanged -= new DataLinkHandler(StateChangedAction);
						_dataLink.OnDataChanged -= new DataLinkHandler(DataChangedAction);
						_dataLink.OnRowChanging -= new DataLinkFieldHandler(RowChangingAction);
						_dataLink.OnRowChanged -= new DataLinkFieldHandler(RowChangedAction);
						_dataLink.Source = null;
						_dataLink.Dispose();
						_dataLink = null;
					}
				}
				finally
				{
					if (_dataSource != null)
					{
						_dataSource.Dispose();
						_dataSource = null;
					}
				}
			}
		}

		public event DataLinkHandler ActiveChanged
		{
			add { _dataLink.OnActiveChanged += value; }
			remove { _dataLink.OnActiveChanged -= value; }
		}
		
		public event DataLinkHandler StateChanged
		{
			add { _dataLink.OnStateChanged += value; }
			remove { _dataLink.OnStateChanged -= value; }
		}

		public event DataLinkHandler DataChanged
		{
			add { _dataLink.OnDataChanged += value; }
			remove { _dataLink.OnDataChanged -= value; }
		}
		
		public event DataLinkFieldHandler RowChanging
		{
			add { _dataLink.OnRowChanging += value; }
			remove { _dataLink.OnRowChanging -= value; }
		}
		
		public event DataLinkFieldHandler RowChanged
		{
			add { _dataLink.OnRowChanged += value; }
			remove { _dataLink.OnRowChanged -= value; }
		}
		
		public event DataLinkHandler Default
		{
			add { _dataLink.OnDefault += value; }
			remove { _dataLink.OnDefault -= value; }
		}
		
		// CursorType
		
		private DAE.CursorType _cursorType = DAE.CursorType.Dynamic;
		[DefaultValue(DAE.CursorType.Dynamic)]
		[Description("Determines the behavior of the cursor with respect to updates made after the cursor is opened.  If the cursor type is dynamic, updates made through the cursor will be visible.  If the cursor type is static, updates will not be visible.")]
		public DAE.CursorType CursorType
		{
			get { return _cursorType; }
			set 
			{ 
				if (_cursorType != value)
				{
					_cursorType = value;
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}
		
		// IsolationLevel

		private DAE.IsolationLevel _isolationLevel = DAE.IsolationLevel.Browse;
		[DefaultValue(DAE.IsolationLevel.Browse)]
		[Description("The isolation level for transactions performed by this view.")]
		public DAE.IsolationLevel IsolationLevel
		{
			get { return _isolationLevel; }
			set 
			{ 
				if (_isolationLevel != value)
				{
					_isolationLevel = value; 
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}
		
		// RequestedIsolation

		private DAE.CursorIsolation _requestedIsolation;
		[DefaultValue(DAE.CursorIsolation.Browse)]
		[Description("The requested relative isolation of the cursor.  This will be used in conjunction with the isolation level of the transaction to determine the actual isolation of the cursor.")]
		public DAE.CursorIsolation RequestedIsolation
		{
			get { return _requestedIsolation; }
			set 
			{ 
				if (_requestedIsolation != value)
				{
					_requestedIsolation = value; 
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}

		// RequestedCapabilities

		private DAE.CursorCapability _requestedCapabilities;
		[
			DefaultValue
			(
				DAE.CursorCapability.Navigable |
				DAE.CursorCapability.BackwardsNavigable |
				DAE.CursorCapability.Bookmarkable |
				DAE.CursorCapability.Searchable |
				DAE.CursorCapability.Updateable
			)
		]
		[Description("Determines the requested behavior of the cursor")]
		public DAE.CursorCapability RequestedCapabilities
		{
			get { return _requestedCapabilities; }
			set 
			{ 
				if (_requestedCapabilities != value)
				{
					_requestedCapabilities = value; 
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}

		// Expression

		private string _expression = String.Empty;
		[DefaultValue("")]
		[Description("The expression to be used to select the data set.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Expression
		{
			get { return _expression; }
			set
			{
				if (_expression != value)
				{
					_expression = ( value == null ? String.Empty : value );
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}
		
		// InsertStatement
		
		private string _insertStatement = String.Empty;
		[DefaultValue("")]
		[Description("A single statement of D4 to be used to override the default insert behavior of the source.  The new columns are accessible as parameters by their names, qualified by 'New.'.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string InsertStatement
		{
			get { return _insertStatement; }
			set
			{
				if (_insertStatement != value)
				{
					_insertStatement = value == null ? String.Empty : value;
					if (_dataView != null)
						_dataView.InsertStatement = _insertStatement;
				}
			}
		}
		
		// UpdateStatement

		private string _updateStatement = String.Empty;
		[DefaultValue("")]
		[Description("A single statement of D4 to be used to override the default update behavior of the source.  The new and old columns are accessible as parameters by their names, qualified by 'New.' and 'Old.'.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string UpdateStatement
		{
			get { return _updateStatement; }
			set
			{
				if (_updateStatement != value)
				{
					_updateStatement = value == null ? String.Empty : value;
					if (_dataView != null)
						_dataView.UpdateStatement = _updateStatement;
				}
			}
		}
		
		// DeleteStatement

		private string _deleteStatement = String.Empty;
		[DefaultValue("")]
		[Description("A single statement of D4 to be used to override the default delete behavior of the source.  The old columns are accessible as parameters by their names, qualified by 'Old.'.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string DeleteStatement
		{
			get { return _deleteStatement; }
			set
			{
				if (_deleteStatement != value)
				{
					_deleteStatement = value == null ? String.Empty : value;
					if (_dataView != null)
						_dataView.DeleteStatement = _deleteStatement;
				}
			}
		}
		
		// Filter

		private string _filter = String.Empty;
		[DefaultValue("")]
		[Description("The filter expression to apply to the data source.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Filter
		{
			get { return _filter; }
			set
			{
				if (_filter != value)
				{
					_filter = value == null ? String.Empty : value;
					if (_dataView != null) // don't call UpdateDataview because we want to ignore the filter if we are surrogate (just like we ignore the expression).
						_dataView.Filter = _filter;
				}
			}
		}

		// Enabled

		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("Represents the state of the data source.")]
		public bool Enabled
		{
			get { return _enabled; }
			set 
			{ 
				if (_enabled != value) 
				{
					_enabled = value; 
					if (Active)
						InternalUpdateView();
				}
			}
		}

		// OnChange

		protected IAction _onChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a different row in the dataset is selected.")]
		public IAction OnChange
		{
			get { return _onChange; }
			set
			{
				if (_onChange != value)
				{
					if (_onChange != null)
						_onChange.Disposed -= new EventHandler(ChangeActionDisposed);
					_onChange = value;
					if (_onChange != null)
						_onChange.Disposed += new EventHandler(ChangeActionDisposed);
				}
			}
		}
		
		protected void ChangeActionDisposed(object sender, EventArgs args)
		{
			OnChange = null;
		}

		protected void DataChangedAction(DataLink link, DataSet dataSet)
		{
			if (Active && (_onChange != null))
				_onChange.Execute(this, new EventParams());
		}
			
		// OnRowChanging

		protected IAction _onRowChanging;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a field in the current row in the dataset is changing.")]
		public IAction OnRowChanging
		{
			get { return _onRowChanging; }
			set
			{
				if (_onRowChanging != value)
				{
					if (_onRowChanging != null)
						_onRowChanging.Disposed -= new EventHandler(RowChangingActionDisposed);
					_onRowChanging = value;
					if (_onRowChanging != null)
						_onRowChanging.Disposed += new EventHandler(RowChangingActionDisposed);
				}
			}
		}
		
		protected void RowChangingActionDisposed(object sender, EventArgs args)
		{
			OnRowChanging = null;
		}

		protected void RowChangingAction(DataLink link, DataSet dataSet, DataField field)
		{
			if (Active && (_onRowChanging != null))
				_onRowChanging.Execute(this, new EventParams("AField", field));
		}
			
		// OnRowChange

		protected IAction _onRowChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a field in the current row in the dataset is changed.")]
		public IAction OnRowChange
		{
			get { return _onRowChange; }
			set
			{
				if (_onRowChange != value)
				{
					if (_onRowChange != null)
						_onRowChange.Disposed -= new EventHandler(RowChangeActionDisposed);
					_onRowChange = value;
					if (_onRowChange != null)
						_onRowChange.Disposed += new EventHandler(RowChangeActionDisposed);
				}
			}
		}
		
		protected void RowChangeActionDisposed(object sender, EventArgs args)
		{
			OnRowChange = null;
		}

		protected void RowChangedAction(DataLink link, DataSet dataSet, DataField field)
		{
			if (Active && (_onRowChange != null))
				_onRowChange.Execute(this, new EventParams("AField", field));
		}
			
		// OnActiveChange

		protected IAction _onActiveChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when the dataset's active property changes.")]
		public IAction OnActiveChange
		{
			get { return _onActiveChange; }
			set
			{
				if (_onActiveChange != value)
				{
					if (_onActiveChange != null)
						_onActiveChange.Disposed += new EventHandler(ActiveChangedActionDisposed);
					_onActiveChange = value;
					if (_onActiveChange != null)
						_onActiveChange.Disposed -= new EventHandler(ActiveChangedActionDisposed);
				}
			}
		}
		
		protected void ActiveChangedActionDisposed(object sender, EventArgs args)
		{
			OnActiveChange = null;
		}

		protected void ActiveChangedAction(DataLink link, DataSet dataSet)
		{
			if (Active && (_onActiveChange != null))
				_onActiveChange.Execute(this, new EventParams());
		}
			
		// OnStateChange

		protected IAction _onStateChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a the dataset's state changes.")]
		public IAction OnStateChange
		{
			get { return _onStateChange; }
			set
			{
				if (_onStateChange != value)
				{
					if (_onStateChange != null)
						_onStateChange.Disposed -= new EventHandler(StateChangedActionDisposed);
					_onStateChange = value;
					if (_onStateChange != null)
						_onStateChange.Disposed += new EventHandler(StateChangedActionDisposed);
				}
			}
		}
		
		protected void StateChangedActionDisposed(object sender, EventArgs args)
		{
			OnStateChange = null;
		}

		protected void StateChangedAction(DataLink link, DataSet dataSet)
		{
			if (Active && (_onStateChange != null))
				_onStateChange.Execute(this, new EventParams());
		}
			
		// OnDefault

		protected IAction _onDefault;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed to allow for setting of default values for a new row. Values set during this action will not set the modified of the source.")]
		public IAction OnDefault
		{
			get { return _onDefault; }
			set
			{
				if (_onDefault != value)
				{
					if (_onDefault != null)
						_onDefault.Disposed -= new EventHandler(DefaultActionDisposed);
					_onDefault = value;
					if (_onDefault != null)
						_onDefault.Disposed += new EventHandler(DefaultActionDisposed);
				}
			}
		}
		
		protected void DefaultActionDisposed(object sender, EventArgs args)
		{
			OnDefault = null;
		}

		protected void DefaultAction(object sender, EventArgs args)
		{
			if (Active && (_onDefault != null))
				_onDefault.Execute(this, new EventParams());
		}
			
		// OnValidate

		protected IAction _onValidate;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set posts the current row.  An exception here will prevent the dataset from posting.")]
		public IAction OnValidate
		{
			get { return _onValidate; }
			set
			{
				if (_onValidate != value)
				{
					if (_onValidate != null)
						_onValidate.Disposed -= new EventHandler(ValidatedActionDisposed);
					_onValidate = value;
					if (_onValidate != null)
						_onValidate.Disposed += new EventHandler(ValidatedActionDisposed);
				}
			}
		}
		
		protected void ValidatedActionDisposed(object sender, EventArgs args)
		{
			OnValidate = null;
		}

		protected void ValidatedAction(object sender, EventArgs args)
		{
			if (Active && (_onValidate != null))
				_onValidate.Execute(this, new EventParams());
		}
			
		// BeforeOpen

		protected IAction _beforeOpen;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set opens.")]
		public IAction BeforeOpen
		{
			get { return _beforeOpen; }
			set
			{
				if (_beforeOpen != value)
				{
					if (_beforeOpen != null)
						_beforeOpen.Disposed -= new EventHandler(BeforeOpenActionDisposed);
					_beforeOpen = value;
					if (_beforeOpen != null)
						_beforeOpen.Disposed += new EventHandler(BeforeOpenActionDisposed);
				}
			}
		}
		
		protected void BeforeOpenActionDisposed(object sender, EventArgs args)
		{
			BeforeOpen = null;
		}

		protected void BeforeOpenAction(object sender, EventArgs args)
		{
			if (Active && (_beforeOpen != null))
				_beforeOpen.Execute(this, new EventParams());
		}
			
		// AfterOpen

		protected IAction _afterOpen;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set opens.")]
		public IAction AfterOpen
		{
			get { return _afterOpen; }
			set
			{
				if (_afterOpen != value)
				{
					if (_afterOpen != null)
						_afterOpen.Disposed -= new EventHandler(AfterOpenActionDisposed);
					_afterOpen = value;
					if (_afterOpen != null)
						_afterOpen.Disposed += new EventHandler(AfterOpenActionDisposed);
				}
			}
		}
		
		protected void AfterOpenActionDisposed(object sender, EventArgs args)
		{
			AfterOpen = null;
		}

		protected void AfterOpenAction(object sender, EventArgs args)
		{
			if (Active && (_afterOpen != null))
				_afterOpen.Execute(this, new EventParams());
		}
			
		// BeforeClose

		protected IAction _beforeClose;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set closes.")]
		public IAction BeforeClose
		{
			get { return _beforeClose; }
			set
			{
				if (_beforeClose != value)
				{
					if (_beforeClose != null)
						_beforeClose.Disposed -= new EventHandler(BeforeCloseActionDisposed);
					_beforeClose = value;
					if (_beforeClose != null)
						_beforeClose.Disposed += new EventHandler(BeforeCloseActionDisposed);
				}
			}
		}
		
		protected void BeforeCloseActionDisposed(object sender, EventArgs args)
		{
			BeforeClose = null;
		}

		protected void BeforeCloseAction(object sender, EventArgs args)
		{
			if (Active && (_beforeClose != null))
				_beforeClose.Execute(this, new EventParams());
		}
			
		// AfterClose

		protected IAction _afterClose;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set closes.")]
		public IAction AfterClose
		{
			get { return _afterClose; }
			set
			{
				if (_afterClose != value)
				{
					if (_afterClose != null)
						_afterClose.Disposed -= new EventHandler(AfterCloseActionDisposed);
					_afterClose = value;
					if (_afterClose != null)
						_afterClose.Disposed += new EventHandler(AfterCloseActionDisposed);
				}
			}
		}
		
		protected void AfterCloseActionDisposed(object sender, EventArgs args)
		{
			AfterClose = null;
		}

		protected void AfterCloseAction(object sender, EventArgs args)
		{
			if (Active && (_afterClose != null))
				_afterClose.Execute(this, new EventParams());
		}
			
		// BeforeInsert

		protected IAction _beforeInsert;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set enters insert state.")]
		public IAction BeforeInsert
		{
			get { return _beforeInsert; }
			set
			{
				if (_beforeInsert != value)
				{
					if (_beforeInsert != null)
						_beforeInsert.Disposed -= new EventHandler(BeforeInsertActionDisposed);
					_beforeInsert = value;
					if (_beforeInsert != null)
						_beforeInsert.Disposed += new EventHandler(BeforeInsertActionDisposed);
				}
			}
		}
		
		protected void BeforeInsertActionDisposed(object sender, EventArgs args)
		{
			BeforeInsert = null;
		}

		protected void BeforeInsertAction(object sender, EventArgs args)
		{
			if (Active && (_beforeInsert != null))
				_beforeInsert.Execute(this, new EventParams());
		}
			
		// AfterInsert

		protected IAction _afterInsert;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set enters insert state.")]
		public IAction AfterInsert
		{
			get { return _afterInsert; }
			set
			{
				if (_afterInsert != value)
				{
					if (_afterInsert != null)
						_afterInsert.Disposed -= new EventHandler(AfterInsertActionDisposed);
					_afterInsert = value;
					if (_afterInsert != null)
						_afterInsert.Disposed += new EventHandler(AfterInsertActionDisposed);
				}
			}
		}
		
		protected void AfterInsertActionDisposed(object sender, EventArgs args)
		{
			AfterInsert = null;
		}

		protected void AfterInsertAction(object sender, EventArgs args)
		{
			if (Active && (_afterInsert != null))
				_afterInsert.Execute(this, new EventParams());
		}
			
		// BeforeEdit

		protected IAction _beforeEdit;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set enters edit state.")]
		public IAction BeforeEdit
		{
			get { return _beforeEdit; }
			set
			{
				if (_beforeEdit != value)
				{
					if (_beforeEdit != null)
						_beforeEdit.Disposed -= new EventHandler(BeforeEditActionDisposed);
					_beforeEdit = value;
					if (_beforeEdit != null)
						_beforeEdit.Disposed += new EventHandler(BeforeEditActionDisposed);
				}
			}
		}
		
		protected void BeforeEditActionDisposed(object sender, EventArgs args)
		{
			BeforeEdit = null;
		}

		protected void BeforeEditAction(object sender, EventArgs args)
		{
			if (Active && (_beforeEdit != null))
				_beforeEdit.Execute(this, new EventParams());
		}
			
		// AfterEdit

		protected IAction _afterEdit;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set enters edit state.")]
		public IAction AfterEdit
		{
			get { return _afterEdit; }
			set
			{
				if (_afterEdit != value)
				{
					if (_afterEdit != null)
						_afterEdit.Disposed -= new EventHandler(AfterEditActionDisposed);
					_afterEdit = value;
					if (_afterEdit != null)
						_afterEdit.Disposed += new EventHandler(AfterEditActionDisposed);
				}
			}
		}
		
		protected void AfterEditActionDisposed(object sender, EventArgs args)
		{
			AfterEdit = null;
		}

		protected void AfterEditAction(object sender, EventArgs args)
		{
			if (Active && (_afterEdit != null))
				_afterEdit.Execute(this, new EventParams());
		}
			
		// BeforeDelete

		protected IAction _beforeDelete;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before a row in the data set is deleted.")]
		public IAction BeforeDelete
		{
			get { return _beforeDelete; }
			set
			{
				if (_beforeDelete != value)
				{
					if (_beforeDelete != null)
						_beforeDelete.Disposed -= new EventHandler(BeforeDeleteActionDisposed);
					_beforeDelete = value;
					if (_beforeDelete != null)
						_beforeDelete.Disposed += new EventHandler(BeforeDeleteActionDisposed);
				}
			}
		}
		
		protected void BeforeDeleteActionDisposed(object sender, EventArgs args)
		{
			BeforeDelete = null;
		}

		protected void BeforeDeleteAction(object sender, EventArgs args)
		{
			if (Active && (_beforeDelete != null))
				_beforeDelete.Execute(this, new EventParams());
		}
			
		// AfterDelete

		protected IAction _afterDelete;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after a row in the data set is deleted.")]
		public IAction AfterDelete
		{
			get { return _afterDelete; }
			set
			{
				if (_afterDelete != value)
				{
					if (_afterDelete != null)
						_afterDelete.Disposed -= new EventHandler(AfterDeleteActionDisposed);
					_afterDelete = value;
					if (_afterDelete != null)
						_afterDelete.Disposed += new EventHandler(AfterDeleteActionDisposed);
				}
			}
		}
		
		protected void AfterDeleteActionDisposed(object sender, EventArgs args)
		{
			AfterDelete = null;
		}

		protected void AfterDeleteAction(object sender, EventArgs args)
		{
			if (Active && (_afterDelete != null))
				_afterDelete.Execute(this, new EventParams());
		}
			
		// BeforePost

		protected IAction _beforePost;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set is posted.")]
		public IAction BeforePost
		{
			get { return _beforePost; }
			set
			{
				if (_beforePost != value)
				{
					if (_beforePost != null)
						_beforePost.Disposed -= new EventHandler(BeforePostActionDisposed);
					_beforePost = value;
					if (_beforePost != null)
						_beforePost.Disposed += new EventHandler(BeforePostActionDisposed);
				}
			}
		}
		
		protected void BeforePostActionDisposed(object sender, EventArgs args)
		{
			BeforePost = null;
		}

		protected void BeforePostAction(object sender, EventArgs args)
		{
			if (Active && (_beforePost != null))
				_beforePost.Execute(this, new EventParams());
		}
			
		// AfterPost

		protected IAction _afterPost;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set is posted.")]
		public IAction AfterPost
		{
			get { return _afterPost; }
			set
			{
				if (_afterPost != value)
				{
					if (_afterPost != null)
						_afterPost.Disposed -= new EventHandler(AfterPostActionDisposed);
					_afterPost = value;
					if (_afterPost != null)
						_afterPost.Disposed += new EventHandler(AfterPostActionDisposed);
				}
			}
		}
		
		protected void AfterPostActionDisposed(object sender, EventArgs args)
		{
			AfterPost = null;
		}

		protected void AfterPostAction(object sender, EventArgs args)
		{
			if (Active && (_afterPost != null))
				_afterPost.Execute(this, new EventParams());
		}
			
		// BeforeCancel

		protected IAction _beforeCancel;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set is canceled.")]
		public IAction BeforeCancel
		{
			get { return _beforeCancel; }
			set
			{
				if (_beforeCancel != value)
				{
					if (_beforeCancel != null)
						_beforeCancel.Disposed -= new EventHandler(BeforeCancelActionDisposed);
					_beforeCancel = value;
					if (_beforeCancel != null)
						_beforeCancel.Disposed += new EventHandler(BeforeCancelActionDisposed);
				}
			}
		}
		
		protected void BeforeCancelActionDisposed(object sender, EventArgs args)
		{
			BeforeCancel = null;
		}

		protected void BeforeCancelAction(object sender, EventArgs args)
		{
			if (Active && (_beforeCancel != null))
				_beforeCancel.Execute(this, new EventParams());
		}
			
		// AfterCancel

		protected IAction _afterCancel;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set is canceled.")]
		public IAction AfterCancel
		{
			get { return _afterCancel; }
			set
			{
				if (_afterCancel != value)
				{
					if (_afterCancel != null)
						_afterCancel.Disposed -= new EventHandler(AfterCancelActionDisposed);
					_afterCancel = value;
					if (_afterCancel != null)
						_afterCancel.Disposed += new EventHandler(AfterCancelActionDisposed);
				}
			}
		}
		
		protected void AfterCancelActionDisposed(object sender, EventArgs args)
		{
			AfterCancel = null;
		}

		protected void AfterCancelAction(object sender, EventArgs args)
		{
			if (Active && (_afterCancel != null))
				_afterCancel.Execute(this, new EventParams());
		}
			
		// DataLink

		private DataLink _dataLink;

		// DataSource

		private DataSource _dataSource;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DataSource DataSource
		{
			get { return _dataSource; }
		}

		// DataView

		private DataView _dataView;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DataView DataView
		{
			get { return (DataView)_dataSource.DataSet; }
		}

		private void ClearDataView()
		{
			try
			{
				if ((_dataView != null) && (_surrogate == null))
				{
					//try
					//{
						_dataView.OnErrors -= new ErrorsOccurredHandler(ErrorsOccurred);
						_dataView.OnValidate -= new EventHandler(ValidatedAction);
						_dataView.OnDefault -= new EventHandler(DefaultAction);
						_dataView.BeforeOpen -= new EventHandler(BeforeOpenAction);
						_dataView.AfterOpen -= new EventHandler(AfterOpenAction);
						_dataView.BeforeClose -= new EventHandler(BeforeCloseAction);
						_dataView.AfterClose -= new EventHandler(AfterCloseAction);
						_dataView.BeforeInsert -= new EventHandler(BeforeInsertAction);
						_dataView.AfterInsert -= new EventHandler(AfterInsertAction);
						_dataView.BeforeEdit -= new EventHandler(BeforeEditAction);
						_dataView.AfterEdit -= new EventHandler(AfterEditAction);
						_dataView.BeforeDelete -= new EventHandler(BeforeDeleteAction);
						_dataView.AfterDelete -= new EventHandler(AfterDeleteAction);
						_dataView.BeforePost -= new EventHandler(BeforePostAction);
						_dataView.AfterPost -= new EventHandler(AfterPostAction);
						_dataView.BeforeCancel -= new EventHandler(BeforeCancelAction);
						_dataView.AfterCancel -= new EventHandler(AfterCancelAction);
						_dataView.Dispose();
						_dataView = null;
					//}  
					//finally
					//{
					//	ExecuteEndScript();
					//}
				}
			}
			finally
			{
				if (DataSource != null)
					DataSource.DataSet = null;
			}
		}
		
		protected virtual DataView CreateDataView()
		{
			DataView result = new DataView();
			try
			{
				result.Session = HostNode.Session.DataSession;
				result.BeginScript = _beginScript;
				result.EndScript = _endScript;
				result.UseBrowse = _useBrowse;
				result.UseApplicationTransactions = _useApplicationTransactions;
				result.ShouldEnlist = _shouldEnlist;
				result.CursorType = _cursorType;
				result.IsolationLevel = _isolationLevel;
				result.RequestedIsolation = _requestedIsolation;
				result.RequestedCapabilities = _requestedCapabilities;
				result.IsReadOnly = _isReadOnly;
				result.IsWriteOnly = _isWriteOnly;
				result.RefreshAfterPost = _refreshAfterPost;
				result.Expression = _expression;
				result.InsertStatement = _insertStatement;
				result.UpdateStatement = _updateStatement;
				result.DeleteStatement = _deleteStatement;
				result.Filter = _filter;
				result.OnErrors += new ErrorsOccurredHandler(ErrorsOccurred);
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
		
		public event EventHandler OnUpdateView;

		protected virtual void InternalUpdateView()
		{
			ClearDataView();

			if (Enabled)
			{
				if ((_surrogate != null) && _surrogate.Enabled)
					DataSource.DataSet = _surrogate.DataView;
				else
				{
					_dataView = CreateDataView();
					_dataView.OnValidate += new EventHandler(ValidatedAction);
					_dataView.OnDefault += new EventHandler(DefaultAction);
					_dataView.BeforeOpen += new EventHandler(BeforeOpenAction);
					_dataView.AfterOpen += new EventHandler(AfterOpenAction);
					_dataView.BeforeClose += new EventHandler(BeforeCloseAction);
					_dataView.AfterClose += new EventHandler(AfterCloseAction);
					_dataView.BeforeInsert += new EventHandler(BeforeInsertAction);
					_dataView.AfterInsert += new EventHandler(AfterInsertAction);
					_dataView.BeforeEdit += new EventHandler(BeforeEditAction);
					_dataView.AfterEdit += new EventHandler(AfterEditAction);
					_dataView.BeforeDelete += new EventHandler(BeforeDeleteAction);
					_dataView.AfterDelete += new EventHandler(AfterDeleteAction);
					_dataView.BeforePost += new EventHandler(BeforePostAction);
					_dataView.AfterPost += new EventHandler(AfterPostAction);
					_dataView.BeforeCancel += new EventHandler(BeforeCancelAction);
					_dataView.AfterCancel += new EventHandler(AfterCancelAction);
					DataSource.DataSet = _dataView;
					InternalUpdateMaster();
					InternalUpdateParams();
					try 
					{
						_dataView.Open(_openState);
					}
					catch
					{
						try
						{
							Enabled = false;
						}
						catch
						{
							// Ignore closing errors here
						}

						throw;
					}
				}
			}

			// call onupdateview
			if (OnUpdateView != null)
				OnUpdateView(this, null);
		}

		// Error Handling
		private void ErrorsOccurred(DataSet dataSet, CompilerMessages messages)
		{
			ErrorList AErrors = new ErrorList();
			AErrors.AddRange(messages);
			HostNode.Session.ReportErrors(HostNode, AErrors);
		}

		private bool _useBrowse = true;
		[DefaultValue(true)]
		[Description("Indicates whether the view will use a browse clause by default to request data.")]
		public bool UseBrowse
		{
			get { return _useBrowse; }
			set 
			{ 
				if (_useBrowse != value)
				{
					_useBrowse = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool _useApplicationTransactions = true;
		[DefaultValue(true)]
		[Description("Indicates whether the view will use application transactions to manipulate data.")]
		public bool UseApplicationTransactions
		{
			get { return _useApplicationTransactions; }
			set
			{
				if (_useApplicationTransactions != value)
				{
					_useApplicationTransactions = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private EnlistMode _shouldEnlist = EnlistMode.Default;
		[DefaultValue(EnlistMode.Default)]
		[Description("Indicates whether the source should enlist in the application transaction of its master source.")]
		public EnlistMode ShouldEnlist
		{
			get { return _shouldEnlist; }
			set
			{
				if (_shouldEnlist != value)
				{
					_shouldEnlist = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool _isReadOnly = false;
		[DefaultValue(false)]
		[Description("Indicates whether the data in the view will be updateable.")]
		public bool IsReadOnly
		{
			get { return _isReadOnly; }
			set
			{
				if (_isReadOnly != value)
				{
					_isReadOnly = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool _isWriteOnly = false;
		[DefaultValue(false)]
		[Description("Indicates whether the view will be used solely for inserting data.")]
		public bool IsWriteOnly
		{
			get { return _isWriteOnly; }
			set
			{
				if (_isWriteOnly != value)
				{
					_isWriteOnly = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private DataSetState _openState = DataSetState.Browse;
		[DefaultValue(DataSetState.Browse)]
		[Description("Indicates the open state for the view.")]
		public DataSetState OpenState
		{
			get { return _openState; }
			set
			{
				if (_openState != value)
				{
					_openState = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool _refreshAfterPost = false;
		[DefaultValue(false)]
		[Description("Indicates whether or not the view will be refreshed after a call to post.")]
		public bool RefreshAfterPost
		{
			get { return _refreshAfterPost; }
			set
			{
				if (_refreshAfterPost != value)
				{
					_refreshAfterPost = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		// BeginScript

		private string _beginScript = String.Empty;
		[DefaultValue("")]
		[Description("A script that will be executed when the source is activated (before opening the expression).")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string BeginScript
		{
			get { return _beginScript; }
			set { _beginScript = value == null ? String.Empty : value; }
		}

		// EndScript

		private string _endScript = String.Empty;
		[DefaultValue("")]
		[Description("A script that will be executed when the source is deactivated.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string EndScript
		{
			get { return _endScript; }
			set { _endScript = value == null ? String.Empty : value; }
		}

		// Surrogate

		private ISource _surrogate;
		/// <remarks> Links and unlinks when Activate and Deactivate is called. </remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public ISource Surrogate
		{
			get { return _surrogate; }
			set
			{
				if (_surrogate != value)
				{
					if (_surrogate != null) 
					{
						_surrogate.Disposed -= new EventHandler(SurrogateDisposed);
						_surrogate.OnUpdateView += new EventHandler(SurrogateUpdateView);
					}
					_surrogate = value;
					if (_surrogate != null)
					{
						_surrogate.Disposed += new EventHandler(SurrogateDisposed);
						_surrogate.OnUpdateView += new EventHandler(SurrogateUpdateView);
					}
					ClearDataView();
					if (Active)
						InternalUpdateView();
				}
			}
		}

		private void SurrogateDisposed(object sender, EventArgs args)
		{
			Surrogate = null;
		}

		private void SurrogateUpdateView(object sender, EventArgs args)
		{
			if (Active)
				InternalUpdateView();
		}

		// Master/Detail

		private string _masterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The key column(s) in the master source to use for the master-detail relationship.")]
		public string MasterKeyNames
		{
			get { return _masterKeyNames; }
			set
			{
				if (_masterKeyNames != value)
				{
					_masterKeyNames = value;
					UpdateMaster();
				}
			}
		}

		private string _detailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The key column(s) in this source to use as the detail key for a master-detail relationship.")]
		public string DetailKeyNames
		{
			get { return _detailKeyNames; }
			set
			{
				if (_detailKeyNames != value)
				{
					_detailKeyNames = value;
					UpdateMaster();
				}
			}
		}

		private ISource _master;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("When set, this source will be filtered and will re-query as the master source navigates.")]
		public ISource Master
		{
			get { return _master; }
			set
			{
				if (_master != value)
				{
					if (_master != null)
						_master.Disposed -= new EventHandler(MasterDisposed);
					_master = value;
					if (_master != null)
						_master.Disposed += new EventHandler(MasterDisposed);
					UpdateMaster();
				}
			}
		}
		
		private void MasterDisposed(object sender, EventArgs args)
		{
			Master = null;
		}

		private void InternalUpdateMaster()
		{
			if (_dataView != null)
			{
				if (Master != null)
				{
					_dataView.MasterKeyNames = MasterKeyNames;
					_dataView.DetailKeyNames = DetailKeyNames;
					_dataView.MasterSource = Master.DataSource;
					_dataView.WriteWhereClause = WriteWhereClause;
				}
				else
					_dataView.MasterSource = null;
			}
		}

		/// <summary> Called when the master source property is changed. </summary>
		private void UpdateMaster()
		{
			if (Active)
				InternalUpdateMaster();
		}
		
		private bool _writeWhereClause = true;
		[DefaultValue(true)]
		[Description("Determines whether the data view will automatically produce the where clause necessary to limit the result set by the master source.  If false, the expression must contain the necessary restrictions.  Current master data view values are available as AMaster<detail column name (with qualifiers replaced with underscores)> parameters within the expression.")]
		public bool WriteWhereClause
		{
			get { return _writeWhereClause; }
			set
			{
				if (_writeWhereClause != value)
				{
					_writeWhereClause = value;
					UpdateMaster();
				}
			}
		}
		
		// Parameters
		
		private DataParams _params;
		public DataParams Params
		{
			get { return _params; }
			set 
			{ 
				_params = value; 
				UpdateParams();
			}
		}
		
		private DataSetParamGroup _paramsParamGroup;
		
		private List<DataSetParamGroup> _argumentParamGroups = new List<DataSetParamGroup>();

		private void InternalUpdateParams()
		{
			if (_dataView != null)
			{
				// Remove the old argument param groups
				foreach (var group in _argumentParamGroups)
				{
					if (_dataView.ParamGroups.Contains(group))
						_dataView.ParamGroups.Remove(group);
				}
				_argumentParamGroups.Clear();
				
				// Add new argument param groups
				BaseArgument.CollectDataSetParamGroup(this, _argumentParamGroups);
				foreach (var group in _argumentParamGroups)
					_dataView.ParamGroups.Add(group);
				
				if (_params != null)
				{
					if (_paramsParamGroup != null)
					{
						DataView.ParamGroups.SafeRemove(_paramsParamGroup);
						_paramsParamGroup = null;
					}
					
					DataSetParamGroup paramGroup = new DataSetParamGroup();
					foreach (DataParam param in _params)
					{
						DataSetParam dataSetParam = new DataSetParam();
						dataSetParam.Name = param.Name;
						dataSetParam.Modifier = param.Modifier;
						dataSetParam.DataType = param.DataType;
						dataSetParam.Value = param.Value;
						paramGroup.Params.Add(dataSetParam);
					}

					_dataView.ParamGroups.Add(paramGroup);
					_paramsParamGroup = paramGroup;
				}
			}
		}
		
		private void UpdateParams()
		{
			if (Active)
				InternalUpdateParams();
		}
		
		// DataView Access
		
		protected void CheckEnabled()
		{
			if (DataView == null)
				throw new ClientException(ClientException.Codes.SourceNotEnabled);
		}
		
		[Browsable(false)]
		public Schema.TableVar TableVar 
		{ 
			get 
			{ 
				CheckEnabled(); 
				return DataView.TableVar; 
			} 
		}

		[Browsable(false)]
		[Publish(PublishMethod.None)]		
		public Schema.Order Order 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView.Order; 
			} 
			set 
			{ 
				CheckEnabled();
				DataView.Order = value; 
			} 
		}
		
		[Browsable(false)]
		[Publish(PublishMethod.None)]		
		public string OrderString 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView.OrderString; 
			} 
			set 
			{ 
				CheckEnabled();
				DataView.OrderString = value; 
			} 
		}
		
		[Browsable(false)]
		public IServerProcess Process 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView.Process; 
			} 
		}
		
		[Browsable(false)]
		public DataSetState State { get { return DataView == null ? DataSetState.Inactive : DataView.State; } }
		
		[Browsable(false)]
		public DataField this[string columnName] 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView[columnName]; 
			} 
		}
		
		[Browsable(false)]
		public DataField this[int columnIndex] 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView[columnIndex]; 
			} 
		}
		
		[Browsable(false)]
		public bool BOF
		{
			get
			{
				CheckEnabled();
				return DataView.BOF;
			}
		}
		
		[Browsable(false)]
		public bool EOF
		{
			get
			{
				CheckEnabled();
				return DataView.EOF;
			}
		}
		
		[Browsable(false)]
		public bool IsEmpty
		{
			get
			{
				CheckEnabled();
				return DataView.IsEmpty();
			}
		}
		
		public void First()
		{
			CheckEnabled();
			DataView.First();
		}
		
		public void Prior()
		{
			CheckEnabled();
			DataView.Prior();
		}
		
		public void Next()
		{
			CheckEnabled();
			DataView.Next();
		}
		
		public void Last()
		{
			CheckEnabled();
			DataView.Last();
		}
		
		public DAE.Runtime.Data.IRow GetKey()
		{
			CheckEnabled();
			return DataView.GetKey();
		}
		
		public bool FindKey(DAE.Runtime.Data.IRow key)
		{
			CheckEnabled();
			return DataView.FindKey(key);
		}
		
		public void FindNearest(DAE.Runtime.Data.IRow key)
		{
			CheckEnabled();
			DataView.FindNearest(key);
		}
		
		public void Refresh()
		{
			CheckEnabled();
			DataView.Refresh();
		}
		
		[Browsable(false)]
		public bool IsModified
		{
			get 
			{
				CheckEnabled();
				return DataView.IsModified;
			}
		}
		
		public void Insert()
		{
			CheckEnabled();
			DataView.Insert();
		}
		
		public void Edit()
		{
			CheckEnabled();
			DataView.Edit();
		}
		
		public void Post()
		{
			CheckEnabled();
			DataView.Post();
		}

		public void PostDetails()
		{
			CheckEnabled();
			DataView.PostDetails();
		}
		
		public void Cancel()
		{
			CheckEnabled();
			DataView.Cancel();
		}
		
		public void Delete()
		{
			CheckEnabled();
			DataView.Delete();
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(ISourceChild).IsAssignableFrom(childType) || typeof(IBaseArgument).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}
		
		protected internal override void ChildrenChanged()
		{
			UpdateParams();
		}

		protected override void Activate()
		{
			if (_expression == String.Empty) 
				Enabled = false;
			try
			{
				base.Activate();
			}
			catch
			{
				ClearDataView();
				throw;
			}
		}
		
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				ClearDataView();
			}
		}

		/// <remarks> Handles ViewActionEvents. </remarks>
		public override void HandleEvent(NodeEvent eventValue)
		{
			if (eventValue is ViewActionEvent)
			{
				if (DataView == null)
					return;

				switch (((ViewActionEvent)eventValue).Action)
				{
					case SourceActions.First :
						DataView.First();
						break;
					case SourceActions.Prior :
						DataView.Prior();
						break;
					case SourceActions.Next :
						DataView.Next();
						break;
					case SourceActions.Last :
						DataView.Last();
						break;
					case SourceActions.Refresh :
						DataView.Refresh();
						break;
					case SourceActions.Insert :
						DataView.Insert();
						break;
					case SourceActions.Append :
						DataView.Append();
						break;
					case SourceActions.Edit :
						DataView.Edit();
						break;
					case SourceActions.Delete :
						DataView.Delete();
						break;
					case SourceActions.Post :
						DataView.Post();
						break;
					case SourceActions.PostDetails :
						DataView.PostDetails();
						break;
					case SourceActions.Cancel :
						DataView.Cancel();
						break;
					case SourceActions.RequestSave :
						DataView.RequestSave();
						break;
					case SourceActions.Validate :
						DataView.Validate();
						break;
					case SourceActions.Close :
						if (Surrogate == null)
							DataView.Close();
						break;
					case SourceActions.Open :
						if (Surrogate == null)
							DataView.Open();
						break;
                    case SourceActions.PostIfModified:
                        if (DataView.IsModified)
                            DataView.Post();
                        break;
				}
			}
			base.HandleEvent(eventValue);
		}

		protected internal override void BeforeDeactivate()
		{
			ClearDataView();
			base.BeforeDeactivate();
		}

		protected internal override void AfterActivate()
		{
			InternalUpdateView();
			base.AfterActivate();
		}
	}

	public enum SourceLinkType
	{
		None,
		Surrogate,
		Detail
	}

	[TypeConverter("System.ComponentModel.ExpandableObjectConverter,System")]
	public abstract class SourceLink: Node
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			UnlinkSource();
			Source = null;
		}

		public SourceLink(INode parent)
		{
			_parent = parent;
		}

		private INode _parent;

		public override IHost HostNode
		{
			get { return _parent.HostNode; }
		}

		// Source

		private ISource _source;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("Specified the source node the element will be attached to.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					UnlinkSource();
					ISource temp = _source;
					if (_source != null)
					{
						_source.Disposed -= new EventHandler(SourceDisposed);
						_source.DataChanged -= new DataLinkHandler(SourceDataChanged);
						_source.StateChanged -= new DataLinkHandler(SourceDataChanged);
					}
					_source = value;
					LinkSource();
					if (_source != null)
					{
						_source.Disposed += new EventHandler(SourceDisposed);
						_source.DataChanged += new DataLinkHandler(SourceDataChanged);
						_source.StateChanged += new DataLinkHandler(SourceDataChanged);
					}
				}
			}
		}
		
		private ISource _targetSource;
		[Browsable(false)]
		public ISource TargetSource
		{
			get { return _targetSource; }
			set
			{
				if (_targetSource != value)
				{
					UnlinkSource();
					if (_targetSource != null) 
						_targetSource.Disposed -= new EventHandler(TargetSourceDisposed);
					_targetSource = value;
					LinkSource();
					if (_targetSource != null) 
						_targetSource.Disposed += new EventHandler(TargetSourceDisposed);
				}
			}
		}

		protected virtual void TargetSourceDisposed(object sender, EventArgs args)
		{
			UnlinkSource();
		}
		
		protected virtual void SourceDisposed(object sender, EventArgs args)
		{
			UnlinkSource();
			Source = null;
		}

		public event EventHandler OnSourceDataChanged;

		private void SourceDataChanged(DataLink link, DataSet dataSet)
		{
			if (OnSourceDataChanged != null)
				OnSourceDataChanged(this, null);
		}

		public abstract void LinkSource();
		public abstract void UnlinkSource();
	}

	public class SurrogateSourceLink: SourceLink
	{
		public SurrogateSourceLink(INode parent): base(parent) {}
			
		public override void LinkSource()
		{
			if (TargetSource != null && Source != null)
				TargetSource.Surrogate = Source;
		}

		public override void UnlinkSource()
		{
			if (TargetSource != null)
				TargetSource.Surrogate = null;
		}	
	}

	public class DetailSourceLink: SourceLink
	{
		public DetailSourceLink(INode parent): base(parent) {}

		public override void LinkSource()
		{
			if (TargetSource != null && Source != null)
			{
				if (_attachMaster && Source.Master != null)
				{
					TargetSource.MasterKeyNames = Source.MasterKeyNames;
					TargetSource.DetailKeyNames = Source.DetailKeyNames;
					TargetSource.Master = Source.Master;
				}
				else if (_masterKeyNames != String.Empty)
				{
					TargetSource.MasterKeyNames = _masterKeyNames;
					TargetSource.DetailKeyNames = _detailKeyNames;
					TargetSource.Master = Source;
				}
			}
		}

		public override void UnlinkSource()
		{
			if (TargetSource != null)
				TargetSource.Master = null;
		}
		
		// MasterKeyNames

		private string _masterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The column names which form the key of the master in the master/detail releationship.")]
		public string MasterKeyNames
		{
			get { return _masterKeyNames; }
			set { _masterKeyNames = value; }
		}

		// DetailKeyNames

		private string _detailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The column names which form the key of the detail in the master/detail releationship.")]
		public string DetailKeyNames
		{
			get { return _detailKeyNames; }
			set { _detailKeyNames = value; }
		}

		// AttachMaster

		private bool _attachMaster = false;
		[DefaultValue(false)]
		[Description("When set to true, if this nodes Source property has a master, then the master will be attached to rather than the Source.")]
		public bool AttachMaster
		{
			get { return _attachMaster; }
			set { _attachMaster = value; }
		}
	}

	/// <summary> Disables any ISource nodes. </summary>
	public class DisableSourceEvent : NodeEvent
	{
		public override void Handle(INode node)
		{
			ISource source = node as ISource;
			if (source != null)
				source.Enabled = false;
		}
	}
}
