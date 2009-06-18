/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor
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
		static bool FInnerResolveAssembly;
		// Error code = A01XXX
		static AssemblyUtility()
		{
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
			FInnerResolveAssembly = false;
		}

		private static Assembly InnerResolveAssembly(Assembly AAssembly, string AAssemblyName)
		{
			FInnerResolveAssembly = true;
			Assembly LReturnAssembly;

			AssemblyName[] LAssemblies = AAssembly.GetReferencedAssemblies();
			for (int LIndex = 0; LIndex < LAssemblies.Length; LIndex++)
			{
				if (String.Compare(LAssemblies[LIndex].Name, AAssemblyName, true) == 0)
				{
					LReturnAssembly = Assembly.Load(LAssemblies[LIndex]);
					FInnerResolveAssembly = false;
					return LReturnAssembly;
				}
			}

			for (int LIndex = 0; LIndex < LAssemblies.Length; LIndex++)
			{
				LReturnAssembly = InnerResolveAssembly(Assembly.Load(LAssemblies[LIndex]), AAssemblyName);
				FInnerResolveAssembly = false;
				return LReturnAssembly;
			}
			
			FInnerResolveAssembly = false;
			return null;
		}
		
		public static Assembly ResolveAssembly(object AObject, ResolveEventArgs AArgs)
		{
			Assembly LResolvedAssembly;

			if (FInnerResolveAssembly == true)
				return null;

			foreach (Assembly LAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				AssemblyName LAssemblyName = LAssembly.GetName();
				if ((String.Compare(LAssemblyName.ToString(), AArgs.Name, true) == 0) || (String.Compare(LAssemblyName.Name, AArgs.Name, true) == 0))
					return LAssembly;
				else
				{
					LResolvedAssembly = InnerResolveAssembly(LAssembly, AArgs.Name);
					if (LResolvedAssembly != null)
						return LResolvedAssembly;
				}
			}
			return null;
		}

		// TODO: Find a less obscure way to do this.

		/// <summary> Call this instead of <see cref="Type.GetType"/> to make sure that AssemblyUtility's static constructor is called. </summary>
		public static Type GetType(string ATypeName, bool AThrowOnError, bool AIgnoreCase) 
		{
			return Type.GetType(ATypeName, AThrowOnError, AIgnoreCase);
		}
	}
}