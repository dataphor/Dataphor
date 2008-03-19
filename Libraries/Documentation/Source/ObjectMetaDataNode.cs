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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Object LObject = null;

			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					LObject = AProcess.Plan.Catalog.Objects[AArguments[0].Value.AsString];
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemName))
					LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString);
				else
					LObject = AProcess.Plan.Catalog.Objects[AArguments[0].Value.AsInt32];
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						MetaData.GetTag(LObject.MetaData, AArguments[1].Value.AsString, AArguments[2].Value.AsString)
					)
				);
			}
		}
	}
}