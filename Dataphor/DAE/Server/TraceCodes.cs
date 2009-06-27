/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Server
{
	public sealed class TraceCodes
	{
		public const string BeginParse = "000001";
		public const string EndParse = "000002";
		public const string BeginCompile = "000003";
		public const string EndCompile = "000004";
		public const string BeginPrepare = "000005";
		public const string EndPrepare = "000006";
		public const string BeginBeginTransaction = "000007";
		public const string EndBeginTransaction = "000008";
		public const string BeginPrepareTransaction = "000009";
		public const string EndPrepareTransaction = "000010";
		public const string BeginCommitTransaction = "000011";
		public const string EndCommitTransaction = "000012";
		public const string BeginRollbackTransaction = "000013";
		public const string EndRollbackTransaction = "000014";
		public const string BeginBeginApplicationTransaction = "000015";
		public const string EndBeginApplicationTransaction = "000016";
		public const string BeginJoinApplicationTransaction = "000017";
		public const string EndJoinApplicationTransaction = "000018";
		public const string BeginPrepareApplicationTransaction = "000019";
		public const string EndPrepareApplicationTransaction = "000020";
		public const string BeginCommitApplicationTransaction = "000021";
		public const string EndCommitApplicationTransaction = "000022";
		public const string BeginRollbackApplicationTransaction = "000023";
		public const string EndRollbackApplicationTransaction = "000024";
		public const string BeginEndApplicationTransaction = "000025";
		public const string EndEndApplicationTransaction = "000026";
		public const string BeginExecute = "000027";
		public const string EndExecute = "000028";
		public const string BeginOpenCursor = "000029";
		public const string EndOpenCursor = "000030";
		public const string UnsupportedNode = "000031";
		public const string StreamTracing = "000032";
		public const string LockTracing = "000033";
	}
}