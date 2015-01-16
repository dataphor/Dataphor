/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.DB2
{
	using System;
	using System.Collections;
	using System.Reflection;
	
	/// <summary>DB2 keywords</summary>    
	public class Keywords
	{
		public const string Cluster = "cluster";
        
		private static string[] _keywords;
        
		private static void PopulateKeywords()
		{
			FieldInfo[] fields = typeof(Keywords).GetFields();

			int fieldCount = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
					fieldCount++;

			_keywords = new string[fieldCount];

			int fieldCounter = 0;
			foreach (FieldInfo field in fields)
				if (field.FieldType.Equals(typeof(string)) && field.IsLiteral)
				{
					_keywords[fieldCounter] = (string)field.GetValue(null);
					fieldCounter++;
				}
		}
        
		public static bool Contains(string identifier)
		{
			if (_keywords == null)
				PopulateKeywords();
				
			return ((IList)_keywords).Contains(identifier);
		}
	}
}