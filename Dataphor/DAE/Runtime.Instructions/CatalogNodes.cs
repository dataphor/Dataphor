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
	
	// operator System.NameFromGuid(const AID : System.Guid) : System.Name
	public class SystemNameFromGuidNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Schema.Object.NameFromGuid((Guid)AArgument1);
		}
	}
	
    public class SystemNameSelectorNode : UnaryInstructionNode
    {
		public static void CheckValidName(string AValue)
		{
			if (!Parser.IsValidQualifiedIdentifier(AValue))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, AValue);
		}
		
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
			{
				string LArgument = (string)AArgument1;
				CheckValidName(LArgument);
				return LArgument;
			}
		}
    }
    
    public class SystemNameReadAccessorNode : UnaryInstructionNode
    {
		public SystemNameReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return AArgument1;
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
    }
    
    public class SystemNameWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			else
			#endif
			{
				string LArgument = (string)AArgument2;
				SystemNameSelectorNode.CheckValidName(LArgument);
				return LArgument;
			}
		}
    }
}
