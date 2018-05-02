using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQF.Model
{
	public partial class Base
	{
		public static bool Equals(Base left, Base right)
		{
			if (left != null)
				return left.Equals(right);

			return right == null;
		}
	}

	public partial class ChoiceTypeInfo
	{
		public override bool Equals(object other)
		{
			var result = base.Equals(other);

			if (result)
			{
				var that = other as ChoiceTypeInfo;

				if (that == null)
					return false;

				if (this.type.Count != that.type.Count)
					return false;

				// TODO: Should be value-based, not position-based, but okay for now
				for (int i = 0; i < this.type.Count; i++)
					if (!this.type[i].Equals(that.type[i]))
						return false;
			}

			return result;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ChoiceTypeSpecifier
	{
		public override bool Equals(object other)
		{
			var that = other as ChoiceTypeSpecifier;
			if (that == null || this.choice == null || that.choice == null)
				return false;

			if (this.choice.Count() != that.choice.Count())
				return false;

			// TODO: Should be value-based, but okay for now
			for (int i = 0; i < this.choice.Count(); i++)
				if (!Base.Equals(this.choice[i], that.choice[i]))
					return false;

			return true;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ClassInfo
	{
		public override bool Equals(object other)
		{
			var result = base.Equals(other);

			if (result)
			{
				var that = other as ClassInfo;

				if (that == null)
					return false;

				// TODO: Should be more than just name, but okay for now
				if (this.name != that.name)
					return false;
			}

			return result;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ClassInfoElement
	{
		public override bool Equals(object other)
		{
			var that = other as ClassInfoElement;
			return
				this.name == that.name
					&& (Base.Equals(this.type, that.type) || Base.Equals(this.typeSpecifier, that.typeSpecifier))
					&& this.oneBasedSpecified == that.oneBasedSpecified
					&& (!this.oneBasedSpecified || this.oneBased == that.oneBased)
					&& this.prohibitedSpecified == that.prohibitedSpecified
					&& (!this.prohibitedSpecified || this.prohibitedSpecified == that.prohibitedSpecified);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ConversionInfo
	{
		public override bool Equals(object other)
		{
			var that = other as ConversionInfo;
			return
				(Base.Equals(this.fromType, that.fromType) || Base.Equals(this.fromTypeSpecifier, that.fromTypeSpecifier))
					&& (Base.Equals(this.toType, that.toType) || Base.Equals(this.toTypeSpecifier, that.toTypeSpecifier))
					&& this.functionName == that.functionName;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class IntervalTypeInfo
	{
		public override bool Equals(object other)
		{
			var result = base.Equals(other);

			if (result)
			{
				var that = other as IntervalTypeInfo;

				if (that == null)
					return false;

				// TODO: Should be value-based, but okay for now
				return (this.pointType != null && this.pointType.Equals(that.pointType))
					|| (this.pointTypeSpecifier != null && this.pointTypeSpecifier.Equals(that.pointTypeSpecifier));
			}

			return result;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class IntervalTypeSpecifier
	{
		public override bool Equals(object other)
		{
			var that = other as IntervalTypeSpecifier;
			return
				that != null
					&& (
						(this.pointType != null && this.pointType.Equals(that.pointType))
							|| (this.pointTypeSpecifier != null && this.pointTypeSpecifier.Equals(that.pointTypeSpecifier))
					);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ListTypeInfo
	{
		public override bool Equals(object other)
		{
			var result = base.Equals(other);

			if (result)
			{
				var that = other as ListTypeInfo;

				if (that == null)
					return false;

				// TODO: Should be value-based but okay for now
				return
					(this.elementType != null && this.elementType.Equals(that.elementType))
						|| (this.elementTypeSpecifier != null && this.elementTypeSpecifier.Equals(that.elementTypeSpecifier));
			}

			return result;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
	
	public partial class ListTypeSpecifier
	{
		public override bool Equals(object other)
		{
			var that = other as ListTypeSpecifier;
			return
				that != null
					&& (
						(this.elementType != null && this.elementType.Equals(that.elementType)) 
							|| (this.elementTypeSpecifier != null && this.elementTypeSpecifier.Equals(that.elementTypeSpecifier))
					);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ModelInfo
	{
		public override bool Equals(object other)
		{
			// TODO: Should be a full equals, but I doubt it matters at this point
			var that = other as ModelInfo;
			return that != null && this.name == that.name && this.version == that.version;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ModelSpecifier
	{
		public override bool Equals(object other)
		{
			var that = other as ModelSpecifier;
			return that != null && this.name == that.name && this.version == that.version;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class NamedTypeSpecifier
	{
		public override bool Equals(object other)
		{
			var that = other as NamedTypeSpecifier;
			return
				that != null
					&& (this.modelName == null || this.modelName == that.modelName)
					&& this.name == that.name;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class ProfileInfo
	{
		public override bool Equals(object other)
		{
			return base.Equals(other);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class SimpleTypeInfo
	{
		public override bool Equals(object other)
		{
			var result = base.Equals(other);

			if (result)
			{
				var that = other as SimpleTypeInfo;
				return that != null && this.name == that.name;
			}

			return result;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class TupleTypeInfo
	{
		public override bool Equals(object other)
		{
			var result = base.Equals(other);

			if (result)
			{
				var that = other as TupleTypeInfo;
				if (that == null)
					return false;

				// TODO: Should be name-based, but I doubt it matters for these purposes
				if (this.element.Count == that.element.Count)
					for (int i = 0; i < this.element.Count; i++)
						if (!Base.Equals(this.element[i], that.element[i]))
							return false;
			}

			return result;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class TupleTypeInfoElement
	{
		public override bool Equals(object other)
		{
			var that = other as TupleTypeInfoElement;
			return
				that != null
					&& this.name == that.name
					&& ((this.type != null && this.type.Equals(that.type)) || (this.typeSpecifier != null && this.typeSpecifier.Equals(that.typeSpecifier)))
					&& this.oneBasedSpecified == that.oneBasedSpecified
					&& (!this.oneBasedSpecified || this.oneBased == that.oneBased)
					&& this.prohibitedSpecified == that.prohibitedSpecified
					&& (!this.prohibitedSpecified || this.prohibitedSpecified == that.prohibitedSpecified);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class TypeInfo
	{
		public override bool Equals(object other)
		{
			var that = other as TypeInfo;
			return
				that != null
				  && (
					(this.baseType == null && this.baseTypeSpecifier == null && that.baseType == null && that.baseTypeSpecifier == null) 
						|| Base.Equals(this.baseType, that.baseType) 
						|| Base.Equals(this.baseTypeSpecifier, that.baseTypeSpecifier)
				);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public partial class TypeSpecifier
	{
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
