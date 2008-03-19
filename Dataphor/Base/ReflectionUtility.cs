/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
using System.ComponentModel;

namespace Alphora.Dataphor
{

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
			MemberInfo LMember = ReflectionUtility.FindSimpleMember(AInstance.GetType(), AMemberName);
			SetMemberValue
			(
				LMember,
				AInstance,
				TypeDescriptor.GetConverter(ReflectionUtility.GetMemberType(LMember)).ConvertFromString(AValue)
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
				LSignature[i] = AssemblyUtility.GetType(LSignatureNames[i], true, true);

			// Find the matching constructor
			ConstructorInfo LConstructor = AType.GetConstructor(LSignature);
			if (LConstructor == null)
				throw new BaseException(BaseException.Codes.DefaultConstructorNotFound, ASignature);

			return LConstructor;
		}

		/// <summary> Converts a string value type to the .NET value based on the provided type. </summary>
		public static object StringToValue(string AValue, Type AType)
		{
			return TypeDescriptor.GetConverter(AType).ConvertFromString(AValue);
		}

		/// <summary> Converts a .NET value to a string based on the provided type. </summary>
		public static string ValueToString(object AValue, Type AType)
		{
			return TypeDescriptor.GetConverter(AType).ConvertToString(AValue);
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
	}
}