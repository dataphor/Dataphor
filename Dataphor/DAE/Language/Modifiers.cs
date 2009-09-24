/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Language
{
    /// <remarks> LanguageModifier </remarks>
    public class LanguageModifier : System.Object
    {
		// constructor
		public LanguageModifier(string AName, string AValue) : base()
		{
			FName = AName == null ? String.Empty : AName;
			FValue = AValue == null ? String.Empty : AValue;
		}
		
		// Name
		protected string FName;
		public string Name { get { return FName; } }
		
		// Value
		protected string FValue = String.Empty;
		public string Value
		{
			get { return FValue; }
			set { FValue = value == null ? String.Empty : value; }
		}

		public int Line = -1;
		public int LinePos = -1;
		
		public override bool Equals(object AObject)
		{
			return (AObject is LanguageModifier) && Schema.Object.NamesEqual(FName, ((LanguageModifier)AObject).Name);
		}
		
		public override int GetHashCode()
		{
			return FName.GetHashCode();
		}
		
		public LanguageModifier Copy()
		{
			return new LanguageModifier(FName, FValue);
		}
    }
    
	public class LanguageModifiers : List
    {		
		protected override void Validate(object AItem)
		{
			LanguageModifier LLanguageModifier = AItem as LanguageModifier;
			if (LLanguageModifier == null)
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "LanguageModifier");
			if (IndexOfName(LLanguageModifier.Name) >= 0)
				throw new LanguageException(LanguageException.Codes.DuplicateLanguageModifierName, LLanguageModifier.Name);

			base.Validate(AItem);
		}
		
		public new LanguageModifier this[int AIndex]
		{
			get { return (LanguageModifier)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public LanguageModifierEnumerator GetEnumerator()
		{
			return new LanguageModifierEnumerator(this);
		}
		
		public class LanguageModifierEnumerator : IEnumerator
		{
			public LanguageModifierEnumerator(LanguageModifiers ALanguageModifiers) : base()
			{
				FLanguageModifiers = ALanguageModifiers;
			}
			
			private LanguageModifiers FLanguageModifiers;
			private int FCurrent = -1;
			
			public LanguageModifier Current { get { return FLanguageModifiers[FCurrent]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				FCurrent++;
				return FCurrent < FLanguageModifiers.Count;
			}
			
			public void Reset()
			{
				FCurrent = -1;
			}
		}
		
		public LanguageModifier this[string AName]
		{
			get
			{
				int LIndex = IndexOf(AName);
				if (LIndex >= 0)
					return this[LIndex];
				else
					throw new LanguageException(LanguageException.Codes.LanguageModifierNotFound, AName);
			}
			set
			{
				int LIndex = IndexOf(AName);
				if (LIndex >= 0)
					this[LIndex] = value;
				else
					throw new LanguageException(LanguageException.Codes.LanguageModifierNotFound, AName);
			}
		}
		
		public int IndexOfName(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Equals(this[LIndex].Name, AName, StringComparison.OrdinalIgnoreCase))
					return LIndex;
			return -1;
		}
		
		public int IndexOf(string AName)
		{
			int LLanguageModifierIndex = -1;
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (Schema.Object.NamesEqual(this[LIndex].Name, AName))
				{
					if (LLanguageModifierIndex >= 0)
						throw new LanguageException(LanguageException.Codes.AmbiguousModifierReference, AName);
					LLanguageModifierIndex = LIndex;
				}
			return LLanguageModifierIndex;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
		
		public void AddOrUpdate(string AName, string AValue)
		{
			int LLanguageModifierIndex = IndexOf(AName);
			if (LLanguageModifierIndex < 0)
				Add(new LanguageModifier(AName, AValue));
			else
				this[LLanguageModifierIndex].Value = AValue;
		}
		
		public static string GetModifier(LanguageModifiers AModifiers, string AModifierName, string ADefaultValue)
		{
			if (AModifiers != null)
			{
				int LModifierIndex = AModifiers.IndexOf(AModifierName);
				if (LModifierIndex >= 0)
					return AModifiers[LModifierIndex].Value;
			}

			return ADefaultValue;
		}
	}
}