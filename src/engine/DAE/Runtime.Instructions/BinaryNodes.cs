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
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			if (argument1 is byte[])
				return (long)((byte[])argument1).Length;
			if (argument1 is StreamID)
			{
				Stream stream = program.StreamManager.Open((StreamID)argument1, LockMode.Exclusive);
				try
				{
					return stream.Length;
				}
				finally
				{
					stream.Close();
				}
			}
			
			IScalar scalar = (IScalar)DataValue.FromNative(program.ValueManager, Nodes[0].DataType, argument1);
			if (scalar.IsNative)
				return scalar.AsByteArray.Length;
			else
			{
				Stream stream = scalar.OpenStream();
				try
				{
					return stream.Length;
				}
				finally
				{
					stream.Close();
				}
			}
		}
	}

	/// <remarks> operator iEqual(Binary, Binary) : Boolean; </remarks>
	public class BinaryEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				if ((argument1 is byte[]) && (argument2 is byte[]))
				{
					byte[] leftByteArray = (byte[])argument1;
					byte[] rightByteArray = (byte[])argument2;
					
					if (leftByteArray.Length != rightByteArray.Length)
						return false;
						
					for (int index = 0; index < leftByteArray.Length; index++)
						if (leftByteArray[index] != rightByteArray[index])
							return false;
						
					return true;
				}
				
				Stream leftStream = null;
				Stream rightStream = null;
				
				if (argument1 is StreamID)
					leftStream = program.StreamManager.Open((StreamID)argument1, LockMode.Exclusive);
				else
					leftStream = ((IScalar)DataValue.FromNative(program.ValueManager, Nodes[0].DataType, argument1)).OpenStream();
				try
				{
					if (argument2 is StreamID)
						rightStream = program.StreamManager.Open((StreamID)argument2, LockMode.Exclusive);
					else
						rightStream = ((IScalar)DataValue.FromNative(program.ValueManager, Nodes[0].DataType, argument2)).OpenStream();
					try
					{
						#if USESTREAMLENGTHWHENCOMPARINGSTREAMS
						bool equal = leftStream.Length == rightStream.Length; // TODO: Will this force a full read of the stream
						#else
						bool equal = true;
						#endif
						int leftByte;
						int rightByte;
						while (equal)
						{
							leftByte = leftStream.ReadByte();
							rightByte = rightStream.ReadByte();
							
							if (leftByte != rightByte)
							{
								equal = false;
								break;
							}
							
							if (leftByte == -1)
								break;
						}
						
						return equal;
					}
					finally
					{
						rightStream.Close();
					}
				}
				finally
				{
					leftStream.Close();
				}	
			}
		}
	}

	/// <remarks> operator ToBase64String(AGraphic : Graphic) : string </remarks>
	/// <remarks> operator ToBase64String(ABinary : Binary) : string </remarks>
	public class BinaryToBase64StringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			if (argument1 is byte[])
				return Convert.ToBase64String(((byte[])argument1));
			if (argument1 is StreamID)
			{
				Stream stream = program.StreamManager.Open((StreamID)argument1, LockMode.Exclusive);				 				
				try
				{
					byte[] buffer = new byte[stream.Length];
					stream.Read(buffer, 0, (int)stream.Length);
					return Convert.ToBase64String(buffer);
				}
				finally
				{
					stream.Close();
				}
			}

			IScalar scalar = (IScalar)DataValue.FromNative(program.ValueManager, Nodes[0].DataType, argument1);
			if (scalar.IsNative)
				return scalar.AsBase64String;
			else
			{
				Stream stream = scalar.OpenStream();
				try
				{
					byte[] buffer = new byte[stream.Length];
					stream.Read(buffer, 0, (int)stream.Length);
					return Convert.ToBase64String(buffer);
				}
				finally
				{
					stream.Close();
				}
			}
		}
	}
}


