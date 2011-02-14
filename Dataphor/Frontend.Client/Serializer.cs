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
		const string NamespaceAttributeName = "name";
		const string AssemblyAttributeName = "assembly";
		const string NodeNameAttributeName = "name";

		/// <summary> Initializez the private FNodeTypes hashtable. </summary>
		public NodeTypeTable()
		{
			_nodeTypes = new Dictionary<string, NodeTypeEntry>(64, StringComparer.OrdinalIgnoreCase);
		}

		private Dictionary<string, NodeTypeEntry> _nodeTypes;

		public NodeTypeEntry this[string nodeName]
		{
			get { return _nodeTypes[nodeName.ToLower()]; }
		}
		
		public bool TryGetValue(string nodeName, out NodeTypeEntry entry)
		{
			return _nodeTypes.TryGetValue(nodeName, out entry);
		}
		
		public bool Contains(string nodeName)
		{
			return _nodeTypes.ContainsKey(nodeName);
		}

		public ICollection Keys 
		{
			get { return _nodeTypes.Keys; }
		}

		public ICollection Entries
		{
			get { return _nodeTypes.Values; }
		}

		public int Count
		{
			get { return _nodeTypes.Count; }
		}

		public void Clear()
		{
			_nodeTypes.Clear();
		}

		/// <summary> Loads the NodeTypeTable from a given stream. </summary>
		/// <param name="source"> A stream containing an XML NodeTypeTable information. </param>
		public void LoadFromStream(Stream source)
		{
			XDocument document = XDocument.Load(new StreamReader(source));
			InternalLoad(document);
		}

		/// <summary> Loads the NodeTypeTable from a given string. </summary>
		public void LoadFromString(string source)
		{
			XDocument document = XDocument.Load(new StringReader(source));
			InternalLoad(document);
		}

		private void InternalLoad(XDocument document)
		{
			// TODO: Better XML validation on the NodeTypeTable load
			NodeTypeEntry nodeTypeEntry;
			foreach (XElement node in document.Root.Elements())
			{
				nodeTypeEntry = new NodeTypeEntry();
				nodeTypeEntry.Namespace = node.Attribute(NamespaceAttributeName).Value;
				nodeTypeEntry.Assembly = node.Attribute(AssemblyAttributeName).Value;
				foreach (XElement child in node.Elements())
					_nodeTypes.Add(child.Attribute(NodeNameAttributeName).Value.ToLower(), nodeTypeEntry);
			}

		}

		/// <summary> Reads the node type namespace information from the specified file. </summary>
		/// <param name="fileName"> A file name referencing a file which contains a valid XML node type document. </param>
		public void LoadFromFile(string fileName)
		{
			try
			{
				using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					LoadFromStream(stream);
				}
			}
			catch (Exception exception)
			{
				throw new ClientException(ClientException.Codes.UnableToLoadTypeTable, exception, fileName);
			}
		}

		/// <summary> Searches thru the NodeTypeTable and returns a type for it. </summary>
		public Type GetClassType(string className)
		{
			NodeTypeEntry entry;
			if (!_nodeTypes.TryGetValue(className, out entry))
				return null;
			else
				return ReflectionUtility.GetType(entry.Namespace, className, entry.Assembly);
		}

		/// <summary> Creates an instance of the specified class using the NodeTypeTable. </summary>
		public object CreateInstance(string className)
		{
			NodeTypeEntry entry;
			if (!_nodeTypes.TryGetValue(className, out entry))
				throw new Exception(String.Format("CreateInstance: Class ({0}) not found in node type list.", className));
			return ReflectionUtility.CreateInstance(entry.Namespace, className, entry.Assembly);
		}
	}

	/// <summary> A custom client serializer decendant that uses NodeTypeTable to get a namespace for each object deserialzed. </summary>
	public class Serializer : BOP.Serializer
    {
		/// <remarks> Never write a namespace since they are retrieved from the NodeTypeTable </remarks>
		protected override string GetElementNamespace(Type type)
		{
			// We don't want namespaces in BOP XML, so we return empty.
			return String.Empty;
		}
	}

	/// <summary> A custom client serializer decendant that uses NodeTypeTable to get a namespace for each object deserialzed. </summary>
	public class Deserializer : BOP.Deserializer
	{
		public Deserializer(NodeTypeTable table)
		{
			_table = table;
		}

		private NodeTypeTable _table;

		/// <remarks> Uses the NodeTypeType to find a type name for the element. </remarks>
		protected override Type GetClassType(string name, string namespaceValue)
		{
			if (namespaceValue == String.Empty)
			{
				NodeTypeEntry entry;
				if (_table.TryGetValue(name, out entry))
					return 
						ReflectionUtility.GetType
						(
							entry.Namespace, 
							name,
							entry.Assembly
						);
			}
			return base.GetClassType(name, namespaceValue);
		}
	}
    
}