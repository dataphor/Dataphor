/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Schema
{
    public abstract class EventHandler : CatalogObject
    {
		public EventHandler(string name) : base(name) {}
		public EventHandler(int iD, string name) : base(iD, name) {}
		
		// Operator
		[Reference]
		protected Operator _operator;
		public Operator Operator 
		{
			get { return _operator; } 
			set { _operator = value; } 
		}
		
		// ATHandlerName
		private string _aTHandlerName = null;
		public string ATHandlerName
		{
			get { return _aTHandlerName; }
			set { _aTHandlerName = value; }
		}
		
		public override bool IsATObject { get { return _aTHandlerName != null; } }
		
		// EventType
		protected EventType _eventType;
		public EventType EventType
		{
			get { return _eventType; }
			set { _eventType = value; }
		}
		
		// IsDeferred
		/// <summary>Indicates whether or not the execution of this handler should be deferred to transaction commit time.</summary>
		/// <remarks>
		///	Event handlers can only be deferred if they are handling the after table variable modification events.
		/// Event handlers attached to these types of modification events are deferred by default if they access global state.
		/// To set whether or not an event handler is deferred, use the DAE.IsDeferred tag.
		/// </remarks>
		public bool IsDeferred
		{
			get 
			{
				switch (_eventType)
				{
					case EventType.AfterInsert :
					case EventType.AfterUpdate :
					case EventType.AfterDelete :
						return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.IsDeferred", (!IsRemotable).ToString()));
				} 
				return false;
			}
			set 
			{
				switch (_eventType)
				{
					case EventType.AfterInsert :
					case EventType.AfterUpdate :
					case EventType.AfterDelete :
						if (MetaData == null)
							MetaData = new MetaData();
						MetaData.Tags.AddOrUpdate("DAE.IsDeferred", value.ToString());
					break;
				}
			}
		}
		
		// ShouldTranslate
		/// <summary>Indicates whether or not this handler should be invoked in an application transaction.</summary>
		/// <remarks>
		/// By default, all event handlers except after table event handlers are translated into an application transaction.  
		/// If a given handler is invoked within an application transaction, it will not be invoked when the application 
		/// transaction is committed. To disable application transaction translation of an event handler, 
		/// use the DAE.ShouldTranslate tag.
		/// </remarks>
		public bool ShouldTranslate
		{
			get 
			{ 
				switch (_eventType)
				{
					case EventType.AfterInsert :
					case EventType.AfterUpdate :
					case EventType.AfterDelete :
						return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.ShouldTranslate", "false")); 
				}
				return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.ShouldTranslate", "true")); 
			}
			set 
			{ 
				if (MetaData == null)
					MetaData = new MetaData();
				MetaData.Tags.AddOrUpdate("DAE.ShouldTranslate", value.ToString(), true); 
			}
		}
		
		// PlanNode
		protected PlanNode _planNode;
		public PlanNode PlanNode
		{
			get { return _planNode; }
			set { _planNode = value; }
		}
		
		public Statement EmitScalarTypeHandler(ScalarType scalarType, EmitMode mode)
		{
			AttachStatement statement = new AttachStatement();
			statement.MetaData = MetaData;
			statement.OperatorName = _operator.OperatorName;
			statement.EventSourceSpecifier = new ObjectEventSourceSpecifier(scalarType.Name);
			statement.EventSpecifier = new EventSpecifier();
			statement.EventSpecifier.EventType = _eventType;
			return statement;
		}
		
		public Statement EmitTableVarHandler(TableVar tableVar, EmitMode mode)
		{
			AttachStatement statement = new AttachStatement();
			statement.MetaData = MetaData;
			statement.OperatorName = _operator.OperatorName;
			statement.EventSourceSpecifier = new ObjectEventSourceSpecifier(tableVar.Name);
			statement.EventSpecifier = new EventSpecifier();
			statement.EventSpecifier.EventType = _eventType;
			return statement;
		}
		
		public Statement EmitColumnHandler(TableVar tableVar, TableVarColumn column, EmitMode mode)
		{
			AttachStatement statement = new AttachStatement();
			statement.MetaData = MetaData;
			statement.OperatorName = _operator.OperatorName;
			statement.EventSourceSpecifier = new ColumnEventSourceSpecifier(tableVar.Name, column.Name);
			statement.EventSpecifier = new EventSpecifier();
			statement.EventSpecifier.EventType = _eventType;
			return statement;
		}
		
		public override void DetermineRemotable(CatalogDeviceSession session)
		{
			#if TRIGGERSREMOTABLE
			base.DetermineRemotable(ACatalog);
			#else
			IsRemotable = false;
			#endif
		}
    }
    
    public class ScalarTypeEventHandler : EventHandler
    {
		public ScalarTypeEventHandler(int iD, string name) : base(iD, name) {}
		
		[Reference]
		internal ScalarType _scalarType;
		public ScalarType ScalarType
		{
			get { return _scalarType; }
			set
			{
				if (_scalarType != null)
					_scalarType.EventHandlers.Remove(this);
				if (value != null)
					value.EventHandlers.Add(this);
			}
		}
		
		public override string Description
		{
			get
			{
				// Operator "{0}" attached to event "{1}" of scalar type "{2}"
				return String.Format(Strings.Get("SchemaObjectDescription.ScalarTypeEventHandler", Operator.OperatorName, EventType.ToString(), _scalarType.DisplayName));
			}
		}

		//public override int CatalogObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

		//public override int ParentObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				AttachStatement statement = new AttachStatement();
				statement.OperatorName = Schema.Object.EnsureRooted(_operator.OperatorName);
				statement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(_scalarType.Name));
				statement.EventSpecifier = new EventSpecifier();
				statement.EventSpecifier.EventType = _eventType;
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				return statement;
			}
			finally
			{
				 if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DetachStatement statement = new DetachStatement();
			statement.OperatorName = Schema.Object.EnsureRooted(_operator.OperatorName);
			statement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(_scalarType.Name));
			statement.EventSpecifier = new EventSpecifier();
			statement.EventSpecifier.EventType = _eventType;
			return statement;
		}
    }
    
    public class TableVarColumnEventHandler : EventHandler
    {
		public TableVarColumnEventHandler(int iD, string name) : base(iD, name) {}
		
		[Reference]
		internal TableVarColumn _tableVarColumn;
		public TableVarColumn TableVarColumn
		{
			get { return _tableVarColumn; }
			set
			{
				if (_tableVarColumn != null)
					_tableVarColumn.EventHandlers.Remove(this);
				if (value != null)
					value.EventHandlers.Add(this);
			}
		}

		public override string Description
		{
			get
			{
				// Operator "{0}" attached to event "{1}" of column "{2}" in table "{3}"
				return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumnEventHandler", Operator.OperatorName, EventType.ToString(), _tableVarColumn.DisplayName, _tableVarColumn.TableVar.DisplayName));
			}
		}

		//public override int CatalogObjectID { get { return FTableVarColumn == null ? -1 : FTableVarColumn.CatalogObjectID; } }

 		//public override int ParentObjectID { get { return FTableVarColumn == null ? -1 : FTableVarColumn.ID; } }
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				AttachStatement statement = new AttachStatement();
				statement.OperatorName = Schema.Object.EnsureRooted(_operator.OperatorName);
				statement.EventSourceSpecifier = new ColumnEventSourceSpecifier(Schema.Object.EnsureRooted(_tableVarColumn.TableVar.Name), _tableVarColumn.Name);
				statement.EventSpecifier = new EventSpecifier();
				statement.EventSpecifier.EventType = _eventType;
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				return statement;
			}
			finally
			{
				 if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DetachStatement statement = new DetachStatement();
			statement.OperatorName = Schema.Object.EnsureRooted(_operator.OperatorName);
			statement.EventSourceSpecifier = new ColumnEventSourceSpecifier(Schema.Object.EnsureRooted(_tableVarColumn.TableVar.Name), _tableVarColumn.Name);
			statement.EventSpecifier = new EventSpecifier();
			statement.EventSpecifier.EventType = _eventType;
			return statement;
		}
    }
    
    public class TableVarEventHandler : EventHandler
    {
		public TableVarEventHandler(string name) : base(name) {}
		public TableVarEventHandler(int iD, string name) : base(iD, name) {}
		
		[Reference]
		internal TableVar _tableVar;
		public TableVar TableVar
		{
			get { return _tableVar; }
			set
			{
				if (_tableVar != null)
					_tableVar.EventHandlers.Remove(this);
				if (value != null)
					value.EventHandlers.Add(this);
			}
		}

		public override string Description
		{
			get
			{
				// Operator "{0}" attached to event "{1}" of table "{2}"
				return String.Format(Strings.Get("SchemaObjectDescription.TableVarEventHandler", Operator.OperatorName, EventType.ToString(), _tableVar.DisplayName));
			}
		}

		//public override int CatalogObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }

		//public override int ParentObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				AttachStatement statement = new AttachStatement();
				statement.OperatorName = Schema.Object.EnsureRooted(_operator.OperatorName);
				statement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(_tableVar.Name));
				statement.EventSpecifier = new EventSpecifier();
				statement.EventSpecifier.EventType = _eventType;
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				return statement;
			}
			finally
			{
				 if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			DetachStatement statement = new DetachStatement();
			statement.OperatorName = Schema.Object.EnsureRooted(_operator.OperatorName);
			statement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(_tableVar.Name));
			statement.EventSpecifier = new EventSpecifier();
			statement.EventSpecifier.EventType = _eventType;
			return statement;
		}
    }
    
    /// <remarks> EventHandlers </remarks>
	public class EventHandlers<T> : Objects<T> where T : EventHandler
    {
		#if USEOBJECTVALIDATE
		protected override void Validate(T item)
		{
			if (IndexOf(item) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateEventHandler, item.Operator.Name, item.EventType.ToString());
			base.Validate(item);
		}
		#endif
		
		protected override void Adding(T item, int index)
		{
			base.Adding(item, index);
			int count;
			if (_hasHandlers.TryGetValue(item.EventType, out count))
				_hasHandlers[item.EventType] = count + 1;
			else
				_hasHandlers.Add(item.EventType, 1);
		}
		
		protected override void Removing(T item, int index)
		{
			base.Removing(item, index);
			int count;
			if (_hasHandlers.TryGetValue(item.EventType, out count) && count == 1)
				_hasHandlers.Remove(item.EventType);
			else
				_hasHandlers[item.EventType] = count - 1;
		}

		private Dictionary<EventType, int> _hasHandlers = new Dictionary<EventType, int>(); // key - EventType, value - integer count of events of that type

		public bool HasHandlers(EventType eventType)
		{
			int count;
			return _hasHandlers.TryGetValue(eventType, out count) && count > 0;
		}

		public EventHandler this[Operator operatorValue, EventType eventType]
		{
			get { return this[IndexOf(operatorValue, eventType)]; }
		}
		
		public int IndexOf(Operator operatorValue, EventType eventType)
		{
			for (int index = 0; index < Count; index++)
				if ((this[index].Operator == operatorValue) && (this[index].EventType == eventType))
					return index;
			return -1;
		}
		
		public int IndexOf(string operatorName, EventType eventType)
		{
			for (int index = 0; index < Count; index++)
				if 
				(
					(
						Schema.Object.NamesEqual(this[index].Operator.OperatorName, operatorName) 
							|| ((this[index].Operator.SessionObjectName != null) && Schema.Object.NamesEqual(this[index].Operator.SessionObjectName, operatorName))
					) 
						&& (this[index].EventType == eventType)
				)
					return index;
			return -1;
		}
		
		public void Add(T handler, List<string> beforeOperatorNames)
		{
			int targetIndex = Count;
			foreach (string operatorName in beforeOperatorNames)
			{
				int index = IndexOf(operatorName, handler.EventType);
				if ((index >= 0) && (index < targetIndex))
					targetIndex = index;
			}
			Insert(targetIndex, handler);
		}
		
		public void MoveBefore(T handler, List<string> beforeOperatorNames)
		{
			int currentIndex = IndexOf(handler);
			int targetIndex = currentIndex;
			foreach (string operatorName in beforeOperatorNames)
			{
				int index = IndexOf(operatorName, handler.EventType);
				if ((index >= 0) && (index < targetIndex))
					targetIndex = index;
			}

			if (currentIndex != targetIndex)
			{	
				RemoveAt(currentIndex);
				Insert(targetIndex, handler);
			}
		}
    }

	public class EventHandlers : EventHandlers<EventHandler>
	{
	}
    
    public class ScalarTypeEventHandlers : EventHandlers
    {
		public ScalarTypeEventHandlers(ScalarType scalarType) : base()
		{
			_scalarType = scalarType;
		}
		
		[Reference]
		private ScalarType _scalarType;
		public ScalarType ScalarType { get { return _scalarType; } }

		protected override void Validate(EventHandler item)
		{
			if (!(item is ScalarTypeEventHandler))
				throw new SchemaException(SchemaException.Codes.EventHandlerContainer);
			base.Validate(item);
		}

		protected override void Adding(EventHandler item, int index)
		{
			base.Adding(item, index);
			((ScalarTypeEventHandler)item)._scalarType = _scalarType;
		}
		
		protected override void Removing(EventHandler item, int index)
		{
			((ScalarTypeEventHandler)item)._scalarType = null;
			base.Removing(item, index);
		}
    }

    public class TableVarColumnEventHandlers : EventHandlers
    {
		public TableVarColumnEventHandlers(TableVarColumn tableVarColumn) : base()
		{
			_tableVarColumn = tableVarColumn;
		}
		
		[Reference]
		private TableVarColumn _tableVarColumn;
		public TableVarColumn TableVarColumn { get { return _tableVarColumn; } }

		protected override void Validate(EventHandler item)
		{
			if (!(item is TableVarColumnEventHandler))
				throw new SchemaException(SchemaException.Codes.EventHandlerContainer);
			base.Validate(item);
		}
		
		protected override void Adding(EventHandler item, int index)
		{
			base.Adding(item, index);
			((TableVarColumnEventHandler)item)._tableVarColumn = _tableVarColumn;
			_tableVarColumn.EventHandlersAdding(this, item);
		}
		
		protected override void Removing(EventHandler item, int index)
		{
			_tableVarColumn.EventHandlersRemoving(this, item);
			((TableVarColumnEventHandler)item)._tableVarColumn = null;
			base.Removing(item, index);
		}
    }

    public class TableVarEventHandlers : EventHandlers
    {
		public TableVarEventHandlers(TableVar tableVar) : base()
		{
			_tableVar = tableVar;
		}
		
		[Reference]
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }

		protected override void Validate(EventHandler item)
		{
			if (!(item is TableVarEventHandler))
				throw new SchemaException(SchemaException.Codes.EventHandlerContainer);
			base.Validate(item);
		}
		
		protected override void Adding(EventHandler item, int index)
		{
			base.Adding(item, index);
			((TableVarEventHandler)item)._tableVar = _tableVar;
		}
		
		protected override void Removing(EventHandler item, int index)
		{
			((TableVarEventHandler)item)._tableVar = null;
			base.Removing(item, index);
		}
    }
}