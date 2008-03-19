/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.DAE.Client.COM
{
	[Serializable]
	[ComVisible(false)]
	public class COMClientException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 129100: "The Session Setting ({0}) is not valid."</summary>
			InvalidSessionSetting = 129100,

			/// <summary>Error code 129101: "Column type '{0}' is not supported by this client.  The COM Client requires column types which are natively representable (such as system types or types that are transitively 'like' system types)."</summary>
			NativeTypeRequired = 129101,

			/// <summary>Error code 129102: "Incorrect data type.  Object of type '{0}' provided, when type '{1}' is required."</summary>
			IncorrectType = 129102,

			/// <summary>Error code 129103: "Unrecognized data type '{0}'.  Unable to convert the value to a scalar."</summary>
			UnrecognizedType = 129103,
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Client.COM.COMClientException", typeof(COMClientException).Assembly);

		// Constructors
		public COMClientException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode) {}
		public COMClientException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, AParams) {}
		public COMClientException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, AInnerException) {}
		public COMClientException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, AInnerException, AParams) {}
		public COMClientException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}
