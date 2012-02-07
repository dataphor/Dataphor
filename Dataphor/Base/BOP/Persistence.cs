using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reflection;
using System.ComponentModel;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Base class for serializer and deserializer. </summary>
	public abstract class Persistence
	{
		public const string BOPName = "name";
		public const string BOPNamespaceURI = "www.alphora.com/schemas/bop";
		public const string BOPNamespacePrefix = "bop";
		public const string BOPType = "typeof-";
		public const string BOPDefault = "default-";
		public const string XmlBOPName = "{" + BOPNamespaceURI + "}" + BOPName;
		public const string XmlBOPType = "{" + BOPNamespaceURI + "}" + BOPType;
		public const string XmlBOPDefault = "{" + BOPNamespaceURI + "}" + BOPDefault;

		protected ErrorList _errors = new ErrorList();
		public ErrorList Errors
		{
			get { return _errors; }
		}

		/// <summary> Determines the name of the member that is the default list for the type. </summary>
		protected static string GetDefaultListMemberName(MemberInfo member)
		{
			PublishDefaultListAttribute attribute = (PublishDefaultListAttribute)ReflectionUtility.GetAttribute(member, typeof(PublishDefaultListAttribute));
			if (attribute != null)
				return attribute.MemberName;
			else
				return String.Empty;
		}

		/// <summary> Determines the member name that is designated as the bop:name for the type. </summary>
		protected static string GetNameMemberName(MemberInfo member)
		{
			PublishNameAttribute attribute = (PublishNameAttribute)ReflectionUtility.GetAttribute(member, typeof(PublishNameAttribute));
			if (attribute == null)
				return String.Empty;
			return attribute.MemberName;
		}

		/// <summary> Determines the element text to use when writing the specified type. </summary>
		protected virtual string GetElementName(Type type)
		{
			PublishAsAttribute publishAs = (PublishAsAttribute)ReflectionUtility.GetAttribute(type, typeof(PublishAsAttribute));
			if (publishAs != null)
				return publishAs.ClassName.ToLower();
			return type.Name.ToLower();
		}

		/// <summary> Gets the type of the member from the name/namespace of the XML element. </summary>
		protected static Type GetMemberType(XElement node, MemberInfo member)
		{
			// Determine the type (look for bop:type)
			XAttribute memberTypeAttribute = node.Attribute(XmlBOPType + member.Name.ToLower());
			if (memberTypeAttribute != null)
				return Type.GetType(memberTypeAttribute.Value, true, true);
			else
				return ReflectionUtility.GetMemberType(member);
		}
		
		/// <summary> Performs a case sensitive comparison on the namespace name, and a case insensitive comparison on the local name. </summary>
		public static bool XNamesEqual(XName left, XName right)
		{
			return String.Equals(left.LocalName, right.LocalName, StringComparison.OrdinalIgnoreCase)
				&& String.Equals(left.NamespaceName, right.NamespaceName);
		}

		protected bool IsValueType(Type type)
		{
			return type.IsValueType || (type == typeof(String));
		}
		
		public static TypeConverter GetTypeConverter(Type type)
		{
			// Attempt to find and use a value converter
			var converterAttribute = ((TypeConverterAttribute)ReflectionUtility.GetAttribute(type, typeof(TypeConverterAttribute)));
			if (converterAttribute != null)
			{
				var converterType = Type.GetType(converterAttribute.ConverterTypeName);
				if (converterType != null)
					return (TypeConverter)Activator.CreateInstance(converterType);
			}
			return null;
		}
	}
}
