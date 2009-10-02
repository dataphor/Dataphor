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
using System.Collections.Generic;
using System.Xml.Linq;

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
			FNodeTypes = new Dictionary<string, NodeTypeEntry>(64);
		}

		private Dictionary<string, NodeTypeEntry> FNodeTypes;

		public NodeTypeEntry this[string ANodeName]
		{
			get { return FNodeTypes[ANodeName.ToLower()]; }
		}
		
		public bool TryGetValue(string ANodeName, out NodeTypeEntry AEntry)
		{
			return FNodeTypes.TryGetValue(ANodeName, out AEntry);
		}
		
		public bool Contains(string ANodeName)
		{
			return FNodeTypes.ContainsKey(ANodeName);
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
			XDocument LDocument = XDocument.Load(new StreamReader(ASource));
			InternalLoad(LDocument);
		}

		/// <summary> Loads the NodeTypeTable from a given string. </summary>
		public void LoadFromString(string ASource)
		{
			XDocument LDocument = XDocument.Load(new StringReader(ASource));
			InternalLoad(LDocument);
		}

		private void InternalLoad(XDocument ADocument)
		{
			// TODO: Better XML validation on the NodeTypeTable load
			NodeTypeEntry LNodeTypeEntry;
			foreach (XElement LNode in ADocument.Root.Elements())
			{
				LNodeTypeEntry = new NodeTypeEntry();
				LNodeTypeEntry.Namespace = LNode.Attribute(CNamespaceAttributeName).Value;
				LNodeTypeEntry.Assembly = LNode.Attribute(CAssemblyAttributeName).Value;
				foreach (XElement LChild in LNode.Elements())
					FNodeTypes.Add(LChild.Attribute(CNodeNameAttributeName).Value.ToLower(), LNodeTypeEntry);
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
			NodeTypeEntry LEntry;
			if (!FNodeTypes.TryGetValue(AClassName, out LEntry))
				return null;
			else
				return Type.GetType(String.Format("{0}.{1},{2}", LEntry.Namespace, AClassName, LEntry.Assembly), true, true);
		}

		/// <summary> Creates an instance of the specified class using the NodeTypeTable. </summary>
		public object CreateInstance(string AClassName)
		{
			NodeTypeEntry LEntry;
			if (!FNodeTypes.TryGetValue(AClassName, out LEntry))
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
				NodeTypeEntry LEntry;
				if (FTable.TryGetValue(AName, out LEntry))
					return 
						Type.GetType
						(
							String.Format
							(
								"{0}.{1},{2}",
								LEntry.Namespace, 
								AName,
								LEntry.Assembly
							), 
							true, 
							true
						);
			}
			return base.GetClassType(AName, ANamespace);
		}
	}
    
}