/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public class SilverlightClientException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 207100: "Cannot synchronize onto the main thread.  Dispatcher reference missing on frontend session."</summary>
			MissingDispatcher = 207100,
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Client.Silverlight.SilverClientException", typeof(SilverlightClientException).Assembly);

		// Constructors
		public SilverlightClientException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public SilverlightClientException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public SilverlightClientException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public SilverlightClientException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public SilverlightClientException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public SilverlightClientException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public SilverlightClientException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public SilverlightClientException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
	}
}
