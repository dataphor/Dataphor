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

	// operator ObjectExists(const AName : Name) : Boolean
	// operator ObjectExists(const ASpecifier : String) : Boolean
	public class ObjectExistsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1, false) != null;
				else
					return program.ResolveCatalogIdentifier((string)argument1, false) != null;
		}
	}
	
	// operator System.NameFromGuid(const AID : System.Guid) : System.Name
	public class SystemNameFromGuidNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Schema.Object.NameFromGuid((Guid)argument1);
		}
	}
	
    public class SystemNameSelectorNode : UnaryInstructionNode
    {
		public static void CheckValidName(string tempValue)
		{
			if (!Parser.IsValidQualifiedIdentifier(tempValue))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, tempValue);
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				string argument = (string)argument1;
				CheckValidName(argument);
				return argument;
			}
		}
    }
    
    public class SystemNameReadAccessorNode : UnaryInstructionNode
    {
		public SystemNameReadAccessorNode() : base()
		{
			_isOrderPreserving = true;
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return argument1;
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			_isOrderPreserving = true;
		}
    }
    
    public class SystemNameWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			else
			#endif
			{
				string argument = (string)argument2;
				SystemNameSelectorNode.CheckValidName(argument);
				return argument;
			}
		}
    }
}
