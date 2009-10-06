/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class ScanException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 110100: "Cannot perform this operation on a closed scan."</summary>
			ScanInactive = 110100,

			/// <summary>Error code 110101: "Scan has no active row."</summary>
			NoActiveRow = 110101,

			/// <summary>Error code 110102: "Row could not be located using clustered index."</summary>
			ClusteredRowNotFound = 110102
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Runtime.Data.ScanException", typeof(ScanException).Assembly);

		// Constructors
		public ScanException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, null, null) {}
		public ScanException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, null, AParams) {}
		public ScanException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, AInnerException, null) {}
		public ScanException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, AInnerException, AParams) {}
		public ScanException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public ScanException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public ScanException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public ScanException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		
		public ScanException(ErrorSeverity ASeverity, int ACode, string AMessage, string ADetails, string AServerContext, DataphorException AInnerException) 
			: base(ASeverity, ACode, AMessage, ADetails, AServerContext, AInnerException)
		{
		}
	}
}