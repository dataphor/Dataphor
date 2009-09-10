/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Windows
{
	using System;
	using System.Reflection;
	
	/// <summary> This class hooks the AppDomain's AssemblyResolve and attemts to match the dynamic assembly name against a staticly linked assembly. </summary>
	/// <remarks>
	///		The AssemblyResolve is hooked in the static constructor, so use 
	///		AssemblyUtility.GetType() to  ensure that this constructor has
	///		executed.
	///	</remarks>
	public class AssemblyUtility
	{
		// Error code = A01XXX
		static AssemblyUtility()
		{
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
		}

		public static Assembly ResolveAssembly(object AObject, ResolveEventArgs AArgs)
		{
			foreach (Assembly LAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				AssemblyName LAssemblyName = LAssembly.GetName();
				if ((String.Compare(LAssemblyName.ToString(), AArgs.Name, true) == 0) || (String.Compare(LAssemblyName.Name, AArgs.Name, true) == 0))
					return LAssembly;
			}
			return null;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		public static void Initialize()
		{
			// Dummy method to cause this class to be loaded
		}
	}
}