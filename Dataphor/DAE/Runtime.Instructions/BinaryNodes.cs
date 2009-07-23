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
	public class BinaryLengthNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			if (AArgument1 is byte[])
				return ((byte[])AArgument1).Length;
			if (AArgument1 is StreamID)
			{
				Stream LStream = AProcess.StreamManager.Open((StreamID)AArgument1, LockMode.Exclusive);
				try
				{
					return LStream.Length;
				}
				finally
				{
					LStream.Close();
				}
			}
			
			Scalar LScalar = (Scalar)DataValue.FromNative(AProcess, Nodes[0].DataType, AArgument1);
			if (LScalar.IsNative)
				return LScalar.AsByteArray.Length;
			else
			{
				Stream LStream = LScalar.OpenStream();
				try
				{
					return LStream.Length;
				}
				finally
				{
					LStream.Close();
				}
			}
		}
	}

	/// <remarks> operator iEqual(Binary, Binary) : Boolean; </remarks>
	public class BinaryEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				if ((AArgument1 is byte[]) && (AArgument2 is byte[]))
				{
					byte[] LLeftByteArray = (byte[])AArgument1;
					byte[] LRightByteArray = (byte[])AArgument2;
					
					if (LLeftByteArray.Length != LRightByteArray.Length)
						return false;
						
					for (int LIndex = 0; LIndex < LLeftByteArray.Length; LIndex++)
						if (LLeftByteArray[LIndex] != LRightByteArray[LIndex])
							return false;
						
					return true;
				}
				
				Stream LLeftStream = null;
				Stream LRightStream = null;
				
				if (AArgument1 is StreamID)
					LLeftStream = AProcess.StreamManager.Open((StreamID)AArgument1, LockMode.Exclusive);
				else
					LLeftStream = ((Scalar)DataValue.FromNative(AProcess, Nodes[0].DataType, AArgument1)).OpenStream();
				try
				{
					if (AArgument2 is StreamID)
						LRightStream = AProcess.StreamManager.Open((StreamID)AArgument2, LockMode.Exclusive);
					else
						LRightStream = ((Scalar)DataValue.FromNative(AProcess, Nodes[0].DataType, AArgument2)).OpenStream();
					try
					{
						#if USESTREAMLENGTHWHENCOMPARINGSTREAMS
						bool LEqual = LLeftStream.Length == LRightStream.Length; // TODO: Will this force a full read of the stream
						#else
						bool LEqual = true;
						#endif
						int LLeftByte;
						int LRightByte;
						while (LEqual)
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
						
						return LEqual;
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


