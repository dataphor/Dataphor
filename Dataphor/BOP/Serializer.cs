/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Text;

namespace Alphora.Dataphor.BOP
{

	#region BOP Attributes

	/// <summary>
	///		Use on a class or struct to identify a List property
	///		as the default parent.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class PublishDefaultListAttribute : Attribute
	{
		public PublishDefaultListAttribute(string AMemberName) : base()
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get { return FMemberName; }
			set { FMemberName = value; }
		}
	}

	/// <summary>
	///		Use on a class or struct to identify the constructor to 
	///		call when deserializing.
	///	</summary>
	///	<remarks>
	///		The constructor's parameters should be labeled with the
	///		<see cref="PublishSourceAttribute"></see> so the values for the
	///		parameters can be determined.
	///	</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	public class PublishDefaultConstructorAttribute : Attribute
	{
		public PublishDefaultConstructorAttribute(string AConstructorSignature) : base()
		{
			FConstructorSignature = AConstructorSignature;
		}

		private string FConstructorSignature;
		public string ConstructorSignature
		{
			get	{ return FConstructorSignature; }
			set	{ FConstructorSignature = value; }
		}
	}

	/// <summary>
	///		Use on parameters of the "default" constructor (as specified by
	///		<see cref="PublishDefaultConstructorAttribute"/>) to identify
	///		which member is associated with this parameter for persistance.
	/// </summary>
	/// <remarks>
	///		The referenced member may be read-only because it will not be
	///		written to.  The referenced member must be flagged with the
	///		PublishAttribute.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public class PublishSourceAttribute : Attribute
	{
		public PublishSourceAttribute(string AMemberName) : base()
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get	{ return FMemberName; }
			set	{ FMemberName = value; }
		}
	}

	/// <summary>
	///		Use on a class or struct to denote the member to use to identify the object
	///		uniquely within it's parent.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class PublishNameAttribute : Attribute
	{
		public PublishNameAttribute(string AMemberName)
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get	{ return FMemberName; }
			set	{ FMemberName = value; }
		}
	}

	/// <summary> Used on a member to identify another member that can be invoked to determine the default for this member. </summary>
	/// <remarks> This attribute cannot be used for properties that are deserialized as arguments for a constructor. </remarks>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
	public class DefaultValueMemberAttribute : Attribute
	{
		public DefaultValueMemberAttribute(string AMemberName)
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get	{ return FMemberName; }
			set	{ FMemberName = value; }
		}
	}

	/// <summary>
	///		Use on a class or struct to denote what the class should be written out as when published.
	///		Usually used to make an object serialize as something that it was derived from.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class PublishAsAttribute : Attribute
	{
		public PublishAsAttribute(string AClassName)
		{
			FClassName = AClassName;
		}

		private string FClassName;
		public string ClassName
		{
			get	{ return FClassName; }
			set	{ FClassName = value; }
		}
	}

	/*
		System.ComponentModel.DefaultValueAttribute -
		
			An member can be made to not persist if it is deemed to be set to it's 
			default value.  If the value of the member is "Equal" to the value
			indicated by this attribute, or vise versa.
	*/

	/// <summary>
	///		Used by <see cref="PublishAttribute"/> to specify the method of	persistance.
	/// </summary>
	/// <remarks>
	///		None - Does no persistence
	///		Value - Persist the value or reference as a single attribute.
	///		Inline - Persist the value or reference as a child.
	///		List - Persist each item in the IList Inline.
	/// </remarks>
	public enum PublishMethod 
	{
		None,
		Value,
		Inline,
		List
	}

	/// <summary>
	///		Use on a value, reference or List property to identify
	///		it for persistance.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
	public class PublishAttribute : Attribute
	{
		public PublishAttribute() : base() {}

		public PublishAttribute(PublishMethod AMethod) : base()
		{
			FMethod = AMethod;
		}

		private PublishMethod FMethod = PublishMethod.Value;
		public PublishMethod Method
		{
			get	{ return FMethod; }
			set { FMethod = value; }
		}
	}

	#endregion

	/// <summary> Events to customize object when serialzed and deserialized. </summary>
	// Specifily implemeted to handle circular containership.
	public interface IBOPSerializationEvents 
	{
		void BeforeSerialize(Serializer ASender); 
		void AfterSerialize(Serializer ASender); 
		void AfterDeserialize(Deserializer ASender);
	}

	public delegate object FindReferenceHandler(string AString);

	public delegate void DeserializedObjectHandler(object AObject); 

	public abstract class Persistence
	{
		public const string CBOPName = "name";
		public const string CBOPNamespaceURI = "www.alphora.com/schemas/bop";
		public const string CBOPType = "typeof-";
		public const string CBOPDefault = "default-";
		public const string CXmlBOPName = "bop:" + CBOPName;
		public const string CXmlBOPType = "bop:" + CBOPType;
		public const string CXmlBOPDefault = "bop:" + CBOPDefault;
		public const string CXmlNamespaceURI = "http://www.w3.org/2000/xmlns/";
		public const string CXmlBOPNamespace = "xmlns:bop";

		private Hashtable FInstances;
		/// <summary> List of written or read instances. </summary>
		protected Hashtable Instances
		{
			get { return FInstances; }
			set { FInstances = value; }
		}

		private ErrorList FErrors;
		public ErrorList Errors
		{
			get { return FErrors; }
		}

		protected void PrepareErrors()
		{
			FErrors = new ErrorList();
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
		protected static Type GetMemberType(XmlNode ANode, MemberInfo AMember)
		{
			// Determine the type (look for bop:type)
			XmlAttribute LMemberTypeAttribute = ANode.Attributes[CXmlBOPType + AMember.Name.ToLower()];
			if (LMemberTypeAttribute != null) 
				return AssemblyUtility.GetType(LMemberTypeAttribute.Value, true, true);
			else
				return ReflectionUtility.GetMemberType(AMember);
		}
	
		protected bool IsValueType(Type AType)
		{
			return AType.IsValueType || (AType == typeof(String));
		}

	}

	/// <summary> Basic Object Persistence (BOP) Serializer. </summary>
	/// <remarks> 
	///		This class is not thread safe.  The Serialize methods should be executed synchronously.
	///	</remarks>
	public class Serializer : Persistence
	{
		/// <summary> Set to false to keep references to objects which will not exist in the serialized XML. </summary>
		public bool RemoveReferencesToObjectsNotSerialized = true;

		/// <summary> Stores an instance of an object as an XML stream. </summary>
		public void Serialize(Stream AStream, object AObject)
		{
			XmlDocument LDocument = new XmlDocument();

			Serialize(LDocument, AObject);

			XmlTextWriter LWriter = new XmlTextWriter(AStream, new UTF8Encoding(false));

			LWriter.Formatting = Formatting.Indented;
			LWriter.Indentation = 4;

			LDocument.WriteTo(LWriter);
			LWriter.Flush();
			
			// Don't call LWriter.Close() or the AStream will be closed and will not acessable after this function is called.
		}
 
		/// <summary> Stores an instance of an object as an XML document. </summary>
		public virtual void Serialize(XmlDocument ADocument, object AObject)
		{
			PrepareErrors();

			BeginFixups();
			try
			{
				// Create root object
				Type LType = AObject.GetType();
				XmlElement LRoot = CreateElement(ADocument, String.Empty, LType);

				// Specify BOP namespace
				AppendAttribute(LRoot, CXmlBOPNamespace, CBOPNamespaceURI, CXmlNamespaceURI);

				WriteMembers(String.Empty, LRoot, AObject, LType);
			}
			finally
			{
				EndFixups();
			}
		}

		private ArrayList FReferences;

		private class Reference
		{
			public Reference(object AInstance, XmlAttribute AAttribute)
			{
				Instance = AInstance;
				Attribute = AAttribute;
			}
			public object Instance;
			public XmlAttribute Attribute;
		}

		protected void BeginFixups()
		{
			Instances = new Hashtable();
			FReferences = new ArrayList();
		}

		protected void EndFixups()
		{
			// Remove all attributes that reference instances that were not written
			if (RemoveReferencesToObjectsNotSerialized)
				foreach (Reference LReference in FReferences)
					if (Instances[LReference.Instance] == null)
						LReference.Attribute.OwnerElement.Attributes.Remove(LReference.Attribute);
			FReferences = null;
			Instances = null;
		}

		/// <summary> Determines the element namespace to use when writing the specified type. </summary>
		protected virtual string GetElementNamespace(Type AType)
		{
			return String.Format("{0},{1}", AType.Namespace, AType.Assembly.GetName().Name);
		}

		/// <summary> Appends an attribute to the specified XML node. </summary>
		private static XmlAttribute AppendAttribute(XmlNode ANode, string AName, string AValue, string ANamespace)
		{
			XmlDocument LDocument = GetDocument(ANode);
			XmlAttribute LNewAttribute = LDocument.CreateAttribute(AName, ANamespace);
			LNewAttribute.Value = AValue;
			ANode.Attributes.Append(LNewAttribute);
			return LNewAttribute;
		}

		/// <summary> Determines if the specified property should be serialized. </summary>
		private bool ShouldPersist(MemberInfo AMember, object AValue, object AInstance)
		{
			DefaultValueAttribute LDefaultValues = (DefaultValueAttribute)ReflectionUtility.GetAttribute(AMember, typeof(DefaultValueAttribute));
			if (LDefaultValues != null)
				return !Object.Equals(AValue, LDefaultValues.Value);

			DefaultValueMemberAttribute LDefaultMember = (DefaultValueMemberAttribute)ReflectionUtility.GetAttribute(AMember, typeof(DefaultValueMemberAttribute));
			if (LDefaultMember != null)
				return 
					!Object.Equals
					(
						AValue,
						ReflectionUtility.GetMemberValue
						(
							ReflectionUtility.FindSimpleMember(AInstance.GetType(), LDefaultMember.MemberName), 
							AInstance
						)
					);
			
			return true;
		}

		/// <summary> Writes a member value to an XML attribute. </summary>
		private void WriteValue(string AName, string ANamespace, XmlNode ANode, MemberInfo AMember, object AValue, object AInstance)
		{
			if (ShouldPersist(AMember, AValue, AInstance))
			{
				bool LIsReference = false;
				Type LType = ReflectionUtility.GetMemberType(AMember);
				string LValue;
				if (IsValueType(LType)) 
					LValue = ReflectionUtility.ValueToString(AValue, LType);
				else if (typeof(Delegate).IsAssignableFrom(LType))
					throw new BOPException(BOPException.Codes.DelegatesNotSupported);		// TODO: Write delegates
				else if (AValue != null)
				{
					LValue = ReferenceToString(AValue);
					LIsReference = true;
				}
				else
					return;
				XmlAttribute LAttribute = 
					AppendAttribute
					(
						ANode, 
						AName, 
						LValue, 
						ANamespace
					);
				if (LIsReference && (Instances[AValue] == null))
					FReferences.Add(new Reference(AValue, LAttribute));		// Add a reference fixup
			}
		}

		/// <summary> Builds a list of names of the specified type's members that are arguments to the constructor. </summary>
		private StringCollection BuildConstructorParamSources(Type AType)
		{
			PublishDefaultConstructorAttribute LConstructorAttribute = (PublishDefaultConstructorAttribute)ReflectionUtility.GetAttribute(AType, typeof(PublishDefaultConstructorAttribute));
			if ((LConstructorAttribute != null) && (LConstructorAttribute.ConstructorSignature != String.Empty)) 
			{
				StringCollection LResult = new StringCollection();
				ParameterInfo[] LParameters = ReflectionUtility.FindConstructor(LConstructorAttribute.ConstructorSignature, AType).GetParameters();
				PublishSourceAttribute LSource;
				foreach (ParameterInfo LParameter in LParameters)
				{
					LSource = (PublishSourceAttribute)ReflectionUtility.GetAttribute(LParameter, typeof(PublishSourceAttribute));
					if ((LSource != null) && (LSource.MemberName != String.Empty))
						LResult.Add(LSource.MemberName);
				}
				return LResult;
			}
			else
				return null;
		}

		/// <summary> Determines what publish method to use for the specified member. </summary>
		private PublishMethod GetPublishMethod(MemberInfo AMember, StringCollection ASources)
		{
			// Check for an explicit publish method
			PublishAttribute LPublishAttribute = (PublishAttribute)ReflectionUtility.GetAttribute(AMember, typeof(PublishAttribute));

			// Figure out the true publish method
			if (LPublishAttribute != null) 
				return LPublishAttribute.Method;
			else
			{
				Type LMemberType = ReflectionUtility.GetMemberType(AMember);
				if (IsValueType(LMemberType))
					if 
					(
						(AMember is PropertyInfo) &&
						(
							(
								!((PropertyInfo)AMember).CanWrite &&
								((ASources == null) || !ASources.Contains(AMember.Name))
							) ||
							((PropertyInfo)AMember).GetIndexParameters().Length > 0
						)
					)																		// read-only or indexed values
						return PublishMethod.None;
					else																	// writable (or constructor source) values
						return PublishMethod.Value;
				else if (typeof(IList).IsAssignableFrom(LMemberType))						// IList implementors
					return PublishMethod.List;
				else if (typeof(Delegate).IsAssignableFrom(LMemberType))
					return PublishMethod.None;
				else if ((AMember is PropertyInfo) && !((PropertyInfo)AMember).CanWrite)	// read-only references
					return PublishMethod.None;
				else																		// writable references
					return PublishMethod.Value;									
			}
		}

		/// <summary> Appends a name qualifier to another name. </summary>
		private string AppendQualifier(string AOriginal, string AQualifier)
		{
			if (AQualifier == String.Empty)
				return AOriginal;
			else
				return String.Format("{0}.{1}", AQualifier, AOriginal);
		}

		private static XmlDocument GetDocument(XmlNode ANode)
		{
			XmlDocument LDocument = ANode.OwnerDocument;
			if (LDocument == null)
				LDocument = (XmlDocument)ANode;
			return LDocument;
		}

		/// <summary> Creates an appends a new XML element. </summary>
		private XmlElement CreateElement(XmlNode AParentNode, string ANameQualifier, Type AType)
		{
			XmlElement LResult = 
				GetDocument(AParentNode).CreateElement
				(
					String.Empty, 
					AppendQualifier(GetElementName(AType), ANameQualifier), 
					GetElementNamespace(AType)
				);
			AParentNode.AppendChild(LResult);
			return LResult;
		}

		/// <summary> Determines an attribute string for the given instance reference. </summary>
		private string ReferenceToString(object AValue)
		{
			if (AValue == null)
				return String.Empty;
			else
			{
				Type LType = AValue.GetType();
				PublishNameAttribute LAttribute = ((PublishNameAttribute)ReflectionUtility.GetAttribute(LType, typeof(PublishNameAttribute)));
				if (LAttribute != null)
					return (string)ReflectionUtility.GetMemberValue(ReflectionUtility.FindSimpleMember(LType, LAttribute.MemberName), AValue);
				else
					return String.Empty;
			}
		}

		/// <summary> Writes all of the members for the given instance to the specified XML node. </summary>
		private void WriteMembers(string ANameQualifier, XmlNode ANode, object AInstance, Type AType)
		{
			if (AInstance is IBOPSerializationEvents)
				((IBOPSerializationEvents)AInstance).BeforeSerialize(this);

			StringCollection LConstructorSources = BuildConstructorParamSources(AType);
			string LNameMemberName = GetNameMemberName(AType);
			string LDefaultListMemberName = GetDefaultListMemberName(AType);
			PublishMethod LPublishMethod;
			object LValue;

			foreach (MemberInfo LMember in AType.FindMembers(MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public, null, null))
			{
				try
				{
					LPublishMethod = GetPublishMethod(LMember, LConstructorSources);

					if (LPublishMethod != PublishMethod.None)
					{
						LValue = ReflectionUtility.GetMemberValue(LMember, AInstance);

						switch (LPublishMethod)
						{
							case PublishMethod.Value :
								string LAttributeName;
								string LAttributeNamespace;
								if		// is the member the name (bop:name)
								(
									(ANameQualifier == String.Empty) && 
									(String.Compare(LNameMemberName, LMember.Name, true) == 0)
								)	
								{
									LAttributeName = CXmlBOPName;
									LAttributeNamespace = CBOPNamespaceURI;
								}
								else
								{
									LAttributeName = AppendQualifier(LMember.Name.ToLower(), ANameQualifier);
									LAttributeNamespace = String.Empty;
								}
								WriteValue(LAttributeName, LAttributeNamespace, ANode, LMember, LValue, AInstance);
								break;
							case PublishMethod.Inline :
								if (LValue != null)
									WriteMembers
									(
										AppendQualifier(LMember.Name.ToLower(), ANameQualifier),
										ANode, 
										LValue,
										LValue.GetType()
//										ReflectionUtility.GetMemberType(LMember)
									);
								break;
							case PublishMethod.List :
								IList LList = (IList)LValue;
								if (LList != null)
								{
									string LNameQualifier = ANameQualifier;
									if		// not the default list
									(
										String.Compare
										(
											LDefaultListMemberName,
											LMember.Name, 
											true
										) != 0
									)
										LNameQualifier = AppendQualifier(LMember.Name.ToLower(), LNameQualifier);

									Type LItemType;
									foreach (object LItem in LList)
									{
										try
										{
											LItemType = LItem.GetType();
											WriteMembers
											(
												String.Empty,
												CreateElement(ANode, LNameQualifier, LItemType),
												LItem,
												LItemType
											);
										}
										catch (Exception LException)
										{
											Errors.Add(LException);
										}
									}
								}
								break;
						}
					}
				}
				catch  (Exception LException)
				{
					Errors.Add(LException);
				}				
			}

			Instances.Add(AInstance, AInstance);

			if (AInstance is IBOPSerializationEvents)
				((IBOPSerializationEvents)AInstance).AfterSerialize(this);
		}
	}

	/// <summary> Basic Object Persistence (BOP) Deserializer. </summary>
	/// <remarks> 
	///		This class is not thread safe.  The Deserialize methods should be executed synchronously.
	///	</remarks>
	public class Deserializer : Persistence
	{
		public event FindReferenceHandler FindReference;
		public event DeserializedObjectHandler AfterDeserialized;
		
		/// <summary> Initializes or creates an instance of an object from a BOP serialized stream. </summary>
		/// <param name="AInstance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public object Deserialize(Stream AStream, object AInstance)
		{
			XmlDocument LDocument = new XmlDocument();
			LDocument.Load(new XmlTextReader(AStream));
			return Deserialize(LDocument, AInstance);
		}

		/// <summary> Initializes or creates an instance of an object from a BOP serialized string. </summary>
		/// <param name="AInstance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public object Deserialize(string AString, object AInstance)
		{
			XmlDocument LDocument = new XmlDocument();
			LDocument.Load(new XmlTextReader(new StringReader(AString)));
			return Deserialize(LDocument, AInstance);
		}

		/// <summary> Initializes or creates an instance of an object from a BOP serialized XML document. </summary>
		/// <param name="AInstance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public virtual object Deserialize(XmlDocument ADocument, object AInstance)
		{
			PrepareErrors();

			BeginFixups();
			try
			{
				return ReadObject(ADocument.DocumentElement, AInstance);
			}
			finally
			{
				EndFixups();
			}
		}

		private ArrayList FFixups;

		private class Fixup
		{
			public Fixup(string AName, object AInstance, MemberInfo AMember)
			{
				Name = AName;
				Instance = AInstance;
				Member = AMember;
			}
			public string Name;
			public object Instance;
			public MemberInfo Member;
		}

		private void BeginFixups()
		{
			FFixups = new ArrayList();
			Instances = new Hashtable(StringComparer.OrdinalIgnoreCase);
		}

		private void EndFixups()
		{
			object LInstance;
			foreach (Fixup LFixup in FFixups)
			{
				try
				{
					LInstance = Instances[LFixup.Name];
					if (LInstance == null)
						Errors.Add(new BOPException(BOPException.Codes.ReferenceNotFound, LFixup.Name));
					else
						ReflectionUtility.SetMemberValue(LFixup.Member, LFixup.Instance, LInstance);
				}
				catch (Exception LException)
				{
					Errors.Add(LException);
				}
			}
			Instances = null;
			FFixups = null;
		}
	
		/// <summary> Constructs a class type from a name and optionally a namespace/assembly. </summary>
		/// <remarks> The namespace may also include an assembly name (after a comma). </remarks>
		protected virtual Type GetClassType(string AName, string ANamespace)
		{
			string LName;
			if (ANamespace == String.Empty)
				LName = AName;
			else
			{
				int LDelimiter = ANamespace.IndexOf(',');	// assembly qualified name if there is a comma
				if (LDelimiter < 0) 
					LName = String.Format("{0}.{1}", ANamespace, AName);
				else
				{
					// Rearrange: <namespace>,<assembly>  -> <namespace>.<classname>,<assembly>
					StringBuilder LTemp = new StringBuilder(ANamespace.Substring(0, LDelimiter).Trim());
					if (LTemp.Length > 0) 
						LTemp.Append('.');
					LTemp.Append(AName);
					LName = ANamespace.Substring(LDelimiter + 1).Trim();
					if (LName != String.Empty) 
					{
						LTemp.Append(',');
						LTemp.Append(LName);
					}
					LName = LTemp.ToString();
				}
			}
			return AssemblyUtility.GetType(LName, true, true);
		}

		private static bool IsBOPNode(XmlNode ANode)
		{
			return String.Compare(ANode.NamespaceURI, CBOPNamespaceURI, true) == 0;
		}

		/// <summary> Resolve an instance reference. </summary>
		/// <remarks> If AInstance or AMember are null than fixups will not be performed if the reference is not found. </remarks>
		private object GetReference(string AName, object AInstance, MemberInfo AMember)
		{
			if (AName == String.Empty)
				return null;

			object LValue = Instances[AName];
			if (LValue != null)
				return LValue;

			if (FindReference != null)
				LValue = FindReference(AName);

			if ((LValue == null) && (AInstance != null) && (AMember != null))
				FFixups.Add(new Fixup(AName, AInstance, AMember));

			return LValue;
		}

		/// <summary> Gets the MemberInfo for the name member of the specified type. </summary>
		private MemberInfo GetNameMemberInfo(Type AType)
		{
			PublishNameAttribute LPublishName = (PublishNameAttribute)ReflectionUtility.GetAttribute(AType, typeof(PublishNameAttribute));
			if (LPublishName == null)
				return null;
			else
				return 
					ReflectionUtility.FindSimpleMember
					(
						AType,
						LPublishName.MemberName
					);
		}

		/// <summary> Reads a property value (value type or reference) from the string. </summary>
		/// <remarks> If AInstance and AMember are null, than fixups will not be performed for references. </remarks>
		private object AttributeToValue(string AValue, Type AType, object AInstance, MemberInfo AMember)
		{
			if (IsValueType(AType)) // value types (strings are effectively a value type)
				return ReflectionUtility.StringToValue(AValue, AType);
			else if (AType.IsSubclassOf(typeof(Delegate)))
				throw new BOPException(BOPException.Codes.DelegatesNotSupported);		// TODO: Read delegates
			else // reference types
				return GetReference(AValue, AInstance, AMember);
		}

		/// <summary> Reads the default value for a type's member. </summary>
		/// <remarks> Throws if no default is provided. </remarks>
		private static object ReadAttributeDefault(MemberInfo AMember, object AInstance)
		{
			DefaultValueAttribute LDefaultValue = (DefaultValueAttribute)ReflectionUtility.GetAttribute(AMember, typeof(DefaultValueAttribute));
			if (LDefaultValue != null)
				return LDefaultValue.Value;

			if (AInstance != null)
			{
				DefaultValueMemberAttribute LDefaultMember = (DefaultValueMemberAttribute)ReflectionUtility.GetAttribute(AMember, typeof(DefaultValueMemberAttribute));
				if (LDefaultMember != null)
					return ReflectionUtility.GetMemberValue(AMember, AInstance);
			}

			throw new BOPException(BOPException.Codes.DefaultNotSpecified, AMember.Name, AMember.DeclaringType.Name);
		}

		/// <summary> Finds the instance for a specific member. </summary>
		/// <remarks> 
		///		Names with dot qualifiers are resolved to their unqualified names by finding 
		///		a by-reference member for each qualifier for each instance.
		///	</remarks>
		///	<param name="LNamePath"> Pass the fully qualified name and get the simple name back. </param>
		///	<returns> The instance containing the member named the simple (unqualified) name. </returns>
		private static object FindMemberInstance(object LInstance, ref string LNamePath)
		{
			int LDotPos;
			while ((LDotPos = LNamePath.IndexOf('.')) >= 0)
			{
				LInstance = ReflectionUtility.GetMemberValue(ReflectionUtility.FindSimpleMember(LInstance.GetType(), LNamePath.Substring(0, LDotPos)), LInstance);
				LNamePath = LNamePath.Substring(LDotPos + 1);
			};
			return LInstance;
		}

		/// <summary> Constructs a new instance given a specified type and signature. </summary>
		/// <remarks> 
		///		Any constructor arguments are loaded from the specified XML node.  
		///		The specified node may be altered by this method.  (clone it first if you do not want it affected)
		///	</remarks>
		private object ConstructInstance(string ASignature, Type AType, XmlNode ANode)
		{
			// Determine the constructor signature
			string[] LSignatureNames = ASignature.Split(new char[] {';'});
			Type[] LSignature = new Type[LSignatureNames.Length];
			for (int i = LSignature.Length - 1; i >= 0; i--)
				LSignature[i] = AssemblyUtility.GetType(LSignatureNames[i], true, true);

			// Find the matching constructor
			ConstructorInfo LConstructor = AType.GetConstructor(LSignature);
			if (LConstructor == null)
				throw new BOPException(BOPException.Codes.DefaultConstructorNotFound, ASignature);

			string LNameMemberName = GetNameMemberName(AType);

			// Build the constructor's parameter list
			ParameterInfo[] LParameters = LConstructor.GetParameters();
			object[] LParameterValues = new object[LParameters.Length];
			PublishSourceAttribute LSource;
			XmlAttribute LAttribute;
			for (int i = LParameters.Length - 1; i >= 0; i--)	// order doesn't matter so step down to avoid re-eval of length - 1
			{
				LSource = (PublishSourceAttribute)ReflectionUtility.GetAttribute(LParameters[i], typeof(PublishSourceAttribute));
				if (LSource == null)
					throw new BOPException(BOPException.Codes.ConstructorArgumentRefNotSpecified, LParameters[i].Name, AType.FullName);

				LAttribute = ANode.Attributes[LSource.MemberName.ToLower()];
				if (LAttribute != null)
				{
					LParameterValues[i] = AttributeToValue
					(
						LAttribute.Value, 
						GetMemberType(ANode, ReflectionUtility.FindSimpleMember(AType, LAttribute.Name)),
						null,
						null
					);

					ANode.Attributes.Remove(LAttribute);	// remove so we don't read the attribute later
				}
				else  // didn't find a regular attribute, so look for a default tag in the xml
				{
					LAttribute = ANode.Attributes[CXmlBOPDefault + LSource.MemberName.ToLower()];
					if (LAttribute != null)
					{
						LParameterValues[i] = ReadAttributeDefault(ReflectionUtility.FindSimpleMember(AType, LAttribute.Name), null);
						ANode.Attributes.Remove(LAttribute);
					}
					else
					{
						// see if the property on the object has a DefaultValueAttribute set
						DefaultValueAttribute LDefault = (DefaultValueAttribute)ReflectionUtility.GetAttribute(ReflectionUtility.FindSimpleMember(AType, LSource.MemberName), typeof(DefaultValueAttribute));
						if (LDefault != null)
							LParameterValues[i] = LDefault.Value;
						else
							throw new BOPException(BOPException.Codes.ConstructorArgumentRefNotFound, LSource.MemberName, AType.FullName);
					}
				}
			}
			return LConstructor.Invoke(LParameterValues);
		}

		/// <summary> Deserializes an instance from a node. </summary>
		/// <param name="ANode"> The XML node.  This node will not be affected by this method. </param>
		/// <param name="AInstance"> An optional instance to deserialize "into". </param>
		/// <returns> The instance that was passed by AInstance or was constructed if null was passed. </returns>
		private object ReadObject(XmlNode ANode, object AInstance)
		{
			string LElementName = ANode.Name.Substring(ANode.Name.LastIndexOf('.') + 1).ToLower();	// simple type name

			Type LType;

			if (AInstance != null)	// instance provided
			{
				LType = AInstance.GetType();
				string LTypeName = GetElementName(LType);	// result is lower case
				if (LElementName != LTypeName)
					Errors.Add(new BOPException(BOPException.Codes.TypeNameMismatch, LElementName, LTypeName));
			}
			else	// construct instance
			{
				LType = GetClassType(LElementName, ANode.NamespaceURI);

				PublishDefaultConstructorAttribute LConstructorAttribute = (PublishDefaultConstructorAttribute)ReflectionUtility.GetAttribute(LType, typeof(PublishDefaultConstructorAttribute));
				try
				{
					if ((LConstructorAttribute == null) || (LConstructorAttribute.ConstructorSignature == String.Empty)) 
						AInstance = Activator.CreateInstance(LType, new object[] {});
					else
					{
						// create a copy of the node to work with
						// so that the original is not corrupted by disappearing attributes
						ANode = ANode.Clone();
						AInstance = ConstructInstance(LConstructorAttribute.ConstructorSignature, LType, ANode);
					}
				}
				catch (Exception E)
				{
					throw new BOPException(BOPException.Codes.UnableToConstruct, E, LType.FullName);
				}
			}

			MemberInfo LMember;
			object LMemberInstance;
			string LMemberName;
			Type LMemberType;
			
			// Have Type and Instance, now read the properties
			try
			{

				// Read attributes
				bool LSetDefault;
				foreach (XmlAttribute LAttribute in ANode.Attributes)
				{
					try
					{
						LMemberName = LAttribute.Name.ToLower();
						if
						(
							(!LMemberName.StartsWith("xml")) 
							&& (!LMemberName.StartsWith(CXmlBOPType))
							&& (!LMemberName.StartsWith(CXmlBOPDefault))
						)
						{
							if (LMemberName.StartsWith(CXmlBOPDefault))
							{
								if (Boolean.Parse(LAttribute.Value))
								{
									LMemberName = LAttribute.Name.Substring(CXmlBOPDefault.Length);
									LSetDefault = true;
								}
								else
									continue;
							}
							else
								LSetDefault = false;
							
							if (!LSetDefault && IsBOPNode(LAttribute))
							{
								LMemberInstance = AInstance;
								if (LMemberName == CXmlBOPName)
								{
									LMember = GetNameMemberInfo(LType);
									if (LMember == null)
										throw new BOPException(BOPException.Codes.InvalidElementName, LType.Name);
								}
								else
									throw new BOPException(BOPException.Codes.InvalidAttribute, LAttribute.Name);
							}
							else
							{
								LMemberInstance = FindMemberInstance(AInstance, ref LMemberName);
								LMember = ReflectionUtility.FindSimpleMember(LMemberInstance.GetType(), LMemberName);
							}
							LMemberType = GetMemberType(ANode, LMember);

							ReflectionUtility.SetMemberValue
							(
								LMember, 
								LMemberInstance, 
								(
									LSetDefault ? 
									ReadAttributeDefault(LMember, LMemberInstance) : 
									AttributeToValue
									(
										LAttribute.Value, 
										LMemberType,
										LMemberInstance,
										LMember
									)
								)
							);
						}
					}
					catch (Exception LException)
					{
						Errors.Add(LException);
					}
				}

				// Add this instance to the list of read instances if it has a name
				LMember = GetNameMemberInfo(AInstance.GetType());
				if (LMember != null)
				{
					LMemberName = (string)ReflectionUtility.GetMemberValue(LMember, AInstance);
					if (LMemberName != String.Empty)
						Instances.Add(LMemberName, AInstance);
				}
			
				// read child nodes
				IList LList;
				foreach (XmlNode LNode in ANode.ChildNodes)
				{
					try
					{
						if (LNode is XmlElement)	// Ignore comments, etc.
						{
							LMemberName = LNode.LocalName.ToLower();
							LMemberInstance = FindMemberInstance(AInstance, ref LMemberName);

							// First see if the member instance has a default list attribute and use it if it does
							string LDefaultListName = GetDefaultListMemberName(LMemberInstance.GetType());
							if (LDefaultListName == String.Empty)
								LList = LMemberInstance as IList;
							else // if no default list, assume the member instance IS the list
								LList = ReflectionUtility.GetMemberValue
								(
									ReflectionUtility.FindSimpleMember(LMemberInstance.GetType(), LDefaultListName), 
									LMemberInstance
								) as IList;
							if (LList == null)
								throw new BOPException(BOPException.Codes.DefaultListNotFound, LMemberInstance.GetType().Name);
							LList.Add(ReadObject(LNode, null));
						}
					}
					catch (Exception LException)
					{
						Errors.Add(LException);
					}
				}

				// call AfterDeserialize
				if (AInstance is IBOPSerializationEvents)
					((IBOPSerializationEvents)AInstance).AfterDeserialize(this);

				if (AfterDeserialized != null)
					AfterDeserialized(AInstance);
			}
			catch
			{
				if ((AInstance != null) && (AInstance is IDisposable))
					((IDisposable)AInstance).Dispose();
				throw;
			}

			return AInstance;
		}
	}
}
