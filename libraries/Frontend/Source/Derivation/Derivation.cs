/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

using Alphora.Dataphor;	
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.Frontend.Server;
using Alphora.Dataphor.Frontend.Server.Elaboration;
using Alphora.Dataphor.Frontend.Server.Structuring;

namespace Alphora.Dataphor.Frontend.Server.Derivation
{

    public sealed class DerivationUtility
    {
		// Do not localize
		public const string Singular = "Singular";
		public const string Plural = "Plural";
		public const string Browse = "Browse";
		public const string List = "List";
		public const string BrowseReport = "Report";
		public const string Add = "Add";
		public const string Edit = "Edit";
		public const string Delete = "Delete";
		public const string View = "View";
		public const string Preview = "Preview";
		public const string Filter = "Filter";
		public const string Manage = "Manage";
		public const string OrderBrowse = "OrderBrowse";
		public const string Visible = "Visible";
		public const string Exposed = "Exposed";
		public const string Embedded = "Embedded";
		public const string SourceName = "Source";
		public const string MainSourceName = "Main";
		public const string MainElaboratedTableName = "Main";

		public const string DefaultPriority = "0";
		
		public static string GetTag(MetaData metaData, string tagName, string defaultValue)
		{
			return MetaData.GetTag(metaData, String.Format("Frontend.{0}", tagName), defaultValue);
		}
		
		public static bool IsSingularPageType(string pageType)
		{
			switch (pageType)
			{
				case Browse :
				case List :
				case OrderBrowse :
				case BrowseReport : return false;
				default : return true;
			}
		}
		
		public static bool IsReadOnlyPageType(string pageType)
		{
			switch (pageType)
			{
				case View :
				case Delete :
				case Preview :
				case List : 
				case OrderBrowse : return true;
				default : return false;
			}
		}
		
		public static string GetPageTypeCardinality(string pageType)
		{
			switch (pageType)
			{
				case Browse :
				case List :
				case OrderBrowse :
				case BrowseReport : return Plural;
				default : return Singular;
			}
		}

		/// <remarks>
		///		Returns the value of the most specific tag given by the parameters, returning ADefaultValue if no tag is found
		///		Frontend.APageType.ATagName
		///		Frontend.Cardinality(APageType).ATagName
		///		Frontend.ATagName
		///		ADefaultValue
		/// </remarks>
		public static string GetTag(MetaData metaData, string tagName, string pageType, string defaultValue)
		{
			if (metaData == null)
				return defaultValue;
				
			if (pageType != String.Empty)
			{
				string locatagName = String.Format("Frontend.{0}.{1}", pageType, tagName);
				Tag tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", pageType, tagName));
				if (tag != Tag.None)
					return tag.Value;
					
				tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(pageType), tagName));
				if (tag != Tag.None)
					return tag.Value;
					
				tag = metaData.Tags.GetTag(String.Format("Frontend.{0}", tagName));
				if (tag != Tag.None)
					return tag.Value;
					
				return defaultValue;
			}

