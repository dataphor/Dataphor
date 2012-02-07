/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Alphora.Dataphor
{
	/// <summary>
	/// Defines the traversal mode used in determining an object's size.
	/// </summary>
	public enum TraversalMode 
	{ 
		/// <summary>
		/// Considers only the object's size.
		/// </summary>
		Shallow, 
		
		/// <summary>
		/// Deep, with the exception that the presence of a ReferenceAttribute will prevent traversal into that member.
		/// </summary>
		Default, 
		
		/// <summary>
		/// Considers the object, and all object's reachable from the object.
		/// </summary>
		Deep 
	}
	
	public struct FieldSizeInfo
	{
		public FieldSizeInfo(string ADeclaringType, string AFieldName, string AFieldType, int AFieldSize)
		{
			DeclaringType = ADeclaringType;
			FieldName = AFieldName;
			FieldType = AFieldType;
			FieldSize = AFieldSize;
		}

		public string DeclaringType;
		public string FieldName;
		public string FieldType;
		public int FieldSize;
	}
	
	/// <summary>
	/// When used on a field, indicates that the field contains a reference and should not be traversed to determine the size of the instance.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class ReferenceAttribute : Attribute
	{
		public ReferenceAttribute() : base() { }
	}
	
	/// <summary>
	/// Provides static methods for investigating the run-time size in bytes of instances.
	/// </summary>
	public static class MemoryUtility
	{
		private class ObjectContext
		{
			private Dictionary<Object, Object> _visitedObjects = new Dictionary<Object, Object>(new ReferenceEqualityComparer());
			
			public bool Visit(Object objectValue)
			{
				if (objectValue == null)
					return false;
					
				#if !SILVERLIGHT					
				if (objectValue is System.Reflection.Pointer)
					return false; // Unmanaged pointer, do not follow
				#endif
				Object visitedObject;
				if (_visitedObjects.TryGetValue(objectValue, out visitedObject))
					return false;
					
				_visitedObjects.Add(objectValue, objectValue);
				return true;
			}
		}
		
		/// <summary>
		/// Returns the estimated allocation size in bytes of an object of the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int SizeOf(Type type)
		{
			switch (type.FullName)
			{
				case "System.Boolean" : return 1;
				case "System.Byte" : return 1;
				case "System.SByte" : return 1;
				case "System.Char" : return 1;
				case "System.Decimal" : return 16;
				case "System.Double" : return 8;
				case "System.Single" : return 4;
				case "System.Int32" : return 4;
				case "System.UInt32" : return 4;
				case "System.Int64" : return 8;
				case "System.UInt64" : return 8;
				case "System.Int16" : return 2;
				case "System.UInt16" : return 2;
			}
			
			// This is a user-defined struct, determine size of each field
			if (type.IsValueType)
			{
				int result = 0;
				foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
					result += SizeOf(fieldInfo.FieldType);
				return result;
			}
			
			// This is a reference type, return the size of a pointer
			return 4;
		}
		
		/// <summary>
		/// Returns true if the given type is a simple type (built-in .NET type such as bool, byte, or int), false otherwise.
		/// </summary>
		public static bool IsSimpleType(Type type)
		{
			switch (type.FullName)
			{
				case "System.Boolean" : 
				case "System.Byte" : 
				case "System.SByte" :
				case "System.Char" :
				case "System.Decimal" :
				case "System.Double" :
				case "System.Single" :
				case "System.Int32" :
				case "System.UInt32" :
				case "System.Int64" :
				case "System.UInt64" :
				case "System.Int16" :
				case "System.UInt16" : return true;
			}
			
			return false;
		}
		
		private static int InternalSizeOf(object objectValue, TraversalMode traversalMode, ObjectContext objectContext)
		{
			if (objectValue == null)
				return 0;
				
			Type type = objectValue.GetType();
				
			switch (type.FullName)
			{
				case "System.Boolean" : return 1;
				case "System.Byte" : return 1;
				case "System.SByte" : return 1;
				case "System.Char" : return 1;
				case "System.Decimal" : return 16;
				case "System.Double" : return 8;
				case "System.Single" : return 4;
				case "System.Int32" : return 4;
				case "System.UInt32" : return 4;
				case "System.Int64" : return 8;
				case "System.UInt64" : return 8;
				case "System.Int16" : return 2;
				case "System.UInt16" : return 2;
				case "System.String" : return ((string)objectValue).Length * 2 + 16; // Interned strings?
			}
			
			if (type.IsArray)
			{
				Array array = (Array)objectValue;
				
				if (array.Rank > 1)
					throw new NotSupportedException("Multi-dimension arrays not supported");
				
				Type elementType = type.GetElementType();
				if (IsSimpleType(elementType))
					return array.Length * SizeOf(elementType) + 32;
				
				int result = array.Length * SizeOf(elementType) + 32; // Size of array itself

				if (traversalMode != TraversalMode.Shallow)
				{
					// Traverse the array elements
					for (int index = 0; index < array.Length; index++)
					{
						object value = array.GetValue(index);
						if (value != null)
						{
							Type valueType = value.GetType();
							if (valueType.IsValueType)
							{
								if (!IsSimpleType(valueType))
									result += InternalSizeOf(value, traversalMode, objectContext);
							}
							else
							{
								if (objectContext.Visit(value))
									result += InternalSizeOf(value, traversalMode, objectContext);
							}
						}
					}
				}

				return result;
			}
			
			{
				int result = 12; // base object size
				while (type != null)
				{
					FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (FieldInfo fieldInfo in fieldInfos)
					{
						result += SizeOf(fieldInfo.FieldType);
						
						bool shouldTraverse = (traversalMode == TraversalMode.Deep) || ((traversalMode == TraversalMode.Default) && !fieldInfo.IsDefined(typeof(ReferenceAttribute), false));
						
						object fieldValue = fieldInfo.GetValue(objectValue);
						if (fieldValue != null)
						{
							Type fieldValueType = fieldValue.GetType();
							if (fieldValueType.IsValueType)
							{
								if (!IsSimpleType(fieldValueType) && shouldTraverse)
									result += InternalSizeOf(fieldValue, traversalMode, objectContext);
							}
							else if ((fieldValueType.IsArray || (fieldValueType.GetInterface("IEnumerable", false) != null)) && objectContext.Visit(fieldValue))
							{
								result += InternalSizeOf(fieldValue, shouldTraverse ? traversalMode : TraversalMode.Shallow, objectContext);
							}
							else
							{	
								if (shouldTraverse && objectContext.Visit(fieldValue))
									result += InternalSizeOf(fieldValue, traversalMode, objectContext);
							}
						}
					}
					
					type = type.BaseType;
				}
				return result;
			}
		}
		
		public static int SizeOf(object objectValue, TraversalMode traversalMode)
		{
			ObjectContext objectContext = new ObjectContext();
			objectContext.Visit(objectValue);
			return InternalSizeOf(objectValue, traversalMode, objectContext);
		}
		
		public static List<FieldSizeInfo> SizesOf(object objectValue, TraversalMode traversalMode)
		{
			if (objectValue == null)
				return new List<FieldSizeInfo>();
				
			Type type = objectValue.GetType();
			
			if (IsSimpleType(type) || (type.FullName == "System.String") || type.IsArray)
				return new List<FieldSizeInfo>() { new FieldSizeInfo(type.FullName, "<object>", type.FullName, SizeOf(objectValue, traversalMode)) };
			
			List<FieldSizeInfo> result = new List<FieldSizeInfo>();
			ObjectContext objectContext = new ObjectContext();
			objectContext.Visit(objectValue);
			while (type != null)
			{
				FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fieldInfos)
					result.Add
					(
						new FieldSizeInfo
						(
							type.FullName, 
							fieldInfo.Name, 
							fieldInfo.FieldType.FullName, 
							((traversalMode == TraversalMode.Shallow) || ((traversalMode == TraversalMode.Default) && fieldInfo.IsDefined(typeof(ReferenceAttribute), false)))
								? SizeOf(fieldInfo.FieldType)
								: InternalSizeOf(fieldInfo.GetValue(objectValue), traversalMode, objectContext)
						)
					);
				type = type.BaseType;
			}
			return result;
		}
	}
}
