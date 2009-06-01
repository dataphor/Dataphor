/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define USETAGNAMECACHE
// This define uses a tag name cache for the names of all tags.
// The idea is to try to reduce the amount of memory used for tags by using
// the same string reference for each tag of a given name.
// However, early indications are that the amount of memory saved is not significant,
// even when dealing with large catalogs such as IBAS.

using System;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Language.D4
{
    /// <remarks> Tag </remarks>
    public class Tag : System.Object
    {
		// constructor
		public Tag(string AName) : base()
		{
			if ((AName == null) || (AName == String.Empty))
				throw new SchemaException(SchemaException.Codes.TagNameRequired);
			#if USETAGNAMECACHE
			FName = TagNames.GetTagName(AName);
			#else
			FName = AName;
			#endif
		}
		
		public Tag(string AName, string AValue) : base()
		{
			if ((AName == null) || (AName == String.Empty))
				throw new SchemaException(SchemaException.Codes.TagNameRequired);
			#if USETAGNAMECACHE
			FName = TagNames.GetTagName(AName);
			#else
			FName = AName;
			#endif
			FValue = AValue == null ? String.Empty : AValue;
		}
		
		public Tag(string AName, string AValue, bool AIsInherited) : base()
		{
			if ((AName == null) || (AName == String.Empty))
				throw new SchemaException(SchemaException.Codes.TagNameRequired);
			#if USETAGNAMECACHE
			FName = TagNames.GetTagName(AName);
			#else
			FName = AName;
			#endif
			FValue = AValue;
			FIsInherited = AIsInherited;
		}

		public Tag(string AName, string AValue, bool AIsInherited, bool AIsStatic) : base()
		{
			if ((AName == null) || (AName == String.Empty))
				throw new SchemaException(SchemaException.Codes.TagNameRequired);
			#if USETAGNAMECACHE
			FName = TagNames.GetTagName(AName);
			#else
			FName = AName;
			#endif
			FValue = AValue;
			FIsInherited = AIsInherited;
			FIsStatic = AIsStatic;
		}
		
		// Name
		protected string FName;
		public string Name { get { return FName; } }
		
		// Value
		protected string FValue = String.Empty;
		/// <summary>The value of the tag. Changing the value of this property will set IsInherited to false for this tag.</summary>
		public string Value
		{
			get { return FValue; }
			set
			{
				if (FValue != value)
				{
					FValue = value == null ? String.Empty : value;
					if (FIsInherited)
						FIsInherited = false;
				}
			}
		}

		// IsInherited
		protected bool FIsInherited;
		/// <summary>Indicates whether this tag was inferred from a dependency or defined directly on the object.</summary>
		public bool IsInherited
		{
			get { return FIsInherited; }
			set 
			{ 
				if (FIsInherited != value)
				{
					FIsInherited = value; 
				}
			}
		}

		//IsStatic
		protected bool FIsStatic;
		/// <summary>Indicates whether this tag should be inferred through expressions.</summary>
		public bool IsStatic
		{
			get { return FIsStatic; }
			set
			{
				if (FIsStatic != value)
				{
					FIsStatic = value;
				}
			}
		}
		
		public int Line = -1;
		public int LinePos = -1;
		
		public override bool Equals(object AObject)
		{
			Tag LTag = AObject as Tag;
			if (LTag == null)
			{
				TagReference LTagReference = AObject as TagReference;
				if (LTagReference != null)
					LTag = LTagReference.Tag;
			}
			return (LTag != null) && (FName == LTag.Name);
		}
		
		public override int GetHashCode()
		{
			return FName.GetHashCode();
		}

		/// <summary>Returns a copy of this tag.</summary>
		public Tag Copy()
		{
			return InternalCopy(FName);
		}
		
		/// <summary>Returns a copy of this tag with the name prefixed by ANameSpace.</summary>
		public Tag Copy(string ANameSpace)
		{
			return InternalCopy(Schema.Object.Qualify(FName, ANameSpace));
		}
		
		protected virtual Tag InternalCopy(string AName)
		{
			return new Tag(AName, FValue, FIsInherited, FIsStatic);
		}

		/// <summary>Returns a copy of this tag with the IsInherited property set to true.</summary>		
		public Tag Inherit()
		{
			if (FIsStatic)
				Error.Fail(@"Static tag ""{0}"" cannot be inherited.", FName);
			return InternalInherit(FName);
		}

		/// <summary>Returns a copy of this tag with the IsInherited property set to true, and with the tag name prefixed by ANameSpace.</summary>		
		public Tag Inherit(string ANameSpace)
		{
			return InternalInherit(Schema.Object.Qualify(FName, ANameSpace));
		}
		
		protected virtual Tag InternalInherit(string AName)
		{
			return new Tag(AName, FValue, true, FIsStatic);
		}
    }

    public class TagReference : System.Object
    {		
		public TagReference(Tag ATag) : base()
		{
			if (ATag == null)
				throw new LanguageException(LanguageException.Codes.TagReferenceRequired);
			FTag = ATag;
		}
		
		private Tag FTag;
		public Tag Tag
		{
			get { return FTag; }
		}

		/// <summary>Returns a new TagReference to the underlying tag for this reference.</summary>		
		public TagReference Copy()
		{
			return new TagReference(FTag);
		}
		
		public override bool Equals(object AObject)
		{
			Tag LTag = AObject as Tag;
			if (LTag == null)
			{
				TagReference LTagReference = AObject as TagReference;
				if (LTagReference != null)
					LTag = LTagReference.Tag;
			}
			
			return (LTag != null) && (FTag.Name == LTag.Name);
		}
		
		public override int GetHashCode()
		{
			return FTag.GetHashCode();
		}
    }
    
	public class Tags : System.Object
	{
		#if USEHASHTABLEFORTAGS
		private Hashtable FTags = new Hashtable();
		#else
		private ArrayList FTags = new ArrayList();
		
		private int IndexOfName(string AName)
		{
			object LObject;
			Tag LTag;
			TagReference LTagReference;
			for (int LIndex = 0; LIndex < FTags.Count; LIndex++)
			{
				LObject = FTags[LIndex];
				LTag = LObject as Tag;
				if (LTag == null)
				{
					LTagReference = LObject as TagReference;
					if (LTagReference != null)
						LTag = LTagReference.Tag;
				}
				
				if (LTag.Name == AName)
					return LIndex;
			}
			
			return -1;
		}
		
		public Tag this[int AIndex]
		{
			get
			{
				Tag LTag = FTags[AIndex] as Tag;
				if (LTag == null)
					LTag = ((TagReference)FTags[AIndex]).Tag;
				return LTag;
			}
		}
		
		#endif
		
		/// <summary>Returns the Tag for the given name, raises an error if the Tag does not exist.</summary>		
		public Tag this[string AName]
		{
			get
			{
				#if USEHASHTABLEFORTAGS
				object LObject = FTags[AName];
				Tag LTag = LObject as Tag;
				if (LTag == null)
				{
					TagReference LTagReference = LObject as TagReference;
					if (LTagReference != null)
						LTag = LTagReference.Tag;
				}
				
				if (LTag == null)
					throw new SchemaException(SchemaException.Codes.TagNotFound, AName);
					
				return LTag;
				#else
				return GetTag(AName, true);
				#endif
			}
		}
		
		/// <summary>Returns the Tag for the given name, if one exists, null otherwise.</summary>
		public Tag GetTag(string AName)
		{
			return GetTag(AName, false);
		}
		
		/// <summary>Returns the Tag for the given name. If the tag does not exist, an error is thrown if AShouldThrow is true, otherwise, null is returned.</summary>
		public Tag GetTag(string AName, bool AShouldThrow)
		{
			#if USEHASHTABLEFORTAGS
			object LObject = FTags[AName];
			if (LObject != null)
			{
				Tag LTag = LObject as Tag;
				if (LTag != null)
					return LTag;

				return ((TagReference)LObject).Tag;
			}
			
			return null;
			#else
			object LObject;
			Tag LTag;
			TagReference LTagReference;
			for (int LIndex = 0; LIndex < FTags.Count; LIndex++)
			{
				LObject = FTags[LIndex];
				LTag = LObject as Tag;
				if (LTag == null)
				{
					LTagReference = LObject as TagReference;
					if (LTagReference != null)
						LTag = LTagReference.Tag;
				}
				
				if (LTag.Name == AName)
					return LTag;
			}
			
			if (AShouldThrow)
				throw new SchemaException(SchemaException.Codes.TagNotFound, AName);
			return null;
			#endif
		}
		
		/// <summary>Returns the value of the specified tag, returning ADefaultValue if the tag does not exist.</summary>
		public string GetTagValue(string AName, string ADefaultValue)
		{
			#if USEHASHTABLESFORTAGS
			object LValue = FTags[AName];
			
			Tag LTag = LValue as Tag;
			if (LTag != null)
				return LTag.Value;
				
			TagReference LTagReference = LValue as TagReference;
			if (LTagReference != null)
				return LTagReference.Tag.Value;
				
			return ADefaultValue;
			#else
			Tag LTag = GetTag(AName);
			if (LTag != null)
				return LTag.Value;
			return ADefaultValue;
			#endif
		}
		
		/// <summary>Returns the value of the specific tag, returning the value of the default tag if the tag does not exist, and ADefaultValue if the default tag does not exist.</summary>
		public string GetTagValue(string AName, string ADefaultName, string ADefaultValue)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[AName];
			if (LValue == null)
				LValue = FTags[ADefaultName];
				
			Tag LTag = LValue as Tag;
			if (LTag != null)
				return LTag.Value;
			
			TagReference LTagReference = LValue as TagReference;
			if (LTagReference != null)
				return LTagReference.Tag.Value;
			
			return ADefaultValue;
			#else
			Tag LTag = GetTag(AName);
			if (LTag == null)
				LTag = GetTag(ADefaultName);
			if (LTag != null)
				return LTag.Value;
			return ADefaultValue;
			#endif
		}
		
		/// <summary>Returns true if the specified tag is a reference.</summary>
		public bool IsReference(string AName)
		{
			#if USEHASHTABLEFORTAGS
			return FTags[AName] is TagReference;
			#else
			return FTags[IndexOfName(AName)] is TagReference;
			#endif
		}
		
		#if !USEHASHTABLEFORTAGS
		public bool IsReference(int AIndex)
		{
			return FTags[AIndex] is TagReference;
		}
		#endif
		
		public int Count { get { return FTags.Count; } }

		#if USEHASHTABLEFORTAGS		
		public TagEnumerator GetEnumerator()
		{
			return new TagEnumerator(FTags);
		}
		
		public class TagEnumerator : IEnumerator
		{
			public TagEnumerator(Hashtable ATags) : base()
			{
				FEnumerator = ATags.GetEnumerator();
			}
			
			private IDictionaryEnumerator FEnumerator;
			
			public Tag Current 
			{ 
				get 
				{ 
					Tag LTag = FEnumerator.Value as Tag;
					if (LTag != null)
						return LTag;
					else
						return ((TagReference)FEnumerator.Value).Tag;
				} 
			}
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				return FEnumerator.MoveNext();
			}
			
			public void Reset()
			{
				FEnumerator.Reset();
			}
		}
		#endif
		
		/// <summary>Adds the given tag, replacing a reference or inherited tag of the same name.</summary>
		/// <remarks>If a tag already exists of the same name as the tag being added, the tag will be removed if it is a reference or inherited tag. Otherwise, an error will be raised.</remarks>
		public void Add(Tag ATag)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[ATag.Name];
			if (LValue != null)
				if ((LValue is TagReference) || ((Tag)LValue).IsInherited)
					FTags.Remove(ATag.Name);
				else
					throw new SchemaException(SchemaException.Codes.DuplicateTagName, ATag.Name);
			
			FTags.Add(ATag.Name, ATag);
			#else
			int LIndex = IndexOfName(ATag.Name);
			if (LIndex >= 0)
				if ((FTags[LIndex] is TagReference) || ((Tag)FTags[LIndex]).IsInherited)
					FTags.RemoveAt(LIndex);
				else
					throw new SchemaException(SchemaException.Codes.DuplicateTagName, ATag.Name);
					
			FTags.Add(ATag);
			#endif
		}
		
		/// <summary>Inherits the given tag into this tag list.</summary>
		/// <remarks>
		/// If a tag with the same name as the given tag does not exist, the given tag is referenced. If the existing tag with the same name as the given tag
		/// is a reference, the tag reference is replaced by a call to the Inherit method of the given tag. Otherwise, the value of the existing tag is set
		/// based on the value of the given tag. Note that changing the value of the tag will cause the IsInherited property to be set to false.
		/// </remarks>
		public void Inherit(Tag ATag)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[ATag.Name];
			if (LValue == null)
				FTags.Add(ATag.Name, new TagReference(ATag));
			else
			{
				TagReference LTagReference = LValue as TagReference;
				if (LTagReference != null)
					FTags[ATag.Name] = ATag.Inherit();
				else
					((Tag)LValue).Value = ATag.Value;
			}
			#else
			int LIndex = IndexOfName(ATag.Name);
			if (LIndex < 0)
				FTags.Add(new TagReference(ATag));
			else
			{
				if (FTags[LIndex] is TagReference)
					FTags[LIndex] = ATag.Inherit();
				else
					((Tag)FTags[LIndex]).Value = ATag.Value;
			}
			#endif
		}
		
		/// <summary>Joins the given tag to this tag list using copy semantics.</summary>
		/// <remarks>
		/// If a tag with the same name as the given tag does not exist, the given tag is copied by calling the Copy method of the given tag.
		/// If the value of the given tag is different than the value of the existing tag, the existing tag is removed. Otherwise, the
		/// existing tag remains and no action is taken.
		/// </remarks>
		public void Join(Tag ATag)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[ATag.Name];
			if (LValue == null)
				FTags.Add(ATag.Name, ATag.Copy());
			else
			{
				Tag LTag = LValue as Tag;
				if (LTag == null)
					LTag = ((TagReference)LValue).Tag;
					
				if (LTag.Value != ATag.Value)
					FTags.Remove(ATag.Name);
			}
			#else
			int LIndex = IndexOfName(ATag.Name);
			if (LIndex < 0)
				FTags.Add(ATag.Copy());
			else
			{
				Tag LTag = FTags[LIndex] as Tag;
				if (LTag == null)
					LTag = ((TagReference)FTags[LIndex]).Tag;
					
				if (LTag.Value != ATag.Value)
					FTags.RemoveAt(LIndex);
			}
			#endif
		}
		
		/// <summary>Joins the given tag to this list using reference semantics.</summary>
		/// <remarks>
		/// If a tag with the same name as the given tag does not exist, the given tag is referenced in this tag list.
		/// If the value of the given tag is different than the value of the existing tag, the existing tag is removed.
		/// Otherwise, the existing tag remains and no action is taken.
		/// </remarks>
		public void JoinInherit(Tag ATag)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[ATag.Name];
			if (LValue == null)
				FTags.Add(ATag.Name, new TagReference(ATag));
			else
			{
				Tag LTag = LValue as Tag;
				if (LTag == null)
					LTag = ((TagReference)LValue).Tag;
					
				if (LTag.Value != ATag.Value)
					FTags.Remove(ATag.Name);
			}
			#else
			int LIndex = IndexOfName(ATag.Name);
			if (LIndex < 0)
				FTags.Add(new TagReference(ATag));
			else
			{
				Tag LTag = FTags[LIndex] as Tag;
				if (LTag == null)
					LTag = ((TagReference)FTags[LIndex]).Tag;
					
				if (LTag.Value != ATag.Value)
					FTags.RemoveAt(LIndex);
			}
			#endif
		}
		
		/// <summary>Updates the value and static setting of the given tag.</summary>
		/// <remarks>
		/// If a tag with the given name does not exist, an error is raised.
		/// If a tag with the given name is being referenced, that tag reference is replaced with a new tag
		/// with the given value and static setting. Otherwise, the tag is updated with the given value
		/// and static setting, and the IsInherited property of the tag is set to false.
		/// </remarks>
		public void Update(string ATagName, string ATagValue, bool AIsStatic)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[ATagName];
			if (LValue != null)
			{
				Tag LTag = LValue as Tag;
				if (LTag != null)
				{
					LTag.Value = ATagValue;
					LTag.IsStatic = AIsStatic;
					LTag.IsInherited = false;
				}
				else
					FTags[ATagName] = new Tag(ATagName, ATagValue, false, AIsStatic);
			}
			else
				throw new SchemaException(SchemaException.Codes.TagNotFound, ATagName);
			#else
			int LIndex = IndexOfName(ATagName);
			if (LIndex >= 0)
			{
				Tag LTag = FTags[LIndex] as Tag;
				if (LTag != null)
				{
					LTag.Value = ATagValue;
					LTag.IsStatic = AIsStatic;
					LTag.IsInherited = false;
				}
				else
					FTags[LIndex] = new Tag(ATagName, ATagValue, false, AIsStatic);
			}
			else
				throw new SchemaException(SchemaException.Codes.TagNotFound, ATagName);
			#endif
		}
		
		/// <summary>Updates the value of the given tag, and sets IsStatic to false.</summary>
		/// <remarks>This overload is equivalent to calling Update(ATagName, ATagValue, false)</remarks>
		public void Update(string ATagName, string ATagValue)
		{
			Update(ATagName, ATagValue, false);
		}
		
		/// <summary>Updates the value of the tag with the same name as the given tag to the value and static setting of the given tag.</summary>
		/// <remarks>This overload is equivalent to calling Update(ATag.Name, ATag.Value, ATag.IsStatic)</remarks>
		public void Update(Tag ATag)
		{
			Update(ATag.Name, ATag.Value, ATag.IsStatic);
		}
		
		/// <summary>Updates or adds a tag of the given name with the given value and static setting.</summary>
		/// <remarks>
		/// If a tag of the given name does not exist, a tag will be created and added.
		/// If a tag with the given name is being referenced, that tag reference is replaced with a new tag
		/// with the given value and static setting. Otherwise, the tag is updated with the given value
		/// and static setting, and the IsInherited property of the tag is set to false.
		/// </remarks>
		public void AddOrUpdate(string ATagName, string ATagValue, bool AIsStatic)
		{
			#if USEHASHTABLEFORTAGS
			object LValue = FTags[ATagName];
			if (LValue == null)
				FTags.Add(ATagName, new Tag(ATagName, ATagValue, false, AIsStatic));
			else
			{
				Tag LTag = LValue as Tag;
				if (LTag != null)
				{
					LTag.Value = ATagValue;
					LTag.IsStatic = AIsStatic;
					LTag.IsInherited = false;
				}
				else
					FTags[ATagName] = new Tag(ATagName, ATagValue, false, AIsStatic);
			}
			#else
			int LIndex = IndexOfName(ATagName);
			if (LIndex < 0)
				FTags.Add(new Tag(ATagName, ATagValue, false, AIsStatic));
			else
			{
				Tag LTag = FTags[LIndex] as Tag;
				if (LTag != null)
				{
					LTag.Value = ATagValue;
					LTag.IsStatic = AIsStatic;
					LTag.IsInherited = false;
				}
				else
					FTags[LIndex] = new Tag(ATagName, ATagValue, false, AIsStatic);
			}
			#endif
		}
		
		/// <summary>Add or updates the given tag, and sets IsStatic to false.</summary>
		/// <remarks>This overload is equivalent to calling AddOrUpdate(ATagName, ATagValue, false)</remarks>
		public void AddOrUpdate(string ATagName, string ATagValue)
		{
			AddOrUpdate(ATagName, ATagValue, false);
		}
		
		/// <summary>Adds or updates the tag with the same name as the given tag to the value and static setting of the given tag.</summary>
		/// <remarks>This overload is equivalent to calling AddOrUpdate(ATag.Name, ATag.Value, ATag.IsStatic)</remarks>
		public void AddOrUpdate(Tag ATag)
		{
			AddOrUpdate(ATag.Name, ATag.Value, ATag.IsStatic);
		}
		
		/// <summary>Calls Add for each Tag in the given array.</summary>
		public void AddRange(Tag[] ATags)
		{
			for (int LIndex = 0; LIndex < ATags.Length; LIndex++)
				Add(ATags[LIndex]);
		}
		
		/// <summary>Calls Add for each Tag in the given list.</summary>
		public void AddRange(Tags ATags)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
				Add(LTag);
			#else
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
				Add(ATags[LIndex]);
			#endif
		}
		
		/// <summary>Calls Update for each Tag in the given array.</summary>
		public void UpdateRange(Tag[] ATags)
		{
			for (int LIndex = 0; LIndex < ATags.Length; LIndex++)
				Update(ATags[LIndex]);
		}
		
		/// <summary>Calls Update for each Tag in the given list.</summary>
		public void UpdateRange(Tags ATags)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
				Update(LTag);
			#else
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
				Update(ATags[LIndex]);
			#endif
		}

		/// <summary>Calls AddOrUpdate for each Tag in the given array.</summary>
		public void AddOrUpdateRange(Tag[] ATags)
		{
			for (int LIndex = 0; LIndex < ATags.Length; LIndex++)
				AddOrUpdate(ATags[LIndex]);
		}
		
		/// <summary>Calls AddOrUpdate for each Tag in the given list.</summary>
		public void AddOrUpdateRange(Tags ATags)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
				AddOrUpdate(LTag);
			#else
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
				AddOrUpdate(ATags[LIndex]);
			#endif
		}
		
		/// <summary>Removes the tag of the given name if it exists. Otherwise, no action is taken.</summary>
		public void SafeRemove(string ATagName)
		{
			#if USEHASHTABLEFORTAGS
			FTags.Remove(ATagName);
			#else
			int LIndex = IndexOfName(ATagName);
			if (LIndex >= 0)
				FTags.RemoveAt(LIndex);
			#endif
		}

		/// <summary>Removes the tag of the given name and returns it, if it exists. Otherwise, null is returned.</summary>		
		public Tag RemoveTag(string ATagName)
		{
			#if USEHASHTABLEFORTAGS
			try
			{
				return FTags[ATagName];
			}
			finally
			{
				FTags.Remove(ATagName);
			}
			#else
			int LIndex = IndexOfName(ATagName);
			if (LIndex >= 0)
			{
				Tag LTag = this[LIndex];
				FTags.RemoveAt(LIndex);
				return LTag;
			}
			return null;
			#endif
		}
		
		/// <summary>Removes the tag of the given name if it exists. Otherwise, an error is raised.</summary>
		public void Remove(string ATagName)
		{
			#if USEHASHTABLEFORTAGS
			if (!FTags.Contains(ATagName))
				throw new SchemaException(SchemaException.Codes.TagNotFound, ATagName);
			FTags.Remove(ATagName);
			#else
			int LIndex = IndexOfName(ATagName);
			if (LIndex >= 0)
				FTags.RemoveAt(LIndex);
			else
				throw new SchemaException(SchemaException.Codes.TagNotFound, ATagName);
			#endif
		}
		
		/// <summary>Removes the tag with the same name as the given tag if one exists. Otherwise no action is taken.</summary>
		public void SafeRemove(Tag ATag)
		{
			SafeRemove(ATag.Name);
		}
		
		/// <summary>Removes the tag with the same name as the given tag if one exists. Otherwise, an error is raised.</summary>
		public void Remove(Tag ATag)
		{
			Remove(ATag.Name);
		}
		
		/// <summary>Calls Remove for each tag in the given array.</summary>
		public void RemoveRange(Tag[] ATags)
		{
			for (int LIndex = 0; LIndex < ATags.Length; LIndex++)
				Remove(ATags[LIndex]);
		}
		
		/// <summary>Calls Remove for each tag in the given list.</summary>
		public void RemoveRange(Tags ATags)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
				Remove(LTag);
			#else
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
				Remove(ATags[LIndex]);
			#endif
		}

		/// <summary>Calls SafeRemove for each tag in the given array.</summary>		
		public void SafeRemoveRange(Tag[] ATags)
		{
			for (int LIndex = 0; LIndex < ATags.Length; LIndex++)
				SafeRemove(ATags[LIndex]);
		}
		
		/// <summary>Calls SafeRemove for each tag in the given list.</summary>
		public void SafeRemoveRange(Tags ATags)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
				SafeRemove(LTag);
			#else
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
				SafeRemove(ATags[LIndex]);
			#endif
		}
		
		/// <summary>Returns true if this list contains a tag with the same name as the given tag, false otherwise.</summary>
		public bool Contains(Tag ATag)
		{
			return Contains(ATag.Name);
		}
		
		/// <summary>Returns true if this list contains a tag with the given name, false otherwise.
		public bool Contains(string ATagName)
		{
			#if USEHASHTABLEFORTAGS
			return FTags.Contains(ATagName);
			#else
			return IndexOfName(ATagName) >= 0;
			#endif
		}
		
		/// <summary>Copies tags from this tag list to the given tag list.</summary>
		/// <remarks>
		/// If the tag in this tag list is a reference, it is copied using the Inherit() method of the tag being referenced,
		/// otheriwse it is copied using the Copy() method of the tag.
		/// </remarks>
		public void CopyTo(Tags ATags)
		{
			#if USEHASHTABLEFORTAGS
			foreach (DictionaryEntry LEntry in FTags)
			{
				TagReference LTagReference = LEntry.Value as TagReference;
				if (LTagReference != null)
					ATags.Add(LTagReference.Tag.Inherit());
				else
					ATags.Add(((Tag)LEntry.Value).Copy());
			}
			#else
			for (int LIndex = 0; LIndex < FTags.Count; LIndex++)
			{
				TagReference LTagReference = FTags[LIndex] as TagReference;
				if (LTagReference != null)
					ATags.Add(LTagReference.Tag.Inherit());
				else
					ATags.Add(((Tag)FTags[LIndex]).Copy());
			}
			#endif
		}

		/// <summary> Returns the set of static or non-static tags (not cloned). </summary>
		public Tags GetSubset(bool AStatic)
		{
			#if USEHASHTABLEFORTAGS
			Tags LTags = new Tags();
			foreach (Tag LTag in this)
				if (LTag.IsStatic = AStatic)
					LTags.Add(LTag);
			return LTags;
			#else
			Tags LTags = new Tags();
			Tag LTag;
			for (int LIndex = 0; LIndex < FTags.Count; LIndex++)
			{
				LTag = this[LIndex];
				if (LTag.IsStatic = AStatic)
					LTags.Add(LTag);
			}
			return LTags;
			#endif
		}
	}

    public sealed class TagNames : System.Object
    {
		public const string CIsSystem = @"DAE.IsSystem";
		#if !NATIVEROW
		public const string CStaticByteSize = @"DAE.StaticByteSize";
		#endif
		
		#if USETAGNAMECACHE
		private static object FDictionarySyncHandle = new object();

		private static Dictionary<string, string> FTagNameCache = new Dictionary<string, string>();
		
		public static void ClearTagNameCache()
		{
			lock (FDictionarySyncHandle)
			{
				FTagNameCache.Clear();
			}
		}

		public static string GetTagName(string AName)
		{
			lock (FDictionarySyncHandle)
			{
				string LName;
				if (FTagNameCache.TryGetValue(AName, out LName))
					return LName;
					
				FTagNameCache.Add(AName, AName);
				return AName;
			}
		}
		#endif
    }
    
	public class MetaData : System.Object
    {
		public MetaData() : base() { }
		
		public MetaData(Tag[] ATags) : base()
		{
			FTags.AddRange(ATags);
		}
		
		public MetaData(Tags ATags) : base()
		{
			FTags.AddRange(ATags);
		}
		
		public int Line = -1;
		public int LinePos = -1;
		
		// Tags
		protected Tags FTags = new Tags();
		public Tags Tags { get { return FTags; } }

		// Copy
		/// <summary>Creates a copy of the metadata, with tag references copied as inherited tags.</summary>
		public MetaData Copy()
		{
			MetaData LMetaData = new MetaData();
			FTags.CopyTo(LMetaData.Tags);
			return LMetaData;
		}
		
		// Inherit
		/// <summary>Inherits all dynamic tags, with tag references inherited.</summary>
		public MetaData Inherit()
		{
			MetaData LMetaData = new MetaData();
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in FTags)
				if (!LTag.IsStatic)
					LMetaData.Tags.Add(LTag.Inherit());
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < FTags.Count; LIndex++)
			{
				LTag = FTags[LIndex];
				if (!LTag.IsStatic)
					LMetaData.Tags.Add(LTag.Inherit());
			}
			#endif
			return LMetaData;
		}
		
		// Merge
		/// <summary>Merges all tags from the given metadata into this metadata.</summary>
		public void Merge(MetaData AMetaData)
		{
			Tags.AddOrUpdateRange(AMetaData.Tags);
		}
		
		// Inherit
		/// <summary>Inherits all dynamic tags from the given metadata into this metadata.</summary>
		public void Inherit(MetaData AMetaData)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in AMetaData.Tags)
				if (!LTag.IsStatic)
					Tags.Inherit(LTag);
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
			{
				LTag = AMetaData.Tags[LIndex];
				if (!LTag.IsStatic)
					Tags.Inherit(LTag);
			}
			#endif
		}
		
		// Join
		/// <summary>Joins each dynamic tag from the given metadata to the tags for this metadata using copy semantics.</summary>
		public void Join(MetaData AMetaData)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in AMetaData.Tags)
				if (!LTag.IsStatic)
					Tags.Join(LTag);
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
			{
				LTag = AMetaData.Tags[LIndex];
				if (!LTag.IsStatic)
					Tags.Join(LTag);
			}
			#endif
		}
		
		// JoinInherit
		/// <summary>Joins each dynamic tag from the given metadata to the tags for this metadata using reference semantics.</summary>
		public void JoinInherit(MetaData AMetaData)
		{
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in AMetaData.Tags)
				if (!LTag.IsStatic)
					Tags.JoinInherit(LTag);
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
			{
				LTag = AMetaData.Tags[LIndex];
				if (!LTag.IsStatic)
					Tags.JoinInherit(LTag);
			}
			#endif
		}
		
		/// <summary>Retrives the tag of the given name from the given metadata, if the tag exists. Otherwise, null is returned.</summary>
		public static Tag GetTag(MetaData AMetaData, string ATagName)
		{
			return AMetaData == null ? null : AMetaData.Tags.GetTag(ATagName);
		}
		
		/// <summary>Removes the tag of the given name from the given metadata and returns it, if it exists. Otherwise, null is returned.</summary>
		public static Tag RemoveTag(MetaData AMetaData, string ATagName)
		{
			return AMetaData == null ? null : AMetaData.Tags.RemoveTag(ATagName);
		}
		
		/// <summary>Retrieves the value of the given tag from the given metadata, defaulted to the value of the default tag, or the given default value, if neither tag exists.</summary>
		public static string GetTag(MetaData AMetaData, string ATagName, string ADefaultTagName, string ADefaultValue)
		{
			if (AMetaData == null)
				return ADefaultValue;
			else
				return AMetaData.Tags.GetTagValue(ATagName, ADefaultTagName, ADefaultValue);
		}
		
		/// <summary>Retrieves the value of the given tag from the given metadata, defaulted to the given default value if no tag of that name exists.</summary>
		public static string GetTag(MetaData AMetaData, string ATagName, string ADefaultValue)
		{
			if (AMetaData == null)
				return ADefaultValue;
			else
				return AMetaData.Tags.GetTagValue(ATagName, ADefaultValue);
		}
    }

    public interface IMetaData
    {
		MetaData MetaData { get; set; }
    }
    
    public interface IAlterMetaData
    {
		AlterMetaData AlterMetaData { get; set; }
    }
    
    public class AlterMetaData : System.Object
    {
		// Comment will be null if the comment is not being altered
		private string FComment;
		public string Comment
		{
			get { return FComment; }
			set { FComment = value; }
		}
		
		private Tags FCreateTags = new Tags();
		public Tags CreateTags { get { return FCreateTags; } }
		
		private Tags FAlterTags = new Tags();
		public Tags AlterTags { get { return FAlterTags; } }
		
		private Tags FDropTags = new Tags();
		public Tags DropTags { get { return FDropTags; } }
   }
}

