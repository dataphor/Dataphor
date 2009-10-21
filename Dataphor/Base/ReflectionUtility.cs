/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	/// <summary>
	/// Defines a Token used by the ResolveReference method to provide the root instance of a given reference.
	/// </summary>
	public class Token
	{
		public Token(string AName, object AValue)
		{
			Name = AName;
			Value = AValue;
		}
		
		public string Name;
		public object Value;
	}
	
	public class ReflectionUtility 
	{
		/// <summary> Gets the Type of the property or field member. </summary>
		public static Type GetMemberType(MemberInfo AMember)
		{
			if (AMember is PropertyInfo)
				return ((PropertyInfo)AMember).PropertyType;
			else
				return ((FieldInfo)AMember).FieldType;
		}

		/// <summary> Sets a property or field member of the specified instance to the specified value. </summary>
		public static void SetMemberValue(MemberInfo AMember, object AInstance, object AValue)
		{
			try
			{
				if (AMember is PropertyInfo)
					((PropertyInfo)AMember).SetValue(AInstance, AValue, new Object[] {});
				else
					((FieldInfo)AMember).SetValue(AInstance, AValue);
			}
			catch (Exception E)
			{
				throw new BaseException(BaseException.Codes.UnableToSetProperty, E, AMember.Name);
			}
		}

		/// <summary> Sets the member of the given instance to the given string value. </summary>
		/// <remarks> Sets the member using the converter for the member's type. </remarks>
		public static void SetInstanceMember(object AInstance, string AMemberName, string AValue)
		{
			MemberInfo LMember = FindSimpleMember(AInstance.GetType(), AMemberName);
			SetMemberValue
			(
				LMember,
				AInstance,
				StringToValue(AValue, GetMemberType(LMember))
			);
		}

		/// <summary> Finds an appropriate field, property, or method member with no parameters. </summary>
		/// <remarks> Throws if a qualifying member is not found. </remarks>
		public static MemberInfo FindSimpleMember(Type AType, string AName)
		{
			// Find an appropriate member matching the attribute
			MemberInfo[] LMembers = AType.GetMember
			(
				AName, 
				MemberTypes.Property | MemberTypes.Field | MemberTypes.Method, 
				BindingFlags.IgnoreCase	| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
			);
			MemberInfo LResult = null;
			foreach (MemberInfo LMember in LMembers)
				if 
				(
					(LMember is FieldInfo) || 
					(
						(
							(LMember is PropertyInfo) && 
							(((PropertyInfo)LMember).GetIndexParameters().Length == 0)
						) ||
						(
							(LMember is MethodInfo) &&
							(((MethodInfo)LMember).GetParameters().Length == 0)
						)
					)
				)
					LResult = LMember;
			if (LResult == null)
				throw new BaseException(BaseException.Codes.MemberNotFound, AName, AType.FullName);
			return LResult;
		}
		
		/// <summary> Gets a single attribute for a type based on the attribute class type. </summary>
		/// <remarks> Returns null if not found.  Throws if more than one instance of the specified attribute appears. </remarks>
		public static object GetAttribute(ICustomAttributeProvider AProvider, Type AAttributeType)
		{
			if (AProvider == null)
				return null;
			object[] LAttributes = AProvider.GetCustomAttributes(AAttributeType, true);
			if (LAttributes.Length > 1)
				throw new BaseException(BaseException.Codes.ExpectingSingleAttribute, AAttributeType.Name);
			else if (LAttributes.Length == 1)
				return LAttributes[0];
			else 
			{
				if (AProvider is MemberInfo)
					return GetAttribute(((MemberInfo)AProvider).DeclaringType, AAttributeType);
				else
					return null;
			}
		}

		/// <summary> Finds a constructor for a type based on the provided signature. </summary>
		/// <remarks> Throws if not found. </remarks>
		public static ConstructorInfo FindConstructor(string ASignature, Type AType)
		{
			// Determine the constructor signature
			string[] LSignatureNames = ASignature.Split(new char[] {';'});
			Type[] LSignature = new Type[LSignatureNames.Length];
			for (int i = LSignature.Length - 1; i >= 0; i--)
				LSignature[i] = Type.GetType(LSignatureNames[i], true, true);

			// Find the matching constructor
			ConstructorInfo LConstructor = AType.GetConstructor(LSignature);
			if (LConstructor == null)
				throw new BaseException(BaseException.Codes.DefaultConstructorNotFound, ASignature);

			return LConstructor;
		}

		/// <summary> Converts a string value type to the .NET value based on the provided type. </summary>
		public static object StringToValue(string AValue, Type AType)
		{
			#if SILVERLIGHT
			if (AValue == null)
				throw new BaseException(BaseException.Codes.CannotConvertNull, ErrorSeverity.System);
			switch (Type.GetTypeCode(AType))
			{
				case TypeCode.Boolean:
					return Convert.ToBoolean(AValue);
				case TypeCode.Char:
					return Convert.ToChar(AValue);
				case TypeCode.SByte:
					return Convert.ToSByte(AValue);
				case TypeCode.Byte:
					return Convert.ToByte(AValue);
				case TypeCode.Int16:
					return Convert.ToInt16(AValue);
				case TypeCode.UInt16:
					return Convert.ToUInt16(AValue);
				case TypeCode.Int32:
					return Convert.ToInt32(AValue);
				case TypeCode.UInt32:
					return Convert.ToUInt32(AValue);
				case TypeCode.Int64:
					return Convert.ToInt64(AValue);
				case TypeCode.UInt64:
					return Convert.ToUInt64(AValue);
				case TypeCode.Single:
					return Convert.ToSingle(AValue);
				case TypeCode.Double:
					return Convert.ToDouble(AValue);
				case TypeCode.Decimal:
					return Convert.ToDecimal(AValue);
				case TypeCode.DateTime:
					return Convert.ToDateTime(AValue);
				case TypeCode.String:
					return AValue;
				default:
					if (AType == typeof(byte[]))
						return Convert.FromBase64String(AValue);
					else if (AType != typeof(object))
					{
						if (AType == typeof(TimeSpan))
							return TimeSpan.Parse(AValue);
						else if (AType == typeof(Guid))
							return new Guid(AValue);
						else if (AType == typeof(Uri))
							return new Uri(AValue);
						else
							break;
					}
					else if (typeof(Enum).IsAssignableFrom(AType))
						return Enum.Parse(AType, AValue, true);
					break;
			}
			throw new BaseException(BaseException.Codes.CannotConvertFromString, AType.Name);
			#else
			return TypeDescriptor.GetConverter(AType).ConvertFromString(AValue);
			#endif
		}

		/// <summary> Converts a .NET value to a string based on the provided type. </summary>
		public static string ValueToString(object AValue, Type AType)
		{
			#if SILVERLIGHT
			if (AValue == null)
				throw new BaseException(BaseException.Codes.CannotConvertNull, ErrorSeverity.System);
			switch (Type.GetTypeCode(AType))
			{
				case TypeCode.Boolean:
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.DateTime:
				case TypeCode.String:
					return AValue.ToString();

				default:
					if (AType == typeof(byte[]))
						return Convert.ToBase64String((byte[])AValue);
					else if (AType != typeof(object))
					{
						if (AType == typeof(TimeSpan) || AType == typeof(Guid) || AType == typeof(Uri) || typeof(Enum).IsAssignableFrom(AType))
							return AValue.ToString();
					}
					break;
			}
			throw new BaseException(BaseException.Codes.CannotConvertToString, AType.Name);
			#else
			return TypeDescriptor.GetConverter(AType).ConvertToString(AValue);
			#endif
		}

		/// <summary> Gets a field, property, or simple method's value for a particular instance. </summary>
		public static object GetMemberValue(MemberInfo AMember, object AInstance)
		{
			if (AMember is PropertyInfo) 
			{
				if (((PropertyInfo)AMember).GetIndexParameters().Length != 0)
					return null;
				return ((PropertyInfo)AMember).GetValue(AInstance, new Object[] {});
			}
			else if (AMember is FieldInfo)
				return ((FieldInfo)AMember).GetValue(AInstance);
			else
				return ((MethodInfo)AMember).Invoke(AInstance, new object[] {});
		}

		public static object ResolveToken(string AToken, params Token[] ATokens)
		{
			for (int LIndex = 0; LIndex < ATokens.Length; LIndex++)
				if (ATokens[LIndex].Name == AToken)
					return ATokens[LIndex].Value;
					
			throw new ArgumentException(String.Format("Could not resolve token {0}.", AToken));
		}
		
		public static object ResolveReference(string AReference, params Token[] ATokens)
		{
			string[] LReferences = AReference.Split('.');
			object LObject = null;
			for (int LIndex = 0; LIndex < LReferences.Length; LIndex++)
			{
				if (LIndex == 0)
					LObject = ResolveToken(LReferences[LIndex], ATokens);
				else
				{
					int LBracketIndex = LReferences[LIndex].IndexOf('[');
					if (LBracketIndex > 0)
					{
						if (LReferences[LIndex].IndexOf(']') != LReferences[LIndex].Length - 1)
							throw new ArgumentException(String.Format("Indexer reference does not have a closing bracket: {0}", LReferences[LIndex]));
						string LMemberName = LReferences[LIndex].Substring(0, LBracketIndex);
						LObject = GetMemberValue(FindSimpleMember(LObject.GetType(), LMemberName), LObject);
						
						string LReferenceIndex = LReferences[LIndex].Substring(LBracketIndex + 1, LReferences[LIndex].Length - 1 - (LBracketIndex + 1));
						string[] LMemberIndexes = LReferenceIndex.Split(',');

						if (LObject.GetType().IsArray)
						{
							Array LArray = (Array)LObject;
							int[] LMemberIndexValues = new int[LMemberIndexes.Length];
							for (int LMemberIndex = 0; LMemberIndex < LMemberIndexes.Length; LMemberIndex++)
								LMemberIndexValues[LMemberIndex] = Int32.Parse(LMemberIndexes[LMemberIndex]);
								
							LObject = LArray.GetValue(LMemberIndexValues);
						}
						else
						{
							DefaultMemberAttribute LDefaultMember = GetAttribute(LObject.GetType(), typeof(DefaultMemberAttribute)) as DefaultMemberAttribute;
							if (LDefaultMember == null)
								throw new ArgumentException(String.Format("Member {0} does not have a default property.", LMemberName));

							Type[] LMemberIndexTypes = new Type[LMemberIndexes.Length];
							object[] LMemberIndexValues = new object[LMemberIndexes.Length];
							for (int LMemberIndex = 0; LMemberIndex < LMemberIndexes.Length; LMemberIndex++)
							{
								int LMemberIndexInt;
								if (Int32.TryParse(LMemberIndexes[LMemberIndex], out LMemberIndexInt))
								{
									LMemberIndexTypes[LMemberIndex] = typeof(Int32);
									LMemberIndexValues[LMemberIndex] = LMemberIndexInt;
								}
								else
								{
									LMemberIndexTypes[LMemberIndex] = typeof(String);
									LMemberIndexValues[LMemberIndex] = LMemberIndexes[LMemberIndex];
								}
							}

							PropertyInfo LInfo = LObject.GetType().GetProperty(LDefaultMember.MemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, null, null, LMemberIndexTypes, null);
							if (LInfo == null)
								throw new ArgumentException(String.Format("Could not resolve an indexer for member {0} with the reference list {1}.", LMemberName, LReferenceIndex));
								
							LObject = LInfo.GetValue(LObject, LMemberIndexValues);
						}
					}
					else
					{
						LObject = GetMemberValue(FindSimpleMember(LObject.GetType(), LReferences[LIndex]), LObject);
					}
				}
			}
		
			return LObject;
		}
		
		/// <summary> Internal dictionary of assembly references by weak (short) name. </summary>
		private static Dictionary<string, Assembly> FAssemblyByName;

		/// <summary> Ensures that the internal dictionary is populated with the set of initially loaded assemblies. </summary>
		/// <remarks> Under Silverlight, this call must be made on the main thread. </remarks>
		public static void EnsureAssemblyByName()
		{
			if (FAssemblyByName == null)
			{
				if 
				(
					System.Threading.Interlocked.CompareExchange<Dictionary<string, Assembly>>
					(
						ref FAssemblyByName, 
						new Dictionary<string, Assembly>(),
						null
					) == null
				)
					lock (FAssemblyByName)
					{
						#if SILVERLIGHT
						foreach (var LPart in System.Windows.Deployment.Current.Parts)
						{
							RegisterAssembly
							(
								new System.Windows.AssemblyPart().Load
								(
									System.Windows.Application.GetResourceStream
									(
										new Uri(LPart.Source, UriKind.Relative)
									).Stream
								)
							);
						}
						#else
						foreach (var LAssembly in AppDomain.CurrentDomain.GetAssemblies())
							RegisterAssembly(LAssembly);
						#endif
					}
			}
		}
		
		/// <summary> Attempts to locate an assembly by weak (short) name. </summary>
		public static bool TryGetAssemblyByName(string AName, out Assembly AAssembly)
		{
			EnsureAssemblyByName();
			lock (FAssemblyByName)
				return FAssemblyByName.TryGetValue(AName, out AAssembly);
		}
		
		/// <summary> Registers an assembly for resolution by weak (short) name. </summary>
		public static void RegisterAssembly(Assembly AAssembly)
		{
			EnsureAssemblyByName();
			lock (FAssemblyByName)
				FAssemblyByName.Add(AssemblyNameUtility.GetName(AAssembly.FullName), AAssembly); 
		}

		/// <summary> Creates a new instance using the given name components. </summary>
		/// <remarks> If the given assembly name is weak (short), an attempt will be made 
		/// to find a strong name in the registered assemblies for it. </remarks>
		public static object CreateInstance(string ANamespace, string AClassName, string AAssemblyName)
		{
			return Activator.CreateInstance(GetType(ANamespace, AClassName, AAssemblyName));
		}
		
		/// <summary> Locates a type, using the given assembly if no assembly name is provided. </summary>
		/// <remarks> If an assembly name is given and it is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string AAssemblyQualifiedClassName, Assembly ADefaultAssembly)
		{
			string LAssemblyName;
			string LQualifiedClassName;
			var LIndex = AAssemblyQualifiedClassName.IndexOf(",");
			if (LIndex >= 0)
			{
				LAssemblyName = AAssemblyQualifiedClassName.Substring(LIndex + 1).Trim();
				LQualifiedClassName = AAssemblyQualifiedClassName.Substring(0, LIndex).Trim();
			}
			else
			{
				LAssemblyName = ADefaultAssembly.FullName;
				LQualifiedClassName = AAssemblyQualifiedClassName.Trim();
			}
			
			return GetType(LQualifiedClassName, LAssemblyName);	
		}

		/// <summary> Locates a type, using the calling assembly if no assembly name is provided. </summary>
		/// <remarks> If an assembly name is given and it is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string AAssemblyQualifiedClassName)
		{
			return GetType(AAssemblyQualifiedClassName, Assembly.GetCallingAssembly());	
		}

		/// <summary> Locates a type using the given name components. </summary>
		/// <remarks> If the given assembly name is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string ANamespace, string AClassName, string AAssemblyName)
		{
			return 
				GetType
				(
					(String.IsNullOrEmpty(ANamespace) ? "" : (ANamespace + "."))
						+ AClassName,
					AAssemblyName
				);
		}

		/// <summary> Locates a type using the given name components. </summary>
		/// <remarks> If the given assembly name is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string AQualifiedClassName, string AAssemblyName)
		{
			// Lookup a full assembly name for the given assembly name if there is one
			Assembly LAssembly;
			if (TryGetAssemblyByName(AAssemblyName, out LAssembly))
				AAssemblyName = LAssembly.FullName;

			return 
				Type.GetType
				(
					AQualifiedClassName + (String.IsNullOrEmpty(AAssemblyName) ? "" : ("," + AAssemblyName)), 
					true, 
					true
				);
		}
	}
}