			return MetaData.GetTag(metaData, String.Format("Frontend.{0}", tagName), defaultValue);
		}

		/// <remarks>
		///		Returns the value of the most specific explicitly page-qualified tag given by the parameters, returning ADefaultValue if no tag is found
		///		Frontend.APageType.ATagName
		///		ADefaultValue
		/// </remarks>
		public static string GetExplicitPageTag(MetaData metaData, string tagName, string pageType, string defaultValue)
		{
			if (pageType != String.Empty)
				return MetaData.GetTag(metaData, String.Format("Frontend.{0}.{1}", pageType, tagName), defaultValue);
			else
				return defaultValue;
		}

		/// <remarks>
		///		Returns the value of the most specific tag given by the parameters, returning ADefaultValue if no tag is found
		///		Frontend.APageType.AReferenceType.ATagName
		///		Frontend.Cardinality(APageType).AReferenceType.ATagName
		///		Frontend.APageType.ATagName
		///		Frontend.Cardinality(APageType).ATagName
		///		Frontend.AReferenceType.ATagName
		///		Frontend.ATagName
		///		ADefaultValue
		/// </remarks>
		public static string GetTag(MetaData metaData, string tagName, string pageType, string referenceType, string defaultValue)
		{
			if (metaData == null)
				return defaultValue;
			else
			{
				if (referenceType == String.Empty)
					return GetTag(metaData, tagName, pageType, defaultValue);
				else
				{
					if (pageType != String.Empty)
					{
						Tag tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}.{2}", pageType, referenceType, tagName));
						if (tag != Tag.None)
							return tag.Value;

						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}.{2}", GetPageTypeCardinality(pageType), referenceType, tagName));
						if (tag != Tag.None)
							return tag.Value;
							
						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", referenceType, tagName));
						if (tag != Tag.None)
							return tag.Value;
							
						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", pageType, tagName));
						if (tag != Tag.None)
							return tag.Value;
							
						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(pageType), tagName));
						if (tag != Tag.None)
							return tag.Value;
							
						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}", tagName));
						if (tag != Tag.None)
							return tag.Value;
							
						return defaultValue;
					}
					else
					{
						Tag tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", referenceType, tagName));
						if (tag != Tag.None)
							return tag.Value;
						
						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}", tagName));
						if (tag != Tag.None)
							return tag.Value;
							
						return defaultValue;
					}
				}
			}
		}
		
		/// <remarks>
		///		Returns the value of the most specific explicitly pagetype qualified tag given by the parameters, 
		///		returning ADefaultValue if no tag is found
		///		Frontend.APageType.AReferenceType.ATagName
		///		Frontend.APageType.ATagName
		///		ADefaultValue
		/// </remarks>
		public static string GetExplicitPageTag(MetaData metaData, string tagName, string pageType, string referenceType, string defaultValue)
		{
			if (metaData == null)
				return defaultValue;
			else
			{
				if (pageType != String.Empty)
				{
					Tag tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}.{2}", pageType, referenceType, tagName));
					if (tag == Tag.None)
						tag = metaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", pageType, tagName));
						
					if (tag != Tag.None)
						return tag.Value;
					else
						return defaultValue;
				}
				else
					return defaultValue;
			}
		}

		/// <remarks>
		///		Returns the set of tags specified by the given qualifier in a new tags list, with the qualifier removed.
		///		For example, the tag Frontend.Grid.ElementType in the input tag list will show up in the output tag list as Frontend.ElementType.
		/// </remarks>
		public static MetaData ExtractTags(Tags tags, string qualifier, string pageType)
		{
			MetaData metaData = new MetaData();
			string pageQualifier = String.Format("Frontend.{0}.{1}", pageType, qualifier);
			string cardinalityQualifier = String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(pageType), qualifier);
			string localQualifier = String.Format("Frontend.{0}", qualifier);
			#if USEHASHTABLEFORTAGS
			foreach (Tag tag in ATags)
			{
			#else
			Tag tag;
			for (int index = 0; index < tags.Count; index++)
			{
				tag = tags[index];
			#endif
				if (tag.Name.IndexOf(pageQualifier) == 0)
					metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", pageType, tag.Name.Substring(pageQualifier.Length)), tag.Value);
				else if (tag.Name.IndexOf(cardinalityQualifier) == 0)
					metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", GetPageTypeCardinality(pageType), tag.Name.Substring(cardinalityQualifier.Length)), tag.Value);
				else if (tag.Name.IndexOf(localQualifier) == 0)
					metaData.Tags.AddOrUpdate(String.Format("Frontend{0}", tag.Name.Substring(localQualifier.Length)), tag.Value);
			}
			return metaData;
		}
		
		public static MetaData ExtractTags(MetaData givenMetaData, string qualifier, string pageType, string referenceType)
		{
			MetaData metaData = new MetaData();
            if (givenMetaData != null && givenMetaData.HasTags())
            {
			    string pageTypeReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}", pageType, referenceType, qualifier);
			    string cardinalityReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}", GetPageTypeCardinality(pageType), referenceType, qualifier);
			    string referenceTypeQualifier = String.Format("Frontend.{0}.{1}", referenceType, qualifier);
			    string pageTypeQualifier = String.Format("Frontend.{0}.{1}", pageType, qualifier);
			    string cardinalityQualifier = String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(pageType), qualifier);
			    string localQualifier = String.Format("Frontend.{0}", qualifier);
			    #if USEHASHTABLEFORTAGS
			    foreach (Tag tag in tags)
			    {
			    #else
			    Tag tag;
			    for (int index = 0; index < givenMetaData.Tags.Count; index++)
			    {
				    tag = givenMetaData.Tags[index];
			    #endif
				    if (tag.Name.IndexOf(pageTypeReferenceTypeQualifier) == 0)
					    metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}.{1}{2}", pageType, referenceType, tag.Name.Substring(pageTypeReferenceTypeQualifier.Length)), tag.Value); 
				    else if (tag.Name.IndexOf(cardinalityReferenceTypeQualifier) == 0)
					    metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}.{1}{2}", GetPageTypeCardinality(pageType), referenceType, tag.Name.Substring(cardinalityReferenceTypeQualifier.Length)), tag.Value);
				    else if (tag.Name.IndexOf(referenceTypeQualifier) == 0)
					    metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", referenceType, tag.Name.Substring(referenceTypeQualifier.Length)), tag.Value);
				    else if (tag.Name.IndexOf(pageTypeQualifier) == 0)
					    metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", pageType, tag.Name.Substring(pageTypeQualifier.Length)), tag.Value);
				    else if (tag.Name.IndexOf(cardinalityQualifier) == 0)
					    metaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", GetPageTypeCardinality(pageType), tag.Name.Substring(cardinalityQualifier.Length)), tag.Value);
				    else if (tag.Name.IndexOf(localQualifier) == 0)
					    metaData.Tags.AddOrUpdate(String.Format("Frontend{0}", tag.Name.Substring(localQualifier.Length)), tag.Value);
			    }
            }
			return metaData;
		}
		
		public static void AddProperty(Tag tag, string qualifier, Tags properties)
		{
			string tagName = tag.Name.Substring(qualifier.Length);
			if (tagName.Length > 0)
				properties.AddOrUpdate(tagName, tag.Value);
		}
		
		public static void ExtractProperties(MetaData metaData, string qualifier, string pageType, Tags properties)
		{
			if (metaData != null)
			{
				string pageQualifier = String.Format("Frontend.{0}.{1}.", pageType, qualifier);
				string cardinalityQualifier = String.Format("Frontend.{0}.{1}.", DerivationUtility.GetPageTypeCardinality(pageType), qualifier);
				string elementQualifier = String.Format("Frontend.{0}.", qualifier);

				#if USEHASHTABLEFORTAGS
				foreach (Tag tag in AMetaData.Tags)
				{
				#else
				Tag tag;
				for (int index = 0; index < metaData.Tags.Count; index++)
				{
					tag = metaData.Tags[index];
				#endif
					if (tag.Name.IndexOf(pageQualifier) == 0) AddProperty(tag, pageQualifier, properties);
					else if (tag.Name.IndexOf(cardinalityQualifier) == 0) AddProperty(tag, cardinalityQualifier, properties);
					else if (tag.Name.IndexOf(elementQualifier) == 0) AddProperty(tag, elementQualifier, properties);
				}
			}
		}
		
		public static void ExtractProperties(MetaData metaData, string qualifier, string pageType, string referenceType, Tags properties)
		{
			if (metaData != null)
			{
				string pageTypeReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}.", pageType, referenceType, qualifier);
				string cardinalityReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}.", GetPageTypeCardinality(pageType), referenceType, qualifier);
				string referenceTypeQualifier = String.Format("Frontend.{0}.{1}.", referenceType, qualifier);
				string pageTypeQualifier = String.Format("Frontend.{0}.{1}.", pageType, qualifier);
				string cardinalityQualifier = String.Format("Frontend.{0}.{1}.", GetPageTypeCardinality(pageType), qualifier);
				string elementQualifier = String.Format("Frontend.{0}.", qualifier);

				#if USEHASHTABLEFORTAGS
				foreach (Tag tag in AMetaData.Tags)
				{
				#else
				Tag tag;
				for (int index = 0; index < metaData.Tags.Count; index++)
				{
					tag = metaData.Tags[index];
				#endif
					if (tag.Name.IndexOf(pageTypeReferenceTypeQualifier) == 0) AddProperty(tag, pageTypeReferenceTypeQualifier, properties);
					else if (tag.Name.IndexOf(cardinalityReferenceTypeQualifier) == 0) AddProperty(tag, cardinalityReferenceTypeQualifier, properties);
					else if (tag.Name.IndexOf(referenceTypeQualifier) == 0) AddProperty(tag, referenceTypeQualifier, properties);
					else if (tag.Name.IndexOf(pageTypeQualifier) == 0) AddProperty(tag, pageTypeQualifier, properties);
					else if (tag.Name.IndexOf(cardinalityQualifier) == 0) AddProperty(tag, cardinalityQualifier, properties);
					else if (tag.Name.IndexOf(elementQualifier) == 0) AddProperty(tag, elementQualifier, properties);
				}
			}
		}
		
		public static bool IsColumnVisible(Schema.TableVarColumn column, string pageType)
		{
			return Convert.ToBoolean(GetTag(column.MetaData, "Visible", pageType, "True"));
		}

		public static bool IsOrderVisible(Schema.Order order, string pageType)
		{
			bool isVisible = Convert.ToBoolean(GetTag(order.MetaData, "Visible", pageType, "True"));
			bool hasVisibleColumns = false;
			if (isVisible)
			{
				bool isColumnVisible;
				bool hasInvisibleColumns = false;
				foreach (Schema.OrderColumn column in order.Columns)
				{
					isColumnVisible = IsColumnVisible(column.Column, pageType);
					if (isColumnVisible)
						hasVisibleColumns = true;
					if (hasInvisibleColumns && isColumnVisible)
					{
						isVisible = false;
						break;
					}
					
					if (!isColumnVisible)
						hasInvisibleColumns = true;
				}
			}
			return hasVisibleColumns && isVisible;
		}
    }
    
    public class DerivationSeed : System.Object
    {
		public DerivationSeed(string pageType, string query, bool elaborate, string masterKeyNames, string detailKeyNames)
		{
			_pageType = pageType == null ? String.Empty : pageType;
			_query = query == null ? String.Empty : query.Trim(); // Remove any excess whitespace before and after the query
			_elaborate = elaborate;
			_masterKeyNames = masterKeyNames == null ? String.Empty : masterKeyNames;
			_detailKeyNames = detailKeyNames == null ? String.Empty : detailKeyNames;
		}
		
		private string _pageType;
		public string PageType { get { return _pageType; } }
		
		private string _query;
		public string Query { get { return _query; } }
		
		private bool _elaborate = true;
		public bool Elaborate { get { return _elaborate; } }
		
		private string _masterKeyNames;
		public string MasterKeyNames { get { return _masterKeyNames; } }
		
		private string _detailKeyNames;
		public string DetailKeyNames { get { return _detailKeyNames; } }
		
		public override int GetHashCode()
		{
			return _pageType.ToLower().GetHashCode() ^ _query.ToLower().GetHashCode() ^ _masterKeyNames.ToLower().GetHashCode() ^ _detailKeyNames.ToLower().GetHashCode();
		}
		
		public override bool Equals(object objectValue)
		{
			if (objectValue is DerivationSeed)
			{
				DerivationSeed key = (DerivationSeed)objectValue;
				return
					(String.Compare(_pageType, key.PageType, true) == 0) &&
					(String.Compare(_query, key.Query, false) == 0) &&
					(_elaborate == key.Elaborate) && 
					(String.Compare(_masterKeyNames, key.MasterKeyNames, false) == 0) &&
					(String.Compare(_detailKeyNames, key.DetailKeyNames, false) == 0);
			}
			return false;
		}
    }
    
    public class DerivationCacheItem : System.Object
    {
		public DerivationCacheItem() : base(){}
		public DerivationCacheItem(XmlDocument document) : base()
		{
			_document = document;
		}
		
		private XmlDocument _document;
		public XmlDocument Document 
		{
			get { return _document; } 
			set { _document = value; } 
		}
    }
    

}