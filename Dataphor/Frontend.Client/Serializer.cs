/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Used by <see cref="NodeTypeTable"/> to associate a node type name with a qualified namespace </summary>
	public class NodeTypeEntry
	{
		public string Namespace;
		public string Assembly;
	}

	/// <summary> Maintains qualified namespace information for node types names. </summary>
	public class NodeTypeTable
	{
		// Do not localize
		const string CNamespaceAttributeName = "name";
		const string CAssemblyAttributeName = "assembly";
		const string CNodeNameAttributeName = "name";

		/// <summary> Initializez the private FNodeTypes hashtable. </summary>
		public NodeTypeTable()
		{
			FNodeTypes = new Hashtable(50);
		}

		private Hashtable FNodeTypes;

		public NodeTypeEntry this[string ANodeName]
		{
			get { return (NodeTypeEntry)FNodeTypes[ANodeName.ToLower()]; }
		}

		public ICollection Keys 
		{
			get { return FNodeTypes.Keys; }
		}

		public ICollection Entries
		{
			get { return FNodeTypes.Values; }
		}

		public int Count
		{
			get { return FNodeTypes.Count; }
		}

		public void Clear()
		{
			FNodeTypes.Clear();
		}

		/// <summary> Loads the NodeTypeTable from a given stream. </summary>
		/// <param name="ASource"> A stream containing an XML NodeTypeTable information. </param>
		public void LoadFromStream(Stream ASource)
		{
			XmlDocument LDocument = new XmlDocument();
			LDocument.Load(ASource);
			InternalLoad(LDocument);
		}

		/// <summary> Loads the NodeTypeTable from a given string. </summary>
		public void LoadFromString(string ASource)
		{
			XmlDocument LDocument = new XmlDocument();
			LDocument.Load(new StringReader(ASource));
			InternalLoad(LDocument);
		}

		private void InternalLoad(XmlDocument ADocument)
		{
			// TODO: Better XML validation on the NodeTypeTable load
			NodeTypeEntry LNodeTypeEntry;
			foreach (XmlNode LNode in ADocument.DocumentElement.ChildNodes)
			{
				if (LNode is XmlElement)
				{
					LNodeTypeEntry = new NodeTypeEntry();
					LNodeTypeEntry.Namespace = LNode.Attributes[CNamespaceAttributeName].Value;
					LNodeTypeEntry.Assembly = LNode.Attributes[CAssemblyAttributeName].Value;
					foreach (XmlNode LChild in LNode.ChildNodes)
					{
						if (LChild is XmlElement)
							FNodeTypes.Add(LChild.Attributes[CNodeNameAttributeName].Value.ToLower(), LNodeTypeEntry);
					}
				}
			}

		}

		/// <summary> Reads the node type namespace information from the specified file. </summary>
		/// <param name="AFileName"> A file name referencing a file which contains a valid XML node type document. </param>
		public void LoadFromFile(string AFileName)
		{
			try
			{
				using (FileStream LStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
				{
					LoadFromStream(LStream);
				}
			}
			catch (Exception LException)
			{
				throw new ClientException(ClientException.Codes.UnableToLoadTypeTable, LException, AFileName);
			}
		}

		/// <summary> Searches thru the NodeTypeTable and returns a type for it. </summary>
		public Type GetClassType(string AClassName)
		{
			NodeTypeEntry LEntry = this[AClassName];
			if (LEntry == null)
				return null;
			return Type.GetType(String.Format("{0}.{1},{2}", LEntry.Namespace, AClassName, LEntry.Assembly), true, true);
		}

		/// <summary> Creates an instance of the specified class using the NodeTypeTable. </summary>
		public object CreateInstance(string AClassName)
		{
			NodeTypeEntry LEntry = this[AClassName];
			if (LEntry == null)
				throw new Exception(String.Format("CreateInstance: Class ({0}) not found in node type list.", AClassName));
			return Activator.CreateInstance(Type.GetType(String.Format("{0}.{1},{2}", LEntry.Namespace, AClassName, LEntry.Assembly), true, true));
		}
	}

	/// <summary> A custom client serializer decendant that uses NodeTypeTable to get a namespace for each object deserialzed. </summary>
	public class Serializer : BOP.Serializer
    {
		/// <remarks> Never write a namespace since they are retrieved from the NodeTypeTable </remarks>
		protected override string GetElementNamespace(Type AType)
		{
			// We don't want namespaces in BOP XML, so we return empty.
			return String.Empty;
		}
	}

	/// <summary> A custom client serializer decendant that uses NodeTypeTable to get a namespace for each object deserialzed. </summary>
	public class Deserializer : BOP.Deserializer
	{
		public Deserializer(NodeTypeTable ATable)
		{
			FTable = ATable;
		}

		private NodeTypeTable FTable;

		/// <remarks> Uses the NodeTypeType to find a type name for the element. </remarks>
		protected override Type GetClassType(string AName, string ANamespace)
		{
			if (ANamespace == String.Empty)
			{
				NodeTypeEntry LEntry = FTable[AName];
				if (LEntry != null)
					return 
						Type.GetType
						(
							Assembly.CreateQualifiedName
							(
								LEntry.Assembly,
								String.Format("{0}.{1}", LEntry.Namespace, AName)
							), 
							true, 
							true
						);
			}
			return base.GetClassType(AName, ANamespace);
		}
	}
    
}