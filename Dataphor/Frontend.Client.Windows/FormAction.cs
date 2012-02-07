/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class ShowLinkAction : Client.ShowLinkAction
	{
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			System.Diagnostics.Process.Start(URL);
		}
	}

	public class EditFilterAction : DataAction, IEditFilterAction
	{
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			if (Source != null)
			{
				Source.Filter = EditFilterForm.ExecuteEditFilter(Source.Filter);
				Source.DataView.Open();		// Ensure the DataView is open in case a previous filter change caused it to close
			}
		}
	}
}
