/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Client.Controls.Design
{
	/// <summary>The base exception class for all exceptions thrown by the Design classes. </summary>
	[Serializable]
	public class DesignException : DAEException
	{
		public enum Codes : int
		{
			/// <summary> Error code 128100: "Add Tag Cancelled"</summary>
			AddTagCancelled = 128100,

			/// <summary> Error code 128110: "No Tag Selected"</summary>
			NoTagSelected = 128110,

			/// <summary> Error code 128111: "No item selected." </summary>
			NoItemSelected = 128111,

			/// <summary> Error code 128121: "Default Definition must have an Expression." </summary>
			InvalidDefaultDefinition = 128121,

			/// <summary> Error code 128122: "AdornColumnExpression cannot be null." </summary>
			AdornColumnExpressionNotNull = 128122,

			/// <summary> Error code 128123: Column name required. </summary>
			ColumnNameNeeded = 128123,

			/// <summary> Error code 128124: "Default definition requires an expression." </summary>
			ExpressionRequired = 128124,

			/// <summary> Error code 128125: "Constraint requires a name." </summary>
			ConstraintNameRequired = 128125,

			/// <summary> Error code 128126: "Constraint requires an expression." </summary>
			ConstraintExpressionRequired = 128126,

			/// <summary> Error code 128127: "Cannot edit null OrderColumnDefinition." </summary>
			InvalidOrderColumnDefinition = 128127,

			/// <summary> Error code 128128: "Cannot edit null order definition." </summary>
			NullOrderDefinition = 128128,

			/// <summary> Error code 128129: "Cannot edit null key definition." </summary>
			NullKeyDefinition = 128129,

			/// <summary> Error code 128130: "Cannot edit null KeyColumnDefinition." </summary>
			InvalidKeyColumnDefinition = 128130
			
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Client.Controls.Design.DesignException", typeof(DesignException).Assembly);

		// Constructors
		public DesignException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
	}
}
