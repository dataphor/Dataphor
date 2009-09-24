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
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1).Name;
				else
					return AProgram.ResolveCatalogIdentifier((string)AArgument1).Name;
		}
	}

	// operator ObjectExists(const AName : Name) : Boolean
	// operator ObjectExists(const ASpecifier : String) : Boolean
	public class ObjectExistsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1, false) != null;
				else
					return AProgram.ResolveCatalogIdentifier((string)AArgument1, false) != null;
		}
	}
	
	// operator System.ObjectID(System.Name) : System.Integer
	// operator System.ObjectID(System.String) : System.Integer
	public class SystemObjectIDNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1).ID;
				else
					return AProgram.ResolveCatalogIdentifier((string)AArgument1).ID;
		}
	}
	
	// operator System.ObjectName(System.Integer) : System.String
	public class SystemObjectNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetObjectHeader((int)AArgument1).Name;
		}
	}
	
	// operator System.ObjectDescription(const AName : System.Name) : System.String
	// operator System.ObjectDescription(const ASpecifier : System.String) : System.String;
	// operator System.ObjectDescription(const AObjectID : System.Integer) : System.String;
	public class ObjectDescriptionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1).Description;
				else if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemName))
					return AProgram.ResolveCatalogIdentifier((string)AArgument1).Description;
				else
					return AProgram.CatalogDeviceSession.ResolveObject((int)AArgument1).Description;
		}
	}
	
	// operator System.ObjectDisplayName(const AName : System.Name) : System.String
	// operator System.ObjectDisplayName(const ASpecifier : System.String) : System.String
	// operator System.ObjectDisplayName(const AObjectID : System.Integer) : System.String
	public class ObjectDisplayNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1).DisplayName;
				else if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemName))
					return AProgram.ResolveCatalogIdentifier((string)AArgument1).DisplayName;
				else
					return ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetObjectHeader((int)AArgument1).DisplayName;
		}
	}
	
	// operator System.OperatorSignature(const AName : System.Name) : System.String
	// operator System.OperatorSignature(const ASpecifier : System.String) : System.String
	// operator System.OperatorSignature(const AObjectID : System.Integer) : System.String
	public class OperatorSignatureNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return ((Schema.Operator)AProgram.ResolveCatalogObjectSpecifier((string)AArgument1)).Signature.ToString();
				else if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemName))
					return ((Schema.Operator)AProgram.ResolveCatalogIdentifier((string)AArgument1)).Signature.ToString();
				else
					return ((Schema.Operator)AProgram.CatalogDeviceSession.ResolveCatalogObject((int)AArgument1)).Signature.ToString();
		}
	}
	
	// overloads supported
	// operator System.ObjectMetaData(const AName : System.Name, const ATagName : System.String, ADefaultValue : System.String) : System.String
	// operator System.ObjectMetaData(const ASpecifier : System.String, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// operator System.ObjectMetaData(const AObjectID : System.Integer, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// similar to ObjectDescriptionNode
	public class ObjectMetaDataNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Schema.Object LObject = null;

			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			else
			#endif
			{
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					LObject = AProgram.ResolveCatalogObjectSpecifier((string)AArguments[0]);
				else if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemName))
					LObject = AProgram.ResolveCatalogIdentifier((string)AArguments[0]);
				else
					LObject = AProgram.CatalogDeviceSession.ResolveObject((int)AArguments[0]);
				return MetaData.GetTag(LObject.MetaData, (string)AArguments[1], (string)AArguments[2]);
			}
		}
	}
	
	// operator System.IsSystem(const AName : System.Name) : System.Boolean
	// operator System.IsSystem(const ASpecifier : System.String) : System.Boolean
	// operator System.IsSystem(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsSystemNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1).IsSystem;
				else if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
					return ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetObjectHeader((int)AArgument1).IsSystem;
				else
					return AProgram.ResolveCatalogIdentifier((string)AArgument1).IsSystem;
		}
	}
	
	// operator System.IsGenerated(const AName : System.Name) : System.Boolean
	// operator System.IsGenerated(const ASpecifier : String) : System.Boolean
	// operator System.IsGenerated(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsGeneratedNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
					return ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetObjectHeader((int)AArgument1).IsGenerated;
				else if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
					return AProgram.ResolveCatalogObjectSpecifier((string)AArgument1).IsGenerated;
				else
					return AProgram.ResolveCatalogIdentifier((string)AArgument1).IsGenerated;
		}
	}

	// operator System.LibraryName() : System.Name
	// operator System.LibraryName(const AName : System.Name) : System.Name
	// operator System.LibraryName(const AObjectID : System.Integer) : System.Name	
	public class SystemLibraryNameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				string LLibraryName;
				if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
					LLibraryName = ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).GetObjectHeader((int)AArguments[0]).LibraryName;
				else
				{
					Schema.Object LObject = AProgram.ResolveCatalogIdentifier((string)AArguments[0], true);
					LLibraryName = LObject.Library == null ? String.Empty : LObject.Library.Name;
				}
				return LLibraryName;
			}
			else
				return AProgram.CurrentLibrary.Name;
		}
	}
	
    /// <remarks> operator System.CatalogTimeStamp() : Long; </remarks>
    public class SystemCatalogTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(Program AProgram)
		{
			return AProgram.Catalog.TimeStamp;
		}
	}
    
    /// <remarks> operator System.CacheTimeStamp() : Long; </remarks>
    public class SystemCacheTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(Program AProgram)
		{
			return AProgram.Catalog.CacheTimeStamp;
		}
    }
    
    /// <remarks> operator System.DerivationTimeStamp() : Long; </remarks>
    public class SystemDerivationTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(Program AProgram)
		{
			return AProgram.Catalog.DerivationTimeStamp;
		}
    }
    
	// operator UpdateTimeStamps();
	public class SystemUpdateTimeStampsNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
			// This call is no longer necessary because the catalog timestamp only controls library information now
			//AProgram.ServerProcess.ServerSession.Server.Catalog.UpdateTimeStamp(); 
			AProgram.Catalog.UpdateCacheTimeStamp();
			AProgram.Catalog.UpdatePlanCacheTimeStamp();
			AProgram.Catalog.UpdateDerivationTimeStamp();
			return null;
		}
	}

    // operator Script(const AName : Name) : String
    // operator Script(const ASpecifier : String) : String
    // operator Script(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator Script(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();

			Schema.Object LObject;		
			if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemName))
				LObject = AProgram.ResolveCatalogIdentifier((string)AArguments[0], true);
			else
				LObject = AProgram.ResolveCatalogObjectSpecifier((string)AArguments[0], true);
				
			bool LIncludeDependents = AArguments.Length > 1 ? (bool)AArguments[1] : false;
			bool LIncludeObject = AArguments.Length > 2 ? (bool)AArguments[2] : true;
				
			return 
				LEmitter.Emit
				(
					AProgram.Catalog.EmitStatement
					(
						AProgram.CatalogDeviceSession,
						EmitMode.ForCopy, 
						new string[] { LObject.Name }, 
						String.Empty, 
						true, 
						true, 
						LIncludeDependents, 
						LIncludeObject
					)
				);
		}
    }
    
    // operator ScriptExpression(const AExpression : String) : String
    public class SystemScriptExpressionNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
		
			IServerExpressionPlan LPlan = ((IServerProcess)AProgram.ServerProcess).PrepareExpression((string)AArguments[0], null);
			try
			{
				LPlan.CheckCompiled();
				return LEmitter.Emit(LPlan.Catalog.EmitStatement(AProgram.CatalogDeviceSession, EmitMode.ForCopy, new string[] { LPlan.TableVar.Name } ));
			}
			finally
			{
				((IServerProcess)AProgram.ServerProcess).UnprepareExpression(LPlan);
			}
		}
    }
    
    /// <remarks>operator ScriptData(AExpression : System.String) : System.String;</remarks>
    public class SystemScriptDataNode : InstructionNode
    {
		// TODO: Update this to work with non-scalar-valued attributes

		private bool IsParserLiteral(Program AProgram, Schema.ScalarType AType)
		{
			switch (AType.Name)
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
		private bool IsRepresentationLiteral(Program AProgram, Schema.Representation ARepresentation)
		{
			foreach (Schema.Property LProperty in ARepresentation.Properties)
				if ((!(LProperty.DataType is Schema.ScalarType)) || !IsParserLiteral(AProgram, (Schema.ScalarType)LProperty.DataType))
					return false;
			return true;
		}
		
		private Expression EmitScalarRepresentationSelector(Program AProgram, Schema.Representation ARepresentation, object AValue)
		{
			CallExpression LSelector = new CallExpression();
			LSelector.Identifier = ARepresentation.Selector.OperatorName;
			foreach (Schema.Property LProperty in ARepresentation.Properties)
				LSelector.Expressions.Add
				(
					EmitScalarSelector
					(
						AProgram, 
						Compiler.EmitCallNode
						(
							AProgram.Plan, 
							LProperty.ReadAccessor.OperatorName, 
							new PlanNode[] { new ValueNode(ARepresentation.ScalarType, AValue) }
						).Execute(AProgram), 
						(Schema.ScalarType)LProperty.DataType
					)
				);
			return LSelector;
		}
		
		private Expression EmitScalarSelector(Program AProgram, object AValue, Schema.ScalarType ADataType)
		{
			// if the value is a parser literal, emit the value expression for it,
			// search for a selector in terms of parser literals, recursively
			// if a parser literal can be converted to the value, emit the expression to convert it
			// Compile the template to use for each row so the search does not have to take place for each row.
			switch (ADataType.Name)
			{
				case "System.Boolean" : return new ValueExpression((bool)AValue);
				case "System.Byte" : return new ValueExpression((int)(byte)AValue);
				case "System.Short" : return new ValueExpression((int)(short)AValue);
				case "System.Integer" : return new ValueExpression((int)AValue);
				case "System.Long" : return new ValueExpression((long)AValue, TokenType.Integer);
				case "System.Decimal" : return new ValueExpression((decimal)AValue);
				case "System.Money" : 
					if ((decimal)AValue < 0)
						return new UnaryExpression(Alphora.Dataphor.DAE.Language.D4.Instructions.Negate, new ValueExpression(-((decimal)AValue), TokenType.Money));
					else
						return new ValueExpression((decimal)AValue, TokenType.Money);
				case "System.String" : return new ValueExpression((string)AValue);
				#if USEISTRING
				case "System.IString" : return new ValueExpression((string)AValue, LexerToken.IString);
				#endif
				default :
				{
					foreach (Schema.Representation LRepresentation in ADataType.Representations)
						if (IsRepresentationLiteral(AProgram, LRepresentation))
							return EmitScalarRepresentationSelector(AProgram, LRepresentation, AValue);
					break;
				}
			}
			
			Error.Fail("Unable to construct a literal selector for values of type {0}.", ADataType.Name);
			return null;
		}
		
		private RowSelectorExpressionBase EmitRowSelector(Program AProgram, Row ARow)
		{
			DAE.Language.D4.RowSelectorExpressionBase LSelector = ARow.DataType is Schema.RowType ? (RowSelectorExpressionBase)new RowSelectorExpression() : new EntrySelectorExpression();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				if (ARow.HasValue(LIndex))
					LSelector.Expressions.Add(new NamedColumnExpression(EmitScalarSelector(AProgram, ARow[LIndex], (Schema.ScalarType)ARow.DataType.Columns[LIndex].DataType), ARow.DataType.Columns[LIndex].Name));
				else
					LSelector.Expressions.Add(new NamedColumnExpression(new AsExpression(new ValueExpression(null, TokenType.Nil), ARow.DataType.Columns[LIndex].DataType.EmitSpecifier(EmitMode.ForCopy)), ARow.DataType.Columns[LIndex].Name));
			return LSelector;
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
		
			string LExpression = (string)AArguments[0];
			CursorNode LNode = (CursorNode)Compiler.BindNode(AProgram.Plan, Compiler.OptimizeNode(AProgram.Plan, Compiler.CompileCursor(AProgram.Plan, new Parser().ParseCursorDefinition(LExpression))));
			TableSelectorExpressionBase LSelector = LNode.SourceNode.DataType is Schema.TableType ? (TableSelectorExpressionBase)new TableSelectorExpression() : new PresentationSelectorExpression();
			LSelector.TypeSpecifier = LNode.SourceNode.DataType.EmitSpecifier(EmitMode.ForCopy);
			LSelector.Keys.Add(AProgram.FindClusteringKey(LNode.SourceNode.TableVar).EmitStatement(EmitMode.ForCopy));
			Table LTable = (Table)LNode.SourceNode.Execute(AProgram);
			try
			{
				Row LRow = new Row(AProgram.ValueManager, LTable.DataType.RowType);
				try
				{
					while (LTable.Next())
					{
						LTable.Select(LRow);
						
						LSelector.Expressions.Add(EmitRowSelector(AProgram, LRow));
					}
					
					return LEmitter.Emit(LSelector);
				}
				finally
				{
					LRow.Dispose();
				}
			}
			finally
			{
				LTable.Dispose();
			}
		}
    }
    
    /// <remarks>operator ScriptLibrary(ALibraryName : System.Name) : System.String;</remarks>
    public class SystemScriptLibraryNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.ScriptLibrary(AProgram.CatalogDeviceSession, (string)AArguments[0]);
		}
    }
    
    /// <remarks>operator ScriptCatalog() : System.String;</remarks>
	public class SystemScriptCatalogNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.ScriptCatalog(AProgram.CatalogDeviceSession);
		}
	}
	
	/// <remarks>operator ScriptServerState() : System.String;</remarks>
	public class SystemScriptServerStateNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return ((Server)AProgram.ServerProcess.ServerSession.Server).ScriptServerState(AProgram.ServerProcess);
		}
	}

    // operator ScriptDrop(const AName : Name) : String
    // operator ScriptDrop(const ASpecifier : String) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptDropNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			
			Schema.Object LObject;
			
			if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemName))
				LObject = AProgram.ResolveCatalogIdentifier((string)AArguments[0], true);
			else
				LObject = AProgram.ResolveCatalogObjectSpecifier((string)AArguments[0], true);
				
			bool LIncludeDependents = AArguments.Length > 1 ? (bool)AArguments[1] : true;
			bool LIncludeObject = AArguments.Length > 2 ? (bool)AArguments[2] : true;
		
			return LEmitter.Emit(AProgram.Catalog.EmitDropStatement(AProgram.CatalogDeviceSession, new string[] { LObject.Name }, String.Empty, true, true, LIncludeDependents, LIncludeObject));
		}
    }
    
    /// <remarks>operator ScriptDropLibrary(AName : System.Name) : System.String;</remarks>
    public class SystemScriptDropLibraryNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.ScriptDropLibrary(AProgram.CatalogDeviceSession, (string)AArguments[0]);
		}
    }
    
    /// <remarks>operator ScriptDropCatalog() : System.String;</remarks>
    public class SystemScriptDropCatalogNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.ScriptDropCatalog(AProgram.CatalogDeviceSession);
		}
    }

	/// <remarks>operator ScriptLibraryChanges(const AOldCatalogDirectory : String, const ALibraryName : Name) : String;</remarks>
	public class SystemScriptLibraryChangesNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return null;
			//return AProgram.ServerProcess.ServerSession.Server.ScriptLibraryChanges((string)AArguments[0], (string)AArguments[1]);
		}
	}

	// operator Diagnostics.IsCatalogObjectLoaded(const AObjectID : Integer) : Boolean;
	public class SystemIsCatalogObjectLoadedNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			else
			#endif
			{
				if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);
					
				if (Nodes[0].DataType.Is(AProgram.DataTypes.SystemInteger))
					return AProgram.CatalogDeviceSession.ResolveCachedCatalogObject((int)AArguments[0], false) != null;

				return AProgram.CatalogDeviceSession.ResolveCachedCatalogObject((string)AArguments[0], false) != null;
			}
		}
	}

	// operator ClearCatalogObject(const AObjectID : Integer);
	// operator ClearCatalogObject(const AObjectName : Name);
    public class SystemClearCatalogObjectNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);

			Schema.CatalogObject LCatalogObject;
			if (Nodes[0].DataType.Is(AProgram.DataTypes.SystemInteger))
				LCatalogObject = AProgram.CatalogDeviceSession.ResolveCachedCatalogObject((int)AArguments[0]);
			else
				LCatalogObject = AProgram.CatalogDeviceSession.ResolveCachedCatalogObject((string)AArguments[0]);
				
			AProgram.CatalogDeviceSession.ClearCachedCatalogObject(LCatalogObject);
				
			return null;
		}
    }
    
	// operator ClearLibrary(string ALibraryName);
    public class SystemClearLibraryNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);
			string LLibraryName = AProgram.Catalog.Libraries[(string)AArguments[0]].Name;
			Schema.Objects LObjects = new Schema.Objects();
			lock (AProgram.Catalog)
			{
				for (int LIndex = 0; LIndex < AProgram.Catalog.Count; LIndex++)
					if ((AProgram.Catalog[LIndex].Library != null) && (AProgram.Catalog[LIndex].Library.Name == LLibraryName))
						LObjects.Add(AProgram.Catalog[LIndex]);
			}
			
			AProgram.CatalogDeviceSession.ClearCachedCatalogObjects(LObjects);
				
			return null;
		}
    }
    
	// operator ClearCatalog();
    public class SystemClearCatalogNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);
			AProgram.ServerProcess.ServerSession.Server.ClearCatalog();
			AProgram.ServerProcess.ServerSession.SetUser(AProgram.ServerProcess.ServerSession.Server.AdminUser);
			AProgram.Plan.UpdateSecurityContexts(AProgram.ServerProcess.ServerSession.User);
			return null;
		}
    }
    
    // operator DependentObjects(const AObjectID : Integer) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectID : Integer, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectName : Name) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectName : Name, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    public class SystemDependentObjectsNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Object_ID", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Object_Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Object_Description", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], false));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					int LID;
					if (Nodes[0].DataType.Is(AProgram.DataTypes.SystemName))
						LID = AProgram.ResolveCatalogIdentifier((string)Nodes[0].Execute(AProgram), true).ID;
					else
						LID = (int)Nodes[0].Execute(AProgram);
						
					List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(LID, Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProgram) : true);
					
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						LRow[0] = LHeaders[LIndex].ID;
						LRow[1] = LHeaders[LIndex].Name;
						LRow[2] = LHeaders[LIndex].Description;
						LRow[3] = LHeaders[LIndex].Sequence;
						LRow[4] = LHeaders[LIndex].Level;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
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
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Object_ID", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Object_Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Object_Description", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], true));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredObject(Program AProgram, Table ATable, Row ARow, Schema.Object AObject, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0] = AObject.ID;
			ARow[1] = AObject.Name;
			ARow[2] = AObject.Description;
			ARow[3] = ASequence;
			ARow[4] = ALevel;
			ATable.Insert(ARow);
			if (ARecursive)
			{
				ALevel += 1;
				PopulateRequiredObjects(AProgram, ATable, ARow, AObject, ARecursive, ref ASequence, ALevel);
			}
		}
		
		private void PopulateRequiredObjects(Program AProgram, Table ATable, Row ARow, Schema.Object AObject, bool ARecursive, ref int ASequence, int ALevel)
		{
			if (AObject.HasDependencies())
				for (int LIndex = 0; LIndex < AObject.Dependencies.Count; LIndex++)
				{
					ASequence += 1;
					PopulateRequiredObject(AProgram, ATable, ARow, AObject.Dependencies.ResolveObject(AProgram.CatalogDeviceSession, LIndex), ARecursive, ref ASequence, ALevel);
				}
		}

		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					
					int LID;
					if (Nodes[0].DataType.Is(AProgram.DataTypes.SystemName))
						LID = AProgram.ResolveCatalogIdentifier((string)Nodes[0].Execute(AProgram), true).ID;
					else
						LID = (int)Nodes[0].Execute(AProgram);
						
					List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependencies(LID, Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProgram) : true);
					
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						LRow[0] = LHeaders[LIndex].ID;
						LRow[1] = LHeaders[LIndex].Name;
						LRow[2] = LHeaders[LIndex].Description;
						LRow[3] = LHeaders[LIndex].Sequence;
						LRow[4] = LHeaders[LIndex].Level;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    // operator DependentLibraries(const ALibraryName : Name) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    // operator DependentLibraries(const ALibraryName : Name, const ARecursive : Boolean) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    public class SystemDependentLibrariesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], false));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateLibrary(Program AProgram, Table ATable, Row ARow, Schema.LoadedLibrary ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0] = ALibrary.Name;
			ARow[1] = ASequence;
			ARow[2] = ALevel;
			ATable.Insert(ARow);
			if (ARecursive)
			{
				ALevel += 1;
				PopulateDependentLibraries(AProgram, ATable, ARow, ALibrary, ARecursive, ref ASequence, ALevel);
			}
		}
		
		private void PopulateDependentLibraries(Program AProgram, Table ATable, Row ARow, Schema.LoadedLibrary ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			foreach (Schema.LoadedLibrary LLibrary in ALibrary.RequiredByLibraries)
			{
				ASequence += 1;
				PopulateLibrary(AProgram, ATable, ARow, LLibrary, ARecursive, ref ASequence, ALevel);
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					int LSequence = 0;
					PopulateDependentLibraries
					(
						AProgram, 
						LResult, 
						LRow,
						AProgram.CatalogDeviceSession.ResolveLoadedLibrary((string)Nodes[0].Execute(AProgram)), 
						Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProgram) : true, 
						ref LSequence, 
						1
					);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }

    // operator RequiredLibraries(const ALibraryName : Name) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    // operator RequiredLibraries(const ALibraryName : Name, const ARecursive : Boolean) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    public class SystemRequiredLibrariesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], true));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredLibrary(Program AProgram, Table ATable, Row ARow, Schema.Library ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0] = ALibrary.Name;
			ARow[1] = ASequence;
			ARow[2] = ALevel;
			ATable.Insert(ARow);
			if (ARecursive)
			{
				ALevel += 1;
				PopulateRequiredLibraries(AProgram, ATable, ARow, ALibrary, ARecursive, ref ASequence, ALevel);
			}
		}
		
		private void PopulateRequiredLibraries(Program AProgram, Table ATable, Row ARow, Schema.Library ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			foreach (Schema.LibraryReference LLibrary in ALibrary.Libraries)
			{
				ASequence += 1;
				PopulateRequiredLibrary(AProgram, ATable, ARow, AProgram.Catalog.Libraries[LLibrary.Name], ARecursive, ref ASequence, ALevel);
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					int LSequence = 0;
					PopulateRequiredLibraries
					(
						AProgram, 
						LResult, 
						LRow, 
						AProgram.Catalog.Libraries[(string)Nodes[0].Execute(AProgram)],
						Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProgram) : true, 
						ref LSequence, 
						1
					);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    public class SystemClearStoreCountersNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).ClearStoreCounters();
			return null;
		}
    }
    
    // operator GetStoreCounters() : table { Sequence : Integer, Operation : String, TableName : String, IndexName : String, IsMatched : Boolean, IsRanged : Boolean, Duration : TimeSpan };
    public class SystemGetStoreCountersNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Operation", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TableName", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IndexName", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsMatched", APlan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRanged", APlan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsUpdatable", APlan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Duration", APlan.DataTypes.SystemTimeSpan));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();
				
				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).PopulateStoreCounters(LResult, LRow);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
}
