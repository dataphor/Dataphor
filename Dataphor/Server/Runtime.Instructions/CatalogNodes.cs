/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Device.Catalog;

	// operator System.ObjectName(const AName : System.Name) : System.Name
	// operator System.ObjectName(const ASpecifier : System.String) : System.Name
	public class FullObjectNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1).Name;
				else
					return program.ResolveCatalogIdentifier((string)argument1).Name;
		}
	}

	// operator System.ObjectID(System.Name) : System.Integer
	// operator System.ObjectID(System.String) : System.Integer
	public class SystemObjectIDNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1).ID;
				else
					return program.ResolveCatalogIdentifier((string)argument1).ID;
		}
	}
	
	// operator System.ObjectName(System.Integer) : System.String
	public class SystemObjectNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetObjectHeader((int)argument1).Name;
		}
	}
	
	// operator System.ObjectDescription(const AName : System.Name) : System.String
	// operator System.ObjectDescription(const ASpecifier : System.String) : System.String;
	// operator System.ObjectDescription(const AObjectID : System.Integer) : System.String;
	public class ObjectDescriptionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1).Description;
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
					return program.ResolveCatalogIdentifier((string)argument1).Description;
				else
					return program.CatalogDeviceSession.ResolveObject((int)argument1).Description;
		}
	}
	
	// operator System.ObjectDisplayName(const AName : System.Name) : System.String
	// operator System.ObjectDisplayName(const ASpecifier : System.String) : System.String
	// operator System.ObjectDisplayName(const AObjectID : System.Integer) : System.String
	public class ObjectDisplayNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1).DisplayName;
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
					return program.ResolveCatalogIdentifier((string)argument1).DisplayName;
				else
					return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetObjectHeader((int)argument1).DisplayName;
		}
	}
	
	// operator System.OperatorSignature(const AName : System.Name) : System.String
	// operator System.OperatorSignature(const ASpecifier : System.String) : System.String
	// operator System.OperatorSignature(const AObjectID : System.Integer) : System.String
	public class OperatorSignatureNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return ((Schema.Operator)program.ResolveCatalogObjectSpecifier((string)argument1)).Signature.ToString();
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
					return ((Schema.Operator)program.ResolveCatalogIdentifier((string)argument1)).Signature.ToString();
				else
					return ((Schema.Operator)program.CatalogDeviceSession.ResolveCatalogObject((int)argument1)).Signature.ToString();
		}
	}
	
	// overloads supported
	// operator System.ObjectMetaData(const AName : System.Name, const ATagName : System.String, ADefaultValue : System.String) : System.String
	// operator System.ObjectMetaData(const ASpecifier : System.String, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// operator System.ObjectMetaData(const AObjectID : System.Integer, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// similar to ObjectDescriptionNode
	public class ObjectMetaDataNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Object objectValue = null;

			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			else
			#endif
			{
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					objectValue = program.ResolveCatalogObjectSpecifier((string)arguments[0]);
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
					objectValue = program.ResolveCatalogIdentifier((string)arguments[0]);
				else
					objectValue = program.CatalogDeviceSession.ResolveObject((int)arguments[0]);
				return MetaData.GetTag(objectValue.MetaData, (string)arguments[1], (string)arguments[2]);
			}
		}
	}
	
	// operator System.IsSystem(const AName : System.Name) : System.Boolean
	// operator System.IsSystem(const ASpecifier : System.String) : System.Boolean
	// operator System.IsSystem(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsSystemNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1).IsSystem;
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
					return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetObjectHeader((int)argument1).IsSystem;
				else
					return program.ResolveCatalogIdentifier((string)argument1).IsSystem;
		}
	}
	
	// operator System.IsGenerated(const AName : System.Name) : System.Boolean
	// operator System.IsGenerated(const ASpecifier : String) : System.Boolean
	// operator System.IsGenerated(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsGeneratedNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
					return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetObjectHeader((int)argument1).IsGenerated;
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1).IsGenerated;
				else
					return program.ResolveCatalogIdentifier((string)argument1).IsGenerated;
		}
	}

	// operator System.LibraryName() : System.Name
	// operator System.LibraryName(const AName : System.Name) : System.Name
	// operator System.LibraryName(const AObjectID : System.Integer) : System.Name	
	public class SystemLibraryNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 1)
			{
				string libraryName;
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
					libraryName = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).GetObjectHeader((int)arguments[0]).LibraryName;
				else
				{
					Schema.Object objectValue = program.ResolveCatalogIdentifier((string)arguments[0], true);
					libraryName = objectValue.Library == null ? String.Empty : objectValue.Library.Name;
				}
				return libraryName;
			}
			else
				return program.CurrentLibrary.Name;
		}
	}
	
    /// <remarks> operator System.CatalogTimeStamp() : Long; </remarks>
    public class SystemCatalogTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(Program program)
		{
			return program.Catalog.TimeStamp;
		}
	}
    
    /// <remarks> operator System.CacheTimeStamp() : Long; </remarks>
    public class SystemCacheTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(Program program)
		{
			return program.Catalog.CacheTimeStamp;
		}
    }
    
    /// <remarks> operator System.DerivationTimeStamp() : Long; </remarks>
    public class SystemDerivationTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(Program program)
		{
			return program.Catalog.DerivationTimeStamp;
		}
    }
    
	// operator UpdateTimeStamps();
	public class SystemUpdateTimeStampsNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			// This call is no longer necessary because the catalog timestamp only controls library information now
			//AProgram.ServerProcess.ServerSession.Server.Catalog.UpdateTimeStamp(); 
			program.Catalog.UpdateCacheTimeStamp();
			program.Catalog.UpdatePlanCacheTimeStamp();
			program.Catalog.UpdateDerivationTimeStamp();
			return null;
		}
	}

    // operator Script(const AName : Name) : String
    // operator Script(const ASpecifier : String) : String
    // operator Script(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator Script(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			D4TextEmitter emitter = new D4TextEmitter();

			Schema.Object objectValue;		
			if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
				objectValue = program.ResolveCatalogIdentifier((string)arguments[0], true);
			else
				objectValue = program.ResolveCatalogObjectSpecifier((string)arguments[0], true);
				
			bool includeDependents = arguments.Length > 1 ? (bool)arguments[1] : false;
			bool includeObject = arguments.Length > 2 ? (bool)arguments[2] : true;
				
			return 
				emitter.Emit
				(
					program.Catalog.EmitStatement
					(
						program.CatalogDeviceSession,
						EmitMode.ForCopy, 
						new string[] { objectValue.Name }, 
						String.Empty, 
						true, 
						true, 
						includeDependents, 
						includeObject
					)
				);
		}
    }
    
    // operator ScriptExpression(const AExpression : String) : String
    public class SystemScriptExpressionNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			D4TextEmitter emitter = new D4TextEmitter();
		
			IServerExpressionPlan plan = ((IServerProcess)program.ServerProcess).PrepareExpression((string)arguments[0], null);
			try
			{
				plan.CheckCompiled();
				return emitter.Emit(plan.Catalog.EmitStatement(program.CatalogDeviceSession, EmitMode.ForCopy, new string[] { plan.TableVar.Name } ));
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareExpression(plan);
			}
		}
    }
    
    /// <remarks>operator ScriptData(AExpression : System.String) : System.String;</remarks>
    public class SystemScriptDataNode : InstructionNode
    {
		// TODO: Update this to work with non-scalar-valued attributes

		private bool IsParserLiteral(Program program, Schema.ScalarType type)
		{
			switch (type.Name)
			{
				case "System.Boolean" :
				case "System.Byte" :
				case "System.Short" :
				case "System.Integer" :
				case "System.Long" :
				case "System.Decimal" :
				case "System.Money" :
				case "System.String" :
				#if USEISTRING
				case "System.IString" : 
				#endif
					return true;
				default : return false;
			}
		}
		
		// returns true if all the properties of the representation are parser literals, or can be specified in terms of parser literals, recursively
		private bool IsRepresentationLiteral(Program program, Schema.Representation representation)
		{
			foreach (Schema.Property property in representation.Properties)
				if ((!(property.DataType is Schema.ScalarType)) || !IsParserLiteral(program, (Schema.ScalarType)property.DataType))
					return false;
			return true;
		}
		
		private Expression EmitScalarRepresentationSelector(Program program, Schema.Representation representation, object tempValue)
		{
			CallExpression selector = new CallExpression();
			selector.Identifier = representation.Selector.OperatorName;
			foreach (Schema.Property property in representation.Properties)
				selector.Expressions.Add
				(
					EmitScalarSelector
					(
						program, 
						Compiler.EmitCallNode
						(
							program.Plan, 
							property.ReadAccessor.OperatorName, 
							new PlanNode[] { new ValueNode(representation.ScalarType, tempValue) }
						).Execute(program), 
						(Schema.ScalarType)property.DataType
					)
				);
			return selector;
		}
		
		private Expression EmitScalarSelector(Program program, object tempValue, Schema.ScalarType dataType)
		{
			// if the value is a parser literal, emit the value expression for it,
			// search for a selector in terms of parser literals, recursively
			// if a parser literal can be converted to the value, emit the expression to convert it
			// Compile the template to use for each row so the search does not have to take place for each row.
			switch (dataType.Name)
			{
				case "System.Boolean" : return new ValueExpression((bool)tempValue);
				case "System.Byte" : return new ValueExpression((int)(byte)tempValue);
				case "System.Short" : return new ValueExpression((int)(short)tempValue);
				case "System.Integer" : return new ValueExpression((int)tempValue);
				case "System.Long" : return new ValueExpression((long)tempValue, TokenType.Integer);
				case "System.Decimal" : return new ValueExpression((decimal)tempValue);
				case "System.Money" : 
					if ((decimal)tempValue < 0)
						return new UnaryExpression(Alphora.Dataphor.DAE.Language.D4.Instructions.Negate, new ValueExpression(-((decimal)tempValue), TokenType.Money));
					else
						return new ValueExpression((decimal)tempValue, TokenType.Money);
				case "System.String" : return new ValueExpression((string)tempValue);
				#if USEISTRING
				case "System.IString" : return new ValueExpression((string)AValue, LexerToken.IString);
				#endif
				default :
				{
					foreach (Schema.Representation representation in dataType.Representations)
						if (IsRepresentationLiteral(program, representation))
							return EmitScalarRepresentationSelector(program, representation, tempValue);
					break;
				}
			}
			
			Error.Fail("Unable to construct a literal selector for values of type {0}.", dataType.Name);
			return null;
		}
		
		private RowSelectorExpressionBase EmitRowSelector(Program program, IRow row)
		{
			DAE.Language.D4.RowSelectorExpressionBase selector = row.DataType is Schema.RowType ? (RowSelectorExpressionBase)new RowSelectorExpression() : new EntrySelectorExpression();
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				if (row.HasValue(index))
					selector.Expressions.Add(new NamedColumnExpression(EmitScalarSelector(program, row[index], (Schema.ScalarType)row.DataType.Columns[index].DataType), row.DataType.Columns[index].Name));
				else
					selector.Expressions.Add(new NamedColumnExpression(new AsExpression(new ValueExpression(null, TokenType.Nil), row.DataType.Columns[index].DataType.EmitSpecifier(EmitMode.ForCopy)), row.DataType.Columns[index].Name));
			return selector;
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			D4TextEmitter emitter = new D4TextEmitter();
		
			string expression = (string)arguments[0];
			CursorNode node = Compiler.Compile(program.Plan, expression, true).ExtractNode<CursorNode>();
			TableSelectorExpressionBase selector = node.SourceNode.DataType is Schema.TableType ? (TableSelectorExpressionBase)new TableSelectorExpression() : new PresentationSelectorExpression();
			selector.TypeSpecifier = node.SourceNode.DataType.EmitSpecifier(EmitMode.ForCopy);
			selector.Keys.Add(program.FindClusteringKey(node.SourceNode.TableVar).EmitStatement(EmitMode.ForCopy));
			ITable table = (ITable)node.SourceNode.Execute(program);
			try
			{
				Row row = new Row(program.ValueManager, table.DataType.RowType);
				try
				{
					while (table.Next())
					{
						table.Select(row);
						
						selector.Expressions.Add(EmitRowSelector(program, row));
					}
					
					return emitter.Emit(selector);
				}
				finally
				{
					row.Dispose();
				}
			}
			finally
			{
				table.Dispose();
			}
		}
    }
    
    /// <remarks>operator ScriptLibrary(ALibraryName : System.Name) : System.String;</remarks>
    public class SystemScriptLibraryNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.ScriptLibrary(program.CatalogDeviceSession, (string)arguments[0]);
		}
    }
    
    /// <remarks>operator ScriptCatalog() : System.String;</remarks>
	public class SystemScriptCatalogNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.ScriptCatalog(program.CatalogDeviceSession);
		}
	}
	
	/// <remarks>operator ScriptServerState() : System.String;</remarks>
	public class SystemScriptServerStateNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return ((Server)program.ServerProcess.ServerSession.Server).ScriptServerState(program.ServerProcess);
		}
	}

    // operator ScriptDrop(const AName : Name) : String
    // operator ScriptDrop(const ASpecifier : String) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptDropNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			D4TextEmitter emitter = new D4TextEmitter();
			
			Schema.Object objectValue;
			
			if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
				objectValue = program.ResolveCatalogIdentifier((string)arguments[0], true);
			else
				objectValue = program.ResolveCatalogObjectSpecifier((string)arguments[0], true);
				
			bool includeDependents = arguments.Length > 1 ? (bool)arguments[1] : true;
			bool includeObject = arguments.Length > 2 ? (bool)arguments[2] : true;
		
			// Do not include generated objects in the drop script, they will be dropped automatically when the generating object is dropped
			return emitter.Emit(program.Catalog.EmitDropStatement(program.CatalogDeviceSession, new string[] { objectValue.Name }, String.Empty, false, false, includeDependents, includeObject));
		}
    }
    
    /// <remarks>operator ScriptDropLibrary(AName : System.Name) : System.String;</remarks>
    public class SystemScriptDropLibraryNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.ScriptDropLibrary(program.CatalogDeviceSession, (string)arguments[0]);
		}
    }
    
    /// <remarks>operator ScriptDropCatalog() : System.String;</remarks>
    public class SystemScriptDropCatalogNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.ScriptDropCatalog(program.CatalogDeviceSession);
		}
    }

	/// <remarks>operator ScriptLibraryChanges(const AOldCatalogDirectory : String, const ALibraryName : Name) : String;</remarks>
	public class SystemScriptLibraryChangesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return null;
			//return AProgram.ServerProcess.ServerSession.Server.ScriptLibraryChanges((string)AArguments[0], (string)AArguments[1]);
		}
	}

	// operator Diagnostics.IsCatalogObjectLoaded(const AObjectID : Integer) : Boolean;
	public class SystemIsCatalogObjectLoadedNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			else
			#endif
			{
				if (program.ServerProcess.ServerSession.User.ID != program.ServerProcess.ServerSession.Server.AdminUser.ID)
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.ServerProcess.ServerSession.User.ID);
					
				if (Nodes[0].DataType.Is(program.DataTypes.SystemInteger))
					return program.CatalogDeviceSession.ResolveCachedCatalogObject((int)arguments[0], false) != null;

				return program.CatalogDeviceSession.ResolveCachedCatalogObject((string)arguments[0], false) != null;
			}
		}
	}

	// operator ClearCatalogObject(const AObjectID : Integer);
	// operator ClearCatalogObject(const AObjectName : Name);
    public class SystemClearCatalogObjectNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (program.ServerProcess.ServerSession.User.ID != program.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.ServerProcess.ServerSession.User.ID);

			Schema.CatalogObject catalogObject;
			if (Nodes[0].DataType.Is(program.DataTypes.SystemInteger))
				catalogObject = program.CatalogDeviceSession.ResolveCachedCatalogObject((int)arguments[0]);
			else
				catalogObject = program.CatalogDeviceSession.ResolveCachedCatalogObject((string)arguments[0]);
				
			program.CatalogDeviceSession.ClearCachedCatalogObject(catalogObject);
				
			return null;
		}
    }
    
	// operator ClearLibrary(string ALibraryName);
    public class SystemClearLibraryNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (program.ServerProcess.ServerSession.User.ID != program.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.ServerProcess.ServerSession.User.ID);
			string libraryName = program.Catalog.Libraries[(string)arguments[0]].Name;
			Schema.Objects objects = new Schema.Objects();
			lock (program.Catalog)
			{
				for (int index = 0; index < program.Catalog.Count; index++)
					if ((program.Catalog[index].Library != null) && (program.Catalog[index].Library.Name == libraryName))
						objects.Add(program.Catalog[index]);
			}
			
			program.CatalogDeviceSession.ClearCachedCatalogObjects(objects);
				
			return null;
		}
    }
    
	// operator ClearCatalog();
    public class SystemClearCatalogNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			throw new NotSupportedException();
			// This hasn't worked for a long time anyway, so rather than try to expose SetUser safely, just commenting it out.
			//if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
			//    throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);
			//AProgram.ServerProcess.ServerSession.Server.ClearCatalog();
			//AProgram.ServerProcess.ServerSession.SetUser(AProgram.ServerProcess.ServerSession.Server.AdminUser);
			//AProgram.Plan.UpdateSecurityContexts(AProgram.ServerProcess.ServerSession.User);
			//return null;
		}
    }
    
    // operator DependentObjects(const AObjectID : Integer) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectID : Integer, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectName : Name) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectName : Name, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    public class SystemDependentObjectsNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Object_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Object_Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Object_Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order order = new Schema.Order();
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], false));
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					int iD;
					if (Nodes[0].DataType.Is(program.DataTypes.SystemName))
						iD = program.ResolveCatalogIdentifier((string)Nodes[0].Execute(program), true).ID;
					else
						iD = (int)Nodes[0].Execute(program);
						
					List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(iD, Nodes.Count == 2 ? (bool)Nodes[1].Execute(program) : true);
					
					for (int index = 0; index < headers.Count; index++)
					{
						row[0] = headers[index].ID;
						row[1] = headers[index].Name;
						row[2] = headers[index].Description;
						row[3] = headers[index].Sequence;
						row[4] = headers[index].Level;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }
    
    // operator RequiredObjects(const AObjectID : Integer) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator RequiredObjects(const AObjectID : Integer, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator RequiredObjects(const AObjectName : Name) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator RequiredObjects(const AObjectName : Name, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    public class SystemRequiredObjectsNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Object_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Object_Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Object_Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order order = new Schema.Order();
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], true));
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredObject(Program program, Table table, Row row, Schema.Object objectValue, bool recursive, ref int sequence, int level)
		{
			row[0] = objectValue.ID;
			row[1] = objectValue.Name;
			row[2] = objectValue.Description;
			row[3] = sequence;
			row[4] = level;
			table.Insert(row);
			if (recursive)
			{
				level += 1;
				PopulateRequiredObjects(program, table, row, objectValue, recursive, ref sequence, level);
			}
		}
		
		private void PopulateRequiredObjects(Program program, Table table, Row row, Schema.Object objectValue, bool recursive, ref int sequence, int level)
		{
			if (objectValue.HasDependencies())
				for (int index = 0; index < objectValue.Dependencies.Count; index++)
				{
					sequence += 1;
					PopulateRequiredObject(program, table, row, objectValue.Dependencies.ResolveObject(program.CatalogDeviceSession, index), recursive, ref sequence, level);
				}
		}

		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					
					int iD;
					if (Nodes[0].DataType.Is(program.DataTypes.SystemName))
						iD = program.ResolveCatalogIdentifier((string)Nodes[0].Execute(program), true).ID;
					else
						iD = (int)Nodes[0].Execute(program);
						
					List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependencies(iD, Nodes.Count == 2 ? (bool)Nodes[1].Execute(program) : true);
					
					for (int index = 0; index < headers.Count; index++)
					{
						row[0] = headers[index].ID;
						row[1] = headers[index].Name;
						row[2] = headers[index].Description;
						row[3] = headers[index].Sequence;
						row[4] = headers[index].Level;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }
    
    // operator DependentLibraries(const ALibraryName : Name) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    // operator DependentLibraries(const ALibraryName : Name, const ARecursive : Boolean) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    public class SystemDependentLibrariesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order order = new Schema.Order();
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], false));
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateLibrary(Program program, Table table, Row row, Schema.LoadedLibrary library, bool recursive, ref int sequence, int level)
		{
			row[0] = library.Name;
			row[1] = sequence;
			row[2] = level;
			table.Insert(row);
			if (recursive)
			{
				level += 1;
				PopulateDependentLibraries(program, table, row, library, recursive, ref sequence, level);
			}
		}
		
		private void PopulateDependentLibraries(Program program, Table table, Row row, Schema.LoadedLibrary library, bool recursive, ref int sequence, int level)
		{
			foreach (Schema.LoadedLibrary localLibrary in library.RequiredByLibraries)
			{
				sequence += 1;
				PopulateLibrary(program, table, row, localLibrary, recursive, ref sequence, level);
			}
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					int sequence = 0;
					PopulateDependentLibraries
					(
						program, 
						result, 
						row,
						program.CatalogDeviceSession.ResolveLoadedLibrary((string)Nodes[0].Execute(program)), 
						Nodes.Count == 2 ? (bool)Nodes[1].Execute(program) : true, 
						ref sequence, 
						1
					);
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }

    // operator RequiredLibraries(const ALibraryName : Name) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    // operator RequiredLibraries(const ALibraryName : Name, const ARecursive : Boolean) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    public class SystemRequiredLibrariesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order order = new Schema.Order();
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], true));
			order.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredLibrary(Program program, Table table, Row row, Schema.Library library, bool recursive, ref int sequence, int level)
		{
			row[0] = library.Name;
			row[1] = sequence;
			row[2] = level;
			table.Insert(row);
			if (recursive)
			{
				level += 1;
				PopulateRequiredLibraries(program, table, row, library, recursive, ref sequence, level);
			}
		}
		
		private void PopulateRequiredLibraries(Program program, Table table, Row row, Schema.Library library, bool recursive, ref int sequence, int level)
		{
			foreach (Schema.LibraryReference localLibrary in library.Libraries)
			{
				sequence += 1;
				PopulateRequiredLibrary(program, table, row, program.Catalog.Libraries[localLibrary.Name], recursive, ref sequence, level);
			}
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					int sequence = 0;
					PopulateRequiredLibraries
					(
						program, 
						result, 
						row, 
						program.Catalog.Libraries[(string)Nodes[0].Execute(program)],
						Nodes.Count == 2 ? (bool)Nodes[1].Execute(program) : true, 
						ref sequence, 
						1
					);
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }
    
    public class SystemClearStoreCountersNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).ClearStoreCounters();
			return null;
		}
    }
    
    // operator GetStoreCounters() : table { Sequence : Integer, Operation : String, TableName : String, IndexName : String, IsMatched : Boolean, IsRanged : Boolean, Duration : TimeSpan };
    public class SystemGetStoreCountersNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Operation", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TableName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IndexName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsMatched", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRanged", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsUpdatable", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Duration", plan.DataTypes.SystemTimeSpan));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();
				
				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					((ServerCatalogDeviceSession)program.CatalogDeviceSession).PopulateStoreCounters(result, row);
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }
}
