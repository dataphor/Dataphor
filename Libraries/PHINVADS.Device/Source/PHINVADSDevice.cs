/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Device;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.PHINVADS.Core;
using hessiancsharp.client;
using VadsClient;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Aphora.Dataphor.PHINVADS.Device
{
	public class PHINVADSDevice : Schema.Device
	{
		public PHINVADSDevice(int iD, string name) : base(iD, name) 
		{ 
			_supportsTransactions = true;
			_supportsNestedTransactions = true;
		}

		public override Schema.DeviceCapability Capabilities
		{
			get
			{
				return 
					Schema.DeviceCapability.RowLevelInsert 
						| Schema.DeviceCapability.RowLevelUpdate 
						| Schema.DeviceCapability.RowLevelDelete;
			}
		}

		private string _endpoint;
		public string Endpoint
		{
			get { return _endpoint; }
			set { _endpoint = value; }
		}

		protected override Schema.DeviceSession InternalConnect(ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo)
		{
			return new PHINVADSDeviceSession(this, serverProcess, deviceSessionInfo);
		}

		protected override DevicePlanNode InternalPrepare(Schema.DevicePlan plan, PlanNode planNode)
		{
			// return a DevicePlanNode appropriate for execution of the given node
			TableNode tableNode = planNode as TableNode;
			if (tableNode != null)
			{
				var fhirTableNode = new PHINVADSDeviceTableNode(tableNode);
				fhirTableNode.Prepare(plan);
				if (plan.IsSupported)
				{
					return fhirTableNode;
				}

				return null;
			}
			
			CreateTableNode createTableNode = planNode as CreateTableNode;
			if (createTableNode != null)
			{
				var fhirCreateTableNode = new PHINVADSCreateTableNode(createTableNode);
				return fhirCreateTableNode;
			}

			DropTableNode dropTableNode = planNode as DropTableNode;
			if (dropTableNode != null)
			{
				var fhirDropTableNode = new PHINVADSDropTableNode(dropTableNode);
				return fhirDropTableNode;
			}

			return null;
		}

		private string ToPHINVADSTableName(string fullTypeName)
		{
			return Schema.Object.Unqualify(fullTypeName) + "s"; // Pluralize the name of the resource to produce the table name
		}

		public override Schema.Catalog GetDeviceCatalog(ServerProcess process, Schema.Catalog serverCatalog, Schema.TableVar tableVar)
		{
			Schema.Catalog catalog = base.GetDeviceCatalog(process, serverCatalog, tableVar);

			using (Plan plan = new Plan(process))
			{

				// Need to support reverse lookup to determine the scalar type for a given native type

				Type[] types = typeof(Authority).Assembly.GetTypes();

				foreach (Type type in types)
				{
					// create a table var for each class
					if (type.IsClass && type.GetField("id") != null)
						if (!type.IsGenericTypeDefinition)
						{
							string tableName = Schema.Object.Qualify(ToPHINVADSTableName(type.Name), plan.CurrentLibrary.Name);
							if (tableVar == null || Schema.Object.NamesEqual(tableName, tableVar.Name))
							{
								Schema.BaseTableVar localTableVar = new Schema.BaseTableVar(tableName, null);
								localTableVar.Owner = plan.User;
								localTableVar.Library = plan.CurrentLibrary;
								localTableVar.Device = this;
								localTableVar.MetaData = new MetaData();
				
								// with a FHIR.ResourceType tag
								localTableVar.MetaData.Tags.Add(new Tag("PHINVADS.ResourceType", type.Name));
								localTableVar.AddDependency(this);
								localTableVar.DataType = new Schema.TableType();

								var d4TypeName = Schema.Object.Qualify(GenerateTypesNode.GetD4TypeName(type.FullName), "PHINVADS.Core");
								var d4Type = Compiler.ResolveCatalogIdentifier(plan, d4TypeName, false) as Schema.ScalarType;
								if (d4Type != null)
								{
									AddColumnsForType(localTableVar, d4Type);
								
									localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["id"] })); 

									switch (type.Name)
									{
										case "CodeSystem": 
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["oid"] })); 
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["codeSystemCode"] })); 
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["hl70396Identifier"] })); 
										break;
										case "CodeSystemConcept": localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["codeSystemOid"], localTableVar.Columns["conceptCode"] })); break;
										case "ValueSet": 
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["oid"] })); 
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["code"] })); 
										break;

										case "ValueSetConcept":
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["codeSystemOid"], localTableVar.Columns["conceptCode"] }));
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["valueSetVersionId"], localTableVar.Columns["conceptCode"] }));
										break;

										case "ValueSetVersion":
											localTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { localTableVar.Columns["valueSetOid"], localTableVar.Columns["versionNumber"] }));
										break;
									}

									catalog.Add(localTableVar);
								}
							}
						}
				}

				return catalog;
			}
		}

		private void AddColumnsForType(Schema.TableVar tableVar, Schema.ScalarType d4Type)
		{
			// Add columns for the base type, if any
			foreach (var parentType in d4Type.ParentTypes)
				AddColumnsForType(tableVar, parentType);

			// Add columns for the default representation
			var representation = Compiler.FindDefaultRepresentation(d4Type);
			if (representation != null)
			{
				// And columns for each property of the default representation
				foreach (var property in representation.Properties)
				{
					var column = new Schema.Column(property.Name, property.DataType);
					var tableVarColumn = new Schema.TableVarColumn(column);
					tableVar.DataType.Columns.Add(column);
					tableVar.Columns.Add(tableVarColumn);
				}
			}

			tableVar.AddDependency(d4Type);
		}

		public static Type GetSearchParamTypeForResource(Plan plan, string resourceType)
		{
			var searchParamsClassName = resourceType + "SearchCriteriaDto";
			return plan.Catalog.ClassLoader.CreateType(plan.CatalogDeviceSession, new ClassDefinition(Schema.Object.Qualify(searchParamsClassName, "VadsClient")));
		}

		public static object GetSearchParamInstanceForResource(Plan plan, string resourceType)
		{
			var searchParamType = GetSearchParamTypeForResource(plan, resourceType);
			if (searchParamType != null)
				return Activator.CreateInstance(searchParamType);
			return null;
		}

		public static Type GetResultTypeForResource(string resourceType)
		{
			var resultTypeClassName = resourceType + "ResultDto";
			return Type.GetType(resultTypeClassName);
		}

		public static string GetSearchParamIndicatorName(string resourceType, string columnName)
		{
			switch (resourceType)
			{
				case "ValueSetVersion":
					switch (columnName)
					{
						case "valueSetOid": return "oidSearch";
						default: return columnName + "Search";
					}

				case "ValueSetConcept":
					switch (columnName)
					{
						case "valueSetOid": return "valueSetOids";
						default: return columnName + "Search";
					}

				default: return columnName + "Search";
			}
		}
	}

	public class PHINVADSDevicePlanNode : DevicePlanNode
	{
		public PHINVADSDevicePlanNode(PlanNode node) : base(node) { }
	}

	public class PHINVADSDropTableNode : PHINVADSDevicePlanNode
	{
		public PHINVADSDropTableNode(DropTableNode node) : base(node) { }

		public new DropTableNode Node { get { return (DropTableNode)base.Node; } }
	}

	public class PHINVADSCreateTableNode : PHINVADSDevicePlanNode
	{
		public PHINVADSCreateTableNode(CreateTableNode node) : base(node) { }

		public new CreateTableNode Node { get { return (CreateTableNode)base.Node; } }
	}

	public class PHINVADSDeviceTableNode : PHINVADSDevicePlanNode
	{
		public PHINVADSDeviceTableNode(TableNode node) : base(node) { }

		public new TableNode Node { get { return (TableNode)base.Node; } }

		private string _resourceType;
		public string ResourceType 
		{ 
			get { return _resourceType; } 
			set { _resourceType = value; }
		}

		public void SetSearchParamContainer(Schema.DevicePlan plan)
		{
			_searchParams = PHINVADSDevice.GetSearchParamInstanceForResource(plan.Plan, _resourceType);
			_directParams = new Dictionary<string, object>();
		}

		private object _searchParams;
		public object SearchParams { get { return _searchParams; } }

		private Dictionary<string, object> _directParams;

		public void Prepare(Schema.DevicePlan plan)
		{
			InternalPrepare(plan, Node);
		}

		private bool IsSearchParamColumn(Schema.TableVarColumn column)
		{
			return _searchParams != null && _searchParams.GetType().GetMember(PHINVADSDevice.GetSearchParamIndicatorName(_resourceType, column.Name)) != null;
		}

		private void InternalPrepare(Schema.DevicePlan plan, TableNode planNode)
		{
			RestrictNode restrictNode = planNode as RestrictNode;
			if (restrictNode != null)
			{
				// Prepare the source
				InternalPrepare(plan, restrictNode.SourceNode);

				if (plan.IsSupported)
				{
					SetSearchParamContainer(plan);
					if (restrictNode.IsSeekable)
					{
						foreach (ColumnConditions columnConditions in restrictNode.Conditions)
						{
							if (!IsSearchParamColumn(columnConditions.Column))
							{
								plan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format("Service does not support restriction by {0}.", columnConditions.Column.Name)));
								plan.IsSupported = false;
								break;
							}
						}
					}
					else if (restrictNode.IsScanable)
					{
						foreach (ColumnConditions columnConditions in restrictNode.Conditions)
						{
							if (!IsSearchParamColumn(columnConditions.Column))
							{
								plan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format("Service does not support restriction by {0}.", columnConditions.Column.Name)));
								plan.IsSupported = false;
								break;
							}

							foreach (ColumnCondition condition in columnConditions)
							{
								if (condition.Instruction != Instructions.Equal)
								{
									plan.IsSupported = false;
									break;
								}
							}

							if (!plan.IsSupported)
								break;
						}
					}
					else if (restrictNode.Nodes[1] is SatisfiesSearchParamNode)
					{
						plan.IsSupported = true;
					}
					else
					{
						plan.TranslationMessages.Add(new Schema.TranslationMessage("Service does not support arbitrary restriction."));
						plan.IsSupported = false;
					}
				}

				return;
			}

			BaseTableVarNode baseTableVarNode = planNode as BaseTableVarNode;
			if (baseTableVarNode != null)
			{
				ResourceType = MetaData.GetTag(baseTableVarNode.TableVar.MetaData, "PHINVADS.ResourceType", Schema.Object.Unqualify(baseTableVarNode.TableVar.Name));
				return;
			}

			plan.TranslationMessages.Add(new Schema.TranslationMessage("Service does not support arbitrary queries."));
			plan.IsSupported = false;
			return;
		}
	}

	public class PHINVADSDeviceSession : Schema.DeviceSession
	{
		public PHINVADSDeviceSession(PHINVADSDevice device, ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo)
		{
			// Initialize the PHINVADS Client
			_factory = new CHessianProxyFactory();
			_client = (VocabService)_factory.Create(typeof(VocabService), device.Endpoint);

			// TODO: Establish authentication/authorization tokens?
		}

		private CHessianProxyFactory _factory;

		private VocabService _client;
		public VocabService Client { get { return _client; } }

		protected override bool IsConnectionFailure(Exception exception)
		{
			return base.IsConnectionFailure(exception);
		}

		protected override bool IsTransactionFailure(Exception exception)
		{
			return base.IsTransactionFailure(exception);
		}

		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			if (planNode.DeviceNode == null)
			{
				throw new DeviceException(DeviceException.Codes.UnpreparedDevicePlan, ErrorSeverity.System);
			}

			var fhirTableNode = planNode.DeviceNode as PHINVADSDeviceTableNode;
			if (fhirTableNode != null)
			{
				var fhirTable = new PHINVADSTable(this, program, fhirTableNode);
				fhirTable.Open();
				return fhirTable;
			}

			var fhirCreateTableNode = planNode.DeviceNode as PHINVADSCreateTableNode;
			if (fhirCreateTableNode != null)
			{
				return null; // TODO: Throw an error if reconilication is not none...
			}

			var fhirDropTableNode = planNode.DeviceNode as PHINVADSDropTableNode;
			if (fhirDropTableNode != null)
			{
				return null; // TODO: Throw an error if reconciliation is not none...
			}

			return base.InternalExecute(program, planNode);
		}
		
		protected override void InternalInsertRow(Program program, Schema.TableVar table, IRow row, BitArray valueFlags)
		{
			base.InternalInsertRow(program, table, row, valueFlags);
			// TODO: Log the operation so it can be performed during the commit
			// NOTE: This will require that the operation log be considered a "cache" that will be read locally prior to returning a result (or as part of post-processing a result?)
			// Can use a PUT if we manage Id generation, assuming the server supports it
			// Must use a POST to allow the server to assign the Id, in that case the Location header of the response will contain the newly assigned Id
		}

		protected override void InternalUpdateRow(Program program, Schema.TableVar table, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			// TODO: Log the operation so it can be performed during the commit
			base.InternalUpdateRow(program, table, oldRow, newRow, valueFlags);
			// PUT, w/ ETag header equal to oldRow.meta.version
		}

		protected override void InternalDeleteRow(Program program, Schema.TableVar table, IRow row)
		{
			// TODO: Log the operation so it can be performed during the commit
			base.InternalDeleteRow(program, table, row);
			// DELETE, w/ ETag header equal to row.meta.version
		}

		protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
		{
			// Do nothing, the transaction is managed as a single request/response to the server
		}

		protected override void InternalPrepareTransaction()
		{
			// If this is the root transaction, invoke the operations for the current transaction against the server
		}

		protected override void InternalCommitTransaction()
		{
			// If no errors occurred during the prepare, the commit is a no-op.
		}

		protected override void InternalRollbackTransaction()
		{
			// Do nothing, the prepare is what actually performs the operations
		}
	}

	public class PHINVADSTable : Table
	{
		const int CMinPageNumber = 1;

		public PHINVADSTable(PHINVADSDeviceSession deviceSession, Program program, PHINVADSDeviceTableNode fhirTableNode) : base(fhirTableNode.Node, program)
		{
			_deviceSession = deviceSession;
			_fhirTableNode = fhirTableNode;
		}

		private PHINVADSDeviceSession _deviceSession;
		private PHINVADSDeviceTableNode _fhirTableNode;
		private object _bundle;
		private List<object> _resources;
		private Type _resourceType;
		private int _pageSize = 20;
		private int _currentIndex;
		private int _currentPage;
		private int _totalResults;
		private object _searchParams;
		private Dictionary<string, object> _directParams;
		private enum APIType { GetAll, Find, Direct };
		private APIType _apiType;
		// private string _idValue;

		private void InitializeResourceType()
		{
			_resourceType = _program.Catalog.ClassLoader.CreateType(_program.CatalogDeviceSession, new ClassDefinition(String.Format("VadsClient.{0}", _fhirTableNode.ResourceType)));
		}

		private string GetSearchParamName(Schema.TableVarColumn column)
		{
			return GetSearchParamName(column.Name);
		}

		private string GetSearchParamName(string columnName)
		{
			return PHINVADSDevice.GetSearchParamIndicatorName(_fhirTableNode.ResourceType, columnName);
		}

		private string GetSearchParamValue(ColumnCondition columnCondition)
		{
			var result = columnCondition.Argument.Execute(_program);
			if (result != null)
				return result.ToString();
			return null;
		}

		private void SetSearchParam(string paramName, object paramValue)
		{
			if (_apiType == APIType.Direct)
			{
				if (!_directParams.ContainsKey(paramName))
					_directParams.Add(paramName, paramValue);
				else
					_directParams[paramName] = paramValue;
			}
			else
			{
				_searchParams.GetType().GetField(paramName).SetValue(_searchParams, true);
				_searchParams.GetType().GetField("searchText").SetValue(_searchParams, Convert.ToString(paramValue));
				_searchParams.GetType().GetField("searchType").SetValue(_searchParams, 1); // Exact Match

				switch (_fhirTableNode.ResourceType)
				{
					case "ValueSetConcept":
					case "ValueSetVersion":
					case "ViewVersion":
						_searchParams.GetType().GetField("versionOption").SetValue(_searchParams, 3); // Latest
					break;
				}
			}
		}

		private bool IsDirectSearch(string columnName)
		{
			if (columnName == "id")
				return true;

			switch (_fhirTableNode.ResourceType)
			{
				case "ValueSetConcept": return columnName == "valueSetVersionId";
				default: return false;
			}

			// getViewById(viewId)
			// getViewByName(viewName)
			// getViewVersionById(viewVersionId)
			// getViewVersionByViewNameAndVersionNumber(viewName, versionNumber)
			// getGroupById(groupId)
			// getGroupByName(name)
			// getValueSetVersionById(valueSetVersionId)
			// getValueSetVersionByValueSetOidAndVersionNumber(valueSetOid, versionNumber)
			// getCodeSystemConceptByOidAndCode(codeSystemOid, conceptCode)
			// getValueSetConceptById(valueSetConceptId)
			// getCodeSystemByOid(codeSystemOid)
			// getAuthorityById(authorityId)
			// getSourceById(sourceId)
			// getValueSetByOid(valueSetOid)

			// getGroupsByValueSetOid(valueSetOid)
			// getGroupIdsByValueSetOid(valueSetOid)
			// getViewVersionsByValueSetVersionId(valueSetVersionId)
			// getViewVersionIdsByValueSetVersionId(valueSetVersionId)
			// getValueSetVersionsByValueSetOid(valueSetOid)
			// getValueSetVersionIdsByValueSetOid(valueSetOid)
			// getValueSetConceptsByValueSetVersionId(valueSetVersionId, pageNumber, pageSize)
			// getCodeSystemConceptAltDesignationByOidAndCode(codeSystemOid, conceptCode)
			// getChildCodeSystemConceptsByRelationshipType(codeSystemOid, conceptCode, relationshipType, pageNumber, pageSize)
			// getParentCodeSystemConceptsByRelationshipType(codeSystemOid, conceptCode, relationshipType)
			// getCodeSystemPropertyDefinitionsByCodeSystemOid(codeSystemOid)
			// getValueSetVersionsByCodeSystemConceptOidAndCode(codeSystemOid, conceptCode)
			// getValueSetVersionIdsByCodeSystemConceptOidAndCode(codeSystemOid, conceptCode)
			// getCodeSystemConceptPertyValuesByOidAndCode(codeSystemOid, conceptCode)
			// getCodeSystemConceptsByCodeSystemOid(codeSystemOid)
			// getValueSetsByGroupId(groupId)
			// getValueSetsByGroupName(name)
			// getValueSetOidsByGroupId(groupId)
			// getValueSetVersionsByViewVersionId(viewVersionId)
		}

		private void InitializeSearchParams()
		{
			_searchParams = PHINVADSDevice.GetSearchParamInstanceForResource(_program.Plan, _fhirTableNode.ResourceType);
			_directParams = new Dictionary<String, Object>();

			// Ignoring the search params on the plan for now, create search params for this execution here
			var restrictNode = _fhirTableNode.Node as RestrictNode;
			if (restrictNode != null)
			{
				_apiType = APIType.Find;
				// TODO: Generalize the stack offset here, this is safe because we know the only scenario we support is restriction of a base table var
				_program.Stack.Push(null);
				try
				{
					if (restrictNode.IsSeekable)
					{
						// Verify that there is only one condition and that it corresponds to a known search parameter for this resource
						foreach (ColumnConditions columnConditions in restrictNode.Conditions)
						{
							// A seekable condition will only have one column condition
							var searchParamName = GetSearchParamName(columnConditions.Column);
							var searchValue = GetSearchParamValue(columnConditions[0]);
							if (IsDirectSearch(columnConditions.Column.Name))
								_apiType = APIType.Direct;
							SetSearchParam(searchParamName, searchValue);
						}
					}
					else if (restrictNode.IsScanable)
					{
						foreach (ColumnConditions columnConditions in restrictNode.Conditions)
						{
							// A scanable condition will always have equality as its instruction
							foreach (ColumnCondition condition in columnConditions)
							{
								var searchParamName = GetSearchParamName(columnConditions.Column);
								var searchValue = GetSearchParamValue(condition);
								if (IsDirectSearch(columnConditions.Column.Name))
									_apiType = APIType.Direct;
								SetSearchParam(searchParamName, searchValue);
							}
						}
					}
					else
					{
						var searchParamNode = restrictNode.Nodes[1] as SatisfiesSearchParamNode;
						if (searchParamNode != null)
						{
							var columnName = (string)searchParamNode.Nodes[1].Execute(_program);
							var searchParamName = GetSearchParamName(columnName);
							var searchValue = (string)searchParamNode.Nodes[2].Execute(_program);
							if (IsDirectSearch(columnName))
								_apiType = APIType.Direct;
							SetSearchParam(searchParamName, searchValue);
						}
					}
				}
				finally
				{
					_program.Stack.Pop();
				}
			}
		}

		private IEnumerable<object> GetResults(object results)
		{
			switch (_fhirTableNode.ResourceType)
			{
				case "Authority": return ((AuthorityResultDto)results).authorities as IEnumerable<object>;
				case "CodeSystemConceptAltDesignation": return ((CodeSystemConceptAltDesignationResultDto)results).codeSystemConceptAltDesignations as IEnumerable<object>;
				case "CodeSystemConceptPropertyValue": return ((CodeSystemConceptPropertyValueResultDto)results).codeSystemConceptPropertyValues as IEnumerable<object>;
				case "CodeSystemConcept": return ((CodeSystemConceptResultDto)results).codeSystemConcepts as IEnumerable<object>;
				case "CodeSystemPropertyDefinition": return ((CodeSystemPropertyDefinitionResultDto)results).codeSystemPropertyDefinitions as IEnumerable<object>;
				case "CodeSystem": return ((CodeSystemResultDto)results).codeSystems as IEnumerable<object>;
				//case "Custom": return results.GetType().GetField("results").GetValue(results) as List<object>;
				case "Group": return ((GroupResultDto)results).groups as IEnumerable<object>;
				case "Id": return ((IdResultDto)results).ids as IEnumerable<object>;
				case "Source": return ((SourceResultDto)results).sources as IEnumerable<object>;
				case "ValueSetConcept": return ((ValueSetConceptResultDto)results).valueSetConcepts as IEnumerable<object>;
				case "ValueSet": return ((ValueSetResultDto)results).valueSet as IEnumerable<object>;
				case "ValueSetVersion": return ((ValueSetVersionResultDto)results).valueSetVersions as IEnumerable<object>;
				case "View": return ((ViewResultDto)results).views as IEnumerable<object>;
				case "ViewVersion": return ((ViewVersionResultDto)results).viewVersions as IEnumerable<object>;
				default: throw new InvalidOperationException(String.Format("Could not retrieve results for resource type {0}.", _fhirTableNode.ResourceType));
			}
		}

		private object GetResult()
		{
			switch (_fhirTableNode.ResourceType)
			{
				case "Authority": return _deviceSession.Client.getAllAuthorities();
				//case "CodeSystemConceptAltDesignation": return _deviceSession.Client.getAllCodeSystemConceptAltDesignations();
				//case "CodeSystemConceptPropertyValue": return _deviceSession.Client.getAllCodeSystemConceptPropertyValues();
				//case "CodeSystemConcept": return _deviceSession.Client.getAllCodeSystemConcepts();
				case "CodeSystemPropertyDefinition": return _deviceSession.Client.getAllCodeSystemPropertyDefinitions();
				case "CodeSystem": return _deviceSession.Client.getAllCodeSystems();
				//case "Custom": return results.GetType().GetField("results").GetValue(results) as List<object>;
				case "Group": return _deviceSession.Client.getAllGroups();
				//case "Id": return results.GetType().GetField("ids").GetValue(results) as List<object>;
				case "Source": return _deviceSession.Client.getAllSources();
				//case "ValueSetConcept": return _deviceSession.Client.getAllValueSetConcepts();
				case "ValueSet": return _deviceSession.Client.getAllValueSets();
				case "ValueSetVersion": return _deviceSession.Client.getAllValueSetVersions();
				case "View": return _deviceSession.Client.getAllViews();
				case "ViewVersion": return _deviceSession.Client.getAllViewVersions();
				default: throw new InvalidOperationException(String.Format("Could not retrieve result for resource type {0}.", _fhirTableNode.ResourceType));
			}
		}

		private object GetDirect(int pageNumber)
		{
			switch (_fhirTableNode.ResourceType)
			{
				case "Authority": return _deviceSession.Client.getAuthorityById((string)_directParams["idSearch"]);
				case "CodeSystemConceptAltDesignation": return _deviceSession.Client.getCodeSystemConceptAltDesignationByOidAndCode((string)_directParams["codeSystemOidSearch"], (string)_directParams["conceptCodeSearch"]);
				//case "CodeSystemConceptPropertyValue": return _deviceSession.Client.getAllCodeSystemConceptPropertyValues();
				//case "CodeSystemConcept": return _deviceSession.Client.getAllCodeSystemConcepts();
				//case "CodeSystemPropertyDefinition": return _deviceSession.Client.getAllCodeSystemPropertyDefinitions();
				//case "CodeSystem": return _deviceSession.Client.getAllCodeSystems();
				//case "Custom": return results.GetType().GetField("results").GetValue(results) as List<object>;
				//case "Group": return _deviceSession.Client.getAllGroups();
				//case "Id": return results.GetType().GetField("ids").GetValue(results) as List<object>;
				case "Source": return _deviceSession.Client.getSourceById((string)_directParams["idSearch"]);
				case "ValueSetConcept": return _deviceSession.Client.getValueSetConceptsByValueSetVersionId((string)_directParams["valueSetVersionIdSearch"], pageNumber, _pageSize);
				case "ValueSet": return _deviceSession.Client.getValueSetByOid((string)_directParams["valueSetOidSearch"]);
				case "ValueSetVersion": return _deviceSession.Client.getValueSetVersionById((string)_directParams["idSearch"]);
				case "View": return _deviceSession.Client.getViewById((string)_directParams["idSearch"]);
				case "ViewVersion": return _deviceSession.Client.getViewVersionById((string)_directParams["idSearch"]);
				default: throw new InvalidOperationException(String.Format("Could not retrieve result for resource type {0}.", _fhirTableNode.ResourceType));
			}

			// getViewById(viewId)
			// getViewByName(viewName)
			// getViewVersionById(viewVersionId)
			// getViewVersionByViewNameAndVersionNumber(viewName, versionNumber)
			// getGroupById(groupId)
			// getGroupByName(name)
			// getValueSetVersionById(valueSetVersionId)
			// getValueSetVersionByValueSetOidAndVersionNumber(valueSetOid, versionNumber)
			// getCodeSystemConceptByOidAndCode(codeSystemOid, conceptCode)
			// getValueSetConceptById(valueSetConceptId)
			// getCodeSystemByOid(codeSystemOid)
			// getAuthorityById(authorityId)
			// getSourceById(sourceId)
			// getValueSetByOid(valueSetOid)

			// getGroupsByValueSetOid(valueSetOid)
			// getGroupIdsByValueSetOid(valueSetOid)
			// getViewVersionsByValueSetVersionId(valueSetVersionId)
			// getViewVersionIdsByValueSetVersionId(valueSetVersionId)
			// getValueSetVersionsByValueSetOid(valueSetOid)
			// getValueSetVersionIdsByValueSetOid(valueSetOid)
			// getValueSetConceptsByValueSetVersionId(valueSetVersionId, pageNumber, pageSize)
			// getCodeSystemConceptAltDesignationByOidAndCode(codeSystemOid, conceptCode)
			// getChildCodeSystemConceptsByRelationshipType(codeSystemOid, conceptCode, relationshipType, pageNumber, pageSize)
			// getParentCodeSystemConceptsByRelationshipType(codeSystemOid, conceptCode, relationshipType)
			// getCodeSystemPropertyDefinitionsByCodeSystemOid(codeSystemOid)
			// getValueSetVersionsByCodeSystemConceptOidAndCode(codeSystemOid, conceptCode)
			// getValueSetVersionIdsByCodeSystemConceptOidAndCode(codeSystemOid, conceptCode)
			// getCodeSystemConceptPertyValuesByOidAndCode(codeSystemOid, conceptCode)
			// getCodeSystemConceptsByCodeSystemOid(codeSystemOid)
			// getValueSetsByGroupId(groupId)
			// getValueSetsByGroupName(name)
			// getValueSetOidsByGroupId(groupId)
			// getValueSetVersionsByViewVersionId(viewVersionId)
		}

		private object FindResult(int pageNumber)
		{
			switch (_fhirTableNode.ResourceType)
			{
				//case "Authority": return _deviceSession.Client.findAuthorities(pageNumber, _searchParams);
				//case "CodeSystemConceptAltDesignation": return _deviceSession.Client.findCodeSystemConceptAltDesignations();
				//case "CodeSystemConceptPropertyValue": return _deviceSession.Client.findCodeSystemConceptPropertyValues();
				case "CodeSystemConcept": return _deviceSession.Client.findCodeSystemConcepts((CodeSystemConceptSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				//case "CodeSystemPropertyDefinition": _deviceSession.Client.findCodeSystemPropertyDefinitions();
				case "CodeSystem": return _deviceSession.Client.findCodeSystems((CodeSystemSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				//case "Custom": return _deviceSession.Client.invokeCustomMethod();
				case "Group": return _deviceSession.Client.findGroups((GroupSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				//case "Id": return _deviceSession.Client.findIds(_searchParams, pageNumber, _pageSize);
				//case "Source": return _deviceSession.Client.findSources(_searchParams, pageNumber, _pageSize);
				case "ValueSetConcept": return _deviceSession.Client.findValueSetConcepts((ValueSetConceptSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				case "ValueSet": return _deviceSession.Client.findValueSets((ValueSetSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				case "ValueSetVersion": return _deviceSession.Client.findValueSetVersions((ValueSetVersionSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				case "View": return _deviceSession.Client.findViews((ViewSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				case "ViewVersion": return _deviceSession.Client.findViewVersions((ViewVersionSearchCriteriaDto)_searchParams, pageNumber, _pageSize);
				default: throw new InvalidOperationException(String.Format("Could not invoke find result for resource type {0}.", _fhirTableNode.ResourceType));
			}
		}

		private void SetTotalResults(object results)
		{
			var totalResults = (Int32)results.GetType().GetField("totalResults").GetValue(results);
			_totalResults = totalResults;
		}

		private string GetErrorText(object results)
		{
			return results.GetType().GetField("errorText").GetValue(results) as String;
		}

		private void CheckError(object results)
		{
			var errorText = GetErrorText(results);
			if (errorText != null)
			{
				// TODO: Create a PHINVADSDeviceException for proper marshalling of this error
				throw new InvalidOperationException(errorText);
			}
		}

		private object OpenResult(int pageNumber)
		{
			// If the search is restricted, issue as a find, otherwise, issue as a getAll
			object result = null;
			switch (_apiType)
			{
				case APIType.Find:
					result = FindResult(pageNumber);
				break;

				case APIType.GetAll:
					result = GetResult();
				break;

				case APIType.Direct:
					result = GetDirect(pageNumber);
				break;
			}

			CheckError(result);
			_currentPage = pageNumber;
			return result;
		}

		private int GetMaxPageNumber()
		{
			if (_apiType != APIType.GetAll)
			{
				return _totalResults / _pageSize + (_totalResults % _pageSize > 0 ? 1 : 0);
			}
			else
			{
				return CMinPageNumber;
			}
		}

		private void OpenBundle(object bundle)
		{
			_bundle = bundle;
			_resources = new List<object>();
			if (_bundle != null)
			{
				// TODO: Would be more efficient to leave it as an IEnumerable, but would change the way the cursor works...
				_resources.AddRange(GetResults(_bundle));
				// Set TotalResults
				SetTotalResults(bundle);
			}
		}

		private bool FirstPage()
		{
			if (_bundle != null)
			{
				OpenBundle(OpenResult(CMinPageNumber));
				return true;
			}
			return false;
		}

		private bool LastPage()
		{
			if (_bundle != null)
			{
				OpenBundle(OpenResult(GetMaxPageNumber()));
				return true;
			}

			return false;
		}

		private bool NextPage()
		{
			if (_bundle != null)
			{
				if (_currentPage < (GetMaxPageNumber()))
				{
					OpenBundle(OpenResult(_currentPage + 1));
					return true;
				}
			}

			return false;
		}

		private bool PriorPage()
		{
			if (_bundle != null)
			{
				if (_currentPage > CMinPageNumber)
				{
					OpenBundle(OpenResult(_currentPage - 1));
					return true;
				}
			}

			return false;
		}

		private bool IsFirstPage()
		{
			if (_bundle != null)
			{
				return _currentPage == CMinPageNumber;
			}

			return true;
		}

		private bool IsLastPage()
		{
			if (_bundle != null)
			{
				return _currentPage == GetMaxPageNumber();
			}

			return true;
		}

		protected override void InternalOpen()
		{
			InitializeResourceType();
			InitializeSearchParams();

			OpenBundle(OpenResult(CMinPageNumber));

			_currentIndex = -1; // Set to the BOF crack
		}

		protected override void InternalClose()
		{
			// Nothing to do
		}

		public override CursorCapability Capabilities
		{
			get
			{
				return CursorCapability.Navigable
					| CursorCapability.BackwardsNavigable;
			}
		}

		public override CursorType CursorType
		{
			get { return CursorType.Dynamic; }
		}

		protected override bool InternalRefresh(IRow row)
		{
			return base.InternalRefresh(row);
		}

		protected override void InternalSelect(IRow row)
		{
			// TODO: Will column order in this row always be guaranteed... if so we could avoid the lookup, but...
			// Return a row containing the values for the current resource in the bundle
			var resource = _resources[_currentIndex];
			for (int index = 0; index < row.DataType.Columns.Count; index++)
			{
				var value = ObjectMarshal.ToNativeOf(Program.ValueManager, row.DataType.Columns[index].DataType, _resourceType.GetField(row.DataType.Columns[index].Name).GetValue(resource));
				row[index] = value;
			}
		}

		protected override bool InternalNext()
		{
			_currentIndex++;
			if (_currentIndex >= _resources.Count)
			{
				if (NextPage())
					_currentIndex = 0;
			}
			return _currentIndex < _resources.Count;
		}

		protected override bool InternalPrior()
		{
			_currentIndex--;
			if (_currentIndex < 0)
			{
				if (PriorPage())
					_currentIndex = _resources.Count;
			}
			return _currentIndex < 0;
		}

		protected override bool InternalBOF()
		{
			return IsFirstPage() && _currentIndex < 0;
		}

		protected override bool InternalEOF()
		{
			return IsLastPage() && _currentIndex >= _resources.Count;
		}

		protected override void InternalFirst()
		{
			FirstPage();
			_currentIndex = -1;
		}

		protected override void InternalLast()
		{
			LastPage();
			_currentIndex = _resources.Count;
		}
	}
}
