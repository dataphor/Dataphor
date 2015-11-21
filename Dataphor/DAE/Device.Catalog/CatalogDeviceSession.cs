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
			public SetScalarTypeSortInstruction(Schema.ScalarType scalarType, Schema.Sort originalSort, bool isUnique)
			{
				_scalarType = scalarType;
				_originalSort = originalSort;
				_isUnique = isUnique;
			}
			
			private Schema.ScalarType _scalarType;
			private Schema.Sort _originalSort;
			private bool _isUnique;

			public override void Undo(CatalogDeviceSession session)
			{
				session.SetScalarTypeSort(_scalarType, _originalSort, _isUnique);
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
			public AlterClassDefinitionInstruction(ClassDefinition classDefinition, AlterClassDefinition alterClassDefinition, ClassDefinition originalClassDefinition, object instance)
			{
				_classDefinition = classDefinition;
				_alterClassDefinition = alterClassDefinition;
				_originalClassDefinition = originalClassDefinition;
				_instance = instance;
			}
			
			private ClassDefinition _classDefinition;
			private AlterClassDefinition _alterClassDefinition;
			private ClassDefinition _originalClassDefinition;
			private object _instance;

			public override void Undo(CatalogDeviceSession session)
			{
				AlterClassDefinition undoClassDefinition = new AlterClassDefinition();
				undoClassDefinition.ClassName = _alterClassDefinition.ClassName == String.Empty ? String.Empty : _originalClassDefinition.ClassName;
				
				foreach (ClassAttributeDefinition attributeDefinition in _alterClassDefinition.DropAttributes)
					undoClassDefinition.CreateAttributes.Add(new ClassAttributeDefinition(attributeDefinition.AttributeName, _originalClassDefinition.Attributes[attributeDefinition.AttributeName].AttributeValue));
					
				foreach (ClassAttributeDefinition attributeDefinition in _alterClassDefinition.AlterAttributes)
					undoClassDefinition.AlterAttributes.Add(new ClassAttributeDefinition(attributeDefinition.AttributeName, _originalClassDefinition.Attributes[attributeDefinition.AttributeName].AttributeValue));
					
				foreach (ClassAttributeDefinition attributeDefinition in _alterClassDefinition.CreateAttributes)
					undoClassDefinition.DropAttributes.Add(new ClassAttributeDefinition(attributeDefinition.AttributeName, String.Empty));
				
				AlterNode.AlterClassDefinition(_classDefinition, undoClassDefinition, _instance);
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

			InternalAfterCommitTransaction();
				
			ExecuteDeferredDeviceStops();
		}

		protected virtual void InternalAfterCommitTransaction()
		{
			// virtual
		}

		protected override void InternalRollbackTransaction()
		{
			base.InternalRollbackTransaction();
			
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
				InternalAfterRollbackTransaction();
					
				ClearDeferredDeviceStops();
			}
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
			lock (Catalog)
			{
				// if the object is already in the cache (by name), then it must be there as a result of some error
				// and the best course of action in a production scenario is to just replace it with the new object
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
				Device.CatalogIndex.Remove(objectValue.ID);
				
				// Remove the object from the cache
				Catalog.SafeRemove(objectValue);
				
				// Clear the name resolution cache
				Device.NameCache.Clear(objectValue.Name);
				
				// Clear the operator name resolution cache
				Device.OperatorNameCache.Clear(objectValue.Name);

				ClearCachedRightAssignments(objectValue.GetRights());
			}
		}
		
		protected void ClearCachedRightAssignments(string[] rightNames)
		{
			foreach (Schema.User user in Device.UsersCache.Values)
				user.ClearCachedRightAssignments(rightNames);
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
		public void UpdateCatalogObject(Schema.CatalogObject objectValue)
		{
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
				InternalUpdateCatalogObject(objectValue);
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
			}
		}
		
		private void RemoveImplicitConversion(Schema.Conversion conversion)
		{
			lock (Catalog)
			{
				conversion.SourceScalarType.ImplicitConversions.SafeRemove(conversion);
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
		
		private void SetScalarTypeSort(Schema.ScalarType scalarType, Schema.Sort sort, bool isUnique)
		{
			if (isUnique)
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

		public void AttachSort(Schema.ScalarType scalarType, Schema.Sort sort, bool isUnique)
		{
			#if LOGDDLINSTRUCTIONS
			Schema.Sort originalSort = isUnique ? scalarType.UniqueSort : scalarType.Sort;
			#endif
			SetScalarTypeSort(scalarType, sort, isUnique);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetScalarTypeSortInstruction(scalarType, originalSort, isUnique));
			#endif
		}
		
		public void DetachSort(Schema.ScalarType scalarType, Schema.Sort sort, bool isUnique)
		{
			SetScalarTypeSort(scalarType, null, isUnique);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				_instructions.Add(new SetScalarTypeSortInstruction(scalarType, sort, isUnique));
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
			InsertCatalogObject(operatorValue);
			
			if (operatorValue.SessionObjectName != null)
			{
				CreateSessionOperator(operatorValue);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new CreateSessionOperatorInstruction(operatorValue));
				#endif
			}
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
				ClassDefinition originalClassDefinition = classDefinition.Clone() as ClassDefinition;
				AlterNode.AlterClassDefinition(classDefinition, alterClassDefinition, instance);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					_instructions.Add(new AlterClassDefinitionInstruction(classDefinition, alterClassDefinition, originalClassDefinition, instance));
				#endif
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
		
		private List<Schema.Device> _deferredDeviceStops;
		private void AddDeferredDeviceStop(Schema.Device device)
		{
			if (_deferredDeviceStops == null)
				_deferredDeviceStops = new List<Schema.Device>();
			_deferredDeviceStops.Add(device);
		}
		
		private void ExecuteDeferredDeviceStops()
		{
			if (_deferredDeviceStops != null)
			{
				while (_deferredDeviceStops.Count > 0)
				{
					InternalStopDevice(_deferredDeviceStops[0]);
					_deferredDeviceStops.RemoveAt(0);
				}
				
				_deferredDeviceStops = null;
			}
		}
		
		private void ClearDeferredDeviceStops()
		{
			if (_deferredDeviceStops != null)
				_deferredDeviceStops = null;
		}
		
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
				AddDeferredDeviceStop(device);
			else
				InternalStopDevice(device);
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
			lock (Catalog)
			{
				// TODO: Look at speeding this up with an index of users for each right? Memory usage may outweigh the benefits of this index...
				foreach (Schema.User user in Device.UsersCache.Values)
					user.ClearCachedRightAssignment(rightName);
			}
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
			Schema.User user;
			if (Device.UsersCache.TryGetValue(userID, out user))
				user.ClearCachedRightAssignments();
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
