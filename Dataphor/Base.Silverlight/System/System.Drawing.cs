using System;

namespace System.Drawing
{
	public class ToolboxBitmapAttribute : Attribute 
	{
		public ToolboxBitmapAttribute(Type A, string B) { }
	}
	
	public class ToolboxItemAttribute : Attribute
	{
		public ToolboxItemAttribute(bool A) { }
	}
}