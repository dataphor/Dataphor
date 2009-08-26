using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor
{
	/// <summary> Reading native types to and from byte arrays. </summary>
	/// <remarks> The read portions of this utility could conceivably be replaced with calls to BinConverter, 
	///  but there are extra bounds checks done by BitConverter that are avoided here. </remarks>
	public static class ByteArrayUtility
	{
		public static void WriteInt32(byte[] ABuffer, int AOffset, int AValue)
		{
			ABuffer[AOffset] = (byte)AValue;
			ABuffer[AOffset + 1] = (byte)(AValue >> 0x08);
			ABuffer[AOffset + 2] = (byte)(AValue >> 0x10);
			ABuffer[AOffset + 3] = (byte)(AValue >> 0x18);
		}

		public static int ReadInt32(byte[] ABuffer, int AOffset)
		{
			return ABuffer[AOffset] | (ABuffer[AOffset + 1] << 0x8) | (ABuffer[AOffset + 2] << 0x10) | (ABuffer[AOffset + 3] << 0x18);
		}

		public static void WriteInt64(byte[] ABuffer, int AOffset, long Value)
		{
			ABuffer[AOffset] = (byte)Value;
			ABuffer[AOffset + 1] = (byte)(Value >> 0x08);
			ABuffer[AOffset + 2] = (byte)(Value >> 0x10);
			ABuffer[AOffset + 3] = (byte)(Value >> 0x18);
			ABuffer[AOffset + 4] = (byte)(Value >> 0x20);
			ABuffer[AOffset + 5] = (byte)(Value >> 0x28);
			ABuffer[AOffset + 6] = (byte)(Value >> 0x30);
			ABuffer[AOffset + 7] = (byte)(Value >> 0x38);
		}

		public static long ReadInt64(byte[] ABuffer, int AOffset)
		{
			return 
				((uint)(((ABuffer[AOffset + 4] | (ABuffer[AOffset + 5] << 0x08)) | (ABuffer[AOffset + 6] << 0x10)) | (ABuffer[AOffset + 7] << 0x18)) << 0x20) 
					| (uint)(((ABuffer[AOffset + 0] | (ABuffer[AOffset + 1] << 0x08)) | (ABuffer[AOffset + 2] << 0x10)) | (ABuffer[AOffset + 3] << 0x18));
		}

		public static short ReadInt16(byte[] ABuffer, int AOffset)
		{
			return (short)(ABuffer[AOffset] | (ABuffer[AOffset + 1] << 0x08));
		}

		public static void WriteInt16(byte[] ABuffer, int AOffset, short AValue)
		{
			ABuffer[AOffset] = (byte)AValue;
			ABuffer[AOffset + 1] = (byte)(AValue >> 0x08);
		}

		public static decimal ReadDecimal(byte[] ABuffer, int AOffset)
		{
			int LFlags = ((ABuffer[AOffset + 12] | (ABuffer[AOffset + 13] << 0x08)) | (ABuffer[AOffset + 14] << 0x10)) | (ABuffer[AOffset + 15] << 0x18);
			return 
				new Decimal
				(
					((ABuffer[AOffset + 0] | (ABuffer[AOffset + 1] << 0x08)) | (ABuffer[AOffset + 2] << 0x10)) | (ABuffer[AOffset + 3] << 0x18),
					((ABuffer[AOffset + 4] | (ABuffer[AOffset + 5] << 0x08)) | (ABuffer[AOffset + 6] << 0x10)) | (ABuffer[AOffset + 7] << 0x18), 
					((ABuffer[AOffset + 8] | (ABuffer[AOffset + 9] << 0x08)) | (ABuffer[AOffset + 10] << 0x10)) | (ABuffer[AOffset + 11] << 0x18),
					(LFlags & Int32.MinValue) != 0,
					(byte)(LFlags >> 0x10)
				);
		}

		public static void WriteDecimal(byte[] ABuffer, int AOffset, decimal AValue)
		{
			var LBits = Decimal.GetBits(AValue);
			ABuffer[AOffset + 0] = (byte)LBits[0];
			ABuffer[AOffset + 1] = (byte)(LBits[0] >> 0x08);
			ABuffer[AOffset + 2] = (byte)(LBits[0] >> 0x10);
			ABuffer[AOffset + 3] = (byte)(LBits[0] >> 0x18);
			ABuffer[AOffset + 4] = (byte)LBits[1];
			ABuffer[AOffset + 5] = (byte)(LBits[1] >> 0x08);
			ABuffer[AOffset + 6] = (byte)(LBits[1] >> 0x10);
			ABuffer[AOffset + 7] = (byte)(LBits[1] >> 0x18);
			ABuffer[AOffset + 8] = (byte)LBits[2];
			ABuffer[AOffset + 9] = (byte)(LBits[2] >> 0x08);
			ABuffer[AOffset + 10] = (byte)(LBits[2] >> 0x10);
			ABuffer[AOffset + 11] = (byte)(LBits[2] >> 0x18);
			ABuffer[AOffset + 12] = (byte)LBits[3];
			ABuffer[AOffset + 13] = (byte)(LBits[3] >> 0x08);
			ABuffer[AOffset + 14] = (byte)(LBits[3] >> 0x10);
			ABuffer[AOffset + 15] = (byte)(LBits[3] >> 0x18);
		}

		public static Guid ReadGuid(byte[] ABuffer, int AOffset)
		{
			return
				new Guid
				(
					ReadInt32(ABuffer, AOffset),
					ReadInt16(ABuffer, AOffset + 4),
					ReadInt16(ABuffer, AOffset + 6),
					ABuffer[AOffset + 8],
					ABuffer[AOffset + 9],
					ABuffer[AOffset + 10],
					ABuffer[AOffset + 11],
					ABuffer[AOffset + 12],
					ABuffer[AOffset + 13],
					ABuffer[AOffset + 14],
					ABuffer[AOffset + 15]
				);
		}

		public static void WriteGuid(byte[] ABuffer, int AOffset, Guid AValue)
		{
			var LBytes = AValue.ToByteArray();
			for (int i = 0; i < LBytes.Length; i++)
				ABuffer[AOffset + i] = LBytes[i];
		}

		public static string ReadString(byte[] ABuffer, int AOffset)
		{
			int LLength = ReadInt32(ABuffer, AOffset);

			if (LLength > 0)
			{
				AOffset += sizeof(int);
				StringBuilder LBuilder = new StringBuilder(LLength, LLength);
				for (int i = 0; i < LLength; i++)
					LBuilder.Append((char)((ushort)ReadInt16(ABuffer, AOffset + i * 2)));
				return LBuilder.ToString();
			}
			else
				return String.Empty;
		}

		public static void WriteString(byte[] ABuffer, int AOffset, string AValue)
		{
			ByteArrayUtility.WriteInt32(ABuffer, AOffset, AValue.Length);

			if (AValue.Length > 0)
			{
				AOffset += sizeof(int);
				for (int i = 0; i < AValue.Length; i++)
					ByteArrayUtility.WriteInt16(ABuffer, AOffset + i * 2, (short)AValue[i]);
			}
		}
	}
}
