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
		public Default(int AID, string AName) : base(AID, AName) {}

		public override bool IsPersistent { get { return true; } }

		// Expression
		private PlanNode FNode;
		public PlanNode Node
		{
			get { return FNode; }
			set { FNode = value; }
		}
		
		public DefaultDefinition EmitDefinition(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			DefaultDefinition LStatement = new DefaultDefinition();
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			LStatement.Expression = (Expression)Node.EmitStatement(AMode);
			return LStatement;
		}
    }
    
    public class ScalarTypeDefault : Default
    {
		public ScalarTypeDefault(int AID, string AName) : base(AID, AName) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.ScalarTypeDefault"), FScalarType.DisplayName); } }

		public override int CatalogObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

		public override int ParentObjectID { get { return FScalarType == null ? -1 : FScalarType.ID; } }

		[Reference]
		internal ScalarType FScalarType;
		public ScalarType ScalarType 
		{ 
			get { return FScalarType; } 
			set
			{
				if (FScalarType != null)
					FScalarType.Default = null;
				if (value != null)
					value.Default = this;	
			}
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			AlterScalarTypeStatement LStatement = new AlterScalarTypeStatement();
			LStatement.ScalarTypeName = Schema.Object.EnsureRooted(FScalarType.Name);
			LStatement.Default = EmitDefinition(AMode);
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			AlterScalarTypeStatement LStatement = new AlterScalarTypeStatement();
			LStatement.ScalarTypeName = Schema.Object.EnsureRooted(FScalarType.Name);
			LStatement.Default = new D4.DropDefaultDefinition();
			return LStatement;
		}
    }
    
    public class TableVarColumnDefault : Default
    {
		public TableVarColumnDefault(int AID, string AName) : base(AID, AName) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumnDefault"), FTableVarColumn.DisplayName, FTableVarColumn.TableVar.DisplayName); } }

		public override int CatalogObjectID { get { return FTableVarColumn == null ? -1 : FTableVarColumn.CatalogObjectID; } }

		public override int ParentObjectID { get { return FTableVarColumn == null ? -1 : FTableVarColumn.ID; } }
		
		public override bool IsATObject { get { return FTableVarColumn == null ? false : FTableVarColumn.IsATObject; } }

		[Reference]
		internal TableVarColumn FTableVarColumn;
		public TableVarColumn TableVarColumn
		{
			get { return FTableVarColumn; }
			set
			{
				if (FTableVarColumn != null)
					FTableVarColumn.Default = null;
				if (value != null)
					value.Default = this;
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AlterTableStatement LStatement = new AlterTableStatement();
			LStatement.TableVarName = Schema.Object.EnsureRooted(FTableVarColumn.TableVar.Name);
			AlterColumnDefinition LDefinition = new AlterColumnDefinition();
			LDefinition.ColumnName = FTableVarColumn.Name;
			LDefinition.Default = EmitDefinition(AMode);
			LStatement.AlterColumns.Add(LDefinition);
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			if (FTableVarColumn.TableVar is BaseTableVar)
			{
				AlterTableStatement LStatement = new AlterTableStatement();
				LStatement.TableVarName = Schema.Object.EnsureRooted(FTableVarColumn.TableVar.Name);
				AlterColumnDefinition LDefinition = new D4.AlterColumnDefinition();
				LDefinition.ColumnName = FTableVarColumn.Name;
				LDefinition.Default = new DropDefaultDefinition();
				LStatement.AlterColumns.Add(LDefinition);
				return LStatement;
			}
			else
				return new Block();
		}
    }
}