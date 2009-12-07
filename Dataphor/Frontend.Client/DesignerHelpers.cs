/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

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
	
	public class ColumnNameSourcePropertyAttribute : Attribute
	{
		public ColumnNameSourcePropertyAttribute(string APropertyName)
		{
			PropertyName = APropertyName;
		}
		
		public string PropertyName { get; set; }
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
				else if (AContext.Instance is ISourceReference) 
					LSource = ((ISourceReference)AContext.Instance).Source;
				else if (AContext.Instance is ISourceReferenceChild)
					LSource = ((ISourceReference)((INode)((ISourceReferenceChild)AContext.Instance)).Parent).Source;
				else
				{
					var LPropertyNameAttribute = AContext.PropertyDescriptor.Attributes[typeof(ColumnNameSourcePropertyAttribute)] as ColumnNameSourcePropertyAttribute;
					if (LPropertyNameAttribute != null)
						LSource = AContext.Instance.GetType().GetProperty(LPropertyNameAttribute.PropertyName).GetValue(AContext.Instance, new object[] {}) as ISource;
					else
						LSource = null;
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
}
