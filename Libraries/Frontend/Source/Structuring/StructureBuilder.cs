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
		public readonly static Char[] ColumnNameDelimiters = new Char[] {',',';'};

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

		public string BuildImageExpression(string imageName)
		{
			return String.Format
			(
				"Image('Frontend', 'Image.{0}')",
				imageName
			);
		}

		public string BuildDerivationExpression(string pageType, string query, string masterKeyNames, string detailKeyNames)
		{
			return BuildDerivationExpression(pageType, query, masterKeyNames, detailKeyNames, true);
		}

		public string BuildDerivationExpression(string pageType, string query, string masterKeyNames, string detailKeyNames, bool elaborate)
		{
			return BuildDerivationExpression
			(
				new DerivationSeed
				(
					pageType,
					query,
					elaborate,
					masterKeyNames,
					detailKeyNames
				)
			);
		}

		/// <summary> Builds a derivation document expression. </summary>
		/// <returns> The document expression to use. </returns>
		public string BuildDerivationExpression(DerivationSeed seed)
		{
			return String.Format
			(
				".Frontend.Derive('{0}', '{1}', '{2}', '{3}', {4})",
				seed.Query.Replace("'", "''"),
				seed.PageType,
				seed.MasterKeyNames,
				seed.DetailKeyNames,
				Convert.ToString(seed.Elaborate).ToLower()
			);
		}
	}
	
	public abstract class StructureBuilder : System.Object
	{
		public StructureBuilder(DerivationInfo derivationInfo) : base()
		{
			_derivationInfo = derivationInfo;
		}
		
		protected ContainerElement _rootElement;

		protected DerivationInfo _derivationInfo;
		
		public abstract Element Build();
		
		protected string GetParentGroupName(string groupName)
		{
			int index = groupName.LastIndexOf('\\');
			if (index >= 0)
				return groupName.Substring(0, index);
			else
				return String.Empty;
		}
		
		protected void EnsureGroups(string groupName, string titleSeed, string pageType, bool isReadOnly)
		{
			string[] groupNames = groupName.Split('\\');
			StringBuilder localGroupName = new StringBuilder();
			for (int index = 0; index < groupNames.Length; index++)
			{
				if (localGroupName.Length > 0)
					localGroupName.Append("\\");
				localGroupName.Append(groupNames[index]);
				EnsureGroup(localGroupName.ToString(), titleSeed, pageType, isReadOnly);
			}
		}
		
		protected void EnsureGroup(string groupName, string titleSeed, string pageType, bool isReadOnly)
		{
			if (groupName != String.Empty)
			{
				GroupElement groupElement = _rootElement.FindElement(groupName) as GroupElement;
				if (groupElement == null)
				{
					ElaboratedGroup group = _derivationInfo.ElaboratedExpression.Groups[groupName];
					groupElement = new GroupElement(groupName);
					MetaData groupMetaData = new MetaData(group.Properties);
					groupElement.ElementType = DerivationUtility.GetTag(groupMetaData, "ElementType", pageType, "Group");
					groupElement.EliminateGroup = Boolean.Parse(DerivationUtility.GetTag(groupMetaData, "EliminateGroup", pageType, "True"));
					CreateContainerElement(groupElement, _derivationInfo.ElaboratedExpression.Groups[groupName], titleSeed, pageType, isReadOnly);
					AddElement(groupElement, GetParentGroupName(groupName), titleSeed, pageType, isReadOnly);
				}
			}
		}
		
		protected void AddElement(Element element, string parentGroupName, string titleSeed, string pageType, bool isReadOnly)
		{
			EnsureGroups(parentGroupName, titleSeed, pageType, isReadOnly);
			if (parentGroupName == String.Empty)
				_rootElement.Elements.Add(element);
			else
				((GroupElement)_rootElement.FindElement(parentGroupName)).Elements.Add(element);
		}
		
		protected virtual string GetDefaultElementType(Schema.IDataType dataType, string pageType)
		{
			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			
			if (scalarType != null)
			{
				if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsBoolean.NativeType)
					return "CheckBox";

				if (!DerivationUtility.IsReadOnlyPageType(pageType))					
				{
					if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsDateTime.NativeType)
						return "DateTimeBox";
						
					if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsDecimal.NativeType)
						return "NumericTextBox";
						
					if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsInt64.NativeType)
						return "NumericTextBox";

					if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsInt32.NativeType)
						return "NumericTextBox";

					if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsInt16.NativeType)
						return "NumericTextBox";

					if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsByte.NativeType)
						return "NumericTextBox";
				}
			}
			
			return "TextBox";
		}
		
		protected string StringArrayToNames(string[] array)
		{
			StringBuilder result = new StringBuilder();
			for (int index = 0; index < array.Length; index++)
			{
				if (index > 0)
					result.Append(";");
				result.Append(array[index]);
			}
			return result.ToString();
		}
		
		protected string GetLookupDocument(ElaboratedReference reference, string[] masterKeyNames, string[] detailKeyNames)
		{
			string pageType =
				Boolean.Parse
				(
					DerivationUtility.GetTag
					(
						reference.Reference.MetaData,
						"UseList",
						"",
						reference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							reference.TargetElaboratedTableVar.TableVar.MetaData, 
							"UseList", 
							"False"
						)
					)
				) ? DerivationUtility.List : DerivationUtility.Browse;

			string query =
				DerivationUtility.GetTag
				(
					reference.Reference.MetaData, 
					"Query", 
					pageType, 
					reference.ReferenceType.ToString(), 
					DerivationUtility.GetTag
					(
						reference.TargetElaboratedTableVar.TableVar.MetaData, 
						"Query", 
						pageType,
						reference.Reference.TargetTable.Name
					)
				);
				
			string localMasterKeyNames =
				DerivationUtility.GetTag
				(
					reference.Reference.MetaData,
					"MasterKeyNames",
					pageType,
					reference.ReferenceType.ToString(),
					StringArrayToNames(masterKeyNames)
				);
				
			string localDetailKeyNames =
				DerivationUtility.GetTag
				(
					reference.Reference.MetaData,
					"DetailKeyNames",
					pageType,
					reference.ReferenceType.ToString(),
					StringArrayToNames(detailKeyNames)
				);

			bool elaborate = 
				Boolean.Parse
				(
					DerivationUtility.GetTag
					(
						reference.Reference.MetaData,
						"Elaborate",
						pageType,
						reference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							reference.TargetElaboratedTableVar.TableVar.MetaData, 
							"Elaborate", 
							pageType, 
							"True"
						)
					)
				);
				
			if (localMasterKeyNames != String.Empty)
			{
				return
					DerivationUtility.GetTag
					(
						reference.Reference.MetaData,
						"Document",
						pageType,
						reference.ReferenceType.ToString(),
						_derivationInfo.BuildDerivationExpression
						(
							pageType, 
							query,
							localMasterKeyNames,
							localDetailKeyNames,
							elaborate
						)
					);
			}
			else
			{
				return 
					DerivationUtility.GetTag
					(
						reference.Reference.MetaData,
						"Document",
						pageType,
						reference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							reference.TargetElaboratedTableVar.TableVar.MetaData,
							"Document",
							pageType,
							_derivationInfo.BuildDerivationExpression
							(
								pageType, 
								query,
								"",
								"",
								elaborate
							)
						)
					);
			}
		}
		
		protected void PrepareElement(Element element, MetaData extractedMetaData, MetaData completeMetaData, string titleSeed, string pageType, bool readOnly)
		{
			PrepareElement(element, extractedMetaData, completeMetaData, titleSeed, pageType, String.Empty, readOnly);
		}
		
		protected virtual void PrepareElement(Element element, MetaData extractedMetaData, MetaData completeMetaData, string titleSeed, string pageType, string referenceType, bool isReadOnly)
		{
			// Common element properties:
			// Title
			// Hint
			// Priority
			// Flow
			// FlowBreak
			// Break
			// Properties
			element.Hint = DerivationUtility.GetTag(extractedMetaData, "Hint", pageType, referenceType, DerivationUtility.GetTag(completeMetaData, "Hint", pageType, referenceType, String.Empty));
			element.Priority = Convert.ToInt32(DerivationUtility.GetTag(extractedMetaData, "Priority", pageType, referenceType, DerivationUtility.GetTag(completeMetaData, "Priority", pageType, referenceType, DerivationUtility.DefaultPriority)));
			element.Flow = (Flow)Enum.Parse(typeof(Flow), DerivationUtility.GetTag(extractedMetaData, "Flow", pageType, referenceType, DerivationUtility.GetTag(completeMetaData, "Flow", pageType, referenceType, Flow.Default.ToString())));
			element.FlowBreak = Convert.ToBoolean(DerivationUtility.GetTag(extractedMetaData, "FlowBreak", pageType, referenceType, DerivationUtility.GetTag(completeMetaData, "FlowBreak", pageType, referenceType, "False")));
			element.Break = Convert.ToBoolean(DerivationUtility.GetTag(extractedMetaData, "Break", pageType, referenceType, DerivationUtility.GetTag(completeMetaData, "Break", pageType, referenceType, "False")));

			if (referenceType == String.Empty)
			{
				DerivationUtility.ExtractProperties(completeMetaData, element.ElementType, pageType, element.Properties);			
				DerivationUtility.ExtractProperties(extractedMetaData, element.ElementType, pageType, element.Properties);
			}
			else
			{
				DerivationUtility.ExtractProperties(completeMetaData, element.ElementType, pageType, referenceType, element.Properties);			
				DerivationUtility.ExtractProperties(extractedMetaData, element.ElementType, pageType, referenceType, element.Properties);
			}
		}
		
		protected virtual ColumnElement CreateColumnElement(ColumnElement element, ElaboratedTableVarColumn elaboratedColumn, Schema.TableVarColumn column, string titleSeed, string pageType, bool isReadOnly)
		{
			PrepareElement(element, column.MetaData, null, titleSeed, pageType, isReadOnly);
			
			if 
			(
				(((Schema.ScalarType)column.DataType).NativeType == DAE.Runtime.Data.NativeAccessors.AsString.NativeType) 
					&& (elaboratedColumn.ElaboratedReference == null)
					&& !element.Properties.Contains("NilIfBlank")
					&& (element.ElementType != "Choice") // TODO: Control types: need a better mechanism for determining whether or not a particular property applies to a particular control type...
			)
			{
				element.Properties.AddOrUpdate("NilIfBlank", "False");
			}

			element.Title = DerivationUtility.GetTag(column.MetaData, "Caption", pageType, DerivationUtility.GetTag(column.MetaData, "Title", pageType, Schema.Object.Unqualify(column.Name)));
			string width = DerivationUtility.GetTag(column.MetaData, "Width", pageType, String.Empty);
			if (width != String.Empty)
				element.Properties.AddOrUpdate("Width", width);
			element.Properties.AddOrUpdate("Source", element.Source);
			element.Properties.AddOrUpdate("ColumnName", element.ColumnName);
			if (column.ReadOnly || isReadOnly || Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "ReadOnly", pageType, "False")))
				element.Properties.AddOrUpdate("ReadOnly", "True");

			return element;
		}
		
		protected virtual GridElement CreateGridElement(GridElement element, Schema.TableVar tableVar, string titleSeed, string pageType, bool isReadOnly)
		{
			MetaData metaData = null;
			if (tableVar.MetaData != null)
				metaData = DerivationUtility.ExtractTags(tableVar.MetaData.Tags, "Grid", pageType);
			PrepareElement(element, metaData, tableVar.MetaData, titleSeed, pageType, isReadOnly);
			
			//AElement.Title = ATitleSeed + DerivationUtility.GetTag(LMetaData, "Title", APageType, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle); // grid has no title
			element.Hint = DerivationUtility.GetTag(metaData, "Hint", pageType, DerivationUtility.GetTag(tableVar.MetaData, "Hint", pageType, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle));
			element.Properties.AddOrUpdate("Source", element.Source);
			//if (AIsReadOnly)
			//	AElement.Properties.AddOrUpdate("ReadOnly", "True");
			
			return element;
		}
		
		protected virtual GridColumnElement CreateGridColumnElement(GridColumnElement element, Schema.TableVarColumn column, string titleSeed, string pageType, bool isReadOnly)
		{
			MetaData metaData = null;
			if (column.MetaData != null)
				metaData = DerivationUtility.ExtractTags(column.MetaData.Tags, "Grid", pageType);
			PrepareElement(element, metaData, column.MetaData, titleSeed, pageType, isReadOnly);
			
			element.Title = DerivationUtility.GetTag(metaData, "Caption", pageType, (column.ColumnType == Schema.TableVarColumnType.RowExists ? String.Empty : titleSeed) + DerivationUtility.GetTag(metaData, "Title", pageType, DerivationUtility.GetTag(column.MetaData, "Title", pageType, Schema.Object.Unqualify(column.Name))));
			element.Hint = DerivationUtility.GetTag(metaData, "Hint", pageType, DerivationUtility.GetTag(column.MetaData, "Hint", pageType, String.Empty));
			element.Width = Convert.ToInt32(DerivationUtility.GetTag(metaData, "Width", pageType, DerivationUtility.GetTag(column.MetaData, "Width", pageType, (20).ToString())));
			element.Properties.AddOrUpdate("Width", element.Width.ToString());
			element.Properties.AddOrUpdate("ColumnName", element.ColumnName);

			return element;
		}
		
		protected virtual SearchElement CreateSearchElement(SearchElement element, Schema.TableVar tableVar, string titleSeed, string pageType, bool isReadOnly)
		{
			MetaData metaData = null;
			if (tableVar.MetaData != null)
				metaData = DerivationUtility.ExtractTags(tableVar.MetaData.Tags, "Search", pageType);
			PrepareElement(element, metaData, tableVar.MetaData, titleSeed, pageType, isReadOnly);
			
			element.Hint = DerivationUtility.GetTag(metaData, "Hint", pageType, "Search for a specific row.");
			element.Properties.AddOrUpdate("Source", element.Source);
		
			return element;
		}
		
		protected virtual SearchColumnElement CreateSearchColumnElement(SearchColumnElement element, Schema.TableVarColumn column, string titleSeed, string pageType, bool isReadOnly)
		{
			MetaData metaData = null;
			if (column.MetaData != null)
				metaData = DerivationUtility.ExtractTags(column.MetaData.Tags, "Search", pageType);
			PrepareElement(element, metaData, column.MetaData, titleSeed, pageType, isReadOnly);
			
			element.Title = DerivationUtility.GetTag(metaData, "Caption", pageType, (column.ColumnType == Schema.TableVarColumnType.RowExists ? String.Empty : titleSeed) + DerivationUtility.GetTag(metaData, "Title", pageType, DerivationUtility.GetTag(column.MetaData, "Title", pageType, Schema.Object.Unqualify(column.Name))));
			element.Hint = DerivationUtility.GetTag(metaData, "Hint", pageType, DerivationUtility.GetTag(column.MetaData, "Hint", pageType, String.Empty));
			element.Width = Convert.ToInt32(DerivationUtility.GetTag(metaData, "Width", pageType, DerivationUtility.GetTag(column.MetaData, "Width", pageType, (20).ToString())));
			element.Properties.AddOrUpdate("Width", element.Width.ToString());
			element.Properties.AddOrUpdate("ColumnName", element.ColumnName);
		
			return element;
		}
		
		protected virtual ContainerElement CreateContainerElement(ContainerElement element, ElaboratedGroup group, string titleSeed, string pageType, bool isReadOnly)
		{
			MetaData metaData = new MetaData(group.Properties);
			PrepareElement(element, metaData, null, titleSeed, pageType, isReadOnly);
			
			element.Title = DerivationUtility.GetTag(metaData, "Title", pageType, group.UnqualifiedName);
			if (!Boolean.Parse(DerivationUtility.GetTag(metaData, "Visible", pageType, "True")))
				element.Properties.AddOrUpdate("Visible", "False");
				
			return element;
		}
		
		protected virtual Element BuildColumnElement(ElaboratedTableVarColumn column, string pageType, string titleSeed, bool readOnly)
		{
			ColumnElement columnElement = new ColumnElement(String.Format("{0}Column{1}", _derivationInfo.MainSourceName, column.ElaboratedName));
			columnElement.ElementType = DerivationUtility.GetTag(column.Column.MetaData, "ElementType", pageType, GetDefaultElementType(column.Column.DataType, pageType));
			columnElement.Source = _derivationInfo.MainSourceName;
			columnElement.ColumnName = Schema.Object.Qualify(column.Column.Name, column.ElaboratedTableVar.ElaboratedName);
			CreateColumnElement(columnElement, column, column.Column, titleSeed, pageType, readOnly);
			AddElement(columnElement, column.GroupName, titleSeed, pageType, readOnly);
			return columnElement;
		}
		
		protected virtual void BuildQuickLookup
		(
			ElaboratedTableVarColumn column,
			ElaboratedReference reference, 
			string[] columnNames,
			string[] lookupColumnNames,
			string[] masterKeyNames,
			string[] detailKeyNames,
			string pageType, 
			string titleSeed, 
			bool readOnly
		)
		{
			// Prepare the quick lookup group
			if (reference.IsEmbedded) 
			{
				string lookupGroupName = _derivationInfo.ElaboratedExpression.Groups.ResolveGroupName(String.Format("{0}{1}", reference.ElaboratedName, "Group"));
				EnsureGroups(lookupGroupName, titleSeed, pageType, readOnly);
				GroupElement groupElement = _rootElement.FindElement(lookupGroupName) as GroupElement;

				MetaData groupMetaData = DerivationUtility.ExtractTags(reference.Reference.MetaData, groupElement.ElementType, pageType, reference.ReferenceType.ToString());
				PrepareElement(groupElement, groupMetaData, reference.Reference.MetaData, titleSeed, pageType, reference.ReferenceType.ToString(), readOnly);
			}
			LookupColumnElement lookupGroup = new LookupColumnElement(String.Format("{0}Column{1}_Lookup", _derivationInfo.MainSourceName, column.ElaboratedName));
			lookupGroup.ElementType = DerivationUtility.GetTag(reference.Reference.MetaData, "ElementType", pageType, reference.ReferenceType.ToString(), "QuickLookup");
			DerivationUtility.ExtractProperties(reference.Reference.MetaData, lookupGroup.ElementType, pageType, lookupGroup.Properties);
			lookupGroup.Properties.SafeAdd(new Tag("Source", _derivationInfo.MainSourceName));
            lookupGroup.Source = lookupGroup.Properties.GetTag("Source").Value;
			lookupGroup.Properties.SafeAdd(new Tag("ColumnName", Schema.Object.Qualify(column.Column.Name, column.ElaboratedTableVar.ElaboratedName)));
            lookupGroup.ColumnName = lookupGroup.Properties.GetTag("ColumnName").Value;
			lookupGroup.Properties.SafeAdd(new Tag("LookupColumnName", Schema.Object.Qualify(reference.Reference.TargetKey.Columns[reference.Reference.SourceKey.Columns.IndexOf(column.Column.Name)].Name, DerivationUtility.MainElaboratedTableName)));
            lookupGroup.LookupColumnName = lookupGroup.Properties.GetTag("LookupColumnName").Value;
			lookupGroup.Properties.SafeAdd(new Tag("Document", GetLookupDocument(reference, masterKeyNames, detailKeyNames)));
            lookupGroup.LookupDocument = lookupGroup.Properties.GetTag("Document").Value;
			if (masterKeyNames.Length > 0)
			{
				lookupGroup.Properties.SafeAdd(new Tag("MasterKeyNames", StringArrayToNames(masterKeyNames)));
				lookupGroup.MasterKeyNames = lookupGroup.Properties.GetTag("MasterKeyNames").Value;
				lookupGroup.Properties.SafeAdd(new Tag("DetailKeyNames", StringArrayToNames(detailKeyNames)));
				lookupGroup.DetailKeyNames = lookupGroup.Properties.GetTag("DetailKeyNames").Value;
			}

			if (column.ReadOnly || readOnly || Convert.ToBoolean(DerivationUtility.GetTag(column.Column.MetaData, "ReadOnly", pageType, "False")))
				lookupGroup.Properties.AddOrUpdate("ReadOnly", "True");

			MetaData metaData = DerivationUtility.ExtractTags(reference.Reference.MetaData, lookupGroup.ElementType, pageType, reference.ReferenceType.ToString());
			PrepareElement(lookupGroup, metaData, reference.Reference.MetaData, titleSeed, pageType, reference.ReferenceType.ToString(), readOnly);

			if (!Boolean.Parse(DerivationUtility.GetTag(metaData, "Visible", pageType, "True")))
				lookupGroup.Properties.AddOrUpdate("Visible", "False");
			
			AddElement(lookupGroup, column.GroupName, titleSeed, pageType, readOnly);
			
			// Build the element
			column.GroupName = lookupGroup.Name;
			Element controlElement = BuildColumnElement(column, pageType, titleSeed, readOnly);
			if (!controlElement.Properties.Contains("TitleAlignment"))
				switch (controlElement.ElementType)
				{
					case "TextBox" :
					case "DateTimeBox" :
					case "NumericTextBox" : controlElement.Properties.Add(new Tag("TitleAlignment", "None")); break;
				}
			
			// Use the column's title for the group (only in the case of a quick lookup)
			lookupGroup.Title = controlElement.Title;
			
			// If the control has a flow break specified, push it onto the lookup group (only in the case of a quick lookup)
			if (controlElement.FlowBreak)
				lookupGroup.FlowBreak = true;
		}

		protected virtual void BuildFullLookup
		(
			ElaboratedReference reference,
			string[] columnNames,
			string[] lookupColumnNames,
			string[] masterKeyNames,
			string[] detailKeyNames,
			string pageType, 
			string titleSeed, 
			bool readOnly
		)
		{
			if (reference.IsEmbedded) 
			{
				string lookupGroupName = _derivationInfo.ElaboratedExpression.Groups.ResolveGroupName(String.Format("{0}{1}", reference.ElaboratedName, "Group"));
				LookupGroupElement lookupGroupElement = _rootElement.FindElement(lookupGroupName) as LookupGroupElement;
				if (lookupGroupElement == null)
				{
					lookupGroupElement = new LookupGroupElement(lookupGroupName);
					lookupGroupElement.ElementType = DerivationUtility.GetTag(reference.GroupMetaData, "ElementType", pageType, reference.ReferenceType.ToString(), DerivationUtility.GetTag(reference.Reference.MetaData, "ElementType", pageType, reference.ReferenceType.ToString(), "FullLookup"));

					CreateContainerElement(lookupGroupElement, _derivationInfo.ElaboratedExpression.Groups[lookupGroupName], titleSeed, pageType, readOnly);

					MetaData metaData = DerivationUtility.ExtractTags(reference.Reference.MetaData, lookupGroupElement.ElementType, pageType, reference.ReferenceType.ToString());
					PrepareElement(lookupGroupElement, metaData, reference.Reference.MetaData, titleSeed, pageType, reference.ReferenceType.ToString(), readOnly);

					lookupGroupElement.Properties.SafeAdd(new Tag("Source", _derivationInfo.MainSourceName));
                    lookupGroupElement.Source = lookupGroupElement.Properties.GetTag("Source").Value;
					lookupGroupElement.Properties.SafeAdd(new Tag("ColumnNames", StringArrayToNames(columnNames)));
                    lookupGroupElement.ColumnNames = lookupGroupElement.Properties.GetTag("ColumnNames").Value;
					lookupGroupElement.Properties.SafeAdd(new Tag("LookupColumnNames", StringArrayToNames(lookupColumnNames)));
                    lookupGroupElement.LookupColumnNames = lookupGroupElement.Properties.GetTag("LookupColumnNames").Value;
					lookupGroupElement.Properties.SafeAdd(new Tag("Document", GetLookupDocument(reference, masterKeyNames, detailKeyNames)));
                    lookupGroupElement.LookupDocument = lookupGroupElement.Properties.GetTag("LookupDocument").Value;
					if (masterKeyNames.Length > 0)
					{
						lookupGroupElement.Properties.SafeAdd(new Tag("MasterKeyNames", StringArrayToNames(masterKeyNames)));
                        lookupGroupElement.MasterKeyNames = lookupGroupElement.Properties.GetTag("MasterKeyNames").Value;
						lookupGroupElement.Properties.SafeAdd(new Tag("DetailKeyNames", StringArrayToNames(detailKeyNames)));
                        lookupGroupElement.DetailKeyNames = lookupGroupElement.Properties.GetTag("DetailKeyNames").Value;
					}
					AddElement(lookupGroupElement, GetParentGroupName(lookupGroupName), titleSeed, pageType, readOnly);
				}
			}
		}
		
		protected virtual void BuildColumn(ElaboratedTableVarColumn column)
		{
			string titleSeed = column.GetTitleSeed();
			string pageType = _derivationInfo.PageType;

			if ((column.ElaboratedTableVar.ElaboratedReference != null) && (column.ElaboratedTableVar.ElaboratedReference.ReferenceType == ReferenceType.Lookup))
				pageType = DerivationUtility.Preview;
				
			if 
			(
				!DerivationUtility.IsReadOnlyPageType(pageType) &&
				(column.ElaboratedReference != null) && 
				(column.ElaboratedReference.ReferenceType == ReferenceType.Lookup) && 
				(
					(column.ElaboratedReference.SourceElaboratedTableVar.ElaboratedReference == null) ||
					(column.ElaboratedReference.SourceElaboratedTableVar.ElaboratedReference.ReferenceType == ReferenceType.Extension)
				)
			)
			{
				int visibleCount = 0;
				List<string> masterColumns = new List<string>();
				List<string> columns = new List<string>();
				foreach (ElaboratedTableVarColumn referenceColumn in column.ElaboratedReference.Columns)
				{
					if (referenceColumn.IsMaster)
						masterColumns.Add(referenceColumn.Column.Name);
					else
						columns.Add(referenceColumn.Column.Name);
						
					if (referenceColumn.Visible)
						visibleCount++;
				}
				
				string[] columnNames = new string[columns.Count];
				string[] lookupColumnNames = new string[columns.Count];
				string[] masterKeyNames = new string[masterColumns.Count];
				string[] detailKeyNames = new string[masterColumns.Count];

				int columnIndex = 0;
				int masterColumnIndex = 0;
				for (int index = 0; index < column.ElaboratedReference.Reference.SourceKey.Columns.Count; index++)
				{
					if (columns.Contains(column.ElaboratedReference.Reference.SourceKey.Columns[index].Name))
					{
						columnNames[columnIndex] = Schema.Object.Qualify(column.ElaboratedReference.Reference.SourceKey.Columns[index].Name, column.ElaboratedTableVar.ElaboratedName);
						lookupColumnNames[columnIndex] = Schema.Object.Qualify(column.ElaboratedReference.Reference.TargetKey.Columns[index].Name, DerivationUtility.MainElaboratedTableName);
						columnIndex++;
					}
					else
					{
						masterKeyNames[masterColumnIndex] = Schema.Object.Qualify(column.ElaboratedReference.Reference.SourceKey.Columns[index].Name, column.ElaboratedTableVar.ElaboratedName);
						detailKeyNames[masterColumnIndex] = Schema.Object.Qualify(column.ElaboratedReference.Reference.TargetKey.Columns[index].Name, DerivationUtility.MainElaboratedTableName);
						masterColumnIndex++;
					}
				}				

				bool useFullLookup = (visibleCount != 1) || Convert.ToBoolean(DerivationUtility.GetTag(column.ElaboratedReference.Reference.MetaData, "UseFullLookup", _derivationInfo.PageType, column.ElaboratedReference.ReferenceType.ToString(), "False"));
				
				if (useFullLookup)
				{
					BuildFullLookup(column.ElaboratedReference, columnNames, lookupColumnNames, masterKeyNames, detailKeyNames, pageType, titleSeed, column.ReadOnly); 
					if (column.Visible)
						BuildColumnElement(column, pageType, titleSeed, column.ReadOnly);
				}
				else
					if (column.Visible)
						BuildQuickLookup(column, column.ElaboratedReference, columnNames, lookupColumnNames, masterKeyNames, detailKeyNames, pageType, titleSeed, column.ReadOnly);
			}
			else
			{
				if (column.Visible)
					BuildColumnElement(column, pageType, titleSeed, column.ReadOnly);
			}
		}
	}
	
	public class SingularStructureBuilder : StructureBuilder
	{
		public SingularStructureBuilder(DerivationInfo derivationInfo) : base(derivationInfo) {}

		/// <summary> Look for column lookups grouped with a single column and apply a flowbreak. </summary>
 		protected virtual void CleanupColumnLookups(Element element)
		{
			ContainerElement localElement = element as ContainerElement;
			if (localElement != null)
			{
				if 
				(
					(localElement.Elements.Count == 2) && 
					(localElement.Elements[0] is LookupColumnElement) && 
					(
						(localElement.Elements[1] is ColumnElement) ||
						(
							(localElement.Elements[1] is GroupElement) &&
							(((GroupElement)localElement.Elements[1]).EliminateGroup)
						)
					)
				)
					localElement.Elements[0].FlowBreak = true;

				for (int i = 0; i < localElement.Elements.Count; i++)
					CleanupColumnLookups(localElement.Elements[i]);
			}
		}

		/// <summary> Applies any changes to the completed element hierarchy. </summary>
		protected virtual void Cleanup()
		{
			CleanupColumnLookups(_rootElement);
		}

		public override Element Build()
		{
			_rootElement = new GroupElement("Root");
			foreach (ElaboratedTableVarColumn column in _derivationInfo.ElaboratedExpression.Columns)
				BuildColumn(column);

			Cleanup();
			return _rootElement;
		}
	}
	
	public class PluralStructureBuilder : StructureBuilder
	{
		public PluralStructureBuilder(DerivationInfo derivationInfo) : base(derivationInfo) {}

		protected override string GetDefaultElementType(Schema.IDataType dataType, string pageType)
		{
			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (scalarType.NativeType == DAE.Runtime.Data.NativeAccessors.AsBoolean.NativeType)
					return "CheckBoxColumn";
			}
			
			return "TextColumn";
		}

		protected override Element BuildColumnElement(ElaboratedTableVarColumn column, string pageType, string titleSeed, bool readOnly)
		{
			GridColumnElement gridColumnElement = new GridColumnElement(String.Format("{0}GridColumn{1}", _derivationInfo.MainSourceName, column.ElaboratedName));
			if (column.ElaboratedReference == null)
				gridColumnElement.ColumnName = column.ElaboratedName;
			else
				gridColumnElement.ColumnName = Schema.Object.Qualify(column.Column.Name, column.ElaboratedTableVar.ElaboratedName);
			gridColumnElement.ElementType = DerivationUtility.GetTag(column.Column.MetaData, "Grid.ElementType", pageType, GetDefaultElementType(column.Column.DataType, pageType));
			CreateGridColumnElement(gridColumnElement, column.Column, titleSeed, pageType, false);
			AddElement(gridColumnElement, column.GroupName, titleSeed, pageType, false);
			return gridColumnElement;
		}

		protected override void BuildQuickLookup
		(
			ElaboratedTableVarColumn column,
			ElaboratedReference reference, 
			string[] columnNames,
			string[] lookupColumnNames,
			string[] masterKeyNames,
			string[] detailKeyNames,
			string pageType, 
			string titleSeed, 
			bool readOnly
		)
		{
			if (reference.IsEmbedded) 
			{
				string lookupGroupName = _derivationInfo.ElaboratedExpression.Groups.ResolveGroupName(String.Format("{0}{1}", reference.ElaboratedName, "Group"));
				EnsureGroups(lookupGroupName, titleSeed, pageType, readOnly);
			}
			BuildColumnElement(column, pageType, titleSeed, readOnly);
		}
		
		public override Element Build()
		{
			GridElement gridElement = new GridElement("Grid");
			_rootElement = gridElement;
			gridElement.ElementType = DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Grid.ElementType", _derivationInfo.PageType, "Grid");
			gridElement.Source = _derivationInfo.MainSourceName;
			CreateGridElement(gridElement, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar, String.Empty, _derivationInfo.PageType, _derivationInfo.IsReadOnly);

			foreach (ElaboratedTableVarColumn column in _derivationInfo.ElaboratedExpression.Columns)
				BuildColumn(column);

			return _rootElement;
		}
	}
	
	public class SearchStructureBuilder : StructureBuilder
	{
		public SearchStructureBuilder(DerivationInfo derivationInfo) : base(derivationInfo) {}
		
		public string GetOrderLookupDocument()
		{
			return
				DerivationUtility.GetTag
				(
					_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData,
					"Document",
					DerivationUtility.OrderBrowse,
					_derivationInfo.BuildDerivationExpression
					(
						DerivationUtility.OrderBrowse,
						DerivationUtility.GetTag
						(
							_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
							"Query", 
							DerivationUtility.OrderBrowse, 
							_derivationInfo.Query
						),
						_derivationInfo.MasterKeyNames,
						_derivationInfo.DetailKeyNames,
						Boolean.Parse
						(
							DerivationUtility.GetTag
							(
								_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
								"Elaborate", 
								DerivationUtility.OrderBrowse, 
								_derivationInfo.Elaborate.ToString()
							)
						)
					)
				);
		}

		public override Element Build()
		{
			SearchElement search = new SearchElement(String.Format("{0}Search", _derivationInfo.MainSourceName));
			_rootElement = search;
			search.ElementType = DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Search.ElementType", _derivationInfo.PageType, "Search");
			search.Source = _derivationInfo.MainSourceName;
			CreateSearchElement(search, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar, String.Empty, _derivationInfo.PageType, false);

			List<string> searchColumnNames = new List<string>();
			SearchColumnElement searchColumn;
			foreach (Schema.Order order in _derivationInfo.TableVar.Orders)
				if (Convert.ToBoolean(DerivationUtility.GetTag(order.MetaData, "Visible", _derivationInfo.PageType, "True")))
					foreach (Schema.OrderColumn orderColumn in order.Columns)
					{
						string orderColumnName = Schema.Object.Qualify(orderColumn.Column.Name, DerivationUtility.MainElaboratedTableName);
						if (!searchColumnNames.Contains(orderColumnName))
						{
							try
							{
								searchColumnNames.Add(orderColumnName);
								searchColumn = new SearchColumnElement(String.Format("{0}SearchColumn{1}", _derivationInfo.MainSourceName, orderColumnName));
								searchColumn.ColumnName = orderColumnName;
								searchColumn.ElementType =	DerivationUtility.GetTag(orderColumn.Column.MetaData, "Search.ElementType", _derivationInfo.PageType, "SearchColumn");
								CreateSearchColumnElement(searchColumn, orderColumn.Column, _derivationInfo.ElaboratedExpression.Columns[orderColumnName].GetTitleSeed(), _derivationInfo.PageType, false);
								search.Elements.Add(searchColumn);
							}
							catch (Exception E)
							{
								throw new ServerException(ServerException.Codes.CannotConstructSearchColumn, E, orderColumnName, order.Name);
							}
						}
					}

			foreach (Schema.Key key in _derivationInfo.TableVar.Keys)
				foreach (Schema.TableVarColumn column in key.Columns)
				{
					string keyColumnName = Schema.Object.Qualify(column.Name, DerivationUtility.MainElaboratedTableName);
					if (!searchColumnNames.Contains(keyColumnName))
					{
						try
						{
							searchColumnNames.Add(keyColumnName);
							searchColumn = new SearchColumnElement(String.Format("{0}SearchColumn{1}", _derivationInfo.MainSourceName, keyColumnName));
							searchColumn.ColumnName = keyColumnName;
							searchColumn.ElementType = DerivationUtility.GetTag(column.MetaData, "Search.ElementType", _derivationInfo.PageType, "SearchColumn");
							CreateSearchColumnElement(searchColumn, column, _derivationInfo.ElaboratedExpression.Columns[keyColumnName].GetTitleSeed(), _derivationInfo.PageType, false);
							search.Elements.Add(searchColumn);
						}
						catch (Exception E)
						{
							throw new ServerException(ServerException.Codes.CannotConstructSearchColumn, E, keyColumnName, key.Name);
						}
					}
				}
				
			return _rootElement;
		}
	}
}

