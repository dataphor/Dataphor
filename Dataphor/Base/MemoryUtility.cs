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
			private class ReferenceEqualityComparer : IEqualityComparer<object>
			{
				#region IEqualityComparer<object> Members
				
				public new bool Equals(object x, object y)
				{
					return Object.ReferenceEquals(x, y);
				}

				public int GetHashCode(object obj)
				{
					return obj.GetHashCode();
				}

				#endregion
			}

			private Dictionary<Object, Object> FVisitedObjects = new Dictionary<Object, Object>(new ReferenceEqualityComparer());
			
			public bool Visit(Object AObject)
			{
				if (AObject == null)
					return false;
					
				if (AObject is System.Reflection.Pointer)
					return false; // Unmanaged pointer, do not follow
					
				Object LVisitedObject;
				if (FVisitedObjects.TryGetValue(AObject, out LVisitedObject))
					return false;
					
				FVisitedObjects.Add(AObject, AObject);
				return true;
			}
		}
		
		/// <summary>
		/// Returns the estimated allocation size in bytes of an object of the given type.
		/// </summary>
		/// <param name="AType"></param>
		/// <returns></returns>
		public static int SizeOf(Type AType)
		{
			switch (AType.FullName)
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
			if (AType.IsValueType)
			{
				int LResult = 0;
				foreach (FieldInfo LFieldInfo in AType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
					LResult += SizeOf(LFieldInfo.FieldType);
				return LResult;
			}
			
			// This is a reference type, return the size of a pointer
			return 4;
		}
		
		/// <summary>
		/// Returns true if the given type is a simple type (built-in .NET type such as bool, byte, or int), false otherwise.
		/// </summary>
		public static bool IsSimpleType(Type AType)
		{
			switch (AType.FullName)
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
		
		private static int InternalSizeOf(object AObject, TraversalMode ATraversalMode, ObjectContext AObjectContext)
		{
			if (AObject == null)
				return 0;
				
			Type LType = AObject.GetType();
				
			switch (LType.FullName)
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
				case "System.String" : return ((string)AObject).Length * 2 + 16; // Interned strings?
			}
			
			if (LType.IsArray)
			{
				Array LArray = (Array)AObject;
				
				if (LArray.Rank > 1)
					throw new NotSupportedException("Multi-dimension arrays not supported");
				
				Type LElementType = LType.GetElementType();
				if (IsSimpleType(LElementType))
					return LArray.Length * SizeOf(LElementType) + 32;
				
				int LResult = LArray.Length * SizeOf(LElementType) + 32; // Size of array itself

				if (ATraversalMode != TraversalMode.Shallow)
				{
					// Traverse the array elements
					for (int LIndex = 0; LIndex < LArray.Length; LIndex++)
					{
						object LValue = LArray.GetValue(LIndex);
						if (LValue != null)
						{
							Type LValueType = LValue.GetType();
							if (LValueType.IsValueType)
							{
								if (!IsSimpleType(LValueType))
									LResult += InternalSizeOf(LValue, ATraversalMode, AObjectContext);
							}
							else
							{
								if (AObjectContext.Visit(LValue))
									LResult += InternalSizeOf(LValue, ATraversalMode, AObjectContext);
							}
						}
					}
				}

				return LResult;
			}
			
			{
				int LResult = 12; // base object size
				while (LType != null)
				{
					FieldInfo[] LFieldInfos = LType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (FieldInfo LFieldInfo in LFieldInfos)
					{
						LResult += SizeOf(LFieldInfo.FieldType);
						
						bool LShouldTraverse = (ATraversalMode == TraversalMode.Deep) || ((ATraversalMode == TraversalMode.Default) && !LFieldInfo.IsDefined(typeof(ReferenceAttribute), false));
						
						object LFieldValue = LFieldInfo.GetValue(AObject);
						if (LFieldValue != null)
						{
							Type LFieldValueType = LFieldValue.GetType();
							if (LFieldValueType.IsValueType)
							{
								if (!IsSimpleType(LFieldValueType) && LShouldTraverse)
									LResult += InternalSizeOf(LFieldValue, ATraversalMode, AObjectContext);
							}
							else if ((LFieldValueType.IsArray || (LFieldValueType.GetInterface("IEnumerable") != null)) && AObjectContext.Visit(LFieldValue))
							{
								LResult += InternalSizeOf(LFieldValue, LShouldTraverse ? ATraversalMode : TraversalMode.Shallow, AObjectContext);
							}
							else
							{	
								if (LShouldTraverse && AObjectContext.Visit(LFieldValue))
									LResult += InternalSizeOf(LFieldValue, ATraversalMode, AObjectContext);
							}
						}
					}
					
					LType = LType.BaseType;
				}
				return LResult;
			}
		}
		
		public static int SizeOf(object AObject, TraversalMode ATraversalMode)
		{
			ObjectContext LObjectContext = new ObjectContext();
			LObjectContext.Visit(AObject);
			return InternalSizeOf(AObject, ATraversalMode, LObjectContext);
		}
		
		public static List<FieldSizeInfo> SizesOf(object AObject, TraversalMode ATraversalMode)
		{
			if (AObject == null)
				return new List<FieldSizeInfo>();
				
			Type LType = AObject.GetType();
			
			if (IsSimpleType(LType) || (LType.FullName == "System.String") || LType.IsArray)
				return new List<FieldSizeInfo>() { new FieldSizeInfo(LType.FullName, "<object>", LType.FullName, SizeOf(AObject, ATraversalMode)) };
			
			List<FieldSizeInfo> LResult = new List<FieldSizeInfo>();
			ObjectContext LObjectContext = new ObjectContext();
			LObjectContext.Visit(AObject);
			while (LType != null)
			{
				FieldInfo[] LFieldInfos = LType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo LFieldInfo in LFieldInfos)
					LResult.Add
					(
						new FieldSizeInfo
						(
							LType.FullName, 
							LFieldInfo.Name, 
							LFieldInfo.FieldType.FullName, 
							((ATraversalMode == TraversalMode.Shallow) || ((ATraversalMode == TraversalMode.Default) && LFieldInfo.IsDefined(typeof(ReferenceAttribute), false)))
								? SizeOf(LFieldInfo.FieldType)
								: InternalSizeOf(LFieldInfo.GetValue(AObject), ATraversalMode, LObjectContext)
						)
					);
				LType = LType.BaseType;
			}
			return LResult;
		}
	}
}
