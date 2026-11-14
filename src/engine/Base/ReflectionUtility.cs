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
		public Token(string name, object value)
		{
			Name = name;
			Value = value;
		}
		
		public string Name;
		public object Value;
	}
	
	public class ReflectionUtility 
	{
		/// <summary> Gets the Type of the property or field member. </summary>
		public static Type GetMemberType(MemberInfo member)
		{
			if (member is PropertyInfo)
				return ((PropertyInfo)member).PropertyType;
			else
				return ((FieldInfo)member).FieldType;
		}

		/// <summary> Sets a property or field member of the specified instance to the specified value. </summary>
		public static void SetMemberValue(MemberInfo member, object instance, object value)
		{
			try
			{
				if (member is PropertyInfo)
					((PropertyInfo)member).SetValue(instance, value, new Object[] {});
				else
					((FieldInfo)member).SetValue(instance, value);
			}
			catch (Exception E)
			{
				throw new BaseException(BaseException.Codes.UnableToSetProperty, E, member.Name);
			}
		}

		/// <summary> Sets the member of the given instance to the given string value. </summary>
		/// <remarks> Sets the member using the converter for the member's type. </remarks>
		public static void SetInstanceMember(object instance, string memberName, string value)
		{
			MemberInfo member = FindSimpleMember(instance.GetType(), memberName);
			SetMemberValue
			(
				member,
				instance,
				StringToValue(value, GetMemberType(member))
			);
		}

		/// <summary> Finds an appropriate field, property, or method member with no parameters. </summary>
		/// <remarks> Throws if a qualifying member is not found. </remarks>
		public static MemberInfo FindSimpleMember(Type type, string name)
		{
			// Find an appropriate member matching the attribute
			MemberInfo[] members = type.GetMember
			(
				name, 
				MemberTypes.Property | MemberTypes.Field | MemberTypes.Method, 
				BindingFlags.IgnoreCase	| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
			);
			MemberInfo result = null;
			foreach (MemberInfo member in members)
				if 
				(
					(member is FieldInfo) || 
					(
						(
							(member is PropertyInfo) && 
							(((PropertyInfo)member).GetIndexParameters().Length == 0)
						) ||
						(
							(member is MethodInfo) &&
							(((MethodInfo)member).GetParameters().Length == 0)
						)
					)
				)
					result = member;
			if (result == null)
				throw new BaseException(BaseException.Codes.MemberNotFound, name, type.FullName);
			return result;
		}
		
		/// <summary> Gets a single attribute for a type based on the attribute class type. </summary>
		/// <remarks> Returns null if not found.  Throws if more than one instance of the specified attribute appears. </remarks>
		public static object GetAttribute(ICustomAttributeProvider provider, Type attributeType)
		{
			if (provider == null)
				return null;
			object[] attributes = provider.GetCustomAttributes(attributeType, true);
			if (attributes.Length > 1)
				throw new BaseException(BaseException.Codes.ExpectingSingleAttribute, attributeType.Name);
			else if (attributes.Length == 1)
				return attributes[0];
			else 
			{
				if (provider is MemberInfo)
					return GetAttribute(((MemberInfo)provider).DeclaringType, attributeType);
				else
					return null;
			}
		}

		/// <summary> Finds a constructor for a type based on the provided signature. </summary>
		/// <remarks> Throws if not found. </remarks>
		public static ConstructorInfo FindConstructor(string signature, Type type)
		{
			// Determine the constructor signature
			string[] signatureNames = signature.Split(new char[] {';'});
			Type[] localSignature = new Type[signatureNames.Length];
			for (int i = localSignature.Length - 1; i >= 0; i--)
				localSignature[i] = GetType(signatureNames[i], type.Assembly);

			// Find the matching constructor
			ConstructorInfo constructor = type.GetConstructor(localSignature);
			if (constructor == null)
				throw new BaseException(BaseException.Codes.DefaultConstructorNotFound, signature);

			return constructor;
		}

		/// <summary> Converts a string value type to the .NET value based on the provided type. </summary>
		public static object StringToValue(string value, Type type)
		{
			#if SILVERLIGHT
			if (value == null)
				throw new BaseException(BaseException.Codes.CannotConvertNull, ErrorSeverity.System);
			if (typeof(Enum).IsAssignableFrom(type))
				return Enum.Parse(type, value, true);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					return Convert.ToBoolean(value);
				case TypeCode.Char:
					return Convert.ToChar(value);
				case TypeCode.SByte:
					return Convert.ToSByte(value);
				case TypeCode.Byte:
					return Convert.ToByte(value);
				case TypeCode.Int16:
					return Convert.ToInt16(value);
				case TypeCode.UInt16:
					return Convert.ToUInt16(value);
				case TypeCode.Int32:
					return Convert.ToInt32(value);
				case TypeCode.UInt32:
					return Convert.ToUInt32(value);
				case TypeCode.Int64:
					return Convert.ToInt64(value);
				case TypeCode.UInt64:
					return Convert.ToUInt64(value);
				case TypeCode.Single:
					return Convert.ToSingle(value);
				case TypeCode.Double:
					return Convert.ToDouble(value);
				case TypeCode.Decimal:
					return Convert.ToDecimal(value);
				case TypeCode.DateTime:
					return Convert.ToDateTime(value);
				case TypeCode.String:
					return value;
				default:
					if (type == typeof(byte[]))
						return Convert.FromBase64String(value);
					else if (type != typeof(object))
					{
						if (type == typeof(TimeSpan))
							return TimeSpan.Parse(value);
						else if (type == typeof(Guid))
							return new Guid(value);
						else if (type == typeof(Uri))
							return new Uri(value);
						else
							break;
					}
					break;
			}
			throw new BaseException(BaseException.Codes.CannotConvertFromString, type.Name);
			#else
			return TypeDescriptor.GetConverter(type).ConvertFromString(value);
			#endif
		}

		/// <summary> Converts a .NET value to a string based on the provided type. </summary>
		public static string ValueToString(object value, Type type)
		{
			#if SILVERLIGHT
			if (value == null)
				throw new BaseException(BaseException.Codes.CannotConvertNull, ErrorSeverity.System);
			switch (Type.GetTypeCode(type))
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
					return value.ToString();

				default:
					if (type == typeof(byte[]))
						return Convert.ToBase64String((byte[])value);
					else if (type != typeof(object))
					{
						if (type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri) || typeof(Enum).IsAssignableFrom(type))
							return value.ToString();
					}
					else if (typeof(IConvertible).IsAssignableFrom(type))
						return ((IConvertible)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
					break;
			}
			throw new BaseException(BaseException.Codes.CannotConvertToString, type.Name);
			#else
			return TypeDescriptor.GetConverter(type).ConvertToString(value);
			#endif
		}

		/// <summary> Gets a field, property, or simple method's value for a particular instance. </summary>
		public static object GetMemberValue(MemberInfo member, object instance)
		{
			if (member is PropertyInfo) 
			{
				if (((PropertyInfo)member).GetIndexParameters().Length != 0)
					return null;
				return ((PropertyInfo)member).GetValue(instance, new Object[] {});
			}
			else if (member is FieldInfo)
				return ((FieldInfo)member).GetValue(instance);
			else
				return ((MethodInfo)member).Invoke(instance, new object[] {});
		}

		public static object ResolveToken(string token, params Token[] tokens)
		{
			for (int index = 0; index < tokens.Length; index++)
				if (tokens[index].Name == token)
					return tokens[index].Value;
					
			throw new ArgumentException(String.Format("Could not resolve token {0}.", token));
		}
		
		public static object ResolveReference(string reference, params Token[] tokens)
		{
			string[] references = reference.Split('.');
			object objectValue = null;
			for (int index = 0; index < references.Length; index++)
			{
				if (index == 0)
					objectValue = ResolveToken(references[index], tokens);
				else
				{
					int bracketIndex = references[index].IndexOf('[');
					if (bracketIndex > 0)
					{
						if (references[index].IndexOf(']') != references[index].Length - 1)
							throw new ArgumentException(String.Format("Indexer reference does not have a closing bracket: {0}", references[index]));
						string memberName = references[index].Substring(0, bracketIndex);
						objectValue = GetMemberValue(FindSimpleMember(objectValue.GetType(), memberName), objectValue);
						
						string referenceIndex = references[index].Substring(bracketIndex + 1, references[index].Length - 1 - (bracketIndex + 1));
						string[] memberIndexes = referenceIndex.Split(',');

						if (objectValue.GetType().IsArray)
						{
							Array array = (Array)objectValue;
							int[] memberIndexValues = new int[memberIndexes.Length];
							for (int memberIndex = 0; memberIndex < memberIndexes.Length; memberIndex++)
								memberIndexValues[memberIndex] = Int32.Parse(memberIndexes[memberIndex]);
								
							objectValue = array.GetValue(memberIndexValues);
						}
						else
						{
							DefaultMemberAttribute defaultMember = GetAttribute(objectValue.GetType(), typeof(DefaultMemberAttribute)) as DefaultMemberAttribute;
							if (defaultMember == null)
								throw new ArgumentException(String.Format("Member {0} does not have a default property.", memberName));

							Type[] memberIndexTypes = new Type[memberIndexes.Length];
							object[] memberIndexValues = new object[memberIndexes.Length];
							for (int memberIndex = 0; memberIndex < memberIndexes.Length; memberIndex++)
							{
								int memberIndexInt;
								if (Int32.TryParse(memberIndexes[memberIndex], out memberIndexInt))
								{
									memberIndexTypes[memberIndex] = typeof(Int32);
									memberIndexValues[memberIndex] = memberIndexInt;
								}
								else
								{
									memberIndexTypes[memberIndex] = typeof(String);
									memberIndexValues[memberIndex] = memberIndexes[memberIndex];
								}
							}

							PropertyInfo info = objectValue.GetType().GetProperty(defaultMember.MemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, null, null, memberIndexTypes, null);
							if (info == null)
								throw new ArgumentException(String.Format("Could not resolve an indexer for member {0} with the reference list {1}.", memberName, referenceIndex));
								
							objectValue = info.GetValue(objectValue, memberIndexValues);
						}
					}
					else
					{
						objectValue = GetMemberValue(FindSimpleMember(objectValue.GetType(), references[index]), objectValue);
					}
				}
			}
		
			return objectValue;
		}
		
		/// <summary> Internal dictionary of assembly references by weak (short) name. </summary>
		private static Dictionary<string, Assembly> _assemblyByName;

		/// <summary> Ensures that the internal dictionary is populated with the set of initially loaded assemblies. </summary>
		/// <remarks> Under Silverlight, this call must be made on the main thread. </remarks>
		public static void EnsureAssemblyByName()
		{
			if (_assemblyByName == null)
			{
				if 
				(
					System.Threading.Interlocked.CompareExchange<Dictionary<string, Assembly>>
					(
						ref _assemblyByName, 
						new Dictionary<string, Assembly>(),
						null
					) == null
				)
					lock (_assemblyByName)
					{
						#if SILVERLIGHT
						foreach (var part in System.Windows.Deployment.Current.Parts)
						{
							RegisterAssembly
							(
								new System.Windows.AssemblyPart().Load
								(
									System.Windows.Application.GetResourceStream
									(
										new Uri(part.Source, UriKind.Relative)
									).Stream
								)
							);
						}
						#else
						foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            if (!_assemblyByName.ContainsKey(AssemblyNameUtility.GetName(assembly.FullName)))
    							RegisterAssembly(assembly);
						#endif
					}
			}
		}
		
		/// <summary> Attempts to locate an assembly by weak (short) name. </summary>
		public static bool TryGetAssemblyByName(string name, out Assembly assembly)
		{
			EnsureAssemblyByName();
			lock (_assemblyByName)
				return _assemblyByName.TryGetValue(name, out assembly);
		}
		
		/// <summary> Registers an assembly for resolution by weak (short) name. </summary>
		public static void RegisterAssembly(Assembly assembly)
		{
			EnsureAssemblyByName();
			lock (_assemblyByName)
				_assemblyByName[AssemblyNameUtility.GetName(assembly.FullName)] = assembly;
		}
		
		public static Assembly[] GetRegisteredAssemblies()
		{
			#if SILVERLIGHT
			EnsureAssemblyByName();
			lock (_assemblyByName)
			{
				var result = new Assembly[_assemblyByName.Count];
				var i = 0;
				foreach (Assembly assembly in _assemblyByName.Values)
				{
					result[i] = assembly;
					i++;
				}
				return result;
			}
			#else
			return AppDomain.CurrentDomain.GetAssemblies();
			#endif
		}

		public static Type SearchAllAssembliesForClass(string qualifiedClassName)
		{
			var assemblies = GetRegisteredAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				var result = assembly.GetType(qualifiedClassName, false);
				if (result != null)
					return result;
			}
			throw new BaseException(BaseException.Codes.ClassNotFound, qualifiedClassName);
		}

		/// <summary> Creates a new instance using the given name components. </summary>
		/// <remarks> If the given assembly name is weak (short), an attempt will be made 
		/// to find a strong name in the registered assemblies for it. </remarks>
		public static object CreateInstance(string namespaceValue, string className, string assemblyName)
		{
			return Activator.CreateInstance(GetType(namespaceValue, className, assemblyName));
		}
		
		/// <summary> Locates a type, using the given assembly if no assembly name is provided. </summary>
		/// <remarks> If an assembly name is given and it is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string assemblyQualifiedClassName, Assembly defaultAssembly)
		{
			string assemblyName;
			string qualifiedClassName;
			var index = assemblyQualifiedClassName.IndexOf(",");
			if (index >= 0)
			{
				assemblyName = assemblyQualifiedClassName.Substring(index + 1).Trim();
				qualifiedClassName = assemblyQualifiedClassName.Substring(0, index).Trim();
			}
			else
			{
				assemblyName = defaultAssembly.FullName;
				qualifiedClassName = assemblyQualifiedClassName.Trim();
				
				Type result = defaultAssembly.GetType(qualifiedClassName, false);
				if (result != null)
					return result;
			}
			
			return GetType(qualifiedClassName, assemblyName);	
		}

		/// <summary> Locates a type, using the calling assembly if no assembly name is provided. </summary>
		/// <remarks> If an assembly name is given and it is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string assemblyQualifiedClassName)
		{
			return GetType(assemblyQualifiedClassName, Assembly.GetCallingAssembly());	
		}

		/// <summary> Locates a type using the given name components. </summary>
		/// <remarks> If the given assembly name is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string namespaceValue, string className, string assemblyName)
		{
			return 
				GetType
				(
					(String.IsNullOrEmpty(namespaceValue) ? "" : (namespaceValue + "."))
						+ className,
					assemblyName
				);
		}

		/// <summary> Locates a type using the given name components. </summary>
		/// <remarks> If the given assembly name is short, an attempt will be made 
		/// to find a full name in the registered assemblies for it. </remarks>
		public static Type GetType(string qualifiedClassName, string assemblyName)
		{
			// Lookup a full assembly name for the given assembly name if there is one
			Assembly assembly;
			if (TryGetAssemblyByName(assemblyName, out assembly))
				assemblyName = assembly.FullName;

			var result =
				Type.GetType
				(
					qualifiedClassName + (String.IsNullOrEmpty(assemblyName) ? "" : ("," + assemblyName)), 
					false, 
					true
				);
			return 
				result != null 
					? result 
					: SearchAllAssembliesForClass(qualifiedClassName);
		}
	}
}