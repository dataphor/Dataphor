/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	[Serializable]
	public class WebClientException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 204100: "Warning: The browser posted from an outdated page.  The posted changes have been ignored.  [Expecting version ({1}), posted from version ({0})]  This usually results from making changes after navigating backward in the browser."</summary>
			LogicalClockWarning = 204100,

			/// <summary>Error code 204101: "Invalid object type added to toolbar.  Only ToolBarButton allowed."</summary>
			InvalidToolbarObject = 204101,

			/// <summary>Error code 204102: "Invalid child for MenuItemList."</summary>
			InvalidMenuChild = 204102,

			/// <summary>Error code 204103: "TreeView expressions incorrectly setup.  Infinite looping parent which is not part of root nodes." </summary>
			TreeViewInfiniteLoop = 204103,
			
			/// <summary>Error code 204104: "TreeView expressions incorrectly setup.  Child or parent expression contains nodes which do not link to root expression nodes." </summary>
			TreeViewUnconnected = 203104,

			/// <summary>Error code 203105: "Context sensitive help is not currently supported in this client."</summary>
			ContextHelpNotSupported = 203105
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Client.Web.WebClientException", typeof(WebClientException).Assembly);

		// Constructors
		public WebClientException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public WebClientException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public WebClientException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public WebClientException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public WebClientException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public WebClientException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public WebClientException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public WebClientException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public WebClientException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}
