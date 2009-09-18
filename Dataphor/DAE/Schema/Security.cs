/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Schema
{
	internal sealed class SecurityUtility : System.Object
	{
		// Note: This Key and IV are also specified in the NativeCLI.SecurityUtility class
		// This is sort of silly anyway because this code is open source. Security through obfuscation?
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
								throw new SchemaException(SchemaException.Codes.EncryptedDataTooLong);
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
		
		// TODO: Move these keys into the system catalog so that they are unique per server instance
		
		private static byte[] FStringKey = new byte[]
		{ 
			0x18, 0x39, 0x6D, 0x08, 0x18, 0x8B, 0x68, 0x91, 0xC4, 0x39, 0x69, 0xA1, 0x7B, 0x9A, 0x7A, 0x41, 
			0xB0, 0x43, 0x79, 0x7A, 0x80, 0x76, 0xE2, 0x28, 0x24, 0x6A, 0x44, 0x89, 0xFC, 0x0B, 0x1D, 0x9C
		};
		
		private static byte[] FStringIV = new byte[]
		{
			0x5C, 0x88, 0x79, 0xC6, 0xD5, 0x29, 0x21, 0x0F, 0xE0, 0x49, 0x32, 0x8A, 0x55, 0x38, 0x79, 0x37
		};
		
		public static string EncryptString(string AValue)
		{
			using (MemoryStream LStream = new MemoryStream())
			{
				RijndaelManaged LRijndael = new RijndaelManaged();
				using (CryptoStream LEncryptionStream = new CryptoStream(LStream, LRijndael.CreateEncryptor(FStringKey, FStringIV), CryptoStreamMode.Write))
				{
					using (BinaryWriter LWriter = new BinaryWriter(LEncryptionStream))
					{
						// Write SALT
						byte[] LSalt = new byte[6];
						new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(LSalt);
						LWriter.Write(LSalt);
						
						// Write string
						LWriter.Write(AValue);
						
						// Flush buffers
						LWriter.Flush();
						LEncryptionStream.FlushFinalBlock();
						
						// Convert to string
						return Convert.ToBase64String(LStream.GetBuffer(), 0, (int)LStream.Length);
					}
				}
			}
		}
			
		public static string DecryptString(string AValue)
		{
			using (Stream LStream = new MemoryStream(Convert.FromBase64String(AValue)))
			{
				RijndaelManaged LRijndael = new RijndaelManaged();
				using (CryptoStream LDecryptionStream = new CryptoStream(LStream, LRijndael.CreateDecryptor(FStringKey, FStringIV), CryptoStreamMode.Read))
				{
					using (BinaryReader LReader = new BinaryReader(LDecryptionStream))
					{
						// Read SALT
						LReader.ReadBytes(6);
						
						// Read string
						return LReader.ReadString();
					}
				}
			}
		}
	}
	
	public static class RightNames
	{
		// Server Right Names
		public const string CreateType = "System.CreateType";
		public const string CreateTable = "System.CreateTable";
		public const string CreateView = "System.CreateView";
		public const string CreateOperator = "System.CreateOperator";
		public const string CreateDevice = "System.CreateDevice";
		public const string CreateServer = "System.CreateServer";
		public const string CreateConstraint = "System.CreateConstraint";
		public const string CreateReference = "System.CreateReference";
		public const string CreateRole = "System.CreateRole";
		public const string CreateRight = "System.CreateRight";
		public const string CreateUser = "System.CreateUser";
		public const string AlterUser = "System.AlterUser";
		public const string DropUser = "System.DropUser";
		public const string MaintainSystemDeviceUsers = "System.MaintainSystemDeviceUsers";
		public const string MaintainUserSessions = "System.MaintainUserSessions";
		public const string HostImplementation = "System.HostImplementation"; // Determines whether a given user can create host-implemented constructs (operators, type maps, etc.)

		// Object-Specific Right Name Suffixes
		public const string Alter = "Alter";
		public const string Drop = "Drop";
		public const string Read = "Read";
		public const string Write = "Write";
		public const string CreateStore = "CreateStore";
		public const string AlterStore = "AlterStore";
		public const string DropStore = "DropStore";
		public const string Reconcile = "Reconcile";
		public const string MaintainUsers = "MaintainUsers";
		public const string Select = "Select";
		public const string Insert = "Insert";
		public const string Update = "Update";
		public const string Delete = "Delete";
		public const string Execute = "Execute";
	}
	
	public class Right : System.Object
	{
		public Right(string AName, string AOwnerID, int ACatalogObjectID) : base() 
		{
			FName = AName;
			FOwnerID = AOwnerID;
			FCatalogObjectID = ACatalogObjectID;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private string FOwnerID;
		public string OwnerID
		{ 
			get { return FOwnerID; } 
			set { FOwnerID = value; }
		}
		
		private int FCatalogObjectID = -1;
		public int CatalogObjectID
		{
			get { return FCatalogObjectID; }
			set { FCatalogObjectID = value; }
		}
		
		/// <summary>Returns true if AUser is the owner of this right, or is a member of a parent Group of the owner of this right, recursively.</summary>
		public bool IsOwner(string AUserID)
		{
			if ((AUserID == Server.Engine.CSystemUserID) || (AUserID == Server.Engine.CAdminUserID) || (AUserID == FOwnerID)) 
				return true;
			
			return false;
		}
		
		public bool IsGenerated
		{
			get { return FCatalogObjectID >= 0; }
		}
	}
	
	public class Rights : Dictionary<string, Right>
	{		
		public Rights() : base(){}
		
		public Right this[string AName]
		{
			get
			{
				Right LResult;
				if (!base.TryGetValue(AName, out LResult))
					throw new SchemaException(SchemaException.Codes.RightNotFound, AName);
				return LResult;
			}
		}
		
		public void Add(Right ARight)
		{
			Add(ARight.Name, ARight);
		}
	}
	
	public class RightAssignment : System.Object
	{
		public RightAssignment(string ARightName, bool AGranted)
		{
			FRightName = ARightName;
			FGranted = AGranted;
		}
		
		private string FRightName;
		public string RightName { get { return FRightName; } }
		
		private bool FGranted;
		public bool Granted
		{
			get { return FGranted; }
			set { FGranted = value; }
		}
	}
	
	public class RightAssignments : Dictionary<string, RightAssignment>
	{
		public RightAssignments() : base(){}
		
		public void Add(RightAssignment ARightAssignment)
		{
			Add(ARightAssignment.RightName, ARightAssignment);
		}
	}
	
	public class User : System.Object
	{
		public User() : base(){}
		public User(string AID, string AName, string APassword) : base()
		{
			ID = AID;
			Name = AName;
			Password = APassword;
		}
		
		private string FID = String.Empty;
		public string ID { get { return FID; } set { FID = value == null ? String.Empty : value; } }
		
		private string FName = String.Empty;
		public string Name { get { return FName; } set { FName = value == null ? String.Empty : value; } }
		
		private string FPassword = String.Empty;
		public string Password { get { return FPassword; } set { FPassword = value == null ? String.Empty : value; } }
		
		private RightAssignments FRightsCache;
		
		public void CacheRightAssignment(RightAssignment ARightAssignment)
		{
			if (FRightsCache == null)
				FRightsCache = new RightAssignments();
			
			FRightsCache.Add(ARightAssignment);
		}
		
		public RightAssignment FindCachedRightAssignment(string ARightName)
		{
			RightAssignment LResult;
			if (FRightsCache != null && FRightsCache.TryGetValue(ARightName, out LResult))
				return LResult;
			else
				return null;
		}
		
		public void ClearCachedRightAssignment(string ARightName)
		{
			if (FRightsCache != null)
				FRightsCache.Remove(ARightName);
		}
		
		public void ClearCachedRightAssignments()
		{
			FRightsCache = null;
		}
		
		public bool IsSystemUser()
		{
			return (String.Compare(ID, Server.Engine.CSystemUserID, true) == 0);
		}
		
		public bool IsAdminUser()
		{
			return ((String.Compare(ID, Server.Engine.CSystemUserID, true) == 0) || (String.Compare(ID, Server.Engine.CAdminUserID, true) == 0));
		}
	}
	
	public class Users : Dictionary<string, User>
	{		
		public Users() : base(StringComparer.OrdinalIgnoreCase){}
		
		public void Add(User AUser)
		{
			Add(AUser.ID, AUser);
		}
	}

	public class Role : CatalogObject
	{
		public Role(string AName) : base(AName) {}
		public Role(int AID, string AName) : base(AID, AName) {}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Role"), DisplayName); } }

		public override string[] GetRights()
		{
			return new string[] { Name + Schema.RightNames.Alter, Name + Schema.RightNames.Drop };
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			CreateRoleStatement LStatement = new CreateRoleStatement();
			LStatement.RoleName = Schema.Object.EnsureRooted(Name);
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			return LStatement;
		}
		
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropRoleStatement LStatement = new DropRoleStatement();
			LStatement.RoleName = Schema.Object.EnsureRooted(Name);
			return LStatement;
		}
	}
}

