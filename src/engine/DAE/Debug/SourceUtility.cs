/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Debug
{
	public static class SourceUtility
	{
		/// <summary>
		/// Copies a delimited section out of a given script.
		/// </summary>
		public static string CopySection(string script, LineInfo lineInfo)
		{
			int pos = 0;
			for (int index = 1; index < lineInfo.Line; index++)
				while (script[pos++] != '\n');
				
			for (int index = 1; index < lineInfo.LinePos; index++)
				pos++;
				
			int endPos = pos;
			for (int index = lineInfo.Line; index < lineInfo.EndLine; index++)
				while (script[endPos++] != '\n');
				
			for (int index = 1; index < lineInfo.EndLinePos; index++)
				endPos++;
				
			return script.Substring(pos, endPos - pos);
		}
	}
}
