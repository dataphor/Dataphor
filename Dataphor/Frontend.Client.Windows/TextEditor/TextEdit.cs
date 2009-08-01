/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.TextEditor;
using SD = ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class TextEdit : SD.TextEditorControl
	{
		private static FindSettings FFindSettings = new FindSettings();
		private static FindAction FFindAction = FindAction.Find;

		static TextEdit()
		{
			// This regex is to replace simple line feeds with CR/LF pairs.  For a long time the Dataphoria editor generated just LFs
			FRepairCRLF = new System.Text.RegularExpressions.Regex(@"(?<!\r)\n", System.Text.RegularExpressions.RegexOptions.Compiled);
		}

		public TextEdit()
		{
			ShowInvalidLines = false;
			TabIndent = 3;
			ShowSpaces = false;
			ShowTabs = false;
			ShowLineNumbers = false;
			ShowEOLMarkers = false;
			ShowVRuler = true;
			EnableFolding = false;
			VRulerRow = 100;
			IsIconBarVisible = true;
			IndentStyle = SD.Document.IndentStyle.Auto;
			LineViewerStyle = SD.Document.LineViewerStyle.None;
			Document.TextEditorProperties.LineTerminator = "\r\n";
			editactions[Keys.L | Keys.Control] = new SD.Actions.DeleteLine();
			editactions[Keys.BrowserBack] = new SD.Actions.GotoPrevBookmark(new Predicate<ICSharpCode.TextEditor.Document.Bookmark>(delegate { return true; }));
			editactions[Keys.BrowserForward] = new SD.Actions.GotoNextBookmark(new Predicate<ICSharpCode.TextEditor.Document.Bookmark>(delegate { return true; }));
			editactions[Keys.BrowserStop] = new SD.Actions.ToggleBookmark();
			editactions[Keys.Left | Keys.Alt] = new SD.Actions.ToggleBookmark();
			editactions[Keys.Up | Keys.Alt] = new SD.Actions.GotoPrevBookmark(new Predicate<ICSharpCode.TextEditor.Document.Bookmark>(delegate { return true; }));
			editactions[Keys.Down | Keys.Alt] = new SD.Actions.GotoNextBookmark(new Predicate<ICSharpCode.TextEditor.Document.Bookmark>(delegate { return true; }));
			editactions[Keys.Home] = new AllTheWayHome();
			editactions[Keys.Home | Keys.Alt] = new SD.Actions.Home();
			editactions.Remove(Keys.D | Keys.Control);
			editactions.Remove(Keys.D | Keys.Shift | Keys.Control);
		    editactions[Keys.Delete | Keys.Shift | Keys.Control] = new SD.Actions.DeleteToLineEnd();
		}

		private static System.Text.RegularExpressions.Regex FRepairCRLF;

		public string GetLineNumberText()
		{
			TextLocation LPosition = ActiveTextAreaControl.Caret.Position;
			return String.Format
			(
				"Ln {0} Col {1}", 
				LPosition.Y + 1,
				LPosition.X + 1
			);
		}

		public void AppendText(string ANewText)
		{
			BeginUpdate();

			TextLocation LOldEndPoint = Document.OffsetToPosition(Document.TextLength);
			
			bool LOldReadOnly = Document.ReadOnly;
			Document.ReadOnly = false;

			if ((Document.TextLength > 0) && (ANewText.Length > 0))
				Document.Insert(Document.TextLength, "\r\n");
			Document.Insert(Document.TextLength, ANewText);

			Document.ReadOnly = LOldReadOnly;

			ActiveTextAreaControl.Caret.Position = LOldEndPoint;
			ActiveTextAreaControl.ScrollToCaret();

			EndUpdate();

			Refresh();  // if this isn't done then half the loaded text won't show up.
		}
		
		public void SetText(string ANewText)
		{
			BeginUpdate();
			Document.TextContent = FRepairCRLF.Replace(ANewText, "\r\n"); 
			Document.UndoStack.ClearAll();
			Document.BookmarkManager.Clear();
			Document.UpdateQueue.Clear();
			ActiveTextAreaControl.Caret.Position = TextLocation.Empty;
			ActiveTextAreaControl.ScrollToCaret();
			EndUpdate();

			Refresh();  // if this isn't done then half the loaded text won't show up.
		}

		public void Clear()
		{
			BeginUpdate();
			Document.TextContent = String.Empty;
			Document.UndoStack.ClearAll();
			Document.BookmarkManager.Clear();
			Document.UpdateQueue.Clear();
			ActiveTextAreaControl.Caret.Position = TextLocation.Empty;
			EndUpdate();

			Refresh();
		}

		public event InitializeTextAreaControlHandler OnInitializeTextAreaControl;

		protected override void InitializeTextAreaControl(SD.TextAreaControl ANewControl)
		{
			base.InitializeTextAreaControl(ANewControl);
			if (OnInitializeTextAreaControl != null)
				OnInitializeTextAreaControl(this, ANewControl);
		}

		#region Extensible Keyboard Handling

		protected override bool ProcessDialogKey(Keys AKey)
		{
			try
			{
				switch (AKey)
				{
					case Keys.F | Keys.Control : PromptFindReplace(false); break;
					case Keys.H | Keys.Control : PromptFindReplace(true); break;
					case Keys.F3 : FindAgain(); break;
					default : return base.ProcessDialogKey(AKey);
				}
			}
			catch (Exception LException)
			{
				Frontend.Client.Windows.Session.HandleException(LException);
			}
			return true;
			
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (editactions[keyData] != null)
				return true;
			else
				return base.IsInputKey(keyData);
		}

		public Dictionary<Keys, SD.Actions.IEditAction> EditActions { get { return editactions; } }
		
		#endregion

		#region Find / Replace

		public event EventHandler BeginningFind;
		protected virtual void DoBeginningFind()
		{
			if (BeginningFind != null)
				BeginningFind(this, EventArgs.Empty);
		}

		public event ReplacementsPerformedHandler ReplacementsPerformed;
		protected virtual void DoReplacementsPerformed(int ACount)
		{
			if (ReplacementsPerformed != null)
				ReplacementsPerformed(this, ACount);
		}

		public event EventHandler TextNotFound;
		protected virtual void DoTextNotFound()
		{
			if (TextNotFound != null)
				TextNotFound(this, EventArgs.Empty);
		}

		/// <returns> True if the Find resulting is wrapping around from the original Find location. </returns>
		public void PerformFind()
		{
			DoBeginningFind();
			if (FFindSettings.FindText != String.Empty)
			{
				Cursor LOldCursor = Cursor.Current;
				Cursor = Cursors.WaitCursor;
				BeginUpdate();
				try
				{
					switch (FFindAction)
					{
						case (FindAction.ReplaceAll):
							DoReplacementsPerformed(FindAndReplaceAll());
							break;
						case (FindAction.Replace):
							if (FindAndReplace())
								DoReplacementsPerformed(1);
							break;	
						case (FindAction.Find):
							if (!Find())
								DoTextNotFound();
							break;
					}
				}
				finally
				{
					EndUpdate();
					Invalidate();
					Cursor = LOldCursor;
				}
			}
		}

		public void PromptFindReplace(bool AReplace)
		{
			DoBeginningFind();
			using (FindReplaceForm LForm = new FindReplaceForm((AReplace ? FindAction.Replace : FindAction.Find)))
			{
				FFindSettings.SelectionOnly = false;
				if (ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				{
					if (ActiveTextAreaControl.SelectionManager.SelectedText.IndexOf("\n") != -1)
						FFindSettings.SelectionOnly = true;
					else
						FFindSettings.FindText = ActiveTextAreaControl.SelectionManager.SelectedText;
				}
				else
				{
					int LWordStart = FindWordStart(Document, ActiveTextAreaControl.Caret.Offset);
					string LFindText = Document.GetText(LWordStart, Math.Max(0, FindWordEnd(Document, LWordStart) - LWordStart));
					if (LFindText != String.Empty)
						FFindSettings.FindText = LFindText;
				}

				LForm.Settings = FFindSettings;

				if (LForm.ShowDialog() != DialogResult.OK)
					throw new AbortException();
				FFindSettings = LForm.Settings;
				FFindAction = LForm.Action;
				PerformFind();
			}
		}

		public void FindAgain()
		{
			FFindSettings.SelectionOnly = false;  // never use selection only on a repeat find, since selection was set by last find
			if (FFindAction == FindAction.ReplaceAll)
				FFindAction = FindAction.Find;  // never repeat Find and Replace All

			if (FFindSettings.FindText == String.Empty)
				PromptFindReplace(false);
			else
				PerformFind();
		}

		#endregion

		#region Find Methods

		private int GetStartOffset()
		{
			if (FFindSettings.SelectionOnly && ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				return Document.PositionToOffset(ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition);
			else
				return 0;
		}

		private int GetEndOffset()
		{
			if (FFindSettings.SelectionOnly && ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
			{
				SD.Document.ISelection LSelection = ActiveTextAreaControl.SelectionManager.SelectionCollection[0];
				return LSelection.Offset + LSelection.Length;
			}
			else
				return Document.TextLength;	// Apparently the Document.TextLength is an offset because if we subtract one we cut off the last character
		}

		public Match GetMatch(int AStartOffset, int AEndOffset, int ACaretOffset, bool AForceRightToLeft)
		{
			string LFindString = FFindSettings.FindText;
			if (!FFindSettings.UseRegEx)
				LFindString = Regex.Escape(LFindString);
			if (FFindSettings.WholeWordOnly)
				LFindString = String.Format(@"\b{0}\b", LFindString);
			Regex LRegex = new Regex(LFindString, (!FFindSettings.CaseSensitive ? RegexOptions.IgnoreCase : 0) | (AForceRightToLeft || FFindSettings.ReverseDirection ? RegexOptions.RightToLeft : 0));

			ACaretOffset = Math.Max(Math.Min(AEndOffset, ACaretOffset), AStartOffset);
			int LLength = (AForceRightToLeft || FFindSettings.ReverseDirection ? (ACaretOffset - AStartOffset) : (AEndOffset - ACaretOffset));
			int LStart = (AForceRightToLeft || FFindSettings.ReverseDirection ? AStartOffset : ACaretOffset);
			Match LMatch = LRegex.Match(Document.TextContent, LStart, LLength);

			if ((!LMatch.Success || (((LMatch.Index + LMatch.Length) > AEndOffset) || (LMatch.Index < AStartOffset))) && FFindSettings.WrapAtEnd)
			{
				if (FFindSettings.ReverseDirection)
					LMatch = LRegex.Match(Document.TextContent, ACaretOffset, (AEndOffset - ACaretOffset)); // Find from end offset
				else
					LMatch = LRegex.Match(Document.TextContent, AStartOffset, (ACaretOffset - AStartOffset)); // Find from start offset

				// Don't return a successful match if the match is out of bounds
				if (LMatch.Success && (((LMatch.Index + LMatch.Length) > AEndOffset) || (LMatch.Index < AStartOffset)))
					LMatch = Match.Empty;
			}
			return LMatch;
		}

		private Match InternalFind()
		{
			int LStartOffset = GetStartOffset();
			int LEndOffset = GetEndOffset();
			int LCaretOffset = (FFindSettings.SelectionOnly ? (FFindSettings.ReverseDirection ? LEndOffset : LStartOffset) : ActiveTextAreaControl.Caret.Offset);
			return GetMatch(LStartOffset, LEndOffset, LCaretOffset, false);
		}

		public bool Find()
		{
			Match LMatch = InternalFind();

			if (LMatch.Success)
			{
				ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new SD.Document.DefaultSelection
					(
						Document, 
						Document.OffsetToPosition(LMatch.Index), 
						Document.OffsetToPosition(LMatch.Index + LMatch.Length)
					)
				);
				if (FFindSettings.ReverseDirection)
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(LMatch.Index);		// Move the caret to the BEGINNING of the selection for backwards search
				else
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(LMatch.Index + LMatch.Length);	// Move the caret to the END of the selection for backwards search
				return true;
			}
			else
				return false;
		}

		public bool FindAndReplace()
		{		
			Match LMatch = InternalFind();
			if (LMatch.Success)
			{
				Document.Replace(LMatch.Index, LMatch.Length, FFindSettings.ReplaceText);

				ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new SD.Document.DefaultSelection
					(
						Document, 
						Document.OffsetToPosition(LMatch.Index), 
						Document.OffsetToPosition(LMatch.Index + FFindSettings.ReplaceText.Length)	// Select through the end of the replaced text
					)
				);
				if (FFindSettings.ReverseDirection)
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(LMatch.Index);		// Move the caret to the BEGINNING of the selection for backwards search
				else
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(LMatch.Index + FFindSettings.ReplaceText.Length);	// Move the caret to the END of the selection for backwards search

				return true;
			}
			else
				return false;
		}

		public int FindAndReplaceAll()
		{
			int LCount = 0;
			int LStartOffset = GetStartOffset();
			int LEndOffset = GetEndOffset();

			Match LMatch = GetMatch(LStartOffset, LEndOffset, LEndOffset, true);	// Force right to left so the indexes don't change

			if (LMatch.Success)
				ActiveTextAreaControl.SelectionManager.ClearSelection();

			ActiveTextAreaControl.TextArea.BeginUpdate();
            Document.UndoStack.StartUndoGroup();
			try
			{
				while (LMatch.Success && (LMatch.Index >= LStartOffset))
				{
					LCount++;
					Document.Replace(LMatch.Index, LMatch.Length, FFindSettings.ReplaceText);
					LMatch = LMatch.NextMatch();
				}
			}
			finally
			{
				ActiveTextAreaControl.TextArea.EndUpdate();
                Document.UndoStack.EndUndoGroup();
			}
			

		    return LCount;
		}

		#endregion

		#region Code from ICSharp for FindWordStart & FindWordEnd

		/// Copied from ICSharp TextAreaMouseHandler.cs
		/// because the FindWordStart in the ICSharp TextUtilities is broken.

		public static int FindWordStart(SD.Document.IDocument document, int offset)
		{
			SD.Document.LineSegment line = document.GetLineSegmentForOffset(offset);
			int         endPos = line.Offset + line.Length;

			if (offset == endPos)
				return offset;
			
			if (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset - 1)) && Char.IsWhiteSpace(document.GetCharAt(offset))) {
				while (offset > line.Offset && Char.IsWhiteSpace(document.GetCharAt(offset - 1))) {
					--offset;
				}
			} else  if (IsSelectableChar(document.GetCharAt(offset)) || (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset)) && IsSelectableChar(document.GetCharAt(offset - 1))))  {
				while (offset > line.Offset && IsSelectableChar(document.GetCharAt(offset - 1))) {
					--offset;
				}
			} else {
				if (offset > 0 && !Char.IsWhiteSpace(document.GetCharAt(offset - 1)) && !IsSelectableChar(document.GetCharAt(offset - 1)) ) {
					return Math.Max(0, offset - 1);
				}
			}
			return offset;
		}
		
		public static int FindWordEnd(SD.Document.IDocument document, int offset)
		{
			SD.Document.LineSegment line   = document.GetLineSegmentForOffset(offset);
			int         endPos = line.Offset + line.Length;
			
			if (offset == endPos)
				return offset;
			
			if (IsSelectableChar(document.GetCharAt(offset)))  {
				while (offset < endPos && IsSelectableChar(document.GetCharAt(offset))) {
					++offset;
				}
			} else if (Char.IsWhiteSpace(document.GetCharAt(offset))) {
				if (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset - 1))) {
					while (offset < endPos && Char.IsWhiteSpace(document.GetCharAt(offset))) {
						++offset;
					}
				}
			} else {
				return Math.Max(0, offset + 1);
			}
			
			return offset;
		}

		static bool IsSelectableChar(char ch)
		{
			return Char.IsLetterOrDigit(ch) || ch=='_';
		}

		#endregion

	}

	public delegate void ReplacementsPerformedHandler(object ASender, int ACount);

	public delegate void InitializeTextAreaControlHandler(object ASender, SD.TextAreaControl ANewControl);

	public class AllTheWayHome : SD.Actions.AbstractEditAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			TextLocation LNewPos = ATextArea.Caret.Position;
			LNewPos.X = 0;
			if (LNewPos != ATextArea.Caret.Position) 
			{
				ATextArea.Caret.Position = LNewPos;
				ATextArea.SetDesiredColumn();
			}
		}
	}

	public class ReplaceEditorAction : SD.Actions.AbstractEditAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			((TextEdit)ATextArea.Parent).PromptFindReplace(true);
		}
	}

	public class FindAgainEditorAction : SD.Actions.AbstractEditAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			((TextEdit)ATextArea.Parent).FindAgain();
		}
	}
}