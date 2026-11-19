/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Windows
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Runtime.Loader;

	/// <summary>
	/// Resolves a library assembly's private managed and native dependencies from the library's own folder in the
	/// runtime layout, and from the folders of the libraries it requires. An assembly the server loads for a D4 library
	/// is registered here together with its probe folders; the two AppDomain hooks look up the requesting assembly and
	/// probe those folders. Assemblies not registered here fall through to the default load path (the host's bin folder).
	/// </summary>
	/// <remarks>
	/// Installed once by <see cref="AssemblyUtility"/>'s static constructor, alongside the by-name-against-loaded-assemblies
	/// hook. No per-library load context, no unloading: a registered assembly stays loaded for the process lifetime, and
	/// Unregister only removes the table entry so a later load from a new location is not treated as the old library's.
	/// </remarks>
	public static class LibraryAssemblyResolver
	{
		private sealed class ProbeInfo
		{
			public ProbeInfo(IReadOnlyList<string> folders)
			{
				Folders = folders;
			}

			/// <summary>Folders to probe for this assembly, in order: the owning library's folder first, then the folders
			/// of the libraries it requires (deduplicated, in the required-libraries order the library declares).</summary>
			public IReadOnlyList<string> Folders { get; }
		}

		private static readonly ConcurrentDictionary<Assembly, ProbeInfo> _probeInfo = new ConcurrentDictionary<Assembly, ProbeInfo>();

		private static int _installed;

		/// <summary>Installs the two resolver hooks. Idempotent; safe to call from multiple static constructors.</summary>
		internal static void Install()
		{
			if (System.Threading.Interlocked.Exchange(ref _installed, 1) != 0)
				return;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveManaged;
			AssemblyLoadContext.Default.ResolvingUnmanagedDll += ResolveUnmanaged;
		}

		/// <summary>Records which folders to probe for an assembly the server has loaded for a D4 library. Overwrites an
		/// existing entry so that re-registration after Unregister is a replace, not an add.</summary>
		public static void Register(Assembly assembly, IReadOnlyList<string> folders)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			if (folders == null) throw new ArgumentNullException(nameof(folders));
			_probeInfo[assembly] = new ProbeInfo(folders);
		}

		/// <summary>Removes the assembly's probe entry. The assembly itself stays loaded; later probes through it miss and
		/// fall through, as for any assembly outside a library folder.</summary>
		public static void Unregister(Assembly assembly)
		{
			if (assembly == null) return;
			_probeInfo.TryRemove(assembly, out _);
		}

		private static Assembly ResolveManaged(object sender, ResolveEventArgs args)
		{
			Assembly requesting = args.RequestingAssembly;
			if (requesting == null)
				return null;
			if (!_probeInfo.TryGetValue(requesting, out ProbeInfo info))
				return null;

			string simpleName;
			try
			{
				simpleName = new AssemblyName(args.Name).Name;
			}
			catch (FileLoadException)
			{
				return null;
			}
			if (String.IsNullOrEmpty(simpleName))
				return null;

			string fileName = simpleName + ".dll";
			foreach (string folder in info.Folders)
			{
				string candidate = Path.Combine(folder, fileName);
				if (File.Exists(candidate) && FileUtility.IsAssembly(candidate))
				{
					// LoadFrom on a copy of an already-loaded assembly returns the existing instance, so a private package
					// duplicated in the host's bin folder resolves once per process no matter which library folder holds it.
					return Assembly.LoadFrom(candidate);
				}
			}
			return null;
		}

		private static IntPtr ResolveUnmanaged(Assembly requestingAssembly, string unmanagedDllName)
		{
			if (requestingAssembly == null || String.IsNullOrEmpty(unmanagedDllName))
				return IntPtr.Zero;
			if (!_probeInfo.TryGetValue(requestingAssembly, out ProbeInfo info))
				return IntPtr.Zero;

			foreach (string folder in info.Folders)
			{
				if (TryLoadNative(Path.Combine(folder, unmanagedDllName), out IntPtr handle))
					return handle;
				if (!unmanagedDllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
					&& TryLoadNative(Path.Combine(folder, unmanagedDllName + ".dll"), out handle))
					return handle;
			}
			// Miss: let the runtime raise DllNotFoundException so the error carries the original DLL name.
			return IntPtr.Zero;
		}

		private static bool TryLoadNative(string path, out IntPtr handle)
		{
			if (!File.Exists(path))
			{
				handle = IntPtr.Zero;
				return false;
			}
			return NativeLibrary.TryLoad(path, out handle);
		}
	}
}
