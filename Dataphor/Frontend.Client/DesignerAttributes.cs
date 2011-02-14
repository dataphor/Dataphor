using System;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Use on a class or struct to identifiy itself to the designer that it is the root design node. </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class DesignRootAttribute: Attribute {}

	/// <summary> Use on a node to tell the designers to list it or not. </summary>
	/// <remarks> If missing then a default of ListInDesigner.Form should be assumed. </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class ListInDesignerAttribute : Attribute
	{
		public ListInDesignerAttribute(bool isListed) : base()
		{
			_isListed = isListed;
		}

		private bool _isListed;
		public bool IsListed
		{
			get { return _isListed; }
			set { IsListed = value; }
		}
	}

	/// <summary> Use on a class or struct to identify which icon to use in the designer. </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class DesignerImageAttribute : Attribute
	{
		public DesignerImageAttribute(string imageExpression) : base()
		{
			_imageExpression = imageExpression;
		}

		private string _imageExpression;
		public string ImageExpression
		{
			get { return _imageExpression; }
			set { _imageExpression = value; }
		}
	}

	/// <summary> Specifies the operator to use (e.g. "Load", "Form") when building a document expression for the given property. </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DocumentExpressionOperatorAttribute : Attribute
	{
		public DocumentExpressionOperatorAttribute(string operatorName)
		{
			_operatorName = operatorName;
		}

		private string _operatorName;
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value; }
		}
	}
}
