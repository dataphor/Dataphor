/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;	
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

using NUnit.Framework;

using Alphora.Dataphor;

namespace Alphora.Dataphor.Common.Tests
{
	[TestFixture]
	public class FixedSizeCacheTest
	{
		[SetUp]
		public void TestSetUp()
		{
		   FTestEntry = 0;
		}

		[Test]
		public void Add()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++)
			{
				FTestCache.Add(GetKey(i), GetValue(i));
				Assert.AreEqual(i, FTestCache.Count);
				Assert.True(FTestCache.ContainsKey(GetKey(i)));
			}
			Assert.AreEqual(5, FTestCache.Count);

			FTestCache.Add(GetKey(FTestCache.Size + 1), GetValue(FTestCache.Size + 1));
			Assert.AreEqual(LCacheSize, FTestCache.Count);
			Assert.True(ValidateList());
		}

		[Test]
		public void ClearAndCount()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++)
			{
				FTestCache.Add(GetKey(i), GetValue(i));
				Assert.AreEqual(i, FTestCache.Count);			
			}
			Assert.AreEqual(LCacheSize, FTestCache.Count);

			FTestCache.Clear();
			Assert.AreEqual(0, FTestCache.Count);
			Assert.True(ValidateList());
		}

		[Test]
		public void ContainsKey()
		{
			int LCacheSize = 5;
			int LTestCacheItem = 3;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);

			Assert.False(FTestCache.ContainsKey(GetKey(LTestCacheItem)));

			for (int i = 1; i <= FTestCache.Size; i++) 			
				FTestCache.Add(GetKey(i), GetValue(i));			
			
			Assert.True(FTestCache.ContainsKey(GetKey(LTestCacheItem)));

			FTestCache.Clear();
			Assert.False(FTestCache.ContainsKey(GetKey(LTestCacheItem)));
			Assert.True(ValidateList());
		}			

		[Test]
		public void Reference()
		{
			string LReference = null;
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++)
			{
				LReference = FTestCache.Reference(GetKey(i), GetValue(i));
				Assert.AreEqual(i, FTestCache.Count);
				Assert.True(FTestCache.ContainsKey(GetKey(i)));
				Assert.IsNull(LReference);
			}
			Assert.AreEqual(5, FTestCache.Count);

			LReference = FTestCache.Reference(GetKey(FTestCache.Size + 1), GetValue(FTestCache.Size + 1));
			Assert.AreEqual(LCacheSize, FTestCache.Count);
			Assert.IsNotNull(LReference);
			Assert.True(ValidateList());
		}

		[Test]
		public void Remove()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);  
			for (int i = 1; i <= FTestCache.Size; i++)
				FTestCache.Add(GetKey(i), GetValue(i)); 

			int LTestKey = 3;
			FTestCache.Remove(GetKey(LTestKey));
			Assert.False(FTestCache.ContainsKey(GetValue(LTestKey)));
			Assert.AreEqual(LCacheSize -1, FTestCache.Count);

			LTestKey = 1; 
			FTestCache.Remove(GetKey(LTestKey));
			Assert.False(FTestCache.ContainsKey(GetValue(LTestKey)));
			Assert.AreEqual(LCacheSize - 2, FTestCache.Count);

			LTestKey = 5;
			FTestCache.Remove(GetKey(LTestKey));
			Assert.False(FTestCache.ContainsKey(GetValue(LTestKey)));
			Assert.AreEqual(LCacheSize - 3, FTestCache.Count);
			Assert.True(ValidateList());
		}

		[Test]
		public void Size()										   
		{
			FTestCache = new FixedSizeCache<string, string>(5);
			Assert.AreEqual(5, FTestCache.Size);
			Assert.True(ValidateList());
		}

		[Test]
		public void IIEnumerable()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++) 		
				FTestCache.Add(GetKey(i), GetValue(i));

			int LValueIndex = 0;
			foreach (string LValue in (IEnumerable<string>)FTestCache)
			{
				LValueIndex++; 
				Assert.AreEqual(GetValue(LValueIndex), LValue);				
			}

			Assert.AreEqual(FTestCache.Size, LValueIndex);

			LValueIndex = 0;
			foreach (FixedSizeCache<string, string>.Entry LEntry in (IEnumerable)FTestCache)
			{
				LValueIndex++;
				Assert.AreEqual(GetValue(LValueIndex), LEntry.Value);				
			}

			Assert.AreEqual(FTestCache.Size, LValueIndex);		
		}

		[Test]
		public void IIndexor()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);

			Assert.IsNull(FTestCache[GetKey(0)]);

			for (int i = 1; i <= FTestCache.Size; i++)
				FTestCache.Add(GetKey(i), GetValue(i));  

			for (int j = 1; j <= FTestCache.Size; j++)
				Assert.AreEqual(GetValue(j), FTestCache[GetKey(j)]);

			Assert.IsNull(FTestCache[GetKey(FTestCache.Size + 1)]);	 
		}

		[Test]
		public void InternalAdd()
		{
			try
			{
				ReflectionPermission LReflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess | ReflectionPermissionFlag.AllFlags);
				LReflectionPermission.Assert();

				FTestCache = new FixedSizeCache<string, string>(25);
				int LPreCutoffCount;
				FixedSizeCache<string, string>.Entry LHead;
				FixedSizeCache<string, string>.Entry LCutoff;
				FixedSizeCache<string, string>.Entry LTail;

				//disregarding CorrelatedReferencePeriod
				while (TestEntry < FTestCache.Size)
				{	 
					IncrementTestEntry();
					LPreCutoffCount = (int)(TestEntry * FixedSizeCache<string, string>.CDefaultCutoff);
					Assert.AreEqual(LPreCutoffCount, PreCutoffCount());

					LHead = GetHead();
					LCutoff = GetCutoff();
					LTail = GetTail();
					
					if (LPreCutoffCount == 0)
					{
						if (TestEntry == 1)
						{
							Assert.True(CompareEntry(TestEntry, LHead));
							Assert.True(CompareEntry(TestEntry, LCutoff));
							Assert.True(CompareEntry(TestEntry, LTail));
							Assert.False(GetPreCutoff(TestEntry));							
						}
						else
						{
							Assert.True(CompareEntry(TestEntry, LHead));
							Assert.True(CompareEntry(TestEntry, LCutoff));
							Assert.False(CompareEntry(TestEntry, LTail));
							Assert.False(GetPreCutoff(TestEntry));
						}
					}
					else if (((TestEntry - 1) % 3) == 0)
					{
						if (LPreCutoffCount == 1)
							Assert.True(CompareEntry(TestEntry, LHead)); 
						else
							Assert.False(CompareEntry(TestEntry, LHead));
						Assert.False(CompareEntry(TestEntry, LCutoff));
						Assert.False(CompareEntry(TestEntry, LTail));
						Assert.True(GetPreCutoff(TestEntry));  						
					}
					else
					{
						Assert.False(CompareEntry(TestEntry, LHead));
						Assert.True(CompareEntry(TestEntry, LCutoff));
						Assert.False(CompareEntry(TestEntry, LTail));
						Assert.False(GetPreCutoff(TestEntry));
					}				
				}	
				
				Assert.True(ValidateList());  								
			}
			finally
			{
				CodeAccessPermission.RevertAssert();	 
			}							
		}

		[Test]
		public void InternalReference()
		{
			try
			{
				ReflectionPermission LReflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess | ReflectionPermissionFlag.AllFlags);
				LReflectionPermission.Assert();

				// Entries don't get promoted until Correlated Period is exceeded and when they do they are promoted to Head
				FTestCache = new FixedSizeCache<string, string>(25);
				while (TestEntry < FTestCache.Size)		 				 
					IncrementTestEntry();
				FixedSizeCache<string, string>.Entry LEntry = GetHead().GetType().GetField(CPriorName, CFieldFlags).GetValue(GetHead()) as FixedSizeCache<string, string>.Entry;
				Assert.AreNotEqual(GetHead(), LEntry); 
				int LLocation = GetLocation(LEntry);
				for (int i = (FTestCache.Size - GetLastAccess(LEntry)); i < FixedSizeCache<string, string>.CDefaultCorrelatedReferencePeriod; i++)
				{
					FTestCache.Reference(LEntry.Key, LEntry.Value);
					Assert.AreEqual(LLocation, GetLocation(LEntry));
				}

				FTestCache.Reference(LEntry.Key, LEntry.Value);
				Assert.AreNotEqual(LLocation, GetLocation(LEntry));
				Assert.AreEqual(GetHead(), LEntry);
				Assert.True(ValidateList());

				// Entries PreCutoff get demoted to not PreCutoff 
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(25);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				LEntry = GetHead();
				FixedSizeCache<string, string>.Entry LReferenced;
				FixedSizeCache<string, string>.Entry LTail;
				
				Assert.True(GetPreCutoff(LEntry));
				int LPreCutoffCount = PreCutoffCount();
				int LMoveCount = 0;			
				while (LMoveCount < LPreCutoffCount)
				{
					LTail = GetTail();
					if (LEntry == LTail)
						LReferenced = LTail.GetType().GetField(CNextName, CFieldFlags).GetValue(LTail) as FixedSizeCache<string, string>.Entry;
					else
						LReferenced = LTail;   
					Assert.False(GetPreCutoff(LReferenced));
					FTestCache.Reference(LReferenced.Key, LReferenced.Value);
					if (GetPreCutoff(LReferenced))
						LMoveCount++;					
				}					
				Assert.False(GetPreCutoff(LEntry));
				Assert.True(ValidateList());
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
		}

		[Test]
		public void InternalRemove()
		{
			try
			{
				ReflectionPermission LReflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess | ReflectionPermissionFlag.AllFlags);
				LReflectionPermission.Assert();

				FTestCache = new FixedSizeCache<string, string>(25);
				while (TestEntry < FTestCache.Size)		 				 
					IncrementTestEntry();

				FTestCache.Remove(GetHead().Key);
				Assert.True(ValidateList());

				FTestCache.Remove(GetCutoff().Key);
				Assert.True(ValidateList());

				FTestCache.Remove(GetTail().Key);
				Assert.True(ValidateList());	 

				while (FTestCache.Count > 0)
				{
					FTestCache.Remove(GetCutoff().Key);
					Assert.True(ValidateList());
				}
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
		}

		[Test]
		public void RandomAccess()
		{
			try
			{
				ReflectionPermission LReflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess | ReflectionPermissionFlag.AllFlags);
				LReflectionPermission.Assert();

				for (int j = 0; j < 50; j++)
				{
					FTestCache = new FixedSizeCache<string, string>(50);
					Random LRandom = new Random();
					int LEntry;
					for (int i = 0; i < 100000; i++)
					{
						LEntry = LRandom.Next(1, 100);
						switch (LRandom.Next(1, 10))
						{
							case 1:
							case 2:
							case 3:
								FTestCache.Add(GetKey(LEntry), GetValue(LEntry));
								break;
							case 4:
							case 5:
							case 6:
							case 7:
							case 8:
								FTestCache.Reference(GetKey(LEntry), GetValue(LEntry));
								break;
							default:
								FTestCache.Remove(GetKey(LEntry));
								break;
						}
					}
				}
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
		}

		#region Helpers

		private FixedSizeCache<string, string> FTestCache;

		private int FTestEntry = 0;
		private void IncrementTestEntry()
		{
			FTestEntry++;
			FTestCache.Add(GetKey(FTestEntry), GetValue(FTestEntry));
		}
		private int TestEntry
		{ get { return FTestEntry; } }

		private string FromTemplate(string AValue, string ATemplate)
		{
			return String.Format(ATemplate, AValue);
		}

		private const string CKeyTemplate = "Key{0}";
		private string GetKey(string AKey)
		{
			return FromTemplate(AKey, CKeyTemplate);
		}

		private string GetKey(int AKey)
		{
			return GetKey(AKey.ToString());
		}

		private const string CValueTemplate = "Value{0}";
		private string GetValue(string AValue)
		{
			return FromTemplate(AValue, CValueTemplate);
		}

		private string GetValue(int AValue)
		{
			return GetValue(AValue.ToString());
		}

		private bool CompareEntry(int ANumber, FixedSizeCache<string, string>.Entry AEntry)
		{
			if (AEntry == null || GetKey(ANumber) != AEntry.Key || GetValue(ANumber) != AEntry.Value)
				return false;
			return true;
		}

		const BindingFlags CFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
		const string CHeadName = "FLRUHead";
		private Type FFixedSizeCacheType;
		private FixedSizeCache<string, string>.Entry GetHead()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return FFixedSizeCacheType.GetField(CHeadName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
		}

		const string CCutoffName = "FLRUCutoff";
		private FixedSizeCache<string, string>.Entry GetCutoff()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return FFixedSizeCacheType.GetField(CCutoffName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
		}

		const string CTailName = "FLRUTail";
		private FixedSizeCache<string, string>.Entry GetTail()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return FFixedSizeCacheType.GetField(CTailName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
		}

		const string CPriorName = "FPrior";
		private FixedSizeCache<string, string>.Entry GetEntry(int AKey)
		{
			FixedSizeCache<string, string>.Entry LEntry = GetHead();
			FixedSizeCache<string, string>.Entry LPrior = LEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;
			while (LEntry.Key != GetKey(AKey) && LPrior != null)
			{
				LEntry = LPrior;
				LPrior = LEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;
			}
			return LEntry;
		}

		const string CPreCutoffName = "FPreCutoff";
		private bool GetPreCutoff(FixedSizeCache<string, string>.Entry AEntry)
		{
			return (bool)AEntry.GetType().GetField(CPreCutoffName, CFieldFlags).GetValue(AEntry);
		}
		private bool GetPreCutoff(int AKey)
		{
			return GetPreCutoff(GetEntry(AKey));

		}

		const string CLastAccessName = "FLastAccess";
		private int GetLastAccess(FixedSizeCache<string, string>.Entry AEntry)
		{
			return (int)AEntry.GetType().GetField(CLastAccessName, CFieldFlags).GetValue(AEntry);
		}

		const string CPreCutoffCountName = "FLRUPreCutoffCount";
		private int PreCutoffCount()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return (int)FFixedSizeCacheType.GetField(CPreCutoffCountName, CFieldFlags).GetValue(FTestCache);
		}

		const string CNextName = "FNext";
		private bool ValidateList()
		{
			FixedSizeCache<string, string>.Entry LEntry = GetHead();
			FixedSizeCache<string, string>.Entry LPrior = LEntry == null ? null : LEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;
			FixedSizeCache<string, string>.Entry LNext = LEntry == null ? null : LEntry.GetType().GetField(CNextName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;

			if (LNext != null)
				return false;

			for (int i = 1; i < FTestCache.Count; i++)
			{
				if (LPrior == null)
					return false;
				LNext = LEntry;
				LEntry = LPrior;
				if (LEntry.GetType().GetField(CNextName, CFieldFlags).GetValue(LEntry) != LNext)
					return false;

				LPrior = LEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;
			}

			if (LPrior != null)
				return false;

			LNext = LEntry;
			LEntry = LPrior;
			if (LEntry != null && LEntry.GetType().GetField(CNextName, CFieldFlags).GetValue(LEntry) != LNext)
				return false;

			return true;
		}

		private int GetLocation(FixedSizeCache<string, string>.Entry AEntry)
		{	 	
			FixedSizeCache<string, string>.Entry LEntry = GetHead();
			FixedSizeCache<string, string>.Entry LPrior = LEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;
			int i;
			for (i = 0; i < FTestCache.Size; i++)
			{
				if (LEntry == AEntry)
					return i;  
							
				LEntry = LPrior;
				LPrior = LEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(LEntry) as FixedSizeCache<string, string>.Entry;
			}

			if (LEntry == AEntry)
				return i; 
			else
				throw new Exception("Entry not in list");			
		}

		#endregion
	}
}
