using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Xml;

namespace Alphora.Dataphor.BOP
{
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
			return Deserialize(XDocument.Load(XmlReader.Create(AStream)), AInstance);
		}

		/// <summary> Initializes or creates an instance of an object from a BOP serialized string. </summary>
		/// <param name="AInstance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public object Deserialize(string AString, object AInstance)
		{
			return Deserialize(XDocument.Load(new StringReader(AString)), AInstance);
		}

		/// <summary> Initializes or creates an instance of an object from a BOP serialized XML document. </summary>
		/// <param name="AInstance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public virtual object Deserialize(XDocument ADocument, object AInstance)
		{
			Errors.Clear();

			BeginFixups();
			try
			{
				return ReadObject(ADocument.Root, AInstance);
			}
			finally
			{
				EndFixups();
			}
		}

		protected Dictionary<string, object> FInstancesByName;
		public Dictionary<string, object> InstancesByName
		{
			get { return FInstancesByName; }
		}

		private List<Fixup> FFixups;

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
			FFixups = new List<Fixup>();
			FInstancesByName = new Dictionary<string, object>();
		}

		private void EndFixups()
		{
			foreach (Fixup LFixup in FFixups)
			{
				try
				{
					object LInstance;
					if (!InstancesByName.TryGetValue(LFixup.Name, out LInstance))
						Errors.Add(new BOPException(BOPException.Codes.ReferenceNotFound, LFixup.Name));
					else
						ReflectionUtility.SetMemberValue(LFixup.Member, LFixup.Instance, LInstance);
				}
				catch (Exception LException)
				{
					Errors.Add(LException);
				}
			}
			FFixups = null;
			FInstancesByName = null;
		}

		/// <summary> Constructs a class type from a name and optionally a namespace/assembly. </summary>
		/// <remarks> The namespace may also include an assembly name (after a comma). </remarks>
		protected virtual Type GetClassType(string AClassName, string ANamespace)
		{
			string LAssemblyName = "";
			if (ANamespace != String.Empty)
			{
				int LDelimiter = ANamespace.IndexOf(',');	// assembly qualified name if there is a comma
				if (LDelimiter < 0)
					LAssemblyName = "";
				else
				{
					LAssemblyName = ANamespace.Substring(LDelimiter + 1).Trim();
					ANamespace = ANamespace.Substring(0, LDelimiter).Trim();
				}
			}
			return ReflectionUtility.GetType(ANamespace, AClassName, LAssemblyName);
		}

		private static bool IsBOPNode(XName AName)
		{
			return String.Equals(AName.NamespaceName, CBOPNamespaceURI);
		}

		/// <summary> Resolve an instance reference. </summary>
		/// <remarks> If AInstance or AMember are null than fixups will not be performed if the reference is not found. </remarks>
		private object GetReference(string AName, object AInstance, MemberInfo AMember)
		{
			if (AName == String.Empty)
				return null;

			object LValue;
			if (InstancesByName.TryGetValue(AName, out LValue))
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
				return ReflectionUtility.FindSimpleMember(AType, LPublishName.MemberName);
		}

		/// <summary> Reads a property value (value type or reference) from the string. </summary>
		/// <remarks> If AInstance and AMember are null, than fixups will not be performed for references. </remarks>
		private object AttributeToValue(string AValue, Type AType, object AInstance, MemberInfo AMember)
		{
			if (IsValueType(AType)) // value types (strings are effectively a value type)
				return ReflectionUtility.StringToValue(AValue, AType);
			else if (AType.IsSubclassOf(typeof(Delegate)))
				throw new BOPException(BOPException.Codes.DelegatesNotSupported);		// TODO: Read delegates
			else
			{
				 // reference type, try a type converter, then assume it is a reference to another object in the tree
				// Attempt to load the reference using a value converter, maybe it's not a name but a converted value
				var LConverter = Persistence.GetTypeConverter(AType);
				if (LConverter != null)
					return LConverter.ConvertFromString(AValue);
				else
					return GetReference(AValue, AInstance, AMember);
			}
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
		private object ConstructInstance(string ASignature, Type AType, XElement ANode)
		{
			// Determine the constructor signature
			string[] LSignatureNames = ASignature.Split(new char[] { ';' });
			Type[] LSignature = new Type[LSignatureNames.Length];
			for (int i = LSignature.Length - 1; i >= 0; i--)
				LSignature[i] = ReflectionUtility.GetType(LSignatureNames[i], AType.Assembly);

			// Find the matching constructor
			ConstructorInfo LConstructor = AType.GetConstructor(LSignature);
			if (LConstructor == null)
				throw new BOPException(BOPException.Codes.DefaultConstructorNotFound, ASignature);

			string LNameMemberName = GetNameMemberName(AType);

			// Build the constructor's parameter list
			ParameterInfo[] LParameters = LConstructor.GetParameters();
			object[] LParameterValues = new object[LParameters.Length];
			PublishSourceAttribute LSource;
			for (int i = LParameters.Length - 1; i >= 0; i--)	// order doesn't matter so step down to avoid re-eval of length - 1
			{
				LSource = (PublishSourceAttribute)ReflectionUtility.GetAttribute(LParameters[i], typeof(PublishSourceAttribute));
				if (LSource == null)
					throw new BOPException(BOPException.Codes.ConstructorArgumentRefNotSpecified, LParameters[i].Name, AType.FullName);

				var LMemberName = LSource.MemberName.ToLower();
				XAttribute LAttribute = ANode.Attribute(LMemberName);
				if (LAttribute != null)
				{
					LParameterValues[i] = AttributeToValue
					(
						LAttribute.Value,
						GetMemberType(ANode, ReflectionUtility.FindSimpleMember(AType, LMemberName)),
						null,
						null
					);

					LAttribute.Remove();	// remove so we don't read the attribute later
				}
				else  // didn't find a regular attribute, so look for a default tag in the xml
				{
					LAttribute = ANode.Attribute(CXmlBOPDefault + LSource.MemberName.ToLower());
					if (LAttribute != null)
					{
						LParameterValues[i] = ReadAttributeDefault(ReflectionUtility.FindSimpleMember(AType, LMemberName), null);
						LAttribute.Remove();
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
		private object ReadObject(XElement ANode, object AInstance)
		{
			string LElementName = ANode.Name.LocalName.Substring(ANode.Name.LocalName.LastIndexOf('.') + 1).ToLower();	// simple type name

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
				LType = GetClassType(LElementName, ANode.Name.NamespaceName);
				
				try
				{
					if (!IsValueType(LType))
					{
						PublishDefaultConstructorAttribute LConstructorAttribute = (PublishDefaultConstructorAttribute)ReflectionUtility.GetAttribute(LType, typeof(PublishDefaultConstructorAttribute));
						if ((LConstructorAttribute == null) || (LConstructorAttribute.ConstructorSignature == String.Empty))
							AInstance = Activator.CreateInstance(LType, new object[] { });
						else
						{
							// create a copy of the node to work with
							// so that the original is not corrupted by disappearing attributes
							ANode = new XElement(ANode);
							AInstance = ConstructInstance(LConstructorAttribute.ConstructorSignature, LType, ANode);
						}
					}
					else
					{
						return ReflectionUtility.StringToValue(ANode.Attribute("value").Value, LType);
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
				foreach (XAttribute LAttribute in ANode.Attributes())
				{
					try
					{
						LMemberName = LAttribute.Name.LocalName.ToLower();
						if
						(
							(!LAttribute.IsNamespaceDeclaration)
								&& 
								!(
									IsBOPNode(LAttribute.Name) 
										&& (LMemberName.StartsWith(CBOPType) || LMemberName.StartsWith(CBOPDefault))
								)
						)
						{
							if (IsBOPNode(LAttribute.Name))
							{
								LMemberInstance = AInstance;
								if (Persistence.XNamesEqual(LAttribute.Name, CXmlBOPName))
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
						InstancesByName.Add(LMemberName, AInstance);
				}

				// read child nodes
				IList LList;
				foreach (XElement LNode in ANode.Elements())
				{
					try
					{
						LMemberName = LNode.Name.LocalName.ToLower();
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
