/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Dataphoria.Coordination.Common;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.Dataphoria.Coordination
{
	[PublishDefaultList("Nodes")]
	public class NodeConfiguration
	{
		public const string NodeConfigurationFileName = "Nodes.config";

		private NodeList _nodes = new NodeList();
		public NodeList Nodes { get { return _nodes; } }

		/// <summary> Loads the node configuration.</summary>
		/// <remarks> Creates a default node configuration if the file doesn't exist. </remarks>
		public static NodeConfiguration Load(string fileName)
		{
			NodeConfiguration configuration = new NodeConfiguration();
			if (File.Exists(fileName))
				using (Stream stream = File.OpenRead(fileName))
				{
					new Deserializer().Deserialize(stream, configuration);
				}
			return configuration;
		}

		public static string GetNodeConfigurationFileName()
		{
			return Path.Combine(PathUtility.CommonAppDataPath("Coordinator", VersionModifier.None), NodeConfigurationFileName);
		}

		public static NodeConfiguration Load()
		{
			return Load(GetNodeConfigurationFileName());
		}
		
		public void Save(string fileName)
		{
			using (Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				new Serializer().Serialize(stream, this);
			}
		}

		public void Save()
		{
			Save(GetNodeConfigurationFileName());
		}
	}

	/// <summary> List of nodes. </summary>
	/// <remarks> Names must be case insensitively unique. </remarks>
	public class NodeList : HashtableList<string, NodeDescriptor>
	{
		public NodeList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object tempValue)
		{
			NodeDescriptor node = (NodeDescriptor)tempValue;
			Add(node.HostName, node);
			return IndexOf(node.HostName);
		}
		
		public new NodeDescriptor this[string hostName]
		{
			get
			{
				NodeDescriptor node = null;
				TryGetValue(hostName, out node);
				return node;
			}
			set
			{
				base[hostName] = value;
			}
		}

		public NodeDescriptor GetNode(string hostName)
		{
			NodeDescriptor node;
			if (!TryGetValue(hostName, out node))
				throw new ArgumentException(String.Format("A node with host name {0} was not found.", hostName));
			else
				return node;
		}
		
		public bool HasNode(string hostName)
		{
			return ContainsKey(hostName);
		}
	}
}
