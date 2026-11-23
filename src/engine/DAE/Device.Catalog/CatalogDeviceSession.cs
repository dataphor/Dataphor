/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define LOGDDLINSTRUCTIONS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDeviceSession : MemoryDeviceSession
	{		
		protected internal CatalogDeviceSession(Schema.Device device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo){}
		
		public Schema.Catalog Catalog { get { return ((Server.Engine)ServerProcess.ServerSession.Server).Catalog; } }

		#region Execute
		
		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			if ((planNode is BaseTableVarNode) || (planNode is OrderNode))
			{
				Schema.TableVar tableVar = null;
				if (planNode is BaseTableVarNode)
					tableVar = ((BaseTableVarNode)planNode).TableVar;
				else if (planNode is OrderNode)
					tableVar = ((BaseTableVarNode)planNode.Nodes[0]).TableVar;
				if (tableVar != null)
				{
					lock (Device.Headers)
					{
						CatalogHeader header = Device.Headers[tableVar];
						if ((header.CacheLevel == CatalogCacheLevel.None) || ((header.CacheLevel == CatalogCacheLevel.Normal) && (Catalog.TimeStamp > header.TimeStamp)) || ((header.CacheLevel == CatalogCacheLevel.Maintained) && !header.Cached))
						{
							Device.PopulateTableVar(program, header);
							if ((header.CacheLevel == CatalogCacheLevel.Maintained) && !header.Cached)
								header.Cached = true;
						}
					}
				}
			}
			object result = base.InternalExecute(program, planNode);
			if (planNode is CreateTableNode)
			{
				Schema.TableVar tableVar = ((CreateTableNode)planNode).Table;
				CatalogCacheLevel cacheLevel = (CatalogCacheLevel)Enum.Parse(typeof(CatalogCacheLevel), MetaData.GetTag(tableVar.MetaData, "Catalog.CacheLevel", "Normal"), true);
				if (!((cacheLevel == CatalogCacheLevel.StoreTable) || (cacheLevel == CatalogCacheLevel.StoreView)))
				{
					lock (Device.Headers)
					{
						CatalogHeader header = new CatalogHeader(tableVar, Device.Tables[tableVar], Int64.MinValue, cacheLevel);
						Device.Headers.Add(header);
					}
				}
			}
			return result;
		}

		#endregion
				
		#region Instructions
		
		#if LOGDDLINSTRUCTIONS
		protected abstract class DDLInstruction 
		{
			public virtual void Undo(CatalogDeviceSession session) {}
		}

		protected class DDLInstructionLog : List<DDLInstruction> {}
		
		protected class BeginTransactionInstruction : DDLInstruction {}
		
		/// <summary>Recomputes what a table variable derives from its definition, after an Undo has restored part of that definition.</summary>
		/// <remarks>
		/// <para>
		/// The ShouldDefault, ShouldValidate and ShouldChange flags, on a table variable and on each of its columns, are derived
		/// from the definition rather than part of it, and the engine reads them as a fast path: TableNode.Default, Validate and
		/// Change each return without doing anything when their flag is false, and ServerProcess decides what to tell a remote
		/// client about a table from them as well. Restoring a column's default or a constraint is therefore only half an undo.
		/// </para>
		/// <para>
		/// Call it last in an Undo, after the restore, so the recompute sees the whole restored definition; several instructions
		/// over one table variable may each recompute, and the last to run sees it fully restored, so their order does not
		/// matter. It goes through DetermineRemotable rather than setting the flags, so that the whole derived family is what a
		/// restart would compute. It walks the table's columns, so never call it from inside a loop over them.
		/// </para>
		/// <para>
		/// A view's override does not reset the flags before recomputing them (Schema.DerivedTableVar.DetermineRemotable), so on
		/// a view this can raise a flag but not lower one. A flag left raised costs a call that finds nothing to do, which is why
		/// only the lowered ones are defects.
		/// </para>
		/// </remarks>
		private void RedetermineRemotable(Schema.TableVar tableVar)
		{
			tableVar.DetermineRemotable(this);
		}

		/// <summary>
		/// <see cref="RedetermineRemotable(Schema.TableVar)"/> for an instruction that holds only the column. A column's
		/// back-reference to its table variable is null only while the column is detached, which an Undo never sees: a drop of
		/// the column is logged after the change to the column, so the reverse-order replay has already put the column back.
		/// </summary>
		private void RedetermineRemotable(Schema.TableVarColumn tableVarColumn)
		{
			if (tableVarColumn.TableVar != null)
				RedetermineRemotable(tableVarColumn.TableVar);
		}

		protected class SetUserNameInstruction : DDLInstruction
		{
			public SetUserNameInstruction(Schema.User user, string originalName) : base()
			{
				_user = user;
				_originalName = originalName;
			}
			
			private Schema.User _user;
			private string _originalName;
			
			public override void Undo(CatalogDeviceSession session)
			{
				_user.Name = _originalName;
			}
		}
		
		protected class SetUserPasswordInstruction : DDLInstruction
		{
			public SetUserPasswordInstruction(Schema.User user, string originalPassword) : base()
			{
				_user = user;
				_originalPassword = originalPassword;
			}
			
			private Schema.User _user;
			private string _originalPassword;
			
			public override void Undo(CatalogDeviceSession session)
			{
				_user.Password = _originalPassword;
			}
		}
		
		protected class SetDeviceUserIDInstruction : DDLInstruction
		{
			public SetDeviceUserIDInstruction(Schema.DeviceUser deviceUser, string originalUserID) : base()
			{
				_deviceUser = deviceUser;
				_originalUserID = originalUserID;
			}
			
			private Schema.DeviceUser _deviceUser;
			private string _originalUserID;
			
			public override void Undo(CatalogDeviceSession session)
			{
				_deviceUser.DeviceUserID = _originalUserID;
			}
		}
		
		protected class SetDeviceUserPasswordInstruction : DDLInstruction
		{
			public SetDeviceUserPasswordInstruction(Schema.DeviceUser deviceUser, string originalPassword) : base()
			{
				_deviceUser = deviceUser;
				_originalPassword = originalPassword;
			}
			
			private Schema.DeviceUser _deviceUser;
			private string _originalPassword;
			
			public override void Undo(CatalogDeviceSession session)
			{
				_deviceUser.DevicePassword = _originalPassword;
			}
		}
		
		protected class SetDeviceUserConnectionParametersInstruction : DDLInstruction
		{
			public SetDeviceUserConnectionParametersInstruction(Schema.DeviceUser deviceUser, string originalConnectionParameters) : base()
			{
				_deviceUser = deviceUser;
				_originalConnectionParameters = originalConnectionParameters;
			}
			
			private Schema.DeviceUser _deviceUser;
			private string _originalConnectionParameters;
			
			public override void Undo(CatalogDeviceSession session)
			{
				_deviceUser.ConnectionParameters = _originalConnectionParameters;
			}
		}
		
		protected class InsertLoadedLibraryInstruction : DDLInstruction
		{
			public InsertLoadedLibraryInstruction(Schema.LoadedLibrary loadedLibrary) : base()
			{
				_loadedLibrary = loadedLibrary;
			}
			
			private Schema.LoadedLibrary _loadedLibrary;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.ClearLoadedLibrary(_loadedLibrary);
			}
		}
		
		protected class DeleteLoadedLibraryInstruction : DDLInstruction
		{
			public DeleteLoadedLibraryInstruction(Schema.LoadedLibrary loadedLibrary) : base()
			{
				_loadedLibrary = loadedLibrary;
			}
			
			private Schema.LoadedLibrary _loadedLibrary;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.CacheLoadedLibrary(_loadedLibrary);
			}
		}
		
		protected class RegisterAssemblyInstruction : DDLInstruction
		{
			public RegisterAssemblyInstruction(Schema.LoadedLibrary loadedLibrary, Assembly assembly) : base()
			{
				_loadedLibrary = loadedLibrary;
				_assembly = assembly;
			}
			
			private Schema.LoadedLibrary _loadedLibrary;
			private Assembly _assembly;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.InternalUnregisterAssembly(_loadedLibrary, _assembly);
			}
		}
		
		protected class UnregisterAssemblyInstruction : DDLInstruction
		{
			public UnregisterAssemblyInstruction(Schema.LoadedLibrary loadedLibrary, Assembly assembly) : base()
			{
				_loadedLibrary = loadedLibrary;
				_assembly = assembly;
			}
			
			private Schema.LoadedLibrary _loadedLibrary;
			private Assembly _assembly;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.InternalRegisterAssembly(_loadedLibrary, _assembly);
			}
		}
		
		protected class CreateCatalogObjectInstruction : DDLInstruction
		{
			public CreateCatalogObjectInstruction(Schema.CatalogObject catalogObject) : base()
			{
				_catalogObject = catalogObject;
			}
			
			private Schema.CatalogObject _catalogObject;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.ClearCatalogObject(_catalogObject);
			}
		}
		
		protected class DropCatalogObjectInstruction : DDLInstruction
		{
			public DropCatalogObjectInstruction(Schema.CatalogObject catalogObject) : base()
			{
				_catalogObject = catalogObject;
			}
			
			private Schema.CatalogObject _catalogObject;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.CacheCatalogObject(_catalogObject);
			}
		}
		
		protected class AddDependenciesInstruction : DDLInstruction
		{
			public AddDependenciesInstruction(Schema.Object objectValue) : base()
			{
				_object = objectValue;
			}
			
			private Schema.Object _object;

			public override void Undo(CatalogDeviceSession session)
			{
				_object.Dependencies.Clear();
			}
		}
		
		protected class RemoveDependenciesInstruction : DDLInstruction
		{
			public RemoveDependenciesInstruction(Schema.Object objectValue, Schema.ObjectList originalDependencies) : base()
			{
				_object = objectValue;
				_originalDependencies = originalDependencies;
			}
			
			private Schema.Object _object;
			private Schema.ObjectList _originalDependencies;

			public override void Undo(CatalogDeviceSession session)
			{
				_object.AddDependencies(_originalDependencies);
				_object.DetermineRemotable(session);
			}
		}
		
		protected class CreateDeviceTableInstruction : DDLInstruction
		{
			public CreateDeviceTableInstruction(Schema.BaseTableVar baseTableVar) : base()
			{
				_baseTableVar = baseTableVar;
			}
			
			private Schema.BaseTableVar _baseTableVar;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.DropDeviceTable(_baseTableVar);
			}
		}
		
		protected class DropDeviceTableInstruction : DDLInstruction
		{
			public DropDeviceTableInstruction(Schema.BaseTableVar baseTableVar) : base()
			{
				_baseTableVar = baseTableVar;
			}
			
			private Schema.BaseTableVar _baseTableVar;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.CreateDeviceTable(_baseTableVar);
			}
		}
		
		protected class CreateSessionObjectInstruction : DDLInstruction
		{
			public CreateSessionObjectInstruction(Schema.CatalogObject sessionObject)
			{
				_sessionObject = sessionObject;
			}
			
			private Schema.CatalogObject _sessionObject;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.DropSessionObject(_sessionObject);
			}
		}
		
		protected class DropSessionObjectInstruction : DDLInstruction
		{
			public DropSessionObjectInstruction(Schema.CatalogObject sessionObject)
			{
				_sessionObject = sessionObject;
			}
			
			private Schema.CatalogObject _sessionObject;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.CreateSessionObject(_sessionObject);
			}
		}
		
		protected class CreateSessionOperatorInstruction : DDLInstruction
		{
			public CreateSessionOperatorInstruction(Schema.Operator sessionOperator)
			{
				_sessionOperator = sessionOperator;
			}
			
			private Schema.Operator _sessionOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DropSessionOperator(_sessionOperator);
			}
		}
		
		protected class DropSessionOperatorInstruction : DDLInstruction
		{
			public DropSessionOperatorInstruction(Schema.Operator sessionOperator)
			{
				_sessionOperator = sessionOperator;
			}
			
			private Schema.Operator _sessionOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				session.CreateSessionOperator(_sessionOperator);
			}
		}
		
		protected class AddImplicitConversionInstruction : DDLInstruction
		{
			public AddImplicitConversionInstruction(Schema.Conversion conversion) : base()
			{
				_conversion = conversion;
			}
			
			private Schema.Conversion _conversion;

			public override void Undo(CatalogDeviceSession session)
			{
				session.RemoveImplicitConversion(_conversion);
			}
		}
		
		protected class RemoveImplicitConversionInstruction : DDLInstruction
		{
			public RemoveImplicitConversionInstruction(Schema.Conversion conversion) : base()
			{
				_conversion = conversion;
			}
			
			private Schema.Conversion _conversion;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AddImplicitConversion(_conversion);
			}
		}
		
		protected class SetScalarTypeSortInstruction : DDLInstruction
		{
			public SetScalarTypeSortInstruction(Schema.ScalarType scalarType, Schema.Sort originalSort, bool isUnique, bool isEquality)
			{
				_scalarType = scalarType;
				_originalSort = originalSort;
				_isUnique = isUnique;
				_isEquality = isEquality;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Sort _originalSort;
			private bool _isUnique;
			private bool _isEquality;

			public override void Undo(CatalogDeviceSession session)
			{
				session.SetScalarTypeSort(_scalarType, _originalSort, _isUnique, _isEquality);
			}
		}
		
		protected class ClearScalarTypeEqualityOperatorInstruction : DDLInstruction
		{
			public ClearScalarTypeEqualityOperatorInstruction(Schema.ScalarType scalarType, Schema.Operator originalEqualityOperator)
			{
				_scalarType = scalarType;
				_originalEqualityOperator = originalEqualityOperator;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Operator _originalEqualityOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				_scalarType.EqualityOperator = _originalEqualityOperator;
			}
		}
		
		protected class ClearScalarTypeComparisonOperatorInstruction : DDLInstruction
		{
			public ClearScalarTypeComparisonOperatorInstruction(Schema.ScalarType scalarType, Schema.Operator originalComparisonOperator)
			{
				_scalarType = scalarType;
				_originalComparisonOperator = originalComparisonOperator;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Operator _originalComparisonOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				_scalarType.ComparisonOperator = _originalComparisonOperator;
			}
		}
		
		protected class ClearScalarTypeIsSpecialOperatorInstruction : DDLInstruction
		{
			public ClearScalarTypeIsSpecialOperatorInstruction(Schema.ScalarType scalarType, Schema.Operator originalIsSpecialOperator)
			{
				_scalarType = scalarType;
				_originalIsSpecialOperator = originalIsSpecialOperator;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Operator _originalIsSpecialOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				_scalarType.IsSpecialOperator = _originalIsSpecialOperator;
			}
		}
		
		protected class ClearRepresentationSelectorInstruction : DDLInstruction
		{
			public ClearRepresentationSelectorInstruction(Schema.Representation representation, Schema.Operator originalSelector)
			{
				_representation = representation;
				_originalSelector = originalSelector;
			}
			
			private Schema.Representation _representation;
			private Schema.Operator _originalSelector;

			public override void Undo(CatalogDeviceSession session)
			{
				_representation.Selector = _originalSelector;
			}
		}
		
		protected class ClearPropertyReadAccessorInstruction : DDLInstruction
		{
			public ClearPropertyReadAccessorInstruction(Schema.Property property, Schema.Operator originalReadAccessor)
			{
				_property = property;
				_originalReadAccessor = originalReadAccessor;
			}
			
			private Schema.Property _property;
			private Schema.Operator _originalReadAccessor;

			public override void Undo(CatalogDeviceSession session)
			{
				_property.ReadAccessor = _originalReadAccessor;
			}
		}
		
		protected class ClearPropertyWriteAccessorInstruction : DDLInstruction
		{
			public ClearPropertyWriteAccessorInstruction(Schema.Property property, Schema.Operator originalWriteAccessor)
			{
				_property = property;
				_originalWriteAccessor = originalWriteAccessor;
			}
			
			private Schema.Property _property;
			private Schema.Operator _originalWriteAccessor;

			public override void Undo(CatalogDeviceSession session)
			{
				_property.WriteAccessor = _originalWriteAccessor;
			}
		}
		
		protected class ClearSpecialSelectorInstruction : DDLInstruction
		{
			public ClearSpecialSelectorInstruction(Schema.Special special, Schema.Operator originalSelector)
			{
				_special = special;
				_originalSelector = originalSelector;
			}
			
			private Schema.Special _special;
			private Schema.Operator _originalSelector;

			public override void Undo(CatalogDeviceSession session)
			{
				_special.Selector = _originalSelector;
			}
		}
		
		protected class ClearSpecialComparerInstruction : DDLInstruction
		{
			public ClearSpecialComparerInstruction(Schema.Special special, Schema.Operator originalComparer)
			{
				_special = special;
				_originalComparer = originalComparer;
			}
			
			private Schema.Special _special;
			private Schema.Operator _originalComparer;

			public override void Undo(CatalogDeviceSession session)
			{
				_special.Comparer = _originalComparer;
			}
		}
		
		protected class AlterMetaDataInstruction : DDLInstruction
		{
			public AlterMetaDataInstruction(Schema.Object objectValue, MetaData originalMetaData)
			{
				_object = objectValue;
				_originalMetaData = originalMetaData;
			}
			
			private Schema.Object _object;
			private MetaData _originalMetaData;

			public override void Undo(CatalogDeviceSession session)
			{
				_object.MetaData = _originalMetaData;
			}
		}
		
		protected class AlterClassDefinitionInstruction : DDLInstruction
		{
			/// <summary>Takes the class definition, and the instance's value for every property the alteration touches, as they stand - so construct it before the change it is to undo.</summary>
			public AlterClassDefinitionInstruction(ClassDefinition classDefinition, AlterClassDefinition alterClassDefinition, object instance)
			{
				_classDefinition = classDefinition;
				_originalClassDefinition = (ClassDefinition)classDefinition.Clone();
				_instance = instance;
				if (instance != null)
				{
					RecordProperties(alterClassDefinition.CreateAttributes);
					RecordProperties(alterClassDefinition.AlterAttributes);
					RecordProperties(alterClassDefinition.DropAttributes);
				}
			}
			
			private ClassDefinition _classDefinition;
			private ClassDefinition _originalClassDefinition;
			private object _instance;
			/// <summary>The instance's own value for each attribute the alteration names, as it stood before; empty when the class definition has no live instance.</summary>
			private List<KeyValuePair<string, string>> _originalProperties = new List<KeyValuePair<string, string>>();

			// An unreadable attribute name is left out rather than reported, because this recording runs before the alteration: a name
			// the instance has no property of at all is one the alteration raises PropertyNotFound for itself, and raising here would
			// move that failure.
			// NOTE: a name it has a property of but no *getter* for is a different case the same skip swallows - the undo is then
			// silently unable to restore it, which is the defect this instruction exists to close - and a property type that does not
			// round-trip through a string is restored only approximately. Neither is reachable here (checked 2026-11-23: no write-only
			// property, and none of such a type, on the device and schema classes). A custom C# extension could bring either; then
			// record the value as an object rather than as a string, and refuse the write-only case outright.
			private void RecordProperties(ClassAttributeDefinitions attributeDefinitions)
			{
				foreach (ClassAttributeDefinition attributeDefinition in attributeDefinitions)
				{
					string propertyValue;
					if (ClassLoader.TryGetProperty(_instance, attributeDefinition.AttributeName, out propertyValue))
						_originalProperties.Add(new KeyValuePair<string, string>(attributeDefinition.AttributeName, propertyValue));
				}
			}

			/// <remarks>
			/// Puts the copy of the class definition back, rather than applying the inverse alteration, and then the recorded property
			/// values back on the instance. Two reasons not to lean on the inverse: the inverse of a created attribute is a drop, which
			/// never touched the instance - the defect this instruction records for - and a create that threw before adding its attribute
			/// leaves the inverse with an attribute to drop that is not there, which failed the rest of the undo with it.
			/// Recorded values are also the only ones an undo may use: instructions are undone in reverse order of recording, so
			/// anything recomputed here would read state a later-undone instruction has still to put back.
			/// </remarks>
			public override void Undo(CatalogDeviceSession session)
			{
				_classDefinition.ClassName = _originalClassDefinition.ClassName;
				_classDefinition.Attributes.Clear();
				foreach (ClassAttributeDefinition attributeDefinition in _originalClassDefinition.Attributes)
					_classDefinition.Attributes.Add(attributeDefinition.Clone());
				
				foreach (KeyValuePair<string, string> originalProperty in _originalProperties)
					ClassLoader.SetProperty(_instance, originalProperty.Key, originalProperty.Value);
			}
		}
		
		protected class AttachCatalogConstraintInstruction : DDLInstruction
		{
			public AttachCatalogConstraintInstruction(Schema.CatalogConstraint catalogConstraint)
			{
				_catalogConstraint = catalogConstraint;
			}
			
			private Schema.CatalogConstraint _catalogConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				CreateConstraintNode.DetachConstraint(_catalogConstraint, _catalogConstraint.Node);
			}
		}
		
		protected class DetachCatalogConstraintInstruction : DDLInstruction
		{
			public DetachCatalogConstraintInstruction(Schema.CatalogConstraint catalogConstraint)
			{
				_catalogConstraint = catalogConstraint;
			}
			
			private Schema.CatalogConstraint _catalogConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				CreateConstraintNode.AttachConstraint(_catalogConstraint, _catalogConstraint.Node);
			}
		}
		
		protected class SetCatalogConstraintNodeInstruction : DDLInstruction
		{
			public SetCatalogConstraintNodeInstruction(Schema.CatalogConstraint catalogConstraint, PlanNode originalNode)
			{
				_catalogConstraint = catalogConstraint;
				_originalNode = originalNode;
			}

			private Schema.CatalogConstraint _catalogConstraint;
			private PlanNode _originalNode;

			public override void Undo(CatalogDeviceSession session)
			{
				_catalogConstraint.Node = _originalNode;
			}
		}
		
		protected class AttachKeyInstruction : DDLInstruction
		{
			public AttachKeyInstruction(Schema.TableVar tableVar, Schema.Key key)
			{
				_tableVar = tableVar;
				_key = key;
			}
			
			private Schema.TableVar _tableVar;
			private Schema.Key _key;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachKey(_tableVar, _key);
			}
		}
		
		protected class DetachKeyInstruction : DDLInstruction
		{
			public DetachKeyInstruction(Schema.TableVar tableVar, Schema.Key key)
			{
				_tableVar = tableVar;
				_key = key;
			}
			
			private Schema.TableVar _tableVar;
			private Schema.Key _key;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachKey(_tableVar, _key);
			}
		}
		
		protected class AttachOrderInstruction : DDLInstruction
		{
			public AttachOrderInstruction(Schema.TableVar tableVar, Schema.Order order)
			{
				_tableVar = tableVar;
				_order = order;
			}
			
			private Schema.TableVar _tableVar;
			private Schema.Order _order;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachOrder(_tableVar, _order);
			}
		}
		
		protected class DetachOrderInstruction : DDLInstruction
		{
			public DetachOrderInstruction(Schema.TableVar tableVar, Schema.Order order)
			{
				_tableVar = tableVar;
				_order = order;
			}
			
			private Schema.TableVar _tableVar;
			private Schema.Order _order;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachOrder(_tableVar, _order);
			}
		}
		
		protected class AttachTableVarConstraintInstruction : DDLInstruction
		{
			public AttachTableVarConstraintInstruction(Schema.TableVar tableVar, Schema.TableVarConstraint tableVarConstraint)
			{
				_tableVar = tableVar;
				_tableVarConstraint = tableVarConstraint;
			}
			
			private Schema.TableVar _tableVar;
			private Schema.TableVarConstraint _tableVarConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachTableVarConstraint(_tableVar, _tableVarConstraint);
				session.RedetermineRemotable(_tableVar);
			}
		}
		
		protected class DetachTableVarConstraintInstruction : DDLInstruction
		{
			public DetachTableVarConstraintInstruction(Schema.TableVar tableVar, Schema.TableVarConstraint tableVarConstraint)
			{
				_tableVar = tableVar;
				_tableVarConstraint = tableVarConstraint;
			}
			
			private Schema.TableVar _tableVar;
			private Schema.TableVarConstraint _tableVarConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachTableVarConstraint(_tableVar, _tableVarConstraint);
				session.RedetermineRemotable(_tableVar);
			}
		}
		
		protected class AttachTableVarColumnInstruction : DDLInstruction
		{
			public AttachTableVarColumnInstruction(Schema.BaseTableVar tableVar, Schema.TableVarColumn tableVarColumn)
			{
				_tableVar = tableVar;
				_tableVarColumn = tableVarColumn;
			}
			
			private Schema.BaseTableVar _tableVar;
			private Schema.TableVarColumn _tableVarColumn;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachTableVarColumn(_tableVar, _tableVarColumn);
				session.RedetermineRemotable(_tableVar);
			}
		}
		
		protected class DetachTableVarColumnInstruction : DDLInstruction
		{
			public DetachTableVarColumnInstruction(Schema.BaseTableVar tableVar, Schema.TableVarColumn tableVarColumn)
			{
				_tableVar = tableVar;
				_tableVarColumn = tableVarColumn;
			}
			
			private Schema.BaseTableVar _tableVar;
			private Schema.TableVarColumn _tableVarColumn;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachTableVarColumn(_tableVar, _tableVarColumn);
				session.RedetermineRemotable(_tableVar);
			}
		}
		
		protected class AttachScalarTypeConstraintInstruction : DDLInstruction
		{
			public AttachScalarTypeConstraintInstruction(Schema.ScalarType scalarType, Schema.ScalarTypeConstraint scalarTypeConstraint)
			{
				_scalarType = scalarType;
				_scalarTypeConstraint = scalarTypeConstraint;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.ScalarTypeConstraint _scalarTypeConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachScalarTypeConstraint(_scalarType, _scalarTypeConstraint);
			}
		}
		
		protected class DetachScalarTypeConstraintInstruction : DDLInstruction
		{
			public DetachScalarTypeConstraintInstruction(Schema.ScalarType scalarType, Schema.ScalarTypeConstraint scalarTypeConstraint)
			{
				_scalarType = scalarType;
				_scalarTypeConstraint = scalarTypeConstraint;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.ScalarTypeConstraint _scalarTypeConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachScalarTypeConstraint(_scalarType, _scalarTypeConstraint);
			}
		}
		
		protected class AttachTableVarColumnConstraintInstruction : DDLInstruction
		{
			public AttachTableVarColumnConstraintInstruction(Schema.TableVarColumn tableVarColumn, Schema.TableVarColumnConstraint tableVarColumnConstraint)
			{
				_tableVarColumn = tableVarColumn;
				_tableVarColumnConstraint = tableVarColumnConstraint;
			}
			
			private Schema.TableVarColumn _tableVarColumn;
			private Schema.TableVarColumnConstraint _tableVarColumnConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachTableVarColumnConstraint(_tableVarColumn, _tableVarColumnConstraint);
				session.RedetermineRemotable(_tableVarColumn);
			}
		}
		
		protected class DetachTableVarColumnConstraintInstruction : DDLInstruction
		{
			public DetachTableVarColumnConstraintInstruction(Schema.TableVarColumn tableVarColumn, Schema.TableVarColumnConstraint tableVarColumnConstraint)
			{
				_tableVarColumn = tableVarColumn;
				_tableVarColumnConstraint = tableVarColumnConstraint;
			}
			
			private Schema.TableVarColumn _tableVarColumn;
			private Schema.TableVarColumnConstraint _tableVarColumnConstraint;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachTableVarColumnConstraint(_tableVarColumn, _tableVarColumnConstraint);
				session.RedetermineRemotable(_tableVarColumn);
			}
		}
		
		protected class AttachSpecialInstruction : DDLInstruction
		{
			public AttachSpecialInstruction(Schema.ScalarType scalarType, Schema.Special special)
			{
				_scalarType = scalarType;
				_special = special;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Special _special;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachSpecial(_scalarType, _special);
			}
		}
		
		protected class DetachSpecialInstruction : DDLInstruction
		{
			public DetachSpecialInstruction(Schema.ScalarType scalarType, Schema.Special special)
			{
				_scalarType = scalarType;
				_special = special;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Special _special;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachSpecial(_scalarType, _special);
			}
		}
		
		protected class AttachRepresentationInstruction : DDLInstruction
		{
			public AttachRepresentationInstruction(Schema.ScalarType scalarType, Schema.Representation representation)
			{
				_scalarType = scalarType;
				_representation = representation;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Representation _representation;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachRepresentation(_scalarType, _representation);
			}
		}
		
		protected class DetachRepresentationInstruction : DDLInstruction
		{
			public DetachRepresentationInstruction(Schema.ScalarType scalarType, Schema.Representation representation)
			{
				_scalarType = scalarType;
				_representation = representation;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Representation _representation;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachRepresentation(_scalarType, _representation);
			}
		}
		
		protected class AttachPropertyInstruction : DDLInstruction
		{
			public AttachPropertyInstruction(Schema.Representation representation, Schema.Property property)
			{
				_representation = representation;
				_property = property;
			}
			
			private Schema.Representation _representation;
			private Schema.Property _property;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachProperty(_representation, _property);
			}
		}
		
		protected class DetachPropertyInstruction : DDLInstruction
		{
			public DetachPropertyInstruction(Schema.Representation representation, Schema.Property property)
			{
				_representation = representation;
				_property = property;
			}
			
			private Schema.Representation _representation;
			private Schema.Property _property;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachProperty(_representation, _property);
			}
		}
		
		protected class SetScalarTypeDefaultInstruction : DDLInstruction
		{
			public SetScalarTypeDefaultInstruction(Schema.ScalarType scalarType, Schema.ScalarTypeDefault originalDefault)
			{
				_scalarType = scalarType;
				_originalDefault = originalDefault;
			}

			private Schema.ScalarType _scalarType;
			private Schema.ScalarTypeDefault _originalDefault;

			public override void Undo(CatalogDeviceSession session)
			{
				_scalarType.Default = _originalDefault;
			}
		}
		
		protected class SetTableVarColumnDefaultInstruction : DDLInstruction
		{
			public SetTableVarColumnDefaultInstruction(Schema.TableVarColumn tableVarColumn, Schema.TableVarColumnDefault originalDefault)
			{
				_tableVarColumn = tableVarColumn;
				_originalDefault = originalDefault;
			}

			private Schema.TableVarColumn _tableVarColumn;
			private Schema.TableVarColumnDefault _originalDefault;

			public override void Undo(CatalogDeviceSession session)
			{
				_tableVarColumn.Default = _originalDefault;
				session.RedetermineRemotable(_tableVarColumn);
			}
		}
		
		protected class SetTableVarColumnIsNilableInstruction : DDLInstruction
		{
			public SetTableVarColumnIsNilableInstruction(Schema.TableVarColumn tableVarColumn, bool originalIsNilable)
			{
				_tableVarColumn = tableVarColumn;
				_originalIsNilable = originalIsNilable;
			}

			private Schema.TableVarColumn _tableVarColumn;
			private bool _originalIsNilable;

			public override void Undo(CatalogDeviceSession session)
			{
				_tableVarColumn.IsNilable = _originalIsNilable;
				// Nothing derived reads nilability today; the recompute is here so that the rule is the same for every
				// instruction that restores part of a column's definition.
				session.RedetermineRemotable(_tableVarColumn);
			}
		}
		
		protected class SetScalarTypeIsSpecialOperatorInstruction : DDLInstruction
		{
			public SetScalarTypeIsSpecialOperatorInstruction(Schema.ScalarType scalarType, Schema.Operator originalOperator)
			{
				_scalarType = scalarType;
				_originalOperator = originalOperator;
			}

			private Schema.ScalarType _scalarType;
			private Schema.Operator _originalOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				_scalarType.IsSpecialOperator = _originalOperator;
			}
		}
		
		protected class SetOperatorBlockNodeInstruction : DDLInstruction
		{
			public SetOperatorBlockNodeInstruction(Schema.OperatorBlock operatorBlock, PlanNode originalNode)
			{
				_operatorBlock = operatorBlock;
				_originalNode = originalNode;
			}

			private Schema.OperatorBlock _operatorBlock;
			private PlanNode _originalNode;

			public override void Undo(CatalogDeviceSession session)
			{
				_operatorBlock.BlockNode = _originalNode;
			}
		}
		
		protected class AttachReferenceInstruction : DDLInstruction
		{
			public AttachReferenceInstruction(Schema.Reference reference)
			{
				_reference = reference;
			}
			
			private Schema.Reference _reference;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachReference(_reference);
			}
		}
		
		protected class DetachReferenceInstruction : DDLInstruction
		{
			public DetachReferenceInstruction(Schema.Reference reference)
			{
				_reference = reference;
			}
			
			private Schema.Reference _reference;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachReference(_reference);
			}
		}
		
		protected class AttachDeviceScalarTypeInstruction : DDLInstruction
		{
			public AttachDeviceScalarTypeInstruction(Schema.DeviceScalarType deviceScalarType)
			{
				_deviceScalarType = deviceScalarType;
			}
			
			private Schema.DeviceScalarType _deviceScalarType;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachDeviceScalarType(_deviceScalarType);
			}
		}
		
		protected class DetachDeviceScalarTypeInstruction : DDLInstruction
		{
			public DetachDeviceScalarTypeInstruction(Schema.DeviceScalarType deviceScalarType)
			{
				_deviceScalarType = deviceScalarType;
			}
			
			private Schema.DeviceScalarType _deviceScalarType;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachDeviceScalarType(_deviceScalarType);
			}
		}
		
		protected class AttachDeviceOperatorInstruction : DDLInstruction
		{
			public AttachDeviceOperatorInstruction(Schema.DeviceOperator deviceOperator)
			{
				_deviceOperator = deviceOperator;
			}
			
			private Schema.DeviceOperator _deviceOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachDeviceOperator(_deviceOperator);
			}
		}
		
		protected class DetachDeviceOperatorInstruction : DDLInstruction
		{
			public DetachDeviceOperatorInstruction(Schema.DeviceOperator deviceOperator)
			{
				_deviceOperator = deviceOperator;
			}
			
			private Schema.DeviceOperator _deviceOperator;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachDeviceOperator(_deviceOperator);
			}
		}
		
		protected class AttachTableMapInstruction : DDLInstruction
		{
			public AttachTableMapInstruction(ApplicationTransactionDevice device, TableMap tableMap)
			{
				_device = device;
				_tableMap = tableMap;
			}
			
			private ApplicationTransactionDevice _device;
			private TableMap _tableMap;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachTableMap(_device, _tableMap);
			}
		}
		
		protected class DetachTableMapInstruction : DDLInstruction
		{
			public DetachTableMapInstruction(ApplicationTransactionDevice device, TableMap tableMap)
			{
				_device = device;
				_tableMap = tableMap;
			}
			
			private ApplicationTransactionDevice _device;
			private TableMap _tableMap;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachTableMap(_device, _tableMap);
			}
		}
		
		protected class AttachOperatorMapInstruction : DDLInstruction
		{
			public AttachOperatorMapInstruction(ApplicationTransaction.OperatorMap operatorMap, Schema.Operator operatorValue)
			{
				_operatorMap = operatorMap;
				_operator = operatorValue;
			}
			
			private ApplicationTransaction.OperatorMap _operatorMap;
			private Schema.Operator _operator;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachOperatorMap(_operatorMap, _operator);
			}
		}
		
		protected class DetachOperatorMapInstruction : DDLInstruction
		{
			public DetachOperatorMapInstruction(ApplicationTransaction.OperatorMap operatorMap, Schema.Operator operatorValue)
			{
				_operatorMap = operatorMap;
				_operator = operatorValue;
			}
			
			private ApplicationTransaction.OperatorMap _operatorMap;
			private Schema.Operator _operator;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachOperatorMap(_operatorMap, _operator);
			}
		}
		
