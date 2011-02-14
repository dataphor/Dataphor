/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;

namespace Alphora.Dataphor.DAE.Server
{
	/// <summary> Serializes and Deserializes the "Started" property last. </summary>
	public class PropertyLastSerializer : CodeDomSerializer
	{
		public const string DefaultPropertyName = "Started";

		private string _propertyName = DefaultPropertyName;
		public string PropertyName
		{
			get { return _propertyName; }
			set { _propertyName = value; }
		}

		public override object Deserialize(IDesignerSerializationManager manager, object codeObject)
		{
			CodeDomSerializer LbaseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(System.ComponentModel.Component), typeof(CodeDomSerializer));
			if (codeObject is CodeStatementCollection) 
			{
				CodeStatementCollection statements = (CodeStatementCollection)codeObject;
				CodeStatement assignPropertyStatement = GetPropertyAssignStatement(statements, PropertyName);
				if (assignPropertyStatement != null)
				{
					statements.Remove(assignPropertyStatement);
					statements.Insert(statements.Count, assignPropertyStatement);
				}
			}
			return LbaseSerializer.Deserialize(manager, codeObject);
		}

		public override object Serialize(IDesignerSerializationManager manager, object tempValue)
		{
			CodeDomSerializer LbaseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(System.ComponentModel.Component), typeof(CodeDomSerializer));
			object codeObject = LbaseSerializer.Serialize(manager, tempValue);
			if (codeObject is CodeStatementCollection) 
			{
				CodeStatementCollection statements = (CodeStatementCollection)codeObject;
				CodeStatement assignPropertyStatement = GetPropertyAssignStatement(statements, PropertyName);
				if (assignPropertyStatement != null)
				{
					statements.Remove(assignPropertyStatement);
					statements.Insert(statements.Count, assignPropertyStatement);
				}
			}
			return codeObject;
		}

		protected CodeStatement GetPropertyAssignStatement(CodeStatementCollection statements, string propertyName)
		{
			foreach (CodeStatement statement in statements)
			{
				if (statement is CodeAssignStatement)
					if (((CodeAssignStatement)statement).Left is CodePropertyReferenceExpression)
					{
						CodePropertyReferenceExpression expression = (CodePropertyReferenceExpression)((CodeAssignStatement)statement).Left;
						if (expression.PropertyName == propertyName)
							return statement;
					}
			}
			return null;
		}

	}
}
