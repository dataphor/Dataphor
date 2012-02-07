/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;

// todo: move to system somewhere, it is in DocSamples for testing/proof of concept
namespace DocSamples
{
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
			if (arguments[0] == null)
				return null;
			else
			#endif
			{
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					objectValue = program.Catalog.Objects[(string)arguments[0]];
				else if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemName))
					objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0]);
				else
					objectValue = program.Catalog.Objects[(int)arguments[0]];
				return MetaData.GetTag(objectValue.MetaData, (string)arguments[1], (string)arguments[2]);
			}
		}
	}
}