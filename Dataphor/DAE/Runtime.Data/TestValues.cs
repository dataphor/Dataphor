/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;

	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	/*
		operators for the implementation of the following type declaration:

create domain ID
{
	representation ID
	{
		AsString : string
		read class "IDReadAsStringNode"
		write class "IDWriteAsStringNode"
	} class "Dataphor.IDSelectorNode"
}
class "StringConveyor";
		
	*/	
	
	// operator ID(AAsString : string) : ID;
	// operator IDReadAsString(AValue : ID) : string;
	// operator IDWriteAsString(AValue : ID, AAsString : string) : ID;
	public class IDSelectorNode	: InstructionNode
	{
		protected override DataVar InternalExecute(Plan APlan, DataVars AArguments)
		{
			Scalar LResult = new Scalar((Schema.ScalarType)FDataType);
			new StringConveyor(LResult.Stream).Value = ((Scalar)AArguments[0].Value).ToString();
			return new DataVar(FDataType, LResult);
		}
	}
	
	public class IDReadAsStringNode : InstructionNode
	{
		protected override DataVar InternalExecute(Plan APlan, DataVars AArguments)
		{
			return new DataVar(FDataType, Scalar.FromString(((Scalar)AArguments[0].Value).ToString()));
		}
	}
	
	public class IDWriteAsStringNode : InstructionNode
	{
		protected override DataVar InternalExecute(Plan APlan, DataVars AArguments)
		{
			Scalar LResult = new Scalar((Schema.ScalarType)FDataType);
			new StringConveyor(LResult.Stream).Value = ((Scalar)AArguments[1].Value).ToString();
			return new DataVar(FDataType, LResult);
		}
	}
}