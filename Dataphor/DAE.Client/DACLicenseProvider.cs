/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client
{
	using System;
	using System.IO;
	using System.Security;
	using System.Security.Cryptography;
	using System.Security.Permissions;
	using System.ComponentModel;
	
	/*
		The DAC license is generator by the LicenseGenerator application.  If the key and initialization vector for the LicenseUtility class in 
		this assembly are changed, they must also be changed in the LicenseUtility class in the LicenseGenerator application.
	*/
	
	internal sealed class LicenseUtility : System.Object
	{
		internal const string CLicenseDataFileName = "DAC.license";
		internal const string CInstallDataFileName = "DAC.data";
		internal const string CLicenseData = "Data Access Components";
		internal const int CEvaluationDays = 30;
		
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

		private static AesManaged FProvider = new AesManaged();

		[SecurityPermission(SecurityAction.Demand)]
		public static string EncryptString(string AString)
		{
			if (AString == String.Empty)
				return AString;
			else
			{
				byte[] LEncryptedData;
				using (MemoryStream LStream = new MemoryStream())
				{
					using (CryptoStream LEncryptionStream = new CryptoStream(LStream, FProvider.CreateEncryptor(FKey, FIV), CryptoStreamMode.Write))
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
								throw new SecurityException("Encrypted data too long");
							LEncryptedData[0] = (byte)LStream.Length;
							return Convert.ToBase64String(LEncryptedData);
						}
					}
				}
			}
		}
		
		[SecurityPermission(SecurityAction.Demand)]
		public static string DecryptString(string AString)
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
					using (CryptoStream LDecryptionStream = new CryptoStream(LStream, FProvider.CreateDecryptor(FKey, FIV), CryptoStreamMode.Read))
					{
						using (StreamReader LReader = new StreamReader(LDecryptionStream))
						{
							return LReader.ReadToEnd();
						}
					}
				}
			}
		}
		
		public static void Validate(Type AType, object AInstance)
		{
			// Get the license from DAC.license ..Alphora\Dataphor\DAC.license
			string LLicenseData = String.Empty;
			string LLicenseFileName = String.Format("{0}{1}", Alphora.Dataphor.Windows.PathUtility.CommonAppDataPath(), LicenseUtility.CLicenseDataFileName);
			if (File.Exists(LLicenseFileName))
				using (StreamReader LReader = new StreamReader(LLicenseFileName))
				{
					LLicenseData = LReader.ReadToEnd();
				}

			// This file will contain the string value "Data Access Components" as an AES encrypted string using the given key and iv.
			if (LicenseUtility.DecryptString(LLicenseData) != LicenseUtility.CLicenseData)
			{
				// If the license check fails
				// Get the installation date from DAC.data ..Alphora\Dataphor\DAC.data
				string LInstallData = String.Empty;
				string LInstallDataFileName = String.Format("{0}{1}", Alphora.Dataphor.Windows.PathUtility.CommonAppDataPath(), LicenseUtility.CInstallDataFileName);

				// This file will contain the date of the installation as an AES encrypted string using the given key and iv.
				if (File.Exists(LInstallDataFileName))
					using (StreamReader LReader = new StreamReader(LInstallDataFileName))
					{
						LInstallData = LReader.ReadToEnd();
					}

				// If DAC.data does not exist, the encryption fails, or the number of days since the installation is more than 30, throw a license exception.
				DateTime LInstallDate = DateTime.MinValue;
				try
				{
					LInstallDate = DateTime.Parse(LicenseUtility.DecryptString(LInstallData));
				}
				catch (Exception E)
				{
					throw new LicenseException(AType, AInstance, "Failed to retrieve license data from DAC installation", E);
				}

				if (DateTime.Today > LInstallDate.AddDays(LicenseUtility.CEvaluationDays))
					throw new LicenseException(AType, AInstance, "Evaluation period has expired");
			}
		}
	}
}