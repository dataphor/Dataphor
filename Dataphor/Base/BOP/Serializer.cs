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
		public void Serialize(Stream AStream, object AObject)
		{
			XDocument LDocument = new XDocument();

			Serialize(LDocument, AObject);

			XmlWriter LWriter = XmlWriter.Create(AStream, new XmlWriterSettings { Indent = true, IndentChars = "\t" });

			LDocument.WriteTo(LWriter);
			LWriter.Flush();
			
			// Don't call LWriter.Close() or the AStream will be closed and will not acessable after this function is called.
		}
 
		/// <summary> Stores an instance of an object as an XML document. </summary>
		public virtual void Serialize(XDocument ADocument, object AObject)
		{
			Errors.Clear();

			BeginFixups();
			try
			{
				// Create root object
				Type LType = AObject.GetType();
				XElement LRoot = CreateElement(ADocument, String.Empty, LType);

				// Specify BOP namespace
				LRoot.SetAttributeValue(XNamespace.Xmlns + CBOPNamespacePrefix, CBOPNamespaceURI);

				WriteMembers(String.Empty, LRoot, AObject, LType);
			}
			finally
			{
				EndFixups();
			}
		}

		protected Set<object> FInstances;
		public Set<object> Instances
		{
			get { return FInstances; }
		}

		private List<Reference> FReferences;

		private class Reference
		{
			public Reference(object AInstance, XAttribute AAttribute)
			{
				Instance = AInstance;
				Attribute = AAttribute;
			}
			public object Instance;
			public XAttribute Attribute;
		}

		private void BeginFixups()
		{
			FInstances = new Set<object>();
			FReferences = new List<Reference>();
		}

		private void EndFixups()
		{
			// Remove all attributes that reference instances that were not written
			if (RemoveReferencesToObjectsNotSerialized)
				foreach (Reference LReference in FReferences)
					if (!Instances.Contains(LReference.Instance))
						LReference.Attribute.Remove();
			FReferences = null;
			FInstances = null;
		}

		/// <summary> Determines the element namespace to use when writing the specified type. </summary>
		protected virtual string GetElementNamespace(Type AType)
		{
			return String.Format("{0},{1}", AType.Namespace, AssemblyNameUtility.GetName(AType.Assembly.FullName));
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
		private void WriteValue(XName AName, XElement ANode, MemberInfo AMember, object AValue, object AInstance)
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
				
				XAttribute LAttribute = new XAttribute(AName, LValue);
				ANode.Add(LAttribute);

				// Add a reference fixup if needed
				if (LIsReference && !Instances.Contains(AValue))
					FReferences.Add(new Reference(AValue, LAttribute));		
			}
		}

		/// <summary> Builds a list of names of the specified type's members that are arguments to the constructor. </summary>
		private List<string> BuildConstructorParamSources(Type AType)
		{
			PublishDefaultConstructorAttribute LConstructorAttribute = (PublishDefaultConstructorAttribute)ReflectionUtility.GetAttribute(AType, typeof(PublishDefaultConstructorAttribute));
			if ((LConstructorAttribute != null) && (LConstructorAttribute.ConstructorSignature != String.Empty)) 
			{
				List<string> LResult = new List<string>();
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
		private PublishMethod GetPublishMethod(MemberInfo AMember, List<string> ASources)
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

		/// <summary> Creates an appends a new XML element. </summary>
		private XElement CreateElement(XContainer AParentNode, string ANameQualifier, Type AType)
		{
			XElement LResult = 
				new XElement
				(
					XName.Get(AppendQualifier(GetElementName(AType), ANameQualifier), 
					GetElementNamespace(AType))
				);
			AParentNode.Add(LResult);
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

		/// <summary> Writes all of the members for the given instance to the specified XML element. </summary>
		private void WriteMembers(string ANameQualifier, XElement ANode, object AInstance, Type AType)
		{
			if (AInstance is IBOPSerializationEvents)
				((IBOPSerializationEvents)AInstance).BeforeSerialize(this);

			List<string> LConstructorSources = BuildConstructorParamSources(AType);
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
								var LName = 
									(	// is the member the name (bop:name)
										(ANameQualifier == String.Empty) && 
										String.Equals(LNameMemberName, LMember.Name, StringComparison.OrdinalIgnoreCase)
									)
										? XName.Get(CXmlBOPName, CBOPNamespaceURI)
										: XName.Get(AppendQualifier(LMember.Name.ToLower(), ANameQualifier));
								WriteValue(LName, ANode, LMember, LValue, AInstance);
								break;
							case PublishMethod.Inline :
								if (LValue != null)
									WriteMembers
									(
										AppendQualifier(LMember.Name.ToLower(), ANameQualifier),
										ANode, 
										LValue,
										LValue.GetType()
									);
								break;
							case PublishMethod.List :
								IList LList = (IList)LValue;
								if (LList != null)
								{
									string LNameQualifier = ANameQualifier;
									
									// If not the default list, qualify
									if (!String.Equals(LDefaultListMemberName, LMember.Name, StringComparison.OrdinalIgnoreCase))
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
				catch (Exception LException)
				{
					Errors.Add(LException);
				}				
			}

			Instances.Add(AInstance);

			if (AInstance is IBOPSerializationEvents)
				((IBOPSerializationEvents)AInstance).AfterSerialize(this);
		}
	}
}
