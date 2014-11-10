/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.Dataphoria.Coordination.Common;
using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Coordination
{
	public class Coordinator
	{
		public Coordinator()
		{
			_nodes = NodeConfiguration.Load();
			_activeNodes.AddRange(_nodes.Nodes.Where(kv => kv.Value.IsEnabled).Cast<NodeDescriptor>());
		}

		// Persisted configuration information for all nodes in the cluster.
		private NodeConfiguration _nodes;

		// Maintained list of nodes where IsEnabled is true.
		private ActiveNodeList _activeNodes = new ActiveNodeList();

		// In-memory index of instance entries by instance name.
		private InstanceEntryList _instances = new InstanceEntryList();

		// Persisted running information for all nodes.
		private NodeEntryList _entries = new NodeEntryList();

		private object _syncHandle = new Object();

		#region NodeManagement

		public NodeDescriptor GetNode(string hostName)
		{
			lock (_syncHandle)
			{
				var result = _nodes.Nodes[hostName];
				if (result != null)
				{
					return result.Copy();
				}

				return result;
			}
		}

		public IEnumerable<NodeDescriptor> GetNodes()
		{
			lock (_syncHandle)
			{
				var result = new List<NodeDescriptor>();
				foreach (var node in _nodes.Nodes.Values)
				{
					result.Add(node.Copy());
				}

				result.Sort((x, y) => x.HostName.CompareTo(y.HostName));

				return result;
			}
		}

		public void PutNode(NodeDescriptor node)
		{
			// Configure a node
				// enable/disable the node
					// disabling a node with running instances is allowed
				// set the port pool
					// adding or removing ports that are already associated with running or deployed instances is allowed
				// set the max instances
					// setting max instances to a value less than the number of currently deployed instances is allowed

			lock (_syncHandle)
			{
				var currentNode = _nodes.Nodes[node.HostName];
				if (currentNode == null)
				{
					currentNode = node.Copy();
					if (currentNode.IsEnabled)
					{
						_activeNodes.Add(currentNode);
					}
					_nodes.Nodes[node.HostName] = currentNode;
				}
				else
				{
					if (currentNode.IsEnabled && !node.IsEnabled)
					{
						_activeNodes.Remove(currentNode);
					}
					currentNode.CopyFrom(node);
				}

				_nodes.Save();
			}
		}

		public void DeleteNode(string hostName)
		{
			lock (_syncHandle)
			{
				if (GetActiveInstances(hostName) > 0)
				{
					throw new InvalidOperationException("Node {0} cannot be removed because it has active instances.");
				}

				var currentNode = _nodes.Nodes[hostName];
				if (currentNode != null)
				{
					if (currentNode.IsEnabled)
					{
						_activeNodes.Remove(currentNode);
					}

					_nodes.Nodes.Remove(hostName);
					_nodes.Save();
				}
			}
		}

		#endregion

		#region InstanceManagement

		// Retrieve instance list

		// Deploy an instance
			
		// Start an instance

		// Stop an instance

		// Kill an instance

		// Probe an instance

		// Remove an instance

		#endregion

		private string GetNextHost()
		{
			var startingNode = _activeNodes.CurrentNode;
			var currentNode = _activeNodes.CurrentNode;

			if (currentNode == null)
			{
				throw new InvalidOperationException("A host could not be selected because there are no active hosts.");
			}

			// First pass just looks for a node with an empty slot
			while (currentNode != null)
			{
				if (GetActiveInstances(currentNode.HostName) < currentNode.MaxInstances)
				{
					return currentNode.HostName;
				}
				else
				{
					currentNode = _activeNodes.MoveNext();
					if (currentNode == startingNode)
					{
						break;
					}
				}
			}

			// Second pass looks for a node with any candidates for destruction
			while (currentNode != null)
			{
				var candidate = GetCandidate(currentNode.HostName);
				if (candidate != null)
				{
					DestroyInstance(candidate);
					return currentNode.HostName;
				}
				else
				{
					currentNode = _activeNodes.MoveNext();
					if (currentNode == startingNode)
					{
						throw new InvalidOperationException("A host could not be selected because all hosts have the maximum number of active instances.");
					}
				}
			}

            // Fail
            throw new InvalidOperationException("A host could not be selected because there were no empty slots, and all hosts have the maximum number of active instances.");
		}

		private int GetActiveInstances(string hostName)
		{
			var entry = _entries[hostName];

			if (entry == null)
			{
				return 0;
			}

			return entry.Instances.Count;
		}

		private InstanceEntry GetCandidate(string hostName)
		{
			// Return the oldest instance that has a LastRequestedDate before the configured keepAliveTime for the node.
			var entry = _entries[hostName];
			if (entry == null)
			{
				return null;
			}

			var cutoff = DateTime.Now - _nodes.Nodes[hostName].KeepAliveTime;
			return entry.Instances.Where(i => i.Value.LastRequested <= cutoff).OrderBy(i => i.Value.LastRequested).FirstOrDefault().Value;
		}

		private void DestroyInstance(InstanceEntry entry)
		{
			throw new NotImplementedException();
		}

		private int DeployInstance(InstanceEntry entry)
		{
			throw new NotImplementedException();
		}

		private int GetNextPort(string hostName)
		{
			throw new NotImplementedException();
		}

		#region CoordinatorClient

		// GetNextPort
		// per node, a hash of used port numbers with a next port number
			// record starting port number
			// while the port number is used
				// get next port number from the configured range for the instance, with rollover
				// if the port number is equal to the starting port number
					// raise all ports used
				// else
					// return the port number

		// Request an instance url
		public string RequestInstance(string instanceName)
		{
			lock (_syncHandle)
			{
				var entry = _instances[instanceName];
				if (entry != null)
				{
					// TODO: More sophisticated handling of server state?
					// The assumption here is that if it is in the list of instances, it's a valid instance
					entry.LastRequested = DateTime.Now;
					return entry.InstanceUri;
				}

				var hostName = GetNextHost();
				var portNumber = GetNextPort(hostName);

				entry = new InstanceEntry { InstanceName = instanceName, HostName = hostName, PortNumber = portNumber };
				entry.InstanceUri = DataphorServiceUtility.BuildInstanceURI(hostName, portNumber, instanceName);
				entry.ProcessId = DeployInstance(entry);
				_instances.Add(instanceName, entry);

				return entry.InstanceUri;
			}
		}

		// Report an instance failure
		public void ReportInstanceFailure(string instanceName)
		{
			throw new NotImplementedException();
		}

		// Report instance usage
        //public void ReportInstanceUsage(InstanceUsage usage)
        //{
        //    throw new NotImplementedException();
        //}

		#endregion
	}
}
