using System;

namespace Alphora.Dataphor.BOP
{
	/// <summary>
	///		Use on a class or struct to identify a List property
	///		as the default parent.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class PublishDefaultListAttribute : Attribute
	{
		public PublishDefaultListAttribute(string AMemberName)
			: base()
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get { return FMemberName; }
			set { FMemberName = value; }
		}
	}

	/// <summary>
	///		Use on a class or struct to identify the constructor to 
	///		call when deserializing.
	///	</summary>
	///	<remarks>
	///		The constructor's parameters should be labeled with the
	///		<see cref="PublishSourceAttribute"></see> so the values for the
	///		parameters can be determined.
	///	</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	public class PublishDefaultConstructorAttribute : Attribute
	{
		public PublishDefaultConstructorAttribute(string AConstructorSignature)
			: base()
		{
			FConstructorSignature = AConstructorSignature;
		}

		private string FConstructorSignature;
		public string ConstructorSignature
		{
			get { return FConstructorSignature; }
			set { FConstructorSignature = value; }
		}
	}

	/// <summary>
	///		Use on parameters of the "default" constructor (as specified by
	///		<see cref="PublishDefaultConstructorAttribute"/>) to identify
	///		which member is associated with this parameter for persistance.
	/// </summary>
	/// <remarks>
	///		The referenced member may be read-only because it will not be
	///		written to.  The referenced member must be flagged with the
	///		PublishAttribute.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public class PublishSourceAttribute : Attribute
	{
		public PublishSourceAttribute(string AMemberName)
			: base()
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get { return FMemberName; }
			set { FMemberName = value; }
		}
	}

	/// <summary>
	///		Use on a class or struct to denote the member to use to identify the object
	///		uniquely within it's parent.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class PublishNameAttribute : Attribute
	{
		public PublishNameAttribute(string AMemberName)
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get { return FMemberName; }
			set { FMemberName = value; }
		}
	}

	/// <summary> Used on a member to identify another member that can be invoked to determine the default for this member. </summary>
	/// <remarks> This attribute cannot be used for properties that are deserialized as arguments for a constructor. </remarks>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
	public class DefaultValueMemberAttribute : Attribute
	{
		public DefaultValueMemberAttribute(string AMemberName)
		{
			FMemberName = AMemberName;
		}

		private string FMemberName;
		public string MemberName
		{
			get { return FMemberName; }
			set { FMemberName = value; }
		}
	}

	/// <summary>
	///		Use on a class or struct to denote what the class should be written out as when published.
	///		Usually used to make an object serialize as something that it was derived from.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class PublishAsAttribute : Attribute
	{
		public PublishAsAttribute(string AClassName)
		{
			FClassName = AClassName;
		}

		private string FClassName;
		public string ClassName
		{
			get { return FClassName; }
			set { FClassName = value; }
		}
	}

	/*
		System.ComponentModel.DefaultValueAttribute -
		
			An member can be made to not persist if it is deemed to be set to it's 
			default value.  If the value of the member is "Equal" to the value
			indicated by this attribute, or vise versa.
	*/

	/// <summary>
	///		Used by <see cref="PublishAttribute"/> to specify the method of	persistance.
	/// </summary>
	/// <remarks>
	///		None - Does no persistence
	///		Value - Persist the value or reference as a single attribute.
	///		Inline - Persist the value or reference as a child.
	///		List - Persist each item in the IList Inline.
	/// </remarks>
	public enum PublishMethod
	{
		None,
		Value,
		Inline,
		List
	}

	/// <summary>
	///		Use on a value, reference or List property to identify
	///		it for persistance.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
	public class PublishAttribute : Attribute
	{
		public PublishAttribute() : base() { }

		public PublishAttribute(PublishMethod AMethod)
			: base()
		{
			FMethod = AMethod;
		}

		private PublishMethod FMethod = PublishMethod.Value;
		public PublishMethod Method
		{
			get { return FMethod; }
			set { FMethod = value; }
		}
	}
}
