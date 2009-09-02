using System.Drawing;

using Alphora.Dataphor.DAE.Debug;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public class CurrentLineBookmark : DebugBookmark
	{
		public CurrentLineBookmark(IDocument ADocument, TextLocation ALocation, DebugLocator ALocator)
			: base(ADocument, ALocation, ALocator)
		{
		}

		public override void Draw(IconBarMargin margin, Graphics g, Point p)
		{
			margin.DrawArrow(g, p.Y);
		}

		protected override TextMarker CreateMarker()
		{
			LineSegment LLineSeg = Document.GetLineSegment(LineNumber);
			TextMarker LMarker = new TextMarker(LLineSeg.Offset + ColumnNumber, LLineSeg.Length - ColumnNumber, TextMarkerType.SolidBlock, Color.Yellow, Color.Blue);
			Document.MarkerStrategy.InsertMarker(0, LMarker);
			return LMarker;
		}
	}
}
