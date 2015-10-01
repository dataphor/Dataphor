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
using Schema = Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Client
{
	/// <summary> A DAEDataSet descendent that implements behavior common to an ordered result set. </summary>
	public abstract class TableDataSet : DAEDataSet
	{
		public TableDataSet()
		{
			_masterLink = new MasterDataLink(this);
			_masterLink.OnDataChanged += new DataLinkHandler(MasterDataChanged);
			_masterLink.OnRowChanged += new DataLinkFieldHandler(MasterRowChanged);
			_masterLink.OnStateChanged += new DataLinkHandler(MasterStateChanged);
			_masterLink.OnPrepareToPost += new DataLinkHandler(MasterPrepareToPost);
			_masterLink.OnPrepareToCancel += new DataLinkHandler(MasterPrepareToCancel);
		}

		protected override void InternalDispose(bool disposing)
		{
			try
			{
				base.InternalDispose(disposing);
			}
			finally
			{
				if (_masterLink != null)
				{
					_masterLink.OnPrepareToPost -= new DataLinkHandler(MasterPrepareToPost);
					_masterLink.OnStateChanged -= new DataLinkHandler(MasterStateChanged);
					_masterLink.OnRowChanged -= new DataLinkFieldHandler(MasterRowChanged);
					_masterLink.OnDataChanged -= new DataLinkHandler(MasterDataChanged);
					_masterLink.Dispose();
					_masterLink = null;
				}
			}
		}

		#region Master/Detail

		// DetailKey
		private Schema.Key _detailKey;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Key DetailKey
		{
			get { return _detailKey; }
			set
			{
				if (_detailKey != value)
				{
					_detailKey = value;
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
			get { return GetNamesFromKey(_detailKey); }
			set
			{
				if (DetailKeyNames != value)
					DetailKey = GetKeyFromNames(value);
			}
		}

		// MasterKey
		private Schema.Key _masterKey;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Key MasterKey
		{
			get { return _masterKey; }
			set
			{
				if (_masterKey != value)
				{
					_masterKey = value;
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
			get { return GetNamesFromKey(_masterKey); }
			set
			{
				if (MasterKeyNames != value)
					MasterKey = GetKeyFromNames(value);
			}
		}
		
		// MasterSource
		private DataLink _masterLink;
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Master source")]
		public DataSource MasterSource
		{
			get { return _masterLink.Source; }
			set
			{
				if (_masterLink.Source != value)
				{
					if (IsLinkedTo(value))
						throw new ClientException(ClientException.Codes.CircularLink);
					_masterLink.Source = value;
				}
			}
		}

		public bool IsDetailKey(string columnName)
		{
			if (IsMasterSetup())
				return DetailKey.Columns.Contains(columnName);
			else
				return false;
		}

		protected string[] GetInvariant()
		{
			// The invariant is the first non-empty intersection of any key of the master table type with the master key
			if (IsMasterSetup())
			{
				List<string> invariant = new List<string>();
				foreach (Schema.Key key in MasterSource.DataSet.TableVar.Keys)
				{
					foreach (Schema.TableVarColumn column in key.Columns)
					{
						int index = _masterKey.Columns.IndexOfName(column.Name);
						if (index >= 0)
							invariant.Add(_detailKey.Columns[index].Name);
					}
					if (invariant.Count > 0)
						return invariant.ToArray();
				}
			}
			return new string[]{};
		}
		
		protected virtual void MasterStateChanged(DataLink link, DataSet dataSet)
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

		protected virtual void MasterPrepareToPost(DataLink link, DataSet dataSet)
		{
			if (Active)
				EnsureBrowseState();
		}

		protected virtual void MasterPrepareToCancel(DataLink link, DataSet dataSet)
		{
			if (Active)
				EnsureBrowseState(false);
		}

		protected virtual void MasterDataChanged(DataLink link, DataSet dataSet)
		{
			if (Active)
				CursorSetChanged(null, false);
		}
		
		protected virtual void MasterRowChanged(DataLink lInk, DataSet dataSet, DataField field)
		{
			if (Active && ((field == null) || MasterKey.Columns.Contains(field.ColumnName)))
				CursorSetChanged(null, false);
		}

		public bool IsLinkedTo(DataSource source)
		{
			DataSet dataSet;
			while (source != null)
			{
				dataSet = source.DataSet;
				if (dataSet == null)
					break;
				if (dataSet == this)
					return true;
				source = (dataSet is TableDataSet) ? ((TableDataSet)dataSet).MasterSource : null;
			}
			return false;
		}
		
		/// <summary> Returns true if the master is set up (see IsMasterSetup()), and there is a value for each of the master's columns. </summary>
		public virtual bool IsMasterValid()
		{
			if (IsMasterSetup())
			{
				TableDataSet dataSet = MasterSource.DataSet as TableDataSet;
				bool isMasterValid = (dataSet == null) || (!dataSet.IsDetail() || dataSet.IsMasterValid());
				if (isMasterValid && !MasterSource.DataSet.IsEmpty())
				{
					foreach (DAE.Schema.TableVarColumn column in _masterKey.Columns)
						if (!(MasterSource.DataSet.Fields[column.Name].HasValue()))
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
			foreach (DataLink link in EnumerateLinks())
			{
				MasterDataLink masterLink = link as MasterDataLink;
				if (masterLink != null)
					masterLink.DetailDataSet.Post();	// A post while not in insert or edit state does nothing
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
				(_masterKey != null) &&
				(_detailKey != null);
		}
		
		/// <summary> Defaults the key columns of a row matching a subset of the type of this DataSet with those of the Master. </summary>
		public void InitializeFromMaster(Row row)
		{
			if (IsMasterSetup() && !MasterSource.DataSet.IsEmpty())
			{
				DataField field;
				for (int index = 0; index < _masterKey.Columns.Count; index++)
					if (row.DataType.Columns.Contains(_detailKey.Columns[index].Name))
					{
						field = MasterSource.DataSet.Fields[_masterKey.Columns[index].Name];
						if (field.HasValue())
							row[_detailKey.Columns[index].Name] = field.Value;
					}
			}
		}
		
		/// <summary> Initializes row values with default data. </summary>
		protected override void InternalInitializeRow(IRow row)
		{
			Process.BeginTransaction(IsolationLevel);
			try
			{
				if (IsMasterSetup() && !MasterSource.DataSet.IsEmpty())
				{
					Row originalRow = new Row(row.Manager, row.DataType);
					try
					{
						Schema.TableVarColumn column;
						DataField field;
						DataField detailField;
						for (int index = 0; index < _masterKey.Columns.Count; index++)
						{
							column = _masterKey.Columns[index];
							field = MasterSource.DataSet.Fields[column.Name];
							if (_fields.Contains(_detailKey.Columns[index].Name))
							{
								detailField = Fields[_detailKey.Columns[index].Name];
								if (field.HasValue())
								{
									IRow saveOldRow = _oldRow;
									_oldRow = originalRow;
									try
									{						
										row[detailField.Name] = field.Value;
										try
										{
											_isModified = InternalColumnChanging(detailField, originalRow, row) || _isModified;
										}
										catch
										{
											row.ClearValue(detailField.Name);
											throw;
										}
										
										_isModified = InternalColumnChanged(detailField, originalRow, row) || _isModified;
									}
									finally
									{
										_oldRow = saveOldRow;
									}
								}
							}
						}
					}
					finally
					{
						originalRow.Dispose();
					}
				}
					
				bool saveIsModified = _isModified;
				try
				{
					base.InternalInitializeRow(row);
				}
				finally
				{
					_isModified = saveIsModified;
				}

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
		
		#endregion

		#region Order

		// Order
		protected Schema.Order _order;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Schema.Order Order
		{
			get { return _order; }
			set
			{
				if (Active)
				{
					using (IRow row = RememberActive())
					{
						_order = value;
						CursorSetChanged(row, true);
					}
				}
				else
					_order = value;
			}
		}
		
		public Schema.Order StringToOrder(string order)
		{
			if (order.IndexOf(Keywords.Key) >= 0)
			{
				KeyDefinition keyDefinition = _parser.ParseKeyDefinition(order);
				Schema.Order localOrder = new Schema.Order();
				foreach (KeyColumnDefinition column in keyDefinition.Columns)
					localOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[column.ColumnName], true));
				return localOrder;
			}
			else
			{
				OrderDefinition orderDefinition = _parser.ParseOrderDefinition(order);
				Schema.Order localOrder = new Schema.Order();
				foreach (OrderColumnDefinition column in orderDefinition.Columns)
					localOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[column.ColumnName], column.Ascending, column.IncludeNils));
				return localOrder;
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
					return (_orderDefinition == null) ? String.Empty : new D4TextEmitter().Emit(_orderDefinition);
				else
					return Order != null ? Order.Name : String.Empty;
			}
			set
			{
				if (!Active)
					_orderDefinition = _parser.ParseOrderDefinition(value);
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
					return GetNamesFromOrder(_order);
				else
					return GetNamesFromOrder(_orderDefinition == null ? null : OrderDefinitionToOrder(_orderDefinition));
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
		
		public Schema.Order OrderDefinitionToOrder(OrderDefinition order)
		{
			Schema.Order localOrder = new Schema.Order(order.MetaData);
			foreach (OrderColumnDefinition column in order.Columns)
				localOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns[column.ColumnName], column.Ascending, column.IncludeNils));
			return localOrder;
		}
		
		protected OrderDefinition _orderDefinition;
		[Category("Data")]
		[DefaultValue(null)]
		[Description("Order of the dataset.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OrderDefinition OrderDefinition
		{
			get
			{
				if (!Active)
					return _orderDefinition;
				else
					return Order != null ? (OrderDefinition)Order.EmitStatement(EmitMode.ForCopy) : null;
			}
			set
			{
				if (!Active)
					_orderDefinition = value;
				else
				{
					if (value == null)
						Order = null;
					else
						Order = OrderDefinitionToOrder(value);
				}
			}
		}
		
		protected Parser _parser = new Parser();
		
		#endregion

		#region Parameters

		protected override void InternalPrepareParams()
		{
			if (IsMasterSetup())
				for (int index = 0; index < _masterKey.Columns.Count; index++)
					_cursor.Params.Add(new MasterDataSetDataParam(GetParameterName(_detailKey.Columns[index].Name), MasterSource.DataSet.TableType.Columns[_masterKey.Columns[index].Name].DataType, Modifier.Const, _masterKey.Columns[index].Name, MasterSource, true));

			base.InternalPrepareParams();
		}

		protected override void SetParamValues()
		{
			bool masterValid = IsMasterValid();
			foreach (DataSetDataParam param in _cursor.Params)
			{
				MasterDataSetDataParam masterParam = param as MasterDataSetDataParam;
				if 
				(
					(masterParam == null) 
					|| (masterParam.IsMaster && masterValid) 
					|| 
					(
						!masterParam.IsMaster 
						&& (masterParam.Source != null) 
						&& (masterParam.Source.DataSet != null) 
						&& masterParam.Source.DataSet.Active
					)
				)
					param.Bind(Process);
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
			_order = _cursor.Order;
			_orderDefinition = null;
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
			_orderDefinition = _order != null ? (OrderDefinition)_order.EmitStatement(EmitMode.ForCopy) : null;
			_order = null;
		}
		
		protected override void InternalClose()
		{
			try
			{
				base.InternalClose();
			}
			finally
			{
				ClearOrder();
			}
		}

		#endregion
	}

	public class MasterDataLink : DataLink
	{
		public MasterDataLink(TableDataSet dataSet)
		{
			_detailDataSet = dataSet;
		}

		private TableDataSet _detailDataSet;
		/// <summary> The detail dataset associated with the master link. </summary>
		public TableDataSet DetailDataSet
		{
			get { return _detailDataSet; }
		}
	}
}

