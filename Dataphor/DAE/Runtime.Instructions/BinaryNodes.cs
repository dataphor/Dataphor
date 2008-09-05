/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

using System;
using System.IO;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator Length(const ABinary : Binary) : Long
	public class BinaryLengthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (((Scalar)AArguments[0].Value).IsNative)
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsByteArray.Length));
				else
				{
					Stream LStream = AArguments[0].Value.OpenStream();
					try
					{
						return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LStream.Length));
					}
					finally
					{
						LStream.Close();
					}
				}
		}
	}

	/// <remarks> operator iEqual(Binary, Binary) : Boolean; </remarks>
	public class BinaryEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				byte[] LLeftByteArray = null;
				Stream LLeftStream = null;
				if (((Scalar)AArguments[0].Value).IsNative)
				{
					LLeftByteArray = AArguments[0].Value.AsByteArray;
					LLeftStream = new MemoryStream(LLeftByteArray, 0, LLeftByteArray.Length, false, true);
				}
				else
					LLeftStream = AArguments[0].Value.OpenStream();
				try
				{
					byte[] LRightByteArray = null;
					Stream LRightStream = null;
					if (((Scalar)AArguments[1].Value).IsNative)
					{
						LRightByteArray = AArguments[1].Value.AsByteArray;
						LRightStream = new MemoryStream(LRightByteArray, 0, LRightByteArray.Length, false, true);
					}
					else
						LRightStream = AArguments[1].Value.OpenStream();
					try
					{
						bool LEqual = true;
						int LLeftByte;
						int LRightByte;
						while (true)
						{
							LLeftByte = LLeftStream.ReadByte();
							LRightByte = LRightStream.ReadByte();
							
							if (LLeftByte != LRightByte)
							{
								LEqual = false;
								break;
							}
							
							if (LLeftByte == -1)
								break;
						}
						
						return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LEqual));
					}
					finally
					{
						LRightStream.Close();
					}
				}
				finally
				{
					LLeftStream.Close();
				}
			}
		}
	}
}


