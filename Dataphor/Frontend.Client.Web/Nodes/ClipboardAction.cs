using System;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class CopyAction : Action, ICopyAction
	{
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			// TODO: Implement
		}

		public string ClipboardFormatName 
		{ 
			get 
			{ 
				// TODO: Implement
				return null;
			} 
			set
			{
				// TODO: Implement
			} 
		}
	}

	public class PasteAction : Action, IPasteAction
	{
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			// TODO: Implement
		}

		public string ClipboardFormatName 
		{ 
			get
			{
				// TODO: Implement
				return null;
			}
			set
			{
				// TODO: Implement
			}
		}
	}
}
