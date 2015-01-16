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
		public LanguageModifier(string name, string tempValue) : base()
		{
			_name = name == null ? String.Empty : name;
			_value = tempValue == null ? String.Empty : tempValue;
		}
		
		// Name
		protected string _name;
		public string Name { get { return _name; } }
		
		// Value
		protected string _value = String.Empty;
		public string Value
		{
			get { return _value; }
			set { _value = value == null ? String.Empty : value; }
		}

		public int Line = -1;
		public int LinePos = -1;
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is LanguageModifier) && Schema.Object.NamesEqual(_name, ((LanguageModifier)objectValue).Name);
		}
		
		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}
		
		public LanguageModifier Copy()
		{
			return new LanguageModifier(_name, _value);
		}
    }
    
	public class LanguageModifiers : List
    {		
		protected override void Validate(object item)
		{
			LanguageModifier languageModifier = item as LanguageModifier;
			if (languageModifier == null)
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "LanguageModifier");
			if (IndexOfName(languageModifier.Name) >= 0)
				throw new LanguageException(LanguageException.Codes.DuplicateLanguageModifierName, languageModifier.Name);

			base.Validate(item);
		}
		
		public new LanguageModifier this[int index]
		{
			get { return (LanguageModifier)base[index]; }
			set { base[index] = value; }
		}
		
		public LanguageModifierEnumerator GetEnumerator()
		{
			return new LanguageModifierEnumerator(this);
		}
		
		public class LanguageModifierEnumerator : IEnumerator
		{
			public LanguageModifierEnumerator(LanguageModifiers languageModifiers) : base()
			{
				_languageModifiers = languageModifiers;
			}
			
			private LanguageModifiers _languageModifiers;
			private int _current = -1;
			
			public LanguageModifier Current { get { return _languageModifiers[_current]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				_current++;
				return _current < _languageModifiers.Count;
			}
			
			public void Reset()
			{
				_current = -1;
			}
		}
		
		public LanguageModifier this[string name]
		{
			get
			{
				int index = IndexOf(name);
				if (index >= 0)
					return this[index];
				else
					throw new LanguageException(LanguageException.Codes.LanguageModifierNotFound, name);
			}
			set
			{
				int index = IndexOf(name);
				if (index >= 0)
					this[index] = value;
				else
					throw new LanguageException(LanguageException.Codes.LanguageModifierNotFound, name);
			}
		}
		
		public int IndexOfName(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Equals(this[index].Name, name, StringComparison.OrdinalIgnoreCase))
					return index;
			return -1;
		}
		
		public int IndexOf(string name)
		{
			int languageModifierIndex = -1;
			for (int index = 0; index < Count; index++)
				if (Schema.Object.NamesEqual(this[index].Name, name))
				{
					if (languageModifierIndex >= 0)
						throw new LanguageException(LanguageException.Codes.AmbiguousModifierReference, name);
					languageModifierIndex = index;
				}
			return languageModifierIndex;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
		
		public void AddOrUpdate(string name, string tempValue)
		{
			int languageModifierIndex = IndexOf(name);
			if (languageModifierIndex < 0)
				Add(new LanguageModifier(name, tempValue));
			else
				this[languageModifierIndex].Value = tempValue;
		}
		
		public static string GetModifier(LanguageModifiers modifiers, string modifierName, string defaultValue)
		{
			if (modifiers != null)
			{
				int modifierIndex = modifiers.IndexOf(modifierName);
				if (modifierIndex >= 0)
					return modifiers[modifierIndex].Value;
			}

			return defaultValue;
		}
	}
}