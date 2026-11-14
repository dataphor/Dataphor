using System.Drawing;

using Alphora.Dataphor.DAE.Debug;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public class BreakpointBookmark : DebugBookmark
	{
		public BreakpointBookmark(IDocument document, TextLocation location, DebugLocator locator)
			: base(document, location, locator)
		{
		}

		static readonly Color MarkerColor = Color.FromArgb(180, 38, 38);

		public override void Draw(IconBarMargin margin, Graphics g, Point p)
		{
			margin.DrawBreakpoint(g, p.Y, IsEnabled, true);
		}

		protected override TextMarker CreateMarker()
		{
			LineSegment ALineSeg = Document.GetLineSegment(LineNumber);
			TextMarker marker = new TextMarker(ALineSeg.Offset, ALineSeg.Length, TextMarkerType.SolidBlock, MarkerColor, Color.White);
			Document.MarkerStrategy.InsertMarker(0, marker);
			return marker;
		}
	}
}
