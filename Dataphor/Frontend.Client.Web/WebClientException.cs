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
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Client.Web.WebClientException", typeof(WebClientException).Assembly);

		// Constructors
		public WebClientException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public WebClientException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public WebClientException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public WebClientException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public WebClientException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public WebClientException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public WebClientException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public WebClientException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public WebClientException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}
	}
}
