using System;

using Alphora.Dataphor.DAE.Debug;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public abstract class DebugBookmark : Bookmark
	{
		public DebugBookmark(IDocument document, TextLocation location, DebugLocator locator)
			: base(document, location)
		{
			_locator = locator;
			SetMarker();
		}

		public override bool CanToggle
		{
			get { return false; }
		}

		private DebugLocator _locator;
		
		public DebugLocator Locator
		{
			get { return _locator; }
		}
		
		TextMarker _oldMarker;
		IDocument _oldDocument;

		protected abstract TextMarker CreateMarker();

		private void SetMarker()
		{
			RemoveMarker();
			if (Document != null)
			{
				TextMarker marker = CreateMarker();
				Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, LineNumber));
				Document.CommitUpdate();
				_oldMarker = marker;
			}
			_oldDocument = Document;
		}

		protected override void OnDocumentChanged(EventArgs e)
		{
			base.OnDocumentChanged(e);
			SetMarker();
		}

		public void RemoveMarker()
		{
			if (_oldDocument != null)
			{
				int from = SafeGetLineNumberForOffset(_oldDocument, _oldMarker.Offset);
				int to = SafeGetLineNumberForOffset(_oldDocument, _oldMarker.Offset + _oldMarker.Length);
				_oldDocument.MarkerStrategy.RemoveMarker(_oldMarker);
				_oldDocument.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, from, to));
				_oldDocument.CommitUpdate();
			}
			_oldDocument = null;
			_oldMarker = null;
		}

		static int SafeGetLineNumberForOffset(IDocument document, int offset)
		{
			if (offset <= 0)
				return 0;
			if (offset >= document.TextLength)
				return document.TotalNumberOfLines;
			return document.GetLineNumberForOffset(offset);
		}
	}
}
