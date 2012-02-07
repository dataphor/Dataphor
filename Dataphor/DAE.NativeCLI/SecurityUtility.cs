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
		private static byte[] _key = new byte[]
		{
			0x34, 0x4B, 0x3B, 0x52, 0xFB, 0x96, 0xF4, 0x1F,
			0x87, 0xA4, 0xB8, 0xA8, 0x22, 0x29, 0x97, 0x52, 
			0xF0, 0x32, 0xB4, 0xCC, 0xC5, 0xEA, 0xE6, 0xC6,
			0x1E, 0x3B, 0x29, 0x00, 0x13, 0x7F, 0xE0, 0x52 
		};

		private static byte[] _iV = new byte[]
		{
			0x8F, 0xDA, 0x11, 0xFB, 0x90, 0xF3, 0xBF, 0x27, 
			0xF3, 0xFC, 0xFF, 0x48, 0xEC, 0x9A, 0xE0, 0x41
		};
		
		private static string Encrypt(string stringValue, byte[] key, byte[] iV)
		{
			if (stringValue == String.Empty)
				return stringValue;
			else
			{
				byte[] encryptedData;
				using (MemoryStream stream = new MemoryStream())
				{
					AesManaged provider = new AesManaged();
					using (CryptoStream encryptionStream = new CryptoStream(stream, provider.CreateEncryptor(key, iV), CryptoStreamMode.Write))
					{
						using (StreamWriter writer = new StreamWriter(encryptionStream))
						{
							writer.Write(stringValue);
							writer.Flush();
							encryptionStream.FlushFinalBlock();
							stream.Position = 0;
							encryptedData = new byte[stream.Length + 1];
							stream.Read(encryptedData, 1, (int)stream.Length);
							if (stream.Length > Byte.MaxValue)
								throw new ArgumentException("Encrypted data must be less than 256 characters long");
							encryptedData[0] = (byte)stream.Length;
							return Convert.ToBase64String(encryptedData);
						}
					}
				}
			}
		}
		
		private static string Decrypt(string stringValue, byte[] key, byte[] iV)
		{
			if (stringValue == String.Empty)
				return stringValue;
			else
			{
				byte[] message = Convert.FromBase64String(stringValue);
				byte[] encryptedData = new byte[message[0]];
				for (int index = 0; index < encryptedData.Length; index++)
					encryptedData[index] = message[index + 1];

				using (Stream stream = new MemoryStream(encryptedData))
				{
					AesManaged provider = new AesManaged();
					using (CryptoStream decryptionStream = new CryptoStream(stream, provider.CreateDecryptor(key, iV), CryptoStreamMode.Read))
					{
						using (StreamReader reader = new StreamReader(decryptionStream))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}
		}

		public static string EncryptPassword(string password)
		{
			return Encrypt(password, _key, _iV);
		}
		
		public static string DecryptPassword(string password)
		{
			return Decrypt(password, _key, _iV);
		}
	}
}
