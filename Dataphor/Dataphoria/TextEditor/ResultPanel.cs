/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Windows.Forms;
using System.Text;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Frontend.Client.Windows;

using SD = ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	public class ResultPanel : TextEdit
	{
		public ResultPanel()
		{
			Document.ReadOnly = true;
			Document.HighlightingStrategy = SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("Result");
			IsIconBarVisible = false;
		}

		// override to return true if the passed key is control-tab
		// this is done so that the result control doesn't eat the control-tab
		// it is used by the tab control in the servermanager
		protected override bool IsInputKey(Keys inputKey)
		{
			if (inputKey == (Keys.Control | Keys.Tab))
				return true;
			return base.IsInputKey(inputKey);
		}
	}

}
