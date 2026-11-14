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
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class TextEdit : SD.TextEditorControl
	{
		private static FindSettings _findSettings = new FindSettings();
		private static FindAction _findAction = FindAction.Find;

		static TextEdit()
		{
			// This regex is to replace simple line feeds with CR/LF pairs.  For a long time the Dataphoria editor generated just LFs
			_repairCRLF = new System.Text.RegularExpressions.Regex(@"(?<!\r)\n", System.Text.RegularExpressions.RegexOptions.Compiled);
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
			Document.DocumentChanged += new ICSharpCode.TextEditor.Document.DocumentEventHandler(DocumentDocumentChanged);
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

		private static System.Text.RegularExpressions.Regex _repairCRLF;

		public string GetLineNumberText()
		{
			TextLocation position = ActiveTextAreaControl.Caret.Position;
			return String.Format
			(
				"Ln {0} Col {1}", 
				position.Y + 1,
				position.X + 1
			);
		}

		private bool _settingText;

		/// <summary> Document changed event that is only raised if the change wasn't caused by text changing. </summary>
		public event DocumentEventHandler DocumentChanged;

		private void DocumentDocumentChanged(object sender, ICSharpCode.TextEditor.Document.DocumentEventArgs args)
		{
			if (!_settingText)
				DoDocumentChanged(sender, args);
		}

		protected virtual void DoDocumentChanged(object sender, DocumentEventArgs args)
		{
			if (DocumentChanged != null)
				DocumentChanged(sender, args);
		}
		
		public void AppendText(string newText)
		{
			_settingText = true;
			BeginUpdate();
			try
			{
				TextLocation oldEndPoint = Document.OffsetToPosition(Document.TextLength);
				
				bool oldReadOnly = Document.ReadOnly;
				Document.ReadOnly = false;

				if ((Document.TextLength > 0) && (newText.Length > 0))
					Document.Insert(Document.TextLength, "\r\n");
				Document.Insert(Document.TextLength, newText);

				Document.ReadOnly = oldReadOnly;

				ActiveTextAreaControl.Caret.Position = oldEndPoint;
				ActiveTextAreaControl.ScrollToCaret();
			}
			finally
			{
				_settingText = false;
				EndUpdate();
			}

			Refresh();  // if this isn't done then half the loaded text won't show up.
		}
		
		public void SetText(string newText)
		{
			_settingText = true;
			BeginUpdate();
			try
			{
				Document.TextContent = _repairCRLF.Replace(newText, "\r\n"); 
				Document.UndoStack.ClearAll();
				Document.BookmarkManager.Clear();
				Document.UpdateQueue.Clear();
				ActiveTextAreaControl.Caret.Position = TextLocation.Empty;
				ActiveTextAreaControl.ScrollToCaret();
			}
			finally
			{
				_settingText = false;
				EndUpdate();
			}

			Refresh();  // if this isn't done then half the loaded text won't show up.
		}

		public void Clear()
		{
			_settingText = true;
			BeginUpdate();
			try
			{
				Document.TextContent = String.Empty;
				Document.UndoStack.ClearAll();
				Document.BookmarkManager.Clear();
				Document.UpdateQueue.Clear();
				ActiveTextAreaControl.Caret.Position = TextLocation.Empty;
			}
			finally
			{
				_settingText = false;
				EndUpdate();
			}

			Refresh();
		}

		public event InitializeTextAreaControlHandler OnInitializeTextAreaControl;

		protected override void InitializeTextAreaControl(SD.TextAreaControl newControl)
		{
			base.InitializeTextAreaControl(newControl);
			if (OnInitializeTextAreaControl != null)
				OnInitializeTextAreaControl(this, newControl);
		}

		#region Extensible Keyboard Handling

		protected override bool ProcessDialogKey(Keys key)
		{
			try
			{
				switch (key)
				{
					case Keys.F | Keys.Control : PromptFindReplace(false); break;
					case Keys.H | Keys.Control : PromptFindReplace(true); break;
					case Keys.F3 : FindAgain(); break;
					default : return base.ProcessDialogKey(key);
				}
			}
			catch (Exception exception)
			{
				Frontend.Client.Windows.Session.HandleException(exception);
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
		protected virtual void DoReplacementsPerformed(int count)
		{
			if (ReplacementsPerformed != null)
				ReplacementsPerformed(this, count);
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
			if (_findSettings.FindText != String.Empty)
			{
				Cursor oldCursor = Cursor.Current;
				Cursor = Cursors.WaitCursor;
				BeginUpdate();
				try
				{
					switch (_findAction)
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
					Cursor = oldCursor;
				}
			}
		}

		public void PromptFindReplace(bool replace)
		{
			DoBeginningFind();
			using (FindReplaceForm form = new FindReplaceForm((replace ? FindAction.Replace : FindAction.Find)))
			{
				_findSettings.SelectionOnly = false;
				if (ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				{
					if (ActiveTextAreaControl.SelectionManager.SelectedText.IndexOf("\n") != -1)
						_findSettings.SelectionOnly = true;
					else
						_findSettings.FindText = ActiveTextAreaControl.SelectionManager.SelectedText;
				}
				else
				{
					int wordStart = FindWordStart(Document, ActiveTextAreaControl.Caret.Offset);
					string findText = Document.GetText(wordStart, Math.Max(0, FindWordEnd(Document, wordStart) - wordStart));
					if (findText != String.Empty)
						_findSettings.FindText = findText;
				}

				form.Settings = _findSettings;

				if (form.ShowDialog() != DialogResult.OK)
					throw new AbortException();
				_findSettings = form.Settings;
				_findAction = form.Action;
				PerformFind();
			}
		}

		public void FindAgain()
		{
			_findSettings.SelectionOnly = false;  // never use selection only on a repeat find, since selection was set by last find
			if (_findAction == FindAction.ReplaceAll)
				_findAction = FindAction.Find;  // never repeat Find and Replace All

			if (_findSettings.FindText == String.Empty)
				PromptFindReplace(false);
			else
				PerformFind();
		}

		#endregion

		#region Find Methods

		private int GetStartOffset()
		{
			if (_findSettings.SelectionOnly && ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				return Document.PositionToOffset(ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition);
			else
				return 0;
		}

		private int GetEndOffset()
		{
			if (_findSettings.SelectionOnly && ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
			{
				SD.Document.ISelection selection = ActiveTextAreaControl.SelectionManager.SelectionCollection[0];
				return selection.Offset + selection.Length;
			}
			else
				return Document.TextLength;	// Apparently the Document.TextLength is an offset because if we subtract one we cut off the last character
		}

		public Match GetMatch(int startOffset, int endOffset, int caretOffset, bool forceRightToLeft)
		{
			string findString = _findSettings.FindText;
			if (!_findSettings.UseRegEx)
				findString = Regex.Escape(findString);
			if (_findSettings.WholeWordOnly)
				findString = String.Format(@"\b{0}\b", findString);
			Regex regex = new Regex(findString, (!_findSettings.CaseSensitive ? RegexOptions.IgnoreCase : 0) | (forceRightToLeft || _findSettings.ReverseDirection ? RegexOptions.RightToLeft : 0));

			caretOffset = Math.Max(Math.Min(endOffset, caretOffset), startOffset);
			int length = (forceRightToLeft || _findSettings.ReverseDirection ? (caretOffset - startOffset) : (endOffset - caretOffset));
			int start = (forceRightToLeft || _findSettings.ReverseDirection ? startOffset : caretOffset);
			Match match = regex.Match(Document.TextContent, start, length);

			if ((!match.Success || (((match.Index + match.Length) > endOffset) || (match.Index < startOffset))) && _findSettings.WrapAtEnd)
			{
				if (_findSettings.ReverseDirection)
					match = regex.Match(Document.TextContent, caretOffset, (endOffset - caretOffset)); // Find from end offset
				else
					match = regex.Match(Document.TextContent, startOffset, (caretOffset - startOffset)); // Find from start offset

				// Don't return a successful match if the match is out of bounds
				if (match.Success && (((match.Index + match.Length) > endOffset) || (match.Index < startOffset)))
					match = Match.Empty;
			}
			return match;
		}

		private Match InternalFind()
		{
			int startOffset = GetStartOffset();
			int endOffset = GetEndOffset();
			int caretOffset = (_findSettings.SelectionOnly ? (_findSettings.ReverseDirection ? endOffset : startOffset) : ActiveTextAreaControl.Caret.Offset);
			return GetMatch(startOffset, endOffset, caretOffset, false);
		}

		public bool Find()
		{
			Match match = InternalFind();

			if (match.Success)
			{
				ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new SD.Document.DefaultSelection
					(
						Document, 
						Document.OffsetToPosition(match.Index), 
						Document.OffsetToPosition(match.Index + match.Length)
					)
				);
				if (_findSettings.ReverseDirection)
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(match.Index);		// Move the caret to the BEGINNING of the selection for backwards search
				else
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(match.Index + match.Length);	// Move the caret to the END of the selection for backwards search
				return true;
			}
			else
				return false;
		}

		public bool FindAndReplace()
		{		
			Match match = InternalFind();
			if (match.Success)
			{
				Document.Replace(match.Index, match.Length, _findSettings.ReplaceText);

				ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new SD.Document.DefaultSelection
					(
						Document, 
						Document.OffsetToPosition(match.Index), 
						Document.OffsetToPosition(match.Index + _findSettings.ReplaceText.Length)	// Select through the end of the replaced text
					)
				);
				if (_findSettings.ReverseDirection)
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(match.Index);		// Move the caret to the BEGINNING of the selection for backwards search
				else
					ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(match.Index + _findSettings.ReplaceText.Length);	// Move the caret to the END of the selection for backwards search

				return true;
			}
			else
				return false;
		}

		public int FindAndReplaceAll()
		{
			int count = 0;
			int startOffset = GetStartOffset();
			int endOffset = GetEndOffset();

			Match match = GetMatch(startOffset, endOffset, endOffset, true);	// Force right to left so the indexes don't change

			if (match.Success)
				ActiveTextAreaControl.SelectionManager.ClearSelection();

			ActiveTextAreaControl.TextArea.BeginUpdate();
            Document.UndoStack.StartUndoGroup();
			try
			{
				while (match.Success && (match.Index >= startOffset))
				{
					count++;
					Document.Replace(match.Index, match.Length, _findSettings.ReplaceText);
					match = match.NextMatch();
				}
			}
			finally
			{
				ActiveTextAreaControl.TextArea.EndUpdate();
                Document.UndoStack.EndUndoGroup();
			}
			

		    return count;
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
		public override void Execute(SD.TextArea textArea)
		{
			TextLocation newPos = textArea.Caret.Position;
			newPos.X = 0;
			if (newPos != textArea.Caret.Position) 
			{
				textArea.Caret.Position = newPos;
				textArea.SetDesiredColumn();
			}
		}
	}

	public class ReplaceEditorAction : SD.Actions.AbstractEditAction
	{
		public override void Execute(SD.TextArea textArea)
		{
			((TextEdit)textArea.Parent).PromptFindReplace(true);
		}
	}

	public class FindAgainEditorAction : SD.Actions.AbstractEditAction
	{
		public override void Execute(SD.TextArea textArea)
		{
			((TextEdit)textArea.Parent).FindAgain();
		}
	}
}