/*
		protected class AttachDeviceScalarTypeDeviceOperatorInstruction : DDLInstruction
		{
			public AttachDeviceScalarTypeDeviceOperatorInstruction(Schema.DeviceScalarType ADeviceScalarType, Schema.DeviceOperator ADeviceOperator)
			{
				FDeviceScalarType = ADeviceScalarType;
				FDeviceOperator = ADeviceOperator;
			}
			
			private Schema.DeviceScalarType FDeviceScalarType;
			private Schema.DeviceOperator FDeviceOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachDeviceScalarTypeDeviceOperator(FDeviceScalarType, FDeviceOperator);
			}
		}
		
		protected class DetachDeviceScalarTypeDeviceOperatorInstruction : DDLInstruction
		{
			public DetachDeviceScalarTypeDeviceOperatorInstruction(Schema.DeviceScalarType ADeviceScalarType, Schema.DeviceOperator ADeviceOperator)
			{
				FDeviceScalarType = ADeviceScalarType;
				FDeviceOperator = ADeviceOperator;
			}
			
			private Schema.DeviceScalarType FDeviceScalarType;
			private Schema.DeviceOperator FDeviceOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachDeviceScalarTypeDeviceOperator(FDeviceScalarType, FDeviceOperator);
			}
		}
*/
		
		protected class SetDeviceReconcileModeInstruction : DDLInstruction
		{
			public SetDeviceReconcileModeInstruction(Schema.Device device, ReconcileMode originalReconcileMode)
			{
				_device = device;
				_originalReconcileMode = originalReconcileMode;
			}
			
			private Schema.Device _device;
			private ReconcileMode _originalReconcileMode;

			public override void Undo(CatalogDeviceSession session)
			{
				_device.ReconcileMode = _originalReconcileMode;
			}
		}
		
		protected class SetDeviceReconcileMasterInstruction : DDLInstruction
		{
			public SetDeviceReconcileMasterInstruction(Schema.Device device, ReconcileMaster originalReconcileMaster)
			{
				_device = device;
				_originalReconcileMaster = originalReconcileMaster;
			}
			
			private Schema.Device _device;
			private ReconcileMaster _originalReconcileMaster;

			public override void Undo(CatalogDeviceSession session)
			{
				_device.ReconcileMaster = _originalReconcileMaster;
			}
		}

		protected class StartDeviceInstruction : DDLInstruction
		{
			public StartDeviceInstruction(Schema.Device device) : base()
			{
				_device = device;
			}
			
			private Schema.Device _device;

			public override void Undo(CatalogDeviceSession session)
			{
				session.StopDevice(_device, true);
			}
		}
		
		protected class RegisterDeviceInstruction : DDLInstruction
		{
			public RegisterDeviceInstruction(Schema.Device device) : base()
			{
				_device = device;
			}
			
			private Schema.Device _device;

			public override void Undo(CatalogDeviceSession session)
			{
				session.UnregisterDevice(_device);
			}
		}
		
		protected class AttachEventHandlerInstruction : DDLInstruction
		{
			public AttachEventHandlerInstruction(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
			{
				_eventHandler = eventHandler;
				_eventSource = eventSource;
				_eventSourceColumnIndex = eventSourceColumnIndex;
				_beforeOperatorNames = beforeOperatorNames;
			}
			
			private Schema.EventHandler _eventHandler;
			private Schema.Object _eventSource;
			private int _eventSourceColumnIndex;
			private List<string> _beforeOperatorNames;

			public override void Undo(CatalogDeviceSession session)
			{
				session.DetachEventHandler(_eventHandler, _eventSource, _eventSourceColumnIndex);
			}
		}
		
		protected class MoveEventHandlerInstruction : DDLInstruction
		{
			public MoveEventHandlerInstruction(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
			{
				_eventHandler = eventHandler;
				_eventSource = eventSource;
				_eventSourceColumnIndex = eventSourceColumnIndex;
				_beforeOperatorNames = beforeOperatorNames;
			}
			
			private Schema.EventHandler _eventHandler;
			private Schema.Object _eventSource;
			private int _eventSourceColumnIndex;
			private List<string> _beforeOperatorNames;

			public override void Undo(CatalogDeviceSession session)
			{
				session.MoveEventHandler(_eventHandler, _eventSource, _eventSourceColumnIndex, _beforeOperatorNames);
			}
		}
		
		protected class DetachEventHandlerInstruction : DDLInstruction
		{
			public DetachEventHandlerInstruction(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
			{
				_eventHandler = eventHandler;
				_eventSource = eventSource;
				_eventSourceColumnIndex = eventSourceColumnIndex;
				_beforeOperatorNames = beforeOperatorNames;
			}
			
			private Schema.EventHandler _eventHandler;
			private Schema.Object _eventSource;
			private int _eventSourceColumnIndex;
			private List<string> _beforeOperatorNames;

			public override void Undo(CatalogDeviceSession session)
			{
				session.AttachEventHandler(_eventHandler, _eventSource, _eventSourceColumnIndex, _beforeOperatorNames);
			}
		}

		protected class SetCatalogObjectOwnerInstruction : DDLInstruction
		{
			public SetCatalogObjectOwnerInstruction(Schema.CatalogObject catalogObject, Schema.User originalOwner)
			{
				_catalogObject = catalogObject;
				_originalOwner = originalOwner;
			}

			private Schema.CatalogObject _catalogObject;
			private Schema.User _originalOwner;

			public override void Undo(CatalogDeviceSession session)
			{
				session.CacheCatalogObjectOwner(_catalogObject, _originalOwner);
			}
		}

		protected class CreateServerLinkUserInstruction : DDLInstruction
		{
			public CreateServerLinkUserInstruction(Schema.ServerLink serverLink, Schema.ServerLinkUser serverLinkUser)
			{
				_serverLink = serverLink;
				_serverLinkUser = serverLinkUser;
			}

			private Schema.ServerLink _serverLink;
			private Schema.ServerLinkUser _serverLinkUser;

			public override void Undo(CatalogDeviceSession session)
			{
				_serverLink.Users.Remove(_serverLinkUser.UserID);
			}
		}

		protected class DropServerLinkUserInstruction : DDLInstruction
		{
			public DropServerLinkUserInstruction(Schema.ServerLink serverLink, Schema.ServerLinkUser serverLinkUser)
			{
				_serverLink = serverLink;
				_serverLinkUser = serverLinkUser;
			}

			private Schema.ServerLink _serverLink;
			private Schema.ServerLinkUser _serverLinkUser;

			/// <remarks>The instance that was removed goes back, not a copy of it: it keeps its user id and its back-reference to the link, and instructions recorded earlier for the same mapping undo onto that same object.</remarks>
			public override void Undo(CatalogDeviceSession session)
			{
				_serverLink.Users.Add(_serverLinkUser);
			}
		}

		protected class SetServerLinkUserIDInstruction : DDLInstruction
		{
			public SetServerLinkUserIDInstruction(Schema.ServerLinkUser serverLinkUser, string originalServerLinkUserID)
			{
				_serverLinkUser = serverLinkUser;
				_originalServerLinkUserID = originalServerLinkUserID;
			}

			private Schema.ServerLinkUser _serverLinkUser;
			private string _originalServerLinkUserID;

			public override void Undo(CatalogDeviceSession session)
			{
				_serverLinkUser.ServerLinkUserID = _originalServerLinkUserID;
			}
		}

		protected class SetServerLinkUserPasswordInstruction : DDLInstruction
		{
			public SetServerLinkUserPasswordInstruction(Schema.ServerLinkUser serverLinkUser, string originalServerLinkPassword)
			{
				_serverLinkUser = serverLinkUser;
				_originalServerLinkPassword = originalServerLinkPassword;
			}

			private Schema.ServerLinkUser _serverLinkUser;
			private string _originalServerLinkPassword;

			public override void Undo(CatalogDeviceSession session)
			{
				_serverLinkUser.ServerLinkPassword = _originalServerLinkPassword;
			}
		}

		protected class ApplyServerLinkMetaDataInstruction : DDLInstruction
		{
			/// <summary>Takes the link's connection settings as they stand, so construct it before the change it is to undo.</summary>
			public ApplyServerLinkMetaDataInstruction(Schema.ServerLink serverLink)
			{
				_serverLink = serverLink;
				_originalHostName = serverLink.HostName;
				_originalInstanceName = serverLink.InstanceName;
				_originalOverridePortNumber = serverLink.OverridePortNumber;
				_originalUseSecureConnection = serverLink.UseSecureConnection;
				_originalOverrideListenerPortNumber = serverLink.OverrideListenerPortNumber;
				_originalUseSecureListenerConnection = serverLink.UseSecureListenerConnection;
				_originalUseSessionInfo = serverLink.UseSessionInfo;
			}

			private Schema.ServerLink _serverLink;
			private string _originalHostName;
			private string _originalInstanceName;
			private int _originalOverridePortNumber;
			private bool _originalUseSecureConnection;
			private int _originalOverrideListenerPortNumber;
			private bool _originalUseSecureListenerConnection;
			private bool _originalUseSessionInfo;

			/// <remarks>
			/// Puts the recorded values back rather than recomputing them from the link's metadata. Instructions are undone in
			/// reverse order of recording, so this runs before the AlterMetaDataInstruction that the same statement recorded
			/// restores the metadata: a recomputation here would read the metadata the rolled-back statement left in place.
			/// </remarks>
			public override void Undo(CatalogDeviceSession session)
			{
				_serverLink.HostName = _originalHostName;
				_serverLink.InstanceName = _originalInstanceName;
				_serverLink.OverridePortNumber = _originalOverridePortNumber;
				_serverLink.UseSecureConnection = _originalUseSecureConnection;
				_serverLink.OverrideListenerPortNumber = _originalOverrideListenerPortNumber;
				_serverLink.UseSecureListenerConnection = _originalUseSecureListenerConnection;
				_serverLink.UseSessionInfo = _originalUseSessionInfo;
			}
		}

		protected DDLInstructionLog _instructions = new DDLInstructionLog();
		
		#endif
		
		#endregion

		#region Transactions
		
		protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
		{
			base.InternalBeginTransaction(isolationLevel);

			#if LOGDDLINSTRUCTIONS
			_instructions.Add(new BeginTransactionInstruction());
			#endif
		}

		/// <summary>Begins the catalog store's write transaction for the open process transaction, if it has not begun.</summary>
		/// <remarks>
		/// A store write begins it anyway; a statement that is about to change the catalog calls this first, before it takes
		/// the catalog's lock, so that it never waits for the store's write lock while holding the catalog's lock (the
		/// session holding the store's write lock may be waiting for that lock). A catalog without a store does nothing.
		/// </remarks>
		public virtual void BeginStoreWriteTransaction()
		{
			// virtual
		}

		protected override void InternalPrepareTransaction()
		{
			base.InternalPrepareTransaction();
		}

		protected override void InternalCommitTransaction()
		{
			base.InternalCommitTransaction();

			#if LOGDDLINSTRUCTIONS
			if (Transactions.Count > 1)
			{
				for (int index = _instructions.Count - 1; index >= 0; index--)
					if (_instructions[index] is BeginTransactionInstruction)
					{
						_instructions.RemoveAt(index);
						break;
					}
			}
			else
				_instructions.Clear();
			#endif

			bool committed = false;
			try
			{
				InternalAfterCommitTransaction();
				committed = true;
			}
			finally
			{
				// After the store commit, which InternalAfterCommitTransaction makes. A failed commit keeps the record for the
				// rollback that follows it.
				if (Transactions.Count == 1)
					ClearChangedRights(committed);
			}
				
			// NOTE: a right change made by a deferred device stop at the outermost level autocommits and is cleared at once, but stays
			// in the record until the session's next transaction ends; none is known, if one appears, replay again after the stops.
			_deferredDeviceStops.Commit(Transactions.Count, InternalStopDevice);
			// Last, and so also after the store commit: closing a remote session makes a network call to the other server, which must
			// not happen while the transaction that session took part in is still open.
			_deferredRemoteSessionCloses.Commit(Transactions.Count, InternalCloseRemoteSession);
		}

		protected virtual void InternalAfterCommitTransaction()
		{
			// virtual
		}

		protected override void InternalRollbackTransaction()
		{
			base.InternalRollbackTransaction();
			
			bool undidDDL = false;
			try
			{
				#if LOGDDLINSTRUCTIONS
				for (int index = _instructions.Count - 1; index >= 0; index--)
				{
					DDLInstruction instruction = _instructions[index];
					_instructions.RemoveAt(index);
					if (instruction is BeginTransactionInstruction)
						break;
					else
					{
						undidDDL = true;
						try
						{
							instruction.Undo(this);
						}
						catch (Exception exception)
						{
							// Log the exception and continue, not really much that can be done, should try to undo as many operations as possible
							// In at least one case, the error may be safely ignored anyway (storage object does not exist because it has already been rolled back by the device transaction rollback)
							ServerProcess.ServerSession.Server.LogError(new ServerException(ServerException.Codes.RollbackError, ErrorSeverity.System, exception, exception.ToString()));
						}
					}
				}
				#endif
			}
			finally
			{
				if (undidDDL)
					InvalidateCachesAfterUndo();

				try
				{
					InternalAfterRollbackTransaction();
				}
				finally
				{
					// After the store rollback, at every level: this session cached answers from the level's uncommitted writes.
					ClearChangedRights(Transactions.Count == 1);
				}
					
				_deferredDeviceStops.Rollback(Transactions.Count);
				_deferredRemoteSessionCloses.Rollback(Transactions.Count);
			}
		}

		/// <summary>Invalidates what the server cached from the catalog as it stood before the rollback undid its DDL.</summary>
		/// <remarks>
		/// Mirrors what the forward drop nodes invalidate (plan cache, client cache and derivation timestamps), plus the
		/// operator resolution and conversion path caches, which this session clears when it makes a forward change
		/// (AddImplicitConversion, CreateOperator and their inverses).
		/// Both caches are cleared whole: an undo can restore a dropped operator, conversion or type as well as remove a
		/// created one, which changes resolutions the per-object Clear overloads would not find.
		/// </remarks>
		private void InvalidateCachesAfterUndo()
		{
			// NOTE: flushes these caches for every session whenever anything was undone, including objects a transaction creates without DDL (a device started on first use, a device operator map the compiler generates for a like type); if a workload that repeatedly rolls back such transactions shows plan-cache churn, invalidate per undone object instead.
			Catalog.UpdatePlanCacheTimeStamp();
			Catalog.UpdateCacheTimeStamp();
			Catalog.UpdateDerivationTimeStamp();
			Catalog.OperatorResolutionCache.Clear();
			Catalog.ConversionPathCache.Clear();
		}

		protected virtual void InternalAfterRollbackTransaction()
		{
			// virtual
		}
		
		#endregion
		
		#region Object Selection

		public virtual List<int> SelectOperatorHandlers(int operatorID)
		{
			return new List<int>();
		}

		public virtual List<int> SelectObjectHandlers(int sourceObjectID)
		{
			return new List<int>();
		}

		public virtual Schema.DependentObjectHeaders SelectObjectDependents(int objectID, bool recursive)
		{
			return new Schema.DependentObjectHeaders();
		}

		public virtual Schema.DependentObjectHeaders SelectObjectDependencies(int objectID, bool recursive)
		{
			throw new NotSupportedException();
		}

		public virtual Schema.CatalogObjectHeaders SelectLibraryCatalogObjects(string libraryName)
		{
			throw new NotSupportedException();
		}
		
		public virtual Schema.CatalogObjectHeaders SelectGeneratedObjects(int objectID)
		{
			throw new NotSupportedException();
		}
		
		#endregion

		#region Updates
				
		protected override void InternalInsertRow(Program program, Schema.TableVar tableVar, IRow row, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "System.TableDum" : break;
				case "System.TableDee" : break;
			}
			base.InternalInsertRow(program, tableVar, row, valueFlags);
		}
		
		protected override void InternalUpdateRow(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "System.TableDee" : break;
				case "System.TableDum" : break;
			}
			base.InternalUpdateRow(program, tableVar, oldRow, newRow, valueFlags);
		}
		
		protected override void InternalDeleteRow(Program program, Schema.TableVar tableVar, IRow row)
		{
			switch (tableVar.Name)
			{
				case "System.TableDee" : break;
				case "System.TableDum" : break;
			}
			base.InternalDeleteRow(program, tableVar, row);
		}
		
		#endregion
		
		#region Resolution
		
		/// <summary>Resolves the given name and returns the catalog object, if an unambiguous match is found. Otherwise, returns null.</summary>
		public virtual Schema.CatalogObject ResolveName(string name, NameResolutionPath path, List<string> names)
		{
			int index = Catalog.ResolveName(name, path, names);
			return index >= 0 ? (Schema.CatalogObject)Catalog[index] : null;
		}

		/// <summary>Ensures that any potential match with the given operator name is in the cache so that operator resolution can occur.</summary>
		public void ResolveOperatorName(string operatorName)
		{
			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				Schema.CatalogObjectHeaders headers = CachedResolveOperatorName(operatorName);
				
				// Only resolve operators in loaded libraries
				for (int index = 0; index < headers.Count; index++)
					if ((headers[index].LibraryName == String.Empty) || Catalog.LoadedLibraries.Contains(headers[index].LibraryName))
						ResolveCatalogObject(headers[index].ID);
			}
		}
		
		/// <summary>Resolves the catalog object with the given id. If the object is not found, an error is raised.</summary>
		public virtual Schema.CatalogObject ResolveCatalogObject(int objectID)
		{
			// TODO: Catalog deserialization concurrency
			// Right now, use the same lock as the user's cache to ensure no deadlocks can occur during deserialization.
			// This effectively places deserialization granularity at the server level, but until we
			// can provide a solution to the deserialization concurrency deadlock problem, this is all we can do.
			lock (Catalog)
			{
				// Lookup the object in the catalog index
				string objectName;
				if (Device.CatalogIndex.TryGetValue(objectID, out objectName))
					return (Schema.CatalogObject)Catalog[objectName];
				else
					return null;
			}
		}
		
		public virtual Schema.ObjectHeader GetObjectHeader(int objectID)
		{
			throw new NotSupportedException();
		}

		public Schema.Object ResolveObject(int objectID)
		{
			Schema.ObjectHeader header = GetObjectHeader(objectID);
			if (header.CatalogObjectID == -1)
				return ResolveCatalogObject(objectID);
				
			Schema.CatalogObject catalogObject = ResolveCatalogObject(header.CatalogObjectID);
			return catalogObject.GetObjectFromHeader(header);
		}
		
		#endregion

		#region Cache
		
		protected virtual Schema.CatalogObjectHeaders CachedResolveOperatorName(string name)
		{
			return Device.OperatorNameCache.Resolve(name);
		}

		/// <summary>Returns the cached object for the given object id, if it exists and is in the cache, null otherwise.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(int objectID)
		{
			return ResolveCachedCatalogObject(objectID, false);
		}
		
		/// <summary>Returns the cached object for the given object id, if it exists and is in the cache. An error is thrown if the object is not in the cache and AMustResolve is true, otherwise null is returned.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(int objectID, bool mustResolve)
		{
			lock (Catalog)
			{
				string objectName;
				if (Device.CatalogIndex.TryGetValue(objectID, out objectName))
					return (Schema.CatalogObject)Catalog[objectName];
					
				if (mustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotCached, objectID);

				return null;
			}
		}
		
		/// <summary>Returns the cached object with the given name, if it exists and is in the cache, null otherwise.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(string name)
		{
			return ResolveCachedCatalogObject(name, false);
		}
		
		/// <summary>Returns the cached object with the given name, if it exists and is in the cache. An error is thrown if the object is not in the cache and AMustResolve is true, otherwise null is returned.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(string name, bool mustResolve)
		{
			lock (Catalog)
			{
				int index = Catalog.IndexOf(name);
				if (index >= 0)
					return (Schema.CatalogObject)Catalog[index];
				
				if (mustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, name);	
				
				return null;
			}
		}
		
		public void ClearCachedCatalogObject(Schema.CatalogObject objectValue)
		{
			Schema.Objects objects = new Schema.Objects();
			objects.Add(objectValue);
			ClearCachedCatalogObjects(objects);
		}
		
		public void ClearCachedCatalogObjects(Schema.Objects objects)
		{
			string[] localObjects = new string[objects.Count];
			for (int index = 0; index < objects.Count; index++)
				localObjects[index] = objects[index].Name;

			// Push a loading context so that the drops only occur in the cache, not the store
			ServerProcess.PushLoadingContext(new LoadingContext(ServerProcess.ServerSession.Server.SystemUser, String.Empty));
			try
			{
				Plan plan = new Plan(ServerProcess);
				try
				{
					plan.PushSecurityContext(new SecurityContext(ServerProcess.ServerSession.Server.SystemUser));
					try
					{
						Block block = (Block)plan.Catalog.EmitDropStatement(this, localObjects, String.Empty, true, true, true, true);
						Program program = new Program(ServerProcess);
						foreach (Statement statement in block.Statements)
						{
							program.Code = Compiler.Compile(plan, statement);
							program.Execute(null);
						}
					}
					finally
					{
						plan.PopSecurityContext();
					}
				}
				finally
				{
					plan.Dispose();
				}
			}
/*
			catch
			{
				// TODO: Determine recovery processing that should take place here.
				// Basically, the cache is in a bad state at this point, and clearing 
				// the catalog cache is the only guaranteed way to get back to a consistent state
			}
*/
			finally
			{
				ServerProcess.PopLoadingContext();
			}
		}
		
		/// <summary>Adds the given object to the catalog cache.</summary>
		public void CacheCatalogObject(Schema.CatalogObject objectValue)
		{
			// An unassigned identifier means no generator numbered the object (see Schema.ObjectIDGenerator, which
			// never allocates zero). The cache index below is keyed by it, so refuse it: a missed allocation site
			// must fail here rather than put an unnumbered object in the catalog cache.
			if (objectValue.ID == Schema.Object.UnassignedID)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectIDRequired, objectValue.Name);
			
			lock (Catalog)
			{
				// if the object is already in the cache (by name), then it must be there as a result of some error
				// and the best course of action in a production scenario is to just replace it with the new object
				// NOTE: accepted tradeoff - Debug and release deliberately diverge here. In release this silently replaces
				// the stale entry, so a catalog cache that has gone wrong degrades instead of failing; in Debug the Add
				// below throws on the duplicate name. Turning it into a throw in release was declined: it is a last-ditch
				// recovery for a cache that is already wrong, so it would turn a degraded client into a dead one with no
				// upside, and a release client that started failing a user's form where it used to limp is a behaviour
				// change applications could notice. Revisit if the catalog cache ever gains a consistency check that fails
				// the same way in both configurations.
				#if !DEBUG
				int index = Catalog.IndexOfName(objectValue.Name);
				if (index >= 0)
					ClearCatalogObject((Schema.CatalogObject)Catalog[index]);
				#endif

				// Add the object to the catalog cache
				Catalog.Add(objectValue);
				
				// Add the object to the cache index
				Device.CatalogIndex.Add(objectValue.ID, Schema.Object.EnsureRooted(objectValue.Name));
			}
		}
		
		/// <summary>Removes the given object from the catalog cache.</summary>
		private void ClearCatalogObject(Schema.CatalogObject objectValue)
		{
			lock (Catalog)
			{
				// Remove the object from the cache index
				// NOTE: removed by identifier, without checking that the entry still names this object. That is sound only
				// while no engine reissues an identifier an object in its catalog already holds - the rule
				// Engine.InitializeCatalog now keeps for a repository engine too. If one ever does, clearing a stale object
				// silently evicts the live object's entry and the live object stops being findable by its identifier
				// (ResolveCatalogObject, ResolveCachedCatalogObject, MarkViewForRecompile). A check is possible should that
				// happen, because nothing renames a catalog object in place today - there is no rename path in this class -
				// so the name in the index always matches the name of the object that put it there.
				Device.CatalogIndex.Remove(objectValue.ID);
				
				// Remove the object from the cache
				Catalog.SafeRemove(objectValue);
				
				// Clear the name resolution cache
				Device.NameCache.Clear(objectValue.Name);
				
				// Clear the operator name resolution cache
				Device.OperatorNameCache.Clear(objectValue.Name);

				RightsChanged(objectValue.GetRights());
			}
		}
		
		protected void ClearCachedRightAssignments(string[] rightNames)
		{
			foreach (Schema.User user in Device.UsersCache.Values)
				user.ClearCachedRightAssignments(rightNames);
		}

		/// <summary>Sets the owner of the given object in the catalog cache.</summary>
		/// <remarks>
		/// The object's rights belong to its owner, so every user's cached answer for them is dropped (see RightsChanged): the
		/// previous owner's cached grants would otherwise outlive the change.
		/// </remarks>
		protected void CacheCatalogObjectOwner(Schema.CatalogObject objectValue, Schema.User owner)
		{
			lock (Catalog)
			{
				objectValue.Owner = owner;
				RightsChanged(objectValue.GetRights());
			}
		}

		#endregion
		
		#region Catalog object
		
		/// <summary>Returns true if the given object is not an A/T object.</summary>
		public bool ShouldSerializeCatalogObject(Schema.CatalogObject objectValue)
		{
			return !objectValue.IsATObject;
		}
		
		/// <summary>Inserts the given object into the catalog cache. If this is not a repository, also inserts the object into the catalog store.</summary>
		public virtual void InsertCatalogObject(Schema.CatalogObject objectValue)
		{
			// Cache the object
			CacheCatalogObject(objectValue);
			
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// Log the DDL instruction
				if (ServerProcess.InTransaction)
					_instructions.Add(new CreateCatalogObjectInstruction(objectValue));
				#endif

				InternalInsertCatalogObject(objectValue);
			}
		}

		protected virtual void InternalInsertCatalogObject(Schema.CatalogObject objectValue)
		{
			// virtual
		}
		
		/// <summary>Updates the given object in the catalog cache. If this is not a repository, also updates the object in the catalog store.</summary>
		/// <remarks>A server link also loses the connection this process holds to it, so that the next use of the link connects with what the catalog now says.</remarks>
		public void UpdateCatalogObject(Schema.CatalogObject objectValue)
		{
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				InternalUpdateCatalogObject(objectValue);

				// A catalog change that affects how a server link connects ends the issuing process's connection to that link, so that
				// its next use of the link connects with what the catalog now says: an alter, and each of the link user operators in
				// SystemNodes.cs, reaches the link through here. After the update, so a failed change closes nothing.
				// NOTE: accepted tradeoff - only this process's connection. A session on another process that has already connected to
				// this link goes on using the settings and credentials it connected with until that process ends, which is what the
				// original author's "the active sessions would just continue to use the current settings" contemplated; revisit if a
				// deployment ever needs such a change to take effect everywhere at once, which would need an operator for closing
				// another session's remote connection (there is none).
				// NOTE: the close makes a network call to the other server, so a caller must not be holding the catalog lock; none of
				// the server-link callers is. That is also why a drop's close sits in DropServerNode rather than beside this one in
				// DeleteCatalogObject: DropServerNode calls that under lock (program.Catalog). Revisit if a caller ever has to
				// update a server link under that lock.
				if (objectValue is Schema.ServerLink serverLink)
					CloseRemoteSession(serverLink);
			}
		}

		protected virtual void InternalUpdateCatalogObject(Schema.CatalogObject objectValue)
		{
			// virtual
		}
		
		/// <summary>Deletes the given object in the catalog cache. If this is not a repository, also deletes the object in the catalog store.</summary>
		public void DeleteCatalogObject(Schema.CatalogObject objectValue)
		{
			lock (Catalog)
			{
				// Remove the object from the catalog cache
				ClearCatalogObject(objectValue);
			}
			
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// Log the DDL instruction
				if (ServerProcess.InTransaction)
					_instructions.Add(new DropCatalogObjectInstruction(objectValue));
				#endif

				InternalDeleteCatalogObject(objectValue);
			}
		}

		protected virtual void InternalDeleteCatalogObject(Schema.CatalogObject objectValue)
		{
			// virtual
		}

		public virtual bool CatalogObjectExists(string objectName)
		{
			return false;
		}
		
		#endregion
		
		#region Loaded library
		
		private void CacheLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			Catalog.LoadedLibraries.Add(loadedLibrary);
		}
		
		private void ClearLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			Catalog.LoadedLibraries.Remove(loadedLibrary);
		}
		
		public void InsertLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			Catalog.UpdateTimeStamp();
			
			CacheLoadedLibrary(loadedLibrary);
			
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new InsertLoadedLibraryInstruction(loadedLibrary));
			#endif

			InternalInsertLoadedLibrary(loadedLibrary);
		}

		protected virtual void InternalInsertLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			// virtual
		}

		public void DeleteLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			Catalog.UpdateTimeStamp();
			
			ClearLoadedLibrary(loadedLibrary);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new DeleteLoadedLibraryInstruction(loadedLibrary));
			#endif
			
			InternalDeleteLoadedLibrary(loadedLibrary);
		}

		protected virtual void InternalDeleteLoadedLibrary(Schema.LoadedLibrary loadedLibrary)
		{
			// virtual
		}
		
		public virtual void ResolveLoadedLibraries()
		{
			// virtual
		}
		
		public bool IsLoadedLibrary(string libraryName)
		{
			return ResolveLoadedLibrary(libraryName, false) != null;
		}
		
		public Schema.LoadedLibrary ResolveLoadedLibrary(string libraryName)
		{
			return ResolveLoadedLibrary(libraryName, true);
		}
		
		public Schema.LoadedLibrary ResolveLoadedLibrary(string libraryName, bool mustResolve)
		{
			Schema.Library library = Catalog.Libraries[libraryName];
			int index = Catalog.LoadedLibraries.IndexOfName(library.Name);
			
			if (index >= 0)
				return Catalog.LoadedLibraries[index];
				
			var result = InternalResolveLoadedLibrary(library);
			if (result != null)
				return result;
			
			if (mustResolve)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryNotRegistered, library.Name);
				
			return null;
		}

		protected virtual LoadedLibrary InternalResolveLoadedLibrary(Schema.Library LLibrary)
		{
			return null;
		}
		
		#endregion

		#region Assembly registration
		
		private SettingsList InternalRegisterAssembly(Schema.LoadedLibrary loadedLibrary, Assembly assembly)
		{
			SettingsList classes = Catalog.ClassLoader.RegisterAssembly(loadedLibrary, assembly);
			loadedLibrary.Assemblies.Add(assembly);
			return classes;
		}
		
		public void RegisterAssembly(Schema.LoadedLibrary loadedLibrary, Assembly assembly)
		{
			SettingsList classes = InternalRegisterAssembly(loadedLibrary, assembly);

			if (!ServerProcess.InLoadingContext())
				InsertRegisteredClasses(loadedLibrary, classes);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new RegisterAssemblyInstruction(loadedLibrary, assembly));
			#endif
		}

		private SettingsList InternalUnregisterAssembly(Schema.LoadedLibrary loadedLibrary, Assembly assembly)
		{
			SettingsList classes = Catalog.ClassLoader.UnregisterAssembly(loadedLibrary, assembly);
			loadedLibrary.Assemblies.Remove(assembly);
			return classes;
		}

		public void UnregisterAssembly(Schema.LoadedLibrary loadedLibrary, Assembly assembly)
		{
			SettingsList classes = InternalUnregisterAssembly(loadedLibrary, assembly);
			
			if (!ServerProcess.InLoadingContext())
				DeleteRegisteredClasses(loadedLibrary, classes);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new UnregisterAssemblyInstruction(loadedLibrary, assembly));
			#endif
		}
		
		protected virtual void InsertRegisteredClasses(Schema.LoadedLibrary loadedLibrary, SettingsList registeredClasses)
		{
		}
		
		protected virtual void DeleteRegisteredClasses(Schema.LoadedLibrary loadedLibrary, SettingsList registeredClasses)
		{
		}

		#endregion

		#region Session objects
				
		private void CreateSessionObject(Schema.CatalogObject sessionObject)
		{
			lock (ServerProcess.ServerSession.SessionObjects)
			{
				ServerProcess.ServerSession.SessionObjects.Add(new Schema.SessionObject(sessionObject.SessionObjectName, sessionObject.Name));
			}
		}
		
		private void DropSessionObject(Schema.CatalogObject sessionObject)
		{
			ServerProcess.ServerSession.Server.DropSessionObject(sessionObject);
		}
		
		private void CreateSessionOperator(Schema.Operator sessionOperator)
		{
			lock (ServerProcess.ServerSession.SessionOperators)
			{
				if (!ServerProcess.ServerSession.SessionOperators.ContainsName(sessionOperator.SessionObjectName))
					ServerProcess.ServerSession.SessionOperators.Add(new Schema.SessionObject(sessionOperator.SessionObjectName, sessionOperator.OperatorName));
			}
		}
		
		private void DropSessionOperator(Schema.Operator sessionOperator)
		{
			ServerProcess.ServerSession.Server.DropSessionOperator(sessionOperator);
		}
		
		#endregion
		
		#region Table
		
		public void CreateTable(Schema.BaseTableVar table)
		{
			InsertCatalogObject(table);
			
			if (table.SessionObjectName != null)
			{
				CreateSessionObject(table);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateSessionObjectInstruction(table));
				#endif
			}
			
			if (!ServerProcess.ServerSession.Server.IsEngine && ServerProcess.IsReconciliationEnabled())
			{
				CreateDeviceTable(table);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateDeviceTableInstruction(table));
				#endif
			}
		}
		
		public void DropTable(Schema.BaseTableVar table)
		{
			DeleteCatalogObject(table);
			
			if (table.SessionObjectName != null)
			{
				DropSessionObject(table);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DropSessionObjectInstruction(table));
				#endif
			}
			
			if (!ServerProcess.ServerSession.Server.IsEngine && ServerProcess.IsReconciliationEnabled())
			{
				DropDeviceTable(table);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DropDeviceTableInstruction(table));
				#endif
			}
		}
		
		#endregion
		
		#region View
		
		public void CreateView(Schema.DerivedTableVar view)
		{
			InsertCatalogObject(view);
			
			if (view.SessionObjectName != null)
			{
				CreateSessionObject(view);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateSessionObjectInstruction(view));
				#endif
			}
		}
		
		public void DropView(Schema.DerivedTableVar view)
		{
			DeleteCatalogObject(view);
			
			if (view.SessionObjectName != null)
			{
				DropSessionObject(view);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DropSessionObjectInstruction(view));
				#endif
			}
		}
		
		public void MarkViewForRecompile(int objectID)
		{
			string objectName;
			if (Device.CatalogIndex.TryGetValue(objectID, out objectName))
				((Schema.DerivedTableVar)Catalog[objectName]).ShouldReinferReferences = true;
		}

		#endregion
		
		#region Conversions
		
		private void AddImplicitConversion(Schema.Conversion conversion)
		{
			lock (Catalog)
			{
				conversion.SourceScalarType.ImplicitConversions.Add(conversion);

				// Here, at the change and under the catalog lock the compiler traces paths under, so that no session keeps an answer from
				// before it. Whole, because a new conversion can shorten a cached path between types it does not touch.
				// NOTE: this also runs for every conversion the server lazily loads, where it is overhead only (a type's conversions load
				// with it, so a load cannot change a path between types already loaded); if catalog warm-up shows churn here, skip both
				// clears when ServerProcess.InLoadingContext().
				Catalog.ConversionPathCache.Clear();
				Catalog.OperatorResolutionCache.Clear();
			}
		}
		
		private void RemoveImplicitConversion(Schema.Conversion conversion)
		{
			lock (Catalog)
			{
				conversion.SourceScalarType.ImplicitConversions.SafeRemove(conversion);

				// As in AddImplicitConversion; every answer a removal changes uses the conversion.
				Catalog.ConversionPathCache.Clear(conversion);
				Catalog.OperatorResolutionCache.Clear(conversion);
			}
		}
		
		public void CreateConversion(Schema.Conversion conversion)
		{
			InsertCatalogObject(conversion);
			
			AddImplicitConversion(conversion);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AddImplicitConversionInstruction(conversion));
			#endif
		}
		
		public void DropConversion(Schema.Conversion conversion)
		{
			DeleteCatalogObject(conversion);
			
			RemoveImplicitConversion(conversion);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new RemoveImplicitConversionInstruction(conversion));
			#endif
		}
		
		#endregion
		
		#region Sort
		
		private void SetScalarTypeSort(Schema.ScalarType scalarType, Schema.Sort sort, bool isUnique, bool isEquality)
		{
			if (isEquality)
				scalarType.EqualitySort = sort;
			else if (isUnique)
				scalarType.UniqueSort = sort;
			else
				scalarType.Sort = sort;
		}
		
		public void CreateSort(Schema.Sort sort)
		{
			InsertCatalogObject(sort);
		}
		
		public void DropSort(Schema.Sort sort)
		{
			DeleteCatalogObject(sort);
		}

		public void AttachSort(Schema.ScalarType scalarType, Schema.Sort sort, bool isUnique, bool isEquality)
		{
			#if LOGDDLINSTRUCTIONS
			Schema.Sort originalSort = isEquality ? scalarType.EqualitySort : (isUnique ? scalarType.UniqueSort : scalarType.Sort);
			#endif
			SetScalarTypeSort(scalarType, sort, isUnique, isEquality);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetScalarTypeSortInstruction(scalarType, originalSort, isUnique, isEquality));
			#endif
		}
		
		public void DetachSort(Schema.ScalarType scalarType, Schema.Sort sort, bool isUnique, bool isEquality)
		{
			SetScalarTypeSort(scalarType, null, isUnique, isEquality);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetScalarTypeSortInstruction(scalarType, sort, isUnique, isEquality));
			#endif
		}
		
		#endregion
		
		#region Metadata
		
		public void AlterMetaData(Schema.Object objectValue, AlterMetaData alterMetaData)
		{
			if (alterMetaData != null)
			{
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				{
					MetaData metaData = null;
					if (objectValue.MetaData != null)
					{
						metaData = new MetaData();
						metaData.Merge(objectValue.MetaData);
					}

					_instructions.Add(new AlterMetaDataInstruction(objectValue, metaData));
				}
				#endif
				
				AlterNode.AlterMetaData(objectValue, alterMetaData);
			}
		}
		
		#endregion
		
		#region Scalar type
		
		public void CreateScalarType(Schema.ScalarType scalarType)
		{
			InsertCatalogObject(scalarType);
		}
		
		public void DropScalarType(Schema.ScalarType scalarType)
		{
			DeleteCatalogObject(scalarType);
		}
		
		#endregion
		
		#region Operator
		
		public void CreateOperator(Schema.Operator operatorValue)
		{	
			// Before InsertCatalogObject, so that the undo log replays in the same order as DropOperator does its work: a rollback
			// walks the log backwards, so recording the session operator first clears the catalog object first and removes the
			// session entry second. Engine.DropSessionOperator only removes the entry once the operator's global name is gone from
			// the catalog (its guard keeps the entry alive while another overload still exists), so the other order leaves the
			// session holding an alias for an operator that no longer exists and Engine.Disconnect then fails to find it.
			if (operatorValue.SessionObjectName != null)
			{
				CreateSessionOperator(operatorValue);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateSessionOperatorInstruction(operatorValue));
				#endif
			}
			
			InsertCatalogObject(operatorValue);
			
			// The compiler cleared this when it compiled the statement, before this change: another session may have cached a
			// resolution since. Outside the catalog lock is enough, because the compiler looks up, resolves and caches under it.
			Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);
		}
		
		public void AlterOperator(Schema.Operator oldOperator, Schema.Operator newOperator)
		{
			#if LOGDDLINSTRUCTIONS
			ObjectList originalDependencies = new ObjectList();
			oldOperator.Dependencies.CopyTo(originalDependencies);
			#endif
			oldOperator.Dependencies.Clear();
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new RemoveDependenciesInstruction(oldOperator, originalDependencies));
			#endif
				
			oldOperator.AddDependencies(newOperator.Dependencies);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AddDependenciesInstruction(oldOperator));
			#endif
			oldOperator.DetermineRemotable(this);
			
			AlterOperatorBlockNode(oldOperator.Block, newOperator.Block.BlockNode);
		}
		
		public void AlterOperatorBlockNode(Schema.OperatorBlock operatorBlock, PlanNode newNode)
		{
			#if LOGDDLINSTRUCTIONS
			PlanNode originalNode = operatorBlock.BlockNode;
			#endif
			operatorBlock.BlockNode = newNode;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetOperatorBlockNodeInstruction(operatorBlock, originalNode));
			#endif
		}

		public void DropOperator(Schema.Operator operatorValue)
		{
			DeleteCatalogObject(operatorValue);
			
			// As in CreateOperator.
			Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);
			
			if (operatorValue.SessionObjectName != null)
			{
				DropSessionOperator(operatorValue);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DropSessionOperatorInstruction(operatorValue));
				#endif
			}
		}
		
		#endregion
		
		#region Constraint
		
		public void CreateConstraint(Schema.CatalogConstraint constraint)
		{
			InsertCatalogObject(constraint);
			
			if (!ServerProcess.ServerSession.Server.IsEngine && constraint.Enforced)
			{
				CreateConstraintNode.AttachConstraint(constraint, constraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new AttachCatalogConstraintInstruction(constraint));
				#endif
			}
				
			if (constraint.SessionObjectName != null)
			{
				CreateSessionObject(constraint);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateSessionObjectInstruction(constraint));
				#endif
			}
		}

		public void AlterConstraint(Schema.CatalogConstraint oldConstraint, Schema.CatalogConstraint newConstraint)
		{
			if (!ServerProcess.ServerSession.Server.IsEngine && oldConstraint.Enforced)
			{
				CreateConstraintNode.DetachConstraint(oldConstraint, oldConstraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DetachCatalogConstraintInstruction(oldConstraint));
				#endif
			}
			
			#if LOGDDLINSTRUCTIONS
			ObjectList originalDependencies = new ObjectList();
			oldConstraint.Dependencies.CopyTo(originalDependencies);
			#endif
			oldConstraint.Dependencies.Clear();
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new RemoveDependenciesInstruction(oldConstraint, originalDependencies));
			#endif
				
			oldConstraint.AddDependencies(newConstraint.Dependencies);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AddDependenciesInstruction(oldConstraint));
			#endif

			#if LOGDDLINSTRUCTIONS
			PlanNode originalNode = oldConstraint.Node;
			#endif
			oldConstraint.Node = newConstraint.Node;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetCatalogConstraintNodeInstruction(oldConstraint, originalNode));
			#endif

			if (!ServerProcess.ServerSession.Server.IsEngine && oldConstraint.Enforced)
			{
				CreateConstraintNode.AttachConstraint(oldConstraint, oldConstraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new AttachCatalogConstraintInstruction(oldConstraint));
				#endif
			}
		}
		
		public void DropConstraint(Schema.CatalogConstraint constraint)
		{
			DeleteCatalogObject(constraint);

			if (!ServerProcess.ServerSession.Server.IsEngine && constraint.Enforced)
			{
				CreateConstraintNode.DetachConstraint(constraint, constraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DetachCatalogConstraintInstruction(constraint));
				#endif
			}

			if (constraint.SessionObjectName != null)
			{
				DropSessionObject(constraint);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DropSessionObjectInstruction(constraint));
				#endif
			}
		}
		
		#endregion
		
		#region Reference
		
		private void AttachReference(Schema.Reference reference)
		{
			if (!ServerProcess.ServerSession.Server.IsEngine && reference.Enforced)
			{
				if ((reference.SourceTable is Schema.BaseTableVar) && (reference.TargetTable is Schema.BaseTableVar))
				{
					reference.SourceTable.Constraints.Add(reference.SourceConstraint);
					reference.SourceTable.InsertConstraints.Add(reference.SourceConstraint);
					reference.SourceTable.UpdateConstraints.Add(reference.SourceConstraint);
					if ((reference.UpdateReferenceAction == ReferenceAction.Require) || (reference.DeleteReferenceAction == ReferenceAction.Require))
					{
						reference.TargetTable.Constraints.Add(reference.TargetConstraint);
						if (reference.UpdateReferenceAction == ReferenceAction.Require)
							reference.TargetTable.UpdateConstraints.Add(reference.TargetConstraint);
						if (reference.DeleteReferenceAction == ReferenceAction.Require)
							reference.TargetTable.DeleteConstraints.Add(reference.TargetConstraint);
					}
				}
				else
				{
					// This constraint is added only in the cache (never persisted)
					CreateConstraintNode.AttachConstraint(reference.CatalogConstraint, reference.CatalogConstraint.Node);
				}
				
				if ((reference.UpdateReferenceAction == ReferenceAction.Cascade) || (reference.UpdateReferenceAction == ReferenceAction.Clear) || (reference.UpdateReferenceAction == ReferenceAction.Set))
				{
					// This object is added only in the cache (never persisted)
					reference.TargetTable.EventHandlers.Add(reference.UpdateHandler);
				}
					
				if ((reference.DeleteReferenceAction == ReferenceAction.Cascade) || (reference.DeleteReferenceAction == ReferenceAction.Clear) || (reference.DeleteReferenceAction == ReferenceAction.Set))
				{
					// This object is added only in the cache (never persisted)
					reference.TargetTable.EventHandlers.Add(reference.DeleteHandler);
				}
			}
					
			//reference.SourceTable.SourceReferences.AddInCreationOrder(reference);
			//reference.TargetTable.TargetReferences.AddInCreationOrder(reference);
			reference.SourceTable.References.AddInCreationOrder(reference);
			if (!reference.SourceTable.Equals(reference.TargetTable))
				reference.TargetTable.References.AddInCreationOrder(reference);
			
			reference.SourceTable.SetShouldReinferReferences(this);
			reference.TargetTable.SetShouldReinferReferences(this);
		}
		
		private void DetachReference(Schema.Reference reference)
		{
			if ((reference.SourceTable is Schema.BaseTableVar) && (reference.TargetTable is Schema.BaseTableVar))
			{
				if (reference.SourceConstraint != null)
				{
					reference.SourceTable.InsertConstraints.SafeRemove(reference.SourceConstraint);
					reference.SourceTable.UpdateConstraints.SafeRemove(reference.SourceConstraint);
					reference.SourceTable.Constraints.SafeRemove(reference.SourceConstraint);
				}
				
				if (reference.TargetConstraint != null)
				{
					if ((reference.UpdateReferenceAction == ReferenceAction.Require) || (reference.DeleteReferenceAction == ReferenceAction.Require))
					{
						if (reference.UpdateReferenceAction == ReferenceAction.Require)
							reference.TargetTable.UpdateConstraints.SafeRemove(reference.TargetConstraint);
						if (reference.DeleteReferenceAction == ReferenceAction.Require)
							reference.TargetTable.DeleteConstraints.SafeRemove(reference.TargetConstraint);				
						reference.TargetTable.Constraints.SafeRemove(reference.TargetConstraint);
					}
				}
			}
			else
			{
				if (reference.CatalogConstraint != null)
				{
					CreateConstraintNode.DetachConstraint(reference.CatalogConstraint, reference.CatalogConstraint.Node);
					ServerProcess.ServerSession.Server.RemoveCatalogConstraintCheck(reference.CatalogConstraint);
				}
			}
			
			if (((reference.UpdateReferenceAction == ReferenceAction.Cascade) || (reference.UpdateReferenceAction == ReferenceAction.Clear) || (reference.UpdateReferenceAction == ReferenceAction.Set)) && (reference.UpdateHandler != null))
			{
				reference.TargetTable.EventHandlers.SafeRemove((TableVarEventHandler)reference.UpdateHandler);
			}
				
			if (((reference.DeleteReferenceAction == ReferenceAction.Cascade) || (reference.DeleteReferenceAction == ReferenceAction.Clear) || (reference.DeleteReferenceAction == ReferenceAction.Set)) && (reference.DeleteHandler != null))
			{
				reference.TargetTable.EventHandlers.SafeRemove((TableVarEventHandler)reference.DeleteHandler);
			}
				
			//reference.SourceTable.SourceReferences.SafeRemove(reference);
			//reference.TargetTable.TargetReferences.SafeRemove(reference);
			reference.SourceTable.References.SafeRemove(reference);
			reference.TargetTable.References.SafeRemove(reference);
			
			reference.SourceTable.SetShouldReinferReferences(this);	
			reference.TargetTable.SetShouldReinferReferences(this);	
		}
		
		public void CreateReference(Schema.Reference reference)
		{
			InsertCatalogObject(reference);

			AttachReference(reference);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachReferenceInstruction(reference));
			#endif

			if (reference.SessionObjectName != null)
			{
				CreateSessionObject(reference);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateSessionObjectInstruction(reference));
				#endif
			}
		}
		
		public void DropReference(Schema.Reference reference)
		{
			DeleteCatalogObject(reference);
			
			DetachReference(reference);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachReferenceInstruction(reference));
			#endif
			
			if (reference.SessionObjectName != null)
			{
				DropSessionObject(reference);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new DropSessionObjectInstruction(reference));
				#endif
			}
		}
		
		#endregion
		
		#region Event handler
		
		private void AttachEventHandler(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
		{
			Schema.TableVar tableVar = eventSource as Schema.TableVar;
			if (tableVar != null)
			{
				if (eventSourceColumnIndex >= 0)
					tableVar.Columns[eventSourceColumnIndex].EventHandlers.Add((TableVarColumnEventHandler)eventHandler, beforeOperatorNames);
				else
					tableVar.EventHandlers.Add((TableVarEventHandler)eventHandler, beforeOperatorNames);
				tableVar.DetermineRemotable(this);
			}
			else
				((Schema.ScalarType)eventSource).EventHandlers.Add((ScalarTypeEventHandler)eventHandler);
		}

		private void MoveEventHandler(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
		{
			Schema.TableVar tableVar = eventSource as Schema.TableVar;
			if (tableVar != null)
			{
				if (eventSourceColumnIndex >= 0)
					tableVar.Columns[eventSourceColumnIndex].EventHandlers.MoveBefore((TableVarColumnEventHandler)eventHandler, beforeOperatorNames);
				else
					tableVar.EventHandlers.MoveBefore((TableVarEventHandler)eventHandler, beforeOperatorNames);
				tableVar.DetermineRemotable(this);
			}
			else
				((Schema.ScalarType)eventSource).EventHandlers.MoveBefore((ScalarTypeEventHandler)eventHandler, beforeOperatorNames);
		}

		private void DetachEventHandler(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex)
		{
			Schema.TableVar tableVar = eventSource as Schema.TableVar;
			if (tableVar != null)
			{
				if (eventSourceColumnIndex >= 0)
					tableVar.Columns[eventSourceColumnIndex].EventHandlers.SafeRemove((TableVarColumnEventHandler)eventHandler);
				else
					tableVar.EventHandlers.SafeRemove((TableVarEventHandler)eventHandler);
				tableVar.DetermineRemotable(this);
			}
			else
				((Schema.ScalarType)eventSource).EventHandlers.SafeRemove((ScalarTypeEventHandler)eventHandler);
		}

		private List<string> GetEventHandlerBeforeOperatorNames(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex)
		{
			List<string> result = new List<string>();
			EventHandlers handlers = null;
			Schema.TableVar tableVar = eventSource as Schema.TableVar;
			if (tableVar != null)
			{
				if (eventSourceColumnIndex >= 0)
					handlers = tableVar.Columns[eventSourceColumnIndex].EventHandlers;
				else
					handlers = tableVar.EventHandlers;
			}
			else
				handlers = ((Schema.ScalarType)eventSource).EventHandlers;
				
			if (handlers != null)
			{
				int handlerIndex = handlers.IndexOfName(eventHandler.Name);
				for (int index = handlerIndex; index >= 0; index--)
					result.Add(handlers[index].Name);
			}
			
			return result;
		}

		public void CreateEventHandler(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
		{
			AttachEventHandler(eventHandler, eventSource, eventSourceColumnIndex, beforeOperatorNames);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachEventHandlerInstruction(eventHandler, eventSource, eventSourceColumnIndex, beforeOperatorNames));
			#endif

			// Note the event handlers must be attached first, otherwise properties on the event handler will not be set properly (CatalogObjectID, ParentObjectID, etc.,.)
			InsertCatalogObject(eventHandler);
		}

		public void AlterEventHandler(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex, List<string> beforeOperatorNames)
		{
			#if LOGDDLINSTRUCTIONS
			List<string> localBeforeOperatorNames = GetEventHandlerBeforeOperatorNames(eventHandler, eventSource, eventSourceColumnIndex);
			#endif
			MoveEventHandler(eventHandler, eventSource, eventSourceColumnIndex, beforeOperatorNames);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new MoveEventHandlerInstruction(eventHandler, eventSource, eventSourceColumnIndex, localBeforeOperatorNames));
			#endif
		}
		
		public void DropEventHandler(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex)
		{
			DeleteCatalogObject(eventHandler);
			
			#if LOGDDLINSTRUCTIONS
			List<string> beforeOperatorNames = GetEventHandlerBeforeOperatorNames(eventHandler, eventSource, eventSourceColumnIndex);
			#endif
			DetachEventHandler(eventHandler, eventSource, eventSourceColumnIndex);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachEventHandlerInstruction(eventHandler, eventSource, eventSourceColumnIndex, beforeOperatorNames));
			#endif
		}
		
		#endregion
		
		#region Class definitions
		
		public void AlterClassDefinition(ClassDefinition classDefinition, AlterClassDefinition alterClassDefinition)
		{
			AlterClassDefinition(classDefinition, alterClassDefinition, null);
		}

		public void AlterClassDefinition(ClassDefinition classDefinition, AlterClassDefinition alterClassDefinition, object instance)
		{
			if (alterClassDefinition != null)
			{
				#if LOGDDLINSTRUCTIONS
				// Recorded before the change, not after: the alteration applies the attributes one at a time and throws on a value it
				// cannot convert to the property's type, and a change that fails part-way has to be undoable too.
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new AlterClassDefinitionInstruction(classDefinition, alterClassDefinition, instance));
				#endif
				AlterNode.AlterClassDefinition(classDefinition, alterClassDefinition, instance);
			}
		}
		
		#endregion
		
		#region Key
		
		private void AttachKey(TableVar tableVar, Key key)
		{
			tableVar.Keys.Add(key);
			if (!tableVar.Constraints.Contains(key.Constraint))
				tableVar.Constraints.Add(key.Constraint);
			if (!tableVar.InsertConstraints.Contains(key.Constraint))
				tableVar.InsertConstraints.Add(key.Constraint);
			if (!tableVar.UpdateConstraints.Contains(key.Constraint))
				tableVar.UpdateConstraints.Add(key.Constraint);
		}

		private void DetachKey(TableVar tableVar, Key key)
		{
			tableVar.Keys.SafeRemove(key);
			tableVar.Constraints.SafeRemove(key.Constraint);
			tableVar.InsertConstraints.SafeRemove(key.Constraint);
			tableVar.UpdateConstraints.SafeRemove(key.Constraint);
		}

		public void CreateKey(TableVar tableVar, Key key)
		{
			AttachKey(tableVar, key);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachKeyInstruction(tableVar, key));
			#endif
		}

		public void DropKey(TableVar tableVar, Key key)
		{
			DetachKey(tableVar, key);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachKeyInstruction(tableVar, key));
			#endif
		}

		#endregion
		
		#region Order
		
		private void AttachOrder(TableVar tableVar, Order order)
		{
			tableVar.Orders.Add(order);
		}

		private void DetachOrder(TableVar tableVar, Order order)
		{
			tableVar.Orders.SafeRemove(order);
		}

		public void CreateOrder(TableVar tableVar, Order order)
		{
			AttachOrder(tableVar, order);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachOrderInstruction(tableVar, order));
			#endif
		}

		public void DropOrder(TableVar tableVar, Order order)
		{
			DetachOrder(tableVar, order);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachOrderInstruction(tableVar, order));
			#endif
		}
		
		#endregion
		
		#region TableVar column
		
		private void AttachTableVarColumn(Schema.BaseTableVar table, Schema.TableVarColumn column)
		{
			table.DataType.Columns.Add(column.Column);
			table.Columns.Add(column);
			table.DataType.ResetRowType();
		}

		private void DetachTableVarColumn(Schema.BaseTableVar table, Schema.TableVarColumn column)
		{
			table.DataType.Columns.SafeRemove(column.Column);
			table.Columns.SafeRemove(column);
			table.DataType.ResetRowType();
		}

		public void CreateTableVarColumn(Schema.BaseTableVar table, Schema.TableVarColumn column)
		{
			AttachTableVarColumn(table, column);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachTableVarColumnInstruction(table, column));
			#endif
		}

		public void DropTableVarColumn(Schema.BaseTableVar table, Schema.TableVarColumn column)
		{
			DetachTableVarColumn(table, column);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachTableVarColumnInstruction(table, column));
			#endif
		}

		public void SetTableVarColumnDefault(Schema.TableVarColumn LColumn, Schema.TableVarColumnDefault defaultValue)
		{
			TableVarColumnDefault originalDefault = LColumn.Default;
			LColumn.Default = defaultValue;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetTableVarColumnDefaultInstruction(LColumn, originalDefault));
			#endif
		}

		public void SetTableVarColumnIsNilable(TableVarColumn LColumn, bool isNilable)
		{
			bool originalIsNilable = LColumn.IsNilable;
			LColumn.IsNilable = isNilable;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetTableVarColumnIsNilableInstruction(LColumn, originalIsNilable));
			#endif
		}
		
		#endregion

		#region TableVar constraint
		
		private void AttachTableVarConstraint(Schema.TableVar tableVar, Schema.TableVarConstraint constraint)
		{
			tableVar.Constraints.Add(constraint);
			if (constraint is Schema.RowConstraint)
				tableVar.RowConstraints.Add(constraint);
			else
			{
				Schema.TransitionConstraint transitionConstraint = (Schema.TransitionConstraint)constraint;
				if (transitionConstraint.OnInsertNode != null)
					tableVar.InsertConstraints.Add(transitionConstraint);
				if (transitionConstraint.OnUpdateNode != null)
					tableVar.UpdateConstraints.Add(transitionConstraint);
				if (transitionConstraint.OnDeleteNode != null)
					tableVar.DeleteConstraints.Add(transitionConstraint);
			}
		}

		private void DetachTableVarConstraint(Schema.TableVar tableVar, Schema.TableVarConstraint constraint)
		{
			tableVar.Constraints.SafeRemove(constraint);
			if (constraint is Schema.RowConstraint)
				tableVar.RowConstraints.SafeRemove((Schema.RowConstraint)constraint);
			else
			{
				Schema.TransitionConstraint transitionConstraint = (Schema.TransitionConstraint)constraint;
				if (transitionConstraint.OnInsertNode != null)
					tableVar.InsertConstraints.SafeRemove(transitionConstraint);
				if (transitionConstraint.OnUpdateNode != null)
					tableVar.UpdateConstraints.SafeRemove(transitionConstraint);
				if (transitionConstraint.OnDeleteNode != null)
					tableVar.DeleteConstraints.SafeRemove(transitionConstraint);
			}
		}
		
		public void CreateTableVarConstraint(Schema.TableVar tableVar, Schema.TableVarConstraint constraint)
		{
			AttachTableVarConstraint(tableVar, constraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachTableVarConstraintInstruction(tableVar, constraint));
			#endif
		}
		
		public void DropTableVarConstraint(Schema.TableVar tableVar, Schema.TableVarConstraint constraint)
		{
			DetachTableVarConstraint(tableVar, constraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachTableVarConstraintInstruction(tableVar, constraint));
			#endif
		}
		
		#endregion
		
		#region TableVar column constraint
		
		private void AttachTableVarColumnConstraint(Schema.TableVarColumn tableVarColumn, Schema.TableVarColumnConstraint constraint)
		{
			tableVarColumn.Constraints.Add(constraint);
		}

		private void DetachTableVarColumnConstraint(Schema.TableVarColumn tableVarColumn, Schema.TableVarColumnConstraint constraint)
		{
			tableVarColumn.Constraints.SafeRemove(constraint);
		}
		
		public void CreateTableVarColumnConstraint(TableVarColumn column, TableVarColumnConstraint constraint)
		{
			AttachTableVarColumnConstraint(column, constraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachTableVarColumnConstraintInstruction(column, constraint));
			#endif
		}

		public void DropTableVarColumnConstraint(TableVarColumn column, TableVarColumnConstraint constraint)
		{
			DetachTableVarColumnConstraint(column, constraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachTableVarColumnConstraintInstruction(column, constraint));
			#endif
		}

		#endregion
		
		#region Scalar type
		
		private void AttachScalarTypeConstraint(Schema.ScalarType scalarType, Schema.ScalarTypeConstraint constraint)
		{
			scalarType.Constraints.Add(constraint);
		}

		private void DetachScalarTypeConstraint(Schema.ScalarType scalarType, Schema.ScalarTypeConstraint constraint)
		{
			scalarType.Constraints.SafeRemove(constraint);
		}
		
		public void CreateScalarTypeConstraint(ScalarType scalarType, ScalarTypeConstraint constraint)
		{
			AttachScalarTypeConstraint(scalarType, constraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachScalarTypeConstraintInstruction(scalarType, constraint));
			#endif
		}

		public void DropScalarTypeConstraint(ScalarType scalarType, ScalarTypeConstraint constraint)
		{
			DetachScalarTypeConstraint(scalarType, constraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachScalarTypeConstraintInstruction(scalarType, constraint));
			#endif
		}

		public void SetScalarTypeDefault(Schema.ScalarType scalarType, Schema.ScalarTypeDefault defaultValue)
		{
			Schema.ScalarTypeDefault originalDefault = scalarType.Default;
			scalarType.Default = defaultValue;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetScalarTypeDefaultInstruction(scalarType, originalDefault));
			#endif
		}

		public void SetScalarTypeIsSpecialOperator(Schema.ScalarType scalarType, Schema.Operator operatorValue)
		{
			Schema.Operator originalOperator = scalarType.IsSpecialOperator;
			scalarType.IsSpecialOperator = operatorValue;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetScalarTypeIsSpecialOperatorInstruction(scalarType, originalOperator));
			#endif
		}

		private void AttachSpecial(Schema.ScalarType scalarType, Schema.Special special)
		{
			scalarType.Specials.Add(special);
		}

		private void DetachSpecial(Schema.ScalarType scalarType, Schema.Special special)
		{
			scalarType.Specials.SafeRemove(special);
		}
		
		public void CreateSpecial(ScalarType scalarType, Special special)
		{
			AttachSpecial(scalarType, special);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachSpecialInstruction(scalarType, special));
			#endif
		}

		public void DropSpecial(ScalarType scalarType, Special special)
		{
			DetachSpecial(scalarType, special);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachSpecialInstruction(scalarType, special));
			#endif
		}

		private void AttachRepresentation(Schema.ScalarType scalarType, Schema.Representation representation)
		{
			if (!scalarType.Representations.Contains(representation))
				scalarType.Representations.Add(representation);
		}

		private void DetachRepresentation(Schema.ScalarType scalarType, Schema.Representation representation)
		{
			scalarType.Representations.SafeRemove(representation);
		}
		
		public void CreateRepresentation(ScalarType scalarType, Representation representation)
		{
			AttachRepresentation(scalarType, representation);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachRepresentationInstruction(scalarType, representation));
			#endif	
		}

		public void DropRepresentation(ScalarType scalarType, Representation representation)
		{
			DetachRepresentation(scalarType, representation);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachRepresentationInstruction(scalarType, representation));
			#endif
		}

		private void AttachProperty(Schema.Representation representation, Schema.Property property)
		{
			if (!representation.Properties.Contains(property))
				representation.Properties.Add(property);
		}

		private void DetachProperty(Schema.Representation representation, Schema.Property property)
		{
			representation.Properties.SafeRemove(property);
		}
		
		public void CreateProperty(Schema.Representation representation, Schema.Property property)
		{
			AttachProperty(representation, property);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachPropertyInstruction(representation, property));
			#endif
		}

		public void DropProperty(Schema.Representation representation, Schema.Property property)
		{
			DetachProperty(representation, property);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachPropertyInstruction(representation, property));
			#endif
		}

		#endregion
		
		#region Device
		
		public new CatalogDevice Device { get { return (CatalogDevice)base.Device; } }

		public void CreateDevice(Schema.Device device)
		{
			InsertCatalogObject(device);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
			{
				_instructions.Add(new StartDeviceInstruction(device));
			}
			#endif
		}
		
		public void StartDevice(Schema.Device device)
		{
			device.Start(ServerProcess);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new StartDeviceInstruction(device));
			#endif
		}
		
		public void RegisterDevice(Schema.Device device)
		{
			if (!device.Registered)
			{
				device.Register(ServerProcess);
				UpdateCatalogObject(device);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new RegisterDeviceInstruction(device));
				#endif
			}
		}
		
		public void UnregisterDevice(Schema.Device device)
		{
			device.ClearRegistered();
		}
		
		public void StopDevice(Schema.Device device)
		{
			StopDevice(device, false);
		}
		
		/// <summary>Teardown a drop made inside an open transaction leaves for the outermost commit.</summary>
		/// <remarks>
		/// <para>
		/// Two kinds of teardown are deferred this way - stopping a dropped device, and closing this process's session
		/// to a dropped or altered server link - for the same reason: the thing being torn down is still a participant
		/// in the transaction, so tearing it down at once would discard work the transaction has not finished with.
		/// Both are discarded for a level that rolls back, because the rollback undoes the drop that asked for them.
		/// The level bookkeeping lives here once rather than once per kind: it has already needed a fix of its own
		/// (bug-catalog-deferred-device-stops-ignore-nesting), and a second copy would be the one that did not get it.
		/// </para>
		/// <para>
		/// Inside InternalCommitTransaction and InternalRollbackTransaction, Transactions.Count is the level being
		/// ended: the base class ends it after calling them. That is the level these methods are given.
		/// </para>
		/// </remarks>
		private class DeferredTeardowns<T>
		{
			private class Entry
			{
				public Entry(T item, int level)
				{
					Item = item;
					Level = level;
				}
				
				public readonly T Item;
				
				/// <summary>The transaction level the teardown belongs to: the level of the drop, then each enclosing level a nested commit hands it to.</summary>
				public int Level;
			}
			
			private List<Entry> _entries;
			
			public void Add(T item, int level)
			{
				if (_entries == null)
					_entries = new List<Entry>();
				_entries.Add(new Entry(item, level));
			}
			
			/// <summary>Runs the pending teardowns when the outermost level commits; a nested commit hands its teardowns to the enclosing level.</summary>
			public void Commit(int level, Action<T> tearDown)
			{
				if (_entries == null)
					return;
				
				if (level > 1)
				{
					foreach (Entry entry in _entries)
						if (entry.Level >= level)
							entry.Level = level - 1;
				}
				else
				{
					while (_entries.Count > 0)
					{
						tearDown(_entries[0].Item);
						_entries.RemoveAt(0);
					}
					
					_entries = null;
				}
			}
			
			/// <summary>Discards the teardowns that belong to the level being rolled back; the undo of their drops restores what was never torn down.</summary>
			public void Rollback(int level)
			{
				if (_entries == null)
					return;
				
				_entries.RemoveAll(entry => entry.Level >= level);
				if (_entries.Count == 0)
					_entries = null;
			}
		}
		
		private readonly DeferredTeardowns<Schema.Device> _deferredDeviceStops = new DeferredTeardowns<Schema.Device>();
		
		private void InternalStopDevice(Schema.Device device)
		{
			if (device.Running)
			{
				if (device.Sessions.Count > 0)
					for (int index = device.Sessions.Count - 1; index >= 0; index--)
						device.Sessions.Dispose();
				// TODO: implement checking and error handling for in use device sessions on this device
				//throw new RuntimeException(RuntimeException.Codes.DeviceInUse, ADevice.Name);

				device.Stop(ServerProcess);					
			}
		}
		
		private void StopDevice(Schema.Device device, bool isUndo)
		{
			if ((ServerProcess.InTransaction) && !isUndo)
				_deferredDeviceStops.Add(device, Transactions.Count);
			else
				InternalStopDevice(device);
		}
		
		private readonly DeferredTeardowns<Schema.ServerLink> _deferredRemoteSessionCloses = new DeferredTeardowns<Schema.ServerLink>();
		
		/// <summary>Closes this process's connection to a server link whose catalog entry has just changed - a drop, an alter, or a change to its link users - so the next use of that link connects afresh.</summary>
		/// <remarks>
		/// <para>
		/// Deferred inside a transaction because this process's remote session may be a participant in it (ServerProcess
		/// enlists its remote sessions in its own commits and rollbacks): closing it at once would roll the remote work
		/// back under a transaction that is still running, and the eventual commit would prepare a session that no
		/// longer exists.
		/// </para>
		/// <para>
		/// This process's connection only. Another process that already holds one to the same link keeps it until that
		/// process ends; see the entry in docs/migration-notes.md for what that means for such a change.
		/// </para>
		/// </remarks>
		public void CloseRemoteSession(Schema.ServerLink serverLink)
		{
			if (ServerProcess.InTransaction)
				_deferredRemoteSessionCloses.Add(serverLink, Transactions.Count);
			else
				InternalCloseRemoteSession(serverLink);
		}
		
		/// <remarks>Checked rather than asked for outright: RemoteDisconnect raises NoRemoteSessionForServerLink for a link this process never used, and a drop is not that caller.</remarks>
		private void InternalCloseRemoteSession(Schema.ServerLink serverLink)
		{
			if (ServerProcess.RemoteSessions.Contains(serverLink))
				ServerProcess.RemoteDisconnect(serverLink);
		}
		
		public void DropDevice(Schema.Device device)
		{
			DeleteCatalogObject(device);
		}

		public void SetDeviceReconcileMode(Schema.Device device, ReconcileMode reconcileMode)
		{
			ReconcileMode originalReconcileMode = device.ReconcileMode;
			device.ReconcileMode = reconcileMode;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetDeviceReconcileModeInstruction(device, originalReconcileMode));
			#endif
		}

		public void SetDeviceReconcileMaster(Schema.Device device, ReconcileMaster reconcileMaster)
		{
			ReconcileMaster originalReconcileMaster = device.ReconcileMaster;
			device.ReconcileMaster = reconcileMaster;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetDeviceReconcileMasterInstruction(device, originalReconcileMaster));
			#endif
		}
		
		#endregion
		
		#region Device user
		
		public virtual Schema.DeviceUser ResolveDeviceUser(Schema.Device device, Schema.User user, bool mustResolve)
		{
			lock (device.Users)
			{
				Schema.DeviceUser deviceUser;
				if (!device.Users.TryGetValue(user.ID, out deviceUser) && mustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DeviceUserNotFound, user.ID);

				return deviceUser;
			}
		}

		public Schema.DeviceUser ResolveDeviceUser(Schema.Device device, Schema.User user)
		{
			return ResolveDeviceUser(device, user, true);
		}

		public bool DeviceUserExists(Schema.Device device, Schema.User user)
		{
			return ResolveDeviceUser(device, user, false) != null;
		}

		#endregion

		#region Server link

		/// <summary>Returns the mapping that gives a local user its account on the linked server; raises ServerLinkUserNotFound if the user has none.</summary>
		/// <remarks>ServerLinkUsers derives from Dictionary, so indexing it for an unmapped user id raises a bare KeyNotFoundException out of whichever D4 operator did so.</remarks>
		public Schema.ServerLinkUser ResolveServerLinkUser(Schema.ServerLink serverLink, string userID)
		{
			Schema.ServerLinkUser serverLinkUser;
			if (!serverLink.Users.TryGetValue(userID, out serverLinkUser))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.ServerLinkUserNotFound, userID);

			return serverLinkUser;
		}

		/// <summary>Adds a link user to a server link in the catalog cache, recording the addition so that a rollback removes it again.</summary>
		/// <remarks>
		/// <para>
		/// The caller persists the link afterwards, with UpdateCatalogObject. That order is forced: the store holds the link's
		/// own create script, which carries its link users, so the cache has to hold the new mapping before the store can be
		/// given the text for it. A store write that then fails leaves the cache changed, which inside a transaction is exactly
		/// what the instruction recorded here undoes; outside one, nothing records anything for any of these changes.
		/// </para>
		/// <para>
		/// The catalog's lock is not taken, here or in the rest of this region: UpdateCatalogObject makes a network call to the
		/// other server (see the remarks there and on CloseRemoteSession), and a caller must not be holding that lock across it.
		/// </para>
		/// </remarks>
		public void CreateServerLinkUser(Schema.ServerLink serverLink, Schema.ServerLinkUser serverLinkUser)
		{
			serverLink.Users.Add(serverLinkUser);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new CreateServerLinkUserInstruction(serverLink, serverLinkUser));
			#endif
		}

		/// <summary>Removes a local user's mapping from a server link in the catalog cache, recording the removal so that a rollback puts it back.</summary>
		public void DropServerLinkUser(Schema.ServerLink serverLink, string userID)
		{
			Schema.ServerLinkUser serverLinkUser = ResolveServerLinkUser(serverLink, userID);
			serverLink.Users.Remove(userID);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DropServerLinkUserInstruction(serverLink, serverLinkUser));
			#endif
		}

		/// <summary>Changes the account a local user connects to the linked server with, recording the account it replaces so that a rollback restores it.</summary>
		public void SetServerLinkUserID(Schema.ServerLink serverLink, string userID, string serverLinkUserID)
		{
			Schema.ServerLinkUser serverLinkUser = ResolveServerLinkUser(serverLink, userID);
			string originalServerLinkUserID = serverLinkUser.ServerLinkUserID;
			serverLinkUser.ServerLinkUserID = serverLinkUserID;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetServerLinkUserIDInstruction(serverLinkUser, originalServerLinkUserID));
			#endif
		}

		/// <summary>Changes the encrypted password a local user connects to the linked server with, recording the password it replaces so that a rollback restores it.</summary>
		public void SetServerLinkUserPassword(Schema.ServerLink serverLink, string userID, string serverLinkPassword)
		{
			Schema.ServerLinkUser serverLinkUser = ResolveServerLinkUser(serverLink, userID);
			string originalServerLinkPassword = serverLinkUser.ServerLinkPassword;
			serverLinkUser.ServerLinkPassword = serverLinkPassword;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetServerLinkUserPasswordInstruction(serverLinkUser, originalServerLinkPassword));
			#endif
		}

		/// <summary>Recomputes a server link's connection settings from its metadata, recording the settings it replaces so that a rollback restores them.</summary>
		/// <remarks>
		/// Recorded separately from the metadata itself, which AlterMetaData records: the settings are what a connection is made
		/// from and what System.ServerLinks reports, and the metadata instruction's undo only puts the metadata object back.
		/// </remarks>
		public void ApplyServerLinkMetaData(Schema.ServerLink serverLink)
		{
			#if LOGDDLINSTRUCTIONS
			// Recorded before the change, as AlterMetaData records its own: the instruction takes the settings as they stand, and
			// ApplyMetaData throws on a tag value it cannot convert (a port number that is not a number) after ResetServerLink has
			// cleared them, so a statement that fails part-way is undone as well as one that is rolled back.
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new ApplyServerLinkMetaDataInstruction(serverLink));
			#endif

			serverLink.ResetServerLink();
			serverLink.ApplyMetaData();
		}

		#endregion

		#region Device scalar type
		
		private void AttachDeviceScalarType(Schema.DeviceScalarType deviceScalarType)
		{
			deviceScalarType.Device.AddDeviceScalarType(deviceScalarType);
		}
		
		private void DetachDeviceScalarType(Schema.DeviceScalarType deviceScalarType)
		{
			deviceScalarType.Device.RemoveDeviceScalarType(deviceScalarType);
		}

		public void CreateDeviceScalarType(DeviceScalarType deviceScalarType)
		{
			InsertCatalogObject(deviceScalarType);
			
			AttachDeviceScalarType(deviceScalarType);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachDeviceScalarTypeInstruction(deviceScalarType));
			#endif
		}

		public void DropDeviceScalarType(DeviceScalarType deviceScalarType)
		{
			DeleteCatalogObject(deviceScalarType);
			
			DetachDeviceScalarType(deviceScalarType);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachDeviceScalarTypeInstruction(deviceScalarType));
			#endif
		}

		#endregion
		
		#region Device table
		
		private void CreateDeviceTable(Schema.BaseTableVar table)
		{
			var node = new CreateTableNode(table);
            using (var plan = new Plan(ServerProcess))
            {
				table.Device.Prepare(plan, node);
            }

			var program = new Program(ServerProcess);
			program.Start(null);
			try
			{
				program.DeviceExecute(table.Device, node);
			}
			finally
			{
				program.Stop(null);
			}
		}
		
		private void DropDeviceTable(Schema.BaseTableVar table)
		{
			var node = new DropTableNode(table);
			using (var plan = new Plan(ServerProcess))
			{
				table.Device.Prepare(plan, node);
			}

			var program = new Program(ServerProcess);
			program.Start(null);
			try
			{
				program.DeviceExecute(table.Device, node);
			}
			finally
			{
				program.Stop(null);
			}
		}
		
		private void AttachTableMap(ApplicationTransactionDevice device, TableMap tableMap)
		{
			device.TableMaps.Add(tableMap);
		}
		
		private void DetachTableMap(ApplicationTransactionDevice device, TableMap tableMap)
		{
			device.TableMaps.RemoveAt(device.TableMaps.IndexOfName(tableMap.SourceTableVar.Name));
		}
		
		public void AddTableMap(ApplicationTransactionDevice device, TableMap tableMap)
		{
			AttachTableMap(device, tableMap);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachTableMapInstruction(device, tableMap));
			#endif
		}
		
		public void RemoveTableMap(ApplicationTransactionDevice device, TableMap tableMap)
		{
			DetachTableMap(device, tableMap);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachTableMapInstruction(device, tableMap));
			#endif
		}
		
		#endregion
		
		#region Device operator
		
		private void AttachDeviceOperator(Schema.DeviceOperator deviceOperator)
		{
			deviceOperator.Device.AddDeviceOperator(deviceOperator);
		}
		
		private void DetachDeviceOperator(Schema.DeviceOperator deviceOperator)
		{
			deviceOperator.Device.RemoveDeviceOperator(deviceOperator);
		}

		public void CreateDeviceOperator(DeviceOperator deviceOperator)
		{
			InsertCatalogObject(deviceOperator);
			
			AttachDeviceOperator(deviceOperator);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachDeviceOperatorInstruction(deviceOperator));
			#endif
		}

		public void DropDeviceOperator(DeviceOperator deviceOperator)
		{
			DeleteCatalogObject(deviceOperator);
			
			DetachDeviceOperator(deviceOperator);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachDeviceOperatorInstruction(deviceOperator));
			#endif
		}
		
		private void AttachOperatorMap(ApplicationTransaction.OperatorMap operatorMap, Schema.Operator operatorValue)
		{
			operatorMap.Operators.Add(operatorValue);
		}
		
		private void DetachOperatorMap(ApplicationTransaction.OperatorMap operatorMap, Schema.Operator operatorValue)
		{
			operatorMap.Operators.Remove(operatorValue);
		}
		
		public void AddOperatorMap(ApplicationTransaction.OperatorMap operatorMap, Schema.Operator operatorValue)
		{
			AttachOperatorMap(operatorMap, operatorValue);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new AttachOperatorMapInstruction(operatorMap, operatorValue));
			#endif
		}
		
		public void RemoveOperatorMap(ApplicationTransaction.OperatorMap operatorMap, Schema.Operator operatorValue)
		{
			DetachOperatorMap(operatorMap, operatorValue);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new DetachOperatorMapInstruction(operatorMap, operatorValue));
			#endif
		}
		
		#endregion
		
		#region Device objects

		public virtual bool HasDeviceObjects(Schema.Device device)
		{
			return false;
		}

		public virtual Schema.DeviceObject ResolveDeviceObject(Schema.Device device, Schema.Object objectValue)
		{
			return null;
		}

		public Schema.DeviceOperator ResolveDeviceOperator(Schema.Device device, Schema.Operator operatorValue)
		{
			return ResolveDeviceObject(device, operatorValue) as Schema.DeviceOperator;
		}

		public Schema.DeviceScalarType ResolveDeviceScalarType(Schema.Device device, Schema.ScalarType scalarType)
		{
			return ResolveDeviceObject(device, scalarType) as Schema.DeviceScalarType;
		}

		#endregion
		
		#region Security
		
		public virtual Right ResolveRight(string rightName, bool mustResolve)
		{
			if (mustResolve)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.RightNotFound, rightName);
			return null;
		}

		public Right ResolveRight(string rightName)
		{
			return ResolveRight(rightName, true);
		}

		public bool RightExists(string rightName)
		{
			return ResolveRight(rightName, false) != null;
		}

		public virtual void InsertRight(string rightName, string userID)
		{
			// virtual
		}

		public virtual void DeleteRight(string rightName)
		{
			RightsChanged(rightName);
		}

		public virtual bool UserHasRight(string userID, string rightName)
		{
			return true;
		}

		public void CheckUserHasRight(string userID, string rightName)
		{
			if (!UserHasRight(userID, rightName))
				throw new ServerException(ServerException.Codes.UnauthorizedRight, ErrorSeverity.Environment, userID, rightName);
		}

		protected void ClearUserCachedRightAssignments(string userID)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (Device.UsersCache.TryGetValue(userID, out user))
					user.ClearCachedRightAssignments();
			}
		}

		// What this session's open transaction changed about who has which right: the rights whose answers every user drops
		// when it ends, the users who drop all of theirs, or every user all of theirs. One record covers the whole stack of
		// levels, so a level's end may clear more than that level changed, which costs only cache misses.
		// NOTE: every object a transaction creates or drops adds its rights here, and the end walks every cached user for each;
		// if registering a large library on a server with many signed-in users shows up in profiles, record AllRightsChanged
		// past a size limit instead.
		private HashSet<string> _changedRightNames;
		private HashSet<string> _changedRightsUserIDs;
		private bool _allRightsChanged;

		/// <summary>Drops every user's cached answer for the given rights, now and again when the open transaction ends.</summary>
		/// <remarks>
		/// <para>
		/// A method that changes who has a right calls this, UserRightsChanged or AllRightsChanged after its store write. The
		/// answers are cached on the users, which every session shares, but each session reads the store through its own
		/// connection: while the transaction is open, another session's check caches the committed answer and this session's
		/// check the uncommitted one, and the end of the transaction makes one of them wrong. So the answers are cleared again
		/// once the store has committed or rolled back (ClearChangedRights).
		/// </para>
		/// <para>
		/// UserHasRight reads the store and caches its answer inside the catalog's lock, which this clear takes, so after the
		/// write a check either finished before the clear or reads the written store.
		/// </para>
		/// </remarks>
		protected void RightsChanged(params string[] rightNames)
		{
			lock (Catalog)
			{
				ClearCachedRightAssignments(rightNames);
				if (InTransaction)
				{
					if (_changedRightNames == null)
						_changedRightNames = new HashSet<string>();
					foreach (string rightName in rightNames)
						if (_changedRightNames.Add(rightName))
							Device.BeginPendingRight(rightName);
				}
			}
		}

		/// <summary>Drops all of the given user's cached answers, now and again when the open transaction ends.</summary>
		/// <remarks>For a change to the user's roles; see RightsChanged.</remarks>
		protected void UserRightsChanged(string userID)
		{
			lock (Catalog)
			{
				ClearUserCachedRightAssignments(userID);
				if (InTransaction)
				{
					if (_changedRightsUserIDs == null)
						_changedRightsUserIDs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					if (_changedRightsUserIDs.Add(userID))
						Device.BeginPendingUser(userID);
				}
			}
		}

		/// <summary>Drops every user's cached answers, now and again when the open transaction ends.</summary>
		/// <remarks>For a role drop, which changes the rights of each of the role's members; see RightsChanged.</remarks>
		protected void AllRightsChanged()
		{
			lock (Catalog)
			{
				ClearAllCachedRightAssignments();
				if (InTransaction && !_allRightsChanged)
				{
					_allRightsChanged = true;
					Device.BeginPendingAllRights();
				}
			}
		}

		private void ClearAllCachedRightAssignments()
		{
			foreach (Schema.User user in Device.UsersCache.Values)
				user.ClearCachedRightAssignments();
		}

		// Called once the store has committed or rolled back a level. Only the outermost level's end forgets the record and
		// drops this record's contribution to the device's pending counts: after a nested level ends, the outer levels'
		// changes are still uncommitted, so the device must still see them as pending for any other session's right check.
		private void ClearChangedRights(bool forget)
		{
			if ((_changedRightNames != null) || (_changedRightsUserIDs != null) || _allRightsChanged)
			{
				lock (Catalog)
				{
					if (_allRightsChanged)
						ClearAllCachedRightAssignments();
					else
					{
						if (_changedRightNames != null)
							foreach (Schema.User user in Device.UsersCache.Values)
								foreach (string rightName in _changedRightNames)
									user.ClearCachedRightAssignment(rightName);

						if (_changedRightsUserIDs != null)
							foreach (string userID in _changedRightsUserIDs)
								ClearUserCachedRightAssignments(userID);
					}

					if (forget)
					{
						if (_changedRightNames != null)
							foreach (string rightName in _changedRightNames)
								Device.EndPendingRight(rightName);
						if (_changedRightsUserIDs != null)
							foreach (string userID in _changedRightsUserIDs)
								Device.EndPendingUser(userID);
						if (_allRightsChanged)
							Device.EndPendingAllRights();
					}
				}
			}

			if (forget)
			{
				_changedRightNames = null;
				_changedRightsUserIDs = null;
				_allRightsChanged = false;
			}
		}

		/// <summary>Adds the given user to the cache, without affecting the underlying store.</summary>
		public void CacheUser(User user)
		{
			lock (Catalog)
			{
				InternalCacheUser(user);
			}
		}

		protected void InternalCacheUser(User user)
		{
			Device.UsersCache.Add(user);
		}

		/// <summary>Removes the given user from the cache, without affecting the underlying store.</summary>		
		public void ClearUser(string userID)
		{
			lock (Catalog)
			{
				Device.UsersCache.Remove(userID);
			}
		}

		/// <summary>Clears the users cache, without affecting the underlying store.</summary>		
		public void ClearUsers()
		{
			lock (Catalog)
			{
				Device.UsersCache.Clear();
			}
		}

		// NOTE: the users cache is per engine and is filled before the creating transaction commits, so another session can
		// authenticate as a user whose creation has not committed (a rollback removes it again, through CreateUserInstruction).
		// It holds no rights there: the role membership CreateUser also inserts is transactional, and the pending-user
		// machinery (CatalogDevice.HasPendingUser) keeps another session from reading an uncommitted grant. If an uncommitted
		// user must stop being visible at all, that machinery is the model to follow here.
		public virtual void InsertUser(Schema.User user)
		{
			CacheUser(user);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				_instructions.Add(new CreateUserInstruction(user));
			#endif
		}

		public Schema.User ResolveUser(string userID, bool mustResolve)
		{
			lock (Catalog)
			{
				Schema.User user;
				if (!Device.UsersCache.TryGetValue(userID, out user))
				{
					user = InternalResolveUser(userID, user);
				}

				if ((user == null) && mustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.UserNotFound, userID);

				return user;
			}
		}

		protected virtual Schema.User InternalResolveUser(string userID, Schema.User LUser)
		{
			return null;
		}

		public Schema.User ResolveUser(string userID)
		{
			return ResolveUser(userID, true);
		}

		protected class CreateUserInstruction : DDLInstruction
		{
			public CreateUserInstruction(Schema.User user) : base()
			{
				_user = user;
			}
			
			private Schema.User _user;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.ClearUser(_user.ID);
			}
		}
		
		protected class DropUserInstruction : DDLInstruction
		{
			public DropUserInstruction(Schema.User user) : base()
			{
				_user = user;
			}
			
			private Schema.User _user;
			
			public override void Undo(CatalogDeviceSession session)
			{
				session.CacheUser(_user);
			}
		}
		
		public void InsertRole(Schema.Role role)
		{
			// Add the role to the Cache
			CacheCatalogObject(role);

			// Clear the name cache (this is done in InsertPersistentObject for all other catalog objects)
			Device.NameCache.Clear(role.Name);

			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// log the DDL instruction
				if (ServerProcess.InTransaction)
					_instructions.Add(new CreateCatalogObjectInstruction(role));
				#endif

				InternalInsertRole(role);
			}
		}

		protected virtual void InternalInsertRole(Schema.Role role)
		{
			// virtual
		}

		public void DeleteRole(Schema.Role role)
		{
			lock (Catalog)
			{
				// Remove the object from the catalog cache
				ClearCatalogObject(role);
			}

			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// log the DDL instruction
				if (ServerProcess.InTransaction)
					_instructions.Add(new DropCatalogObjectInstruction(role));
				#endif

				// If this is not a repository, remove it from the catalog store
				InternalDeleteRole(role);
			}
		}

		protected virtual void InternalDeleteRole(Schema.Role role)
		{
			// virtual
		}

		#endregion
	}
}
