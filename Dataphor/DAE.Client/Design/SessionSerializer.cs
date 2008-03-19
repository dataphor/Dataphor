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

namespace Alphora.Dataphor.DAE.Client.Design
{
	internal class ActiveLastSerializer : CodeDomSerializer
	{
		public const string CPropertyName = "Active";

		public override object Deserialize(IDesignerSerializationManager AManager, object ACodeObject)
		{
			CodeDomSerializer LbaseSerializer = (CodeDomSerializer)AManager.GetSerializer(typeof(Alphora.Dataphor.DAE.Client.DataSession).BaseType, typeof(CodeDomSerializer));
			if (ACodeObject is CodeStatementCollection) 
			{
				CodeStatementCollection LStatements = (CodeStatementCollection)ACodeObject;
				CodeStatement LSetActivePropertyStatement = GetPropertyAssignStatement(LStatements, CPropertyName);
				if (LSetActivePropertyStatement != null)
				{
					LStatements.Remove(LSetActivePropertyStatement);
					LStatements.Insert(LStatements.Count, LSetActivePropertyStatement);
				}
			}
			return LbaseSerializer.Deserialize(AManager, ACodeObject);
		}

		public override object Serialize(IDesignerSerializationManager AManager, object AValue)
		{
			CodeDomSerializer LbaseSerializer = (CodeDomSerializer)AManager.GetSerializer(typeof(Alphora.Dataphor.DAE.Client.DataSession).BaseType, typeof(CodeDomSerializer));
			object LCodeObject = LbaseSerializer.Serialize(AManager, AValue);
			if (LCodeObject is CodeStatementCollection) 
			{
				CodeStatementCollection LStatements = (CodeStatementCollection)LCodeObject;
				CodeStatement LSetActivePropertyStatement = GetPropertyAssignStatement(LStatements, CPropertyName);
				if (LSetActivePropertyStatement != null)
				{
					LStatements.Remove(LSetActivePropertyStatement);
					LStatements.Insert(LStatements.Count, LSetActivePropertyStatement);
				}
			}
			return LCodeObject;
		}

		protected CodeStatement GetPropertyAssignStatement(CodeStatementCollection AStatements, string APropertyName)
		{
			foreach (CodeStatement LStatement in AStatements)
			{
				if (LStatement is CodeAssignStatement)
					if (((CodeAssignStatement)LStatement).Left is CodePropertyReferenceExpression)
					{
						CodePropertyReferenceExpression LExpression = (CodePropertyReferenceExpression)((CodeAssignStatement)LStatement).Left;
						if (LExpression.PropertyName == APropertyName)
							return LStatement;
					}
			}
			return null;
		}

	}
}
