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
        
		private static string[] FKeywords;
        
		private static void PopulateKeywords()
		{
			FieldInfo[] LFields = typeof(Keywords).GetFields();

			int LFieldCount = 0;
			foreach (FieldInfo LField in LFields)
				if (LField.FieldType.Equals(typeof(string)) && LField.IsLiteral)
					LFieldCount++;

			FKeywords = new string[LFieldCount];

			int LFieldCounter = 0;
			foreach (FieldInfo LField in LFields)
				if (LField.FieldType.Equals(typeof(string)) && LField.IsLiteral)
				{
					FKeywords[LFieldCounter] = (string)LField.GetValue(null);
					LFieldCounter++;
				}
		}
        
		public static bool Contains(string AIdentifier)
		{
			if (FKeywords == null)
				PopulateKeywords();
				
			return ((IList)FKeywords).Contains(AIdentifier);
		}
	}
}