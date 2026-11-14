/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;

using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public delegate void SchemaHandler(BaseSchema AObject);

	public abstract class BaseSchema
	{
		public event SchemaHandler OnModified;
		/// <summary> Called when a modification has occured to the schema object. </summary>
		protected void Modified()
		{
			if (OnModified != null)
				OnModified(this);
		}

		public event SchemaHandler OnDeleted;
		protected void Deleted()
		{
			if (OnDeleted != null)
				OnDeleted(this);
		}
	}

	public class ServerSchema : BaseSchema, IDisposable
	{
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		protected virtual void Dispose(bool ADisposed)
		{
			if (FServer != null)
			{
				FServer.Dispose();
				FServer = null;
			}
		}

		~ServerSchema()
		{
			Dispose(false);
		}

		public ServerSchema(string AScript)
		{
			FServer = new Server();
			try
			{
				FServer.IsRepository = true;	// Don't do any reconciliation or other such thing
				FServer.Start(AScript);
			}
			catch
			{
				FServer.Dispose();
				throw;
			}

		}

		private Server FServer;
		protected internal Server Server
		{
			get { return FServer; }
		}
		
		public CatalogSchema GetCatalog()
		{
			return new CatalogSchema(this);
		}

		public string GetScript()
		{
			return FServer.ScriptCatalog();
		}
	}

	public class CatalogSchema : BaseSchema, IEnumerable
	{
		public CatalogSchema(ServerSchema AServer)
		{
			FServer = AServer;
		}

		private ServerSchema FServer;
		public ServerSchema Server
		{
			get { return FServer; }
		}

		protected internal Catalog Catalog
		{
			get { return FServer.Server.Catalog; }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ObjectEnumerator(this);
		}

		public ObjectEnumerator GetEnumerator()
		{
			return new ObjectEnumerator(this);
		}

		public class ObjectEnumerator : IEnumerator
		{
			internal ObjectEnumerator(CatalogSchema ACatalog)
			{
				FCatalog = ACatalog;
				FIndex = -1;
			}

			private int FIndex;
			private CatalogSchema FCatalog;

			public bool MoveNext()
			{
				do
					FIndex++;
				while ((FIndex < FCatalog.Catalog.Count) && FCatalog.Catalog[FIndex].IsSystem);
				return FIndex < FCatalog.Catalog.Count;
			}

			private ObjectSchema GetObject()
			{
				DAE.Schema.Object LObject = FCatalog.Catalog[FIndex];
				if (LObject is BaseTableVar)
					return new TableSchema(LObject, FCatalog);
				else if (LObject is DerivedTableVar)
					return new ViewSchema(LObject, FCatalog);
				else if (LObject is Operator)
					return new OperatorSchema(LObject, FCatalog);
				else
					return null;
			}

			object IEnumerator.Current
			{
				get { return GetObject(); }
			}

			public ObjectSchema Current
			{
				get { return GetObject(); }
			}

			public void Reset()
			{
				FIndex = -1;
			}
		}
	}

	public class ObjectSchema : BaseSchema
	{
		public ObjectSchema(DAE.Schema.Object AObject, CatalogSchema ACatalog)
		{
			FObject = AObject;
			FCatalog = ACatalog;
		}

		// Object

		private DAE.Schema.Object FObject;
		protected internal DAE.Schema.Object Object
		{
			get { return FObject; }
		}

		// Catalog

		private CatalogSchema FCatalog;
		public CatalogSchema Catalog
		{
			get { return FCatalog; }
		}

		// ShortName

		protected virtual string GetShortName()
		{
			return FObject.Name;
		}

		protected virtual void SetShortName(string AName)
		{
			FObject.Name = AName;
		}

		// Description

		public virtual string Description
		{
			get { return GetShortName(); }
			set
			{
				string LOldName = GetShortName();
				if (LOldName != value)
				{
					// Make sure the new name is kosher
					if (DAE.Language.D4.Parser.IsReservedWord(value) || !DAE.Language.D4.Parser.IsValidIdentifier(value))
						throw new DataphoriaException(DataphoriaException.Codes.InvalidIdentifierName);

					FCatalog.Catalog.Remove(FObject);
					try
					{
						SetShortName(value);
						FCatalog.Catalog.Add(FObject);
					}
					catch
					{
						SetShortName(LOldName);
						FCatalog.Catalog.Add(FObject);	// This better not blow up because this thing was just barely named this
						throw;
					}
					Modified();
				}
			}
		}

		public void Delete()
		{
			// TODO: Do this right
			FCatalog.Catalog.Remove(FObject);
			Deleted();
		}
	}

	public class OperatorSchema : ObjectSchema
	{
		public OperatorSchema(DAE.Schema.Object AObject, CatalogSchema ACatalog) : base(AObject, ACatalog)
		{
		}

		protected internal Operator Operator
		{
			get { return (Operator)Object; }
		}

		protected override string GetShortName()
		{
			return Operator.OperatorName;
		}

		protected override void SetShortName(string AName)
		{
			Operator.OperatorName = AName;
		}
	}

	public class TableSchema : ObjectSchema
	{
		public TableSchema(DAE.Schema.Object AObject, CatalogSchema ACatalog) : base(AObject, ACatalog)
		{
		}

		protected internal BaseTableVar BaseTableVar
		{
			get { return (BaseTableVar)Object; }
		}

		public ColumnSchemaEnumerator GetColumns()
		{
			return new ColumnSchemaEnumerator(this);
		}

		public class ColumnSchemaEnumerator : IEnumerator
		{
			public ColumnSchemaEnumerator(TableSchema ATable)
			{
				FTable = ATable;
				FIndex = -1;
			}

			private TableSchema FTable;
			private int FIndex;

			public void Reset()
			{
				FIndex = -1;
			}

			private ColumnSchema GetCurrent()
			{
				return new ColumnSchema(FTable.BaseTableVar.Columns[FIndex], FTable);
			}

			object IEnumerator.Current
			{
				get { return GetCurrent(); }
			}

			public ColumnSchema Current
			{
				get { return GetCurrent(); }
			}

			public bool MoveNext()
			{
				FIndex++;
				return FIndex < FTable.BaseTableVar.DataType.Columns.Count;
			}
		}

		public KeySchemaEnumerator GetKeys()
		{
			return new KeySchemaEnumerator(this);
		}

		public class KeySchemaEnumerator : IEnumerator
		{
			public KeySchemaEnumerator(TableSchema ATable)
			{
				FTable = ATable;
				FIndex = -1;
			}

			private TableSchema FTable;
			private int FIndex;

			public void Reset()
			{
				FIndex = -1;
			}

			private KeySchema GetCurrent()
			{
				return new KeySchema(FTable.BaseTableVar.Keys[FIndex], FTable);
			}

			object IEnumerator.Current
			{
				get { return GetCurrent(); }
			}

			public KeySchema Current
			{
				get { return GetCurrent(); }
			}

			public bool MoveNext()
			{
				FIndex++;
				return FIndex < FTable.BaseTableVar.Keys.Count;
			}
		}

		public OrderSchemaEnumerator GetOrders()
		{
			return new OrderSchemaEnumerator(this);
		}

		public class OrderSchemaEnumerator : IEnumerator
		{
			public OrderSchemaEnumerator(TableSchema ATable)
			{
				FTable = ATable;
				FIndex = -1;
			}

			private TableSchema FTable;
			private int FIndex;

			public void Reset()
			{
				FIndex = -1;
			}

			private OrderSchema GetCurrent()
			{
				return new OrderSchema(FTable.BaseTableVar.Orders[FIndex], FTable);
			}

			object IEnumerator.Current
			{
				get { return GetCurrent(); }
			}

			public OrderSchema Current
			{
				get { return GetCurrent(); }
			}

			public bool MoveNext()
			{
				FIndex++;
				return FIndex < FTable.BaseTableVar.Orders.Count;
			}
		}
	}

	public class ColumnSchema : BaseSchema
	{
		public ColumnSchema(TableVarColumn AColumn, TableSchema ATable)
		{
			FColumn = AColumn;
			FTable = ATable;
		}

		// Column

		private TableVarColumn FColumn;
		protected internal TableVarColumn Column
		{
			get { return FColumn; }
		}

		// Table

		private TableSchema FTable;
		public TableSchema Table
		{
			get { return FTable; }
		}

		public string Name
		{
			get { return FColumn.Name; }
		}

		// TODO: Expose DataType/Default/etc, or wrap them???

		// DataType

		public DAE.Schema.IDataType DataType
		{
			get { return FColumn.DataType; }
		}

		// Defaults

		public DAE.Schema.TableVarColumnDefault Default
		{
			get { return FColumn.Default; }
		}
	}

	public abstract class BaseColumnListSchema : BaseSchema
	{
		public BaseColumnListSchema(TableSchema ATable)
		{
			FTable = ATable;
		}

		private TableSchema FTable;
		public TableSchema Table
		{
			get { return FTable; }
		}

		public abstract int ColumnCount { get; }

		public abstract string GetColumnName(int AIndex);
	}

	public class KeySchema : BaseColumnListSchema
	{
		public KeySchema(Key AKey, TableSchema ATable) : base(ATable)
		{
			FKey = AKey;
		}

		private Key FKey;
		protected internal Key Key
		{
			get { return FKey; }
		}

		public override int ColumnCount
		{
			get { return FKey.Columns.Count; }
		}

		public override string GetColumnName(int AIndex)
		{
			return FKey.Columns[AIndex].Name;
		}
	}


	public class OrderSchema : BaseColumnListSchema
	{
		public OrderSchema(Order AOrder, TableSchema ATable) : base(ATable)
		{
			FOrder = AOrder;
		}

		private Order FOrder;
		protected internal Order Order
		{
			get { return FOrder; }
		}

		public override int ColumnCount
		{
			get { return FOrder.Columns.Count; }
		}

		public override string GetColumnName(int AIndex)
		{
			return FOrder.Columns[AIndex].Column.Name;
		}

		public OrderColumnDetail GetColumnDetails(int AIndex)
		{
			OrderColumn LColumn = FOrder.Columns[AIndex];

			OrderColumnDetail LResult = new OrderColumnDetail();
			LResult.Ascending = LColumn.Ascending;
			LResult.IsDefaultSort = LColumn.IsDefaultSort;
			if (LColumn.IsDefaultSort)
				LResult.OrderExpression = String.Empty;
			else
				LResult.OrderExpression = new DAE.Language.D4.D4TextEmitter().Emit(LColumn.Sort.EmitStatement(DAE.Language.D4.EmitMode.ForCopy));
			return LResult;
		}
	}

	public struct OrderColumnDetail
	{
		public bool Ascending;
		public bool IsDefaultSort;
		public string OrderExpression;
	}

	public class ViewSchema : ObjectSchema
	{
		public ViewSchema(DAE.Schema.Object AObject, CatalogSchema ACatalog) : base(AObject, ACatalog)
		{
		}

		protected DerivedTableVar DerivedTableVar
		{
			get { return (DerivedTableVar)Object; }
		}
	}
}
