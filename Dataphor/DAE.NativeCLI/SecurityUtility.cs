/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	internal sealed class SecurityUtility
	{
		private static byte[] FKey = new byte[]
		{
			0x34, 0x4B, 0x3B, 0x52, 0xFB, 0x96, 0xF4, 0x1F,
			0x87, 0xA4, 0xB8, 0xA8, 0x22, 0x29, 0x97, 0x52, 
			0xF0, 0x32, 0xB4, 0xCC, 0xC5, 0xEA, 0xE6, 0xC6,
			0x1E, 0x3B, 0x29, 0x00, 0x13, 0x7F, 0xE0, 0x52 
		};

		private static byte[] FIV = new byte[]
		{
			0x8F, 0xDA, 0x11, 0xFB, 0x90, 0xF3, 0xBF, 0x27, 
			0xF3, 0xFC, 0xFF, 0x48, 0xEC, 0x9A, 0xE0, 0x41
		};
		
		private static string Encrypt(string AString, byte[] AKey, byte[] AIV)
		{
			if (AString == String.Empty)
				return AString;
			else
			{
				byte[] LEncryptedData;
				using (MemoryStream LStream = new MemoryStream())
				{
					RijndaelManaged LRijndael = new RijndaelManaged();
					using (CryptoStream LEncryptionStream = new CryptoStream(LStream, LRijndael.CreateEncryptor(AKey, AIV), CryptoStreamMode.Write))
					{
						using (StreamWriter LWriter = new StreamWriter(LEncryptionStream))
						{
							LWriter.Write(AString);
							LWriter.Flush();
							LEncryptionStream.FlushFinalBlock();
							LStream.Position = 0;
							LEncryptedData = new byte[LStream.Length + 1];
							LStream.Read(LEncryptedData, 1, (int)LStream.Length);
							if (LStream.Length > Byte.MaxValue)
								throw new ArgumentException("Encrypted data must be less than 256 characters long");
							LEncryptedData[0] = (byte)LStream.Length;
							return Convert.ToBase64String(LEncryptedData);
						}
					}
				}
			}
		}
		
		private static string Decrypt(string AString, byte[] AKey, byte[] AIV)
		{
			if (AString == String.Empty)
				return AString;
			else
			{
				byte[] LMessage = Convert.FromBase64String(AString);
				byte[] LEncryptedData = new byte[LMessage[0]];
				for (int LIndex = 0; LIndex < LEncryptedData.Length; LIndex++)
					LEncryptedData[LIndex] = LMessage[LIndex + 1];

				using (Stream LStream = new MemoryStream(LEncryptedData))
				{
					RijndaelManaged LRijndael = new RijndaelManaged();
					using (CryptoStream LDecryptionStream = new CryptoStream(LStream, LRijndael.CreateDecryptor(AKey, AIV), CryptoStreamMode.Read))
					{
						using (StreamReader LReader = new StreamReader(LDecryptionStream))
						{
							return LReader.ReadToEnd();
						}
					}
				}
			}
		}

		public static string EncryptPassword(string APassword)
		{
			return Encrypt(APassword, FKey, FIV);
		}
		
		public static string DecryptPassword(string APassword)
		{
			return Decrypt(APassword, FKey, FIV);
		}
	}
}
