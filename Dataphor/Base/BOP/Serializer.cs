/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Xml;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Basic Object Persistence (BOP) Serializer. </summary>
	/// <remarks> 
	///		This class is not thread safe.  The Serialize methods should be executed synchronously.
	///	</remarks>
	public class Serializer : Persistence
	{
		/// <summary> Set to false to keep references to objects which will not exist in the serialized XML. </summary>
		public bool RemoveReferencesToObjectsNotSerialized = true;

		/// <summary> Stores an instance of an object as an XML stream. </summary>
		public void Serialize(Stream stream, object objectValue)
		{
			XDocument document = new XDocument();

			Serialize(document, objectValue);

			XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, IndentChars = "\t" });

			document.WriteTo(writer);
			writer.Flush();
			
			// Don't call LWriter.Close() or the AStream will be closed and will not acessable after this function is called.
		}
 
		/// <summary> Stores an instance of an object as an XML document. </summary>
		public virtual void Serialize(XDocument document, object objectValue)
		{
			Errors.Clear();

			BeginFixups();
			try
			{
				// Create root object
				Type type = objectValue.GetType();
				XElement root = CreateElement(document, String.Empty, type);

				// Specify BOP namespace
				root.SetAttributeValue(XNamespace.Xmlns + BOPNamespacePrefix, BOPNamespaceURI);

				WriteMembers(String.Empty, root, objectValue, type);
			}
			finally
			{
				EndFixups();
			}
		}

		protected Set<object> _instances;
		public Set<object> Instances
		{
			get { return _instances; }
		}

		private List<Reference> _references;

		private class Reference
		{
			public Reference(object instance, XAttribute attribute)
			{
				Instance = instance;
				Attribute = attribute;
			}
			public object Instance;
			public XAttribute Attribute;
		}

		private void BeginFixups()
		{
			_instances = new Set<object>();
			_references = new List<Reference>();
		}

		private void EndFixups()
		{
			// Remove all attributes that reference instances that were not written
			if (RemoveReferencesToObjectsNotSerialized)
				foreach (Reference reference in _references)
					if (!Instances.Contains(reference.Instance))
						reference.Attribute.Remove();
			_references = null;
			_instances = null;
		}

		/// <summary> Determines the element namespace to use when writing the specified type. </summary>
		protected virtual string GetElementNamespace(Type type)
		{
			return String.Format("{0},{1}", type.Namespace, AssemblyNameUtility.GetName(type.Assembly.FullName));
		}

		/// <summary> Determines if the specified property should be serialized. </summary>
		private bool ShouldPersist(MemberInfo member, object tempValue, object instance)
		{
			DefaultValueAttribute defaultValues = (DefaultValueAttribute)ReflectionUtility.GetAttribute(member, typeof(DefaultValueAttribute));
			if (defaultValues != null)
				return !Object.Equals(tempValue, defaultValues.Value);

			DefaultValueMemberAttribute defaultMember = (DefaultValueMemberAttribute)ReflectionUtility.GetAttribute(member, typeof(DefaultValueMemberAttribute));
			if (defaultMember != null)
				return 
					!Object.Equals
					(
						tempValue,
						ReflectionUtility.GetMemberValue
						(
							ReflectionUtility.FindSimpleMember(instance.GetType(), defaultMember.MemberName), 
							instance
						)
					);
			
			return true;
		}

		/// <summary> Writes a member value to an XML attribute. </summary>
		private void WriteValue(XName name, XElement node, MemberInfo member, object tempValue, object instance)
		{
			if (ShouldPersist(member, tempValue, instance))
			{
				bool isReference = false;
				Type type = ReflectionUtility.GetMemberType(member);

				string localTempValue;
				if (IsValueType(type)) 
					localTempValue = ReflectionUtility.ValueToString(tempValue, type);
				else if (typeof(Delegate).IsAssignableFrom(type))
					throw new BOPException(BOPException.Codes.DelegatesNotSupported);		// TODO: Write delegates
				else if (tempValue != null)
					localTempValue = ReferenceToString(tempValue, out isReference);
				else
					return;
				
				XAttribute attribute = new XAttribute(name, localTempValue);
				node.Add(attribute);

				// Add a reference fixup if needed
				if (isReference && !Instances.Contains(tempValue))
					_references.Add(new Reference(tempValue, attribute));		
			}
		}

		/// <summary> Builds a list of names of the specified type's members that are arguments to the constructor. </summary>
		private List<string> BuildConstructorParamSources(Type type)
		{
			PublishDefaultConstructorAttribute constructorAttribute = (PublishDefaultConstructorAttribute)ReflectionUtility.GetAttribute(type, typeof(PublishDefaultConstructorAttribute));
			if ((constructorAttribute != null) && (constructorAttribute.ConstructorSignature != String.Empty)) 
			{
				List<string> result = new List<string>();
				ParameterInfo[] parameters = ReflectionUtility.FindConstructor(constructorAttribute.ConstructorSignature, type).GetParameters();
				PublishSourceAttribute source;
				foreach (ParameterInfo parameter in parameters)
				{
					source = (PublishSourceAttribute)ReflectionUtility.GetAttribute(parameter, typeof(PublishSourceAttribute));
					if ((source != null) && (source.MemberName != String.Empty))
						result.Add(source.MemberName);
				}
				return result;
			}
			else
				return null;
		}

		/// <summary> Determines what publish method to use for the specified member. </summary>
		private PublishMethod GetPublishMethod(MemberInfo member, List<string> sources)
		{
			// Check for an explicit publish method
			PublishAttribute publishAttribute = (PublishAttribute)ReflectionUtility.GetAttribute(member, typeof(PublishAttribute));

			// Figure out the true publish method
			if (publishAttribute != null) 
				return publishAttribute.Method;
			else
			{
				Type memberType = ReflectionUtility.GetMemberType(member);
				if (IsValueType(memberType))
					if 
					(
						(member is PropertyInfo) &&
						(
							(
								!((PropertyInfo)member).CanWrite &&
								((sources == null) || !sources.Contains(member.Name))
							) ||
							((PropertyInfo)member).GetIndexParameters().Length > 0
						)
					)																		// read-only or indexed values
						return PublishMethod.None;
					else																	// writable (or constructor source) values
						return PublishMethod.Value;
				else if (typeof(IList).IsAssignableFrom(memberType))						// IList implementors
					return PublishMethod.List;
				else if (typeof(Delegate).IsAssignableFrom(memberType))
					return PublishMethod.None;
				else if ((member is PropertyInfo) && !((PropertyInfo)member).CanWrite)	// read-only references
					return PublishMethod.None;
				else																		// writable references
					return PublishMethod.Value;									
			}
		}

		/// <summary> Appends a name qualifier to another name. </summary>
		private string AppendQualifier(string original, string qualifier)
		{
			if (qualifier == String.Empty)
				return original;
			else
				return String.Format("{0}.{1}", qualifier, original);
		}

		/// <summary> Creates an appends a new XML element. </summary>
		private XElement CreateElement(XContainer parentNode, string nameQualifier, Type type)
		{
			XElement result = 
				new XElement
				(
					XName.Get(AppendQualifier(GetElementName(type), nameQualifier), 
					GetElementNamespace(type))
				);
			parentNode.Add(result);
			return result;
		}

		/// <summary> Determines an attribute string for the given instance reference. </summary>
		private string ReferenceToString(object tempValue, out bool reference)
		{
			reference = true;
			if (tempValue == null)
				return String.Empty;
			else
			{
				Type type = tempValue.GetType();
				PublishNameAttribute attribute = ((PublishNameAttribute)ReflectionUtility.GetAttribute(type, typeof(PublishNameAttribute)));
				if (attribute != null)
					return (string)ReflectionUtility.GetMemberValue(ReflectionUtility.FindSimpleMember(type, attribute.MemberName), tempValue);
				else
				{
					var converter = Persistence.GetTypeConverter(type);
					if (converter != null)
					{
						reference = false;
						return converter.ConvertToString(tempValue);
					}
				}
				return String.Empty;
			}
		}

		private Dictionary<Type, MemberInfo[]> memberCache = new Dictionary<Type, MemberInfo[]>();

		private MemberInfo[] GetMembers(Type type)
		{
			MemberInfo[] members;
			if (!memberCache.TryGetValue(type, out members))
			{
				members = type.FindMembers(MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public, null, null).OrderBy(mi => mi.Name).ToArray();
				memberCache.Add(type, members);
			}

			return members;
		}

		/// <summary> Writes all of the members for the given instance to the specified XML element. </summary>
		private void WriteMembers(string nameQualifier, XElement node, object instance, Type type)
		{
			if (instance is IBOPSerializationEvents)
				((IBOPSerializationEvents)instance).BeforeSerialize(this);

			List<string> constructorSources = BuildConstructorParamSources(type);
			string nameMemberName = GetNameMemberName(type);
			string defaultListMemberName = GetDefaultListMemberName(type);
			PublishMethod publishMethod;
			object tempValue;

			foreach (MemberInfo member in GetMembers(type))
			{
				try
				{
					publishMethod = GetPublishMethod(member, constructorSources);

					if (publishMethod != PublishMethod.None)
					{
						tempValue = ReflectionUtility.GetMemberValue(member, instance);

						switch (publishMethod)
						{
							case PublishMethod.Value :
								var name = 
									(	// is the member the name (bop:name)
										(nameQualifier == String.Empty) && 
										String.Equals(nameMemberName, member.Name, StringComparison.OrdinalIgnoreCase)
									)
										? XName.Get(BOPName, BOPNamespaceURI)
										: XName.Get(AppendQualifier(member.Name.ToLower(), nameQualifier));
								WriteValue(name, node, member, tempValue, instance);
								break;
							case PublishMethod.Inline :
								if (tempValue != null)
									WriteMembers
									(
										AppendQualifier(member.Name.ToLower(), nameQualifier),
										node, 
										tempValue,
										tempValue.GetType()
									);
								break;
							case PublishMethod.List :
								IList list = (IList)tempValue;
								if (list != null)
								{
									string localNameQualifier = nameQualifier;
									
									// If not the default list, qualify
									if (!String.Equals(defaultListMemberName, member.Name, StringComparison.OrdinalIgnoreCase))
										localNameQualifier = AppendQualifier(member.Name.ToLower(), localNameQualifier);

									Type itemType;
									foreach (object item in list)
									{
										try
										{
											itemType = item.GetType();
											var element = CreateElement(node, localNameQualifier, itemType);
											if (!IsValueType(itemType))
												WriteMembers
												(
													String.Empty,
													element,
													item,
													itemType
												);
											else
												element.SetAttributeValue("value", item.ToString());
										}
										catch (Exception exception)
										{
											Errors.Add(exception);
										}
									}
								}
								break;
						}
					}
				}
				catch (Exception exception)
				{
					Errors.Add(exception);
				}				
			}

			Instances.Add(instance);

			if (instance is IBOPSerializationEvents)
				((IBOPSerializationEvents)instance).AfterSerialize(this);
		}
	}
}
