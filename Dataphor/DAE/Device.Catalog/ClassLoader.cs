/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Server;
	
	public delegate void ClassLoaderMissedEvent(ClassLoader AClassLoader, CatalogDeviceSession ASession, ClassDefinition AClassDefinition);
	
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
		private void DoMiss(CatalogDeviceSession ASession, ClassDefinition AClassDefinition)
		{
			if (OnMiss != null)
				OnMiss(this, ASession, AClassDefinition);
		}
		
		// CreateObject
		public static void SetProperty(object AObject, string APropertyName, string APropertyValue)
		{
			PropertyInfo LPropertyInfo = AObject.GetType().GetProperty(APropertyName);
			if (LPropertyInfo == null)
				throw new Alphora.Dataphor.DAE.Server.ServerException(Alphora.Dataphor.DAE.Server.ServerException.Codes.PropertyNotFound, AObject.GetType().Name, APropertyName);
			LPropertyInfo.SetValue(AObject, Convert.ChangeType(APropertyValue, LPropertyInfo.PropertyType, System.Threading.Thread.CurrentThread.CurrentCulture), null);
		}
		
		public object CreateObject(CatalogDeviceSession ASession, ClassDefinition AClassDefinition, object[] AActualParameters)
		{
			object LObject = Activator.CreateInstance(CreateType(ASession, AClassDefinition), AActualParameters);

			foreach (ClassAttributeDefinition LAttribute in AClassDefinition.Attributes)
				SetProperty(LObject, LAttribute.AttributeName, LAttribute.AttributeValue);
			return LObject;
		}
		
		public Type CreateType(CatalogDeviceSession ASession, ClassDefinition AClassDefinition)
		{
			RegisteredClass LClass = GetClass(ASession, AClassDefinition);

			string LClassName = LClass.ClassName;
			try
			{
				return Type.GetType(LClassName, true, true);
			}
			catch (Exception E)
			{
				throw new ServerException(ServerException.Codes.ClassLoadError, E, LClassName);
			}
		}
		
		public RegisteredClass GetClass(CatalogDeviceSession ASession, ClassDefinition AClassDefinition)
		{
			if (!FClasses.Contains(AClassDefinition.ClassName))
				DoMiss(ASession, AClassDefinition);
				
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

			Type LType = AAssembly.GetType(LClassName, true);
			System.Reflection.MethodInfo LMethodInfo = LType.GetMethod(CDAERegisterGetClassesMethodName);
			return (SettingsList)LMethodInfo.Invoke(null, new object[]{});
		}
		
		public SettingsList RegisterAssembly(LoadedLibrary ALibrary, Assembly AAssembly)
		{
			lock (FAssemblies)
			{
				try
				{
					RegisteredAssembly LAssembly = new RegisteredAssembly(AAssembly, ALibrary);
					if (!FAssemblies.Contains(AAssembly.FullName))
					{
						SettingsList LClasses = GetClassList(AAssembly);
						foreach (SettingsItem LSetting in LClasses.Values)
						{
							RegisteredClass LClass = new RegisteredClass(LSetting.Name, ALibrary, LAssembly, LSetting.Value);
							FClasses.Add(LClass);
						}
						FAssemblies.Add(LAssembly);
						return LClasses;
					}
					
					return new SettingsList();
				}
				catch
				{
					UnregisterAssembly(ALibrary, AAssembly);
					throw;
				}
			}
		}
        
		public SettingsList UnregisterAssembly(LoadedLibrary ALibrary, Assembly AAssembly)
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
					
				int LIndex = FAssemblies.IndexOf(AAssembly.FullName);
				if (LIndex >= 0)
					FAssemblies.RemoveAt(LIndex);
					
				return LClasses;
			}
		}
	}
}
