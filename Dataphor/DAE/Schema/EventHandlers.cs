/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
    public abstract class EventHandler : CatalogObject
    {
		public EventHandler(string AName) : base(AName) {}
		public EventHandler(int AID, string AName) : base(AID, AName) {}
		
		// Operator
		[Reference]
		protected Operator FOperator;
		public Operator Operator 
		{
			get { return FOperator; } 
			set { FOperator = value; } 
		}
		
		// ATHandlerName
		private string FATHandlerName = null;
		public string ATHandlerName
		{
			get { return FATHandlerName; }
			set { FATHandlerName = value; }
		}
		
		public override bool IsATObject { get { return FATHandlerName != null; } }
		
		// EventType
		protected EventType FEventType;
		public EventType EventType
		{
			get { return FEventType; }
			set { FEventType = value; }
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
				switch (FEventType)
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
				switch (FEventType)
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
				switch (FEventType)
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
		protected PlanNode FPlanNode;
		public PlanNode PlanNode
		{
			get { return FPlanNode; }
			set { FPlanNode = value; }
		}
		
		public Statement EmitScalarTypeHandler(ScalarType AScalarType, EmitMode AMode)
		{
			AttachStatement LStatement = new AttachStatement();
			LStatement.MetaData = MetaData;
			LStatement.OperatorName = FOperator.OperatorName;
			LStatement.EventSourceSpecifier = new ObjectEventSourceSpecifier(AScalarType.Name);
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			return LStatement;
		}
		
		public Statement EmitTableVarHandler(TableVar ATableVar, EmitMode AMode)
		{
			AttachStatement LStatement = new AttachStatement();
			LStatement.MetaData = MetaData;
			LStatement.OperatorName = FOperator.OperatorName;
			LStatement.EventSourceSpecifier = new ObjectEventSourceSpecifier(ATableVar.Name);
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			return LStatement;
		}
		
		public Statement EmitColumnHandler(TableVar ATableVar, TableVarColumn AColumn, EmitMode AMode)
		{
			AttachStatement LStatement = new AttachStatement();
			LStatement.MetaData = MetaData;
			LStatement.OperatorName = FOperator.OperatorName;
			LStatement.EventSourceSpecifier = new ColumnEventSourceSpecifier(ATableVar.Name, AColumn.Name);
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			return LStatement;
		}
		
		public override void DetermineRemotable(ServerProcess AProcess)
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
		public ScalarTypeEventHandler(int AID, string AName) : base(AID, AName) {}
		
		[Reference]
		internal ScalarType FScalarType;
		public ScalarType ScalarType
		{
			get { return FScalarType; }
			set
			{
				if (FScalarType != null)
					FScalarType.EventHandlers.Remove(this);
				if (value != null)
					value.EventHandlers.Add(this);
			}
		}
		
		public override string Description
		{
			get
			{
				// Operator "{0}" attached to event "{1}" of scalar type "{2}"
				return String.Format(Strings.Get("SchemaObjectDescription.ScalarTypeEventHandler", Operator.OperatorName, EventType.ToString(), FScalarType.DisplayName));
			}
		}

		//public override int CatalogObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

		//public override int ParentObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			AttachStatement LStatement = new AttachStatement();
			LStatement.OperatorName = Schema.Object.EnsureRooted(FOperator.OperatorName);
			LStatement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(FScalarType.Name));
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			return LStatement;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DetachStatement LStatement = new DetachStatement();
			LStatement.OperatorName = Schema.Object.EnsureRooted(FOperator.OperatorName);
			LStatement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(FScalarType.Name));
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			return LStatement;
		}
    }
    
    public class TableVarColumnEventHandler : EventHandler
    {
		public TableVarColumnEventHandler(int AID, string AName) : base(AID, AName) {}
		
		[Reference]
		internal TableVarColumn FTableVarColumn;
		public TableVarColumn TableVarColumn
		{
			get { return FTableVarColumn; }
			set
			{
				if (FTableVarColumn != null)
					FTableVarColumn.EventHandlers.Remove(this);
				if (value != null)
					value.EventHandlers.Add(this);
			}
		}

		public override string Description
		{
			get
			{
				// Operator "{0}" attached to event "{1}" of column "{2}" in table "{3}"
				return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumnEventHandler", Operator.OperatorName, EventType.ToString(), FTableVarColumn.DisplayName, FTableVarColumn.TableVar.DisplayName));
			}
		}

		//public override int CatalogObjectID { get { return FTableVarColumn == null ? -1 : FTableVarColumn.CatalogObjectID; } }

 		//public override int ParentObjectID { get { return FTableVarColumn == null ? -1 : FTableVarColumn.ID; } }
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			AttachStatement LStatement = new AttachStatement();
			LStatement.OperatorName = Schema.Object.EnsureRooted(FOperator.OperatorName);
			LStatement.EventSourceSpecifier = new ColumnEventSourceSpecifier(Schema.Object.EnsureRooted(FTableVarColumn.TableVar.Name), FTableVarColumn.Name);
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			return LStatement;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DetachStatement LStatement = new DetachStatement();
			LStatement.OperatorName = Schema.Object.EnsureRooted(FOperator.OperatorName);
			LStatement.EventSourceSpecifier = new ColumnEventSourceSpecifier(Schema.Object.EnsureRooted(FTableVarColumn.TableVar.Name), FTableVarColumn.Name);
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			return LStatement;
		}
    }
    
    public class TableVarEventHandler : EventHandler
    {
		public TableVarEventHandler(string AName) : base(AName) {}
		public TableVarEventHandler(int AID, string AName) : base(AID, AName) {}
		
		[Reference]
		internal TableVar FTableVar;
		public TableVar TableVar
		{
			get { return FTableVar; }
			set
			{
				if (FTableVar != null)
					FTableVar.EventHandlers.Remove(this);
				if (value != null)
					value.EventHandlers.Add(this);
			}
		}

		public override string Description
		{
			get
			{
				// Operator "{0}" attached to event "{1}" of table "{2}"
				return String.Format(Strings.Get("SchemaObjectDescription.TableVarEventHandler", Operator.OperatorName, EventType.ToString(), FTableVar.DisplayName));
			}
		}

		//public override int CatalogObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }

		//public override int ParentObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			AttachStatement LStatement = new AttachStatement();
			LStatement.OperatorName = Schema.Object.EnsureRooted(FOperator.OperatorName);
			LStatement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(FTableVar.Name));
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DetachStatement LStatement = new DetachStatement();
			LStatement.OperatorName = Schema.Object.EnsureRooted(FOperator.OperatorName);
			LStatement.EventSourceSpecifier = new ObjectEventSourceSpecifier(Schema.Object.EnsureRooted(FTableVar.Name));
			LStatement.EventSpecifier = new EventSpecifier();
			LStatement.EventSpecifier.EventType = FEventType;
			return LStatement;
		}
    }
    
    /// <remarks> EventHandlers </remarks>
	public class EventHandlers : Objects
    {
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is EventHandler))
				throw new SchemaException(SchemaException.Codes.EventHandlerContainer);
			if (IndexOf((EventHandler)AItem) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateEventHandler, ((EventHandler)AItem).Operator.Name, ((EventHandler)AItem).EventType.ToString());
			base.Validate(AItem);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			EventHandler LHandler = (EventHandler)AItem;
			object LCount = FHasHandlers[LHandler.EventType];
			if (LCount != null)
				FHasHandlers[LHandler.EventType] = (int)LCount + 1;
			else
				FHasHandlers.Add(LHandler.EventType, 1);
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			base.Removing(AItem, AIndex);
			EventHandler LHandler = (EventHandler)AItem;
			int LCount = (int)FHasHandlers[LHandler.EventType];
			if (LCount == 1)
				FHasHandlers.Remove(LHandler.EventType);
			else
				FHasHandlers[LHandler.EventType] = LCount - 1;
		}
		
		private Hashtable FHasHandlers = new Hashtable(); // key - EventType, value - integer count of events of that type

		public bool HasHandlers(EventType AEventType)
		{
			object LCount = FHasHandlers[AEventType];
			return (LCount != null) && ((int)LCount > 0);
		}

		public new EventHandler this[int AIndex]
		{
			get { return (EventHandler)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public new EventHandler this[string AName]
		{
			get { return (EventHandler)base[AName]; }
			set { base[AName] = value; }
		}
		
		public EventHandler this[Operator AOperator, EventType AEventType]
		{
			get { return this[IndexOf(AOperator, AEventType)]; }
		}
		
		public int IndexOf(Operator AOperator, EventType AEventType)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if ((this[LIndex].Operator == AOperator) && (this[LIndex].EventType == AEventType))
					return LIndex;
			return -1;
		}
		
		public int IndexOf(string AOperatorName, EventType AEventType)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if 
				(
					(
						Schema.Object.NamesEqual(this[LIndex].Operator.OperatorName, AOperatorName) 
							|| ((this[LIndex].Operator.SessionObjectName != null) && Schema.Object.NamesEqual(this[LIndex].Operator.SessionObjectName, AOperatorName))
					) 
						&& (this[LIndex].EventType == AEventType)
				)
					return LIndex;
			return -1;
		}
		
		public void Add(EventHandler AHandler, StringCollection ABeforeOperatorNames)
		{
			int LTargetIndex = Count;
			foreach (string LOperatorName in ABeforeOperatorNames)
			{
				int LIndex = IndexOf(LOperatorName, AHandler.EventType);
				if ((LIndex >= 0) && (LIndex < LTargetIndex))
					LTargetIndex = LIndex;
			}
			Insert(LTargetIndex, AHandler);
		}
		
		public void MoveBefore(EventHandler AHandler, StringCollection ABeforeOperatorNames)
		{
			int LCurrentIndex = IndexOf(AHandler);
			int LTargetIndex = LCurrentIndex;
			foreach (string LOperatorName in ABeforeOperatorNames)
			{
				int LIndex = IndexOf(LOperatorName, AHandler.EventType);
				if ((LIndex >= 0) && (LIndex < LTargetIndex))
					LTargetIndex = LIndex;
			}

			if (LCurrentIndex != LTargetIndex)
			{	
				RemoveAt(LCurrentIndex);
				Insert(LTargetIndex, AHandler);
			}
		}
    }
    
    public class ScalarTypeEventHandlers : EventHandlers
    {
		public ScalarTypeEventHandlers(ScalarType AScalarType) : base()
		{
			FScalarType = AScalarType;
		}
		
		[Reference]
		private ScalarType FScalarType;
		public ScalarType ScalarType { get { return FScalarType; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is ScalarTypeEventHandler))
				throw new SchemaException(SchemaException.Codes.InvalidContainer, "ScalarTypeEventHandler");
			base.Validate(AItem);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((ScalarTypeEventHandler)AItem).FScalarType = FScalarType;
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			((ScalarTypeEventHandler)AItem).FScalarType = null;
			base.Removing(AItem, AIndex);
		}
    }

    public class TableVarColumnEventHandlers : EventHandlers
    {
		public TableVarColumnEventHandlers(TableVarColumn ATableVarColumn) : base()
		{
			FTableVarColumn = ATableVarColumn;
		}
		
		[Reference]
		private TableVarColumn FTableVarColumn;
		public TableVarColumn TableVarColumn { get { return FTableVarColumn; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is TableVarColumnEventHandler))
				throw new SchemaException(SchemaException.Codes.InvalidContainer, "TableVarColumnEventHandler");
			base.Validate(AItem);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((TableVarColumnEventHandler)AItem).FTableVarColumn = FTableVarColumn;
			FTableVarColumn.EventHandlersAdding(this, AItem);
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			FTableVarColumn.EventHandlersRemoving(this, AItem);
			((TableVarColumnEventHandler)AItem).FTableVarColumn = null;
			base.Removing(AItem, AIndex);
		}
    }

    public class TableVarEventHandlers : EventHandlers
    {
		public TableVarEventHandlers(TableVar ATableVar) : base()
		{
			FTableVar = ATableVar;
		}
		
		[Reference]
		private TableVar FTableVar;
		public TableVar TableVar { get { return FTableVar; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is TableVarEventHandler))
				throw new SchemaException(SchemaException.Codes.InvalidContainer, "TableVarEventHandler");
			base.Validate(AItem);
		}
		#endif
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((TableVarEventHandler)AItem).FTableVar = FTableVar;
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			((TableVarEventHandler)AItem).FTableVar = null;
			base.Removing(AItem, AIndex);
		}
    }
}