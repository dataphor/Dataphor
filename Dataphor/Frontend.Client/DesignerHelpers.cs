/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Drawing.Design;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

using DAE = Alphora.Dataphor.DAE.Server;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> For use on a property of a node, which refers to another node. </summary>
	public class NodeReferenceConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext AContext, Type ASourceType) 
		{
			if (ASourceType == typeof(string))
				return true;
			return base.CanConvertFrom(AContext, ASourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext AContext, CultureInfo ACulture, object AValue)
		{
			if (AValue == null)
				return null;
			else
			{
				string s = AValue as string;
				if (s != null)
				{
					if (s == "(None)")
						return null;
					else
						return ((INode)AContext.Instance).HostNode.FindNode(s);
				}
				else
					return base.ConvertFrom(AContext, ACulture, AValue);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, CultureInfo ACulture, object AValue, Type ADestinationType) 
		{
			if (ADestinationType == typeof(string))
			{
				if (AValue == null)
					return "(None)";
				if(AValue is String && (String)AValue == "(None)")
					return AValue;

                return ((INode)AValue).Name;
			}
			else
				return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}

		public override bool GetStandardValuesSupported(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext AContext)
		{
			List<INode> LCollection = new List<INode>();
			if (AContext != null)
			{
				foreach (INode LNode in ((INode)AContext.Instance).HostNode.Children[0].Children) 
					WalkNode(LCollection, LNode, AContext);

				LCollection.Sort(new NodeReferenceComparer(this));
			}
			LCollection.Insert(0, null);
			return new TypeConverter.StandardValuesCollection(LCollection);
		}

		// checks a node and recurses to each of it's children.
		private void WalkNode(List<INode> ACollection, INode ANode, ITypeDescriptorContext AContext) 
		{
			if (AContext.PropertyDescriptor.PropertyType.IsAssignableFrom(ANode.GetType()) && ANode.Name != String.Empty)
				ACollection.Add(ANode);

			foreach (Node LNode in ANode.Children)
				WalkNode(ACollection, LNode, AContext);
		}

		protected class NodeReferenceComparer : IComparer<INode>
		{
			public NodeReferenceComparer(NodeReferenceConverter AConverter)
			{
				FConverter = AConverter;
			}

			private NodeReferenceConverter FConverter;

			public int Compare(INode AItem1, INode AItem2)
			{
				return String.Compare(FConverter.ConvertToString(AItem1), FConverter.ConvertToString(AItem2));
			}
		}
	}

	/// <summary> Used by MemberNameConverter to determine the referenced node. </summary>
	public interface INodeReference
	{ 
		INode Node { get; } 
	}

	/// <summary> Looks up a referenced node's member names. </summary>
	/// <remarks> The member's class must implement INodeReference. </remarks>
	public class MemberNameConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override object ConvertFrom(ITypeDescriptorContext AContext, CultureInfo ACulture, object AValue)
		{
			string s = AValue as string;
			if (s != null && s == "(None)")
				return null;
			else
				return base.ConvertFrom(AContext, ACulture, AValue);
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, CultureInfo ACulture, object AValue, Type ADestinationType) 
		{
			if (ADestinationType == typeof(string) && AValue == null)
				return "(None)";
			return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext AContext)
		{
			ArrayList LCollection = new ArrayList();
			if (AContext != null)
			{
				INodeReference LNode = AContext.Instance as INodeReference;
				if ((LNode != null) && (LNode.Node != null)) 
				{
					foreach (PropertyInfo LPropertyInfo in LNode.Node.GetType().GetProperties()) 
					{
						if (LPropertyInfo.CanRead && LPropertyInfo.CanWrite) 
						{
							bool LBrowseableFlag = true;
							Object[] LAttributes = (Object[])LPropertyInfo.GetCustomAttributes(true);
							foreach (Attribute LAttribute in LAttributes) 
							{
								if ((LAttribute is BrowsableAttribute) && (((BrowsableAttribute)LAttribute).Browsable == false))
								{
									LBrowseableFlag = false;
									break;
								}
							}
							if (LBrowseableFlag) 
								LCollection.Add(LPropertyInfo.Name);
						}
					}
				}
				LCollection.Sort(CaseInsensitiveComparer.Default);
			}
			LCollection.Insert(0, null);
			return new TypeConverter.StandardValuesCollection(LCollection);
		}
	}

	/// <summary> For use on a property of an ISourceReference implementing node, which refers to a column within the data source. </summary>
	public class ColumnNameConverter : TypeConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext AContext)
		{
			ArrayList LCollection = new ArrayList();
			if (AContext != null)
			{
				ISource LSource;
				if (AContext.Instance is ISourceChild) 
					LSource = ((ISource)((INode)AContext.Instance).Parent);
				else
				{
					if (AContext.Instance is ISourceReference) 
						LSource = ((ISourceReference)AContext.Instance).Source;
					else
						// ISourceReferenceChild
						LSource = ((ISourceReference)((INode)((ISourceReferenceChild)AContext.Instance)).Parent).Source;
				}

				if (LSource != null) 
				{
					if ((LSource != null) && (LSource.DataView != null))
						foreach (DAE.Schema.Column LColumn in LSource.DataView.TableType.Columns) 
							LCollection.Add(LColumn.Name);
					LCollection.Sort(CaseInsensitiveComparer.Default);
				}
			}
			LCollection.Insert(0, "");
			return new TypeConverter.StandardValuesCollection(LCollection);
		}
	}

	/// <summary> Use on a class or struct to identifiy itself to the designer that it is the root design node. </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class DesignRootAttribute: Attribute {}

	/// <summary> Use on a node to tell the designers to list it or not. </summary>
	/// <remarks> If missing then a default of ListInDesigner.Form should be assumed. </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class ListInDesignerAttribute : Attribute
	{
		public ListInDesignerAttribute(bool AIsListed) : base()
		{
			FIsListed = AIsListed;
		}

		private bool FIsListed;
		public bool IsListed
		{
			get { return FIsListed; }
			set { IsListed = value; }
		}
	}

	/// <summary> Use on a class or struct to identify which icon to use in the designer. </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class DesignerImageAttribute : Attribute
	{
		public DesignerImageAttribute(string AImageExpression) : base()
		{
			FImageExpression = AImageExpression;
		}

		private string FImageExpression;
		public string ImageExpression
		{
			get { return FImageExpression; }
			set { FImageExpression = value; }
		}
	}

	/// <summary> Specifies the operator to use (e.g. "Load", "Form") when building a document expression for the given property. </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DocumentExpressionOperatorAttribute : Attribute
	{
		public DocumentExpressionOperatorAttribute(string AOperatorName)
		{
			FOperatorName = AOperatorName;
		}

		private string FOperatorName;
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value; }
		}
	}
}
