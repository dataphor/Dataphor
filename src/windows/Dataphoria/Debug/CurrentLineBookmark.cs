using System.Drawing;

using Alphora.Dataphor.DAE.Debug;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public class CurrentLineBookmark : DebugBookmark
	{
		public CurrentLineBookmark(IDocument document, TextLocation location, DebugLocator locator)
			: base(document, location, locator)
		{
		}

		public override void Draw(IconBarMargin margin, Graphics g, Point p)
		{
			margin.DrawArrow(g, p.Y);
		}

		protected override TextMarker CreateMarker()
		{
			LineSegment lineSeg = Document.GetLineSegment(LineNumber);
			TextMarker marker = new TextMarker(lineSeg.Offset + ColumnNumber, lineSeg.Length - ColumnNumber, TextMarkerType.SolidBlock, Color.Yellow, Color.Blue);
			Document.MarkerStrategy.InsertMarker(0, marker);
			return marker;
		}
	}
}
