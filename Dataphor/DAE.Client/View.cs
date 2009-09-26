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
	[DesignerSerializer(typeof(Alphora.Dataphor.DAE.Client.Design.ActiveLastSerializer), typeof(CodeDomSerializer))]
	public class DataView : TableDataSet
	{
		public DataView()
		{
			InternalUseBrowse = true;
			InternalWriteWhereClause = true;
			UseApplicationTransactions = true;
			FShouldEnlist = EnlistMode.Default;
		}

		public DataView(IContainer AContainer) : this()
		{
			if (AContainer != null)
				AContainer.Add(this);
		}

		protected override void InternalDispose(bool ADisposing)
		{
			try
			{
				base.InternalDispose(ADisposing);
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

		#region Cursor

		// Cursor
		// The A/T Cursor is only used if this is an A/T server opened in Browse Mode.
		// In all other cases, the normal cursor will already be participating in the A/T.
		// This new cursor is only used for update and proposable calls. Once the update
		// is complete, if an A/T cursor was being used, the main cursor must be refreshed
		// to the newly inserted/updated row. This is the purpose of the UpdatesThroughCursor()
		// call, which is overridden by this component to return false if this is an A/T server,
		// or if custom insert/update/delete statements are provided.
		private DAECursor FATCursor;

		// This call is used to return the cursor that should be used to perform update
		// and proposable calls.		
		protected override DAECursor GetEditCursor()
		{
			if (IsApplicationTransactionServer && (FOpenState == DataSetState.Browse))
				return FATCursor;
			else
				return FCursor;
		}

		#endregion
		
		#region Master / Detail
		
		protected override void MasterRowChanged(DataLink ALInk, DataSet ADataSet, DataField AField)
		{
			if (Active)
			{
				bool LChanged = ADataSet.IsEmpty() != IsEmpty();
				if (!LChanged && !IsEmpty())
					for (int i = 0; i < MasterKey.Columns.Count; i++)
					{
						string LMasterColumn = MasterKey.Columns[i].Name;
						string LDetailColumn = DetailKey.Columns[i].Name;
						LChanged = !FFields.Contains(LDetailColumn) || (ADataSet[LMasterColumn].IsNil != Fields[LDetailColumn].IsNil);
						if (LChanged)
							break;
						if (!Fields[LDetailColumn].IsNil)
						{
							object LMasterValue = ADataSet[LMasterColumn].AsNative;
							object LDetailValue = Fields[LDetailColumn].AsNative;
							if (LMasterValue is IComparable)
								LChanged = ((IComparable)LMasterValue).CompareTo(LDetailValue) != 0;
							else
								LChanged = !(LMasterValue.Equals(LDetailValue));
							if (LChanged)
								break;
						}
					}
				if (LChanged)
					CursorSetChanged(null, IsApplicationTransactionClient && !IsJoined);
			}
		}
		
		/// <summary>Returns true if the master is enlisted in an A/T. Cannot be invoked without a master source.</summary>
		public bool IsMasterEnlisted()
		{
			DataView LDataView = MasterSource.DataSet as DataView;
			return (LDataView != null) && (LDataView.IsApplicationTransactionClient || LDataView.IsApplicationTransactionServer);
		}

		/// <summary> Returns true if the master is set up (see IsMasterSetup()), and there is a value for each of the master's columns (or WriteWhereClause is true). </summary>
		public override bool IsMasterValid()
		{
			if (IsMasterSetup())
			{
				TableDataSet LDataSet = MasterSource.DataSet as TableDataSet;
				bool LIsMasterValid = (LDataSet == null) || (!LDataSet.IsDetail() || LDataSet.IsMasterValid());
				if (LIsMasterValid && !MasterSource.DataSet.IsEmpty())
				{
					if (!WriteWhereClause) // If the where clause is custom, allow nil master values
						return true;
						
					foreach (DAE.Schema.TableVarColumn LColumn in MasterKey.Columns)
						if (!(MasterSource.DataSet.Fields[LColumn.Name].HasValue()))
							return false;
					return true;
				}
			}
			return false;
		}
		
		protected override bool InternalColumnChanging(DataField AField, Row AOldRow, Row ANewRow)
		{
			base.InternalColumnChanging(AField, AOldRow, ANewRow);
			if (GetEditCursor().Validate(AOldRow, ANewRow, AField.ColumnName))
			{
				FValueFlags.SetAll(true);
				return true;
			}
			return false;
		}

		protected override bool InternalColumnChanged(DataField AField, Row AOldRow, Row ANewRow)
		{
			if (GetEditCursor().Change(AOldRow, ANewRow, AField.ColumnName))
			{
				FValueFlags.SetAll(true);
				return base.InternalColumnChanged(null, AOldRow, ANewRow);
			}
			else
				return base.InternalColumnChanged(AField, AOldRow, ANewRow);
		}
		
		protected override void InternalChangeColumn(DataField AField, Row AOldRow, Row ANewRow)
		{
			Process.BeginTransaction(IsolationLevel);
			try
			{
				base.InternalChangeColumn(AField, AOldRow, ANewRow);

				Process.CommitTransaction();
			}
			catch
			{
				Process.RollbackTransaction();
				throw;
			}
		}

		protected override void InternalDefault(Row ARow)
		{
			base.InternalDefault(ARow);
			if (GetEditCursor().Default(ARow, String.Empty))
				FValueFlags.SetAll(true);
		}
		
		#endregion

		#region Adorn Expression

		private AdornColumnExpressions FColumns = new AdornColumnExpressions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AdornColumnExpressions Columns { get { return FColumns; } }
		
		private CreateConstraintDefinitions FConstraints = new CreateConstraintDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CreateConstraintDefinitions Constraints { get { return FConstraints; } }
		
		private OrderDefinitions FOrders = new OrderDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OrderDefinitions Orders { get { return FOrders; } }

		private KeyDefinitions FKeys = new KeyDefinitions();
		[Category("Definitions")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public KeyDefinitions Keys { get { return FKeys; } }

		private string AdornExpressionToString()
		{
			if (IsAdorned())
			{
				AdornExpression LAdornExpression = new AdornExpression();
				LAdornExpression.Expression = new IdentifierExpression("A");
				LAdornExpression.Expressions.AddRange(FColumns);
				LAdornExpression.Constraints.AddRange(FConstraints);
				LAdornExpression.Orders.AddRange(FOrders);
				LAdornExpression.Keys.AddRange(FKeys);
				return new D4TextEmitter().Emit(LAdornExpression);
			}
			else
				return String.Empty;
		}

		private void StringToAdornExpression(string AValue)
		{
			if (AValue == String.Empty)
			{
				FOrders.Clear();
				FConstraints.Clear();
				FColumns.Clear();
			}
			else
			{
				Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				AdornExpression LExpression = (AdornExpression)LParser.ParseExpression(AValue);
				FColumns = LExpression.Expressions;
				FConstraints = LExpression.Constraints;
				FOrders = LExpression.Orders;
				FKeys = LExpression.Keys;
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
				(FColumns.Count > 0) ||
				(FConstraints.Count > 0) ||
				(FOrders.Count > 0);
		}

		#endregion

		#region Expression

		// Expression
		protected string FExpression = String.Empty;
		[DefaultValue("")]
		[Category("Data")]
		[Description("Expression")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string Expression
		{
			get { return FExpression; }
			set
			{
				CheckInactive();
				FExpression = value == null ? String.Empty : value;
			}
		}
		
		protected bool InternalWriteWhereClause
		{
			get { return FFlags[WriteWhereClauseMask]; }
			set { FFlags[WriteWhereClauseMask] = value; }
		}

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
			get { return InternalWriteWhereClause; }
			set
			{
				if (InternalWriteWhereClause != value)
				{
					InternalWriteWhereClause = value;
					if (Active)
						CursorSetChanged(null, true);
				}
			}
		}

		protected bool InternalUseBrowse
		{
			get { return FFlags[UseBrowseMask]; }
			set { FFlags[UseBrowseMask] = value; }
		}

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
			get { return InternalUseBrowse; }
			set 
			{
				if (InternalUseBrowse != value)
				{
					if (Active)
					{
						using (Row LRow = RememberActive())
						{
							InternalUseBrowse = value; 
							CursorSetChanged(LRow, true);
						}
					}
					else
						InternalUseBrowse = value;
				}
			}
		}
		
		// Filter
		protected string FFilter = String.Empty;
		/// <summary> A D4 restriction filter expression to limit the data set. </summary>
		/// <remarks> The filter is a D4 expression returning a true if the row is to be included or false otherwise. </remarks>
		[DefaultValue("")]
		[Category("Behavior")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Description("A D4 restriction filter expression to limit the data set.")]
		public string Filter
		{
			get { return FFilter; }
			set
			{
				if (Active)
				{
					using (Row LRow = RememberActive())
					{
						string LOldFilter = FFilter;
						FFilter = (value == null ? String.Empty : value);
						try
						{
							CursorSetChanged(LRow, true);
						}
						catch
						{
							FFilter = LOldFilter;
							Open();
							throw;
						}
					}
				}
				else
					FFilter = (value == null ? String.Empty : value);
			}
		}
		
		protected static Expression GetMasterDetailCondition(Schema.Key ADetailKey)
		{
			Expression LCondition = null;
			Expression LEqualExpression;
			for (int LIndex = 0; LIndex < ADetailKey.Columns.Count; LIndex++)
			{
				LEqualExpression = new BinaryExpression
				(
					new IdentifierExpression(ADetailKey.Columns[LIndex].Name), 
					Instructions.Equal, 
					new IdentifierExpression(GetParameterName(ADetailKey.Columns[LIndex].Name))
				);

				if (LCondition == null)
					LCondition = LEqualExpression;
				else
					LCondition = new BinaryExpression(LCondition, Instructions.And, LEqualExpression);
			}
			return LCondition;
		}
		
		protected static Expression MergeRestrictCondition(Expression AExpression, Expression ACondition)
		{
			RestrictExpression LRestrictExpression;
			if (AExpression is RestrictExpression)
				LRestrictExpression = (RestrictExpression)AExpression;
			else
			{
				LRestrictExpression = new RestrictExpression();
				LRestrictExpression.Expression = AExpression;
			}
			if (LRestrictExpression.Condition == null)
				LRestrictExpression.Condition = ACondition;
			else
				LRestrictExpression.Condition = new BinaryExpression(LRestrictExpression.Condition, Instructions.And, ACondition);
			return LRestrictExpression;
		}

		// Returns a D4 syntax tree for the base user expression
		protected virtual Expression GetSeedExpression()
		{
			Expression LExpression = FParser.ParseCursorDefinition(FExpression);

			if (LExpression is CursorDefinition)
				LExpression = ((CursorDefinition)LExpression).Expression;

			return LExpression;			
		}
		
		protected override string InternalGetExpression()
		{
			Expression LExpression = GetSeedExpression();

			OrderExpression LSaveOrderExpression = LExpression as OrderExpression;
			BrowseExpression LSaveBrowseExpression = LExpression as BrowseExpression;
			
			if (LSaveOrderExpression != null)
				LExpression = LSaveOrderExpression.Expression;
				
			if (LSaveBrowseExpression != null)
				LExpression = LSaveBrowseExpression.Expression;
			
			// Eat irrelevant browse and order operators			
			OrderExpression LOrderExpression = null;
			BrowseExpression LBrowseExpression = null;
			while (((LOrderExpression = LExpression as OrderExpression) != null) || ((LBrowseExpression = LExpression as BrowseExpression) != null))
			{
				if (LOrderExpression != null)
				{
					LExpression = LOrderExpression.Expression;
					LOrderExpression = null;
				}
				
				if (LBrowseExpression != null)
				{
					LExpression = LBrowseExpression.Expression;
					LBrowseExpression = null;
				}
			}
			
			if (IsMasterSetup() && InternalWriteWhereClause)
			{
				LExpression = MergeRestrictCondition
				(
					LExpression, 
					GetMasterDetailCondition(DetailKey)
				);
			}
			
			if (FFilter != String.Empty)
			{
				LExpression = MergeRestrictCondition
				(
					LExpression, 
					FParser.ParseExpression(FFilter)
				);
			}
			
			if (IsAdorned())
			{
				AdornExpression LAdornExpression = new AdornExpression();
				LAdornExpression.Expression = LExpression;
				LAdornExpression.Expressions.AddRange(FColumns);
				LAdornExpression.Constraints.AddRange(FConstraints);
				LAdornExpression.Orders.AddRange(FOrders);
				LAdornExpression.Keys.AddRange(FKeys);
				LExpression = LAdornExpression;
			}
			
			if (FOrderDefinition != null)
			{
				if (InternalUseBrowse)
				{
					LBrowseExpression = new Language.D4.BrowseExpression();
					LBrowseExpression.Expression = LExpression;
					LBrowseExpression.Columns.AddRange(FOrderDefinition.Columns);
					LExpression = LBrowseExpression;
				}
				else
				{
					LOrderExpression = new Language.D4.OrderExpression();
					LOrderExpression.Expression = LExpression;
					LOrderExpression.Columns.AddRange(FOrderDefinition.Columns);
					LExpression = LOrderExpression;
				}					
			}
			else if (FOrder != null)
			{
				if (InternalUseBrowse)
				{
					LBrowseExpression = new BrowseExpression();
					foreach (Schema.OrderColumn LColumn in FOrder.Columns)
						if (LColumn.IsDefaultSort)
							LBrowseExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils));
						else
							LBrowseExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils, LColumn.Sort.EmitDefinition(EmitMode.ForCopy)));
					LBrowseExpression.Expression = LExpression;
					LExpression = LBrowseExpression;
				}
				else
				{
					LOrderExpression = new OrderExpression();
					foreach (Schema.OrderColumn LColumn in FOrder.Columns)
						if (LColumn.IsDefaultSort)
							LOrderExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils));
						else
							LOrderExpression.Columns.Add(new OrderColumnDefinition(LColumn.Column.Name, LColumn.Ascending, LColumn.IncludeNils, LColumn.Sort.EmitDefinition(EmitMode.ForCopy)));
					LOrderExpression.Expression = LExpression;
					LExpression = LOrderExpression;
				}
			}
			else
			{
				if (LSaveOrderExpression != null)
				{
					LSaveOrderExpression.Expression = LExpression;
					LExpression = LSaveOrderExpression;
				}
				else if (LSaveBrowseExpression != null)
				{
					LSaveBrowseExpression.Expression = LExpression;
					LExpression = LSaveBrowseExpression;
				}
			}
			
			CursorDefinition LCursorExpression = new CursorDefinition(LExpression);
			LCursorExpression.Isolation = FRequestedIsolation;
			LCursorExpression.Capabilities = FRequestedCapabilities;
			LCursorExpression.SpecifiesType = true;
			LCursorExpression.CursorType = FCursorType;
			
			return new D4TextEmitter().Emit(LCursorExpression);
		}

		#endregion
		
		#region Connection
		
		// BeginScript
		private string FBeginScript = String.Empty;
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
			get { return FBeginScript; }
			set { FBeginScript = value == null ? String.Empty : value; }
		}
		
		// EndScript
		private string FEndScript = String.Empty;
		/// <summary> A D4 script that will be run on the view's process, just after closing the cursor for the view. </summary>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 script that will be run on the view's process, just after closing the cursor for the view.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string EndScript
		{
			get { return FEndScript; }
			set { FEndScript = value == null ? String.Empty : value; }
		}
		
		protected void ExecuteBeginScript()
		{
			if (FBeginScript != String.Empty)
				FProcess.ExecuteScript(FBeginScript);
		}
		
		protected void ExecuteEndScript()
		{
			if (FEndScript != String.Empty)
				FProcess.ExecuteScript(FEndScript);
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
		
		private DataSetState FOpenState = DataSetState.Browse;

		protected override bool ShouldFetchAtOpen()
		{
			return base.ShouldFetchAtOpen() && (FOpenState != DataSetState.Insert);
		}

		protected override void InternalOpen()
		{
			if ((FOpenState != DataSetState.Browse) && UseApplicationTransactions && !IsApplicationTransactionClient && (FApplicationTransactionID == Guid.Empty))
				FApplicationTransactionID = BeginApplicationTransaction(FOpenState == DataSetState.Insert);

			if (IsApplicationTransactionClient)
				JoinApplicationTransaction(FOpenState == DataSetState.Insert);
				
			try
			{
				base.InternalOpen();
			}
			catch
			{
				if (IsApplicationTransactionServer)
				{
					RollbackApplicationTransaction();
					UnprepareApplicationTransactionServer();
				}
				else if (IsApplicationTransactionClient && IsJoined)
					LeaveApplicationTransaction();
				throw;
			}
		}
		
		/// <summary> Activates the DataView, with the specification of the initial state. </summary>
		/// <remarks> If the DataView is already active, then this method has no effect. </remarks>
		public void Open(DataSetState AOpenState)
		{
			if (State == DataSetState.Inactive)
			{
				FOpenState = AOpenState == DataSetState.Inactive ? DataSetState.Browse : AOpenState;
				Open();
				
				try
				{
					switch (FOpenState)
					{
						case DataSetState.Insert: Insert(); break;
						case DataSetState.Edit: Edit(); break;
					}
				}
				catch
				{
					Close();
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
					RollbackApplicationTransaction();
					UnprepareApplicationTransactionServer();
				}
				else if (IsApplicationTransactionClient && IsJoined)
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
		public bool UseApplicationTransactions
		{
			get { return FFlags[UseApplicationTransactionsMask]; }
			set { FFlags[UseApplicationTransactionsMask] = value; }
		}
		
		private EnlistMode FShouldEnlist;
		/// <summary> Specifies whether or not to enlist in the application transaction of the master DataView. </summary>
		[DefaultValue(EnlistMode.Default)]
		[Category("Behavior")]
		[Description("Specifies whether or not to enlist in the application transaction of the master data view")]
		public EnlistMode ShouldEnlist
		{
			get { return FShouldEnlist; }
			set { FShouldEnlist = value; }
		}
		
		private Guid FApplicationTransactionID = Guid.Empty;
		/// <summary> The ID of the application transaction if this DataView started one. </summary>
		[Browsable(false)]
		public Guid ApplicationTransactionID { get { return FApplicationTransactionID; } }

		/// <summary> Returns the DataView that is started the application transaction that this DataView participating in. </summary>
		/// <remarks> This may return this DataView if it was the one that started the application transaction. </remarks>
		[Browsable(false)]
		public DataView ApplicationTransactionServer 
		{ 
			get 
			{
				// return the highest level view in the link tree that is in insert/edit mode
				DataView LApplicationTransactionServer = null;
				if (UseApplicationTransactions)
				{
					if (ApplicationTransactionID != Guid.Empty)
						LApplicationTransactionServer = this;
					else
					{
						if (IsMasterValid())
						{
							DataView LDataView = MasterSource.DataSet as DataView;
							if (LDataView != null)
							{
								switch (FShouldEnlist)
								{
									case EnlistMode.Default :
										bool LIsSuperset = false;
										foreach (Schema.Key LKey in LDataView.TableVar.Keys)
										{
											Schema.Key LMasterKey = new Schema.Key();
											LMasterKey.Columns.AddRange(LKey.Columns);
											if (LDataView.IsMasterSetup() && LDataView.IsMasterEnlisted())
												foreach (Schema.TableVarColumn LKeyColumn in LDataView.DetailKey.Columns)
													if (!LMasterKey.Columns.Contains(LKeyColumn))
														LMasterKey.Columns.Add(LKeyColumn);
											if (MasterKey.Columns.IsSubsetOf(LMasterKey.Columns) || MasterKey.Columns.IsSupersetOf(LMasterKey.Columns))
											{
												LIsSuperset = true;
												break;
											}
										}
											
										if (LIsSuperset)
											LApplicationTransactionServer = LDataView.ApplicationTransactionServer;
									break;
									
									case EnlistMode.True :
										LApplicationTransactionServer = LDataView.ApplicationTransactionServer;
									break;
								}
							}
						}
					}
				}
				return LApplicationTransactionServer;
			} 
		}
		
		private bool IsApplicationTransactionServer { get { return (ApplicationTransactionServer == this); } }
		
		private bool IsApplicationTransactionClient { get { return (ApplicationTransactionServer != null) && (ApplicationTransactionServer != this); } }
		
		private Guid BeginApplicationTransaction(bool AIsInsert)
		{
			Guid LATID = FProcess.BeginApplicationTransaction(true, AIsInsert);
			IsJoined = true;
			return LATID;
		}
		
		private void JoinApplicationTransaction(bool AIsInsert)
		{
			FProcess.JoinApplicationTransaction(ApplicationTransactionServer.ApplicationTransactionID, AIsInsert);
			IsJoined = true;
		}
		
		private void LeaveApplicationTransaction()
		{
			FProcess.LeaveApplicationTransaction();
			IsJoined = false;
		}
		
		private void PrepareApplicationTransaction()
		{
			FProcess.PrepareApplicationTransaction(FApplicationTransactionID);
		}
		
		private void CommitApplicationTransaction()
		{
			FProcess.CommitApplicationTransaction(FApplicationTransactionID);
			IsJoined = false;
		}

		private void RollbackApplicationTransaction()
		{
			FProcess.RollbackApplicationTransaction(FApplicationTransactionID);
			IsJoined = false;
		}
		
		private bool JoinAsInsert
		{
			get { return FFlags[JoinAsInsertMask]; }
			set { FFlags[JoinAsInsertMask] = value; }
		}

		private bool IsJoined
		{
			get { return FFlags[IsJoinedMask]; }
			set { FFlags[IsJoinedMask] = value; }
		}

		private void PrepareApplicationTransactionServer(bool AIsInsert)
		{
			if (!IsApplicationTransactionClient && UseApplicationTransactions && (FOpenState == DataSetState.Browse))
			{
				using (Row LRow = RememberActive())
				{
					if (FApplicationTransactionID == Guid.Empty)
						FApplicationTransactionID = BeginApplicationTransaction(AIsInsert);
						
					FATCursor = new DAECursor(FProcess);
					try
					{
						FATCursor.OnErrors += new CursorErrorsOccurredHandler(CursorOnErrors);
						FATCursor.Expression = InternalGetExpression();
						// Copy the params, but only if they are not already pushed on the process through the FCursor
						if ((FCursor == null) || !FCursor.ShouldOpen)
							FATCursor.Params.AddRange(FCursor.Params);
						FATCursor.Prepare();
						try
						{
							SetParamValues();
							FATCursor.ShouldOpen = ShouldOpenCursor();
							FATCursor.Open();
							try
							{
								if (!AIsInsert && (LRow != null))
									if (!FATCursor.FindKey(LRow))
										throw new ClientException(ClientException.Codes.RecordNotFound);
							}
							catch
							{
								FATCursor.Close();
								throw;
							}
						}
						catch
						{
							FATCursor.Unprepare();
							throw;
						}
					}
					catch
					{
						FATCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
						FATCursor.Dispose();
						FATCursor = null;
						throw;
					}
				}
			}
		}
		
		private void UnprepareApplicationTransactionServer()
		{
			Guid LID = FApplicationTransactionID;
			FApplicationTransactionID = Guid.Empty;
			if (FOpenState == DataSetState.Browse)
			{
				if (FATCursor != null)
				{
					FATCursor.OnErrors -= new CursorErrorsOccurredHandler(CursorOnErrors);
					FATCursor.Dispose();
					FATCursor = null;
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
			if (InternalIsReadOnly && (FInsertStatement == String.Empty) && (FUpdateStatement == String.Empty) && (FDeleteStatement == String.Empty))
				throw new ClientException(ClientException.Codes.IsReadOnly);
		}
		
		// InsertStatement
		private string FInsertStatement = String.Empty;
		/// <summary> A D4 statement that will be used to insert any new rows. </summary>
		/// <remarks> If no statement is specified, the insert will be performed through the cursor to the Dataphor server. </remarks>
		[DefaultValue("")]														   
		[Category("Data")]
		[Description("A D4 statement that will be used to insert any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string InsertStatement
		{
			get { return FInsertStatement; }
			set { FInsertStatement = value == null ? String.Empty : value; }
		}
		
		// UpdateStatement
		private string FUpdateStatement = String.Empty;
		/// <summary> A D4 statement that will be used to update any new rows. </summary>
		/// <remarks> If no statement is specified, the update will be performed through the cursor to the Dataphor server. </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 statement that will be used to update any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string UpdateStatement
		{
			get { return FUpdateStatement; }
			set { FUpdateStatement = value == null ? String.Empty : value; }
		}
		
		// DeleteStatement
		private string FDeleteStatement = String.Empty;
		/// <summary> A D4 statement that will be used to delete any new rows. </summary>
		/// <remarks> If no statement is specified, the delete will be performed through the cursor to the Dataphor server. </remarks>
		[DefaultValue("")]
		[Category("Data")]
		[Description("A D4 statement that will be used to delete any new rows.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[EditorDocumentType("d4")]
		public string DeleteStatement
		{
			get { return FDeleteStatement; }
			set { FDeleteStatement = value == null ? String.Empty : value; }
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
		
		protected override void InternalValidate(bool AIsPosting)
		{
			base.InternalValidate(AIsPosting);

			if (!AIsPosting)			
				GetEditCursor().Validate(null, FBuffer[FActiveOffset].Row, String.Empty);
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
					((State == DataSetState.Insert) && (FInsertStatement == String.Empty)) || 
					((State == DataSetState.Edit) && (FUpdateStatement == String.Empty))
				);
		}
		
		protected override void InternalPost(Row ARow)
		{
			base.InternalPost(ARow);
			
			bool LShouldRefresh = !UpdatesThroughCursor() && RefreshAfterPost;
			
			if (IsApplicationTransactionServer)
				UnprepareApplicationTransactionServer();

			// Refresh the main cursor to the newly inserted application transaction row
			if (LShouldRefresh)
				InternalRefresh(ARow);
		}

		private DAE.Runtime.DataParams GetParamsFromRow(Row ARow)
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LParams.Add(new DAE.Runtime.DataParam(ARow.DataType.Columns[LIndex].Name, ARow.DataType.Columns[LIndex].DataType, Modifier.In, ARow[LIndex]));
			return LParams;
		}
		
		protected override void InternalInsert(Row ARow)
		{
			if (FInsertStatement == String.Empty)
				base.InternalInsert(ARow);
			else
			{
				DAE.Runtime.DataParams LParams = GetParamsFromRow(FBuffer[FActiveOffset].Row);
				IServerStatementPlan LPlan = FProcess.PrepareStatement(FInsertStatement, LParams);
				try
				{
					LPlan.Execute(LParams);
				}
				finally
				{
					FProcess.UnprepareStatement(LPlan);
				}
			}
		}
		
		protected override void InternalUpdate(Row ARow)
		{
			if (FUpdateStatement == String.Empty)
				base.InternalUpdate(ARow);
			else
			{
				DAE.Runtime.DataParams LParams = GetParamsFromRow(FBuffer[FActiveOffset].Row);
				IServerStatementPlan LPlan = FProcess.PrepareStatement(FUpdateStatement, LParams);
				try
				{
					LPlan.Execute(LParams);
				}
				finally
				{
					FProcess.UnprepareStatement(LPlan);
				}
			}
		}
		
		protected override void InternalDelete()
		{
			if (FDeleteStatement == String.Empty)
				base.InternalDelete();
			else
			{
				DAE.Runtime.DataParams LParams = GetParamsFromRow(FBuffer[FActiveOffset].Row);
				IServerStatementPlan LPlan = FProcess.PrepareStatement(FDeleteStatement, LParams);
				try
				{
					LPlan.Execute(LParams);
				}
				finally
				{
					FProcess.UnprepareStatement(LPlan);
				}
			}
		}
		
		#endregion

		#region Utility

		private DataSource FDataSource = null;
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
				if (FDataSource == null)
				{
					FDataSource = new DataSource();
					FDataSource.DataSet = this;
				}
				return FDataSource;
			}
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames, DataSetState AInitialState, bool AIsReadOnly)
		{
			DataView LDataView = new DataView();
			try
			{
				LDataView.Session = this.Session;
				LDataView.Expression = AExpression;
				LDataView.IsReadOnly = AIsReadOnly;
				LDataView.MasterSource = DataSource;
				LDataView.MasterKeyNames = AMasterKeyNames;
				LDataView.DetailKeyNames = ADetailKeyNames;
				LDataView.Open(AInitialState);
				return LDataView;
			}
			catch
			{
				LDataView.Dispose();
				throw;
			}
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The ReadOnly property for this overload will be false (read/write). </remarks>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames, DataSetState AInitialState)
		{
			return OpenDetail(AExpression, AMasterKeyNames, ADetailKeyNames, AInitialState, false);
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The OpenState will be Browse. </remarks>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames, bool AReadOnly)
		{
			return OpenDetail(AExpression, AMasterKeyNames, ADetailKeyNames, DataSetState.Browse, AReadOnly);
		}

		/// <summary> Creates and opens a new DataView detailed to this one. </summary>
		/// <remarks> The OpenState will be Browse and the ReadOnly property for this overload will be false (read/write). </remarks>
		public DataView OpenDetail(string AExpression, string AMasterKeyNames, string ADetailKeyNames)
		{
			return OpenDetail(AExpression, AMasterKeyNames, ADetailKeyNames, DataSetState.Browse, false);
		}

		#endregion
	}

	public enum EnlistMode { Default, True, False }
}

