/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	/// <remarks> Default </remarks>
	public abstract class Default : Object
    {
		// constructor
		public Default(int iD, string name) : base(iD, name) {}

		public override bool IsPersistent { get { return true; } }

		// Expression
		private PlanNode _node;
		public PlanNode Node
		{
			get { return _node; }
			set { _node = value; }
		}
		
		public DefaultDefinition EmitDefinition(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				DefaultDefinition statement = new DefaultDefinition();
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				statement.Expression = (Expression)Node.EmitStatement(mode);
				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
    }
    
    public class ScalarTypeDefault : Default
    {
		public ScalarTypeDefault(int iD, string name) : base(iD, name) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.ScalarTypeDefault"), _scalarType.DisplayName); } }

		public override int CatalogObjectID { get { return _scalarType == null ? -1 : _scalarType.ID; } }

		public override int ParentObjectID { get { return _scalarType == null ? -1 : _scalarType.ID; } }

		[Reference]
		internal ScalarType _scalarType;
		public ScalarType ScalarType 
		{ 
			get { return _scalarType; } 
			set
			{
				if (_scalarType != null)
					_scalarType.Default = null;
				if (value != null)
					value.Default = this;	
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			AlterScalarTypeStatement statement = new AlterScalarTypeStatement();
			statement.ScalarTypeName = Schema.Object.EnsureRooted(_scalarType.Name);
			statement.Default = EmitDefinition(mode);
			return statement;
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			AlterScalarTypeStatement statement = new AlterScalarTypeStatement();
			statement.ScalarTypeName = Schema.Object.EnsureRooted(_scalarType.Name);
			statement.Default = new D4.DropDefaultDefinition();
			return statement;
		}
    }
    
    public class TableVarColumnDefault : Default
    {
		public TableVarColumnDefault(int iD, string name) : base(iD, name) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumnDefault"), _tableVarColumn.DisplayName, _tableVarColumn.TableVar.DisplayName); } }

		public override int CatalogObjectID { get { return _tableVarColumn == null ? -1 : _tableVarColumn.CatalogObjectID; } }

		public override int ParentObjectID { get { return _tableVarColumn == null ? -1 : _tableVarColumn.ID; } }
		
		public override bool IsATObject { get { return _tableVarColumn == null ? false : _tableVarColumn.IsATObject; } }

		[Reference]
		internal TableVarColumn _tableVarColumn;
		public TableVarColumn TableVarColumn
		{
			get { return _tableVarColumn; }
			set
			{
				if (_tableVarColumn != null)
					_tableVarColumn.Default = null;
				if (value != null)
					value.Default = this;
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			AlterTableStatement statement = new AlterTableStatement();
			statement.TableVarName = Schema.Object.EnsureRooted(_tableVarColumn.TableVar.Name);
			AlterColumnDefinition definition = new AlterColumnDefinition();
			definition.ColumnName = _tableVarColumn.Name;
			definition.Default = EmitDefinition(mode);
			statement.AlterColumns.Add(definition);
			return statement;
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			if (_tableVarColumn.TableVar is BaseTableVar)
			{
				AlterTableStatement statement = new AlterTableStatement();
				statement.TableVarName = Schema.Object.EnsureRooted(_tableVarColumn.TableVar.Name);
				AlterColumnDefinition definition = new D4.AlterColumnDefinition();
				definition.ColumnName = _tableVarColumn.Name;
				definition.Default = new DropDefaultDefinition();
				statement.AlterColumns.Add(definition);
				return statement;
			}
			else
				return new Block();
		}
    }
}