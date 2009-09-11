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
		public static string CopySection(string AScript, LineInfo ALineInfo)
		{
			int LPos = 0;
			for (int LIndex = 1; LIndex < ALineInfo.Line; LIndex++)
				while (AScript[LPos++] != '\n');
				
			for (int LIndex = 1; LIndex < ALineInfo.LinePos; LIndex++)
				LPos++;
				
			int LEndPos = LPos;
			for (int LIndex = ALineInfo.Line; LIndex < ALineInfo.EndLine; LIndex++)
				while (AScript[LEndPos++] != '\n');
				
			for (int LIndex = 1; LIndex < ALineInfo.EndLinePos; LIndex++)
				LEndPos++;
				
			return AScript.Substring(LPos, LEndPos - LPos);
		}
	}
}
