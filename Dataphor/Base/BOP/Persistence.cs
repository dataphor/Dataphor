using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reflection;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Base class for serializer and deserializer. </summary>
	public abstract class Persistence
	{
		public const string CBOPName = "name";
		public const string CBOPNamespaceURI = "www.alphora.com/schemas/bop";
		public const string CBOPNamespacePrefix = "bop";
		public const string CBOPType = "typeof-";
		public const string CBOPDefault = "default-";
		public const string CXmlBOPName = "{" + CBOPNamespaceURI + "}" + CBOPName;
		public const string CXmlBOPType = "{" + CBOPNamespaceURI + "}" + CBOPType;
		public const string CXmlBOPDefault = "{" + CBOPNamespaceURI + "}" + CBOPDefault;

		protected ErrorList FErrors = new ErrorList();
		public ErrorList Errors
		{
			get { return FErrors; }
		}

		/// <summary> Determines the name of the member that is the default list for the type. </summary>
		protected static string GetDefaultListMemberName(MemberInfo AMember)
		{
			PublishDefaultListAttribute LAttribute = (PublishDefaultListAttribute)ReflectionUtility.GetAttribute(AMember, typeof(PublishDefaultListAttribute));
			if (LAttribute != null)
				return LAttribute.MemberName;
			else
				return String.Empty;
		}

		/// <summary> Determines the member name that is designated as the bop:name for the type. </summary>
		protected static string GetNameMemberName(MemberInfo AMember)
		{
			PublishNameAttribute LAttribute = (PublishNameAttribute)ReflectionUtility.GetAttribute(AMember, typeof(PublishNameAttribute));
			if (LAttribute == null)
				return String.Empty;
			return LAttribute.MemberName;
		}

		/// <summary> Determines the element text to use when writing the specified type. </summary>
		protected virtual string GetElementName(Type AType)
		{
			PublishAsAttribute LPublishAs = (PublishAsAttribute)ReflectionUtility.GetAttribute(AType, typeof(PublishAsAttribute));
			if (LPublishAs != null)
				return LPublishAs.ClassName.ToLower();
			return AType.Name.ToLower();
		}

		/// <summary> Gets the type of the member from the name/namespace of the XML element. </summary>
		protected static Type GetMemberType(XElement ANode, MemberInfo AMember)
		{
			// Determine the type (look for bop:type)
			XAttribute LMemberTypeAttribute = ANode.Attribute(CXmlBOPType + AMember.Name.ToLower());
			if (LMemberTypeAttribute != null)
				return Type.GetType(LMemberTypeAttribute.Value, true, true);
			else
				return ReflectionUtility.GetMemberType(AMember);
		}

		protected bool IsValueType(Type AType)
		{
			return AType.IsValueType || (AType == typeof(String));
		}
	}
}
