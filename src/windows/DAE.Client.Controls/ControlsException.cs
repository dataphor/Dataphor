/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[Serializable]
	public class ControlsException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 122100: "Can not convert {0} to {1}."</summary>
			InvalidCast = 122100, 

			/// <summary>Error code 122101: "Column ({0}) not found."</summary>
			ColumnNotFound = 122101,
			
			/// <summary>Error code 122102: "IncrementalControls can only contain IIncremental controls."</summary>
			InvalidControlChild = 122102,

			/// <summary>Error code 122103: "IncrementalColumns can only contain IncrementalSearchColumns."</summary>
			InvalidColumnChild = 122103,

			/// <summary>Error code 122104: "View not in insert or edit mode."</summary>
			InvalidViewState = 122104,

			/// <summary>Error code 122105: "{0}: Invalid Column Data Type."</summary>
			InvalidColumn = 122105,
			
			/// <summary>Error code 122106: "View not active."</summary>
			ViewNotActive = 122106,
			
			/// <summary>Error code 122107: "ScrollInterval must be greater than 0."</summary>
			InvalidInterval = 122107,
			
			/// <summary>Error code 122108: "GridColumns can only contain GridColumn objects."</summary>
			InvalidGridChild = 122108,
			
			/// <summary>Error code 122109: "Invalid range, start index must be less than stop index."</summary>
			InvalidRange = 122109,

			/// <summary>Error code 122110: "Can not modify the ColumnName proerty of a default grid column."</summary>
			InvalidUpdate = 122110,
			
			/// <summary>Error code 122111: "Error binding data. Column name ({0}) not found."</summary>
			DataColumnNotFound = 122111,
						
			/// <summary>Error code 122112: "Data link not active for column({0})."</summary>
			DataLinkNotActive = 122112,

			/// <summary>Error code 122113: "Error Column not visible in grid client area."</summary>
			NotInGridClientArea = 122113,

			/// <summary>Error code 122114: "Error Row index out of range."</summary>
			RowIndexOutOfRange = 122114,
			
			/// <summary> Error code 122115: "Grid is read-only."</summary>
			GridIsReadOnly = 122115,

			/// <summary> Error code 122116: "ParentKey columns cannot equal key columns." </summary>
			InvalidParentKey = 122116,

			/// <summary> Error code 122117: "Key columns cannot equal parent key columns." </summary>
			InvalidKey = 122117,

			/// <summary> "Minimum row height must be greater than 0." </summary>
			ZeroMinRowHeight = 122118,

			/// <summary> "Minimum row height cannot exceed maximum row height." </summary>
			InvalidMinRowHeight = 122119,

			/// <summary> "Maximum row height must be greater than minimum row height." </summary>
			InvalidMaxRowHeight = 122120,

			/// <summary> "Only GridRow objects allowed in GridRows." </summary>
			GridRowsOnly = 122121,

			/// <summary> "Minimum width cannot exceed maximum width." </summary>
			InvalidMinimumWidth = 122122,

			/// <summary> "Maximum width cannot be less than minimum width." </summary>
			InvalidMaximumWidth = 122123,

			/// <summary> "TreeView expressions incorrectly setup.  Infinite looping parent which is not part of root nodes." </summary>
			TreeViewInfiniteLoop = 122124,
			
			/// <summary> "TreeView expressions incorrectly setup.  Child or parent expression contains nodes which do not link to root expression nodes." </summary>
			TreeViewUnconnected = 122125,

			/// <summary> Error Code 122126: "Notebook may not contain '{0}' controls as children, only NotebookPage controls are allowed." </summary>
			InvalidNotebookChild = 122126,

			/// <summary> Error Code 122127: "Invalid tab alignment ({0}), only top is supported." </summary>
			InvalidTabAlignment = 122127,

			/// <summary> Error Code 122128: "Length of content ({0}) exceeds configured maximum ({1})."</summary>
			MaximumContentLengthExceeded = 122128,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Client.Controls.ControlsException", typeof(ControlsException).Assembly);

		// Constructors
		public ControlsException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ControlsException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ControlsException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ControlsException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ControlsException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ControlsException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ControlsException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ControlsException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public ControlsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}
	}
}
