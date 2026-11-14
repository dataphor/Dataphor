/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.Frontend.Server.Derivation;
using Alphora.Dataphor.Frontend.Server.Elaboration;
using Alphora.Dataphor.Frontend.Server.Structuring;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Server.Production
{
	public enum SecureBehavior { Hidden, Disabled, Visible }
		
	public abstract class DocumentBuilder : System.Object
	{
		public DocumentBuilder(DerivationInfo derivationInfo) : base() 
		{
			_derivationInfo = derivationInfo;
		}
		
		protected DerivationInfo _derivationInfo;
		
		protected int _nameCount = 0;
		protected string GetUniqueName()
		{
			_nameCount++;
			return String.Format("Element{0}", _nameCount.ToString());
		}
		
		protected virtual XmlElement BuildInterface(XmlDocument document)
		{
			// "<interface xmlns:bop='www.alphora.com/schemas/bop' text='Browse "<%= PageType %>"' mainsource='"<%= MainSourceName %>"'> ... </interface>"
			XmlElement interfaceValue = document.CreateElement("interface");
			interfaceValue.SetAttribute("xmlns:bop", BOP.Serializer.BOPNamespaceURI);
			interfaceValue.SetAttribute
			(
				"text", 
				DerivationUtility.GetTag
				(
					_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					"Caption", 
					_derivationInfo.PageType, 
					String.Format("{0} {1}", _derivationInfo.PageType, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle)
				)
			);
			interfaceValue.SetAttribute("mainsource", _derivationInfo.MainSourceName);
			document.AppendChild(interfaceValue);

			Tags properties = new Tags();
			DerivationUtility.ExtractProperties(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Interface", _derivationInfo.PageType, properties);
#if USEHASHTABLEFORTAGS
			foreach (Tag tag in properties)
			{
#else
			Tag tag;
			for (int index = 0; index < properties.Count; index++)
			{
				tag = properties[index];
#endif
				interfaceValue.SetAttribute(tag.Name.ToLower(), tag.Value);
			}

			return interfaceValue;
		}
		
		protected virtual void BuildSource(XmlElement element)
		{
			// "<source bop:name="<%= MainSourceName %>" expression="<%= Expression %>" usebrowse="<%= UseBrowse %>" useapplicationtransactions="<%= UseApplicationTransactions %> openstate="<% OpenState %>"/>"
			XmlElement source = element.OwnerDocument.CreateElement("source");
			element.AppendChild(source);
			source.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, _derivationInfo.MainSourceName);
			source.SetAttribute("expression", _derivationInfo.Expression);
			if (!Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "UseBrowse", _derivationInfo.PageType, "True")))
				source.SetAttribute("usebrowse", "False");
			if (!Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "UseApplicationTransactions", _derivationInfo.PageType, "True")))
				source.SetAttribute("useapplicationtransactions", "False");
			string enlistMode = DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "ShouldEnlist", _derivationInfo.PageType, "Default");
			if (enlistMode != "Default")
				source.SetAttribute("shouldenlist", enlistMode);
			if (Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "ReadOnly", _derivationInfo.PageType, (DerivationUtility.IsReadOnlyPageType(_derivationInfo.PageType) && (_derivationInfo.PageType != DerivationUtility.Delete)) ? "True" : "False")))
				source.SetAttribute("isreadonly", "True");
				
			Tags properties = new Tags();
			DerivationUtility.ExtractProperties(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Source", _derivationInfo.PageType, properties);
			#if USEHASHTABLEFORTAGS
			foreach (Tag tag in properties)
			{
			#else
			Tag tag;
			for (int index = 0; index < properties.Count; index++)
			{
				tag = properties[index];
			#endif
				source.SetAttribute(tag.Name.ToLower(), tag.Value);
			}
			
			//if (FDerivationInfo.PageType == DerivationUtility.CAdd)
			//	LSource.SetAttribute("openstate", "Insert");
			//if (FDerivationInfo.PageType == DerivationUtility.CEdit)
			//	LSource.SetAttribute("openstate", "Edit");
		}

		protected static string GetColumnNames(Schema.Key key, string qualifier)
		{
			return GetColumnNames(key.Columns, qualifier);
		}
		
		protected static string GetColumnNames(Schema.TableVarColumnsBase columns, string qualifier)
		{
			StringBuilder columnNames = new StringBuilder();
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					columnNames.Append(",");
				columnNames.Append(Schema.Object.Qualify(columns[index].Name, qualifier));
			}
			return columnNames.ToString();
		}
		
		protected virtual void BuildDetailActions(XmlElement element, ElaboratedTableVar tableVar, List<string> menuItems, List<string> toolbarItems)
		{
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Parent))
					BuildDetailActions(element, reference.TargetElaboratedTableVar, menuItems, toolbarItems);

			XmlElement action;			
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
			{
				if (reference.ReferenceType == ReferenceType.Detail)
				{
					string masterKeyNames = 
						(string)DerivationUtility.GetTag
						(
							reference.Reference.MetaData,
							"MasterKeyNames",
							"",
							reference.ReferenceType.ToString(),
							GetColumnNames(reference.Reference.TargetKey.Columns, reference.TargetElaboratedTableVar.ElaboratedName)
						);

					string detailKeyNames = 
						(string)DerivationUtility.GetTag
						(
							reference.Reference.MetaData,
							"DetailKeyNames",
							"",
							reference.ReferenceType.ToString(),
							GetColumnNames(reference.Reference.SourceKey.Columns, DerivationUtility.MainElaboratedTableName)
						);
					
					action = element.OwnerDocument.CreateElement("showformaction");
					action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, reference.ElaboratedName);
					action.SetAttribute("text", String.Format("{0}...", reference.ReferenceTitle));

					string detailPageType = 
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
									reference.Reference.SourceTable.MetaData, 
									"UseList", 
									"False"
								)
							)
						) ? DerivationUtility.List : DerivationUtility.Browse;
						
					bool elaborate =
						Boolean.Parse
						(
							DerivationUtility.GetTag
							(
								reference.Reference.MetaData,
								"Elaborate",
								detailPageType,
								reference.ReferenceType.ToString(),
								DerivationUtility.GetTag
								(
									reference.Reference.SourceTable.MetaData,
									"Elaborate",
									detailPageType,
									"True"
								)
							)
						);

					action.SetAttribute
					(
						"document", 
						DerivationUtility.GetTag
						(
							reference.Reference.MetaData, 
							"Document", 
							detailPageType, 
							reference.ReferenceType.ToString(),
							_derivationInfo.BuildDerivationExpression
							(
								detailPageType,
								DerivationUtility.GetTag
								(
									reference.Reference.MetaData, "Query", 
									detailPageType, 
									reference.ReferenceType.ToString(), 
									DerivationUtility.GetTag
									(
										reference.Reference.SourceTable.MetaData, 
										"Query", 
										detailPageType, 
										reference.Reference.SourceTable.Name
									)
								),
								masterKeyNames, 
								detailKeyNames, 
								elaborate
							)
						)
					);
					action.SetAttribute("sourcelinktype", "Detail");
					action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
					action.SetAttribute("sourcelink.masterkeynames", masterKeyNames);
					action.SetAttribute("sourcelink.detailkeynames", detailKeyNames);
					action.SetAttribute("sourcelinkrefresh", "False");
					element.AppendChild(action);

					if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, DerivationUtility.Visible, _derivationInfo.PageType, reference.ReferenceType.ToString(), "True")))
						menuItems.Add(reference.ElaboratedName);
						
					if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, DerivationUtility.Exposed, _derivationInfo.PageType, reference.ReferenceType.ToString(), "False")))
						toolbarItems.Add(reference.ElaboratedName);
				}
			}
			
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Extension))
					BuildDetailActions(element, reference.SourceElaboratedTableVar, menuItems, toolbarItems);
		}

		protected virtual void BuildDetailActions(XmlElement element)
		{
			//foreach one to many reference in which TableName is a target
			//	write a detail action with a browse mode
			List<string> menuItems = new List<string>();
			List<string> toolbarItems = new List<string>();
			BuildDetailActions(element, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar, menuItems, toolbarItems);
			
			if (menuItems.Count > 0)
			{
				// "<menu text='De&amp;tails' bop:name='DetailsMenuItem'>"
				XmlElement menu = element.OwnerDocument.CreateElement("menu");
				menu.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "DetailsMenuItem");
				menu.SetAttribute("text", "De&tails");
				element.AppendChild(menu);
				
				XmlElement menuItem;
				foreach (string stringValue in menuItems)
				{
					// String.Format("<menu action='{0}' bop:name='{0}DetailsMenuItem'/>", LString);
					menuItem = element.OwnerDocument.CreateElement("menu");
					menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}DetailsMenuItem", stringValue));
					menuItem.SetAttribute("action", stringValue);
					menu.AppendChild(menuItem);
				}
			}
			
			XmlElement toolbarItem;
			foreach (string stringValue in toolbarItems)
			{
				// Response.Write(String.Format("<exposed action='{0}' bop:name='{0}DetailsExposed' />", LString));
				toolbarItem = element.OwnerDocument.CreateElement("exposed");
				toolbarItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}DetailsExposed", stringValue));
				toolbarItem.SetAttribute("action", stringValue);
				element.AppendChild(toolbarItem);
			}
		}
		
		protected virtual void BuildExtensionActions(XmlElement element)
		{
			XmlElement action;
			List<string> menuItems = new List<string>();
			List<string> toolbarItems = new List<string>();
			string formMode;
			string pageType;
			foreach (ElaboratedReference reference in _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.ElaboratedReferences)
			{
				if (!Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, DerivationUtility.Embedded, _derivationInfo.PageType, reference.ReferenceType.ToString(), "False")))
				{
					if (reference.ReferenceType == ReferenceType.Extension)
					{
						switch (_derivationInfo.PageType)
						{
							case DerivationUtility.Browse:
							case DerivationUtility.List:
							case DerivationUtility.Edit: formMode = "Edit"; pageType = DerivationUtility.Edit; break;
							case DerivationUtility.Add: formMode = "Edit"; pageType = DerivationUtility.Edit; break;
							default: formMode = "None"; pageType = DerivationUtility.View; break;
						}

						string masterKeyNames = GetColumnNames(reference.Reference.TargetKey.Columns, reference.TargetElaboratedTableVar.ElaboratedName);
						string detailKeyNames = GetColumnNames(reference.Reference.SourceKey.Columns, DerivationUtility.MainElaboratedTableName);
						
						action = element.OwnerDocument.CreateElement("showformaction");
						action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, reference.ElaboratedName);
						action.SetAttribute("text", String.Format("{0}...", reference.ReferenceTitle));

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
										reference.Reference.SourceTable.MetaData,
										"Elaborate",
										pageType,
										"True"
									)
								)
							);

						action.SetAttribute
						(
							"document", 
							DerivationUtility.GetTag
							(
								reference.Reference.MetaData,
								"Document",
								pageType,
								reference.ReferenceType.ToString(),
								_derivationInfo.BuildDerivationExpression
								(
									pageType, 
									DerivationUtility.GetTag
									(
										reference.Reference.MetaData, 
										"Query", 
										pageType, 
										reference.ReferenceType.ToString(), 
										DerivationUtility.GetTag
										(
											reference.Reference.SourceTable.MetaData, 
											"Query", 
											pageType, 
											reference.Reference.SourceTable.Name
										)
									), 
									masterKeyNames, 
									detailKeyNames,
									elaborate
								)
							)
						);

						action.SetAttribute("sourcelinktype", "Detail");
						action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
						action.SetAttribute("sourcelink.masterkeynames", masterKeyNames);
						action.SetAttribute("sourcelink.detailkeynames", detailKeyNames);
						action.SetAttribute("mode", formMode);
						action.SetAttribute("sourcelinkrefresh", "False");
						element.AppendChild(action);

						if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, "Visible", _derivationInfo.PageType, reference.ReferenceType.ToString(), "True")))
							menuItems.Add(reference.ElaboratedName);
							
						if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, "Exposed", _derivationInfo.PageType, reference.ReferenceType.ToString(), "False")))
							toolbarItems.Add(reference.ElaboratedName);
					}
				}
			}
			
			if (menuItems.Count > 0)
			{
				// Response.Write("<menu text='E&amp;xtensions' bop:name='ExtensionsMenuItem'>");
				XmlElement menu = element.OwnerDocument.CreateElement("menu");
				menu.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "ExtensionsMenuItem");
				menu.SetAttribute("text", "E&xtensions");
				element.AppendChild(menu);
				
				XmlElement menuItem;
				foreach (string stringValue in menuItems)
				{
					// Response.Write(String.Format("<menu action='{0}' bop:name='{0}ExtensionsMenuItem' />", LString));
					menuItem = element.OwnerDocument.CreateElement("menu");
					menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}ExtensionsMenuItem", stringValue));
					menuItem.SetAttribute("action", stringValue);
					menu.AppendChild(menuItem);
				}
			}
			
			XmlElement exposed;
			foreach (string stringValue in toolbarItems)
			{
				// Response.Write(String.Format("<exposed action='{0}' />", LString));
				exposed = element.OwnerDocument.CreateElement("exposed");
				exposed.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}ExtensionsExposed", stringValue));
				exposed.SetAttribute("action", stringValue);
				element.AppendChild(exposed);
			}
		}
		
		protected virtual void BuildLookupActions(XmlElement element, ElaboratedTableVar tableVar, List<string> menuItems, List<string> toolbarItems)
		{
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Parent))
					BuildLookupActions(element, reference.TargetElaboratedTableVar, menuItems, toolbarItems);
			
			XmlElement action;
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
			{
				if ((reference.ReferenceType == ReferenceType.Lookup) || (reference.ReferenceType == ReferenceType.Parent))
				{
					string masterKeyNames = GetColumnNames(reference.Reference.SourceKey.Columns, reference.SourceElaboratedTableVar.ElaboratedName);
					string detailKeyNames = GetColumnNames(reference.Reference.TargetKey.Columns, DerivationUtility.MainElaboratedTableName);
					
					action = element.OwnerDocument.CreateElement("showformaction");
					action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, reference.ElaboratedName);
					action.SetAttribute("text", String.Format("{0}...", reference.ReferenceTitle));

					string pageType = DerivationUtility.View;
						
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
									reference.Reference.TargetTable.MetaData,
									"Elaborate",
									pageType,
									"True"
								)
							)
						);


					action.SetAttribute
					(
						"document", 
						DerivationUtility.GetTag
						(
							reference.Reference.MetaData,
							"Document",
							DerivationUtility.View,
							reference.ReferenceType.ToString(),
							_derivationInfo.BuildDerivationExpression
							(
								pageType, 
								DerivationUtility.GetTag
								(
									reference.Reference.MetaData, 
									"Query", 
									pageType, 
									reference.ReferenceType.ToString(), 
									DerivationUtility.GetTag
									(
										reference.Reference.TargetTable.MetaData, 
										"Query", 
										pageType, 
										reference.Reference.TargetTable.Name
									)
								),
								"", 
								"",
								elaborate
							)
						)
					);

					action.SetAttribute("sourcelinktype", "Detail");
					action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
					action.SetAttribute("sourcelink.masterkeynames", masterKeyNames);
					action.SetAttribute("sourcelink.detailkeynames", detailKeyNames);
					action.SetAttribute("sourcelinkrefresh", "False");
					element.AppendChild(action);

					if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, "Visible", _derivationInfo.PageType, reference.ReferenceType.ToString(), "True")))
						menuItems.Add(reference.ElaboratedName);

					if (Convert.ToBoolean(DerivationUtility.GetTag(reference.Reference.MetaData, "Exposed", _derivationInfo.PageType, reference.ReferenceType.ToString(), "False")))
						toolbarItems.Add(reference.ElaboratedName);
				}
			}
			
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Extension))
					BuildLookupActions(element, reference.SourceElaboratedTableVar, menuItems, toolbarItems);
		}
		
		protected virtual void BuildLookupActions(XmlElement element)
		{
			List<string> menuItems = new List<string>();
			List<string> toolbarItems = new List<string>();
			BuildLookupActions(element, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar, menuItems, toolbarItems);

			if (menuItems.Count > 0)
			{
				// Response.Write("<menu text='Vie&amp;w' bop:name='LookupViewMenuItem'>");
				XmlElement menu = element.OwnerDocument.CreateElement("menu");
				menu.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "LookupViewMenuItem");
				menu.SetAttribute("text", "Vie&w");
				element.AppendChild(menu);
				
				XmlElement menuItem;
				foreach (string stringValue in menuItems)
				{
					// Response.Write(String.Format("<menu action='{0}' bop:name='{0}LookupMenuItem'/>", LString));
					menuItem = element.OwnerDocument.CreateElement("menu");
					menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}LookupMenuItem", stringValue));
					menuItem.SetAttribute("action", stringValue);
					menu.AppendChild(menuItem);
				}
			}
			
			XmlElement exposed;
			foreach (string stringValue in toolbarItems)
			{
				// Response.Write(String.Format("<exposed action='{0}' bop:name='{0}LookupExposed'/>", LString));
				exposed = element.OwnerDocument.CreateElement("exposed");
				exposed.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}LookupExposed", stringValue));
				exposed.SetAttribute("action", stringValue);
				element.AppendChild(exposed);
			}
		}
		
		protected ElaboratedReferences GatherEmbeddedDetails(ElaboratedTableVar tableVar)
		{
			ElaboratedReferences references = new ElaboratedReferences();
			references.OrderByPriorityOnly = true;
			GatherEmbeddedDetails(tableVar, references);
			return references;
		}
		
		protected virtual void GatherEmbeddedDetails(ElaboratedTableVar tableVar, ElaboratedReferences references)
		{
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Parent))
					GatherEmbeddedDetails(reference.TargetElaboratedTableVar, references);
					
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Detail))
					references.Add(reference);
					
			foreach (ElaboratedReference reference in tableVar.ElaboratedReferences)
				if (reference.IsEmbedded && (reference.ReferenceType == ReferenceType.Extension))
					GatherEmbeddedDetails(reference.SourceElaboratedTableVar, references);
		}
		
		protected virtual void BuildEmbeddedDetails(XmlElement element, ElaboratedTableVar tableVar)
		{
			ElaboratedReferences references = GatherEmbeddedDetails(tableVar);
			
			if (references.Count > 0)
			{
				XmlElement notebook = element.OwnerDocument.CreateElement("notebook");
				notebook.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, tableVar.ElaboratedName + "EmbeddedDetailsNotebook");
				element.AppendChild(notebook);
				
				foreach (ElaboratedReference reference in references)
					BuildEmbeddedDetail(element, notebook, reference);
			}
		}
		
		protected virtual void BuildEmbeddedDetail(XmlElement element, XmlElement notebook, ElaboratedReference reference)
		{
			string masterKeyNames = GetColumnNames(reference.Reference.TargetKey.Columns, reference.TargetElaboratedTableVar.ElaboratedName);
			string detailKeyNames = GetColumnNames(reference.Reference.SourceKey.Columns, DerivationUtility.MainElaboratedTableName);
		
			XmlElement notebookPage = element.OwnerDocument.CreateElement("notebookframepage");
			notebookPage.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}Frame", reference.Reference.OriginatingReferenceName()));

			string detailPageType = 
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
							reference.Reference.SourceTable.MetaData, 
							"UseList", 
							"False"
						)
					)
				) ? DerivationUtility.List : DerivationUtility.Browse;
				
			bool elaborate =
				Boolean.Parse
				(
					DerivationUtility.GetTag
					(
						reference.Reference.MetaData,
						"Elaborate",
						detailPageType,
						reference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							reference.Reference.SourceTable.MetaData,
							"Elaborate",
							detailPageType,
							"True"
						)
					)
				);

			notebookPage.SetAttribute
			(
				"document",
				DerivationUtility.GetTag
				(
					reference.Reference.MetaData,
					"Document",
					detailPageType,
					reference.ReferenceType.ToString(),
					_derivationInfo.BuildDerivationExpression
					(
						detailPageType, 
						DerivationUtility.GetTag
						(
							reference.Reference.MetaData, 
							"Query", 
							detailPageType, 
							reference.ReferenceType.ToString(), 
							DerivationUtility.GetTag
							(
								reference.Reference.SourceTable.MetaData, 
								"Query", 
								detailPageType, 
								reference.Reference.SourceTable.Name
							)
						), 
						masterKeyNames, 
						detailKeyNames,
						elaborate
					)
				)
			);

			notebookPage.SetAttribute("sourcelinktype", "Detail");
			notebookPage.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
			notebookPage.SetAttribute("sourcelink.masterkeynames", masterKeyNames);
			notebookPage.SetAttribute("sourcelink.detailkeynames", detailKeyNames);
			notebookPage.SetAttribute("menutext", reference.ReferenceTitle);
			notebookPage.SetAttribute("title", reference.ReferenceTitle);
			notebook.AppendChild(notebookPage);
		}
		
		protected virtual void BuildEmbeddedDetails(XmlElement element)
		{
			BuildEmbeddedDetails(element, _derivationInfo.ElaboratedExpression.MainElaboratedTableVar);
		}
		
		protected abstract void InternalBuild(XmlElement element);
		
		public virtual XmlDocument Build()
		{
			XmlDocument document = new XmlDocument();
			XmlElement interfaceValue = BuildInterface(document);
			BuildSource(interfaceValue);
			InternalBuild(interfaceValue);
			return document;
		}
		
		protected virtual XmlElement BuildElement(XmlElement xmlElement, Element element)
		{
			XmlElement localElement = xmlElement.OwnerDocument.CreateElement(element.ElementType.ToLower());
			localElement.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, element.Name);
			if ((element.Title != null) && (element.Title != String.Empty))
				localElement.SetAttribute("title", element.Title);
			if (element.Hint != String.Empty)
				localElement.SetAttribute("hint", element.Hint);
			#if USEHASHTABLEFORTAGS
			foreach (Tag tag in AElement.Properties)
			{
			#else
			Tag tag;
			for (int index = 0; index < element.Properties.Count; index++)
			{
				tag = element.Properties[index];
			#endif
				localElement.SetAttribute(tag.Name.ToLower(), tag.Value);
			}
			return localElement;
		}
		
		protected virtual void SkipContainerElement(XmlElement element, ContainerElement containerElement)
		{
			for (int index = 0; index < containerElement.Elements.Count; index++)
				if (containerElement.Elements[index] is ContainerElement)
					SkipContainerElement(element, ((ContainerElement)containerElement.Elements[index]));
				else
					element.AppendChild(BuildElement(element, containerElement.Elements[index]));
		}
		
		// Build container element is called for grids and searches only, so skip groups used to order the columns
		protected virtual XmlElement BuildContainerElement(XmlElement element, ContainerElement containerElement)
		{
			XmlElement localContainerElement = BuildElement(element, containerElement);
			SkipContainerElement(localContainerElement, containerElement);
			return localContainerElement;
		}
		
		protected virtual XmlElement BuildElementNode(XmlElement element, ElementNode node)
		{
			return BuildElement(element, node.Element);
		}
		
		protected virtual XmlElement BuildColumnNode(XmlElement element, ColumnNode node)
		{
			XmlElement localElement = element.OwnerDocument.CreateElement("column");
			localElement.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, GetUniqueName());
			return localElement;
		}
		
		protected virtual XmlElement BuildRowNode(XmlElement element, RowNode node)
		{
			XmlElement localElement = element.OwnerDocument.CreateElement("row");
			localElement.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, GetUniqueName());
			return localElement;
		}
		
		protected virtual XmlElement BuildLayoutNode(XmlElement element, LayoutNode node)
		{
			XmlElement localElement = null;
			
			if (node is ElementNode)	
			{
				GroupElement groupElement = ((ElementNode)node).Element as GroupElement;
				if ((groupElement != null) && !groupElement.ContainsMultipleElements())
					node = node.Children[0]; // Skip the group node
				localElement = BuildElementNode(element, (ElementNode)node);
			}
			else if (node is ColumnNode)
				localElement = BuildColumnNode(element, (ColumnNode)node);
			else if (node is RowNode)
				localElement = BuildRowNode(element, (RowNode)node);

			foreach (LayoutNode localNode in node.Children)
				localElement.AppendChild(BuildLayoutNode(localElement, localNode));
				
			return localElement;
		}
	}

	public abstract class PluralDocumentBuilder : DocumentBuilder
	{
		public PluralDocumentBuilder(DerivationInfo derivationInfo) : base(derivationInfo)  {}

		protected virtual void BuildNavigationActions(XmlElement element)
		{
			// Response.Write(String.Format("<sourceaction bop:name='MoveFirst' source='{0}' text='&amp;First' action='First' />\r\n", SourceName));
			XmlElement action = element.OwnerDocument.CreateElement("sourceaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MoveFirst");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			action.SetAttribute("text", "&First");
			action.SetAttribute("action", "First");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("First"));
			element.AppendChild(action);
			
			// Response.Write(String.Format("<sourceaction bop:name='MovePrior' source='{0}' text='&amp;Prior' action='Prior' />\r\n", SourceName));
			action = element.OwnerDocument.CreateElement("sourceaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MovePrior");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			action.SetAttribute("text", "&Prior");
			action.SetAttribute("action", "Prior");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Prior"));
			element.AppendChild(action);
			
			// Response.Write(String.Format("<sourceaction bop:name='MoveNext' source='{0}' text='&amp;Next' action='Next' />\r\n", SourceName));
			action = element.OwnerDocument.CreateElement("sourceaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MoveNext");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			action.SetAttribute("text", "&Next");
			action.SetAttribute("action", "Next");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Next"));
			element.AppendChild(action);
			
			// Response.Write(String.Format("<sourceaction bop:name='MoveLast' source='{0}' text='&amp;Last' action='Last' />\r\n", SourceName));
			action = element.OwnerDocument.CreateElement("sourceaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MoveLast");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			action.SetAttribute("text", "&Last");
			action.SetAttribute("action", "Last");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Last"));
			element.AppendChild(action);
			
			// Response.Write(String.Format("<sourceaction bop:name='Refresh' source='{0}' text='&amp;Refresh' action='Refresh' />\r\n", SourceName));
			action = element.OwnerDocument.CreateElement("sourceaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "Refresh");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			action.SetAttribute("text", "&Refresh");
			action.SetAttribute("action", "Refresh");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Refresh"));
			element.AppendChild(action);
			
			// Response.Write("<menu bop:name='NavigationMenu' text='&amp;Navigation'>");
			XmlElement menu = element.OwnerDocument.CreateElement("menu");
			menu.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "NavigationMenu");
			menu.SetAttribute("text", "&Navigation");
			element.AppendChild(menu);

			// Response.Write("<menu bop:name='MoveFirstMenu' action='MoveFirst'/>");
			XmlElement menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MoveFirstMenu");
			menuItem.SetAttribute("action", "MoveFirst");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='MovePriorMenu' action='MovePrior'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MovePriorMenu");
			menuItem.SetAttribute("action", "MovePrior");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='MoveNextMenu' action='MoveNext'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MoveNextMenu");
			menuItem.SetAttribute("action", "MoveNext");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='MoveLastMenu' action='MoveLast'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MoveLastMenu");
			menuItem.SetAttribute("action", "MoveLast");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='NavSepMenu1' text='-'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "NavSepMenu1");
			menuItem.SetAttribute("text", "-");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='RefreshMenu' action='Refresh'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RefreshMenu");
			menuItem.SetAttribute("action", "Refresh");
			menu.AppendChild(menuItem);
		}

		protected virtual XmlElement BuildRootBrowseColumn(XmlElement element)
		{
			// "<column bop:name='RootBrowseColumn'> ... </column>"
			XmlElement rootBrowseColumn = element.OwnerDocument.CreateElement("column");
			rootBrowseColumn.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootBrowseColumn");
			element.AppendChild(rootBrowseColumn);
			return rootBrowseColumn;
		}
	}
	
	public abstract class SearchableDocumentBuilder : PluralDocumentBuilder
	{
		public SearchableDocumentBuilder(DerivationInfo derivationInfo, SearchElement searchElement, GridElement gridElement) : base(derivationInfo)
		{
			_searchElement = searchElement;
			_gridElement = gridElement;
		}

		protected SearchElement _searchElement;
		public SearchElement SearchElement { get { return _searchElement; } }
		
		protected GridElement _gridElement;
		public GridElement GridElement { get { return _gridElement; } }
		
		protected virtual void BuildSearch(XmlElement element)
		{
			element.AppendChild(BuildContainerElement(element, SearchElement));
		}
		
		protected virtual void BuildGrid(XmlElement element)
		{
			element.AppendChild(BuildContainerElement(element, GridElement));
		}

		protected virtual XmlElement BuildGridRow(XmlElement element)
		{
			XmlElement gridRow = element.OwnerDocument.CreateElement("row");
			gridRow.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "GridRow");
			element.AppendChild(gridRow);
			return gridRow;
		}
	}
	
	public class ListDocumentBuilder : SearchableDocumentBuilder
	{
		public ListDocumentBuilder(DerivationInfo derivationInfo, SearchElement searchElement, GridElement gridElement) : base(derivationInfo, searchElement, gridElement) {}

		protected override void InternalBuild(XmlElement element)
		{
			BuildDetailActions(element);
			BuildExtensionActions(element);
			BuildLookupActions(element);
			BuildNavigationActions(element);
			XmlElement rootColumn = BuildRootBrowseColumn(element);
			BuildSearch(rootColumn);
			XmlElement gridRow = BuildGridRow(rootColumn);
			BuildGrid(gridRow);
			BuildEmbeddedDetails(rootColumn);
		}
	}

	public class BrowseDocumentBuilder : SearchableDocumentBuilder
	{
		public BrowseDocumentBuilder(DerivationInfo derivationInfo, SearchElement searchElement, GridElement gridElement) : base(derivationInfo, searchElement, gridElement) {}

		// Frontend.Secure = "Visible" | "Disabled" | "Hidden" (Default)
		
		protected virtual void BuildUpdateActions(XmlElement element)
		{
			XmlElement action;

			SecureBehavior secureBehavior = SecureBehavior.Visible;
			if (!_derivationInfo.Program.Plan.HasRight(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.GetRight(Schema.RightNames.Insert)))
				secureBehavior = (SecureBehavior)Enum.Parse(typeof(SecureBehavior), DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Secure", DerivationUtility.Add, "Hidden"), true);

			action = element.OwnerDocument.CreateElement("showformaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "Add");
			action.SetAttribute("text", "&Add...");
			action.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					String.Format("{0}.Document", DerivationUtility.Add),
					_derivationInfo.BuildDerivationExpression
					(
						DerivationUtility.Add,
						DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.Add, _derivationInfo.Query),
						_derivationInfo.MasterKeyNames,
						_derivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.Add, _derivationInfo.Elaborate.ToString()))
					)
				)
			);
			action.SetAttribute("mode", "Insert");
			action.SetAttribute("sourcelinktype", "Detail");
			action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
			if (_derivationInfo.DetailKeyNames != String.Empty)
				action.SetAttribute("sourcelink.attachmaster",  "True");
			action.SetAttribute("hint", "Add a new row.");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Add"));
			switch (secureBehavior)
			{
				case SecureBehavior.Disabled : action.SetAttribute("enabled", "False"); break;
				case SecureBehavior.Hidden : action.SetAttribute("visible", "False"); break;
			}
			element.AppendChild(action);
			
			secureBehavior = SecureBehavior.Visible;
			if (!_derivationInfo.Program.Plan.HasRight(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.GetRight(Schema.RightNames.Update)))
				secureBehavior = (SecureBehavior)Enum.Parse(typeof(SecureBehavior), DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Secure", DerivationUtility.Edit, "Hidden"), true);

			action = element.OwnerDocument.CreateElement("showformaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "Edit");
			action.SetAttribute("text", "&Edit...");
			action.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData,
					String.Format("{0}.Document", DerivationUtility.Edit),
					_derivationInfo.BuildDerivationExpression
					(
						DerivationUtility.Edit,
						DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.Edit, _derivationInfo.Query),
						_derivationInfo.MasterKeyNames,
						_derivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.Edit, _derivationInfo.Elaborate.ToString()))
					)
				)
			);
			action.SetAttribute("mode", "Edit");
			action.SetAttribute("sourcelinktype", "Detail");
			action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
			action.SetAttribute("sourcelink.masterkeynames", _derivationInfo.KeyNames);
			action.SetAttribute("sourcelink.detailkeynames", _derivationInfo.KeyNames);
			action.SetAttribute("hint", "Edit the current row.");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Edit"));
			switch (secureBehavior)
			{
				case SecureBehavior.Disabled : action.SetAttribute("enabled", "False"); break;
				case SecureBehavior.Hidden : action.SetAttribute("visible", "False"); break;
			}
			element.AppendChild(action);
			
			secureBehavior = SecureBehavior.Visible;
			if (!_derivationInfo.Program.Plan.HasRight(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.GetRight(Schema.RightNames.Delete)))
				secureBehavior = (SecureBehavior)Enum.Parse(typeof(SecureBehavior), DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Secure", "Delete", "Hidden"), true);			

			action = element.OwnerDocument.CreateElement("showformaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "Delete");
			action.SetAttribute("text", "&Delete...");
			action.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					String.Format("{0}.Document", DerivationUtility.Delete),
					_derivationInfo.BuildDerivationExpression
					(
						DerivationUtility.Delete,
						DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.Delete, _derivationInfo.Query), 
						_derivationInfo.MasterKeyNames, 
						_derivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.Delete, _derivationInfo.Elaborate.ToString()))
					)
				)
			);
			action.SetAttribute("mode", "Delete");
			action.SetAttribute("sourcelinktype", "Detail");
			action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
			action.SetAttribute("sourcelink.masterkeynames", _derivationInfo.KeyNames);
			action.SetAttribute("sourcelink.detailkeynames", _derivationInfo.KeyNames);
			action.SetAttribute("hint", "Delete the current row.");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("Delete"));
			switch (secureBehavior)
			{
				case SecureBehavior.Disabled : action.SetAttribute("enabled", "False"); break;
				case SecureBehavior.Hidden : action.SetAttribute("visible", "False"); break;
			}
			element.AppendChild(action);
			
			action = element.OwnerDocument.CreateElement("showformaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "View");
			action.SetAttribute("text", "&View...");
			action.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					String.Format("{0}.Document", DerivationUtility.View), 
					_derivationInfo.BuildDerivationExpression
					(
						DerivationUtility.View,
						DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.View, _derivationInfo.Query),
						_derivationInfo.MasterKeyNames,
						_derivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(_derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.View, _derivationInfo.Elaborate.ToString()))
					)
				)
			);
			action.SetAttribute("sourcelinktype", "Detail");
			action.SetAttribute("sourcelink.source", _derivationInfo.MainSourceName);
			action.SetAttribute("sourcelink.masterkeynames", _derivationInfo.KeyNames);
			action.SetAttribute("sourcelink.detailkeynames", _derivationInfo.KeyNames);
			action.SetAttribute("hint", "View the current row.");
			action.SetAttribute("image", _derivationInfo.BuildImageExpression("View"));
			element.AppendChild(action);
			
			// Response.Write(String.Format("<editfilteraction bop:name='EditFilter' source='{0}' text='&amp;Filter...' />\r\n", SourceName));
			action = element.OwnerDocument.CreateElement("editfilteraction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "EditFilter");
			action.SetAttribute("text", "&Filter...");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			element.AppendChild(action);

			// Response.Write(String.Format("<menu bop:name='{0}Menu' text='{0}'>", TableTitle));
			XmlElement menu = element.OwnerDocument.CreateElement("menu");
			menu.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}Menu", _derivationInfo.Query));
			menu.SetAttribute("text", _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle);
			menu.AppendChild(action);
			
			// Response.Write("<menu bop:name='AddMenu' action='Add'/>");
			XmlElement menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "AddMenu");
			menuItem.SetAttribute("action", "Add");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='EditMenu' action='Edit'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "EditMenu");
			menuItem.SetAttribute("action", "Edit");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='DeleteMenu' action='Delete'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "DeleteMenu");
			menuItem.SetAttribute("action", "Delete");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='ViewMenu' action='View'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "ViewMenu");
			menuItem.SetAttribute("action", "View");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='UpdateSepMenu1' text='-'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "UpdateSepMenu1");
			menuItem.SetAttribute("text", "-");
			menu.AppendChild(menuItem);

			// Response.Write("<menu bop:name='EditFilterMenu' action='EditFilter'/>");
			menuItem = element.OwnerDocument.CreateElement("menu");
			menuItem.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "EditFilterMenu");
			menuItem.SetAttribute("action", "EditFilter");
			menu.AppendChild(menuItem);
		}
		
		protected virtual void BuildGridBar(XmlElement element)
		{
			// Response.Write("<column bop:name='GridBar'>");
			XmlElement column = element.OwnerDocument.CreateElement("column");
			column.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "GridBar");
			
			// Response.Write("<trigger action='Add' bop:name='AddTrigger' imagewidth='11' imageheight='13'/>");
			XmlElement trigger = element.OwnerDocument.CreateElement("trigger");
			trigger.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "AddTrigger");
			trigger.SetAttribute("action", "Add");
			trigger.SetAttribute("imagewidth", "11");
			trigger.SetAttribute("imageheight", "13");
			column.AppendChild(trigger);
			
			// Response.Write("<trigger action='Edit' bop:name='EditTrigger' imagewidth='11' imageheight='13'/>");
			trigger = element.OwnerDocument.CreateElement("trigger");
			trigger.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "EditTrigger");
			trigger.SetAttribute("action", "Edit");
			trigger.SetAttribute("imagewidth", "11");
			trigger.SetAttribute("imageheight", "13");
			column.AppendChild(trigger);
			
			// Response.Write("<trigger action='Delete' bop:name='DeleteTrigger' imagewidth='11' imageheight='13'/>");
			trigger = element.OwnerDocument.CreateElement("trigger");
			trigger.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "DeleteTrigger");
			trigger.SetAttribute("action", "Delete");
			trigger.SetAttribute("imagewidth", "11");
			trigger.SetAttribute("imageheight", "13");
			column.AppendChild(trigger);
			
			// Response.Write("<trigger action='View' bop:name='ViewTrigger' imagewidth='11' imageheight='13'/>");
			trigger = element.OwnerDocument.CreateElement("trigger");
			trigger.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "ViewTrigger");
			trigger.SetAttribute("action", "View");
			trigger.SetAttribute("imagewidth", "11");
			trigger.SetAttribute("imageheight", "13");
			column.AppendChild(trigger);
			
			// Response.Write("</column>");
			element.AppendChild(column);
		}
		
		protected override XmlElement BuildInterface(XmlDocument document)
		{
			XmlElement interfaceValue = base.BuildInterface(document);
			interfaceValue.SetAttribute("OnDefault", "Edit");
			return interfaceValue;
		}

		protected override void InternalBuild(XmlElement element)
		{
			BuildDetailActions(element);
			BuildExtensionActions(element);
			BuildLookupActions(element);
			BuildUpdateActions(element);
			BuildNavigationActions(element);
			XmlElement rootColumn = BuildRootBrowseColumn(element);
			BuildSearch(rootColumn);
			XmlElement gridRow = BuildGridRow(rootColumn);
			BuildGrid(gridRow);
			BuildGridBar(gridRow);
			BuildEmbeddedDetails(rootColumn);
		}
	}
	
	public class OrderBrowseDocumentBuilder : PluralDocumentBuilder
	{
		public OrderBrowseDocumentBuilder(DerivationInfo derivationInfo) : base(derivationInfo) {}
		
		protected override void BuildSource(XmlElement element)
		{
			XmlElement source = element.OwnerDocument.CreateElement("source");
			source.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, _derivationInfo.MainSourceName);
			source.SetAttribute("usebrowse", "False");
			source.SetAttribute("expression", GetOrderExpression());
			source.SetAttribute("isreadonly", "True");
			element.AppendChild(source);
		}
		
		protected virtual string GetColumnTitle(Schema.TableVarColumn column)
		{
			return
				DerivationUtility.GetTag
				(
					column.MetaData,
					"Caption",
					_derivationInfo.PageType,
					_derivationInfo.ElaboratedExpression.Columns[column.Name].GetTitleSeed() + 
						DerivationUtility.GetTag(column.MetaData, "Title", _derivationInfo.PageType, Schema.Object.Unqualify(column.Name))
				);
		}
		
		protected virtual string GetDefaultOrderTitle(Schema.Order order)
		{
			StringBuilder name = new StringBuilder();
			foreach (Schema.OrderColumn column in order.Columns)
			{
				if (IsColumnVisible(column.Column))
				{
					if (name.Length > 0)
						name.Append(", ");
					name.Append(GetColumnTitle(column.Column));
					if (!column.Ascending)
						name.Append(" (descending)");
				}
			}

			return String.Format("by {0}", name.ToString());
		}
		
		protected bool IsColumnVisible(Schema.TableVarColumn column)
		{
			return DerivationUtility.IsColumnVisible(column, _derivationInfo.PageType);
		}
		
		protected bool IsOrderVisible(Schema.Order order)
		{
			return DerivationUtility.IsOrderVisible(order, _derivationInfo.PageType);
		}
		
		protected virtual string GetOrderExpression()
		{
			// Build a table selector with a row selector for each possible order in the page expression
			Schema.Orders orders = new Schema.Orders();
			orders.AddRange(_derivationInfo.TableVar.Orders);
			Schema.Order orderForKey;
			foreach (Schema.Key key in _derivationInfo.TableVar.Keys)
			{
				orderForKey = _derivationInfo.Program.OrderFromKey(key);
				if (!orders.Contains(orderForKey))
					orders.Add(orderForKey);
			}
				
			StringBuilder expression = new StringBuilder();
			foreach (Schema.Order order in orders)
				if (IsOrderVisible(order))
					expression.AppendFormat
					(
						@"{0}row {{ ""{1}"" Description, ""{2}"" OrderName }} ", 
						expression.Length > 0 ? ", " : String.Empty, 
						DerivationUtility.GetTag(order.MetaData, "Title", _derivationInfo.PageType, GetDefaultOrderTitle(order)), 
						order.Name
					);

			// return an empty table if there were no visible orders, otherwise the expression will be invalid
			return String.Format("table of {{ OrderName : String, Description : String }} {{ {0} }} order by {{ OrderName }}", expression.ToString());
		}

		protected override void InternalBuild(XmlElement element)
		{
			BuildNavigationActions(element);
			XmlElement rootColumn = BuildRootBrowseColumn(element);
			XmlElement grid = element.OwnerDocument.CreateElement("grid");
			grid.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MainGrid");
			grid.SetAttribute("source", "Main");
			grid.SetAttribute("readonly", "True");
			XmlElement gridColumn = element.OwnerDocument.CreateElement("textcolumn");
			gridColumn.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "MainGridColumnDescription");
			gridColumn.SetAttribute("columnname", "Description");
			gridColumn.SetAttribute("width", "45");
			grid.AppendChild(gridColumn);
			rootColumn.AppendChild(grid);
		}
	}
	
	public class ReportDocumentBuilder : DocumentBuilder
	{
		public ReportDocumentBuilder(DerivationInfo derivationInfo) : base(derivationInfo) {}
		
		private int COffsetMultiplier = 5;

		protected override XmlElement BuildInterface(XmlDocument document)
		{
			XmlElement report = document.CreateElement("report");
			report.SetAttribute("xmlns:bop", "www.alphora.com/schemas/bop");
			document.AppendChild(report);
			return report;
		}
		
		protected virtual void BuildHeader(XmlElement element)
		{
			XmlElement header = element.OwnerDocument.CreateElement("header");
			header.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "PageHeader");
			header.SetAttribute("height", "15");
			element.AppendChild(header);
			
			XmlElement textArea = element.OwnerDocument.CreateElement("textarea");
			textArea.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "ReportName");
			textArea.SetAttribute("text", String.Format("{0} Report", _derivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle));
			textArea.SetAttribute("y", "0");
			header.AppendChild(textArea);

			XmlElement date = element.OwnerDocument.CreateElement("date");
			date.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "HeaderDate");
			date.SetAttribute("x", "520");
			date.SetAttribute("y", "0");
			header.AppendChild(date);			
		}
		
		protected virtual void BuildDataBandHeader(XmlElement element) 
		{
			int currentOffset = 10;
			int width;
			foreach (Schema.TableVarColumn column in _derivationInfo.TableVar.Columns) 
			{
				if (Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "Visible", _derivationInfo.PageType, "True"))) 
				{
					width = Convert.ToInt32(DerivationUtility.GetTag(column.MetaData, "Width", _derivationInfo.PageType, "40"));
					XmlElement textArea = element.OwnerDocument.CreateElement("textarea");
					textArea.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}Column{1}Header", _derivationInfo.MainSourceName, column.Name));
					textArea.SetAttribute("text", DerivationUtility.GetTag(column.MetaData, "Title", _derivationInfo.PageType, column.Name));
					textArea.SetAttribute("x", currentOffset.ToString());
					textArea.SetAttribute("y", "3");
					textArea.SetAttribute("maxlength", width.ToString());
					element.AppendChild(textArea);
					
					currentOffset += width * COffsetMultiplier;
				}
			}
		}

		protected virtual void BuildDataBandContent(XmlElement element) 
		{
			int currentOffset = 10;
			int width;
			foreach (Schema.TableVarColumn column in _derivationInfo.TableVar.Columns) 
			{
				if (Convert.ToBoolean(DerivationUtility.GetTag(column.MetaData, "Visible", _derivationInfo.PageType, "True"))) 
				{
					width = Convert.ToInt32(DerivationUtility.GetTag(column.MetaData, "Width", _derivationInfo.PageType, "40"));
					XmlElement field = element.OwnerDocument.CreateElement("field");
					field.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, String.Format("{0}Column{1}", _derivationInfo.MainSourceName, column.Name));
					field.SetAttribute("source", _derivationInfo.MainSourceName);
					field.SetAttribute("columnname", column.Name);
					field.SetAttribute("x", currentOffset.ToString());
					field.SetAttribute("maxlength", width.ToString());
					element.AppendChild(field);

					currentOffset += width * COffsetMultiplier;
				}
			}
		}

		protected virtual void BuildDataBand(XmlElement element)
		{
			XmlElement dataBand = element.OwnerDocument.CreateElement("databand");
			dataBand.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootDataBand");
			dataBand.SetAttribute("source", "Main");
			element.AppendChild(dataBand);
			
			XmlElement header = element.OwnerDocument.CreateElement("header");
			header.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootDataBandHeader");
			dataBand.AppendChild(header);
			
			XmlElement box = element.OwnerDocument.CreateElement("box");
			box.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootDataBandHeaderBox");
			box.SetAttribute("height", "11");
			box.SetAttribute("width", "600");
			box.SetAttribute("x", "5");
			box.SetAttribute("y", "2");
			header.AppendChild(box);
			
			BuildDataBandHeader(header);
			
			XmlElement staticGroup = element.OwnerDocument.CreateElement("staticgroup");
			staticGroup.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootDataBandContent");
			dataBand.AppendChild(staticGroup);
			
			BuildDataBandContent(staticGroup);
		}
		
		protected virtual void BuildFooter(XmlElement element)
		{
			XmlElement footer = element.OwnerDocument.CreateElement("footer");
			footer.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "PageFooter");
			footer.SetAttribute("height", "20");
			element.AppendChild(footer);
			
			XmlElement textArea = element.OwnerDocument.CreateElement("textarea");
			textArea.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "PageFooterPageText");
			textArea.SetAttribute("text", "Page");
			textArea.SetAttribute("x", "290");
			textArea.SetAttribute("y", "5");
			footer.AppendChild(textArea);
			
			XmlElement pageNumber = element.OwnerDocument.CreateElement("pagenumber");
			pageNumber.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "PageFooterPageNumber");
			pageNumber.SetAttribute("x", "310");
			pageNumber.SetAttribute("y", "5");
			footer.AppendChild(pageNumber);
		}
		
		
		protected override void InternalBuild(XmlElement element)
		{
			// visible elements
			BuildHeader(element);
			BuildDataBand(element);
			BuildFooter(element);
		}
	}
	
	public class SingularDocumentBuilder : DocumentBuilder
	{
		public SingularDocumentBuilder(DerivationInfo derivationInfo, LayoutNode layoutNode) : base(derivationInfo)
		{
			_layoutNode = layoutNode;
		}
		
		protected LayoutNode _layoutNode;
		public LayoutNode LayoutNode { get { return _layoutNode; } }
		
		protected virtual XmlElement BuildRootEditColumn(XmlElement element)
		{
			// "<column bop:name='RootEditColumn'> ... </column>"
			XmlElement rootEditColumn = element.OwnerDocument.CreateElement("column");
			rootEditColumn.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootEditColumn");
			element.AppendChild(rootEditColumn);
			return rootEditColumn;
		}
		
		protected virtual void BuildBody(XmlElement element)
		{
			// Build the body based on the LayoutNodes given
			if (_layoutNode != null)
				element.AppendChild(BuildLayoutNode(element, LayoutNode));
		}
		
		protected virtual void BuildValidateAction(XmlElement element)
		{
			// Response.Write(String.Format("<sourceaction bop:name='MoveFirst' source='{0}' text='&amp;First' action='First' />\r\n", SourceName));
			XmlElement action = element.OwnerDocument.CreateElement("sourceaction");
			action.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "Validate");
			action.SetAttribute("source", _derivationInfo.MainSourceName);
			action.SetAttribute("text", "&Validate");
			action.SetAttribute("action", "Validate");
			//LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Validate"));
			element.AppendChild(action);
		}

		protected override void InternalBuild(XmlElement element)
		{
			BuildDetailActions(element);
			BuildExtensionActions(element);
			BuildLookupActions(element);
			BuildValidateAction(element);
			XmlElement rootColumn = BuildRootEditColumn(element);
			BuildBody(rootColumn);
			BuildEmbeddedDetails(rootColumn);
		}
	}

	public class DeleteDocumentBuilder : SingularDocumentBuilder
	{
		public DeleteDocumentBuilder(DerivationInfo derivationInfo, LayoutNode layoutNode) : base(derivationInfo, layoutNode) {}

		protected override XmlElement BuildRootEditColumn(XmlElement element)
		{
			XmlElement rootDeleteRow = element.OwnerDocument.CreateElement("row");
			rootDeleteRow.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "RootDeleteRow");
			element.AppendChild(rootDeleteRow);

			XmlElement deleteImage = element.OwnerDocument.CreateElement("staticimage");
			deleteImage.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "DeleteImage");
			deleteImage.SetAttribute("image", _derivationInfo.BuildImageExpression("Warning"));
			deleteImage.SetAttribute("imagewidth", "32");
			deleteImage.SetAttribute("imageheight", "32");
			rootDeleteRow.AppendChild(deleteImage);

			XmlElement deleteGroup = element.OwnerDocument.CreateElement("group");
			deleteGroup.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "DeleteGroup");
			deleteGroup.SetAttribute("title", Strings.Get("DeleteText"));
			rootDeleteRow.AppendChild(deleteGroup);

			XmlElement deleteColumn = element.OwnerDocument.CreateElement("column");
			deleteColumn.SetAttribute("name", BOP.Serializer.BOPNamespaceURI, "DeleteColumn");
			deleteGroup.AppendChild(deleteColumn);

			return deleteColumn;
		}
		

	}
}

