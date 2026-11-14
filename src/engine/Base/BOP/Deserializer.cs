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
		/// <param name="instance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public object Deserialize(Stream stream, object instance)
		{
			return Deserialize(XDocument.Load(XmlReader.Create(stream)), instance);
		}

		/// <summary> Initializes or creates an instance of an object from a BOP serialized string. </summary>
		/// <param name="instance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public object Deserialize(string stringValue, object instance)
		{
			return Deserialize(XDocument.Load(new StringReader(stringValue)), instance);
		}

		/// <summary> Initializes or creates an instance of an object from a BOP serialized XML document. </summary>
		/// <param name="instance"> Instance to initialize.  If null, a new instance is created. </param>
		/// <returns> A new object instance, or the object passed as AInstance </returns>
		public virtual object Deserialize(XDocument document, object instance)
		{
			Errors.Clear();

			BeginFixups();
			try
			{
				return ReadObject(document.Root, instance);
			}
			finally
			{
				EndFixups();
			}
		}

		protected Dictionary<string, object> _instancesByName;
		public Dictionary<string, object> InstancesByName
		{
			get { return _instancesByName; }
		}

		private List<Fixup> _fixups;

		private class Fixup
		{
			public Fixup(string name, object instance, MemberInfo member)
			{
				Name = name;
				Instance = instance;
				Member = member;
			}
			public string Name;
			public object Instance;
			public MemberInfo Member;
		}

		private void BeginFixups()
		{
			_fixups = new List<Fixup>();
			_instancesByName = new Dictionary<string, object>();
		}

		private void EndFixups()
		{
			foreach (Fixup fixup in _fixups)
			{
				try
				{
					object instance;
					if (!InstancesByName.TryGetValue(fixup.Name, out instance))
						Errors.Add(new BOPException(BOPException.Codes.ReferenceNotFound, fixup.Name));
					else
						ReflectionUtility.SetMemberValue(fixup.Member, fixup.Instance, instance);
				}
				catch (Exception exception)
				{
					Errors.Add(exception);
				}
			}
			_fixups = null;
			_instancesByName = null;
		}

		/// <summary> Constructs a class type from a name and optionally a namespace/assembly. </summary>
		/// <remarks> The namespace may also include an assembly name (after a comma). </remarks>
		protected virtual Type GetClassType(string className, string namespaceValue)
		{
			string assemblyName = "";
			if (namespaceValue != String.Empty)
			{
				int delimiter = namespaceValue.IndexOf(',');	// assembly qualified name if there is a comma
				if (delimiter < 0)
					assemblyName = "";
				else
				{
					assemblyName = namespaceValue.Substring(delimiter + 1).Trim();
					namespaceValue = namespaceValue.Substring(0, delimiter).Trim();
				}
			}
			return ReflectionUtility.GetType(namespaceValue, className, assemblyName);
		}

		private static bool IsBOPNode(XName name)
		{
			return String.Equals(name.NamespaceName, BOPNamespaceURI);
		}

		/// <summary> Resolve an instance reference. </summary>
		/// <remarks> If AInstance or AMember are null than fixups will not be performed if the reference is not found. </remarks>
		private object GetReference(string name, object instance, MemberInfo member)
		{
			if (name == String.Empty)
				return null;

			object tempValue;
			if (InstancesByName.TryGetValue(name, out tempValue))
				return tempValue;

			if (FindReference != null)
				tempValue = FindReference(name);

			if ((tempValue == null) && (instance != null) && (member != null))
				_fixups.Add(new Fixup(name, instance, member));

			return tempValue;
		}

		/// <summary> Gets the MemberInfo for the name member of the specified type. </summary>
		private MemberInfo GetNameMemberInfo(Type type)
		{
			PublishNameAttribute publishName = (PublishNameAttribute)ReflectionUtility.GetAttribute(type, typeof(PublishNameAttribute));
			if (publishName == null)
				return null;
			else
				return ReflectionUtility.FindSimpleMember(type, publishName.MemberName);
		}

		/// <summary> Reads a property value (value type or reference) from the string. </summary>
		/// <remarks> If AInstance and AMember are null, than fixups will not be performed for references. </remarks>
		private object AttributeToValue(string tempValue, Type type, object instance, MemberInfo member)
		{
			if (IsValueType(type)) // value types (strings are effectively a value type)
				return ReflectionUtility.StringToValue(tempValue, type);
			else if (type.IsSubclassOf(typeof(Delegate)))
				throw new BOPException(BOPException.Codes.DelegatesNotSupported);		// TODO: Read delegates
			else
			{
				 // reference type, try a type converter, then assume it is a reference to another object in the tree
				// Attempt to load the reference using a value converter, maybe it's not a name but a converted value
				var converter = Persistence.GetTypeConverter(type);
				if (converter != null)
					return converter.ConvertFromString(tempValue);
				else
					return GetReference(tempValue, instance, member);
			}
		}

		/// <summary> Reads the default value for a type's member. </summary>
		/// <remarks> Throws if no default is provided. </remarks>
		private static object ReadAttributeDefault(MemberInfo member, object instance)
		{
			DefaultValueAttribute defaultValue = (DefaultValueAttribute)ReflectionUtility.GetAttribute(member, typeof(DefaultValueAttribute));
			if (defaultValue != null)
				return defaultValue.Value;

			if (instance != null)
			{
				DefaultValueMemberAttribute defaultMember = (DefaultValueMemberAttribute)ReflectionUtility.GetAttribute(member, typeof(DefaultValueMemberAttribute));
				if (defaultMember != null)
					return ReflectionUtility.GetMemberValue(member, instance);
			}

			throw new BOPException(BOPException.Codes.DefaultNotSpecified, member.Name, member.DeclaringType.Name);
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
			int dotPos;
			while ((dotPos = LNamePath.IndexOf('.')) >= 0)
			{
				LInstance = ReflectionUtility.GetMemberValue(ReflectionUtility.FindSimpleMember(LInstance.GetType(), LNamePath.Substring(0, dotPos)), LInstance);
				LNamePath = LNamePath.Substring(dotPos + 1);
			};
			return LInstance;
		}

		/// <summary> Constructs a new instance given a specified type and signature. </summary>
		/// <remarks> 
		///		Any constructor arguments are loaded from the specified XML node.  
		///		The specified node may be altered by this method.  (clone it first if you do not want it affected)
		///	</remarks>
		private object ConstructInstance(string signature, Type type, XElement node)
		{
			// Determine the constructor signature
			string[] signatureNames = signature.Split(new char[] { ';' });
			Type[] localSignature = new Type[signatureNames.Length];
			for (int i = localSignature.Length - 1; i >= 0; i--)
				localSignature[i] = ReflectionUtility.GetType(signatureNames[i], type.Assembly);

			// Find the matching constructor
			ConstructorInfo constructor = type.GetConstructor(localSignature);
			if (constructor == null)
				throw new BOPException(BOPException.Codes.DefaultConstructorNotFound, signature);

			string nameMemberName = GetNameMemberName(type);

			// Build the constructor's parameter list
			ParameterInfo[] parameters = constructor.GetParameters();
			object[] parameterValues = new object[parameters.Length];
			PublishSourceAttribute source;
			for (int i = parameters.Length - 1; i >= 0; i--)	// order doesn't matter so step down to avoid re-eval of length - 1
			{
				source = (PublishSourceAttribute)ReflectionUtility.GetAttribute(parameters[i], typeof(PublishSourceAttribute));
				if (source == null)
					throw new BOPException(BOPException.Codes.ConstructorArgumentRefNotSpecified, parameters[i].Name, type.FullName);

				var memberName = source.MemberName.ToLower();
				XAttribute attribute = node.Attribute(memberName);
				if (attribute != null)
				{
					parameterValues[i] = AttributeToValue
					(
						attribute.Value,
						GetMemberType(node, ReflectionUtility.FindSimpleMember(type, memberName)),
						null,
						null
					);

					attribute.Remove();	// remove so we don't read the attribute later
				}
				else  // didn't find a regular attribute, so look for a default tag in the xml
				{
					attribute = node.Attribute(XmlBOPDefault + source.MemberName.ToLower());
					if (attribute != null)
					{
						parameterValues[i] = ReadAttributeDefault(ReflectionUtility.FindSimpleMember(type, memberName), null);
						attribute.Remove();
					}
					else
					{
						// see if the property on the object has a DefaultValueAttribute set
						DefaultValueAttribute defaultValue = (DefaultValueAttribute)ReflectionUtility.GetAttribute(ReflectionUtility.FindSimpleMember(type, source.MemberName), typeof(DefaultValueAttribute));
						if (defaultValue != null)
							parameterValues[i] = defaultValue.Value;
						else
							throw new BOPException(BOPException.Codes.ConstructorArgumentRefNotFound, source.MemberName, type.FullName);
					}
				}
			}
			return constructor.Invoke(parameterValues);
		}

		/// <summary> Deserializes an instance from a node. </summary>
		/// <param name="node"> The XML node.  This node will not be affected by this method. </param>
		/// <param name="instance"> An optional instance to deserialize "into". </param>
		/// <returns> The instance that was passed by AInstance or was constructed if null was passed. </returns>
		private object ReadObject(XElement node, object instance)
		{
			string elementName = node.Name.LocalName.Substring(node.Name.LocalName.LastIndexOf('.') + 1).ToLower();	// simple type name

			Type type;

			if (instance != null)	// instance provided
			{
				type = instance.GetType();
				string typeName = GetElementName(type);	// result is lower case
				if (elementName != typeName)
					Errors.Add(new BOPException(BOPException.Codes.TypeNameMismatch, elementName, typeName));
			}
			else	// construct instance
			{
				type = GetClassType(elementName, node.Name.NamespaceName);
				
				try
				{
					if (!IsValueType(type))
					{
						PublishDefaultConstructorAttribute constructorAttribute = (PublishDefaultConstructorAttribute)ReflectionUtility.GetAttribute(type, typeof(PublishDefaultConstructorAttribute));
						if ((constructorAttribute == null) || (constructorAttribute.ConstructorSignature == String.Empty))
							instance = Activator.CreateInstance(type, new object[] { });
						else
						{
							// create a copy of the node to work with
							// so that the original is not corrupted by disappearing attributes
							node = new XElement(node);
							instance = ConstructInstance(constructorAttribute.ConstructorSignature, type, node);
						}
					}
					else
					{
						return ReflectionUtility.StringToValue(node.Attribute("value").Value, type);
					}
				}
				catch (Exception E)
				{
					throw new BOPException(BOPException.Codes.UnableToConstruct, E, type.FullName);
				}
			}

			MemberInfo member;
			object memberInstance;
			string memberName;
			Type memberType;

			// Have Type and Instance, now read the properties
			try
			{

				// Read attributes
				foreach (XAttribute attribute in node.Attributes())
				{
					try
					{
						memberName = attribute.Name.LocalName.ToLower();
						if
						(
							(!attribute.IsNamespaceDeclaration)
								&& 
								!(
									IsBOPNode(attribute.Name) 
										&& (memberName.StartsWith(BOPType) || memberName.StartsWith(BOPDefault))
								)
						)
						{
							if (IsBOPNode(attribute.Name))
							{
								memberInstance = instance;
								if (Persistence.XNamesEqual(attribute.Name, XmlBOPName))
								{
									member = GetNameMemberInfo(type);
									if (member == null)
										throw new BOPException(BOPException.Codes.InvalidElementName, type.Name);
								}
								else
									throw new BOPException(BOPException.Codes.InvalidAttribute, attribute.Name);
							}
							else
							{
								memberInstance = FindMemberInstance(instance, ref memberName);
								member = ReflectionUtility.FindSimpleMember(memberInstance.GetType(), memberName);
							}
							memberType = GetMemberType(node, member);

							ReflectionUtility.SetMemberValue
							(
								member,
								memberInstance,
								(
									AttributeToValue
									(
										attribute.Value,
										memberType,
										memberInstance,
										member
									)
								)
							);
						}
					}
					catch (Exception exception)
					{
						Errors.Add(exception);
					}
				}

				// Add this instance to the list of read instances if it has a name
				member = GetNameMemberInfo(instance.GetType());
				if (member != null)
				{
					memberName = (string)ReflectionUtility.GetMemberValue(member, instance);
					if (memberName != String.Empty)
						InstancesByName.Add(memberName, instance);
				}

				// read child nodes
				IList list;
				foreach (XElement localNode in node.Elements())
				{
					try
					{
						memberName = localNode.Name.LocalName.ToLower();
						memberInstance = FindMemberInstance(instance, ref memberName);

						// First see if the member instance has a default list attribute and use it if it does
						string defaultListName = GetDefaultListMemberName(memberInstance.GetType());
						if (defaultListName == String.Empty)
							list = memberInstance as IList;
						else // if no default list, assume the member instance IS the list
							list = ReflectionUtility.GetMemberValue
							(
								ReflectionUtility.FindSimpleMember(memberInstance.GetType(), defaultListName),
								memberInstance
							) as IList;
						if (list == null)
							throw new BOPException(BOPException.Codes.DefaultListNotFound, memberInstance.GetType().Name);
						list.Add(ReadObject(localNode, null));
					}
					catch (Exception exception)
					{
						Errors.Add(exception);
					}
				}

				// call AfterDeserialize
				if (instance is IBOPSerializationEvents)
					((IBOPSerializationEvents)instance).AfterDeserialize(this);

				if (AfterDeserialized != null)
					AfterDeserialized(instance);
			}
			catch
			{
				if ((instance != null) && (instance is IDisposable))
					((IDisposable)instance).Dispose();
				throw;
			}

			return instance;
		}
	}
}
