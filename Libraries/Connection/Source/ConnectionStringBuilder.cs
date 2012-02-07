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
		public virtual Tags Map (Tags tags)
		{
			Tags localTags = new Tags();
			
			#if USEHASHTABLEFORTAGS
			foreach (Tag tag in ATags)
			{
			#else
			Tag tag;
			for (int index = 0; index < tags.Count; index++)
			{
				tag = tags[index];
			#endif
				Tag legendTag = _legend.GetTag(tag.Name);
				if (legendTag != Tag.None)
					localTags.AddOrUpdate(legendTag.Value, tag.Value);
				else
					localTags.AddOrUpdate(tag.Name, tag.Value);
			}
			
			localTags.AddOrUpdateRange(_parameters);

			return localTags;
		}

		protected Tags _legend = new Tags();
		/// <summary>Used to map connection parameters based on name.</summary>
		public Tags Legend { get { return _legend; } }
		
		protected Tags _parameters = new Tags();
		/// <summary>Specifies additional connection parameters to be added after the mapping operation takes place.</summary>
		public Tags Parameters { get { return _parameters; } }
	}
}
