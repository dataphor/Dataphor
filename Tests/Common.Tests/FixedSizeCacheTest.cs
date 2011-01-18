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
		}

		[Test]
		public void Size()										   
		{
			FTestCache = new FixedSizeCache<string, string>(5);
			Assert.AreEqual(5, FTestCache.Size);
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

			return	FFixedSizeCacheType.GetField(CHeadName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
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
		private bool GetPreCutoff(int AKey)
		{
			FixedSizeCache<string, string>.Entry LEntry = GetEntry(AKey);
			return (bool)LEntry.GetType().GetField(CPreCutoffName, CFieldFlags).GetValue(LEntry);
		}

		const string CPreCutoffCountName = "FLRUPreCutoffCount";
		private int PreCutoffCount()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return (int)FFixedSizeCacheType.GetField(CPreCutoffCountName, CFieldFlags).GetValue(FTestCache);	  		
		}
		
		[Test]
		public void InternalAdd()
		{
			try
			{
				ReflectionPermission LReflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess | ReflectionPermissionFlag.AllFlags);
				LReflectionPermission.Assert();

				FTestCache = new FixedSizeCache<string, string>(25);

				//disregarding CorrelatedReferencePeriod
				while (TestEntry < FTestCache.Size)
				{	 
					IncrementTestEntry(); 

					if (TestEntry == 1)
					{	  								
						Assert.IsTrue(CompareEntry(TestEntry, GetHead())); 
						Assert.IsTrue(CompareEntry(TestEntry, GetCutoff()));
						Assert.IsTrue(CompareEntry(TestEntry, GetTail()));
						Assert.IsFalse(GetPreCutoff(TestEntry));
						Assert.AreEqual(0, PreCutoffCount());
					} 					
					else if (TestEntry <= 3)
					{
						Assert.IsTrue(CompareEntry(TestEntry, GetHead()));
						Assert.IsTrue(CompareEntry(TestEntry, GetCutoff()));
						Assert.IsFalse(CompareEntry(TestEntry, GetTail()));
						Assert.IsFalse(GetPreCutoff(TestEntry));
						Assert.AreEqual(0, PreCutoffCount());
					}
					else if (((TestEntry - 1) % 3) == 0)
					{
						if (PreCutoffCount() == 1)
							Assert.IsTrue(CompareEntry(TestEntry, GetHead())); 
						else
							Assert.IsFalse(CompareEntry(TestEntry, GetHead()));						
						Assert.IsFalse(CompareEntry(TestEntry, GetCutoff()));
						Assert.IsFalse(CompareEntry(TestEntry, GetTail()));
						Assert.IsTrue(GetPreCutoff(TestEntry));
						Assert.AreEqual((int)((TestEntry - 1) / 3), PreCutoffCount());
					}
					else
					{
						Assert.IsFalse(CompareEntry(TestEntry, GetHead()));
						Assert.IsTrue(CompareEntry(TestEntry, GetCutoff()));
						Assert.IsFalse(CompareEntry(TestEntry, GetTail()));
						Assert.IsFalse(GetPreCutoff(TestEntry));
					}					
				}						
			}
			finally
			{
				CodeAccessPermission.RevertAssert();	 
			}
			
		}

		[Test]
		public void InternalReference()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void InternalRemove()
		{
			throw new NotImplementedException();
		}
	}
}
