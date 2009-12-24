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
			FCursorType = DAE.CursorType.Dynamic;
			FRequestedIsolation = DAE.CursorIsolation.Browse;
			FRequestedCapabilities = DAE.CursorCapability.Navigable | DAE.CursorCapability.BackwardsNavigable | DAE.CursorCapability.Bookmarkable | DAE.CursorCapability.Searchable | DAE.CursorCapability.Updateable;
			FDataSource = new DataSource();
			try
			{
				FDataLink = new DataLink();
				try
				{
					FDataLink.Source = FDataSource;
					FDataLink.OnActiveChanged += new DataLinkHandler(ActiveChangedAction);
					FDataLink.OnStateChanged += new DataLinkHandler(StateChangedAction);
					FDataLink.OnDataChanged += new DataLinkHandler(DataChangedAction);
					FDataLink.OnRowChanging += new DataLinkFieldHandler(RowChangingAction);
					FDataLink.OnRowChanged += new DataLinkFieldHandler(RowChangedAction);
				}
				catch
				{
					FDataLink.Dispose();
					FDataLink = null;
					throw;
				}
			}
			catch
			{
				FDataSource.Dispose();
				FDataSource = null;
				throw;
			}
		}

		/// <remarks> Removes the DataSource and DataLink objects and unhooks the event handlers of the DataLink. </remarks>
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				base.Dispose(ADisposing);
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
					if (FDataLink != null)
					{
						FDataLink.OnActiveChanged -= new DataLinkHandler(ActiveChangedAction);
						FDataLink.OnStateChanged -= new DataLinkHandler(StateChangedAction);
						FDataLink.OnDataChanged -= new DataLinkHandler(DataChangedAction);
						FDataLink.OnRowChanging -= new DataLinkFieldHandler(RowChangingAction);
						FDataLink.OnRowChanged -= new DataLinkFieldHandler(RowChangedAction);
						FDataLink.Source = null;
						FDataLink.Dispose();
						FDataLink = null;
					}
				}
				finally
				{
					if (FDataSource != null)
					{
						FDataSource.Dispose();
						FDataSource = null;
					}
				}
			}
		}

		public event DataLinkHandler ActiveChanged
		{
			add { FDataLink.OnActiveChanged += value; }
			remove { FDataLink.OnActiveChanged -= value; }
		}
		
		public event DataLinkHandler StateChanged
		{
			add { FDataLink.OnStateChanged += value; }
			remove { FDataLink.OnStateChanged -= value; }
		}

		public event DataLinkHandler DataChanged
		{
			add { FDataLink.OnDataChanged += value; }
			remove { FDataLink.OnDataChanged -= value; }
		}
		
		public event DataLinkFieldHandler RowChanging
		{
			add { FDataLink.OnRowChanging += value; }
			remove { FDataLink.OnRowChanging -= value; }
		}
		
		public event DataLinkFieldHandler RowChanged
		{
			add { FDataLink.OnRowChanged += value; }
			remove { FDataLink.OnRowChanged -= value; }
		}
		
		public event DataLinkHandler Default
		{
			add { FDataLink.OnDefault += value; }
			remove { FDataLink.OnDefault -= value; }
		}
		
		// CursorType
		
		private DAE.CursorType FCursorType = DAE.CursorType.Dynamic;
		[DefaultValue(DAE.CursorType.Dynamic)]
		[Description("Determines the behavior of the cursor with respect to updates made after the cursor is opened.  If the cursor type is dynamic, updates made through the cursor will be visible.  If the cursor type is static, updates will not be visible.")]
		public DAE.CursorType CursorType
		{
			get { return FCursorType; }
			set 
			{ 
				if (FCursorType != value)
				{
					FCursorType = value;
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}
		
		// IsolationLevel

		private DAE.IsolationLevel FIsolationLevel = DAE.IsolationLevel.Browse;
		[DefaultValue(DAE.IsolationLevel.Browse)]
		[Description("The isolation level for transactions performed by this view.")]
		public DAE.IsolationLevel IsolationLevel
		{
			get { return FIsolationLevel; }
			set 
			{ 
				if (FIsolationLevel != value)
				{
					FIsolationLevel = value; 
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}
		
		// RequestedIsolation

		private DAE.CursorIsolation FRequestedIsolation;
		[DefaultValue(DAE.CursorIsolation.Browse)]
		[Description("The requested relative isolation of the cursor.  This will be used in conjunction with the isolation level of the transaction to determine the actual isolation of the cursor.")]
		public DAE.CursorIsolation RequestedIsolation
		{
			get { return FRequestedIsolation; }
			set 
			{ 
				if (FRequestedIsolation != value)
				{
					FRequestedIsolation = value; 
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}

		// RequestedCapabilities

		private DAE.CursorCapability FRequestedCapabilities;
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
			get { return FRequestedCapabilities; }
			set 
			{ 
				if (FRequestedCapabilities != value)
				{
					FRequestedCapabilities = value; 
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}

		// Expression

		private string FExpression = String.Empty;
		[DefaultValue("")]
		[Description("The expression to be used to select the data set.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Expression
		{
			get { return FExpression; }
			set
			{
				if (FExpression != value)
				{
					FExpression = ( value == null ? String.Empty : value );
					if (Active && Enabled)
						InternalUpdateView();
				}
			}
		}
		
		// InsertStatement
		
		private string FInsertStatement = String.Empty;
		[DefaultValue("")]
		[Description("A single statement of D4 to be used to override the default insert behavior of the source.  The new columns are accessible as parameters by their names, qualified by 'New.'.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string InsertStatement
		{
			get { return FInsertStatement; }
			set
			{
				if (FInsertStatement != value)
				{
					FInsertStatement = value == null ? String.Empty : value;
					if (FDataView != null)
						FDataView.InsertStatement = FInsertStatement;
				}
			}
		}
		
		// UpdateStatement

		private string FUpdateStatement = String.Empty;
		[DefaultValue("")]
		[Description("A single statement of D4 to be used to override the default update behavior of the source.  The new and old columns are accessible as parameters by their names, qualified by 'New.' and 'Old.'.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string UpdateStatement
		{
			get { return FUpdateStatement; }
			set
			{
				if (FUpdateStatement != value)
				{
					FUpdateStatement = value == null ? String.Empty : value;
					if (FDataView != null)
						FDataView.UpdateStatement = FUpdateStatement;
				}
			}
		}
		
		// DeleteStatement

		private string FDeleteStatement = String.Empty;
		[DefaultValue("")]
		[Description("A single statement of D4 to be used to override the default delete behavior of the source.  The old columns are accessible as parameters by their names, qualified by 'Old.'.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string DeleteStatement
		{
			get { return FDeleteStatement; }
			set
			{
				if (FDeleteStatement != value)
				{
					FDeleteStatement = value == null ? String.Empty : value;
					if (FDataView != null)
						FDataView.DeleteStatement = FDeleteStatement;
				}
			}
		}
		
		// Filter

		private string FFilter = String.Empty;
		[DefaultValue("")]
		[Description("The filter expression to apply to the data source.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Filter
		{
			get { return FFilter; }
			set
			{
				if (FFilter != value)
				{
					FFilter = value == null ? String.Empty : value;
					if (FDataView != null) // don't call UpdateDataview because we want to ignore the filter if we are surrogate (just like we ignore the expression).
						FDataView.Filter = FFilter;
				}
			}
		}

		// Enabled

		private bool FEnabled = true;
		[DefaultValue(true)]
		[Description("Represents the state of the data source.")]
		public bool Enabled
		{
			get { return FEnabled; }
			set 
			{ 
				if (FEnabled != value) 
				{
					FEnabled = value; 
					if (Active)
						InternalUpdateView();
				}
			}
		}

		// OnChange

		protected IAction FOnChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a different row in the dataset is selected.")]
		public IAction OnChange
		{
			get { return FOnChange; }
			set
			{
				if (FOnChange != value)
				{
					if (FOnChange != null)
						FOnChange.Disposed -= new EventHandler(ChangeActionDisposed);
					FOnChange = value;
					if (FOnChange != null)
						FOnChange.Disposed += new EventHandler(ChangeActionDisposed);
				}
			}
		}
		
		protected void ChangeActionDisposed(object ASender, EventArgs AArgs)
		{
			OnChange = null;
		}

		protected void DataChangedAction(DataLink ALink, DataSet ADataSet)
		{
			if (Active && (FOnChange != null))
				FOnChange.Execute(this, new EventParams());
		}
			
		// OnRowChanging

		protected IAction FOnRowChanging;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a field in the current row in the dataset is changing.")]
		public IAction OnRowChanging
		{
			get { return FOnRowChanging; }
			set
			{
				if (FOnRowChanging != value)
				{
					if (FOnRowChanging != null)
						FOnRowChanging.Disposed -= new EventHandler(RowChangingActionDisposed);
					FOnRowChanging = value;
					if (FOnRowChanging != null)
						FOnRowChanging.Disposed += new EventHandler(RowChangingActionDisposed);
				}
			}
		}
		
		protected void RowChangingActionDisposed(object ASender, EventArgs AArgs)
		{
			OnRowChanging = null;
		}

		protected void RowChangingAction(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (Active && (FOnRowChanging != null))
				FOnRowChanging.Execute(this, new EventParams("AField", AField));
		}
			
		// OnRowChange

		protected IAction FOnRowChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a field in the current row in the dataset is changed.")]
		public IAction OnRowChange
		{
			get { return FOnRowChange; }
			set
			{
				if (FOnRowChange != value)
				{
					if (FOnRowChange != null)
						FOnRowChange.Disposed -= new EventHandler(RowChangeActionDisposed);
					FOnRowChange = value;
					if (FOnRowChange != null)
						FOnRowChange.Disposed += new EventHandler(RowChangeActionDisposed);
				}
			}
		}
		
		protected void RowChangeActionDisposed(object ASender, EventArgs AArgs)
		{
			OnRowChange = null;
		}

		protected void RowChangedAction(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (Active && (FOnRowChange != null))
				FOnRowChange.Execute(this, new EventParams("AField", AField));
		}
			
		// OnActiveChange

		protected IAction FOnActiveChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when the dataset's active property changes.")]
		public IAction OnActiveChange
		{
			get { return FOnActiveChange; }
			set
			{
				if (FOnActiveChange != value)
				{
					if (FOnActiveChange != null)
						FOnActiveChange.Disposed += new EventHandler(ActiveChangedActionDisposed);
					FOnActiveChange = value;
					if (FOnActiveChange != null)
						FOnActiveChange.Disposed -= new EventHandler(ActiveChangedActionDisposed);
				}
			}
		}
		
		protected void ActiveChangedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnActiveChange = null;
		}

		protected void ActiveChangedAction(DataLink ALink, DataSet ADataSet)
		{
			if (Active && (FOnActiveChange != null))
				FOnActiveChange.Execute(this, new EventParams());
		}
			
		// OnStateChange

		protected IAction FOnStateChange;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed when a the dataset's state changes.")]
		public IAction OnStateChange
		{
			get { return FOnStateChange; }
			set
			{
				if (FOnStateChange != value)
				{
					if (FOnStateChange != null)
						FOnStateChange.Disposed -= new EventHandler(StateChangedActionDisposed);
					FOnStateChange = value;
					if (FOnStateChange != null)
						FOnStateChange.Disposed += new EventHandler(StateChangedActionDisposed);
				}
			}
		}
		
		protected void StateChangedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnStateChange = null;
		}

		protected void StateChangedAction(DataLink ALink, DataSet ADataSet)
		{
			if (Active && (FOnStateChange != null))
				FOnStateChange.Execute(this, new EventParams());
		}
			
		// OnDefault

		protected IAction FOnDefault;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed to allow for setting of default values for a new row. Values set during this action will not set the modified of the source.")]
		public IAction OnDefault
		{
			get { return FOnDefault; }
			set
			{
				if (FOnDefault != value)
				{
					if (FOnDefault != null)
						FOnDefault.Disposed -= new EventHandler(DefaultActionDisposed);
					FOnDefault = value;
					if (FOnDefault != null)
						FOnDefault.Disposed += new EventHandler(DefaultActionDisposed);
				}
			}
		}
		
		protected void DefaultActionDisposed(object ASender, EventArgs AArgs)
		{
			OnDefault = null;
		}

		protected void DefaultAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FOnDefault != null))
				FOnDefault.Execute(this, new EventParams());
		}
			
		// OnValidate

		protected IAction FOnValidate;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set posts the current row.  An exception here will prevent the dataset from posting.")]
		public IAction OnValidate
		{
			get { return FOnValidate; }
			set
			{
				if (FOnValidate != value)
				{
					if (FOnValidate != null)
						FOnValidate.Disposed -= new EventHandler(ValidatedActionDisposed);
					FOnValidate = value;
					if (FOnValidate != null)
						FOnValidate.Disposed += new EventHandler(ValidatedActionDisposed);
				}
			}
		}
		
		protected void ValidatedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnValidate = null;
		}

		protected void ValidatedAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FOnValidate != null))
				FOnValidate.Execute(this, new EventParams());
		}
			
		// BeforeOpen

		protected IAction FBeforeOpen;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set opens.")]
		public IAction BeforeOpen
		{
			get { return FBeforeOpen; }
			set
			{
				if (FBeforeOpen != value)
				{
					if (FBeforeOpen != null)
						FBeforeOpen.Disposed -= new EventHandler(BeforeOpenActionDisposed);
					FBeforeOpen = value;
					if (FBeforeOpen != null)
						FBeforeOpen.Disposed += new EventHandler(BeforeOpenActionDisposed);
				}
			}
		}
		
		protected void BeforeOpenActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeOpen = null;
		}

		protected void BeforeOpenAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforeOpen != null))
				FBeforeOpen.Execute(this, new EventParams());
		}
			
		// AfterOpen

		protected IAction FAfterOpen;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set opens.")]
		public IAction AfterOpen
		{
			get { return FAfterOpen; }
			set
			{
				if (FAfterOpen != value)
				{
					if (FAfterOpen != null)
						FAfterOpen.Disposed -= new EventHandler(AfterOpenActionDisposed);
					FAfterOpen = value;
					if (FAfterOpen != null)
						FAfterOpen.Disposed += new EventHandler(AfterOpenActionDisposed);
				}
			}
		}
		
		protected void AfterOpenActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterOpen = null;
		}

		protected void AfterOpenAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterOpen != null))
				FAfterOpen.Execute(this, new EventParams());
		}
			
		// BeforeClose

		protected IAction FBeforeClose;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set closes.")]
		public IAction BeforeClose
		{
			get { return FBeforeClose; }
			set
			{
				if (FBeforeClose != value)
				{
					if (FBeforeClose != null)
						FBeforeClose.Disposed -= new EventHandler(BeforeCloseActionDisposed);
					FBeforeClose = value;
					if (FBeforeClose != null)
						FBeforeClose.Disposed += new EventHandler(BeforeCloseActionDisposed);
				}
			}
		}
		
		protected void BeforeCloseActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeClose = null;
		}

		protected void BeforeCloseAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforeClose != null))
				FBeforeClose.Execute(this, new EventParams());
		}
			
		// AfterClose

		protected IAction FAfterClose;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set closes.")]
		public IAction AfterClose
		{
			get { return FAfterClose; }
			set
			{
				if (FAfterClose != value)
				{
					if (FAfterClose != null)
						FAfterClose.Disposed -= new EventHandler(AfterCloseActionDisposed);
					FAfterClose = value;
					if (FAfterClose != null)
						FAfterClose.Disposed += new EventHandler(AfterCloseActionDisposed);
				}
			}
		}
		
		protected void AfterCloseActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterClose = null;
		}

		protected void AfterCloseAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterClose != null))
				FAfterClose.Execute(this, new EventParams());
		}
			
		// BeforeInsert

		protected IAction FBeforeInsert;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set enters insert state.")]
		public IAction BeforeInsert
		{
			get { return FBeforeInsert; }
			set
			{
				if (FBeforeInsert != value)
				{
					if (FBeforeInsert != null)
						FBeforeInsert.Disposed -= new EventHandler(BeforeInsertActionDisposed);
					FBeforeInsert = value;
					if (FBeforeInsert != null)
						FBeforeInsert.Disposed += new EventHandler(BeforeInsertActionDisposed);
				}
			}
		}
		
		protected void BeforeInsertActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeInsert = null;
		}

		protected void BeforeInsertAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforeInsert != null))
				FBeforeInsert.Execute(this, new EventParams());
		}
			
		// AfterInsert

		protected IAction FAfterInsert;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set enters insert state.")]
		public IAction AfterInsert
		{
			get { return FAfterInsert; }
			set
			{
				if (FAfterInsert != value)
				{
					if (FAfterInsert != null)
						FAfterInsert.Disposed -= new EventHandler(AfterInsertActionDisposed);
					FAfterInsert = value;
					if (FAfterInsert != null)
						FAfterInsert.Disposed += new EventHandler(AfterInsertActionDisposed);
				}
			}
		}
		
		protected void AfterInsertActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterInsert = null;
		}

		protected void AfterInsertAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterInsert != null))
				FAfterInsert.Execute(this, new EventParams());
		}
			
		// BeforeEdit

		protected IAction FBeforeEdit;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set enters edit state.")]
		public IAction BeforeEdit
		{
			get { return FBeforeEdit; }
			set
			{
				if (FBeforeEdit != value)
				{
					if (FBeforeEdit != null)
						FBeforeEdit.Disposed -= new EventHandler(BeforeEditActionDisposed);
					FBeforeEdit = value;
					if (FBeforeEdit != null)
						FBeforeEdit.Disposed += new EventHandler(BeforeEditActionDisposed);
				}
			}
		}
		
		protected void BeforeEditActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeEdit = null;
		}

		protected void BeforeEditAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforeEdit != null))
				FBeforeEdit.Execute(this, new EventParams());
		}
			
		// AfterEdit

		protected IAction FAfterEdit;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set enters edit state.")]
		public IAction AfterEdit
		{
			get { return FAfterEdit; }
			set
			{
				if (FAfterEdit != value)
				{
					if (FAfterEdit != null)
						FAfterEdit.Disposed -= new EventHandler(AfterEditActionDisposed);
					FAfterEdit = value;
					if (FAfterEdit != null)
						FAfterEdit.Disposed += new EventHandler(AfterEditActionDisposed);
				}
			}
		}
		
		protected void AfterEditActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterEdit = null;
		}

		protected void AfterEditAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterEdit != null))
				FAfterEdit.Execute(this, new EventParams());
		}
			
		// BeforeDelete

		protected IAction FBeforeDelete;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before a row in the data set is deleted.")]
		public IAction BeforeDelete
		{
			get { return FBeforeDelete; }
			set
			{
				if (FBeforeDelete != value)
				{
					if (FBeforeDelete != null)
						FBeforeDelete.Disposed -= new EventHandler(BeforeDeleteActionDisposed);
					FBeforeDelete = value;
					if (FBeforeDelete != null)
						FBeforeDelete.Disposed += new EventHandler(BeforeDeleteActionDisposed);
				}
			}
		}
		
		protected void BeforeDeleteActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeDelete = null;
		}

		protected void BeforeDeleteAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforeDelete != null))
				FBeforeDelete.Execute(this, new EventParams());
		}
			
		// AfterDelete

		protected IAction FAfterDelete;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after a row in the data set is deleted.")]
		public IAction AfterDelete
		{
			get { return FAfterDelete; }
			set
			{
				if (FAfterDelete != value)
				{
					if (FAfterDelete != null)
						FAfterDelete.Disposed -= new EventHandler(AfterDeleteActionDisposed);
					FAfterDelete = value;
					if (FAfterDelete != null)
						FAfterDelete.Disposed += new EventHandler(AfterDeleteActionDisposed);
				}
			}
		}
		
		protected void AfterDeleteActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterDelete = null;
		}

		protected void AfterDeleteAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterDelete != null))
				FAfterDelete.Execute(this, new EventParams());
		}
			
		// BeforePost

		protected IAction FBeforePost;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set is posted.")]
		public IAction BeforePost
		{
			get { return FBeforePost; }
			set
			{
				if (FBeforePost != value)
				{
					if (FBeforePost != null)
						FBeforePost.Disposed -= new EventHandler(BeforePostActionDisposed);
					FBeforePost = value;
					if (FBeforePost != null)
						FBeforePost.Disposed += new EventHandler(BeforePostActionDisposed);
				}
			}
		}
		
		protected void BeforePostActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforePost = null;
		}

		protected void BeforePostAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforePost != null))
				FBeforePost.Execute(this, new EventParams());
		}
			
		// AfterPost

		protected IAction FAfterPost;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set is posted.")]
		public IAction AfterPost
		{
			get { return FAfterPost; }
			set
			{
				if (FAfterPost != value)
				{
					if (FAfterPost != null)
						FAfterPost.Disposed -= new EventHandler(AfterPostActionDisposed);
					FAfterPost = value;
					if (FAfterPost != null)
						FAfterPost.Disposed += new EventHandler(AfterPostActionDisposed);
				}
			}
		}
		
		protected void AfterPostActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterPost = null;
		}

		protected void AfterPostAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterPost != null))
				FAfterPost.Execute(this, new EventParams());
		}
			
		// BeforeCancel

		protected IAction FBeforeCancel;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed before the data set is canceled.")]
		public IAction BeforeCancel
		{
			get { return FBeforeCancel; }
			set
			{
				if (FBeforeCancel != value)
				{
					if (FBeforeCancel != null)
						FBeforeCancel.Disposed -= new EventHandler(BeforeCancelActionDisposed);
					FBeforeCancel = value;
					if (FBeforeCancel != null)
						FBeforeCancel.Disposed += new EventHandler(BeforeCancelActionDisposed);
				}
			}
		}
		
		protected void BeforeCancelActionDisposed(object ASender, EventArgs AArgs)
		{
			BeforeCancel = null;
		}

		protected void BeforeCancelAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FBeforeCancel != null))
				FBeforeCancel.Execute(this, new EventParams());
		}
			
		// AfterCancel

		protected IAction FAfterCancel;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action that will be executed after the data set is canceled.")]
		public IAction AfterCancel
		{
			get { return FAfterCancel; }
			set
			{
				if (FAfterCancel != value)
				{
					if (FAfterCancel != null)
						FAfterCancel.Disposed -= new EventHandler(AfterCancelActionDisposed);
					FAfterCancel = value;
					if (FAfterCancel != null)
						FAfterCancel.Disposed += new EventHandler(AfterCancelActionDisposed);
				}
			}
		}
		
		protected void AfterCancelActionDisposed(object ASender, EventArgs AArgs)
		{
			AfterCancel = null;
		}

		protected void AfterCancelAction(object ASender, EventArgs AArgs)
		{
			if (Active && (FAfterCancel != null))
				FAfterCancel.Execute(this, new EventParams());
		}
			
		// DataLink

		private DataLink FDataLink;

		// DataSource

		private DataSource FDataSource;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DataSource DataSource
		{
			get { return FDataSource; }
		}

		// DataView

		private DataView FDataView;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DataView DataView
		{
			get { return (DataView)FDataSource.DataSet; }
		}

		private void ClearDataView()
		{
			try
			{
				if ((FDataView != null) && (FSurrogate == null))
				{
					//try
					//{
						FDataView.OnErrors -= new ErrorsOccurredHandler(ErrorsOccurred);
						FDataView.OnValidate -= new EventHandler(ValidatedAction);
						FDataView.OnDefault -= new EventHandler(DefaultAction);
						FDataView.BeforeOpen -= new EventHandler(BeforeOpenAction);
						FDataView.AfterOpen -= new EventHandler(AfterOpenAction);
						FDataView.BeforeClose -= new EventHandler(BeforeCloseAction);
						FDataView.AfterClose -= new EventHandler(AfterCloseAction);
						FDataView.BeforeInsert -= new EventHandler(BeforeInsertAction);
						FDataView.AfterInsert -= new EventHandler(AfterInsertAction);
						FDataView.BeforeEdit -= new EventHandler(BeforeEditAction);
						FDataView.AfterEdit -= new EventHandler(AfterEditAction);
						FDataView.BeforeDelete -= new EventHandler(BeforeDeleteAction);
						FDataView.AfterDelete -= new EventHandler(AfterDeleteAction);
						FDataView.BeforePost -= new EventHandler(BeforePostAction);
						FDataView.AfterPost -= new EventHandler(AfterPostAction);
						FDataView.BeforeCancel -= new EventHandler(BeforeCancelAction);
						FDataView.AfterCancel -= new EventHandler(AfterCancelAction);
						FDataView.Dispose();
						FDataView = null;
					//}  
					//finally
					//{
					//	ExecuteEndScript();
					//}
				}
			}
			finally
			{
				DataSource.DataSet = null;
			}
		}
		
		protected virtual DataView CreateDataView()
		{
			DataView LResult = new DataView();
			try
			{
				LResult.Session = HostNode.Session.DataSession;
				LResult.BeginScript = FBeginScript;
				LResult.EndScript = FEndScript;
				LResult.UseBrowse = FUseBrowse;
				LResult.UseApplicationTransactions = FUseApplicationTransactions;
				LResult.ShouldEnlist = FShouldEnlist;
				LResult.CursorType = FCursorType;
				LResult.IsolationLevel = FIsolationLevel;
				LResult.RequestedIsolation = FRequestedIsolation;
				LResult.RequestedCapabilities = FRequestedCapabilities;
				LResult.IsReadOnly = FIsReadOnly;
				LResult.IsWriteOnly = FIsWriteOnly;
				LResult.RefreshAfterPost = FRefreshAfterPost;
				LResult.Expression = FExpression;
				LResult.InsertStatement = FInsertStatement;
				LResult.UpdateStatement = FUpdateStatement;
				LResult.DeleteStatement = FDeleteStatement;
				LResult.Filter = FFilter;
				LResult.OnErrors += new ErrorsOccurredHandler(ErrorsOccurred);
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
		
		public event EventHandler OnUpdateView;

		protected virtual void InternalUpdateView()
		{
			ClearDataView();

			if (Enabled)
			{
				if ((FSurrogate != null) && FSurrogate.Enabled)
					DataSource.DataSet = FSurrogate.DataView;
				else
				{
					FDataView = CreateDataView();
					FDataView.OnValidate += new EventHandler(ValidatedAction);
					FDataView.OnDefault += new EventHandler(DefaultAction);
					FDataView.BeforeOpen += new EventHandler(BeforeOpenAction);
					FDataView.AfterOpen += new EventHandler(AfterOpenAction);
					FDataView.BeforeClose += new EventHandler(BeforeCloseAction);
					FDataView.AfterClose += new EventHandler(AfterCloseAction);
					FDataView.BeforeInsert += new EventHandler(BeforeInsertAction);
					FDataView.AfterInsert += new EventHandler(AfterInsertAction);
					FDataView.BeforeEdit += new EventHandler(BeforeEditAction);
					FDataView.AfterEdit += new EventHandler(AfterEditAction);
					FDataView.BeforeDelete += new EventHandler(BeforeDeleteAction);
					FDataView.AfterDelete += new EventHandler(AfterDeleteAction);
					FDataView.BeforePost += new EventHandler(BeforePostAction);
					FDataView.AfterPost += new EventHandler(AfterPostAction);
					FDataView.BeforeCancel += new EventHandler(BeforeCancelAction);
					FDataView.AfterCancel += new EventHandler(AfterCancelAction);
					DataSource.DataSet = FDataView;
					InternalUpdateMaster();
					InternalUpdateParams();
					try 
					{
						FDataView.Open(FOpenState);
					}
					catch
					{
						Enabled = false;
						throw;
					}
				}
			}

			// call onupdateview
			if (OnUpdateView != null)
				OnUpdateView(this, null);
		}

		// Error Handling
		private void ErrorsOccurred(DataSet ADataSet, CompilerMessages AMessages)
		{
			ErrorList AErrors = new ErrorList();
			AErrors.AddRange(AMessages);
			HostNode.Session.ReportErrors(HostNode, AErrors);
		}

		private bool FUseBrowse = true;
		[DefaultValue(true)]
		[Description("Indicates whether the view will use a browse clause by default to request data.")]
		public bool UseBrowse
		{
			get { return FUseBrowse; }
			set 
			{ 
				if (FUseBrowse != value)
				{
					FUseBrowse = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool FUseApplicationTransactions = true;
		[DefaultValue(true)]
		[Description("Indicates whether the view will use application transactions to manipulate data.")]
		public bool UseApplicationTransactions
		{
			get { return FUseApplicationTransactions; }
			set
			{
				if (FUseApplicationTransactions != value)
				{
					FUseApplicationTransactions = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private EnlistMode FShouldEnlist = EnlistMode.Default;
		[DefaultValue(EnlistMode.Default)]
		[Description("Indicates whether the source should enlist in the application transaction of its master source.")]
		public EnlistMode ShouldEnlist
		{
			get { return FShouldEnlist; }
			set
			{
				if (FShouldEnlist != value)
				{
					FShouldEnlist = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool FIsReadOnly = false;
		[DefaultValue(false)]
		[Description("Indicates whether the data in the view will be updateable.")]
		public bool IsReadOnly
		{
			get { return FIsReadOnly; }
			set
			{
				if (FIsReadOnly != value)
				{
					FIsReadOnly = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool FIsWriteOnly = false;
		[DefaultValue(false)]
		[Description("Indicates whether the view will be used solely for inserting data.")]
		public bool IsWriteOnly
		{
			get { return FIsWriteOnly; }
			set
			{
				if (FIsWriteOnly != value)
				{
					FIsWriteOnly = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private DataSetState FOpenState = DataSetState.Browse;
		[DefaultValue(DataSetState.Browse)]
		[Description("Indicates the open state for the view.")]
		public DataSetState OpenState
		{
			get { return FOpenState; }
			set
			{
				if (FOpenState != value)
				{
					FOpenState = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		private bool FRefreshAfterPost = false;
		[DefaultValue(false)]
		[Description("Indicates whether or not the view will be refreshed after a call to post.")]
		public bool RefreshAfterPost
		{
			get { return FRefreshAfterPost; }
			set
			{
				if (FRefreshAfterPost != value)
				{
					FRefreshAfterPost = value;
					if (Active)
						InternalUpdateView();
				}
			}
		}
		
		// BeginScript

		private string FBeginScript = String.Empty;
		[DefaultValue("")]
		[Description("A script that will be executed when the source is activated (before opening the expression).")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string BeginScript
		{
			get { return FBeginScript; }
			set { FBeginScript = value == null ? String.Empty : value; }
		}

		// EndScript

		private string FEndScript = String.Empty;
		[DefaultValue("")]
		[Description("A script that will be executed when the source is deactivated.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string EndScript
		{
			get { return FEndScript; }
			set { FEndScript = value == null ? String.Empty : value; }
		}

		// Surrogate

		private ISource FSurrogate;
		/// <remarks> Links and unlinks when Activate and Deactivate is called. </remarks>
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public ISource Surrogate
		{
			get { return FSurrogate; }
			set
			{
				if (FSurrogate != value)
				{
					if (FSurrogate != null) 
					{
						FSurrogate.Disposed -= new EventHandler(SurrogateDisposed);
						FSurrogate.OnUpdateView += new EventHandler(SurrogateUpdateView);
					}
					FSurrogate = value;
					if (FSurrogate != null)
					{
						FSurrogate.Disposed += new EventHandler(SurrogateDisposed);
						FSurrogate.OnUpdateView += new EventHandler(SurrogateUpdateView);
					}
					ClearDataView();
					if (Active)
						InternalUpdateView();
				}
			}
		}

		private void SurrogateDisposed(object ASender, EventArgs AArgs)
		{
			Surrogate = null;
		}

		private void SurrogateUpdateView(object ASender, EventArgs AArgs)
		{
			if (Active)
				InternalUpdateView();
		}

		// Master/Detail

		private string FMasterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The key column(s) in the master source to use for the master-detail relationship.")]
		public string MasterKeyNames
		{
			get { return FMasterKeyNames; }
			set
			{
				if (FMasterKeyNames != value)
				{
					FMasterKeyNames = value;
					UpdateMaster();
				}
			}
		}

		private string FDetailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The key column(s) in this source to use as the detail key for a master-detail relationship.")]
		public string DetailKeyNames
		{
			get { return FDetailKeyNames; }
			set
			{
				if (FDetailKeyNames != value)
				{
					FDetailKeyNames = value;
					UpdateMaster();
				}
			}
		}

		private ISource FMaster;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("When set, this source will be filtered and will re-query as the master source navigates.")]
		public ISource Master
		{
			get { return FMaster; }
			set
			{
				if (FMaster != value)
				{
					if (FMaster != null)
						FMaster.Disposed -= new EventHandler(MasterDisposed);
					FMaster = value;
					if (FMaster != null)
						FMaster.Disposed += new EventHandler(MasterDisposed);
					UpdateMaster();
				}
			}
		}
		
		private void MasterDisposed(object ASender, EventArgs AArgs)
		{
			Master = null;
		}

		private void InternalUpdateMaster()
		{
			if (FDataView != null)
			{
				if (Master != null)
				{
					FDataView.MasterKeyNames = MasterKeyNames;
					FDataView.DetailKeyNames = DetailKeyNames;
					FDataView.MasterSource = Master.DataSource;
					FDataView.WriteWhereClause = WriteWhereClause;
				}
				else
					FDataView.MasterSource = null;
			}
		}

		/// <summary> Called when the master source property is changed. </summary>
		private void UpdateMaster()
		{
			if (Active)
				InternalUpdateMaster();
		}
		
		private bool FWriteWhereClause = true;
		[DefaultValue(true)]
		[Description("Determines whether the data view will automatically produce the where clause necessary to limit the result set by the master source.  If false, the expression must contain the necessary restrictions.  Current master data view values are available as AMaster<detail column name (with qualifiers replaced with underscores)> parameters within the expression.")]
		public bool WriteWhereClause
		{
			get { return FWriteWhereClause; }
			set
			{
				if (FWriteWhereClause != value)
				{
					FWriteWhereClause = value;
					UpdateMaster();
				}
			}
		}
		
		// Parameters
		
		private DataParams FParams;
		public DataParams Params
		{
			get { return FParams; }
			set 
			{ 
				FParams = value; 
				UpdateParams();
			}
		}
		
		private DataSetParamGroup FParamsParamGroup;
		
		private List<DataSetParamGroup> FArgumentParamGroups = new List<DataSetParamGroup>();

		private void InternalUpdateParams()
		{
			if (FDataView != null)
			{
				// Remove the old argument param groups
				foreach (var LGroup in FArgumentParamGroups)
				{
					if (FDataView.ParamGroups.Contains(LGroup))
						FDataView.ParamGroups.Remove(LGroup);
				}
				FArgumentParamGroups.Clear();
				
				// Add new argument param groups
				BaseArgument.CollectDataSetParamGroup(this, FArgumentParamGroups);
				foreach (var LGroup in FArgumentParamGroups)
					FDataView.ParamGroups.Add(LGroup);
				
				if (FParams != null)
				{
					if (FParamsParamGroup != null)
					{
						DataView.ParamGroups.SafeRemove(FParamsParamGroup);
						FParamsParamGroup = null;
					}
					
					DataSetParamGroup LParamGroup = new DataSetParamGroup();
					foreach (DataParam LParam in FParams)
					{
						DataSetParam LDataSetParam = new DataSetParam();
						LDataSetParam.Name = LParam.Name;
						LDataSetParam.Modifier = LParam.Modifier;
						LDataSetParam.DataType = LParam.DataType;
						LDataSetParam.Value = LParam.Value;
						LParamGroup.Params.Add(LDataSetParam);
					}

					FDataView.ParamGroups.Add(LParamGroup);
					FParamsParamGroup = LParamGroup;
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
		public DataField this[string AColumnName] 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView[AColumnName]; 
			} 
		}
		
		[Browsable(false)]
		public DataField this[int AColumnIndex] 
		{ 
			get 
			{ 
				CheckEnabled();
				return DataView[AColumnIndex]; 
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
		
		public Row GetKey()
		{
			CheckEnabled();
			return DataView.GetKey();
		}
		
		public bool FindKey(Row AKey)
		{
			CheckEnabled();
			return DataView.FindKey(AKey);
		}
		
		public void FindNearest(Row AKey)
		{
			CheckEnabled();
			DataView.FindNearest(AKey);
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

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(ISourceChild).IsAssignableFrom(AChildType) || typeof(IBaseArgument).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}
		
		protected internal override void ChildrenChanged()
		{
			UpdateParams();
		}

		protected override void Activate()
		{
			if (FExpression == String.Empty) 
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
		public override void HandleEvent(NodeEvent AEvent)
		{
			if (AEvent is ViewActionEvent)
			{
				if (DataView == null)
					return;

				switch (((ViewActionEvent)AEvent).Action)
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
			base.HandleEvent(AEvent);
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
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			UnlinkSource();
			Source = null;
		}

		public SourceLink(INode AParent)
		{
			FParent = AParent;
		}

		private INode FParent;

		public override IHost HostNode
		{
			get { return FParent.HostNode; }
		}

		// Source

		private ISource FSource;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("Specified the source node the element will be attached to.")]
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
				{
					UnlinkSource();
					ISource LTemp = FSource;
					if (FSource != null)
					{
						FSource.Disposed -= new EventHandler(SourceDisposed);
						FSource.DataChanged -= new DataLinkHandler(SourceDataChanged);
						FSource.StateChanged -= new DataLinkHandler(SourceDataChanged);
					}
					FSource = value;
					LinkSource();
					if (FSource != null)
					{
						FSource.Disposed += new EventHandler(SourceDisposed);
						FSource.DataChanged += new DataLinkHandler(SourceDataChanged);
						FSource.StateChanged += new DataLinkHandler(SourceDataChanged);
					}
				}
			}
		}
		
		private ISource FTargetSource;
		[Browsable(false)]
		public ISource TargetSource
		{
			get { return FTargetSource; }
			set
			{
				if (FTargetSource != value)
				{
					UnlinkSource();
					if (FTargetSource != null) 
						FTargetSource.Disposed -= new EventHandler(TargetSourceDisposed);
					FTargetSource = value;
					LinkSource();
					if (FTargetSource != null) 
						FTargetSource.Disposed += new EventHandler(TargetSourceDisposed);
				}
			}
		}

		protected virtual void TargetSourceDisposed(object ASender, EventArgs AArgs)
		{
			UnlinkSource();
		}
		
		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			UnlinkSource();
			Source = null;
		}

		public event EventHandler OnSourceDataChanged;

		private void SourceDataChanged(DataLink ALink, DataSet ADataSet)
		{
			if (OnSourceDataChanged != null)
				OnSourceDataChanged(this, null);
		}

		public abstract void LinkSource();
		public abstract void UnlinkSource();
	}

	public class SurrogateSourceLink: SourceLink
	{
		public SurrogateSourceLink(INode AParent): base(AParent) {}
			
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
		public DetailSourceLink(INode AParent): base(AParent) {}

		public override void LinkSource()
		{
			if (TargetSource != null && Source != null)
			{
				if (FAttachMaster && Source.Master != null)
				{
					TargetSource.MasterKeyNames = Source.MasterKeyNames;
					TargetSource.DetailKeyNames = Source.DetailKeyNames;
					TargetSource.Master = Source.Master;
				}
				else if (FMasterKeyNames != String.Empty)
				{
					TargetSource.MasterKeyNames = FMasterKeyNames;
					TargetSource.DetailKeyNames = FDetailKeyNames;
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

		private string FMasterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The column names which form the key of the master in the master/detail releationship.")]
		public string MasterKeyNames
		{
			get { return FMasterKeyNames; }
			set { FMasterKeyNames = value; }
		}

		// DetailKeyNames

		private string FDetailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("The column names which form the key of the detail in the master/detail releationship.")]
		public string DetailKeyNames
		{
			get { return FDetailKeyNames; }
			set { FDetailKeyNames = value; }
		}

		// AttachMaster

		private bool FAttachMaster = false;
		[DefaultValue(false)]
		[Description("When set to true, if this nodes Source property has a master, then the master will be attached to rather than the Source.")]
		public bool AttachMaster
		{
			get { return FAttachMaster; }
			set { FAttachMaster = value; }
		}
	}

	/// <summary> Disables any ISource nodes. </summary>
	public class DisableSourceEvent : NodeEvent
	{
		public override void Handle(INode ANode)
		{
			ISource LSource = ANode as ISource;
			if (LSource != null)
				LSource.Enabled = false;
		}
	}
}
