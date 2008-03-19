/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Xml;
using System.Text;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.Frontend;
using Alphora.Dataphor.Frontend.Server.Structuring;
using Alphora.Dataphor.Frontend.Server.Elaboration;
using Alphora.Dataphor.Frontend.Server.Production;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Server.Derivation
{
	public class InterfaceBuilder : System.Object
	{
		public InterfaceBuilder(ServerProcess AProcess, DerivationSeed ASeed) : base()
		{
			FProcess = AProcess;
			FSeed = ASeed;
		}
		
		protected ServerProcess FProcess;
		protected DerivationSeed FSeed;

		// ElaborableExpression seed
		protected Schema.Catalog FElaborableCatalog;
		protected string FElaborableTableVarName;
		
		protected Schema.Catalog FCatalog; // main catalog where all referenced objects can be found

		protected virtual void Describe()
		{
			FCatalog = FProcess.Plan.Catalog;
			Schema.Object LObject = null;
			if (Parser.IsValidQualifiedIdentifier(FSeed.Query))
				LObject = Compiler.ResolveCatalogIdentifier(FProcess.Plan, FSeed.Query, false);

			if (LObject != null)
			{
				FElaborableTableVarName = LObject.Name;
				FElaborableCatalog = FProcess.Plan.Catalog;
			}
			else
			{
				IServerExpressionPlan LPlan = ((IServerProcess)FProcess).PrepareExpression(String.Format(@"select {0}", FSeed.Query), null);
				try
				{
					FElaborableTableVarName = LPlan.TableVar.Name;
					FElaborableCatalog = LPlan.Catalog;
				}
				finally
				{
					((IServerProcess)FProcess).UnprepareExpression(LPlan);
				}
			}
		}

		// ElaboratedExpression
		protected ElaboratedExpression FElaboratedExpression;
		
		protected virtual void Elaborate()
		{
			// Set the main table
			FElaboratedExpression = 
				new ElaboratedExpression
				(
					FProcess,
					FSeed.Query,
					FSeed.Elaborate,
					FElaborableCatalog[FElaborableTableVarName] as Schema.TableVar,
					FCatalog,
					FSeed.DetailKeyNames == String.Empty
						? new String[0]
						: FSeed.DetailKeyNames.Split(DerivationInfo.CColumnNameDelimiters), 
					DerivationUtility.CMainElaboratedTableName, 
					FSeed.PageType
				);
		}
		
		// Prepare
		protected string FExpression;
		protected Schema.TableVar FTableVar;
		protected DerivationInfo FDerivationInfo;

		protected string GetKeyNames(Schema.TableVar ATableVar)
		{
			StringBuilder LKeyNames = new StringBuilder();
			Schema.Key LClusteringKey = ATableVar.FindClusteringKey();
			for (int LIndex = 0; LIndex < LClusteringKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LKeyNames.Append(", ");
				LKeyNames.Append(Schema.Object.Qualify(LClusteringKey.Columns[LIndex].Name, DerivationUtility.CMainElaboratedTableName));
			}
			return LKeyNames.ToString();
		}
		
		protected bool IsOrderVisible(Schema.Order AOrder)
		{
			return DerivationUtility.IsOrderVisible(AOrder, FSeed.PageType);
		}
		
		protected Schema.Order GetDefaultOrder(Schema.TableVar ATableVar)
		{
			foreach (Schema.Order LOrder in ATableVar.Orders)
				if (Convert.ToBoolean(DerivationUtility.GetTag(LOrder.MetaData, "IsDefault", "False")))
					return new Schema.Order(LOrder);
			
			foreach (Schema.Key LKey in ATableVar.Keys)
				if (Convert.ToBoolean(DerivationUtility.GetTag(LKey.MetaData, "IsDefault", "False")))
					return new Schema.Order(LKey);
			
			if (ATableVar.Keys.Count > 0)
			{
				Schema.Order LOrder = new Schema.Order(ATableVar.FindClusteringKey());
				if (IsOrderVisible(LOrder))
					return LOrder;

				foreach (Schema.Key LKey in ATableVar.Keys)
				{
					LOrder = new Schema.Order(LKey);
					if (IsOrderVisible(LOrder))
						return LOrder;
				}
			}

			if (ATableVar.Orders.Count > 0)
				foreach (Schema.Order LOrder in ATableVar.Orders)
				{
					if (IsOrderVisible(LOrder))
						return LOrder;
				}

			return null;
		}

		protected virtual void Prepare()
		{
			Expression LExpression = FElaboratedExpression.Expression;
			FTableVar = FElaboratedExpression.MainElaboratedTableVar.TableVar;
			Schema.Order LOrder = GetDefaultOrder(FTableVar);
			if (LOrder != null)
			{
				BaseOrderExpression LBrowseExpression;
				if (Convert.ToBoolean(DerivationUtility.GetTag(FTableVar.MetaData, "UseBrowse", FElaboratedExpression.PageType, "True")))
					LBrowseExpression = new BrowseExpression();
				else
					LBrowseExpression = new OrderExpression();
				LBrowseExpression.Expression = LExpression;
				foreach (Schema.OrderColumn LColumn in LOrder.Columns)
				{
					OrderColumnDefinition LDefinition = LColumn.EmitStatement(EmitMode.ForCopy) as OrderColumnDefinition;
					LDefinition.ColumnName = Schema.Object.Qualify(LColumn.Column.Name, FElaboratedExpression.MainElaboratedTableVar.ElaboratedName);
					LBrowseExpression.Columns.Add(LDefinition);
				}
				LExpression = LBrowseExpression;
			}

			FExpression = 
				new D4TextEmitter().Emit
				(
					new CursorDefinition
					(
						LExpression, 
							CursorCapability.Navigable | 
							CursorCapability.BackwardsNavigable | 
							CursorCapability.Bookmarkable | 
							CursorCapability.Searchable | 
							CursorCapability.Updateable, 
						CursorIsolation.Browse, 
						CursorType.Dynamic
					)
				);

			// Build the derivation info structure for use in structuring, layout and document production.			
			FDerivationInfo = new DerivationInfo();
			FDerivationInfo.Process = FProcess;
			FDerivationInfo.PageType = FSeed.PageType;
			FDerivationInfo.Query = FSeed.Query;
			FDerivationInfo.Elaborate = FSeed.Elaborate;
			FDerivationInfo.MasterKeyNames = FSeed.MasterKeyNames;
			FDerivationInfo.DetailKeyNames = FSeed.DetailKeyNames;
			FDerivationInfo.KeyNames = GetKeyNames(FTableVar);
			FDerivationInfo.Expression = FExpression;
			FDerivationInfo.ElaboratedExpression = FElaboratedExpression;
			FDerivationInfo.TableVar = FTableVar;
			FDerivationInfo.MainSourceName = DerivationUtility.CMainSourceName;
			FDerivationInfo.IsReadOnly = DerivationUtility.IsReadOnlyPageType(FSeed.PageType);
		}
		
		public virtual XmlDocument Structure()
		{
			switch (FSeed.PageType)
			{
				case DerivationUtility.CBrowse :
					return 
						new BrowseDocumentBuilder
						(
							FDerivationInfo, 
							(SearchElement)new SearchStructureBuilder(FDerivationInfo).Build(), 
							(GridElement)new PluralStructureBuilder(FDerivationInfo).Build()
						).Build();
						
				case DerivationUtility.CList :
					return
						new ListDocumentBuilder
						(
							FDerivationInfo,
							(SearchElement)new SearchStructureBuilder(FDerivationInfo).Build(),
							(GridElement)new PluralStructureBuilder(FDerivationInfo).Build()
						).Build();
						
				case DerivationUtility.CBrowseReport :
					return new ReportDocumentBuilder(FDerivationInfo).Build();
				
				case DerivationUtility.COrderBrowse :
					return new OrderBrowseDocumentBuilder(FDerivationInfo).Build();
				
				case DerivationUtility.CAdd :
				case DerivationUtility.CEdit :
				case DerivationUtility.CView :
					return new SingularDocumentBuilder(FDerivationInfo, new LayoutBuilder().Layout(((GroupElement)new SingularStructureBuilder(FDerivationInfo).Build()).Elements)).Build();

				case DerivationUtility.CDelete :
					return new DeleteDocumentBuilder(FDerivationInfo, new LayoutBuilder().Layout(((GroupElement)new SingularStructureBuilder(FDerivationInfo).Build()).Elements)).Build();
				
				default : throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.UnknownPageType, FSeed.PageType);
			}
		}
		
		public virtual XmlDocument Build()
		{
			Describe();
			Elaborate();
			Prepare();
			return Structure();
		}
	}
}
