/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Diagnostics.LibraryLoadingProbe
{
	using System;
	using System.Runtime.InteropServices;

	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	using Newtonsoft.Json.Bson;

	/// <summary>
	/// Proves the managed resolver by referencing a type from a private NuGet package that does not sit in the host's bin
	/// directory. The static field forces the CLR to resolve Newtonsoft.Json.Bson when this node first runs; the resolver
	/// finds the assembly flat in this library's folder and loads it from there. A successful return is proof.
	/// </summary>
	public class ProbeManagedNode : InstructionNode
	{
		// Static readonly, not a constant: used instead of a local to force the dependency load at class-init time rather
		// than at method JIT, so a resolver failure surfaces here rather than inside InternalExecute.
		private static readonly Type _bsonType = typeof(BsonDataWriter);

		public override object InternalExecute(Program program, object[] arguments)
		{
			return _bsonType.Assembly.GetName().Name;
		}
	}

	/// <summary>
	/// Proves the native resolver. The library's folder gets Microsoft.Data.SqlClient.SNI.dll (a real native dll) flat,
	/// through the layout's RID flattening of the private SNI runtime package; a load through this assembly's load context
	/// trips the unmanaged resolver, which finds the file in the folder. The handle is freed; the operator returns a short
	/// success string so an assertion can compare its value.
	/// </summary>
	public class ProbeNativeNode : InstructionNode
	{
		private const string NativeLibraryName = "Microsoft.Data.SqlClient.SNI";

		public override object InternalExecute(Program program, object[] arguments)
		{
			IntPtr handle = NativeLibrary.Load(NativeLibraryName, typeof(ProbeNativeNode).Assembly, null);
			try
			{
				return NativeLibraryName;
			}
			finally
			{
				NativeLibrary.Free(handle);
			}
		}
	}
}
