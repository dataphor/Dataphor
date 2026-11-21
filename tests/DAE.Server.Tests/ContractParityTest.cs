/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Each of the three WCF service contracts is declared twice - synchronously on the server, and as an asynchronous copy
	/// on the client - in separate assemblies with no compiler relationship between them. These tests compare the two
	/// declarations of each contract by reflection, so that a hand edit to one copy fails here instead of failing at run
	/// time on a customer's network as a WCF action mismatch.
	/// </summary>
	/// <remarks>
	/// <para>
	/// What is compared is what the wire carries. An operation's action URI comes from the contract's Name and Namespace and
	/// from the operation name, and its message body from the parameter and return types, so those are compared; so are the
	/// declared fault detail types, which decide whether the other side sees a typed fault or a bare one. An operation can
	/// also name its action outright instead of letting WCF derive it, and can declare itself one-way, so those are compared
	/// too. Parameter names are not on the wire and are not compared.
	/// </para>
	/// <para>
	/// A client operation is an [OperationContract(AsyncPattern = true)] method named Begin&lt;Name&gt; paired with an
	/// End&lt;Name&gt; method that carries no attribute. Its operation name is &lt;Name&gt;, its wire parameters are Begin's
	/// without the trailing AsyncCallback and object, and its return type is End's. The pairing is checked, not assumed.
	/// </para>
	/// <para>
	/// No server and no socket: these tests read interface declarations only, so they hold whatever the transport turns out
	/// to be.
	/// </para>
	/// <para>
	/// NOTE: the comparison below has no test of its own. Its branches - the Begin/End pairing, the malformed-client
	/// reports, the overload detection, the known-difference matching - were each confirmed against purpose-built broken
	/// interface pairs that were deliberately not committed, so a later edit to them is unguarded except by the check that
	/// the server side yielded at least one operation. Build the synthetic pairs again before reworking the reading.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class ContractParityTest
	{
		/// <summary>A fault declaration that differs between the two copies of a contract today.</summary>
		/// <remarks>
		/// The pattern the repository already uses for a known difference (the coverage manifest, the parser corpus allow
		/// list): the difference is recorded with the ticket that owns its cause, so the test stays green on what is known
		/// and fails on anything else. An entry whose difference is no longer there fails the test as well, so a fix cannot
		/// land without removing its entry.
		/// </remarks>
		private sealed class KnownFaultDifference
		{
			/// <param name="contract">Name of the server interface type, e.g. "IDataphorService".</param>
			/// <param name="operation">Operation name as it appears on the wire.</param>
			/// <param name="serverOnly">Fault detail types the server declares and the client does not.</param>
			/// <param name="clientOnly">Fault detail types the client declares and the server does not.</param>
			public KnownFaultDifference(string contract, string operation, Type[] serverOnly, Type[] clientOnly)
			{
				Contract = contract;
				Operation = operation;
				ServerOnly = serverOnly;
				ClientOnly = clientOnly;
			}

			public string Contract { get; private set; }
			public string Operation { get; private set; }
			public Type[] ServerOnly { get; private set; }
			public Type[] ClientOnly { get; private set; }
		}

		// Fault declarations that differ today: none. The six contracts declare the same faults on every operation, and
		// an entry here is how a difference that cannot be fixed at once is recorded - server interface name, operation,
		// faults only the server declares, faults only the client declares - so that this test stays green on what is
		// known and fails on anything else.
		private static readonly KnownFaultDifference[] KnownFaultDifferences = { };

		// The namespace WCF gives a contract that does not name one, so that an omitted Namespace is compared as the value
		// it would actually put in the action URI rather than as null.
		private const string DefaultContractNamespace = "http://tempuri.org/";

		[Test]
		public void DataphorServiceDeclarationsMatch()
		{
			AssertDeclarationsMatch(typeof(IDataphorService), typeof(IClientDataphorService));
		}

		[Test]
		public void NativeCLIServiceDeclarationsMatch()
		{
			AssertDeclarationsMatch(typeof(INativeCLIService), typeof(IClientNativeCLIService));
		}

		[Test]
		public void ListenerServiceDeclarationsMatch()
		{
			AssertDeclarationsMatch(typeof(IListenerService), typeof(IClientListenerService));
		}

		/// <summary>One operation of one declaration, reduced to what the wire carries.</summary>
		private sealed class Operation
		{
			/// <summary>The method the operation was read from: the operation itself on the server, the Begin half on the client.</summary>
			public MethodInfo Method;

			/// <summary>Parameter types in order, without the client's trailing AsyncCallback and object.</summary>
			public Type[] ParameterTypes;

			/// <summary>The operation's return type: the method's own on the server, the End half's on the client.</summary>
			public Type ReturnType;

			/// <summary>The FaultContract detail types declared on the operation, in declaration order.</summary>
			public Type[] FaultDetailTypes;

			/// <summary>The [OperationContract] the operation was declared with, for the properties on it that change the wire.</summary>
			public OperationContractAttribute Contract;
		}

		/// <summary>Compares the two declarations of one contract and fails with every difference found, not just the first.</summary>
		private static void AssertDeclarationsMatch(Type serverInterface, Type clientInterface)
		{
			List<string> differences = new List<string>();

			CompareContractIdentity(serverInterface, clientInterface, differences);

			Dictionary<string, Operation> serverOperations = ReadServerOperations(serverInterface, differences);
			Dictionary<string, Operation> clientOperations = ReadClientOperations(clientInterface, differences);

			// A mistake in the reading above that found no operations would otherwise pass silently.
			if (serverOperations.Count == 0)
				differences.Add(String.Format("{0}: no operations found; the comparison would pass on nothing.", serverInterface.Name));

			CompareOperations(serverInterface, serverOperations, clientOperations, differences);

			if (differences.Count > 0)
			{
				StringBuilder message = new StringBuilder();
				message.AppendLine(String.Format("{0} and {1} differ in {2} way(s):", serverInterface.Name, clientInterface.Name, differences.Count));
				foreach (string difference in differences)
					message.AppendLine("  " + difference);
				message.Append("The two declarations describe one wire protocol and must agree; neither is free to change alone.");
				Assert.Fail(message.ToString());
			}
		}

		/// <summary>Compares the contract Name and Namespace, which with the operation name make up each operation's action URI.</summary>
		private static void CompareContractIdentity(Type serverInterface, Type clientInterface, List<string> differences)
		{
			ServiceContractAttribute serverContract = serverInterface.GetCustomAttribute<ServiceContractAttribute>();
			ServiceContractAttribute clientContract = clientInterface.GetCustomAttribute<ServiceContractAttribute>();

			if (serverContract == null)
				differences.Add(String.Format("{0} carries no [ServiceContract].", serverInterface.Name));
			if (clientContract == null)
				differences.Add(String.Format("{0} carries no [ServiceContract].", clientInterface.Name));
			if ((serverContract == null) || (clientContract == null))
				return;

			// An unset Name or Namespace is compared as the value WCF would use, so that dropping either is a difference.
			string serverName = serverContract.Name ?? serverInterface.Name;
			string clientName = clientContract.Name ?? clientInterface.Name;
			if (serverName != clientName)
				differences.Add(String.Format("contract Name differs - server \"{0}\", client \"{1}\".", serverName, clientName));

			string serverNamespace = serverContract.Namespace ?? DefaultContractNamespace;
			string clientNamespace = clientContract.Namespace ?? DefaultContractNamespace;
			if (serverNamespace != clientNamespace)
				differences.Add(String.Format("contract Namespace differs - server \"{0}\", client \"{1}\".", serverNamespace, clientNamespace));
		}

		/// <summary>Reads the server's operations: every method carrying [OperationContract], keyed by its wire name.</summary>
		private static Dictionary<string, Operation> ReadServerOperations(Type serverInterface, List<string> differences)
		{
			Dictionary<string, Operation> operations = new Dictionary<string, Operation>();
			foreach (MethodInfo method in serverInterface.GetMethods())
			{
				OperationContractAttribute attribute = method.GetCustomAttribute<OperationContractAttribute>();
				if (attribute == null)
					continue; // Not on the wire. A dropped attribute shows up below as an operation the client has and the server does not.

				Operation operation =
					new Operation
					{
						Method = method,
						ParameterTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
						ReturnType = method.ReturnType,
						FaultDetailTypes = ReadFaultDetailTypes(method),
						Contract = attribute
					};
				AddOperation(operations, serverInterface, attribute.Name ?? method.Name, operation, differences);
			}
			return operations;
		}

		/// <summary>Reads the client's operations by pairing each [OperationContract(AsyncPattern = true)] Begin method with its End method.</summary>
		private static Dictionary<string, Operation> ReadClientOperations(Type clientInterface, List<string> differences)
		{
			const string BeginPrefix = "Begin";
			const string EndPrefix = "End";

			Dictionary<string, Operation> operations = new Dictionary<string, Operation>();
			foreach (MethodInfo method in clientInterface.GetMethods())
			{
				OperationContractAttribute attribute = method.GetCustomAttribute<OperationContractAttribute>();
				if (attribute == null)
				{
					// The End half of a pair is the only member allowed to carry no attribute.
					if (!method.Name.StartsWith(EndPrefix, StringComparison.Ordinal)
							|| (FindMethods(clientInterface, BeginPrefix + method.Name.Substring(EndPrefix.Length)).Length == 0))
						differences.Add(String.Format("{0}.{1} carries no [OperationContract] and is not the End half of a Begin/End pair.", clientInterface.Name, method.Name));
					continue;
				}

				if (!attribute.AsyncPattern)
				{
					differences.Add(String.Format("{0}.{1} is declared without AsyncPattern; the client declares each operation as an [OperationContract(AsyncPattern = true)] Begin/End pair.", clientInterface.Name, method.Name));
					continue;
				}

				if (!method.Name.StartsWith(BeginPrefix, StringComparison.Ordinal))
				{
					differences.Add(String.Format("{0}.{1} declares AsyncPattern but is not named Begin<Name>, so it has no operation name and no End half to pair with.", clientInterface.Name, method.Name));
					continue;
				}

				string baseName = method.Name.Substring(BeginPrefix.Length);
				MethodInfo[] endMethods = FindMethods(clientInterface, EndPrefix + baseName);
				if (endMethods.Length == 0)
				{
					differences.Add(String.Format("{0}.{1} has no {2}{3}: a Begin without its End is a malformed contract, and the operation's return type cannot be read.", clientInterface.Name, method.Name, EndPrefix, baseName));
					continue;
				}
				if (endMethods.Length > 1)
				{
					differences.Add(String.Format("{0} declares {1}{2} {3} times; the End half of a pair cannot be overloaded, because an operation has one return type.", clientInterface.Name, EndPrefix, baseName, endMethods.Length));
					continue;
				}

				MethodInfo endMethod = endMethods[0];
				ParameterInfo[] endParameters = endMethod.GetParameters();
				if ((endParameters.Length != 1) || (endParameters[0].ParameterType != typeof(IAsyncResult)))
					differences.Add(String.Format("{0}.{1}{2} takes ({3}) rather than (IAsyncResult), so it is not the End half of {4}.", clientInterface.Name, EndPrefix, baseName, FormatTypeList(endParameters.Select(parameter => parameter.ParameterType)), method.Name));

				ParameterInfo[] beginParameters = method.GetParameters();
				if ((beginParameters.Length < 2)
						|| (beginParameters[beginParameters.Length - 2].ParameterType != typeof(AsyncCallback))
						|| (beginParameters[beginParameters.Length - 1].ParameterType != typeof(object)))
				{
					differences.Add(String.Format("{0}.{1} takes ({2}) rather than ending in (AsyncCallback, object), so its wire parameters cannot be read.", clientInterface.Name, method.Name, FormatTypeList(beginParameters.Select(parameter => parameter.ParameterType))));
					continue;
				}

				Operation operation =
					new Operation
					{
						Method = method,
						ParameterTypes = beginParameters.Take(beginParameters.Length - 2).Select(parameter => parameter.ParameterType).ToArray(),
						ReturnType = endMethod.ReturnType,
						FaultDetailTypes = ReadFaultDetailTypes(method),
						Contract = attribute
					};
				AddOperation(operations, clientInterface, attribute.Name ?? baseName, operation, differences);
			}
			return operations;
		}

		/// <summary>Adds an operation under its wire name, reporting a collision rather than keying two operations to one name.</summary>
		private static void AddOperation(Dictionary<string, Operation> operations, Type contract, string name, Operation operation, List<string> differences)
		{
			Operation existing;
			if (operations.TryGetValue(name, out existing))
			{
				// The comparison keys operations by name, so it only holds while the names are unique. An overload has to
				// name itself on the wire anyway: two operations in one contract cannot share an action URI.
				differences.Add(String.Format("{0}: {1}({2}) and {3}({4}) are both the operation \"{5}\"; an overload needs an explicit [OperationContract(Name = ...)].", contract.Name, existing.Method.Name, FormatTypeList(existing.ParameterTypes), operation.Method.Name, FormatTypeList(operation.ParameterTypes), name));
				return;
			}
			operations.Add(name, operation);
		}

		/// <summary>The interface's methods of that name, however many there are.</summary>
		/// <remarks>
		/// Type.GetMethod(string) throws AmbiguousMatchException on an overload, which would end the comparison with a
		/// reflection error instead of reporting the overload as the difference it is.
		/// </remarks>
		private static MethodInfo[] FindMethods(Type contract, string name)
		{
			return contract.GetMethods().Where(method => method.Name == name).ToArray();
		}

		private static Type[] ReadFaultDetailTypes(MethodInfo method)
		{
			return method.GetCustomAttributes<FaultContractAttribute>().Select(attribute => attribute.DetailType).ToArray();
		}

		/// <summary>Compares the two operation sets: which operations exist, then each shared operation's signature and faults.</summary>
		/// <remarks>
		/// NOTE: what is compared is the operation set, the signatures, the action and one-way properties and the declared
		/// faults. Four things are not: [ServiceKnownType], base contract interfaces (GetMethods on an interface returns only
		/// the interface's own methods), and the ProtectionLevel settings on [ServiceContract] and [OperationContract] -
		/// none of the six interfaces uses any of them. A contract that starts to would pass here while differing on the
		/// wire, so widen this comparison with it; protocol-transport-security, which owns the secured-connection options,
		/// is the work item most likely to need the protection levels compared.
		/// </remarks>
		private static void CompareOperations(Type serverInterface, Dictionary<string, Operation> serverOperations, Dictionary<string, Operation> clientOperations, List<string> differences)
		{
			List<KnownFaultDifference> unmatchedKnownDifferences =
				KnownFaultDifferences.Where(known => known.Contract == serverInterface.Name).ToList();

			// The two directions are reported separately: an operation the client is missing and one the server is missing
			// are fixed on opposite sides.
			foreach (string name in serverOperations.Keys.Except(clientOperations.Keys).OrderBy(name => name, StringComparer.Ordinal))
				differences.Add(String.Format("{0}: the server declares operation \"{1}\" and the client does not.", serverInterface.Name, name));
			foreach (string name in clientOperations.Keys.Except(serverOperations.Keys).OrderBy(name => name, StringComparer.Ordinal))
				differences.Add(String.Format("{0}: the client declares operation \"{1}\" and the server does not.", serverInterface.Name, name));

			foreach (string name in serverOperations.Keys.Intersect(clientOperations.Keys).OrderBy(name => name, StringComparer.Ordinal))
			{
				Operation server = serverOperations[name];
				Operation client = clientOperations[name];

				CompareSignature(serverInterface, name, server, client, differences);
				CompareMessageExchange(serverInterface, name, server, client, differences);
				CompareFaults(serverInterface, name, server, client, unmatchedKnownDifferences, differences);
			}

			foreach (KnownFaultDifference known in unmatchedKnownDifferences)
				differences.Add(String.Format("{0}.{1}: the recorded fault difference (server only: {2}; client only: {3}) is no longer there. If it was fixed, remove its entry from KnownFaultDifferences; if a declaration was dropped from the other side instead, put it back.", known.Contract, known.Operation, FormatTypeList(known.ServerOnly), FormatTypeList(known.ClientOnly)));
		}

		/// <summary>Compares one operation's parameter types, by position, and its return type.</summary>
		private static void CompareSignature(Type serverInterface, string name, Operation server, Operation client, List<string> differences)
		{
			if (server.ParameterTypes.Length != client.ParameterTypes.Length)
				differences.Add(String.Format("{0}.{1}: the server takes {2} parameter(s) ({3}), the client {4} ({5}).", serverInterface.Name, name, server.ParameterTypes.Length, FormatTypeList(server.ParameterTypes), client.ParameterTypes.Length, FormatTypeList(client.ParameterTypes)));
			else
				for (int index = 0; index < server.ParameterTypes.Length; index++)
					// Compared by type identity, not by name: two same-named types from different assemblies are exactly the
					// drift this test is for, so the message names each type's assembly.
					if (server.ParameterTypes[index] != client.ParameterTypes[index])
						differences.Add(String.Format("{0}.{1}: parameter {2} differs - server {3}, client {4}.", serverInterface.Name, name, index + 1, FormatTypeWithAssembly(server.ParameterTypes[index]), FormatTypeWithAssembly(client.ParameterTypes[index])));

			if (server.ReturnType != client.ReturnType)
				differences.Add(String.Format("{0}.{1}: the return type differs - server {2}, client {3}.", serverInterface.Name, name, FormatTypeWithAssembly(server.ReturnType), FormatTypeWithAssembly(client.ReturnType)));
		}

		/// <summary>Compares the [OperationContract] properties that decide the operation's action URIs and whether it carries a reply.</summary>
		/// <remarks>
		/// None of the six interfaces sets any of these, so each comparison here is between two unset values. They are
		/// compared because setting one on a single copy compiles on both sides and then mismatches on the wire, which is
		/// what this test exists to catch. An unset Action is compared as null rather than resolved to the URI WCF derives
		/// from it, because that URI is built from the contract identity and the operation name, both already compared.
		/// </remarks>
		private static void CompareMessageExchange(Type serverInterface, string name, Operation server, Operation client, List<string> differences)
		{
			if (server.Contract.Action != client.Contract.Action)
				differences.Add(String.Format("{0}.{1}: the request Action differs - server {2}, client {3}.", serverInterface.Name, name, FormatAction(server.Contract.Action), FormatAction(client.Contract.Action)));

			if (server.Contract.ReplyAction != client.Contract.ReplyAction)
				differences.Add(String.Format("{0}.{1}: the ReplyAction differs - server {2}, client {3}.", serverInterface.Name, name, FormatAction(server.Contract.ReplyAction), FormatAction(client.Contract.ReplyAction)));

			if (server.Contract.IsOneWay != client.Contract.IsOneWay)
				differences.Add(String.Format("{0}.{1}: IsOneWay differs - server {2}, client {3}; a one-way operation has no reply message, so one side would wait for a reply the other never sends.", serverInterface.Name, name, server.Contract.IsOneWay, client.Contract.IsOneWay));
		}

		/// <summary>An action URI as declared, or a note that it is the one WCF derives.</summary>
		private static string FormatAction(string action)
		{
			return action == null ? "derived from the contract and operation name" : "\"" + action + "\"";
		}

		/// <summary>Compares the set of declared fault detail types, allowing only the differences recorded in <see cref="KnownFaultDifferences"/>.</summary>
		private static void CompareFaults(Type serverInterface, string name, Operation server, Operation client, List<KnownFaultDifference> unmatchedKnownDifferences, List<string> differences)
		{
			Type[] serverOnly = server.FaultDetailTypes.Except(client.FaultDetailTypes).ToArray();
			Type[] clientOnly = client.FaultDetailTypes.Except(server.FaultDetailTypes).ToArray();
			if ((serverOnly.Length == 0) && (clientOnly.Length == 0))
				return;

			KnownFaultDifference known =
				unmatchedKnownDifferences.FirstOrDefault
				(
					candidate =>
						(candidate.Operation == name)
							&& SameTypeSet(candidate.ServerOnly, serverOnly)
							&& SameTypeSet(candidate.ClientOnly, clientOnly)
				);
			if (known != null)
			{
				unmatchedKnownDifferences.Remove(known);
				return;
			}

			// A fault declared on one side only changes what the other side does with the error: without the declaration WCF
			// hands it over as a bare FaultException, and a catch of the typed fault never fires.
			differences.Add(String.Format("{0}.{1}: the declared faults differ - server {2}, client {3}.", serverInterface.Name, name, FormatTypeList(server.FaultDetailTypes), FormatTypeList(client.FaultDetailTypes)));
		}

		private static bool SameTypeSet(IEnumerable<Type> left, IEnumerable<Type> right)
		{
			return !left.Except(right).Any() && !right.Except(left).Any();
		}

		private static string FormatTypeList(IEnumerable<Type> types)
		{
			string text = String.Join(", ", types.Select(FormatType));
			return text.Length == 0 ? "none" : text;
		}

		private static string FormatTypeWithAssembly(Type type)
		{
			return type == null ? "none" : String.Format("{0} (from {1})", FormatType(type), type.Assembly.GetName().Name);
		}

		private static string FormatType(Type type)
		{
			if (type == null)
				return "none";
			if (type.IsByRef)
				return "ref " + FormatType(type.GetElementType());
			if (type.IsArray)
				return FormatType(type.GetElementType()) + "[]";
			if (type.IsGenericType)
				return String.Format("{0}<{1}>", type.Name.Substring(0, type.Name.IndexOf('`')), String.Join(", ", type.GetGenericArguments().Select(FormatType)));
			return type.Name;
		}
	}
}
