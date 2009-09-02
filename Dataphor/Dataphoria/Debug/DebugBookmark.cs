using System;

using Alphora.Dataphor.DAE.Debug;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public abstract class DebugBookmark : Bookmark
	{
		public DebugBookmark(IDocument ADocument, TextLocation ALocation, DebugLocator ALocator)
			: base(ADocument, ALocation)
		{
			FLocator = ALocator;
			SetMarker();
		}

		public override bool CanToggle
		{
			get { return false; }
		}

		private DebugLocator FLocator;
		
		public DebugLocator Locator
		{
			get { return FLocator; }
		}
		
		TextMarker FOldMarker;
		IDocument FOldDocument;

		protected abstract TextMarker CreateMarker();

		private void SetMarker()
		{
			RemoveMarker();
			if (Document != null)
			{
				TextMarker LMarker = CreateMarker();
				Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, LineNumber));
				Document.CommitUpdate();
				FOldMarker = LMarker;
			}
			FOldDocument = Document;
		}

		protected override void OnDocumentChanged(EventArgs e)
		{
			base.OnDocumentChanged(e);
			SetMarker();
		}

		public void RemoveMarker()
		{
			if (FOldDocument != null)
			{
				int LFrom = SafeGetLineNumberForOffset(FOldDocument, FOldMarker.Offset);
				int LTo = SafeGetLineNumberForOffset(FOldDocument, FOldMarker.Offset + FOldMarker.Length);
				FOldDocument.MarkerStrategy.RemoveMarker(FOldMarker);
				FOldDocument.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, LFrom, LTo));
				FOldDocument.CommitUpdate();
			}
			FOldDocument = null;
			FOldMarker = null;
		}

		static int SafeGetLineNumberForOffset(IDocument ADocument, int AOffset)
		{
			if (AOffset <= 0)
				return 0;
			if (AOffset >= ADocument.TextLength)
				return ADocument.TotalNumberOfLines;
			return ADocument.GetLineNumberForOffset(AOffset);
		}
	}
}
