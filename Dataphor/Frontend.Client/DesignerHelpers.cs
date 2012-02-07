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
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
		{
			if (sourceType == typeof(string))
				return true;
			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				return null;
			else
			{
				string s = value as string;
				if (s != null)
				{
					if (s == "(None)")
						return null;
					else
						return ((INode)context.Instance).HostNode.FindNode(s);
				}
				else
					return base.ConvertFrom(context, culture, value);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) 
		{
			if (destinationType == typeof(string))
			{
				if (value == null)
					return "(None)";
				if(value is String && (String)value == "(None)")
					return value;

                return ((INode)value).Name;
			}
			else
				return base.ConvertTo(context, culture, value, destinationType);
		}

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			List<INode> collection = new List<INode>();
			if (context != null)
			{
				foreach (INode node in ((INode)context.Instance).HostNode.Children[0].Children) 
					WalkNode(collection, node, context);

				collection.Sort(new NodeReferenceComparer(this));
			}
			collection.Insert(0, null);
			return new TypeConverter.StandardValuesCollection(collection);
		}

		// checks a node and recurses to each of it's children.
		private void WalkNode(List<INode> collection, INode node, ITypeDescriptorContext context) 
		{
			if (context.PropertyDescriptor.PropertyType.IsAssignableFrom(node.GetType()) && node.Name != String.Empty)
				collection.Add(node);

			foreach (Node localNode in node.Children)
				WalkNode(collection, localNode, context);
		}

		protected class NodeReferenceComparer : IComparer<INode>
		{
			public NodeReferenceComparer(NodeReferenceConverter converter)
			{
				_converter = converter;
			}

			private NodeReferenceConverter _converter;

			public int Compare(INode item1, INode item2)
			{
				return String.Compare(_converter.ConvertToString(item1), _converter.ConvertToString(item2));
			}
		}
	}

	/// <summary> Looks up a referenced node's member names. </summary>
	/// <remarks> The member's class must implement INodeReference. </remarks>
	public class MemberNameConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string s = value as string;
			if (s != null && s == "(None)")
				return null;
			else
				return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) 
		{
			if (destinationType == typeof(string) && value == null)
				return "(None)";
			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			ArrayList collection = new ArrayList();
			if (context != null)
			{
				INodeReference node = context.Instance as INodeReference;
				if ((node != null) && (node.Node != null)) 
				{
					foreach (PropertyInfo propertyInfo in node.Node.GetType().GetProperties()) 
					{
						if (propertyInfo.CanRead && propertyInfo.CanWrite) 
						{
							bool browseableFlag = true;
							Object[] attributes = (Object[])propertyInfo.GetCustomAttributes(true);
							foreach (Attribute attribute in attributes) 
							{
								if ((attribute is BrowsableAttribute) && (((BrowsableAttribute)attribute).Browsable == false))
								{
									browseableFlag = false;
									break;
								}
							}
							if (browseableFlag) 
								collection.Add(propertyInfo.Name);
						}
					}
				}
				collection.Sort(CaseInsensitiveComparer.Default);
			}
			collection.Insert(0, null);
			return new TypeConverter.StandardValuesCollection(collection);
		}
	}
	
	public class ColumnNameSourcePropertyAttribute : Attribute
	{
		public ColumnNameSourcePropertyAttribute(string propertyName)
		{
			PropertyName = propertyName;
		}
		
		public string PropertyName { get; set; }
	}

	/// <summary> For use on a property of an ISourceReference implementing node, which refers to a column within the data source. </summary>
	public class ColumnNameConverter : TypeConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			ArrayList collection = new ArrayList();
			if (context != null)
			{
				ISource source;
				if (context.Instance is ISourceChild) 
					source = ((ISource)((INode)context.Instance).Parent);
				else if (context.Instance is ISourceReference) 
					source = ((ISourceReference)context.Instance).Source;
				else if (context.Instance is ISourceReferenceChild)
					source = ((ISourceReference)((INode)((ISourceReferenceChild)context.Instance)).Parent).Source;
				else
				{
					var propertyNameAttribute = context.PropertyDescriptor.Attributes[typeof(ColumnNameSourcePropertyAttribute)] as ColumnNameSourcePropertyAttribute;
					if (propertyNameAttribute != null)
						source = context.Instance.GetType().GetProperty(propertyNameAttribute.PropertyName).GetValue(context.Instance, new object[] {}) as ISource;
					else
						source = null;
				}

				if (source != null) 
				{
					if ((source != null) && (source.DataView != null))
						foreach (DAE.Schema.Column column in source.DataView.TableType.Columns) 
							collection.Add(column.Name);
					collection.Sort(CaseInsensitiveComparer.Default);
				}
			}
			collection.Insert(0, "");
			return new TypeConverter.StandardValuesCollection(collection);
		}
	}
}
