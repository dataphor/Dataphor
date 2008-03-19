/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Connection
{
	/// <summary>
	/// ConnectionStringBuilder is unique to ConnectionClass and Device.  It creates a connection
	/// string based on what the D4 Device takes, and what the ConnectionClass needs.
	/// </summary>
	public abstract class ConnectionStringBuilder
	{
		public virtual Tags Map (Tags ATags)
		{
			Tags LTags = new Tags();
			
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
			{
				LTag = ATags[LIndex];
			#endif
				Tag LLegendTag = FLegend.GetTag(LTag.Name);
				if (LLegendTag != null)
					LTags.AddOrUpdate(LLegendTag.Value, LTag.Value);
				else
					LTags.AddOrUpdate(LTag.Name, LTag.Value);
			}
			
			LTags.AddOrUpdateRange(FParameters);

			return LTags;
		}

		protected Tags FLegend = new Tags();
		/// <summary>Used to map connection parameters based on name.</summary>
		public Tags Legend { get { return FLegend; } }
		
		protected Tags FParameters = new Tags();
		/// <summary>Specifies additional connection parameters to be added after the mapping operation takes place.</summary>
		public Tags Parameters { get { return FParameters; } }
	}
}
