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
	public static class SecurityUtility : System.Object
	{
		// Note: This Key and IV are also specified in the NativeCLI.SecurityUtility class
		// This is sort of silly anyway because this code is open source. Security through obfuscation?
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
								throw new SchemaException(SchemaException.Codes.EncryptedDataTooLong);
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
		
		// TODO: Move these keys into the system catalog so that they are unique per server instance
		
		private static byte[] _stringKey = new byte[]
		{ 
			0x18, 0x39, 0x6D, 0x08, 0x18, 0x8B, 0x68, 0x91, 0xC4, 0x39, 0x69, 0xA1, 0x7B, 0x9A, 0x7A, 0x41, 
			0xB0, 0x43, 0x79, 0x7A, 0x80, 0x76, 0xE2, 0x28, 0x24, 0x6A, 0x44, 0x89, 0xFC, 0x0B, 0x1D, 0x9C
		};
		
		private static byte[] _stringIV = new byte[]
		{
			0x5C, 0x88, 0x79, 0xC6, 0xD5, 0x29, 0x21, 0x0F, 0xE0, 0x49, 0x32, 0x8A, 0x55, 0x38, 0x79, 0x37
		};
		
		public static string EncryptString(string tempValue)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				AesManaged provider = new AesManaged();
				using (CryptoStream encryptionStream = new CryptoStream(stream, provider.CreateEncryptor(_stringKey, _stringIV), CryptoStreamMode.Write))
				{
					using (BinaryWriter writer = new BinaryWriter(encryptionStream))
					{
						// Write SALT
						byte[] salt = new byte[6];
						new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(salt);
						writer.Write(salt);
						
						// Write string
						writer.Write(tempValue);
						
						// Flush buffers
						writer.Flush();
						encryptionStream.FlushFinalBlock();
						
						// Convert to string
						return Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
					}
				}
			}
		}
			
		public static string DecryptString(string tempValue)
		{
			using (Stream stream = new MemoryStream(Convert.FromBase64String(tempValue)))
			{
				AesManaged provider = new AesManaged();
				using (CryptoStream decryptionStream = new CryptoStream(stream, provider.CreateDecryptor(_stringKey, _stringIV), CryptoStreamMode.Read))
				{
					using (BinaryReader reader = new BinaryReader(decryptionStream))
					{
						// Read SALT
						reader.ReadBytes(6);
						
						// Read string
						return reader.ReadString();
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
		public Right(string name, string ownerID, int catalogObjectID) : base() 
		{
			_name = name;
			_ownerID = ownerID;
			_catalogObjectID = catalogObjectID;
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		private string _ownerID;
		public string OwnerID
		{ 
			get { return _ownerID; } 
			set { _ownerID = value; }
		}
		
		private int _catalogObjectID = -1;
		public int CatalogObjectID
		{
			get { return _catalogObjectID; }
			set { _catalogObjectID = value; }
		}
		
		/// <summary>Returns true if AUser is the owner of this right, or is a member of a parent Group of the owner of this right, recursively.</summary>
		public bool IsOwner(string userID)
		{
			if ((userID == Server.Engine.SystemUserID) || (userID == Server.Engine.AdminUserID) || (userID == _ownerID)) 
				return true;
			
			return false;
		}
		
		public bool IsGenerated
		{
			get { return _catalogObjectID >= 0; }
		}
	}
	
	public class Rights : Dictionary<string, Right>
	{		
		public Rights() : base(){}
		
		public new Right this[string name]
		{
			get
			{
				Right result;
				if (!base.TryGetValue(name, out result))
					throw new SchemaException(SchemaException.Codes.RightNotFound, name);
				return result;
			}
		}
		
		public void Add(Right right)
		{
			Add(right.Name, right);
		}
	}
	
	public class RightAssignment : System.Object
	{
		public RightAssignment(string rightName, bool granted)
		{
			_rightName = rightName;
			_granted = granted;
		}
		
		private string _rightName;
		public string RightName { get { return _rightName; } }
		
		private bool _granted;
		public bool Granted
		{
			get { return _granted; }
			set { _granted = value; }
		}
	}
	
	public class RightAssignments : Dictionary<string, RightAssignment>
	{
		public RightAssignments() : base(){}
		
		public void Add(RightAssignment rightAssignment)
		{
			Add(rightAssignment.RightName, rightAssignment);
		}
	}
	
	public class User : System.Object
	{
		public User() : base(){}
		public User(string iD, string name, string password) : base()
		{
			ID = iD;
			Name = name;
			Password = password;
		}
		
		private string _iD = String.Empty;
		public string ID { get { return _iD; } set { _iD = value == null ? String.Empty : value; } }
		
		private string _name = String.Empty;
		public string Name { get { return _name; } set { _name = value == null ? String.Empty : value; } }
		
		private string _password = String.Empty;
		public string Password { get { return _password; } set { _password = value == null ? String.Empty : value; } }
		
		private RightAssignments _rightsCache;
		
		public void CacheRightAssignment(RightAssignment rightAssignment)
		{
			if (_rightsCache == null)
				_rightsCache = new RightAssignments();
			
			_rightsCache.Add(rightAssignment);
		}
		
		public RightAssignment FindCachedRightAssignment(string rightName)
		{
			RightAssignment result;
			if (_rightsCache != null && _rightsCache.TryGetValue(rightName, out result))
				return result;
			else
				return null;
		}
		
		public void ClearCachedRightAssignment(string rightName)
		{
			if (_rightsCache != null)
				_rightsCache.Remove(rightName);
		}

		public void ClearCachedRightAssignments(string[] rightNames)
		{
			if (_rightsCache != null)
			{
				for (int i = 0; i < rightNames.Length; i++)
				{
					_rightsCache.Remove(rightNames[i]);
				}
			}
		}
		
		public void ClearCachedRightAssignments()
		{
			_rightsCache = null;
		}
		
		public bool IsSystemUser()
		{
			return String.Equals(ID, Server.Engine.SystemUserID, StringComparison.OrdinalIgnoreCase);
		}
		
		public bool IsAdminUser()
		{
			return 
				String.Equals(ID, Server.Engine.SystemUserID, StringComparison.OrdinalIgnoreCase) 
					|| String.Equals(ID, Server.Engine.AdminUserID, StringComparison.OrdinalIgnoreCase);
		}
	}
	
	public class Users : Dictionary<string, User>
	{		
		public Users() : base(StringComparer.OrdinalIgnoreCase){}
		
		public void Add(User user)
		{
			Add(user.ID, user);
		}
	}

	public class Role : CatalogObject
	{
		public Role(string name) : base(name) {}
		public Role(int iD, string name) : base(iD, name) {}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Role"), DisplayName); } }

		public override string[] GetRights()
		{
			return new string[] { Name + Schema.RightNames.Alter, Name + Schema.RightNames.Drop };
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				CreateRoleStatement statement = new CreateRoleStatement();
				statement.RoleName = Schema.Object.EnsureRooted(Name);
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropRoleStatement statement = new DropRoleStatement();
			statement.RoleName = Schema.Object.EnsureRooted(Name);
			return statement;
		}
	}
}

