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
	/// <summary> A DAEDataSet descendent that implements behavior common to an ordered result set. </summary>
	public abstract class TableDataSet : DAEDataSet
	{
		public TableDataSet()
		{
			FMasterLink = new MasterDataLink(this);
			FMasterLink.OnDataChanged += new DataLinkHandler(MasterDataChanged);
			FMasterLink.OnRowChanged += new DataLinkFieldHandler(MasterRowChanged);
			FMasterLink.OnStateChanged += new DataLinkHandler(MasterStateChanged);
			FMasterLink.OnPrepareToPost += new DataLinkHandler(MasterPrepareToPost);
			FMasterLink.OnPrepareToCancel += new DataLinkHandler(MasterPrepareToCancel);
		}

		protected override void InternalDispose(bool ADisposing)
		{
			try
			{
				base.InternalDispose(ADisposing);
			}
			finally
			{
				if (FMasterLink != null)
				{
					FMasterLink.OnPrepareToPost -= new DataLinkHandler(MasterPrepareToPost);
					FMasterLink.OnStateChanged -= new DataLinkHandler(MasterStateChanged);
					FMasterLink.OnRowChanged -= new DataLinkFieldHandler(MasterRowChanged);
					FMasterLink.OnDataChanged -= new DataLinkHandler(MasterDataChanged);
					FMasterLink.Dispose();
					FMasterLink = null;
				}
			}
		}

		#region Master/Detail

		// DetailKey
		private Schema.Key FDetailKey;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Key DetailKey
		{
			get { return FDetailKey; }
			set
			{
				if (FDetailKey != value)
				{
					FDetailKey = value;
					if (Active)
						CursorSetChanged(null, true);
				}
			}
		}

		[DefaultValue("")]
		[Category("Behavior")]
		[Description("Detail key names")]
		public string DetailKeyNames
		{
			get { return GetNamesFromKey(FDetailKey); }
			set
			{
				if (DetailKeyNames != value)
					DetailKey = GetKeyFromNames(value);
			}
		}

		// MasterKey
		private Schema.Key FMasterKey;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Key MasterKey
		{
			get { return FMasterKey; }
			set
			{
				if (FMasterKey != value)
				{
					FMasterKey = value;
					if (Active)
						CursorSetChanged(null, true);
				}
			}
		}

		[DefaultValue("")]
		[Category("Behavior")]
		[Description("Master key names")]
		public string MasterKeyNames
		{
			get { return GetNamesFromKey(FMasterKey); }
			set
			{
				if (MasterKeyNames != value)
					MasterKey = GetKeyFromNames(value);
			}
		}
		
		// MasterSource
		private DataLink FMasterLink;
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Master source")]
		public DataSource MasterSource
		{
			get { return FMasterLink.Source; }
			set
			{
				if (FMasterLink.Source != value)
				{
					if (IsLinkedTo(value))
						throw new ClientException(ClientException.Codes.CircularLink);
					FMasterLink.Source = value;
				}
			}
		}

		public bool IsDetailKey(string AColumnName)
		{
			if (IsMasterSetup())
				return DetailKey.Columns.Contains(AColumnName);
			else
				return false;
		}

		protected string[] GetInvariant()
		{
			// The invariant is the first non-empty intersection of any key of the master table type with the master key
			if (IsMasterSetup())
			{					
				ArrayList LInvariant = new ArrayList();
				foreach (Schema.Key LKey in MasterSource.DataSet.TableVar.Keys)
				{
					foreach (Schema.TableVarColumn LColumn in LKey.Columns)
					{
						int LIndex = FMasterKey.Columns.IndexOfName(LColumn.Name);
						if (LIndex >= 0)
							LInvariant.Add(FDetailKey.Columns[LIndex].Name);
					}
					if (LInvariant.Count > 0)
						return (string[])LInvariant.ToArray(typeof(string));
				}
			}
			return new string[]{};
		}
		
		protected virtual void MasterStateChanged(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
			{
				if (IsDetail() && !IsMasterSetup())
					InternalClose();
				else
				{
					StateChanged(); // Broadcast a state change to detail data sets so they know to exit the A/T
					CursorSetChanged(null, true);
				}
			}
		}

		protected virtual void MasterPrepareToPost(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
				EnsureBrowseState();
		}

		protected virtual void MasterPrepareToCancel(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
				EnsureBrowseState(false);
		}

		protected virtual void MasterDataChanged(DataLink ALink, DataSet ADataSet)
		{
			if (Active)
				CursorSetChanged(null, false);
		}
		
		protected virtual void MasterRowChanged(DataLink ALInk, DataSet ADataSet, DataField AField)
		{
			if (Active && ((AField == null) || MasterKey.Columns.Contains(AField.ColumnName)))
				CursorSetChanged(null, false);
		}

		public bool IsLinkedTo(DataSource ASource)
		{
			DataSet LDataSet;
			while (ASource != null)
			{
				LDataSet = ASource.DataSet;
				if (LDataSet == null)
					break;
				if (LDataSet == this)
					return true;
				ASource = (LDataSet is TableDataSet) ? ((TableDataSet)LDataSet).MasterSource : null;
			}
			return false;
		}
		
		/// <summary> Returns true if the master is set up (see IsMasterSetup()), and there is a value for each of the master's columns. </summary>
		public virtual bool IsMasterValid()
		{
			if (IsMasterSetup())
			{
				TableDataSet LDataSet = MasterSource.DataSet as TableDataSet;
				bool LIsMasterValid = (LDataSet == null) || (!LDataSet.IsDetail() || LDataSet.IsMasterValid());
				if (LIsMasterValid && !MasterSource.DataSet.IsEmpty())
				{
					foreach (DAE.Schema.TableVarColumn LColumn in FMasterKey.Columns)
						if (!(MasterSource.DataSet.Fields[LColumn.Name].HasValue()))
							return false;
					return true;
				}
			}
			return false;
		}
		
		public bool IsDetail()
		{
			return (MasterSource != null) || (MasterKey != null) || (DetailKey != null);
		}

		/// <summary> Posts each detail table of this dataset. </summary>
		public void PostDetails()
		{
			foreach (DataLink LLink in EnumerateLinks())
			{
				MasterDataLink LMasterLink = LLink as MasterDataLink;
				if (LMasterLink != null)
					LMasterLink.DetailDataSet.Post();	// A post while not in insert or edit state does nothing
			}
		}

		/// <summary> Returns true if the master exists, is active and it's schema in relation to the linked fields is known. </summary>
		public bool IsMasterSetup()
		{
			// Make sure that the master detail relationship is fully defined so that parameters can be built
			return
				(MasterSource != null) &&
				(MasterSource.DataSet != null) &&
				(MasterSource.DataSet.Active) &&
				(FMasterKey != null) &&
				(FDetailKey != null);
		}
		
		/// <summary> Defaults the key columns of a row matching a subset of the type of this DataSet with those of the Master. </summary>
		public void InitializeFromMaster(Row ARow)
		{
			if (IsMasterSetup() && !MasterSource.DataSet.IsEmpty())
			{
				DataField LField;
				for (int LIndex = 0; LIndex < FMasterKey.Columns.Count; LIndex++)
					if (ARow.DataType.Columns.Contains(FDetailKey.Columns[LIndex].Name))
					{
						LField = MasterSource.DataSet.Fields[FMasterKey.Columns[LIndex].Name];
						if (LField.HasValue())
							ARow[FDetailKey.Columns[LIndex].Name] = LField.Value;
					}
			}
		}
		
		/// <summary> Initializes row values with default data. </summary>
		protected override void InternalInitializeRow(Row ARow)
		{
			Process.BeginTransaction(IsolationLevel);
			try
			{
				if (IsMasterSetup() && !MasterSource.DataSet.IsEmpty())
				{
					Row LOriginalRow = new Row(ARow.Process, ARow.DataType);
					try
					{
						Schema.TableVarColumn LColumn;
						DataField LField;
						DataField LDetailField;
						for (int LIndex = 0; LIndex < FMasterKey.Columns.Count; LIndex++)
						{
							LColumn = FMasterKey.Columns[LIndex];
							LField = MasterSource.DataSet.Fields[LColumn.Name];
							if (FFields.Contains(FDetailKey.Columns[LIndex].Name))
							{
								LDetailField = Fields[FDetailKey.Columns[LIndex].Name];
								if (LField.HasValue())
								{
									Row LSaveOldRow = FOldRow;
									FOldRow = LOriginalRow;
									try
									{						
										ARow[LDetailField.Name] = LField.Value;
										try
										{
											InternalIsModified = InternalColumnChanging(LDetailField, LOriginalRow, ARow) || InternalIsModified;
										}
										catch
										{
											ARow.ClearValue(LDetailField.Name);
											throw;
										}
										
										InternalIsModified = InternalColumnChanged(LDetailField, LOriginalRow, ARow) || InternalIsModified;
									}
									finally
									{
										FOldRow = LSaveOldRow;
									}
								}
							}
						}
					}
					finally
					{
						LOriginalRow.Dispose();
					}
				}
					
				bool LSaveIsModified = InternalIsModified;
				try
				{
					base.InternalInitializeRow(ARow);
				}
				finally
				{
					InternalIsModified = LSaveIsModified;
				}

				Process.CommitTransaction();
			}
			catch
			{
				Process.RollbackTransaction();
				throw;
			}
		}
		
		#endregion

		#region Order

		// Order
		protected Schema.Order FOrder;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Order Order
		{
			get { return FOrder; }
			set
			{
				if (Active)
				{
					using (Row LRow = RememberActive())
					{
						FOrder = value;
						CursorSetChanged(LRow, true);
					}
				}
				else
					FOrder = value;
			}
		}
		
		public Schema.Order StringToOrder(string AOrder)
		{
			if (AOrder.IndexOf(Keywords.Key) >= 0)
			{
				KeyDefinition LKeyDefinition = FParser.ParseKeyDefinition(AOrder);
				Schema.Order LOrder = new Schema.Order();
				foreach (KeyColumnDefinition LColumn in LKeyDefinition.Columns)
					LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[LColumn.ColumnName], true));
				return LOrder;
			}
			else
			{
				OrderDefinition LOrderDefinition = FParser.ParseOrderDefinition(AOrder);
				Schema.Order LOrder = new Schema.Order();
				foreach (OrderColumnDefinition LColumn in LOrderDefinition.Columns)
					LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[LColumn.ColumnName], LColumn.Ascending, LColumn.IncludeNils));
				return LOrder;
			}
		}
		
		// OrderString
		[DefaultValue("")]
		[Browsable(false)]
		public string OrderString
		{
			get 
			{
				if (!Active)
					return (FOrderDefinition == null) ? String.Empty : new D4TextEmitter().Emit(FOrderDefinition);
				else
					return Order != null ? Order.Name : String.Empty;
			}
			set
			{
				if (!Active)
					FOrderDefinition = FParser.ParseOrderDefinition(value);
				else
				{
					if ((value == null) || (value == String.Empty))
						Order = null;
					else
						Order = StringToOrder(value);
				}
			}
		}
		
		// OrderColumnNames
		[DefaultValue("")]
		[Category("Behavior")]
		[Description("Order of the dataset as a comma- or semi-colon delimited list of column names")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string OrderColumnNames
		{
			get 
			{ 
				if (Active)
					return GetNamesFromOrder(FOrder);
				else
					return GetNamesFromOrder(FOrderDefinition == null ? null : OrderDefinitionToOrder(FOrderDefinition));
			}
			set
			{
				if (OrderColumnNames != value)
				{
					if (Active)
						Order = value == String.Empty ? null : GetOrderFromNames(value);
					else
						OrderDefinition = value == String.Empty ? null : (OrderDefinition)GetOrderFromNames(value).EmitStatement(EmitMode.ForCopy);
				}
			}
		}
		
		public Schema.Order OrderDefinitionToOrder(OrderDefinition AOrder)
		{
			Schema.Order LOrder = new Schema.Order(AOrder.MetaData);
			foreach (OrderColumnDefinition LColumn in AOrder.Columns)
				LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[LColumn.ColumnName], LColumn.Ascending, LColumn.IncludeNils));
			return LOrder;
		}
		
		protected OrderDefinition FOrderDefinition;
		[Category("Data")]
		[DefaultValue(null)]
		[Description("Order of the dataset.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OrderDefinition OrderDefinition
		{
			get
			{
				if (!Active)
					return FOrderDefinition;
				else
					return Order != null ? (OrderDefinition)Order.EmitStatement(EmitMode.ForCopy) : null;
			}
			set
			{
				if (!Active)
					FOrderDefinition = value;
				else
				{
					if (value == null)
						Order = null;
					else
						Order = OrderDefinitionToOrder(value);
				}
			}
		}
		
		protected Parser FParser = new Parser();
		
		#endregion

		#region Parameters

		protected override void InternalPrepareParams()
		{
			if (IsMasterSetup())
				for (int LIndex = 0; LIndex < FMasterKey.Columns.Count; LIndex++)
					FCursor.Params.Add(new MasterDataSetDataParam(GetParameterName(FDetailKey.Columns[LIndex].Name), MasterSource.DataSet.TableType.Columns[FMasterKey.Columns[LIndex].Name].DataType, Modifier.Const, FMasterKey.Columns[LIndex].Name, MasterSource, true));

			base.InternalPrepareParams();
		}

		protected override void SetParamValues()
		{
			bool LMasterValid = IsMasterValid();
			foreach (DataSetDataParam LParam in FCursor.Params)
			{
				MasterDataSetDataParam LMasterParam = LParam as MasterDataSetDataParam;
				if 
				(
					(LMasterParam == null) 
					|| (LMasterParam.IsMaster && LMasterValid) 
					|| 
					(
						!LMasterParam.IsMaster 
						&& (LMasterParam.Source != null) 
						&& (LMasterParam.Source.DataSet != null) 
						&& LMasterParam.Source.DataSet.Active
					)
				)
					LParam.Bind(Process);
			}
		}
		
		protected override bool ShouldOpenCursor()
		{
			return base.ShouldOpenCursor() && (!IsDetail() || IsMasterValid());
		}
		
		#endregion
		
		#region Open
		
		private void SetOrder()
		{
			FOrder = FCursor.Order;
			FOrderDefinition = null;
		}

		protected override void InternalOpen()
		{
			base.InternalOpen();
			SetOrder();
		}
		
		#endregion

		#region Close

		private void ClearOrder()
		{
			FOrderDefinition = FOrder != null ? (OrderDefinition)FOrder.EmitStatement(EmitMode.ForCopy) : null;
			FOrder = null;
		}
		
		protected override void InternalClose()
		{
			base.InternalClose();
			ClearOrder();
		}

		#endregion
	}

	public class MasterDataLink : DataLink
	{
		public MasterDataLink(TableDataSet ADataSet)
		{
			FDetailDataSet = ADataSet;
		}

		private TableDataSet FDetailDataSet;
		/// <summary> The detail dataset associated with the master link. </summary>
		public TableDataSet DetailDataSet
		{
			get { return FDetailDataSet; }
		}
	}
}

