/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Specialized;
using System.Text;	   

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client.Design;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Client
{
	/// <summary> A DataSet descendent for general use with a Dataphor Server. </summary>
	/// <remarks>
	/// A note about optimization in the DataView: There are several properties that are specifically
	/// designed to minimize the amount of data traffic caused by the most common usage patterns
	/// for a typical Dataphor Frontend application. These are OpenState, IsWriteOnly, and RefreshAfterPost.
	/// For optimal usage, these options should be set as follows:
	/// Mode				|	OpenState	|	RefreshAfterPost	|	WriteOnly
	///	Browse (Browse Form)|	Browse		|	True				|	False
	///	Insert (Add Form)	|	Insert		|	False				|	True
	/// Edit (Edit Form)	|	Edit		|	False				|	False
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DataView),"Icons.DataView.bmp")]
	[DesignerSerializer("Alphora.Dataphor.DAE.Client.Design.ActiveLastSerializer,Alphora.Dataphor.DAE.Client", "System.ComponentModel.Design.Serialization.CodeDomSerializer,System.Design")]
	public class DataView : TableDataSet
	{
		public DataView()
		{
			_useBrowse = true;
			_writeWhereClause = true;
			UseApplicationTransactions = true;
			_shouldEnlist = EnlistMode.Default;
		}

		public DataView(IContainer container) : this()
		{
			if (container != null)
				container.Add(this);
		}

		protected override void InternalDispose(bool disposing)
		{
			try
			{
				base.InternalDispose(disposing);
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

		#region Cursor

		// Cursor
		// The A/T Cursor is only used if this is an A/T server opened in Browse Mode.
		// In all other cases, the normal cursor will already be participating in the A/T.
		// This new cursor is only used for update and proposable calls. Once the update
		// is complete, if an A/T cursor was being used, the main cursor must be refreshed
		// to the newly inserted/updated row. This is the purpose of the UpdatesThroughCursor()
		// call, which is overridden by this component to return false if this is an A/T server,
		// or if custom insert/update/delete statements are provided.
		private DAECursor _aTCursor;

		// This call is used to return the cursor that should be used to perform update
		// and proposable calls.		
		protected override DAECursor GetEditCursor()
		{
			if (IsApplicationTransactionServer && (_openState == DataSetState.Browse))
				return _aTCursor;
			else
				return _cursor;
		}

		#endregion
		
		#region Master / Detail
		
		protected override void MasterRowChanged(DataLink lInk, DataSet dataSet, DataField field)
		{
			if (Active)
			{
				bool changed = dataSet.IsEmpty() != IsEmpty();
				if (!changed && !IsEmpty())
					for (int i = 0; i < MasterKey.Columns.Count; i++)
					{
						string masterColumn = MasterKey.Columns[i].Name;
						string detailColumn = DetailKey.Columns[i].Name;
						changed = !_fields.Contains(detailColumn) || (dataSet[masterColumn].IsNil != Fields[detailColumn].IsNil);
						if (changed)
							break;
						if (!Fields[detailColumn].IsNil)
						{
							object masterValue = dataSet[masterColumn].AsNative;
							object detailValue = Fields[detailColumn].AsNative;
							if (masterValue is IComparable)
								changed = ((IComparable)masterValue).CompareTo(detailValue) != 0;
							else
								changed = !(masterValue.Equals(detailValue));
							if (changed)
								break;
						}
					}
				if (changed)
					CursorSetChanged(null, IsApplicationTransactionClient && !_isJoined);
			}
		}
		
		/// <summary>Returns true if the master is enlisted in an A/T. Cannot be invoked without a master source.</summary>
		public bool IsMasterEnlisted()
		{
			DataView dataView = MasterSource.DataSet as DataView;
			return (dataView != null) && (dataView.IsApplicationTransactionClient || dataView.IsApplicationTransactionServer);
		}

		/// <summary> Returns true if the master is set up (see IsMasterSetup()), and there is a value for each of the master's columns (or WriteWhereClause is true). </summary>
		public override bool IsMasterValid()
		{
			if (IsMasterSetup())
			{
				TableDataSet dataSet = MasterSource.DataSet as TableDataSet;
				bool isMasterValid = (dataSet == null) || (!dataSet.IsDetail() || dataSet.IsMasterValid());
				if (isMasterValid && !MasterSource.DataSet.IsEmpty())
				{
					if (!WriteWhereClause) // If the where clause is custom, allow nil master values
						return true;
						
					foreach (DAE.Schema.TableVarColumn column in MasterKey.Columns)
						if (!(MasterSource.DataSet.Fields[column.Name].HasValue()))
							return false;
					return true;
				}
			}
			return false;
		}
		
		protected override bool InternalColumnChanging(DataField field, IRow oldRow, IRow newRow)
		{
			base.InternalColumnChanging(field, oldRow, newRow);
			if (GetEditCursor().Validate(oldRow, newRow, field.ColumnName))
			{
				_valueFlags.SetAll(true);
				return true;
			}
			return false;
		}

		protected override bool InternalColumnChanged(DataField field, IRow oldRow, IRow newRow)
		{
			if (GetEditCursor().Change(oldRow, newRow, field.ColumnName))
			{
				_valueFlags.SetAll(true);
				return base.InternalColumnChanged(null, oldRow, newRow);
			}
			else
				return base.InternalColumnChanged(field, oldRow, newRow);
		}
		
		protected override void InternalChangeColumn(DataField field, IRow oldRow, IRow newRow)
		{
			Process.BeginTransaction(IsolationLevel);
			try
			{
				base.InternalChangeColumn(field, oldRow, newRow);

				Process.CommitTransaction();
			}
			catch (Exception e)
			{
				try
				{
					Process.RollbackTransaction();
				}
				catch (Exception rollbackException)
				{
					throw new DAE.Server.ServerException(DAE.Server.ServerException.Codes.RollbackError, e, rollbackException.ToString());
				}

				throw;
			}
		}

		protected override void InternalDefault(IRow row)
		{
			base.InternalDefault(row);
			if (GetEditCursor().Default(row, String.Empty))
				_valueFlags.SetAll(true);
		}
		
		#endregion

		#region Adorn Expression

		private AdornColumnExpressions _columns = new AdornColumnExpressions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AdornColumnExpressions Columns { get { return _columns; } }
		
		private CreateConstraintDefinitions _constraints = new CreateConstraintDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CreateConstraintDefinitions Constraints { get { return _constraints; } }
		
		private OrderDefinitions _orders = new OrderDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OrderDefinitions Orders { get { return _orders; } }

		private KeyDefinitions _keys = new KeyDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public KeyDefinitions Keys { get { return _keys; } }

		private string AdornExpressionToString()
		{
			if (IsAdorned())
			{
				AdornExpression adornExpression = new AdornExpression();
				adornExpression.Expression = new IdentifierExpression("A");
				adornExpression.Expressions.AddRange(_columns);
				adornExpression.Constraints.AddRange(_constraints);
				adornExpression.Orders.AddRange(_orders);
				adornExpression.Keys.AddRange(_keys);
				return new D4TextEmitter().Emit(adornExpression);
			}
			else
				return String.Empty;
		}

		private void StringToAdornExpression(string value)
		{
			if (value == String.Empty)
			{
				_orders.Clear();
				_constraints.Clear();
				_columns.Clear();
			}
			else
			{
				Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				AdornExpression expression = (AdornExpression)parser.ParseExpression(value);
				_columns = expression.Expressions;
				_constraints = expression.Constraints;
				_orders = expression.Orders;
				_keys = expression.Keys;
			}
		}

		[Browsable(false)]
		[DefaultValue("")]
		public string AdornExpression
		{
			get { return AdornExpressionToString(); }
			set { StringToAdornExpression(value); }
		}
		
		private bool IsAdorned()
		{
			return
				(_columns.Count > 0) ||
				(_constraints.Count > 0) ||
				(_orders.Count > 0);
		}

		#endregion

		#region Expression

		// Expression
		protected string _expression = String.Empty;
		[DefaultValue("")]
		[Category("Data")]
		[Description("Expression")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string Expression
		{
			get { return _expression; }
			set
			{
				CheckInactive();
				_expression = value == null ? String.Empty : value;
			}
		}
		
		protected bool _writeWhereClause;

		/// <summary> When true, the DataSet will automatically restrict the expression based on the Master/Detail relationship. </summary>
		/// <remarks> 
		///		If this is set to false, the DataSet user must manually include the restriction column in the expression.  
		///		The values of the master DataSet are exposed as parameters named AMasterXXX where XXX is the name 
		///		of the <emphasis role="bold">detail key name</emphasis> with '.' replaced with '_'.  The default is True.
		///	</remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When true, the DataSet will automatically restrict the expression based on the Master/Detail relationship.")]
		public bool WriteWhereClause
		{
			get { return _writeWhereClause; }
			set
			{
				if (_writeWhereClause != value)
				{
					_writeWhereClause = value;
					if (Active)
						CursorSetChanged(null, true);
				}
			}
		}

		protected bool _useBrowse;

		/// <summary> When true, the DataView will use a "browse" rather than an "order" cursor. </summary>
		/// <remarks> 
		///		The default is True.  Typically a browse is used when the underlying storage device 
		///		can effeciently search and navigate based on the DataView's sort order.  If this is not 
		///		the case, then an order may be more effecient. 
		///	</remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public bool UseBrowse
		{
			get { return _useBrowse; }
			set 
			{
				if (_useBrowse != value)
				{
					if (Active)
					{
						using (IRow row = RememberActive())
						{
							_useBrowse = value; 
							CursorSetChanged(row, true);
						}
					}
					else
						_useBrowse = value;
				}
			}
		}
		
		// Filter
		protected string _filter = String.Empty;
		/// <summary> A D4 restriction filter expression to limit the data set. </summary>
		/// <remarks> The filter is a D4 expression returning a true if the row is to be included or false otherwise. </remarks>
		[DefaultValue("")]
		[Category("Behavior")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Description("A D4 restriction filter expression to limit the data set.")]
		public string Filter
		{
			get { return _filter; }
			set
			{
				if (Active)
				{
					using (IRow row = RememberActive())
					{
						string oldFilter = _filter;
						_filter = (value == null ? String.Empty : value);
						try
						{
							CursorSetChanged(row, true);
						}
						catch (Exception e)
						{
							_filter = oldFilter;
							Open();
							throw;
						}
					}
				}
				else
					_filter = (value == null ? String.Empty : value);
			}
		}
		
		protected static Expression GetMasterDetailCondition(Schema.Key detailKey)
		{
			Expression condition = null;
			Expression equalExpression;
			for (int index = 0; index < detailKey.Columns.Count; index++)
			{
				equalExpression = new BinaryExpression
				(
					new IdentifierExpression(detailKey.Columns[index].Name), 
					Instructions.Equal, 
					new IdentifierExpression(GetParameterName(detailKey.Columns[index].Name))
				);

				if (condition == null)
					condition = equalExpression;
				else
					condition = new BinaryExpression(condition, Instructions.And, equalExpression);
			}
			return condition;
		}
		
		protected static Expression MergeRestrictCondition(Expression expression, Expression condition)
		{
			RestrictExpression restrictExpression;
			if (expression is RestrictExpression)
				restrictExpression = (RestrictExpression)expression;
			else
			{
				restrictExpression = new RestrictExpression();
				restrictExpression.Expression = expression;
			}
			if (restrictExpression.Condition == null)
				restrictExpression.Condition = condition;
			else
				restrictExpression.Condition = new BinaryExpression(restrictExpression.Condition, Instructions.And, condition);
			return restrictExpression;
		}

		// Returns a D4 syntax tree for the base user expression
		protected virtual Expression GetSeedExpression()
		{
			Expression expression = _parser.ParseCursorDefinition(_expression);

			if (expression is CursorDefinition)
				expression = ((CursorDefinition)expression).Expression;

			return expression;			
		}
		
		protected override string InternalGetExpression()
		{
			Expression expression = GetSeedExpression();

			OrderExpression saveOrderExpression = expression as OrderExpression;
			BrowseExpression saveBrowseExpression = expression as BrowseExpression;
			
			if (saveOrderExpression != null)
				expression = saveOrderExpression.Expression;
				
			if (saveBrowseExpression != null)
				expression = saveBrowseExpression.Expression;
			
			// Eat irrelevant browse and order operators			
			OrderExpression orderExpression = null;
			BrowseExpression browseExpression = null;
			while (((orderExpression = expression as OrderExpression) != null) || ((browseExpression = expression as BrowseExpression) != null))
			{
				if (orderExpression != null)
				{
					expression = orderExpression.Expression;
					orderExpression = null;
				}
				
				if (browseExpression != null)
				{
					expression = browseExpression.Expression;
					browseExpression = null;
				}
			}
			
			if (IsMasterSetup() && _writeWhereClause)
			{
				expression = MergeRestrictCondition
				(
					expression, 
					GetMasterDetailCondition(DetailKey)
				);
			}
			
			if (_filter != String.Empty)
			{
				expression = MergeRestrictCondition
				(
					expression, 
					_parser.ParseExpression(_filter)
				);
			}
			
			if (IsAdorned())
			{
				AdornExpression adornExpression = new AdornExpression();
				adornExpression.Expression = expression;
				adornExpression.Expressions.AddRange(_columns);
				adornExpression.Constraints.AddRange(_constraints);
				adornExpression.Orders.AddRange(_orders);
				adornExpression.Keys.AddRange(_keys);
				expression = adornExpression;
			}
			
			if (_orderDefinition != null)
			{
				if (_useBrowse)
				{
					browseExpression = new Language.D4.BrowseExpression();
					browseExpression.Expression = expression;
					browseExpression.Columns.AddRange(_orderDefinition.Columns);
					expression = browseExpression;
				}
				else
				{
					orderExpression = new Language.D4.OrderExpression();
					orderExpression.Expression = expression;
					orderExpression.Columns.AddRange(_orderDefinition.Columns);
					expression = orderExpression;
				}					
			}
			else if (_order != null)
			{
				if (_useBrowse)
				{
					browseExpression = new BrowseExpression();
					foreach (Schema.OrderColumn column in _order.Columns)
						if (column.IsDefaultSort)
							browseExpression.Columns.Add(new OrderColumnDefinition(column.Column.Name, column.Ascending, column.IncludeNils));
						else
							browseExpression.Columns.Add(new OrderColumnDefinition(column.Column.Name, column.Ascending, column.IncludeNils, column.Sort.EmitDefinition(EmitMode.ForCopy)));
					browseExpression.Expression = expression;
					expression = browseExpression;
				}
				else
				{
					orderExpression = new OrderExpression();
					foreach (Schema.OrderColumn column in _order.Columns)
						if (column.IsDefaultSort)
							orderExpression.Columns.Add(new OrderColumnDefinition(column.Column.Name, column.Ascending, column.IncludeNils));
						else
							orderExpression.Columns.Add(new OrderColumnDefinition(column.Column.Name, column.Ascending, column.IncludeNils, column.Sort.EmitDefinition(EmitMode.ForCopy)));
					orderExpression.Expression = expression;
					expression = orderExpression;
				}
			}
			else
			{
				if (saveOrderExpression != null)
				{
					saveOrderExpression.Expression = expression;
					expression = saveOrderExpression;
				}
				else if (saveBrowseExpression != null)
				{
					saveBrowseExpression.Expression = expression;
					expression = saveBrowseExpression;
				}
			}
			
			CursorDefinition cursorExpression = new CursorDefinition(expression);
			cursorExpression.Isolation = _requestedIsolation;
			cursorExpression.Capabilities = _requestedCapabilities;
			cursorExpression.SpecifiesType = true;
			cursorExpression.CursorType = _cursorType;
			
			return new D4TextEmitter().Emit(cursorExpression);
		}

		#endregion
		
		#region Connection
		
		// BeginScript
		private string _beginScript = String.Empty;
		/// <summary> A D4 script that will be run on the view's process, just prior to opening the cursor for the view. </summary>
		/// <remarks> 
		/// Because this script will execute on the view's process, it can be used to create local variables that will be accessible
		/// within the expression for the view. For example, creating a local table variable can allow a 'scratchpad' type table for use in
		/// building query-by-example forms.
		/// </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 script that will be run on the view's process, just prior to opening the cursor for the view.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string BeginScript
		{
			get { return _beginScript; }
			set { _beginScript = value == null ? String.Empty : value; }
		}
		
		// EndScript
		private string _endScript = String.Empty;
		/// <summary> A D4 script that will be run on the view's process, just after closing the cursor for the view. </summary>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 script that will be run on the view's process, just after closing the cursor for the view.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string EndScript
		{
			get { return _endScript; }
			set { _endScript = value == null ? String.Empty : value; }
		}
		
		protected void ExecuteBeginScript()
		{
			if (_beginScript != String.Empty)
				_process.ExecuteScript(_beginScript);
		}
		
		protected void ExecuteEndScript()
		{
			if (_endScript != String.Empty)
				_process.ExecuteScript(_endScript);
		}

		protected override void InternalConnect()
		{
			base.InternalConnect();
			ExecuteBeginScript();
		}

		protected override void InternalDisconnect()
		{
			ExecuteEndScript();
			base.InternalDisconnect();
		}
		
		#endregion
		
		#region Open
		
		private DataSetState _openState = DataSetState.Browse;

		protected override bool ShouldFetchAtOpen()
		{
			return base.ShouldFetchAtOpen() && (_openState != DataSetState.Insert);
		}

		protected override void InternalOpen()
		{
			if ((_openState != DataSetState.Browse) && UseApplicationTransactions && !IsApplicationTransactionClient && (_applicationTransactionID == Guid.Empty))
				_applicationTransactionID = BeginApplicationTransaction(_openState == DataSetState.Insert);

			if (IsApplicationTransactionClient)
				JoinApplicationTransaction(_openState == DataSetState.Insert);
				
			try
			{
				base.InternalOpen();
			}
			catch
			{
				try
				{
					if (IsApplicationTransactionServer)
					{
						try
						{
							RollbackApplicationTransaction();
						}
						finally
						{
							UnprepareApplicationTransactionServer();
						}
					}
					else if (IsApplicationTransactionClient && _isJoined)
						LeaveApplicationTransaction();
				}
				catch
				{
					// ignore errors here
				}

				throw;
			}
		}
		
		/// <summary> Activates the DataView, with the specification of the initial state. </summary>
		/// <remarks> If the DataView is already active, then this method has no effect. </remarks>
		public void Open(DataSetState openState)
		{
			if (State == DataSetState.Inactive)
			{
				_openState = openState == DataSetState.Inactive ? DataSetState.Browse : openState;
				Open();
				
				try
				{
					switch (_openState)
					{
						case DataSetState.Insert: Insert(); break;
						case DataSetState.Edit: Edit(); break;
					}
				}
				catch
				{
					try
					{
						Close();
					}
					catch
					{
						// Ignore close errors here
					}

					throw;
				}
			}
		}

		#endregion

		#region Close

		protected override void InternalClose()
		{
			try
			{
				base.InternalClose();
			}
			finally
			{
				if (IsApplicationTransactionServer)
				{
					try
					{
						RollbackApplicationTransaction();
					}
					finally
					{
						UnprepareApplicationTransactionServer();
					}
				}
				else if (IsApplicationTransactionClient && _isJoined)
					LeaveApplicationTransaction();
			}
		}

		#endregion
		
		#region Application Transactions

		/// <summary> Specifies whether or not to start or enlist in application transactions. </summary>
		/// <remarks> 
		///		When true then going into Insert or Edit will start an application transaction or enlist 
		///		in an existing one if this DataView is detailed to another DataView. 
		///	</remarks>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Specifies whether or not to start or enlist in application transactions.")]
		public bool UseApplicationTransactions { get; set; }
		
		private EnlistMode _shouldEnlist;
		/// <summary> Specifies whether or not to enlist in the application transaction of the master DataView. </summary>
		[DefaultValue(EnlistMode.Default)]
		[Category("Behavior")]
		[Description("Specifies whether or not to enlist in the application transaction of the master data view")]
		public EnlistMode ShouldEnlist
		{
			get { return _shouldEnlist; }
			set { _shouldEnlist = value; }
		}
		
		private Guid _applicationTransactionID = Guid.Empty;
		/// <summary> The ID of the application transaction if this DataView started one. </summary>
		[Browsable(false)]
		public Guid ApplicationTransactionID { get { return _applicationTransactionID; } }

		/// <summary> Returns the DataView that is started the application transaction that this DataView participating in. </summary>
		/// <remarks> This may return this DataView if it was the one that started the application transaction. </remarks>
		[Browsable(false)]
		public DataView ApplicationTransactionServer 
		{ 
			get 
			{
				// return the highest level view in the link tree that is in insert/edit mode
				DataView applicationTransactionServer = null;
				if (UseApplicationTransactions)
				{
					if (ApplicationTransactionID != Guid.Empty)
						applicationTransactionServer = this;
					else
					{
						if (IsMasterValid())
						{
							DataView dataView = MasterSource.DataSet as DataView;
							if (dataView != null)
							{
								switch (_shouldEnlist)
								{
									case EnlistMode.Default :
										bool isSuperset = false;
										foreach (Schema.Key key in dataView.TableVar.Keys)
										{
											Schema.Key masterKey = new Schema.Key();
											masterKey.Columns.AddRange(key.Columns);
											if (dataView.IsMasterSetup() && dataView.IsMasterEnlisted())
												foreach (Schema.TableVarColumn keyColumn in dataView.DetailKey.Columns)
													if (!masterKey.Columns.Contains(keyColumn))
														masterKey.Columns.Add(keyColumn);
											if (MasterKey.Columns.IsSubsetOf(masterKey.Columns) || MasterKey.Columns.IsSupersetOf(masterKey.Columns))
											{
												isSuperset = true;
												break;
											}
										}
											
										if (isSuperset)
											applicationTransactionServer = dataView.ApplicationTransactionServer;
									break;
									
									case EnlistMode.True :
										applicationTransactionServer = dataView.ApplicationTransactionServer;
									break;
								}
							}
						}
					}
				}
				return applicationTransactionServer;
			} 
		}
		
		private bool IsApplicationTransactionServer { get { return (ApplicationTransactionServer == this); } }
		
		private bool IsApplicationTransactionClient { get { return (ApplicationTransactionServer != null) && (ApplicationTransactionServer != this); } }
		
		private Guid BeginApplicationTransaction(bool isInsert)
		{
			Guid aTID = _process.BeginApplicationTransaction(true, isInsert);
			_isJoined = true;
			return aTID;
		}
		
		private void JoinApplicationTransaction(bool isInsert)
		{
			_process.JoinApplicationTransaction(ApplicationTransactionServer.ApplicationTransactionID, isInsert);
			_isJoined = true;
		}
		
		private void LeaveApplicationTransaction()
		{
			_process.LeaveApplicationTransaction();
			_isJoined = false;
		}
		
		private void PrepareApplicationTransaction()
		{
			_process.PrepareApplicationTransaction(_applicationTransactionID);
		}
		
		private void CommitApplicationTransaction()
		{
			_process.CommitApplicationTransaction(_applicationTransactionID);
			_isJoined = false;
		}

		private void RollbackApplicationTransaction()
		{
			_process.RollbackApplicationTransaction(_applicationTransactionID);
			_isJoined = false;
		}
		
		private bool _isJoined;

		private void PrepareApplicationTransactionServer(bool isInsert)
		{
			if (!IsApplicationTransactionClient && UseApplicationTransactions && (_openState == DataSetState.Browse))
			{
				using (IRow row = RememberActive())
				{
					if (_applicationTransactionID == Guid.Empty)
						_applicationTransactionID = BeginApplicationTransaction(isInsert);
						
					_aTCursor = new DAECursor(_process);
					try
					{
						_aTCursor.OnErrors += new CursorErrorsOccurredHandler(CursorOnErrors);
						_aTCursor.Expression = InternalGetExpression();
						// Copy the params, but only if they are not already pushed on the process through the FCursor
						if ((_cursor == null) || !_cursor.ShouldOpen)
							_aTCursor.Params.AddRange(_cursor.Params);
						_aTCursor.Prepare();
						try
						{
							SetParamValues();
							_aTCursor.ShouldOpen = ShouldOpenCursor();
							_aTCursor.Open();
							try
							{
								if (!isInsert && (row != null))
									if (!_aTCursor.FindKey(row))
										throw new ClientException(ClientException.Codes.RecordNotFound);
							}
							catch
							{
								_aTCursor.Close();
								throw;
							}
						}
						catch
						{
							_aTCursor.Unprepare();
							throw;
						}
					}
					catch
					{
						_aTCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
						_aTCursor.Dispose();
						_aTCursor = null;
						throw;
					}
				}
			}
		}
		
		private void UnprepareApplicationTransactionServer()
		{
			Guid iD = _applicationTransactionID;
			_applicationTransactionID = Guid.Empty;
			if (_openState == DataSetState.Browse)
			{
				if (_aTCursor != null)
				{
					_aTCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
					_aTCursor.Dispose();
					_aTCursor = null;
				}
			}
			else
				CloseCursor();
		}
		
		#endregion
		
		#region Modification

		/// <summary> Throws an exception if the DataSet cannot be edited. </summary>
		/// <remarks> This is tied to the ReadOnly property unless a custom insert, update, or delete statement has been provided. </remarks>
		public override void CheckCanModify()
		{
			if (InternalIsReadOnly && (_insertStatement == String.Empty) && (_updateStatement == String.Empty) && (_deleteStatement == String.Empty))
				throw new ClientException(ClientException.Codes.IsReadOnly);
		}
		
		// InsertStatement
		private string _insertStatement = String.Empty;
		/// <summary> A D4 statement that will be used to insert any new rows. </summary>
		/// <remarks> If no statement is specified, the insert will be performed through the cursor to the Dataphor server.  The new columns are accessible as parameters by their names, qualified by "New.". </remarks>
		[DefaultValue("")]														   
		[Category("Data")]
		[Description("A D4 statement that will be used to insert any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string InsertStatement
		{
			get { return _insertStatement; }
			set { _insertStatement = value == null ? String.Empty : value; }
		}
		
		// UpdateStatement
		private string _updateStatement = String.Empty;
		/// <summary> A D4 statement that will be used to update any new rows. </summary>
		/// <remarks> If no statement is specified, the update will be performed through the cursor to the Dataphor server.  The new and old columns are accessible as parameters by their names, qualified by "New." and "Old." respectively. </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 statement that will be used to update any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string UpdateStatement
		{
			get { return _updateStatement; }
			set { _updateStatement = value == null ? String.Empty : value; }
		}
		
		// DeleteStatement
		private string _deleteStatement = String.Empty;
		/// <summary> A D4 statement that will be used to delete any new rows. </summary>
		/// <remarks> If no statement is specified, the delete will be performed through the cursor to the Dataphor server.  The old columns are accessible as parameters by their names, qualified by "Old.". </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 statement that will be used to delete any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string DeleteStatement
		{
			get { return _deleteStatement; }
			set { _deleteStatement = value == null ? String.Empty : value; }
		}
		
		protected override void InternalEdit()
		{
			try
			{
				PrepareApplicationTransactionServer(false);
			}
			catch
			{
				Refresh();
				throw;
			}
		}

		protected override void InternalInsertAppend()
		{
			try
			{
				PrepareApplicationTransactionServer(true);
			}
			catch
			{
				Refresh();
				throw;
			}
		}
		
		protected override void InternalCancel()
		{
			if (IsApplicationTransactionServer)
			{
				RollbackApplicationTransaction();
				UnprepareApplicationTransactionServer();
			}
		}
		
		protected override void InternalValidate(bool isPosting)
		{
			base.InternalValidate(isPosting);

			if (!isPosting)			
				GetEditCursor().Validate(null, _buffer[_activeOffset].Row, String.Empty);
		}
		
		protected override void PrepareTransaction()
		{
			if (IsApplicationTransactionServer)
				PrepareApplicationTransaction();
			base.PrepareTransaction();
		}
		
		protected override void CommitTransaction()
		{
			base.CommitTransaction();
			if (IsApplicationTransactionServer)
				CommitApplicationTransaction();
		}

		protected override bool UpdatesThroughCursor()
		{
			return 
				!IsApplicationTransactionServer && 
				(
					((State == DataSetState.Insert) && (_insertStatement == String.Empty)) || 
					((State == DataSetState.Edit) && (_updateStatement == String.Empty))
				);
		}
		
		protected override void InternalPost(IRow row)
		{
			base.InternalPost(row);
			
			bool shouldRefresh = !UpdatesThroughCursor() && RefreshAfterPost;
			
			if (IsApplicationTransactionServer)
				UnprepareApplicationTransactionServer();

			// Refresh the main cursor to the newly inserted application transaction row
			if (shouldRefresh)
				InternalRefresh(row);
		}

		private DAE.Runtime.DataParams GetParamsFromRow(IRow row, string prefix)
		{
			DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
			GetParamsFromRow(row, paramsValue, prefix);
			return paramsValue;
		}

		private static void GetParamsFromRow(IRow row, DAE.Runtime.DataParams LParams, string prefix)
		{
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				LParams.Add(new DAE.Runtime.DataParam(prefix + row.DataType.Columns[index].Name, row.DataType.Columns[index].DataType, Modifier.In, row[index]));
		}
		
		protected override void InternalInsert(IRow row)
		{
			if (_insertStatement == String.Empty)
				base.InternalInsert(row);
			else
			{
				DAE.Runtime.DataParams paramsValue = GetParamsFromRow(_buffer[_activeOffset].Row, "New.");
				IServerStatementPlan plan = _process.PrepareStatement(_insertStatement, paramsValue);
				try
				{
					plan.Execute(paramsValue);
				}
				finally
				{
					_process.UnprepareStatement(plan);
				}
			}
		}
		
		protected override void InternalUpdate(IRow row)
		{
			if (_updateStatement == String.Empty)
				base.InternalUpdate(row);
			else
			{
				DAE.Runtime.DataParams paramsValue = GetParamsFromRow(_originalRow, "Old.");
				GetParamsFromRow(row, paramsValue, "New.");
				IServerStatementPlan plan = _process.PrepareStatement(_updateStatement, paramsValue);
				try
				{
					plan.Execute(paramsValue);
				}
				finally
				{
					_process.UnprepareStatement(plan);
				}
			}
		}
		
		protected override void InternalDelete()
		{
			if (_deleteStatement == String.Empty)
				base.InternalDelete();
			else
			{
				DAE.Runtime.DataParams paramsValue = GetParamsFromRow(_buffer[_activeOffset].Row, "Old.");
				IServerStatementPlan plan = _process.PrepareStatement(_deleteStatement, paramsValue);
				try
				{
					plan.Execute(paramsValue);
				}
				finally
				{
					_process.UnprepareStatement(plan);
				}
			}
		}
		
		#endregion

		#region Utility

		private DataSource _dataSource = null;
		/// <summary> Utility DataSource that is linked to this DataView. </summary>
		/// <remarks> 
		///		This DataSource is created, attached and disposed by this DataView.  This is useful when programmatically 
		///		accessing a DataView to avoid having to create attach and deallocate a DataSource. 
		///	</remarks>
		[Browsable(false)]
		public DataSource DataSource
		{
			get
			{
				if (_dataSource == null)
				{
					_dataSource = new DataSource();
					_dataSource.DataSet = this;
				}
				return _dataSource;
			}
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		public DataView OpenDetail(string expression, string masterKeyNames, string detailKeyNames, DataSetState initialState, bool isReadOnly)
		{
			DataView dataView = new DataView();
			try
			{
				dataView.Session = this.Session;
				dataView.Expression = expression;
				dataView.IsReadOnly = isReadOnly;
				dataView.MasterSource = DataSource;
				dataView.MasterKeyNames = masterKeyNames;
				dataView.DetailKeyNames = detailKeyNames;
				dataView.Open(initialState);
				return dataView;
			}
			catch
			{
				dataView.Dispose();
				throw;
			}
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The ReadOnly property for this overload will be false (read/write). </remarks>
		public DataView OpenDetail(string expression, string masterKeyNames, string detailKeyNames, DataSetState initialState)
		{
			return OpenDetail(expression, masterKeyNames, detailKeyNames, initialState, false);
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The OpenState will be Browse. </remarks>
		public DataView OpenDetail(string expression, string masterKeyNames, string detailKeyNames, bool readOnly)
		{
			return OpenDetail(expression, masterKeyNames, detailKeyNames, DataSetState.Browse, readOnly);
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The OpenState will be Browse and the ReadOnly property for this overload will be false (read/write). </remarks>
		public DataView OpenDetail(string expression, string masterKeyNames, string detailKeyNames)
		{
			return OpenDetail(expression, masterKeyNames, detailKeyNames, DataSetState.Browse, false);
		}

		#endregion
	}

	public enum EnlistMode { Default, True, False }
}

