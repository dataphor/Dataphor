using System;

namespace System.ComponentModel.Design.Serialization
{
	public class DesignerSerializerAttribute : Attribute
	{
		public DesignerSerializerAttribute(Type A, Type B) { }
		public DesignerSerializerAttribute(string A, string B) { }
	}
}
