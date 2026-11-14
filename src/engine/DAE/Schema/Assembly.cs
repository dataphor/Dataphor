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
		public RegisteredAssembly(Assembly assembly, LoadedLibrary library) : base()
		{
			_name = assembly.FullName;
			_assembly = assembly;
			_library = library;
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		[Reference]
		private Assembly _assembly;
		public Assembly Assembly { get { return _assembly; } }
		
		[Reference]
		private LoadedLibrary _library;
		public LoadedLibrary Library { get { return _library; } }
		
		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}
		
		public override bool Equals(object objectValue)
		{
			RegisteredAssembly localObjectValue = objectValue as RegisteredAssembly;
			return localObjectValue != null && _name.Equals(localObjectValue.Name);
		}
	}
	
	public class RegisteredAssemblies : List<RegisteredAssembly>
	{
		public RegisteredAssembly this[string name] { get { return this[IndexOf(name)]; } }
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Equals(name, this[index].Name, StringComparison.OrdinalIgnoreCase))
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class RegisteredClass : Schema.Object
	{
		public RegisteredClass(string name, LoadedLibrary library, RegisteredAssembly assembly, string className) : base(name) 
		{
			_library = library;
			_assembly = assembly;
			_className = className;
		}
		
		[Reference]
		private RegisteredAssembly _assembly;
		public RegisteredAssembly Assembly { get { return _assembly; } }
		
		private string _className;
		public string ClassName 
		{
			get { return _className; } 
			set { _className = value; }
		}
	}
	
	public class RegisteredClasses : Schema.Objects<RegisteredClass>
	{
		public RegisteredClasses() : base() {}
		
		public new RegisteredClass this[int index]
		{
			get { return (RegisteredClass)base[index]; }
			set { base[index] = value; }
		}
		
		public new RegisteredClass this[string name]
		{
			get 
			{ 
				int index = IndexOf(name);
				if (index >= 0)
					return this[index];
				else
					throw new SchemaException(SchemaException.Codes.RegisteredClassNotFound, name);
			}
			set 
			{ 
				int index = IndexOf(name);
				if (index >= 0)
					this[index] = value;
				else
					throw new SchemaException(SchemaException.Codes.RegisteredClassNotFound, name);
			}
		}
	}
}