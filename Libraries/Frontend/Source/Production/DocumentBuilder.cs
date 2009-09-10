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
		public DocumentBuilder(DerivationInfo ADerivationInfo) : base() 
		{
			FDerivationInfo = ADerivationInfo;
		}
		
		protected DerivationInfo FDerivationInfo;
		
		protected int FNameCount = 0;
		protected string GetUniqueName()
		{
			FNameCount++;
			return String.Format("Element{0}", FNameCount.ToString());
		}
		
		protected virtual XmlElement BuildInterface(XmlDocument ADocument)
		{
			// "<interface xmlns:bop='www.alphora.com/schemas/bop' text='Browse "<%= PageType %>"' mainsource='"<%= MainSourceName %>"'> ... </interface>"
			XmlElement LInterface = ADocument.CreateElement("interface");
			LInterface.SetAttribute("xmlns:bop", BOP.Serializer.CBOPNamespaceURI);
			LInterface.SetAttribute
			(
				"text", 
				DerivationUtility.GetTag
				(
					FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					"Caption", 
					FDerivationInfo.PageType, 
					String.Format("{0} {1}", FDerivationInfo.PageType, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle)
				)
			);
			LInterface.SetAttribute("mainsource", FDerivationInfo.MainSourceName);
			ADocument.AppendChild(LInterface);

			Tags LProperties = new Tags();
			DerivationUtility.ExtractProperties(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Interface", FDerivationInfo.PageType, LProperties);
#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in LProperties)
			{
#else
			Tag LTag;
			for (int LIndex = 0; LIndex < LProperties.Count; LIndex++)
			{
				LTag = LProperties[LIndex];
#endif
				LInterface.SetAttribute(LTag.Name.ToLower(), LTag.Value);
			}

			return LInterface;
		}
		
		protected virtual void BuildSource(XmlElement AElement)
		{
			// "<source bop:name="<%= MainSourceName %>" expression="<%= Expression %>" usebrowse="<%= UseBrowse %>" useapplicationtransactions="<%= UseApplicationTransactions %> openstate="<% OpenState %>"/>"
			XmlElement LSource = AElement.OwnerDocument.CreateElement("source");
			AElement.AppendChild(LSource);
			LSource.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, FDerivationInfo.MainSourceName);
			LSource.SetAttribute("expression", FDerivationInfo.Expression);
			if (!Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "UseBrowse", FDerivationInfo.PageType, "True")))
				LSource.SetAttribute("usebrowse", "False");
			if (!Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "UseApplicationTransactions", FDerivationInfo.PageType, "True")))
				LSource.SetAttribute("useapplicationtransactions", "False");
			string LEnlistMode = DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "ShouldEnlist", FDerivationInfo.PageType, "Default");
			if (LEnlistMode != "Default")
				LSource.SetAttribute("shouldenlist", LEnlistMode);
			if (Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "ReadOnly", FDerivationInfo.PageType, (DerivationUtility.IsReadOnlyPageType(FDerivationInfo.PageType) && (FDerivationInfo.PageType != DerivationUtility.CDelete)) ? "True" : "False")))
				LSource.SetAttribute("isreadonly", "True");
				
			Tags LProperties = new Tags();
			DerivationUtility.ExtractProperties(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Source", FDerivationInfo.PageType, LProperties);
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in LProperties)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < LProperties.Count; LIndex++)
			{
				LTag = LProperties[LIndex];
			#endif
				LSource.SetAttribute(LTag.Name.ToLower(), LTag.Value);
			}
			
			//if (FDerivationInfo.PageType == DerivationUtility.CAdd)
			//	LSource.SetAttribute("openstate", "Insert");
			//if (FDerivationInfo.PageType == DerivationUtility.CEdit)
			//	LSource.SetAttribute("openstate", "Edit");
		}
		
		protected static string GetColumnNames(Schema.Key AKey, string AQualifier)
		{
			StringBuilder LColumnNames = new StringBuilder();
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LColumnNames.Append(",");
				LColumnNames.Append(Schema.Object.Qualify(AKey.Columns[LIndex].Name, AQualifier));
			}
			return LColumnNames.ToString();
		}
		
		protected virtual void BuildDetailActions(XmlElement AElement, ElaboratedTableVar ATableVar, List<string> AMenuItems, List<string> AToolbarItems)
		{
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Parent))
					BuildDetailActions(AElement, LReference.TargetElaboratedTableVar, AMenuItems, AToolbarItems);

			XmlElement LAction;			
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
			{
				if (LReference.ReferenceType == ReferenceType.Detail)
				{
					string LMasterKeyNames = 
						(string)DerivationUtility.GetTag
						(
							LReference.Reference.MetaData,
							"MasterKeyNames",
							"",
							LReference.ReferenceType.ToString(),
							GetColumnNames(LReference.Reference.TargetKey, LReference.TargetElaboratedTableVar.ElaboratedName)
						);

					string LDetailKeyNames = 
						(string)DerivationUtility.GetTag
						(
							LReference.Reference.MetaData,
							"DetailKeyNames",
							"",
							LReference.ReferenceType.ToString(),
							GetColumnNames(LReference.Reference.SourceKey, DerivationUtility.CMainElaboratedTableName)
						);
					
					LAction = AElement.OwnerDocument.CreateElement("showformaction");
					LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, LReference.ElaboratedName);
					LAction.SetAttribute("text", String.Format("{0}...", LReference.ReferenceTitle));

					string LDetailPageType = 
						Boolean.Parse
						(
							DerivationUtility.GetTag
							(
								LReference.Reference.MetaData, 
								"UseList", 
								"", 
								LReference.ReferenceType.ToString(), 
								DerivationUtility.GetTag
								(
									LReference.Reference.SourceTable.MetaData, 
									"UseList", 
									"False"
								)
							)
						) ? DerivationUtility.CList : DerivationUtility.CBrowse;
						
					bool LElaborate =
						Boolean.Parse
						(
							DerivationUtility.GetTag
							(
								LReference.Reference.MetaData,
								"Elaborate",
								LDetailPageType,
								LReference.ReferenceType.ToString(),
								DerivationUtility.GetTag
								(
									LReference.Reference.SourceTable.MetaData,
									"Elaborate",
									LDetailPageType,
									"True"
								)
							)
						);

					LAction.SetAttribute
					(
						"document", 
						DerivationUtility.GetTag
						(
							LReference.Reference.MetaData, 
							"Document", 
							LDetailPageType, 
							LReference.ReferenceType.ToString(),
							FDerivationInfo.BuildDerivationExpression
							(
								LDetailPageType,
								DerivationUtility.GetTag
								(
									LReference.Reference.MetaData, "Query", 
									LDetailPageType, 
									LReference.ReferenceType.ToString(), 
									DerivationUtility.GetTag
									(
										LReference.Reference.SourceTable.MetaData, 
										"Query", 
										LDetailPageType, 
										LReference.Reference.SourceTable.Name
									)
								),
								LMasterKeyNames, 
								LDetailKeyNames, 
								LElaborate
							)
						)
					);
					LAction.SetAttribute("sourcelinktype", "Detail");
					LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
					LAction.SetAttribute("sourcelink.masterkeynames", LMasterKeyNames);
					LAction.SetAttribute("sourcelink.detailkeynames", LDetailKeyNames);
					LAction.SetAttribute("sourcelinkrefresh", "False");
					AElement.AppendChild(LAction);

					if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, DerivationUtility.CVisible, FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "True")))
						AMenuItems.Add(LReference.ElaboratedName);
						
					if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, DerivationUtility.CExposed, FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "False")))
						AToolbarItems.Add(LReference.ElaboratedName);
				}
			}
			
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Extension))
					BuildDetailActions(AElement, LReference.SourceElaboratedTableVar, AMenuItems, AToolbarItems);
		}

		protected virtual void BuildDetailActions(XmlElement AElement)
		{
			//foreach one to many reference in which TableName is a target
			//	write a detail action with a browse mode
			List<string> LMenuItems = new List<string>();
			List<string> LToolbarItems = new List<string>();
			BuildDetailActions(AElement, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar, LMenuItems, LToolbarItems);
			
			if (LMenuItems.Count > 0)
			{
				// "<menu text='De&amp;tails' bop:name='DetailsMenuItem'>"
				XmlElement LMenu = AElement.OwnerDocument.CreateElement("menu");
				LMenu.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "DetailsMenuItem");
				LMenu.SetAttribute("text", "De&tails");
				AElement.AppendChild(LMenu);
				
				XmlElement LMenuItem;
				foreach (string LString in LMenuItems)
				{
					// String.Format("<menu action='{0}' bop:name='{0}DetailsMenuItem'/>", LString);
					LMenuItem = AElement.OwnerDocument.CreateElement("menu");
					LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}DetailsMenuItem", LString));
					LMenuItem.SetAttribute("action", LString);
					LMenu.AppendChild(LMenuItem);
				}
			}
			
			XmlElement LToolbarItem;
			foreach (string LString in LToolbarItems)
			{
				// Response.Write(String.Format("<exposed action='{0}' bop:name='{0}DetailsExposed' />", LString));
				LToolbarItem = AElement.OwnerDocument.CreateElement("exposed");
				LToolbarItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}DetailsExposed", LString));
				LToolbarItem.SetAttribute("action", LString);
				AElement.AppendChild(LToolbarItem);
			}
		}
		
		protected virtual void BuildExtensionActions(XmlElement AElement)
		{
			XmlElement LAction;
			List<string> LMenuItems = new List<string>();
			List<string> LToolbarItems = new List<string>();
			string LFormMode;
			string LPageType;
			foreach (ElaboratedReference LReference in FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.ElaboratedReferences)
			{
				if (!Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, DerivationUtility.CEmbedded, FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "False")))
				{
					if (LReference.ReferenceType == ReferenceType.Extension)
					{
						switch (FDerivationInfo.PageType)
						{
							case DerivationUtility.CBrowse:
							case DerivationUtility.CList:
							case DerivationUtility.CEdit: LFormMode = "Edit"; LPageType = DerivationUtility.CEdit; break;
							case DerivationUtility.CAdd: LFormMode = "Edit"; LPageType = DerivationUtility.CEdit; break;
							default: LFormMode = "None"; LPageType = DerivationUtility.CView; break;
						}

						string LMasterKeyNames = GetColumnNames(LReference.Reference.TargetKey, LReference.TargetElaboratedTableVar.ElaboratedName);
						string LDetailKeyNames = GetColumnNames(LReference.Reference.SourceKey, DerivationUtility.CMainElaboratedTableName);
						
						LAction = AElement.OwnerDocument.CreateElement("showformaction");
						LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, LReference.ElaboratedName);
						LAction.SetAttribute("text", String.Format("{0}...", LReference.ReferenceTitle));

						bool LElaborate =
							Boolean.Parse
							(
								DerivationUtility.GetTag
								(
									LReference.Reference.MetaData,
									"Elaborate",
									LPageType,
									LReference.ReferenceType.ToString(),
									DerivationUtility.GetTag
									(
										LReference.Reference.SourceTable.MetaData,
										"Elaborate",
										LPageType,
										"True"
									)
								)
							);

						LAction.SetAttribute
						(
							"document", 
							DerivationUtility.GetTag
							(
								LReference.Reference.MetaData,
								"Document",
								LPageType,
								LReference.ReferenceType.ToString(),
								FDerivationInfo.BuildDerivationExpression
								(
									LPageType, 
									DerivationUtility.GetTag
									(
										LReference.Reference.MetaData, 
										"Query", 
										LPageType, 
										LReference.ReferenceType.ToString(), 
										DerivationUtility.GetTag
										(
											LReference.Reference.SourceTable.MetaData, 
											"Query", 
											LPageType, 
											LReference.Reference.SourceTable.Name
										)
									), 
									LMasterKeyNames, 
									LDetailKeyNames,
									LElaborate
								)
							)
						);

						LAction.SetAttribute("sourcelinktype", "Detail");
						LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
						LAction.SetAttribute("sourcelink.masterkeynames", LMasterKeyNames);
						LAction.SetAttribute("sourcelink.detailkeynames", LDetailKeyNames);
						LAction.SetAttribute("mode", LFormMode);
						LAction.SetAttribute("sourcelinkrefresh", "False");
						AElement.AppendChild(LAction);

						if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, "Visible", FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "True")))
							LMenuItems.Add(LReference.ElaboratedName);
							
						if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, "Exposed", FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "False")))
							LToolbarItems.Add(LReference.ElaboratedName);
					}
				}
			}
			
			if (LMenuItems.Count > 0)
			{
				// Response.Write("<menu text='E&amp;xtensions' bop:name='ExtensionsMenuItem'>");
				XmlElement LMenu = AElement.OwnerDocument.CreateElement("menu");
				LMenu.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "ExtensionsMenuItem");
				LMenu.SetAttribute("text", "E&xtensions");
				AElement.AppendChild(LMenu);
				
				XmlElement LMenuItem;
				foreach (string LString in LMenuItems)
				{
					// Response.Write(String.Format("<menu action='{0}' bop:name='{0}ExtensionsMenuItem' />", LString));
					LMenuItem = AElement.OwnerDocument.CreateElement("menu");
					LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}ExtensionsMenuItem", LString));
					LMenuItem.SetAttribute("action", LString);
					LMenu.AppendChild(LMenuItem);
				}
			}
			
			XmlElement LExposed;
			foreach (string LString in LToolbarItems)
			{
				// Response.Write(String.Format("<exposed action='{0}' />", LString));
				LExposed = AElement.OwnerDocument.CreateElement("exposed");
				LExposed.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}ExtensionsExposed", LString));
				LExposed.SetAttribute("action", LString);
				AElement.AppendChild(LExposed);
			}
		}
		
		protected virtual void BuildLookupActions(XmlElement AElement, ElaboratedTableVar ATableVar, List<string> AMenuItems, List<string> AToolbarItems)
		{
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Parent))
					BuildLookupActions(AElement, LReference.TargetElaboratedTableVar, AMenuItems, AToolbarItems);
			
			XmlElement LAction;
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
			{
				if ((LReference.ReferenceType == ReferenceType.Lookup) || (LReference.ReferenceType == ReferenceType.Parent))
				{
					string LMasterKeyNames = GetColumnNames(LReference.Reference.SourceKey, LReference.SourceElaboratedTableVar.ElaboratedName);
					string LDetailKeyNames = GetColumnNames(LReference.Reference.TargetKey, DerivationUtility.CMainElaboratedTableName);
					
					LAction = AElement.OwnerDocument.CreateElement("showformaction");
					LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, LReference.ElaboratedName);
					LAction.SetAttribute("text", String.Format("{0}...", LReference.ReferenceTitle));

					string LPageType = DerivationUtility.CView;
						
					bool LElaborate =
						Boolean.Parse
						(
							DerivationUtility.GetTag
							(
								LReference.Reference.MetaData,
								"Elaborate",
								LPageType,
								LReference.ReferenceType.ToString(),
								DerivationUtility.GetTag
								(
									LReference.Reference.TargetTable.MetaData,
									"Elaborate",
									LPageType,
									"True"
								)
							)
						);


					LAction.SetAttribute
					(
						"document", 
						DerivationUtility.GetTag
						(
							LReference.Reference.MetaData,
							"Document",
							DerivationUtility.CView,
							LReference.ReferenceType.ToString(),
							FDerivationInfo.BuildDerivationExpression
							(
								LPageType, 
								DerivationUtility.GetTag
								(
									LReference.Reference.MetaData, 
									"Query", 
									LPageType, 
									LReference.ReferenceType.ToString(), 
									DerivationUtility.GetTag
									(
										LReference.Reference.TargetTable.MetaData, 
										"Query", 
										LPageType, 
										LReference.Reference.TargetTable.Name
									)
								),
								"", 
								"",
								LElaborate
							)
						)
					);

					LAction.SetAttribute("sourcelinktype", "Detail");
					LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
					LAction.SetAttribute("sourcelink.masterkeynames", LMasterKeyNames);
					LAction.SetAttribute("sourcelink.detailkeynames", LDetailKeyNames);
					LAction.SetAttribute("sourcelinkrefresh", "False");
					AElement.AppendChild(LAction);

					if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, "Visible", FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "True")))
						AMenuItems.Add(LReference.ElaboratedName);

					if (Convert.ToBoolean(DerivationUtility.GetTag(LReference.Reference.MetaData, "Exposed", FDerivationInfo.PageType, LReference.ReferenceType.ToString(), "False")))
						AToolbarItems.Add(LReference.ElaboratedName);
				}
			}
			
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Extension))
					BuildLookupActions(AElement, LReference.SourceElaboratedTableVar, AMenuItems, AToolbarItems);
		}
		
		protected virtual void BuildLookupActions(XmlElement AElement)
		{
			List<string> LMenuItems = new List<string>();
			List<string> LToolbarItems = new List<string>();
			BuildLookupActions(AElement, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar, LMenuItems, LToolbarItems);

			if (LMenuItems.Count > 0)
			{
				// Response.Write("<menu text='Vie&amp;w' bop:name='LookupViewMenuItem'>");
				XmlElement LMenu = AElement.OwnerDocument.CreateElement("menu");
				LMenu.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "LookupViewMenuItem");
				LMenu.SetAttribute("text", "Vie&w");
				AElement.AppendChild(LMenu);
				
				XmlElement LMenuItem;
				foreach (string LString in LMenuItems)
				{
					// Response.Write(String.Format("<menu action='{0}' bop:name='{0}LookupMenuItem'/>", LString));
					LMenuItem = AElement.OwnerDocument.CreateElement("menu");
					LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}LookupMenuItem", LString));
					LMenuItem.SetAttribute("action", LString);
					LMenu.AppendChild(LMenuItem);
				}
			}
			
			XmlElement LExposed;
			foreach (string LString in LToolbarItems)
			{
				// Response.Write(String.Format("<exposed action='{0}' bop:name='{0}LookupExposed'/>", LString));
				LExposed = AElement.OwnerDocument.CreateElement("exposed");
				LExposed.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}LookupExposed", LString));
				LExposed.SetAttribute("action", LString);
				AElement.AppendChild(LExposed);
			}
		}
		
		protected ElaboratedReferences GatherEmbeddedDetails(ElaboratedTableVar ATableVar)
		{
			ElaboratedReferences LReferences = new ElaboratedReferences();
			LReferences.OrderByPriorityOnly = true;
			GatherEmbeddedDetails(ATableVar, LReferences);
			return LReferences;
		}
		
		protected virtual void GatherEmbeddedDetails(ElaboratedTableVar ATableVar, ElaboratedReferences AReferences)
		{
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Parent))
					GatherEmbeddedDetails(LReference.TargetElaboratedTableVar, AReferences);
					
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Detail))
					AReferences.Add(LReference);
					
			foreach (ElaboratedReference LReference in ATableVar.ElaboratedReferences)
				if (LReference.IsEmbedded && (LReference.ReferenceType == ReferenceType.Extension))
					GatherEmbeddedDetails(LReference.SourceElaboratedTableVar, AReferences);
		}
		
		protected virtual void BuildEmbeddedDetails(XmlElement AElement, ElaboratedTableVar ATableVar)
		{
			ElaboratedReferences LReferences = GatherEmbeddedDetails(ATableVar);
			
			if (LReferences.Count > 0)
			{
				XmlElement LNotebook = AElement.OwnerDocument.CreateElement("notebook");
				LNotebook.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, ATableVar.ElaboratedName + "EmbeddedDetailsNotebook");
				AElement.AppendChild(LNotebook);
				
				foreach (ElaboratedReference LReference in LReferences)
					BuildEmbeddedDetail(AElement, LNotebook, LReference);
			}
		}
		
		protected virtual void BuildEmbeddedDetail(XmlElement AElement, XmlElement ANotebook, ElaboratedReference AReference)
		{
			string LMasterKeyNames = GetColumnNames(AReference.Reference.TargetKey, AReference.TargetElaboratedTableVar.ElaboratedName);
			string LDetailKeyNames = GetColumnNames(AReference.Reference.SourceKey, DerivationUtility.CMainElaboratedTableName);
		
			XmlElement LNotebookPage = AElement.OwnerDocument.CreateElement("notebookframepage");
			LNotebookPage.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}Frame", AReference.Reference.OriginatingReferenceName()));

			string LDetailPageType = 
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
							AReference.Reference.SourceTable.MetaData, 
							"UseList", 
							"False"
						)
					)
				) ? DerivationUtility.CList : DerivationUtility.CBrowse;
				
			bool LElaborate =
				Boolean.Parse
				(
					DerivationUtility.GetTag
					(
						AReference.Reference.MetaData,
						"Elaborate",
						LDetailPageType,
						AReference.ReferenceType.ToString(),
						DerivationUtility.GetTag
						(
							AReference.Reference.SourceTable.MetaData,
							"Elaborate",
							LDetailPageType,
							"True"
						)
					)
				);

			LNotebookPage.SetAttribute
			(
				"document",
				DerivationUtility.GetTag
				(
					AReference.Reference.MetaData,
					"Document",
					LDetailPageType,
					AReference.ReferenceType.ToString(),
					FDerivationInfo.BuildDerivationExpression
					(
						LDetailPageType, 
						DerivationUtility.GetTag
						(
							AReference.Reference.MetaData, 
							"Query", 
							LDetailPageType, 
							AReference.ReferenceType.ToString(), 
							DerivationUtility.GetTag
							(
								AReference.Reference.SourceTable.MetaData, 
								"Query", 
								LDetailPageType, 
								AReference.Reference.SourceTable.Name
							)
						), 
						LMasterKeyNames, 
						LDetailKeyNames,
						LElaborate
					)
				)
			);

			LNotebookPage.SetAttribute("sourcelinktype", "Detail");
			LNotebookPage.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
			LNotebookPage.SetAttribute("sourcelink.masterkeynames", LMasterKeyNames);
			LNotebookPage.SetAttribute("sourcelink.detailkeynames", LDetailKeyNames);
			LNotebookPage.SetAttribute("menutext", AReference.ReferenceTitle);
			LNotebookPage.SetAttribute("title", AReference.ReferenceTitle);
			ANotebook.AppendChild(LNotebookPage);
		}
		
		protected virtual void BuildEmbeddedDetails(XmlElement AElement)
		{
			BuildEmbeddedDetails(AElement, FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar);
		}
		
		protected abstract void InternalBuild(XmlElement AElement);
		
		public virtual XmlDocument Build()
		{
			XmlDocument LDocument = new XmlDocument();
			XmlElement LInterface = BuildInterface(LDocument);
			BuildSource(LInterface);
			InternalBuild(LInterface);
			return LDocument;
		}
		
		protected virtual XmlElement BuildElement(XmlElement AXmlElement, Element AElement)
		{
			XmlElement LElement = AXmlElement.OwnerDocument.CreateElement(AElement.ElementType.ToLower());
			LElement.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, AElement.Name);
			if ((AElement.Title != null) && (AElement.Title != String.Empty))
				LElement.SetAttribute("title", AElement.Title);
			if (AElement.Hint != String.Empty)
				LElement.SetAttribute("hint", AElement.Hint);
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in AElement.Properties)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < AElement.Properties.Count; LIndex++)
			{
				LTag = AElement.Properties[LIndex];
			#endif
				LElement.SetAttribute(LTag.Name.ToLower(), LTag.Value);
			}
			return LElement;
		}
		
		protected virtual void SkipContainerElement(XmlElement AElement, ContainerElement AContainerElement)
		{
			for (int LIndex = 0; LIndex < AContainerElement.Elements.Count; LIndex++)
				if (AContainerElement.Elements[LIndex] is ContainerElement)
					SkipContainerElement(AElement, ((ContainerElement)AContainerElement.Elements[LIndex]));
				else
					AElement.AppendChild(BuildElement(AElement, AContainerElement.Elements[LIndex]));
		}
		
		// Build container element is called for grids and searches only, so skip groups used to order the columns
		protected virtual XmlElement BuildContainerElement(XmlElement AElement, ContainerElement AContainerElement)
		{
			XmlElement LContainerElement = BuildElement(AElement, AContainerElement);
			SkipContainerElement(LContainerElement, AContainerElement);
			return LContainerElement;
		}
		
		protected virtual XmlElement BuildElementNode(XmlElement AElement, ElementNode ANode)
		{
			return BuildElement(AElement, ANode.Element);
		}
		
		protected virtual XmlElement BuildColumnNode(XmlElement AElement, ColumnNode ANode)
		{
			XmlElement LElement = AElement.OwnerDocument.CreateElement("column");
			LElement.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, GetUniqueName());
			return LElement;
		}
		
		protected virtual XmlElement BuildRowNode(XmlElement AElement, RowNode ANode)
		{
			XmlElement LElement = AElement.OwnerDocument.CreateElement("row");
			LElement.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, GetUniqueName());
			return LElement;
		}
		
		protected virtual XmlElement BuildLayoutNode(XmlElement AElement, LayoutNode ANode)
		{
			XmlElement LElement = null;
			
			if (ANode is ElementNode)	
			{
				GroupElement LGroupElement = ((ElementNode)ANode).Element as GroupElement;
				if ((LGroupElement != null) && !LGroupElement.ContainsMultipleElements())
					ANode = ANode.Children[0]; // Skip the group node
				LElement = BuildElementNode(AElement, (ElementNode)ANode);
			}
			else if (ANode is ColumnNode)
				LElement = BuildColumnNode(AElement, (ColumnNode)ANode);
			else if (ANode is RowNode)
				LElement = BuildRowNode(AElement, (RowNode)ANode);

			foreach (LayoutNode LNode in ANode.Children)
				LElement.AppendChild(BuildLayoutNode(LElement, LNode));
				
			return LElement;
		}
	}

	public abstract class PluralDocumentBuilder : DocumentBuilder
	{
		public PluralDocumentBuilder(DerivationInfo ADerivationInfo) : base(ADerivationInfo)  {}

		protected virtual void BuildNavigationActions(XmlElement AElement)
		{
			// Response.Write(String.Format("<sourceaction bop:name='MoveFirst' source='{0}' text='&amp;First' action='First' />\r\n", SourceName));
			XmlElement LAction = AElement.OwnerDocument.CreateElement("sourceaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MoveFirst");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("text", "&First");
			LAction.SetAttribute("action", "First");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("First"));
			AElement.AppendChild(LAction);
			
			// Response.Write(String.Format("<sourceaction bop:name='MovePrior' source='{0}' text='&amp;Prior' action='Prior' />\r\n", SourceName));
			LAction = AElement.OwnerDocument.CreateElement("sourceaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MovePrior");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("text", "&Prior");
			LAction.SetAttribute("action", "Prior");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Prior"));
			AElement.AppendChild(LAction);
			
			// Response.Write(String.Format("<sourceaction bop:name='MoveNext' source='{0}' text='&amp;Next' action='Next' />\r\n", SourceName));
			LAction = AElement.OwnerDocument.CreateElement("sourceaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MoveNext");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("text", "&Next");
			LAction.SetAttribute("action", "Next");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Next"));
			AElement.AppendChild(LAction);
			
			// Response.Write(String.Format("<sourceaction bop:name='MoveLast' source='{0}' text='&amp;Last' action='Last' />\r\n", SourceName));
			LAction = AElement.OwnerDocument.CreateElement("sourceaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MoveLast");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("text", "&Last");
			LAction.SetAttribute("action", "Last");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Last"));
			AElement.AppendChild(LAction);
			
			// Response.Write(String.Format("<sourceaction bop:name='Refresh' source='{0}' text='&amp;Refresh' action='Refresh' />\r\n", SourceName));
			LAction = AElement.OwnerDocument.CreateElement("sourceaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "Refresh");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("text", "&Refresh");
			LAction.SetAttribute("action", "Refresh");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Refresh"));
			AElement.AppendChild(LAction);
			
			// Response.Write("<menu bop:name='NavigationMenu' text='&amp;Navigation'>");
			XmlElement LMenu = AElement.OwnerDocument.CreateElement("menu");
			LMenu.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "NavigationMenu");
			LMenu.SetAttribute("text", "&Navigation");
			AElement.AppendChild(LMenu);

			// Response.Write("<menu bop:name='MoveFirstMenu' action='MoveFirst'/>");
			XmlElement LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MoveFirstMenu");
			LMenuItem.SetAttribute("action", "MoveFirst");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='MovePriorMenu' action='MovePrior'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MovePriorMenu");
			LMenuItem.SetAttribute("action", "MovePrior");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='MoveNextMenu' action='MoveNext'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MoveNextMenu");
			LMenuItem.SetAttribute("action", "MoveNext");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='MoveLastMenu' action='MoveLast'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MoveLastMenu");
			LMenuItem.SetAttribute("action", "MoveLast");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='NavSepMenu1' text='-'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "NavSepMenu1");
			LMenuItem.SetAttribute("text", "-");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='RefreshMenu' action='Refresh'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RefreshMenu");
			LMenuItem.SetAttribute("action", "Refresh");
			LMenu.AppendChild(LMenuItem);
		}

		protected virtual XmlElement BuildRootBrowseColumn(XmlElement AElement)
		{
			// "<column bop:name='RootBrowseColumn'> ... </column>"
			XmlElement LRootBrowseColumn = AElement.OwnerDocument.CreateElement("column");
			LRootBrowseColumn.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootBrowseColumn");
			AElement.AppendChild(LRootBrowseColumn);
			return LRootBrowseColumn;
		}
	}
	
	public abstract class SearchableDocumentBuilder : PluralDocumentBuilder
	{
		public SearchableDocumentBuilder(DerivationInfo ADerivationInfo, SearchElement ASearchElement, GridElement AGridElement) : base(ADerivationInfo)
		{
			FSearchElement = ASearchElement;
			FGridElement = AGridElement;
		}

		protected SearchElement FSearchElement;
		public SearchElement SearchElement { get { return FSearchElement; } }
		
		protected GridElement FGridElement;
		public GridElement GridElement { get { return FGridElement; } }
		
		protected virtual void BuildSearch(XmlElement AElement)
		{
			AElement.AppendChild(BuildContainerElement(AElement, SearchElement));
		}
		
		protected virtual void BuildGrid(XmlElement AElement)
		{
			AElement.AppendChild(BuildContainerElement(AElement, GridElement));
		}

		protected virtual XmlElement BuildGridRow(XmlElement AElement)
		{
			XmlElement LGridRow = AElement.OwnerDocument.CreateElement("row");
			LGridRow.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "GridRow");
			AElement.AppendChild(LGridRow);
			return LGridRow;
		}
	}
	
	public class ListDocumentBuilder : SearchableDocumentBuilder
	{
		public ListDocumentBuilder(DerivationInfo ADerivationInfo, SearchElement ASearchElement, GridElement AGridElement) : base(ADerivationInfo, ASearchElement, AGridElement) {}

		protected override void InternalBuild(XmlElement AElement)
		{
			BuildDetailActions(AElement);
			BuildExtensionActions(AElement);
			BuildLookupActions(AElement);
			BuildNavigationActions(AElement);
			XmlElement LRootColumn = BuildRootBrowseColumn(AElement);
			BuildSearch(LRootColumn);
			XmlElement LGridRow = BuildGridRow(LRootColumn);
			BuildGrid(LGridRow);
			BuildEmbeddedDetails(LRootColumn);
		}
	}

	public class BrowseDocumentBuilder : SearchableDocumentBuilder
	{
		public BrowseDocumentBuilder(DerivationInfo ADerivationInfo, SearchElement ASearchElement, GridElement AGridElement) : base(ADerivationInfo, ASearchElement, AGridElement) {}

		// Frontend.Secure = "Visible" | "Disabled" | "Hidden" (Default)
		
		protected virtual void BuildUpdateActions(XmlElement AElement)
		{
			XmlElement LAction;

			SecureBehavior LSecureBehavior = SecureBehavior.Visible;
			if (!FDerivationInfo.Program.Plan.HasRight(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.GetRight(Schema.RightNames.Insert)))
				LSecureBehavior = (SecureBehavior)Enum.Parse(typeof(SecureBehavior), DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Secure", DerivationUtility.CAdd, "Hidden"), true);

			LAction = AElement.OwnerDocument.CreateElement("showformaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "Add");
			LAction.SetAttribute("text", "&Add...");
			LAction.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					String.Format("{0}.Document", DerivationUtility.CAdd),
					FDerivationInfo.BuildDerivationExpression
					(
						DerivationUtility.CAdd,
						DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.CAdd, FDerivationInfo.Query),
						FDerivationInfo.MasterKeyNames,
						FDerivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.CAdd, FDerivationInfo.Elaborate.ToString()))
					)
				)
			);
			LAction.SetAttribute("mode", "Insert");
			LAction.SetAttribute("sourcelinktype", "Detail");
			LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
			if (FDerivationInfo.DetailKeyNames != String.Empty)
				LAction.SetAttribute("sourcelink.attachmaster",  "True");
			LAction.SetAttribute("hint", "Add a new row.");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Add"));
			switch (LSecureBehavior)
			{
				case SecureBehavior.Disabled : LAction.SetAttribute("enabled", "False"); break;
				case SecureBehavior.Hidden : LAction.SetAttribute("visible", "False"); break;
			}
			AElement.AppendChild(LAction);
			
			LSecureBehavior = SecureBehavior.Visible;
			if (!FDerivationInfo.Program.Plan.HasRight(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.GetRight(Schema.RightNames.Update)))
				LSecureBehavior = (SecureBehavior)Enum.Parse(typeof(SecureBehavior), DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Secure", DerivationUtility.CEdit, "Hidden"), true);

			LAction = AElement.OwnerDocument.CreateElement("showformaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "Edit");
			LAction.SetAttribute("text", "&Edit...");
			LAction.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData,
					String.Format("{0}.Document", DerivationUtility.CEdit),
					FDerivationInfo.BuildDerivationExpression
					(
						DerivationUtility.CEdit,
						DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.CEdit, FDerivationInfo.Query),
						FDerivationInfo.MasterKeyNames,
						FDerivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.CEdit, FDerivationInfo.Elaborate.ToString()))
					)
				)
			);
			LAction.SetAttribute("mode", "Edit");
			LAction.SetAttribute("sourcelinktype", "Detail");
			LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("sourcelink.masterkeynames", FDerivationInfo.KeyNames);
			LAction.SetAttribute("sourcelink.detailkeynames", FDerivationInfo.KeyNames);
			LAction.SetAttribute("hint", "Edit the current row.");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Edit"));
			switch (LSecureBehavior)
			{
				case SecureBehavior.Disabled : LAction.SetAttribute("enabled", "False"); break;
				case SecureBehavior.Hidden : LAction.SetAttribute("visible", "False"); break;
			}
			AElement.AppendChild(LAction);
			
			LSecureBehavior = SecureBehavior.Visible;
			if (!FDerivationInfo.Program.Plan.HasRight(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.GetRight(Schema.RightNames.Delete)))
				LSecureBehavior = (SecureBehavior)Enum.Parse(typeof(SecureBehavior), DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Secure", "Delete", "Hidden"), true);			

			LAction = AElement.OwnerDocument.CreateElement("showformaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "Delete");
			LAction.SetAttribute("text", "&Delete...");
			LAction.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					String.Format("{0}.Document", DerivationUtility.CDelete),
					FDerivationInfo.BuildDerivationExpression
					(
						DerivationUtility.CDelete,
						DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.CDelete, FDerivationInfo.Query), 
						FDerivationInfo.MasterKeyNames, 
						FDerivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.CDelete, FDerivationInfo.Elaborate.ToString()))
					)
				)
			);
			LAction.SetAttribute("mode", "Delete");
			LAction.SetAttribute("sourcelinktype", "Detail");
			LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("sourcelink.masterkeynames", FDerivationInfo.KeyNames);
			LAction.SetAttribute("sourcelink.detailkeynames", FDerivationInfo.KeyNames);
			LAction.SetAttribute("hint", "Delete the current row.");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Delete"));
			switch (LSecureBehavior)
			{
				case SecureBehavior.Disabled : LAction.SetAttribute("enabled", "False"); break;
				case SecureBehavior.Hidden : LAction.SetAttribute("visible", "False"); break;
			}
			AElement.AppendChild(LAction);
			
			LAction = AElement.OwnerDocument.CreateElement("showformaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "View");
			LAction.SetAttribute("text", "&View...");
			LAction.SetAttribute
			(
				"document", 
				DerivationUtility.GetTag
				(
					FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, 
					String.Format("{0}.Document", DerivationUtility.CView), 
					FDerivationInfo.BuildDerivationExpression
					(
						DerivationUtility.CView,
						DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Query", DerivationUtility.CView, FDerivationInfo.Query),
						FDerivationInfo.MasterKeyNames,
						FDerivationInfo.DetailKeyNames,
						Boolean.Parse(DerivationUtility.GetTag(FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableVar.MetaData, "Elaborate", DerivationUtility.CView, FDerivationInfo.Elaborate.ToString()))
					)
				)
			);
			LAction.SetAttribute("sourcelinktype", "Detail");
			LAction.SetAttribute("sourcelink.source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("sourcelink.masterkeynames", FDerivationInfo.KeyNames);
			LAction.SetAttribute("sourcelink.detailkeynames", FDerivationInfo.KeyNames);
			LAction.SetAttribute("hint", "View the current row.");
			LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("View"));
			AElement.AppendChild(LAction);
			
			// Response.Write(String.Format("<editfilteraction bop:name='EditFilter' source='{0}' text='&amp;Filter...' />\r\n", SourceName));
			LAction = AElement.OwnerDocument.CreateElement("editfilteraction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "EditFilter");
			LAction.SetAttribute("text", "&Filter...");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			AElement.AppendChild(LAction);

			// Response.Write(String.Format("<menu bop:name='{0}Menu' text='{0}'>", TableTitle));
			XmlElement LMenu = AElement.OwnerDocument.CreateElement("menu");
			LMenu.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}Menu", FDerivationInfo.Query));
			LMenu.SetAttribute("text", FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle);
			LMenu.AppendChild(LAction);
			
			// Response.Write("<menu bop:name='AddMenu' action='Add'/>");
			XmlElement LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "AddMenu");
			LMenuItem.SetAttribute("action", "Add");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='EditMenu' action='Edit'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "EditMenu");
			LMenuItem.SetAttribute("action", "Edit");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='DeleteMenu' action='Delete'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "DeleteMenu");
			LMenuItem.SetAttribute("action", "Delete");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='ViewMenu' action='View'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "ViewMenu");
			LMenuItem.SetAttribute("action", "View");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='UpdateSepMenu1' text='-'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "UpdateSepMenu1");
			LMenuItem.SetAttribute("text", "-");
			LMenu.AppendChild(LMenuItem);

			// Response.Write("<menu bop:name='EditFilterMenu' action='EditFilter'/>");
			LMenuItem = AElement.OwnerDocument.CreateElement("menu");
			LMenuItem.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "EditFilterMenu");
			LMenuItem.SetAttribute("action", "EditFilter");
			LMenu.AppendChild(LMenuItem);
		}
		
		protected virtual void BuildGridBar(XmlElement AElement)
		{
			// Response.Write("<column bop:name='GridBar'>");
			XmlElement LColumn = AElement.OwnerDocument.CreateElement("column");
			LColumn.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "GridBar");
			
			// Response.Write("<trigger action='Add' bop:name='AddTrigger' imagewidth='11' imageheight='13'/>");
			XmlElement LTrigger = AElement.OwnerDocument.CreateElement("trigger");
			LTrigger.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "AddTrigger");
			LTrigger.SetAttribute("action", "Add");
			LTrigger.SetAttribute("imagewidth", "11");
			LTrigger.SetAttribute("imageheight", "13");
			LColumn.AppendChild(LTrigger);
			
			// Response.Write("<trigger action='Edit' bop:name='EditTrigger' imagewidth='11' imageheight='13'/>");
			LTrigger = AElement.OwnerDocument.CreateElement("trigger");
			LTrigger.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "EditTrigger");
			LTrigger.SetAttribute("action", "Edit");
			LTrigger.SetAttribute("imagewidth", "11");
			LTrigger.SetAttribute("imageheight", "13");
			LColumn.AppendChild(LTrigger);
			
			// Response.Write("<trigger action='Delete' bop:name='DeleteTrigger' imagewidth='11' imageheight='13'/>");
			LTrigger = AElement.OwnerDocument.CreateElement("trigger");
			LTrigger.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "DeleteTrigger");
			LTrigger.SetAttribute("action", "Delete");
			LTrigger.SetAttribute("imagewidth", "11");
			LTrigger.SetAttribute("imageheight", "13");
			LColumn.AppendChild(LTrigger);
			
			// Response.Write("<trigger action='View' bop:name='ViewTrigger' imagewidth='11' imageheight='13'/>");
			LTrigger = AElement.OwnerDocument.CreateElement("trigger");
			LTrigger.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "ViewTrigger");
			LTrigger.SetAttribute("action", "View");
			LTrigger.SetAttribute("imagewidth", "11");
			LTrigger.SetAttribute("imageheight", "13");
			LColumn.AppendChild(LTrigger);
			
			// Response.Write("</column>");
			AElement.AppendChild(LColumn);
		}
		
		protected override XmlElement BuildInterface(XmlDocument ADocument)
		{
			XmlElement LInterface = base.BuildInterface(ADocument);
			LInterface.SetAttribute("OnDefault", "Edit");
			return LInterface;
		}

		protected override void InternalBuild(XmlElement AElement)
		{
			BuildDetailActions(AElement);
			BuildExtensionActions(AElement);
			BuildLookupActions(AElement);
			BuildUpdateActions(AElement);
			BuildNavigationActions(AElement);
			XmlElement LRootColumn = BuildRootBrowseColumn(AElement);
			BuildSearch(LRootColumn);
			XmlElement LGridRow = BuildGridRow(LRootColumn);
			BuildGrid(LGridRow);
			BuildGridBar(LGridRow);
			BuildEmbeddedDetails(LRootColumn);
		}
	}
	
	public class OrderBrowseDocumentBuilder : PluralDocumentBuilder
	{
		public OrderBrowseDocumentBuilder(DerivationInfo ADerivationInfo) : base(ADerivationInfo) {}
		
		protected override void BuildSource(XmlElement AElement)
		{
			XmlElement LSource = AElement.OwnerDocument.CreateElement("source");
			LSource.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, FDerivationInfo.MainSourceName);
			LSource.SetAttribute("usebrowse", "False");
			LSource.SetAttribute("expression", GetOrderExpression());
			LSource.SetAttribute("isreadonly", "True");
			AElement.AppendChild(LSource);
		}
		
		protected virtual string GetColumnTitle(Schema.TableVarColumn AColumn)
		{
			return
				DerivationUtility.GetTag
				(
					AColumn.MetaData,
					"Caption",
					FDerivationInfo.PageType,
					FDerivationInfo.ElaboratedExpression.Columns[AColumn.Name].GetTitleSeed() + 
						DerivationUtility.GetTag(AColumn.MetaData, "Title", FDerivationInfo.PageType, Schema.Object.Unqualify(AColumn.Name))
				);
		}
		
		protected virtual string GetDefaultOrderTitle(Schema.Order AOrder)
		{
			StringBuilder LName = new StringBuilder();
			foreach (Schema.OrderColumn LColumn in AOrder.Columns)
			{
				if (IsColumnVisible(LColumn.Column))
				{
					if (LName.Length > 0)
						LName.Append(", ");
					LName.Append(GetColumnTitle(LColumn.Column));
					if (!LColumn.Ascending)
						LName.Append(" (descending)");
				}
			}

			return String.Format("by {0}", LName.ToString());
		}
		
		protected bool IsColumnVisible(Schema.TableVarColumn AColumn)
		{
			return DerivationUtility.IsColumnVisible(AColumn, FDerivationInfo.PageType);
		}
		
		protected bool IsOrderVisible(Schema.Order AOrder)
		{
			return DerivationUtility.IsOrderVisible(AOrder, FDerivationInfo.PageType);
		}
		
		protected virtual string GetOrderExpression()
		{
			// Build a table selector with a row selector for each possible order in the page expression
			Schema.Orders LOrders = new Schema.Orders();
			LOrders.AddRange(FDerivationInfo.TableVar.Orders);
			Schema.Order LOrderForKey;
			foreach (Schema.Key LKey in FDerivationInfo.TableVar.Keys)
			{
				LOrderForKey = FDerivationInfo.Program.OrderFromKey(LKey);
				if (!LOrders.Contains(LOrderForKey))
					LOrders.Add(LOrderForKey);
			}
				
			StringBuilder LExpression = new StringBuilder();
			foreach (Schema.Order LOrder in LOrders)
				if (IsOrderVisible(LOrder))
					LExpression.AppendFormat
					(
						@"{0}row {{ ""{1}"" Description, ""{2}"" OrderName }} ", 
						LExpression.Length > 0 ? ", " : String.Empty, 
						DerivationUtility.GetTag(LOrder.MetaData, "Title", FDerivationInfo.PageType, GetDefaultOrderTitle(LOrder)), 
						LOrder.Name
					);

			// return an empty table if there were no visible orders, otherwise the expression will be invalid
			return String.Format("table of {{ OrderName : String, Description : String }} {{ {0} }} order by {{ OrderName }}", LExpression.ToString());
		}

		protected override void InternalBuild(XmlElement AElement)
		{
			BuildNavigationActions(AElement);
			XmlElement LRootColumn = BuildRootBrowseColumn(AElement);
			XmlElement LGrid = AElement.OwnerDocument.CreateElement("grid");
			LGrid.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MainGrid");
			LGrid.SetAttribute("source", "Main");
			LGrid.SetAttribute("readonly", "True");
			XmlElement LGridColumn = AElement.OwnerDocument.CreateElement("textcolumn");
			LGridColumn.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "MainGridColumnDescription");
			LGridColumn.SetAttribute("columnname", "Description");
			LGridColumn.SetAttribute("width", "45");
			LGrid.AppendChild(LGridColumn);
			LRootColumn.AppendChild(LGrid);
		}
	}
	
	public class ReportDocumentBuilder : DocumentBuilder
	{
		public ReportDocumentBuilder(DerivationInfo ADerivationInfo) : base(ADerivationInfo) {}
		
		private int COffsetMultiplier = 5;

		protected override XmlElement BuildInterface(XmlDocument ADocument)
		{
			XmlElement LReport = ADocument.CreateElement("report");
			LReport.SetAttribute("xmlns:bop", "www.alphora.com/schemas/bop");
			ADocument.AppendChild(LReport);
			return LReport;
		}
		
		protected virtual void BuildHeader(XmlElement AElement)
		{
			XmlElement LHeader = AElement.OwnerDocument.CreateElement("header");
			LHeader.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "PageHeader");
			LHeader.SetAttribute("height", "15");
			AElement.AppendChild(LHeader);
			
			XmlElement LTextArea = AElement.OwnerDocument.CreateElement("textarea");
			LTextArea.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "ReportName");
			LTextArea.SetAttribute("text", String.Format("{0} Report", FDerivationInfo.ElaboratedExpression.MainElaboratedTableVar.TableTitle));
			LTextArea.SetAttribute("y", "0");
			LHeader.AppendChild(LTextArea);

			XmlElement LDate = AElement.OwnerDocument.CreateElement("date");
			LDate.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "HeaderDate");
			LDate.SetAttribute("x", "520");
			LDate.SetAttribute("y", "0");
			LHeader.AppendChild(LDate);			
		}
		
		protected virtual void BuildDataBandHeader(XmlElement AElement) 
		{
			int LCurrentOffset = 10;
			int LWidth;
			foreach (Schema.TableVarColumn LColumn in FDerivationInfo.TableVar.Columns) 
			{
				if (Convert.ToBoolean(DerivationUtility.GetTag(LColumn.MetaData, "Visible", FDerivationInfo.PageType, "True"))) 
				{
					LWidth = Convert.ToInt32(DerivationUtility.GetTag(LColumn.MetaData, "Width", FDerivationInfo.PageType, "40"));
					XmlElement LTextArea = AElement.OwnerDocument.CreateElement("textarea");
					LTextArea.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}Column{1}Header", FDerivationInfo.MainSourceName, LColumn.Name));
					LTextArea.SetAttribute("text", DerivationUtility.GetTag(LColumn.MetaData, "Title", FDerivationInfo.PageType, LColumn.Name));
					LTextArea.SetAttribute("x", LCurrentOffset.ToString());
					LTextArea.SetAttribute("y", "3");
					LTextArea.SetAttribute("maxlength", LWidth.ToString());
					AElement.AppendChild(LTextArea);
					
					LCurrentOffset += LWidth * COffsetMultiplier;
				}
			}
		}

		protected virtual void BuildDataBandContent(XmlElement AElement) 
		{
			int LCurrentOffset = 10;
			int LWidth;
			foreach (Schema.TableVarColumn LColumn in FDerivationInfo.TableVar.Columns) 
			{
				if (Convert.ToBoolean(DerivationUtility.GetTag(LColumn.MetaData, "Visible", FDerivationInfo.PageType, "True"))) 
				{
					LWidth = Convert.ToInt32(DerivationUtility.GetTag(LColumn.MetaData, "Width", FDerivationInfo.PageType, "40"));
					XmlElement LField = AElement.OwnerDocument.CreateElement("field");
					LField.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, String.Format("{0}Column{1}", FDerivationInfo.MainSourceName, LColumn.Name));
					LField.SetAttribute("source", FDerivationInfo.MainSourceName);
					LField.SetAttribute("columnname", LColumn.Name);
					LField.SetAttribute("x", LCurrentOffset.ToString());
					LField.SetAttribute("maxlength", LWidth.ToString());
					AElement.AppendChild(LField);

					LCurrentOffset += LWidth * COffsetMultiplier;
				}
			}
		}

		protected virtual void BuildDataBand(XmlElement AElement)
		{
			XmlElement LDataBand = AElement.OwnerDocument.CreateElement("databand");
			LDataBand.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootDataBand");
			LDataBand.SetAttribute("source", "Main");
			AElement.AppendChild(LDataBand);
			
			XmlElement LHeader = AElement.OwnerDocument.CreateElement("header");
			LHeader.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootDataBandHeader");
			LDataBand.AppendChild(LHeader);
			
			XmlElement LBox = AElement.OwnerDocument.CreateElement("box");
			LBox.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootDataBandHeaderBox");
			LBox.SetAttribute("height", "11");
			LBox.SetAttribute("width", "600");
			LBox.SetAttribute("x", "5");
			LBox.SetAttribute("y", "2");
			LHeader.AppendChild(LBox);
			
			BuildDataBandHeader(LHeader);
			
			XmlElement LStaticGroup = AElement.OwnerDocument.CreateElement("staticgroup");
			LStaticGroup.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootDataBandContent");
			LDataBand.AppendChild(LStaticGroup);
			
			BuildDataBandContent(LStaticGroup);
		}
		
		protected virtual void BuildFooter(XmlElement AElement)
		{
			XmlElement LFooter = AElement.OwnerDocument.CreateElement("footer");
			LFooter.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "PageFooter");
			LFooter.SetAttribute("height", "20");
			AElement.AppendChild(LFooter);
			
			XmlElement LTextArea = AElement.OwnerDocument.CreateElement("textarea");
			LTextArea.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "PageFooterPageText");
			LTextArea.SetAttribute("text", "Page");
			LTextArea.SetAttribute("x", "290");
			LTextArea.SetAttribute("y", "5");
			LFooter.AppendChild(LTextArea);
			
			XmlElement LPageNumber = AElement.OwnerDocument.CreateElement("pagenumber");
			LPageNumber.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "PageFooterPageNumber");
			LPageNumber.SetAttribute("x", "310");
			LPageNumber.SetAttribute("y", "5");
			LFooter.AppendChild(LPageNumber);
		}
		
		
		protected override void InternalBuild(XmlElement AElement)
		{
			// visible elements
			BuildHeader(AElement);
			BuildDataBand(AElement);
			BuildFooter(AElement);
		}
	}
	
	public class SingularDocumentBuilder : DocumentBuilder
	{
		public SingularDocumentBuilder(DerivationInfo ADerivationInfo, LayoutNode ALayoutNode) : base(ADerivationInfo)
		{
			FLayoutNode = ALayoutNode;
		}
		
		protected LayoutNode FLayoutNode;
		public LayoutNode LayoutNode { get { return FLayoutNode; } }
		
		protected virtual XmlElement BuildRootEditColumn(XmlElement AElement)
		{
			// "<column bop:name='RootEditColumn'> ... </column>"
			XmlElement LRootEditColumn = AElement.OwnerDocument.CreateElement("column");
			LRootEditColumn.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootEditColumn");
			AElement.AppendChild(LRootEditColumn);
			return LRootEditColumn;
		}
		
		protected virtual void BuildBody(XmlElement AElement)
		{
			// Build the body based on the LayoutNodes given
			if (FLayoutNode != null)
				AElement.AppendChild(BuildLayoutNode(AElement, LayoutNode));
		}
		
		protected virtual void BuildValidateAction(XmlElement AElement)
		{
			// Response.Write(String.Format("<sourceaction bop:name='MoveFirst' source='{0}' text='&amp;First' action='First' />\r\n", SourceName));
			XmlElement LAction = AElement.OwnerDocument.CreateElement("sourceaction");
			LAction.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "Validate");
			LAction.SetAttribute("source", FDerivationInfo.MainSourceName);
			LAction.SetAttribute("text", "&Validate");
			LAction.SetAttribute("action", "Validate");
			//LAction.SetAttribute("image", FDerivationInfo.BuildImageExpression("Validate"));
			AElement.AppendChild(LAction);
		}

		protected override void InternalBuild(XmlElement AElement)
		{
			BuildDetailActions(AElement);
			BuildExtensionActions(AElement);
			BuildLookupActions(AElement);
			BuildValidateAction(AElement);
			XmlElement LRootColumn = BuildRootEditColumn(AElement);
			BuildBody(LRootColumn);
			BuildEmbeddedDetails(LRootColumn);
		}
	}

	public class DeleteDocumentBuilder : SingularDocumentBuilder
	{
		public DeleteDocumentBuilder(DerivationInfo ADerivationInfo, LayoutNode ALayoutNode) : base(ADerivationInfo, ALayoutNode) {}

		protected override XmlElement BuildRootEditColumn(XmlElement AElement)
		{
			XmlElement LRootDeleteRow = AElement.OwnerDocument.CreateElement("row");
			LRootDeleteRow.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "RootDeleteRow");
			AElement.AppendChild(LRootDeleteRow);

			XmlElement LDeleteImage = AElement.OwnerDocument.CreateElement("staticimage");
			LDeleteImage.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "DeleteImage");
			LDeleteImage.SetAttribute("image", FDerivationInfo.BuildImageExpression("Warning"));
			LDeleteImage.SetAttribute("imagewidth", "32");
			LDeleteImage.SetAttribute("imageheight", "32");
			LRootDeleteRow.AppendChild(LDeleteImage);

			XmlElement LDeleteGroup = AElement.OwnerDocument.CreateElement("group");
			LDeleteGroup.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "DeleteGroup");
			LDeleteGroup.SetAttribute("title", Strings.Get("DeleteText"));
			LRootDeleteRow.AppendChild(LDeleteGroup);

			XmlElement LDeleteColumn = AElement.OwnerDocument.CreateElement("column");
			LDeleteColumn.SetAttribute("name", BOP.Serializer.CBOPNamespaceURI, "DeleteColumn");
			LDeleteGroup.AppendChild(LDeleteColumn);

			return LDeleteColumn;
		}
		

	}
}

