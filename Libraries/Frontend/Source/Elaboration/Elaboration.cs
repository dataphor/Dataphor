#define ORDERREFERENCESBYPRIORITYONLY

/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Frontend.Server.Derivation;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Server.Elaboration
{
	//	Build the expression tree out of the catalog data returned by DescribeTableVar
	//	
	//	Given a catalog, and the name of a table, derive an expression for use in interface derivation
	//	
	//	ElaboratedExpression
	//		ElaboratedTableVar
	//			ElaboratedName
	//			TableVar
	//			ElaboratedReferences
	//			ElaboratedReference (The reference responsible for the inclusion of this table)
	//		ElaboratedTableVarColumns
	//			ElaboratedTableVarColumn
	//				ElaboratedName
	//				ElaboratedTableVar
	//				Visible
	//		
	//	ElaboratedReference
	//		Reference
	//		ReferenceType
	//		SourceElaboratedTableVar
	//		TargetElaboratedTableVar
    
    public enum ReferenceType { Detail, Extension, Lookup, Parent }
    
	public class ElaboratedExpression : Object
	{		
		// Initial constructor used to start elaboration of an expression for a given table variable
		public ElaboratedExpression
		(
			Program program,
			string query,
			bool elaborate,
			Schema.TableVar tableVar,
			Schema.Catalog catalog,
			string[] detailKeys,
			string mainElaboratedTableName,
			string pageType
		)
		{
			_program = program;
			_process = program.ServerProcess;
			_pageType = pageType;
			_detailKeys = detailKeys;
			_elaborate = elaborate;
			_mainElaboratedTableVar = new ElaboratedTableVar(this, tableVar, _pageType, AddTableName(mainElaboratedTableName), query);

			// The invariant is the dequlified detail key
			_invariant = new Schema.Key();
			for (int index = 0; index < _detailKeys.Length; index++)
				_invariant.Columns.Add(_mainElaboratedTableVar.TableVar.Columns[Schema.Object.Dequalify(_detailKeys[index])]);

			if (elaborate)
			{
				BuildParentReferences(_mainElaboratedTableVar);
				BuildExtensionReferences(_mainElaboratedTableVar);
				BuildLookupReferences(catalog, _mainElaboratedTableVar);
				BuildDetailReferences(_mainElaboratedTableVar);
			}
		}

		// Subsequent constructor used to start nested derivation of a lookup expression for a given table variable
		protected ElaboratedExpression
		(
			Program program,
			ElaboratedExpression parentExpression,
			bool elaborate,
			Schema.Catalog catalog,
			string tableName,
			string[] detailKeys,
			string mainElaboratedTableName,
			string pageType
		)
		{
			_program = program;
			_process = program.ServerProcess;
			_pageType = pageType;
			_detailKeys = detailKeys;
			_elaborate = elaborate;
			_parentExpression = parentExpression;
			_mainElaboratedTableVar = new ElaboratedTableVar(this, (Schema.TableVar)catalog.Objects[tableName], _pageType, mainElaboratedTableName);
			if (elaborate)
			{
				BuildParentReferences(_mainElaboratedTableVar);
				BuildExtensionReferences(_mainElaboratedTableVar);
				BuildLookupReferences(catalog, _mainElaboratedTableVar);
			}
		}

		public static string[] QualifyNames(string[] names, string nameSpace)
		{
			string[] result = new string[names.Length];
			for (int index = 0; index < names.Length; index++)
				result[index] = Schema.Object.Qualify(names[index], nameSpace);
			return result;
		}
		
		public static string[] DequalifyNames(string[] names)
		{
			string[] result = new string[names.Length];
			for (int index = 0; index < names.Length; index++)
				result[index] = Schema.Object.Dequalify(names[index]);
			return result;
		}
		
		public static string CombineGroups(string outerGroup, string innerGroup)
		{
			if (outerGroup != String.Empty)
				if (innerGroup != String.Empty)
					return String.Format("{0}\\{1}", outerGroup, innerGroup);
				else
					return outerGroup;
			else
				return innerGroup;
		}
		
		protected ElaboratedExpression _parentExpression;
		public ElaboratedExpression RootExpression { get { return (_parentExpression == null) ? this : _parentExpression.RootExpression; } }
		
		protected bool _elaborate;
		public bool Elaborate { get { return _elaborate; } }
		
		protected Program _program;
		public Program Program { get { return _program; } }
		
		protected ServerProcess _process;
		public ServerProcess Process { get { return _process; } }
		
		/*
			An inclusion reference is a reference that should not be followed for the purpose of elaboration, or included for the purpose of presentation,
			because it is the reference that was used to arrive at this expression.
			
			In the case that this is a nested expression, the inclusion reference is simply the reference that was used to arrive at this expression;
			otherwise any reference that terminates as a subset of the detail columns is an inclusion reference.
		*/		
		protected bool IsInclusionReference(Schema.ReferenceBase reference)
		{
			if (_parentExpression == null)
				return
					(
						reference.TargetTable.Equals(MainElaboratedTableVar.TableVar) &&
						reference.TargetKey.Columns.IsSubsetOf(Invariant.Columns)
					) ||
					(
						reference.SourceTable.Equals(MainElaboratedTableVar.TableVar) &&
						reference.SourceKey.Columns.IsSubsetOf(Invariant.Columns)
					);
			else
				return (MainElaboratedTableVar.ElaboratedReference != null) && (MainElaboratedTableVar.ElaboratedReference.Reference.OriginatingReferenceName() == reference.OriginatingReferenceName());
		}
				
		protected virtual bool IsCircularReference(ElaboratedTableVar tableVar, Schema.ReferenceBase reference)
		{
			// A reference is circular if the source tablevar = target tablevar of the atablevar.derivedreference
			return
				(tableVar.ElaboratedReference != null) &&
				(
					(
						reference.SourceTable.Equals(tableVar.ElaboratedReference.Reference.TargetTable) &&
						reference.SourceKey.Equals(tableVar.ElaboratedReference.Reference.TargetKey)
					) ||
					(
						reference.TargetTable.Equals(tableVar.ElaboratedReference.Reference.SourceTable) &&
						reference.TargetKey.Equals(tableVar.ElaboratedReference.Reference.SourceKey)
					) ||
					(
						reference.SourceTable.Equals(tableVar.ElaboratedReference.Reference.SourceTable) &&
						reference.SourceKey.Equals(tableVar.ElaboratedReference.Reference.SourceKey)
					) ||
					(
						reference.TargetTable.Equals(tableVar.ElaboratedReference.Reference.TargetTable) &&
						reference.TargetKey.Equals(tableVar.ElaboratedReference.Reference.TargetKey)
					)
				);
		}
		
		protected virtual bool IsIncludedReference(ElaboratedTableVar tableVar, Schema.ReferenceBase reference, ReferenceType referenceType)
		{
			// A reference is included if all the columns in the reference key are included in the table variable
			foreach (Schema.TableVarColumn column in ((referenceType == ReferenceType.Parent) || (referenceType == ReferenceType.Lookup)) ? reference.SourceKey.Columns : reference.TargetKey.Columns)
				if (!tableVar.ColumnNames.Contains(column.Name))
					return false;
			
			return Convert.ToBoolean(DerivationUtility.GetTag(reference.MetaData, "Include", _pageType, referenceType.ToString(), _elaborate ? "True" : "False"));
		}
		
		protected bool _treatParentAsLookup = true;
		
		protected virtual bool ShouldTreatParentAsLookup(Schema.ReferenceBase reference)
		{
			return Convert.ToBoolean(DerivationUtility.GetTag(reference.MetaData, "TreatAsLookup", _pageType, ReferenceType.Parent.ToString(), _treatParentAsLookup.ToString()));
		}
		
		protected virtual void BuildParentReferences(ElaboratedTableVar table)
		{
			if (table.TableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in table.TableVar.References)
					if (reference.SourceTable.Equals(table.TableVar) && reference.SourceKey.IsUnique && !reference.IsExcluded && _program.Plan.HasRight(reference.TargetTable.GetRight(Schema.RightNames.Select)) && !ShouldTreatParentAsLookup(reference) && !IsInclusionReference(reference) && !IsCircularReference(table, reference) && IsIncludedReference(table, reference, ReferenceType.Parent))
					{
						ElaboratedReference elaboratedReference = 
							new ElaboratedReference
							(
								this, 
								reference, 
								ReferenceType.Parent, 
								table, 
								new ElaboratedTableVar(this, reference.TargetTable, DerivationUtility.View)
							);
						table.ElaboratedReferences.Add(elaboratedReference);
						elaboratedReference.TargetElaboratedTableVar.ElaboratedReference = elaboratedReference;
						BuildParentReferences(elaboratedReference.TargetElaboratedTableVar);
					}
		}
		
		protected virtual void BuildExtensionReferences(ElaboratedTableVar table)
		{
			if (table.TableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in table.TableVar.References)
					if (reference.TargetTable.Equals(table.TableVar) && reference.SourceKey.IsUnique && !reference.IsExcluded && _program.Plan.HasRight(reference.SourceTable.GetRight(Schema.RightNames.Select)) && !IsInclusionReference(reference) && !IsCircularReference(table, reference) && IsIncludedReference(table, reference, ReferenceType.Extension))
					{
						ElaboratedReference elaboratedReference =
							new ElaboratedReference
							(
								this,
								reference,
								ReferenceType.Extension,
								new ElaboratedTableVar(this, reference.SourceTable, DerivationUtility.IsReadOnlyPageType(_pageType) ? DerivationUtility.View : DerivationUtility.Edit),
								table
							);
						elaboratedReference.SourceElaboratedTableVar.ElaboratedReference = elaboratedReference;
						table.ElaboratedReferences.Add(elaboratedReference);
					}
		}
		
		protected virtual void BuildLookupReferences(Schema.Catalog catalog, ElaboratedTableVar table)
		{
			if (table.TableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in table.TableVar.References)
					if (reference.SourceTable.Equals(table.TableVar) && (ShouldTreatParentAsLookup(reference) || !reference.SourceKey.IsUnique) && !reference.IsExcluded && _program.Plan.HasRight(reference.TargetTable.GetRight(Schema.RightNames.Select)) && !IsInclusionReference(reference) && !IsCircularReference(table, reference) && IsIncludedReference(table, reference, ReferenceType.Lookup))
					{
						string elaboratedName = AddTableName(reference.TargetTable.Name);
						ElaboratedExpression lookupExpression = 
							new ElaboratedExpression
							(
								_program,
								this, 
								Convert.ToBoolean(DerivationUtility.GetTag(reference.MetaData, "Elaborate", DerivationUtility.Preview, ReferenceType.Lookup.ToString(), DerivationUtility.GetTag(reference.TargetTable.MetaData, "Elaborate", DerivationUtility.Preview, "False"))),
								catalog, 
								reference.TargetTable.Name, 
								QualifyNames(reference.TargetKey.Columns.ColumnNames, elaboratedName),
								elaboratedName,
								DerivationUtility.Preview
							);

						ElaboratedReference elaboratedReference =
							new ElaboratedReference
							(
								this,
								reference,
								ReferenceType.Lookup,
								table,
								lookupExpression.MainElaboratedTableVar
							);

						elaboratedReference.TargetElaboratedTableVar.ElaboratedReference = elaboratedReference;
						table.ElaboratedReferences.Add(elaboratedReference);
					}

			foreach (ElaboratedReference reference in table.ElaboratedReferences)
				if (reference.ReferenceType == ReferenceType.Parent)
					BuildLookupReferences(catalog, reference.TargetElaboratedTableVar);
				else if (reference.ReferenceType == ReferenceType.Extension)
					BuildLookupReferences(catalog, reference.SourceElaboratedTableVar);
		}
		
		protected virtual void BuildDetailReferences(ElaboratedTableVar table)
		{
			if (table.TableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in table.TableVar.References)
					if (reference.TargetTable.Equals(table.TableVar) && !reference.SourceKey.IsUnique && !reference.IsExcluded && _program.Plan.HasRight(reference.SourceTable.GetRight(Schema.RightNames.Select)) && !IsInclusionReference(reference) && IsIncludedReference(table, reference, ReferenceType.Detail))
					{
						ElaboratedReference elaboratedReference =
							new ElaboratedReference
							(
								this,
								reference,
								ReferenceType.Detail,
								new ElaboratedTableVar(this, reference.SourceTable, DerivationUtility.IsSingularPageType(_pageType) ? (DerivationUtility.IsReadOnlyPageType(_pageType) ? DerivationUtility.List : DerivationUtility.Browse) : _pageType),
								table
							);
						elaboratedReference.SourceElaboratedTableVar.ElaboratedReference = elaboratedReference;
						table.ElaboratedReferences.Add(elaboratedReference);
					}
			
			foreach (ElaboratedReference reference in table.ElaboratedReferences)
				if (reference.ReferenceType == ReferenceType.Parent)
					BuildDetailReferences(reference.TargetElaboratedTableVar);
				else if (reference.ReferenceType == ReferenceType.Extension)
					BuildDetailReferences(reference.SourceElaboratedTableVar);
		}
		
		// Stores the names of all tables in the expression to ensure uniqueness is maintained
		protected List<string> _tableNames = new List<string>();
		
		protected string InternalAddTableName(string tableName)
		{
			string result = Schema.Object.Unqualify(tableName);
			if (_tableNames.Contains(result))
			{
				int counter = 0;
				do
				{
					counter++;
				} while (_tableNames.Contains(String.Format("{0}{1}", result, counter.ToString())));
				result = String.Format("{0}{1}", result, counter.ToString());
			}
			_tableNames.Add(result);
			return result;
		}

		protected internal string AddTableName(string tableName)
		{
			return RootExpression.InternalAddTableName(tableName);
		}
		
		// Stores the names of all references in the expression to ensure uniqueness is maintained
		// Note that the derived reference names could be used for this purpose, but this would
		// break backwards compatibility with forms and customizations created before the derived
		// reference name was introduced (#23692).
		protected List<string> _referenceNames = new List<string>();
		
		protected string InternalAddReferenceName(string referenceName)
		{
			string result = referenceName;
			if (_referenceNames.Contains(result))
			{
				int counter = 0; 
				do
				{
					counter++;
				} while (_referenceNames.Contains(String.Format("{0}{1}", result, counter.ToString())));
				result = String.Format("{0}{1}", result, counter.ToString());
			}
			_referenceNames.Add(result);
			return result;
		}
		
		protected internal string AddReferenceName(string referenceName)
		{
			return RootExpression.InternalAddReferenceName(referenceName);
		}
		
		// AGroupName must always be a qualification of AUnqualifiedGroupName (e.g. AGroupName = "Address\City", AUnqualifiedGroupName = "City")
		// It is assumed that groups already exist for the qualifier portions of AGroupName, in other words, this procedure only ensures that
		// groups exist for the groups specified in AUnqualifiedGroupName
		public ElaboratedGroup EnsureGroups(string groupName, string unqualifiedGroupName, string groupTitle, ElaboratedTableVar tableVar, ElaboratedReference reference)
		{
			string qualifier = groupName;
			if (unqualifiedGroupName != String.Empty)
			{
				int unqualifiedIndex = groupName.LastIndexOf(unqualifiedGroupName);
				if (unqualifiedIndex != (groupName.Length - unqualifiedGroupName.Length))
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.InvalidGrouping, unqualifiedGroupName, groupName);
				
				qualifier = groupName.Substring(0, unqualifiedIndex);
			}
				
			string[] unqualifiedGroupNames = unqualifiedGroupName.Split('\\');
			
			StringBuilder localUnqualifiedGroupName = new StringBuilder();
			ElaboratedGroup group = null;
			for (int index = 0; index < unqualifiedGroupNames.Length; index++)
			{
				if (localUnqualifiedGroupName.Length > 0)
					localUnqualifiedGroupName.Append("\\");
				localUnqualifiedGroupName.Append(unqualifiedGroupNames[index]);
				group =
					EnsureGroup
					(
						String.Format
						(
							"{0}{1}", 
							qualifier, 
							localUnqualifiedGroupName.ToString()
						), 
						localUnqualifiedGroupName.ToString(), 
						(index < (unqualifiedGroupNames.Length - 1)) ? String.Empty : groupTitle,	// only use title for innermost group
						tableVar,
						reference
					);
			}
			return group;
		}
		
		public ElaboratedGroup EnsureGroup(string groupName, string unqualifiedGroupName, string groupTitle, ElaboratedTableVar tableVar, ElaboratedReference reference)
		{
			int groupIndex = _groups.IndexOf(groupName);
			if (groupIndex >= 0)
				return _groups[groupIndex];
			return AddGroup(groupName, unqualifiedGroupName, groupTitle, tableVar, reference);
		}
		
		public ElaboratedGroup AddGroup(string groupName, string unqualifiedGroupName, string groupTitle, ElaboratedTableVar tableVar, ElaboratedReference reference)
		{
			ElaboratedGroup group = new ElaboratedGroup(groupName, unqualifiedGroupName);

			if (unqualifiedGroupName != String.Empty)
			{
				if (tableVar.TableVar.MetaData != null)
				{
					MetaData groupMetaData = DerivationUtility.ExtractTags(tableVar.TableVar.MetaData.Tags, String.Format("Group.{0}", unqualifiedGroupName.Replace("\\", ".")), tableVar.PageType);
					group.Properties.AddOrUpdateRange(groupMetaData.Tags);
				}

				if ((reference != null) && (reference.GroupMetaData != null))
					group.Properties.AddOrUpdateRange(reference.GroupMetaData.Tags);
			}

			string title = DerivationUtility.GetTag(new MetaData(group.Properties), "Title", tableVar.PageType, String.Empty);
			if ((title == String.Empty) && (groupTitle != String.Empty))
				group.Properties.AddOrUpdate("Frontend.Title", groupTitle);
			_groups.Add(group);
			return group;
		}
		
		protected ElaboratedGroups _groups = new ElaboratedGroups();
		public ElaboratedGroups Groups { get { return _groups; } }
		
		// GroupStack
		protected ElaboratedGroups _groupStack = new ElaboratedGroups();
		protected void InternalPushGroup(string groupName)
		{
			InternalPushGroup(new ElaboratedGroup(groupName));
		}
		
		protected void InternalPushGroup(ElaboratedGroup group)
		{
			_groupStack.Add(group);
		}
		
		public void PushGroup(string groupName)
		{
			RootExpression.InternalPushGroup(groupName);
		}
		
		public void PushGroup(ElaboratedGroup group)
		{
			RootExpression.InternalPushGroup(group);
		}
		
		protected void InternalPopGroup()
		{
			_groupStack.RemoveAt(_groupStack.Count - 1);
		}
		
		public void PopGroup()
		{
			RootExpression.InternalPopGroup();
		}
		
		protected string InternalCurrentGroupName()
		{
			StringBuilder groupName = new StringBuilder();
			foreach (ElaboratedGroup group in _groupStack)
			{
				if (groupName.Length > 0)
					groupName.Append("\\");
				groupName.Append(group.Name);
			}
			return groupName.ToString();
		}
		
		public string CurrentGroupName()
		{
			return RootExpression.InternalCurrentGroupName();
		}

		// PageType
		protected string _pageType;
		public string PageType { get { return _pageType; } }
		
		// DetailKeys
		protected string[] _detailKeys;
		public string[] DetailKeys { get { return _detailKeys; } }
		
		// Invariant
		protected Schema.Key _invariant;
		public Schema.Key Invariant { get { return _invariant; } }
		
		// InclusionReference
		protected Schema.Reference _inclusionReference;
		public Schema.Reference InclusionReference { get { return _inclusionReference; } }
		
		// MainElaboratedTableVar		
		protected ElaboratedTableVar _mainElaboratedTableVar;
		public ElaboratedTableVar MainElaboratedTableVar
		{
			get { return _mainElaboratedTableVar; }
			set
			{
				if (_mainElaboratedTableVar != null)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.MainTableSet);
				if (value == null)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.MainTableRequired);
				_mainElaboratedTableVar = value;
			}
		}
		
		protected ElaboratedTableVarColumns _columns = new ElaboratedTableVarColumns();
		public ElaboratedTableVarColumns Columns { get { return _columns; } }
		
		public Expression Expression
		{
			get
			{
				_columns.Clear();
				_mainElaboratedTableVar.AddColumns();
				Expression expression = _mainElaboratedTableVar.Expression;
				return expression;
			}
		}
	}
	
	public class ElaboratedGroup : System.Object
	{
		public ElaboratedGroup(string name) : base()
		{
			_name = name;
			_unqualifiedName = name;
		}
		
		public ElaboratedGroup(string name, string unqualifiedName) : base()
		{
			_name = name;
			_unqualifiedName = unqualifiedName;
		}

		private string _name;
		public string Name { get { return _name; } }
		
		private string _unqualifiedName;
		public string UnqualifiedName { get { return _unqualifiedName; } }
		
		private Tags _properties = new Tags();
		public Tags Properties { get { return _properties; } }
	}

	#if USETYPEDLIST
	public class ElaboratedGroups : TypedList
	{
		public ElaboratedGroups() : base(typeof(ElaboratedGroup)){}
		
		public new ElaboratedGroup this[int AIndex] 
		{ 
			get { return (ElaboratedGroup)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
	
	#else
	public class ElaboratedGroups : BaseList<ElaboratedGroup>
	{
	#endif
		public ElaboratedGroup this[string name] { get { return this[IndexOf(name)]; } }
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].Name == name)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}

		public string ResolveGroupName(string name)
		{
			if (IndexOf(name) != -1)
				return name;
            
			// seach for any group that ends in AName
			for (int index = 0; index < Count; index++)
				if (this[index].Name.EndsWith(name))
					return this[index].Name;

			throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.GroupNotFound, name);
		}
	}
	
	public class ElaboratedTableVar : Object
	{
		public ElaboratedTableVar(ElaboratedExpression expression, Schema.TableVar tableVar, string pageType) : base()
		{
			_elaboratedName = expression.AddTableName(tableVar.Name);
			InternalCreateElaboratedTableVar(expression, tableVar, pageType, tableVar.Name);
		}
		
		public ElaboratedTableVar(ElaboratedExpression expression, Schema.TableVar tableVar, string pageType, string elaboratedName) : base()
		{
			_elaboratedName = elaboratedName;
			InternalCreateElaboratedTableVar(expression, tableVar, pageType, tableVar.Name);
		}
		
		public ElaboratedTableVar(ElaboratedExpression expression, Schema.TableVar tableVar, string pageType, string elaboratedName, string query) : base()
		{
			_elaboratedName = elaboratedName;
			InternalCreateElaboratedTableVar(expression, tableVar, pageType, query);
		}

		/// <remarks> Don't try to parse an expression with normalized white space, line comments and embedded quotes can invalidate the query. </remarks>
		private string NormalizeWhiteSpace(string stringValue)
		{
			StringBuilder result = new StringBuilder();
			bool inWhiteSpace = false;
			for (int index = 0; index < stringValue.Length; index++)
			{
				if (Char.IsWhiteSpace(stringValue, index))
				{
					if (!inWhiteSpace)
					{
						inWhiteSpace = true;
						result.Append(" ");
					}
				}
				else
				{
					inWhiteSpace = false;
					result.Append(stringValue[index]);
				}
			}
			
			return result.ToString();
		}
		
		private void InternalCreateElaboratedTableVar(ElaboratedExpression expression, Schema.TableVar tableVar, string pageType, string query)
		{
			_query = query;
			string defaultTitle = NormalizeWhiteSpace(_query);	// Don't try to parse an expression with normalized white space, line comments can invalidate the query
			Expression localExpression = new Parser().ParseExpression(_query);
			if ((localExpression is IdentifierExpression) || (localExpression is QualifierExpression))
				defaultTitle = Schema.Object.Unqualify(defaultTitle);
			_elaboratedExpression = expression;
			_tableVar = tableVar;
			_pageType = pageType;
			_tableTitle = 
				DerivationUtility.GetTag
				(
					_tableVar.MetaData, 
					"Title", 
					_pageType, 
					defaultTitle
				);
				
			// Gather the included column list
			#if USEINCLUDETAG
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
				if (Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "Include", _pageType, _pageType == DerivationUtility.Preview ? "False" : "True")))
					_columnNames.Add(column.Name);
			#endif

			// If no columns are marked to be included, the default for the include tag is true				
			#if INCLUDEBYDEFAULT
			if (FColumnNames.Count == 0)
				foreach (Schema.TableVarColumn column in FTableVar.Columns)
					#if USEINCLUDETAG
					if (Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "Include", FPageType, "True")))
					#endif
						FColumnNames.Add(column.Name);
			#endif
						
			int insertIndex = 0;

			// Ensure that key columns for the clustered key are preserved if this is not a preview page type
			if (_pageType != DerivationUtility.Preview)
				// TODO: Refactor this, it _cannot_ access the compiler, this is way too much.
				foreach (Schema.TableVarColumn column in expression.Program.FindClusteringKey(_tableVar).Columns)
					if (!_columnNames.Contains(column.Name))
					{
						_columnNames.Insert(insertIndex, column.Name);
						insertIndex++;
					}

			// Ensure that the detail key columns are preserved, if they are part of this table var
			foreach (Schema.Column column in _tableVar.DataType.Columns)
				if (((IList)_elaboratedExpression.DetailKeys).Contains(Schema.Object.Qualify(column.Name, _elaboratedName)) && !_columnNames.Contains(column.Name))
				{
					_columnNames.Insert(insertIndex, column.Name);
					insertIndex++;
				}
		}
		
		// ElaboratedExpression
		protected ElaboratedExpression _elaboratedExpression;
		public ElaboratedExpression ElaboratedExpression { get { return _elaboratedExpression; } }
		
		// ElaboratedName
		protected string _elaboratedName = String.Empty;
		public string ElaboratedName { get { return _elaboratedName; } }
		
		// TableVar
		protected Schema.TableVar _tableVar;
		public Schema.TableVar TableVar { get { return _tableVar; } }
		
		// PageType
		protected string _pageType;
		public string PageType { get { return _pageType; } }
		
		// Query
		protected string _query;
		public string Query { get { return _query; } }
		
		// TableTitle
		protected string _tableTitle = String.Empty;
		public string TableTitle { get { return _tableTitle; } }

		// ColumnNames
		protected List<string> _columnNames = new List<string>();
		public List<string> ColumnNames { get { return _columnNames; } }
		
		// ElaboratedReferences
		protected ElaboratedReferences _elaboratedReferences = new ElaboratedReferences();
		public ElaboratedReferences ElaboratedReferences { get { return _elaboratedReferences; } }
		
		// ElaboratedReference
		protected ElaboratedReference _elaboratedReference;
		public ElaboratedReference ElaboratedReference
		{
			get { return _elaboratedReference; }
			set
			{
				if (_elaboratedReference != null)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.ElaboratedReferenceSet, _elaboratedName);
				_elaboratedReference = value;
				EnsureElaboratedReferenceColumns();
			}
		}
		
		protected void EnsureElaboratedReferenceColumns()
		{
			// Make sure that all the columns named by the including reference are included in the expression
			foreach (Schema.TableVarColumn column in (_elaboratedReference.ReferenceType == ReferenceType.Lookup) || (_elaboratedReference.ReferenceType == ReferenceType.Parent) ? _elaboratedReference.Reference.TargetKey.Columns : _elaboratedReference.Reference.SourceKey.Columns)
				if (!_columnNames.Contains(column.Name))
					_columnNames.Add(column.Name);
		}
		
		protected bool _isEmbedded; // Internal to determine whether this table has been embedded into the expression
		public bool IsEmbedded
		{
			get { return _isEmbedded; }
			set
			{
				if (_isEmbedded)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.EmbeddedSet, _elaboratedName); 
				_isEmbedded = value;
			}
		}
		
		protected void InsertColumn(ElaboratedReference reference, Schema.TableVarColumn column, bool visible, string groupName, string unqualifiedGroupName, string groupTitle, int index)
		{
			string elaboratedName = Schema.Object.Qualify(column.Name, ElaboratedName);
			visible = visible && !((IList)_elaboratedExpression.RootExpression.DetailKeys).Contains(elaboratedName) && Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "Visible", PageType, "True"));
			bool readOnly = column.ReadOnly || DerivationUtility.IsReadOnlyPageType(PageType) || Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "ReadOnly", PageType, "False")) || ((ElaboratedReference != null) && (ElaboratedReference.ReferenceType == ReferenceType.Lookup));
			_elaboratedExpression.RootExpression.EnsureGroups(groupName, unqualifiedGroupName, groupTitle, this, reference);
			ElaboratedTableVarColumn localColumn = new ElaboratedTableVarColumn(this, reference, column, visible, readOnly, groupName);
			_elaboratedExpression.RootExpression.Columns.Insert(index, localColumn);
			if (reference != null)
				reference.Columns.Add(localColumn);
		} 
		
		protected void ProcessKey(ElaboratedReference reference, Schema.TableVarColumnsBase columns)
		{
			foreach (ElaboratedTableVarColumn referenceColumn in reference.Columns)
				if (columns.Contains(referenceColumn.Column.Name))
				{
					referenceColumn.IsMaster = true;
					referenceColumn.Visible = false;
				}
		}
		
		protected void InternalInsertColumns(ref int columnIndex)
		{
			// Add all columns
			if (_elaboratedReference != null && (_elaboratedReference.ReferenceType == ReferenceType.Extension))
			{
				Schema.TableVarColumn rowExistsColumn = new Schema.TableVarColumn(new Schema.Column("RowExists", _elaboratedExpression.Process.DataTypes.SystemBoolean), Schema.TableVarColumnType.RowExists);
				rowExistsColumn.MetaData = new MetaData();
				rowExistsColumn.MetaData.Tags.Add
				(
					new Tag
					(
						"Frontend.Title",
						String.Format
						(
							Strings.Get("HasColumnTitle"), 
							(_elaboratedReference.ReferenceTitle != null ? _elaboratedReference.ReferenceTitle : _elaboratedReference.SourceElaboratedTableVar.TableTitle)
						)
					)
				);
				rowExistsColumn.MetaData.Tags.Add(new Tag("Frontend.ElementType", "CheckBox"));

				if (_elaboratedReference.Reference.MetaData != null)
				{
					MetaData metaData = DerivationUtility.ExtractTags(_elaboratedReference.Reference.MetaData.Tags, "RowExists", _elaboratedReference.TargetElaboratedTableVar.PageType);
					rowExistsColumn.MetaData.Tags.AddOrUpdateRange(metaData.Tags);
				}

				InsertColumn(null, rowExistsColumn, true, _elaboratedExpression.CurrentGroupName(), String.Empty, String.Empty, columnIndex);
				columnIndex++;
			}
				
			// Add each column, making it invisible if it is the right side of a reference
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				if (_columnNames.Contains(column.Name))
				{
					string groupName = DerivationUtility.GetTag(column.MetaData, "Group", _pageType, String.Empty);
					bool isRightSide =
						(_elaboratedReference != null) && 
						(
							(
								(_elaboratedReference.ReferenceType == ReferenceType.Extension) && 
								_elaboratedReference.Reference.SourceKey.Columns.Contains(column.Name)
							) ||
							(
								(_elaboratedReference.ReferenceType == ReferenceType.Lookup) &&
								_elaboratedReference.Reference.TargetKey.Columns.Contains(column.Name)
							)
						);
					
					InsertColumn(null, column, !isRightSide, ElaboratedExpression.CombineGroups(_elaboratedExpression.CurrentGroupName(), groupName), groupName, Schema.Object.Unqualify(groupName.Replace("\\", ".")), columnIndex);
					columnIndex++;
				}
			}			

			// Add columns for lookup references
			foreach (ElaboratedReference reference in ElaboratedReferences)
			{
				if (reference.ReferenceType == ReferenceType.Lookup)
				{
					string lookupGroupName = String.Format("{0}{1}", reference.ElaboratedName, "Group");

					string referenceGroupName = null;

					// Default group name for the reference is the group name for the appearing columns, if the group name for all appearing columns is the same
					foreach (ElaboratedTableVarColumn referenceColumn in reference.Columns)
					{
						if (referenceColumn.Visible)
						{
							if (referenceGroupName == null)
								referenceGroupName = DerivationUtility.GetTag(referenceColumn.Column.MetaData, "Group", _pageType, String.Empty);
							else
								if (referenceGroupName != DerivationUtility.GetTag(referenceColumn.Column.MetaData, "Group", _pageType, String.Empty))
								{
									referenceGroupName = String.Empty;
									break;
								}
						}
					}
					
					if (referenceGroupName == null)
						referenceGroupName = String.Empty;
						
					referenceGroupName = DerivationUtility.GetTag(reference.Reference.MetaData, "Group", _pageType, reference.ReferenceType.ToString(), referenceGroupName);
					string referenceLookupGroupName = ElaboratedExpression.CombineGroups(referenceGroupName, lookupGroupName);
					string fullReferenceGroupName = ElaboratedExpression.CombineGroups(ElaboratedExpression.CurrentGroupName(), referenceLookupGroupName);
					
					ElaboratedExpression.RootExpression.EnsureGroups(fullReferenceGroupName, referenceLookupGroupName, reference.GroupTitle, this, reference);

/*
					// Ensure that each column appearing in the reference is displayed within the same group.						
					foreach (ElaboratedTableVarColumn LReferenceColumn in LReference.Columns)
						if (LReferenceColumn.Visible && (LReferenceColumn.GroupName != LFullReferenceGroupName))
							LReferenceColumn.GroupName = LFullReferenceGroupName;
*/
					
					// insert all source columns after the last source column as lookup reference columns
					int localColumnIndex = -1;
					foreach (Schema.TableVarColumn referenceColumn in reference.Reference.SourceKey.Columns)
					{
						int elaboratedColumnIndex = ElaboratedExpression.RootExpression.Columns.IndexOf(Schema.Object.Qualify(referenceColumn.Column.Name, ElaboratedName));
						if (elaboratedColumnIndex > localColumnIndex)
							localColumnIndex = elaboratedColumnIndex;
					}
					if (localColumnIndex == -1)
						localColumnIndex = ElaboratedExpression.RootExpression.Columns.Count;

					foreach (Schema.TableVarColumn referenceColumn in reference.Reference.SourceKey.Columns)
					{
						string groupName = DerivationUtility.GetTag(referenceColumn.MetaData, "Group", _pageType, String.Empty);
						bool isRightSide =
							(_elaboratedReference != null) && 
							(
								(
									(_elaboratedReference.ReferenceType == ReferenceType.Extension) && 
									_elaboratedReference.Reference.SourceKey.Columns.Contains(referenceColumn.Name)
								) ||
								(
									(_elaboratedReference.ReferenceType == ReferenceType.Lookup) &&
									_elaboratedReference.Reference.TargetKey.Columns.Contains(referenceColumn.Name)
								)
							);

						InsertColumn
						(
							reference,
							referenceColumn, 
							!isRightSide,
							fullReferenceGroupName,
							referenceLookupGroupName,
							reference.GroupTitle,
							localColumnIndex
						);
						
						if (columnIndex >= localColumnIndex)
							columnIndex++;
						localColumnIndex++;
					}
					
					// If the intersection of the reference columns and the detail key of the derivation is non-empty
					Schema.Key subsetKey = new Schema.Key();
					if (ElaboratedExpression.DetailKeys.Length > 0)
					{
						foreach (Schema.TableVarColumn column in reference.Reference.SourceKey.Columns)
						{
							string referenceColumnName = Schema.Object.Qualify(column.Name, ElaboratedName);
							if ((_elaboratedReference != null) && (_elaboratedReference.ReferenceType == ReferenceType.Extension))
							{
								int extensionKeyIndex = _elaboratedReference.Reference.SourceKey.Columns.IndexOf(column.Name);
								if (extensionKeyIndex >= 0)
									referenceColumnName = Schema.Object.Qualify(_elaboratedReference.Reference.TargetKey.Columns[extensionKeyIndex].Name, _elaboratedReference.TargetElaboratedTableVar.ElaboratedName);
							}
							
							if (((IList)ElaboratedExpression.DetailKeys).Contains(referenceColumnName))
								subsetKey.Columns.Add(column);
						}
					}
					
					if (subsetKey.Columns.Count > 0)
					{
						// the reference is a detail lookup
						reference.IsDetailLookup = true;

						// set the key lookup reference columns invisible
						ProcessKey(reference, subsetKey.Columns);
					}
					
					foreach (Schema.Key key in TableVar.Keys)
						// if the columns in the reference form a non-trivial proper superset of the key
						if ((key.Columns.Count > 0) && reference.Reference.SourceKey.Columns.IsProperSupersetOf(key.Columns))
						{
							// the reference is a detail lookup
							reference.IsDetailLookup = true;
							
							// set the key lookup reference columns invisible
							ProcessKey(reference, key.Columns);
						}
					
					// For every reference that is included in the current reference, mark the columns of that reference as master
					foreach (ElaboratedReference otherReference in ElaboratedReferences)	
						if (otherReference.IsEmbedded && (otherReference.ReferenceType == ReferenceType.Lookup) && !Object.ReferenceEquals(reference, otherReference) && reference.Reference.SourceKey.Columns.IsProperSupersetOf(otherReference.Reference.SourceKey.Columns))
							ProcessKey(reference, otherReference.Reference.SourceKey.Columns);
							
					// If this is an extension table and the intersection of the extension reference and the current reference is non-empty
					if 
					(
						(_elaboratedReference != null) && 
						(_elaboratedReference.ReferenceType == ReferenceType.Extension) && 
						(_elaboratedReference.Reference.SourceKey.Columns.Intersect(reference.Reference.SourceKey.Columns).Columns.Count > 0)
					)
					{
						// Search the parent table for references including the current reference
						foreach (ElaboratedReference otherReference in _elaboratedReference.TargetElaboratedTableVar.ElaboratedReferences)
							if (otherReference.IsEmbedded && (otherReference.ReferenceType == ReferenceType.Lookup) && otherReference.Reference.SourceKey.Columns.IsSubsetOf(_elaboratedReference.Reference.TargetKey.Columns))
							{
								// Translate the source key for LOtherReference into the current table var
								Schema.Key key = new Schema.Key();
								foreach (Schema.TableVarColumn column in otherReference.Reference.SourceKey.Columns)
									key.Columns.Add(_elaboratedReference.Reference.SourceKey.Columns[_elaboratedReference.Reference.TargetKey.Columns.IndexOfName(column.Name)]);
									
								// Mark the columns of the included reference master
								if (reference.Reference.SourceKey.Columns.IsProperSupersetOf(key.Columns))
									ProcessKey(reference, key.Columns);
							}
					}
					
					// All columns still appearing with the reference should not appear in the rest of the interface
					foreach (ElaboratedTableVarColumn referenceColumn in reference.Columns)
						if (!referenceColumn.IsMaster)
							ElaboratedExpression.RootExpression.Columns[Schema.Object.Qualify(referenceColumn.Column.Name, ElaboratedName)].Visible = false;
						
					if (reference.IsEmbedded)
					{
						// insert all columns
						_elaboratedExpression.PushGroup(referenceLookupGroupName);
						try
						{
							int saveColumnIndex = localColumnIndex;
							reference.TargetElaboratedTableVar.InsertColumns(ref localColumnIndex);
							if (columnIndex >= saveColumnIndex)
								columnIndex += localColumnIndex - saveColumnIndex;
						}
						finally
						{
							_elaboratedExpression.PopGroup();
						}
					}
				}
			}
		}
				
		public void AddColumns()
		{
			int columnIndex = ElaboratedExpression.RootExpression.Columns.Count;
			InsertColumns(ref columnIndex);
		}
		
		public static bool HasContributingColumns(ElaboratedTableVar tableVar)
		{
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if ((reference.ReferenceType == ReferenceType.Lookup) && HasContributingColumns(reference.TargetElaboratedTableVar))
					return true;
					
			return (tableVar.ElaboratedReference == null) || (tableVar.ElaboratedReference.ReferenceType != ReferenceType.Lookup) || (tableVar.ColumnNames.Count > tableVar.ElaboratedReference.Reference.SourceKey.Columns.Count);
		}
		
		public void InsertColumns(ref int columnIndex)
		{
			// Do not add any columns if this is a lookup reference that will not add any information to the expression
			if (!HasContributingColumns(this))
				return;
				
			string groupName;

			// Add parent columns
			foreach (ElaboratedReference reference in _elaboratedReferences)
				if ((reference.ReferenceType == ReferenceType.Parent) && reference.IsEmbedded)
				{
					// If the reference has a group tag specified, ensure the group and push it on the group stack
					groupName = DerivationUtility.GetTag(reference.Reference.MetaData, "Group", _pageType, reference.ReferenceType.ToString(), String.Empty);
					if (groupName != String.Empty)
						ElaboratedExpression.PushGroup(ElaboratedExpression.RootExpression.EnsureGroups(ElaboratedExpression.CombineGroups(ElaboratedExpression.CurrentGroupName(), groupName), groupName, reference.GroupTitle, this, reference));
					try
					{
						reference.TargetElaboratedTableVar.InsertColumns(ref columnIndex);
					}
					finally
					{
						if (groupName != String.Empty)
							ElaboratedExpression.PopGroup();
					}
				}
					
			InternalInsertColumns(ref columnIndex);
				
			// Add extension columns
			foreach (ElaboratedReference reference in _elaboratedReferences)
				if ((reference.ReferenceType == ReferenceType.Extension) && reference.IsEmbedded)
				{
					// If the reference has a group tag specified, ensure the group and push it on the group stack
					groupName = DerivationUtility.GetTag(reference.Reference.MetaData, "Group", _pageType, reference.ReferenceType.ToString(), String.Empty);
					if (groupName != String.Empty)
						ElaboratedExpression.PushGroup(ElaboratedExpression.RootExpression.EnsureGroups(ElaboratedExpression.CombineGroups(ElaboratedExpression.CurrentGroupName(), groupName), groupName, reference.GroupTitle, this, reference));
					try
					{
						reference.SourceElaboratedTableVar.InsertColumns(ref columnIndex);
					}
					finally
					{
						if (groupName != String.Empty)
							ElaboratedExpression.PopGroup();
					}
				}
		}
		
		private static void ProcessModifierTags(ElaboratedReference reference, JoinExpression joinExpression)
		{
			MetaData modifiers = 
				DerivationUtility.ExtractTags
				(
					reference.Reference.MetaData, 
					"Modifier", 						
					((reference.ReferenceType == ReferenceType.Lookup) || (reference.ReferenceType == ReferenceType.Parent)) 
						? reference.SourceElaboratedTableVar.PageType 
						: reference.TargetElaboratedTableVar.PageType,
					reference.ReferenceType.ToString()
				);

			#if USEHASHTABLEFORTAGS
			foreach (Tag tag in modifiers.Tags)
			{
			#else
			Tag tag;
			for (int index = 0; index < modifiers.Tags.Count; index++)
			{
				tag = modifiers.Tags[index];
			#endif
				if (joinExpression.Modifiers == null)
					joinExpression.Modifiers = new LanguageModifiers();
				joinExpression.Modifiers.Add(new LanguageModifier(Schema.Object.Unqualify(tag.Name), tag.Value));
			}
		}

		public Expression Expression
		{
			get
			{
				// Build the base expression (projecting over columnnames if necessary)				
				Expression expression = new Parser().ParseExpression(_query);
				if (ColumnNames.Count < _tableVar.DataType.Columns.Count)
				{
					ProjectExpression projectExpression = new ProjectExpression();
					projectExpression.Expression = expression;
					foreach (string stringValue in ColumnNames)
						projectExpression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(stringValue)));
					expression = projectExpression;
				}
				expression = new RenameAllExpression(expression, _elaboratedName);
				
				// Join all lookup references
				foreach (ElaboratedReference reference in _elaboratedReferences)
					if ((reference.ReferenceType == ReferenceType.Lookup) && reference.IsEmbedded && HasContributingColumns(reference.TargetElaboratedTableVar)) //(LReference.TargetElaboratedTableVar.ColumnNames.Count > LReference.Reference.TargetKey.Columns.Count)) // LReference.TargetElaboratedTableVar.TableVar.Columns.IsProperSupersetOf(LReference.Reference.TargetKey.Columns))
					{
						LeftOuterJoinExpression joinExpression = new LeftOuterJoinExpression();
						joinExpression.IsLookup = true;
						if (reference.IsDetailLookup)
						{
							joinExpression.Modifiers = new LanguageModifiers();
							joinExpression.Modifiers.Add(new LanguageModifier("IsDetailLookup", "True"));
						}
						ProcessModifierTags(reference, joinExpression);
						joinExpression.LeftExpression = expression;
						joinExpression.RightExpression = reference.TargetElaboratedTableVar.Expression;
						joinExpression.Condition = reference.JoinExpression;
						expression = joinExpression;
					}

				// Join all the extension references
				foreach (ElaboratedReference reference in _elaboratedReferences)
					if ((reference.ReferenceType == ReferenceType.Extension) && reference.IsEmbedded)
					{
						reference.SourceElaboratedTableVar.IsEmbedded = true;
						LeftOuterJoinExpression joinExpression = new LeftOuterJoinExpression();
						ProcessModifierTags(reference, joinExpression);
						joinExpression.LeftExpression = expression;
						joinExpression.RightExpression = reference.SourceElaboratedTableVar.Expression;
						joinExpression.Condition = reference.JoinExpression;
						joinExpression.RowExistsColumn = new IncludeColumnExpression();
						joinExpression.RowExistsColumn.ColumnAlias = Schema.Object.Qualify("RowExists", reference.SourceElaboratedTableVar.ElaboratedName);
						expression = joinExpression;
					}

				// Join all parent references
				foreach (ElaboratedReference reference in _elaboratedReferences)
					if ((reference.ReferenceType == ReferenceType.Parent) && reference.IsEmbedded)
					{
						reference.TargetElaboratedTableVar.IsEmbedded = true;
						InnerJoinExpression joinExpression = new InnerJoinExpression();
						ProcessModifierTags(reference, joinExpression);
						joinExpression.LeftExpression = reference.TargetElaboratedTableVar.Expression;
						joinExpression.RightExpression = expression;
						joinExpression.Condition = reference.JoinExpression;
						expression = joinExpression;
					}
					
				return expression;
			}
		}
	}
	
	public class ElaboratedTableVarColumn : Object
	{
		public ElaboratedTableVarColumn(ElaboratedTableVar table, Schema.TableVarColumn column, bool visible, string groupName) : base()
		{
			_elaboratedTableVar = table;
			_column = column;
			_visible = visible;
			_elaboratedName = Schema.Object.Qualify(column.Name, _elaboratedTableVar.ElaboratedName);
			_groupName = groupName;
		}
		
		public ElaboratedTableVarColumn(ElaboratedTableVar table, ElaboratedReference reference, Schema.TableVarColumn column, bool visible, bool readOnly, string groupName) : base()
		{
			_elaboratedTableVar = table;
			_elaboratedReference = reference;
			_column = column;
			_visible = visible;
			_readOnly = readOnly;
			if (reference == null)
				_elaboratedName = Schema.Object.Qualify(column.Name, _elaboratedTableVar.ElaboratedName);
			else
				_elaboratedName = String.Format("{0}_{1}.{2}", _elaboratedReference.Reference.OriginatingReferenceName(), _elaboratedTableVar.ElaboratedName, column.Name);
			_groupName = groupName;
		}
		
		public ElaboratedTableVarColumn(ElaboratedTableVar table, Schema.TableVarColumn column, string elaboratedName, bool visible, string groupName) : base()
		{
			_elaboratedTableVar = table;
			_column = column;
			_visible = visible;
			_elaboratedName = elaboratedName;
			_groupName = groupName;
		}
		
		// ElaboratedName
		protected string _elaboratedName = String.Empty;
		public string ElaboratedName {	get { return _elaboratedName; } }
		
		// ElaboratedTableVar
		protected ElaboratedTableVar _elaboratedTableVar;
		public ElaboratedTableVar ElaboratedTableVar
		{
			get { return _elaboratedTableVar; }
			set { _elaboratedTableVar = value; }
		}
		
		// ElaboratedReference
		protected ElaboratedReference _elaboratedReference;
		public ElaboratedReference ElaboratedReference
		{
			get { return _elaboratedReference; }
			set { _elaboratedReference = value; }
		}

		// Schema.TableVarColumn
		protected Schema.TableVarColumn _column;
		public Schema.TableVarColumn Column { get { return _column; } }
		
		// IsMaster
		protected bool _isMaster;
		public bool IsMaster
		{
			get { return _isMaster; }
			set { _isMaster = value; }
		}
		
		// Visible
		protected bool _visible;
		public bool Visible
		{
			get { return _visible; }
			set { _visible = value; }
		}
		
		// ReadOnly
		protected bool _readOnly;
		public bool ReadOnly
		{
			get { return _readOnly; }
			set { _readOnly = value; }
		}
		
		// GroupName
		protected string _groupName = String.Empty;
		public string GroupName 
		{ 
			get { return _groupName; } 
			set { _groupName = value == null ? String.Empty : value; }
		}

		// TitleSeed
		public string GetTitleSeed()
		{
			StringBuilder titleSeed = new StringBuilder();
			ElaboratedReference reference = _elaboratedTableVar.ElaboratedReference;
			while (reference != null)
			{
				if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, "IncludeGroupTitle", "True")))
					titleSeed.Insert(0, String.Format("{0} ", reference.GroupTitle));
				else
					break;

				if ((reference.ReferenceType == ReferenceType.Parent) || (reference.ReferenceType == ReferenceType.Lookup))
					reference = reference.SourceElaboratedTableVar.ElaboratedReference;
				else
					reference = reference.TargetElaboratedTableVar.ElaboratedReference;
			}
			return titleSeed.ToString();
		}
	}
	
	#if USETYPEDLIST
	public class ElaboratedTableVarColumns : TypedList
	{
		public ElaboratedTableVarColumns() : base(typeof(ElaboratedTableVarColumn)){}
		
		public new ElaboratedTableVarColumn this[int AIndex]
		{
			get { return (ElaboratedTableVarColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
	#else
	public class ElaboratedTableVarColumns : BaseList<ElaboratedTableVarColumn>
	{
	#endif
		public ElaboratedTableVarColumn this[string columnName] 
		{ 
			get 
			{ 
				int index = IndexOf(columnName);
				if (index < 0)
					throw new ServerException(ServerException.Codes.ColumnNotFound, columnName);
				return this[index]; 
			} 
		}
		
		public int IndexOf(string columnName)
		{
			for (int index = 0; index < Count; index++)
				if (String.Compare(columnName, this[index].ElaboratedName) == 0)
					return index;
			return -1;
		}
		
		public bool Contains(string columnName)
		{
			return IndexOf(columnName) >= 0;
		}
	}
	
	public class ElaboratedReference : Object
	{
		public ElaboratedReference
		(
			ElaboratedExpression expression, 
			Schema.ReferenceBase reference, 
			ReferenceType referenceType, 
			ElaboratedTableVar sourceTable, 
			ElaboratedTableVar targetTable
		) : base()
		{
			_elaboratedExpression = expression;
			_reference = reference;
			_referenceType = referenceType;
			_referenceTitle = 
				DerivationUtility.GetTag
				(
					_reference.MetaData,
					"Title",
					((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? targetTable.PageType : sourceTable.PageType,
					_referenceType.ToString(),
					((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? targetTable.TableTitle : sourceTable.TableTitle
				);
				
			_priority = 
				Convert.ToInt32
				(
					DerivationUtility.GetTag
					(
						_reference.MetaData, 
						"Priority", 
						((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? targetTable.PageType : sourceTable.PageType,
						_referenceType.ToString(),
						DerivationUtility.DefaultPriority
					)
				);

			_sourceElaboratedTableVar = sourceTable;
			_targetElaboratedTableVar = targetTable;

			if ((_referenceType == ReferenceType.Extension) && (sourceTable.PageType == DerivationUtility.Preview))
			{
				_isEmbedded =
					Convert.ToBoolean
					(
						DerivationUtility.GetExplicitPageTag
						(
							_reference.MetaData,
							"Embedded",
							sourceTable.PageType,
							_referenceType.ToString(),
							"False"
						)
					);
			}
			else
			{
				_isEmbedded = 
					Convert.ToBoolean
					(
						DerivationUtility.GetTag
						(
							_reference.MetaData, 
							"Embedded",
							((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? sourceTable.PageType : targetTable.PageType,
							_referenceType.ToString(),
							((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? "True" : "False"
						)
					);
			}
				
			StringBuilder name = new StringBuilder();
			if ((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent))
			{
				for (int index = 0; index < _reference.SourceKey.Columns.Count; index++)
				{
					if (name.Length > 0)
						name.Append(Keywords.Qualifier);
					name.Append(Schema.Object.Qualify(_reference.SourceKey.Columns[index].Name, _sourceElaboratedTableVar.ElaboratedName));
				}
			}
			else
			{
				for (int index = 0; index < _reference.TargetKey.Columns.Count; index++)
				{
					if (name.Length > 0)
						name.Append(Keywords.Qualifier);
					name.Append(Schema.Object.Qualify(_reference.TargetKey.Columns[index].Name, _targetElaboratedTableVar.ElaboratedName));
				}
			}
			if (name.Length > 0)
				name.Append(Keywords.Qualifier);
			name.Append(_reference.OriginatingReferenceName());
			_elaboratedName = expression.AddReferenceName(name.ToString());
			
			if (_reference.MetaData != null)
				_groupMetaData = 
					DerivationUtility.ExtractTags
					(
						_reference.MetaData, 
						"Group", 
						((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? 
							sourceTable.PageType :
							targetTable.PageType, 
						_referenceType.ToString()
					);
					
			_groupTitle = 
				DerivationUtility.GetTag
				(
					_groupMetaData, 
					"Title", 
					((_referenceType == ReferenceType.Lookup) || (_referenceType == ReferenceType.Parent)) ? 
						sourceTable.PageType :
						targetTable.PageType, 
					_referenceType.ToString(),
					_referenceTitle
				);
		}
		
		// ElaboratedExpression
		protected ElaboratedExpression _elaboratedExpression;
		public ElaboratedExpression ElaboratedExpression { get { return _elaboratedExpression; } }
		
		// Reference
		protected Schema.ReferenceBase _reference;
		public Schema.ReferenceBase Reference { get { return _reference; } }

		// Columns		
		protected ElaboratedTableVarColumns _columns = new ElaboratedTableVarColumns();
		public ElaboratedTableVarColumns Columns { get { return _columns; } }
		
		// Priority
		protected int _priority;
		public int Priority { get { return _priority; } }
		
		// ReferenceType
		protected ReferenceType _referenceType;
		public ReferenceType ReferenceType { get { return _referenceType; } }
		
		// ReferenceTitle
		protected string _referenceTitle;
		public string ReferenceTitle { get { return _referenceTitle; } }
		
		// GroupTitle
		protected string _groupTitle;
		public string GroupTitle { get { return _groupTitle; } }
		
		protected MetaData _groupMetaData;
		public MetaData GroupMetaData { get { return _groupMetaData; } }
		
		// ElaboratedName
		protected string _elaboratedName;
		public string ElaboratedName { get { return _elaboratedName; } }
		
		// SourceElaboratedTableVar
		protected ElaboratedTableVar _sourceElaboratedTableVar;
		public ElaboratedTableVar SourceElaboratedTableVar { get { return _sourceElaboratedTableVar; } }
		
		// TargetElaboratedTableVar
		protected ElaboratedTableVar _targetElaboratedTableVar;
		public ElaboratedTableVar TargetElaboratedTableVar { get { return _targetElaboratedTableVar; } }
		
		// IsEmbedded
		protected bool _isEmbedded;
		public bool IsEmbedded { get { return _isEmbedded; } }
		
		// IsDetailLookup
		protected bool _isDetailLookup;
		public bool IsDetailLookup 
		{ 
			get { return _isDetailLookup; } 
			set { _isDetailLookup = value; }
		}
		
		// JoinExpression
		public Expression JoinExpression
		{
			get
			{
				Expression joinCondition = null;
				BinaryExpression columnExpression;
				for (int index = 0; index < _reference.SourceKey.Columns.Count; index++)
				{
					columnExpression = 
						new BinaryExpression
						(
							new IdentifierExpression
							(
								Schema.Object.EnsureRooted
								(
									Schema.Object.Qualify
									(
										_reference.SourceKey.Columns[index].Name, 
										_sourceElaboratedTableVar.ElaboratedName
									)
								)
							),
							Instructions.Equal,
							new IdentifierExpression
							(
								Schema.Object.EnsureRooted
								(
									Schema.Object.Qualify
									(
										_reference.TargetKey.Columns[index].Name,
										_targetElaboratedTableVar.ElaboratedName
									)
								)
							)
						);
						
					if (joinCondition != null)
						joinCondition =
							new BinaryExpression
							(
								joinCondition,
								Instructions.And,
								columnExpression
							);
					else
						joinCondition = columnExpression;
				}
				
				return joinCondition;
			}
		}
	}
	
	public class ElaboratedReferences : System.Object, IEnumerable
	{
		private const int DefaultCapacity = 4;
		private const int DefaultGrowth = 4;
	
		public ElaboratedReferences() : base()
		{
			_elaboratedReferences = new ElaboratedReference[DefaultCapacity];
		}

		private int _count;		
		public int Count { get { return _count; } }

		private ElaboratedReference[] _elaboratedReferences;
		public ElaboratedReference this[int index] 
		{ 
			get 
			{
				if ((index < 0) || index >= _count)
					throw new IndexOutOfRangeException();
				return _elaboratedReferences[index]; 
			} 
		}
		
		private bool _orderByPriorityOnly = false;
		/// <summary>Determines whether to order the list of references by priority only, or by number of columns, then priority.</summary>
		/// <remarks>
		/// <para>By default, the list of references for a table var is ordered by number of columns, then priority. This ordering is in
		/// place to control the order in which references are processed during elaboration, because the algorithm for determining
		/// which columns should be displayed for each reference requires that the references be processed in column-count order to
		/// allow references that include columns that other references include to have first option to display the column.</para>
		/// <para>Setting this option will reorder the list.</para>
		/// </remarks>
		public bool OrderByPriorityOnly
		{
			get { return _orderByPriorityOnly; }
			set 
			{ 
				if (_orderByPriorityOnly != value)
				{
					_orderByPriorityOnly = value; 
					if (_count > 0)
					{
						ElaboratedReference[] references = _elaboratedReferences;
						_elaboratedReferences = new ElaboratedReference[references.Length];
						_count = 0;
						for (int index = 0; index < references.Length; index++)
							Add(references[index]);
					}
				}
			}
		}
		
		public void Add(ElaboratedReference elaboratedReference)
		{
			EnsureSize(_count + 1);
			if (_orderByPriorityOnly)
				for (int index = 0; index < _count; index++)
				{
					if (elaboratedReference.Priority < _elaboratedReferences[index].Priority)
					{
						InsertAt(elaboratedReference, index);
						return;
					}
				}
			else
				for (int index = 0; index < _count; index++)
				{
					if 
					(
						(
							(elaboratedReference.Reference.SourceKey.Columns.Count == _elaboratedReferences[index].Reference.SourceKey.Columns.Count) && 
							(elaboratedReference.Priority < _elaboratedReferences[index].Priority)
						) || 
						(elaboratedReference.Reference.SourceKey.Columns.Count < _elaboratedReferences[index].Reference.SourceKey.Columns.Count)
					)
					{
						InsertAt(elaboratedReference, index);
						return;
					}
				}

			InsertAt(elaboratedReference, _count);
		}
		
		public void Remove(ElaboratedReference elaboratedReference)
		{
			RemoveAt(IndexOf(elaboratedReference));
		}
		
		public int IndexOf(ElaboratedReference elaboratedReference)
		{
			for (int index = 0; index < _count; index++)
				if (Object.ReferenceEquals(_elaboratedReferences[index], elaboratedReference))
					return index;
			return -1;
		}
		
		public bool Contains(ElaboratedReference elaboratedReference)
		{
			return IndexOf(elaboratedReference) >= 0;
		}
		
		private void InsertAt(ElaboratedReference elaboratedReference, int index)
		{
			Array.Copy(_elaboratedReferences, index, _elaboratedReferences, index + 1, _count - index);
			_elaboratedReferences[index] = elaboratedReference;
			_count++;
		}
		
		private void RemoveAt(int index)
		{
			Array.Copy(_elaboratedReferences, index + 1, _elaboratedReferences, index, _count - index);
			_count--;
			_elaboratedReferences[_count] = null;
		}
		
		private void EnsureSize(int count)
		{
			if (_elaboratedReferences.Length < count)
			{
				ElaboratedReference[] newElaboratedReferences = new ElaboratedReference[_elaboratedReferences.Length + DefaultGrowth];
				Array.Copy(_elaboratedReferences, newElaboratedReferences, _count);
				_elaboratedReferences = newElaboratedReferences;
			}
		}

        // IEnumerable interface
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        ElaboratedReferenceEnumerator GetEnumerator()
        {
			return new ElaboratedReferenceEnumerator(this);
        }

		public class ElaboratedReferenceEnumerator : IEnumerator
        {
            public ElaboratedReferenceEnumerator(ElaboratedReferences elaboratedReferences) : base()
            {
                _elaboratedReferences = elaboratedReferences;
            }
            
            private int _current = -1;
            private ElaboratedReferences _elaboratedReferences;

            public object Current { get { return _elaboratedReferences[_current]; } }

            public void Reset()
            {
                _current = -1;
            }

            public bool MoveNext()
            {
				_current++;
				return _current < _elaboratedReferences.Count;
            }
        }
	}
}

