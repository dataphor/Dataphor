/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.DAE.Storage
{
/*
	public class Transaction
	{
		public Transaction(TransactionLog ALog)
		{
			FLog = ALog;
		}

		private TransactionLog FLog;
		private long FMinLogOffset;
		private long FMaxLogOffset;
		private long FID;

		public LogStream BeginAppendLog(int ACount, IResourceManager AManager)
		{
			LogStream LStream = FLog.BeginAppend(ACount + sizeof(TransactionLogRecordHeader), AManager);
			TransactionLogRecordHeader* LHeader = stackalloc TransactionLogRecordHeader[1];
			LStream.Write(LHeader, sizeof(TransactionLogRecordHeader));
			return LStream;
		}

		public void EndAppendLog(LogStream AStream)
		{
			FLog.EndAppend(AStream);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 16)]
	public struct TransactionLogRecordHeader
	{
		[FieldOffset(0)]	long ID;
		[FieldOffset(8)]	long PriorID;
	}
*/
}
