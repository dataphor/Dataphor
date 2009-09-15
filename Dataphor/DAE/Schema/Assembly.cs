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
				if (String.Compare(AName.FullName, this[LIndex].Name.FullName, true) == 0)
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

	public delegate void ClassLoaderMissedEvent(ClassLoader AClassLoader, ClassDefinition AClassDefinition);
	
	public class ClassLoader : System.Object
	{
		public const string CDAERegisterGetClassesMethodName = "GetClasses";

		// Assemblies
		private RegisteredAssemblies FAssemblies = new RegisteredAssemblies();
		public RegisteredAssemblies Assemblies { get { return FAssemblies; } }
		
		// Classes
		private RegisteredClasses FClasses = new RegisteredClasses();
		public RegisteredClasses Classes { get { return FClasses; } }
		
		// OnMiss
		public event ClassLoaderMissedEvent OnMiss;
		private void DoMiss(ClassDefinition AClassDefinition)
		{
			if (OnMiss != null)
				OnMiss(this, AClassDefinition);
		}
        
		// CreateObject
		public static void SetProperty(object AObject, string APropertyName, string APropertyValue)
		{
			PropertyInfo LPropertyInfo = AObject.GetType().GetProperty(APropertyName);
			if (LPropertyInfo == null)
				throw new Alphora.Dataphor.DAE.Server.ServerException(Alphora.Dataphor.DAE.Server.ServerException.Codes.PropertyNotFound, AObject.GetType().Name, APropertyName);
			LPropertyInfo.SetValue(AObject, Convert.ChangeType(APropertyValue, LPropertyInfo.PropertyType), null);
		}
		
		public object CreateObject(ClassDefinition AClassDefinition, object[] AActualParameters)
		{
			object LObject = Activator.CreateInstance(CreateType(AClassDefinition), AActualParameters);

			foreach (ClassAttributeDefinition LAttribute in AClassDefinition.Attributes)
				SetProperty(LObject, LAttribute.AttributeName, LAttribute.AttributeValue);
			return LObject;
		}
		
		public Type CreateType(ClassDefinition AClassDefinition)
		{
			RegisteredClass LClass = GetClass(AClassDefinition);

			string LClassName = LClass.ClassName;
			try
			{
				return Type.GetType(LClassName, true, true);
			}
			catch (Exception E)
			{
				throw new Alphora.Dataphor.DAE.Server.ServerException(Alphora.Dataphor.DAE.Server.ServerException.Codes.ClassLoadError, E, LClassName);
			}
		}
		
		public RegisteredClass GetClass(ClassDefinition AClassDefinition)
		{
			if (!FClasses.Contains(AClassDefinition.ClassName))
				DoMiss(AClassDefinition);
				
			return FClasses[AClassDefinition.ClassName];
		}

		// Assemblies
		private string GetRegisterClassName(Assembly AAssembly)
		{
			object[] LAttributes = AAssembly.GetCustomAttributes(true);
			foreach (object LObject in LAttributes)
				if (LObject is DAERegisterAttribute)
					return ((DAERegisterAttribute)LObject).RegisterClassName;
			return String.Empty;
		}
		
		private SettingsList GetClassList(Assembly AAssembly)
		{
			string LClassName = GetRegisterClassName(AAssembly);
			if ((LClassName == null) || (LClassName == String.Empty))
				throw new ServerException(ServerException.Codes.RegisterClassNameNotFound, AAssembly.FullName);

			Type LType = AAssembly.GetType(LClassName, true, true);
			System.Reflection.MethodInfo LMethodInfo = LType.GetMethod(CDAERegisterGetClassesMethodName);
			return (SettingsList)LMethodInfo.Invoke(null, new object[]{});
		}
		
		public void RegisterAssembly(LoadedLibrary ALibrary, Assembly AAssembly)
		{
			lock (FAssemblies)
			{
				try
				{
					RegisteredAssembly LAssembly = new RegisteredAssembly(AAssembly, ALibrary);
					if (!FAssemblies.Contains(LAssembly))
					{
						SettingsList LClasses = GetClassList(AAssembly);
						foreach (SettingsItem LSetting in LClasses.Values)
						{
							RegisteredClass LClass = new RegisteredClass(LSetting.Name, ALibrary, LAssembly, LSetting.Value);
							FClasses.Add(LClass);
						}
						
						FAssemblies.Add(LAssembly);
					}
				}
				catch
				{
					UnregisterAssembly(AAssembly);
					throw;
				}
			}
		}
        
		public void UnregisterAssembly(Assembly AAssembly)
		{
			lock (FAssemblies)
			{
				SettingsList LClasses = GetClassList(AAssembly);
				foreach (SettingsItem LSetting in LClasses.Values)
					if (FClasses.Contains(LSetting.Name))
					{
						Schema.RegisteredClass LClass = FClasses[LSetting.Name];
						FClasses.RemoveAt(FClasses.IndexOf(LSetting.Name));
					}
					
				int LIndex = FAssemblies.IndexOf(AAssembly.GetName());
				if (LIndex >= 0)
					FAssemblies.RemoveAt(LIndex);
			}
		}
	}
}