/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Frontend.Server.Derivation;
using Alphora.Dataphor.Frontend.Server.Elaboration;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Server.Structuring
{
	/*
		Structuring is the process of determining grouping of columns within a derived interface.
		Each column in the result set is placed into the appropriate group, with the elements
		within a group ordered by priority, then arrival order.
	*/
	
	public enum Flow { Default, Vertical, Horizontal }
	
	public class DerivationInfo : System.Object
	{
		public readonly static Char[] CColumnNameDelimiters = new Char[] {',',';'};

		public Program Program;
		public ServerProcess Process;
		public Schema.TableVar TableVar;
		public ElaboratedExpression ElaboratedExpression;
		public string Expression;
		public string Query;
		public bool Elaborate;
		public string KeyNames;
		public string DetailKeyNames;
		public string MasterKeyNames;
		public string MainSourceName;
		public string PageType;
		public bool IsReadOnly;

		public string BuildImageExpression(string AImageName)
		{
			return String.Format
			(
				"Image('Frontend', 'Image.{0}')",
				AImageName
			);
		}

		public string BuildDerivationExpression(string APageType, string AQuery, string AMasterKeyNames, string ADetailKeyNames)
		{
			return BuildDerivationExpression(APageType, AQuery, AMasterKeyNames, ADetailKeyNames, true);
		}

		public string BuildDerivationExpression(string APageType, string AQuery, string AMasterKeyNames, string ADetailKeyNames, bool AElaborate)
		{
			return BuildDerivationExpression
			(
				new DerivationSeed
				(
					APageType,
					AQuery,
					AElaborate,
					AMasterKeyNames,
					ADetailKeyNames
				)
			);
		}

		/// <summary> Builds a derivation document expression. </summary>
		/// <returns> The document expression to use. </returns>
		public string BuildDerivationExpression(DerivationSeed ASeed)
		{
			return String.Format
			(
				".Frontend.Derive('{0}', '{1}', '{2}', '{3}', {4})",
				ASeed.Query.Replace("'", "''"),
				ASeed.PageType,
				ASeed.MasterKeyNames,
				ASeed.DetailKeyNames,
				Convert.ToString(ASeed.Elaborate).ToLower()
			);
		}
	}
	
	public abstract class StructureBuilder : System.Object
	{
		public StructureBuilder(DerivationInfo ADerivationInfo) : base()
		{
			FDerivationInfo = ADerivationInfo;
		}
		
		protected ContainerElement FRootElement;

		protected DerivationInfo FDerivationInfo;
		
		public abstract Element Build();
		
		protected string GetParentGroupName(string AGroupName)
		{
			int LIndex = AGroupName.LastIndexOf('\\');
			if (LIndex >= 0)
				return AGroupName.Substring(0, LIndex);
			else
				return String.Empty;
		}
		
		protected void EnsureGroups(string AGroupName, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			string[] LGroupNames = AGroupName.Split('\\');
			StringBuilder LGroupName = new StringBuilder();
			for (int LIndex = 0; LIndex < LGroupNames.Length; LIndex++)
			{
				if (LGroupName.Length > 0)
					LGroupName.Append("\\");
				LGroupName.Append(LGroupNames[LIndex]);
				EnsureGroup(LGroupName.ToString(), ATitleSeed, APageType, AIsReadOnly);
			}
		}
		
		protected void EnsureGroup(string AGroupName, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			if (AGroupName != String.Empty)
			{
				GroupElement LGroupElement = FRootElement.FindElement(AGroupName) as GroupElement;
				if (LGroupElement == null)
				{
					ElaboratedGroup LGroup = FDerivationInfo.ElaboratedExpression.Groups[AGroupName];
					LGroupElement = new GroupElement(AGroupName);
					MetaData LGroupMetaData = new MetaData(LGroup.Properties);
					LGroupElement.ElementType = DerivationUtility.GetTag(LGroupMetaData, "ElementType", APageType, "Group");
					LGroupElement.EliminateGroup = Boolean.Parse(DerivationUtility.GetTag(LGroupMetaData, "EliminateGroup", APageType, "True"));
					CreateContainerElement(LGroupElement, FDerivationInfo.ElaboratedExpression.Groups[AGroupName], ATitleSeed, APageType, AIsReadOnly);
					AddElement(LGroupElement, GetParentGroupName(AGroupName), ATitleSeed, APageType, AIsReadOnly);
				}
			}
		}
		
		protected void AddElement(Element AElement, string AParentGroupName, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			EnsureGroups(AParentGroupName, ATitleSeed, APageType, AIsReadOnly);
			if (AParentGroupName == String.Empty)
				FRootElement.Elements.Add(AElement);
			else
				((GroupElement)FRootElement.FindElement(AParentGroupName)).Elements.Add(AElement);
		}
		
		protected virtual string GetDefaultElementType(Schema.IDataType ADataType, string APageType)
		{
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			
			if (LScalarType != null)
			{
				if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsBoolean.NativeType)
					return "CheckBox";

				if (!DerivationUtility.IsReadOnlyPageType(APageType))					
				{
					if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsDateTime.NativeType)
						return "DateTimeBox";
						
					if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsDecimal.NativeType)
						return "NumericTextBox";
						
					if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsInt64.NativeType)
						return "NumericTextBox";

					if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsInt32.NativeType)
						return "NumericTextBox";

					if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsInt16.NativeType)
						return "NumericTextBox";

					if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsByte.NativeType)
						return "NumericTextBox";
				}
			}
			
			return "TextBox";
		}
		
		protected string StringArrayToNames(string[] AArray)
		{
			StringBuilder LResult = new StringBuilder();
			for (int LIndex = 0; LIndex < AArray.Length; LIndex++)
			{
				if (LIndex > 0)
					LResult.Append(";");
				LResult.Append(AArray[LIndex]);
			}
			return LResult.ToString();
		}
		
		protected string GetLookupDocument(ElaboratedReference AReference, string[] AMasterKeyNames, string[] ADetailKeyNames)
		{
			string LPageType =
				Boolean.Parse
				(
					DerivationUtility.GetTag
					(
						AReference.Reference.MetaData,
						"UseList",
						"",
						AReference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							AReference.TargetElaboratedTableVar.TableVar.MetaData, 
							"UseList", 
							"False"
						)
					)
				) ? DerivationUtility.CList : DerivationUtility.CBrowse;

			string LQuery =
				DerivationUtility.GetTag
				(
					AReference.Reference.MetaData, 
					"Query", 
					LPageType, 
					AReference.ReferenceType.ToString(), 
					DerivationUtility.GetTag
					(
						AReference.TargetElaboratedTableVar.TableVar.MetaData, 
						"Query", 
						LPageType,
						AReference.Reference.TargetTable.Name
					)
				);
				
			string LMasterKeyNames =
				DerivationUtility.GetTag
				(
					AReference.Reference.MetaData,
					"MasterKeyNames",
					LPageType,
					AReference.ReferenceType.ToString(),
					StringArrayToNames(AMasterKeyNames)
				);
				
			string LDetailKeyNames =
				DerivationUtility.GetTag
				(
					AReference.Reference.MetaData,
					"DetailKeyNames",
					LPageType,
					AReference.ReferenceType.ToString(),
					StringArrayToNames(ADetailKeyNames)
				);

			bool LElaborate = 
				Boolean.Parse
				(
					DerivationUtility.GetTag
					(
						AReference.Reference.MetaData,
						"Elaborate",
						LPageType,
						AReference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							AReference.TargetElaboratedTableVar.TableVar.MetaData, 
							"Elaborate", 
							LPageType, 
							"True"
						)
					)
				);
				
			if (LMasterKeyNames != String.Empty)
			{
				return
					DerivationUtility.GetTag
					(
						AReference.Reference.MetaData,
						"Document",
						LPageType,
						AReference.ReferenceType.ToString(),
						FDerivationInfo.BuildDerivationExpression
						(
							LPageType, 
							LQuery,
							LMasterKeyNames,
							LDetailKeyNames,
							LElaborate
						)
					);
			}
			else
			{
				return 
					DerivationUtility.GetTag
					(
						AReference.Reference.MetaData,
						"Document",
						LPageType,
						AReference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							AReference.TargetElaboratedTableVar.TableVar.MetaData,
							"Document",
							LPageType,
							FDerivationInfo.BuildDerivationExpression
							(
								LPageType, 
								LQuery,
								"",
								"",
								LElaborate
							)
						)
					);
			}
		}
		
		protected void PrepareElement(Element AElement, MetaData AExtractedMetaData, MetaData ACompleteMetaData, string ATitleSeed, string APageType, bool AReadOnly)
		{
			PrepareElement(AElement, AExtractedMetaData, ACompleteMetaData, ATitleSeed, APageType, String.Empty, AReadOnly);
		}
		
		protected virtual void PrepareElement(Element AElement, MetaData AExtractedMetaData, MetaData ACompleteMetaData, string ATitleSeed, string APageType, string AReferenceType, bool AIsReadOnly)
		{
			// Common element properties:
			// Title
			// Hint
			// Priority
			// Flow
			// FlowBreak
			// Break
			// Properties
			AElement.Hint = DerivationUtility.GetTag(AExtractedMetaData, "Hint", APageType, AReferenceType, DerivationUtility.GetTag(ACompleteMetaData, "Hint", APageType, AReferenceType, String.Empty));
			AElement.Priority = Convert.ToInt32(DerivationUtility.GetTag(AExtractedMetaData, "Priority", APageType, AReferenceType, DerivationUtility.GetTag(ACompleteMetaData, "Priority", APageType, AReferenceType, DerivationUtility.CDefaultPriority)));
			AElement.Flow = (Flow)Enum.Parse(typeof(Flow), DerivationUtility.GetTag(AExtractedMetaData, "Flow", APageType, AReferenceType, DerivationUtility.GetTag(ACompleteMetaData, "Flow", APageType, AReferenceType, Flow.Default.ToString())));
			AElement.FlowBreak = Convert.ToBoolean(DerivationUtility.GetTag(AExtractedMetaData, "FlowBreak", APageType, AReferenceType, DerivationUtility.GetTag(ACompleteMetaData, "FlowBreak", APageType, AReferenceType, "False")));
			AElement.Break = Convert.ToBoolean(DerivationUtility.GetTag(AExtractedMetaData, "Break", APageType, AReferenceType, DerivationUtility.GetTag(ACompleteMetaData, "Break", APageType, AReferenceType, "False")));

			if (AReferenceType == String.Empty)
			{
				DerivationUtility.ExtractProperties(ACompleteMetaData, AElement.ElementType, APageType, AElement.Properties);			
				DerivationUtility.ExtractProperties(AExtractedMetaData, AElement.ElementType, APageType, AElement.Properties);
			}
			else
			{
				DerivationUtility.ExtractProperties(ACompleteMetaData, AElement.ElementType, APageType, AReferenceType, AElement.Properties);			
				DerivationUtility.ExtractProperties(AExtractedMetaData, AElement.ElementType, APageType, AReferenceType, AElement.Properties);
			}
		}
		
		protected virtual ColumnElement CreateColumnElement(ColumnElement AElement, ElaboratedTableVarColumn AElaboratedColumn, Schema.TableVarColumn AColumn, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			PrepareElement(AElement, AColumn.MetaData, null, ATitleSeed, APageType, AIsReadOnly);
			
			if 
			(
				(((Schema.ScalarType)AColumn.DataType).NativeType == DAE.Runtime.Data.NativeAccessors.AsString.NativeType) 
					&& (AElaboratedColumn.ElaboratedReference == null)
					&& !AElement.Properties.Contains("NilIfBlank")
					&& (AElement.ElementType != "Choice") // TODO: Control types: need a better mechanism for determining whether or not a particular property applies to a particular control type...
			)
			{
				AElement.Properties.AddOrUpdate("NilIfBlank", "False");
			}

			AElement.Title = DerivationUtility.GetTag(AColumn.MetaData, "Caption", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Title", APageType, Schema.Object.Unqualify(AColumn.Name)));
			string LWidth = DerivationUtility.GetTag(AColumn.MetaData, "Width", APageType, String.Empty);
			if (LWidth != String.Empty)
				AElement.Properties.AddOrUpdate("Width", LWidth);
			AElement.Properties.AddOrUpdate("Source", AElement.Source);
			AElement.Properties.AddOrUpdate("ColumnName", AElement.ColumnName);
			if (AColumn.ReadOnly || AIsReadOnly || Convert.ToBoolean(DerivationUtility.GetTag(AColumn.MetaData, "ReadOnly", APageType, "False")))
				AElement.Properties.AddOrUpdate("ReadOnly", "True");

			return AElement;
		}
		
		protected virtual GridElement CreateGridElement(GridElement AElement, Schema.TableVar ATableVar, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			MetaData LMetaData = null;
			if (ATableVar.MetaData != null)
				LMetaData = DerivationUtility.ExtractTags(ATableVar.MetaData.Tags, "Grid", APageType);
			PrepareElement(AElement, LMetaData, ATableVar.MetaData, ATitleSeed, APageType, AIsReadOnly);
			
			//AElement.Title = ATitleSeed + DerivationUtility.GetTag(LMetaData, "Title", APageType, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle); // grid has no title
			AElement.Hint = DerivationUtility.GetTag(LMetaData, "Hint", APageType, DerivationUtility.GetTag(ATableVar.MetaData, "Hint", APageType, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle));
			AElement.Properties.AddOrUpdate("Source", AElement.Source);
			//if (AIsReadOnly)
			//	AElement.Properties.AddOrUpdate("ReadOnly", "True");
			
			return AElement;
		}
		
		protected virtual GridColumnElement CreateGridColumnElement(GridColumnElement AElement, Schema.TableVarColumn AColumn, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			MetaData LMetaData = null;
			if (AColumn.MetaData != null)
				LMetaData = DerivationUtility.ExtractTags(AColumn.MetaData.Tags, "Grid", APageType);
			PrepareElement(AElement, LMetaData, AColumn.MetaData, ATitleSeed, APageType, AIsReadOnly);
			
			AElement.Title = DerivationUtility.GetTag(LMetaData, "Caption", APageType, (AColumn.ColumnType == Schema.TableVarColumnType.RowExists ? String.Empty : ATitleSeed) + DerivationUtility.GetTag(LMetaData, "Title", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Title", APageType, Schema.Object.Unqualify(AColumn.Name))));
			AElement.Hint = DerivationUtility.GetTag(LMetaData, "Hint", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Hint", APageType, String.Empty));
			AElement.Width = Convert.ToInt32(DerivationUtility.GetTag(LMetaData, "Width", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Width", APageType, (20).ToString())));
			AElement.Properties.AddOrUpdate("Width", AElement.Width.ToString());
			AElement.Properties.AddOrUpdate("ColumnName", AElement.ColumnName);

			return AElement;
		}
		
		protected virtual SearchElement CreateSearchElement(SearchElement AElement, Schema.TableVar ATableVar, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			MetaData LMetaData = null;
			if (ATableVar.MetaData != null)
				LMetaData = DerivationUtility.ExtractTags(ATableVar.MetaData.Tags, "Search", APageType);
			PrepareElement(AElement, LMetaData, ATableVar.MetaData, ATitleSeed, APageType, AIsReadOnly);
			
			AElement.Hint = DerivationUtility.GetTag(LMetaData, "Hint", APageType, "Search for a specific row.");
			AElement.Properties.AddOrUpdate("Source", AElement.Source);
		
			return AElement;
		}
		
		protected virtual SearchColumnElement CreateSearchColumnElement(SearchColumnElement AElement, Schema.TableVarColumn AColumn, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			MetaData LMetaData = null;
			if (AColumn.MetaData != null)
				LMetaData = DerivationUtility.ExtractTags(AColumn.MetaData.Tags, "Search", APageType);
			PrepareElement(AElement, LMetaData, AColumn.MetaData, ATitleSeed, APageType, AIsReadOnly);
			
			AElement.Title = DerivationUtility.GetTag(LMetaData, "Caption", APageType, (AColumn.ColumnType == Schema.TableVarColumnType.RowExists ? String.Empty : ATitleSeed) + DerivationUtility.GetTag(LMetaData, "Title", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Title", APageType, Schema.Object.Unqualify(AColumn.Name))));
			AElement.Hint = DerivationUtility.GetTag(LMetaData, "Hint", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Hint", APageType, String.Empty));
			AElement.Width = Convert.ToInt32(DerivationUtility.GetTag(LMetaData, "Width", APageType, DerivationUtility.GetTag(AColumn.MetaData, "Width", APageType, (20).ToString())));
			AElement.Properties.AddOrUpdate("Width", AElement.Width.ToString());
			AElement.Properties.AddOrUpdate("ColumnName", AElement.ColumnName);
		
			return AElement;
		}
		
		protected virtual ContainerElement CreateContainerElement(ContainerElement AElement, ElaboratedGroup AGroup, string ATitleSeed, string APageType, bool AIsReadOnly)
		{
			MetaData LMetaData = new MetaData(AGroup.Properties);
			PrepareElement(AElement, LMetaData, null, ATitleSeed, APageType, AIsReadOnly);
			
			AElement.Title = DerivationUtility.GetTag(LMetaData, "Title", APageType, AGroup.UnqualifiedName);
			if (!Boolean.Parse(DerivationUtility.GetTag(LMetaData, "Visible", APageType, "True")))
				AElement.Properties.AddOrUpdate("Visible", "False");
				
			return AElement;
		}
		
		protected virtual Element BuildColumnElement(ElaboratedTableVarColumn AColumn, string APageType, string ATitleSeed, bool AReadOnly)
		{
			ColumnElement LColumnElement = new ColumnElement(String.Format("{0}Column{1}", FDerivationInfo.MainSourceName, AColumn.ElaboratedName));
			LColumnElement.ElementType = DerivationUtility.GetTag(AColumn.Column.MetaData, "ElementType", APageType, GetDefaultElementType(AColumn.Column.DataType, APageType));
			LColumnElement.Source = FDerivationInfo.MainSourceName;
			LColumnElement.ColumnName = Schema.Object.Qualify(AColumn.Column.Name, AColumn.ElaboratedTableVar.ElaboratedName);
			CreateColumnElement(LColumnElement, AColumn, AColumn.Column, ATitleSeed, APageType, AReadOnly);
			AddElement(LColumnElement, AColumn.GroupName, ATitleSeed, APageType, AReadOnly);
			return LColumnElement;
		}
		
		protected virtual void BuildQuickLookup
		(
			ElaboratedTableVarColumn AColumn,
			ElaboratedReference AReference, 
			string[] AColumnNames,
			string[] ALookupColumnNames,
			string[] AMasterKeyNames,
			string[] ADetailKeyNames,
			string APageType, 
			string ATitleSeed, 
			bool AReadOnly
		)
		{
			// Prepare the quick lookup group
			if (AReference.IsEmbedded) 
			{
				string LLookupGroupName = FDerivationInfo.ElaboratedExpression.Groups.ResolveGroupName(String.Format("{0}{1}", AReference.ElaboratedName, "Group"));
				EnsureGroups(LLookupGroupName, ATitleSeed, APageType, AReadOnly);
				GroupElement LGroupElement = FRootElement.FindElement(LLookupGroupName) as GroupElement;

				MetaData LGroupMetaData = DerivationUtility.ExtractTags(AReference.Reference.MetaData.Tags, LGroupElement.ElementType, APageType, AReference.ReferenceType.ToString());
				PrepareElement(LGroupElement, LGroupMetaData, AReference.Reference.MetaData, ATitleSeed, APageType, AReference.ReferenceType.ToString(), AReadOnly);
			}
			LookupColumnElement LLookupGroup = new LookupColumnElement(String.Format("{0}Column{1}_Lookup", FDerivationInfo.MainSourceName, AColumn.ElaboratedName));
			LLookupGroup.ElementType = DerivationUtility.GetTag(AReference.Reference.MetaData, "ElementType", APageType, AReference.ReferenceType.ToString(), "QuickLookup");
			DerivationUtility.ExtractProperties(AReference.Reference.MetaData, LLookupGroup.ElementType, APageType, LLookupGroup.Properties);
			LLookupGroup.Source = FDerivationInfo.MainSourceName;
			LLookupGroup.Properties.Add(new Tag("Source", LLookupGroup.Source));
			LLookupGroup.ColumnName = Schema.Object.Qualify(AColumn.Column.Name, AColumn.ElaboratedTableVar.ElaboratedName);
			LLookupGroup.Properties.Add(new Tag("ColumnName", LLookupGroup.ColumnName));
			LLookupGroup.LookupColumnName = Schema.Object.Qualify(AReference.Reference.TargetKey.Columns[AReference.Reference.SourceKey.Columns.IndexOf(AColumn.Column.Name)].Name, DerivationUtility.CMainElaboratedTableName);
			LLookupGroup.Properties.Add(new Tag("LookupColumnName", LLookupGroup.LookupColumnName));
			LLookupGroup.LookupDocument = GetLookupDocument(AReference, AMasterKeyNames, ADetailKeyNames);
			LLookupGroup.Properties.Add(new Tag("Document", LLookupGroup.LookupDocument));
			if (AMasterKeyNames.Length > 0)
			{
				LLookupGroup.MasterKeyNames = StringArrayToNames(AMasterKeyNames);
				LLookupGroup.Properties.Add(new Tag("MasterKeyNames", LLookupGroup.MasterKeyNames));
				LLookupGroup.DetailKeyNames = StringArrayToNames(ADetailKeyNames);
				LLookupGroup.Properties.Add(new Tag("DetailKeyNames", LLookupGroup.DetailKeyNames));
			}

			if (AColumn.ReadOnly || AReadOnly || Convert.ToBoolean(DerivationUtility.GetTag(AColumn.Column.MetaData, "ReadOnly", APageType, "False")))
				LLookupGroup.Properties.AddOrUpdate("ReadOnly", "True");

			MetaData LMetaData = DerivationUtility.ExtractTags(AReference.Reference.MetaData.Tags, LLookupGroup.ElementType, APageType, AReference.ReferenceType.ToString());
			PrepareElement(LLookupGroup, LMetaData, AReference.Reference.MetaData, ATitleSeed, APageType, AReference.ReferenceType.ToString(), AReadOnly);

			if (!Boolean.Parse(DerivationUtility.GetTag(LMetaData, "Visible", APageType, "True")))
				LLookupGroup.Properties.AddOrUpdate("Visible", "False");
			
			AddElement(LLookupGroup, AColumn.GroupName, ATitleSeed, APageType, AReadOnly);
			
			// Build the element
			AColumn.GroupName = LLookupGroup.Name;
			Element LControlElement = BuildColumnElement(AColumn, APageType, ATitleSeed, AReadOnly);
			if (!LControlElement.Properties.Contains("TitleAlignment"))
				switch (LControlElement.ElementType)
				{
					case "TextBox" :
					case "DateTimeBox" :
					case "NumericTextBox" : LControlElement.Properties.Add(new Tag("TitleAlignment", "None")); break;
				}
			
			// Use the column's title for the group (only in the case of a quick lookup)
			LLookupGroup.Title = LControlElement.Title;
			
			// If the control has a flow break specified, push it onto the lookup group (only in the case of a quick lookup)
			if (LControlElement.FlowBreak)
				LLookupGroup.FlowBreak = true;
		}

		protected virtual void BuildFullLookup
		(
			ElaboratedReference AReference,
			string[] AColumnNames,
			string[] ALookupColumnNames,
			string[] AMasterKeyNames,
			string[] ADetailKeyNames,
			string APageType, 
			string ATitleSeed, 
			bool AReadOnly
		)
		{
			if (AReference.IsEmbedded) 
			{
				string LLookupGroupName = FDerivationInfo.ElaboratedExpression.Groups.ResolveGroupName(String.Format("{0}{1}", AReference.ElaboratedName, "Group"));
				LookupGroupElement LLookupGroupElement = FRootElement.FindElement(LLookupGroupName) as LookupGroupElement;
				if (LLookupGroupElement == null)
				{
					LLookupGroupElement = new LookupGroupElement(LLookupGroupName);
					LLookupGroupElement.ElementType = DerivationUtility.GetTag(AReference.GroupMetaData, "ElementType", APageType, AReference.ReferenceType.ToString(), DerivationUtility.GetTag(AReference.Reference.MetaData, "ElementType", APageType, AReference.ReferenceType.ToString(), "FullLookup"));

					CreateContainerElement(LLookupGroupElement, FDerivationInfo.ElaboratedExpression.Groups[LLookupGroupName], ATitleSeed, APageType, AReadOnly);

					MetaData LMetaData = DerivationUtility.ExtractTags(AReference.Reference.MetaData.Tags, LLookupGroupElement.ElementType, APageType, AReference.ReferenceType.ToString());
					PrepareElement(LLookupGroupElement, LMetaData, AReference.Reference.MetaData, ATitleSeed, APageType, AReference.ReferenceType.ToString(), AReadOnly);

					LLookupGroupElement.Source = FDerivationInfo.MainSourceName;
					LLookupGroupElement.Properties.Add(new Tag("Source", LLookupGroupElement.Source));
					LLookupGroupElement.ColumnNames = StringArrayToNames(AColumnNames);
					LLookupGroupElement.Properties.Add(new Tag("ColumnNames", LLookupGroupElement.ColumnNames));
					LLookupGroupElement.LookupColumnNames = StringArrayToNames(ALookupColumnNames);
					LLookupGroupElement.Properties.Add(new Tag("LookupColumnNames", LLookupGroupElement.LookupColumnNames));
					LLookupGroupElement.LookupDocument = GetLookupDocument(AReference, AMasterKeyNames, ADetailKeyNames);
					LLookupGroupElement.Properties.Add(new Tag("Document", LLookupGroupElement.LookupDocument));
					if (AMasterKeyNames.Length > 0)
					{
						LLookupGroupElement.MasterKeyNames = StringArrayToNames(AMasterKeyNames);
						LLookupGroupElement.Properties.Add(new Tag("MasterKeyNames", LLookupGroupElement.MasterKeyNames));
						LLookupGroupElement.DetailKeyNames = StringArrayToNames(ADetailKeyNames);
						LLookupGroupElement.Properties.Add(new Tag("DetailKeyNames", LLookupGroupElement.DetailKeyNames));
					}
					AddElement(LLookupGroupElement, GetParentGroupName(LLookupGroupName), ATitleSeed, APageType, AReadOnly);
				}
			}
		}
		
		protected virtual void BuildColumn(ElaboratedTableVarColumn AColumn)
		{
			string LTitleSeed = AColumn.GetTitleSeed();
			string LPageType = FDerivationInfo.PageType;

			if ((AColumn.ElaboratedTableVar.ElaboratedReference != null) && (AColumn.ElaboratedTableVar.ElaboratedReference.ReferenceType == ReferenceType.Lookup))
				LPageType = DerivationUtility.CPreview;
				
			if 
			(
				!DerivationUtility.IsReadOnlyPageType(LPageType) &&
				(AColumn.ElaboratedReference != null) && 
				(AColumn.ElaboratedReference.ReferenceType == ReferenceType.Lookup) && 
				(
					(AColumn.ElaboratedReference.SourceElaboratedTableVar.ElaboratedReference == null) ||
					(AColumn.ElaboratedReference.SourceElaboratedTableVar.ElaboratedReference.ReferenceType == ReferenceType.Extension)
				)
			)
			{
				int LVisibleCount = 0;
				List<string> LMasterColumns = new List<string>();
				List<string> LColumns = new List<string>();
				foreach (ElaboratedTableVarColumn LReferenceColumn in AColumn.ElaboratedReference.Columns)
				{
					if (LReferenceColumn.IsMaster)
						LMasterColumns.Add(LReferenceColumn.Column.Name);
					else
						LColumns.Add(LReferenceColumn.Column.Name);
						
					if (LReferenceColumn.Visible)
						LVisibleCount++;
				}
				
				string[] LColumnNames = new string[LColumns.Count];
				string[] LLookupColumnNames = new string[LColumns.Count];
				string[] LMasterKeyNames = new string[LMasterColumns.Count];
				string[] LDetailKeyNames = new string[LMasterColumns.Count];

				int LColumnIndex = 0;
				int LMasterColumnIndex = 0;
				for (int LIndex = 0; LIndex < AColumn.ElaboratedReference.Reference.SourceKey.Columns.Count; LIndex++)
				{
					if (LColumns.Contains(AColumn.ElaboratedReference.Reference.SourceKey.Columns[LIndex].Name))
					{
						LColumnNames[LColumnIndex] = Schema.Object.Qualify(AColumn.ElaboratedReference.Reference.SourceKey.Columns[LIndex].Name, AColumn.ElaboratedTableVar.ElaboratedName);
						LLookupColumnNames[LColumnIndex] = Schema.Object.Qualify(AColumn.ElaboratedReference.Reference.TargetKey.Columns[LIndex].Name, DerivationUtility.CMainElaboratedTableName);
						LColumnIndex++;
					}
					else
					{
						LMasterKeyNames[LMasterColumnIndex] = Schema.Object.Qualify(AColumn.ElaboratedReference.Reference.SourceKey.Columns[LIndex].Name, AColumn.ElaboratedTableVar.ElaboratedName);
						LDetailKeyNames[LMasterColumnIndex] = Schema.Object.Qualify(AColumn.ElaboratedReference.Reference.TargetKey.Columns[LIndex].Name, DerivationUtility.CMainElaboratedTableName);
						LMasterColumnIndex++;
					}
				}				

				bool LUseFullLookup = (LVisibleCount != 1) || Convert.ToBoolean(DerivationUtility.GetTag(AColumn.ElaboratedReference.Reference.MetaData, "UseFullLookup", FDerivationInfo.PageType, AColumn.ElaboratedReference.ReferenceType.ToString(), "False"));
				
				if (LUseFullLookup)
				{
					BuildFullLookup(AColumn.ElaboratedReference, LColumnNames, LLookupColumnNames, LMasterKeyNames, LDetailKeyNames, LPageType, LTitleSeed, AColumn.ReadOnly); 
					if (AColumn.Visible)
						BuildColumnElement(AColumn, LPageType, LTitleSeed, AColumn.ReadOnly);
				}
				else
					if (AColumn.Visible)
						BuildQuickLookup(AColumn, AColumn.ElaboratedReference, LColumnNames, LLookupColumnNames, LMasterKeyNames, LDetailKeyNames, LPageType, LTitleSeed, AColumn.ReadOnly);
			}
			else
			{
				if (AColumn.Visible)
					BuildColumnElement(AColumn, LPageType, LTitleSeed, AColumn.ReadOnly);
			}
		}
	}
	
	public class SingularStructureBuilder : StructureBuilder
	{
		public SingularStructureBuilder(DerivationInfo ADerivationInfo) : base(ADerivationInfo) {}

		/// <summary> Look for column lookups grouped with a single column and apply a flowbreak. </summary>
 		protected virtual void CleanupColumnLookups(Element AElement)
		{
			ContainerElement LElement = AElement as ContainerElement;
			if (LElement != null)
			{
				if 
				(
					(LElement.Elements.Count == 2) && 
					(LElement.Elements[0] is LookupColumnElement) && 
					(
						(LElement.Elements[1] is ColumnElement) ||
						(
							(LElement.Elements[1] is GroupElement) &&
							(((GroupElement)LElement.Elements[1]).EliminateGroup)
						)
					)
				)
					LElement.Elements[0].FlowBreak = true;

				for (int i = 0; i < LElement.Elements.Count; i++)
					CleanupColumnLookups(LElement.Elements[i]);
			}
		}

		/// <summary> Applies any changes to the completed element hierarchy. </summary>
		protected virtual void Cleanup()
		{
			CleanupColumnLookups(FRootElement);
		}

		public override Element Build()
		{
			FRootElement = new GroupElement("Root");
			foreach (ElaboratedTableVarColumn LColumn in FDerivationInfo.ElaboratedExpression.Columns)
				BuildColumn(LColumn);

			Cleanup();
			return FRootElement;
		}
	}
	
	public class PluralStructureBuilder : StructureBuilder
	{
		public PluralStructureBuilder(DerivationInfo ADerivationInfo) : base(ADerivationInfo) {}

		protected override string GetDefaultElementType(Schema.IDataType ADataType, string APageType)
		{
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (LScalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsBoolean.NativeType)
					return "CheckBoxColumn";
			}
			
			return "TextColumn";
		}

		protected override Element BuildColumnElement(ElaboratedTableVarColumn AColumn, string APageType, string ATitleSeed, bool AReadOnly)
		{
			GridColumnElement LGridColumnElement = new GridColumnElement(String.Format("{0}GridColumn{1}", FDerivationInfo.MainSourceName, AColumn.ElaboratedName));
			if (AColumn.ElaboratedReference == null)
				LGridColumnElement.ColumnName = AColumn.ElaboratedName;
			else
				LGridColumnElement.ColumnName = Schema.Object.Qualify(AColumn.Column.Name, AColumn.ElaboratedTableVar.ElaboratedName);
			LGridColumnElement.ElementType = DerivationUtility.GetTag(AColumn.Column.MetaData, "Grid.ElementType", APageType, GetDefaultElementType(AColumn.Column.DataType, APageType));
			CreateGridColumnElement(LGridColumnElement, AColumn.Column, ATitleSeed, APageType, false);
			AddElement(LGridColumnElement, AColumn.GroupName, ATitleSeed, APageType, false);
			return LGridColumnElement;
		}

		protected override void BuildQuickLookup
		(
			ElaboratedTableVarColumn AColumn,
			ElaboratedReference AReference, 
			string[] AColumnNames,
			string[] ALookupColumnNames,
			string[] AMasterKeyNames,
			string[] ADetailKeyNames,
			string APageType, 
			string ATitleSeed, 
			bool AReadOnly
		)
		{
			if (AReference.IsEmbedded) 
			{
				string LLookupGroupName = FDerivationInfo.ElaboratedExpression.Groups.ResolveGroupName(String.Format("{0}{1}", AReference.ElaboratedName, "Group"));
				EnsureGroups(LLookupGroupName, ATitleSeed, APageType, AReadOnly);
			}
			BuildColumnElement(AColumn, APageType, ATitleSeed, AReadOnly);
		}
		
		public override Element Build()
		{
			GridElement LGridElement = new GridElement("Grid");
			FRootElement = LGridElement;
			LGridElement.ElementType = DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Grid.ElementType", FDerivationInfo.PageType, "Grid");
			LGridElement.Source = FDerivationInfo.MainSourceName;
			CreateGridElement(LGridElement, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar, String.Empty, FDerivationInfo.PageType, FDerivationInfo.IsReadOnly);

			foreach (ElaboratedTableVarColumn LColumn in FDerivationInfo.ElaboratedExpression.Columns)
				BuildColumn(LColumn);

			return FRootElement;
		}
	}
	
	public class SearchStructureBuilder : StructureBuilder
	{
		public SearchStructureBuilder(DerivationInfo ADerivationInfo) : base(ADerivationInfo) {}
		
		public string GetOrderLookupDocument()
		{
			return
				DerivationUtility.GetTag
				(
					FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData,
					"Document",
					DerivationUtility.COrderBrowse,
					FDerivationInfo.BuildDerivationExpression
					(
						DerivationUtility.COrderBrowse,
						DerivationUtility.GetTag
						(
							FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
							"Query", 
							DerivationUtility.COrderBrowse, 
							FDerivationInfo.Query
						),
						FDerivationInfo.MasterKeyNames,
						FDerivationInfo.DetailKeyNames,
						Boolean.Parse
						(
							DerivationUtility.GetTag
							(
								FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
								"Elaborate", 
								DerivationUtility.COrderBrowse, 
								FDerivationInfo.Elaborate.ToString()
							)
						)
					)
				);
		}

		public override Element Build()
		{
			SearchElement LSearch = new SearchElement(String.Format("{0}Search", FDerivationInfo.MainSourceName));
			FRootElement = LSearch;
			LSearch.ElementType = DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Search.ElementType", FDerivationInfo.PageType, "Search");
			LSearch.Source = FDerivationInfo.MainSourceName;
			CreateSearchElement(LSearch, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar, String.Empty, FDerivationInfo.PageType, false);

			List<string> LSearchColumnNames = new List<string>();
			SearchColumnElement LSearchColumn;
			foreach (Schema.Order LOrder in FDerivationInfo.TableVar.Orders)
				if (Convert.ToBoolean(DerivationUtility.GetTag(LOrder.MetaData, "Visible", FDerivationInfo.PageType, "True")))
					foreach (Schema.OrderColumn LOrderColumn in LOrder.Columns)
					{
						string LOrderColumnName = Schema.Object.Qualify(LOrderColumn.Column.Name, DerivationUtility.CMainElaboratedTableName);
						if (!LSearchColumnNames.Contains(LOrderColumnName))
						{
							try
							{
								LSearchColumnNames.Add(LOrderColumnName);
								LSearchColumn = new SearchColumnElement(String.Format("{0}SearchColumn{1}", FDerivationInfo.MainSourceName, LOrderColumnName));
								LSearchColumn.ColumnName = LOrderColumnName;
								LSearchColumn.ElementType =	DerivationUtility.GetTag(LOrderColumn.Column.MetaData, "Search.ElementType", FDerivationInfo.PageType, "SearchColumn");
								CreateSearchColumnElement(LSearchColumn, LOrderColumn.Column, FDerivationInfo.ElaboratedExpression.Columns[LOrderColumnName].GetTitleSeed(), FDerivationInfo.PageType, false);
								LSearch.Elements.Add(LSearchColumn);
							}
							catch (Exception E)
							{
								throw new ServerException(ServerException.Codes.CannotConstructSearchColumn, E, LOrderColumnName, LOrder.Name);
							}
						}
					}

			foreach (Schema.Key LKey in FDerivationInfo.TableVar.Keys)
				foreach (Schema.TableVarColumn LColumn in LKey.Columns)
				{
					string LKeyColumnName = Schema.Object.Qualify(LColumn.Name, DerivationUtility.CMainElaboratedTableName);
					if (!LSearchColumnNames.Contains(LKeyColumnName))
					{
						try
						{
							LSearchColumnNames.Add(LKeyColumnName);
							LSearchColumn = new SearchColumnElement(String.Format("{0}SearchColumn{1}", FDerivationInfo.MainSourceName, LKeyColumnName));
							LSearchColumn.ColumnName = LKeyColumnName;
							LSearchColumn.ElementType = DerivationUtility.GetTag(LColumn.MetaData, "Search.ElementType", FDerivationInfo.PageType, "SearchColumn");
							CreateSearchColumnElement(LSearchColumn, LColumn, FDerivationInfo.ElaboratedExpression.Columns[LKeyColumnName].GetTitleSeed(), FDerivationInfo.PageType, false);
							LSearch.Elements.Add(LSearchColumn);
						}
						catch (Exception E)
						{
							throw new ServerException(ServerException.Codes.CannotConstructSearchColumn, E, LKeyColumnName, LKey.Name);
						}
					}
				}
				
			return FRootElement;
		}
	}
}

