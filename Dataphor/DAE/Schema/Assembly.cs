/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Reflection;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Schema
{
	public class RegisteredAssembly : System.Object
	{
		public RegisteredAssembly(Assembly AAssembly, LoadedLibrary ALibrary) : base()
		{
			FName = AAssembly.GetName();
			FAssembly = AAssembly;
			FLibrary = ALibrary;
		}
		
		private AssemblyName FName;
		public AssemblyName Name { get { return FName; } }
		
		[Reference]
		private Assembly FAssembly;
		public Assembly Assembly { get { return FAssembly; } }
		
		[Reference]
		private LoadedLibrary FLibrary;
		public LoadedLibrary Library { get { return FLibrary; } }
		
		public override int GetHashCode()
		{
			return FName.GetHashCode();
		}
		
		public override bool Equals(object AObject)
		{
			return FName.Equals(AObject);
		}
	}
	
	public class RegisteredAssemblies : List<RegisteredAssembly>
	{
		public RegisteredAssembly this[AssemblyName AName] { get { return this[IndexOf(AName)]; } }
		
		public int IndexOf(AssemblyName AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Equals(AName.FullName, this[LIndex].Name.FullName, StringComparison.OrdinalIgnoreCase))
					return LIndex;
			return -1;
		}
		
		public bool Contains(AssemblyName AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class RegisteredClass : Schema.Object
	{
		public RegisteredClass(string AName, LoadedLibrary ALibrary, RegisteredAssembly AAssembly, string AClassName) : base(AName) 
		{
			FLibrary = ALibrary;
			FAssembly = AAssembly;
			FClassName = AClassName;
		}
		
		[Reference]
		private RegisteredAssembly FAssembly;
		public RegisteredAssembly Assembly { get { return FAssembly; } }
		
		private string FClassName;
		public string ClassName 
		{
			get { return FClassName; } 
			set { FClassName = value; }
		}
	}
	
	public class RegisteredClasses : Schema.Objects
	{
		public RegisteredClasses() : base() {}
		
		public new RegisteredClass this[int AIndex]
		{
			get { return (RegisteredClass)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public new RegisteredClass this[string AName]
		{
			get 
			{ 
				int LIndex = IndexOf(AName);
				if (LIndex >= 0)
					return this[LIndex];
				else
					throw new SchemaException(SchemaException.Codes.RegisteredClassNotFound, AName);
			}
			set 
			{ 
				int LIndex = IndexOf(AName);
				if (LIndex >= 0)
					this[LIndex] = value;
				else
					throw new SchemaException(SchemaException.Codes.RegisteredClassNotFound, AName);
			}
		}
	}
}