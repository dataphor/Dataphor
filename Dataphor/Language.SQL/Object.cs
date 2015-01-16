using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Schema
{
    public class Object
    {
        public static bool NamesEqual(string leftName, string rightName)
        {
			if (((leftName.Length > 0) && (leftName[0].Equals('.'))) || ((rightName.Length > 0) && (rightName[0].Equals('.'))))
				return 
					String.Compare
					(
						leftName.Substring(leftName.IndexOf(Keywords.Qualifier) == 0 ? 1 : 0), 
						rightName.Substring(rightName.IndexOf(Keywords.Qualifier) == 0 ? 1 : 0)
					) == 0;
			else
			{
				int leftIndex = leftName.Length - 1;
				int rightIndex = rightName.Length - 1;
				
				if (leftIndex >= rightIndex)
				{
					while (true)
					{
						if (rightIndex < 0)
						{
							if ((leftIndex < 0) || leftName[leftIndex].Equals('.'))
								return true;
							else
								return false;
						}
						
						if (!leftName[leftIndex].Equals(rightName[rightIndex]))
							return false;
						
						rightIndex--;
						leftIndex--;
					}
				}

				return false;
			}
        }

		public static int GetQualifierCount(string name)
		{
			int result = 0;
			for (int index = 0; index < name.Length; index++)
				if (name[index] == '.')
					result++;
			return result;
		}
		
        public static string Qualify(string name, string nameSpace)
        {
			if (name.IndexOf(Keywords.Qualifier) == 0)
				return name.Substring(1);
			else if (nameSpace != String.Empty)
				return String.Format("{0}{1}{2}", nameSpace, Keywords.Qualifier, name);
			else
				return name;
        }
        
        public static bool IsRooted(string name)
        {
			return name.IndexOf(Keywords.Qualifier) == 0;
        }
        
        public static string EnsureRooted(string name)
        {
			return String.Format("{0}{1}", name.IndexOf(Keywords.Qualifier) == 0 ? String.Empty : Keywords.Qualifier, name);
        }
        
        public static string EnsureUnrooted(string name)
        {
			return name.IndexOf(Keywords.Qualifier) == 0 ? name.Substring(1) : name;
        }
        
        /// <summary>Replaces all qualifiers in the given name with an underscore.</summary>
        public static string MangleQualifiers(string name)
        {
			return name.Replace(Keywords.Qualifier, "_");
		}

		/// <summary>Removes one level of qualification from the given identifier.</summary>
		public static string Dequalify(string name)
		{
			name = EnsureUnrooted(name);
			int index = name.IndexOf(Keywords.Qualifier);
			if ((index > 0) && (index < name.Length - 1))
				return name.Substring(index + 1);
			else
				return name;
		}
		
		/// <summary>Returns the unqualified identifier.</summary>
		public static string Unqualify(string name)
		{
			int index = name.LastIndexOf(Keywords.Qualifier);
			if ((index > 0) && (index < name.Length - 1))
				return name.Substring(index + 1);
			else
				return name;
		}
		
		/// <summary>Returns the qualifier of the given name.  If the name does not contain a qualifier, the empty string is returned.</summary>
		public static string Qualifier(string name)
		{
			name = EnsureUnrooted(name);
			int index = name.IndexOf(Keywords.Qualifier);
			if (index >= 0)
				return name.Substring(0, index);
			else
				return String.Empty;
		}
		
		/// <summary> Returns true if the given identifier isn't qualified (not including the root).</summary>
		public static bool IsQualified(string name)
		{
			name = EnsureUnrooted(name);
			return name.IndexOf(Keywords.Qualifier) >= 0;
		}
		
		/// <summary>Returns the given name with the given qualifier removed.  If the name does not begin with the given qualifier, the given name is returned unchanged.  If the name is equal to the given qualifier, the empty string is returned.</summary>
		public static string RemoveQualifier(string name, string qualifier)
		{
			int index = name.IndexOf(qualifier);
			if (index == 0)
				if (name.Length == qualifier.Length)
					return String.Empty;
				else
					return name.Substring(qualifier.Length + 1);
			else
				return name;
		}
    }
}
