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
		public const string CSingular = "Singular";
		public const string CPlural = "Plural";
		public const string CBrowse = "Browse";
		public const string CList = "List";
		public const string CBrowseReport = "Report";
		public const string CAdd = "Add";
		public const string CEdit = "Edit";
		public const string CDelete = "Delete";
		public const string CView = "View";
		public const string CPreview = "Preview";
		public const string CFilter = "Filter";
		public const string CManage = "Manage";
		public const string COrderBrowse = "OrderBrowse";
		public const string CVisible = "Visible";
		public const string CExposed = "Exposed";
		public const string CEmbedded = "Embedded";
		public const string CSourceName = "Source";
		public const string CMainSourceName = "Main";
		public const string CMainElaboratedTableName = "Main";

		public const string CDefaultPriority = "0";
		
		public static string GetTag(MetaData AMetaData, string ATagName, string ADefaultValue)
		{
			return MetaData.GetTag(AMetaData, String.Format("Frontend.{0}", ATagName), ADefaultValue);
		}
		
		public static bool IsSingularPageType(string APageType)
		{
			switch (APageType)
			{
				case CBrowse :
				case CList :
				case COrderBrowse :
				case CBrowseReport : return false;
				default : return true;
			}
		}
		
		public static bool IsReadOnlyPageType(string APageType)
		{
			switch (APageType)
			{
				case CView :
				case CDelete :
				case CPreview :
				case CList : 
				case COrderBrowse : return true;
				default : return false;
			}
		}
		
		public static string GetPageTypeCardinality(string APageType)
		{
			switch (APageType)
			{
				case CBrowse :
				case CList :
				case COrderBrowse :
				case CBrowseReport : return CPlural;
				default : return CSingular;
			}
		}

		/// <remarks>
		///		Returns the value of the most specific tag given by the parameters, returning ADefaultValue if no tag is found
		///		Frontend.APageType.ATagName
		///		Frontend.Cardinality(APageType).ATagName
		///		Frontend.ATagName
		///		ADefaultValue
		/// </remarks>
		public static string GetTag(MetaData AMetaData, string ATagName, string APageType, string ADefaultValue)
		{
			if (AMetaData == null)
				return ADefaultValue;
				
			if (APageType != String.Empty)
			{
				string LTagName = String.Format("Frontend.{0}.{1}", APageType, ATagName);
				Tag LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", APageType, ATagName));
				if (LTag != null)
					return LTag.Value;
					
				LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(APageType), ATagName));
				if (LTag != null)
					return LTag.Value;
					
				LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}", ATagName));
				if (LTag != null)
					return LTag.Value;
					
				return ADefaultValue;
			}

			return MetaData.GetTag(AMetaData, String.Format("Frontend.{0}", ATagName), ADefaultValue);
		}

		/// <remarks>
		///		Returns the value of the most specific explicitly page-qualified tag given by the parameters, returning ADefaultValue if no tag is found
		///		Frontend.APageType.ATagName
		///		ADefaultValue
		/// </remarks>
		public static string GetExplicitPageTag(MetaData AMetaData, string ATagName, string APageType, string ADefaultValue)
		{
			if (APageType != String.Empty)
				return MetaData.GetTag(AMetaData, String.Format("Frontend.{0}.{1}", APageType, ATagName), ADefaultValue);
			else
				return ADefaultValue;
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
		public static string GetTag(MetaData AMetaData, string ATagName, string APageType, string AReferenceType, string ADefaultValue)
		{
			if (AMetaData == null)
				return ADefaultValue;
			else
			{
				if (AReferenceType == String.Empty)
					return GetTag(AMetaData, ATagName, APageType, ADefaultValue);
				else
				{
					if (APageType != String.Empty)
					{
						Tag LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}.{2}", APageType, AReferenceType, ATagName));
						if (LTag != null)
							return LTag.Value;

						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}.{2}", GetPageTypeCardinality(APageType), AReferenceType, ATagName));
						if (LTag != null)
							return LTag.Value;
							
						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", AReferenceType, ATagName));
						if (LTag != null)
							return LTag.Value;
							
						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", APageType, ATagName));
						if (LTag != null)
							return LTag.Value;
							
						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(APageType), ATagName));
						if (LTag != null)
							return LTag.Value;
							
						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}", ATagName));
						if (LTag != null)
							return LTag.Value;
							
						return ADefaultValue;
					}
					else
					{
						Tag LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", AReferenceType, ATagName));
						if (LTag != null)
							return LTag.Value;
						
						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}", ATagName));
						if (LTag != null)
							return LTag.Value;
							
						return ADefaultValue;
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
		public static string GetExplicitPageTag(MetaData AMetaData, string ATagName, string APageType, string AReferenceType, string ADefaultValue)
		{
			if (AMetaData == null)
				return ADefaultValue;
			else
			{
				if (APageType != String.Empty)
				{
					Tag LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}.{2}", APageType, AReferenceType, ATagName));
					if (LTag == null)
						LTag = AMetaData.Tags.GetTag(String.Format("Frontend.{0}.{1}", APageType, ATagName));
						
					if (LTag != null)
						return LTag.Value;
					else
						return ADefaultValue;
				}
				else
					return ADefaultValue;
			}
		}

		/// <remarks>
		///		Returns the set of tags specified by the given qualifier in a new tags list, with the qualifier removed.
		///		For example, the tag Frontend.Grid.ElementType in the input tag list will show up in the output tag list as Frontend.ElementType.
		/// </remarks>
		public static MetaData ExtractTags(Tags ATags, string AQualifier, string APageType)
		{
			MetaData LMetaData = new MetaData();
			string LPageQualifier = String.Format("Frontend.{0}.{1}", APageType, AQualifier);
			string LCardinalityQualifier = String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(APageType), AQualifier);
			string LQualifier = String.Format("Frontend.{0}", AQualifier);
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
			{
				LTag = ATags[LIndex];
			#endif
				if (LTag.Name.IndexOf(LPageQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", APageType, LTag.Name.Substring(LPageQualifier.Length)), LTag.Value);
				else if (LTag.Name.IndexOf(LCardinalityQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", GetPageTypeCardinality(APageType), LTag.Name.Substring(LCardinalityQualifier.Length)), LTag.Value);
				else if (LTag.Name.IndexOf(LQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend{0}", LTag.Name.Substring(LQualifier.Length)), LTag.Value);
			}
			return LMetaData;
		}
		
		public static MetaData ExtractTags(Tags ATags, string AQualifier, string APageType, string AReferenceType)
		{
			MetaData LMetaData = new MetaData();
			string LPageTypeReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}", APageType, AReferenceType, AQualifier);
			string LCardinalityReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}", GetPageTypeCardinality(APageType), AReferenceType, AQualifier);
			string LReferenceTypeQualifier = String.Format("Frontend.{0}.{1}", AReferenceType, AQualifier);
			string LPageTypeQualifier = String.Format("Frontend.{0}.{1}", APageType, AQualifier);
			string LCardinalityQualifier = String.Format("Frontend.{0}.{1}", GetPageTypeCardinality(APageType), AQualifier);
			string LQualifier = String.Format("Frontend.{0}", AQualifier);
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
			{
				LTag = ATags[LIndex];
			#endif
				if (LTag.Name.IndexOf(LPageTypeReferenceTypeQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}.{1}{2}", APageType, AReferenceType, LTag.Name.Substring(LPageTypeReferenceTypeQualifier.Length)), LTag.Value); 
				else if (LTag.Name.IndexOf(LCardinalityReferenceTypeQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}.{1}{2}", GetPageTypeCardinality(APageType), AReferenceType, LTag.Name.Substring(LCardinalityReferenceTypeQualifier.Length)), LTag.Value);
				else if (LTag.Name.IndexOf(LReferenceTypeQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", AReferenceType, LTag.Name.Substring(LReferenceTypeQualifier.Length)), LTag.Value);
				else if (LTag.Name.IndexOf(LPageTypeQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", APageType, LTag.Name.Substring(LPageTypeQualifier.Length)), LTag.Value);
				else if (LTag.Name.IndexOf(LCardinalityQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend.{0}{1}", GetPageTypeCardinality(APageType), LTag.Name.Substring(LCardinalityQualifier.Length)), LTag.Value);
				else if (LTag.Name.IndexOf(LQualifier) == 0)
					LMetaData.Tags.AddOrUpdate(String.Format("Frontend{0}", LTag.Name.Substring(LQualifier.Length)), LTag.Value);
			}
			return LMetaData;
		}
		
		public static void AddProperty(Tag ATag, string AQualifier, Tags AProperties)
		{
			string LTagName = ATag.Name.Substring(AQualifier.Length);
			if (LTagName.Length > 0)
				AProperties.AddOrUpdate(LTagName, ATag.Value);
		}
		
		public static void ExtractProperties(MetaData AMetaData, string AQualifier, string APageType, Tags AProperties)
		{
			if (AMetaData != null)
			{
				string LPageQualifier = String.Format("Frontend.{0}.{1}.", APageType, AQualifier);
				string LCardinalityQualifier = String.Format("Frontend.{0}.{1}.", DerivationUtility.GetPageTypeCardinality(APageType), AQualifier);
				string LElementQualifier = String.Format("Frontend.{0}.", AQualifier);

				#if USEHASHTABLEFORTAGS
				foreach (Tag LTag in AMetaData.Tags)
				{
				#else
				Tag LTag;
				for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
				{
					LTag = AMetaData.Tags[LIndex];
				#endif
					if (LTag.Name.IndexOf(LPageQualifier) == 0) AddProperty(LTag, LPageQualifier, AProperties);
					else if (LTag.Name.IndexOf(LCardinalityQualifier) == 0) AddProperty(LTag, LCardinalityQualifier, AProperties);
					else if (LTag.Name.IndexOf(LElementQualifier) == 0) AddProperty(LTag, LElementQualifier, AProperties);
				}
			}
		}
		
		public static void ExtractProperties(MetaData AMetaData, string AQualifier, string APageType, string AReferenceType, Tags AProperties)
		{
			if (AMetaData != null)
			{
				string LPageTypeReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}.", APageType, AReferenceType, AQualifier);
				string LCardinalityReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.{2}.", GetPageTypeCardinality(APageType), AReferenceType, AQualifier);
				string LReferenceTypeQualifier = String.Format("Frontend.{0}.{1}.", AReferenceType, AQualifier);
				string LPageTypeQualifier = String.Format("Frontend.{0}.{1}.", APageType, AQualifier);
				string LCardinalityQualifier = String.Format("Frontend.{0}.{1}.", GetPageTypeCardinality(APageType), AQualifier);
				string LElementQualifier = String.Format("Frontend.{0}.", AQualifier);

				#if USEHASHTABLEFORTAGS
				foreach (Tag LTag in AMetaData.Tags)
				{
				#else
				Tag LTag;
				for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
				{
					LTag = AMetaData.Tags[LIndex];
				#endif
					if (LTag.Name.IndexOf(LPageTypeReferenceTypeQualifier) == 0) AddProperty(LTag, LPageTypeReferenceTypeQualifier, AProperties);
					else if (LTag.Name.IndexOf(LCardinalityReferenceTypeQualifier) == 0) AddProperty(LTag, LCardinalityReferenceTypeQualifier, AProperties);
					else if (LTag.Name.IndexOf(LReferenceTypeQualifier) == 0) AddProperty(LTag, LReferenceTypeQualifier, AProperties);
					else if (LTag.Name.IndexOf(LPageTypeQualifier) == 0) AddProperty(LTag, LPageTypeQualifier, AProperties);
					else if (LTag.Name.IndexOf(LCardinalityQualifier) == 0) AddProperty(LTag, LCardinalityQualifier, AProperties);
					else if (LTag.Name.IndexOf(LElementQualifier) == 0) AddProperty(LTag, LElementQualifier, AProperties);
				}
			}
		}
		
		public static bool IsColumnVisible(Schema.TableVarColumn AColumn, string APageType)
		{
			return Convert.ToBoolean(GetTag(AColumn.MetaData, "Visible", APageType, "True"));
		}

		public static bool IsOrderVisible(Schema.Order AOrder, string APageType)
		{
			bool LIsVisible = Convert.ToBoolean(GetTag(AOrder.MetaData, "Visible", APageType, "True"));
			bool LHasVisibleColumns = false;
			if (LIsVisible)
			{
				bool LIsColumnVisible;
				bool LHasInvisibleColumns = false;
				foreach (Schema.OrderColumn LColumn in AOrder.Columns)
				{
					LIsColumnVisible = IsColumnVisible(LColumn.Column, APageType);
					if (LIsColumnVisible)
						LHasVisibleColumns = true;
					if (LHasInvisibleColumns && LIsColumnVisible)
					{
						LIsVisible = false;
						break;
					}
					
					if (!LIsColumnVisible)
						LHasInvisibleColumns = true;
				}
			}
			return LHasVisibleColumns && LIsVisible;
		}
    }
    
    public class DerivationSeed : System.Object
    {
		public DerivationSeed(string APageType, string AQuery, bool AElaborate, string AMasterKeyNames, string ADetailKeyNames)
		{
			FPageType = APageType == null ? String.Empty : APageType;
			FQuery = AQuery == null ? String.Empty : AQuery.Trim(); // Remove any excess whitespace before and after the query
			FElaborate = AElaborate;
			FMasterKeyNames = AMasterKeyNames == null ? String.Empty : AMasterKeyNames;
			FDetailKeyNames = ADetailKeyNames == null ? String.Empty : ADetailKeyNames;
		}
		
		private string FPageType;
		public string PageType { get { return FPageType; } }
		
		private string FQuery;
		public string Query { get { return FQuery; } }
		
		private bool FElaborate = true;
		public bool Elaborate { get { return FElaborate; } }
		
		private string FMasterKeyNames;
		public string MasterKeyNames { get { return FMasterKeyNames; } }
		
		private string FDetailKeyNames;
		public string DetailKeyNames { get { return FDetailKeyNames; } }
		
		public override int GetHashCode()
		{
			return FPageType.ToLower().GetHashCode() ^ FQuery.ToLower().GetHashCode() ^ FMasterKeyNames.ToLower().GetHashCode() ^ FDetailKeyNames.ToLower().GetHashCode();
		}
		
		public override bool Equals(object AObject)
		{
			if (AObject is DerivationSeed)
			{
				DerivationSeed LKey = (DerivationSeed)AObject;
				return
					(String.Compare(FPageType, LKey.PageType, true) == 0) &&
					(String.Compare(FQuery, LKey.Query, false) == 0) &&
					(FElaborate == LKey.Elaborate) && 
					(String.Compare(FMasterKeyNames, LKey.MasterKeyNames, false) == 0) &&
					(String.Compare(FDetailKeyNames, LKey.DetailKeyNames, false) == 0);
			}
			return false;
		}
    }
    
    public class DerivationCacheItem : System.Object
    {
		public DerivationCacheItem() : base(){}
		public DerivationCacheItem(XmlDocument ADocument) : base()
		{
			FDocument = ADocument;
		}
		
		private XmlDocument FDocument;
		public XmlDocument Document 
		{
			get { return FDocument; } 
			set { FDocument = value; } 
		}
    }
    

}