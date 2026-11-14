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
		public static void WriteInt32(byte[] buffer, int offset, int value)
		{
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 0x08);
			buffer[offset + 2] = (byte)(value >> 0x10);
			buffer[offset + 3] = (byte)(value >> 0x18);
		}

		public static int ReadInt32(byte[] buffer, int offset)
		{
			return buffer[offset] | (buffer[offset + 1] << 0x8) | (buffer[offset + 2] << 0x10) | (buffer[offset + 3] << 0x18);
		}

		public static void WriteInt64(byte[] buffer, int offset, long Value)
		{
			buffer[offset] = (byte)Value;
			buffer[offset + 1] = (byte)(Value >> 0x08);
			buffer[offset + 2] = (byte)(Value >> 0x10);
			buffer[offset + 3] = (byte)(Value >> 0x18);
			buffer[offset + 4] = (byte)(Value >> 0x20);
			buffer[offset + 5] = (byte)(Value >> 0x28);
			buffer[offset + 6] = (byte)(Value >> 0x30);
			buffer[offset + 7] = (byte)(Value >> 0x38);
		}

		public static long ReadInt64(byte[] buffer, int offset)
		{
			return 
				((long)(((buffer[offset + 4] | (buffer[offset + 5] << 0x08)) | (buffer[offset + 6] << 0x10)) | (buffer[offset + 7] << 0x18)) << 0x20) 
					| (uint)(((buffer[offset + 0] | (buffer[offset + 1] << 0x08)) | (buffer[offset + 2] << 0x10)) | (buffer[offset + 3] << 0x18));
		}

		public static short ReadInt16(byte[] buffer, int offset)
		{
			return (short)(buffer[offset] | (buffer[offset + 1] << 0x08));
		}

		public static void WriteInt16(byte[] buffer, int offset, short value)
		{
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 0x08);
		}

		public static decimal ReadDecimal(byte[] buffer, int offset)
		{
			int flags = ((buffer[offset + 12] | (buffer[offset + 13] << 0x08)) | (buffer[offset + 14] << 0x10)) | (buffer[offset + 15] << 0x18);
			return 
				new Decimal
				(
					((buffer[offset + 0] | (buffer[offset + 1] << 0x08)) | (buffer[offset + 2] << 0x10)) | (buffer[offset + 3] << 0x18),
					((buffer[offset + 4] | (buffer[offset + 5] << 0x08)) | (buffer[offset + 6] << 0x10)) | (buffer[offset + 7] << 0x18), 
					((buffer[offset + 8] | (buffer[offset + 9] << 0x08)) | (buffer[offset + 10] << 0x10)) | (buffer[offset + 11] << 0x18),
					(flags & Int32.MinValue) != 0,
					(byte)(flags >> 0x10)
				);
		}

		public static void WriteDecimal(byte[] buffer, int offset, decimal value)
		{
			var bits = Decimal.GetBits(value);
			buffer[offset + 0] = (byte)bits[0];
			buffer[offset + 1] = (byte)(bits[0] >> 0x08);
			buffer[offset + 2] = (byte)(bits[0] >> 0x10);
			buffer[offset + 3] = (byte)(bits[0] >> 0x18);
			buffer[offset + 4] = (byte)bits[1];
			buffer[offset + 5] = (byte)(bits[1] >> 0x08);
			buffer[offset + 6] = (byte)(bits[1] >> 0x10);
			buffer[offset + 7] = (byte)(bits[1] >> 0x18);
			buffer[offset + 8] = (byte)bits[2];
			buffer[offset + 9] = (byte)(bits[2] >> 0x08);
			buffer[offset + 10] = (byte)(bits[2] >> 0x10);
			buffer[offset + 11] = (byte)(bits[2] >> 0x18);
			buffer[offset + 12] = (byte)bits[3];
			buffer[offset + 13] = (byte)(bits[3] >> 0x08);
			buffer[offset + 14] = (byte)(bits[3] >> 0x10);
			buffer[offset + 15] = (byte)(bits[3] >> 0x18);
		}

		public static Guid ReadGuid(byte[] buffer, int offset)
		{
			return
				new Guid
				(
					ReadInt32(buffer, offset),
					ReadInt16(buffer, offset + 4),
					ReadInt16(buffer, offset + 6),
					buffer[offset + 8],
					buffer[offset + 9],
					buffer[offset + 10],
					buffer[offset + 11],
					buffer[offset + 12],
					buffer[offset + 13],
					buffer[offset + 14],
					buffer[offset + 15]
				);
		}

		public static void WriteGuid(byte[] buffer, int offset, Guid value)
		{
			var bytes = value.ToByteArray();
			for (int i = 0; i < bytes.Length; i++)
				buffer[offset + i] = bytes[i];
		}

		public static string ReadString(byte[] buffer, int offset)
		{
			int length = ReadInt32(buffer, offset);

			if (length > 0)
			{
				offset += sizeof(int);
				StringBuilder builder = new StringBuilder(length, length);
				for (int i = 0; i < length; i++)
					builder.Append((char)((ushort)ReadInt16(buffer, offset + i * 2)));
				return builder.ToString();
			}
			else
				return String.Empty;
		}

		public static void WriteString(byte[] buffer, int offset, string value)
		{
			ByteArrayUtility.WriteInt32(buffer, offset, value.Length);

			if (value.Length > 0)
			{
				offset += sizeof(int);
				for (int i = 0; i < value.Length; i++)
					ByteArrayUtility.WriteInt16(buffer, offset + i * 2, (short)value[i]);
			}
		}
	}
}
