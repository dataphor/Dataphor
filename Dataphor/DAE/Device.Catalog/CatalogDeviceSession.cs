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
		protected internal CatalogDeviceSession(Schema.Device ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		public Schema.Catalog Catalog { get { return ((Server.Engine)ServerProcess.ServerSession.Server).Catalog; } }

		#region Execute
		
		protected override object InternalExecute(Program AProgram, Schema.DevicePlan ADevicePlan)
		{
			if ((ADevicePlan.Node is BaseTableVarNode) || (ADevicePlan.Node is OrderNode))
			{
				Schema.TableVar LTableVar = null;
				if (ADevicePlan.Node is BaseTableVarNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node).TableVar;
				else if (ADevicePlan.Node is OrderNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node.Nodes[0]).TableVar;
				if (LTableVar != null)
				{
					lock (Device.Headers)
					{
						CatalogHeader LHeader = Device.Headers[LTableVar];
						if ((LHeader.CacheLevel == CatalogCacheLevel.None) || ((LHeader.CacheLevel == CatalogCacheLevel.Normal) && (Catalog.TimeStamp > LHeader.TimeStamp)) || ((LHeader.CacheLevel == CatalogCacheLevel.Maintained) && !LHeader.Cached))
						{
							Device.PopulateTableVar(AProgram, LHeader);
							if ((LHeader.CacheLevel == CatalogCacheLevel.Maintained) && !LHeader.Cached)
								LHeader.Cached = true;
						}
					}
				}
			}
			object LResult = base.InternalExecute(AProgram, ADevicePlan);
			if (ADevicePlan.Node is CreateTableNode)
			{
				Schema.TableVar LTableVar = ((CreateTableNode)ADevicePlan.Node).Table;
				CatalogCacheLevel LCacheLevel = (CatalogCacheLevel)Enum.Parse(typeof(CatalogCacheLevel), MetaData.GetTag(LTableVar.MetaData, "Catalog.CacheLevel", "Normal"), true);
				if (!((LCacheLevel == CatalogCacheLevel.StoreTable) || (LCacheLevel == CatalogCacheLevel.StoreView)))
				{
					lock (Device.Headers)
					{
						CatalogHeader LHeader = new CatalogHeader(LTableVar, Device.Tables[LTableVar], Int64.MinValue, LCacheLevel);
						Device.Headers.Add(LHeader);
					}
				}
			}
			return LResult;
		}

		#endregion
				
		#region Instructions
		
		#if LOGDDLINSTRUCTIONS
		protected abstract class DDLInstruction 
		{
			public virtual void Undo(CatalogDeviceSession ASession) {}
		}

		protected class DDLInstructionLog : List<DDLInstruction> {}
		
		protected class BeginTransactionInstruction : DDLInstruction {}
		
		protected class SetUserNameInstruction : DDLInstruction
		{
			public SetUserNameInstruction(Schema.User AUser, string AOriginalName) : base()
			{
				FUser = AUser;
				FOriginalName = AOriginalName;
			}
			
			private Schema.User FUser;
			private string FOriginalName;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				FUser.Name = FOriginalName;
			}
		}
		
		protected class SetUserPasswordInstruction : DDLInstruction
		{
			public SetUserPasswordInstruction(Schema.User AUser, string AOriginalPassword) : base()
			{
				FUser = AUser;
				FOriginalPassword = AOriginalPassword;
			}
			
			private Schema.User FUser;
			private string FOriginalPassword;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				FUser.Password = FOriginalPassword;
			}
		}
		
		protected class SetDeviceUserIDInstruction : DDLInstruction
		{
			public SetDeviceUserIDInstruction(Schema.DeviceUser ADeviceUser, string AOriginalUserID) : base()
			{
				FDeviceUser = ADeviceUser;
				FOriginalUserID = AOriginalUserID;
			}
			
			private Schema.DeviceUser FDeviceUser;
			private string FOriginalUserID;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				FDeviceUser.DeviceUserID = FOriginalUserID;
			}
		}
		
		protected class SetDeviceUserPasswordInstruction : DDLInstruction
		{
			public SetDeviceUserPasswordInstruction(Schema.DeviceUser ADeviceUser, string AOriginalPassword) : base()
			{
				FDeviceUser = ADeviceUser;
				FOriginalPassword = AOriginalPassword;
			}
			
			private Schema.DeviceUser FDeviceUser;
			private string FOriginalPassword;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				FDeviceUser.DevicePassword = FOriginalPassword;
			}
		}
		
		protected class SetDeviceUserConnectionParametersInstruction : DDLInstruction
		{
			public SetDeviceUserConnectionParametersInstruction(Schema.DeviceUser ADeviceUser, string AOriginalConnectionParameters) : base()
			{
				FDeviceUser = ADeviceUser;
				FOriginalConnectionParameters = AOriginalConnectionParameters;
			}
			
			private Schema.DeviceUser FDeviceUser;
			private string FOriginalConnectionParameters;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				FDeviceUser.ConnectionParameters = FOriginalConnectionParameters;
			}
		}
		
		protected class InsertLoadedLibraryInstruction : DDLInstruction
		{
			public InsertLoadedLibraryInstruction(Schema.LoadedLibrary ALoadedLibrary) : base()
			{
				FLoadedLibrary = ALoadedLibrary;
			}
			
			private Schema.LoadedLibrary FLoadedLibrary;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.ClearLoadedLibrary(FLoadedLibrary);
			}
		}
		
		protected class DeleteLoadedLibraryInstruction : DDLInstruction
		{
			public DeleteLoadedLibraryInstruction(Schema.LoadedLibrary ALoadedLibrary) : base()
			{
				FLoadedLibrary = ALoadedLibrary;
			}
			
			private Schema.LoadedLibrary FLoadedLibrary;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.CacheLoadedLibrary(FLoadedLibrary);
			}
		}
		
		protected class RegisterAssemblyInstruction : DDLInstruction
		{
			public RegisterAssemblyInstruction(Schema.LoadedLibrary ALoadedLibrary, Assembly AAssembly) : base()
			{
				FLoadedLibrary = ALoadedLibrary;
				FAssembly = AAssembly;
			}
			
			private Schema.LoadedLibrary FLoadedLibrary;
			private Assembly FAssembly;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.InternalUnregisterAssembly(FLoadedLibrary, FAssembly);
			}
		}
		
		protected class UnregisterAssemblyInstruction : DDLInstruction
		{
			public UnregisterAssemblyInstruction(Schema.LoadedLibrary ALoadedLibrary, Assembly AAssembly) : base()
			{
				FLoadedLibrary = ALoadedLibrary;
				FAssembly = AAssembly;
			}
			
			private Schema.LoadedLibrary FLoadedLibrary;
			private Assembly FAssembly;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.InternalRegisterAssembly(FLoadedLibrary, FAssembly);
			}
		}
		
		protected class CreateCatalogObjectInstruction : DDLInstruction
		{
			public CreateCatalogObjectInstruction(Schema.CatalogObject ACatalogObject) : base()
			{
				FCatalogObject = ACatalogObject;
			}
			
			private Schema.CatalogObject FCatalogObject;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.ClearCatalogObject(FCatalogObject);
			}
		}
		
		protected class DropCatalogObjectInstruction : DDLInstruction
		{
			public DropCatalogObjectInstruction(Schema.CatalogObject ACatalogObject) : base()
			{
				FCatalogObject = ACatalogObject;
			}
			
			private Schema.CatalogObject FCatalogObject;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.CacheCatalogObject(FCatalogObject);
			}
		}
		
		protected class AddDependenciesInstruction : DDLInstruction
		{
			public AddDependenciesInstruction(Schema.Object AObject) : base()
			{
				FObject = AObject;
			}
			
			private Schema.Object FObject;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FObject.Dependencies.Clear();
			}
		}
		
		protected class RemoveDependenciesInstruction : DDLInstruction
		{
			public RemoveDependenciesInstruction(Schema.Object AObject, Schema.ObjectList AOriginalDependencies) : base()
			{
				FObject = AObject;
				FOriginalDependencies = AOriginalDependencies;
			}
			
			private Schema.Object FObject;
			private Schema.ObjectList FOriginalDependencies;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FObject.AddDependencies(FOriginalDependencies);
				FObject.DetermineRemotable(ASession);
			}
		}
		
		protected class CreateDeviceTableInstruction : DDLInstruction
		{
			public CreateDeviceTableInstruction(Schema.BaseTableVar ABaseTableVar) : base()
			{
				FBaseTableVar = ABaseTableVar;
			}
			
			private Schema.BaseTableVar FBaseTableVar;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DropDeviceTable(FBaseTableVar);
			}
		}
		
		protected class DropDeviceTableInstruction : DDLInstruction
		{
			public DropDeviceTableInstruction(Schema.BaseTableVar ABaseTableVar) : base()
			{
				FBaseTableVar = ABaseTableVar;
			}
			
			private Schema.BaseTableVar FBaseTableVar;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.CreateDeviceTable(FBaseTableVar);
			}
		}
		
		protected class CreateSessionObjectInstruction : DDLInstruction
		{
			public CreateSessionObjectInstruction(Schema.CatalogObject ASessionObject)
			{
				FSessionObject = ASessionObject;
			}
			
			private Schema.CatalogObject FSessionObject;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DropSessionObject(FSessionObject);
			}
		}
		
		protected class DropSessionObjectInstruction : DDLInstruction
		{
			public DropSessionObjectInstruction(Schema.CatalogObject ASessionObject)
			{
				FSessionObject = ASessionObject;
			}
			
			private Schema.CatalogObject FSessionObject;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.CreateSessionObject(FSessionObject);
			}
		}
		
		protected class CreateSessionOperatorInstruction : DDLInstruction
		{
			public CreateSessionOperatorInstruction(Schema.Operator ASessionOperator)
			{
				FSessionOperator = ASessionOperator;
			}
			
			private Schema.Operator FSessionOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DropSessionOperator(FSessionOperator);
			}
		}
		
		protected class DropSessionOperatorInstruction : DDLInstruction
		{
			public DropSessionOperatorInstruction(Schema.Operator ASessionOperator)
			{
				FSessionOperator = ASessionOperator;
			}
			
			private Schema.Operator FSessionOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.CreateSessionOperator(FSessionOperator);
			}
		}
		
		protected class AddImplicitConversionInstruction : DDLInstruction
		{
			public AddImplicitConversionInstruction(Schema.Conversion AConversion) : base()
			{
				FConversion = AConversion;
			}
			
			private Schema.Conversion FConversion;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.RemoveImplicitConversion(FConversion);
			}
		}
		
		protected class RemoveImplicitConversionInstruction : DDLInstruction
		{
			public RemoveImplicitConversionInstruction(Schema.Conversion AConversion) : base()
			{
				FConversion = AConversion;
			}
			
			private Schema.Conversion FConversion;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AddImplicitConversion(FConversion);
			}
		}
		
		protected class SetScalarTypeSortInstruction : DDLInstruction
		{
			public SetScalarTypeSortInstruction(Schema.ScalarType AScalarType, Schema.Sort AOriginalSort, bool AIsUnique)
			{
				FScalarType = AScalarType;
				FOriginalSort = AOriginalSort;
				FIsUnique = AIsUnique;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Sort FOriginalSort;
			private bool FIsUnique;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.SetScalarTypeSort(FScalarType, FOriginalSort, FIsUnique);
			}
		}
		
		protected class ClearScalarTypeEqualityOperatorInstruction : DDLInstruction
		{
			public ClearScalarTypeEqualityOperatorInstruction(Schema.ScalarType AScalarType, Schema.Operator AOriginalEqualityOperator)
			{
				FScalarType = AScalarType;
				FOriginalEqualityOperator = AOriginalEqualityOperator;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Operator FOriginalEqualityOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FScalarType.EqualityOperator = FOriginalEqualityOperator;
			}
		}
		
		protected class ClearScalarTypeComparisonOperatorInstruction : DDLInstruction
		{
			public ClearScalarTypeComparisonOperatorInstruction(Schema.ScalarType AScalarType, Schema.Operator AOriginalComparisonOperator)
			{
				FScalarType = AScalarType;
				FOriginalComparisonOperator = AOriginalComparisonOperator;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Operator FOriginalComparisonOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FScalarType.ComparisonOperator = FOriginalComparisonOperator;
			}
		}
		
		protected class ClearScalarTypeIsSpecialOperatorInstruction : DDLInstruction
		{
			public ClearScalarTypeIsSpecialOperatorInstruction(Schema.ScalarType AScalarType, Schema.Operator AOriginalIsSpecialOperator)
			{
				FScalarType = AScalarType;
				FOriginalIsSpecialOperator = AOriginalIsSpecialOperator;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Operator FOriginalIsSpecialOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FScalarType.IsSpecialOperator = FOriginalIsSpecialOperator;
			}
		}
		
		protected class ClearRepresentationSelectorInstruction : DDLInstruction
		{
			public ClearRepresentationSelectorInstruction(Schema.Representation ARepresentation, Schema.Operator AOriginalSelector)
			{
				FRepresentation = ARepresentation;
				FOriginalSelector = AOriginalSelector;
			}
			
			private Schema.Representation FRepresentation;
			private Schema.Operator FOriginalSelector;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FRepresentation.Selector = FOriginalSelector;
			}
		}
		
		protected class ClearPropertyReadAccessorInstruction : DDLInstruction
		{
			public ClearPropertyReadAccessorInstruction(Schema.Property AProperty, Schema.Operator AOriginalReadAccessor)
			{
				FProperty = AProperty;
				FOriginalReadAccessor = AOriginalReadAccessor;
			}
			
			private Schema.Property FProperty;
			private Schema.Operator FOriginalReadAccessor;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FProperty.ReadAccessor = FOriginalReadAccessor;
			}
		}
		
		protected class ClearPropertyWriteAccessorInstruction : DDLInstruction
		{
			public ClearPropertyWriteAccessorInstruction(Schema.Property AProperty, Schema.Operator AOriginalWriteAccessor)
			{
				FProperty = AProperty;
				FOriginalWriteAccessor = AOriginalWriteAccessor;
			}
			
			private Schema.Property FProperty;
			private Schema.Operator FOriginalWriteAccessor;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FProperty.WriteAccessor = FOriginalWriteAccessor;
			}
		}
		
		protected class ClearSpecialSelectorInstruction : DDLInstruction
		{
			public ClearSpecialSelectorInstruction(Schema.Special ASpecial, Schema.Operator AOriginalSelector)
			{
				FSpecial = ASpecial;
				FOriginalSelector = AOriginalSelector;
			}
			
			private Schema.Special FSpecial;
			private Schema.Operator FOriginalSelector;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FSpecial.Selector = FOriginalSelector;
			}
		}
		
		protected class ClearSpecialComparerInstruction : DDLInstruction
		{
			public ClearSpecialComparerInstruction(Schema.Special ASpecial, Schema.Operator AOriginalComparer)
			{
				FSpecial = ASpecial;
				FOriginalComparer = AOriginalComparer;
			}
			
			private Schema.Special FSpecial;
			private Schema.Operator FOriginalComparer;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FSpecial.Comparer = FOriginalComparer;
			}
		}
		
		protected class AlterMetaDataInstruction : DDLInstruction
		{
			public AlterMetaDataInstruction(Schema.Object AObject, MetaData AOriginalMetaData)
			{
				FObject = AObject;
				FOriginalMetaData = AOriginalMetaData;
			}
			
			private Schema.Object FObject;
			private MetaData FOriginalMetaData;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FObject.MetaData = FOriginalMetaData;
			}
		}
		
		protected class AlterClassDefinitionInstruction : DDLInstruction
		{
			public AlterClassDefinitionInstruction(ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition, ClassDefinition AOriginalClassDefinition, object AInstance)
			{
				FClassDefinition = AClassDefinition;
				FAlterClassDefinition = AAlterClassDefinition;
				FOriginalClassDefinition = AOriginalClassDefinition;
				FInstance = AInstance;
			}
			
			private ClassDefinition FClassDefinition;
			private AlterClassDefinition FAlterClassDefinition;
			private ClassDefinition FOriginalClassDefinition;
			private object FInstance;

			public override void Undo(CatalogDeviceSession ASession)
			{
				AlterClassDefinition LUndoClassDefinition = new AlterClassDefinition();
				LUndoClassDefinition.ClassName = FAlterClassDefinition.ClassName == String.Empty ? String.Empty : FOriginalClassDefinition.ClassName;
				
				foreach (ClassAttributeDefinition LAttributeDefinition in FAlterClassDefinition.DropAttributes)
					LUndoClassDefinition.CreateAttributes.Add(new ClassAttributeDefinition(LAttributeDefinition.AttributeName, FOriginalClassDefinition.Attributes[LAttributeDefinition.AttributeName].AttributeValue));
					
				foreach (ClassAttributeDefinition LAttributeDefinition in FAlterClassDefinition.AlterAttributes)
					LUndoClassDefinition.AlterAttributes.Add(new ClassAttributeDefinition(LAttributeDefinition.AttributeName, FOriginalClassDefinition.Attributes[LAttributeDefinition.AttributeName].AttributeValue));
					
				foreach (ClassAttributeDefinition LAttributeDefinition in FAlterClassDefinition.CreateAttributes)
					LUndoClassDefinition.DropAttributes.Add(new ClassAttributeDefinition(LAttributeDefinition.AttributeName, String.Empty));
				
				AlterNode.AlterClassDefinition(FClassDefinition, LUndoClassDefinition, FInstance);
			}
		}
		
		protected class AttachCatalogConstraintInstruction : DDLInstruction
		{
			public AttachCatalogConstraintInstruction(Schema.CatalogConstraint ACatalogConstraint)
			{
				FCatalogConstraint = ACatalogConstraint;
			}
			
			private Schema.CatalogConstraint FCatalogConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				CreateConstraintNode.DetachConstraint(FCatalogConstraint, FCatalogConstraint.Node);
			}
		}
		
		protected class DetachCatalogConstraintInstruction : DDLInstruction
		{
			public DetachCatalogConstraintInstruction(Schema.CatalogConstraint ACatalogConstraint)
			{
				FCatalogConstraint = ACatalogConstraint;
			}
			
			private Schema.CatalogConstraint FCatalogConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				CreateConstraintNode.AttachConstraint(FCatalogConstraint, FCatalogConstraint.Node);
			}
		}
		
		protected class SetCatalogConstraintNodeInstruction : DDLInstruction
		{
			public SetCatalogConstraintNodeInstruction(Schema.CatalogConstraint ACatalogConstraint, PlanNode AOriginalNode)
			{
				FCatalogConstraint = ACatalogConstraint;
				FOriginalNode = AOriginalNode;
			}

			private Schema.CatalogConstraint FCatalogConstraint;
			private PlanNode FOriginalNode;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FCatalogConstraint.Node = FOriginalNode;
			}
		}
		
		protected class AttachKeyInstruction : DDLInstruction
		{
			public AttachKeyInstruction(Schema.TableVar ATableVar, Schema.Key AKey)
			{
				FTableVar = ATableVar;
				FKey = AKey;
			}
			
			private Schema.TableVar FTableVar;
			private Schema.Key FKey;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachKey(FTableVar, FKey);
			}
		}
		
		protected class DetachKeyInstruction : DDLInstruction
		{
			public DetachKeyInstruction(Schema.TableVar ATableVar, Schema.Key AKey)
			{
				FTableVar = ATableVar;
				FKey = AKey;
			}
			
			private Schema.TableVar FTableVar;
			private Schema.Key FKey;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachKey(FTableVar, FKey);
			}
		}
		
		protected class AttachOrderInstruction : DDLInstruction
		{
			public AttachOrderInstruction(Schema.TableVar ATableVar, Schema.Order AOrder)
			{
				FTableVar = ATableVar;
				FOrder = AOrder;
			}
			
			private Schema.TableVar FTableVar;
			private Schema.Order FOrder;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachOrder(FTableVar, FOrder);
			}
		}
		
		protected class DetachOrderInstruction : DDLInstruction
		{
			public DetachOrderInstruction(Schema.TableVar ATableVar, Schema.Order AOrder)
			{
				FTableVar = ATableVar;
				FOrder = AOrder;
			}
			
			private Schema.TableVar FTableVar;
			private Schema.Order FOrder;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachOrder(FTableVar, FOrder);
			}
		}
		
		protected class AttachTableVarConstraintInstruction : DDLInstruction
		{
			public AttachTableVarConstraintInstruction(Schema.TableVar ATableVar, Schema.TableVarConstraint ATableVarConstraint)
			{
				FTableVar = ATableVar;
				FTableVarConstraint = ATableVarConstraint;
			}
			
			private Schema.TableVar FTableVar;
			private Schema.TableVarConstraint FTableVarConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachTableVarConstraint(FTableVar, FTableVarConstraint);
			}
		}
		
		protected class DetachTableVarConstraintInstruction : DDLInstruction
		{
			public DetachTableVarConstraintInstruction(Schema.TableVar ATableVar, Schema.TableVarConstraint ATableVarConstraint)
			{
				FTableVar = ATableVar;
				FTableVarConstraint = ATableVarConstraint;
			}
			
			private Schema.TableVar FTableVar;
			private Schema.TableVarConstraint FTableVarConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachTableVarConstraint(FTableVar, FTableVarConstraint);
			}
		}
		
		protected class AttachTableVarColumnInstruction : DDLInstruction
		{
			public AttachTableVarColumnInstruction(Schema.BaseTableVar ATableVar, Schema.TableVarColumn ATableVarColumn)
			{
				FTableVar = ATableVar;
				FTableVarColumn = ATableVarColumn;
			}
			
			private Schema.BaseTableVar FTableVar;
			private Schema.TableVarColumn FTableVarColumn;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachTableVarColumn(FTableVar, FTableVarColumn);
			}
		}
		
		protected class DetachTableVarColumnInstruction : DDLInstruction
		{
			public DetachTableVarColumnInstruction(Schema.BaseTableVar ATableVar, Schema.TableVarColumn ATableVarColumn)
			{
				FTableVar = ATableVar;
				FTableVarColumn = ATableVarColumn;
			}
			
			private Schema.BaseTableVar FTableVar;
			private Schema.TableVarColumn FTableVarColumn;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachTableVarColumn(FTableVar, FTableVarColumn);
			}
		}
		
		protected class AttachScalarTypeConstraintInstruction : DDLInstruction
		{
			public AttachScalarTypeConstraintInstruction(Schema.ScalarType AScalarType, Schema.ScalarTypeConstraint AScalarTypeConstraint)
			{
				FScalarType = AScalarType;
				FScalarTypeConstraint = AScalarTypeConstraint;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.ScalarTypeConstraint FScalarTypeConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachScalarTypeConstraint(FScalarType, FScalarTypeConstraint);
			}
		}
		
		protected class DetachScalarTypeConstraintInstruction : DDLInstruction
		{
			public DetachScalarTypeConstraintInstruction(Schema.ScalarType AScalarType, Schema.ScalarTypeConstraint AScalarTypeConstraint)
			{
				FScalarType = AScalarType;
				FScalarTypeConstraint = AScalarTypeConstraint;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.ScalarTypeConstraint FScalarTypeConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachScalarTypeConstraint(FScalarType, FScalarTypeConstraint);
			}
		}
		
		protected class AttachTableVarColumnConstraintInstruction : DDLInstruction
		{
			public AttachTableVarColumnConstraintInstruction(Schema.TableVarColumn ATableVarColumn, Schema.TableVarColumnConstraint ATableVarColumnConstraint)
			{
				FTableVarColumn = ATableVarColumn;
				FTableVarColumnConstraint = ATableVarColumnConstraint;
			}
			
			private Schema.TableVarColumn FTableVarColumn;
			private Schema.TableVarColumnConstraint FTableVarColumnConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachTableVarColumnConstraint(FTableVarColumn, FTableVarColumnConstraint);
			}
		}
		
		protected class DetachTableVarColumnConstraintInstruction : DDLInstruction
		{
			public DetachTableVarColumnConstraintInstruction(Schema.TableVarColumn ATableVarColumn, Schema.TableVarColumnConstraint ATableVarColumnConstraint)
			{
				FTableVarColumn = ATableVarColumn;
				FTableVarColumnConstraint = ATableVarColumnConstraint;
			}
			
			private Schema.TableVarColumn FTableVarColumn;
			private Schema.TableVarColumnConstraint FTableVarColumnConstraint;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachTableVarColumnConstraint(FTableVarColumn, FTableVarColumnConstraint);
			}
		}
		
		protected class AttachSpecialInstruction : DDLInstruction
		{
			public AttachSpecialInstruction(Schema.ScalarType AScalarType, Schema.Special ASpecial)
			{
				FScalarType = AScalarType;
				FSpecial = ASpecial;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Special FSpecial;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachSpecial(FScalarType, FSpecial);
			}
		}
		
		protected class DetachSpecialInstruction : DDLInstruction
		{
			public DetachSpecialInstruction(Schema.ScalarType AScalarType, Schema.Special ASpecial)
			{
				FScalarType = AScalarType;
				FSpecial = ASpecial;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Special FSpecial;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachSpecial(FScalarType, FSpecial);
			}
		}
		
		protected class AttachRepresentationInstruction : DDLInstruction
		{
			public AttachRepresentationInstruction(Schema.ScalarType AScalarType, Schema.Representation ARepresentation)
			{
				FScalarType = AScalarType;
				FRepresentation = ARepresentation;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Representation FRepresentation;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachRepresentation(FScalarType, FRepresentation);
			}
		}
		
		protected class DetachRepresentationInstruction : DDLInstruction
		{
			public DetachRepresentationInstruction(Schema.ScalarType AScalarType, Schema.Representation ARepresentation)
			{
				FScalarType = AScalarType;
				FRepresentation = ARepresentation;
			}
			
			private Schema.ScalarType FScalarType;
			private Schema.Representation FRepresentation;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachRepresentation(FScalarType, FRepresentation);
			}
		}
		
		protected class AttachPropertyInstruction : DDLInstruction
		{
			public AttachPropertyInstruction(Schema.Representation ARepresentation, Schema.Property AProperty)
			{
				FRepresentation = ARepresentation;
				FProperty = AProperty;
			}
			
			private Schema.Representation FRepresentation;
			private Schema.Property FProperty;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachProperty(FRepresentation, FProperty);
			}
		}
		
		protected class DetachPropertyInstruction : DDLInstruction
		{
			public DetachPropertyInstruction(Schema.Representation ARepresentation, Schema.Property AProperty)
			{
				FRepresentation = ARepresentation;
				FProperty = AProperty;
			}
			
			private Schema.Representation FRepresentation;
			private Schema.Property FProperty;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachProperty(FRepresentation, FProperty);
			}
		}
		
		protected class SetScalarTypeDefaultInstruction : DDLInstruction
		{
			public SetScalarTypeDefaultInstruction(Schema.ScalarType AScalarType, Schema.ScalarTypeDefault AOriginalDefault)
			{
				FScalarType = AScalarType;
				FOriginalDefault = AOriginalDefault;
			}

			private Schema.ScalarType FScalarType;
			private Schema.ScalarTypeDefault FOriginalDefault;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FScalarType.Default = FOriginalDefault;
			}
		}
		
		protected class SetTableVarColumnDefaultInstruction : DDLInstruction
		{
			public SetTableVarColumnDefaultInstruction(Schema.TableVarColumn ATableVarColumn, Schema.TableVarColumnDefault AOriginalDefault)
			{
				FTableVarColumn = ATableVarColumn;
				FOriginalDefault = AOriginalDefault;
			}

			private Schema.TableVarColumn FTableVarColumn;
			private Schema.TableVarColumnDefault FOriginalDefault;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FTableVarColumn.Default = FOriginalDefault;
			}
		}
		
		protected class SetTableVarColumnIsNilableInstruction : DDLInstruction
		{
			public SetTableVarColumnIsNilableInstruction(Schema.TableVarColumn ATableVarColumn, bool AOriginalIsNilable)
			{
				FTableVarColumn = ATableVarColumn;
				FOriginalIsNilable = AOriginalIsNilable;
			}

			private Schema.TableVarColumn FTableVarColumn;
			private bool FOriginalIsNilable;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FTableVarColumn.IsNilable = FOriginalIsNilable;
			}
		}
		
		protected class SetScalarTypeIsSpecialOperatorInstruction : DDLInstruction
		{
			public SetScalarTypeIsSpecialOperatorInstruction(Schema.ScalarType AScalarType, Schema.Operator AOriginalOperator)
			{
				FScalarType = AScalarType;
				FOriginalOperator = AOriginalOperator;
			}

			private Schema.ScalarType FScalarType;
			private Schema.Operator FOriginalOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FScalarType.IsSpecialOperator = FOriginalOperator;
			}
		}
		
		protected class SetOperatorBlockNodeInstruction : DDLInstruction
		{
			public SetOperatorBlockNodeInstruction(Schema.OperatorBlock AOperatorBlock, PlanNode AOriginalNode)
			{
				FOperatorBlock = AOperatorBlock;
				FOriginalNode = AOriginalNode;
			}

			private Schema.OperatorBlock FOperatorBlock;
			private PlanNode FOriginalNode;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FOperatorBlock.BlockNode = FOriginalNode;
			}
		}
		
		protected class AttachReferenceInstruction : DDLInstruction
		{
			public AttachReferenceInstruction(Schema.Reference AReference)
			{
				FReference = AReference;
			}
			
			private Schema.Reference FReference;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachReference(FReference);
			}
		}
		
		protected class DetachReferenceInstruction : DDLInstruction
		{
			public DetachReferenceInstruction(Schema.Reference AReference)
			{
				FReference = AReference;
			}
			
			private Schema.Reference FReference;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachReference(FReference);
			}
		}
		
		protected class AttachDeviceScalarTypeInstruction : DDLInstruction
		{
			public AttachDeviceScalarTypeInstruction(Schema.DeviceScalarType ADeviceScalarType)
			{
				FDeviceScalarType = ADeviceScalarType;
			}
			
			private Schema.DeviceScalarType FDeviceScalarType;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachDeviceScalarType(FDeviceScalarType);
			}
		}
		
		protected class DetachDeviceScalarTypeInstruction : DDLInstruction
		{
			public DetachDeviceScalarTypeInstruction(Schema.DeviceScalarType ADeviceScalarType)
			{
				FDeviceScalarType = ADeviceScalarType;
			}
			
			private Schema.DeviceScalarType FDeviceScalarType;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachDeviceScalarType(FDeviceScalarType);
			}
		}
		
		protected class AttachDeviceOperatorInstruction : DDLInstruction
		{
			public AttachDeviceOperatorInstruction(Schema.DeviceOperator ADeviceOperator)
			{
				FDeviceOperator = ADeviceOperator;
			}
			
			private Schema.DeviceOperator FDeviceOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachDeviceOperator(FDeviceOperator);
			}
		}
		
		protected class DetachDeviceOperatorInstruction : DDLInstruction
		{
			public DetachDeviceOperatorInstruction(Schema.DeviceOperator ADeviceOperator)
			{
				FDeviceOperator = ADeviceOperator;
			}
			
			private Schema.DeviceOperator FDeviceOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachDeviceOperator(FDeviceOperator);
			}
		}
		
		protected class AttachTableMapInstruction : DDLInstruction
		{
			public AttachTableMapInstruction(ApplicationTransactionDevice ADevice, TableMap ATableMap)
			{
				FDevice = ADevice;
				FTableMap = ATableMap;
			}
			
			private ApplicationTransactionDevice FDevice;
			private TableMap FTableMap;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachTableMap(FDevice, FTableMap);
			}
		}
		
		protected class DetachTableMapInstruction : DDLInstruction
		{
			public DetachTableMapInstruction(ApplicationTransactionDevice ADevice, TableMap ATableMap)
			{
				FDevice = ADevice;
				FTableMap = ATableMap;
			}
			
			private ApplicationTransactionDevice FDevice;
			private TableMap FTableMap;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachTableMap(FDevice, FTableMap);
			}
		}
		
		protected class AttachOperatorMapInstruction : DDLInstruction
		{
			public AttachOperatorMapInstruction(ApplicationTransaction.OperatorMap AOperatorMap, Schema.Operator AOperator)
			{
				FOperatorMap = AOperatorMap;
				FOperator = AOperator;
			}
			
			private ApplicationTransaction.OperatorMap FOperatorMap;
			private Schema.Operator FOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachOperatorMap(FOperatorMap, FOperator);
			}
		}
		
		protected class DetachOperatorMapInstruction : DDLInstruction
		{
			public DetachOperatorMapInstruction(ApplicationTransaction.OperatorMap AOperatorMap, Schema.Operator AOperator)
			{
				FOperatorMap = AOperatorMap;
				FOperator = AOperator;
			}
			
			private ApplicationTransaction.OperatorMap FOperatorMap;
			private Schema.Operator FOperator;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachOperatorMap(FOperatorMap, FOperator);
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
			public SetDeviceReconcileModeInstruction(Schema.Device ADevice, ReconcileMode AOriginalReconcileMode)
			{
				FDevice = ADevice;
				FOriginalReconcileMode = AOriginalReconcileMode;
			}
			
			private Schema.Device FDevice;
			private ReconcileMode FOriginalReconcileMode;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FDevice.ReconcileMode = FOriginalReconcileMode;
			}
		}
		
		protected class SetDeviceReconcileMasterInstruction : DDLInstruction
		{
			public SetDeviceReconcileMasterInstruction(Schema.Device ADevice, ReconcileMaster AOriginalReconcileMaster)
			{
				FDevice = ADevice;
				FOriginalReconcileMaster = AOriginalReconcileMaster;
			}
			
			private Schema.Device FDevice;
			private ReconcileMaster FOriginalReconcileMaster;

			public override void Undo(CatalogDeviceSession ASession)
			{
				FDevice.ReconcileMaster = FOriginalReconcileMaster;
			}
		}

		protected class StartDeviceInstruction : DDLInstruction
		{
			public StartDeviceInstruction(Schema.Device ADevice) : base()
			{
				FDevice = ADevice;
			}
			
			private Schema.Device FDevice;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.StopDevice(FDevice, true);
			}
		}
		
		protected class RegisterDeviceInstruction : DDLInstruction
		{
			public RegisterDeviceInstruction(Schema.Device ADevice) : base()
			{
				FDevice = ADevice;
			}
			
			private Schema.Device FDevice;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.UnregisterDevice(FDevice);
			}
		}
		
		protected class AttachEventHandlerInstruction : DDLInstruction
		{
			public AttachEventHandlerInstruction(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
			{
				FEventHandler = AEventHandler;
				FEventSource = AEventSource;
				FEventSourceColumnIndex = AEventSourceColumnIndex;
				FBeforeOperatorNames = ABeforeOperatorNames;
			}
			
			private Schema.EventHandler FEventHandler;
			private Schema.Object FEventSource;
			private int FEventSourceColumnIndex;
			private List<string> FBeforeOperatorNames;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.DetachEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex);
			}
		}
		
		protected class MoveEventHandlerInstruction : DDLInstruction
		{
			public MoveEventHandlerInstruction(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
			{
				FEventHandler = AEventHandler;
				FEventSource = AEventSource;
				FEventSourceColumnIndex = AEventSourceColumnIndex;
				FBeforeOperatorNames = ABeforeOperatorNames;
			}
			
			private Schema.EventHandler FEventHandler;
			private Schema.Object FEventSource;
			private int FEventSourceColumnIndex;
			private List<string> FBeforeOperatorNames;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.MoveEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex, FBeforeOperatorNames);
			}
		}
		
		protected class DetachEventHandlerInstruction : DDLInstruction
		{
			public DetachEventHandlerInstruction(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
			{
				FEventHandler = AEventHandler;
				FEventSource = AEventSource;
				FEventSourceColumnIndex = AEventSourceColumnIndex;
				FBeforeOperatorNames = ABeforeOperatorNames;
			}
			
			private Schema.EventHandler FEventHandler;
			private Schema.Object FEventSource;
			private int FEventSourceColumnIndex;
			private List<string> FBeforeOperatorNames;

			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.AttachEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex, FBeforeOperatorNames);
			}
		}
		
		protected DDLInstructionLog FInstructions = new DDLInstructionLog();
		
		#endif
		
		#endregion

		#region Transactions
		
		protected override void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			base.InternalBeginTransaction(AIsolationLevel);

			#if LOGDDLINSTRUCTIONS
			FInstructions.Add(new BeginTransactionInstruction());
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
			for (int LIndex = FInstructions.Count - 1; LIndex >= 0; LIndex--)
				if (FInstructions[LIndex] is BeginTransactionInstruction)
				{
					FInstructions.RemoveAt(LIndex);
					break;
				}
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
				for (int LIndex = FInstructions.Count - 1; LIndex >= 0; LIndex--)
				{
					DDLInstruction LInstruction = FInstructions[LIndex];
					FInstructions.RemoveAt(LIndex);
					if (LInstruction is BeginTransactionInstruction)
						break;
					else
					{
						try
						{
							LInstruction.Undo(this);
						}
						catch (Exception LException)
						{
							// Log the exception and continue, not really much that can be done, should try to undo as many operations as possible
							// In at least one case, the error may be safely ignored anyway (storage object does not exist because it has already been rolled back by the device transaction rollback)
							ServerProcess.ServerSession.Server.LogError(new ServerException(ServerException.Codes.RollbackError, ErrorSeverity.System, LException, LException.ToString()));
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

		public virtual List<int> SelectOperatorHandlers(int AOperatorID)
		{
			return new List<int>();
		}

		public virtual List<int> SelectObjectHandlers(int ASourceObjectID)
		{
			return new List<int>();
		}

		public virtual Schema.DependentObjectHeaders SelectObjectDependents(int AObjectID, bool ARecursive)
		{
			return new Schema.DependentObjectHeaders();
		}

		public virtual Schema.DependentObjectHeaders SelectObjectDependencies(int AObjectID, bool ARecursive)
		{
			throw new NotSupportedException();
		}

		public virtual Schema.CatalogObjectHeaders SelectLibraryCatalogObjects(string ALibraryName)
		{
			throw new NotSupportedException();
		}
		
		public virtual Schema.CatalogObjectHeaders SelectGeneratedObjects(int AObjectID)
		{
			throw new NotSupportedException();
		}
		
		#endregion

		#region Updates
				
		protected override void InternalInsertRow(Program AProgram, Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "System.TableDum" : break;
				case "System.TableDee" : break;
			}
			base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
		}
		
		protected override void InternalUpdateRow(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "System.TableDee" : break;
				case "System.TableDum" : break;
			}
			base.InternalUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
		}
		
		protected override void InternalDeleteRow(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			switch (ATableVar.Name)
			{
				case "System.TableDee" : break;
				case "System.TableDum" : break;
			}
			base.InternalDeleteRow(AProgram, ATableVar, ARow);
		}
		
		#endregion
		
		#region Resolution
		
		/// <summary>Resolves the given name and returns the catalog object, if an unambiguous match is found. Otherwise, returns null.</summary>
		public virtual Schema.CatalogObject ResolveName(string AName, NameResolutionPath APath, List<string> ANames)
		{
			int LIndex = Catalog.ResolveName(AName, APath, ANames);
			return LIndex >= 0 ? (Schema.CatalogObject)Catalog[LIndex] : null;
		}

		/// <summary>Ensures that any potential match with the given operator name is in the cache so that operator resolution can occur.</summary>
		public void ResolveOperatorName(string AOperatorName)
		{
			if (!ServerProcess.ServerSession.Server.IsEngine)
			{
				Schema.CatalogObjectHeaders LHeaders = CachedResolveOperatorName(AOperatorName);
				
				// Only resolve operators in loaded libraries
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					if ((LHeaders[LIndex].LibraryName == String.Empty) || Catalog.LoadedLibraries.Contains(LHeaders[LIndex].LibraryName))
						ResolveCatalogObject(LHeaders[LIndex].ID);
			}
		}
		
		/// <summary>Resolves the catalog object with the given id. If the object is not found, an error is raised.</summary>
		public virtual Schema.CatalogObject ResolveCatalogObject(int AObjectID)
		{
			// TODO: Catalog deserialization concurrency
			// Right now, use the same lock as the user's cache to ensure no deadlocks can occur during deserialization.
			// This effectively places deserialization granularity at the server level, but until we
			// can provide a solution to the deserialization concurrency deadlock problem, this is all we can do.
			lock (Catalog)
			{
				// Lookup the object in the catalog index
				string LObjectName;
				if (Device.CatalogIndex.TryGetValue(AObjectID, out LObjectName))
					return (Schema.CatalogObject)Catalog[LObjectName];
				else
					return null;
			}
		}
		
		public virtual Schema.ObjectHeader GetObjectHeader(int AObjectID)
		{
			throw new NotSupportedException();
		}

		public Schema.Object ResolveObject(int AObjectID)
		{
			Schema.ObjectHeader LHeader = GetObjectHeader(AObjectID);
			if (LHeader.CatalogObjectID == -1)
				return ResolveCatalogObject(AObjectID);
				
			Schema.CatalogObject LCatalogObject = ResolveCatalogObject(LHeader.CatalogObjectID);
			return LCatalogObject.GetObjectFromHeader(LHeader);
		}
		
		#endregion

		#region Cache
		
		protected virtual Schema.CatalogObjectHeaders CachedResolveOperatorName(string AName)
		{
			return Device.OperatorNameCache.Resolve(AName);
		}

		/// <summary>Returns the cached object for the given object id, if it exists and is in the cache, null otherwise.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(int AObjectID)
		{
			return ResolveCachedCatalogObject(AObjectID, false);
		}
		
		/// <summary>Returns the cached object for the given object id, if it exists and is in the cache. An error is thrown if the object is not in the cache and AMustResolve is true, otherwise null is returned.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(int AObjectID, bool AMustResolve)
		{
			lock (Catalog)
			{
				string LObjectName;
				if (Device.CatalogIndex.TryGetValue(AObjectID, out LObjectName))
					return (Schema.CatalogObject)Catalog[LObjectName];
					
				if (AMustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotCached, AObjectID);

				return null;
			}
		}
		
		/// <summary>Returns the cached object with the given name, if it exists and is in the cache, null otherwise.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(string AName)
		{
			return ResolveCachedCatalogObject(AName, false);
		}
		
		/// <summary>Returns the cached object with the given name, if it exists and is in the cache. An error is thrown if the object is not in the cache and AMustResolve is true, otherwise null is returned.</summary>
		public Schema.CatalogObject ResolveCachedCatalogObject(string AName, bool AMustResolve)
		{
			lock (Catalog)
			{
				int LIndex = Catalog.IndexOf(AName);
				if (LIndex >= 0)
					return (Schema.CatalogObject)Catalog[LIndex];
				
				if (AMustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, AName);	
				
				return null;
			}
		}
		
		public void ClearCachedCatalogObject(Schema.CatalogObject AObject)
		{
			Schema.Objects LObjects = new Schema.Objects();
			LObjects.Add(AObject);
			ClearCachedCatalogObjects(LObjects);
		}
		
		public void ClearCachedCatalogObjects(Schema.Objects AObjects)
		{
			string[] LObjects = new string[AObjects.Count];
			for (int LIndex = 0; LIndex < AObjects.Count; LIndex++)
				LObjects[LIndex] = AObjects[LIndex].Name;

			// Push a loading context so that the drops only occur in the cache, not the store
			ServerProcess.PushLoadingContext(new LoadingContext(ServerProcess.ServerSession.Server.SystemUser, String.Empty));
			try
			{
				Plan LPlan = new Plan(ServerProcess);
				try
				{
					LPlan.PushSecurityContext(new SecurityContext(ServerProcess.ServerSession.Server.SystemUser));
					try
					{
						Block LBlock = (Block)LPlan.Catalog.EmitDropStatement(this, LObjects, String.Empty, true, true, true, true);
						Program LProgram = new Program(ServerProcess);
						foreach (Statement LStatement in LBlock.Statements)
						{
							LProgram.Code = Compiler.Bind(LPlan, Compiler.Compile(LPlan, LStatement));
							LProgram.Execute(null);
						}
					}
					finally
					{
						LPlan.PopSecurityContext();
					}
				}
				finally
				{
					LPlan.Dispose();
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
		public void CacheCatalogObject(Schema.CatalogObject AObject)
		{
			lock (Catalog)
			{
				// if the object is already in the cache (by name), then it must be there as a result of some error
				// and the best course of action in a production scenario is to just replace it with the new object
				#if !DEBUG
				int LIndex = Catalog.IndexOfName(AObject.Name);
				if (LIndex >= 0)
					ClearCatalogObject((Schema.CatalogObject)Catalog[LIndex]);
				#endif

				// Add the object to the catalog cache
				Catalog.Add(AObject);
				
				// Add the object to the cache index
				Device.CatalogIndex.Add(AObject.ID, Schema.Object.EnsureRooted(AObject.Name));
			}
		}
		
		/// <summary>Removes the given object from the catalog cache.</summary>
		private void ClearCatalogObject(Schema.CatalogObject AObject)
		{
			lock (Catalog)
			{
				// Remove the object from the cache index
				Device.CatalogIndex.Remove(AObject.ID);
				
				// Remove the object from the cache
				Catalog.SafeRemove(AObject);
				
				// Clear the name resolution cache
				Device.NameCache.Clear();
				
				// Clear the operator name resolution cache
				Device.OperatorNameCache.Clear();
			}
		}
		
		#endregion
		
		#region Catalog object
		
		/// <summary>Returns true if the given object is not an A/T object.</summary>
		public bool ShouldSerializeCatalogObject(Schema.CatalogObject AObject)
		{
			return !AObject.IsATObject;
		}
		
		/// <summary>Inserts the given object into the catalog cache. If this is not a repository, also inserts the object into the catalog store.</summary>
		public virtual void InsertCatalogObject(Schema.CatalogObject AObject)
		{
			// Cache the object
			CacheCatalogObject(AObject);
			
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// Log the DDL instruction
				if (ServerProcess.InTransaction)
					FInstructions.Add(new CreateCatalogObjectInstruction(AObject));
				#endif

				InternalInsertCatalogObject(AObject);
			}
		}

		protected virtual void InternalInsertCatalogObject(Schema.CatalogObject AObject)
		{
			// virtual
		}
		
		/// <summary>Updates the given object in the catalog cache. If this is not a repository, also updates the object in the catalog store.</summary>
		public void UpdateCatalogObject(Schema.CatalogObject AObject)
		{
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
				InternalUpdateCatalogObject(AObject);
		}

		protected virtual void InternalUpdateCatalogObject(Schema.CatalogObject AObject)
		{
			// virtual
		}
		
		/// <summary>Deletes the given object in the catalog cache. If this is not a repository, also deletes the object in the catalog store.</summary>
		public void DeleteCatalogObject(Schema.CatalogObject AObject)
		{
			lock (Catalog)
			{
				// Remove the object from the catalog cache
				ClearCatalogObject(AObject);
			}
			
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// Log the DDL instruction
				if (ServerProcess.InTransaction)
					FInstructions.Add(new DropCatalogObjectInstruction(AObject));
				#endif

				InternalDeleteCatalogObject(AObject);
			}
		}

		protected virtual void InternalDeleteCatalogObject(Schema.CatalogObject AObject)
		{
			// virtual
		}

		public virtual bool CatalogObjectExists(string AObjectName)
		{
			return false;
		}
		
		#endregion
		
		#region Loaded library
		
		private void CacheLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			Catalog.LoadedLibraries.Add(ALoadedLibrary);
		}
		
		private void ClearLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			Catalog.LoadedLibraries.Remove(ALoadedLibrary);
		}
		
		public void InsertLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			Catalog.UpdateTimeStamp();
			
			CacheLoadedLibrary(ALoadedLibrary);
			
			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new InsertLoadedLibraryInstruction(ALoadedLibrary));
			#endif

			InternalInsertLoadedLibrary(ALoadedLibrary);
		}

		protected virtual void InternalInsertLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			// virtual
		}

		public void DeleteLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			Catalog.UpdateTimeStamp();
			
			ClearLoadedLibrary(ALoadedLibrary);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new DeleteLoadedLibraryInstruction(ALoadedLibrary));
			#endif
			
			InternalDeleteLoadedLibrary(ALoadedLibrary);
		}

		protected virtual void InternalDeleteLoadedLibrary(Schema.LoadedLibrary ALoadedLibrary)
		{
			// virtual
		}
		
		public virtual void ResolveLoadedLibraries()
		{
			// virtual
		}
		
		public bool IsLoadedLibrary(string ALibraryName)
		{
			return ResolveLoadedLibrary(ALibraryName, false) != null;
		}
		
		public Schema.LoadedLibrary ResolveLoadedLibrary(string ALibraryName)
		{
			return ResolveLoadedLibrary(ALibraryName, true);
		}
		
		public Schema.LoadedLibrary ResolveLoadedLibrary(string ALibraryName, bool AMustResolve)
		{
			Schema.Library LLibrary = Catalog.Libraries[ALibraryName];
			int LIndex = Catalog.LoadedLibraries.IndexOfName(LLibrary.Name);
			
			if (LIndex >= 0)
				return Catalog.LoadedLibraries[LIndex];
				
			var LResult = InternalResolveLoadedLibrary(LLibrary);
			if (LResult != null)
				return LResult;
			
			if (AMustResolve)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryNotRegistered, LLibrary.Name);
				
			return null;
		}

		protected virtual LoadedLibrary InternalResolveLoadedLibrary(Schema.Library LLibrary)
		{
			return null;
		}
		
		#endregion

		#region Assembly registration
		
		private void InternalRegisterAssembly(Schema.LoadedLibrary ALoadedLibrary, Assembly AAssembly)
		{
			Catalog.ClassLoader.RegisterAssembly(ALoadedLibrary, AAssembly);
			ALoadedLibrary.Assemblies.Add(AAssembly);
		}
		
		public void RegisterAssembly(Schema.LoadedLibrary ALoadedLibrary, Assembly AAssembly)
		{
			InternalRegisterAssembly(ALoadedLibrary, AAssembly);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new RegisterAssemblyInstruction(ALoadedLibrary, AAssembly));
			#endif
		}

		private void InternalUnregisterAssembly(Schema.LoadedLibrary ALoadedLibrary, Assembly AAssembly)
		{
			Catalog.ClassLoader.UnregisterAssembly(AAssembly);
			ALoadedLibrary.Assemblies.Remove(AAssembly);
		}

		public void UnregisterAssembly(Schema.LoadedLibrary ALoadedLibrary, Assembly AAssembly)
		{
			InternalUnregisterAssembly(ALoadedLibrary, AAssembly);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new UnregisterAssemblyInstruction(ALoadedLibrary, AAssembly));
			#endif
		}

		#endregion

		#region Session objects
				
		private void CreateSessionObject(Schema.CatalogObject ASessionObject)
		{
			lock (ServerProcess.ServerSession.SessionObjects)
			{
				ServerProcess.ServerSession.SessionObjects.Add(new Schema.SessionObject(ASessionObject.SessionObjectName, ASessionObject.Name));
			}
		}
		
		private void DropSessionObject(Schema.CatalogObject ASessionObject)
		{
			ServerProcess.ServerSession.Server.DropSessionObject(ASessionObject);
		}
		
		private void CreateSessionOperator(Schema.Operator ASessionOperator)
		{
			lock (ServerProcess.ServerSession.SessionOperators)
			{
				if (!ServerProcess.ServerSession.SessionOperators.ContainsName(ASessionOperator.SessionObjectName))
					ServerProcess.ServerSession.SessionOperators.Add(new Schema.SessionObject(ASessionOperator.SessionObjectName, ASessionOperator.OperatorName));
			}
		}
		
		private void DropSessionOperator(Schema.Operator ASessionOperator)
		{
			ServerProcess.ServerSession.Server.DropSessionOperator(ASessionOperator);
		}
		
		#endregion
		
		#region Table
		
		public void CreateTable(Schema.BaseTableVar ATable)
		{
			InsertCatalogObject(ATable);
			
			if (ATable.SessionObjectName != null)
			{
				CreateSessionObject(ATable);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new CreateSessionObjectInstruction(ATable));
				#endif
			}
			
			if (!ServerProcess.ServerSession.Server.IsEngine && ServerProcess.IsReconciliationEnabled())
			{
				CreateDeviceTable(ATable);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new CreateDeviceTableInstruction(ATable));
				#endif
			}
		}
		
		public void DropTable(Schema.BaseTableVar ATable)
		{
			DeleteCatalogObject(ATable);
			
			if (ATable.SessionObjectName != null)
			{
				DropSessionObject(ATable);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DropSessionObjectInstruction(ATable));
				#endif
			}
			
			if (!ServerProcess.ServerSession.Server.IsEngine && ServerProcess.IsReconciliationEnabled())
			{
				DropDeviceTable(ATable);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DropDeviceTableInstruction(ATable));
				#endif
			}
		}
		
		#endregion
		
		#region View
		
		public void CreateView(Schema.DerivedTableVar AView)
		{
			InsertCatalogObject(AView);
			
			if (AView.SessionObjectName != null)
			{
				CreateSessionObject(AView);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new CreateSessionObjectInstruction(AView));
				#endif
			}
		}
		
		public void DropView(Schema.DerivedTableVar AView)
		{
			DeleteCatalogObject(AView);
			
			if (AView.SessionObjectName != null)
			{
				DropSessionObject(AView);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DropSessionObjectInstruction(AView));
				#endif
			}
		}
		
		public void MarkViewForRecompile(int AObjectID)
		{
			string LObjectName;
			if (Device.CatalogIndex.TryGetValue(AObjectID, out LObjectName))
				((Schema.DerivedTableVar)Catalog[LObjectName]).ShouldReinferReferences = true;
		}

		#endregion
		
		#region Conversions
		
		private void AddImplicitConversion(Schema.Conversion AConversion)
		{
			lock (Catalog)
			{
				AConversion.SourceScalarType.ImplicitConversions.Add(AConversion);
			}
		}
		
		private void RemoveImplicitConversion(Schema.Conversion AConversion)
		{
			lock (Catalog)
			{
				AConversion.SourceScalarType.ImplicitConversions.SafeRemove(AConversion);
			}
		}
		
		public void CreateConversion(Schema.Conversion AConversion)
		{
			InsertCatalogObject(AConversion);
			
			AddImplicitConversion(AConversion);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AddImplicitConversionInstruction(AConversion));
			#endif
		}
		
		public void DropConversion(Schema.Conversion AConversion)
		{
			DeleteCatalogObject(AConversion);
			
			RemoveImplicitConversion(AConversion);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new RemoveImplicitConversionInstruction(AConversion));
			#endif
		}
		
		#endregion
		
		#region Sort
		
		private void SetScalarTypeSort(Schema.ScalarType AScalarType, Schema.Sort ASort, bool AIsUnique)
		{
			if (AIsUnique)
				AScalarType.UniqueSort = ASort;
			else
				AScalarType.Sort = ASort;
		}
		
		public void CreateSort(Schema.Sort ASort)
		{
			InsertCatalogObject(ASort);
		}
		
		public void DropSort(Schema.Sort ASort)
		{
			DeleteCatalogObject(ASort);
		}

		public void AttachSort(Schema.ScalarType AScalarType, Schema.Sort ASort, bool AIsUnique)
		{
			#if LOGDDLINSTRUCTIONS
			Schema.Sort LOriginalSort = AIsUnique ? AScalarType.UniqueSort : AScalarType.Sort;
			#endif
			SetScalarTypeSort(AScalarType, ASort, AIsUnique);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetScalarTypeSortInstruction(AScalarType, LOriginalSort, AIsUnique));
			#endif
		}
		
		public void DetachSort(Schema.ScalarType AScalarType, Schema.Sort ASort, bool AIsUnique)
		{
			SetScalarTypeSort(AScalarType, null, AIsUnique);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetScalarTypeSortInstruction(AScalarType, ASort, AIsUnique));
			#endif
		}
		
		#endregion
		
		#region Metadata
		
		public void AlterMetaData(Schema.Object AObject, AlterMetaData AAlterMetaData)
		{
			if (AAlterMetaData != null)
			{
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				{
					MetaData LMetaData = null;
					if (AObject.MetaData != null)
					{
						LMetaData = new MetaData();
						LMetaData.Merge(AObject.MetaData);
					}

					FInstructions.Add(new AlterMetaDataInstruction(AObject, LMetaData));
				}
				#endif
				
				AlterNode.AlterMetaData(AObject, AAlterMetaData);
			}
		}
		
		#endregion
		
		#region Scalar type
		
		public void CreateScalarType(Schema.ScalarType AScalarType)
		{
			InsertCatalogObject(AScalarType);
		}
		
		public void DropScalarType(Schema.ScalarType AScalarType)
		{
			DeleteCatalogObject(AScalarType);
		}
		
		#endregion
		
		#region Operator
		
		public void CreateOperator(Schema.Operator AOperator)
		{	
			InsertCatalogObject(AOperator);
			
			if (AOperator.SessionObjectName != null)
			{
				CreateSessionOperator(AOperator);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new CreateSessionOperatorInstruction(AOperator));
				#endif
			}
		}
		
		public void AlterOperator(Schema.Operator AOldOperator, Schema.Operator ANewOperator)
		{
			#if LOGDDLINSTRUCTIONS
			ObjectList LOriginalDependencies = new ObjectList();
			AOldOperator.Dependencies.CopyTo(LOriginalDependencies);
			#endif
			AOldOperator.Dependencies.Clear();
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new RemoveDependenciesInstruction(AOldOperator, LOriginalDependencies));
			#endif
				
			AOldOperator.AddDependencies(ANewOperator.Dependencies);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AddDependenciesInstruction(AOldOperator));
			#endif
			AOldOperator.DetermineRemotable(this);
			
			AlterOperatorBlockNode(AOldOperator.Block, ANewOperator.Block.BlockNode);
		}
		
		public void AlterOperatorBlockNode(Schema.OperatorBlock AOperatorBlock, PlanNode ANewNode)
		{
			#if LOGDDLINSTRUCTIONS
			PlanNode LOriginalNode = AOperatorBlock.BlockNode;
			#endif
			AOperatorBlock.BlockNode = ANewNode;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetOperatorBlockNodeInstruction(AOperatorBlock, LOriginalNode));
			#endif
		}

		public void DropOperator(Schema.Operator AOperator)
		{
			DeleteCatalogObject(AOperator);
			
			if (AOperator.SessionObjectName != null)
			{
				DropSessionOperator(AOperator);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DropSessionOperatorInstruction(AOperator));
				#endif
			}
		}
		
		#endregion
		
		#region Constraint
		
		public void CreateConstraint(Schema.CatalogConstraint AConstraint)
		{
			InsertCatalogObject(AConstraint);
			
			if (!ServerProcess.ServerSession.Server.IsEngine && AConstraint.Enforced)
			{
				CreateConstraintNode.AttachConstraint(AConstraint, AConstraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new AttachCatalogConstraintInstruction(AConstraint));
				#endif
			}
				
			if (AConstraint.SessionObjectName != null)
			{
				CreateSessionObject(AConstraint);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new CreateSessionObjectInstruction(AConstraint));
				#endif
			}
		}

		public void AlterConstraint(Schema.CatalogConstraint AOldConstraint, Schema.CatalogConstraint ANewConstraint)
		{
			if (!ServerProcess.ServerSession.Server.IsEngine && AOldConstraint.Enforced)
			{
				CreateConstraintNode.DetachConstraint(AOldConstraint, AOldConstraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DetachCatalogConstraintInstruction(AOldConstraint));
				#endif
			}
			
			#if LOGDDLINSTRUCTIONS
			ObjectList LOriginalDependencies = new ObjectList();
			AOldConstraint.Dependencies.CopyTo(LOriginalDependencies);
			#endif
			AOldConstraint.Dependencies.Clear();
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new RemoveDependenciesInstruction(AOldConstraint, LOriginalDependencies));
			#endif
				
			AOldConstraint.AddDependencies(ANewConstraint.Dependencies);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AddDependenciesInstruction(AOldConstraint));
			#endif

			#if LOGDDLINSTRUCTIONS
			PlanNode LOriginalNode = AOldConstraint.Node;
			#endif
			AOldConstraint.Node = ANewConstraint.Node;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetCatalogConstraintNodeInstruction(AOldConstraint, LOriginalNode));
			#endif

			if (!ServerProcess.ServerSession.Server.IsEngine && AOldConstraint.Enforced)
			{
				CreateConstraintNode.AttachConstraint(AOldConstraint, AOldConstraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new AttachCatalogConstraintInstruction(AOldConstraint));
				#endif
			}
		}
		
		public void DropConstraint(Schema.CatalogConstraint AConstraint)
		{
			DeleteCatalogObject(AConstraint);

			if (!ServerProcess.ServerSession.Server.IsEngine && AConstraint.Enforced)
			{
				CreateConstraintNode.DetachConstraint(AConstraint, AConstraint.Node);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DetachCatalogConstraintInstruction(AConstraint));
				#endif
			}

			if (AConstraint.SessionObjectName != null)
			{
				DropSessionObject(AConstraint);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DropSessionObjectInstruction(AConstraint));
				#endif
			}
		}
		
		#endregion
		
		#region Reference
		
		private void AttachReference(Schema.Reference AReference)
		{
			if (!ServerProcess.ServerSession.Server.IsEngine && AReference.Enforced)
			{
				if ((AReference.SourceTable is Schema.BaseTableVar) && (AReference.TargetTable is Schema.BaseTableVar))
				{
					AReference.SourceTable.Constraints.Add(AReference.SourceConstraint);
					AReference.SourceTable.InsertConstraints.Add(AReference.SourceConstraint);
					AReference.SourceTable.UpdateConstraints.Add(AReference.SourceConstraint);
					if ((AReference.UpdateReferenceAction == ReferenceAction.Require) || (AReference.DeleteReferenceAction == ReferenceAction.Require))
					{
						AReference.TargetTable.Constraints.Add(AReference.TargetConstraint);
						if (AReference.UpdateReferenceAction == ReferenceAction.Require)
							AReference.TargetTable.UpdateConstraints.Add(AReference.TargetConstraint);
						if (AReference.DeleteReferenceAction == ReferenceAction.Require)
						{
							AReference.TargetTable.DeleteConstraints.Add(AReference.TargetConstraint);
						}
					}
				}
				else
				{
					// This constraint is added only in the cache (never persisted)
					CreateConstraintNode.AttachConstraint(AReference.CatalogConstraint, AReference.CatalogConstraint.Node);
				}
				
				if ((AReference.UpdateReferenceAction == ReferenceAction.Cascade) || (AReference.UpdateReferenceAction == ReferenceAction.Clear) || (AReference.UpdateReferenceAction == ReferenceAction.Set))
				{
					// This object is added only in the cache (never persisted)
					AReference.TargetTable.EventHandlers.Add(AReference.UpdateHandler);
				}
					
				if ((AReference.DeleteReferenceAction == ReferenceAction.Cascade) || (AReference.DeleteReferenceAction == ReferenceAction.Clear) || (AReference.DeleteReferenceAction == ReferenceAction.Set))
				{
					// This object is added only in the cache (never persisted)
					AReference.TargetTable.EventHandlers.Add(AReference.DeleteHandler);
				}
			}
					
			AReference.SourceTable.SourceReferences.AddInCreationOrder(AReference);
			AReference.TargetTable.TargetReferences.AddInCreationOrder(AReference);
			
			AReference.SourceTable.SetShouldReinferReferences(this);
			AReference.TargetTable.SetShouldReinferReferences(this);
		}
		
		private void DetachReference(Schema.Reference AReference)
		{
			if ((AReference.SourceTable is Schema.BaseTableVar) && (AReference.TargetTable is Schema.BaseTableVar))
			{
				if (AReference.SourceConstraint != null)
				{
					AReference.SourceTable.InsertConstraints.SafeRemove(AReference.SourceConstraint);
					AReference.SourceTable.UpdateConstraints.SafeRemove(AReference.SourceConstraint);
					AReference.SourceTable.Constraints.SafeRemove(AReference.SourceConstraint);
				}
				
				if (AReference.TargetConstraint != null)
				{
					if ((AReference.UpdateReferenceAction == ReferenceAction.Require) || (AReference.DeleteReferenceAction == ReferenceAction.Require))
					{
						if (AReference.UpdateReferenceAction == ReferenceAction.Require)
							AReference.TargetTable.UpdateConstraints.SafeRemove(AReference.TargetConstraint);
						if (AReference.DeleteReferenceAction == ReferenceAction.Require)
							AReference.TargetTable.DeleteConstraints.SafeRemove(AReference.TargetConstraint);				
						AReference.TargetTable.Constraints.SafeRemove(AReference.TargetConstraint);
					}
				}
			}
			else
			{
				if (AReference.CatalogConstraint != null)
				{
					CreateConstraintNode.DetachConstraint(AReference.CatalogConstraint, AReference.CatalogConstraint.Node);
					ServerProcess.ServerSession.Server.RemoveCatalogConstraintCheck(AReference.CatalogConstraint);
				}
			}
			
			if (((AReference.UpdateReferenceAction == ReferenceAction.Cascade) || (AReference.UpdateReferenceAction == ReferenceAction.Clear) || (AReference.UpdateReferenceAction == ReferenceAction.Set)) && (AReference.UpdateHandler != null))
			{
				AReference.TargetTable.EventHandlers.SafeRemove(AReference.UpdateHandler);
			}
				
			if (((AReference.DeleteReferenceAction == ReferenceAction.Cascade) || (AReference.DeleteReferenceAction == ReferenceAction.Clear) || (AReference.DeleteReferenceAction == ReferenceAction.Set)) && (AReference.DeleteHandler != null))
			{
				AReference.TargetTable.EventHandlers.SafeRemove(AReference.DeleteHandler);
			}
				
			AReference.SourceTable.SourceReferences.SafeRemove(AReference);
			AReference.TargetTable.TargetReferences.SafeRemove(AReference);
			
			AReference.SourceTable.SetShouldReinferReferences(this);	
			AReference.TargetTable.SetShouldReinferReferences(this);	
		}
		
		public void CreateReference(Schema.Reference AReference)
		{
			InsertCatalogObject(AReference);

			AttachReference(AReference);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachReferenceInstruction(AReference));
			#endif

			if (AReference.SessionObjectName != null)
			{
				CreateSessionObject(AReference);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new CreateSessionObjectInstruction(AReference));
				#endif
			}
		}
		
		public void DropReference(Schema.Reference AReference)
		{
			DeleteCatalogObject(AReference);
			
			DetachReference(AReference);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachReferenceInstruction(AReference));
			#endif
			
			if (AReference.SessionObjectName != null)
			{
				DropSessionObject(AReference);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new DropSessionObjectInstruction(AReference));
				#endif
			}
		}
		
		#endregion
		
		#region Event handler
		
		private void AttachEventHandler(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
		{
			Schema.TableVar LTableVar = AEventSource as Schema.TableVar;
			if (LTableVar != null)
			{
				if (AEventSourceColumnIndex >= 0)
					LTableVar.Columns[AEventSourceColumnIndex].EventHandlers.Add(AEventHandler, ABeforeOperatorNames);
				else
					LTableVar.EventHandlers.Add(AEventHandler, ABeforeOperatorNames);
				LTableVar.DetermineRemotable(this);
			}
			else
				((Schema.ScalarType)AEventSource).EventHandlers.Add(AEventHandler);
		}

		private void MoveEventHandler(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
		{
			Schema.TableVar LTableVar = AEventSource as Schema.TableVar;
			if (LTableVar != null)
			{
				if (AEventSourceColumnIndex >= 0)
					LTableVar.Columns[AEventSourceColumnIndex].EventHandlers.MoveBefore(AEventHandler, ABeforeOperatorNames);
				else
					LTableVar.EventHandlers.MoveBefore(AEventHandler, ABeforeOperatorNames);
				LTableVar.DetermineRemotable(this);
			}
			else
				((Schema.ScalarType)AEventSource).EventHandlers.MoveBefore(AEventHandler, ABeforeOperatorNames);
		}

		private void DetachEventHandler(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex)
		{
			Schema.TableVar LTableVar = AEventSource as Schema.TableVar;
			if (LTableVar != null)
			{
				if (AEventSourceColumnIndex >= 0)
					LTableVar.Columns[AEventSourceColumnIndex].EventHandlers.SafeRemove(AEventHandler);
				else
					LTableVar.EventHandlers.SafeRemove(AEventHandler);
				LTableVar.DetermineRemotable(this);
			}
			else
				((Schema.ScalarType)AEventSource).EventHandlers.SafeRemove(AEventHandler);
		}

		private List<string> GetEventHandlerBeforeOperatorNames(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex)
		{
			List<string> LResult = new List<string>();
			EventHandlers LHandlers = null;
			Schema.TableVar LTableVar = AEventSource as Schema.TableVar;
			if (LTableVar != null)
			{
				if (AEventSourceColumnIndex >= 0)
					LHandlers = LTableVar.Columns[AEventSourceColumnIndex].EventHandlers;
				else
					LHandlers = LTableVar.EventHandlers;
			}
			else
				LHandlers = ((Schema.ScalarType)AEventSource).EventHandlers;
				
			if (LHandlers != null)
			{
				int LHandlerIndex = LHandlers.IndexOfName(AEventHandler.Name);
				for (int LIndex = LHandlerIndex; LIndex >= 0; LIndex--)
					LResult.Add(LHandlers[LIndex].Name);
			}
			
			return LResult;
		}

		public void CreateEventHandler(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
		{
			AttachEventHandler(AEventHandler, AEventSource, AEventSourceColumnIndex, ABeforeOperatorNames);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachEventHandlerInstruction(AEventHandler, AEventSource, AEventSourceColumnIndex, ABeforeOperatorNames));
			#endif

			// Note the event handlers must be attached first, otherwise properties on the event handler will not be set properly (CatalogObjectID, ParentObjectID, etc.,.)
			InsertCatalogObject(AEventHandler);
		}

		public void AlterEventHandler(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex, List<string> ABeforeOperatorNames)
		{
			#if LOGDDLINSTRUCTIONS
			List<string> LBeforeOperatorNames = GetEventHandlerBeforeOperatorNames(AEventHandler, AEventSource, AEventSourceColumnIndex);
			#endif
			MoveEventHandler(AEventHandler, AEventSource, AEventSourceColumnIndex, ABeforeOperatorNames);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new MoveEventHandlerInstruction(AEventHandler, AEventSource, AEventSourceColumnIndex, LBeforeOperatorNames));
			#endif
		}
		
		public void DropEventHandler(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex)
		{
			DeleteCatalogObject(AEventHandler);
			
			#if LOGDDLINSTRUCTIONS
			List<string> LBeforeOperatorNames = GetEventHandlerBeforeOperatorNames(AEventHandler, AEventSource, AEventSourceColumnIndex);
			#endif
			DetachEventHandler(AEventHandler, AEventSource, AEventSourceColumnIndex);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachEventHandlerInstruction(AEventHandler, AEventSource, AEventSourceColumnIndex, LBeforeOperatorNames));
			#endif
		}
		
		#endregion
		
		#region Class definitions
		
		public void AlterClassDefinition(ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition)
		{
			AlterClassDefinition(AClassDefinition, AAlterClassDefinition, null);
		}

		public void AlterClassDefinition(ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition, object AInstance)
		{
			if (AAlterClassDefinition != null)
			{
				ClassDefinition LOriginalClassDefinition = AClassDefinition.Clone() as ClassDefinition;
				AlterNode.AlterClassDefinition(AClassDefinition, AAlterClassDefinition, AInstance);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new AlterClassDefinitionInstruction(AClassDefinition, AAlterClassDefinition, LOriginalClassDefinition, AInstance));
				#endif
			}
		}
		
		#endregion
		
		#region Key
		
		private void AttachKey(TableVar ATableVar, Key AKey)
		{
			ATableVar.Keys.Add(AKey);
			if (!ATableVar.Constraints.Contains(AKey.Constraint))
				ATableVar.Constraints.Add(AKey.Constraint);
			if (!ATableVar.InsertConstraints.Contains(AKey.Constraint))
				ATableVar.InsertConstraints.Add(AKey.Constraint);
			if (!ATableVar.UpdateConstraints.Contains(AKey.Constraint))
				ATableVar.UpdateConstraints.Add(AKey.Constraint);
		}

		private void DetachKey(TableVar ATableVar, Key AKey)
		{
			ATableVar.Keys.SafeRemove(AKey);
			ATableVar.Constraints.SafeRemove(AKey.Constraint);
			ATableVar.InsertConstraints.SafeRemove(AKey.Constraint);
			ATableVar.UpdateConstraints.SafeRemove(AKey.Constraint);
		}

		public void CreateKey(TableVar ATableVar, Key AKey)
		{
			AttachKey(ATableVar, AKey);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachKeyInstruction(ATableVar, AKey));
			#endif
		}

		public void DropKey(TableVar ATableVar, Key AKey)
		{
			DetachKey(ATableVar, AKey);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachKeyInstruction(ATableVar, AKey));
			#endif
		}

		#endregion
		
		#region Order
		
		private void AttachOrder(TableVar ATableVar, Order AOrder)
		{
			ATableVar.Orders.Add(AOrder);
		}

		private void DetachOrder(TableVar ATableVar, Order AOrder)
		{
			ATableVar.Orders.SafeRemove(AOrder);
		}

		public void CreateOrder(TableVar ATableVar, Order AOrder)
		{
			AttachOrder(ATableVar, AOrder);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachOrderInstruction(ATableVar, AOrder));
			#endif
		}

		public void DropOrder(TableVar ATableVar, Order AOrder)
		{
			DetachOrder(ATableVar, AOrder);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachOrderInstruction(ATableVar, AOrder));
			#endif
		}
		
		#endregion
		
		#region TableVar column
		
		private void AttachTableVarColumn(Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn)
		{
			ATable.DataType.Columns.Add(AColumn.Column);
			ATable.Columns.Add(AColumn);
			ATable.DataType.ResetRowType();
		}

		private void DetachTableVarColumn(Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn)
		{
			ATable.DataType.Columns.SafeRemove(AColumn.Column);
			ATable.Columns.SafeRemove(AColumn);
			ATable.DataType.ResetRowType();
		}

		public void CreateTableVarColumn(Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn)
		{
			AttachTableVarColumn(ATable, AColumn);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachTableVarColumnInstruction(ATable, AColumn));
			#endif
		}

		public void DropTableVarColumn(Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn)
		{
			DetachTableVarColumn(ATable, AColumn);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachTableVarColumnInstruction(ATable, AColumn));
			#endif
		}

		public void SetTableVarColumnDefault(Schema.TableVarColumn LColumn, Schema.TableVarColumnDefault ADefault)
		{
			TableVarColumnDefault LOriginalDefault = LColumn.Default;
			LColumn.Default = ADefault;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetTableVarColumnDefaultInstruction(LColumn, LOriginalDefault));
			#endif
		}

		public void SetTableVarColumnIsNilable(TableVarColumn LColumn, bool AIsNilable)
		{
			bool LOriginalIsNilable = LColumn.IsNilable;
			LColumn.IsNilable = AIsNilable;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetTableVarColumnIsNilableInstruction(LColumn, LOriginalIsNilable));
			#endif
		}
		
		#endregion

		#region TableVar constraint
		
		private void AttachTableVarConstraint(Schema.TableVar ATableVar, Schema.TableVarConstraint AConstraint)
		{
			ATableVar.Constraints.Add(AConstraint);
			if (AConstraint is Schema.RowConstraint)
				ATableVar.RowConstraints.Add(AConstraint);
			else
			{
				Schema.TransitionConstraint LTransitionConstraint = (Schema.TransitionConstraint)AConstraint;
				if (LTransitionConstraint.OnInsertNode != null)
					ATableVar.InsertConstraints.Add(LTransitionConstraint);
				if (LTransitionConstraint.OnUpdateNode != null)
					ATableVar.UpdateConstraints.Add(LTransitionConstraint);
				if (LTransitionConstraint.OnDeleteNode != null)
					ATableVar.DeleteConstraints.Add(LTransitionConstraint);
			}
		}

		private void DetachTableVarConstraint(Schema.TableVar ATableVar, Schema.TableVarConstraint AConstraint)
		{
			ATableVar.Constraints.SafeRemove(AConstraint);
			if (AConstraint is Schema.RowConstraint)
				ATableVar.RowConstraints.SafeRemove(AConstraint);
			else
			{
				Schema.TransitionConstraint LTransitionConstraint = (Schema.TransitionConstraint)AConstraint;
				if (LTransitionConstraint.OnInsertNode != null)
					ATableVar.InsertConstraints.SafeRemove(LTransitionConstraint);
				if (LTransitionConstraint.OnUpdateNode != null)
					ATableVar.UpdateConstraints.SafeRemove(LTransitionConstraint);
				if (LTransitionConstraint.OnDeleteNode != null)
					ATableVar.DeleteConstraints.SafeRemove(LTransitionConstraint);
			}
		}
		
		public void CreateTableVarConstraint(Schema.TableVar ATableVar, Schema.TableVarConstraint AConstraint)
		{
			AttachTableVarConstraint(ATableVar, AConstraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachTableVarConstraintInstruction(ATableVar, AConstraint));
			#endif
		}
		
		public void DropTableVarConstraint(Schema.TableVar ATableVar, Schema.TableVarConstraint AConstraint)
		{
			DetachTableVarConstraint(ATableVar, AConstraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachTableVarConstraintInstruction(ATableVar, AConstraint));
			#endif
		}
		
		#endregion
		
		#region TableVar column constraint
		
		private void AttachTableVarColumnConstraint(Schema.TableVarColumn ATableVarColumn, Schema.TableVarColumnConstraint AConstraint)
		{
			ATableVarColumn.Constraints.Add(AConstraint);
		}

		private void DetachTableVarColumnConstraint(Schema.TableVarColumn ATableVarColumn, Schema.TableVarColumnConstraint AConstraint)
		{
			ATableVarColumn.Constraints.SafeRemove(AConstraint);
		}
		
		public void CreateTableVarColumnConstraint(TableVarColumn AColumn, TableVarColumnConstraint AConstraint)
		{
			AttachTableVarColumnConstraint(AColumn, AConstraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachTableVarColumnConstraintInstruction(AColumn, AConstraint));
			#endif
		}

		public void DropTableVarColumnConstraint(TableVarColumn AColumn, TableVarColumnConstraint AConstraint)
		{
			DetachTableVarColumnConstraint(AColumn, AConstraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachTableVarColumnConstraintInstruction(AColumn, AConstraint));
			#endif
		}

		#endregion
		
		#region Scalar type
		
		private void AttachScalarTypeConstraint(Schema.ScalarType AScalarType, Schema.ScalarTypeConstraint AConstraint)
		{
			AScalarType.Constraints.Add(AConstraint);
		}

		private void DetachScalarTypeConstraint(Schema.ScalarType AScalarType, Schema.ScalarTypeConstraint AConstraint)
		{
			AScalarType.Constraints.SafeRemove(AConstraint);
		}
		
		public void CreateScalarTypeConstraint(ScalarType AScalarType, ScalarTypeConstraint AConstraint)
		{
			AttachScalarTypeConstraint(AScalarType, AConstraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachScalarTypeConstraintInstruction(AScalarType, AConstraint));
			#endif
		}

		public void DropScalarTypeConstraint(ScalarType AScalarType, ScalarTypeConstraint AConstraint)
		{
			DetachScalarTypeConstraint(AScalarType, AConstraint);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachScalarTypeConstraintInstruction(AScalarType, AConstraint));
			#endif
		}

		public void SetScalarTypeDefault(Schema.ScalarType AScalarType, Schema.ScalarTypeDefault ADefault)
		{
			Schema.ScalarTypeDefault LOriginalDefault = AScalarType.Default;
			AScalarType.Default = ADefault;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetScalarTypeDefaultInstruction(AScalarType, LOriginalDefault));
			#endif
		}

		public void SetScalarTypeIsSpecialOperator(Schema.ScalarType AScalarType, Schema.Operator AOperator)
		{
			Schema.Operator LOriginalOperator = AScalarType.IsSpecialOperator;
			AScalarType.IsSpecialOperator = AOperator;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetScalarTypeIsSpecialOperatorInstruction(AScalarType, LOriginalOperator));
			#endif
		}

		private void AttachSpecial(Schema.ScalarType AScalarType, Schema.Special ASpecial)
		{
			AScalarType.Specials.Add(ASpecial);
		}

		private void DetachSpecial(Schema.ScalarType AScalarType, Schema.Special ASpecial)
		{
			AScalarType.Specials.SafeRemove(ASpecial);
		}
		
		public void CreateSpecial(ScalarType AScalarType, Special ASpecial)
		{
			AttachSpecial(AScalarType, ASpecial);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachSpecialInstruction(AScalarType, ASpecial));
			#endif
		}

		public void DropSpecial(ScalarType AScalarType, Special ASpecial)
		{
			DetachSpecial(AScalarType, ASpecial);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachSpecialInstruction(AScalarType, ASpecial));
			#endif
		}

		private void AttachRepresentation(Schema.ScalarType AScalarType, Schema.Representation ARepresentation)
		{
			if (!AScalarType.Representations.Contains(ARepresentation))
				AScalarType.Representations.Add(ARepresentation);
		}

		private void DetachRepresentation(Schema.ScalarType AScalarType, Schema.Representation ARepresentation)
		{
			AScalarType.Representations.SafeRemove(ARepresentation);
		}
		
		public void CreateRepresentation(ScalarType AScalarType, Representation ARepresentation)
		{
			AttachRepresentation(AScalarType, ARepresentation);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachRepresentationInstruction(AScalarType, ARepresentation));
			#endif	
		}

		public void DropRepresentation(ScalarType AScalarType, Representation ARepresentation)
		{
			DetachRepresentation(AScalarType, ARepresentation);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachRepresentationInstruction(AScalarType, ARepresentation));
			#endif
		}

		private void AttachProperty(Schema.Representation ARepresentation, Schema.Property AProperty)
		{
			if (!ARepresentation.Properties.Contains(AProperty))
				ARepresentation.Properties.Add(AProperty);
		}

		private void DetachProperty(Schema.Representation ARepresentation, Schema.Property AProperty)
		{
			ARepresentation.Properties.SafeRemove(AProperty);
		}
		
		public void CreateProperty(Schema.Representation ARepresentation, Schema.Property AProperty)
		{
			AttachProperty(ARepresentation, AProperty);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachPropertyInstruction(ARepresentation, AProperty));
			#endif
		}

		public void DropProperty(Schema.Representation ARepresentation, Schema.Property AProperty)
		{
			DetachProperty(ARepresentation, AProperty);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachPropertyInstruction(ARepresentation, AProperty));
			#endif
		}

		#endregion
		
		#region Device
		
		public new CatalogDevice Device { get { return (CatalogDevice)base.Device; } }

		public void CreateDevice(Schema.Device ADevice)
		{
			InsertCatalogObject(ADevice);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
			{
				FInstructions.Add(new StartDeviceInstruction(ADevice));
			}
			#endif
		}
		
		public void StartDevice(Schema.Device ADevice)
		{
			ADevice.Start(ServerProcess);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new StartDeviceInstruction(ADevice));
			#endif
		}
		
		public void RegisterDevice(Schema.Device ADevice)
		{
			if (!ADevice.Registered)
			{
				ADevice.Register(ServerProcess);
				UpdateCatalogObject(ADevice);
				#if LOGDDLINSTRUCTIONS
				if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
					FInstructions.Add(new RegisterDeviceInstruction(ADevice));
				#endif
			}
		}
		
		public void UnregisterDevice(Schema.Device ADevice)
		{
			ADevice.ClearRegistered();
		}
		
		public void StopDevice(Schema.Device ADevice)
		{
			StopDevice(ADevice, false);
		}
		
		private List<Schema.Device> FDeferredDeviceStops;
		private void AddDeferredDeviceStop(Schema.Device ADevice)
		{
			if (FDeferredDeviceStops == null)
				FDeferredDeviceStops = new List<Schema.Device>();
			FDeferredDeviceStops.Add(ADevice);
		}
		
		private void ExecuteDeferredDeviceStops()
		{
			if (FDeferredDeviceStops != null)
			{
				while (FDeferredDeviceStops.Count > 0)
				{
					InternalStopDevice(FDeferredDeviceStops[0]);
					FDeferredDeviceStops.RemoveAt(0);
				}
				
				FDeferredDeviceStops = null;
			}
		}
		
		private void ClearDeferredDeviceStops()
		{
			if (FDeferredDeviceStops != null)
				FDeferredDeviceStops = null;
		}
		
		private void InternalStopDevice(Schema.Device ADevice)
		{
			if (ADevice.Running)
			{
				if (ADevice.Sessions.Count > 0)
					for (int LIndex = ADevice.Sessions.Count - 1; LIndex >= 0; LIndex--)
						ADevice.Sessions.Dispose();
				// TODO: implement checking and error handling for in use device sessions on this device
				//throw new RuntimeException(RuntimeException.Codes.DeviceInUse, ADevice.Name);

				ADevice.Stop(ServerProcess);					
			}
		}
		
		private void StopDevice(Schema.Device ADevice, bool AIsUndo)
		{
			if ((ServerProcess.InTransaction) && !AIsUndo)
				AddDeferredDeviceStop(ADevice);
			else
				InternalStopDevice(ADevice);
		}
		
		public void DropDevice(Schema.Device ADevice)
		{
			DeleteCatalogObject(ADevice);
		}

		public void SetDeviceReconcileMode(Schema.Device ADevice, ReconcileMode AReconcileMode)
		{
			ReconcileMode LOriginalReconcileMode = ADevice.ReconcileMode;
			ADevice.ReconcileMode = AReconcileMode;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetDeviceReconcileModeInstruction(ADevice, LOriginalReconcileMode));
			#endif
		}

		public void SetDeviceReconcileMaster(Schema.Device ADevice, ReconcileMaster AReconcileMaster)
		{
			ReconcileMaster LOriginalReconcileMaster = ADevice.ReconcileMaster;
			ADevice.ReconcileMaster = AReconcileMaster;
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new SetDeviceReconcileMasterInstruction(ADevice, LOriginalReconcileMaster));
			#endif
		}
		
		#endregion
		
		#region Device user
		
		public virtual Schema.DeviceUser ResolveDeviceUser(Schema.Device ADevice, Schema.User AUser, bool AMustResolve)
		{
			lock (ADevice.Users)
			{
				Schema.DeviceUser LDeviceUser;
				if (!ADevice.Users.TryGetValue(AUser.ID, out LDeviceUser) && AMustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DeviceUserNotFound, AUser.ID);

				return LDeviceUser;
			}
		}

		public Schema.DeviceUser ResolveDeviceUser(Schema.Device ADevice, Schema.User AUser)
		{
			return ResolveDeviceUser(ADevice, AUser, true);
		}

		public bool DeviceUserExists(Schema.Device ADevice, Schema.User AUser)
		{
			return ResolveDeviceUser(ADevice, AUser, false) != null;
		}

		#endregion
		
		#region Device scalar type
		
		private void AttachDeviceScalarType(Schema.DeviceScalarType ADeviceScalarType)
		{
			ADeviceScalarType.Device.AddDeviceScalarType(ADeviceScalarType);
		}
		
		private void DetachDeviceScalarType(Schema.DeviceScalarType ADeviceScalarType)
		{
			ADeviceScalarType.Device.RemoveDeviceScalarType(ADeviceScalarType);
		}

		public void CreateDeviceScalarType(DeviceScalarType ADeviceScalarType)
		{
			InsertCatalogObject(ADeviceScalarType);
			
			AttachDeviceScalarType(ADeviceScalarType);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachDeviceScalarTypeInstruction(ADeviceScalarType));
			#endif
		}

		public void DropDeviceScalarType(DeviceScalarType ADeviceScalarType)
		{
			DeleteCatalogObject(ADeviceScalarType);
			
			DetachDeviceScalarType(ADeviceScalarType);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachDeviceScalarTypeInstruction(ADeviceScalarType));
			#endif
		}

		#endregion
		
		#region Device table
		
		private void CreateDeviceTable(Schema.BaseTableVar ATable)
		{
			Program LProgram = new Program(ServerProcess);
			LProgram.Start(null);
			try
			{
				LProgram.DeviceExecute(ATable.Device, new CreateTableNode(ATable));
			}
			finally
			{
				LProgram.Stop(null);
			}
		}
		
		private void DropDeviceTable(Schema.BaseTableVar ATable)
		{
			Program LProgram = new Program(ServerProcess);
			LProgram.Start(null);
			try
			{
				LProgram.DeviceExecute(ATable.Device, new DropTableNode(ATable));
			}
			finally
			{
				LProgram.Stop(null);
			}
		}
		
		private void AttachTableMap(ApplicationTransactionDevice ADevice, TableMap ATableMap)
		{
			ADevice.TableMaps.Add(ATableMap);
		}
		
		private void DetachTableMap(ApplicationTransactionDevice ADevice, TableMap ATableMap)
		{
			ADevice.TableMaps.RemoveAt(ADevice.TableMaps.IndexOfName(ATableMap.SourceTableVar.Name));
		}
		
		public void AddTableMap(ApplicationTransactionDevice ADevice, TableMap ATableMap)
		{
			AttachTableMap(ADevice, ATableMap);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachTableMapInstruction(ADevice, ATableMap));
			#endif
		}
		
		public void RemoveTableMap(ApplicationTransactionDevice ADevice, TableMap ATableMap)
		{
			DetachTableMap(ADevice, ATableMap);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachTableMapInstruction(ADevice, ATableMap));
			#endif
		}
		
		#endregion
		
		#region Device operator
		
		private void AttachDeviceOperator(Schema.DeviceOperator ADeviceOperator)
		{
			ADeviceOperator.Device.AddDeviceOperator(ADeviceOperator);
		}
		
		private void DetachDeviceOperator(Schema.DeviceOperator ADeviceOperator)
		{
			ADeviceOperator.Device.RemoveDeviceOperator(ADeviceOperator);
		}

		public void CreateDeviceOperator(DeviceOperator ADeviceOperator)
		{
			InsertCatalogObject(ADeviceOperator);
			
			AttachDeviceOperator(ADeviceOperator);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachDeviceOperatorInstruction(ADeviceOperator));
			#endif
		}

		public void DropDeviceOperator(DeviceOperator ADeviceOperator)
		{
			DeleteCatalogObject(ADeviceOperator);
			
			DetachDeviceOperator(ADeviceOperator);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachDeviceOperatorInstruction(ADeviceOperator));
			#endif
		}
		
		private void AttachOperatorMap(ApplicationTransaction.OperatorMap AOperatorMap, Schema.Operator AOperator)
		{
			AOperatorMap.Operators.Add(AOperator);
		}
		
		private void DetachOperatorMap(ApplicationTransaction.OperatorMap AOperatorMap, Schema.Operator AOperator)
		{
			AOperatorMap.Operators.Remove(AOperator);
		}
		
		public void AddOperatorMap(ApplicationTransaction.OperatorMap AOperatorMap, Schema.Operator AOperator)
		{
			AttachOperatorMap(AOperatorMap, AOperator);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new AttachOperatorMapInstruction(AOperatorMap, AOperator));
			#endif
		}
		
		public void RemoveOperatorMap(ApplicationTransaction.OperatorMap AOperatorMap, Schema.Operator AOperator)
		{
			DetachOperatorMap(AOperatorMap, AOperator);
			#if LOGDDLINSTRUCTIONS
			if ((!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction)
				FInstructions.Add(new DetachOperatorMapInstruction(AOperatorMap, AOperator));
			#endif
		}
		
		#endregion
		
		#region Device objects

		public virtual bool HasDeviceObjects(Schema.Device ADevice)
		{
			return false;
		}

		public virtual Schema.DeviceObject ResolveDeviceObject(Schema.Device ADevice, Schema.Object AObject)
		{
			return null;
		}

		public Schema.DeviceOperator ResolveDeviceOperator(Schema.Device ADevice, Schema.Operator AOperator)
		{
			return ResolveDeviceObject(ADevice, AOperator) as Schema.DeviceOperator;
		}

		public Schema.DeviceScalarType ResolveDeviceScalarType(Schema.Device ADevice, Schema.ScalarType AScalarType)
		{
			return ResolveDeviceObject(ADevice, AScalarType) as Schema.DeviceScalarType;
		}

		#endregion
		
		#region Security
		
		public virtual Right ResolveRight(string ARightName, bool AMustResolve)
		{
			if (AMustResolve)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.RightNotFound, ARightName);
			return null;
		}

		public Right ResolveRight(string ARightName)
		{
			return ResolveRight(ARightName, true);
		}

		public bool RightExists(string ARightName)
		{
			return ResolveRight(ARightName, false) != null;
		}

		public virtual void InsertRight(string ARightName, string AUserID)
		{
			// virtual
		}

		public virtual void DeleteRight(string ARightName)
		{
			lock (Catalog)
			{
				// TODO: Look at speeding this up with an index of users for each right? Memory usage may outweigh the benefits of this index...
				foreach (Schema.User LUser in Device.UsersCache.Values)
					LUser.ClearCachedRightAssignment(ARightName);
			}
		}

		public virtual bool UserHasRight(string AUserID, string ARightName)
		{
			return true;
		}

		public void CheckUserHasRight(string AUserID, string ARightName)
		{
			if (!UserHasRight(AUserID, ARightName))
				throw new ServerException(ServerException.Codes.UnauthorizedRight, ErrorSeverity.Environment, AUserID, ARightName);
		}

		protected void ClearUserCachedRightAssignments(string AUserID)
		{
			Schema.User LUser;
			if (Device.UsersCache.TryGetValue(AUserID, out LUser))
				LUser.ClearCachedRightAssignments();
		}

		/// <summary>Adds the given user to the cache, without affecting the underlying store.</summary>
		public void CacheUser(User AUser)
		{
			lock (Catalog)
			{
				InternalCacheUser(AUser);
			}
		}

		protected void InternalCacheUser(User AUser)
		{
			Device.UsersCache.Add(AUser);
		}

		/// <summary>Removes the given user from the cache, without affecting the underlying store.</summary>		
		public void ClearUser(string AUserID)
		{
			lock (Catalog)
			{
				Device.UsersCache.Remove(AUserID);
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

		public virtual void InsertUser(Schema.User AUser)
		{
			CacheUser(AUser);

			#if LOGDDLINSTRUCTIONS
			if (ServerProcess.InTransaction)
				FInstructions.Add(new CreateUserInstruction(AUser));
			#endif
		}

		public Schema.User ResolveUser(string AUserID, bool AMustResolve)
		{
			lock (Catalog)
			{
				Schema.User LUser;
				if (!Device.UsersCache.TryGetValue(AUserID, out LUser))
				{
					LUser = InternalResolveUser(AUserID, LUser);
				}

				if ((LUser == null) && AMustResolve)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.UserNotFound, AUserID);

				return LUser;
			}
		}

		protected virtual Schema.User InternalResolveUser(string AUserID, Schema.User LUser)
		{
			return null;
		}

		public Schema.User ResolveUser(string AUserID)
		{
			return ResolveUser(AUserID, true);
		}

		protected class CreateUserInstruction : DDLInstruction
		{
			public CreateUserInstruction(Schema.User AUser) : base()
			{
				FUser = AUser;
			}
			
			private Schema.User FUser;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.ClearUser(FUser.ID);
			}
		}
		
		protected class DropUserInstruction : DDLInstruction
		{
			public DropUserInstruction(Schema.User AUser) : base()
			{
				FUser = AUser;
			}
			
			private Schema.User FUser;
			
			public override void Undo(CatalogDeviceSession ASession)
			{
				ASession.CacheUser(FUser);
			}
		}
		
		public void InsertRole(Schema.Role ARole)
		{
			// Add the role to the Cache
			CacheCatalogObject(ARole);

			// Clear the name cache (this is done in InsertPersistentObject for all other catalog objects)
			Device.NameCache.Clear();

			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// log the DDL instruction
				if (ServerProcess.InTransaction)
					FInstructions.Add(new CreateCatalogObjectInstruction(ARole));
				#endif

				InternalInsertRole(ARole);
			}
		}

		protected virtual void InternalInsertRole(Schema.Role ARole)
		{
			// virtual
		}

		public void DeleteRole(Schema.Role ARole)
		{
			lock (Catalog)
			{
				// Remove the object from the catalog cache
				ClearCatalogObject(ARole);
			}

			if (!ServerProcess.InLoadingContext())
			{
				#if LOGDDLINSTRUCTIONS
				// log the DDL instruction
				if (ServerProcess.InTransaction)
					FInstructions.Add(new DropCatalogObjectInstruction(ARole));
				#endif

				// If this is not a repository, remove it from the catalog store
				InternalDeleteRole(ARole);
			}
		}

		protected virtual void InternalDeleteRole(Schema.Role ARole)
		{
			// virtual
		}

		#endregion
	}
}
