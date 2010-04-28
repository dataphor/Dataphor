/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	[TestFixture]
	class InternalCoverageTests
	{
		[Test]
		public void TestByteArrayUtility()
		{
			// short | int | long | decimal | guid | string
			byte[] LBuffer = new byte[sizeof(short) + sizeof(int) + sizeof(long) + sizeof(decimal) + 16 + 12];
			int LOffset = 0;
			ByteArrayUtility.WriteInt16(LBuffer, LOffset, Int16.MinValue);
			if (ByteArrayUtility.ReadInt16(LBuffer, LOffset) != Int16.MinValue)
				throw new Exception("Int16 failed");

			ByteArrayUtility.WriteInt16(LBuffer, LOffset, Int16.MaxValue);
			if (ByteArrayUtility.ReadInt16(LBuffer, LOffset) != Int16.MaxValue)
				throw new Exception("Int16 failed");

			LOffset += sizeof(short);
			ByteArrayUtility.WriteInt32(LBuffer, LOffset, Int32.MinValue);
			if (ByteArrayUtility.ReadInt32(LBuffer, LOffset) != Int32.MinValue)
				throw new Exception("Int32 failed");

			ByteArrayUtility.WriteInt32(LBuffer, LOffset, Int32.MaxValue);
			if (ByteArrayUtility.ReadInt32(LBuffer, LOffset) != Int32.MaxValue)
				throw new Exception("Int32 failed");

			LOffset += sizeof(int);	
			ByteArrayUtility.WriteInt64(LBuffer, LOffset, Int64.MinValue);
			if (ByteArrayUtility.ReadInt64(LBuffer, LOffset) != Int64.MinValue)
				throw new Exception("Int64 failed");

			ByteArrayUtility.WriteInt64(LBuffer, LOffset, Int64.MaxValue);
			if (ByteArrayUtility.ReadInt64(LBuffer, LOffset) != Int64.MaxValue)
				throw new Exception("Int64 failed");

			LOffset += sizeof(long);
			ByteArrayUtility.WriteDecimal(LBuffer, LOffset, Decimal.MinValue);
			if (ByteArrayUtility.ReadDecimal(LBuffer, LOffset) != Decimal.MinValue)
				throw new Exception("Decimal failed");

			ByteArrayUtility.WriteDecimal(LBuffer, LOffset, Decimal.MaxValue);
			if (ByteArrayUtility.ReadDecimal(LBuffer, LOffset) != Decimal.MaxValue)
				throw new Exception("Decimal failed");

			LOffset += sizeof(decimal);
			Guid LGuid = Guid.NewGuid();
			ByteArrayUtility.WriteGuid(LBuffer, LOffset, LGuid);
			if (ByteArrayUtility.ReadGuid(LBuffer, LOffset) != LGuid)
				throw new Exception("Guid failed");
			
			LOffset += 16;
			string LString = "Test";
			ByteArrayUtility.WriteString(LBuffer, LOffset, LString);
			if (ByteArrayUtility.ReadString(LBuffer, LOffset) != LString)
				throw new Exception("String failed");
		}
	}
}
