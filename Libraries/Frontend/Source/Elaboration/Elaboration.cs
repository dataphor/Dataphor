#define ORDERREFERENCESBYPRIORITYONLY

/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Schema = Alphora.Dataphor.DAE.Schema;
using Frontend = Alphora.Dataphor.Frontend;
using Alphora.Dataphor.Frontend.Server.Derivation;

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
			ServerProcess AProcess,
			string AQuery,
			bool AElaborate,
			Schema.TableVar ATableVar,
			Schema.Catalog ACatalog,
			string[] ADetailKeys,
			string AMainElaboratedTableName,
			string APageType
		)
		{
			FProcess = AProcess;
			FPageType = APageType;
			FDetailKeys = ADetailKeys;
			FElaborate = AElaborate;
			FMainElaboratedTableVar = new ElaboratedTableVar(this, ATableVar, FPageType, AddTableName(AMainElaboratedTableName), AQuery);

			// The invariant is the dequlified detail key
			FInvariant = new Schema.Key();
			for (int LIndex = 0; LIndex < FDetailKeys.Length; LIndex++)
				FInvariant.Columns.Add(FMainElaboratedTableVar.TableVar.Columns[Schema.Object.Dequalify(FDetailKeys[LIndex])]);

			if (AElaborate)
			{
				BuildParentReferences(FMainElaboratedTableVar);
				BuildExtensionReferences(FMainElaboratedTableVar);
				BuildLookupReferences(ACatalog, FMainElaboratedTableVar);
				BuildDetailReferences(FMainElaboratedTableVar);
			}
		}

		// Subsequent constructor used to start nested derivation of a lookup expression for a given table variable
		protected ElaboratedExpression
		(
			ServerProcess AProcess,
			ElaboratedExpression AParentExpression,
			bool AElaborate,
			Schema.Catalog ACatalog,
			string ATableName,
			string[] ADetailKeys,
			string AMainElaboratedTableName,
			string APageType
		)
		{
			FProcess = AProcess;
			FPageType = APageType;
			FDetailKeys = ADetailKeys;
			FElaborate = AElaborate;
			FParentExpression = AParentExpression;
			FMainElaboratedTableVar = new ElaboratedTableVar(this, (Schema.TableVar)ACatalog.Objects[ATableName], FPageType, AMainElaboratedTableName);
			if (AElaborate)
			{
				BuildParentReferences(FMainElaboratedTableVar);
				BuildExtensionReferences(FMainElaboratedTableVar);
				BuildLookupReferences(ACatalog, FMainElaboratedTableVar);
			}
		}

		public static string[] QualifyNames(string[] ANames, string ANameSpace)
		{
			string[] LResult = new string[ANames.Length];
			for (int LIndex = 0; LIndex < ANames.Length; LIndex++)
				LResult[LIndex] = Schema.Object.Qualify(ANames[LIndex], ANameSpace);
			return LResult;
		}
		
		public static string[] DequalifyNames(string[] ANames)
		{
			string[] LResult = new string[ANames.Length];
			for (int LIndex = 0; LIndex < ANames.Length; LIndex++)
				LResult[LIndex] = Schema.Object.Dequalify(ANames[LIndex]);
			return LResult;
		}
		
		public static string CombineGroups(string AOuterGroup, string AInnerGroup)
		{
			if (AOuterGroup != String.Empty)
				if (AInnerGroup != String.Empty)
					return String.Format("{0}\\{1}", AOuterGroup, AInnerGroup);
				else
					return AOuterGroup;
			else
				return AInnerGroup;
		}
		
		protected ElaboratedExpression FParentExpression;
		public ElaboratedExpression RootExpression { get { return (FParentExpression == null) ? this : FParentExpression.RootExpression; } }
		
		protected bool FElaborate;
		public bool Elaborate { get { return FElaborate; } }
		
		protected ServerProcess FProcess;
		public ServerProcess Process { get { return FProcess; } }
		
		/*
			An inclusion reference is a reference that should not be followed for the purpose of elaboration, or included for the purpose of presentation,
			because it is the reference that was used to arrive at this expression.
			
			In the case that this is a nested expression, the inclusion reference is simply the reference that was used to arrive at this expression;
			otherwise any reference that terminates as a subset of the detail columns is an inclusion reference.
		*/		
		protected bool IsInclusionReference(Schema.Reference AReference)
		{
			if (FParentExpression == null)
				return
					(
						AReference.TargetTable.Equals(MainElaboratedTableVar.TableVar) &&
						AReference.TargetKey.Columns.IsSubsetOf(Invariant.Columns)
					) ||
					(
						AReference.SourceTable.Equals(MainElaboratedTableVar.TableVar) &&
						AReference.SourceKey.Columns.IsSubsetOf(Invariant.Columns)
					);
			else
				return (MainElaboratedTableVar.ElaboratedReference != null) && (MainElaboratedTableVar.ElaboratedReference.Reference.OriginatingReferenceName() == AReference.OriginatingReferenceName());
		}
				
		protected virtual bool IsCircularReference(ElaboratedTableVar ATableVar, Schema.Reference AReference)
		{
			// A reference is circular if the source tablevar = target tablevar of the atablevar.derivedreference
			return
				(ATableVar.ElaboratedReference != null) &&
				(
					(
						AReference.SourceTable.Equals(ATableVar.ElaboratedReference.Reference.TargetTable) &&
						AReference.SourceKey.Equals(ATableVar.ElaboratedReference.Reference.TargetKey)
					) ||
					(
						AReference.TargetTable.Equals(ATableVar.ElaboratedReference.Reference.SourceTable) &&
						AReference.TargetKey.Equals(ATableVar.ElaboratedReference.Reference.SourceKey)
					) ||
					(
						AReference.SourceTable.Equals(ATableVar.ElaboratedReference.Reference.SourceTable) &&
						AReference.SourceKey.Equals(ATableVar.ElaboratedReference.Reference.SourceKey)
					) ||
					(
						AReference.TargetTable.Equals(ATableVar.ElaboratedReference.Reference.TargetTable) &&
						AReference.TargetKey.Equals(ATableVar.ElaboratedReference.Reference.TargetKey)
					)
				);
		}
		
		protected virtual bool IsIncludedReference(ElaboratedTableVar ATableVar, Schema.Reference AReference, ReferenceType AReferenceType)
		{
			// A reference is included if all the columns in the reference key are included in the table variable
			foreach (Schema.TableVarColumn LColumn in ((AReferenceType == ReferenceType.Parent) || (AReferenceType == ReferenceType.Lookup)) ? AReference.SourceKey.Columns : AReference.TargetKey.Columns)
				if (!ATableVar.ColumnNames.Contains(LColumn.Name))
					return false;
			
			return Convert.ToBoolean(DerivationUtility.GetTag(AReference.MetaData, "Include", FPageType, AReferenceType.ToString(), FElaborate ? "True" : "False"));
		}
		
		protected bool FTreatParentAsLookup = true;
		
		protected virtual bool ShouldTreatParentAsLookup(Schema.Reference AReference)
		{
			return Convert.ToBoolean(DerivationUtility.GetTag(AReference.MetaData, "TreatAsLookup", FPageType, ReferenceType.Parent.ToString(), FTreatParentAsLookup.ToString()));
		}
		
		protected virtual void BuildParentReferences(ElaboratedTableVar ATable)
		{
			foreach (Schema.Reference LReference in ATable.TableVar.SourceReferences)
				if (LReference.SourceKey.IsUnique && !LReference.IsExcluded && FProcess.Plan.HasRight(LReference.TargetTable.GetRight(Schema.RightNames.Select)) && !ShouldTreatParentAsLookup(LReference) && !IsInclusionReference(LReference) && !IsCircularReference(ATable, LReference) && IsIncludedReference(ATable, LReference, ReferenceType.Parent))
				{
					ElaboratedReference LElaboratedReference = 
						new ElaboratedReference
						(
							this, 
							LReference, 
							ReferenceType.Parent, 
							ATable, 
							new ElaboratedTableVar(this, LReference.TargetTable, DerivationUtility.CView)
						);
					ATable.ElaboratedReferences.Add(LElaboratedReference);
					LElaboratedReference.TargetElaboratedTableVar.ElaboratedReference = LElaboratedReference;
					BuildParentReferences(LElaboratedReference.TargetElaboratedTableVar);
				}
		}
		
		protected virtual void BuildExtensionReferences(ElaboratedTableVar ATable)
		{
			foreach (Schema.Reference LReference in ATable.TableVar.TargetReferences)
				if (LReference.SourceKey.IsUnique && !LReference.IsExcluded && FProcess.Plan.HasRight(LReference.SourceTable.GetRight(Schema.RightNames.Select)) && !IsInclusionReference(LReference) && !IsCircularReference(ATable, LReference) && IsIncludedReference(ATable, LReference, ReferenceType.Extension))
				{
					ElaboratedReference LElaboratedReference =
						new ElaboratedReference
						(
							this,
							LReference,
							ReferenceType.Extension,
							new ElaboratedTableVar(this, LReference.SourceTable, DerivationUtility.IsReadOnlyPageType(FPageType) ? DerivationUtility.CView : DerivationUtility.CEdit),
							ATable
						);
					LElaboratedReference.SourceElaboratedTableVar.ElaboratedReference = LElaboratedReference;
					ATable.ElaboratedReferences.Add(LElaboratedReference);
				}
		}
		
		protected virtual void BuildLookupReferences(Schema.Catalog ACatalog, ElaboratedTableVar ATable)
		{
			foreach (Schema.Reference LReference in ATable.TableVar.SourceReferences)
				if ((ShouldTreatParentAsLookup(LReference) || !LReference.SourceKey.IsUnique) && !LReference.IsExcluded && FProcess.Plan.HasRight(LReference.TargetTable.GetRight(Schema.RightNames.Select)) && !IsInclusionReference(LReference) && !IsCircularReference(ATable, LReference) && IsIncludedReference(ATable, LReference, ReferenceType.Lookup))
				{
					string LElaboratedName = AddTableName(LReference.TargetTable.Name);
					ElaboratedExpression LLookupExpression = 
						new ElaboratedExpression
						(
							FProcess,
							this, 
							Convert.ToBoolean(DerivationUtility.GetTag(LReference.MetaData, "Elaborate", DerivationUtility.CPreview, ReferenceType.Lookup.ToString(), DerivationUtility.GetTag(LReference.TargetTable.MetaData, "Elaborate", DerivationUtility.CPreview, "False"))),
							ACatalog, 
							LReference.TargetTable.Name, 
							QualifyNames(LReference.TargetKey.Columns.ColumnNames, LElaboratedName),
							LElaboratedName,
							DerivationUtility.CPreview
						);

					ElaboratedReference LElaboratedReference =
						new ElaboratedReference
						(
							this,
							LReference,
							ReferenceType.Lookup,
							ATable,
							LLookupExpression.MainElaboratedTableVar
						);

					LElaboratedReference.TargetElaboratedTableVar.ElaboratedReference = LElaboratedReference;
					ATable.ElaboratedReferences.Add(LElaboratedReference);
				}

			foreach (ElaboratedReference LReference in ATable.ElaboratedReferences)
				if (LReference.ReferenceType == ReferenceType.Parent)
					BuildLookupReferences(ACatalog, LReference.TargetElaboratedTableVar);
				else if (LReference.ReferenceType == ReferenceType.Extension)
					BuildLookupReferences(ACatalog, LReference.SourceElaboratedTableVar);
		}
		
		protected virtual void BuildDetailReferences(ElaboratedTableVar ATable)
		{
			foreach (Schema.Reference LReference in ATable.TableVar.TargetReferences)
				if (!LReference.SourceKey.IsUnique && !LReference.IsExcluded && FProcess.Plan.HasRight(LReference.SourceTable.GetRight(Schema.RightNames.Select)) && !IsInclusionReference(LReference) && IsIncludedReference(ATable, LReference, ReferenceType.Detail))
				{
					ElaboratedReference LElaboratedReference =
						new ElaboratedReference
						(
							this,
							LReference,
							ReferenceType.Detail,
							new ElaboratedTableVar(this, LReference.SourceTable, DerivationUtility.IsSingularPageType(FPageType) ? (DerivationUtility.IsReadOnlyPageType(FPageType) ? DerivationUtility.CList : DerivationUtility.CBrowse) : FPageType),
							ATable
						);
					LElaboratedReference.SourceElaboratedTableVar.ElaboratedReference = LElaboratedReference;
					ATable.ElaboratedReferences.Add(LElaboratedReference);
				}
			
			foreach (ElaboratedReference LReference in ATable.ElaboratedReferences)
				if (LReference.ReferenceType == ReferenceType.Parent)
					BuildDetailReferences(LReference.TargetElaboratedTableVar);
				else if (LReference.ReferenceType == ReferenceType.Extension)
					BuildDetailReferences(LReference.SourceElaboratedTableVar);
		}
		
		// Stores the names of all tables in the expression to ensure uniqueness is maintained
		protected StringCollection FTableNames = new StringCollection();
		
		protected string InternalAddTableName(string ATableName)
		{
			string LResult = Schema.Object.Unqualify(ATableName);
			if (FTableNames.Contains(LResult))
			{
				int LCounter = 0;
				do
				{
					LCounter++;
				} while (FTableNames.Contains(String.Format("{0}{1}", LResult, LCounter.ToString())));
				LResult = String.Format("{0}{1}", LResult, LCounter.ToString());
			}
			FTableNames.Add(LResult);
			return LResult;
		}

		protected internal string AddTableName(string ATableName)
		{
			return RootExpression.InternalAddTableName(ATableName);
		}
		
		// Stores the names of all references in the expression to ensure uniqueness is maintained
		// Note that the derived reference names could be used for this purpose, but this would
		// break backwards compatibility with forms and customizations created before the derived
		// reference name was introduced (#23692).
		protected StringCollection FReferenceNames = new StringCollection();
		
		protected string InternalAddReferenceName(string AReferenceName)
		{
			string LResult = AReferenceName;
			if (FReferenceNames.Contains(LResult))
			{
				int LCounter = 0; 
				do
				{
					LCounter++;
				} while (FReferenceNames.Contains(String.Format("{0}{1}", LResult, LCounter.ToString())));
				LResult = String.Format("{0}{1}", LResult, LCounter.ToString());
			}
			FReferenceNames.Add(LResult);
			return LResult;
		}
		
		protected internal string AddReferenceName(string AReferenceName)
		{
			return RootExpression.InternalAddReferenceName(AReferenceName);
		}
		
		// AGroupName must always be a qualification of AUnqualifiedGroupName (e.g. AGroupName = "Address\City", AUnqualifiedGroupName = "City")
		// It is assumed that groups already exist for the qualifier portions of AGroupName, in other words, this procedure only ensures that
		// groups exist for the groups specified in AUnqualifiedGroupName
		public ElaboratedGroup EnsureGroups(string AGroupName, string AUnqualifiedGroupName, string AGroupTitle, ElaboratedTableVar ATableVar, ElaboratedReference AReference)
		{
			string LQualifier = AGroupName;
			if (AUnqualifiedGroupName != String.Empty)
			{
				int LUnqualifiedIndex = AGroupName.LastIndexOf(AUnqualifiedGroupName);
				if (LUnqualifiedIndex != (AGroupName.Length - AUnqualifiedGroupName.Length))
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.InvalidGrouping, AUnqualifiedGroupName, AGroupName);
				
				LQualifier = AGroupName.Substring(0, LUnqualifiedIndex);
			}
				
			string[] LUnqualifiedGroupNames = AUnqualifiedGroupName.Split('\\');
			
			StringBuilder LUnqualifiedGroupName = new StringBuilder();
			ElaboratedGroup LGroup = null;
			for (int LIndex = 0; LIndex < LUnqualifiedGroupNames.Length; LIndex++)
			{
				if (LUnqualifiedGroupName.Length > 0)
					LUnqualifiedGroupName.Append("\\");
				LUnqualifiedGroupName.Append(LUnqualifiedGroupNames[LIndex]);
				LGroup =
					EnsureGroup
					(
						String.Format
						(
							"{0}{1}", 
							LQualifier, 
							LUnqualifiedGroupName.ToString()
						), 
						LUnqualifiedGroupName.ToString(), 
						(LIndex < (LUnqualifiedGroupNames.Length - 1)) ? String.Empty : AGroupTitle,	// only use title for innermost group
						ATableVar,
						AReference
					);
			}
			return LGroup;
		}
		
		public ElaboratedGroup EnsureGroup(string AGroupName, string AUnqualifiedGroupName, string AGroupTitle, ElaboratedTableVar ATableVar, ElaboratedReference AReference)
		{
			int LGroupIndex = FGroups.IndexOf(AGroupName);
			if (LGroupIndex >= 0)
				return FGroups[LGroupIndex];
			return AddGroup(AGroupName, AUnqualifiedGroupName, AGroupTitle, ATableVar, AReference);
		}
		
		public ElaboratedGroup AddGroup(string AGroupName, string AUnqualifiedGroupName, string AGroupTitle, ElaboratedTableVar ATableVar, ElaboratedReference AReference)
		{
			ElaboratedGroup LGroup = new ElaboratedGroup(AGroupName, AUnqualifiedGroupName);

			if (AUnqualifiedGroupName != String.Empty)
			{
				if (ATableVar.TableVar.MetaData != null)
				{
					MetaData LGroupMetaData = DerivationUtility.ExtractTags(ATableVar.TableVar.MetaData.Tags, String.Format("Group.{0}", AUnqualifiedGroupName.Replace("\\", ".")), ATableVar.PageType);
					LGroup.Properties.AddOrUpdateRange(LGroupMetaData.Tags);
				}

				if ((AReference != null) && (AReference.GroupMetaData != null))
					LGroup.Properties.AddOrUpdateRange(AReference.GroupMetaData.Tags);
			}

			string LTitle = DerivationUtility.GetTag(new MetaData(LGroup.Properties), "Title", ATableVar.PageType, String.Empty);
			if ((LTitle == String.Empty) && (AGroupTitle != String.Empty))
				LGroup.Properties.AddOrUpdate("Frontend.Title", AGroupTitle);
			FGroups.Add(LGroup);
			return LGroup;
		}
		
		protected ElaboratedGroups FGroups = new ElaboratedGroups();
		public ElaboratedGroups Groups { get { return FGroups; } }
		
		// GroupStack
		protected ElaboratedGroups FGroupStack = new ElaboratedGroups();
		protected void InternalPushGroup(string AGroupName)
		{
			InternalPushGroup(new ElaboratedGroup(AGroupName));
		}
		
		protected void InternalPushGroup(ElaboratedGroup AGroup)
		{
			FGroupStack.Add(AGroup);
		}
		
		public void PushGroup(string AGroupName)
		{
			RootExpression.InternalPushGroup(AGroupName);
		}
		
		public void PushGroup(ElaboratedGroup AGroup)
		{
			RootExpression.InternalPushGroup(AGroup);
		}
		
		protected void InternalPopGroup()
		{
			FGroupStack.RemoveAt(FGroupStack.Count - 1);
		}
		
		public void PopGroup()
		{
			RootExpression.InternalPopGroup();
		}
		
		protected string InternalCurrentGroupName()
		{
			StringBuilder LGroupName = new StringBuilder();
			foreach (ElaboratedGroup LGroup in FGroupStack)
			{
				if (LGroupName.Length > 0)
					LGroupName.Append("\\");
				LGroupName.Append(LGroup.Name);
			}
			return LGroupName.ToString();
		}
		
		public string CurrentGroupName()
		{
			return RootExpression.InternalCurrentGroupName();
		}

		// PageType
		protected string FPageType;
		public string PageType { get { return FPageType; } }
		
		// DetailKeys
		protected string[] FDetailKeys;
		public string[] DetailKeys { get { return FDetailKeys; } }
		
		// Invariant
		protected Schema.Key FInvariant;
		public Schema.Key Invariant { get { return FInvariant; } }
		
		// InclusionReference
		protected Schema.Reference FInclusionReference;
		public Schema.Reference InclusionReference { get { return FInclusionReference; } }
		
		// MainElaboratedTableVar		
		protected ElaboratedTableVar FMainElaboratedTableVar;
		public ElaboratedTableVar MainElaboratedTableVar
		{
			get { return FMainElaboratedTableVar; }
			set
			{
				if (FMainElaboratedTableVar != null)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.MainTableSet);
				if (value == null)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.MainTableRequired);
				FMainElaboratedTableVar = value;
			}
		}
		
		protected ElaboratedTableVarColumns FColumns = new ElaboratedTableVarColumns();
		public ElaboratedTableVarColumns Columns { get { return FColumns; } }
		
		public Expression Expression
		{
			get
			{
				FColumns.Clear();
				FMainElaboratedTableVar.AddColumns();
				Expression LExpression = FMainElaboratedTableVar.Expression;
				return LExpression;
			}
		}
	}
	
	public class ElaboratedGroup : System.Object
	{
		public ElaboratedGroup(string AName) : base()
		{
			FName = AName;
			FUnqualifiedName = AName;
		}
		
		public ElaboratedGroup(string AName, string AUnqualifiedName) : base()
		{
			FName = AName;
			FUnqualifiedName = AUnqualifiedName;
		}

		private string FName;
		public string Name { get { return FName; } }
		
		private string FUnqualifiedName;
		public string UnqualifiedName { get { return FUnqualifiedName; } }
		
		private Tags FProperties = new Tags();
		public Tags Properties { get { return FProperties; } }
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
		public ElaboratedGroup this[string AName] { get { return this[IndexOf(AName)]; } }
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].Name == AName)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}

		public string ResolveGroupName(string AName)
		{
			if (IndexOf(AName) != -1)
				return AName;
            
			// seach for any group that ends in AName
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].Name.EndsWith(AName))
					return this[LIndex].Name;

			throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.GroupNotFound, AName);
		}
	}
	
	public class ElaboratedTableVar : Object
	{
		public ElaboratedTableVar(ElaboratedExpression AExpression, Schema.TableVar ATableVar, string APageType) : base()
		{
			FElaboratedName = AExpression.AddTableName(ATableVar.Name);
			InternalCreateElaboratedTableVar(AExpression, ATableVar, APageType, ATableVar.Name);
		}
		
		public ElaboratedTableVar(ElaboratedExpression AExpression, Schema.TableVar ATableVar, string APageType, string AElaboratedName) : base()
		{
			FElaboratedName = AElaboratedName;
			InternalCreateElaboratedTableVar(AExpression, ATableVar, APageType, ATableVar.Name);
		}
		
		public ElaboratedTableVar(ElaboratedExpression AExpression, Schema.TableVar ATableVar, string APageType, string AElaboratedName, string AQuery) : base()
		{
			FElaboratedName = AElaboratedName;
			InternalCreateElaboratedTableVar(AExpression, ATableVar, APageType, AQuery);
		}

		/// <remarks> Don't try to parse an expression with normalized white space, line comments and embedded quotes can invalidate the query. </remarks>
		private string NormalizeWhiteSpace(string AString)
		{
			StringBuilder LResult = new StringBuilder();
			bool LInWhiteSpace = false;
			for (int LIndex = 0; LIndex < AString.Length; LIndex++)
			{
				if (Char.IsWhiteSpace(AString, LIndex))
				{
					if (!LInWhiteSpace)
					{
						LInWhiteSpace = true;
						LResult.Append(" ");
					}
				}
				else
				{
					LInWhiteSpace = false;
					LResult.Append(AString[LIndex]);
				}
			}
			
			return LResult.ToString();
		}
		
		private void InternalCreateElaboratedTableVar(ElaboratedExpression AExpression, Schema.TableVar ATableVar, string APageType, string AQuery)
		{
			FQuery = AQuery;
			string LDefaultTitle = NormalizeWhiteSpace(FQuery);	// Don't try to parse an expression with normalized white space, line comments can invalidate the query
			Expression LExpression = new Parser().ParseExpression(FQuery);
			if ((LExpression is IdentifierExpression) || (LExpression is QualifierExpression))
				LDefaultTitle = Schema.Object.Unqualify(LDefaultTitle);
			FElaboratedExpression = AExpression;
			FTableVar = ATableVar;
			FPageType = APageType;
			FTableTitle = 
				DerivationUtility.GetTag
				(
					FTableVar.MetaData, 
					"Title", 
					FPageType, 
					LDefaultTitle
				);
				
			// Gather the included column list
			#if USEINCLUDETAG
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
				if (Convert.ToBoolean(DerivationUtility.GetTag(LColumn.MetaData, "Include", FPageType, FPageType == DerivationUtility.CPreview ? "False" : "True")))
					FColumnNames.Add(LColumn.Name);
			#endif

			// If no columns are marked to be included, the default for the include tag is true				
			#if INCLUDEBYDEFAULT
			if (FColumnNames.Count == 0)
				foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
					#if USEINCLUDETAG
					if (Convert.ToBoolean(DerivationUtility.GetTag(LColumn.MetaData, "Include", FPageType, "True")))
					#endif
						FColumnNames.Add(LColumn.Name);
			#endif
						
			int LInsertIndex = 0;

			// Ensure that key columns for the clustered key are preserved if this is not a preview page type
			if (FPageType != DerivationUtility.CPreview)
				foreach (Schema.TableVarColumn LColumn in FTableVar.FindClusteringKey().Columns)
					if (!FColumnNames.Contains(LColumn.Name))
					{
						FColumnNames.Insert(LInsertIndex, LColumn.Name);
						LInsertIndex++;
					}

			// Ensure that the detail key columns are preserved, if they are part of this table var
			foreach (Schema.Column LColumn in FTableVar.DataType.Columns)
				if (((IList)FElaboratedExpression.DetailKeys).Contains(Schema.Object.Qualify(LColumn.Name, FElaboratedName)) && !FColumnNames.Contains(LColumn.Name))
				{
					FColumnNames.Insert(LInsertIndex, LColumn.Name);
					LInsertIndex++;
				}
		}
		
		// ElaboratedExpression
		protected ElaboratedExpression FElaboratedExpression;
		public ElaboratedExpression ElaboratedExpression { get { return FElaboratedExpression; } }
		
		// ElaboratedName
		protected string FElaboratedName = String.Empty;
		public string ElaboratedName { get { return FElaboratedName; } }
		
		// TableVar
		protected Schema.TableVar FTableVar;
		public Schema.TableVar TableVar { get { return FTableVar; } }
		
		// PageType
		protected string FPageType;
		public string PageType { get { return FPageType; } }
		
		// Query
		protected string FQuery;
		public string Query { get { return FQuery; } }
		
		// TableTitle
		protected string FTableTitle = String.Empty;
		public string TableTitle { get { return FTableTitle; } }

		// ColumnNames
		protected StringCollection FColumnNames = new StringCollection();
		public StringCollection ColumnNames { get { return FColumnNames; } }
		
		// ElaboratedReferences
		protected ElaboratedReferences FElaboratedReferences = new ElaboratedReferences();
		public ElaboratedReferences ElaboratedReferences { get { return FElaboratedReferences; } }
		
		// ElaboratedReference
		protected ElaboratedReference FElaboratedReference;
		public ElaboratedReference ElaboratedReference
		{
			get { return FElaboratedReference; }
			set
			{
				if (FElaboratedReference != null)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.ElaboratedReferenceSet, FElaboratedName);
				FElaboratedReference = value;
				EnsureElaboratedReferenceColumns();
			}
		}
		
		protected void EnsureElaboratedReferenceColumns()
		{
			// Make sure that all the columns named by the including reference are included in the expression
			foreach (Schema.TableVarColumn LColumn in (FElaboratedReference.ReferenceType == ReferenceType.Lookup) || (FElaboratedReference.ReferenceType == ReferenceType.Parent) ? FElaboratedReference.Reference.TargetKey.Columns : FElaboratedReference.Reference.SourceKey.Columns)
				if (!FColumnNames.Contains(LColumn.Name))
					FColumnNames.Add(LColumn.Name);
		}
		
		protected bool FIsEmbedded; // Internal to determine whether this table has been embedded into the expression
		public bool IsEmbedded
		{
			get { return FIsEmbedded; }
			set
			{
				if (FIsEmbedded)
					throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.EmbeddedSet, FElaboratedName); 
				FIsEmbedded = value;
			}
		}
		
		protected void InsertColumn(ElaboratedReference AReference, Schema.TableVarColumn AColumn, bool AVisible, string AGroupName, string AUnqualifiedGroupName, string AGroupTitle, int AIndex)
		{
			string LElaboratedName = Schema.Object.Qualify(AColumn.Name, ElaboratedName);
			AVisible = AVisible && !((IList)FElaboratedExpression.RootExpression.DetailKeys).Contains(LElaboratedName) && Convert.ToBoolean(DerivationUtility.GetTag(AColumn.MetaData, "Visible", PageType, "True"));
			bool LReadOnly = AColumn.ReadOnly || DerivationUtility.IsReadOnlyPageType(PageType) || Convert.ToBoolean(DerivationUtility.GetTag(AColumn.MetaData, "ReadOnly", PageType, "False")) || ((ElaboratedReference != null) && (ElaboratedReference.ReferenceType == ReferenceType.Lookup));
			FElaboratedExpression.RootExpression.EnsureGroups(AGroupName, AUnqualifiedGroupName, AGroupTitle, this, AReference);
			ElaboratedTableVarColumn LColumn = new ElaboratedTableVarColumn(this, AReference, AColumn, AVisible, LReadOnly, AGroupName);
			FElaboratedExpression.RootExpression.Columns.Insert(AIndex, LColumn);
			if (AReference != null)
				AReference.Columns.Add(LColumn);
		} 
		
		protected void ProcessKey(ElaboratedReference AReference, Schema.Key AKey)
		{
			foreach (ElaboratedTableVarColumn LReferenceColumn in AReference.Columns)
				if (AKey.Columns.Contains(LReferenceColumn.Column.Name))
				{
					LReferenceColumn.IsMaster = true;
					LReferenceColumn.Visible = false;
				}
		}
		
		protected void InternalInsertColumns(ref int AColumnIndex)
		{
			// Add all columns
			if (FElaboratedReference != null && (FElaboratedReference.ReferenceType == ReferenceType.Extension))
			{
				Schema.TableVarColumn LRowExistsColumn = new Schema.TableVarColumn(new Schema.Column("RowExists", FElaboratedExpression.Process.DataTypes.SystemBoolean), Schema.TableVarColumnType.RowExists);
				LRowExistsColumn.MetaData = new MetaData();
				LRowExistsColumn.MetaData.Tags.Add
				(
					new Tag
					(
						"Frontend.Title",
						String.Format
						(
							Strings.Get("HasColumnTitle"), 
							(FElaboratedReference.ReferenceTitle != null ? FElaboratedReference.ReferenceTitle : FElaboratedReference.SourceElaboratedTableVar.TableTitle)
						)
					)
				);
				LRowExistsColumn.MetaData.Tags.Add(new Tag("Frontend.ElementType", "CheckBox"));

				if (FElaboratedReference.Reference.MetaData != null)
				{
					MetaData LMetaData = DerivationUtility.ExtractTags(FElaboratedReference.Reference.MetaData.Tags, "RowExists", FElaboratedReference.TargetElaboratedTableVar.PageType);
					LRowExistsColumn.MetaData.Tags.AddOrUpdateRange(LMetaData.Tags);
				}

				InsertColumn(null, LRowExistsColumn, true, FElaboratedExpression.CurrentGroupName(), String.Empty, String.Empty, AColumnIndex);
				AColumnIndex++;
			}
				
			// Add each column, making it invisible if it is the right side of a reference
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				if (FColumnNames.Contains(LColumn.Name))
				{
					string LGroupName = DerivationUtility.GetTag(LColumn.MetaData, "Group", FPageType, String.Empty);
					bool LIsRightSide =
						(FElaboratedReference != null) && 
						(
							(
								(FElaboratedReference.ReferenceType == ReferenceType.Extension) && 
								FElaboratedReference.Reference.SourceKey.Columns.Contains(LColumn.Name)
							) ||
							(
								(FElaboratedReference.ReferenceType == ReferenceType.Lookup) &&
								FElaboratedReference.Reference.TargetKey.Columns.Contains(LColumn.Name)
							)
						);
					
					InsertColumn(null, LColumn, !LIsRightSide, ElaboratedExpression.CombineGroups(FElaboratedExpression.CurrentGroupName(), LGroupName), LGroupName, Schema.Object.Unqualify(LGroupName.Replace("\\", ".")), AColumnIndex);
					AColumnIndex++;
				}
			}			

			// Add columns for lookup references
			foreach (ElaboratedReference LReference in ElaboratedReferences)
			{
				if (LReference.ReferenceType == ReferenceType.Lookup)
				{
					string LLookupGroupName = String.Format("{0}{1}", LReference.ElaboratedName, "Group");

					string LReferenceGroupName = null;

					// Default group name for the reference is the group name for the appearing columns, if the group name for all appearing columns is the same
					foreach (ElaboratedTableVarColumn LReferenceColumn in LReference.Columns)
					{
						if (LReferenceColumn.Visible)
						{
							if (LReferenceGroupName == null)
								LReferenceGroupName = DerivationUtility.GetTag(LReferenceColumn.Column.MetaData, "Group", FPageType, String.Empty);
							else
								if (LReferenceGroupName != DerivationUtility.GetTag(LReferenceColumn.Column.MetaData, "Group", FPageType, String.Empty))
								{
									LReferenceGroupName = String.Empty;
									break;
								}
						}
					}
					
					if (LReferenceGroupName == null)
						LReferenceGroupName = String.Empty;
						
					LReferenceGroupName = DerivationUtility.GetTag(LReference.Reference.MetaData, "Group", FPageType, LReference.ReferenceType.ToString(), LReferenceGroupName);
					string LReferenceLookupGroupName = ElaboratedExpression.CombineGroups(LReferenceGroupName, LLookupGroupName);
					string LFullReferenceGroupName = ElaboratedExpression.CombineGroups(ElaboratedExpression.CurrentGroupName(), LReferenceLookupGroupName);
					
					ElaboratedExpression.RootExpression.EnsureGroups(LFullReferenceGroupName, LReferenceLookupGroupName, LReference.GroupTitle, this, LReference);

/*
					// Ensure that each column appearing in the reference is displayed within the same group.						
					foreach (ElaboratedTableVarColumn LReferenceColumn in LReference.Columns)
						if (LReferenceColumn.Visible && (LReferenceColumn.GroupName != LFullReferenceGroupName))
							LReferenceColumn.GroupName = LFullReferenceGroupName;
*/
					
					// insert all source columns after the last source column as lookup reference columns
					int LColumnIndex = -1;
					foreach (Schema.TableVarColumn LReferenceColumn in LReference.Reference.SourceKey.Columns)
					{
						int LElaboratedColumnIndex = ElaboratedExpression.RootExpression.Columns.IndexOf(Schema.Object.Qualify(LReferenceColumn.Column.Name, ElaboratedName));
						if (LElaboratedColumnIndex > LColumnIndex)
							LColumnIndex = LElaboratedColumnIndex;
					}
					if (LColumnIndex == -1)
						LColumnIndex = ElaboratedExpression.RootExpression.Columns.Count;

					foreach (Schema.TableVarColumn LReferenceColumn in LReference.Reference.SourceKey.Columns)
					{
						string LGroupName = DerivationUtility.GetTag(LReferenceColumn.MetaData, "Group", FPageType, String.Empty);
						bool LIsRightSide =
							(FElaboratedReference != null) && 
							(
								(
									(FElaboratedReference.ReferenceType == ReferenceType.Extension) && 
									FElaboratedReference.Reference.SourceKey.Columns.Contains(LReferenceColumn.Name)
								) ||
								(
									(FElaboratedReference.ReferenceType == ReferenceType.Lookup) &&
									FElaboratedReference.Reference.TargetKey.Columns.Contains(LReferenceColumn.Name)
								)
							);

						InsertColumn
						(
							LReference,
							LReferenceColumn, 
							!LIsRightSide,
							LFullReferenceGroupName,
							LReferenceLookupGroupName,
							LReference.GroupTitle,
							LColumnIndex
						);
						
						if (AColumnIndex >= LColumnIndex)
							AColumnIndex++;
						LColumnIndex++;
					}
					
					// If the intersection of the reference columns and the detail key of the derivation is non-empty
					Schema.Key LSubsetKey = new Schema.Key();
					if (ElaboratedExpression.DetailKeys.Length > 0)
					{
						foreach (Schema.TableVarColumn LColumn in LReference.Reference.SourceKey.Columns)
						{
							string LReferenceColumnName = Schema.Object.Qualify(LColumn.Name, ElaboratedName);
							if ((FElaboratedReference != null) && (FElaboratedReference.ReferenceType == ReferenceType.Extension))
							{
								int LExtensionKeyIndex = FElaboratedReference.Reference.SourceKey.Columns.IndexOf(LColumn.Name);
								if (LExtensionKeyIndex >= 0)
									LReferenceColumnName = Schema.Object.Qualify(FElaboratedReference.Reference.TargetKey.Columns[LExtensionKeyIndex].Name, FElaboratedReference.TargetElaboratedTableVar.ElaboratedName);
							}
							
							if (((IList)ElaboratedExpression.DetailKeys).Contains(LReferenceColumnName))
								LSubsetKey.Columns.Add(LColumn);
						}
					}
					
					if (LSubsetKey.Columns.Count > 0)
					{
						// the reference is a detail lookup
						LReference.IsDetailLookup = true;

						// set the key lookup reference columns invisible
						ProcessKey(LReference, LSubsetKey);
					}
					
					foreach (Schema.Key LKey in TableVar.Keys)
						// if the columns in the reference form a non-trivial proper superset of the key
						if ((LKey.Columns.Count > 0) && LReference.Reference.SourceKey.Columns.IsProperSupersetOf(LKey.Columns))
						{
							// the reference is a detail lookup
							LReference.IsDetailLookup = true;
							
							// set the key lookup reference columns invisible
							ProcessKey(LReference, LKey);
						}
					
					// For every reference that is included in the current reference, mark the columns of that reference as master
					foreach (ElaboratedReference LOtherReference in ElaboratedReferences)	
						if (LOtherReference.IsEmbedded && (LOtherReference.ReferenceType == ReferenceType.Lookup) && !Object.ReferenceEquals(LReference, LOtherReference) && LReference.Reference.SourceKey.Columns.IsProperSupersetOf(LOtherReference.Reference.SourceKey.Columns))
							ProcessKey(LReference, LOtherReference.Reference.SourceKey);
							
					// If this is an extension table and the intersection of the extension reference and the current reference is non-empty
					if 
					(
						(FElaboratedReference != null) && 
						(FElaboratedReference.ReferenceType == ReferenceType.Extension) && 
						(FElaboratedReference.Reference.SourceKey.Columns.Intersect(LReference.Reference.SourceKey.Columns).Columns.Count > 0)
					)
					{
						// Search the parent table for references including the current reference
						foreach (ElaboratedReference LOtherReference in FElaboratedReference.TargetElaboratedTableVar.ElaboratedReferences)
							if (LOtherReference.IsEmbedded && (LOtherReference.ReferenceType == ReferenceType.Lookup) && LOtherReference.Reference.SourceKey.Columns.IsSubsetOf(FElaboratedReference.Reference.TargetKey.Columns))
							{
								// Translate the source key for LOtherReference into the current table var
								Schema.Key LKey = new Schema.Key();
								foreach (Schema.TableVarColumn LColumn in LOtherReference.Reference.SourceKey.Columns)
									LKey.Columns.Add(FElaboratedReference.Reference.SourceKey.Columns[FElaboratedReference.Reference.TargetKey.Columns.IndexOfName(LColumn.Name)]);
									
								// Mark the columns of the included reference master
								if (LReference.Reference.SourceKey.Columns.IsProperSupersetOf(LKey.Columns))
									ProcessKey(LReference, LKey);
							}
					}
					
					// All columns still appearing with the reference should not appear in the rest of the interface
					foreach (ElaboratedTableVarColumn LReferenceColumn in LReference.Columns)
						if (!LReferenceColumn.IsMaster)
							ElaboratedExpression.RootExpression.Columns[Schema.Object.Qualify(LReferenceColumn.Column.Name, ElaboratedName)].Visible = false;
						
					if (LReference.IsEmbedded)
					{
						// insert all columns
						FElaboratedExpression.PushGroup(LReferenceLookupGroupName);
						try
						{
							int LSaveColumnIndex = LColumnIndex;
							LReference.TargetElaboratedTableVar.InsertColumns(ref LColumnIndex);
							if (AColumnIndex >= LSaveColumnIndex)
								AColumnIndex += LColumnIndex - LSaveColumnIndex;
						}
						finally
						{
							FElaboratedExpression.PopGroup();
						}
					}
				}
			}
		}
				
		public void AddColumns()
		{
			int LColumnIndex = ElaboratedExpression.RootExpression.Columns.Count;
			InsertColumns(ref LColumnIndex);
		}
		
		public static bool HasContributingColumns(ElaboratedTableVar ATableVar)
		{
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if ((LReference.ReferenceType == ReferenceType.Lookup) && HasContributingColumns(LReference.TargetElaboratedTableVar))
					return true;
					
			return (ATableVar.ElaboratedReference == null) || (ATableVar.ElaboratedReference.ReferenceType != ReferenceType.Lookup) || (ATableVar.ColumnNames.Count > ATableVar.ElaboratedReference.Reference.SourceKey.Columns.Count);
		}
		
		public void InsertColumns(ref int AColumnIndex)
		{
			// Do not add any columns if this is a lookup reference that will not add any information to the expression
			if (!HasContributingColumns(this))
				return;
				
			string LGroupName;

			// Add parent columns
			foreach (ElaboratedReference LReference in FElaboratedReferences)
				if ((LReference.ReferenceType == ReferenceType.Parent) && LReference.IsEmbedded)
				{
					// If the reference has a group tag specified, ensure the group and push it on the group stack
					LGroupName = DerivationUtility.GetTag(LReference.Reference.MetaData, "Group", FPageType, LReference.ReferenceType.ToString(), String.Empty);
					if (LGroupName != String.Empty)
						ElaboratedExpression.PushGroup(ElaboratedExpression.RootExpression.EnsureGroups(ElaboratedExpression.CombineGroups(ElaboratedExpression.CurrentGroupName(), LGroupName), LGroupName, LReference.GroupTitle, this, LReference));
					try
					{
						LReference.TargetElaboratedTableVar.InsertColumns(ref AColumnIndex);
					}
					finally
					{
						if (LGroupName != String.Empty)
							ElaboratedExpression.PopGroup();
					}
				}
					
			InternalInsertColumns(ref AColumnIndex);
				
			// Add extension columns
			foreach (ElaboratedReference LReference in FElaboratedReferences)
				if ((LReference.ReferenceType == ReferenceType.Extension) && LReference.IsEmbedded)
				{
					// If the reference has a group tag specified, ensure the group and push it on the group stack
					LGroupName = DerivationUtility.GetTag(LReference.Reference.MetaData, "Group", FPageType, LReference.ReferenceType.ToString(), String.Empty);
					if (LGroupName != String.Empty)
						ElaboratedExpression.PushGroup(ElaboratedExpression.RootExpression.EnsureGroups(ElaboratedExpression.CombineGroups(ElaboratedExpression.CurrentGroupName(), LGroupName), LGroupName, LReference.GroupTitle, this, LReference));
					try
					{
						LReference.SourceElaboratedTableVar.InsertColumns(ref AColumnIndex);
					}
					finally
					{
						if (LGroupName != String.Empty)
							ElaboratedExpression.PopGroup();
					}
				}
		}
		
		private static void ProcessModifierTags(ElaboratedReference AReference, JoinExpression AJoinExpression)
		{
			MetaData LModifiers = 
				DerivationUtility.ExtractTags
				(
					AReference.Reference.MetaData.Tags, 
					"Modifier", 						
					((AReference.ReferenceType == ReferenceType.Lookup) || (AReference.ReferenceType == ReferenceType.Parent)) 
						? AReference.SourceElaboratedTableVar.PageType 
						: AReference.TargetElaboratedTableVar.PageType,
					AReference.ReferenceType.ToString()
				);

			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in LModifiers.Tags)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < LModifiers.Tags.Count; LIndex++)
			{
				LTag = LModifiers.Tags[LIndex];
			#endif
				if (AJoinExpression.Modifiers == null)
					AJoinExpression.Modifiers = new LanguageModifiers();
				AJoinExpression.Modifiers.Add(new LanguageModifier(Schema.Object.Unqualify(LTag.Name), LTag.Value));
			}
		}

		public Expression Expression
		{
			get
			{
				// Build the base expression (projecting over columnnames if necessary)				
				Expression LExpression = new Parser().ParseExpression(FQuery);
				if (ColumnNames.Count < FTableVar.DataType.Columns.Count)
				{
					ProjectExpression LProjectExpression = new ProjectExpression();
					LProjectExpression.Expression = LExpression;
					foreach (string LString in ColumnNames)
						LProjectExpression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(LString)));
					LExpression = LProjectExpression;
				}
				LExpression = new RenameAllExpression(LExpression, FElaboratedName);
				
				// Join all lookup references
				foreach (ElaboratedReference LReference in FElaboratedReferences)
					if ((LReference.ReferenceType == ReferenceType.Lookup) && LReference.IsEmbedded && HasContributingColumns(LReference.TargetElaboratedTableVar)) //(LReference.TargetElaboratedTableVar.ColumnNames.Count > LReference.Reference.TargetKey.Columns.Count)) // LReference.TargetElaboratedTableVar.TableVar.Columns.IsProperSupersetOf(LReference.Reference.TargetKey.Columns))
					{
						LeftOuterJoinExpression LJoinExpression = new LeftOuterJoinExpression();
						LJoinExpression.IsLookup = true;
						if (LReference.IsDetailLookup)
						{
							LJoinExpression.Modifiers = new LanguageModifiers();
							LJoinExpression.Modifiers.Add(new LanguageModifier("IsDetailLookup", "True"));
						}
						ProcessModifierTags(LReference, LJoinExpression);
						LJoinExpression.LeftExpression = LExpression;
						LJoinExpression.RightExpression = LReference.TargetElaboratedTableVar.Expression;
						LJoinExpression.Condition = LReference.JoinExpression;
						LExpression = LJoinExpression;
					}

				// Join all the extension references
				foreach (ElaboratedReference LReference in FElaboratedReferences)
					if ((LReference.ReferenceType == ReferenceType.Extension) && LReference.IsEmbedded)
					{
						LReference.SourceElaboratedTableVar.IsEmbedded = true;
						LeftOuterJoinExpression LJoinExpression = new LeftOuterJoinExpression();
						ProcessModifierTags(LReference, LJoinExpression);
						LJoinExpression.LeftExpression = LExpression;
						LJoinExpression.RightExpression = LReference.SourceElaboratedTableVar.Expression;
						LJoinExpression.Condition = LReference.JoinExpression;
						LJoinExpression.RowExistsColumn = new IncludeColumnExpression();
						LJoinExpression.RowExistsColumn.ColumnAlias = Schema.Object.Qualify("RowExists", LReference.SourceElaboratedTableVar.ElaboratedName);
						LExpression = LJoinExpression;
					}

				// Join all parent references
				foreach (ElaboratedReference LReference in FElaboratedReferences)
					if ((LReference.ReferenceType == ReferenceType.Parent) && LReference.IsEmbedded)
					{
						LReference.TargetElaboratedTableVar.IsEmbedded = true;
						InnerJoinExpression LJoinExpression = new InnerJoinExpression();
						ProcessModifierTags(LReference, LJoinExpression);
						LJoinExpression.LeftExpression = LReference.TargetElaboratedTableVar.Expression;
						LJoinExpression.RightExpression = LExpression;
						LJoinExpression.Condition = LReference.JoinExpression;
						LExpression = LJoinExpression;
					}
					
				return LExpression;
			}
		}
	}
	
	public class ElaboratedTableVarColumn : Object
	{
		public ElaboratedTableVarColumn(ElaboratedTableVar ATable, Schema.TableVarColumn AColumn, bool AVisible, string AGroupName) : base()
		{
			FElaboratedTableVar = ATable;
			FColumn = AColumn;
			FVisible = AVisible;
			FElaboratedName = Schema.Object.Qualify(AColumn.Name, FElaboratedTableVar.ElaboratedName);
			FGroupName = AGroupName;
		}
		
		public ElaboratedTableVarColumn(ElaboratedTableVar ATable, ElaboratedReference AReference, Schema.TableVarColumn AColumn, bool AVisible, bool AReadOnly, string AGroupName) : base()
		{
			FElaboratedTableVar = ATable;
			FElaboratedReference = AReference;
			FColumn = AColumn;
			FVisible = AVisible;
			FReadOnly = AReadOnly;
			if (AReference == null)
				FElaboratedName = Schema.Object.Qualify(AColumn.Name, FElaboratedTableVar.ElaboratedName);
			else
				FElaboratedName = String.Format("{0}_{1}.{2}", FElaboratedReference.Reference.OriginatingReferenceName(), FElaboratedTableVar.ElaboratedName, AColumn.Name);
			FGroupName = AGroupName;
		}
		
		public ElaboratedTableVarColumn(ElaboratedTableVar ATable, Schema.TableVarColumn AColumn, string AElaboratedName, bool AVisible, string AGroupName) : base()
		{
			FElaboratedTableVar = ATable;
			FColumn = AColumn;
			FVisible = AVisible;
			FElaboratedName = AElaboratedName;
			FGroupName = AGroupName;
		}
		
		// ElaboratedName
		protected string FElaboratedName = String.Empty;
		public string ElaboratedName {	get { return FElaboratedName; } }
		
		// ElaboratedTableVar
		protected ElaboratedTableVar FElaboratedTableVar;
		public ElaboratedTableVar ElaboratedTableVar
		{
			get { return FElaboratedTableVar; }
			set { FElaboratedTableVar = value; }
		}
		
		// ElaboratedReference
		protected ElaboratedReference FElaboratedReference;
		public ElaboratedReference ElaboratedReference
		{
			get { return FElaboratedReference; }
			set { FElaboratedReference = value; }
		}

		// Schema.TableVarColumn
		protected Schema.TableVarColumn FColumn;
		public Schema.TableVarColumn Column { get { return FColumn; } }
		
		// IsMaster
		protected bool FIsMaster;
		public bool IsMaster
		{
			get { return FIsMaster; }
			set { FIsMaster = value; }
		}
		
		// Visible
		protected bool FVisible;
		public bool Visible
		{
			get { return FVisible; }
			set { FVisible = value; }
		}
		
		// ReadOnly
		protected bool FReadOnly;
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set { FReadOnly = value; }
		}
		
		// GroupName
		protected string FGroupName = String.Empty;
		public string GroupName 
		{ 
			get { return FGroupName; } 
			set { FGroupName = value == null ? String.Empty : value; }
		}

		// TitleSeed
		public string GetTitleSeed()
		{
			StringBuilder LTitleSeed = new StringBuilder();
			ElaboratedReference LReference = FElaboratedTableVar.ElaboratedReference;
			while (LReference != null)
			{
				if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, "IncludeGroupTitle", "True")))
					LTitleSeed.Insert(0, String.Format("{0} ", LReference.GroupTitle));
				else
					break;

				if ((LReference.ReferenceType == ReferenceType.Parent) || (LReference.ReferenceType == ReferenceType.Lookup))
					LReference = LReference.SourceElaboratedTableVar.ElaboratedReference;
				else
					LReference = LReference.TargetElaboratedTableVar.ElaboratedReference;
			}
			return LTitleSeed.ToString();
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
		public ElaboratedTableVarColumn this[string AColumnName] 
		{ 
			get 
			{ 
				int LIndex = IndexOf(AColumnName);
				if (LIndex < 0)
					throw new ServerException(ServerException.Codes.ColumnNotFound, AColumnName);
				return this[LIndex]; 
			} 
		}
		
		public int IndexOf(string AColumnName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Compare(AColumnName, this[LIndex].ElaboratedName) == 0)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AColumnName)
		{
			return IndexOf(AColumnName) >= 0;
		}
	}
	
	public class ElaboratedReference : Object
	{
		public ElaboratedReference
		(
			ElaboratedExpression AExpression, 
			Schema.Reference AReference, 
			ReferenceType AReferenceType, 
			ElaboratedTableVar ASourceTable, 
			ElaboratedTableVar ATargetTable
		) : base()
		{
			FElaboratedExpression = AExpression;
			FReference = AReference;
			FReferenceType = AReferenceType;
			FReferenceTitle = 
				DerivationUtility.GetTag
				(
					FReference.MetaData,
					"Title",
					((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? ATargetTable.PageType : ASourceTable.PageType,
					FReferenceType.ToString(),
					((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? ATargetTable.TableTitle : ASourceTable.TableTitle
				);
				
			FPriority = 
				Convert.ToInt32
				(
					DerivationUtility.GetTag
					(
						FReference.MetaData, 
						"Priority", 
						((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? ATargetTable.PageType : ASourceTable.PageType,
						FReferenceType.ToString(),
						DerivationUtility.CDefaultPriority
					)
				);

			FSourceElaboratedTableVar = ASourceTable;
			FTargetElaboratedTableVar = ATargetTable;

			if ((FReferenceType == ReferenceType.Extension) && (ASourceTable.PageType == DerivationUtility.CPreview))
			{
				FIsEmbedded =
					Convert.ToBoolean
					(
						DerivationUtility.GetExplicitPageTag
						(
							FReference.MetaData,
							"Embedded",
							ASourceTable.PageType,
							FReferenceType.ToString(),
							"False"
						)
					);
			}
			else
			{
				FIsEmbedded = 
					Convert.ToBoolean
					(
						DerivationUtility.GetTag
						(
							FReference.MetaData, 
							"Embedded",
							((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? ASourceTable.PageType : ATargetTable.PageType,
							FReferenceType.ToString(),
							((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? "True" : "False"
						)
					);
			}
				
			StringBuilder LName = new StringBuilder();
			if ((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent))
			{
				for (int LIndex = 0; LIndex < FReference.SourceKey.Columns.Count; LIndex++)
				{
					if (LName.Length > 0)
						LName.Append(Keywords.Qualifier);
					LName.Append(Schema.Object.Qualify(FReference.SourceKey.Columns[LIndex].Name, FSourceElaboratedTableVar.ElaboratedName));
				}
			}
			else
			{
				for (int LIndex = 0; LIndex < FReference.TargetKey.Columns.Count; LIndex++)
				{
					if (LName.Length > 0)
						LName.Append(Keywords.Qualifier);
					LName.Append(Schema.Object.Qualify(FReference.TargetKey.Columns[LIndex].Name, FTargetElaboratedTableVar.ElaboratedName));
				}
			}
			if (LName.Length > 0)
				LName.Append(Keywords.Qualifier);
			LName.Append(FReference.OriginatingReferenceName());
			FElaboratedName = AExpression.AddReferenceName(LName.ToString());
			
			if (FReference.MetaData != null)
				FGroupMetaData = 
					DerivationUtility.ExtractTags
					(
						FReference.MetaData.Tags, 
						"Group", 
						((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? 
							ASourceTable.PageType :
							ATargetTable.PageType, 
						FReferenceType.ToString()
					);
					
			FGroupTitle = 
				DerivationUtility.GetTag
				(
					FGroupMetaData, 
					"Title", 
					((FReferenceType == ReferenceType.Lookup) || (FReferenceType == ReferenceType.Parent)) ? 
						ASourceTable.PageType :
						ATargetTable.PageType, 
					FReferenceType.ToString(),
					FReferenceTitle
				);
		}
		
		// ElaboratedExpression
		protected ElaboratedExpression FElaboratedExpression;
		public ElaboratedExpression ElaboratedExpression { get { return FElaboratedExpression; } }
		
		// Reference
		protected Schema.Reference FReference;
		public Schema.Reference Reference { get { return FReference; } }

		// Columns		
		protected ElaboratedTableVarColumns FColumns = new ElaboratedTableVarColumns();
		public ElaboratedTableVarColumns Columns { get { return FColumns; } }
		
		// Priority
		protected int FPriority;
		public int Priority { get { return FPriority; } }
		
		// ReferenceType
		protected ReferenceType FReferenceType;
		public ReferenceType ReferenceType { get { return FReferenceType; } }
		
		// ReferenceTitle
		protected string FReferenceTitle;
		public string ReferenceTitle { get { return FReferenceTitle; } }
		
		// GroupTitle
		protected string FGroupTitle;
		public string GroupTitle { get { return FGroupTitle; } }
		
		protected MetaData FGroupMetaData;
		public MetaData GroupMetaData { get { return FGroupMetaData; } }
		
		// ElaboratedName
		protected string FElaboratedName;
		public string ElaboratedName { get { return FElaboratedName; } }
		
		// SourceElaboratedTableVar
		protected ElaboratedTableVar FSourceElaboratedTableVar;
		public ElaboratedTableVar SourceElaboratedTableVar { get { return FSourceElaboratedTableVar; } }
		
		// TargetElaboratedTableVar
		protected ElaboratedTableVar FTargetElaboratedTableVar;
		public ElaboratedTableVar TargetElaboratedTableVar { get { return FTargetElaboratedTableVar; } }
		
		// IsEmbedded
		protected bool FIsEmbedded;
		public bool IsEmbedded { get { return FIsEmbedded; } }
		
		// IsDetailLookup
		protected bool FIsDetailLookup;
		public bool IsDetailLookup 
		{ 
			get { return FIsDetailLookup; } 
			set { FIsDetailLookup = value; }
		}
		
		// JoinExpression
		public Expression JoinExpression
		{
			get
			{
				Expression LJoinCondition = null;
				BinaryExpression LColumnExpression;
				for (int LIndex = 0; LIndex < FReference.SourceKey.Columns.Count; LIndex++)
				{
					LColumnExpression = 
						new BinaryExpression
						(
							new IdentifierExpression
							(
								Schema.Object.EnsureRooted
								(
									Schema.Object.Qualify
									(
										FReference.SourceKey.Columns[LIndex].Name, 
										FSourceElaboratedTableVar.ElaboratedName
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
										FReference.TargetKey.Columns[LIndex].Name,
										FTargetElaboratedTableVar.ElaboratedName
									)
								)
							)
						);
						
					if (LJoinCondition != null)
						LJoinCondition =
							new BinaryExpression
							(
								LJoinCondition,
								Instructions.And,
								LColumnExpression
							);
					else
						LJoinCondition = LColumnExpression;
				}
				
				return LJoinCondition;
			}
		}
	}
	
	public class ElaboratedReferences : System.Object, IEnumerable
	{
		private const int CDefaultCapacity = 4;
		private const int CDefaultGrowth = 4;
	
		public ElaboratedReferences() : base()
		{
			FElaboratedReferences = new ElaboratedReference[CDefaultCapacity];
		}

		private int FCount;		
		public int Count { get { return FCount; } }

		private ElaboratedReference[] FElaboratedReferences;
		public ElaboratedReference this[int AIndex] 
		{ 
			get 
			{
				if ((AIndex < 0) || AIndex >= FCount)
					throw new IndexOutOfRangeException();
				return FElaboratedReferences[AIndex]; 
			} 
		}
		
		private bool FOrderByPriorityOnly = false;
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
			get { return FOrderByPriorityOnly; }
			set 
			{ 
				if (FOrderByPriorityOnly != value)
				{
					FOrderByPriorityOnly = value; 
					if (FCount > 0)
					{
						ElaboratedReference[] LReferences = FElaboratedReferences;
						FElaboratedReferences = new ElaboratedReference[LReferences.Length];
						FCount = 0;
						for (int LIndex = 0; LIndex < LReferences.Length; LIndex++)
							Add(LReferences[LIndex]);
					}
				}
			}
		}
		
		public void Add(ElaboratedReference AElaboratedReference)
		{
			EnsureSize(FCount + 1);
			if (FOrderByPriorityOnly)
				for (int LIndex = 0; LIndex < FCount; LIndex++)
				{
					if (AElaboratedReference.Priority < FElaboratedReferences[LIndex].Priority)
					{
						InsertAt(AElaboratedReference, LIndex);
						return;
					}
				}
			else
				for (int LIndex = 0; LIndex < FCount; LIndex++)
				{
					if 
					(
						(
							(AElaboratedReference.Reference.SourceKey.Columns.Count == FElaboratedReferences[LIndex].Reference.SourceKey.Columns.Count) && 
							(AElaboratedReference.Priority < FElaboratedReferences[LIndex].Priority)
						) || 
						(AElaboratedReference.Reference.SourceKey.Columns.Count < FElaboratedReferences[LIndex].Reference.SourceKey.Columns.Count)
					)
					{
						InsertAt(AElaboratedReference, LIndex);
						return;
					}
				}

			InsertAt(AElaboratedReference, FCount);
		}
		
		public void Remove(ElaboratedReference AElaboratedReference)
		{
			RemoveAt(IndexOf(AElaboratedReference));
		}
		
		public int IndexOf(ElaboratedReference AElaboratedReference)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (Object.ReferenceEquals(FElaboratedReferences[LIndex], AElaboratedReference))
					return LIndex;
			return -1;
		}
		
		public bool Contains(ElaboratedReference AElaboratedReference)
		{
			return IndexOf(AElaboratedReference) >= 0;
		}
		
		private void InsertAt(ElaboratedReference AElaboratedReference, int AIndex)
		{
			Array.Copy(FElaboratedReferences, AIndex, FElaboratedReferences, AIndex + 1, FCount - AIndex);
			FElaboratedReferences[AIndex] = AElaboratedReference;
			FCount++;
		}
		
		private void RemoveAt(int AIndex)
		{
			Array.Copy(FElaboratedReferences, AIndex + 1, FElaboratedReferences, AIndex, FCount - AIndex);
			FCount--;
			FElaboratedReferences[FCount] = null;
		}
		
		private void EnsureSize(int ACount)
		{
			if (FElaboratedReferences.Length < ACount)
			{
				ElaboratedReference[] LNewElaboratedReferences = new ElaboratedReference[FElaboratedReferences.Length + CDefaultGrowth];
				Array.Copy(FElaboratedReferences, LNewElaboratedReferences, FCount);
				FElaboratedReferences = LNewElaboratedReferences;
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
            public ElaboratedReferenceEnumerator(ElaboratedReferences AElaboratedReferences) : base()
            {
                FElaboratedReferences = AElaboratedReferences;
            }
            
            private int FCurrent = -1;
            private ElaboratedReferences FElaboratedReferences;

            public object Current { get { return FElaboratedReferences[FCurrent]; } }

            public void Reset()
            {
                FCurrent = -1;
            }

            public bool MoveNext()
            {
				FCurrent++;
				return FCurrent < FElaboratedReferences.Count;
            }
        }
	}
}

