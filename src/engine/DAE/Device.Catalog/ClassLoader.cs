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
		public const string DAERegisterGetClassesMethodName = "GetClasses";

		// Assemblies
		private RegisteredAssemblies _assemblies = new RegisteredAssemblies();
		public RegisteredAssemblies Assemblies { get { return _assemblies; } }
		
		// Classes
		private RegisteredClasses _classes = new RegisteredClasses();
		public RegisteredClasses Classes { get { return _classes; } }
		
		// OnMiss
		public event ClassLoaderMissedEvent OnMiss;
		private void DoMiss(CatalogDeviceSession session, ClassDefinition classDefinition)
		{
			if (OnMiss != null)
				OnMiss(this, session, classDefinition);
		}
		
		// CreateObject
		public static void SetProperty(object objectValue, string propertyName, string propertyValue)
		{
			PropertyInfo propertyInfo = objectValue.GetType().GetProperty(propertyName);
			if (propertyInfo == null)
				throw new Alphora.Dataphor.DAE.Server.ServerException(Alphora.Dataphor.DAE.Server.ServerException.Codes.PropertyNotFound, objectValue.GetType().Name, propertyName);
			propertyInfo.SetValue(objectValue, Convert.ChangeType(propertyValue, propertyInfo.PropertyType, System.Threading.Thread.CurrentThread.CurrentCulture), null);
		}
		
		/// <summary>The value of a named property of an object, in the string form SetProperty takes; false if the object exposes no readable property of that name.</summary>
		/// <remarks>
		/// The read counterpart to SetProperty, for recording an object's settings before a class definition alteration overwrites
		/// them. It answers false rather than raising PropertyNotFound because such a recording runs before the alteration it is to
		/// undo: a name the alteration itself would reject has to go on failing where it failed before, not in the recording.
		/// </remarks>
		public static bool TryGetProperty(object objectValue, string propertyName, out string propertyValue)
		{
			PropertyInfo propertyInfo = objectValue.GetType().GetProperty(propertyName);
			if ((propertyInfo == null) || !propertyInfo.CanRead)
			{
				propertyValue = null;
				return false;
			}
			object value = propertyInfo.GetValue(objectValue, null);
			propertyValue = value == null ? null : (string)Convert.ChangeType(value, typeof(string), System.Threading.Thread.CurrentThread.CurrentCulture);
			return true;
		}
		
		public object CreateObject(CatalogDeviceSession session, ClassDefinition classDefinition, object[] actualParameters)
		{
			object objectValue = Activator.CreateInstance(CreateType(session, classDefinition), actualParameters);

			foreach (ClassAttributeDefinition attribute in classDefinition.Attributes)
				SetProperty(objectValue, attribute.AttributeName, attribute.AttributeValue);
			return objectValue;
		}
		
		public Type CreateType(CatalogDeviceSession session, ClassDefinition classDefinition)
		{
			RegisteredClass classValue = GetClass(session, classDefinition);

			string className = classValue.ClassName;
			try
			{
				return Type.GetType(className, true);
			}
			catch (Exception E)
			{
				throw new ServerException(ServerException.Codes.ClassLoadError, E, className);
			}
		}
		
		public RegisteredClass GetClass(CatalogDeviceSession session, ClassDefinition classDefinition)
		{
			if (!_classes.Contains(classDefinition.ClassName))
				DoMiss(session, classDefinition);
				
			return _classes[classDefinition.ClassName];
		}

		// Assemblies
		private string GetRegisterClassName(Assembly assembly)
		{
			object[] attributes = assembly.GetCustomAttributes(true);
			foreach (object objectValue in attributes)
				if (objectValue is DAERegisterAttribute)
					return ((DAERegisterAttribute)objectValue).RegisterClassName;
			return String.Empty;
		}
		
		private SettingsList GetClassList(Assembly assembly)
		{
			string className = GetRegisterClassName(assembly);
			if ((className == null) || (className == String.Empty))
				throw new ServerException(ServerException.Codes.RegisterClassNameNotFound, assembly.FullName);

			Type type = assembly.GetType(className, true);
			System.Reflection.MethodInfo methodInfo = type.GetMethod(DAERegisterGetClassesMethodName);
			return (SettingsList)methodInfo.Invoke(null, new object[]{});
		}
		
		public SettingsList RegisterAssembly(LoadedLibrary library, Assembly assembly)
		{
			lock (_assemblies)
			{
				try
				{
					RegisteredAssembly localAssembly = new RegisteredAssembly(assembly, library);
					if (!_assemblies.Contains(assembly.FullName))
					{
						SettingsList classes = GetClassList(assembly);
						foreach (SettingsItem setting in classes.Values)
						{
							RegisteredClass classValue = new RegisteredClass(setting.Name, library, localAssembly, setting.Value);
							_classes.Add(classValue);
						}
						_assemblies.Add(localAssembly);
						return classes;
					}
					
					return new SettingsList();
				}
				catch
				{
					UnregisterAssembly(library, assembly);
					throw;
				}
			}
		}
        
		public SettingsList UnregisterAssembly(LoadedLibrary library, Assembly assembly)
		{
			lock (_assemblies)
			{
				SettingsList classes = GetClassList(assembly);
				foreach (SettingsItem setting in classes.Values)
					if (_classes.Contains(setting.Name))
					{
						Schema.RegisteredClass classValue = _classes[setting.Name];
						_classes.RemoveAt(_classes.IndexOf(setting.Name));
					}
					
				int index = _assemblies.IndexOf(assembly.FullName);
				if (index >= 0)
					_assemblies.RemoveAt(index);
					
				return classes;
			}
		}
	}
}
