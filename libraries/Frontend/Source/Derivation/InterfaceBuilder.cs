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
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.Frontend;
using Alphora.Dataphor.Frontend.Server.Structuring;
using Alphora.Dataphor.Frontend.Server.Elaboration;
using Alphora.Dataphor.Frontend.Server.Production;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.Frontend.Server.Derivation
{
	public class InterfaceBuilder : System.Object
	{
		public InterfaceBuilder(Program program, DerivationSeed seed) : base()
		{
			_program = program;
			_process = program.ServerProcess;
			_seed = seed;
		}
		
		protected Program _program;
		protected ServerProcess _process;
		protected DerivationSeed _seed;

		// ElaborableExpression seed
		protected Schema.Catalog _elaborableCatalog;
		protected string _elaborableTableVarName;
		
		protected Schema.Catalog _catalog; // main catalog where all referenced objects can be found

		protected virtual void Describe()
		{
			_catalog = _process.Catalog;
			Schema.Object objectValue = null;
			if (Parser.IsValidQualifiedIdentifier(_seed.Query))
				objectValue = Compiler.ResolveCatalogIdentifier(_program.Plan, _seed.Query, false);

			if (objectValue != null)
			{
				_elaborableTableVarName = objectValue.Name;
				_elaborableCatalog = _process.Catalog;
			}
			else
			{
				IServerExpressionPlan plan = ((IServerProcess)_process).PrepareExpression(String.Format(@"select {0}{1}", _seed.Query, _seed.Query.IndexOf(Keywords.Capabilities) > 0 || !_seed.Elaborate ? "" : String.Format(" {0} {{ elaborable }}", Keywords.Capabilities)), null);
				try
				{
					_elaborableTableVarName = plan.TableVar.Name;
					_elaborableCatalog = plan.Catalog;
				}
				finally
				{
					((IServerProcess)_process).UnprepareExpression(plan);
				}
			}
		}

		// ElaboratedExpression
		protected ElaboratedExpression _elaboratedExpression;
		
		protected virtual void Elaborate()
		{
			// Set the main table
			_elaboratedExpression = 
				new ElaboratedExpression
				(
					_program,
					_seed.Query,
					_seed.Elaborate,
					_elaborableCatalog[_elaborableTableVarName] as Schema.TableVar,
					_catalog,
					_seed.DetailKeyNames == String.Empty
						? new String[0]
						: _seed.DetailKeyNames.Split(DerivationInfo.ColumnNameDelimiters), 
					DerivationUtility.MainElaboratedTableName, 
					_seed.PageType
				);
		}
		
		// Prepare
		protected string _expression;
		protected Schema.TableVar _tableVar;
		protected DerivationInfo _derivationInfo;

		protected string GetKeyNames(Schema.TableVar tableVar)
		{
			StringBuilder keyNames = new StringBuilder();
			Schema.Key clusteringKey = _program.FindClusteringKey(tableVar);
			for (int index = 0; index < clusteringKey.Columns.Count; index++)
			{
				if (index > 0)
					keyNames.Append(", ");
				keyNames.Append(Schema.Object.Qualify(clusteringKey.Columns[index].Name, DerivationUtility.MainElaboratedTableName));
			}
			return keyNames.ToString();
		}
		
		protected bool IsOrderVisible(Schema.Order order)
		{
			return DerivationUtility.IsOrderVisible(order, _seed.PageType);
		}
		
		protected Schema.Order GetDefaultOrder(Schema.TableVar tableVar)
		{
			foreach (Schema.Order order in tableVar.Orders)
				if (Convert.ToBoolean(DerivationUtility.GetTag(order.MetaData, "IsDefault", "False")))
					return new Schema.Order(order);
			
			foreach (Schema.Key key in tableVar.Keys)
				if (Convert.ToBoolean(DerivationUtility.GetTag(key.MetaData, "IsDefault", "False")))
					return new Schema.Order(key);
			
			if (tableVar.Keys.Count > 0)
			{
				Schema.Order order = new Schema.Order(_program.FindClusteringKey(tableVar));
				if (IsOrderVisible(order))
					return order;

				foreach (Schema.Key key in tableVar.Keys)
				{
					order = new Schema.Order(key);
					if (IsOrderVisible(order))
						return order;
				}
			}

			if (tableVar.Orders.Count > 0)
				foreach (Schema.Order order in tableVar.Orders)
				{
					if (IsOrderVisible(order))
						return order;
				}

			return null;
		}

		protected virtual void Prepare()
		{
			Expression expression = _elaboratedExpression.Expression;
			_tableVar = _elaboratedExpression.MainElaboratedTableVar.TableVar;
			Schema.Order order = GetDefaultOrder(_tableVar);
			if (order != null)
			{
				BaseOrderExpression browseExpression;
				if (Convert.ToBoolean(DerivationUtility.GetTag(_tableVar.MetaData, "UseBrowse", _elaboratedExpression.PageType, "True")))
					browseExpression = new BrowseExpression();
				else
					browseExpression = new OrderExpression();
				browseExpression.Expression = expression;
				foreach (Schema.OrderColumn column in order.Columns)
				{
					OrderColumnDefinition definition = column.EmitStatement(EmitMode.ForCopy) as OrderColumnDefinition;
					definition.ColumnName = Schema.Object.Qualify(column.Column.Name, _elaboratedExpression.MainElaboratedTableVar.ElaboratedName);
					browseExpression.Columns.Add(definition);
				}
				expression = browseExpression;
			}

			_expression = 
				new D4TextEmitter().Emit
				(
					new CursorDefinition
					(
						expression, 
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
			_derivationInfo = new DerivationInfo();
			_derivationInfo.Program = _program;
			_derivationInfo.Process = _process;
			_derivationInfo.PageType = _seed.PageType;
			_derivationInfo.Query = _seed.Query;
			_derivationInfo.Elaborate = _seed.Elaborate;
			_derivationInfo.MasterKeyNames = _seed.MasterKeyNames;
			_derivationInfo.DetailKeyNames = _seed.DetailKeyNames;
			_derivationInfo.KeyNames = GetKeyNames(_tableVar);
			_derivationInfo.Expression = _expression;
			_derivationInfo.ElaboratedExpression = _elaboratedExpression;
			_derivationInfo.TableVar = _tableVar;
			_derivationInfo.MainSourceName = DerivationUtility.MainSourceName;
			_derivationInfo.IsReadOnly = DerivationUtility.IsReadOnlyPageType(_seed.PageType);
		}
		
		public virtual XmlDocument Structure()
		{
			switch (_seed.PageType)
			{
				case DerivationUtility.Browse :
					return 
						new BrowseDocumentBuilder
						(
							_derivationInfo, 
							(SearchElement)new SearchStructureBuilder(_derivationInfo).Build(), 
							(GridElement)new PluralStructureBuilder(_derivationInfo).Build()
						).Build();
						
				case DerivationUtility.List :
					return
						new ListDocumentBuilder
						(
							_derivationInfo,
							(SearchElement)new SearchStructureBuilder(_derivationInfo).Build(),
							(GridElement)new PluralStructureBuilder(_derivationInfo).Build()
						).Build();
						
				case DerivationUtility.BrowseReport :
					return new ReportDocumentBuilder(_derivationInfo).Build();
				
				case DerivationUtility.OrderBrowse :
					return new OrderBrowseDocumentBuilder(_derivationInfo).Build();
				
				case DerivationUtility.Add :
				case DerivationUtility.Edit :
				case DerivationUtility.View :
					return new SingularDocumentBuilder(_derivationInfo, new LayoutBuilder().Layout(((GroupElement)new SingularStructureBuilder(_derivationInfo).Build()).Elements)).Build();

				case DerivationUtility.Delete :
					return new DeleteDocumentBuilder(_derivationInfo, new LayoutBuilder().Layout(((GroupElement)new SingularStructureBuilder(_derivationInfo).Build()).Elements)).Build();
				
				default : throw new Frontend.Server.ServerException(Frontend.Server.ServerException.Codes.UnknownPageType, _seed.PageType);
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
