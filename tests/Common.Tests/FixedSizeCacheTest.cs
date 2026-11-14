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
using System.Threading;

using NUnit.Framework;
using NUnit.Framework.Legacy;

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
				ClassicAssert.AreEqual(i, FTestCache.Count);
				ClassicAssert.AreEqual(FTestCache[GetKey(i)], GetValue(i));
				ClassicAssert.True(FTestCache.ContainsKey(GetKey(i)));

				ClassicAssert.True(ValidateCutoff());
			}
			ClassicAssert.AreEqual(5, FTestCache.Count);

			FTestCache.Add(GetKey(FTestCache.Size + 1), GetValue(FTestCache.Size + 1));
			ClassicAssert.AreEqual(LCacheSize, FTestCache.Count);
			ClassicAssert.True(ValidateCutoff());
			ClassicAssert.True(ValidateList());
		}

		[Test]
		public void ClearAndCount()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++)
			{
				FTestCache.Add(GetKey(i), GetValue(i));
				ClassicAssert.AreEqual(i, FTestCache.Count);
			}
			ClassicAssert.AreEqual(LCacheSize, FTestCache.Count);

			FTestCache.Clear();
			ClassicAssert.AreEqual(0, FTestCache.Count);
			ClassicAssert.True(ValidateList());
		}

		[Test]
		public void ContainsKey()
		{
			int LCacheSize = 5;
			int LTestCacheItem = 3;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);

			ClassicAssert.False(FTestCache.ContainsKey(GetKey(LTestCacheItem)));

			for (int i = 1; i <= FTestCache.Size; i++)
				FTestCache.Add(GetKey(i), GetValue(i));

			ClassicAssert.True(FTestCache.ContainsKey(GetKey(LTestCacheItem)));

			FTestCache.Clear();
			ClassicAssert.False(FTestCache.ContainsKey(GetKey(LTestCacheItem)));
			ClassicAssert.True(ValidateList());
		}

		[Test]
		public void Reference()
		{
			string LReference = null;
			int LCacheSize = 2;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++)
			{
				LReference = FTestCache.Reference(GetKey(i), GetValue(i));
				ClassicAssert.AreEqual(i, FTestCache.Count);
				ClassicAssert.True(FTestCache.ContainsKey(GetKey(i)));
				ClassicAssert.AreEqual(FTestCache[GetKey(i)], GetValue(i));
				ClassicAssert.IsNull(LReference);
			}
			ClassicAssert.AreEqual(LCacheSize, FTestCache.Count);

			LReference = FTestCache.Reference(GetKey(1), GetValue(1));

			ClassicAssert.IsNull(LReference);

			LReference = null;
			LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);
			for (int i = 1; i <= FTestCache.Size; i++)
			{
				LReference = FTestCache.Reference(GetKey(i), GetValue(i));
				ClassicAssert.AreEqual(i, FTestCache.Count);
				ClassicAssert.True(FTestCache.ContainsKey(GetKey(i)));
				ClassicAssert.AreEqual(FTestCache[GetKey(i)], GetValue(i));
				ClassicAssert.IsNull(LReference);
			}
			ClassicAssert.AreEqual(LCacheSize, FTestCache.Count);

			LReference = FTestCache.Reference(GetKey(FTestCache.Size + 1), GetValue(FTestCache.Size + 1));
			ClassicAssert.AreEqual(LCacheSize, FTestCache.Count);
			ClassicAssert.IsNotNull(LReference);
			ClassicAssert.True(ValidateList());
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
			ClassicAssert.False(FTestCache.ContainsKey(GetValue(LTestKey)));
			ClassicAssert.AreEqual(LCacheSize - 1, FTestCache.Count);

			LTestKey = 1;
			FTestCache.Remove(GetKey(LTestKey));
			ClassicAssert.False(FTestCache.ContainsKey(GetValue(LTestKey)));
			ClassicAssert.AreEqual(LCacheSize - 2, FTestCache.Count);

			LTestKey = 5;
			FTestCache.Remove(GetKey(LTestKey));
			ClassicAssert.False(FTestCache.ContainsKey(GetValue(LTestKey)));
			ClassicAssert.AreEqual(LCacheSize - 3, FTestCache.Count);
			ClassicAssert.True(ValidateList());
		}

		[Test]
		public void Size()
		{
			FTestCache = new FixedSizeCache<string, string>(5);
			ClassicAssert.AreEqual(5, FTestCache.Size);
			ClassicAssert.True(ValidateList());
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
				ClassicAssert.AreEqual(GetValue(LValueIndex), LValue);
			}

			ClassicAssert.AreEqual(FTestCache.Size, LValueIndex);

			LValueIndex = 0;
			foreach (FixedSizeCache<string, string>.Entry LEntry in (IEnumerable)FTestCache)
			{
				LValueIndex++;
				ClassicAssert.AreEqual(GetValue(LValueIndex), LEntry.Value);
			}

			ClassicAssert.AreEqual(FTestCache.Size, LValueIndex);
		}

		[Test]
		public void IIndexor()
		{
			int LCacheSize = 5;
			FTestCache = new FixedSizeCache<string, string>(LCacheSize);

			ClassicAssert.IsNull(FTestCache[GetKey(0)]);

			for (int i = 1; i <= FTestCache.Size; i++)
				FTestCache.Add(GetKey(i), GetValue(i));

			for (int j = 1; j <= FTestCache.Size; j++)
				ClassicAssert.AreEqual(GetValue(j), FTestCache[GetKey(j)]);

			ClassicAssert.IsNull(FTestCache[GetKey(FTestCache.Size + 1)]);
		}

		[Test]
		public void InternalAdd()
		{
			{

				int LPreCutoffCount;
				FixedSizeCache<string, string>.Entry LHead;
				FixedSizeCache<string, string>.Entry LCutoff;
				FixedSizeCache<string, string>.Entry LTail;

				FTestCache = new FixedSizeCache<string, string>(7);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				ClassicAssert.AreEqual(0, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());
				ClassicAssert.IsNull(LHead);
				ClassicAssert.IsNull(LCutoff);
				ClassicAssert.IsNull(LTail);

				IncrementTestEntry();

				string LKey = GetKey(1);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry1 = LHead;

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LKey, LHead.Key);
				ClassicAssert.AreEqual(LKey, LCutoff.Key);
				ClassicAssert.AreEqual(LKey, LTail.Key);

				ClassicAssert.IsNull(GetPrior(LHead));
				ClassicAssert.IsNull(GetNext(LHead));

				ClassicAssert.False(GetPreCutoff(LHead));

				IncrementTestEntry();

				LKey = GetKey(2);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry2 = GetEntry(2);

				ClassicAssert.AreEqual(2, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead, LEntry2);
				ClassicAssert.AreEqual(LCutoff, LEntry2);
				ClassicAssert.IsNull(GetNext(LEntry2));
				ClassicAssert.AreEqual(LEntry1, GetPrior(LEntry2));
				ClassicAssert.False(GetPreCutoff(LEntry2));

				ClassicAssert.AreEqual(LTail, LEntry1);
				ClassicAssert.AreEqual(LEntry2, GetNext(LEntry1));
				ClassicAssert.IsNull(GetPrior(LEntry1));
				ClassicAssert.False(GetPreCutoff(LEntry1));

				IncrementTestEntry();

				LKey = GetKey(3);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry3 = GetEntry(3);

				ClassicAssert.AreEqual(3, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead, LEntry3);
				ClassicAssert.AreEqual(LCutoff, LEntry3);
				ClassicAssert.IsNull(GetNext(LEntry3));
				ClassicAssert.AreEqual(LEntry2, GetPrior(LEntry3));
				ClassicAssert.False(GetPreCutoff(LEntry3));

				ClassicAssert.AreEqual(LEntry3, GetNext(LEntry2));
				ClassicAssert.AreEqual(LEntry1, GetPrior(LEntry2));
				ClassicAssert.False(GetPreCutoff(LEntry2));

				ClassicAssert.AreEqual(LTail, LEntry1);
				ClassicAssert.AreEqual(LEntry2, GetNext(LEntry1));
				ClassicAssert.IsNull(GetPrior(LEntry1));
				ClassicAssert.False(GetPreCutoff(LEntry1));

				IncrementTestEntry();

				LKey = GetKey(4);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry4 = GetEntry(4);

				ClassicAssert.AreEqual(4, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead, LEntry4);
				ClassicAssert.IsNull(GetNext(LEntry4));
				ClassicAssert.AreEqual(LEntry3, GetPrior(LEntry4));
				ClassicAssert.True(GetPreCutoff(LEntry4));

				ClassicAssert.AreEqual(LCutoff, LEntry3);
				ClassicAssert.AreEqual(LEntry4, GetNext(LEntry3));
				ClassicAssert.AreEqual(LEntry2, GetPrior(LEntry3));
				ClassicAssert.False(GetPreCutoff(LEntry3));

				ClassicAssert.AreEqual(LEntry3, GetNext(LEntry2));
				ClassicAssert.AreEqual(LEntry1, GetPrior(LEntry2));
				ClassicAssert.False(GetPreCutoff(LEntry2));

				ClassicAssert.AreEqual(LTail, LEntry1);
				ClassicAssert.AreEqual(LEntry2, GetNext(LEntry1));
				ClassicAssert.IsNull(GetPrior(LEntry1));
				ClassicAssert.False(GetPreCutoff(LEntry1));

				IncrementTestEntry();

				LKey = GetKey(5);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry5 = GetEntry(5);

				ClassicAssert.AreEqual(5, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead, LEntry4);
				ClassicAssert.IsNull(GetNext(LEntry4));
				ClassicAssert.AreEqual(LEntry5, GetPrior(LEntry4));
				ClassicAssert.True(GetPreCutoff(LEntry4));

				ClassicAssert.AreEqual(LCutoff, LEntry5);
				ClassicAssert.AreEqual(LEntry4, GetNext(LEntry5));
				ClassicAssert.AreEqual(LEntry3, GetPrior(LEntry5));
				ClassicAssert.False(GetPreCutoff(LEntry5));

				ClassicAssert.AreEqual(LEntry5, GetNext(LEntry3));
				ClassicAssert.AreEqual(LEntry2, GetPrior(LEntry3));
				ClassicAssert.False(GetPreCutoff(LEntry3));

				ClassicAssert.AreEqual(LEntry3, GetNext(LEntry2));
				ClassicAssert.AreEqual(LEntry1, GetPrior(LEntry2));
				ClassicAssert.False(GetPreCutoff(LEntry2));

				ClassicAssert.AreEqual(LTail, LEntry1);
				ClassicAssert.AreEqual(LEntry2, GetNext(LEntry1));
				ClassicAssert.IsNull(GetPrior(LEntry1));
				ClassicAssert.False(GetPreCutoff(LEntry1));

				IncrementTestEntry();

				LKey = GetKey(6);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry6 = GetEntry(6);

				ClassicAssert.AreEqual(6, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead, LEntry4);
				ClassicAssert.IsNull(GetNext(LEntry4));
				ClassicAssert.AreEqual(LEntry6, GetPrior(LEntry4));
				ClassicAssert.True(GetPreCutoff(LEntry4));

				ClassicAssert.AreEqual(LCutoff, LEntry6);
				ClassicAssert.AreEqual(LEntry4, GetNext(LEntry6));
				ClassicAssert.AreEqual(LEntry5, GetPrior(LEntry6));
				ClassicAssert.False(GetPreCutoff(LEntry6));

				ClassicAssert.AreEqual(LEntry6, GetNext(LEntry5));
				ClassicAssert.AreEqual(LEntry3, GetPrior(LEntry5));
				ClassicAssert.False(GetPreCutoff(LEntry5));

				ClassicAssert.AreEqual(LEntry5, GetNext(LEntry3));
				ClassicAssert.AreEqual(LEntry2, GetPrior(LEntry3));
				ClassicAssert.False(GetPreCutoff(LEntry3));

				ClassicAssert.AreEqual(LEntry3, GetNext(LEntry2));
				ClassicAssert.AreEqual(LEntry1, GetPrior(LEntry2));
				ClassicAssert.False(GetPreCutoff(LEntry2));

				IncrementTestEntry();

				LKey = GetKey(7);
				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();
				FixedSizeCache<string, string>.Entry LEntry7 = GetEntry(7);

				ClassicAssert.AreEqual(7, FTestCache.Count);
				ClassicAssert.AreEqual(2, PreCutoffCount());

				ClassicAssert.AreEqual(LHead, LEntry4);
				ClassicAssert.IsNull(GetNext(LEntry4));
				ClassicAssert.AreEqual(LEntry7, GetPrior(LEntry4));
				ClassicAssert.True(GetPreCutoff(LEntry4));

				ClassicAssert.AreEqual(LEntry4, GetNext(LEntry7));
				ClassicAssert.AreEqual(LEntry6, GetPrior(LEntry7));
				ClassicAssert.True(GetPreCutoff(LEntry7));

				ClassicAssert.AreEqual(LCutoff, LEntry6);
				ClassicAssert.AreEqual(LEntry7, GetNext(LEntry6));
				ClassicAssert.AreEqual(LEntry5, GetPrior(LEntry6));
				ClassicAssert.False(GetPreCutoff(LEntry6));

				ClassicAssert.AreEqual(LEntry6, GetNext(LEntry5));
				ClassicAssert.AreEqual(LEntry3, GetPrior(LEntry5));
				ClassicAssert.False(GetPreCutoff(LEntry5));

				ClassicAssert.AreEqual(LEntry5, GetNext(LEntry3));
				ClassicAssert.AreEqual(LEntry2, GetPrior(LEntry3));
				ClassicAssert.False(GetPreCutoff(LEntry3));

				ClassicAssert.AreEqual(LEntry3, GetNext(LEntry2));
				ClassicAssert.AreEqual(LEntry1, GetPrior(LEntry2));
				ClassicAssert.False(GetPreCutoff(LEntry2));
				ClassicAssert.AreEqual(LTail, LEntry1);
				ClassicAssert.AreEqual(LEntry2, GetNext(LEntry1));
				ClassicAssert.IsNull(GetPrior(LEntry1));
				ClassicAssert.False(GetPreCutoff(LEntry1));

				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(25);

				//disregarding CorrelatedReferencePeriod
				while (TestEntry < FTestCache.Size)
				{
					IncrementTestEntry();
					LPreCutoffCount = (int)(TestEntry * FixedSizeCache<string, string>.DefaultCutoff);
					ClassicAssert.AreEqual(LPreCutoffCount, PreCutoffCount());

					LHead = GetHead();
					LCutoff = GetCutoff();
					LTail = GetTail();

					if (LPreCutoffCount == 0)
					{
						if (TestEntry == 1)
						{
							ClassicAssert.True(CompareEntry(TestEntry, LHead));
							ClassicAssert.True(CompareEntry(TestEntry, LCutoff));
							ClassicAssert.True(CompareEntry(TestEntry, LTail));
							ClassicAssert.False(GetPreCutoff(TestEntry));
						}
						else
						{
							ClassicAssert.True(CompareEntry(TestEntry, LHead));
							ClassicAssert.True(CompareEntry(TestEntry, LCutoff));
							ClassicAssert.False(CompareEntry(TestEntry, LTail));
							ClassicAssert.False(GetPreCutoff(TestEntry));
						}
					}
					else if (((TestEntry - 1) % 3) == 0)
					{
						if (LPreCutoffCount == 1)
							ClassicAssert.True(CompareEntry(TestEntry, LHead));
						else
							ClassicAssert.False(CompareEntry(TestEntry, LHead));
						ClassicAssert.False(CompareEntry(TestEntry, LCutoff));
						ClassicAssert.False(CompareEntry(TestEntry, LTail));
						ClassicAssert.True(GetPreCutoff(TestEntry));
					}
					else
					{
						ClassicAssert.False(CompareEntry(TestEntry, LHead));
						ClassicAssert.True(CompareEntry(TestEntry, LCutoff));
						ClassicAssert.False(CompareEntry(TestEntry, LTail));
						ClassicAssert.False(GetPreCutoff(TestEntry));
					}
				}

				ClassicAssert.True(ValidateList());
			}
		}

		[Test]
		public void InternalReference()
		{
			{

				// Entries don't get promoted until Correlated Period is exceeded and when they do they are promoted to Head
				FTestCache = new FixedSizeCache<string, string>(25);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();
				FixedSizeCache<string, string>.Entry LEntry = GetPrior(GetHead());
				ClassicAssert.AreNotEqual(GetHead(), LEntry);
				int LLocation = GetLocation(LEntry);
				for (int i = (FTestCache.Size - GetLastAccess(LEntry)); i < FixedSizeCache<string, string>.DefaultCorrelatedReferencePeriod; i++)
				{
					FTestCache.Reference(LEntry.Key, LEntry.Value);
					ClassicAssert.AreEqual(LLocation, GetLocation(LEntry));
				}

				FTestCache.Reference(LEntry.Key, LEntry.Value);
				ClassicAssert.AreNotEqual(LLocation, GetLocation(LEntry));
				ClassicAssert.AreEqual(GetHead(), LEntry);
				ClassicAssert.True(ValidateList());

				// Entries PreCutoff get demoted to not PreCutoff 
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(25);
				while (TestEntry < FTestCache.Size)
				{
					IncrementTestEntry();
					ClassicAssert.True(ValidateCutoff());
				}

				LEntry = GetHead();
				FixedSizeCache<string, string>.Entry LReferenced;
				FixedSizeCache<string, string>.Entry LTail;

				ClassicAssert.True(GetPreCutoff(LEntry));
				int LPreCutoffCount = PreCutoffCount();
				int LMoveCount = 0;
				while (LMoveCount < LPreCutoffCount)
				{
					LTail = GetTail();
					if (LEntry == LTail)
						LReferenced = LTail.GetType().GetField(CNextName, CFieldFlags).GetValue(LTail) as FixedSizeCache<string, string>.Entry;
					else
						LReferenced = LTail;
					ClassicAssert.True(ValidateCutoff());
					ClassicAssert.False(GetPreCutoff(LReferenced));
					FTestCache.Reference(LReferenced.Key, LReferenced.Value);
					if (GetPreCutoff(LReferenced))
						LMoveCount++;
				}
				ClassicAssert.False(GetPreCutoff(LEntry));
				ClassicAssert.True(ValidateList());
			}
		}

		[Test]
		public void InternalRemove()
		{
			{

				//add four remove middle
				//add four remove tail


				FixedSizeCache<string, string>.Entry LHead;
				FixedSizeCache<string, string>.Entry LCutoff;
				FixedSizeCache<string, string>.Entry LTail;

				//add one remove one
				FTestCache = new FixedSizeCache<string, string>(2);
				IncrementTestEntry();

				FTestCache.Remove(GetKey(1));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(0, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.IsNull(LHead);
				ClassicAssert.IsNull(LCutoff);
				ClassicAssert.IsNull(LTail);

				//add two remove head								   
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(2);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				string LKey = GetKey(2);

				FTestCache.Remove(LKey);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(1));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(1));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				ClassicAssert.IsNull(GetNext(LHead));
				ClassicAssert.IsNull(GetPrior(LHead));
				ClassicAssert.False(GetPreCutoff(LHead));

				//add two remove tail
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(2);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				LKey = GetKey(1);

				FTestCache.Remove(LKey);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(2));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(2));
				ClassicAssert.AreEqual(LTail.Key, GetKey(2));

				//add three remove head, head
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(3);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				LKey = GetKey(3);

				FTestCache.Remove(LKey);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(2, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(2));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(2));
				ClassicAssert.IsNull(GetNext(LHead));
				ClassicAssert.AreEqual(LTail, GetPrior(LHead));
				ClassicAssert.False(GetPreCutoff(LHead));

				ClassicAssert.AreEqual(LTail.Key, GetKey(1));
				ClassicAssert.AreEqual(LHead, GetNext(LTail));
				ClassicAssert.IsNull(GetPrior(LTail));
				ClassicAssert.False(GetPreCutoff(LTail));

				FTestCache.Remove(GetKey(2));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(1));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(1));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//add three remove head, tail
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(3);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				LKey = GetKey(3);

				FTestCache.Remove(LKey);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(2, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(2));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(2));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				FTestCache.Remove(GetKey(1));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(2));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(2));
				ClassicAssert.AreEqual(LTail.Key, GetKey(2));

				//add three remove tail, middle
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(3);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				LKey = GetKey(1);

				FTestCache.Remove(LKey);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(2, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(3));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(3));
				ClassicAssert.IsNull(GetNext(LHead));
				ClassicAssert.AreEqual(LTail, GetPrior(LHead));
				ClassicAssert.False(GetPreCutoff(LHead));

				ClassicAssert.AreEqual(LTail.Key, GetKey(2));
				ClassicAssert.AreEqual(LHead, GetNext(LTail));
				ClassicAssert.IsNull(GetPrior(LTail));
				ClassicAssert.False(GetPreCutoff(LTail));

				FTestCache.Remove(GetKey(2));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(3));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(3));
				ClassicAssert.AreEqual(LTail.Key, GetKey(3));

				//add three remove tail, head
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(3);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				LKey = GetKey(1);

				FTestCache.Remove(LKey);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(2, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(3));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(3));
				ClassicAssert.AreEqual(LTail.Key, GetKey(2));

				FTestCache.Remove(GetKey(3));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(1, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(2));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(2));
				ClassicAssert.AreEqual(LTail.Key, GetKey(2));

				//add four remove head
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(4);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetHead().Key);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(3, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());


				ClassicAssert.AreEqual(LHead.Key, GetKey(3));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(3));
				ClassicAssert.IsNull(GetNext(LHead));
				//ClassicAssert.AreEqual(LTail, GetPrior(LHead));
				//ClassicAssert.False(GetPreCutoff(LHead));

				//ClassicAssert.AreEqual(LTail.Key, GetKey(2));
				//ClassicAssert.AreEqual(LHead, GetNext(LTail));
				//ClassicAssert.IsNull(GetPrior(LTail));
				//ClassicAssert.False(GetPreCutoff(LTail));


				//add four remove tail
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(4);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetTail().Key);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(3, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(4));
				ClassicAssert.AreEqual(LTail.Key, GetKey(2));

				//add four remove cutoff
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(4);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetCutoff().Key);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(3, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(4));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//add four remove middle
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(4);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetKey(2));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(3, FTestCache.Count);
				ClassicAssert.AreEqual(0, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(4));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//add five remove head
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(5);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetHead().Key);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(4, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(5));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(3));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//add five remove tail
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(5);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetTail().Key);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(4, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(5));
				ClassicAssert.AreEqual(LTail.Key, GetKey(2));

				//add five remove cutoff
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(5);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetCutoff().Key);

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(4, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(3));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//add five remove middle, next to cutoff
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(5);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetKey(3));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(4, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(5));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//add five remove middle, next to tail
				TestSetUp();
				FTestCache = new FixedSizeCache<string, string>(5);
				while (TestEntry < FTestCache.Size)
					IncrementTestEntry();

				FTestCache.Remove(GetKey(3));

				LHead = GetHead();
				LCutoff = GetCutoff();
				LTail = GetTail();

				ClassicAssert.AreEqual(4, FTestCache.Count);
				ClassicAssert.AreEqual(1, PreCutoffCount());

				ClassicAssert.AreEqual(LHead.Key, GetKey(4));
				ClassicAssert.AreEqual(LCutoff.Key, GetKey(5));
				ClassicAssert.AreEqual(LTail.Key, GetKey(1));

				//FTestCache = new FixedSizeCache<string, string>(2);
				//while (TestEntry < FTestCache.Size)
				//    IncrementTestEntry();


				//FTestCache = new FixedSizeCache<string, string>(25);
				//while (TestEntry < FTestCache.Size)		 				 
				//    IncrementTestEntry();

				//FTestCache.Remove(GetHead().Key);
				//ClassicAssert.True(ValidateList());

				//FTestCache.Remove(GetCutoff().Key);
				//ClassicAssert.True(ValidateList());

				//FTestCache.Remove(GetTail().Key);
				//ClassicAssert.True(ValidateList());	 

				//while (FTestCache.Count > 0)
				//{
				//    FTestCache.Remove(GetCutoff().Key);
				//    ClassicAssert.True(ValidateList());
				//}
			}
		}

		[Test]
		public void RandomAccess()
		{
			{

				new RandomFixedSizeCacheAccess(new FixedSizeCache<string, string>(50), 50, 100000, 100).Run();
				ClassicAssert.True(ValidateList());
			}
		}

		//[Test]
		//public void ConcurrentAccess()
		//{
		//    try
		//    {
		//        ReflectionPermission LReflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess | ReflectionPermissionFlag.AllFlags);
		//        LReflectionPermission.Assert();

		//        FTestCache = new FixedSizeCache<string, string>(50);
		//        RandomFixedSizeCacheAccess LRandomFixedSizeCacheAccess;
		//        Thread LThread;
		//        Thread[] LThreads = new Thread[5];
		//        TimeSpan LWaitTime = new TimeSpan(0, 0, 30);
		//        for (int i = 0; i < 5; i++)
		//        {
		//            LRandomFixedSizeCacheAccess = new RandomFixedSizeCacheAccess(FTestCache, 50, 100000, 100);
		//            LThread = new Thread(new ThreadStart(LRandomFixedSizeCacheAccess.Run));
		//            LThreads[i] = LThread;
		//            LThread.Start();
		//        }
		//        LThreads[4].Join(LWaitTime);
		//        bool LDeadlock = false;
		//        foreach (Thread LRunningThread in LThreads)
		//        {
		//            if (LRunningThread.ThreadState != ThreadState.Stopped)
		//            {
		//                LDeadlock = true;
		//                LRunningThread.Abort();
		//            }
		//            if (LDeadlock)
		//                throw new Exception("Deadlock");
		//        }
		//    }
		//    finally
		//    {
		//        CodeAccessPermission.RevertAssert();
		//    }

		//}

		#region Helpers

		private FixedSizeCache<string, string> FTestCache;

		private bool ValidateCutoff()
		{
			FixedSizeCache<string, string>.Entry LEntry = GetHead();
			for (int i = 0; i < PreCutoffCount(); i++)
				LEntry = GetPrior(LEntry);

			return GetCutoff() == LEntry;
		}

		private int FTestEntry = 0;
		private void IncrementTestEntry()
		{
			FTestEntry++;
			FTestCache.Add(GetKey(FTestEntry), GetValue(FTestEntry));
		}
		private int TestEntry
		{ get { return FTestEntry; } }

		private static string FromTemplate(string AValue, string ATemplate)
		{
			return String.Format(ATemplate, AValue);
		}

		private const string CKeyTemplate = "Key{0}";
		public static string GetKey(string AKey)
		{
			return FromTemplate(AKey, CKeyTemplate);
		}

		public static string GetKey(int AKey)
		{
			return GetKey(AKey.ToString());
		}

		private const string CValueTemplate = "Value{0}";
		public static string GetValue(string AValue)
		{
			return FromTemplate(AValue, CValueTemplate);
		}

		public static string GetValue(int AValue)
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
		const string CHeadName = "_lRUHead";
		private Type FFixedSizeCacheType;
		private FixedSizeCache<string, string>.Entry GetHead()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return FFixedSizeCacheType.GetField(CHeadName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
		}

		const string CCutoffName = "_lRUCutoff";
		private FixedSizeCache<string, string>.Entry GetCutoff()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return FFixedSizeCacheType.GetField(CCutoffName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
		}

		const string CTailName = "_lRUTail";
		private FixedSizeCache<string, string>.Entry GetTail()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return FFixedSizeCacheType.GetField(CTailName, CFieldFlags).GetValue(FTestCache) as FixedSizeCache<string, string>.Entry;
		}

		const string CPriorName = "_prior";
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

		private FixedSizeCache<string, string>.Entry GetPrior(FixedSizeCache<string, string>.Entry AEntry)
		{
			return AEntry.GetType().GetField(CPriorName, CFieldFlags).GetValue(AEntry) as FixedSizeCache<string, string>.Entry;
		}

		const string CFNextName = "_next";
		private FixedSizeCache<string, string>.Entry GetNext(FixedSizeCache<string, string>.Entry AEntry)
		{
			return AEntry.GetType().GetField(CFNextName, CFieldFlags).GetValue(AEntry) as FixedSizeCache<string, string>.Entry;
		}

		const string CPreCutoffName = "_preCutoff";
		private bool GetPreCutoff(FixedSizeCache<string, string>.Entry AEntry)
		{
			return (bool)AEntry.GetType().GetField(CPreCutoffName, CFieldFlags).GetValue(AEntry);
		}
		private bool GetPreCutoff(int AKey)
		{
			return GetPreCutoff(GetEntry(AKey));

		}

		const string CLastAccessName = "_lastAccess";
		private int GetLastAccess(FixedSizeCache<string, string>.Entry AEntry)
		{
			return (int)AEntry.GetType().GetField(CLastAccessName, CFieldFlags).GetValue(AEntry);
		}

		const string CPreCutoffCountName = "_lRUPreCutoffCount";
		private int PreCutoffCount()
		{
			if (FFixedSizeCacheType == null)
				FFixedSizeCacheType = FTestCache.GetType();

			return (int)FFixedSizeCacheType.GetField(CPreCutoffCountName, CFieldFlags).GetValue(FTestCache);
		}

		const string CNextName = "_next";
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
				ClassicAssert.AreEqual(LEntry.Key.Replace("Key", "Value"), LEntry.Value);
				ClassicAssert.AreEqual(FTestCache[LEntry.Key], LEntry.Value);
				ClassicAssert.AreEqual(LEntry.Key.Replace("Key", "Value"), FTestCache[LEntry.Key]);

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

	public class RandomFixedSizeCacheAccess
	{
		public RandomFixedSizeCacheAccess(FixedSizeCache<string, string> AFixedSizeCache, int AIterations, int AAccessCount, int ASeed)
		{
			FFixedSizeCache = AFixedSizeCache;
			FIterations = AIterations;
			FAccessCount = AAccessCount;
			FSeed = ASeed;
		}

		FixedSizeCache<string, string> FFixedSizeCache;
		int FIterations;
		int FAccessCount;
		int FSeed;

		public void Run()
		{
			Random LRandom = new Random();
			for (int j = 0; j < FIterations; j++)
			{
				int LEntry;
				for (int i = 0; i < FAccessCount; i++)
				{
					LEntry = LRandom.Next(1, FSeed);
					switch (LRandom.Next(1, 10))
					{
						case 1:
						case 2:
						case 3:
							FFixedSizeCache.Add(FixedSizeCacheTest.GetKey(LEntry), FixedSizeCacheTest.GetValue(LEntry));
							break;
						case 4:
						case 5:
						case 6:
							FFixedSizeCache[FixedSizeCacheTest.GetKey(LEntry)] = FixedSizeCacheTest.GetValue(LRandom.Next(1, FSeed));
							break;
						case 8:
							FFixedSizeCache.Reference(FixedSizeCacheTest.GetKey(LEntry), FixedSizeCacheTest.GetValue(LEntry));
							break;
						default:
							FFixedSizeCache.Remove(FixedSizeCacheTest.GetKey(LEntry));
							break;
					}
				}
			}
		}
	}
}
